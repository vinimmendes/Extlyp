@echo off
setlocal
title Cyber Extraction - Servidor de desenvolvimento
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Server-Console.ps1" -Channel Development
if errorlevel 1 pause
