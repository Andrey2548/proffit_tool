@echo off
setlocal

call "%~dp0build-and-install.bat" /no-pause
if errorlevel 1 (
    echo Build or install failed. The game was not started.
    exit /b 1
)

set "GAME_EXE=%~dp0..\..\Probably Stolen.exe"
if not exist "%GAME_EXE%" (
    echo Game executable not found: "%GAME_EXE%"
    exit /b 1
)

echo Starting Probably Stolen...
start "" "%GAME_EXE%"
