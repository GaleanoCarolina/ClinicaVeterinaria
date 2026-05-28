using System;
using System.Collections.Generic;
using System.Linq;
using ClinicaVeterinaria.Data;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Utils;
using MySql.Data.MySqlClient;

namespace ClinicaVeterinaria.Services;

public sealed class ExpedienteService
{
    public List<MascotaExpedienteBusquedaModel> BuscarMascotas(string? termino)
    {
        ExigirLecturaExpediente();
        const string sql = """
            SELECT m.id_mascota, m.codigo_paciente, m.nombre mascota, m.especie,
                   d.nombre_completo dueno, d.telefono_principal telefono
            FROM mascotas m
            INNER JOIN duenos d ON d.id_dueno = m.id_dueno
            WHERE m.activo = 1
              AND (@Termino = '' OR m.nombre LIKE @Like OR m.codigo_paciente LIKE @Like
                   OR d.nombre_completo LIKE @Like OR d.telefono_principal LIKE @Like
                   OR COALESCE(m.microchip, '') LIKE @Like)
            ORDER BY m.nombre, d.nombre_completo
            LIMIT 100;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        string buscar = termino?.Trim() ?? string.Empty;
        comando.Parameters.Add("@Termino", MySqlDbType.VarChar).Value = buscar;
        comando.Parameters.Add("@Like", MySqlDbType.VarChar).Value = $"%{buscar}%";
        using MySqlDataReader lector = comando.ExecuteReader();
        List<MascotaExpedienteBusquedaModel> lista = new();
        while (lector.Read())
        {
            lista.Add(new MascotaExpedienteBusquedaModel
            {
                IdMascota = lector.GetInt64("id_mascota"),
                CodigoPaciente = lector.GetString("codigo_paciente"),
                Mascota = lector.GetString("mascota"),
                Dueno = lector.GetString("dueno"),
                Especie = lector.GetString("especie"),
                Telefono = lector.GetString("telefono")
            });
        }
        return lista;
    }

    public ExpedienteEncabezadoModel ObtenerEncabezado(long idMascota)
    {
        ExigirLecturaExpediente();
        const string sql = """
            SELECT m.id_mascota, m.id_dueno, m.codigo_paciente, m.nombre mascota,
                   m.especie, COALESCE(m.raza, '') raza, m.sexo, COALESCE(m.color, '') color,
                   m.fecha_nacimiento, m.peso_actual, m.estado_vital, COALESCE(m.microchip, '') microchip,
                   COALESCE(m.observaciones, '') observaciones, m.fecha_creacion,
                   d.nombre_completo dueno, d.telefono_principal telefono,
                   COALESCE((SELECT SUM(f.saldo_pendiente) FROM facturas f
                             WHERE f.id_mascota = m.id_mascota AND f.estado <> 'Anulada'), 0) saldo_pendiente,
                   (SELECT MIN(r.fecha_programada) FROM recordatorios r
                    WHERE r.id_mascota = m.id_mascota AND r.tipo_recordatorio = 'Vacuna'
                      AND r.estado IN ('Pendiente','Contactado') AND r.fecha_programada >= CURDATE()) proxima_vacuna
            FROM mascotas m
            INNER JOIN duenos d ON d.id_dueno = m.id_dueno
            WHERE m.id_mascota = @Mascota;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Mascota", MySqlDbType.UInt64).Value = idMascota;
        using MySqlDataReader lector = comando.ExecuteReader();
        if (!lector.Read())
        {
            throw new InvalidOperationException("No se encontró la mascota solicitada.");
        }
        ExpedienteEncabezadoModel resultado = new()
        {
            IdMascota = lector.GetInt64("id_mascota"),
            IdDueno = lector.GetInt64("id_dueno"),
            CodigoPaciente = lector.GetString("codigo_paciente"),
            Mascota = lector.GetString("mascota"),
            Especie = lector.GetString("especie"),
            Raza = lector.GetString("raza"),
            Sexo = lector.GetString("sexo"),
            Color = lector.GetString("color"),
            FechaNacimiento = FechaNullable(lector, "fecha_nacimiento"),
            PesoActual = DecimalNullable(lector, "peso_actual"),
            EstadoVital = lector.GetString("estado_vital"),
            Microchip = lector.GetString("microchip"),
            Observaciones = lector.GetString("observaciones"),
            FechaCreacion = lector.GetDateTime("fecha_creacion"),
            Dueno = lector.GetString("dueno"),
            Telefono = lector.GetString("telefono"),
            SaldoPendiente = lector.GetDecimal("saldo_pendiente"),
            ProximaVacuna = FechaNullable(lector, "proxima_vacuna")
        };
        lector.Close();
        resultado.AlertasActivas = ListarAlertasActivas(idMascota, conexion);
        return resultado;
    }

