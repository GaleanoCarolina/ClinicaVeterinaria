using System;

namespace ClinicaVeterinaria.Models;

public sealed class PagoModel
{
    public long IdPago { get; set; }
    public long IdFactura { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public int IdMetodoPago { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public DateTime FechaPago { get; set; }
    public decimal Monto { get; set; }
    public string Referencia { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public string UsuarioRegistro { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime? FechaAnulacion { get; set; }
    public string MotivoAnulacion { get; set; } = string.Empty;
}

public sealed class RegistrarPagoRequestModel
{
    public long IdFactura { get; set; }
    public int IdMetodoPago { get; set; }
    public decimal Monto { get; set; }
    public string Referencia { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
}
