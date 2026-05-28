using System;

namespace ClinicaVeterinaria.Models;

public sealed class DisponibilidadBloqueModel
{
    public DateTime FechaHoraInicio { get; set; }
    public DateTime FechaHoraFin { get; set; }
    public bool Disponible { get; set; }
    public string MotivoNoDisponible { get; set; } = string.Empty;
    public string Etiqueta => $"{FechaHoraInicio:HH:mm} - {FechaHoraFin:HH:mm}";
    public string Visualizacion => Disponible ? $"{Etiqueta}   Disponible" : $"{Etiqueta}   No disponible: {MotivoNoDisponible}";
}
