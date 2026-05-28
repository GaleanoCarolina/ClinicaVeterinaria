using System;
using System.Collections.Generic;
using ClinicaVeterinaria.Data;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Utils;
using MySql.Data.MySqlClient;

namespace ClinicaVeterinaria.Services;

public sealed class ReporteService
{
    public List<ReporteDefinicionModel> ListarReportesDisponibles()
    {
        ExigirAccesoReportes();
        List<ReporteDefinicionModel> todos = new()
        {
            new() { Grupo = "Agenda", Nombre = TiposReporte.CitasPorFecha },
            new() { Grupo = "Agenda", Nombre = TiposReporte.CitasPorVeterinario },
            new() { Grupo = "Clínica", Nombre = TiposReporte.ConsultasRealizadas },
            new() { Grupo = "Caja", Nombre = TiposReporte.IngresosPorDia, DisponibleCaja = true },
            new() { Grupo = "Caja", Nombre = TiposReporte.FacturasPendientes, DisponibleCaja = true },
            new() { Grupo = "Caja", Nombre = TiposReporte.PagosPorMetodo, DisponibleCaja = true },
            new() { Grupo = "Clínica", Nombre = TiposReporte.ServiciosMasUtilizados },
            new() { Grupo = "Seguimiento", Nombre = TiposReporte.VacunasProximas },
            new() { Grupo = "Inventario", Nombre = TiposReporte.StockBajo },
            new() { Grupo = "Inventario", Nombre = TiposReporte.ProductosProximosVencer },
            new() { Grupo = "Clínica", Nombre = TiposReporte.Hospitalizaciones }
        };
        if (SesionActual.EsRol("Caja")) return todos.FindAll(x => x.DisponibleCaja);
        return todos;
    }

