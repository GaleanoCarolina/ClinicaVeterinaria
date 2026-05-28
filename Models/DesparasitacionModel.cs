using System;

namespace ClinicaVeterinaria.Models;

public sealed class DesparasitanteModel
{
    public int IdDesparasitante { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Presentacion { get; set; } = string.Empty;
    public string DosisSugerida { get; set; } = string.Empty;
    public int? IntervaloDiasSugerido { get; set; }
    public decimal PrecioBase { get; set; }
    public bool ControlaInventario { get; set; }
    public long? IdProductoInventario { get; set; }
    public override string ToString() => Nombre;
}

public sealed class DesparasitacionModel
{
    public long IdDesparasitacion { get; set; }
    public int IdDesparasitante { get; set; }
    public string Desparasitante { get; set; } = string.Empty;
    public long? IdLoteInventario { get; set; }
    public string Dosis { get; set; } = string.Empty;
    public decimal? PesoReferencia { get; set; }
    public DateTime FechaAplicacion { get; set; } = DateTime.Now;
    public DateTime? FechaProxima { get; set; }
    public string Observaciones { get; set; } = string.Empty;
    public decimal PrecioAplicado { get; set; }
}
