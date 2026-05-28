using System;
using System.Collections.Generic;

namespace ClinicaVeterinaria.Models;

public sealed class CargoPendienteModel
{
    public long IdCargo { get; set; }
    public long IdDueno { get; set; }
    public long? IdMascota { get; set; }
    public long? IdConsulta { get; set; }
    public long? IdCita { get; set; }
    public string CodigoPaciente { get; set; } = string.Empty;
    public string Dueno { get; set; } = string.Empty;
    public string Mascota { get; set; } = string.Empty;
    public string TipoItem { get; set; } = string.Empty;
    public long? IdReferencia { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal Subtotal { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public sealed class CrearFacturaRequestModel
{
    public List<long> IdsCargos { get; set; } = new();
    public decimal DescuentoTotal { get; set; }
    public string Observaciones { get; set; } = string.Empty;
}

public sealed class FacturaModel
{
    public long IdFactura { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public long IdDueno { get; set; }
    public long? IdMascota { get; set; }
    public long? IdCita { get; set; }
    public long? IdConsulta { get; set; }
    public string Dueno { get; set; } = string.Empty;
    public string Mascota { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DescuentoTotal { get; set; }
    public decimal ImpuestoTotal { get; set; }
    public decimal Total { get; set; }
    public decimal TotalPagado { get; set; }
    public decimal SaldoPendiente { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public string UsuarioCreacion { get; set; } = string.Empty;
    public DateTime? FechaAnulacion { get; set; }
    public string MotivoAnulacion { get; set; } = string.Empty;
    public List<FacturaDetalleModel> Detalles { get; set; } = new();
    public List<PagoModel> Pagos { get; set; } = new();
}

public sealed class FacturaDetalleModel
{
    public long IdDetalle { get; set; }
    public string TipoItem { get; set; } = string.Empty;
    public long? IdReferencia { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal Subtotal { get; set; }
}

public sealed class MetodoPagoModel
{
    public int IdMetodoPago { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public sealed class CajaResumenModel
{
    public DateTime Fecha { get; set; }
    public decimal TotalCobrado { get; set; }
    public int FacturasEmitidas { get; set; }
    public int FacturasPagadas { get; set; }
    public int FacturasParciales { get; set; }
    public int FacturasAnuladas { get; set; }
    public decimal SaldosPendientesGenerados { get; set; }
    public int PagosAplicados { get; set; }
    public List<CajaMetodoPagoModel> TotalesPorMetodo { get; set; } = new();
    public List<PagoModel> PagosDelDia { get; set; } = new();
}

public sealed class CajaMetodoPagoModel
{
    public string MetodoPago { get; set; } = string.Empty;
    public int CantidadPagos { get; set; }
    public decimal Total { get; set; }
}
