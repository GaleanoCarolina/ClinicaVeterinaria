using System;
using System.Collections.Generic;
using System.Linq;
using ClinicaVeterinaria.Data;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Utils;
using MySql.Data.MySqlClient;

namespace ClinicaVeterinaria.Services;

public sealed class FacturacionService
{
    public List<CargoPendienteModel> ListarCargosPendientes(string filtro = "")
    {
        ExigirPermiso();
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(@"
SELECT cp.id_cargo, cp.id_dueno, cp.id_mascota, cp.id_consulta, cp.id_cita,
       IFNULL(m.codigo_paciente, '') AS codigo_paciente,
       d.nombre_completo AS dueno, IFNULL(m.nombre, '') AS mascota,
       cp.tipo_item, cp.id_referencia, cp.descripcion, cp.cantidad,
       cp.precio_unitario, cp.descuento, cp.subtotal, cp.fecha_creacion
FROM cargos_pendientes cp
INNER JOIN duenos d ON d.id_dueno = cp.id_dueno
LEFT JOIN mascotas m ON m.id_mascota = cp.id_mascota
WHERE cp.estado = 'Pendiente'
  AND (@filtro = '' OR d.nombre_completo LIKE @buscar OR IFNULL(m.nombre, '') LIKE @buscar
       OR IFNULL(m.codigo_paciente, '') LIKE @buscar OR cp.descripcion LIKE @buscar)
ORDER BY cp.fecha_creacion, cp.id_cargo;", conexion);
        comando.Parameters.AddWithValue("@filtro", filtro.Trim());
        comando.Parameters.AddWithValue("@buscar", $"%{filtro.Trim()}%");
        using MySqlDataReader lector = comando.ExecuteReader();
        List<CargoPendienteModel> resultado = new();
        while (lector.Read()) resultado.Add(MapearCargo(lector));
        return resultado;
    }

    public List<FacturaModel> ListarFacturas(DateTime fechaInicio, DateTime fechaFin, string estado = "Todos", string filtro = "")
    {
        ExigirPermiso();
        if (fechaFin.Date < fechaInicio.Date) throw new ArgumentException("La fecha final no puede ser anterior a la inicial.");
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(@"
SELECT f.id_factura, f.numero_factura, f.id_dueno, f.id_mascota, f.id_cita, f.id_consulta,
       d.nombre_completo AS dueno, IFNULL(m.nombre, '') AS mascota, f.fecha_emision,
       f.subtotal, f.descuento_total, f.impuesto_total, f.total, f.total_pagado,
       f.saldo_pendiente, f.estado, IFNULL(f.observaciones, '') AS observaciones,
       u.nombre_completo AS usuario_creacion, f.fecha_anulacion,
       IFNULL(f.motivo_anulacion, '') AS motivo_anulacion
FROM facturas f
INNER JOIN duenos d ON d.id_dueno = f.id_dueno
LEFT JOIN mascotas m ON m.id_mascota = f.id_mascota
INNER JOIN usuarios u ON u.id_usuario = f.id_usuario_creacion
WHERE f.fecha_emision >= @inicio AND f.fecha_emision < DATE_ADD(@fin, INTERVAL 1 DAY)
  AND (@estado = 'Todos' OR f.estado = @estado)
  AND (@filtro = '' OR f.numero_factura LIKE @buscar OR d.nombre_completo LIKE @buscar OR IFNULL(m.nombre, '') LIKE @buscar)
ORDER BY f.fecha_emision DESC, f.id_factura DESC;", conexion);
        comando.Parameters.AddWithValue("@inicio", fechaInicio.Date);
        comando.Parameters.AddWithValue("@fin", fechaFin.Date);
        comando.Parameters.AddWithValue("@estado", estado);
        comando.Parameters.AddWithValue("@filtro", filtro.Trim());
        comando.Parameters.AddWithValue("@buscar", $"%{filtro.Trim()}%");
        using MySqlDataReader lector = comando.ExecuteReader();
        List<FacturaModel> resultado = new();
        while (lector.Read()) resultado.Add(MapearFactura(lector));
        return resultado;
    }

    public FacturaModel ObtenerFactura(long idFactura)
    {
        ExigirPermiso();
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        FacturaModel factura = ObtenerFactura(conexion, null, idFactura, false);
        factura.Detalles = ListarDetalles(conexion, null, idFactura);
        factura.Pagos = ListarPagos(conexion, null, idFactura);
        return factura;
    }

    public PagoModel ObtenerPagoParaDocumento(long idPago)
    {
        ExigirPermiso();
        return DbTransactionHelper.Ejecutar((conexion, transaccion) => ObtenerPago(conexion, transaccion, idPago));
    }

    public List<MetodoPagoModel> ListarMetodosPago()
    {
        ExigirPermiso();
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new("SELECT id_metodo_pago, nombre FROM metodos_pago WHERE activo = 1 ORDER BY nombre;", conexion);
        using MySqlDataReader lector = comando.ExecuteReader();
        List<MetodoPagoModel> metodos = new();
        while (lector.Read()) metodos.Add(new MetodoPagoModel { IdMetodoPago = lector.GetInt32("id_metodo_pago"), Nombre = lector.GetString("nombre") });
        return metodos;
    }

    public FacturaModel CrearFactura(CrearFacturaRequestModel solicitud)
    {
        ExigirPermiso();
        if (solicitud.IdsCargos.Count == 0) throw new InvalidOperationException("Seleccione al menos un cargo pendiente para facturar.");
        if (solicitud.DescuentoTotal < 0) throw new InvalidOperationException("El descuento no puede ser negativo.");
        List<long> ids = solicitud.IdsCargos.Distinct().ToList();
        return DbTransactionHelper.Ejecutar((conexion, transaccion) =>
        {
            List<CargoPendienteModel> cargos = ObtenerCargosParaFacturar(conexion, transaccion, ids);
            if (cargos.Count != ids.Count) throw new InvalidOperationException("Uno o más cargos ya fueron facturados o no existen.");
            long idDueno = cargos[0].IdDueno;
            if (cargos.Any(c => c.IdDueno != idDueno)) throw new InvalidOperationException("Una factura solo puede agrupar cargos del mismo dueño.");
            decimal subtotal = cargos.Sum(c => c.Subtotal);
            if (solicitud.DescuentoTotal > subtotal) throw new InvalidOperationException("El descuento no puede ser mayor que el subtotal.");
            // En Guatemala el precio de venta/servicio ya incluye IVA.
            // El total del cliente no aumenta: se extrae automáticamente el IVA incluido.
            decimal total = FiscalGuatemala.TotalConIva(subtotal, solicitud.DescuentoTotal);
            decimal impuestoIncluido = FiscalGuatemala.CalcularIvaIncluido(total);
            if (total <= 0) throw new InvalidOperationException("El total de la factura debe ser mayor que cero.");
            long? idMascota = cargos.Select(c => c.IdMascota).Distinct().Count() == 1 ? cargos[0].IdMascota : null;
            long? idConsulta = cargos.Select(c => c.IdConsulta).Distinct().Count() == 1 ? cargos[0].IdConsulta : null;
            long? idCita = cargos.Select(c => c.IdCita).Distinct().Count() == 1 ? cargos[0].IdCita : null;
            string numero = ObtenerNumeroFactura(conexion, transaccion, DateTime.Today.Year);
            using MySqlCommand insertar = new(@"
INSERT INTO facturas(numero_factura, id_dueno, id_mascota, id_cita, id_consulta, fecha_emision,
 subtotal, descuento_total, impuesto_total, total, total_pagado, saldo_pendiente,
 estado, observaciones, id_usuario_creacion)
VALUES(@numero, @dueno, @mascota, @cita, @consulta, NOW(), @subtotal, @descuento,
 @impuesto, @total, 0, @total, 'Emitida', @observaciones, @usuario);", conexion, transaccion);
            insertar.Parameters.AddWithValue("@numero", numero);
            insertar.Parameters.AddWithValue("@dueno", idDueno);
            insertar.Parameters.AddWithValue("@mascota", (object?)idMascota ?? DBNull.Value);
            insertar.Parameters.AddWithValue("@cita", (object?)idCita ?? DBNull.Value);
            insertar.Parameters.AddWithValue("@consulta", (object?)idConsulta ?? DBNull.Value);
            insertar.Parameters.AddWithValue("@subtotal", subtotal);
            insertar.Parameters.AddWithValue("@descuento", solicitud.DescuentoTotal);
            insertar.Parameters.AddWithValue("@impuesto", impuestoIncluido);
            insertar.Parameters.AddWithValue("@total", total);
            insertar.Parameters.AddWithValue("@observaciones", ValorNulo(solicitud.Observaciones));
            insertar.Parameters.AddWithValue("@usuario", IdUsuarioActual());
            insertar.ExecuteNonQuery();
            long idFactura = insertar.LastInsertedId;
            foreach (CargoPendienteModel cargo in cargos)
            {
                InsertarDetalleFactura(conexion, transaccion, idFactura, cargo);
                MarcarCargoFacturado(conexion, transaccion, cargo.IdCargo, idFactura);
                MarcarOrigenFacturado(conexion, transaccion, cargo);
            }
            FacturaModel creada = ObtenerFactura(conexion, transaccion, idFactura, false);
            creada.Detalles = ListarDetalles(conexion, transaccion, idFactura);
            creada.Pagos = new List<PagoModel>();
            return creada;
        });
    }

    public PagoModel RegistrarPago(RegistrarPagoRequestModel solicitud)
    {
        ExigirPermiso();
        if (solicitud.Monto <= 0) throw new InvalidOperationException("El monto del pago debe ser mayor que cero.");
        if (solicitud.IdMetodoPago <= 0) throw new InvalidOperationException("Seleccione un método de pago válido.");
        return DbTransactionHelper.Ejecutar((conexion, transaccion) =>
        {
            FacturaModel factura = ObtenerFactura(conexion, transaccion, solicitud.IdFactura, true);
            if (factura.Estado == "Anulada") throw new InvalidOperationException("Una factura anulada no puede recibir pagos.");
            if (factura.SaldoPendiente <= 0) throw new InvalidOperationException("La factura ya no tiene saldo pendiente.");
            if (solicitud.Monto > factura.SaldoPendiente) throw new InvalidOperationException("El pago no puede ser mayor que el saldo pendiente.");
            using MySqlCommand metodo = new("SELECT COUNT(*) FROM metodos_pago WHERE id_metodo_pago = @id AND activo = 1;", conexion, transaccion);
            metodo.Parameters.AddWithValue("@id", solicitud.IdMetodoPago);
            if (Convert.ToInt32(metodo.ExecuteScalar()) == 0) throw new InvalidOperationException("El método de pago seleccionado no está activo.");
            using MySqlCommand insertar = new(@"
INSERT INTO pagos(id_factura, id_metodo_pago, fecha_pago, monto, referencia, observaciones, id_usuario_registro, estado)
VALUES(@factura, @metodo, NOW(), @monto, @referencia, @observaciones, @usuario, 'Aplicado');", conexion, transaccion);
            insertar.Parameters.AddWithValue("@factura", solicitud.IdFactura);
            insertar.Parameters.AddWithValue("@metodo", solicitud.IdMetodoPago);
            insertar.Parameters.AddWithValue("@monto", solicitud.Monto);
            insertar.Parameters.AddWithValue("@referencia", ValorNulo(solicitud.Referencia));
            insertar.Parameters.AddWithValue("@observaciones", ValorNulo(solicitud.Observaciones));
            insertar.Parameters.AddWithValue("@usuario", IdUsuarioActual());
            insertar.ExecuteNonQuery();
            long idPago = insertar.LastInsertedId;
            RecalcularFactura(conexion, transaccion, solicitud.IdFactura);
            return ObtenerPago(conexion, transaccion, idPago);
        });
    }

    public void AnularPago(long idPago, string motivo)
    {
        ExigirPermiso();
        if (string.IsNullOrWhiteSpace(motivo)) throw new InvalidOperationException("El motivo de anulación del pago es obligatorio.");
        DbTransactionHelper.Ejecutar((conexion, transaccion) =>
        {
            PagoModel pago = ObtenerPago(conexion, transaccion, idPago, true);
            if (pago.Estado == "Anulado") throw new InvalidOperationException("El pago ya se encuentra anulado.");
            FacturaModel factura = ObtenerFactura(conexion, transaccion, pago.IdFactura, true);
            if (factura.Estado == "Anulada") throw new InvalidOperationException("No puede anular pagos después de anular la factura.");
            using MySqlCommand comando = new(@"
UPDATE pagos SET estado = 'Anulado', id_usuario_anulacion = @usuario,
 fecha_anulacion = NOW(), motivo_anulacion = @motivo
WHERE id_pago = @id AND estado = 'Aplicado';", conexion, transaccion);
            comando.Parameters.AddWithValue("@usuario", IdUsuarioActual());
            comando.Parameters.AddWithValue("@motivo", motivo.Trim());
            comando.Parameters.AddWithValue("@id", idPago);
            if (comando.ExecuteNonQuery() != 1) throw new InvalidOperationException("No fue posible anular el pago.");
            RecalcularFactura(conexion, transaccion, pago.IdFactura);
        });
    }

    public void AnularFactura(long idFactura, string motivo)
    {
        ExigirPermiso();
        if (string.IsNullOrWhiteSpace(motivo)) throw new InvalidOperationException("El motivo de anulación de la factura es obligatorio.");
        DbTransactionHelper.Ejecutar((conexion, transaccion) =>
        {
            FacturaModel factura = ObtenerFactura(conexion, transaccion, idFactura, true);
            if (factura.Estado == "Anulada") throw new InvalidOperationException("La factura ya se encuentra anulada.");
            using MySqlCommand pagosActivos = new("SELECT COUNT(*) FROM pagos WHERE id_factura = @id AND estado = 'Aplicado';", conexion, transaccion);
            pagosActivos.Parameters.AddWithValue("@id", idFactura);
            if (Convert.ToInt32(pagosActivos.ExecuteScalar()) > 0)
                throw new InvalidOperationException("Anule primero los pagos aplicados antes de anular la factura.");
            using MySqlCommand anular = new(@"
UPDATE facturas SET estado = 'Anulada', id_usuario_anulacion = @usuario,
 fecha_anulacion = NOW(), motivo_anulacion = @motivo
WHERE id_factura = @id;", conexion, transaccion);
            anular.Parameters.AddWithValue("@usuario", IdUsuarioActual());
            anular.Parameters.AddWithValue("@motivo", motivo.Trim());
            anular.Parameters.AddWithValue("@id", idFactura);
            anular.ExecuteNonQuery();
            using MySqlCommand liberar = new("UPDATE cargos_pendientes SET estado = 'Pendiente', id_factura = NULL WHERE id_factura = @id AND estado = 'Facturado';", conexion, transaccion);
            liberar.Parameters.AddWithValue("@id", idFactura);
            liberar.ExecuteNonQuery();
            RestablecerOrigenes(conexion, transaccion, idFactura);
        });
    }

    public CajaResumenModel ObtenerCajaDiaria(DateTime fecha)
    {
        ExigirPermiso();
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        CajaResumenModel resumen = new() { Fecha = fecha.Date };
        using (MySqlCommand comando = new(@"
SELECT IFNULL(SUM(CASE WHEN p.estado='Aplicado' THEN p.monto ELSE 0 END),0) total_cobrado,
       SUM(CASE WHEN p.estado='Aplicado' THEN 1 ELSE 0 END) pagos_aplicados
FROM pagos p WHERE p.fecha_pago >= @fecha AND p.fecha_pago < DATE_ADD(@fecha, INTERVAL 1 DAY);", conexion))
        {
            comando.Parameters.AddWithValue("@fecha", fecha.Date);
            using MySqlDataReader lector = comando.ExecuteReader();
            if (lector.Read()) { resumen.TotalCobrado = lector.GetDecimal("total_cobrado"); resumen.PagosAplicados = lector.IsDBNull(lector.GetOrdinal("pagos_aplicados")) ? 0 : lector.GetInt32("pagos_aplicados"); }
        }
        using (MySqlCommand comando = new(@"
SELECT COUNT(*) facturas_emitidas,
 SUM(CASE WHEN estado='Pagada' THEN 1 ELSE 0 END) facturas_pagadas,
 SUM(CASE WHEN estado='Parcialmente pagada' THEN 1 ELSE 0 END) facturas_parciales,
 SUM(CASE WHEN estado='Anulada' THEN 1 ELSE 0 END) facturas_anuladas,
 IFNULL(SUM(CASE WHEN estado <> 'Anulada' THEN saldo_pendiente ELSE 0 END),0) saldos
FROM facturas WHERE fecha_emision >= @fecha AND fecha_emision < DATE_ADD(@fecha, INTERVAL 1 DAY);", conexion))
        {
            comando.Parameters.AddWithValue("@fecha", fecha.Date);
            using MySqlDataReader lector = comando.ExecuteReader();
            if (lector.Read())
            {
                resumen.FacturasEmitidas = lector.GetInt32("facturas_emitidas");
                resumen.FacturasPagadas = ValorInt(lector, "facturas_pagadas");
                resumen.FacturasParciales = ValorInt(lector, "facturas_parciales");
                resumen.FacturasAnuladas = ValorInt(lector, "facturas_anuladas");
                resumen.SaldosPendientesGenerados = lector.GetDecimal("saldos");
            }
        }
        using (MySqlCommand comando = new(@"
SELECT mp.nombre metodo_pago, COUNT(p.id_pago) cantidad_pagos, IFNULL(SUM(p.monto),0) total
FROM metodos_pago mp LEFT JOIN pagos p ON p.id_metodo_pago = mp.id_metodo_pago AND p.estado='Aplicado'
 AND p.fecha_pago >= @fecha AND p.fecha_pago < DATE_ADD(@fecha, INTERVAL 1 DAY)
WHERE mp.activo = 1 GROUP BY mp.id_metodo_pago, mp.nombre ORDER BY mp.nombre;", conexion))
        {
            comando.Parameters.AddWithValue("@fecha", fecha.Date);
            using MySqlDataReader lector = comando.ExecuteReader();
            while (lector.Read()) resumen.TotalesPorMetodo.Add(new CajaMetodoPagoModel { MetodoPago = lector.GetString("metodo_pago"), CantidadPagos = Convert.ToInt32(lector.GetInt64("cantidad_pagos")), Total = lector.GetDecimal("total") });
        }
        resumen.PagosDelDia = ListarPagosDelDia(conexion, fecha.Date);
        return resumen;
    }

    private static void ExigirPermiso() => SesionActual.ExigirRoles("Administrador", "Caja");
    private static int IdUsuarioActual() => SesionActual.Usuario?.IdUsuario ?? throw new InvalidOperationException("No existe una sesión activa.");
    private static object ValorNulo(string? texto) => string.IsNullOrWhiteSpace(texto) ? DBNull.Value : texto.Trim();
    private static int ValorInt(MySqlDataReader lector, string nombre) => lector.IsDBNull(lector.GetOrdinal(nombre)) ? 0 : Convert.ToInt32(lector.GetInt64(nombre));

    private static CargoPendienteModel MapearCargo(MySqlDataReader l) => new()
    {
        IdCargo = l.GetInt64("id_cargo"), IdDueno = l.GetInt64("id_dueno"),
        IdMascota = l.IsDBNull(l.GetOrdinal("id_mascota")) ? null : l.GetInt64("id_mascota"),
        IdConsulta = l.IsDBNull(l.GetOrdinal("id_consulta")) ? null : l.GetInt64("id_consulta"),
        IdCita = l.IsDBNull(l.GetOrdinal("id_cita")) ? null : l.GetInt64("id_cita"),
        CodigoPaciente = l.GetString("codigo_paciente"), Dueno = l.GetString("dueno"), Mascota = l.GetString("mascota"),
        TipoItem = l.GetString("tipo_item"), IdReferencia = l.IsDBNull(l.GetOrdinal("id_referencia")) ? null : l.GetInt64("id_referencia"),
        Descripcion = l.GetString("descripcion"), Cantidad = l.GetDecimal("cantidad"), PrecioUnitario = l.GetDecimal("precio_unitario"),
        Descuento = l.GetDecimal("descuento"), Subtotal = l.GetDecimal("subtotal"), FechaCreacion = l.GetDateTime("fecha_creacion")
    };

    private static FacturaModel MapearFactura(MySqlDataReader l) => new()
    {
        IdFactura = l.GetInt64("id_factura"), NumeroFactura = l.GetString("numero_factura"), IdDueno = l.GetInt64("id_dueno"),
        IdMascota = l.IsDBNull(l.GetOrdinal("id_mascota")) ? null : l.GetInt64("id_mascota"),
        IdCita = l.IsDBNull(l.GetOrdinal("id_cita")) ? null : l.GetInt64("id_cita"),
        IdConsulta = l.IsDBNull(l.GetOrdinal("id_consulta")) ? null : l.GetInt64("id_consulta"),
        Dueno = l.GetString("dueno"), Mascota = l.GetString("mascota"), FechaEmision = l.GetDateTime("fecha_emision"),
        Subtotal = l.GetDecimal("subtotal"), DescuentoTotal = l.GetDecimal("descuento_total"), ImpuestoTotal = l.GetDecimal("impuesto_total"), Total = l.GetDecimal("total"),
        TotalPagado = l.GetDecimal("total_pagado"), SaldoPendiente = l.GetDecimal("saldo_pendiente"), Estado = l.GetString("estado"),
        Observaciones = l.GetString("observaciones"), UsuarioCreacion = l.GetString("usuario_creacion"),
        FechaAnulacion = l.IsDBNull(l.GetOrdinal("fecha_anulacion")) ? null : l.GetDateTime("fecha_anulacion"), MotivoAnulacion = l.GetString("motivo_anulacion")
    };

    private static List<CargoPendienteModel> ObtenerCargosParaFacturar(MySqlConnection conexion, MySqlTransaction tx, List<long> ids)
    {
        string parametros = string.Join(",", ids.Select((_, i) => $"@id{i}"));
        using MySqlCommand comando = new($@"
SELECT cp.id_cargo, cp.id_dueno, cp.id_mascota, cp.id_consulta, cp.id_cita,
       IFNULL(m.codigo_paciente, '') codigo_paciente, d.nombre_completo dueno, IFNULL(m.nombre, '') mascota,
       cp.tipo_item, cp.id_referencia, cp.descripcion, cp.cantidad, cp.precio_unitario, cp.descuento, cp.subtotal, cp.fecha_creacion
FROM cargos_pendientes cp INNER JOIN duenos d ON d.id_dueno=cp.id_dueno LEFT JOIN mascotas m ON m.id_mascota=cp.id_mascota
WHERE cp.estado='Pendiente' AND cp.id_cargo IN ({parametros}) FOR UPDATE;", conexion, tx);
        for (int i = 0; i < ids.Count; i++) comando.Parameters.AddWithValue($"@id{i}", ids[i]);
        using MySqlDataReader lector = comando.ExecuteReader();
        List<CargoPendienteModel> cargos = new(); while (lector.Read()) cargos.Add(MapearCargo(lector)); return cargos;
    }

    private static string ObtenerNumeroFactura(MySqlConnection conexion, MySqlTransaction tx, int anio)
    {
        using MySqlCommand secuencia = new(@"
INSERT INTO secuencias_documentos(tipo_documento, anio, ultimo_numero) VALUES('FAC', @anio, 1)
ON DUPLICATE KEY UPDATE ultimo_numero = ultimo_numero + 1;", conexion, tx);
        secuencia.Parameters.AddWithValue("@anio", anio); secuencia.ExecuteNonQuery();
        using MySqlCommand leer = new("SELECT ultimo_numero FROM secuencias_documentos WHERE tipo_documento='FAC' AND anio=@anio FOR UPDATE;", conexion, tx);
        leer.Parameters.AddWithValue("@anio", anio);
        long numero = Convert.ToInt64(leer.ExecuteScalar());
        return $"FAC-{anio}-{numero:000000}";
    }

    private static void InsertarDetalleFactura(MySqlConnection conexion, MySqlTransaction tx, long idFactura, CargoPendienteModel cargo)
    {
        using MySqlCommand comando = new(@"
INSERT INTO factura_detalles(id_factura,tipo_item,id_referencia,descripcion,cantidad,precio_unitario,descuento,subtotal)
VALUES(@factura,@tipo,@referencia,@descripcion,@cantidad,@precio,@descuento,@subtotal);", conexion, tx);
        comando.Parameters.AddWithValue("@factura", idFactura); comando.Parameters.AddWithValue("@tipo", cargo.TipoItem);
        comando.Parameters.AddWithValue("@referencia", (object?)cargo.IdReferencia ?? DBNull.Value); comando.Parameters.AddWithValue("@descripcion", cargo.Descripcion);
        comando.Parameters.AddWithValue("@cantidad", cargo.Cantidad); comando.Parameters.AddWithValue("@precio", cargo.PrecioUnitario);
        comando.Parameters.AddWithValue("@descuento", cargo.Descuento); comando.Parameters.AddWithValue("@subtotal", cargo.Subtotal); comando.ExecuteNonQuery();
    }

    private static void MarcarCargoFacturado(MySqlConnection conexion, MySqlTransaction tx, long idCargo, long idFactura)
    {
        using MySqlCommand comando = new("UPDATE cargos_pendientes SET estado='Facturado', id_factura=@factura WHERE id_cargo=@cargo AND estado='Pendiente';", conexion, tx);
        comando.Parameters.AddWithValue("@factura", idFactura); comando.Parameters.AddWithValue("@cargo", idCargo);
        if (comando.ExecuteNonQuery() != 1) throw new InvalidOperationException("Un cargo cambió de estado durante la facturación.");
    }

    private static void MarcarOrigenFacturado(MySqlConnection conexion, MySqlTransaction tx, CargoPendienteModel cargo)
    {
        if (!cargo.IdReferencia.HasValue) return;
        string? sql = cargo.TipoItem switch
        {
            "Servicio" => "UPDATE consulta_servicios SET facturado=1 WHERE id_consulta_servicio=@id;",
            "Vacuna" => "UPDATE vacunas_aplicadas SET facturado=1 WHERE id_aplicacion=@id;",
            "Desparasitante" => "UPDATE desparasitaciones SET facturado=1 WHERE id_desparasitacion=@id;",
            "Laboratorio" => "UPDATE ordenes_clinicas SET facturado=1 WHERE id_orden=@id;",
            _ => null
        };
        if (sql is null) return;
        using MySqlCommand comando = new(sql, conexion, tx); comando.Parameters.AddWithValue("@id", cargo.IdReferencia.Value); comando.ExecuteNonQuery();
    }

    private static void RestablecerOrigenes(MySqlConnection conexion, MySqlTransaction tx, long idFactura)
    {
        foreach (string sql in new[]
        {
            "UPDATE consulta_servicios cs INNER JOIN factura_detalles fd ON fd.id_referencia=cs.id_consulta_servicio AND fd.tipo_item='Servicio' SET cs.facturado=0 WHERE fd.id_factura=@id;",
            "UPDATE vacunas_aplicadas va INNER JOIN factura_detalles fd ON fd.id_referencia=va.id_aplicacion AND fd.tipo_item='Vacuna' SET va.facturado=0 WHERE fd.id_factura=@id;",
            "UPDATE desparasitaciones dp INNER JOIN factura_detalles fd ON fd.id_referencia=dp.id_desparasitacion AND fd.tipo_item='Desparasitante' SET dp.facturado=0 WHERE fd.id_factura=@id;",
            "UPDATE ordenes_clinicas oc INNER JOIN factura_detalles fd ON fd.id_referencia=oc.id_orden AND fd.tipo_item='Laboratorio' SET oc.facturado=0 WHERE fd.id_factura=@id;"
        })
        { using MySqlCommand comando = new(sql, conexion, tx); comando.Parameters.AddWithValue("@id", idFactura); comando.ExecuteNonQuery(); }
    }

    private static FacturaModel ObtenerFactura(MySqlConnection conexion, MySqlTransaction? tx, long idFactura, bool bloquear)
    {
        using MySqlCommand comando = new($@"
SELECT f.id_factura,f.numero_factura,f.id_dueno,f.id_mascota,f.id_cita,f.id_consulta,d.nombre_completo dueno,IFNULL(m.nombre,'') mascota,
 f.fecha_emision,f.subtotal,f.descuento_total,f.impuesto_total,f.total,f.total_pagado,f.saldo_pendiente,f.estado,
 IFNULL(f.observaciones,'') observaciones,u.nombre_completo usuario_creacion,f.fecha_anulacion,IFNULL(f.motivo_anulacion,'') motivo_anulacion
FROM facturas f INNER JOIN duenos d ON d.id_dueno=f.id_dueno LEFT JOIN mascotas m ON m.id_mascota=f.id_mascota
INNER JOIN usuarios u ON u.id_usuario=f.id_usuario_creacion WHERE f.id_factura=@id {(bloquear ? "FOR UPDATE" : string.Empty)};", conexion, tx);
        comando.Parameters.AddWithValue("@id", idFactura);
        using MySqlDataReader lector = comando.ExecuteReader();
        if (!lector.Read()) throw new InvalidOperationException("No se encontró la factura seleccionada.");
        return MapearFactura(lector);
    }

    private static List<FacturaDetalleModel> ListarDetalles(MySqlConnection conexion, MySqlTransaction? tx, long idFactura)
    {
        using MySqlCommand comando = new("SELECT id_detalle,tipo_item,id_referencia,descripcion,cantidad,precio_unitario,descuento,subtotal FROM factura_detalles WHERE id_factura=@id ORDER BY id_detalle;", conexion, tx);
        comando.Parameters.AddWithValue("@id", idFactura); using MySqlDataReader l = comando.ExecuteReader(); List<FacturaDetalleModel> items = new();
        while (l.Read()) items.Add(new FacturaDetalleModel { IdDetalle=l.GetInt64("id_detalle"), TipoItem=l.GetString("tipo_item"), IdReferencia=l.IsDBNull(l.GetOrdinal("id_referencia"))?null:l.GetInt64("id_referencia"), Descripcion=l.GetString("descripcion"), Cantidad=l.GetDecimal("cantidad"), PrecioUnitario=l.GetDecimal("precio_unitario"), Descuento=l.GetDecimal("descuento"), Subtotal=l.GetDecimal("subtotal") });
        return items;
    }

    private static List<PagoModel> ListarPagos(MySqlConnection conexion, MySqlTransaction? tx, long idFactura)
    {
        using MySqlCommand comando = new(@"
SELECT p.id_pago,p.id_factura,f.numero_factura,p.id_metodo_pago,mp.nombre metodo_pago,p.fecha_pago,p.monto,IFNULL(p.referencia,'') referencia,
 IFNULL(p.observaciones,'') observaciones,u.nombre_completo usuario_registro,p.estado,p.fecha_anulacion,IFNULL(p.motivo_anulacion,'') motivo_anulacion
FROM pagos p INNER JOIN facturas f ON f.id_factura=p.id_factura INNER JOIN metodos_pago mp ON mp.id_metodo_pago=p.id_metodo_pago
INNER JOIN usuarios u ON u.id_usuario=p.id_usuario_registro WHERE p.id_factura=@id ORDER BY p.fecha_pago DESC,p.id_pago DESC;", conexion, tx);
        comando.Parameters.AddWithValue("@id", idFactura); using MySqlDataReader l = comando.ExecuteReader(); List<PagoModel> items = new(); while(l.Read()) items.Add(MapearPago(l)); return items;
    }

    private static List<PagoModel> ListarPagosDelDia(MySqlConnection conexion, DateTime fecha)
    {
        using MySqlCommand comando = new(@"
SELECT p.id_pago,p.id_factura,f.numero_factura,p.id_metodo_pago,mp.nombre metodo_pago,p.fecha_pago,p.monto,IFNULL(p.referencia,'') referencia,
 IFNULL(p.observaciones,'') observaciones,u.nombre_completo usuario_registro,p.estado,p.fecha_anulacion,IFNULL(p.motivo_anulacion,'') motivo_anulacion
FROM pagos p INNER JOIN facturas f ON f.id_factura=p.id_factura INNER JOIN metodos_pago mp ON mp.id_metodo_pago=p.id_metodo_pago
INNER JOIN usuarios u ON u.id_usuario=p.id_usuario_registro
WHERE p.fecha_pago>=@fecha AND p.fecha_pago<DATE_ADD(@fecha,INTERVAL 1 DAY) ORDER BY p.fecha_pago DESC;", conexion);
        comando.Parameters.AddWithValue("@fecha", fecha); using MySqlDataReader l=comando.ExecuteReader(); List<PagoModel> items=new(); while(l.Read()) items.Add(MapearPago(l)); return items;
    }

    private static PagoModel ObtenerPago(MySqlConnection conexion, MySqlTransaction tx, long idPago, bool bloquear = false)
    {
        using MySqlCommand comando = new($@"
SELECT p.id_pago,p.id_factura,f.numero_factura,p.id_metodo_pago,mp.nombre metodo_pago,p.fecha_pago,p.monto,IFNULL(p.referencia,'') referencia,
 IFNULL(p.observaciones,'') observaciones,u.nombre_completo usuario_registro,p.estado,p.fecha_anulacion,IFNULL(p.motivo_anulacion,'') motivo_anulacion
FROM pagos p INNER JOIN facturas f ON f.id_factura=p.id_factura INNER JOIN metodos_pago mp ON mp.id_metodo_pago=p.id_metodo_pago
INNER JOIN usuarios u ON u.id_usuario=p.id_usuario_registro WHERE p.id_pago=@id {(bloquear ? "FOR UPDATE" : string.Empty)};", conexion, tx);
        comando.Parameters.AddWithValue("@id", idPago); using MySqlDataReader l=comando.ExecuteReader(); if(!l.Read()) throw new InvalidOperationException("No se encontró el pago seleccionado."); return MapearPago(l);
    }

    private static PagoModel MapearPago(MySqlDataReader l) => new()
    {
        IdPago=l.GetInt64("id_pago"), IdFactura=l.GetInt64("id_factura"), NumeroFactura=l.GetString("numero_factura"), IdMetodoPago=l.GetInt32("id_metodo_pago"),
        MetodoPago=l.GetString("metodo_pago"), FechaPago=l.GetDateTime("fecha_pago"), Monto=l.GetDecimal("monto"), Referencia=l.GetString("referencia"),
        Observaciones=l.GetString("observaciones"), UsuarioRegistro=l.GetString("usuario_registro"), Estado=l.GetString("estado"),
        FechaAnulacion=l.IsDBNull(l.GetOrdinal("fecha_anulacion"))?null:l.GetDateTime("fecha_anulacion"), MotivoAnulacion=l.GetString("motivo_anulacion")
    };

    private static void RecalcularFactura(MySqlConnection conexion, MySqlTransaction tx, long idFactura)
    {
        using MySqlCommand monto = new("SELECT IFNULL(SUM(monto),0) FROM pagos WHERE id_factura=@id AND estado='Aplicado';", conexion, tx);
        monto.Parameters.AddWithValue("@id", idFactura); decimal pagado = Convert.ToDecimal(monto.ExecuteScalar());
        using MySqlCommand consultar = new("SELECT total FROM facturas WHERE id_factura=@id FOR UPDATE;", conexion, tx);
        consultar.Parameters.AddWithValue("@id", idFactura); decimal total = Convert.ToDecimal(consultar.ExecuteScalar());
        decimal saldo = total - pagado; if (saldo < 0) throw new InvalidOperationException("El total pagado excede el total de la factura.");
        string estado = saldo == 0 ? "Pagada" : pagado > 0 ? "Parcialmente pagada" : "Emitida";
        using MySqlCommand actualizar = new("UPDATE facturas SET total_pagado=@pagado,saldo_pendiente=@saldo,estado=@estado WHERE id_factura=@id AND estado<>'Anulada';", conexion, tx);
        actualizar.Parameters.AddWithValue("@pagado", pagado); actualizar.Parameters.AddWithValue("@saldo", saldo); actualizar.Parameters.AddWithValue("@estado", estado); actualizar.Parameters.AddWithValue("@id", idFactura); actualizar.ExecuteNonQuery();
    }
}
