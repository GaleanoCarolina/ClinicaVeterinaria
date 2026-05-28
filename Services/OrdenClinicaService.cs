using System;
using System.Collections.Generic;
using ClinicaVeterinaria.Data;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Utils;
using MySql.Data.MySqlClient;

namespace ClinicaVeterinaria.Services;

public sealed class OrdenClinicaService
{
    private static readonly HashSet<string> Tipos = new(StringComparer.OrdinalIgnoreCase)
    {
        "Laboratorio", "Imagen", "Otro estudio"
    };

    public List<OrdenClinicaModel> Listar(DateTime desde, DateTime hasta, string estado, string filtro)
    {
        ExigirConsulta();
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(@"
SELECT o.id_orden, o.id_consulta, c.id_mascota, m.codigo_paciente, m.nombre mascota,
       d.nombre_completo dueno, d.telefono_principal telefono, v.nombre_completo veterinario,
       o.tipo_orden, o.nombre_estudio, COALESCE(o.motivo, '') motivo,
       COALESCE(o.observaciones, '') observaciones, o.estado, o.precio, o.facturado,
       o.fecha_solicitud, o.fecha_resultado, COALESCE(o.resultado_texto, '') resultado_texto,
       COALESCE(o.ruta_archivo, '') ruta_archivo
FROM ordenes_clinicas o
INNER JOIN consultas c ON c.id_consulta = o.id_consulta
INNER JOIN mascotas m ON m.id_mascota = c.id_mascota
INNER JOIN duenos d ON d.id_dueno = m.id_dueno
INNER JOIN veterinarios v ON v.id_veterinario = o.id_veterinario
WHERE DATE(o.fecha_solicitud) BETWEEN @desde AND @hasta
  AND (@estado = '' OR o.estado = @estado)
  AND (@filtro = '' OR m.nombre LIKE @buscar OR m.codigo_paciente LIKE @buscar
       OR d.nombre_completo LIKE @buscar OR o.nombre_estudio LIKE @buscar)
  AND (@esAdmin = 1 OR o.id_veterinario = @veterinario)
ORDER BY o.fecha_solicitud DESC, o.id_orden DESC;", conexion);
        comando.Parameters.AddWithValue("@desde", desde.Date);
        comando.Parameters.AddWithValue("@hasta", hasta.Date);
        comando.Parameters.AddWithValue("@estado", estado == "Todos" ? string.Empty : estado.Trim());
        comando.Parameters.AddWithValue("@filtro", filtro.Trim());
        comando.Parameters.AddWithValue("@buscar", $"%{filtro.Trim()}%");
        comando.Parameters.AddWithValue("@esAdmin", SesionActual.EsRol("Administrador") ? 1 : 0);
        comando.Parameters.AddWithValue("@veterinario", SesionActual.Usuario?.IdVeterinario ?? 0);
        using MySqlDataReader lector = comando.ExecuteReader();
        List<OrdenClinicaModel> lista = new();
        while (lector.Read())
        {
            lista.Add(new OrdenClinicaModel
            {
                IdOrden = lector.GetInt64("id_orden"),
                IdConsulta = lector.GetInt64("id_consulta"),
                IdMascota = lector.GetInt64("id_mascota"),
                CodigoPaciente = lector.GetString("codigo_paciente"),
                Mascota = lector.GetString("mascota"),
                Dueno = lector.GetString("dueno"),
                Telefono = lector.GetString("telefono"),
                Veterinario = lector.GetString("veterinario"),
                TipoOrden = lector.GetString("tipo_orden"),
                NombreEstudio = lector.GetString("nombre_estudio"),
                Motivo = lector.GetString("motivo"),
                Observaciones = lector.GetString("observaciones"),
                Estado = lector.GetString("estado"),
                Precio = lector.GetDecimal("precio"),
                Facturado = lector.GetBoolean("facturado"),
                FechaSolicitud = lector.GetDateTime("fecha_solicitud"),
                FechaResultado = lector.IsDBNull(lector.GetOrdinal("fecha_resultado")) ? null : lector.GetDateTime("fecha_resultado"),
                ResultadoTexto = lector.GetString("resultado_texto"),
                RutaArchivo = lector.GetString("ruta_archivo")
            });
        }
        return lista;
    }

    public List<ConsultaDisponibleOrdenModel> ListarConsultas(string filtro)
    {
        ExigirEdicion();
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(@"
SELECT c.id_consulta, c.id_mascota, c.id_veterinario, m.codigo_paciente, m.nombre mascota,
       d.nombre_completo dueno, v.nombre_completo veterinario, c.fecha_atencion
FROM consultas c
INNER JOIN mascotas m ON m.id_mascota = c.id_mascota
INNER JOIN duenos d ON d.id_dueno = m.id_dueno
INNER JOIN veterinarios v ON v.id_veterinario = c.id_veterinario
WHERE (@esAdmin = 1 OR c.id_veterinario = @veterinario)
 AND (@filtro = '' OR m.nombre LIKE @buscar OR m.codigo_paciente LIKE @buscar OR d.nombre_completo LIKE @buscar)
ORDER BY c.fecha_atencion DESC LIMIT 100;", conexion);
        comando.Parameters.AddWithValue("@esAdmin", SesionActual.EsRol("Administrador") ? 1 : 0);
        comando.Parameters.AddWithValue("@veterinario", SesionActual.Usuario?.IdVeterinario ?? 0);
        comando.Parameters.AddWithValue("@filtro", filtro.Trim());
        comando.Parameters.AddWithValue("@buscar", $"%{filtro.Trim()}%");
        using MySqlDataReader lector = comando.ExecuteReader();
        List<ConsultaDisponibleOrdenModel> lista = new();
        while (lector.Read())
        {
            lista.Add(new ConsultaDisponibleOrdenModel
            {
                IdConsulta = lector.GetInt64("id_consulta"), IdMascota = lector.GetInt64("id_mascota"),
                IdVeterinario = lector.GetInt32("id_veterinario"), CodigoPaciente = lector.GetString("codigo_paciente"),
                Mascota = lector.GetString("mascota"), Dueno = lector.GetString("dueno"),
                Veterinario = lector.GetString("veterinario"), FechaAtencion = lector.GetDateTime("fecha_atencion")
            });
        }
        return lista;
    }

    public long Crear(OrdenClinicaModel registro)
    {
        ExigirEdicion();
        Validar(registro);
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlTransaction tx = conexion.BeginTransaction();
        try
        {
            ContextoOrden contexto = ObtenerContextoConsulta(conexion, tx, registro.IdConsulta);
            AutorizarVeterinario(contexto.IdVeterinario);
            using MySqlCommand insertar = new(@"
INSERT INTO ordenes_clinicas
(id_consulta, tipo_orden, nombre_estudio, motivo, observaciones, estado, precio, facturado, fecha_solicitud, id_veterinario)
VALUES (@consulta, @tipo, @estudio, @motivo, @observaciones, 'Solicitada', @precio, 0, CURRENT_TIMESTAMP, @veterinario);", conexion, tx);
            insertar.Parameters.AddWithValue("@consulta", registro.IdConsulta);
            insertar.Parameters.AddWithValue("@tipo", registro.TipoOrden);
            insertar.Parameters.AddWithValue("@estudio", registro.NombreEstudio.Trim());
            insertar.Parameters.AddWithValue("@motivo", TextoONulo(registro.Motivo));
            insertar.Parameters.AddWithValue("@observaciones", TextoONulo(registro.Observaciones));
            insertar.Parameters.AddWithValue("@precio", registro.Precio);
            insertar.Parameters.AddWithValue("@veterinario", contexto.IdVeterinario);
            insertar.ExecuteNonQuery();
            long idOrden = insertar.LastInsertedId;
            if (registro.Precio > 0)
            {
                CrearCargo(conexion, tx, contexto, idOrden, registro.NombreEstudio.Trim(), registro.Precio);
            }
            tx.Commit();
            return idOrden;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void MarcarEnProceso(long idOrden)
    {
        ExigirEdicion();
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        VerificarOrdenAutorizada(conexion, null, idOrden);
        using MySqlCommand comando = new("UPDATE ordenes_clinicas SET estado = 'En proceso' WHERE id_orden = @id AND estado = 'Solicitada';", conexion);
        comando.Parameters.AddWithValue("@id", idOrden);
        if (comando.ExecuteNonQuery() == 0) throw new InvalidOperationException("Solo una orden solicitada puede pasar a En proceso.");
    }

    public void RegistrarResultado(long idOrden, string resultado, string rutaArchivo)
    {
        ExigirEdicion();
        if (string.IsNullOrWhiteSpace(resultado)) throw new InvalidOperationException("El resultado textual es obligatorio.");
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        VerificarOrdenAutorizada(conexion, null, idOrden);
        using MySqlCommand comando = new(@"
UPDATE ordenes_clinicas
SET estado = 'Resultado recibido', fecha_resultado = CURRENT_TIMESTAMP,
    resultado_texto = @resultado, ruta_archivo = @ruta
WHERE id_orden = @id AND estado IN ('Solicitada','En proceso');", conexion);
        comando.Parameters.AddWithValue("@resultado", resultado.Trim());
        comando.Parameters.AddWithValue("@ruta", TextoONulo(rutaArchivo));
        comando.Parameters.AddWithValue("@id", idOrden);
        if (comando.ExecuteNonQuery() == 0) throw new InvalidOperationException("La orden ya está cerrada o no puede recibir resultados.");
    }

    public void Cancelar(long idOrden, string motivo)
    {
        ExigirEdicion();
        if (string.IsNullOrWhiteSpace(motivo)) throw new InvalidOperationException("Indique el motivo de cancelación.");
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlTransaction tx = conexion.BeginTransaction();
        try
        {
            OrdenClinicaModel orden = VerificarOrdenAutorizada(conexion, tx, idOrden, bloquear: true);
            if (orden.Facturado) throw new InvalidOperationException("La orden ya fue facturada y no puede cancelarse desde este módulo.");
            if (orden.Estado is "Resultado recibido" or "Cancelada") throw new InvalidOperationException("La orden ya se encuentra cerrada.");
            using MySqlCommand comando = new(@"
UPDATE ordenes_clinicas SET estado = 'Cancelada', observaciones = CONCAT(COALESCE(observaciones,''), @nota)
WHERE id_orden = @id;", conexion, tx);
            comando.Parameters.AddWithValue("@nota", $"\nCancelación: {motivo.Trim()}");
            comando.Parameters.AddWithValue("@id", idOrden);
            comando.ExecuteNonQuery();
            using MySqlCommand cargo = new("UPDATE cargos_pendientes SET estado='Anulado' WHERE tipo_item='Laboratorio' AND id_referencia=@id AND estado='Pendiente';", conexion, tx);
            cargo.Parameters.AddWithValue("@id", idOrden);
            cargo.ExecuteNonQuery();
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private OrdenClinicaModel VerificarOrdenAutorizada(MySqlConnection conexion, MySqlTransaction? tx, long idOrden, bool bloquear = false)
    {
        string bloqueo = bloquear ? " FOR UPDATE" : string.Empty;
        using MySqlCommand comando = new($@"
SELECT o.id_orden, o.id_veterinario, o.estado, o.facturado
FROM ordenes_clinicas o WHERE o.id_orden = @id{bloqueo};", conexion, tx);
        comando.Parameters.AddWithValue("@id", idOrden);
        using MySqlDataReader lector = comando.ExecuteReader();
        if (!lector.Read()) throw new InvalidOperationException("La orden clínica no existe.");
        int idVeterinario = lector.GetInt32("id_veterinario");
        OrdenClinicaModel resultado = new() { IdOrden = idOrden, Estado = lector.GetString("estado"), Facturado = lector.GetBoolean("facturado") };
        lector.Close();
        AutorizarVeterinario(idVeterinario);
        return resultado;
    }

    private static ContextoOrden ObtenerContextoConsulta(MySqlConnection conexion, MySqlTransaction tx, long idConsulta)
    {
        using MySqlCommand comando = new(@"
SELECT c.id_consulta, c.id_mascota, c.id_cita, c.id_veterinario, m.id_dueno
FROM consultas c INNER JOIN mascotas m ON m.id_mascota = c.id_mascota
WHERE c.id_consulta = @id FOR UPDATE;", conexion, tx);
        comando.Parameters.AddWithValue("@id", idConsulta);
        using MySqlDataReader lector = comando.ExecuteReader();
        if (!lector.Read()) throw new InvalidOperationException("La consulta seleccionada no existe.");
        return new ContextoOrden
        {
            IdConsulta = lector.GetInt64("id_consulta"), IdMascota = lector.GetInt64("id_mascota"),
            IdCita = lector.GetInt64("id_cita"), IdVeterinario = lector.GetInt32("id_veterinario"),
            IdDueno = lector.GetInt64("id_dueno")
        };
    }

    private static void CrearCargo(MySqlConnection conexion, MySqlTransaction tx, ContextoOrden c, long idOrden, string descripcion, decimal precio)
    {
        using MySqlCommand comando = new(@"
INSERT INTO cargos_pendientes
(id_dueno, id_mascota, id_consulta, id_cita, tipo_item, id_referencia, descripcion, cantidad, precio_unitario, descuento, subtotal, estado)
VALUES (@dueno, @mascota, @consulta, @cita, 'Laboratorio', @referencia, @descripcion, 1, @precio, 0, @precio, 'Pendiente');", conexion, tx);
        comando.Parameters.AddWithValue("@dueno", c.IdDueno); comando.Parameters.AddWithValue("@mascota", c.IdMascota);
        comando.Parameters.AddWithValue("@consulta", c.IdConsulta); comando.Parameters.AddWithValue("@cita", c.IdCita);
        comando.Parameters.AddWithValue("@referencia", idOrden); comando.Parameters.AddWithValue("@descripcion", descripcion);
        comando.Parameters.AddWithValue("@precio", precio); comando.ExecuteNonQuery();
    }

    private static void Validar(OrdenClinicaModel registro)
    {
        if (registro.IdConsulta <= 0) throw new InvalidOperationException("Seleccione una consulta clínica.");
        if (!Tipos.Contains(registro.TipoOrden)) throw new InvalidOperationException("Seleccione un tipo de orden válido.");
        if (string.IsNullOrWhiteSpace(registro.NombreEstudio)) throw new InvalidOperationException("El nombre del estudio es obligatorio.");
        if (registro.Precio < 0) throw new InvalidOperationException("El precio no puede ser negativo.");
    }

    private static object TextoONulo(string texto) => string.IsNullOrWhiteSpace(texto) ? DBNull.Value : texto.Trim();
    private static void ExigirConsulta() => SesionActual.ExigirRoles("Administrador", "Veterinario");
    private static void ExigirEdicion()
    {
        SesionActual.ExigirRoles("Administrador", "Veterinario");
        if (SesionActual.EsRol("Veterinario") && !SesionActual.Usuario!.IdVeterinario.HasValue)
            throw new UnauthorizedAccessException("El usuario veterinario no está asociado a un profesional clínico.");
    }
    private static void AutorizarVeterinario(int idVeterinario)
    {
        if (SesionActual.EsRol("Veterinario") && SesionActual.Usuario!.IdVeterinario != idVeterinario)
            throw new UnauthorizedAccessException("Solo puede administrar órdenes clínicas de sus propios pacientes.");
    }

    private sealed class ContextoOrden
    {
        public long IdConsulta { get; set; }
        public long IdMascota { get; set; }
        public long IdCita { get; set; }
        public long IdDueno { get; set; }
        public int IdVeterinario { get; set; }
    }
}
