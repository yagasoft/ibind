@echo off
REM start_service.bat - Production startup script

setlocal enabledelayedexpansion

echo Starting IBKR RESTful Web Service...

REM Check if required environment variables are set
set "missing=0"
for %%V in (IBIND_ACCOUNT_ID IBIND_CONSUMER_KEY IBIND_CONSUMER_SECRET IBIND_ACCESS_TOKEN IBIND_ACCESS_TOKEN_SECRET) do (
    if not defined %%V (
        echo Error: Required environment variable %%V is not set
        set "missing=1"
    )
)

if not "!missing!"=="0" (
    echo Please check your environment configuration
    exit /b 1
)

REM Set default values
if not defined IBKR_SERVICE_HOST set "IBKR_SERVICE_HOST=127.0.0.1"
if not defined IBKR_SERVICE_PORT set "IBKR_SERVICE_PORT=8000"
if not defined IBKR_SERVICE_LOG_LEVEL set "IBKR_SERVICE_LOG_LEVEL=info"

REM Create logs directory if it doesn't exist
if not exist logs mkdir logs

REM Check if we're in development or production
if /I "%1"=="dev" (
    echo Starting in development mode with auto-reload...
    python main.py --host %IBKR_SERVICE_HOST% --port %IBKR_SERVICE_PORT% --reload --log-level debug
) else if /I "%1"=="prod" (
    echo Starting in production mode with gunicorn...
    where gunicorn >nul 2>&1
    if errorlevel 1 (
        echo Gunicorn not found. Installing...
        pip install gunicorn
    )
    gunicorn main:app -w 4 -k uvicorn.workers.UvicornWorker --bind %IBKR_SERVICE_HOST%:%IBKR_SERVICE_PORT% --log-level %IBKR_SERVICE_LOG_LEVEL%
) else (
    echo Starting in standard mode...
    python main.py --host %IBKR_SERVICE_HOST% --port %IBKR_SERVICE_PORT% --log-level %IBKR_SERVICE_LOG_LEVEL%
)

endlocal
