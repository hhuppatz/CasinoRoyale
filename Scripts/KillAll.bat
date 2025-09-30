@echo off
echo ========================================
echo Casino Royale - Kill All Processes
echo ========================================
echo.

echo Killing all Casino Royale processes...
taskkill /f /im CasinoRoyaleServer.exe 2>nul

if %errorlevel% equ 0 (
    echo All Casino Royale processes terminated.
) else (
    echo No Casino Royale processes were running.
)

echo.
pause
