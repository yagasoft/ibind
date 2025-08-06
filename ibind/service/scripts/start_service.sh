#!/bin/bash
# start_service.sh - Production startup script

set -e

echo "Starting IBKR RESTful Web Service..."

# Check if required environment variables are set
required_vars=("IBIND_ACCOUNT_ID" "IBIND_CONSUMER_KEY" "IBIND_CONSUMER_SECRET" "IBIND_ACCESS_TOKEN" "IBIND_ACCESS_TOKEN_SECRET")

for var in "${required_vars[@]}"; do
    if [ -z "${!var}" ]; then
        echo "Error: Required environment variable $var is not set"
        echo "Please check your .env file or environment configuration"
        exit 1
    fi
done

# Set default values
export IBKR_SERVICE_HOST=${IBKR_SERVICE_HOST:-"127.0.0.1"}
export IBKR_SERVICE_PORT=${IBKR_SERVICE_PORT:-"8000"}
export IBKR_SERVICE_LOG_LEVEL=${IBKR_SERVICE_LOG_LEVEL:-"info"}

# Create logs directory if it doesn't exist
mkdir -p logs

# Check if we're in development or production
if [ "$1" = "dev" ]; then
    echo "Starting in development mode with auto-reload..."
    python main.py --host $IBKR_SERVICE_HOST --port $IBKR_SERVICE_PORT --reload --log-level debug
elif [ "$1" = "prod" ]; then
    echo "Starting in production mode with gunicorn..."
    if ! command -v gunicorn &> /dev/null; then
        echo "Gunicorn not found. Installing..."
        pip install gunicorn
    fi
    gunicorn main:app -w 4 -k uvicorn.workers.UvicornWorker --bind $IBKR_SERVICE_HOST:$IBKR_SERVICE_PORT --log-level $IBKR_SERVICE_LOG_LEVEL
else
    echo "Starting in standard mode..."
    python main.py --host $IBKR_SERVICE_HOST --port $IBKR_SERVICE_PORT --log-level $IBKR_SERVICE_LOG_LEVEL
fi

---
# stop_service.sh - Stop the service gracefully

#!/bin/bash

echo "Stopping IBKR Web Service..."

# Find and kill the service processes
PIDS=$(ps aux | grep -E "(main.py|gunicorn.*main:app)" | grep -v grep | awk '{print $2}')

if [ -z "$PIDS" ]; then
    echo "No IBKR Web Service processes found"
else
    echo "Found processes: $PIDS"
    for PID in $PIDS; do
        echo "Stopping process $PID..."
        kill -TERM $PID
        sleep 2
        
        # Force kill if still running
        if kill -0 $PID 2>/dev/null; then
            echo "Force killing process $PID..."
            kill -KILL $PID
        fi
    done
    echo "IBKR Web Service stopped"
fi

---
# health_check.sh - Health check script for monitoring

#!/bin/bash

SERVICE_URL=${1:-"http://127.0.0.1:8000"}
HEALTH_ENDPOINT="$SERVICE_URL/health"

echo "Checking health of IBKR Web Service at $HEALTH_ENDPOINT"

# Perform health check
RESPONSE=$(curl -s -w "%{http_code}" -o /tmp/health_response.json "$HEALTH_ENDPOINT" 2>/dev/null)
HTTP_CODE="${RESPONSE: -3}"

if [ "$HTTP_CODE" = "200" ]; then
    STATUS=$(cat /tmp/health_response.json | python -c "import sys, json; data=json.load(sys.stdin); print(data.get('status', 'unknown'))")
    AUTHENTICATED=$(cat /tmp/health_response.json | python -c "import sys, json; data=json.load(sys.stdin); print(data.get('authenticated', False))")
    
    echo "Service Status: $STATUS"
    echo "Authenticated: $AUTHENTICATED"
    
    if [ "$STATUS" = "healthy" ] && [ "$AUTHENTICATED" = "True" ]; then
        echo "✅ Service is healthy and authenticated"
        exit 0
    else
        echo "⚠️  Service is running but not healthy or not authenticated"
        exit 1
    fi
else
    echo "❌ Service health check failed (HTTP $HTTP_CODE)"
    exit 2
fi

rm -f /tmp/health_response.json

---
# install_dependencies.sh - Install all required dependencies

#!/bin/bash

echo "Installing IBKR Web Service dependencies..."

# Check Python version
PYTHON_VERSION=$(python --version 2>&1 | cut -d' ' -f2 | cut -d'.' -f1-2)
REQUIRED_VERSION="3.8"

if [ "$(printf '%s\n' "$REQUIRED_VERSION" "$PYTHON_VERSION" | sort -V | head -n1)" != "$REQUIRED_VERSION" ]; then 
    echo "Error: Python $REQUIRED_VERSION or higher is required (found $PYTHON_VERSION)"
    exit 1
fi

echo "Python version check passed ($PYTHON_VERSION)"

# Install base IBind library with OAuth support
echo "Installing IBind with OAuth support..."
pip install "ibind[oauth]"

# Install web service dependencies
echo "Installing web service dependencies..."
pip install fastapi uvicorn[standard] pydantic python-multipart

# Optional: Install production dependencies
if [ "$1" = "prod" ]; then
    echo "Installing production dependencies..."
    pip install gunicorn structlog
fi

echo "✅ All dependencies installed successfully"

# Check if OAuth dependencies are available
echo "Checking OAuth dependencies..."
python -c "
try:
    from Crypto.Signature import pkcs1_15
    print('✅ OAuth cryptographic dependencies available')
except ImportError:
    print('❌ OAuth dependencies missing. Install with: pip install ibind[oauth]')
    exit(1)
"

echo "Installation complete!"
echo ""
echo "Next steps:"
echo "1. Copy .env.example to .env and configure your IBKR OAuth credentials"
echo "2. Run: ./start_service.sh"
echo "3. Visit http://127.0.0.1:8000/docs for API documentation"

---
# test_service.sh - Basic service testing script

#!/bin/bash

SERVICE_URL=${1:-"http://127.0.0.1:8000"}

echo "Testing IBKR Web Service at $SERVICE_URL"
echo "========================================="

# Test health endpoint
echo "1. Testing health endpoint..."
curl -s "$SERVICE_URL/health" | python -m json.tool || echo "❌ Health check failed"
echo ""

# Test tickle endpoint  
echo "2. Testing tickle endpoint..."
curl -s "$SERVICE_URL/tickle" | python -m json.tool || echo "❌ Tickle failed"
echo ""

# Test accounts endpoint
echo "3. Testing accounts endpoint..."
curl -s "$SERVICE_URL/portfolio/accounts" | python -m json.tool || echo "❌ Accounts endpoint failed"
echo ""

# Test OpenAPI docs
echo "4. Testing OpenAPI documentation..."
curl -s -I "$SERVICE_URL/docs" | head -1 || echo "❌ Docs endpoint failed"
echo ""

echo "Basic tests completed. Check the responses above for any errors."
echo "For interactive testing, visit: $SERVICE_URL/docs"
