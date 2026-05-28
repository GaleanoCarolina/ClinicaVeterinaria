using System;
using System.Collections.Generic;
using ClinicaVeterinaria.Data;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Utils;
using MySql.Data.MySqlClient;

namespace ClinicaVeterinaria.Services;

public sealed class VacunacionService
{
    public List<VacunaModel> ListarVacunas()
    {
        ExigirLecturaClinica();
        const string sql = """
            SELECT id_vacuna, codigo, nombre, COALESCE(especie_aplicable, '') especie_aplicable,
                   intervalo_dias_sugerido, precio_base, controla_inventario, id_producto_inventario
            FROM catalogo_vacunas WHERE activo = 1 ORDER BY nombre;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        using MySqlDataReader lector = comando.ExecuteReader();
        List<VacunaModel> lista = new();
        while (lector.Read())
        {
            lista.Add(new VacunaModel
            {
                IdVacuna = lector.GetInt32("id_vacuna"), Codigo = lector.GetString("codigo"), Nombre = lector.GetString("nombre"),
                EspecieAplicable = lector.GetString("especie_aplicable"),
                IntervaloDiasSugerido = lector.IsDBNull(lector.GetOrdinal("intervalo_dias_sugerido")) ? null : lector.GetInt32("intervalo_dias_sugerido"),
                PrecioBase = lector.GetDecimal("precio_base"), ControlaInventario = lector.GetBoolean("controla_inventario"),
                IdProductoInventario = lector.IsDBNull(lector.GetOrdinal("id_producto_inventario")) ? null : lector.GetInt64("id_producto_inventario")
            });
        }
        return lista;
    }

    public List<DesparasitanteModel> ListarDesparasitantes()
    {
        ExigirLecturaClinica();
        const string sql = """
            SELECT id_desparasitante, codigo, nombre, COALESCE(presentacion, '') presentacion,
                   COALESCE(dosis_sugerida, '') dosis_sugerida, intervalo_dias_sugerido,
                   precio_base, controla_inventario, id_producto_inventario
            FROM catalogo_desparasitantes WHERE activo = 1 ORDER BY nombre;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        using MySqlDataReader lector = comando.ExecuteReader();
        List<DesparasitanteModel> lista = new();
        while (lector.Read())
        {
            lista.Add(new DesparasitanteModel
            {
                IdDesparasitante = lector.GetInt32("id_desparasitante"), Codigo = lector.GetString("codigo"), Nombre = lector.GetString("nombre"),
                Presentacion = lector.GetString("presentacion"), DosisSugerida = lector.GetString("dosis_sugerida"),
                IntervaloDiasSugerido = lector.IsDBNull(lector.GetOrdinal("intervalo_dias_sugerido")) ? null : lector.GetInt32("intervalo_dias_sugerido"),
                PrecioBase = lector.GetDecimal("precio_base"), ControlaInventario = lector.GetBoolean("controla_inventario"),
                IdProductoInventario = lector.IsDBNull(lector.GetOrdinal("id_producto_inventario")) ? null : lector.GetInt64("id_producto_inventario")
            });
        }
        return lista;
    }

    public List<LoteDisponibleModel> ListarLotesDisponibles(long? idProductoInventario)
    {
        ExigirLecturaClinica();
        if (!idProductoInventario.HasValue) return new List<LoteDisponibleModel>();
        const string sql = """
            SELECT id_lote, id_producto, numero_lote, fecha_vencimiento, cantidad_disponible
            FROM inventario_lotes
            WHERE id_producto = @Producto AND estado = 'Disponible' AND cantidad_disponible > 0
              AND (fecha_vencimiento IS NULL OR fecha_vencimiento >= CURDATE())
            ORDER BY fecha_vencimiento IS NULL, fecha_vencimiento, fecha_ingreso;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Producto", MySqlDbType.UInt64).Value = idProductoInventario.Value;
        using MySqlDataReader lector = comando.ExecuteReader();
        List<LoteDisponibleModel> lotes = new();
        while (lector.Read())
        {
            lotes.Add(new LoteDisponibleModel
            {
                IdLote = lector.GetInt64("id_lote"), IdProducto = lector.GetInt64("id_producto"), NumeroLote = lector.GetString("numero_lote"),
                FechaVencimiento = lector.IsDBNull(lector.GetOrdinal("fecha_vencimiento")) ? null : lector.GetDateTime("fecha_vencimiento"),
                CantidadDisponible = lector.GetDecimal("cantidad_disponible")
            });
        }
        return lotes;
    }

    private static void ExigirLecturaClinica() => SesionActual.ExigirRoles("Administrador", "Veterinario");
}
