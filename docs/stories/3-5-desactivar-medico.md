---
epic: 3
story: "3.5"
title: "Desactivar y reactivar un médico, con filtro Activos/Inactivos en A1"
status: ready-for-dev
sprint: 3
priority: "P1"
approach: "[ADAPT]"
depends_on: ["2.2", "3.2", "3.3", "4.2", "4.3", "4.4"]
followup_of:
  - id: "3.2"
    type: "extension"
    summary: "Los médicos pasan a tener baja lógica: desactivar y reactivar desde A1 (menú de la fila y sección final de la ficha), con filtro Activos/Inactivos/Todos"
followup_reason: cambio-de-requisito
runtime_not_verifiable:
  - ac: 5
    reason: "Habla del esquema de la base (columna activo sin valor por defecto, xmin sin columna nueva, permisos). No se ve en la app: lo prueban T5 contra la base de pruebas y R1 con psql contra la base del Compose."
    covered_by: T5
  - ac: 12
    reason: "Dos guardados sobre el mismo médico en el mismo instante no se pueden provocar a mano con dos pestañas: la segunda siempre lee después de que la primera guardó y recibe la regla del dominio (AC 10). T12 fuerza la carrera con InterceptorQueSeAdelanta."
    covered_by: T12
  - ac: 13
    reason: "La tabla de turnos no existe hasta la 5.3: no hay turnos que mostrar en la app. Lo fija la prueba-trampa T13, que falla el día que aparezcan."
    covered_by: T13
  - ac: 31
    reason: "Es el contrato con stories que todavía no existen (5.2, 5.3, 5.5, 6.4, 7.1, 8.1, 8.2, 9.1). Lo fijan T13 y T15; cada una de esas stories lo prueba al implementarse."
    covered_by: T13
  - ac: 33
    reason: "Habla de las pruebas existentes y de la CI. No hay nada que mirar en la app en ejecución: lo prueban la corrida de las suites (R2) y el workflow CI (.github/workflows/ci.yml) sobre el PR de esta story (R3)."
    covered_by: R2
files_to_create:
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/<marca>_AgregarBajaLogicaDeMedicos.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/<marca>_AgregarBajaLogicaDeMedicos.Designer.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Catalogo/BajaLogicaDeMedicosTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Catalogo/ContratoTurnosFuturosDelMedicoTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Catalogo/BajaDeMedicosInteractivaTests.cs"
files_to_modify:
  - "src/HospitalTurnos.Dominio/Catalogo/Medico.cs"
  - "src/HospitalTurnos.Dominio/Auditoria/AccionesAuditoria.cs"
  - "src/HospitalTurnos.Aplicacion/Catalogo/IGestionMedicos.cs"
  - "src/HospitalTurnos.Aplicacion/Agendas/IPropuestaAgenda.cs"
  - "src/HospitalTurnos.Infraestructura/Catalogo/GestionMedicosEnBase.cs"
  - "src/HospitalTurnos.Infraestructura/Agendas/PropuestaAgendaEnBase.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/MedicoConfiguracion.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/HospitalDbContextModelSnapshot.cs"
  - "src/HospitalTurnos.Web/Components/Comunes/VistaDeDetalle.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/Medicos.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/TablaMedicos.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/FichaMedico.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Agendas/MiPropuesta.razor"
  - "src/HospitalTurnos.Web/wwwroot/app.css"
  - "tests/HospitalTurnos.Dominio.Tests/Catalogo/MedicoTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Catalogo/GestionMedicosEnBaseTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Catalogo/MedicosInteractivaTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Personal/UsuariosInteractivaTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Comunes/ComponentesComunesTests.cs"
  - "docs/stories/00-contrato-tecnico.md"
  - "docs/04-arquitectura.md"
---

# Story 3.5: Desactivar y reactivar un médico, con filtro Activos/Inactivos en A1

Status: ready-for-dev

## Story

Como administradora del Hospital Las Acacias (Lucía),
quiero desactivar a un médico que deja de atender en el hospital y reactivarlo si vuelve, desde A1,
para que deje de ofrecerse para turnos y de armar propuestas de agenda sin perder sus datos, su agenda ni
su historia, y para poder ver con un filtro quiénes están inactivos.

**Por qué ahora.** La [guía UX/UI](../ux/guia-ux-ui-abm.md) pide baja lógica en toda entidad referenciada
por otras, "Desactivar" en el menú "⋯" de la fila o en una sección final de la ficha, y el filtro
"Activos / Inactivos / Todos" con "Activos" por defecto. La 3.3 no lo hizo porque los médicos no tenían baja
(brechas **B-3.5** y **B-4.5**, impacto N; decisión **D3** del
[análisis de brechas](../ux/analisis-brechas-abm.md): story nueva al backlog; el docente la sumó al S3 en la
sesión 17). Va **después** de la 4.3 y la 4.4 (decisión del docente) porque decide qué pasa con la agenda, las
propuestas pendientes y las correcciones de un médico desactivado, **sin reabrir** esas stories: esta story
agrega sus reglas en los archivos que ellas crean.

## Acceptance Criteria

> Convenciones: los textos entre comillas son exactos. "Ríos, Pablo" es un médico de ejemplo (personas como
> "Apellido, Nombre", guía §Formatos y los ejemplos de la propia guía: "Más acciones para Ríos, Pablo",
> "registrada para Pereyra, Martín José"). "Administrador" = el inicial del Compose o uno creado en la prueba.
> Los `[DECIDIDO S-5x]` son decisiones que ningún documento resolvía: las tomó el docente el 2026-10-23
> (ver "Preguntas para el docente").
>
> **Campos de auditoría** (AC 6 y 7), como la 3.2: actor devuelto por `IActorVigente.ExigirAdministradorActivoAsync`
> (nombre leído de la base), `Rol = Administrador`, `Canal = CanalAuditoria.Personal`, `EntidadTipo = "Medico"`,
> `EntidadId` = id del médico, `Detalle = {"medico":"Ríos, Pablo","matricula":"MP 1234"}`.

### A. Dominio — `Medico` con baja lógica (guía, "Baja, desactivación"; FR-13)

1. **Given** un médico recién creado con `new Medico(...)` **When** se mira `Activo` **Then** es `true`. El
   `true` lo pone la clase, no un valor por defecto de la base (mismo motivo que `Especialidad.Activa` y
   `UsuarioPersonal.Activo`: EF tomaría `false` como "sin valor").
2. **Given** un médico activo **When** se llama `Desactivar()` **Then** queda `Activo = false`. **Given** uno
   ya inactivo **When** se llama `Desactivar()` **Then** lanza `ConflictoException` con
   "El médico Ríos, Pablo ya está desactivado." y no cambia nada.
3. **Given** un médico inactivo **When** se llama `Reactivar()` **Then** queda `Activo = true`. **Given** uno
   activo **Then** lanza `ConflictoException` con "El médico Ríos, Pablo ya está activo."
4. **Given** un médico inactivo **When** se llama `CambiarDatos(...)` con datos válidos **Then** cambia los datos
   como hoy (3.2 AC 16 y 17) y sigue `Activo = false`: corregir un dato no reactiva.

### B. Persistencia

5. Migración `<marca>_AgregarBajaLogicaDeMedicos` (con `dotnet ef migrations add`): suma a `medicos` la columna
   `activo boolean NOT NULL`, que deja **en `true` a todos los médicos que ya existen** y queda **sin** valor por
   defecto en la base (el `defaultValue: true` se usa sólo para llenar las filas existentes y después se quita);
   y mapea la concurrencia optimista con la columna de sistema `xmin` (propiedad sombra `uint Version`,
   `IsRowVersion()`, `HasColumnName("xmin")`, como `agendas_medico` en la 4.1 AC 27) **sin** crear ninguna
   columna. `turnos_app` sigue con `SELECT, INSERT, UPDATE` y sin `DELETE` sobre `medicos`
   (`[DECIDIDO S-56]` para `xmin`).

### C. Casos de uso — desactivar y reactivar (`IGestionMedicos`)

6. **Desactivar.** **Given** un médico activo y un administrador activo **When** se llama
   `DesactivarAsync(id, actor, ct)` **Then**, en este orden: (1) permiso en la base (AC 11); (2) carga con
   seguimiento (AC 10); (3) `Desactivar()` del dominio; (4) control de la propuesta pendiente (AC 9);
   (5) auditoría `AccionesAuditoria.MedicoDesactivado` (`"medico.desactivado"`) con los campos de la tabla de
   arriba; (6) **un solo** `SaveChangesAsync`. La fila **no** se borra. Su usuario del sistema vinculado **no**
   cambia: sigue activo y vinculado (`[DECIDIDO S-50]`).
7. **Reactivar.** **Given** un médico inactivo **When** se llama `ReactivarAsync(id, actor, ct)` **Then** el
   mismo orden sin el paso (4), `Reactivar()` del dominio y auditoría `AccionesAuditoria.MedicoReactivado`
   (`"medico.reactivado"`). Se reactiva aunque su especialidad esté desactivada (la conserva, 3.2 AC 17).
8. **La agenda se conserva** (`[DECIDIDO S-52]`; ADR-06: una aprobada no se edita nunca). **Given** un médico
   con una agenda aprobada que rige hoy, otra aprobada a futuro y un borrador **When** se lo desactiva y
   después se lo reactiva **Then** ninguna agenda cambia (estado, marcas, franjas); `EstadoDeAgenda.Calcular`
   sigue dando lo mismo, y al reactivarlo la vigente vuelve a regir sin hacer nada.
9. **Propuesta enviada pendiente** (`[DECIDIDO S-51]`). **Given** un médico con una propuesta `Enviada` **When**
   se lo desactiva **Then** lanza `ConflictoException` con
   "No se puede desactivar a Ríos, Pablo: tiene una propuesta de agenda esperando aprobación. Revisala en «Propuestas de agenda»."
   y no cambia nada (ni el médico, ni la propuesta, ni auditoría). Un `Borrador` o una `Rechazada` **no**
   bloquean y quedan como están.
10. **Inexistente o estado repetido.** **Given** un id que no existe **When** se desactiva o reactiva **Then**
    `NoEncontradoException` con "No existe el médico pedido." (texto de la 3.2). **Given** uno ya inactivo
    (desactivar) o ya activo (reactivar) **Then** el `ConflictoException` del AC 2 / 3. En todos, sin auditoría.
