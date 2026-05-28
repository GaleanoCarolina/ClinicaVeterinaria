using System;

namespace ClinicaVeterinaria.Models;

public sealed class RecordatorioModel
{
    public long IdRecordatorio { get; set; }
    public long IdMascota { get; set; }
    public string CodigoPaciente { get; set; } = string.Empty;
    public string Mascota { get; set; } = string.Empty;
    public string Dueno { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string TipoRecordatorio { get; set; } = string.Empty;
    public DateTime FechaProgramada { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime? FechaContacto { get; set; }
    public string ObservacionesContacto { get; set; } = string.Empty;
    public int DiasRestantes { get; set; }
    public bool Vencido => FechaProgramada.Date < DateTime.Today && Estado is not "Completado" and not "Cancelado";
}

public sealed class ResumenRecordatoriosModel
{
    public int PendientesHoy { get; set; }
    public int ProximosSieteDias { get; set; }
    public int Vencidos { get; set; }
    public int Contactados { get; set; }
    public int Completados { get; set; }
}

public sealed class NuevoRecordatorioModel
{
    public long IdMascota { get; set; }
    public string TipoRecordatorio { get; set; } = "Manual";
    public DateTime FechaProgramada { get; set; } = DateTime.Today;
    public string Descripcion { get; set; } = string.Empty;
}
