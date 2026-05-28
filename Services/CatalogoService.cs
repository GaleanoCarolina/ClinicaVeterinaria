using System;
using System.Collections.Generic;
using System.Data;
using ClinicaVeterinaria.Data;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Utils;
using MySql.Data.MySqlClient;

namespace ClinicaVeterinaria.Services;

public sealed class CatalogoService
{
    public List<ServicioCatalogoModel> ListarServicios(bool incluirInactivos = true)
    {
        ExigirAdmin();
        using MySqlConnection conexion = Abrir();
        using MySqlCommand comando = new("""
            SELECT id_servicio, codigo, nombre, COALESCE(descripcion,'') descripcion,
                   precio_base, duracion_minutos, genera_cargo, activo
            FROM catalogo_servicios
            WHERE (@Todos = 1 OR activo = 1)
            ORDER BY nombre;
            """, conexion);
        comando.Parameters.Add("@Todos", MySqlDbType.Bit).Value = incluirInactivos;
        using MySqlDataReader lector = comando.ExecuteReader();
        List<ServicioCatalogoModel> lista = new();
        while (lector.Read())
        {
            lista.Add(new ServicioCatalogoModel
            {
                IdServicio = lector.GetInt32("id_servicio"),
                Codigo = lector.GetString("codigo"),
                Nombre = lector.GetString("nombre"),
                Descripcion = lector.GetString("descripcion"),
                PrecioBase = lector.GetDecimal("precio_base"),
                DuracionMinutos = lector.GetInt32("duracion_minutos"),
                GeneraCargo = lector.GetBoolean("genera_cargo"),
                Activo = lector.GetBoolean("activo")
            });
        }
        return lista;
    }

    public void GuardarServicio(ServicioCatalogoModel item)
    {
        ExigirAdmin();
        ValidarNombrePrecio(item.Nombre, item.PrecioBase);
        if (item.DuracionMinutos <= 0 || item.DuracionMinutos % 30 != 0)
        {
            throw new InvalidOperationException("La duración debe ser múltiplo de 30 minutos.");
        }

        using MySqlConnection conexion = Abrir();
        if (item.IdServicio == 0)
        {
            using MySqlTransaction tx = conexion.BeginTransaction(IsolationLevel.ReadCommitted);
            try
            {
                using MySqlCommand insertar = new("""
                    INSERT INTO catalogo_servicios
                      (codigo, nombre, descripcion, precio_base, duracion_minutos, genera_cargo, activo)
                    VALUES
                      (@Codigo, @Nombre, @Descripcion, @Precio, @Duracion, @Cargo, @Activo);
                    """, conexion, tx);
                insertar.Parameters.Add("@Codigo", MySqlDbType.VarChar).Value = CodigoTemporal();
                AgregarServicio(insertar, item);
                insertar.ExecuteNonQuery();
                int id = Convert.ToInt32(insertar.LastInsertedId);
                ActualizarCodigo(conexion, tx, "catalogo_servicios", "id_servicio", id, $"SER-{id:000000}");
                tx.Commit();
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                tx.Rollback();
                throw new InvalidOperationException("Ya existe un servicio con el nombre o código indicado.", ex);
            }
            return;
        }

        using MySqlCommand actualizar = new("""
            UPDATE catalogo_servicios SET
                nombre=@Nombre, descripcion=@Descripcion, precio_base=@Precio,
                duracion_minutos=@Duracion, genera_cargo=@Cargo, activo=@Activo
            WHERE id_servicio=@Id;
            """, conexion);
        AgregarServicio(actualizar, item);
        actualizar.Parameters.Add("@Id", MySqlDbType.Int32).Value = item.IdServicio;
        actualizar.ExecuteNonQuery();
    }

