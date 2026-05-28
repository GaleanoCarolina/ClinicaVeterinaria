using System;
using System.Configuration;
using MySql.Data.MySqlClient;

namespace ClinicaVeterinaria.Data;

public static class Database
{
    private static string ObtenerCadenaConexion()
    {
        string? cadena = ConfigurationManager.ConnectionStrings["ClinicaDb"]?.ConnectionString;

        if (string.IsNullOrWhiteSpace(cadena))
        {
            throw new InvalidOperationException(
                "No se encontró la cadena de conexión 'ClinicaDb' en App.config.");
        }

        return cadena;
    }

    public static MySqlConnection CrearConexion()
    {
        return new MySqlConnection(ObtenerCadenaConexion());
    }

    public static void ProbarConexion()
    {
        using MySqlConnection conexion = CrearConexion();
        conexion.Open();

        using MySqlCommand comando = new("SELECT DATABASE();", conexion);
        string? baseActual = Convert.ToString(comando.ExecuteScalar());

        if (!string.Equals(baseActual, "clinica_veterinaria", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "La conexión se abrió, pero no apunta a la base clinica_veterinaria.");
        }
    }
}
