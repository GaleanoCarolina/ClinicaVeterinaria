using System;
using System.Collections.Generic;
using System.Data;
using ClinicaVeterinaria.Data;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Utils;
using MySql.Data.MySqlClient;

namespace ClinicaVeterinaria.Services;

public sealed class CitaService
{
    private readonly DisponibilidadService _disponibilidad = new();

    public List<ServicioModel> ListarServiciosAgenda()
    {
        ExigirLecturaAgenda();
        const string sql = """
            SELECT id_servicio, codigo, nombre, precio_base, duracion_minutos, genera_cargo, activo
            FROM catalogo_servicios
            WHERE activo = 1 AND duracion_minutos IN (30,60,90,120)
            ORDER BY nombre;
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
                PrecioBase = lector.GetDecimal("precio_base"), DuracionMinutos = lector.GetInt32("duracion_minutos"),
                GeneraCargo = lector.GetBoolean("genera_cargo"), Activo = lector.GetBoolean("activo")
            });
        }
        return lista;
    }

    public List<MascotaBusquedaModel> BuscarMascotas(string termino)
    {
        ExigirLecturaAgenda();
        string filtro = (termino ?? string.Empty).Trim();
        const string sql = """
            SELECT m.id_mascota, m.codigo_paciente, m.nombre, m.especie,
                   d.nombre_completo dueno, d.telefono_principal
            FROM mascotas m
            INNER JOIN duenos d ON d.id_dueno = m.id_dueno
            WHERE m.activo = 1 AND m.estado_vital = 'Viva' AND d.activo = 1
              AND (@Termino = '' OR m.codigo_paciente LIKE @Like OR m.nombre LIKE @Like
                   OR d.nombre_completo LIKE @Like OR d.telefono_principal LIKE @Like
                   OR m.microchip LIKE @Like)
            ORDER BY d.nombre_completo, m.nombre
            LIMIT 100;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Termino", MySqlDbType.VarChar).Value = filtro;
        comando.Parameters.Add("@Like", MySqlDbType.VarChar).Value = $"%{filtro}%";
        using MySqlDataReader lector = comando.ExecuteReader();
        List<MascotaBusquedaModel> resultado = new();
        while (lector.Read())
        {
            resultado.Add(new MascotaBusquedaModel
            {
                IdMascota = lector.GetInt64("id_mascota"), CodigoPaciente = lector.GetString("codigo_paciente"),
                NombreMascota = lector.GetString("nombre"), Especie = lector.GetString("especie"),
                Dueno = lector.GetString("dueno"), Telefono = lector.GetString("telefono_principal")
            });
        }
        return resultado;
    }

