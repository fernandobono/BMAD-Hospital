namespace HospitalTurnos.Dominio.Agendas;

/// <summary>Las tres palabras de la columna de A1 (UX). La regla de cuál corresponde la escribe la 4.1.</summary>
public enum EstadoAgendaMedico
{
    SinAgenda,          // hoy no ofrece turnos (FR-13)
    Vigente,            // hoy rige una agenda aprobada
    PropuestaPendiente, // tiene una propuesta enviada que espera al administrador
}
