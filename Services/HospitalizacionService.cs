using System;
using System.Collections.Generic;
using ClinicaVeterinaria.Data;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Utils;
using MySql.Data.MySqlClient;

namespace ClinicaVeterinaria.Services;

public sealed class HospitalizacionService
{
    private static readonly HashSet<string> Estados = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ingresada", "En observación", "Alta", "Cancelada"
    };
    private static readonly HashSet<string> TiposCargo = new(StringComparer.OrdinalIgnoreCase)
    {
        "Hospitalización", "Medicamento", "Servicio", "Laboratorio", "Otro"
    };

    public HospitalizacionResumenModel ObtenerResumen()
    {
        ExigirConsulta();
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(@"
SELECT
 SUM(CASE WHEN h.estado = 'Ingresada' THEN 1 ELSE 0 END) ingresados,
 SUM(CASE WHEN h.estado = 'En observación' THEN 1 ELSE 0 END) observacion,
 SUM(CASE WHEN h.estado = 'Alta' AND DATE(h.fecha_hora_alta) = CURDATE() THEN 1 ELSE 0 END) altas,
 COALESCE((SELECT SUM(cp.subtotal) FROM cargos_pendientes cp
           INNER JOIN hospitalizaciones hx ON hx.id_hospitalizacion = cp.id_referencia
           WHERE cp.tipo_item = 'Hospitalización' AND cp.estado = 'Pendiente'
             AND (@esAdmin = 1 OR hx.id_veterinario = @veterinario)), 0) cargos
FROM hospitalizaciones h
WHERE (@esAdmin = 1 OR h.id_veterinario = @veterinario);", conexion);
        AgregarPermiso(comando);
        using MySqlDataReader lector = comando.ExecuteReader();
        lector.Read();
        return new HospitalizacionResumenModel
        {
            PacientesIngresados = lector.IsDBNull(lector.GetOrdinal("ingresados")) ? 0 : Convert.ToInt32(lector["ingresados"]),
            EnObservacion = lector.IsDBNull(lector.GetOrdinal("observacion")) ? 0 : Convert.ToInt32(lector["observacion"]),
            AltasHoy = lector.IsDBNull(lector.GetOrdinal("altas")) ? 0 : Convert.ToInt32(lector["altas"]),
            CargosPendientes = lector.GetDecimal("cargos")
        };
    }

    public List<HospitalizacionModel> Listar(DateTime desde, DateTime hasta, string estado, string filtro)
    {
        ExigirConsulta();
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(@"
SELECT h.id_hospitalizacion, h.id_mascota, h.id_consulta_origen, h.id_veterinario,
       m.codigo_paciente, m.nombre mascota, m.especie, d.nombre_completo dueno, d.telefono_principal telefono,
       v.nombre_completo veterinario, h.fecha_hora_ingreso, h.fecha_hora_alta, h.motivo,
       COALESCE(h.espacio_asignado, '') espacio_asignado, h.estado, COALESCE(h.observaciones, '') observaciones,
       (SELECT COUNT(*) FROM hospitalizacion_evoluciones e WHERE e.id_hospitalizacion = h.id_hospitalizacion) evoluciones,
       COALESCE((SELECT SUM(cp.subtotal) FROM cargos_pendientes cp WHERE cp.tipo_item='Hospitalización'
          AND cp.id_referencia=h.id_hospitalizacion AND cp.estado='Pendiente'), 0) saldo_pendiente
FROM hospitalizaciones h
INNER JOIN mascotas m ON m.id_mascota = h.id_mascota
INNER JOIN duenos d ON d.id_dueno = m.id_dueno
INNER JOIN veterinarios v ON v.id_veterinario = h.id_veterinario
WHERE DATE(h.fecha_hora_ingreso) BETWEEN @desde AND @hasta
 AND (@estado = '' OR h.estado = @estado)
 AND (@filtro = '' OR m.nombre LIKE @buscar OR m.codigo_paciente LIKE @buscar OR d.nombre_completo LIKE @buscar OR h.espacio_asignado LIKE @buscar)
 AND (@esAdmin = 1 OR h.id_veterinario = @veterinario)
ORDER BY FIELD(h.estado, 'Ingresada','En observación','Alta','Cancelada'), h.fecha_hora_ingreso DESC;", conexion);
        comando.Parameters.AddWithValue("@desde", desde.Date);
        comando.Parameters.AddWithValue("@hasta", hasta.Date);
        comando.Parameters.AddWithValue("@estado", estado == "Todos" ? string.Empty : estado.Trim());
        comando.Parameters.AddWithValue("@filtro", filtro.Trim());
        comando.Parameters.AddWithValue("@buscar", $"%{filtro.Trim()}%");
        AgregarPermiso(comando);
        using MySqlDataReader lector = comando.ExecuteReader();
        List<HospitalizacionModel> lista = new();
        while (lector.Read())
        {
            lista.Add(new HospitalizacionModel
            {
                IdHospitalizacion = lector.GetInt64("id_hospitalizacion"), IdMascota = lector.GetInt64("id_mascota"),
                IdConsultaOrigen = lector.IsDBNull(lector.GetOrdinal("id_consulta_origen")) ? null : lector.GetInt64("id_consulta_origen"),
                IdVeterinario = lector.GetInt32("id_veterinario"), CodigoPaciente = lector.GetString("codigo_paciente"),
                Mascota = lector.GetString("mascota"), Especie = lector.GetString("especie"), Dueno = lector.GetString("dueno"),
                Telefono = lector.GetString("telefono"), Veterinario = lector.GetString("veterinario"),
                FechaHoraIngreso = lector.GetDateTime("fecha_hora_ingreso"),
                FechaHoraAlta = lector.IsDBNull(lector.GetOrdinal("fecha_hora_alta")) ? null : lector.GetDateTime("fecha_hora_alta"),
                Motivo = lector.GetString("motivo"), EspacioAsignado = lector.GetString("espacio_asignado"),
                Estado = lector.GetString("estado"), Observaciones = lector.GetString("observaciones"),
                Evoluciones = lector.GetInt32("evoluciones"), SaldoPendiente = lector.GetDecimal("saldo_pendiente")
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
WHERE m.activo=1 AND m.estado_vital='Viva'
AND (@filtro='' OR m.nombre LIKE @buscar OR m.codigo_paciente LIKE @buscar OR d.nombre_completo LIKE @buscar)
ORDER BY m.nombre LIMIT 100;", conexion);
        comando.Parameters.AddWithValue("@filtro", filtro.Trim()); comando.Parameters.AddWithValue("@buscar", $"%{filtro.Trim()}%");
        using MySqlDataReader lector = comando.ExecuteReader();
        List<MascotaBusquedaModel> lista = new();
        while (lector.Read()) lista.Add(new MascotaBusquedaModel
        {
            IdMascota = lector.GetInt64("id_mascota"), CodigoPaciente = lector.GetString("codigo_paciente"),
            NombreMascota = lector.GetString("nombre"), Especie = lector.GetString("especie"),
            Dueno = lector.GetString("dueno"), Telefono = lector.GetString("telefono_principal")
        });
        return lista;
    }

    public List<VeterinarioModel> ListarVeterinariosActivos()
    {
        ExigirEdicion();
        using MySqlConnection conexion = Database.CrearConexion(); conexion.Open();
        using MySqlCommand comando = new(@"
SELECT id_veterinario, codigo_veterinario, nombre_completo, especialidad, activo
FROM veterinarios WHERE activo=1 AND (@esAdmin=1 OR id_veterinario=@veterinario)
ORDER BY nombre_completo;", conexion);
        AgregarPermiso(comando);
        using MySqlDataReader lector = comando.ExecuteReader(); List<VeterinarioModel> lista = new();
        while (lector.Read()) lista.Add(new VeterinarioModel { IdVeterinario=lector.GetInt32("id_veterinario"), CodigoVeterinario=lector.GetString("codigo_veterinario"), NombreCompleto=lector.GetString("nombre_completo"), Especialidad=lector.GetString("especialidad"), Activo=lector.GetBoolean("activo") });
        return lista;
    }

    public List<ConsultaOrigenHospitalizacionModel> ListarConsultasMascota(long idMascota)
    {
        ExigirEdicion();
        using MySqlConnection conexion = Database.CrearConexion(); conexion.Open();
        using MySqlCommand comando = new(@"
SELECT c.id_consulta, c.id_mascota, c.fecha_atencion, c.motivo_consulta, v.nombre_completo veterinario
FROM consultas c INNER JOIN veterinarios v ON v.id_veterinario=c.id_veterinario
WHERE c.id_mascota=@mascota AND (@esAdmin=1 OR c.id_veterinario=@veterinario)
ORDER BY c.fecha_atencion DESC LIMIT 30;", conexion);
        comando.Parameters.AddWithValue("@mascota", idMascota); AgregarPermiso(comando);
        using MySqlDataReader lector = comando.ExecuteReader(); List<ConsultaOrigenHospitalizacionModel> lista = new();
        while (lector.Read()) lista.Add(new ConsultaOrigenHospitalizacionModel { IdConsulta=lector.GetInt64("id_consulta"), IdMascota=lector.GetInt64("id_mascota"), FechaAtencion=lector.GetDateTime("fecha_atencion"), Veterinario=lector.GetString("veterinario"), MotivoConsulta=lector.GetString("motivo_consulta") });
        return lista;
    }

    public long Ingresar(NuevaHospitalizacionModel registro)
    {
        ExigirEdicion(); ValidarIngreso(registro); AutorizarVeterinario(registro.IdVeterinario);
        using MySqlConnection conexion = Database.CrearConexion(); conexion.Open(); using MySqlTransaction tx = conexion.BeginTransaction();
        try
        {
            using MySqlCommand validar = new(@"
SELECT id_hospitalizacion FROM hospitalizaciones
WHERE id_mascota=@mascota AND estado IN ('Ingresada','En observación') LIMIT 1 FOR UPDATE;", conexion, tx);
            validar.Parameters.AddWithValue("@mascota", registro.IdMascota);
            if (validar.ExecuteScalar() is not null) throw new InvalidOperationException("El paciente ya posee una hospitalización activa.");
            if (registro.IdConsultaOrigen.HasValue)
            {
                using MySqlCommand consulta = new("SELECT COUNT(*) FROM consultas WHERE id_consulta=@consulta AND id_mascota=@mascota;", conexion, tx);
                consulta.Parameters.AddWithValue("@consulta", registro.IdConsultaOrigen.Value); consulta.Parameters.AddWithValue("@mascota", registro.IdMascota);
                if (Convert.ToInt32(consulta.ExecuteScalar()) == 0) throw new InvalidOperationException("La consulta de origen no corresponde al paciente seleccionado.");
            }
            using MySqlCommand insertar = new(@"
INSERT INTO hospitalizaciones
(id_mascota, id_consulta_origen, id_veterinario, fecha_hora_ingreso, motivo, espacio_asignado, estado, observaciones)
VALUES (@mascota, @consulta, @veterinario, @ingreso, @motivo, @espacio, 'Ingresada', @observaciones);", conexion, tx);
            insertar.Parameters.AddWithValue("@mascota", registro.IdMascota); insertar.Parameters.AddWithValue("@consulta", registro.IdConsultaOrigen.HasValue ? registro.IdConsultaOrigen.Value : DBNull.Value);
            insertar.Parameters.AddWithValue("@veterinario", registro.IdVeterinario); insertar.Parameters.AddWithValue("@ingreso", registro.FechaHoraIngreso);
            insertar.Parameters.AddWithValue("@motivo", registro.Motivo.Trim()); insertar.Parameters.AddWithValue("@espacio", TextoONulo(registro.EspacioAsignado)); insertar.Parameters.AddWithValue("@observaciones", TextoONulo(registro.Observaciones));
            insertar.ExecuteNonQuery(); long id = insertar.LastInsertedId; tx.Commit(); return id;
        }
        catch { tx.Rollback(); throw; }
    }

    public List<HospitalizacionEvolucionModel> ListarEvoluciones(long idHospitalizacion)
    {
        ExigirConsulta();
        using MySqlConnection conexion = Database.CrearConexion(); conexion.Open(); VerificarAutorizacion(conexion, null, idHospitalizacion);
        using MySqlCommand comando = new(@"
SELECT e.id_evolucion, e.id_hospitalizacion, e.fecha_hora, e.id_veterinario, v.nombre_completo veterinario,
 e.temperatura, e.peso, e.frecuencia_cardiaca, e.frecuencia_respiratoria, COALESCE(e.observaciones,'') observaciones,
 COALESCE(e.medicacion_administrada,'') medicacion_administrada, COALESCE(e.alimentacion,'') alimentacion, COALESCE(e.incidencias,'') incidencias
FROM hospitalizacion_evoluciones e INNER JOIN veterinarios v ON v.id_veterinario=e.id_veterinario
WHERE e.id_hospitalizacion=@id ORDER BY e.fecha_hora DESC;", conexion);
        comando.Parameters.AddWithValue("@id", idHospitalizacion);
        using MySqlDataReader lector = comando.ExecuteReader(); List<HospitalizacionEvolucionModel> lista = new();
        while (lector.Read()) lista.Add(new HospitalizacionEvolucionModel
        {
            IdEvolucion=lector.GetInt64("id_evolucion"), IdHospitalizacion=lector.GetInt64("id_hospitalizacion"), FechaHora=lector.GetDateTime("fecha_hora"),
            IdVeterinario=lector.GetInt32("id_veterinario"), Veterinario=lector.GetString("veterinario"),
            Temperatura=lector.IsDBNull(lector.GetOrdinal("temperatura")) ? null : lector.GetDecimal("temperatura"),
            Peso=lector.IsDBNull(lector.GetOrdinal("peso")) ? null : lector.GetDecimal("peso"),
            FrecuenciaCardiaca=lector.IsDBNull(lector.GetOrdinal("frecuencia_cardiaca")) ? null : lector.GetInt32("frecuencia_cardiaca"),
            FrecuenciaRespiratoria=lector.IsDBNull(lector.GetOrdinal("frecuencia_respiratoria")) ? null : lector.GetInt32("frecuencia_respiratoria"),
            Observaciones=lector.GetString("observaciones"), MedicacionAdministrada=lector.GetString("medicacion_administrada"), Alimentacion=lector.GetString("alimentacion"), Incidencias=lector.GetString("incidencias")
        });
        return lista;
    }

    public void RegistrarEvolucion(HospitalizacionEvolucionModel registro)
    {
        ExigirEdicion();
        if (string.IsNullOrWhiteSpace(registro.Observaciones) && string.IsNullOrWhiteSpace(registro.MedicacionAdministrada) && string.IsNullOrWhiteSpace(registro.Incidencias))
            throw new InvalidOperationException("Registre observaciones, medicación o incidencias de la evolución.");
        using MySqlConnection conexion = Database.CrearConexion(); conexion.Open(); using MySqlTransaction tx = conexion.BeginTransaction();
        try
        {
            HospitalizacionModel h = VerificarAutorizacion(conexion, tx, registro.IdHospitalizacion, true);
            if (h.Estado is "Alta" or "Cancelada") throw new InvalidOperationException("No puede registrar evolución en una hospitalización cerrada.");
            int vet = SesionActual.EsRol("Veterinario") ? SesionActual.Usuario!.IdVeterinario!.Value : h.IdVeterinario;
            using MySqlCommand comando = new(@"
INSERT INTO hospitalizacion_evoluciones
(id_hospitalizacion, fecha_hora, id_veterinario, temperatura, peso, frecuencia_cardiaca, frecuencia_respiratoria, observaciones, medicacion_administrada, alimentacion, incidencias)
VALUES (@hospitalizacion, @fecha, @veterinario, @temperatura, @peso, @fc, @fr, @observaciones, @medicacion, @alimentacion, @incidencias);", conexion, tx);
            comando.Parameters.AddWithValue("@hospitalizacion", registro.IdHospitalizacion); comando.Parameters.AddWithValue("@fecha", registro.FechaHora);
            comando.Parameters.AddWithValue("@veterinario", vet); comando.Parameters.AddWithValue("@temperatura", registro.Temperatura.HasValue ? registro.Temperatura.Value : DBNull.Value);
            comando.Parameters.AddWithValue("@peso", registro.Peso.HasValue ? registro.Peso.Value : DBNull.Value); comando.Parameters.AddWithValue("@fc", registro.FrecuenciaCardiaca.HasValue ? registro.FrecuenciaCardiaca.Value : DBNull.Value);
            comando.Parameters.AddWithValue("@fr", registro.FrecuenciaRespiratoria.HasValue ? registro.FrecuenciaRespiratoria.Value : DBNull.Value); comando.Parameters.AddWithValue("@observaciones", TextoONulo(registro.Observaciones));
            comando.Parameters.AddWithValue("@medicacion", TextoONulo(registro.MedicacionAdministrada)); comando.Parameters.AddWithValue("@alimentacion", TextoONulo(registro.Alimentacion)); comando.Parameters.AddWithValue("@incidencias", TextoONulo(registro.Incidencias));
            comando.ExecuteNonQuery();
            using MySqlCommand observar = new("UPDATE hospitalizaciones SET estado='En observación' WHERE id_hospitalizacion=@id AND estado='Ingresada';", conexion, tx);
            observar.Parameters.AddWithValue("@id", registro.IdHospitalizacion); observar.ExecuteNonQuery(); tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    public void RegistrarCargo(long idHospitalizacion, CargoHospitalizacionModel cargo)
    {
        ExigirEdicion();
        if (!TiposCargo.Contains(cargo.TipoItem)) throw new InvalidOperationException("Seleccione un tipo de cargo válido.");
        if (string.IsNullOrWhiteSpace(cargo.Descripcion)) throw new InvalidOperationException("La descripción del cargo es obligatoria.");
        if (cargo.Cantidad <= 0 || cargo.PrecioUnitario < 0 || cargo.Descuento < 0 || cargo.Subtotal < 0) throw new InvalidOperationException("Revise los importes del cargo.");
        using MySqlConnection conexion = Database.CrearConexion(); conexion.Open(); using MySqlTransaction tx = conexion.BeginTransaction();
        try
        {
            HospitalizacionModel h = VerificarAutorizacion(conexion, tx, idHospitalizacion, true);
            if (h.Estado == "Cancelada") throw new InvalidOperationException("No puede cargar conceptos a una hospitalización cancelada.");
            using MySqlCommand contexto = new(@"
SELECT m.id_dueno, h.id_mascota, h.id_consulta_origen,
       (SELECT id_cita FROM consultas WHERE id_consulta=h.id_consulta_origen) id_cita
FROM hospitalizaciones h INNER JOIN mascotas m ON m.id_mascota=h.id_mascota
WHERE h.id_hospitalizacion=@id;", conexion, tx);
            contexto.Parameters.AddWithValue("@id", idHospitalizacion);
            using MySqlDataReader lector = contexto.ExecuteReader(); lector.Read();
            long idDueno = lector.GetInt64("id_dueno"); long idMascota = lector.GetInt64("id_mascota");
            object consulta = lector.IsDBNull(lector.GetOrdinal("id_consulta_origen")) ? DBNull.Value : lector.GetInt64("id_consulta_origen");
            object cita = lector.IsDBNull(lector.GetOrdinal("id_cita")) ? DBNull.Value : lector.GetInt64("id_cita"); lector.Close();
            using MySqlCommand insertar = new(@"
INSERT INTO cargos_pendientes
(id_dueno, id_mascota, id_consulta, id_cita, tipo_item, id_referencia, descripcion, cantidad, precio_unitario, descuento, subtotal, estado)
VALUES (@dueno, @mascota, @consulta, @cita, 'Hospitalización', @referencia, @descripcion, @cantidad, @precio, @descuento, @subtotal, 'Pendiente');", conexion, tx);
            insertar.Parameters.AddWithValue("@dueno", idDueno); insertar.Parameters.AddWithValue("@mascota", idMascota); insertar.Parameters.AddWithValue("@consulta", consulta); insertar.Parameters.AddWithValue("@cita", cita);
            insertar.Parameters.AddWithValue("@referencia", idHospitalizacion); insertar.Parameters.AddWithValue("@descripcion", $"{cargo.TipoItem}: {cargo.Descripcion.Trim()}"); insertar.Parameters.AddWithValue("@cantidad", cargo.Cantidad);
            insertar.Parameters.AddWithValue("@precio", cargo.PrecioUnitario); insertar.Parameters.AddWithValue("@descuento", cargo.Descuento); insertar.Parameters.AddWithValue("@subtotal", cargo.Subtotal);
            insertar.ExecuteNonQuery(); tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    public void DarAlta(long idHospitalizacion, DateTime fechaAlta, string observaciones)
    {
        ExigirEdicion();
        using MySqlConnection conexion = Database.CrearConexion(); conexion.Open(); using MySqlTransaction tx = conexion.BeginTransaction();
        try
        {
            HospitalizacionModel h = VerificarAutorizacion(conexion, tx, idHospitalizacion, true);
            if (h.Estado is "Alta" or "Cancelada") throw new InvalidOperationException("La hospitalización ya se encuentra cerrada.");
            if (fechaAlta < h.FechaHoraIngreso) throw new InvalidOperationException("La fecha de alta no puede ser anterior al ingreso.");
            using MySqlCommand comando = new(@"
UPDATE hospitalizaciones SET estado='Alta', fecha_hora_alta=@alta,
 observaciones=CONCAT(COALESCE(observaciones,''), @nota) WHERE id_hospitalizacion=@id;", conexion, tx);
            comando.Parameters.AddWithValue("@alta", fechaAlta); comando.Parameters.AddWithValue("@nota", string.IsNullOrWhiteSpace(observaciones) ? string.Empty : $"\nAlta: {observaciones.Trim()}"); comando.Parameters.AddWithValue("@id", idHospitalizacion);
            comando.ExecuteNonQuery(); tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    public void Cancelar(long idHospitalizacion, string motivo)
    {
        ExigirEdicion(); if (string.IsNullOrWhiteSpace(motivo)) throw new InvalidOperationException("Indique el motivo de cancelación.");
        using MySqlConnection conexion = Database.CrearConexion(); conexion.Open(); using MySqlTransaction tx = conexion.BeginTransaction();
        try
        {
            HospitalizacionModel h = VerificarAutorizacion(conexion, tx, idHospitalizacion, true);
            if (h.Estado is "Alta" or "Cancelada") throw new InvalidOperationException("La hospitalización ya está cerrada.");
            using MySqlCommand verificarCargos = new("SELECT COUNT(*) FROM cargos_pendientes WHERE tipo_item='Hospitalización' AND id_referencia=@id AND estado='Facturado';", conexion, tx);
            verificarCargos.Parameters.AddWithValue("@id", idHospitalizacion);
            if (Convert.ToInt32(verificarCargos.ExecuteScalar()) > 0) throw new InvalidOperationException("Existen cargos ya facturados. Anule primero la factura correspondiente.");
            using MySqlCommand comando = new(@"
UPDATE hospitalizaciones SET estado='Cancelada', observaciones=CONCAT(COALESCE(observaciones,''), @nota) WHERE id_hospitalizacion=@id;", conexion, tx);
            comando.Parameters.AddWithValue("@nota", $"\nCancelación: {motivo.Trim()}"); comando.Parameters.AddWithValue("@id", idHospitalizacion); comando.ExecuteNonQuery();
            using MySqlCommand cargos = new("UPDATE cargos_pendientes SET estado='Anulado' WHERE tipo_item='Hospitalización' AND id_referencia=@id AND estado='Pendiente';", conexion, tx);
            cargos.Parameters.AddWithValue("@id", idHospitalizacion); cargos.ExecuteNonQuery(); tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    private HospitalizacionModel VerificarAutorizacion(MySqlConnection conexion, MySqlTransaction? tx, long idHospitalizacion, bool bloquear = false)
    {
        string fin = bloquear ? " FOR UPDATE" : string.Empty;
        using MySqlCommand comando = new($"SELECT id_hospitalizacion, id_veterinario, fecha_hora_ingreso, estado FROM hospitalizaciones WHERE id_hospitalizacion=@id{fin};", conexion, tx);
        comando.Parameters.AddWithValue("@id", idHospitalizacion); using MySqlDataReader lector = comando.ExecuteReader();
        if (!lector.Read()) throw new InvalidOperationException("La hospitalización no existe.");
        HospitalizacionModel h = new() { IdHospitalizacion=lector.GetInt64("id_hospitalizacion"), IdVeterinario=lector.GetInt32("id_veterinario"), FechaHoraIngreso=lector.GetDateTime("fecha_hora_ingreso"), Estado=lector.GetString("estado") };
        lector.Close(); AutorizarVeterinario(h.IdVeterinario); return h;
    }

    private static void ValidarIngreso(NuevaHospitalizacionModel registro)
    {
        if (registro.IdMascota <= 0) throw new InvalidOperationException("Seleccione un paciente.");
        if (registro.IdVeterinario <= 0) throw new InvalidOperationException("Seleccione el veterinario responsable.");
        if (string.IsNullOrWhiteSpace(registro.Motivo)) throw new InvalidOperationException("El motivo del ingreso es obligatorio.");
        if (registro.FechaHoraIngreso > DateTime.Now.AddMinutes(5)) throw new InvalidOperationException("La fecha de ingreso no puede quedar en el futuro.");
    }
    private static void AgregarPermiso(MySqlCommand comando)
    {
        comando.Parameters.AddWithValue("@esAdmin", SesionActual.EsRol("Administrador") ? 1 : 0);
        comando.Parameters.AddWithValue("@veterinario", SesionActual.Usuario?.IdVeterinario ?? 0);
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
            throw new UnauthorizedAccessException("Solo puede gestionar hospitalizaciones asignadas a usted.");
    }
}
