namespace HospitalTurnos.Dominio.Excepciones;

/// <summary>Lo buscado no existe (un paciente, un turno). Mensaje para el usuario.</summary>
public class NoEncontradoException(string mensaje) : Exception(mensaje);
