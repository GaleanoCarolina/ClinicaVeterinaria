using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Services;
using ClinicaVeterinaria.Utils;

namespace ClinicaVeterinaria.Forms;

public sealed class FormInventario : Form
{
    private readonly InventarioService _servicio = new();
    private ProductoInventarioModel? _productoSeleccionado;
    private readonly BindingSource _productosSource = new();
    private readonly BindingSource _lotesSource = new();
    private readonly BindingSource _movimientosSource = new();
    private readonly BindingSource _stockBajoSource = new();
    private readonly BindingSource _vencimientosSource = new();

    private TextBox _buscarProductos = null!;
    private CheckBox _verInactivos = null!;
    private DataGridView _gridProductos = null!;
    private DataGridView _gridLotes = null!;
    private DataGridView _gridMovimientos = null!;
    private DataGridView _gridStockBajo = null!;
    private DataGridView _gridVencimientos = null!;
    private Label _lblProductoLotes = null!;
    private Label _lblProductosActivos = null!;
    private Label _lblStockBajo = null!;
    private Label _lblPorVencer = null!;
    private Label _lblVencidos = null!;
    private Label _lblValor = null!;
    private DateTimePicker _movInicio = null!;
    private DateTimePicker _movFin = null!;
    private ComboBox _movProducto = null!;
    private ComboBox _movTipo = null!;
    private NumericUpDown _diasVencimiento = null!;
    private TabControl _tabs = null!;

    public FormInventario()
    {
        UiTheme.PrepararFormulario(this);
        ConstruirInterfaz();
        Shown += (_, _) => CargarTodo();
    }

