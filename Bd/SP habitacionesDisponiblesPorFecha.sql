USE FodunReservas;
GO

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
