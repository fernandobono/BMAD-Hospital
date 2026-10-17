---
epic: 4
story: "4.3"
title: "El administrador aprueba o rechaza propuestas, con aviso de turnos fuera de agenda"
status: ready-for-dev
sprint: 3
priority: "P0"
approach: "[CREATE]"
depends_on: ["1.3", "1.4", "2.2", "3.2", "4.1", "4.2"]
files_to_create:
  - "src/HospitalTurnos.Aplicacion/Agendas/IAprobacionPropuestas.cs"
  - "src/HospitalTurnos.Aplicacion/Agendas/IContadorDePropuestas.cs"
  - "src/HospitalTurnos.Aplicacion/Agendas/ITurnosFueraDeAgenda.cs"
  - "src/HospitalTurnos.Infraestructura/Agendas/AprobacionPropuestasEnBase.cs"
  - "src/HospitalTurnos.Infraestructura/Agendas/ContadorDePropuestasEnBase.cs"
  - "src/HospitalTurnos.Infraestructura/Agendas/TurnosFueraDeAgendaSinTurnos.cs"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/Propuestas.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/TablaPropuestas.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/RevisarPropuesta.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/FormularioRechazo.cs"
  - "src/HospitalTurnos.Web/Components/Personal/Agendas/AvisoTurnosFueraDeAgenda.razor"
  - "src/HospitalTurnos.Web/Components/Personal/ContadorPropuestasPendientes.razor"
  - "tests/HospitalTurnos.Dominio.Tests/Agendas/RevisionDePropuestaTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Agendas/Dobles/TurnosFueraDeAgendaEspia.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Agendas/AprobacionPropuestasTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Agendas/ContratoTurnosFueraDeAgendaTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Agendas/Dobles/TurnosFueraDeAgendaFijos.cs"
  - "tests/HospitalTurnos.Web.Tests/Ayudas/ContadorDePropuestasFijo.cs"
  - "tests/HospitalTurnos.Web.Tests/Agendas/PropuestasHttpTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Agendas/PropuestasComponentesTests.cs"
files_to_modify:
  - "src/HospitalTurnos.Dominio/Agendas/AgendaMedico.cs"
  - "src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs"
  - "src/HospitalTurnos.Web/Personal/RutasPersonal.cs"
  - "src/HospitalTurnos.Web/Components/Personal/MenuLateral.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Secciones/SeccionesAdministracion.razor"
  - "src/HospitalTurnos.Aplicacion/Agendas/IPropuestaAgenda.cs"
  - "src/HospitalTurnos.Infraestructura/Agendas/PropuestaAgendaEnBase.cs"
  - "src/HospitalTurnos.Web/Components/Personal/Agendas/MiPropuesta.razor"
  - "tests/HospitalTurnos.Aplicacion.Tests/Agendas/DatosDePropuesta.cs"
  - "tests/HospitalTurnos.Web.Tests/Agendas/DatosDePropuesta.cs"
  - "tests/HospitalTurnos.Web.Tests/Personal/SesionYPermisosHttpTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Personal/MenuLateralTests.cs"
---

# Story 4.3: El administrador aprueba o rechaza propuestas, con aviso de turnos fuera de agenda

Status: ready-for-dev

## Story

Como administradora del Hospital Las Acacias (Lucía),
quiero ver las propuestas de agenda que mandaron los médicos, compararlas con la agenda que rige hoy
y aprobarlas o rechazarlas con un motivo,
para que cada médico tenga su agenda aprobada a tiempo, se entere del resultado y nadie pierda un
turno ya dado por un cambio de agenda.

> **Qué es y qué no es esta story.** Es la pantalla **A6** (listado y detalle), los dos casos de uso
> (aprobar y rechazar), el **contador** de pendientes en el menú, el **email** al médico y el nombre
> de quién rechazó en M2. Las reglas de la agenda (estados, 7 días, qué agenda rige) **ya existen**
> desde la 4.1: esta story no las reescribe, las usa. El editor de agenda y la pantalla M2 son de la
> 4.2. La corrección de urgencia es de la 4.4. La tabla de turnos no existe hasta la 5.3 (sprint 4):
> para FR-15 esta story deja un **punto de extensión** con su prueba-trampa (AC 20–23).

## Acceptance Criteria

> Vocabulario de esta story:
>
> - **Propuesta pendiente**: una `AgendaMedico` en estado `Enviada` (4.1). Es lo único que A6 lista.
> - **Revisar**: aprobar o rechazar una propuesta pendiente.
> - **Vigente**: la agenda que rige **hoy** para ese médico (`IConsultaAgendas.AgendaQueRigeAsync(medicoId, hoy)`, 4.1).
> - **Turno fuera de agenda** (FR-15): un turno ya dado cuyo horario no es un inicio de turno de la
>   agenda nueva (`!agendaNueva.AtiendeEn(fechaHora)`, 4.1 AC 18), dentro del período en que esa
>   agenda va a regir.
> - **Hoy**: la fecha local del hospital (Córdoba), leída del `TimeProvider` inyectado.
>
> Errores: como en todo el repo, el texto de cada excepción es el que ve la persona en el cartel de
> la pantalla, con el código del manejador global (`ManejadorGlobalDeErrores.FijarEstadoEnPagina`):
> `ReglaDeNegocioException` → 400, `ConflictoException` → 409, `NoEncontradoException` → 404,
> `AccesoDenegadoException` → 403. Fechas y horas con `CultureInfo.InvariantCulture`, con la regla del
> sprint (M-2 de la revisión): `dd/MM/yyyy` en los textos que dicen "desde" o "el"; `dd/MM` sólo en
> columnas angostas (la columna **Enviada** del listado) y en el renglón de datos del detalle; horas
> `HH:mm`.

### Listado de propuestas (A6) — FR-14c, UX A6

1. **Given** el administrador **When** entra a `/personal/admin/propuestas` **Then** ve
   `<h1>Propuestas de agenda</h1>` y una tabla con **todas** las propuestas pendientes de todos los
   médicos. Columnas: **Médico** (`Apellido, Nombre`), **Especialidad**, **Enviada** (`dd/MM HH:mm`,
   hora local del hospital), **Rige desde** (`dd/MM/yyyy`) y un enlace **Revisar** que lleva al
   detalle (AC 4). Orden: por **Rige desde**, la más próxima primero; con la misma fecha, la enviada
   antes (decisión 9). Una propuesta cuya fecha de inicio ya pasó (AC 18) lleva en su
   fila el texto `La fecha de inicio ya pasó` (decisión 11: se ve antes de entrar). Borradores,
   rechazadas y aprobadas **no** aparecen. Sin pendientes, en lugar de la tabla:
   `No hay propuestas esperando aprobación.`
2. **La sección deja de estar "en construcción"**: `SeccionesAdministracion.razor` pierde
   `@page "/personal/admin/propuestas"` (L-6: dos páginas en la misma ruta dan un error interno) y la
   ruta se suma a `RutasYaConstruidas` en la T19 de la 2.1. Recepción y médico reciben el `403` con
   `No tenés permiso para entrar a esta sección.` en el listado (ya lo prueba la T19 de la 2.1) **y**
   en el detalle `/personal/admin/propuestas/{id}` (T32).

### Contador de pendientes en el menú — FR-14e, UX §3 y A6

3. **Given** un administrador con sesión **When** abre **cualquier** pantalla del personal **Then**
   el enlace del menú dice `Propuestas de agenda (N)`, con N = cantidad de propuestas pendientes de
   todos los médicos, leída en ese pedido (UX: "se actualiza al entrar a cualquier pantalla"). Con
   N = 0 dice sólo `Propuestas de agenda`, sin número (decisión 8). El `href` del enlace
   no cambia. Recepción y médico no tienen esa sección: para ellos no se cuenta nada (ninguna
   consulta).

### Detalle de una propuesta (A6) — FR-14c, FR-14d, UX A6

4. **Given** una propuesta pendiente **When** el administrador entra a
   `/personal/admin/propuestas/{Id:long}` (`RutasPersonal.Propuesta(long id)`) **Then** ve:
   - `<h1>Propuesta de Apellido, Nombre</h1>` y debajo
     `Especialidad · enviada dd/MM HH:mm · Rige desde dd/MM/yyyy`;
   - dos columnas, **Vigente** y **Propuesta**, cada una con el resumen de agenda en sólo lectura de
     la 4.2, `ResumenDeAgenda` con `Forma = FormaDeResumen.EnLista` (duración, una franja por renglón y
     `N turnos/semana`); en la columna Propuesta, con `CompararCon` = la vigente, las líneas que cambian
     llevan `← cambia` (lo hace el componente de la 4.2, AC 24 de la 4.2). Sin agenda vigente, la
     columna Vigente dice `Sin agenda vigente.` y la Propuesta va sin `CompararCon`;
   - los avisos de los AC 6, 7 y 22, **arriba de los botones**;
   - el botón **Aprobar** y un desplegable **Rechazar…** (`<details>`, funciona sin JavaScript) con el
     campo `Motivo del rechazo` (`<textarea>` con `maxlength="500"` y `required`, validación doble
     NFR-10) y el botón **Rechazar la propuesta**.
5. **Given** un id que no existe, o que es de una agenda que **no** está `Enviada` (un borrador del
   médico, una ya aprobada o rechazada) **When** se pide el detalle por GET **Then** `404` con el
   cartel `Esa propuesta no existe o ya fue revisada.` y el enlace `Volver a Propuestas de agenda`
   (con `FijarEstadoEnPagina`, nunca `StatusCode = 404` a mano: L-1). El administrador no ve
   borradores ajenos por el detalle.
6. **Fecha de inicio ya pasada — decisión 11.** **Given** una propuesta pendiente cuya
   `VigenteDesde` es anterior a hoy **When** se abre el detalle **Then** arriba de los botones se lee
   `La agenda iba a regir desde el dd/MM/yyyy, que ya pasó: rechazala para que el médico elija otra fecha.`
   (el mismo texto del 409 del dominio, 4.1 AC 11) y el botón **Aprobar no se dibuja**; **Rechazar…**
   sí. Si igual llega un POST de aprobar (armado a mano o con la pantalla abierta desde ayer), el
   dominio responde `409` con ese texto y no cambia nada. La pantalla y el dominio usan **la misma**
   regla: `AgendaMedico.FechaDeInicioYaPaso(reloj)` (AC 18).
