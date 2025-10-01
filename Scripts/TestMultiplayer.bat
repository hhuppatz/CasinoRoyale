@echo off
cd /d "%~dp0.."
echo ========================================
echo Casino Royale - Multiplayer Test
echo ========================================
echo.
echo This will start two local game instances.
echo They will connect to the remote relay server at:
echo   3.107.27.32
echo.
echo Press any key to continue...
pause > nul

echo.
echo Checking if build is needed...
if not exist "Build\Instance1\CasinoRoyaleServer.exe" (
    echo Instances not found! Running BuildAll.bat...
    call Scripts\BuildAll.bat
)

echo.
echo [1/2] Starting Instance 1 (Host)...
start "Casino Royale Instance 1" cmd /k "cd /d %~dp0..\Build\Instance1 && CasinoRoyaleServer.exe"

echo Waiting for instance 1 to initialize...
timeout /t 3 > nul

echo.
echo [2/2] Starting Instance 2 (Client)...
start "Casino Royale Instance 2" cmd /k "cd /d %~dp0..\Build\Instance2 && CasinoRoyaleServer.exe"

echo.
echo ========================================
echo All instances started!
echo ========================================
echo.
echo Instructions:
echo 1. In Instance 1 window, click "HOST GAME"
echo 2. Wait for the lobby code to appear in top-left corner
echo 3. In Instance 2 window, type the EXACT lobby code
echo 4. Press Enter to join
echo.
echo Note: Both instances connect to remote relay server
echo       at 54.252.174.8:9051
echo.
echo Happy gaming!
echo ========================================
