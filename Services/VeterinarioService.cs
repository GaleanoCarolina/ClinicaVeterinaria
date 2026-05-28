using System;
using System.Collections.Generic;
using System.Data;
using ClinicaVeterinaria.Data;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Utils;
using MySql.Data.MySqlClient;

namespace ClinicaVeterinaria.Services;

public sealed class VeterinarioService
{
    public List<VeterinarioModel> ListarVeterinarios(bool soloActivos = false)
    {
        ExigirLectura();
        const string sql = """
            SELECT v.id_veterinario, v.codigo_veterinario, v.id_usuario,
                   v.nombre_completo, v.numero_profesional, v.especialidad,
                   v.telefono, v.correo, v.activo, v.fecha_creacion,
                   COALESCE(u.nombre_usuario, '') usuario_asociado
            FROM veterinarios v
            LEFT JOIN usuarios u ON u.id_usuario = v.id_usuario
            WHERE (@SoloActivos = 0 OR v.activo = 1)
            ORDER BY v.nombre_completo;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@SoloActivos", MySqlDbType.Bit).Value = soloActivos;
        using MySqlDataReader lector = comando.ExecuteReader();
        List<VeterinarioModel> resultado = new();
        while (lector.Read())
        {
            resultado.Add(new VeterinarioModel
            {
                IdVeterinario = lector.GetInt32("id_veterinario"),
                CodigoVeterinario = lector.GetString("codigo_veterinario"),
                IdUsuario = lector.IsDBNull("id_usuario") ? null : lector.GetInt32("id_usuario"),
                NombreCompleto = lector.GetString("nombre_completo"),
                NumeroProfesional = lector.IsDBNull("numero_profesional") ? string.Empty : lector.GetString("numero_profesional"),
                Especialidad = lector.IsDBNull("especialidad") ? string.Empty : lector.GetString("especialidad"),
                Telefono = lector.IsDBNull("telefono") ? string.Empty : lector.GetString("telefono"),
                Correo = lector.IsDBNull("correo") ? string.Empty : lector.GetString("correo"),
                Activo = lector.GetBoolean("activo"),
                FechaCreacion = lector.GetDateTime("fecha_creacion"),
                UsuarioAsociado = lector.GetString("usuario_asociado")
            });
        }
        return resultado;
    }

    public List<UsuarioModel> ListarUsuariosVeterinarioDisponibles(int? idVeterinarioActual = null)
    {
        SesionActual.ExigirRoles("Administrador");
        const string sql = """
            SELECT u.id_usuario, u.id_rol, u.nombre_usuario, u.nombre_completo, r.nombre rol,
                   v.id_veterinario
            FROM usuarios u
            INNER JOIN roles r ON r.id_rol = u.id_rol
            LEFT JOIN veterinarios v ON v.id_usuario = u.id_usuario
            WHERE u.activo = 1 AND r.nombre = 'Veterinario'
              AND (v.id_veterinario IS NULL OR v.id_veterinario = @IdActual)
            ORDER BY u.nombre_completo;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@IdActual", MySqlDbType.Int32).Value = idVeterinarioActual.HasValue ? idVeterinarioActual.Value : DBNull.Value;
        using MySqlDataReader lector = comando.ExecuteReader();
        List<UsuarioModel> resultado = new();
        while (lector.Read())
        {
            resultado.Add(new UsuarioModel
            {
                IdUsuario = lector.GetInt32("id_usuario"),
                IdRol = lector.GetInt32("id_rol"),
                NombreUsuario = lector.GetString("nombre_usuario"),
                NombreCompleto = lector.GetString("nombre_completo"),
                Rol = lector.GetString("rol"),
                IdVeterinario = lector.IsDBNull("id_veterinario") ? null : lector.GetInt32("id_veterinario")
            });
        }
        return resultado;
    }