    public MascotaBusquedaModel ObtenerMascotaParaAgenda(long idMascota)
    {
        ExigirLecturaAgenda();
        const string sql = """
            SELECT m.id_mascota, m.codigo_paciente, m.nombre, m.especie,
                   d.nombre_completo dueno, d.telefono_principal
            FROM mascotas m INNER JOIN duenos d ON d.id_dueno = m.id_dueno
            WHERE m.id_mascota = @Id AND m.activo = 1 AND m.estado_vital = 'Viva' AND d.activo = 1;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Id", MySqlDbType.UInt64).Value = idMascota;
        using MySqlDataReader lector = comando.ExecuteReader();
        if (!lector.Read()) throw new InvalidOperationException("La mascota ya no está disponible para agendar.");
        return new MascotaBusquedaModel
        {
            IdMascota = lector.GetInt64("id_mascota"), CodigoPaciente = lector.GetString("codigo_paciente"),
            NombreMascota = lector.GetString("nombre"), Especie = lector.GetString("especie"),
            Dueno = lector.GetString("dueno"), Telefono = lector.GetString("telefono_principal")
        };
    }

    public List<CitaModel> ListarAgenda(DateTime fecha, int? idVeterinario, string? estado, int? idServicio, string? busqueda)
    {
        ExigirLecturaAgenda();
        if (SesionActual.EsRol("Veterinario"))
        {
            if (!SesionActual.Usuario!.IdVeterinario.HasValue)
            {
                return new List<CitaModel>();
            }
            idVeterinario = SesionActual.Usuario.IdVeterinario.Value;
        }
        const string sql = """
            SELECT c.id_cita, c.id_mascota, m.codigo_paciente, m.nombre mascota,
                d.nombre_completo dueno, d.telefono_principal,
                c.id_veterinario, v.nombre_completo veterinario, c.id_servicio, s.nombre servicio,
                c.fecha_hora_inicio, c.duracion_minutos, c.fecha_hora_fin, c.motivo_consulta,
                COALESCE(c.observaciones_recepcion, '') observaciones_recepcion, c.estado, c.fecha_llegada,
                (SELECT COUNT(*) FROM mascota_alertas_clinicas a WHERE a.id_mascota = m.id_mascota AND a.activa = 1) alertas,
                COALESCE((SELECT SUM(f.saldo_pendiente) FROM facturas f
                    WHERE f.id_mascota = m.id_mascota AND f.estado <> 'Anulada'), 0) saldo
            FROM citas c
            INNER JOIN mascotas m ON m.id_mascota = c.id_mascota
            INNER JOIN duenos d ON d.id_dueno = m.id_dueno
            INNER JOIN veterinarios v ON v.id_veterinario = c.id_veterinario
            INNER JOIN catalogo_servicios s ON s.id_servicio = c.id_servicio
            WHERE c.fecha_hora_inicio >= @Desde AND c.fecha_hora_inicio < @Hasta
              AND (@Veterinario IS NULL OR c.id_veterinario = @Veterinario)
              AND (@Estado = '' OR c.estado = @Estado)
              AND (@Servicio IS NULL OR c.id_servicio = @Servicio)
              AND (@Buscar = '' OR m.nombre LIKE @Like OR d.nombre_completo LIKE @Like
                   OR m.codigo_paciente LIKE @Like OR d.telefono_principal LIKE @Like)
            ORDER BY c.fecha_hora_inicio, v.nombre_completo;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Desde", MySqlDbType.DateTime).Value = fecha.Date;
        comando.Parameters.Add("@Hasta", MySqlDbType.DateTime).Value = fecha.Date.AddDays(1);
        comando.Parameters.Add("@Veterinario", MySqlDbType.Int32).Value = idVeterinario.HasValue ? idVeterinario.Value : DBNull.Value;
        comando.Parameters.Add("@Estado", MySqlDbType.VarChar).Value = estado ?? string.Empty;
        comando.Parameters.Add("@Servicio", MySqlDbType.Int32).Value = idServicio.HasValue ? idServicio.Value : DBNull.Value;
        comando.Parameters.Add("@Buscar", MySqlDbType.VarChar).Value = (busqueda ?? string.Empty).Trim();
        comando.Parameters.Add("@Like", MySqlDbType.VarChar).Value = $"%{(busqueda ?? string.Empty).Trim()}%";
        using MySqlDataReader lector = comando.ExecuteReader();
        List<CitaModel> resultado = new();
        while (lector.Read())
        {
            resultado.Add(MapearCita(lector));
        }
        return resultado;
    }

