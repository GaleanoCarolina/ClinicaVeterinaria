using System;

namespace ClinicaVeterinaria.Models;

public sealed class OrdenClinicaModel
{
    public long IdOrden { get; set; }
    public long IdConsulta { get; set; }
    public long IdMascota { get; set; }
    public string CodigoPaciente { get; set; } = string.Empty;
    public string Mascota { get; set; } = string.Empty;
    public string Dueno { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Veterinario { get; set; } = string.Empty;
    public string TipoOrden { get; set; } = string.Empty;
    public string NombreEstudio { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public string Estado { get; set; } = "Solicitada";
    public decimal Precio { get; set; }
    public bool Facturado { get; set; }
    public DateTime FechaSolicitud { get; set; } = DateTime.Now;
    public DateTime? FechaResultado { get; set; }
    public string ResultadoTexto { get; set; } = string.Empty;
    public string RutaArchivo { get; set; } = string.Empty;
}

public sealed class ConsultaDisponibleOrdenModel
{
    public long IdConsulta { get; set; }
    public long IdMascota { get; set; }
    public int IdVeterinario { get; set; }
    public string CodigoPaciente { get; set; } = string.Empty;
    public string Mascota { get; set; } = string.Empty;
    public string Dueno { get; set; } = string.Empty;
    public string Veterinario { get; set; } = string.Empty;
    public DateTime FechaAtencion { get; set; }
    public string Descripcion => $"{FechaAtencion:dd/MM/yyyy} · {CodigoPaciente} - {Mascota} · {Dueno}";
}
