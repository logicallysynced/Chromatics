@echo off
REM Runs the Chromatics unit test suite from the repo root.
REM Usage:
REM   test.cmd            -- Release, minimal output
REM   test.cmd Debug      -- Debug configuration
REM   test.cmd Release    -- explicit Release configuration
setlocal

set CONFIG=%~1
if "%CONFIG%"=="" set CONFIG=Release

pushd "%~dp0"

REM Resolve Sharlayan reference for the test project too.
python prebuild.py --configuration %CONFIG%
if errorlevel 1 (
    popd
    exit /b 1
)

dotnet test Chromatics.Tests\Chromatics.Tests.csproj --configuration %CONFIG% --nologo --verbosity minimal
set EXITCODE=%ERRORLEVEL%
popd
exit /b %EXITCODE%