11. **Permiso en la base** (decisión 16, Arq. §7.7). **Given** un actor que no es un administrador activo en la
    base (recepcionista, médico, administrador desactivado con la sesión todavía viva, id inventado) **When**
    llama a `DesactivarAsync` o `ReactivarAsync` **Then** `AccesoDenegadoException` con
    "Sólo un administrador puede dar de alta o modificar médicos." (el de la 3.2), antes de leer nada, sin cambios.
12. **Dos administradores a la vez** (`[DECIDIDO S-56]`). **Given** dos alcances que leyeron el mismo médico
    **When** el primero guarda (desactiva, reactiva o edita) y después el segundo guarda otro cambio **Then** el
    segundo `SaveChangesAsync` lanza `DbUpdateConcurrencyException` y el caso de uso la traduce a
    `ConflictoException` con "Otro administrador acaba de cambiar este médico. Volvé a abrir la ficha para ver cómo quedó."
    Del segundo no queda nada: ni su cambio ni su auditoría. Vale también para `EditarAsync` de la 3.2 (deja de
    "ganar el último", M-6 de la 3.2).
13. **Turnos futuros** (como la 3.1 AC 10 y 17, decisión 5). Hoy no hay tabla de turnos (la crea la 5.3), así que
    desactivar **no** consulta turnos. Una prueba-trampa falla en cuanto el modelo tenga una entidad `Turno` o
    una tabla `turnos`, con el mensaje exacto:
    "La story 5.3 agregó los turnos: cumplí el contrato de la story 3.5 (AC 13: no desactivar un médico con turnos futuros; AC 31: la reserva verifica que el médico siga activo) y reemplazá esta prueba por las de turnos futuros del médico."
    Contrato para la 5.3: `DesactivarAsync` cuenta los turnos futuros no cancelados del médico (futuro = después
    de `TimeProvider.AhoraLocal()`) y, con N > 0, lanza `ConflictoException` con
    "No se puede desactivar a Ríos, Pablo: tiene N turnos futuros." ("1 turno futuro" en singular). La pantalla no
    cambia: el diálogo ya pasa a "no se puede" (3.3 AC 39 y 85; guía: "No se puede desactivar: hay turnos
    futuros asignados").
14. **Matrícula de un médico inactivo** (como la 3.1 AC 6 con una especialidad desactivada). **Given** un médico
    inactivo con la matrícula `MP 1234` **When** se da de alta otro con ` mp 1234 ` **Then** `ConflictoException`
    con "Ya hay un médico con la matrícula MP 1234: Ríos, Pablo, desactivado. Reactivalo desde Médicos con el filtro Inactivos."
    (debajo del campo Matrícula, como la 3.3 AC 68). Con un médico activo el texto sigue siendo el de hoy. La
    carrera contra el índice único sigue dando el texto de hoy (3.2 AC 8).

### D. Listado A1 — guía, "Barra de filtros", "Tabla" y "Baja"

15. **Given** un administrador en A1 **When** se dibuja **Then** la barra de filtros tiene, en este orden, el
    buscador, Especialidad, Estado de agenda y el `FiltroDeEstado` "Estado" con "Activos / Inactivos / Todos" y
    "Activos" elegido, y después "Limpiar filtros" (guía §Aplicación, Médicos, punto 2). Con "Activos" sólo se
    listan los activos y el encabezado dice "Médicos · N" con N = activos que cumplen los demás filtros.
16. **Given** A1 **When** se elige "Inactivos" o "Todos" **Then** la URL lleva `estado=inactivos` / `estado=todos`
    junto con los demás parámetros que no estén en su defecto (por ejemplo
    `?buscar=rios&estado=todos&especialidad=3&agenda=vigente&orden=-matricula&pagina=2`), los filtros activos
    muestran "Estado: Inactivos" / "Estado: Todos", la página vuelve a 1 y el total se recalcula; **When** se abre
    esa URL o se vuelve con Atrás **Then** el listado se restaura igual; **When** se pulsa "Limpiar filtros"
    **Then** vuelve a "Activos". Un `?estado=otro` se ignora y queda "Activos" (3.3 AC 26 y 80, sin cambios en
    `EstadoDeListado`).
17. **Insignia de inactivo** (`[DECIDIDO S-54]`). **Given** el filtro "Todos" **When** se dibuja la tabla
    **Then** aparece la columna "Estado" con `InsigniaDeEstado` "Activo" (verde, `.badge-estado--activo`) o
    "Inactivo" (gris, `.badge-estado--inactivo`); con "Activos" o "Inactivos" la columna no se muestra (como A2,
    3.3 AC 50). En la vista de tarjetas (menos de 768 px) un médico inactivo muestra además la insignia
    "Inactivo" con cualquier filtro. La columna "Estado de agenda" se sigue mostrando calculada (AC 8).
18. **Menú de la fila.** **Given** una fila **Then** sigue el "Editar" visible y el `MenuDeAcciones` ofrece:
    médico activo → "Vincular usuario" y "Desactivar"; médico inactivo → "Reactivar". Ninguna fila tiene un
    botón rojo. "Ver agenda" sigue sin mostrarse (no existe todavía).
19. **Desactivar desde la fila.** **Given** Ríos, Pablo activo **When** se elige "Desactivar" **Then** se abre
    el `DialogoDeConfirmacion` "¿Desactivar a Ríos, Pablo?" con la consecuencia
    "No se va a ofrecer para turnos nuevos y, si tiene usuario del sistema, no va a poder armar propuestas de agenda. Sus datos y su agenda se conservan."
    y los botones "Cancelar" (secundario, **con el foco**) y "Desactivar médico" (rojo, el único rojo de la
    vista); **When** se confirma **Then** aparece la notificación "Médico desactivado", la tabla se recarga y,
    con "Activos", la fila ya no está y N baja en 1; **When** se cancela o se pulsa Esc **Then** no cambia nada y
    el foco vuelve al botón "⋯" de esa fila.
20. **Reactivar desde la fila** (`[DECIDIDO S-55]`). **Given** Ríos, Pablo inactivo (filtro "Inactivos" o
    "Todos") **When** se elige "Reactivar" **Then** se abre "¿Reactivar a Ríos, Pablo?" con
    "Vuelve a ofrecerse para turnos con su agenda aprobada y, si tiene usuario del sistema, puede volver a armar propuestas de agenda."
    y el botón "Reactivar médico" (no rojo); al confirmar aparece "Médico reactivado" y la tabla se recarga.
21. **Rechazo del dominio en el diálogo.** **Given** que el caso de uso rechaza la acción (propuesta pendiente
    del AC 9, ya desactivado o reactivado por otro del AC 10, carrera del AC 12, permiso del AC 11) **When** se
    confirma **Then** el diálogo pasa a "no se puede" con el texto exacto del caso de uso y sólo "Entendido"
    (3.3 AC 39 y 85), sin notificación; **When** se pulsa "Entendido" **Then** el diálogo se cierra y la tabla se
    vuelve a cargar, para mostrar el estado real.
22. **Sin resultados.** **Given** el filtro "Inactivos" y ningún médico inactivo **When** se dibuja **Then**
    `TablaAbm` muestra "No hay médicos que coincidan con los filtros aplicados." con "Limpiar filtros"
    (3.3 AC 28, sin cambios en el componente).

### E. Ficha del médico — guía §Aplicación, Detalle de médico, punto 6

23. **Given** la ficha de un médico inactivo **When** se abre **Then** el encabezado muestra, al lado del
    nombre, la insignia "Inactivo" (gris) y la del estado de agenda (3.3 AC 65). Un médico activo no lleva
    insignia de estado.
24. **Sección final separada.** **Given** la ficha en modo lectura **Then**, **después** del botón "Editar" y
    antes del pie de trazabilidad, hay una sección separada con línea divisoria:
    - médico activo: `<h2>Desactivar médico</h2>`, el texto de la consecuencia del AC 19 y el botón
      "Desactivar médico" (secundario: el rojo queda para el diálogo, guía §Diseño visual);
    - médico inactivo: `<h2>Reactivar médico</h2>`, el texto
      "Este médico está inactivo: no se ofrece para turnos nuevos ni puede armar propuestas de agenda." y el botón
      "Reactivar médico".
    En modo edición la sección no se muestra. La dibuja `VistaDeDetalle` con un fragmento nuevo `Baja` (ver Dev
    Notes): A2 y A4 no lo usan y no cambian.
25. **Given** la ficha **When** se pulsa "Desactivar médico" o "Reactivar médico" **Then** se abre el mismo
    diálogo de los AC 19 / 20; al confirmar, la ficha queda en lectura, aparece "Médico desactivado" /
    "Médico reactivado", el encabezado y la sección final cambian al estado nuevo, y el pie de trazabilidad
    muestra el nuevo "Modificado por" (3.3 AC 40); al cancelar, el foco vuelve al botón de la sección. Un rechazo
    se comporta como el AC 21 y, al pulsar "Entendido", la ficha se vuelve a cargar.
26. **Given** un médico inactivo **When** se pulsa "Editar", se cambia un dato y se guarda **Then** aparece
    "Médico actualizado" y el médico sigue inactivo (AC 4).
27. **Corrección de agenda** (4.4; `[DECIDIDO S-53]`). **Given** un médico inactivo **When** se abre su ficha
    **Then** en el lugar del enlace "Corregir agenda" se lee "Reactivá al médico para corregir su agenda."; y
    **Given** una corrección pedida por otra vía (la pantalla abierta desde antes, un pedido armado) **When**
    llega a `ICorreccionDeAgenda.CorregirAsync` **Then** lanza `ConflictoException` con
    "No se puede corregir la agenda de Ríos, Pablo: está desactivado. Reactivalo primero.", después del permiso y
    de comprobar que el médico existe, y no guarda ni audita nada.
    **Given** un médico inactivo **When** se abre por URL la pantalla "Corregir agenda" de la 4.4
    (`/personal/admin/medicos/{id}/corregir-agenda`) **Then** muestra las migas y el título de siempre y, en
    lugar del editor y del campo de motivo, el aviso "No se puede corregir la agenda de Ríos, Pablo: está desactivado. Reactivalo primero."
    con un enlace "Ir a la ficha" a `/personal/admin/medicos/{id}`; no hay botón "Corregir agenda"
    (`[DECIDIDO S-57]`).

### F. Usuario vinculado, A4 y M2

28. **A4.** **Given** un usuario vinculado a un médico inactivo **When** se abre A4 **Then** la columna
    "Médico vinculado" dice "Ríos, Pablo (inactivo)" (como la ficha muestra "Cardiología (desactivada)", 3.2
    AC 15). El usuario sigue con su estado propio (Activo, Inactivo…): esta story no lo cambia.
29. **M2** (`[DECIDIDO S-50]`). **Given** un usuario médico vinculado a un médico inactivo **When** abre
    "Mi propuesta de agenda" **Then** ve el `<h1>` de siempre y, en lugar del contenido, el texto exacto
    "Estás desactivado como médico del hospital: no podés armar ni enviar propuestas de agenda. Si es un error, avisale al administrador."
    (sin formulario ni pantalla vacía); **When** llega un POST de guardar o enviar (pestaña vieja o pedido armado)
    **Then** `403` con ese texto en el cartel y no se guarda ni se audita nada. **Given** que se lo reactiva
    **Then** vuelve a ver su pantalla como antes, con su borrador o rechazada intactos.

### Modos de falla

30. **Given** que la base no responde al confirmar una desactivación o reactivación (error no esperable)
    **When** se confirma **Then** se ve "Algo falló. Probá de nuevo en unos minutos." (`LimiteDeErrores`, 3.3
    AC 4), sin detalle técnico, y el detalle queda en el registro técnico (NFR-09).

> Los demás modos de falla están en su AC: propuesta pendiente (9), inexistente y estado repetido (10), sin
> permiso (11), carrera (12), turnos futuros (13), matrícula de un inactivo (14), rechazo en el diálogo (21),
> sin resultados (22), corrección de un inactivo (27), M2 de un inactivo (29).

### Contrato con otras stories

31. **(Declarativo; lo fijan T13 y T15 y la Task 1 de cada story siguiente.)**
    - **5.2, 5.5, 6.4, 7.1 y 8.2** (selectores de médico y horarios libres): ofrecen **sólo médicos activos**
      (`Medico.Activo`). Hoy ninguna pantalla elige un médico de una lista (verificado: A1 lo lista, A4 elige
      usuarios, A6 lista propuestas, la 4.4 entra desde la ficha), así que no hay selector que cambiar.
    - **5.3** (reserva): cumple el AC 13 (turnos futuros) y verifica **dentro de su transacción** que el médico
      siga activo, como la 3.1 AC 17 (c) con la especialidad.
    - **8.1** (Mi agenda): un médico inactivo ve el texto del AC 29.
    - **9.1** (auditoría): lee `medico.desactivado` y `medico.reactivado` con `EntidadTipo = "Medico"`.
    - **A2** (3.1/3.3): "Médicos asociados" y la confirmación "N médicos la tienen asignada" siguen contando
      activos **e** inactivos (todos la tienen asignada).
    - **Pendiente fuera de la story (docente):** sumar a la fila de la 5.3 del plan "hereda: 3.5 AC 13 y 31".

### Guía UX/UI

32. A1 (listado y ficha) con la baja **cumple la guía UX/UI** (`docs/ux/guia-ux-ui-abm.md`, checklist "Baja",
    "Listado" y "Accesibilidad") y **usa los componentes de la arquitectura §5.1** (`FiltroDeEstado`,
    `MenuDeAcciones`, `DialogoDeConfirmacion`, `InsigniaDeEstado`, `VistaDeDetalle`, `Notificaciones`), sin
    `style="…"` ni hexadecimales; operable con teclado y en 375 px de ancho.

### No-regresión

33. - Un GET sin sesión a A1 o a la ficha sigue dando `302` al ingreso, y con otro rol `403` con "No tenés permiso
      para entrar a esta sección." (3.3 AC 89).
    - `ListarAsync` de médicos sigue devolviendo **todos** (activos e inactivos): el aviso de homónimo del alta
      (3.3 AC 69) también avisa por un inactivo. `MedicoListado` y `DatosFichaMedico` suman `bool Activo = true`
      **al final**, con valor por defecto, para no romper los `new` existentes.
    - Cambios **esperados** en pruebas existentes, y sólo éstos: `Medicos_AccionesEditarYVincularUsuario`
      (3.3) espera ahora ["Vincular usuario", "Desactivar"]; los dobles de `IGestionMedicos`
      (`GestionCaida`, `GestionSinMedicos`, `SinVinculos`) suman los dos métodos nuevos; el `INSERT INTO medicos`
      con SQL directo de `GuardarAsync_OtroPedidoGanaLaCarrera_TraduceElIndiceUnicoAConflicto`
      (`GestionMedicosEnBaseTests.cs`, 3.2) suma la columna `activo` con `true` (sin valor por defecto en la base,
      ese `INSERT` fallaría por `NOT NULL`). Las pruebas de Dominio,
      Aplicación y Web de 3.1, 3.2, 3.3, 4.1, 4.2, 4.3 y 4.4 pasan sin otro cambio.
    - La suite completa y la CI quedan en verde, con cobertura de líneas ≥ 80 % en Web y sin baja en Dominio y
      Aplicación.

### Tests (37 verificaciones)

Todas llevan `[Trait("Story", "3.5")]` y un `[Trait("AC", "n")]` por criterio cubierto.

| # | Test | AC | Nivel | Escenario | Resultado esperado |
|---|------|----|-------|-----------|--------------------|
| T1 | `Constructor_MedicoNuevo_NaceActivo` | 1 | unit | `new Medico("Pablo", "Ríos", "MP 1234", 3, null)` | `Activo = true` |
| T2 | `Desactivar_SegunEstado_DesactivaOLanzaConflicto` | 2 | unit | `[Theory]`: (a) activo; (b) ya inactivo | (a) `Activo = false`; (b) `ConflictoException` con "El médico Ríos, Pablo ya está desactivado." y sigue `false` |
| T3 | `Reactivar_SegunEstado_ReactivaOLanzaConflicto` | 3 | unit | `[Theory]`: (a) inactivo; (b) activo | (a) `Activo = true`; (b) `ConflictoException` con "El médico Ríos, Pablo ya está activo." |
| T4 | `CambiarDatos_MedicoInactivo_CambiaLosDatosYSigueInactivo` | 4 | unit | Médico desactivado; `CambiarDatos` con otro apellido | Un `CambioDeDato` (`apellido`); `Activo = false` |
| T5 | `MigracionBajaLogica_ColumnaActivoSinDefaultYSinColumnaNueva` | 5 | integration | `information_schema.columns` y `has_table_privilege` con `CadenaDuenio` | `activo` `boolean`, `is_nullable = NO`, `column_default` nulo; no hay columnas `version` ni `xmin` creadas por la migración; `turnos_app`: SELECT/INSERT/UPDATE = t, DELETE = f |
| T6 | `DesactivarAsync_Activo_LoDesactivaYAuditaSinTocarVinculoNiUsuario` | 6 | integration | Médico propio vinculado a un usuario médico activo (`MedicoConUsuarioAsync`); admin propio | `activo = false` y la fila existe; 1 registro `medico.desactivado` con `entidad_tipo = Medico`, `entidad_id`, `quien` = nombre del admin leído de la base, canal `Personal`, detalle `{"medico","matricula"}`; `usuario_personal_id` igual; el usuario sigue `Activo = true` |
| T7 | `ReactivarAsync_Inactivo_LoReactivaYAudita` | 7 | integration | Médico propio inactivo en una especialidad que después se desactiva | `activo = true`; 1 registro `medico.reactivado`; la especialidad sigue la misma |
| T8 | `DesactivarYReactivar_ConAgendas_NoCambianYLaVigenteVuelveARegir` | 8 | integration | Reloj 03/11/2026; médico propio con aprobada desde 02/11, aprobada desde 30/11 y un borrador (`DatosDeAgenda.GuardarAgendaAsync`); desactivar y reactivar | Las tres agendas con el mismo estado, marcas y franjas antes y después; `EstadosDeAgendaAsync` da `Vigente` con `CambiaEl = 30/11/2026` en los tres momentos; `AgendaQueRigeAsync(03/11)` devuelve la de 02/11 |
| T9 | `DesactivarAsync_ConPropuesta_SoloLaEnviadaBloquea` | 9 | integration | `[Theory]`: médico con una `Enviada`; con un `Borrador`; con una `Rechazada` | `Enviada`: `ConflictoException` con el texto del AC 9, sigue `activo = true`, la propuesta sigue `Enviada`, 0 registros `medico.desactivado`. `Borrador` y `Rechazada`: se desactiva y la propuesta no cambia |
| T10 | `CambiarEstadoAsync_InexistenteOEstadoRepetido_LanzaSinAuditar` | 10 | integration | `[Theory]`: desactivar y reactivar `long.MaxValue`; desactivar un inactivo; reactivar un activo | `NoEncontradoException` "No existe el médico pedido." / el `ConflictoException` del AC 2 o 3; 0 registros nuevos con su id |
| T11 | `CambiarEstadoAsync_ActorQueNoEsAdministradorActivo_LanzaAccesoDenegadoSinCambios` | 11 | integration | `[Theory]` × {desactivar, reactivar}: recepcionista propio; médico propio; admin propio con `Activo = false`; `UsuarioId` inventado | `AccesoDenegadoException` con "Sólo un administrador puede dar de alta o modificar médicos."; `activo` sin cambio; 0 registros |
| T12 | `CambiarEstadoYEditar_OtroAdministradorSeAdelanta_ConflictoSinAuditoria` | 12 | integration | `[Theory]` × {desactivar, reactivar, editar}; contenedor con `InterceptorQueSeAdelanta(CadenaApp, "UPDATE medicos SET nombre = nombre || '' WHERE id = <id>")` | `ConflictoException` con el texto del AC 12; `interceptor.SeAdelanto`; `activo` y datos como los dejó la otra conexión; 0 registros `medico.desactivado` / `medico.reactivado` / `medico.editado` de este intento. Sin la traducción, la prueba ve `DbUpdateConcurrencyException` (se comprueba rompiéndola, L-2) |
| T13 | `ModeloDeDatos_SinTurnos_ElContratoDeTurnosFuturosDelMedicoSigueVigente` | 13, 31 | unit | `ContextoDePrueba.Crear("Host=no-se-usa;Database=no-se-usa").Model` | Ninguna entidad con `ClrType.Name == "Turno"` ni `GetTableName() == "turnos"`; si aparece, falla con el mensaje exacto del AC 13 |
| T14 | `CrearAsync_MatriculaDeUnMedicoInactivo_ConflictoQueSugiereReactivar` | 14 | integration | `[Theory]`: médico propio `T-<MARCA>` inactivo; el mismo activo; alta con ` t-<marca> ` | Inactivo: "Ya hay un médico con la matrícula T-<MARCA>: Prueba<marca>, Ana, desactivado. Reactivalo desde Médicos con el filtro Inactivos."; activo: "Ya hay un médico con la matrícula T-<MARCA>."; nada guardado |
| T15 | `ListarPaginadoAsync_FiltroDeEstado_SeCombinaConLosDemasYCuentaEnLaBase` | 15, 16, 31 | integration | 4 médicos propios `Estado<marca>`: 2 activos (uno con aprobada vigente) y 2 inactivos (uno con aprobada vigente); `[Theory]` `estado` = activos / inactivos / todos / ausente, con `buscar = Estado<marca>`, y con `agenda = vigente` | activos → 2, inactivos → 2, todos y ausente → 4 (`Total` igual al conteo); con `agenda = vigente`: 1 / 1 / 2; cada `MedicoListado.Activo` correcto |
| T16 | `ListarYObtener_MedicoInactivo_TraenActivoYListarAsyncLoSigueIncluyendo` | 17, 33 | integration | Médico propio inactivo | `ObtenerAsync` → `DatosFichaMedico.Activo = false`; `ListarAsync("<marca>")` lo trae con `Activo = false` |
| T17 | `MedicosVinculadosAsync_MedicoInactivo_LoMarcaInactivo` | 28 | integration | Dos médicos propios vinculados, uno inactivo | El activo: "Prueba<marca>, Ana"; el inactivo: "Prueba<marca>, Beto (inactivo)" |
| T18 | `PropuestaAgenda_MedicoInactivo_LaVistaLoIndicaYLasEscriturasSeNiegan` | 29 | integration | `IPropuestaAgenda` con el actor de `MedicoConUsuarioAsync`; borrador propio; se desactiva el médico; `ObtenerAsync`, `GuardarBorradorAsync`, `EnviarAsync`; después se reactiva y `ObtenerAsync` | Inactivo: `VistaPropuesta.MedicoInactivo = true`; las dos escrituras `AccesoDenegadoException` con el texto del AC 29, el borrador sin cambios y 0 registros `propuesta-agenda.enviada`. Reactivado: `MedicoInactivo = false` y `EnCurso` = el borrador |
| T19 | `CorregirAsync_MedicoInactivo_LanzaConflictoSinGuardar` | 27 | integration | `ICorreccionDeAgenda` (4.4) del contenedor; médico propio inactivo; corrección válida desde hoy con motivo | `ConflictoException` con el texto del AC 27; 0 agendas nuevas del médico; 0 registros `agenda.corregida` |
| T20 | `Medicos_FiltroDeEstado_ActivosPorDefectoYEnLaUrlConLosDemas` | 15, 16, 22 | integration | bUnit de A1 con la base; médicos propios activos e inactivos de una especialidad propia; abrir A1; elegir "Inactivos", luego "Todos"; abrir una URL con todos los parámetros; "Limpiar filtros"; abrir `?estado=otro`; filtro "Inactivos" con una búsqueda sin inactivos | Orden de la barra y "Activos" elegido; sólo activos y su total; URL con `estado=inactivos` / `estado=todos` y los demás; "Estado: Inactivos" en filtros activos; la URL completa restaura todo; "Limpiar filtros" vuelve a "Activos"; con `?estado=otro`, "Activos" elegido, sólo activos y ningún filtro activo "Estado: …"; el texto de sin resultados del AC 22 |
| T21 | `Medicos_InsigniaInactivo_ColumnaSoloConTodosYEnLaTarjeta` | 17 | integration | A1 con un activo y un inactivo propios; filtros "Activos", "Inactivos", "Todos" | Columna "Estado" sólo con "Todos", con "Activo" / "Inactivo" y sus clases; la tarjeta del inactivo tiene la insignia "Inactivo" con "Inactivos" y con "Todos"; la del activo no |
| T22 | `Medicos_MenuDeLaFila_SegunEstadoDesactivarOReactivar` | 18 | integration | Fila activa y fila inactiva (filtro "Todos"); abrir cada "⋯" | Activa: ["Vincular usuario", "Desactivar"]; inactiva: ["Reactivar"]; las dos con "Editar"; ningún `.btn-danger` en la tabla |
| T23 | `Medicos_Desactivar_ConfirmaNombrandoLaConsecuenciaYSaleDeActivos` | 19 | integration | Médico propio activo; "⋯" → "Desactivar"; primero Esc; después confirmar | Título, consecuencia y botones del AC 19; foco en "Cancelar"; tras Esc el foco vuelve al "⋯" y sigue activo; tras confirmar, "Médico desactivado" en `Notificaciones.Visibles`, la fila ya no está y el total bajó 1; en la base `activo = false` |
| T24 | `Medicos_Reactivar_ConfirmaYVuelveAActivos` | 20 | integration | Médico propio inactivo; filtro "Inactivos"; "⋯" → "Reactivar"; confirmar | Título, consecuencia y botón "Reactivar médico" sin `.btn-danger`; "Médico reactivado"; ya no aparece con "Inactivos"; en la base `activo = true` |
| T25 | `Medicos_ConPropuestaPendiente_PasaANoSePuedeYRecargaAlCerrar` | 21, 9 | integration | Médico propio con una `Enviada`; "Desactivar" → confirmar; "Entendido" | El diálogo con el texto del AC 9 y sólo "Entendido"; ninguna notificación; al cerrar, la tabla se recarga (la fila sigue, activa) |
| T26 | `Ficha_SegunEstado_InsigniaYSeccionFinalDespuesDeEditar` | 23, 24 | integration | Ficha de un médico activo y de uno inactivo; después "Editar" en el activo | Activo: sin insignia "Inactivo", sección "Desactivar médico" con el texto y el botón (sin `.btn-danger`) después de "Editar" y antes de `.pie-de-trazabilidad`; inactivo: insignia "Inactivo" en `.page-header` y sección "Reactivar médico"; en edición no hay sección |
| T27 | `Ficha_DesactivarYReactivar_DesdeLaSeccionFinal` | 25 | integration | Ficha de un médico propio activo; "Desactivar médico" → Cancelar; → confirmar; "Reactivar médico" → confirmar | Al cancelar, el foco vuelve al botón de la sección; "Médico desactivado", insignia "Inactivo" y sección "Reactivar médico", pie con "· Modificado por Lucía Pérez"; luego "Médico reactivado" y vuelve la sección "Desactivar médico" |
| T28 | `Ficha_MedicoInactivo_SeEditaSinReactivarYSinCorregirAgenda` | 26, 27, 4 | integration | Ficha de un médico propio inactivo; "Editar", otro apellido, "Guardar" | "Médico actualizado"; sigue la insignia "Inactivo"; "Reactivá al médico para corregir su agenda." y ningún enlace "Corregir agenda" |
| T29 | `VistaDeDetalle_ConBaja_LaDibujaDespuesDeEditarYAntesDelPie` | 24 | unit | bUnit de `VistaDeDetalle` con `Baja` y `Pie`, y sin `Baja` | Con: orden contenido → "Editar" → `.vista-de-detalle__baja` → pie; sin: no hay `.vista-de-detalle__baja` (A2 y A4 igual que hoy) |
| T30 | `Usuarios_ColumnaMedicoVinculado_MarcaAlMedicoInactivo` | 28 | integration | bUnit de A4 con la base; usuario médico propio vinculado a un médico propio inactivo | La fila dice "Prueba<marca>, Ana (inactivo)" en "Médico vinculado" y el badge del usuario sigue "Activo" |
| T31 | `GetMiPropuesta_MedicoInactivo_MuestraElAvisoYNiegaGuardar` | 29 | integration | `MedicoIngresadoAsync` (4.2); se desactiva su médico; GET de M2; POST `guardar` con `FormularioHttp.EnviarAsync` | GET `200` con `<h1>Mi propuesta de agenda</h1>`, el texto del AC 29 y ningún `<form` del editor; POST `403` con el texto en `role="alert"`; 0 agendas nuevas |
| T32 | `ConfirmarBaja_ErrorInesperado_MuestraElMensajeGenericoSinDetalle` | 30 | integration | bUnit de A1 con un doble de `IGestionMedicos` cuyo `DesactivarAsync` lanza `InvalidOperationException("detalle interno")` | "Algo falló. Probá de nuevo en unos minutos."; el texto "detalle interno" no aparece; el registro en memoria tiene la excepción |
| T33 | `Pantalla_CumpleChecklistDeLaGuiaUx_YUsaLosComponentesComunes` | 32 | integration | Matriz en vivo en Chrome contra el Compose: checklist "Baja", "Listado" y "Accesibilidad" sobre A1 y la ficha, con teclado y a 375 px; revisión de que no hay `style=` ni hexadecimales nuevos | Todos los puntos **Debe** cumplidos; T8 de la 3.3 (`NingunComponente_UsaStyleEnLinea_NiHexadecimal`) en verde |
| R1 | Base de demo migrada | 5 | integration | Después de `docker compose up -d --build`: `select count(*) from medicos where not activo;` y `\d medicos` | 0 filas inactivas (todos los médicos existentes quedaron activos); `activo` sin default; sin columna `xmin` ni `version` listada |
| R2 | Pruebas existentes con sólo los cambios esperados | 33 | integration | `dotnet test` de los tres proyectos y `git diff` de `tests/` | 0 fallas; las únicas pruebas existentes modificadas son las del AC 33 |
| T34 | `CorregirAgenda_MedicoInactivoPorUrl_MuestraElAvisoSinEditor` | 27 | integration | bUnit de la pantalla "Corregir agenda" de la 4.4 con la base; médico propio inactivo; abrir `/personal/admin/medicos/{id}/corregir-agenda` | El aviso del AC 27; un enlace "Ir a la ficha" a `/personal/admin/medicos/{id}`; ningún campo del editor ni "Motivo de la corrección", ningún botón "Corregir agenda"; 0 agendas nuevas del médico |
| R3 | CI y cobertura | 33 | integration | Workflow `CI` (`.github/workflows/ci.yml`) sobre el PR de esta story | Verde; líneas ≥ 80 % en Web |

> 37 verificaciones: T1–T34 más R1–R3 (T34 la sumó la decisión S-57). Los casos de cada `[Theory]` se suman al cerrar. No hay E2E con
> Playwright (el proyecto `HospitalTurnos.E2E` llega con la 10.1); en su lugar, la **matriz en vivo** del Compose
> (T33) recorre un paso por AC de las secciones D, E y F y el AC 30 (con el contenedor `db` detenido).

#### Given/Fixture

- **Dominio.Tests (T1–T4):** `new Medico(...)` sin base, en `MedicoTests` (3.2).
- **Aplicacion.Tests (T5–T19):** `[Collection("Postgres")]`, `ServiceProvider` como `GestionMedicosEnBaseTests`
  (3.2) y `DatosDeAgenda.Proveedor` (4.1), un alcance por operación, `IGestionMedicos` **resuelto del
  contenedor** (nunca con `new`). Médicos con `DatosDeCatalogo.CrearMedicoAsync` y la marca propia; un inactivo
  se arma con `DesactivarAsync` de esta story. Admin propio con `UsuariosDePrueba.CrearAsync` + `ActorDe`.
  Médico con usuario con `DatosDePropuesta.MedicoConUsuarioAsync` (4.2). Agendas con
  `DatosDeAgenda.GuardarAgendaAsync` y `Reloj(...)`. Auditoría filtrada por `entidad_tipo = 'Medico' AND entidad_id = '<id>'`.
- **T12:** `InterceptorQueSeAdelanta` de la 3.1 con un `UPDATE` (como la T13 de la 4.3): cambia `xmin` por otra
  conexión justo antes del guardado. Se agrega con `ConfigureDbContext<HospitalDbContext>(o => o.AddInterceptors(interceptor))`.
- **T13:** idéntica en forma a `ContratoTurnosFuturosTests` (3.1), en su propio archivo, sin `[Collection]`.
- **T19:** usa el `ICorreccionDeAgenda` que crea la 4.4 (Task 1 lo confirma).
- **Web.Tests (T20–T28, T30, T32):** bUnit interactivo como `MedicosInteractivaTests` (3.3): `this.ConLaApp(app)`,
  `this.ComoAdmin1Async(app)`, `AbrirAsync<T>`, `PulsarAsync`, `CambiarAsync`; `PersonalWebApplicationFactory`
  con `[Collection("Postgres")]`. Médicos con `app.CrearMedicoAsync` y se desactivan resolviendo `IGestionMedicos`
  de `app.Services` con `app.ActorDeAdmin1Async()`. La propuesta `Enviada` de T25 con
  `DatosDePropuesta.GuardarAgendaAsync(app, agenda)` (4.2). T32 registra el doble como en
  `Especialidades_RechazoDelDominio_PasaANoSePuede` (3.3) y `RegistroEnMemoria`.
- **T29:** bUnit sin base, junto a las demás de `ComponentesComunesTests`.
- **T31:** HTTP como `MiPropuestaHttpTests` (4.2): M2 sigue siendo SSR.

## Tasks / Subtasks

- [ ] [SEQ] Task 1: Salvaguarda de estado (AC: todos) — revalidar contra `main` el día que se implementa
  - [ ] Confirmar que la 4.3 y la 4.4 están en `main` (depends_on) y anotar en el Debug Log, con su firma real:
        `ICorreccionDeAgenda.CorregirAsync` y `CorreccionDeAgendaEnBase` (4.4), dónde quedó el enlace
        "Corregir agenda" en `FichaMedico.razor`, `VistaPropuesta` con `RechazadaPor` al final (4.3). Si la 4.3 o
        la 4.4 cambiaron al pasar a interactivas, ajustar esta story antes de programar.
  - [ ] Sumar a `files_to_modify` `src/HospitalTurnos.Infraestructura/Agendas/CorreccionDeAgendaEnBase.cs` y
        `src/HospitalTurnos.Web/Components/Personal/Administracion/CorregirAgenda.razor` (los crea la 4.4: hoy no
        existen y el control del frontmatter rechaza rutas que no existen), y el archivo de pruebas de esa pantalla
        donde va T34.
  - [ ] La ficha real es interactiva y consulta con `AlcancePorOperacion` (3.3 AC 94); la 4.4 se escribió para una
        ficha SSR con `?resultado=`. Si la 4.4 llega así, la 3.5 igual consulta con `Operaciones.EjecutarAsync`
        (nunca inyecta el caso de uso en la página) y anota en el Debug Log dónde quedó el texto del AC 27 en la
        ficha y en "Corregir agenda".
  - [ ] Confirmar que `medicos` no tiene `activo` ni `xmin` mapeado, que los dobles de `IGestionMedicos` son
        los tres del AC 33 y que ninguna pantalla elige un médico de una lista (`grep -rn "IGestionMedicos" src/HospitalTurnos.Web`).
  - [ ] Salvaguarda de BD (abajo). Si algo no coincide → DETENERSE y corregir la story.
- [ ] [SEQ] Task 2: Dominio y auditoría (AC 1–4): `Activo`, `Desactivar`, `Reactivar` en `Medico`; constantes en
      `AccionesAuditoria`. T1–T4.
- [ ] [SEQ] Task 3: Persistencia (AC 5): `MedicoConfiguracion` (`xmin`), migración con el ajuste a mano del
      `defaultValue` y el `DROP DEFAULT`, snapshot. T5, R1.
- [ ] [SEQ] Task 4: Casos de uso (AC 6–14, 17, 28): `IGestionMedicos` + `GestionMedicosEnBase`, traducción de
      concurrencia también en `EditarAsync`, filtro de estado en `ListarPaginadoAsync`, `Activo` en los registros,
      "(inactivo)" en `MedicosVinculadosAsync`, matrícula de un inactivo. T6–T17.
- [ ] [PAR] Task 5: Agendas (AC 27, 29): `VistaPropuesta.MedicoInactivo` y el control en
      `PropuestaAgendaEnBase`; el control en `CorreccionDeAgendaEnBase` y el aviso en la pantalla "Corregir
      agenda" (S-57). T18, T19, T34.
- [ ] [SEQ] Task 6: Componente común (AC 24): fragmento `Baja` en `VistaDeDetalle` y la clase en `app.css`. T29.
- [ ] [SEQ] Task 7: Pantallas (AC 15–30): `Medicos.razor`, `TablaMedicos.razor`, `FichaMedico.razor`,
      `MiPropuesta.razor`; los cambios esperados en pruebas existentes (incluido el `INSERT` de
      `GestionMedicosEnBaseTests.cs`, que se ajusta junto con la migración de la Task 3). T20–T28, T30–T32.
- [ ] [SEQ] Task 8: Documentos (AC 31): contrato técnico y arquitectura §6.
- [ ] [SEQ] Task 9: Suite, matriz en vivo y CI (AC 32, 33). T33, R2, R3.

### Task Dependencies

T1 → T2 → T3 → T4 → (T5 en paralelo con T6) → T7 → T8 → T9. Ninguna pantalla se toca antes de que sus casos de
uso tengan prueba en verde.

## Dev Notes

### Que existe hoy

#### En hospital-turnos (verificado 2026-10-23 contra la rama `cierre/sesion-17`, `4df6e51`, con 3.3 y 4.2 en done; 4.3 y 4.4 en ready-for-dev)

- **`src/HospitalTurnos.Dominio/Catalogo/Medico.cs`** — sin `Activo`; el comentario de la clase dice "No se borra
  ni se desactiva" (cambiarlo). `CambiarDatos` (~37-55), `NombreCompleto => $"{Apellido}, {Nombre}"` (~34).
- **`src/HospitalTurnos.Dominio/Catalogo/Especialidad.cs:55-65`** — `Desactivar()` / `Reactivar()` con
  `ConflictoException` del estado repetido: el modelo a copiar.
- **`src/HospitalTurnos.Dominio/Auditoria/AccionesAuditoria.cs`** — termina en el bloque "Story 3.2" con
  `MedicoCreado` y `MedicoEditado`; no hay `MedicoDesactivado` ni `MedicoReactivado`.
- **`src/HospitalTurnos.Aplicacion/Catalogo/IGestionMedicos.cs`** — `ListarAsync`, `ListarPaginadoAsync`,
  `ObtenerAsync`, `UsuariosParaVincularAsync`, `MedicosVinculadosAsync`, `CrearAsync`, `EditarAsync`;
  `MedicoListado(..., SituacionDeAgenda Agenda)` y `DatosFichaMedico(..., SituacionDeAgenda Agenda)`.
  **No existen** `DesactivarAsync` / `ReactivarAsync`.
- **`src/HospitalTurnos.Infraestructura/Catalogo/GestionMedicosEnBase.cs`** — `ListarPaginadoAsync` (~66-90)
  **ignora** el filtro `estado` que ya le manda la pantalla (`EstadoDeListado.AConsulta()` siempre lo pone);
  `ObtenerAsync` (~119-130); `MedicosVinculadosAsync` (~147-150); `ExigirMatriculaLibreAsync` (~221-225);
  `EditarAsync` (~180-213); `GuardarTraduciendoCarrerasAsync` (~249-270) traduce sólo los `23505`.
  `SinPermiso` y `NoExiste` son constantes de la clase.
- **`src/HospitalTurnos.Infraestructura/Catalogo/CatalogoEspecialidadesEnBase.cs:119-135`** —
  `DesactivarAsync`/`ReactivarAsync` → `CambiarEstadoAsync`: permiso, carga con seguimiento (404), regla del
  dominio, auditoría, un guardado. Filtro de estado del listado en `ListarPaginadoAsync` (~49-54):
  `"activos"`, `"inactivos"` y cualquier otro valor = todos.
- **`MedicoConfiguracion.cs`** — sin `xmin`. **Ninguna** tabla del catálogo tiene concurrencia optimista hoy
  (3.2, decisión M-6: "gana el último"); la tienen sólo `agendas_medico`
  (`AgendaMedicoConfiguracion.cs:22`) y lo que la usa (`PropuestaAgendaEnBase.cs`, el `catch` de `GuardarTraduciendoAsync`, hoy 181-184, traduce
  `DbUpdateConcurrencyException`).
- **`src/HospitalTurnos.Infraestructura/Agendas/PropuestaAgendaEnBase.cs`** — `MedicoDeAsync` (~105-111) busca el
  médico por `UsuarioPersonalId` sin mirar si está activo; `MedicoDeLaSesionAsync` (~99-103) lo usa en las
  escrituras; `ObtenerAsync` (~39-58) arma `VistaPropuesta(SinVinculo, …)`.
- **Web** — `Medicos.razor` (barra de filtros ~13-30, `Reglas` ~42-43, `FiltrosActivos` ~56-58); `TablaMedicos.razor`
  (`Columnas` estáticas ~49-55, `OpcionesDe` ~59-63 con sólo "Vincular usuario" y el comentario "Desactivar no
  existe", `Tarjeta` ~15-18); `FichaMedico.razor` (`Encabezado` ~39, `VistaDeDetalle` ~43-61);
  `Especialidades.razor` es el modelo de la baja: `Columnas` con `Visible: estado.Estado == "todos"` (~77-84),
  `OpcionesDe` (~131-133), `ConfirmarDesactivarAsync` (~135-151), `ConfirmarReactivar` (~153-160),
  `TerminarAsync` (~162-168), `AlElegirDesde="r => origen = r"` (~30).
- **Componentes** — `FiltroDeEstado.razor` (Activos/Inactivos/Todos, `Femenino` para A2); `EstadoDeListado.cs`
  (`EstadoPorDefecto = "activos"`, ida y vuelta con la URL, valor desconocido → defecto); `DialogoDeConfirmacion.razor`
  (`ConfirmarAsync` ~60-78: atrapa sólo `EsEsperable` y pasa a "no se puede"; "Entendido" llama a `AlCancelar`);
  `VistaDeDetalle.razor` (contenido → botón "Editar" → `Pie`; **no** tiene lugar para una sección después de
  "Editar"); `InsigniaDeEstado` (tonos `Activo`, `Inactivo`…); `TablaAbm` (sin resultados con filtros y sin
  búsqueda: "No hay {Entidad} que coincidan con los filtros aplicados.", ~22).
- **A4** — `UsuariosPersonal.razor` `MedicoVinculadoDe` (~189-192) muestra lo que devuelve `MedicosVinculadosAsync`:
  no hay que tocar la página.
- **Elegir un médico** — ninguna pantalla lo hace hoy: A1 lista, A4 elige usuarios, M2 usa el médico de la
  sesión, la 4.3 lista propuestas y la 4.4 entra desde la ficha.
- **Dobles que implementan `IGestionMedicos`** — `GestionCaida` y `GestionSinMedicos`
  (`MedicosInteractivaTests.cs` ~142 y ~178) y `SinVinculos` (`UsuariosInteractivaTests.cs` ~267).
- **4.4 (ready-for-dev)** crea `CorreccionDeAgendaEnBase.cs` y agrega a la ficha "Agenda vigente" y el enlace
  "Corregir agenda" (su AC 17). Esta story **modifica** esos dos lugares después de la 4.4 (Task 1).
- **No existe:** la columna `activo` en `medicos`, `xmin` en `medicos`, la baja de médicos en ninguna capa, el
  fragmento `Baja` de `VistaDeDetalle`.

### Patron de referencia verificado

| Proyecto | Ancla | Qué se verificó y qué se toma |
|---|---|---|
| hospital-turnos (3.1) | `src/HospitalTurnos.Dominio/Catalogo/Especialidad.cs:55-65` | `Desactivar`/`Reactivar` lanzan `ConflictoException` con el nombre si el estado ya es ése. `Medico` igual, con `NombreCompleto`. |
| hospital-turnos (3.1) | `src/HospitalTurnos.Infraestructura/Catalogo/CatalogoEspecialidadesEnBase.cs:119-135` | `CambiarEstadoAsync`: `ExigirAdministradorActivoAsync` → `FirstOrDefaultAsync` con seguimiento o `NoEncontradoException` → regla del dominio → `auditoria.Registrar` → un `SaveChangesAsync`. Se copia la forma, más el control de la propuesta y la traducción de `xmin`. |
| hospital-turnos (3.3) | `src/HospitalTurnos.Web/Components/Personal/Administracion/Especialidades.razor:77-84,131-168` | Columna Estado sólo con "todos"; menú con "Desactivar" o "Reactivar"; `Confirmacion(Titulo, Consecuencia, Verbo, Peligro, Accion)`; `ActorDelCircuito.DesdeAsync`; `Notificaciones.Mostrar` y `tabla.RecargarAsync()`. A1 hace lo mismo. |
| hospital-turnos (4.1/4.2) | `src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/AgendaMedicoConfiguracion.cs:22` y `src/HospitalTurnos.Infraestructura/Agendas/PropuestaAgendaEnBase.cs` (el `catch` de `GuardarTraduciendoAsync`, hoy 181-184) | `Property<uint>("Version").IsRowVersion().HasColumnName("xmin")` (sin el nombre, la convención la renombra a `version` y crea una columna) y `catch (DbUpdateConcurrencyException) → ConflictoException`. |
| hospital-turnos (3.1) | `tests/HospitalTurnos.Aplicacion.Tests/Catalogo/ContratoTurnosFuturosTests.cs` | Trampa sin base sobre `contexto.Model.GetEntityTypes()`. T13 es la misma forma con su mensaje. |
| hospital-turnos (3.1/4.3) | `tests/HospitalTurnos.Aplicacion.Tests/Ayudas/InterceptorQueSeAdelanta.cs:12-16` | `(cadena, sql)`, ejecuta el SQL una vez por otra conexión antes del guardado, `SeAdelanto`. Con un `UPDATE` prueba `xmin` (T12). |
| hospital-turnos (3.3) | `tests/HospitalTurnos.Web.Tests/Catalogo/EspecialidadesInteractivaTests.cs` (`Especialidades_RechazoDelDominio_PasaANoSePuede`) | Doble que rechaza registrado en bUnit con `AlcancePorOperacion`; se toma para T32. |

**Migración que agrega un `boolean NOT NULL` a una tabla con filas:** no hay precedente en el repo (las columnas
`activo` de `asp_net_users` y `especialidades` nacieron con su tabla). Se ajusta a mano lo que genera
`dotnet ef`: `AddColumn<bool>(... defaultValue: true)` para llenar las filas existentes y, a continuación,
`migrationBuilder.Sql("ALTER TABLE medicos ALTER COLUMN activo DROP DEFAULT;")` para que la base quede como el
modelo (sin `HasDefaultValue`). El `Down` hace `DropColumn`.

### Que hay que hacer (la delta)

1. Dominio: `Activo`, `Desactivar`, `Reactivar` en `Medico`; dos constantes de auditoría.
2. Persistencia: columna `activo` y `xmin` en `medicos` (una migración).
3. Casos de uso: `DesactivarAsync` / `ReactivarAsync`; el filtro `estado` del listado; `Activo` en los registros;
   la traducción de concurrencia también al editar; "(inactivo)" en A4; la matrícula de un inactivo.
4. Agendas: M2 y la corrección de urgencia se niegan para un médico inactivo.
5. Web: filtro, columna, menú y diálogos en A1; insignia y sección final en la ficha (con el fragmento nuevo de
   `VistaDeDetalle`); aviso en M2.
6. El contrato con 5.2/5.3/5.5/6.4/7.1/8.1/8.2/9.1 y su trampa.

### Implementacion concreta

**`Medico`**: `public bool Activo { get; private set; }`; en el constructor público `Activo = true;` (no en la
base). `Desactivar()` / `Reactivar()` como `Especialidad` con `$"El médico {NombreCompleto} ya está desactivado."`
/ `"... ya está activo."`. `CambiarDatos` no toca `Activo`.

**`AccionesAuditoria`**, en el bloque de la 3.2:

```csharp
// Story 3.5: la baja lógica de médicos (A1, canal Personal).
public const string MedicoDesactivado = "medico.desactivado";
public const string MedicoReactivado = "medico.reactivado";
```

**`MedicoConfiguracion`**: `medico.Property<uint>("Version").IsRowVersion().HasColumnName("xmin");` con el
comentario de por qué el nombre va explícito; `Activo` sin `HasDefaultValue`.

**`IGestionMedicos`** — dos métodos y un dato al final de cada registro:

```csharp
/// <summary>Baja lógica (story 3.5): sin turnos futuros (5.3) ni propuesta enviada pendiente.</summary>
Task DesactivarAsync(long id, ActorAuditoria actor, CancellationToken ct);
Task ReactivarAsync(long id, ActorAuditoria actor, CancellationToken ct);

public sealed record MedicoListado(long Id, string Apellido, string Nombre, string Matricula, string Especialidad,
    SituacionDeAgenda Agenda, bool Activo = true);
public sealed record DatosFichaMedico(long Id, DatosMedico Datos, string Especialidad, bool EspecialidadActiva,
    SituacionDeAgenda Agenda, bool Activo = true);
```

**`GestionMedicosEnBase`**:

```csharp
public Task DesactivarAsync(long id, ActorAuditoria actor, CancellationToken ct) =>
    CambiarEstadoAsync(id, actor, m => m.Desactivar(), AccionesAuditoria.MedicoDesactivado, exigirSinPropuesta: true, ct);

private async Task CambiarEstadoAsync(long id, ActorAuditoria actor, Action<Medico> cambio, string accion,
    bool exigirSinPropuesta, CancellationToken ct)
{
    actor = await actorVigente.ExigirAdministradorActivoAsync(actor, SinPermiso, ct);    // AC 11, antes de leer
    var medico = await contexto.Medicos.FirstOrDefaultAsync(m => m.Id == id, ct)
        ?? throw new NoEncontradoException(NoExiste);                                     // AC 10
    cambio(medico);                                                                       // AC 2 y 3, en el dominio
    if (exigirSinPropuesta && await contexto.AgendasMedico.AnyAsync(
            a => a.MedicoId == id && a.Estado == EstadoPropuesta.Enviada, ct))
        throw new ConflictoException($"No se puede desactivar a {medico.NombreCompleto}: tiene una propuesta ..."); // AC 9
    // Hoy no hay turnos: no se consultan (AC 13). La 5.3 agrega acá el conteo de turnos futuros.
    auditoria.Registrar(actor, accion, EntidadTipo, Texto(id), new { medico = medico.NombreCompleto, matricula = medico.Matricula });
    await GuardarTraduciendoCarrerasAsync(medico, ct);                                    // un solo guardado
}
```

- `GuardarTraduciendoCarrerasAsync` suma, **primero**, `catch (DbUpdateConcurrencyException) => throw new
  ConflictoException(CambioMientrasTanto)` (AC 12): así lo usan también `EditarAsync` y `CrearAsync` (en el alta
  no puede saltar: la fila es nueva).
- `ListarPaginadoAsync`: `consulta.Filtro("estado") switch { "activos" => Where(m => m.Activo), "inactivos" =>
  Where(m => !m.Activo), _ => todos }` (como A2), antes del conteo; proyectar `m.Activo` a `MedicoListado`.
- `ObtenerAsync` y `ListarAsync`: proyectar `Activo`. `ListarAsync` **no** filtra (AC 33).
- `MedicosVinculadosAsync`: `m.Activo ? "Apellido, Nombre" : "Apellido, Nombre (inactivo)"`.
- `ExigirMatriculaLibreAsync`: si la repetida está inactiva, el texto del AC 14 con su `NombreCompleto`.

**Agendas.** `VistaPropuesta` suma `bool MedicoInactivo = false` **al final** (después del `RechazadaPor` de la
4.3). `PropuestaAgendaEnBase`: `MedicoDeAsync` devuelve también `Activo`; `ObtenerAsync` con un médico inactivo
devuelve la vista con `MedicoInactivo = true`; `MedicoDeLaSesionAsync` lanza `AccesoDenegadoException` con el
texto del AC 29 (constante `MedicoDesactivado`). `MiPropuesta.razor` muestra ese texto como hoy muestra el de
`SinVinculo`. `CorreccionDeAgendaEnBase.CorregirAsync` (4.4): después de "médico existe" y antes del motivo,
si `!medico.Activo` → `ConflictoException` del AC 27. Para la pantalla "Corregir agenda" (S-57), los datos que
carga la 4.4 (`DatosParaCorregir`, su AC 8) suman `bool MedicoActivo = true` **al final**; con `false`, la página
dibuja el aviso del AC 27 y el enlace "Ir a la ficha" en lugar del editor (consulta con
`Operaciones.EjecutarAsync`, como la ficha).

**`VistaDeDetalle` — extensión (AC 24).** Nuevo `[Parameter] public RenderFragment? Baja { get; set; }`, dibujado
después de `.formulario-abm__botones` y antes de `@Pie`, dentro de `<div class="vista-de-detalle__baja">`, sólo si
no es `null`. En `app.css`, con tokens: `.vista-de-detalle__baja { margin-top: var(--esp-4); padding-top:
var(--esp-3); border-top: 1px solid var(--color-separador); }`. A2 y A4 no lo pasan: no cambian (T29).

**A1.**
- `Medicos.razor`: `<FiltroDeEstado Valor="@estado.Estado" ValorCambiado="CambiarEstado" />` como último filtro
  de la barra; `CambiarEstado(string valor) => Navegar(estado with { Estado = valor, Pagina = 1 })`.
  `FiltrosActivos` ya escribe "Estado: …".
- `TablaMedicos.razor`: `Columnas` deja de ser estático (la columna "Estado" con `Visible: Estado.Estado ==
  "todos"`); `OpcionesDe` según `medico.Activo`; la `Tarjeta` suma la insignia "Inactivo" si `!medico.Activo`.
  Recibe del padre `[Parameter] EventCallback<MedicoListado> AlDesactivar`, `AlReactivar` y
  `EventCallback<ElementReference> AlElegirDesde`: la confirmación y la recarga viven en `Medicos.razor`, como en
  `Especialidades.razor` (la tabla no llama casos de uso).
- `Medicos.razor`: `Confirmacion` y `DialogoDeConfirmacion` como A2; la acción llama a
  `Operaciones.EjecutarAsync<IGestionMedicos>(g => g.DesactivarAsync(...))`. Para el AC 21, la acción marca
  `recargarAlCerrar = true` en un `catch { ...; throw; }` (el diálogo necesita la excepción para pasar a "no se
  puede") y `AlCancelar` recarga la tabla si la marca está puesta. `TablaMedicos` expone `RecargarAsync()` que
  llama a la de su `TablaAbm`.
- Textos en constantes compartidas por la tabla y la ficha (`TextosDeBaja` o constantes públicas en
  `TablaMedicos`), para no repetir la consecuencia.

**Ficha.** `Encabezado`: insignia "Inactivo" antes del badge de agenda si `!ficha.Activo`. `VistaDeDetalle` con
`Baja` según `ficha.Activo`; el botón de la sección (`@ref`) es el `Origen` del diálogo. Tras confirmar:
`Avisos.Mostrar(...)` y `CargarAsync()` (actualiza encabezado, sección y pie). El enlace "Corregir agenda" de la
4.4 sólo con `ficha.Activo`; si no, el texto del AC 27.

### Decisiones de esta story (con su porqué)

- **Baja lógica, nunca borrar** (guía: entidad referenciada por agendas, auditoría y, desde la 5.3, turnos;
  `turnos_app` ya no tiene `DELETE`).
- **"Desactivar" en el menú de la fila y en la sección final de la ficha**: la guía §Aplicación lo pide en los
  dos lugares (listado: "⋯ con … Desactivar"; detalle: punto 6).
- **"Reactivar" sólo visible con el filtro** "Inactivos" o "Todos" (guía: "visibles con el filtro de estado").
- **"Ríos, Pablo" en los textos**: es lo que hacen la guía en sus ejemplos y el repo en sus mensajes (3.2, 3.3).
- **El texto de la matrícula de un inactivo sugiere reactivar**: mismo motivo que la 3.1 AC 6 con una
  especialidad desactivada; sin eso la matrícula queda bloqueada sin pista.
- **"(inactivo)" en la columna de A4**: misma forma que "(desactivada)" de la especialidad en la ficha (3.2 AC 15).
- **Editar un inactivo se permite**: corregir un dato no cambia el estado (3.2 AC 17: lo que no cambia no se revalida).
- **Al reactivar no se revalida la especialidad**: la conserva aunque esté desactivada, como la 3.2 AC 17.
- **La carrera "el médico envía su propuesta justo cuando lo desactivan" se acepta**: el envío no toca la fila del
  médico y cerrarla obligaría a bloquear filas en la 4.2. Si pasa, la propuesta aparece en A6 y el administrador
  la revisa; M2 ya no le deja enviar otra. Caso de segundos en un plantel de decenas.
- **Desactivar no consulta turnos hoy** (no existen), con trampa y contrato, igual que la 3.1 (decisión 5): no es
  un stub.

### Archivos a crear/modificar

| Accion | Archivo |
|--------|---------|
| Crear | La migración `<marca>_AgregarBajaLogicaDeMedicos` (+ `.Designer.cs`) |
| Crear | `tests/HospitalTurnos.Aplicacion.Tests/Catalogo/BajaLogicaDeMedicosTests.cs` (T5–T12, T14–T19) |
| Crear | `tests/HospitalTurnos.Aplicacion.Tests/Catalogo/ContratoTurnosFuturosDelMedicoTests.cs` (T13) |
| Crear | `tests/HospitalTurnos.Web.Tests/Catalogo/BajaDeMedicosInteractivaTests.cs` (T20–T28, T30–T32; T34 si la 4.4 no deja un archivo de pruebas de su pantalla) |
| Modificar | `Medico.cs` (`Activo`, `Desactivar`, `Reactivar`, comentario), `AccionesAuditoria.cs` (2 constantes) |
| Modificar | `IGestionMedicos.cs` (2 métodos, `Activo` en 2 registros), `GestionMedicosEnBase.cs` (ver Implementación) |
| Modificar | `MedicoConfiguracion.cs` (`xmin`), `HospitalDbContextModelSnapshot.cs` (lo regenera `dotnet ef`) |
| Modificar | `IPropuestaAgenda.cs` (`MedicoInactivo`), `PropuestaAgendaEnBase.cs`, `CorreccionDeAgendaEnBase.cs` y `CorregirAgenda.razor` (los crea la 4.4: entran al frontmatter en la Task 1) |
| Modificar | `VistaDeDetalle.razor` (fragmento `Baja`), `app.css` (`.vista-de-detalle__baja`) |
| Modificar | `Medicos.razor`, `TablaMedicos.razor`, `FichaMedico.razor`, `MiPropuesta.razor` |
| Modificar | `MedicoTests.cs` (T1–T4), `ComponentesComunesTests.cs` (T29), `GestionMedicosEnBaseTests.cs` (el `INSERT` suma `activo`, AC 33), `MedicosInteractivaTests.cs` y `UsuariosInteractivaTests.cs` (cambios esperados del AC 33) |
| Modificar | `docs/stories/00-contrato-tecnico.md` (§Auditoría: `medico.desactivado` / `medico.reactivado`), `docs/04-arquitectura.md` (§6: `Medico` con `Activo`) |

### Lo que NO hay que hacer

- **NO borrar médicos** ni dar `DELETE` a `turnos_app`.
- **NO tocar el usuario vinculado** al desactivar (ni desactivarlo ni desvincularlo): es de A4 (2.2) y del S-50.
- **NO cambiar ni rechazar agendas** al desactivar o reactivar (ADR-06; S-51 y S-52).
- **NO crear un contador de turnos que devuelva 0** ni la tabla de turnos: es de la 5.3 (decisión 5 de la 3.1).
- **NO filtrar `ListarAsync`** por activo: el aviso de homónimo y las stories viejas lo usan con todos.
- **NO cambiar el conteo de "Médicos asociados" de A2**: un inactivo sigue teniendo la especialidad asignada.
- **NO crear otro diálogo, otro filtro ni otra insignia**: `DialogoDeConfirmacion`, `FiltroDeEstado`,
  `InsigniaDeEstado` sirven tal cual; la única extensión es el fragmento `Baja` de `VistaDeDetalle`.
- **NO poner el botón de la sección final en rojo**: el rojo es sólo el del diálogo (guía §Diseño visual).
- **NO usar `HasDefaultValue(true)`** en `Activo` ni dejar el default en la base (Implementación).
- **NO llamar casos de uso desde `TablaMedicos`** ni inyectarlos directo en las páginas: `AlcancePorOperacion`
  (3.3 AC 94).
- **NO usar `DateTime.Now`** (contrato §Hora) ni esperar tiempo real en las pruebas (el toast se prueba con el
  `FakeTimeProvider` de la 3.3).

### Como verificar que funciona

```bash
cd ~/SourceCode/hospital-turnos

# 1. Migración (proyecto = Infraestructura, arranque = Web). Ajustar a mano defaultValue y DROP DEFAULT.
dotnet ef migrations add AgregarBajaLogicaDeMedicos \
  --project src/HospitalTurnos.Infraestructura --startup-project src/HospitalTurnos.Web \
  --output-dir Persistencia/Migraciones
dotnet ef migrations script --idempotent \
  --project src/HospitalTurnos.Infraestructura --startup-project src/HospitalTurnos.Web | grep -n -i "xmin\|activo"
# Esperado: ADD activo ... DEFAULT TRUE y DROP DEFAULT; NINGÚN "ADD xmin" (es columna de sistema)

# 2. Pruebas (Docker para Testcontainers), por proyecto
dotnet test tests/HospitalTurnos.Dominio.Tests
dotnet test tests/HospitalTurnos.Aplicacion.Tests
dotnet test tests/HospitalTurnos.Web.Tests
# Esperado: 0 fallas; dotnet test --filter "Story=3.5" lista T1–T34 (más los casos de cada [Theory])

# 3. Compose y matriz en vivo (T33): como administrador, con teclado y a 375 px
docker compose up -d --build
# A1: filtro Estado, Desactivar/Reactivar desde "⋯", columna Estado con "Todos", URL y Atrás;
# ficha: insignia, sección final, editar un inactivo; A4: "(inactivo)"; M2 con el médico desactivado;
# AC 30: docker compose stop db, confirmar una baja, ver el mensaje genérico; docker compose start db

# 4. R1 y auditoría
docker compose exec db psql -U postgres -d hospital_turnos -c "select count(*) from medicos where not activo;"
docker compose exec db psql -U postgres -d hospital_turnos -c \
  "select cuando, quien, accion, entidad_id, detalle from registros_auditoria where accion like 'medico.%activado' order by id desc limit 5;"
```

### Salvaguarda de BD

1. `\d medicos` antes de migrar: no hay `activo`; anotar las columnas reales.
2. `select count(*) from medicos;` en la base de demo: si hay filas, R1 debe dar 0 inactivos después.
3. `select has_table_privilege('turnos_app','medicos','UPDATE');` → `t` (desactivar es un `UPDATE`); `DELETE` → `f`.
4. Después de migrar: `\d medicos` muestra `activo boolean not null` **sin** `default`, y ninguna columna `xmin`
   ni `version` agregada. Si el script del paso 1 de "Como verificar" trae un `ADD xmin` → DETENERSE.
5. Si cualquier resultado no coincide → DETENERSE → corregir la story primero.

### References

- [Source: docs/ux/guia-ux-ui-abm.md] — "Barra de filtros", "Tabla", "Baja, desactivación y acciones destructivas",
  "Aplicación" (Médicos y Detalle de médico, punto 6), "Microcopy", checklist "Baja".
- [Source: docs/ux/analisis-brechas-abm.md] — tablas 3 y 4 (B-3.5, B-4.5), decisión D3.
- [Source: docs/stories/3-3-alinear-abm-con-guia-ux.md] — AC 25, 26, 28, 39, 50, 52–53, 65, 80, 85; "Fuera de
  esta story (decisión D3)".
- [Source: docs/stories/3-1-especialidades.md] — AC 6, 10, 17; decisión 5.
- [Source: docs/stories/3-2-medicos.md] — AC 8, 15, 17, 20, 23; decisión M-6.
- [Source: docs/stories/4-1-dominio-agenda.md] — AC 20, 22, 27; ADR-06.
- [Source: docs/stories/4-2-propuesta-agenda.md] — AC 2, 21, 24; [Source: docs/stories/4-3-aprobacion-propuestas.md]
  — AC 13, 17; [Source: docs/stories/4-4-correccion-urgencia.md] — AC 8, 10, 17.
- [Source: docs/04-arquitectura.md] — §5.1 (componentes, `AlcancePorOperacion`), §6, §7.7, ADR-06, ADR-08.
- [Source: docs/stories/00-contrato-tecnico.md] — §Auditoría, §Pruebas.

## Preguntas para el docente

> **Decidido por el docente (2026-10-23):** todas las recomendadas (S-50 a S-57), aceptadas en bloque.
>
> Cada decisión queda marcada `[DECIDIDO S-5x]` en su AC. Lo que sí resuelve un documento está citado en el
> AC y no se preguntó.

**S-50 — ¿Qué pasa con el usuario del sistema de un médico desactivado y con su acceso a M2?** (AC 6, 29; ni el
PRD ni la guía lo dicen: FR-10 vincula, no dice qué pasa en la baja.)
- **A (recomendada).** El usuario queda activo y vinculado; M2 muestra el aviso y niega guardar y enviar.
  Reactivar al médico lo deja todo como estaba, sin volver a vincular. A4 sigue siendo el único lugar que
  cambia usuarios (2.2).
- B. Desactivar también al usuario (no entra más al sistema). Más cerrado, pero mezcla dos ABM y al reactivar
  hay que acordarse de reactivar el usuario en A4.
- C. Desvincularlo. Libera el usuario, pero se pierde el dato y reactivar obliga a vincular de nuevo.

**S-51 — ¿Qué pasa con una propuesta de agenda enviada y pendiente?** (AC 9)
- **A (recomendada).** Bloquear la desactivación con el motivo ("Revisala en «Propuestas de agenda»"): la guía
  pide bloquear y explicar cuando la baja rompe una regla, no toca la 4.3 y el administrador decide la propuesta
  primero.
- B. Rechazarla sola con un motivo fijo ("El médico fue desactivado"), con su auditoría y el email de la 4.3.
  Un paso menos, pero la aplicación decide por el administrador y agrega una tercera forma de rechazar.
- C. Dejarla pendiente: A6 la mostraría y se podría aprobar la agenda de alguien inactivo.

**S-52 — ¿Qué pasa con la agenda aprobada (vigente o programada) y qué muestra la columna "Estado de agenda" de
un inactivo?** (AC 8, 17)
- **A (recomendada).** Las agendas no se tocan (ADR-06) y el badge de agenda se sigue mostrando calculado, junto
  a la insignia "Inactivo". Que no se ofrezcan turnos lo garantiza el contrato con la 5.2 (AC 31). Al reactivar,
  todo vuelve a regir solo.
- B. Igual que A, pero la columna dice "Sin agenda" para un inactivo. Más simple de leer, pero oculta un dato
  verdadero y la ficha y el listado dirían cosas distintas que la base.

**S-53 — ¿Se puede corregir la agenda (4.4) de un médico inactivo?** (AC 27)
- **A (recomendada).** No: la ficha lo explica y el caso de uso lo niega. La corrección es para urgencias de un
  médico que atiende.
- B. Sí, para dejarle la agenda lista antes de reactivarlo. Útil en una vuelta de licencia larga, pero es una
  agenda aprobada de alguien que no atiende.

**S-54 — ¿Dónde se ve la insignia "Inactivo" en el listado A1?** (AC 17; la guía lista las columnas de Médicos
sin "Estado".)
- **A (recomendada).** Como A2 (3.3 AC 50): columna "Estado" sólo con el filtro "Todos"; en la tarjeta del
  celular, la insignia siempre que esté inactivo. Mismo patrón que Especialidades.
- B. La insignia al lado del nombre, en la columna Médico, siempre que esté inactivo. Se ve con cualquier filtro,
  pero A1 y A2 se resolverían distinto.
- C. Columna "Estado" siempre visible. Repite "Activo" en cada fila con el filtro por defecto (la guía lo marca
  como ruido en Especialidades).

**S-55 — ¿"Reactivar" pide confirmación?** (AC 20)
- **A (recomendada).** Sí, como Reactivar especialidad (3.3 AC 53): A1 y A2 son catálogos y se usan igual, y la
  reactivación tiene consecuencias (vuelve a ofrecerse para turnos).
- B. No, como Reactivar usuario en A4 (UX §1.5, "confirmar sólo lo irreversible"; 3.3 AC 77). Un clic menos,
  pero dos catálogos se comportarían distinto.

**S-56 — ¿Concurrencia optimista (`xmin`) en `medicos`?** (AC 5, 12; hoy A1 y A2 **no** la tienen: la 3.2 aceptó
"gana el último", M-6.)
- **A (recomendada).** Sí, en esta story: dos administradores que cambian el mismo médico reciben un 409 claro, y
  vale también para editar (deja de "ganar el último"). Es el patrón de `agendas_medico` y no crea columnas.
- B. No, como A2: la regla del dominio ("ya está desactivado") frena al segundo sólo si lee después del primero;
  dos bajas en el mismo instante dejarían dos registros de auditoría.

**S-57 — (nueva, de la revisión) ¿Qué muestra la pantalla "Corregir agenda" abierta por URL para un médico
inactivo?** (AC 27, T34)
- **A (recomendada, decidida).** El aviso de S-53 sin el editor, con un enlace a la ficha.
- B. Redirigir a la ficha. Más corto, pero sin explicar por qué.
- C. Dejarlo fuera de alcance: el editor se dibujaría y el error recién saldría al guardar.

## Change Log

| Fecha | Sesión | Cambio | Por qué |
|---|---|---|---|
| 2026-10-23 | 18 | Story escrita como borrador contra la rama `cierre/sesion-17` (`4df6e51`): 33 AC, 36 verificaciones, 7 preguntas para el docente (S-50 a S-56) | Brechas B-3.5 y B-4.5 (decisión D3); entra al S3 por decisión del docente (sesión 17), después de la 4.3 y la 4.4 |
| 2026-10-23 | 18 | Revisión adversarial previa (SADV-A5): el `INSERT INTO medicos` de `GestionMedicosEnBaseTests.cs` suma `activo`; va a `files_to_modify`, al AC 33 y a la Task 7 | Bloqueante: la columna `NOT NULL` sin valor por defecto rompía esa prueba de la 3.2 |
| 2026-10-23 | 18 | Revisión adversarial previa (SADV-A6): Task 1 y Dev Notes: si la 4.4 llega con la ficha SSR, la 3.5 consulta con `Operaciones.EjecutarAsync` y anota dónde queda el texto del AC 27 | La ficha real es interactiva (3.3 AC 94) |
| 2026-10-23 | 18 | Revisión adversarial previa (SADV-A7): motivo del `runtime_not_verifiable` del AC 33 corregido; R3 nombra el workflow `CI` (`.github/workflows/ci.yml`) | La CI ya existe |
| 2026-10-23 | 18 | Revisión adversarial previa (SADV-A8): T20 suma `?estado=otro` (queda "Activos" y sin filtro "Estado: …") | Esa cláusula del AC 16 no la ejercitaba ninguna prueba |
| 2026-10-23 | 18 | Decisiones del docente: S-50 a S-56 aceptadas en bloque (`[SUPUESTO]` → `[DECIDIDO]`) y S-57 nueva, decidida: "Corregir agenda" por URL para un inactivo muestra el aviso sin el editor (AC 27, T34). Quedan 33 AC y 37 verificaciones | `docs/stories/revision-sesion-18.md` |

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