    public List<MedicamentoCatalogoModel> ListarMedicamentos(bool incluirInactivos = true)
    {
        ExigirAdmin();
        using MySqlConnection conexion = Abrir();
        using MySqlCommand comando = new("""
            SELECT m.id_medicamento, m.codigo, m.nombre, COALESCE(m.presentacion,'') presentacion,
                   COALESCE(m.concentracion,'') concentracion, COALESCE(m.via_administracion,'') via_administracion,
                   COALESCE(m.indicaciones_predeterminadas,'') indicaciones_predeterminadas, m.precio_venta,
                   m.controla_inventario, m.id_producto_inventario, COALESCE(p.nombre,'') producto_inventario, m.activo
            FROM catalogo_medicamentos m
            LEFT JOIN inventario_productos p ON p.id_producto=m.id_producto_inventario
            WHERE (@Todos=1 OR m.activo=1)
            ORDER BY m.nombre;
            """, conexion);
        comando.Parameters.Add("@Todos", MySqlDbType.Bit).Value = incluirInactivos;
        using MySqlDataReader lector = comando.ExecuteReader();
        List<MedicamentoCatalogoModel> lista = new();
        while (lector.Read())
        {
            lista.Add(new MedicamentoCatalogoModel
            {
                IdMedicamento = lector.GetInt32("id_medicamento"),
                Codigo = lector.GetString("codigo"),
                Nombre = lector.GetString("nombre"),
                Presentacion = lector.GetString("presentacion"),
                Concentracion = lector.GetString("concentracion"),
                ViaAdministracion = lector.GetString("via_administracion"),
                IndicacionesPredeterminadas = lector.GetString("indicaciones_predeterminadas"),
                PrecioVenta = lector.GetDecimal("precio_venta"),
                ControlaInventario = lector.GetBoolean("controla_inventario"),
                IdProductoInventario = lector.IsDBNull(lector.GetOrdinal("id_producto_inventario")) ? null : lector.GetInt64("id_producto_inventario"),
                ProductoInventario = lector.GetString("producto_inventario"),
                Activo = lector.GetBoolean("activo")
            });
        }
        return lista;
    }

    public void GuardarMedicamento(MedicamentoCatalogoModel item)
    {
        ExigirAdmin();
        ValidarClinico(item.Nombre, item.PrecioVenta, item.ControlaInventario, item.IdProductoInventario);
        using MySqlConnection conexion = Abrir();

        if (item.IdMedicamento == 0)
        {
            using MySqlTransaction tx = conexion.BeginTransaction(IsolationLevel.ReadCommitted);
            try
            {
                using MySqlCommand insertar = new("""
                    INSERT INTO catalogo_medicamentos
                      (codigo,nombre,presentacion,concentracion,via_administracion,
                       indicaciones_predeterminadas,precio_venta,controla_inventario,id_producto_inventario,activo)
                    VALUES
                      (@Codigo,@Nombre,@Presentacion,@Concentracion,@Via,@Indicaciones,
                       @Precio,@Controla,@Producto,@Activo);
                    """, conexion, tx);
                insertar.Parameters.Add("@Codigo", MySqlDbType.VarChar).Value = CodigoTemporal();
                AgregarMedicamento(insertar, item);
                insertar.ExecuteNonQuery();
                int id = Convert.ToInt32(insertar.LastInsertedId);
                ActualizarCodigo(conexion, tx, "catalogo_medicamentos", "id_medicamento", id, $"MED-{id:000000}");
                tx.Commit();
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                tx.Rollback();
                throw new InvalidOperationException("No fue posible registrar el medicamento porque existe un dato duplicado.", ex);
            }
            return;
        }

        using MySqlCommand actualizar = new("""
            UPDATE catalogo_medicamentos SET
                nombre=@Nombre, presentacion=@Presentacion, concentracion=@Concentracion,
                via_administracion=@Via, indicaciones_predeterminadas=@Indicaciones,
                precio_venta=@Precio, controla_inventario=@Controla,
                id_producto_inventario=@Producto, activo=@Activo
            WHERE id_medicamento=@Id;
            """, conexion);
        AgregarMedicamento(actualizar, item);
        actualizar.Parameters.Add("@Id", MySqlDbType.Int32).Value = item.IdMedicamento;
        actualizar.ExecuteNonQuery();
    }

