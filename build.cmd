@echo off
REM Builds the full Chromatics solution from the repo root.
REM Usage:
REM   build.cmd           -- Release build
REM   build.cmd Debug     -- Debug build
REM   build.cmd Release   -- explicit Release build
REM
REM Packaging (dotnet publish + vpk pack) is handled by publish.py.
setlocal

set CONFIG=%~1
if "%CONFIG%"=="" set CONFIG=Release

pushd "%~dp0"

REM Resolve Sharlayan reference (NuGet or local DLL) before restore/build.
python prebuild.py --configuration %CONFIG%
if errorlevel 1 (
    popd
    exit /b 1
)

dotnet build Chromatics.sln --configuration %CONFIG% --nologo
set EXITCODE=%ERRORLEVEL%

if %EXITCODE%==0 (
    REM Copy loose Build Dependencies (e.g. Interop.AuraServiceLib.dll).
    REM Sharlayan DLLs are handled by Sharlayan.Reference.props so we
    REM explicitly skip the Sharlayan subfolder to avoid clobbering the
    REM NuGet-resolved version with a stale local copy on Release builds.
    xcopy /Y /Q "Build Dependencies\*.dll" "Chromatics\bin\%CONFIG%\net10.0-windows7.0\"
)

popd
exit /b %EXITCODE%
