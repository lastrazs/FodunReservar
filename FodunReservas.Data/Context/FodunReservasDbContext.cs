using FodunReservas.Business.DTOs;
using FodunReservas.Business.Entities;
using FodunReservas.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FodunReservas.Data.Context;

public class FodunReservasDbContext(DbContextOptions<FodunReservasDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<Sede> Sedes => Set<Sede>();
    public DbSet<TipoAlojamiento> TiposAlojamiento => Set<TipoAlojamiento>();
    public DbSet<Alojamiento> Alojamientos => Set<Alojamiento>();
    public DbSet<Habitacion> Habitaciones => Set<Habitacion>();
    public DbSet<Temporada> Temporadas => Set<Temporada>();
    public DbSet<Tarifa> Tarifas => Set<Tarifa>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<DetalleReserva> DetallesReserva => Set<DetalleReserva>();
    public DbSet<DisponibilidadResult> DisponibilidadResultados => Set<DisponibilidadResult>();
    public DbSet<ConsultaTarifaResult> ConsultaTarifaResultados => Set<ConsultaTarifaResult>();
    public DbSet<CalculoReservaResult> CalculoReservaResultados => Set<CalculoReservaResult>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Usuario");
            entity.HasIndex(x => x.NroDocumento).IsUnique();
            entity.Property(x => x.NroDocumento).HasMaxLength(20).IsRequired();
            entity.Property(x => x.NombreCompleto).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.FechaNacimiento).HasColumnType("date");
            entity.Property(x => x.Celular).HasMaxLength(20);
            entity.Property(x => x.Departamento).HasMaxLength(80);
            entity.Property(x => x.Municipio).HasMaxLength(80);
            entity.Property(x => x.Barrio).HasMaxLength(80);
            entity.Property(x => x.DireccionResidencia).HasMaxLength(200);
            entity.Property(x => x.TelefonoResidencia).HasMaxLength(20);
            entity.Property(x => x.PreguntaSecreta).HasMaxLength(200);
            entity.Property(x => x.RespuestaSecretaHash).HasMaxLength(500);
            entity.Property(x => x.FechaRegistro).HasDefaultValueSql("GETDATE()");
        });

        builder.Entity<Sede>(entity =>
        {
            entity.ToTable("Sedes", table =>
            {
                table.HasCheckConstraint("CK_Sedes_TipoSede", "[TipoSede] IN ('Sede Recreativa','Apartamento')");
            });

            entity.Property(x => x.TipoSede).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Descripcion).HasMaxLength(500);
            entity.Property(x => x.Ubicacion).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Direccion).HasMaxLength(200);
            entity.Property(x => x.ValorLavanderia).HasColumnType("decimal(12,2)");
            entity.Property(x => x.ValorAcompanante).HasColumnType("decimal(12,2)");
        });

        builder.Entity<TipoAlojamiento>(entity =>
        {
            entity.ToTable("TipoAlojamiento");
            entity.Property(x => x.Nombre).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Descripcion).HasMaxLength(200);
        });

        builder.Entity<Alojamiento>(entity =>
        {
            entity.ToTable("Alojamiento");
            entity.HasIndex(x => new { x.SedeId, x.NumeroAlojamiento }).IsUnique();
            entity.Property(x => x.NumeroAlojamiento).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Descripcion).HasMaxLength(500);
            entity.HasOne(x => x.Sede)
                .WithMany(x => x.Alojamientos)
                .HasForeignKey(x => x.SedeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TipoAlojamiento)
                .WithMany(x => x.Alojamientos)
                .HasForeignKey(x => x.TipoAlojamientoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Habitacion>(entity =>
        {
            entity.ToTable("Habitacion");
            entity.HasIndex(x => new { x.AlojamientoId, x.Nombre }).IsUnique();
            entity.Property(x => x.Nombre).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Observaciones).HasMaxLength(250);
            entity.HasOne(x => x.Alojamiento)
                .WithMany(x => x.Habitaciones)
                .HasForeignKey(x => x.AlojamientoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Temporada>(entity =>
        {
            entity.ToTable("Temporada");
            entity.Property(x => x.Nombre).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Descripcion).HasMaxLength(300);
            entity.Property(x => x.Prioridad).HasDefaultValue(99);
        });

        builder.Entity<Tarifa>(entity =>
        {
            entity.ToTable("Tarifa");
            entity.HasIndex(x => new { x.SedeId, x.TemporadaId, x.NumeroHabitaciones, x.PersonasBase }).IsUnique();
            entity.Property(x => x.ValorNoche).HasColumnType("decimal(12,2)");
            entity.Property(x => x.ValorPersonaAdicional).HasColumnType("decimal(12,2)");
            entity.HasOne(x => x.Sede)
                .WithMany(x => x.Tarifas)
                .HasForeignKey(x => x.SedeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Temporada)
                .WithMany(x => x.Tarifas)
                .HasForeignKey(x => x.TemporadaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Reserva>(entity =>
        {
            entity.ToTable("Reserva", table =>
            {
                table.HasCheckConstraint("CK_Reserva_Fechas", "[FechaSalida] > [FechaLlegada]");
                table.HasCheckConstraint("CK_Reserva_Personas", "[NroPersonas] >= 1");
                table.HasCheckConstraint("CK_Reserva_NumHabitaciones", "[NroHabitaciones] >= 1");
                table.HasCheckConstraint("CK_Reserva_Acompanantes", "[NroAcompanantes] BETWEEN 0 AND 10");
                table.HasCheckConstraint("CK_Reserva_Estado", "[Estado] IN ('Pendiente','Confirmada','Cancelada','Completada')");
            });

            entity.Property(x => x.FechaReserva).HasDefaultValueSql("GETDATE()");
            entity.Property(x => x.FechaLlegada).HasColumnType("date");
            entity.Property(x => x.FechaSalida).HasColumnType("date");
            entity.Property(x => x.ValorTotal).HasColumnType("decimal(12,2)");
            entity.Property(x => x.Estado).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Observaciones).HasMaxLength(500);
            entity.HasOne(x => x.Sede)
                .WithMany(x => x.Reservas)
                .HasForeignKey(x => x.SedeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<DetalleReserva>(entity =>
        {
            entity.ToTable("DetalleReserva");
            entity.HasIndex(x => x.ReservaId);
            entity.HasIndex(x => new { x.ReservaId, x.AlojamientoId }).IsUnique();
            entity.Property(x => x.ValorNoche).HasColumnType("decimal(12,2)");
            entity.Property(x => x.SubTotal).HasColumnType("decimal(12,2)");
            entity.HasOne(x => x.Reserva)
                .WithMany(x => x.Detalles)
                .HasForeignKey(x => x.ReservaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Alojamiento)
                .WithMany(x => x.DetallesReserva)
                .HasForeignKey(x => x.AlojamientoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<DisponibilidadResult>(entity =>
        {
            entity.HasNoKey();
            entity.ToView(null);
            entity.Property(x => x.AlojamientoId).HasColumnName("Id");
        });

        builder.Entity<ConsultaTarifaResult>(entity =>
        {
            entity.HasNoKey();
            entity.ToView(null);
            entity.Property(x => x.ValorNoche).HasColumnType("decimal(12,2)");
            entity.Property(x => x.ValorPersonaAdicional).HasColumnType("decimal(12,2)");
            entity.Property(x => x.ValorAdicional).HasColumnType("decimal(12,2)");
        });

        builder.Entity<CalculoReservaResult>(entity =>
        {
            entity.HasNoKey();
            entity.ToView(null);
            entity.Property(x => x.ValorNoche).HasColumnType("decimal(12,2)");
            entity.Property(x => x.ValorPersonaAdicional).HasColumnType("decimal(12,2)");
            entity.Property(x => x.SubtotalNoches).HasColumnType("decimal(12,2)");
            entity.Property(x => x.ValorAdicionales).HasColumnType("decimal(12,2)");
            entity.Property(x => x.ValorAcompanantes).HasColumnType("decimal(12,2)");
            entity.Property(x => x.ValorLavanderia).HasColumnType("decimal(12,2)");
            entity.Property(x => x.TotalServicios).HasColumnType("decimal(12,2)");
            entity.Property(x => x.TotalReserva).HasColumnType("decimal(12,2)");
        });
    }
}
