---
epic: 3
story: "3.4"
title: "Renombrar una especialidad desde A2 (Editar en la fila, modal con el nombre)"
status: ready-for-dev
sprint: 3
priority: "P1"
approach: "[ADAPT]"
depends_on: ["3.1", "3.3"]
followup_of:
  - id: "3.1"
    type: "extension"
    summary: "A2 suma Editar en cada fila: renombrar una especialidad activa o inactiva, con su auditoría y control de concurrencia"
followup_reason: cambio-de-requisito
runtime_not_verifiable:
  - ac: 13
    reason: "La ventana entre leer y guardar dura milisegundos dentro de una misma operación: no se puede provocar desde dos pestañas. T13 la abre con InterceptorQueSeAdelanta, que actualiza la fila desde otra conexión justo antes del guardado. La parte visible (otra pestaña cambió el nombre antes de guardar) la cubre el AC 26 en vivo."
    covered_by: T13
  - ac: 14
    reason: "Misma ventana de milisegundos, en desactivar y reactivar. T14 la abre con el interceptor."
    covered_by: T14
  - ac: 15
    reason: "Habla del mapeo de EF, de la migración y de los permisos de la base: no se ve en la app en ejecución. Lo prueban T15 y el diff."
    covered_by: T15
  - ac: 32
    reason: "Es un cambio en el contrato técnico (documentación). Se revisa en el diff."
    covered_by: R3
  - ac: 33
    reason: "Es un cambio en el PRD (documentación). Se revisa en el diff."
    covered_by: R3
  - ac: 37
    reason: "La CI corre sobre el PR, que todavía no existe: se verifica con la corrida del PR antes del merge."
    covered_by: R2
files_to_create:
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/<marca>_ConcurrenciaEnEspecialidades.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/<marca>_ConcurrenciaEnEspecialidades.Designer.cs"
files_to_modify:
  - "src/HospitalTurnos.Dominio/Catalogo/Especialidad.cs"
  - "src/HospitalTurnos.Dominio/Auditoria/AccionesAuditoria.cs"
  - "src/HospitalTurnos.Aplicacion/Catalogo/ICatalogoEspecialidades.cs"
  - "src/HospitalTurnos.Infraestructura/Catalogo/CatalogoEspecialidadesEnBase.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/EspecialidadConfiguracion.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/HospitalDbContextModelSnapshot.cs"
  - "src/HospitalTurnos.Web/Components/Comunes/MenuDeAcciones.razor"
  - "src/HospitalTurnos.Web/Components/Comunes/FormularioAbm.razor"
  - "src/HospitalTurnos.Web/Components/Comunes/DialogoModal.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/Especialidades.razor"
  - "src/HospitalTurnos.Web/wwwroot/app.css"
  - "docs/stories/00-contrato-tecnico.md"
  - "docs/02-prd.md"
  - "tests/HospitalTurnos.Dominio.Tests/Catalogo/EspecialidadTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Catalogo/CatalogoEspecialidadesTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Catalogo/EspecialidadesInteractivaTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Catalogo/EspecialidadesPaginaTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Catalogo/MedicosInteractivaTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Comunes/BaseInteractivaTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Comunes/ComponentesComunesTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Comunes/HojaDeEstilosTests.cs"
---

# Story 3.4: Renombrar una especialidad desde A2 (Editar en la fila, modal con el nombre)

Status: ready-for-dev

## Story

Como administradora del hospital (Lucía),
quiero corregir el nombre de una especialidad desde su fila en A2, con "Editar",
para arreglar un error de tipeo o un cambio de nombre sin crear una especialidad nueva ni perder los médicos
que ya la tienen asignada.

