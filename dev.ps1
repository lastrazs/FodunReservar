# ============================================================
# FodunReservas - Development Commands for PowerShell
# ============================================================
# Uso: ./dev.ps1 [comando]
# Ejemplo: ./dev.ps1 install
# ============================================================

param(
    [Parameter(Position = 0)]
    [string]$Command = "help"
)

$ErrorActionPreference = "Continue"

function Write-Header {
    param([string]$Message)
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
}

function Write-Info {
    param([string]$Message)
    Write-Host "[*] $Message" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Message)
    Write-Host "[✓] $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "[✗] $Message" -ForegroundColor Red
}

function Show-Help {
    Write-Host ""
    Write-Header "FodunReservas - Development Tools"
    Write-Host ""
    Write-Host "SETUP & INSTALLATION:" -ForegroundColor Green
    Write-Host "  ./dev.ps1 install       - Restaurar dependencias NuGet"
    Write-Host "  ./dev.ps1 build         - Compilar solución"
    Write-Host "  ./dev.ps1 build-debug   - Compilar en Debug"
    Write-Host "  ./dev.ps1 setup-env     - Crear archivo .env local"
    Write-Host ""
    Write-Host "DATABASE:" -ForegroundColor Green
    Write-Host "  ./dev.ps1 migrate       - Ejecutar migraciones"
    Write-Host "  ./dev.ps1 migrate-fresh - Recrear BD desde cero"
    Write-Host "  ./dev.ps1 seed          - Cargar datos iniciales"
    Write-Host "  ./dev.ps1 db-clean      - Limpiar BD"
    Write-Host ""
    Write-Host "DEVELOPMENT:" -ForegroundColor Green
    Write-Host "  ./dev.ps1 run           - Ejecutar aplicación"
    Write-Host "  ./dev.ps1 watch         - Ejecutar con watch"
    Write-Host "  ./dev.ps1 test          - Ejecutar tests"
    Write-Host "  ./dev.ps1 clean         - Limpiar build"
    Write-Host ""
    Write-Host "DEPLOYMENT:" -ForegroundColor Green
    Write-Host "  ./dev.ps1 publish       - Publicar en Release"
    Write-Host "  ./dev.ps1 docker-build  - Construir Docker"
    Write-Host ""
    Write-Host "UTILITIES:" -ForegroundColor Green
    Write-Host "  ./dev.ps1 version       - Ver versiones"
    Write-Host "  ./dev.ps1 info          - Ver información"
    Write-Host ""
}

function Install {
    Write-Info "Restaurando dependencias NuGet..."
    dotnet restore FodunReservas.sln
    if ($?) { Write-Success "Dependencias restauradas" }
    else { Write-Error "Error restaurando dependencias" }
}

function Build {
    Write-Info "Compilando solución (Release)..."
    dotnet build FodunReservas.sln -c Release
    if ($?) { Write-Success "Compilación exitosa" }
    else { Write-Error "Error en compilación" }
}

function BuildDebug {
    Write-Info "Compilando solución (Debug)..."
    dotnet build FodunReservas.sln -c Debug
    if ($?) { Write-Success "Compilación Debug exitosa" }
    else { Write-Error "Error en compilación" }
}

function SetupEnv {
    if (Test-Path ".env") {
        Write-Info "Archivo .env ya existe"
    } else {
        Write-Info "Creando archivo .env..."
        Copy-Item ".env.example" ".env"
        Write-Success ".env creado. Edítalo con tus credenciales locales"
    }
}

function Migrate {
    Write-Info "Ejecutando migraciones..."
    Push-Location FodunReservas.Data
    dotnet ef database update -s ../FodunReservas.Web
    Pop-Location
    if ($?) { Write-Success "Migraciones ejecutadas" }
    else { Write-Error "Error en migraciones" }
}

function MigrateFresh {
    Write-Info "Eliminando BD y aplicando migraciones nuevamente..."
    Push-Location FodunReservas.Data
    dotnet ef database drop --force -s ../FodunReservas.Web
    dotnet ef database update -s ../FodunReservas.Web
    Pop-Location
    if ($?) { Write-Success "BD recreada con éxito" }
    else { Write-Error "Error recreando BD" }
}

function Seed {
    Write-Info "Cargando datos iniciales..."
    Write-Success "Ver DatabaseBootstrapper.cs para datos iniciales"
}

function Run {
    Write-Info "Ejecutando aplicación..."
    dotnet run --project FodunReservas.Web
}

function Watch {
    Write-Info "Ejecutando con watch (recompila al guardar)..."
    dotnet watch --project FodunReservas.Web run
}

function Test {
    Write-Info "Ejecutando tests..."
    dotnet test FodunReservas.sln --verbosity normal
    if ($?) { Write-Success "Tests completados" }
    else { Write-Error "Algunos tests fallaron" }
}

function Clean {
    Write-Info "Limpiando archivos generados..."
    Get-ChildItem -Path . -Include bin, obj, .vs -Recurse -Directory | Remove-Item -Recurse -Force
    dotnet clean FodunReservas.sln
    Write-Success "Limpieza completada"
}

function Publish {
    Write-Info "Publicando en Release..."
    dotnet publish FodunReservas.Web -c Release -o ./publish
    if ($?) { Write-Success "Publicado en ./publish" }
    else { Write-Error "Error en publicación" }
}

function DockerBuild {
    Write-Info "Construyendo imagen Docker..."
    docker build -t fodun-reservas:latest .
    if ($?) { Write-Success "Imagen Docker construida" }
    else { Write-Error "Error construyendo Docker" }
}

function ShowVersion {
    Write-Host ""
    Write-Host "Versiones:" -ForegroundColor Cyan
    Write-Host "  .NET: $(dotnet --version)"
    Write-Host "  PowerShell: $($PSVersionTable.PSVersion.Major).$($PSVersionTable.PSVersion.Minor)"
}

function ShowInfo {
    Write-Header "Información del Proyecto"
    Write-Host "  Repositorio: FodunReservas"
    Write-Host "  Versión Framework: .NET 8.0 LTS"
    Write-Host "  Base de Datos: SQL Server"
    Write-Host "  Patrón: MVC en Capas"
    ShowVersion
    Write-Host ""
}

# Ejecutar comando
switch ($Command.ToLower()) {
    "help" { Show-Help }
    "install" { Install }
    "build" { Build }
    "build-debug" { BuildDebug }
    "setup-env" { SetupEnv }
    "migrate" { Migrate }
    "migrate-fresh" { MigrateFresh }
    "seed" { Seed }
    "db-clean" { MigrateFresh }
    "run" { Run }
    "watch" { Watch }
    "test" { Test }
    "clean" { Clean }
    "publish" { Publish }
    "docker-build" { DockerBuild }
    "version" { ShowVersion }
    "info" { ShowInfo }
    default {
        Write-Error "Comando desconocido: $Command"
        Write-Host ""
        Show-Help
    }
}
