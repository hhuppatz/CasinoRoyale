@echo off
cd /d "%~dp0.."
echo ========================================
echo Casino Royale - Build for Distribution
echo ========================================
echo.

REM Prompt for server IP
set /p SERVER_IP="Enter relay server IP (or press Enter for localhost 127.0.0.1): "

REM Default to localhost if empty
if "%SERVER_IP%"=="" set SERVER_IP=127.0.0.1

echo.
echo Updating app.properties with relay server: %SERVER_IP%
powershell -Command "(Get-Content app.properties) -replace 'relay.server.address=.*', 'relay.server.address=%SERVER_IP%' | Set-Content app.properties"

echo.
echo Building self-contained Windows executable...
echo This may take a few minutes...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./Distribution

if %errorlevel% neq 0 (
    echo.
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo.
echo Copying configuration files...
copy app.properties Distribution\app.properties /Y > nul

echo.
echo Creating player instructions...
(
echo 🎰 Casino Royale - How to Play
echo ================================
echo.
echo INSTALLATION:
echo   1. Extract all files to a folder
echo   2. Run CasinoRoyaleServer.exe
echo.
echo HOSTING A GAME:
echo   1. Click "HOST GAME" button
echo   2. A 6-character lobby code will appear in the top-left corner
echo   3. Share this code with your friends
echo.
echo JOINING A GAME:
echo   1. Get the lobby code from the host
echo   2. Type the code carefully in the input box
echo   3. Press Enter to join
echo.
echo CONTROLS:
echo   - WASD / Arrow Keys: Move your character
echo   - Space: Jump
echo   - ESC: Exit game
echo.
echo TROUBLESHOOTING:
echo   - Can't connect? Check your internet connection
echo   - Lobby code doesn't work? Double-check for typos
echo   - Game crashes? Check Logs\debug.log for errors
echo.
echo SYSTEM REQUIREMENTS:
echo   - Windows 10 or later
echo   - .NET Runtime 8.0 ^(included in this package^)
echo   - Internet connection for multiplayer
echo.
echo Relay Server: %SERVER_IP%:9051
echo.
echo Have fun! 🎮
) > Distribution\HOW_TO_PLAY.txt

echo.
echo ========================================
echo Build Complete!
echo ========================================
echo.
echo Distribution files are in: Distribution\
echo.
echo Files included:
echo   - CasinoRoyaleServer.exe (main game)
echo   - Content\ (game assets)
echo   - runtimes\ (native libraries)
echo   - app.properties (configuration)
echo   - HOW_TO_PLAY.txt (player instructions)
echo.
echo Next steps:
echo   1. Test: Distribution\CasinoRoyaleServer.exe
echo   2. Create ZIP: Right-click Distribution folder ^> Send to ^> Compressed folder
echo   3. Upload to itch.io / your website / GitHub Releases
echo.
echo To create a ZIP from PowerShell:
echo   Compress-Archive -Path Distribution\* -DestinationPath CasinoRoyale_v1.0.zip
echo.
pause
