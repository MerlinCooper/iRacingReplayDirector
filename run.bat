@echo off
setlocal

set CONFIG=%1
if "%CONFIG%"=="" set CONFIG=Debug

set EXE=bin\%CONFIG%\iRacingReplayDirector.exe

if not exist "%EXE%" (
    echo %EXE% not found. Building first...
    call build.bat %CONFIG%
    if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%
)

echo Starting %EXE%...
start "" "%EXE%"
