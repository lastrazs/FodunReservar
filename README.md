# FodunReservas - Sistema de Reservas en Línea

Sistema MVC completo para reservas de sedes recreativas y apartamentos con autenticación, disponibilidad de alojamientos, cálculo de tarifas y administración.

## 📋 Resumen de Capacidades

✅ **Autenticación segura** con .NET Identity  
✅ **Consulta de disponibilidad** por fecha y número de personas  
✅ **Cálculo de tarifas** dinámico según temporada  
✅ **Reservas completas** con confirmación y seguimiento  
✅ **Recuperación de contraseña** por correo SMTP  
✅ **Panel de administración** para gestionar sedes, alojamientos, temporadas y tarifas  
✅ **Procedimientos almacenados** optimizados en SQL Server  

## 🏗️ Stack Tecnológico

| Componente | Versión/Tecnología |
|-----------|-------------------|
| **Framework** | .NET 8 MVC |
| **Base de datos** | Microsoft SQL Server |
| **ORM** | Entity Framework Core |
| **Seguridad** | ASP.NET Core Identity |
| **API de datos** | Procedimientos almacenados |

## 📁 Estructura del Proyecto

```
FodunReservas/
├── FodunReservas.Web/          # Aplicación MVC, controladores, vistas, formularios
├── FodunReservas.Business/     # Lógica de negocio, DTOs, interfaces, servicios
├── FodunReservas.Data/         # DbContext, repositorios, migraciones
├── Bd/                         # Scripts SQL de base de datos y procedimientos
└── [scripts PowerShell]        # Automatización de ejecución
```

### Descripción de Capas

- **FodunReservas.Web**: Interfaz MVC, autenticación, flujo de reservas, simulación de pago y panel administrativo
- **FodunReservas.Business**: Entidades de dominio, DTOs, interfaces de servicios y repositorios
- **FodunReservas.Data**: DbContext de EF Core, implementación de repositorios, migraciones, integración SQL Server

## 🚀 Instalación y Configuración Inicial

### Prerrequisitos

- **Microsoft SQL Server 2016+** (Express, Standard o Enterprise)
  - Descargar: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
  - O usar: SQL Server LocalDB (incluido con Visual Studio)
  
- **.NET SDK 8.0+**
  - Descargar: https://dotnet.microsoft.com/download
  - Verificar: `dotnet --version`

- **Visual Studio 2022+** o **VS Code** con extensiones de C#

### Paso 1: Clonar y Preparar el Proyecto

```powershell
# Navegar a la carpeta del proyecto
cd C:\Users\[usuario]\OneDrive\Escritorio\FodunReservar

# Restaurar dependencias NuGet
dotnet restore FodunReservas.sln

# Compilar la solución
dotnet build FodunReservas.sln
```

### Paso 2: Configurar la Base de Datos

#### 2.1 Crear Base de Datos Vacía

Abre **SQL Server Management Studio** o **SQL Server Object Explorer** en Visual Studio:

```sql
-- Crear BD vacía
CREATE DATABASE FodunReservas 
COLLATE SQL_Latin1_General_CP1_CI_AS;
GO
```

#### 2.2 Ejecutar Scripts SQL

En el orden indicado:

1. **Abrir** `Bd/BdScript.sql` en SQL Server Management Studio
2. **Ejecutar** el script completo (Ctrl + Shift + E)
   - Crea todas las tablas necesarias
   - Configura relaciones y restricciones
   - Insertaría datos de referencia

3. **Abrir** `Bd/SPfodun.sql` en SQL Server Management Studio
4. **Ejecutar** el script completo
   - Crea procedimientos almacenados de disponibilidad
   - Crea procedimientos de tarifas
   - Crea procedimiento de cálculo de reserva

**Verificación:**
```sql
-- En SQL Server, ejecutar en la BD FodunReservas:
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES;
-- Deberías ver: Sedes, Alojamiento, Tarifa, Reserva, DetalleReserva, Usuario, AspNetUsers, etc.

-- Verificar SPs:
SELECT NAME FROM sys.procedures WHERE NAME LIKE 'sp_%';
-- Deberías ver: sp_HabitacionesDisponiblesPorFecha, sp_HabitacionesDisponiblesPorFechaYPersonas, etc.
```

### Paso 3: Configurar Cadena de Conexión

`appsettings.json` y `appsettings.Development.json` son archivos locales y **no deben versionarse**.

1. Copia `FodunReservas.Web/appsettings.example.json` a `FodunReservas.Web/appsettings.json`
2. Copia `FodunReservas.Web/appsettings.Development.example.json` a `FodunReservas.Web/appsettings.Development.json`
3. Edita solo tus copias locales

