using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Windows.Forms;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Services;
using ClinicaVeterinaria.Utils;

namespace ClinicaVeterinaria.Forms;

public sealed class FormMenuPrincipal : Form
{
    private readonly Dictionary<string, Button> _botonesModulo = new(StringComparer.OrdinalIgnoreCase);
    private Panel _panelContenido = null!;
    private Label _lblUsuario = null!;
    private Label _lblReloj = null!;
    private System.Windows.Forms.Timer _reloj = null!;
    private Button? _botonActivo;

    public FormMenuPrincipal()
    {
        ConstruirInterfaz();
        AplicarPermisos();
        ConfigurarEventos();
        AbrirDashboard();
    }

    private void ConstruirInterfaz()
    {
        UiTheme.PrepararFormulario(this);
        Text = "Patitas - Sistema de Gestión";
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1200, 720);

        Panel lateral = new()
        {
            Dock = DockStyle.Left,
            Width = 245,
            BackColor = UiTheme.PrimarioOscuro,
            Padding = new Padding(10, 16, 10, 12)
        };
        Controls.Add(lateral);

        Label marca = new()
        {
            Text = "🐾  Patitas",
            Dock = DockStyle.Top,
            Height = 60,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0)
        };
        lateral.Controls.Add(marca);

        FlowLayoutPanel menu = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(0, 10, 0, 0)
        };
        lateral.Controls.Add(menu);
        menu.BringToFront();

        string[] modulos =
        {
            "Dashboard", "Agenda", "Clientes y Pacientes", "Atención Clínica",
            "Expedientes", "Facturación y Caja", "Inventario", "Recordatorios",
            "Veterinarios", "Catálogos", "Órdenes Clínicas", "Hospitalización", "Reportes"
        };

        foreach (string modulo in modulos)
        {
            Button boton = CrearBotonMenu(modulo);
            _botonesModulo[modulo] = boton;
            menu.Controls.Add(boton);
        }

        Button cerrar = CrearBotonMenu("Cerrar sesión");
        cerrar.ForeColor = Color.FromArgb(255, 216, 216);
        cerrar.Click += (_, _) => Close();
        menu.Controls.Add(cerrar);

        Panel superior = new()
        {
            Dock = DockStyle.Top,
            Height = 68,
            BackColor = Color.White,
            Padding = new Padding(28, 0, 28, 0)
        };
        Controls.Add(superior);
        superior.BringToFront();

        string nombreClinica = ConfigurationManager.AppSettings["NombreClinica"] ?? "Patitas";
        Label lblClinica = new()
        {
            Text = nombreClinica,
            Dock = DockStyle.Left,
            AutoSize = false,
            Width = 330,
            Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
            ForeColor = UiTheme.Primario,
            TextAlign = ContentAlignment.MiddleLeft
        };
        superior.Controls.Add(lblClinica);

        _lblReloj = new Label
        {
            Dock = DockStyle.Right,
            Width = 225,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = UiTheme.TextoSecundario
        };
        superior.Controls.Add(_lblReloj);

        _lblUsuario = new Label
        {
            Dock = DockStyle.Right,
            Width = 300,
            TextAlign = ContentAlignment.MiddleRight,
            Font = UiTheme.FuenteSubtitulo
        };
        superior.Controls.Add(_lblUsuario);

        _panelContenido = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Fondo,
            Padding = new Padding(22)
        };
        Controls.Add(_panelContenido);
        _panelContenido.BringToFront();
    }

    private Button CrearBotonMenu(string texto)
    {
        Button boton = new()
        {
            Text = texto,
            Width = 220,
            Height = 43,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            ForeColor = Color.FromArgb(223, 237, 237),
            BackColor = UiTheme.PrimarioOscuro,
            Font = UiTheme.FuenteNormal,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(18, 0, 0, 0),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 2, 0, 2)
        };
        return boton;
    }

    private void ConfigurarEventos()
    {
        _botonesModulo["Dashboard"].Click += (_, _) => AbrirDashboard();
        _botonesModulo["Agenda"].Click += (_, _) => AbrirAgenda();
        _botonesModulo["Clientes y Pacientes"].Click += (_, _) => AbrirClientesPacientes();
        _botonesModulo["Veterinarios"].Click += (_, _) => AbrirVeterinarios();
        _botonesModulo["Atención Clínica"].Click += (_, _) => AbrirAtencionClinica();
        _botonesModulo["Expedientes"].Click += (_, _) => AbrirExpediente();
        _botonesModulo["Facturación y Caja"].Click += (_, _) => AbrirFacturacionCaja();
        _botonesModulo["Inventario"].Click += (_, _) => AbrirInventario();
        _botonesModulo["Recordatorios"].Click += (_, _) => AbrirRecordatorios();
        _botonesModulo["Catálogos"].Click += (_, _) => AbrirCatalogos();
        _botonesModulo["Órdenes Clínicas"].Click += (_, _) => AbrirOrdenesClinicas();
        _botonesModulo["Hospitalización"].Click += (_, _) => AbrirHospitalizacion();
        _botonesModulo["Reportes"].Click += (_, _) => AbrirReportes();

        foreach ((string modulo, Button boton) in _botonesModulo)
        {
            if (!string.Equals(modulo, "Dashboard", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(modulo, "Agenda", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(modulo, "Clientes y Pacientes", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(modulo, "Veterinarios", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(modulo, "Atención Clínica", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(modulo, "Expedientes", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(modulo, "Facturación y Caja", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(modulo, "Inventario", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(modulo, "Recordatorios", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(modulo, "Catálogos", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(modulo, "Órdenes Clínicas", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(modulo, "Hospitalización", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(modulo, "Reportes", StringComparison.OrdinalIgnoreCase))
            {
                boton.Click += (_, _) => MostrarModuloPendiente(modulo, boton);
            }
        }

        _reloj = new System.Windows.Forms.Timer { Interval = 1000 };
        _reloj.Tick += (_, _) => ActualizarCabecera();
        _reloj.Start();
        FormClosed += (_, _) => _reloj.Stop();
        ActualizarCabecera();
    }

    private void AplicarPermisos()
    {
        foreach ((string modulo, Button boton) in _botonesModulo)
        {
            boton.Visible = SesionActual.TieneAccesoModulo(modulo);
        }
    }

    private void ActualizarCabecera()
    {
        UsuarioModel? usuario = SesionActual.Usuario;
        _lblUsuario.Text = usuario is null ? string.Empty : $"{usuario.NombreCompleto}  ·  {usuario.Rol}   ";
        _lblReloj.Text = DateTime.Now.ToString("dddd, dd/MM/yyyy  HH:mm:ss");
    }

    private void AbrirDashboard()
    {
        MostrarFormulario(new FormDashboard(), _botonesModulo["Dashboard"]);
    }

    private void AbrirAgenda(long? idMascota = null)
    {
        MostrarFormulario(new FormAgenda(idMascota), _botonesModulo["Agenda"]);
    }

    private void AbrirClientesPacientes()
    {
        MostrarFormulario(new FormClientesPacientes(idMascota => AbrirAgenda(idMascota), idMascota => AbrirExpediente(idMascota), idMascota => ExportarExpediente(idMascota)), _botonesModulo["Clientes y Pacientes"]);
    }

    private void AbrirVeterinarios()
    {
        MostrarFormulario(new FormVeterinarios(), _botonesModulo["Veterinarios"]);
    }

    private void AbrirAtencionClinica()
    {
        MostrarFormulario(new FormAtencionClinica(), _botonesModulo["Atención Clínica"]);
    }

    private void AbrirExpediente(long? idMascota = null)
    {
        MostrarFormulario(new FormExpedienteMascota(idMascota), _botonesModulo["Expedientes"]);
    }

    private void AbrirFacturacionCaja()
    {
        MostrarFormulario(new FormFacturacionCaja(), _botonesModulo["Facturación y Caja"]);
    }

    private void AbrirInventario()
    {
        MostrarFormulario(new FormInventario(), _botonesModulo["Inventario"]);
    }

    private void AbrirRecordatorios()
    {
        MostrarFormulario(new FormRecordatorios(idMascota => AbrirAgenda(idMascota)), _botonesModulo["Recordatorios"]);
    }

    private void AbrirCatalogos()
    {
        MostrarFormulario(new FormCatalogos(), _botonesModulo["Catálogos"]);
    }

    private void AbrirOrdenesClinicas()
    {
        MostrarFormulario(new FormOrdenesClinicas(), _botonesModulo["Órdenes Clínicas"]);
    }

    private void AbrirHospitalizacion()
    {
        MostrarFormulario(new FormHospitalizacion(), _botonesModulo["Hospitalización"]);
    }

    private void AbrirReportes()
    {
        MostrarFormulario(new FormReportes(), _botonesModulo["Reportes"]);
    }

    private void ExportarExpediente(long idMascota)
    {
        using SaveFileDialog dialogo = new()
        {
            Filter = "Documento PDF (*.pdf)|*.pdf",
            FileName = $"Expediente_{DateTime.Now:yyyyMMdd}.pdf"
        };
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            new PdfService().GenerarExpediente(idMascota, dialogo.FileName);
            MessageBox.Show("Expediente PDF generado correctamente.", "Documentos PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No fue posible generar el expediente PDF.\n\n{ex.Message}", "Documentos PDF", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void MostrarModuloPendiente(string modulo, Button boton)
    {
        Panel panel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(38)
        };
        Label titulo = UiTheme.CrearTitulo(modulo);
        titulo.Location = new Point(38, 38);
        panel.Controls.Add(titulo);
        Label detalle = new()
        {
            Text = "La navegación y el permiso del módulo están listos.\n" +
                   "La operación completa se incorpora en su macrobloque correspondiente.",
            AutoSize = true,
            Location = new Point(40, 92),
            ForeColor = UiTheme.TextoSecundario,
            Font = new Font("Segoe UI", 12F)
        };
        panel.Controls.Add(detalle);
        _panelContenido.Controls.Clear();
        _panelContenido.Controls.Add(panel);
        ResaltarBoton(boton);
    }

    private void MostrarFormulario(Form formulario, Button boton)
    {
        _panelContenido.Controls.Clear();
        formulario.TopLevel = false;
        formulario.FormBorderStyle = FormBorderStyle.None;
        formulario.Dock = DockStyle.Fill;
        _panelContenido.Controls.Add(formulario);
        formulario.Show();
        ResaltarBoton(boton);
    }

    private void ResaltarBoton(Button boton)
    {
        if (_botonActivo is not null)
        {
            _botonActivo.BackColor = UiTheme.PrimarioOscuro;
            _botonActivo.ForeColor = Color.FromArgb(223, 237, 237);
        }

        _botonActivo = boton;
        _botonActivo.BackColor = UiTheme.Acento;
        _botonActivo.ForeColor = Color.White;
    }
}
