using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Services;
using ClinicaVeterinaria.Utils;

namespace ClinicaVeterinaria.Forms;

public sealed class FormHospitalizacion : Form
{
    private readonly HospitalizacionService _servicio = new();
    private Label _ingresados = null!;
    private Label _observacion = null!;
    private Label _altas = null!;
    private Label _cargos = null!;
    private DateTimePicker _desde = null!;
    private DateTimePicker _hasta = null!;
    private ComboBox _estado = null!;
    private TextBox _buscar = null!;
    private DataGridView _grid = null!;
    private DataGridView _gridEvoluciones = null!;
    private Label _detalle = null!;

    public FormHospitalizacion()
    {
        UiTheme.PrepararFormulario(this); Dock = DockStyle.Fill; ConstruirInterfaz(); Shown += (_, _) => CargarTodo();
    }

    private void ConstruirInterfaz()
    {
        TableLayoutPanel raiz = new() { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1, BackColor = UiTheme.Fondo };
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 70)); raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 102)); raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 58)); raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 42)); Controls.Add(raiz);
        Panel cabecera = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20, 10, 16, 8) };
        Label titulo = UiTheme.CrearTitulo("Hospitalización y observación"); titulo.Location = new Point(20, 9); cabecera.Controls.Add(titulo);
        cabecera.Controls.Add(new Label { Text = "Ingresos, evoluciones, alta médica y cargos asociados a estancia.", AutoSize = true, Location = new Point(22, 43), ForeColor = UiTheme.TextoSecundario });
        Button actualizar = UiTheme.CrearBoton("Actualizar", true); actualizar.Dock = DockStyle.Right; actualizar.Width = 108; actualizar.Click += (_, _) => CargarTodo(); cabecera.Controls.Add(actualizar); raiz.Controls.Add(cabecera, 0, 0);
        TableLayoutPanel tarjetas = new() { Dock = DockStyle.Fill, ColumnCount = 4, Padding = new Padding(0, 8, 0, 8) }; for (int i=0;i<4;i++) tarjetas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));
        _ingresados = CrearTarjeta(tarjetas, "Ingresados", 0); _observacion = CrearTarjeta(tarjetas, "En observación", 1); _altas = CrearTarjeta(tarjetas, "Altas de hoy", 2); _cargos = CrearTarjeta(tarjetas, "Cargos pendientes", 3); raiz.Controls.Add(tarjetas, 0, 1);
        FlowLayoutPanel filtros = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(14, 10, 10, 8), WrapContents = false };
        filtros.Controls.Add(new Label { Text="Desde", AutoSize=true, Margin=new Padding(0,10,5,0) }); _desde = new DateTimePicker { Width=120, Format=DateTimePickerFormat.Short, Value=DateTime.Today.AddMonths(-1), Margin=new Padding(0,5,13,0) }; filtros.Controls.Add(_desde);
        filtros.Controls.Add(new Label { Text="Hasta", AutoSize=true, Margin=new Padding(0,10,5,0) }); _hasta = new DateTimePicker { Width=120, Format=DateTimePickerFormat.Short, Value=DateTime.Today.AddDays(1), Margin=new Padding(0,5,13,0) }; filtros.Controls.Add(_hasta);
        _estado = new ComboBox { Width=150, DropDownStyle=ComboBoxStyle.DropDownList, Margin=new Padding(0,6,10,0) }; _estado.Items.AddRange(new object[] { "Todos", "Ingresada", "En observación", "Alta", "Cancelada" }); _estado.SelectedIndex=0; filtros.Controls.Add(_estado);
        _buscar = new TextBox { Width=240, PlaceholderText="Paciente, dueño o jaula", Margin=new Padding(0,6,10,0) }; filtros.Controls.Add(_buscar);
        Button buscar = UiTheme.CrearBoton("Buscar", true); buscar.Width=82; buscar.Click += (_,_) => CargarLista(); filtros.Controls.Add(buscar);
        Button ingreso = UiTheme.CrearBoton("Nuevo ingreso", true); ingreso.Width=128; ingreso.Click += (_,_) => NuevoIngreso(); filtros.Controls.Add(ingreso); raiz.Controls.Add(filtros,0,2);
        Panel lista = new() { Dock=DockStyle.Fill, BackColor=Color.White, Padding=new Padding(14,8,14,5) };
        _grid = new DataGridView { Dock=DockStyle.Fill, AutoGenerateColumns=false }; UiTheme.PrepararGrid(_grid);
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName="FechaHoraIngreso", HeaderText="Ingreso", FillWeight=18, DefaultCellStyle=new DataGridViewCellStyle { Format="dd/MM/yyyy HH:mm" } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName="CodigoPaciente", HeaderText="Código", FillWeight=15 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName="Mascota", HeaderText="Paciente", FillWeight=17 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName="Dueno", HeaderText="Dueño", FillWeight=25 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName="Veterinario", HeaderText="Responsable", FillWeight=27 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName="EspacioAsignado", HeaderText="Espacio", FillWeight=17 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName="Estado", HeaderText="Estado", FillWeight=17 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName="Evoluciones", HeaderText="Evol.", FillWeight=10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName="SaldoPendiente", HeaderText="Pendiente", FillWeight=16, DefaultCellStyle=new DataGridViewCellStyle { Format="C2" } });
        _grid.SelectionChanged += (_,_) => CargarEvoluciones(); lista.Controls.Add(_grid); raiz.Controls.Add(lista,0,3);
        TableLayoutPanel inferior = new() { Dock=DockStyle.Fill, ColumnCount=2, Padding=new Padding(14,4,14,10), BackColor=Color.White }; inferior.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,72)); inferior.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,28));
        TableLayoutPanel evolPanel = new() { Dock=DockStyle.Fill, Padding=new Padding(0,0,10,0), ColumnCount=1, RowCount=2 };
        evolPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); evolPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _detalle = new Label { Text="Seleccione una hospitalización.", Dock=DockStyle.Fill, Height=38, Font=UiTheme.FuenteSubtitulo, ForeColor=UiTheme.Primario, TextAlign=ContentAlignment.MiddleLeft }; evolPanel.Controls.Add(_detalle,0,0);
        _gridEvoluciones = new DataGridView { Dock=DockStyle.Fill, AutoGenerateColumns=false }; UiTheme.PrepararGrid(_gridEvoluciones);
        _gridEvoluciones.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName="FechaHora", HeaderText="Fecha", FillWeight=18, DefaultCellStyle=new DataGridViewCellStyle { Format="dd/MM HH:mm" } });
        _gridEvoluciones.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName="Veterinario", HeaderText="Veterinario", FillWeight=24 });
        _gridEvoluciones.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName="Temperatura", HeaderText="Temp.", FillWeight=11 });
        _gridEvoluciones.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName="Peso", HeaderText="Peso", FillWeight=11 });
        _gridEvoluciones.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName="Observaciones", HeaderText="Observaciones", FillWeight=43 });
        evolPanel.Controls.Add(_gridEvoluciones,0,1); inferior.Controls.Add(evolPanel,0,0);
        FlowLayoutPanel acciones = new() { Dock=DockStyle.Fill, FlowDirection=FlowDirection.TopDown, Padding=new Padding(8,35,0,0), WrapContents=false };
        Button evolucion = UiTheme.CrearBoton("Registrar evolución", true); evolucion.Width=180; evolucion.Click += (_,_) => RegistrarEvolucion();
        Button cargo = UiTheme.CrearBoton("Agregar cargo", true); cargo.Width=180; cargo.Click += (_,_) => RegistrarCargo();
        Button alta = UiTheme.CrearBoton("Dar alta"); alta.Width=180; alta.Click += (_,_) => DarAlta();
        Button cancelar = UiTheme.CrearBoton("Cancelar ingreso"); cancelar.Width=180; cancelar.Click += (_,_) => Cancelar(); acciones.Controls.AddRange(new Control[] { evolucion, cargo, alta, cancelar }); inferior.Controls.Add(acciones,1,0); raiz.Controls.Add(inferior,0,4);
    }
    private HospitalizacionModel? Seleccionada => _grid.CurrentRow?.DataBoundItem as HospitalizacionModel;
    private static Label CrearTarjeta(TableLayoutPanel tabla, string titulo, int columna)
    {
        Panel p = new() { Dock=DockStyle.Fill, BackColor=Color.White, Margin=new Padding(5), Padding=new Padding(14,8,14,8) }; p.Controls.Add(new Label { Text=titulo, Dock=DockStyle.Top, Height=24, ForeColor=UiTheme.TextoSecundario }); Label v = new() { Text="0", Dock=DockStyle.Fill, TextAlign=ContentAlignment.MiddleLeft, Font=new Font("Segoe UI Semibold",20F,FontStyle.Bold), ForeColor=UiTheme.Primario }; p.Controls.Add(v); v.BringToFront(); tabla.Controls.Add(p,columna,0); return v;
    }
    private void CargarTodo() { try { HospitalizacionResumenModel r=_servicio.ObtenerResumen(); _ingresados.Text=r.PacientesIngresados.ToString(); _observacion.Text=r.EnObservacion.ToString(); _altas.Text=r.AltasHoy.ToString(); _cargos.Text=r.CargosPendientes.ToString("C2"); CargarLista(); } catch(Exception ex) { Error(ex); } }
    private void CargarLista() { try { _grid.DataSource=_servicio.Listar(_desde.Value,_hasta.Value,_estado.Text,_buscar.Text); CargarEvoluciones(); } catch(Exception ex) { Error(ex); } }
    private void CargarEvoluciones()
    {
        try { HospitalizacionModel? h=Seleccionada; if(h is null) { _detalle.Text="Seleccione una hospitalización."; _gridEvoluciones.DataSource=null; return; } _detalle.Text=$"{h.Mascota} · {h.Motivo} · {h.Estado} · Responsable: {h.Veterinario}"; _gridEvoluciones.DataSource=_servicio.ListarEvoluciones(h.IdHospitalizacion); }
        catch(Exception ex) { Error(ex); }
    }
    private void NuevoIngreso() { using FormNuevoIngresoHospitalario d=new(_servicio); if(d.ShowDialog(this)!=DialogResult.OK) return; try { _servicio.Ingresar(d.Resultado); CargarTodo(); MessageBox.Show("Ingreso hospitalario registrado correctamente.","Hospitalización",MessageBoxButtons.OK,MessageBoxIcon.Information); } catch(Exception ex){ Error(ex); } }
    private void RegistrarEvolucion() { if(Seleccionada is not HospitalizacionModel h) return; using FormEvolucionHospitalaria d=new(h); if(d.ShowDialog(this)!=DialogResult.OK) return; try { _servicio.RegistrarEvolucion(d.Resultado); CargarTodo(); } catch(Exception ex){ Error(ex); } }
    private void RegistrarCargo() { if(Seleccionada is not HospitalizacionModel h) return; using FormCargoHospitalario d=new(h); if(d.ShowDialog(this)!=DialogResult.OK) return; try { _servicio.RegistrarCargo(h.IdHospitalizacion,d.Resultado); CargarTodo(); MessageBox.Show("Cargo pendiente generado para caja.","Hospitalización",MessageBoxButtons.OK,MessageBoxIcon.Information); } catch(Exception ex){ Error(ex); } }
    private void DarAlta() { if(Seleccionada is not HospitalizacionModel h) return; using FormAltaHospitalaria d=new(h); if(d.ShowDialog(this)!=DialogResult.OK) return; try { _servicio.DarAlta(h.IdHospitalizacion,d.FechaAlta,d.Observaciones); CargarTodo(); } catch(Exception ex){ Error(ex); } }
    private void Cancelar() { if(Seleccionada is not HospitalizacionModel h) return; using FormNotaSimple d=new("Cancelar ingreso","Motivo de cancelación *"); if(d.ShowDialog(this)!=DialogResult.OK) return; try { _servicio.Cancelar(h.IdHospitalizacion,d.Texto); CargarTodo(); } catch(Exception ex){ Error(ex); } }
    private static void Error(Exception ex) => MessageBox.Show(ex.Message,"Hospitalización",MessageBoxButtons.OK,MessageBoxIcon.Warning);
}

