using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Services;
using ClinicaVeterinaria.Utils;

namespace ClinicaVeterinaria.Forms;

public sealed class FormOrdenesClinicas : Form
{
    private readonly OrdenClinicaService _servicio = new();
    private DataGridView _grid = null!;
    private DateTimePicker _desde = null!;
    private DateTimePicker _hasta = null!;
    private ComboBox _estado = null!;
    private TextBox _buscar = null!;
    private TextBox _resultado = null!;
    private Label _detalle = null!;

    public FormOrdenesClinicas()
    {
        UiTheme.PrepararFormulario(this);
        Dock = DockStyle.Fill;
        ConstruirInterfaz();
        Shown += (_, _) => CargarLista();
    }

    private void ConstruirInterfaz()
    {
        TableLayoutPanel raiz = new() { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, BackColor = UiTheme.Fondo };
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 64));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 36));
        Controls.Add(raiz);

        Panel encabezado = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20, 10, 16, 8) };
        Label titulo = UiTheme.CrearTitulo("Órdenes clínicas y resultados"); titulo.Location = new Point(20, 9); encabezado.Controls.Add(titulo);
        encabezado.Controls.Add(new Label { Text = "Laboratorios, imágenes, estudios y carga de resultados clínicos.", Location = new Point(22, 43), AutoSize = true, ForeColor = UiTheme.TextoSecundario });
        Button actualizar = UiTheme.CrearBoton("Actualizar", true); actualizar.Dock = DockStyle.Right; actualizar.Width = 110; actualizar.Click += (_, _) => CargarLista(); encabezado.Controls.Add(actualizar);
        raiz.Controls.Add(encabezado, 0, 0);

        FlowLayoutPanel filtros = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(15, 10, 10, 8), WrapContents = false };
        filtros.Controls.Add(new Label { Text = "Desde", AutoSize = true, Margin = new Padding(0, 10, 6, 0) });
        _desde = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1), Margin = new Padding(0, 5, 14, 0) }; filtros.Controls.Add(_desde);
        filtros.Controls.Add(new Label { Text = "Hasta", AutoSize = true, Margin = new Padding(0, 10, 6, 0) });
        _hasta = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(1), Margin = new Padding(0, 5, 14, 0) }; filtros.Controls.Add(_hasta);
        _estado = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, Margin = new Padding(0, 6, 12, 0) };
        _estado.Items.AddRange(new object[] { "Todos", "Solicitada", "En proceso", "Resultado recibido", "Cancelada" }); _estado.SelectedIndex = 0; filtros.Controls.Add(_estado);
        _buscar = new TextBox { Width = 250, PlaceholderText = "Paciente, dueño o estudio", Margin = new Padding(0, 6, 10, 0) }; filtros.Controls.Add(_buscar);
        Button buscar = UiTheme.CrearBoton("Buscar", true); buscar.Width = 82; buscar.Click += (_, _) => CargarLista(); filtros.Controls.Add(buscar);
        Button nueva = UiTheme.CrearBoton("Nueva orden", true); nueva.Width = 120; nueva.Click += (_, _) => NuevaOrden(); filtros.Controls.Add(nueva);
        raiz.Controls.Add(filtros, 0, 1);

        Panel tabla = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(14, 10, 14, 5) };
        _grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false }; UiTheme.PrepararGrid(_grid);
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FechaSolicitud", HeaderText = "Fecha", FillWeight = 17, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CodigoPaciente", HeaderText = "Código", FillWeight = 16 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Mascota", HeaderText = "Paciente", FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Dueno", HeaderText = "Dueño", FillWeight = 24 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TipoOrden", HeaderText = "Tipo", FillWeight = 17 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NombreEstudio", HeaderText = "Estudio", FillWeight = 29 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Estado", HeaderText = "Estado", FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Precio", HeaderText = "Precio", FillWeight = 14, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
        _grid.SelectionChanged += (_, _) => MostrarDetalle();
        tabla.Controls.Add(_grid); raiz.Controls.Add(tabla, 0, 2);

        TableLayoutPanel inferior = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(14, 5, 14, 10), BackColor = Color.White };
        inferior.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58)); inferior.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        Panel info = new() { Dock = DockStyle.Fill, Padding = new Padding(12), BorderStyle = BorderStyle.FixedSingle };
        _detalle = new Label { Dock = DockStyle.Top, Height = 48, Font = UiTheme.FuenteSubtitulo, ForeColor = UiTheme.Primario, Text = "Seleccione una orden." };
        _resultado = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
        info.Controls.Add(_resultado); info.Controls.Add(_detalle); inferior.Controls.Add(info, 0, 0);
        FlowLayoutPanel acciones = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(10, 18, 0, 0) };
        Button proceso = UiTheme.CrearBoton("En proceso", true); proceso.Width = 110; proceso.Click += (_, _) => MarcarProceso();
        Button resultado = UiTheme.CrearBoton("Registrar resultado", true); resultado.Width = 155; resultado.Click += (_, _) => RegistrarResultado();
        Button archivo = UiTheme.CrearBoton("Abrir archivo"); archivo.Width = 115; archivo.Click += (_, _) => AbrirArchivo();
        Button cancelar = UiTheme.CrearBoton("Cancelar orden"); cancelar.Width = 130; cancelar.Click += (_, _) => Cancelar();
        acciones.Controls.AddRange(new Control[] { proceso, resultado, archivo, cancelar }); inferior.Controls.Add(acciones, 1, 0);
        raiz.Controls.Add(inferior, 0, 3);
    }

    private OrdenClinicaModel? Seleccionada => _grid.CurrentRow?.DataBoundItem as OrdenClinicaModel;

    private void CargarLista()
    {
        try
        {
            if (_desde.Value.Date > _hasta.Value.Date) throw new InvalidOperationException("La fecha inicial no puede ser posterior a la final.");
            _grid.DataSource = _servicio.Listar(_desde.Value, _hasta.Value, _estado.Text, _buscar.Text);
            MostrarDetalle();
        }
        catch (Exception ex) { MostrarError(ex); }
    }

    private void MostrarDetalle()
    {
        OrdenClinicaModel? orden = Seleccionada;
        if (orden is null) { _detalle.Text = "Seleccione una orden."; _resultado.Text = string.Empty; return; }
        _detalle.Text = $"{orden.Mascota} · {orden.NombreEstudio} · {orden.Estado}   |   Veterinario: {orden.Veterinario}";
        _resultado.Text = string.IsNullOrWhiteSpace(orden.ResultadoTexto)
            ? $"Motivo: {orden.Motivo}\r\nObservaciones: {orden.Observaciones}\r\nArchivo: {orden.RutaArchivo}"
            : $"RESULTADO ({orden.FechaResultado:dd/MM/yyyy HH:mm})\r\n{orden.ResultadoTexto}\r\n\r\nArchivo: {orden.RutaArchivo}";
    }

    private void NuevaOrden()
    {
        using FormNuevaOrdenClinica dialogo = new(_servicio);
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        try { _servicio.Crear(dialogo.Resultado); CargarLista(); MessageBox.Show("Orden clínica creada correctamente.", "Órdenes clínicas", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { MostrarError(ex); }
    }

    private void MarcarProceso()
    {
        if (Seleccionada is not OrdenClinicaModel orden) return;
        try { _servicio.MarcarEnProceso(orden.IdOrden); CargarLista(); }
        catch (Exception ex) { MostrarError(ex); }
    }

    private void RegistrarResultado()
    {
        if (Seleccionada is not OrdenClinicaModel orden) return;
        using FormResultadoOrden dialogo = new(orden);
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        try { _servicio.RegistrarResultado(orden.IdOrden, dialogo.Resultado, dialogo.RutaArchivo); CargarLista(); MessageBox.Show("Resultado registrado correctamente.", "Órdenes clínicas", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { MostrarError(ex); }
    }

    private void Cancelar()
    {
        if (Seleccionada is not OrdenClinicaModel orden) return;
        using FormNotaSimple dialogo = new("Cancelar orden", "Motivo de cancelación *");
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        try { _servicio.Cancelar(orden.IdOrden, dialogo.Texto); CargarLista(); }
        catch (Exception ex) { MostrarError(ex); }
    }

    private void AbrirArchivo()
    {
        OrdenClinicaModel? orden = Seleccionada;
        if (orden is null || string.IsNullOrWhiteSpace(orden.RutaArchivo)) { MessageBox.Show("La orden no tiene archivo adjunto registrado.", "Órdenes clínicas"); return; }
        if (!File.Exists(orden.RutaArchivo)) { MessageBox.Show("El archivo ya no existe en la ruta registrada.", "Órdenes clínicas", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        Process.Start(new ProcessStartInfo(orden.RutaArchivo) { UseShellExecute = true });
    }

    private static void MostrarError(Exception ex) => MessageBox.Show(ex.Message, "Órdenes clínicas", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}

internal sealed class FormNuevaOrdenClinica : Form
{
    private readonly OrdenClinicaService _servicio;
    private TextBox _filtro = null!;
    private ComboBox _consulta = null!;
    private ComboBox _tipo = null!;
    private TextBox _estudio = null!;
    private TextBox _motivo = null!;
    private TextBox _observaciones = null!;
    private NumericUpDown _precio = null!;
    public OrdenClinicaModel Resultado { get; private set; } = new();

    public FormNuevaOrdenClinica(OrdenClinicaService servicio)
    {
        _servicio = servicio; UiTheme.PrepararFormulario(this); Text = "Nueva orden clínica"; Width = 670; Height = 540;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false; Construir(); CargarConsultas();
    }

    private void Construir()
    {
        Controls.Add(new Label { Text = "Nueva orden clínica", Font = UiTheme.FuenteTitulo, Location = new Point(22, 18), AutoSize = true });
        Controls.Add(new Label { Text = "Buscar consulta", Location = new Point(24, 73), AutoSize = true });
        _filtro = new TextBox { Location = new Point(165, 69), Width = 270, PlaceholderText = "Paciente o dueño" }; Controls.Add(_filtro);
        Button buscar = UiTheme.CrearBoton("Buscar", true); buscar.Location = new Point(445, 64); buscar.Width = 90; buscar.Click += (_, _) => CargarConsultas(); Controls.Add(buscar);
        Controls.Add(new Label { Text = "Consulta *", Location = new Point(24, 116), AutoSize = true });
        _consulta = new ComboBox { Location = new Point(165, 112), Width = 458, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Descripcion", ValueMember = "IdConsulta" }; Controls.Add(_consulta);
        Controls.Add(new Label { Text = "Tipo *", Location = new Point(24, 157), AutoSize = true });
        _tipo = new ComboBox { Location = new Point(165, 153), Width = 185, DropDownStyle = ComboBoxStyle.DropDownList }; _tipo.Items.AddRange(new object[] { "Laboratorio", "Imagen", "Otro estudio" }); _tipo.SelectedIndex = 0; Controls.Add(_tipo);
        Controls.Add(new Label { Text = "Precio", Location = new Point(377, 157), AutoSize = true });
        _precio = new NumericUpDown { Location = new Point(455, 153), Width = 168, DecimalPlaces = 2, Maximum = 1000000 }; Controls.Add(_precio);
        Controls.Add(new Label { Text = "Estudio *", Location = new Point(24, 198), AutoSize = true });
        _estudio = new TextBox { Location = new Point(165, 194), Width = 458 }; Controls.Add(_estudio);
        Controls.Add(new Label { Text = "Motivo", Location = new Point(24, 239), AutoSize = true });
        _motivo = new TextBox { Location = new Point(165, 235), Width = 458, Height = 60, Multiline = true }; Controls.Add(_motivo);
        Controls.Add(new Label { Text = "Observaciones", Location = new Point(24, 313), AutoSize = true });
        _observaciones = new TextBox { Location = new Point(165, 309), Width = 458, Height = 70, Multiline = true }; Controls.Add(_observaciones);
        Button guardar = UiTheme.CrearBoton("Guardar", true); guardar.Width = 110; guardar.Location = new Point(513, 423); guardar.Click += (_, _) => Confirmar(); Controls.Add(guardar);
        Button cancelar = UiTheme.CrearBoton("Cancelar"); cancelar.Width = 110; cancelar.Location = new Point(393, 423); cancelar.DialogResult = DialogResult.Cancel; Controls.Add(cancelar); CancelButton = cancelar;
    }
    private void CargarConsultas() { try { _consulta.DataSource = _servicio.ListarConsultas(_filtro.Text); } catch (Exception ex) { MessageBox.Show(ex.Message); } }
    private void Confirmar()
    {
        if (_consulta.SelectedItem is not ConsultaDisponibleOrdenModel consulta) { MessageBox.Show("Seleccione una consulta clínica."); return; }
        Resultado = new OrdenClinicaModel { IdConsulta = consulta.IdConsulta, TipoOrden = _tipo.Text, NombreEstudio = _estudio.Text.Trim(), Motivo = _motivo.Text.Trim(), Observaciones = _observaciones.Text.Trim(), Precio = _precio.Value };
        DialogResult = DialogResult.OK;
    }
}

internal sealed class FormResultadoOrden : Form
{
    private readonly TextBox _resultado;
    private readonly TextBox _ruta;
    public string Resultado => _resultado.Text.Trim();
    public string RutaArchivo => _ruta.Text.Trim();
    public FormResultadoOrden(OrdenClinicaModel orden)
    {
        UiTheme.PrepararFormulario(this); Text = "Registrar resultado"; Width = 650; Height = 420; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
        Controls.Add(new Label { Text = orden.NombreEstudio, Font = UiTheme.FuenteTitulo, Location = new Point(20, 18), AutoSize = true });
        Controls.Add(new Label { Text = "Resultado textual *", Location = new Point(22, 72), AutoSize = true });
        _resultado = new TextBox { Location = new Point(22, 98), Width = 588, Height = 140, Multiline = true, ScrollBars = ScrollBars.Vertical, Text = orden.ResultadoTexto }; Controls.Add(_resultado);
        Controls.Add(new Label { Text = "Archivo local opcional", Location = new Point(22, 255), AutoSize = true });
        _ruta = new TextBox { Location = new Point(22, 280), Width = 465, Text = orden.RutaArchivo }; Controls.Add(_ruta);
        Button examinar = UiTheme.CrearBoton("Examinar"); examinar.Location = new Point(495, 274); examinar.Width = 115; examinar.Click += (_, _) => Examinar(); Controls.Add(examinar);
        Button guardar = UiTheme.CrearBoton("Guardar", true); guardar.Location = new Point(500, 326); guardar.Width = 110; guardar.Click += (_, _) => { if (string.IsNullOrWhiteSpace(Resultado)) { MessageBox.Show("El resultado textual es obligatorio."); return; } DialogResult = DialogResult.OK; }; Controls.Add(guardar);
        Button cancelar = UiTheme.CrearBoton("Cancelar"); cancelar.Location = new Point(380, 326); cancelar.Width = 110; cancelar.DialogResult = DialogResult.Cancel; Controls.Add(cancelar); CancelButton = cancelar;
    }
    private void Examinar() { using OpenFileDialog d = new() { Filter = "Todos los archivos (*.*)|*.*|PDF (*.pdf)|*.pdf|Imágenes (*.png;*.jpg)|*.png;*.jpg" }; if (d.ShowDialog(this) == DialogResult.OK) _ruta.Text = d.FileName; }
}

internal sealed class FormNotaSimple : Form
{
    private readonly TextBox _texto;
    public string Texto => _texto.Text.Trim();
    public FormNotaSimple(string titulo, string etiqueta)
    {
        UiTheme.PrepararFormulario(this); Text = titulo; Width = 475; Height = 245; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
        Controls.Add(new Label { Text = titulo, Font = UiTheme.FuenteTitulo, Location = new Point(20, 16), AutoSize = true });
        Controls.Add(new Label { Text = etiqueta, Location = new Point(22, 68), AutoSize = true });
        _texto = new TextBox { Location = new Point(22, 92), Width = 415, Height = 55, Multiline = true }; Controls.Add(_texto);
        Button ok = UiTheme.CrearBoton("Confirmar", true); ok.Location = new Point(327, 160); ok.Width = 110; ok.Click += (_, _) => { if (string.IsNullOrWhiteSpace(Texto)) { MessageBox.Show("Debe registrar una observación."); return; } DialogResult = DialogResult.OK; }; Controls.Add(ok);
        Button cancelar = UiTheme.CrearBoton("Cancelar"); cancelar.Location = new Point(210, 160); cancelar.Width = 106; cancelar.DialogResult = DialogResult.Cancel; Controls.Add(cancelar); CancelButton = cancelar;
    }
}
