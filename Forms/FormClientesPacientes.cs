using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Services;
using ClinicaVeterinaria.Utils;

namespace ClinicaVeterinaria.Forms;

public sealed class FormClientesPacientes : Form
{
    private readonly ClientePacienteService _servicio = new();
    private readonly bool _puedeEditar = SesionActual.EsRol("Administrador", "Recepción");
    private readonly bool _puedeGestionarAlertas = SesionActual.EsRol("Administrador", "Recepción", "Veterinario");

    private TextBox _txtBuscar = null!;
    private DataGridView _gridDuenos = null!;
    private DataGridView _gridMascotas = null!;
    private DataGridView _gridAlertas = null!;
    private DataGridView _gridVacunas = null!;
    private DataGridView _gridFacturas = null!;
    private Label _lblDueno = null!;
    private Label _lblContacto = null!;
    private Label _lblDireccion = null!;
    private Label _lblCuenta = null!;
    private Label _lblMascotaResumen = null!;
    private Label _lblAlertaVisible = null!;
    private PictureBox _fotoMascota = null!;
    private Button _btnEditarDueno = null!;
    private Button _btnNuevaMascota = null!;
    private Button _btnEditarMascota = null!;
    private Button _btnNuevaAlerta = null!;
    private Button _btnCerrarAlerta = null!;
    private DuenoModel? _duenoActual;
    private MascotaModel? _mascotaActual;
    private readonly Action<long>? _abrirAgenda;
    private readonly Action<long>? _abrirExpediente;
    private readonly Action<long>? _exportarHistorial;

    public FormClientesPacientes(
        Action<long>? abrirAgenda = null,
        Action<long>? abrirExpediente = null,
        Action<long>? exportarHistorial = null)
    {
        _abrirAgenda = abrirAgenda;
        _abrirExpediente = abrirExpediente;
        _exportarHistorial = exportarHistorial;
        ConstruirInterfaz();
        ConfigurarEventos();
        CargarDuenos();
    }

    private void ConstruirInterfaz()
    {
        UiTheme.PrepararFormulario(this);
        BackColor = UiTheme.Fondo;
        Padding = new Padding(10);

        TableLayoutPanel estructura = new()
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Fondo,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        estructura.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        estructura.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
        estructura.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
        estructura.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        Controls.Add(estructura);

        Panel cabecera = new()
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Fondo,
            Margin = new Padding(0)
        };
        estructura.Controls.Add(cabecera, 0, 0);

        Label titulo = UiTheme.CrearTitulo("Clientes y pacientes");
        titulo.Location = new Point(4, 6);
        cabecera.Controls.Add(titulo);

        Label subtitulo = new()
        {
            Text = "Ficha integrada del dueño, mascotas, alertas, vacunas y saldos.",
            AutoSize = true,
            Location = new Point(7, 40),
            ForeColor = UiTheme.TextoSecundario
        };
        cabecera.Controls.Add(subtitulo);

