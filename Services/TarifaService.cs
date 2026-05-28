using ClinicaVeterinaria.Models;

namespace ClinicaVeterinaria.Services;

public sealed class TarifaService
{
    public TarifaCalculadaModel CalcularPrecioServicio(ServicioModel servicio, string especie)
        => CalcularPrecioServicio(servicio.PrecioBase, especie);

    public TarifaCalculadaModel CalcularPrecioServicio(decimal precioBase, string especie)
    {
        PoliticaTarifaMascota politica = FabricaPoliticaTarifaMascota.Crear(especie);

        // Llamada polimórfica: según el subtipo real (convencional, ave o exótica)
        // se ejecuta un cálculo distinto.
        return politica.Aplicar(precioBase);
    }
}
