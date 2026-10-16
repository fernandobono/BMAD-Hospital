---
epic: 4
story: "4.1"
title: "Dominio de agenda: franjas, duración, estados de la propuesta, horarios del día y vigencia"
status: ready-for-dev
sprint: 2
priority: "P0"
approach: "[ADAPT]"
depends_on: ["1.1", "1.2", "1.3", "2.2", "3.1", "3.2"]
files_to_create:
  - "src/HospitalTurnos.Dominio/Agendas/Franja.cs"
  - "src/HospitalTurnos.Dominio/Agendas/EstadoPropuesta.cs"
  - "src/HospitalTurnos.Dominio/Agendas/AgendaMedico.cs"
  - "src/HospitalTurnos.Dominio/Agendas/EstadoDeAgenda.cs"
  - "src/HospitalTurnos.Aplicacion/Agendas/IConsultaAgendas.cs"
  - "src/HospitalTurnos.Infraestructura/Agendas/ConsultaAgendasEnBase.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/AgendaMedicoConfiguracion.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/FranjaConfiguracion.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/<marca>_AgregarAgendas.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/<marca>_AgregarAgendas.Designer.cs"
  - "tests/HospitalTurnos.Dominio.Tests/Ayudas/AgendaBuilder.cs"
  - "tests/HospitalTurnos.Dominio.Tests/Ayudas/RelojDePrueba.cs"
  - "tests/HospitalTurnos.Dominio.Tests/Agendas/FranjaTests.cs"
  - "tests/HospitalTurnos.Dominio.Tests/Agendas/EdicionDeAgendaTests.cs"
  - "tests/HospitalTurnos.Dominio.Tests/Agendas/EstadosDeAgendaTests.cs"
  - "tests/HospitalTurnos.Dominio.Tests/Agendas/HorariosDelDiaTests.cs"
  - "tests/HospitalTurnos.Dominio.Tests/Agendas/VigenciaTests.cs"
  - "tests/HospitalTurnos.Dominio.Tests/Agendas/EstadoDeAgendaTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Agendas/DatosDeAgenda.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Agendas/PersistenciaAgendasTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Agendas/ConsultaAgendasTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Catalogo/EstadoDeAgendaEnA1HttpTests.cs"
files_to_modify:
  - "src/HospitalTurnos.Infraestructura/Persistencia/HospitalDbContext.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/HospitalDbContextModelSnapshot.cs"
  - "src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs"
  - "src/HospitalTurnos.Infraestructura/Catalogo/GestionMedicosEnBase.cs"
---

# Story 4.1: Dominio de agenda: franjas, duración, estados de la propuesta, horarios del día y vigencia

Status: ready-for-dev

## Story

Como equipo de desarrollo del Hospital Las Acacias,
quiero las reglas de la agenda de un médico escritas y probadas en el dominio, guardadas en la
base y con una consulta de "qué agenda rige en una fecha",
para que la propuesta del médico (4.2), la aprobación (4.3), la corrección de urgencia (4.4), el
listado de médicos (3.2) y los horarios libres (5.2) usen **una sola** versión de esas reglas.

> **Qué es y qué no es esta story.** Es el corazón de la épica 4: entidades, reglas, tablas y una
> consulta. **No** tiene pantallas ni casos de uso. Nadie puede crear una agenda desde la web hasta
> la 4.2 (sprint 3). Por eso tampoco audita: no hay ninguna acción de una persona que registrar.

## Acceptance Criteria

> Vocabulario de esta story:
>
> - **Agenda** (`AgendaMedico`): una versión de la agenda semanal de un médico. Mientras no está
>   aprobada, se la llama **propuesta** (FR-14).
> - **Franja**: un tramo semanal de atención: día de la semana, hora desde y hora hasta. Ejemplo:
>   lunes de 08:00 a 12:00.
> - **Horario**: el inicio de un turno dentro de una franja (lunes 9/11 a las 08:20).
> - **Hoy**: la fecha **local del hospital** (Córdoba), leída del `TimeProvider` inyectado
>   (contrato técnico §Hora). Nunca la fecha UTC.
>
> Errores: las reglas de datos lanzan `ReglaDeNegocioException` (→ 400 por el manejador de la 1.2);
> un cambio de estado que no corresponde lanza `ConflictoException` (→ 409). El texto de cada
> excepción es el que verá la persona en las pantallas de 4.2 y 4.3, y se compara exacto en las
> pruebas. Las fechas de los mensajes van `dd/MM/yyyy` y las horas `HH:mm`, con
> `CultureInfo.InvariantCulture`; los días, en castellano y en minúscula (`lunes`).

### Franjas — FR-14, R-02

1. **Given** un día, una hora desde y una hora hasta **When** se crea `new Franja(dia, desde, hasta)`
   **Then** queda con esos valores. `Hasta = 00:00` significa **medianoche, al final del día**
   (`TimeOnly` no puede valer 24:00). Expone `DuracionMinutos` (fin menos inicio, contando la
   medianoche como 1440) y `TerminaAMedianoche`. Ejemplos: 08:00–12:00 → 240 y `false`;
   20:00–00:00 → 240 y `true`; 00:00–00:00 → 1440 y `true` (el día entero).
2. **Given** horas que no forman una franja **When** se crea **Then** lanza `ReglaDeNegocioException`:
   (a) el fin no es posterior al inicio (10:00–10:00, 12:00–08:00, y también 22:00–02:00: una
   franja **no cruza** la medianoche; se carga como dos franjas, lunes 22:00–00:00 y martes
   00:00–02:00) → `La franja tiene que terminar después de empezar.`; (b) una hora con segundos o
   fracciones → `Las horas de la franja van en horas y minutos, sin segundos.`
3. **Given** dos franjas **When** se llama `SeSuperponeCon(otra)` **Then** es `true` sólo si son
   del mismo día y `inicio < finDeLaOtra && inicioDeLaOtra < fin` (en minutos, medianoche = 1440).
   Tocarse no es superponerse: 08:00–12:00 y 12:00–16:00 conviven. Distinto día, nunca.
4. **Given** una agenda editable con la franja lunes 08:00–12:00 **When** se agrega lunes
   10:00–14:00 **Then** lanza `ReglaDeNegocioException` con
   `La franja del lunes de 10:00 a 14:00 se superpone con la de 08:00 a 12:00.` y la agenda no
   cambia. Una franja que termina a medianoche se muestra `a 24:00`. Varias franjas el mismo día
   que no se pisan se aceptan (mañana y tarde). Una franja repetida exacta también es superposición.
5. **Given** una agenda editable **When** se llama `QuitarFranja(dia, desde)` **Then** saca la
   franja de ese día que empieza a esa hora. Si no hay ninguna, lanza `NoEncontradoException` con
   `Esa franja no está en la agenda.`

### Duración y vigencia — FR-14, R-02

6. **Given** una duración entre 10 y 60 minutos, bordes incluidos **When** se crea la agenda o se
   llama `CambiarDuracion` **Then** se acepta. **Given** 9, 61 o 0 **Then** lanza
   `ReglaDeNegocioException` con `La duración del turno tiene que estar entre 10 y 60 minutos.` y la
   agenda no cambia. Cualquier número entero del rango vale (FR-14 no pide múltiplos de 5).
7. **Given** una fecha **When** se crea la agenda o se llama `CambiarVigenteDesde` **Then**
   `VigenteDesde` (`DateOnly`) toma ese valor. La regla de los 7 días **no** se mira acá: se mira
   al enviar (AC 9), porque S-31 la cuenta desde el envío.

### Estados de la propuesta — FR-14b, Arq. §6

8. **Given** un médico **When** se crea `new AgendaMedico(medicoId, duracionTurnoMinutos, vigenteDesde)`
   **Then** queda en `Borrador`, sin franjas, con `MotivoRechazo`, `EnviadaEl`, `AprobadaEl` y
   `RechazadaEl` en `null`.
9. **Given** una agenda en `Borrador` **When** se llama `Enviar(reloj)` **Then**:
   (a) sin franjas → `ReglaDeNegocioException` con `Agregá al menos una franja antes de enviar la propuesta.`;
   (b) con `VigenteDesde` anterior a hoy + 7 días (S-31: "al menos 7 días posterior al envío") →
   `ReglaDeNegocioException` con `La agenda tiene que regir desde el <hoy+7> o después: 7 días desde hoy.`;
   (c) si no, pasa a `Enviada`, `EnviadaEl = reloj.GetUtcNow()` (offset 0, marca técnica) y
   `MotivoRechazo = null`. Hoy + 7 exacto se acepta. "Hoy" es la fecha local del hospital: a las
   23:30 del viernes 16/10 en Córdoba (02:30 UTC del sábado 17) el mínimo es el 23/10, no el 24/10.
10. **Given** una agenda `Enviada` **When** se llama `Rechazar(motivo, reloj)` **Then** el motivo se
    recorta (`Trim`) y: vacío o sólo espacios → `ReglaDeNegocioException` con
    `Escribí el motivo del rechazo.`; más de 500 caracteres → `El motivo tiene como máximo 500 caracteres.`;
    si no, pasa a `Rechazada` con `MotivoRechazo` y `RechazadaEl = reloj.GetUtcNow()` (FR-14c).
11. **Given** una agenda `Enviada` **When** se llama `Aprobar(reloj)` **Then** pasa a `Aprobada`
    con `AprobadaEl = reloj.GetUtcNow()`. **Given** que su `VigenteDesde` ya pasó (es anterior a
    hoy) **Then** lanza `ConflictoException` con
    `La agenda iba a regir desde el <VigenteDesde>, que ya pasó: rechazala para que el médico elija otra fecha.`
    y sigue `Enviada` (decisión 11 del docente). Aprobar el mismo día de `VigenteDesde` se acepta.
12. **Given** una agenda `Rechazada` **When** se la edita con cualquiera de los cuatro métodos de
    edición (`CambiarDuracion`, `CambiarVigenteDesde`, `AgregarFranja`, `QuitarFranja`) y la
    edición es válida **Then** pasa a `Borrador` ("Rechazada → Borrador: médico corrige", Arq. §6).
    `MotivoRechazo` y `RechazadaEl` se **conservan** hasta el próximo `Enviar` (UX M2 muestra el
    motivo mientras el médico corrige). **Given** una edición inválida **Then** lanza su error y la
    agenda sigue `Rechazada`, sin ningún cambio.
13. **Given** un cambio de estado que el diagrama de Arq. §6 no tiene **When** se intenta **Then**
    lanza `ConflictoException` y la agenda no cambia en nada:

    | Intento | Desde | Mensaje |
    |---|---|---|
    | `Enviar` | `Enviada` | `La propuesta ya fue enviada.` |
    | `Enviar` | `Rechazada` (sin corregir) | `Corregí la propuesta antes de reenviarla.` |
    | `Enviar` | `Aprobada` | `Una agenda aprobada no se modifica: armá una propuesta nueva.` |
    | `Aprobar` | `Borrador`, `Rechazada`, `Aprobada` | `Sólo se puede aprobar una propuesta enviada.` |
    | `Rechazar` | `Borrador`, `Rechazada`, `Aprobada` | `Sólo se puede rechazar una propuesta enviada.` |
    | cualquier edición | `Enviada` | `La propuesta está esperando aprobación: no se puede modificar.` |
    | cualquier edición | `Aprobada` | `Una agenda aprobada no se modifica: armá una propuesta nueva.` |

    El orden de los controles es: primero el estado, después los datos. Así un `Enviar` sobre una
    `Rechazada` cuya `VigenteDesde` ya no cumple los 7 días da el 409 del estado, no el 400 de la
    fecha.

### Horarios del día — FR-14, FR-17 (la parte de la agenda), ADR-04

