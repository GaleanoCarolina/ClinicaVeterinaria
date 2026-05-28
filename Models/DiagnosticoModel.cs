namespace ClinicaVeterinaria.Models;

public sealed class DiagnosticoModel
{
    public long IdDiagnostico { get; set; }
    public long IdConsulta { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public bool EsPrincipal { get; set; }
    public string Observaciones { get; set; } = string.Empty;
    public string Tipo => EsPrincipal ? "Principal" : "Secundario";
}
