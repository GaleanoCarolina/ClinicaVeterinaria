using System;
using System.Collections.Generic;
using ClinicaVeterinaria.Data;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Utils;
using MySql.Data.MySqlClient;

namespace ClinicaVeterinaria.Services;

public sealed class InventarioService
{
    private static readonly HashSet<string> Categorias = new(StringComparer.OrdinalIgnoreCase)
    {
        "Medicamento", "Vacuna", "Desparasitante", "Insumo", "Producto de venta"
    };

    private static readonly HashSet<string> TiposSalidaManual = new(StringComparer.OrdinalIgnoreCase)
    {
        "Salida por venta", "Ajuste negativo", "Merma", "Vencimiento"
    };

    public ResumenInventarioModel ObtenerResumen()
    {
        ExigirPermiso();
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(@"
SELECT
  (SELECT COUNT(*) FROM inventario_productos WHERE activo = 1) AS productos_activos,
  (SELECT COUNT(*) FROM (
       SELECT p.id_producto
       FROM inventario_productos p
       LEFT JOIN inventario_lotes l ON l.id_producto = p.id_producto AND l.estado <> 'Vencido'
       WHERE p.activo = 1
       GROUP BY p.id_producto, p.stock_minimo
       HAVING COALESCE(SUM(l.cantidad_disponible), 0) <= p.stock_minimo
   ) bajos) AS productos_bajos,
  (SELECT COUNT(*) FROM inventario_lotes l INNER JOIN inventario_productos p ON p.id_producto = l.id_producto
       WHERE p.activo = 1 AND l.estado = 'Disponible' AND l.cantidad_disponible > 0
       AND l.fecha_vencimiento BETWEEN CURDATE() AND DATE_ADD(CURDATE(), INTERVAL 30 DAY)) AS lotes_vencer,
  (SELECT COUNT(*) FROM inventario_lotes l INNER JOIN inventario_productos p ON p.id_producto = l.id_producto
       WHERE p.activo = 1 AND l.cantidad_disponible > 0 AND l.fecha_vencimiento < CURDATE()) AS lotes_vencidos,
  (SELECT COALESCE(SUM(l.cantidad_disponible * l.costo_unitario), 0)
       FROM inventario_lotes l INNER JOIN inventario_productos p ON p.id_producto = l.id_producto
       WHERE p.activo = 1 AND l.estado <> 'Vencido') AS valor_stock;", conexion);
        using MySqlDataReader lector = comando.ExecuteReader();
        lector.Read();
        return new ResumenInventarioModel
        {
            ProductosActivos = lector.GetInt32("productos_activos"),
            ProductosStockBajo = lector.GetInt32("productos_bajos"),
            LotesPorVencer = lector.GetInt32("lotes_vencer"),
            LotesVencidosConStock = lector.GetInt32("lotes_vencidos"),
            ValorStock = lector.GetDecimal("valor_stock")
        };
    }

    public List<ProductoInventarioModel> ListarProductos(string filtro = "", bool incluirInactivos = false)
    {
        ExigirPermiso();
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(@"
SELECT p.id_producto, p.codigo, p.nombre, p.categoria, COALESCE(p.presentacion, '') AS presentacion,
       p.unidad_medida, p.precio_compra, p.precio_venta, p.stock_minimo, p.controla_lotes, p.activo,
       COALESCE(SUM(CASE WHEN l.estado <> 'Vencido' THEN l.cantidad_disponible ELSE 0 END), 0) AS stock_disponible,
       MIN(CASE WHEN l.estado = 'Disponible' AND l.cantidad_disponible > 0 THEN l.fecha_vencimiento END) AS proximo_vencimiento
FROM inventario_productos p
LEFT JOIN inventario_lotes l ON l.id_producto = p.id_producto
WHERE (@inactivos = 1 OR p.activo = 1)
  AND (@filtro = '' OR p.codigo LIKE @buscar OR p.nombre LIKE @buscar OR p.categoria LIKE @buscar)
GROUP BY p.id_producto, p.codigo, p.nombre, p.categoria, p.presentacion, p.unidad_medida,
         p.precio_compra, p.precio_venta, p.stock_minimo, p.controla_lotes, p.activo
ORDER BY p.activo DESC, p.nombre;", conexion);
        comando.Parameters.AddWithValue("@inactivos", incluirInactivos);
        comando.Parameters.AddWithValue("@filtro", filtro.Trim());
        comando.Parameters.AddWithValue("@buscar", $"%{filtro.Trim()}%");
        using MySqlDataReader lector = comando.ExecuteReader();
        List<ProductoInventarioModel> resultado = new();
        while (lector.Read()) resultado.Add(MapearProducto(lector));
        return resultado;
    }

    public ProductoInventarioModel ObtenerProducto(long idProducto)
    {
        ExigirPermiso();
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(@"
SELECT p.id_producto, p.codigo, p.nombre, p.categoria, COALESCE(p.presentacion, '') AS presentacion,
       p.unidad_medida, p.precio_compra, p.precio_venta, p.stock_minimo, p.controla_lotes, p.activo,
       COALESCE(SUM(CASE WHEN l.estado <> 'Vencido' THEN l.cantidad_disponible ELSE 0 END), 0) AS stock_disponible,
       MIN(CASE WHEN l.estado = 'Disponible' AND l.cantidad_disponible > 0 THEN l.fecha_vencimiento END) AS proximo_vencimiento
FROM inventario_productos p LEFT JOIN inventario_lotes l ON l.id_producto = p.id_producto
WHERE p.id_producto = @id
GROUP BY p.id_producto, p.codigo, p.nombre, p.categoria, p.presentacion, p.unidad_medida,
         p.precio_compra, p.precio_venta, p.stock_minimo, p.controla_lotes, p.activo;", conexion);
        comando.Parameters.AddWithValue("@id", idProducto);
        using MySqlDataReader lector = comando.ExecuteReader();
        if (!lector.Read()) throw new InvalidOperationException("El producto seleccionado no existe.");
        return MapearProducto(lector);
    }

    public long GuardarProducto(ProductoInventarioModel producto)
    {
        ExigirPermiso();
        ValidarProducto(producto);
        return DbTransactionHelper.Ejecutar((conexion, transaccion) =>
        {
            string codigo = producto.Codigo.Trim();
            if (producto.IdProducto == 0)
            {
                string codigoTemporal = string.IsNullOrWhiteSpace(codigo) ? $"TMP-{Guid.NewGuid():N}" : codigo;
                using MySqlCommand insertar = new(@"
INSERT INTO inventario_productos
(codigo, nombre, categoria, presentacion, unidad_medida, precio_compra, precio_venta, stock_minimo, controla_lotes, activo)
VALUES (@codigo, @nombre, @categoria, @presentacion, @unidad, @compra, @venta, @minimo, @lotes, @activo);", conexion, transaccion);
                AgregarParametrosProducto(insertar, producto, codigoTemporal);
                try { insertar.ExecuteNonQuery(); }
                catch (MySqlException ex) when (ex.Number == 1062) { throw new InvalidOperationException("Ya existe un producto con ese código.", ex); }
                long idProducto = insertar.LastInsertedId;
                if (string.IsNullOrWhiteSpace(codigo))
                {
                    codigo = $"INV-{idProducto:000000}";
                    using MySqlCommand asignarCodigo = new("UPDATE inventario_productos SET codigo = @codigo WHERE id_producto = @id;", conexion, transaccion);
                    asignarCodigo.Parameters.AddWithValue("@codigo", codigo);
                    asignarCodigo.Parameters.AddWithValue("@id", idProducto);
                    asignarCodigo.ExecuteNonQuery();
                }
                return idProducto;
            }

            using MySqlCommand actualizar = new(@"
UPDATE inventario_productos SET codigo = @codigo, nombre = @nombre, categoria = @categoria,
 presentacion = @presentacion, unidad_medida = @unidad, precio_compra = @compra,
 precio_venta = @venta, stock_minimo = @minimo, controla_lotes = @lotes, activo = @activo
WHERE id_producto = @id;", conexion, transaccion);
            AgregarParametrosProducto(actualizar, producto, codigo);
            actualizar.Parameters.AddWithValue("@id", producto.IdProducto);
            try
            {
                if (actualizar.ExecuteNonQuery() == 0) throw new InvalidOperationException("El producto seleccionado ya no existe.");
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                throw new InvalidOperationException("Ya existe otro producto con ese código.", ex);
            }
            return producto.IdProducto;
        });
    }

    public List<LoteInventarioModel> ListarLotes(long idProducto, bool incluirAgotados = true)
    {
        ExigirPermiso();
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(@"
SELECT l.id_lote, l.id_producto, p.codigo, p.nombre, l.numero_lote, l.fecha_vencimiento,
       l.cantidad_inicial, l.cantidad_disponible, l.costo_unitario, l.fecha_ingreso,
       COALESCE(l.proveedor, '') AS proveedor, l.estado
FROM inventario_lotes l INNER JOIN inventario_productos p ON p.id_producto = l.id_producto
WHERE l.id_producto = @producto AND (@todos = 1 OR l.cantidad_disponible > 0)
ORDER BY l.fecha_vencimiento IS NULL, l.fecha_vencimiento, l.fecha_ingreso DESC;", conexion);
        comando.Parameters.AddWithValue("@producto", idProducto);
        comando.Parameters.AddWithValue("@todos", incluirAgotados);
        using MySqlDataReader lector = comando.ExecuteReader();
        List<LoteInventarioModel> resultado = new();
        while (lector.Read()) resultado.Add(MapearLote(lector));
        return resultado;
    }

    public void RegistrarEntrada(EntradaInventarioRequestModel solicitud)
    {
        ExigirPermiso();
        if (solicitud.IdProducto <= 0) throw new InvalidOperationException("Seleccione un producto.");
        if (solicitud.Cantidad <= 0) throw new InvalidOperationException("La cantidad de entrada debe ser mayor que cero.");
        if (solicitud.CostoUnitario < 0) throw new InvalidOperationException("El costo unitario no puede ser negativo.");
        if (string.IsNullOrWhiteSpace(solicitud.NumeroLote)) throw new InvalidOperationException("El número de lote es obligatorio.");
        if (solicitud.FechaVencimiento.HasValue && solicitud.FechaVencimiento.Value.Date < DateTime.Today)
            throw new InvalidOperationException("No puede ingresar un lote que ya está vencido.");

        DbTransactionHelper.Ejecutar((conexion, transaccion) =>
        {
            ValidarProductoActivo(conexion, transaccion, solicitud.IdProducto);
            long idLote;
            using (MySqlCommand buscar = new(@"
SELECT id_lote, fecha_vencimiento FROM inventario_lotes
WHERE id_producto = @producto AND numero_lote = @lote FOR UPDATE;", conexion, transaccion))
            {
                buscar.Parameters.AddWithValue("@producto", solicitud.IdProducto);
                buscar.Parameters.AddWithValue("@lote", solicitud.NumeroLote.Trim());
                using MySqlDataReader lector = buscar.ExecuteReader();
                if (lector.Read())
                {
                    idLote = lector.GetInt64("id_lote");
                    DateTime? vencimientoExistente = lector.IsDBNull(lector.GetOrdinal("fecha_vencimiento")) ? null : lector.GetDateTime("fecha_vencimiento");
                    if (vencimientoExistente.HasValue && solicitud.FechaVencimiento.HasValue && vencimientoExistente.Value.Date != solicitud.FechaVencimiento.Value.Date)
                        throw new InvalidOperationException("El lote ya existe con otra fecha de vencimiento.");
                }
                else idLote = 0;
            }

            if (idLote == 0)
            {
                using MySqlCommand insertar = new(@"
INSERT INTO inventario_lotes
(id_producto, numero_lote, fecha_vencimiento, cantidad_inicial, cantidad_disponible, costo_unitario, proveedor, estado)
VALUES (@producto, @lote, @vencimiento, @cantidad, @cantidad, @costo, @proveedor, 'Disponible');", conexion, transaccion);
                insertar.Parameters.AddWithValue("@producto", solicitud.IdProducto);
                insertar.Parameters.AddWithValue("@lote", solicitud.NumeroLote.Trim());
                insertar.Parameters.AddWithValue("@vencimiento", (object?)solicitud.FechaVencimiento?.Date ?? DBNull.Value);
                insertar.Parameters.AddWithValue("@cantidad", solicitud.Cantidad);
                insertar.Parameters.AddWithValue("@costo", solicitud.CostoUnitario);
                insertar.Parameters.AddWithValue("@proveedor", ValorNulo(solicitud.Proveedor));
                insertar.ExecuteNonQuery();
                idLote = insertar.LastInsertedId;
            }
            else
            {
                using MySqlCommand actualizar = new(@"
UPDATE inventario_lotes SET cantidad_inicial = cantidad_inicial + @cantidad,
 cantidad_disponible = cantidad_disponible + @cantidad,
 costo_unitario = @costo, proveedor = COALESCE(@proveedor, proveedor),
 fecha_vencimiento = COALESCE(fecha_vencimiento, @vencimiento), estado = 'Disponible'
WHERE id_lote = @lote;", conexion, transaccion);
                actualizar.Parameters.AddWithValue("@cantidad", solicitud.Cantidad);
                actualizar.Parameters.AddWithValue("@costo", solicitud.CostoUnitario);
                actualizar.Parameters.AddWithValue("@proveedor", ValorNulo(solicitud.Proveedor));
                actualizar.Parameters.AddWithValue("@vencimiento", (object?)solicitud.FechaVencimiento?.Date ?? DBNull.Value);
                actualizar.Parameters.AddWithValue("@lote", idLote);
                actualizar.ExecuteNonQuery();
            }
            InsertarMovimiento(conexion, transaccion, solicitud.IdProducto, idLote, "Entrada", solicitud.Cantidad, solicitud.Observaciones);
        });
    }

    public void RegistrarMovimientoManual(MovimientoManualRequestModel solicitud)
    {
        ExigirPermiso();
        if (solicitud.IdProducto <= 0 || solicitud.IdLote <= 0) throw new InvalidOperationException("Seleccione producto y lote.");
        if (solicitud.Cantidad <= 0) throw new InvalidOperationException("La cantidad debe ser mayor que cero.");
        if (!TiposSalidaManual.Contains(solicitud.TipoMovimiento) && !string.Equals(solicitud.TipoMovimiento, "Ajuste positivo", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Tipo de movimiento manual no válido.");
        if (string.IsNullOrWhiteSpace(solicitud.Observaciones)) throw new InvalidOperationException("La observación del movimiento es obligatoria.");

        DbTransactionHelper.Ejecutar((conexion, transaccion) =>
        {
            LoteInventarioModel lote = ObtenerLoteParaActualizar(conexion, transaccion, solicitud.IdLote, solicitud.IdProducto);
            bool positivo = string.Equals(solicitud.TipoMovimiento, "Ajuste positivo", StringComparison.OrdinalIgnoreCase);
            if (!positivo)
            {
                if (solicitud.TipoMovimiento != "Vencimiento" && lote.Vencido)
                    throw new InvalidOperationException("No puede retirar existencias de un lote vencido; procese su vencimiento.");
                if (lote.CantidadDisponible < solicitud.Cantidad)
                    throw new InvalidOperationException("El lote no cuenta con existencias suficientes.");
                using MySqlCommand restar = new(@"
UPDATE inventario_lotes SET cantidad_disponible = cantidad_disponible - @cantidad,
 estado = CASE WHEN @tipo = 'Vencimiento' THEN 'Vencido'
               WHEN cantidad_disponible - @cantidad <= 0 THEN 'Agotado' ELSE estado END
WHERE id_lote = @lote;", conexion, transaccion);
                restar.Parameters.AddWithValue("@cantidad", solicitud.Cantidad);
                restar.Parameters.AddWithValue("@tipo", solicitud.TipoMovimiento);
                restar.Parameters.AddWithValue("@lote", solicitud.IdLote);
                restar.ExecuteNonQuery();
            }
            else
            {
                using MySqlCommand sumar = new(@"
UPDATE inventario_lotes SET cantidad_inicial = cantidad_inicial + @cantidad,
 cantidad_disponible = cantidad_disponible + @cantidad,
 estado = CASE WHEN fecha_vencimiento IS NOT NULL AND fecha_vencimiento < CURDATE() THEN 'Vencido' ELSE 'Disponible' END
WHERE id_lote = @lote;", conexion, transaccion);
                sumar.Parameters.AddWithValue("@cantidad", solicitud.Cantidad);
                sumar.Parameters.AddWithValue("@lote", solicitud.IdLote);
                sumar.ExecuteNonQuery();
            }
            InsertarMovimiento(conexion, transaccion, solicitud.IdProducto, solicitud.IdLote, solicitud.TipoMovimiento, solicitud.Cantidad, solicitud.Observaciones);
        });
    }

    public int ProcesarLotesVencidos()
    {
        ExigirPermiso();
        return DbTransactionHelper.Ejecutar((conexion, transaccion) =>
        {
            List<LoteInventarioModel> lotes = new();
            using (MySqlCommand comando = new(@"
SELECT l.id_lote, l.id_producto, p.codigo, p.nombre, l.numero_lote, l.fecha_vencimiento,
 l.cantidad_inicial, l.cantidad_disponible, l.costo_unitario, l.fecha_ingreso,
 COALESCE(l.proveedor, '') proveedor, l.estado
FROM inventario_lotes l INNER JOIN inventario_productos p ON p.id_producto = l.id_producto
WHERE l.fecha_vencimiento < CURDATE() AND l.estado <> 'Vencido' FOR UPDATE;", conexion, transaccion))
            using (MySqlDataReader lector = comando.ExecuteReader())
            {
                while (lector.Read()) lotes.Add(MapearLote(lector));
            }
            foreach (LoteInventarioModel lote in lotes)
            {
                if (lote.CantidadDisponible > 0)
                    InsertarMovimiento(conexion, transaccion, lote.IdProducto, lote.IdLote, "Vencimiento", lote.CantidadDisponible, "Baja automática de lote vencido.");
                using MySqlCommand actualizar = new("UPDATE inventario_lotes SET cantidad_disponible = 0, estado = 'Vencido' WHERE id_lote = @id;", conexion, transaccion);
                actualizar.Parameters.AddWithValue("@id", lote.IdLote);
                actualizar.ExecuteNonQuery();
            }
            return lotes.Count;
        });
    }

    public List<MovimientoInventarioModel> ListarMovimientos(DateTime inicio, DateTime fin, long? idProducto = null, string tipo = "Todos")
    {
        ExigirPermiso();
        if (fin.Date < inicio.Date) throw new ArgumentException("La fecha final no puede ser anterior a la fecha inicial.");
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(@"
SELECT m.id_movimiento, m.fecha_registro, m.id_producto, p.codigo, p.nombre,
       m.id_lote, COALESCE(l.numero_lote, '') lote, m.tipo_movimiento, m.cantidad,
       u.nombre_completo usuario, COALESCE(m.observaciones, '') observaciones
FROM inventario_movimientos m
INNER JOIN inventario_productos p ON p.id_producto = m.id_producto
LEFT JOIN inventario_lotes l ON l.id_lote = m.id_lote
INNER JOIN usuarios u ON u.id_usuario = m.id_usuario_registro
WHERE m.fecha_registro >= @inicio AND m.fecha_registro < DATE_ADD(@fin, INTERVAL 1 DAY)
  AND (@producto IS NULL OR m.id_producto = @producto)
  AND (@tipo = 'Todos' OR m.tipo_movimiento = @tipo)
ORDER BY m.fecha_registro DESC, m.id_movimiento DESC;", conexion);
        comando.Parameters.AddWithValue("@inicio", inicio.Date);
        comando.Parameters.AddWithValue("@fin", fin.Date);
        comando.Parameters.AddWithValue("@producto", (object?)idProducto ?? DBNull.Value);
        comando.Parameters.AddWithValue("@tipo", tipo);
        using MySqlDataReader lector = comando.ExecuteReader();
        List<MovimientoInventarioModel> resultado = new();
        while (lector.Read())
        {
            resultado.Add(new MovimientoInventarioModel
            {
                IdMovimiento = lector.GetInt64("id_movimiento"), FechaRegistro = lector.GetDateTime("fecha_registro"),
                IdProducto = lector.GetInt64("id_producto"), CodigoProducto = lector.GetString("codigo"), Producto = lector.GetString("nombre"),
                IdLote = lector.IsDBNull(lector.GetOrdinal("id_lote")) ? null : lector.GetInt64("id_lote"), Lote = lector.GetString("lote"),
                TipoMovimiento = lector.GetString("tipo_movimiento"), Cantidad = lector.GetDecimal("cantidad"),
                Usuario = lector.GetString("usuario"), Observaciones = lector.GetString("observaciones")
            });
        }
        return resultado;
    }

    public List<ProductoInventarioModel> ListarStockBajo()
    {
        ExigirPermiso();
        return ListarProductos().FindAll(p => p.StockBajo);
    }

    public List<LoteInventarioModel> ListarVencimientos(int dias = 30, bool incluirVencidos = true)
    {
        ExigirPermiso();
        if (dias < 0 || dias > 3650) throw new ArgumentOutOfRangeException(nameof(dias));
        using MySqlConnection conexion = Database.CrearConexion();
        conexion.Open();
        using MySqlCommand comando = new(@"
SELECT l.id_lote, l.id_producto, p.codigo, p.nombre, l.numero_lote, l.fecha_vencimiento,
       l.cantidad_inicial, l.cantidad_disponible, l.costo_unitario, l.fecha_ingreso,
       COALESCE(l.proveedor, '') proveedor, l.estado
FROM inventario_lotes l INNER JOIN inventario_productos p ON p.id_producto = l.id_producto
WHERE p.activo = 1 AND l.cantidad_disponible > 0 AND l.fecha_vencimiento IS NOT NULL
  AND l.fecha_vencimiento <= DATE_ADD(CURDATE(), INTERVAL @dias DAY)
  AND (@vencidos = 1 OR l.fecha_vencimiento >= CURDATE())
ORDER BY l.fecha_vencimiento, p.nombre;", conexion);
        comando.Parameters.AddWithValue("@dias", dias);
        comando.Parameters.AddWithValue("@vencidos", incluirVencidos);
        using MySqlDataReader lector = comando.ExecuteReader();
        List<LoteInventarioModel> resultado = new();
        while (lector.Read()) resultado.Add(MapearLote(lector));
        return resultado;
    }

    private static ProductoInventarioModel MapearProducto(MySqlDataReader lector) => new()
    {
        IdProducto = lector.GetInt64("id_producto"), Codigo = lector.GetString("codigo"), Nombre = lector.GetString("nombre"),
        Categoria = lector.GetString("categoria"), Presentacion = lector.GetString("presentacion"), UnidadMedida = lector.GetString("unidad_medida"),
        PrecioCompra = lector.GetDecimal("precio_compra"), PrecioVenta = lector.GetDecimal("precio_venta"), StockMinimo = lector.GetDecimal("stock_minimo"),
        ControlaLotes = lector.GetBoolean("controla_lotes"), Activo = lector.GetBoolean("activo"), StockDisponible = lector.GetDecimal("stock_disponible"),
        ProximoVencimiento = lector.IsDBNull(lector.GetOrdinal("proximo_vencimiento")) ? null : lector.GetDateTime("proximo_vencimiento")
    };

    private static LoteInventarioModel MapearLote(MySqlDataReader lector) => new()
    {
        IdLote = lector.GetInt64("id_lote"), IdProducto = lector.GetInt64("id_producto"), CodigoProducto = lector.GetString("codigo"),
        Producto = lector.GetString("nombre"), NumeroLote = lector.GetString("numero_lote"),
        FechaVencimiento = lector.IsDBNull(lector.GetOrdinal("fecha_vencimiento")) ? null : lector.GetDateTime("fecha_vencimiento"),
        CantidadInicial = lector.GetDecimal("cantidad_inicial"), CantidadDisponible = lector.GetDecimal("cantidad_disponible"),
        CostoUnitario = lector.GetDecimal("costo_unitario"), FechaIngreso = lector.GetDateTime("fecha_ingreso"),
        Proveedor = lector.GetString("proveedor"), Estado = lector.GetString("estado")
    };

    private static LoteInventarioModel ObtenerLoteParaActualizar(MySqlConnection conexion, MySqlTransaction transaccion, long idLote, long idProducto)
    {
        using MySqlCommand comando = new(@"
SELECT l.id_lote, l.id_producto, p.codigo, p.nombre, l.numero_lote, l.fecha_vencimiento,
 l.cantidad_inicial, l.cantidad_disponible, l.costo_unitario, l.fecha_ingreso,
 COALESCE(l.proveedor, '') proveedor, l.estado
FROM inventario_lotes l INNER JOIN inventario_productos p ON p.id_producto = l.id_producto
WHERE l.id_lote = @lote AND l.id_producto = @producto FOR UPDATE;", conexion, transaccion);
        comando.Parameters.AddWithValue("@lote", idLote);
        comando.Parameters.AddWithValue("@producto", idProducto);
        using MySqlDataReader lector = comando.ExecuteReader();
        if (!lector.Read()) throw new InvalidOperationException("El lote seleccionado no existe para el producto.");
        return MapearLote(lector);
    }

    private static void ValidarProductoActivo(MySqlConnection conexion, MySqlTransaction transaccion, long idProducto)
    {
        using MySqlCommand comando = new("SELECT COUNT(*) FROM inventario_productos WHERE id_producto = @id AND activo = 1;", conexion, transaccion);
        comando.Parameters.AddWithValue("@id", idProducto);
        if (Convert.ToInt32(comando.ExecuteScalar()) == 0) throw new InvalidOperationException("El producto no existe o se encuentra inactivo.");
    }

    private static void ValidarProducto(ProductoInventarioModel producto)
    {
        if (string.IsNullOrWhiteSpace(producto.Nombre)) throw new InvalidOperationException("El nombre del producto es obligatorio.");
        if (!Categorias.Contains(producto.Categoria)) throw new InvalidOperationException("La categoría seleccionada no es válida.");
        if (string.IsNullOrWhiteSpace(producto.UnidadMedida)) throw new InvalidOperationException("La unidad de medida es obligatoria.");
        if (producto.PrecioCompra < 0 || producto.PrecioVenta < 0 || producto.StockMinimo < 0)
            throw new InvalidOperationException("Precios y stock mínimo no pueden ser negativos.");
    }

    private static void AgregarParametrosProducto(MySqlCommand comando, ProductoInventarioModel producto, string codigo)
    {
        comando.Parameters.AddWithValue("@codigo", codigo.Trim());
        comando.Parameters.AddWithValue("@nombre", producto.Nombre.Trim());
        comando.Parameters.AddWithValue("@categoria", producto.Categoria);
        comando.Parameters.AddWithValue("@presentacion", ValorNulo(producto.Presentacion));
        comando.Parameters.AddWithValue("@unidad", producto.UnidadMedida.Trim());
        comando.Parameters.AddWithValue("@compra", producto.PrecioCompra);
        comando.Parameters.AddWithValue("@venta", producto.PrecioVenta);
        comando.Parameters.AddWithValue("@minimo", producto.StockMinimo);
        comando.Parameters.AddWithValue("@lotes", producto.ControlaLotes);
        comando.Parameters.AddWithValue("@activo", producto.Activo);
    }

    private static void InsertarMovimiento(MySqlConnection conexion, MySqlTransaction transaccion, long idProducto, long? idLote, string tipo, decimal cantidad, string observaciones)
    {
        using MySqlCommand comando = new(@"
INSERT INTO inventario_movimientos(id_producto, id_lote, tipo_movimiento, cantidad, id_usuario_registro, observaciones)
VALUES (@producto, @lote, @tipo, @cantidad, @usuario, @observaciones);", conexion, transaccion);
        comando.Parameters.AddWithValue("@producto", idProducto);
        comando.Parameters.AddWithValue("@lote", (object?)idLote ?? DBNull.Value);
        comando.Parameters.AddWithValue("@tipo", tipo);
        comando.Parameters.AddWithValue("@cantidad", cantidad);
        comando.Parameters.AddWithValue("@usuario", IdUsuarioActual());
        comando.Parameters.AddWithValue("@observaciones", ValorNulo(observaciones));
        comando.ExecuteNonQuery();
    }

    private static int IdUsuarioActual() => SesionActual.Usuario?.IdUsuario
        ?? throw new UnauthorizedAccessException("No existe una sesión activa.");

    private static object ValorNulo(string? valor) => string.IsNullOrWhiteSpace(valor) ? DBNull.Value : valor.Trim();

    private static void ExigirPermiso() => SesionActual.ExigirRoles("Administrador");
}
