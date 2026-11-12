@echo off
setlocal

set OUTDIR=bin
set RELEASEDIR=release
set ZIPNAME=ime-watcher.zip

if not exist %OUTDIR% (
    echo bin folder not found. Run build.bat first.
    exit /b 1
)

if not exist %RELEASEDIR% mkdir %RELEASEDIR%

echo Creating release zip...
powershell -NoProfile -Command "Compress-Archive -Path '%OUTDIR%\*.exe', 'LICENSE' -DestinationPath '%RELEASEDIR%\%ZIPNAME%' -Force"
if errorlevel 1 goto :error

echo.
echo Release created: %CD%\%RELEASEDIR%\%ZIPNAME%
goto :end

:error
echo.
echo Failed to create zip.
exit /b 1

:end
endlocal
