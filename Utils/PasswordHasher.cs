using System;
using System.Security.Cryptography;

namespace ClinicaVeterinaria.Utils;

public static class PasswordHasher
{
    private const int IteracionesPredeterminadas = 100_000;
    private const int LongitudSalt = 16;
    private const int LongitudHash = 32;
    private const string Algoritmo = "PBKDF2-SHA256";

    public static string CrearHash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("La contraseña no puede estar vacía.", nameof(password));
        }

        byte[] salt = RandomNumberGenerator.GetBytes(LongitudSalt);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, IteracionesPredeterminadas, HashAlgorithmName.SHA256, LongitudHash);

        return $"{Algoritmo}${IteracionesPredeterminadas}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string password, string hashGuardado)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(hashGuardado))
        {
            return false;
        }

        string[] partes = hashGuardado.Split('$');
        if (partes.Length != 4 || !string.Equals(partes[0], Algoritmo, StringComparison.Ordinal))
        {
            return false;
        }

        if (!int.TryParse(partes[1], out int iteraciones) || iteraciones < 10_000)
        {
            return false;
        }

        try
        {
            byte[] salt = Convert.FromBase64String(partes[2]);
            byte[] hashEsperado = Convert.FromBase64String(partes[3]);
            byte[] hashCalculado = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, iteraciones, HashAlgorithmName.SHA256, hashEsperado.Length);

            return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
