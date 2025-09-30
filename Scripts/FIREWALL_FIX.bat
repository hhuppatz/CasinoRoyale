@echo off
echo ========================================
echo Casino Royale - Windows Firewall Fix
echo ========================================
echo.
echo This will allow CasinoRoyaleServer.exe through Windows Firewall
echo Run this script AS ADMINISTRATOR
echo.
pause

echo Adding firewall rules...

REM Allow outbound UDP (connect to relay server)
netsh advfirewall firewall add rule name="Casino Royale - UDP Out" dir=out action=allow protocol=UDP program="%~dp0..\Build\bin\Release\net8.0\CasinoRoyaleServer.exe"

REM Allow inbound UDP (receive packets from relay)
netsh advfirewall firewall add rule name="Casino Royale - UDP In" dir=in action=allow protocol=UDP program="%~dp0..\Build\bin\Release\net8.0\CasinoRoyaleServer.exe"

echo.
echo ========================================
echo Firewall rules added!
echo ========================================
echo.
echo The Windows popup should no longer appear.
echo You can now run the game normally.
echo.
pause
