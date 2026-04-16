@echo off
setlocal

set MSBUILD="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
set CONFIG=%1
if "%CONFIG%"=="" set CONFIG=Debug

echo Building iRacingReplayDirector [%CONFIG%]...
%MSBUILD% iRacingReplayDirectorApps.sln -p:Configuration=%CONFIG% -p:Platform=x64 -restore -m -nologo -verbosity:minimal

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo BUILD FAILED.
    exit /b %ERRORLEVEL%
)

echo.
echo Build succeeded. Output: bin\%CONFIG%\iRacingReplayDirector.exe
