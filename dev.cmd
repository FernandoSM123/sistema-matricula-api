@echo off
rem Levanta el entorno de desarrollo: smtp4dev y la API con dotnet watch, cada uno en su ventana.
if "%~1"=="api" goto api

cd /d "%~dp0"

where smtp4dev >nul 2>&1
if errorlevel 1 (
    echo No se encontro smtp4dev. Instalalo con: dotnet tool install --global Rnwood.Smtp4dev
    pause
    exit /b 1
)

start "smtp4dev" cmd /k smtp4dev --smtpport=2525 --urls=http://localhost:5080
start "Matricula API" cmd /k ""%~f0" api"
exit /b

:api
echo ============================================================
echo  Matricula API - modo desarrollo (dotnet watch)
echo ============================================================
echo  API ........... http://localhost:5022
echo  Scalar ........ http://localhost:5022/scalar
echo  Health check .. http://localhost:5022/health
echo  smtp4dev ...... http://localhost:5080
echo ------------------------------------------------------------
echo  Los cambios se aplican al guardar. Ctrl+R reinicia la API,
echo  Ctrl+C la detiene.
echo ============================================================
echo.
set DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true
dotnet watch --project src/Matricula.Api --launch-profile http
