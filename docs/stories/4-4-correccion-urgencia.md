---
epic: 4
story: "4.4"
title: "Corrección de agenda por urgencia, con motivo obligatorio"
status: ready-for-dev
sprint: 3
priority: "P1"
approach: "[ADAPT]"
depends_on: ["1.3", "2.2", "3.2", "4.1", "4.2", "4.3"]
files_to_create:
  - "src/HospitalTurnos.Dominio/Agendas/MotivoDeCorreccion.cs"
  - "src/HospitalTurnos.Aplicacion/Agendas/ICorreccionDeAgenda.cs"
  - "src/HospitalTurnos.Infraestructura/Agendas/CorreccionDeAgendaEnBase.cs"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/CorregirAgenda.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/FormularioCorreccion.cs"
  - "tests/HospitalTurnos.Dominio.Tests/Agendas/CorreccionDeUrgenciaTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Agendas/CorreccionDeAgendaTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Agendas/CorreccionDeAgendaHttpTests.cs"
files_to_modify:
  - "src/HospitalTurnos.Dominio/Agendas/AgendaMedico.cs"
  - "src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs"
  - "src/HospitalTurnos.Web/Personal/RutasPersonal.cs"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/FichaMedico.razor"
  - "tests/HospitalTurnos.Dominio.Tests/Ayudas/AgendaBuilder.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Agendas/DatosDeAgenda.cs"
---

# Story 4.4: Corrección de agenda por urgencia, con motivo obligatorio

Status: ready-for-dev

## Story

Como administradora del Hospital Las Acacias (Lucía),
quiero corregir yo misma la agenda de un médico cuando hay una urgencia (se rompió un consultorio, el
médico cambió de turno de un día para el otro), dejando escrito el motivo,
para que el sistema ofrezca los horarios correctos desde hoy, sin esperar los 7 días ni el ida y vuelta
de una propuesta, y para que quede en la auditoría quién lo hizo y por qué (FR-14f, S-32).

> **Qué es y qué no es esta story.** Es un **camino corto** para crear una agenda **ya aprobada**: no pasa
> por Borrador → Enviada (FR-14b) ni por la regla de los 7 días (S-31). Reusa todo lo demás: la grilla
> de la 4.2, la regla de vigencia de la 4.1 (`QueRigeEn`), el punto de extensión de turnos fuera de
> agenda de la 4.3 y el permiso confirmado en la base de la 2.2. **No** crea pantallas nuevas en el menú:
> se entra desde la ficha del médico (A1). **No** toca la propuesta del médico ni A6.

## Acceptance Criteria

> Pantalla A1 de `docs/03-ux.md` §7: "«Corregir agenda» (urgencias, S-32) abre la misma grilla de M2 y
> pide un motivo obligatorio". Rutas bajo `/personal/admin/medicos`, sólo para el rol `Administrador`
> (PRD §7.8). Las páginas son SSR estáticas con formularios comunes, como el resto de A1 (3.2).
>
> Vocabulario: **corrección** = una `AgendaMedico` creada por este camino. **Hoy** = la fecha local del
> hospital (Córdoba), leída del `TimeProvider` inyectado. Fechas de los mensajes `dd/MM/yyyy` con
> `CultureInfo.InvariantCulture`, salvo donde se indica `dd/MM`.
>
> **Nombres que usa de la 4.2 y la 4.3** (se escribieron en paralelo; la revisión adversarial del
> sprint los igualó con sus AC de contrato). Si al implementar algo no coincide, vale la dueña y se
> corrige esta story antes de programar (Task 1):
>
> | Lo que usa | Nombre real | Dueña |
> |---|---|---|
> | Editor de agenda | `EditorDeAgenda` (`Components/Personal/Agendas/EditorDeAgenda.razor`), **dentro** del `EditForm` SSR de la página, con los parámetros `Input` (`FormularioAgenda`), `MinimoVigenteDesde` (`DateOnly?`, el "(mín. dd/MM/yyyy)") y `TurnosPorSemanaAntes` (`int?`), y sólo ésos. Los campos se envían como `Input.DuracionTurnoMinutos`, `Input.VigenteDesde`, `Input.Franjas[i].Dia\|Desde\|Hasta`, y los botones como `Input.Accion` (`actualizar`, `agregar-franja`, `quitar-franja:<i>`) | 4.2 AC 24 (a) |
> | Modelo del editor | `FormularioAgenda` (no `sealed`): `AplicarAccionDeEdicion()`, `Revisar()`, `ADatos()`, `static Desde(AgendaMedico)`. Esta story lo **hereda** en `FormularioCorreccion` | 4.2 AC 24 |
> | Resumen en sólo lectura | `ResumenDeAgenda` (misma carpeta), parámetros `Agenda`, `Forma` y `CompararCon` | 4.2 AC 24 (b) |
> | Datos de una agenda | `DatosAgenda(int DuracionTurnoMinutos, DateOnly VigenteDesde, IReadOnlyList<DatosFranja> Franjas)` y `DatosFranja(DayOfWeek Dia, TimeOnly Desde, TimeOnly Hasta)` en `Aplicacion/Agendas/` | 4.2 |
> | Formato de franjas en la auditoría | día como número de la base (0–6), horas `HH:mm`, medianoche final `00:00` | 4.2 AC 17 |
> | Turnos fuera de agenda | `ITurnosFueraDeAgenda.BuscarAsync(AgendaMedico agendaNueva, DateOnly? hasta, CancellationToken ct)`, con `hasta` = `AgendaMedico.ProximaAprobadaDespuesDe(aprobadasDelMedico, agendaNueva.VigenteDesde)?.VigenteDesde`; hoy lo implementa `TurnosFueraDeAgendaSinTurnos` (lista vacía) | 4.3 AC 19, 20, 26 |
> | Aviso de turnos fuera | componente `AvisoTurnosFueraDeAgenda` (`Components/Personal/Agendas/`, parámetro `Turnos`) | 4.3 AC 22 |
> | Aviso de reemplazo (decisión 13 de la 4.1) | `Hay una agenda aprobada que empieza el dd/MM/yyyy y va a reemplazar a esta.` | 4.3 AC 7 |
> | Fecha de inicio pasada | `AgendaMedico.FechaDeInicioYaPaso(TimeProvider reloj)` | 4.3 AC 18 |
> | Auditoría de agendas | `EntidadTipo = "AgendaMedico"` y `EntidadId` = id de la agenda (como texto) | 4.2 y 4.3 |
> | Dobles y ayudas de prueba | `TurnosFueraDeAgendaEspia` (Aplicacion.Tests/Agendas/Dobles), `TurnosFueraDeAgendaFijos` (Web.Tests/Agendas/Dobles), `DatosDePropuesta` de Web.Tests (`GuardarAgendaAsync(app, agenda)`, `RelojCorridoDias`) | 4.3 y 4.2 |
>
> **Campos de auditoría** (AC 11). El actor es el que devuelve `IActorVigente.ExigirAdministradorActivoAsync`
> (2.2), con el nombre leído de la base:
>
> | Campo | Valor |
> |---|---|
> | `Accion` | `AccionesAuditoria.AgendaCorregida` (`"agenda.corregida"`, ya existe desde la 1.3) |
> | `UsuarioId` / `Quien` / `Rol` | el administrador / su nombre completo / `Administrador` |
> | `Canal` | `CanalAuditoria.Personal` |
> | `EntidadTipo` / `EntidadId` | `"AgendaMedico"` / id de la agenda nueva |
> | `Detalle` | `{"medicoId", "motivo", "vigenteDesde" ("yyyy-MM-dd"), "duracionTurnoMinutos", "franjas": [{"dia": 1, "desde": "08:00", "hasta": "12:00"}, …], "agendaAnteriorId" (la que regía hoy, o null), "turnosFueraDeAgenda" (cantidad)}`. Las franjas, con el formato del AC 17 de la 4.2 (día como número de la base, `HH:mm`, la medianoche final `00:00`): el mismo que `propuesta-agenda.enviada`, para que la 9.1 lea las dos igual (I-6 de la revisión) |

### Dominio — el camino propio de la corrección (FR-14f, ADR-06)

1. **Given** un médico, una duración, una fecha y franjas **When** se llama
   `AgendaMedico.CorreccionDeUrgencia(medicoId, duracionTurnoMinutos, vigenteDesde, franjas, reloj)`
   **Then** devuelve una agenda en estado `Aprobada`, con esas franjas, `AprobadaEl = reloj.GetUtcNow()`
   (offset 0), y `EnviadaEl`, `RechazadaEl` y `MotivoRechazo` en `null`. No pasa por `Borrador` ni por
   `Enviada`: es un método estático de fábrica, no una transición.
2. **Given** una corrección **When** se mira `EsCorreccion` **Then** es `true`; para cualquier agenda
   que nació como propuesta (borrador, enviada, aprobada o rechazada) es `false`. La marca sale de los
   datos que ya existen (aprobada sin `EnviadaEl`): **no** hay columna nueva (decisión 13).
3. **Fecha de inicio** (decisión 10): **Given** `vigenteDesde` igual a hoy o posterior **Then** se
   acepta; la regla de los 7 días **no** aplica (S-32 es para urgencias). **Given** una fecha anterior a
   hoy **Then** lanza `ReglaDeNegocioException` con
   `La corrección tiene que regir desde hoy (<hoy dd/MM/yyyy>) o después.` "Hoy" es la fecha local: a las
   23:30 del viernes 16/10 en Córdoba (02:30 UTC del sábado 17), el 16/10 se acepta. La comparación es
   la regla de la 4.3, `FechaDeInicioYaPaso(reloj)` (una sola vez en el dominio, M-6 de la revisión),
   con el mensaje propio de la corrección.
4. **Datos** (las mismas reglas de la 4.1, sin copiarlas): **Given** una duración fuera de 10–60 o franjas
   que se superponen **Then** lanza el mismo error y mensaje que los AC 4 y 6 de la 4.1 (el método usa el
   constructor y `AgregarFranja`). **Given** ninguna franja **Then** lanza `ReglaDeNegocioException` con
   `Agregá al menos una franja para corregir la agenda.` Orden de los controles: duración → franjas
   (superposición) → al menos una franja → fecha.