14. **Given** una agenda y una fecha **When** se llama `HorariosDelDia(fecha)` **Then** devuelve
    (`IReadOnlyList<DateTime>`, `Kind = Unspecified`, hora local del hospital, contrato §Base de
    datos) los inicios de turno de las franjas de `fecha.DayOfWeek`, **ordenados**: en cada franja,
    uno cada `DuracionTurnoMinutos` desde `Desde`, mientras el turno **entre completo**
    (`inicio + duración ≤ fin`). Ejemplo: lunes 08:00–12:00 de 20 minutos → 12 horarios, de 08:00 a
    11:40. Este cálculo **no** mira el estado ni la vigencia de la agenda (eso es el AC 20), ni
    bloqueos, ni turnos tomados, ni la ventana de 60 días (eso es de la 5.2). Los horarios no se
    guardan: se calculan (ADR-04).
15. **Given** una franja que **no es múltiplo** de la duración (lunes 08:00–10:50, turnos de 20)
    **When** se calculan los horarios **Then** salen 8 (08:00 a 10:20) y los últimos 10 minutos no
    se ofrecen: un turno que no entra completo no se da (decisión 12).
16. **Given** una franja que **termina a medianoche** (lunes 20:00–00:00, turnos de 30) **When** se
    calculan los horarios del lunes **Then** salen 8, el último a las 23:30 (termina justo a las
    24:00), y el martes no aparece ninguno que venga de esa franja.
17. **Given** una fecha cuyo día de la semana **no tiene franja** **When** se calculan los horarios
    **Then** la lista está vacía. No es un error.
18. **Given** una agenda **When** se llama `AtiendeEn(fechaHora)` **Then** es `true` sólo si esa
    fecha y hora es uno de los `HorariosDelDia` de su fecha (lunes 08:20 sí; lunes 08:10 no, cae en
    el medio de un turno; martes 08:20 no, no hay franja).
19. **Given** una agenda **When** se llama `TurnosPorSemana()` **Then** devuelve la suma, en todas
    sus franjas, de los turnos que entran completos (UX M2 "ofrecés N turnos por semana" y UX A6
    "24 turnos/semana"). Lunes 08:00–12:00 y miércoles 08:00–10:50, de 20 minutos → 12 + 8 = 20.

### Qué agenda rige en una fecha — FR-14d, ADR-06

20. **Given** las agendas de un médico y una fecha **When** se llama
    `AgendaMedico.QueRigeEn(agendas, fecha)` **Then** devuelve la **Aprobada** con la `VigenteDesde`
    más reciente que no sea posterior a esa fecha (Arq. §6, "Cómo se arma la disponibilidad"). Si
    dos aprobadas tienen la misma `VigenteDesde`, gana la de `AprobadaEl` más reciente (la que el
    administrador aprobó después). Borradores, enviadas y rechazadas no rigen nunca. Sin ninguna
    que cumpla → `null` ("sin agenda": el médico no ofrece turnos ese día, FR-13). Es la regla de
    Arq. §6 tal cual (decisión 13): una aprobada nueva con fecha anterior a otra aprobada que todavía
    no empezó rige hasta que empiece la otra.
21. **Given** una agenda A aprobada desde el lunes 02/11 y otra B aprobada desde el lunes 16/11
    **When** se pregunta por el domingo 01/11, el lunes 09/11, el domingo 15/11 y el lunes 16/11
    **Then** rige nada, A, A y B respectivamente: "hasta esa fecha sigue rigiendo la anterior"
    (FR-14d). Los horarios del 09/11 salen de las franjas de A y los del 16/11, de las de B.
22. **Given** las agendas de **un** médico y hoy **When** se llama
    `EstadoDeAgenda.Calcular(agendas, hoy)` (`Dominio/Agendas/EstadoDeAgenda.cs`) **Then** devuelve
    una `SituacionDeAgenda` **de la 3.2** (`HospitalTurnos.Dominio.Agendas`: registro con el enum
    `EstadoAgendaMedico` y una fecha `RigeDesde`; contrato del AC 23 de la 3.2), con la regla de la
    decisión 8 del docente: `PropuestaPendiente` si tiene alguna `Enviada`; si no, `Vigente` si
    `QueRigeEn(agendas, hoy)` devuelve una; si no, `SinAgenda`. Una aprobada que **todavía no rige**
    da `SinAgenda` (hoy no ofrece turnos; RU-07: la Dra. Méndez "todavía no tiene agenda" hasta el 1
    de diciembre) **con** `RigeDesde` = la `VigenteDesde` más próxima de las aprobadas que todavía no
    rigen, para que A1 muestre `Sin agenda (rige desde 01/12)`. Sin ninguna aprobada futura,
    `RigeDesde = null`. Borradores y rechazadas no cuentan.

### Persistencia

23. Migración `<marca>_AgregarAgendas` (generada con `dotnet ef migrations add`, como la 2.1) y
    snapshot actualizado. Crea, en `snake_case` por la convención de la 1.1:

    | Tabla | Columna | Tipo | Nota |
    |---|---|---|---|
    | `agendas_medico` | `id` | `bigint` identity | |
    | | `medico_id` | `bigint` (como `Medico.Id`, `long`, 3.2) | FK a `medicos.id`, `ON DELETE RESTRICT` |
    | | `duracion_turno_minutos` | `integer` | |
    | | `vigente_desde` | `date` | |
    | | `estado` | `varchar(20)` | el nombre del enum (`Borrador`, `Enviada`, ...), como `canal` en la 1.3 |
    | | `motivo_rechazo` | `varchar(500)` null | |
    | | `enviada_el`, `aprobada_el`, `rechazada_el` | `timestamptz` null | marcas técnicas UTC |
    | `franjas_agenda` | `id` | `bigint` identity | |
    | | `agenda_medico_id` | `bigint` `NOT NULL` | FK a `agendas_medico`, `ON DELETE CASCADE`; no hay franjas huérfanas |
    | | `dia` | `integer` | `DayOfWeek` de .NET: 0 = domingo ... 6 = sábado |
    | | `desde`, `hasta` | `time without time zone` | `hasta = 00:00` = medianoche |

    Índices: `ix_agendas_medico_medico_id_estado_vigente_desde` (la consulta del AC 29) y el único
    parcial del AC 25. La migración se aplica al arrancar, como todas (contrato §Base de datos).
24. Permisos de `turnos_app` (con `REVOKE ALL` + `GRANT`, como `AgregarBandejaDeSalida` de la 1.4):
    sobre `agendas_medico`, `SELECT, INSERT, UPDATE` y **no** `DELETE` (ADR-06: las agendas se
    versionan y su historia no se borra); sobre `franjas_agenda`, `SELECT, INSERT, UPDATE, DELETE`
    (quitar una franja de un borrador borra su fila). `registros_auditoria` sigue sin `UPDATE`
    para `turnos_app` (no regresión de la 1.3).
25. **Una sola enviada por médico** (FR-14b: "como máximo una propuesta enviada a la vez"): índice
    único parcial `ux_agendas_medico_una_enviada` sobre `medico_id` con filtro `estado = 'Enviada'`.
    Una segunda `Enviada` del mismo médico falla al guardar con `23505` y ese nombre de restricción.
    Otro médico puede tener la suya; y cuando la primera pasa a `Rechazada` o `Aprobada`, la segunda
    ya se puede guardar `Enviada`. Es la segunda defensa, como el índice de turnos de Arq. §7.1: la
    primera (el control previo con un mensaje claro) es de la 4.2.
26. **Controles en la base** (NFR-10, validación doble): `ck_agendas_medico_duracion`
    (`duracion_turno_minutos BETWEEN 10 AND 60`), `ck_franjas_agenda_dia` (`dia BETWEEN 0 AND 6`) y
    `ck_franjas_agenda_horas` (`hasta > desde OR hasta = '00:00'`). Un `INSERT` a mano que los
    viole falla con `23514`.
27. **Cambios simultáneos**: `agendas_medico` lleva control de concurrencia optimista con la
    columna de sistema `xmin` de PostgreSQL (propiedad sombra `uint Version` con `IsRowVersion()`).
    **Given** dos contextos que leyeron la misma agenda `Enviada` **When** uno la aprueba y guarda,
    y el otro la rechaza y guarda **Then** el segundo `SaveChangesAsync` lanza
    `DbUpdateConcurrencyException` y en la base queda `Aprobada`. (La 4.3 lo traduce a un 409.)
28. **Ida y vuelta**: una agenda guardada con `turnos_app` se lee igual: estado, duración,
    `VigenteDesde`, marcas y franjas, incluida una que termina a `00:00`. Quitar una franja de un
    borrador guardado y guardar borra su fila de `franjas_agenda`.

### Consulta de agendas — para 3.2, 4.2, 4.3 y 5.2

29. Puerto `IConsultaAgendas` (Aplicacion/Agendas), implementado por `ConsultaAgendasEnBase`
    (Infraestructura/Agendas) y registrado `Scoped` en `AgregarInfraestructura`:
    - `Task<AgendaMedico?> AgendaQueRigeAsync(long medicoId, DateOnly fecha, CancellationToken ct)`:
      la agenda que rige con sus franjas, sin seguimiento de cambios (`AsNoTracking`). Lee de la
      base sólo las `Aprobada` de ese médico con `VigenteDesde <= fecha` y elige con
      `AgendaMedico.QueRigeEn` (AC 20): **una sola regla**, en el dominio. Médico sin agendas,
      fecha anterior a todas, o médico que no existe → `null`.
    - `Task<IReadOnlyDictionary<long, SituacionDeAgenda>> EstadosDeAgendaAsync(IReadOnlyCollection<long> medicoIds, CancellationToken ct)`:
      una situación por **cada** id pedido (los que no tienen agendas, `SituacionDeAgenda.SinAgenda`), calculada con
      `EstadoDeAgenda.Calcular` (AC 22) y "hoy" tomado del `TimeProvider` inyectado en
      `ConsultaAgendasEnBase`, con **una** consulta a la base para toda la lista (no una por médico:
      el listado A1 de la 3.2 muestra todos los médicos juntos).
30. **El estado real en A1 lo conecta esta story, en el mismo sprint** (contrato del AC 23 de la 3.2;
    decisión 8): `GestionMedicosEnBase` pasa de
    `(contexto, auditoria, catalogo, actorVigente)` a
    `(contexto, auditoria, catalogo, actorVigente, consultaAgendas)` (`IConsultaAgendas`, agregado al
    final) y, en `ListarAsync` y `ObtenerAsync`, reemplaza la asignación
    `Agenda = SituacionDeAgenda.SinAgenda` (la línea marcada con el comentario que nombra a la 4.1)
    por el valor de `EstadosDeAgendaAsync`: una sola llamada con los ids de todo el listado, no una
    por fila. `EstadoAgendaMedico`, `SituacionDeAgenda`, `TablaMedicos` y las pantallas de la 3.2
    **no** cambian. Las pruebas de la 3.2 resuelven `GestionMedicosEnBase` desde el contenedor (no lo
    construyen a mano): no se tocan, y sus resultados esperados siguen valiendo (esos médicos no
    tienen agendas: `SinAgenda`). **Given** un médico con una aprobada que todavía no rige **When** el
    administrador abre A1 **Then** su fila dice `Sin agenda (rige desde dd/MM)` con la fecha de esa
    agenda (T44).

### Contrato con otras stories

