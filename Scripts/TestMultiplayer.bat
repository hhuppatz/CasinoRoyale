@echo off
cd /d "%~dp0.."
echo ========================================
echo Casino Royale - Multiplayer Test
echo ========================================
echo.
echo This will start:
echo 1. LiteNetLib Relay Server
echo 2. Host Instance
echo 3. Client Instance
echo.
echo Press any key to continue...
pause > nul

echo.
echo [1/3] Starting Relay Server...
start "Casino Royale Relay Server" cmd /k "cd /d C:\Users\harry\Personal Projects\CasinoRoyaleLiteNetRelay\CasinoRoyaleRelayServer && dotnet run"

echo Waiting for relay server to start...
timeout /t 3 > nul

echo.
echo [2/3] Starting Instance 1...
start "Casino Royale Instance 1" cmd /k "cd /d %~dp0..\Build\Instance1 && CasinoRoyaleServer.exe"

echo Waiting for instance 1 to initialize...
timeout /t 3 > nul

echo.
echo [3/3] Starting Instance 2...
start "Casino Royale Instance 2" cmd /k "cd /d %~dp0..\Build\Instance2 && CasinoRoyaleServer.exe"

echo.
echo ========================================
echo All instances started!
echo ========================================
echo.
echo Instructions:
echo 1. In Instance 1 window, click "HOST GAME"
echo 2. Note the lobby code displayed in top-left corner
echo 3. In Instance 2 window, type the EXACT lobby code
echo 4. Press Enter to join
echo.
echo Happy gaming!
echo ========================================
