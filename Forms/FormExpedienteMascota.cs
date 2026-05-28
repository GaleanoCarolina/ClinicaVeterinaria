using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Services;
using ClinicaVeterinaria.Utils;

namespace ClinicaVeterinaria.Forms;

public sealed class FormExpedienteMascota : Form
{
    private readonly ExpedienteService _servicio = new();
    private readonly PdfService _pdfService = new();
    private readonly long? _idMascotaInicial;
    private ExpedienteEncabezadoModel? _paciente;
    private TextBox _buscar = null!;
    private DataGridView _gridPacientes = null!;
    private Label _lblPaciente = null!;
    private Label _lblDatos = null!;
    private Label _lblAlertas = null!;
    private Label _lblCuenta = null!;
    private TabControl _tabs = null!;
    private DataGridView _gridTimeline = null!;
    private DataGridView _gridConsultas = null!;
    private DataGridView _gridDiagnosticos = null!;
    private DataGridView _gridRecetas = null!;
    private DataGridView _gridVacunas = null!;
    private DataGridView _gridDesparasitaciones = null!;
    private DataGridView _gridOrdenes = null!;
    private DataGridView _gridHospitalizaciones = null!;
    private DataGridView _gridCitas = null!;
    private DataGridView _gridFacturas = null!;
    private Button _btnExpedientePdf = null!;
    private Button _btnCarnetPdf = null!;
    private Button _btnEstadoCuentaPdf = null!;
    private Button _btnResumenConsultaPdf = null!;
    private Button _btnRecetaPdf = null!;

    public FormExpedienteMascota(long? idMascotaInicial = null)
    {
        _idMascotaInicial = idMascotaInicial;
        ConstruirInterfaz();
        Shown += (_, _) => Inicializar();
    }

    private void ConstruirInterfaz()
    {
        UiTheme.PrepararFormulario(this);
        Text = "Expediente médico";
        BackColor = UiTheme.Fondo;
        Padding = new Padding(8);

        TableLayoutPanel principal = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = UiTheme.Fondo,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        principal.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        principal.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
        principal.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(principal);
        principal.Controls.Add(ConstruirCabecera(), 0, 0);
        principal.Controls.Add(ConstruirFicha(), 0, 1);
        principal.Controls.Add(ConstruirCuerpo(), 0, 2);
    }

