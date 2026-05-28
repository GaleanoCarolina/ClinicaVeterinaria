using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Utils;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace ClinicaVeterinaria.Services;

public sealed class PdfService
{
    private readonly ExpedienteService _expediente = new();
    private readonly FacturacionService _facturacion = new();

    public string GenerarResumenConsulta(long idConsulta, string rutaArchivo)
    {
        ExpedienteConsultaDetalleModel consulta = _expediente.ObtenerConsultaDetalle(idConsulta);
        ExpedienteEncabezadoModel paciente = _expediente.ObtenerEncabezado(_expediente.ObtenerIdMascotaPorConsulta(idConsulta));
        using Document documento = CrearDocumento(rutaArchivo);
        PdfTheme.AgregarEncabezado(documento, "Resumen de consulta médica", $"Consulta No. {idConsulta}");
        documento.Add(PdfTheme.DatosPaciente(paciente.CodigoPaciente, paciente.Mascota, paciente.Dueno, $"{paciente.Especie} / {paciente.Raza}", paciente.Telefono));
        documento.Add(PdfTheme.TituloSeccion("Atención"));
        Table atencion = PdfTheme.Tabla(new float[] { 22, 78 });
        AgregarPar(atencion, "Fecha", consulta.FechaAtencion.ToString("dd/MM/yyyy HH:mm"));
        AgregarPar(atencion, "Veterinario", consulta.Veterinario);
        AgregarPar(atencion, "Motivo", consulta.MotivoConsulta);
        AgregarPar(atencion, "Anamnesis", consulta.Anamnesis);
        AgregarPar(atencion, "Signos vitales", $"Peso: {Formatear(consulta.Peso, "kg")} | Temperatura: {Formatear(consulta.Temperatura, "°C")} | FC: {consulta.FrecuenciaCardiaca?.ToString() ?? "-"} | FR: {consulta.FrecuenciaRespiratoria?.ToString() ?? "-"}");
        AgregarPar(atencion, "Hallazgos físicos", consulta.HallazgosFisicos);
        AgregarPar(atencion, "Pronóstico", consulta.Pronostico);
        AgregarPar(atencion, "Tratamiento", consulta.TratamientoGeneral);
        AgregarPar(atencion, "Indicaciones", consulta.Indicaciones);
        AgregarPar(atencion, "Estado al egreso", consulta.EstadoEgreso);
        AgregarPar(atencion, "Próxima revisión", consulta.ProximaRevision?.ToString("dd/MM/yyyy HH:mm") ?? "No programada");
        documento.Add(atencion);
        AgregarDiagnosticos(documento, consulta.Diagnosticos);
        AgregarServicios(documento, consulta.Servicios);
        PdfTheme.AgregarPie(documento);
        return rutaArchivo;
    }

