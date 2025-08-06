# config.py - Advanced configuration management
import os
from typing import Optional, List
from pydantic import BaseSettings, Field

class ServiceSettings(BaseSettings):
    """Service configuration settings"""
    
    # Service Configuration
    host: str = Field(default="127.0.0.1", description="Host to bind the service to")
    port: int = Field(default=8000, description="Port to bind the service to")
    log_level: str = Field(default="info", description="Logging level")
    reload: bool = Field(default=False, description="Enable auto-reload for development")
    workers: int = Field(default=1, description="Number of worker processes")
    
    # IBKR Configuration
    account_id: Optional[str] = Field(default=None, description="IBKR Account ID")
    cacert: str = Field(default="false", description="CA certificate path or false to disable SSL verification")
    
    # OAuth Configuration
    use_oauth: bool = Field(default=True, description="Use OAuth authentication")
    consumer_key: Optional[str] = Field(default=None, description="OAuth consumer key")
    consumer_secret: Optional[str] = Field(default=None, description="OAuth consumer secret")
    access_token: Optional[str] = Field(default=None, description="OAuth access token")
    access_token_secret: Optional[str] = Field(default=None, description="OAuth access token secret")
    oauth_rest_url: str = Field(default="https://api.ibkr.com/v1/api/", description="OAuth REST URL")
    
    # OAuth Behavior
    init_oauth: bool = Field(default=True, description="Initialize OAuth on startup")
    maintain_oauth: bool = Field(default=True, description="Maintain OAuth session")
    init_brokerage_session: bool = Field(default=True, description="Initialize brokerage session")
    shutdown_oauth: bool = Field(default=True, description="Shutdown OAuth on service stop")
    
    # Service Features
    enable_cors: bool = Field(default=True, description="Enable CORS")
    cors_origins: List[str] = Field(default=["*"], description="Allowed CORS origins")
    enable_health_checks: bool = Field(default=True, description="Enable health check endpoints")
    enable_metrics: bool = Field(default=False, description="Enable metrics collection")
    
    # Rate Limiting
    enable_rate_limiting: bool = Field(default=False, description="Enable rate limiting")
    rate_limit_requests: int = Field(default=100, description="Requests per minute per IP")
    
    # Security
    api_key_header: Optional[str] = Field(default=None, description="API key header name for authentication")
    required_api_key: Optional[str] = Field(default=None, description="Required API key value")
    
    # Logging
    log_file_path: Optional[str] = Field(default=None, description="Log file path")
    log_requests: bool = Field(default=False, description="Log all requests")
    log_responses: bool = Field(default=False, description="Log all responses")
    
    class Config:
        env_prefix = "IBKR_SERVICE_"
        env_file = ".env"
        case_sensitive = False

# middleware.py - Custom middleware for authentication and rate limiting
from fastapi import Request, HTTPException, status
from fastapi.responses import JSONResponse
from starlette.middleware.base import BaseHTTPMiddleware
from starlette.responses import Response
import time
from collections import defaultdict, deque
from typing import Dict, Deque
import logging

logger = logging.getLogger(__name__)

class APIKeyMiddleware(BaseHTTPMiddleware):
    """Middleware for API key authentication"""
    
    def __init__(self, app, api_key_header: str, required_api_key: str):
        super().__init__(app)
        self.api_key_header = api_key_header
        self.required_api_key = required_api_key
        self.exempt_paths = ["/docs", "/openapi.json", "/redoc", "/health"]
    
    async def dispatch(self, request: Request, call_next):
        # Skip authentication for exempt paths
        if any(request.url.path.startswith(path) for path in self.exempt_paths):
            return await call_next(request)
        
        # Check for API key
        api_key = request.headers.get(self.api_key_header)
        if not api_key or api_key != self.required_api_key:
            return JSONResponse(
                status_code=status.HTTP_401_UNAUTHORIZED,
                content={"error": "Invalid or missing API key"}
            )
        
        return await call_next(request)