5. **Una corrección es una aprobada como cualquier otra**: **Given** una corrección **When** se intenta
   editarla, enviarla, aprobarla o rechazarla **Then** lanza el `ConflictoException` de la tabla del AC 13
   de la 4.1 (`Una agenda aprobada no se modifica: armá una propuesta nueva.`, `Sólo se puede aprobar una
   propuesta enviada.`, `Sólo se puede rechazar una propuesta enviada.`). Para volver a cambiarla se
   hace otra corrección.
6. **Vigencia** (FR-14d, AC 20 de la 4.1): **Given** las agendas de un médico con una corrección **When**
   se llama `AgendaMedico.QueRigeEn` o `EstadoDeAgenda.Calcular` **Then** la corrección se elige con la
   misma regla que una propuesta aprobada, sin código nuevo: si tiene la misma `VigenteDesde` que otra
   aprobada, gana la que se aprobó después (`AprobadaEl`); una aprobada que empieza más adelante la
   reemplaza desde su fecha (decisión 13 de la 4.1).
7. **Motivo** (`MotivoDeCorreccion`, `Dominio/Agendas/`): **Given** un texto **When** se crea
   `new MotivoDeCorreccion(texto)` **Then** se recorta (`Trim`) y: vacío, `null` o sólo espacios →
   `ReglaDeNegocioException` con `Escribí el motivo de la corrección.`; más de 500 caracteres
   (`AgendaMedico.LargoMaximoMotivo`) → `El motivo tiene como máximo 500 caracteres.`; si no, `Texto` es
   el recortado. Con 500 exactos se acepta.

### Caso de uso (Aplicacion + Infraestructura)

8. Puerto `ICorreccionDeAgenda` (`Aplicacion/Agendas/`), implementado por `CorreccionDeAgendaEnBase`
   (`Infraestructura/Agendas/`, recibe `HospitalDbContext`, `IAuditoria`, `IActorVigente`,
   `IConsultaAgendas`, `ITurnosFueraDeAgenda` y `TimeProvider`) y registrado `Scoped` en
   `AgregarInfraestructura`:
   - `Task<DatosParaCorregir?> ObtenerAsync(long medicoId, CancellationToken ct)`: `null` si el médico no
     existe; si existe, `DatosParaCorregir(long MedicoId, string Medico, AgendaMedico? Vigente,
     DateOnly? PropuestaEnviadaDesde, IReadOnlyList<AgendaMedico> Aprobadas, DateOnly Hoy)`, donde
     `Medico` es `Apellido, Nombre`, `Vigente` es `IConsultaAgendas.AgendaQueRigeAsync(medicoId, hoy)`
     (con sus franjas), `PropuestaEnviadaDesde` la `VigenteDesde` de su `Enviada` si tiene, y `Aprobadas`
     todas sus agendas aprobadas (sin franjas): con ellas la pantalla calcula el aviso de reemplazo
     contra la fecha que elige la administradora, no contra hoy (AC 19; I-2 de la revisión).
   - `Task<ResultadoCorreccion> CorregirAsync(long medicoId, DatosCorreccion datos, ActorAuditoria actor,
     CancellationToken ct)`, con `DatosCorreccion(DatosAgenda Agenda, string? Motivo, bool ConfirmaTurnosFuera)`
     y `ResultadoCorreccion(long? AgendaId, IReadOnlyList<TurnoFueraDeAgenda> TurnosFueraDeAgenda)` (la lista
     que devolvió el puerto, para que la pantalla la pase a `AvisoTurnosFueraDeAgenda`): `AgendaId` con valor
     = se guardó; `null` = falta confirmar (AC 12).
9. **Permiso en la base** (Arq. §7.7, decisión 16): **Given** un actor que no es un administrador activo en
   la base (otro rol, o un administrador desactivado con la sesión todavía abierta) **When** llama a
   `CorregirAsync` **Then** lanza `AccesoDenegadoException` con
   `Sólo un administrador puede corregir la agenda de un médico.`, antes de leer nada, y no guarda ni
   audita.
10. **Orden y errores**: permiso (403) → médico existe (si no, `NoEncontradoException` con
    `No existe el médico pedido.`, el texto de la 3.2) → motivo (AC 7) → agenda (AC 3 y 4) → turnos fuera
    de agenda (AC 12) → guardar. Un error en cualquier paso no guarda ni audita nada.
11. **Guardar con su auditoría, juntos o nada** (Arq. §7.5): **Given** datos válidos **When** se corrige
    **Then** en una transacción explícita (como `GestionMedicosEnBase.CrearAsync`: el id nace al guardar y
    la auditoría lo necesita) se guarda la agenda y un registro con los campos de la tabla del inicio.
    **Given** que la auditoría falla **Then** la agenda no queda en la base.
12. **Turnos fuera de la agenda corregida** (FR-15, S-21; mismo punto de extensión de la 4.3, no otro,
    con su firma del AC 20 de la 4.3): antes de guardar se llama
    `ITurnosFueraDeAgenda.BuscarAsync(agendaNueva, hasta, ct)`, con
    `hasta = AgendaMedico.ProximaAprobadaDespuesDe(aprobadasDelMedico, agendaNueva.VigenteDesde)?.VigenteDesde`
    (4.3 AC 19 y 26: después de esa fecha manda la otra agenda). **Given** una lista vacía **Then** se
    guarda. **Given** N > 0 y `ConfirmaTurnosFuera = false` **Then**
    no guarda nada y devuelve `ResultadoCorreccion(null, <los N turnos>)`. **Given** N > 0 y `ConfirmaTurnosFuera =
    true` **Then** guarda, y `turnosFueraDeAgenda = N` en el `Detalle`. Los turnos se mantienen (FR-15):
    esta story no los toca. Hoy la tabla de turnos no existe (llega con la 5.3) y la lista es siempre
    vacía; la prueba-trampa y la obligación para la 5.3 son de la 4.3. El paso de confirmación existe
    acá y no en A6 por la decisión 25: en la corrección la agenda se escribe en el mismo formulario, y
    el aviso no se puede mostrar antes de que la administradora la escriba.
13. **Propuesta enviada pendiente** (decisión 11): **Given** un médico con una propuesta
    `Enviada` **When** se corrige su agenda **Then** la propuesta **sigue** `Enviada`, sin cambios (la
    revisa el administrador en A6, y si la aprueba rige desde su fecha, FR-14d). A1 sigue diciendo
    `Propuesta pendiente` (decisión 8 de la 4.1).
14. **Sin email al médico** (decisión 1): la corrección no encola ningún mensaje en la bandeja de salida,
    tampoco cuando la 2.4 (sprint 4) cargue el email del personal. El médico ve su agenda vigente en M2
    (la muestra la 4.2 arriba de su propuesta).
15. **Médico sin agenda** (decisión 12): **Given** un médico sin ninguna agenda aprobada
    **When** se corrige **Then** se guarda igual: la corrección es su primera agenda, y `agendaAnteriorId`
    queda `null`.
16. **Dos correcciones a la vez** (carrera): no hay índice único que las separe **a propósito**: dos
    aprobadas con la misma fecha son válidas y gana la última aprobada (AC 20 de la 4.1, que la eligió
    así para permitir correcciones). **Given** que otra conexión guarda otra aprobada del mismo médico y
    la misma fecha justo antes del guardado (doble `InterceptorQueSeAdelanta` de la 3.1) **When** se
    corrige **Then** se guardan las dos, no hay error interno, y `AgendaQueRigeAsync` devuelve la
    corrección (su `AprobadaEl` es posterior).

### Pantallas — la ficha (A1) y la corrección (FR-14f, UX A1)

17. **Ficha del médico** (decisión 10 de la 3.2: la 4.4 agrega esto a `FichaMedico.razor`): **Given** un
    administrador en `/personal/admin/medicos/{id}` **Then**, debajo del estado de agenda, ve el título
    `<h2>Agenda vigente</h2>` y el resumen de la agenda que rige hoy con `ResumenDeAgenda` de la 4.2
    (forma `EnLinea`, la de por defecto), o `Sin agenda vigente.` si no rige ninguna; y un enlace `Corregir agenda` a
    `/personal/admin/medicos/{id}/corregir-agenda`. El formulario de datos del médico (3.2) no cambia.
18. **Pantalla de corrección** — **Given** un administrador **When** pide
    `/personal/admin/medicos/{id}/corregir-agenda` **Then** recibe `200` con:
    - `<h1>Corregir agenda: Apellido, Nombre</h1>` y el texto
      `Para urgencias: la agenda corregida queda aprobada sin pasar por la propuesta del médico, y el motivo queda en la auditoría.`
    - `Agenda vigente:` con `ResumenDeAgenda`, o `Sin agenda vigente.`
    - el editor de agenda de la 4.2 (`EditorDeAgenda`, con los campos, la revisión por fila y el cálculo
      de sus AC 7, 9 y 10), **precargado** con la duración y las franjas de la vigente (vacío si no
      tiene), con `Rige desde` = hoy, `MinimoVigenteDesde` = hoy (no hoy + 7: `(mín. dd/MM/yyyy)` con
      la fecha de hoy) y `TurnosPorSemanaAntes` = los de la vigente;
    - el campo `Motivo de la corrección (obligatorio)` (área de texto, máximo 500);
    - el botón `Corregir agenda` (`Input.Accion = corregir`) y un enlace `Cancelar` a la ficha.

    **Los botones del editor no guardan ni piden el motivo** (C-1 de la revisión): `+ Agregar franja`,
    `Quitar`, `Actualizar el cálculo` y Enter vuelven a dibujar la pantalla con `200`, con lo escrito
    (motivo incluido) y el cálculo actualizado, sin guardar nada y **sin** el mensaje del motivo
    aunque esté vacío: el orden natural es primero la grilla y después el motivo. Una acción que no es
    del editor ni de esta pantalla (POST armado a mano) → `400` con
    `No reconocemos esa acción. Recargá la página.` (4.2 AC 8). El cálculo se actualiza con esos
    botones, como en M2 (SSR, decisión 3), no mientras se escribe.