    public List<VacunaCatalogoModel> ListarVacunas(bool incluirInactivos = true)
    {
        ExigirAdmin();
        using MySqlConnection conexion = Abrir();
        using MySqlCommand comando = new("""
            SELECT v.id_vacuna, v.codigo, v.nombre, COALESCE(v.especie_aplicable,'') especie_aplicable,
                   COALESCE(v.descripcion,'') descripcion, v.intervalo_dias_sugerido, v.precio_base,
                   v.controla_inventario, v.id_producto_inventario, COALESCE(p.nombre,'') producto_inventario, v.activo
            FROM catalogo_vacunas v
            LEFT JOIN inventario_productos p ON p.id_producto=v.id_producto_inventario
            WHERE (@Todos=1 OR v.activo=1)
            ORDER BY v.nombre;
            """, conexion);
        comando.Parameters.Add("@Todos", MySqlDbType.Bit).Value = incluirInactivos;
        using MySqlDataReader lector = comando.ExecuteReader();
        List<VacunaCatalogoModel> lista = new();
        while (lector.Read())
        {
            lista.Add(new VacunaCatalogoModel
            {
                IdVacuna = lector.GetInt32("id_vacuna"),
                Codigo = lector.GetString("codigo"),
                Nombre = lector.GetString("nombre"),
                EspecieAplicable = lector.GetString("especie_aplicable"),
                Descripcion = lector.GetString("descripcion"),
                IntervaloDiasSugerido = lector.IsDBNull(lector.GetOrdinal("intervalo_dias_sugerido")) ? null : lector.GetInt32("intervalo_dias_sugerido"),
                PrecioBase = lector.GetDecimal("precio_base"),
                ControlaInventario = lector.GetBoolean("controla_inventario"),
                IdProductoInventario = lector.IsDBNull(lector.GetOrdinal("id_producto_inventario")) ? null : lector.GetInt64("id_producto_inventario"),
                ProductoInventario = lector.GetString("producto_inventario"),
                Activo = lector.GetBoolean("activo")
            });
        }
        return lista;
    }

    public void GuardarVacuna(VacunaCatalogoModel item)
    {
        ExigirAdmin();
        ValidarClinico(item.Nombre, item.PrecioBase, item.ControlaInventario, item.IdProductoInventario);
        ValidarIntervalo(item.IntervaloDiasSugerido);
        using MySqlConnection conexion = Abrir();

        if (item.IdVacuna == 0)
        {
            using MySqlTransaction tx = conexion.BeginTransaction(IsolationLevel.ReadCommitted);
            try
            {
                using MySqlCommand insertar = new("""
                    INSERT INTO catalogo_vacunas
                      (codigo,nombre,especie_aplicable,descripcion,intervalo_dias_sugerido,
                       precio_base,controla_inventario,id_producto_inventario,activo)
                    VALUES
                      (@Codigo,@Nombre,@Especie,@Descripcion,@Dias,@Precio,@Controla,@Producto,@Activo);
                    """, conexion, tx);
                insertar.Parameters.Add("@Codigo", MySqlDbType.VarChar).Value = CodigoTemporal();
                AgregarVacuna(insertar, item);
                insertar.ExecuteNonQuery();
                int id = Convert.ToInt32(insertar.LastInsertedId);
                ActualizarCodigo(conexion, tx, "catalogo_vacunas", "id_vacuna", id, $"VAC-{id:000000}");
                tx.Commit();
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                tx.Rollback();
                throw new InvalidOperationException("No fue posible registrar la vacuna porque existe un dato duplicado.", ex);
            }
            return;
        }

        using MySqlCommand actualizar = new("""
            UPDATE catalogo_vacunas SET
                nombre=@Nombre, especie_aplicable=@Especie, descripcion=@Descripcion,
                intervalo_dias_sugerido=@Dias, precio_base=@Precio, controla_inventario=@Controla,
                id_producto_inventario=@Producto, activo=@Activo
            WHERE id_vacuna=@Id;
            """, conexion);
        AgregarVacuna(actualizar, item);
        actualizar.Parameters.Add("@Id", MySqlDbType.Int32).Value = item.IdVacuna;
        actualizar.ExecuteNonQuery();
    }