**De dónde sale.** Brecha **B-2.10** del [análisis de brechas](../ux/analisis-brechas-abm.md) (tabla 2: "El
dominio no permite renombrar una especialidad", impacto N). La decisión **D3** la mandó al backlog y el docente
la sumó al sprint 3 el 2026-10-17 ([plan](../05-epicas-y-plan.md), fila 3.4). La 3.3 la dejó afuera a propósito
(su línea "Fuera de esta story"). La [guía UX/UI](../ux/guia-ux-ui-abm.md), §"Aplicación", punto 3 de
Especialidades, fija dónde va: **"Acciones (Editar + '⋯' con Desactivar o Reactivar)"**, y el alta de 1 campo
vive en un modal (§"Dónde vive el formulario"): la edición usa el mismo modal.

## Acceptance Criteria

> Convenciones: los textos entre comillas son exactos. "Administrador" = el administrador inicial del Compose o
> uno creado en la prueba. Los campos de auditoría (`UsuarioId`, `Quien`, `Rol`, `Canal = Personal`,
> `EntidadTipo = "Especialidad"`, `EntidadId`) son los de la tabla de la 3.1; el actor sale de `IActorVigente`
> (leído de la base) y, en la página, de `ActorDelCircuito` (3.3, AC 2). `[DECIDIDO S-4x]` = la opción recomendada,
> aceptada por el docente el 2026-10-23 (sección "Preguntas para el docente").

### A. Dominio — `Especialidad.Renombrar`

1. **Given** una `Especialidad` "Cardiologia" (activa o desactivada) **When** se llama
   `Renombrar("  Cardiología ")` **Then** queda `Nombre = "Cardiología"` y `NombreNormalizado = "CARDIOLOGIA"`
   (las reglas de limpieza y de clave de la 3.1, AC 1 y 2), `Activa` no cambia, y devuelve
   `CambioDeDato("nombre", "Cardiologia", "Cardiología")`.
2. **Given** un nombre vacío, sólo con espacios o de más de 80 caracteres después de limpiarlo **When** se llama
   `Renombrar` **Then** lanza `ReglaDeNegocioException` con "Ingresá el nombre de la especialidad." o "El nombre
   de la especialidad tiene como máximo 80 caracteres." (los de la 3.1) y `Nombre` y `NombreNormalizado` no
   cambian.
3. **Given** "Cardiología" **When** se llama `Renombrar(" Cardiología  ")` (el mismo nombre una vez limpio)
   **Then** devuelve `null` y no cambia nada. Un cambio sólo de mayúsculas o tildes ("cardiología" →
   "Cardiología") **sí** es un cambio: devuelve el `CambioDeDato`.

### B. Caso de uso — `ICatalogoEspecialidades.RenombrarAsync`

4. **Given** un administrador activo y "Cardiologia" (id N) **When** llama
   `RenombrarAsync(actor, N, nombreVisto: "Cardiologia", nombreNuevo: "Cardiología", ct)` **Then** devuelve
   `true`; la fila queda con `nombre = 'Cardiología'` y `nombre_normalizado = 'CARDIOLOGIA'`; y en el **mismo**
   guardado queda un `RegistroAuditoria` con `Accion = AccionesAuditoria.EspecialidadRenombrada`
   (`"especialidad.renombrada"`), los campos de la tabla de la 3.1 y
   `Detalle = {"cambios":{"nombre":{"antes":"Cardiologia","despues":"Cardiología"}}}` (la misma forma que
   `medico.editado`). Los registros anteriores de esa especialidad (`especialidad.creada` con
   `{"nombre":"Cardiologia"}`) no se tocan: la historia conserva el nombre viejo (ADR-07).
5. **Given** un `nombreNuevo` que, limpio, es igual al actual **When** se renombra **Then** devuelve `false`, no
   guarda nada y no audita.
6. **Given** "Cardiologia" **When** se renombra a "Cardiología" (misma clave `CARDIOLOGIA`, la de **ella
   misma**) **Then** se permite: la búsqueda de repetidas excluye su propio id.
7. **Given** otra especialidad con la misma clave que el nombre nuevo **When** se renombra **Then** lanza
   `ConflictoException` con el mensaje de la 3.1 (AC 6), con el nombre **guardado** de la otra: "Ya existe la
   especialidad Pediatría." si está activa, o "Ya existe la especialidad Pediatría, desactivada. Reactivala
   desde la lista." si está desactivada; no se guarda nada ni se audita.
8. **Given** que otro administrador crea o renombra otra especialidad con la misma clave entre la verificación y
   el guardado **When** el índice único rechaza el guardado (`23505` sobre
   `ix_especialidades_nombre_normalizado`) **Then** se traduce a la **misma** `ConflictoException` del AC 7 (con
   el nombre de la ganadora), no queda el registro de auditoría del intento perdedor y nunca hay un `500`.
9. **Given** una especialidad **desactivada** **When** se la renombra **Then** se renombra y audita como en el
   AC 4 y **sigue desactivada**: no aparece en `ListarActivasAsync`. (La guía pone "Editar" en toda fila, también
   en las inactivas que se ven con el filtro: §"Aplicación", Especialidades, punto 3.)
10. **Given** un id que no existe **When** se renombra **Then** lanza `NoEncontradoException` con "La
    especialidad ya no existe. Actualizá la lista." (el de la 3.1, AC 12) y no audita.
11. **Given** un actor que no es un administrador activo **en la base** (otro rol, administrador desactivado con
    la sesión todavía abierta, id inexistente) **When** renombra **Then** lanza `AccesoDenegadoException` con
    "Sólo el administrador puede modificar las especialidades." (3.1, AC 13), sin cambios ni auditoría.

### C. Concurrencia (dos administradores a la vez) — `[DECIDIDO S-40]`

12. **Given** que el administrador abrió "Editar" viendo "Cardiologia" y, antes de guardar, otro administrador
    la renombró a "Cardiología médica" **When** guarda con `nombreVisto: "Cardiologia"` **Then** lanza
    `ConflictoException` con "Otro administrador cambió esta especialidad mientras la editabas. Revisá el nombre
    y guardá de nuevo." y no cambia nada ni audita. La comparación es exacta (ordinal) contra el `Nombre`
    guardado.
13. **Given** que entre la lectura y el guardado de `RenombrarAsync` otra conexión actualiza la fila (cambia el
    nombre **o** el estado) **When** se guarda **Then** el control `xmin` lo detecta
    (`DbUpdateConcurrencyException`) y se traduce a la misma `ConflictoException` del AC 12; en la base queda lo
    de la otra conexión y no queda el registro de auditoría.
14. **Given** la misma carrera en `DesactivarAsync` o `ReactivarAsync` **When** se guarda **Then** se traduce a
    `ConflictoException` con "Otro administrador cambió esta especialidad recién. Actualizá la lista y probá de
    nuevo." (sin esta traducción, al sumar `xmin` esa carrera terminaría en "Algo falló"). Sin carrera, los
    mensajes de la 3.1 no cambian.
15. `EspecialidadConfiguracion` mapea la propiedad sombra `Version` (`uint`) con `.IsRowVersion()
    .HasColumnName("xmin")`, como `AgendaMedicoConfiguracion`. La migración `ConcurrenciaEnEspecialidades` sólo
    actualiza el modelo: **no agrega ninguna columna** (si EF genera `AddColumn("xmin", …)`, se borra de `Up` y de
    `Down`; PostgreSQL rechaza una columna con el nombre de una columna de sistema). `turnos_app` sigue sin
    `DELETE` sobre `especialidades`.

### D. Contrato con médicos y otras pantallas

16. **Given** Cardiología con el médico Ríos, Pablo **When** se renombra a "Cardiología clínica" **Then**, sin
    tocar la fila de `medicos` ni auditar `medico.editado`: A1 (`IGestionMedicos.ListarPaginadoAsync` y
    `ListarAsync`) muestra "Cardiología clínica" en la columna Especialidad; la ficha (`ObtenerAsync`) la muestra
    en "Especialidad" (con " (desactivada)" si lo está, como hoy); el filtro Especialidad de A1 la ofrece con el
    nombre nuevo y una URL vieja `?especialidad=<id>` sigue filtrando lo mismo (va por id); y
    `ListarActivasAsync` la ordena por la clave nueva.
17. Lo que ya está escrito con el nombre viejo no se reescribe: la auditoría anterior (AC 4) y los emails ya
    enviados. Lo que se lee de `especialidades` muestra el nombre nuevo.

### E. Componentes comunes que se extienden (arquitectura §5.1)

18. `MenuDeAcciones` acepta un parámetro opcional `Visible` (`MenuDeAcciones.Opcion?`): si viene, dibuja **antes**
    del "⋯" un botón secundario chico (`btn btn-outline-secondary btn-sm`) con su texto, y al pulsarlo invoca
    `AlElegirDesde` con **ese** botón (para devolverle el foco al cerrar el diálogo que abra) y después la
    acción. El "⋯" y sus opciones no cambian. Sin `Visible`, el marcado es idéntico al de hoy (A1 y A4 no
    cambian).
19. `FormularioAbm` expone `public Task PedirCancelarAsync()`: hace lo mismo que su botón "Cancelar" (sin cambios
    cancela; con cambios abre "Tenés cambios sin guardar. ¿Salir igual?"). El botón "Cancelar" pasa a llamarlo;
    su comportamiento no cambia (3.3, AC 36). **Given** un guardado en curso ("Guardando…") **When** se pulsa
    "Cancelar" o Esc (que llama `PedirCancelarAsync`) **Then** no hace nada: ni cierra el modal ni abre la
    pregunta, y al volver la respuesta el guardado termina como siempre (AC 22, 23 o 25).

### F. Pantalla A2 — guía UX/UI

20. **Given** un administrador en A2 **When** se dibuja la tabla con cualquier filtro de estado **Then** cada
    fila, activa o inactiva, tiene en la columna de acciones "Editar" visible y después el "⋯", que sigue
    ofreciendo sólo "Desactivar" (activas) o "Reactivar" (inactivas). **Given** una ventana de menos de 768 px
    **Then** la tarjeta de cada fila muestra el nombre, el badge, "Editar" y "⋯", sin salirse de 375 px.
21. **Given** la fila de "Cardiologia" **When** se pulsa "Editar" **Then** se abre un `DialogoModal` titulado
    "Editar especialidad" con un `FormularioAbm`: la nota "\* Obligatorio", el campo "Nombre \*" (`id="nombre"`)
    con "Cardiologia" cargado y **con el foco**, y "Guardar" (primario) y "Cancelar" (secundario) al pie. Sin
    texto de ayuda sobre los médicos que la tienen `[DECIDIDO S-44]`.
22. **Given** el modal con "Cardiología" **When** se pulsa "Guardar" y el dominio acepta **Then** el modal se
    cierra, la tabla se recarga con el nombre nuevo **sin perder** búsqueda, filtro, orden ni página de la URL, y
    aparece la notificación "Especialidad actualizada" (verbo en pasado, como "Médico actualizado" de la guía;
    toast de la 3.3, AC 20).
23. **Given** el modal sin cambios efectivos (el mismo nombre, o sólo espacios de más) **When** se pulsa "Guardar"
    **Then** el modal se cierra, aparece "No había cambios para guardar" (texto de la 3.3, AC 20) y no hay
    auditoría.
24. **Given** el campo vacío o con más de 80 caracteres **When** el usuario sale del campo (blur) o pulsa
    "Guardar" **Then** aparece debajo del campo, en rojo, con ícono y enlazado con `aria-describedby`, "Ingresá el
    nombre de la especialidad." o "El nombre de la especialidad tiene como máximo 80 caracteres.", y no se llama
    al caso de uso (el dominio repite la regla en el servidor: AC 2).
25. **Given** un error del dominio al guardar (nombre de otra especialidad activa o desactivada, AC 7 y 8; ya no
    existe, AC 10; administrador desactivado con la sesión abierta, AC 11) **When** vuelve **Then** el modal **no**
    se cierra, conserva lo tipeado y muestra el mensaje con su texto exacto en el resumen del formulario
    (`role="alert"`, con el foco), sin código HTTP ni detalle técnico (3.3, AC 3).
26. **Given** dos pestañas de administradores con el modal de "Cardiologia" abierto **When** la primera guarda
    "Cardiología" y después la segunda guarda "Cardiología pediátrica" **Then** la segunda ve en el resumen el
    mensaje del AC 12 con lo tipeado intacto; la página vuelve a leer la especialidad (`BuscarAsync`) y toma
    "Cardiología" como nuevo `nombreVisto`, así que **un segundo "Guardar"** de la segunda pestaña sí renombra a
    "Cardiología pediátrica" (y audita `antes = "Cardiología"`).
27. **Given** un nombre válido **When** se pulsa "Guardar" **Then** el botón queda deshabilitado con "Guardando…"
    hasta que vuelve la respuesta y un segundo clic no llama dos veces al caso de uso (3.3, AC 35).
28. **Given** el modal con un cambio sin guardar **When** el usuario pulsa "Cancelar" **o** Esc **Then** se
    pregunta "Tenés cambios sin guardar. ¿Salir igual?" con "Seguir editando" (con el foco) y "Salir sin
    guardar"; "Salir sin guardar" cierra el modal sin guardar `[DECIDIDO S-41]` (Esc usa
    `FormularioAbm.PedirCancelarAsync`, AC 19). **Given** esa pregunta abierta **When** el usuario pulsa Esc
    **Then** equivale a "Seguir editando": la pregunta se cierra, el modal de edición sigue abierto con lo tipeado y
    la pregunta **no** se vuelve a abrir (el Esc no llega al modal de afuera: `DialogoModal` corta la propagación
    del `keydown` con `@onkeydown:stopPropagation`). **When** sigue un enlace del menú lateral o de las migas, o
    recarga la pestaña **Then** pregunta el navegador, como en la 3.3 (AC 36).
