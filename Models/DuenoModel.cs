using System;

namespace ClinicaVeterinaria.Models;

public sealed class DuenoModel
{
    public long IdDueno { get; set; }
    public string CodigoCliente { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public string TelefonoPrincipal { get; set; } = string.Empty;
    public string? TelefonoAlternativo { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaModificacion { get; set; }
    public int CantidadMascotas { get; set; }
    public decimal SaldoPendiente { get; set; }
}
