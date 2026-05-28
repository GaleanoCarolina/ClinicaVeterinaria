using System;

namespace ClinicaVeterinaria.Models;

public sealed class ProductoInventarioModel
{
    public long IdProducto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = "Medicamento";
    public string Presentacion { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = "Unidad";
    public decimal PrecioCompra { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal StockMinimo { get; set; }
    public bool ControlaLotes { get; set; } = true;
    public bool Activo { get; set; } = true;
    public decimal StockDisponible { get; set; }
    public DateTime? ProximoVencimiento { get; set; }
    public bool StockBajo => StockDisponible <= StockMinimo;
    public string EstadoStock => StockBajo ? "Stock bajo" : "Normal";
    public override string ToString() => $"{Codigo} - {Nombre}";
}

public sealed class LoteInventarioModel
{
    public long IdLote { get; set; }
    public long IdProducto { get; set; }
    public string CodigoProducto { get; set; } = string.Empty;
    public string Producto { get; set; } = string.Empty;
    public string NumeroLote { get; set; } = string.Empty;
    public DateTime? FechaVencimiento { get; set; }
    public decimal CantidadInicial { get; set; }
    public decimal CantidadDisponible { get; set; }
    public decimal CostoUnitario { get; set; }
    public DateTime FechaIngreso { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public string Estado { get; set; } = "Disponible";
    public bool Vencido => FechaVencimiento.HasValue && FechaVencimiento.Value.Date < DateTime.Today;
    public override string ToString() => $"{NumeroLote} | Stock: {CantidadDisponible:0.###}";
}

public sealed class MovimientoInventarioModel
{
    public long IdMovimiento { get; set; }
    public DateTime FechaRegistro { get; set; }
    public long IdProducto { get; set; }
    public string CodigoProducto { get; set; } = string.Empty;
    public string Producto { get; set; } = string.Empty;
    public long? IdLote { get; set; }
    public string Lote { get; set; } = string.Empty;
    public string TipoMovimiento { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
}

public sealed class ResumenInventarioModel
{
    public int ProductosActivos { get; set; }
    public int ProductosStockBajo { get; set; }
    public int LotesPorVencer { get; set; }
    public int LotesVencidosConStock { get; set; }
    public decimal ValorStock { get; set; }
}

public sealed class EntradaInventarioRequestModel
{
    public long IdProducto { get; set; }
    public string NumeroLote { get; set; } = string.Empty;
    public DateTime? FechaVencimiento { get; set; }
    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
}

public sealed class MovimientoManualRequestModel
{
    public long IdProducto { get; set; }
    public long IdLote { get; set; }
    public string TipoMovimiento { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string Observaciones { get; set; } = string.Empty;
}
