# IBKR RESTful Web Service

A FastAPI-based RESTful web service that wraps the IBind library to provide HTTP access to Interactive Brokers (IBKR) API functionality using OAuth 1.0a authentication.

## Features

- **OAuth 1.0a Authentication**: Fully headless authentication with IBKR
- **RESTful API**: Clean HTTP endpoints following REST principles
- **Automatic Session Management**: Handles IBKR session lifecycle automatically
- **Comprehensive Coverage**: Supports accounts, portfolio, market data, orders, and more
- **Error Handling**: Proper HTTP error responses and logging
- **Health Checks**: Monitor service and IBKR connection health
- **Interactive Documentation**: Auto-generated OpenAPI/Swagger documentation

## Quick Start

### 1. Installation

```bash
# Install additional dependencies for the web service
pip install fastapi uvicorn[standard] pydantic

# Or install from requirements file
pip install -r requirements-service.txt
```

### 2. Configuration

Copy the example environment file and configure your IBKR OAuth credentials:

```bash
cp .env.example .env
# Edit .env with your IBKR OAuth credentials
```

Required environment variables:
- `IBIND_ACCOUNT_ID`: Your IBKR account ID
- `IBIND_CONSUMER_KEY`: OAuth consumer key from IBKR
- `IBIND_CONSUMER_SECRET`: OAuth consumer secret from IBKR  
- `IBIND_ACCESS_TOKEN`: OAuth access token from IBKR
- `IBIND_ACCESS_TOKEN_SECRET`: OAuth access token secret from IBKR

### 3. Run the Service

```bash
# Run directly
python main.py

# Or with custom options
python main.py --host 0.0.0.0 --port 8000 --log-level debug

# Or using uvicorn directly
uvicorn main:app --host 127.0.0.1 --port 8000 --reload
```

### 4. Access the API

- **API Base URL**: `http://127.0.0.1:8000`
- **Interactive Documentation**: `http://127.0.0.1:8000/docs`
- **OpenAPI Schema**: `http://127.0.0.1:8000/openapi.json`
- **Health Check**: `http://127.0.0.1:8000/health`

## API Endpoints

### Health & Status
- `GET /health` - Check service and IBKR connection health
- `GET /tickle` - Ping IBKR to maintain session
- `POST /iserver/auth/status` - Get authentication status
- `POST /logout` - Logout from IBKR session

### Accounts
- `GET /portfolio/accounts` - List portfolio accounts
- `GET /iserver/accounts` - Get brokerage accounts  
- `GET /portfolio/{account_id}/summary` - Get account summary
- `GET /portfolio/{account_id}/ledger` - Get account ledger

### Portfolio
- `GET /portfolio/{account_id}/positions` - Get all positions
- `GET /portfolio/{account_id}/positions/{page}` - Get positions with pagination

### Market Data
- `POST /iserver/marketdata/snapshot` - Get real-time market data snapshot
- `GET /iserver/marketdata/history` - Get historical market data

### Contracts
- `POST /iserver/secdef/search` - Search contracts by symbol
- `GET /trsrv/secdef` - Get security definitions by contract IDs

### Orders
- `GET /iserver/account/orders` - Get live orders
- `POST /iserver/account/{account_id}/orders` - Place new order
- `GET /iserver/account/trades` - Get trade executions

## Usage Examples

### Get Account Information
```bash
# Get health status
curl http://127.0.0.1:8000/health

# Get accounts
curl http://127.0.0.1:8000/portfolio/accounts

# Get positions for specific account  
curl http://127.0.0.1:8000/portfolio/DU12345/positions
```

### Market Data
```bash
# Get market data snapshot
curl -X POST http://127.0.0.1:8000/iserver/marketdata/snapshot \
  -H "Content-Type: application/json" \
  -d '{"conids": [265598], "fields": ["31", "84", "86"]}'

# Get historical data
curl "http://127.0.0.1:8000/iserver/marketdata/history?conid=265598&bar=1d&period=1w"
```

### Search Contracts
```bash
# Search for Apple stock
curl -X POST http://127.0.0.1:8000/iserver/secdef/search \
  -H "Content-Type: application/json" \
  -d '{"symbol": "AAPL", "secType": "STK"}'
```

### Place Orders
```bash
# Place a market buy order
curl -X POST http://127.0.0.1:8000/iserver/account/DU12345/orders \
  -H "Content-Type: application/json" \
  -d '{
    "conid": 265598,
    "orderType": "MKT", 
    "side": "BUY",
    "quantity": 10,
    "tif": "DAY"
  }'
```

