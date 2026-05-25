USE master;
GO

IF DB_ID('FodunReservas') IS NOT NULL
BEGIN
    ALTER DATABASE FodunReservas SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE FodunReservas;
END
GO

CREATE DATABASE FodunReservas COLLATE SQL_Latin1_General_CP1_CI_AS;
GO

USE FodunReservas;
GO

CREATE TABLE Sedes (
    Id                      INT IDENTITY(1,1) PRIMARY KEY,
    TipoSede                NVARCHAR(50)  NOT NULL,
    Nombre                  NVARCHAR(100) NOT NULL,
    Descripcion             NVARCHAR(500) NULL,
    Ubicacion               NVARCHAR(100) NOT NULL,
    Direccion               NVARCHAR(200) NULL,
    TieneServicioLavanderia BIT           NOT NULL,
    ValorLavanderia         DECIMAL(12,2) NOT NULL,
    PermiteAcompanantes     BIT           NOT NULL,
    ValorAcompanante        DECIMAL(12,2) NOT NULL,
    Activa                  BIT           NOT NULL,
    CONSTRAINT CK_Sedes_TipoSede CHECK (TipoSede IN ('Sede Recreativa','Apartamento'))
);
GO

CREATE TABLE Temporada (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Nombre      NVARCHAR(50)  NOT NULL,
    Descripcion NVARCHAR(300) NULL,
    MesInicio   INT           NULL,
    DiaInicio   INT           NULL,
    MesFin      INT           NULL,
    DiaFin      INT           NULL,
    EsEspecial  BIT           NOT NULL,
    Prioridad   INT           NOT NULL CONSTRAINT DF_Temporada_Prioridad DEFAULT 99,
    Activa      BIT           NOT NULL
);
GO

CREATE TABLE TipoAlojamiento (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Nombre      NVARCHAR(50)  NOT NULL,
    Descripcion NVARCHAR(200) NULL
);
GO

CREATE TABLE Usuario (
    Id                   NVARCHAR(450) NOT NULL PRIMARY KEY,
    NroDocumento         NVARCHAR(20)  NOT NULL,
    NombreCompleto       NVARCHAR(150) NOT NULL,
    FechaNacimiento      DATE          NULL,
    Celular              NVARCHAR(20)  NULL,
    Departamento         NVARCHAR(80)  NULL,
    Municipio            NVARCHAR(80)  NULL,
    Barrio               NVARCHAR(80)  NULL,
    DireccionResidencia  NVARCHAR(200) NULL,
    TelefonoResidencia   NVARCHAR(20)  NULL,
    AutorizaCorreo       BIT           NOT NULL,
    AutorizaCelular      BIT           NOT NULL,
    PreguntaSecreta      NVARCHAR(200) NULL,
    RespuestaSecretaHash NVARCHAR(500) NULL,
    FechaRegistro        DATETIME2     NOT NULL CONSTRAINT DF_Usuario_FechaRegistro DEFAULT GETDATE(),
    Activo               BIT           NOT NULL,
    UserName             NVARCHAR(256) NULL,
    NormalizedUserName   NVARCHAR(256) NULL,
    Email                NVARCHAR(256) NULL,
    NormalizedEmail      NVARCHAR(256) NULL,
    EmailConfirmed       BIT           NOT NULL,
    PasswordHash         NVARCHAR(MAX) NULL,
    SecurityStamp        NVARCHAR(MAX) NULL,
    ConcurrencyStamp     NVARCHAR(MAX) NULL,
    PhoneNumber          NVARCHAR(MAX) NULL,
    PhoneNumberConfirmed BIT           NOT NULL,
    TwoFactorEnabled     BIT           NOT NULL,
    LockoutEnd           DATETIMEOFFSET NULL,
    LockoutEnabled       BIT           NOT NULL,
    AccessFailedCount    INT           NOT NULL
);
GO

