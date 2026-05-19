@echo off
title Kit Tecnico Pro — Inicializando...

:: Garante que estamos na pasta do projeto independente de onde o bat foi chamado
set "PROJ=%~dp0KitTecnicoPro"

:: Build incremental (rapido se nada mudou)
dotnet build "%PROJ%\KitTecnicoPro.csproj" -c Release --nologo -v q 2>nul
if errorlevel 1 (
    echo.
    echo [ERRO] Falha no build. Pressione qualquer tecla para ver o log.
    dotnet build "%PROJ%\KitTecnicoPro.csproj" -c Release --nologo
    pause
    exit /b 1
)

:: Abre o executavel diretamente (sem dotnet run)
start "" "%PROJ%\bin\Release\net8.0-windows\KitTecnicoPro.exe"
exit
