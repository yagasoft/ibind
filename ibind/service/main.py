#!/usr/bin/env python3
"""
IBKR RESTful Web Service
A FastAPI-based web service that wraps the IBind library to provide RESTful access to IBKR API functionality.
"""

import os
import logging
from typing import Optional, List, Dict, Any, Union
from datetime import datetime
from contextlib import asynccontextmanager

from fastapi import FastAPI, HTTPException, Query, Path, Body, status
from fastapi.responses import JSONResponse
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field
import uvicorn

from ibind import IbkrClient, ibind_logs_initialize
from ibind.oauth.oauth1a import OAuth1aConfig
from ibind.support.errors import ExternalBrokerError

# Initialize logging
ibind_logs_initialize()
logger = logging.getLogger(__name__)

# Global client instance
ibkr_client: Optional[IbkrClient] = None

# Pydantic models for request/response validation
class ErrorResponse(BaseModel):
    error: str
    detail: Optional[str] = None
    status_code: int

class HealthResponse(BaseModel):
    status: str
    authenticated: bool
    connected: bool
    account_id: Optional[str] = None

class AccountInfo(BaseModel):
    accountId: str
    accountVan: Optional[str] = None
    accountTitle: Optional[str] = None
    displayName: Optional[str] = None
    accountAlias: Optional[str] = None
    accountStatus: Optional[int] = None
    currency: Optional[str] = None
    type: Optional[str] = None
    tradingType: Optional[str] = None
    faclient: Optional[bool] = None
    clearingStatus: Optional[str] = None

class Position(BaseModel):
    conid: Optional[int] = None
    ticker: Optional[str] = None
    position: Optional[float] = None
    mktPrice: Optional[float] = None
    mktValue: Optional[float] = None
    avgCost: Optional[float] = None
    unrealizedPnl: Optional[float] = None
    realizedPnl: Optional[float] = None
    sector: Optional[str] = None
    secType: Optional[str] = None
    currency: Optional[str] = None

class OrderRequest(BaseModel):
    conid: int = Field(..., description="Contract ID")
    orderType: str = Field(..., description="Order type (e.g., 'MKT', 'LMT')")
    side: str = Field(..., description="Order side ('BUY' or 'SELL')")
    quantity: float = Field(..., description="Order quantity")
    price: Optional[float] = Field(None, description="Limit price (required for limit orders)")
    tif: Optional[str] = Field("DAY", description="Time in force")
    auxPrice: Optional[float] = Field(None, description="Aux price for stop orders")

class MarketDataRequest(BaseModel):
    conids: List[int] = Field(..., description="List of contract IDs")
    fields: List[str] = Field(default=["31", "84", "86"], description="Market data fields")

class StockSearchRequest(BaseModel):
    symbol: str = Field(..., description="Stock symbol to search for")
    name: Optional[bool] = Field(False, description="Search by company name instead of symbol")
    secType: Optional[str] = Field("STK", description="Security type")

# Application lifecycle management
@asynccontextmanager
async def lifespan(app: FastAPI):
    """Manage application startup and shutdown"""
    global ibkr_client
    
    logger.info("Starting IBKR Web Service...")
    
    try:
        # Initialize IBKR client
        cacert = os.getenv('IBIND_CACERT', False)
        account_id = os.getenv('IBIND_ACCOUNT_ID')
        
        ibkr_client = IbkrClient(
            cacert=cacert,
            use_oauth=True,
            account_id=account_id
        )
        
        logger.info(f"IBKR Client initialized with account: {ibkr_client.account_id}")
        
        # Set account ID if not set and accounts are available
        if not ibkr_client.account_id:
            try:
                accounts = ibkr_client.portfolio_accounts().data
                if accounts and len(accounts) > 0:
                    ibkr_client.account_id = accounts[0]['accountId']
                    logger.info(f"Auto-selected account: {ibkr_client.account_id}")
            except Exception as e:
                logger.warning(f"Could not auto-select account: {e}")
        
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
    title="IBKR RESTful API Service",
    description="A RESTful web service wrapper for Interactive Brokers API using IBind library",
    version="1.0.0",
    lifespan=lifespan
)