31. **(Declarativo: no tiene prueba propia; lo cumplen los AC citados y la Task 1 de cada story
    siguiente, M-4 de la revisión.)** Lo que esta story **ofrece** y lo que cada story siguiente
    **tiene que cumplir**:
    - **3.2 Médicos** (se implementa antes; **su AC 23 es el contrato único** del estado de agenda, y
      esta story lo copia sin cambiarlo): `Medico.Id` es `long`, la tabla es `medicos` y
      `AgendaMedico.MedicoId` es `long` con FK a `medicos.id`; `EstadoAgendaMedico` y
      `SituacionDeAgenda` están en `src/HospitalTurnos.Dominio/Agendas/` (los creó la 3.2: esta story
      no crea otro enum ni otra carpeta); `GestionMedicosEnBase` recibe `IConsultaAgendas` como
      quinto parámetro y **esta story** conecta el estado real (AC 30). Esta story **no** modifica la
      entidad `Medico` ni le agrega navegación a sus agendas (decisión 14).
    - **4.2 Propuesta del médico**: crea y edita con los métodos de esta story y llama a
      `Enviar(reloj)`. Antes de guardar una `Enviada` revisa que el médico no tenga otra, para dar
      un mensaje claro (el índice del AC 25 es la segunda defensa; si igual salta, traduce el
      `23505` de `ux_agendas_medico_una_enviada` a `ConflictoException`). Decide si un médico puede
      tener más de un borrador a la vez (el PRD no lo dice). Audita con la constante que **ya
      existe** `AccionesAuditoria.PropuestaAgendaEnviada` (`"propuesta-agenda.enviada"`, story 1.3).
      Un usuario médico sin vínculo ve
      `Tu usuario no está vinculado a un médico. Pedíselo al administrador.` (contrato de la 3.2,
      decisión 17).
    - **4.3 Aprobación**: llama a `Aprobar(reloj)` / `Rechazar(motivo, reloj)`; audita con
      `PropuestaAgendaAprobada` y `PropuestaAgendaRechazada` (ya existen); traduce
      `DbUpdateConcurrencyException` a `ConflictoException`. "Rechazada por Lucía P." (UX M2) sale
      del registro de auditoría del rechazo (`Quien`), no de una columna de esta tabla. Además
      (decisiones 11 y 13): si la `VigenteDesde` de la propuesta ya pasó, A6 lo muestra **antes** del
      botón `Aprobar` (no sólo al fallar con el 409 del AC 11); y al aprobar una agenda con fecha
      anterior a otra aprobada que todavía no empezó, avisa "Hay una agenda aprobada que empieza el
      dd/MM y va a reemplazar a esta".
    - **4.4 Corrección de urgencia** (FR-14f, S-32): no entra por el ciclo Borrador → Enviada. La
      4.4 agrega su propia forma de crear una agenda ya aprobada (con motivo, auditada con
      `AgendaCorregida`, que ya existe) y la elige `QueRigeEn` como a cualquier aprobada.
    - **5.2 Disponibilidad**: usa `AgendaQueRigeAsync` + `HorariosDelDia` y les quita bloqueos,
      turnos tomados y lo que está fuera de ahora a 60 días (FR-17). Si necesita muchos días de
      muchos médicos de una vez, agrega su propia consulta reutilizando `QueRigeEn`.
    - **5.3 Reserva**: valida el horario pedido con `AtiendeEn` de la agenda que rige.
    - **Auditoría**: esta story no escribe en `registros_auditoria` ni agrega constantes a
      `AccionesAuditoria`: no tiene casos de uso. Las constantes de agenda ya están desde la 1.3.

### No-regresión

32. Siguen en verde las pruebas de las stories 1.1 a 2.1 y las de 2.2, 3.1 y 3.2 (sin modificar
    ninguna: las de la 3.2 resuelven `GestionMedicosEnBase` del contenedor). La prueba de la
    1.2 que prohíbe leer la hora del sistema (`HoraDelSistemaTests`) sigue en verde: el dominio
    recibe el `TimeProvider` como parámetro. La cobertura de líneas de Dominio y Aplicacion sigue
    sobre el 80 % de la CI (NFR-11).

### Tests (44 verificaciones)

