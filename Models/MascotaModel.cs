using System;

namespace ClinicaVeterinaria.Models;

public sealed class MascotaModel
{
    public long IdMascota { get; set; }
    public string CodigoPaciente { get; set; } = string.Empty;
    public long IdDueno { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string? Raza { get; set; }
    public string Sexo { get; set; } = "Desconocido";
    public string? Color { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public decimal? PesoActual { get; set; }
    public bool Esterilizado { get; set; }
    public string? Microchip { get; set; }
    public string? RutaFoto { get; set; }
    public string EstadoVital { get; set; } = "Viva";
    public DateTime? FechaFallecimiento { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaModificacion { get; set; }
    public int AlertasActivas { get; set; }
    public DateTime? ProximaVacuna { get; set; }
    public decimal SaldoPendiente { get; set; }

    public string EdadTexto
    {
        get
        {
            if (!FechaNacimiento.HasValue)
            {
                return "No registrada";
            }

            DateTime hoy = DateTime.Today;
            DateTime nacimiento = FechaNacimiento.Value.Date;
            if (nacimiento > hoy)
            {
                return "Fecha inválida";
            }

            int anos = hoy.Year - nacimiento.Year;
            if (nacimiento > hoy.AddYears(-anos))
            {
                anos--;
            }

            if (anos >= 1)
            {
                return anos == 1 ? "1 año" : $"{anos} años";
            }

            int meses = ((hoy.Year - nacimiento.Year) * 12) + hoy.Month - nacimiento.Month;
            if (nacimiento.Day > hoy.Day)
            {
                meses--;
            }

            meses = Math.Max(meses, 0);
            return meses == 1 ? "1 mes" : $"{meses} meses";
        }
    }
}
