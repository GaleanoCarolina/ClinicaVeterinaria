using System;

namespace ClinicaVeterinaria.Models;

/// <summary>
/// Resultado auditable del cálculo de una tarifa clínica.
/// </summary>
public sealed class TarifaCalculadaModel
{
    public decimal PrecioBase { get; init; }
    public decimal Recargo { get; init; }
    public decimal PrecioFinal { get; init; }
    public string TipoTarifa { get; init; } = string.Empty;
    public string Explicacion { get; init; } = string.Empty;
    public string Resumen => Recargo <= 0
        ? $"{TipoTarifa}: {PrecioFinal:C2}"
        : $"{TipoTarifa}: {PrecioFinal:C2} (base {PrecioBase:C2} + {Recargo:C2})";
}

/// <summary>
/// Clase abstracta para demostrar polimorfismo:
/// la aplicación trabaja con PoliticaTarifaMascota, pero en ejecución
/// cada subtipo calcula el precio de manera diferente.
/// </summary>
public abstract class PoliticaTarifaMascota
{
    public abstract string TipoTarifa { get; }
    public abstract string Explicacion { get; }
    public abstract decimal CalcularPrecio(decimal precioBase);

    public TarifaCalculadaModel Aplicar(decimal precioBase)
    {
        if (precioBase < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(precioBase), "El precio base no puede ser negativo.");
        }

        decimal precioFinal = decimal.Round(CalcularPrecio(precioBase), 2, MidpointRounding.AwayFromZero);
        return new TarifaCalculadaModel
        {
            PrecioBase = precioBase,
            PrecioFinal = precioFinal,
            Recargo = decimal.Round(precioFinal - precioBase, 2, MidpointRounding.AwayFromZero),
            TipoTarifa = TipoTarifa,
            Explicacion = Explicacion
        };
    }
}

public sealed class TarifaConvencional : PoliticaTarifaMascota
{
    public override string TipoTarifa => "Tarifa convencional";
    public override string Explicacion => "Canino o felino: no requiere recargo de manejo especializado.";
    public override decimal CalcularPrecio(decimal precioBase) => precioBase;
}

public sealed class TarifaAve : PoliticaTarifaMascota
{
    public override string TipoTarifa => "Tarifa ave / manejo delicado";
    public override string Explicacion => "Ave: recargo del 20% por contención y manejo especializado.";
    public override decimal CalcularPrecio(decimal precioBase) => precioBase * 1.20m;
}

public sealed class TarifaExotica : PoliticaTarifaMascota
{
    public override string TipoTarifa => "Tarifa mascota exótica";
    public override string Explicacion => "Mascota no convencional: recargo del 30% por manejo y valoración especializada.";
    public override decimal CalcularPrecio(decimal precioBase) => precioBase * 1.30m;
}

/// <summary>
/// Fábrica que decide qué implementación polimórfica usar.
/// Las especies escritas manualmente que no sean Canino/Felino/Ave
/// se tratan como exóticas de forma conservadora.
/// </summary>
public static class FabricaPoliticaTarifaMascota
{
    public static PoliticaTarifaMascota Crear(string? especie)
    {
        string valor = (especie ?? string.Empty).Trim();

        if (valor.Equals("Canino", StringComparison.OrdinalIgnoreCase) ||
            valor.Equals("Felino", StringComparison.OrdinalIgnoreCase))
        {
            return new TarifaConvencional();
        }

        if (valor.Equals("Ave", StringComparison.OrdinalIgnoreCase))
        {
            return new TarifaAve();
        }

        return new TarifaExotica();
    }
}
