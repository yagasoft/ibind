# Complete IBKR RESTful Web Service Deployment Guide

## 📁 Project Structure

Add these files to your IBind repository:

```
ibind/
├── service/                           # New web service directory
│   ├── main.py                       # Basic FastAPI service
│   ├── enhanced_main.py              # Production-ready service with advanced features
│   ├── config.py                     # Configuration management
│   ├── models.py                     # Pydantic data models
│   ├── middleware.py                 # Custom middleware (auth, rate limiting, etc.)
│   ├── metrics.py                    # Prometheus metrics (optional)
│   ├── utils.py                      # Utility functions
│   ├── requirements-service.txt      # Service dependencies
│   ├── .env.example                  # Environment configuration template
│   ├── docker-compose.yml           # Docker deployment
│   ├── Dockerfile                   # Docker image
│   ├── scripts/
│   │   ├── start_service.sh         # Service startup script
│   │   ├── stop_service.sh          # Service shutdown script
│   │   ├── health_check.sh          # Health monitoring script
│   │   ├── install_dependencies.sh  # Dependency installation
│   │   └── test_service.sh          # Basic testing script
│   ├── clients/
│   │   ├── csharp/
│   │   │   ├── IBKRApiClient.cs     # C# .NET client
│   │   │   └── Program.cs           # C# example usage
│   │   ├── python/
│   │   │   └── client_example.py    # Python client example
│   │   └── javascript/
│   │       └── client_example.js    # Node.js client example
│   ├── docs/
│   │   ├── README.md               # Service documentation
│   │   ├── API.md                  # API endpoint documentation
│   │   ├── DEPLOYMENT.md           # Deployment instructions
│   │   └── EXAMPLES.md             # Usage examples
│   └── tests/
│       ├── test_endpoints.py       # API endpoint tests
│       ├── test_auth.py           # Authentication tests
│       └── conftest.py            # Test configuration
```

## 🚀 Quick Start (Basic Service)

### 1. Basic Setup

```bash
# Navigate to your IBind repository
cd /path/to/ibind

# Create service directory
mkdir -p service/scripts service/clients/csharp service/docs service/tests

# Copy the main service files
# (Copy main.py, requirements-service.txt, .env.example from the artifacts above)

# Install dependencies
cd service
pip install -r requirements-service.txt
```

### 2. Configure Environment

```bash
# Copy and edit environment file
cp .env.example .env

# Edit .env with your IBKR OAuth credentials
nano .env
```

Required environment variables:
```bash
IBKR_SERVICE_ACCOUNT_ID=your_account_id
IBKR_SERVICE_CONSUMER_KEY=your_consumer_key
IBKR_SERVICE_CONSUMER_SECRET=your_consumer_secret
IBKR_SERVICE_ACCESS_TOKEN=your_access_token
IBKR_SERVICE_ACCESS_TOKEN_SECRET=your_access_token_secret
```

### 3. Run Basic Service

```bash
# Run directly
python main.py

# Or with custom settings
python main.py --host 0.0.0.0 --port 8000 --reload
```

## 🏭 Production Deployment (Enhanced Service)

### 1. Enhanced Setup

```bash
# Use the enhanced service with all advanced features
# Copy all the advanced configuration files (config.py, models.py, etc.)

# Install production dependencies
pip install gunicorn prometheus-client structlog
```

### 2. Production Configuration

Create a comprehensive `.env` file:

```bash
# Service Configuration
IBKR_SERVICE_HOST=0.0.0.0
IBKR_SERVICE_PORT=8000
IBKR_SERVICE_LOG_LEVEL=info
IBKR_SERVICE_WORKERS=4

# IBKR OAuth Configuration
IBKR_SERVICE_USE_OAUTH=true
IBKR_SERVICE_ACCOUNT_ID=your_account_id
IBKR_SERVICE_CONSUMER_KEY=your_consumer_key
IBKR_SERVICE_CONSUMER_SECRET=your_consumer_secret
IBKR_SERVICE_ACCESS_TOKEN=your_access_token
IBKR_SERVICE_ACCESS_TOKEN_SECRET=your_access_token_secret

# OAuth Behavior
IBKR_SERVICE_INIT_OAUTH=true
IBKR_SERVICE_MAINTAIN_OAUTH=true
IBKR_SERVICE_INIT_BROKERAGE_SESSION=true
IBKR_SERVICE_SHUTDOWN_OAUTH=true

# Security
IBKR_SERVICE_API_KEY_HEADER=X-API-Key
IBKR_SERVICE_REQUIRED_API_KEY=your_secure_api_key_here

# Features
IBKR_SERVICE_ENABLE_CORS=true
IBKR_SERVICE_ENABLE_RATE_LIMITING=true
IBKR_SERVICE_RATE_LIMIT_REQUESTS=100
IBKR_SERVICE_ENABLE_METRICS=true

# Logging
IBKR_SERVICE_LOG_REQUESTS=true
IBKR_SERVICE_LOG_RESPONSES=false
```

### 3. Run Production Service

```bash
# Make scripts executable
chmod +x scripts/*.sh

# Install dependencies
./scripts/install_dependencies.sh prod

# Start production service with gunicorn
./scripts/start_service.sh prod

# Or run enhanced service directly
python enhanced_main.py --production --workers 4
```

## 🐳 Docker Deployment

### 1. Build and Run with Docker

```bash
# Build image
docker build -t ibkr-service .

# Run container
docker run -d \
  --name ibkr-service \
  -p 8000:8000 \
  --env-file .env \
  --restart unless-stopped \
  ibkr-service

# Or use docker-compose
docker-compose up -d
```

### 2. Docker Compose with Monitoring

```yaml
# docker-compose.prod.yml
version: '3.8'

services:
  ibkr-service:
    build: .
    ports:
      - "8000:8000"
    environment:
      - IBKR_SERVICE_HOST=0.0.0.0
      - IBKR_SERVICE_ENABLE_METRICS=true
    env_file:
      - .env
    volumes:
      - ./logs:/app/logs
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8000/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml
    depends_on:
      - ibkr-service

  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
    volumes:
      - grafana-storage:/var/lib/grafana
    depends_on:
      - prometheus

volumes:
  grafana-storage:
```

## 🔧 Load Balancer Configuration

### Nginx Configuration

```nginx
# /etc/nginx/sites-available/ibkr-api
upstream ibkr_backend {
    least_conn;
    server 127.0.0.1:8000 max_fails=3 fail_timeout=30s;
    server 127.0.0.1:8001 max_fails=3 fail_timeout=30s;
    server 127.0.0.1:8002 max_fails=3 fail_timeout=30s;
}

server {
    listen 80;
    server_name your-api-domain.com;

    # Redirect HTTP to HTTPS
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name your-api-domain.com;

    # SSL Configuration
    ssl_certificate /path/to/your/certificate.pem;
    ssl_certificate_key /path/to/your/private.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES256-GCM-SHA512:DHE-RSA-AES256-GCM-SHA512;

    # Security Headers
    add_header X-Content-Type-Options nosniff;
    add_header X-Frame-Options DENY;
    add_header X-XSS-Protection "1; mode=block";
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains";

    # Rate Limiting
    limit_req_zone $binary_remote_addr zone=api_limit:10m rate=10r/s;
    limit_req zone=api_limit burst=20 nodelay;

    # Proxy Configuration
    location / {
        proxy_pass http://ibkr_backend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # Timeouts
        proxy_connect_timeout 30s;
        proxy_send_timeout 30s;
        proxy_read_timeout 30s;
        
        # Buffering
        proxy_buffering off;
        proxy_request_buffering off;
    }

    # Health check endpoint
    location /health {
        proxy_pass http://ibkr_backend/health;
        access_log off;
    }

    # Static files (if any)
    location /static/ {
        alias /path/to/static/files/;
        expires 1y;
        add_header Cache-Control "public, immutable";
    }

    # Metrics endpoint (restrict access)
    location /metrics {
        allow 127.0.0.1;
        allow 10.0.0.0/8;
        deny all;
        proxy_pass http://ibkr_backend/metrics;
    }
}
```