    public List<AlertaClinicaModel> ListarAlertasActivas(long idMascota)
    {
        ExigirLecturaExpediente();
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        return ListarAlertasActivas(idMascota, conexion);
    }

    private static List<AlertaClinicaModel> ListarAlertasActivas(long idMascota, MySqlConnection conexion)
    {
        const string sql = """
            SELECT a.id_alerta, a.id_mascota, a.tipo_alerta, a.descripcion, a.activa,
                   a.fecha_registro, u.nombre_completo usuario_registro
            FROM mascota_alertas_clinicas a
            INNER JOIN usuarios u ON u.id_usuario = a.id_usuario_registro
            WHERE a.id_mascota = @Mascota AND a.activa = 1
            ORDER BY a.fecha_registro DESC;
            """;
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Mascota", MySqlDbType.UInt64).Value = idMascota;
        using MySqlDataReader lector = comando.ExecuteReader();
        List<AlertaClinicaModel> lista = new();
        while (lector.Read())
        {
            lista.Add(new AlertaClinicaModel
            {
                IdAlerta = lector.GetInt64("id_alerta"), IdMascota = lector.GetInt64("id_mascota"),
                TipoAlerta = lector.GetString("tipo_alerta"), Descripcion = lector.GetString("descripcion"),
                Activa = lector.GetBoolean("activa"), FechaRegistro = lector.GetDateTime("fecha_registro"),
                UsuarioRegistro = lector.GetString("usuario_registro")
            });
        }
        return lista;
    }

