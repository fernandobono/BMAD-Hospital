---
documento: PRD — Requisitos del producto
proyecto: Gestión de turnos — Hospital Municipal Las Acacias (caso simulado)
version: 1.2
fecha: 2026-10-09
autor: John (producto) con Mary, Winston y Sally
estado: aprobado (2026-10-09)
entrada: docs/01-brief.md (v1.0, aprobado)
workflowType: prd
---

# PRD — Gestión de turnos del Hospital Municipal Las Acacias

> Sale del [brief aprobado](01-brief.md). Las reglas R-nn y los supuestos S-01 a S-16 vienen de ahí.
> Los supuestos nuevos de este documento arrancan en **S-17** y esperan confirmación.

## 1. Resumen ejecutivo

El hospital da ~400 turnos por día en consultorios externos con teléfono, ventanilla y una planilla
de Excel. Eso satura la línea a primera hora, deja ~25 % de turnos vacíos sin aviso y produce turnos
pisados.

El sistema reemplaza la planilla por una agenda única con dos puertas de entrada:

- **Recepción**: el personal da, cancela y consulta turnos por teléfono o en ventanilla.
- **Web del paciente**: el paciente saca y cancela sus propios turnos desde el celular, sin contraseña.

Además, el administrador gestiona médicos, horarios y bloqueos por licencia. El médico consulta e
imprime su agenda del día. Todo cambio sobre un turno queda registrado.

**Usuarios:** paciente, recepcionista, médico, administrador de agendas.

## 2. Criterios de éxito

| Id | Criterio | Meta | Cómo se mide | Traza |
|---|---|---|---|---|
| CE-01 | Turnos sacados por la web | ≥ 40 % del total a los 6 meses | Canal registrado en cada turno (auditoría) | Brief §5 |
| CE-02 | Turnos vacíos sin aviso | ≤ 15 % a los 6 meses | Medición manual del hospital (presente/ausente queda fuera de v1) | Brief §5 |
| CE-03 | Turnos duplicados | 0 | Restricción del sistema + prueba de reservas simultáneas | R-01 |
| CE-04 | Tiempo para dar un turno en ventanilla | < 60 s desde que se busca al paciente hasta que se confirma | Prueba con recepcionistas sobre 10 casos | Brief §5 |
| CE-05 | Pacientes afectados por un bloqueo que reciben aviso | 100 %: por email o en la lista para llamar | Comparar turnos cancelados por el hospital vs. avisos registrados | R-10 |

## 3. Alcance

### Primera versión (MVP)

Todo lo que figura en el brief §6 "Entra": pacientes, médicos y horarios, disponibilidad,
reserva y cancelación por los dos canales, "Mis turnos", agenda del día con impresión, bloqueo de
agenda con aviso, usuarios del personal con roles y auditoría.

### Segunda versión (crecimiento)

- Recordatorio por email 48 h antes del turno, con enlace para cancelar.
- Marcar presente / ausente y medir el ausentismo dentro del sistema (CE-02 pasa a ser automático).
- Lista de espera por especialidad que se ofrece cuando se libera un turno.
- Reportes: turnos por especialidad, demanda no cubierta, cancelaciones.
- Ideas de capacidad de agenda (duración por especialidad, primera vez / control, cupo por canal, consultorios): ver [brainstorm](brainstorm-capacidad-agenda.md). En la v1 no hay tope de turnos por día.

### Visión

- Obras sociales y prepagas.
- Recordatorios por WhatsApp.
- Integración con la historia clínica.

## 4. Recorridos de usuario

### RU-01 — El paciente saca un turno desde el celular

Rosa, 68 años, necesita control con el cardiólogo. Entra a la web desde el celular, pone su DNI y su
email y recibe un código de 6 dígitos. Lo ingresa, elige **Cardiología** y después al Dr. Fernández,
que ya la atendió antes (o "cualquier médico" si no tiene preferencia). Ve los próximos días con
lugar y toca el martes 10:30. El sistema le muestra el resumen y ella confirma. Recibe un email con
el turno.

**Si ya tiene un turno futuro de Cardiología**, el sistema se lo muestra y no la deja sacar otro (R-04).
**Si es la primera vez**, después del código completa nombre, apellido y teléfono.