    public string GenerarReceta(long idReceta, string rutaArchivo)
    {
        RecetaModel receta = _expediente.ObtenerReceta(idReceta);
        ExpedienteConsultaDetalleModel consulta = _expediente.ObtenerConsultaDetalle(receta.IdConsulta);
        ExpedienteEncabezadoModel paciente = _expediente.ObtenerEncabezado(_expediente.ObtenerIdMascotaPorConsulta(receta.IdConsulta));
        using Document documento = CrearDocumento(rutaArchivo);
        PdfTheme.AgregarEncabezado(documento, "Receta médica veterinaria", $"Receta No. {idReceta}");
        documento.Add(PdfTheme.DatosPaciente(paciente.CodigoPaciente, paciente.Mascota, paciente.Dueno, $"{paciente.Especie} / {paciente.Raza}", paciente.Telefono));
        Table datos = PdfTheme.Tabla(new float[] { 20, 80 });
        AgregarPar(datos, "Fecha", receta.FechaEmision.ToString("dd/MM/yyyy HH:mm"));
        AgregarPar(datos, "Veterinario", consulta.Veterinario);
        AgregarPar(datos, "Diagnóstico", consulta.Diagnosticos.FirstOrDefault(x => x.EsPrincipal)?.Descripcion ?? "No especificado");
        documento.Add(datos);
        documento.Add(PdfTheme.TituloSeccion("Medicamentos e indicaciones"));
        Table detalle = PdfTheme.Tabla(new float[] { 20, 13, 15, 14, 14, 24 });
        foreach (string titulo in new[] { "Medicamento", "Dosis", "Frecuencia", "Duración", "Vía", "Indicaciones" }) detalle.AddHeaderCell(PdfTheme.EncabezadoTabla(titulo));
        foreach (RecetaDetalleModel item in receta.Detalles)
        {
            detalle.AddCell(PdfTheme.Celda(item.NombreMostrado + TextoOpcional(item.Concentracion)));
            detalle.AddCell(PdfTheme.Celda(item.Dosis));
            detalle.AddCell(PdfTheme.Celda(item.Frecuencia));
            detalle.AddCell(PdfTheme.Celda(item.Duracion));
            detalle.AddCell(PdfTheme.Celda(item.ViaAdministracion));
            detalle.AddCell(PdfTheme.Celda(item.Indicaciones));
        }
        documento.Add(detalle);
        if (!string.IsNullOrWhiteSpace(receta.IndicacionesGenerales))
        {
            documento.Add(PdfTheme.TituloSeccion("Indicaciones generales"));
            documento.Add(new Paragraph(receta.IndicacionesGenerales).SetFontSize(9));
        }
        PdfTheme.AgregarPie(documento);
        return rutaArchivo;
    }

    public string GenerarCarnetVacunacion(long idMascota, string rutaArchivo)
    {
        ExpedienteEncabezadoModel paciente = _expediente.ObtenerEncabezado(idMascota);
        List<ExpedienteVacunaModel> vacunas = _expediente.ListarVacunas(idMascota);
        using Document documento = CrearDocumento(rutaArchivo);
        PdfTheme.AgregarEncabezado(documento, "Carnet de vacunación", "Histórico de vacunas aplicadas");
        documento.Add(PdfTheme.DatosPaciente(paciente.CodigoPaciente, paciente.Mascota, paciente.Dueno, $"{paciente.Especie} / {paciente.Raza}", paciente.Telefono));
        documento.Add(PdfTheme.TituloSeccion("Vacunas aplicadas"));
        Table tabla = PdfTheme.Tabla(new float[] { 15, 24, 11, 15, 13, 22 });
        foreach (string encabezado in new[] { "Fecha", "Vacuna", "Dosis", "Lote", "Próxima", "Veterinario" }) tabla.AddHeaderCell(PdfTheme.EncabezadoTabla(encabezado));
        if (vacunas.Count == 0)
        {
            tabla.AddCell(new Cell(1, 6).Add(new Paragraph("No existen vacunas registradas para este paciente.")));
        }
        else
        {
            foreach (ExpedienteVacunaModel item in vacunas)
            {
                tabla.AddCell(PdfTheme.Celda(item.FechaAplicacion.ToString("dd/MM/yyyy")));
                tabla.AddCell(PdfTheme.Celda(item.Vacuna));
                tabla.AddCell(PdfTheme.Celda(item.Dosis));
                tabla.AddCell(PdfTheme.Celda(item.Lote));
                tabla.AddCell(PdfTheme.Celda(item.ProximaDosis?.ToString("dd/MM/yyyy") ?? "-"));
                tabla.AddCell(PdfTheme.Celda(item.Veterinario));
            }
        }
        documento.Add(tabla);
        PdfTheme.AgregarPie(documento);
        return rutaArchivo;
    }