    public List<ExpedienteConsultaResumenModel> ListarConsultas(long idMascota)
    {
        ExigirLecturaExpediente();
        const string sql = """
            SELECT c.id_consulta, c.fecha_atencion, v.nombre_completo veterinario,
                   c.motivo_consulta, COALESCE(c.estado_egreso, '') estado_egreso, c.peso,
                   COALESCE((SELECT cd.descripcion FROM consulta_diagnosticos cd
                             WHERE cd.id_consulta = c.id_consulta AND cd.es_principal = 1
                             ORDER BY cd.id_diagnostico LIMIT 1), '') diagnostico_principal
            FROM consultas c
            INNER JOIN veterinarios v ON v.id_veterinario = c.id_veterinario
            WHERE c.id_mascota = @Mascota
            ORDER BY c.fecha_atencion DESC;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Mascota", MySqlDbType.UInt64).Value = idMascota;
        using MySqlDataReader lector = comando.ExecuteReader();
        List<ExpedienteConsultaResumenModel> lista = new();
        while (lector.Read())
        {
            lista.Add(new ExpedienteConsultaResumenModel
            {
                IdConsulta = lector.GetInt64("id_consulta"), FechaAtencion = lector.GetDateTime("fecha_atencion"),
                Veterinario = lector.GetString("veterinario"), MotivoConsulta = lector.GetString("motivo_consulta"),
                DiagnosticoPrincipal = lector.GetString("diagnostico_principal"), EstadoEgreso = lector.GetString("estado_egreso"),
                Peso = DecimalNullable(lector, "peso")
            });
        }
        return lista;
    }

    public ExpedienteConsultaDetalleModel ObtenerConsultaDetalle(long idConsulta)
    {
        ExigirLecturaExpediente();
        const string sql = """
            SELECT c.id_consulta, c.fecha_atencion, v.nombre_completo veterinario, c.motivo_consulta,
                   COALESCE(c.anamnesis, '') anamnesis, c.peso, c.temperatura,
                   c.frecuencia_cardiaca, c.frecuencia_respiratoria, COALESCE(c.hidratacion, '') hidratacion,
                   COALESCE(c.hallazgos_fisicos, '') hallazgos_fisicos, COALESCE(c.pronostico, '') pronostico,
                   COALESCE(c.tratamiento_general, '') tratamiento_general, COALESCE(c.indicaciones, '') indicaciones,
                   c.proxima_revision, COALESCE(c.estado_egreso, '') estado_egreso
            FROM consultas c INNER JOIN veterinarios v ON v.id_veterinario = c.id_veterinario
            WHERE c.id_consulta = @Consulta;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Consulta", MySqlDbType.UInt64).Value = idConsulta;
        using MySqlDataReader lector = comando.ExecuteReader();
        if (!lector.Read()) throw new InvalidOperationException("No se encontró la consulta solicitada.");
        ExpedienteConsultaDetalleModel detalle = new()
        {
            IdConsulta = lector.GetInt64("id_consulta"), FechaAtencion = lector.GetDateTime("fecha_atencion"),
            Veterinario = lector.GetString("veterinario"), MotivoConsulta = lector.GetString("motivo_consulta"),
            Anamnesis = lector.GetString("anamnesis"), Peso = DecimalNullable(lector, "peso"),
            Temperatura = DecimalNullable(lector, "temperatura"), FrecuenciaCardiaca = EnteroNullable(lector, "frecuencia_cardiaca"),
            FrecuenciaRespiratoria = EnteroNullable(lector, "frecuencia_respiratoria"), Hidratacion = lector.GetString("hidratacion"),
            HallazgosFisicos = lector.GetString("hallazgos_fisicos"), Pronostico = lector.GetString("pronostico"),
            TratamientoGeneral = lector.GetString("tratamiento_general"), Indicaciones = lector.GetString("indicaciones"),
            ProximaRevision = FechaNullable(lector, "proxima_revision"), EstadoEgreso = lector.GetString("estado_egreso")
        };
        lector.Close();
        detalle.Diagnosticos = ListarDiagnosticosConsulta(idConsulta, conexion);
        detalle.Servicios = ListarServiciosConsulta(idConsulta, conexion);
        return detalle;
    }

    private static List<DiagnosticoModel> ListarDiagnosticosConsulta(long idConsulta, MySqlConnection conexion)
    {
        const string sql = """
            SELECT id_diagnostico, descripcion, es_principal, COALESCE(observaciones, '') observaciones
            FROM consulta_diagnosticos WHERE id_consulta = @Consulta
            ORDER BY es_principal DESC, id_diagnostico;
            """;
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Consulta", MySqlDbType.UInt64).Value = idConsulta;
        using MySqlDataReader lector = comando.ExecuteReader();
        List<DiagnosticoModel> lista = new();
        while (lector.Read())
        {
            lista.Add(new DiagnosticoModel
            {
                IdDiagnostico = lector.GetInt64("id_diagnostico"), Descripcion = lector.GetString("descripcion"),
                EsPrincipal = lector.GetBoolean("es_principal"), Observaciones = lector.GetString("observaciones")
            });
        }
        return lista;
    }

