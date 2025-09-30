@echo off
cd /d "%~dp0.."
echo ========================================
echo Casino Royale - Starting Instance 1
echo ========================================
echo.

echo Checking if build is needed...
if not exist "Build\Instance1\CasinoRoyaleServer.exe" (
    echo Instance1 not found! Running BuildAll.bat...
    call Scripts\BuildAll.bat
)

echo Starting Instance 1...
cd Build\Instance1
start "Casino Royale Instance 1" CasinoRoyaleServer.exe
cd ..\..

echo.
echo ========================================
echo Instance 1 started!
echo ========================================
echo.
echo You can choose to Host or Join a game.
echo This terminal will close automatically in 3 seconds...
timeout /t 3 /nobreak >nul
