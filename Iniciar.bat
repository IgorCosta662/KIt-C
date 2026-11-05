@echo off
setlocal EnableExtensions
title Kit Tecnico Pro - Inicializador

rem ============================================================
rem  Kit Tecnico Pro - Inicializador unico (C# ou Java)
rem  Usa as dependencias portateis da pasta "Dependencias\".
rem  Uso direto:  Iniciar.bat csharp | java | preparar
rem ============================================================

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"
set "DEP=%ROOT%\Dependencias"
set "CS_DIR=%ROOT%\CSharp\KitTecnicoPro"
set "CS_EXE=%CS_DIR%\bin\Release\net8.0-windows\KitTecnicoPro.exe"
set "JAVA_DIR=%ROOT%\Java"

call :setup_env

rem --- Atalhos por argumento ---
if /i "%~1"=="csharp"   goto :run_csharp
if /i "%~1"=="cs"       goto :run_csharp
if /i "%~1"=="java"     goto :run_java
if /i "%~1"=="preparar" goto :prepare

:menu
cls
echo.
echo  ==========================================
echo              KIT TECNICO PRO
echo  ==========================================
echo.
echo    [1]  Versao C#   (WPF / .NET 8)
echo    [2]  Versao Java (JavaFX / Java 21)
echo    [3]  Preparar / atualizar dependencias (precisa de internet)
echo    [0]  Sair
echo.
echo  ------------------------------------------
echo    .NET  : %DOTNET_SRC%
echo    Java  : %JAVA_SRC%
echo    Maven : %MAVEN_SRC%
echo  ------------------------------------------
echo.
choice /c 1230 /n /m "  Escolha uma opcao: "
if errorlevel 4 goto :end
if errorlevel 3 goto :prepare
if errorlevel 2 goto :run_java
if errorlevel 1 goto :run_csharp
goto :menu

rem ============================================================
:run_csharp
echo.
echo  [C#] Compilando (incremental)...
where dotnet >nul 2>&1 || (echo  [ERRO] .NET SDK nao encontrado. Use a opcao [3]. & goto :fail)
dotnet build "%CS_DIR%\KitTecnicoPro.csproj" -c Release --nologo -v q
if errorlevel 1 (
    echo  [ERRO] Falha no build da versao C#.
    goto :fail
)
if not exist "%CS_EXE%" (
    echo  [ERRO] Executavel nao encontrado: %CS_EXE%
    goto :fail
)
echo  [C#] Abrindo Kit Tecnico Pro...
start "" "%CS_EXE%"
goto :end

rem ============================================================
:run_java
echo.
where java >nul 2>&1 || (echo  [ERRO] Java nao encontrado. Use a opcao [3]. & goto :fail)
where mvn  >nul 2>&1 || (echo  [ERRO] Maven nao encontrado. Use a opcao [3]. & goto :fail)
pushd "%JAVA_DIR%"
echo  [Java] Compilando e abrindo (esta janela fica aberta enquanto o app roda)...
rem -o = modo offline, usa somente o repositorio em Dependencias\m2
call mvn -o -q %MVN_REPO% compile javafx:run
if errorlevel 1 (
    echo.
    echo  [!] Falhou em modo offline. Tentando online...
    call mvn -q %MVN_REPO% compile javafx:run
    if errorlevel 1 (
        popd
        echo  [ERRO] Falha ao executar a versao Java.
        goto :fail
    )
)
popd
goto :end

rem ============================================================
:prepare
echo.
echo  [1/2] Baixando pacotes NuGet da versao C# para Dependencias\nuget ...
where dotnet >nul 2>&1 || (echo  [ERRO] .NET SDK nao encontrado. & goto :fail)
dotnet restore "%CS_DIR%\KitTecnicoPro.csproj" --nologo
if errorlevel 1 goto :fail
dotnet build "%CS_DIR%\KitTecnicoPro.csproj" -c Release --nologo -v q --no-restore
if errorlevel 1 goto :fail

echo.
echo  [2/2] Baixando bibliotecas Maven da versao Java para Dependencias\m2 ...
where mvn >nul 2>&1 || (echo  [ERRO] Maven nao encontrado. & goto :fail)
pushd "%JAVA_DIR%"
call mvn -q %MVN_REPO% dependency:go-offline compile
set "ERR=%errorlevel%"
popd
if not "%ERR%"=="0" goto :fail

echo.
echo  [OK] Dependencias prontas. As duas versoes podem rodar sem internet.
pause
if "%~1"=="" goto :menu
goto :end

rem ============================================================
:setup_env
rem Ferramentas portateis tem prioridade sobre as instaladas no sistema.
set "DOTNET_SRC=sistema"
set "JAVA_SRC=sistema"
set "MAVEN_SRC=sistema"

if exist "%DEP%\dotnet\dotnet.exe" (
    set "DOTNET_ROOT=%DEP%\dotnet"
    set "DOTNET_MULTILEVEL_LOOKUP=0"
    set "PATH=%DEP%\dotnet;%PATH%"
    set "DOTNET_SRC=portatil (Dependencias\dotnet)"
)
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_NOLOGO=1"
set "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"
set "NUGET_PACKAGES=%DEP%\nuget"

if exist "%DEP%\jdk\bin\java.exe" (
    set "JAVA_HOME=%DEP%\jdk"
    set "PATH=%DEP%\jdk\bin;%PATH%"
    set "JAVA_SRC=portatil (Dependencias\jdk)"
)
if exist "%DEP%\maven\bin\mvn.cmd" (
    set "MAVEN_HOME=%DEP%\maven"
    set "PATH=%DEP%\maven\bin;%PATH%"
    set "MAVEN_SRC=portatil (Dependencias\maven)"
)
set MVN_REPO="-Dmaven.repo.local=%DEP%\m2"
exit /b 0

rem ============================================================
:fail
echo.
pause
endlocal
exit /b 1

:end
endlocal
exit /b 0

