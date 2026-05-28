using System;
using System.Collections.Generic;

namespace ClinicaVeterinaria.Models;

public static class TiposReporte
{
    public const string CitasPorFecha = "Citas por fecha";
    public const string CitasPorVeterinario = "Citas por veterinario";
    public const string ConsultasRealizadas = "Consultas realizadas";
    public const string IngresosPorDia = "Ingresos por día";
    public const string FacturasPendientes = "Facturas pendientes";
    public const string PagosPorMetodo = "Pagos por método";
    public const string ServiciosMasUtilizados = "Servicios más utilizados";
    public const string VacunasProximas = "Vacunas próximas";
    public const string StockBajo = "Productos con stock bajo";
    public const string ProductosProximosVencer = "Productos próximos a vencer";
    public const string Hospitalizaciones = "Historial de hospitalizaciones";
}

public sealed class ReporteDefinicionModel
{
    public string Nombre { get; set; } = string.Empty;
    public string Grupo { get; set; } = string.Empty;
    public bool DisponibleCaja { get; set; }
    public override string ToString() => $"{Grupo} - {Nombre}";
}

public sealed class ReporteFiltroModel
{
    public string TipoReporte { get; set; } = string.Empty;
    public DateTime Desde { get; set; } = DateTime.Today.AddDays(-30);
    public DateTime Hasta { get; set; } = DateTime.Today;
    public string Buscar { get; set; } = string.Empty;
}

public sealed class ReporteResumenModel
{
    public int Citas { get; set; }
    public int Consultas { get; set; }
    public decimal Cobrado { get; set; }
    public decimal SaldosPendientes { get; set; }
    public int StockBajo { get; set; }
}

public sealed class ReporteResultadoModel
{
    public string Titulo { get; set; } = string.Empty;
    public string DescripcionPeriodo { get; set; } = string.Empty;
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public string FiltroAplicado { get; set; } = string.Empty;
    public List<string> Columnas { get; set; } = new();
    public List<List<string>> Filas { get; set; } = new();
    public string IndicadorNombre { get; set; } = "Registros";
    public string IndicadorValor { get; set; } = "0";
}