    private static List<ConsultaServicioModel> ListarServiciosConsulta(long idConsulta, MySqlConnection conexion)
    {
        const string sql = """
            SELECT cs.id_consulta_servicio, cs.id_servicio, s.nombre servicio, cs.descripcion,
                   cs.cantidad, cs.precio_unitario, cs.descuento, cs.subtotal, cs.genera_cargo
            FROM consulta_servicios cs
            INNER JOIN catalogo_servicios s ON s.id_servicio = cs.id_servicio
            WHERE cs.id_consulta = @Consulta ORDER BY cs.id_consulta_servicio;
            """;
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Consulta", MySqlDbType.UInt64).Value = idConsulta;
        using MySqlDataReader lector = comando.ExecuteReader();
        List<ConsultaServicioModel> lista = new();
        while (lector.Read())
        {
            lista.Add(new ConsultaServicioModel
            {
                IdConsultaServicio = lector.GetInt64("id_consulta_servicio"), IdServicio = lector.GetInt32("id_servicio"),
                Servicio = lector.GetString("servicio"), Descripcion = lector.GetString("descripcion"),
                Cantidad = lector.GetDecimal("cantidad"), PrecioUnitario = lector.GetDecimal("precio_unitario"),
                Descuento = lector.GetDecimal("descuento"), GeneraCargo = lector.GetBoolean("genera_cargo")
            });
        }
        return lista;
    }

    public List<DiagnosticoModel> ListarDiagnosticosMascota(long idMascota)
    {
        ExigirLecturaExpediente();
        const string sql = """
            SELECT cd.id_diagnostico, cd.descripcion, cd.es_principal,
                   CONCAT(DATE_FORMAT(c.fecha_atencion, '%d/%m/%Y'), ' - ', COALESCE(cd.observaciones, '')) observaciones
            FROM consulta_diagnosticos cd INNER JOIN consultas c ON c.id_consulta = cd.id_consulta
            WHERE c.id_mascota = @Mascota ORDER BY c.fecha_atencion DESC, cd.es_principal DESC;
            """;
        using MySqlConnection conexion = Database.CrearConexion(); conexion.Open();
        using MySqlCommand comando = new(sql, conexion); comando.Parameters.Add("@Mascota", MySqlDbType.UInt64).Value = idMascota;
        using MySqlDataReader lector = comando.ExecuteReader();
        List<DiagnosticoModel> lista = new();
        while (lector.Read()) lista.Add(new DiagnosticoModel { IdDiagnostico = lector.GetInt64("id_diagnostico"), Descripcion = lector.GetString("descripcion"), EsPrincipal = lector.GetBoolean("es_principal"), Observaciones = lector.GetString("observaciones") });
        return lista;
    }

    public List<ExpedienteRecetaResumenModel> ListarRecetas(long idMascota)
    {
        ExigirLecturaExpediente();
        const string sql = """
            SELECT r.id_receta, r.id_consulta, r.fecha_emision, v.nombre_completo veterinario,
                   COALESCE((SELECT cd.descripcion FROM consulta_diagnosticos cd WHERE cd.id_consulta = r.id_consulta AND cd.es_principal = 1 LIMIT 1), '') diagnostico,
                   (SELECT COUNT(*) FROM receta_detalles rd WHERE rd.id_receta = r.id_receta) cantidad
            FROM recetas r
            INNER JOIN consultas c ON c.id_consulta = r.id_consulta
            INNER JOIN veterinarios v ON v.id_veterinario = c.id_veterinario
            WHERE c.id_mascota = @Mascota ORDER BY r.fecha_emision DESC;
            """;
        using MySqlConnection conexion = Database.CrearConexion(); conexion.Open();
        using MySqlCommand comando = new(sql, conexion); comando.Parameters.Add("@Mascota", MySqlDbType.UInt64).Value = idMascota;
        using MySqlDataReader lector = comando.ExecuteReader(); List<ExpedienteRecetaResumenModel> lista = new();
        while (lector.Read()) lista.Add(new ExpedienteRecetaResumenModel { IdReceta = lector.GetInt64("id_receta"), IdConsulta = lector.GetInt64("id_consulta"), FechaEmision = lector.GetDateTime("fecha_emision"), Veterinario = lector.GetString("veterinario"), DiagnosticoPrincipal = lector.GetString("diagnostico"), CantidadMedicamentos = Convert.ToInt32(lector.GetInt64("cantidad")) });
        return lista;
    }

