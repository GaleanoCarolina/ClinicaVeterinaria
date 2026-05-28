using System;
using System.Collections.Generic;
using System.Data;
using ClinicaVeterinaria.Data;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Utils;
using MySql.Data.MySqlClient;

namespace ClinicaVeterinaria.Services;

public sealed class ClientePacienteService
{
    public List<DuenoModel> BuscarDuenos(string? termino)
    {
        ExigirLectura();
        string filtro = (termino ?? string.Empty).Trim();
        const string sql = """
            SELECT DISTINCT d.id_dueno, d.codigo_cliente, d.nombre_completo, d.documento,
                d.telefono_principal, d.telefono_alternativo, d.correo, d.direccion,
                d.observaciones, d.activo, d.fecha_creacion, d.fecha_modificacion,
                (SELECT COUNT(*) FROM mascotas mx WHERE mx.id_dueno = d.id_dueno AND mx.activo = 1) cantidad_mascotas,
                COALESCE((SELECT SUM(f.saldo_pendiente) FROM facturas f
                          WHERE f.id_dueno = d.id_dueno AND f.estado <> 'Anulada'), 0) saldo_pendiente
            FROM duenos d
            LEFT JOIN mascotas m ON m.id_dueno = d.id_dueno
            WHERE d.activo = 1
              AND (@Termino = ''
                   OR d.codigo_cliente LIKE @LikeTermino
                   OR d.nombre_completo LIKE @LikeTermino
                   OR d.documento LIKE @LikeTermino
                   OR d.telefono_principal LIKE @LikeTermino
                   OR d.telefono_alternativo LIKE @LikeTermino
                   OR d.correo LIKE @LikeTermino
                   OR m.codigo_paciente LIKE @LikeTermino
                   OR m.nombre LIKE @LikeTermino
                   OR m.microchip LIKE @LikeTermino)
            ORDER BY d.nombre_completo
            LIMIT 250;
            """;

        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Termino", MySqlDbType.VarChar).Value = filtro;
        comando.Parameters.Add("@LikeTermino", MySqlDbType.VarChar).Value = $"%{filtro}%";
        using MySqlDataReader lector = comando.ExecuteReader();
        List<DuenoModel> resultado = new();
        while (lector.Read())
        {
            resultado.Add(MapearDueno(lector));
        }
        return resultado;
    }