    public string GenerarEstadoCuenta(long idMascota, string rutaArchivo)
    {
        ExpedienteEncabezadoModel paciente = _expediente.ObtenerEncabezado(idMascota);
        List<ExpedienteFacturaModel> facturas = _expediente.ListarFacturas(idMascota);
        using Document documento = CrearDocumento(rutaArchivo);
        PdfTheme.AgregarEncabezado(documento, "Estado de cuenta", "Facturas asociadas al paciente");
        documento.Add(PdfTheme.DatosPaciente(paciente.CodigoPaciente, paciente.Mascota, paciente.Dueno, $"{paciente.Especie} / {paciente.Raza}", paciente.Telefono));
        documento.Add(PdfTheme.TituloSeccion("Facturas"));
        Table tabla = PdfTheme.Tabla(new float[] { 22, 17, 16, 16, 16, 13 });
        foreach (string titulo in new[] { "Factura", "Fecha", "Total", "Pagado", "Saldo", "Estado" }) tabla.AddHeaderCell(PdfTheme.EncabezadoTabla(titulo));
        foreach (ExpedienteFacturaModel factura in facturas)
        {
            tabla.AddCell(PdfTheme.Celda(factura.NumeroFactura));
            tabla.AddCell(PdfTheme.Celda(factura.FechaEmision.ToString("dd/MM/yyyy")));
            tabla.AddCell(PdfTheme.Celda(factura.Total.ToString("C2")));
            tabla.AddCell(PdfTheme.Celda(factura.TotalPagado.ToString("C2")));
            tabla.AddCell(PdfTheme.Celda(factura.SaldoPendiente.ToString("C2")));
            tabla.AddCell(PdfTheme.Celda(factura.Estado));
        }
        if (facturas.Count == 0) tabla.AddCell(new Cell(1, 6).Add(new Paragraph("No existen facturas asociadas al paciente.")));
        documento.Add(tabla);
        documento.Add(new Paragraph($"Saldo pendiente total: {paciente.SaldoPendiente:C2}").SetFont(PdfTheme.CrearFuenteNegrita()).SetTextAlignment(TextAlignment.RIGHT).SetMarginTop(10));
        PdfTheme.AgregarPie(documento);
        return rutaArchivo;
    }

