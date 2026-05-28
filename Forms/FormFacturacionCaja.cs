using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Services;
using ClinicaVeterinaria.Utils;

namespace ClinicaVeterinaria.Forms;

public sealed class FormFacturacionCaja : Form
{
    private readonly FacturacionService _servicio = new();
    private readonly PdfService _pdf = new();
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private DataGridView _gridCargos = null!;
    private DataGridView _gridFacturas = null!;
    private DataGridView _gridDetalles = null!;
    private DataGridView _gridPagos = null!;
    private DataGridView _gridMetodosCaja = null!;
    private DataGridView _gridPagosCaja = null!;
    private TextBox _filtroCargo = null!;
    private TextBox _filtroFactura = null!;
    private ComboBox _estadoFactura = null!;
    private DateTimePicker _inicioFacturas = null!;
    private DateTimePicker _finFacturas = null!;
    private DateTimePicker _fechaCaja = null!;
    private Label _lblCargos = null!;
    private Label _lblCajaCobrado = null!;
    private Label _lblCajaFacturas = null!;
    private Label _lblCajaSaldos = null!;
    private FacturaModel? _facturaActual;

    public FormFacturacionCaja()
    {
        UiTheme.PrepararFormulario(this);
        Dock = DockStyle.Fill;
        ConstruirInterfaz();
        CargarInicial();
    }