| # | Test | AC | Nivel | Escenario | Resultado esperado |
|---|------|----|-------|-----------|--------------------|
| T1 | `Franja_HorasValidas_ExponeDuracionYMedianoche` | 1 | unit | `[Theory]`: lunes 08:00–12:00; lunes 20:00–00:00; domingo 00:00–00:00 | `DuracionMinutos` 240 / 240 / 1440; `TerminaAMedianoche` false / true / true; `Dia`, `Desde`, `Hasta` iguales a lo pasado |
| T2 | `Franja_HorasInvalidas_LanzaReglaDeNegocio` | 2 | unit | `[Theory]`: 10:00–10:00; 12:00–08:00; 22:00–02:00; 08:00:30–12:00; 08:00–12:00:15 | `ReglaDeNegocioException`; los tres primeros con `La franja tiene que terminar después de empezar.`, los dos últimos con `Las horas de la franja van en horas y minutos, sin segundos.` |
| T3 | `SeSuperponeCon_CasosDeBorde_DevuelveSiSePisan` | 3 | unit | `[Theory]` lunes 08–12 contra: lunes 10–14 (true); lunes 12–16 (false, se tocan); lunes 06–08 (false); lunes 09–10 (true, contenida); martes 08–12 (false); y lunes 22:00–00:00 contra lunes 23:00–23:30 (true) y contra lunes 00:00–02:00 (false) | El valor indicado, y el mismo resultado al invertir el orden (`b.SeSuperponeCon(a)`) |
| T4 | `AgregarFranja_SuperpuestaEnElMismoDia_LanzaYNoLaAgrega` | 4 | unit | Borrador con lunes 08:00–12:00; `[Theory]`: agregar lunes 10:00–14:00; agregar lunes 08:00–12:00 (repetida); con lunes 20:00–00:00 cargada, agregar lunes 23:00–23:30 | `ReglaDeNegocioException`; mensajes `La franja del lunes de 10:00 a 14:00 se superpone con la de 08:00 a 12:00.` / `... de 08:00 a 12:00 se superpone con la de 08:00 a 12:00.` / `La franja del lunes de 23:00 a 23:30 se superpone con la de 20:00 a 24:00.`; `Franjas` sin cambios |
| T5 | `AgregarFranja_MananaYTardeElMismoDia_AgregaLasDos` | 4 | unit | Borrador; agregar lunes 08:00–12:00 y lunes 14:00–18:00 y martes 08:00–12:00 | 3 franjas |
| T6 | `QuitarFranja_SegunDiaYHora_SacaSoloEsaOLanzaSiNoEsta` | 5 | unit | Borrador con lunes 08–12 y lunes 14–18: quitar (lunes, 14:00); después quitar (martes, 08:00) | Queda sólo lunes 08–12; la segunda lanza `NoEncontradoException` con `Esa franja no está en la agenda.` |
| T7 | `Duracion_EnLosBordes_SeAcepta` | 6 | unit | `[Theory]` 10 y 60, en el constructor y en `CambiarDuracion` | `DuracionTurnoMinutos` = el valor |
| T8 | `Duracion_FueraDeRango_LanzaYNoCambia` | 6 | unit | `[Theory]` 9, 61 y 0: en el constructor; y `CambiarDuracion` sobre un borrador de 20 | `ReglaDeNegocioException` con `La duración del turno tiene que estar entre 10 y 60 minutos.`; el borrador sigue en 20 |
| T9 | `NuevaAgenda_RecienCreada_QuedaEnBorradorSinFranjasNiMarcas` | 7,8 | unit | `new AgendaMedico(<id>, 20, 02/11/2026)`; después `CambiarVigenteDesde(09/11/2026)` | `Estado = Borrador`, `Franjas` vacía, `MotivoRechazo`, `EnviadaEl`, `AprobadaEl`, `RechazadaEl` nulos, `MedicoId` el pasado; `VigenteDesde` = 09/11/2026 (no valida los 7 días) |
| T10 | `Enviar_ConSieteDiasOMas_PasaAEnviadaConMarcaUtc` | 9 | unit | Reloj 16/10/2026 12:00 Córdoba; `[Theory]` `VigenteDesde` 23/10/2026 (hoy + 7) y 30/11/2026 | `Enviada`; `EnviadaEl` = `reloj.GetUtcNow()` y `EnviadaEl.Offset = 0` |
| T11 | `Enviar_ConMenosDeSieteDias_LanzaConLaFechaMinima` | 9 | unit | Mismo reloj; `[Theory]` `VigenteDesde` 22/10/2026 (hoy + 6) y 16/10/2026 | `ReglaDeNegocioException` con `La agenda tiene que regir desde el 23/10/2026 o después: 7 días desde hoy.`; sigue `Borrador`, `EnviadaEl` nulo |
| T12 | `Enviar_CercaDeMedianoche_UsaLaFechaLocalDelHospital` | 9 | unit | Reloj en `2026-10-17T02:30Z` (16/10 23:30 en Córdoba); `[Theory]` `VigenteDesde` 23/10 y 22/10 | 23/10 → `Enviada` (si usara la fecha UTC, el mínimo sería el 24/10 y fallaría); 22/10 → lanza con `... desde el 23/10/2026 ...` |
| T13 | `Enviar_SinFranjas_Lanza` | 9 | unit | Borrador sin franjas, `VigenteDesde` válida | `ReglaDeNegocioException` con `Agregá al menos una franja antes de enviar la propuesta.`; sigue `Borrador` |
| T14 | `Rechazar_ConMotivo_PasaARechazadaConElMotivoRecortado` | 10 | unit | Enviada; `Rechazar("  El consultorio 3 está ocupado.  ", reloj)` | `Rechazada`; `MotivoRechazo = "El consultorio 3 está ocupado."`; `RechazadaEl` = `reloj.GetUtcNow()` |
| T15 | `Rechazar_MotivoVacioOLargo_LanzaYSigueEnviada` | 10 | unit | Enviada; `[Theory]` `""`, `"   "`, 501 letras `a` | `""` y `"   "` → `Escribí el motivo del rechazo.`; 501 → `El motivo tiene como máximo 500 caracteres.`; sigue `Enviada`, `RechazadaEl` nulo. (Con 500 letras exactas se acepta: caso extra del mismo `[Theory]`) |
| T16 | `Aprobar_Enviada_PasaAAprobadaConMarca` | 11 | unit | Enviada con `VigenteDesde` 02/11/2026; `[Theory]` aprobar con el reloj en 20/10/2026 y en 02/11/2026 (el mismo día) | `Aprobada`; `AprobadaEl` = `reloj.GetUtcNow()` |
| T17 | `Aprobar_VigenteDesdeYaPasada_LanzaConflictoYSigueEnviada` | 11 | unit | Enviada con `VigenteDesde` 02/11/2026; reloj en 03/11/2026 | `ConflictoException` con `La agenda iba a regir desde el 02/11/2026, que ya pasó: rechazala para que el médico elija otra fecha.`; sigue `Enviada`, `AprobadaEl` nulo |
| T18 | `CorregirRechazada_CualquierEdicionValida_VuelveABorradorConElMotivo` | 12 | unit | `[Theory]` sobre una Rechazada con motivo `X`: `CambiarDuracion(30)`; `CambiarVigenteDesde(...)`; `AgregarFranja(miércoles 08–12)`; `QuitarFranja(lunes, 08:00)` | `Borrador`; `MotivoRechazo = "X"` y `RechazadaEl` sin cambios; el dato editado cambió |
| T19 | `CorregirRechazada_EdicionInvalida_SigueRechazadaSinCambios` | 12 | unit | Rechazada; `[Theory]`: `CambiarDuracion(5)`; `AgregarFranja` superpuesta | Lanza el error de su AC; `Rechazada`; duración y franjas sin cambios |
| T20 | `RecorridoCompleto_RechazoYReenvio_TerminaAprobada` | 8–12 | unit | Borrador → `Enviar` → `Rechazar("motivo")` → `CambiarDuracion(30)` → `Enviar` → `Aprobar` (el recorrido RU-07b) | Estados en ese orden: Borrador, Enviada, Rechazada, Borrador, Enviada, Aprobada; al reenviar `MotivoRechazo = null` |
| T21 | `TransicionNoPrevista_EnCadaEstado_LanzaConflictoSinCambiar` | 13 | unit | `[Theory]` con las 15 combinaciones de la tabla del AC 13 (Enviar ×3, Aprobar ×3, Rechazar ×3, las 4 ediciones sobre Enviada, `CambiarDuracion` y `AgregarFranja` sobre Aprobada), más `Enviar` sobre una Rechazada con `VigenteDesde` 20/10/2026 (reloj en 16/10: tampoco cumple los 7 días) | `ConflictoException` con el mensaje de la tabla; `Estado`, duración, `VigenteDesde`, franjas y marcas iguales a antes |
| T22 | `HorariosDelDia_FranjaMultiploDeLaDuracion_DevuelveCadaInicio` | 14 | unit | Lunes 08:00–12:00, 20 min; fecha lunes 09/11/2026 | 12 horarios: 08:00, 08:20, ..., 11:40 del 09/11/2026; todos `Kind = Unspecified` |
| T23 | `HorariosDelDia_FranjaNoMultiplo_NoOfreceElTurnoQueNoEntra` | 15 | unit | Lunes 08:00–10:50, 20 min | 8 horarios, el último 10:20 |
| T24 | `HorariosDelDia_FranjaHastaMedianoche_ElUltimoTerminaALas24` | 16 | unit | Lunes 20:00–00:00, 30 min; fechas lunes 09/11 y martes 10/11 | Lunes: 8 horarios, el último 23:30 del 09/11; martes: vacío |
| T25 | `HorariosDelDia_DiaSinFranja_DevuelveVacio` | 17 | unit | Lunes 08–12; fecha martes 10/11 | Lista vacía, sin excepción |
| T26 | `HorariosDelDia_FranjasCargadasDesordenadas_DevuelveEnOrden` | 14 | unit | Agregar primero lunes 14:00–15:00 y después lunes 08:00–09:00, 30 min | 08:00, 08:30, 14:00, 14:30 |
| T27 | `AtiendeEn_SegunLaHora_DevuelveSiEsUnInicioDeTurno` | 18 | unit | Lunes 08–12, 20 min; `[Theory]` lunes 09/11 08:20 (true), 08:10 (false), 11:40 (true), 12:00 (false); martes 10/11 08:20 (false) | El valor indicado |
| T28 | `TurnosPorSemana_VariasFranjas_SumaLosQueEntranCompletos` | 19 | unit | Lunes 08:00–12:00 y miércoles 08:00–10:50, 20 min | 20 |
| T29 | `QueRigeEn_VariasAgendas_EligeLaAprobadaMasRecienteNoPosterior` | 20,21 | unit | A aprobada desde 02/11, B aprobada desde 16/11, C enviada desde 09/11, D rechazada desde 26/10; `[Theory]` fechas 26/10, 01/11, 02/11, 09/11, 15/11, 16/11, 30/11 | null, null, A, A, A, B, B |
| T30 | `QueRigeEn_MismaVigencia_GanaLaAprobadaDespues` | 20 | unit | A y B con `VigenteDesde` 02/11; A aprobada con el reloj en 20/10 10:00, B en 20/10 11:00; lista `[B, A]` y lista `[A, B]` | B en los dos órdenes |
| T31 | `CambioDeAgenda_AntesYDesdeLaFecha_UsaLasFranjasDeCadaAgenda` | 21 | unit | A: lunes 08–12 de 20 min, desde 02/11; B: lunes 14–16 de 30 min, desde 16/11; `QueRigeEn` + `HorariosDelDia` para lunes 09/11 y lunes 16/11 | 09/11 → 12 horarios de 08:00 a 11:40; 16/11 → 14:00, 14:30, 15:00, 15:30 |
| T32 | `Calcular_SegunLasAgendasDelMedico_DevuelveSuSituacion` | 22 | unit | `EstadoDeAgenda.Calcular(agendas, hoy = 16/10/2026)`; `[Theory]`: (a) ninguna; (b) un borrador; (c) una rechazada; (d) una aprobada desde el 12/10 (rige hoy); (e) una aprobada que empieza el 02/11; (f) una aprobada que rige hoy y una enviada; (g) sólo una enviada; (h) dos aprobadas que todavía no rigen, desde el 16/11 y desde el 02/11 | `Estado` / `RigeDesde`: (a–c) SinAgenda / null; (d) Vigente / null; (e) **SinAgenda / 02/11/2026**; (f, g) PropuestaPendiente / null; (h) SinAgenda / 02/11/2026 (la más próxima) |
| T33 | `Migracion_AgregarAgendas_CreaTablasColumnasYPermisos` | 23,24,32 | integration | `information_schema` y `has_table_privilege` contra la `PostgresFixture` | Las 2 tablas con las columnas y tipos del AC 23; `turnos_app`: `agendas_medico` SELECT/INSERT/UPDATE = t, DELETE = f; `franjas_agenda` DELETE = t; `registros_auditoria` UPDATE = f |
| T34 | `Guardar_AgendaConFranjas_SeLeeIgual` | 23,28 | integration | Médico propio; agenda con lunes 08–12 y lunes 20:00–00:00, enviada y rechazada con motivo; guardar con `CadenaApp`, leer con otro contexto (con `Include`) | Mismos `MedicoId`, duración, `VigenteDesde`, `Estado = Rechazada`, `MotivoRechazo`, `EnviadaEl`, `RechazadaEl` y las 2 franjas (la de medianoche con `Hasta = 00:00`); en SQL, `estado = 'Rechazada'` |
| T35 | `QuitarFranja_EnBorradorGuardado_BorraLaFila` | 24,28 | integration | Médico propio; borrador guardado con 2 franjas; otro contexto la carga, `QuitarFranja`, guarda con `CadenaApp` | `SELECT count(*) FROM franjas_agenda WHERE agenda_medico_id = <id>` = 1 |
| T36 | `Base_DosEnviadasDelMismoMedico_ElIndiceRechazaLaSegunda` | 25 | integration | Médico propio M1: agenda 1 `Enviada` guardada; agenda 2 `Enviada` → guardar; médico M2 con su `Enviada`; después agenda 1 → `Rechazada`, guardar, y otra vez agenda 2 | Primer intento de la 2: `DbUpdateException` con `PostgresException` `SqlState = 23505` y `ConstraintName = ux_agendas_medico_una_enviada`; M2 se guarda; tras rechazar la 1, la 2 se guarda |
| T37 | `Base_ValoresFueraDeRango_LosRechazanLosControles` | 26 | integration | `[Theory]` `INSERT` a mano con `CadenaApp`: agenda con duración 5; franja con `dia = 7`; franja 12:00–08:00 | `PostgresException` `23514` con `ConstraintName` `ck_agendas_medico_duracion` / `ck_franjas_agenda_dia` / `ck_franjas_agenda_horas` |
| T38 | `Guardar_DosCambiosSobreLaMismaAgenda_ElSegundoFallaPorConcurrencia` | 27 | integration | Médico propio; `Enviada` guardada; contextos 1 y 2 la cargan; 1 `Aprobar` + guardar; 2 `Rechazar` + guardar | El segundo lanza `DbUpdateConcurrencyException`; en la base `estado = 'Aprobada'` y `motivo_rechazo` nulo |
| T39 | `AgendaQueRigeAsync_SegunFecha_DevuelveLaQueRigeConSusFranjas` | 29,20,21 | integration | Médico propio con A, B, C y D del T29 guardadas; `[Theory]` 02/11, 15/11, 16/11 | A, A, B (por `Id`), con sus franjas cargadas |
| T40 | `AgendaQueRigeAsync_SinAgendaQueRija_DevuelveNull` | 29 | integration | `[Theory]`: médico propio sin agendas; el del T39 el 01/11; médico propio con sólo una `Enviada`; un id que no existe | `null` en los cuatro |
| T41 | `EstadosDeAgendaAsync_VariosMedicos_DevuelveUnaSituacionPorCadaId` | 29,22 | integration | `new ConsultaAgendasEnBase(contexto, reloj)` con el reloj en 03/11/2026; 4 médicos propios: sin agendas; con una aprobada desde 02/11; con una aprobada desde 02/11 y una enviada; con sólo una aprobada desde 16/11. Más un id que no existe. Comandos SQL de EF capturados en una `List<string>` con `LogTo` | 5 entradas: SinAgenda, Vigente, PropuestaPendiente, SinAgenda con `RigeDesde` 16/11/2026, SinAgenda; **un** solo `SELECT` sobre `agendas_medico` en la lista |
| T42 | `AgregarInfraestructura_ResuelveLaConsultaYLaGestionDeMedicos` | 29,30 | integration | `ServiceProvider` con `AgregarInfraestructura` y `AddIdentidadPersonal` (como T1 de la 2.1: `IGestionMedicos` también necesita `IActorVigente` de la 2.2); un scope | `IConsultaAgendas` se resuelve como `ConsultaAgendasEnBase`, e `IGestionMedicos` (que ahora la recibe) también se resuelve |
| T43 | `ListarYObtener_MedicosConAgendas_MuestranElEstadoReal` | 30 | integration | `IGestionMedicos` resuelto del contenedor (como en las pruebas de la 3.2), con un `FakeTimeProvider` en 03/11/2026 y los comandos SQL capturados con `ConfigureDbContext` + `LogTo`; 4 médicos propios con apellido `Agenda<marca>`: sin agendas, con una aprobada desde 02/11, con una aprobada y una enviada, con sólo una aprobada desde 16/11; `ListarAsync("Agenda<marca>")` y `ObtenerAsync(id)` de cada uno | Listado y fichas: `SinAgenda`, `Vigente`, `PropuestaPendiente`, `SinAgenda` con `RigeDesde` 16/11/2026; el listado hace **un** solo `SELECT` sobre `agendas_medico` |
| T44 | `GetMedicos_AgendasReales_MuestranVigentePendienteYSinAgendaConFecha` | 22,30 | integration | `PersonalWebApplicationFactory` (2.1) e ingreso de `admin1`; 3 médicos propios `Prueba<marca>, A/B/C` con `DatosDeCatalogo` de Web.Tests (3.2); agendas guardadas desde `app.Services`: A con una aprobada que rige hoy, B con una enviada, C con una aprobada que empieza dentro de 30 días (`VigenteDesde` = hoy del hospital + 30, con `app.Reloj`); GET `/personal/admin/medicos?buscar=<marca>` | `200`; la fila de A dice `Vigente`; la de B, `Propuesta pendiente`; la de C, `Sin agenda (rige desde dd/MM)` con la fecha de su agenda (decisión 8: la aprobada que todavía no rige se ve con su fecha) |

Todas llevan `[Trait("Story", "4.1")]` y un `[Trait("AC", "n")]` por AC de su columna.
T1–T32 van en `HospitalTurnos.Dominio.Tests` (unitarias puras: sin base, sin red, con
`FakeTimeProvider`; T32 prueba `EstadoDeAgenda`, que ahora vive en el dominio). T33–T43, en
`HospitalTurnos.Aplicacion.Tests` contra PostgreSQL real. T44, en `HospitalTurnos.Web.Tests` contra
la aplicación y PostgreSQL reales.

> El estimado del plan era 14 pruebas; quedan 44 porque la story junta cuatro cosas que cada una
> tiene sus bordes: franjas (medianoche, superposición), estados (15 transiciones prohibidas, la
> fecha local), horarios (múltiplo, medianoche, días sin franja) y persistencia con sus dos
> defensas en la base, más el estado real en el listado de la 3.2 (T44 lo prueba en la pantalla,
> por la decisión 8). 32 de las 44 son unitarias y corren en milisegundos.

**Por qué no hay E2E (regla 6.1 del formato).** Esta story no tiene pantallas propias: no toca
ningún componente. Lo único que cambia en una pantalla es el valor del estado en A1 (3.2, SSR sin
JavaScript), y T44 lo verifica con `HttpClient` sobre la aplicación real. El proyecto E2E lo crea la
10.1.

