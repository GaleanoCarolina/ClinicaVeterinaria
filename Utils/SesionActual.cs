using System;
using System.Collections.Generic;
using ClinicaVeterinaria.Models;

namespace ClinicaVeterinaria.Utils;

public static class SesionActual
{
    public static UsuarioModel? Usuario { get; private set; }

    public static bool HaySesion => Usuario is not null;

    public static void Iniciar(UsuarioModel usuario)
    {
        Usuario = usuario ?? throw new ArgumentNullException(nameof(usuario));
    }

    public static void Cerrar()
    {
        Usuario = null;
    }

    public static bool EsRol(params string[] roles)
    {
        if (Usuario is null)
        {
            return false;
        }

        foreach (string rol in roles)
        {
            if (string.Equals(Usuario.Rol, rol, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TieneAccesoModulo(string modulo)
    {
        if (Usuario is null)
        {
            return false;
        }

        if (EsRol("Administrador"))
        {
            return true;
        }

        Dictionary<string, HashSet<string>> permisos = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Recepción"] = new(StringComparer.OrdinalIgnoreCase)
            {
                "Dashboard", "Agenda", "Clientes y Pacientes", "Expedientes", "Recordatorios"
            },
            ["Veterinario"] = new(StringComparer.OrdinalIgnoreCase)
            {
                "Dashboard", "Agenda", "Atención Clínica", "Expedientes",
                "Recordatorios", "Órdenes Clínicas", "Hospitalización"
            },
            ["Caja"] = new(StringComparer.OrdinalIgnoreCase)
            {
                "Dashboard", "Clientes y Pacientes", "Facturación y Caja", "Reportes"
            }
        };

        return permisos.TryGetValue(Usuario.Rol, out HashSet<string>? modulos)
            && modulos.Contains(modulo);
    }

    public static void ExigirRoles(params string[] rolesPermitidos)
    {
        if (!EsRol(rolesPermitidos))
        {
            throw new UnauthorizedAccessException(
                "El usuario actual no tiene permisos para realizar esta operación.");
        }
    }
}