# Add CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

def handle_ibkr_error(e: Exception) -> HTTPException:
    """Convert IBind errors to HTTP exceptions"""
    if isinstance(e, ExternalBrokerError):
        return HTTPException(
            status_code=e.status_code if hasattr(e, 'status_code') else 500,
            detail=str(e)
        )
    else:
        logger.error(f"Unexpected error: {e}")
        return HTTPException(status_code=500, detail=str(e))

# Health and Status Endpoints
@app.get("/health", response_model=HealthResponse, tags=["Health"])
async def health_check():
    """Check the health and authentication status of the IBKR connection"""
    try:
        if not ibkr_client:
            raise HTTPException(status_code=503, detail="IBKR client not initialized")
        
        is_healthy = ibkr_client.check_health()
        
        return HealthResponse(
            status="healthy" if is_healthy else "unhealthy",
            authenticated=is_healthy,
            connected=is_healthy,
            account_id=ibkr_client.account_id
        )
    except Exception as e:
        raise handle_ibkr_error(e)

@app.get("/tickle", tags=["Health"])
async def tickle():
    """Ping the server to maintain the session"""
    try:
        if not ibkr_client:
            raise HTTPException(status_code=503, detail="IBKR client not initialized")
        
        result = ibkr_client.tickle()
        return result.data
    except Exception as e:
        raise handle_ibkr_error(e)

# Account Endpoints
@app.get("/portfolio/accounts", response_model=List[AccountInfo], tags=["Accounts"])
async def get_portfolio_accounts():
    """Get list of portfolio accounts"""
    try:
        if not ibkr_client:
            raise HTTPException(status_code=503, detail="IBKR client not initialized")
        
        result = ibkr_client.portfolio_accounts()
        return result.data
    except Exception as e:
        raise handle_ibkr_error(e)

@app.get("/iserver/accounts", tags=["Accounts"])
async def get_brokerage_accounts():
    """Get brokerage accounts"""
    try:
        if not ibkr_client:
            raise HTTPException(status_code=503, detail="IBKR client not initialized")
        
        result = ibkr_client.receive_brokerage_accounts()
        return result.data
    except Exception as e:
        raise handle_ibkr_error(e)

@app.get("/portfolio/{account_id}/summary", tags=["Accounts"])
async def get_account_summary(account_id: str = Path(..., description="Account ID")):
    """Get account summary information"""
    try:
        if not ibkr_client:
            raise HTTPException(status_code=503, detail="IBKR client not initialized")
        
        result = ibkr_client.account_summary(account_id)
        return result.data
    except Exception as e:
        raise handle_ibkr_error(e)

@app.get("/portfolio/{account_id}/ledger", tags=["Accounts"])
async def get_ledger(account_id: str = Path(..., description="Account ID")):
    """Get account ledger information"""
    try:
        if not ibkr_client:
            raise HTTPException(status_code=503, detail="IBKR client not initialized")
        
        result = ibkr_client.get_ledger(account_id)
        return result.data
    except Exception as e:
        raise handle_ibkr_error(e)

# Portfolio Endpoints
@app.get("/portfolio/{account_id}/positions/{page}", response_model=List[Position], tags=["Portfolio"])
async def get_positions(
    account_id: str = Path(..., description="Account ID"),
    page: int = Path(0, description="Page number"),
    model: Optional[str] = Query(None, description="Model code"),
    sort: Optional[str] = Query(None, description="Sort column"),
    direction: Optional[str] = Query(None, description="Sort direction (a/d)"),
    period: Optional[str] = Query(None, description="Period for PnL (1D, 7D, 1M)")
):
    """Get positions for account with pagination"""
    try:
        if not ibkr_client:
            raise HTTPException(status_code=503, detail="IBKR client not initialized")
        
        result = ibkr_client.positions(account_id, page, model, sort, direction, period)
        return result.data
    except Exception as e:
        raise handle_ibkr_error(e)

