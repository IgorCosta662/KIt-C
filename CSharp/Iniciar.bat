@echo off
setlocal EnableExtensions
title Kit Tecnico Pro — Inicializando...

set "PROJ=%~dp0KitTecnicoPro"
set "DEP=%~dp0..\Dependencias"

if exist "%DEP%\dotnet\dotnet.exe" (
    set "DOTNET_ROOT=%DEP%\dotnet"
    set "DOTNET_MULTILEVEL_LOOKUP=0"
    set "PATH=%DEP%\dotnet;%PATH%"
)
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_NOLOGO=1"
set "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"
if exist "%DEP%\nuget" set "NUGET_PACKAGES=%DEP%\nuget"

dotnet build "%PROJ%\KitTecnicoPro.csproj" -c Release --nologo -v q
if errorlevel 1 (
    echo.
    echo [ERRO] Falha no build. Pressione qualquer tecla para ver o log detalhado.
    dotnet build "%PROJ%\KitTecnicoPro.csproj" -c Release --nologo
    pause
    exit /b 1
)

start "" "%PROJ%\bin\Release\net8.0-windows\KitTecnicoPro.exe"
endlocal
exit /b 0
