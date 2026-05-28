using System;
using System.IO;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Layout.Borders;

namespace ClinicaVeterinaria.Utils;

public static class PdfTheme
{
    public const string NombreClinica = "Patitas";

    public static PdfFont CrearFuenteNormal() => PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
    public static PdfFont CrearFuenteNegrita() => PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

    public static readonly DeviceRgb Primario = new(30, 89, 89);
    public static readonly DeviceRgb Acento = new(47, 158, 138);
    public static readonly DeviceRgb FondoSuave = new(239, 246, 245);
    public static readonly DeviceRgb TextoSecundario = new(92, 105, 105);
    public static readonly DeviceRgb PeligroSuave = new(255, 235, 235);

    public static void AgregarEncabezado(Document documento, string titulo, string subtitulo, string categoria = "Documento clínico")
    {
        string? rutaLogo = BuscarRutaLogo();
        bool tieneLogo = !string.IsNullOrWhiteSpace(rutaLogo);

        Table cabecera = tieneLogo
            ? new Table(UnitValue.CreatePercentArray(new float[] { 15, 57, 28 })).UseAllAvailableWidth()
            : new Table(UnitValue.CreatePercentArray(new float[] { 72, 28 })).UseAllAvailableWidth();

        cabecera.SetMarginBottom(16);

        if (tieneLogo)
        {
            Cell logo = new Cell().SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.MIDDLE);
            try
            {
                Image imagen = new Image(ImageDataFactory.Create(rutaLogo!))
                    .SetAutoScale(true)
                    .SetMaxWidth(62)
                    .SetMaxHeight(52);
                logo.Add(imagen);
            }
            catch
            {
                logo.Add(new Paragraph(string.Empty));
            }
            cabecera.AddCell(logo);
        }

        Cell izquierda = new Cell().SetBorder(Border.NO_BORDER);
        izquierda.Add(new Paragraph("PATITAS")
            .SetFont(CrearFuenteNegrita())
            .SetFontColor(Primario)
            .SetFontSize(18));
        izquierda.Add(new Paragraph("Clínica Veterinaria")
            .SetFont(CrearFuenteNormal())
            .SetFontColor(Acento)
            .SetFontSize(9)
            .SetMarginTop(0)
            .SetMarginBottom(3));
        izquierda.Add(new Paragraph(titulo)
            .SetFont(CrearFuenteNegrita())
            .SetFontSize(13)
            .SetMarginTop(3)
            .SetMarginBottom(0));
        izquierda.Add(new Paragraph(subtitulo)
            .SetFontSize(9)
            .SetFontColor(TextoSecundario)
            .SetMarginTop(2));

        Cell derecha = new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetTextAlignment(TextAlignment.RIGHT);
        derecha.Add(new Paragraph(categoria)
            .SetFont(CrearFuenteNegrita())
            .SetFontColor(Acento)
            .SetFontSize(10));
        derecha.Add(new Paragraph($"Emisión: {DateTime.Now:dd/MM/yyyy HH:mm}")
            .SetFontSize(9)
            .SetFontColor(TextoSecundario));

        cabecera.AddCell(izquierda);
        cabecera.AddCell(derecha);
        documento.Add(cabecera);
    }

    public static Paragraph TituloSeccion(string titulo)
    {
        return new Paragraph(titulo)
            .SetFont(CrearFuenteNegrita())
            .SetFontColor(Primario)
            .SetFontSize(11)
            .SetMarginTop(12)
            .SetMarginBottom(6);
    }

    public static Table Tabla(float[] anchos)
    {
        return new Table(UnitValue.CreatePercentArray(anchos))
            .UseAllAvailableWidth()
            .SetFontSize(8.5F);
    }

    public static Cell EncabezadoTabla(string texto)
    {
        return new Cell()
            .SetBackgroundColor(Primario)
            .SetFontColor(ColorConstants.WHITE)
            .SetPadding(5)
            .Add(new Paragraph(texto).SetFont(CrearFuenteNegrita()).SetMargin(0));
    }

    public static Cell Celda(string texto)
    {
        return new Cell()
            .SetPadding(5)
            .Add(new Paragraph(texto ?? string.Empty).SetMargin(0));
    }

    public static Table DatosPaciente(string codigo, string mascota, string dueno, string especieRaza, string telefono)
    {
        Table datos = Tabla(new float[] { 18, 32, 18, 32 });
        datos.SetBackgroundColor(FondoSuave);
        datos.AddCell(CeldaClave("Paciente")); datos.AddCell(Celda($"{mascota} - {codigo}"));
        datos.AddCell(CeldaClave("Dueño")); datos.AddCell(Celda(dueno));
        datos.AddCell(CeldaClave("Especie / raza")); datos.AddCell(Celda(especieRaza));
        datos.AddCell(CeldaClave("Teléfono")); datos.AddCell(Celda(telefono));
        datos.SetMarginBottom(10);
        return datos;
    }

    public static Cell CeldaClave(string texto)
    {
        return new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetPadding(5)
            .SetFontColor(Primario)
            .Add(new Paragraph(texto).SetFont(CrearFuenteNegrita()).SetMargin(0));
    }

    public static void AgregarPie(Document documento)
    {
        documento.Add(new Paragraph($"Documento emitido por {NombreClinica}.")
            .SetFontSize(8)
            .SetFontColor(TextoSecundario)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(20)
            .SetBorderTop(new SolidBorder(FondoSuave, 1))
            .SetPaddingTop(8));
    }

    /// <summary>
    /// Busca el logo en Assets/logo.png desde el directorio de ejecución
    /// y también hacia arriba para soportar ejecución desde bin/Debug.
    /// </summary>
    private static string? BuscarRutaLogo()
    {
        string? directorio = AppContext.BaseDirectory;

        for (int nivel = 0; nivel < 7 && !string.IsNullOrWhiteSpace(directorio); nivel++)
        {
            string candidata = System.IO.Path.Combine(directorio, "Assets", "logo.png");
            if (File.Exists(candidata))
            {
                return candidata;
            }

            DirectoryInfo? padre = Directory.GetParent(directorio);
            directorio = padre?.FullName;
        }

        return null;
    }
}
