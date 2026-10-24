---
epic: 9
story: "9.2"
title: "Historial por registro en la ficha del médico (sección Historial de la guía UX/UI)"
status: ready-for-dev
sprint: 6
priority: "P2"
approach: "[ADAPT]"
depends_on: ["1.3", "3.2", "3.3", "3.5", "9.1"]
followup_of:
  - id: "3.3"
    type: "extension"
    summary: "La ficha del médico suma el Historial que la 3.3 dejó fuera por la decisión D3 (parte de B-4.2)"
followup_reason: cambio-de-requisito
runtime_not_verifiable:
  - ac: 16
    reason: "El índice vive en la base: la app en ejecución no lo muestra. Lo prueba T10 contra pg_indexes y los permisos de la tabla."
    covered_by: T10
  - ac: 17
    reason: "Que la consulta no escriba nada es una propiedad del código: en la app no hay nada que mirar. T11 cuenta las filas de auditoría antes y después de leer."
    covered_by: T11
  - ac: 22
    reason: "La app siempre escribe un detalle bien formado; un detalle nulo, roto o con un campo desconocido no se puede provocar desde la pantalla. Lo cubre T4 sobre el traductor."
    covered_by: T4
  - ac: 23
    reason: "Una acción que el traductor no conoce no se puede producir desde la app de hoy. Lo cubren T5 y la trampa T6."
    covered_by: T5
  - ac: 24
    reason: "Especialidades y usuarios no se borran: una referencia que no se resuelve no se puede provocar desde la pantalla. Lo cubren T3 y T9."
    covered_by: T9
  - ac: 28
    reason: "Permisos y disparadores de la tabla de auditoría: propiedad de la base, no de la pantalla."
    covered_by: R1
  - ac: 29
    reason: "La CI corre en GitHub sobre el PR, que todavía no existe: se verifica con su corrida antes del merge."
    covered_by: R2
files_to_create:
  - "src/HospitalTurnos.Aplicacion/Auditoria/IConsultaHistorial.cs"
  - "src/HospitalTurnos.Aplicacion/Auditoria/DescripcionDeAuditoria.cs"
  - "src/HospitalTurnos.Infraestructura/Auditoria/ConsultaHistorialEnBase.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/AAAAMMDDHHMMSS_IndiceAuditoriaPorEntidad.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/AAAAMMDDHHMMSS_IndiceAuditoriaPorEntidad.Designer.cs"
  - "src/HospitalTurnos.Web/Components/Comunes/HistorialDeRegistro.razor"
  - "tests/HospitalTurnos.Aplicacion.Tests/Auditoria/DescripcionDeAuditoriaTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Auditoria/ConsultaHistorialTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Catalogo/HistorialMedicoTests.cs"
files_to_modify:
  - "src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/RegistroAuditoriaConfiguracion.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/HospitalDbContextModelSnapshot.cs"
  - "src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/FichaMedico.razor"
  - "src/HospitalTurnos.Web/wwwroot/app.css"
  - "docs/stories/00-contrato-tecnico.md"
  - "docs/04-arquitectura.md"
---

# Story 9.2: Historial por registro en la ficha del médico (sección Historial de la guía UX/UI)

Status: ready-for-dev

## Story

Como administrador del hospital,
quiero ver en la ficha de un médico quién lo dio de alta, quién cambió sus datos, cuándo y qué cambió,
en texto llano,
para responder un reclamo o una duda sin tener que pedir una consulta a la base ni leer la auditoría completa.

**Origen.** La guía UX/UI pide organizar el detalle "en pestañas o secciones (Datos, Agenda, Usuario,
Historial)" (guía, "Detalle frente a edición", **Conviene**) y en su §Aplicación dibuja la ficha del médico con
"Datos · Agenda · Usuario del sistema · Historial". El análisis de brechas marcó el Historial como capacidad
nueva (B-4.2, impacto N) y el docente lo mandó al backlog (decisión D3, 2026-10-17); la 3.3 lo dejó fuera de
forma explícita ("historial por registro (parte de B-4.2)"). Entra al S6 por decisión del docente (sesión 17).

**Qué NO es.** No es la consulta general de auditoría (A5, story 9.1, FR-34): acá se ve la historia de **un**
registro, desde su ficha, sin filtros. No cambia ninguna regla de negocio ni escribe nada.

## Acceptance Criteria

> Convenciones: "administrador" = el administrador inicial del Compose o uno creado en la prueba. Los textos
> entre comillas son exactos. "Registro de auditoría" = una fila de `registros_auditoria` (story 1.3).

### A. La sección Historial en la ficha del médico

1. **Given** un administrador en la ficha de un médico (`/personal/admin/medicos/{id}`), en modo lectura
   **When** se dibuja **Then** debajo del pie "Creado por … · Modificado por …" aparece una sección con el
   título `<h2>` "Historial" y la tabla de movimientos de ese médico. Es una **sección**, no una pestaña
   **[DECIDIDO S-61]**, y es la única ficha que la tiene en esta story **[DECIDIDO S-62]**.
2. **Given** la sección Historial con movimientos **When** se dibuja **Then** es un `<section>` con
   `aria-labelledby` apuntando a su `<h2>`, y la tabla tiene, en este orden, las columnas "Cuándo", "Quién",
   "Qué hizo" y "Detalle", con `<th scope="col">`; ninguna columna se puede ordenar (sin ▲▼ ni `aria-sort`) y
   no hay columna de acciones.
3. **Given** un registro escrito con el reloj falso en 2026-11-16 08:05 UTC **When** se dibuja la fila
   **Then** "Cuándo" dice "16/11/2026 05:05" (hora del hospital, `Formatos.FechaHora` con la zona del
   `TimeProvider` inyectado, igual que `PieDeTrazabilidad`); nunca una fecha relativa ni sin año.
4. **Given** un registro **When** se dibuja la fila **Then** "Quién" muestra el `Quien` guardado en el registro
   (el nombre que tenía la persona cuando actuó, aunque después haya cambiado) y debajo, como texto de ayuda,
   su `Rol` ("Administrador").
5. **Given** un registro de cada acción del médico **When** se dibuja la fila **Then** "Qué hizo" dice, con el
   microcopy de éxito de la guía (verbo en pasado, como las notificaciones de la 3.3, AC 20):
   - `medico.creado` → "Médico creado"
   - `medico.editado` → "Médico actualizado"
   - **sólo si la 3.5 está en `done`** (lo verifica la Task 1): sus acciones de desactivar y reactivar, con el
     nombre que tengan en `AccionesAuditoria`, → "Médico desactivado" y "Médico reactivado"
     **[DECIDIDO S-65]**, sin líneas de detalle en esta story. Mientras la 3.5 no esté hecha, esas acciones no
     existen y este punto no se implementa ni se prueba (la Task 1 corta antes).
6. **Given** un registro `medico.creado` **When** se dibuja **Then** "Detalle" es una lista con, en este orden:
   "Nombre: Pablo", "Apellido: Ríos", "Matrícula: MP 1234", "Especialidad: Cardiología" y "Usuario del
   sistema: Sin usuario" (o, con usuario vinculado, "Usuario del sistema: Pablo Ríos (pablo.rios)"). Las
   etiquetas son las mismas del formulario de la ficha (`CamposMedico`).