#### Given/Fixture

- **Reloj** (`Ayudas/RelojDePrueba.cs` en Dominio.Tests): `RelojDePrueba.En(int anio, int mes, int dia, int hora, int minuto)`
  arma un `FakeTimeProvider` con `SetLocalTimeZone(America/Argentina/Cordoba)` en esa hora **local**
  (convierte a UTC restando 3 horas). T12 lo arma directo en UTC (`2026-10-17T02:30Z`) para que se
  vea la diferencia. Ninguna prueba espera tiempo real (retrospectiva, acción A-4).
- **`AgendaBuilder`** (`Ayudas/AgendaBuilder.cs`), con la forma del `VentaBuilder` del proyecto guía:
  `AgendaBuilder.UnaAgenda().DelMedico(id).DeMinutos(20).Desde(new DateOnly(2026, 11, 2)).ConFranja(DayOfWeek.Monday, "08:00", "12:00")`
  y los terminales `.Borrador()`, `.Enviada(reloj)`, `.Rechazada(reloj, motivo)` y
  `.Aprobada(reloj)`. **Llega a cada estado sólo con los métodos públicos** (`Enviar`, `Aprobar`,
  ...), nunca escribiendo `Estado` por reflexión: así el builder no puede armar un estado que el
  dominio no permite. El caso extra de T21 (estado primero, AC 13) se arma con un reloj del 10/10
  para enviar y rechazar una agenda desde el 20/10, y se intenta reenviarla con el reloj del 16/10:
  la fecha ya no cumple los 7 días, pero lo que tiene que salir es el 409 del estado. El reloj por
  defecto del builder es 16/10/2026 12:00 Córdoba y la `VigenteDesde` por defecto, 02/11/2026.
- **Fechas de los ejemplos** (verificadas con `date`): viernes 16/10/2026 (hoy en las pruebas),
  domingo 01/11, lunes 02/11, lunes 09/11, martes 10/11, domingo 15/11, lunes 16/11, lunes 30/11.
- **Base de datos — la `PostgresFixture` de Aplicacion.Tests** (`tests/HospitalTurnos.Aplicacion.Tests/PostgresFixture.cs`,
  colección `"Postgres"`; ver "Que existe hoy": está en la raíz del proyecto, no en
  `Infraestructura/` como dice el contrato). Esta story no la modifica. Las pruebas guardan y leen
  con `CadenaApp` (`turnos_app`), así que prueban los permisos reales.
- **Datos propios por prueba** (base compartida): cada prueba crea **su** médico con
  `DatosDeCatalogo.CrearMedicoAsync(...)` de la 3.2 (`tests/HospitalTurnos.Aplicacion.Tests/Catalogo/DatosDeCatalogo.cs`;
  no se escribe otra ayuda para lo mismo) y filtra por él: no hace falta limpiar tablas, y no se
  toca `registros_auditoria`. `DatosDeAgenda` (método de extensión sobre `PostgresFixture`, como
  `LimpiezaDeBandeja` de la 1.4) sólo arma y guarda agendas: `GuardarAgendaAsync(medicoId, estado, vigenteDesde, reloj, franjas)`
  llega a cada estado con los métodos públicos (`Enviar`, `Aprobar`, `Rechazar`), igual que el
  `AgendaBuilder`.
- **T32**: va en Dominio.Tests (`EstadoDeAgenda` vive en el dominio) y arma sus agendas con
  `AgendaBuilder`, sólo con métodos públicos.
- **Contextos**: `ContextoDePrueba.Crear(fixture.CadenaApp)` (ayuda de la 1.3). T38 usa dos
  contextos a la vez. T37 usa `NpgsqlCommand` directo (el dominio no dejaría armar esos datos).
- **T41 — contar consultas** (M-2 de la revisión): `RegistroEnMemoria<T>` es un `ILogger<T>` y no
  sirve para `LogTo`, que pide un `Action<string>`. El contexto se arma con
  `new DbContextOptionsBuilder<HospitalDbContext>().UseNpgsql(fixture.CadenaApp).LogTo(comandos.Add, [RelationalEventId.CommandExecuted]).Options`,
  con `var comandos = new List<string>();`, y se cuentan las entradas que contienen
  `FROM agendas_medico`.
- **T42**: igual que T1 de la 2.1 (`IngresoPersonalIdentityTests`): configuración en memoria con
  `ConnectionStrings:App = CadenaApp`, `AgregarInfraestructura`, `AddIdentidadPersonal`,
  `BuildServiceProvider`.
- **T43**: el `ServiceProvider` de las pruebas de la 3.2 (`AgregarInfraestructura` +
  `AddIdentidadPersonal`), con `IGestionMedicos` **resuelto del contenedor** (nunca con `new`: el
  contrato del AC 23 de la 3.2), más `AddSingleton<TimeProvider>(reloj)` al final (lo último
  registrado gana) y
  `servicios.ConfigureDbContext<HospitalDbContext>(o => o.LogTo(comandos.Add, [RelationalEventId.CommandExecuted]))`
  para contar consultas como en T41. Los médicos se crean con `DatosDeCatalogo.CrearMedicoAsync`
  (3.2) y las agendas con `DatosDeAgenda`.
- **T44**: `PersonalWebApplicationFactory` de la 2.1 (sin cambios) con `[Collection("Postgres")]`;
  `admin1` ingresa con `FormularioHttp.IngresarAsync` y sólo navega. Médicos y especialidad con
  `DatosDeCatalogo` de Web.Tests (3.2). Las agendas se arman con los métodos públicos del dominio y
  se guardan con el `HospitalDbContext` de `app.Services.CreateScope()`. "Hoy" es la fecha del
  hospital según `app.Reloj` (arranca en la hora real). La agenda de A "rige hoy": se envía y aprueba
  con **otro** `FakeTimeProvider`, 30 días antes de `app.Reloj` (zona Córdoba), y una `VigenteDesde`
  = hoy − 20 (cumple los 7 días desde ese envío y no estaba pasada al aprobar); no se adelanta
  `app.Reloj` (cortaría la sesión). La de C se envía y aprueba con `app.Reloj`, con `VigenteDesde`
  = hoy + 30. El texto esperado se arma con `VigenteDesde.ToString("dd/MM", CultureInfo.InvariantCulture)`.

## Tasks / Subtasks

- [ ] [SEQ] Task 1: Salvaguarda de estado (AC: todos)
  - [ ] Confirmar que 2.2, 3.1 y 3.2 están en `main` tal como las describe el contrato único del
        AC 23 de la 3.2: `Medico` en `Dominio/Catalogo/` con `long Id`, tabla `medicos`, `DbSet`
        `Medicos`; `EstadoAgendaMedico { SinAgenda, Vigente, PropuestaPendiente }` y
        `SituacionDeAgenda(EstadoAgendaMedico Estado, DateOnly? RigeDesde = null)` en
        `src/HospitalTurnos.Dominio/Agendas/`; `GestionMedicosEnBase(contexto, auditoria, catalogo, actorVigente)`
        con la línea `Agenda = SituacionDeAgenda.SinAgenda` marcada para la 4.1 en `ListarAsync` y
        `ObtenerAsync`; las pruebas de la 3.2 resuelven `IGestionMedicos` del contenedor;
        `DatosDeCatalogo.CrearMedicoAsync` en Aplicacion.Tests y en Web.Tests; `IActorVigente` de la
        2.2. Anotar en el Debug Log lo que difiera; si algo no coincide → DETENERSE: **no** crear un
        segundo enum ni otra carpeta para "arreglarlo".
  - [ ] Confirmar que `AccionesAuditoria` ya tiene `PropuestaAgendaEnviada`, `PropuestaAgendaAprobada`,
        `PropuestaAgendaRechazada` y `AgendaCorregida` (AC 31), que `ContextoDePrueba` y la
        `PostgresFixture` de Aplicacion.Tests existen, y que
        `HospitalTurnos.Dominio.Tests` referencia `Microsoft.Extensions.TimeProvider.Testing`.
  - [ ] `bash scripts/ci/local.sh` en verde antes de empezar.
  - [ ] Si algo no coincide → DETENERSE y corregir la story primero.
- [ ] [SEQ] Task 2: Dominio, con las pruebas primero (AC: 1–21)
  - [ ] `RelojDePrueba`, `AgendaBuilder` y T1–T31, viéndolas fallar (no compilan).
  - [ ] `EstadoPropuesta` (`Borrador`, `Enviada`, `Aprobada`, `Rechazada`), con un comentario por
        valor (como `EstadoVenta` del proyecto guía). El enum de A1 **no** se crea: es el
        `EstadoAgendaMedico` de la 3.2, en la misma carpeta.
  - [ ] `Franja` (AC 1–3) y `AgendaMedico` (AC 4–21), con la forma de "Implementación concreta".
  - [ ] `EstadoDeAgenda.Calcular` (AC 22) en `Dominio/Agendas/`, con T32 en rojo primero.
  - [ ] T1–T32 en verde.
- [ ] [SEQ] Task 3: Aplicacion — el puerto (AC: 29)
  - [ ] `IConsultaAgendas` con las dos operaciones del AC 29 y un comentario de quién la usa.
- [ ] [SEQ] Task 4: Infraestructura — mapeo y migración (AC: 23–28)
  - [ ] `AgendaMedicoConfiguracion` y `FranjaConfiguracion` (ver "Implementación concreta").
  - [ ] `HospitalDbContext`: `DbSet<AgendaMedico> AgendasMedico` y las dos `ApplyConfiguration`
        **antes** de `ConvencionSnakeCase.Aplicar`.
  - [ ] `dotnet ef migrations add AgregarAgendas` (ver Como verificar) y, al final de su `Up`, el
        `migrationBuilder.Sql(...)` de permisos. Revisar el código generado: nombres en
        `snake_case`, filtro del índice, los tres `CHECK`, `xmin` **sin** columna nueva.
  - [ ] Correr la Salvaguarda de BD (abajo).
- [ ] [SEQ] Task 5: Infraestructura — la consulta y el estado real en A1 (AC: 29, 30)
  - [ ] `ConsultaAgendasEnBase` (recibe `HospitalDbContext` y `TimeProvider`) y su registro `Scoped`
        en `AgregarInfraestructura`.
  - [ ] `GestionMedicosEnBase`: quinto parámetro `IConsultaAgendas` (al final) y el reemplazo del
        AC 30. Las pruebas de la 3.2 no se tocan (resuelven del contenedor).
- [ ] [SEQ] Task 6: Pruebas de integración (AC: 22–30, 32)
  - [ ] `DatosDeAgenda` y T33–T43 en verde.
  - [ ] T44 (Web.Tests) en verde: el estado real llega a la pantalla A1.
- [ ] [SEQ] Task 7: Cierre (AC: 32)
  - [ ] Suite completa (`dotnet test`) y `bash scripts/ci/local.sh` en verde; anotar la cobertura de
        líneas de Dominio y Aplicacion en el Dev Agent Record.

### Task Dependencies

T1 → T2 → T3 → T4 → T5 → T6 → T7 (secuencial). T2 no necesita Docker: se puede hacer entera
antes de tocar la base.

## Dev Notes

### Que existe hoy

#### En hospital-turnos (verificado 2026-10-16 contra `main`, commit `c3413cc`, con 1.1 a 2.1 en done)

- **No existe nada de agendas**: no hay carpeta `src/HospitalTurnos.Dominio/Agendas/`, ni tablas, ni
  puertos. `src/HospitalTurnos.Dominio/` tiene hoy `Auditoria/`, `Excepciones/` y `Personal/`.