### RU-02 — El paciente cancela su turno

Rosa no puede ir. Entra con su DNI y el código, ve **Mis turnos** y cancela el del martes. El horario
queda libre para otra persona. Si faltan menos de 2 horas, el sistema no la deja cancelar y le
muestra el teléfono de la recepción (R-06).

### RU-03 — La recepcionista da un turno por teléfono

Carla atiende el teléfono. Busca al paciente por DNI; si no existe, lo da de alta en el momento.
Elige especialidad y ve los primeros horarios libres de todos los médicos de esa especialidad, o los
de un médico en particular si el paciente lo pide. Confirma y le dicta fecha, hora y médico. Todo en
menos de un minuto (CE-04).

### RU-04 — La recepcionista cancela un turno

Un paciente llama a la mañana para cancelar el turno de las 10. Carla lo busca por DNI y lo cancela.
A la recepción no la frena el límite de 2 horas: puede cancelar mientras el turno no haya empezado.

### RU-05 — El administrador bloquea la agenda por una licencia

El Dr. Fernández avisa que se toma licencia del 3 al 14 de noviembre. Lucía, administradora, carga
el bloqueo con el motivo. Antes de confirmar, el sistema le muestra los 46 turnos afectados. Confirma.
Los turnos pasan a **Cancelado por el hospital**. A los 31 pacientes con email les llega un aviso con
un enlace para sacar un turno nuevo. Los 15 sin email aparecen en la lista **Pacientes para avisar**
de la recepción, que los llama y los marca como avisados (R-09, R-10).

### RU-06 — El médico mira e imprime su agenda

La Dra. Ruiz entra con su usuario y ve su agenda del día: horario, paciente, DNI y estado. Puede mirar
otro día. Sólo ve su propia agenda (R-13). La recepción imprime la misma lista en A4 para los médicos
que prefieren el papel.

### RU-07 — Un médico nuevo propone su agenda

Llega la Dra. Méndez, traumatóloga. Lucía, administradora, la da de alta con matrícula y especialidad
y le crea un usuario con rol **médico**. Todavía no tiene agenda, así que no ofrece turnos.

La Dra. Méndez entra y arma su propuesta: turnos de 15 minutos, lunes de 8 a 12 y jueves de 14 a 18,
desde el 1 de diciembre. La envía. Lucía ve "1 propuesta pendiente", la revisa y la aprueba. La
médica recibe un email y desde el 1 de diciembre sus horarios aparecen como disponibles.

### RU-07b — Un médico cambia su agenda y se la rechazan

El Dr. Pereyra propone pasar los martes a la mañana. Lucía la rechaza con el motivo "El consultorio 3
está ocupado los martes a la mañana; proponé miércoles". Él recibe el aviso, corrige y la reenvía.
Mientras tanto sigue vigente su agenda anterior, sin cambios.

### RU-08 — Recorridos de error

| Situación | Qué ve la persona |
|---|---|
| Dos personas eligen el mismo horario a la vez | La segunda ve "Ese horario se acaba de ocupar" y la lista de horarios actualizada. |
| El código del paciente venció o es incorrecto | "El código no es válido o venció. Pedí uno nuevo." A los 5 intentos fallidos se bloquea el pedido de código por 15 min. |
| El paciente no tiene email | La web le indica que saque turno por teléfono, con el número de la recepción. |
| Se intenta bloquear un período que ya pasó | El sistema no lo permite. |
| El email de aviso no se pudo enviar | El paciente pasa a la lista **Pacientes para avisar** de la recepción. |

## 5. Requisitos del dominio

| Id | Requisito | Origen |
|---|---|---|
| DOM-01 | Los datos de pacientes son datos personales de salud: sólo los ve quien los necesita para su tarea. | Ley 25.326 de Protección de Datos Personales |
| DOM-02 | El sistema no guarda motivos de consulta, diagnósticos ni ningún dato clínico. | Brief §6 — reduce la exposición |
| DOM-03 | Todo acceso que cambia un turno queda registrado y no se puede alterar. | R-12, Ley 25.326 |
| DOM-04 | El paciente puede consultar qué datos suyos tiene el sistema (los ve en su perfil). | Ley 25.326, derecho de acceso |
| DOM-05 | Los registros de auditoría se conservan 5 años. **[SUPUESTO S-17]** | Respaldo ante reclamos |