29. **Given** el modal sin cambios **When** el usuario pulsa Esc o "Cancelar" **Then** el modal se cierra y el foco
    vuelve al botón "Editar" de esa fila. Todo se opera con teclado: Tab llega a "Editar", Enter lo abre, Tab y
    Mayús+Tab circulan dentro del modal (3.3, AC 38), Enter en el campo envía.
30. **Given** una ventana de menos de 768 px **When** se dibuja una tarjeta **Then** "Editar" y "⋯" tienen un área
    de al menos 44 × 44 px (guía, Accesibilidad: táctil) `[DECIDIDO S-43]`, y también cada opción del "⋯" abierto
    en la tarjeta (`.menu-de-acciones__opcion`) mide al menos 44 px de alto; desde 768 px, al menos 24 × 24 px. El
    cambio va en `app.css`, en las reglas de `.tarjeta-abm`, sin estilos en la vista.
38. **Given** A2 con un filtro o una página que incluye la fila (por ejemplo `?buscar=card` con "Cardiología", o la
    última fila de la página 2) **When** se la renombra a un nombre que ya no cumple el filtro ("Pediatría") o que
    la ordena en otra página **Then** el modal se cierra, la tabla se recarga con la URL de antes y la fila ya no
    aparece en esa vista, el aviso "Especialidad actualizada" se muestra igual, el total del encabezado baja si
    corresponde, y si la página quedó vacía se muestra la última página con datos (3.3, AC 81), nunca una página
    fuera de rango ni un error.

### Modos de falla

31. **Given** una excepción no esperable al guardar (por ejemplo, la base caída) **When** se pulsa "Guardar"
    **Then** se ve "Algo falló. Probá de nuevo en unos minutos." sin detalle técnico (`LimiteDeErrores`, 3.3,
    AC 4) y el detalle queda en el registro técnico; no queda nada guardado ni auditado.

### Documentación

32. El contrato técnico (`00-contrato-tecnico.md`) suma: en §Auditoría, `especialidad.renombrada` entre los
    ejemplos de acciones; en §Base de datos, que `agendas_medico` (4.1) y `especialidades` (3.4) usan concurrencia
    optimista con `xmin` (propiedad sombra `Version`, `HasColumnName("xmin")`).
33. FR-12 del PRD dice "El administrador puede crear especialidades, **renombrarlas** y desactivarlas si no tienen
    turnos futuros." `[DECIDIDO S-42]`

### No-regresión

34. El alta, desactivar, reactivar, el buscador, el filtro de estado, el orden y la paginación de A2 se comportan
    como en la 3.3 (AC 47 a 55, 84, 85) y la 3.1; las pruebas existentes de A2 pasan sin cambiar lo que verifican
    (los dobles de `ICatalogoEspecialidades` sólo suman el método nuevo).
35. A1 y A4 dibujan su "⋯" igual que hoy (sin `Visible`) y sus tarjetas siguen entrando en 375 px con el área
    táctil del AC 30.
36. Un GET sin sesión a A2 sigue respondiendo `302` al ingreso y con otro rol `403` con "No tenés permiso para
    entrar a esta sección." (3.3, AC 89).
37. La suite completa y la CI quedan en verde, con cobertura de líneas ≥ 80 % en Web y sin baja en Dominio y
    Aplicación.

### Trazabilidad guía → criterio

| Punto de la guía ("Debe") | AC |
|---|---|
| Aplicación, Especialidades 3: Editar + "⋯" con Desactivar/Reactivar | 18, 20 |
| Dónde vive el formulario: 1 a 3 campos → modal | 21 |
| Foco en el primer campo; una columna; obligatorios marcados | 21 |
| Validar al salir del campo y en el servidor; error debajo con ícono | 2, 24 |
| Detectar duplicados antes de guardar (sin distinguir mayúsculas ni tildes) | 6, 7, 8 |
| "Guardar" + "Cancelar" al pie; "Guardando…" (sin doble envío ni cancelar a medias) | 19, 21, 27 |
| Avisar si se sale con cambios sin guardar | 28 |
| Tras guardar, notificación con el verbo en pasado (también si la fila sale de la vista) | 22, 23, 38 |
| Error al guardar: el formulario conserva lo cargado | 25, 26 |
| Teclado completo, Esc cierra, foco vuelve a quien abrió | 29 |
| Áreas de clic 44 × 44 táctil / 24 × 24 escritorio (botones y opciones del "⋯") | 30 |
| Tarjetas bajo 768 px con el menú | 20 |
| Errores sin detalle técnico | 25, 31 |

### Tests (32 verificaciones)

Todas llevan `[Trait("Story", "3.4")]` y un `[Trait("AC", "n")]` por criterio cubierto.