internal sealed class FormNuevoIngresoHospitalario : Form
{
    private readonly HospitalizacionService _servicio; private TextBox _filtro=null!; private ComboBox _mascota=null!; private ComboBox _consulta=null!; private ComboBox _veterinario=null!; private DateTimePicker _ingreso=null!; private TextBox _motivo=null!; private TextBox _espacio=null!; private TextBox _observaciones=null!;
    public NuevaHospitalizacionModel Resultado { get; private set; }=new();
    public FormNuevoIngresoHospitalario(HospitalizacionService servicio) { _servicio=servicio; UiTheme.PrepararFormulario(this); Text="Nuevo ingreso hospitalario"; Width=720; Height=595; FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; MinimizeBox=false; ShowInTaskbar=false; Construir(); CargarVeterinarios(); BuscarMascotas(); }
    private void Construir()
    {
        Controls.Add(new Label { Text="Nuevo ingreso hospitalario", Font=UiTheme.FuenteTitulo, Location=new Point(22,18), AutoSize=true });
        AgregarEtiqueta("Buscar paciente",73); _filtro=new TextBox { Location=new Point(180,69), Width=305, PlaceholderText="Nombre, código o dueño" }; Controls.Add(_filtro); Button buscar=UiTheme.CrearBoton("Buscar",true); buscar.Width=93; buscar.Location=new Point(495,64); buscar.Click += (_,_)=>BuscarMascotas(); Controls.Add(buscar);
        AgregarEtiqueta("Paciente *",114); _mascota=new ComboBox { Location=new Point(180,110), Width=475, DropDownStyle=ComboBoxStyle.DropDownList, DisplayMember="Descripcion", ValueMember="IdMascota" }; _mascota.SelectedIndexChanged += (_,_)=>CargarConsultas(); Controls.Add(_mascota);
        AgregarEtiqueta("Consulta de origen",155); _consulta=new ComboBox { Location=new Point(180,151), Width=475, DropDownStyle=ComboBoxStyle.DropDownList, DisplayMember="Descripcion", ValueMember="IdConsulta" }; Controls.Add(_consulta);
        AgregarEtiqueta("Responsable *",196); _veterinario=new ComboBox { Location=new Point(180,192), Width=270, DropDownStyle=ComboBoxStyle.DropDownList, DisplayMember="NombreCompleto", ValueMember="IdVeterinario" }; Controls.Add(_veterinario);
        AgregarEtiqueta("Fecha ingreso *",237); _ingreso=new DateTimePicker { Location=new Point(180,233), Width=270, Format=DateTimePickerFormat.Custom, CustomFormat="dd/MM/yyyy HH:mm" }; Controls.Add(_ingreso);
        AgregarEtiqueta("Espacio / jaula",278); _espacio=new TextBox { Location=new Point(180,274), Width=270 }; Controls.Add(_espacio);
        AgregarEtiqueta("Motivo *",319); _motivo=new TextBox { Location=new Point(180,315), Width=475, Height=65, Multiline=true }; Controls.Add(_motivo);
        AgregarEtiqueta("Observaciones",395); _observaciones=new TextBox { Location=new Point(180,391), Width=475, Height=58, Multiline=true }; Controls.Add(_observaciones);
        Button guardar=UiTheme.CrearBoton("Registrar ingreso",true); guardar.Location=new Point(510,487); guardar.Width=145; guardar.Click += (_,_)=>Confirmar(); Controls.Add(guardar); Button cancelar=UiTheme.CrearBoton("Cancelar"); cancelar.Location=new Point(390,487); cancelar.Width=110; cancelar.DialogResult=DialogResult.Cancel; Controls.Add(cancelar); CancelButton=cancelar;
    }
    private void AgregarEtiqueta(string texto,int y)=>Controls.Add(new Label { Text=texto, Location=new Point(24,y), AutoSize=true });
    private void BuscarMascotas() { try { _mascota.DataSource=_servicio.BuscarMascotas(_filtro.Text); CargarConsultas(); } catch(Exception ex){ MessageBox.Show(ex.Message); } }
    private void CargarConsultas() { if(_mascota.SelectedItem is not MascotaBusquedaModel m) { _consulta.DataSource=null; return; } List<ConsultaOrigenHospitalizacionModel> lista=_servicio.ListarConsultasMascota(m.IdMascota); lista.Insert(0,new ConsultaOrigenHospitalizacionModel { IdConsulta=0, MotivoConsulta="Sin consulta de origen" }); _consulta.DataSource=lista; }
    private void CargarVeterinarios() { _veterinario.DataSource=_servicio.ListarVeterinariosActivos(); }
    private void Confirmar() { if(_mascota.SelectedItem is not MascotaBusquedaModel m || _veterinario.SelectedItem is not VeterinarioModel v) { MessageBox.Show("Seleccione paciente y veterinario."); return; } long? consulta=null; if(_consulta.SelectedItem is ConsultaOrigenHospitalizacionModel c && c.IdConsulta>0) consulta=c.IdConsulta; Resultado=new NuevaHospitalizacionModel { IdMascota=m.IdMascota, IdConsultaOrigen=consulta, IdVeterinario=v.IdVeterinario, FechaHoraIngreso=_ingreso.Value, Motivo=_motivo.Text.Trim(), EspacioAsignado=_espacio.Text.Trim(), Observaciones=_observaciones.Text.Trim() }; DialogResult=DialogResult.OK; }
}