    public ReporteResumenModel ObtenerResumen(DateTime desde, DateTime hasta)
    {
        ExigirAccesoReportes();
        ValidarPeriodo(desde, hasta);
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        ReporteResumenModel resumen = new();
        if (!SesionActual.EsRol("Caja"))
        {
            resumen.Citas = EjecutarEntero(conexion, "SELECT COUNT(*) FROM citas WHERE fecha_hora_inicio >= @desde AND fecha_hora_inicio < DATE_ADD(@hasta, INTERVAL 1 DAY);", desde, hasta);
            resumen.Consultas = EjecutarEntero(conexion, "SELECT COUNT(*) FROM consultas WHERE fecha_atencion >= @desde AND fecha_atencion < DATE_ADD(@hasta, INTERVAL 1 DAY);", desde, hasta);
            using MySqlCommand stock = new(@"
SELECT COUNT(*) FROM (
 SELECT p.id_producto
 FROM inventario_productos p LEFT JOIN inventario_lotes l ON l.id_producto=p.id_producto
 WHERE p.activo=1
 GROUP BY p.id_producto, p.stock_minimo
 HAVING COALESCE(SUM(CASE WHEN l.estado <> 'Vencido' THEN l.cantidad_disponible ELSE 0 END),0) <= p.stock_minimo
) bajos;", conexion);
            resumen.StockBajo = Convert.ToInt32(stock.ExecuteScalar());
        }
        resumen.Cobrado = EjecutarDecimal(conexion, "SELECT COALESCE(SUM(monto),0) FROM pagos WHERE estado='Aplicado' AND fecha_pago >= @desde AND fecha_pago < DATE_ADD(@hasta, INTERVAL 1 DAY);", desde, hasta);
        resumen.SaldosPendientes = EjecutarDecimal(conexion, "SELECT COALESCE(SUM(saldo_pendiente),0) FROM facturas WHERE estado <> 'Anulada' AND fecha_emision >= @desde AND fecha_emision < DATE_ADD(@hasta, INTERVAL 1 DAY);", desde, hasta);
        return resumen;
    }

    public ReporteResultadoModel Generar(ReporteFiltroModel filtro)
    {
        ExigirReportePermitido(filtro.TipoReporte);
        ValidarPeriodo(filtro.Desde, filtro.Hasta);
        return filtro.TipoReporte switch
        {
            TiposReporte.CitasPorFecha => CitasPorFecha(filtro),
            TiposReporte.CitasPorVeterinario => CitasPorVeterinario(filtro),
            TiposReporte.ConsultasRealizadas => ConsultasRealizadas(filtro),
            TiposReporte.IngresosPorDia => IngresosPorDia(filtro),
            TiposReporte.FacturasPendientes => FacturasPendientes(filtro),
            TiposReporte.PagosPorMetodo => PagosPorMetodo(filtro),
            TiposReporte.ServiciosMasUtilizados => ServiciosMasUtilizados(filtro),
            TiposReporte.VacunasProximas => VacunasProximas(filtro),
            TiposReporte.StockBajo => StockBajo(filtro),
            TiposReporte.ProductosProximosVencer => ProductosProximosVencer(filtro),
            TiposReporte.Hospitalizaciones => Hospitalizaciones(filtro),
            _ => throw new InvalidOperationException("Seleccione un reporte válido.")
        };
    }

    private static ReporteResultadoModel CitasPorFecha(ReporteFiltroModel filtro)
    {
        ReporteResultadoModel r = Nuevo(filtro, TiposReporte.CitasPorFecha, new[] { "Fecha", "Hora", "Paciente", "Dueño", "Veterinario", "Servicio", "Estado" });
        using MySqlConnection c = Abrir();
        using MySqlCommand cmd = new(@"
SELECT DATE_FORMAT(ci.fecha_hora_inicio,'%d/%m/%Y') fecha, DATE_FORMAT(ci.fecha_hora_inicio,'%H:%i') hora,
 m.nombre mascota, d.nombre_completo dueno, v.nombre_completo veterinario, s.nombre servicio, ci.estado
FROM citas ci INNER JOIN mascotas m ON m.id_mascota=ci.id_mascota INNER JOIN duenos d ON d.id_dueno=m.id_dueno
INNER JOIN veterinarios v ON v.id_veterinario=ci.id_veterinario INNER JOIN catalogo_servicios s ON s.id_servicio=ci.id_servicio
WHERE ci.fecha_hora_inicio >= @desde AND ci.fecha_hora_inicio < DATE_ADD(@hasta, INTERVAL 1 DAY)
AND (@filtro='' OR m.nombre LIKE @buscar OR d.nombre_completo LIKE @buscar OR v.nombre_completo LIKE @buscar OR s.nombre LIKE @buscar)
ORDER BY ci.fecha_hora_inicio, v.nombre_completo;", c);
        ParametrosFiltro(cmd, filtro);
        Leer(cmd, r, "fecha", "hora", "mascota", "dueno", "veterinario", "servicio", "estado");
        r.IndicadorValor = r.Filas.Count.ToString();
        return r;
    }

    private static ReporteResultadoModel CitasPorVeterinario(ReporteFiltroModel filtro)
    {
        ReporteResultadoModel r = Nuevo(filtro, TiposReporte.CitasPorVeterinario, new[] { "Veterinario", "Total", "Atendidas", "Canceladas", "No asistió", "Pendientes" });
        using MySqlConnection c = Abrir();
        using MySqlCommand cmd = new(@"
SELECT v.nombre_completo veterinario, COUNT(*) total,
 SUM(ci.estado='Atendida') atendidas, SUM(ci.estado='Cancelada') canceladas,
 SUM(ci.estado='No asistió') no_asistio,
 SUM(ci.estado IN ('Pendiente','Confirmada','Llegó','En consulta','Reagendada')) pendientes
FROM citas ci INNER JOIN veterinarios v ON v.id_veterinario=ci.id_veterinario
WHERE ci.fecha_hora_inicio >= @desde AND ci.fecha_hora_inicio < DATE_ADD(@hasta, INTERVAL 1 DAY)
AND (@filtro='' OR v.nombre_completo LIKE @buscar OR v.codigo_veterinario LIKE @buscar)
GROUP BY v.id_veterinario, v.nombre_completo ORDER BY total DESC, v.nombre_completo;", c);
        ParametrosFiltro(cmd, filtro);
        Leer(cmd, r, "veterinario", "total", "atendidas", "canceladas", "no_asistio", "pendientes");
        r.IndicadorNombre = "Veterinarios"; r.IndicadorValor = r.Filas.Count.ToString();
        return r;
    }

    private static ReporteResultadoModel ConsultasRealizadas(ReporteFiltroModel filtro)
    {
        ReporteResultadoModel r = Nuevo(filtro, TiposReporte.ConsultasRealizadas, new[] { "Fecha", "Paciente", "Dueño", "Veterinario", "Diagnóstico principal", "Egreso" });
        using MySqlConnection c = Abrir();
        using MySqlCommand cmd = new(@"
SELECT DATE_FORMAT(co.fecha_atencion,'%d/%m/%Y %H:%i') fecha, m.nombre mascota, d.nombre_completo dueno,
 v.nombre_completo veterinario, COALESCE((SELECT cd.descripcion FROM consulta_diagnosticos cd WHERE cd.id_consulta=co.id_consulta AND cd.es_principal=1 LIMIT 1),'') diagnostico,
 COALESCE(co.estado_egreso,'') egreso
FROM consultas co INNER JOIN mascotas m ON m.id_mascota=co.id_mascota INNER JOIN duenos d ON d.id_dueno=m.id_dueno INNER JOIN veterinarios v ON v.id_veterinario=co.id_veterinario
WHERE co.fecha_atencion >= @desde AND co.fecha_atencion < DATE_ADD(@hasta, INTERVAL 1 DAY)
AND (@filtro='' OR m.nombre LIKE @buscar OR d.nombre_completo LIKE @buscar OR v.nombre_completo LIKE @buscar)
ORDER BY co.fecha_atencion DESC;", c);
        ParametrosFiltro(cmd, filtro);
        Leer(cmd, r, "fecha", "mascota", "dueno", "veterinario", "diagnostico", "egreso");
        r.IndicadorValor = r.Filas.Count.ToString();
        return r;
    }

    private static ReporteResultadoModel IngresosPorDia(ReporteFiltroModel filtro)
    {
        ReporteResultadoModel r = Nuevo(filtro, TiposReporte.IngresosPorDia, new[] { "Fecha", "Pagos aplicados", "Total cobrado" });
        using MySqlConnection c = Abrir();
        using MySqlCommand cmd = new(@"
SELECT DATE_FORMAT(DATE(fecha_pago),'%d/%m/%Y') fecha, COUNT(*) pagos, COALESCE(SUM(monto),0) total
FROM pagos WHERE estado='Aplicado' AND fecha_pago >= @desde AND fecha_pago < DATE_ADD(@hasta, INTERVAL 1 DAY)
GROUP BY DATE(fecha_pago) ORDER BY DATE(fecha_pago);", c);
        ParametrosFecha(cmd, filtro);
        decimal total = 0;
        using MySqlDataReader dr = cmd.ExecuteReader();
        while (dr.Read()) { decimal monto = dr.GetDecimal("total"); total += monto; r.Filas.Add(new() { dr.GetString("fecha"), dr.GetInt32("pagos").ToString(), monto.ToString("C2") }); }
        r.IndicadorNombre = "Total cobrado"; r.IndicadorValor = total.ToString("C2");
        return r;
    }

    private static ReporteResultadoModel FacturasPendientes(ReporteFiltroModel filtro)
    {
        ReporteResultadoModel r = Nuevo(filtro, TiposReporte.FacturasPendientes, new[] { "Factura", "Fecha", "Dueño", "Paciente", "Total", "Pagado", "Saldo", "Estado" });
        using MySqlConnection c = Abrir();
        using MySqlCommand cmd = new(@"
SELECT f.numero_factura, DATE_FORMAT(f.fecha_emision,'%d/%m/%Y') fecha, d.nombre_completo dueno, COALESCE(m.nombre,'-') mascota,
 f.total, f.total_pagado, f.saldo_pendiente, f.estado
FROM facturas f INNER JOIN duenos d ON d.id_dueno=f.id_dueno LEFT JOIN mascotas m ON m.id_mascota=f.id_mascota
WHERE f.estado IN ('Emitida','Parcialmente pagada') AND f.saldo_pendiente > 0
AND f.fecha_emision >= @desde AND f.fecha_emision < DATE_ADD(@hasta, INTERVAL 1 DAY)
AND (@filtro='' OR f.numero_factura LIKE @buscar OR d.nombre_completo LIKE @buscar OR m.nombre LIKE @buscar)
ORDER BY f.fecha_emision;", c);
        ParametrosFiltro(cmd, filtro);
        decimal saldo = 0;
        using MySqlDataReader dr = cmd.ExecuteReader();
        while (dr.Read()) { saldo += dr.GetDecimal("saldo_pendiente"); r.Filas.Add(new() { dr.GetString("numero_factura"), dr.GetString("fecha"), dr.GetString("dueno"), dr.GetString("mascota"), dr.GetDecimal("total").ToString("C2"), dr.GetDecimal("total_pagado").ToString("C2"), dr.GetDecimal("saldo_pendiente").ToString("C2"), dr.GetString("estado") }); }
        r.IndicadorNombre = "Saldo pendiente"; r.IndicadorValor = saldo.ToString("C2");
        return r;
    }

    private static ReporteResultadoModel PagosPorMetodo(ReporteFiltroModel filtro)
    {
        ReporteResultadoModel r = Nuevo(filtro, TiposReporte.PagosPorMetodo, new[] { "Método de pago", "Pagos", "Total cobrado" });
        using MySqlConnection c = Abrir();
        using MySqlCommand cmd = new(@"
SELECT mp.nombre metodo, COUNT(*) pagos, COALESCE(SUM(p.monto),0) total
FROM pagos p INNER JOIN metodos_pago mp ON mp.id_metodo_pago=p.id_metodo_pago
WHERE p.estado='Aplicado' AND p.fecha_pago >= @desde AND p.fecha_pago < DATE_ADD(@hasta, INTERVAL 1 DAY)
AND (@filtro='' OR mp.nombre LIKE @buscar)
GROUP BY mp.id_metodo_pago, mp.nombre ORDER BY total DESC;", c);
        ParametrosFiltro(cmd, filtro);
        decimal total = 0;
        using MySqlDataReader dr = cmd.ExecuteReader();
        while (dr.Read()) { decimal monto = dr.GetDecimal("total"); total += monto; r.Filas.Add(new() { dr.GetString("metodo"), dr.GetInt32("pagos").ToString(), monto.ToString("C2") }); }
        r.IndicadorNombre = "Cobrado"; r.IndicadorValor = total.ToString("C2");
        return r;
    }

    private static ReporteResultadoModel ServiciosMasUtilizados(ReporteFiltroModel filtro)
    {
        ReporteResultadoModel r = Nuevo(filtro, TiposReporte.ServiciosMasUtilizados, new[] { "Servicio", "Veces realizado", "Unidades", "Importe" });
        using MySqlConnection c = Abrir();
        using MySqlCommand cmd = new(@"
SELECT s.nombre servicio, COUNT(*) veces, SUM(cs.cantidad) unidades, SUM(cs.subtotal) importe
FROM consulta_servicios cs INNER JOIN catalogo_servicios s ON s.id_servicio=cs.id_servicio INNER JOIN consultas co ON co.id_consulta=cs.id_consulta
WHERE co.fecha_atencion >= @desde AND co.fecha_atencion < DATE_ADD(@hasta, INTERVAL 1 DAY)
AND (@filtro='' OR s.nombre LIKE @buscar)
GROUP BY s.id_servicio, s.nombre ORDER BY unidades DESC, importe DESC;", c);
        ParametrosFiltro(cmd, filtro);
        using MySqlDataReader dr = cmd.ExecuteReader();
        while (dr.Read()) r.Filas.Add(new() { dr.GetString("servicio"), dr.GetInt32("veces").ToString(), dr.GetDecimal("unidades").ToString("0.##"), dr.GetDecimal("importe").ToString("C2") });
        r.IndicadorNombre = "Servicios"; r.IndicadorValor = r.Filas.Count.ToString();
        return r;
    }

    private static ReporteResultadoModel VacunasProximas(ReporteFiltroModel filtro)
    {
        ReporteResultadoModel r = Nuevo(filtro, TiposReporte.VacunasProximas, new[] { "Próxima dosis", "Paciente", "Dueño", "Teléfono", "Vacuna", "Última aplicación" });
        using MySqlConnection c = Abrir();
        using MySqlCommand cmd = new(@"
SELECT DATE_FORMAT(va.fecha_proxima_dosis,'%d/%m/%Y') proxima, m.nombre mascota, d.nombre_completo dueno, d.telefono_principal telefono,
 cv.nombre vacuna, DATE_FORMAT(va.fecha_aplicacion,'%d/%m/%Y') aplicacion
FROM vacunas_aplicadas va INNER JOIN mascotas m ON m.id_mascota=va.id_mascota INNER JOIN duenos d ON d.id_dueno=m.id_dueno INNER JOIN catalogo_vacunas cv ON cv.id_vacuna=va.id_vacuna
WHERE va.fecha_proxima_dosis BETWEEN @desde AND @hasta
AND (@filtro='' OR m.nombre LIKE @buscar OR d.nombre_completo LIKE @buscar OR cv.nombre LIKE @buscar)
ORDER BY va.fecha_proxima_dosis;", c);
        ParametrosFiltro(cmd, filtro);
        Leer(cmd, r, "proxima", "mascota", "dueno", "telefono", "vacuna", "aplicacion");
        r.IndicadorValor = r.Filas.Count.ToString();
        return r;
    }

    private static ReporteResultadoModel StockBajo(ReporteFiltroModel filtro)
    {
        ReporteResultadoModel r = Nuevo(filtro, TiposReporte.StockBajo, new[] { "Código", "Producto", "Categoría", "Stock", "Mínimo", "Diferencia" });
        using MySqlConnection c = Abrir();
        using MySqlCommand cmd = new(@"
SELECT p.codigo, p.nombre producto, p.categoria, COALESCE(SUM(CASE WHEN l.estado <> 'Vencido' THEN l.cantidad_disponible ELSE 0 END),0) stock, p.stock_minimo minimo,
 p.stock_minimo - COALESCE(SUM(CASE WHEN l.estado <> 'Vencido' THEN l.cantidad_disponible ELSE 0 END),0) diferencia
FROM inventario_productos p LEFT JOIN inventario_lotes l ON l.id_producto=p.id_producto
WHERE p.activo=1 AND (@filtro='' OR p.codigo LIKE @buscar OR p.nombre LIKE @buscar OR p.categoria LIKE @buscar)
GROUP BY p.id_producto,p.codigo,p.nombre,p.categoria,p.stock_minimo
HAVING stock <= minimo ORDER BY diferencia DESC, p.nombre;", c);
        cmd.Parameters.AddWithValue("@filtro", filtro.Buscar.Trim()); cmd.Parameters.AddWithValue("@buscar", $"%{filtro.Buscar.Trim()}%");
        using MySqlDataReader dr = cmd.ExecuteReader();
        while (dr.Read()) r.Filas.Add(new() { dr.GetString("codigo"), dr.GetString("producto"), dr.GetString("categoria"), dr.GetDecimal("stock").ToString("0.###"), dr.GetDecimal("minimo").ToString("0.###"), dr.GetDecimal("diferencia").ToString("0.###") });
        r.IndicadorNombre = "Productos"; r.IndicadorValor = r.Filas.Count.ToString();
        return r;
    }

    private static ReporteResultadoModel ProductosProximosVencer(ReporteFiltroModel filtro)
    {
        ReporteResultadoModel r = Nuevo(filtro, TiposReporte.ProductosProximosVencer, new[] { "Producto", "Lote", "Vencimiento", "Existencia", "Estado" });
        using MySqlConnection c = Abrir();
        using MySqlCommand cmd = new(@"
SELECT p.nombre producto, l.numero_lote lote, DATE_FORMAT(l.fecha_vencimiento,'%d/%m/%Y') vencimiento, l.cantidad_disponible existencia,
 CASE WHEN l.fecha_vencimiento < CURDATE() THEN 'Vencido' ELSE 'Próximo a vencer' END situacion
FROM inventario_lotes l INNER JOIN inventario_productos p ON p.id_producto=l.id_producto
WHERE l.cantidad_disponible > 0 AND l.fecha_vencimiento IS NOT NULL AND l.fecha_vencimiento BETWEEN @desde AND @hasta
AND (@filtro='' OR p.nombre LIKE @buscar OR l.numero_lote LIKE @buscar)
ORDER BY l.fecha_vencimiento;", c);
        ParametrosFiltro(cmd, filtro);
        using MySqlDataReader dr = cmd.ExecuteReader();
        while (dr.Read()) r.Filas.Add(new() { dr.GetString("producto"), dr.GetString("lote"), dr.GetString("vencimiento"), dr.GetDecimal("existencia").ToString("0.###"), dr.GetString("situacion") });
        r.IndicadorNombre = "Lotes"; r.IndicadorValor = r.Filas.Count.ToString();
        return r;
    }

    private static ReporteResultadoModel Hospitalizaciones(ReporteFiltroModel filtro)
    {
        ReporteResultadoModel r = Nuevo(filtro, TiposReporte.Hospitalizaciones, new[] { "Ingreso", "Alta", "Paciente", "Dueño", "Veterinario", "Espacio", "Estado" });
        using MySqlConnection c = Abrir();
        using MySqlCommand cmd = new(@"
SELECT DATE_FORMAT(h.fecha_hora_ingreso,'%d/%m/%Y %H:%i') ingreso,
 COALESCE(DATE_FORMAT(h.fecha_hora_alta,'%d/%m/%Y %H:%i'),'-') alta, m.nombre mascota, d.nombre_completo dueno,
 v.nombre_completo veterinario, COALESCE(h.espacio_asignado,'-') espacio, h.estado
FROM hospitalizaciones h INNER JOIN mascotas m ON m.id_mascota=h.id_mascota INNER JOIN duenos d ON d.id_dueno=m.id_dueno INNER JOIN veterinarios v ON v.id_veterinario=h.id_veterinario
WHERE h.fecha_hora_ingreso >= @desde AND h.fecha_hora_ingreso < DATE_ADD(@hasta, INTERVAL 1 DAY)
AND (@filtro='' OR m.nombre LIKE @buscar OR d.nombre_completo LIKE @buscar OR v.nombre_completo LIKE @buscar OR h.espacio_asignado LIKE @buscar)
ORDER BY h.fecha_hora_ingreso DESC;", c);
        ParametrosFiltro(cmd, filtro);
        Leer(cmd, r, "ingreso", "alta", "mascota", "dueno", "veterinario", "espacio", "estado");
        r.IndicadorNombre = "Ingresos"; r.IndicadorValor = r.Filas.Count.ToString();
        return r;
    }

    private static MySqlConnection Abrir()
    {
        MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        return conexion;
    }

    private static ReporteResultadoModel Nuevo(ReporteFiltroModel filtro, string titulo, IEnumerable<string> columnas)
    {
        return new ReporteResultadoModel
        {
            Titulo = titulo,
            Desde = filtro.Desde.Date,
            Hasta = filtro.Hasta.Date,
            DescripcionPeriodo = $"Periodo: {filtro.Desde:dd/MM/yyyy} al {filtro.Hasta:dd/MM/yyyy}",
            FiltroAplicado = filtro.Buscar.Trim(),
            Columnas = new List<string>(columnas)
        };
    }

    private static void ParametrosFecha(MySqlCommand cmd, ReporteFiltroModel filtro)
    {
        cmd.Parameters.AddWithValue("@desde", filtro.Desde.Date);
        cmd.Parameters.AddWithValue("@hasta", filtro.Hasta.Date);
    }

    private static void ParametrosFiltro(MySqlCommand cmd, ReporteFiltroModel filtro)
    {
        ParametrosFecha(cmd, filtro);
        cmd.Parameters.AddWithValue("@filtro", filtro.Buscar.Trim());
        cmd.Parameters.AddWithValue("@buscar", $"%{filtro.Buscar.Trim()}%");
    }

    private static void Leer(MySqlCommand cmd, ReporteResultadoModel resultado, params string[] columnas)
    {
        using MySqlDataReader dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            List<string> fila = new();
            foreach (string columna in columnas) fila.Add(dr.IsDBNull(dr.GetOrdinal(columna)) ? string.Empty : Convert.ToString(dr[columna]) ?? string.Empty);
            resultado.Filas.Add(fila);
        }
    }

    private static int EjecutarEntero(MySqlConnection conexion, string sql, DateTime desde, DateTime hasta)
    {
        using MySqlCommand cmd = new(sql, conexion);
        cmd.Parameters.AddWithValue("@desde", desde.Date); cmd.Parameters.AddWithValue("@hasta", hasta.Date);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static decimal EjecutarDecimal(MySqlConnection conexion, string sql, DateTime desde, DateTime hasta)
    {
        using MySqlCommand cmd = new(sql, conexion);
        cmd.Parameters.AddWithValue("@desde", desde.Date); cmd.Parameters.AddWithValue("@hasta", hasta.Date);
        return Convert.ToDecimal(cmd.ExecuteScalar());
    }

    private static void ValidarPeriodo(DateTime desde, DateTime hasta)
    {
        if (desde.Date > hasta.Date) throw new InvalidOperationException("La fecha inicial no puede ser posterior a la fecha final.");
        if ((hasta.Date - desde.Date).TotalDays > 730) throw new InvalidOperationException("El periodo máximo permitido para un reporte es de dos años.");
    }

    private static void ExigirAccesoReportes() => SesionActual.ExigirRoles("Administrador", "Caja");

    private static void ExigirReportePermitido(string tipoReporte)
    {
        ExigirAccesoReportes();
        if (SesionActual.EsRol("Caja") && tipoReporte != TiposReporte.IngresosPorDia && tipoReporte != TiposReporte.FacturasPendientes && tipoReporte != TiposReporte.PagosPorMetodo)
            throw new UnauthorizedAccessException("El usuario de Caja solo puede consultar reportes financieros.");
    }
}
