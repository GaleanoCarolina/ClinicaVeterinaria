using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Services;
using ClinicaVeterinaria.Utils;

namespace ClinicaVeterinaria.Forms;

public sealed class FormAtencionClinica : Form
{
    private readonly ConsultaService _consultaService = new();
    private readonly RecetaService _recetaService = new();
    private readonly VacunacionService _vacunacionService = new();
    private readonly BindingList<DiagnosticoModel> _diagnosticos = new();
    private readonly BindingList<ConsultaServicioModel> _servicios = new();
    private readonly BindingList<RecetaDetalleModel> _recetaDetalles = new();
    private readonly BindingList<VacunaAplicadaModel> _vacunas = new();
    private readonly BindingList<DesparasitacionModel> _desparasitaciones = new();
    private readonly BindingList<OrdenClinicaModel> _ordenes = new();

    private DataGridView _gridCola = null!;
    private Label _lblPaciente = null!;
    private Label _lblDetalle = null!;
    private Label _lblCita = null!;
    private Label _lblAlertas = null!;
    private TabControl _tabs = null!;
    private TextBox _motivo = null!;
    private TextBox _anamnesis = null!;
    private NumericUpDown _peso = null!;
    private NumericUpDown _temperatura = null!;
    private NumericUpDown _fc = null!;
    private NumericUpDown _fr = null!;
    private ComboBox _hidratacion = null!;
    private TextBox _hallazgos = null!;
    private TextBox _pronostico = null!;
    private TextBox _tratamiento = null!;
    private TextBox _indicaciones = null!;
    private DateTimePicker _proximaRevision = null!;
    private ComboBox _estadoEgreso = null!;
    private TextBox _indicacionesReceta = null!;
    private DataGridView _gridDiagnosticos = null!;
    private DataGridView _gridServicios = null!;
    private DataGridView _gridReceta = null!;
    private DataGridView _gridVacunas = null!;
    private DataGridView _gridDesparasitaciones = null!;
    private DataGridView _gridOrdenes = null!;
    private Button _btnFinalizar = null!;
    private AtencionEncabezadoModel? _atencionActual;

    public FormAtencionClinica()
    {
        ConstruirInterfaz();
        Shown += (_, _) => CargarCola();
    }