    private Control ConstruirCabecera()
    {
        Panel panel = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16, 13, 16, 10), Margin = new Padding(0, 0, 0, 8) };
        Label titulo = UiTheme.CrearTitulo("Expediente médico");
        titulo.Location = new Point(16, 7);
        panel.Controls.Add(titulo);
        _buscar = new TextBox { Location = new Point(330, 15), Width = 360, PlaceholderText = "Paciente, dueño, código, teléfono o microchip" };
        _buscar.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) BuscarPacientes(); };
        panel.Controls.Add(_buscar);
        Button buscar = UiTheme.CrearBoton("Buscar", true); buscar.Width = 95; buscar.Location = new Point(700, 10); buscar.Click += (_, _) => BuscarPacientes(); panel.Controls.Add(buscar);
        Button limpiar = UiTheme.CrearBoton("Limpiar"); limpiar.Width = 94; limpiar.Location = new Point(803, 10); limpiar.Click += (_, _) => { _buscar.Clear(); BuscarPacientes(); }; panel.Controls.Add(limpiar);
        return panel;
    }

    private Control ConstruirFicha()
    {
        Panel panel = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18, 10, 18, 10), Margin = new Padding(0, 0, 0, 8) };
        _lblPaciente = new Label { Text = "Seleccione un paciente", AutoSize = true, Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold), ForeColor = UiTheme.Primario, Location = new Point(18, 10) };
        _lblDatos = new Label { Text = string.Empty, AutoSize = true, ForeColor = UiTheme.TextoSecundario, Location = new Point(20, 46) };
        _lblCuenta = new Label { Text = string.Empty, AutoSize = true, ForeColor = UiTheme.Primario, Location = new Point(20, 71) };
        _lblAlertas = new Label { Text = string.Empty, AutoSize = false, Width = 600, Height = 48, ForeColor = UiTheme.Peligro, Font = UiTheme.FuenteSubtitulo, Location = new Point(690, 12) };
        panel.Controls.Add(_lblPaciente); panel.Controls.Add(_lblDatos); panel.Controls.Add(_lblCuenta); panel.Controls.Add(_lblAlertas);
        return panel;
    }

    private Control ConstruirCuerpo()
    {
        TableLayoutPanel cuerpo = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = UiTheme.Fondo, Margin = new Padding(0), Padding = new Padding(0) };
        cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));
        cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        cuerpo.Controls.Add(ConstruirListaPacientes(), 0, 0);
        cuerpo.Controls.Add(ConstruirDetalle(), 1, 0);
        return cuerpo;
    }

    private Control ConstruirListaPacientes()
    {
        TableLayoutPanel panel = new() { Dock = DockStyle.Fill, BackColor = Color.White, RowCount = 2, ColumnCount = 1, Padding = new Padding(12), Margin = new Padding(0, 0, 8, 0) };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { Text = "Pacientes encontrados", Dock = DockStyle.Fill, Font = UiTheme.FuenteSubtitulo, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        _gridPacientes = CrearGrid();
        _gridPacientes.AutoGenerateColumns = false;
        _gridPacientes.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CodigoPaciente", HeaderText = "Código", FillWeight = 34 });
        _gridPacientes.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Mascota", HeaderText = "Paciente", FillWeight = 45 });
        _gridPacientes.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Especie", HeaderText = "Especie", FillWeight = 30 });
        _gridPacientes.SelectionChanged += (_, _) => SeleccionarPacienteActual();
        panel.Controls.Add(_gridPacientes, 0, 1);
        return panel;
    }

    private Control ConstruirDetalle()
    {
        TableLayoutPanel contenedor = new() { Dock = DockStyle.Fill, BackColor = Color.White, ColumnCount = 1, RowCount = 2, Margin = new Padding(0), Padding = new Padding(8) };
        contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        FlowLayoutPanel acciones = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, BackColor = Color.White, Padding = new Padding(0, 3, 0, 6) };
        _btnExpedientePdf = CrearBotonPdf("Expediente PDF", GenerarExpedientePdf, true);
        _btnCarnetPdf = CrearBotonPdf("Carnet vacunas", GenerarCarnetPdf);
        _btnEstadoCuentaPdf = CrearBotonPdf("Estado de cuenta", GenerarEstadoCuentaPdf);
        _btnResumenConsultaPdf = CrearBotonPdf("Resumen consulta", GenerarResumenConsultaPdf);
        _btnRecetaPdf = CrearBotonPdf("Receta seleccionada", GenerarRecetaPdf);
        acciones.Controls.AddRange(new Control[] { _btnExpedientePdf, _btnCarnetPdf, _btnEstadoCuentaPdf, _btnResumenConsultaPdf, _btnRecetaPdf });
        contenedor.Controls.Add(acciones, 0, 0);
        _tabs = new TabControl { Dock = DockStyle.Fill };
        _gridTimeline = AgregarTabGrid("Línea de tiempo", new[] { ("Fecha", "Fecha", 18), ("Tipo", "Tipo", 18), ("Descripcion", "Descripción", 42), ("ProfesionalEstado", "Profesional / estado", 26) });
        _gridConsultas = AgregarTabGrid("Consultas", new[] { ("FechaAtencion", "Fecha", 18), ("Veterinario", "Veterinario", 24), ("MotivoConsulta", "Motivo", 30), ("DiagnosticoPrincipal", "Diagnóstico", 34), ("EstadoEgreso", "Egreso", 20) });
        _gridDiagnosticos = AgregarTabGrid("Diagnósticos", new[] { ("Descripcion", "Diagnóstico", 55), ("EsPrincipal", "Principal", 13), ("Observaciones", "Observaciones", 40) });
        _gridRecetas = AgregarTabGrid("Recetas", new[] { ("FechaEmision", "Fecha", 20), ("Veterinario", "Veterinario", 28), ("DiagnosticoPrincipal", "Diagnóstico", 38), ("CantidadMedicamentos", "Medicamentos", 18) });
        _gridVacunas = AgregarTabGrid("Vacunas", new[] { ("FechaAplicacion", "Aplicación", 16), ("Vacuna", "Vacuna", 25), ("Lote", "Lote", 20), ("ProximaDosis", "Próxima", 18), ("Veterinario", "Veterinario", 25) });
        _gridDesparasitaciones = AgregarTabGrid("Desparasitación", new[] { ("FechaAplicacion", "Aplicación", 16), ("Producto", "Producto", 30), ("Dosis", "Dosis", 20), ("ProximaAplicacion", "Próxima", 17), ("Veterinario", "Veterinario", 25) });
        _gridOrdenes = AgregarTabGrid("Órdenes", new[] { ("FechaSolicitud", "Solicitud", 15), ("TipoOrden", "Tipo", 14), ("NombreEstudio", "Estudio", 25), ("Estado", "Estado", 16), ("FechaResultado", "Resultado fecha", 16), ("ResultadoTexto", "Resultado", 32), ("Precio", "Precio", 12) });
        _gridHospitalizaciones = AgregarTabGrid("Hospitalización", new[] { ("FechaIngreso", "Ingreso", 18), ("FechaAlta", "Alta", 18), ("Veterinario", "Veterinario", 25), ("Motivo", "Motivo", 34), ("Estado", "Estado", 16) });
        _gridCitas = AgregarTabGrid("Citas", new[] { ("FechaHoraInicio", "Fecha", 20), ("Veterinario", "Veterinario", 25), ("Servicio", "Servicio", 25), ("Estado", "Estado", 16), ("MotivoConsulta", "Motivo", 34) });
        _gridFacturas = AgregarTabGrid("Facturación", new[] { ("NumeroFactura", "Factura", 24), ("FechaEmision", "Fecha", 18), ("Total", "Total", 17), ("TotalPagado", "Pagado", 17), ("SaldoPendiente", "Saldo", 17), ("Estado", "Estado", 18) });
        contenedor.Controls.Add(_tabs, 0, 1);
        HabilitarBotones(false);
        return contenedor;
    }

    private DataGridView AgregarTabGrid(string titulo, (string Propiedad, string Titulo, int Peso)[] columnas)
    {
        TabPage tab = new(titulo) { BackColor = Color.White, Padding = new Padding(8) };
        DataGridView grid = CrearGrid();
        foreach ((string propiedad, string texto, int peso) in columnas)
        {
            DataGridViewTextBoxColumn columna = new() { DataPropertyName = propiedad, HeaderText = texto, FillWeight = peso };
            if (propiedad.Contains("Fecha", StringComparison.OrdinalIgnoreCase)) columna.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            if (propiedad is "Total" or "TotalPagado" or "SaldoPendiente" or "Precio") columna.DefaultCellStyle.Format = "C2";
            grid.Columns.Add(columna);
        }
        grid.AutoGenerateColumns = false;
        tab.Controls.Add(grid);
        _tabs.TabPages.Add(tab);
        return grid;
    }

    private static DataGridView CrearGrid()
    {
        DataGridView grid = new() { Dock = DockStyle.Fill, Margin = new Padding(0) };
        UiTheme.PrepararGrid(grid);
        return grid;
    }

    private Button CrearBotonPdf(string texto, Action accion, bool principal = false)
    {
        Button boton = UiTheme.CrearBoton(texto, principal); boton.Width = 145; boton.Click += (_, _) => accion(); return boton;
    }

    private void Inicializar()
    {
        BuscarPacientes();
        if (_idMascotaInicial.HasValue)
        {
            CargarPaciente(_idMascotaInicial.Value);
        }
    }

    private void BuscarPacientes()
    {
        try
        {
            _gridPacientes.DataSource = _servicio.BuscarMascotas(_buscar.Text);
            if (!_idMascotaInicial.HasValue && _gridPacientes.Rows.Count > 0) _gridPacientes.Rows[0].Selected = true;
        }
        catch (Exception ex) { MostrarError("No fue posible consultar pacientes.", ex); }
    }

    private void SeleccionarPacienteActual()
    {
        if (_gridPacientes.CurrentRow?.DataBoundItem is MascotaExpedienteBusquedaModel item) CargarPaciente(item.IdMascota);
    }

    private void CargarPaciente(long idMascota)
    {
        try
        {
            _paciente = _servicio.ObtenerEncabezado(idMascota);
            _lblPaciente.Text = $"{_paciente.Mascota}  ·  {_paciente.CodigoPaciente}";
            _lblDatos.Text = $"{_paciente.Especie} / {_paciente.Raza}  |  {_paciente.Sexo}  |  Edad: {_paciente.EdadTexto}  |  Peso: {(_paciente.PesoActual.HasValue ? $"{_paciente.PesoActual:0.00} kg" : "No registrado")}  |  Dueño: {_paciente.Dueno} / {_paciente.Telefono}";
            _lblCuenta.Text = $"Estado vital: {_paciente.EstadoVital}  |  Próxima vacuna: {(_paciente.ProximaVacuna?.ToString("dd/MM/yyyy") ?? "Sin programar")}  |  Saldo: {_paciente.SaldoPendiente:C2}";
            _lblAlertas.Text = _paciente.AlertasActivas.Count == 0 ? "Sin alertas clínicas activas." : "⚠ ALERTAS: " + string.Join(" | ", _paciente.AlertasActivas.Select(a => $"{a.TipoAlerta}: {a.Descripcion}"));
            _lblAlertas.ForeColor = _paciente.AlertasActivas.Count == 0 ? UiTheme.TextoSecundario : UiTheme.Peligro;
            _gridTimeline.DataSource = _servicio.ListarLineaTiempo(idMascota);
            _gridConsultas.DataSource = _servicio.ListarConsultas(idMascota);
            _gridDiagnosticos.DataSource = _servicio.ListarDiagnosticosMascota(idMascota);
            _gridRecetas.DataSource = _servicio.ListarRecetas(idMascota);
            _gridVacunas.DataSource = _servicio.ListarVacunas(idMascota);
            _gridDesparasitaciones.DataSource = _servicio.ListarDesparasitaciones(idMascota);
            _gridOrdenes.DataSource = _servicio.ListarOrdenes(idMascota);
            _gridHospitalizaciones.DataSource = _servicio.ListarHospitalizaciones(idMascota);
            _gridCitas.DataSource = _servicio.ListarCitas(idMascota);
            _gridFacturas.DataSource = _servicio.ListarFacturas(idMascota);
            HabilitarBotones(true);
        }
        catch (Exception ex) { MostrarError("No fue posible cargar el expediente.", ex); }
    }

    private void HabilitarBotones(bool habilitar)
    {
        _btnExpedientePdf.Enabled = habilitar; _btnCarnetPdf.Enabled = habilitar; _btnEstadoCuentaPdf.Enabled = habilitar;
        _btnResumenConsultaPdf.Enabled = habilitar; _btnRecetaPdf.Enabled = habilitar;
    }

    private void GenerarExpedientePdf() => Exportar("Expediente", ruta => _pdfService.GenerarExpediente(ExigirPaciente(), ruta));
    private void GenerarCarnetPdf() => Exportar("CarnetVacunacion", ruta => _pdfService.GenerarCarnetVacunacion(ExigirPaciente(), ruta));
    private void GenerarEstadoCuentaPdf() => Exportar("EstadoCuenta", ruta => _pdfService.GenerarEstadoCuenta(ExigirPaciente(), ruta));
    private void GenerarResumenConsultaPdf()
    {
        if (_gridConsultas.CurrentRow?.DataBoundItem is not ExpedienteConsultaResumenModel consulta)
        {
            MessageBox.Show("Seleccione una consulta para generar su resumen PDF.", "PDF", MessageBoxButtons.OK, MessageBoxIcon.Information); return;
        }
        Exportar($"ResumenConsulta_{consulta.IdConsulta}", ruta => _pdfService.GenerarResumenConsulta(consulta.IdConsulta, ruta));
    }
    private void GenerarRecetaPdf()
    {
        if (_gridRecetas.CurrentRow?.DataBoundItem is not ExpedienteRecetaResumenModel receta)
        {
            MessageBox.Show("Seleccione una receta para reimprimirla.", "PDF", MessageBoxButtons.OK, MessageBoxIcon.Information); return;
        }
        Exportar($"Receta_{receta.IdReceta}", ruta => _pdfService.GenerarReceta(receta.IdReceta, ruta));
    }

    private long ExigirPaciente() => _paciente?.IdMascota ?? throw new InvalidOperationException("Seleccione un paciente.");

    private void Exportar(string nombreBase, Func<string, string> generar)
    {
        if (_paciente is null) return;
        using SaveFileDialog dialogo = new() { Filter = "Documento PDF (*.pdf)|*.pdf", FileName = $"{nombreBase}_{_paciente.CodigoPaciente}_{DateTime.Now:yyyyMMdd}.pdf" };
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            string ruta = generar(dialogo.FileName);
            DialogResult abrir = MessageBox.Show($"PDF generado correctamente:\n{ruta}\n\n¿Desea abrirlo ahora?", "Documento generado", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (abrir == DialogResult.Yes) Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true });
        }
        catch (Exception ex) { MostrarError("No fue posible generar el PDF.", ex); }
    }

    private static void MostrarError(string mensaje, Exception ex) => MessageBox.Show($"{mensaje}\n\n{ex.Message}", "Expediente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