- **Todavía no existen `Medico` ni `Especialidad`**: los crean la 3.1 y la 3.2, que se implementan
  antes (orden del sprint: 2.2 → 3.1 → 3.2 → 4.1). Sus stories ya están escritas
  (`docs/stories/3-1-especialidades.md`, `docs/stories/3-2-medicos.md`) y la 3.2 fija en su AC 23
  el **contrato único** con esta: `Medico` en `Dominio/Catalogo/` con `long Id` y tabla `medicos`;
  `EstadoAgendaMedico` y `SituacionDeAgenda` en `Dominio/Agendas/`; `GestionMedicosEnBase` con
  cuatro parámetros y la línea `Agenda = SituacionDeAgenda.SinAgenda` marcada para la 4.1; la regla
  del estado (decisión 8) la escribe esta story en `EstadoDeAgenda.Calcular`. La Task 1 confirma que
  el código quedó así.
- `HospitalDbContext` (`Infraestructura/Persistencia/`): hereda de
  `IdentityDbContext<UsuarioPersonal, IdentityRole<Guid>, Guid>`; cada configuración se aplica **a
  mano** con `modelBuilder.ApplyConfiguration(new ...())` (no hay `ApplyConfigurationsFromAssembly`)
  y **antes** de `ConvencionSnakeCase.Aplicar(modelBuilder)`, que va al final. Los nombres de tabla
  salen del nombre del `DbSet` pasado a `snake_case` (`RegistrosAuditoria` → `registros_auditoria`).
  La convención renombra tablas, columnas, claves, FK e índices; **no** renombra los `CHECK`, por eso
  sus nombres se escriben ya en `snake_case`.
- Configuraciones existentes en `Persistencia/Configuraciones/`: `RegistroAuditoriaConfiguracion`
  (enum guardado como texto con `HasConversion<string>().HasMaxLength(20)`) y
  `MensajeSalienteConfiguracion` (índice parcial con `HasFilter("enviado_el IS NULL AND ...")`
  escrito con los nombres ya en `snake_case`). Ids propios: `long` identity.
- Migraciones en `Persistencia/Migraciones/`, nombre `<marca>_Nombre` (la última,
  `20261016180914_IdentidadPersonal`). `20261016020303_AgregarBandejaDeSalida` termina con
  `REVOKE ALL ON TABLE ... FROM turnos_app; GRANT SELECT, INSERT, UPDATE ...` y el `GRANT USAGE` de
  la secuencia: es el modelo de permisos que copia esta story.
- `docker/postgres/initdb/01-roles.sh` da por defecto `SELECT, INSERT, UPDATE, DELETE` a toda tabla
  nueva del dueño, y `USAGE, SELECT` a sus secuencias.
- Excepciones del dominio (`Dominio/Excepciones/`): `ReglaDeNegocioException` (400),
  `ConflictoException : ReglaDeNegocioException` (409), `NoEncontradoException` (404).
- Ids propios: `long` identity (`RegistroAuditoria`, `MensajeSaliente`; y `Especialidad`, `Medico` de
  la 3.1/3.2). `AgendaMedico.MedicoId` es `long` (decisión 15).
- `AccionesAuditoria` ya trae, desde la 1.3, `PropuestaAgendaEnviada = "propuesta-agenda.enviada"`,
  `PropuestaAgendaAprobada`, `PropuestaAgendaRechazada` y `AgendaCorregida`.
- Hora: `RelojHospital : TimeProvider` (Córdoba) registrado singleton en `AgregarInfraestructura`;
  `TimeProviderExtensions.AhoraLocal()` vive en **Aplicacion**, así que el dominio no la puede usar
  (Dominio no referencia a nadie): calcula "hoy" con `reloj.GetLocalNow()`.
- `HoraDelSistemaTests` (Dominio.Tests, 1.2) recorre todo `src/` y falla si aparece
  `DateTime.Now`, `UtcNow`, `Today` o `DateTimeOffset.Now/UtcNow` fuera de `RelojHospital`.
- `HospitalTurnos.Dominio.csproj` no tiene paquetes; `TimeProvider` es parte de .NET (no es un
  paquete), así que el dominio puede recibirlo.
- Pruebas: `PostgresFixture` de Aplicacion.Tests en `tests/HospitalTurnos.Aplicacion.Tests/PostgresFixture.cs`
  (el contrato dice `Infraestructura/PostgresFixture.cs`; vale el código). Ayudas existentes:
  `Ayudas/ContextoDePrueba.cs`, `Ayudas/RegistroEnMemoria.cs`, `Email/LimpiezaDeBandeja.cs`
  (extensión sobre la fixture). Dominio.Tests ya referencia `Microsoft.Extensions.TimeProvider.Testing`.
- **Código descartable a reaprovechar** (`git show 48f29a1:src/HospitalTurnos.Api/Dominio/Medico.cs`
  y `.../HorarioAtencion.cs`): `HorariosDelDia` con `for (var t = inicio; t + duracion <= fin; t += duracion)`
  (la idea de "un turno que no entra completo no se ofrece" se conserva), `SeSuperponeCon` con
  `Desde < otro.Hasta && otro.Desde < Hasta` (se conserva, en minutos para la medianoche) y
  `AtiendeEn`. **No** se conservan: la duración de 5 a 240 (R-02 dice 10 a 60), las franjas colgadas
  del médico (ahora son de cada agenda, ADR-06), `hasta <= desde` como única validación (rechazaba
  terminar a medianoche) y `AgendaService` (mezclaba disponibilidad y reserva: eso es de 5.2 y 5.3).
- `docs/brainstorm-capacidad-agenda.md`, decisión del docente (2026-10-09): **sin tope** de turnos
  por día; salen de horas de atención ÷ duración. Esta story no agrega topes ni cupos.

### Patron de referencia verificado

**Patrón 1 — agregado con estado y colección privada** (proyecto guía de la materia):

| Proyecto | Ancla `archivo:línea` | Qué se verificó exactamente |
|---|---|---|
| changomas-testing-csharp | `src/ChangoMas.Dominio/Venta.cs:15` y `:28` | `private readonly List<ItemVenta> _items = [];` y `public IReadOnlyList<ItemVenta> Items => _items;`: nadie cambia la colección sin pasar por los métodos. `AgendaMedico` hace lo mismo con `_franjas` / `Franjas`. |
| changomas-testing-csharp | `src/ChangoMas.Dominio/Venta.cs:35` y `:76-81` | Cada método que modifica empieza con `VerificarQueSePuedaModificar()`, que lanza si el estado no lo permite. Acá, `AsegurarEditable()`, que además pasa una `Rechazada` a `Borrador` (AC 12). Diferencia: el guía lanza `InvalidOperationException`; acá `ConflictoException` del contrato (→ 409). |
| changomas-testing-csharp | `src/ChangoMas.Dominio/EstadoVenta.cs:7-20` | Enum con un comentario por estado que explica qué se puede hacer en él ("el diagrama de estados ... ahora ejecutable"). Se toma para `EstadoPropuesta`. |
| changomas-testing-csharp | `tests/ChangoMas.Tests.Unit/Ayudas/VentaBuilder.cs:16` y `:22` | `public class VentaBuilder` con `public static VentaBuilder UnaVenta() => new();`. Se toma para `AgendaBuilder.UnaAgenda()`. |

**Patrón 2 — mapeo, migración y permisos** (este mismo repo, ya en `main`):

| Proyecto | Ancla | Qué se verificó exactamente |
|---|---|---|
| hospital-turnos | `src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/RegistroAuditoriaConfiguracion.cs` | Enum como texto (`HasConversion<string>().HasMaxLength(20)`), sin `ToTable`. Se toma para `Estado`. |
| hospital-turnos | `src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/MensajeSalienteConfiguracion.cs` | `HasIndex(...).HasFilter("...")` con el filtro en `snake_case`. Se toma para `ux_agendas_medico_una_enviada`. |
| hospital-turnos | `src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/20261016020303_AgregarBandejaDeSalida.cs` | Al final de `Up`, `migrationBuilder.Sql("""REVOKE ALL ...; GRANT SELECT, INSERT, UPDATE ...; DO $$ ... GRANT USAGE ON SEQUENCE ... $$;""")`. Se copia para `agendas_medico`. |
| hospital-turnos | `tests/HospitalTurnos.Aplicacion.Tests/Email/LimpiezaDeBandeja.cs` | Ayuda propia de una story como método de extensión sobre `PostgresFixture` (contrato §Pruebas). Se toma para `DatosDeAgenda.GuardarAgendaAsync`. |