| # | Test | AC | Nivel | Escenario | Resultado esperado |
|---|------|----|-------|-----------|--------------------|
| T1 | `Renombrar_NombreValido_CambiaNombreYClave_YDevuelveElCambio` [Theory] | 1, 3 | unit | (a) activa "Cardiologia" → "  Cardiología "; (b) desactivada, ídem; (c) "cardiología" → "Cardiología" | Nombre y clave del AC 1; `Activa` igual que antes; devuelve el `CambioDeDato` con antes y después |
| T2 | `Renombrar_NombreVacioOLargo_LanzaReglaSinCambiar` [Theory] | 2 | unit | `""`, `"   "`, 81 letras | `ReglaDeNegocioException` con el texto del AC 2; `Nombre` y `NombreNormalizado` sin cambios |
| T3 | `Renombrar_MismoNombreLimpio_DevuelveNullSinCambiar` | 3 | unit | "Cardiología" → " Cardiología  " | `null`; nada cambió |
| T4 | `RenombrarAsync_Administrador_GuardaYAuditaConAntesYDespues` | 4, 17 | integration | Administrador propio; "Cardiologia <marca>" creada antes; renombrar a "Cardiología <marca>" | `true`; fila con el nombre y la clave nuevos; 1 registro `especialidad.renombrada` con los campos de la 3.1 y el `Detalle` del AC 4; el `especialidad.creada` sigue con el nombre viejo |
| T5 | `RenombrarAsync_SinCambios_DevuelveFalseSinGuardarNiAuditar` | 5 | integration | Renombrar a " <mismo nombre> " | `false`; 0 registros `especialidad.renombrada` con su id |
| T6 | `RenombrarAsync_SoloMayusculasOTildesDeLaMisma_LoPermite` | 6 | integration | "cardiologia <marca>" → "Cardiología <marca>" | `true`; sin `ConflictoException` |
| T7 | `RenombrarAsync_NombreDeOtra_LanzaConflictoSinGuardarNiAuditar` [Theory] | 7 | integration | Otra "Pediatría <marca>" (a) activa, (b) desactivada; renombrar a "pediatria <marca>" | `ConflictoException` con el texto del AC 7 y el nombre guardado; la renombrada no cambió; 0 registros |
| T8 | `RenombrarAsync_OtraGanaLaCarreraDelNombre_TraduceElIndiceUnicoAConflicto` | 8 | integration | `InterceptorQueSeAdelanta` inserta "Neumo <marca>" justo antes del guardado; se renombra otra a "neumo <marca>" | `ConflictoException` "Ya existe la especialidad Neumo <marca>."; la renombrada no cambió; 0 registros del intento |
| T9 | `RenombrarAsync_Desactivada_LaRenombraYSigueDesactivada` | 9 | integration | Especialidad propia desactivada | Renombrada y auditada; `activa = false`; no está en `ListarActivasAsync` |
| T10 | `RenombrarAsync_IdInexistente_LanzaNoEncontradoSinAuditar` | 10 | integration | id `long.MaxValue` | `NoEncontradoException` con el texto del AC 10; 0 registros |
| T11 | `RenombrarAsync_ActorQueNoEsAdministradorActivo_LanzaAccesoDenegadoSinCambios` [Theory] | 11 | integration | Recepcionista, médico, administrador con `Activo = false`, Guid nuevo con rol Administrador | `AccesoDenegadoException` con el texto del AC 11; nombre sin cambios; 0 registros |
| T12 | `RenombrarAsync_NombreVistoDistintoDelActual_LanzaConflictoSinCambios` | 12 | integration | Renombrada antes por otro actor; se llama con el `nombreVisto` viejo | `ConflictoException` con el texto del AC 12; queda el nombre del otro; 0 registros del intento |
| T13 | `RenombrarAsync_OtroCambioEntreLeerYGuardar_TraduceLaConcurrenciaAConflicto` [Theory] | 13 | integration | Interceptor con (a) `UPDATE especialidades SET nombre = …, nombre_normalizado = … WHERE id = <id>`; (b) `UPDATE especialidades SET activa = false WHERE id = <id>` | `ConflictoException` con el texto del AC 12; en la base, lo del interceptor; 0 registros `especialidad.renombrada` |
| T14 | `CambiarEstadoAsync_OtroCambioEntreLeerYGuardar_TraduceLaConcurrenciaAConflicto` [Theory] | 14 | integration | Interceptor con un `UPDATE` del nombre; (a) desactivar una activa; (b) reactivar una desactivada | `ConflictoException` con el texto del AC 14; el estado no cambió; 0 registros del intento |
| T15 | `Especialidades_ConcurrenciaConXmin_SinColumnaNiPermisosNuevos` | 15 | integration | Modelo de EF + `information_schema.columns` + `has_table_privilege` | `Version` es row version con columna `xmin`; no existe la columna `version` en `especialidades`; `DELETE` de `turnos_app` sigue en `false` |
| T16 | `RenombrarAsync_MedicosVenElNombreNuevo_SinTocarlos` | 16 | integration | `DatosDeCatalogo`: un médico en la especialidad; renombrar | `ListarPaginadoAsync` (con y sin `especialidad=<id>`), `ListarAsync` y `ObtenerAsync` traen el nombre nuevo; la fila del médico igual; 0 `medico.editado`; `ListarActivasAsync` en el orden de la clave nueva |
| T17 | `MenuDeAcciones_ConAccionVisible_LaDibujaAntesYDevuelveSuBoton` | 18 | unit | bUnit con `Visible = "Editar"` y una opción | El botón "Editar" va antes del "⋯"; al pulsarlo, `AlElegirDesde` recibe ese botón y la acción corre una vez |
| T18 | `MenuDeAcciones_SinAccionVisible_DibujaIgualQueAntes` | 18, 35 | unit | bUnit sin `Visible` | Un solo `<button>` fuera de la lista (el "⋯"); mismo marcado que hoy |
| T19 | `Formulario_PedirCancelar_ConCambiosPreguntaYSinCambiosCancela` | 19 | unit | bUnit: `PedirCancelarAsync` sin cambios; cambiar un campo y llamarlo otra vez | Sin cambios: `AlCancelar` una vez; con cambios: el diálogo "Tenés cambios sin guardar. ¿Salir igual?" y `AlCancelar` sin llamar |
| T20 | `Especialidades_EditarVisibleEnCadaFila_YMenuConLoDeHoy` | 20 | integration | bUnit de A2 con filtro "Todas", una activa y una inactiva | Cada fila (y cada tarjeta) tiene "Editar" y "⋯"; el "⋯" ofrece "Desactivar" o "Reactivar", nada más |
| T21 | `Especialidades_Editar_AbreModalConElNombreYElFoco` | 21 | integration | Pulsar "Editar" en "Cardiologia <marca>" | Diálogo "Editar especialidad"; "Nombre \*" con el valor; foco en `#nombre`; "Guardar" antes que "Cancelar" |
| T22 | `Especialidades_EditarYGuardar_CierraRecargaYNotifica` [Theory] | 22, 23, 38 | integration | (a) con `?buscar=<marca>&estado=todos` cambiar a "Cardiología <marca>"; (b) guardar el mismo nombre; (c) con `?buscar=card<marca>` renombrar la única fila a "Pediatría <marca>"; (d) con `?tamano=25&pagina=2` y 26 propias, renombrar la única fila de la página 2 para que pase a la 1 | (a) Sin diálogo, la fila con el nombre nuevo, la URL igual, "Especialidad actualizada"; (b) "No había cambios para guardar" y 0 registros; (c) "Especialidad actualizada", la fila ya no está y se ve el estado "sin resultados"; (d) "Especialidad actualizada" y se muestra la página 1 con datos, sin error |
| T23 | `Especialidades_Editar_ValidacionYErroresDelDominioQuedanEnElModal` [Theory] | 24, 25 | integration | Vacío (blur), 81 caracteres, nombre de otra activa, de otra desactivada, administrador propio desactivado en la base | Mensajes de los AC 24 y 25 con el modal abierto y lo tipeado intacto; el resumen con `role="alert"` |
| T24 | `Especialidades_Editar_ConflictoDeConcurrencia_ReleeYElSegundoGuardarFunciona` | 26 | integration | Abrir el modal; renombrar por el caso de uso desde otro alcance; guardar; volver a guardar | Primero el mensaje del AC 12; el segundo guardado renombra y audita `antes` = el nombre del otro |
| T25 | `Especialidades_Editar_GuardandoSinDobleEnvioNiCancelar` | 19, 27 | integration | Doble de `ICatalogoEspecialidades` cuyo `RenombrarAsync` espera un `TaskCompletionSource`; mientras espera: doble clic en "Guardar", clic en "Cancelar" y Esc; después se completa la tarea | "Guardando…" deshabilitado; `RenombrarAsync` llamado una vez; mientras espera, el modal sigue abierto y no aparece la pregunta de cambios sin guardar; al completar, se cierra con "Especialidad actualizada" |
| T26 | `Especialidades_Editar_CambiosSinGuardar_PreguntanConCancelarYConEsc` [Theory] | 28 | integration | Cambiar el campo y (a) "Cancelar"; (b) Esc; (c) Esc para abrir la pregunta y otro Esc sobre ella | (a, b) El diálogo de cambios sin guardar con el foco en "Seguir editando"; "Salir sin guardar" cierra sin llamar al caso de uso; (c) la pregunta se cierra, no se vuelve a abrir y el modal de edición sigue abierto con lo tipeado |
| T27 | `Especialidades_Editar_SinCambiosEscCierraYDevuelveElFoco` | 29 | integration | Abrir con "Editar", Esc sin tocar nada | Sin diálogo; el foco vuelve al botón "Editar" de esa fila (el `Origen` del `DialogoModal` es ese botón) |
| T28 | `Tarjeta_AccionesConAreaTactilDe44` | 30, 35 | unit | Leer `app.css` | Bajo 768 px, los botones de `.tarjeta-abm .table-actions` (incluido `.menu-de-acciones__boton`) tienen `min-width` y `min-height` ≥ `2.75rem`, y `.tarjeta-abm .menu-de-acciones__opcion` tiene `min-height` ≥ `2.75rem`; sin hexadecimales nuevos |
| T29 | `Especialidades_Editar_ErrorInesperado_MuestraElMensajeGenerico` | 31 | integration | Doble cuyo `RenombrarAsync` lanza `InvalidOperationException` | "Algo falló. Probá de nuevo en unos minutos."; el registro en memoria tiene la excepción |
| R1 | Suite existente de A2, A1, A4 y permisos | 34, 35, 36 | integration | `dotnet test` de los tres proyectos | 0 fallas; las pruebas de la 3.1 y la 3.3 de A2 (T48–T51, `MedicosHttpTests.PaginasAbm_SinSesionOConOtroRol_SiguenEn302O403`) sin cambiar sus aserciones |
| R2 | CI y cobertura | 37 | integration | CI del PR | Verde; líneas ≥ 80 % en Web |
| R3 | Revisión del diff de documentación | 32, 33 | integration | Diff de `00-contrato-tecnico.md` y `02-prd.md` | Los textos de los AC 32 y 33 |