7. **Given** un registro `medico.editado` **When** se dibuja **Then** "Detalle" tiene una línea por campo que
   cambió, con el formato "<Etiqueta>: <antes> → <después>", en el orden Nombre, Apellido, Matrícula,
   Especialidad, Usuario del sistema; la especialidad y el usuario se muestran por su **nombre** (no por su
   id), y un usuario vacío se muestra como "Sin usuario" (por ejemplo, "Usuario del sistema: Sin usuario →
   Pablo Ríos (pablo.rios)").
8. **Given** cualquier movimiento **When** se dibuja la sección **Then** su texto no contiene JSON (`{`, `}`,
   `"antes"`), ni códigos de acción (`medico.`), ni nombres de campo técnicos (`especialidadId`,
   `usuarioPersonalId`), ni ids numéricos o GUID.
9. **Given** un médico creado con el reloj falso a las 10:00 y editado a las 10:05 y a las 10:10 (hora del
   hospital; el reloj se adelanta con `Advance`, nunca esperando) **When** se abre su ficha **Then** el
   Historial muestra primero el de las 10:10, después el de las 10:05 y al final "Médico creado": **el más
   reciente primero**, por orden de inserción (`Id` descendente) **[DECIDIDO S-64]**.
10. **Given** un médico con 26 o más movimientos **When** se dibuja el Historial **Then** debajo de la tabla
    aparece el `Paginador` con "Mostrando 1–25 de N" y 25 / 50 / 100 por página; "Siguiente" muestra los
    siguientes; con 25 o menos no aparece. La página vive en el estado de la sección y no se escribe en la URL
    de la ficha **[DECIDIDO S-64]**.
11. **Given** la sección Historial **When** se dibuja **Then** es de **sólo lectura** (FR-35): no tiene botones
    ni enlaces salvo los del `Paginador`, ni `MenuDeAcciones`, ni nada que edite, desactive o borre un
    movimiento.
12. **Given** un médico A con registros propios, otro médico B con los suyos y una agenda de A con registros
    `propuesta-agenda.*` o `agenda.corregida` (`EntidadTipo = "AgendaMedico"`) **When** se abre la ficha de A
    **Then** el Historial muestra **sólo** los registros con `EntidadTipo = "Medico"` y `EntidadId` = el id de A;
    ni los de B ni los de la agenda **[DECIDIDO S-63]**.
13. **Given** la ficha en lectura **When** el administrador pulsa "Editar" **Then** el Historial no se ve
    (forma parte de la vista de lectura); **When** guarda un cambio de matrícula **Then** vuelve a la lectura
    con "Médico actualizado" y el Historial ya muestra arriba de todo la fila nueva con "Matrícula: <antes> →
    <después>", sin recargar la página; **When** cancela **Then** el Historial vuelve igual que antes.
14. **Given** la ficha (que ya exige `Roles.Administrador`, 3.2/3.3) **When** la pide alguien sin sesión **Then**
    `302` al ingreso; **When** la pide un recepcionista o un médico **Then** `403` con "No tenés permiso para
    entrar a esta sección.". El Historial lo ve sólo el administrador: es el único rol que consulta la
    auditoría (PRD §7.8, tabla de permisos) y el único que entra a la ficha.

### B. La consulta y el rendimiento

15. **Given** `IConsultaHistorial.ListarAsync(entidadTipo, entidadId, pagina, tamano, ct)` **When** se ejecuta
    **Then** devuelve `PaginaDeResultados<EntradaDeHistorial>(Items, Total)` con el filtro, el orden, el
    `Skip`/`Take` y el `CountAsync` hechos en la base; una página mayor que la última devuelve la última con
    datos (como `ConsultaDeListado.PaginaPosible`). Cada `EntradaDeHistorial` trae `Id`, `Cuando`, `Quien`,
    `Rol`, `QueHizo` y `Detalles` (las líneas ya en texto llano). La define **esta** story, junto con el
    traductor `DescripcionDeAuditoria` **[DECIDIDO S-60]**; si al implementar la 9.1 ya trae una consulta por
    registro o un traductor equivalentes, se usan esos (Task 1).
16. **Given** la migración nueva `IndiceAuditoriaPorEntidad` **When** se aplica **Then** existe el índice
    `ix_registros_auditoria_entidad` sobre `registros_auditoria (entidad_tipo, entidad_id, id)`; la migración
    sólo crea el índice (su `Down` lo borra) y no cambia permisos ni disparadores. Hoy la tabla no tiene ningún
    índice salvo la clave primaria (verificado en `20261016010013_AgregarRegistroAuditoria.cs` y en el
    snapshot).
17. **Given** la consulta del Historial **When** se ejecuta **Then** sólo lee: `AsNoTracking`, sin
    `IAuditoria.Registrar` y sin `SaveChangesAsync`. Leer el historial no es un cambio y no se audita (FR-33
    lista sólo cambios); no hay concurrencia optimista que cuidar porque no escribe.

### C. Accesibilidad, responsive y guía

18. **Given** una ventana de menos de 768 px (375 px de ancho) **When** se dibuja el Historial **Then** cada
    movimiento es una tarjeta (la `Tarjeta` de `TablaAbm`) con Cuándo, Quién, Qué hizo y Detalle, sin scroll
    horizontal; los botones del `Paginador` se operan con Tab y Enter y muestran el foco de 3 px de la 3.3.
19. La sección **cumple la guía UX/UI** (`docs/ux/guia-ux-ui-abm.md`) **y usa los componentes de la arquitectura
    §5.1** (`TablaAbm` con su `Paginador`, `Formatos` y las clases de `app.css`). Se verifican estos puntos del
    checklist del final de la guía, cada uno con su prueba:
    - fechas `dd/mm/aaaa` con año (guía, "Tabla" y "Formatos") → T12;
    - textos según la tabla de microcopy (éxito con verbo en pasado) → T1, T2, T12;
    - estado vacío propio (no hay filtros, así que no existe "sin resultados") → T17;
    - error de carga con "Reintentar", nunca una pantalla en blanco → T18;
    - paginación con "Mostrando x–y de z" desde 26 → T15;
    - vista en celular como tarjetas → T19;
    - operable con teclado y foco visible → matriz en vivo (Task 8) y la regla de foco de la 3.3 (T9 de la 3.3, en R1);
    - sin `style="…"` ni hexadecimales → T7 y T8 de la 3.3, que recorren todo `Components/` y `wwwroot/` (en R1);
    - visible sólo para los roles autorizados → T20.

    **Excepción nombrada:** "Filtros, orden y página reflejados en la URL" no aplica: el Historial no tiene
    filtros ni orden elegible y la página no va a la URL (S-64). Tampoco aplican los puntos de listado
    (título con total, botón "Nuevo …", buscador) ni los de formulario y baja: es un detalle de sólo lectura.

### Modos de falla

20. **Given** un médico sin ningún registro de auditoría (por ejemplo, cargado por un script) **When** se abre
    su ficha **Then** el Historial muestra el vacío de `TablaAbm` con "Todavía no hay movimientos registrados
    para este médico." y **ningún** botón de acción (es de sólo lectura), y no aparece el `Paginador`.
21. **Given** que la consulta del Historial falla (base caída o error inesperado) **When** se dibuja la ficha
    **Then** dentro de la sección aparece "No pudimos cargar la lista." con "Reintentar" (el error de carga de
    `TablaAbm`), el detalle va al registro técnico (NFR-09), y Datos, Usuario del sistema y el pie siguen
    visibles; **When** se pulsa "Reintentar" **Then** se vuelve a consultar.
22. **Given** un registro con `Detalle` nulo, con JSON mal formado, con un campo que el traductor no conoce, o
    con JSON **bien formado pero de otra forma** (un arreglo en vez de un objeto, `cambios` como texto, un
    `antes` numérico, un `especialidadId` como texto que no es número) **When** se describe **Then** no lanza:
    muestra su "Qué hizo" y, de detalle, sólo las líneas que entiende; el traductor lee cada propiedad con
    `TryGetProperty` y mira su `ValueKind` antes de leerla (nunca `GetProperty`/`GetString` sin comprobar). Un
    campo desconocido de `medico.editado` se muestra como "Otro dato: <antes> → <después>" y nunca con su nombre
    técnico ni como JSON.
23. **Given** un registro de un tipo de acción que el traductor no conoce **When** se describe **Then** "Qué
    hizo" dice "Cambio registrado" y no tiene líneas de detalle. **Y** toda constante de `AccionesAuditoria`
    cuyo valor empiece con `medico.` tiene un texto propio (una prueba recorre las constantes por reflexión: si
    la 3.5 o una story posterior agrega una acción del médico sin texto, la suite se pone roja).
24. **Given** un `especialidadId` o un `usuarioPersonalId` del detalle que no existe en la base **When** se
    describe **Then** se muestra "Especialidad (no encontrada)" o "Usuario (no encontrado)", nunca el id.
25. **Given** un médico o un administrador con caracteres de marcado en el nombre (`Ríos <b>x</b>`) **When** se
    dibuja el Historial **Then** se ve el texto literal: nada se dibuja como HTML (sin `MarkupString`).
26. **Given** la ficha de un médico que no existe **When** se abre **Then** sigue mostrando "No existe el médico
    pedido." (3.3, AC 87) y **no** se consulta el Historial.

### No-regresión

27. `PieDeTrazabilidad`, `IConsultaTrazabilidad` y `ConsultaTrazabilidadEnBase` no cambian; las pruebas de la
    ficha de la 3.2, la 3.3 (T40, T46, T55) y la 4.4 pasan sin modificarse.
28. Después de la migración, `turnos_app` sigue teniendo **sólo** `SELECT` e `INSERT` sobre
    `registros_auditoria` y los disparadores `auditoria_sin_update_ni_delete` y `auditoria_sin_truncate` siguen
    activos (1.3, T6 a T8, sin cambios).
29. La suite completa y la CI quedan en verde, con cobertura de líneas ≥ 80 % en Web y sin baja en Aplicación.

### Tests (23 verificaciones)

Todas llevan `[Trait("Story", "9.2")]` y un `[Trait("AC", "n")]` por criterio cubierto.

| # | Test | AC | Nivel | Escenario | Resultado esperado |
|---|------|----|-------|-----------|--------------------|
| T1 | `Describir_MedicoCreado_DevuelveMedicoCreadoYSusDatos` | 5, 6 | unit | `medico.creado` con el detalle real de `GestionMedicosEnBase.CrearAsync`, especialidad 7 = "Cardiología", sin usuario | `QueHizo` = "Médico creado"; las cinco líneas del AC 6 en orden |
| T2 | `Describir_MedicoEditado_UnaLineaPorCampoConAntesYDespues` | 5, 7 | unit | `medico.editado` con cambios de matrícula y especialidad (7 → 9, "Cardiología" → "Clínica médica") | "Médico actualizado"; "Matrícula: MP 1234 → MP 5678" y "Especialidad: Cardiología → Clínica médica", en ese orden |
| T3 | `Describir_EspecialidadYUsuario_UsanNombresONoEncontrado` [Theory] | 6, 7, 24 | unit | Usuario de null a un GUID conocido; especialidad y usuario con ids que no están en los nombres | "Usuario del sistema: Sin usuario → Pablo Ríos (pablo.rios)"; "Especialidad (no encontrada)", "Usuario (no encontrado)"; ningún id en el texto |
| T4 | `Describir_DetalleNuloMalformadoOCampoDesconocido_NoLanzaNiMuestraJson` [Theory] | 8, 22 | unit | Detalle `null`; `"{no es json"`; `medico.editado` con un campo `"telefono"`; JSON válido de otra forma: `[]`, `{"cambios":"x"}`, `{"cambios":{"matricula":{"antes":5,"despues":null}}}`, `medico.creado` con `"especialidadId":"siete"` | No lanza en ningún caso; "Qué hizo" presente; el campo desconocido sale como "Otro dato: a → b"; las formas inesperadas dan sólo las líneas que se entienden; ninguna línea contiene `{` ni el nombre técnico |
| T5 | `Describir_AccionDesconocida_DiceCambioRegistrado` | 23 | unit | Acción `"medico.algo-nuevo"` que el traductor no conoce | "Cambio registrado", 0 líneas de detalle |
| T6 | `Describir_TodaAccionDeMedico_TieneTextoPropio` | 5, 23 | unit | Reflexión sobre las constantes `public const string` de `AccionesAuditoria` que empiezan con `medico.` | Para cada una, `QueHizo` ≠ "Cambio registrado" |
| T7 | `Historial_SoloDelRegistro_MasRecientePrimero` | 9, 12, 15 | integration | Testcontainers; reloj falso a las 10:00, 10:05 y 10:10: crea el médico A y lo edita dos veces; crea el médico B; inserta un `agenda.corregida` con `EntidadTipo = "AgendaMedico"` y `medicoId` de A en el detalle | 3 entradas de A, de la más reciente a la más vieja, con esos `Cuando`; ninguna de B ni de la agenda; `Total` = 3 |
| T8 | `Historial_PaginaEnLaBase_YUltimaSiSePasa` | 10, 15 | integration | Médico con 30 registros (alta + 29 ediciones); página 2 de 25; página 9 de 25 | Página 2: 5 ítems, `Total` 30; página 9 devuelve los mismos 5 |
| T9 | `Historial_ResuelveEspecialidadYUsuario_DesdeLaBase` | 7, 24 | integration | Edición que cambia a una especialidad que después se desactiva y vincula un usuario médico | La línea dice el nombre de la especialidad y "Nombre Completo (usuario)"; con un id inexistente forzado en un registro insertado a mano, "(no encontrada)" |
| T10 | `Migracion_IndicePorEntidad_SinTocarPermisosNiDisparadores` | 16, 28 | integration | `pg_indexes` de `registros_auditoria`; `information_schema.role_table_grants` de `turnos_app`; `pg_trigger` (todo como `CadenaDuenio`) | Existe `ix_registros_auditoria_entidad` con `(entidad_tipo, entidad_id, id)`; privilegios = {`INSERT`, `SELECT`}; los dos disparadores |
| T11 | `ConsultaHistorial_SoloLee_NoAgregaRegistros` | 17 | integration | Cuenta las filas de `registros_auditoria` del médico, lista su historial tres veces y vuelve a contar | Mismo conteo; el contexto no tiene entidades en seguimiento después de leer |
| T12 | `Ficha_SeccionHistorial_ColumnasFechaQuienYTextos` | 1, 2, 3, 4, 5, 6, 19 | integration | bUnit con la app (`ContextoDeComponente.ConLaApp`) y reloj falso propio del contexto (Córdoba); médico creado con `admin1` y un `medico.creado` más insertado con un `AuditoriaEnBase` armado con un reloj propio en 2026-11-16 08:05 UTC (ver Given/Fixture) | `<section aria-labelledby>` con `<h2>Historial</h2>` después del pie; en `thead`, Cuándo · Quién · Qué hizo · Detalle sin `aria-sort`; la fila de `tbody tr` de ese registro dice "16/11/2026 05:05", su `Quien`, "Administrador", "Médico creado" y las cinco líneas |
| T13 | `Ficha_Historial_SinJsonNiCodigos_YTextoLiteral` | 8, 25 | integration | Médico "Pablo" "Ríos <b>x</b>" creado y editado (especialidad y usuario) | El texto de la sección (tabla y `.tarjeta-abm`) no contiene `{`, `medico.`, `especialidadId`, `usuarioPersonalId` ni un GUID; "Ríos <b>x</b>" aparece literal y no hay elemento `<b>` en la sección |
| T14 | `Ficha_Guardar_ApareceArribaElNuevoMovimiento_YEnEdicionNoSeVe` | 7, 9, 13 | integration | Ficha en lectura; "Editar" (sin sección Historial); cambia la matrícula y guarda; otra vez "Editar" y "Cancelar" | En edición no hay `<h2>Historial</h2>`; tras guardar, la primera `tbody tr` (la de mayor `Id`, leído de la base para la aserción; el orden no depende del reloj) es "Médico actualizado" con "Matrícula: <antes> → <después>"; tras cancelar, las mismas `tbody tr` en el mismo orden |
| T15 | `Ficha_Historial_PaginaDesde26` [Theory] | 10 | integration | Médico con 25 y con 26 movimientos | Con 25, sin `.paginador`; con 26, "Mostrando 1–25 de 26" y "Siguiente" deja 1 `tbody tr` (y 1 `.tarjeta-abm`); la URL de la ficha no cambia |
| T16 | `Ficha_Historial_SoloLectura` | 11 | integration | Médico con 3 movimientos | Dentro de la sección: 0 `button` y 0 `a` (no aparece el `Paginador`), 0 elementos `.menu-de-acciones`, 0 `th.table-actions` |
| T17 | `Ficha_Historial_VacioSinAccion` | 20 | integration | Médico insertado directo en la base, sin registros de auditoría | "Todavía no hay movimientos registrados para este médico."; 0 botones en la sección; sin `Paginador` |
| T18 | `Ficha_Historial_ErrorDeCarga_ReintentarYElRestoVisible` | 21 | integration | `IConsultaHistorial` reemplazada por un doble que lanza la primera vez y responde la segunda | "No pudimos cargar la lista." y "Reintentar" en la sección; Datos y el pie visibles; el registro en memoria tiene la excepción; tras "Reintentar", las filas |
| T19 | `Ficha_Historial_EnCelularComoTarjetas` | 18 | integration | Marcado de la sección con 2 movimientos | 2 `tbody tr` y 2 `.tarjeta-abm` (la tabla y las tarjetas se dibujan juntas y el CSS elige según el ancho); cada `.tarjeta-abm` tiene Cuándo, Quién, Qué hizo y Detalle; ningún ancho fijo en línea |
| T20 | `Ficha_SinSesionOConOtroRol_SiguenEn302O403` [Theory] | 14 | integration | `PersonalWebApplicationFactory`: GET a la ficha sin cookie, como recepcionista y como médico | `302` al ingreso; `403` con "No tenés permiso para entrar a esta sección." |
| T21 | `FichaInexistente_NoConsultaElHistorial` | 26 | integration | `IConsultaHistorial` reemplazada por un doble que cuenta las llamadas; ficha de un id inexistente | "No existe el médico pedido."; 0 llamadas al historial |
| R1 | Pruebas de la ficha, de la auditoría y de la hoja de estilos sin cambios | 19, 27, 28 | integration | `dotnet test` de Aplicación (1.3 T6–T8, 3.3 T46) y Web (3.2, 3.3 T7, T8, T9, T40 y T55, 4.4), con `HistorialDeRegistro.razor` y el `app.css` nuevos ya en el árbol | 0 fallas, sin archivos de prueba existentes modificados; T7–T9 de la 3.3 recorren también lo nuevo |
| R2 | CI y cobertura | 29 | integration | CI del PR | Verde; líneas ≥ 80 % en Web |

> 23 verificaciones: T1–T21 más R1–R2. Los casos de cada `[Theory]` se suman al cerrar. No hay E2E con
> Playwright (el proyecto `HospitalTurnos.E2E` llega con la 10.1, como en la 3.3); en su lugar, la **matriz en
> vivo** en Chrome contra el Compose recorre un paso por AC de las secciones A y C y los modos de falla 20, 21,
> 25 y 26 (el 21 apagando el servicio `db` con la ficha abierta, como en la 3.3).

#### Given/Fixture

- **T1–T6** — `DescripcionDeAuditoria.Describir(accion, detalleJson, nombres)` sin base. Detalles copiados del
  formato real: `medico.creado` →
  `{"nombre":"Pablo","apellido":"Ríos","matricula":"MP 1234","especialidadId":7,"usuarioPersonalId":null}`;
  `medico.editado` → `{"cambios":{"matricula":{"antes":"MP 1234","despues":"MP 5678"},"especialidadId":{"antes":"7","despues":"9"}}}`
  (en `editado`, los ids van **como texto**: salen de `Medico.CambiarDatos`).
- **T7–T11** — `fixture.Proveedor(s => s.AddSingleton<TimeProvider>(reloj))` con
  `DatosDeAgenda.Reloj(2026, 11, 16, 10, 0)` y `reloj.Advance(TimeSpan.FromMinutes(5))` entre acciones;
  administradores con `UsuariosDePrueba.CrearAsync`; especialidad con `EspecialidadPropiaAsync`, como
  `ConsultaTrazabilidadTests`. El registro "a mano" de T7 y T9 se inserta con `IAuditoria.Registrar` +
  `SaveChangesAsync` (la app sólo puede insertar), con su propia marca en `EntidadId`.
- **T12–T21** — `BunitContext` con `ContextoDeComponente.ConLaApp(app)` y `ComoAdmin1Async`, como
  `MedicosInteractivaTests`. **`app.Reloj` no se mueve** (convención del repo, `EstadoDeAgendaEnA1HttpTests`:
  "app.Reloj no se adelanta"). Cada prueba usa un `FakeTimeProvider` propio con `SetLocalTimeZone` en Córdoba,
  registrado en `Services` del contexto bUnit **después** de `ConLaApp` (gana el último): la página dibuja la
  hora con él. Para un registro con instante fijo (T12), se inserta en un alcance de la app con
  `new AuditoriaEnBase(contexto, relojPropio).Registrar(...)` + `SaveChangesAsync` (la app sólo puede insertar)
  y `relojPropio` en 2026-11-16 08:05 UTC. Las aserciones de orden (T14, T15) usan el `Id` leído de la base, no
  la hora. Los selectores se acotan a `section … tbody tr` o a `.tarjeta-abm`: `TablaAbm` dibuja las dos cosas a
  la vez. Los dobles de T18 y T21 se registran en `Services` del contexto bUnit antes del fallback de la app.

## Tasks / Subtasks

- [ ] [SEQ] Task 1: Salvaguarda de estado y de BD (AC: todos)
  - [ ] Confirmar que la 3.5 está en `done` y leer en `AccionesAuditoria.cs` los **nombres reales** de sus
        constantes de desactivar y reactivar; si no son `medico.desactivado` / `medico.reactivado`, usar los
        reales (el texto del AC 5 no cambia). Si la 3.5 no está hecha, DETENERSE y avisar.
  - [ ] Confirmar el estado de la 9.1 (va antes en el plan): si ya trae una consulta de auditoría por registro
        o un traductor de acciones a texto, **se usa eso** y se ajusta esta story (AC 15, Tasks 2 y 3, archivos)
        en vez de duplicar (S-60). Si no los trae, se crean como dice esta story.
  - [ ] Si la 4.4 ya agregó la sección "Agenda vigente" a `FichaMedico.razor`, el Historial va igual después
        del pie (AC 1); si la 3.5 agregó una sección "Desactivar médico", también queda antes del Historial.
  - [ ] Correr la "Salvaguarda de BD" de abajo.
- [ ] [SEQ] Task 2: Traductor `DescripcionDeAuditoria` con sus pruebas primero (AC 5–8, 22–24). T1–T6.
- [ ] [SEQ] Task 3: `IConsultaHistorial` + `ConsultaHistorialEnBase` + registro en `ServiciosDeInfraestructura`
      (AC 9, 12, 15, 17). T7, T8, T9, T11.
- [ ] [PAR] Task 4: Índice en `RegistroAuditoriaConfiguracion` y migración `IndiceAuditoriaPorEntidad` (AC 16,
      28). T10.
- [ ] [SEQ] Task 5: Componente `HistorialDeRegistro` sobre `TablaAbm` (con `Entidad` y `Columnas`) y su lugar en
      `FichaMedico` (AC 1–4, 10, 11, 13, 18–21, 25, 26). T12–T19, T21.
- [ ] [SEQ] Task 6: Contrato técnico (§Auditoría: `IConsultaHistorial`, `DescripcionDeAuditoria`, el índice) y
      arquitectura §5.1 (fila de `HistorialDeRegistro`).
- [ ] [SEQ] Task 7: Permisos y suite completa (AC 14, 27, 29). T20, R1, R2.
- [ ] [SEQ] Task 8: Matriz en vivo en Chrome contra el Compose, también a 375 px y con teclado (AC 1–14, 18–21,
      25, 26).

### Task Dependencies

T1 → T2 → T3 → T5 → T6 → T7 → T8. T4 en paralelo con T2–T3 (no depende del traductor). Ninguna parte de la
pantalla se arma antes de que el traductor y la consulta tengan sus pruebas en verde.

## Dev Notes

### Que existe hoy

#### En hospital-turnos (verificado 2026-10-23 contra `cierre/sesion-17`, `4df6e51`)

- **`src/HospitalTurnos.Dominio/Auditoria/RegistroAuditoria.cs`** — `Id` (long, orden de inserción), `Cuando`
  (UTC), `UsuarioId`, `Quien`, `Rol`, `Canal`, `Accion`, `EntidadTipo`, `EntidadId`, `Detalle` (JSON como texto,
  columna `jsonb`). Sin setters públicos.
- **`src/HospitalTurnos.Dominio/Auditoria/AccionesAuditoria.cs`** — del médico hoy hay **dos**:
  `MedicoCreado = "medico.creado"` y `MedicoEditado = "medico.editado"` (story 3.2). La 3.5 agrega las de
  desactivar y reactivar.
- **`EntidadTipo` en uso** (constantes privadas, no `<entidad>` en kebab-case): `"Medico"`
  (`GestionMedicosEnBase.cs:38`), `"Especialidad"` (`CatalogoEspecialidadesEnBase.cs:27`), `"UsuarioPersonal"`
  (`GestionUsuariosPersonalIdentity.cs:29`, `IngresoPersonalIdentity.cs:23`), `"AgendaMedico"`
  (`PropuestaAgendaEnBase.cs:35`; la 4.3 y la 4.4 lo usan igual). La ficha ya consulta con `"Medico"`
  (`FichaMedico.razor`, `CargarAsync`).
- **Detalle que escribe el médico** (`src/HospitalTurnos.Infraestructura/Catalogo/GestionMedicosEnBase.cs`):
  - `CrearAsync` (~167): `new { nombre, apellido, matricula, especialidadId, usuarioPersonalId }` →
    `especialidadId` número, `usuarioPersonalId` GUID o `null`.
  - `EditarAsync` (~207): `new { cambios = cambios.ToDictionary(c => c.Campo, c => new { antes, despues }) }`;
    las claves salen de `Medico.CambiarDatos` (`Dominio/Catalogo/Medico.cs:37-56`) en el orden `nombre`,
    `apellido`, `matricula`, `especialidadId`, `usuarioPersonalId`, y los valores son **texto** (los ids con
    `ToString` invariante; usuario `null` = sin usuario). Se serializa con `JsonSerializerOptions.Web`
    (`AuditoriaEnBase.cs:16`): las claves del diccionario no se convierten.
- **`src/HospitalTurnos.Aplicacion/Auditoria/IConsultaTrazabilidad.cs`** y
  **`src/HospitalTurnos.Infraestructura/Auditoria/ConsultaTrazabilidadEnBase.cs`** (3.3, AC 46) — la única
  lectura de auditoría por entidad: primero y último por `Id`. Es el patrón de esta story y **no se toca**.
- **`RegistroAuditoriaConfiguracion.cs`** — sin `HasIndex`. La migración `20261016010013_AgregarRegistroAuditoria.cs`
  sólo crea la clave primaria; el snapshot (la entidad `RegistroAuditoria` en `HospitalDbContextModelSnapshot.cs`, hoy líneas 136-197; la migración nueva lo regenera) tampoco tiene índices.
  Hoy el `WHERE entidad_tipo = … AND entidad_id = …` de la trazabilidad recorre la tabla entera.
- **`src/HospitalTurnos.Web/Components/Personal/Administracion/FichaMedico.razor`** — `[Authorize(Roles =
  Administrador)]`, interactiva sin prerender. En lectura: `VistaDeDetalle` con las secciones "Datos" y
  "Usuario del sistema", el botón "Editar" y en `Pie` el `PieDeTrazabilidad`. `Input is null` = lectura. Al
  guardar, `GuardarAsync` pone `Input = null` y vuelve a `CargarAsync`: la vista de lectura se dibuja de nuevo
  (el Historial, al estar dentro, se vuelve a crear y relee: AC 13 sin código extra). `UsuarioVinculado` arma
  `"{NombreCompleto} ({Usuario})"`: el Historial usa el mismo formato.
- **Componentes de la 3.3** (`Components/Comunes/`): `TablaAbm<TFila>` (columnas `ColumnaAbm`, `Consultar`,
  `Clave` para recargar, `TextoVacio`/`AccionVacio`, `Tarjeta`, error con "Reintentar", y ya dibuja el
  `Paginador` con `Pagina`/`Tamano`/`PaginaCambiada`/`TamanoCambiado`); `Paginador` (aparece con
  `Total > 25`); `Formatos.FechaHora(instante, zona)`; `PieDeTrazabilidad` (usa `Reloj.LocalTimeZone`).
- **`AlcancePorOperacion`** (`Web/Personal/AlcancePorOperacion.cs`) — toda consulta de una página interactiva
  pasa por acá (3.3, AC 94).
- **Permisos de la tabla** — `turnos_app` tiene `SELECT, INSERT` (migración de la 1.3): leer no requiere
  tocar nada.
- **No existe:** consulta paginada de auditoría por entidad, traductor de acciones a texto, índice por entidad,
  sección Historial, pestañas en la ficha. **La 9.1 no está escrita** (`docs/sprint-status.yaml`: `backlog`):
  de ella sólo se sabe lo que dicen FR-34 (filtros por DNI, médico y fechas), UX A5 (tabla de sólo lectura con
  fecha y hora, quién, rol, canal, acción y detalle) y el contrato ("La 9.1 filtra por la entidad").

### Patron de referencia verificado

**Patrón**: lectura de auditoría por `EntidadTipo` + `EntidadId` sin seguimiento, paginada en la base con
`CountAsync` + `Skip`/`Take`, pedida desde la página por `AlcancePorOperacion` y dibujada con `TablaAbm`.

| Proyecto | Ancla | Qué se verificó y qué se toma |
|---|---|---|
| hospital-turnos (3.3) | `src/HospitalTurnos.Infraestructura/Auditoria/ConsultaTrazabilidadEnBase.cs:15-21` | `AsNoTracking().Where(r => r.EntidadTipo == … && r.EntidadId == …)` y orden por `Id`. Se toma igual, con `OrderByDescending(r => r.Id)`. |
| hospital-turnos (3.3) | `src/HospitalTurnos.Infraestructura/Catalogo/GestionMedicosEnBase.cs:66-85` | `ListarPaginadoAsync`: `CountAsync` (75) y `Skip(consulta.Saltear(total)).Take(consulta.Tamano)` (85). Se toma igual; la página se ajusta con la misma cuenta que `ConsultaDeListado.PaginaPosible`. |
| hospital-turnos (3.2) | `src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/MedicoConfiguracion.cs:13,23` | Índice con nombre fijo en una constante (`HasDatabaseName`). Se toma para `ix_registros_auditoria_entidad`. |
| hospital-turnos (3.3) | `src/HospitalTurnos.Web/Components/Comunes/PieDeTrazabilidad.razor` | Fecha y hora con `Formatos.FechaHora(…, Reloj.LocalTimeZone)` e `@inject TimeProvider Reloj`. Se toma igual. |
| hospital-turnos (3.3) | `tests/HospitalTurnos.Aplicacion.Tests/Auditoria/ConsultaTrazabilidadTests.cs` | Prueba de lectura de auditoría con actores reales y `fixture.Proveedor()`. Se toma para T7–T11, con el reloj falso registrado. |
| hospital-turnos (3.3) | `tests/HospitalTurnos.Web.Tests/Catalogo/MedicosInteractivaTests.cs` | bUnit de la ficha con `ConLaApp` y `ComoAdmin1Async`. Se toma para T12–T21. |

El proyecto guía de la materia (`changomas-testing-csharp`) no tiene auditoría ni Blazor (verificado en la 1.3
y la 2.1); por eso la referencia es el propio repo.

### Que hay que hacer (la delta)

1. **Aplicación** — `IConsultaHistorial` y sus registros; `DescripcionDeAuditoria`, una función pura que pasa
   de (acción, detalle JSON, nombres resueltos) a `QueHizo` + líneas de detalle.
2. **Infraestructura** — `ConsultaHistorialEnBase`: la página de registros, las referencias que hay que
   nombrar (especialidades y usuarios de **esa** página, en dos consultas `WHERE id IN (…)`), y el traductor.
   El índice y su migración.
3. **Web** — `HistorialDeRegistro` (común, para que otra ficha lo use cuando el docente lo decida) y su
   lugar en `FichaMedico`, después de `</VistaDeDetalle>` y dentro del bloque de lectura.
4. **Docs** — contrato técnico y arquitectura §5.1.

### Implementacion concreta

```csharp
// src/HospitalTurnos.Aplicacion/Auditoria/IConsultaHistorial.cs
public interface IConsultaHistorial
{
    /// <summary>Los movimientos de un registro, el más reciente primero, paginados en la base. Sólo lee (ADR-07).</summary>
    Task<PaginaDeResultados<EntradaDeHistorial>> ListarAsync(string entidadTipo, string entidadId, int pagina,
        int tamano, CancellationToken ct);
}

public sealed record EntradaDeHistorial(long Id, DateTimeOffset Cuando, string Quien, string Rol, string QueHizo,
    IReadOnlyList<string> Detalles);

// src/HospitalTurnos.Aplicacion/Auditoria/DescripcionDeAuditoria.cs
public sealed record NombresDeReferencia(IReadOnlyDictionary<long, string> Especialidades,
    IReadOnlyDictionary<Guid, string> Usuarios);   // usuario: "Nombre Completo (usuario)"

public static class DescripcionDeAuditoria
{
    public const string AccionDesconocida = "Cambio registrado";
    // Qué ids hay que nombrar en un detalle (la consulta los junta para toda la página).
    public static (IReadOnlyCollection<long> Especialidades, IReadOnlyCollection<Guid> Usuarios) Referencias(
        string accion, string? detalle);
    public static (string QueHizo, IReadOnlyList<string> Detalles) Describir(string accion, string? detalle,
        NombresDeReferencia nombres);
}
```

- Textos de "Qué hizo": un diccionario `AccionesAuditoria.X → texto` (AC 5); el resto, `AccionDesconocida`.
- Etiquetas: `nombre` → "Nombre", `apellido` → "Apellido", `matricula` → "Matrícula", `especialidadId` →
  "Especialidad", `usuarioPersonalId` → "Usuario del sistema"; otra clave → "Otro dato" (AC 22).
- JSON con `JsonDocument.Parse` dentro de un `try` que atrapa sólo `JsonException`: un detalle roto da 0
  líneas (AC 22). Un JSON válido de otra forma tampoco lanza: la raíz se lee sólo si `ValueKind == Object`, cada
  propiedad con `TryGetProperty`, y cada valor según su `ValueKind` (`String` → `GetString`; `Number` →
  `TryGetInt64`; `Null` → vacío; otro → la línea se omite). Nunca `GetProperty` ni `GetString` sin comprobar
  (AC 22). Un id que no se puede convertir o que no está en `nombres` → "(no encontrada/o)" (AC 24).
- `ConsultaHistorialEnBase`: `var filas = contexto.RegistrosAuditoria.AsNoTracking().Where(…)`;
  `total = await filas.CountAsync(ct)`; página ajustada `Math.Clamp(pagina, 1, max(1, ceil(total / tamano)))`;
  `OrderByDescending(r => r.Id).Skip(…).Take(tamano).Select(r => new { r.Id, r.Cuando, r.Quien, r.Rol, r.Accion, r.Detalle })`;
  luego `Referencias` de cada fila, `contexto.Especialidades.Where(e => ids.Contains(e.Id))` (todas, también
  las inactivas) y `contexto.Users.Where(u => guids.Contains(u.Id))` → `$"{u.NombreCompleto} ({u.UserName})"`.
- Índice: `registro.HasIndex(r => new { r.EntidadTipo, r.EntidadId, r.Id }).HasDatabaseName(IndicePorEntidad)`
  con `public const string IndicePorEntidad = "ix_registros_auditoria_entidad";`. Crear un índice no dispara
  los disparadores de la 1.3 (sólo `UPDATE`, `DELETE` y `TRUNCATE`) y lo corre el dueño, como toda migración.
- `HistorialDeRegistro.razor`: parámetros `EntidadTipo`, `EntidadId`, `TextoVacio`; estado `pagina`/`tamano`;
  `TablaAbm<EntradaDeHistorial>` con `Clave = (pagina, tamano)`, `Consultar = ct => Operaciones.EjecutarAsync<IConsultaHistorial, …>(c => c.ListarAsync(…, ct))`,
  `Entidad = "movimientos"` (lo usa el texto de "sin resultados" de `TablaAbm`; acá no hay filtros, así que
  `HayFiltros = false` y sólo se ve `TextoVacio`), `Columnas` = cuatro `ColumnaAbm` ("Cuándo", "Quién", "Qué
  hizo", "Detalle") sin `Orden`, sin `Acciones` y con `Tarjeta` (las cuatro cosas en bloque). `TablaAbm` dibuja
  la tabla **y** las tarjetas a la vez (el CSS muestra una u otra según el ancho): las pruebas buscan en
  `tbody tr` o en `.tarjeta-abm`, nunca "la primera fila" a secas. Detalle como `<ul class="historial__detalle">`.
  Clases nuevas en `app.css` sólo con variables de `tokens.css`.
- En `FichaMedico`: `<HistorialDeRegistro EntidadTipo="Medico" EntidadId="@entidadId" TextoVacio="Todavía no hay movimientos registrados para este médico." />`
  sólo dentro de `@if (Input is null && ficha is { } leida)`, después de `</VistaDeDetalle>`. Con `ficha is null`
  no se dibuja (AC 26).

### Archivos a crear/modificar

| Accion | Archivo |
|--------|---------|
| Crear | `src/HospitalTurnos.Aplicacion/Auditoria/IConsultaHistorial.cs` (puerto y `EntradaDeHistorial`) |
| Crear | `src/HospitalTurnos.Aplicacion/Auditoria/DescripcionDeAuditoria.cs` (traductor y `NombresDeReferencia`) |
| Crear | `src/HospitalTurnos.Infraestructura/Auditoria/ConsultaHistorialEnBase.cs` |
| Crear | `src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/AAAAMMDDHHMMSS_IndiceAuditoriaPorEntidad.cs` (+ `.Designer.cs`) |
| Crear | `src/HospitalTurnos.Web/Components/Comunes/HistorialDeRegistro.razor` |
| Crear | `tests/HospitalTurnos.Aplicacion.Tests/Auditoria/DescripcionDeAuditoriaTests.cs` (T1–T6) |
| Crear | `tests/HospitalTurnos.Aplicacion.Tests/Auditoria/ConsultaHistorialTests.cs` (T7–T11) |
| Crear | `tests/HospitalTurnos.Web.Tests/Catalogo/HistorialMedicoTests.cs` (T12–T21) |
| Modificar | `src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/RegistroAuditoriaConfiguracion.cs` (`HasIndex` con nombre) |
| Modificar | `src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/HospitalDbContextModelSnapshot.cs` (lo regenera la migración) |
| Modificar | `src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs` (`AddScoped<IConsultaHistorial, ConsultaHistorialEnBase>()`, junto a la trazabilidad) |
| Modificar | `src/HospitalTurnos.Web/Components/Personal/Administracion/FichaMedico.razor` (la sección después del detalle) |
| Modificar | `src/HospitalTurnos.Web/wwwroot/app.css` (`.historial__detalle` y lo que haga falta, sin hexadecimales) |
| Modificar | `docs/stories/00-contrato-tecnico.md` (§Auditoría: `IConsultaHistorial`, `DescripcionDeAuditoria`, índice `ix_registros_auditoria_entidad`) |
| Modificar | `docs/04-arquitectura.md` (§5.1: fila `HistorialDeRegistro`) |

### Lo que NO hay que hacer

- **NO tocar `IConsultaTrazabilidad` ni `PieDeTrazabilidad`** — el pie ya funciona y tiene sus pruebas (3.3).
- **NO mostrar el `Detalle` crudo ni con `MarkupString`** — la guía pide texto llano y un nombre con `<` se
  dibujaría como HTML (AC 8, 25).
- **NO armar pestañas** ni mover Datos y Usuario del sistema a pestañas — es la alternativa B de S-61; no hay
  componente de pestañas en §5.1 y la guía no define su patrón.
- **NO consultar `Detalle->>'medicoId'`** para traer las agendas — S-63; sin índice de expresión sería lento y
  la historia de la agenda ya la muestra A5 (9.1).
- **NO escribir en `registros_auditoria`** ni deshabilitar disparadores en la migración — sólo `CREATE INDEX`
  (1.3, ADR-07).
- **NO usar `DateTime.Now` ni esperar tiempo real en las pruebas** — la hora sale del `TimeProvider` y las
  pruebas mueven el reloj falso (contrato §Hora).
- **NO inyectar `IConsultaHistorial` directo en la página** — se pide a `AlcancePorOperacion` (3.3, AC 94).
- **NO crear la consulta de A5 ni sus filtros** — es la 9.1.

### Como verificar que funciona

```bash
cd ~/SourceCode/hospital-turnos
# Migración (como en la 1.4)
dotnet ef migrations add IndiceAuditoriaPorEntidad \
  --project src/HospitalTurnos.Infraestructura \
  --startup-project src/HospitalTurnos.Web \
  --output-dir Persistencia/Migraciones

# Pruebas (Docker para Testcontainers), por proyecto
dotnet test tests/HospitalTurnos.Aplicacion.Tests --filter "Story=9.2"   # T1–T11
dotnet test tests/HospitalTurnos.Web.Tests --filter "Story=9.2"          # T12–T21
dotnet test tests/HospitalTurnos.Aplicacion.Tests && dotnet test tests/HospitalTurnos.Web.Tests   # R1

# En vivo
docker compose up -d --build
# Como administrador: Médicos → un médico → bajar hasta "Historial"; editar la matrícula y guardar;
# ver la fila nueva arriba. Repetir a 375 px y sólo con teclado.
docker compose exec db psql -U postgres -d hospital_turnos -c \
  "select indexname, indexdef from pg_indexes where tablename = 'registros_auditoria';"
# Esperado: pk_registros_auditoria e ix_registros_auditoria_entidad (entidad_tipo, entidad_id, id)
```

### Salvaguarda de BD

1. `select entidad_tipo, accion, count(*) from registros_auditoria where entidad_tipo = 'Medico' group by 1, 2;`
   — confirma `EntidadTipo = 'Medico'` y las acciones del médico (con la 3.5, también las suyas).
2. `select detalle from registros_auditoria where accion = 'medico.editado' order by id desc limit 3;` —
   confirma la forma `{"cambios": {"<campo>": {"antes": …, "despues": …}}}` con ids como texto.
3. `\d registros_auditoria` — sólo la clave primaria. Si ya hay un índice por entidad (lo creó la 9.1),
   DETENERSE: no se crea otro.
4. `\dp registros_auditoria` — `turnos_app` con `SELECT` e `INSERT` (a, r). Si no, DETENERSE.

### References

- [Source: docs/ux/guia-ux-ui-abm.md#patrón-estándar-de-alta-y-edición] — "Detalle frente a edición" (pestañas o secciones, Historial).
- [Source: docs/ux/guia-ux-ui-abm.md#aplicación-rediseño-de-las-tres-pantallas] — ficha del médico con Historial.
- [Source: docs/ux/guia-ux-ui-abm.md#microcopy-y-nomenclatura] — éxito con verbo en pasado, fechas dd/mm/aaaa.
- [Source: docs/ux/analisis-brechas-abm.md] — tabla 4 (B-4.2) y decisión D3.
- [Source: docs/02-prd.md#78-auditoría-y-permisos] — FR-33, FR-34, FR-35 y la tabla de permisos.
- [Source: docs/04-arquitectura.md#75-auditoría-que-no-se-puede-alterar] — ADR-07; §5.1 componentes.
- [Source: docs/stories/00-contrato-tecnico.md#auditoría--r-12-adr-07] — entidad, acciones, permisos.
- [Source: docs/stories/3-3-alinear-abm-con-guia-ux.md] — AC 40, 46, 87, 94 y "Fuera de esta story".

## Preguntas para el docente

> **Decidido por el docente (2026-10-23):** todas las recomendadas (S-60 a S-65), aceptadas en bloque; S-60 con el ajuste de la revisión.

**S-60 — ¿Quién define la consulta de lectura por entidad, la 9.2 o la 9.1?** La 9.1 no está escrita y el plan
la pone antes.
- **(a) Recomendada y decidida: la define la 9.2** (`IConsultaHistorial` y el traductor a texto llano), sin
  afirmar nada de lo que hará la 9.1. La 9.1 sigue antes en el plan y en `depends_on`; si al implementar la
  9.2 la 9.1 ya trae algo equivalente, se usa eso (Task 1). Al escribir la 9.1 se le pasa esta definición.
- (b) Esperar a escribir la 9.1 y que ella defina una consulta general con filtro por entidad; la 9.2 la usa.
  Cuesta reescribir la 9.2 después.
- (c) Sacar la 9.1 de `depends_on`: la 9.2 no necesita nada de ella. Más honesto, pero cambia el plan.

**S-61 — ¿Pestaña o sección?** La guía dice "pestañas **o** secciones" (Conviene); su §Aplicación dibuja pestañas.
- **(a) Recomendada: una sección "Historial" al final de la vista de lectura**, después del pie. No toca Datos
  ni Usuario ni lo que agregue la 4.4, y no pide un componente nuevo de pestañas.
- (b) Pestañas Datos · Agenda · Usuario del sistema · Historial, con un componente nuevo (ARIA `tablist`,
  flechas). Es el dibujo de la guía, pero la guía no define el patrón y cambia la ficha de la 3.3 y la 4.4.
- (c) Una página aparte (`/personal/admin/medicos/{id}/historial`) enlazada desde la ficha.

**S-62 — ¿En qué fichas va?** Sólo el médico tiene ficha; A2 (Especialidades) y A4 (Usuarios) no tienen detalle.
- **(a) Recomendada: sólo la ficha del médico** (la única que la guía dibuja con Historial). El componente
  queda común para sumarlo después.
- (b) También Especialidades y Usuarios, con "Ver historial" en el menú "⋯" que abre un `DialogoModal`. Suma
  textos para 9 acciones más (las de la 3.4 incluidas) y dos pantallas.
- (c) Médico ahora y las otras dos como story nueva del backlog.

**S-63 — ¿El historial del médico incluye lo de su agenda?** Propuestas y correcciones se auditan con
`EntidadTipo = "AgendaMedico"` (4.2, 4.3, 4.4), no con el médico.
- **(a) Recomendada: no.** El historial es "por registro" y la agenda es otro registro; su historia se ve en A5
  (9.1) y, cuando exista, en la sección Agenda.
- (b) Sí, buscando `medicoId` dentro del detalle. Pide un índice de expresión sobre `jsonb` y textos para cuatro
  acciones de agenda.

**S-64 — Orden y página.** La guía no dice cómo se ordena un historial.
- **(a) Recomendada: el más reciente primero, 25 por página con el `Paginador`, y la página no va a la URL**
  (es una sección de la ficha, no un listado; la URL de la ficha sigue siendo la del médico).
- (b) Igual, pero con la página en la URL (`?historial=2`), como pide la guía para los listados (Conviene).
- (c) El más viejo primero (orden cronológico de lectura).

**S-65 — Las acciones de la 3.5.** Se escribió con los nombres que siguen el contrato (`medico.desactivado`,
`medico.reactivado`) y los textos "Médico desactivado" / "Médico reactivado".
- **(a) Recomendada: la 9.2 los toma de `AccionesAuditoria` al implementar** (Task 1 lo verifica) y la trampa T6
  obliga a que toda acción `medico.*` tenga texto. Sin detalle para esas dos.
- (b) Mostrar también el detalle que guarde la 3.5 (por ejemplo, un motivo), cuando la 3.5 lo defina.

## Change Log

| Fecha | Sesión | Cambio | Por qué |
|---|---|---|---|
| 2026-10-23 | 18 | Story escrita como borrador | Decisión D3 del análisis de brechas; entra al S6 (sesión 17) |
| 2026-10-23 | 18 | Decisiones S-60 a S-65: todas las recomendadas, en bloque; `[SUPUESTO]` → `[DECIDIDO]`. S-60 con el ajuste de la revisión: el AC 15 ya no afirma qué hará la 9.1 y la Task 1 usa lo que traiga la 9.1 si ya existe | Decisión del docente (`docs/stories/revision-sesion-18.md`) |
| 2026-10-23 | 18 | Revisión adversarial previa (SADV-W1): el AC 5 condiciona los textos de desactivar y reactivar a la 3.5 en `done` | A-9: daba por existentes acciones que trae la 3.5 |
| 2026-10-23 | 18 | Revisión adversarial previa (SADV-C3): el AC 22 y T4 cubren JSON bien formado de otra forma (`TryGetProperty` + `ValueKind`) | A-10: el traductor sólo atrapaba JSON roto |
| 2026-10-23 | 18 | Revisión adversarial previa (SADV-W1): reloj falso propio del contexto bUnit; `app.Reloj` no se mueve; el registro con hora fija va con un `AuditoriaEnBase` propio; T14 y T15 ordenan por `Id` | A-11: las pruebas web movían el reloj compartido de la fábrica |
| 2026-10-23 | 18 | Revisión adversarial previa (SADV-W1): selectores acotados a `tbody tr` y `.tarjeta-abm` (T12–T16, T19); `HistorialDeRegistro` pasa `Entidad` y `Columnas` | A-12: `TablaAbm` dibuja tabla y tarjetas a la vez |
| 2026-10-23 | 18 | Revisión adversarial previa (SADV-W2): el AC 19 enumera los puntos de la guía con su prueba y nombra la excepción de la URL; R1 suma T7–T9 de la 3.3 y cubre el AC 19 | A-13: "cumple la guía en lo que aplica" no era observable |

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