    public string GenerarExpediente(long idMascota, string rutaArchivo)
    {
        ExpedienteEncabezadoModel paciente = _expediente.ObtenerEncabezado(idMascota);
        List<LineaTiempoExpedienteModel> lineaTiempo = _expediente.ListarLineaTiempo(idMascota);
        List<ExpedienteConsultaResumenModel> consultas = _expediente.ListarConsultas(idMascota);
        List<ExpedienteVacunaModel> vacunas = _expediente.ListarVacunas(idMascota);
        List<ExpedienteDesparasitacionModel> desparasitaciones = _expediente.ListarDesparasitaciones(idMascota);
        List<ExpedienteOrdenModel> ordenes = _expediente.ListarOrdenes(idMascota);
        using Document documento = CrearDocumento(rutaArchivo);
        PdfTheme.AgregarEncabezado(documento, "Expediente médico integral", $"Paciente {paciente.CodigoPaciente}");
        documento.Add(PdfTheme.DatosPaciente(paciente.CodigoPaciente, paciente.Mascota, paciente.Dueno, $"{paciente.Especie} / {paciente.Raza}", paciente.Telefono));
        Table resumen = PdfTheme.Tabla(new float[] { 18, 32, 18, 32 });
        AgregarPar(resumen, "Sexo / edad", $"{paciente.Sexo} / {paciente.EdadTexto}");
        AgregarPar(resumen, "Peso reciente", paciente.PesoActual.HasValue ? $"{paciente.PesoActual:0.00} kg" : "No registrado");
        AgregarPar(resumen, "Estado vital", paciente.EstadoVital);
        AgregarPar(resumen, "Microchip", string.IsNullOrWhiteSpace(paciente.Microchip) ? "No registrado" : paciente.Microchip);
        documento.Add(resumen);
        if (paciente.AlertasActivas.Count > 0)
        {
            documento.Add(PdfTheme.TituloSeccion("Alertas clínicas activas"));
            foreach (AlertaClinicaModel alerta in paciente.AlertasActivas)
                documento.Add(new Paragraph($"- {alerta.TipoAlerta}: {alerta.Descripcion}").SetFontSize(9));
        }
        documento.Add(PdfTheme.TituloSeccion("Línea de tiempo"));
        Table timeline = PdfTheme.Tabla(new float[] { 17, 18, 43, 22 });
        foreach (string titulo in new[] { "Fecha", "Tipo", "Descripción", "Profesional / estado" }) timeline.AddHeaderCell(PdfTheme.EncabezadoTabla(titulo));
        foreach (LineaTiempoExpedienteModel evento in lineaTiempo.Take(40))
        {
            timeline.AddCell(PdfTheme.Celda(evento.Fecha.ToString("dd/MM/yyyy HH:mm")));
            timeline.AddCell(PdfTheme.Celda(evento.Tipo)); timeline.AddCell(PdfTheme.Celda(evento.Descripcion)); timeline.AddCell(PdfTheme.Celda(evento.ProfesionalEstado));
        }
        documento.Add(timeline);
        documento.Add(PdfTheme.TituloSeccion("Consultas"));
        Table tablaConsultas = PdfTheme.Tabla(new float[] { 16, 22, 34, 28 });
        foreach (string titulo in new[] { "Fecha", "Veterinario", "Diagnóstico principal", "Estado al egreso" }) tablaConsultas.AddHeaderCell(PdfTheme.EncabezadoTabla(titulo));
        foreach (ExpedienteConsultaResumenModel item in consultas)
        {
            tablaConsultas.AddCell(PdfTheme.Celda(item.FechaAtencion.ToString("dd/MM/yyyy"))); tablaConsultas.AddCell(PdfTheme.Celda(item.Veterinario));
            tablaConsultas.AddCell(PdfTheme.Celda(item.DiagnosticoPrincipal)); tablaConsultas.AddCell(PdfTheme.Celda(item.EstadoEgreso));
        }
        documento.Add(tablaConsultas);
        AgregarVacunasCompactas(documento, vacunas);
        AgregarDesparasitacionesCompactas(documento, desparasitaciones);
        AgregarOrdenesCompactas(documento, ordenes);
        PdfTheme.AgregarPie(documento);
        return rutaArchivo;
    }

