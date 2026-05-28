using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Services;
using ClinicaVeterinaria.Utils;

namespace ClinicaVeterinaria.Forms;

public sealed class FormRecordatorios : Form
{
    private readonly RecordatorioService _servicio = new();
    private readonly Action<long>? _crearCita;
    private DataGridView _grid = null!;
    private DateTimePicker _desde = null!;
    private DateTimePicker _hasta = null!;
    private ComboBox _tipo = null!;
    private ComboBox _estado = null!;
    private TextBox _buscar = null!;
    private Label _hoy = null!;
    private Label _proximos = null!;
    private Label _vencidos = null!;
    private Label _contactados = null!;
    private Label _completados = null!;

    public FormRecordatorios(Action<long>? crearCita = null)
    {
        _crearCita = crearCita;
        UiTheme.PrepararFormulario(this);
        Dock = DockStyle.Fill;
        ConstruirInterfaz();
        Shown += (_, _) => CargarTodo();
    }

    private void ConstruirInterfaz()
    {
        TableLayoutPanel raiz = new() { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, BackColor = UiTheme.Fondo };
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(raiz);

        Panel cabecera = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20, 10, 18, 8) };
        Label titulo = UiTheme.CrearTitulo("Recordatorios y seguimiento"); titulo.Location = new Point(20, 10); cabecera.Controls.Add(titulo);
        cabecera.Controls.Add(new Label { Text = "Vacunas, desparasitaciones, revisiones y seguimientos pendientes.", AutoSize = true, ForeColor = UiTheme.TextoSecundario, Location = new Point(22, 41) });
        Button actualizar = UiTheme.CrearBoton("Actualizar", true); actualizar.Width = 108; actualizar.Dock = DockStyle.Right; actualizar.Click += (_, _) => CargarTodo(); cabecera.Controls.Add(actualizar);
        raiz.Controls.Add(cabecera, 0, 0);

        TableLayoutPanel tarjetas = new() { Dock = DockStyle.Fill, ColumnCount = 5, Padding = new Padding(0, 8, 0, 8) };
        for (int i = 0; i < 5; i++) tarjetas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        _hoy = CrearTarjeta(tarjetas, "Programados para hoy", 0);
        _proximos = CrearTarjeta(tarjetas, "Próximos 7 días", 1);
        _vencidos = CrearTarjeta(tarjetas, "Vencidos", 2, UiTheme.Peligro);
        _contactados = CrearTarjeta(tarjetas, "Contactados", 3);
        _completados = CrearTarjeta(tarjetas, "Completados", 4);
        raiz.Controls.Add(tarjetas, 0, 1);

        FlowLayoutPanel filtros = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(14, 11, 12, 8), FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        filtros.Controls.Add(new Label { Text = "Desde", AutoSize = true, Margin = new Padding(0, 10, 5, 0) });
        _desde = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-7), Margin = new Padding(0, 5, 14, 0) };
        filtros.Controls.Add(_desde);
        filtros.Controls.Add(new Label { Text = "Hasta", AutoSize = true, Margin = new Padding(0, 10, 5, 0) });
        _hasta = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(30), Margin = new Padding(0, 5, 14, 0) };
        filtros.Controls.Add(_hasta);
        _tipo = CrearCombo(new[] { "Todos", "Vacuna", "Desparasitación", "Revisión clínica", "Cita por confirmar", "Control posoperatorio", "Manual" }, 160); filtros.Controls.Add(_tipo);
        _estado = CrearCombo(new[] { "Todos", "Pendiente", "Contactado", "Pospuesto", "Completado", "Cancelado" }, 140); filtros.Controls.Add(_estado);
        _buscar = new TextBox { Width = 230, PlaceholderText = "Paciente, dueño o teléfono", Margin = new Padding(8, 6, 8, 0) }; filtros.Controls.Add(_buscar);
        Button buscar = UiTheme.CrearBoton("Buscar", true); buscar.Width = 82; buscar.Click += (_, _) => CargarLista(); filtros.Controls.Add(buscar);
        Button nuevo = UiTheme.CrearBoton("Nuevo", true); nuevo.Width = 82; nuevo.Click += (_, _) => NuevoRecordatorio(); filtros.Controls.Add(nuevo);
        raiz.Controls.Add(filtros, 0, 2);

        TableLayoutPanel cuerpo = new() { Dock = DockStyle.Fill, BackColor = Color.White, RowCount = 2, Padding = new Padding(14, 10, 14, 10) };
        cuerpo.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); cuerpo.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        _grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false }; UiTheme.PrepararGrid(_grid);
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FechaProgramada", HeaderText = "Fecha", FillWeight = 14, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TipoRecordatorio", HeaderText = "Tipo", FillWeight = 20 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CodigoPaciente", HeaderText = "Código", FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Mascota", HeaderText = "Paciente", FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Dueno", HeaderText = "Dueño", FillWeight = 26 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Telefono", HeaderText = "Teléfono", FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Descripcion", HeaderText = "Descripción", FillWeight = 45 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Estado", HeaderText = "Estado", FillWeight = 16 });
        _grid.CellFormatting += (_, e) => { if (e.RowIndex >= 0 && _grid.Rows[e.RowIndex].DataBoundItem is RecordatorioModel r && r.Vencido) _grid.Rows[e.RowIndex].DefaultCellStyle.ForeColor = UiTheme.Peligro; };
        cuerpo.Controls.Add(_grid, 0, 0);

        FlowLayoutPanel acciones = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 6, 0, 0) };
        Button cita = UiTheme.CrearBoton("Crear cita", true); cita.Width = 105; cita.Click += (_, _) => CrearCita();
        Button contacto = UiTheme.CrearBoton("Marcar contactado"); contacto.Width = 145; contacto.Click += (_, _) => Contactado();
        Button posponer = UiTheme.CrearBoton("Posponer"); posponer.Width = 100; posponer.Click += (_, _) => Posponer();
        Button completar = UiTheme.CrearBoton("Completar"); completar.Width = 105; completar.Click += (_, _) => Completar();
        Button cancelar = UiTheme.CrearBoton("Cancelar"); cancelar.Width = 100; cancelar.Click += (_, _) => Cancelar();
        Button copiar = UiTheme.CrearBoton("Copiar texto"); copiar.Width = 112; copiar.Click += (_, _) => CopiarTexto();
        acciones.Controls.AddRange(new Control[] { cita, contacto, posponer, completar, cancelar, copiar });
        cuerpo.Controls.Add(acciones, 0, 1);
        raiz.Controls.Add(cuerpo, 0, 3);
    }

    private static ComboBox CrearCombo(IEnumerable<string> elementos, int ancho)
    {
        ComboBox combo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = ancho, Margin = new Padding(0, 6, 8, 0) };
        combo.Items.AddRange(elementos.Cast<object>().ToArray()); combo.SelectedIndex = 0; return combo;
    }

    private static Label CrearTarjeta(TableLayoutPanel tabla, string titulo, int columna, Color? color = null)
    {
        Panel panel = new() { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(5), Padding = new Padding(14, 8, 14, 8) };
        panel.Controls.Add(new Label { Text = titulo, Dock = DockStyle.Top, Height = 24, ForeColor = UiTheme.TextoSecundario });
        Label valor = new() { Text = "0", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold), ForeColor = color ?? UiTheme.Primario };
        panel.Controls.Add(valor); valor.BringToFront(); tabla.Controls.Add(panel, columna, 0); return valor;
    }

    private void CargarTodo()
    {
        try
        {
            ResumenRecordatoriosModel r = _servicio.ObtenerResumen();
            _hoy.Text = r.PendientesHoy.ToString(); _proximos.Text = r.ProximosSieteDias.ToString(); _vencidos.Text = r.Vencidos.ToString(); _contactados.Text = r.Contactados.ToString(); _completados.Text = r.Completados.ToString();
            CargarLista();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Recordatorios", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void CargarLista()
    {
        if (_hasta.Value.Date < _desde.Value.Date) { MessageBox.Show("La fecha final no puede ser menor que la inicial."); return; }
        string tipo = _tipo.SelectedIndex <= 0 ? string.Empty : _tipo.Text;
        string estado = _estado.SelectedIndex <= 0 ? string.Empty : _estado.Text;
        _grid.DataSource = _servicio.Listar(_desde.Value, _hasta.Value, tipo, estado, _buscar.Text);
    }

    private RecordatorioModel? Seleccionado() => _grid.CurrentRow?.DataBoundItem as RecordatorioModel;

    private void NuevoRecordatorio()
    {
        using FormNuevoRecordatorio dialogo = new(_servicio);
        if (dialogo.ShowDialog(this) == DialogResult.OK) CargarTodo();
    }

    private void CrearCita()
    {
        RecordatorioModel? r = Seleccionado(); if (r is null) { AvisoSeleccion(); return; }
        if (_crearCita is null) { MessageBox.Show("La navegación a Agenda no está configurada."); return; }
        _crearCita(r.IdMascota);
    }

    private void Contactado()
    {
        RecordatorioModel? r = Seleccionado(); if (r is null) { AvisoSeleccion(); return; }
        using FormTextoSimple dialogo = new("Marcar contactado", "Nota del contacto *");
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        try { _servicio.MarcarContactado(r.IdRecordatorio, dialogo.Texto); CargarTodo(); } catch (Exception ex) { MostrarError(ex); }
    }

    private void Posponer()
    {
        RecordatorioModel? r = Seleccionado(); if (r is null) { AvisoSeleccion(); return; }
        using FormPosponerRecordatorio dialogo = new();
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        try { _servicio.Posponer(r.IdRecordatorio, dialogo.Fecha, dialogo.Observacion); CargarTodo(); } catch (Exception ex) { MostrarError(ex); }
    }

    private void Completar()
    {
        RecordatorioModel? r = Seleccionado(); if (r is null) { AvisoSeleccion(); return; }
        using FormTextoSimple dialogo = new("Completar recordatorio", "Nota final");
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        try { _servicio.MarcarCompletado(r.IdRecordatorio, dialogo.Texto); CargarTodo(); } catch (Exception ex) { MostrarError(ex); }
    }

    private void Cancelar()
    {
        RecordatorioModel? r = Seleccionado(); if (r is null) { AvisoSeleccion(); return; }
        using FormTextoSimple dialogo = new("Cancelar recordatorio", "Motivo de cancelación *");
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        try { _servicio.Cancelar(r.IdRecordatorio, dialogo.Texto); CargarTodo(); } catch (Exception ex) { MostrarError(ex); }
    }

    private void CopiarTexto()
    {
        RecordatorioModel? r = Seleccionado(); if (r is null) { AvisoSeleccion(); return; }
        StringBuilder texto = new(); texto.AppendLine($"Clínica Veterinaria - Recordatorio de {r.TipoRecordatorio}"); texto.AppendLine($"Paciente: {r.Mascota} ({r.CodigoPaciente})"); texto.AppendLine($"Propietario: {r.Dueno}"); texto.AppendLine($"Fecha: {r.FechaProgramada:dd/MM/yyyy}"); texto.AppendLine(r.Descripcion);
        Clipboard.SetText(texto.ToString()); MessageBox.Show("Texto copiado al portapapeles.", "Recordatorios", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void AvisoSeleccion() => MessageBox.Show("Seleccione un recordatorio.", "Recordatorios", MessageBoxButtons.OK, MessageBoxIcon.Information);
    private static void MostrarError(Exception ex) => MessageBox.Show(ex.Message, "Recordatorios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}

internal sealed class FormNuevoRecordatorio : Form
{
    private readonly RecordatorioService _servicio;
    private TextBox _buscar = null!; private ComboBox _mascota = null!; private ComboBox _tipo = null!; private DateTimePicker _fecha = null!; private TextBox _descripcion = null!;
    public FormNuevoRecordatorio(RecordatorioService servicio) { _servicio = servicio; UiTheme.PrepararFormulario(this); Construir(); }
    private void Construir()
    {
        Text = "Nuevo recordatorio"; Width = 640; Height = 480; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
        Controls.Add(new Label { Text = "Nuevo recordatorio", AutoSize = true, Font = UiTheme.FuenteTitulo, Location = new Point(24, 20) });
        AgregarEtiqueta("Buscar paciente", 76); _buscar = new TextBox { Location = new Point(180, 72), Width = 285, PlaceholderText = "Nombre, código o dueño" }; Controls.Add(_buscar);
        Button buscar = UiTheme.CrearBoton("Buscar", true); buscar.Location = new Point(474, 70); buscar.Width = 100; buscar.Click += (_, _) => CargarMascotas(); Controls.Add(buscar);
        AgregarEtiqueta("Paciente *", 121); _mascota = new ComboBox { Location = new Point(180, 116), Width = 394, DropDownStyle = ComboBoxStyle.DropDownList }; Controls.Add(_mascota);
        AgregarEtiqueta("Tipo *", 164); _tipo = new ComboBox { Location = new Point(180, 159), Width = 224, DropDownStyle = ComboBoxStyle.DropDownList }; _tipo.Items.AddRange(new object[] { "Vacuna", "Desparasitación", "Revisión clínica", "Cita por confirmar", "Control posoperatorio", "Manual" }); _tipo.SelectedIndex = 5; Controls.Add(_tipo);
        AgregarEtiqueta("Fecha *", 207); _fecha = new DateTimePicker { Location = new Point(180, 202), Width = 224, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(1) }; Controls.Add(_fecha);
        AgregarEtiqueta("Descripción *", 251); _descripcion = new TextBox { Location = new Point(180, 246), Width = 394, Height = 86, Multiline = true }; Controls.Add(_descripcion);
        Button guardar = UiTheme.CrearBoton("Guardar", true); guardar.Location = new Point(464, 370); guardar.Width = 110; guardar.Click += (_, _) => Guardar(); Controls.Add(guardar);
        Button cancelar = UiTheme.CrearBoton("Cancelar"); cancelar.Location = new Point(346, 370); cancelar.Width = 110; cancelar.DialogResult = DialogResult.Cancel; Controls.Add(cancelar); CancelButton = cancelar;
    }
    private void AgregarEtiqueta(string texto, int y) => Controls.Add(new Label { Text = texto, AutoSize = true, Location = new Point(28, y + 5) });
    private void CargarMascotas() { try { _mascota.DataSource = _servicio.BuscarMascotas(_buscar.Text); _mascota.DisplayMember = "Descripcion"; _mascota.ValueMember = "IdMascota"; } catch (Exception ex) { MessageBox.Show(ex.Message); } }
    private void Guardar()
    {
        if (_mascota.SelectedItem is not MascotaBusquedaModel mascota) { MessageBox.Show("Busque y seleccione un paciente."); return; }
        try { _servicio.Crear(new NuevoRecordatorioModel { IdMascota = mascota.IdMascota, TipoRecordatorio = _tipo.Text, FechaProgramada = _fecha.Value, Descripcion = _descripcion.Text }); DialogResult = DialogResult.OK; Close(); } catch (Exception ex) { MessageBox.Show(ex.Message, "Recordatorio", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
}

internal sealed class FormTextoSimple : Form
{
    private readonly TextBox _texto; public string Texto => _texto.Text.Trim();
    public FormTextoSimple(string titulo, string etiqueta)
    {
        UiTheme.PrepararFormulario(this); Text = titulo; Width = 520; Height = 260; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
        Controls.Add(new Label { Text = titulo, AutoSize = true, Font = UiTheme.FuenteTitulo, Location = new Point(22, 18) }); Controls.Add(new Label { Text = etiqueta, AutoSize = true, Location = new Point(24, 62) });
        _texto = new TextBox { Location = new Point(24, 87), Width = 450, Height = 64, Multiline = true }; Controls.Add(_texto);
        Button aceptar = UiTheme.CrearBoton("Aceptar", true); aceptar.Width = 102; aceptar.Location = new Point(372, 171); aceptar.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); }; Controls.Add(aceptar);
        Button cancelar = UiTheme.CrearBoton("Cancelar"); cancelar.Width = 102; cancelar.Location = new Point(261, 171); cancelar.DialogResult = DialogResult.Cancel; Controls.Add(cancelar); CancelButton = cancelar;
    }
}

internal sealed class FormPosponerRecordatorio : Form
{
    private readonly DateTimePicker _fecha; private readonly TextBox _nota; public DateTime Fecha => _fecha.Value.Date; public string Observacion => _nota.Text.Trim();
    public FormPosponerRecordatorio()
    {
        UiTheme.PrepararFormulario(this); Text = "Posponer recordatorio"; Width = 520; Height = 290; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
        Controls.Add(new Label { Text = "Posponer recordatorio", AutoSize = true, Font = UiTheme.FuenteTitulo, Location = new Point(22, 18) });
        Controls.Add(new Label { Text = "Nueva fecha *", AutoSize = true, Location = new Point(24, 66) }); _fecha = new DateTimePicker { Location = new Point(150, 62), Width = 176, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(7) }; Controls.Add(_fecha);
        Controls.Add(new Label { Text = "Motivo *", AutoSize = true, Location = new Point(24, 106) }); _nota = new TextBox { Location = new Point(150, 102), Width = 324, Height = 65, Multiline = true }; Controls.Add(_nota);
        Button aceptar = UiTheme.CrearBoton("Guardar", true); aceptar.Width = 104; aceptar.Location = new Point(370, 194); aceptar.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); }; Controls.Add(aceptar);
        Button cancelar = UiTheme.CrearBoton("Cancelar"); cancelar.Width = 104; cancelar.Location = new Point(257, 194); cancelar.DialogResult = DialogResult.Cancel; Controls.Add(cancelar); CancelButton = cancelar;
    }
}
