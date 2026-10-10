using System.Net.Mail;

namespace HospitalTurnos.Api.Dominio;

public class Paciente
{
    public int Id { get; private set; }
    public string Dni { get; private set; } = "";
    public string Nombre { get; private set; } = "";
    public string Apellido { get; private set; } = "";
    public string Telefono { get; private set; } = "";
    public string? Email { get; private set; }

    private Paciente() { }

    public Paciente(string dni, string nombre, string apellido, string telefono, string? email)
    {
        dni = (dni ?? "").Trim().Replace(".", "");
        if (dni.Length is < 7 or > 8 || !dni.All(char.IsAsciiDigit))
            throw new ReglaDeNegocioException("El DNI debe tener 7 u 8 dígitos.");
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido))
            throw new ReglaDeNegocioException("El paciente necesita nombre y apellido.");
        if (string.IsNullOrWhiteSpace(telefono))
            throw new ReglaDeNegocioException("El paciente necesita un teléfono de contacto.");
        if (!string.IsNullOrWhiteSpace(email) && !MailAddress.TryCreate(email.Trim(), out _))
            throw new ReglaDeNegocioException("El email no es válido.");

        Dni = dni;
        Nombre = nombre.Trim();
        Apellido = apellido.Trim();
        Telefono = telefono.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }

    public string NombreCompleto => $"{Apellido}, {Nombre}";
}
