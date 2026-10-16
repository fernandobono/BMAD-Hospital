namespace HospitalTurnos.Dominio.Excepciones;

/// <summary>El usuario no tiene permiso para esta operación. Mensaje para el usuario.</summary>
public class AccesoDenegadoException(string mensaje) : Exception(mensaje);
