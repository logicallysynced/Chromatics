@echo off
REM Builds the full Chromatics solution from the repo root.
REM Usage:
REM   build.cmd           -- Release build
REM   build.cmd Debug     -- Debug build
REM   build.cmd Release   -- explicit Release build
setlocal

set CONFIG=%~1
if "%CONFIG%"=="" set CONFIG=Release

pushd "%~dp0"
dotnet build Chromatics.sln --configuration %CONFIG% --nologo
set EXITCODE=%ERRORLEVEL%

if %EXITCODE%==0 (
    xcopy /Y /Q "Build Dependencies\*" "Chromatics\bin\%CONFIG%\net8.0-windows7.0\"
)

popd
exit /b %EXITCODE%
