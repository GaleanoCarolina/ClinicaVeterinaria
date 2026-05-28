using System;
using System.Collections.Generic;
using System.Linq;
using ClinicaVeterinaria.Data;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Utils;
using MySql.Data.MySqlClient;

namespace ClinicaVeterinaria.Services;

public sealed class ConsultaService
{
    private readonly TarifaService _tarifaService = new();

    public List<CitaModel> ListarCitasEnConsulta()
    {
        int idVeterinario = ExigirVeterinarioAsociado();
        const string sql = """
            SELECT c.id_cita, c.id_mascota, m.codigo_paciente, m.nombre mascota,
                   d.nombre_completo dueno, d.telefono_principal, c.id_veterinario,
                   v.nombre_completo veterinario, c.id_servicio, s.nombre servicio,
                   c.fecha_hora_inicio, c.duracion_minutos, c.fecha_hora_fin, c.motivo_consulta,
                   COALESCE(c.observaciones_recepcion, '') observaciones_recepcion, c.estado,
                   c.fecha_llegada,
                   (SELECT COUNT(*) FROM mascota_alertas_clinicas a WHERE a.id_mascota = m.id_mascota AND a.activa = 1) alertas,
                   COALESCE((SELECT SUM(f.saldo_pendiente) FROM facturas f WHERE f.id_mascota = m.id_mascota AND f.estado NOT IN ('Pagada','Anulada')), 0) saldo
            FROM citas c
            INNER JOIN mascotas m ON m.id_mascota = c.id_mascota
            INNER JOIN duenos d ON d.id_dueno = m.id_dueno
            INNER JOIN veterinarios v ON v.id_veterinario = c.id_veterinario
            INNER JOIN catalogo_servicios s ON s.id_servicio = c.id_servicio
            WHERE c.id_veterinario = @Veterinario AND c.estado = 'En consulta'
              AND NOT EXISTS (SELECT 1 FROM consultas co WHERE co.id_cita = c.id_cita)
            ORDER BY c.fecha_hora_inicio;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Veterinario", MySqlDbType.Int32).Value = idVeterinario;
        using MySqlDataReader lector = comando.ExecuteReader();
        List<CitaModel> citas = new();
        while (lector.Read())
        {
            citas.Add(new CitaModel
            {
                IdCita = lector.GetInt64("id_cita"), IdMascota = lector.GetInt64("id_mascota"), CodigoPaciente = lector.GetString("codigo_paciente"),
                Mascota = lector.GetString("mascota"), Dueno = lector.GetString("dueno"), TelefonoDueno = lector.GetString("telefono_principal"),
                IdVeterinario = lector.GetInt32("id_veterinario"), Veterinario = lector.GetString("veterinario"), IdServicio = lector.GetInt32("id_servicio"),
                Servicio = lector.GetString("servicio"), FechaHoraInicio = lector.GetDateTime("fecha_hora_inicio"), DuracionMinutos = lector.GetInt32("duracion_minutos"),
                FechaHoraFin = lector.GetDateTime("fecha_hora_fin"), MotivoConsulta = lector.GetString("motivo_consulta"),
                ObservacionesRecepcion = lector.GetString("observaciones_recepcion"), Estado = lector.GetString("estado"),
                FechaLlegada = lector.IsDBNull(lector.GetOrdinal("fecha_llegada")) ? null : lector.GetDateTime("fecha_llegada"),
                AlertasActivas = lector.GetInt32("alertas"), SaldoPendiente = lector.GetDecimal("saldo")
            });
        }
        return citas;
    }

    public AtencionEncabezadoModel ObtenerEncabezado(long idCita)
    {
        int idVeterinario = ExigirVeterinarioAsociado();
        const string sql = """
            SELECT c.id_cita, c.id_mascota, c.id_veterinario, c.id_servicio,
                   s.nombre servicio_cita, s.precio_base precio_servicio_cita,
                   m.codigo_paciente, m.nombre mascota, m.especie, COALESCE(m.raza, '') raza,
                   m.sexo, m.fecha_nacimiento, m.peso_actual, COALESCE(m.ruta_foto, '') ruta_foto,
                   d.nombre_completo dueno, d.telefono_principal, v.nombre_completo veterinario,
                   c.fecha_hora_inicio, c.motivo_consulta,
                   COALESCE((SELECT GROUP_CONCAT(CONCAT(a.tipo_alerta, ': ', a.descripcion) SEPARATOR ' | ')
                             FROM mascota_alertas_clinicas a WHERE a.id_mascota = m.id_mascota AND a.activa = 1), '') alertas,
                   (SELECT COUNT(*) FROM mascota_alertas_clinicas a WHERE a.id_mascota = m.id_mascota AND a.activa = 1) alertas_activas
            FROM citas c
            INNER JOIN mascotas m ON m.id_mascota = c.id_mascota
            INNER JOIN duenos d ON d.id_dueno = m.id_dueno
            INNER JOIN veterinarios v ON v.id_veterinario = c.id_veterinario
            INNER JOIN catalogo_servicios s ON s.id_servicio = c.id_servicio
            WHERE c.id_cita = @Cita AND c.id_veterinario = @Veterinario AND c.estado = 'En consulta'
              AND NOT EXISTS (SELECT 1 FROM consultas co WHERE co.id_cita = c.id_cita);
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Cita", MySqlDbType.UInt64).Value = idCita;
        comando.Parameters.Add("@Veterinario", MySqlDbType.Int32).Value = idVeterinario;
        using MySqlDataReader lector = comando.ExecuteReader();
        if (!lector.Read())
        {
            throw new InvalidOperationException("La cita no está disponible para atención o no pertenece al veterinario actual.");
        }
        AtencionEncabezadoModel encabezado = new AtencionEncabezadoModel
        {
            IdCita = lector.GetInt64("id_cita"), IdMascota = lector.GetInt64("id_mascota"), IdVeterinario = lector.GetInt32("id_veterinario"),
            IdServicioCita = lector.GetInt32("id_servicio"), ServicioCita = lector.GetString("servicio_cita"), PrecioServicioCita = lector.GetDecimal("precio_servicio_cita"),
            CodigoPaciente = lector.GetString("codigo_paciente"), Mascota = lector.GetString("mascota"), Especie = lector.GetString("especie"),
            Raza = lector.GetString("raza"), Sexo = lector.GetString("sexo"), FechaNacimiento = lector.IsDBNull(lector.GetOrdinal("fecha_nacimiento")) ? null : lector.GetDateTime("fecha_nacimiento"),
            PesoAnterior = lector.IsDBNull(lector.GetOrdinal("peso_actual")) ? null : lector.GetDecimal("peso_actual"), RutaFoto = lector.GetString("ruta_foto"),
            Dueno = lector.GetString("dueno"), Telefono = lector.GetString("telefono_principal"), Veterinario = lector.GetString("veterinario"),
            FechaHoraCita = lector.GetDateTime("fecha_hora_inicio"), MotivoConsulta = lector.GetString("motivo_consulta"),
            Alertas = lector.GetString("alertas"), AlertasActivas = lector.GetInt32("alertas_activas")
        };
        TarifaCalculadaModel tarifa = _tarifaService.CalcularPrecioServicio(encabezado.PrecioServicioCita, encabezado.Especie);
        encabezado.PrecioServicioBase = tarifa.PrecioBase;
        encabezado.PrecioServicioCita = tarifa.PrecioFinal;
        encabezado.TipoTarifa = tarifa.TipoTarifa;
        encabezado.DetalleTarifa = tarifa.Explicacion;
        return encabezado;
    }