CREATE TABLE AspNetRoles (
    Id               NVARCHAR(450) NOT NULL PRIMARY KEY,
    Name             NVARCHAR(256) NULL,
    NormalizedName   NVARCHAR(256) NULL,
    ConcurrencyStamp NVARCHAR(MAX) NULL
);
GO

CREATE TABLE Tarifa (
    Id                    INT IDENTITY(1,1) PRIMARY KEY,
    SedeId                INT           NOT NULL,
    TemporadaId           INT           NOT NULL,
    NumeroHabitaciones    INT           NOT NULL,
    PersonasBase          INT           NOT NULL,
    ValorNoche            DECIMAL(12,2) NOT NULL,
    ValorPersonaAdicional DECIMAL(12,2) NOT NULL,
    Activa                BIT           NOT NULL,
    CONSTRAINT FK_Tarifa_Sedes FOREIGN KEY (SedeId) REFERENCES Sedes(Id),
    CONSTRAINT FK_Tarifa_Temporada FOREIGN KEY (TemporadaId) REFERENCES Temporada(Id),
    CONSTRAINT UQ_Tarifa UNIQUE (SedeId, TemporadaId, NumeroHabitaciones, PersonasBase)
);
GO

CREATE TABLE Alojamiento (
    Id                 INT IDENTITY(1,1) PRIMARY KEY,
    SedeId             INT           NOT NULL,
    TipoAlojamientoId  INT           NOT NULL,
    NumeroAlojamiento  NVARCHAR(10)  NOT NULL,
    Descripcion        NVARCHAR(500) NULL,
    CapacidadMaxima    INT           NOT NULL,
    NumeroHabitaciones INT           NOT NULL,
    TieneBano          BIT           NOT NULL,
    TieneCocineta      BIT           NOT NULL,
    TieneTelevision    BIT           NOT NULL,
    TieneNevera        BIT           NOT NULL,
    TieneTerraza       BIT           NOT NULL,
    TieneSalaEstar     BIT           NOT NULL,
    TieneParqueadero   BIT           NOT NULL,
    EsNuevo            BIT           NOT NULL,
    Activo             BIT           NOT NULL,
    CONSTRAINT FK_Alojamiento_Sedes FOREIGN KEY (SedeId) REFERENCES Sedes(Id),
    CONSTRAINT FK_Alojamiento_TipoAlojamiento FOREIGN KEY (TipoAlojamientoId) REFERENCES TipoAlojamiento(Id),
    CONSTRAINT UQ_Alojamiento_Sede_Numero UNIQUE (SedeId, NumeroAlojamiento),
    CONSTRAINT CK_Alojamiento_Capacidad CHECK (CapacidadMaxima >= 1),
    CONSTRAINT CK_Alojamiento_NumHabitaciones CHECK (NumeroHabitaciones >= 1)
);
GO

CREATE TABLE AspNetRoleClaims (
    Id         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    RoleId     NVARCHAR(450) NOT NULL,
    ClaimType  NVARCHAR(MAX) NULL,
    ClaimValue NVARCHAR(MAX) NULL,
    CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id) ON DELETE CASCADE
);
GO

CREATE TABLE AspNetUserClaims (
    Id         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId     NVARCHAR(450) NOT NULL,
    ClaimType  NVARCHAR(MAX) NULL,
    ClaimValue NVARCHAR(MAX) NULL,
    CONSTRAINT FK_AspNetUserClaims_Usuario_UserId FOREIGN KEY (UserId) REFERENCES Usuario(Id) ON DELETE CASCADE
);
GO

CREATE TABLE AspNetUserLogins (
    LoginProvider       NVARCHAR(450) NOT NULL,
    ProviderKey         NVARCHAR(450) NOT NULL,
    ProviderDisplayName NVARCHAR(MAX) NULL,
    UserId              NVARCHAR(450) NOT NULL,
    CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
    CONSTRAINT FK_AspNetUserLogins_Usuario_UserId FOREIGN KEY (UserId) REFERENCES Usuario(Id) ON DELETE CASCADE
);
GO

