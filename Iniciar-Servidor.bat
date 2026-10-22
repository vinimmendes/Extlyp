@echo off
setlocal
title Cyber Extraction - Controle do servidor
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Server-Console.ps1"
if errorlevel 1 (
    echo.
    echo O console encerrou com erro. Confira a mensagem acima.
    pause
)
