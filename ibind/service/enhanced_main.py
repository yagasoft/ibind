#!/usr/bin/env python3
"""
Enhanced IBKR RESTful Web Service
A production-ready FastAPI service with advanced features like authentication, 
rate limiting, metrics, and comprehensive error handling.
"""

import os
import sys
import logging
import asyncio
from typing import Optional, List, Dict, Any
from contextlib import asynccontextmanager
from datetime import datetime

from fastapi import FastAPI, HTTPException, Query, Path, Body, Depends, Request
from fastapi.responses import JSONResponse, Response
from fastapi.middleware.cors import CORSMiddleware
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
import uvicorn

# Import configuration and utilities
from config import ServiceSettings
from models import (
    ErrorResponse, BaseResponse, PaginatedResponse, 
    EnhancedPosition, OrderRequestWithValidation
)
from middleware import (
    APIKeyMiddleware, RateLimitMiddleware, RequestLoggingMiddleware
)

# Optional metrics support
try:
    from metrics import MetricsMiddleware, update_connection_health, generate_latest, CONTENT_TYPE_LATEST
    METRICS_AVAILABLE = True
except ImportError:
    METRICS_AVAILABLE = False

from ibind import IbkrClient, ibind_logs_initialize
from ibind.oauth.oauth1a import OAuth1aConfig
from ibind.support.errors import ExternalBrokerError

# Initialize settings
settings = ServiceSettings()