    public string GenerarFactura(long idFactura, string rutaArchivo)
    {
        FacturaModel factura = _facturacion.ObtenerFactura(idFactura);
        using Document documento = CrearDocumento(rutaArchivo);
        PdfTheme.AgregarEncabezado(documento, "Factura", $"{factura.NumeroFactura} | Moneda: GTQ / Quetzales | IVA incluido 12%");
        Table datos = PdfTheme.Tabla(new float[] { 20, 30, 20, 30 });
        AgregarPar(datos, "Factura", factura.NumeroFactura);
        AgregarPar(datos, "Emisión", factura.FechaEmision.ToString("dd/MM/yyyy HH:mm"));
        AgregarPar(datos, "Dueño", factura.Dueno);
        AgregarPar(datos, "Paciente", string.IsNullOrWhiteSpace(factura.Mascota) ? "-" : factura.Mascota);
        AgregarPar(datos, "Estado", factura.Estado);
        AgregarPar(datos, "Usuario", factura.UsuarioCreacion);
        documento.Add(datos);
        documento.Add(PdfTheme.TituloSeccion("Detalle facturado"));
        Table detalle = PdfTheme.Tabla(new float[] { 16, 38, 11, 14, 10, 14 });
        foreach (string texto in new[] { "Tipo", "Descripción", "Cant.", "Precio", "Desc.", "Subtotal" }) detalle.AddHeaderCell(PdfTheme.EncabezadoTabla(texto));
        foreach (FacturaDetalleModel item in factura.Detalles)
        {
            detalle.AddCell(PdfTheme.Celda(item.TipoItem)); detalle.AddCell(PdfTheme.Celda(item.Descripcion));
            detalle.AddCell(PdfTheme.Celda(item.Cantidad.ToString("0.##"))); detalle.AddCell(PdfTheme.Celda(item.PrecioUnitario.ToString("C2")));
            detalle.AddCell(PdfTheme.Celda(item.Descuento.ToString("C2"))); detalle.AddCell(PdfTheme.Celda(item.Subtotal.ToString("C2")));
        }
        documento.Add(detalle);
        Table totales = PdfTheme.Tabla(new float[] { 72, 28 }); totales.SetMarginTop(10);
        AgregarPar(totales, "Total listado (IVA incluido)", factura.Subtotal.ToString("C2"));
        AgregarPar(totales, "Descuento", factura.DescuentoTotal.ToString("C2"));
        AgregarPar(totales, "Base imponible sin IVA", FiscalGuatemala.CalcularBaseImponible(factura.Total).ToString("C2"));
        AgregarPar(totales, "IVA incluido (12%)", factura.ImpuestoTotal.ToString("C2"));
        AgregarPar(totales, "Total a pagar", factura.Total.ToString("C2"));
        AgregarPar(totales, "Pagado", factura.TotalPagado.ToString("C2"));
        AgregarPar(totales, "Saldo pendiente", factura.SaldoPendiente.ToString("C2"));
        documento.Add(totales);
        if (!string.IsNullOrWhiteSpace(factura.Observaciones)) documento.Add(new Paragraph($"Observaciones: {factura.Observaciones}").SetMarginTop(10));
        if (factura.Estado == "Anulada") documento.Add(new Paragraph($"FACTURA ANULADA: {factura.MotivoAnulacion}").SetFont(PdfTheme.CrearFuenteNegrita()).SetFontColor(PdfTheme.PeligroSuave).SetMarginTop(10));
        PdfTheme.AgregarPie(documento);
        return rutaArchivo;
    }

    public string GenerarRecibo(long idPago, string rutaArchivo)
    {
        PagoModel pago = _facturacion.ObtenerPagoParaDocumento(idPago);
        FacturaModel factura = _facturacion.ObtenerFactura(pago.IdFactura);
        using Document documento = CrearDocumento(rutaArchivo);
        PdfTheme.AgregarEncabezado(documento, "Recibo de pago", $"Recibo No. {pago.IdPago} | Moneda: GTQ / Quetzales");
        Table datos = PdfTheme.Tabla(new float[] { 20, 30, 20, 30 });
        AgregarPar(datos, "Factura", pago.NumeroFactura); AgregarPar(datos, "Fecha", pago.FechaPago.ToString("dd/MM/yyyy HH:mm"));
        AgregarPar(datos, "Cliente", factura.Dueno); AgregarPar(datos, "Paciente", string.IsNullOrWhiteSpace(factura.Mascota) ? "-" : factura.Mascota);
        AgregarPar(datos, "Método", pago.MetodoPago); AgregarPar(datos, "Referencia", string.IsNullOrWhiteSpace(pago.Referencia) ? "-" : pago.Referencia);
        AgregarPar(datos, "Estado", pago.Estado); AgregarPar(datos, "Registró", pago.UsuarioRegistro);
        documento.Add(datos);
        documento.Add(new Paragraph($"Monto recibido: {pago.Monto:C2}").SetFont(PdfTheme.CrearFuenteNegrita()).SetFontSize(15).SetFontColor(PdfTheme.Primario).SetTextAlignment(TextAlignment.RIGHT).SetMarginTop(22));
        documento.Add(new Paragraph($"Saldo posterior de factura: {factura.SaldoPendiente:C2}").SetTextAlignment(TextAlignment.RIGHT).SetMarginTop(4));
        if (!string.IsNullOrWhiteSpace(pago.Observaciones)) documento.Add(new Paragraph($"Observaciones: {pago.Observaciones}").SetMarginTop(12));
        if (pago.Estado == "Anulado") documento.Add(new Paragraph($"PAGO ANULADO: {pago.MotivoAnulacion}").SetFont(PdfTheme.CrearFuenteNegrita()).SetMarginTop(12));
        PdfTheme.AgregarPie(documento);
        return rutaArchivo;
    }

