@echo off
echo ========================================
echo   BACKEND CONNECTION TROUBLESHOOTING
echo ========================================
echo.

echo 1. Checking if backend is running on correct ports...
echo.
echo === Port 5000 (HTTP) ===
netstat -ano | findstr :5000
echo.
echo === Port 7000 (HTTPS) ===  
netstat -ano | findstr :7000
echo.

echo 2. Testing backend endpoints...
echo.
echo Testing HTTP health endpoint...
powershell -Command "try { $result = Invoke-RestMethod -Uri 'http://localhost:5000/api/health' -TimeoutSec 5; Write-Host 'SUCCESS: Backend is running!' -ForegroundColor Green; Write-Host ($result | ConvertTo-Json) -ForegroundColor Yellow } catch { Write-Host 'ERROR: Backend not responding on HTTP' -ForegroundColor Red; Write-Host $_.Exception.Message -ForegroundColor Red }"
echo.

echo Testing auth endpoint...
powershell -Command "try { $result = Invoke-RestMethod -Uri 'http://localhost:5000/api/auth/health' -TimeoutSec 5; Write-Host 'SUCCESS: Auth endpoint working!' -ForegroundColor Green; Write-Host ($result | ConvertTo-Json) -ForegroundColor Yellow } catch { Write-Host 'ERROR: Auth endpoint not working' -ForegroundColor Red; Write-Host $_.Exception.Message -ForegroundColor Red }"
echo.

echo 3. Testing CORS...
powershell -Command "try { $headers = @{'Origin' = 'http://localhost:3000'}; $result = Invoke-WebRequest -Uri 'http://localhost:5000/api/health' -Headers $headers -TimeoutSec 5; Write-Host 'SUCCESS: CORS headers present' -ForegroundColor Green; $result.Headers | ForEach-Object { if ($_.Key -like '*cors*' -or $_.Key -like '*access-control*') { Write-Host \"$($_.Key): $($_.Value)\" -ForegroundColor Yellow } } } catch { Write-Host 'WARNING: CORS test failed' -ForegroundColor Yellow; Write-Host $_.Exception.Message -ForegroundColor Red }"
echo.

echo ========================================
echo   FRONTEND CONFIGURATION NEEDED
echo ========================================
echo.
echo In your frontend project, create/update .env.local:
echo.
echo NEXT_PUBLIC_API_URL=http://localhost:5000/api
echo.
echo Then restart your frontend:
echo   npm run dev
echo.
echo ========================================
echo If backend is not running, start it with:
echo   dotnet run
echo Or press F5 in Visual Studio
echo ========================================
pause