# Initialize logging
ibind_logs_initialize()
logging.basicConfig(
    level=getattr(logging, settings.log_level.upper()),
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# Global client instance
ibkr_client: Optional[IbkrClient] = None

# Security dependencies
security = HTTPBearer(auto_error=False) if settings.required_api_key else None

async def verify_api_key(credentials: Optional[HTTPAuthorizationCredentials] = Depends(security)):
    """Verify API key if required"""
    if settings.required_api_key:
        if not credentials or credentials.credentials != settings.required_api_key:
            raise HTTPException(
                status_code=401,
                detail="Invalid or missing API key",
                headers={"WWW-Authenticate": "Bearer"},
            )
    return True

# Health monitoring task
async def health_monitoring_task():
    """Background task to monitor IBKR connection health"""
    while True:
        try:
            if METRICS_AVAILABLE and ibkr_client:
                await update_connection_health(ibkr_client)
            await asyncio.sleep(60)  # Check every minute
        except Exception as e:
            logger.error(f"Error in health monitoring task: {e}")
            await asyncio.sleep(60)

# Application lifecycle management
@asynccontextmanager
async def lifespan(app: FastAPI):
    """Manage application startup and shutdown"""
    global ibkr_client
    
    logger.info("Starting Enhanced IBKR Web Service...")
    logger.info(f"Configuration: OAuth={settings.use_oauth}, Rate Limiting={settings.enable_rate_limiting}, Metrics={settings.enable_metrics and METRICS_AVAILABLE}")
    
    try:
        # Initialize OAuth config if using OAuth
        oauth_config = None
        if settings.use_oauth:
            oauth_config = OAuth1aConfig(
                consumer_key=settings.consumer_key,
                consumer_secret=settings.consumer_secret,
                access_token=settings.access_token,
                access_token_secret=settings.access_token_secret,
                oauth_rest_url=settings.oauth_rest_url,
                init_oauth=settings.init_oauth,
                maintain_oauth=settings.maintain_oauth,
                init_brokerage_session=settings.init_brokerage_session,
                shutdown_oauth=settings.shutdown_oauth
            )
        
        # Initialize IBKR client
        ibkr_client = IbkrClient(
            account_id=settings.account_id,
            cacert=settings.cacert if settings.cacert != "false" else False,
            use_oauth=settings.use_oauth,
            oauth_config=oauth_config,
            log_responses=settings.log_responses
        )
        
        logger.info(f"IBKR Client initialized with account: {ibkr_client.account_id}")
        
        # Auto-select account if not specified
        if not ibkr_client.account_id:
            try:
                accounts = ibkr_client.portfolio_accounts().data
                if accounts and len(accounts) > 0:
                    ibkr_client.account_id = accounts[0]['accountId']
                    logger.info(f"Auto-selected account: {ibkr_client.account_id}")
            except Exception as e:
                logger.warning(f"Could not auto-select account: {e}")
        
        # Start background monitoring task
        if settings.enable_health_checks:
            asyncio.create_task(health_monitoring_task())
        
        yield
        
    except Exception as e:
        logger.error(f"Failed to initialize IBKR client: {e}")
        raise
    finally:
        # Cleanup
        if ibkr_client:
            logger.info("Shutting down IBKR client...")
            try:
                ibkr_client.close()
            except Exception as e:
                logger.error(f"Error during client shutdown: {e}")

# Create FastAPI application
app = FastAPI(
    title="Enhanced IBKR RESTful API Service",
    description="A production-ready RESTful web service wrapper for Interactive Brokers API using IBind library",
    version="2.0.0",
    lifespan=lifespan,
    docs_url="/docs" if not settings.required_api_key else None,  # Disable docs if API key required
    redoc_url="/redoc" if not settings.required_api_key else None
)

# Add middleware in reverse order (last added is executed first)
if settings.enable_cors:
    app.add_middleware(
        CORSMiddleware,
        allow_origins=settings.cors_origins,
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"],
    )

if settings.enable_rate_limiting:
    app.add_middleware(RateLimitMiddleware, requests_per_minute=settings.rate_limit_requests)

if settings.log_requests:
    app.add_middleware(RequestLoggingMiddleware)

if settings.enable_metrics and METRICS_AVAILABLE:
    app.add_middleware(MetricsMiddleware)

if settings.required_api_key and settings.api_key_header:
    app.add_middleware(APIKeyMiddleware, 
                      api_key_header=settings.api_key_header, 
                      required_api_key=settings.required_api_key)

# Helper functions
def handle_ibkr_error(e: Exception) -> HTTPException:
    """Convert IBind errors to HTTP exceptions"""
    if isinstance(e, ExternalBrokerError):
        status_code = getattr(e, 'status_code', 500)
        return HTTPException(status_code=status_code, detail=str(e))
    else:
        logger.error(f"Unexpected error: {e}", exc_info=True)
        return HTTPException(status_code=500, detail="Internal server error")

def get_client() -> IbkrClient:
    """Get IBKR client with validation"""
    if not ibkr_client:
        raise HTTPException(status_code=503, detail="IBKR client not initialized")
    return ibkr_client

# Enhanced endpoints with dependencies
@app.get("/health", response_model=Dict[str, Any], tags=["Health"])
async def enhanced_health_check():
    """Enhanced health check with detailed status"""
    try:
        client = get_client()
        is_healthy = client.check_health()
        
        # Get authentication status
        auth_status = {}
        try:
            auth_result = client.authentication_status()
            auth_status = auth_result.data.get('iserver', {}).get('authStatus', {})
        except Exception as e:
            logger.warning(f"Could not get auth status: {e}")
        
        return {
            "service": {
                "status": "healthy" if is_healthy else "unhealthy",
                "version": "2.0.0",
                "timestamp": datetime.utcnow().isoformat()
            },
            "ibkr": {
                "connected": is_healthy,
                "authenticated": auth_status.get('authenticated', False),
                "competing": auth_status.get('competing', False),
                "account_id": client.account_id
            },
            "features": {
                "oauth": settings.use_oauth,
                "rate_limiting": settings.enable_rate_limiting,
                "metrics": settings.enable_metrics and METRICS_AVAILABLE,
                "cors": settings.enable_cors
            }
        }
    except Exception as e:
        raise handle_ibkr_error(e)

@app.get("/metrics", tags=["Monitoring"])
async def get_metrics():
    """Prometheus metrics endpoint"""
    if not (settings.enable_metrics and METRICS_AVAILABLE):
        raise HTTPException(status_code=404, detail="Metrics not enabled")
    
    return Response(generate_latest(), media_type=CONTENT_TYPE_LATEST)

# Enhanced Portfolio Endpoints
@app.get("/portfolio/{account_id}/positions", 
         response_model=List[EnhancedPosition], 
         tags=["Portfolio"])
async def get_enhanced_positions(
    account_id: str = Path(..., description="Account ID"),
    page: int = Query(0, ge=0, description="Page number"),
    page_size: int = Query(100, ge=1, le=500, description="Items per page"),
    sort_by: Optional[str] = Query(None, description="Sort field"),
    sort_order: Optional[str] = Query("asc", regex="^(asc|desc)$", description="Sort order"),
    filter_currency: Optional[str] = Query(None, description="Filter by currency"),
    filter_sector: Optional[str] = Query(None, description="Filter by sector"),
    authenticated: bool = Depends(verify_api_key)
):
    """Get enhanced positions with filtering and sorting"""
    try:
        client = get_client()
        
        # Get positions from IBKR
        result = client.positions(
            account_id=account_id, 
            page=page,
            sort=sort_by,
            direction="d" if sort_order == "desc" else "a"
        )
        
        positions = result.data
        
        # Apply filters
        if filter_currency:
            positions = [p for p in positions if p.get('currency') == filter_currency]
        
        if filter_sector:
            positions = [p for p in positions if p.get('sector') == filter_sector]
        
        # Convert to enhanced positions
        enhanced_positions = []
        for pos in positions:
            enhanced_pos = EnhancedPosition(
                conid=pos.get('conid'),
                ticker=pos.get('ticker'),
                company_name=pos.get('companyName'),
                position=pos.get('position'),
                market_price=pos.get('mktPrice'),
                market_value=pos.get('mktValue'),
                average_cost=pos.get('avgCost'),
                unrealized_pnl=pos.get('unrealizedPnl'),
                realized_pnl=pos.get('realizedPnl'),
                sector=pos.get('sector'),
                security_type=pos.get('secType'),
                currency=pos.get('currency'),
                exchange=pos.get('listingExchange'),
                last_updated=datetime.utcnow()
            )
            
            # Calculate additional metrics
            if enhanced_pos.position and enhanced_pos.average_cost:
                cost_basis = enhanced_pos.position * enhanced_pos.average_cost
                if cost_basis and enhanced_pos.unrealized_pnl:
                    enhanced_pos.unrealized_pnl_percent = (enhanced_pos.unrealized_pnl / cost_basis) * 100
            
            enhanced_positions.append(enhanced_pos)
        
        return enhanced_positions
        
    except Exception as e:
        raise handle_ibkr_error(e)

# Enhanced Order Management
@app.post("/orders/{account_id}/place", 
          response_model=Dict[str, Any], 
          tags=["Orders"])
async def place_enhanced_order(
    account_id: str = Path(..., description="Account ID"),
    order: OrderRequestWithValidation = Body(..., description="Order details"),
    dry_run: bool = Query(False, description="Validate order without placing"),
    authenticated: bool = Depends(verify_api_key)
):
    """Place order with enhanced validation"""
    try:
        client = get_client()
        
        # Convert to IBind format
        ibind_order = {
            "conid": order.conid,
            "orderType": order.order_type,
            "side": order.side,
            "quantity": float(order.quantity),
            "tif": order.time_in_force
        }
        
        if order.price:
            ibind_order["price"] = float(order.price)
        if order.stop_price:
            ibind_order["auxPrice"] = float(order.stop_price)
        
        if dry_run:
            # Use whatif endpoint for dry run
            result = client.whatif_order(ibind_order, account_id)
        else:
            # Place actual order (empty answers for auto-handling)
            result = client.place_order(ibind_order, [], account_id)
        
        return {
            "success": True,
            "dry_run": dry_run,
            "order_details": order.dict(),
            "ibkr_response": result.data,
            "timestamp": datetime.utcnow().isoformat()
        }
        
    except Exception as e:
        raise handle_ibkr_error(e)

# Batch operations
@app.post("/market-data/batch-snapshot", 
          response_model=List[Dict[str, Any]], 
          tags=["Market Data"])
async def get_batch_market_data(
    request: Dict[str, Any] = Body(..., description="Batch market data request"),
    authenticated: bool = Depends(verify_api_key)
):
    """Get market data for multiple contracts in batch"""
    try:
        client = get_client()
        
        conids = request.get('conids', [])
        fields = request.get('fields', ['31', '84', '86'])  # Default: last, bid, ask
        
        if not conids:
            raise HTTPException(status_code=400, detail="No contract IDs provided")
        
        if len(conids) > 100:
            raise HTTPException(status_code=400, detail="Maximum 100 contracts per request")
        
        # Ensure accounts endpoint is called first
        client.receive_brokerage_accounts()
        
        # Get market data
        result = client.live_marketdata_snapshot([str(c) for c in conids], fields)
        
        # Enhance response with metadata
        enhanced_data = []
        for item in result.data:
            enhanced_item = {
                "conid": item.get('conid'),
                "data": item,
                "timestamp": datetime.utcnow().isoformat(),
                "fields_requested": fields
            }
            enhanced_data.append(enhanced_item)
        
        return enhanced_data
        
    except Exception as e:
        raise handle_ibkr_error(e)

# Account Analytics
@app.get("/analytics/{account_id}/summary", 
         response_model=Dict[str, Any], 
         tags=["Analytics"])
async def get_account_analytics(
    account_id: str = Path(..., description="Account ID"),
    include_positions: bool = Query(True, description="Include position analytics"),
    include_performance: bool = Query(True, description="Include performance metrics"),
    authenticated: bool = Depends(verify_api_key)
):
    """Get comprehensive account analytics"""
    try:
        client = get_client()
        
        analytics = {
            "account_id": account_id,
            "timestamp": datetime.utcnow().isoformat(),
            "summary": {},
            "positions": {},
            "performance": {}
        }
        
        # Get account summary
        try:
            summary_result = client.portfolio_summary(account_id)
            analytics["summary"] = summary_result.data
        except Exception as e:
            logger.warning(f"Could not get account summary: {e}")
        
        # Get ledger information
        try:
            ledger_result = client.get_ledger(account_id)
            analytics["ledger"] = ledger_result.data
        except Exception as e:
            logger.warning(f"Could not get ledger: {e}")
        
        if include_positions:
            try:
                positions_result = client.positions(account_id, 0)
                positions = positions_result.data
                
                # Calculate position analytics
                total_value = sum(float(p.get('mktValue', 0)) for p in positions if p.get('mktValue'))
                total_pnl = sum(float(p.get('unrealizedPnl', 0)) for p in positions if p.get('unrealizedPnl'))
                
                # Sector breakdown
                sectors = {}
                for pos in positions:
                    sector = pos.get('sector', 'Unknown')
                    value = float(pos.get('mktValue', 0))
                    if sector in sectors:
                        sectors[sector] += value
                    else:
                        sectors[sector] = value
                
                analytics["positions"] = {
                    "total_positions": len(positions),
                    "total_market_value": total_value,
                    "total_unrealized_pnl": total_pnl,
                    "sector_breakdown": sectors,
                    "top_positions": sorted(positions, 
                                          key=lambda x: abs(float(x.get('mktValue', 0))), 
                                          reverse=True)[:10]
                }
            except Exception as e:
                logger.warning(f"Could not get position analytics: {e}")
        
        if include_performance:
            try:
                # Get performance data for different periods
                periods = ["1D", "7D", "1M", "YTD"]
                performance = {}
                
                for period in periods:
                    try:
                        perf_result = client.account_performance([account_id], period)
                        performance[period] = perf_result.data
                    except Exception as e:
                        logger.warning(f"Could not get {period} performance: {e}")
                
                analytics["performance"] = performance
            except Exception as e:
                logger.warning(f"Could not get performance analytics: {e}")
        
        return analytics
        
    except Exception as e:
        raise handle_ibkr_error(e)

# WebSocket-style streaming endpoint (using Server-Sent Events)
@app.get("/stream/market-data/{conid}", tags=["Streaming"])
async def stream_market_data(
    conid: str = Path(..., description="Contract ID"),
    fields: str = Query("31,84,86", description="Comma-separated field IDs"),
    authenticated: bool = Depends(verify_api_key)
):
    """Stream market data using Server-Sent Events"""
    from fastapi.responses import StreamingResponse
    import json
    
    async def generate_market_data():
        client = get_client()
        field_list = fields.split(',')
        
        # Ensure brokerage accounts are called
        client.receive_brokerage_accounts()
        
        while True:
            try:
                # Get market data snapshot
                result = client.live_marketdata_snapshot([conid], field_list)
                data = result.data[0] if result.data else {}
                
                # Format as Server-Sent Event
                event_data = {
                    "conid": conid,
                    "timestamp": datetime.utcnow().isoformat(),
                    "data": data
                }
                
                yield f"data: {json.dumps(event_data)}\n\n"
                
                # Wait before next update
                await asyncio.sleep(5)  # Update every 5 seconds
                
            except Exception as e:
                error_event = {
                    "error": str(e),
                    "timestamp": datetime.utcnow().isoformat()
                }
                yield f"data: {json.dumps(error_event)}\n\n"
                await asyncio.sleep(10)  # Wait longer after error
    
    return StreamingResponse(
        generate_market_data(),
        media_type="text/event-stream",
        headers={
            "Cache-Control": "no-cache",
            "Connection": "keep-alive",
            "X-Accel-Buffering": "no"  # Disable nginx buffering
        }
    )

# Administrative endpoints
@app.post("/admin/refresh-session", tags=["Administration"])
async def refresh_session(authenticated: bool = Depends(verify_api_key)):
    """Refresh IBKR session"""
    try:
        client = get_client()
        
        if settings.use_oauth:
            # For OAuth, regenerate live session token
            client.generate_live_session_token()
            if settings.init_brokerage_session:
                client.initialize_brokerage_session()
        else:
            # For non-OAuth, try reauthentication
            result = client.reauthenticate()
        
        return {
            "success": True,
            "message": "Session refreshed successfully",
            "timestamp": datetime.utcnow().isoformat()
        }
        
    except Exception as e:
        raise handle_ibkr_error(e)

@app.get("/admin/session-status", tags=["Administration"])
async def get_session_status(authenticated: bool = Depends(verify_api_key)):
    """Get detailed session status"""
    try:
        client = get_client()
        
        # Get authentication status
        auth_result = client.authentication_status()
        
        # Get account information
        accounts_result = client.receive_brokerage_accounts()
        
        return {
            "authentication": auth_result.data,
            "accounts": accounts_result.data,
            "current_account": client.account_id,
            "oauth_enabled": settings.use_oauth,
            "session_health": client.check_health(),
            "timestamp": datetime.utcnow().isoformat()
        }
        
    except Exception as e:
        raise handle_ibkr_error(e)

# Market data utilities
@app.get("/utils/contract-search", tags=["Utilities"])
async def search_contracts_enhanced(
    symbol: str = Query(..., description="Symbol to search"),
    search_type: str = Query("symbol", regex="^(symbol|name)$", description="Search by symbol or company name"),
    sec_type: str = Query("STK", description="Security type"),
    limit: int = Query(10, ge=1, le=50, description="Maximum results"),
    authenticated: bool = Depends(verify_api_key)
):
    """Enhanced contract search with additional metadata"""
    try:
        client = get_client()
        
        # Search contracts
        result = client.search_contract_by_symbol(
            symbol=symbol,
            name=(search_type == "name"),
            sec_type=sec_type
        )
        
        contracts = result.data[:limit] if result.data else []
        
        # Enhance with additional information
        enhanced_contracts = []
        for contract in contracts:
            enhanced_contract = {
                **contract,
                "search_symbol": symbol,
                "search_type": search_type,
                "timestamp": datetime.utcnow().isoformat()
            }
            enhanced_contracts.append(enhanced_contract)
        
        return {
            "query": {
                "symbol": symbol,
                "search_type": search_type,
                "sec_type": sec_type,
                "limit": limit
            },
            "results_count": len(enhanced_contracts),
            "contracts": enhanced_contracts,
            "timestamp": datetime.utcnow().isoformat()
        }
        
    except Exception as e:
        raise handle_ibkr_error(e)

# Error handlers
@app.exception_handler(ExternalBrokerError)
async def external_broker_error_handler(request: Request, exc: ExternalBrokerError):
    """Handle IBKR API errors"""
    status_code = getattr(exc, 'status_code', 500)
    
    error_response = ErrorResponse(
        error="IBKR API Error",
        detail=str(exc),
        error_code=f"IBKR_{status_code}"
    )
    
    return JSONResponse(
        status_code=status_code,
        content=error_response.dict()
    )

@app.exception_handler(HTTPException)
async def http_exception_handler(request: Request, exc: HTTPException):
    """Handle HTTP exceptions"""
    error_response = ErrorResponse(
        error=exc.detail,
        error_code=f"HTTP_{exc.status_code}"
    )
    
    return JSONResponse(
        status_code=exc.status_code,
        content=error_response.dict()
    )

@app.exception_handler(Exception)
async def general_exception_handler(request: Request, exc: Exception):
    """Handle unexpected exceptions"""
    logger.error(f"Unhandled exception: {exc}", exc_info=True)
    
    error_response = ErrorResponse(
        error="Internal server error",
        detail="An unexpected error occurred" if not settings.log_level == "debug" else str(exc),
        error_code="INTERNAL_ERROR"
    )
    
    return JSONResponse(
        status_code=500,
        content=error_response.dict()
    )

def main():
    """Run the enhanced web service"""
    import argparse
    
    parser = argparse.ArgumentParser(description='Enhanced IBKR RESTful Web Service')
    parser.add_argument('--host', default=settings.host, help='Host to bind to')
    parser.add_argument('--port', type=int, default=settings.port, help='Port to bind to')
    parser.add_argument('--reload', action='store_true', default=settings.reload, help='Enable auto-reload')
    parser.add_argument('--log-level', default=settings.log_level, help='Log level')
    parser.add_argument('--workers', type=int, default=settings.workers, help='Number of workers (production)')
    parser.add_argument('--production', action='store_true', help='Run in production mode with gunicorn')
    
    args = parser.parse_args()
    
    # Validate required OAuth settings if using OAuth
    if settings.use_oauth:
        required_oauth_vars = ['consumer_key', 'consumer_secret', 'access_token', 'access_token_secret']
        missing_vars = [var for var in required_oauth_vars if not getattr(settings, var)]
        
        if missing_vars:
            logger.error(f"Missing required OAuth environment variables: {missing_vars}")
            logger.error("Please set the following environment variables:")
            for var in missing_vars:
                logger.error(f"  IBKR_SERVICE_{var.upper()}")
            sys.exit(1)
    
    logger.info(f"Starting Enhanced IBKR Web Service on {args.host}:{args.port}")
    logger.info(f"Features enabled: OAuth={settings.use_oauth}, Rate Limiting={settings.enable_rate_limiting}, Metrics={settings.enable_metrics and METRICS_AVAILABLE}")
    
    if args.production:
        # Use gunicorn for production
        try:
            import gunicorn.app.wsgiapp as wsgi
            
            sys.argv = [
                'gunicorn',
                '--bind', f'{args.host}:{args.port}',
                '--workers', str(args.workers),
                '--worker-class', 'uvicorn.workers.UvicornWorker',
                '--log-level', args.log_level,
                '--access-logfile', '-',
                '--error-logfile', '-',
                'main:app'
            ]
            
            wsgi.run()
        except ImportError:
            logger.error("Gunicorn not available. Install with: pip install gunicorn")
            sys.exit(1)
    else:
        # Use uvicorn for development
        uvicorn.run(
            "main:app",
            host=args.host,
            port=args.port,
            reload=args.reload,
            log_level=args.log_level,
            access_log=settings.log_requests
        )

if __name__ == "__main__":
    main()