## 🔍 Monitoring and Alerting

### 1. Health Monitoring Script

```bash
#!/bin/bash
# monitoring/health_monitor.sh

SERVICE_URL="https://your-api-domain.com"
WEBHOOK_URL="https://hooks.slack.com/your/webhook/url"  # Optional Slack webhook

check_health() {
    local response=$(curl -s -w "%{http_code}" -o /dev/null "$SERVICE_URL/health")
    echo $response
}

send_alert() {
    local message=$1
    echo "$(date): $message" >> /var/log/ibkr-service-alerts.log
    
    # Optional: Send Slack notification
    if [ ! -z "$WEBHOOK_URL" ]; then
        curl -X POST -H 'Content-type: application/json' \
            --data "{\"text\":\"IBKR Service Alert: $message\"}" \
            "$WEBHOOK_URL"
    fi
}

main() {
    local health_code=$(check_health)
    
    if [ "$health_code" != "200" ]; then
        send_alert "Service health check failed (HTTP $health_code)"
        exit 1
    else
        echo "$(date): Service is healthy"
        exit 0
    fi
}

main
```

### 2. Prometheus Configuration

```yaml
# monitoring/prometheus.yml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'ibkr-service'
    static_configs:
      - targets: ['ibkr-service:8000']
    scrape_interval: 10s
    metrics_path: '/metrics'

rule_files:
  - "alert_rules.yml"

alerting:
  alertmanagers:
    - static_configs:
        - targets:
          - alertmanager:9093
```

### 3. Grafana Dashboard

```json
{
  "dashboard": {
    "title": "IBKR API Service Dashboard",
    "panels": [
      {
        "title": "Request Rate",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(ibkr_api_requests_total[5m])",
            "legendFormat": "{{method}} {{endpoint}}"
          }
        ]
      },
      {
        "title": "Response Times",
        "type": "graph", 
        "targets": [
          {
            "expr": "histogram_quantile(0.95, rate(ibkr_api_request_duration_seconds_bucket[5m]))",
            "legendFormat": "95th percentile"
          }
        ]
      },
      {
        "title": "IBKR Connection Health",
        "type": "singlestat",
        "targets": [
          {
            "expr": "ibkr_connection_healthy",
            "legendFormat": "Healthy"
          }
        ]
      }
    ]
  }
}
```

## 🧪 Testing

### 1. Unit Tests

```python
# tests/test_endpoints.py
import pytest
from fastapi.testclient import TestClient
from unittest.mock import Mock, patch

@pytest.fixture
def client():
    from main import app
    return TestClient(app)

@pytest.fixture
def mock_ibkr_client():
    with patch('main.ibkr_client') as mock:
        mock_client = Mock()
        mock_client.check_health.return_value = True
        mock_client.account_id = "DU12345"
        mock.return_value = mock_client
        yield mock_client

def test_health_endpoint(client, mock_ibkr_client):
    response = client.get("/health")
    assert response.status_code == 200
    data = response.json()
    assert data["authenticated"] == True

def test_accounts_endpoint(client, mock_ibkr_client):
    mock_ibkr_client.portfolio_accounts.return_value.data = [
        {"accountId": "DU12345", "displayName": "Test Account"}
    ]
    
    response = client.get("/portfolio/accounts")
    assert response.status_code == 200
    assert len(response.json()) == 1
```

### 2. Integration Tests

```bash
# Run integration tests
./scripts/test_service.sh

# Or run specific test scenarios
pytest tests/ -v
```

### 3. Load Testing