19. **Avisos antes de corregir** (en `role="status"`, siempre como texto):
    - con una propuesta enviada (AC 13):
      `Este médico tiene una propuesta enviada que rige desde el dd/MM/yyyy. La corrección no la cambia: revisala en «Propuestas de agenda».`
    - con una aprobada **posterior a la fecha `Rige desde` del formulario** (decisión 13 de la 4.1): el
      texto de A6 (4.3 AC 7) `Hay una agenda aprobada que empieza el dd/MM/yyyy y va a reemplazar a esta.`
      con la fecha de `AgendaMedico.ProximaAprobadaDespuesDe(datos.Aprobadas, Input.VigenteDesde)`. Se
      calcula en **cada** dibujo contra la fecha que eligió la administradora (con `Rige desde` vacío,
      contra hoy), no contra hoy fijo: si elige una fecha posterior a esa aprobada, el aviso no es
      verdad y no aparece (I-2 de la revisión).
20. **Éxito**: **Given** datos válidos **When** se envía **Then** `302` a
    `/personal/admin/medicos/{id}?resultado=correccion`, y la ficha muestra `Agenda corregida.` en
    `role="status"` (un cuarto texto fijo en el `switch` de la ficha: nunca se muestra lo que venga en la
    consulta). Si la corrección rige hoy, la ficha muestra su resumen y el listado A1 dice `Vigente`.
21. **Motivo vacío o largo** (validación doble, NFR-10): **Given** el motivo vacío, sólo espacios o de más
    de 500 caracteres **When** se aprieta `Corregir agenda` o `Corregir igual` **Then** responde `200`
    con el mensaje del AC 7 debajo del campo (validación del formulario), lo cargado en el editor sigue
    escrito, y no se guarda ni se audita. Con los botones del editor no se valida (AC 18).
22. **Reglas de la agenda**: **Given** una fecha anterior a hoy, franjas superpuestas o ninguna franja
    **When** se envía **Then** responde con el código del manejador (`400`) y el mensaje del AC 3 o 4 en
    `role="alert"` arriba del formulario (`ManejadorGlobalDeErrores.FijarEstadoEnPagina` de la 2.2), con
    lo cargado todavía escrito.
23. **Turnos fuera de agenda** (AC 12, decisión 25): **Given** que el punto de extensión devuelve N > 0
    **When** se aprieta `Corregir agenda` **Then** responde `200` sin guardar, con el componente
    `AvisoTurnosFueraDeAgenda` de la 4.3 (sus textos: `⚠ N turnos ya dados quedan fuera de la nueva
    agenda. Se mantienen.`, con 1 `⚠ 1 turno ya dado queda fuera de la nueva agenda. Se mantiene.`, y la
    lista en `Ver`; esta story no los escribe) y un botón `Corregir igual` (`Input.Accion =
    corregir-igual`) que reenvía lo mismo con la confirmación. Con la confirmación se guarda y sigue el
    AC 20.
24. **Médico inexistente**: **Given** un id que no existe **When** se pide la pantalla (GET) o se envía
    (POST) **Then** `404` con `No existe el médico pedido.` y un enlace `Volver a Médicos`, con
    `FijarEstadoEnPagina` (nunca `StatusCode = 404` a mano, L-1).

### Permisos y antifalsificación

25. **Given** un usuario con rol `Recepcionista` o `Medico` **When** pide la pantalla de corrección
    **Then** la respuesta final es `403` con `No tenés permiso para entrar a esta sección.` (página de la
    2.1). **Given** nadie ingresado **Then** `302` a `/personal/ingreso?ReturnUrl=<ruta>`. **Given** un
    administrador desactivado con la sesión todavía abierta **When** envía la corrección **Then** el
    cartel del AC 9 con `403` (mismo mecanismo que la 3.2) y nada guardado.
26. **Given** un POST sin el token antifalsificación **Then** `400` y no se guarda ni se audita nada.

### Contrato con otras stories

27. **(Con pruebas asignadas, B-1.)** Lo que esta story **usa** y lo que **deja**:
    - **4.1** — crea la corrección con su propio método de fábrica en `AgendaMedico` (AC 1), sin tocar la
      máquina de estados ni la tabla: **no hay migración**. `QueRigeEn` y `EstadoDeAgenda.Calcular` la
      eligen sin cambios (AC 6). Pruebas: **T6, T7, T11**.
    - **4.2** — reusa el editor, `FormularioAgenda`, `DatosAgenda` y el resumen como los fija el AC 24 de
      la 4.2 (modelo heredado, propiedad `Input`, `OnSubmit` con `AplicarAccionDeEdicion()` primero,
      valores propios de `Input.Accion`), y el formato de franjas de su AC 17 en la auditoría. Esta story
      **no** escribe otra grilla ni otro resumen, ni otro cálculo de turnos por semana (`TurnosPorSemana`
      de la 4.1), y no toca `Franja.cs`. Pruebas: **T10, T20, T21, T28**.
    - **4.3** — usa el **mismo** punto de extensión de turnos fuera de agenda con su firma y su `hasta`
      (no crea otro puerto ni otra implementación), el componente `AvisoTurnosFueraDeAgenda`, el texto
      del aviso de la decisión 13, `FechaDeInicioYaPaso` y los dobles `TurnosFueraDeAgendaEspia` y
      `TurnosFueraDeAgendaFijos`. La obligación para la 5.3 (reemplazar la implementación vacía) ya la
      escribe la 4.3: cuando la 5.3 la cumpla, la corrección queda cubierta sin cambios. Pruebas:
      **T4, T15, T16, T21, T27, T28**.
    - **3.2** — agrega a `FichaMedico.razor` la agenda vigente y `Corregir agenda` (su decisión 10), sin
      cambiar `DatosFichaMedico`, `IGestionMedicos` ni sus pruebas. Prueba: **T20**.
    - **9.1 (Auditoría)** — lee `agenda.corregida` con `EntidadTipo = "AgendaMedico"` y el motivo en
      `Detalle.motivo`. Prueba: **T10**.

### No-regresión

28. Siguen en verde, sin modificarlas, las pruebas de las stories 1.1 a 4.1 y las de 2.3, 4.2 y 4.3
    (en particular las de la ficha de la 3.2: `Sin agenda`, `Cambios guardados.` y el `404`). La ruta
    nueva queda **fuera del menú** (como la ficha): `SesionYPermisosHttpTests` y `MenuLateralTests` de la
    2.1 no cambian. `HoraDelSistemaTests` sigue en verde. Cobertura de líneas de Dominio y Aplicacion
    sobre el 80 % (NFR-11).

### Tests (28 verificaciones)

