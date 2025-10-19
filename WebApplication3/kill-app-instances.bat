@echo off
echo Checking for running WebApplication3 instances...
echo.

:: Find WebApplication3 processes
for /f "tokens=2" %%i in ('tasklist /FI "IMAGENAME eq WebApplication3.exe" /FO CSV ^| findstr /V "PID"') do (
    if not "%%i"=="" (
        echo Found WebApplication3.exe with PID: %%i
        echo Terminating process...
        taskkill /PID %%i /F
        if %ERRORLEVEL%==0 (
            echo ? Process terminated successfully
        ) else (
            echo ? Failed to terminate process
        )
    )
)

:: Also check for dotnet processes running WebApplication3.dll
echo.
echo Checking for dotnet processes running WebApplication3.dll...
for /f "tokens=2,9" %%i in ('wmic process where "name='dotnet.exe'" get ProcessId^,CommandLine /format:csv') do (
    echo %%j | findstr /i "WebApplication3" >nul
    if %ERRORLEVEL%==0 (
        echo Found dotnet.exe running WebApplication3 with PID: %%i
        echo Terminating process...
        taskkill /PID %%i /F
        if %ERRORLEVEL%==0 (
            echo ? Process terminated successfully
        ) else (
            echo ? Failed to terminate process
        )
    )
)

echo.
echo Checking ports status...
echo === Port 5000 (HTTP) ===
netstat -ano | findstr :5000
echo === Port 7000 (HTTPS) ===
netstat -ano | findstr :7000

echo.
echo Done! You can now run your application.
pause