    public List<DesparasitanteCatalogoModel> ListarDesparasitantes(bool incluirInactivos = true)
    {
        ExigirAdmin();
        using MySqlConnection conexion = Abrir();
        using MySqlCommand comando = new("""
            SELECT d.id_desparasitante,d.codigo,d.nombre,COALESCE(d.presentacion,'') presentacion,
                   COALESCE(d.dosis_sugerida,'') dosis_sugerida,d.intervalo_dias_sugerido,d.precio_base,
                   d.controla_inventario,d.id_producto_inventario,COALESCE(p.nombre,'') producto_inventario,d.activo
            FROM catalogo_desparasitantes d
            LEFT JOIN inventario_productos p ON p.id_producto=d.id_producto_inventario
            WHERE (@Todos=1 OR d.activo=1)
            ORDER BY d.nombre;
            """, conexion);
        comando.Parameters.Add("@Todos", MySqlDbType.Bit).Value = incluirInactivos;
        using MySqlDataReader lector = comando.ExecuteReader();
        List<DesparasitanteCatalogoModel> lista = new();
        while (lector.Read())
        {
            lista.Add(new DesparasitanteCatalogoModel
            {
                IdDesparasitante = lector.GetInt32("id_desparasitante"),
                Codigo = lector.GetString("codigo"),
                Nombre = lector.GetString("nombre"),
                Presentacion = lector.GetString("presentacion"),
                DosisSugerida = lector.GetString("dosis_sugerida"),
                IntervaloDiasSugerido = lector.IsDBNull(lector.GetOrdinal("intervalo_dias_sugerido")) ? null : lector.GetInt32("intervalo_dias_sugerido"),
                PrecioBase = lector.GetDecimal("precio_base"),
                ControlaInventario = lector.GetBoolean("controla_inventario"),
                IdProductoInventario = lector.IsDBNull(lector.GetOrdinal("id_producto_inventario")) ? null : lector.GetInt64("id_producto_inventario"),
                ProductoInventario = lector.GetString("producto_inventario"),
                Activo = lector.GetBoolean("activo")
            });
        }
        return lista;
    }

    public void GuardarDesparasitante(DesparasitanteCatalogoModel item)
    {
        ExigirAdmin();
        ValidarClinico(item.Nombre, item.PrecioBase, item.ControlaInventario, item.IdProductoInventario);
        ValidarIntervalo(item.IntervaloDiasSugerido);
        using MySqlConnection conexion = Abrir();

        if (item.IdDesparasitante == 0)
        {
            using MySqlTransaction tx = conexion.BeginTransaction(IsolationLevel.ReadCommitted);
            try
            {
                using MySqlCommand insertar = new("""
                    INSERT INTO catalogo_desparasitantes
                      (codigo,nombre,presentacion,dosis_sugerida,intervalo_dias_sugerido,
                       precio_base,controla_inventario,id_producto_inventario,activo)
                    VALUES
                      (@Codigo,@Nombre,@Presentacion,@Dosis,@Dias,@Precio,@Controla,@Producto,@Activo);
                    """, conexion, tx);
                insertar.Parameters.Add("@Codigo", MySqlDbType.VarChar).Value = CodigoTemporal();
                AgregarDesparasitante(insertar, item);
                insertar.ExecuteNonQuery();
                int id = Convert.ToInt32(insertar.LastInsertedId);
                ActualizarCodigo(conexion, tx, "catalogo_desparasitantes", "id_desparasitante", id, $"DES-{id:000000}");
                tx.Commit();
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                tx.Rollback();
                throw new InvalidOperationException("No fue posible registrar el desparasitante porque existe un dato duplicado.", ex);
            }
            return;
        }

        using MySqlCommand actualizar = new("""
            UPDATE catalogo_desparasitantes SET
                nombre=@Nombre, presentacion=@Presentacion, dosis_sugerida=@Dosis,
                intervalo_dias_sugerido=@Dias, precio_base=@Precio, controla_inventario=@Controla,
                id_producto_inventario=@Producto, activo=@Activo
            WHERE id_desparasitante=@Id;
            """, conexion);
        AgregarDesparasitante(actualizar, item);
        actualizar.Parameters.Add("@Id", MySqlDbType.Int32).Value = item.IdDesparasitante;
        actualizar.ExecuteNonQuery();
    }