7. **Una aprobada posterior la va a reemplazar — decisión 13.** **Given** una propuesta pendiente
   con `VigenteDesde` P y una agenda **aprobada** del mismo médico con `VigenteDesde` B > P **When**
   se abre el detalle **Then** arriba de los botones se lee
   `Hay una agenda aprobada que empieza el dd/MM/yyyy y va a reemplazar a esta.` con la fecha B de la
   **más próxima** de esas aprobadas (`AgendaMedico.ProximaAprobadaDespuesDe`, AC 19). No impide
   aprobar: la propuesta regirá de P al día anterior a B (regla de Arq. §6, 4.1 AC 20). Una aprobada
   con la **misma** fecha P no genera aviso: la nueva gana el desempate (4.1 AC 20, "la aprobada
   después") y es el caso "el médico corrige antes de que empiece".

### Aprobar y rechazar — FR-14c, FR-14d, FR-11

8. **Aprobar.** **Given** una propuesta pendiente y un administrador activo **When** pulsa
   **Aprobar** **Then**, en este orden: (1) se confirma **en la base** el permiso (AC 11); (2) se
   carga la agenda con sus franjas y con seguimiento de cambios (si no existe →
   `NoEncontradoException` con `Esa propuesta no existe o ya fue revisada.`); (3) `Aprobar(reloj)`
   del dominio (sus 409 del 4.1 AC 11 y 13); (4) se consultan los turnos fuera de agenda (AC 21);
   (5) se audita `PropuestaAgendaAprobada` (AC 24); (6) se encola el email al médico (AC 15); (7)
   **un solo** `SaveChangesAsync`: agenda, registro de auditoría y email quedan juntos o ninguno
   (Arq. §7.4 y §7.5). Después la página vuelve al listado con el resultado
   (`?resultado=aprobada` o `aprobada-sin-email`) y muestra, en un aviso `role="status"`,
   `Propuesta aprobada. Le avisamos al médico por email.` o
   `Propuesta aprobada. El médico no tiene email cargado: lo va a ver en «Mi propuesta de agenda».`
   Sólo esos textos fijos: nunca se muestra lo que venga en la consulta (como la ficha de la 3.2).
   **FR-14d**: desde la `VigenteDesde` de la aprobada, `AgendaQueRigeAsync` la devuelve; el día
   anterior sigue devolviendo la vigente anterior (lo resuelve la 4.1; T7 lo comprueba de punta a
   punta). El estado de A1 pasa de `Propuesta pendiente` a lo que corresponda (4.1 AC 22).
9. **Rechazar.** **Given** una propuesta pendiente **When** el administrador escribe un motivo y
   pulsa **Rechazar la propuesta** **Then** el mismo orden del AC 8 con `Rechazar(motivo, reloj)` (el
   dominio recorta el motivo, 4.1 AC 10), auditoría `PropuestaAgendaRechazada` con el motivo, email
   con el motivo y un solo guardado. Vuelve al listado con `?resultado=rechazada` o
   `rechazada-sin-email`: `Propuesta rechazada. Le avisamos al médico por email, con el motivo.` o
   `Propuesta rechazada. El médico no tiene email cargado: va a ver el motivo en «Mi propuesta de agenda».`
   La vigente del médico no cambia (RU-07b).
10. **Motivo inválido.** **Given** el motivo vacío, sólo espacios o de más de 500 caracteres **When**
    se envía el rechazo **Then** `400` con el cartel del dominio (`Escribí el motivo del rechazo.` o
    `El motivo tiene como máximo 500 caracteres.`), el desplegable queda **abierto** con lo escrito
    intacto, y en la base no cambia nada: ni la agenda, ni auditoría, ni email.
11. **Permiso en la base, en cada cambio** (decisión 16, Arq. §7.7). `AprobarAsync` y `RechazarAsync`
    llaman primero, antes de leer la agenda, a
    `IActorVigente.ExigirAdministradorActivoAsync(actor, "Sólo un administrador puede aprobar o rechazar propuestas de agenda.", ct)`
    y auditan con el actor que devuelve (nombre leído de la base). Un recepcionista, un médico, un
    administrador **desactivado** (con su cookie todavía viva, 2.1 AC 25) o un id inventado →
    `AccesoDenegadoException` → `403` con ese texto en el cartel, y nada cambia.
12. **Ya revisada o inexistente, por POST.** **Given** una propuesta que otro administrador ya aprobó
    o rechazó antes (la pantalla quedó abierta) **When** se pulsa **Aprobar** o se rechaza **Then**
    `409` con el texto del dominio (`Sólo se puede aprobar una propuesta enviada.` /
    `Sólo se puede rechazar una propuesta enviada.`). Un id que no existe → `404` con
    `Esa propuesta no existe o ya fue revisada.` Como en la ficha de la 3.2, en un POST la página
    dibuja los formularios aunque no haya podido leer la propuesta, para que el envío llegue al caso
    de uso y responda su código.
13. **Dos administradores a la vez** (4.1 AC 27, L-3). **Given** dos administradores que abrieron la
    misma propuesta **When** uno aprueba y el otro rechaza "al mismo tiempo" (el segundo cargó la
    agenda antes de que el primero guardara) **Then** el segundo `SaveChangesAsync` lanza
    `DbUpdateConcurrencyException` (columna `xmin`, 4.1) y el caso de uso la traduce a
    `ConflictoException` con
    `Otro administrador acaba de revisar esta propuesta. Volvé a Propuestas de agenda para ver cómo quedó.`
    (`409`, nunca un `500`). Del segundo no queda **nada**: ni su cambio de estado, ni su registro de
    auditoría, ni su email. Se prueba con el doble de carreras de la 3.1 (`InterceptorQueSeAdelanta`),
    no con la suerte.
14. **Sin token antiforgery**: un POST de aprobar o de rechazar sin `__RequestVerificationToken` →
    `400` y nada cambia (lo hace Blazor; se prueba porque es una acción que cambia datos, como la T30
    de la 3.2).

### Email al médico — FR-14e, ADR-05

15. **Given** una propuesta revisada cuyo médico tiene un usuario vinculado (`Medico.UsuarioPersonalId`,
    3.2) con email cargado **When** se aprueba o se rechaza **Then** se encola **un** `MensajeEmail`
    con `IBandejaDeSalida.Encolar` (1.4), en el mismo guardado del AC 8:
    - `Para` = el `Email` de ese `UsuarioPersonal` (decisión 1; quién lo carga: la 2.4, AC 29);
    - `Tipo` = `"propuesta-agenda-aprobada"` o `"propuesta-agenda-rechazada"` (constantes de esta
      story; el contrato de la 1.4 deja los tipos a cada story); `ReferenciaId` = el id de la agenda;
    - aprobada: `Asunto` = `Tu agenda fue aprobada`; el texto dice
      `La administración del hospital aprobó tu propuesta de agenda. Rige desde el dd/MM/yyyy: turnos de N minutos, T turnos por semana.`
      y cómo verla (`«Mi propuesta de agenda»`);
    - rechazada: `Asunto` = `Tu propuesta de agenda fue rechazada`; el texto dice
      `La administración del hospital rechazó tu propuesta de agenda que iba a regir desde el dd/MM/yyyy.`,
      `Motivo: <motivo>`, `Tu agenda vigente no cambia.` y cómo corregirla;
    - el cuerpo HTML lleva lo mismo con el nombre y el motivo **codificados** (`WebUtility.HtmlEncode`):
      un motivo con `<b>` llega como texto, no como HTML.
    Si la revisión falla (cualquier excepción antes o durante el guardado), el email **no** queda en
    la bandeja. No se registra ningún manejador de fallo definitivo para estos tipos: si el email se
    pierde, el médico igual ve el estado en M2 (el procesador de la 1.4 deja su `Warning` "sin
    manejador para el tipo").
16. **Sin email.** **Given** un médico sin usuario vinculado (se desvinculó después de enviar, 3.2) o
    con un usuario sin email (`Email` nulo o en blanco: hoy A4 no lo pide; lo suma la 2.4 en el
    sprint 4, decisión 1) **When** se revisa su propuesta **Then** la revisión se hace igual, **sin
    error**, **no** se encola nada, la auditoría lo anota (`emailEncolado: false`) y la pantalla
    muestra el texto "sin email" del AC 8 / AC 9. Nunca se encola un email con `Para` vacío.
    **FR-14e queda a medias hasta la 2.4** (I-7 de la revisión): el médico ve el estado en M2, pero
    no recibe el aviso mientras su usuario no tenga email. Es lo esperado hasta esa story, no un error.

### Quién rechazó, en M2 — FR-14e, UX M2

17. **Given** una propuesta rechazada (o rechazada y en corrección: `Borrador` con `MotivoRechazo`,
    4.1 AC 12) **When** el médico abre M2 (pantalla de la 4.2) **Then** la línea del rechazo que ya
    muestra la 4.2 (su AC 5) se lee completa, en una sola línea (decisión 7):
    `Rechazada el dd/MM/yyyy a las HH:mm por Lucía Pérez.` (fecha y hora local de `RechazadaEl`). El
    nombre es el `Quien` del **último** registro de auditoría `PropuestaAgendaRechazada` de esa agenda
    (`EntidadTipo = "AgendaMedico"`, `EntidadId` = su id), tal como quedó en la auditoría (el nombre
    completo: no hay que adivinar el apellido). Llega a la página por el dato nuevo
    `VistaPropuesta.RechazadaPor` (`string?`, **al final** del registro, con valor por defecto `null`),
    que carga `PropuestaAgendaEnBase.ObtenerAsync` (contrato del AC 24 de la 4.2). Si la agenda se
    rechazó dos veces, el del último rechazo. Sin registro (rechazada por fuera de la aplicación)
    → `RechazadaPor = null` y la línea queda como la dejó la 4.2: `Rechazada el dd/MM/yyyy a las HH:mm.`

### Dos reglas nuevas en el dominio (`AgendaMedico`, 4.1)

18. `public bool FechaDeInicioYaPaso(TimeProvider reloj)` → `VigenteDesde < hoy` (fecha **local** del
    hospital). `Aprobar(reloj)` pasa a usarlo para su 409, con el mismo texto: una sola regla para la
    pantalla (AC 6), el listado (AC 1) y el dominio. Las pruebas de la 4.1 (T16, T17) siguen en verde
    sin tocarlas. Borde: a las 23:30 del domingo 01/11 en Córdoba (02:30 UTC del lunes 02/11), una
    agenda desde el 02/11 **todavía no** pasó.
19. `public static AgendaMedico? ProximaAprobadaDespuesDe(IEnumerable<AgendaMedico> agendas, DateOnly fecha)`
    → la **aprobada** con la `VigenteDesde` más chica que sea **estrictamente posterior** a `fecha`;
    con la misma fecha, la aprobada después (como `QueRigeEn`). Borradores, enviadas y rechazadas no
    cuentan. Ninguna → `null`. Dice hasta cuándo va a regir una agenda nueva: hasta el día anterior a
    la `VigenteDesde` de la que devuelve. La usan el aviso del AC 7, el límite del AC 21 y la 4.4.

### Turnos fuera de la agenda nueva — FR-15 (punto de extensión; contrato con la 4.4 y la 5.3)

20. **El puerto — nombre y firma fijos (contrato).** En `src/HospitalTurnos.Aplicacion/Agendas/ITurnosFueraDeAgenda.cs`:

    ```csharp
    public interface ITurnosFueraDeAgenda
    {
        Task<IReadOnlyList<TurnoFueraDeAgenda>> BuscarAsync(AgendaMedico agendaNueva, DateOnly? hasta,
            CancellationToken ct);
    }

    public sealed record TurnoFueraDeAgenda(long TurnoId, DateTime FechaHora, string Paciente);
    ```

    Significado: los turnos **ya dados** (reservados, no cancelados) del médico `agendaNueva.MedicoId`
    con fecha y hora desde el inicio de `agendaNueva.VigenteDesde` (y no antes de ahora) hasta el
    inicio de `hasta` sin incluirlo (`null` = sin límite), cuyo horario **no** es un inicio de turno
    de la agenda nueva (`!agendaNueva.AtiendeEn(fechaHora)`), ordenados por `FechaHora`. `FechaHora`
    en hora local del hospital (`Kind = Unspecified`, contrato §Base de datos); `Paciente` =
    `Apellido, Nombre`. **Hoy** la implementación registrada es `TurnosFueraDeAgendaSinTurnos`
    (Infraestructura/Agendas), `Scoped` en `AgregarInfraestructura`, y devuelve **siempre** una lista
    vacía: no hay tabla de turnos, así que ningún turno puede quedar afuera (es la respuesta verdadera
    de hoy, como la 3.1 AC 10). No recibe la base.
21. **Quién lo llama y con qué.** `AprobacionPropuestasEnBase` lo llama en `ObtenerAsync` (para el
    aviso de A6) y otra vez en `AprobarAsync` (el dato al momento de aprobar, para la auditoría), con
    la propuesta como `agendaNueva` y `hasta` = `ProximaAprobadaDespuesDe(aprobadasDelMedico,
    propuesta.VigenteDesde)?.VigenteDesde` (AC 19): después de esa fecha manda la otra agenda, y sus
    turnos no son de esta revisión. **No bloquea**: con N > 0 se puede aprobar igual (FR-15: "decide
    si igual aprueba; esos turnos se mantienen"); la aprobación no toca ningún turno y la auditoría
    anota `turnosFueraDeAgenda: N`. `RechazarAsync` no lo llama (rechazar no cambia la agenda que rige).
22. **El lugar del aviso en A6** — componente `AvisoTurnosFueraDeAgenda`
    (`Components/Personal/Agendas/AvisoTurnosFueraDeAgenda.razor`, parámetro
    `[Parameter, EditorRequired] IReadOnlyList<TurnoFueraDeAgenda> Turnos`), que la 4.4 reusa. Con la
    lista vacía no dibuja nada. Con 1:
    `⚠ 1 turno ya dado queda fuera de la nueva agenda. Se mantiene.`; con N > 1:
    `⚠ N turnos ya dados quedan fuera de la nueva agenda. Se mantienen.`; en los dos casos, un
    desplegable `Ver` (`<details>`) con una línea por turno: `<fecha> <hora> · <paciente>`
    (`dd/MM/yyyy HH:mm`). Va arriba de los botones del detalle (AC 4), con `role="alert"`. En A6 **no**
    hay paso extra de confirmación: el aviso está antes del botón y aprobar es la decisión (decisión
    25). La 4.4 usa el mismo componente y sí pide `Corregir igual`, porque allí la agenda se escribe en
    el mismo formulario y el aviso no se puede mostrar antes.
23. **Prueba-trampa** (como la T15 de la 3.1, L-7). Una prueba sin base recorre el modelo de
    `HospitalDbContext` y falla en cuanto exista una entidad con `ClrType.Name == "Turno"` **o**
    mapeada a la tabla `turnos`, con el mensaje exacto:
    `La story 5.3 agregó los turnos: implementá ITurnosFueraDeAgenda contra la tabla de turnos (contrato del AC 20 de la story 4.3, FR-15), registrala en lugar de TurnosFueraDeAgendaSinTurnos y reemplazá esta prueba por las de turnos fuera de agenda.`
    Además, otra prueba fija que hoy el contenedor resuelve `TurnosFueraDeAgendaSinTurnos` y que
    devuelve vacío para cualquier agenda (T16).

### Auditoría — FR-33, R-12

24. Los dos casos de uso auditan en el mismo guardado (AC 8), canal `Personal`, con el actor
    confirmado en la base, `EntidadTipo = "AgendaMedico"` y `EntidadId` = el id de la agenda:
    - `AccionesAuditoria.PropuestaAgendaAprobada` con detalle
      `{ medicoId, vigenteDesde ("yyyy-MM-dd"), turnosFueraDeAgenda (número), reemplazadaDesde ("yyyy-MM-dd" o null), emailEncolado }`;
    - `AccionesAuditoria.PropuestaAgendaRechazada` con detalle
      `{ medicoId, vigenteDesde, motivo (recortado), emailEncolado }`.
    No se crean constantes nuevas: las dos existen desde la 1.3. El detalle **no** lleva la dirección
    de email (dato personal; alcanza con `emailEncolado`).

### Contrato con otras stories (B-1: cada viñeta con sus pruebas)

25. **Con la 4.2 (se implementa antes; esta story usa lo que fija el AC 24 de la 4.2).** Los nombres
    los igualó la revisión adversarial del sprint con los de la 4.2 (dueña); si al implementar algo no
    coincide, vale la 4.2 y se corrige esta story antes de programar (Task 1). Pruebas: T23 (el resumen
    con `← cambia` en A6), T15 (`RechazadaPor`) y T34 (la línea completa en M2).

    | Lo que usa | Nombre real (4.2) | Dónde lo usa la 4.3 |
    |---|---|---|
    | Resumen de agenda en sólo lectura | `ResumenDeAgenda` (`Components/Personal/Agendas/ResumenDeAgenda.razor`), parámetros `Agenda`, `Forma` (`FormaDeResumen.EnLista`) y `CompararCon` | Las dos columnas del detalle (AC 4) |
    | Turnos por semana | `AgendaMedico.TurnosPorSemana()` (4.1 AC 19) | El texto del email aprobado (AC 15) |
    | Pantalla M2 | `Components/Personal/Agendas/MiPropuesta.razor` en `RutasPersonal.MiPropuesta`, con la línea `Rechazada el dd/MM/yyyy a las HH:mm.` (4.2 AC 5) | AC 17: la línea pasa a `… por <RechazadaPor>.` |
    | La vista de M2 | `VistaPropuesta` (`Aplicacion/Agendas/IPropuestaAgenda.cs`), cargada por `PropuestaAgendaEnBase.ObtenerAsync` | AC 17: se suma `string? RechazadaPor = null` al final |
    | Auditoría de agendas | `EntidadTipo = "AgendaMedico"`, `EntidadId` = id de la agenda | AC 24: la historia de una agenda en A5 (9.1) queda bajo una sola entidad |
    | Ayudas de prueba | `DatosDePropuesta` de Aplicacion.Tests (`MedicoConUsuarioAsync(proveedor, activo, email)` → `(MedicoId, Actor, UsuarioId)`) y de Web.Tests (`MedicoIngresadoAsync`, `GuardarAgendaAsync(app, agenda)`, `RelojCorridoDias`) | T3–T34; esta story les **suma** sus ayudas (Given/Fixture), no crea otro archivo |
    | Una sola editable por médico | índice `ux_agendas_medico_una_editable` (decisión 2) | Los "Given" de T3 y T15 no arman dos editables del mismo médico |

26. **Con la 4.4 (Corrección de urgencia, se implementa después).** La 4.4 **reusa** y no crea otro:
    (a) `ITurnosFueraDeAgenda.BuscarAsync(agendaCorregida, hasta, ct)` del AC 20, con `hasta` =
    `AgendaMedico.ProximaAprobadaDespuesDe(aprobadasDelMedico, agendaCorregida.VigenteDesde)?.VigenteDesde`
    (AC 19); (b) el componente `AvisoTurnosFueraDeAgenda` del AC 22 en su pantalla "Corregir agenda",
    con el paso `Corregir igual` que A6 no tiene (decisión 25); (c) los dobles de prueba
    `TurnosFueraDeAgendaEspia` (Aplicacion.Tests) y `TurnosFueraDeAgendaFijos` (Web.Tests, con
    `WithWebHostBuilder`), sin crear otros.
    Pruebas que fijan lo que la 4.4 recibe: T2 (`ProximaAprobadaDespuesDe`), T18 (el puerto se llama
    con la agenda y el límite, y no bloquea) y T33 (los textos del componente). La 4.4 escribe sus
    propias pruebas de que lo llama.
27. **Con la 5.3 (Reserva, sprint 4) — herencia.** Cuando la 5.3 cree los turnos, en la **misma**
    story: (a) implementa `ITurnosFueraDeAgenda` contra su tabla con el significado exacto del AC 20
    (reservados y no cancelados; desde `max(inicio de VigenteDesde, AhoraLocal())`; antes del inicio
    de `hasta`; `!agendaNueva.AtiendeEn(fechaHora)`; ordenados por `FechaHora`), la registra en
    `AgregarInfraestructura` en lugar de `TurnosFueraDeAgendaSinTurnos` y borra esa clase;
    (b) reemplaza las pruebas T16 y T17 de esta story por pruebas reales con `FakeTimeProvider`
    (turno dentro, turno fuera por día, por hora y por duración distinta, turno cancelado, turno
    pasado, turno después de `hasta`); (c) no cambia la firma ni el componente (la 4.4 los usa). La
    trampa T17 salta junto con la T15 de la 3.1, y su mensaje nombra esta obligación.
    **Pendiente fuera de la story (docente):** sumar a la fila de la 5.3 del plan "hereda: 4.3 AC 27
    (FR-15)", como se hizo con la 3.1 (I-8 b de la revisión del S2).
28. **Con la 9.1 (Auditoría, A5)** — lee `propuesta-agenda.aprobada` y `propuesta-agenda.rechazada`
    con `EntidadTipo = "AgendaMedico"` y el detalle del AC 24. Pruebas: T7 y T8 leen el registro y su
    detalle tal como lo va a leer A5.

29. **Con la 2.4 (Email del personal en A4, sprint 4; decisión 1).** Esta story **lee**
    `UsuarioPersonal.Email` (la columna `email` de `asp_net_users`, que Identity ya trae) del usuario
    vinculado al médico (`Medico.UsuarioPersonalId`); **no** lo carga. La 2.4 suma el campo al alta y
    a la edición de A4, y valida su forma al cargarlo. Lo que esta story le deja: (a) cuando la 2.4
    cargue el email, el aviso empieza a salir **sin tocar esta story** (mismo `EmailDelMedicoAsync`);
    (b) un email nulo, vacío o sólo con espacios se trata como "sin email", sin error (AC 16); (c) esta
    story no valida la forma de la dirección (si llegara una inválida, el procesador de la 1.4 la da
    por perdida sin intentar, 1.4 AC 8). Con la 2.4 FR-14e queda completo. Pruebas: T7 (con email
    cargado, se encola con ese `Para`) y T14 (nulo y en blanco: sin email y sin error).

### No-regresión

30. Siguen en verde las pruebas de las stories 1.1 a 4.2. Cambios **esperados** en lo existente, y
    sólo éstos:
    - `SeccionesAdministracion.razor` pierde `@page "/personal/admin/propuestas"` (quedan bloqueos y
      auditoría "en construcción"); en la T19 de la 2.1 (`SesionYPermisosHttpTests`),
      `/personal/admin/propuestas` se suma a `RutasYaConstruidas` (sigue esperando `200` +
      `<h1>Propuestas de agenda</h1>` para el administrador y `403` para los otros roles);
    - en `MenuLateralTests` (2.1) se agrega **una línea de armado**
      (`Services.AddSingleton<IContadorDePropuestas>(new ContadorDePropuestasFijo(0))`): el menú del
      administrador ahora dibuja el contador. Sus aserciones (los `href` en el orden de UX, el nombre,
      el botón Salir) no cambian;
    - `AgendaMedico.Aprobar` usa `FechaDeInicioYaPaso` (AC 18): T16 y T17 de la 4.1, sin tocar;
    - `VistaPropuesta` (4.2) suma `RechazadaPor` al final con valor por defecto `null`, y M2 agrega
      ` por <RechazadaPor>` a su línea sólo cuando hay dato (AC 17): las pruebas de la 4.2 no se tocan
      (su T41 arma la rechazada sin registro de auditoría y sigue viendo la línea sin nombre).
    La ruta de inicio del administrador, `registros_auditoria` sin `UPDATE` para `turnos_app` y la
    cobertura de líneas de Dominio y Aplicacion sobre el 80 % de la CI (NFR-11) no cambian. **Esta
    story no tiene migración**: las tablas que usa ya existen (`agendas_medico`, `franjas_agenda`,
    `registros_auditoria`, `mensajes_salientes`, `asp_net_users`).

### Tests (34 verificaciones)

| # | Test | AC | Nivel | Escenario | Resultado esperado |
|---|------|----|-------|-----------|--------------------|
| T1 | `FechaDeInicioYaPaso_SegunHoyDelHospital_DevuelveSiYaPaso` | 18 | unit | Agenda desde 02/11/2026; `[Theory]` reloj en 03/11 12:00 (true), 02/11 08:00 (false), 01/11 12:00 (false), y `2026-11-02T02:30Z` = 01/11 23:30 en Córdoba (false); más una enviada desde 02/11 aprobada con el reloj en 03/11 | Los valores indicados; el `Aprobar` del 03/11 lanza el 409 del 4.1 AC 11 con el mismo texto de siempre (`La agenda iba a regir desde el 02/11/2026, que ya pasó: ...`) |
| T2 | `ProximaAprobadaDespuesDe_VariasAgendas_DevuelveLaMasProximaPosterior` | 19,7,26 | unit | A aprobada desde 02/11, B aprobada desde 16/11, C aprobada desde 30/11, D enviada desde 09/11, E rechazada desde 23/11; `[Theory]` fechas 26/10, 02/11, 09/11, 16/11, 30/11; y A2 y A3 aprobadas las dos desde 16/11 (A3 después), fecha 09/11 | 26/10 → A; 02/11 → B (la de la misma fecha no cuenta); 09/11 → B; 16/11 → C; 30/11 → null; con A2/A3 → A3 |
| T3 | `ListarPendientesAsync_VariasAgendas_SoloEnviadasOrdenadasPorRigeDesde` | 1 | integration | 6 médicos propios: M1 enviada desde 30/11 (enviada 16/10 10:00), M2 enviada desde 23/10 (16/10 11:00), M3 enviada desde 30/11 (16/10 09:00), M4 con un borrador y una aprobada, M6 con una rechazada (en médicos distintos: el índice de la decisión 2 no deja dos editables del mismo médico); M5 con una enviada el 10/10 desde el 20/10; reloj de lectura 21/10 | Filtrando el resultado por los ids propios: M5, M2, M3, M1 (por rige desde y, empatadas, por envío); M4 y M6 no aparecen; la de M5 aparece con `FechaDeInicioYaPaso = true`; `Medico` = `Apellido, Nombre`, `Especialidad`, `EnviadaElLocal` en hora de Córdoba |
| T4 | `PendientesAsync_EnviarYRevisar_SubeYBajaLaCuenta` | 3 | integration | `IContadorDePropuestas` del contenedor; contar (N0); guardar 2 enviadas de médicos propios; contar; aprobar una y rechazar la otra con `IAprobacionPropuestas`; contar | N0, N0 + 2, N0 (cuenta global: la base es compartida, por eso se mide la diferencia) |
| T5 | `ObtenerAsync_Enviada_TraeVigentePropuestaYAvisos` | 4,6,7,21 | integration | Médico propio; `[Theory]`: (a) sin vigente; (b) vigente desde 12/10 (aprobada antes) y enviada desde 02/11; (c) enviada el 10/10 desde el 20/10, leída con el reloj en 21/10; (d) aprobada desde 16/11 y enviada desde 02/11 | (a) `Vigente = null`; (b) `Vigente.Id` = la de 12/10 con sus franjas, `Propuesta.Id` = la enviada con sus franjas; (c) `FechaDeInicioYaPaso = true`; (d) `ReemplazadaDesde = 16/11/2026`; en todos, `TurnosFueraDeAgenda` vacía y `Medico`/`Especialidad` del médico |
| T6 | `ObtenerAsync_NoEnviadaOInexistente_DevuelveNull` | 5 | integration | `[Theory]`: un borrador, una rechazada, una aprobada, un id que no existe | `null` en los cuatro |
| T7 | `AprobarAsync_Enviada_ApruebaAuditaYEncolaEmailEnUnSoloGuardado` | 8,15,24,28,29 | integration | Reloj 20/10/2026; médico propio con usuario vinculado con email `medico-<marca>@ejemplo.com`, vigente desde 12/10, enviada desde 02/11 (lunes 08–12, 20 min); admin propio | Devuelve `AvisoPorEmail = true`; en la base `estado = 'Aprobada'`, `aprobada_el` = la hora del reloj; un registro `propuesta-agenda.aprobada` con `entidad_tipo = 'AgendaMedico'`, `entidad_id` = id, `quien` = el nombre del admin leído de la base, canal `Personal`, detalle `{ medicoId, vigenteDesde: "2026-11-02", turnosFueraDeAgenda: 0, reemplazadaDesde: null, emailEncolado: true }`; un mensaje con ese `Para`, `tipo = 'propuesta-agenda-aprobada'`, `referencia_id` = id, asunto `Tu agenda fue aprobada`, el texto con `Rige desde el 02/11/2026: turnos de 20 minutos, 12 turnos por semana.`; `AgendaQueRigeAsync` el 01/11 → la de 12/10 y el 02/11 → la aprobada (FR-14d) |
| T8 | `RechazarAsync_ConMotivo_RechazaAuditaYEncolaEmailConElMotivo` | 9,15,24,28 | integration | Igual que T7; motivo `"  El consultorio 3 está <b>ocupado</b>.  "` | `estado = 'Rechazada'`, `motivo_rechazo` recortado; registro `propuesta-agenda.rechazada` con `motivo` recortado y `emailEncolado: true`; mensaje `propuesta-agenda-rechazada` con asunto `Tu propuesta de agenda fue rechazada`, el texto con `Motivo: El consultorio 3 está <b>ocupado</b>.` y el HTML con `&lt;b&gt;ocupado&lt;/b&gt;`; la vigente sigue rigiendo el 02/11 |
| T9 | `RechazarAsync_MotivoInvalido_LanzaYNoCambiaNada` | 10 | integration | Enviada propia; `[Theory]` `""`, `"   "`, 501 letras `a` | `ReglaDeNegocioException` con el texto del dominio; en la base sigue `Enviada`; 0 registros de auditoría y 0 mensajes con ese id |
| T10 | `AprobarAsync_FechaDeInicioYaPaso_ConflictoSinCambios` | 6,18 | integration | Enviada el 10/10 desde el 20/10; reloj de la aprobación en 21/10 | `ConflictoException` con `La agenda iba a regir desde el 20/10/2026, que ya pasó: rechazala para que el médico elija otra fecha.`; sigue `Enviada`; 0 auditoría, 0 mensajes |
| T11 | `AprobarYRechazar_SinPermisoEnLaBase_AccesoDenegadoSinCambios` | 11 | integration | Enviada propia; `[Theory]` × {aprobar, rechazar}: actor recepcionista, médico, administrador desactivado (`UsuariosDePrueba.CrearAsync(..., activo: false)`), `UsuarioId` inventado | `AccesoDenegadoException` con `Sólo un administrador puede aprobar o rechazar propuestas de agenda.`; sigue `Enviada`; 0 auditoría, 0 mensajes |
| T12 | `AprobarYRechazar_YaRevisadaOInexistente_ConflictoONoEncontrado` | 12 | integration | `[Theory]`: aprobar una ya aprobada; rechazar una ya aprobada; aprobar una ya rechazada; aprobar un id que no existe | Los 409 `Sólo se puede aprobar una propuesta enviada.` / `Sólo se puede rechazar una propuesta enviada.`; el 404 `Esa propuesta no existe o ya fue revisada.`; nada cambia |
| T13 | `RevisarAsync_OtroAdministradorSeAdelanta_ConflictoSinAuditoriaNiEmail` | 13 | integration | Enviada propia con usuario con email; `[Theory]` × {aprobar, rechazar}; el contenedor con `ConfigureDbContext` + `InterceptorQueSeAdelanta(CadenaApp, "UPDATE agendas_medico SET estado = 'Rechazada', motivo_rechazo = 'Se adelantó otro', rechazada_el = now() WHERE id = <id>")` | `ConflictoException` con `Otro administrador acaba de revisar esta propuesta. Volvé a Propuestas de agenda para ver cómo quedó.`; `interceptor.SeAdelanto`; en la base queda lo del otro (`Rechazada`, `Se adelantó otro`); 0 registros de esta revisión y 0 mensajes con ese id |
| T14 | `AprobarYRechazar_MedicoSinEmail_RevisaSinEncolarYLoAudita` | 16,29 | integration | `[Theory]` × {aprobar, rechazar}: médico sin usuario vinculado; vinculado con `Email = null`; vinculado con `Email = "   "` | Revisada; `AvisoPorEmail = false`; 0 mensajes con ese id; detalle con `emailEncolado: false` |
| T15 | `ObtenerAsync_PropuestaRechazada_RechazadaPorEsElDelUltimoRechazo` | 17,25 | integration | `IPropuestaAgenda.ObtenerAsync` (4.2) con el actor de cada médico; reloj que avanza. Médico A (`MedicoConUsuarioAsync`): enviada → rechazada por el admin "Ana Uno" → corregida y reenviada → rechazada por "Beto Dos". Médico B, **otro** médico propio con usuario: enviada y rechazada por "Ana Uno". Médico C: una rechazada armada con `GuardarAgendaAsync` (sin registro de auditoría). Médico D: una enviada nunca rechazada | A → `RechazadaPor = "Beto Dos"`; B → `"Ana Uno"`; C → `null`; D → `null` |
| T16 | `TurnosFueraDeAgenda_ImplementacionDeHoy_EstaRegistradaYDevuelveVacio` | 20,27 | integration | Contenedor de `DatosDeAgenda.Proveedor`; un alcance; `[Theory]` con una enviada desde 02/11 y `hasta` null y 16/11 | Se resuelve `TurnosFueraDeAgendaSinTurnos`; `BuscarAsync` devuelve una lista vacía en los dos casos |
| T17 | `ModeloDeDatos_SinTurnos_ElContratoDeTurnosFueraDeAgendaSigueVigente` | 23,27 | unit | `ContextoDePrueba.Crear("Host=no-se-usa;Database=no-se-usa").Model` (no abre conexión) | Ninguna entidad con `ClrType.Name == "Turno"` ni `GetTableName() == "turnos"`; si aparece, falla con el mensaje exacto del AC 23 |
| T18 | `ObtenerYAprobar_ConTurnosFuera_LlamanAlPuertoConLaAgendaYElLimiteSinBloquear` | 21,26 | integration | Contenedor con `TurnosFueraDeAgendaEspia` (devuelve 2 turnos y anota cada llamada) en lugar del real; médico propio; `[Theory]`: (a) sólo la enviada desde 02/11; (b) además una aprobada desde 16/11 | `ObtenerAsync`: `TurnosFueraDeAgenda.Count = 2`; el espía recibió la agenda con el id de la propuesta y `hasta` = (a) null / (b) 16/11/2026. `AprobarAsync`: aprueba igual (no bloquea), el espía tiene una llamada más con los mismos datos, y el detalle de auditoría dice `turnosFueraDeAgenda: 2` y `reemplazadaDesde` null / `"2026-11-16"`. `RechazarAsync` sobre otra enviada: el espía no suma llamadas |
| T19 | `AgregarInfraestructura_ResuelveAprobacionContadorYPuerto` | 3,8,20 | integration | `ServiceProvider` con `AgregarInfraestructura` + `AddIdentidadPersonal` (como T42 de la 4.1); un alcance | `IAprobacionPropuestas` → `AprobacionPropuestasEnBase`, `IContadorDePropuestas` → `ContadorDePropuestasEnBase`, `ITurnosFueraDeAgenda` → `TurnosFueraDeAgendaSinTurnos` |
| T20 | `GetPropuestas_ConPendientes_ListaConEnlaceYContadorEnElMenu` | 1,2,3 | integration | `PersonalWebApplicationFactory`; `admin1`; 2 médicos propios `Prueba<marca>`: A enviada desde hoy + 30; B enviada hace 30 días desde hoy − 5 (con un reloj corrido, como la T44 de la 4.1); GET `/personal/admin/propuestas` | `200`; `<h1>Propuestas de agenda</h1>`; la fila de A tiene el enlace `Revisar` a `/personal/admin/propuestas/<idA>` y su `Rige desde`; la fila de B dice `La fecha de inicio ya pasó`; B está antes que A; el menú dice `Propuestas de agenda (N)` con N = `SELECT count(*) FROM agendas_medico WHERE estado = 'Enviada'` leído en la prueba |
| T21 | `TablaPropuestas_SegunFilasYVacio_MuestraLoQueCorresponde` | 1 | unit | bUnit: dos `FilaPropuesta` (una con `FechaDeInicioYaPaso = true`); y lista vacía | Dos filas en el orden recibido, con sus columnas y `La fecha de inicio ya pasó` sólo en la primera; vacía: `No hay propuestas esperando aprobación.` y ninguna `table` |
| T22 | `MenuLateral_SegunRolYPendientes_MuestraElContadorSoloAlAdministrador` | 3 | unit | bUnit con `ContadorDePropuestasFijo`; `[Theory]`: administrador con 3; administrador con 0; recepcionista con 3 | Admin 3: el enlace `/personal/admin/propuestas` dice `Propuestas de agenda (3)`; admin 0: dice `Propuestas de agenda` (sin paréntesis); recepcionista: no hay ese enlace y el doble tiene 0 llamadas; los `href` iguales a los de la T de la 2.1 |
| T23 | `GetDetalle_Enviada_MuestraVigenteYPropuestaLadoALado` | 4,25 | integration | `admin1`; médico propio con vigente (aprobada hace 30 días, desde hoy − 20: martes 14–18 y jueves 14–18, 20 min) y enviada desde hoy + 30 (martes 08–12 y jueves 14–18, 20 min) | `200`; `<h1>Propuesta de Prueba<marca>, Ana</h1>`; la línea con especialidad, `enviada` y `Rige desde`; las columnas `Vigente` y `Propuesta` con el resumen de la 4.2 (`24 turnos/semana` en las dos); `← cambia` sólo en la línea del martes de la propuesta; el botón `Aprobar` y el desplegable `Rechazar…` con el campo `Motivo del rechazo` (`maxlength="500"`). El `← cambia` lo dibuja `ResumenDeAgenda` con `CompararCon` (4.2): esta prueba verifica que A6 se lo pasa |
| T24 | `GetDetalle_FechaPasadaOReemplazo_MuestraElAvisoAntesDeLosBotones` | 6,7 | integration | `admin1`; `[Theory]`: (a) enviada hace 30 días desde hoy − 5; (b) aprobada desde hoy + 60 y enviada desde hoy + 30, del mismo médico | (a) el texto de la decisión 11 con la fecha y **ningún** botón `Aprobar`, sí `Rechazar…`; (b) `Hay una agenda aprobada que empieza el dd/MM/yyyy y va a reemplazar a esta.` con la fecha de hoy + 60, y el botón `Aprobar` presente; en los dos, el aviso aparece antes que los botones en el HTML |
| T25 | `GetDetalle_InexistenteONoEnviada_404ConCartel` | 5 | integration | `admin1`; `[Theory]`: id que no existe; un borrador propio; una aprobada propia | `404`; `Esa propuesta no existe o ya fue revisada.` y el enlace `Volver a Propuestas de agenda` |
| T26 | `PostAprobar_Enviada_VuelveAlListadoConElResultado` | 8,15 | integration | `admin1`; `[Theory]`: médico con usuario con email; médico sin usuario; `FormularioHttp.EnviarAsync(cliente, ruta del detalle, "aprobar", {})` y seguir la redirección | `302` a `/personal/admin/propuestas?resultado=aprobada` / `aprobada-sin-email`; el listado muestra `Propuesta aprobada. Le avisamos al médico por email.` / `Propuesta aprobada. El médico no tiene email cargado: lo va a ver en «Mi propuesta de agenda».`; en la base `Aprobada` y el registro con `quien = 'Lucía Pérez'` |
| T27 | `PostRechazar_ConMotivo_VuelveAlListadoYQuedaRechazada` | 9 | integration | `admin1`; enviada propia con usuario con email; POST `rechazar` con `Input.Motivo = "Proponé miércoles."` | `302` a `?resultado=rechazada`; el listado muestra `Propuesta rechazada. Le avisamos al médico por email, con el motivo.`; en la base `Rechazada` con ese motivo |
| T28 | `PostRechazar_MotivoInvalido_400ConCartelYLoEscrito` | 10 | integration | `admin1`; `[Theory]` motivo `"   "` y 501 letras; `accept: "text/html"` | `400`; el cartel con el texto del dominio; el `<details>` del rechazo con `open` y el `<textarea>` con lo escrito; en la base sigue `Enviada` |
| T29 | `PostAprobar_YaRevisada_409ConCartel` | 12 | integration | `admin1`; enviada propia aprobada antes desde `app.Services` con el caso de uso; POST `aprobar` desde la pantalla | `409`; `Sólo se puede aprobar una propuesta enviada.` en el cartel; un solo registro `propuesta-agenda.aprobada` con ese id |
| T30 | `PostRevisar_SinToken_400SinCambios` | 14 | integration | `admin1`; `[Theory]` aprobar y rechazar con `conToken: false` | `400`; en la base sigue `Enviada` |
| T31 | `GetDetalle_ConTurnosFuera_MuestraElAvisoYLaLista` | 21,22 | integration | `PersonalWebApplicationFactory` con `WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddScoped<ITurnosFueraDeAgenda>(_ => new TurnosFueraDeAgendaFijos(...))))` (como la 2.2 reemplaza un servicio); `[Theory]` con 2 turnos y con 1 | `⚠ 2 turnos ya dados quedan fuera de la nueva agenda. Se mantienen.` / `⚠ 1 turno ya dado queda fuera de la nueva agenda. Se mantiene.`; el desplegable `Ver` con `dd/MM/yyyy HH:mm · Apellido, Nombre` de cada turno; el botón `Aprobar` presente; POST `aprobar` → `302` (no bloquea) |
| T32 | `Detalle_OtroRolOAdminDesactivado_403` | 2,11 | integration | `[Theory]`: GET del detalle como `recepcion1`; GET como `medico1`; POST `aprobar` de un administrador propio que se desactiva en la base después de ingresar (cookie viva) | Los dos GET: `403` con `No tenés permiso para entrar a esta sección.`; el POST: `403` con `Sólo un administrador puede aprobar o rechazar propuestas de agenda.` en el cartel y la propuesta sigue `Enviada` |
| T33 | `AvisoTurnosFueraDeAgenda_SegunCantidad_MuestraElTextoQueCorresponde` | 22,26 | unit | bUnit; `[Theory]` lista vacía, 1 turno, 3 turnos | Vacía: markup vacío; 1: el texto en singular; 3: en plural; con turnos, `role="alert"` y una línea por turno en `<details>` |
| T34 | `GetMiPropuesta_Rechazada_MuestraQuienLaRechazo` | 17,25 | integration | Médico propio vinculado (`MedicoIngresadoAsync` de la 4.2); su enviada rechazada por `admin1` desde la pantalla (POST `rechazar`); el médico abre M2 (`RutasPersonal.MiPropuesta`) | `200`; el motivo y la línea completa `Rechazada el dd/MM/yyyy a las HH:mm por Lucía Pérez.` (fecha y hora local de `rechazada_el` leída de la base) |

Todas llevan `[Trait("Story", "4.3")]` y un `[Trait("AC", "n")]` por AC de su columna.
T1–T2 van en `HospitalTurnos.Dominio.Tests` (unitarias, con `RelojDePrueba` y `AgendaBuilder`).
T3–T19 en `HospitalTurnos.Aplicacion.Tests` (T17 sin base; las demás contra PostgreSQL real). T20–T34
en `HospitalTurnos.Web.Tests` (T21, T22 y T33 con bUnit; las demás contra la aplicación y PostgreSQL
reales).

> El plan estimaba 10 pruebas; quedan 34. La story junta cuatro cosas que tienen sus bordes: dos
> casos de uso con permiso, concurrencia y email (T7–T15), una pantalla con tres avisos y su 404
> (T20–T32), el contador en el menú (T4, T22) y el contrato de FR-15 con su trampa (T16–T18, T31,
> T33). Con la velocidad medida del S2 (el reuso pesó más que la cantidad de pruebas), el costo real
> depende más de lo que se reusa de la 4.2 (AC 25) que del número.

**Por qué no hay E2E (regla 6.1 del formato).** El proyecto E2E lo crea la 10.1. Las pantallas son
SSR sin JavaScript (`<details>` nativo): T20–T34 las verifican con `HttpClient` y bUnit. La prueba en
vivo con el navegador (Playwright, buscando por texto o rol, L-14) va en "Cómo verificar" y en la
Task 7, con la pasada del tester no técnico (B-6).

#### Given/Fixture

- **Reloj**: Dominio.Tests con `RelojDePrueba.En(...)` (4.1). Aplicacion.Tests con
  `DatosDeAgenda.Reloj(...)` y `DatosDeAgenda.RelojDeEnvio()` (viernes 16/10/2026 12:00 Córdoba, 4.1),
  registrado con `Proveedor(s => s.AddSingleton<TimeProvider>(reloj))` (lo último registrado gana).
  Las agendas que tienen que estar "ya pasadas" se **envían** con un reloj anterior y se leen o
  aprueban con uno posterior: ninguna prueba espera tiempo real (A-4). Web.Tests con `app.Reloj`
  (arranca en la hora real) y relojes corridos de él para enviar en el pasado; `app.Reloj` no se
  adelanta (cortaría la sesión), como la T44 de la 4.1.
- **Fechas de los ejemplos** (verificadas con `date`): viernes 16/10/2026, martes 20/10, miércoles
  21/10, viernes 23/10, lunes 02/11, lunes 09/11, lunes 16/11, lunes 23/11, lunes 30/11.
- **Base compartida**: cada prueba crea **sus** médicos (`DatosDeAgenda.MedicoPropioAsync`, 4.1, que
  usa `DatosDeCatalogo.CrearMedicoAsync` de la 3.2) y sus agendas
  (`DatosDeAgenda.GuardarAgendaAsync`, que llega a cada estado con los métodos públicos). Filtra
  auditoría por `entidad_tipo = 'AgendaMedico' AND entidad_id = '<id propio>'` y mensajes por
  `referencia_id = '<id propio>'`. Nunca limpia tablas. El contador (T4, T20) es global: se mide la
  diferencia o se compara con un `count(*)` leído en el mismo momento.
- **`Aplicacion.Tests/Agendas/DatosDePropuesta.cs`** (de la 4.2; I-5 de la revisión): el médico con
  usuario y email sale de su `MedicoConUsuarioAsync(proveedor, activo: true, email: …)`, que devuelve
  `(MedicoId, Actor, UsuarioId)` y fija el email con `UserManager.SetEmailAsync`. Esta story le
  **suma**, en el mismo archivo: `AdminPropioAsync(this ServiceProvider, string nombre)` (el
  `ActorAuditoria` de un administrador propio: `UsuariosDePrueba.CrearAsync` + `ActorDe`), y
  `RegistrosDe(fixture, agendaId)` y `MensajesDe(fixture, agendaId)`, que leen con `NpgsqlCommand` y
  `CadenaApp` (turnos_app tiene `SELECT` en las dos tablas). No se crea otro archivo de ayudas.
- **T13 — la carrera**: el `InterceptorQueSeAdelanta(cadena, sql)` de la 3.1 (sin cambios) con un
  `UPDATE` en lugar de un `INSERT`: cambia la fila por otra conexión justo antes del guardado, así
  `xmin` ya no coincide. Se agrega con
  `Proveedor(s => s.ConfigureDbContext<HospitalDbContext>(o => o.AddInterceptors(interceptor)))`.
  L-2: se comprueba que, sin la traducción del caso de uso, la prueba falla (sale
  `DbUpdateConcurrencyException`).
- **T18 — el espía**: `Agendas/Dobles/TurnosFueraDeAgendaEspia.cs`, a mano (sin Moq, contrato
  §Paquetes), con la forma de `EnviadorEmailFalso` de la 1.4: una lista configurable de turnos para
  devolver y una lista `Llamadas` de `(AgendaMedico Agenda, DateOnly? Hasta)` (guarda la agenda
  recibida, no sólo su id: la 4.4 lo reusa con una agenda todavía sin guardar, `Id = 0`, y verifica sus
  franjas). Registrado `Scoped` al
  final con `Proveedor(s => s.AddScoped<ITurnosFueraDeAgenda>(_ => espia))`.
- **T17**: idéntica en forma a `ContratoTurnosFuturosTests` de la 3.1 (`ContextoDePrueba.Crear` con
  una cadena sin servidor: leer el modelo no abre conexión).
- **`Web.Tests/Agendas/DatosDePropuesta.cs`** (de la 4.2): agendas con su
  `GuardarAgendaAsync(app, AgendaMedico agenda)` (la agenda se arma antes con los métodos públicos;
  las "ya pasadas", enviadas con un reloj de `RelojCorridoDias(app.Reloj, -30)`), médicos vinculados
  con `MedicoIngresadoAsync`. Esta story le **suma** `VincularMedicoConEmailAsync(app, medicoId, email)`
  (crea un usuario médico propio con ese email y lo vincula). Médicos sin usuario y especialidad con
  `DatosDeCatalogo` de Web.Tests (3.2); `admin1` ("Lucía Pérez") sólo ingresa y revisa propuestas
  propias.
- **T22 y la línea nueva de `MenuLateralTests`**: `Web.Tests/Ayudas/ContadorDePropuestasFijo.cs`
  (`IContadorDePropuestas` con un número fijo y un contador `Llamadas`).
- **T31**: `Web.Tests/Agendas/Dobles/TurnosFueraDeAgendaFijos.cs` devuelve los turnos que recibe en el
  constructor; se reemplaza con `WithWebHostBuilder` + `ConfigureTestServices`, como
  `UsuariosPersonalHttpTests` (2.2) reemplaza `IGestionUsuariosPersonal`.
- **T34**: usa M2 de la 4.2 (`MiPropuesta.razor`). El médico propio vinculado e ingresado sale de
  `MedicoIngresadoAsync(app)` de la 4.2: no se usa `medico1` (fijo, compartido y sin vínculo).
- **T15**: el `IPropuestaAgenda` de la 4.2 se resuelve del contenedor, un alcance por operación, con el
  actor que devuelve `MedicoConUsuarioAsync`. Los rechazos se hacen con `IAprobacionPropuestas` y los
  actores de `AdminPropioAsync("Ana Uno")` y `AdminPropioAsync("Beto Dos")`; la corrección del médico A,
  con `GuardarBorradorAsync` + `EnviarAsync` de la 4.2 (otra duración: 4.1 AC 12 pide un cambio).

## Tasks / Subtasks

- [ ] [SEQ] Task 1: Salvaguarda de estado (AC: todos) — **revalidar contra el código el día que se
      implementa** (A-1: esta story se escribió en paralelo con la 4.2; la revisión igualó los nombres)
  - [ ] Confirmar que la 4.2 está en `main` con lo que esta story usa de ella (tabla del AC 25):
        `ResumenDeAgenda` con `Agenda`, `Forma` y `CompararCon`; `MiPropuesta.razor` en
        `Components/Personal/Agendas/` con la línea `Rechazada el dd/MM/yyyy a las HH:mm.`;
        `VistaPropuesta` y `PropuestaAgendaEnBase.ObtenerAsync`; `DatosDePropuesta` en los dos proyectos
        de pruebas; el índice `ux_agendas_medico_una_editable`; `@using HospitalTurnos.Aplicacion.Agendas`
        en `_Imports.razor`. Si algo no coincide → DETENERSE y corregir esta story antes de programar.
  - [ ] Confirmar de la 4.1 lo que usa esta story: `AgendaMedico.Aprobar/Rechazar/AtiendeEn/TurnosPorSemana/QueRigeEn`,
        `IConsultaAgendas.AgendaQueRigeAsync`, `xmin` en `agendas_medico`, `DatosDeAgenda` y
        `AgendaBuilder`; de la 2.2: `IActorVigente`, `ActorDeSesion.Desde`, `FijarEstadoEnPagina`,
        `FormularioHttp.EnviarAsync`, `UsuariosDePrueba`; de la 3.1: `InterceptorQueSeAdelanta`; de la
        1.4: `IBandejaDeSalida.Encolar` y `MensajeEmail`.
  - [ ] Confirmar que `SeccionesAdministracion.razor` todavía tiene `@page "/personal/admin/propuestas"`
        y que no existe nada de `files_to_create`.
  - [ ] Las decisiones 1, 7, 8, 9 y 25 están tomadas (ver "Decisiones tomadas por el docente").
  - [ ] `bash scripts/ci/local.sh` en verde antes de empezar. Si algo no coincide → DETENERSE.
- [ ] [SEQ] Task 2: Dominio, pruebas primero (AC: 18, 19)
  - [ ] T1–T2 en rojo; `FechaDeInicioYaPaso` y `ProximaAprobadaDespuesDe` en `AgendaMedico`;
        `Aprobar` usa `FechaDeInicioYaPaso`. T1–T2 y las de la 4.1 en verde.
- [ ] [SEQ] Task 3: Aplicacion — los puertos (AC: 1, 3–5, 8–9, 17, 20)
  - [ ] `IAprobacionPropuestas` con sus registros, `IContadorDePropuestas` e `ITurnosFueraDeAgenda`
        con `TurnoFueraDeAgenda` (firma exacta del AC 20), cada uno con el comentario de quién lo usa.
  - [ ] `VistaPropuesta` (4.2) suma `string? RechazadaPor = null` al final (AC 17).
- [ ] [SEQ] Task 4: Infraestructura — casos de uso y puerto de hoy (AC: 1, 3–13, 15–17, 20, 21, 23, 24, 29)
  - [ ] `TurnosFueraDeAgendaSinTurnos`, `ContadorDePropuestasEnBase`, `AprobacionPropuestasEnBase`
        (ver Implementación concreta) y su registro `Scoped` en `AgregarInfraestructura`.
  - [ ] `PropuestaAgendaEnBase.ObtenerAsync` (4.2) carga `RechazadaPor` (AC 17).
  - [ ] Las ayudas que esta story suma a `DatosDePropuesta` (Aplicacion.Tests), `TurnosFueraDeAgendaEspia`
        y T3–T19 en verde. En T13, romper a propósito la traducción y ver que falla (L-2).
- [ ] [SEQ] Task 5: Web — A6, contador y M2 (AC: 1–17, 22)
  - [ ] `RutasPersonal.Propuesta(long id)`. `_Imports.razor` ya tiene los dos `@using` de agendas (los
        agrega la 4.2): esta story no lo toca.
  - [ ] Quitar `@page "/personal/admin/propuestas"` de `SeccionesAdministracion.razor`.
  - [ ] `TablaPropuestas`, `Propuestas`, `FormularioRechazo`, `RevisarPropuesta` en
        `Components/Personal/Administracion/`; `AvisoTurnosFueraDeAgenda` en
        `Components/Personal/Agendas/` (junto al resumen de la 4.2); `ContadorPropuestasPendientes` y
        su lugar en `MenuLateral`.
  - [ ] En M2 (`MiPropuesta.razor`, 4.2), la línea del rechazo con ` por <RechazadaPor>` cuando hay
        dato (AC 17).
- [ ] [SEQ] Task 6: Pruebas de Web (AC: todos)
  - [ ] `/personal/admin/propuestas` en `RutasYaConstruidas` (2.1 T19); la línea de armado en
        `MenuLateralTests`.
  - [ ] `VincularMedicoConEmailAsync` en `DatosDePropuesta` (Web.Tests), `TurnosFueraDeAgendaFijos`,
        `ContadorDePropuestasFijo`, T20–T34 en verde.
  - [ ] `dotnet test` completo y `bash scripts/ci/local.sh` sin regresiones; anotar la cobertura.
- [ ] [SEQ] Task 7: Verificación en vivo y pasada del tester no técnico (AC: todos con pantalla) — B-6
  - [ ] Guía `docs/qa/4.3-pasos.md` (como las de la 3.1 y la 3.2) a partir de "Cómo verificar", paso 4.
  - [ ] Script de Playwright contra el Compose que busca **por texto o rol** (L-14: el menú repite el
        enlace "Propuestas de agenda"; usar el enlace dentro de `main` o el `href`), registrado con
        `smoke-run.sh`, con la matriz por AC.
  - [ ] Pasada del tester no técnico sobre la guía (`docs/qa/4.3-legibilidad.md`) y sobre las
        pantallas A6 y M2 (`docs/qa/4.3-legibilidad-ui.md`); corregir la guía con sus marcas y llevar
        al docente lo que toque las pantallas.

### Task Dependencies

T1 → T2 → T3 → T4 → T5 → T6 → T7 (secuencial). T2 no necesita Docker. Dentro de la T5,
`TablaPropuestas`, `AvisoTurnosFueraDeAgenda` y sus pruebas bUnit (T21, T33) se pueden hacer en
paralelo con el resto. Fuera de esta story se respeta el orden del sprint (2.3 → 4.2 → 4.3 → 4.4 →
5.1): la 5.1 toca archivos que también tocan la 4.2 y la 4.3 (`SesionYPermisosHttpTests`,
`_Imports.razor`, `ServiciosDeInfraestructura`, `RutasPersonal`, `IActorVigente`), y en paralelo
habría conflictos al juntar las ramas (M-4 de la revisión).

## Dev Notes

### Que existe hoy

#### En hospital-turnos (verificado 2026-10-17 contra `main` `a8432c3`, con 1.1 a 4.1 en done; la 4.2 todavía no existe)

- **Dominio de agenda (4.1)**, `src/HospitalTurnos.Dominio/Agendas/AgendaMedico.cs`:
  `Aprobar(TimeProvider reloj)` (409 si no está `Enviada`; 409 con
  `La agenda iba a regir desde el {dd/MM/yyyy}, que ya pasó: rechazala para que el médico elija otra fecha.`
  si `VigenteDesde < Hoy(reloj)`), `Rechazar(string motivo, TimeProvider reloj)` (recorta; vacío y
  más de 500 → 400), `AtiendeEn(DateTime)`, `TurnosPorSemana()`, `static QueRigeEn(agendas, fecha)`
  y el privado `static Hoy(reloj)` = fecha local. **No** existen `FechaDeInicioYaPaso` ni
  `ProximaAprobadaDespuesDe`. Propiedades: `Id`, `MedicoId` (long), `DuracionTurnoMinutos`,
  `VigenteDesde` (`DateOnly`), `Estado` (`EstadoPropuesta`), `MotivoRechazo`, `EnviadaEl`,
  `AprobadaEl`, `RechazadaEl` (`DateTimeOffset?`, UTC), `Franjas`.
- **`IConsultaAgendas`** (Aplicacion/Agendas): `AgendaQueRigeAsync(long medicoId, DateOnly fecha, ct)`
  (`AsNoTracking`, con franjas) y `EstadosDeAgendaAsync(ids, ct)`. Implementación
  `ConsultaAgendasEnBase(HospitalDbContext db, TimeProvider reloj)`, `Scoped`.
- `agendas_medico` tiene concurrencia optimista con `xmin` (propiedad sombra `Version`, 4.1 AC 27);
  `turnos_app` tiene `SELECT, INSERT, UPDATE` (sin `DELETE`). T38 de la 4.1 ya probó que dos
  guardados sobre la misma agenda dan `DbUpdateConcurrencyException`.
- **`AccionesAuditoria`** ya tiene `PropuestaAgendaAprobada = "propuesta-agenda.aprobada"` y
  `PropuestaAgendaRechazada = "propuesta-agenda.rechazada"` (1.3). `IAuditoria.Registrar(actor, accion,
  entidadTipo, entidadId, detalle)` agrega sin guardar. `RegistroAuditoria` tiene `Quien`, `Accion`,
  `EntidadTipo`, `EntidadId`, `Cuando`, `Detalle` (jsonb); `turnos_app` tiene `INSERT` y `SELECT`.
  No hay índice por entidad en `registros_auditoria`.
- **Bandeja de salida (1.4)**: `IBandejaDeSalida.Encolar(MensajeEmail)` agrega sin guardar;
  `MensajeEmail(Para, Asunto, CuerpoTexto, CuerpoHtml, Tipo, ReferenciaId?)`; `Tipo` hasta 50
  caracteres. `BandejaDeSalidaEnBase` no valida la dirección; el procesador da un destinatario
  inválido por perdido sin intentar (1.4 AC 8) y, sin manejador para el tipo, deja un `Warning`. La
  1.4 dejó las constantes de `Tipo` a cada story. No hay ningún `Tipo` de agenda.
- **Email del médico — no hay ninguno cargado.** `Medico` (3.2) no tiene email; guarda
  `UsuarioPersonalId` (`Guid?`). `UsuarioPersonal : IdentityUser<Guid>` hereda `Email` (columna
  `email` de `asp_net_users`, en el snapshot), pero `RegistroIdentidad` pone
  `RequireUniqueEmail = false` con el comentario "el email no es obligatorio ni único", el alta de A4
  es `NuevoUsuarioPersonal(NombreCompleto, Usuario, Rol)` (sin email) y `SembradorPersonal` tampoco
  lo carga. Por eso la decisión 1: esta story lo lee y la 2.4 (sprint 4) lo carga en A4 (AC 29).
- **`IActorVigente.ExigirAdministradorActivoAsync(actor, mensajeSiNoLoEs, ct)`** (2.2) devuelve el
  actor con el nombre leído de la base.
- **Web**: `RutasPersonal.Propuestas = "/personal/admin/propuestas"`, título `Propuestas de agenda`,
  segunda sección del administrador. `SeccionesAdministracion.razor` atiende hoy
  `/personal/admin/propuestas`, `/bloqueos` y `/auditoria` con `SeccionEnConstruccion`. `MenuLateral`
  dibuja `seccion.Titulo` como texto del enlace y no inyecta servicios. `LayoutPersonal` lo dibuja en
  cada pantalla (SSR: se recalcula en cada pedido). `_Imports.razor` no tiene
  `HospitalTurnos.Aplicacion.Agendas` en `main`; lo agrega la 4.2, que va antes. Patrón de página con formulario, 404 y cartel:
  `Components/Personal/Administracion/FichaMedico.razor` (3.2).
- **Pruebas que esta story toca**: `SesionYPermisosHttpTests` (2.1 T19) tiene
  `RutasYaConstruidas = [usuarios, especialidades, medicos]` y exige `<h1>{Titulo}</h1>`;
  `MenuLateralTests` arma bUnit sólo con `AntiforgeryStateProvider` y autorización (si el menú
  inyecta un servicio que no está, falla al dibujar).
- **Ayudas de prueba existentes**: `DatosDeAgenda` (`Proveedor`, `Reloj`, `RelojDeEnvio`,
  `MedicoPropioAsync`, `GuardarAgendaAsync`, `ArmarAgenda`, `Franja` — esta última sólo conoce
  lunes, martes y miércoles: las pruebas de Aplicacion usan esos días), `UsuariosDePrueba`
  (`CrearAsync(proveedor, roles, activo, nombre)`, sin email), `InterceptorQueSeAdelanta(cadena, sql)`,
  `ContextoDePrueba.Crear`, `ContratoTurnosFuturosTests` (la trampa T15 de la 3.1); en Web.Tests,
  `PersonalWebApplicationFactory` (`admin1` = "Lucía Pérez", `app.Reloj`), `FormularioHttp`
  (`EnviarAsync`, `Alerta`, `Estado`, `CrearUsuarioAsync`), `DatosDeCatalogo`.
- **Decisión 5 de la 3.1**: descartó "la interfaz contador de turnos que devuelve 0" por ser un stub
  en producción. Esta story sí crea un puerto que hoy devuelve vacío, porque el resumen común del
  sprint 3 lo fija así (ver "Decisiones de esta story").

### Patron de referencia verificado

**Patrón 1 — caso de uso con permiso, auditoría y guardado único** (este repo):

| Proyecto | Ancla | Qué se verificó exactamente |
|---|---|---|
| hospital-turnos | `src/HospitalTurnos.Infraestructura/Catalogo/GestionMedicosEnBase.cs`, método `EditarAsync` | Primero `actorVigente.ExigirAdministradorActivoAsync`, después cargar con seguimiento (404 si no existe), cambiar con el dominio, `auditoria.Registrar` y **un** guardado que traduce el rechazo de la base a la regla. `AprobacionPropuestasEnBase` sigue ese orden; traduce `DbUpdateConcurrencyException` en lugar del `23505`. |
| hospital-turnos | `src/HospitalTurnos.Infraestructura/Email/BandejaDeSalidaEnBase.cs` | `Encolar` agrega al mismo `HospitalDbContext`: el email se guarda con el cambio o no se guarda. |

**Patrón 2 — página SSR con formulario y cartel** (este repo):

| Proyecto | Ancla | Qué se verificó exactamente |
|---|---|---|
| hospital-turnos | `src/HospitalTurnos.Web/Components/Personal/Administracion/FichaMedico.razor` | `[SupplyParameterFromForm(FormName = ...)]`, `catch (Exception e) when (ManejadorGlobalDeErrores.EsEsperable(e))` + `FijarEstadoEnPagina`, 404 con cartel por `FijarEstadoEnPagina`, `NavigateTo` fuera del `try`, `?resultado=` con textos fijos, formularios dibujados también en un POST sin datos. `RevisarPropuesta` tiene dos formularios (`aprobar`, `rechazar`). |
| hospital-turnos | `src/HospitalTurnos.Web/Components/Personal/Administracion/TablaMedicos.razor` + `tests/.../Catalogo/TablaMedicosTests.cs` | La tabla como componente aparte para probar el vacío con bUnit (por HTTP no se ve en una base compartida). Se toma para `TablaPropuestas` (T21). |

**Patrón 3 — prueba-trampa** (este repo): `tests/HospitalTurnos.Aplicacion.Tests/Catalogo/ContratoTurnosFuturosTests.cs`
recorre `contexto.Model.GetEntityTypes()` buscando `ClrType.Name == "Turno" || GetTableName() == "turnos"`
y falla con un mensaje que nombra la obligación. T17 es la misma forma con otro mensaje.

**Patrón 4 — doble a mano que espía** (este repo y el guía): `tests/HospitalTurnos.Aplicacion.Tests/Email/Dobles/EnviadorEmailFalso.cs`
(1.4, tomado de `RepositorioVentasEnMemoria` de changomas-testing-csharp): implementa el puerto y
anota lo recibido. Se toma para `TurnosFueraDeAgendaEspia`.

### Que hay que hacer (la delta)

1. Dominio: dos métodos en `AgendaMedico` (AC 18, 19).
2. Aplicacion: tres puertos (`IAprobacionPropuestas`, `IContadorDePropuestas`, `ITurnosFueraDeAgenda`).
3. Infraestructura: los dos casos de uso con permiso, auditoría, email y concurrencia; el contador; la
   implementación de hoy de FR-15.
4. Web: A6 (listado y detalle), el contador en el menú, el aviso de FR-15 como componente
   reutilizable y el nombre de quien rechazó en M2.
5. El contrato de FR-15 con la 4.4 y la 5.3, con su trampa.

### Decisiones de esta story (con su porqué)

- **El puerto de FR-15 que hoy devuelve vacío no es un stub, y difiere a propósito de la decisión 5 de
  la 3.1.** La 3.1 descartó un "contador de turnos que devuelve 0" porque nadie más lo necesitaba: la
  regla se podía escribir entera en la 5.3. Acá el resumen común del sprint 3 pide el puerto, y hay
  dos razones que en la 3.1 no estaban: (1) la 4.4 tiene que llamar **al mismo** punto, y si no
  existe con nombre y firma, cada una inventa el suyo (I-1 del S2); (2) la pantalla A6 ya tiene el
  lugar del aviso, y sin el puerto ese lugar no se podría probar (T31). Lo que lo separa de un stub
  es lo mismo que en la 3.1: hoy no puede haber turnos, la respuesta vacía es la verdadera, y la
  trampa T17 obliga a reemplazarla el día que aparezcan.
- **El puerto recibe la agenda nueva, no una lista de horarios.** Así la regla "¿este turno entra?"
  sigue siendo `AtiendeEn` de la 4.1, una sola vez; la 5.3 sólo trae los turnos del período.
- **`hasta` lo calcula quien llama, con una regla del dominio** (`ProximaAprobadaDespuesDe`). Es el
  mismo dato que el aviso de la decisión 13: calcularlo dos veces en dos lugares daría dos respuestas.
- **El puerto se consulta otra vez al aprobar.** Entre que el administrador abrió A6 y pulsó Aprobar
  pudo reservarse un turno; la auditoría guarda lo que había al momento de aprobar.
- **El detalle sólo muestra propuestas enviadas** (AC 5). Un borrador es trabajo del médico en curso;
  una ya revisada no tiene nada que hacer en A6. En un POST, en cambio, responde el dominio con su
  409, que explica mejor qué pasó ("Sólo se puede aprobar una propuesta enviada.").
- **Botón Aprobar oculto con la fecha pasada, no deshabilitado** (AC 6). Un botón deshabilitado sin
  explicación confunde; el aviso dice qué hacer (rechazar) y el botón que sí sirve queda solo.
- **El contador en un puerto propio y un componente propio.** El menú se dibuja en todas las
  pantallas de todos los roles: un componente que sólo aparece en la sección del administrador evita
  una consulta para recepción y médicos, y un puerto de un método se reemplaza fácil en bUnit (la
  línea nueva de `MenuLateralTests`).
- **Email desde Infraestructura, leyendo `contexto.Users`.** `GestionMedicosEnBase` ya lee las tablas
  de Identity del mismo contexto (`Vinculables()`); el dominio no conoce a Identity y no hace falta
  otro puerto para un dato.
- **Sin manejador de fallo definitivo para estos emails.** El médico ve el estado en M2 (FR-14e: "y ve
  el estado en el sistema"). Un aviso a la recepción, como los de bloqueo (7.2), no aporta.
- **La dirección no va a la auditoría ni a los registros técnicos** (Ley 25.326, como la 1.4): el
  detalle anota sólo `emailEncolado`.
- **Quién rechazó viaja en `VistaPropuesta.RechazadaPor`, no en un método del puerto del
  administrador** (I-1 de la revisión): M2 es del médico y lee todo por `IPropuestaAgenda`; inyectarle
  el puerto de A6 mezclaría las dos pantallas. El dato va al final del registro con valor por defecto,
  así los `new VistaPropuesta(...)` de la 4.2 no cambian.
- **Sin índice nuevo en `registros_auditoria`** para leer quién rechazó. Se consulta sólo cuando M2
  muestra un rechazo, y agregar índices a la tabla de auditoría es de la 9.1 (A5), que filtra por
  entidad. Así esta story no tiene migración.
- **Rechazar no traduce el `23505` de `ux_agendas_medico_una_editable`** (M-10 de la revisión,
  aceptado por escrito). Al rechazar, la enviada pasa a `Rechazada` y ocupa el lugar de la editable;
  la 4.2 garantiza que mientras hay una enviada el médico no tiene otra editable (su AC 18). Sólo con
  datos armados a mano (un `Borrador` insertado al lado de una `Enviada`) rechazar daría el `500`
  genérico; no se agrega código para un caso que la aplicación no puede producir.
- **Sin confirmación extra para aprobar con turnos fuera** (FR-15, decisión 25). El aviso está arriba
  del botón y el botón **es** la decisión ("decide si igual aprueba"). La 4.4 sí pide `Corregir igual`
  con el mismo puerto y el mismo componente, porque allí la agenda se escribe en el mismo formulario.

### Decisiones tomadas por el docente (2026-10-17)

Numeradas como en `revision-sprint-3.md` ("Decisiones abiertas consolidadas"). Se aceptaron todas con
la opción recomendada; los AC y las pruebas ya las reflejan.

- **Decisión 1 — ¿De dónde sale el email del médico?** (era D-1, junto con la D-3 de la 4.4; AC 15,
  16, 29). **Elegido:** el `Email` del usuario médico vinculado (`UsuarioPersonal.Email`). Esta story lo
  **lee**; cargarlo es de una story corta nueva, la **2.4 "Email del personal en A4"** (sprint 4). Hasta
  entonces FR-14e queda a medias: un médico sin email se revisa igual, sin error, y ve el resultado en
  M2 (AC 16). El contrato con la 2.4 está en el AC 29. La 4.4 no manda email.
- **Decisión 7 — ¿Cómo se muestra quién rechazó en M2?** (era D-2; AC 17). **Elegido:** el nombre
  completo de la auditoría, en una sola línea con el formato de la 4.2:
  `Rechazada el dd/MM/yyyy a las HH:mm por Lucía Pérez.` (la UX dibuja "— Lucía P., 12/11 10:05"; el
  nombre completo evita adivinar el apellido).
- **Decisión 8 — El contador con cero pendientes** (era D-3; AC 3). **Elegido:** sin número:
  `Propuestas de agenda`.
- **Decisión 9 — Orden del listado A6** (era D-4; AC 1). **Elegido:** por **Rige desde**, la más
  próxima primero (empate: la enviada antes); las de fecha pasada quedan arriba para rechazarlas.
- **Decisión 25 — Turnos fuera de agenda: ¿confirmación extra?** (de la revisión, I-2; AC 22, 26).
  **Elegido:** A6 sin paso extra (el aviso está antes del botón); la corrección de la 4.4 con
  `Corregir igual`. Las dos con el mismo puerto y el mismo componente de aviso.

### Implementacion concreta

**Dominio — `AgendaMedico` (dos métodos; `Aprobar` usa el primero)**:

```csharp
/// <summary>Decisión 11: la fecha de inicio ya pasó (fecha LOCAL del hospital). La usan Aprobar, A6 y su listado.</summary>
public bool FechaDeInicioYaPaso(TimeProvider reloj) => VigenteDesde < Hoy(reloj);

public void Aprobar(TimeProvider reloj)
{
    if (Estado != EstadoPropuesta.Enviada) throw new ConflictoException(SoloAprobarEnviada);
    if (FechaDeInicioYaPaso(reloj))
        throw new ConflictoException(
            $"La agenda iba a regir desde el {Fecha(VigenteDesde)}, que ya pasó: rechazala para que el médico elija otra fecha.");
    Estado = EstadoPropuesta.Aprobada;
    AprobadaEl = reloj.GetUtcNow();
}

/// <summary>
/// La aprobada que empieza después de la fecha y, por lo tanto, corta a una agenda que rige desde ella
/// (Arq. §6, decisión 13). Con la misma VigenteDesde, la aprobada después (como QueRigeEn). La usan la 4.3 y la 4.4.
/// </summary>
public static AgendaMedico? ProximaAprobadaDespuesDe(IEnumerable<AgendaMedico> agendas, DateOnly fecha) =>
    agendas.Where(a => a.Estado == EstadoPropuesta.Aprobada && a.VigenteDesde > fecha)
           .OrderBy(a => a.VigenteDesde)
           .ThenByDescending(a => a.AprobadaEl)
           .FirstOrDefault();
```

**Aplicacion — los puertos**:

```csharp
// Aplicacion/Agendas/IAprobacionPropuestas.cs
/// <summary>
/// A6 (story 4.3, FR-14c/e): las propuestas enviadas, su comparación con la vigente y su revisión. Las
/// escrituras confirman en la base que el actor es un administrador activo (decisión 16). Quién rechazó
/// NO está acá: M2 lo lee de VistaPropuesta.RechazadaPor (4.2, AC 17).
/// </summary>
public interface IAprobacionPropuestas
{
    /// <summary>Todas las enviadas, por VigenteDesde y, empatadas, por envío (decisión 9).</summary>
    Task<IReadOnlyList<FilaPropuesta>> ListarPendientesAsync(CancellationToken ct);

    /// <summary>La propuesta ENVIADA con su vigente y sus avisos; null si no existe o no está enviada (AC 5).</summary>
    Task<DetallePropuesta?> ObtenerAsync(long agendaId, CancellationToken ct);

    Task<RevisionHecha> AprobarAsync(long agendaId, ActorAuditoria actor, CancellationToken ct);

    Task<RevisionHecha> RechazarAsync(long agendaId, string? motivo, ActorAuditoria actor, CancellationToken ct);
}

public sealed record FilaPropuesta(long AgendaId, string Medico, string Especialidad, DateTime EnviadaElLocal,
    DateOnly VigenteDesde, bool FechaDeInicioYaPaso);

public sealed record DetallePropuesta(long AgendaId, string Medico, string Especialidad, DateTime EnviadaElLocal,
    AgendaMedico Propuesta, AgendaMedico? Vigente, bool FechaDeInicioYaPaso, DateOnly? ReemplazadaDesde,
    IReadOnlyList<TurnoFueraDeAgenda> TurnosFueraDeAgenda);

/// <summary>AvisoPorEmail = se encoló el email al médico (si no, la pantalla dice "sin email", AC 16).</summary>
public sealed record RevisionHecha(bool AvisoPorEmail);

// Aplicacion/Agendas/IContadorDePropuestas.cs — el número del menú (AC 3).
public interface IContadorDePropuestas
{
    Task<int> PendientesAsync(CancellationToken ct);
}

// Aplicacion/Agendas/ITurnosFueraDeAgenda.cs — firma del AC 20, tal cual (contrato con la 4.4 y la 5.3).
```

**Infraestructura — `AprobacionPropuestasEnBase`** (el núcleo de aprobar; rechazar es igual sin el
puerto de FR-15):

```csharp
public sealed class AprobacionPropuestasEnBase(
    HospitalDbContext contexto,
    IAuditoria auditoria,
    IActorVigente actorVigente,
    IConsultaAgendas consultaAgendas,
    ITurnosFueraDeAgenda turnosFueraDeAgenda,
    IBandejaDeSalida bandeja,
    TimeProvider reloj) : IAprobacionPropuestas
{
    private const string SinPermiso = "Sólo un administrador puede aprobar o rechazar propuestas de agenda.";
    private const string NoExiste = "Esa propuesta no existe o ya fue revisada.";
    private const string OtroSeAdelanto =
        "Otro administrador acaba de revisar esta propuesta. Volvé a Propuestas de agenda para ver cómo quedó.";
    private const string EntidadTipo = "AgendaMedico";
    public const string TipoEmailAprobada = "propuesta-agenda-aprobada";   // la 1.4 deja los tipos a cada story
    public const string TipoEmailRechazada = "propuesta-agenda-rechazada";

    public async Task<RevisionHecha> AprobarAsync(long agendaId, ActorAuditoria actor, CancellationToken ct)
    {
        actor = await actorVigente.ExigirAdministradorActivoAsync(actor, SinPermiso, ct);   // AC 11: antes de leer nada
        var agenda = await contexto.AgendasMedico.Include(a => a.Franjas)
            .FirstOrDefaultAsync(a => a.Id == agendaId, ct) ?? throw new NoEncontradoException(NoExiste);
        agenda.Aprobar(reloj);                                                              // 409 del dominio
        var hasta = (await AprobadasPosterioresAsync(agenda, ct))?.VigenteDesde;           // AC 19 y 21
        var fuera = await turnosFueraDeAgenda.BuscarAsync(agenda, hasta, ct);              // FR-15: informa, no bloquea
        var email = await EmailDelMedicoAsync(agenda.MedicoId, ct);                         // null → sin email (AC 16)
        if (email is not null)
            bandeja.Encolar(EmailAprobada(email, agenda));                                  // mismo guardado (ADR-05)
        auditoria.Registrar(actor, AccionesAuditoria.PropuestaAgendaAprobada, EntidadTipo, Texto(agenda.Id), new
        {
            medicoId = agenda.MedicoId,
            vigenteDesde = agenda.VigenteDesde.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            turnosFueraDeAgenda = fuera.Count,
            reemplazadaDesde = hasta?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            emailEncolado = email is not null,
        });
        await GuardarTraduciendoConcurrenciaAsync(ct);                                     // todo junto o nada
        return new(email is not null);
    }

    // AC 13: el otro administrador guardó primero (xmin, 4.1 AC 27). Nada de esta revisión queda guardado.
    private async Task GuardarTraduciendoConcurrenciaAsync(CancellationToken ct)
    {
        try { await contexto.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ConflictoException(OtroSeAdelanto); }
    }

    // El usuario médico vinculado y su Email (decisión 1; lo carga la 2.4). Sin vínculo, sin email o en blanco → null.
    private async Task<string?> EmailDelMedicoAsync(long medicoId, CancellationToken ct) { /* contexto.Medicos join contexto.Users */ }
}
```

- `ObtenerAsync`: una consulta trae la enviada con franjas y los datos del médico y su especialidad
  (join, como `GestionMedicosEnBase.ObtenerAsync`); `Vigente` =
  `consultaAgendas.AgendaQueRigeAsync(medicoId, hoy, ct)` (reuso, no otra consulta de vigencia);
  otra consulta trae las aprobadas del médico con `VigenteDesde > propuesta.VigenteDesde` (sin
  franjas) para `ProximaAprobadaDespuesDe`. `EnviadaElLocal` =
  `TimeZoneInfo.ConvertTime(EnviadaEl, reloj.LocalTimeZone).DateTime`.
- `ListarPendientesAsync`: una consulta (agendas `Enviada` + médico + especialidad, `AsNoTracking`);
  `FechaDeInicioYaPaso` con el método del dominio sobre cada agenda.
- **Quién rechazó (AC 17), en `PropuestaAgendaEnBase.ObtenerAsync` de la 4.2**: si `EnCurso` tiene
  `MotivoRechazo`, `RechazadaPor = contexto.RegistrosAuditoria.Where(r => r.Accion == AccionesAuditoria.PropuestaAgendaRechazada
  && r.EntidadTipo == "AgendaMedico" && r.EntidadId == enCurso.Id.ToString(CultureInfo.InvariantCulture)).OrderByDescending(r => r.Cuando).ThenByDescending(r => r.Id).Select(r => r.Quien).FirstOrDefaultAsync(ct)`;
  si no, `null`. El registro `VistaPropuesta` suma `string? RechazadaPor = null` al final. En
  `MiPropuesta.razor`, la línea es `Rechazada el {fecha} a las {hora}`, más ` por {RechazadaPor}` si
  hay dato, más el punto final.
- **Emails** (texto y HTML con los mismos datos; en el HTML, `WebUtility.HtmlEncode` del nombre y del
  motivo):

  ```text
  Asunto: Tu agenda fue aprobada
  Hola, {Nombre} {Apellido}.

  La administración del hospital aprobó tu propuesta de agenda. Rige desde el {dd/MM/yyyy}: turnos de {N} minutos, {T} turnos por semana.

  Podés verla en el sistema del personal, en «Mi propuesta de agenda».
  ```

  ```text
  Asunto: Tu propuesta de agenda fue rechazada
  Hola, {Nombre} {Apellido}.

  La administración del hospital rechazó tu propuesta de agenda que iba a regir desde el {dd/MM/yyyy}.

  Motivo: {motivo}

  Tu agenda vigente no cambia. Podés corregir la propuesta y reenviarla en «Mi propuesta de agenda».
  ```

**Infraestructura — `TurnosFueraDeAgendaSinTurnos`**:

```csharp
/// <summary>
/// FR-15 mientras no existan los turnos (story 4.3, AC 20): ningún turno puede quedar fuera de una agenda nueva
/// porque todavía no hay turnos. No es un stub: es la respuesta verdadera de hoy. La story 5.3 la reemplaza por la
/// consulta real (AC 27) y la prueba-trampa ContratoTurnosFueraDeAgendaTests se lo recuerda.
/// </summary>
public sealed class TurnosFueraDeAgendaSinTurnos : ITurnosFueraDeAgenda
{
    public Task<IReadOnlyList<TurnoFueraDeAgenda>> BuscarAsync(AgendaMedico agendaNueva, DateOnly? hasta,
        CancellationToken ct) => Task.FromResult<IReadOnlyList<TurnoFueraDeAgenda>>([]);
}
```

**Registro en `AgregarInfraestructura`**: `AddScoped<IAprobacionPropuestas, AprobacionPropuestasEnBase>()`,
`AddScoped<IContadorDePropuestas, ContadorDePropuestasEnBase>()`,
`AddScoped<ITurnosFueraDeAgenda, TurnosFueraDeAgendaSinTurnos>()`.

**Web — menú**: en el `foreach` de `MenuLateral`, dentro del enlace:
`@seccion.Titulo@if (seccion.Ruta == RutasPersonal.Propuestas) { <ContadorPropuestasPendientes /> }`.
`ContadorPropuestasPendientes` inyecta `IContadorDePropuestas`, lee en `OnInitializedAsync` y dibuja
` (N)` sólo si N > 0 (decisión 8).

**Web — `RevisarPropuesta.razor`** (`@page "/personal/admin/propuestas/{Id:long}"`, `[Authorize(Roles = Administrador)]`):
dos formularios con nombre, `aprobar` (sin campos) y `rechazar` (`FormularioRechazo` con
`string? Motivo`); los dos atrapan como `FichaMedico`
(`catch (NoEncontradoException)` → el 404 de la página; `catch (Exception e) when (EsEsperable(e))` →
`FijarEstadoEnPagina` + cartel); el de rechazo vuelve a dibujar el `<details open>` con lo escrito.
`NavigateTo($"{RutasPersonal.Propuestas}?resultado=...")` fuera del `try`. `Propuestas.razor` mapea
`resultado` a los cuatro textos fijos de los AC 8 y 9 (cualquier otro valor: nada).

### Salvaguarda de BD

Esta story **no** tiene migración: no hay `dotnet ef migrations add` ni cambios en el snapshot. Si al
implementar aparece la necesidad de una, DETENERSE y corregir la story
primero. Igual se corre, contra el Compose, la comprobación de que `turnos_app` puede lo que la story
necesita:

```sql
SELECT has_table_privilege('turnos_app','agendas_medico','UPDATE'),       -- t
       has_table_privilege('turnos_app','registros_auditoria','SELECT'),  -- t
       has_table_privilege('turnos_app','registros_auditoria','UPDATE'),  -- f (no regresión 1.3)
       has_table_privilege('turnos_app','mensajes_salientes','INSERT');   -- t
```

### Archivos a crear/modificar

| Acción | Archivo |
|---|---|
| Crear | Los de `files_to_create` del frontmatter |
| Modificar | `src/HospitalTurnos.Dominio/Agendas/AgendaMedico.cs` (AC 18, 19; `Aprobar` usa `FechaDeInicioYaPaso`) |
| Modificar | `src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs` (tres `AddScoped`) |
| Modificar | `src/HospitalTurnos.Web/Personal/RutasPersonal.cs` (`Propuesta(long id)`) |
| Modificar | `src/HospitalTurnos.Web/Components/Personal/MenuLateral.razor` (el contador) |
| Modificar | `src/HospitalTurnos.Web/Components/Personal/Secciones/SeccionesAdministracion.razor` (quitar `@page "/personal/admin/propuestas"`) |
| Modificar | `src/HospitalTurnos.Aplicacion/Agendas/IPropuestaAgenda.cs` (4.2: `VistaPropuesta` suma `RechazadaPor` al final, AC 17) |
| Modificar | `src/HospitalTurnos.Infraestructura/Agendas/PropuestaAgendaEnBase.cs` (4.2: `ObtenerAsync` carga `RechazadaPor`) |
| Modificar | `src/HospitalTurnos.Web/Components/Personal/Agendas/MiPropuesta.razor` (4.2: ` por <RechazadaPor>` en la línea del rechazo) |
| Modificar | `tests/HospitalTurnos.Aplicacion.Tests/Agendas/DatosDePropuesta.cs` (4.2: suma `AdminPropioAsync`, `RegistrosDe`, `MensajesDe`) |
| Modificar | `tests/HospitalTurnos.Web.Tests/Agendas/DatosDePropuesta.cs` (4.2: suma `VincularMedicoConEmailAsync`) |
| Modificar | `tests/HospitalTurnos.Web.Tests/Personal/SesionYPermisosHttpTests.cs` (`RutasYaConstruidas`) |
| Modificar | `tests/HospitalTurnos.Web.Tests/Personal/MenuLateralTests.cs` (una línea de armado) |

### Lo que NO hay que hacer

- **NO reescribir reglas de la 4.1**: estados, 7 días, motivo y "qué agenda rige" se usan, no se
  copian. Lo único que se agrega al dominio son los dos métodos de los AC 18 y 19.
- **NO crear otro "turnos por semana"** ni otro resumen de agenda, ni tocar `ResumenDeAgenda`: son de
  la 4.1/4.2 (AC 25); `CompararCon` ya lo trae la 4.2.
- **NO crear otro archivo de ayudas de agendas** (`DatosDePropuestas` u otro nombre): se suma a
  `DatosDePropuesta` de la 4.2.
- **NO crear acciones de auditoría nuevas**: las dos existen desde la 1.3.
- **NO cancelar, mover ni tocar turnos** al aprobar (FR-15: "esos turnos se mantienen"). Tampoco hay
  tabla de turnos todavía: **NO** crearla ni crear una entidad `Turno` (dispararía la trampa de la 3.1
  y la propia).
- **NO bloquear la aprobación** por turnos fuera de agenda.
- **NO mandar el email desde el caso de uso**: se encola en la bandeja (ADR-05).
- **NO poner la dirección de email en la auditoría ni en un log** (Ley 25.326).
- **NO encolar con `Para` vacío** (AC 16).
- **NO agregar el campo email a A4 ni a `Medico`**: lo carga la 2.4 (decisión 1, AC 29).
- **NO mostrar borradores en A6** ni en su detalle (AC 5).
- **NO escribir `StatusCode` a mano**: `FijarEstadoEnPagina` (L-1).
- **NO dejar la ruta en `SeccionesAdministracion.razor`** al crear la página (L-6).
- **NO probar la carrera con la suerte** (dos tareas a la vez): `InterceptorQueSeAdelanta`.
- **NO leer la hora con `DateTime.Now`/`UtcNow`** ni calcular "hoy" con la fecha UTC.
- **NO crear una migración** (ver Salvaguarda de BD).
- **NO buscar por clases CSS** en los scripts de la prueba en vivo (L-14).

### Como verificar que funciona

```bash
cd ~/SourceCode/hospital-turnos

# 1. Unitarias de dominio (sin Docker)
dotnet test tests/HospitalTurnos.Dominio.Tests --filter "Story=4.3|Story=4.1"
# Esperado: T1–T2 de esta story y las 32 unitarias de la 4.1, 0 fallas

# 2. Todas las de esta story (Docker para Testcontainers)
dotnet test --filter "Story=4.3"
# Esperado: 34 pruebas declaradas (más los casos de cada [Theory]), 0 fallas
dotnet test --filter "Story=2.1"          # la T19 y el menú, con sus ajustes (AC 30)
dotnet test --filter "Story=4.2"          # sin cambios, T41 incluida (AC 30)

# 3. Suite completa y CI local
dotnet test
bash scripts/ci/local.sh; echo "rc=$?"    # rc=0, cobertura de Dominio y Aplicacion >= 80 %

# 4. Verificación en el navegador (guía docs/qa/4.3-pasos.md)
docker compose up -d --build
open http://localhost:8080/personal/ingreso
# Esperado, en este orden:
#  a. Como médico vinculado (4.2), enviar una propuesta que rija dentro de 10 días.
#  b. Como administrador: el menú dice "Propuestas de agenda (1)"; en otra pantalla (Médicos) también.
#  c. "Propuestas de agenda": la fila del médico con "Revisar"; el detalle con Vigente y Propuesta
#     lado a lado y "← cambia" donde corresponde; sin aviso de turnos fuera (no hay turnos).
#  d. "Rechazar…" sin motivo: el navegador pide completarlo; con espacios (sacando el "required" del
#     campo con las herramientas del navegador): cartel "Escribí el motivo del rechazo." y el campo
#     con lo escrito.
#  e. Rechazar con "El consultorio 3 está ocupado los martes; proponé miércoles.": vuelve al listado
#     con "Propuesta rechazada. ..."; el contador ya no tiene número.
#  f. Como médico: M2 muestra el motivo y "Rechazada el dd/MM/yyyy a las HH:mm por <nombre del administrador>."
#  g. Corregir y reenviar; como administrador, Aprobar: "Propuesta aprobada. ..."; en Médicos, el
#     estado del médico pasa a "Sin agenda (rige desde dd/MM)".
#  h. Dos pestañas con la misma propuesta: aprobar en una y rechazar en la otra: la segunda muestra
#     "Otro administrador acaba de revisar esta propuesta. ..." (o, si la otra pestaña ya recargó,
#     "Sólo se puede rechazar una propuesta enviada.").
#  i. Como recepción, pedir /personal/admin/propuestas/1: "No tenés permiso...".

# 5. Mailpit: sin email cargado (decisión 1: lo carga la 2.4), no hay emails; con un email cargado a
#    mano en asp_net_users para el usuario médico, el aviso aparece en http://localhost:8025
docker compose exec db psql -U postgres -d hospital_turnos \
  -c "SELECT tipo, referencia_id, enviado_el FROM mensajes_salientes WHERE tipo LIKE 'propuesta-agenda-%' ORDER BY id DESC LIMIT 5;"

# 6. La auditoría quedó escrita
docker compose exec db psql -U postgres -d hospital_turnos \
  -c "SELECT cuando, quien, accion, entidad_id, detalle FROM registros_auditoria WHERE entidad_tipo = 'AgendaMedico' ORDER BY id DESC LIMIT 10;"
```

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

### Change Log

- 2026-10-17: Story escrita (Mary) contra `main` `a8432c3` (1.1 a 4.1 en done), en paralelo con la
  4.2. 29 AC, 34 verificaciones, 4 decisiones abiertas (D-1 a D-4) y 6 supuestos sobre la 4.2 (S-1 a
  S-6) para igualar en la revisión. Punto de extensión de FR-15: `ITurnosFueraDeAgenda.BuscarAsync(AgendaMedico agendaNueva, DateOnly? hasta, CancellationToken ct)`.
- 2026-10-17: Correcciones de `revision-sprint-3.md` y decisiones 1, 7, 8, 9 y 25 del docente (opción
  recomendada; marcas reemplazadas y sección "Decisiones tomadas"). I-1: supuestos S-1 a S-6 igualados
  con la 4.2 (AC 25 con los nombres reales: `ResumenDeAgenda` con `CompararCon`, `MiPropuesta.razor` en
  `Personal/Agendas/`); quién rechazó por `VistaPropuesta.RechazadaPor` (sin `QuienRechazoAsync`),
  línea única `… por Lucía Pérez.` (T15 y T34 reescritas); `files_to_modify` con rutas reales. I-4: T3
  y T15 sin dos editables del mismo médico. I-5: sin `DatosDePropuestas` propios; se suma a
  `DatosDePropuesta` de la 4.2. I-2/I-7: decisión 25 en AC 22 y 26; FR-14e a medias hasta la 2.4 (AC
  16) y contrato nuevo con la 2.4 (AC 29; la no-regresión pasa a AC 30). M-2 (fechas con año en el
  aviso de reemplazo), M-3 (sin tocar `_Imports.razor`), M-4, M-10 (aceptado por escrito). 30 AC, 34
  verificaciones.