internal sealed class FormEvolucionHospitalaria : Form
{
    private readonly NumericUpDown _temperatura; private readonly NumericUpDown _peso; private readonly NumericUpDown _fc; private readonly NumericUpDown _fr; private readonly TextBox _observaciones; private readonly TextBox _medicacion; private readonly TextBox _alimentacion; private readonly TextBox _incidencias; private readonly DateTimePicker _fecha;
    public HospitalizacionEvolucionModel Resultado { get; private set; }=new();
    public FormEvolucionHospitalaria(HospitalizacionModel h)
    {
        UiTheme.PrepararFormulario(this); Text="Registrar evolución"; Width=760; Height=595; FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; MinimizeBox=false; ShowInTaskbar=false;
        Controls.Add(new Label { Text=$"Evolución - {h.Mascota}", Font=UiTheme.FuenteTitulo, Location=new Point(22,18), AutoSize=true });
        Controls.Add(new Label { Text="Fecha y hora", Location=new Point(22,76), AutoSize=true }); _fecha=new DateTimePicker { Location=new Point(150,72), Width=210, Format=DateTimePickerFormat.Custom, CustomFormat="dd/MM/yyyy HH:mm" }; Controls.Add(_fecha);
        _temperatura=Numero(150,112,45); Etiqueta("Temperatura °C",22,116); _peso=Numero(510,112,500); Etiqueta("Peso kg",400,116); _fc=Numero(150,152,500); Etiqueta("Frec. cardiaca",22,156); _fr=Numero(510,152,500); Etiqueta("Frec. respiratoria",400,156);
        _observaciones=Texto(150,197,555,62); Etiqueta("Observaciones",22,201); _medicacion=Texto(150,276,555,62); Etiqueta("Medicación",22,280); _alimentacion=Texto(150,355,555,55); Etiqueta("Alimentación",22,359); _incidencias=Texto(150,427,555,55); Etiqueta("Incidencias",22,431);
        Button guardar=UiTheme.CrearBoton("Guardar",true); guardar.Location=new Point(595,512); guardar.Width=110; guardar.Click += (_,_)=> { Resultado=new HospitalizacionEvolucionModel { IdHospitalizacion=h.IdHospitalizacion, FechaHora=_fecha.Value, Temperatura=_temperatura.Value==0?null:_temperatura.Value, Peso=_peso.Value==0?null:_peso.Value, FrecuenciaCardiaca=_fc.Value==0?null:(int)_fc.Value, FrecuenciaRespiratoria=_fr.Value==0?null:(int)_fr.Value, Observaciones=_observaciones.Text.Trim(), MedicacionAdministrada=_medicacion.Text.Trim(), Alimentacion=_alimentacion.Text.Trim(), Incidencias=_incidencias.Text.Trim() }; DialogResult=DialogResult.OK; }; Controls.Add(guardar);
        Button cancelar=UiTheme.CrearBoton("Cancelar"); cancelar.Location=new Point(475,512); cancelar.Width=110; cancelar.DialogResult=DialogResult.Cancel; Controls.Add(cancelar); CancelButton=cancelar;
    }
    private void Etiqueta(string t,int x,int y)=>Controls.Add(new Label { Text=t, Location=new Point(x,y), AutoSize=true });
    private NumericUpDown Numero(int x,int y,decimal max) { NumericUpDown n=new() { Location=new Point(x,y), Width=150, DecimalPlaces=2, Maximum=max }; Controls.Add(n); return n; }
    private TextBox Texto(int x,int y,int w,int h) { TextBox t=new() { Location=new Point(x,y), Width=w, Height=h, Multiline=true }; Controls.Add(t); return t; }
}

