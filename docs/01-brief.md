---
documento: Brief del producto
proyecto: Gestión de turnos — Hospital Municipal Las Acacias (caso simulado)
version: 1.1
fecha: 2026-10-09
autora: Mary (analista) con John, Winston y Sally
estado: aprobado (2026-10-09)
---

# Brief — Gestión de turnos del Hospital Municipal Las Acacias

> **Caso simulado.** El hospital, sus números y sus personas son inventados para la materia
> Modelación y Diseño de Sistemas. Se trabaja como si fuera un cliente real.
> Todo lo marcado **[SUPUESTO S-nn]** lo propuso el equipo y espera confirmación del docente.

## 1. El hospital

Hospital público municipal de una ciudad del interior de Córdoba (~120.000 habitantes).
Atiende consultorios externos de lunes a viernes, de 8 a 18 h. **[SUPUESTO S-01]**

- 4 especialidades en consultorios externos: Clínica médica, Pediatría, Cardiología y Traumatología. **[SUPUESTO S-02]**
- 18 médicos de planta. Cada uno atiende en franjas fijas por semana (por ejemplo, lunes y miércoles de 8 a 12). **[SUPUESTO S-03]**
- Unos 400 turnos por día. **[SUPUESTO S-04]**
- La guardia (urgencias) **no** usa turnos y queda fuera de este sistema.

## 2. Cómo se dan los turnos hoy

1. El paciente llama por teléfono o hace fila en la ventanilla desde las 7 de la mañana.
2. La recepcionista busca un hueco en una planilla de Excel compartida, una por médico.
3. Anota el turno a mano. El paciente se va con un papel o con el horario de memoria.
4. Para cancelar hay que volver a llamar. Casi nadie lo hace.

## 3. El problema

| Problema | Cómo se nota hoy |
|---|---|
| Teléfono saturado a primera hora | Las líneas dan ocupado entre 7 y 9 h; la gente se rinde o viene en persona. **[SUPUESTO S-05]** |
| Ausentismo alto | ~25 % de los turnos queda vacío porque el paciente no viene y no avisó. **[SUPUESTO S-06]** |
| Turnos que se pisan | Dos recepcionistas editan la misma planilla y le dan el mismo horario a dos personas. |
| Sin datos | Nadie sabe cuántos turnos se dan por especialidad ni cuánta demanda queda sin atender. |

## 4. Quiénes lo usan

| Persona | Quién es | Qué necesita |
|---|---|---|
| **Paciente** | Cualquier vecino. Muchos son mayores y usan sólo el celular. | Sacar y cancelar su turno sin llamar, y saber cuándo es. |
| **Recepcionista** | Personal administrativo, 6 personas en dos turnos. Atiende teléfono y ventanilla a la vez. | Encontrar un hueco y dar el turno en menos de un minuto. |
| **Médico** | Profesional de planta. | Ver la lista de pacientes del día. |
| **Administrador de agendas** | Una persona de la dirección del hospital. | Dar de alta médicos, especialidades y horarios de atención. |

## 5. Qué queremos lograr

| Objetivo | Cómo se mide | Meta a 6 meses |
|---|---|---|
| Menos llamadas | % de turnos sacados por la web | 40 % **[SUPUESTO S-07]** |
| Menos ausentismo | % de turnos vacíos sin aviso | de 25 % a 15 % |
| Cero turnos pisados | Turnos duplicados para el mismo médico y horario | 0 (lo garantiza el sistema) |
| Atención rápida en ventanilla | Tiempo para dar un turno | menos de 1 minuto |

## 6. Alcance de la primera versión

**Entra:**

- Alta y consulta de pacientes (por la recepción o autoregistro web).
- Alta de especialidades, médicos y horarios de atención semanales.
- Usuarios del personal con roles: recepcionista, médico y administrador.
- Propuesta de agenda por el médico y aprobación por el administrador (R-14).
- Registro de auditoría de turnos y bloqueos (R-12).
- Ver los horarios libres de un médico o de una especialidad en un día.
- Reservar un turno: la recepción para cualquier paciente, el paciente para sí mismo.
- Cancelar un turno: la recepción o el propio paciente.
- "Mis turnos" para el paciente y "Agenda del día" para el médico: el médico la consulta en el sistema (sólo lectura) y la recepción también puede imprimirla.
- **Bloqueo de agenda** por licencia o ausencia del médico, con aviso a los pacientes afectados (ver R-09 y R-10).

**No entra (queda para versiones siguientes):**

