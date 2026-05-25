USE FodunReservas;
GO

CREATE OR ALTER PROCEDURE sp_CalcularTotalReserva
(
    @SedeId          INT,
    @AlojamientoId   INT,
    @NroHabitaciones INT,
    @NroPersonas     INT,
    @NroAcompanantes INT = 0,
    @RequiereLavanderia BIT = 0,
    @FechaEntrada    DATE,
    @FechaSalida     DATE
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NumeroHabitacionesReal INT;
    DECLARE @NumeroNoches           INT;
    DECLARE @TarifaId               INT;
    DECLARE @Temporada              NVARCHAR(50);
    DECLARE @PersonasBase           INT;
    DECLARE @ValorNoche             DECIMAL(12,2);
    DECLARE @ValorPersonaAdicional  DECIMAL(12,2);
    DECLARE @PersonasAdicionales    INT;
    DECLARE @ValorAdicionales       DECIMAL(12,2);
    DECLARE @SubtotalNoches         DECIMAL(12,2);
    DECLARE @ValorAcompanantes      DECIMAL(12,2) = 0;
    DECLARE @ValorLavanderia        DECIMAL(12,2) = 0;
    DECLARE @PermiteAcompanantes    BIT = 0;
    DECLARE @ValorAcompananteUnit   DECIMAL(12,2) = 0;
    DECLARE @TieneServicioLavanderia BIT = 0;
    DECLARE @ValorLavanderiaUnit    DECIMAL(12,2) = 0;

    DECLARE @Tarifa TABLE
    (
        TarifaId               INT,
        Temporada              NVARCHAR(50),
        NumeroHabitaciones     INT,
        PersonasBase           INT,
        ValorNoche             DECIMAL(12,2),
        ValorPersonaAdicional  DECIMAL(12,2),
        PersonasAdicionales    INT,
        ValorAdicional         DECIMAL(12,2)
    );

    IF (@FechaSalida <= @FechaEntrada)
    BEGIN
        RAISERROR('La fecha de salida debe ser mayor a la fecha de entrada.', 16, 1);
        RETURN;
    END;

    IF (@NroPersonas <= 0)
    BEGIN
        RAISERROR('El numero de personas debe ser mayor a cero.', 16, 1);
        RETURN;
    END;

    IF (@NroAcompanantes < 0 OR @NroAcompanantes > 10)
    BEGIN
        RAISERROR('El numero de acompanantes debe estar entre 0 y 10.', 16, 1);
        RETURN;
    END;

    SELECT @NumeroHabitacionesReal = NumeroHabitaciones
    FROM Alojamiento
    WHERE Id = @AlojamientoId
      AND SedeId = @SedeId
      AND Activo = 1;

    IF (@NumeroHabitacionesReal IS NULL)
    BEGIN
        RAISERROR('El alojamiento indicado no existe o no pertenece a la sede.', 16, 1);
        RETURN;
    END;

    IF (@NroHabitaciones <> @NumeroHabitacionesReal)
    BEGIN
        SET @NroHabitaciones = @NumeroHabitacionesReal;
    END;

    SET @NumeroNoches = DATEDIFF(DAY, @FechaEntrada, @FechaSalida);

    INSERT INTO @Tarifa
    EXEC sp_ConsultarTarifas
        @SedeId = @SedeId,
        @AlojamientoId = @AlojamientoId,
        @NroPersonas = @NroPersonas,
        @FechaEntrada = @FechaEntrada;

    SELECT TOP 1
        @TarifaId = TarifaId,
        @Temporada = Temporada,
        @PersonasBase = PersonasBase,
        @ValorNoche = ValorNoche,
        @ValorPersonaAdicional = ValorPersonaAdicional,
        @PersonasAdicionales = PersonasAdicionales,
        @ValorAdicionales = ValorAdicional
    FROM @Tarifa;

    IF (@TarifaId IS NULL)
    BEGIN
        RAISERROR('No se encontro una tarifa activa para la reserva solicitada.', 16, 1);
        RETURN;
    END;

    SET @SubtotalNoches = @ValorNoche * @NumeroNoches;
    SET @ValorAdicionales = @ValorAdicionales * @NumeroNoches;

    SELECT
        @PermiteAcompanantes = PermiteAcompanantes,
        @ValorAcompananteUnit = ValorAcompanante,
        @TieneServicioLavanderia = TieneServicioLavanderia,
        @ValorLavanderiaUnit = ValorLavanderia
    FROM Sedes
    WHERE Id = @SedeId;

    IF (@PermiteAcompanantes = 1 AND @NroAcompanantes > 0)
    BEGIN
        SET @ValorAcompanantes = @NroAcompanantes * @ValorAcompananteUnit;
    END;

    IF (@TieneServicioLavanderia = 1 AND @RequiereLavanderia = 1)
    BEGIN
        SET @ValorLavanderia = @ValorLavanderiaUnit;
    END;

    SELECT
        @NumeroNoches AS NumeroNoches,
        @NroHabitaciones AS NumeroHabitacionesAplicadas,
        @Temporada AS Temporada,
        @ValorNoche AS ValorNoche,
        @PersonasBase AS PersonasIncluidas,
        @PersonasAdicionales AS PersonasAdicionales,
        @ValorPersonaAdicional AS ValorPersonaAdicional,
        @SubtotalNoches AS SubtotalNoches,
        @ValorAdicionales AS ValorAdicionales,
        @ValorAcompanantes AS ValorAcompanantes,
        @ValorLavanderia AS ValorLavanderia,
        (@ValorAcompanantes + @ValorLavanderia) AS TotalServicios,
        (@SubtotalNoches + @ValorAdicionales + @ValorAcompanantes + @ValorLavanderia) AS TotalReserva;
END;
GO