class RateLimitMiddleware(BaseHTTPMiddleware):
    """Simple rate limiting middleware"""
    
    def __init__(self, app, requests_per_minute: int = 100):
        super().__init__(app)
        self.requests_per_minute = requests_per_minute
        self.requests: Dict[str, Deque[float]] = defaultdict(deque)
    
    async def dispatch(self, request: Request, call_next):
        # Get client IP
        client_ip = request.client.host
        current_time = time.time()
        
        # Clean old requests
        minute_ago = current_time - 60
        while self.requests[client_ip] and self.requests[client_ip][0] < minute_ago:
            self.requests[client_ip].popleft()
        
        # Check rate limit
        if len(self.requests[client_ip]) >= self.requests_per_minute:
            return JSONResponse(
                status_code=status.HTTP_429_TOO_MANY_REQUESTS,
                content={"error": "Rate limit exceeded"}
            )
        
        # Record this request
        self.requests[client_ip].append(current_time)
        
        return await call_next(request)

class RequestLoggingMiddleware(BaseHTTPMiddleware):
    """Middleware for request/response logging"""
    
    async def dispatch(self, request: Request, call_next):
        start_time = time.time()
        
        # Log request
        logger.info(f"Request: {request.method} {request.url.path}")
        
        response = await call_next(request)
        
        # Log response
        process_time = time.time() - start_time
        logger.info(f"Response: {response.status_code} - {process_time:.3f}s")
        
        return response

# models.py - Enhanced Pydantic models
from pydantic import BaseModel, Field, validator
from typing import Optional, List, Dict, Any, Union
from datetime import datetime
from decimal import Decimal

class BaseResponse(BaseModel):
    """Base response model"""
    success: bool = True
    timestamp: datetime = Field(default_factory=datetime.utcnow)
    
class ErrorResponse(BaseResponse):
    """Error response model"""
    success: bool = False
    error: str
    detail: Optional[str] = None
    error_code: Optional[str] = None

class PaginatedResponse(BaseResponse):
    """Paginated response model"""
    page: int
    page_size: int
    total_count: Optional[int] = None
    has_next: bool = False
    has_prev: bool = False

class EnhancedPosition(BaseModel):
    """Enhanced position model with validation"""
    conid: Optional[int] = None
    ticker: Optional[str] = None
    company_name: Optional[str] = None
    position: Optional[Decimal] = None
    market_price: Optional[Decimal] = None
    market_value: Optional[Decimal] = None
    average_cost: Optional[Decimal] = None
    unrealized_pnl: Optional[Decimal] = None
    unrealized_pnl_percent: Optional[Decimal] = None
    realized_pnl: Optional[Decimal] = None
    sector: Optional[str] = None
    security_type: Optional[str] = None
    currency: Optional[str] = None
    exchange: Optional[str] = None
    last_updated: Optional[datetime] = None
    
    @validator('position', 'market_price', 'market_value', 'average_cost', 
              'unrealized_pnl', 'realized_pnl', pre=True)
    def convert_to_decimal(cls, v):
        if v is not None:
            return Decimal(str(v))
        return v

class OrderRequestWithValidation(BaseModel):
    """Enhanced order request with validation"""
    conid: int = Field(..., description="Contract ID", gt=0)
    order_type: str = Field(..., description="Order type", regex="^(MKT|LMT|STP|STPLMT)$")
    side: str = Field(..., description="Order side", regex="^(BUY|SELL)$")
    quantity: Decimal = Field(..., description="Order quantity", gt=0)
    price: Optional[Decimal] = Field(None, description="Limit price", gt=0)
    stop_price: Optional[Decimal] = Field(None, description="Stop price", gt=0)
    time_in_force: str = Field("DAY", description="Time in force", regex="^(DAY|GTC|IOC|FOK)$")
    account_id: Optional[str] = None
    
    @validator('price')
    def validate_price_for_limit_orders(cls, v, values):
        if values.get('order_type') in ['LMT', 'STPLMT'] and v is None:
            raise ValueError('Price is required for limit orders')
        return v
    
    @validator('stop_price')
    def validate_stop_price_for_stop_orders(cls, v, values):
        if values.get('order_type') in ['STP', 'STPLMT'] and v is None:
            raise ValueError('Stop price is required for stop orders')
        return v

