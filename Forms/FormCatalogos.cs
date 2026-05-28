using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Services;
using ClinicaVeterinaria.Utils;

namespace ClinicaVeterinaria.Forms;

public sealed class FormCatalogos : Form
{
    private readonly CatalogoService _servicio = new();
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private DataGridView _servicios = null!; private DataGridView _medicamentos = null!; private DataGridView _vacunas = null!; private DataGridView _desparasitantes = null!; private DataGridView _pagos = null!; private DataGridView _bloqueos = null!;

    public FormCatalogos()
    {
        UiTheme.PrepararFormulario(this); Dock = DockStyle.Fill; ConstruirInterfaz(); Shown += (_, _) => CargarTodo();
    }

    private void ConstruirInterfaz()
    {
        TableLayoutPanel raiz = new() { Dock = DockStyle.Fill, RowCount = 2, BackColor = UiTheme.Fondo };
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 66)); raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); Controls.Add(raiz);
        Panel cabecera = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20, 10, 18, 8) };
        Label titulo = UiTheme.CrearTitulo("Catálogos administrativos"); titulo.Location = new Point(20, 10); cabecera.Controls.Add(titulo);
        cabecera.Controls.Add(new Label { Text = "Servicios, medicamentos, vacunas, desparasitantes y parámetros operativos.", AutoSize = true, ForeColor = UiTheme.TextoSecundario, Location = new Point(22, 41) });
        Button actualizar = UiTheme.CrearBoton("Actualizar", true); actualizar.Width = 108; actualizar.Dock = DockStyle.Right; actualizar.Click += (_, _) => CargarTodo(); cabecera.Controls.Add(actualizar); raiz.Controls.Add(cabecera, 0, 0);
        _tabs.TabPages.Add(ConstruirServicios()); _tabs.TabPages.Add(ConstruirMedicamentos()); _tabs.TabPages.Add(ConstruirVacunas()); _tabs.TabPages.Add(ConstruirDesparasitantes()); _tabs.TabPages.Add(ConstruirMetodos()); _tabs.TabPages.Add(ConstruirBloqueos()); raiz.Controls.Add(_tabs, 0, 1);
    }

    private TabPage ConstruirServicios()
    {
        TabPage tab = CrearTab("Servicios"); _servicios = CrearGrid();
        _servicios.Columns.Add(Col("Codigo", "Código", 16)); _servicios.Columns.Add(Col("Nombre", "Servicio", 30)); _servicios.Columns.Add(Col("PrecioBase", "Precio", 15, "C2")); _servicios.Columns.Add(Col("DuracionMinutos", "Duración", 15)); _servicios.Columns.Add(Check("GeneraCargo", "Cargo", 12)); _servicios.Columns.Add(Check("Activo", "Activo", 12));
        AgregarGridConAcciones(tab, _servicios, () => EditarServicio(null), () => EditarServicio(Seleccion<ServicioCatalogoModel>(_servicios))); return tab;
    }
    private TabPage ConstruirMedicamentos()
    {
        TabPage tab = CrearTab("Medicamentos"); _medicamentos = CrearGrid();
        _medicamentos.Columns.Add(Col("Codigo", "Código", 15)); _medicamentos.Columns.Add(Col("Nombre", "Medicamento", 26)); _medicamentos.Columns.Add(Col("Presentacion", "Presentación", 18)); _medicamentos.Columns.Add(Col("Concentracion", "Concentración", 16)); _medicamentos.Columns.Add(Col("PrecioVenta", "Precio", 13, "C2")); _medicamentos.Columns.Add(Col("ProductoInventario", "Inventario", 22)); _medicamentos.Columns.Add(Check("Activo", "Activo", 10));
        AgregarGridConAcciones(tab, _medicamentos, () => EditarMedicamento(null), () => EditarMedicamento(Seleccion<MedicamentoCatalogoModel>(_medicamentos))); return tab;
    }
    private TabPage ConstruirVacunas()
    {
        TabPage tab = CrearTab("Vacunas"); _vacunas = CrearGrid();
        _vacunas.Columns.Add(Col("Codigo", "Código", 15)); _vacunas.Columns.Add(Col("Nombre", "Vacuna", 25)); _vacunas.Columns.Add(Col("EspecieAplicable", "Especie", 18)); _vacunas.Columns.Add(Col("IntervaloDiasSugerido", "Intervalo días", 15)); _vacunas.Columns.Add(Col("PrecioBase", "Precio", 13, "C2")); _vacunas.Columns.Add(Col("ProductoInventario", "Inventario", 22)); _vacunas.Columns.Add(Check("Activo", "Activo", 10));
        AgregarGridConAcciones(tab, _vacunas, () => EditarVacuna(null), () => EditarVacuna(Seleccion<VacunaCatalogoModel>(_vacunas))); return tab;
    }
    private TabPage ConstruirDesparasitantes()
    {
        TabPage tab = CrearTab("Desparasitantes"); _desparasitantes = CrearGrid();
        _desparasitantes.Columns.Add(Col("Codigo", "Código", 15)); _desparasitantes.Columns.Add(Col("Nombre", "Producto", 25)); _desparasitantes.Columns.Add(Col("Presentacion", "Presentación", 18)); _desparasitantes.Columns.Add(Col("DosisSugerida", "Dosis sugerida", 24)); _desparasitantes.Columns.Add(Col("PrecioBase", "Precio", 13, "C2")); _desparasitantes.Columns.Add(Col("ProductoInventario", "Inventario", 22)); _desparasitantes.Columns.Add(Check("Activo", "Activo", 10));
        AgregarGridConAcciones(tab, _desparasitantes, () => EditarDesparasitante(null), () => EditarDesparasitante(Seleccion<DesparasitanteCatalogoModel>(_desparasitantes))); return tab;
    }
    private TabPage ConstruirMetodos()
    {
        TabPage tab = CrearTab("Métodos de pago"); _pagos = CrearGrid(); _pagos.Columns.Add(Col("Nombre", "Método de pago", 80)); _pagos.Columns.Add(Check("Activo", "Activo", 20));
        AgregarGridConAcciones(tab, _pagos, () => EditarMetodo(null), () => EditarMetodo(Seleccion<MetodoPagoCatalogoModel>(_pagos))); return tab;
    }
    private TabPage ConstruirBloqueos()
    {
        TabPage tab = CrearTab("Tipos de bloqueo"); _bloqueos = CrearGrid(); _bloqueos.Columns.Add(Col("Nombre", "Tipo de bloqueo", 80)); _bloqueos.Columns.Add(Check("Activo", "Activo", 20));
        AgregarGridConAcciones(tab, _bloqueos, () => EditarBloqueo(null), () => EditarBloqueo(Seleccion<TipoBloqueoCatalogoModel>(_bloqueos))); return tab;
    }

    private static TabPage CrearTab(string nombre) => new(nombre) { BackColor = Color.White, Padding = new Padding(14) };
    private static DataGridView CrearGrid() { DataGridView grid = new() { Dock = DockStyle.Fill, AutoGenerateColumns = false }; UiTheme.PrepararGrid(grid); return grid; }
    private static DataGridViewTextBoxColumn Col(string propiedad, string titulo, float peso, string? formato = null) => new() { DataPropertyName = propiedad, HeaderText = titulo, FillWeight = peso, DefaultCellStyle = formato is null ? new DataGridViewCellStyle() : new DataGridViewCellStyle { Format = formato } };
    private static DataGridViewCheckBoxColumn Check(string propiedad, string titulo, float peso) => new() { DataPropertyName = propiedad, HeaderText = titulo, FillWeight = peso };
    private static T? Seleccion<T>(DataGridView grid) where T : class => grid.CurrentRow?.DataBoundItem as T;

    private static void AgregarGridConAcciones(TabPage tab, DataGridView grid, Action nuevo, Action editar)
    {
        TableLayoutPanel panel = new() { Dock = DockStyle.Fill, RowCount = 2 }; panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        FlowLayoutPanel acciones = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 5, 0, 5) };
        Button btnNuevo = UiTheme.CrearBoton("Nuevo", true); btnNuevo.Width = 98; btnNuevo.Click += (_, _) => nuevo(); Button btnEditar = UiTheme.CrearBoton("Editar"); btnEditar.Width = 98; btnEditar.Click += (_, _) => editar(); acciones.Controls.AddRange(new Control[] { btnNuevo, btnEditar });
        panel.Controls.Add(acciones, 0, 0); panel.Controls.Add(grid, 0, 1); tab.Controls.Add(panel);
    }

    private void CargarTodo()
    {
        try { _servicios.DataSource = _servicio.ListarServicios(); _medicamentos.DataSource = _servicio.ListarMedicamentos(); _vacunas.DataSource = _servicio.ListarVacunas(); _desparasitantes.DataSource = _servicio.ListarDesparasitantes(); _pagos.DataSource = _servicio.ListarMetodosPago(); _bloqueos.DataSource = _servicio.ListarTiposBloqueo(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Catálogos", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    private void EditarServicio(ServicioCatalogoModel? item) { using FormServicioCatalogo dlg = new(item); if (dlg.ShowDialog(this) == DialogResult.OK) { Ejecutar(() => _servicio.GuardarServicio(dlg.Registro)); } }
    private void EditarMedicamento(MedicamentoCatalogoModel? item) { using FormMedicamentoCatalogo dlg = new(item, _servicio.ListarProductosActivos()); if (dlg.ShowDialog(this) == DialogResult.OK) Ejecutar(() => _servicio.GuardarMedicamento(dlg.Registro)); }
    private void EditarVacuna(VacunaCatalogoModel? item) { using FormVacunaCatalogo dlg = new(item, _servicio.ListarProductosActivos()); if (dlg.ShowDialog(this) == DialogResult.OK) Ejecutar(() => _servicio.GuardarVacuna(dlg.Registro)); }
    private void EditarDesparasitante(DesparasitanteCatalogoModel? item) { using FormDesparasitanteCatalogo dlg = new(item, _servicio.ListarProductosActivos()); if (dlg.ShowDialog(this) == DialogResult.OK) Ejecutar(() => _servicio.GuardarDesparasitante(dlg.Registro)); }
    private void EditarMetodo(MetodoPagoCatalogoModel? item) { using FormNombreActivo dlg = new("Método de pago", item?.Nombre ?? "", item?.Activo ?? true); if (dlg.ShowDialog(this) == DialogResult.OK) Ejecutar(() => _servicio.GuardarMetodoPago(new MetodoPagoCatalogoModel { IdMetodoPago = item?.IdMetodoPago ?? 0, Nombre = dlg.Nombre, Activo = dlg.Activo })); }
    private void EditarBloqueo(TipoBloqueoCatalogoModel? item) { using FormNombreActivo dlg = new("Tipo de bloqueo", item?.Nombre ?? "", item?.Activo ?? true); if (dlg.ShowDialog(this) == DialogResult.OK) Ejecutar(() => _servicio.GuardarTipoBloqueo(new TipoBloqueoCatalogoModel { IdTipoBloqueo = item?.IdTipoBloqueo ?? 0, Nombre = dlg.Nombre, Activo = dlg.Activo })); }
    private void Ejecutar(Action accion) { try { accion(); CargarTodo(); } catch (Exception ex) { MessageBox.Show(ex.Message, "Catálogos", MessageBoxButtons.OK, MessageBoxIcon.Warning); } }
}

internal abstract class FormCatalogoBase : Form
{
    protected FormCatalogoBase(string titulo, int alto = 555)
    {
        UiTheme.PrepararFormulario(this); Text = titulo; Width = 650; Height = alto; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
        Controls.Add(new Label { Text = titulo, AutoSize = true, Font = UiTheme.FuenteTitulo, Location = new Point(25, 18) });
    }
    protected void Etiqueta(string texto, int y) => Controls.Add(new Label { Text = texto, AutoSize = true, Location = new Point(28, y + 5) });
    protected TextBox Texto(string etiqueta, int y, int ancho = 400) { Etiqueta(etiqueta, y); TextBox c = new() { Location = new Point(192, y), Width = ancho }; Controls.Add(c); return c; }
    protected NumericUpDown Numero(string etiqueta, int y, int decimales = 2) { Etiqueta(etiqueta, y); NumericUpDown n = new() { Location = new Point(192, y), Width = 165, DecimalPlaces = decimales, Maximum = 1000000, ThousandsSeparator = true }; Controls.Add(n); return n; }
    protected CheckBox Check(string texto, int x, int y) { CheckBox c = new() { Text = texto, AutoSize = true, Location = new Point(x, y) }; Controls.Add(c); return c; }
    protected void Botones(Action guardar, int y) { Button ok = UiTheme.CrearBoton("Guardar", true); ok.Width = 110; ok.Location = new Point(482, y); ok.Click += (_, _) => guardar(); Controls.Add(ok); Button cancelar = UiTheme.CrearBoton("Cancelar"); cancelar.Width = 110; cancelar.Location = new Point(364, y); cancelar.DialogResult = DialogResult.Cancel; Controls.Add(cancelar); CancelButton = cancelar; }
}

internal sealed class FormServicioCatalogo : FormCatalogoBase
{
    private readonly ServicioCatalogoModel _base; private readonly TextBox _codigo; private readonly TextBox _nombre; private readonly TextBox _descripcion; private readonly NumericUpDown _precio; private readonly NumericUpDown _duracion; private readonly CheckBox _cargo; private readonly CheckBox _activo;
    public ServicioCatalogoModel Registro { get; private set; } = new();
    public FormServicioCatalogo(ServicioCatalogoModel? item) : base(item is null ? "Nuevo servicio" : "Editar servicio", 430)
    {
        _base = item ?? new ServicioCatalogoModel(); _codigo = Texto("Código automático", 74); _codigo.ReadOnly = true; _nombre = Texto("Nombre *", 112); _descripcion = Texto("Descripción", 150); _precio = Numero("Precio (Q, IVA incluido)", 188); _duracion = Numero("Duración minutos", 226, 0); _duracion.Increment = 30; _duracion.Minimum = 30; _cargo = Check("Genera cargo", 192, 270); _activo = Check("Activo", 328, 270); Botones(Guardar, 320);
        _codigo.Text = _base.Codigo; _nombre.Text = _base.Nombre; _descripcion.Text = _base.Descripcion; _precio.Value = _base.PrecioBase; _duracion.Value = _base.DuracionMinutos <= 0 ? 30 : _base.DuracionMinutos; _cargo.Checked = _base.GeneraCargo; _activo.Checked = _base.Activo;
    }
    private void Guardar() { Registro = new ServicioCatalogoModel { IdServicio = _base.IdServicio, Codigo = _codigo.Text, Nombre = _nombre.Text, Descripcion = _descripcion.Text, PrecioBase = _precio.Value, DuracionMinutos = (int)_duracion.Value, GeneraCargo = _cargo.Checked, Activo = _activo.Checked }; DialogResult = DialogResult.OK; Close(); }
}

internal sealed class FormMedicamentoCatalogo : FormCatalogoBase
{
    private readonly MedicamentoCatalogoModel _base; private readonly TextBox _codigo; private readonly TextBox _nombre; private readonly TextBox _presentacion; private readonly TextBox _concentracion; private readonly TextBox _via; private readonly TextBox _indicaciones; private readonly NumericUpDown _precio; private readonly CheckBox _controla; private readonly CheckBox _activo; private readonly ComboBox _producto;
    public MedicamentoCatalogoModel Registro { get; private set; } = new();
    public FormMedicamentoCatalogo(MedicamentoCatalogoModel? item, List<ProductoCatalogoVinculoModel> productos) : base(item is null ? "Nuevo medicamento" : "Editar medicamento", 590)
    {
        _base=item??new MedicamentoCatalogoModel(); _codigo=Texto("Código automático",70); _codigo.ReadOnly=true; _nombre=Texto("Nombre *",106); _presentacion=Texto("Presentación",142); _concentracion=Texto("Concentración",178); _via=Texto("Vía",214); _indicaciones=Texto("Indicaciones",250); _precio=Numero("Precio (Q, IVA incluido)",286); _controla=Check("Controla inventario",192,326); _activo=Check("Activo",350,326); Etiqueta("Producto inventario",362); _producto=new ComboBox { Location=new Point(192,362), Width=400, DropDownStyle=ComboBoxStyle.DropDownList }; _producto.Items.Add("-- Sin vínculo --"); foreach(var p in productos) _producto.Items.Add(p); Controls.Add(_producto); Botones(Guardar,435);
        _codigo.Text=_base.Codigo; _nombre.Text=_base.Nombre; _presentacion.Text=_base.Presentacion; _concentracion.Text=_base.Concentracion; _via.Text=_base.ViaAdministracion; _indicaciones.Text=_base.IndicacionesPredeterminadas; _precio.Value=_base.PrecioVenta; _controla.Checked=_base.ControlaInventario; _activo.Checked=_base.Activo; SeleccionarProducto(_base.IdProductoInventario);
    }
    private void SeleccionarProducto(long? id) { _producto.SelectedIndex=0; if(!id.HasValue) return; for(int i=1;i<_producto.Items.Count;i++) if(_producto.Items[i] is ProductoCatalogoVinculoModel p && p.IdProducto==id.Value) { _producto.SelectedIndex=i; break; } }
    private void Guardar() { Registro=new MedicamentoCatalogoModel { IdMedicamento=_base.IdMedicamento,Codigo=_codigo.Text,Nombre=_nombre.Text,Presentacion=_presentacion.Text,Concentracion=_concentracion.Text,ViaAdministracion=_via.Text,IndicacionesPredeterminadas=_indicaciones.Text,PrecioVenta=_precio.Value,ControlaInventario=_controla.Checked,IdProductoInventario=(_producto.SelectedItem as ProductoCatalogoVinculoModel)?.IdProducto,Activo=_activo.Checked }; DialogResult=DialogResult.OK; Close(); }
}

internal sealed class FormVacunaCatalogo : FormCatalogoBase
{
    private readonly VacunaCatalogoModel _base; private readonly TextBox _codigo; private readonly TextBox _nombre; private readonly TextBox _especie; private readonly TextBox _descripcion; private readonly NumericUpDown _dias; private readonly NumericUpDown _precio; private readonly CheckBox _controla; private readonly CheckBox _activo; private readonly ComboBox _producto;
    public VacunaCatalogoModel Registro { get; private set; } = new();
    public FormVacunaCatalogo(VacunaCatalogoModel? item, List<ProductoCatalogoVinculoModel> productos) : base(item is null?"Nueva vacuna":"Editar vacuna",540)
    {
        _base=item??new VacunaCatalogoModel(); _codigo=Texto("Código automático",70); _codigo.ReadOnly=true; _nombre=Texto("Nombre *",106); _especie=Texto("Especie aplicable",142); _descripcion=Texto("Descripción",178); _dias=Numero("Intervalo días",214,0); _precio=Numero("Precio (Q, IVA incluido)",250); _controla=Check("Controla inventario",192,290); _activo=Check("Activo",350,290); Etiqueta("Producto inventario",326); _producto=new ComboBox { Location=new Point(192,326), Width=400, DropDownStyle=ComboBoxStyle.DropDownList }; _producto.Items.Add("-- Sin vínculo --"); foreach(var p in productos) _producto.Items.Add(p); Controls.Add(_producto); Botones(Guardar,402);
        _codigo.Text=_base.Codigo; _nombre.Text=_base.Nombre; _especie.Text=_base.EspecieAplicable; _descripcion.Text=_base.Descripcion; _dias.Value=_base.IntervaloDiasSugerido??365; _precio.Value=_base.PrecioBase; _controla.Checked=_base.ControlaInventario; _activo.Checked=_base.Activo; Seleccionar(_base.IdProductoInventario);
    }
    private void Seleccionar(long? id) { _producto.SelectedIndex=0; if(!id.HasValue)return; for(int i=1;i<_producto.Items.Count;i++) if(_producto.Items[i] is ProductoCatalogoVinculoModel p && p.IdProducto==id.Value){_producto.SelectedIndex=i; break;} }
    private void Guardar() { Registro=new VacunaCatalogoModel { IdVacuna=_base.IdVacuna,Codigo=_codigo.Text,Nombre=_nombre.Text,EspecieAplicable=_especie.Text,Descripcion=_descripcion.Text,IntervaloDiasSugerido=(int)_dias.Value,PrecioBase=_precio.Value,ControlaInventario=_controla.Checked,IdProductoInventario=(_producto.SelectedItem as ProductoCatalogoVinculoModel)?.IdProducto,Activo=_activo.Checked }; DialogResult=DialogResult.OK; Close(); }
}

internal sealed class FormDesparasitanteCatalogo : FormCatalogoBase
{
    private readonly DesparasitanteCatalogoModel _base; private readonly TextBox _codigo; private readonly TextBox _nombre; private readonly TextBox _presentacion; private readonly TextBox _dosis; private readonly NumericUpDown _dias; private readonly NumericUpDown _precio; private readonly CheckBox _controla; private readonly CheckBox _activo; private readonly ComboBox _producto;
    public DesparasitanteCatalogoModel Registro { get; private set; } = new();
    public FormDesparasitanteCatalogo(DesparasitanteCatalogoModel? item, List<ProductoCatalogoVinculoModel> productos) : base(item is null?"Nuevo desparasitante":"Editar desparasitante",540)
    {
        _base=item??new DesparasitanteCatalogoModel(); _codigo=Texto("Código automático",70); _codigo.ReadOnly=true; _nombre=Texto("Nombre *",106); _presentacion=Texto("Presentación",142); _dosis=Texto("Dosis sugerida",178); _dias=Numero("Intervalo días",214,0); _precio=Numero("Precio (Q, IVA incluido)",250); _controla=Check("Controla inventario",192,290); _activo=Check("Activo",350,290); Etiqueta("Producto inventario",326); _producto=new ComboBox { Location=new Point(192,326), Width=400, DropDownStyle=ComboBoxStyle.DropDownList }; _producto.Items.Add("-- Sin vínculo --"); foreach(var p in productos) _producto.Items.Add(p); Controls.Add(_producto); Botones(Guardar,402);
        _codigo.Text=_base.Codigo; _nombre.Text=_base.Nombre; _presentacion.Text=_base.Presentacion; _dosis.Text=_base.DosisSugerida; _dias.Value=_base.IntervaloDiasSugerido??90; _precio.Value=_base.PrecioBase; _controla.Checked=_base.ControlaInventario; _activo.Checked=_base.Activo; Seleccionar(_base.IdProductoInventario);
    }
    private void Seleccionar(long? id) { _producto.SelectedIndex=0; if(!id.HasValue)return; for(int i=1;i<_producto.Items.Count;i++) if(_producto.Items[i] is ProductoCatalogoVinculoModel p && p.IdProducto==id.Value){_producto.SelectedIndex=i;break;} }
    private void Guardar() { Registro=new DesparasitanteCatalogoModel { IdDesparasitante=_base.IdDesparasitante,Codigo=_codigo.Text,Nombre=_nombre.Text,Presentacion=_presentacion.Text,DosisSugerida=_dosis.Text,IntervaloDiasSugerido=(int)_dias.Value,PrecioBase=_precio.Value,ControlaInventario=_controla.Checked,IdProductoInventario=(_producto.SelectedItem as ProductoCatalogoVinculoModel)?.IdProducto,Activo=_activo.Checked }; DialogResult=DialogResult.OK; Close(); }
}

internal sealed class FormNombreActivo : Form
{
    private readonly TextBox _nombre; private readonly CheckBox _activo; public string Nombre => _nombre.Text.Trim(); public bool Activo => _activo.Checked;
    public FormNombreActivo(string titulo, string nombre, bool activo)
    {
        UiTheme.PrepararFormulario(this); Text=titulo; Width=500; Height=250; FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; MinimizeBox=false; ShowInTaskbar=false;
        Controls.Add(new Label { Text=titulo, AutoSize=true, Font=UiTheme.FuenteTitulo, Location=new Point(24,20) }); Controls.Add(new Label { Text="Nombre *", AutoSize=true, Location=new Point(28,82) }); _nombre=new TextBox { Location=new Point(145,78), Width=300, Text=nombre }; Controls.Add(_nombre); _activo=new CheckBox { Text="Activo", AutoSize=true, Location=new Point(145,118), Checked=activo }; Controls.Add(_activo);
        Button guardar=UiTheme.CrearBoton("Guardar", true); guardar.Width=104; guardar.Location=new Point(341,158); guardar.Click+=(_,_)=>{DialogResult=DialogResult.OK;Close();}; Controls.Add(guardar); Button cancelar=UiTheme.CrearBoton("Cancelar"); cancelar.Width=104; cancelar.Location=new Point(228,158); cancelar.DialogResult=DialogResult.Cancel; Controls.Add(cancelar); CancelButton=cancelar;
    }
}
