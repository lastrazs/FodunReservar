@echo off
REM ============================================================
REM FodunReservas - Development Commands for Windows
REM ============================================================

setlocal enabledelayedexpansion

if "%1"=="" goto help
if "%1"=="help" goto help
if "%1"=="--help" goto help
if "%1"=="-h" goto help

goto %1%

:help
echo.
echo  =========================================================
echo              FodunReservas - Development Tools
echo  =========================================================
echo.
echo  SETUP ^& INSTALLATION:
echo    dev install       - Restaurar dependencias NuGet
echo    dev build         - Compilar solución
echo    dev build-debug   - Compilar en Debug
echo    dev setup-env     - Crear archivo .env local
echo.
echo  DATABASE:
echo    dev migrate       - Ejecutar migraciones
echo    dev migrate-fresh - Recrear BD desde cero
echo    dev seed          - Cargar datos iniciales
echo.
echo  DEVELOPMENT:
echo    dev run           - Ejecutar aplicación
echo    dev watch         - Ejecutar con watch (recompila)
echo    dev test          - Ejecutar tests
echo    dev clean         - Limpiar build
echo.
echo  DEPLOYMENT:
echo    dev publish       - Publicar en Release
echo    dev docker-build  - Construir Docker
echo.
echo  UTILITIES:
echo    dev version       - Ver versiones instaladas
echo    dev info          - Ver información del proyecto
echo.
goto end

:install
echo [*] Restaurando dependencias NuGet...
dotnet restore FodunReservas.sln
if !errorlevel! equ 0 (
    echo [✓] Dependencias restauradas
) else (
    echo [✗] Error restaurando dependencias
)
goto end

:build
echo [*] Compilando solución (Release)...
dotnet build FodunReservas.sln -c Release
if !errorlevel! equ 0 (
    echo [✓] Compilación exitosa
) else (
    echo [✗] Error en compilación
)
goto end

:build-debug
echo [*] Compilando solución (Debug)...
dotnet build FodunReservas.sln -c Debug
if !errorlevel! equ 0 (
    echo [✓] Compilación Debug exitosa
) else (
    echo [✗] Error en compilación
)
goto end

:setup-env
if exist .env (
    echo [!] Archivo .env ya existe
) else (
    echo [*] Creando archivo .env...
    copy .env.example .env
    echo [✓] .env creado. Edítalo con tus credenciales locales
)
goto end

:migrate
echo [*] Ejecutando migraciones...
cd FodunReservas.Data
dotnet ef database update -s ../FodunReservas.Web
cd ..
if !errorlevel! equ 0 (
    echo [✓] Migraciones ejecutadas
) else (
    echo [✗] Error en migraciones
)
goto end

:migrate-fresh
echo [*] Eliminando BD y aplicando migraciones nuevamente...
cd FodunReservas.Data
dotnet ef database drop --force -s ../FodunReservas.Web
dotnet ef database update -s ../FodunReservas.Web
cd ..
if !errorlevel! equ 0 (
    echo [✓] BD recreada con éxito
) else (
    echo [✗] Error recreando BD
)
goto end

:seed
echo [*] Cargando datos iniciales...
echo [✓] Ver DatabaseBootstrapper.cs para datos iniciales
goto end

:run
echo [*] Ejecutando aplicación...
dotnet run --project FodunReservas.Web
goto end

:watch
echo [*] Ejecutando con watch (recompila al guardar)...
dotnet watch --project FodunReservas.Web run
goto end

:test
echo [*] Ejecutando tests...
dotnet test FodunReservas.sln --verbosity normal
if !errorlevel! equ 0 (
    echo [✓] Tests completados
) else (
    echo [✗] Algunos tests fallaron
)
goto end

:clean
echo [*] Limpiando archivos generados...
for /d /r . %%d in (bin) do @if exist "%%d" rd /s /q "%%d"
for /d /r . %%d in (obj) do @if exist "%%d" rd /s /q "%%d"
for /d /r . %%d in (.vs) do @if exist "%%d" rd /s /q "%%d"
dotnet clean FodunReservas.sln
echo [✓] Limpieza completada
goto end

:publish
echo [*] Publicando en Release...
dotnet publish FodunReservas.Web -c Release -o .\publish
if !errorlevel! equ 0 (
    echo [✓] Publicado en .\publish
) else (
    echo [✗] Error en publicación
)
goto end

:docker-build
echo [*] Construyendo imagen Docker...
docker build -t fodun-reservas:latest .
if !errorlevel! equ 0 (
    echo [✓] Imagen Docker construida
) else (
    echo [✗] Error construyendo Docker
)
goto end

:version
echo [info] Versiones:
dotnet --version
goto end

:info
echo [info] Información del Proyecto:
echo   Repositorio: FodunReservas
echo   Versión Framework: .NET 8.0 LTS
echo   Base de Datos: SQL Server
echo   Patrón: MVC en Capas
echo.
call :version
goto end

:end
endlocal