    public RecetaModel ObtenerReceta(long idReceta)
    {
        ExigirLecturaExpediente();
        const string cabeceraSql = """SELECT id_receta, id_consulta, fecha_emision, COALESCE(indicaciones_generales, '') indicaciones_generales FROM recetas WHERE id_receta = @Receta;""";
        using MySqlConnection conexion = Database.CrearConexion(); conexion.Open();
        using MySqlCommand cabecera = new(cabeceraSql, conexion); cabecera.Parameters.Add("@Receta", MySqlDbType.UInt64).Value = idReceta;
        using MySqlDataReader lector = cabecera.ExecuteReader(); if (!lector.Read()) throw new InvalidOperationException("No se encontró la receta solicitada.");
        RecetaModel receta = new() { IdReceta = lector.GetInt64("id_receta"), IdConsulta = lector.GetInt64("id_consulta"), FechaEmision = lector.GetDateTime("fecha_emision"), IndicacionesGenerales = lector.GetString("indicaciones_generales") };
        lector.Close();
        const string detalleSql = """
            SELECT rd.id_detalle, rd.id_medicamento, COALESCE(cm.nombre, '') medicamento, COALESCE(rd.medicamento_libre, '') medicamento_libre,
                   COALESCE(rd.presentacion, '') presentacion, COALESCE(rd.concentracion, '') concentracion, rd.dosis, rd.frecuencia,
                   rd.duracion, COALESCE(rd.cantidad, '') cantidad, COALESCE(rd.via_administracion, '') via, COALESCE(rd.indicaciones, '') indicaciones
            FROM receta_detalles rd LEFT JOIN catalogo_medicamentos cm ON cm.id_medicamento = rd.id_medicamento
            WHERE rd.id_receta = @Receta ORDER BY rd.id_detalle;
            """;
        using MySqlCommand detalle = new(detalleSql, conexion); detalle.Parameters.Add("@Receta", MySqlDbType.UInt64).Value = idReceta;
        using MySqlDataReader detalleReader = detalle.ExecuteReader();
        while (detalleReader.Read()) receta.Detalles.Add(new RecetaDetalleModel { IdDetalle = detalleReader.GetInt64("id_detalle"), IdMedicamento = detalleReader.IsDBNull(detalleReader.GetOrdinal("id_medicamento")) ? null : detalleReader.GetInt32("id_medicamento"), Medicamento = detalleReader.GetString("medicamento"), MedicamentoLibre = detalleReader.GetString("medicamento_libre"), Presentacion = detalleReader.GetString("presentacion"), Concentracion = detalleReader.GetString("concentracion"), Dosis = detalleReader.GetString("dosis"), Frecuencia = detalleReader.GetString("frecuencia"), Duracion = detalleReader.GetString("duracion"), Cantidad = detalleReader.GetString("cantidad"), ViaAdministracion = detalleReader.GetString("via"), Indicaciones = detalleReader.GetString("indicaciones") });
        return receta;
    }

    public List<ExpedienteVacunaModel> ListarVacunas(long idMascota)
    {
        ExigirLecturaExpediente();
        const string sql = """
            SELECT va.id_aplicacion, va.fecha_aplicacion, cv.nombre vacuna, va.dosis,
                   COALESCE(va.lote_texto, il.numero_lote, '') lote, COALESCE(va.laboratorio, '') laboratorio,
                   va.fecha_proxima_dosis, v.nombre_completo veterinario
            FROM vacunas_aplicadas va INNER JOIN catalogo_vacunas cv ON cv.id_vacuna = va.id_vacuna
            INNER JOIN veterinarios v ON v.id_veterinario = va.id_veterinario LEFT JOIN inventario_lotes il ON il.id_lote = va.id_lote_inventario
            WHERE va.id_mascota = @Mascota ORDER BY va.fecha_aplicacion DESC;
            """;
        using MySqlConnection conexion = Database.CrearConexion(); conexion.Open(); using MySqlCommand comando = new(sql, conexion); comando.Parameters.Add("@Mascota", MySqlDbType.UInt64).Value = idMascota;
        using MySqlDataReader lector = comando.ExecuteReader(); List<ExpedienteVacunaModel> lista = new();
        while (lector.Read()) lista.Add(new ExpedienteVacunaModel { IdAplicacion = lector.GetInt64("id_aplicacion"), FechaAplicacion = lector.GetDateTime("fecha_aplicacion"), Vacuna = lector.GetString("vacuna"), Dosis = lector.GetString("dosis"), Lote = lector.GetString("lote"), Laboratorio = lector.GetString("laboratorio"), ProximaDosis = FechaNullable(lector, "fecha_proxima_dosis"), Veterinario = lector.GetString("veterinario") });
        return lista;
    }

