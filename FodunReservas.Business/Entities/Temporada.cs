namespace FodunReservas.Business.Entities;

public class Temporada
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? MesInicio { get; set; }
    public int? DiaInicio { get; set; }
    public int? MesFin { get; set; }
    public int? DiaFin { get; set; }
    public bool EsEspecial { get; set; }
    public int Prioridad { get; set; }
    public bool Activa { get; set; } = true;
    public ICollection<Tarifa> Tarifas { get; set; } = new List<Tarifa>();
}