    private void ConstruirInterfaz()
    {
        TableLayoutPanel layout = new() { Dock = DockStyle.Fill, BackColor = UiTheme.Fondo, RowCount = 2, ColumnCount = 1, Padding = new Padding(0) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(layout);
        Panel encabezado = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20, 12, 20, 8) };
        Label titulo = UiTheme.CrearTitulo("Facturación y caja"); titulo.Location = new Point(20, 10); encabezado.Controls.Add(titulo);
        Label subtitulo = new() { Text = "Cargos, facturas y caja en quetzales. Precios con IVA Guatemala incluido (12%).", AutoSize = true, ForeColor = UiTheme.TextoSecundario, Location = new Point(22, 42) };
        encabezado.Controls.Add(subtitulo);
        layout.Controls.Add(encabezado, 0, 0);
        _tabs.TabPages.Add(ConstruirTabCargos());
        _tabs.TabPages.Add(ConstruirTabFacturas());
        _tabs.TabPages.Add(ConstruirTabCaja());
        layout.Controls.Add(_tabs, 0, 1);
    }

    private TabPage ConstruirTabCargos()
    {
        TabPage tab = new("Cargos pendientes") { BackColor = Color.White, Padding = new Padding(14) };
        TableLayoutPanel panel = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        FlowLayoutPanel filtros = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 6, 0, 4) };
        _filtroCargo = new TextBox { Width = 290, PlaceholderText = "Dueño, paciente o descripción", Margin = new Padding(0, 5, 8, 0) };
        Button buscar = UiTheme.CrearBoton("Buscar", true); buscar.Width = 92; buscar.Click += (_, _) => CargarCargos();
        Button limpiar = UiTheme.CrearBoton("Limpiar"); limpiar.Width = 92; limpiar.Click += (_, _) => { _filtroCargo.Clear(); CargarCargos(); };
        _lblCargos = new Label { AutoSize = false, Width = 400, Height = 40, TextAlign = ContentAlignment.MiddleLeft, ForeColor = UiTheme.Primario, Font = UiTheme.FuenteSubtitulo, Margin = new Padding(22, 2, 0, 0) };
        filtros.Controls.AddRange(new Control[] { _filtroCargo, buscar, limpiar, _lblCargos });
        panel.Controls.Add(filtros, 0, 0);
        _gridCargos = CrearGrid(); _gridCargos.MultiSelect = true;
        _gridCargos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FechaCreacion", HeaderText = "Fecha", FillWeight = 17, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" } });
        _gridCargos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Dueno", HeaderText = "Dueño", FillWeight = 26 });
        _gridCargos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Mascota", HeaderText = "Paciente", FillWeight = 20 });
        _gridCargos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TipoItem", HeaderText = "Tipo", FillWeight = 17 });
        _gridCargos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Descripcion", HeaderText = "Descripción", FillWeight = 42 });
        _gridCargos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Subtotal", HeaderText = "Total (Q, IVA incl.)", FillWeight = 16, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
        panel.Controls.Add(_gridCargos, 0, 1);
        FlowLayoutPanel acciones = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 5, 0, 0) };
        Button factura = UiTheme.CrearBoton("Generar factura", true); factura.Width = 155; factura.Click += (_, _) => CrearFacturaSeleccionada();
        Button actualizar = UiTheme.CrearBoton("Actualizar"); actualizar.Width = 105; actualizar.Click += (_, _) => CargarCargos();
        acciones.Controls.AddRange(new Control[] { factura, actualizar }); panel.Controls.Add(acciones, 0, 2);
        tab.Controls.Add(panel); return tab;
    }

    private TabPage ConstruirTabFacturas()
    {
        TabPage tab = new("Facturas y pagos") { BackColor = Color.White, Padding = new Padding(14) };
        TableLayoutPanel panel = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 56)); panel.RowStyles.Add(new RowStyle(SizeType.Percent, 48)); panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); panel.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
        FlowLayoutPanel filtros = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 6, 0, 4) };
        _inicioFacturas = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 112, Value = DateTime.Today.AddDays(-30), Margin = new Padding(0, 5, 6, 0) };
        _finFacturas = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 112, Value = DateTime.Today, Margin = new Padding(0, 5, 10, 0) };
        _estadoFactura = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 175, Margin = new Padding(0, 5, 10, 0) };
        _estadoFactura.Items.AddRange(new object[] { "Todos", "Emitida", "Parcialmente pagada", "Pagada", "Anulada" }); _estadoFactura.SelectedIndex = 0;
        _filtroFactura = new TextBox { Width = 260, PlaceholderText = "Factura, dueño o paciente", Margin = new Padding(0, 5, 8, 0) };
        Button buscar = UiTheme.CrearBoton("Buscar", true); buscar.Width = 90; buscar.Click += (_, _) => CargarFacturas();
        filtros.Controls.Add(new Label { Text = "Desde", AutoSize = true, Margin = new Padding(0, 12, 5, 0) }); filtros.Controls.Add(_inicioFacturas);
        filtros.Controls.Add(new Label { Text = "Hasta", AutoSize = true, Margin = new Padding(0, 12, 5, 0) }); filtros.Controls.Add(_finFacturas);
        filtros.Controls.Add(_estadoFactura); filtros.Controls.Add(_filtroFactura); filtros.Controls.Add(buscar); panel.Controls.Add(filtros, 0, 0);
        _gridFacturas = CrearGrid();
        _gridFacturas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NumeroFactura", HeaderText = "Factura", FillWeight = 20 });
        _gridFacturas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FechaEmision", HeaderText = "Emisión", FillWeight = 16, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" } });
        _gridFacturas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Dueno", HeaderText = "Dueño", FillWeight = 25 });
        _gridFacturas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Mascota", HeaderText = "Paciente", FillWeight = 18 });
        foreach (string campo in new[] { "Total", "TotalPagado", "SaldoPendiente" }) _gridFacturas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = campo, HeaderText = campo == "TotalPagado" ? "Pagado" : campo == "SaldoPendiente" ? "Saldo" : campo, FillWeight = 15, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
        _gridFacturas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Estado", HeaderText = "Estado", FillWeight = 20 });
        _gridFacturas.SelectionChanged += (_, _) => CargarDetalleFacturaSeleccionada(); panel.Controls.Add(_gridFacturas, 0, 1);
        FlowLayoutPanel acciones = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 5, 0, 3) };
        Button pago = UiTheme.CrearBoton("Registrar pago", true); pago.Width = 135; pago.Click += (_, _) => RegistrarPago();
        Button anularPago = UiTheme.CrearBoton("Anular pago"); anularPago.Width = 122; anularPago.Click += (_, _) => AnularPago();
        Button anularFactura = UiTheme.CrearBoton("Anular factura"); anularFactura.Width = 128; anularFactura.Click += (_, _) => AnularFactura();
        Button pdfFactura = UiTheme.CrearBoton("Factura PDF"); pdfFactura.Width = 112; pdfFactura.Click += (_, _) => GenerarFacturaPdf();
        Button pdfRecibo = UiTheme.CrearBoton("Recibo PDF"); pdfRecibo.Width = 110; pdfRecibo.Click += (_, _) => GenerarReciboPdf();
        acciones.Controls.AddRange(new Control[] { pago, anularPago, anularFactura, pdfFactura, pdfRecibo }); panel.Controls.Add(acciones, 0, 2);
        TableLayoutPanel detalle = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
        detalle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 53F)); detalle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 47F));
        _gridDetalles = CrearGrid();
        _gridDetalles.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TipoItem", HeaderText = "Tipo", FillWeight = 20 }); _gridDetalles.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Descripcion", HeaderText = "Detalle", FillWeight = 44 });
        _gridDetalles.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Cantidad", HeaderText = "Cant.", FillWeight = 13 }); _gridDetalles.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Subtotal", HeaderText = "Total (Q, IVA incl.)", FillWeight = 18, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
        _gridPagos = CrearGrid();
        _gridPagos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FechaPago", HeaderText = "Fecha", FillWeight = 22, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" } }); _gridPagos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "MetodoPago", HeaderText = "Método", FillWeight = 22 });
        _gridPagos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Monto", HeaderText = "Monto", FillWeight = 18, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } }); _gridPagos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Estado", HeaderText = "Estado", FillWeight = 18 });
        detalle.Controls.Add(_gridDetalles, 0, 0); detalle.Controls.Add(_gridPagos, 1, 0); panel.Controls.Add(detalle, 0, 3); tab.Controls.Add(panel); return tab;
    }

    private TabPage ConstruirTabCaja()
    {
        TabPage tab = new("Caja diaria") { BackColor = Color.White, Padding = new Padding(14) };
        TableLayoutPanel panel = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 55)); panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); panel.RowStyles.Add(new RowStyle(SizeType.Percent, 38)); panel.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
        FlowLayoutPanel encabezado = new() { Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 5) };
        encabezado.Controls.Add(new Label { Text = "Fecha de caja", AutoSize = true, Margin = new Padding(0, 12, 8, 0) });
        _fechaCaja = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 130, Value = DateTime.Today, Margin = new Padding(0, 5, 8, 0) };
        Button actualizar = UiTheme.CrearBoton("Actualizar", true); actualizar.Width = 108; actualizar.Click += (_, _) => CargarCaja();
        Button exportar = UiTheme.CrearBoton("Exportar PDF"); exportar.Width = 122; exportar.Click += (_, _) => GenerarCajaPdf();
        encabezado.Controls.AddRange(new Control[] { _fechaCaja, actualizar, exportar }); panel.Controls.Add(encabezado, 0, 0);
        FlowLayoutPanel tarjetas = new() { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 8) };
        _lblCajaCobrado = CrearTarjeta(); _lblCajaFacturas = CrearTarjeta(); _lblCajaSaldos = CrearTarjeta(); tarjetas.Controls.AddRange(new Control[] { _lblCajaCobrado, _lblCajaFacturas, _lblCajaSaldos }); panel.Controls.Add(tarjetas, 0, 1);
        _gridMetodosCaja = CrearGrid(); _gridMetodosCaja.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "MetodoPago", HeaderText = "Método de pago", FillWeight = 45 }); _gridMetodosCaja.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CantidadPagos", HeaderText = "Pagos", FillWeight = 20 }); _gridMetodosCaja.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Total", HeaderText = "Total", FillWeight = 25, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } }); panel.Controls.Add(_gridMetodosCaja, 0, 2);
        _gridPagosCaja = CrearGrid(); _gridPagosCaja.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FechaPago", HeaderText = "Hora", FillWeight = 16, DefaultCellStyle = new DataGridViewCellStyle { Format = "HH:mm" } }); _gridPagosCaja.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NumeroFactura", HeaderText = "Factura", FillWeight = 26 }); _gridPagosCaja.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "MetodoPago", HeaderText = "Método", FillWeight = 22 }); _gridPagosCaja.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Monto", HeaderText = "Monto", FillWeight = 18, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } }); _gridPagosCaja.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Estado", HeaderText = "Estado", FillWeight = 18 }); panel.Controls.Add(_gridPagosCaja, 0, 3); tab.Controls.Add(panel); return tab;
    }

    private static DataGridView CrearGrid() { DataGridView grid = new() { Dock = DockStyle.Fill, AutoGenerateColumns = false, Margin = new Padding(0) }; UiTheme.PrepararGrid(grid); return grid; }
    private static Label CrearTarjeta() => new() { Width = 270, Height = 62, Margin = new Padding(0, 0, 12, 0), Padding = new Padding(12), BackColor = UiTheme.Fondo, Font = UiTheme.FuenteSubtitulo, ForeColor = UiTheme.Primario, TextAlign = ContentAlignment.MiddleLeft };

    private void CargarInicial()
    {
        try { CargarCargos(); CargarFacturas(); CargarCaja(); }
        catch (Exception ex) { MostrarError("No fue posible cargar facturación y caja.", ex); }
    }
    private void CargarCargos()
    {
        List<CargoPendienteModel> items = _servicio.ListarCargosPendientes(_filtroCargo.Text); _gridCargos.DataSource = items; _lblCargos.Text = $"Pendientes: {items.Count}  |  Total: {items.Sum(x => x.Subtotal):C2}";
    }
    private void CargarFacturas()
    {
        _gridFacturas.DataSource = _servicio.ListarFacturas(_inicioFacturas.Value, _finFacturas.Value, _estadoFactura.SelectedItem?.ToString() ?? "Todos", _filtroFactura.Text); CargarDetalleFacturaSeleccionada();
    }
    private void CargarDetalleFacturaSeleccionada()
    {
        if (_gridFacturas.CurrentRow?.DataBoundItem is not FacturaModel seleccionada) { _facturaActual = null; _gridDetalles.DataSource = null; _gridPagos.DataSource = null; return; }
        _facturaActual = _servicio.ObtenerFactura(seleccionada.IdFactura); _gridDetalles.DataSource = _facturaActual.Detalles; _gridPagos.DataSource = _facturaActual.Pagos;
    }
    private void CargarCaja()
    {
        CajaResumenModel caja = _servicio.ObtenerCajaDiaria(_fechaCaja.Value.Date); _lblCajaCobrado.Text = $"Cobrado del día\n{caja.TotalCobrado:C2}"; _lblCajaFacturas.Text = $"Facturas emitidas: {caja.FacturasEmitidas}\nPagadas: {caja.FacturasPagadas}  Parciales: {caja.FacturasParciales}"; _lblCajaSaldos.Text = $"Saldos pendientes\n{caja.SaldosPendientesGenerados:C2}"; _gridMetodosCaja.DataSource = caja.TotalesPorMetodo; _gridPagosCaja.DataSource = caja.PagosDelDia;
    }

    private void CrearFacturaSeleccionada()
    {
        List<CargoPendienteModel> seleccion = _gridCargos.SelectedRows.Cast<DataGridViewRow>().Select(r => r.DataBoundItem).OfType<CargoPendienteModel>().ToList();
        if (seleccion.Count == 0) { Aviso("Seleccione uno o más cargos pendientes."); return; }
        using FormCrearFactura dialogo = new(seleccion);
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            FacturaModel factura = _servicio.CrearFactura(dialogo.Solicitud);
            MessageBox.Show($"Factura {factura.NumeroFactura} emitida correctamente.", "Facturación", MessageBoxButtons.OK, MessageBoxIcon.Information);
            CargarCargos(); CargarFacturas(); _tabs.SelectedIndex = 1;
        }
        catch (Exception ex) { MostrarError("No fue posible crear la factura.", ex); }
    }
    private void RegistrarPago()
    {
        if (_facturaActual is null) { Aviso("Seleccione una factura."); return; }
        using FormRegistrarPago dialogo = new(_facturaActual, _servicio.ListarMetodosPago());
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        try { PagoModel pago = _servicio.RegistrarPago(dialogo.Solicitud); MessageBox.Show($"Pago por {pago.Monto:C2} registrado correctamente.", "Caja", MessageBoxButtons.OK, MessageBoxIcon.Information); CargarFacturas(); CargarCaja(); }
        catch (Exception ex) { MostrarError("No fue posible registrar el pago.", ex); }
    }
    private void AnularPago()
    {
        if (_gridPagos.CurrentRow?.DataBoundItem is not PagoModel pago) { Aviso("Seleccione un pago."); return; }
        string? motivo = SolicitarMotivo("Motivo de anulación del pago"); if (motivo is null) return;
        try { _servicio.AnularPago(pago.IdPago, motivo); CargarFacturas(); CargarCaja(); }
        catch (Exception ex) { MostrarError("No fue posible anular el pago.", ex); }
    }
    private void AnularFactura()
    {
        if (_facturaActual is null) { Aviso("Seleccione una factura."); return; }
        string? motivo = SolicitarMotivo("Motivo de anulación de factura"); if (motivo is null) return;
        try { _servicio.AnularFactura(_facturaActual.IdFactura, motivo); CargarFacturas(); CargarCargos(); CargarCaja(); }
        catch (Exception ex) { MostrarError("No fue posible anular la factura.", ex); }
    }
    private void GenerarFacturaPdf() { if (_facturaActual is null) { Aviso("Seleccione una factura."); return; } GuardarPdf($"Factura_{_facturaActual.NumeroFactura}.pdf", ruta => _pdf.GenerarFactura(_facturaActual.IdFactura, ruta)); }
    private void GenerarReciboPdf() { if (_gridPagos.CurrentRow?.DataBoundItem is not PagoModel pago) { Aviso("Seleccione un pago para imprimir su recibo."); return; } GuardarPdf($"Recibo_{pago.NumeroFactura}_{pago.IdPago}.pdf", ruta => _pdf.GenerarRecibo(pago.IdPago, ruta)); }
    private void GenerarCajaPdf() { GuardarPdf($"Caja_{_fechaCaja.Value:yyyyMMdd}.pdf", ruta => _pdf.GenerarReporteCaja(_fechaCaja.Value.Date, ruta)); }
    private void GuardarPdf(string nombre, Func<string, string> generar)
    {
        using SaveFileDialog dialogo = new() { Filter = "Documento PDF (*.pdf)|*.pdf", FileName = nombre };
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        try { generar(dialogo.FileName); MessageBox.Show("Documento PDF generado correctamente.", "PDF", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { MostrarError("No fue posible generar el PDF.", ex); }
    }
    private static string? SolicitarMotivo(string titulo) { using FormMotivo dialogo = new(titulo); return dialogo.ShowDialog() == DialogResult.OK ? dialogo.Motivo : null; }
    private static void Aviso(string mensaje) => MessageBox.Show(mensaje, "Facturación y caja", MessageBoxButtons.OK, MessageBoxIcon.Information);
    private static void MostrarError(string mensaje, Exception ex) => MessageBox.Show($"{mensaje}\n\n{ex.Message}", "Facturación y caja", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}

internal sealed class FormCrearFactura : Form
{
    private readonly NumericUpDown _descuento = new() { DecimalPlaces = 2, Maximum = 9999999, Width = 140 };
    private readonly TextBox _observaciones = new() { Multiline = true, Width = 405, Height = 65 };
    private readonly decimal _subtotal;
    private readonly Label _baseImponible = new() { AutoSize = true, ForeColor = UiTheme.TextoSecundario };
    private readonly Label _ivaIncluido = new() { AutoSize = true, ForeColor = UiTheme.TextoSecundario };
    private readonly Label _total = new() { AutoSize = true, Font = UiTheme.FuenteSubtitulo, ForeColor = UiTheme.Primario };
    public CrearFacturaRequestModel Solicitud { get; private set; } = new();
    public FormCrearFactura(List<CargoPendienteModel> cargos)
    {
        _subtotal = cargos.Sum(x => x.Subtotal); Solicitud.IdsCargos = cargos.Select(x => x.IdCargo).ToList();
        UiTheme.PrepararFormulario(this); Text = "Generar factura"; Width = 525; Height = 405; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
        Label titulo = UiTheme.CrearTitulo("Emitir factura"); titulo.Location = new Point(24, 18); Controls.Add(titulo);
        Controls.Add(new Label { Text = $"Cargos seleccionados: {cargos.Count}   Total listado: {_subtotal:C2}", AutoSize = true, Location = new Point(26, 62) });
        Controls.Add(new Label { Text = FiscalGuatemala.LeyendaPrecios, AutoSize = true, Location = new Point(26, 86), ForeColor = UiTheme.TextoSecundario });
        AgregarCampo("Descuento total", _descuento, 119); AgregarCampo("Observaciones", _observaciones, 159);
        _baseImponible.Location = new Point(185, 242); Controls.Add(_baseImponible);
        _ivaIncluido.Location = new Point(185, 267); Controls.Add(_ivaIncluido);
        _total.Location = new Point(185, 295); Controls.Add(_total); _descuento.ValueChanged += (_, _) => ActualizarTotal(); ActualizarTotal();
        Button guardar = UiTheme.CrearBoton("Emitir factura", true); guardar.Width = 135; guardar.Location = new Point(345, 337); guardar.Click += (_, _) => Confirmar(); Controls.Add(guardar);
        Button cancelar = UiTheme.CrearBoton("Cancelar"); cancelar.Width = 100; cancelar.Location = new Point(235, 337); cancelar.DialogResult = DialogResult.Cancel; Controls.Add(cancelar); CancelButton = cancelar;
    }
    private void AgregarCampo(string etiqueta, Control control, int y) { Controls.Add(new Label { Text = etiqueta, AutoSize = true, Location = new Point(26, y + 7) }); control.Location = new Point(185, y); Controls.Add(control); }
    private void ActualizarTotal()
    {
        decimal total = FiscalGuatemala.TotalConIva(_subtotal, _descuento.Value);
        decimal iva = FiscalGuatemala.CalcularIvaIncluido(total);
        decimal baseSinIva = FiscalGuatemala.CalcularBaseImponible(total);
        _baseImponible.Text = $"Base imponible sin IVA: {baseSinIva:C2}";
        _ivaIncluido.Text = $"IVA incluido (12%): {iva:C2}";
        _total.Text = $"Total a pagar: {total:C2}";
    }
    private void Confirmar() { if (_descuento.Value > _subtotal) { MessageBox.Show("El descuento no puede exceder el subtotal."); return; } Solicitud.DescuentoTotal = _descuento.Value; Solicitud.Observaciones = _observaciones.Text.Trim(); DialogResult = DialogResult.OK; Close(); }
}

internal sealed class FormRegistrarPago : Form
{
    private readonly ComboBox _metodo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
    private readonly NumericUpDown _monto = new() { DecimalPlaces = 2, Maximum = 9999999, Width = 150 };
    private readonly TextBox _referencia = new() { Width = 240 };
    private readonly TextBox _observaciones = new() { Multiline = true, Width = 240, Height = 55 };
    private readonly FacturaModel _factura;
    public RegistrarPagoRequestModel Solicitud { get; private set; } = new();
    public FormRegistrarPago(FacturaModel factura, List<MetodoPagoModel> metodos)
    {
        _factura = factura; UiTheme.PrepararFormulario(this); Text = "Registrar pago"; Width = 460; Height = 375; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
        Label titulo = UiTheme.CrearTitulo("Registrar pago"); titulo.Location = new Point(24, 18); Controls.Add(titulo);
        Controls.Add(new Label { Text = $"{factura.NumeroFactura}  |  Saldo pendiente: {factura.SaldoPendiente:C2}", AutoSize = true, Location = new Point(26, 62), ForeColor = UiTheme.Primario });
        _metodo.DataSource = metodos; _metodo.DisplayMember = "Nombre"; _metodo.ValueMember = "IdMetodoPago"; _monto.Maximum = factura.SaldoPendiente; _monto.Value = factura.SaldoPendiente;
        AgregarCampo("Método", _metodo, 100); AgregarCampo("Monto", _monto, 140); AgregarCampo("Referencia", _referencia, 180); AgregarCampo("Observaciones", _observaciones, 220);
        Button guardar = UiTheme.CrearBoton("Registrar", true); guardar.Width = 115; guardar.Location = new Point(306, 305); guardar.Click += (_, _) => Confirmar(); Controls.Add(guardar);
        Button cancelar = UiTheme.CrearBoton("Cancelar"); cancelar.Width = 105; cancelar.Location = new Point(190, 305); cancelar.DialogResult = DialogResult.Cancel; Controls.Add(cancelar); CancelButton = cancelar;
    }
    private void AgregarCampo(string texto, Control control, int y) { Controls.Add(new Label { Text = texto, AutoSize = true, Location = new Point(26, y + 7) }); control.Location = new Point(166, y); Controls.Add(control); }
    private void Confirmar() { if (_monto.Value <= 0) { MessageBox.Show("Ingrese un monto válido."); return; } if (_metodo.SelectedItem is not MetodoPagoModel metodo) { MessageBox.Show("Seleccione un método de pago."); return; } Solicitud = new RegistrarPagoRequestModel { IdFactura = _factura.IdFactura, IdMetodoPago = metodo.IdMetodoPago, Monto = _monto.Value, Referencia = _referencia.Text.Trim(), Observaciones = _observaciones.Text.Trim() }; DialogResult = DialogResult.OK; Close(); }
}

internal sealed class FormMotivo : Form
{
    private readonly TextBox _texto = new() { Multiline = true, Width = 360, Height = 90 };
    public string Motivo => _texto.Text.Trim();
    public FormMotivo(string titulo)
    {
        UiTheme.PrepararFormulario(this); Text = titulo; Width = 430; Height = 250; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
        Controls.Add(new Label { Text = titulo, AutoSize = true, Location = new Point(24, 22), Font = UiTheme.FuenteSubtitulo }); _texto.Location = new Point(24, 60); Controls.Add(_texto);
        Button aceptar = UiTheme.CrearBoton("Aceptar", true); aceptar.Width = 100; aceptar.Location = new Point(285, 165); aceptar.Click += (_, _) => { if (string.IsNullOrWhiteSpace(_texto.Text)) { MessageBox.Show("El motivo es obligatorio."); return; } DialogResult = DialogResult.OK; Close(); }; Controls.Add(aceptar);
        Button cancelar = UiTheme.CrearBoton("Cancelar"); cancelar.Width = 100; cancelar.Location = new Point(177, 165); cancelar.DialogResult = DialogResult.Cancel; Controls.Add(cancelar); CancelButton = cancelar;
    }
}
