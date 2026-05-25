USE FodunReservas;
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