    public List<ExpedienteDesparasitacionModel> ListarDesparasitaciones(long idMascota)
    {
        ExigirLecturaExpediente();
        const string sql = """
            SELECT d.id_desparasitacion, d.fecha_aplicacion, cd.nombre producto, d.dosis, d.peso_referencia, d.fecha_proxima, v.nombre_completo veterinario
            FROM desparasitaciones d INNER JOIN catalogo_desparasitantes cd ON cd.id_desparasitante = d.id_desparasitante
            INNER JOIN veterinarios v ON v.id_veterinario = d.id_veterinario WHERE d.id_mascota = @Mascota ORDER BY d.fecha_aplicacion DESC;
            """;
        using MySqlConnection conexion = Database.CrearConexion(); conexion.Open(); using MySqlCommand comando = new(sql, conexion); comando.Parameters.Add("@Mascota", MySqlDbType.UInt64).Value = idMascota;
        using MySqlDataReader lector = comando.ExecuteReader(); List<ExpedienteDesparasitacionModel> lista = new();
        while (lector.Read()) lista.Add(new ExpedienteDesparasitacionModel { IdDesparasitacion = lector.GetInt64("id_desparasitacion"), FechaAplicacion = lector.GetDateTime("fecha_aplicacion"), Producto = lector.GetString("producto"), Dosis = lector.GetString("dosis"), PesoReferencia = DecimalNullable(lector, "peso_referencia"), ProximaAplicacion = FechaNullable(lector, "fecha_proxima"), Veterinario = lector.GetString("veterinario") });
        return lista;
    }

    public List<ExpedienteOrdenModel> ListarOrdenes(long idMascota)
    {
        ExigirLecturaExpediente();
        const string sql = """
            SELECT o.id_orden, o.fecha_solicitud, o.tipo_orden, o.nombre_estudio, o.estado,
                   o.fecha_resultado, COALESCE(o.resultado_texto, '') resultado, o.precio
            FROM ordenes_clinicas o INNER JOIN consultas c ON c.id_consulta = o.id_consulta
            WHERE c.id_mascota = @Mascota ORDER BY o.fecha_solicitud DESC;
            """;
        using MySqlConnection conexion = Database.CrearConexion(); conexion.Open(); using MySqlCommand comando = new(sql, conexion); comando.Parameters.Add("@Mascota", MySqlDbType.UInt64).Value = idMascota;
        using MySqlDataReader lector = comando.ExecuteReader(); List<ExpedienteOrdenModel> lista = new();
        while (lector.Read()) lista.Add(new ExpedienteOrdenModel { IdOrden = lector.GetInt64("id_orden"), FechaSolicitud = lector.GetDateTime("fecha_solicitud"), TipoOrden = lector.GetString("tipo_orden"), NombreEstudio = lector.GetString("nombre_estudio"), Estado = lector.GetString("estado"), FechaResultado = FechaNullable(lector, "fecha_resultado"), ResultadoTexto = lector.GetString("resultado"), Precio = lector.GetDecimal("precio") });
        return lista;
    }

