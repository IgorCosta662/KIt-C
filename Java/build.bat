@echo off
setlocal EnableExtensions
chcp 65001 >nul
title Kit Tecnico Pro - Java Build

cd /d "%~dp0"
call "%~dp0run.bat" %*
exit /b %errorlevel%