```python
# tests/load_test.py
import asyncio
import aiohttp
import time

async def make_request(session, url):
    async with session.get(url) as response:
        return response.status

async def load_test():
    url = "http://127.0.0.1:8000/health"
    concurrent_requests = 100
    total_requests = 1000
    
    async with aiohttp.ClientSession() as session:
        start_time = time.time()
        
        tasks = []
        for i in range(total_requests):
            task = make_request(session, url)
            tasks.append(task)
            
            if len(tasks) >= concurrent_requests:
                results = await asyncio.gather(*tasks, return_exceptions=True)
                tasks = []
        
        # Handle remaining tasks
        if tasks:
            await asyncio.gather(*tasks, return_exceptions=True)
        
        end_time = time.time()
        print(f"Completed {total_requests} requests in {end_time - start_time:.2f} seconds")

if __name__ == "__main__":
    asyncio.run(load_test())
```

## 📊 Performance Optimization

### 1. Connection Pooling

```python
# In your service configuration
import asyncio
from ibind import IbkrClient

class ConnectionPool:
    def __init__(self, size=5):
        self.size = size
        self.connections = asyncio.Queue(maxsize=size)
        self._initialize_pool()
    
    def _initialize_pool(self):
        for _ in range(self.size):
            client = IbkrClient(use_oauth=True)
            self.connections.put_nowait(client)
    
    async def acquire(self):
        return await self.connections.get()
    
    async def release(self, connection):
        await self.connections.put(connection)
```

### 2. Caching Strategy

```python
# Add caching for frequently requested data
import redis
import json
from functools import wraps

redis_client = redis.Redis(host='localhost', port=6379, db=0)

def cache_result(ttl=60):
    def decorator(func):
        @wraps(func)
        async def wrapper(*args, **kwargs):
            # Generate cache key
            cache_key = f"{func.__name__}:{hash(str(args) + str(kwargs))}"
            
            # Try to get from cache
            cached = redis_client.get(cache_key)
            if cached:
                return json.loads(cached)
            
            # Execute function and cache result
            result = await func(*args, **kwargs)
            redis_client.setex(cache_key, ttl, json.dumps(result))
            
            return result
        return wrapper
    return decorator
```

## 🔐 Security Checklist

- [ ] **API Authentication**: Implement API key or JWT authentication
- [ ] **Rate Limiting**: Configure appropriate rate limits
- [ ] **HTTPS**: Use SSL/TLS certificates
- [ ] **CORS**: Configure CORS policies properly
- [ ] **Input Validation**: Validate all input parameters
- [ ] **Logging**: Log security events and access attempts
- [ ] **Error Handling**: Don't expose sensitive information in errors
- [ ] **Environment Variables**: Secure storage of credentials
- [ ] **Network Security**: Use VPC/private networks
- [ ] **Regular Updates**: Keep dependencies updated

## 🚀 C# .NET Integration Examples

### 1. Basic Console Application

```csharp
// Program.cs
using IBKRClient;
using System.Text.Json;

var client = new IBKRApiClient("https://your-api-domain.com");

try
{
    // Check service health
    var health = await client.GetHealthAsync();
    Console.WriteLine($"Service Status: {health.Status}");
    
    if (!health.Authenticated)
    {
        Console.WriteLine("Service not authenticated with IBKR");
        return;
    }
    
    // Get accounts
    var accounts = await client.GetPortfolioAccountsAsync();
    Console.WriteLine($"Found {accounts.Count} accounts:");
    
    foreach (var account in accounts)
    {
        Console.WriteLine($"  {account.AccountId}: {account.DisplayName}");
        
        // Get positions for this account
        var positions = await client.GetPositionsAsync(account.AccountId);
        Console.WriteLine($"    {positions.Count} positions");
        
        foreach (var position in positions.Take(5))
        {
            Console.WriteLine($"      {position.Ticker}: {position.PositionSize} @ ${position.MktPrice}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

### 2. ASP.NET Core Web API Integration

```csharp
// Controllers/TradingController.cs
using Microsoft.AspNetCore.Mvc;
using IBKRClient;

[ApiController]
[Route("api/[controller]")]
public class TradingController : ControllerBase
{
    private readonly IBKRApiClient _ibkrClient;
    private readonly ILogger<TradingController> _logger;
    