    public List<ExpedienteCitaModel> ListarCitas(long idMascota)
    {
        ExigirLecturaExpediente();
        const string sql = """
            SELECT c.id_cita, c.fecha_hora_inicio, v.nombre_completo veterinario, s.nombre servicio, c.estado, c.motivo_consulta
            FROM citas c INNER JOIN veterinarios v ON v.id_veterinario = c.id_veterinario
            INNER JOIN catalogo_servicios s ON s.id_servicio = c.id_servicio
            WHERE c.id_mascota = @Mascota ORDER BY c.fecha_hora_inicio DESC;
            """;
        using MySqlConnection conexion = Database.CrearConexion(); conexion.Open(); using MySqlCommand comando = new(sql, conexion); comando.Parameters.Add("@Mascota", MySqlDbType.UInt64).Value = idMascota;
        using MySqlDataReader lector = comando.ExecuteReader(); List<ExpedienteCitaModel> lista = new();
        while (lector.Read()) lista.Add(new ExpedienteCitaModel { IdCita = lector.GetInt64("id_cita"), FechaHoraInicio = lector.GetDateTime("fecha_hora_inicio"), Veterinario = lector.GetString("veterinario"), Servicio = lector.GetString("servicio"), Estado = lector.GetString("estado"), MotivoConsulta = lector.GetString("motivo_consulta") });
        return lista;
    }

    public List<ExpedienteFacturaModel> ListarFacturas(long idMascota)
    {
        ExigirLecturaExpediente();
        const string sql = """
            SELECT id_factura, numero_factura, fecha_emision, total, total_pagado, saldo_pendiente, estado
            FROM facturas WHERE id_mascota = @Mascota ORDER BY fecha_emision DESC;
            """;
        using MySqlConnection conexion = Database.CrearConexion(); conexion.Open(); using MySqlCommand comando = new(sql, conexion); comando.Parameters.Add("@Mascota", MySqlDbType.UInt64).Value = idMascota;
        using MySqlDataReader lector = comando.ExecuteReader(); List<ExpedienteFacturaModel> lista = new();
        while (lector.Read()) lista.Add(new ExpedienteFacturaModel { IdFactura = lector.GetInt64("id_factura"), NumeroFactura = lector.GetString("numero_factura"), FechaEmision = lector.GetDateTime("fecha_emision"), Total = lector.GetDecimal("total"), TotalPagado = lector.GetDecimal("total_pagado"), SaldoPendiente = lector.GetDecimal("saldo_pendiente"), Estado = lector.GetString("estado") });
        return lista;
    }

    public List<ExpedienteHospitalizacionModel> ListarHospitalizaciones(long idMascota)
    {
        ExigirLecturaExpediente();
        const string sql = """
            SELECT h.id_hospitalizacion, h.fecha_hora_ingreso, h.fecha_hora_alta, v.nombre_completo veterinario, h.motivo, h.estado
            FROM hospitalizaciones h INNER JOIN veterinarios v ON v.id_veterinario = h.id_veterinario
            WHERE h.id_mascota = @Mascota ORDER BY h.fecha_hora_ingreso DESC;
            """;
        using MySqlConnection conexion = Database.CrearConexion(); conexion.Open(); using MySqlCommand comando = new(sql, conexion); comando.Parameters.Add("@Mascota", MySqlDbType.UInt64).Value = idMascota;
        using MySqlDataReader lector = comando.ExecuteReader(); List<ExpedienteHospitalizacionModel> lista = new();
        while (lector.Read()) lista.Add(new ExpedienteHospitalizacionModel { IdHospitalizacion = lector.GetInt64("id_hospitalizacion"), FechaIngreso = lector.GetDateTime("fecha_hora_ingreso"), FechaAlta = FechaNullable(lector, "fecha_hora_alta"), Veterinario = lector.GetString("veterinario"), Motivo = lector.GetString("motivo"), Estado = lector.GetString("estado") });
        return lista;
    }

    public long ObtenerIdMascotaPorConsulta(long idConsulta)
    {
        ExigirLecturaExpediente();
        const string sql = "SELECT id_mascota FROM consultas WHERE id_consulta = @Consulta;";
        using MySqlConnection conexion = Database.CrearConexion(); conexion.Open();
        using MySqlCommand comando = new(sql, conexion); comando.Parameters.Add("@Consulta", MySqlDbType.UInt64).Value = idConsulta;
        object? resultado = comando.ExecuteScalar();
        if (resultado is null || resultado == DBNull.Value) throw new InvalidOperationException("La consulta no existe.");
        return Convert.ToInt64(resultado);
    }

