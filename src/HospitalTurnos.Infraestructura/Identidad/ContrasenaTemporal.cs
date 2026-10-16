using System.Security.Cryptography;

namespace HospitalTurnos.Infraestructura.Identidad;

/// <summary>
/// La contraseña que el administrador le da a una persona nueva (story 2.2, AC 4). Cumple la política real
/// de Identity: 10 o más caracteres, mayúscula, minúscula, número y símbolo.
/// </summary>
public static class ContrasenaTemporal
{
    // Sin 0/O/o, 1/l/I: se dicta en voz alta o se copia a mano (AC 4).
    private const string Mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Minusculas = "abcdefghijkmnpqrstuvwxyz";
    private const string Numeros = "23456789";
    private const string Simbolos = "-#.";
    private const string Todos = Mayusculas + Minusculas + Numeros + Simbolos;
    private const int Largo = 12;

    // Uno de cada grupo (la política los exige), el resto de todos, y se mezcla. Todo con
    // RandomNumberGenerator.GetInt32: un generador predecible daría contraseñas adivinables.
    public static string Generar()
    {
        var caracteres = new char[Largo];
        caracteres[0] = AlAzar(Mayusculas);
        caracteres[1] = AlAzar(Minusculas);
        caracteres[2] = AlAzar(Numeros);
        caracteres[3] = AlAzar(Simbolos);
        for (var i = 4; i < Largo; i++)
            caracteres[i] = AlAzar(Todos);
        RandomNumberGenerator.Shuffle(caracteres.AsSpan());
        return new string(caracteres);
    }

    private static char AlAzar(string grupo) => grupo[RandomNumberGenerator.GetInt32(grupo.Length)];
}