Ejemplo para `FodunReservas.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "FodunReservasConnection": "Server=localhost;Database=FodunReservas;Integrated Security=true;Encrypt=False;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

**Opciones de servidor:**

| Opción | Valor | Casos de Uso |
|--------|-------|------------|
| `localhost` | Conexión local | Desarrollo en tu máquina |
| `.\SQLEXPRESS` | LocalDB Express | Visual Studio con SQL Server Express |
| `(localdb)\mssqllocaldb` | SQL Server LocalDB | Desarrollo sin SQL Server instalado |
| `NOMBRE\INSTANCIA` | SQL Server remoto | Servidor en red |

### Paso 4: Configurar SMTP (Correos)

Por defecto, el proyecto usa `PickupDirectory` en desarrollo y guarda correos localmente.

Configura SMTP real solo en tu archivo local `FodunReservas.Web/appsettings.json`.

Edita tu archivo local `FodunReservas.Web/appsettings.json` para envío de correos real:

```json
{
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "UserName": "tu-email@gmail.com",
    "Password": "tu-credencial-de-aplicacion",
    "FromEmail": "tu-email@gmail.com",
    "UsePickupDirectory": false
  }
}
```

**Para desarrollo sin enviar correos reales** (default):

Usa `FodunReservas.Web/appsettings.Development.json`:

```json
{
  "Smtp": {
    "UsePickupDirectory": true,
    "PickupDirectory": "App_Data/Emails"
  }
}
```

Los correos se guardarán en archivos `.eml` en `FodunReservas.Web/App_Data/Emails/` para revisar localmente.

## ▶️ Ejecutar la Aplicación

### Opción 1: Desde Visual Studio 2022

1. Abrir `FodunReservas.sln`
2. Establecer `FodunReservas.Web` como proyecto de inicio
3. Presionar **F5** o **Iniciar sin depuración (Ctrl+F5)**
4. Se abrirá automáticamente en `https://localhost:7000` (puerto puede variar)

### Opción 2: Desde Terminal/PowerShell

```powershell
# Navegar a la carpeta del proyecto
cd C:\Users\[usuario]\OneDrive\Escritorio\FodunReservar

# Ejecutar solo con dotnet
dotnet run --project FodunReservas.Web

# O especificar configuración Debug:
dotnet run --project FodunReservas.Web --configuration Debug
```

**Esperado:** Se abrirá la aplicación en `https://localhost:[puerto]`

## 📋 Uso de la Aplicación

### 1️⃣ Registro de Usuarios

1. En la página de inicio, haz clic en **Registrarse**
2. Completa el formulario con:
   - Número de documento (único)
   - Nombre completo
   - Email (único)
   - Contraseña (mínimo 8 caracteres, 1 mayúscula, 1 número)
   - Datos personales (teléfono, departamento, dirección, etc.)
   - Pregunta y respuesta secreta (para recuperación)
3. Haz clic en **Registrar**
4. Serás redirigido al login

### 2️⃣ Iniciar Sesión

1. En **Iniciar Sesión**, ingresa:
   - **Usuario**: Número de documento (el usado en registro)
   - **Contraseña**: La contraseña registrada
   - Marca **Recuérdame** (opcional)
2. Haz clic en **Iniciar Sesión**

⚠️ **Nota:** Después de 5 intentos fallidos, tu cuenta se bloquea por 15 minutos

### 3️⃣ Consultar Disponibilidad

1. En la página de inicio (tras login), selecciona:
   - **Sede** (lugar donde deseas reservar)
   - **Fecha de entrada** (mínimo mañana)
   - **Fecha de salida** (después de entrada)
   - **Número de personas** (mínimo 1)
2. Haz clic en **Buscar Disponibilidad**
3. Se muestran alojamientos disponibles con sus características

### 4️⃣ Crear Reserva

1. En los resultados de disponibilidad, selecciona un alojamiento
2. Haz clic en **Reservar**
3. Completa los datos adicionales:
   - Número de acompañantes (si aplica)
   - ¿Requiere lavandería?
   - Observaciones (opcional)
4. Revisa el **resumen de tarifa** (se calcula automáticamente según temporada)
5. Haz clic en **Confirmar Reserva**
6. La reserva se guarda con estado **Pendiente de confirmación**

### 5️⃣ Consultar Reservas

1. En el menú, ve a **Mis Reservas**
2. Verás todas tus reservas con:
   - Sede y alojamiento
   - Fechas y número de personas
   - Tarifa total
   - Estado (Pendiente, Confirmada, Cancelada, Completada)

