using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Services;
using ClinicaVeterinaria.Utils;

namespace ClinicaVeterinaria.Forms;

public sealed class FormReportes : Form
{
    private readonly ReporteService _servicio = new();
    private readonly PdfService _pdf = new();
    private ComboBox _cmbReporte = null!;
    private DateTimePicker _desde = null!;
    private DateTimePicker _hasta = null!;
    private TextBox _buscar = null!;
    private DataGridView _grid = null!;
    private Label _lblCitas = null!;
    private Label _lblConsultas = null!;
    private Label _lblCobrado = null!;
    private Label _lblSaldos = null!;
    private Label _lblStock = null!;
    private Label _lblIndicador = null!;
    private Button _btnExportar = null!;
    private ReporteResultadoModel? _resultadoActual;

    public FormReportes()
    {
        ConstruirInterfaz();
        CargarReportes();
        Consultar();
    }

    private void ConstruirInterfaz()
    {
        UiTheme.PrepararFormulario(this);
        Text = "Reportes";

        TableLayoutPanel raiz = new()
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Fondo,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 86F));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        Controls.Add(raiz);

        Panel cabecera = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(22, 12, 16, 10) };
        Label titulo = UiTheme.CrearTitulo("Reportes");
        titulo.Location = new Point(0, 15);
        cabecera.Controls.Add(titulo);
        Label detalle = new()
        {
            Text = "Análisis operativo, clínico, financiero e inventario con exportación PDF.",
            ForeColor = UiTheme.TextoSecundario,
            AutoSize = true,
            Location = new Point(130, 25)
        };
        cabecera.Controls.Add(detalle);
        raiz.Controls.Add(cabecera, 0, 0);

        Panel filtros = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18, 15, 18, 12), Margin = new Padding(0, 8, 0, 0) };
        FlowLayoutPanel flujo = new() { Dock = DockStyle.Fill, WrapContents = false, FlowDirection = FlowDirection.LeftToRight };
        filtros.Controls.Add(flujo);
        flujo.Controls.Add(Etiqueta("Reporte"));
        _cmbReporte = new ComboBox { Width = 265, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(4, 4, 18, 0) };
        flujo.Controls.Add(_cmbReporte);
        flujo.Controls.Add(Etiqueta("Desde"));
        _desde = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 112, Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), Margin = new Padding(4, 4, 16, 0) };
        flujo.Controls.Add(_desde);
        flujo.Controls.Add(Etiqueta("Hasta"));
        _hasta = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 112, Value = DateTime.Today, Margin = new Padding(4, 4, 16, 0) };
        flujo.Controls.Add(_hasta);
        flujo.Controls.Add(Etiqueta("Buscar"));
        _buscar = new TextBox { Width = 220, Margin = new Padding(4, 6, 14, 0) };
        flujo.Controls.Add(_buscar);
        Button consultar = UiTheme.CrearBoton("Consultar", true); consultar.Width = 102; consultar.Click += (_, _) => Consultar(); flujo.Controls.Add(consultar);
        _btnExportar = UiTheme.CrearBoton("Exportar PDF"); _btnExportar.Width = 122; _btnExportar.Enabled = false; _btnExportar.Click += (_, _) => ExportarPdf(); flujo.Controls.Add(_btnExportar);
        raiz.Controls.Add(filtros, 0, 1);

        TableLayoutPanel tarjetas = new() { Dock = DockStyle.Fill, ColumnCount = 5, Padding = new Padding(0, 10, 0, 10) };
        for (int i = 0; i < 5; i++) tarjetas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        _lblCitas = AgregarTarjeta(tarjetas, 0, "Citas periodo");
        _lblConsultas = AgregarTarjeta(tarjetas, 1, "Consultas");
        _lblCobrado = AgregarTarjeta(tarjetas, 2, "Total cobrado");
        _lblSaldos = AgregarTarjeta(tarjetas, 3, "Saldos pendientes");
        _lblStock = AgregarTarjeta(tarjetas, 4, "Stock bajo");
        raiz.Controls.Add(tarjetas, 0, 2);

        TableLayoutPanel contenido = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12), Margin = new Padding(0, 8, 0, 0), ColumnCount = 1, RowCount = 2 };
        contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        Panel cabeceraResultado = new() { Dock = DockStyle.Fill, BackColor = Color.White };
        Label seccion = new() { Text = "Resultado", Font = UiTheme.FuenteSubtitulo, ForeColor = UiTheme.Primario, AutoSize = true, Location = new Point(2, 10) };
        cabeceraResultado.Controls.Add(seccion);
        _lblIndicador = new Label { Text = "", ForeColor = UiTheme.TextoSecundario, AutoSize = true, Location = new Point(88, 12) };
        cabeceraResultado.Controls.Add(_lblIndicador);
        contenido.Controls.Add(cabeceraResultado, 0, 0);
        _grid = new DataGridView { Dock = DockStyle.Fill, Margin = new Padding(0) };
        UiTheme.PrepararGrid(_grid);
        contenido.Controls.Add(_grid, 0, 1);
        raiz.Controls.Add(contenido, 0, 3);
    }

    private static Label Etiqueta(string texto) => new()
    {
        Text = texto,
        AutoSize = true,
        Margin = new Padding(0, 10, 4, 0),
        ForeColor = UiTheme.TextoSecundario
    };

    private static Label AgregarTarjeta(TableLayoutPanel panel, int columna, string titulo)
    {
        Panel tarjeta = new() { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(14, 10, 14, 10) };
        Label encabezado = new() { Text = titulo, Dock = DockStyle.Top, Height = 25, ForeColor = UiTheme.TextoSecundario };
        Label valor = new() { Text = "0", Dock = DockStyle.Fill, ForeColor = UiTheme.Primario, Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
        tarjeta.Controls.Add(valor); tarjeta.Controls.Add(encabezado);
        panel.Controls.Add(tarjeta, columna, 0);
        return valor;
    }

    private void CargarReportes()
    {
        try
        {
            List<ReporteDefinicionModel> tipos = _servicio.ListarReportesDisponibles();
            _cmbReporte.DataSource = tipos;
            _cmbReporte.DisplayMember = "Nombre";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void Consultar()
    {
        try
        {
            ReporteResumenModel resumen = _servicio.ObtenerResumen(_desde.Value.Date, _hasta.Value.Date);
            _lblCitas.Text = SesionActual.EsRol("Caja") ? "-" : resumen.Citas.ToString();
            _lblConsultas.Text = SesionActual.EsRol("Caja") ? "-" : resumen.Consultas.ToString();
            _lblCobrado.Text = resumen.Cobrado.ToString("C2");
            _lblSaldos.Text = resumen.SaldosPendientes.ToString("C2");
            _lblStock.Text = SesionActual.EsRol("Caja") ? "-" : resumen.StockBajo.ToString();

            if (_cmbReporte.SelectedItem is not ReporteDefinicionModel seleccionado) return;
            _resultadoActual = _servicio.Generar(new ReporteFiltroModel
            {
                TipoReporte = seleccionado.Nombre,
                Desde = _desde.Value.Date,
                Hasta = _hasta.Value.Date,
                Buscar = _buscar.Text.Trim()
            });
            _grid.DataSource = CrearTabla(_resultadoActual);
            _lblIndicador.Text = $"|  {_resultadoActual.IndicadorNombre}: {_resultadoActual.IndicadorValor}  |  {_resultadoActual.DescripcionPeriodo}";
            _btnExportar.Enabled = true;
        }
        catch (Exception ex)
        {
            _btnExportar.Enabled = false;
            MessageBox.Show(ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static DataTable CrearTabla(ReporteResultadoModel reporte)
    {
        DataTable tabla = new();
        foreach (string columna in reporte.Columnas) tabla.Columns.Add(columna);
        foreach (List<string> fila in reporte.Filas) tabla.Rows.Add(fila.ToArray());
        return tabla;
    }

    private void ExportarPdf()
    {
        if (_resultadoActual is null) return;
        using SaveFileDialog dialogo = new()
        {
            Filter = "Documento PDF (*.pdf)|*.pdf",
            FileName = $"Reporte_{_resultadoActual.Titulo.Replace(' ', '_')}_{DateTime.Now:yyyyMMdd}.pdf"
        };
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            _pdf.GenerarReporteOperativo(_resultadoActual, dialogo.FileName);
            MessageBox.Show("Reporte PDF generado correctamente.", "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No fue posible generar el reporte PDF.\n\n{ex.Message}", "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