- Obras sociales, prepagas y autorizaciones. **[SUPUESTO S-08: hospital público, atención gratuita]**
- Recordatorios por WhatsApp o email.
- Lista de espera y sobreturnos.
- Registrar si el paciente vino o no (presente/ausente).
- Historia clínica, recetas o cualquier dato médico.
- Reportes y tableros.

## 7. Reglas del negocio

| Id | Regla | Origen |
|---|---|---|
| R-01 | Un horario de un médico se puede dar a un solo paciente. | Problema "turnos que se pisan" |
| R-02 | Sólo se dan turnos dentro de los horarios de atención del médico. Cada médico tiene su duración de turno (entre 10 y 60 minutos). | **[SUPUESTO S-09]** |
| R-03 | No se dan turnos en el pasado ni con más de 60 días de anticipación. | **[SUPUESTO S-10]** |
| R-04 | Un paciente puede tener como máximo **un turno futuro por especialidad**. Evita que alguien acapare turnos. | **[SUPUESTO S-11]** |
| R-05 | Un paciente no puede tener dos turnos a la misma hora. | Sentido común |
| R-06 | El paciente puede cancelar por la web hasta 2 horas antes. Después, sólo la recepción. | **[SUPUESTO S-12]** |
| R-07 | Un turno cancelado libera el horario para otra persona. | Problema "ausentismo" |
| R-08 | El paciente se identifica con su DNI. No puede haber dos pacientes con el mismo DNI. | **[SUPUESTO S-13]** |
| R-09 | El administrador de agendas puede bloquear días u horas de un médico (licencia, congreso, enfermedad). En un período bloqueado no se ofrecen turnos. | Decisión del docente (P-01) |
| R-10 | Al bloquear, los turnos ya dados en ese período pasan a **"Cancelado por el hospital"**. Al paciente con email se le avisa por email; los que no tienen email aparecen en una lista para que la recepción los llame. | **[SUPUESTO S-15]** |
| R-11 | Un paciente con un turno cancelado por el hospital puede sacar uno nuevo aunque ya tenga otro de esa especialidad (excepción a R-04). | **[SUPUESTO S-16]** |
| R-12 | Cada alta, cancelación o bloqueo queda registrado con quién lo hizo, cuándo y desde qué canal (web o recepción). El registro no se puede modificar ni borrar. | Decisión del docente (P-03) · Ley 25.326 |
| R-14 | Cada médico propone en el sistema su agenda (franjas semanales y duración del turno). El administrador la aprueba o la rechaza con un motivo. Sólo una agenda aprobada genera turnos. | Decisión del docente |
| R-13 | El personal (recepción, médicos, administrador) entra con usuario y contraseña, y cada rol ve sólo lo que necesita. El médico ve sólo su propia agenda. | Decisión del docente (P-02) |

## 8. Cómo entra el paciente por la web

Propuesta: el paciente pone su DNI y su email, y recibe un **código de un solo uso** por email
para entrar. No tiene que recordar una contraseña, que es lo que más falla con gente mayor.
**[SUPUESTO S-14]**

Si el paciente no tiene email, saca el turno por teléfono como hoy.

## 9. Restricciones

- Tecnología: C# / .NET con base de datos relacional (decisión del docente).
- Los datos de pacientes son **datos personales de salud**: acceso sólo a quien lo necesita, y la Ley 25.326 de Protección de Datos Personales aplica.
- Hora oficial de Argentina (sin horario de verano).
- Tiene que funcionar bien en un celular de gama baja.

## 10. Riesgos

| Riesgo | Qué hacemos |
|---|---|
| Que alguien saque turnos con el DNI de otro | Código por email en cada ingreso (S-14). |
| Que la recepción siga usando el Excel | El sistema tiene que ser más rápido que la planilla (meta de 1 minuto). |
| Que un médico falte o se tome licencia con turnos ya dados | Bloqueo de agenda con aviso al paciente (R-09, R-10). |

## 11. Preguntas abiertas

- ~~**P-01.**~~ Resuelta: el bloqueo de agenda entra en la primera versión (R-09 a R-11).
- ~~**P-02.**~~ Resuelta: las dos cosas. El médico entra a ver su agenda del día y la recepción puede imprimirla.
- ~~**P-03.**~~ Resuelta: sí, se registra quién dio, canceló o bloqueó cada turno (R-12).

## 12. Próximos pasos

1. El docente corrige supuestos y responde las preguntas abiertas.
2. John escribe el PRD (requisitos) a partir de este brief.
3. Sally diseña las pantallas de recepción y del paciente.
4. Winston define la arquitectura.
5. Se cortan las stories y recién ahí se programa.