    public int GuardarVeterinario(VeterinarioModel veterinario)
    {
        SesionActual.ExigirRoles("Administrador");
        ValidarVeterinario(veterinario);
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            const string insertar = """
                INSERT INTO veterinarios
                  (codigo_veterinario, id_usuario, nombre_completo, numero_profesional,
                   especialidad, telefono, correo, activo)
                VALUES (@CodigoTemporal, @IdUsuario, @Nombre, @Numero, @Especialidad,
                        @Telefono, @Correo, @Activo);
                """;
            using MySqlCommand comando = new(insertar, conexion, transaccion);
            comando.Parameters.Add("@CodigoTemporal", MySqlDbType.VarChar).Value = $"TMP-{Guid.NewGuid():N}"[..20];
            AgregarParametrosVeterinario(comando, veterinario);
            comando.ExecuteNonQuery();
            int id = checked((int)comando.LastInsertedId);
            string codigo = $"VET-{id:000000}";
            using MySqlCommand actualizar = new("UPDATE veterinarios SET codigo_veterinario = @Codigo WHERE id_veterinario = @Id;", conexion, transaccion);
            actualizar.Parameters.Add("@Codigo", MySqlDbType.VarChar).Value = codigo;
            actualizar.Parameters.Add("@Id", MySqlDbType.Int32).Value = id;
            actualizar.ExecuteNonQuery();
            transaccion.Commit();
            return id;
        }
        catch
        {
            transaccion.Rollback();
            throw;
        }
    }

    public void ActualizarVeterinario(VeterinarioModel veterinario)
    {
        SesionActual.ExigirRoles("Administrador");
        if (veterinario.IdVeterinario <= 0)
        {
            throw new ArgumentException("Seleccione un veterinario válido.");
        }
        ValidarVeterinario(veterinario);
        const string sql = """
            UPDATE veterinarios SET id_usuario = @IdUsuario, nombre_completo = @Nombre,
                numero_profesional = @Numero, especialidad = @Especialidad,
                telefono = @Telefono, correo = @Correo, activo = @Activo
            WHERE id_veterinario = @Id;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Id", MySqlDbType.Int32).Value = veterinario.IdVeterinario;
        AgregarParametrosVeterinario(comando, veterinario);
        if (comando.ExecuteNonQuery() != 1)
        {
            throw new InvalidOperationException("No fue posible actualizar el veterinario.");
        }
    }

    public List<HorarioVeterinarioModel> ListarHorarios(int idVeterinario)
    {
        ExigirLectura();
        const string sql = """
            SELECT id_horario, id_veterinario, dia_semana, hora_inicio, hora_fin, activo
            FROM veterinario_horarios
            WHERE id_veterinario = @Id
            ORDER BY dia_semana, hora_inicio;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Id", MySqlDbType.Int32).Value = idVeterinario;
        using MySqlDataReader lector = comando.ExecuteReader();
        List<HorarioVeterinarioModel> resultado = new();
        while (lector.Read())
        {
            resultado.Add(new HorarioVeterinarioModel
            {
                IdHorario = lector.GetInt64("id_horario"),
                IdVeterinario = lector.GetInt32("id_veterinario"),
                DiaSemana = lector.GetByte("dia_semana"),
                HoraInicio = lector.GetTimeSpan("hora_inicio"),
                HoraFin = lector.GetTimeSpan("hora_fin"),
                Activo = lector.GetBoolean("activo")
            });
        }
        return resultado;
    }

    public long GuardarHorario(HorarioVeterinarioModel horario)
    {
        SesionActual.ExigirRoles("Administrador");
        if (horario.IdVeterinario <= 0 || horario.DiaSemana is < 1 or > 7 || horario.HoraFin <= horario.HoraInicio)
        {
            throw new ArgumentException("El horario indicado no es válido.");
        }
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        const string cruce = """
            SELECT COUNT(*) FROM veterinario_horarios
            WHERE id_veterinario = @Id AND dia_semana = @Dia AND activo = 1
              AND hora_inicio < @Fin AND hora_fin > @Inicio;
            """;
        using (MySqlCommand validar = new(cruce, conexion))
        {
            validar.Parameters.Add("@Id", MySqlDbType.Int32).Value = horario.IdVeterinario;
            validar.Parameters.Add("@Dia", MySqlDbType.Byte).Value = horario.DiaSemana;
            validar.Parameters.Add("@Inicio", MySqlDbType.Time).Value = horario.HoraInicio;
            validar.Parameters.Add("@Fin", MySqlDbType.Time).Value = horario.HoraFin;
            if (Convert.ToInt32(validar.ExecuteScalar()) > 0)
            {
                throw new InvalidOperationException("El intervalo se solapa con otro horario activo del veterinario.");
            }
        }
        const string insertar = """
            INSERT INTO veterinario_horarios (id_veterinario, dia_semana, hora_inicio, hora_fin, activo)
            VALUES (@Id, @Dia, @Inicio, @Fin, 1);
            """;
        using MySqlCommand comando = new(insertar, conexion);
        comando.Parameters.Add("@Id", MySqlDbType.Int32).Value = horario.IdVeterinario;
        comando.Parameters.Add("@Dia", MySqlDbType.Byte).Value = horario.DiaSemana;
        comando.Parameters.Add("@Inicio", MySqlDbType.Time).Value = horario.HoraInicio;
        comando.Parameters.Add("@Fin", MySqlDbType.Time).Value = horario.HoraFin;
        comando.ExecuteNonQuery();
        return comando.LastInsertedId;
    }

    public void CambiarEstadoHorario(long idHorario, bool activo)
    {
        SesionActual.ExigirRoles("Administrador");
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new("UPDATE veterinario_horarios SET activo = @Activo WHERE id_horario = @Id;", conexion);
        comando.Parameters.Add("@Activo", MySqlDbType.Bit).Value = activo;
        comando.Parameters.Add("@Id", MySqlDbType.Int64).Value = idHorario;
        comando.ExecuteNonQuery();
    }

    public List<CatalogoSimpleModel> ListarTiposBloqueo()
    {
        ExigirLectura();
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new("SELECT id_tipo_bloqueo, nombre FROM tipos_bloqueo WHERE activo = 1 ORDER BY nombre;", conexion);
        using MySqlDataReader lector = comando.ExecuteReader();
        List<CatalogoSimpleModel> resultado = new();
        while (lector.Read())
        {
            resultado.Add(new CatalogoSimpleModel { Id = lector.GetInt32(0), Nombre = lector.GetString(1) });
        }
        return resultado;
    }

    public List<BloqueoVeterinarioModel> ListarBloqueos(int idVeterinario, DateTime desde, DateTime hasta)
    {
        ExigirLectura();
        const string sql = """
            SELECT b.id_bloqueo, b.id_veterinario, b.id_tipo_bloqueo, t.nombre tipo,
                   b.fecha_hora_inicio, b.fecha_hora_fin, b.motivo, b.estado,
                   u.nombre_completo usuario_creacion
            FROM veterinario_bloqueos b
            INNER JOIN tipos_bloqueo t ON t.id_tipo_bloqueo = b.id_tipo_bloqueo
            INNER JOIN usuarios u ON u.id_usuario = b.id_usuario_creacion
            WHERE b.id_veterinario = @Id
              AND b.fecha_hora_inicio < @Hasta AND b.fecha_hora_fin > @Desde
            ORDER BY b.fecha_hora_inicio DESC;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Id", MySqlDbType.Int32).Value = idVeterinario;
        comando.Parameters.Add("@Desde", MySqlDbType.DateTime).Value = desde;
        comando.Parameters.Add("@Hasta", MySqlDbType.DateTime).Value = hasta;
        using MySqlDataReader lector = comando.ExecuteReader();
        List<BloqueoVeterinarioModel> resultado = new();
        while (lector.Read())
        {
            resultado.Add(new BloqueoVeterinarioModel
            {
                IdBloqueo = lector.GetInt64("id_bloqueo"), IdVeterinario = lector.GetInt32("id_veterinario"),
                IdTipoBloqueo = lector.GetInt32("id_tipo_bloqueo"), TipoBloqueo = lector.GetString("tipo"),
                FechaHoraInicio = lector.GetDateTime("fecha_hora_inicio"), FechaHoraFin = lector.GetDateTime("fecha_hora_fin"),
                Motivo = lector.GetString("motivo"), Estado = lector.GetString("estado"), UsuarioCreacion = lector.GetString("usuario_creacion")
            });
        }
        return resultado;
    }

    public long GuardarBloqueo(BloqueoVeterinarioModel bloqueo)
    {
        SesionActual.ExigirRoles("Administrador");
        if (bloqueo.IdVeterinario <= 0 || bloqueo.IdTipoBloqueo <= 0 || bloqueo.FechaHoraFin <= bloqueo.FechaHoraInicio || string.IsNullOrWhiteSpace(bloqueo.Motivo))
        {
            throw new ArgumentException("Complete correctamente los datos del bloqueo.");
        }
        if (SesionActual.Usuario is null)
        {
            throw new InvalidOperationException("No existe usuario autenticado.");
        }
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        const string solape = """
            SELECT COUNT(*) FROM veterinario_bloqueos
            WHERE id_veterinario = @Id AND estado = 'Vigente'
              AND fecha_hora_inicio < @Fin AND fecha_hora_fin > @Inicio;
            """;
        using (MySqlCommand validar = new(solape, conexion))
        {
            validar.Parameters.Add("@Id", MySqlDbType.Int32).Value = bloqueo.IdVeterinario;
            validar.Parameters.Add("@Inicio", MySqlDbType.DateTime).Value = bloqueo.FechaHoraInicio;
            validar.Parameters.Add("@Fin", MySqlDbType.DateTime).Value = bloqueo.FechaHoraFin;
            if (Convert.ToInt32(validar.ExecuteScalar()) > 0)
            {
                throw new InvalidOperationException("El veterinario ya posee un bloqueo vigente que se cruza con ese intervalo.");
            }
        }
        const string citasCruzadas = """
            SELECT COUNT(*) FROM citas
            WHERE id_veterinario = @Id AND estado NOT IN ('Cancelada','No asistió')
              AND fecha_hora_inicio < @Fin AND fecha_hora_fin > @Inicio;
            """;
        using (MySqlCommand validar = new(citasCruzadas, conexion))
        {
            validar.Parameters.Add("@Id", MySqlDbType.Int32).Value = bloqueo.IdVeterinario;
            validar.Parameters.Add("@Inicio", MySqlDbType.DateTime).Value = bloqueo.FechaHoraInicio;
            validar.Parameters.Add("@Fin", MySqlDbType.DateTime).Value = bloqueo.FechaHoraFin;
            if (Convert.ToInt32(validar.ExecuteScalar()) > 0)
            {
                throw new InvalidOperationException("No puede crear el bloqueo porque existen citas activas dentro del intervalo.");
            }
        }
        const string sql = """
            INSERT INTO veterinario_bloqueos
                (id_veterinario, id_tipo_bloqueo, fecha_hora_inicio, fecha_hora_fin, motivo, estado, id_usuario_creacion)
            VALUES (@Id, @Tipo, @Inicio, @Fin, @Motivo, 'Vigente', @Usuario);
            """;
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Id", MySqlDbType.Int32).Value = bloqueo.IdVeterinario;
        comando.Parameters.Add("@Tipo", MySqlDbType.Int32).Value = bloqueo.IdTipoBloqueo;
        comando.Parameters.Add("@Inicio", MySqlDbType.DateTime).Value = bloqueo.FechaHoraInicio;
        comando.Parameters.Add("@Fin", MySqlDbType.DateTime).Value = bloqueo.FechaHoraFin;
        comando.Parameters.Add("@Motivo", MySqlDbType.VarChar).Value = bloqueo.Motivo.Trim();
        comando.Parameters.Add("@Usuario", MySqlDbType.Int32).Value = SesionActual.Usuario!.IdUsuario;
        comando.ExecuteNonQuery();
        return comando.LastInsertedId;
    }

    public void CancelarBloqueo(long idBloqueo)
    {
        SesionActual.ExigirRoles("Administrador");
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        const string sql = """
            UPDATE veterinario_bloqueos SET estado = 'Cancelado', id_usuario_cancelacion = @Usuario,
                   fecha_cancelacion = CURRENT_TIMESTAMP
            WHERE id_bloqueo = @Id AND estado = 'Vigente';
            """;
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Usuario", MySqlDbType.Int32).Value = SesionActual.Usuario!.IdUsuario;
        comando.Parameters.Add("@Id", MySqlDbType.Int64).Value = idBloqueo;
        if (comando.ExecuteNonQuery() != 1)
        {
            throw new InvalidOperationException("El bloqueo no existe o ya fue cancelado.");
        }
    }

    private static void AgregarParametrosVeterinario(MySqlCommand comando, VeterinarioModel veterinario)
    {
        comando.Parameters.Add("@IdUsuario", MySqlDbType.Int32).Value = veterinario.IdUsuario.HasValue ? veterinario.IdUsuario.Value : DBNull.Value;
        comando.Parameters.Add("@Nombre", MySqlDbType.VarChar).Value = veterinario.NombreCompleto.Trim();
        comando.Parameters.Add("@Numero", MySqlDbType.VarChar).Value = VacioANulo(veterinario.NumeroProfesional);
        comando.Parameters.Add("@Especialidad", MySqlDbType.VarChar).Value = VacioANulo(veterinario.Especialidad);
        comando.Parameters.Add("@Telefono", MySqlDbType.VarChar).Value = VacioANulo(veterinario.Telefono);
        comando.Parameters.Add("@Correo", MySqlDbType.VarChar).Value = VacioANulo(veterinario.Correo);
        comando.Parameters.Add("@Activo", MySqlDbType.Bit).Value = veterinario.Activo;
    }

    private static object VacioANulo(string valor) => string.IsNullOrWhiteSpace(valor) ? DBNull.Value : valor.Trim();

    private static void ValidarVeterinario(VeterinarioModel veterinario)
    {
        if (string.IsNullOrWhiteSpace(veterinario.NombreCompleto))
        {
            throw new ArgumentException("El nombre completo del veterinario es obligatorio.");
        }
    }

    private static void ExigirLectura() => SesionActual.ExigirRoles("Administrador", "Recepción", "Veterinario");
}