## Integration with C# .NET

You can easily consume this service from your C# .NET 9 application:

```csharp
using System.Text.Json;
using System.Text;

public class IBKRApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public IBKRApiClient(string baseUrl = "http://127.0.0.1:8000")
    {
        _httpClient = new HttpClient();
        _baseUrl = baseUrl;
    }

    public async Task<List<Account>> GetAccountsAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/portfolio/accounts");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Account>>(json);
    }

    public async Task<List<Position>> GetPositionsAsync(string accountId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/portfolio/{accountId}/positions");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Position>>(json);
    }

    // Add more methods as needed
}
```

## Docker Deployment

Build and run using Docker:

```bash
# Build image
docker build -t ibkr-service .

# Run container
docker run -p 8000:8000 --env-file .env ibkr-service

# Or use docker-compose
docker-compose up -d
```

## Development

### Adding New Endpoints

1. Add the endpoint function to `main.py`
2. Use the appropriate IBind client method
3. Add request/response models using Pydantic
4. Include proper error handling
5. Update the documentation

Example:
```python
@app.get("/new-endpoint/{param}", tags=["Category"])
async def new_endpoint(param: str = Path(..., description="Parameter description")):
    """Endpoint description"""
    try:
        if not ibkr_client:
            raise HTTPException(status_code=503, detail="IBKR client not initialized")
        
        result = ibkr_client.some_method(param)
        return result.data
    except Exception as e:
        raise handle_ibkr_error(e)
```

### Testing

```bash
# Run the service in development mode
python main.py --reload --log-level debug

# Test endpoints
curl http://127.0.0.1:8000/health
```

### Logging

The service uses the IBind logging system. Logs are written to:
- Console output
- Log files in the `logs/` directory (if configured)

## Error Handling

The service provides comprehensive error handling:

- **HTTP 503**: Service unavailable (IBKR client not initialized)
- **HTTP 400**: Bad request (invalid parameters)  
- **HTTP 401**: Unauthorized (authentication failed)
- **HTTP 404**: Not found (endpoint or resource not found)
- **HTTP 500**: Internal server error

All errors return JSON responses with error details:
```json
{
  "error": "Error description",
  "detail": "Detailed error message"
}
```

## Security Considerations

1. **Environment Variables**: Store sensitive OAuth credentials in environment variables or secure vaults
2. **HTTPS**: Use HTTPS in production deployments
3. **Authentication**: Consider adding API authentication for production use
4. **CORS**: Configure CORS appropriately for your use case
5. **Rate Limiting**: Consider adding rate limiting for production deployments

## Production Deployment

### Using Gunicorn (Recommended)

```bash
# Install gunicorn
pip install gunicorn

# Run with gunicorn
gunicorn main:app -w 4 -k uvicorn.workers.UvicornWorker --bind 0.0.0.0:8000
```

### Environment Variables for Production

```bash
export IBIND_ACCOUNT_ID=your_account
export IBIND_CONSUMER_KEY=your_key
export IBIND_CONSUMER_SECRET=your_secret
export IBIND_ACCESS_TOKEN=your_token  
export IBIND_ACCESS_TOKEN_SECRET=your_token_secret
export IBKR_SERVICE_HOST=0.0.0.0
export IBKR_SERVICE_PORT=8000
```

### Nginx Reverse Proxy

```nginx
server {
    listen 80;
    server_name your-domain.com;

    location / {
        proxy_pass http://127.0.0.1:8000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }
}
```

## Troubleshooting

### Common Issues

1. **"IBKR client not initialized"**
   - Check your OAuth credentials in environment variables
   - Ensure IBKR OAuth application is properly configured

2. **"Bad Request: no bridge"** 
   - Try calling the health endpoint first
   - Check if brokerage session initialization is enabled

3. **Authentication failures**
   - Verify OAuth credentials are correct
   - Check if tokens have expired
   - Ensure account permissions are properly configured

4. **Connection timeouts**
   - Check network connectivity to IBKR servers
   - Verify firewall settings

### Debugging

Enable debug logging:
```bash
python main.py --log-level debug
```

Check service health:
```bash
curl http://127.0.0.1:8000/health
```

## API Reference

For complete API documentation, visit the interactive docs at `/docs` when the service is running, or refer to the OpenAPI schema at `/openapi.json`.

## Contributing

1. Follow the existing code style
2. Add proper error handling and logging
3. Include request/response models for new endpoints
4. Update documentation
5. Test thoroughly with various scenarios

## License

This service wrapper follows the same license as the IBind library.