    public DuenoModel ObtenerDueno(long idDueno)
    {
        ExigirLectura();
        const string sql = """
            SELECT d.id_dueno, d.codigo_cliente, d.nombre_completo, d.documento,
                d.telefono_principal, d.telefono_alternativo, d.correo, d.direccion,
                d.observaciones, d.activo, d.fecha_creacion, d.fecha_modificacion,
                (SELECT COUNT(*) FROM mascotas mx WHERE mx.id_dueno = d.id_dueno AND mx.activo = 1) cantidad_mascotas,
                COALESCE((SELECT SUM(f.saldo_pendiente) FROM facturas f
                          WHERE f.id_dueno = d.id_dueno AND f.estado <> 'Anulada'), 0) saldo_pendiente
            FROM duenos d
            WHERE d.id_dueno = @IdDueno;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@IdDueno", MySqlDbType.UInt64).Value = idDueno;
        using MySqlDataReader lector = comando.ExecuteReader();
        if (!lector.Read())
        {
            throw new InvalidOperationException("No se encontró el dueño solicitado.");
        }
        return MapearDueno(lector);
    }

    public long GuardarDueno(DuenoModel dueno)
    {
        ExigirEdicionCliente();
        ValidarDueno(dueno);
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            const string insertar = """
                INSERT INTO duenos
                    (codigo_cliente, nombre_completo, documento, telefono_principal, telefono_alternativo,
                     correo, direccion, observaciones, activo)
                VALUES
                    (@CodigoTemporal, @Nombre, @Documento, @Telefono, @TelefonoAlternativo,
                     @Correo, @Direccion, @Observaciones, 1);
                """;
            using MySqlCommand comando = new(insertar, conexion, transaccion);
            comando.Parameters.Add("@CodigoTemporal", MySqlDbType.VarChar).Value = $"TMP-{Guid.NewGuid():N}"[..20];
            AgregarParametrosDueno(comando, dueno);
            comando.ExecuteNonQuery();
            long id = comando.LastInsertedId;
            string codigo = $"CLI-{id:000000}";
            using MySqlCommand actualizar = new("UPDATE duenos SET codigo_cliente = @Codigo WHERE id_dueno = @Id;", conexion, transaccion);
            actualizar.Parameters.Add("@Codigo", MySqlDbType.VarChar).Value = codigo;
            actualizar.Parameters.Add("@Id", MySqlDbType.UInt64).Value = id;
            actualizar.ExecuteNonQuery();
            transaccion.Commit();
            return id;
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            transaccion.Rollback();
            throw new InvalidOperationException("Ya existe un dueño con el mismo documento o código de cliente.", ex);
        }
        catch
        {
            transaccion.Rollback();
            throw;
        }
    }

    public void ActualizarDueno(DuenoModel dueno)
    {
        ExigirEdicionCliente();
        if (dueno.IdDueno <= 0)
        {
            throw new ArgumentException("Debe seleccionar un dueño válido.");
        }
        ValidarDueno(dueno);
        const string sql = """
            UPDATE duenos SET
                nombre_completo = @Nombre,
                documento = @Documento,
                telefono_principal = @Telefono,
                telefono_alternativo = @TelefonoAlternativo,
                correo = @Correo,
                direccion = @Direccion,
                observaciones = @Observaciones
            WHERE id_dueno = @Id AND activo = 1;
            """;
        try
        {
            using MySqlConnection conexion = Database.CrearConexion();
            conexion.Open();
            using MySqlCommand comando = new(sql, conexion);
            comando.Parameters.Add("@Id", MySqlDbType.UInt64).Value = dueno.IdDueno;
            AgregarParametrosDueno(comando, dueno);
            if (comando.ExecuteNonQuery() == 0)
            {
                throw new InvalidOperationException("El dueño ya no existe o se encuentra inactivo.");
            }
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            throw new InvalidOperationException("Ya existe un dueño con ese documento.", ex);
        }
    }

    public List<MascotaModel> ListarMascotas(long idDueno)
    {
        ExigirLectura();
        const string sql = """
            SELECT m.id_mascota, m.codigo_paciente, m.id_dueno, m.nombre, m.especie, m.raza,
                   m.sexo, m.color, m.fecha_nacimiento, m.peso_actual, m.esterilizado, m.microchip,
                   m.ruta_foto, m.estado_vital, m.fecha_fallecimiento, m.observaciones,
                   m.activo, m.fecha_creacion, m.fecha_modificacion,
                   (SELECT COUNT(*) FROM mascota_alertas_clinicas a
                    WHERE a.id_mascota = m.id_mascota AND a.activa = 1) alertas_activas,
                   (SELECT MIN(r.fecha_programada) FROM recordatorios r
                    WHERE r.id_mascota = m.id_mascota AND r.tipo_recordatorio = 'Vacuna'
                      AND r.estado IN ('Pendiente','Pospuesto')) proxima_vacuna,
                   COALESCE((SELECT SUM(f.saldo_pendiente) FROM facturas f
                             WHERE f.id_mascota = m.id_mascota AND f.estado <> 'Anulada'), 0) saldo_pendiente
            FROM mascotas m
            WHERE m.id_dueno = @IdDueno AND m.activo = 1
            ORDER BY m.nombre;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@IdDueno", MySqlDbType.UInt64).Value = idDueno;
        using MySqlDataReader lector = comando.ExecuteReader();
        List<MascotaModel> resultado = new();
        while (lector.Read())
        {
            resultado.Add(MapearMascota(lector));
        }
        return resultado;
    }

