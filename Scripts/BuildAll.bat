@echo off
cd /d "%~dp0.."
echo ========================================
echo Casino Royale - Build Everything
echo ========================================
echo.

echo [1/3] Building main project...
dotnet build --configuration Release
if %errorlevel% neq 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo [2/3] Updating Instance1...
echo Killing any running Casino Royale processes...
taskkill /f /im CasinoRoyaleServer.exe 2>nul
timeout /t 1 /nobreak >nul

if exist "Build\Instance1" rmdir /s /q "Build\Instance1"
mkdir "Build\Instance1"

echo Copying all game files...
xcopy "Build\bin\Release\net8.0\*" "Build\Instance1\" /Y /E /I /Q
copy "app.properties" "Build\Instance1\app.properties" /Y > nul

echo [3/3] Updating Instance2...
if exist "Build\Instance2" rmdir /s /q "Build\Instance2"
mkdir "Build\Instance2"

echo Copying all game files...
xcopy "Build\bin\Release\net8.0\*" "Build\Instance2\" /Y /E /I /Q
copy "app.properties" "Build\Instance2\app.properties" /Y > nul

echo.
echo ========================================
echo Build completed successfully!
echo ========================================
echo.
echo All required files copied:
echo   - Game executables and DLLs
echo   - Content folder (game assets)
echo   - Runtimes folder (SDL2.dll and native libraries)
echo   - Configuration files
echo.
echo You can now run:
echo   - Instance1 (any role)
echo   - Instance2 (any role)
echo.
echo This terminal will close automatically in 5 seconds...
timeout /t 5 /nobreak >nul
exit
