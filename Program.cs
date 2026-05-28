using System;
using System.Globalization;
using System.Windows.Forms;
using ClinicaVeterinaria.Forms;
using ClinicaVeterinaria.Utils;

namespace ClinicaVeterinaria;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        CultureInfo guatemala = FiscalGuatemala.CrearCulturaMonetaria();
        CultureInfo.DefaultThreadCurrentCulture = guatemala;
        CultureInfo.DefaultThreadCurrentUICulture = guatemala;
        CultureInfo.CurrentCulture = guatemala;
        CultureInfo.CurrentUICulture = guatemala;

        ApplicationConfiguration.Initialize();
        Application.Run(new FormLogin());
    }
}