    public string GenerarReporteCaja(DateTime fecha, string rutaArchivo)
    {
        CajaResumenModel caja = _facturacion.ObtenerCajaDiaria(fecha);
        using Document documento = CrearDocumento(rutaArchivo);
        PdfTheme.AgregarEncabezado(documento, "Reporte de caja diaria", fecha.ToString("dd/MM/yyyy"));
        Table resumen = PdfTheme.Tabla(new float[] { 24, 26, 24, 26 });
        AgregarPar(resumen, "Total cobrado", caja.TotalCobrado.ToString("C2"));
        AgregarPar(resumen, "Pagos aplicados", caja.PagosAplicados.ToString());
        AgregarPar(resumen, "Facturas emitidas", caja.FacturasEmitidas.ToString());
        AgregarPar(resumen, "Facturas pagadas", caja.FacturasPagadas.ToString());
        AgregarPar(resumen, "Facturas parciales", caja.FacturasParciales.ToString());
        AgregarPar(resumen, "Facturas anuladas", caja.FacturasAnuladas.ToString());
        AgregarPar(resumen, "Saldo pendiente generado", caja.SaldosPendientesGenerados.ToString("C2"));
        AgregarPar(resumen, "Fecha", fecha.ToString("dd/MM/yyyy"));
        documento.Add(resumen);
        documento.Add(PdfTheme.TituloSeccion("Totales por método de pago"));
        Table metodos = PdfTheme.Tabla(new float[] { 50, 22, 28 });
        foreach (string texto in new[] { "Método", "Pagos", "Total" }) metodos.AddHeaderCell(PdfTheme.EncabezadoTabla(texto));
        foreach (CajaMetodoPagoModel item in caja.TotalesPorMetodo) { metodos.AddCell(PdfTheme.Celda(item.MetodoPago)); metodos.AddCell(PdfTheme.Celda(item.CantidadPagos.ToString())); metodos.AddCell(PdfTheme.Celda(item.Total.ToString("C2"))); }
        documento.Add(metodos);
        documento.Add(PdfTheme.TituloSeccion("Movimientos del día"));
        Table pagos = PdfTheme.Tabla(new float[] { 18, 24, 22, 18, 18 });
        foreach (string texto in new[] { "Hora", "Factura", "Método", "Monto", "Estado" }) pagos.AddHeaderCell(PdfTheme.EncabezadoTabla(texto));
        foreach (PagoModel item in caja.PagosDelDia) { pagos.AddCell(PdfTheme.Celda(item.FechaPago.ToString("HH:mm"))); pagos.AddCell(PdfTheme.Celda(item.NumeroFactura)); pagos.AddCell(PdfTheme.Celda(item.MetodoPago)); pagos.AddCell(PdfTheme.Celda(item.Monto.ToString("C2"))); pagos.AddCell(PdfTheme.Celda(item.Estado)); }
        documento.Add(pagos);
        PdfTheme.AgregarPie(documento);
        return rutaArchivo;
    }


