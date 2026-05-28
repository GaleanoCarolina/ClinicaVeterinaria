using System;

namespace ClinicaVeterinaria.Models;

public sealed class CitaModel
{
    public long IdCita { get; set; }
    public long IdMascota { get; set; }
    public string CodigoPaciente { get; set; } = string.Empty;
    public string Mascota { get; set; } = string.Empty;
    public string Dueno { get; set; } = string.Empty;
    public string TelefonoDueno { get; set; } = string.Empty;
    public int IdVeterinario { get; set; }
    public string Veterinario { get; set; } = string.Empty;
    public int IdServicio { get; set; }
    public string Servicio { get; set; } = string.Empty;
    public DateTime FechaHoraInicio { get; set; }
    public int DuracionMinutos { get; set; }
    public DateTime FechaHoraFin { get; set; }
    public string MotivoConsulta { get; set; } = string.Empty;
    public string ObservacionesRecepcion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime? FechaLlegada { get; set; }
    public int AlertasActivas { get; set; }
    public decimal SaldoPendiente { get; set; }
    public string Hora => FechaHoraInicio.ToString("HH:mm");
}
