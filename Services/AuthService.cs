using System;
using ClinicaVeterinaria.Data;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Utils;
using MySql.Data.MySqlClient;

namespace ClinicaVeterinaria.Services;

public sealed class AuthService
{
    public UsuarioModel? Autenticar(string nombreUsuario, string password)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        const string sql = """
            SELECT u.id_usuario, u.id_rol, u.nombre_usuario, u.password_hash,
                   u.nombre_completo, u.ultimo_acceso, r.nombre AS rol,
                   v.id_veterinario
            FROM usuarios u
            INNER JOIN roles r ON r.id_rol = u.id_rol AND r.activo = 1
            LEFT JOIN veterinarios v ON v.id_usuario = u.id_usuario AND v.activo = 1
            WHERE u.nombre_usuario = @Usuario
              AND u.activo = 1
            LIMIT 1;
            """;

        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();

        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Usuario", MySqlDbType.VarChar).Value = nombreUsuario.Trim();

        using MySqlDataReader lector = comando.ExecuteReader();
        if (!lector.Read())
        {
            return null;
        }

        string hashGuardado = Convert.ToString(lector["password_hash"]) ?? string.Empty;
        if (!PasswordHasher.Verificar(password, hashGuardado))
        {
            return null;
        }

        UsuarioModel usuario = new()
        {
            IdUsuario = Convert.ToInt32(lector["id_usuario"]),
            IdRol = Convert.ToInt32(lector["id_rol"]),
            NombreUsuario = Convert.ToString(lector["nombre_usuario"]) ?? string.Empty,
            NombreCompleto = Convert.ToString(lector["nombre_completo"]) ?? string.Empty,
            Rol = Convert.ToString(lector["rol"]) ?? string.Empty,
            UltimoAcceso = lector.IsDBNull(lector.GetOrdinal("ultimo_acceso"))
                ? null
                : Convert.ToDateTime(lector["ultimo_acceso"]),
            IdVeterinario = lector.IsDBNull(lector.GetOrdinal("id_veterinario"))
                ? null
                : Convert.ToInt32(lector["id_veterinario"])
        };

        lector.Close();
        ActualizarUltimoAcceso(usuario.IdUsuario, conexion);
        return usuario;
    }

    private static void ActualizarUltimoAcceso(int idUsuario, MySqlConnection conexion)
    {
        const string sql = """
            UPDATE usuarios
            SET ultimo_acceso = CURRENT_TIMESTAMP
            WHERE id_usuario = @IdUsuario;
            """;

        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@IdUsuario", MySqlDbType.Int32).Value = idUsuario;
        comando.ExecuteNonQuery();
    }
}
