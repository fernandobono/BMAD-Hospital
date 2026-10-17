---
epic: 4
story: "4.2"
title: "El médico arma, guarda y envía su propuesta de agenda"
status: done
sprint: 3
priority: "P0"
approach: "[CREATE]"
depends_on: ["1.2", "1.3", "2.1", "2.2", "3.2", "4.1"]
files_to_create:
  - "src/HospitalTurnos.Aplicacion/Agendas/IPropuestaAgenda.cs"
  - "src/HospitalTurnos.Aplicacion/Agendas/DatosAgenda.cs"
  - "src/HospitalTurnos.Infraestructura/Agendas/PropuestaAgendaEnBase.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/<marca>_UnaPropuestaEditablePorMedico.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/<marca>_UnaPropuestaEditablePorMedico.Designer.cs"
  - "src/HospitalTurnos.Web/Components/Personal/Agendas/MiPropuesta.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Agendas/EditorDeAgenda.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Agendas/ResumenDeAgenda.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Agendas/FormularioAgenda.cs"
  - "src/HospitalTurnos.Web/Components/Personal/Agendas/FormaDeResumen.cs"
  - "tests/HospitalTurnos.Web.Tests/Agendas/FormularioAgendaTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Agendas/ComponentesDeAgendaTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Agendas/MiPropuestaHttpTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Agendas/DatosDePropuesta.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Agendas/PropuestaAgendaTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Agendas/DatosDePropuesta.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Personal/ActorVigenteIdentityTests.cs"
  - "docs/qa/4.2-pasos.md"
  - "docs/qa/4.2-legibilidad.md"
  - "docs/qa/4.2-legibilidad-ui.md"
files_to_modify:
  - "src/HospitalTurnos.Aplicacion/Personal/IActorVigente.cs"
  - "src/HospitalTurnos.Infraestructura/Identidad/ActorVigenteIdentity.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/AgendaMedicoConfiguracion.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/HospitalDbContextModelSnapshot.cs"
  - "src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs"
  - "src/HospitalTurnos.Web/Components/Personal/Secciones/SeccionesMedico.razor"
  - "src/HospitalTurnos.Web/Components/_Imports.razor"
  - "tests/HospitalTurnos.Web.Tests/Personal/SesionYPermisosHttpTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Agendas/DatosDeAgenda.cs"
---

# Story 4.2: El médico arma, guarda y envía su propuesta de agenda

Status: done

## Story

Como médico del Hospital Las Acacias (la Dra. Méndez, el Dr. Pereyra),
quiero armar mi propuesta de agenda (duración del turno, franjas de cada semana y fecha desde la que
rige), guardarla como borrador mientras la pienso y enviarla para que el administrador la apruebe,
viendo siempre mi agenda vigente y cuántos turnos ofrezco,
para que mis horarios en el sistema sean los que yo propongo (RU-07) y, si me la rechazan, pueda
corregirla y reenviarla (RU-07b).

> **Qué es y qué no es esta story.** Es la pantalla **M2** y sus casos de uso: crear, editar, guardar
> borrador y enviar. Usa las reglas de la 4.1 sin cambiarlas. **No** aprueba ni rechaza (4.3), **no**
> manda emails (FR-14e es de la 4.3), **no** corrige agendas por urgencia (4.4). Deja hechos los dos
> componentes de pantalla que la 4.3 y la 4.4 reusan (AC 24).

## Acceptance Criteria

> Pantalla M2 de `docs/03-ux.md` §6 (FR-14, FR-14b; FR-14e sólo en "ve el estado en el sistema").
> Ruta `/personal/medico/mi-propuesta` (ya existe en `RutasPersonal.MiPropuesta` y en el menú del
> médico desde la 2.1), sólo para el rol `Medico`. La página es de **renderizado estático en el
> servidor** (SSR: el servidor arma el HTML completo y el navegador envía formularios comunes), como
> A1 de la 3.2 (decisión 3). Mensajes en castellano rioplatense.
>
> Vocabulario de esta story:
>
> - **Médico de la sesión**: el `Medico` cuyo `UsuarioPersonalId` es el usuario que ingresó (contrato
>   del AC 23 de la 3.2). Nunca sale de un dato del formulario (Arq. §7.7).
> - **Propuesta en curso**: la agenda del médico de la sesión que está en `Borrador`, `Rechazada` o
>   `Enviada`. **Editable** = `Borrador` o `Rechazada`. Hay a lo sumo una editable y a lo sumo una
>   enviada, y nunca las dos a la vez (AC 18 y 19; decisión 2).
> - **Hoy**: la fecha local del hospital (Córdoba), del `TimeProvider` inyectado. **Mínimo** = hoy + 7
>   días (`AgendaMedico.DiasMinimosDeAnticipacion`, S-31).
>
> Fechas en pantalla `dd/MM/yyyy` y horas `HH:mm`, con `CultureInfo.InvariantCulture`, como los
> mensajes del dominio de la 4.1. Las marcas `EnviadaEl` y `RechazadaEl` (UTC) se muestran en hora de
> Córdoba (`TimeZoneInfo.ConvertTime(marca, reloj.LocalTimeZone)`). Regla de fechas del sprint (M-2 de
> la revisión): con año en los textos que dicen "desde" o "el"; sin año sólo en columnas angostas. M2
> no tiene columnas: todas sus fechas van con año.

### Pantalla M2 — lo que el médico ve (FR-14, FR-14b, FR-14e)

1. **Given** un médico ingresado y vinculado a un médico **When** pide `/personal/medico/mi-propuesta`
   **Then** recibe `200` con `<h1>Mi propuesta de agenda</h1>` (exacto, sin atributos: lo compara la
   T19 de la 2.1) y el texto `Esta sección se habilita en una próxima versión.` ya no aparece. La
   entrada `Mi propuesta de agenda` del menú del médico (2.1) lleva a esta pantalla y no cambia.
   `Mi agenda` (M1, story 8.1) sigue en construcción.
2. **Given** un usuario médico **sin** vínculo con ningún médico **When** pide la pantalla **Then**
   `200` con el `<h1>` del AC 1 y, en lugar del contenido, el texto exacto
   `Tu usuario no está vinculado a un médico. Pedíselo al administrador.` (contrato del AC 23 de la
   3.2, decisión 17), sin formulario, sin `500` y sin pantalla vacía.
3. **Agenda vigente arriba** (UX M2: "arriba siempre se ve la agenda vigente"): **Given** que al
   médico le rige hoy una agenda (`AgendaMedico.QueRigeEn(agendas, hoy)`, 4.1) **Then** se lee
   `Mi agenda vigente:` seguido del resumen de esa agenda (AC 24, `ResumenDeAgenda`); **Given** que no
   le rige ninguna **Then** `Todavía no tenés una agenda vigente.` **Given** además una aprobada que
   todavía no rige **Then** se lee `Tu agenda nueva ya está aprobada y rige desde el dd/MM/yyyy.` con
   su resumen (la más próxima, igual que `RigeDesde` de la 4.1 AC 22).
4. **Sin propuesta en curso** **When** abre la pantalla **Then** ve el formulario bajo el título
   `Nueva propuesta`, ya cargado (decisión 4): con la duración y las franjas de la
   **última aprobada** (la de `VigenteDesde` mayor; con la misma fecha, la de `AprobadaEl` mayor), o
   duración 20 y sin franjas si no tiene ninguna; y `Rige desde` = el mínimo. Abrir la pantalla no
   guarda nada.
5. **Con una propuesta editable** **Then** el formulario se carga con sus datos bajo el título
   `Tu propuesta`, con la línea `Estado: Borrador` o `Estado: Rechazada`. **Given** que tiene
   `MotivoRechazo` (una rechazada, o una rechazada que ya empezó a corregir y volvió a `Borrador`, 4.1
   AC 12) **Then** se leen `Motivo del rechazo: «<motivo>»` y `Rechazada el dd/MM/yyyy a las HH:mm.`
   Quién rechazó lo agrega la 4.3 **en esta misma línea** (decisión 7, contrato en el AC 24):
   `Rechazada el dd/MM/yyyy a las HH:mm por Lucía Pérez.`; sin ese dato, la línea queda como acá.
6. **Con una propuesta enviada** **Then** la pantalla queda en sólo lectura (UX M2): `Estado: Enviada`,
   `Esperando aprobación desde el dd/MM/yyyy.` (fecha local de `EnviadaEl`),
   `Rige desde el dd/MM/yyyy.` y el resumen de la propuesta. No hay formulario ni botones.

### El editor de agenda (FR-14, R-02, UX M2)

7. **Campos**, cada uno con etiqueta visible (NFR-05): `Duración del turno (minutos)` (número),
   `Rige desde` (fecha) con `(mín. dd/MM/yyyy)` al lado, y una tabla de franjas con una fila por
   franja (decisión 5): `Día` (lista `Lunes` a `Domingo`), `Desde` y `Hasta` (hora), y un
   botón `Quitar` por fila. Debajo de la tabla: `Para terminar a medianoche, poné 00:00.`, el botón
   `+ Agregar franja` y el botón `Actualizar el cálculo`. Al pie, `Guardar borrador` y
   `Enviar para aprobación`. Apretar Enter en un campo sólo actualiza el cálculo: nunca quita una
   franja ni envía (el primer botón del formulario es uno oculto con la acción `actualizar`).
8. **Los botones de edición no guardan**: **Given** el formulario **When** se aprieta
   `+ Agregar franja` **Then** la respuesta es `200` con una fila más al final (`Lunes`, sin horas);
   `Quitar` saca **esa** fila; `Actualizar el cálculo` vuelve a dibujar con lo escrito. En los tres,
   la base no cambia y lo escrito se conserva. **Given** una acción que el formulario no conoce (POST
   armado a mano, o `Quitar` de una fila que no existe) **Then** `400` con el cartel
   `No reconocemos esa acción. Recargá la página.` (el texto de la 3.1).
