using System;
using System.Collections.Generic;
using ClinicaVeterinaria.Data;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Utils;
using MySql.Data.MySqlClient;

namespace ClinicaVeterinaria.Services;

public sealed class DisponibilidadService
{
    public List<DisponibilidadBloqueModel> ObtenerHorariosDisponibles(
        int idVeterinario, DateTime fecha, int duracionMinutos, long? idMascota = null, long? excluirIdCita = null)
    {
        ExigirLectura();
        ValidarDuracion(duracionMinutos);

        if (fecha.Date < DateTime.Today)
        {
            return new List<DisponibilidadBloqueModel>();
        }

        byte diaSemana = ObtenerDiaSemana(fecha);
        List<(TimeSpan Inicio, TimeSpan Fin)> jornadas = ObtenerJornadas(idVeterinario, diaSemana);
        List<DisponibilidadBloqueModel> resultado = new();
        foreach ((TimeSpan inicio, TimeSpan fin) in jornadas)
        {
            DateTime cursor = fecha.Date.Add(inicio);
            DateTime finJornada = fecha.Date.Add(fin);
            while (cursor.AddMinutes(duracionMinutos) <= finJornada)
            {
                DateTime final = cursor.AddMinutes(duracionMinutos);

                // Para el día actual solo se muestran bloques que todavía pueden reservarse.
                // El bloque debe comenzar después de la hora actual.
                if (fecha.Date == DateTime.Today && cursor <= DateTime.Now)
                {
                    cursor = cursor.AddMinutes(30);
                    continue;
                }

                string motivo = ObtenerMotivoNoDisponible(idVeterinario, cursor, final, idMascota, excluirIdCita);
                resultado.Add(new DisponibilidadBloqueModel
                {
                    FechaHoraInicio = cursor,
                    FechaHoraFin = final,
                    Disponible = string.IsNullOrEmpty(motivo),
                    MotivoNoDisponible = motivo
                });
                cursor = cursor.AddMinutes(30);
            }
        }
        return resultado;
    }

    public void ValidarReserva(int idVeterinario, DateTime inicio, int duracionMinutos, long idMascota, long? excluirIdCita = null)
    {
        ExigirLectura();
        ValidarDuracion(duracionMinutos);

        if (inicio <= DateTime.Now)
        {
            throw new InvalidOperationException("No se puede agendar una cita en una fecha u hora que ya pasó.");
        }

        if (inicio.Second != 0 || inicio.Millisecond != 0 || inicio.Minute % 30 != 0)
        {
            throw new InvalidOperationException("La hora de la cita debe iniciar en un bloque exacto de 30 minutos.");
        }
        DateTime fin = inicio.AddMinutes(duracionMinutos);
        if (!IntervaloDentroDeJornada(idVeterinario, inicio, fin))
        {
            throw new InvalidOperationException("El intervalo seleccionado está fuera de la jornada laboral del veterinario.");
        }
        string motivo = ObtenerMotivoNoDisponible(idVeterinario, inicio, fin, idMascota, excluirIdCita);
        if (!string.IsNullOrEmpty(motivo))
        {
            throw new InvalidOperationException(motivo);
        }
    }

    public bool IntervaloDentroDeJornada(int idVeterinario, DateTime inicio, DateTime fin)
    {
        byte dia = ObtenerDiaSemana(inicio);
        const string sql = """
            SELECT COUNT(*) FROM veterinario_horarios
            WHERE id_veterinario = @Veterinario AND dia_semana = @Dia AND activo = 1
              AND hora_inicio <= @HoraInicio AND hora_fin >= @HoraFin;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Veterinario", MySqlDbType.Int32).Value = idVeterinario;
        comando.Parameters.Add("@Dia", MySqlDbType.Byte).Value = dia;
        comando.Parameters.Add("@HoraInicio", MySqlDbType.Time).Value = inicio.TimeOfDay;
        comando.Parameters.Add("@HoraFin", MySqlDbType.Time).Value = fin.TimeOfDay;
        return Convert.ToInt32(comando.ExecuteScalar()) > 0;
    }

    private static List<(TimeSpan Inicio, TimeSpan Fin)> ObtenerJornadas(int idVeterinario, byte dia)
    {
        const string sql = """
            SELECT hora_inicio, hora_fin FROM veterinario_horarios
            WHERE id_veterinario = @Veterinario AND dia_semana = @Dia AND activo = 1
            ORDER BY hora_inicio;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Veterinario", MySqlDbType.Int32).Value = idVeterinario;
        comando.Parameters.Add("@Dia", MySqlDbType.Byte).Value = dia;
        using MySqlDataReader lector = comando.ExecuteReader();
        List<(TimeSpan Inicio, TimeSpan Fin)> resultado = new();
        while (lector.Read())
        {
            resultado.Add((lector.GetTimeSpan(0), lector.GetTimeSpan(1)));
        }
        return resultado;
    }

