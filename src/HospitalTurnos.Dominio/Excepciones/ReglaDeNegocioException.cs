namespace HospitalTurnos.Dominio.Excepciones;

/// <summary>
/// Una regla del hospital impide la operación. El mensaje está escrito para el usuario, en
/// castellano: es lo que se le muestra tal cual.
/// </summary>
public class ReglaDeNegocioException(string mensaje) : Exception(mensaje);