> 32 verificaciones: T1–T29 más R1–R3. Los casos de cada `[Theory]` se suman al cerrar. No hay E2E con Playwright
> (el proyecto `HospitalTurnos.E2E` llega con la 10.1, igual que en la 3.3); en su lugar, la **matriz en vivo** del
> Compose recorre en Chrome un paso por AC de las secciones F, modos de falla y no-regresión visibles (20–31,
> 34–36 y 38), con teclado y a 375 px. El AC 26 se prueba en vivo con dos pestañas.

#### Given/Fixture

- **T1–T3** — `HospitalTurnos.Dominio.Tests/Catalogo/EspecialidadTests.cs`; `new Especialidad("Cardiologia")`; la
  desactivada con `Desactivar()`.
- **T4–T16** — `CatalogoEspecialidadesTests` (`[Collection("Postgres")]`), con su `Proveedor()`,
  `AdministradorAsync` y `Marca()` de hoy; cada prueba usa nombres con su marca (base compartida, sin limpiadores).
  T8, T13 y T14 usan `Proveedor(s => s.ConfigureDbContext<HospitalDbContext>(o => o.AddInterceptors(interceptor)))`
  con `new InterceptorQueSeAdelanta(fixture.CadenaApp, "<SQL>")` (como T8 de la 3.1 y T29 de la 4.2): el `UPDATE`
  con los valores escritos en el propio SQL. T16 arma el médico con `DatosDeCatalogo` y lee con `IGestionMedicos`.
- **T17–T19** — `ComponentesComunesTests`, como `MenuDeAcciones_TieneNombreAccesible_YTeclado` y
  `Formulario_MarcaObligatorios_YEnlazaErrores`.
- **T20–T27, T29** — `EspecialidadesInteractivaTests`, con `PersonalWebApplicationFactory`, `ConLaApp`,
  `ComoAdmin1Async` y las ayudas `AbrirAsync`, `FilaDe`, `CambiarAsync`, `EnviarAsync` de hoy. Para pulsar
  "Editar" se suma una ayuda `EditarAsync(pagina, nombre)` que busca en `FilaDe(...)` el botón
  `.table-actions .btn-outline-secondary`. T25 y T29 registran un doble de `ICatalogoEspecialidades` en lugar del
  real (como `CatalogoQueRechaza`); el resto, los casos de uso reales contra Testcontainers. T24 renombra "desde
  otro administrador" resolviendo `ICatalogoEspecialidades` de un alcance propio de la fábrica.
- **T28** — `HojaDeEstilosTests`, como `Tarjeta_LasAccionesNoSeAchicanNiSalenDeLaPantalla`.

## Tasks / Subtasks

- [ ] [SEQ] Task 1: Salvaguarda de estado (AC: todos)
  - [ ] Confirmar contra `main` lo de "Que existe hoy": firmas de `ICatalogoEspecialidades`, `Especialidad`,
        `MenuDeAcciones`, `FormularioAbm`, `Especialidades.razor` y los cinco dobles de `ICatalogoEspecialidades`.
  - [ ] Confirmar que `especialidades` no tiene hoy control de concurrencia (`grep -n xmin` en
        `EspecialidadConfiguracion.cs` → 0) y que `turnos_app` no tiene `DELETE` (T14 de la 3.1).
- [ ] [SEQ] Task 2: Dominio (AC 1–3). T1–T3 en rojo, después `Especialidad.Renombrar`.
- [ ] [SEQ] Task 3: Persistencia (AC 15). `Version`/`xmin` en `EspecialidadConfiguracion`, migración vacía, T15.
- [ ] [SEQ] Task 4: Caso de uso (AC 4–14, 16, 17). `AccionesAuditoria.EspecialidadRenombrada`, `RenombrarAsync`,
      la traducción de concurrencia en `CambiarEstadoAsync`; el método nuevo en los cinco dobles. T4–T14, T16.
- [ ] [PAR] Task 5: Componentes (AC 18, 19, 30). `MenuDeAcciones.Visible`, `FormularioAbm.PedirCancelarAsync`, área
      táctil en `app.css`. T17–T19, T28.
- [ ] [SEQ] Task 6: Pantalla A2 (AC 20–29, 31). "Editar" con `Visible`, modal de edición, relectura tras conflicto.
      T20–T27, T29.
- [ ] [PAR] Task 7: Documentación (AC 32, 33). Contrato técnico y FR-12 del PRD.
- [ ] [SEQ] Task 8: Suite completa, CI y matriz en vivo en el Compose (AC 34–37). R1–R3.

### Task Dependencies

T1 → T2 → T3 → T4 → T6 → T8. T5 en paralelo con T2–T4 (debe estar en verde antes de T6). T7 en paralelo con todo.

## Dev Notes

### Que existe hoy

#### En hospital-turnos (verificado 2026-10-23 contra `main` en `7951d55`)

- **`src/HospitalTurnos.Dominio/Catalogo/Especialidad.cs`** — `Nombre`, `NombreNormalizado`, `Activa`;
  `Limpiar(string?)` (lanza las reglas del AC 2), `Normalizar(string)` (clave sin tildes ni mayúsculas),
  `Desactivar()`, `Reactivar()`. **No existe** `Renombrar`: los setters son privados, se agrega en la clase.
- **`src/HospitalTurnos.Dominio/Catalogo/CambioDeDato.cs`** — `record CambioDeDato(string Campo, string? Antes,
  string? Despues)`; lo devuelve `Medico.CambiarDatos` (3.2). Se reusa.
- **`src/HospitalTurnos.Dominio/Auditoria/AccionesAuditoria.cs:33-35`** — `EspecialidadCreada`,
  `EspecialidadDesactivada`, `EspecialidadReactivada`. **No existe** `EspecialidadRenombrada`.
- **`src/HospitalTurnos.Aplicacion/Catalogo/ICatalogoEspecialidades.cs`** — `ListarAsync`, `ListarActivasAsync`,
  `BuscarAsync`, `ListarPaginadoAsync`, `ContarMedicosAsync`, `CrearAsync(actor, nombre, ct)`,
  `DesactivarAsync(actor, id, ct)`, `ReactivarAsync(actor, id, ct)`. **No existe** `RenombrarAsync`.
- **`src/HospitalTurnos.Infraestructura/Catalogo/CatalogoEspecialidadesEnBase.cs`** — constantes `SinPermiso`,
  `YaNoExiste`, `EntidadTipo = "Especialidad"`, `IndiceNombre`; `CrearAsync` (verificación previa + traducción del
  `23505` en el `catch (DbUpdateException …)` de `CrearAsync`, hoy ~línea 100); `CambiarEstadoAsync` (permiso → buscar → cambio → auditar → un `SaveChangesAsync`);
  `Repetida(existente)` arma los dos mensajes del AC 7.
- **`src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/EspecialidadConfiguracion.cs`** — clave, largos
  e índice único de `NombreNormalizado`. **Sin `xmin`.**
- **Concurrencia en A1 y A2 hoy: no hay.** Sólo `agendas_medico` tiene `xmin`
  (`AgendaMedicoConfiguracion.cs:22`). En médicos se aceptó "gana el último" (3.2, Dev Notes, M-6). Por eso el AC 12
  a 15 es una decisión (S-40), no una copia de A1.
- **`src/HospitalTurnos.Web/Components/Personal/Administracion/Especialidades.razor`** — A2 de la 3.3: `PaginaAbm`,
  `BarraDeFiltros`, `TablaAbm` con `<Acciones>` = sólo `MenuDeAcciones` (el `<MenuDeAcciones>` dentro de `<Acciones>`, hoy línea 30, `AlElegirDesde="r => origen =
  r"`), modal de alta con `FormularioAbm` y la clase privada `FormularioAlta` (`[Required]` y `[StringLength(80)]`
  con los textos de la 3.1); `ConCatalogoAsync` pide el caso de uso a `AlcancePorOperacion`; `TerminarAsync` cierra,
  avisa y recarga la tabla; `ActorDelCircuito.DesdeAsync(EstadoDeAutenticacion)`.
