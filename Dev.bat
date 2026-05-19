@echo off
title Kit Tecnico Pro — Dev

set "PROJ=%~dp0KitTecnicoPro"

dotnet build "%PROJ%\KitTecnicoPro.csproj" -c Debug --nologo -v q 2>nul
if errorlevel 1 (
    echo [ERRO] Falha no build.
    dotnet build "%PROJ%\KitTecnicoPro.csproj" -c Debug --nologo
    pause
    exit /b 1
)

start "" "%PROJ%\bin\Debug\net8.0-windows\KitTecnicoPro.exe"
exit
