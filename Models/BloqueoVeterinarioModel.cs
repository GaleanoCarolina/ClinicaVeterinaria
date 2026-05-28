using System;

namespace ClinicaVeterinaria.Models;

public sealed class BloqueoVeterinarioModel
{
    public long IdBloqueo { get; set; }
    public int IdVeterinario { get; set; }
    public int IdTipoBloqueo { get; set; }
    public string TipoBloqueo { get; set; } = string.Empty;
    public DateTime FechaHoraInicio { get; set; }
    public DateTime FechaHoraFin { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string UsuarioCreacion { get; set; } = string.Empty;
}
