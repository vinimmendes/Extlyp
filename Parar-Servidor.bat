@echo off
title Cyber Extraction - Parar servidor
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Stop-Server.ps1"
if errorlevel 1 (
    echo.
    echo Nao foi possivel desligar o servidor. Confira a mensagem acima.
)
echo.
pause
