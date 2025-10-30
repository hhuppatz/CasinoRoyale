@echo off
cd /d "%~dp0.."
echo ========================================
echo Casino Royale - Starting Instance 2
echo ========================================
echo.

echo Checking if build is needed...
if not exist "Build\Instance2\CasinoRoyaleServer.exe" (
    echo Instance2 not found! Running BuildAll.bat...
    call Scripts\BuildAll.bat
)

echo Starting Instance 2...
cd Build\Instance2
start "Casino Royale Instance 2" CasinoRoyaleServer.exe
cd ..\..

echo.
echo ========================================
echo Instance 2 started!
echo ========================================
echo.
echo You can choose to Host or Join a game.
