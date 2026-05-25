USE FodunReservas;
GO

/*
    Script maestro de procedimientos almacenados.
    Puede ejecutarse despues de BdScript.sql.
*/

CREATE OR ALTER PROCEDURE sp_HabitacionesDisponiblesPorFecha
(
    @FechaEntrada DATE,
    @FechaSalida  DATE,
    @SedeId       INT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF (@FechaSalida <= @FechaEntrada)
    BEGIN
        RAISERROR('La fecha de salida debe ser mayor a la fecha de entrada.', 16, 1);
        RETURN;
    END;

    SELECT
        A.Id,
        A.NumeroAlojamiento,
        TA.Nombre AS TipoAlojamiento,
        A.Descripcion,
        A.CapacidadMaxima,
        A.NumeroHabitaciones,
        A.TieneBano,
        A.TieneCocineta,
        A.TieneTelevision,
        A.TieneNevera,
        A.TieneTerraza,
        A.TieneSalaEstar,
        A.TieneParqueadero
    FROM Alojamiento A
    INNER JOIN TipoAlojamiento TA
        ON TA.Id = A.TipoAlojamientoId
    WHERE A.SedeId = @SedeId
      AND A.Activo = 1
      AND NOT EXISTS
      (
          SELECT 1
          FROM DetalleReserva DR
          INNER JOIN Reserva R
              ON R.Id = DR.ReservaId
          WHERE DR.AlojamientoId = A.Id
            AND R.Estado IN ('Pendiente', 'Confirmada')
            AND @FechaEntrada < R.FechaSalida
            AND @FechaSalida > R.FechaLlegada
      )
    ORDER BY A.NumeroAlojamiento;
END;
GO

CREATE OR ALTER PROCEDURE sp_HabitacionesDisponiblesPorFechaYPersonas
(
    @FechaEntrada DATE,
    @FechaSalida  DATE,
    @NroPersonas  INT,
    @SedeId       INT
)
AS
BEGIN
    SET NOCOUNT ON;

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

    SELECT
        A.Id,
        A.NumeroAlojamiento,
        TA.Nombre AS TipoAlojamiento,
        A.Descripcion,
        A.CapacidadMaxima,
        A.NumeroHabitaciones,
        A.TieneBano,
        A.TieneCocineta,
        A.TieneTelevision,
        A.TieneNevera,
        A.TieneTerraza,
        A.TieneSalaEstar,
        A.TieneParqueadero
    FROM Alojamiento A
    INNER JOIN TipoAlojamiento TA
        ON TA.Id = A.TipoAlojamientoId
    WHERE A.SedeId = @SedeId
      AND A.Activo = 1
      AND A.CapacidadMaxima >= @NroPersonas
      AND NOT EXISTS
      (
          SELECT 1
          FROM DetalleReserva DR
          INNER JOIN Reserva R
              ON R.Id = DR.ReservaId
          WHERE DR.AlojamientoId = A.Id
            AND R.Estado IN ('Pendiente', 'Confirmada')
            AND @FechaEntrada < R.FechaSalida
            AND @FechaSalida > R.FechaLlegada
      )
    ORDER BY A.CapacidadMaxima, A.NumeroAlojamiento;
END;
GO

CREATE OR ALTER PROCEDURE sp_ConsultarTarifas
(
    @SedeId        INT,
    @AlojamientoId INT,
    @NroPersonas   INT,
    @FechaEntrada  DATE
)
AS
BEGIN
    SET NOCOUNT ON;
    SET DATEFIRST 1;

    DECLARE @NumeroHabitaciones INT;
    DECLARE @TipoSede           NVARCHAR(50);
    DECLARE @Ubicacion          NVARCHAR(100);
    DECLARE @TemporadaId        INT;
    DECLARE @MesDia             INT;

    IF (@NroPersonas <= 0)
    BEGIN
        RAISERROR('El numero de personas debe ser mayor a cero.', 16, 1);
        RETURN;
    END;

    SELECT
        @NumeroHabitaciones = A.NumeroHabitaciones,
        @TipoSede = S.TipoSede,
        @Ubicacion = S.Ubicacion
    FROM Alojamiento A
    INNER JOIN Sedes S
        ON S.Id = A.SedeId
    WHERE A.Id = @AlojamientoId
      AND A.SedeId = @SedeId
      AND A.Activo = 1;

    IF (@NumeroHabitaciones IS NULL)
    BEGIN
        RAISERROR('El alojamiento indicado no existe o no pertenece a la sede.', 16, 1);
        RETURN;
    END;

    SET @MesDia = (MONTH(@FechaEntrada) * 100) + DAY(@FechaEntrada);

    SELECT TOP 1 @TemporadaId = T.Id
    FROM Temporada T
    WHERE T.Activa = 1
      AND T.MesInicio IS NOT NULL
      AND T.DiaInicio IS NOT NULL
      AND T.MesFin IS NOT NULL
      AND T.DiaFin IS NOT NULL
      AND
      (
          (
              ((T.MesInicio * 100) + T.DiaInicio) <= ((T.MesFin * 100) + T.DiaFin)
              AND @MesDia BETWEEN ((T.MesInicio * 100) + T.DiaInicio) AND ((T.MesFin * 100) + T.DiaFin)
          )
          OR
          (
              ((T.MesInicio * 100) + T.DiaInicio) > ((T.MesFin * 100) + T.DiaFin)
              AND (@MesDia >= ((T.MesInicio * 100) + T.DiaInicio) OR @MesDia <= ((T.MesFin * 100) + T.DiaFin))
          )
      )
      AND EXISTS
      (
          SELECT 1
          FROM Tarifa TR
          WHERE TR.SedeId = @SedeId
            AND TR.TemporadaId = T.Id
            AND TR.NumeroHabitaciones = @NumeroHabitaciones
            AND TR.Activa = 1
      )
    ORDER BY T.Prioridad, T.Id;

    IF (@TemporadaId IS NULL AND @TipoSede = 'Apartamento' AND @Ubicacion LIKE '%Santa Marta%')
    BEGIN
        IF ((@MesDia BETWEEN 615 AND 731) OR (@MesDia >= 1215) OR (@MesDia <= 115))
        BEGIN
            SELECT TOP 1 @TemporadaId = Id
            FROM Temporada
            WHERE Nombre = 'Alta' AND Activa = 1
            ORDER BY Prioridad, Id;
        END
        ELSE
        BEGIN
            SELECT TOP 1 @TemporadaId = Id
            FROM Temporada
            WHERE Nombre = 'Baja' AND Activa = 1
            ORDER BY Prioridad, Id;
        END;
    END
    ELSE IF (@TemporadaId IS NULL)
    BEGIN
        IF ((@MesDia BETWEEN 615 AND 731) OR (@MesDia >= 1215) OR (@MesDia <= 115))
           AND EXISTS
           (
               SELECT 1
               FROM Tarifa T
               INNER JOIN Temporada TP
                   ON TP.Id = T.TemporadaId
               WHERE T.SedeId = @SedeId
                 AND T.NumeroHabitaciones = @NumeroHabitaciones
                 AND T.Activa = 1
                 AND TP.Nombre = 'Alta'
                 AND TP.Activa = 1
           )
        BEGIN
            SELECT TOP 1 @TemporadaId = Id
            FROM Temporada
            WHERE Nombre = 'Alta' AND Activa = 1
            ORDER BY Prioridad, Id;
        END
        ELSE IF DATEPART(WEEKDAY, @FechaEntrada) BETWEEN 1 AND 4
            AND EXISTS
            (
                SELECT 1
                FROM Tarifa T
                INNER JOIN Temporada TP
                    ON TP.Id = T.TemporadaId
                WHERE T.SedeId = @SedeId
                  AND T.NumeroHabitaciones = @NumeroHabitaciones
                  AND T.Activa = 1
                  AND TP.Nombre = 'Especial'
                  AND TP.Activa = 1
            )
        BEGIN
            SELECT TOP 1 @TemporadaId = Id
            FROM Temporada
            WHERE Nombre = 'Especial' AND Activa = 1
            ORDER BY Prioridad, Id;
        END
        ELSE
        BEGIN
            SELECT TOP 1 @TemporadaId = Id
            FROM Temporada
            WHERE Nombre = 'Ordinaria' AND Activa = 1
            ORDER BY Prioridad, Id;
        END;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM Tarifa
        WHERE SedeId = @SedeId
          AND TemporadaId = @TemporadaId
          AND NumeroHabitaciones = @NumeroHabitaciones
          AND Activa = 1
    )
    BEGIN
        SELECT TOP 1 @TemporadaId = T.TemporadaId
        FROM Tarifa T
        INNER JOIN Temporada TP
            ON TP.Id = T.TemporadaId
        WHERE T.SedeId = @SedeId
          AND T.NumeroHabitaciones = @NumeroHabitaciones
          AND T.Activa = 1
          AND TP.Activa = 1
        ORDER BY TP.Prioridad, TP.Id;
    END;

    SELECT TOP 1
        T.Id AS TarifaId,
        TP.Nombre AS Temporada,
        T.NumeroHabitaciones,
        T.PersonasBase,
        T.ValorNoche,
        T.ValorPersonaAdicional,
        CASE
            WHEN @NroPersonas > T.PersonasBase THEN @NroPersonas - T.PersonasBase
            ELSE 0
        END AS PersonasAdicionales,
        CASE
            WHEN @NroPersonas > T.PersonasBase THEN (@NroPersonas - T.PersonasBase) * T.ValorPersonaAdicional
            ELSE 0
        END AS ValorAdicional
    FROM Tarifa T
    INNER JOIN Temporada TP
        ON TP.Id = T.TemporadaId
    WHERE T.SedeId = @SedeId
      AND T.TemporadaId = @TemporadaId
      AND T.NumeroHabitaciones = @NumeroHabitaciones
      AND T.Activa = 1
    ORDER BY
        CASE WHEN T.PersonasBase <= @NroPersonas THEN 0 ELSE 1 END,
        CASE WHEN T.PersonasBase <= @NroPersonas THEN T.PersonasBase END DESC,
        CASE WHEN T.PersonasBase > @NroPersonas THEN T.PersonasBase END ASC;
END;
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
        TarifaId              INT,
        Temporada             NVARCHAR(50),
        NumeroHabitaciones    INT,
        PersonasBase          INT,
        ValorNoche            DECIMAL(12,2),
        ValorPersonaAdicional DECIMAL(12,2),
        PersonasAdicionales   INT,
        ValorAdicional        DECIMAL(12,2)
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

PRINT 'Procedimientos almacenados aplicados correctamente.';
GO
