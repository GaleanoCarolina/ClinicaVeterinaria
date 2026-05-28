using System;
using System.Collections.Generic;
using ClinicaVeterinaria.Data;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Utils;
using MySql.Data.MySqlClient;

namespace ClinicaVeterinaria.Services;

public sealed class RecetaService
{
    public List<MedicamentoModel> ListarMedicamentos()
    {
        ExigirLecturaClinica();
        const string sql = """
            SELECT id_medicamento, codigo, nombre, COALESCE(presentacion, '') presentacion,
                   COALESCE(concentracion, '') concentracion,
                   COALESCE(via_administracion, '') via_administracion,
                   COALESCE(indicaciones_predeterminadas, '') indicaciones_predeterminadas,
                   precio_venta, controla_inventario
            FROM catalogo_medicamentos
            WHERE activo = 1
            ORDER BY nombre;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        using MySqlDataReader lector = comando.ExecuteReader();
        List<MedicamentoModel> lista = new();
        while (lector.Read())
        {
            lista.Add(new MedicamentoModel
            {
                IdMedicamento = lector.GetInt32("id_medicamento"),
                Codigo = lector.GetString("codigo"),
                Nombre = lector.GetString("nombre"),
                Presentacion = lector.GetString("presentacion"),
                Concentracion = lector.GetString("concentracion"),
                ViaAdministracion = lector.GetString("via_administracion"),
                IndicacionesPredeterminadas = lector.GetString("indicaciones_predeterminadas"),
                PrecioVenta = lector.GetDecimal("precio_venta"),
                ControlaInventario = lector.GetBoolean("controla_inventario")
            });
        }
        return lista;
    }

    public RecetaModel? ObtenerRecetaPorConsulta(long idConsulta)
    {
        ExigirLecturaClinica();
        const string cabeceraSql = """
            SELECT id_receta, id_consulta, fecha_emision, COALESCE(indicaciones_generales, '') indicaciones_generales
            FROM recetas WHERE id_consulta = @Consulta ORDER BY id_receta DESC LIMIT 1;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand cabecera = new(cabeceraSql, conexion);
        cabecera.Parameters.Add("@Consulta", MySqlDbType.UInt64).Value = idConsulta;
        using MySqlDataReader lector = cabecera.ExecuteReader();
        if (!lector.Read()) return null;
        RecetaModel receta = new()
        {
            IdReceta = lector.GetInt64("id_receta"),
            IdConsulta = lector.GetInt64("id_consulta"),
            FechaEmision = lector.GetDateTime("fecha_emision"),
            IndicacionesGenerales = lector.GetString("indicaciones_generales")
        };
        lector.Close();
        const string detalleSql = """
            SELECT rd.id_detalle, rd.id_medicamento, COALESCE(cm.nombre, '') medicamento,
                   COALESCE(rd.medicamento_libre, '') medicamento_libre,
                   COALESCE(rd.presentacion, '') presentacion, COALESCE(rd.concentracion, '') concentracion,
                   rd.dosis, rd.frecuencia, rd.duracion, COALESCE(rd.cantidad, '') cantidad,
                   COALESCE(rd.via_administracion, '') via_administracion, COALESCE(rd.indicaciones, '') indicaciones
            FROM receta_detalles rd
            LEFT JOIN catalogo_medicamentos cm ON cm.id_medicamento = rd.id_medicamento
            WHERE rd.id_receta = @Receta ORDER BY rd.id_detalle;
            """;
        using MySqlCommand detalle = new(detalleSql, conexion);
        detalle.Parameters.Add("@Receta", MySqlDbType.UInt64).Value = receta.IdReceta;
        using MySqlDataReader lectorDetalle = detalle.ExecuteReader();
        while (lectorDetalle.Read())
        {
            receta.Detalles.Add(new RecetaDetalleModel
            {
                IdDetalle = lectorDetalle.GetInt64("id_detalle"),
                IdMedicamento = lectorDetalle.IsDBNull(lectorDetalle.GetOrdinal("id_medicamento")) ? null : lectorDetalle.GetInt32("id_medicamento"),
                Medicamento = lectorDetalle.GetString("medicamento"), MedicamentoLibre = lectorDetalle.GetString("medicamento_libre"),
                Presentacion = lectorDetalle.GetString("presentacion"), Concentracion = lectorDetalle.GetString("concentracion"),
                Dosis = lectorDetalle.GetString("dosis"), Frecuencia = lectorDetalle.GetString("frecuencia"),
                Duracion = lectorDetalle.GetString("duracion"), Cantidad = lectorDetalle.GetString("cantidad"),
                ViaAdministracion = lectorDetalle.GetString("via_administracion"), Indicaciones = lectorDetalle.GetString("indicaciones")
            });
        }
        return receta;
    }

    private static void ExigirLecturaClinica() => SesionActual.ExigirRoles("Administrador", "Veterinario");
}
