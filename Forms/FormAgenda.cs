using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Services;
using ClinicaVeterinaria.Utils;

namespace ClinicaVeterinaria.Forms;

public sealed class FormAgenda : Form
{
    private readonly CitaService _servicio = new();
    private readonly VeterinarioService _veterinarioService = new();
    private readonly long? _mascotaInicial;
    private readonly bool _puedeGestionar = SesionActual.EsRol("Administrador", "Recepción");
    private DateTimePicker _fecha = null!;
    private ComboBox _veterinario = null!;
    private ComboBox _filtroServicio = null!;
    private ComboBox _estado = null!;
    private TextBox _buscar = null!;
    private DataGridView _grid = null!;
    private Label _indicador = null!;

    public FormAgenda(long? mascotaInicial = null)
    {
        _mascotaInicial = mascotaInicial;
        ConstruirInterfaz();
        CargarCatalogos();
        CargarAgenda();
        if (_mascotaInicial.HasValue && _puedeGestionar)
        {
            Shown += (_, _) => BeginInvoke(new Action(() => NuevaCita(_mascotaInicial)));
        }
    }

    private void ConstruirInterfaz()
    {
        UiTheme.PrepararFormulario(this);
        Padding = new Padding(10);
        TableLayoutPanel layout = new() { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, BackColor = UiTheme.Fondo };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(layout);
        Panel encabezado = new() { Dock = DockStyle.Fill };
        Label titulo = UiTheme.CrearTitulo("Agenda y recepción"); titulo.Location = new Point(4, 5); encabezado.Controls.Add(titulo);
        encabezado.Controls.Add(new Label { Text = "Reserva por disponibilidad real, bloques de 30 minutos, llegadas, cancelaciones y reagendamientos.", AutoSize = true, Location = new Point(7, 41), ForeColor = UiTheme.TextoSecundario });
        layout.Controls.Add(encabezado, 0, 0);

        FlowLayoutPanel filtros = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12, 12, 12, 8), WrapContents = false };
        filtros.Controls.Add(Etiqueta("Fecha"));
        _fecha = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 118, Margin = new Padding(4, 4, 14, 0), Value = DateTime.Today };
        filtros.Controls.Add(_fecha);
        filtros.Controls.Add(Etiqueta("Veterinario"));
        _veterinario = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 205, Margin = new Padding(4, 4, 14, 0) }; filtros.Controls.Add(_veterinario);
        filtros.Controls.Add(Etiqueta("Estado"));
        _estado = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130, Margin = new Padding(4, 4, 14, 0) }; filtros.Controls.Add(_estado);
        filtros.Controls.Add(Etiqueta("Servicio"));
        _filtroServicio = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190, Margin = new Padding(4, 4, 14, 0) }; filtros.Controls.Add(_filtroServicio);
        _buscar = new TextBox { Width = 215, PlaceholderText = "Dueño o mascota", Margin = new Padding(4, 4, 8, 0) }; filtros.Controls.Add(_buscar);
        Button buscar = UiTheme.CrearBoton("Filtrar", true); buscar.Width = 86; buscar.Margin = new Padding(4, 0, 0, 0); buscar.Click += (_, _) => CargarAgenda(); filtros.Controls.Add(buscar);
        layout.Controls.Add(filtros, 0, 1);

        FlowLayoutPanel acciones = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12, 7, 12, 7), WrapContents = false };
        Button nueva = Boton("Nueva cita", true, (_, _) => NuevaCita(null)); nueva.Enabled = _puedeGestionar;
        Button confirmar = Boton("Confirmar", false, (_, _) => EjecutarCitaSeleccionada(c => _servicio.ConfirmarCita(c.IdCita))); confirmar.Enabled = _puedeGestionar;
        Button reagendar = Boton("Reagendar", false, (_, _) => Reagendar()); reagendar.Enabled = _puedeGestionar;
        Button cancelar = Boton("Cancelar", false, (_, _) => Cancelar()); cancelar.Enabled = _puedeGestionar;
        Button llegada = Boton("Marcar llegada", true, (_, _) => MarcarLlegada()); llegada.Enabled = _puedeGestionar;
        Button ausencia = Boton("No asistió", false, (_, _) => NoAsistio()); ausencia.Enabled = _puedeGestionar;
        Button consulta = Boton("Iniciar consulta", true, (_, _) => IniciarConsulta()); consulta.Enabled = SesionActual.EsRol("Administrador", "Veterinario") && SesionActual.Usuario!.IdVeterinario.HasValue;
        Button refrescar = Boton("Actualizar", false, (_, _) => CargarAgenda());
        acciones.Controls.AddRange(new Control[] { nueva, confirmar, reagendar, cancelar, llegada, ausencia, consulta, refrescar });
        layout.Controls.Add(acciones, 0, 2);

        TableLayoutPanel listado = new() { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Color.White, Margin = new Padding(0, 10, 0, 0), Padding = new Padding(10) };
        listado.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); listado.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _indicador = new Label { Dock = DockStyle.Fill, Font = UiTheme.FuenteSubtitulo, ForeColor = UiTheme.Texto, TextAlign = ContentAlignment.MiddleLeft };
        listado.Controls.Add(_indicador, 0, 0);
        _grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false };
        UiTheme.PrepararGrid(_grid);
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Hora", HeaderText = "Hora", FillWeight = 48 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Mascota", HeaderText = "Mascota", FillWeight = 88 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Dueno", HeaderText = "Dueño", FillWeight = 118 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Veterinario", HeaderText = "Veterinario", FillWeight = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Servicio", HeaderText = "Servicio", FillWeight = 102 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DuracionMinutos", HeaderText = "Min", FillWeight = 42 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Estado", HeaderText = "Estado", FillWeight = 70 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "AlertasActivas", HeaderText = "Alertas", FillWeight = 45 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SaldoPendiente", HeaderText = "Saldo", FillWeight = 68, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
        _grid.CellFormatting += ColorearEstado;
        listado.Controls.Add(_grid, 0, 1);
        layout.Controls.Add(listado, 0, 3);
    }

    private static Label Etiqueta(string texto) => new() { Text = texto, AutoSize = true, Margin = new Padding(0, 9, 2, 0), ForeColor = UiTheme.TextoSecundario };
    private static Button Boton(string texto, bool principal, EventHandler click) { Button b = UiTheme.CrearBoton(texto, principal); b.Width = texto.Length > 12 ? 138 : 110; b.Margin = new Padding(0, 0, 8, 0); b.Click += click; return b; }

    private void CargarCatalogos()
    {
        try
        {
            List<VeterinarioModel> vets = _veterinarioService.ListarVeterinarios(true);
            vets.Insert(0, new VeterinarioModel { IdVeterinario = 0, NombreCompleto = "Todos" });
            _veterinario.DataSource = vets; _veterinario.DisplayMember = "NombreCompleto"; _veterinario.ValueMember = "IdVeterinario";
            if (SesionActual.EsRol("Veterinario") && SesionActual.Usuario!.IdVeterinario.HasValue)
            {
                _veterinario.SelectedValue = SesionActual.Usuario.IdVeterinario.Value; _veterinario.Enabled = false;
            }
            List<ServicioModel> servicios = _servicio.ListarServiciosAgenda();
            servicios.Insert(0, new ServicioModel { IdServicio = 0, Nombre = "Todos" });
            _filtroServicio.DataSource = servicios; _filtroServicio.DisplayMember = "Nombre"; _filtroServicio.ValueMember = "IdServicio";
            _estado.Items.AddRange(new object[] { "Todos", "Pendiente", "Confirmada", "Llegó", "En consulta", "Atendida", "Cancelada", "No asistió", "Reagendada" });
            _estado.SelectedIndex = 0;
        }
        catch (Exception ex) { MostrarError(ex); }
    }

    private void CargarAgenda()
    {
        try
        {
            int idVet = _veterinario.SelectedValue is int v ? v : 0;
            int idServicio = _filtroServicio.SelectedValue is int s ? s : 0;
            string estado = _estado.SelectedItem?.ToString() == "Todos" ? string.Empty : _estado.SelectedItem?.ToString() ?? string.Empty;
            List<CitaModel> citas = _servicio.ListarAgenda(_fecha.Value.Date, idVet == 0 ? null : idVet, estado, idServicio == 0 ? null : idServicio, _buscar.Text);
            _grid.DataSource = citas;
            _indicador.Text = $"{citas.Count} cita(s) para {_fecha.Value:dd/MM/yyyy}";
        }
        catch (Exception ex) { MostrarError(ex); }
    }

    private CitaModel? Seleccionada() => _grid.CurrentRow?.DataBoundItem as CitaModel;
    private void NuevaCita(long? idMascota)
    {
        using FormEditarCitaAgenda ventana = new(_servicio, _veterinarioService, null, idMascota);
        if (ventana.ShowDialog(this) != DialogResult.OK) return;
        try { _servicio.CrearCita(ventana.Resultado); _fecha.Value = ventana.Resultado.FechaHoraInicio.Date; CargarAgenda(); MessageBox.Show("Cita creada y bloques reservados correctamente.", "Agenda", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { MostrarError(ex); }
    }
    private void Reagendar()
    {
        CitaModel? cita = Seleccionada(); if (cita is null) { MessageBox.Show("Seleccione una cita."); return; }
        using FormEditarCitaAgenda ventana = new(_servicio, _veterinarioService, cita, null);
        if (ventana.ShowDialog(this) != DialogResult.OK) return;
        string? motivo = PedirTexto("Motivo obligatorio del reagendamiento", "Reagendar cita"); if (motivo is null) return;
        try { _servicio.ReagendarCita(cita.IdCita, ventana.Resultado, motivo); _fecha.Value = ventana.Resultado.FechaHoraInicio.Date; CargarAgenda(); }
        catch (Exception ex) { MostrarError(ex); }
    }
    private void Cancelar()
    {
        CitaModel? cita = Seleccionada(); if (cita is null) return;
        string? motivo = PedirTexto("Motivo obligatorio de cancelación", "Cancelar cita"); if (motivo is null) return;
        try { _servicio.CancelarCita(cita.IdCita, motivo); CargarAgenda(); }
        catch (Exception ex) { MostrarError(ex); }
    }
    private void NoAsistio()
    {
        CitaModel? cita = Seleccionada(); if (cita is null) return;
        string? motivo = PedirTexto("Motivo o nota obligatoria", "Marcar no asistencia"); if (motivo is null) return;
        try { _servicio.MarcarNoAsistencia(cita.IdCita, motivo); CargarAgenda(); }
        catch (Exception ex) { MostrarError(ex); }
    }
    private void MarcarLlegada()
    {
        CitaModel? cita = Seleccionada(); if (cita is null) return;
        string? nota = PedirTexto("Observación de llegada (puede dejarse vacía)", "Marcar llegada", permitirVacio: true); if (nota is null) return;
        try { _servicio.MarcarLlegada(cita.IdCita, nota); CargarAgenda(); }
        catch (Exception ex) { MostrarError(ex); }
    }
    private void IniciarConsulta()
    {
        CitaModel? cita = Seleccionada(); if (cita is null) return;
        try { _servicio.IniciarConsulta(cita.IdCita); CargarAgenda(); MessageBox.Show("La cita quedó en estado En consulta. Abra el módulo Atención Clínica para registrar la evaluación."); }
        catch (Exception ex) { MostrarError(ex); }
    }
    private void EjecutarCitaSeleccionada(Action<CitaModel> accion)
    {
        CitaModel? cita = Seleccionada(); if (cita is null) return;
        try { accion(cita); CargarAgenda(); }
        catch (Exception ex) { MostrarError(ex); }
    }
    private void ColorearEstado(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (_grid.Rows[e.RowIndex].DataBoundItem is not CitaModel c) return;
        _grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = c.Estado switch
        {
            "Llegó" => Color.FromArgb(224, 246, 239), "En consulta" => Color.FromArgb(220, 236, 252),
            "Cancelada" or "No asistió" => Color.FromArgb(252, 231, 231), "Atendida" => Color.FromArgb(235, 245, 235), _ => Color.White
        };
    }
    private static string? PedirTexto(string etiqueta, string titulo, bool permitirVacio = false)
    {
        using FormTextoMotivo form = new(etiqueta, titulo, permitirVacio); return form.ShowDialog() == DialogResult.OK ? form.Resultado : null;
    }
    private static void MostrarError(Exception ex) => MessageBox.Show(ex.Message, "Agenda", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}

internal sealed class FormEditarCitaAgenda : Form
{
    private readonly CitaService _citaService; private readonly DisponibilidadService _disponibilidad = new(); private readonly TarifaService _tarifas = new(); private readonly CitaModel? _original;
    private readonly TextBox _buscarMascota = new(); private readonly DataGridView _mascotas = new(); private readonly ComboBox _veterinario = new(); private readonly ComboBox _servicio = new();
    private readonly DateTimePicker _fecha = new(); private readonly Label _precioTarifa = new() { AutoSize = true, ForeColor = UiTheme.Primario, Font = UiTheme.FuenteSubtitulo }; private readonly ListBox _horarios = new(); private readonly TextBox _motivo = new(); private readonly TextBox _observaciones = new();
    private MascotaBusquedaModel? _mascotaSeleccionada;
    public CitaModel Resultado { get; private set; } = new();

    public FormEditarCitaAgenda(CitaService citas, VeterinarioService veterinarios, CitaModel? original, long? idMascotaInicial)
    {
        _citaService = citas; _original = original; UiTheme.PrepararFormulario(this); Text = original is null ? "Nueva cita" : "Reagendar cita";
        Size = new Size(1025, 700); MinimumSize = Size; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        Construir(); CargarCatalogos(veterinarios); if (original is not null) CargarOriginal(original); else if (idMascotaInicial.HasValue) SeleccionarMascotaInicial(idMascotaInicial.Value); else BuscarMascotas();
    }

    private void Construir()
    {
        TableLayoutPanel layout = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(18) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62)); Controls.Add(layout);
        Label titulo = UiTheme.CrearTitulo(Text); layout.Controls.Add(titulo, 0, 0); layout.SetColumnSpan(titulo, 2);
        Panel izq = new() { Dock = DockStyle.Fill, Padding = new Padding(0, 6, 12, 0) }; Panel der = new() { Dock = DockStyle.Fill, Padding = new Padding(12, 6, 0, 0) }; layout.Controls.Add(izq, 0, 1); layout.Controls.Add(der, 1, 1);
        TableLayoutPanel paciente = new() { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 }; paciente.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); paciente.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); paciente.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); izq.Controls.Add(paciente);
        paciente.Controls.Add(new Label { Text = "Paciente", Dock = DockStyle.Fill, Font = UiTheme.FuenteSubtitulo }, 0, 0);
        FlowLayoutPanel buscarPanel = new() { Dock = DockStyle.Fill, WrapContents = false }; _buscarMascota.Width = 295; _buscarMascota.PlaceholderText = "Paciente, dueño, teléfono o código"; Button buscar = UiTheme.CrearBoton("Buscar", true); buscar.Width = 90; buscar.Click += (_, _) => BuscarMascotas(); buscarPanel.Controls.AddRange(new Control[] { _buscarMascota, buscar }); paciente.Controls.Add(buscarPanel, 0, 1);
        _mascotas.Dock = DockStyle.Fill; UiTheme.PrepararGrid(_mascotas); _mascotas.AutoGenerateColumns = false; _mascotas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CodigoPaciente", HeaderText = "Código", FillWeight = 62 }); _mascotas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NombreMascota", HeaderText = "Mascota", FillWeight = 88 }); _mascotas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Dueno", HeaderText = "Dueño", FillWeight = 128 }); _mascotas.SelectionChanged += (_, _) => { _mascotaSeleccionada = _mascotas.CurrentRow?.DataBoundItem as MascotaBusquedaModel; CargarHorarios(); }; paciente.Controls.Add(_mascotas, 0, 2);
        TableLayoutPanel datos = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 8 }; datos.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145)); datos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        datos.RowStyles.Add(new RowStyle(SizeType.Absolute, 43)); datos.RowStyles.Add(new RowStyle(SizeType.Absolute, 43)); datos.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        datos.RowStyles.Add(new RowStyle(SizeType.Absolute, 43)); datos.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); datos.RowStyles.Add(new RowStyle(SizeType.Absolute, 74)); datos.RowStyles.Add(new RowStyle(SizeType.Absolute, 74)); datos.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); der.Controls.Add(datos);
        Campo(datos, "Veterinario *", _veterinario, 0); _veterinario.DropDownStyle = ComboBoxStyle.DropDownList; _veterinario.SelectedIndexChanged += (_, _) => CargarHorarios();
        Campo(datos, "Servicio *", _servicio, 1); _servicio.DropDownStyle = ComboBoxStyle.DropDownList; _servicio.SelectedIndexChanged += (_, _) => CargarHorarios();
        Campo(datos, "Fecha *", _fecha, 2); _fecha.Format = DateTimePickerFormat.Short; _fecha.MinDate = DateTime.Today; _fecha.ValueChanged += (_, _) => CargarHorarios();
        Campo(datos, "Precio calculado (Q, IVA incl.)", _precioTarifa, 3);
        datos.Controls.Add(new Label { Text = "Horarios", Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft, Padding = new Padding(0, 8, 0, 0) }, 0, 4); _horarios.Dock = DockStyle.Fill; _horarios.DisplayMember = "Visualizacion"; datos.Controls.Add(_horarios, 1, 4);
        Campo(datos, "Motivo *", _motivo, 5); _motivo.Multiline = true;
        Campo(datos, "Observaciones", _observaciones, 6); _observaciones.Multiline = true;
        Button recargar = UiTheme.CrearBoton("Consultar disponibilidad", true); recargar.Dock = DockStyle.Left; recargar.Width = 180; recargar.Click += (_, _) => CargarHorarios(); datos.Controls.Add(recargar, 1, 7);
        FlowLayoutPanel pie = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 10, 0, 0) }; Button guardar = UiTheme.CrearBoton(_original is null ? "Crear cita" : "Guardar nuevo horario", true); guardar.Width = 180; guardar.Click += (_, _) => Guardar(); Button cancelar = UiTheme.CrearBoton("Cancelar"); cancelar.Width = 110; cancelar.DialogResult = DialogResult.Cancel; pie.Controls.AddRange(new Control[] { cancelar, guardar }); layout.Controls.Add(pie, 0, 2); layout.SetColumnSpan(pie, 2);
    }
    private static void Campo(TableLayoutPanel layout, string etiqueta, Control control, int row) { layout.Controls.Add(new Label { Text = etiqueta, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row); control.Dock = DockStyle.Fill; control.Margin = new Padding(0, 4, 0, 4); layout.Controls.Add(control, 1, row); }
    private void CargarCatalogos(VeterinarioService veterinarios)
    {
        _veterinario.DataSource = veterinarios.ListarVeterinarios(true); _veterinario.DisplayMember = "NombreCompleto"; _veterinario.ValueMember = "IdVeterinario";
        _servicio.DataSource = _citaService.ListarServiciosAgenda(); _servicio.DisplayMember = "NombreDuracion"; _servicio.ValueMember = "IdServicio"; _fecha.Value = DateTime.Today;
    }
    private void BuscarMascotas() { List<MascotaBusquedaModel> datos = _citaService.BuscarMascotas(_buscarMascota.Text); _mascotas.DataSource = datos; }
    private void SeleccionarMascotaInicial(long id) { MascotaBusquedaModel m = _citaService.ObtenerMascotaParaAgenda(id); _mascotas.DataSource = new List<MascotaBusquedaModel> { m }; _mascotaSeleccionada = m; CargarHorarios(); }
    private void CargarOriginal(CitaModel original)
    {
        MascotaBusquedaModel m = _citaService.ObtenerMascotaParaAgenda(original.IdMascota); _mascotas.DataSource = new List<MascotaBusquedaModel> { m }; _mascotas.Enabled = false; _buscarMascota.Enabled = false; _mascotaSeleccionada = m;
        _veterinario.SelectedValue = original.IdVeterinario; _servicio.SelectedValue = original.IdServicio; _fecha.Value = original.FechaHoraInicio.Date; _motivo.Text = original.MotivoConsulta; _observaciones.Text = original.ObservacionesRecepcion; CargarHorarios();
    }
    private void CargarHorarios()
    {
        if (_mascotaSeleccionada is null || _veterinario.SelectedValue is not int vet || _servicio.SelectedItem is not ServicioModel servicio) return;
        try
        {
            TarifaCalculadaModel tarifa = _tarifas.CalcularPrecioServicio(servicio, _mascotaSeleccionada.Especie);
            _precioTarifa.Text = tarifa.Resumen;
            List<DisponibilidadBloqueModel> bloques = _disponibilidad.ObtenerHorariosDisponibles(vet, _fecha.Value.Date, servicio.DuracionMinutos, _mascotaSeleccionada.IdMascota, _original?.IdCita);
            _horarios.DataSource = bloques;
            if (_original is not null)
            {
                foreach (DisponibilidadBloqueModel b in bloques) if (b.FechaHoraInicio == _original.FechaHoraInicio) { _horarios.SelectedItem = b; break; }
            }
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Disponibilidad", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    private void Guardar()
    {
        if (_mascotaSeleccionada is null || _veterinario.SelectedValue is not int vet || _servicio.SelectedItem is not ServicioModel servicio || _horarios.SelectedItem is not DisponibilidadBloqueModel bloque || !bloque.Disponible || string.IsNullOrWhiteSpace(_motivo.Text))
        { MessageBox.Show("Seleccione paciente, veterinario, servicio, un horario disponible y escriba el motivo.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        Resultado = new CitaModel { IdMascota = _mascotaSeleccionada.IdMascota, IdVeterinario = vet, IdServicio = servicio.IdServicio, FechaHoraInicio = bloque.FechaHoraInicio, DuracionMinutos = servicio.DuracionMinutos, FechaHoraFin = bloque.FechaHoraFin, MotivoConsulta = _motivo.Text.Trim(), ObservacionesRecepcion = _observaciones.Text.Trim() };
        DialogResult = DialogResult.OK; Close();
    }
}

internal sealed class FormTextoMotivo : Form
{
    private readonly TextBox _texto = new(); private readonly bool _permitirVacio; public string Resultado { get; private set; } = string.Empty;
    public FormTextoMotivo(string etiqueta, string titulo, bool permitirVacio)
    {
        _permitirVacio = permitirVacio; UiTheme.PrepararFormulario(this); Text = titulo; Size = new Size(520, 260); FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        Controls.Add(new Label { Text = etiqueta, Location = new Point(25, 25), AutoSize = true, Font = UiTheme.FuenteSubtitulo }); _texto.Location = new Point(28, 62); _texto.Size = new Size(445, 78); _texto.Multiline = true; Controls.Add(_texto);
        Button aceptar = UiTheme.CrearBoton("Aceptar", true); aceptar.Location = new Point(257, 165); aceptar.Width = 102; aceptar.Click += (_, _) => Guardar(); Controls.Add(aceptar); Button cancelar = UiTheme.CrearBoton("Cancelar"); cancelar.Location = new Point(369, 165); cancelar.Width = 104; cancelar.DialogResult = DialogResult.Cancel; Controls.Add(cancelar);
    }
    private void Guardar() { if (!_permitirVacio && string.IsNullOrWhiteSpace(_texto.Text)) { MessageBox.Show("Debe registrar un motivo."); return; } Resultado = _texto.Text.Trim(); DialogResult = DialogResult.OK; Close(); }
}
