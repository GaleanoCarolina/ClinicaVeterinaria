using System;

namespace ClinicaVeterinaria.Models;

public sealed class FacturaMascotaResumenModel
{
    public long IdFactura { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public decimal Total { get; set; }
    public decimal TotalPagado { get; set; }
    public decimal SaldoPendiente { get; set; }
    public string Estado { get; set; } = string.Empty;
}
