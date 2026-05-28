using System;

namespace ClinicaVeterinaria.Models;

public sealed class AlertaClinicaModel
{
    public long IdAlerta { get; set; }
    public long IdMascota { get; set; }
    public string TipoAlerta { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public bool Activa { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string UsuarioRegistro { get; set; } = string.Empty;
    public DateTime? FechaCierre { get; set; }
    public string? UsuarioCierre { get; set; }
}