| # | Test | AC | Nivel | Escenario | Resultado esperado |
|---|------|----|-------|-----------|--------------------|
| T1 | `CorreccionDeUrgencia_DatosValidos_NaceAprobadaSinPasarPorElEnvio` | 1 | unit | Reloj 16/10/2026 12:00 Córdoba; médico 7, 20 min, desde 16/10/2026, lunes 08–12 y miércoles 08–12 | `Aprobada`; `AprobadaEl = reloj.GetUtcNow()`, `Offset = 0`; `EnviadaEl`, `RechazadaEl`, `MotivoRechazo` nulos; 2 franjas; `MedicoId = 7` |
| T2 | `EsCorreccion_SegunComoNacio_DistingueCorreccionDePropuesta` | 2 | unit | `[Theory]`: corrección; borrador; enviada; aprobada por el ciclo; rechazada (con `AgendaBuilder`) | `true` sólo para la corrección |
| T3 | `CorreccionDeUrgencia_DesdeHoyOMasAdelante_SeAceptaSinLosSieteDias` | 3 | unit | Reloj 16/10/2026; `[Theory]` desde 16/10 (hoy), 17/10, 30/11 | `Aprobada` con esa `VigenteDesde` |
| T4 | `CorreccionDeUrgencia_FechaPasada_LanzaConLaFechaDeHoy` | 3, 27 | unit | `[Theory]`: reloj 16/10 12:00 y desde 15/10; reloj en `2026-10-17T02:30Z` (16/10 23:30 en Córdoba) y desde 16/10 (se acepta) y 15/10 (lanza) | Las que lanzan: `ReglaDeNegocioException` con `La corrección tiene que regir desde hoy (16/10/2026) o después.`; con la fecha UTC el 16/10 fallaría |
| T5 | `CorreccionDeUrgencia_DatosInvalidos_LanzaElErrorDeSuReglaEnOrden` | 4 | unit | `[Theory]`: duración 9; lunes 08–12 y lunes 10–14; sin franjas; duración 9 **y** sin franjas **y** fecha pasada (orden) | Mensajes de la 4.1 (AC 6 y 4) / `Agregá al menos una franja para corregir la agenda.` / el de la duración (primero en el orden) |
| T6 | `Correccion_CualquierCambioDeEstadoOEdicion_LanzaConflicto` | 5, 27 | unit | `[Theory]` sobre una corrección: las 4 ediciones, `Enviar`, `Aprobar`, `Rechazar("x")` | `ConflictoException` con el mensaje de la tabla del AC 13 de la 4.1; la agenda no cambia |
| T7 | `QueRigeEn_ConCorrecciones_EligeComoACualquierAprobada` | 6, 27 | unit | A aprobada por el ciclo desde 02/11 (aprobada el 20/10); C corrección desde 02/11 creada el 25/10; F aprobada desde 16/11; `[Theory]` fechas 01/11, 02/11, 15/11, 16/11; y `EstadoDeAgenda.Calcular([C], hoy = 02/11)` | null, C, C, F (gana C sobre A por `AprobadaEl`; F la reemplaza desde su fecha); `Calcular` → `Vigente` |
| T8 | `MotivoDeCorreccion_SegunElTexto_RecortaOLanza` | 7 | unit | `[Theory]` `"  Consultorio inundado  "`, `""`, `"   "`, `null`, 500 letras, 501 letras | `Texto = "Consultorio inundado"` / `Escribí el motivo de la corrección.` ×3 / se acepta / `El motivo tiene como máximo 500 caracteres.` |
| T9 | `AgregarInfraestructura_ResuelveLaCorreccionDeAgenda` | 8 | integration | `ServiceProvider` con `AgregarInfraestructura` + `AddIdentidadPersonal` (como T42 de la 4.1); un scope | `ICorreccionDeAgenda` se resuelve como `CorreccionDeAgendaEnBase` |
| T10 | `CorregirAsync_DatosValidos_GuardaAprobadaYAuditaConElMotivo` | 1, 8, 11, 27 | integration | Médico propio con una aprobada que rige hoy (id X); admin propio; reloj 03/11/2026; corrige desde 03/11, 30 min, martes 14–18, motivo `"  Se inundó el consultorio 3  "` | `AgendaId` con valor; en la base `estado = 'Aprobada'`, `enviada_el` nulo, 1 franja; 1 registro `agenda.corregida` con `EntidadTipo = "AgendaMedico"`, `EntidadId` = el id, `Quien` = nombre del admin leído de la base, `Canal = Personal`, `Detalle` con `motivo = "Se inundó el consultorio 3"`, `medicoId`, `vigenteDesde = "2026-11-03"`, `duracionTurnoMinutos = 30`, la franja con el formato del AC 17 de la 4.2 (`{"dia": 2, "desde": "14:00", "hasta": "18:00"}`: el día como número, no `"martes"`), `agendaAnteriorId = X`, `turnosFueraDeAgenda = 0` |
| T11 | `CorregirAsync_RigeHoy_LaConsultaLaDevuelveYA1DiceVigente` | 6, 15, 27 | integration | Médico propio **sin** agendas; corrige desde hoy (reloj 03/11) | `AgendaQueRigeAsync(medico, 03/11)` devuelve la corrección con su franja; `EstadosDeAgendaAsync` → `Vigente`; `Detalle.agendaAnteriorId` nulo |
| T12 | `CorregirAsync_ActorNoAdministradorActivo_LanzaAccesoDenegadoSinGuardar` | 9, 10 | integration | `[Theory]` actor recepcionista propio, médico propio, administrador propio desactivado | `AccesoDenegadoException` con `Sólo un administrador puede corregir la agenda de un médico.`; 0 agendas del médico; 0 registros `agenda.corregida` con su `medicoId` |
| T13 | `CorregirAsync_DatosOMedicoInvalidos_LanzaYNoGuardaNada` | 10 | integration | `[Theory]`: médico inexistente (`long.MaxValue`); motivo vacío; fecha pasada; franjas superpuestas | `NoEncontradoException` (`No existe el médico pedido.`) / `ReglaDeNegocioException` con el mensaje de su AC; 0 agendas nuevas, 0 registros |
| T14 | `CorregirAsync_FallaLaAuditoria_NoQuedaLaAgenda` | 11 | integration | `IAuditoria` reemplazada por un doble que lanza `InvalidOperationException` al registrar | Lanza; `SELECT count(*) FROM agendas_medico WHERE medico_id = <propio>` = 0 |
| T15 | `CorregirAsync_TurnosFueraDeAgenda_PideConfirmacionAntesDeGuardar` | 12, 27 | integration | `ITurnosFueraDeAgenda` reemplazado por `TurnosFueraDeAgendaEspia` de la 4.3 que devuelve 2; reloj 03/11/2026, corrección desde 09/11; `[Theory]`: (a) el médico sin otras aprobadas; (b) con una aprobada desde 30/11 (posterior a la corrección) y otra desde 02/11 (anterior); en cada caso, primero sin confirmar y después con `ConfirmaTurnosFuera = true` | Primero `AgendaId = null` con los 2 turnos en `TurnosFueraDeAgenda`, y 0 agendas nuevas, 0 registros; después se guarda y `Detalle.turnosFueraDeAgenda = 2`; cada llamada del espía recibió la agenda nueva (con el `MedicoId` del médico y sus franjas) y `hasta` = (a) `null` / (b) 30/11/2026 (`ProximaAprobadaDespuesDe` de la 4.3) |
| T16 | `PuntoDeExtension_TurnosFueraDeAgenda_EsElMismoDeLa43` | 12, 27 | integration | Contenedor real; reflexión sobre el ensamblado de Infraestructura | `ITurnosFueraDeAgenda` se resuelve; **una sola** clase lo implementa; la corrección con el contenedor real devuelve `TurnosFueraDeAgenda` vacía y guarda |
| T17 | `CorregirAsync_ConPropuestaEnviada_LaPropuestaSigueEnviada` | 13 | integration | Médico propio con una aprobada vigente y una `Enviada` desde 30/11; corrige | La enviada sigue `Enviada`, sin cambios en sus marcas; `EstadosDeAgendaAsync` → `PropuestaPendiente` |
| T18 | `CorregirAsync_NoEncolaEmails` | 14 | integration | Médico propio vinculado a un usuario médico propio **con email cargado** (`MedicoConUsuarioAsync(proveedor, email: "medico-<marca>@ejemplo.com")` de `DatosDePropuesta`, 4.2: así la prueba no pasa sólo porque falta el email); corrige | 0 filas en `mensajes_salientes` con `referencia_id` = id de la agenda o el `para` del usuario |
| T19 | `CorregirAsync_OtraAprobadaSeGuardaJustoAntes_GuardanLasDosYRigeLaCorreccion` | 16 | integration | `InterceptorQueSeAdelanta` (3.1) con un `INSERT` de otra aprobada del mismo médico, misma `vigente_desde`, `aprobada_el` = un minuto antes del reloj | Sin excepción; 2 aprobadas en la base; `AgendaQueRigeAsync` devuelve la corrección (por `Id`); `SeAdelanto = true` |
| T20 | `GetFicha_ConYSinAgendaVigente_MuestraElResumenYElEnlaceCorregir` | 17, 27 | integration | `PersonalWebApplicationFactory`, `admin1`; médico A con una aprobada que rige hoy (lunes 08–12, 20 min, armada como en T44 de la 4.1) y médico B sin agendas | A: `<h2>Agenda vigente</h2>`, el resumen con el formato de `ResumenDeAgenda` de la 4.2 (`turnos de 20 min`, `Lun 8–12`, `12 turnos/semana`), enlace `Corregir agenda` a su ruta; B: `Sin agenda vigente.` y el enlace; los dos siguen con el formulario de la 3.2 |
| T21 | `GetCorregirAgenda_MedicoConVigenteYAvisos_PrecargaLaAgendaYPideMotivo` | 18, 19, 27 | integration | Médico con vigente (martes 14–18, 30 min), una `Enviada` desde hoy + 20 y una aprobada desde hoy + 40 | `200`; `<h1>Corregir agenda: Apellido, Nombre</h1>`; el texto de urgencias; el editor con `Input.DuracionTurnoMinutos` = 30, `Input.Franjas[0].Dia` = `Tuesday` con 14:00–18:00, `Input.VigenteDesde` = hoy y `(mín. <hoy dd/MM/yyyy>)`; el campo `Motivo de la corrección (obligatorio)`; el botón `Corregir agenda` con `name="Input.Accion"` y `value="corregir"`; los dos avisos del AC 19 con sus fechas (`dd/MM/yyyy`); enlace `Cancelar` a la ficha |
| T22 | `PostCorregirAgenda_DatosValidos_RedirigeALaFichaYA1DiceVigente` | 20 | integration | Médico sin agendas; POST `corregir` con lunes 08–12, 20 min, desde hoy y un motivo | `302` a `/personal/admin/medicos/{id}?resultado=correccion`; la ficha tiene `Agenda corregida.` en `role="status"` y el resumen; GET `/personal/admin/medicos?buscar=<marca>` → su fila dice `Vigente`; 1 registro `agenda.corregida` con `Quien = "Lucía Pérez"` |
| T23 | `PostCorregirAgenda_MotivoInvalido_Responde200ConElMensajeSinGuardar` | 21 | integration | `[Theory]` POST `corregir` con motivo `""`, `"   "`, 501 letras | `200`; el mensaje del AC 7 junto al campo; la franja enviada sigue en el editor; 0 agendas y 0 registros del médico |
| T24 | `PostCorregirAgenda_ReglaDeLaAgenda_Responde400ConElCartel` | 22 | integration | `[Theory]`: desde ayer; lunes 08–12 y lunes 10–14; sin franjas | `400`; `role="alert"` con el mensaje del AC 3 o 4; 0 agendas del médico |
| T25 | `CorregirAgenda_MedicoInexistente_Responde404ConEnlace` | 24 | integration | `[Theory]` GET y POST (con token y `_handler` de la pantalla de otro médico) a `/personal/admin/medicos/9223372036854775807/corregir-agenda` | `404`; `No existe el médico pedido.`; enlace a `/personal/admin/medicos`; el cuerpo no contiene `Algo falló` |
| T26 | `CorregirAgenda_SinPermisoOSinToken_NoEntraNiGuarda` | 25, 26 | integration | `[Theory]`: GET con `recepcion1` y con `medico1` → 403; GET sin ingresar → 302; POST de un administrador propio desactivado después de ingresar → 403 con cartel; POST de `admin1` sin `__RequestVerificationToken` → 400 | Los códigos indicados; los textos `No tenés permiso para entrar a esta sección.` / el del AC 9; `ReturnUrl` con la ruta; en los dos POST, 0 agendas y 0 registros del médico |
| T27 | `PostCorregirAgenda_TurnosFueraDeAgenda_AvisaYCorrigeAlConfirmar` | 23, 27 | integration | `app.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddScoped<ITurnosFueraDeAgenda>(_ => new TurnosFueraDeAgendaFijos(...))))` con 1 y con 3 turnos (como la T31 de la 4.3); POST `corregir` y después POST `corregir-igual` con lo mismo | Con `corregir`: `200`, el aviso de `AvisoTurnosFueraDeAgenda` (`⚠ 1 turno ya dado queda fuera de la nueva agenda. Se mantiene.` / `⚠ 3 turnos ya dados quedan fuera de la nueva agenda. Se mantienen.`), el botón `Corregir igual` (`value="corregir-igual"`), 0 agendas; con `corregir-igual`: `302` a la ficha |
| T28 | `PostCorregirAgenda_BotonesDelEditor_RedibujanSinGuardarNiPedirElMotivo` | 18, 19, 21, 27 | integration | Médico con vigente (lunes 08–12, 20 min) y una aprobada desde hoy + 40; motivo vacío; `[Theory]` de POST: `agregar-franja`; `quitar-franja:0`; `actualizar` con `Rige desde` = hoy + 50; `actualizar` con `Rige desde` = hoy + 10; `Input.Accion = borrar` | `200` con una fila más / sin la fila / sin el aviso de reemplazo / con el aviso y la fecha de hoy + 40 / `400` con `No reconocemos esa acción. Recargá la página.`; en los cuatro `200`, **ningún** `Escribí el motivo de la corrección.`; en todos, 0 agendas nuevas y 0 registros del médico (C-1 e I-2 de la revisión) |