        Panel buscador = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(15, 9, 15, 9)
        };
        estructura.Controls.Add(buscador, 0, 1);

        _txtBuscar = new TextBox
        {
            Width = 380,
            Height = 34,
            Location = new Point(16, 10),
            PlaceholderText = "Dueño, teléfono, documento, mascota, código o microchip"
        };
        buscador.Controls.Add(_txtBuscar);

        Button btnBuscar = UiTheme.CrearBoton("Buscar", true);
        btnBuscar.Width = 96;
        btnBuscar.Location = new Point(407, 7);
        btnBuscar.Click += (_, _) => CargarDuenos();
        buscador.Controls.Add(btnBuscar);

        Button btnLimpiar = UiTheme.CrearBoton("Limpiar");
        btnLimpiar.Width = 92;
        btnLimpiar.Location = new Point(510, 7);
        btnLimpiar.Click += (_, _) => { _txtBuscar.Clear(); CargarDuenos(); };
        buscador.Controls.Add(btnLimpiar);

        Button btnNuevoDueno = UiTheme.CrearBoton("Nuevo dueño", true);
        btnNuevoDueno.Width = 130;
        btnNuevoDueno.Location = new Point(620, 7);
        btnNuevoDueno.Enabled = _puedeEditar;
        btnNuevoDueno.Click += (_, _) => NuevoDueno();
        buscador.Controls.Add(btnNuevoDueno);

        TableLayoutPanel cuerpo = new()
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Fondo,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 12, 0, 0),
            Padding = new Padding(0)
        };
        cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360F));
        cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        cuerpo.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        estructura.Controls.Add(cuerpo, 0, 2);

        Panel panelListado = new()
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Fondo,
            Margin = new Padding(0, 0, 12, 0),
            Padding = new Padding(0)
        };
        Panel panelFicha = new()
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Fondo,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        cuerpo.Controls.Add(panelListado, 0, 0);
        cuerpo.Controls.Add(panelFicha, 1, 0);

        ConstruirPanelListado(panelListado);
        ConstruirPanelFicha(panelFicha);
    }

    private void ConstruirPanelListado(Panel panel)
    {
        Panel tarjeta = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(12)
        };
        panel.Controls.Add(tarjeta);

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.White
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tarjeta.Controls.Add(layout);

        Label titulo = new()
        {
            Text = "Dueños encontrados",
            Dock = DockStyle.Fill,
            Font = UiTheme.FuenteSubtitulo,
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(titulo, 0, 0);

        _gridDuenos = new DataGridView { Dock = DockStyle.Fill, Margin = new Padding(0) };
        UiTheme.PrepararGrid(_gridDuenos);
        _gridDuenos.AutoGenerateColumns = false;
        _gridDuenos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CodigoCliente", HeaderText = "Código", FillWeight = 70 });
        _gridDuenos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NombreCompleto", HeaderText = "Dueño", FillWeight = 130 });
        _gridDuenos.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TelefonoPrincipal", HeaderText = "Teléfono", FillWeight = 92 });
        layout.Controls.Add(_gridDuenos, 0, 1);
    }

    private void ConstruirPanelFicha(Panel panel)
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = UiTheme.Fondo
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 128F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 224F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        panel.Controls.Add(layout);

        Panel fichaDueno = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(18, 13, 18, 10)
        };
        layout.Controls.Add(fichaDueno, 0, 0);

        _lblDueno = new Label
        {
            Text = "Seleccione un dueño",
            Location = new Point(18, 12),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
            ForeColor = UiTheme.Primario
        };
        fichaDueno.Controls.Add(_lblDueno);
        _lblContacto = CrearEtiquetaFicha(new Point(20, 48));
        _lblDireccion = CrearEtiquetaFicha(new Point(20, 73));
        _lblCuenta = CrearEtiquetaFicha(new Point(20, 97));
        fichaDueno.Controls.Add(_lblContacto);
        fichaDueno.Controls.Add(_lblDireccion);
        fichaDueno.Controls.Add(_lblCuenta);

        FlowLayoutPanel botonesFicha = new()
        {
            Dock = DockStyle.Right,
            Width = 282,
            Padding = new Padding(0, 2, 0, 0),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.White
        };
        fichaDueno.Controls.Add(botonesFicha);
        botonesFicha.BringToFront();

        _btnEditarDueno = UiTheme.CrearBoton("Editar dueño");
        _btnEditarDueno.Width = 118;
        _btnEditarDueno.Enabled = false;
        _btnEditarDueno.Margin = new Padding(0, 0, 8, 0);
        _btnEditarDueno.Click += (_, _) => EditarDueno();
        botonesFicha.Controls.Add(_btnEditarDueno);

        _btnNuevaMascota = UiTheme.CrearBoton("Nueva mascota", true);
        _btnNuevaMascota.Width = 130;
        _btnNuevaMascota.Enabled = false;
        _btnNuevaMascota.Margin = new Padding(0);
        _btnNuevaMascota.Click += (_, _) => NuevaMascota();
        botonesFicha.Controls.Add(_btnNuevaMascota);

        Panel mascotasPanel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(18, 10, 18, 12)
        };
        layout.Controls.Add(mascotasPanel, 0, 1);

        TableLayoutPanel mascotasLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.White
        };
        mascotasLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mascotasLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        mascotasLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mascotasPanel.Controls.Add(mascotasLayout);

        Label mascotasTitulo = new()
        {
            Text = "Mascotas asociadas",
            Dock = DockStyle.Fill,
            Font = UiTheme.FuenteSubtitulo,
            TextAlign = ContentAlignment.MiddleLeft
        };
        mascotasLayout.Controls.Add(mascotasTitulo, 0, 0);

        _gridMascotas = new DataGridView { Dock = DockStyle.Fill, Margin = new Padding(0) };
        UiTheme.PrepararGrid(_gridMascotas);
        _gridMascotas.AutoGenerateColumns = false;
        _gridMascotas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CodigoPaciente", HeaderText = "Código", FillWeight = 72 });
        _gridMascotas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Nombre", HeaderText = "Nombre", FillWeight = 100 });
        _gridMascotas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Especie", HeaderText = "Especie", FillWeight = 72 });
        _gridMascotas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Raza", HeaderText = "Raza", FillWeight = 85 });
        _gridMascotas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EdadTexto", HeaderText = "Edad", FillWeight = 76 });
        _gridMascotas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PesoActual", HeaderText = "Peso kg", FillWeight = 65, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        _gridMascotas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "AlertasActivas", HeaderText = "Alertas", FillWeight = 62 });
        _gridMascotas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SaldoPendiente", HeaderText = "Saldo", FillWeight = 75, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
        mascotasLayout.Controls.Add(_gridMascotas, 0, 1);

        FlowLayoutPanel acciones = new()
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Fondo,
            Margin = new Padding(0),
            Padding = new Padding(0, 8, 0, 8),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        layout.Controls.Add(acciones, 0, 2);

        _btnEditarMascota = AgregarBotonAccion(acciones, "Editar mascota", 0, EditarMascota);
        Button btnCrearCita = AgregarBotonAccion(acciones, "Crear cita", 0, AccionCrearCita, principal: true);
        Button btnExpediente = AgregarBotonAccion(acciones, "Expediente", 0, AccionExpediente);
        Button btnExportar = AgregarBotonAccion(acciones, "Exportar historial", 0, AccionExportar);
        _btnEditarMascota.Enabled = false;
        btnCrearCita.Enabled = false;
        btnExpediente.Enabled = false;
        btnExportar.Enabled = false;
        btnCrearCita.Tag = "requiereMascota";
        btnExpediente.Tag = "requiereMascota";
        btnExportar.Tag = "requiereMascota";

        TabControl tabs = new()
        {
            Dock = DockStyle.Fill,
            Font = UiTheme.FuenteNormal,
            Margin = new Padding(0)
        };
        layout.Controls.Add(tabs, 0, 3);

        TabPage tabResumen = new("Resumen") { BackColor = Color.White, Padding = new Padding(16) };
        TabPage tabAlertas = new("Alertas clínicas") { BackColor = Color.White, Padding = new Padding(12) };
        TabPage tabVacunas = new("Vacunas") { BackColor = Color.White, Padding = new Padding(12) };
        TabPage tabFacturas = new("Facturas y saldos") { BackColor = Color.White, Padding = new Padding(12) };
        tabs.TabPages.AddRange(new[] { tabResumen, tabAlertas, tabVacunas, tabFacturas });
        ConstruirResumenMascota(tabResumen);
        ConstruirAlertas(tabAlertas);
        ConstruirVacunas(tabVacunas);
        ConstruirFacturas(tabFacturas);
    }

    private void ConstruirResumenMascota(TabPage tab)
    {
        _fotoMascota = new PictureBox
        {
            Width = 145,
            Height = 142,
            Location = new Point(18, 18),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = UiTheme.Fondo
        };
        tab.Controls.Add(_fotoMascota);
        _lblMascotaResumen = new Label
        {
            Text = "Seleccione una mascota para consultar su ficha.",
            Location = new Point(188, 22),
            AutoSize = true,
            MaximumSize = new Size(720, 0),
            Font = new Font("Segoe UI", 11F),
            ForeColor = UiTheme.Texto
        };
        tab.Controls.Add(_lblMascotaResumen);
        _lblAlertaVisible = new Label
        {
            Text = string.Empty,
            Location = new Point(188, 122),
            AutoSize = false,
            Height = 52,
            Width = 680,
            Padding = new Padding(10),
            BackColor = Color.FromArgb(255, 244, 225),
            ForeColor = Color.FromArgb(140, 82, 0),
            Font = UiTheme.FuenteSubtitulo,
            Visible = false
        };
        tab.Controls.Add(_lblAlertaVisible);
    }

    private void ConstruirAlertas(TabPage tab)
    {
        TableLayoutPanel layoutAlertas = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        layoutAlertas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layoutAlertas.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
        layoutAlertas.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tab.Controls.Add(layoutAlertas);

        FlowLayoutPanel comandos = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 7),
            Margin = new Padding(0),
            BackColor = Color.White
        };

        _btnNuevaAlerta = UiTheme.CrearBoton("Registrar alerta", true);
        _btnNuevaAlerta.Width = 135;
        _btnNuevaAlerta.Margin = new Padding(0, 0, 9, 0);
        _btnNuevaAlerta.Enabled = false;
        _btnNuevaAlerta.Click += (_, _) => NuevaAlerta();
        comandos.Controls.Add(_btnNuevaAlerta);

        _btnCerrarAlerta = UiTheme.CrearBoton("Cerrar alerta");
        _btnCerrarAlerta.Width = 122;
        _btnCerrarAlerta.Margin = new Padding(0);
        _btnCerrarAlerta.Enabled = false;
        _btnCerrarAlerta.Click += (_, _) => CerrarAlerta();
        comandos.Controls.Add(_btnCerrarAlerta);

        layoutAlertas.Controls.Add(comandos, 0, 0);

        _gridAlertas = new DataGridView
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };
        UiTheme.PrepararGrid(_gridAlertas);
        _gridAlertas.AutoGenerateColumns = false;
        _gridAlertas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TipoAlerta", HeaderText = "Tipo", FillWeight = 92 });
        _gridAlertas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Descripcion", HeaderText = "Descripción", FillWeight = 205 });
        _gridAlertas.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "Activa", HeaderText = "Activa", FillWeight = 48 });
        _gridAlertas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FechaRegistro", HeaderText = "Fecha", FillWeight = 78, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } });
        _gridAlertas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "UsuarioRegistro", HeaderText = "Registró", FillWeight = 95 });

        layoutAlertas.Controls.Add(_gridAlertas, 0, 1);
    }

    private void ConstruirVacunas(TabPage tab)
    {
        _gridVacunas = new DataGridView { Dock = DockStyle.Fill };
        UiTheme.PrepararGrid(_gridVacunas);
        _gridVacunas.AutoGenerateColumns = false;
        _gridVacunas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Vacuna", HeaderText = "Vacuna", FillWeight = 125 });
        _gridVacunas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FechaAplicacion", HeaderText = "Aplicación", FillWeight = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } });
        _gridVacunas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProximaDosis", HeaderText = "Próxima dosis", FillWeight = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } });
        _gridVacunas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Lote", HeaderText = "Lote", FillWeight = 80 });
        _gridVacunas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Veterinario", HeaderText = "Veterinario", FillWeight = 120 });
        tab.Controls.Add(_gridVacunas);
    }

    private void ConstruirFacturas(TabPage tab)
    {
        _gridFacturas = new DataGridView { Dock = DockStyle.Fill };
        UiTheme.PrepararGrid(_gridFacturas);
        _gridFacturas.AutoGenerateColumns = false;
        _gridFacturas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NumeroFactura", HeaderText = "Factura", FillWeight = 95 });
        _gridFacturas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FechaEmision", HeaderText = "Fecha", FillWeight = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } });
        _gridFacturas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Total", HeaderText = "Total", FillWeight = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
        _gridFacturas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalPagado", HeaderText = "Pagado", FillWeight = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
        _gridFacturas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SaldoPendiente", HeaderText = "Saldo", FillWeight = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
        _gridFacturas.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Estado", HeaderText = "Estado", FillWeight = 90 });
        tab.Controls.Add(_gridFacturas);
    }

    private static Label CrearEtiquetaFicha(Point ubicacion)
    {
        return new Label
        {
            AutoSize = true,
            Location = ubicacion,
            ForeColor = UiTheme.TextoSecundario,
            Font = UiTheme.FuenteNormal
        };
    }

    private Button AgregarBotonAccion(Panel panel, string texto, int x, Action accion, bool principal = false)
    {
        Button boton = UiTheme.CrearBoton(texto, principal);
        boton.Width = texto.Length > 14 ? 145 : 112;
        boton.Margin = new Padding(0, 0, 10, 0);
        boton.Click += (_, _) => accion();
        panel.Controls.Add(boton);
        return boton;
    }

    private void ConfigurarEventos()
    {
        _txtBuscar.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                CargarDuenos();
                e.SuppressKeyPress = true;
            }
        };
        _gridDuenos.SelectionChanged += (_, _) => SeleccionarDuenoDesdeGrid();
        _gridMascotas.SelectionChanged += (_, _) => SeleccionarMascotaDesdeGrid();
        _gridAlertas.SelectionChanged += (_, _) => ActualizarEstadoBotonesAlertas();
    }

    private void CargarDuenos(long? seleccionarId = null)
    {
        try
        {
            List<DuenoModel> duenos = _servicio.BuscarDuenos(_txtBuscar.Text);
            _gridDuenos.DataSource = duenos;
            if (seleccionarId.HasValue)
            {
                SeleccionarFilaPorId(_gridDuenos, seleccionarId.Value, d => ((DuenoModel)d).IdDueno);
            }
            else if (_gridDuenos.Rows.Count > 0)
            {
                _gridDuenos.Rows[0].Selected = true;
            }
            else
            {
                LimpiarFicha();
            }
        }
        catch (Exception ex)
        {
            MostrarError("No se pudieron cargar los clientes.", ex);
        }
    }

    private void SeleccionarDuenoDesdeGrid()
    {
        if (_gridDuenos.CurrentRow?.DataBoundItem is not DuenoModel seleccionado)
        {
            return;
        }
        try
        {
            _duenoActual = _servicio.ObtenerDueno(seleccionado.IdDueno);
            MostrarDueno();
            CargarMascotas();
        }
        catch (Exception ex)
        {
            MostrarError("No se pudo obtener la ficha del dueño.", ex);
        }
    }

    private void MostrarDueno()
    {
        if (_duenoActual is null)
        {
            LimpiarFicha();
            return;
        }
        _lblDueno.Text = $"{_duenoActual.NombreCompleto}   ·   {_duenoActual.CodigoCliente}";
        _lblContacto.Text = $"Teléfono: {_duenoActual.TelefonoPrincipal}   |   Alternativo: {_duenoActual.TelefonoAlternativo ?? "-"}   |   Correo: {_duenoActual.Correo ?? "-"}";
        _lblDireccion.Text = $"Documento: {_duenoActual.Documento ?? "-"}   |   Dirección: {_duenoActual.Direccion ?? "-"}";
        _lblCuenta.Text = $"Mascotas activas: {_duenoActual.CantidadMascotas}   |   Saldo pendiente: {_duenoActual.SaldoPendiente:C2}";
        _btnEditarDueno.Enabled = _puedeEditar;
        _btnNuevaMascota.Enabled = _puedeEditar;
    }

    private void CargarMascotas(long? seleccionarId = null)
    {
        if (_duenoActual is null)
        {
            return;
        }
        List<MascotaModel> mascotas = _servicio.ListarMascotas(_duenoActual.IdDueno);
        _gridMascotas.DataSource = mascotas;
        if (seleccionarId.HasValue)
        {
            SeleccionarFilaPorId(_gridMascotas, seleccionarId.Value, m => ((MascotaModel)m).IdMascota);
        }
        else if (_gridMascotas.Rows.Count > 0)
        {
            _gridMascotas.Rows[0].Selected = true;
        }
        else
        {
            LimpiarMascota();
        }
    }

    private void SeleccionarMascotaDesdeGrid()
    {
        if (_gridMascotas.CurrentRow?.DataBoundItem is not MascotaModel mascota)
        {
            LimpiarMascota();
            return;
        }
        _mascotaActual = mascota;
        MostrarMascota();
        CargarDetalleMascota();
    }

    private void MostrarMascota()
    {
        if (_mascotaActual is null)
        {
            LimpiarMascota();
            return;
        }
        string peso = _mascotaActual.PesoActual.HasValue ? $"{_mascotaActual.PesoActual:N2} kg" : "No registrado";
        string proximaVacuna = _mascotaActual.ProximaVacuna.HasValue ? _mascotaActual.ProximaVacuna.Value.ToString("dd/MM/yyyy") : "Sin recordatorio";
        _lblMascotaResumen.Text =
            $"{_mascotaActual.Nombre}  ·  {_mascotaActual.CodigoPaciente}\n" +
            $"{_mascotaActual.Especie} / {_mascotaActual.Raza ?? "Sin raza"}  ·  {_mascotaActual.Sexo}  ·  Edad: {_mascotaActual.EdadTexto}\n" +
            $"Peso reciente: {peso}  ·  Esterilizado: {(_mascotaActual.Esterilizado ? "Sí" : "No")}  ·  Estado: {_mascotaActual.EstadoVital}\n" +
            $"Microchip: {_mascotaActual.Microchip ?? "-"}  ·  Próxima vacuna: {proximaVacuna}  ·  Saldo: {_mascotaActual.SaldoPendiente:C2}";
        MostrarFoto(_mascotaActual.RutaFoto);
        _lblAlertaVisible.Visible = _mascotaActual.AlertasActivas > 0;
        _lblAlertaVisible.Text = _mascotaActual.AlertasActivas > 0
            ? $"⚠ {_mascotaActual.AlertasActivas} alerta(s) clínica(s) activa(s). Revise la pestaña Alertas clínicas."
            : string.Empty;
        _btnEditarMascota.Enabled = _puedeEditar;
        _btnNuevaAlerta.Enabled = _puedeGestionarAlertas;
        HabilitarBotonesMascota(true);
    }

    private void CargarDetalleMascota()
    {
        if (_mascotaActual is null)
        {
            return;
        }
        try
        {
            _gridAlertas.DataSource = _servicio.ListarAlertas(_mascotaActual.IdMascota);
            _gridVacunas.DataSource = _servicio.ListarVacunas(_mascotaActual.IdMascota);
            _gridFacturas.DataSource = _servicio.ListarFacturasMascota(_mascotaActual.IdMascota);
            ActualizarEstadoBotonesAlertas();
        }
        catch (Exception ex)
        {
            MostrarError("No se pudo cargar el detalle clínico de la mascota.", ex);
        }
    }

    private void MostrarFoto(string? ruta)
    {
        _fotoMascota.Image?.Dispose();
        _fotoMascota.Image = null;
        if (!string.IsNullOrWhiteSpace(ruta) && File.Exists(ruta))
        {
            using Image temporal = Image.FromFile(ruta);
            _fotoMascota.Image = new Bitmap(temporal);
        }
    }

    private void NuevoDueno()
    {
        using FormEdicionDueno dialogo = new(null);
        if (dialogo.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }
        try
        {
            long id = _servicio.GuardarDueno(dialogo.Resultado);
            CargarDuenos(id);
            MessageBox.Show("Dueño registrado correctamente.", "Clientes", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MostrarError("No se pudo guardar el dueño.", ex);
        }
    }

    private void EditarDueno()
    {
        if (_duenoActual is null)
        {
            return;
        }
        using FormEdicionDueno dialogo = new(_duenoActual);
        if (dialogo.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }
        try
        {
            dialogo.Resultado.IdDueno = _duenoActual.IdDueno;
            _servicio.ActualizarDueno(dialogo.Resultado);
            CargarDuenos(_duenoActual.IdDueno);
            MessageBox.Show("Información del dueño actualizada.", "Clientes", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MostrarError("No se pudo actualizar el dueño.", ex);
        }
    }

    private void NuevaMascota()
    {
        if (_duenoActual is null)
        {
            return;
        }
        using FormEdicionMascota dialogo = new(_duenoActual.IdDueno, null);
        if (dialogo.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }
        try
        {
            long id = _servicio.GuardarMascota(dialogo.Resultado);
            CargarDuenos(_duenoActual.IdDueno);
            CargarMascotas(id);
            MessageBox.Show("Mascota registrada correctamente.", "Pacientes", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MostrarError("No se pudo guardar la mascota.", ex);
        }
    }

    private void EditarMascota()
    {
        if (_mascotaActual is null)
        {
            return;
        }
        using FormEdicionMascota dialogo = new(_mascotaActual.IdDueno, _mascotaActual);
        if (dialogo.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }
        try
        {
            dialogo.Resultado.IdMascota = _mascotaActual.IdMascota;
            _servicio.ActualizarMascota(dialogo.Resultado);
            long idMascota = _mascotaActual.IdMascota;
            CargarDuenos(_mascotaActual.IdDueno);
            CargarMascotas(idMascota);
            MessageBox.Show("Información de la mascota actualizada.", "Pacientes", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MostrarError("No se pudo actualizar la mascota.", ex);
        }
    }

    private void NuevaAlerta()
    {
        if (_mascotaActual is null)
        {
            return;
        }
        using FormNuevaAlerta dialogo = new(_mascotaActual.Nombre);
        if (dialogo.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }
        try
        {
            dialogo.Resultado.IdMascota = _mascotaActual.IdMascota;
            _servicio.RegistrarAlerta(dialogo.Resultado);
            RecargarMascotaActual();
            MessageBox.Show("Alerta clínica registrada.", "Alertas", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MostrarError("No se pudo registrar la alerta.", ex);
        }
    }

    private void CerrarAlerta()
    {
        if (_gridAlertas.CurrentRow?.DataBoundItem is not AlertaClinicaModel alerta || !alerta.Activa)
        {
            return;
        }
        DialogResult confirmar = MessageBox.Show(
            $"¿Cerrar la alerta '{alerta.TipoAlerta}'?\n\nLa alerta permanecerá en el historial.",
            "Cerrar alerta", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirmar != DialogResult.Yes)
        {
            return;
        }
        try
        {
            _servicio.CerrarAlerta(alerta.IdAlerta);
            RecargarMascotaActual();
        }
        catch (Exception ex)
        {
            MostrarError("No se pudo cerrar la alerta.", ex);
        }
    }

    private void RecargarMascotaActual()
    {
        if (_duenoActual is null || _mascotaActual is null)
        {
            return;
        }
        long id = _mascotaActual.IdMascota;
        CargarMascotas(id);
    }

    private void ActualizarEstadoBotonesAlertas()
    {
        bool alertaActiva = _gridAlertas.CurrentRow?.DataBoundItem is AlertaClinicaModel alerta && alerta.Activa;
        _btnCerrarAlerta.Enabled = _puedeGestionarAlertas && alertaActiva;
    }

    private void AccionCrearCita()
    {
        if (_mascotaActual is null) return;
        if (_abrirAgenda is not null)
        {
            _abrirAgenda(_mascotaActual.IdMascota);
            return;
        }
        MessageBox.Show("Abra el módulo Agenda para registrar una cita.", "Crear cita", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void AccionExpediente()
    {
        if (_mascotaActual is null) return;
        if (_abrirExpediente is not null)
        {
            _abrirExpediente(_mascotaActual.IdMascota);
            return;
        }
        MessageBox.Show("Abra el módulo Expedientes para consultar el historial clínico.", "Expediente", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void AccionExportar()
    {
        if (_mascotaActual is null) return;
        if (_exportarHistorial is not null)
        {
            _exportarHistorial(_mascotaActual.IdMascota);
            return;
        }
        MessageBox.Show("Abra el expediente para exportar el historial PDF.", "Exportar historial", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void LimpiarFicha()
    {
        _duenoActual = null;
        _lblDueno.Text = "Seleccione un dueño";
        _lblContacto.Text = string.Empty;
        _lblDireccion.Text = string.Empty;
        _lblCuenta.Text = string.Empty;
        _gridMascotas.DataSource = null;
        _btnEditarDueno.Enabled = false;
        _btnNuevaMascota.Enabled = false;
        LimpiarMascota();
    }

    private void LimpiarMascota()
    {
        _mascotaActual = null;
        _lblMascotaResumen.Text = "Seleccione una mascota para consultar su ficha.";
        _lblAlertaVisible.Visible = false;
        MostrarFoto(null);
        _gridAlertas.DataSource = null;
        _gridVacunas.DataSource = null;
        _gridFacturas.DataSource = null;
        _btnEditarMascota.Enabled = false;
        _btnNuevaAlerta.Enabled = false;
        _btnCerrarAlerta.Enabled = false;
        HabilitarBotonesMascota(false);
    }

    private void HabilitarBotonesMascota(bool habilitar)
    {
        foreach (Control control in _btnEditarMascota.Parent!.Controls)
        {
            if (control is Button boton && Equals(boton.Tag, "requiereMascota"))
            {
                boton.Enabled = habilitar;
            }
        }
    }

    private static void SeleccionarFilaPorId(DataGridView grid, long id, Func<object, long> obtenerId)
    {
        foreach (DataGridViewRow fila in grid.Rows)
        {
            if (fila.DataBoundItem is not null && obtenerId(fila.DataBoundItem) == id)
            {
                fila.Selected = true;
                grid.CurrentCell = fila.Cells[0];
                return;
            }
        }
    }

    private static void MostrarError(string mensaje, Exception ex)
    {
        MessageBox.Show($"{mensaje}\n\n{ex.Message}", "Clientes y pacientes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}

internal sealed class FormEdicionDueno : Form
{
    private readonly TextBox _nombre = new();
    private readonly TextBox _documento = new();
    private readonly TextBox _telefono = new();
    private readonly TextBox _telefonoAlternativo = new();
    private readonly TextBox _correo = new();
    private readonly TextBox _direccion = new();
    private readonly TextBox _observaciones = new();
    public DuenoModel Resultado { get; private set; } = new();

    public FormEdicionDueno(DuenoModel? existente)
    {
        UiTheme.PrepararFormulario(this);
        Text = existente is null ? "Nuevo dueño" : "Editar dueño";
        Size = new Size(600, 585);
        MinimumSize = Size;
        MaximumSize = Size;
        MaximizeBox = false;
        MinimizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Construir();
        if (existente is not null)
        {
            _nombre.Text = existente.NombreCompleto;
            _documento.Text = existente.Documento;
            _telefono.Text = existente.TelefonoPrincipal;
            _telefonoAlternativo.Text = existente.TelefonoAlternativo;
            _correo.Text = existente.Correo;
            _direccion.Text = existente.Direccion;
            _observaciones.Text = existente.Observaciones;
        }
    }

    private void Construir()
    {
        Label titulo = UiTheme.CrearTitulo(Text);
        titulo.Location = new Point(25, 18);
        Controls.Add(titulo);
        int y = 68;
        AgregarCampo("Nombre completo *", _nombre, ref y);
        AgregarCampo("Documento", _documento, ref y);
        AgregarCampo("Teléfono principal *", _telefono, ref y);
        AgregarCampo("Teléfono alternativo", _telefonoAlternativo, ref y);
        AgregarCampo("Correo", _correo, ref y);
        AgregarCampo("Dirección", _direccion, ref y);
        Label lblObs = new() { Text = "Observaciones", Location = new Point(27, y), AutoSize = true, ForeColor = UiTheme.TextoSecundario };
        Controls.Add(lblObs);
        _observaciones.Location = new Point(190, y - 3);
        _observaciones.Width = 360;
        _observaciones.Height = 66;
        _observaciones.Multiline = true;
        Controls.Add(_observaciones);
        y += 82;
        Button guardar = UiTheme.CrearBoton("Guardar", true);
        guardar.Location = new Point(352, y);
        guardar.Width = 96;
        guardar.Click += (_, _) => Guardar();
        Controls.Add(guardar);
        Button cancelar = UiTheme.CrearBoton("Cancelar");
        cancelar.Location = new Point(457, y);
        cancelar.Width = 93;
        cancelar.DialogResult = DialogResult.Cancel;
        Controls.Add(cancelar);
        AcceptButton = guardar;
        CancelButton = cancelar;
    }

    private void AgregarCampo(string etiqueta, TextBox caja, ref int y)
    {
        Label label = new() { Text = etiqueta, Location = new Point(27, y + 4), AutoSize = true, ForeColor = UiTheme.TextoSecundario };
        Controls.Add(label);
        caja.Location = new Point(190, y);
        caja.Width = 360;
        Controls.Add(caja);
        y += 43;
    }

    private void Guardar()
    {
        if (string.IsNullOrWhiteSpace(_nombre.Text) || string.IsNullOrWhiteSpace(_telefono.Text))
        {
            MessageBox.Show("Nombre completo y teléfono principal son obligatorios.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        Resultado = new DuenoModel
        {
            NombreCompleto = _nombre.Text.Trim(),
            Documento = _documento.Text.Trim(),
            TelefonoPrincipal = _telefono.Text.Trim(),
            TelefonoAlternativo = _telefonoAlternativo.Text.Trim(),
            Correo = _correo.Text.Trim(),
            Direccion = _direccion.Text.Trim(),
            Observaciones = _observaciones.Text.Trim()
        };
        DialogResult = DialogResult.OK;
        Close();
    }
}

internal sealed class FormEdicionMascota : Form
{
    private readonly long _idDueno;
    private readonly TextBox _nombre = new();
    private readonly ComboBox _especie = new() { DropDownStyle = ComboBoxStyle.DropDown };
    private readonly ComboBox _raza = new() { DropDownStyle = ComboBoxStyle.DropDown };
    private readonly ComboBox _sexo = new();
    private readonly TextBox _color = new();
    private readonly DateTimePicker _nacimiento = new();
    private readonly NumericUpDown _peso = new();
    private readonly CheckBox _esterilizado = new();
    private readonly TextBox _microchip = new();
    private readonly ComboBox _estadoVital = new();
    private readonly DateTimePicker _fallecimiento = new();
    private readonly TextBox _rutaFoto = new();
    private readonly TextBox _observaciones = new();
    private readonly Label _tarifaInfo = new();
    public MascotaModel Resultado { get; private set; } = new();

    public FormEdicionMascota(long idDueno, MascotaModel? existente)
    {
        _idDueno = idDueno;
        UiTheme.PrepararFormulario(this);
        Text = existente is null ? "Nueva mascota" : "Editar mascota";
        Size = new Size(760, 765);
        MinimumSize = Size;
        MaximumSize = Size;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Construir();
        if (existente is not null) CargarExistente(existente);
        ActualizarRazas();
        ActualizarTarifa();
        ActualizarEstadoFallecimiento();
    }

    private void Construir()
    {
        Label titulo = UiTheme.CrearTitulo(Text);
        titulo.Location = new Point(25, 16);
        Controls.Add(titulo);
        int y = 63;
        AgregarControl("Nombre *", _nombre, 26, ref y);

        _especie.Items.AddRange(new List<string>(CatalogoEspeciesMascotas.Especies).ToArray());
        _especie.TextChanged += (_, _) => { ActualizarRazas(); ActualizarTarifa(); };
        AgregarControl("Especie *", _especie, 26, ref y);

        AgregarControl("Raza / variedad", _raza, 26, ref y);
        Label ayuda = new()
        {
            Text = "Puede seleccionar una opción o escribir una especie/raza que no aparezca en la lista.",
            Location = new Point(185, y - 7),
            AutoSize = true,
            ForeColor = UiTheme.TextoSecundario
        };
        Controls.Add(ayuda);
        y += 22;

        _tarifaInfo.Location = new Point(185, y);
        _tarifaInfo.AutoSize = true;
        _tarifaInfo.ForeColor = UiTheme.Primario;
        _tarifaInfo.Font = UiTheme.FuenteSubtitulo;
        Controls.Add(_tarifaInfo);
        y += 34;

        AgregarControl("Sexo", _sexo, 26, ref y);
        _sexo.DropDownStyle = ComboBoxStyle.DropDownList;
        _sexo.Items.AddRange(new object[] { "Macho", "Hembra", "Desconocido" });
        _sexo.SelectedIndex = 2;
        AgregarControl("Color", _color, 26, ref y);
        AgregarControl("Fecha nacimiento", _nacimiento, 26, ref y);
        _nacimiento.Format = DateTimePickerFormat.Short; _nacimiento.ShowCheckBox = true; _nacimiento.Checked = false;
        AgregarControl("Peso actual (kg)", _peso, 26, ref y);
        _peso.DecimalPlaces = 2; _peso.Maximum = 500; _peso.Minimum = 0; _peso.Width = 130;
        _esterilizado.Text = "Esterilizado/a"; _esterilizado.Location = new Point(410, y - 38); _esterilizado.AutoSize = true; Controls.Add(_esterilizado);
        AgregarControl("Microchip", _microchip, 26, ref y);
        AgregarControl("Estado vital", _estadoVital, 26, ref y);
        _estadoVital.DropDownStyle = ComboBoxStyle.DropDownList;
        _estadoVital.Items.AddRange(new object[] { "Viva", "Fallecida", "Inactiva" });
        _estadoVital.SelectedIndex = 0; _estadoVital.SelectedIndexChanged += (_, _) => ActualizarEstadoFallecimiento();
        AgregarControl("Fecha fallecimiento", _fallecimiento, 26, ref y);
        _fallecimiento.Format = DateTimePickerFormat.Short; _fallecimiento.ShowCheckBox = true;
        AgregarRutaFoto(ref y);
        Controls.Add(new Label { Text = "Observaciones", Location = new Point(27, y + 5), AutoSize = true, ForeColor = UiTheme.TextoSecundario });
        _observaciones.Location = new Point(185, y); _observaciones.Width = 520; _observaciones.Height = 55; _observaciones.Multiline = true; Controls.Add(_observaciones);
        y += 72;
        Button guardar = UiTheme.CrearBoton("Guardar", true); guardar.Width = 100; guardar.Location = new Point(496, y); guardar.Click += (_, _) => Guardar(); Controls.Add(guardar);
        Button cancelar = UiTheme.CrearBoton("Cancelar"); cancelar.Width = 100; cancelar.Location = new Point(605, y); cancelar.DialogResult = DialogResult.Cancel; Controls.Add(cancelar);
        AcceptButton = guardar; CancelButton = cancelar;
    }

    private void AgregarControl(string etiqueta, Control control, int x, ref int y)
    {
        Controls.Add(new Label { Text = etiqueta, Location = new Point(x, y + 4), AutoSize = true, ForeColor = UiTheme.TextoSecundario });
        control.Location = new Point(185, y); control.Width = 520; Controls.Add(control); y += 40;
    }

    private void AgregarRutaFoto(ref int y)
    {
        Controls.Add(new Label { Text = "Fotografía", Location = new Point(26, y + 4), AutoSize = true, ForeColor = UiTheme.TextoSecundario });
        _rutaFoto.Location = new Point(185, y); _rutaFoto.Width = 418; _rutaFoto.ReadOnly = true; Controls.Add(_rutaFoto);
        Button seleccionar = UiTheme.CrearBoton("Elegir..."); seleccionar.Location = new Point(612, y - 5); seleccionar.Width = 93; seleccionar.Click += (_, _) => SeleccionarFoto(); Controls.Add(seleccionar);
        y += 40;
    }

    private void ActualizarRazas()
    {
        string textoAnterior = _raza.Text;
        _raza.Items.Clear();
        _raza.Items.AddRange(new List<string>(CatalogoEspeciesMascotas.RazasPara(_especie.Text)).ToArray());
        if (!string.IsNullOrWhiteSpace(textoAnterior)) _raza.Text = textoAnterior;
    }

    private void ActualizarTarifa()
    {
        PoliticaTarifaMascota politica = FabricaPoliticaTarifaMascota.Crear(_especie.Text);
        TarifaCalculadaModel ejemplo = politica.Aplicar(100m);
        _tarifaInfo.Text = ejemplo.Recargo == 0
            ? "Tarifa de servicios: convencional, sin recargo."
            : $"Tarifa de servicios: {politica.TipoTarifa} (+{ejemplo.Recargo:0}% sobre cada Q100.00 base).";
    }

    private void SeleccionarFoto()
    {
        using OpenFileDialog dialogo = new()
        {
            Filter = "Imágenes (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|Todos los archivos (*.*)|*.*",
            Title = "Seleccione fotografía de la mascota"
        };
        if (dialogo.ShowDialog(this) == DialogResult.OK) _rutaFoto.Text = dialogo.FileName;
    }

    private void CargarExistente(MascotaModel existente)
    {
        _nombre.Text = existente.Nombre; _especie.Text = existente.Especie; ActualizarRazas(); _raza.Text = existente.Raza ?? string.Empty;
        _sexo.SelectedItem = existente.Sexo; _color.Text = existente.Color;
        if (existente.FechaNacimiento.HasValue) { _nacimiento.Value = existente.FechaNacimiento.Value; _nacimiento.Checked = true; }
        if (existente.PesoActual.HasValue) _peso.Value = Math.Min(_peso.Maximum, existente.PesoActual.Value);
        _esterilizado.Checked = existente.Esterilizado; _microchip.Text = existente.Microchip; _estadoVital.SelectedItem = existente.EstadoVital;
        if (existente.FechaFallecimiento.HasValue) { _fallecimiento.Value = existente.FechaFallecimiento.Value; _fallecimiento.Checked = true; }
        _rutaFoto.Text = existente.RutaFoto; _observaciones.Text = existente.Observaciones; ActualizarTarifa();
    }

    private void ActualizarEstadoFallecimiento()
    {
        bool fallecida = string.Equals(_estadoVital.SelectedItem?.ToString(), "Fallecida", StringComparison.OrdinalIgnoreCase);
        _fallecimiento.Enabled = fallecida;
        if (!fallecida) _fallecimiento.Checked = false;
    }

    private void Guardar()
    {
        if (string.IsNullOrWhiteSpace(_nombre.Text) || string.IsNullOrWhiteSpace(_especie.Text))
        {
            MessageBox.Show("Nombre y especie son obligatorios.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        string estado = _estadoVital.SelectedItem?.ToString() ?? "Viva";
        if (estado == "Fallecida" && !_fallecimiento.Checked)
        {
            MessageBox.Show("Registre la fecha de fallecimiento.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        Resultado = new MascotaModel
        {
            IdDueno = _idDueno, Nombre = _nombre.Text.Trim(), Especie = _especie.Text.Trim(),
            Raza = string.IsNullOrWhiteSpace(_raza.Text) ? "Sin raza definida" : _raza.Text.Trim(),
            Sexo = _sexo.SelectedItem?.ToString() ?? "Desconocido", Color = _color.Text.Trim(),
            FechaNacimiento = _nacimiento.Checked ? _nacimiento.Value.Date : null,
            PesoActual = _peso.Value == 0 ? null : _peso.Value, Esterilizado = _esterilizado.Checked,
            Microchip = _microchip.Text.Trim(), EstadoVital = estado,
            FechaFallecimiento = _fallecimiento.Checked ? _fallecimiento.Value.Date : null,
            RutaFoto = _rutaFoto.Text.Trim(), Observaciones = _observaciones.Text.Trim()
        };
        DialogResult = DialogResult.OK;
        Close();
    }
}

internal sealed class FormNuevaAlerta : Form
{
    private readonly ComboBox _tipo = new();
    private readonly TextBox _descripcion = new();
    public AlertaClinicaModel Resultado { get; private set; } = new();

    public FormNuevaAlerta(string mascota)
    {
        UiTheme.PrepararFormulario(this);
        Text = "Registrar alerta clínica";
        Size = new Size(575, 350);
        MinimumSize = Size;
        MaximumSize = Size;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Label titulo = UiTheme.CrearTitulo("Nueva alerta clínica");
        titulo.Location = new Point(24, 18);
        Controls.Add(titulo);
        Label paciente = new() { Text = $"Paciente: {mascota}", Location = new Point(27, 56), AutoSize = true, ForeColor = UiTheme.TextoSecundario };
        Controls.Add(paciente);
        Label lblTipo = new() { Text = "Tipo *", Location = new Point(27, 101), AutoSize = true };
        Controls.Add(lblTipo);
        _tipo.Location = new Point(151, 97);
        _tipo.Width = 370;
        _tipo.DropDownStyle = ComboBoxStyle.DropDownList;
        _tipo.Items.AddRange(new object[] { "Alergia", "Condición crónica", "Medicamento contraindicado", "Conducta agresiva", "Recomendación especial", "Otra" });
        _tipo.SelectedIndex = 0;
        Controls.Add(_tipo);
        Label lblDescripcion = new() { Text = "Descripción *", Location = new Point(27, 143), AutoSize = true };
        Controls.Add(lblDescripcion);
        _descripcion.Location = new Point(151, 140);
        _descripcion.Width = 370;
        _descripcion.Height = 78;
        _descripcion.Multiline = true;
        Controls.Add(_descripcion);
        Button guardar = UiTheme.CrearBoton("Registrar", true);
        guardar.Width = 103;
        guardar.Location = new Point(309, 245);
        guardar.Click += (_, _) => Guardar();
        Controls.Add(guardar);
        Button cancelar = UiTheme.CrearBoton("Cancelar");
        cancelar.Width = 101;
        cancelar.Location = new Point(420, 245);
        cancelar.DialogResult = DialogResult.Cancel;
        Controls.Add(cancelar);
        AcceptButton = guardar;
        CancelButton = cancelar;
    }

    private void Guardar()
    {
        if (string.IsNullOrWhiteSpace(_descripcion.Text))
        {
            MessageBox.Show("La descripción es obligatoria.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        Resultado = new AlertaClinicaModel
        {
            TipoAlerta = _tipo.SelectedItem?.ToString() ?? "Otra",
            Descripcion = _descripcion.Text.Trim(),
            Activa = true
        };
        DialogResult = DialogResult.OK;
        Close();
    }
}