@app.get("/portfolio/{account_id}/positions", response_model=List[Position], tags=["Portfolio"])
async def get_all_positions(account_id: str = Path(..., description="Account ID")):
    """Get all positions for account (simplified endpoint)"""
    try:
        if not ibkr_client:
            raise HTTPException(status_code=503, detail="IBKR client not initialized")
        
        result = ibkr_client.positions(account_id, 0)
        return result.data
    except Exception as e:
        raise handle_ibkr_error(e)

# Market Data Endpoints
@app.post("/iserver/marketdata/snapshot", tags=["Market Data"])
async def get_market_data_snapshot(request: MarketDataRequest):
    """Get market data snapshot for specified contract IDs"""
    try:
        if not ibkr_client:
            raise HTTPException(status_code=503, detail="IBKR client not initialized")
        
        # Ensure brokerage accounts are called first (required by IBKR)
        ibkr_client.receive_brokerage_accounts()
        
        conids = [str(conid) for conid in request.conids]
        result = ibkr_client.live_marketdata_snapshot(conids, request.fields)
        return result.data
    except Exception as e:
        raise handle_ibkr_error(e)

@app.get("/iserver/marketdata/history", tags=["Market Data"])
async def get_market_data_history(
    conid: str = Query(..., description="Contract ID"),
    bar: str = Query(..., description="Bar size (1min, 5min, 1h, 1d, etc.)"),
    exchange: Optional[str] = Query(None, description="Exchange"),
    period: Optional[str] = Query("1w", description="Time period"),
    outside_rth: Optional[bool] = Query(None, description="Include outside regular trading hours"),
    start_time: Optional[str] = Query(None, description="Start time (YYYYMMDD-HH:mm:SS)")
):
    """Get historical market data"""
    try:
        if not ibkr_client:
            raise HTTPException(status_code=503, detail="IBKR client not initialized")
        
        # Convert start_time string to datetime if provided
        start_dt = None
        if start_time:
            try:
                start_dt = datetime.strptime(start_time, "%Y%m%d-%H:%M:%S")
            except ValueError:
                raise HTTPException(status_code=400, detail="Invalid start_time format. Use YYYYMMDD-HH:mm:SS")
        
        result = ibkr_client.marketdata_history_by_conid(
            conid, bar, exchange, period, outside_rth, start_dt
        )
        return result.data
    except Exception as e:
        raise handle_ibkr_error(e)

# Contract Search Endpoints
@app.post("/iserver/secdef/search", tags=["Contracts"])
async def search_contracts(request: StockSearchRequest):
    """Search for contracts by symbol"""
    try:
        if not ibkr_client:
            raise HTTPException(status_code=503, detail="IBKR client not initialized")
        
        result = ibkr_client.search_contract_by_symbol(
            request.symbol, request.name, request.sec_type
        )
        return result.data
    except Exception as e:
        raise handle_ibkr_error(e)

@app.get("/trsrv/secdef", tags=["Contracts"])
async def get_security_definition(
    conids: str = Query(..., description="Comma-separated contract IDs")
):
    """Get security definitions by contract IDs"""
    try:
        if not ibkr_client:
            raise HTTPException(status_code=503, detail="IBKR client not initialized")
        
        conid_list = conids.split(',')
        result = ibkr_client.security_definition_by_conid(conid_list)
        return result.data
    except Exception as e:
        raise handle_ibkr_error(e)

