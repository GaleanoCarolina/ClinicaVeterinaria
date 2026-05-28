using System;

namespace ClinicaVeterinaria.Models;

public sealed class VeterinarioModel
{
    public int IdVeterinario { get; set; }
    public string CodigoVeterinario { get; set; } = string.Empty;
    public int? IdUsuario { get; set; }
    public string UsuarioAsociado { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string NumeroProfesional { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
}
