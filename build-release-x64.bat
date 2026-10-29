@echo off
setlocal
rem One-click build: BiliLite x64 Release (MSIX output -> Z:\)
rem Usage: double-click, or `build-release-x64.bat -Clean` / `-NoVersionBump`

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build-release-x64.ps1" %*
set "EXITCODE=%ERRORLEVEL%"

echo.
if "%EXITCODE%"=="0" (
    echo Build succeeded.
) else (
    echo Build failed, exit code %EXITCODE%.
)
pause
exit /b %EXITCODE%
