namespace ClinicaVeterinaria.Models;

public sealed class ConsultaServicioModel
{
    public long IdConsultaServicio { get; set; }
    public int IdServicio { get; set; }
    public string Servicio { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; } = 1m;
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal Subtotal => decimal.Max(0, decimal.Round((Cantidad * PrecioUnitario) - Descuento, 2));
    public bool GeneraCargo { get; set; } = true;
}