# utils.py - Utility functions
import asyncio
from functools import wraps
from typing import Callable, Any
import logging

logger = logging.getLogger(__name__)

def retry_on_failure(max_retries: int = 3, delay: float = 1.0):
    """Decorator for retrying failed operations"""
    def decorator(func: Callable) -> Callable:
        @wraps(func)
        async def wrapper(*args, **kwargs) -> Any:
            last_exception = None
            
            for attempt in range(max_retries + 1):
                try:
                    if asyncio.iscoroutinefunction(func):
                        return await func(*args, **kwargs)
                    else:
                        return func(*args, **kwargs)
                except Exception as e:
                    last_exception = e
                    if attempt < max_retries:
                        logger.warning(f"Attempt {attempt + 1} failed: {e}. Retrying in {delay}s...")
                        await asyncio.sleep(delay)
                    else:
                        logger.error(f"All {max_retries + 1} attempts failed")
            
            raise last_exception
        
        return wrapper
    return decorator

def validate_account_access(account_id: str, allowed_accounts: List[str]) -> bool:
    """Validate if user has access to the specified account"""
    if not allowed_accounts:  # If no restrictions, allow all
        return True
    return account_id in allowed_accounts

def format_currency(amount: Optional[Union[float, Decimal]], currency: str = "USD") -> Optional[str]:
    """Format currency amounts"""
    if amount is None:
        return None
    
    if isinstance(amount, str):
        try:
            amount = Decimal(amount)
        except:
            return None
    
    return f"{amount:,.2f} {currency}"

def calculate_position_metrics(position_data: Dict[str, Any]) -> Dict[str, Any]:
    """Calculate additional position metrics"""
    try:
        position_size = Decimal(str(position_data.get('position', 0)))
        market_price = Decimal(str(position_data.get('mktPrice', 0)))
        avg_cost = Decimal(str(position_data.get('avgCost', 0)))
        
        if position_size and avg_cost:
            cost_basis = position_size * avg_cost
            current_value = position_size * market_price
            unrealized_pnl = current_value - cost_basis
            unrealized_pnl_percent = (unrealized_pnl / cost_basis * 100) if cost_basis else Decimal(0)
            
            return {
                'cost_basis': cost_basis,
                'current_value': current_value,
                'unrealized_pnl': unrealized_pnl,
                'unrealized_pnl_percent': unrealized_pnl_percent
            }
    except Exception as e:
        logger.warning(f"Error calculating position metrics: {e}")
    
    return {}

# metrics.py - Optional metrics collection
from prometheus_client import Counter, Histogram, Gauge, generate_latest, CONTENT_TYPE_LATEST
import time

# Define metrics
REQUEST_COUNT = Counter('ibkr_api_requests_total', 'Total API requests', ['method', 'endpoint', 'status_code'])
REQUEST_DURATION = Histogram('ibkr_api_request_duration_seconds', 'Request duration', ['method', 'endpoint'])
ACTIVE_CONNECTIONS = Gauge('ibkr_api_active_connections', 'Active connections')
IBKR_CONNECTION_STATUS = Gauge('ibkr_connection_healthy', 'IBKR connection health status')

class MetricsMiddleware(BaseHTTPMiddleware):
    """Middleware for collecting metrics"""
    
    async def dispatch(self, request: Request, call_next):
        start_time = time.time()
        
        # Skip metrics collection for metrics endpoint
        if request.url.path == "/metrics":
            return await call_next(request)
        
        response = await call_next(request)
        
        # Record metrics
        REQUEST_COUNT.labels(
            method=request.method,
            endpoint=request.url.path,
            status_code=response.status_code
        ).inc()
        
        REQUEST_DURATION.labels(
            method=request.method,
            endpoint=request.url.path
        ).observe(time.time() - start_time)
        
        return response

async def update_connection_health(client):
    """Update IBKR connection health metric"""
    try:
        is_healthy = client.check_health() if client else False
        IBKR_CONNECTION_STATUS.set(1 if is_healthy else 0)
    except Exception:
        IBKR_CONNECTION_STATUS.set(0)
