@echo off
setlocal EnableExtensions
chcp 65001 >nul
title Kit Tecnico Pro - Java Build

cd /d "%~dp0"
set "DEP=%~dp0..\Dependencias"

if exist "%DEP%\jdk\bin\java.exe" (
    set "JAVA_HOME=%DEP%\jdk"
    set "PATH=%DEP%\jdk\bin;%PATH%"
)
if exist "%DEP%\maven\bin\mvn.cmd" (
    set "MAVEN_HOME=%DEP%\maven"
    set "PATH=%DEP%\maven\bin;%PATH%"
)
set MVN_REPO=
if exist "%DEP%\m2" set MVN_REPO="-Dmaven.repo.local=%DEP%\m2"

echo ==========================================
echo   Kit Tecnico Pro - Java Build
echo ==========================================
echo.

set "DO_CLEAN=0"
set "SKIP_COMPILE=0"
for %%A in (%*) do (
    if /i "%%~A"=="clean"          set "DO_CLEAN=1"
    if /i "%%~A"=="--skip-compile" set "SKIP_COMPILE=1"
)

if not exist "pom.xml" (
    echo [ERRO] pom.xml nao encontrado em: %CD%
    goto :fail
)

where java >nul 2>&1 || (
    echo [ERRO] Java nao encontrado no PATH nem em Dependencias\jdk.
    goto :fail
)

where mvn >nul 2>&1 || (
    echo [ERRO] Maven nao encontrado no PATH nem em Dependencias\maven.
    goto :fail
)

if "%DO_CLEAN%"=="1" (
    echo [*] Limpando build anterior...
    call mvn clean -q %MVN_REPO%
    if errorlevel 1 goto :build_error
)

if "%SKIP_COMPILE%"=="0" (
    echo [1/2] Compilando...
    call mvn -o compile -q %MVN_REPO% || call mvn compile -q %MVN_REPO%
    if errorlevel 1 goto :build_error
) else (
    echo [1/2] Compilacao ignorada.
)

echo [2/2] Executando...
echo.
call mvn -o javafx:run %MVN_REPO% || call mvn javafx:run %MVN_REPO%
if errorlevel 1 (
    echo.
    echo [ERRO] A aplicacao terminou com erro (codigo %errorlevel%).
    goto :fail
)

endlocal
exit /b 0

:build_error
echo.
echo [ERRO] Falha no build (codigo %errorlevel%). Veja as mensagens acima.

:fail
echo.
pause
endlocal
exit /b 1
