@echo off
setlocal

set CSC="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set OUTDIR=bin
set OUT=%OUTDIR%\ime-watcher.exe

set OPTS=/nologo /platform:x64 /optimize+ /debug- /warn:4 /target:exe

if not exist %OUTDIR% mkdir %OUTDIR%

echo Building release...
%CSC% %OPTS% /out:%OUT% ime-watcher.cs
if errorlevel 1 goto :error

echo.
echo Release build succeeded.
echo Output: %CD%\%OUT%
goto :end

:error
echo.
echo Build failed.
exit /b 1

:end
endlocal