    public List<MetodoPagoCatalogoModel> ListarMetodosPago()
    {
        ExigirAdmin();
        using MySqlConnection conexion = Abrir();
        using MySqlCommand comando = new("SELECT id_metodo_pago,nombre,activo FROM metodos_pago ORDER BY nombre;", conexion);
        using MySqlDataReader lector = comando.ExecuteReader();
        List<MetodoPagoCatalogoModel> lista = new();
        while (lector.Read())
        {
            lista.Add(new MetodoPagoCatalogoModel
            {
                IdMetodoPago = lector.GetInt32("id_metodo_pago"),
                Nombre = lector.GetString("nombre"),
                Activo = lector.GetBoolean("activo")
            });
        }
        return lista;
    }

    public void GuardarMetodoPago(MetodoPagoCatalogoModel item)
    {
        ExigirAdmin();
        if (string.IsNullOrWhiteSpace(item.Nombre))
        {
            throw new InvalidOperationException("El nombre es obligatorio.");
        }

        using MySqlConnection conexion = Abrir();
        string sql = item.IdMetodoPago == 0
            ? "INSERT INTO metodos_pago (nombre,activo) VALUES (@Nombre,@Activo);"
            : "UPDATE metodos_pago SET nombre=@Nombre,activo=@Activo WHERE id_metodo_pago=@Id;";
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Nombre", MySqlDbType.VarChar).Value = item.Nombre.Trim();
        comando.Parameters.Add("@Activo", MySqlDbType.Bit).Value = item.Activo;
        comando.Parameters.Add("@Id", MySqlDbType.Int32).Value = item.IdMetodoPago;
        EjecutarDuplicado(comando, "Ya existe un método de pago con ese nombre.");
    }

    public List<TipoBloqueoCatalogoModel> ListarTiposBloqueo()
    {
        ExigirAdmin();
        using MySqlConnection conexion = Abrir();
        using MySqlCommand comando = new("SELECT id_tipo_bloqueo,nombre,activo FROM tipos_bloqueo ORDER BY nombre;", conexion);
        using MySqlDataReader lector = comando.ExecuteReader();
        List<TipoBloqueoCatalogoModel> lista = new();
        while (lector.Read())
        {
            lista.Add(new TipoBloqueoCatalogoModel
            {
                IdTipoBloqueo = lector.GetInt32("id_tipo_bloqueo"),
                Nombre = lector.GetString("nombre"),
                Activo = lector.GetBoolean("activo")
            });
        }
        return lista;
    }