## 6. Tipo de proyecto

Aplicación web con dos públicos:

- **Personal del hospital**: uso en computadoras de escritorio de la red del hospital, con usuario y contraseña.
- **Pacientes**: uso desde internet, principalmente en celulares de gama baja, con pantallas desde 360 px de ancho.

Una sola aplicación sirve a los dos públicos. No hay aplicación móvil nativa en la v1.

## 7. Requisitos funcionales

### 7.1 Pacientes

- **FR-01.** La recepción puede buscar un paciente por DNI y ver sus datos y sus turnos.
- **FR-02.** La recepción puede dar de alta un paciente con DNI, nombre, apellido, teléfono y email opcional. El DNI tiene 7 u 8 dígitos y no se repite (R-08).
- **FR-03.** La recepción puede corregir nombre, apellido, teléfono y email de un paciente. El DNI no se modifica. **[SUPUESTO S-18]**
- **FR-04.** El paciente puede registrarse por la web con DNI, email verificado por código, nombre, apellido y teléfono.
- **FR-05.** Si el DNI ya existe sin email, el paciente no puede asociarle un email por la web: debe hacerlo la recepción. Evita que alguien se apropie del DNI de otro. **[SUPUESTO S-19]**
- **FR-06.** El paciente puede ver sus datos personales y actualizar su teléfono.

### 7.2 Acceso e identificación

- **FR-07.** El paciente entra con DNI + email y un código de 6 dígitos enviado a ese email. El código vence a los 10 minutos y sirve una sola vez. **[SUPUESTO S-20]**
- **FR-08.** Tras 5 códigos incorrectos para un mismo DNI, el sistema no envía ni acepta códigos por 15 minutos.
- **FR-09.** El personal entra con usuario y contraseña. Cada usuario tiene un rol: recepcionista, médico o administrador.
- **FR-10.** El administrador puede crear, desactivar y reactivar usuarios del personal, y asignarles rol. Un usuario médico queda vinculado a un médico.
- **FR-11.** Cada rol accede sólo a sus funciones (ver tabla de permisos, §7.8).

### 7.3 Médicos, especialidades y horarios

- **FR-12.** El administrador puede crear especialidades y desactivarlas si no tienen turnos futuros.
- **FR-13.** El administrador puede dar de alta un médico con nombre, apellido, matrícula única y especialidad. Un médico recién creado no tiene agenda y no ofrece turnos.
- **FR-14.** El médico puede armar una **propuesta de agenda**: duración del turno (10 a 60 min, R-02), franjas semanales (día, hora desde, hora hasta) sin superposiciones, y fecha desde la que rige. La fecha es al menos 7 días posterior al envío. **[SUPUESTO S-31]**
- **FR-14b.** La propuesta pasa por estos estados: **Borrador → Enviada → Aprobada** o **Rechazada**. El médico edita sólo borradores y rechazadas; una rechazada se puede corregir y reenviar. Cada médico tiene como máximo una propuesta enviada a la vez.
- **FR-14c.** El administrador ve las propuestas enviadas, comparadas contra la agenda vigente del médico, y las aprueba o las rechaza. El rechazo exige un motivo.
- **FR-14d.** Al aprobarse, la propuesta reemplaza a la agenda vigente desde su fecha de inicio. Hasta esa fecha sigue rigiendo la anterior.
- **FR-14e.** El médico recibe un email cuando su propuesta se aprueba o se rechaza, y ve el estado en el sistema. El menú del administrador muestra la cantidad de propuestas pendientes.
- **FR-14f.** El administrador puede corregir directamente la agenda de un médico sin propuesta, para urgencias. Queda en la auditoría con un motivo obligatorio. **[SUPUESTO S-32]**
- **FR-15.** Una agenda nueva no cancela turnos ya dados. Si alguno queda fuera de las nuevas franjas, el administrador ve la lista antes de aprobar y decide si igual aprueba; esos turnos se mantienen. **[SUPUESTO S-21]**

### 7.4 Disponibilidad y reserva

