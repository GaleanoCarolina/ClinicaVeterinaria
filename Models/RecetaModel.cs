using System;
using System.Collections.Generic;

namespace ClinicaVeterinaria.Models;

public sealed class MedicamentoModel
{
    public int IdMedicamento { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Presentacion { get; set; } = string.Empty;
    public string Concentracion { get; set; } = string.Empty;
    public string ViaAdministracion { get; set; } = string.Empty;
    public string IndicacionesPredeterminadas { get; set; } = string.Empty;
    public decimal PrecioVenta { get; set; }
    public bool ControlaInventario { get; set; }
    public override string ToString() => Nombre;
}

public sealed class RecetaModel
{
    public long IdReceta { get; set; }
    public long IdConsulta { get; set; }
    public DateTime FechaEmision { get; set; }
    public string IndicacionesGenerales { get; set; } = string.Empty;
    public List<RecetaDetalleModel> Detalles { get; set; } = new();
}

public sealed class RecetaDetalleModel
{
    public long IdDetalle { get; set; }
    public int? IdMedicamento { get; set; }
    public string Medicamento { get; set; } = string.Empty;
    public string MedicamentoLibre { get; set; } = string.Empty;
    public string Presentacion { get; set; } = string.Empty;
    public string Concentracion { get; set; } = string.Empty;
    public string Dosis { get; set; } = string.Empty;
    public string Frecuencia { get; set; } = string.Empty;
    public string Duracion { get; set; } = string.Empty;
    public string Cantidad { get; set; } = string.Empty;
    public string ViaAdministracion { get; set; } = string.Empty;
    public string Indicaciones { get; set; } = string.Empty;
    public string NombreMostrado => IdMedicamento.HasValue ? Medicamento : MedicamentoLibre;
}