- **`TablaAbm.razor:92` y `:108`** — `<Acciones>` se dibuja **dos veces** por fila (tabla y tarjeta, una oculta por
  CSS). Por eso el foco de vuelta no puede salir de un diccionario por id: lo da el propio botón (AC 18).
- **`MenuDeAcciones.razor`** — `Opcion(Texto, Accion)`, `AlElegirDesde` (le pasa el botón "⋯" antes de la acción),
  `UnoAbierto`. **No existe** una acción visible.
- **`FormularioAbm.razor`** — `CancelarAsync` (privado), `NavigationLock` con `ConfirmExternalNavigation`, diálogo
  "Tenés cambios sin guardar. ¿Salir igual?", "Guardando…", resumen con foco. **No existe** un método público para
  pedir cancelar.
- **`DialogoModal.razor`** — `AlCerrar` (Esc) y `Origen` (recibe el foco al cerrarse; tolera que ya no exista).
- **`TablaMedicos.razor:12`** — en A1 "Editar" es un enlace a la ficha: no abre modal ni necesita devolver el foco.
- **`FichaMedico.razor:165`** — `Avisos.Mostrar(guardo ? "Médico actualizado" : "No había cambios para guardar")`:
  el patrón del AC 22 y 23.
- **`IGestionMedicos`** — `MedicoListado.Especialidad` y `DatosFichaMedico.Especialidad` salen de un join con
  `especialidades` en cada lectura: el nombre nuevo se ve sin tocar médicos (AC 16).
- **Dobles que implementan `ICatalogoEspecialidades`** (hay que sumarles el método): `BaseInteractivaTests.cs:126`
  (`CatalogoQueFalla`), `MedicosInteractivaTests.cs:129` (`CatalogoCaido`) y `:400` (`CatalogoSinActivas`),
  `EspecialidadesInteractivaTests.cs:158` (`CatalogoQueRechaza`), `EspecialidadesPaginaTests.cs:20` (`CatalogoVacio`).
- **`app.css`** — `.btn { min-height: 2.5rem }` (40 px) y `.menu-de-acciones__boton { min-width: 2.75rem;
  min-height: 2.5rem }`: bajo 768 px no llegan a 44 px de alto (AC 30).

### Patron de referencia verificado

**Patrón**: editar una entidad del catálogo en una sola operación (permiso en la base → leer → regla del dominio →
auditar con antes/después → un guardado), con la carrera del índice único y la de concurrencia (`xmin`) traducidas a
`ConflictoException`. Todo ya está resuelto en el repo; el proyecto guía de la materia no tiene EF ni Blazor
(verificado en la 2.1 y la 3.3).

| Fuente | Ancla | Qué se verificó y qué se toma |
|---|---|---|
| hospital-turnos (3.2) | `src/HospitalTurnos.Infraestructura/Catalogo/GestionMedicosEnBase.cs:180-212` | `EditarAsync`: `ExigirAdministradorActivoAsync`, busca con seguimiento o `NoEncontradoException`, el dominio devuelve la lista de `CambioDeDato`, sin cambios devuelve `false`, audita `medico.editado` con `cambios = cambios.ToDictionary(c => c.Campo, c => new { antes, despues })` (línea 209) y guarda una vez. Se toma igual, con un solo cambio (`nombre`). |
| hospital-turnos (3.1) | `src/HospitalTurnos.Infraestructura/Catalogo/CatalogoEspecialidadesEnBase.cs` (el `catch` de `CrearAsync`, hoy 100-111) | `catch (DbUpdateException e) when (… SqlState: UniqueViolation, ConstraintName: IndiceNombre)` → relee la ganadora y `throw Repetida(ganadora)`. Se toma para el AC 8 (sin transacción explícita: hay un solo guardado). |
| hospital-turnos (4.2) | `src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/AgendaMedicoConfiguracion.cs:20-22` | `Property<uint>("Version").IsRowVersion().HasColumnName("xmin")`, con el comentario de por qué el nombre va explícito (la convención snake_case lo renombraría a `version`). Se copia en `EspecialidadConfiguracion`. |
| hospital-turnos (4.2) | `src/HospitalTurnos.Infraestructura/Agendas/PropuestaAgendaEnBase.cs:181-183` | `catch (DbUpdateConcurrencyException) { throw new ConflictoException(CambioMientrasTanto); }`. Se toma para el AC 13 y 14. |
| hospital-turnos (4.2) | `tests/HospitalTurnos.Aplicacion.Tests/Agendas/PropuestaAgendaTests.cs:391-400` | La prueba de concurrencia: `ProveedorConCarrera("UPDATE agendas_medico … WHERE id = …")` con `InterceptorQueSeAdelanta`. Se toma para T13 y T14. |
| hospital-turnos (3.3) | `Especialidades.razor` (el `<MenuDeAcciones>` dentro de `<Acciones>`, hoy línea 30) + `MenuDeAcciones.razor` (`ElegirAsync`: `AlElegirDesde.InvokeAsync(Boton)` antes de la acción) | El botón que abre el diálogo se entrega a la página para `Origen`. La acción visible del AC 18 hace lo mismo con su propio botón. |

**Conteo**: 1/1 entidad con `xmin` (`AgendaMedico`) lo mapea así; 2/2 escrituras con índice único del catálogo
(especialidad, matrícula) traducen el `23505` a la regla.

### Que hay que hacer (la delta)

1. **Dominio** — en `Especialidad`:
   ```csharp
   /// <summary>Corrige el nombre (story 3.4). Mismas reglas que el alta; null si, limpio, no cambia nada.</summary>
   public CambioDeDato? Renombrar(string? nombre)
   {
       var limpio = Limpiar(nombre);                       // lanza las reglas del AC 2 antes de tocar nada
       if (limpio == Nombre) return null;                   // ordinal: "cardiología" → "Cardiología" sí cambia
       var cambio = new CambioDeDato("nombre", Nombre, limpio);
       Nombre = limpio;
       NombreNormalizado = Normalizar(limpio);
       return cambio;
   }
   ```
2. **Auditoría** — `public const string EspecialidadRenombrada = "especialidad.renombrada";` junto a las de la 3.1.
3. **Puerto** — en `ICatalogoEspecialidades`:
   `Task<bool> RenombrarAsync(ActorAuditoria actor, long id, string nombreVisto, string nombreNuevo, CancellationToken ct);`
   con su comentario (`nombreVisto` = el nombre que tenía la pantalla al abrir "Editar"; `false` = sin cambios).
4. **Adaptador** — en `CatalogoEspecialidadesEnBase`:
   ```csharp
   private const string CambioMientrasEditabas =
       "Otro administrador cambió esta especialidad mientras la editabas. Revisá el nombre y guardá de nuevo.";
   private const string CambioRecien =
       "Otro administrador cambió esta especialidad recién. Actualizá la lista y probá de nuevo.";

   public async Task<bool> RenombrarAsync(ActorAuditoria actor, long id, string nombreVisto, string nombreNuevo,
       CancellationToken ct)
   {
       actor = await actorVigente.ExigirAdministradorActivoAsync(actor, SinPermiso, ct);          // AC 11
       var especialidad = await contexto.Especialidades.FirstOrDefaultAsync(e => e.Id == id, ct)
           ?? throw new NoEncontradoException(YaNoExiste);                                        // AC 10
       if (especialidad.Nombre != nombreVisto) throw new ConflictoException(CambioMientrasEditabas); // AC 12
       var cambio = especialidad.Renombrar(nombreNuevo);                                          // AC 2
       if (cambio is null) return false;                                                          // AC 5
       var otra = await contexto.Especialidades.AsNoTracking()                                    // AC 6 y 7
           .FirstOrDefaultAsync(e => e.NombreNormalizado == especialidad.NombreNormalizado && e.Id != id, ct);
       if (otra is not null) throw Repetida(otra);
       auditoria.Registrar(actor, AccionesAuditoria.EspecialidadRenombrada, EntidadTipo,
           id.ToString(CultureInfo.InvariantCulture),
           new { cambios = new Dictionary<string, object> { [cambio.Campo] = new { antes = cambio.Antes, despues = cambio.Despues } } });
       try
       {
           await contexto.SaveChangesAsync(ct);             // un guardado: el cambio y su registro, juntos o nada
       }
       catch (DbUpdateConcurrencyException)
       {
           throw new ConflictoException(CambioMientrasEditabas);                                  // AC 13
       }
       catch (DbUpdateException e) when (e.InnerException is PostgresException
       {
           SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: IndiceNombre,
       })
       {
           var ganadora = await contexto.Especialidades.AsNoTracking()                            // AC 8
               .FirstAsync(x => x.NombreNormalizado == especialidad.NombreNormalizado && x.Id != id, ct);
           throw Repetida(ganadora);
       }
       return true;
   }
   ```
   y en `CambiarEstadoAsync`, el `SaveChangesAsync` dentro de
   `try { … } catch (DbUpdateConcurrencyException) { throw new ConflictoException(CambioRecien); }` (AC 14).