### 6️⃣ Recuperar Contraseña

1. En la página de login, haz clic en **¿Olvidaste tu contraseña?**
2. Ingresa tu **número de documento**
3. Responde tu **pregunta secreta**
4. Se enviará un enlace de recuperación a tu correo
5. Sigue el enlace y establece una nueva contraseña

📧 **En desarrollo:** Revisa `FodunReservas.Web/App_Data/Emails/` para ver el correo de recuperación

## 🔑 Funciones de Administración

Accedible desde el menú si tienes rol de administrador (actualmente libre, descomentar si es necesario).

### Gestionar Sedes

- **Crear** nuevas sedes (recreativas o apartamentos)
- **Editar** información de sedes (nombre, ubicación, servicios)
- **Activar/Desactivar** sedes

### Gestionar Alojamientos

- **Crear** alojamientos dentro de sedes
- **Definir** capacidad, número de habitaciones
- **Agregar** características (baño, cocineta, TV, nevera, etc.)

### Gestionar Temporadas

- **Crear** temporadas (Baja, Media, Alta)
- **Definir** rangos de fechas
- **Establecer** prioridad de aplicación

### Gestionar Tarifas

- **Crear** tarifas por sede, temporada y número de alojamientos
- **Definir** valor por noche y adicionales por persona
- **Modificar** según negocio

## 🐛 Solución de Problemas

### "La cadena de conexión 'FodunReservasConnection' no está configurada"

**Solución:** Verifica que tu `appsettings.json` local tenga la cadena correcta y que SQL Server esté accesible.

```powershell
# Probar conexión a SQL Server
sqlcmd -S localhost -d FodunReservas -Q "SELECT 1"
```

### "Procedimiento almacenado no encontrado"

**Solución:** Ejecuta nuevamente `Bd/SPfodun.sql` en SQL Server Management Studio:

```sql
USE FodunReservas;
GO
-- Ejecutar todo el contenido de SPfodun.sql
```

### "No se envía el correo de recuperación"

**Solución (Desarrollo):** 
- Verifica que `UsePickupDirectory` sea `true` en tu `appsettings.Development.json` local
- Revisa carpeta: `FodunReservas.Web/App_Data/Emails/`
- Busca el archivo `.eml` más reciente

**Solución (Producción):**
- Configura credenciales SMTP reales solo en tu `appsettings.json` local
- Usa credenciales de aplicación de Gmail, Outlook, etc.
- Verifica firewall permita puerto 587 o 465

### "La aplicación no inicia"

**Solución:**

```powershell
# Limpiar build anterior
dotnet clean FodunReservas.sln

# Restaurar dependencias
dotnet restore FodunReservas.sln

# Compilar nuevamente
dotnet build FodunReservas.sln

# Ejecutar con logs detallados
dotnet run --project FodunReservas.Web --verbosity diagnostic
```

## 📊 Estructura de Datos

### Tablas Principales

| Tabla | Propósito |
|-------|----------|
| **Sedes** | Ubicaciones recreativas y apartamentos |
| **Alojamiento** | Unidades disponibles en cada sede |
| **Tarifa** | Precios por sede, temporada y tipo |
| **Temporada** | Períodos del año con rangos de fechas |
| **Reserva** | Registros de reservas de usuarios |
| **DetalleReserva** | Alojamientos incluidos en cada reserva |
| **Usuario** | Información extendida de usuarios (heredada de AspNetUsers) |
| **AspNetUsers** | Usuarios y autenticación (.NET Identity) |

### Procedimientos Almacenados

| SP | Función |
|----|---------|
| `sp_HabitacionesDisponiblesPorFecha` | Encuentra alojamientos libres en rango de fechas |
| `sp_HabitacionesDisponiblesPorFechaYPersonas` | Filtra además por capacidad para N personas |
| `sp_ConsultarTarifas` | Obtiene tarifa según sede, temporada y personas |
| `sp_CalcularTotalReserva` | Calcula tarifa total con adicionales |

## 📚 Documentación Técnica

Se incluye documento técnico completo (véase sección siguiente) con:
- Diagrama del modelo de datos relacional
- Explicación detallada de procedimientos almacenados
- Arquitectura de capas
- Flujo de lógica de disponibilidad y reservas
- Instrucciones de despliegue a producción

## 🤝 Soporte

Para problemas técnicos:
1. Revisa la sección **Solución de Problemas** arriba
2. Consulta los logs en consola o `FodunReservas.Web/bin/Debug`
3. Verifica que SQL Server esté ejecutándose: `sqlcmd -S localhost -Q "SELECT 1"`
