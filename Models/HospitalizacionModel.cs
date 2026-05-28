using System;

namespace ClinicaVeterinaria.Models;

public sealed class HospitalizacionModel
{
    public long IdHospitalizacion { get; set; }
    public long IdMascota { get; set; }
    public long? IdConsultaOrigen { get; set; }
    public int IdVeterinario { get; set; }
    public string CodigoPaciente { get; set; } = string.Empty;
    public string Mascota { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Dueno { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Veterinario { get; set; } = string.Empty;
    public DateTime FechaHoraIngreso { get; set; } = DateTime.Now;
    public DateTime? FechaHoraAlta { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string EspacioAsignado { get; set; } = string.Empty;
    public string Estado { get; set; } = "Ingresada";
    public string Observaciones { get; set; } = string.Empty;
    public int Evoluciones { get; set; }
    public decimal SaldoPendiente { get; set; }
}

public sealed class NuevaHospitalizacionModel
{
    public long IdMascota { get; set; }
    public long? IdConsultaOrigen { get; set; }
    public int IdVeterinario { get; set; }
    public DateTime FechaHoraIngreso { get; set; } = DateTime.Now;
    public string Motivo { get; set; } = string.Empty;
    public string EspacioAsignado { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
}

public sealed class HospitalizacionEvolucionModel
{
    public long IdEvolucion { get; set; }
    public long IdHospitalizacion { get; set; }
    public DateTime FechaHora { get; set; } = DateTime.Now;
    public int IdVeterinario { get; set; }
    public string Veterinario { get; set; } = string.Empty;
    public decimal? Temperatura { get; set; }
    public decimal? Peso { get; set; }
    public int? FrecuenciaCardiaca { get; set; }
    public int? FrecuenciaRespiratoria { get; set; }
    public string Observaciones { get; set; } = string.Empty;
    public string MedicacionAdministrada { get; set; } = string.Empty;
    public string Alimentacion { get; set; } = string.Empty;
    public string Incidencias { get; set; } = string.Empty;
}

public sealed class CargoHospitalizacionModel
{
    public string TipoItem { get; set; } = "Hospitalización";
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal Subtotal => Math.Max(0, (Cantidad * PrecioUnitario) - Descuento);
}

public sealed class HospitalizacionResumenModel
{
    public int PacientesIngresados { get; set; }
    public int EnObservacion { get; set; }
    public int AltasHoy { get; set; }
    public decimal CargosPendientes { get; set; }
}

public sealed class ConsultaOrigenHospitalizacionModel
{
    public long IdConsulta { get; set; }
    public long IdMascota { get; set; }
    public DateTime FechaAtencion { get; set; }
    public string Veterinario { get; set; } = string.Empty;
    public string MotivoConsulta { get; set; } = string.Empty;
    public string Descripcion => $"{FechaAtencion:dd/MM/yyyy} · {Veterinario} · {MotivoConsulta}";
}
