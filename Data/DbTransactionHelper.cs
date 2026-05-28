using System;
using MySql.Data.MySqlClient;

namespace ClinicaVeterinaria.Data;

public static class DbTransactionHelper
{
    public static void Ejecutar(Action<MySqlConnection, MySqlTransaction> operacion)
    {
        ArgumentNullException.ThrowIfNull(operacion);

        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlTransaction transaccion = conexion.BeginTransaction();

        try
        {
            operacion(conexion, transaccion);
            transaccion.Commit();
        }
        catch
        {
            transaccion.Rollback();
            throw;
        }
    }

    public static T Ejecutar<T>(Func<MySqlConnection, MySqlTransaction, T> operacion)
    {
        ArgumentNullException.ThrowIfNull(operacion);

        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlTransaction transaccion = conexion.BeginTransaction();

        try
        {
            T resultado = operacion(conexion, transaccion);
            transaccion.Commit();
            return resultado;
        }
        catch
        {
            transaccion.Rollback();
            throw;
        }
    }
}