Todas llevan `[Trait("Story", "4.4")]` y un `[Trait("AC", "n")]` por AC de su columna.
T1–T8 van en `HospitalTurnos.Dominio.Tests` (sin base, con `FakeTimeProvider`). T9–T19, en
`HospitalTurnos.Aplicacion.Tests` contra PostgreSQL real (`[Collection("Postgres")]`). T20–T28, en
`HospitalTurnos.Web.Tests/Agendas/CorreccionDeAgendaHttpTests.cs` (junto a las de agendas de la 4.2 y la
4.3, M-7 de la revisión) contra la aplicación y PostgreSQL reales.

> El plan estimaba 5 pruebas; quedan 28. La story parece chica (un botón y un método), pero junta un
> camino de dominio nuevo con sus bordes (fecha local, orden de los controles, inmutable como cualquier
> aprobada), un caso de uso con permiso en la base, transacción con auditoría, FR-15 con confirmación y
> una carrera, y una pantalla con sus caminos de error (200, 400, 403, 404, antifalsificación). Ocho
> son unitarias y corren en milisegundos. Lo que se reusa (editor, resumen, permiso, cartel, ayudas de
> prueba) no suma pruebas propias.

**Por qué no hay E2E (regla 6.1 del formato).** La pantalla es SSR, como M2 (decisión 3): el editor y su
cálculo son de la 4.2, que los prueba, y se actualizan con botones del formulario. Lo propio de esta
story (precargar, botones del editor sin pedir el motivo, motivo, avisos, guardar, errores) se ve
completo en el HTML del servidor y se prueba con `HttpClient` sobre la aplicación real (T20–T28). El proyecto E2E lo crea la
10.1. La prueba en vivo de "Cómo verificar" cubre el navegador.

#### Given/Fixture

- **Reloj**: `RelojDePrueba.En(...)` (Dominio.Tests, 4.1) y `DatosDeAgenda.Reloj(...)` (Aplicacion.Tests,
  4.1). T4 arma el reloj en UTC (`2026-10-17T02:30Z`) para que se vea el borde de la medianoche (L-12).
  Ninguna prueba espera tiempo real (A-4).
- **`AgendaBuilder`** (Dominio.Tests, 4.1) suma un terminal `.Corregida(reloj)` que llama a
  `AgendaMedico.CorreccionDeUrgencia` con lo cargado (si no hay franjas, lunes 08:00–12:00, como
  `.Enviada`). Sigue sin reflexión.
- **`DatosDeAgenda`** (Aplicacion.Tests, 4.1) suma `GuardarCorreccionAsync(this PostgresFixture, long medicoId,
  DateOnly vigenteDesde, TimeProvider reloj, params Franja[] franjas)`, con la misma forma que
  `GuardarAgendaAsync`. Médicos con `MedicoPropioAsync` (4.1) y admin propio con
  `UsuariosDePrueba.CrearAsync(proveedor, [Roles.Administrador])` + `UsuariosDePrueba.ActorDe(...)` (2.2).
  El `ServiceProvider` es `fixture.Proveedor(reemplazos)` de `DatosDeAgenda`, con
  `AddSingleton<TimeProvider>(reloj)` y los dobles en `reemplazos` (lo último registrado gana).
- **Base compartida**: cada prueba crea su médico y filtra por él (agendas por `medico_id`, auditoría por
  `Detalle->>'medicoId'` o `EntidadId`, emails por `para`). Nada de limpiar tablas.
- **`TurnosFueraDeAgendaEspia`** (de la 4.3, `Aplicacion.Tests/Agendas/Dobles/`): devuelve los turnos que
  se le configuran y anota cada llamada con la agenda recibida y el `hasta`. Se registra al final con
  `Proveedor(s => s.AddScoped<ITurnosFueraDeAgenda>(_ => espia))`. No se crea otro doble (I-2 de la
  revisión).
- **T14**: un doble de `IAuditoria` escrito a mano en el mismo archivo de pruebas (lanza en `Registrar`).
- **T19**: `InterceptorQueSeAdelanta(fixture.CadenaApp, "<INSERT de una aprobada con sus columnas del AC 23 de la 4.1>")`
  agregado con `ConfigureDbContext<HospitalDbContext>(o => o.AddInterceptors(...))`, como en la 3.2.
- **Web**: `PersonalWebApplicationFactory` (2.1) con `[Collection("Postgres")]`; `admin1` ingresa con
  `FormularioHttp.IngresarAsync`; los POST con `FormularioHttp.EnviarAsync(cliente, ruta, "correccion", campos)`.
  Médicos con `DatosDeCatalogo` de Web.Tests (3.2). "Hoy" = fecha del hospital según `app.Reloj` (hora
  real); no se adelanta el reloj (cortaría la sesión). Las agendas previas se arman con los métodos
  públicos y se guardan con `GuardarAgendaAsync(app, agenda)` de `DatosDePropuesta` (4.2); las que tienen
  que regir hoy se envían y aprueban con `RelojCorridoDias(app.Reloj, -30)`, como la T44 de la 4.1. Los
  campos son los del AC 24 de la 4.2 (`Input.DuracionTurnoMinutos`, `Input.VigenteDesde` en
  `yyyy-MM-dd`, `Input.Franjas[i].Dia`/`Desde`/`Hasta`, `Input.Accion`) más `Input.Motivo`: una ayuda
  `CamposDeCorreccion(duracion, desde, franjas, motivo, accion)` en el archivo de pruebas los arma en un
  solo lugar.
- **T27**: el puerto se reemplaza con `WithWebHostBuilder` + `ConfigureTestServices` y
  `TurnosFueraDeAgendaFijos` de la 4.3 (`Web.Tests/Agendas/Dobles/`), como su T31. No se crea otra
  fábrica.
- **T26**: el administrador desactivado se crea con `FormularioHttp.CrearUsuarioAsync` (2.2), ingresa y
  después se desactiva en la base (como en la 3.2).

## Tasks / Subtasks

- [ ] [SEQ] Task 1: Salvaguarda de estado (AC: todos)
  - [ ] Confirmar en `main` que la 4.2 y la 4.3 están en done y que dejaron lo de la tabla "Nombres que
        usa de la 4.2 y la 4.3" con esos nombres y firmas. **Si el editor no tiene el contrato del AC 24
        de la 4.2** (parámetros `Input`, `MinimoVigenteDesde` y `TurnosPorSemanaAntes`; campos `Input.…`;
        `FormularioAgenda` no `sealed` con `AplicarAccionDeEdicion()`; la T46 de la 4.2 en verde),
        **DETENERSE**: cambian la pantalla y las pruebas T20–T28, y se corrige la story primero.
  - [ ] Confirmar las piezas de la 4.1 y la 3.2 que se citan: `AgendaMedico` (constructor, `AgregarFranja`,
        `LargoMaximoMotivo`, `Hoy` privado, y de la 4.3 `FechaDeInicioYaPaso` y `ProximaAprobadaDespuesDe`),
        `IConsultaAgendas`, `GestionMedicosEnBase.CrearAsync` (transacción
        explícita), `FichaMedico.razor` con el comentario de la decisión 10, `IActorVigente`,
        `AccionesAuditoria.AgendaCorregida`, `InterceptorQueSeAdelanta`, `DatosDeAgenda`, `AgendaBuilder`.
  - [ ] Las decisiones 1, 10, 11, 12, 13 y 25 están tomadas (ver "Decisiones tomadas por el docente").
  - [ ] `bash scripts/ci/local.sh` en verde antes de empezar. Si algo no coincide → DETENERSE.
- [ ] [SEQ] Task 2: Dominio, con las pruebas primero (AC: 1–7)
  - [ ] `.Corregida(reloj)` en `AgendaBuilder`; T1–T8 en rojo.
  - [ ] `MotivoDeCorreccion`; `AgendaMedico.CorreccionDeUrgencia` y `EsCorreccion` (ver Implementación).
  - [ ] T1–T8 en verde, y las pruebas de dominio de la 4.1 siguen en verde.
- [ ] [SEQ] Task 3: Caso de uso (AC: 8–16)
  - [ ] `ICorreccionDeAgenda` y sus registros; `CorreccionDeAgendaEnBase`; registro `Scoped`.
  - [ ] `GuardarCorreccionAsync` en `DatosDeAgenda`; T9–T19 en verde (T15 con el espía de la 4.3).
- [ ] [SEQ] Task 4: Pantallas (AC: 17–26)
  - [ ] `RutasPersonal.CorregirAgenda(long id)`; `FormularioCorreccion : FormularioAgenda`;
        `CorregirAgenda.razor` con `OnSubmit` (ver Implementación concreta).
  - [ ] `FichaMedico.razor`: agenda vigente, enlace `Corregir agenda`, texto `correccion`; quitar del
        comentario de la cabecera "los agrega la 4.4".
  - [ ] T20–T28 en verde (T27 con `TurnosFueraDeAgendaFijos` de la 4.3).
- [ ] [SEQ] Task 5: Cierre (AC: 28)
  - [ ] Suite completa y `bash scripts/ci/local.sh` en verde; cobertura en el Dev Agent Record.
  - [ ] Pasada del **tester no técnico** (B-6) sobre la guía "Cómo verificar" y sobre las dos pantallas:
        ¿se entiende qué hace "Corregir agenda", qué pasa con la propuesta pendiente y por qué pide motivo?
        Lo que no se entienda se corrige en el texto de la pantalla y se anota.
  - [ ] Prueba en vivo con Compose (Cómo verificar §4), con scripts que buscan por texto o rol (L-14).

### Task Dependencies

