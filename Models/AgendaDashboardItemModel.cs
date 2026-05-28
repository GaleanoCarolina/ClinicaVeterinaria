using System;

namespace ClinicaVeterinaria.Models;

public sealed class AgendaDashboardItemModel
{
    public long IdCita { get; set; }
    public DateTime Hora { get; set; }
    public string Mascota { get; set; } = string.Empty;
    public string Dueno { get; set; } = string.Empty;
    public string Veterinario { get; set; } = string.Empty;
    public string Servicio { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public decimal SaldoPendiente { get; set; }
}
