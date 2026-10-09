@echo off
title Cyber Extraction - Iniciar salas
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Start-Server.ps1"
if errorlevel 1 (
    echo.
    echo Nao foi possivel iniciar o servidor. Confira a mensagem acima.
) else (
    echo.
    echo Pode fechar esta janela. As salas continuam disponiveis.
    echo Para desligar, abra Parar-Servidor.bat.
)
echo.
pause