5. **Persistencia** — en `EspecialidadConfiguracion`, la línea de `xmin` copiada de `AgendaMedicoConfiguracion` con
   su comentario. Generar la migración y **revisarla**: si trae `AddColumn`/`DropColumn` de `xmin`, se borran y
   `Up`/`Down` quedan vacíos (con un comentario que explica por qué); el snapshot sí cambia.
6. **`MenuDeAcciones`** — parámetro `[Parameter] public Opcion? Visible { get; set; }`; si no es null, antes del
   botón "⋯" un `<button type="button" class="btn btn-outline-secondary btn-sm" @ref="botonVisible"
   @onclick="ElegirVisibleAsync">@Visible.Texto</button>`, y `ElegirVisibleAsync` hace
   `await AlElegirDesde.InvokeAsync(botonVisible); await Visible.Accion.InvokeAsync();`. Al pulsarlo, el "⋯"
   abierto (si lo hay) se cierra.
7. **`FormularioAbm`** — `CancelarAsync` pasa a `public Task PedirCancelarAsync()` y el botón lo llama; empieza con
   `if (guardando) return Task.CompletedTask;` (AC 19: mientras guarda, Cancelar y Esc no hacen nada).
7 bis. **`DialogoModal`** — el `div` con `role="dialog"` suma `@onkeydown:stopPropagation="true"`: el Esc de un
   diálogo anidado (la pregunta de cambios sin guardar dentro del modal de edición) lo atiende sólo ese diálogo y no
   reabre la pregunta desde el de afuera (AC 28). Los diálogos sin anidar no cambian.
8. **A2 (`Especialidades.razor`)**:
   - En `<Acciones>`: `<MenuDeAcciones Registro="@fila.Nombre" Visible="EditarDe(fila)" Opciones="OpcionesDe(fila)"
     AlElegirDesde="r => origen = r" />`, con `EditarDe(fila) => new("Editar", EventCallback.Factory.Create(this,
     () => AbrirEdicion(fila)))`.
   - La validación del nombre de `FormularioAlta` pasa a una clase base privada `FormularioNombre` (mismos atributos
     y textos); `FormularioEdicion : FormularioNombre` suma `Id` y `NombreVisto`. El alta no cambia de
     comportamiento.
   - Un segundo bloque `@if (edicion is { } formularioEdicion)` (variable local, como el alta: ver el comentario del
     bloque `@if (alta is …)`, hoy línea 39) con `<DialogoModal Titulo="Editar especialidad" AlCerrar="EscEnEdicionAsync" Origen="origen">` y
     `<FormularioAbm @ref="formularioDeEdicion" Model="formularioEdicion" AlGuardar="RenombrarAsync"
     AlCancelar="CerrarEdicion">` con el mismo `CampoDeFormulario` "Nombre". `EscEnEdicionAsync` llama
     `formularioDeEdicion.PedirCancelarAsync()` (AC 28 y 29).
   - `RenombrarAsync`: actor del circuito → `ConCatalogoAsync(c => c.RenombrarAsync(actor, id, visto, nuevo, ct))`;
     si devuelve `true`, "Especialidad actualizada"; si `false`, "No había cambios para guardar"; cierra y recarga la
     tabla con el mismo `estado` (si la fila sale del filtro o de la página, la consulta paginada ya devuelve la última
     página con datos por `ConsultaDeListado.PaginaPosible`, 3.3 AC 81: AC 38). Ante `ConflictoException`: `BuscarAsync(id)` y, si existe, `formularioEdicion.NombreVisto = actual.Nombre`;
     después `throw;` para que `FormularioAbm` muestre el mensaje (AC 26).
9. **`app.css`** — dentro del `@media (max-width: 767.98px)` existente:
   `.tarjeta-abm .table-actions .btn, .tarjeta-abm .menu-de-acciones__boton { min-width: 2.75rem; min-height: 2.75rem; }`
   y `.tarjeta-abm .menu-de-acciones__opcion { min-height: 2.75rem; }` (AC 30).
10. **Documentación** — contrato técnico (AC 32) y FR-12 del PRD (AC 33).

### Decisiones de esta story (con su porqué)

- **Renombrar también las inactivas:** la guía pone "Editar" en toda fila de A2; con el filtro "Inactivas" o
  "Todas" también se ven. El mensaje de la 3.1 para el nombre de una desactivada ("Reactivala desde la lista") ya
  supone que se ven y se operan.