- **FR-16.** Cualquier usuario autorizado puede ver los horarios libres de un médico en un día, o los primeros 10 horarios libres de una especialidad entre todos sus médicos.
- **FR-16b.** El paciente y la recepción pueden ver los médicos de una especialidad con el próximo día libre de cada uno, y elegir uno o "cualquier médico".
- **FR-17.** Un horario es libre si cae dentro de una franja del médico, no está bloqueado, no tiene un turno reservado y está entre ahora y 60 días adelante (R-02, R-03, R-09).
- **FR-18.** La recepción puede reservar un turno libre para cualquier paciente; el paciente, sólo para sí mismo.
- **FR-19.** El sistema rechaza una reserva si el horario dejó de estar libre, si el paciente ya tiene otro turno a esa hora (R-05) o si ya tiene un turno futuro en esa especialidad (R-04, salvo R-11).
- **FR-20.** Al reservar, el paciente con email recibe una confirmación por email con fecha, hora, médico y especialidad.

### 7.5 Cancelación

- **FR-21.** El paciente puede cancelar su turno hasta 2 horas antes (R-06).
- **FR-22.** La recepción puede cancelar cualquier turno hasta la hora de inicio.
- **FR-23.** Un turno cancelado libera su horario de inmediato (R-07).
- **FR-24.** Cada cancelación registra quién canceló: el paciente, la recepción o el hospital por bloqueo.

### 7.6 Bloqueo de agenda

- **FR-25.** El administrador puede bloquear un período de un médico (desde fecha y hora, hasta fecha y hora) con un motivo.
- **FR-26.** Antes de confirmar un bloqueo, el sistema muestra la cantidad y la lista de turnos afectados.
- **FR-27.** Al confirmar, los turnos afectados pasan a **Cancelado por el hospital** (R-10).
- **FR-28.** El paciente afectado con email recibe un aviso dentro de los 5 minutos, con enlace a la web para sacar otro turno. **[SUPUESTO S-22]**
- **FR-29.** El paciente afectado sin email, o con un email que falló, aparece en la lista **Pacientes para avisar**. La recepción lo marca como avisado y el sistema registra quién y cuándo.
- **FR-30.** El administrador puede levantar un bloqueo futuro. Los turnos ya cancelados no se restauran. **[SUPUESTO S-23]**

### 7.7 Agenda del día

- **FR-31.** El médico ve su agenda de un día: hora, paciente, DNI y estado de cada turno. Por defecto, la de hoy.
- **FR-32.** La recepción ve la agenda de cualquier médico y la imprime en una hoja A4 con hora, paciente y DNI.

### 7.8 Auditoría y permisos

- **FR-33.** Cada reserva, cancelación, bloqueo, levantamiento de bloqueo, "marcado como avisado", envío, aprobación o rechazo de propuesta de agenda y corrección directa de agenda registra: quién, rol, canal (web o recepción), fecha y hora, y turno o bloqueo afectado.
- **FR-34.** El administrador puede consultar la auditoría filtrando por paciente (DNI), médico o rango de fechas.
- **FR-35.** Nadie puede modificar ni borrar registros de auditoría desde el sistema.

**Permisos por rol:**

| Función | Paciente | Recepción | Médico | Administrador |
|---|:-:|:-:|:-:|:-:|
| Sacar / cancelar turno propio | ✔ | | | |
| Sacar / cancelar turno de cualquier paciente | | ✔ | | |
| Alta y edición de pacientes | | ✔ | | |
| Ver agenda de un médico | | ✔ | sólo la suya | ✔ |
| Imprimir agenda del día | | ✔ | ✔ | ✔ |
| Lista "Pacientes para avisar" | | ✔ | | ✔ |
| Proponer su propia agenda | | | ✔ | |
| Aprobar o rechazar propuestas de agenda | | | | ✔ |
| Médicos y especialidades | | | | ✔ |
| Bloqueos de agenda | | | | ✔ |
| Usuarios del personal | | | | ✔ |
| Consultar auditoría | | | | ✔ |

## 8. Requisitos no funcionales