# Order Management Endpoints
@app.get("/iserver/account/orders", tags=["Orders"])
async def get_live_orders(
    filters: Optional[str] = Query(None, description="Comma-separated order filters"),
    force: Optional[bool] = Query(None, description="Force refresh"),
    account_id: Optional[str] = Query(None, description="Account ID")
):
    """Get live orders"""
    try:
        if not ibkr_client:
            raise HTTPException(status_code=503, detail="IBKR client not initialized")
        
        filter_list = filters.split(',') if filters else None
        result = ibkr_client.live_orders(filter_list, force, account_id)
        return result.data
    except Exception as e:
        raise handle_ibkr_error(e)

@app.post("/iserver/account/{account_id}/orders", tags=["Orders"])
async def place_order(
    account_id: str = Path(..., description="Account ID"),
    order_request: OrderRequest = Body(..., description="Order details")
):
    """Place a new order"""
    try:
        if not ibkr_client:
            raise HTTPException(status_code=503, detail="IBKR client not initialized")
        
        # Convert OrderRequest to the format expected by IBind
        order_dict = {
            "conid": order_request.conid,
            "orderType": order_request.orderType,
            "side": order_request.side,
            "quantity": order_request.quantity,
            "tif": order_request.tif
        }
        
        if order_request.price is not None:
            order_dict["price"] = order_request.price
        if order_request.auxPrice is not None:
            order_dict["auxPrice"] = order_request.auxPrice
        
        # Empty answers for automatic handling - in production you might want more sophisticated handling
        answers = []
        
        result = ibkr_client.place_order(order_dict, answers, account_id)
        return result.data
    except Exception as e:
        raise handle_ibkr_error(e)

@app.get("/iserver/account/trades", tags=["Orders"])
async def get_trades(
    days: Optional[str] = Query(None, description="Number of days"),
    account_id: Optional[str] = Query(None, description="Account ID")
):
    """Get trade executions"""
    try:
        if not ibkr_client:
            raise HTTPException(status_code=503, detail="IBKR client not initialized")
        
        result = ibkr_client.trades(days, account_id)
        return result.data
    except Exception as e:
        raise handle_ibkr_error(e)

# Authentication Endpoints
@app.post("/iserver/auth/status", tags=["Authentication"])
async def get_auth_status():
    """Get current authentication status"""
    try:
        if not ibkr_client:
            raise HTTPException(status_code=503, detail="IBKR client not initialized")
        
        result = ibkr_client.authentication_status()
        return result.data
    except Exception as e:
        raise handle_ibkr_error(e)

@app.post("/logout", tags=["Authentication"])
async def logout():
    """Logout from IBKR session"""
    try:
        if not ibkr_client:
            raise HTTPException(status_code=503, detail="IBKR client not initialized")
        
        result = ibkr_client.logout()
        return result.data
    except Exception as e:
        raise handle_ibkr_error(e)

# Error handlers
@app.exception_handler(ExternalBrokerError)
async def external_broker_error_handler(request, exc: ExternalBrokerError):
    return JSONResponse(
        status_code=getattr(exc, 'status_code', 500),
        content={"error": "IBKR API Error", "detail": str(exc)}
    )

@app.exception_handler(HTTPException)
async def http_exception_handler(request, exc: HTTPException):
    return JSONResponse(
        status_code=exc.status_code,
        content={"error": exc.detail}
    )

def main():
    """Run the web service"""
    import argparse
    
    parser = argparse.ArgumentParser(description='IBKR RESTful Web Service')
    parser.add_argument('--host', default='127.0.0.1', help='Host to bind to')
    parser.add_argument('--port', type=int, default=8000, help='Port to bind to')
    parser.add_argument('--reload', action='store_true', help='Enable auto-reload for development')
    parser.add_argument('--log-level', default='info', help='Log level')
    
    args = parser.parse_args()
    
    logger.info(f"Starting IBKR Web Service on {args.host}:{args.port}")
    
    uvicorn.run(
        "main:app",
        host=args.host,
        port=args.port,
        reload=args.reload,
        log_level=args.log_level
    )

if __name__ == "__main__":
    main()
