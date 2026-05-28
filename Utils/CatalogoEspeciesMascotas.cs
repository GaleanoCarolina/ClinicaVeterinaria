using System;
using System.Collections.Generic;

namespace ClinicaVeterinaria.Utils;

public static class CatalogoEspeciesMascotas
{
    public static IReadOnlyList<string> Especies { get; } = new[]
    {
        "Canino", "Felino", "Ave", "Conejo", "Hurón",
        "Reptil", "Tortuga", "Erizo", "Roedor", "Anfibio", "Otro exótico"
    };

    public static IReadOnlyList<string> RazasPara(string? especie)
    {
        string valor = (especie ?? string.Empty).Trim();
        if (valor.Equals("Canino", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { "Mestizo / Sin raza definida", "Labrador Retriever", "Golden Retriever", "Pastor Alemán",
                "Poodle", "Bulldog Inglés", "Beagle", "Chihuahua", "Husky Siberiano", "Rottweiler",
                "Border Collie", "Boxer", "Schnauzer", "Dálmata", "Otra / Escribir manualmente" };
        }
        if (valor.Equals("Felino", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { "Mestizo / Sin raza definida", "Europeo", "Siamés", "Persa", "Bengalí",
                "Angora", "Maine Coon", "Azul Ruso", "Otra / Escribir manualmente" };
        }
        if (valor.Equals("Ave", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { "Sin raza definida", "Periquito", "Canario", "Ninfa", "Agapornis",
                "Loro", "Otra / Escribir manualmente" };
        }
        if (valor.Equals("Conejo", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { "Sin raza definida", "Holland Lop", "Mini Rex", "Cabeza de León",
                "Angora", "Otra / Escribir manualmente" };
        }

        return new[] { "Sin raza definida", "Otra / Escribir manualmente" };
    }
}
