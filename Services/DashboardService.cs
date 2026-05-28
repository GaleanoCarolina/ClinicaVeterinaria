using System;
using System.Collections.Generic;
using ClinicaVeterinaria.Data;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Utils;
using MySql.Data.MySqlClient;

namespace ClinicaVeterinaria.Services;

public sealed class DashboardService
{
    public DashboardResumenModel ObtenerResumen(DateTime fecha)
    {
        UsuarioModel usuario = ObtenerUsuario();
        bool filtrarVeterinario = string.Equals(usuario.Rol, "Veterinario", StringComparison.OrdinalIgnoreCase);
        bool puedeVerFinanzas = SesionActual.EsRol("Administrador", "Caja");
        bool puedeVerInventario = SesionActual.EsRol("Administrador");

        const string sql = """
            SELECT
              (SELECT COUNT(*) FROM citas c
                WHERE DATE(c.fecha_hora_inicio) = @Fecha
                  AND (@FiltrarVet = 0 OR c.id_veterinario = @IdVeterinario)) AS citas_hoy,
              (SELECT COUNT(*) FROM citas c
                WHERE DATE(c.fecha_hora_inicio) = @Fecha AND c.estado = 'Llegó'
                  AND (@FiltrarVet = 0 OR c.id_veterinario = @IdVeterinario)) AS en_espera,
              (SELECT COUNT(*) FROM citas c
                WHERE DATE(c.fecha_hora_inicio) = @Fecha AND c.estado = 'En consulta'
                  AND (@FiltrarVet = 0 OR c.id_veterinario = @IdVeterinario)) AS en_consulta,
              (SELECT COUNT(*) FROM citas c
                WHERE DATE(c.fecha_hora_inicio) = @Fecha AND c.estado = 'Atendida'
                  AND (@FiltrarVet = 0 OR c.id_veterinario = @IdVeterinario)) AS terminadas,
              (SELECT COUNT(*) FROM citas c
                WHERE DATE(c.fecha_hora_inicio) = @Fecha AND c.estado IN ('Cancelada','No asistió')
                  AND (@FiltrarVet = 0 OR c.id_veterinario = @IdVeterinario)) AS canceladas,
              (SELECT CASE WHEN @VerFinanzas = 1 THEN COUNT(*) ELSE 0 END
                FROM facturas f WHERE f.estado IN ('Emitida','Parcialmente pagada')) AS facturas_pendientes,
              (SELECT CASE WHEN @VerFinanzas = 1 THEN COALESCE(SUM(p.monto), 0) ELSE 0 END
                FROM pagos p WHERE DATE(p.fecha_pago) = @Fecha AND p.estado = 'Aplicado') AS cobrado_hoy,
              (SELECT COUNT(*) FROM recordatorios r
                WHERE r.estado IN ('Pendiente','Pospuesto')
                  AND r.fecha_programada BETWEEN @Fecha AND DATE_ADD(@Fecha, INTERVAL 7 DAY)) AS recordatorios,
              (SELECT CASE WHEN @VerInventario = 1 THEN COUNT(*) ELSE 0 END
                FROM inventario_productos p
                LEFT JOIN (
                    SELECT id_producto, SUM(cantidad_disponible) stock
                    FROM inventario_lotes
                    WHERE estado IN ('Disponible','Agotado')
                    GROUP BY id_producto
                ) s ON s.id_producto = p.id_producto
                WHERE p.activo = 1 AND COALESCE(s.stock, 0) <= p.stock_minimo) AS stock_bajo,
              (SELECT CASE WHEN @VerInventario = 1 THEN COUNT(*) ELSE 0 END
                FROM inventario_lotes l
                WHERE l.estado = 'Disponible' AND l.cantidad_disponible > 0
                  AND l.fecha_vencimiento BETWEEN @Fecha AND DATE_ADD(@Fecha, INTERVAL 30 DAY)) AS por_vencer;
            """;

        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        AgregarParametrosPanel(comando, fecha.Date, filtrarVeterinario, usuario.IdVeterinario, puedeVerFinanzas, puedeVerInventario);

        using MySqlDataReader lector = comando.ExecuteReader();
        if (!lector.Read())
        {
            return new DashboardResumenModel();
        }

        return new DashboardResumenModel
        {
            CitasHoy = Convert.ToInt32(lector["citas_hoy"]),
            PacientesEnEspera = Convert.ToInt32(lector["en_espera"]),
            PacientesEnConsulta = Convert.ToInt32(lector["en_consulta"]),
            ConsultasTerminadasHoy = Convert.ToInt32(lector["terminadas"]),
            CanceladasNoAsistidasHoy = Convert.ToInt32(lector["canceladas"]),
            FacturasPendientes = Convert.ToInt32(lector["facturas_pendientes"]),
            TotalCobradoHoy = Convert.ToDecimal(lector["cobrado_hoy"]),
            RecordatoriosProximos = Convert.ToInt32(lector["recordatorios"]),
            ProductosStockBajo = Convert.ToInt32(lector["stock_bajo"]),
            ProductosPorVencer = Convert.ToInt32(lector["por_vencer"])
        };
    }

