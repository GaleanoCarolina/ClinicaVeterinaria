using System;
using System.Collections.Generic;

namespace ClinicaVeterinaria.Models;

public sealed class MascotaExpedienteBusquedaModel
{
    public long IdMascota { get; set; }
    public string CodigoPaciente { get; set; } = string.Empty;
    public string Mascota { get; set; } = string.Empty;
    public string Dueno { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
}

public sealed class ExpedienteEncabezadoModel
{
    public long IdMascota { get; set; }
    public long IdDueno { get; set; }
    public string CodigoPaciente { get; set; } = string.Empty;
    public string Mascota { get; set; } = string.Empty;
    public string Dueno { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Raza { get; set; } = string.Empty;
    public string Sexo { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public DateTime? FechaNacimiento { get; set; }
    public decimal? PesoActual { get; set; }
    public string EstadoVital { get; set; } = string.Empty;
    public string Microchip { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? ProximaVacuna { get; set; }
    public decimal SaldoPendiente { get; set; }
    public List<AlertaClinicaModel> AlertasActivas { get; set; } = new();

    public string EdadTexto
    {
        get
        {
            if (!FechaNacimiento.HasValue) return "No registrada";
            DateTime hoy = DateTime.Today;
            int anios = hoy.Year - FechaNacimiento.Value.Year;
            if (FechaNacimiento.Value.Date > hoy.AddYears(-anios)) anios--;
            return anios < 1 ? "Menor de 1 año" : $"{anios} año(s)";
        }
    }
}

public sealed class ExpedienteConsultaResumenModel
{
    public long IdConsulta { get; set; }
    public DateTime FechaAtencion { get; set; }
    public string Veterinario { get; set; } = string.Empty;
    public string MotivoConsulta { get; set; } = string.Empty;
    public string DiagnosticoPrincipal { get; set; } = string.Empty;
    public string EstadoEgreso { get; set; } = string.Empty;
    public decimal? Peso { get; set; }
}

public sealed class ExpedienteConsultaDetalleModel
{
    public long IdConsulta { get; set; }
    public DateTime FechaAtencion { get; set; }
    public string Veterinario { get; set; } = string.Empty;
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
    public List<DiagnosticoModel> Diagnosticos { get; set; } = new();
    public List<ConsultaServicioModel> Servicios { get; set; } = new();
}

public sealed class ExpedienteRecetaResumenModel
{
    public long IdReceta { get; set; }
    public long IdConsulta { get; set; }
    public DateTime FechaEmision { get; set; }
    public string Veterinario { get; set; } = string.Empty;
    public string DiagnosticoPrincipal { get; set; } = string.Empty;
    public int CantidadMedicamentos { get; set; }
}

public sealed class ExpedienteVacunaModel
{
    public long IdAplicacion { get; set; }
    public DateTime FechaAplicacion { get; set; }
    public string Vacuna { get; set; } = string.Empty;
    public string Dosis { get; set; } = string.Empty;
    public string Lote { get; set; } = string.Empty;
    public string Laboratorio { get; set; } = string.Empty;
    public DateTime? ProximaDosis { get; set; }
    public string Veterinario { get; set; } = string.Empty;
}

public sealed class ExpedienteDesparasitacionModel
{
    public long IdDesparasitacion { get; set; }
    public DateTime FechaAplicacion { get; set; }
    public string Producto { get; set; } = string.Empty;
    public string Dosis { get; set; } = string.Empty;
    public decimal? PesoReferencia { get; set; }
    public DateTime? ProximaAplicacion { get; set; }
    public string Veterinario { get; set; } = string.Empty;
}

public sealed class ExpedienteOrdenModel
{
    public long IdOrden { get; set; }
    public DateTime FechaSolicitud { get; set; }
    public string TipoOrden { get; set; } = string.Empty;
    public string NombreEstudio { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime? FechaResultado { get; set; }
    public string ResultadoTexto { get; set; } = string.Empty;
    public decimal Precio { get; set; }
}

public sealed class ExpedienteCitaModel
{
    public long IdCita { get; set; }
    public DateTime FechaHoraInicio { get; set; }
    public string Veterinario { get; set; } = string.Empty;
    public string Servicio { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string MotivoConsulta { get; set; } = string.Empty;
}

public sealed class ExpedienteFacturaModel
{
    public long IdFactura { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public decimal Total { get; set; }
    public decimal TotalPagado { get; set; }
    public decimal SaldoPendiente { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public sealed class ExpedienteHospitalizacionModel
{
    public long IdHospitalizacion { get; set; }
    public DateTime FechaIngreso { get; set; }
    public DateTime? FechaAlta { get; set; }
    public string Veterinario { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}

public sealed class LineaTiempoExpedienteModel
{
    public DateTime Fecha { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string ProfesionalEstado { get; set; } = string.Empty;
}