    public TradingController(IBKRApiClient ibkrClient, ILogger<TradingController> logger)
    {
        _ibkrClient = ibkrClient;
        _logger = logger;
    }
    
    [HttpGet("health")]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var health = await _ibkrClient.GetHealthAsync();
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, "Service unavailable");
        }
    }
    
    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts()
    {
        try
        {
            var accounts = await _ibkrClient.GetPortfolioAccountsAsync();
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get accounts");
            return StatusCode(500, "Failed to retrieve accounts");
        }
    }
    
    [HttpGet("accounts/{accountId}/positions")]
    public async Task<IActionResult> GetPositions(string accountId)
    {
        try
        {
            var positions = await _ibkrClient.GetPositionsAsync(accountId);
            return Ok(positions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get positions for account {AccountId}", accountId);
            return StatusCode(500, "Failed to retrieve positions");
        }
    }
    
    [HttpPost("accounts/{accountId}/orders")]
    public async Task<IActionResult> PlaceOrder(string accountId, [FromBody] OrderRequest order)
    {
        try
        {
            var result = await _ibkrClient.PlaceOrderAsync(accountId, order);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to place order for account {AccountId}", accountId);
            return StatusCode(500, "Failed to place order");
        }
    }
}

// Program.cs (ASP.NET Core setup)
using IBKRClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IBKRApiClient>(provider => 
{
    var config = provider.GetService<IConfiguration>();
    var baseUrl = config["IBKRService:BaseUrl"] ?? "http://localhost:8000";
    return new IBKRApiClient(baseUrl);
});

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Run();
```

### 3. Background Service for Market Data

```csharp
// Services/MarketDataService.cs
using Microsoft.Extensions.Hosting;
using IBKRClient;
using System.Text.Json;

public class MarketDataService : BackgroundService
{
    private readonly IBKRApiClient _client;
    private readonly ILogger<MarketDataService> _logger;
    private readonly IConfiguration _configuration;
    