T1 → T2 → T3 → T4 → T5. T2 no necesita Docker. T1 depende de que la 4.2 y la 4.3 estén en done (orden
del sprint: 2.3 → 4.2 → 4.3 → **4.4** → 5.1).

## Dev Notes

### Que existe hoy

#### En hospital-turnos (verificado 2026-10-17 contra `main` en `a8432c3`, con 1.1 a 4.1 en done; 4.2 y 4.3 se escriben en paralelo)

- **`AgendaMedico`** (`Dominio/Agendas/AgendaMedico.cs`, 4.1): constructor `(medicoId, duracionTurnoMinutos,
  vigenteDesde)` que valida la duración y deja `Borrador`; `AgregarFranja` valida la superposición con el
  mensaje del AC 4 de la 4.1; `Estado`, `AprobadaEl`, `EnviadaEl` con `private set`; constantes
  `DuracionMinima`, `DuracionMaxima`, `LargoMaximoMotivo = 500`; en `Franja`, `NombreDelDia` y `Tramo` son
  `internal`; helpers privados `Hoy(TimeProvider)`
  (fecha local con `GetLocalNow()`) y `Fecha(DateOnly)` (`dd/MM/yyyy` invariante). `QueRigeEn` desempata
  por `AprobadaEl`. No hay ningún camino a `Aprobada` que no sea `Aprobar`.
- **Tabla `agendas_medico`** (migración `20261017025538_AgregarAgendas`): `enviada_el` y `aprobada_el`
  anulables, sin `CHECK` que relacione estado y marcas: una aprobada sin `enviada_el` se puede guardar
  sin migración. `turnos_app` tiene `SELECT, INSERT, UPDATE` y no `DELETE`. Hay un único parcial
  `ux_agendas_medico_una_enviada` (sólo enviadas): dos aprobadas del mismo médico conviven.
- **`IConsultaAgendas`** (`Aplicacion/Agendas/`): `AgendaQueRigeAsync(medicoId, fecha, ct)` y
  `EstadosDeAgendaAsync(ids, ct)`. Implementada por `ConsultaAgendasEnBase(HospitalDbContext, TimeProvider)`.
- **`GestionMedicosEnBase`** (3.2 + 4.1): cinco parámetros; `CrearAsync` muestra el patrón de esta story:
  `ExigirAdministradorActivoAsync` primero, transacción explícita, `SaveChangesAsync`, `auditoria.Registrar`,
  `SaveChangesAsync`, `CommitAsync`. Constante `NoExiste = "No existe el médico pedido."`.
- **`FichaMedico.razor`** (`Components/Personal/Administracion/`): `@page "/personal/admin/medicos/{Id:long}"`,
  SSR, `[Authorize(Roles = Administrador)]`; muestra `Estado de agenda: <strong>…</strong>` con
  `TablaMedicos.TextoDe(ficha.Agenda)`; `switch` de `Resultado` con tres textos fijos; el 404 con
  `ManejadorGlobalDeErrores.FijarEstadoEnPagina`. Su comentario de cabecera dice "La agenda vigente y
  «Corregir agenda» los agrega la 4.4 (decisión 10)".
- **`RutasPersonal`** (`Web/Personal/`): `FichaMedico(long id)`, `Medicos`, `Propuestas`; las rutas de
  dentro de A1 quedan fuera del menú.
- **`AccionesAuditoria.AgendaCorregida = "agenda.corregida"`** existe desde la 1.3. No se crean constantes.
- **`IActorVigente.ExigirAdministradorActivoAsync(actor, mensaje, ct)`** (2.2) y `ActorDeSesion.Desde(User)`.
- **Ayudas de prueba**: `AgendaBuilder`, `RelojDePrueba` (Dominio.Tests); `DatosDeAgenda` con `Reloj`,
  `Proveedor`, `MedicoPropioAsync`, `GuardarAgendaAsync`, `ArmarAgenda`, `Franja` (Aplicacion.Tests);
  `InterceptorQueSeAdelanta`, `UsuariosDePrueba`, `ContextoDePrueba` (Aplicacion.Tests/Ayudas);
  `PersonalWebApplicationFactory` (con `Reloj` en hora real y `admin1` = "Lucía Pérez"),
  `FormularioHttp.EnviarAsync`/`IngresarAsync`/`Estado`/`Alerta`/`CrearUsuarioAsync`, `DatosDeCatalogo`
  (Web.Tests). bUnit está en Web.Tests.
- **El renderizado interactivo está habilitado** en `Program.cs` (`AddInteractiveServerComponents`), pero
  todas las pantallas de hoy son SSR estáticas. M2 de la 4.2 también (decisión 3), y esta pantalla igual.
- **No existen todavía** (los crean la 4.2 y la 4.3 antes que ésta): el editor, el resumen, `DatosAgenda`,
  `ITurnosFueraDeAgenda`, `FechaDeInicioYaPaso`, `ProximaAprobadaDespuesDe` y la pantalla A6. `/personal/admin/propuestas` y `/personal/medico/mi-propuesta`
  siguen "en construcción" en `main`.
- **`UsuarioPersonal`** tiene `Email` (de Identity) pero `Medico` no tiene email propio y nadie lo carga
  todavía (lo hará la 2.4, decisión 1). La corrección no manda email (AC 14).

### Patron de referencia verificado

| Proyecto | Ancla | Qué se verificó exactamente |
|---|---|---|
| hospital-turnos | `src/HospitalTurnos.Infraestructura/Catalogo/GestionMedicosEnBase.cs` (`CrearAsync`) | Permiso en la base primero, transacción explícita porque el id nace al guardar y la auditoría lo necesita, todo o nada. `CorreccionDeAgendaEnBase.CorregirAsync` copia esa forma. |
| hospital-turnos | `src/HospitalTurnos.Web/Components/Personal/Administracion/FichaMedico.razor` | Página SSR con `EditForm` con nombre, `catch (Exception e) when (ManejadorGlobalDeErrores.EsEsperable(e))` + `FijarEstadoEnPagina`, `NavigateTo` fuera del `try`, 404 sin `StatusCode` a mano, textos fijos para `?resultado=`. `CorregirAgenda.razor` copia esa forma. |
| hospital-turnos | `src/HospitalTurnos.Dominio/Agendas/AgendaMedico.cs` (`Rechazar`) | Motivo con `Trim`, vacío y largo máximo con `LargoMaximoMotivo`. `MotivoDeCorreccion` hace lo mismo con su texto propio. |
| hospital-turnos | `tests/HospitalTurnos.Aplicacion.Tests/Ayudas/InterceptorQueSeAdelanta.cs` | El doble de carreras de la 3.1. T19 lo reusa con su `INSERT`; acá prueba que **no** hay rechazo. |
| changomas-testing-csharp | `src/ChangoMas.Dominio/Venta.cs` | Estado privado que sólo cambian los métodos de la entidad. Por eso la corrección es un método **de `AgendaMedico`** (único que puede poner `Aprobada` sin `Aprobar`) y no un caso de uso que escribe el estado. |

### Que hay que hacer (la delta)

1. Dominio: `AgendaMedico.CorreccionDeUrgencia` + `EsCorreccion`, y `MotivoDeCorreccion`.
2. Un caso de uso con dos operaciones (leer lo que la pantalla necesita y corregir), registrado en el contenedor.
3. Una pantalla nueva dentro de A1 y dos agregados en la ficha.
4. Sin migración, sin constantes de auditoría nuevas, sin librerías.

### Decisiones de esta story (con su porqué)

- **Un método de fábrica en `AgendaMedico`, no una transición nueva.** FR-14b define el ciclo de la
  propuesta; la corrección no es una propuesta. Si fuera una transición (`Borrador → Aprobada`), el
  diagrama de Arq. §6 tendría una flecha que sólo usa el administrador, y la tabla del AC 13 de la 4.1
  dejaría de ser cierta. Como fábrica, la máquina de estados no cambia y la corrección nace aprobada.
  Vive en la entidad porque es la única que puede poner `Estado` y `AprobadaEl`.
- **La fábrica usa el constructor y `AgregarFranja`.** Así la duración y la superposición se validan
  con el mismo código y los mismos mensajes de la 4.1 (una regla, una vez).
- **El motivo es un objeto del dominio aparte** (`MotivoDeCorreccion`) y no un parámetro de la fábrica:
  el motivo no se guarda en la agenda (decisión 13), va a la auditoría; pero su regla (vacío, largo) sigue en el
  dominio y se prueba sin base (T8).
- **Sin índice único para las correcciones** (AC 16): la 4.1 eligió desempatar por `AprobadaEl` justamente
  para permitir corregir una aprobada con otra de la misma fecha. La carrera se prueba igual con el doble
  (T19), para dejar escrito que no rompe nada.
- **La pantalla de corrección es una página propia** (`/corregir-agenda`) y no un formulario dentro de la
  ficha: la ficha ya tiene su `EditForm` con nombre (`medico`), y la grilla es grande.
- **Precargar con la vigente**: una corrección casi siempre cambia una franja o la duración; arrancar de
  cero obligaría a recargar todo en una urgencia.
