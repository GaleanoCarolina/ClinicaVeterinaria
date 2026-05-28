namespace ClinicaVeterinaria.Models;

public sealed class DashboardResumenModel
{
    public int CitasHoy { get; set; }
    public int PacientesEnEspera { get; set; }
    public int PacientesEnConsulta { get; set; }
    public int ConsultasTerminadasHoy { get; set; }
    public int CanceladasNoAsistidasHoy { get; set; }
    public int FacturasPendientes { get; set; }
    public decimal TotalCobradoHoy { get; set; }
    public int RecordatoriosProximos { get; set; }
    public int ProductosStockBajo { get; set; }
    public int ProductosPorVencer { get; set; }
}
