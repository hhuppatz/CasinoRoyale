@echo off
cd /d "%~dp0.."
echo ========================================
echo Casino Royale - Multiplayer Test
echo ========================================
echo.
echo This will start two or three local game instances.
echo They will connect to the remote relay server at:
echo   3.107.27.32
echo.
echo Press any key to continue...
pause > nul

echo Checking if build is needed...
if not exist "Build\Instance1\CasinoRoyaleServer.exe" (
    echo Instances not found! Running BuildAll.bat...
    call Scripts\BuildAll.bat
)

echo.
echo Choose number of instances to start:
echo 1. Two instances (Instance1 + Instance2)
echo 2. Three instances (Instance1 + Instance2 + Instance3)
echo.
set /p choice="Enter your choice (1 or 2): "

if "%choice%"=="1" goto two_instances
if "%choice%"=="2" goto three_instances
echo Invalid choice. Defaulting to two instances.
goto two_instances

:two_instances
echo.
echo [1/2] Starting Instance 1 (Host)...
start "Casino Royale Instance 1" cmd /k "cd /d %~dp0..\Build\Instance1 && CasinoRoyaleServer.exe"

timeout /t 3 > nul

echo.
echo [2/2] Starting Instance 2 (Client)...
start "Casino Royale Instance 2" cmd /k "cd /d %~dp0..\Build\Instance2 && CasinoRoyaleServer.exe"

echo.
echo ========================================
echo Two instances started!
echo ========================================
echo.
echo Instructions:
echo 1. In Instance 1 window, click "HOST GAME"
echo 2. Wait for the lobby code to appear in top-left corner
echo 3. In Instance 2 window, type the EXACT lobby code
echo 4. Press Enter to join
echo.
echo Note: Both instances connect to remote relay server
echo.
goto end

:three_instances
echo.
echo [1/3] Starting Instance 1 (Host)...
start "Casino Royale Instance 1" cmd /k "cd /d %~dp0..\Build\Instance1 && CasinoRoyaleServer.exe"

timeout /t 3 > nul

echo.
echo [2/3] Starting Instance 2 (Client)...
start "Casino Royale Instance 2" cmd /k "cd /d %~dp0..\Build\Instance2 && CasinoRoyaleServer.exe"

timeout /t 3 > nul

echo.
echo [3/3] Starting Instance 3 (Client)...
start "Casino Royale Instance 3" cmd /k "cd /d %~dp0..\Build\Instance3 && CasinoRoyaleServer.exe"

echo.
echo ========================================
echo Three instances started!
echo ========================================
echo.
echo Instructions:
echo 1. In Instance 1 window, click "HOST GAME"
echo 2. Wait for the lobby code to appear in top-left corner
echo 3. In Instance 2 window, type the EXACT lobby code
echo 4. Press Enter to join
echo 5. In Instance 3 window, type the SAME lobby code
echo 6. Press Enter to join
echo.
echo Note: All instances connect to remote relay server
echo.

:end
echo ========================================
