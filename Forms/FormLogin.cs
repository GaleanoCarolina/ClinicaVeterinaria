using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ClinicaVeterinaria.Data;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Services;
using ClinicaVeterinaria.Utils;

namespace ClinicaVeterinaria.Forms;

public sealed class FormLogin : Form
{
    private readonly AuthService _authService = new();
    private TextBox _txtUsuario = null!;
    private TextBox _txtPassword = null!;
    private Button _btnIngresar = null!;
    private Label _lblEstado = null!;

    public FormLogin()
    {
        ConstruirInterfaz();
        ConfigurarEventos();
    }

    private void ConstruirInterfaz()
    {
        UiTheme.PrepararFormulario(this);
        Text = "Patitas - Inicio de sesión";
        ClientSize = new Size(920, 560);
        MinimumSize = new Size(820, 500);
        MaximizeBox = false;

        TableLayoutPanel contenedor = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            BackColor = UiTheme.Fondo
        };
        contenedor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
        contenedor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));
        Controls.Add(contenedor);

        Panel marca = new()
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Primario,
            Padding = new Padding(42)
        };
        contenedor.Controls.Add(marca, 0, 0);

        PictureBox logoGrande = CrearLogoBox(300, 210);
        logoGrande.Location = new Point(55, 95);
        marca.Controls.Add(logoGrande);

        Label tituloMarca = new()
        {
            Text = "PATITAS",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 27F, FontStyle.Bold),
            AutoSize = false,
            Width = 320,
            Height = 48,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(45, 315)
        };
        marca.Controls.Add(tituloMarca);

        Label mensaje = new()
        {
            Text = "Clínica Veterinaria\nGestión clínica y administrativa",
            ForeColor = Color.FromArgb(226, 245, 242),
            Font = new Font("Segoe UI", 11F),
            AutoSize = false,
            Width = 320,
            Height = 60,
            TextAlign = ContentAlignment.TopCenter,
            Location = new Point(45, 370)
        };
        marca.Controls.Add(mensaje);

        Panel panelIngreso = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(62, 60, 62, 60),
            BackColor = Color.White
        };
        contenedor.Controls.Add(panelIngreso, 1, 0);

        PictureBox logoSuperior = CrearLogoBox(170, 85);
        logoSuperior.Location = new Point(150, 35);
        panelIngreso.Controls.Add(logoSuperior);

        Label titulo = UiTheme.CrearTitulo("Iniciar sesión");
        titulo.Location = new Point(62, 135);
        panelIngreso.Controls.Add(titulo);

        Label subtitulo = new()
        {
            Text = "Ingrese sus credenciales para continuar.",
            ForeColor = UiTheme.TextoSecundario,
            AutoSize = true,
            Location = new Point(64, 175)
        };
        panelIngreso.Controls.Add(subtitulo);

        Label lblUsuario = CrearEtiqueta("Usuario", 228);
        panelIngreso.Controls.Add(lblUsuario);
        _txtUsuario = CrearCajaTexto(257, false);
        panelIngreso.Controls.Add(_txtUsuario);

        Label lblPassword = CrearEtiqueta("Contraseña", 318);
        panelIngreso.Controls.Add(lblPassword);
        _txtPassword = CrearCajaTexto(347, true);
        panelIngreso.Controls.Add(_txtPassword);

        _btnIngresar = UiTheme.CrearBoton("Ingresar", true);
        _btnIngresar.Location = new Point(62, 414);
        _btnIngresar.Width = 350;
        panelIngreso.Controls.Add(_btnIngresar);

        Button btnConexion = UiTheme.CrearBoton("Probar conexión");
        btnConexion.Name = "btnConexion";
        btnConexion.Location = new Point(62, 464);
        btnConexion.Width = 350;
        btnConexion.Click += (_, _) => ProbarConexion();
        panelIngreso.Controls.Add(btnConexion);

        _lblEstado = new Label
        {
            AutoSize = false,
            Width = 350,
            Height = 40,
            Location = new Point(62, 510),
            ForeColor = UiTheme.Peligro,
            TextAlign = ContentAlignment.MiddleLeft
        };
        panelIngreso.Controls.Add(_lblEstado);

        AcceptButton = _btnIngresar;
        _txtUsuario.Text = "admin";
    }

    private static Label CrearEtiqueta(string texto, int y)
    {
        return new Label
        {
            Text = texto,
            AutoSize = true,
            Font = UiTheme.FuenteSubtitulo,
            Location = new Point(62, y)
        };
    }

    private static TextBox CrearCajaTexto(int y, bool password)
    {
        return new TextBox
        {
            Location = new Point(62, y),
            Width = 350,
            Height = 32,
            Font = new Font("Segoe UI", 11F),
            UseSystemPasswordChar = password,
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    private static PictureBox CrearLogoBox(int ancho, int alto)
    {
        PictureBox picture = new()
        {
            Width = ancho,
            Height = alto,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };

        string? ruta = BuscarLogo();
        if (!string.IsNullOrWhiteSpace(ruta))
        {
            try
            {
                using FileStream fs = new(ruta, FileMode.Open, FileAccess.Read);
                picture.Image = Image.FromStream(fs);
            }
            catch
            {
                picture.Image = null;
            }
        }

        if (picture.Image is null)
        {
            Bitmap fallback = new(ancho, alto);
            using Graphics g = Graphics.FromImage(fallback);
            g.Clear(Color.Transparent);
            using Font fuente = new("Segoe UI Semibold", 30F, FontStyle.Bold);
            using SolidBrush brocha = new(Color.White);
            StringFormat formato = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("PATITAS", fuente, brocha, new RectangleF(0, 0, ancho, alto), formato);
            picture.Image = fallback;
        }

        return picture;
    }

    private static string? BuscarLogo()
    {
        string? dir = AppContext.BaseDirectory;

        for (int i = 0; i < 8 && !string.IsNullOrWhiteSpace(dir); i++)
        {
            string candidato = Path.Combine(dir, "Assets", "logo.png");
            if (File.Exists(candidato))
            {
                return candidato;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        return null;
    }

    private void ConfigurarEventos()
    {
        _btnIngresar.Click += (_, _) => IniciarSesion();
        Shown += (_, _) => _txtUsuario.Focus();
    }

    private void IniciarSesion()
    {
        _lblEstado.Text = string.Empty;
        if (string.IsNullOrWhiteSpace(_txtUsuario.Text) || string.IsNullOrEmpty(_txtPassword.Text))
        {
            _lblEstado.Text = "Escriba usuario y contraseña.";
            return;
        }

        try
        {
            _btnIngresar.Enabled = false;
            UsuarioModel? usuario = _authService.Autenticar(_txtUsuario.Text, _txtPassword.Text);
            if (usuario is null)
            {
                _lblEstado.Text = "Credenciales incorrectas o usuario inactivo.";
                return;
            }

            SesionActual.Iniciar(usuario);
            Hide();
            using FormMenuPrincipal menu = new();
            menu.ShowDialog(this);
            SesionActual.Cerrar();
            _txtPassword.Clear();
            _lblEstado.Text = string.Empty;
            Show();
            _txtUsuario.Focus();
        }
        catch (Exception ex)
        {
            _lblEstado.Text = "No fue posible ingresar. Verifique la conexión.";
            MessageBox.Show($"Detalle técnico:\n{ex.Message}", "Error de acceso",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnIngresar.Enabled = true;
        }
    }

    private void ProbarConexion()
    {
        try
        {
            Database.ProbarConexion();
            _lblEstado.ForeColor = UiTheme.Acento;
            _lblEstado.Text = "Conexión exitosa con clinica_veterinaria.";
        }
        catch (Exception ex)
        {
            _lblEstado.ForeColor = UiTheme.Peligro;
            _lblEstado.Text = "No se logró conectar con la base de datos.";
            MessageBox.Show(ex.Message, "Conexión fallida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