    public string GenerarReporteOperativo(ReporteResultadoModel reporte, string rutaArchivo)
    {
        if (reporte is null) throw new ArgumentNullException(nameof(reporte));
        using Document documento = CrearDocumento(rutaArchivo);
        PdfTheme.AgregarEncabezado(documento, reporte.Titulo, reporte.DescripcionPeriodo, "Reporte administrativo");
        Table contexto = PdfTheme.Tabla(new float[] { 22, 78 });
        AgregarPar(contexto, "Periodo", reporte.DescripcionPeriodo);
        AgregarPar(contexto, "Filtro", string.IsNullOrWhiteSpace(reporte.FiltroAplicado) ? "Sin filtro adicional" : reporte.FiltroAplicado);
        AgregarPar(contexto, reporte.IndicadorNombre, reporte.IndicadorValor);
        documento.Add(contexto);
        documento.Add(PdfTheme.TituloSeccion("Detalle"));
        float[] anchos = Enumerable.Repeat(1F, reporte.Columnas.Count == 0 ? 1 : reporte.Columnas.Count).ToArray();
        Table tabla = PdfTheme.Tabla(anchos);
        foreach (string encabezado in reporte.Columnas) tabla.AddHeaderCell(PdfTheme.EncabezadoTabla(encabezado));
        if (reporte.Filas.Count == 0)
        {
            tabla.AddCell(new Cell(1, reporte.Columnas.Count == 0 ? 1 : reporte.Columnas.Count).Add(new Paragraph("No existen registros para los filtros seleccionados.")));
        }
        else
        {
            foreach (List<string> fila in reporte.Filas)
            {
                foreach (string valor in fila) tabla.AddCell(PdfTheme.Celda(valor));
            }
        }
        documento.Add(tabla);
        PdfTheme.AgregarPie(documento);
        return rutaArchivo;
    }

    private static Document CrearDocumento(string rutaArchivo)
    {
        string? directorio = System.IO.Path.GetDirectoryName(rutaArchivo);
        if (!string.IsNullOrWhiteSpace(directorio)) Directory.CreateDirectory(directorio);
        PdfWriter writer = new(rutaArchivo);
        PdfDocument pdf = new(writer);
        Document documento = new(pdf, PageSize.A4);
        documento.SetMargins(38, 38, 38, 38);
        documento.SetFont(PdfTheme.CrearFuenteNormal());
        documento.SetFontSize(9);
        return documento;
    }

    private static void AgregarPar(Table tabla, string etiqueta, string valor)
    {
        tabla.AddCell(PdfTheme.CeldaClave(etiqueta));
        tabla.AddCell(PdfTheme.Celda(valor));
    }

    private static void AgregarDiagnosticos(Document documento, IEnumerable<DiagnosticoModel> diagnosticos)
    {
        documento.Add(PdfTheme.TituloSeccion("Diagnósticos"));
        Table tabla = PdfTheme.Tabla(new float[] { 18, 48, 34 });
        foreach (string titulo in new[] { "Tipo", "Diagnóstico", "Observaciones" }) tabla.AddHeaderCell(PdfTheme.EncabezadoTabla(titulo));
        foreach (DiagnosticoModel d in diagnosticos)
        {
            tabla.AddCell(PdfTheme.Celda(d.EsPrincipal ? "Principal" : "Secundario")); tabla.AddCell(PdfTheme.Celda(d.Descripcion)); tabla.AddCell(PdfTheme.Celda(d.Observaciones));
        }
        documento.Add(tabla);
    }

    private static void AgregarServicios(Document documento, IEnumerable<ConsultaServicioModel> servicios)
    {
        documento.Add(PdfTheme.TituloSeccion("Servicios y procedimientos"));
        Table tabla = PdfTheme.Tabla(new float[] { 42, 12, 16, 14, 16 });
        foreach (string titulo in new[] { "Descripción", "Cant.", "Precio", "Desc.", "Subtotal" }) tabla.AddHeaderCell(PdfTheme.EncabezadoTabla(titulo));
        foreach (ConsultaServicioModel item in servicios)
        {
            tabla.AddCell(PdfTheme.Celda(item.Descripcion)); tabla.AddCell(PdfTheme.Celda(item.Cantidad.ToString("0.##"))); tabla.AddCell(PdfTheme.Celda(item.PrecioUnitario.ToString("C2"))); tabla.AddCell(PdfTheme.Celda(item.Descuento.ToString("C2"))); tabla.AddCell(PdfTheme.Celda(item.Subtotal.ToString("C2")));
        }
        documento.Add(tabla);
    }

