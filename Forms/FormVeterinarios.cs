using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Services;
using ClinicaVeterinaria.Utils;

namespace ClinicaVeterinaria.Forms;

public sealed class FormVeterinarios : Form
{
    private readonly VeterinarioService _servicio = new();
    private readonly DataGridView _gridVeterinarios = new();
    private readonly DataGridView _gridHorarios = new();
    private readonly DataGridView _gridBloqueos = new();
    private readonly Label _lblSeleccion = new();
    private readonly DateTimePicker _desdeBloqueos = new();
    private VeterinarioModel? _seleccionado;

    public FormVeterinarios()
    {
        SesionActual.ExigirRoles("Administrador");
        ConstruirInterfaz();
        CargarVeterinarios();
    }

    private void ConstruirInterfaz()
    {
        UiTheme.PrepararFormulario(this);
        Padding = new Padding(10);
        TableLayoutPanel layout = new() { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = UiTheme.Fondo };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(layout);

        Panel cabecera = new() { Dock = DockStyle.Fill };
        Label titulo = UiTheme.CrearTitulo("Veterinarios y disponibilidad");
        titulo.Location = new Point(4, 5);
        cabecera.Controls.Add(titulo);
        cabecera.Controls.Add(new Label { Text = "Profesionales, jornadas semanales, almuerzos, vacaciones y ausencias.", Location = new Point(7, 41), AutoSize = true, ForeColor = UiTheme.TextoSecundario });
        layout.Controls.Add(cabecera, 0, 0);

        FlowLayoutPanel acciones = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10, 6, 10, 6) };
        Button nuevo = UiTheme.CrearBoton("Nuevo veterinario", true); nuevo.Width = 150; nuevo.Click += (_, _) => EditarVeterinario(null);
        Button editar = UiTheme.CrearBoton("Editar veterinario"); editar.Width = 150; editar.Click += (_, _) => EditarVeterinario(_seleccionado);
        Button refrescar = UiTheme.CrearBoton("Actualizar"); refrescar.Width = 105; refrescar.Click += (_, _) => CargarVeterinarios();
        acciones.Controls.AddRange(new Control[] { nuevo, editar, refrescar });
        layout.Controls.Add(acciones, 0, 1);

        TableLayoutPanel cuerpo = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 10, 0, 0) };
        cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        layout.Controls.Add(cuerpo, 0, 2);

        TableLayoutPanel panelVets = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12), Margin = new Padding(0, 0, 10, 0), RowCount = 2, ColumnCount = 1 };
        panelVets.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panelVets.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panelVets.Controls.Add(new Label { Text = "Profesionales registrados", Dock = DockStyle.Fill, Font = UiTheme.FuenteSubtitulo, ForeColor = UiTheme.Texto }, 0, 0);
        PrepararGridVeterinarios();
        panelVets.Controls.Add(_gridVeterinarios, 0, 1);
        cuerpo.Controls.Add(panelVets, 0, 0);

        TableLayoutPanel panelDetalle = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12), RowCount = 2, ColumnCount = 1 };
        panelDetalle.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        panelDetalle.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _lblSeleccion.Dock = DockStyle.Fill; _lblSeleccion.Font = UiTheme.FuenteSubtitulo; _lblSeleccion.Text = "Seleccione un veterinario";
        panelDetalle.Controls.Add(_lblSeleccion, 0, 0);
        TabControl tabs = new() { Dock = DockStyle.Fill };
        TabPage horarios = new("Horarios semanales");
        TabPage bloqueos = new("Bloqueos");
        tabs.TabPages.AddRange(new[] { horarios, bloqueos });
        ConstruirHorarios(horarios);
        ConstruirBloqueos(bloqueos);
        panelDetalle.Controls.Add(tabs, 0, 1);
        cuerpo.Controls.Add(panelDetalle, 1, 0);
    }

    private void PrepararGridVeterinarios()
    {
        _gridVeterinarios.Dock = DockStyle.Fill;
        UiTheme.PrepararGrid(_gridVeterinarios);
        _gridVeterinarios.AutoGenerateColumns = false;
        _gridVeterinarios.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CodigoVeterinario", HeaderText = "Código", FillWeight = 62 });
        _gridVeterinarios.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NombreCompleto", HeaderText = "Veterinario", FillWeight = 128 });
        _gridVeterinarios.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Especialidad", HeaderText = "Especialidad", FillWeight = 105 });
        _gridVeterinarios.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "Activo", HeaderText = "Activo", FillWeight = 47 });
        _gridVeterinarios.SelectionChanged += (_, _) => SeleccionarVeterinario();
    }

    private void ConstruirHorarios(TabPage tab)
    {
        TableLayoutPanel layout = new() { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        FlowLayoutPanel comandos = new() { Dock = DockStyle.Fill, Padding = new Padding(5, 7, 5, 7) };
        Button agregar = UiTheme.CrearBoton("Agregar intervalo", true); agregar.Width = 140; agregar.Click += (_, _) => AgregarHorario();
        Button estado = UiTheme.CrearBoton("Activar / desactivar"); estado.Width = 160; estado.Click += (_, _) => CambiarHorario();
        comandos.Controls.AddRange(new Control[] { agregar, estado });
        layout.Controls.Add(comandos, 0, 0);
        _gridHorarios.Dock = DockStyle.Fill; UiTheme.PrepararGrid(_gridHorarios); _gridHorarios.AutoGenerateColumns = false;
        _gridHorarios.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DiaNombre", HeaderText = "Día" });
        _gridHorarios.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "HoraInicio", HeaderText = "Inicio", DefaultCellStyle = new DataGridViewCellStyle { Format = @"hh\:mm" } });
        _gridHorarios.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "HoraFin", HeaderText = "Fin", DefaultCellStyle = new DataGridViewCellStyle { Format = @"hh\:mm" } });
        _gridHorarios.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "Activo", HeaderText = "Activo" });
        layout.Controls.Add(_gridHorarios, 0, 1);
        tab.Controls.Add(layout);
    }

    private void ConstruirBloqueos(TabPage tab)
    {
        TableLayoutPanel layout = new() { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        FlowLayoutPanel comandos = new() { Dock = DockStyle.Fill, Padding = new Padding(5, 8, 5, 7) };
        comandos.Controls.Add(new Label { Text = "Desde:", AutoSize = true, Margin = new Padding(0, 10, 4, 0) });
        _desdeBloqueos.Width = 130; _desdeBloqueos.Format = DateTimePickerFormat.Short; _desdeBloqueos.Value = DateTime.Today.AddMonths(-1); _desdeBloqueos.Margin = new Padding(0, 5, 12, 0);
        comandos.Controls.Add(_desdeBloqueos);
        Button nuevo = UiTheme.CrearBoton("Nuevo bloqueo", true); nuevo.Width = 128; nuevo.Click += (_, _) => AgregarBloqueo();
        Button cancelar = UiTheme.CrearBoton("Cancelar bloqueo"); cancelar.Width = 140; cancelar.Click += (_, _) => CancelarBloqueo();
        Button actualizar = UiTheme.CrearBoton("Actualizar"); actualizar.Width = 100; actualizar.Click += (_, _) => CargarBloqueos();
        comandos.Controls.AddRange(new Control[] { nuevo, cancelar, actualizar });
        layout.Controls.Add(comandos, 0, 0);
        _gridBloqueos.Dock = DockStyle.Fill; UiTheme.PrepararGrid(_gridBloqueos); _gridBloqueos.AutoGenerateColumns = false;
        _gridBloqueos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TipoBloqueo", HeaderText = "Tipo", FillWeight = 76 });
        _gridBloqueos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FechaHoraInicio", HeaderText = "Inicio", FillWeight = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" } });
        _gridBloqueos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FechaHoraFin", HeaderText = "Fin", FillWeight = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" } });
        _gridBloqueos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Motivo", HeaderText = "Motivo", FillWeight = 135 });
        _gridBloqueos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Estado", HeaderText = "Estado", FillWeight = 65 });
        layout.Controls.Add(_gridBloqueos, 0, 1);
        tab.Controls.Add(layout);
    }

    private void CargarVeterinarios()
    {
        try
        {
            _gridVeterinarios.DataSource = _servicio.ListarVeterinarios();
            if (_gridVeterinarios.Rows.Count == 0) LimpiarSeleccion();
        }
        catch (Exception ex) { MostrarError(ex); }
    }

    private void SeleccionarVeterinario()
    {
        if (_gridVeterinarios.CurrentRow?.DataBoundItem is not VeterinarioModel veterinario) return;
        _seleccionado = veterinario;
        _lblSeleccion.Text = $"{veterinario.NombreCompleto} · {veterinario.CodigoVeterinario}";
        CargarHorarios();
        CargarBloqueos();
    }

    private void CargarHorarios()
    {
        if (_seleccionado is null) { _gridHorarios.DataSource = null; return; }
        _gridHorarios.DataSource = _servicio.ListarHorarios(_seleccionado.IdVeterinario);
    }

    private void CargarBloqueos()
    {
        if (_seleccionado is null) { _gridBloqueos.DataSource = null; return; }
        _gridBloqueos.DataSource = _servicio.ListarBloqueos(_seleccionado.IdVeterinario, _desdeBloqueos.Value.Date, _desdeBloqueos.Value.Date.AddYears(2));
    }

    private void EditarVeterinario(VeterinarioModel? veterinario)
    {
        using FormEditarVeterinario ventana = new(_servicio, veterinario);
        if (ventana.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            if (veterinario is null) _servicio.GuardarVeterinario(ventana.Resultado);
            else { ventana.Resultado.IdVeterinario = veterinario.IdVeterinario; _servicio.ActualizarVeterinario(ventana.Resultado); }
            CargarVeterinarios();
        }
        catch (Exception ex) { MostrarError(ex); }
    }

    private void AgregarHorario()
    {
        if (_seleccionado is null) { MessageBox.Show("Seleccione un veterinario."); return; }
        using FormHorario ventana = new();
        if (ventana.ShowDialog(this) != DialogResult.OK) return;
        try { ventana.Resultado.IdVeterinario = _seleccionado.IdVeterinario; _servicio.GuardarHorario(ventana.Resultado); CargarHorarios(); }
        catch (Exception ex) { MostrarError(ex); }
    }

    private void CambiarHorario()
    {
        if (_gridHorarios.CurrentRow?.DataBoundItem is not HorarioVeterinarioModel horario) return;
        try { _servicio.CambiarEstadoHorario(horario.IdHorario, !horario.Activo); CargarHorarios(); }
        catch (Exception ex) { MostrarError(ex); }
    }

    private void AgregarBloqueo()
    {
        if (_seleccionado is null) { MessageBox.Show("Seleccione un veterinario."); return; }
        using FormBloqueo ventana = new(_servicio.ListarTiposBloqueo());
        if (ventana.ShowDialog(this) != DialogResult.OK) return;
        try { ventana.Resultado.IdVeterinario = _seleccionado.IdVeterinario; _servicio.GuardarBloqueo(ventana.Resultado); CargarBloqueos(); }
        catch (Exception ex) { MostrarError(ex); }
    }

    private void CancelarBloqueo()
    {
        if (_gridBloqueos.CurrentRow?.DataBoundItem is not BloqueoVeterinarioModel bloqueo || bloqueo.Estado != "Vigente") return;
        if (MessageBox.Show("¿Cancelar este bloqueo?", "Confirmación", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try { _servicio.CancelarBloqueo(bloqueo.IdBloqueo); CargarBloqueos(); }
        catch (Exception ex) { MostrarError(ex); }
    }

    private void LimpiarSeleccion()
    {
        _seleccionado = null; _lblSeleccion.Text = "Seleccione un veterinario"; _gridHorarios.DataSource = null; _gridBloqueos.DataSource = null;
    }

    private static void MostrarError(Exception ex) => MessageBox.Show(ex.Message, "Veterinarios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}

internal sealed class FormEditarVeterinario : Form
{
    private readonly VeterinarioService _servicio;
    private readonly TextBox _nombre = new(); private readonly TextBox _numero = new(); private readonly TextBox _especialidad = new();
    private readonly TextBox _telefono = new(); private readonly TextBox _correo = new(); private readonly ComboBox _usuario = new(); private readonly CheckBox _activo = new();
    public VeterinarioModel Resultado { get; private set; } = new();

    public FormEditarVeterinario(VeterinarioService servicio, VeterinarioModel? actual)
    {
        _servicio = servicio; UiTheme.PrepararFormulario(this); Text = actual is null ? "Nuevo veterinario" : "Editar veterinario";
        Size = new Size(610, 470); FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        Construir(); CargarUsuarios(actual); if (actual is not null) Cargar(actual);
    }

    private void Construir()
    {
        Label titulo = UiTheme.CrearTitulo(Text); titulo.Location = new Point(24, 18); Controls.Add(titulo);
        int y = 73; AgregarCampo("Nombre completo *", _nombre, ref y); AgregarCampo("Número profesional", _numero, ref y); AgregarCampo("Especialidad", _especialidad, ref y);
        AgregarCampo("Teléfono", _telefono, ref y); AgregarCampo("Correo", _correo, ref y);
        Controls.Add(new Label { Text = "Usuario veterinario", Location = new Point(27, y + 4), AutoSize = true });
        _usuario.Location = new Point(180, y); _usuario.Width = 370; _usuario.DropDownStyle = ComboBoxStyle.DropDownList; Controls.Add(_usuario); y += 43;
        _activo.Text = "Activo"; _activo.Checked = true; _activo.Location = new Point(180, y); Controls.Add(_activo);
        Button guardar = UiTheme.CrearBoton("Guardar", true); guardar.Width = 105; guardar.Location = new Point(333, 381); guardar.Click += (_, _) => Guardar(); Controls.Add(guardar);
        Button cancelar = UiTheme.CrearBoton("Cancelar"); cancelar.Width = 105; cancelar.Location = new Point(445, 381); cancelar.DialogResult = DialogResult.Cancel; Controls.Add(cancelar);
        AcceptButton = guardar; CancelButton = cancelar;
    }
    private void AgregarCampo(string texto, TextBox control, ref int y) { Controls.Add(new Label { Text = texto, Location = new Point(27, y + 4), AutoSize = true }); control.Location = new Point(180, y); control.Width = 370; Controls.Add(control); y += 43; }
    private void CargarUsuarios(VeterinarioModel? actual)
    {
        List<UsuarioModel> usuarios = _servicio.ListarUsuariosVeterinarioDisponibles(actual?.IdVeterinario);
        usuarios.Insert(0, new UsuarioModel { IdUsuario = 0, NombreCompleto = "(Sin usuario asociado)" });
        _usuario.DataSource = usuarios; _usuario.DisplayMember = "NombreCompleto"; _usuario.ValueMember = "IdUsuario";
    }
    private void Cargar(VeterinarioModel v) { _nombre.Text = v.NombreCompleto; _numero.Text = v.NumeroProfesional; _especialidad.Text = v.Especialidad; _telefono.Text = v.Telefono; _correo.Text = v.Correo; _activo.Checked = v.Activo; _usuario.SelectedValue = v.IdUsuario ?? 0; }
    private void Guardar()
    {
        if (string.IsNullOrWhiteSpace(_nombre.Text)) { MessageBox.Show("El nombre es obligatorio."); return; }
        int seleccionado = _usuario.SelectedValue is int id ? id : 0;
        Resultado = new VeterinarioModel { NombreCompleto = _nombre.Text.Trim(), NumeroProfesional = _numero.Text.Trim(), Especialidad = _especialidad.Text.Trim(), Telefono = _telefono.Text.Trim(), Correo = _correo.Text.Trim(), Activo = _activo.Checked, IdUsuario = seleccionado == 0 ? null : seleccionado };
        DialogResult = DialogResult.OK; Close();
    }
}

internal sealed class FormHorario : Form
{
    private readonly ComboBox _dia = new(); private readonly DateTimePicker _inicio = new(); private readonly DateTimePicker _fin = new();
    public HorarioVeterinarioModel Resultado { get; private set; } = new();
    public FormHorario()
    {
        UiTheme.PrepararFormulario(this); Text = "Agregar intervalo laboral"; Size = new Size(470, 292); FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        Label titulo = UiTheme.CrearTitulo("Intervalo laboral"); titulo.Location = new Point(24, 17); Controls.Add(titulo);
        Controls.Add(new Label { Text = "Día", Location = new Point(28, 78), AutoSize = true }); _dia.Location = new Point(155, 74); _dia.Width = 260; _dia.DropDownStyle = ComboBoxStyle.DropDownList; _dia.Items.AddRange(new object[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" }); _dia.SelectedIndex = 0; Controls.Add(_dia);
        PrepararHora(_inicio, 117, new TimeSpan(8, 0, 0), "Hora de inicio"); PrepararHora(_fin, 157, new TimeSpan(17, 0, 0), "Hora final");
        Button guardar = UiTheme.CrearBoton("Guardar", true); guardar.Location = new Point(199, 207); guardar.Width = 100; guardar.Click += (_, _) => Guardar(); Controls.Add(guardar);
        Button cancelar = UiTheme.CrearBoton("Cancelar"); cancelar.Location = new Point(307, 207); cancelar.Width = 108; cancelar.DialogResult = DialogResult.Cancel; Controls.Add(cancelar);
    }
    private void PrepararHora(DateTimePicker picker, int y, TimeSpan hora, string texto) { Controls.Add(new Label { Text = texto, Location = new Point(28, y + 4), AutoSize = true }); picker.Location = new Point(155, y); picker.Width = 260; picker.Format = DateTimePickerFormat.Time; picker.ShowUpDown = true; picker.Value = DateTime.Today.Add(hora); Controls.Add(picker); }
    private void Guardar() { if (_fin.Value.TimeOfDay <= _inicio.Value.TimeOfDay) { MessageBox.Show("La hora final debe ser posterior."); return; } Resultado = new HorarioVeterinarioModel { DiaSemana = checked((byte)(_dia.SelectedIndex + 1)), HoraInicio = _inicio.Value.TimeOfDay, HoraFin = _fin.Value.TimeOfDay }; DialogResult = DialogResult.OK; Close(); }
}

internal sealed class FormBloqueo : Form
{
    private readonly ComboBox _tipo = new(); private readonly DateTimePicker _inicio = new(); private readonly DateTimePicker _fin = new(); private readonly TextBox _motivo = new();
    public BloqueoVeterinarioModel Resultado { get; private set; } = new();
    public FormBloqueo(List<CatalogoSimpleModel> tipos)
    {
        UiTheme.PrepararFormulario(this); Text = "Nuevo bloqueo"; Size = new Size(600, 392); FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        Label titulo = UiTheme.CrearTitulo("Bloqueo de disponibilidad"); titulo.Location = new Point(24, 18); Controls.Add(titulo);
        Controls.Add(new Label { Text = "Tipo *", Location = new Point(28, 80), AutoSize = true }); _tipo.Location = new Point(165, 76); _tipo.Width = 380; _tipo.DropDownStyle = ComboBoxStyle.DropDownList; _tipo.DataSource = tipos; _tipo.DisplayMember = "Nombre"; _tipo.ValueMember = "Id"; Controls.Add(_tipo);
        AgregarFecha(_inicio, "Inicio *", 118, DateTime.Today.AddHours(12)); AgregarFecha(_fin, "Final *", 160, DateTime.Today.AddHours(13));
        Controls.Add(new Label { Text = "Motivo *", Location = new Point(28, 205), AutoSize = true }); _motivo.Location = new Point(165, 202); _motivo.Width = 380; _motivo.Height = 55; _motivo.Multiline = true; Controls.Add(_motivo);
        Button guardar = UiTheme.CrearBoton("Guardar", true); guardar.Width = 105; guardar.Location = new Point(330, 289); guardar.Click += (_, _) => Guardar(); Controls.Add(guardar);
        Button cancelar = UiTheme.CrearBoton("Cancelar"); cancelar.Width = 105; cancelar.Location = new Point(442, 289); cancelar.DialogResult = DialogResult.Cancel; Controls.Add(cancelar);
    }
    private void AgregarFecha(DateTimePicker picker, string etiqueta, int y, DateTime valor) { Controls.Add(new Label { Text = etiqueta, Location = new Point(28, y + 4), AutoSize = true }); picker.Location = new Point(165, y); picker.Width = 380; picker.Format = DateTimePickerFormat.Custom; picker.CustomFormat = "dd/MM/yyyy HH:mm"; picker.Value = valor; Controls.Add(picker); }
    private void Guardar() { if (_fin.Value <= _inicio.Value || string.IsNullOrWhiteSpace(_motivo.Text)) { MessageBox.Show("Complete un intervalo y motivo válidos."); return; } Resultado = new BloqueoVeterinarioModel { IdTipoBloqueo = (int)_tipo.SelectedValue, FechaHoraInicio = _inicio.Value, FechaHoraFin = _fin.Value, Motivo = _motivo.Text.Trim() }; DialogResult = DialogResult.OK; Close(); }
}
