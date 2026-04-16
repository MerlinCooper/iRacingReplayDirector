@echo off
call build.bat %1
if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%
call run.bat %1
