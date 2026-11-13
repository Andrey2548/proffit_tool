@echo off
setlocal

set "PAUSE_AT_END=1"
if /i "%~1"=="/no-pause" set "PAUSE_AT_END=0"

pushd "%~dp0"
if errorlevel 1 (
    echo Failed to open the mod project folder.
    if "%PAUSE_AT_END%"=="1" pause
    exit /b 1
)

set "OUTPUT=%~dp0bin\Release\netstandard2.1"
set "DESTINATION=%~dp0..\..\Mods\proffit_tool"

echo Building Proffit Tool...
dotnet build -c Release
if errorlevel 1 goto build_failed

if not exist "%OUTPUT%\" (
    echo Build output folder was not found: "%OUTPUT%"
    popd
    if "%PAUSE_AT_END%"=="1" pause
    exit /b 1
)

echo Copying build output to "%DESTINATION%"...
robocopy "%OUTPUT%" "%DESTINATION%" /E
set "COPY_RESULT=%ERRORLEVEL%"
if %COPY_RESULT% GEQ 8 goto copy_failed

echo Build and install completed successfully.
popd
if "%PAUSE_AT_END%"=="1" pause
exit /b 0

:build_failed
echo Build failed. The mod files were not copied.
popd
if "%PAUSE_AT_END%"=="1" pause
exit /b 1

:copy_failed
echo Copy failed with robocopy exit code %COPY_RESULT%.
popd
if "%PAUSE_AT_END%"=="1" pause
exit /b %COPY_RESULT%