CREATE TABLE AspNetUserRoles (
    UserId NVARCHAR(450) NOT NULL,
    RoleId NVARCHAR(450) NOT NULL,
    CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AspNetUserRoles_Usuario_UserId FOREIGN KEY (UserId) REFERENCES Usuario(Id) ON DELETE CASCADE
);
GO

CREATE TABLE AspNetUserTokens (
    UserId        NVARCHAR(450) NOT NULL,
    LoginProvider NVARCHAR(450) NOT NULL,
    Name          NVARCHAR(450) NOT NULL,
    Value         NVARCHAR(MAX) NULL,
    CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
    CONSTRAINT FK_AspNetUserTokens_Usuario_UserId FOREIGN KEY (UserId) REFERENCES Usuario(Id) ON DELETE CASCADE
);
GO

CREATE TABLE Reserva (
    Id                 INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioId          NVARCHAR(450) NOT NULL,
    SedeId             INT           NOT NULL,
    FechaReserva       DATETIME2     NOT NULL CONSTRAINT DF_Reserva_FechaReserva DEFAULT GETDATE(),
    FechaLlegada       DATE          NOT NULL,
    FechaSalida        DATE          NOT NULL,
    NroPersonas        INT           NOT NULL,
    NroHabitaciones    INT           NOT NULL,
    NroAcompanantes    INT           NOT NULL,
    RequiereLavanderia BIT           NOT NULL,
    DiasOrdinarios     INT           NOT NULL,
    DiasEspeciales     INT           NOT NULL,
    ValorTotal         DECIMAL(12,2) NOT NULL,
    Estado             NVARCHAR(20)  NOT NULL,
    Observaciones      NVARCHAR(500) NULL,
    CONSTRAINT FK_Reserva_Usuario FOREIGN KEY (UsuarioId) REFERENCES Usuario(Id),
    CONSTRAINT FK_Reserva_Sedes FOREIGN KEY (SedeId) REFERENCES Sedes(Id),
    CONSTRAINT CK_Reserva_Fechas CHECK (FechaSalida > FechaLlegada),
    CONSTRAINT CK_Reserva_Personas CHECK (NroPersonas >= 1),
    CONSTRAINT CK_Reserva_NumHabitaciones CHECK (NroHabitaciones >= 1),
    CONSTRAINT CK_Reserva_Acompanantes CHECK (NroAcompanantes BETWEEN 0 AND 10),
    CONSTRAINT CK_Reserva_Estado CHECK (Estado IN ('Pendiente','Confirmada','Cancelada','Completada'))
);
GO

