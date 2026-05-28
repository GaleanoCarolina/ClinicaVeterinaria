namespace ClinicaVeterinaria.Models;

public sealed class CatalogoSimpleModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public override string ToString() => Nombre;
}
