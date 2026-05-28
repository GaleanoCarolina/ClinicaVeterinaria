using System;

namespace ClinicaVeterinaria.Models;

public sealed class VacunaMascotaResumenModel
{
    public string Vacuna { get; set; } = string.Empty;
    public DateTime FechaAplicacion { get; set; }
    public DateTime? ProximaDosis { get; set; }
    public string? Lote { get; set; }
    public string Veterinario { get; set; } = string.Empty;
}