- **FR-15 con confirmación en dos pasos** (AC 23, decisión 25): la misma idea de A6 ("ve la lista antes
  de decidir"), pero en A6 el aviso está antes del botón y acá la agenda se escribe en el mismo
  formulario, así que el aviso aparece recién al apretar `Corregir agenda`. Mismo puerto y mismo
  componente que A6. Hoy nunca se ve (no hay turnos); el día que la 5.3 cumpla su obligación con la 4.3,
  esta pantalla ya está lista.
- **El formulario hereda de `FormularioAgenda`** (C-1 de la revisión): así los campos que dibuja el
  editor (`Input.…`) llegan a la agenda corregida, y los botones del editor funcionan sin pedir el
  motivo. Un modelo que *contuviera* un `FormularioAgenda` dejaría los campos con otro nombre.
- **El aviso de reemplazo se calcula contra la fecha del formulario** (I-2 de la revisión): la
  administradora puede elegir un `Rige desde` posterior a la aprobada futura, y entonces esa aprobada no
  la reemplaza. La pantalla recibe las aprobadas y llama a la regla de la 4.3 en cada dibujo.

### Decisiones tomadas por el docente (2026-10-17)

Numeradas como en `revision-sprint-3.md` ("Decisiones abiertas consolidadas"). Se aceptaron todas con
la opción recomendada; los AC y las pruebas ya las reflejan.

- **Decisión 10 — ¿Desde cuándo puede regir una corrección?** (era D-1; AC 3, T3, T4). **Elegido:**
  desde hoy o cualquier fecha posterior; la pantalla propone hoy. Es para urgencias.
- **Decisión 11 — Propuesta enviada pendiente del mismo médico** (era D-2; AC 13, 19, T17). **Elegido:**
  sigue enviada, sin cambios, con un aviso en la pantalla; si el administrador la aprueba, reemplaza a la
  corrección desde su fecha (FR-14d).
- **Decisión 1 — ¿Se avisa por email al médico?** (era D-3, junto con la D-1 de la 4.3; AC 14, T18).
  **Elegido:** no. FR-14f no lo pide y el médico ve su agenda vigente en M2. El email del personal lo
  carga la 2.4 (sprint 4) para los avisos de la 4.3; la corrección sigue sin mandar.
- **Decisión 12 — Corregir la agenda de un médico sin ninguna** (era D-4; AC 15, T11, T22). **Elegido:**
  sí. Cubre al médico nuevo que tiene que atender ya y todavía no tiene usuario para proponer.
- **Decisión 13 — ¿Dónde queda el motivo?** (era D-5; AC 2, 7, 11). **Elegido:** sólo en la auditoría
  (`Detalle.motivo`), sin migración; una corrección se reconoce por ser aprobada sin `EnviadaEl`.
- **Decisión 25 — Turnos fuera de agenda: ¿confirmación extra?** (de la revisión, I-2; AC 12, 23, T15,
  T27). **Elegido:** la corrección pide `Corregir igual`; A6 no tiene ese paso. Las dos con el mismo
  puerto y el mismo componente de aviso de la 4.3.

### Implementacion concreta

**`AgendaMedico`** — lo que se agrega (el resto no cambia):

```csharp
/// <summary>
/// FR-14f, S-32: el administrador crea una agenda YA APROBADA, sin propuesta, para urgencias. No es una
/// transición del ciclo de la propuesta (FR-14b): la máquina de estados no cambia. Sin la regla de los 7 días;
/// rige desde hoy o después (decisión 10). El motivo no vive acá: va a la auditoría (decisión 13).
/// </summary>
public static AgendaMedico CorreccionDeUrgencia(long medicoId, int duracionTurnoMinutos, DateOnly vigenteDesde,
    IEnumerable<Franja> franjas, TimeProvider reloj)
{
    var agenda = new AgendaMedico(medicoId, duracionTurnoMinutos, vigenteDesde);   // 1. duración (4.1)
    foreach (var franja in franjas)
        agenda.AgregarFranja(franja);                                             // 2. superposición (4.1)
    if (agenda._franjas.Count == 0)
        throw new ReglaDeNegocioException("Agregá al menos una franja para corregir la agenda.");
    if (agenda.FechaDeInicioYaPaso(reloj))                                        // regla de la 4.3 (M-6), fecha del HOSPITAL
        throw new ReglaDeNegocioException($"La corrección tiene que regir desde hoy ({Fecha(Hoy(reloj))}) o después.");
    agenda.Estado = EstadoPropuesta.Aprobada;
    agenda.AprobadaEl = reloj.GetUtcNow();
    return agenda;
}

// Aprobada sin haber sido enviada: sólo una corrección llega a eso.
public bool EsCorreccion => Estado == EstadoPropuesta.Aprobada && EnviadaEl is null;
```

**`MotivoDeCorreccion`** (`Dominio/Agendas/`):

```csharp
public sealed class MotivoDeCorreccion
{
    public string Texto { get; }

    public MotivoDeCorreccion(string? texto)
    {
        var recortado = texto?.Trim();
        if (string.IsNullOrEmpty(recortado))
            throw new ReglaDeNegocioException("Escribí el motivo de la corrección.");
        if (recortado.Length > AgendaMedico.LargoMaximoMotivo)
            throw new ReglaDeNegocioException($"El motivo tiene como máximo {AgendaMedico.LargoMaximoMotivo} caracteres.");
        Texto = recortado;
    }
}
```

**`CorreccionDeAgendaEnBase.CorregirAsync`** — el orden del AC 10:

```csharp
actor = await actorVigente.ExigirAdministradorActivoAsync(actor, SinPermiso, ct);            // AC 9
if (!await contexto.Medicos.AnyAsync(m => m.Id == medicoId, ct))
    throw new NoEncontradoException(NoExiste);                                               // AC 10
var motivo = new MotivoDeCorreccion(datos.Motivo);                                           // AC 7
var franjas = datos.Agenda.Franjas.Select(f => new Franja(f.Dia, f.Desde, f.Hasta));
var agenda = AgendaMedico.CorreccionDeUrgencia(medicoId, datos.Agenda.DuracionTurnoMinutos,
    datos.Agenda.VigenteDesde, franjas, reloj);                                              // AC 1, 3, 4
var aprobadas = await contexto.AgendasMedico.AsNoTracking()
    .Where(a => a.MedicoId == medicoId && a.Estado == EstadoPropuesta.Aprobada).ToListAsync(ct);
var hasta = AgendaMedico.ProximaAprobadaDespuesDe(aprobadas, agenda.VigenteDesde)?.VigenteDesde;   // 4.3 AC 19 y 26
var fuera = await turnosFueraDeAgenda.BuscarAsync(agenda, hasta, ct);                        // AC 12, firma de la 4.3
if (fuera.Count > 0 && !datos.ConfirmaTurnosFuera)
    return new ResultadoCorreccion(null, fuera);
var hoy = DateOnly.FromDateTime(reloj.AhoraLocal());
var anterior = await consultaAgendas.AgendaQueRigeAsync(medicoId, hoy, ct);                  // agendaAnteriorId

await using var transaccion = await contexto.Database.BeginTransactionAsync(ct);             // AC 11
contexto.AgendasMedico.Add(agenda);
await contexto.SaveChangesAsync(ct);
auditoria.Registrar(actor, AccionesAuditoria.AgendaCorregida, "AgendaMedico", agenda.Id.ToString(CultureInfo.InvariantCulture), new
{
    medicoId, motivo = motivo.Texto, vigenteDesde = agenda.VigenteDesde.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
    duracionTurnoMinutos = agenda.DuracionTurnoMinutos,
    franjas = agenda.Franjas.Select(f => new { dia = (int)f.Dia, desde = f.Desde.ToString("HH:mm", CultureInfo.InvariantCulture),
        hasta = f.Hasta.ToString("HH:mm", CultureInfo.InvariantCulture) }),     // formato del AC 17 de la 4.2: medianoche = "00:00"
    agendaAnteriorId = anterior?.Id, turnosFueraDeAgenda = fuera.Count,
});
await contexto.SaveChangesAsync(ct);
await transaccion.CommitAsync(ct);
return new ResultadoCorreccion(agenda.Id, fuera);
```

`Franja` no cambia: el día va como número (`(int)f.Dia`) y la medianoche como `00:00`, el formato del AC 17
de la 4.2 (I-6 de la revisión). `NombreDelDia` y `Tramo` siguen `internal`.

**`CorregirAgenda.razor`** — forma (SSR, como `FichaMedico.razor` y M2 de la 4.2):

```razor
@page "/personal/admin/medicos/{Id:long}/corregir-agenda"
@layout LayoutPersonal
@attribute [Authorize(Roles = HospitalTurnos.Dominio.Personal.Roles.Administrador)]
@inject ICorreccionDeAgenda Correccion
@inject NavigationManager Navegacion

@* FR-14f: la misma grilla de M2 (4.2), con motivo obligatorio. La agenda nace aprobada (S-32). *@
<EditForm Model="Input" method="post" OnSubmit="AlEnviarAsync" FormName="correccion">
    <DataAnnotationsValidator />
    <EditorDeAgenda Input="Input" MinimoVigenteDesde="datos.Hoy" TurnosPorSemanaAntes="@datos.Vigente?.TurnosPorSemana()" />
    @* aviso de reemplazo (AC 19): AgendaMedico.ProximaAprobadaDespuesDe(datos.Aprobadas, Input.VigenteDesde ?? datos.Hoy) *@
    <label for="motivo">Motivo de la corrección (obligatorio)</label>
    <InputTextArea id="motivo" @bind-Value="Input.Motivo" maxlength="500" />
    <ValidationMessage For="() => Input.Motivo" />
    @if (turnosFuera.Count > 0)
    {
        <AvisoTurnosFueraDeAgenda Turnos="turnosFuera" />   @* 4.3 AC 22 *@
        <button type="submit" name="Input.Accion" value="corregir-igual">Corregir igual</button>
    }
    <button type="submit" name="Input.Accion" value="corregir">Corregir agenda</button>
</EditForm>
```

`FormularioCorreccion : FormularioAgenda` (`Components/Personal/Administracion/FormularioCorreccion.cs`;
hereda los campos, `Accion`, `AplicarAccionDeEdicion()` y `ADatos()` de la 4.2) suma `string? Motivo` con
`[Required(ErrorMessage = "Escribí el motivo de la corrección.")]` y
`[StringLength(500, ErrorMessage = "El motivo tiene como máximo 500 caracteres.")]` (los textos del
dominio, AC 21), y `public bool ConfirmaTurnosFuera => Accion == "corregir-igual";`. `[Required]` rechaza
también un texto de sólo espacios (con `AllowEmptyStrings = false`, el valor por defecto). El caso de uso
igual lo valida con `MotivoDeCorreccion` (T13): la pantalla no es la única defensa (NFR-10). La
propiedad de la página es `[SupplyParameterFromForm(FormName = "correccion")] FormularioCorreccion? Input`.

En `OnInitializedAsync`: `datos = ObtenerAsync(Id)`; `null` en GET → 404 con `FijarEstadoEnPagina` (L-1);
`Input ??= ` uno precargado con `FormularioAgenda.Desde(vigente)` (o vacío), `VigenteDesde = hoy`; el
formulario se dibuja recién con el modelo listo (L-8). `AlEnviarAsync(EditContext contexto)`, con el
orden de M2 (4.2):

```csharp
private async Task AlEnviarAsync(EditContext contexto)
{
    try
    {
        if (Input!.AplicarAccionDeEdicion()) return;                     // AC 18: redibuja, sin validar el motivo
        if (Input.Accion is not ("corregir" or "corregir-igual"))
            throw new ReglaDeNegocioException(AccionDesconocida);         // 400, texto de la 3.1/4.2
        if (!contexto.Validate()) return;                                 // AC 21: 200 con el mensaje del motivo
        var resultado = await Correccion.CorregirAsync(Id,
            new DatosCorreccion(Input.ADatos(), Input.Motivo, Input.ConfirmaTurnosFuera),
            ActorDeSesion.Desde(HttpContext.User), HttpContext.RequestAborted);
        if (resultado.AgendaId is null) { turnosFuera = resultado.TurnosFueraDeAgenda; return; }   // AC 23
    }
    catch (NoEncontradoException e) { /* 404 de la página, AC 24 */ … return; }
    catch (Exception e) when (ManejadorGlobalDeErrores.EsEsperable(e))
    {
        ManejadorGlobalDeErrores.FijarEstadoEnPagina(HttpContext, e);    // AC 22: 400/403 con el cartel
        error = e.Message;
        return;
    }
    Navegacion.NavigateTo($"{RutasPersonal.FichaMedico(Id)}?resultado=correccion");   // fuera del try
}
```

`turnosFuera` empieza como lista vacía (`IReadOnlyList<TurnoFueraDeAgenda>`, `[]`).

**`FichaMedico.razor`** — lo que se agrega: `@inject IConsultaAgendas Agendas` y `@inject TimeProvider Reloj`;
en `OnInitializedAsync`, `vigente = await Agendas.AgendaQueRigeAsync(Id, DateOnly.FromDateTime(Reloj.AhoraLocal()), ct)`;
debajo del estado, `<h2>Agenda vigente</h2>` + `<ResumenDeAgenda Agenda="vigente" />` o
`Sin agenda vigente.`, y `<a href="@RutasPersonal.CorregirAgenda(Id)">Corregir agenda</a>`; en el
`switch`, `"correccion" => "Agenda corregida."`.

**`RutasPersonal`**: `public static string CorregirAgenda(long id) => $"/personal/admin/medicos/{id}/corregir-agenda";`
(fuera del menú, como `FichaMedico`).

**Registro**: `servicios.AddScoped<ICorreccionDeAgenda, CorreccionDeAgendaEnBase>();` con el comentario de la story.

### Archivos a crear/modificar

| Acción | Archivo |
|---|---|
| Crear | Los de `files_to_create` del frontmatter |
| Modificar | `src/HospitalTurnos.Dominio/Agendas/AgendaMedico.cs` (`CorreccionDeUrgencia`, `EsCorreccion`) |
| Modificar | `src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs` (registro `Scoped`) |
| Modificar | `src/HospitalTurnos.Web/Personal/RutasPersonal.cs` (`CorregirAgenda(long id)`) |
| Modificar | `src/HospitalTurnos.Web/Components/Personal/Administracion/FichaMedico.razor` (agenda vigente, enlace, texto `correccion`) |
| Modificar | `tests/HospitalTurnos.Dominio.Tests/Ayudas/AgendaBuilder.cs` (`.Corregida(reloj)`) |
| Modificar | `tests/HospitalTurnos.Aplicacion.Tests/Agendas/DatosDeAgenda.cs` (`GuardarCorreccionAsync`) |

### Lo que NO hay que hacer

- **NO agregar una transición** `Borrador → Aprobada` ni tocar `Enviar`/`Aprobar`/`Rechazar`: la corrección
  es una fábrica (AC 1).
- **NO escribir `Estado` ni `AprobadaEl` desde el caso de uso ni por reflexión**: sólo `AgendaMedico` los cambia.
- **NO escribir otra grilla, otro resumen ni otro cálculo de turnos por semana**: son de la 4.2.
- **NO armar el modelo con un `FormularioAgenda` adentro** ni usar `OnValidSubmit`: hereda y usa
  `OnSubmit` con el orden de M2 (C-1 de la revisión).
- **NO crear otro puerto ni otra implementación de turnos fuera de agenda**: es el de la 4.3. Tampoco
  otro doble ni otra fábrica web: `TurnosFueraDeAgendaEspia` y `TurnosFueraDeAgendaFijos` de la 4.3.
- **NO escribir a mano el texto del aviso de turnos fuera**: es `AvisoTurnosFueraDeAgenda` de la 4.3.
- **NO tocar `Franja.cs`** ni escribir los días con nombre en la auditoría: formato del AC 17 de la 4.2.
- **NO crear constantes de auditoría**: `AgendaCorregida` ya existe.
- **NO crear migraciones** (decisión 13) ni un índice único para las aprobadas (AC 16).
- **NO tocar la propuesta enviada del médico** (decisión 11) ni encolar emails (decisión 1).
- **NO agregar la ruta al menú lateral**: se entra desde la ficha (rompería `MenuLateralTests` de la 2.1).
- **NO cambiar `DatosFichaMedico` ni `IGestionMedicos`** (3.2): la ficha lee la vigente con `IConsultaAgendas`.
- **NO fijar `StatusCode` a mano** en las páginas (L-1) ni mostrar en la ficha un texto que venga en la consulta.
- **NO leer la hora del sistema** ni calcular "hoy" con la fecha UTC (rompe T4 y `HoraDelSistemaTests`).
- **NO limpiar tablas en las pruebas**: filtrar por el médico propio.

### Como verificar que funciona

```bash
cd ~/SourceCode/hospital-turnos

# 1. Unitarias (sin Docker, segundos)
dotnet test tests/HospitalTurnos.Dominio.Tests --filter "Story=4.4"
# Esperado: T1–T8 (más los casos de cada [Theory]), 0 fallas

# 2. Todas las de la story (Docker para Testcontainers)
dotnet test --filter "Story=4.4"
# Esperado: 28 pruebas declaradas, 0 fallas

# 3. Suite completa y CI local (AC 28)
dotnet test
bash scripts/ci/local.sh; echo "rc=$?"     # rc=0, cobertura de Dominio y Aplicacion >= 80 %

# 4. Prueba en vivo
docker compose up -d --build
```

**Guía para el tester no técnico** (en el navegador, `http://localhost:8080/personal/ingreso`, como
administrador). Cada paso dice qué hacer y qué tiene que ver:

1. Entrá a **Médicos** y abrí la ficha de un médico que tenga agenda. Tenés que ver **Agenda vigente**
   con sus horarios ("turnos de 20 min · Lun 8–12…") y un enlace **Corregir agenda**.
2. Tocá **Corregir agenda**. Arriba dice para qué sirve (urgencias). La grilla viene cargada con la
   agenda de hoy y "Rige desde" dice la fecha de hoy.
3. Sacá una franja, dejá el motivo vacío y tocá **Corregir agenda**: aparece "Escribí el motivo de la
   corrección." y lo que cambiaste sigue ahí.
4. Escribí el motivo "Se inundó el consultorio 3" y volvé a tocar **Corregir agenda**: volvés a la ficha
   con "Agenda corregida." y la agenda nueva. En **Médicos**, su estado dice "Vigente".
5. Probá poner en "Rige desde" la fecha de ayer: tiene que decir "La corrección tiene que regir desde hoy
   (…) o después." y no guardar nada.
6. Con un médico que tenga una propuesta enviada (cargada desde M2 con un usuario médico), abrí
   **Corregir agenda**: tiene que avisar que la propuesta sigue y que se revisa en «Propuestas de agenda».
   Después de corregir, en **Médicos** sigue diciendo "Propuesta pendiente".
7. Entrá como recepción y pegá la dirección de la corrección: "No tenés permiso para entrar a esta sección."
8. En **Auditoría** (cuando exista, 9.1) o con la consulta de abajo: el registro con tu nombre y el motivo.

```bash
docker compose exec db psql -U postgres -d hospital_turnos -c \
  "SELECT quien, accion, detalle->>'motivo' FROM registros_auditoria WHERE accion = 'agenda.corregida' ORDER BY id DESC LIMIT 3;"
```

Los scripts que manejen el navegador buscan por texto o rol (`Corregir agenda`, `role="status"`), no por
clases CSS, y cuentan con que el enlace "Médicos" aparece también en el menú (L-14).

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

### Change Log

- 2026-10-17: Story escrita (Mary) contra `main` (`a8432c3`, 1.1 a 4.1 en done), en paralelo con 2.3, 4.2,
  4.3 y 5.1. 28 AC, 27 verificaciones, 5 decisiones abiertas para el docente, 6 nombres asumidos de la
  4.2 y la 4.3 marcados `[ASUMIDO A-n]` para que la revisión los iguale.
- 2026-10-17: Correcciones de `revision-sprint-3.md` y decisiones 1, 10, 11, 12, 13 y 25 del docente
  (opción recomendada; marcas reemplazadas y sección "Decisiones tomadas"). C-1: `FormularioCorreccion`
  hereda de `FormularioAgenda`, propiedad `Input`, `OnSubmit` con el orden de M2 y botones `corregir` /
  `corregir-igual`; los botones del editor no piden el motivo (AC 18, T28 nueva); sin "cálculo en vivo";
  la Task 1 se detiene si el editor no tiene el contrato del AC 24 de la 4.2. I-2: firma de la 4.3 con
  `hasta` por `ProximaAprobadaDespuesDe` (T15), `AvisoTurnosFueraDeAgenda`, aviso de reemplazo contra la
  fecha del formulario (`DatosParaCorregir.Aprobadas` en lugar de `AprobadaFuturaDesde`), dobles de la
  4.3 (sin `TurnosFueraDeAgendaDePrueba` ni `CorreccionWebApplicationFactory`). I-6: franjas de la
  auditoría con el formato de la 4.2, sin tocar `Franja.cs` (T10). M-2, M-6 (`FechaDeInicioYaPaso`), M-7
  (pruebas web en `Web.Tests/Agendas/`). Marcas `[ASUMIDO A-n]` quitadas. 28 AC, 28 verificaciones.