**Patrón 3 — concurrencia optimista con `xmin`: no está en el guía ni en el repo (opción B).**
`grep -rn -E "IsRowVersion|IsConcurrencyToken|xmin" src/ ~/SourceCode/changomas-testing-csharp/src`
→ 0 resultados (el guía usa SQLite; acá Identity trae su propio `ConcurrencyStamp`, que no sirve
para otras tablas). La referencia es la documentación de Npgsql para EF Core 10 (paquete
`Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3, fijado en `Directory.Packages.props`): una
propiedad `uint` con `IsRowVersion()` se mapea a la columna de sistema `xmin`, que PostgreSQL cambia
en cada `UPDATE`. La migración no crea ninguna columna. T38 prueba que funciona.

### Que hay que hacer (la delta)

1. Dominio: `Franja`, `AgendaMedico` con su máquina de estados, `HorariosDelDia`, `AtiendeEn`,
   `TurnosPorSemana` y `QueRigeEn`; el enum `EstadoPropuesta`.
2. Persistencia: dos tablas, una migración con permisos, un índice único parcial, tres controles y
   concurrencia optimista.
3. Dominio y Aplicacion: `EstadoDeAgenda.Calcular` (dominio) y un puerto de consulta (Aplicacion),
   con su implementación registrada en el contenedor.
4. El estado real de agenda en el listado y la ficha de la 3.2 (sólo el cálculo, no la pantalla),
   probado hasta la pantalla (T44).

**Diferencias clave con el código descartable y con Arq. §6:**
- Las franjas son de cada **agenda**, no del médico: así conviven la vigente, la propuesta y la
  historia (ADR-06).
- `HorariosDelDia` y `AtiendeEn` quedan en `AgendaMedico`, no en `Medico` (decisión 14).
- La medianoche se modela como `Hasta = 00:00` y se calcula en minutos (fin = 1440).

### Decisiones de esta story (con su porqué)

- **El dominio recibe el reloj (`TimeProvider`) como parámetro** en `Enviar`, `Aprobar` y
  `Rechazar`, y no un `DateOnly hoy`. Porque la regla de los 7 días depende de cuál es "hoy" en el
  hospital, y un error común es usar la fecha UTC: a las 23:30 de Córdoba ya es mañana en UTC. Si
  el cálculo vive en el dominio, se prueba una vez (T12) y ningún caso de uso lo puede hacer mal.
  El `TimeProvider` es parte de .NET, así que el dominio sigue sin depender de nadie.
- **`HorariosDelDia` no mira la vigencia.** Una sola responsabilidad por método: `QueRigeEn` elige
  la agenda, `HorariosDelDia` expande sus franjas. Así M2 (4.2) puede mostrar los horarios de un
  borrador que todavía no rige, y la 5.2 compone los dos.
- **`QueRigeEn` vive en el dominio y la consulta de la base lo usa.** La base trae las aprobadas
  del médico (son pocas: unas por año) y el dominio elige. Si la elección estuviera escrita también
  en SQL, habría dos versiones de la misma regla que podrían separarse.
- **Desempate por `AprobadaEl`** (AC 20). Caso real: el administrador aprueba una agenda desde el
  02/11 y, antes de que empiece, el médico la corrige con otra propuesta también desde el 02/11.
  Tiene que regir la segunda. Prohibir dos aprobadas con la misma fecha (con un índice único)
  impediría esa corrección.
- **Una franja no cruza la medianoche.** `Franja` tiene un solo día; una franja de 22:00 a 02:00
  pertenecería a dos días y complicaría `HorariosDelDia` y la superposición. Se carga como dos
  franjas. El hospital no tiene hoy agendas nocturnas en el PRD; si aparecen, sigue funcionando.
- **El estado de A1 sigue la decisión 8 del docente**: `PropuestaPendiente` gana (es lo que pide
  una acción del administrador) y una aprobada que todavía no rige da `SinAgenda` (RU-07), pero con
  su fecha (`Sin agenda (rige desde 01/12)`), para que la administradora no vea "Sin agenda" semanas
  después de aprobar sin saber por qué.
- **`EstadoDeAgenda` en el dominio** (`Dominio/Agendas/`, junto a `EstadoAgendaMedico` de la 3.2).
  Antes vivía en Aplicacion porque el enum estaba ahí; con el contrato único (C-1) los dos están en
  el dominio. Es una función pura sobre `QueRigeEn`: la regla de vigencia sigue estando una sola vez,
  y se prueba sin base (T32).
- **`GestionMedicosEnBase` recibe la consulta por constructor, como quinto parámetro** (AC 30). La 3.2
  dejó marcada la línea que cambia la 4.1; llamar a `EstadosDeAgendaAsync` una vez con todos los ids
  evita una consulta por fila del listado.
- **`AgendaMedico.MedicoId` es `long`** (decisión 15), como `Medico.Id` y toda entidad propia.
- **La rechazada conserva su motivo mientras se corrige.** UX M2 dibuja la propuesta rechazada con
  su motivo arriba de los campos editables. El motivo se borra recién al reenviar; la historia del
  rechazo queda en la auditoría de la 4.3.
- **Estado primero, datos después** (AC 13). Un 409 dice "esto ya no se puede hacer"; un 400 dice
  "corregí este dato". Si una agenda aprobada recibiera "agregá una franja", el médico corregiría
  algo que igual no se puede enviar.
- **Las agendas no se borran** (AC 24). ADR-06 quiere la historia completa de qué agenda regía en
  cada fecha; sin permiso de `DELETE`, ni un error de código la pierde. Las franjas sí se borran,
  pero sólo de un borrador (lo controla el dominio).
- **Concurrencia en esta story y no en la 4.3.** El mapeo de la tabla es de acá; agregar `xmin`
  después obligaría a reabrir la configuración. Cuesta una línea.

### Decisiones tomadas por el docente (2026-10-16)

Numeradas como en `revision-sprint-2.md` ("Decisiones abiertas consolidadas").

- **Decisión 11 — ¿Se puede aprobar una propuesta cuya fecha de inicio ya pasó?** (era DA-1; AC 11,
  T16, T17). **Elegido: no** (409, "rechazala para que el médico elija otra fecha"). Aprobarla haría
  regir una agenda desde una fecha pasada, con turnos ya dados con la anterior. Matiz del docente:
  la 4.3 lo muestra **antes** del botón `Aprobar`, no sólo al fallar (contrato, AC 31).
- **Decisión 12 — Franjas que no son múltiplo de la duración** (era DA-2; AC 15, T23). **Elegido:
  aceptarlas** y no ofrecer el sobrante (como el código descartable y el "horas ÷ duración" del
  brainstorm). La 4.2 puede avisarle al médico "los últimos 10 minutos de la franja no se usan".
- **Decisión 13 — Una aprobada nueva con fecha anterior a otra aprobada que todavía no empezó** (era
  DA-3; AC 20). **Elegido: la regla de Arq. §6 tal cual** (B rige del 15/11 al 30/11 y A vuelve a
  regir desde el 01/12), y la 4.3 avisa al aprobar B (contrato, AC 31).
- **Decisión 14 — `HorariosDelDia` y `AtiendeEn` en `AgendaMedico` y no en `Medico`** (era DA-4).
  **Elegido: en `AgendaMedico`**. Con versiones por fecha (ADR-06) el médico tiene varias agendas, y
  "¿qué horarios hay el 09/11?" necesita primero elegir cuál rige. El diagrama de Arq. §6 se
  actualiza al cerrar el sprint (lo hace Winston; esta story no toca otros archivos).
- **Decisión 8 — Estado de A1 y aprobada que todavía no rige.** **Elegido:** esta story conecta el
  estado real en el mismo sprint (AC 30); una aprobada que todavía no rige se ve
  `Sin agenda (rige desde dd/MM)` (AC 22, T32, T41, T43 y la prueba nueva T44).
- **Decisión 15 — Tipo de `Medico.Id`.** **Elegido:** `long`; acá, `AgendaMedico.MedicoId` y
  `medico_id bigint` (AC 23, 29).

### Implementacion concreta

**`Franja`** — la medianoche en minutos:

```csharp
public sealed class Franja
{
    private const int MinutosDelDia = 24 * 60;

    public long Id { get; private set; }                  // para EF; el dominio no lo usa
    public DayOfWeek Dia { get; private set; }
    public TimeOnly Desde { get; private set; }
    public TimeOnly Hasta { get; private set; }           // 00:00 = medianoche, al final del día

    private Franja() { }                                  // para EF

    public Franja(DayOfWeek dia, TimeOnly desde, TimeOnly hasta)
    {
        // TimeOnly no puede valer 24:00: por eso "hasta 00:00" se lee como el fin del día.
        if (desde.Second != 0 || desde.Millisecond != 0 || hasta.Second != 0 || hasta.Millisecond != 0
            || desde.Ticks % TimeSpan.TicksPerMinute != 0 || hasta.Ticks % TimeSpan.TicksPerMinute != 0)
            throw new ReglaDeNegocioException("Las horas de la franja van en horas y minutos, sin segundos.");
        if (FinEnMinutos(hasta) <= InicioEnMinutos(desde))
            throw new ReglaDeNegocioException("La franja tiene que terminar después de empezar.");
        (Dia, Desde, Hasta) = (dia, desde, hasta);
    }

    public bool TerminaAMedianoche => Hasta == TimeOnly.MinValue;
    public int DuracionMinutos => FinEnMinutos(Hasta) - InicioEnMinutos(Desde);
    internal int Inicio => InicioEnMinutos(Desde);
    internal int Fin => FinEnMinutos(Hasta);

    public bool SeSuperponeCon(Franja otra) =>
        Dia == otra.Dia && Inicio < otra.Fin && otra.Inicio < Fin;   // tocarse no es pisarse

    private static int InicioEnMinutos(TimeOnly t) => t.Hour * 60 + t.Minute;
    private static int FinEnMinutos(TimeOnly t) => t == TimeOnly.MinValue ? MinutosDelDia : InicioEnMinutos(t);
}
```

Los nombres de los días para los mensajes salen de un arreglo propio
(`["domingo", "lunes", ..., "sábado"]` indexado por `(int)dia`), no de `CultureInfo("es-AR")`: la
imagen de Docker puede no traer los datos de cultura y el mensaje cambiaría según la máquina.

**`AgendaMedico`** — esqueleto de la máquina de estados:

```csharp
public sealed class AgendaMedico
{
    public const int DuracionMinima = 10, DuracionMaxima = 60;   // R-02
    public const int DiasMinimosDeAnticipacion = 7;              // S-31
    public const int LargoMaximoMotivo = 500;

    private readonly List<Franja> _franjas = [];
    public long Id { get; private set; }
    public long MedicoId { get; private set; }                   // tipo de Medico.Id (3.2): long
    public int DuracionTurnoMinutos { get; private set; }
    public DateOnly VigenteDesde { get; private set; }
    public EstadoPropuesta Estado { get; private set; }
    public string? MotivoRechazo { get; private set; }
    public DateTimeOffset? EnviadaEl { get; private set; }
    public DateTimeOffset? AprobadaEl { get; private set; }
    public DateTimeOffset? RechazadaEl { get; private set; }
    public IReadOnlyList<Franja> Franjas => _franjas;

    public void AgregarFranja(Franja franja)
    {
        AsegurarEditable();                                   // 1. estado (409)
        var pisada = _franjas.FirstOrDefault(f => f.SeSuperponeCon(franja));
        if (pisada is not null) throw new ReglaDeNegocioException(/* AC 4 */);   // 2. datos (400)
        _franjas.Add(franja);
        CorregirSiEstabaRechazada();                          // 3. recién con el cambio hecho
    }

    public void Enviar(TimeProvider reloj)
    {
        if (Estado != EstadoPropuesta.Borrador) throw new ConflictoException(/* tabla del AC 13 */);
        if (_franjas.Count == 0) throw new ReglaDeNegocioException("Agregá al menos una franja antes de enviar la propuesta.");
        var minima = Hoy(reloj).AddDays(DiasMinimosDeAnticipacion);
        if (VigenteDesde < minima) throw new ReglaDeNegocioException(/* AC 9b con minima */);
        Estado = EstadoPropuesta.Enviada;
        EnviadaEl = reloj.GetUtcNow();
        MotivoRechazo = null;
    }

    // La fecha del HOSPITAL, no la UTC: RelojHospital (y el FakeTimeProvider de las pruebas) traen Córdoba.
    private static DateOnly Hoy(TimeProvider reloj) => DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);

    public IReadOnlyList<DateTime> HorariosDelDia(DateOnly fecha)
    {
        var horarios = new List<DateTime>();
        foreach (var franja in _franjas.Where(f => f.Dia == fecha.DayOfWeek).OrderBy(f => f.Inicio))
            for (var inicio = franja.Inicio; inicio + DuracionTurnoMinutos <= franja.Fin; inicio += DuracionTurnoMinutos)
                horarios.Add(fecha.ToDateTime(TimeOnly.MinValue).AddMinutes(inicio));   // Kind = Unspecified
        return horarios;
    }

    public static AgendaMedico? QueRigeEn(IEnumerable<AgendaMedico> agendas, DateOnly fecha) =>
        agendas.Where(a => a.Estado == EstadoPropuesta.Aprobada && a.VigenteDesde <= fecha)
               .OrderByDescending(a => a.VigenteDesde)
               .ThenByDescending(a => a.AprobadaEl)
               .FirstOrDefault();
}
```

`AsegurarEditable()` lanza el 409 de la tabla del AC 13 si está `Enviada` o `Aprobada`.
`CorregirSiEstabaRechazada()` pasa `Rechazada` → `Borrador` **después** de aplicar un cambio válido,
para que una edición que falla deje la agenda intacta (AC 12, T19). Cada método de edición sigue el
mismo orden: estado → validar → cambiar → corregir.

**Mapeo (`AgendaMedicoConfiguracion`)**:

```csharp
agenda.HasKey(a => a.Id);                                                   // long → identity
agenda.Property(a => a.Estado).HasConversion<string>().HasMaxLength(20);    // como Canal (1.3)
agenda.Property(a => a.MotivoRechazo).HasMaxLength(AgendaMedico.LargoMaximoMotivo);
agenda.Property<uint>("Version").IsRowVersion();                            // xmin, AC 27
agenda.HasOne<Medico>().WithMany().HasForeignKey(a => a.MedicoId).OnDelete(DeleteBehavior.Restrict);
agenda.HasMany(a => a.Franjas).WithOne().HasForeignKey("AgendaMedicoId")
      .IsRequired()                                                         // NOT NULL: sin franjas huérfanas (M-1)
      .OnDelete(DeleteBehavior.Cascade);
agenda.Navigation(a => a.Franjas).UsePropertyAccessMode(PropertyAccessMode.Field);   // usa _franjas
agenda.HasIndex(a => a.MedicoId).IsUnique().HasFilter("estado = 'Enviada'")
      .HasDatabaseName("ux_agendas_medico_una_enviada");                    // AC 25