    private static string ObtenerMotivoNoDisponible(int idVeterinario, DateTime inicio, DateTime fin, long? idMascota, long? excluirIdCita)
    {
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        const string bloqueo = """
            SELECT COUNT(*) FROM veterinario_bloqueos
            WHERE id_veterinario = @Veterinario AND estado = 'Vigente'
              AND fecha_hora_inicio < @Fin AND fecha_hora_fin > @Inicio;
            """;
        using (MySqlCommand comando = new(bloqueo, conexion))
        {
            comando.Parameters.Add("@Veterinario", MySqlDbType.Int32).Value = idVeterinario;
            comando.Parameters.Add("@Inicio", MySqlDbType.DateTime).Value = inicio;
            comando.Parameters.Add("@Fin", MySqlDbType.DateTime).Value = fin;
            if (Convert.ToInt32(comando.ExecuteScalar()) > 0)
            {
                return "El veterinario tiene un bloqueo vigente en el intervalo seleccionado.";
            }
        }
        const string ocupado = """
            SELECT COUNT(*) FROM cita_bloques cb
            INNER JOIN citas c ON c.id_cita = cb.id_cita
            WHERE cb.id_veterinario = @Veterinario
              AND cb.fecha_hora_bloque >= @Inicio AND cb.fecha_hora_bloque < @Fin
              AND (@Excluir IS NULL OR c.id_cita <> @Excluir);
            """;
        using (MySqlCommand comando = new(ocupado, conexion))
        {
            comando.Parameters.Add("@Veterinario", MySqlDbType.Int32).Value = idVeterinario;
            comando.Parameters.Add("@Inicio", MySqlDbType.DateTime).Value = inicio;
            comando.Parameters.Add("@Fin", MySqlDbType.DateTime).Value = fin;
            comando.Parameters.Add("@Excluir", MySqlDbType.UInt64).Value = excluirIdCita.HasValue ? excluirIdCita.Value : DBNull.Value;
            if (Convert.ToInt32(comando.ExecuteScalar()) > 0)
            {
                return "El veterinario ya tiene una cita que ocupa uno o más bloques seleccionados.";
            }
        }
        if (idMascota.HasValue)
        {
            const string mascota = """
                SELECT COUNT(*) FROM citas
                WHERE id_mascota = @Mascota
                  AND estado NOT IN ('Cancelada','No asistió')
                  AND fecha_hora_inicio < @Fin AND fecha_hora_fin > @Inicio
                  AND (@Excluir IS NULL OR id_cita <> @Excluir);
                """;
            using MySqlCommand comando = new(mascota, conexion);
            comando.Parameters.Add("@Mascota", MySqlDbType.UInt64).Value = idMascota.Value;
            comando.Parameters.Add("@Inicio", MySqlDbType.DateTime).Value = inicio;
            comando.Parameters.Add("@Fin", MySqlDbType.DateTime).Value = fin;
            comando.Parameters.Add("@Excluir", MySqlDbType.UInt64).Value = excluirIdCita.HasValue ? excluirIdCita.Value : DBNull.Value;
            if (Convert.ToInt32(comando.ExecuteScalar()) > 0)
            {
                return "La mascota ya tiene otra cita activa que se cruza con este horario.";
            }
        }
        return string.Empty;
    }

    private static void ValidarDuracion(int duracionMinutos)
    {
        if (duracionMinutos <= 0 || duracionMinutos % 30 != 0)
        {
            throw new ArgumentException("La duración debe ser un múltiplo positivo de 30 minutos.");
        }
    }

    private static byte ObtenerDiaSemana(DateTime fecha) => fecha.DayOfWeek == DayOfWeek.Sunday ? (byte)7 : (byte)fecha.DayOfWeek;
    private static void ExigirLectura() => SesionActual.ExigirRoles("Administrador", "Recepción", "Veterinario");
}
