using System;

namespace ClinicaVeterinaria.Models;

public sealed class UsuarioModel
{
    public int IdUsuario { get; set; }
    public int IdRol { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public int? IdVeterinario { get; set; }
    public DateTime? UltimoAcceso { get; set; }
}