internal sealed class FormCargoHospitalario : Form
{
    private readonly ComboBox _tipo; private readonly TextBox _descripcion; private readonly NumericUpDown _cantidad; private readonly NumericUpDown _precio; private readonly NumericUpDown _descuento; private readonly Label _total;
    public CargoHospitalizacionModel Resultado { get; private set; }=new();
    public FormCargoHospitalario(HospitalizacionModel h)
    {
        UiTheme.PrepararFormulario(this); Text="Agregar cargo"; Width=570; Height=365; FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; MinimizeBox=false; ShowInTaskbar=false;
        Controls.Add(new Label { Text=$"Cargo - {h.Mascota}", Font=UiTheme.FuenteTitulo, Location=new Point(20,18), AutoSize=true }); Etiqueta("Tipo",22,78); _tipo=new ComboBox { Location=new Point(174,74), Width=285, DropDownStyle=ComboBoxStyle.DropDownList }; _tipo.Items.AddRange(new object[] { "Hospitalización", "Medicamento", "Servicio", "Laboratorio", "Otro" }); _tipo.SelectedIndex=0; Controls.Add(_tipo);
        Etiqueta("Descripción *",22,119); _descripcion=new TextBox { Location=new Point(174,115), Width=330 }; Controls.Add(_descripcion); Etiqueta("Cantidad",22,160); _cantidad=Num(174,156,1000,2,1); Etiqueta("Precio unitario",286,160); _precio=Num(410,156,1000000,2,0); Etiqueta("Descuento",22,201); _descuento=Num(174,197,1000000,2,0); Etiqueta("Subtotal",286,201); _total=new Label { Text="$0.00", Location=new Point(410,201), AutoSize=true, Font=UiTheme.FuenteSubtitulo, ForeColor=UiTheme.Primario }; Controls.Add(_total); _cantidad.ValueChanged += (_,_)=>Calcular(); _precio.ValueChanged += (_,_)=>Calcular(); _descuento.ValueChanged += (_,_)=>Calcular();
        Button guardar=UiTheme.CrearBoton("Crear cargo",true); guardar.Location=new Point(394,262); guardar.Width=110; guardar.Click += (_,_)=> { Resultado=new CargoHospitalizacionModel { TipoItem=_tipo.Text, Descripcion=_descripcion.Text.Trim(), Cantidad=_cantidad.Value, PrecioUnitario=_precio.Value, Descuento=_descuento.Value }; DialogResult=DialogResult.OK; }; Controls.Add(guardar); Button cancelar=UiTheme.CrearBoton("Cancelar"); cancelar.Location=new Point(274,262); cancelar.Width=110; cancelar.DialogResult=DialogResult.Cancel; Controls.Add(cancelar); CancelButton=cancelar;
    }
    private void Etiqueta(string t,int x,int y)=>Controls.Add(new Label { Text=t, Location=new Point(x,y), AutoSize=true });
    private NumericUpDown Num(int x,int y,decimal max,int dec,decimal val) { NumericUpDown n=new() { Location=new Point(x,y), Width=100, Maximum=max, DecimalPlaces=dec, Value=val }; Controls.Add(n); return n; }
    private void Calcular()=>_total.Text=Math.Max(0,(_cantidad.Value*_precio.Value)-_descuento.Value).ToString("C2");
}