- **El detalle de auditoría con la forma de `medico.editado`:** A5 (9.1) muestra igual cualquier edición.
- **"Especialidad actualizada"** y no "renombrada": la guía usa "Médico actualizado" para guardar una edición.
- **Título del modal "Editar especialidad":** el verbo de la acción (guía) y el patrón del alta ("Nueva
  especialidad").
- **No se relee el nombre al abrir el modal:** se usa el de la fila, que es justamente el `nombreVisto`.
- **El "Editar" vive en `MenuDeAcciones`** y no en un componente nuevo: la guía pone juntas "una acción frecuente
  visible y el resto en ⋯", y así el foco de vuelta sale del botón dibujado (la fila se dibuja dos veces).

### Archivos a crear/modificar

| Accion | Archivo |
|--------|---------|
| Crear | `src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/<marca>_ConcurrenciaEnEspecialidades.cs` y `.Designer.cs` (migración sin columnas) |
| Modificar | `src/HospitalTurnos.Dominio/Catalogo/Especialidad.cs` (`Renombrar`) |
| Modificar | `src/HospitalTurnos.Dominio/Auditoria/AccionesAuditoria.cs` (`EspecialidadRenombrada`) |
| Modificar | `src/HospitalTurnos.Aplicacion/Catalogo/ICatalogoEspecialidades.cs` (`RenombrarAsync`) |
| Modificar | `src/HospitalTurnos.Infraestructura/Catalogo/CatalogoEspecialidadesEnBase.cs` (`RenombrarAsync`, concurrencia en `CambiarEstadoAsync`) |
| Modificar | `src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/EspecialidadConfiguracion.cs` (`xmin`) |
| Modificar | `src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/HospitalDbContextModelSnapshot.cs` (lo genera EF) |
| Modificar | `src/HospitalTurnos.Web/Components/Comunes/MenuDeAcciones.razor` (`Visible`) |
| Modificar | `src/HospitalTurnos.Web/Components/Comunes/FormularioAbm.razor` (`PedirCancelarAsync` público, que no hace nada mientras guarda) |
| Modificar | `src/HospitalTurnos.Web/Components/Comunes/DialogoModal.razor` (el Esc no sube a un diálogo de afuera) |
| Modificar | `src/HospitalTurnos.Web/Components/Personal/Administracion/Especialidades.razor` (Editar y modal de edición) |
| Modificar | `src/HospitalTurnos.Web/wwwroot/app.css` (área táctil de las tarjetas) |
| Modificar | `docs/stories/00-contrato-tecnico.md`, `docs/02-prd.md` |
| Modificar | Pruebas: `EspecialidadTests`, `CatalogoEspecialidadesTests`, `EspecialidadesInteractivaTests`, `ComponentesComunesTests`, `HojaDeEstilosTests`; los dobles de `EspecialidadesPaginaTests`, `MedicosInteractivaTests`, `BaseInteractivaTests` |

### Lo que NO hay que hacer

- **NO poner "Editar" dentro del "⋯"** — la guía lo quiere visible; en el "⋯" van sólo Desactivar o Reactivar.
- **NO tocar la fila de `medicos` al renombrar** — el nombre se lee por join; copiarlo duplicaría el dato.
- **NO reescribir registros de auditoría viejos** — la tabla rechaza `UPDATE` (1.3) y la historia es la que fue.
- **NO dejar que EF agregue una columna `xmin`** — PostgreSQL la rechaza; la migración queda vacía.
- **NO traducir la concurrencia a un mensaje genérico de error** — sería "Algo falló", y el usuario puede resolverlo.
- **NO crear un componente nuevo para el botón "Editar"** — se extiende `MenuDeAcciones` (AC 18).
- **NO cambiar el Esc del modal de alta ni el de A4** — fuera de alcance (pregunta S-41).
- **NO inyectar los casos de uso en la página** — siempre por `AlcancePorOperacion` (3.3, AC 94).
- **NO usar `HttpContext` ni `FijarEstadoEnPagina`** — la página es interactiva (ADR-08).
- **NO dejar especialidades con nombres de prueba en la base de demo** — la matriz en vivo renombra una propia con
  su marca y la deja con un nombre verosímil, o usa una existente y le devuelve su nombre (guía, Datos de prueba).

### Como verificar que funciona

```bash
# 1. Pruebas (Docker para Testcontainers), por proyecto
dotnet test tests/HospitalTurnos.Dominio.Tests
dotnet test tests/HospitalTurnos.Aplicacion.Tests
dotnet test tests/HospitalTurnos.Web.Tests
# Esperado: 0 fallas; dotnet test --filter "Story=3.4" lista T1–T29 (más los casos de cada [Theory])

# 2. La migración no agrega columnas (esperado: 0 líneas)
grep -n "AddColumn\|DropColumn" src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/*_ConcurrenciaEnEspecialidades.cs

# 3. Compose y matriz en vivo (Chrome, teclado y 375 px): AC 20–31, 34–36 y 38; AC 26 con dos pestañas
docker compose up -d --build

# 4. Auditoría del renombre en la base de demo
docker compose exec db psql -U postgres -d hospital_turnos -c \
  "select accion, detalle from registros_auditoria where accion = 'especialidad.renombrada' order by id desc limit 3;"
```

### Salvaguarda de BD

1. `select column_name from information_schema.columns where table_name = 'especialidades';` — esperado: `id`,
   `nombre`, `nombre_normalizado`, `activa` (sin `version`).
2. `select has_table_privilege('turnos_app', 'especialidades', 'UPDATE'), has_table_privilege('turnos_app',
   'especialidades', 'DELETE');` — esperado `t`, `f`. Si `UPDATE` da `f`, DETENERSE: renombrar lo necesita.
3. Si cualquier asunción no coincide → DETENERSE → corregir la story primero.

### References

- [Source: docs/ux/guia-ux-ui-abm.md] — §Aplicación (Especialidades 1–5), §Patrón de alta y edición, §Estados,
  §Accesibilidad y responsive, §Microcopy.
- [Source: docs/ux/analisis-brechas-abm.md] — B-2.10 y decisión D3.
- [Source: docs/stories/3-1-especialidades.md] — AC 1, 2, 6, 7, 12, 13 (reglas y mensajes que se reusan).
- [Source: docs/stories/3-3-alinear-abm-con-guia-ux.md] — AC 2–4, 20, 30, 33–38, 89, 94 y la línea "Fuera de esta
  story".
- [Source: docs/stories/3-2-medicos.md] — Dev Notes M-6 (médicos sin concurrencia).
- [Source: docs/stories/4-2-propuesta-agenda.md] — AC 19 (concurrencia con `xmin`).
- [Source: docs/04-arquitectura.md#51-componentes-de-interfaz-reutilizables] — ADR-08 y reglas de uso.
- [Source: docs/stories/00-contrato-tecnico.md] — §Auditoría, §Pruebas.

## Preguntas para el docente

> **Decidido por el docente (2026-10-23):** todas las recomendadas (S-40 a S-44), aceptadas en bloque.

**S-40 — Concurrencia: ¿qué pasa si dos administradores renombran la misma especialidad a la vez?** El encargo pide
"concurrencia optimista (xmin) como A1/A2", pero A1 y A2 **no** la tienen: en médicos se aceptó "gana el último"
(3.2, M-6). Sólo `agendas_medico` usa `xmin` (4.2).
- **(a, recomendada)** `xmin` en `especialidades` + el nombre que vio la pantalla: el segundo ve "Otro
  administrador cambió esta especialidad mientras la editabas…" y guarda de nuevo sobre lo último. Es el patrón de
  la 4.2, nadie pisa a otro sin enterarse y la migración no agrega columnas. Costo: también desactivar y reactivar
  pasan a detectar la carrera (AC 14).
- (b) "Gana el último", como médicos: más simple, pero un renombre pisa a otro sin aviso (la auditoría sí deja
  los dos).
- (c) Sólo comparar el nombre visto, sin `xmin`: cubre las dos pestañas pero deja una ventana de milisegundos.

**S-41 — ¿Esc en el modal con cambios pregunta como "Cancelar"?** La guía pide avisar al salir con cambios; hoy el
Esc del modal de alta (3.3) cierra sin preguntar.
- **(a, recomendada)** Sí en el modal de edición (esta story), con `FormularioAbm.PedirCancelarAsync`; el alta de
  A2 y A4 quedan como están y se alinean en otra story. Cumple la guía sin tocar pantallas ya aprobadas.
- (b) Sí en todos los modales ahora: más parejo, pero cambia A2 y A4 de la 3.3 dentro de esta story.
- (c) No: Esc cierra siempre. Choca con el "Debe" de la guía.

**S-42 — ¿Se actualiza FR-12 del PRD para decir "renombrarlas"?** FR-12 sólo dice "crear y desactivar".
- **(a, recomendada)** Sí, en el mismo PR: el PRD queda diciendo lo que el sistema hace.
- (b) No: alcanza con la decisión D3 y el plan.

**S-43 — Área táctil de 44 × 44 px en las tarjetas.** La guía lo pide ("Debe"), pero hoy los botones miden 40 px
de alto en todo el sistema (3.3).
- **(a, recomendada)** Subir a 44 px sólo los botones de acción de las tarjetas bajo 768 px (CSS): cumple la guía
  en celular y mejora también A1 y A4 sin cambiar el escritorio.
- (b) Dejarlo como la 3.3 y corregirlo en una story de accesibilidad para todo el sistema.

**S-44 — ¿El modal avisa cuántos médicos verán el nombre nuevo?** La guía pide mostrar dependencias antes de
**desactivar**, no al editar.
- **(a, recomendada)** No: renombrar no es destructivo ni cambia lo que pueden elegir; el modal queda de un campo.
- (b) Sí, como ayuda bajo el campo ("12 médicos la tienen asignada: van a ver el nombre nuevo."): suma una
  consulta (`ContarMedicosAsync`) al abrir.

## Change Log

| Fecha | Sesión | Cambio | Por qué |
|---|---|---|---|
| 2026-10-23 | 18 | Story escrita en borrador | Brecha B-2.10 (decisión D3), sumada al S3 por el docente |
| 2026-10-23 | 18 | Decisiones S-40 a S-44: todas las recomendadas, aceptadas en bloque; `[SUPUESTO]` pasa a `[DECIDIDO]` | Decisión del docente ([revisión](revision-sesion-18.md)) |
| 2026-10-23 | 18 | Revisión adversarial previa (SADV-01, A-1): AC 28 suma que Esc sobre la pregunta de cambios sin guardar equivale a "Seguir editando" y no la reabre (`DialogoModal` corta la propagación); caso (c) en T26 | El Esc subía al modal de afuera y volvía a abrir la pregunta |
| 2026-10-23 | 18 | Revisión adversarial previa (SADV-02, A-2): AC 38 nuevo (la fila renombrada sale del filtro o de la página); casos (c) y (d) en T22 | No decía qué se ve cuando el renombre saca la fila de la vista |
| 2026-10-23 | 18 | Revisión adversarial previa (SADV-03, A-3): AC 19 suma que mientras dice "Guardando…" Cancelar y Esc no hacen nada; T25 lo prueba con su doble | Sólo estaba cubierto el doble "Guardar" |
| 2026-10-23 | 18 | Revisión adversarial previa (SADV-04, A-4): AC 30 y T28 incluyen las opciones del "⋯" de la tarjeta (`.menu-de-acciones__opcion`) en los 44 px | Las opciones del menú abierto medían 40 px |

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