    public List<LineaTiempoExpedienteModel> ListarLineaTiempo(long idMascota)
    {
        ExpedienteEncabezadoModel paciente = ObtenerEncabezado(idMascota);
        List<LineaTiempoExpedienteModel> eventos = new();
        foreach (ExpedienteCitaModel cita in ListarCitas(idMascota)) eventos.Add(new LineaTiempoExpedienteModel { Fecha = cita.FechaHoraInicio, Tipo = "Cita", Descripcion = $"{cita.Servicio}: {cita.MotivoConsulta}", ProfesionalEstado = $"{cita.Veterinario} - {cita.Estado}" });
        foreach (ExpedienteConsultaResumenModel consulta in ListarConsultas(idMascota)) eventos.Add(new LineaTiempoExpedienteModel { Fecha = consulta.FechaAtencion, Tipo = "Consulta", Descripcion = string.IsNullOrWhiteSpace(consulta.DiagnosticoPrincipal) ? consulta.MotivoConsulta : consulta.DiagnosticoPrincipal, ProfesionalEstado = consulta.Veterinario });
        foreach (ExpedienteRecetaResumenModel receta in ListarRecetas(idMascota)) eventos.Add(new LineaTiempoExpedienteModel { Fecha = receta.FechaEmision, Tipo = "Receta", Descripcion = $"Receta emitida ({receta.CantidadMedicamentos} medicamento(s))", ProfesionalEstado = receta.Veterinario });
        foreach (ExpedienteVacunaModel vacuna in ListarVacunas(idMascota)) eventos.Add(new LineaTiempoExpedienteModel { Fecha = vacuna.FechaAplicacion, Tipo = "Vacuna", Descripcion = vacuna.Vacuna, ProfesionalEstado = vacuna.Veterinario });
        foreach (ExpedienteDesparasitacionModel item in ListarDesparasitaciones(idMascota)) eventos.Add(new LineaTiempoExpedienteModel { Fecha = item.FechaAplicacion, Tipo = "Desparasitación", Descripcion = item.Producto, ProfesionalEstado = item.Veterinario });
        foreach (ExpedienteOrdenModel orden in ListarOrdenes(idMascota)) eventos.Add(new LineaTiempoExpedienteModel { Fecha = orden.FechaSolicitud, Tipo = "Orden clínica", Descripcion = orden.NombreEstudio, ProfesionalEstado = orden.Estado });
        foreach (ExpedienteHospitalizacionModel hospitalizacion in ListarHospitalizaciones(idMascota)) eventos.Add(new LineaTiempoExpedienteModel { Fecha = hospitalizacion.FechaIngreso, Tipo = "Hospitalización", Descripcion = hospitalizacion.Motivo, ProfesionalEstado = hospitalizacion.Estado });
        eventos.Add(new LineaTiempoExpedienteModel { Fecha = paciente.FechaCreacion, Tipo = "Registro", Descripcion = $"Registro inicial del paciente {paciente.Mascota}.", ProfesionalEstado = paciente.EstadoVital });
        return eventos.OrderByDescending(x => x.Fecha).ToList();
    }

    private static DateTime? FechaNullable(MySqlDataReader lector, string columna) => lector.IsDBNull(lector.GetOrdinal(columna)) ? null : lector.GetDateTime(columna);
    private static decimal? DecimalNullable(MySqlDataReader lector, string columna) => lector.IsDBNull(lector.GetOrdinal(columna)) ? null : lector.GetDecimal(columna);
    private static int? EnteroNullable(MySqlDataReader lector, string columna) => lector.IsDBNull(lector.GetOrdinal(columna)) ? null : lector.GetInt32(columna);
    private static void ExigirLecturaExpediente() => SesionActual.ExigirRoles("Administrador", "Recepción", "Veterinario");
}