    private void ConstruirInterfaz()
    {
        Dock = DockStyle.Fill;
        TableLayoutPanel raiz = new()
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            BackColor = UiTheme.Fondo,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(raiz);

        Panel cabecera = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20, 10, 18, 8) };
        Label titulo = UiTheme.CrearTitulo("Inventario");
        titulo.Location = new Point(18, 12);
        cabecera.Controls.Add(titulo);
        Label descripcion = new()
        {
            Text = "Productos, lotes, vencimientos, entradas, salidas y trazabilidad de movimientos.",
            AutoSize = true,
            ForeColor = UiTheme.TextoSecundario,
            Location = new Point(220, 20)
        };
        cabecera.Controls.Add(descripcion);
        Button actualizar = UiTheme.CrearBoton("Actualizar", true);
        actualizar.Width = 108;
        actualizar.Dock = DockStyle.Right;
        actualizar.Click += (_, _) => CargarTodo();
        cabecera.Controls.Add(actualizar);
        raiz.Controls.Add(cabecera, 0, 0);

        TableLayoutPanel tarjetas = new() { Dock = DockStyle.Fill, ColumnCount = 5, Padding = new Padding(0, 8, 0, 8) };
        for (int i = 0; i < 5; i++) tarjetas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        _lblProductosActivos = CrearTarjeta(tarjetas, "Productos activos", 0);
        _lblStockBajo = CrearTarjeta(tarjetas, "Stock bajo", 1);
        _lblPorVencer = CrearTarjeta(tarjetas, "Vencen en 30 días", 2);
        _lblVencidos = CrearTarjeta(tarjetas, "Vencidos con stock", 3);
        _lblValor = CrearTarjeta(tarjetas, "Valor del stock", 4);
        raiz.Controls.Add(tarjetas, 0, 1);

        _tabs = new TabControl { Dock = DockStyle.Fill, Font = UiTheme.FuenteNormal };
        TabPage productos = new("Productos");
        TabPage lotes = new("Lotes y operaciones");
        TabPage movimientos = new("Movimientos");
        TabPage alertas = new("Alertas de stock y vencimiento");
        _tabs.TabPages.AddRange(new[] { productos, lotes, movimientos, alertas });
        raiz.Controls.Add(_tabs, 0, 2);

        ConstruirProductos(productos);
        ConstruirLotes(lotes);
        ConstruirMovimientos(movimientos);
        ConstruirAlertas(alertas);
    }

    private static Label CrearTarjeta(TableLayoutPanel contenedor, string texto, int columna)
    {
        Panel panel = new() { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(5), Padding = new Padding(14, 8, 14, 9) };
        panel.Controls.Add(new Label { Text = texto, Dock = DockStyle.Top, Height = 24, ForeColor = UiTheme.TextoSecundario });
        Label valor = new()
        {
            Text = "0",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            ForeColor = UiTheme.Primario
        };
        panel.Controls.Add(valor);
        valor.BringToFront();
        contenedor.Controls.Add(panel, columna, 0);
        return valor;
    }

    private void ConstruirProductos(TabPage tab)
    {
        TableLayoutPanel layout = new() { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tab.Controls.Add(layout);

        FlowLayoutPanel comandos = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        comandos.Controls.Add(new Label { Text = "Buscar", AutoSize = true, Margin = new Padding(0, 12, 8, 0) });
        _buscarProductos = new TextBox { Width = 260, Margin = new Padding(0, 8, 10, 0), PlaceholderText = "Código, producto o categoría" };
        comandos.Controls.Add(_buscarProductos);
        Button buscar = UiTheme.CrearBoton("Buscar", true); buscar.Width = 90; buscar.Click += (_, _) => CargarProductos(); comandos.Controls.Add(buscar);
        Button limpiar = UiTheme.CrearBoton("Limpiar"); limpiar.Width = 90; limpiar.Click += (_, _) => { _buscarProductos.Clear(); _verInactivos.Checked = false; CargarProductos(); }; comandos.Controls.Add(limpiar);
        _verInactivos = new CheckBox { Text = "Mostrar inactivos", AutoSize = true, Margin = new Padding(16, 14, 12, 0) }; _verInactivos.CheckedChanged += (_, _) => CargarProductos(); comandos.Controls.Add(_verInactivos);
        Button nuevo = UiTheme.CrearBoton("Nuevo producto", true); nuevo.Width = 134; nuevo.Margin = new Padding(24, 4, 4, 4); nuevo.Click += (_, _) => NuevoProducto(); comandos.Controls.Add(nuevo);
        Button editar = UiTheme.CrearBoton("Editar"); editar.Width = 90; editar.Click += (_, _) => EditarProducto(); comandos.Controls.Add(editar);
        layout.Controls.Add(comandos, 0, 0);

        _gridProductos = new DataGridView { Dock = DockStyle.Fill, DataSource = _productosSource };
        UiTheme.PrepararGrid(_gridProductos);
        _gridProductos.AutoGenerateColumns = false;
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Codigo", HeaderText = "Código", FillWeight = 72 });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Nombre", HeaderText = "Producto", FillWeight = 160 });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Categoria", HeaderText = "Categoría", FillWeight = 92 });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Presentacion", HeaderText = "Presentación", FillWeight = 105 });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StockDisponible", HeaderText = "Stock", FillWeight = 60, DefaultCellStyle = new DataGridViewCellStyle { Format = "0.###" } });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StockMinimo", HeaderText = "Mínimo", FillWeight = 60, DefaultCellStyle = new DataGridViewCellStyle { Format = "0.###" } });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EstadoStock", HeaderText = "Nivel", FillWeight = 76 });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PrecioVenta", HeaderText = "Venta (Q, IVA incl.)", FillWeight = 70, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
        _gridProductos.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "Activo", HeaderText = "Activo", FillWeight = 45 });
        _gridProductos.SelectionChanged += (_, _) => SeleccionarProducto();
        _gridProductos.DoubleClick += (_, _) => EditarProducto();
        layout.Controls.Add(_gridProductos, 0, 1);
    }

    private void ConstruirLotes(TabPage tab)
    {
        TableLayoutPanel layout = new() { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tab.Controls.Add(layout);

        _lblProductoLotes = new Label
        {
            Text = "Seleccione un producto en la pestaña Productos.",
            Dock = DockStyle.Fill,
            Font = UiTheme.FuenteSubtitulo,
            ForeColor = UiTheme.Primario,
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(_lblProductoLotes, 0, 0);

        FlowLayoutPanel comandos = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        Button entrada = UiTheme.CrearBoton("Registrar entrada", true); entrada.Width = 145; entrada.Click += (_, _) => RegistrarEntrada(); comandos.Controls.Add(entrada);
        Button movimiento = UiTheme.CrearBoton("Salida / ajuste"); movimiento.Width = 132; movimiento.Click += (_, _) => RegistrarMovimiento(); comandos.Controls.Add(movimiento);
        Button vencidos = UiTheme.CrearBoton("Procesar vencidos"); vencidos.Width = 145; vencidos.Click += (_, _) => ProcesarVencidos(); comandos.Controls.Add(vencidos);
        Button refrescar = UiTheme.CrearBoton("Actualizar lotes"); refrescar.Width = 130; refrescar.Click += (_, _) => CargarLotes(); comandos.Controls.Add(refrescar);
        layout.Controls.Add(comandos, 0, 1);

        _gridLotes = new DataGridView { Dock = DockStyle.Fill, DataSource = _lotesSource };
        UiTheme.PrepararGrid(_gridLotes);
        _gridLotes.AutoGenerateColumns = false;
        _gridLotes.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NumeroLote", HeaderText = "Lote", FillWeight = 102 });
        _gridLotes.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FechaVencimiento", HeaderText = "Vencimiento", FillWeight = 85, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } });
        _gridLotes.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CantidadInicial", HeaderText = "Inicial", FillWeight = 60, DefaultCellStyle = new DataGridViewCellStyle { Format = "0.###" } });
        _gridLotes.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CantidadDisponible", HeaderText = "Disponible", FillWeight = 65, DefaultCellStyle = new DataGridViewCellStyle { Format = "0.###" } });
        _gridLotes.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CostoUnitario", HeaderText = "Costo (Q)", FillWeight = 65, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
        _gridLotes.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Proveedor", HeaderText = "Proveedor", FillWeight = 125 });
        _gridLotes.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Estado", HeaderText = "Estado", FillWeight = 72 });
        layout.Controls.Add(_gridLotes, 0, 2);
    }

    private void ConstruirMovimientos(TabPage tab)
    {
        TableLayoutPanel layout = new() { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tab.Controls.Add(layout);
        FlowLayoutPanel filtros = new() { Dock = DockStyle.Fill, WrapContents = false };
        filtros.Controls.Add(new Label { Text = "Desde", AutoSize = true, Margin = new Padding(0, 14, 5, 0) });
        _movInicio = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30), Margin = new Padding(0, 9, 10, 0) }; filtros.Controls.Add(_movInicio);
        filtros.Controls.Add(new Label { Text = "Hasta", AutoSize = true, Margin = new Padding(0, 14, 5, 0) });
        _movFin = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Today, Margin = new Padding(0, 9, 10, 0) }; filtros.Controls.Add(_movFin);
        _movProducto = new ComboBox { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 9, 10, 0) }; filtros.Controls.Add(_movProducto);
        _movTipo = new ComboBox { Width = 185, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 9, 10, 0) };
        _movTipo.Items.AddRange(new object[] { "Todos", "Entrada", "Salida por consulta", "Salida por venta", "Salida por vacuna aplicada", "Ajuste positivo", "Ajuste negativo", "Merma", "Vencimiento", "Movimiento compensatorio por anulación" }); _movTipo.SelectedIndex = 0; filtros.Controls.Add(_movTipo);
        Button filtrar = UiTheme.CrearBoton("Consultar", true); filtrar.Width = 100; filtrar.Click += (_, _) => CargarMovimientos(); filtros.Controls.Add(filtrar);
        layout.Controls.Add(filtros, 0, 0);

        _gridMovimientos = new DataGridView { Dock = DockStyle.Fill, DataSource = _movimientosSource };
        UiTheme.PrepararGrid(_gridMovimientos);
        _gridMovimientos.AutoGenerateColumns = false;
        _gridMovimientos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FechaRegistro", HeaderText = "Fecha", FillWeight = 92, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" } });
        _gridMovimientos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CodigoProducto", HeaderText = "Código", FillWeight = 70 });
        _gridMovimientos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Producto", HeaderText = "Producto", FillWeight = 135 });
        _gridMovimientos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Lote", HeaderText = "Lote", FillWeight = 100 });
        _gridMovimientos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TipoMovimiento", HeaderText = "Movimiento", FillWeight = 122 });
        _gridMovimientos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Cantidad", HeaderText = "Cantidad", FillWeight = 62, DefaultCellStyle = new DataGridViewCellStyle { Format = "0.###" } });
        _gridMovimientos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Usuario", HeaderText = "Usuario", FillWeight = 105 });
        _gridMovimientos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Observaciones", HeaderText = "Observaciones", FillWeight = 180 });
        layout.Controls.Add(_gridMovimientos, 0, 1);
    }

    private void ConstruirAlertas(TabPage tab)
    {
        TableLayoutPanel layout = new() { Dock = DockStyle.Fill, RowCount = 4, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        tab.Controls.Add(layout);
        layout.Controls.Add(new Label { Text = "Productos con stock bajo", Dock = DockStyle.Fill, Font = UiTheme.FuenteSubtitulo, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        _gridStockBajo = new DataGridView { Dock = DockStyle.Fill, DataSource = _stockBajoSource };
        UiTheme.PrepararGrid(_gridStockBajo); _gridStockBajo.AutoGenerateColumns = false;
        _gridStockBajo.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Codigo", HeaderText = "Código", FillWeight = 75 });
        _gridStockBajo.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Nombre", HeaderText = "Producto", FillWeight = 170 });
        _gridStockBajo.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Categoria", HeaderText = "Categoría", FillWeight = 90 });
        _gridStockBajo.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StockDisponible", HeaderText = "Disponible", FillWeight = 70, DefaultCellStyle = new DataGridViewCellStyle { Format = "0.###" } });
        _gridStockBajo.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StockMinimo", HeaderText = "Mínimo", FillWeight = 70, DefaultCellStyle = new DataGridViewCellStyle { Format = "0.###" } });
        layout.Controls.Add(_gridStockBajo, 0, 1);

        FlowLayoutPanel vencimientoCabecera = new() { Dock = DockStyle.Fill, WrapContents = false };
        vencimientoCabecera.Controls.Add(new Label { Text = "Lotes próximos a vencer / vencidos   Mostrar próximos", AutoSize = true, Font = UiTheme.FuenteSubtitulo, Margin = new Padding(0, 15, 8, 0) });
        _diasVencimiento = new NumericUpDown { Minimum = 1, Maximum = 365, Value = 30, Width = 58, Margin = new Padding(0, 11, 4, 0) }; vencimientoCabecera.Controls.Add(_diasVencimiento);
        vencimientoCabecera.Controls.Add(new Label { Text = "días", AutoSize = true, Margin = new Padding(0, 15, 8, 0) });
        Button consultar = UiTheme.CrearBoton("Actualizar alertas", true); consultar.Width = 135; consultar.Click += (_, _) => CargarAlertas(); vencimientoCabecera.Controls.Add(consultar);
        layout.Controls.Add(vencimientoCabecera, 0, 2);
        _gridVencimientos = new DataGridView { Dock = DockStyle.Fill, DataSource = _vencimientosSource };
        UiTheme.PrepararGrid(_gridVencimientos); _gridVencimientos.AutoGenerateColumns = false;
        _gridVencimientos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Producto", HeaderText = "Producto", FillWeight = 160 });
        _gridVencimientos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NumeroLote", HeaderText = "Lote", FillWeight = 100 });
        _gridVencimientos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FechaVencimiento", HeaderText = "Vencimiento", FillWeight = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } });
        _gridVencimientos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CantidadDisponible", HeaderText = "Stock", FillWeight = 65, DefaultCellStyle = new DataGridViewCellStyle { Format = "0.###" } });
        _gridVencimientos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Estado", HeaderText = "Estado", FillWeight = 75 });
        layout.Controls.Add(_gridVencimientos, 0, 3);
    }

    private void CargarTodo()
    {
        try
        {
            CargarResumen();
            CargarProductos();
            CargarMovimientos();
            CargarAlertas();
        }
        catch (Exception ex) { MostrarError("No fue posible cargar inventario.", ex); }
    }

    private void CargarResumen()
    {
        ResumenInventarioModel resumen = _servicio.ObtenerResumen();
        _lblProductosActivos.Text = resumen.ProductosActivos.ToString();
        _lblStockBajo.Text = resumen.ProductosStockBajo.ToString();
        _lblPorVencer.Text = resumen.LotesPorVencer.ToString();
        _lblVencidos.Text = resumen.LotesVencidosConStock.ToString();
        _lblValor.Text = resumen.ValorStock.ToString("C2");
        _lblStockBajo.ForeColor = resumen.ProductosStockBajo > 0 ? UiTheme.Peligro : UiTheme.Primario;
        _lblVencidos.ForeColor = resumen.LotesVencidosConStock > 0 ? UiTheme.Peligro : UiTheme.Primario;
    }

    private void CargarProductos()
    {
        long? seleccionado = _productoSeleccionado?.IdProducto;
        List<ProductoInventarioModel> productos = _servicio.ListarProductos(_buscarProductos.Text, _verInactivos.Checked);
        _productosSource.DataSource = productos;
        CargarComboMovimientos(productos);
        if (productos.Count == 0)
        {
            _productoSeleccionado = null;
            CargarLotes();
            return;
        }
        int posicion = seleccionado.HasValue ? productos.FindIndex(x => x.IdProducto == seleccionado) : 0;
        if (posicion < 0) posicion = 0;
        _gridProductos.ClearSelection();
        if (_gridProductos.Rows.Count > posicion)
        {
            _gridProductos.Rows[posicion].Selected = true;
            _gridProductos.CurrentCell = _gridProductos.Rows[posicion].Cells[0];
        }
        SeleccionarProducto();
    }

    private void CargarComboMovimientos(List<ProductoInventarioModel> productos)
    {
        long? seleccionado = (_movProducto.SelectedItem as ProductoInventarioModel)?.IdProducto;
        List<ProductoInventarioModel> opciones = new() { new ProductoInventarioModel { IdProducto = 0, Nombre = "Todos los productos", Codigo = string.Empty } };
        opciones.AddRange(productos.Where(p => p.Activo));
        _movProducto.DataSource = opciones;
        _movProducto.DisplayMember = "Nombre";
        _movProducto.ValueMember = "IdProducto";
        if (seleccionado.HasValue)
        {
            int i = opciones.FindIndex(x => x.IdProducto == seleccionado.Value);
            if (i >= 0) _movProducto.SelectedIndex = i;
        }
    }

    private void SeleccionarProducto()
    {
        _productoSeleccionado = _gridProductos.CurrentRow?.DataBoundItem as ProductoInventarioModel;
        CargarLotes();
    }

    private void CargarLotes()
    {
        if (_productoSeleccionado is null)
        {
            _lblProductoLotes.Text = "Seleccione un producto en la pestaña Productos.";
            _lotesSource.DataSource = new List<LoteInventarioModel>();
            return;
        }
        _lblProductoLotes.Text = $"{_productoSeleccionado.Codigo} · {_productoSeleccionado.Nombre}   |   Stock disponible: {_productoSeleccionado.StockDisponible:0.###} {_productoSeleccionado.UnidadMedida}";
        _lotesSource.DataSource = _servicio.ListarLotes(_productoSeleccionado.IdProducto);
    }

    private void CargarMovimientos()
    {
        long? producto = _movProducto.SelectedItem is ProductoInventarioModel elegido && elegido.IdProducto > 0 ? elegido.IdProducto : null;
        string tipo = Convert.ToString(_movTipo.SelectedItem) ?? "Todos";
        _movimientosSource.DataSource = _servicio.ListarMovimientos(_movInicio.Value.Date, _movFin.Value.Date, producto, tipo);
    }

    private void CargarAlertas()
    {
        _stockBajoSource.DataSource = _servicio.ListarStockBajo();
        _vencimientosSource.DataSource = _servicio.ListarVencimientos(Convert.ToInt32(_diasVencimiento.Value), true);
    }

    private void NuevoProducto()
    {
        using FormProductoInventario dialogo = new(null);
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        Ejecutar(() => { _servicio.GuardarProducto(dialogo.Producto); CargarTodo(); }, "Producto registrado correctamente.");
    }

    private void EditarProducto()
    {
        if (_productoSeleccionado is null) { Aviso("Seleccione un producto para editar."); return; }
        using FormProductoInventario dialogo = new(_servicio.ObtenerProducto(_productoSeleccionado.IdProducto));
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        Ejecutar(() => { _servicio.GuardarProducto(dialogo.Producto); CargarTodo(); }, "Producto actualizado correctamente.");
    }

    private void RegistrarEntrada()
    {
        if (_productoSeleccionado is null) { Aviso("Seleccione un producto en la pestaña Productos."); return; }
        using FormEntradaInventario dialogo = new(_productoSeleccionado);
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        Ejecutar(() => { _servicio.RegistrarEntrada(dialogo.Solicitud); CargarTodo(); }, "Entrada registrada correctamente.");
    }

    private void RegistrarMovimiento()
    {
        if (_productoSeleccionado is null) { Aviso("Seleccione un producto en la pestaña Productos."); return; }
        List<LoteInventarioModel> lotes = _servicio.ListarLotes(_productoSeleccionado.IdProducto, false);
        if (lotes.Count == 0) { Aviso("El producto no tiene lotes con existencias para operar."); return; }
        using FormMovimientoInventario dialogo = new(_productoSeleccionado, lotes);
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        Ejecutar(() => { _servicio.RegistrarMovimientoManual(dialogo.Solicitud); CargarTodo(); }, "Movimiento registrado correctamente.");
    }

    private void ProcesarVencidos()
    {
        if (MessageBox.Show("Se darán de baja las existencias de todos los lotes vencidos y se registrará trazabilidad. ¿Continuar?", "Inventario", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        Ejecutar(() =>
        {
            int cantidad = _servicio.ProcesarLotesVencidos();
            CargarTodo();
            Aviso(cantidad == 0 ? "No existen lotes vencidos pendientes de procesar." : $"Se procesaron {cantidad} lote(s) vencido(s).");
        });
    }

    private static void Ejecutar(Action accion, string? mensaje = null)
    {
        try
        {
            accion();
            if (!string.IsNullOrWhiteSpace(mensaje)) Aviso(mensaje);
        }
        catch (Exception ex) { MostrarError("La operación no pudo completarse.", ex); }
    }

    private static void Aviso(string mensaje) => MessageBox.Show(mensaje, "Inventario", MessageBoxButtons.OK, MessageBoxIcon.Information);
    private static void MostrarError(string mensaje, Exception ex) => MessageBox.Show($"{mensaje}\n\n{ex.Message}", "Inventario", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}

internal sealed class FormProductoInventario : Form
{
    private readonly TextBox _codigo = new() { Width = 220 };
    private readonly TextBox _nombre = new() { Width = 280 };
    private readonly ComboBox _categoria = new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _presentacion = new() { Width = 280 };
    private readonly TextBox _unidad = new() { Width = 160 };
    private readonly NumericUpDown _compra = new() { Width = 150, DecimalPlaces = 2, Maximum = 99999999 };
    private readonly NumericUpDown _venta = new() { Width = 150, DecimalPlaces = 2, Maximum = 99999999 };
    private readonly NumericUpDown _minimo = new() { Width = 150, DecimalPlaces = 3, Maximum = 99999999 };
    private readonly CheckBox _lotes = new() { Text = "Controla lotes", AutoSize = true, Checked = true };
    private readonly CheckBox _activo = new() { Text = "Activo", AutoSize = true, Checked = true };
    private readonly long _id;
    public ProductoInventarioModel Producto { get; private set; } = new();

    public FormProductoInventario(ProductoInventarioModel? producto)
    {
        _id = producto?.IdProducto ?? 0;
        UiTheme.PrepararFormulario(this);
        Text = producto is null ? "Nuevo producto" : "Editar producto";
        Width = 610; Height = 570; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
        Label titulo = UiTheme.CrearTitulo(Text); titulo.Location = new Point(24, 18); Controls.Add(titulo);
        _categoria.Items.AddRange(new object[] { "Medicamento", "Vacuna", "Desparasitante", "Insumo", "Producto de venta" });
        Agregar("Código", _codigo, 80);
        Controls.Add(new Label { Text = "Déjalo vacío para generar un código interno automáticamente.", AutoSize = true, ForeColor = UiTheme.TextoSecundario, Location = new Point(188, 111) });
        Agregar("Nombre *", _nombre, 145); Agregar("Categoría *", _categoria, 185); Agregar("Presentación", _presentacion, 225); Agregar("Unidad medida *", _unidad, 265); Agregar("Precio compra (Q)", _compra, 305); Agregar("Precio venta (Q, IVA incl.)", _venta, 345); Agregar("Stock mínimo", _minimo, 385);
        _lotes.Location = new Point(188, 429); Controls.Add(_lotes); _activo.Location = new Point(318, 429); Controls.Add(_activo);
        Button guardar = UiTheme.CrearBoton("Guardar", true); guardar.Width = 110; guardar.Location = new Point(455, 473); guardar.Click += (_, _) => Confirmar(); Controls.Add(guardar);
        Button cancelar = UiTheme.CrearBoton("Cancelar"); cancelar.Width = 104; cancelar.Location = new Point(340, 473); cancelar.DialogResult = DialogResult.Cancel; Controls.Add(cancelar); CancelButton = cancelar;
        if (producto is not null) Cargar(producto); else { _categoria.SelectedIndex = 0; _unidad.Text = "Unidad"; }
    }
    private void Agregar(string etiqueta, Control control, int y) { Controls.Add(new Label { Text = etiqueta, AutoSize = true, Location = new Point(26, y + 7) }); control.Location = new Point(188, y); Controls.Add(control); }
    private void Cargar(ProductoInventarioModel p) { _codigo.Text = p.Codigo; _nombre.Text = p.Nombre; _categoria.SelectedItem = p.Categoria; _presentacion.Text = p.Presentacion; _unidad.Text = p.UnidadMedida; _compra.Value = p.PrecioCompra; _venta.Value = p.PrecioVenta; _minimo.Value = p.StockMinimo; _lotes.Checked = p.ControlaLotes; _activo.Checked = p.Activo; }
    private void Confirmar()
    {
        if (string.IsNullOrWhiteSpace(_nombre.Text) || _categoria.SelectedItem is null || string.IsNullOrWhiteSpace(_unidad.Text)) { MessageBox.Show("Nombre, categoría y unidad son obligatorios."); return; }
        Producto = new ProductoInventarioModel { IdProducto = _id, Codigo = _codigo.Text.Trim(), Nombre = _nombre.Text.Trim(), Categoria = _categoria.SelectedItem.ToString()!, Presentacion = _presentacion.Text.Trim(), UnidadMedida = _unidad.Text.Trim(), PrecioCompra = _compra.Value, PrecioVenta = _venta.Value, StockMinimo = _minimo.Value, ControlaLotes = _lotes.Checked, Activo = _activo.Checked };
        DialogResult = DialogResult.OK; Close();
    }
}

internal sealed class FormEntradaInventario : Form
{
    private readonly ProductoInventarioModel _producto;
    private readonly TextBox _lote = new() { Width = 240 };
    private readonly DateTimePicker _vencimiento = new() { Width = 160, Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = true, Value = DateTime.Today.AddYears(1) };
    private readonly NumericUpDown _cantidad = new() { Width = 140, DecimalPlaces = 3, Maximum = 9999999, Minimum = 0.001M, Value = 1 };
    private readonly NumericUpDown _costo = new() { Width = 140, DecimalPlaces = 2, Maximum = 99999999 };
    private readonly TextBox _proveedor = new() { Width = 270 };
    private readonly TextBox _observaciones = new() { Width = 270, Height = 60, Multiline = true };
    public EntradaInventarioRequestModel Solicitud { get; private set; } = new();

    public FormEntradaInventario(ProductoInventarioModel producto)
    {
        _producto = producto;
        UiTheme.PrepararFormulario(this); Text = "Entrada de inventario"; Width = 570; Height = 495; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
        Label titulo = UiTheme.CrearTitulo("Registrar entrada"); titulo.Location = new Point(24, 18); Controls.Add(titulo);
        Controls.Add(new Label { Text = $"{producto.Codigo} · {producto.Nombre}", AutoSize = true, ForeColor = UiTheme.Primario, Font = UiTheme.FuenteSubtitulo, Location = new Point(26, 62) });
        _costo.Value = producto.PrecioCompra;
        Agregar("Número de lote *", _lote, 103);
        Controls.Add(new Label { Text = "Ingrese el lote impreso por el fabricante o proveedor.", AutoSize = true, ForeColor = UiTheme.TextoSecundario, Location = new Point(180, 134) });
        Agregar("Fecha vencimiento", _vencimiento, 163); Agregar("Cantidad *", _cantidad, 203); Agregar("Costo unitario", _costo, 243); Agregar("Proveedor", _proveedor, 283); Agregar("Observaciones", _observaciones, 323);
        Button guardar = UiTheme.CrearBoton("Registrar", true); guardar.Width = 112; guardar.Location = new Point(418, 412); guardar.Click += (_, _) => Confirmar(); Controls.Add(guardar);
        Button cancelar = UiTheme.CrearBoton("Cancelar"); cancelar.Width = 105; cancelar.Location = new Point(305, 412); cancelar.DialogResult = DialogResult.Cancel; Controls.Add(cancelar); CancelButton = cancelar;
    }
    private void Agregar(string etiqueta, Control control, int y) { Controls.Add(new Label { Text = etiqueta, AutoSize = true, Location = new Point(26, y + 7) }); control.Location = new Point(180, y); Controls.Add(control); }
    private void Confirmar()
    {
        if (string.IsNullOrWhiteSpace(_lote.Text)) { MessageBox.Show("El número de lote es obligatorio."); return; }
        Solicitud = new EntradaInventarioRequestModel { IdProducto = _producto.IdProducto, NumeroLote = _lote.Text.Trim(), FechaVencimiento = _vencimiento.Checked ? _vencimiento.Value.Date : null, Cantidad = _cantidad.Value, CostoUnitario = _costo.Value, Proveedor = _proveedor.Text.Trim(), Observaciones = _observaciones.Text.Trim() };
        DialogResult = DialogResult.OK; Close();
    }
}

internal sealed class FormMovimientoInventario : Form
{
    private readonly ProductoInventarioModel _producto;
    private readonly ComboBox _lote = new() { Width = 275, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _tipo = new() { Width = 230, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _cantidad = new() { Width = 140, DecimalPlaces = 3, Maximum = 9999999, Minimum = 0.001M, Value = 1 };
    private readonly TextBox _observaciones = new() { Width = 290, Height = 74, Multiline = true };
    public MovimientoManualRequestModel Solicitud { get; private set; } = new();

    public FormMovimientoInventario(ProductoInventarioModel producto, List<LoteInventarioModel> lotes)
    {
        _producto = producto;
        UiTheme.PrepararFormulario(this); Text = "Salida o ajuste de inventario"; Width = 545; Height = 365; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
        Label titulo = UiTheme.CrearTitulo("Movimiento manual"); titulo.Location = new Point(24, 18); Controls.Add(titulo);
        Controls.Add(new Label { Text = $"{producto.Codigo} · {producto.Nombre}", AutoSize = true, ForeColor = UiTheme.Primario, Font = UiTheme.FuenteSubtitulo, Location = new Point(26, 62) });
        _lote.DataSource = lotes; _lote.DisplayMember = "NumeroLote"; _lote.ValueMember = "IdLote";
        _tipo.Items.AddRange(new object[] { "Salida por venta", "Ajuste positivo", "Ajuste negativo", "Merma", "Vencimiento" }); _tipo.SelectedIndex = 0;
        Agregar("Lote *", _lote, 103); Agregar("Movimiento *", _tipo, 143); Agregar("Cantidad *", _cantidad, 183); Agregar("Observaciones *", _observaciones, 223);
        Button guardar = UiTheme.CrearBoton("Registrar", true); guardar.Width = 112; guardar.Location = new Point(391, 307); guardar.Click += (_, _) => Confirmar(); Controls.Add(guardar);
        Button cancelar = UiTheme.CrearBoton("Cancelar"); cancelar.Width = 105; cancelar.Location = new Point(278, 307); cancelar.DialogResult = DialogResult.Cancel; Controls.Add(cancelar); CancelButton = cancelar;
    }
    private void Agregar(string etiqueta, Control control, int y) { Controls.Add(new Label { Text = etiqueta, AutoSize = true, Location = new Point(26, y + 7) }); control.Location = new Point(185, y); Controls.Add(control); }
    private void Confirmar()
    {
        if (_lote.SelectedItem is not LoteInventarioModel lote || _tipo.SelectedItem is null) { MessageBox.Show("Seleccione lote y tipo de movimiento."); return; }
        if (string.IsNullOrWhiteSpace(_observaciones.Text)) { MessageBox.Show("La observación es obligatoria para conservar trazabilidad."); return; }
        Solicitud = new MovimientoManualRequestModel { IdProducto = _producto.IdProducto, IdLote = lote.IdLote, TipoMovimiento = _tipo.SelectedItem.ToString()!, Cantidad = _cantidad.Value, Observaciones = _observaciones.Text.Trim() };
        DialogResult = DialogResult.OK; Close();
    }
}
