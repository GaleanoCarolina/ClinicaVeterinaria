namespace ClinicaVeterinaria.Models;

public sealed class ServicioCatalogoModel
{
    public int IdServicio { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal PrecioBase { get; set; }
    public int DuracionMinutos { get; set; } = 30;
    public bool GeneraCargo { get; set; } = true;
    public bool Activo { get; set; } = true;
}

public sealed class MedicamentoCatalogoModel
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
    public long? IdProductoInventario { get; set; }
    public string ProductoInventario { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

public sealed class VacunaCatalogoModel
{
    public int IdVacuna { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string EspecieAplicable { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int? IntervaloDiasSugerido { get; set; }
    public decimal PrecioBase { get; set; }
    public bool ControlaInventario { get; set; } = true;
    public long? IdProductoInventario { get; set; }
    public string ProductoInventario { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

public sealed class DesparasitanteCatalogoModel
{
    public int IdDesparasitante { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Presentacion { get; set; } = string.Empty;
    public string DosisSugerida { get; set; } = string.Empty;
    public int? IntervaloDiasSugerido { get; set; }
    public decimal PrecioBase { get; set; }
    public bool ControlaInventario { get; set; } = true;
    public long? IdProductoInventario { get; set; }
    public string ProductoInventario { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

public sealed class MetodoPagoCatalogoModel
{
    public int IdMetodoPago { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

public sealed class TipoBloqueoCatalogoModel
{
    public int IdTipoBloqueo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

public sealed class ProductoCatalogoVinculoModel
{
    public long IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Etiqueta => $"{Nombre} ({Categoria})";
    public override string ToString() => Etiqueta;
}
