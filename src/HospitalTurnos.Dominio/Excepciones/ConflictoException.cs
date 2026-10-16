namespace HospitalTurnos.Dominio.Excepciones;

/// <summary>
/// Una regla de negocio en particular: lo pedido choca con algo que ya existe (un horario ocupado).
/// Mensaje para el usuario.
/// </summary>
public class ConflictoException(string mensaje) : ReglaDeNegocioException(mensaje);