    public long GuardarMascota(MascotaModel mascota)
    {
        ExigirEdicionCliente();
        ValidarMascota(mascota);
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            using (MySqlCommand validarDueno = new("SELECT COUNT(*) FROM duenos WHERE id_dueno = @Id AND activo = 1;", conexion, transaccion))
            {
                validarDueno.Parameters.Add("@Id", MySqlDbType.UInt64).Value = mascota.IdDueno;
                if (Convert.ToInt32(validarDueno.ExecuteScalar()) != 1)
                {
                    throw new InvalidOperationException("El dueño seleccionado no existe o está inactivo.");
                }
            }

            const string sql = """
                INSERT INTO mascotas
                    (codigo_paciente, id_dueno, nombre, especie, raza, sexo, color, fecha_nacimiento,
                     peso_actual, esterilizado, microchip, ruta_foto, estado_vital, fecha_fallecimiento,
                     observaciones, activo)
                VALUES
                    (@CodigoTemporal, @IdDueno, @Nombre, @Especie, @Raza, @Sexo, @Color, @FechaNacimiento,
                     @Peso, @Esterilizado, @Microchip, @RutaFoto, @EstadoVital, @FechaFallecimiento,
                     @Observaciones, 1);
                """;
            using MySqlCommand comando = new(sql, conexion, transaccion);
            comando.Parameters.Add("@CodigoTemporal", MySqlDbType.VarChar).Value = $"TMP-{Guid.NewGuid():N}"[..20];
            AgregarParametrosMascota(comando, mascota);
            comando.ExecuteNonQuery();
            long id = comando.LastInsertedId;
            string codigo = $"PAC-{id:000000}";
            using MySqlCommand actualizar = new("UPDATE mascotas SET codigo_paciente = @Codigo WHERE id_mascota = @Id;", conexion, transaccion);
            actualizar.Parameters.Add("@Codigo", MySqlDbType.VarChar).Value = codigo;
            actualizar.Parameters.Add("@Id", MySqlDbType.UInt64).Value = id;
            actualizar.ExecuteNonQuery();
            transaccion.Commit();
            return id;
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            transaccion.Rollback();
            throw new InvalidOperationException("Ya existe una mascota con el mismo microchip o código de paciente.", ex);
        }
        catch
        {
            transaccion.Rollback();
            throw;
        }
    }

    public void ActualizarMascota(MascotaModel mascota)
    {
        ExigirEdicionCliente();
        if (mascota.IdMascota <= 0)
        {
            throw new ArgumentException("Debe seleccionar una mascota válida.");
        }
        ValidarMascota(mascota);
        const string sql = """
            UPDATE mascotas SET
                nombre = @Nombre, especie = @Especie, raza = @Raza, sexo = @Sexo,
                color = @Color, fecha_nacimiento = @FechaNacimiento, peso_actual = @Peso,
                esterilizado = @Esterilizado, microchip = @Microchip, ruta_foto = @RutaFoto,
                estado_vital = @EstadoVital, fecha_fallecimiento = @FechaFallecimiento,
                observaciones = @Observaciones
            WHERE id_mascota = @IdMascota AND id_dueno = @IdDueno AND activo = 1;
            """;
        try
        {
            using MySqlConnection conexion = Database.CrearConexion();
            conexion.Open();
            using MySqlCommand comando = new(sql, conexion);
            comando.Parameters.Add("@IdMascota", MySqlDbType.UInt64).Value = mascota.IdMascota;
            AgregarParametrosMascota(comando, mascota);
            if (comando.ExecuteNonQuery() == 0)
            {
                throw new InvalidOperationException("La mascota ya no existe o se encuentra inactiva.");
            }
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            throw new InvalidOperationException("Ya existe una mascota con ese microchip.", ex);
        }
    }

    public List<AlertaClinicaModel> ListarAlertas(long idMascota, bool soloActivas = false)
    {
        ExigirLectura();
        const string sql = """
            SELECT a.id_alerta, a.id_mascota, a.tipo_alerta, a.descripcion, a.activa,
                   a.fecha_registro, ur.nombre_completo usuario_registro,
                   a.fecha_cierre, uc.nombre_completo usuario_cierre
            FROM mascota_alertas_clinicas a
            INNER JOIN usuarios ur ON ur.id_usuario = a.id_usuario_registro
            LEFT JOIN usuarios uc ON uc.id_usuario = a.id_usuario_cierre
            WHERE a.id_mascota = @IdMascota AND (@SoloActivas = 0 OR a.activa = 1)
            ORDER BY a.activa DESC, a.fecha_registro DESC;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@IdMascota", MySqlDbType.UInt64).Value = idMascota;
        comando.Parameters.Add("@SoloActivas", MySqlDbType.Byte).Value = soloActivas ? 1 : 0;
        using MySqlDataReader lector = comando.ExecuteReader();
        List<AlertaClinicaModel> resultado = new();
        while (lector.Read())
        {
            resultado.Add(new AlertaClinicaModel
            {
                IdAlerta = lector.GetInt64("id_alerta"),
                IdMascota = lector.GetInt64("id_mascota"),
                TipoAlerta = lector.GetString("tipo_alerta"),
                Descripcion = lector.GetString("descripcion"),
                Activa = lector.GetBoolean("activa"),
                FechaRegistro = lector.GetDateTime("fecha_registro"),
                UsuarioRegistro = lector.GetString("usuario_registro"),
                FechaCierre = lector.IsDBNull("fecha_cierre") ? null : lector.GetDateTime("fecha_cierre"),
                UsuarioCierre = lector.IsDBNull("usuario_cierre") ? null : lector.GetString("usuario_cierre")
            });
        }
        return resultado;
    }

    public long RegistrarAlerta(AlertaClinicaModel alerta)
    {
        ExigirGestionAlerta();
        if (alerta.IdMascota <= 0)
        {
            throw new ArgumentException("Debe seleccionar una mascota.");
        }
        if (string.IsNullOrWhiteSpace(alerta.TipoAlerta) || string.IsNullOrWhiteSpace(alerta.Descripcion))
        {
            throw new ArgumentException("El tipo y la descripción de la alerta son obligatorios.");
        }
        UsuarioModel usuario = ObtenerUsuario();
        const string sql = """
            INSERT INTO mascota_alertas_clinicas
                (id_mascota, tipo_alerta, descripcion, activa, id_usuario_registro)
            VALUES (@IdMascota, @Tipo, @Descripcion, 1, @IdUsuario);
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@IdMascota", MySqlDbType.UInt64).Value = alerta.IdMascota;
        comando.Parameters.Add("@Tipo", MySqlDbType.VarChar).Value = alerta.TipoAlerta;
        comando.Parameters.Add("@Descripcion", MySqlDbType.VarChar).Value = alerta.Descripcion.Trim();
        comando.Parameters.Add("@IdUsuario", MySqlDbType.UInt32).Value = usuario.IdUsuario;
        comando.ExecuteNonQuery();
        return comando.LastInsertedId;
    }

    public void CerrarAlerta(long idAlerta)
    {
        ExigirGestionAlerta();
        UsuarioModel usuario = ObtenerUsuario();
        const string sql = """
            UPDATE mascota_alertas_clinicas
            SET activa = 0, fecha_cierre = NOW(), id_usuario_cierre = @IdUsuario
            WHERE id_alerta = @IdAlerta AND activa = 1;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@IdAlerta", MySqlDbType.UInt64).Value = idAlerta;
        comando.Parameters.Add("@IdUsuario", MySqlDbType.UInt32).Value = usuario.IdUsuario;
        if (comando.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException("La alerta seleccionada ya estaba cerrada o no existe.");
        }
    }

    public List<VacunaMascotaResumenModel> ListarVacunas(long idMascota)
    {
        ExigirLectura();
        const string sql = """
            SELECT cv.nombre vacuna, va.fecha_aplicacion, va.fecha_proxima_dosis,
                   COALESCE(va.lote_texto, il.numero_lote) lote, v.nombre_completo veterinario
            FROM vacunas_aplicadas va
            INNER JOIN catalogo_vacunas cv ON cv.id_vacuna = va.id_vacuna
            INNER JOIN veterinarios v ON v.id_veterinario = va.id_veterinario
            LEFT JOIN inventario_lotes il ON il.id_lote = va.id_lote_inventario
            WHERE va.id_mascota = @IdMascota
            ORDER BY va.fecha_aplicacion DESC;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@IdMascota", MySqlDbType.UInt64).Value = idMascota;
        using MySqlDataReader lector = comando.ExecuteReader();
        List<VacunaMascotaResumenModel> resultado = new();
        while (lector.Read())
        {
            resultado.Add(new VacunaMascotaResumenModel
            {
                Vacuna = lector.GetString("vacuna"),
                FechaAplicacion = lector.GetDateTime("fecha_aplicacion"),
                ProximaDosis = lector.IsDBNull("fecha_proxima_dosis") ? null : lector.GetDateTime("fecha_proxima_dosis"),
                Lote = lector.IsDBNull("lote") ? null : lector.GetString("lote"),
                Veterinario = lector.GetString("veterinario")
            });
        }
        return resultado;
    }

    public List<FacturaMascotaResumenModel> ListarFacturasMascota(long idMascota)
    {
        ExigirLectura();
        const string sql = """
            SELECT id_factura, numero_factura, fecha_emision, total, total_pagado, saldo_pendiente, estado
            FROM facturas
            WHERE id_mascota = @IdMascota
            ORDER BY fecha_emision DESC;
            """;
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@IdMascota", MySqlDbType.UInt64).Value = idMascota;
        using MySqlDataReader lector = comando.ExecuteReader();
        List<FacturaMascotaResumenModel> resultado = new();
        while (lector.Read())
        {
            resultado.Add(new FacturaMascotaResumenModel
            {
                IdFactura = lector.GetInt64("id_factura"),
                NumeroFactura = lector.GetString("numero_factura"),
                FechaEmision = lector.GetDateTime("fecha_emision"),
                Total = lector.GetDecimal("total"),
                TotalPagado = lector.GetDecimal("total_pagado"),
                SaldoPendiente = lector.GetDecimal("saldo_pendiente"),
                Estado = lector.GetString("estado")
            });
        }
        return resultado;
    }

    private static UsuarioModel ObtenerUsuario()
    {
        return SesionActual.Usuario ?? throw new UnauthorizedAccessException("No existe una sesión activa.");
    }

    private static void ExigirLectura()
    {
        SesionActual.ExigirRoles("Administrador", "Recepción", "Veterinario", "Caja");
    }

    private static void ExigirEdicionCliente()
    {
        SesionActual.ExigirRoles("Administrador", "Recepción");
    }

    private static void ExigirGestionAlerta()
    {
        SesionActual.ExigirRoles("Administrador", "Recepción", "Veterinario");
    }

    private static void ValidarDueno(DuenoModel dueno)
    {
        if (string.IsNullOrWhiteSpace(dueno.NombreCompleto))
        {
            throw new ArgumentException("El nombre completo del dueño es obligatorio.");
        }
        if (string.IsNullOrWhiteSpace(dueno.TelefonoPrincipal))
        {
            throw new ArgumentException("El teléfono principal del dueño es obligatorio.");
        }
        if (!string.IsNullOrWhiteSpace(dueno.Correo) && !dueno.Correo.Contains('@'))
        {
            throw new ArgumentException("El correo indicado no tiene un formato válido.");
        }
    }

    private static void ValidarMascota(MascotaModel mascota)
    {
        if (mascota.IdDueno <= 0)
        {
            throw new ArgumentException("Debe seleccionar un dueño para la mascota.");
        }
        if (string.IsNullOrWhiteSpace(mascota.Nombre))
        {
            throw new ArgumentException("El nombre de la mascota es obligatorio.");
        }
        if (string.IsNullOrWhiteSpace(mascota.Especie))
        {
            throw new ArgumentException("La especie de la mascota es obligatoria.");
        }
        if (mascota.PesoActual < 0)
        {
            throw new ArgumentException("El peso no puede ser negativo.");
        }
        if (string.Equals(mascota.EstadoVital, "Fallecida", StringComparison.OrdinalIgnoreCase) && !mascota.FechaFallecimiento.HasValue)
        {
            throw new ArgumentException("Debe registrar la fecha de fallecimiento.");
        }
        if (!string.Equals(mascota.EstadoVital, "Fallecida", StringComparison.OrdinalIgnoreCase))
        {
            mascota.FechaFallecimiento = null;
        }
    }

    private static void AgregarParametrosDueno(MySqlCommand comando, DuenoModel dueno)
    {
        comando.Parameters.Add("@Nombre", MySqlDbType.VarChar).Value = dueno.NombreCompleto.Trim();
        AgregarTextoNullable(comando, "@Documento", dueno.Documento);
        comando.Parameters.Add("@Telefono", MySqlDbType.VarChar).Value = dueno.TelefonoPrincipal.Trim();
        AgregarTextoNullable(comando, "@TelefonoAlternativo", dueno.TelefonoAlternativo);
        AgregarTextoNullable(comando, "@Correo", dueno.Correo);
        AgregarTextoNullable(comando, "@Direccion", dueno.Direccion);
        AgregarTextoNullable(comando, "@Observaciones", dueno.Observaciones);
    }

    private static void AgregarParametrosMascota(MySqlCommand comando, MascotaModel mascota)
    {
        comando.Parameters.Add("@IdDueno", MySqlDbType.UInt64).Value = mascota.IdDueno;
        comando.Parameters.Add("@Nombre", MySqlDbType.VarChar).Value = mascota.Nombre.Trim();
        comando.Parameters.Add("@Especie", MySqlDbType.VarChar).Value = mascota.Especie.Trim();
        AgregarTextoNullable(comando, "@Raza", mascota.Raza);
        comando.Parameters.Add("@Sexo", MySqlDbType.VarChar).Value = mascota.Sexo;
        AgregarTextoNullable(comando, "@Color", mascota.Color);
        comando.Parameters.Add("@FechaNacimiento", MySqlDbType.Date).Value = mascota.FechaNacimiento.HasValue ? mascota.FechaNacimiento.Value.Date : DBNull.Value;
        comando.Parameters.Add("@Peso", MySqlDbType.Decimal).Value = mascota.PesoActual.HasValue ? mascota.PesoActual.Value : DBNull.Value;
        comando.Parameters.Add("@Esterilizado", MySqlDbType.Byte).Value = mascota.Esterilizado ? 1 : 0;
        AgregarTextoNullable(comando, "@Microchip", mascota.Microchip);
        AgregarTextoNullable(comando, "@RutaFoto", mascota.RutaFoto);
        comando.Parameters.Add("@EstadoVital", MySqlDbType.VarChar).Value = mascota.EstadoVital;
        comando.Parameters.Add("@FechaFallecimiento", MySqlDbType.Date).Value = mascota.FechaFallecimiento.HasValue ? mascota.FechaFallecimiento.Value.Date : DBNull.Value;
        AgregarTextoNullable(comando, "@Observaciones", mascota.Observaciones);
    }

    private static void AgregarTextoNullable(MySqlCommand comando, string nombre, string? valor)
    {
        comando.Parameters.Add(nombre, MySqlDbType.VarChar).Value = string.IsNullOrWhiteSpace(valor) ? DBNull.Value : valor.Trim();
    }

    private static DuenoModel MapearDueno(MySqlDataReader lector)
    {
        return new DuenoModel
        {
            IdDueno = lector.GetInt64("id_dueno"),
            CodigoCliente = lector.GetString("codigo_cliente"),
            NombreCompleto = lector.GetString("nombre_completo"),
            Documento = lector.IsDBNull("documento") ? null : lector.GetString("documento"),
            TelefonoPrincipal = lector.GetString("telefono_principal"),
            TelefonoAlternativo = lector.IsDBNull("telefono_alternativo") ? null : lector.GetString("telefono_alternativo"),
            Correo = lector.IsDBNull("correo") ? null : lector.GetString("correo"),
            Direccion = lector.IsDBNull("direccion") ? null : lector.GetString("direccion"),
            Observaciones = lector.IsDBNull("observaciones") ? null : lector.GetString("observaciones"),
            Activo = lector.GetBoolean("activo"),
            FechaCreacion = lector.GetDateTime("fecha_creacion"),
            FechaModificacion = lector.GetDateTime("fecha_modificacion"),
            CantidadMascotas = Convert.ToInt32(lector["cantidad_mascotas"]),
            SaldoPendiente = Convert.ToDecimal(lector["saldo_pendiente"])
        };
    }

    private static MascotaModel MapearMascota(MySqlDataReader lector)
    {
        return new MascotaModel
        {
            IdMascota = lector.GetInt64("id_mascota"),
            CodigoPaciente = lector.GetString("codigo_paciente"),
            IdDueno = lector.GetInt64("id_dueno"),
            Nombre = lector.GetString("nombre"),
            Especie = lector.GetString("especie"),
            Raza = lector.IsDBNull("raza") ? null : lector.GetString("raza"),
            Sexo = lector.GetString("sexo"),
            Color = lector.IsDBNull("color") ? null : lector.GetString("color"),
            FechaNacimiento = lector.IsDBNull("fecha_nacimiento") ? null : lector.GetDateTime("fecha_nacimiento"),
            PesoActual = lector.IsDBNull("peso_actual") ? null : lector.GetDecimal("peso_actual"),
            Esterilizado = lector.GetBoolean("esterilizado"),
            Microchip = lector.IsDBNull("microchip") ? null : lector.GetString("microchip"),
            RutaFoto = lector.IsDBNull("ruta_foto") ? null : lector.GetString("ruta_foto"),
            EstadoVital = lector.GetString("estado_vital"),
            FechaFallecimiento = lector.IsDBNull("fecha_fallecimiento") ? null : lector.GetDateTime("fecha_fallecimiento"),
            Observaciones = lector.IsDBNull("observaciones") ? null : lector.GetString("observaciones"),
            Activo = lector.GetBoolean("activo"),
            FechaCreacion = lector.GetDateTime("fecha_creacion"),
            FechaModificacion = lector.GetDateTime("fecha_modificacion"),
            AlertasActivas = Convert.ToInt32(lector["alertas_activas"]),
            ProximaVacuna = lector.IsDBNull("proxima_vacuna") ? null : lector.GetDateTime("proxima_vacuna"),
            SaldoPendiente = Convert.ToDecimal(lector["saldo_pendiente"])
        };
    }
}
