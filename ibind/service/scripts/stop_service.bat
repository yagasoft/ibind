@echo off
REM stop_service.bat - Stop the service gracefully

setlocal enabledelayedexpansion

echo Stopping IBKR Web Service...

for /f "tokens=2 delims== " %%P in ('wmic process where "CommandLine like '%%main.py%%' or CommandLine like '%%gunicorn%%main:app%%'" get ProcessId /value ^| find "="') do (
    set "PIDS=!PIDS! %%P"
)

if not defined PIDS (
    echo No IBKR Web Service processes found
) else (
    echo Found processes: !PIDS!
    for %%P in (!PIDS!) do (
        echo Stopping process %%P...
        taskkill /PID %%P /T >nul 2>&1
        timeout /t 2 >nul
        tasklist /FI "PID eq %%P" | find "%%P" >nul
        if not errorlevel 1 (
            echo Force killing process %%P...
            taskkill /PID %%P /T /F >nul 2>&1
        )
    )
    echo IBKR Web Service stopped
)

endlocal
