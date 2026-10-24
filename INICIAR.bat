@echo off
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
 echo Instala el SDK de .NET 8 y vuelve a ejecutar este archivo.
 pause
 exit /b 1
)
dotnet run --project GOZO.csproj -- --crear-dueno
if errorlevel 1 exit /b 1
echo Iniciando GOZO en http://localhost:5080
start "" cmd /c "timeout /t 6 >nul & start http://localhost:5080"
dotnet run --project GOZO.csproj
pause
