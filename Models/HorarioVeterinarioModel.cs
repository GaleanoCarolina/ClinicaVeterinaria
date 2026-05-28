using System;

namespace ClinicaVeterinaria.Models;

public sealed class HorarioVeterinarioModel
{
    public long IdHorario { get; set; }
    public int IdVeterinario { get; set; }
    public byte DiaSemana { get; set; }
    public string DiaNombre => DiaSemana switch
    {
        1 => "Lunes", 2 => "Martes", 3 => "Miércoles", 4 => "Jueves",
        5 => "Viernes", 6 => "Sábado", 7 => "Domingo", _ => string.Empty
    };
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
    public bool Activo { get; set; } = true;
}