    public MarketDataService(IBKRApiClient client, ILogger<MarketDataService> logger, IConfiguration configuration)
    {
        _client = client;
        _logger = logger;
        _configuration = configuration;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var watchlistConids = _configuration.GetSection("Watchlist:Conids").Get<List<int>>() ?? new List<int>();
        
        if (!watchlistConids.Any())
        {
            _logger.LogWarning("No watchlist configured");
            return;
        }
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Get market data for watchlist
                var marketDataRequest = new MarketDataRequest(
                    Conids: watchlistConids,
                    Fields: new List<string> { "31", "84", "86" } // Last, Bid, Ask
                );
                
                var marketData = await _client.GetMarketDataSnapshotAsync(marketDataRequest);
                
                foreach (var item in marketData)
                {
                    _logger.LogInformation("Market data for {Conid}: {Data}", 
                        item.GetValueOrDefault("conid"), 
                        JsonSerializer.Serialize(item));
                }
                
                // Wait 30 seconds before next update
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting market data");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
```

## 🐍 Python Client Examples

### 1. Async Python Client

```python
# clients/python/async_client.py
import asyncio
import aiohttp
import json
from typing import List, Dict, Optional
from dataclasses import dataclass

@dataclass
class Position:
    conid: Optional[int] = None
    ticker: Optional[str] = None
    position: Optional[float] = None
    mkt_price: Optional[float] = None
    mkt_value: Optional[float] = None

class AsyncIBKRClient:
    def __init__(self, base_url: str = "http://localhost:8000", api_key: str = None):
        self.base_url = base_url.rstrip('/')
        self.api_key = api_key
        self.session = None
    
    async def __aenter__(self):
        self.session = aiohttp.ClientSession()
        return self
    
    async def __aexit__(self, exc_type, exc_val, exc_tb):
        if self.session:
            await self.session.close()
    
    def _get_headers(self) -> Dict[str, str]:
        headers = {"Content-Type": "application/json"}
        if self.api_key:
            headers["X-API-Key"] = self.api_key
        return headers
    
    async def get_health(self) -> Dict:
        async with self.session.get(
            f"{self.base_url}/health",
            headers=self._get_headers()
        ) as response:
            response.raise_for_status()
            return await response.json()
    
    async def get_accounts(self) -> List[Dict]:
        async with self.session.get(
            f"{self.base_url}/portfolio/accounts",
            headers=self._get_headers()
        ) as response:
            response.raise_for_status()
            return await response.json()
    
    async def get_positions(self, account_id: str) -> List[Position]:
        async with self.session.get(
            f"{self.base_url}/portfolio/{account_id}/positions",
            headers=self._get_headers()
        ) as response:
            response.raise_for_status()
            data = await response.json()
            return [Position(**pos) for pos in data]
    
    async def get_market_data(self, conids: List[int], fields: List[str] = None) -> List[Dict]:
        if fields is None:
            fields = ["31", "84", "86"]  # Last, Bid, Ask
        
        payload = {
            "conids": conids,
            "fields": fields
        }
        
        async with self.session.post(
            f"{self.base_url}/iserver/marketdata/snapshot",
            json=payload,
            headers=self._get_headers()
        ) as response:
            response.raise_for_status()
            return await response.json()

# Usage example
async def main():
    async with AsyncIBKRClient("http://localhost:8000", api_key="your-api-key") as client:
        # Check health
        health = await client.get_health()
        print(f"Service healthy: {health.get('service', {}).get('status') == 'healthy'}")
        
        # Get accounts
        accounts = await client.get_accounts()
        print(f"Found {len(accounts)} accounts")
        
        if accounts:
            account_id = accounts[0]["accountId"]
            
            # Get positions
            positions = await client.get_positions(account_id)
            print(f"Found {len(positions)} positions")
            
            for pos in positions[:5]:  # Show first 5
                print(f"  {pos.ticker}: {pos.position} @ ${pos.mkt_price}")

if __name__ == "__main__":
    asyncio.run(main())
```

### 2. Trading Bot Example

```python
# clients/python/trading_bot.py
import asyncio
import logging
from datetime import datetime, timedelta
from typing import Dict, List
from async_client import AsyncIBKRClient

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class TradingBot:
    def __init__(self, client: AsyncIBKRClient, account_id: str):
        self.client = client
        self.account_id = account_id
        self.watchlist = []  # List of conids to watch
        
    async def initialize(self):
        """Initialize bot with account validation"""
        try:
            health = await self.client.get_health()
            if not health.get('ibkr', {}).get('authenticated'):
                raise Exception("IBKR not authenticated")
            
            accounts = await self.client.get_accounts()
            account_ids = [acc['accountId'] for acc in accounts]
            
            if self.account_id not in account_ids:
                raise Exception(f"Account {self.account_id} not found")
            
            logger.info(f"Bot initialized for account {self.account_id}")
            
        except Exception as e:
            logger.error(f"Bot initialization failed: {e}")
            raise
    
    async def add_to_watchlist(self, symbol: str) -> bool:
        """Add symbol to watchlist"""
        try:
            # Search for contract
            search_payload = {
                "symbol": symbol,
                "secType": "STK"
            }
            
            contracts = await self.client.session.post(
                f"{self.client.base_url}/iserver/secdef/search",
                json=search_payload,
                headers=self.client._get_headers()
            )
            contracts.raise_for_status()
            contract_data = await contracts.json()
            
            if contract_data and len(contract_data) > 0:
                conid = contract_data[0].get('conid')
                if conid:
                    self.watchlist.append(conid)
                    logger.info(f"Added {symbol} (conid: {conid}) to watchlist")
                    return True
            
            logger.warning(f"Could not find contract for {symbol}")
            return False
            
        except Exception as e:
            logger.error(f"Error adding {symbol} to watchlist: {e}")
            return False
    
    async def get_market_data(self) -> Dict[int, Dict]:
        """Get current market data for watchlist"""
        if not self.watchlist:
            return {}
        
        try:
            market_data = await self.client.get_market_data(self.watchlist)
            
            # Convert to conid -> data mapping
            data_by_conid = {}
            for item in market_data:
                conid = item.get('conid')
                if conid:
                    data_by_conid[conid] = item.get('data', {})
            
            return data_by_conid
            
        except Exception as e:
            logger.error(f"Error getting market data: {e}")
            return {}
    
    async def analyze_signals(self, market_data: Dict[int, Dict]) -> List[Dict]:
        """Analyze market data for trading signals"""
        signals = []
        
        for conid, data in market_data.items():
            try:
                last_price = data.get('31')  # Last price field
                bid_price = data.get('84')   # Bid price field
                ask_price = data.get('86')   # Ask price field
                
                if not all([last_price, bid_price, ask_price]):
                    continue
                
                # Simple example: if spread is less than 1% of price, it's a good signal
                spread = ask_price - bid_price
                spread_pct = (spread / last_price) * 100 if last_price > 0 else 0
                
                if spread_pct < 1.0:  # Less than 1% spread
                    signals.append({
                        'conid': conid,
                        'action': 'BUY',
                        'price': last_price,
                        'spread_pct': spread_pct,
                        'timestamp': datetime.utcnow()
                    })
            
            except Exception as e:
                logger.error(f"Error analyzing signal for conid {conid}: {e}")
                continue
        
        return signals
    
    async def run(self, duration_minutes: int = 60):
        """Run trading bot for specified duration"""
        logger.info(f"Starting trading bot for {duration_minutes} minutes")
        
        start_time = datetime.utcnow()
        end_time = start_time + timedelta(minutes=duration_minutes)
        
        while datetime.utcnow() < end_time:
            try:
                # Get market data
                market_data = await self.get_market_data()
                logger.info(f"Retrieved market data for {len(market_data)} instruments")
                
                # Analyze for signals
                signals = await self.analyze_signals(market_data)
                
                if signals:
                    logger.info(f"Found {len(signals)} trading signals")
                    for signal in signals:
                        logger.info(f"Signal: {signal['action']} conid {signal['conid']} at ${signal['price']}")
                
                # Wait before next iteration
                await asyncio.sleep(30)  # 30 seconds
                
            except Exception as e:
                logger.error(f"Error in bot loop: {e}")
                await asyncio.sleep(60)  # Wait longer after error
        
        logger.info("Trading bot completed")

# Usage
async def main():
    async with AsyncIBKRClient("http://localhost:8000", api_key="your-api-key") as client:
        bot = TradingBot(client, "DU12345")  # Your account ID
        
        await bot.initialize()
        
        # Add symbols to watchlist
        await bot.add_to_watchlist("AAPL")
        await bot.add_to_watchlist("GOOGL")
        await bot.add_to_watchlist("MSFT")
        
        # Run bot for 30 minutes
        await bot.run(duration_minutes=30)

if __name__ == "__main__":
    asyncio.run(main())
```

## 📱 JavaScript/Node.js Integration

### 1. Node.js Client

```javascript
// clients/javascript/client.js
const axios = require('axios');

class IBKRApiClient {
    constructor(baseUrl = 'http://localhost:8000', apiKey = null) {
        this.baseUrl = baseUrl.replace(/\/$/, '');
        this.apiKey = apiKey;
        
        // Create axios instance with default config
        this.client = axios.create({
            baseURL: this.baseUrl,
            timeout: 30000,
            headers: {
                'Content-Type': 'application/json',
                ...(this.apiKey ? { 'X-API-Key': this.apiKey } : {})
            }
        });
        
        // Add response interceptor for error handling
        this.client.interceptors.response.use(
            response => response.data,
            error => {
                const message = error.response?.data?.detail || error.message;
                throw new Error(`IBKR API Error: ${message}`);
            }
        );
    }
    
    async getHealth() {
        return await this.client.get('/health');
    }
    
    async getAccounts() {
        return await this.client.get('/portfolio/accounts');
    }
    
    async getPositions(accountId, page = 0) {
        return await this.client.get(`/portfolio/${accountId}/positions/${page}`);
    }
    
    async getMarketDataSnapshot(conids, fields = ['31', '84', '86']) {
        return await this.client.post('/iserver/marketdata/snapshot', {
            conids: conids,
            fields: fields
        });
    }
    
    async searchContracts(symbol, searchType = 'symbol', secType = 'STK') {
        return await this.client.post('/iserver/secdef/search', {
            symbol: symbol,
            name: searchType === 'name',
            secType: secType
        });
    }
    
    async placeOrder(accountId, order) {
        return await this.client.post(`/iserver/account/${accountId}/orders`, order);
    }
    
    async getLiveOrders(filters = null, accountId = null) {
        const params = {};
        if (filters) params.filters = filters;
        if (accountId) params.account_id = accountId;
        
        return await this.client.get('/iserver/account/orders', { params });
    }
}

module.exports = IBKRApiClient;

// Example usage
async function example() {
    const client = new IBKRApiClient('http://localhost:8000', 'your-api-key');
    
    try {
        // Check health
        const health = await client.getHealth();
        console.log('Service status:', health.service.status);
        
        // Get accounts
        const accounts = await client.getAccounts();
        console.log(`Found ${accounts.length} accounts`);
        
        if (accounts.length > 0) {
            const accountId = accounts[0].accountId;
            
            // Get positions
            const positions = await client.getPositions(accountId);
            console.log(`Found ${positions.length} positions`);
            
            // Get market data for first few positions
            const conids = positions.slice(0, 3).map(p => p.conid).filter(Boolean);
            if (conids.length > 0) {
                const marketData = await client.getMarketDataSnapshot(conids);
                console.log('Market data:', marketData);
            }
        }
        
    } catch (error) {
        console.error('Error:', error.message);
    }
}

if (require.main === module) {
    example();
}
```

## 🔧 Troubleshooting Guide

### Common Issues and Solutions

#### 1. "IBKR client not initialized"
**Cause**: Service started but OAuth authentication failed
**Solution**:
- Check environment variables are set correctly
- Verify IBKR OAuth credentials are valid
- Check logs for specific authentication errors

#### 2. "Bad Request: no bridge" 
**Cause**: Brokerage session not established
**Solution**:
```bash
# Check session status
curl http://localhost:8000/admin/session-status

# Refresh session
curl -X POST http://localhost:8000/admin/refresh-session
```

#### 3. Rate Limiting Errors
**Cause**: Too many requests per minute
**Solution**:
- Implement exponential backoff in client
- Reduce request frequency
- Consider upgrading rate limits in production

#### 4. Market Data Issues
**Cause**: Market data subscriptions or permissions
**Solution**:
- Ensure `/iserver/accounts` is called before market data requests
- Check IBKR account has appropriate market data permissions
- Verify contract IDs are correct

### Performance Tuning

1. **Connection Pooling**: Use connection pools for high-frequency requests
2. **Caching**: Cache frequently requested static data (contract info, account details)
3. **Batch Requests**: Group multiple operations where possible
4. **Async Processing**: Use async/await patterns for I/O operations

### Monitoring Checklist

- [ ] Service health endpoint responding
- [ ] IBKR authentication status healthy
- [ ] Error rates within acceptable limits
- [ ] Response times under thresholds
- [ ] Memory and CPU usage normal
- [ ] Log files being written and rotated
- [ ] SSL certificates not expiring soon

## 📝 Final Notes

This comprehensive service provides:

1. **Two service variants**: Basic (`main.py`) and Enhanced (`enhanced_main.py`)
2. **Production-ready features**: Authentication, rate limiting, metrics, caching
3. **Multi-language clients**: C#, Python, JavaScript examples
4. **Deployment options**: Docker, traditional server, cloud platforms
5. **Monitoring & alerting**: Health checks, Prometheus metrics, Grafana dashboards
6. **Security best practices**: API keys, HTTPS, input validation
7. **Comprehensive documentation**: Setup, configuration, troubleshooting

The service acts as a bridge between your C# .NET applications and the IBKR API, providing a clean RESTful interface while handling all the complexity of OAuth authentication and session management internally.