    public List<ServicioModel> ListarServiciosClinicos(string especie)
    {
        ExigirVeterinarioAsociado();
        const string sql = """
            SELECT id_servicio, codigo, nombre, precio_base, duracion_minutos, genera_cargo, activo
            FROM catalogo_servicios WHERE activo = 1 ORDER BY nombre;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        using MySqlDataReader lector = comando.ExecuteReader();
        List<ServicioModel> lista = new();
        while (lector.Read())
        {
            lista.Add(new ServicioModel
            {
                IdServicio = lector.GetInt32("id_servicio"), Codigo = lector.GetString("codigo"), Nombre = lector.GetString("nombre"),
                PrecioBase = _tarifaService.CalcularPrecioServicio(lector.GetDecimal("precio_base"), especie).PrecioFinal,
                DuracionMinutos = lector.GetInt32("duracion_minutos"),
                GeneraCargo = lector.GetBoolean("genera_cargo"), Activo = lector.GetBoolean("activo")
            });
        }
        return lista;
    }

    public long FinalizarConsulta(ConsultaCierreModel cierre)
    {
        int idVeterinario = ExigirVeterinarioAsociado();
        ValidarCierre(cierre, idVeterinario);
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlTransaction tx = conexion.BeginTransaction();
        try
        {
            long idDueno = ValidarCitaParaCierre(conexion, tx, cierre.Consulta, idVeterinario);
            long idConsulta = InsertarConsulta(conexion, tx, cierre.Consulta);
            ActualizarPesoMascota(conexion, tx, cierre.Consulta);
            InsertarDiagnosticos(conexion, tx, idConsulta, cierre.Diagnosticos);
            InsertarServicios(conexion, tx, idConsulta, idDueno, cierre.Consulta, cierre.Servicios);
            if (cierre.Receta is { Detalles.Count: > 0 }) InsertarReceta(conexion, tx, idConsulta, cierre.Receta);
            InsertarVacunas(conexion, tx, idConsulta, idDueno, cierre.Consulta, cierre.Vacunas);
            InsertarDesparasitaciones(conexion, tx, idConsulta, idDueno, cierre.Consulta, cierre.Desparasitaciones);
            InsertarOrdenes(conexion, tx, idConsulta, idDueno, cierre.Consulta, cierre.Ordenes);
            CrearRecordatorioRevision(conexion, tx, cierre.Consulta);
            FinalizarCita(conexion, tx, cierre.Consulta.IdCita);
            tx.Commit();
            return idConsulta;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static long ValidarCitaParaCierre(MySqlConnection conexion, MySqlTransaction tx, ConsultaModel consulta, int idVeterinario)
    {
        const string sql = """
            SELECT m.id_dueno, c.id_mascota, c.id_veterinario, c.estado
            FROM citas c INNER JOIN mascotas m ON m.id_mascota = c.id_mascota
            WHERE c.id_cita = @Cita FOR UPDATE;
            """;
        using MySqlCommand comando = new(sql, conexion, tx);
        comando.Parameters.Add("@Cita", MySqlDbType.UInt64).Value = consulta.IdCita;
        using MySqlDataReader lector = comando.ExecuteReader();
        if (!lector.Read()) throw new InvalidOperationException("La cita solicitada no existe.");
        long idDueno = lector.GetInt64("id_dueno");
        long mascota = lector.GetInt64("id_mascota");
        int veterinario = lector.GetInt32("id_veterinario");
        string estado = lector.GetString("estado");
        lector.Close();
        if (mascota != consulta.IdMascota || veterinario != idVeterinario || consulta.IdVeterinario != idVeterinario)
            throw new UnauthorizedAccessException("La cita no pertenece al veterinario autorizado.");
        if (!string.Equals(estado, "En consulta", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La cita debe encontrarse en estado En consulta para finalizarse.");
        using MySqlCommand verificar = new("SELECT COUNT(*) FROM consultas WHERE id_cita = @Cita;", conexion, tx);
        verificar.Parameters.Add("@Cita", MySqlDbType.UInt64).Value = consulta.IdCita;
        if (Convert.ToInt32(verificar.ExecuteScalar()) > 0)
            throw new InvalidOperationException("Esta cita ya cuenta con una consulta clínica finalizada.");
        return idDueno;
    }

    private static long InsertarConsulta(MySqlConnection conexion, MySqlTransaction tx, ConsultaModel c)
    {
        const string sql = """
            INSERT INTO consultas
            (id_cita, id_mascota, id_veterinario, fecha_atencion, motivo_consulta, anamnesis, peso, temperatura,
             frecuencia_cardiaca, frecuencia_respiratoria, hidratacion, hallazgos_fisicos, pronostico,
             tratamiento_general, indicaciones, proxima_revision, estado_egreso, id_usuario_creacion)
            VALUES
            (@Cita, @Mascota, @Veterinario, CURRENT_TIMESTAMP, @Motivo, @Anamnesis, @Peso, @Temperatura,
             @FC, @FR, @Hidratacion, @Hallazgos, @Pronostico, @Tratamiento, @Indicaciones, @Revision, @Egreso, @Usuario);
            """;
        using MySqlCommand comando = new(sql, conexion, tx);
        comando.Parameters.Add("@Cita", MySqlDbType.UInt64).Value = c.IdCita;
        comando.Parameters.Add("@Mascota", MySqlDbType.UInt64).Value = c.IdMascota;
        comando.Parameters.Add("@Veterinario", MySqlDbType.Int32).Value = c.IdVeterinario;
        comando.Parameters.Add("@Motivo", MySqlDbType.VarChar).Value = c.MotivoConsulta.Trim();
        AgregarTexto(comando, "@Anamnesis", c.Anamnesis);
        AgregarDecimal(comando, "@Peso", c.Peso);
        AgregarDecimal(comando, "@Temperatura", c.Temperatura);
        AgregarEntero(comando, "@FC", c.FrecuenciaCardiaca);
        AgregarEntero(comando, "@FR", c.FrecuenciaRespiratoria);
        AgregarTexto(comando, "@Hidratacion", c.Hidratacion);
        AgregarTexto(comando, "@Hallazgos", c.HallazgosFisicos);
        AgregarTexto(comando, "@Pronostico", c.Pronostico);
        AgregarTexto(comando, "@Tratamiento", c.TratamientoGeneral);
        AgregarTexto(comando, "@Indicaciones", c.Indicaciones);
        comando.Parameters.Add("@Revision", MySqlDbType.DateTime).Value = c.ProximaRevision.HasValue ? c.ProximaRevision.Value : DBNull.Value;
        AgregarTexto(comando, "@Egreso", c.EstadoEgreso);
        comando.Parameters.Add("@Usuario", MySqlDbType.Int32).Value = SesionActual.Usuario!.IdUsuario;
        comando.ExecuteNonQuery();
        return comando.LastInsertedId;
    }

    private static void ActualizarPesoMascota(MySqlConnection conexion, MySqlTransaction tx, ConsultaModel c)
    {
        if (!c.Peso.HasValue) return;
        using MySqlCommand comando = new("UPDATE mascotas SET peso_actual = @Peso WHERE id_mascota = @Mascota;", conexion, tx);
        comando.Parameters.Add("@Peso", MySqlDbType.Decimal).Value = c.Peso.Value;
        comando.Parameters.Add("@Mascota", MySqlDbType.UInt64).Value = c.IdMascota;
        comando.ExecuteNonQuery();
    }

    private static void InsertarDiagnosticos(MySqlConnection conexion, MySqlTransaction tx, long idConsulta, IEnumerable<DiagnosticoModel> diagnosticos)
    {
        const string sql = """
            INSERT INTO consulta_diagnosticos (id_consulta, descripcion, es_principal, observaciones)
            VALUES (@Consulta, @Descripcion, @Principal, @Observaciones);
            """;
        foreach (DiagnosticoModel d in diagnosticos)
        {
            using MySqlCommand comando = new(sql, conexion, tx);
            comando.Parameters.Add("@Consulta", MySqlDbType.UInt64).Value = idConsulta;
            comando.Parameters.Add("@Descripcion", MySqlDbType.VarChar).Value = d.Descripcion.Trim();
            comando.Parameters.Add("@Principal", MySqlDbType.Bit).Value = d.EsPrincipal;
            AgregarTexto(comando, "@Observaciones", d.Observaciones);
            comando.ExecuteNonQuery();
        }
    }

    private static void InsertarServicios(MySqlConnection conexion, MySqlTransaction tx, long idConsulta, long idDueno, ConsultaModel consulta, IEnumerable<ConsultaServicioModel> servicios)
    {
        const string sql = """
            INSERT INTO consulta_servicios
            (id_consulta, id_servicio, descripcion, cantidad, precio_unitario, descuento, subtotal, genera_cargo, facturado)
            VALUES (@Consulta, @Servicio, @Descripcion, @Cantidad, @Precio, @Descuento, @Subtotal, @GeneraCargo, 0);
            """;
        foreach (ConsultaServicioModel s in servicios)
        {
            using MySqlCommand comando = new(sql, conexion, tx);
            comando.Parameters.Add("@Consulta", MySqlDbType.UInt64).Value = idConsulta;
            comando.Parameters.Add("@Servicio", MySqlDbType.Int32).Value = s.IdServicio;
            comando.Parameters.Add("@Descripcion", MySqlDbType.VarChar).Value = s.Descripcion.Trim();
            comando.Parameters.Add("@Cantidad", MySqlDbType.Decimal).Value = s.Cantidad;
            comando.Parameters.Add("@Precio", MySqlDbType.Decimal).Value = s.PrecioUnitario;
            comando.Parameters.Add("@Descuento", MySqlDbType.Decimal).Value = s.Descuento;
            comando.Parameters.Add("@Subtotal", MySqlDbType.Decimal).Value = s.Subtotal;
            comando.Parameters.Add("@GeneraCargo", MySqlDbType.Bit).Value = s.GeneraCargo;
            comando.ExecuteNonQuery();
            long referencia = comando.LastInsertedId;
            if (s.GeneraCargo && s.Subtotal > 0)
                CrearCargo(conexion, tx, idDueno, consulta.IdMascota, idConsulta, consulta.IdCita, "Servicio", referencia, s.Descripcion, s.Cantidad, s.PrecioUnitario, s.Descuento, s.Subtotal);
        }
    }

    private static void InsertarReceta(MySqlConnection conexion, MySqlTransaction tx, long idConsulta, RecetaModel receta)
    {
        const string cabecera = """
            INSERT INTO recetas (id_consulta, fecha_emision, indicaciones_generales, id_usuario_creacion)
            VALUES (@Consulta, CURRENT_TIMESTAMP, @Indicaciones, @Usuario);
            """;
        using MySqlCommand cmdReceta = new(cabecera, conexion, tx);
        cmdReceta.Parameters.Add("@Consulta", MySqlDbType.UInt64).Value = idConsulta;
        AgregarTexto(cmdReceta, "@Indicaciones", receta.IndicacionesGenerales);
        cmdReceta.Parameters.Add("@Usuario", MySqlDbType.Int32).Value = SesionActual.Usuario!.IdUsuario;
        cmdReceta.ExecuteNonQuery();
        long idReceta = cmdReceta.LastInsertedId;
        const string detalleSql = """
            INSERT INTO receta_detalles
            (id_receta, id_medicamento, medicamento_libre, presentacion, concentracion, dosis, frecuencia, duracion, cantidad, via_administracion, indicaciones)
            VALUES (@Receta, @Medicamento, @Libre, @Presentacion, @Concentracion, @Dosis, @Frecuencia, @Duracion, @Cantidad, @Via, @Indicaciones);
            """;
        foreach (RecetaDetalleModel d in receta.Detalles)
        {
            using MySqlCommand comando = new(detalleSql, conexion, tx);
            comando.Parameters.Add("@Receta", MySqlDbType.UInt64).Value = idReceta;
            comando.Parameters.Add("@Medicamento", MySqlDbType.Int32).Value = d.IdMedicamento.HasValue ? d.IdMedicamento.Value : DBNull.Value;
            AgregarTexto(comando, "@Libre", d.MedicamentoLibre);
            AgregarTexto(comando, "@Presentacion", d.Presentacion);
            AgregarTexto(comando, "@Concentracion", d.Concentracion);
            comando.Parameters.Add("@Dosis", MySqlDbType.VarChar).Value = d.Dosis.Trim();
            comando.Parameters.Add("@Frecuencia", MySqlDbType.VarChar).Value = d.Frecuencia.Trim();
            comando.Parameters.Add("@Duracion", MySqlDbType.VarChar).Value = d.Duracion.Trim();
            AgregarTexto(comando, "@Cantidad", d.Cantidad);
            AgregarTexto(comando, "@Via", d.ViaAdministracion);
            AgregarTexto(comando, "@Indicaciones", d.Indicaciones);
            comando.ExecuteNonQuery();
        }
    }

    private static void InsertarVacunas(MySqlConnection conexion, MySqlTransaction tx, long idConsulta, long idDueno, ConsultaModel consulta, IEnumerable<VacunaAplicadaModel> vacunas)
    {
        foreach (VacunaAplicadaModel vacuna in vacunas)
        {
            (bool controla, long? producto, string nombre) = ObtenerVacuna(conexion, tx, vacuna.IdVacuna);
            if (controla && producto.HasValue)
                ValidarDescontarLote(conexion, tx, producto.Value, vacuna.IdLoteInventario, vacuna.FechaAplicacion, idConsulta, true, null);
            const string sql = """
                INSERT INTO vacunas_aplicadas
                (id_mascota, id_consulta, id_vacuna, id_lote_inventario, lote_texto, laboratorio, fecha_vencimiento_lote, dosis,
                 fecha_aplicacion, fecha_proxima_dosis, observaciones, id_veterinario, precio_aplicado, facturado)
                VALUES (@Mascota, @Consulta, @Vacuna, @Lote, @LoteTexto, @Laboratorio, @Vencimiento, @Dosis,
                        @Aplicacion, @Proxima, @Observaciones, @Veterinario, @Precio, 0);
                """;
            using MySqlCommand comando = new(sql, conexion, tx);
            comando.Parameters.Add("@Mascota", MySqlDbType.UInt64).Value = consulta.IdMascota;
            comando.Parameters.Add("@Consulta", MySqlDbType.UInt64).Value = idConsulta;
            comando.Parameters.Add("@Vacuna", MySqlDbType.Int32).Value = vacuna.IdVacuna;
            comando.Parameters.Add("@Lote", MySqlDbType.UInt64).Value = vacuna.IdLoteInventario.HasValue ? vacuna.IdLoteInventario.Value : DBNull.Value;
            AgregarTexto(comando, "@LoteTexto", vacuna.LoteTexto);
            AgregarTexto(comando, "@Laboratorio", vacuna.Laboratorio);
            comando.Parameters.Add("@Vencimiento", MySqlDbType.Date).Value = vacuna.FechaVencimientoLote.HasValue ? vacuna.FechaVencimientoLote.Value.Date : DBNull.Value;
            comando.Parameters.Add("@Dosis", MySqlDbType.VarChar).Value = vacuna.Dosis.Trim();
            comando.Parameters.Add("@Aplicacion", MySqlDbType.DateTime).Value = vacuna.FechaAplicacion;
            comando.Parameters.Add("@Proxima", MySqlDbType.Date).Value = vacuna.FechaProximaDosis.HasValue ? vacuna.FechaProximaDosis.Value.Date : DBNull.Value;
            AgregarTexto(comando, "@Observaciones", vacuna.Observaciones);
            comando.Parameters.Add("@Veterinario", MySqlDbType.Int32).Value = consulta.IdVeterinario;
            comando.Parameters.Add("@Precio", MySqlDbType.Decimal).Value = vacuna.PrecioAplicado;
            comando.ExecuteNonQuery();
            long idAplicacion = comando.LastInsertedId;
            if (controla && producto.HasValue && vacuna.IdLoteInventario.HasValue)
                InsertarMovimiento(conexion, tx, producto.Value, vacuna.IdLoteInventario.Value, "Salida por vacuna aplicada", idConsulta, idAplicacion, $"Vacuna aplicada: {nombre}");
            if (vacuna.PrecioAplicado > 0)
                CrearCargo(conexion, tx, idDueno, consulta.IdMascota, idConsulta, consulta.IdCita, "Vacuna", idAplicacion, nombre, 1, vacuna.PrecioAplicado, 0, vacuna.PrecioAplicado);
            if (vacuna.FechaProximaDosis.HasValue)
                CrearRecordatorio(conexion, tx, consulta.IdMascota, "Vacuna", vacuna.FechaProximaDosis.Value, $"Próxima dosis de {nombre}.");
        }
    }

    private static void InsertarDesparasitaciones(MySqlConnection conexion, MySqlTransaction tx, long idConsulta, long idDueno, ConsultaModel consulta, IEnumerable<DesparasitacionModel> items)
    {
        foreach (DesparasitacionModel item in items)
        {
            (bool controla, long? producto, string nombre) = ObtenerDesparasitante(conexion, tx, item.IdDesparasitante);
            if (controla && producto.HasValue)
                ValidarDescontarLote(conexion, tx, producto.Value, item.IdLoteInventario, item.FechaAplicacion, idConsulta, false, null);
            const string sql = """
                INSERT INTO desparasitaciones
                (id_mascota, id_consulta, id_desparasitante, id_lote_inventario, dosis, peso_referencia, fecha_aplicacion,
                 fecha_proxima, observaciones, id_veterinario, precio_aplicado, facturado)
                VALUES (@Mascota, @Consulta, @Item, @Lote, @Dosis, @Peso, @Aplicacion, @Proxima, @Observaciones, @Veterinario, @Precio, 0);
                """;
            using MySqlCommand comando = new(sql, conexion, tx);
            comando.Parameters.Add("@Mascota", MySqlDbType.UInt64).Value = consulta.IdMascota;
            comando.Parameters.Add("@Consulta", MySqlDbType.UInt64).Value = idConsulta;
            comando.Parameters.Add("@Item", MySqlDbType.Int32).Value = item.IdDesparasitante;
            comando.Parameters.Add("@Lote", MySqlDbType.UInt64).Value = item.IdLoteInventario.HasValue ? item.IdLoteInventario.Value : DBNull.Value;
            comando.Parameters.Add("@Dosis", MySqlDbType.VarChar).Value = item.Dosis.Trim();
            AgregarDecimal(comando, "@Peso", item.PesoReferencia);
            comando.Parameters.Add("@Aplicacion", MySqlDbType.DateTime).Value = item.FechaAplicacion;
            comando.Parameters.Add("@Proxima", MySqlDbType.Date).Value = item.FechaProxima.HasValue ? item.FechaProxima.Value.Date : DBNull.Value;
            AgregarTexto(comando, "@Observaciones", item.Observaciones);
            comando.Parameters.Add("@Veterinario", MySqlDbType.Int32).Value = consulta.IdVeterinario;
            comando.Parameters.Add("@Precio", MySqlDbType.Decimal).Value = item.PrecioAplicado;
            comando.ExecuteNonQuery();
            long idDesparasitacion = comando.LastInsertedId;
            if (controla && producto.HasValue && item.IdLoteInventario.HasValue)
                InsertarMovimiento(conexion, tx, producto.Value, item.IdLoteInventario.Value, "Salida por consulta", idConsulta, null, $"Desparasitación aplicada: {nombre}");
            if (item.PrecioAplicado > 0)
                CrearCargo(conexion, tx, idDueno, consulta.IdMascota, idConsulta, consulta.IdCita, "Desparasitante", idDesparasitacion, nombre, 1, item.PrecioAplicado, 0, item.PrecioAplicado);
            if (item.FechaProxima.HasValue)
                CrearRecordatorio(conexion, tx, consulta.IdMascota, "Desparasitación", item.FechaProxima.Value, $"Próxima desparasitación con {nombre}.");
        }
    }

    private static void InsertarOrdenes(MySqlConnection conexion, MySqlTransaction tx, long idConsulta, long idDueno, ConsultaModel consulta, IEnumerable<OrdenClinicaModel> ordenes)
    {
        const string sql = """
            INSERT INTO ordenes_clinicas
            (id_consulta, tipo_orden, nombre_estudio, motivo, observaciones, estado, precio, facturado, fecha_solicitud, id_veterinario)
            VALUES (@Consulta, @Tipo, @Estudio, @Motivo, @Observaciones, 'Solicitada', @Precio, 0, CURRENT_TIMESTAMP, @Veterinario);
            """;
        foreach (OrdenClinicaModel item in ordenes)
        {
            using MySqlCommand comando = new(sql, conexion, tx);
            comando.Parameters.Add("@Consulta", MySqlDbType.UInt64).Value = idConsulta;
            comando.Parameters.Add("@Tipo", MySqlDbType.VarChar).Value = item.TipoOrden;
            comando.Parameters.Add("@Estudio", MySqlDbType.VarChar).Value = item.NombreEstudio.Trim();
            AgregarTexto(comando, "@Motivo", item.Motivo);
            AgregarTexto(comando, "@Observaciones", item.Observaciones);
            comando.Parameters.Add("@Precio", MySqlDbType.Decimal).Value = item.Precio;
            comando.Parameters.Add("@Veterinario", MySqlDbType.Int32).Value = consulta.IdVeterinario;
            comando.ExecuteNonQuery();
            long idOrden = comando.LastInsertedId;
            if (item.Precio > 0)
                CrearCargo(conexion, tx, idDueno, consulta.IdMascota, idConsulta, consulta.IdCita, "Laboratorio", idOrden, item.NombreEstudio, 1, item.Precio, 0, item.Precio);
        }
    }

    private static void CrearRecordatorioRevision(MySqlConnection conexion, MySqlTransaction tx, ConsultaModel consulta)
    {
        if (consulta.ProximaRevision.HasValue)
            CrearRecordatorio(conexion, tx, consulta.IdMascota, "Revisión clínica", consulta.ProximaRevision.Value.Date, "Revisión posterior a consulta clínica.");
    }

    private static void FinalizarCita(MySqlConnection conexion, MySqlTransaction tx, long idCita)
    {
        const string actualizar = """
            UPDATE citas SET estado = 'Atendida', fecha_finalizacion = CURRENT_TIMESTAMP,
                   id_usuario_modificacion = @Usuario
            WHERE id_cita = @Cita;
            """;
        using MySqlCommand comando = new(actualizar, conexion, tx);
        comando.Parameters.Add("@Usuario", MySqlDbType.Int32).Value = SesionActual.Usuario!.IdUsuario;
        comando.Parameters.Add("@Cita", MySqlDbType.UInt64).Value = idCita;
        comando.ExecuteNonQuery();
        const string historial = """
            INSERT INTO cita_historial_estados (id_cita, estado_anterior, estado_nuevo, motivo, id_usuario)
            VALUES (@Cita, 'En consulta', 'Atendida', 'Consulta clínica finalizada.', @Usuario);
            """;
        using MySqlCommand h = new(historial, conexion, tx);
        h.Parameters.Add("@Cita", MySqlDbType.UInt64).Value = idCita;
        h.Parameters.Add("@Usuario", MySqlDbType.Int32).Value = SesionActual.Usuario!.IdUsuario;
        h.ExecuteNonQuery();
    }

    private static void CrearCargo(MySqlConnection conexion, MySqlTransaction tx, long dueno, long mascota, long consulta, long cita, string tipo, long referencia, string descripcion, decimal cantidad, decimal precio, decimal descuento, decimal subtotal)
    {
        const string sql = """
            INSERT INTO cargos_pendientes
            (id_dueno, id_mascota, id_consulta, id_cita, tipo_item, id_referencia, descripcion, cantidad, precio_unitario, descuento, subtotal, estado)
            VALUES (@Dueno, @Mascota, @Consulta, @Cita, @Tipo, @Referencia, @Descripcion, @Cantidad, @Precio, @Descuento, @Subtotal, 'Pendiente');
            """;
        using MySqlCommand comando = new(sql, conexion, tx);
        comando.Parameters.Add("@Dueno", MySqlDbType.UInt64).Value = dueno;
        comando.Parameters.Add("@Mascota", MySqlDbType.UInt64).Value = mascota;
        comando.Parameters.Add("@Consulta", MySqlDbType.UInt64).Value = consulta;
        comando.Parameters.Add("@Cita", MySqlDbType.UInt64).Value = cita;
        comando.Parameters.Add("@Tipo", MySqlDbType.VarChar).Value = tipo;
        comando.Parameters.Add("@Referencia", MySqlDbType.UInt64).Value = referencia;
        comando.Parameters.Add("@Descripcion", MySqlDbType.VarChar).Value = descripcion.Trim();
        comando.Parameters.Add("@Cantidad", MySqlDbType.Decimal).Value = cantidad;
        comando.Parameters.Add("@Precio", MySqlDbType.Decimal).Value = precio;
        comando.Parameters.Add("@Descuento", MySqlDbType.Decimal).Value = descuento;
        comando.Parameters.Add("@Subtotal", MySqlDbType.Decimal).Value = subtotal;
        comando.ExecuteNonQuery();
    }

    private static void CrearRecordatorio(MySqlConnection conexion, MySqlTransaction tx, long mascota, string tipo, DateTime fecha, string descripcion)
    {
        const string sql = """
            INSERT INTO recordatorios (id_mascota, tipo_recordatorio, fecha_programada, descripcion, estado, id_usuario_creacion)
            VALUES (@Mascota, @Tipo, @Fecha, @Descripcion, 'Pendiente', @Usuario);
            """;
        using MySqlCommand comando = new(sql, conexion, tx);
        comando.Parameters.Add("@Mascota", MySqlDbType.UInt64).Value = mascota;
        comando.Parameters.Add("@Tipo", MySqlDbType.VarChar).Value = tipo;
        comando.Parameters.Add("@Fecha", MySqlDbType.Date).Value = fecha.Date;
        comando.Parameters.Add("@Descripcion", MySqlDbType.VarChar).Value = descripcion;
        comando.Parameters.Add("@Usuario", MySqlDbType.Int32).Value = SesionActual.Usuario!.IdUsuario;
        comando.ExecuteNonQuery();
    }

    private static (bool Controla, long? Producto, string Nombre) ObtenerVacuna(MySqlConnection conexion, MySqlTransaction tx, int id)
    {
        using MySqlCommand comando = new("SELECT nombre, controla_inventario, id_producto_inventario FROM catalogo_vacunas WHERE id_vacuna = @Id AND activo = 1;", conexion, tx);
        comando.Parameters.Add("@Id", MySqlDbType.Int32).Value = id;
        using MySqlDataReader lector = comando.ExecuteReader();
        if (!lector.Read()) throw new InvalidOperationException("La vacuna seleccionada ya no está disponible.");
        return (lector.GetBoolean("controla_inventario"), lector.IsDBNull(lector.GetOrdinal("id_producto_inventario")) ? null : lector.GetInt64("id_producto_inventario"), lector.GetString("nombre"));
    }

    private static (bool Controla, long? Producto, string Nombre) ObtenerDesparasitante(MySqlConnection conexion, MySqlTransaction tx, int id)
    {
        using MySqlCommand comando = new("SELECT nombre, controla_inventario, id_producto_inventario FROM catalogo_desparasitantes WHERE id_desparasitante = @Id AND activo = 1;", conexion, tx);
        comando.Parameters.Add("@Id", MySqlDbType.Int32).Value = id;
        using MySqlDataReader lector = comando.ExecuteReader();
        if (!lector.Read()) throw new InvalidOperationException("El desparasitante seleccionado ya no está disponible.");
        return (lector.GetBoolean("controla_inventario"), lector.IsDBNull(lector.GetOrdinal("id_producto_inventario")) ? null : lector.GetInt64("id_producto_inventario"), lector.GetString("nombre"));
    }

    private static void ValidarDescontarLote(MySqlConnection conexion, MySqlTransaction tx, long producto, long? lote, DateTime aplicacion, long consulta, bool vacuna, long? aplicacionId)
    {
        if (!lote.HasValue) throw new InvalidOperationException("Seleccione un lote disponible para el producto que controla inventario.");
        const string sql = """
            SELECT cantidad_disponible, fecha_vencimiento, estado
            FROM inventario_lotes WHERE id_lote = @Lote AND id_producto = @Producto FOR UPDATE;
            """;
        using MySqlCommand comando = new(sql, conexion, tx);
        comando.Parameters.Add("@Lote", MySqlDbType.UInt64).Value = lote.Value;
        comando.Parameters.Add("@Producto", MySqlDbType.UInt64).Value = producto;
        using MySqlDataReader lector = comando.ExecuteReader();
        if (!lector.Read()) throw new InvalidOperationException("El lote seleccionado no corresponde al producto clínico.");
        decimal disponible = lector.GetDecimal("cantidad_disponible");
        DateTime? vence = lector.IsDBNull(lector.GetOrdinal("fecha_vencimiento")) ? null : lector.GetDateTime("fecha_vencimiento");
        string estado = lector.GetString("estado");
        lector.Close();
        if (!string.Equals(estado, "Disponible", StringComparison.OrdinalIgnoreCase) || disponible < 1)
            throw new InvalidOperationException("El lote seleccionado no tiene existencias disponibles.");
        if (vence.HasValue && vence.Value.Date < aplicacion.Date)
            throw new InvalidOperationException("No puede utilizarse un lote vencido.");
        using MySqlCommand actualizar = new("UPDATE inventario_lotes SET cantidad_disponible = cantidad_disponible - 1, estado = IF(cantidad_disponible - 1 <= 0, 'Agotado', estado) WHERE id_lote = @Lote;", conexion, tx);
        actualizar.Parameters.Add("@Lote", MySqlDbType.UInt64).Value = lote.Value;
        actualizar.ExecuteNonQuery();
    }

    private static void InsertarMovimiento(MySqlConnection conexion, MySqlTransaction tx, long producto, long lote, string tipo, long consulta, long? vacuna, string observaciones)
    {
        const string sql = """
            INSERT INTO inventario_movimientos
            (id_producto, id_lote, tipo_movimiento, cantidad, id_consulta, id_vacuna_aplicada, id_usuario_registro, observaciones)
            VALUES (@Producto, @Lote, @Tipo, 1, @Consulta, @Vacuna, @Usuario, @Observaciones);
            """;
        using MySqlCommand comando = new(sql, conexion, tx);
        comando.Parameters.Add("@Producto", MySqlDbType.UInt64).Value = producto;
        comando.Parameters.Add("@Lote", MySqlDbType.UInt64).Value = lote;
        comando.Parameters.Add("@Tipo", MySqlDbType.VarChar).Value = tipo;
        comando.Parameters.Add("@Consulta", MySqlDbType.UInt64).Value = consulta;
        comando.Parameters.Add("@Vacuna", MySqlDbType.UInt64).Value = vacuna.HasValue ? vacuna.Value : DBNull.Value;
        comando.Parameters.Add("@Usuario", MySqlDbType.Int32).Value = SesionActual.Usuario!.IdUsuario;
        comando.Parameters.Add("@Observaciones", MySqlDbType.VarChar).Value = observaciones;
        comando.ExecuteNonQuery();
    }

    private static void ValidarCierre(ConsultaCierreModel cierre, int idVeterinario)
    {
        ConsultaModel c = cierre.Consulta ?? throw new ArgumentException("La consulta es obligatoria.");
        if (c.IdCita <= 0 || c.IdMascota <= 0 || c.IdVeterinario != idVeterinario)
            throw new UnauthorizedAccessException("La consulta no pertenece al veterinario actual.");
        if (string.IsNullOrWhiteSpace(c.MotivoConsulta)) throw new ArgumentException("El motivo de consulta es obligatorio.");
        if (string.IsNullOrWhiteSpace(c.Anamnesis)) throw new ArgumentException("Registre la anamnesis del paciente.");
        if (string.IsNullOrWhiteSpace(c.HallazgosFisicos)) throw new ArgumentException("Registre los hallazgos físicos.");
        if (string.IsNullOrWhiteSpace(c.Indicaciones)) throw new ArgumentException("Registre las indicaciones al egreso.");
        if (string.IsNullOrWhiteSpace(c.EstadoEgreso)) throw new ArgumentException("Seleccione el estado del paciente al egreso.");
        if (c.Peso.HasValue && c.Peso.Value <= 0) throw new ArgumentException("El peso debe ser mayor que cero.");
        if (c.Temperatura.HasValue && (c.Temperatura < 20 || c.Temperatura > 50)) throw new ArgumentException("La temperatura registrada no es válida.");
        if (cierre.Diagnosticos.Count == 0 || cierre.Diagnosticos.Count(d => d.EsPrincipal) != 1)
            throw new ArgumentException("Registre exactamente un diagnóstico principal.");
        if (cierre.Servicios.Count == 0) throw new ArgumentException("Registre al menos un servicio realizado.");
        foreach (ConsultaServicioModel servicio in cierre.Servicios)
            if (servicio.IdServicio <= 0 || string.IsNullOrWhiteSpace(servicio.Descripcion) || servicio.Cantidad <= 0 || servicio.PrecioUnitario < 0 || servicio.Descuento < 0 || servicio.Subtotal < 0)
                throw new ArgumentException("Existe un servicio realizado con datos inválidos.");
        if (cierre.Receta is not null)
            foreach (RecetaDetalleModel d in cierre.Receta.Detalles)
                if ((!d.IdMedicamento.HasValue && string.IsNullOrWhiteSpace(d.MedicamentoLibre)) || string.IsNullOrWhiteSpace(d.Dosis) || string.IsNullOrWhiteSpace(d.Frecuencia) || string.IsNullOrWhiteSpace(d.Duracion))
                    throw new ArgumentException("Existe un medicamento de receta incompleto.");
        foreach (VacunaAplicadaModel v in cierre.Vacunas)
            if (v.IdVacuna <= 0 || string.IsNullOrWhiteSpace(v.Dosis) || v.PrecioAplicado < 0)
                throw new ArgumentException("Existe una vacuna incompleta.");
        foreach (DesparasitacionModel d in cierre.Desparasitaciones)
            if (d.IdDesparasitante <= 0 || string.IsNullOrWhiteSpace(d.Dosis) || d.PrecioAplicado < 0)
                throw new ArgumentException("Existe una desparasitación incompleta.");
        foreach (OrdenClinicaModel o in cierre.Ordenes)
            if (string.IsNullOrWhiteSpace(o.TipoOrden) || string.IsNullOrWhiteSpace(o.NombreEstudio) || o.Precio < 0)
                throw new ArgumentException("Existe una orden clínica incompleta.");
    }

    private static int ExigirVeterinarioAsociado()
    {
        if (!SesionActual.HaySesion || SesionActual.Usuario is null || !SesionActual.Usuario.IdVeterinario.HasValue)
            throw new UnauthorizedAccessException("Para registrar atención clínica, el usuario debe estar asociado a un veterinario activo.");
        if (!SesionActual.EsRol("Veterinario", "Administrador"))
            throw new UnauthorizedAccessException("El rol actual no está autorizado para registrar atención clínica.");
        return SesionActual.Usuario.IdVeterinario.Value;
    }

    private static void AgregarTexto(MySqlCommand comando, string nombre, string? valor) => comando.Parameters.Add(nombre, MySqlDbType.Text).Value = string.IsNullOrWhiteSpace(valor) ? DBNull.Value : valor.Trim();
    private static void AgregarDecimal(MySqlCommand comando, string nombre, decimal? valor) => comando.Parameters.Add(nombre, MySqlDbType.Decimal).Value = valor.HasValue ? valor.Value : DBNull.Value;
    private static void AgregarEntero(MySqlCommand comando, string nombre, int? valor) => comando.Parameters.Add(nombre, MySqlDbType.Int32).Value = valor.HasValue ? valor.Value : DBNull.Value;
}