    public void GuardarTipoBloqueo(TipoBloqueoCatalogoModel item)
    {
        ExigirAdmin();
        if (string.IsNullOrWhiteSpace(item.Nombre))
        {
            throw new InvalidOperationException("El nombre es obligatorio.");
        }

        using MySqlConnection conexion = Abrir();
        string sql = item.IdTipoBloqueo == 0
            ? "INSERT INTO tipos_bloqueo (nombre,activo) VALUES (@Nombre,@Activo);"
            : "UPDATE tipos_bloqueo SET nombre=@Nombre,activo=@Activo WHERE id_tipo_bloqueo=@Id;";
        using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.Add("@Nombre", MySqlDbType.VarChar).Value = item.Nombre.Trim();
        comando.Parameters.Add("@Activo", MySqlDbType.Bit).Value = item.Activo;
        comando.Parameters.Add("@Id", MySqlDbType.Int32).Value = item.IdTipoBloqueo;
        EjecutarDuplicado(comando, "Ya existe un tipo de bloqueo con ese nombre.");
    }

    public List<ProductoCatalogoVinculoModel> ListarProductosActivos()
    {
        ExigirAdmin();
        using MySqlConnection conexion = Abrir();
        using MySqlCommand comando = new("""
            SELECT id_producto,nombre,categoria
            FROM inventario_productos
            WHERE activo=1
            ORDER BY nombre;
            """, conexion);
        using MySqlDataReader lector = comando.ExecuteReader();
        List<ProductoCatalogoVinculoModel> lista = new();
        while (lector.Read())
        {
            lista.Add(new ProductoCatalogoVinculoModel
            {
                IdProducto = lector.GetInt64("id_producto"),
                Nombre = lector.GetString("nombre"),
                Categoria = lector.GetString("categoria")
            });
        }
        return lista;
    }

    private static void AgregarServicio(MySqlCommand comando, ServicioCatalogoModel item)
    {
        comando.Parameters.Add("@Nombre", MySqlDbType.VarChar).Value = item.Nombre.Trim();
        comando.Parameters.Add("@Descripcion", MySqlDbType.VarChar).Value = Nulo(item.Descripcion);
        comando.Parameters.Add("@Precio", MySqlDbType.Decimal).Value = item.PrecioBase;
        comando.Parameters.Add("@Duracion", MySqlDbType.Int32).Value = item.DuracionMinutos;
        comando.Parameters.Add("@Cargo", MySqlDbType.Bit).Value = item.GeneraCargo;
        comando.Parameters.Add("@Activo", MySqlDbType.Bit).Value = item.Activo;
    }

    private static void AgregarMedicamento(MySqlCommand comando, MedicamentoCatalogoModel item)
    {
        comando.Parameters.Add("@Nombre", MySqlDbType.VarChar).Value = item.Nombre.Trim();
        comando.Parameters.Add("@Presentacion", MySqlDbType.VarChar).Value = Nulo(item.Presentacion);
        comando.Parameters.Add("@Concentracion", MySqlDbType.VarChar).Value = Nulo(item.Concentracion);
        comando.Parameters.Add("@Via", MySqlDbType.VarChar).Value = Nulo(item.ViaAdministracion);
        comando.Parameters.Add("@Indicaciones", MySqlDbType.Text).Value = Nulo(item.IndicacionesPredeterminadas);
        comando.Parameters.Add("@Precio", MySqlDbType.Decimal).Value = item.PrecioVenta;
        comando.Parameters.Add("@Controla", MySqlDbType.Bit).Value = item.ControlaInventario;
        comando.Parameters.Add("@Producto", MySqlDbType.UInt64).Value = item.IdProductoInventario.HasValue ? item.IdProductoInventario.Value : DBNull.Value;
        comando.Parameters.Add("@Activo", MySqlDbType.Bit).Value = item.Activo;
    }

