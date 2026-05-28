namespace ClinicaVeterinaria.Models;

public sealed class ServicioModel
{
    public int IdServicio { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal PrecioBase { get; set; }
    public int DuracionMinutos { get; set; }
    public bool GeneraCargo { get; set; }
    public bool Activo { get; set; }
    public string NombreDuracion => $"{Nombre} ({DuracionMinutos} min)";
}