    public List<AgendaDashboardItemModel> ObtenerAgendaDelDia(DateTime fecha)
    {
        UsuarioModel usuario = ObtenerUsuario();
        bool filtrarVeterinario = string.Equals(usuario.Rol, "Veterinario", StringComparison.OrdinalIgnoreCase);
        bool mostrarSaldos = SesionActual.EsRol("Administrador", "Recepción", "Caja");

        const string sql = """
            SELECT c.id_cita, c.fecha_hora_inicio, m.nombre AS mascota,
                   d.nombre_completo AS dueno, v.nombre_completo AS veterinario,
                   s.nombre AS servicio, c.estado,
                   CASE WHEN @MostrarSaldos = 1 THEN COALESCE((
                       SELECT SUM(f.saldo_pendiente)
                       FROM facturas f
                       WHERE f.id_mascota = m.id_mascota
                         AND f.estado IN ('Emitida','Parcialmente pagada')
                   ), 0) ELSE 0 END AS saldo_pendiente
            FROM citas c
            INNER JOIN mascotas m ON m.id_mascota = c.id_mascota
            INNER JOIN duenos d ON d.id_dueno = m.id_dueno
            INNER JOIN veterinarios v ON v.id_veterinario = c.id_veterinario
            INNER JOIN catalogo_servicios s ON s.id_servicio = c.id_servicio
            WHERE DATE(c.fecha_hora_inicio) = @Fecha
              AND (@FiltrarVet = 0 OR c.id_veterinario = @IdVeterinario)
            ORDER BY c.fecha_hora_inicio, v.nombre_completo;
            """;

        List<AgendaDashboardItemModel> agenda = new();
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Fecha", MySqlDbType.Date).Value = fecha.Date;
        comando.Parameters.Add("@FiltrarVet", MySqlDbType.Byte).Value = filtrarVeterinario ? 1 : 0;
        comando.Parameters.Add("@IdVeterinario", MySqlDbType.Int32).Value = usuario.IdVeterinario ?? 0;
        comando.Parameters.Add("@MostrarSaldos", MySqlDbType.Byte).Value = mostrarSaldos ? 1 : 0;

        using MySqlDataReader lector = comando.ExecuteReader();
        while (lector.Read())
        {
            agenda.Add(new AgendaDashboardItemModel
            {
                IdCita = Convert.ToInt64(lector["id_cita"]),
                Hora = Convert.ToDateTime(lector["fecha_hora_inicio"]),
                Mascota = Convert.ToString(lector["mascota"]) ?? string.Empty,
                Dueno = Convert.ToString(lector["dueno"]) ?? string.Empty,
                Veterinario = Convert.ToString(lector["veterinario"]) ?? string.Empty,
                Servicio = Convert.ToString(lector["servicio"]) ?? string.Empty,
                Estado = Convert.ToString(lector["estado"]) ?? string.Empty,
                SaldoPendiente = Convert.ToDecimal(lector["saldo_pendiente"])
            });
        }

        return agenda;
    }

    private static UsuarioModel ObtenerUsuario()
    {
        return SesionActual.Usuario
            ?? throw new UnauthorizedAccessException("Debe iniciar sesión.");
    }

    private static void AgregarParametrosPanel(
        MySqlCommand comando,
        DateTime fecha,
        bool filtrarVeterinario,
        int? idVeterinario,
        bool verFinanzas,
        bool verInventario)
    {
        comando.Parameters.Add("@Fecha", MySqlDbType.Date).Value = fecha;
        comando.Parameters.Add("@FiltrarVet", MySqlDbType.Byte).Value = filtrarVeterinario ? 1 : 0;
        comando.Parameters.Add("@IdVeterinario", MySqlDbType.Int32).Value = idVeterinario ?? 0;
        comando.Parameters.Add("@VerFinanzas", MySqlDbType.Byte).Value = verFinanzas ? 1 : 0;
        comando.Parameters.Add("@VerInventario", MySqlDbType.Byte).Value = verInventario ? 1 : 0;
    }
}
