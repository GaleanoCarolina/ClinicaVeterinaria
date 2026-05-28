using System;
using System.Globalization;

namespace ClinicaVeterinaria.Utils;

/// <summary>
/// Reglas fiscales y presentación monetaria para la versión Guatemala.
/// Los precios mostrados al cliente están expresados en quetzales
/// e incluyen el IVA general vigente del 12%.
/// </summary>
public static class FiscalGuatemala
{
    public const decimal TasaIva = 0.12m;
    public const string SimboloMoneda = "Q";
    public const string LeyendaPrecios = "Precios expresados en quetzales (Q) con IVA incluido (12%).";

    public static CultureInfo CrearCulturaMonetaria()
    {
        CultureInfo cultura = (CultureInfo)CultureInfo.GetCultureInfo("es-GT").Clone();
        cultura.NumberFormat.CurrencySymbol = "Q";
        cultura.NumberFormat.CurrencyDecimalDigits = 2;
        cultura.NumberFormat.CurrencyPositivePattern = 2; // Q 1,234.56
        cultura.NumberFormat.CurrencyNegativePattern = 12;
        return cultura;
    }

    public static decimal TotalConIva(decimal subtotalConIva, decimal descuento)
        => Redondear(subtotalConIva - descuento);

    /// <summary>
    /// Extrae el IVA incluido en un precio final: total * 12 / 112.
    /// </summary>
    public static decimal CalcularIvaIncluido(decimal totalConIva)
        => totalConIva <= 0 ? 0 : Redondear(totalConIva * TasaIva / (1m + TasaIva));

    public static decimal CalcularBaseImponible(decimal totalConIva)
        => Redondear(totalConIva - CalcularIvaIncluido(totalConIva));

    public static string Moneda(decimal monto)
        => monto.ToString("C2", CrearCulturaMonetaria());

    private static decimal Redondear(decimal valor)
        => decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
}