    private static void AgregarVacuna(MySqlCommand comando, VacunaCatalogoModel item)
    {
        comando.Parameters.Add("@Nombre", MySqlDbType.VarChar).Value = item.Nombre.Trim();
        comando.Parameters.Add("@Especie", MySqlDbType.VarChar).Value = Nulo(item.EspecieAplicable);
        comando.Parameters.Add("@Descripcion", MySqlDbType.VarChar).Value = Nulo(item.Descripcion);
        comando.Parameters.Add("@Dias", MySqlDbType.Int32).Value = item.IntervaloDiasSugerido.HasValue ? item.IntervaloDiasSugerido.Value : DBNull.Value;
        comando.Parameters.Add("@Precio", MySqlDbType.Decimal).Value = item.PrecioBase;
        comando.Parameters.Add("@Controla", MySqlDbType.Bit).Value = item.ControlaInventario;
        comando.Parameters.Add("@Producto", MySqlDbType.UInt64).Value = item.IdProductoInventario.HasValue ? item.IdProductoInventario.Value : DBNull.Value;
        comando.Parameters.Add("@Activo", MySqlDbType.Bit).Value = item.Activo;
    }

    private static void AgregarDesparasitante(MySqlCommand comando, DesparasitanteCatalogoModel item)
    {
        comando.Parameters.Add("@Nombre", MySqlDbType.VarChar).Value = item.Nombre.Trim();
        comando.Parameters.Add("@Presentacion", MySqlDbType.VarChar).Value = Nulo(item.Presentacion);
        comando.Parameters.Add("@Dosis", MySqlDbType.VarChar).Value = Nulo(item.DosisSugerida);
        comando.Parameters.Add("@Dias", MySqlDbType.Int32).Value = item.IntervaloDiasSugerido.HasValue ? item.IntervaloDiasSugerido.Value : DBNull.Value;
        comando.Parameters.Add("@Precio", MySqlDbType.Decimal).Value = item.PrecioBase;
        comando.Parameters.Add("@Controla", MySqlDbType.Bit).Value = item.ControlaInventario;
        comando.Parameters.Add("@Producto", MySqlDbType.UInt64).Value = item.IdProductoInventario.HasValue ? item.IdProductoInventario.Value : DBNull.Value;
        comando.Parameters.Add("@Activo", MySqlDbType.Bit).Value = item.Activo;
    }

    private static void ActualizarCodigo(MySqlConnection conexion, MySqlTransaction tx, string tabla, string llave, int id, string codigo)
    {
        using MySqlCommand actualizar = new($"UPDATE {tabla} SET codigo=@Codigo WHERE {llave}=@Id;", conexion, tx);
        actualizar.Parameters.Add("@Codigo", MySqlDbType.VarChar).Value = codigo;
        actualizar.Parameters.Add("@Id", MySqlDbType.Int32).Value = id;
        actualizar.ExecuteNonQuery();
    }

    private static string CodigoTemporal() => "TMP-" + Guid.NewGuid().ToString("N")[..20];

    private static MySqlConnection Abrir()
    {
        MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        return conexion;
    }

    private static object Nulo(string texto) => string.IsNullOrWhiteSpace(texto) ? DBNull.Value : texto.Trim();

    private static void ValidarNombrePrecio(string nombre, decimal precio)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new InvalidOperationException("El nombre es obligatorio.");
        }
        if (precio < 0)
        {
            throw new InvalidOperationException("El precio no puede ser negativo.");
        }
    }

    private static void ValidarClinico(string nombre, decimal precio, bool controla, long? producto)
    {
        ValidarNombrePrecio(nombre, precio);
        if (controla && !producto.HasValue)
        {
            throw new InvalidOperationException("Seleccione el producto de inventario cuando el catálogo controla existencias.");
        }
    }

    private static void ValidarIntervalo(int? dias)
    {
        if (dias.HasValue && dias <= 0)
        {
            throw new InvalidOperationException("El intervalo debe ser mayor que cero.");
        }
    }

    private static void EjecutarDuplicado(MySqlCommand comando, string mensaje)
    {
        try
        {
            comando.ExecuteNonQuery();
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            throw new InvalidOperationException(mensaje, ex);
        }
    }

    private static void ExigirAdmin() => SesionActual.ExigirRoles("Administrador");
}