agenda.HasIndex(a => new { a.MedicoId, a.Estado, a.VigenteDesde });       // la consulta del AC 29
agenda.ToTable(t => t.HasCheckConstraint("ck_agendas_medico_duracion", "duracion_turno_minutos BETWEEN 10 AND 60"));
```

`FranjaConfiguracion`: `ToTable("franjas_agenda", t => { t.HasCheckConstraint("ck_franjas_agenda_dia", "dia BETWEEN 0 AND 6"); t.HasCheckConstraint("ck_franjas_agenda_horas", "hasta > desde OR hasta = '00:00'"); })`.
`DayOfWeek` se guarda como `integer` (el valor por defecto de EF para un enum); `DateOnly` y
`TimeOnly` los mapea Npgsql a `date` y `time without time zone` sin configuración. La FK sombra
`AgendaMedicoId` sale `agenda_medico_id` por la convención; **sin** `IsRequired()` EF la haría
anulable (es una propiedad sombra de tipo `long?`), por eso la relación lo pide (AC 23, M-1). Quitar
una franja de `_franjas` la borra al guardar (relación requerida: EF borra la huérfana).

**Permisos, al final del `Up` de la migración**:

```csharp
// Las agendas se versionan (ADR-06): la app crea y cambia de estado, pero nunca borra. Las franjas sí:
// quitar una franja de un borrador es un DELETE. 01-roles.sh da DELETE por defecto: por eso REVOKE + GRANT.
migrationBuilder.Sql("""
    REVOKE ALL ON TABLE agendas_medico FROM turnos_app;
    GRANT SELECT, INSERT, UPDATE ON TABLE agendas_medico TO turnos_app;
    DO $$ BEGIN
      EXECUTE format('GRANT USAGE ON SEQUENCE %s TO turnos_app',
                     pg_get_serial_sequence('agendas_medico', 'id'));
    END $$;
    """);
```

**`EstadoDeAgenda`** (`Dominio/Agendas/EstadoDeAgenda.cs`) — la regla de A1 sobre la regla de vigencia.
Devuelve la `SituacionDeAgenda` de la 3.2 (misma carpeta):

```csharp
public static class EstadoDeAgenda
{
    // Decisión 8 del docente: la pendiente pide una acción del administrador; una aprobada que todavía
    // no rige no ofrece turnos hoy (RU-07), pero se muestra con su fecha: "Sin agenda (rige desde 01/12)".
    public static SituacionDeAgenda Calcular(IEnumerable<AgendaMedico> agendasDelMedico, DateOnly hoy)
    {
        var agendas = agendasDelMedico.ToList();
        if (agendas.Any(a => a.Estado == EstadoPropuesta.Enviada))
            return new(EstadoAgendaMedico.PropuestaPendiente);
        if (AgendaMedico.QueRigeEn(agendas, hoy) is not null)
            return new(EstadoAgendaMedico.Vigente);
        var proxima = agendas.Where(a => a.Estado == EstadoPropuesta.Aprobada && a.VigenteDesde > hoy)
                             .Select(a => (DateOnly?)a.VigenteDesde).Min();
        return new(EstadoAgendaMedico.SinAgenda, proxima);   // proxima = null si no hay ninguna
    }
}
```

**`ConsultaAgendasEnBase(HospitalDbContext db, TimeProvider reloj) : IConsultaAgendas`**:

```csharp
public async Task<AgendaMedico?> AgendaQueRigeAsync(long medicoId, DateOnly fecha, CancellationToken ct)
{
    var candidatas = await db.AgendasMedico.AsNoTracking().Include(a => a.Franjas)
        .Where(a => a.MedicoId == medicoId && a.Estado == EstadoPropuesta.Aprobada && a.VigenteDesde <= fecha)
        .ToListAsync(ct);
    return AgendaMedico.QueRigeEn(candidatas, fecha);     // la regla, una sola vez, en el dominio
}

public async Task<IReadOnlyDictionary<long, SituacionDeAgenda>> EstadosDeAgendaAsync(
    IReadOnlyCollection<long> medicoIds, CancellationToken ct)
{
    // Una consulta para todo el listado; sin franjas: el estado no las necesita.
    var agendas = await db.AgendasMedico.AsNoTracking()
        .Where(a => medicoIds.Contains(a.MedicoId)
                    && (a.Estado == EstadoPropuesta.Aprobada || a.Estado == EstadoPropuesta.Enviada))
        .ToListAsync(ct);
    var porMedico = agendas.ToLookup(a => a.MedicoId);
    var hoy = DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);   // fecha del hospital
    return medicoIds.Distinct().ToDictionary(id => id, id => EstadoDeAgenda.Calcular(porMedico[id], hoy));
}
```

### Salvaguarda de BD (obligatoria antes de las pruebas de integración)

**No** usar `dotnet ef database update` (usaría `turnos_app`, sin `CREATE`). Las migraciones las
aplica el arranque con `ConnectionStrings:Migraciones`:

```bash
docker compose up -d --build     # la app aplica AgregarAgendas al arrancar
```

Después, contra el PostgreSQL de Compose (mismo `01-roles.sh` que la `PostgresFixture`):

1. `docker compose exec db psql -U postgres -d hospital_turnos -c "\d agendas_medico" -c "\d franjas_agenda"`
   → columnas y tipos del AC 23, los tres `CHECK`, el índice `ux_agendas_medico_una_enviada` con su
   `WHERE`, y **ninguna** columna `version` (el control usa `xmin`).
2. `SELECT has_table_privilege('turnos_app','agendas_medico','DELETE'), has_table_privilege('turnos_app','agendas_medico','UPDATE'), has_table_privilege('turnos_app','franjas_agenda','DELETE');`
   → `f | t | t`.
3. `SELECT has_table_privilege('turnos_app','registros_auditoria','UPDATE');` → `f` (AC 32).
4. Si CUALQUIER resultado no coincide → DETENERSE → corregir la story primero.

### Archivos a crear/modificar

| Acción | Archivo |
|---|---|
| Crear | Los de `files_to_create` del frontmatter |
| Modificar | `src/HospitalTurnos.Infraestructura/Persistencia/HospitalDbContext.cs` (`DbSet<AgendaMedico> AgendasMedico` y dos `ApplyConfiguration`, antes de la convención) |
| Modificar | `src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/HospitalDbContextModelSnapshot.cs` (lo regenera `dotnet ef`) |
| Modificar | `src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs` (`AddScoped<IConsultaAgendas, ConsultaAgendasEnBase>()`) |
| Modificar | `src/HospitalTurnos.Infraestructura/Catalogo/GestionMedicosEnBase.cs` (3.2: recibe `IConsultaAgendas` como quinto parámetro; estado real en `ListarAsync` y `ObtenerAsync`, AC 30) |

### Lo que NO hay que hacer

- **NO hacer pantallas ni casos de uso**: M2 es de la 4.2, A6 de la 4.3. En A1 sólo cambia el
  cálculo de `GestionMedicosEnBase` (AC 30): ni `TablaMedicos`, ni `EstadoAgendaMedico`, ni
  `SituacionDeAgenda`, ni las páginas.
- **NO crear otro enum de estado de agenda** (`EstadoAgenda` u otro nombre) ni la carpeta
  `Aplicacion/Catalogo` para él: el contrato único es el de la 3.2 (C-1 de la revisión). Las secciones "en construcción" de la 2.1 (`/personal/medico/mi-propuesta`,
  `/personal/admin/propuestas`) no se tocan.
- **NO auditar ni agregar constantes a `AccionesAuditoria`**: no hay ninguna acción de una persona.
  Las constantes de agenda ya existen.
- **NO modificar `Medico` ni `Especialidad`** (3.1, 3.2): la relación se configura desde
  `AgendaMedicoConfiguracion` con `HasOne<Medico>().WithMany()`, sin navegación del lado del médico.
- **NO guardar horarios** en ninguna tabla (ADR-04): se calculan.
- **NO filtrar bloqueos, turnos tomados ni la ventana de 60 días**: es de la 5.2.
- **NO crear la corrección de urgencia** (FR-14f): es de la 4.4.
- **NO agregar topes, cupos ni duraciones por especialidad**: el brainstorm se cerró "sin tope".
- **NO leer la hora con `DateTime.Now`/`UtcNow`/`Today`** ni calcular "hoy" con la fecha UTC
  (`GetUtcNow().Date`): rompe T12 y la prueba de la 1.2.
- **NO usar `DateTime` para `VigenteDesde`**: es una fecha sin hora (`DateOnly` → `date`).
- **NO armar estados por reflexión** en el `AgendaBuilder`: sólo con los métodos públicos.
- **NO dar `DELETE` sobre `agendas_medico`** ni borrar agendas desde el código.
- **NO usar `dotnet ef database update`**: falla con `turnos_app` (Salvaguarda de BD).
- **NO limpiar tablas en las pruebas** ni contar filas sin filtrar por el médico propio de la
  prueba (base compartida); nunca un limpiador genérico (chocaría con `registros_auditoria`).
- **NO repetir la regla de `QueRigeEn` en SQL** (ver "Decisiones de esta story").

### Como verificar que funciona

```bash
cd ~/SourceCode/hospital-turnos

# 1. Migración (proyecto de migraciones = Infraestructura, arranque = Web). No se conecta a la base.
dotnet ef migrations add AgregarAgendas \
  --project src/HospitalTurnos.Infraestructura --startup-project src/HospitalTurnos.Web \
  --output-dir Persistencia/Migraciones
# Se APLICA al arrancar la app, no con dotnet ef database update (ver Salvaguarda de BD).

# 2. Unitarias de esta story (sin Docker, en segundos)
dotnet test tests/HospitalTurnos.Dominio.Tests --filter "Story=4.1"
# Esperado: T1–T32 (más los casos de cada [Theory]), 0 fallas

# 3. Todas las de esta story (necesita Docker para Testcontainers)
dotnet test --filter "Story=4.1"
# Esperado: 44 pruebas declaradas, 0 fallas
dotnet test --filter "Story=4.1&AC=9"      # sólo la regla de los 7 días

# 4. Suite completa y CI local (AC 32)
dotnet test
bash scripts/ci/local.sh; echo "rc=$?"     # rc=0, cobertura de Dominio y Aplicacion >= 80 %

# 5. La base de Compose (Salvaguarda de BD, pasos 1 a 3)
docker compose up -d --build
docker compose exec db psql -U postgres -d hospital_turnos -c "\d agendas_medico"
```

Verificación en el navegador (corta: la story no tiene pantallas propias): con un médico que tenga
una agenda aprobada que empieza dentro de unas semanas (cargada por SQL o con la 4.3 cuando exista),
`/personal/admin/medicos` muestra `Sin agenda (rige desde dd/MM)` en su fila. Si no hay forma de
cargarla sin la 4.2/4.3, alcanza con T44 y se anota en el Dev Agent Record.

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

### Change Log

- 2026-10-16: Story escrita (Mary, con Winston) contra el código de `main` (`c3413cc`), antes de
  que existan 3.1 y 3.2. 32 AC, 43 verificaciones, 4 decisiones abiertas para el docente.
- 2026-10-16: Corregida con la revisión adversarial (`revision-sprint-2.md`) y las decisiones del
  docente. El contrato del estado de agenda se copia del AC 23 de la 3.2 (C-1):
  `EstadoAgendaMedico` y `SituacionDeAgenda` en `Dominio/Agendas/`, `EstadoDeAgenda.Calcular` pasa
  al dominio (y T32 a Dominio.Tests), `GestionMedicosEnBase` recibe `IConsultaAgendas` como quinto
  parámetro, sin tocar las pruebas de la 3.2; se quita la cita a una decisión inexistente.
  `MedicoId` `long` y `medico_id bigint` (I-4, decisión 15). Aprobada que todavía no rige:
  `SinAgenda` con su fecha (decisión 8), con T32 (h), T41, T43 y la prueba nueva T44 en la pantalla.
  FK de franjas `NOT NULL` (M-1); `LogTo` con una lista (M-2); `depends_on` con la 2.2 (M-3); AC 31
  declarativo (M-4). Decisiones 11–14 tomadas, con los avisos de la 4.3 en el contrato. 32 AC,
  44 verificaciones; ninguna decisión abierta.
