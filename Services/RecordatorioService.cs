using System;
using System.Collections.Generic;
using ClinicaVeterinaria.Data;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Utils;
using MySql.Data.MySqlClient;

namespace ClinicaVeterinaria.Services;

public sealed class RecordatorioService
{
    private static readonly HashSet<string> Tipos = new(StringComparer.OrdinalIgnoreCase)
    {
        "Vacuna", "Desparasitación", "Revisión clínica", "Cita por confirmar", "Control posoperatorio", "Manual"
    };

    private static readonly HashSet<string> Estados = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pendiente", "Contactado", "Pospuesto", "Completado", "Cancelado"
    };

    public ResumenRecordatoriosModel ObtenerResumen()
    {
        ExigirConsulta();
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(@"
SELECT
 (SELECT COUNT(*) FROM recordatorios WHERE estado IN ('Pendiente','Pospuesto') AND fecha_programada = CURDATE()) hoy,
 (SELECT COUNT(*) FROM recordatorios WHERE estado IN ('Pendiente','Pospuesto') AND fecha_programada BETWEEN CURDATE() AND DATE_ADD(CURDATE(), INTERVAL 7 DAY)) proximos,
 (SELECT COUNT(*) FROM recordatorios WHERE estado IN ('Pendiente','Pospuesto') AND fecha_programada < CURDATE()) vencidos,
 (SELECT COUNT(*) FROM recordatorios WHERE estado = 'Contactado') contactados,
 (SELECT COUNT(*) FROM recordatorios WHERE estado = 'Completado') completados;", conexion);
        using MySqlDataReader lector = comando.ExecuteReader();
        lector.Read();
        return new ResumenRecordatoriosModel
        {
            PendientesHoy = lector.GetInt32("hoy"),
            ProximosSieteDias = lector.GetInt32("proximos"),
            Vencidos = lector.GetInt32("vencidos"),
            Contactados = lector.GetInt32("contactados"),
            Completados = lector.GetInt32("completados")
        };
    }

    public List<RecordatorioModel> Listar(DateTime desde, DateTime hasta, string tipo, string estado, string filtro)
    {
        ExigirConsulta();
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(@"
SELECT r.id_recordatorio, r.id_mascota, m.codigo_paciente, m.nombre mascota, d.nombre_completo dueno,
 d.telefono_principal telefono, r.tipo_recordatorio, r.fecha_programada, r.descripcion, r.estado,
 r.fecha_contacto, COALESCE(r.observaciones_contacto, '') observaciones_contacto,
 DATEDIFF(r.fecha_programada, CURDATE()) dias_restantes
FROM recordatorios r
INNER JOIN mascotas m ON m.id_mascota = r.id_mascota
INNER JOIN duenos d ON d.id_dueno = m.id_dueno
WHERE r.fecha_programada BETWEEN @desde AND @hasta
 AND (@tipo = '' OR r.tipo_recordatorio = @tipo)
 AND (@estado = '' OR r.estado = @estado)
 AND (@filtro = '' OR m.nombre LIKE @buscar OR m.codigo_paciente LIKE @buscar
      OR d.nombre_completo LIKE @buscar OR d.telefono_principal LIKE @buscar OR r.descripcion LIKE @buscar)
ORDER BY r.fecha_programada, m.nombre;", conexion);
        comando.Parameters.AddWithValue("@desde", desde.Date);
        comando.Parameters.AddWithValue("@hasta", hasta.Date);
        comando.Parameters.AddWithValue("@tipo", tipo.Trim());
        comando.Parameters.AddWithValue("@estado", estado.Trim());
        comando.Parameters.AddWithValue("@filtro", filtro.Trim());
        comando.Parameters.AddWithValue("@buscar", $"%{filtro.Trim()}%");
        using MySqlDataReader lector = comando.ExecuteReader();
        List<RecordatorioModel> lista = new();
        while (lector.Read())
        {
            lista.Add(new RecordatorioModel
            {
                IdRecordatorio = lector.GetInt64("id_recordatorio"),
                IdMascota = lector.GetInt64("id_mascota"),
                CodigoPaciente = lector.GetString("codigo_paciente"),
                Mascota = lector.GetString("mascota"),
                Dueno = lector.GetString("dueno"),
                Telefono = lector.GetString("telefono"),
                TipoRecordatorio = lector.GetString("tipo_recordatorio"),
                FechaProgramada = lector.GetDateTime("fecha_programada"),
                Descripcion = lector.GetString("descripcion"),
                Estado = lector.GetString("estado"),
                FechaContacto = lector.IsDBNull(lector.GetOrdinal("fecha_contacto")) ? null : lector.GetDateTime("fecha_contacto"),
                ObservacionesContacto = lector.GetString("observaciones_contacto"),
                DiasRestantes = lector.GetInt32("dias_restantes")
            });
        }
        return lista;
    }

    public List<MascotaBusquedaModel> BuscarMascotas(string filtro)
    {
        ExigirEdicion();
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(@"
SELECT m.id_mascota, m.codigo_paciente, m.nombre, m.especie, d.nombre_completo dueno, d.telefono_principal
FROM mascotas m INNER JOIN duenos d ON d.id_dueno = m.id_dueno
WHERE m.activo = 1 AND m.estado_vital = 'Viva'
 AND (@filtro = '' OR m.nombre LIKE @buscar OR m.codigo_paciente LIKE @buscar OR d.nombre_completo LIKE @buscar OR d.telefono_principal LIKE @buscar)
ORDER BY m.nombre LIMIT 50;", conexion);
        comando.Parameters.AddWithValue("@filtro", filtro.Trim());
        comando.Parameters.AddWithValue("@buscar", $"%{filtro.Trim()}%");
        using MySqlDataReader lector = comando.ExecuteReader();
        List<MascotaBusquedaModel> lista = new();
        while (lector.Read())
        {
            lista.Add(new MascotaBusquedaModel
            {
                IdMascota = lector.GetInt64("id_mascota"), CodigoPaciente = lector.GetString("codigo_paciente"),
                NombreMascota = lector.GetString("nombre"), Especie = lector.GetString("especie"),
                Dueno = lector.GetString("dueno"), Telefono = lector.GetString("telefono_principal")
            });
        }
        return lista;
    }

    public long Crear(NuevoRecordatorioModel registro)
    {
        ExigirEdicion();
        ValidarNuevo(registro);
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(@"
INSERT INTO recordatorios (id_mascota, tipo_recordatorio, fecha_programada, descripcion, estado, id_usuario_creacion)
VALUES (@mascota, @tipo, @fecha, @descripcion, 'Pendiente', @usuario);", conexion);
        comando.Parameters.AddWithValue("@mascota", registro.IdMascota);
        comando.Parameters.AddWithValue("@tipo", registro.TipoRecordatorio);
        comando.Parameters.AddWithValue("@fecha", registro.FechaProgramada.Date);
        comando.Parameters.AddWithValue("@descripcion", registro.Descripcion.Trim());
        comando.Parameters.AddWithValue("@usuario", SesionActual.Usuario!.IdUsuario);
        comando.ExecuteNonQuery();
        return comando.LastInsertedId;
    }

    public void MarcarContactado(long idRecordatorio, string observacion)
    {
        ExigirEdicion();
        if (string.IsNullOrWhiteSpace(observacion)) throw new InvalidOperationException("Registre una nota del contacto realizado.");
        ActualizarEstado(idRecordatorio, "Contactado", observacion, incluirFechaContacto: true);
    }

    public void Posponer(long idRecordatorio, DateTime nuevaFecha, string observacion)
    {
        ExigirEdicion();
        if (nuevaFecha.Date < DateTime.Today) throw new InvalidOperationException("La nueva fecha no puede estar en el pasado.");
        if (string.IsNullOrWhiteSpace(observacion)) throw new InvalidOperationException("Indique el motivo del aplazamiento.");
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(@"
UPDATE recordatorios SET fecha_programada = @fecha, estado = 'Pospuesto', observaciones_contacto = @nota,
 id_usuario_modificacion = @usuario WHERE id_recordatorio = @id AND estado NOT IN ('Completado','Cancelado');", conexion);
        comando.Parameters.AddWithValue("@fecha", nuevaFecha.Date);
        comando.Parameters.AddWithValue("@nota", observacion.Trim());
        comando.Parameters.AddWithValue("@usuario", SesionActual.Usuario!.IdUsuario);
        comando.Parameters.AddWithValue("@id", idRecordatorio);
        if (comando.ExecuteNonQuery() == 0) throw new InvalidOperationException("El recordatorio ya está cerrado o no existe.");
    }

    public void MarcarCompletado(long idRecordatorio, string observacion)
    {
        ExigirEdicion();
        ActualizarEstado(idRecordatorio, "Completado", observacion, incluirFechaContacto: false);
    }

    public void Cancelar(long idRecordatorio, string observacion)
    {
        ExigirEdicion();
        if (string.IsNullOrWhiteSpace(observacion)) throw new InvalidOperationException("Indique el motivo de cancelación.");
        ActualizarEstado(idRecordatorio, "Cancelado", observacion, incluirFechaContacto: false);
    }

    private static void ActualizarEstado(long id, string estado, string observacion, bool incluirFechaContacto)
    {
        if (!Estados.Contains(estado)) throw new InvalidOperationException("Estado no válido.");
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        string sql = incluirFechaContacto
            ? @"UPDATE recordatorios SET estado = @estado, fecha_contacto = NOW(), observaciones_contacto = @nota, id_usuario_modificacion = @usuario WHERE id_recordatorio = @id AND estado NOT IN ('Completado','Cancelado');"
            : @"UPDATE recordatorios SET estado = @estado, observaciones_contacto = @nota, id_usuario_modificacion = @usuario WHERE id_recordatorio = @id AND estado <> 'Cancelado';";
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.AddWithValue("@estado", estado);
        comando.Parameters.AddWithValue("@nota", string.IsNullOrWhiteSpace(observacion) ? (object)DBNull.Value : observacion.Trim());
        comando.Parameters.AddWithValue("@usuario", SesionActual.Usuario!.IdUsuario);
        comando.Parameters.AddWithValue("@id", id);
        if (comando.ExecuteNonQuery() == 0) throw new InvalidOperationException("El recordatorio no puede actualizarse en su estado actual.");
    }

    private static void ValidarNuevo(NuevoRecordatorioModel registro)
    {
        if (registro.IdMascota <= 0) throw new InvalidOperationException("Seleccione una mascota.");
        if (!Tipos.Contains(registro.TipoRecordatorio)) throw new InvalidOperationException("Seleccione un tipo de recordatorio válido.");
        if (registro.FechaProgramada.Date < DateTime.Today) throw new InvalidOperationException("La fecha programada no puede estar en el pasado.");
        if (string.IsNullOrWhiteSpace(registro.Descripcion)) throw new InvalidOperationException("La descripción es obligatoria.");
    }

    private static void ExigirConsulta() => SesionActual.ExigirRoles("Administrador", "Recepción", "Veterinario");
    private static void ExigirEdicion() => SesionActual.ExigirRoles("Administrador", "Recepción", "Veterinario");
}
