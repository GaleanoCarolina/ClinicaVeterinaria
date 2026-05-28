using System;

namespace ClinicaVeterinaria.Models;

public sealed class VacunaModel
{
    public int IdVacuna { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string EspecieAplicable { get; set; } = string.Empty;
    public int? IntervaloDiasSugerido { get; set; }
    public decimal PrecioBase { get; set; }
    public bool ControlaInventario { get; set; }
    public long? IdProductoInventario { get; set; }
    public override string ToString() => Nombre;
}

public sealed class LoteDisponibleModel
{
    public long IdLote { get; set; }
    public long IdProducto { get; set; }
    public string NumeroLote { get; set; } = string.Empty;
    public DateTime? FechaVencimiento { get; set; }
    public decimal CantidadDisponible { get; set; }
    public string Descripcion => $"{NumeroLote} | stock {CantidadDisponible:0.###} | vence {(FechaVencimiento.HasValue ? FechaVencimiento.Value.ToString("dd/MM/yyyy") : "N/A")}";
    public override string ToString() => Descripcion;
}

public sealed class VacunaAplicadaModel
{
    public long IdAplicacion { get; set; }
    public int IdVacuna { get; set; }
    public string Vacuna { get; set; } = string.Empty;
    public long? IdLoteInventario { get; set; }
    public string LoteTexto { get; set; } = string.Empty;
    public string Laboratorio { get; set; } = string.Empty;
    public DateTime? FechaVencimientoLote { get; set; }
    public string Dosis { get; set; } = string.Empty;
    public DateTime FechaAplicacion { get; set; } = DateTime.Now;
    public DateTime? FechaProximaDosis { get; set; }
    public string Observaciones { get; set; } = string.Empty;
    public decimal PrecioAplicado { get; set; }
}