    public CitaModel ObtenerCita(long idCita)
    {
        ExigirLecturaAgenda();
        const string sql = """
            SELECT c.id_cita, c.id_mascota, m.codigo_paciente, m.nombre mascota,
                d.nombre_completo dueno, d.telefono_principal,
                c.id_veterinario, v.nombre_completo veterinario, c.id_servicio, s.nombre servicio,
                c.fecha_hora_inicio, c.duracion_minutos, c.fecha_hora_fin, c.motivo_consulta,
                COALESCE(c.observaciones_recepcion, '') observaciones_recepcion, c.estado, c.fecha_llegada,
                (SELECT COUNT(*) FROM mascota_alertas_clinicas a WHERE a.id_mascota = m.id_mascota AND a.activa = 1) alertas,
                COALESCE((SELECT SUM(f.saldo_pendiente) FROM facturas f
                    WHERE f.id_mascota = m.id_mascota AND f.estado <> 'Anulada'), 0) saldo
            FROM citas c
            INNER JOIN mascotas m ON m.id_mascota = c.id_mascota
            INNER JOIN duenos d ON d.id_dueno = m.id_dueno
            INNER JOIN veterinarios v ON v.id_veterinario = c.id_veterinario
            INNER JOIN catalogo_servicios s ON s.id_servicio = c.id_servicio
            WHERE c.id_cita = @Id;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Id", MySqlDbType.UInt64).Value = idCita;
        using MySqlDataReader lector = comando.ExecuteReader();
        if (!lector.Read())
        {
            throw new InvalidOperationException("No se encontró la cita seleccionada.");
        }
        return MapearCita(lector);
    }

    public long CrearCita(CitaModel cita)
    {
        ExigirGestionAgenda();
        ValidarDatosCita(cita, false);
        _disponibilidad.ValidarReserva(cita.IdVeterinario, cita.FechaHoraInicio, cita.DuracionMinutos, cita.IdMascota);
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlTransaction tx = conexion.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            ValidarEntidadesActivas(conexion, tx, cita);
            ValidarSinBloqueo(conexion, tx, cita.IdVeterinario, cita.FechaHoraInicio, cita.FechaHoraFin);
            ValidarSinCitaMascota(conexion, tx, cita.IdMascota, cita.FechaHoraInicio, cita.FechaHoraFin, null);
            const string insertar = """
                INSERT INTO citas
                  (id_mascota, id_veterinario, id_servicio, fecha_hora_inicio, duracion_minutos,
                   fecha_hora_fin, motivo_consulta, observaciones_recepcion, estado, id_usuario_creacion)
                VALUES (@Mascota, @Veterinario, @Servicio, @Inicio, @Duracion, @Fin,
                        @Motivo, @Observaciones, 'Pendiente', @Usuario);
                """;
            using MySqlCommand comando = new(insertar, conexion, tx);
            AgregarDatosCita(comando, cita);
            comando.Parameters.Add("@Usuario", MySqlDbType.Int32).Value = SesionActual.Usuario!.IdUsuario;
            comando.ExecuteNonQuery();
            long idCita = comando.LastInsertedId;
            InsertarBloques(conexion, tx, idCita, cita.IdVeterinario, cita.FechaHoraInicio, cita.DuracionMinutos);
            RegistrarEstado(conexion, tx, idCita, null, "Pendiente", "Cita creada");
            tx.Commit();
            return idCita;
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            tx.Rollback();
            throw new InvalidOperationException("El veterinario acaba de ser reservado en uno de los bloques elegidos. Seleccione otro horario.", ex);
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void ReagendarCita(long idCita, CitaModel nuevaCita, string motivo)
    {
        ExigirGestionAgenda();
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new ArgumentException("El motivo de reagendamiento es obligatorio.");
        }
        ValidarDatosCita(nuevaCita, true);
        _disponibilidad.ValidarReserva(nuevaCita.IdVeterinario, nuevaCita.FechaHoraInicio, nuevaCita.DuracionMinutos, nuevaCita.IdMascota, idCita);
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlTransaction tx = conexion.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            CitaModel anterior = ObtenerCitaParaActualizar(conexion, tx, idCita);
            if (anterior.Estado is "Cancelada" or "No asistió" or "Atendida" or "En consulta")
            {
                throw new InvalidOperationException("La cita seleccionada ya no puede reagendarse en su estado actual.");
            }
            nuevaCita.IdMascota = anterior.IdMascota;
            ValidarEntidadesActivas(conexion, tx, nuevaCita);
            ValidarSinBloqueo(conexion, tx, nuevaCita.IdVeterinario, nuevaCita.FechaHoraInicio, nuevaCita.FechaHoraFin);
            ValidarSinCitaMascota(conexion, tx, nuevaCita.IdMascota, nuevaCita.FechaHoraInicio, nuevaCita.FechaHoraFin, idCita);
            using (MySqlCommand borrar = new("DELETE FROM cita_bloques WHERE id_cita = @Id;", conexion, tx))
            {
                borrar.Parameters.Add("@Id", MySqlDbType.UInt64).Value = idCita;
                borrar.ExecuteNonQuery();
            }
            const string actualizar = """
                UPDATE citas SET id_veterinario = @Veterinario, id_servicio = @Servicio,
                    fecha_hora_inicio = @Inicio, duracion_minutos = @Duracion, fecha_hora_fin = @Fin,
                    motivo_consulta = @Motivo, observaciones_recepcion = @Observaciones,
                    estado = 'Reagendada', id_usuario_modificacion = @Usuario
                WHERE id_cita = @Id;
                """;
            using (MySqlCommand comando = new(actualizar, conexion, tx))
            {
                comando.Parameters.Add("@Id", MySqlDbType.UInt64).Value = idCita;
                AgregarDatosCita(comando, nuevaCita);
                comando.Parameters.Add("@Usuario", MySqlDbType.Int32).Value = SesionActual.Usuario!.IdUsuario;
                comando.ExecuteNonQuery();
            }
            InsertarBloques(conexion, tx, idCita, nuevaCita.IdVeterinario, nuevaCita.FechaHoraInicio, nuevaCita.DuracionMinutos);
            const string historial = """
                INSERT INTO cita_reagendamientos
                   (id_cita, fecha_hora_anterior, fecha_hora_nueva, duracion_anterior, duracion_nueva,
                    id_veterinario_anterior, id_veterinario_nuevo, motivo, id_usuario)
                VALUES (@Id, @InicioAnterior, @InicioNuevo, @DuracionAnterior, @DuracionNueva,
                        @VetAnterior, @VetNuevo, @Motivo, @Usuario);
                """;
            using (MySqlCommand comando = new(historial, conexion, tx))
            {
                comando.Parameters.Add("@Id", MySqlDbType.UInt64).Value = idCita;
                comando.Parameters.Add("@InicioAnterior", MySqlDbType.DateTime).Value = anterior.FechaHoraInicio;
                comando.Parameters.Add("@InicioNuevo", MySqlDbType.DateTime).Value = nuevaCita.FechaHoraInicio;
                comando.Parameters.Add("@DuracionAnterior", MySqlDbType.Int32).Value = anterior.DuracionMinutos;
                comando.Parameters.Add("@DuracionNueva", MySqlDbType.Int32).Value = nuevaCita.DuracionMinutos;
                comando.Parameters.Add("@VetAnterior", MySqlDbType.Int32).Value = anterior.IdVeterinario;
                comando.Parameters.Add("@VetNuevo", MySqlDbType.Int32).Value = nuevaCita.IdVeterinario;
                comando.Parameters.Add("@Motivo", MySqlDbType.VarChar).Value = motivo.Trim();
                comando.Parameters.Add("@Usuario", MySqlDbType.Int32).Value = SesionActual.Usuario!.IdUsuario;
                comando.ExecuteNonQuery();
            }
            RegistrarEstado(conexion, tx, idCita, anterior.Estado, "Reagendada", motivo.Trim());
            tx.Commit();
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            tx.Rollback();
            throw new InvalidOperationException("No fue posible reagendar: otro usuario tomó uno de los bloques elegidos.", ex);
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void ConfirmarCita(long idCita)
    {
        ExigirGestionAgenda();
        CambiarEstadoSimple(idCita, new[] { "Pendiente", "Reagendada" }, "Confirmada", "Confirmación de cita", false);
    }

    public void MarcarLlegada(long idCita, string? observacion)
    {
        ExigirGestionAgenda();
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlTransaction tx = conexion.BeginTransaction();
        try
        {
            CitaModel cita = ObtenerCitaParaActualizar(conexion, tx, idCita);
            if (cita.Estado is not ("Pendiente" or "Confirmada" or "Reagendada"))
            {
                throw new InvalidOperationException("Solo una cita pendiente, confirmada o reagendada puede registrar llegada.");
            }
            const string sql = """
                UPDATE citas SET estado = 'Llegó', fecha_llegada = CURRENT_TIMESTAMP,
                    observaciones_recepcion = CASE WHEN @Observacion = '' THEN observaciones_recepcion ELSE @Observacion END,
                    id_usuario_modificacion = @Usuario WHERE id_cita = @Id;
                """;
            using MySqlCommand comando = new(sql, conexion, tx);
            comando.Parameters.Add("@Observacion", MySqlDbType.Text).Value = (observacion ?? string.Empty).Trim();
            comando.Parameters.Add("@Usuario", MySqlDbType.Int32).Value = SesionActual.Usuario!.IdUsuario;
            comando.Parameters.Add("@Id", MySqlDbType.UInt64).Value = idCita;
            comando.ExecuteNonQuery();
            RegistrarEstado(conexion, tx, idCita, cita.Estado, "Llegó", "Paciente recibido en clínica");
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void CancelarCita(long idCita, string motivo)
    {
        ExigirGestionAgenda();
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new ArgumentException("El motivo de cancelación es obligatorio.");
        }
        CambiarEstadoConLiberacion(idCita, "Cancelada", motivo.Trim(), "motivo_cancelacion");
    }

    public void MarcarNoAsistencia(long idCita, string motivo)
    {
        ExigirGestionAgenda();
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new ArgumentException("El motivo de no asistencia es obligatorio.");
        }
        CambiarEstadoConLiberacion(idCita, "No asistió", motivo.Trim(), "motivo_no_asistencia");
    }

    public void IniciarConsulta(long idCita)
    {
        if (!SesionActual.EsRol("Veterinario", "Administrador") || !SesionActual.Usuario!.IdVeterinario.HasValue)
        {
            throw new UnauthorizedAccessException("Solo un usuario vinculado a un veterinario puede iniciar la consulta.");
        }
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlTransaction tx = conexion.BeginTransaction();
        try
        {
            CitaModel cita = ObtenerCitaParaActualizar(conexion, tx, idCita);
            if (cita.IdVeterinario != SesionActual.Usuario.IdVeterinario.Value)
            {
                throw new UnauthorizedAccessException("El veterinario solamente puede atender citas asignadas a él.");
            }
            if (cita.Estado != "Llegó")
            {
                throw new InvalidOperationException("La consulta solo puede iniciarse cuando recepción haya marcado la llegada del paciente.");
            }
            using MySqlCommand comando = new("UPDATE citas SET estado = 'En consulta', fecha_inicio_consulta = CURRENT_TIMESTAMP, id_usuario_modificacion = @Usuario WHERE id_cita = @Id;", conexion, tx);
            comando.Parameters.Add("@Usuario", MySqlDbType.Int32).Value = SesionActual.Usuario.IdUsuario;
            comando.Parameters.Add("@Id", MySqlDbType.UInt64).Value = idCita;
            comando.ExecuteNonQuery();
            RegistrarEstado(conexion, tx, idCita, cita.Estado, "En consulta", "Consulta iniciada por veterinario asignado");
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private void CambiarEstadoSimple(long idCita, string[] estadosPermitidos, string estadoNuevo, string motivo, bool liberarBloques)
    {
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlTransaction tx = conexion.BeginTransaction();
        try
        {
            CitaModel cita = ObtenerCitaParaActualizar(conexion, tx, idCita);
            if (Array.IndexOf(estadosPermitidos, cita.Estado) < 0)
            {
                throw new InvalidOperationException($"La cita en estado '{cita.Estado}' no puede cambiar a '{estadoNuevo}'.");
            }
            using MySqlCommand comando = new("UPDATE citas SET estado = @Estado, id_usuario_modificacion = @Usuario WHERE id_cita = @Id;", conexion, tx);
            comando.Parameters.Add("@Estado", MySqlDbType.VarChar).Value = estadoNuevo;
            comando.Parameters.Add("@Usuario", MySqlDbType.Int32).Value = SesionActual.Usuario!.IdUsuario;
            comando.Parameters.Add("@Id", MySqlDbType.UInt64).Value = idCita;
            comando.ExecuteNonQuery();
            if (liberarBloques)
            {
                LiberarBloques(conexion, tx, idCita);
            }
            RegistrarEstado(conexion, tx, idCita, cita.Estado, estadoNuevo, motivo);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private void CambiarEstadoConLiberacion(long idCita, string estadoNuevo, string motivo, string columnaMotivo)
    {
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlTransaction tx = conexion.BeginTransaction();
        try
        {
            CitaModel cita = ObtenerCitaParaActualizar(conexion, tx, idCita);
            if (cita.Estado is "Atendida" or "Cancelada" or "No asistió" or "En consulta")
            {
                throw new InvalidOperationException("La cita ya no admite esta operación.");
            }
            string sql = $"UPDATE citas SET estado = @Estado, {columnaMotivo} = @Motivo, id_usuario_modificacion = @Usuario WHERE id_cita = @Id;";
            using MySqlCommand comando = new(sql, conexion, tx);
            comando.Parameters.Add("@Estado", MySqlDbType.VarChar).Value = estadoNuevo;
            comando.Parameters.Add("@Motivo", MySqlDbType.VarChar).Value = motivo;
            comando.Parameters.Add("@Usuario", MySqlDbType.Int32).Value = SesionActual.Usuario!.IdUsuario;
            comando.Parameters.Add("@Id", MySqlDbType.UInt64).Value = idCita;
            comando.ExecuteNonQuery();
            LiberarBloques(conexion, tx, idCita);
            RegistrarEstado(conexion, tx, idCita, cita.Estado, estadoNuevo, motivo);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static void InsertarBloques(MySqlConnection conexion, MySqlTransaction tx, long idCita, int idVeterinario, DateTime inicio, int duracion)
    {
        for (int minutos = 0; minutos < duracion; minutos += 30)
        {
            using MySqlCommand comando = new("INSERT INTO cita_bloques (id_cita, id_veterinario, fecha_hora_bloque) VALUES (@Cita, @Vet, @Bloque);", conexion, tx);
            comando.Parameters.Add("@Cita", MySqlDbType.UInt64).Value = idCita;
            comando.Parameters.Add("@Vet", MySqlDbType.Int32).Value = idVeterinario;
            comando.Parameters.Add("@Bloque", MySqlDbType.DateTime).Value = inicio.AddMinutes(minutos);
            comando.ExecuteNonQuery();
        }
    }

    private static void LiberarBloques(MySqlConnection conexion, MySqlTransaction tx, long idCita)
    {
        using MySqlCommand comando = new("DELETE FROM cita_bloques WHERE id_cita = @Id;", conexion, tx);
        comando.Parameters.Add("@Id", MySqlDbType.UInt64).Value = idCita;
        comando.ExecuteNonQuery();
    }

    private static void RegistrarEstado(MySqlConnection conexion, MySqlTransaction tx, long idCita, string? anterior, string nuevo, string? motivo)
    {
        using MySqlCommand comando = new("INSERT INTO cita_historial_estados (id_cita, estado_anterior, estado_nuevo, motivo, id_usuario) VALUES (@Cita, @Anterior, @Nuevo, @Motivo, @Usuario);", conexion, tx);
        comando.Parameters.Add("@Cita", MySqlDbType.UInt64).Value = idCita;
        comando.Parameters.Add("@Anterior", MySqlDbType.VarChar).Value = string.IsNullOrWhiteSpace(anterior) ? DBNull.Value : anterior;
        comando.Parameters.Add("@Nuevo", MySqlDbType.VarChar).Value = nuevo;
        comando.Parameters.Add("@Motivo", MySqlDbType.VarChar).Value = string.IsNullOrWhiteSpace(motivo) ? DBNull.Value : motivo;
        comando.Parameters.Add("@Usuario", MySqlDbType.Int32).Value = SesionActual.Usuario!.IdUsuario;
        comando.ExecuteNonQuery();
    }

    private static void ValidarEntidadesActivas(MySqlConnection conexion, MySqlTransaction tx, CitaModel cita)
    {
        const string sql = """
            SELECT
                EXISTS(SELECT 1 FROM mascotas WHERE id_mascota = @Mascota AND activo = 1 AND estado_vital = 'Viva') mascota,
                EXISTS(SELECT 1 FROM veterinarios WHERE id_veterinario = @Vet AND activo = 1) veterinario,
                EXISTS(SELECT 1 FROM catalogo_servicios WHERE id_servicio = @Servicio AND activo = 1) servicio;
            """;
        using MySqlCommand comando = new(sql, conexion, tx);
        comando.Parameters.Add("@Mascota", MySqlDbType.UInt64).Value = cita.IdMascota;
        comando.Parameters.Add("@Vet", MySqlDbType.Int32).Value = cita.IdVeterinario;
        comando.Parameters.Add("@Servicio", MySqlDbType.Int32).Value = cita.IdServicio;
        using MySqlDataReader lector = comando.ExecuteReader();
        lector.Read();
        bool mascota = lector.GetBoolean("mascota");
        bool veterinario = lector.GetBoolean("veterinario");
        bool servicio = lector.GetBoolean("servicio");
        lector.Close();
        if (!mascota) throw new InvalidOperationException("La mascota no existe o no está activa y viva.");
        if (!veterinario) throw new InvalidOperationException("El veterinario no existe o no está activo.");
        if (!servicio) throw new InvalidOperationException("El servicio no existe o no está activo.");
    }

    private static void ValidarSinBloqueo(MySqlConnection conexion, MySqlTransaction tx, int veterinario, DateTime inicio, DateTime fin)
    {
        const string sql = """
            SELECT COUNT(*) FROM veterinario_bloqueos
            WHERE id_veterinario = @Vet AND estado = 'Vigente'
              AND fecha_hora_inicio < @Fin AND fecha_hora_fin > @Inicio;
            """;
        using MySqlCommand comando = new(sql, conexion, tx);
        comando.Parameters.Add("@Vet", MySqlDbType.Int32).Value = veterinario;
        comando.Parameters.Add("@Inicio", MySqlDbType.DateTime).Value = inicio;
        comando.Parameters.Add("@Fin", MySqlDbType.DateTime).Value = fin;
        if (Convert.ToInt32(comando.ExecuteScalar()) > 0)
        {
            throw new InvalidOperationException("El veterinario tiene un bloqueo vigente en el intervalo seleccionado.");
        }
    }

    private static void ValidarSinCitaMascota(MySqlConnection conexion, MySqlTransaction tx, long mascota, DateTime inicio, DateTime fin, long? excluir)
    {
        const string sql = """
            SELECT id_cita FROM citas
            WHERE id_mascota = @Mascota AND estado NOT IN ('Cancelada','No asistió')
              AND fecha_hora_inicio < @Fin AND fecha_hora_fin > @Inicio
              AND (@Excluir IS NULL OR id_cita <> @Excluir)
            LIMIT 1 FOR UPDATE;
            """;
        using MySqlCommand comando = new(sql, conexion, tx);
        comando.Parameters.Add("@Mascota", MySqlDbType.UInt64).Value = mascota;
        comando.Parameters.Add("@Inicio", MySqlDbType.DateTime).Value = inicio;
        comando.Parameters.Add("@Fin", MySqlDbType.DateTime).Value = fin;
        comando.Parameters.Add("@Excluir", MySqlDbType.UInt64).Value = excluir.HasValue ? excluir.Value : DBNull.Value;
        if (comando.ExecuteScalar() is not null)
        {
            throw new InvalidOperationException("La mascota tiene otra cita activa que se cruza con el horario elegido.");
        }
    }

    private static CitaModel ObtenerCitaParaActualizar(MySqlConnection conexion, MySqlTransaction tx, long idCita)
    {
        const string sql = """
            SELECT id_cita, id_mascota, id_veterinario, id_servicio, fecha_hora_inicio,
                   duracion_minutos, fecha_hora_fin, motivo_consulta,
                   COALESCE(observaciones_recepcion, '') observaciones_recepcion, estado
            FROM citas WHERE id_cita = @Id FOR UPDATE;
            """;
        using MySqlCommand comando = new(sql, conexion, tx);
        comando.Parameters.Add("@Id", MySqlDbType.UInt64).Value = idCita;
        using MySqlDataReader lector = comando.ExecuteReader();
        if (!lector.Read())
        {
            throw new InvalidOperationException("La cita solicitada no existe.");
        }
        return new CitaModel
        {
            IdCita = lector.GetInt64("id_cita"), IdMascota = lector.GetInt64("id_mascota"),
            IdVeterinario = lector.GetInt32("id_veterinario"), IdServicio = lector.GetInt32("id_servicio"),
            FechaHoraInicio = lector.GetDateTime("fecha_hora_inicio"), DuracionMinutos = lector.GetInt32("duracion_minutos"),
            FechaHoraFin = lector.GetDateTime("fecha_hora_fin"), MotivoConsulta = lector.GetString("motivo_consulta"),
            ObservacionesRecepcion = lector.GetString("observaciones_recepcion"), Estado = lector.GetString("estado")
        };
    }

    private static void AgregarDatosCita(MySqlCommand comando, CitaModel cita)
    {
        comando.Parameters.Add("@Mascota", MySqlDbType.UInt64).Value = cita.IdMascota;
        comando.Parameters.Add("@Veterinario", MySqlDbType.Int32).Value = cita.IdVeterinario;
        comando.Parameters.Add("@Servicio", MySqlDbType.Int32).Value = cita.IdServicio;
        comando.Parameters.Add("@Inicio", MySqlDbType.DateTime).Value = cita.FechaHoraInicio;
        comando.Parameters.Add("@Duracion", MySqlDbType.Int32).Value = cita.DuracionMinutos;
        comando.Parameters.Add("@Fin", MySqlDbType.DateTime).Value = cita.FechaHoraFin;
        comando.Parameters.Add("@Motivo", MySqlDbType.VarChar).Value = cita.MotivoConsulta.Trim();
        comando.Parameters.Add("@Observaciones", MySqlDbType.Text).Value = string.IsNullOrWhiteSpace(cita.ObservacionesRecepcion) ? DBNull.Value : cita.ObservacionesRecepcion.Trim();
    }

    private static void ValidarDatosCita(CitaModel cita, bool esReagenda)
    {
        if (cita.IdMascota <= 0 || cita.IdVeterinario <= 0 || cita.IdServicio <= 0)
        {
            throw new ArgumentException("Seleccione mascota, veterinario y servicio.");
        }
        if (string.IsNullOrWhiteSpace(cita.MotivoConsulta))
        {
            throw new ArgumentException("El motivo de consulta es obligatorio.");
        }
        if (cita.DuracionMinutos <= 0 || cita.DuracionMinutos % 30 != 0)
        {
            throw new ArgumentException("La duración debe respetar bloques de 30 minutos.");
        }
        cita.FechaHoraFin = cita.FechaHoraInicio.AddMinutes(cita.DuracionMinutos);
        if (cita.FechaHoraInicio < DateTime.Now.AddMinutes(-1) && !SesionActual.EsRol("Administrador"))
        {
            throw new InvalidOperationException(esReagenda ? "No se puede reagendar hacia una fecha pasada." : "No se puede crear una cita en una fecha pasada.");
        }
    }

    private static CitaModel MapearCita(MySqlDataReader lector) => new()
    {
        IdCita = lector.GetInt64("id_cita"), IdMascota = lector.GetInt64("id_mascota"), CodigoPaciente = lector.GetString("codigo_paciente"),
        Mascota = lector.GetString("mascota"), Dueno = lector.GetString("dueno"), TelefonoDueno = lector.GetString("telefono_principal"),
        IdVeterinario = lector.GetInt32("id_veterinario"), Veterinario = lector.GetString("veterinario"),
        IdServicio = lector.GetInt32("id_servicio"), Servicio = lector.GetString("servicio"),
        FechaHoraInicio = lector.GetDateTime("fecha_hora_inicio"), DuracionMinutos = lector.GetInt32("duracion_minutos"),
        FechaHoraFin = lector.GetDateTime("fecha_hora_fin"), MotivoConsulta = lector.GetString("motivo_consulta"),
        ObservacionesRecepcion = lector.GetString("observaciones_recepcion"), Estado = lector.GetString("estado"),
        FechaLlegada = lector.IsDBNull("fecha_llegada") ? null : lector.GetDateTime("fecha_llegada"),
        AlertasActivas = lector.GetInt32("alertas"), SaldoPendiente = lector.GetDecimal("saldo")
    };

    private static void ExigirGestionAgenda() => SesionActual.ExigirRoles("Administrador", "Recepción");
    private static void ExigirLecturaAgenda() => SesionActual.ExigirRoles("Administrador", "Recepción", "Veterinario");
}