    private static void AgregarVacunasCompactas(Document documento, List<ExpedienteVacunaModel> vacunas)
    {
        documento.Add(PdfTheme.TituloSeccion("Vacunación"));
        if (vacunas.Count == 0) { documento.Add(new Paragraph("Sin registros de vacunación.").SetFontSize(9)); return; }
        Table tabla = PdfTheme.Tabla(new float[] { 18, 27, 18, 18, 19 });
        foreach (string titulo in new[] { "Fecha", "Vacuna", "Lote", "Próxima", "Veterinario" }) tabla.AddHeaderCell(PdfTheme.EncabezadoTabla(titulo));
        foreach (ExpedienteVacunaModel v in vacunas) { tabla.AddCell(PdfTheme.Celda(v.FechaAplicacion.ToString("dd/MM/yyyy"))); tabla.AddCell(PdfTheme.Celda(v.Vacuna)); tabla.AddCell(PdfTheme.Celda(v.Lote)); tabla.AddCell(PdfTheme.Celda(v.ProximaDosis?.ToString("dd/MM/yyyy") ?? "-")); tabla.AddCell(PdfTheme.Celda(v.Veterinario)); }
        documento.Add(tabla);
    }

    private static void AgregarDesparasitacionesCompactas(Document documento, List<ExpedienteDesparasitacionModel> items)
    {
        documento.Add(PdfTheme.TituloSeccion("Desparasitaciones"));
        if (items.Count == 0) { documento.Add(new Paragraph("Sin registros de desparasitación.").SetFontSize(9)); return; }
        Table tabla = PdfTheme.Tabla(new float[] { 18, 30, 18, 17, 17 });
        foreach (string titulo in new[] { "Fecha", "Producto", "Dosis", "Próxima", "Veterinario" }) tabla.AddHeaderCell(PdfTheme.EncabezadoTabla(titulo));
        foreach (ExpedienteDesparasitacionModel i in items) { tabla.AddCell(PdfTheme.Celda(i.FechaAplicacion.ToString("dd/MM/yyyy"))); tabla.AddCell(PdfTheme.Celda(i.Producto)); tabla.AddCell(PdfTheme.Celda(i.Dosis)); tabla.AddCell(PdfTheme.Celda(i.ProximaAplicacion?.ToString("dd/MM/yyyy") ?? "-")); tabla.AddCell(PdfTheme.Celda(i.Veterinario)); }
        documento.Add(tabla);
    }

    private static void AgregarOrdenesCompactas(Document documento, List<ExpedienteOrdenModel> items)
    {
        documento.Add(PdfTheme.TituloSeccion("Órdenes clínicas"));
        if (items.Count == 0) { documento.Add(new Paragraph("Sin órdenes clínicas registradas.").SetFontSize(9)); return; }
        Table tabla = PdfTheme.Tabla(new float[] { 18, 18, 34, 18, 12 });
        foreach (string titulo in new[] { "Fecha", "Tipo", "Estudio", "Estado", "Precio" }) tabla.AddHeaderCell(PdfTheme.EncabezadoTabla(titulo));
        foreach (ExpedienteOrdenModel i in items) { tabla.AddCell(PdfTheme.Celda(i.FechaSolicitud.ToString("dd/MM/yyyy"))); tabla.AddCell(PdfTheme.Celda(i.TipoOrden)); tabla.AddCell(PdfTheme.Celda(i.NombreEstudio)); tabla.AddCell(PdfTheme.Celda(i.Estado)); tabla.AddCell(PdfTheme.Celda(i.Precio.ToString("C2"))); }
        documento.Add(tabla);
    }

    private static string Formatear(decimal? valor, string unidad) => valor.HasValue ? $"{valor:0.##} {unidad}" : "-";
    private static string TextoOpcional(string texto) => string.IsNullOrWhiteSpace(texto) ? string.Empty : $" ({texto})";
}
