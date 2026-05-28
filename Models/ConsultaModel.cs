using System;
using System.Collections.Generic;

namespace ClinicaVeterinaria.Models;

public sealed class AtencionEncabezadoModel
{
    public long IdCita { get; set; }
    public long IdMascota { get; set; }
    public int IdVeterinario { get; set; }
    public int IdServicioCita { get; set; }
    public string ServicioCita { get; set; } = string.Empty;
    public decimal PrecioServicioBase { get; set; }
    public decimal PrecioServicioCita { get; set; }
    public string TipoTarifa { get; set; } = string.Empty;
    public string DetalleTarifa { get; set; } = string.Empty;
    public string CodigoPaciente { get; set; } = string.Empty;
    public string Mascota { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Raza { get; set; } = string.Empty;
    public string Sexo { get; set; } = string.Empty;
    public DateTime? FechaNacimiento { get; set; }
    public decimal? PesoAnterior { get; set; }
    public string RutaFoto { get; set; } = string.Empty;
    public string Dueno { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Veterinario { get; set; } = string.Empty;
    public DateTime FechaHoraCita { get; set; }
    public string MotivoConsulta { get; set; } = string.Empty;
    public string Alertas { get; set; } = string.Empty;
    public int AlertasActivas { get; set; }
    public string Edad
    {
        get
        {
            if (!FechaNacimiento.HasValue) return "No registrada";
            DateTime hoy = DateTime.Today;
            int anios = hoy.Year - FechaNacimiento.Value.Year;
            if (FechaNacimiento.Value.Date > hoy.AddYears(-anios)) anios--;
            return anios <= 0 ? "Menor de 1 año" : $"{anios} año(s)";
        }
    }
}

public sealed class ConsultaModel
{
    public long IdConsulta { get; set; }
    public long IdCita { get; set; }
    public long IdMascota { get; set; }
    public int IdVeterinario { get; set; }
    public string MotivoConsulta { get; set; } = string.Empty;
    public string Anamnesis { get; set; } = string.Empty;
    public decimal? Peso { get; set; }
    public decimal? Temperatura { get; set; }
    public int? FrecuenciaCardiaca { get; set; }
    public int? FrecuenciaRespiratoria { get; set; }
    public string Hidratacion { get; set; } = string.Empty;
    public string HallazgosFisicos { get; set; } = string.Empty;
    public string Pronostico { get; set; } = string.Empty;
    public string TratamientoGeneral { get; set; } = string.Empty;
    public string Indicaciones { get; set; } = string.Empty;
    public DateTime? ProximaRevision { get; set; }
    public string EstadoEgreso { get; set; } = string.Empty;
}

public sealed class ConsultaCierreModel
{
    public ConsultaModel Consulta { get; set; } = new();
    public List<DiagnosticoModel> Diagnosticos { get; set; } = new();
    public List<ConsultaServicioModel> Servicios { get; set; } = new();
    public RecetaModel? Receta { get; set; }
    public List<VacunaAplicadaModel> Vacunas { get; set; } = new();
    public List<DesparasitacionModel> Desparasitaciones { get; set; } = new();
    public List<OrdenClinicaModel> Ordenes { get; set; } = new();
}
