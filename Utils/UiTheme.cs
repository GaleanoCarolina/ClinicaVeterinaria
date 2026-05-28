using System.Drawing;
using System.Windows.Forms;

namespace ClinicaVeterinaria.Utils;

public static class UiTheme
{
    // Paleta inspirada en el logo Patitas.
    public static readonly Color Primario = Color.FromArgb(0, 132, 155);
    public static readonly Color PrimarioOscuro = Color.FromArgb(0, 92, 110);
    public static readonly Color Acento = Color.FromArgb(244, 160, 0);
    public static readonly Color Fondo = Color.FromArgb(245, 248, 248);
    public static readonly Color Superficie = Color.White;
    public static readonly Color Texto = Color.FromArgb(32, 40, 42);
    public static readonly Color TextoSecundario = Color.FromArgb(90, 105, 105);
    public static readonly Color Borde = Color.FromArgb(214, 225, 225);
    public static readonly Color Exito = Color.FromArgb(47, 158, 138);
    public static readonly Color Advertencia = Color.FromArgb(244, 160, 0);
    public static readonly Color Peligro = Color.FromArgb(205, 62, 62);
    public static readonly Color PeligroSuave = Color.FromArgb(255, 235, 235);

    public static readonly Font FuenteTitulo = new("Segoe UI", 18F, FontStyle.Bold);
    public static readonly Font FuenteSubtitulo = new("Segoe UI", 11F, FontStyle.Bold);
    public static readonly Font FuenteNormal = new("Segoe UI", 9.5F, FontStyle.Regular);
    public static readonly Font FuentePequena = new("Segoe UI", 8.5F, FontStyle.Regular);

    public static void PrepararFormulario(Form form)
    {
        form.BackColor = Fondo;
        form.Font = FuenteNormal;
        form.ForeColor = Texto;
        form.StartPosition = FormStartPosition.CenterScreen;
    }

    public static Label CrearTitulo(string texto)
    {
        return new Label
        {
            Text = texto,
            AutoSize = true,
            Font = FuenteTitulo,
            ForeColor = PrimarioOscuro
        };
    }

    public static Label CrearSubtitulo(string texto)
    {
        return new Label
        {
            Text = texto,
            AutoSize = true,
            Font = FuenteSubtitulo,
            ForeColor = TextoSecundario
        };
    }

    public static Button CrearBoton(string texto, bool primario = false)
    {
        Button boton = new()
        {
            Text = texto,
            Height = 38,
            FlatStyle = FlatStyle.Flat,
            BackColor = primario ? Primario : Color.White,
            ForeColor = primario ? Color.White : Texto,
            Cursor = Cursors.Hand,
            Font = FuenteNormal,
            UseVisualStyleBackColor = false
        };

        boton.FlatAppearance.BorderColor = primario ? Primario : Borde;
        boton.FlatAppearance.MouseOverBackColor = primario ? PrimarioOscuro : Color.FromArgb(238, 246, 246);
        boton.FlatAppearance.MouseDownBackColor = primario ? PrimarioOscuro : Color.FromArgb(228, 239, 239);

        return boton;
    }

    public static Panel CrearTarjeta()
    {
        return new Panel
        {
            BackColor = Superficie,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(12)
        };
    }

    public static void PrepararGrid(DataGridView grid)
    {
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.EnableHeadersVisualStyles = false;
        grid.GridColor = Color.FromArgb(220, 228, 228);
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.ReadOnly = true;
        grid.Font = FuenteNormal;

        grid.ColumnHeadersDefaultCellStyle.BackColor = PrimarioOscuro;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = PrimarioOscuro;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
        grid.ColumnHeadersHeight = 32;

        grid.DefaultCellStyle.BackColor = Color.White;
        grid.DefaultCellStyle.ForeColor = Texto;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(213, 239, 237);
        grid.DefaultCellStyle.SelectionForeColor = Texto;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 252, 252);
    }

    public static TextBox CrearTextBox()
    {
        return new TextBox
        {
            BorderStyle = BorderStyle.FixedSingle,
            Font = FuenteNormal
        };
    }

    public static ComboBox CrearComboBox()
    {
        return new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = FuenteNormal
        };
    }
}
