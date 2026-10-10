namespace HospitalTurnos.Api.Dominio;

/// <summary>Se viola una regla del dominio (datos inválidos, turno no disponible, etc.).</summary>
public class ReglaDeNegocioException(string mensaje) : Exception(mensaje);

/// <summary>La operación choca con el estado actual (por ejemplo, un turno ya tomado).</summary>
public class ConflictoException(string mensaje) : ReglaDeNegocioException(mensaje);

/// <summary>No existe la entidad pedida.</summary>
public class NoEncontradoException(string mensaje) : Exception(mensaje);