internal sealed class FormAltaHospitalaria : Form
{
    private readonly DateTimePicker _fecha; private readonly TextBox _observaciones; public DateTime FechaAlta=>_fecha.Value; public string Observaciones=>_observaciones.Text.Trim();
    public FormAltaHospitalaria(HospitalizacionModel h)
    {
        UiTheme.PrepararFormulario(this); Text="Alta hospitalaria"; Width=520; Height=300; FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; MinimizeBox=false; ShowInTaskbar=false;
        Controls.Add(new Label { Text=$"Alta - {h.Mascota}", Font=UiTheme.FuenteTitulo, Location=new Point(20,18), AutoSize=true }); Controls.Add(new Label { Text="Fecha y hora de alta", Location=new Point(22,77), AutoSize=true }); _fecha=new DateTimePicker { Location=new Point(175,73), Width=265, Format=DateTimePickerFormat.Custom, CustomFormat="dd/MM/yyyy HH:mm" }; Controls.Add(_fecha); Controls.Add(new Label { Text="Observaciones", Location=new Point(22,119), AutoSize=true }); _observaciones=new TextBox { Location=new Point(175,115), Width=290, Height=54, Multiline=true }; Controls.Add(_observaciones);
        Button alta=UiTheme.CrearBoton("Dar alta",true); alta.Location=new Point(355,202); alta.Width=110; alta.Click += (_,_)=>DialogResult=DialogResult.OK; Controls.Add(alta); Button cancelar=UiTheme.CrearBoton("Cancelar"); cancelar.Location=new Point(235,202); cancelar.Width=110; cancelar.DialogResult=DialogResult.Cancel; Controls.Add(cancelar); CancelButton=cancelar;
    }
}