9. **Revisión de cada fila**, en cada dibujo del formulario (UX M2: "una franja que se superpone se
   marca en rojo antes de enviar"). La fila con un problema se marca en rojo **y** lleva el texto del
   problema en la misma fila (nunca sólo color, NFR-05):

   | Caso | Texto en la fila |
   |---|---|
   | Falta `Desde` o `Hasta` | `Completá la hora desde y la hora hasta, o quitá la franja.` |
   | Horas que no forman una franja (4.1 AC 2) | el mensaje del dominio, p. ej. `La franja tiene que terminar después de empezar.` |
   | Se superpone con una fila anterior (4.1 AC 4) | el mensaje del dominio, p. ej. `La franja del lunes de 10:00 a 14:00 se superpone con la de 08:00 a 12:00.` |

   Se marca la fila que choca con una **anterior** (la primera queda sin marca). **Given** una franja
   que no es múltiplo de la duración (decisión 12 de la 4.1: se acepta) **Then** la fila lleva, sin
   rojo y sin bloquear nada, `Los últimos N minutos de esta franja no se usan: no entra un turno completo.`
   (lunes 08:00–10:50 con turnos de 20 → `Los últimos 10 minutos…`).
10. **Turnos por semana** (UX M2): debajo de la tabla se lee
    `Con esta agenda ofrecés N turnos por semana (antes: M).`, donde N es
    `AgendaMedico.TurnosPorSemana()` (4.1 AC 19) de la agenda que arma el formulario y M el de la
    agenda vigente (sin vigente, sin el paréntesis). El cálculo es **uno solo**, en el dominio: el
    editor, el resumen y la A6 de la 4.3 usan ese método. **Given** alguna fila con problema o una
    duración fuera de 10–60 **Then** `Corregí lo marcado para ver cuántos turnos ofrecés.`; **Given**
    ninguna franja (y una duración válida: si la duración está fuera de rango, manda el cartel anterior)
    **Then** `Agregá franjas para ver cuántos turnos ofrecés por semana.` En SSR el
    número se actualiza con cada botón del formulario, no mientras se escribe (decisión 3).
11. **Validación de campos** (sólo con `Guardar borrador` y `Enviar para aprobación`): **Given** un
    campo inválido **Then** `200` con el mensaje debajo del campo, lo escrito conservado, y no se
    guarda ni se audita nada. Corre en el servidor (un POST armado a mano da lo mismo):

    | Caso | Mensaje |
    |---|---|
    | Duración vacía | `Ingresá la duración del turno.` |
    | Duración fuera de 10–60 | `La duración del turno tiene que estar entre 10 y 60 minutos.` (el del dominio) |
    | `Rige desde` vacía | `Elegí desde qué fecha rige.` |
    | Alguna fila sin `Desde` o sin `Hasta` | debajo de la tabla: `Completá la hora desde y la hora hasta de cada franja, o quitá las que sobran.` |

### Guardar borrador (FR-14, FR-14b)

12. **Given** un médico sin propuesta en curso **When** aprieta `Guardar borrador` con datos válidos
    **Then** se crea una agenda en `Borrador` del médico de la sesión con esa duración, esa fecha y esas
    franjas (con los métodos públicos de la 4.1), y la respuesta es `302` a
    `/personal/medico/mi-propuesta?resultado=borrador`, donde se lee `Borrador guardado.` en
    `role="status"`. Un borrador **no se audita**: sólo el envío (constantes fijas de la 1.3; ver
    Decisiones). Un borrador puede no tener franjas (enviarlo, no: 4.1 AC 9).
13. **Given** una propuesta editable **When** guarda **Then** se aplican **sólo las diferencias**: las
    franjas que no cambiaron conservan su fila; las que ya no están se quitan (`QuitarFranja`); las
    nuevas se agregan (`AgregarFranja`); duración y fecha sólo si cambiaron. Respuesta `302` a
    `?resultado=borrador`. Una `Rechazada` con algún cambio pasa a `Borrador` conservando motivo y
    fecha del rechazo (4.1 AC 12). **Given** que no cambió nada **Then** no se guarda, una `Rechazada`
    **sigue** `Rechazada`, y el `302` va a `?resultado=sin-cambios` con `No había cambios para guardar.`
    `?resultado=` se traduce con un `switch` de valores conocidos; cualquier otro no muestra nada.
14. **Given** datos que el dominio rechaza (superposición, horas inválidas, duración fuera de rango)
    **When** guarda **Then** `400` con el mensaje del dominio en el cartel `role="alert"` arriba del
    formulario (UX §8), lo escrito conservado (las filas con problema marcadas, AC 9) y nada guardado.

### Enviar para aprobación (FR-14, FR-14b, S-31)

15. **Given** datos válidos **When** aprieta `Enviar para aprobación` **Then**, en **un solo paso** y
    en una sola transacción (decisión 6): se aplican los datos de la pantalla a la
    propuesta editable (como el AC 13) o se crea una nueva, se llama a `Enviar(reloj)` (4.1 AC 9) y se
    registra la auditoría del AC 17. La respuesta es `302` a `?resultado=enviada`, donde se lee
    `Propuesta enviada para aprobación.` en `role="status"` y la vista de sólo lectura del AC 6. El
    listado A1 del administrador pasa a mostrar `Propuesta pendiente` para ese médico (lo calcula la
    4.1; no se toca).
16. **Given** un envío que falla **Then** responde con el código y el cartel del mensaje del dominio,
    lo escrito conservado, y **no se guarda nada** (tampoco los cambios de la pantalla) ni se audita:

    | Caso | Código | Mensaje (de la 4.1) |
    |---|---|---|
    | `Rige desde` antes del mínimo | `400` | `La agenda tiene que regir desde el <mínimo> o después: 7 días desde hoy.` |
    | Sin franjas | `400` | `Agregá al menos una franja antes de enviar la propuesta.` |
    | Superposición u horas inválidas | `400` | el del AC 14 |
    | Una `Rechazada` reenviada sin ningún cambio | `409` | `Corregí la propuesta antes de reenviarla.` |

17. **Auditoría del envío** (FR-33, R-12): un registro `AccionesAuditoria.PropuestaAgendaEnviada`
    (`"propuesta-agenda.enviada"`, ya existe desde la 1.3; no se crea ninguna constante), guardado
    junto con el envío o ninguno de los dos (Arq. §7.5):

    | Campo | Valor |
    |---|---|
    | `UsuarioId` | id del usuario médico |
    | `Quien` | su nombre completo, leído de la base (AC 21) |
    | `Rol` | `Medico` |
    | `Canal` | `CanalAuditoria.Personal` |
    | `EntidadTipo` | `"AgendaMedico"` |
    | `EntidadId` | id de la agenda (`long` como texto) |
    | `Detalle` | `{"medicoId", "duracionTurnoMinutos", "vigenteDesde": "yyyy-MM-dd", "franjas": [{"dia": <0–6 como en la base>, "desde": "HH:mm", "hasta": "HH:mm"}], "turnosPorSemana"}` |

    **Formato de franja en la auditoría — contrato** (I-6 de la revisión): el día como el número de la
    base (`(int)DayOfWeek`: 0 = domingo … 6 = sábado), `desde` y `hasta` en `HH:mm` con
    `CultureInfo.InvariantCulture`, y la medianoche final como `00:00` (igual que en la base). Las franjas
    van ordenadas por ese número de día y, en cada día, por `desde` (el domingo primero). Todo
    registro de auditoría que describa franjas de una `AgendaMedico` usa este formato: la 4.4 lo copia en
    `agenda.corregida` y la 9.1 (A5) lo lee igual en los dos. Lo prueba T23.

### Una propuesta a la vez (FR-14b; 4.1 AC 25 y AC 31; decisión 2)

18. **Control previo** (la primera defensa que la 4.1 dejó a esta story): **Given** que el médico ya
    tiene una propuesta `Enviada` **When** llega un `Guardar borrador` o un `Enviar` (POST armado a
    mano o desde una pestaña vieja) **Then** `409` con
    `Ya tenés una propuesta enviada esperando aprobación: hasta que el administrador la revise no podés modificarla ni armar otra.`
    y nada cambia.
19. **Las carreras, contra la base** (nunca un `500`):
    - Índice único parcial nuevo `ux_agendas_medico_una_editable` sobre `medico_id` con filtro
      `estado IN ('Borrador', 'Rechazada')`, en la migración `<marca>_UnaPropuestaEditablePorMedico`
      (sólo crea el índice: no toca datos ni el índice `ux_agendas_medico_una_enviada` de la 4.1).
    - `23505` de `ux_agendas_medico_una_enviada` (contrato de la 4.1) → `ConflictoException` con el
      texto del AC 18.
    - `23505` de `ux_agendas_medico_una_editable` (dos "primeros borradores" a la vez) →
      `ConflictoException` con `Ya tenés una propuesta en curso: volvé a abrir la pantalla para seguir con ésa.`
    - `DbUpdateConcurrencyException` (la propuesta cambió entre que se leyó y se guardó; `xmin`, 4.1
      AC 27) → `ConflictoException` con
      `Tu propuesta cambió mientras la guardabas: volvé a abrir la pantalla para ver cómo quedó.`
      Todo cambio sobre una propuesta existente actualiza su fila de `agendas_medico`, aunque sólo
      toque franjas, para que `xmin` también lo proteja.

### Permisos — FR-11, Arq. §7.7

20. **Given** un usuario con rol `Recepcionista` o `Administrador` **When** pide
    `/personal/medico/mi-propuesta` **Then** la respuesta final es `403` con
    `No tenés permiso para entrar a esta sección.` (página de la 2.1). **Given** nadie ingresado
    **Then** `302` a `/personal/ingreso?ReturnUrl=<ruta>`.
21. **El control está también en el caso de uso, en la base** (como la decisión 16 para el
    administrador): cada escritura confirma primero, con el método general **nuevo**
    `IActorVigente.ExigirActivoConRolAsync(actor, rol, mensajeSiNoLoEs, ct)` llamado con `Roles.Medico`
    (se agrega al servicio de la 2.2, no se crea otro), que el actor es un usuario existente, activo y
    con el único rol `Medico`; si no, `AccesoDenegadoException` con
    `Sólo un médico activo puede armar o enviar su propuesta de agenda.` El método devuelve el actor con
    `Quien` = nombre leído de la base, `Rol` = el rol pedido y canal `Personal`.
    `ExigirAdministradorActivoAsync` pasa a llamarlo con `Roles.Administrador`, **sin** cambiar su firma
    ni su comportamiento (2.2, 2.3, 3.1, 3.2 y 4.3 no cambian). Es **el** método de permiso por rol del
    sprint: la 5.1 lo reusa con `Roles.Recepcionista` (I-3 de la revisión). Después busca el médico de
    la sesión; si no hay, `AccesoDenegadoException` con el texto del AC 2. En la página se ven como un
    cartel con `403` (mecanismo de la 2.2). Leer la pantalla (AC 1–6) no consulta el rol en la base,
    como los listados de la 3.2.
22. **Sólo la propuesta del médico de la sesión**: el formulario no tiene ningún campo con un id de
    médico ni de agenda; el caso de uso busca la propuesta en curso por el médico de la sesión. Un
    médico nunca lee ni cambia la agenda de otro.
23. **Given** un POST sin el token antifalsificación (antiforgery) **Then** `400` y no se guarda ni se
    audita nada.

### Contrato con otras stories (con pruebas asignadas: acción B-1 de la retrospectiva del S2)

24. Lo que esta story **fija** y las siguientes **usan sin cambiar**. Lo prueban T1, T6, T7 y T9–T14
    (componentes y formulario), T46 (el editor con un modelo heredado, como lo usa la 4.4), T47 (la
    marca `← cambia` que usa la 4.3), T15 (el permiso por rol que reusa la 5.1), T17 (la vista para la
    4.3), T23 (el formato de franjas de la auditoría), T27 y T32 (índices) y T41 (bloque del rechazo):
    - **Ubicación**: los dos componentes compartidos viven en
      `src/HospitalTurnos.Web/Components/Personal/Agendas/` (espacio de nombres
      `HospitalTurnos.Web.Components.Personal.Agendas`, importado en `_Imports.razor`). Ahí también
      vive M2 (`MiPropuesta.razor`).
    - **(a) `EditorDeAgenda`** (`EditorDeAgenda.razor`): dibuja los campos de los AC 7, 9 y 10 **dentro
      del `EditForm` de quien lo usa** (no tiene formulario propio, como `CamposMedico` de la 3.2).
      Parámetros, y sólo éstos: `[Parameter, EditorRequired] FormularioAgenda Input`,
      `[Parameter] int? TurnosPorSemanaAntes` (el "(antes: M)"; `null` lo oculta) y
      `[Parameter] DateOnly? MinimoVigenteDesde` (el "(mín. …)"; `null` lo oculta). La página que lo
      usa llama a su propiedad de formulario **`Input`** (`[SupplyParameterFromForm] … Input`): los
      campos se envían como `Input.DuracionTurnoMinutos`, `Input.VigenteDesde`,
      `Input.Franjas[i].Dia|Desde|Hasta` y los botones como `Input.Accion`. Valores de `Input.Accion`
      que resuelve el formulario: `actualizar`, `agregar-franja` y `quitar-franja:<i>`; cualquier otro
      es de la página (`guardar` y `enviar` en M2; `corregir` y `corregir-igual` en la 4.4). El editor
      funciona igual si el modelo de la página **hereda** de `FormularioAgenda` (T46).
    - **`FormularioAgenda`** (`FormularioAgenda.cs`, **no** `sealed`: la 4.4 lo hereda para sumar el
      motivo): `DuracionTurnoMinutos`, `VigenteDesde`, `Franjas` (`List<FilaFranja>`),
      `Accion`; `bool AplicarAccionDeEdicion()` (true si era una acción del editor, ya aplicada; lanza
      `ReglaDeNegocioException` del AC 8 si es una acción del editor mal formada; false si es de la
      página); `RevisionDeAgenda Revisar()` (problemas por fila, avisos y turnos por semana);
      `DatosAgenda ADatos()`; `static FormularioAgenda Desde(AgendaMedico agenda)` y
      `static FormularioAgenda Nueva(DateOnly vigenteDesde)`.
    - **(b) `ResumenDeAgenda`** (`ResumenDeAgenda.razor`): sólo lectura. Parámetros, y sólo éstos:
      `[Parameter, EditorRequired] AgendaMedico Agenda` (con sus franjas cargadas),
      `[Parameter] FormaDeResumen Forma` (`EnLinea` por defecto; `EnLista`) y
      `[Parameter] AgendaMedico? CompararCon` (`null` por defecto; I-1 de la revisión). `EnLinea`:
      `turnos de 20 min · Lun 8–12 · Mié 8:30–12 · Vie 20–24` y en otro renglón `34 turnos/semana`
      (`1 turno/semana` en singular). `EnLista`: `turnos de 20 min`, una `<li>` por franja y el renglón
      de turnos. Franjas de lunes a domingo y, en cada día, por hora; horas sin minutos si son en punto;
      la medianoche final se escribe `24`; sin franjas, `Sin franjas`. Con `CompararCon` y forma
      `EnLista`, cada `<li>` de una franja que no tiene una igual (mismo día, desde y hasta) en
      `CompararCon` termina en ` ← cambia`, y el renglón `turnos de N min` también si la duración es
      distinta; las franjas que sólo están en `CompararCon` no se dibujan (las muestra la otra columna).
      Con `EnLinea`, `CompararCon` se ignora. Expone `public static string TextoDeFranja(Franja franja)`
      (`Lun 8–12`): el formato de una franja, escrito una sola vez.
    - **Turnos por semana**: `AgendaMedico.TurnosPorSemana()`
      (`src/HospitalTurnos.Dominio/Agendas/AgendaMedico.cs`, lo creó la 4.1 en su AC 19). Esta story no
      crea otro cálculo: el editor y el resumen llaman a ése, y la A6 de la 4.3 también.
    - **4.3 (aprobación)**: usa `ResumenDeAgenda` (forma `EnLista`) para vigente y propuesta lado a
      lado, con `CompararCon` = la vigente en la columna de la propuesta (T47). Agrega a M2 quién
      rechazó (decisión 7): suma **al final** de `VistaPropuesta` el dato `string? RechazadaPor = null`
      (con valor por defecto, para no romper los `new VistaPropuesta(...)` de esta story);
      `PropuestaAgendaEnBase.ObtenerAsync` lo carga con el `Quien` del **último** registro
      `propuesta-agenda.rechazada` de la propuesta en curso (`EntidadTipo = "AgendaMedico"`), y la línea
      del AC 5 pasa a `Rechazada el dd/MM/yyyy a las HH:mm por <RechazadaPor>.` cuando hay dato. Sin
      dato queda como acá: T41 sigue igual, porque su rechazada se arma sin registro de auditoría. La
      línea completa la prueba la T34 de la 4.3. Al rechazar, la propuesta
      pasa a `Rechazada` y ocupa el lugar de la editable: la 4.2 garantiza que en ese momento el médico
      no tiene otra editable (AC 18), así que `ux_agendas_medico_una_editable` no salta en la 4.3.
    - **4.4 (corrección de urgencia)**: reusa `EditorDeAgenda`, `FormularioAgenda` y `DatosAgenda` así,
      y sólo así (C-1 de la revisión): su modelo `FormularioCorreccion` **hereda** de `FormularioAgenda`
      (suma el motivo y la confirmación de turnos fuera); la propiedad de su página se llama `Input`;
      dibuja `<EditorDeAgenda Input="Input" MinimoVigenteDesde="…" TurnosPorSemanaAntes="…" />` dentro de
      su `EditForm`; usa `OnSubmit` (no `OnValidSubmit`) con el orden de M2: `AplicarAccionDeEdicion()` →
      acción desconocida → validar → su caso de uso; sus botones llevan sus propios valores de
      `Input.Accion` (`corregir`, `corregir-igual`). Pasa su propio `MinimoVigenteDesde` (hoy, decisión
      10). En la auditoría de franjas usa el formato del AC 17. Su agenda nace `Aprobada`: no la alcanza
      ningún índice de esta story.
    - **5.1 (pacientes)**: usa `IActorVigente.ExigirActivoConRolAsync` (AC 21) con
      `Roles.Recepcionista`, sin crear otro método; suma su prueba de la recepción al archivo
      `Aplicacion.Tests/Personal/ActorVigenteIdentityTests.cs` que crea esta story (T15).
    - **Ayudas de prueba de agendas**: `DatosDePropuesta` de Aplicacion.Tests y de Web.Tests (Given/Fixture)
      son de esta story (I-5 de la revisión). La 4.3 y la 4.4 las reusan y les suman lo suyo; nadie crea
      otra ayuda de agendas con otro nombre.
    - **8.1 (Mi agenda)**: el texto del médico sin vínculo es el mismo del AC 2.

### No-regresión

25. Siguen en verde las pruebas de las stories 1.1 a 4.1 y la 2.3 (ya en `main`). Cambios
    **esperados** en lo existente, y sólo éstos: `SeccionesMedico.razor` pierde
    `@page "/personal/medico/mi-propuesta"` (dos páginas con la misma ruta dan un error interno, L-6);
    en la T19 de la 2.1 (`SesionYPermisosHttpTests`), `/personal/medico/mi-propuesta` se suma a
    `RutasYaConstruidas` (sigue esperando `200` + `<h1>Mi propuesta de agenda</h1>` para `medico1`,
    que no está vinculado: el AC 2 dibuja igual el `<h1>`, y `403` para los otros roles);
    `IActorVigente` suma un método (`ExigirActivoConRolAsync`) y `ExigirAdministradorActivoAsync` lo
    llama sin cambiar su firma ni su comportamiento. **Sigue en verde sin tocarlo** el caso `"duracion"`
    de `Base_ValoresFueraDeRango_LosRechazanLosControles` (4.1, `PersistenciaAgendasTests`): inserta un
    segundo `Borrador` para un médico que ya tiene uno, y no choca con el índice nuevo porque PostgreSQL
    revisa el `CHECK` antes que el índice único (M-8 de la revisión). Si alguna vez falla con `23505` en
    lugar del control, se corrige el armado de esa prueba, nunca el índice. El índice `ux_agendas_medico_una_enviada`, la tabla
    `registros_auditoria` sin `UPDATE` y las pantallas de la 3.2 no cambian. La cobertura de líneas de
    Dominio y Aplicacion sigue sobre el 80 % de la CI (NFR-11).

### Tests (47 verificaciones)

| # | Test | AC | Nivel | Escenario | Resultado esperado |
|---|------|----|-------|-----------|--------------------|
| T1 | `Revisar_FranjasValidas_CalculaLosTurnosConElDominio` | 10, 24 | unit | `FormularioAgenda` de 20 min con lunes 08:00–12:00 y miércoles 08:00–10:50 | Sin problemas; `TurnosPorSemana = 20`, igual a `AgendaMedico.TurnosPorSemana()` de la misma agenda armada con el dominio (el ejemplo de la 4.1 T28) |
| T2 | `Revisar_FilaIncompletaOHorasInvalidas_MarcaLaFilaConSuMensaje` | 9 | unit | `[Theory]`: fila sin `Desde`; fila sin `Hasta`; 12:00–08:00 | Problema en esa fila con `Completá la hora desde y la hora hasta, o quitá la franja.` (dos casos) / `La franja tiene que terminar después de empezar.`; `TurnosPorSemana = null` |
| T3 | `Revisar_FranjaSuperpuesta_MarcaLaQueChocaConElTextoDelDominio` | 9 | unit | Filas lunes 08:00–12:00 y lunes 10:00–14:00 | Fila 0 sin problema; fila 1 con `La franja del lunes de 10:00 a 14:00 se superpone con la de 08:00 a 12:00.`; `TurnosPorSemana = null` |
| T4 | `Revisar_FranjaQueNoEsMultiplo_AvisaLosMinutosQueNoSeUsan` | 9, 10 | unit | 20 min, lunes 08:00–10:50 | Aviso (no problema) en la fila: `Los últimos 10 minutos de esta franja no se usan: no entra un turno completo.`; `TurnosPorSemana = 8` |
| T5 | `Revisar_DuracionInvalidaOSinFranjas_NoDaElCalculo` | 10 | unit | `[Theory]`: duración 5 con una franja válida; duración vacía; 20 min sin filas | `TurnosPorSemana = null` en los tres; el tercero con `SinFranjas = true` |
| T6 | `AplicarAccionDeEdicion_SegunElBoton_CambiaLasFilasOLaDejaALaPagina` | 8, 24 | unit | 2 filas; `[Theory]` `Accion`: `agregar-franja`; `quitar-franja:1`; `actualizar`; `guardar`; `enviar`; `quitar-franja:9`; `quitar-franja:x` | `true` y 3 filas (la nueva `Monday`, sin horas) / `true` y queda la fila 0 / `true` sin cambios / `false` / `false` / `ReglaDeNegocioException` con `No reconocemos esa acción. Recargá la página.` (dos casos) |
| T7 | `DesdeYADatos_AgendaGuardada_IdaYVueltaConMedianoche` | 4, 5, 24 | unit | `FormularioAgenda.Desde(agenda)` de una agenda de 30 min desde 02/11/2026 con viernes 20:00–00:00 y lunes 14:00–18:00 cargadas en ese orden; después `ADatos()`. Y `Nueva(23/10/2026)` | Filas ordenadas lunes, viernes; `Hasta = 00:00`; `ADatos()` devuelve los mismos valores; `Nueva` trae 20 min, sin filas y esa fecha |
| T8 | `Validar_CamposFaltantesOFueraDeRango_DevuelveLosMensajes` | 11 | unit | `Validator.TryValidateObject(…, validateAllProperties: true)`; `[Theory]`: duración `null`; 61; `VigenteDesde` `null`; una fila sin `Hasta`; duración `null` y una fila sin `Hasta` a la vez | Los mensajes de la tabla del AC 11, con el nombre del campo (`DuracionTurnoMinutos`, `VigenteDesde`, `Franjas`); en el último caso, los dos |
| T9 | `EditorDeAgenda_ConFranjas_DibujaCamposConEtiquetaYBotones` | 7, 22, 24 | unit | bUnit: `EditorDeAgenda` dentro de un `EditForm` con 2 filas | Etiquetas `Duración del turno (minutos)`, `Rige desde`, `Día`, `Desde`, `Hasta`; un `Quitar` por fila con `name="Input.Accion"` y `value="quitar-franja:<i>"`; `+ Agregar franja` (`agregar-franja`) y `Actualizar el cálculo` (`actualizar`); el **primer** `button[type=submit]` del marcado tiene `value="actualizar"`; los botones de edición llevan `formnovalidate`; ningún campo tiene un nombre con `MedicoId` ni `AgendaId`; el texto `Para terminar a medianoche, poné 00:00.` |
| T10 | `EditorDeAgenda_FilaConProblema_LaMarcaConColorYTexto` | 9, 24 | unit | bUnit; filas lunes 08–12 y lunes 10–14; y otro caso con lunes 08:00–10:50 de 20 min | La segunda fila (`<tr>`) tiene la clase `table-danger` y contiene el texto del dominio; la primera no tiene la clase; el aviso de los 10 minutos aparece en su fila **sin** `table-danger` |
| T11 | `EditorDeAgenda_SegunLaRevision_MuestraElCalculoYLaFechaMinima` | 7, 10, 24 | unit | bUnit; `[Theory]`: válido con `TurnosPorSemanaAntes = 36`; válido sin antes; con superposición; sin filas; duración 5 sin filas. `MinimoVigenteDesde = 23/10/2026` | `Con esta agenda ofrecés 20 turnos por semana (antes: 36).` / `…por semana.` / `Corregí lo marcado para ver cuántos turnos ofrecés.` / `Agregá franjas para ver cuántos turnos ofrecés por semana.` / `Corregí lo marcado…`; en todos `(mín. 23/10/2026)` |
| T12 | `ResumenDeAgenda_EnLinea_MuestraDuracionFranjasYTurnosPorSemana` | 3, 24 | unit | bUnit; agenda de 20 min con viernes 20:00–00:00, lunes 08:00–12:00 y miércoles 08:30–12:00 cargadas en ese orden; y otra de 60 min con lunes 08:00–09:00 | `turnos de 20 min · Lun 8–12 · Mié 8:30–12 · Vie 20–24` y `34 turnos/semana` (12 + 10 + 12); la segunda, `1 turno/semana` |
| T13 | `ResumenDeAgenda_EnLista_UnaFranjaPorRenglonDeLunesADomingo` | 24 | unit | bUnit con `Forma = EnLista`; domingo 08–12 y lunes 08–12; y una agenda sin franjas | `<li>` en orden `Lun 8–12`, `Dom 8–12`; `TextoDeFranja` devuelve lo mismo; sin franjas: `Sin franjas` |
| T14 | `ComponentesCompartidos_Contrato_NombresParametrosYUbicacionFijos` | 24 | unit | Reflexión sobre `EditorDeAgenda`, `ResumenDeAgenda`, `FormularioAgenda`, `AgendaMedico` | Espacio de nombres `HospitalTurnos.Web.Components.Personal.Agendas`; propiedades `[Parameter]` exactamente las del AC 24 con su tipo y `[EditorRequired]` donde corresponde (en `ResumenDeAgenda`: `Agenda`, `Forma` y `CompararCon`); `FormularioAgenda` no es `sealed` y tiene `AplicarAccionDeEdicion`, `Revisar`, `ADatos`, `Desde` y `Nueva`; `AgendaMedico.TurnosPorSemana()` público, sin parámetros, devuelve `int` |
| T15 | `ExigirActivoConRolAsync_SegunElUsuarioEnLaBase_DevuelveElActorOLanza` | 21, 24 | integration | En `ActorVigenteIdentityTests`. `[Theory]` con usuarios propios reales: `ExigirActivoConRolAsync(…, Roles.Medico, …)` con médico activo; recepcionista; médico desactivado; administrador; médico con un segundo rol; un `Guid` que no existe. Y `ExigirAdministradorActivoAsync` (que ahora lo llama) con un administrador activo y con uno desactivado | Los dos que pasan (médico activo; administrador activo) devuelven el actor con `Quien` = nombre de la base, `Rol` = el pedido (`Medico` / `Administrador`) y `Canal = Personal`; los otros seis, `AccesoDenegadoException` con el mensaje pasado |
| T16 | `ObtenerAsync_SinVinculo_LoIndicaSinAgendas` | 2 | integration | Usuario médico propio sin médico vinculado | `SinVinculo = true`; `Vigente`, `ProximaAprobada`, `ParaCopiar` y `EnCurso` nulos |
| T17 | `ObtenerAsync_ConVariasAgendas_DevuelveVigenteProximaParaCopiarYEnCurso` | 3, 4, 5, 24 | integration | Reloj en 16/10/2026. Médico propio con A aprobada que rige hoy, B aprobada desde 30/11, R rechazada; otro médico propio con su borrador | `Vigente = A`, `ProximaAprobada = B`, `ParaCopiar = B`, `EnCurso = R` (con sus franjas), `MinimoVigenteDesde = 23/10/2026`; nada del otro médico |
| T18 | `GuardarBorradorAsync_SinPropuesta_CreaUnBorradorSinAuditar` | 12 | integration | Médico propio sin agendas; datos 20 min, 30/11, lunes 08–12 | `Creada`; una agenda `Borrador` del médico con esa franja; 0 registros de auditoría con `entidad_tipo = 'AgendaMedico'` y su id |
| T19 | `GuardarBorradorAsync_ConCambios_AplicaSoloLasDiferencias` | 13 | integration | Borrador con lunes 08–12 y miércoles 08–12; guardar 30 min, lunes 08–12 (igual), miércoles 14–18 (cambia) y viernes 08–12 (nueva) | `Guardada`; duración 30; 3 franjas; la del lunes conserva su `id` de `franjas_agenda` |
| T20 | `GuardarBorradorAsync_SinCambios_NoGuardaYLaRechazadaSigueRechazada` | 13 | integration | Rechazada; guardar exactamente sus datos (las franjas en otro orden) | `SinCambios`; sigue `Rechazada` con su motivo |
| T21 | `GuardarBorradorAsync_RechazadaCorregida_PasaABorradorConElMotivo` | 13 | integration | Rechazada con motivo `X`; guardar con otra duración | `Guardada`; `Borrador`; `MotivoRechazo = X` y `RechazadaEl` sin cambios |
| T22 | `GuardarBorradorAsync_DatosQueElDominioRechaza_LanzaSinGuardar` | 14 | integration | Borrador guardado; `[Theory]`: dos franjas superpuestas; duración 5; franja 12:00–08:00 | `ReglaDeNegocioException` con el mensaje del dominio; la fila de la base sin cambios |
| T23 | `EnviarAsync_BorradorValido_GuardaEnviaYAuditaJuntos` | 15, 17, 24 | integration | Reloj en 16/10/2026 12:00; borrador; enviar con `VigenteDesde` 23/10/2026 (el mínimo) y otra franja | `Enviada`, `EnviadaEl` = hora del reloj, la franja nueva guardada; 1 registro `propuesta-agenda.enviada` con los campos de la tabla del AC 17 (`quien` leído de la base, `cuando` del reloj, `detalle` con las franjas en el formato del AC 17 —`dia` numérico, `HH:mm`— y `turnosPorSemana`) |
| T24 | `EnviarAsync_SinPropuestaGuardada_CreaYEnviaEnUnPaso` | 15, 17 | integration | Médico propio sin agendas; enviar datos válidos | Una sola agenda, `Enviada`; el registro de auditoría con `entidad_id` = su id |
| T25 | `EnviarAsync_ReglaQueFalla_NoGuardaNadaNiAudita` | 16 | integration | Borrador de 20 min; `[Theory]`: `VigenteDesde` 22/10/2026 con duración 30; sin franjas; y una Rechazada enviada con sus mismos datos | `ReglaDeNegocioException` / `ReglaDeNegocioException` / `ConflictoException` con los textos de la tabla del AC 16; la agenda de la base sin cambios (duración 20, sigue `Borrador` o `Rechazada`); 0 registros |
| T26 | `EscriturasDePropuesta_YaHayUnaEnviada_LanzaConflictoSinTocarNada` | 18 | integration | Médico propio con una `Enviada`; `[Theory]` (`GuardarBorradorAsync`, `EnviarAsync`) con datos válidos | `ConflictoException` con el texto del AC 18; la enviada intacta; ninguna agenda nueva del médico |
| T27 | `EnviarAsync_OtraEnviadaSeAdelanta_TraduceElIndiceAConflicto` | 19, 24 | integration | Borrador guardado; `InterceptorQueSeAdelanta` de la 3.1 con un `INSERT` de una agenda `Enviada` del mismo médico | `ConflictoException` con el texto del AC 18; el borrador sigue `Borrador`; una sola `Enviada` (la del interceptor); 0 registros de auditoría |
| T28 | `GuardarBorradorAsync_OtroBorradorSeAdelanta_TraduceElIndiceAConflicto` | 19 | integration | Médico sin agendas; interceptor con un `INSERT` de un `Borrador` del mismo médico | `ConflictoException` con `Ya tenés una propuesta en curso: volvé a abrir la pantalla para seguir con ésa.`; un solo borrador (el del interceptor) |
| T29 | `GuardarBorradorAsync_LaPropuestaCambioMientrasTanto_TraduceLaConcurrenciaAConflicto` | 19 | integration | Borrador guardado; interceptor con `UPDATE agendas_medico SET duracion_turno_minutos = 45 WHERE id = <id>`; `[Theory]`: el guardado cambia la duración; el guardado **sólo** agrega una franja | `ConflictoException` con el texto de concurrencia del AC 19 en los dos; en la base duración 45 y sin la franja nueva |
| T30 | `EscriturasDePropuesta_ActorNoMedicoActivoOSinVinculo_LanzaAccesoDenegadoSinGuardar` | 21 | integration | `[Theory]` (`GuardarBorradorAsync`, `EnviarAsync`) × (recepcionista propio; médico propio desactivado con su vínculo; médico propio activo sin vínculo): 6 casos con datos válidos | `AccesoDenegadoException` con `Sólo un médico activo…` (los dos primeros actores) / el texto del AC 2 (el tercero); ninguna agenda nueva; 0 registros |
| T31 | `EscriturasDePropuesta_SoloTocanLaDelMedicoDeLaSesion` | 22 | integration | Médicos propios M1 y M2, cada uno con su borrador; M1 guarda y después envía | El de M1 cambia y queda `Enviada`; el de M2, idéntico (duración, franjas, estado) |
| T32 | `Migracion_UnaPropuestaEditable_CreaElIndiceYConservaLosDeLa41` | 19, 24, 25 | integration | `pg_indexes` con `CadenaDuenio`; después, con `CadenaApp`, dos `INSERT` a mano de un `Borrador` y una `Rechazada` del mismo médico | `ux_agendas_medico_una_editable` único con `WHERE` sobre `Borrador` y `Rechazada`; `ux_agendas_medico_una_enviada` sigue igual; el segundo `INSERT` falla con `23505` y ese nombre; `registros_auditoria` sigue sin `UPDATE` para `turnos_app` |
| T33 | `GetMiPropuesta_SinVinculo_MuestraElAvisoSinFormulario` | 1, 2, 21 | integration | Usuario médico propio sin vínculo; GET | `200`; `<h1>Mi propuesta de agenda</h1>`; el texto del AC 2; no hay `<form` con `_handler` `propuesta`; no contiene `Esta sección se habilita`; un POST `guardar` igual (armado a mano) → `403` con el texto del AC 2 en `role="alert"`, no un `400` en blanco |
| T34 | `GetMiPropuesta_SinAgendas_OfreceUnaPropuestaNuevaVacia` | 1, 3, 4, 7 | integration | Médico propio vinculado, sin agendas; GET | `Todavía no tenés una agenda vigente.`; `Nueva propuesta`; duración `20`; `Rige desde` = hoy + 7 (de `app.Reloj`) y `(mín. dd/MM/yyyy)`; ninguna fila; botones `Guardar borrador` y `Enviar para aprobación`; 0 agendas del médico en la base |
| T35 | `GetMiPropuesta_ConVigente_LaMuestraArribaYCopiaSusFranjas` | 3, 4, 10 | integration | Médico con una aprobada que rige hoy (20 min, lunes 08–12); GET | `Mi agenda vigente:` con `turnos de 20 min · Lun 8–12` y `12 turnos/semana`; el formulario trae `Input.Franjas[0].Desde` = `08:00` (verifica el `name` real que dibuja Blazor) y duración 20; `ofrecés 12 turnos por semana (antes: 12).` |
| T36 | `PostMiPropuesta_GuardarBorrador_RedirigeYAnuncia` | 12, 13 | integration | `[Theory]`: médico sin agendas; POST `guardar` con 20 min, hoy + 30 y lunes 08–12; y el mismo POST sin ninguna fila (el navegador no manda `Input.Franjas`) | `302` a `/personal/medico/mi-propuesta?resultado=borrador`; la página dice `Borrador guardado.` en `role="status"` y `Estado: Borrador`; en la base, un borrador con esa franja; el mismo POST otra vez → `302` a `?resultado=sin-cambios` con `No había cambios para guardar.`; un `?resultado=` desconocido no muestra nada |
| T37 | `PostMiPropuesta_BotonesDeEdicion_CambianLasFilasSinGuardar` | 8, 9 | integration | Médico sin agendas; `[Theory]` POST: `agregar-franja` con 1 fila; `agregar-franja` sin ninguna fila; `quitar-franja:0` con 2 filas; `actualizar` con lunes 08–12 y lunes 10–14 | `200`; 2 filas / 1 fila / 1 fila (la segunda) / la segunda fila con el texto de superposición; en todos, 0 agendas del médico en la base |
| T38 | `PostMiPropuesta_Enviar_RedirigeYQuedaEnSoloLectura` | 6, 15 | integration | Borrador guardado; POST `enviar` con `Rige desde` = hoy + 7 | `302` a `?resultado=enviada`; la página: `Propuesta enviada para aprobación.`, `Estado: Enviada`, `Esperando aprobación desde el <hoy>.`, `Rige desde el <hoy+7>.`, el resumen; sin `<input` del editor ni botón `Enviar para aprobación` |
| T39 | `PostMiPropuesta_ReglaQueFalla_RespondeConElCodigoYElCartel` | 14, 16, 18 | integration | `[Theory]` POST `enviar` o `guardar`: hoy + 6 → `400`; dos franjas superpuestas → `400`; una `Rechazada` reenviada sin cambios → `409`; la página abierta y, antes del POST, una `Enviada` guardada para el médico desde `app.Services` → `409` | Código esperado; `role="alert"` con el texto del AC; la duración escrita sigue en el campo; la base sin cambios |
| T40 | `PostMiPropuesta_CamposInvalidos_MuestraMensajesSinGuardar` | 11 | integration | `[Theory]` POST `guardar`: duración vacía; duración 61; `Rige desde` vacía; una fila sin `Hasta` | `200`; el mensaje del AC 11 que corresponde; sin `role="alert"`; 0 agendas del médico |
| T41 | `GetMiPropuesta_RechazadaYCorregida_MuestraEstadoMotivoYFecha` | 5, 13, 24 | integration | Rechazada con motivo `El consultorio 3 está ocupado.`; GET; después POST `guardar` con otra duración y GET | Primero `Estado: Rechazada`, `Motivo del rechazo: «El consultorio 3 está ocupado.»` y `Rechazada el dd/MM/yyyy a las HH:mm.` (de `RechazadaEl` en hora de Córdoba); después `Estado: Borrador` y el mismo motivo y fecha |
| T42 | `GetMiPropuesta_AprobadaQueTodaviaNoRige_LoAvisa` | 3 | integration | Médico con una aprobada desde hoy + 30 y ninguna que rija hoy | `Todavía no tenés una agenda vigente.` y `Tu agenda nueva ya está aprobada y rige desde el <hoy+30>.` con su resumen; el formulario de `Nueva propuesta` copia sus franjas |
| T43 | `RutasDePropuesta_SinRolMedicoOSinSesion_NoEntran` | 20 | integration | `[Theory]`: `admin1` y `recepcion1` siguiendo redirecciones; sin sesión | `403` y `No tenés permiso para entrar a esta sección.`; sin sesión, `302` a `/personal/ingreso?ReturnUrl=%2Fpersonal%2Fmedico%2Fmi-propuesta` |
| T44 | `PostMiPropuesta_MedicoDesactivadoConSesion_Responde403ConCartel` | 21 | integration | Médico propio ingresa; GET (toma el token); se lo desactiva en la base (`Activo = false`); POST `guardar` | `403`; `role="alert"` con `Sólo un médico activo puede armar o enviar su propuesta de agenda.`; 0 agendas del médico |
| T45 | `PostMiPropuesta_SinTokenAntiforgery_Responde400SinGuardar` | 23 | integration | POST `guardar` válido sin `__RequestVerificationToken` | `400`; 0 agendas del médico |
| T46 | `EditorDeAgenda_ModeloHeredado_DibujaLosMismosNombresDeCampo` | 24 | unit | bUnit: `EditorDeAgenda` dentro de un `EditForm` cuyo modelo es una clase de la prueba `FormularioQueHereda : FormularioAgenda` con una propiedad más (`Motivo`) y 1 fila; después `AplicarAccionDeEdicion()` sobre ese modelo con `Accion = agregar-franja` | Los campos y botones con los mismos `name` que en T9 (`Input.DuracionTurnoMinutos`, `Input.VigenteDesde`, `Input.Franjas[0].Dia`/`Desde`/`Hasta`, `Input.Accion`); el editor no dibuja nada de `Motivo`; `AplicarAccionDeEdicion` devuelve `true` y quedan 2 filas (el uso de la 4.4, C-1 de la revisión) |
| T47 | `ResumenDeAgenda_ConCompararCon_MarcaLoQueCambia` | 24 | unit | bUnit con `Forma = EnLista`; `Agenda` de 20 min con martes 08–12 y jueves 14–18 y `[Theory]` de `CompararCon`: 20 min con martes 14–18 y jueves 14–18; 30 min con martes 08–12 y jueves 14–18; `null` | `← cambia` sólo en la `<li>` del martes / sólo en el renglón `turnos de 20 min` / ningún `← cambia`; en los tres, las dos `<li>` de `Agenda` y ninguna franja que esté sólo en `CompararCon` |

Todas llevan `[Trait("Story", "4.2")]` y un `[Trait("AC", "n")]` por AC de su columna.
T1–T14 van en `HospitalTurnos.Web.Tests` sin base (T1–T8 C# puro sobre `FormularioAgenda`; T9–T14
con bUnit o reflexión), y también T46 y T47 (bUnit, en `ComponentesDeAgendaTests`; las sumó la
revisión al final para no renumerar). T15–T32 en `HospitalTurnos.Aplicacion.Tests` contra PostgreSQL
real (T15 en `Personal/ActorVigenteIdentityTests.cs`). T33–T45 en `HospitalTurnos.Web.Tests` contra la
aplicación y PostgreSQL reales. La T19 de la 2.1 se
**modifica** (AC 25) y no cuenta acá.

> El plan estimaba 9 pruebas; quedan 47. La story junta tres cosas con sus bordes: un editor con
> revisión por fila y cálculo (T1–T14, T46 y T47, sin base, corren en milisegundos), un caso de uso con dos
> defensas contra carreras y una de concurrencia (T15–T32) y una pantalla con cuatro estados, errores
> con código, permisos y antiforgery (T33–T45). Sumar la pasada del tester no técnico (B-6).

**Por qué no hay E2E (regla 6.1 del formato).** El proyecto `tests/HospitalTurnos.E2E/` lo crea la
10.1. M2 es SSR sin JavaScript propio: lo que hace el navegador (GET, POST con token, botones con
`Input.Accion`, redirecciones) lo reproducen T33–T45 con `HttpClient` sobre la aplicación real; lo que
depende sólo de los datos al dibujar lo prueba bUnit (T9–T13, T46 y T47). La prueba en el navegador queda en
"Cómo verificar" con su resultado en el Dev Agent Record.

#### Given/Fixture

- **Reloj**: toda regla de tiempo con `FakeTimeProvider` (A-4). En Aplicacion.Tests,
  `DatosDeAgenda.Reloj(2026, 10, 16)` (viernes, 12:00 de Córdoba; el mínimo es el 23/10/2026),
  registrado con `AddSingleton<TimeProvider>(reloj)` en los `reemplazos` de `DatosDeAgenda.Proveedor`
  (lo último registrado gana). En Web.Tests, "hoy" es la fecha local de `app.Reloj` de la
  `PersonalWebApplicationFactory` (arranca en la hora real y no avanza solo); nunca se adelanta
  (cortaría la sesión). Las agendas que tienen que "regir hoy" se envían y aprueban con otro
  `FakeTimeProvider` corrido 30 días hacia atrás, igual que la T44 de la 4.1. Ninguna prueba espera
  tiempo real.
- **Base compartida**: nada se limpia; cada prueba crea **su** médico y filtra por su id o por la marca
  `Guid.NewGuid().ToString("N")[..8]`. Auditoría: se cuenta sólo
  `WHERE entidad_tipo = 'AgendaMedico' AND entidad_id = '<id propio>'`.
- **Aplicacion.Tests — `DatosDePropuesta`** (extensiones nuevas, reusan las de la 4.1 y la 3.2, sin
  copiarlas; **esta story es su dueña**, la 4.3 le suma sus ayudas, I-5 de la revisión):
  `MedicoConUsuarioAsync(this ServiceProvider proveedor, bool activo = true, string? email = null)` →
  `(long MedicoId, ActorAuditoria Actor, Guid UsuarioId)`: usuario con `UsuariosDePrueba.CrearAsync(proveedor, [Roles.Medico], activo)`
  y, si `email` no es `null`, el email fijado con `UserManager.SetEmailAsync` (lo usa la 4.3 para el
  aviso al médico); especialidad con `DatosDeAgenda.EspecialidadPropiaAsync`, médico con
  `DatosDeCatalogo.CrearMedicoAsync(…, usuarioPersonalId: usuario.Id)` y actor con
  `UsuariosDePrueba.ActorDe(usuario, Roles.Medico)`. Las agendas del "Given" con
  `fixture.GuardarAgendaAsync` (4.1), que llega a cada estado con los métodos públicos.
  `IPropuestaAgenda` se resuelve **del contenedor**, un alcance por operación (como un pedido web).
- **Carreras (T27–T29)**: el interceptor es `InterceptorQueSeAdelanta(fixture.CadenaApp, sql)` de la
  3.1, agregado con `servicios.ConfigureDbContext<HospitalDbContext>(o => o.AddInterceptors(interceptor))`.
  Se dispara en el **primer** `SaveChanges` de su contenedor: por eso el "Given" (médico, agendas) se
  arma con un contenedor **sin** interceptor y la acción se ejecuta con un segundo contenedor que lo
  tiene. SQL: T27
  `INSERT INTO agendas_medico (medico_id, duracion_turno_minutos, vigente_desde, estado, enviada_el) VALUES (<id>, 20, '2026-12-01', 'Enviada', now())`;
  T28 igual con `'Borrador'` y `enviada_el` nulo; T29 el `UPDATE` de la tabla. `turnos_app` tiene
  `INSERT` y `UPDATE` sobre `agendas_medico` (4.1 AC 24).
- **T15**: archivo nuevo `Aplicacion.Tests/Personal/ActorVigenteIdentityTests.cs` (hasta hoy
  `ActorVigenteIdentity` no tiene pruebas propias; la 5.1 le suma la suya). Usuarios propios con
  `UsuariosDePrueba.CrearAsync` (roles y `activo` del caso); el "segundo rol" con
  `[Roles.Medico, Roles.Recepcionista]`. `IActorVigente` resuelto del contenedor.
- **T32**: como la T33 de la 4.1: `pg_indexes` (`indexname`, `indexdef`) con `CadenaDuenio`; los
  `INSERT` a mano con `NpgsqlCommand` y `CadenaApp`, sobre un médico propio.
- **Web.Tests — `DatosDePropuesta`** (extensiones sobre `PersonalWebApplicationFactory`; esta story es
  su dueña, la 4.3 le suma `VincularMedicoConEmailAsync` y la 4.4 la reusa):
  `MedicoIngresadoAsync(app, bool vinculado = true)` → `(HttpClient Cliente, long? MedicoId, UsuarioPersonal Usuario)`:
  usuario con `FormularioHttp.CrearUsuarioAsync(app, [Roles.Medico], clave)`, médico con
  `app.CrearMedicoAsync(…, usuario.Id)` de `DatosDeCatalogo` (3.2), ingreso con
  `FormularioHttp.IngresarAsync`. `GuardarAgendaAsync(app, AgendaMedico agenda)` guarda con el
  `HospitalDbContext` de `app.Services.CreateScope()`. `RelojCorridoDias(FakeTimeProvider base_, int dias)`:
  el mismo nombre y la misma idea que la ayuda privada de `EstadoDeAgendaEnA1HttpTests` (4.1), que no
  se toca; ésta es pública para las stories de agendas (M-9 de la revisión).
  `medico1`, `admin1` y `recepcion1` sólo ingresan y navegan (T43): `medico1` no está vinculado.
- **Cómo se arma el POST de M2**: `FormularioHttp.EnviarAsync(cliente, ruta, "propuesta", campos, conToken)`
  de la 2.2, con `Input.DuracionTurnoMinutos`, `Input.VigenteDesde` (`yyyy-MM-dd`),
  `Input.Franjas[i].Dia` (`Monday`…), `Input.Franjas[i].Desde` / `Hasta` (`HH:mm`) e `Input.Accion`.
  Textos con `WebUtility.HtmlDecode`; carteles con `FormularioHttp.Alerta` / `Estado`; destinos con
  `FormularioHttp.Destino`.
- **T39, caso "ya hay una enviada"**: GET de la página (toma el token y ve el formulario), después se
  guarda una `Enviada` para ese médico desde `app.Services`, y recién entonces el POST: es la pestaña
  vieja del AC 18.
- **bUnit (T9–T13, T46, T47)**: `BunitContext`, como `TablaMedicosTests` (3.2). T46 declara su clase
  `FormularioQueHereda : FormularioAgenda` dentro del archivo de pruebas. `EditorDeAgenda` se dibuja dentro
  de un `EditForm` con su `FormularioAgenda` (necesita el `EditContext` en cascada). `ResumenDeAgenda`
  recibe una `AgendaMedico` armada con los métodos públicos del dominio.

## Tasks / Subtasks

- [x] [SEQ] Task 1: Salvaguarda de estado (AC: todos) — revalidar contra `main` el día que se implementa
  - [x] Confirmar lo que esta story usa de la 4.1, con su firma real: `AgendaMedico` (constructor,
        `CambiarDuracion`, `CambiarVigenteDesde`, `AgregarFranja`, `QuitarFranja(dia, desde)`,
        `Enviar(reloj)`, `TurnosPorSemana()`, `QueRigeEn`, `DiasMinimosDeAnticipacion`), `Franja` (y
        que `NombreDelDia` y `Tramo` son `internal`: la Web no los usa), `IConsultaAgendas`,
        `AgendaMedicoConfiguracion.IndiceUnaEnviada`, `DbSet AgendasMedico`, `DatosDeAgenda`.
  - [x] De la 2.2/3.2: `IActorVigente` con un solo método (`ExigirAdministradorActivoAsync`) y una sola
        implementación (`ActorVigenteIdentity`, sin dobles de prueba que la implementen),
        `ActorDeSesion.Desde`, `ManejadorGlobalDeErrores.EsEsperable`/`FijarEstadoEnPagina`,
        `FormularioHttp.EnviarAsync`, `RutasYaConstruidas`, `InterceptorQueSeAdelanta`,
        `Medico.UsuarioPersonalId` con índice único.
  - [x] Que `SeccionesMedico.razor` tiene `@page "/personal/medico/mi-propuesta"` y que
        `RutasPersonal.MiPropuesta` y la entrada del menú existen.
  - [x] Que la 2.3 (ya en `main`) no cambió `IActorVigente` ni `ActorVigenteIdentity`, y que
        no existe todavía `ExigirActivoConRolAsync` (lo crea esta story; la 5.1 lo reusa).
  - [x] `bash scripts/ci/local.sh` en verde. Si algo no coincide → DETENERSE y corregir la story.
- [x] [SEQ] Task 2: Permiso del médico en la base (AC: 21)
  - [x] `ExigirActivoConRolAsync` **público** en `IActorVigente` y `ActorVigenteIdentity` (el cuerpo
        actual del método del administrador, con el rol de parámetro); `ExigirAdministradorActivoAsync`
        pasa a llamarlo. T15 en rojo y después en verde; las de 2.2, 3.1 y 3.2 siguen en verde
        (`dotnet test --filter "Story=2.2|Story=3.1|Story=3.2"`).
- [x] [SEQ] Task 3: Índice y migración (AC: 19)
  - [x] Salvaguarda de BD, paso 1 (antes de generar la migración).
  - [x] `AgendaMedicoConfiguracion`: constante `IndiceUnaEditable` y el índice con **nombre propio**
        (ver Implementación concreta: sin nombre, EF reusaría el índice de la 4.1).
  - [x] `dotnet ef migrations add UnaPropuestaEditablePorMedico`; leer el código generado: un solo
        `CreateIndex`, con el filtro; ningún `DropIndex` ni columna nueva.
- [x] [SEQ] Task 4: Caso de uso (AC: 2–6, 12–19, 21, 22)
  - [x] `DatosAgenda`/`DatosFranja` e `IPropuestaAgenda` (con `VistaPropuesta` y `ResultadoGuardado`)
        en Aplicacion/Agendas.
  - [x] `PropuestaAgendaEnBase` en Infraestructura/Agendas, registrado `Scoped` en
        `AgregarInfraestructura`.
  - [x] `DatosDePropuesta` (Aplicacion.Tests) y T16–T32 en verde. Validar T27–T29 rompiendo el código
        (sin la traducción tienen que fallar siempre, L-2).
- [x] [SEQ] Task 5: Formulario y componentes compartidos (AC: 7–11, 24)
  - [x] `FormularioAgenda` (con `FilaFranja` y `RevisionDeAgenda`) y `FormaDeResumen`; T1–T8.
  - [x] `EditorDeAgenda` y `ResumenDeAgenda` (con `CompararCon`); T9–T14, T46 y T47.
- [x] [SEQ] Task 6: Pantalla M2 (AC: 1–16, 18, 20–23, 25)
  - [x] `MiPropuesta.razor` en `Components/Personal/Agendas/`; quitar el `@page` de
        `SeccionesMedico.razor`; `_Imports.razor` con `@using HospitalTurnos.Aplicacion.Agendas` y
        `@using HospitalTurnos.Web.Components.Personal.Agendas`.
  - [x] `/personal/medico/mi-propuesta` en `RutasYaConstruidas` (T19 de la 2.1).
  - [x] `DatosDePropuesta` (Web.Tests) y T33–T45 en verde.
- [x] [SEQ] Task 7: Verificación y cierre (AC: todos)
  - [x] `dotnet test` completo y `bash scripts/ci/local.sh` en verde; cobertura en el Dev Agent Record.
  - [x] Salvaguarda de BD, pasos 2 a 4, contra Compose.
  - [x] Prueba en el navegador (Cómo verificar, paso 5), con scripts que buscan por texto o rol, nunca
        por clases CSS, y que cuentan con que el menú repite el enlace (L-14).
  - [x] Guía `docs/qa/4.2-pasos.md` y **pasada del tester no técnico** sobre la guía y sobre las
        capturas de M2 (B-6), con sus marcas en `docs/qa/4.2-legibilidad.md` y
        `docs/qa/4.2-legibilidad-ui.md`, como en la 3.2.

### Task Dependencies

T1 → T2 → T3 → T4 → T6 → T7. La T5 no necesita base ni caso de uso (sólo el dominio de la 4.1): se
puede hacer en paralelo con T2–T4, y tiene que estar antes de la T6.

## Dev Notes

### Qué existe hoy

#### En hospital-turnos (verificado 2026-10-17 contra `main` en `121ff7c`, con 1.1 a 4.1 y la 2.3 en done)

La 2.3 no tocó `IActorVigente` ni `ActorVigenteIdentity`; sumó `ICambioContrasenaPersonal`, registrado en
`RegistroIdentidad` junto a `IActorVigente`. Nada de lo que sigue cambió con ella.

- **Dominio de agenda (4.1)**, en `src/HospitalTurnos.Dominio/Agendas/`: `AgendaMedico` con
  `DuracionMinima`/`DuracionMaxima` (10/60), `DiasMinimosDeAnticipacion` (7), los cuatro métodos de
  edición (cada uno: estado → validar → cambiar → corregir la rechazada), `Enviar(TimeProvider)`,
  `Aprobar`, `Rechazar`, `HorariosDelDia`, `AtiendeEn`, **`TurnosPorSemana()`**
  (`_franjas.Sum(f => f.DuracionMinutos / DuracionTurnoMinutos)`) y `QueRigeEn`. `Franja` con
  `DuracionMinutos`, `TerminaAMedianoche` y `SeSuperponeCon` públicos; `NombreDelDia`, `Tramo`,
  `Inicio` y `Fin` son `internal`. `EstadoPropuesta { Borrador, Enviada, Aprobada, Rechazada }`.
  `EstadoDeAgenda.Calcular` y `SituacionDeAgenda`/`EstadoAgendaMedico` (3.2).
- **Persistencia de agendas (4.1)**: tablas `agendas_medico` y `franjas_agenda`; `turnos_app` con
  `SELECT, INSERT, UPDATE` en la primera y también `DELETE` en la segunda. Índice
  `ux_agendas_medico_una_enviada` (constante `AgendaMedicoConfiguracion.IndiceUnaEnviada`, con el
  comentario "la 4.2 traduce su 23505"), declarado con `HasIndex(a => a.MedicoId)` **sin nombre de
  modelo** y `HasDatabaseName`. Concurrencia con `xmin` (propiedad sombra `Version`). Última
  migración: `20261017025538_AgregarAgendas`.
- **`IConsultaAgendas`** (Aplicacion/Agendas) con `AgendaQueRigeAsync` y `EstadosDeAgendaAsync`;
  `ConsultaAgendasEnBase(HospitalDbContext, TimeProvider)`. Esta story no la modifica: su consulta
  de M2 trae todas las agendas del médico de una vez (ver Implementación concreta).
- **Auditoría (1.3)**: `IAuditoria.Registrar(actor, accion, entidadTipo, entidadId, detalle)` agrega
  sin guardar; `AccionesAuditoria.PropuestaAgendaEnviada = "propuesta-agenda.enviada"` existe.
  `AuditoriaEnBase` serializa con `JsonSerializerOptions.Web`.
- **Permiso en la base (2.2)**: `IActorVigente` (Aplicacion/Personal) con **un** método,
  `ExigirAdministradorActivoAsync(actor, mensajeSiNoLoEs, ct)`; implementado por
  `ActorVigenteIdentity(UserManager<UsuarioPersonal>)` y registrado `Scoped` en
  `RegistroIdentidad` (`AddIdentidadPersonal`). Ningún doble de prueba implementa la interfaz.
- **Médico de la sesión (3.2)**: `Medico.UsuarioPersonalId` (`Guid?`) con índice único
  `ix_medicos_usuario_personal_id`: a lo sumo un médico por usuario.
- **Web (2.1, 2.2, 3.1, 3.2)**: `RutasPersonal.MiPropuesta = "/personal/medico/mi-propuesta"` con
  título `Mi propuesta de agenda`, ya en el menú del rol `Medico` (segunda entrada, después de
  `Mi agenda`). `SeccionesMedico.razor` atiende `mi-agenda` y `mi-propuesta` con
  `SeccionEnConstruccion`. Todas las páginas del personal son SSR, sin `@rendermode` (la 3.2 lo dejó
  escrito: "la interactividad empieza en la 5.5"). Modelo de página: `FichaMedico.razor` (formulario
  con `EditForm`, `FormName`, `[SupplyParameterFromForm] Input`, `?resultado=` traducido con un
  `switch`, `FijarEstadoEnPagina`, `NavigateTo` fuera del `try`); campos compartidos dentro del
  formulario del padre: `CamposMedico.razor` (parámetro `Input`); un campo `Accion` con el valor del
  botón: `Especialidades.razor` y `UsuariosPersonal.razor`, con el texto
  `No reconocemos esa acción. Recargá la página.` `_Imports.razor` ya importa
  `HospitalTurnos.Dominio.Agendas` y `HospitalTurnos.Aplicacion.Catalogo`.
- **Pruebas**: `DatosDeAgenda` (Aplicacion.Tests/Agendas: `Reloj`, `Proveedor`, `EspecialidadPropiaAsync`,
  `MedicoPropioAsync`, `GuardarAgendaAsync`, `ArmarAgenda`, `Franja`; ojo: `Franja(string dia, …)` sólo
  conoce `lunes`, `martes` y `miércoles` y con otro día lanza `ArgumentOutOfRangeException`: esta story
  le suma los otros cuatro días —`jueves`, `viernes`, `sábado`, `domingo`— sin cambiar los que tiene, porque
  T19 usa el viernes), `InterceptorQueSeAdelanta`
  (`(string cadena, string sqlQueInserta)`, ejecuta cualquier SQL una vez), `UsuariosDePrueba`
  (`CrearAsync(proveedor, roles, activo, nombre)`, `ActorDe`), `DatosDeCatalogo` en los dos proyectos,
  `PersonalWebApplicationFactory` (`Reloj`, usuarios fijos `medico1`/`admin1`/`recepcion1`),
  `FormularioHttp`, `EstadoDeAgendaEnA1HttpTests` (relojes corridos), `TablaMedicosTests` (bUnit).
  La T19 de la 2.1 tiene `RutasYaConstruidas` con usuarios, especialidades y médicos.

#### En el framework (.NET 10)

- Un formulario SSR mapea listas con índice: `Input.Franjas[0].Desde` llena `Input.Franjas[0]`.
  Los componentes `Input*` dibujan su `name` a partir de la expresión enlazada; dentro de un `for`, con
  una copia local del índice (`var i = indice;` y `@bind-Value="Input.Franjas[i].Desde"`) sale
  `Input.Franjas[0].Desde`. **Verificarlo en el rojo** (T35 mira el `name` real); si no sale así, se
  pasa `name="Input.Franjas[@i].Desde"` explícito.
- `InputDate<TimeOnly>` con `Type="InputDateType.Time"` dibuja `<input type="time">` y lee `HH:mm`;
  `InputDate<DateOnly?>` lee `yyyy-MM-dd`; `InputSelect<DayOfWeek>` envía el nombre del enum.
- En un formulario HTML, Enter envía con el **primer** botón de envío del formulario: por eso el
  editor empieza con un botón oculto `actualizar` (AC 7). Los botones de edición llevan
  `formnovalidate` para que el navegador no los frene por una validación propia del HTML.
- EF Core: dos `HasIndex(a => a.MedicoId)` sin nombre devuelven **el mismo** índice (el segundo pisaría
  el filtro del primero). Un segundo índice sobre la misma columna se declara con nombre:
  `HasIndex(a => a.MedicoId, IndiceUnaEditable)`.
- Si un guardado sólo agrega o quita filas hijas, EF no actualiza la fila del padre y `xmin` no se
  controla. Marcar el padre como modificado (`contexto.Entry(agenda).State = EntityState.Modified`)
  hace un `UPDATE … WHERE xmin = <leído>` (AC 19).
- `DbUpdateException` con `PostgresException { SqlState: "23505", ConstraintName }` (como la 3.2).

### Patrón de referencia verificado

| Proyecto | Ancla | Qué se verificó y qué se toma |
|---|---|---|
| hospital-turnos (3.2) | `src/HospitalTurnos.Web/Components/Personal/Administracion/FichaMedico.razor` (`OnInitializedAsync`, `GuardarAsync`, `NoExiste`) | Página SSR: datos cargados en cada pedido, formulario dibujado sólo con el modelo listo (L-8), `catch (Exception e) when (EsEsperable(e))` + `FijarEstadoEnPagina`, `NavigateTo` fuera del `try`, `?resultado=` con textos fijos. M2 igual. |
| hospital-turnos (3.2) | `src/HospitalTurnos.Web/Components/Personal/Administracion/CamposMedico.razor` | Campos compartidos que se dibujan dentro del `EditForm` del padre, con parámetro `Input`. `EditorDeAgenda` igual (así la 4.4 suma su motivo en el mismo formulario). |
| hospital-turnos (3.1) | `src/HospitalTurnos.Web/Components/Personal/Administracion/Especialidades.razor` (`EjecutarAccionAsync`) | Un solo formulario; el botón pulsado viaja en `Accion`; acción desconocida → `ReglaDeNegocioException("No reconocemos esa acción. Recargá la página.")`. Se toma para `Input.Accion`. |
| hospital-turnos (3.2) | `src/HospitalTurnos.Infraestructura/Catalogo/GestionMedicosEnBase.cs` (`CrearAsync`, `GuardarTraduciendoCarrerasAsync`) | Permiso en la base primero; transacción explícita cuando la auditoría necesita el id recién creado; traducción del `23505` por `ConstraintName` (`_ => e` para lo desconocido). Se toma para el envío. |
| hospital-turnos (2.2) | `src/HospitalTurnos.Infraestructura/Identidad/ActorVigenteIdentity.cs` | `FindByIdAsync` + `Activo` + `GetRolesAsync is not [rol]` → `AccesoDenegadoException`; devuelve el actor con el nombre de la base. `ExigirActivoConRolAsync` es ese mismo cuerpo con el rol de parámetro. |
| hospital-turnos (3.2) | `tests/HospitalTurnos.Web.Tests/Catalogo/TablaMedicosTests.cs` | Componente de pantalla probado con bUnit sin servicios ni base. Se toma para T9–T13. |
| hospital-turnos (3.1) | `tests/HospitalTurnos.Aplicacion.Tests/Ayudas/InterceptorQueSeAdelanta.cs` | Doble de la carrera: otra conexión ejecuta un SQL justo antes del guardado. Se reusa para T27–T29 (también con un `UPDATE`, para la concurrencia). |
| changomas-testing-csharp (guía) | `src/ChangoMas.Dominio/Venta.cs` (`Items` y `VerificarQueSePuedaModificar`) | Las reglas viven en el agregado y se cambian sólo por sus métodos: el caso de uso aplica las diferencias llamando a `QuitarFranja`/`AgregarFranja`, nunca tocando `_franjas`. |

### Qué hay que hacer (la delta)

1. Un caso de uso (puerto + adaptador) con la lectura de M2, guardar borrador y enviar, con control
   previo, dos índices, concurrencia y auditoría del envío; el permiso del médico en la base.
2. Un índice único parcial nuevo (una editable por médico) con su migración.
3. Un formulario con revisión por fila y dos componentes compartidos (editor y resumen).
4. La pantalla M2, que reemplaza la sección en construcción.

### Decisiones de esta story (con su porqué)

- **Turnos por semana: se usa el de la 4.1** (`AgendaMedico.TurnosPorSemana()`, AC 19 de la 4.1, T28
  de la 4.1). El brief pedía el cálculo en el dominio para que A6 use el mismo: ya está. El editor
  arma una `AgendaMedico` transitoria con los métodos públicos (nunca se guarda) y le pregunta a ella.
- **La revisión por fila usa el propio dominio.** El editor crea cada `Franja` y la agrega a la agenda
  transitoria; si el dominio lanza, ese mensaje va a la fila. Así el texto rojo de la pantalla es
  exactamente el del cartel al guardar (4.1 AC 2 y 4), y no hay una segunda copia de la regla de
  superposición ni del formato de los días (que en `Franja` es `internal`).
- **Sólo se audita el envío.** El brief y la regla 3 del sprint fijan las constantes de agenda; no hay
  una para el borrador, y un borrador es trabajo del médico que nadie más ve ni usa.
- **Leer M2 no consulta el rol en la base; escribir, sí** (AC 21). Igual que los listados y fichas de
  la 3.2: leer la propia propuesta no cambia nada; cada escritura confirma rol y actividad, porque la
  sesión atrasa hasta 5 minutos (2.1 AC 25).
- **Un método general `ExigirActivoConRolAsync` en el servicio de la 2.2**, no un control nuevo en el
  caso de uso ni un método por rol: regla 1 del sprint ("el permiso confirmado en la base en cada
  cambio", reusar). La 5.1 necesita lo mismo para la recepción y lo reusa (I-3 de la revisión).
  `ExigirAdministradorActivoAsync` queda con su firma para no tocar las stories que ya lo usan.
- **El formulario no lleva ids** (AC 22). El caso de uso encuentra todo por el médico de la sesión
  (Arq. §7.7); una pestaña vieja choca con el control previo o con la concurrencia, nunca toca la
  agenda equivocada.
- **Todo cambio sobre una propuesta existente actualiza su fila** (AC 19): con dos pestañas abiertas,
  una que sólo agrega una franja podría colarse en una propuesta que la otra acaba de enviar. Cuesta
  una línea.
- **Sin cambios no se guarda, y la rechazada sigue rechazada** (AC 13). "Guardar" sin tocar nada no
  tiene que hacer que una rechazada parezca corregida.
- **Dos carteles distintos para la misma situación de base** (AC 18 y 19): el control previo y la
  traducción del `23505` de `ux_agendas_medico_una_enviada` dicen lo mismo, como la matrícula
  repetida de la 3.2. El médico no tiene por qué saber cuál de las dos defensas lo frenó.
- **Fechas `dd/MM/yyyy`**: los mensajes del dominio (4.1) ya usan ese formato y UX M2 dibuja fechas
  numéricas; con el año no se confunde con otra fecha cercana. Es la regla del sprint (M-2 de la
  revisión): con año en los textos que dicen "desde" o "el".
- **`CompararCon` en el resumen, no en la 4.3** (I-1 de la revisión): la marca `← cambia` es parte de
  cómo se dibuja un resumen, y T14 cierra los parámetros del componente. Sumarlo acá cuesta una prueba
  (T47) y evita que la 4.3 abra un componente cerrado.
- **Componentes en `Components/Personal/Agendas/`**, no en `Administracion/` (los usa el médico) ni
  en una carpeta `Medico/` (un espacio de nombres `…Personal.Medico` le ganaría al tipo `Medico` del
  dominio en las páginas de A1, que lo usan sin calificar).
- **`DatosAgenda` y no `DatosPropuesta`**: la 4.4 manda los mismos datos para su corrección.

### Decisiones tomadas por el docente (2026-10-17)

Numeradas como en `revision-sprint-3.md` ("Decisiones abiertas consolidadas"). Se aceptaron todas con
la opción recomendada; los AC y las pruebas ya las reflejan.

- **Decisión 2 — ¿Un médico puede tener más de un borrador a la vez?** (era D-1; AC 18, 19, T28,
  T32). **Elegido:** una sola propuesta editable (`Borrador` o `Rechazada`) por médico y, mientras haya
  una enviada, ninguna otra, con el índice único parcial `ux_agendas_medico_una_editable`. M2 trabaja
  siempre sobre "la" propuesta, como dibuja UX M2. Consecuencia en la 4.3: sus pruebas no arman dos
  editables del mismo médico (I-4 de la revisión).
- **Decisión 3 — ¿M2 es SSR o interactiva?** (era D-2; AC 7, 8, 10). **Elegido:** SSR, como el resto
  del personal. El cálculo y las marcas rojas se actualizan con cada botón (`+ Agregar franja`,
  `Quitar`, `Actualizar el cálculo`, Enter), no mientras se escribe. La pantalla de corrección de la
  4.4 hereda lo mismo (C-1 de la revisión).
- **Decisión 4 — ¿Con qué arranca una propuesta nueva?** (era D-3; AC 4, T17, T35, T42). **Elegido:**
  con la duración y las franjas de la última aprobada; sin aprobada, 20 minutos y sin franjas.
  `Rige desde` = el mínimo.
- **Decisión 5 — ¿Cómo es la "grilla" de franjas?** (era D-4; AC 7). **Elegido:** una tabla con una fila
  por franja (día, desde, hasta, quitar): se usa con teclado y lector de pantalla y funciona en SSR.
- **Decisión 6 — ¿"Enviar para aprobación" envía lo que está en pantalla?** (era D-5; AC 15, 16, T24,
  T25). **Elegido:** sí, guarda lo de la pantalla y envía en un solo paso; si algo falla, no se guarda
  nada y lo escrito sigue en pantalla.
- **Decisión 7 — Quién rechazó, en M2** (de la 4.3 y la revisión; AC 5, 24). **Elegido:** el nombre
  completo de la auditoría, en la misma línea: `Rechazada el dd/MM/yyyy a las HH:mm por Lucía Pérez.`
  Esta story fija el formato de la línea; el dato lo agrega la 4.3 (AC 24).

### Implementación concreta

**Aplicacion — `DatosAgenda.cs`** (lo reusa la 4.4):

```csharp
public sealed record DatosFranja(DayOfWeek Dia, TimeOnly Desde, TimeOnly Hasta);
public sealed record DatosAgenda(int DuracionTurnoMinutos, DateOnly VigenteDesde, IReadOnlyList<DatosFranja> Franjas);
```

**Aplicacion — `IPropuestaAgenda.cs`**:

```csharp
/// <summary>M2 (story 4.2): la propuesta del médico de la SESIÓN (Arq. §7.7). Nunca recibe un id de médico.</summary>
public interface IPropuestaAgenda
{
    Task<VistaPropuesta> ObtenerAsync(ActorAuditoria actor, CancellationToken ct);
    Task<ResultadoGuardado> GuardarBorradorAsync(DatosAgenda datos, ActorAuditoria actor, CancellationToken ct);
    /// <summary>Aplica los datos y envía, en un paso (decisión 6). Audita con PropuestaAgendaEnviada.</summary>
    Task EnviarAsync(DatosAgenda datos, ActorAuditoria actor, CancellationToken ct);
}

public enum ResultadoGuardado { Creada, Guardada, SinCambios }

/// <summary>Lo que M2 muestra. Las agendas, con sus franjas y sin seguimiento. La 4.3 agrega
/// string? RechazadaPor = null AL FINAL (contrato, AC 24; decisión 7).</summary>
public sealed record VistaPropuesta(
    bool SinVinculo,
    AgendaMedico? Vigente,          // QueRigeEn(hoy)
    AgendaMedico? ProximaAprobada,  // la aprobada más próxima que todavía no rige
    AgendaMedico? ParaCopiar,       // la última aprobada (decisión 4)
    AgendaMedico? EnCurso,          // Borrador, Rechazada o Enviada
    DateOnly MinimoVigenteDesde);   // hoy + 7
```

**Infraestructura — `PropuestaAgendaEnBase(HospitalDbContext contexto, IAuditoria auditoria, IActorVigente actorVigente, TimeProvider reloj)`**:

```csharp
private const string SinPermiso = "Sólo un médico activo puede armar o enviar su propuesta de agenda.";
private const string SinVinculo = "Tu usuario no está vinculado a un médico. Pedíselo al administrador.";
private const string YaHayEnviada = "Ya tenés una propuesta enviada esperando aprobación: hasta que el administrador la revise no podés modificarla ni armar otra.";
private const string YaHayEnCurso = "Ya tenés una propuesta en curso: volvé a abrir la pantalla para seguir con ésa.";
private const string CambioMientrasTanto = "Tu propuesta cambió mientras la guardabas: volvé a abrir la pantalla para ver cómo quedó.";

public async Task EnviarAsync(DatosAgenda datos, ActorAuditoria actor, CancellationToken ct)
{
    var (medicoId, actorDeLaBase) = await MedicoDeLaSesionAsync(actor, ct);   // AC 21: rol y actividad, después vínculo
    var (editable, hayEnviada) = await EnCursoAsync(medicoId, ct);           // con Include(Franjas), con seguimiento
    if (hayEnviada) throw new ConflictoException(YaHayEnviada);               // AC 18, control previo
    var agenda = editable ?? Nueva(medicoId, datos);                          // AC 15: crea si no hay
    if (editable is not null) AplicarDiferencias(editable, datos);            // AC 13
    agenda.Enviar(reloj);                                                     // 4.1 AC 9 (estado primero, después datos)

    // El id de una agenda nueva nace al guardar y la auditoría lo necesita: transacción, como CrearAsync de la 3.2.
    await using var transaccion = await contexto.Database.BeginTransactionAsync(ct);
    await GuardarTraduciendoAsync(agenda, ct);
    auditoria.Registrar(actorDeLaBase, AccionesAuditoria.PropuestaAgendaEnviada, "AgendaMedico", agenda.Id.ToString(),
        DetalleDe(agenda));                                                   // AC 17
    await contexto.SaveChangesAsync(ct);
    await transaccion.CommitAsync(ct);
}

// AC 19: las tres carreras se traducen a la regla; un índice desconocido sigue como error (500 genérico).
private async Task GuardarTraduciendoAsync(AgendaMedico agenda, CancellationToken ct)
{
    if (agenda.Id != 0) contexto.Entry(agenda).State = EntityState.Modified;  // xmin también si sólo cambian franjas
    try { await contexto.SaveChangesAsync(ct); }
    catch (DbUpdateConcurrencyException) { throw new ConflictoException(CambioMientrasTanto); }
    catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
    {
        throw pg.ConstraintName switch
        {
            AgendaMedicoConfiguracion.IndiceUnaEnviada => new ConflictoException(YaHayEnviada),
            AgendaMedicoConfiguracion.IndiceUnaEditable => new ConflictoException(YaHayEnCurso),
            _ => e,
        };
    }
}
```

Ojo con `Entry(agenda).State = Modified` sobre una agenda con franjas nuevas: tiene que marcar sólo
la fila del padre (las franjas agregadas siguen `Added` y las quitadas, `Deleted`). Si EF las
reclasifica, usar `contexto.Entry(agenda).Property("Version").IsModified = false` + marcar una propiedad
propia como modificada (`Property(a => a.DuracionTurnoMinutos).IsModified = true`): el `UPDATE` sale
igual con `WHERE xmin = …`. T29 (caso "sólo agrega una franja") decide cuál anda.

- `GuardarBorradorAsync`: mismo comienzo (permiso, vínculo, control previo); sin editable →
  `Nueva(medicoId, datos)`, `Add`, un `SaveChanges` traducido → `Creada`; con editable →
  `AplicarDiferencias`; si no hubo ninguna → `SinCambios` sin guardar; si hubo → guardar traducido →
  `Guardada`. Sin auditoría.
- `AplicarDiferencias(agenda, datos)`: devuelve si cambió algo. Primero **todas** las `QuitarFranja`
  de las existentes que no están igual (día, desde **y** hasta) en `datos`; después las
  `AgregarFranja` de las que faltan, en el orden de `datos` (así una franja que cambió de hora no
  choca con su versión vieja); después `CambiarDuracion` y `CambiarVigenteDesde` sólo si difieren. Si
  el dominio lanza a mitad de camino, la entidad queda cambiada en memoria pero nunca se guarda (el
  contexto es del pedido), como en la 3.2.
- `Nueva(medicoId, datos)`: `new AgendaMedico(medicoId, datos.DuracionTurnoMinutos, datos.VigenteDesde)`
  y un `AgregarFranja(new Franja(...))` por fila.
- `MedicoDeLaSesionAsync`: `actorVigente.ExigirActivoConRolAsync(actor, Roles.Medico, SinPermiso, ct)`; después
  `contexto.Medicos.Where(m => m.UsuarioPersonalId == <Guid del actor>).Select(m => m.Id)`; ninguno →
  `AccesoDenegadoException(SinVinculo)`.
- `ObtenerAsync`: sin médico vinculado → `new VistaPropuesta(true, null, null, null, null, minimo)`.
  Si no, **una** consulta `AsNoTracking().Include(a => a.Franjas).Where(a => a.MedicoId == id)` (un
  médico tiene pocas agendas: unas por año) y, en memoria: `Vigente = AgendaMedico.QueRigeEn(agendas, hoy)`;
  `ProximaAprobada` = aprobada con `VigenteDesde > hoy` y la menor fecha; `ParaCopiar` = aprobada con
  la mayor `VigenteDesde`, desempate por `AprobadaEl`; `EnCurso` = la `Enviada` si hay, si no la
  editable. Hoy = `DateOnly.FromDateTime(reloj.GetLocalNow().DateTime)`.
- `DetalleDe(agenda)`: `new { medicoId, duracionTurnoMinutos, vigenteDesde = "yyyy-MM-dd", franjas = [...] { dia = (int)f.Dia, desde = "HH:mm", hasta = "HH:mm" }, turnosPorSemana = agenda.TurnosPorSemana() }`,
  con los textos armados con `CultureInfo.InvariantCulture` (no se deja a la serialización de
  `DateOnly`/`TimeOnly`).

**Aplicacion/Infraestructura — `IActorVigente.ExigirActivoConRolAsync`** (AC 21; el método de permiso
por rol del sprint, que la 5.1 reusa):

```csharp
// IActorVigente: se agrega (público, en la interfaz)
/// <summary>Confirma en la BASE que el actor existe, está activo y su ÚNICO rol es `rol`. Si no,
/// AccesoDenegadoException(mensajeSiNoLoEs). Devuelve el actor con el nombre de la base, ese rol y canal Personal.</summary>
Task<ActorAuditoria> ExigirActivoConRolAsync(ActorAuditoria actor, string rol, string mensajeSiNoLoEs, CancellationToken ct);

// ActorVigenteIdentity
public async Task<ActorAuditoria> ExigirActivoConRolAsync(ActorAuditoria actor, string rol, string mensajeSiNoLoEs, CancellationToken ct)
{
    var usuario = Guid.TryParse(actor.UsuarioId, out var id) ? await usuarios.FindByIdAsync(id.ToString()) : null;
    // Un rol por usuario (FR-09): con dos roles tampoco pasa, como hoy para el administrador.
    if (usuario is null || !usuario.Activo || await usuarios.GetRolesAsync(usuario) is not [var unico] || unico != rol)
        throw new AccesoDenegadoException(mensajeSiNoLoEs);
    return new(usuario.Id.ToString(), usuario.NombreCompleto, rol, CanalAuditoria.Personal);
}

public Task<ActorAuditoria> ExigirAdministradorActivoAsync(ActorAuditoria actor, string mensajeSiNoLoEs, CancellationToken ct) =>
    ExigirActivoConRolAsync(actor, Roles.Administrador, mensajeSiNoLoEs, ct);   // misma firma y comportamiento
```

**Persistencia — `AgendaMedicoConfiguracion`**:

```csharp
public const string IndiceUnaEditable = "ux_agendas_medico_una_editable";   // la 4.2 traduce su 23505

// Decisión 2: una sola propuesta editable por médico. CON NOMBRE: un HasIndex(a => a.MedicoId) sin nombre devolvería
// el índice de la una-enviada y le cambiaría el filtro.
agenda.HasIndex(a => a.MedicoId, IndiceUnaEditable).IsUnique().HasFilter("estado IN ('Borrador', 'Rechazada')");
```

La migración generada tiene que tener **sólo** un `CreateIndex` con ese nombre, `unique: true` y el
filtro; ningún `DropIndex`/`RenameIndex` del índice de la 4.1 y ninguna columna. No hace falta
`migrationBuilder.Sql` de permisos (no hay tabla nueva).

**Web — `FormularioAgenda.cs`** (espacio de nombres `HospitalTurnos.Web.Components.Personal.Agendas`):

```csharp
public class FormularioAgenda                               // no sealed: la 4.4 lo hereda (FormularioCorreccion)
{
    [Required(ErrorMessage = "Ingresá la duración del turno.")]
    [Range(AgendaMedico.DuracionMinima, AgendaMedico.DuracionMaxima,
        ErrorMessage = "La duración del turno tiene que estar entre 10 y 60 minutos.")]
    public int? DuracionTurnoMinutos { get; set; }

    [Required(ErrorMessage = "Elegí desde qué fecha rige.")]
    public DateOnly? VigenteDesde { get; set; }

    public List<FilaFranja> Franjas { get; set; } = [];
    public string? Accion { get; set; }                     // el botón pulsado (Input.Accion)

    public bool AplicarAccionDeEdicion() { ... }            // actualizar | agregar-franja | quitar-franja:<i>
    public RevisionDeAgenda Revisar() { ... }               // ver abajo
    public DatosAgenda ADatos() { ... }                     // sólo después de validar
    public static FormularioAgenda Desde(AgendaMedico agenda) { ... }   // filas de lunes a domingo, por hora
    public static FormularioAgenda Nueva(DateOnly vigenteDesde) => new() { DuracionTurnoMinutos = 20, VigenteDesde = vigenteDesde };

    // [FilasCompletas] sobre Franjas: fila sin Desde o sin Hasta → "Completá la hora desde y la hora hasta de cada
    // franja, o quitá las que sobran." en "Franjas". Es un control del CAMPO (no IValidatableObject, que sólo corre
    // con los demás campos bien): así sale junto con el de la duración (AC 11, T8).
}

public sealed class FilaFranja { public DayOfWeek Dia { get; set; } = DayOfWeek.Monday; public TimeOnly? Desde { get; set; } public TimeOnly? Hasta { get; set; } }

public sealed record RevisionDeAgenda(
    IReadOnlyDictionary<int, string> Problemas,   // fila → texto (marca roja)
    IReadOnlyDictionary<int, string> Avisos,      // fila → "Los últimos N minutos…" (sin rojo)
    int? TurnosPorSemana,                         // null si hay problemas, duración inválida o sin franjas
    bool SinFranjas);
```

`Revisar()`: arma `new AgendaMedico(0, duracionValida ?? AgendaMedico.DuracionMinima, fecha)` (si la
duración no es válida igual revisa las franjas, pero no da el número); recorre las filas en orden:
incompleta → problema; `new Franja(...)` que lanza → problema con `e.Message`; `AgregarFranja` que
lanza → problema con `e.Message`; si entra y `franja.DuracionMinutos % duracion != 0` → aviso con el
resto. `TurnosPorSemana = agenda.TurnosPorSemana()` sólo sin problemas, con duración válida y alguna
franja. La agenda transitoria nunca se guarda (no tiene contexto).

**Web — `EditorDeAgenda.razor`**: primero el botón oculto
`<button type="submit" name="Input.Accion" value="actualizar" formnovalidate class="visually-hidden" tabindex="-1" aria-hidden="true">`;
después duración (`InputNumber`), fecha (`InputDate`) con el mínimo, la tabla (`<tr class="table-danger">`
si la fila tiene problema, y el texto en una celda de la misma fila), los botones de edición y la línea
del AC 10. Llama a `Input.Revisar()` una vez por dibujo. Los días con un arreglo propio
(`Lunes`…`Domingo`), no con `CultureInfo("es-AR")` (la imagen de Docker puede no traerla; misma razón
que en `Franja`).

**Web — `ResumenDeAgenda.razor`**: días abreviados con arreglo propio (`Lun`, `Mar`, `Mié`, `Jue`,
`Vie`, `Sáb`, `Dom`); orden lunes→domingo (`((int)dia + 6) % 7`) y por `Desde`; hora `H` si los minutos
son 0, si no `H:mm`; `Hasta = 00:00` → `24`. El número sale de `Agenda.TurnosPorSemana()`. Con
`CompararCon` (sólo `EnLista`): una franja "cambia" si `CompararCon.Franjas` no tiene ninguna con el mismo
`Dia`, `Desde` y `Hasta`; la duración "cambia" si `DuracionTurnoMinutos` es distinta.

**Web — `MiPropuesta.razor`** (`@page "/personal/medico/mi-propuesta"`, `@layout LayoutPersonal`,
`@attribute [Authorize(Roles = HospitalTurnos.Dominio.Personal.Roles.Medico)]`, inyecta
`IPropuestaAgenda`, `TimeProvider` y `NavigationManager`):

- `<h1>Mi propuesta de agenda</h1>` siempre (AC 1, 2 y la T19 de la 2.1).
- `OnInitializedAsync`: `vista = await Propuesta.ObtenerAsync(ActorDeSesion.Desde(HttpContext.User), …)`
  en **cada** pedido (GET y POST: en SSR la página se arma entera). Sin vínculo → el texto y nada más.
  Si no, `Input ??= EnCurso editable ? Desde(EnCurso) : ParaCopiar is not null ? Desde(ParaCopiar) con VigenteDesde = mínimo : Nueva(mínimo)`.
  El `EditForm` se dibuja sólo con `Input` listo (L-8).
- `<EditForm Model="Input" method="post" FormName="propuesta" OnSubmit="AlEnviarAsync">` con
  `DataAnnotationsValidator`, `<EditorDeAgenda Input="Input" TurnosPorSemanaAntes="@vista.Vigente?.TurnosPorSemana()" MinimoVigenteDesde="@vista.MinimoVigenteDesde" />`
  y los botones `guardar` y `enviar` (`name="Input.Accion"`).
- `AlEnviarAsync(EditContext contexto)`:

```csharp
private async Task AlEnviarAsync(EditContext contexto)
{
    string destino;
    try
    {
        if (Input!.AplicarAccionDeEdicion()) return;                    // AC 8: se vuelve a dibujar, nada se guarda
        if (Input.Accion is not ("guardar" or "enviar"))
            throw new ReglaDeNegocioException(AccionDesconocida);
        if (!contexto.Validate()) return;                                // AC 11: 200 con los mensajes
        var actor = ActorDeSesion.Desde(HttpContext.User);
        if (Input.Accion == "enviar")
        {
            await Propuesta.EnviarAsync(Input.ADatos(), actor, HttpContext.RequestAborted);
            destino = "enviada";
        }
        else
            destino = await Propuesta.GuardarBorradorAsync(Input.ADatos(), actor, HttpContext.RequestAborted)
                is ResultadoGuardado.SinCambios ? "sin-cambios" : "borrador";
    }
    catch (Exception e) when (ManejadorGlobalDeErrores.EsEsperable(e))   // sólo las del dominio
    {
        ManejadorGlobalDeErrores.FijarEstadoEnPagina(HttpContext, e);   // 400/403/409 con el cartel (UX §8)
        error = e.Message;
        return;
    }
    Navegacion.NavigateTo($"{RutasPersonal.MiPropuesta}?resultado={destino}");   // fuera del try (SSR)
}
```

- **Sin vínculo, un POST igual recibe el cartel.** Con `SinVinculo` la página dibuja, sólo en un POST, el
  `EditForm` vacío (sin campos a la vista): Blazor lo necesita para despachar el envío, y el caso de uso responde
  con el `403` y el texto del AC 2 (AC 21, T33). Sin eso, el envío recibe un `400` en blanco.
- **El sólo lectura del AC 6 es del GET.** En un POST el formulario se dibuja igual aunque la propuesta ya esté
  `Enviada` (Blazor necesita el formulario con su `FormName` para despachar el envío, como la ficha de la 3.2):
  `Guardar borrador` y `Enviar` reciben el `409` del AC 18 con lo escrito conservado (T39), y un botón de edición
  sólo vuelve a dibujar las filas, sin guardar nada. Con una propuesta en curso el título es `Tu propuesta`, nunca
  `Nueva propuesta`.
- Textos de `?resultado=`: `borrador` → `Borrador guardado.`; `sin-cambios` →
  `No había cambios para guardar.`; `enviada` → `Propuesta enviada para aprobación.`; otro → nada.
- Marcas en hora de Córdoba: `TimeZoneInfo.ConvertTime(marca, Reloj.LocalTimeZone)`.

### Salvaguarda de BD

Las migraciones las aplica **el arranque** (nunca `dotnet ef database update`).

1. **Antes** de generar la migración, contra la base de Compose (puede tener agendas de la sonda de la
   4.1, L-13):
   `SELECT medico_id, count(*) FROM agendas_medico WHERE estado IN ('Borrador','Rechazada') GROUP BY 1 HAVING count(*) > 1;`
   → **0 filas**. Si hay alguna, el índice no se va a poder crear al arrancar: DETENERSE y consultar
   al docente (no se borran agendas: ADR-06 y regla 6 del sprint).
2. `docker compose up -d --build` y
   `docker compose exec db psql -U postgres -d hospital_turnos -c "\d agendas_medico"` → los dos índices
   `ux_agendas_medico_una_enviada` y `ux_agendas_medico_una_editable` con su `WHERE`, y ninguna
   columna nueva.
3. `SELECT has_table_privilege('turnos_app','registros_auditoria','UPDATE');` → `f`.
4. Si CUALQUIER resultado no coincide → DETENERSE → corregir la story primero.

### Archivos a crear/modificar

| Acción | Archivo |
|---|---|
| Crear | Los de `files_to_create` del frontmatter |
| Modificar | `src/HospitalTurnos.Aplicacion/Personal/IActorVigente.cs` (`ExigirActivoConRolAsync`, AC 21) |
| Modificar | `src/HospitalTurnos.Infraestructura/Identidad/ActorVigenteIdentity.cs` (su implementación; `ExigirAdministradorActivoAsync` lo llama) |
| Modificar | `src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/AgendaMedicoConfiguracion.cs` (`IndiceUnaEditable`, AC 19) |
| Modificar | `src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/HospitalDbContextModelSnapshot.cs` (lo regenera `dotnet ef`) |
| Modificar | `src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs` (`AddScoped<IPropuestaAgenda, PropuestaAgendaEnBase>()`) |
| Modificar | `src/HospitalTurnos.Web/Components/Personal/Secciones/SeccionesMedico.razor` (quitar el `@page` de `mi-propuesta`) |
| Modificar | `src/HospitalTurnos.Web/Components/_Imports.razor` (`@using HospitalTurnos.Aplicacion.Agendas` y `@using HospitalTurnos.Web.Components.Personal.Agendas`) |
| Modificar | `tests/HospitalTurnos.Web.Tests/Personal/SesionYPermisosHttpTests.cs` (una ruta más en `RutasYaConstruidas`, AC 25) |
| Modificar | `tests/HospitalTurnos.Aplicacion.Tests/Agendas/DatosDeAgenda.cs` (`Franja` suma jueves a domingo; los tres días de la 4.1 no cambian) |
| Crear | `docs/qa/4.2-pasos.md`, `docs/qa/4.2-legibilidad.md` y `docs/qa/4.2-legibilidad-ui.md` (Task 7) |

### Lo que NO hay que hacer

- **NO aprobar, rechazar, contar pendientes ni mandar emails**: 4.3. **NO** corregir agendas por
  urgencia ni tocar `FichaMedico.razor`: 4.4.
- **NO crear otro cálculo de turnos por semana** ni repetir en la Web la regla de superposición o el
  formato de los mensajes de la 4.1: se usa el dominio.
- **NO modificar `AgendaMedico`, `Franja` ni `IConsultaAgendas`** (4.1). Si parece hacer falta,
  DETENERSE y corregir la story.
- **NO crear constantes en `AccionesAuditoria`** ni auditar el borrador.
- **NO tomar el médico de un campo del formulario** ni poner ids en el formulario (AC 22).
- **NO "retirar" una enviada ni borrar propuestas**: el PRD no lo pide y `turnos_app` no tiene
  `DELETE` sobre `agendas_medico` (4.1 AC 24).
- **NO hacer interactiva la página** (`@rendermode`): decisión 3, SSR.
- **NO crear `ExigirMedicoActivoAsync`** ni otro método por rol: uno general, `ExigirActivoConRolAsync`.
- **NO declarar el índice nuevo sin nombre** (`HasIndex(a => a.MedicoId)` reusaría el de la 4.1).
- **NO volver a crear las piezas compartidas** (`ActorDeSesion`, otro mapeo excepción → código, otro
  control de permiso, otra ayuda de POST, otro doble de carrera): se reusan con su firma.
- **NO fijar `HttpContext.Response.StatusCode` a mano**: siempre `FijarEstadoEnPagina`.
- **NO atrapar `Exception` sin el filtro `EsEsperable`** ni envolver `NavigateTo` en un `try`.
- **NO usar `dotnet ef database update`** ni `DateTime.Now`/`UtcNow` (la prueba de la 1.2 lo detecta).
- **NO contar filas totales** en las pruebas (base compartida) ni usar los usuarios fijos para flujos
  que cambian estado.
- **NO crear el proyecto E2E**.

### Cómo verificar que funciona

```bash
cd ~/SourceCode/hospital-turnos

# 1. Migración (antes, la Salvaguarda de BD paso 1). No se conecta a la base.
dotnet ef migrations add UnaPropuestaEditablePorMedico \
  --project src/HospitalTurnos.Infraestructura --startup-project src/HospitalTurnos.Web \
  --output-dir Persistencia/Migraciones
# Leer el Up generado: un solo CreateIndex con el filtro.

# 2. Las de esta story sin base (segundos)
dotnet test tests/HospitalTurnos.Web.Tests --filter "Story=4.2&(FullyQualifiedName~FormularioAgendaTests|FullyQualifiedName~ComponentesDeAgendaTests)"
# Esperado: T1–T14, T46 y T47 (más los casos de cada [Theory]), 0 fallas

# 3. Todas las de esta story (Docker para Testcontainers)
dotnet test --filter "Story=4.2"
# Esperado: 47 pruebas declaradas, 0 fallas
dotnet test --filter "Story=2.1"      # la T19 ajustada sigue en verde (AC 25)

# 4. Suite completa y CI local (AC 25)
dotnet test
bash scripts/ci/local.sh; echo "rc=$?"   # rc=0, cobertura de Dominio y Aplicacion >= 80 %

# 5. En el navegador, con Compose
docker compose up -d --build
open http://localhost:8080/personal/ingreso
```

Recorrido en el navegador (paso 5), con un usuario médico vinculado (crearlo en A4 y vincularlo en
A1 como administrador) y otro sin vincular:

- a. El médico sin vincular entra a "Mi propuesta de agenda": ve el aviso
  `Tu usuario no está vinculado a un médico. Pedíselo al administrador.`
- b. El vinculado, sin agendas: `Todavía no tenés una agenda vigente.` y la propuesta nueva con 20
  minutos y la fecha mínima.
- c. `+ Agregar franja` dos veces; lunes 08:00–12:00 y lunes 10:00–14:00; `Actualizar el cálculo`: la
  segunda fila en rojo con el texto de superposición y `Corregí lo marcado…`.
- d. Cambiar la segunda a miércoles 08:00–10:50: aviso de los 10 minutos y
  `Con esta agenda ofrecés 20 turnos por semana.`; `Guardar borrador`: `Borrador guardado.`
- e. Poner `Rige desde` dentro de 3 días y `Enviar para aprobación`: cartel con la fecha mínima, lo
  escrito sigue ahí. Con la fecha mínima: `Propuesta enviada para aprobación.` y la pantalla en sólo
  lectura con `Esperando aprobación desde el …`.
- f. Como administrador, A1 muestra `Propuesta pendiente` para ese médico.
- g. Entrar como recepción a `/personal/medico/mi-propuesta`: `No tenés permiso…`.
- h. La auditoría quedó escrita:
  `docker compose exec db psql -U postgres -d hospital_turnos -c "SELECT cuando, quien, rol, accion, entidad_id, detalle FROM registros_auditoria WHERE entidad_tipo = 'AgendaMedico' ORDER BY id DESC LIMIT 5;"`

Después, la guía `docs/qa/4.2-pasos.md` y la pasada del tester no técnico sobre esa guía y sobre las
capturas de M2 (B-6), con su resultado en el Dev Agent Record.

## Dev Agent Record

### Agent Model Used

Claude Opus 5.5 (Amelia, pipeline del equipo), sesión 15 (2026-10-17).

### Debug Log References

- Corrida: `.advisors/pipeline-runs/4.2-20261017142946/` (validación, plan, informe, hallazgos, prueba en vivo).
- Prueba en vivo: `.advisors/dod-runtime/4.2-smoke-ledger.jsonl` y `4.2-smoke-outputs/`; matriz `4.2-runtime-matrix.md`.
- Salvaguarda de BD, paso 1: la base de Compose tenía 2 médicos de la sonda de la 4.1 con dos editables cada uno
  (6 agendas `Sonda…`, sin vínculo). Con permiso del docente se borraron con el dueño de la base (la app sigue sin
  `DELETE`); después, 0 filas. Pasos 2 y 3: los dos índices con su `WHERE`, sin columnas nuevas, `f` para el
  `UPDATE` de la auditoría.

### Completion Notes List

- Pruebas primero en los cinco tramos, con rojo legítimo (tipos que la story crea, o la pantalla en construcción).
  Romper a propósito: sin la traducción de carreras, T27–T29 fallan con el error interno de EF; sin marcar la
  agenda como modificada, el caso "sólo agrega una franja" de T29 se cuela sobre una propuesta que cambió.
- `Entry(agenda).State = Modified` alcanzó (no hizo falta el plan B de la story).
- Blazor SSR: `InputDate` de hora dibuja `HH:mm:ss` (el navegador muestra `HH:mm`); el `name` de las filas sale
  `Input.Franjas[0].Desde` con la copia local del índice, como preveía la story.
- La prueba en vivo encontró dos errores que las pruebas no veían, arreglados con su prueba primero: con la tabla
  vacía el navegador no manda `Input.Franjas` y la lista quedaba nula (`500` al agregar la primera franja o guardar
  un borrador sin franjas); un POST de un médico sin vínculo daba `400` en blanco (no había formulario que
  despachar). También la lista de días cortaba "Lunes".
- Revisores: vuelta 1 con 6 (ningún crítico; 8 avisos arreglados, 2 aceptados), vuelta 2 con 5 (limpia salvo el
  Change Log). Pase adversarial salteado por `deep_review: off` (decisión del docente).
- Tester no técnico: guía en 3 pasadas (11 → 14 → 0 marcas); pantalla, 6 marcas aceptadas sin cambios por el
  docente (`docs/qa/4.2-legibilidad-ui.md`).
- Resultado: 601 pruebas en verde (138 + 216 + 247; antes 503), CI local 97,1 % de líneas y 87,7 % de ramas;
  prueba en vivo 78/78 en cinco grupos; matriz 25/25 AC en runtime.

### File List

- Aplicación: `Agendas/DatosAgenda.cs`, `Agendas/IPropuestaAgenda.cs` (nuevos); `Personal/IActorVigente.cs`.
- Infraestructura: `Agendas/PropuestaAgendaEnBase.cs` y la migración `20261017145758_UnaPropuestaEditablePorMedico`
  (nuevos); `Identidad/ActorVigenteIdentity.cs`, `Persistencia/Configuraciones/AgendaMedicoConfiguracion.cs`,
  `HospitalDbContextModelSnapshot.cs`, `ServiciosDeInfraestructura.cs`.
- Web: `Components/Personal/Agendas/` con `MiPropuesta.razor`, `EditorDeAgenda.razor`, `ResumenDeAgenda.razor`,
  `FormularioAgenda.cs` y `FormaDeResumen.cs` (nuevos); `Components/_Imports.razor`, `Secciones/SeccionesMedico.razor`.
- Pruebas: `Aplicacion.Tests/Agendas/PropuestaAgendaTests.cs`, `Agendas/DatosDePropuesta.cs`,
  `Personal/ActorVigenteIdentityTests.cs`, `Web.Tests/Agendas/` (`FormularioAgendaTests.cs`,
  `ComponentesDeAgendaTests.cs`, `MiPropuestaHttpTests.cs`, `DatosDePropuesta.cs`) (nuevos);
  `Aplicacion.Tests/Agendas/DatosDeAgenda.cs`, `Web.Tests/Personal/SesionYPermisosHttpTests.cs`.
- QA: `docs/qa/4.2-pasos.md`, `4.2-legibilidad.md`, `4.2-legibilidad-ui.md`.

### Change Log

- 2026-10-17: Story escrita (Mary) contra `main` en `a8432c3` (1.1 a 4.1 en done), con los contratos
  del sprint 3 fijados por el docente. 25 AC, 45 verificaciones, 5 decisiones abiertas para el
  docente (D-1 a D-5). `TurnosPorSemana` ya existía en la 4.1 (su AC 19): se reusa y se fija en el
  contrato, sin crear otro cálculo.
- 2026-10-17: Correcciones de `revision-sprint-3.md` y decisiones 2 a 7 del docente (opción
  recomendada; marcas reemplazadas y sección "Decisiones tomadas"). C-1: el AC 24 fija cómo usa la 4.4
  el editor (modelo heredado, `Input`, `OnSubmit`, sus valores de `Input.Accion`) y T46 lo prueba.
  I-1: `ResumenDeAgenda` suma `CompararCon` (T14 y T47) y el AC 24 fija `VistaPropuesta.RechazadaPor` y
  la línea `… por <Quien>.` para la 4.3. I-3: `ExigirActivoConRolAsync` público en `IActorVigente` (sin
  `ExigirMedicoActivoAsync`), T15 con los casos del médico y del administrador en el archivo
  `ActorVigenteIdentityTests.cs`. I-5: `DatosDePropuesta` de esta story en los dos proyectos
  (`MedicoConUsuarioAsync` con email y `Guid`; `RelojCorridoDias`). I-6: formato de franjas de la
  auditoría como contrato (AC 17, T23). M-2 (regla de fechas), M-8 (no-regresión nombrada), M-9.
  25 AC, 47 verificaciones.
- 2026-10-17: Revalidada contra `main` en `121ff7c` (la 2.3 ya mergeada): lo citado de la 4.1, la 2.2 y la
  2.3 coincide con el código. Se corrigió lo desactualizado: base de la verificación, las condiciones "si la
  2.3 ya está en `main`" (Task 1 y AC 25), la ayuda `DatosDeAgenda.Franja` (suma jueves a domingo para T19) y
  los tres archivos de `docs/qa/` en `files_to_create`. Sin cambios en AC ni en pruebas.
- 2026-10-17: Durante la implementación (Amelia), por los revisores y la prueba en vivo; sigue con 25 AC y 47
  verificaciones, con casos nuevos dentro de las mismas pruebas. AC 10: si la duración está fuera de rango, manda
  `Corregí lo marcado…` aunque no haya franjas (T11). AC 11: el control de filas incompletas es del campo, así sale
  junto con el de la duración (T8). AC 17: el orden de las franjas del detalle (día numérico, el domingo primero).
  AC 12 y 13: con la tabla vacía el navegador no manda `Input.Franjas`, y la lista quedaba nula → `500` al agregar
  la primera franja o guardar un borrador sin franjas; lo encontró la prueba en vivo (T36 y T37 con casos sin
  filas). AC 21: un POST de un médico sin vínculo daba `400` en blanco; ahora el `403` con el cartel (T33). AC 13
  en pantalla: `sin-cambios` y un `?resultado=` desconocido (T36). Implementación concreta: tres notas (sólo
  lectura del AC 6 en el GET, POST sin vínculo, título `Tu propuesta`).
- 2026-10-17: `done` tras el merge del PR #15 (`7e098b7`), con la CI en verde (6/6 en las dos corridas).


## Review Follow-ups (Advisors)

<!-- tycon-pipeline: generado por Step 6/7. Cada item = finding advisor no resuelto (CRITICAL waived o WARNING). Al resolver el finding en el código, marcá [x]. Step 0/1 detecta los abiertos al retomar (pipeline.js followups-status). -->

- [x] **[WARNING]** VAL-W1 — `docs/stories/4-2-propuesta-agenda.md` — Status del cuerpo alineado y docs/qa/4.2-* en files_to_create (paso 1) <!-- fkey:VAL-W1|docs/stories/4-2-propuesta-agenda.md -->
- [x] **[WARNING]** VAL-W6 — `docs/stories/4-2-propuesta-agenda.md` — Base 121ff7c con la 2.3 en main; DatosDeAgenda.Franja suma jueves a domingo (paso 1) <!-- fkey:VAL-W6|docs/stories/4-2-propuesta-agenda.md -->
- [x] **[WARNING]** SPEC-W2 — `src/HospitalTurnos.Web/Components/Personal/Agendas/MiPropuesta.razor` — Con una enviada, un POST de pestaña vieja decía Nueva propuesta: título Tu propuesta y nota en la story (sólo lectura del AC 6 en el GET) <!-- fkey:SPEC-W2|src/HospitalTurnos.Web/Components/Personal/Agendas/MiPropuesta.razor -->
- [x] **[WARNING]** SPEC-W2 — `src/HospitalTurnos.Infraestructura/Agendas/PropuestaAgendaEnBase.cs` — Orden de franjas del detalle de auditoría escrito en el AC 17 <!-- fkey:SPEC-W2|src/HospitalTurnos.Infraestructura/Agendas/PropuestaAgendaEnBase.cs -->
- [x] **[WARNING]** ACCOV-C1 — `tests/HospitalTurnos.Web.Tests/Agendas/MiPropuestaHttpTests.cs` — T36 prueba sin-cambios y un ?resultado= desconocido en pantalla <!-- fkey:ACCOV-C1|tests/HospitalTurnos.Web.Tests/Agendas/MiPropuestaHttpTests.cs -->
- [x] **[WARNING]** ACCOV-C2 — `src/HospitalTurnos.Web/Components/Personal/Agendas/EditorDeAgenda.razor` — AC 10: la duración inválida manda sobre Agregá franjas (T11) <!-- fkey:ACCOV-C2|src/HospitalTurnos.Web/Components/Personal/Agendas/EditorDeAgenda.razor -->
- [x] **[WARNING]** ACCOV-C2 — `src/HospitalTurnos.Web/Components/Personal/Agendas/FormularioAgenda.cs` — AC 11: control de filas incompletas como atributo del campo, sale junto con la duración (T8) <!-- fkey:ACCOV-C2|src/HospitalTurnos.Web/Components/Personal/Agendas/FormularioAgenda.cs -->
- [x] **[WARNING]** ACCOV-W1 — `src/HospitalTurnos.Web/Components/Personal/Agendas/MiPropuesta.razor` — Mismo título de pestaña vieja que SPEC-W2 <!-- fkey:ACCOV-W1|src/HospitalTurnos.Web/Components/Personal/Agendas/MiPropuesta.razor -->
- [ ] **[WARNING]** SILENT-C3 — `src/HospitalTurnos.Infraestructura/Agendas/PropuestaAgendaEnBase.cs` — Traducción de carreras sin la excepción original: misma convención que GestionMedicosEnBase (3.2); las excepciones del dominio no llevan causa <!-- fkey:SILENT-C3|src/HospitalTurnos.Infraestructura/Agendas/PropuestaAgendaEnBase.cs -->
- [ ] **[WARNING]** SILENT-W7 — `src/HospitalTurnos.Infraestructura/Agendas/PropuestaAgendaEnBase.cs` — Guid inválido en la sesión tratado como sin vínculo: el id sale de la cookie propia, probabilidad ínfima <!-- fkey:SILENT-W7|src/HospitalTurnos.Infraestructura/Agendas/PropuestaAgendaEnBase.cs -->
- [ ] **[WARNING]** TEST-W2 — `src/HospitalTurnos.Infraestructura/Agendas/PropuestaAgendaEnBase.cs` — El portón no cruza cobertura por archivo con .slnx; la CI local mide 97,1 % de líneas <!-- fkey:TEST-W2|src/HospitalTurnos.Infraestructura/Agendas/PropuestaAgendaEnBase.cs -->
- [x] **[WARNING]** TEST-W3 — `docs/stories/4-2-propuesta-agenda.md` — Recuento: mech-check confirma las 47 de la tabla con implementación <!-- fkey:TEST-W3|docs/stories/4-2-propuesta-agenda.md -->
- [x] **[WARNING]** SPEC-W5 — `docs/stories/4-2-propuesta-agenda.md` — Change Log con los cambios de AC y pruebas de la implementación <!-- fkey:SPEC-W5|docs/stories/4-2-propuesta-agenda.md -->
- [x] **[CRITICAL]** DODRT-C5 — `src/HospitalTurnos.Web/Components/Personal/Agendas/FormularioAgenda.cs` — Prueba en vivo: con la tabla vacía Franjas llegaba nula y +Agregar franja / guardar sin franjas daban 500 (T36 y T37 con casos sin filas) <!-- fkey:DODRT-C5|src/HospitalTurnos.Web/Components/Personal/Agendas/FormularioAgenda.cs -->
- [x] **[CRITICAL]** DODRT-C5 — `src/HospitalTurnos.Web/Components/Personal/Agendas/MiPropuesta.razor` — Prueba en vivo: un POST de un médico sin vínculo daba 400 en blanco; ahora 403 con el cartel del AC 21 (T33) <!-- fkey:DODRT-C5|src/HospitalTurnos.Web/Components/Personal/Agendas/MiPropuesta.razor -->
- [x] **[WARNING]** DODRT-W1 — `src/HospitalTurnos.Web/Components/Personal/Agendas/EditorDeAgenda.razor` — Prueba en vivo: la lista de días cortaba Lunes en Lun; más ancha <!-- fkey:DODRT-W1|src/HospitalTurnos.Web/Components/Personal/Agendas/EditorDeAgenda.razor -->