| Id | Requisito | Cómo se verifica |
|---|---|---|
| NFR-01 | Las búsquedas de horarios libres y las reservas responden en menos de 1 segundo para el 95 % de las operaciones, con 30 usuarios simultáneos. | Prueba de carga |
| NFR-02 | Nunca hay dos turnos reservados para el mismo médico y horario, aun con reservas simultáneas. | Prueba de concurrencia automatizada |
| NFR-03 | Todo el tráfico va cifrado (HTTPS). Las contraseñas del personal se guardan con un algoritmo de hash lento, nunca en texto. | Revisión de seguridad |
| NFR-04 | La sesión del personal vence tras 30 minutos sin uso; la del paciente, tras 15. **[SUPUESTO S-24]** | Prueba funcional |
| NFR-05 | Las pantallas del paciente cumplen WCAG 2.1 nivel AA y funcionan en 360 px de ancho. | Auditoría de accesibilidad |
| NFR-06 | Todas las fechas y horas se manejan en hora de Argentina (UTC−3, sin horario de verano). | Prueba funcional |
| NFR-07 | Disponible de lunes a viernes de 7 a 19 h con 99,5 % de disponibilidad mensual. **[SUPUESTO S-25]** | Monitoreo |
| NFR-08 | Respaldo diario de la base, con prueba de restauración mensual. Se pierde como máximo un día de datos. | Procedimiento operativo |
| NFR-09 | Los errores no muestran detalles técnicos al usuario; quedan en el registro técnico. | Revisión de seguridad |
| NFR-10 | Todo dato que ingresa se valida en la pantalla (para ayudar) y en el servidor (para proteger). | Pruebas de seguridad |
| NFR-11 | Cobertura de pruebas automáticas ≥ 80 % sobre las reglas de negocio. | Reporte de cobertura |

## 9. Trazabilidad

| Regla / objetivo del brief | Recorridos | Requisitos |
|---|---|---|
| R-01 turno único por horario | RU-01, RU-03, RU-08 | FR-17, FR-19, NFR-02 |
| R-02 franjas y duración | RU-07 | FR-14, FR-17 |
| R-14 agenda propuesta por el médico | RU-07, RU-07b | FR-13, FR-14 a FR-14f, FR-15 |
| R-03 sin pasado, hasta 60 días | RU-01 | FR-17 |
| R-04 un turno futuro por especialidad | RU-01 | FR-19 |
| R-05 sin dos turnos a la misma hora | RU-01, RU-03 | FR-19 |
| R-06 cancelación web hasta 2 h | RU-02 | FR-21 |
| R-07 cancelación libera el horario | RU-02, RU-04 | FR-23 |
| R-08 DNI único | RU-03 | FR-02, FR-04, FR-05 |
| R-09 / R-10 / R-11 bloqueo y aviso | RU-05 | FR-25 a FR-30, FR-19 |
| R-12 auditoría | RU-05 | FR-24, FR-29, FR-33 a FR-35 |
| R-13 roles del personal | RU-06, RU-07 | FR-09 a FR-11, FR-31, FR-32 |
| Ingreso del paciente sin contraseña (S-14) | RU-01, RU-02, RU-08 | FR-07, FR-08 |
| CE-04 turno en menos de 1 minuto | RU-03 | FR-01, FR-16, NFR-01 |

## 10. Supuestos nuevos de este documento

| Id | Supuesto |
|---|---|
| S-17 | La auditoría se conserva 5 años. |
| S-18 | El DNI de un paciente no se modifica después del alta. |
| S-19 | Si el DNI ya existe sin email, el email sólo lo asocia la recepción. |
| S-20 | Código del paciente: 6 dígitos, vence a los 10 minutos, un solo uso. |
| S-21 | Cambiar las franjas de un médico no cancela turnos ya dados; sólo avisa al administrador. |
| S-22 | El aviso por bloqueo llega dentro de los 5 minutos. |
| S-23 | Levantar un bloqueo no restaura los turnos cancelados. |
| S-24 | La sesión vence a los 30 minutos sin uso (personal) y a los 15 (paciente). |
| S-25 | Disponibilidad exigida: días hábiles de 7 a 19 h, 99,5 % mensual. |
| S-31 | Una agenda propuesta rige como mínimo 7 días después de enviarla, para que el administrador tenga tiempo de revisarla. |
| S-32 | El administrador puede corregir una agenda sin propuesta del médico, para urgencias, dejando el motivo. |
