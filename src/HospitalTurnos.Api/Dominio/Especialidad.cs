namespace HospitalTurnos.Api.Dominio;

public class Especialidad
{
    public int Id { get; private set; }
    public string Nombre { get; private set; } = "";

    private Especialidad() { }

    public Especialidad(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ReglaDeNegocioException("La especialidad necesita un nombre.");
        Nombre = nombre.Trim();
    }
}