    private void ConstruirInterfaz()
    {
        UiTheme.PrepararFormulario(this);
        Text = "Atención clínica";
        BackColor = UiTheme.Fondo;
        Padding = new Padding(0);

        TableLayoutPanel principal = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8),
            BackColor = UiTheme.Fondo
        };
        principal.RowStyles.Add(new RowStyle(SizeType.Absolute, 118F));
        principal.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        Controls.Add(principal);
        principal.Controls.Add(ConstruirEncabezado(), 0, 0);
        principal.Controls.Add(ConstruirContenido(), 0, 1);
    }

    private Control ConstruirEncabezado()
    {
        Panel panel = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18, 12, 18, 10), Margin = new Padding(0, 0, 0, 8) };
        _lblPaciente = new Label { AutoSize = true, Text = "Seleccione un paciente en consulta", Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold), ForeColor = UiTheme.Primario, Location = new Point(18, 10) };
        _lblDetalle = new Label { AutoSize = true, Text = "", ForeColor = UiTheme.TextoSecundario, Location = new Point(20, 48) };
        _lblCita = new Label { AutoSize = true, Text = "", ForeColor = UiTheme.TextoSecundario, Location = new Point(20, 74) };
        _lblAlertas = new Label { AutoEllipsis = true, Width = 530, Height = 68, TextAlign = ContentAlignment.MiddleLeft, Font = UiTheme.FuenteSubtitulo, ForeColor = UiTheme.Peligro, Location = new Point(650, 18), Text = "Sin paciente seleccionado" };
        panel.Controls.AddRange(new Control[] { _lblPaciente, _lblDetalle, _lblCita, _lblAlertas });
        return panel;
    }

    private Control ConstruirContenido()
    {
        TableLayoutPanel layout = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 315F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.Controls.Add(ConstruirCola(), 0, 0);
        layout.Controls.Add(ConstruirAreaClinica(), 1, 0);
        return layout;
    }

    private Control ConstruirCola()
    {
        Panel panel = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12), Margin = new Padding(0, 0, 10, 0) };
        Label titulo = new() { Text = "Pacientes en consulta", Dock = DockStyle.Top, Height = 35, Font = UiTheme.FuenteSubtitulo, ForeColor = UiTheme.Primario };
        Button actualizar = UiTheme.CrearBoton("Actualizar cola");
        actualizar.Dock = DockStyle.Bottom;
        actualizar.Height = 42;
        actualizar.Click += (_, _) => CargarCola();
        _gridCola = new DataGridView { Dock = DockStyle.Fill, Margin = new Padding(0) };
        UiTheme.PrepararGrid(_gridCola);
        _gridCola.AutoGenerateColumns = false;
        _gridCola.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Hora", HeaderText = "Hora", FillWeight = 36 });
        _gridCola.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Mascota", HeaderText = "Paciente", FillWeight = 75 });
        _gridCola.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Servicio", HeaderText = "Servicio", FillWeight = 90 });
        _gridCola.SelectionChanged += (_, _) => SeleccionarCita();
        _gridCola.CellClick += (_, _) => SeleccionarCita();
        panel.Controls.Add(_gridCola);
        panel.Controls.Add(actualizar);
        panel.Controls.Add(titulo);
        return panel;
    }

    private Control ConstruirAreaClinica()
    {
        TableLayoutPanel layout = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
        _tabs = new TabControl { Dock = DockStyle.Fill, Font = UiTheme.FuenteNormal };
        _tabs.TabPages.Add(CrearTab("Evaluación", ConstruirEvaluacion()));
        _tabs.TabPages.Add(CrearTab("Diagnósticos", ConstruirDiagnosticos()));
        _tabs.TabPages.Add(CrearTab("Servicios y procedimientos", ConstruirServicios()));
        _tabs.TabPages.Add(CrearTab("Receta", ConstruirReceta()));
        _tabs.TabPages.Add(CrearTab("Vacunas y desparasitación", ConstruirVacunas()));
        _tabs.TabPages.Add(CrearTab("Órdenes clínicas", ConstruirOrdenes()));
        _tabs.TabPages.Add(CrearTab("Cierre", ConstruirCierre()));
        layout.Controls.Add(_tabs, 0, 0);

        FlowLayoutPanel acciones = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8, 7, 8, 3), BackColor = Color.White };
        _btnFinalizar = UiTheme.CrearBoton("Finalizar consulta", true);
        _btnFinalizar.Width = 170;
        ConfigurarBotonFinalizar(false);
        _btnFinalizar.Click += (_, _) => FinalizarConsulta();
        acciones.Controls.Add(_btnFinalizar);
        Button recetaPdf = UiTheme.CrearBoton("Receta PDF"); recetaPdf.Width = 120; recetaPdf.Click += (_, _) => AvisoPdf();
        Button resumenPdf = UiTheme.CrearBoton("Resumen PDF"); resumenPdf.Width = 130; resumenPdf.Click += (_, _) => AvisoPdf();
        acciones.Controls.Add(recetaPdf); acciones.Controls.Add(resumenPdf);
        layout.Controls.Add(acciones, 0, 1);
        return layout;
    }

    private static TabPage CrearTab(string texto, Control contenido)
    {
        TabPage tab = new(texto) { BackColor = Color.White, Padding = new Padding(12) };
        tab.Controls.Add(contenido);
        return tab;
    }

    private Control ConstruirEvaluacion()
    {
        TableLayoutPanel grid = CrearFormulario(4, 7);
        _motivo = CrearTexto(false); _anamnesis = CrearTexto(true); _peso = CrearDecimal(0, 250, 2); _temperatura = CrearDecimal(20, 50, 2);
        _fc = CrearEntero(0, 400); _fr = CrearEntero(0, 300);
        _hidratacion = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _hidratacion.Items.AddRange(new object[] { "No evaluada", "Normal", "Leve deshidratación", "Moderada", "Severa" }); _hidratacion.SelectedIndex = 0;
        _hallazgos = CrearTexto(true); _pronostico = CrearTexto(true); _tratamiento = CrearTexto(true);
        AgregarCampo(grid, "Motivo de consulta *", _motivo, 0, 0, 3);
        AgregarCampo(grid, "Anamnesis *", _anamnesis, 0, 1, 3);
        AgregarCampo(grid, "Peso actual (kg)", _peso, 0, 2); AgregarCampo(grid, "Temperatura (°C)", _temperatura, 2, 2);
        AgregarCampo(grid, "Frecuencia cardiaca", _fc, 0, 3); AgregarCampo(grid, "Frecuencia respiratoria", _fr, 2, 3);
        AgregarCampo(grid, "Hidratación", _hidratacion, 0, 4); AgregarCampo(grid, "Hallazgos físicos *", _hallazgos, 2, 4);
        AgregarCampo(grid, "Pronóstico", _pronostico, 0, 5); AgregarCampo(grid, "Tratamiento general", _tratamiento, 2, 5);
        return grid;
    }

    private Control ConstruirDiagnosticos()
    {
        TableLayoutPanel panel = CrearPanelConBotones(out FlowLayoutPanel botones);
        Button agregar = UiTheme.CrearBoton("Agregar diagnóstico", true); agregar.Width = 165; agregar.Click += (_, _) => AgregarDiagnostico();
        Button quitar = UiTheme.CrearBoton("Quitar seleccionado"); quitar.Width = 155; quitar.Click += (_, _) => QuitarSeleccion(_gridDiagnosticos, _diagnosticos);
        botones.Controls.AddRange(new Control[] { agregar, quitar });
        _gridDiagnosticos = CrearGrid(_diagnosticos);
        _gridDiagnosticos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Tipo", HeaderText = "Tipo", FillWeight = 55 });
        _gridDiagnosticos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Descripcion", HeaderText = "Diagnóstico", FillWeight = 150 });
        _gridDiagnosticos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Observaciones", HeaderText = "Observaciones", FillWeight = 130 });
        panel.Controls.Add(_gridDiagnosticos, 0, 1);
        return panel;
    }

    private Control ConstruirServicios()
    {
        TableLayoutPanel panel = CrearPanelConBotones(out FlowLayoutPanel botones);
        Button agregar = UiTheme.CrearBoton("Agregar servicio", true); agregar.Width = 150; agregar.Click += (_, _) => AgregarServicio();
        Button quitar = UiTheme.CrearBoton("Quitar seleccionado"); quitar.Width = 155; quitar.Click += (_, _) => QuitarSeleccion(_gridServicios, _servicios);
        botones.Controls.AddRange(new Control[] { agregar, quitar });
        _gridServicios = CrearGrid(_servicios);
        _gridServicios.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Servicio", HeaderText = "Servicio", FillWeight = 115 });
        _gridServicios.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Descripcion", HeaderText = "Descripción", FillWeight = 145 });
        _gridServicios.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Cantidad", HeaderText = "Cant.", FillWeight = 48 });
        _gridServicios.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PrecioUnitario", HeaderText = "Precio (Q)", FillWeight = 62, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
        _gridServicios.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Descuento", HeaderText = "Desc.", FillWeight = 60, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
        _gridServicios.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Subtotal", HeaderText = "Total (Q, IVA incl.)", FillWeight = 68, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
        panel.Controls.Add(_gridServicios, 0, 1);
        return panel;
    }

    private Control ConstruirReceta()
    {
        TableLayoutPanel layout = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55F)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86F));
        FlowLayoutPanel botones = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 4, 0, 4) };
        Button agregar = UiTheme.CrearBoton("Agregar medicamento", true); agregar.Width = 175; agregar.Click += (_, _) => AgregarMedicamento();
        Button quitar = UiTheme.CrearBoton("Quitar seleccionado"); quitar.Width = 155; quitar.Click += (_, _) => QuitarSeleccion(_gridReceta, _recetaDetalles);
        botones.Controls.AddRange(new Control[] { agregar, quitar }); layout.Controls.Add(botones, 0, 0);
        _gridReceta = CrearGrid(_recetaDetalles);
        _gridReceta.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NombreMostrado", HeaderText = "Medicamento", FillWeight = 105 });
        _gridReceta.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Dosis", HeaderText = "Dosis", FillWeight = 70 });
        _gridReceta.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Frecuencia", HeaderText = "Frecuencia", FillWeight = 75 });
        _gridReceta.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Duracion", HeaderText = "Duración", FillWeight = 65 });
        _gridReceta.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ViaAdministracion", HeaderText = "Vía", FillWeight = 60 });
        _gridReceta.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Indicaciones", HeaderText = "Indicaciones", FillWeight = 130 });
        layout.Controls.Add(_gridReceta, 0, 1);
        _indicacionesReceta = CrearTexto(true);
        GroupBox indicaciones = new() { Text = "Indicaciones generales de la receta", Dock = DockStyle.Fill, Padding = new Padding(8) };
        indicaciones.Controls.Add(_indicacionesReceta); layout.Controls.Add(indicaciones, 0, 2);
        return layout;
    }

    private Control ConstruirVacunas()
    {
        TableLayoutPanel layout = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        FlowLayoutPanel botVac = BarraAcciones();
        Button agregarVac = UiTheme.CrearBoton("Aplicar vacuna", true); agregarVac.Width = 140; agregarVac.Click += (_, _) => AgregarVacuna();
        Button quitarVac = UiTheme.CrearBoton("Quitar"); quitarVac.Width = 90; quitarVac.Click += (_, _) => QuitarSeleccion(_gridVacunas, _vacunas);
        botVac.Controls.AddRange(new Control[] { agregarVac, quitarVac }); layout.Controls.Add(botVac, 0, 0);
        _gridVacunas = CrearGrid(_vacunas);
        _gridVacunas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Vacuna", HeaderText = "Vacuna" });
        _gridVacunas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Dosis", HeaderText = "Dosis" });
        _gridVacunas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "LoteTexto", HeaderText = "Lote" });
        _gridVacunas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FechaProximaDosis", HeaderText = "Próxima", DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } });
        _gridVacunas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PrecioAplicado", HeaderText = "Precio (Q)", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
        layout.Controls.Add(_gridVacunas, 0, 1);
        FlowLayoutPanel botDes = BarraAcciones();
        Button agregarDes = UiTheme.CrearBoton("Registrar desparasitación", true); agregarDes.Width = 205; agregarDes.Click += (_, _) => AgregarDesparasitacion();
        Button quitarDes = UiTheme.CrearBoton("Quitar"); quitarDes.Width = 90; quitarDes.Click += (_, _) => QuitarSeleccion(_gridDesparasitaciones, _desparasitaciones);
        botDes.Controls.AddRange(new Control[] { agregarDes, quitarDes }); layout.Controls.Add(botDes, 0, 2);
        _gridDesparasitaciones = CrearGrid(_desparasitaciones);
        _gridDesparasitaciones.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Desparasitante", HeaderText = "Producto" });
        _gridDesparasitaciones.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Dosis", HeaderText = "Dosis" });
        _gridDesparasitaciones.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PesoReferencia", HeaderText = "Peso", DefaultCellStyle = new DataGridViewCellStyle { Format = "0.00" } });
        _gridDesparasitaciones.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FechaProxima", HeaderText = "Próxima", DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } });
        _gridDesparasitaciones.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PrecioAplicado", HeaderText = "Precio (Q)", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
        layout.Controls.Add(_gridDesparasitaciones, 0, 3);
        return layout;
    }

    private Control ConstruirOrdenes()
    {
        TableLayoutPanel panel = CrearPanelConBotones(out FlowLayoutPanel botones);
        Button agregar = UiTheme.CrearBoton("Nueva orden", true); agregar.Width = 135; agregar.Click += (_, _) => AgregarOrden();
        Button quitar = UiTheme.CrearBoton("Quitar seleccionado"); quitar.Width = 155; quitar.Click += (_, _) => QuitarSeleccion(_gridOrdenes, _ordenes);
        botones.Controls.AddRange(new Control[] { agregar, quitar });
        _gridOrdenes = CrearGrid(_ordenes);
        _gridOrdenes.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TipoOrden", HeaderText = "Tipo", FillWeight = 60 });
        _gridOrdenes.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NombreEstudio", HeaderText = "Estudio", FillWeight = 115 });
        _gridOrdenes.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Motivo", HeaderText = "Motivo", FillWeight = 120 });
        _gridOrdenes.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Precio", HeaderText = "Precio (Q)", FillWeight = 55, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
        panel.Controls.Add(_gridOrdenes, 0, 1);
        return panel;
    }

    private Control ConstruirCierre()
    {
        TableLayoutPanel grid = CrearFormulario(4, 4);
        _indicaciones = CrearTexto(true);
        _proximaRevision = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", ShowCheckBox = true };
        _estadoEgreso = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _estadoEgreso.Items.AddRange(new object[] { "Estable", "Tratamiento ambulatorio", "Observación recomendada", "Hospitalización recomendada", "Referido" });
        _estadoEgreso.SelectedIndex = 0;
        Label nota = new() { Text = "Al finalizar se guardará la consulta, se crearán cargos pendientes, se actualizará la cita a Atendida y se descontará inventario controlado.", Dock = DockStyle.Fill, ForeColor = UiTheme.TextoSecundario, Padding = new Padding(0, 18, 0, 0) };
        AgregarCampo(grid, "Indicaciones al egreso *", _indicaciones, 0, 0, 3);
        AgregarCampo(grid, "Próxima revisión", _proximaRevision, 0, 1);
        AgregarCampo(grid, "Estado al egreso *", _estadoEgreso, 2, 1);
        grid.Controls.Add(nota, 0, 2); grid.SetColumnSpan(nota, 4);
        return grid;
    }

    private void CargarCola()
    {
        try
        {
            List<CitaModel> cola = _consultaService.ListarCitasEnConsulta();
            _gridCola.DataSource = null;
            _gridCola.DataSource = cola;

            if (cola.Count == 0)
            {
                LimpiarAtencion();
                _lblPaciente.Text = "No hay pacientes en consulta para el veterinario actual";
                return;
            }

            _gridCola.ClearSelection();
            _gridCola.Rows[0].Selected = true;
            _gridCola.CurrentCell = _gridCola.Rows[0].Cells[0];
            SeleccionarCita();
        }
        catch (Exception ex)
        {
            LimpiarAtencion();
            MessageBox.Show(ex.Message, "Atención clínica", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void SeleccionarCita()
    {
        if (_gridCola.CurrentRow?.DataBoundItem is not CitaModel cita)
        {
            ConfigurarBotonFinalizar(false);
            return;
        }
        try
        {
            _atencionActual = _consultaService.ObtenerEncabezado(cita.IdCita);
            _lblPaciente.Text = $"{_atencionActual.Mascota}   ·   {_atencionActual.CodigoPaciente}";
            _lblDetalle.Text = $"{_atencionActual.Especie} / {_atencionActual.Raza}   |   {_atencionActual.Sexo}   |   Edad: {_atencionActual.Edad}   |   Peso previo: {(_atencionActual.PesoAnterior.HasValue ? _atencionActual.PesoAnterior.Value.ToString("0.00") + " kg" : "N/R")}   |   Dueño: {_atencionActual.Dueno} / {_atencionActual.Telefono}";
            _lblCita.Text = $"Veterinario: {_atencionActual.Veterinario}   |   Cita: {_atencionActual.FechaHoraCita:dd/MM/yyyy HH:mm}   |   Motivo: {_atencionActual.MotivoConsulta}   |   {_atencionActual.TipoTarifa}: {_atencionActual.PrecioServicioCita:C2}";
            _lblAlertas.Text = _atencionActual.AlertasActivas > 0 ? $"⚠ ALERTAS CLÍNICAS: {_atencionActual.Alertas}" : "Sin alertas clínicas activas.";
            _lblAlertas.ForeColor = _atencionActual.AlertasActivas > 0 ? UiTheme.Peligro : UiTheme.Acento;
            PrepararBorrador();
            ConfigurarBotonFinalizar(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Atención clínica", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void PrepararBorrador()
    {
        if (_atencionActual is null) return;
        _motivo.Text = _atencionActual.MotivoConsulta;
        _anamnesis.Clear(); _peso.Value = _atencionActual.PesoAnterior ?? 0; _temperatura.Value = 0; _fc.Value = 0; _fr.Value = 0;
        _hidratacion.SelectedIndex = 0; _hallazgos.Clear(); _pronostico.Clear(); _tratamiento.Clear(); _indicaciones.Clear();
        _proximaRevision.Checked = false; _estadoEgreso.SelectedIndex = 0; _indicacionesReceta.Clear();
        _diagnosticos.Clear(); _servicios.Clear(); _recetaDetalles.Clear(); _vacunas.Clear(); _desparasitaciones.Clear(); _ordenes.Clear();
        _servicios.Add(new ConsultaServicioModel
        {
            IdServicio = _atencionActual.IdServicioCita, Servicio = _atencionActual.ServicioCita,
            Descripcion = _atencionActual.ServicioCita, Cantidad = 1, PrecioUnitario = _atencionActual.PrecioServicioCita, GeneraCargo = true
        });
    }

    private void LimpiarAtencion()
    {
        _atencionActual = null; ConfigurarBotonFinalizar(false);
        _lblDetalle.Text = string.Empty; _lblCita.Text = string.Empty; _lblAlertas.Text = "Sin paciente seleccionado";
        _diagnosticos.Clear(); _servicios.Clear(); _recetaDetalles.Clear(); _vacunas.Clear(); _desparasitaciones.Clear(); _ordenes.Clear();
    }

    private void AgregarDiagnostico()
    {
        using FormDiagnosticoDialog dialogo = new(_diagnosticos.Any(d => d.EsPrincipal));
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        if (dialogo.Resultado.EsPrincipal)
            foreach (DiagnosticoModel d in _diagnosticos) d.EsPrincipal = false;
        _diagnosticos.Add(dialogo.Resultado); _gridDiagnosticos.Refresh();
    }

    private void AgregarServicio()
    {
        using FormServicioDialog dialogo = new(_consultaService.ListarServiciosClinicos(_atencionActual?.Especie ?? "Canino"));
        if (dialogo.ShowDialog(this) == DialogResult.OK) _servicios.Add(dialogo.Resultado);
    }

    private void AgregarMedicamento()
    {
        using FormRecetaDetalleDialog dialogo = new(_recetaService.ListarMedicamentos());
        if (dialogo.ShowDialog(this) == DialogResult.OK) _recetaDetalles.Add(dialogo.Resultado);
    }

    private void AgregarVacuna()
    {
        using FormVacunaDialog dialogo = new(_vacunacionService, _vacunacionService.ListarVacunas());
        if (dialogo.ShowDialog(this) == DialogResult.OK) _vacunas.Add(dialogo.Resultado);
    }

    private void AgregarDesparasitacion()
    {
        using FormDesparasitacionDialog dialogo = new(_vacunacionService, _vacunacionService.ListarDesparasitantes(), _peso.Value > 0 ? _peso.Value : null);
        if (dialogo.ShowDialog(this) == DialogResult.OK) _desparasitaciones.Add(dialogo.Resultado);
    }

    private void AgregarOrden()
    {
        using FormOrdenDialog dialogo = new();
        if (dialogo.ShowDialog(this) == DialogResult.OK) _ordenes.Add(dialogo.Resultado);
    }

    private void ConfigurarBotonFinalizar(bool hayPacienteCargado)
    {
        // El botón permanece accionable. La validación real se realiza al presionarlo.
        // Esto evita que WinForms lo deje bloqueado visualmente tras cambios de selección.
        _btnFinalizar.Enabled = true;
        _btnFinalizar.Cursor = Cursors.Hand;
        _btnFinalizar.BackColor = hayPacienteCargado ? UiTheme.Acento : Color.FromArgb(205, 220, 217);
        _btnFinalizar.ForeColor = hayPacienteCargado ? Color.White : UiTheme.TextoSecundario;
        _btnFinalizar.FlatAppearance.BorderColor = hayPacienteCargado ? UiTheme.Acento : Color.FromArgb(205, 220, 217);
    }

    private void FinalizarConsulta()
    {
        if (_atencionActual is null)
        {
            MessageBox.Show(
                "Seleccione un paciente en consulta antes de finalizar la atención.",
                "Atención clínica",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }
        try
        {
            ConsultaCierreModel cierre = new()
            {
                Consulta = new ConsultaModel
                {
                    IdCita = _atencionActual.IdCita, IdMascota = _atencionActual.IdMascota, IdVeterinario = _atencionActual.IdVeterinario,
                    MotivoConsulta = _motivo.Text, Anamnesis = _anamnesis.Text, Peso = ValorOpcional(_peso), Temperatura = ValorOpcional(_temperatura),
                    FrecuenciaCardiaca = ValorOpcionalEntero(_fc), FrecuenciaRespiratoria = ValorOpcionalEntero(_fr), Hidratacion = _hidratacion.Text,
                    HallazgosFisicos = _hallazgos.Text, Pronostico = _pronostico.Text, TratamientoGeneral = _tratamiento.Text,
                    Indicaciones = _indicaciones.Text, ProximaRevision = _proximaRevision.Checked ? _proximaRevision.Value : null, EstadoEgreso = _estadoEgreso.Text
                },
                Diagnosticos = _diagnosticos.ToList(), Servicios = _servicios.ToList(), Vacunas = _vacunas.ToList(),
                Desparasitaciones = _desparasitaciones.ToList(), Ordenes = _ordenes.ToList(),
                Receta = _recetaDetalles.Count == 0 ? null : new RecetaModel { IndicacionesGenerales = _indicacionesReceta.Text, Detalles = _recetaDetalles.ToList() }
            };
            long idConsulta = _consultaService.FinalizarConsulta(cierre);
            MessageBox.Show($"Consulta finalizada correctamente.\nID de consulta: {idConsulta}\nLos cargos pendientes ya están disponibles para caja.", "Atención clínica", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LimpiarAtencion(); CargarCola();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "No fue posible finalizar la consulta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static decimal? ValorOpcional(NumericUpDown input) => input.Value > 0 ? input.Value : null;
    private static int? ValorOpcionalEntero(NumericUpDown input) => input.Value > 0 ? decimal.ToInt32(input.Value) : null;
    private static void AvisoPdf() => MessageBox.Show("La emisión de PDF por ID persistido se habilita en el MACROBLOQUE 5.", "Documentos PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
    private static void QuitarSeleccion<T>(DataGridView grid, BindingList<T> lista) { if (grid.CurrentRow?.DataBoundItem is T item) lista.Remove(item); }

    private static TableLayoutPanel CrearFormulario(int columnas, int filas)
    {
        TableLayoutPanel panel = new() { Dock = DockStyle.Fill, ColumnCount = columnas, RowCount = filas, AutoScroll = true, Padding = new Padding(4) };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145)); panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145)); panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (int i = 0; i < filas; i++) panel.RowStyles.Add(new RowStyle(SizeType.Absolute, i is 1 or 4 or 5 or 0 ? 70 : 50));
        return panel;
    }
    private static void AgregarCampo(TableLayoutPanel p, string etiqueta, Control control, int col, int fila, int spanControl = 1)
    {
        Label l = new() { Text = etiqueta, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = UiTheme.TextoSecundario };
        control.Margin = new Padding(4); p.Controls.Add(l, col, fila); p.Controls.Add(control, col + 1, fila); if (spanControl > 1) p.SetColumnSpan(control, spanControl);
    }
    private static TextBox CrearTexto(bool multilinea) => new() { Dock = DockStyle.Fill, Multiline = multilinea, ScrollBars = multilinea ? ScrollBars.Vertical : ScrollBars.None };
    private static NumericUpDown CrearDecimal(decimal minimo, decimal maximo, int decimales) => new() { Dock = DockStyle.Fill, DecimalPlaces = decimales, Minimum = minimo, Maximum = maximo, Increment = .1m };
    private static NumericUpDown CrearEntero(decimal minimo, decimal maximo) => new() { Dock = DockStyle.Fill, DecimalPlaces = 0, Minimum = minimo, Maximum = maximo };
    private static FlowLayoutPanel BarraAcciones() => new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 4, 0, 4), WrapContents = false };
    private static TableLayoutPanel CrearPanelConBotones(out FlowLayoutPanel botones)
    {
        TableLayoutPanel panel = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        botones = BarraAcciones();
        panel.Controls.Add(botones, 0, 0);
        return panel;
    }
    private static DataGridView CrearGrid<T>(BindingList<T> fuente)
    {
        DataGridView grid = new() { Dock = DockStyle.Fill, AutoGenerateColumns = false, DataSource = fuente, Margin = new Padding(0) };
        UiTheme.PrepararGrid(grid); grid.BringToFront(); return grid;
    }
}

internal abstract class DialogoClinicoBase : Form
{
    protected readonly TableLayoutPanel Campos = new() { Dock = DockStyle.Fill, ColumnCount = 2, AutoScroll = true, Padding = new Padding(14) };
    protected DialogoClinicoBase(string titulo, int altura)
    {
        UiTheme.PrepararFormulario(this); Text = titulo; Width = 620; Height = altura; MinimumSize = new Size(560, altura); FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        Campos.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155)); Campos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); Controls.Add(Campos);
    }
    protected void Campo(string etiqueta, Control control, int fila)
    {
        Campos.RowStyles.Add(new RowStyle(SizeType.Absolute, control is TextBox t && t.Multiline ? 70 : 43));
        Campos.Controls.Add(new Label { Text = etiqueta, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, fila); control.Dock = DockStyle.Fill; control.Margin = new Padding(4); Campos.Controls.Add(control, 1, fila);
    }
    protected void AgregarGuardar(int fila, Action guardar)
    {
        Campos.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); FlowLayoutPanel botones = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        Button aceptar = UiTheme.CrearBoton("Guardar", true); aceptar.Width = 115; aceptar.Click += (_, _) => guardar();
        Button cancelar = UiTheme.CrearBoton("Cancelar"); cancelar.Width = 115; cancelar.Click += (_, _) => DialogResult = DialogResult.Cancel;
        botones.Controls.Add(aceptar); botones.Controls.Add(cancelar); Campos.Controls.Add(botones, 0, fila); Campos.SetColumnSpan(botones, 2);
    }
}

internal sealed class FormDiagnosticoDialog : DialogoClinicoBase
{
    private readonly TextBox _descripcion = new(); private readonly TextBox _observaciones = new() { Multiline = true }; private readonly CheckBox _principal = new() { Text = "Diagnóstico principal" };
    public DiagnosticoModel Resultado { get; private set; } = new();
    public FormDiagnosticoDialog(bool existePrincipal) : base("Agregar diagnóstico", 315)
    {
        Campo("Descripción *", _descripcion, 0); Campo("Observaciones", _observaciones, 1); Campo("Clasificación", _principal, 2);
        _principal.Checked = !existePrincipal; AgregarGuardar(3, Guardar);
    }
    private void Guardar()
    {
        if (string.IsNullOrWhiteSpace(_descripcion.Text)) { MessageBox.Show("Escriba el diagnóstico."); return; }
        Resultado = new DiagnosticoModel { Descripcion = _descripcion.Text.Trim(), Observaciones = _observaciones.Text.Trim(), EsPrincipal = _principal.Checked };
        DialogResult = DialogResult.OK;
    }
}

internal sealed class FormServicioDialog : DialogoClinicoBase
{
    private readonly ComboBox _servicio = new() { DropDownStyle = ComboBoxStyle.DropDownList }; private readonly TextBox _descripcion = new();
    private readonly NumericUpDown _cantidad = new() { Minimum = 1, Maximum = 100, DecimalPlaces = 2, Value = 1 };
    private readonly NumericUpDown _precio = new() { Minimum = 0, Maximum = 1000000, DecimalPlaces = 2 };
    private readonly NumericUpDown _descuento = new() { Minimum = 0, Maximum = 1000000, DecimalPlaces = 2 };
    private readonly CheckBox _cargo = new() { Text = "Generar cargo para caja", Checked = true };
    public ConsultaServicioModel Resultado { get; private set; } = new();
    public FormServicioDialog(List<ServicioModel> servicios) : base("Servicio o procedimiento realizado", 410)
    {
        _servicio.DataSource = servicios; _servicio.DisplayMember = "Nombre"; _servicio.SelectedIndexChanged += (_, _) => Completar();
        Campo("Servicio *", _servicio, 0); Campo("Descripción *", _descripcion, 1); Campo("Cantidad", _cantidad, 2); Campo("Precio (Q, IVA incl.)", _precio, 3); Campo("Descuento", _descuento, 4); Campo("Facturación", _cargo, 5);
        AgregarGuardar(6, Guardar); Completar();
    }
    private void Completar() { if (_servicio.SelectedItem is ServicioModel s) { _descripcion.Text = s.Nombre; _precio.Value = Math.Min(_precio.Maximum, s.PrecioBase); _cargo.Checked = s.GeneraCargo; } }
    private void Guardar()
    {
        if (_servicio.SelectedItem is not ServicioModel s || string.IsNullOrWhiteSpace(_descripcion.Text)) { MessageBox.Show("Seleccione el servicio y escriba la descripción."); return; }
        Resultado = new ConsultaServicioModel { IdServicio = s.IdServicio, Servicio = s.Nombre, Descripcion = _descripcion.Text.Trim(), Cantidad = _cantidad.Value, PrecioUnitario = _precio.Value, Descuento = _descuento.Value, GeneraCargo = _cargo.Checked };
        if (Resultado.Descuento > Resultado.Cantidad * Resultado.PrecioUnitario) { MessageBox.Show("El descuento no puede superar el importe."); return; }
        DialogResult = DialogResult.OK;
    }
}

internal sealed class FormRecetaDetalleDialog : DialogoClinicoBase
{
    private readonly ComboBox _medicamento = new() { DropDownStyle = ComboBoxStyle.DropDownList }; private readonly CheckBox _libre = new() { Text = "Medicamento libre" };
    private readonly TextBox _nombreLibre = new(); private readonly TextBox _presentacion = new(); private readonly TextBox _concentracion = new(); private readonly TextBox _dosis = new();
    private readonly ComboBox _frecuencia = new() { DropDownStyle = ComboBoxStyle.DropDown }; private readonly TextBox _duracion = new(); private readonly TextBox _cantidad = new();
    private readonly TextBox _via = new(); private readonly TextBox _indicaciones = new() { Multiline = true };
    public RecetaDetalleModel Resultado { get; private set; } = new();
    public FormRecetaDetalleDialog(List<MedicamentoModel> medicamentos) : base("Medicamento de receta", 650)
    {
        _medicamento.DataSource = medicamentos; _medicamento.DisplayMember = "Nombre"; _medicamento.SelectedIndexChanged += (_, _) => Completar();
        _libre.CheckedChanged += (_, _) => AlternarLibre(); _frecuencia.Items.AddRange(new object[] { "Cada 8 horas", "Cada 12 horas", "Cada 24 horas", "Cada 48 horas", "Dosis única", "Uso tópico", "Según necesidad" });
        Campo("Catálogo", _medicamento, 0); Campo("Modo", _libre, 1); Campo("Nombre libre", _nombreLibre, 2); Campo("Presentación", _presentacion, 3); Campo("Concentración", _concentracion, 4);
        Campo("Dosis *", _dosis, 5); Campo("Frecuencia *", _frecuencia, 6); Campo("Duración *", _duracion, 7); Campo("Cantidad", _cantidad, 8); Campo("Vía", _via, 9); Campo("Indicaciones", _indicaciones, 10);
        AgregarGuardar(11, Guardar); Completar(); AlternarLibre();
    }
    private void AlternarLibre() { _medicamento.Enabled = !_libre.Checked; _nombreLibre.Enabled = _libre.Checked; if (!_libre.Checked) Completar(); }
    private void Completar() { if (_medicamento.SelectedItem is MedicamentoModel m && !_libre.Checked) { _presentacion.Text = m.Presentacion; _concentracion.Text = m.Concentracion; _via.Text = m.ViaAdministracion; _indicaciones.Text = m.IndicacionesPredeterminadas; } }
    private void Guardar()
    {
        MedicamentoModel? m = _medicamento.SelectedItem as MedicamentoModel;
        if ((_libre.Checked && string.IsNullOrWhiteSpace(_nombreLibre.Text)) || (!_libre.Checked && m is null) || string.IsNullOrWhiteSpace(_dosis.Text) || string.IsNullOrWhiteSpace(_frecuencia.Text) || string.IsNullOrWhiteSpace(_duracion.Text))
        { MessageBox.Show("Complete medicamento, dosis, frecuencia y duración."); return; }
        Resultado = new RecetaDetalleModel { IdMedicamento = _libre.Checked ? null : m!.IdMedicamento, Medicamento = _libre.Checked ? string.Empty : m!.Nombre, MedicamentoLibre = _libre.Checked ? _nombreLibre.Text.Trim() : string.Empty, Presentacion = _presentacion.Text.Trim(), Concentracion = _concentracion.Text.Trim(), Dosis = _dosis.Text.Trim(), Frecuencia = _frecuencia.Text.Trim(), Duracion = _duracion.Text.Trim(), Cantidad = _cantidad.Text.Trim(), ViaAdministracion = _via.Text.Trim(), Indicaciones = _indicaciones.Text.Trim() };
        DialogResult = DialogResult.OK;
    }
}

internal sealed class FormVacunaDialog : DialogoClinicoBase
{
    private readonly VacunacionService _service; private readonly ComboBox _vacuna = new() { DropDownStyle = ComboBoxStyle.DropDownList }; private readonly ComboBox _lote = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _laboratorio = new(); private readonly TextBox _dosis = new() { Text = "1 dosis" }; private readonly DateTimePicker _fecha = new() { Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker _proxima = new() { Format = DateTimePickerFormat.Short, ShowCheckBox = true }; private readonly NumericUpDown _precio = new() { Minimum = 0, Maximum = 1000000, DecimalPlaces = 2 };
    private readonly TextBox _observaciones = new() { Multiline = true };
    public VacunaAplicadaModel Resultado { get; private set; } = new();
    public FormVacunaDialog(VacunacionService service, List<VacunaModel> vacunas) : base("Vacuna aplicada", 560)
    {
        _service = service; _vacuna.DataSource = vacunas; _vacuna.DisplayMember = "Nombre"; _vacuna.SelectedIndexChanged += (_, _) => ActualizarVacuna();
        Campo("Vacuna *", _vacuna, 0); Campo("Lote disponible", _lote, 1); Campo("Laboratorio", _laboratorio, 2); Campo("Dosis *", _dosis, 3); Campo("Fecha aplicación", _fecha, 4); Campo("Próxima dosis", _proxima, 5); Campo("Precio (Q, IVA incl.)", _precio, 6); Campo("Observaciones", _observaciones, 7); AgregarGuardar(8, Guardar); ActualizarVacuna();
    }
    private void ActualizarVacuna()
    {
        if (_vacuna.SelectedItem is not VacunaModel v) return;
        _precio.Value = Math.Min(_precio.Maximum, v.PrecioBase); _lote.DataSource = _service.ListarLotesDisponibles(v.IdProductoInventario); _lote.DisplayMember = "Descripcion";
        if (v.IntervaloDiasSugerido.HasValue) { _proxima.Checked = true; _proxima.Value = _fecha.Value.Date.AddDays(v.IntervaloDiasSugerido.Value); }
    }
    private void Guardar()
    {
        if (_vacuna.SelectedItem is not VacunaModel v || string.IsNullOrWhiteSpace(_dosis.Text)) { MessageBox.Show("Seleccione la vacuna y escriba la dosis."); return; }
        LoteDisponibleModel? lote = _lote.SelectedItem as LoteDisponibleModel;
        if (v.ControlaInventario && v.IdProductoInventario.HasValue && lote is null) { MessageBox.Show("La vacuna controla inventario; seleccione un lote disponible."); return; }
        Resultado = new VacunaAplicadaModel { IdVacuna = v.IdVacuna, Vacuna = v.Nombre, IdLoteInventario = lote?.IdLote, LoteTexto = lote?.NumeroLote ?? string.Empty, FechaVencimientoLote = lote?.FechaVencimiento, Laboratorio = _laboratorio.Text.Trim(), Dosis = _dosis.Text.Trim(), FechaAplicacion = _fecha.Value, FechaProximaDosis = _proxima.Checked ? _proxima.Value.Date : null, Observaciones = _observaciones.Text.Trim(), PrecioAplicado = _precio.Value };
        DialogResult = DialogResult.OK;
    }
}

internal sealed class FormDesparasitacionDialog : DialogoClinicoBase
{
    private readonly VacunacionService _service; private readonly ComboBox _producto = new() { DropDownStyle = ComboBoxStyle.DropDownList }; private readonly ComboBox _lote = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _dosis = new(); private readonly NumericUpDown _peso = new() { Minimum = 0, Maximum = 250, DecimalPlaces = 2 }; private readonly DateTimePicker _fecha = new() { Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker _proxima = new() { Format = DateTimePickerFormat.Short, ShowCheckBox = true }; private readonly NumericUpDown _precio = new() { Minimum = 0, Maximum = 1000000, DecimalPlaces = 2 };
    private readonly TextBox _observaciones = new() { Multiline = true };
    public DesparasitacionModel Resultado { get; private set; } = new();
    public FormDesparasitacionDialog(VacunacionService service, List<DesparasitanteModel> productos, decimal? pesoReferencia) : base("Desparasitación aplicada", 560)
    {
        _service = service; _producto.DataSource = productos; _producto.DisplayMember = "Nombre"; _producto.SelectedIndexChanged += (_, _) => ActualizarProducto(); if (pesoReferencia.HasValue) _peso.Value = pesoReferencia.Value;
        Campo("Producto *", _producto, 0); Campo("Lote disponible", _lote, 1); Campo("Dosis *", _dosis, 2); Campo("Peso referencia", _peso, 3); Campo("Fecha aplicación", _fecha, 4); Campo("Próxima fecha", _proxima, 5); Campo("Precio (Q, IVA incl.)", _precio, 6); Campo("Observaciones", _observaciones, 7); AgregarGuardar(8, Guardar); ActualizarProducto();
    }
    private void ActualizarProducto()
    {
        if (_producto.SelectedItem is not DesparasitanteModel d) return;
        _dosis.Text = d.DosisSugerida; _precio.Value = Math.Min(_precio.Maximum, d.PrecioBase); _lote.DataSource = _service.ListarLotesDisponibles(d.IdProductoInventario); _lote.DisplayMember = "Descripcion";
        if (d.IntervaloDiasSugerido.HasValue) { _proxima.Checked = true; _proxima.Value = _fecha.Value.Date.AddDays(d.IntervaloDiasSugerido.Value); }
    }
    private void Guardar()
    {
        if (_producto.SelectedItem is not DesparasitanteModel d || string.IsNullOrWhiteSpace(_dosis.Text)) { MessageBox.Show("Seleccione el producto y registre la dosis."); return; }
        LoteDisponibleModel? lote = _lote.SelectedItem as LoteDisponibleModel;
        if (d.ControlaInventario && d.IdProductoInventario.HasValue && lote is null) { MessageBox.Show("El producto controla inventario; seleccione un lote disponible."); return; }
        Resultado = new DesparasitacionModel { IdDesparasitante = d.IdDesparasitante, Desparasitante = d.Nombre, IdLoteInventario = lote?.IdLote, Dosis = _dosis.Text.Trim(), PesoReferencia = _peso.Value > 0 ? _peso.Value : null, FechaAplicacion = _fecha.Value, FechaProxima = _proxima.Checked ? _proxima.Value.Date : null, Observaciones = _observaciones.Text.Trim(), PrecioAplicado = _precio.Value };
        DialogResult = DialogResult.OK;
    }
}

internal sealed class FormOrdenDialog : DialogoClinicoBase
{
    private readonly ComboBox _tipo = new() { DropDownStyle = ComboBoxStyle.DropDownList }; private readonly TextBox _estudio = new(); private readonly TextBox _motivo = new(); private readonly TextBox _observaciones = new() { Multiline = true }; private readonly NumericUpDown _precio = new() { Minimum = 0, Maximum = 1000000, DecimalPlaces = 2 };
    public OrdenClinicaModel Resultado { get; private set; } = new();
    public FormOrdenDialog() : base("Orden clínica", 410)
    {
        _tipo.Items.AddRange(new object[] { "Laboratorio", "Imagen", "Otro estudio" }); _tipo.SelectedIndex = 0;
        Campo("Tipo *", _tipo, 0); Campo("Nombre estudio *", _estudio, 1); Campo("Motivo", _motivo, 2); Campo("Observaciones", _observaciones, 3); Campo("Precio", _precio, 4); AgregarGuardar(5, Guardar);
    }
    private void Guardar()
    {
        if (string.IsNullOrWhiteSpace(_estudio.Text)) { MessageBox.Show("Escriba el nombre del estudio."); return; }
        Resultado = new OrdenClinicaModel { TipoOrden = _tipo.Text, NombreEstudio = _estudio.Text.Trim(), Motivo = _motivo.Text.Trim(), Observaciones = _observaciones.Text.Trim(), Precio = _precio.Value };
        DialogResult = DialogResult.OK;
    }
}