CREATE TABLE Habitacion (
    Id                   INT IDENTITY(1,1) PRIMARY KEY,
    AlojamientoId        INT           NOT NULL,
    Nombre               NVARCHAR(50)  NOT NULL,
    CapacidadReferencial INT           NULL,
    TieneBanoPrivado     BIT           NOT NULL,
    Observaciones        NVARCHAR(250) NULL,
    Activa               BIT           NOT NULL,
    CONSTRAINT FK_Habitacion_Alojamiento FOREIGN KEY (AlojamientoId) REFERENCES Alojamiento(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Habitacion_Alojamiento_Nombre UNIQUE (AlojamientoId, Nombre)
);
GO

CREATE TABLE DetalleReserva (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    ReservaId     INT           NOT NULL,
    AlojamientoId INT           NOT NULL,
    ValorNoche    DECIMAL(12,2) NOT NULL,
    NroNoches     INT           NOT NULL,
    SubTotal      DECIMAL(12,2) NOT NULL,
    CONSTRAINT FK_DetalleReserva_Reserva FOREIGN KEY (ReservaId) REFERENCES Reserva(Id) ON DELETE CASCADE,
    CONSTRAINT FK_DetalleReserva_Alojamiento FOREIGN KEY (AlojamientoId) REFERENCES Alojamiento(Id),
    CONSTRAINT UQ_DetalleReserva UNIQUE (ReservaId, AlojamientoId),
    CONSTRAINT CK_DetalleReserva_NroNoches CHECK (NroNoches >= 1)
);
GO

CREATE TABLE __EFMigrationsHistory (
    MigrationId  NVARCHAR(150) NOT NULL PRIMARY KEY,
    ProductVersion NVARCHAR(32) NOT NULL
);
GO

CREATE UNIQUE INDEX IX_Usuario_NroDocumento ON Usuario(NroDocumento);
CREATE INDEX EmailIndex ON Usuario(NormalizedEmail);
CREATE UNIQUE INDEX UserNameIndex ON Usuario(NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
CREATE UNIQUE INDEX RoleNameIndex ON AspNetRoles(NormalizedName) WHERE NormalizedName IS NOT NULL;
CREATE INDEX IX_AspNetRoleClaims_RoleId ON AspNetRoleClaims(RoleId);
CREATE INDEX IX_AspNetUserClaims_UserId ON AspNetUserClaims(UserId);
CREATE INDEX IX_AspNetUserLogins_UserId ON AspNetUserLogins(UserId);
CREATE INDEX IX_AspNetUserRoles_RoleId ON AspNetUserRoles(RoleId);
CREATE UNIQUE INDEX IX_Alojamiento_SedeId_NumeroAlojamiento ON Alojamiento(SedeId, NumeroAlojamiento);
CREATE INDEX IX_Alojamiento_TipoAlojamientoId ON Alojamiento(TipoAlojamientoId);
CREATE INDEX IX_Habitacion_AlojamientoId_Nombre ON Habitacion(AlojamientoId, Nombre);
CREATE UNIQUE INDEX IX_Tarifa_SedeId_TemporadaId_NumeroHabitaciones_PersonasBase ON Tarifa(SedeId, TemporadaId, NumeroHabitaciones, PersonasBase);
CREATE INDEX IX_Tarifa_TemporadaId ON Tarifa(TemporadaId);
CREATE INDEX IX_Reserva_SedeId ON Reserva(SedeId);
CREATE INDEX IX_Reserva_UsuarioId ON Reserva(UsuarioId);
CREATE INDEX IX_Reserva_Fechas ON Reserva(FechaLlegada, FechaSalida);
CREATE INDEX IX_DetalleReserva_AlojamientoId ON DetalleReserva(AlojamientoId);
CREATE INDEX IX_DetalleReserva_ReservaId ON DetalleReserva(ReservaId);
CREATE UNIQUE INDEX IX_DetalleReserva_ReservaId_AlojamientoId ON DetalleReserva(ReservaId, AlojamientoId);
GO

INSERT INTO Sedes (TipoSede, Nombre, Descripcion, Ubicacion, Direccion, TieneServicioLavanderia, ValorLavanderia, PermiteAcompanantes, ValorAcompanante, Activa) VALUES
('Sede Recreativa','Sede Recreativa Villeta','Sede en barrio San Jorge, Villeta, Cundinamarca. A 90 km de Bogota por autopista Bogota-Medellin.','Villeta',NULL,0,0,1,5500,1),
('Sede Recreativa','El Placer - Fusagasuga','Sede en vereda El Placer, Fusagasuga, a 10 minutos del casco urbano.','Fusagasuga',NULL,0,0,1,5500,1),
('Sede Recreativa','Gonzalo Morante - Chinchina','Sede recreativa en Chinchina con alojamientos multiples y bloque de cabanas.','Chinchina',NULL,0,0,1,5500,1),
('Sede Recreativa','Tablones - Palmira','Sede recreativa Tablones en Palmira. Cuatro alojamientos.','Palmira',NULL,0,0,1,5500,1),
('Sede Recreativa','Manguruma - Santa Fe de Antioquia','Sede Manguruma. Alojamientos principales y bloque nuevo.','Santa Fe de Antioquia',NULL,0,0,1,5500,1),
('Sede Recreativa','Federman - Bogota','Zona humeda, gimnasio, sala de masajes, billar, salas de musica/video, cafeteria. 4 habitaciones.','Bogota',NULL,0,0,0,0,1),
('Apartamento','Edificio Suramericana - Medellin','Calle 49B No. 64B-15, Edificio Suramericana No. 6 Apto 1204. Cerca del campus Universidad Nacional.','Medellin','Calle 49B No. 64B-15',0,0,0,0,1),
('Apartamento','Edificio Reina 1 - Santa Marta','Carrera 3 No. 7-85, El Rodadero, Santa Marta. A tres cuadras de la playa.','Santa Marta','Carrera 3 No. 7-85',1,18000,0,0,1);
GO

INSERT INTO TipoAlojamiento (Nombre, Descripcion) VALUES
('Habitacion','Habitacion individual dentro de sede o apartamento'),
('Cabana','Unidad tipo cabana con sala y cocineta'),
('Alojamiento','Unidad compuesta con multiples habitaciones'),
('Apartamento','Apartamento completo con sala, cocina, banos y habitaciones');
GO

INSERT INTO Alojamiento (SedeId, TipoAlojamientoId, NumeroAlojamiento, Descripcion, CapacidadMaxima, NumeroHabitaciones, TieneBano, TieneCocineta, TieneTelevision, TieneNevera, TieneTerraza, TieneSalaEstar, TieneParqueadero, EsNuevo, Activo) VALUES
(1,1,'1','Cama doble+camarote, bano, nevera, TV, terraza cubierta',4,1,1,0,1,1,1,0,0,0,1),
(1,1,'2','Cama doble+camarote, bano, nevera, TV, terraza cubierta',4,1,1,0,1,1,1,0,0,0,1),
(1,1,'3','Cama doble+camarote, bano, nevera, TV, terraza cubierta',4,1,1,0,1,1,1,0,0,0,1),
(1,1,'4','Cama doble+camarote, bano, nevera, TV, terraza cubierta',4,1,1,0,1,1,1,0,0,0,1),
(1,1,'5','Cama doble+camarote, bano, nevera, TV, terraza cubierta',4,1,1,0,1,1,1,0,0,0,1),
(1,1,'6','Cama doble+camarote, bano, nevera, TV, terraza cubierta',4,1,1,0,1,1,1,0,0,0,1),
(1,1,'7','Cama doble+camarote, bano, nevera, TV, terraza cubierta',4,1,1,0,1,1,1,0,0,0,1),
(1,1,'8','Cama doble+camarote, bano, nevera, TV, terraza cubierta',4,1,1,0,1,1,1,0,0,0,1),
(2,3,'1','2 hab (doble+sencilla / sencilla), bano, TV',4,2,1,0,1,0,0,0,0,0,1),
(2,3,'2','2 hab (doble / 4 sencillas), bano, TV',6,2,1,0,1,0,0,0,0,0,1),
(2,3,'3','1 hab (doble+2 sencillas), bano, TV',4,1,1,0,1,0,0,0,0,0,1),
(2,3,'4','2 hab (doble+sencilla / sencilla), bano, TV',4,2,1,0,1,0,0,0,0,0,1),
(2,2,'5','Cabana nueva: sofa-cama, TV, bano, hab doble+sencilla, cocineta, nevera, terraza',4,1,1,1,1,1,1,1,0,1,1),
(2,2,'6','Cabana nueva: sofa-cama, TV, bano, hab doble+sencilla, cocineta, nevera, terraza',4,1,1,1,1,1,1,1,0,1,1),
(2,2,'7','Cabana nueva: sofa-cama, TV, bano, hab doble+sencilla, cocineta, nevera, terraza',4,1,1,1,1,1,1,1,0,1,1),
(2,2,'8','Cabana nueva: sofa-cama, TV, bano, hab doble+sencilla, cocineta, nevera, terraza',4,1,1,1,1,1,1,1,0,1,1),
(3,3,'1','Cocineta, bano, TV, 2 hab: 2 senc+adicionales / doble+sencilla',6,2,1,1,1,0,0,0,0,0,1),
(3,3,'2','Cocineta, bano, TV, 2 hab: doble+aux doble / 2 senc+aux',6,2,1,1,1,0,0,0,0,0,1),
(3,3,'4','Cocineta, bano, TV, 1 hab doble+sencilla',3,1,1,1,1,0,0,0,0,0,1),
(3,2,'3','Tipo A: cocineta, 2 banos, sala comedor, TV, 2 hab: doble / 2 senc+aux',6,2,1,1,1,0,0,1,0,0,1),
(3,2,'5','Tipo B: cocineta, bano, sala+sofa, TV, hab doble+sencilla',3,1,1,1,1,0,0,1,0,0,1),
(3,2,'6','Tipo B: cocineta, bano, sala+sofa, TV, hab doble+sencilla',3,1,1,1,1,0,0,1,0,0,1),
(4,3,'1','1 hab doble+camarote, TV, bano, cocineta+nevera, comedor',4,1,1,1,1,1,0,0,0,0,1),
(4,3,'2','1 hab doble+camarote, TV, bano, cocineta+nevera, comedor',4,1,1,1,1,1,0,0,0,0,1),
(4,3,'3','2 hab (doble+camarote / 2 camarotes), sala TV, bano, cocineta',8,2,1,1,1,0,0,1,0,0,1),
(4,3,'4','2 hab (doble+camarote / 2 camarotes), sala TV, bano, cocineta',8,2,1,1,1,0,0,1,0,0,1),
(5,1,'1','Cama doble+camarote, bano, terraza, TV',4,1,1,0,1,0,1,0,0,0,1),
(5,1,'2','Cama doble+camarote+sofa-cama, bano, terraza, TV',5,1,1,0,1,0,1,0,0,0,1),
(5,1,'3','Cama doble+camarote+sofa-cama, bano, terraza, TV',5,1,1,0,1,0,1,0,0,0,1),
(5,1,'A1','Bloque nuevo: 2 gemelas+camarote, bano, terraza-comedor, cocina, nevera, TV',4,1,1,1,1,1,1,0,0,1,1),
(5,1,'A2','Bloque nuevo: 2 gemelas+camarote, bano, terraza-comedor, cocina, nevera, TV',4,1,1,1,1,1,1,0,0,1,1),
(5,1,'A3','Bloque nuevo: 2 gemelas+camarote, bano, terraza-comedor, cocina, nevera, TV',4,1,1,1,1,1,1,0,0,1,1),
(5,1,'A4','Bloque nuevo: 2 gemelas+camarote, bano, terraza-comedor, cocina, nevera, TV',4,1,1,1,1,1,1,0,0,1,1),
(5,1,'A5','Bloque nuevo: 2 gemelas+camarote, bano, terraza-comedor, cocina, nevera, TV',4,1,1,1,1,1,1,0,0,1,1),
(5,1,'A6','Bloque nuevo: 2 gemelas+camarote, bano, terraza-comedor, cocina, nevera, TV',4,1,1,1,1,1,1,0,0,1,1),
(5,1,'A7','Bloque nuevo: 2 gemelas+camarote, bano, terraza-comedor, cocina, nevera, TV',4,1,1,1,1,1,1,0,0,1,1),
(5,1,'A8','Bloque nuevo: 2 gemelas+camarote, bano, terraza-comedor, cocina, nevera, TV',4,1,1,1,1,1,1,0,0,1,1),
(6,1,'1','Habitacion para alojamiento de asociados',4,1,1,0,0,0,0,0,0,0,1),
(6,1,'2','Habitacion para alojamiento de asociados',4,1,1,0,0,0,0,0,0,0,1),
(6,1,'3','Habitacion para alojamiento de asociados',4,1,1,0,0,0,0,0,0,0,1),
(6,1,'4','Habitacion para alojamiento de asociados',4,1,1,0,0,0,0,0,0,0,1),
(7,1,'1','2 camas sencillas y bano privado',2,1,1,0,0,0,0,0,0,0,1),
(7,1,'2','2 camas sencillas',2,1,0,0,0,0,0,0,0,0,1),
(7,1,'3','2 camas sencillas',2,1,0,0,0,0,0,0,0,0,1),
(7,1,'4','2 camas sencillas',2,1,0,0,0,0,0,0,0,0,1),
(7,1,'5','1 cama sencilla y bano privado',1,1,1,0,0,0,0,0,0,0,1),
(8,4,'202','Sala comedor, cocina, 2 banos, 3 hab, parqueadero. Max 8p',8,3,1,1,0,0,0,1,1,0,1),
(8,4,'301','Sala comedor, cocina, 1 bano, 2 hab, parqueadero. Max 6p',6,2,1,1,0,0,0,1,1,0,1),
(8,4,'401','Sala comedor, cocina, 1 bano, 2 hab, parqueadero. Max 6p',6,2,1,1,0,0,0,1,1,0,1);
GO

INSERT INTO Habitacion (AlojamientoId, Nombre, CapacidadReferencial, TieneBanoPrivado, Observaciones, Activa)
SELECT
    A.Id,
    CONCAT('Habitacion ', N.N),
    CASE WHEN A.NumeroHabitaciones = 1 THEN A.CapacidadMaxima ELSE NULL END,
    CASE WHEN N.N = 1 AND A.TieneBano = 1 THEN 1 ELSE 0 END,
    CASE
        WHEN A.NumeroHabitaciones = 1 THEN A.Descripcion
        ELSE CONCAT('Espacio interno del alojamiento ', A.NumeroAlojamiento)
    END,
    1
FROM Alojamiento A
INNER JOIN (VALUES (1),(2),(3),(4),(5)) N(N)
    ON N.N <= A.NumeroHabitaciones;
GO

INSERT INTO Temporada (Nombre, Descripcion, MesInicio, DiaInicio, MesFin, DiaFin, EsEspecial, Prioridad, Activa) VALUES
('Alta','Vacaciones de mitad y fin de ano. En los SP se evalua por ventanas vacacionales fijas.',NULL,NULL,NULL,NULL,0,10,1),
('Especial','Lunes a jueves para sedes recreativas, salvo reglas especiales de negocio.',NULL,NULL,NULL,NULL,1,20,1),
('Ordinaria','Tarifa base para fines de semana y periodos no especiales en sedes recreativas.',NULL,NULL,NULL,NULL,0,30,1),
('Baja','Temporada baja para apartamentos de Santa Marta.',NULL,NULL,NULL,NULL,0,40,1);
GO

INSERT INTO Tarifa (SedeId, TemporadaId, NumeroHabitaciones, PersonasBase, ValorNoche, ValorPersonaAdicional, Activa) VALUES
(1,3,1,4,70000,16000,1),(1,3,2,4,90000,16000,1),
(2,3,1,4,70000,16000,1),(2,3,2,4,90000,16000,1),
(3,3,1,4,70000,16000,1),(3,3,2,4,90000,16000,1),
(4,3,1,4,70000,16000,1),(4,3,2,4,90000,16000,1),
(5,3,1,4,70000,16000,1),(5,3,2,4,90000,16000,1),
(1,2,1,4,27000,11000,1),(1,2,2,4,37000,11000,1),
(2,2,1,4,27000,11000,1),(2,2,2,4,37000,11000,1),
(3,2,1,4,27000,11000,1),(3,2,2,4,37000,11000,1),
(4,2,1,4,27000,11000,1),(4,2,2,4,37000,11000,1),
(5,2,1,4,27000,11000,1),(5,2,2,4,37000,11000,1),
(7,3,1,1,63000,0,1),
(7,3,1,2,75000,0,1),
(8,4,2,6,89000,0,1),
(8,4,3,8,103000,0,1),
(8,1,2,6,124000,0,1),
(8,1,3,8,143000,0,1);
GO

INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20260523033456_InitialIdentityAndDomain', '8.0.0');
GO

PRINT 'FodunReservas creada correctamente con esquema compatible con EF Core e Identity.';
GO
