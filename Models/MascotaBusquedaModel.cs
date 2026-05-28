namespace ClinicaVeterinaria.Models;

public sealed class MascotaBusquedaModel
{
    public long IdMascota { get; set; }
    public string CodigoPaciente { get; set; } = string.Empty;
    public string NombreMascota { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Dueno { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Descripcion => $"{CodigoPaciente} - {NombreMascota} ({Especie}) · {Dueno}";
}
