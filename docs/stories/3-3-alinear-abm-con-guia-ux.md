---
epic: 3
story: "3.3"
title: "Alinear pantallas ABM existentes con la guía UX/UI (A1 Médicos, A2 Especialidades, A4 Usuarios del personal y marco del personal)"
status: ready-for-dev
sprint: 3
priority: "P0"
approach: "[ADAPT]"
depends_on: ["2.2", "2.3", "3.1", "3.2", "4.1"]
followup_of:
  - id: "3.1"
    type: "extension"
    summary: "A2 pasa al estándar de la guía UX/UI: modal de alta, filtros, menú de acciones y confirmación de baja"
  - id: "3.2"
    type: "extension"
    summary: "A1 pasa al estándar: listado con filtros, orden, paginación y badges; ficha en modo lectura"
  - id: "2.2"
    type: "extension"
    summary: "A4 pasa al estándar: modal de alta, badges, menú de acciones y confirmaciones"
followup_reason: cambio-de-requisito
files_to_create:
  - "src/HospitalTurnos.Web/wwwroot/css/tokens.css"
  - "src/HospitalTurnos.Web/Components/Comunes/MarcoPersonal.razor"
  - "src/HospitalTurnos.Web/Components/Comunes/MenuDeUsuario.razor"
  - "src/HospitalTurnos.Web/Components/Comunes/MigasDePan.razor"
  - "src/HospitalTurnos.Web/Components/Comunes/PaginaAbm.razor"
  - "src/HospitalTurnos.Web/Components/Comunes/BarraDeFiltros.razor"
  - "src/HospitalTurnos.Web/Components/Comunes/FiltroDeEstado.razor"
  - "src/HospitalTurnos.Web/Components/Comunes/EstadoDeListado.cs"
  - "src/HospitalTurnos.Web/Components/Comunes/TablaAbm.razor"
  - "src/HospitalTurnos.Web/Components/Comunes/ColumnaAbm.cs"
  - "src/HospitalTurnos.Web/Components/Comunes/MenuDeAcciones.razor"
  - "src/HospitalTurnos.Web/Components/Comunes/Paginador.razor"
  - "src/HospitalTurnos.Web/Components/Comunes/InsigniaDeEstado.razor"
  - "src/HospitalTurnos.Web/Components/Comunes/FormularioAbm.razor"
  - "src/HospitalTurnos.Web/Components/Comunes/CampoDeFormulario.razor"
  - "src/HospitalTurnos.Web/Components/Comunes/SelectConBusqueda.razor"
  - "src/HospitalTurnos.Web/Components/Comunes/VistaDeDetalle.razor"
  - "src/HospitalTurnos.Web/Components/Comunes/PieDeTrazabilidad.razor"
  - "src/HospitalTurnos.Web/Components/Comunes/DialogoModal.razor"
  - "src/HospitalTurnos.Web/Components/Comunes/DialogoDeConfirmacion.razor"
  - "src/HospitalTurnos.Web/Components/Comunes/Notificaciones.cs"
  - "src/HospitalTurnos.Web/Components/Comunes/ZonaDeNotificaciones.razor"
  - "src/HospitalTurnos.Web/Components/Comunes/LimiteDeErrores.razor"
  - "src/HospitalTurnos.Web/Components/Comunes/Formatos.cs"
  - "src/HospitalTurnos.Web/Personal/RevalidacionDeCircuito.cs"
  - "src/HospitalTurnos.Web/Personal/ActorDelCircuito.cs"
  - "src/HospitalTurnos.Aplicacion/Comunes/ConsultaDeListado.cs"
  - "src/HospitalTurnos.Aplicacion/Auditoria/IConsultaTrazabilidad.cs"
  - "src/HospitalTurnos.Infraestructura/Auditoria/ConsultaTrazabilidadEnBase.cs"
  - "tests/HospitalTurnos.Web.Tests/Comunes/BaseInteractivaTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Comunes/HojaDeEstilosTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Comunes/MarcoPersonalTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Comunes/ComponentesComunesTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Catalogo/EspecialidadesInteractivaTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Catalogo/MedicosInteractivaTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Personal/UsuariosInteractivaTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Catalogo/ListadosPaginadosTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Auditoria/ConsultaTrazabilidadTests.cs"
files_to_modify:
  - "src/HospitalTurnos.Web/wwwroot/app.css"
  - "src/HospitalTurnos.Web/Components/App.razor"
  - "src/HospitalTurnos.Web/Components/Personal/LayoutPersonal.razor"
  - "src/HospitalTurnos.Web/Components/Personal/MenuLateral.razor"
  - "src/HospitalTurnos.Web/Personal/RutasPersonal.cs"
  - "src/HospitalTurnos.Web/Program.cs"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/Especialidades.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/Medicos.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/TablaMedicos.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/MedicoNuevo.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/FichaMedico.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/CamposMedico.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/UsuariosPersonal.razor"
  - "src/HospitalTurnos.Aplicacion/Catalogo/ICatalogoEspecialidades.cs"
  - "src/HospitalTurnos.Aplicacion/Catalogo/IGestionMedicos.cs"
  - "src/HospitalTurnos.Aplicacion/Personal/IGestionUsuariosPersonal.cs"
  - "src/HospitalTurnos.Infraestructura/Catalogo/CatalogoEspecialidadesEnBase.cs"
  - "src/HospitalTurnos.Infraestructura/Catalogo/GestionMedicosEnBase.cs"
  - "src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs"
  - "tests/HospitalTurnos.Web.Tests/Catalogo/EspecialidadesHttpTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Catalogo/EspecialidadesPaginaTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Catalogo/MedicosHttpTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Catalogo/EstadoDeAgendaEnA1HttpTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Catalogo/TablaMedicosTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Personal/UsuariosPersonalHttpTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Personal/MenuLateralTests.cs"
---

# Story 3.3: Alinear pantallas ABM existentes con la guía UX/UI

Status: ready-for-dev

## Story

Como administrador del hospital,
quiero que Médicos, Especialidades y Usuarios del personal se usen igual, según el estándar de la guía
UX/UI,
para aprender las pantallas una sola vez, ver siempre qué pasó y no desactivar nada por accidente.

**Por qué ahora.** El 2026-10-17 el docente fijó la [guía UX/UI](../ux/guia-ux-ui-abm.md) como estándar
obligatorio ([UX §0](../03-ux.md)). Las stories 2.2, 3.1 y 3.2 están cerradas y no se reabren: esta story
corrige sus pantallas. Cada criterio sale de una fila del [análisis de brechas](../ux/analisis-brechas-abm.md);
la tabla de trazabilidad está al final de los criterios.

**Orden obligatorio:** primero los componentes comunes (con sus pruebas), después la migración de las
pantallas. **No cambia ninguna regla de negocio**: validaciones, permisos, auditoría y mensajes de regla
quedan como están.

## Acceptance Criteria

> Convenciones: "B-n.m" cita la fila m de la tabla n del análisis de brechas ("B-4bis" = A4). Los textos
> entre comillas son exactos. "Administrador" = el administrador inicial del Compose o uno creado en la prueba.

### A. Base interactiva — ADR-08, arquitectura §5.1 (decisión D1)

1. **Given** las páginas A1 (listado, nuevo, ficha), A2 y A4 **When** se revisa su declaración **Then**
   llevan `@rendermode @(new InteractiveServerRenderMode(prerender: false))`; el ingreso (S0), el cambio de
   contraseña, `Salir`, el layout y el menú no declaran modo (siguen en SSR estático).
2. **Given** una acción que audita (crear, desactivar, editar…) hecha desde una página interactiva **When**
   termina **Then** la fila de auditoría tiene el mismo usuario, nombre, rol, canal `Personal` y acción que
   escribe hoy; el actor sale del `AuthenticationState` en cascada (`ActorDelCircuito`), no de `HttpContext`.
3. **Given** una excepción esperable del dominio (`ManejadorGlobalDeErrores.EsEsperable` → true) **When**
   ocurre en una acción de una página interactiva **Then** su mensaje aparece, **con el mismo texto que hoy**,
   en el resumen del formulario o del diálogo, y no se fija ningún código HTTP.
4. **Given** una excepción no esperable en una página interactiva **When** ocurre **Then** `LimiteDeErrores`
   (un `ErrorBoundary`) muestra "Algo falló. Probá de nuevo en unos minutos.", sin detalle técnico, y el
   detalle queda en el registro técnico (NFR-09).
5. **Given** un administrador con una página interactiva abierta **When** otro administrador lo desactiva (o
   cambia su sello de seguridad) y pasa `SesionPersonal.IntervaloRevalidacion` **Then**
   `RevalidacionDeCircuito` lo detecta con `IIngresoPersonal.SesionVigenteAsync` y la página navega al
   ingreso con recarga completa. (Cierra la nota que la 2.1 dejó para la primera pantalla interactiva.)

### B. Hoja de estilos y tokens — guía "Diseño visual" (B-1.9 a B-1.12)

6. `wwwroot/css/tokens.css` define como variables CSS: primario `#1F5FA8`, éxito, atención `#B45309`,
   peligro, información y neutro; tipografía sans serif del sistema con la escala 28/20/16/14 px; grilla de
   8 px (`--esp-1` a `--esp-4`); radio y sombras. Bootstrap se personaliza desde ahí con `--bs-*`.
   `App.razor` lo carga antes que `app.css`.
7. `app.css` deja de ser la plantilla de fábrica: define `.page-header`, `.filter-bar`, `.table-actions` y
   `.badge-estado--vigente|programada|pendiente|inactivo|activo|info` sólo con variables; **ningún
   hexadecimal fuera de `tokens.css`** en `wwwroot/`.
8. Ningún `.razor` bajo `Components/` contiene `style="` ni un color hexadecimal.
9. No queda ninguna regla `outline: none` sin reemplazo; el foco se ve con un contorno de 3 px del color
   primario en todo control (UX §9).
10. Cada par texto/fondo declarado (cuerpo, botones, enlaces, cada badge, error) tiene contraste ≥ 4,5:1, y
    los bordes de controles ≥ 3:1.

### C. Marco y menú del personal — guía "Navegación y menú principal" (B-1.1 a B-1.8)

11. **Given** un usuario del personal ingresado **When** abre cualquier sección **Then** ve una barra
    superior con "Las Acacias" a la izquierda y su nombre con "▾" a la derecha (B-1.1).
12. **Given** la barra superior **When** el usuario activa su nombre (clic o Enter) **Then** se despliega un
    `<details>` con "Cambiar contraseña" (enlace a `RutasPersonal.CambiarContrasena`) y "Cerrar sesión" (POST
    con antifalsificación a `RutasPersonal.Salir`, igual que el botón de hoy); el botón "Salir" suelto ya no
    existe (B-1.2).
13. **Given** cada rol **When** ve el menú lateral **Then** las opciones aparecen agrupadas con un rótulo por
    grupo, en este orden (**S-36**: los grupos de recepción y médico no los define la guía; el docente los
    aprobó con la story el 2026-10-17) (B-1.4):
    - Administrador: **Agenda** (Agendas · Propuestas de agenda · Bloqueos de agenda · Pacientes para
      avisar) · **Catálogos** (Médicos · Especialidades) · **Administración** (Usuarios del personal ·
      Auditoría).
    - Recepcionista: **Recepción** (Turnos) · **Agenda** (Agendas · Pacientes para avisar).
    - Médico: **Mi agenda** (Mi agenda · Mi propuesta de agenda).
14. **Given** un usuario de cualquier rol **When** ingresa **Then** `RutasPersonal.InicioDe(rol)` lo lleva al
    mismo lugar que hoy (administrador → Médicos, recepción → Turnos, médico → Mi agenda), aunque el menú
    haya cambiado de orden.
15. **Given** el usuario en `/personal/admin/medicos/12` **When** se dibuja el menú **Then** "Médicos" se ve
    resaltado (fondo, barra lateral de color y negrita) y tiene `aria-current="page"`; ninguna otra opción lo
    tiene (B-1.5).
16. Cada opción del menú es un ítem de navegación con texto sin subrayado y alto mínimo de 40 px, sin color
    en línea (B-1.6).
17. El ítem del menú acepta un contador opcional (`Seccion` con `Pendientes`); en esta story ninguna sección
    lo muestra. El primero lo trae la 4.3 (B-1.7).
18. **Given** una ventana de menos de 992 px **When** se carga cualquier sección **Then** el menú lateral no
    se ve y aparece "☰ Menú" (un `<details>` con su `<summary>`, operable con teclado); al abrirlo, el menú se
    superpone al contenido (B-1.8).
19. **Given** cualquier pantalla de A1, A2 o A4 **When** se dibuja **Then** muestra migas de pan
    ("Catálogos / Especialidades", "Catálogos / Médicos", "Catálogos / Médicos / Nuevo médico",
    "Catálogos / Médicos / Ríos, Pablo", "Administración / Usuarios del personal") con todos los niveles
    menos el último como enlace, y el `<h1>` coincide con el texto del menú. Los enlaces "Volver a Médicos"
    desaparecen (B-1.3, B-4.6).

### D. Feedback y datos — guía "Estados, estados vacíos y feedback" (B-1.13, B-1.14)

20. **Given** una acción que termina bien **When** vuelve el control **Then** aparece una notificación arriba
    a la derecha, dentro de una región `aria-live="polite"`, con uno de estos textos: "Especialidad creada",
    "Especialidad desactivada", "Especialidad reactivada", "Médico creado", "Médico actualizado", "No había
    cambios para guardar", "Usuario creado", "Usuario desactivado", "Usuario reactivado", "Usuario
    desbloqueado"; y **When** pasan 4 segundos del `TimeProvider` inyectado **Then** desaparece. Reemplaza las
    alertas verdes fijas (B-1.13).
21. **Given** el Compose de demo **When** se consultan médicos y especialidades cuyo nombre contenga "Sonda"
    **Then** hay 0 filas; las sondas de prueba en vivo de esta story borran lo que crean (B-1.14).

### E. Componentes comunes — arquitectura §5.1

22. **Given** una página que usa `PaginaAbm` **When** se dibuja **Then** tiene migas, un encabezado
    `.page-header` con "Título · N" (N = total del filtro aplicado) y **un único** `.btn-primary`, a la
    derecha del título.
23. **Given** `BarraDeFiltros` **When** el usuario escribe en el buscador **Then** el listado se filtra 300 ms
    (del `TimeProvider`) después de la última tecla; **When** pulsa Enter **Then** se filtra en el acto. El
    buscador tiene etiqueta visible y un placeholder que dice qué busca.
24. **Given** filtros aplicados **When** se dibuja la barra **Then** muestra los filtros activos y "Limpiar
    filtros"; **When** el usuario lo activa **Then** búsqueda, filtros, orden y página vuelven a su valor por
    defecto.
25. `FiltroDeEstado` ofrece "Activos / Inactivos / Todos" (o "Activas / Inactivas / Todas") con "Activos"
    por defecto.
26. **Given** un listado con búsqueda, filtros, orden y página **When** se cambia cualquiera **Then** la URL
    queda como `?buscar=&estado=&especialidad=&agenda=&rol=&orden=&pagina=&tamano=` (sólo los que no están en
    su valor por defecto) y **When** se abre esa URL o se vuelve con Atrás **Then** el listado se restaura igual.
27. `TablaAbm<TFila>`: encabezados ordenables con ▲/▼ y `aria-sort`; filas de 48 px con separadores
    horizontales y sin bordes verticales; encabezado fijo al hacer scroll; resaltado de fila al pasar el
    mouse; números alineados a la derecha; última columna de acciones (`.table-actions`).
28. **Given** `TablaAbm` **When** está cargando **Then** muestra filas esqueleto; **When** no hay ningún
    registro sin filtros **Then** muestra el vacío inicial (ícono, frase y la acción de alta); **When** hay
    filtros y 0 resultados **Then** muestra "No hay <entidad> que coincidan con '<texto>'" y "Limpiar filtros".
29. **Given** una ventana de menos de 768 px **When** se dibuja `TablaAbm` **Then** cada fila es una tarjeta
    con el nombre, el badge y el menú "⋯", y el botón de alta queda fijo abajo.
30. **Given** `MenuDeAcciones` **When** se dibuja **Then** es un botón "⋯" de al menos 24 × 24 px con
    `aria-label="Más acciones para <registro>"`; **When** se pulsa Enter **Then** abre, las flechas recorren
    las opciones y Esc cierra devolviendo el foco al botón.
31. **Given** `Paginador` con 26 o más registros **When** se dibuja **Then** dice "Mostrando 1–25 de N" y
    ofrece 25 / 50 / 100 por página; con 25 o menos no aparece.
32. `InsigniaDeEstado` siempre muestra el texto y una clase `.badge-estado--*`; nunca sólo color.
33. `FormularioAbm` + `CampoDeFormulario`: una columna de hasta 640 px; nota "\* Obligatorio" al inicio;
    asterisco en obligatorios y "(opcional)" en el resto; etiqueta arriba asociada con `for`; ayuda debajo;
    error debajo, en rojo, con ícono y enlazado con `aria-describedby`; "Guardar" (primario) y "Cancelar"
    (secundario) al pie, en ese orden; foco en el primer campo al abrir.
34. **Given** un campo con un valor inválido **When** el usuario sale del campo (blur) **Then** su error
    aparece debajo sin enviar; **When** envía con errores **Then** se validan todos y el foco va al primero.
35. **Given** un formulario válido **When** se pulsa "Guardar" **Then** el botón queda deshabilitado y dice
    "Guardando…" hasta que vuelve la respuesta; un segundo clic no envía de nuevo.
36. **Given** un formulario con cambios **When** el usuario sigue un enlace del sistema o pulsa "Cancelar"
    **Then** se pregunta "Tenés cambios sin guardar. ¿Salir igual?" con "Salir sin guardar" y "Seguir
    editando"; **When** cierra o recarga la pestaña **Then** el navegador muestra su aviso (`NavigationLock`
    con `ConfirmExternalNavigation`).
37. `SelectConBusqueda`: con más de 10 opciones permite escribir para filtrar; con 10 o menos es un select
    común; operable con teclado.
38. **Given** un `DialogoModal` abierto **When** el usuario pulsa Tab o Mayús+Tab **Then** el foco circula
    sólo dentro del diálogo; **When** pulsa Esc **Then** se cierra y el foco vuelve al control que lo abrió.
    Tiene `role="dialog"`, `aria-modal="true"` y `aria-labelledby`.
39. **Given** un `DialogoDeConfirmacion` **When** se abre **Then** muestra "¿<Verbo> <registro>?", la
    consecuencia, el botón con el verbo (rojo, el único rojo de la vista) y "Cancelar" (secundario) **con el
    foco**; en modo "no se puede" muestra el motivo y sólo "Entendido".
40. **Given** `VistaDeDetalle` **When** se abre **Then** está en modo lectura con "Editar"; `PieDeTrazabilidad`
    muestra "Creado por <nombre> el dd/mm/aaaa HH:mm" y, si hubo cambios, "· Modificado por <nombre> el
    dd/mm/aaaa HH:mm", leídos de la auditoría.
41. `Formatos`: fechas `dd/mm/aaaa` (siempre con año) y horas `HH:mm` en la zona del hospital; personas
    "Apellido, Nombre" en listados.

### F. Consultas de sólo lectura (decisión D2)

42. **Given** `ConsultaDeListado(Buscar, Filtros, Orden, Pagina, Tamano)` **When** se ejecuta **Then** devuelve
    `PaginaDeResultados<T>(Items, Total)` con la paginación, el orden y el conteo hechos en la base
    (`Skip`/`Take` + `CountAsync`).
43. **Given** `ICatalogoEspecialidades.ListarPaginadoAsync` **When** se busca "cardiologia" **Then** encuentra
    "Cardiología" (compara contra `NombreNormalizado`); filtra por estado; ordena por nombre o por médicos
    asociados; cada fila trae `MedicosAsociados`. `ContarMedicosAsync(id)` alimenta la confirmación.
44. **Given** `IGestionMedicos.ListarPaginadoAsync` **When** se busca y se filtra **Then** busca en apellido,
    nombre o matrícula con el límite de 100 caracteres de hoy, filtra por especialidad y por estado de agenda
    (Vigente, Programada, Pendiente de aprobación, Sin agenda, calculado como hoy con `SituacionDeAgenda`) y
    ordena por médico (apellido, nombre), matrícula o especialidad.
45. **Given** `IGestionUsuariosPersonal.ListarPaginadoAsync` **When** se busca y se filtra **Then** busca en
    usuario o nombre, filtra por rol y por estado, y ordena por nombre o rol.
46. **Given** `IConsultaTrazabilidad.ObtenerAsync(entidadTipo, entidadId)` **When** la entidad tiene registros
    de auditoría **Then** devuelve el primero y el último (quién y cuándo); sólo lee y no toca la protección
    de la tabla (ADR-07).

### G. Especialidades (A2) — B-2.x

47. **Given** un administrador en A2 **When** se dibuja **Then** el encabezado dice "Especialidades · N" con
    "Nueva especialidad" como único primario, y no hay formulario fijo arriba de la tabla (B-2.1, B-2.2).
48. **Given** A2 **When** se pulsa "Nueva especialidad" **Then** se abre un `DialogoModal` con el campo
    "Nombre \*" con el foco, "Guardar" y "Cancelar"; **When** se guarda bien **Then** el modal se cierra, la
    lista se recarga y aparece "Especialidad creada" (B-2.2, B-2.15).
49. **Given** A2 **When** se dibuja **Then** tiene el buscador "Buscar por nombre" y el filtro "Activas /
    Inactivas / Todas" con "Activas" por defecto, ambos en la URL, con los filtros activos y "Limpiar
    filtros" (B-2.3, B-2.4, B-2.5).
50. Columnas de A2: Nombre · Médicos asociados (a la derecha) · Estado (badge "Activa"/"Inactiva", **sólo
    con "Todas"**) · Acciones (B-2.6, B-2.7).
51. Cada fila tiene un `MenuDeAcciones` con "Desactivar" (activas) o "Reactivar" (inactivas); ninguna fila
    tiene un botón rojo (B-2.8).
52. **Given** Cardiología activa con 12 médicos **When** se elige "Desactivar" **Then** se abre "¿Desactivar
    Cardiología?" con "12 médicos la tienen asignada; no podrá elegirse en nuevos médicos." y los botones
    "Desactivar especialidad" y "Cancelar"; con 0 médicos dice "Ningún médico la tiene asignada; no podrá
    elegirse en nuevos médicos."; **When** se confirma **Then** queda inactiva y aparece "Especialidad
    desactivada" (B-2.9).
53. **Given** Pediatría inactiva **When** se elige "Reactivar" **Then** se abre "¿Reactivar Pediatría?" con
    "Vuelve a ofrecerse al dar de alta o editar un médico." y "Reactivar especialidad"; al confirmar aparece
    "Especialidad reactivada".
54. **Given** A2 sin especialidades **When** se dibuja **Then** muestra "Todavía no hay especialidades. Creá
    la primera." con "Nueva especialidad"; **Given** una búsqueda sin resultados **Then** "No hay
    especialidades que coincidan con '<texto>'" y "Limpiar filtros" (B-2.12).
55. A2 usa `TablaAbm`, con tarjetas bajo 768 px (B-2.14).

### H. Médicos — listado (A1) — B-3.x

56. **Given** un administrador en A1 **When** se dibuja **Then** el encabezado dice "Médicos · N" con "Nuevo
    médico" a la derecha como único primario (B-3.1).
57. El buscador tiene la etiqueta "Buscar" y el placeholder "Buscar por apellido, nombre o matrícula", y
    filtra al escribir (300 ms) y con Enter (B-3.2, B-3.3).
58. **Given** A1 **When** se dibuja **Then** tiene los filtros Especialidad (`SelectConBusqueda`, con las
    especialidades que tienen médicos) y Estado de agenda, con filtros activos, "Limpiar filtros" y todo en
    la URL (B-3.4, B-3.6).
59. A1 ordena por Médico, Matrícula y Especialidad con ▲/▼; por defecto, Médico ascendente (B-3.7).
60. A1 pagina con el `Paginador` desde 26 médicos (B-3.8).
61. **Given** cada `SituacionDeAgenda` (cálculo de la 4.1, sin cambios) **When** se dibuja la columna Estado de
    agenda **Then** muestra un badge: "Vigente" (verde) · "Programada · desde dd/mm/aaaa" (azul) · "Pendiente
    de aprobación" (ámbar) · "Sin agenda" (gris); con agenda vigente y otra aprobada a futuro, el badge
    "Vigente" y debajo, como texto de ayuda, "Cambia el dd/mm/aaaa". "Sin agenda (rige desde dd/MM)" ya no
    aparece (B-3.9).
62. Cada fila de A1 tiene "Editar" visible (lleva a la ficha en modo edición) y un `MenuDeAcciones` con
    "Vincular usuario" (lleva a la ficha en modo edición con el foco en "Usuario del sistema"). "Ver agenda"
    y "Desactivar" no se muestran: no existen todavía (B-3.10).
63. **Given** A1 sin médicos **When** se dibuja **Then** "Todavía no hay médicos. Creá el primero." con
    "Nuevo médico"; **Given** una búsqueda sin resultados **Then** "No hay médicos que coincidan con
    '<texto>'" y "Limpiar filtros". Tarjetas bajo 768 px (B-3.11, B-3.12).

### I. Médicos — nuevo y ficha (A1) — B-4.x

64. **Given** un médico existente **When** se abre su ficha **Then** está en modo lectura (`VistaDeDetalle`)
    con las secciones Datos y Usuario del sistema y el botón "Editar"; **When** se pulsa "Editar" **Then**
    aparece el `FormularioAbm` con "Guardar" y "Cancelar"; **When** se pulsa "Cancelar" sin cambios **Then**
    vuelve a la lectura (B-4.1, B-4.2).
65. El encabezado de la ficha es "Apellido, Nombre" con el badge del estado de agenda al lado (B-4.3).
66. El pie de la ficha es `PieDeTrazabilidad` con `medico.creado` y el último `medico.editado` (B-4.4).
67. Los campos van en este orden: Nombre \*, Apellido \*, Matrícula \* (ayuda con un ejemplo que el dominio
    acepta, "Ejemplo: MP 1234"), Especialidad \* (`SelectConBusqueda`) y Usuario del sistema (opcional), con la
    ayuda "Vinculalo para que el médico pueda ver y gestionar su agenda."; el foco empieza en Nombre (B-4.7 a
    B-4.11).
68. **Given** una matrícula que ya tiene otro médico **When** se guarda **Then** el mensaje del dominio de hoy
    aparece **debajo del campo Matrícula**, con ícono y `aria-describedby`, y lo cargado se conserva (B-4.12,
    B-4.13).
69. **Given** un médico "Pereyra, Martín José" **When** se da de alta otro con el mismo apellido y nombre
    (comparados sin tildes ni mayúsculas) y otra matrícula **Then** antes de guardar se avisa "Ya hay un
    médico Pereyra, Martín José (MP 1234). ¿Es otra persona?" con "Guardar igual" y "Cancelar"; "Guardar
    igual" lo crea. Es un aviso: no bloquea ni cambia la regla del dominio (B-4.14).
70. **Given** el alta **When** se guarda bien **Then** se pasa a la ficha en lectura con "Médico creado";
    **When** se cancela **Then** vuelve al listado. **Given** la ficha en edición **When** se guarda **Then**
    vuelve a la lectura con "Médico actualizado" o, sin cambios, "No había cambios para guardar". Desaparecen
    "Dar de alta", "Guardar cambios" y el parámetro `?resultado=` (B-4.15, B-4.19).
71. El alta y la edición cumplen "Guardando…" (AC 35) y el aviso de cambios sin guardar (AC 36) (B-4.17,
    B-4.18).

### J. Usuarios del personal (A4) — B-4bis.x (decisión D4)

72. **Given** un administrador en A4 **When** se dibuja **Then** el encabezado dice "Usuarios del personal · N"
    con "Nuevo usuario" como único primario; **When** lo pulsa **Then** se abre un `DialogoModal` con Nombre
    completo \*, Usuario \* (con su ayuda de hoy) y Rol \*, con las validaciones y mensajes de hoy (B-4bis.1).
73. A4 tiene el buscador "Buscar por nombre o usuario" y los filtros Rol y "Activos / Inactivos / Todos", en
    la URL (B-4bis.2).
74. Columnas de A4: Nombre · Usuario · Rol · Médico vinculado · Estado · Acciones (B-4bis.3).
75. El estado se muestra con badges: "Activo" (verde) · "Inactivo" (gris) · "Bloqueado hasta HH:mm" (ámbar)
    · "Contraseña temporal" (azul, cuando debe cambiarla) (B-4bis.4).
76. Cada fila tiene un `MenuDeAcciones` con "Nueva contraseña temporal", "Desbloquear" (si está bloqueado) y
    "Desactivar" o "Reactivar"; la fila propia muestra "(sos vos)" y ninguna acción que el dominio prohíba
    (B-4bis.5).
77. **Given** ana.ruiz activa **When** se elige "Desactivar" **Then** se abre "¿Desactivar a ana.ruiz?" con
    "No va a poder ingresar hasta que la reactives." y "Desactivar usuario"; **When** se elige "Nueva
    contraseña temporal" **Then** "¿Generar otra contraseña temporal para ana.ruiz?" con "La contraseña actual
    deja de servir." y "Generar contraseña". Reactivar y desbloquear no piden confirmación (UX §1.5)
    (B-4bis.6).
78. **Given** un alta o una contraseña restablecida **When** termina **Then** la contraseña temporal se
    muestra en un `DialogoModal` propio (no un toast), con el texto de hoy, el código en `<code>` y el botón
    "Listo"; al cerrarlo no queda en ningún otro lado (AC 4 de la 2.2, AC 2 de la 2.3) (B-4bis.8).
79. A4 pagina y ordena con los componentes estándar; textos "Nuevo usuario", "Inactivo", "Usuario creado"
    (B-4bis.7, B-4bis.9).

### Modos de falla

80. **Given** un valor desconocido en la URL (`?orden=xyz`, `?pagina=-3`, `?tamano=7`, `?estado=otro`)
    **When** se abre el listado **Then** se ignora y se usa el valor por defecto; la página no falla.
81. **Given** `?pagina=` mayor que la última **When** se abre el listado **Then** se muestra la última página
    con datos.
82. **Given** una búsqueda de más de 100 caracteres en A1 **When** se filtra **Then** no filtra y muestra "La
    búsqueda tiene como máximo 100 caracteres." debajo del buscador, como hoy.
83. **Given** que la consulta del listado falla (base caída) **When** se carga **Then** la tabla muestra "No
    pudimos cargar la lista." con "Reintentar"; nunca una pantalla en blanco ni un detalle técnico.
84. **Given** el alta de una especialidad con error del dominio (vacía, más de 80 caracteres, duplicada,
    duplicada desactivada) **When** se guarda **Then** el modal no se cierra, conserva lo tipeado y muestra el
    mensaje de hoy.
85. **Given** que el dominio rechaza una desactivación (por ejemplo, la regla de turnos futuros que trae la
    5.3) **When** se confirma **Then** el `DialogoDeConfirmacion` pasa a modo "no se puede" con el motivo; la
    fila no cambia.
86. **Given** "Nuevo médico" sin especialidades activas **When** se abre **Then** muestra el vacío "Primero
    creá una especialidad." con el enlace "Ir a Especialidades" y no el formulario.
87. **Given** la ficha de un médico que no existe **When** se abre **Then** muestra "No existe el médico
    pedido." con las migas y el enlace a Médicos.
88. **Given** que otro administrador cambió el registro mientras se editaba **When** se guarda **Then** el
    mensaje del dominio de hoy aparece en el resumen del formulario y lo cargado se conserva.

### No-regresión

89. Un GET sin sesión a A1, A2 o A4 sigue respondiendo `302` al ingreso, y con otro rol `403` con "No tenés
    permiso para entrar a esta sección." (2.1, 3.1, 3.2).
90. Las pruebas de Dominio y de Aplicación existentes pasan sin modificarse (sólo se agregan las de F); los
    mensajes de regla y las acciones de auditoría no cambian.
91. `ListarAsync` y `ListarActivasAsync` de especialidades y médicos siguen existiendo y devuelven lo mismo
    (las usan 3.2, 4.2 y las stories que vienen).
92. Las pruebas HTTP que verificaban formularios SSR (POST `alta-especialidad`, `acciones-especialidades`,
    `medico`, `alta-usuario`, `acciones-usuarios`) se reemplazan por pruebas de componente bUnit del mismo
    comportamiento; las HTTP de permisos y de carga se conservan.
93. La suite completa y la CI quedan en verde, con cobertura de líneas ≥ 80 % en Web y sin baja en Dominio y
    Aplicación.

### Trazabilidad brecha → criterio

| Brecha (análisis) | AC |
|---|---|
| B-1.1 barra superior | 11 |
| B-1.2 menú de usuario | 12 |
| B-1.3 migas | 19 |
| B-1.4 menú agrupado | 13, 14 |
| B-1.5 opción activa | 15 |
| B-1.6 ítems de navegación | 16 |
| B-1.7 contadores | 17 |
| B-1.8 ☰ pantallas chicas | 18 |
| B-1.9 tokens | 6 |
| B-1.10 clases de componente | 7, 8 |
| B-1.11 escala tipográfica | 6 |
| B-1.12 foco visible | 9 |
| B-1.13 notificación de éxito | 20 |
| B-1.14 datos de prueba | 21 |
| B-2.1 título con total | 47 |
| B-2.2 alta en modal | 47, 48 |
| B-2.3 / B-2.4 / B-2.5 buscador, estado, limpiar | 49 |
| B-2.6 médicos asociados | 50, 43 |
| B-2.7 badge de estado | 50 |
| B-2.8 menú "⋯" | 51 |
| B-2.9 confirmación | 52, 53 |
| B-2.12 vacíos | 54 |
| B-2.14 tabla y celular | 55 |
| B-2.15 microcopy | 48, 20 |
| B-3.1 encabezado | 56 |
| B-3.2 / B-3.3 buscador | 57 |
| B-3.4 / B-3.6 filtros y URL | 58, 44, 26 |
| B-3.7 orden | 59 |
| B-3.8 paginación | 60, 31 |
| B-3.9 badge de agenda | 61 |
| B-3.10 acciones por fila | 62 |
| B-3.11 / B-3.12 vacíos y celular | 63 |
| B-4.1 / B-4.2 modo lectura y secciones | 64 |
| B-4.3 badge en el encabezado | 65 |
| B-4.4 creado / modificado por | 66, 46 |
| B-4.6 migas | 19 |
| B-4.7 a B-4.11 campos, ayudas, foco | 67 |
| B-4.12 / B-4.13 validación y error junto al campo | 34, 68 |
| B-4.14 homónimo | 69 |
| B-4.15 / B-4.19 Guardar, Cancelar y notificación | 70 |
| B-4.17 / B-4.18 Guardando… y cambios sin guardar | 71, 35, 36 |
| B-4.20 ancho del formulario | 33 |
| B-4bis.1 a B-4bis.9 (A4) | 72 a 79 |
| Interactividad (decisión D1) | 1 a 5 |

**Fuera de esta story (decisión D3, al backlog):** renombrar especialidad (B-2.10), desactivar médico y su
filtro Activos/Inactivos (B-3.5, B-4.5), historial por registro (parte de B-4.2) y "Deshacer" (B-2.11, sólo
Conviene). El bloqueo por turnos futuros (B-2.13) lo trae la 5.3; el diálogo ya lo soporta (AC 39, 85).

### Tests (61 verificaciones)

Todas llevan `[Trait("Story", "3.3")]` y un `[Trait("AC", "n")]` por criterio cubierto.

| # | Test | AC | Nivel | Escenario | Resultado esperado |
|---|------|----|-------|-----------|--------------------|
| T1 | `PaginasAbm_DeclaranInteractivoSinPrerender_YLasDeCookieNo` | 1 | unit | Reflexión sobre los tipos de A1, A2, A4, Ingreso, CambiarContrasena | Las ABM tienen el modo con `prerender: false`; las de cookie no tienen modo |
| T2 | `AccionEnCircuito_AuditaConElActorDelEstadoDeAutenticacion` | 2 | integration | bUnit con `AuthenticationState` de un administrador; crea una especialidad | La fila de auditoría tiene su id, nombre, rol, canal `Personal` y `especialidad.creada` |
| T3 | `ErrorDelDominio_SeMuestraEnLaPagina_ConElMismoTexto` | 3 | integration | Alta de especialidad duplicada en el modal | Resumen del modal con "Ya existe la especialidad Cardiología." |
| T4 | `ErrorInesperado_MuestraElMensajeGenerico_YRegistraElDetalle` | 4 | integration | Doble del catálogo que lanza `InvalidOperationException` | "Algo falló. Probá de nuevo en unos minutos." y el registro en memoria tiene la excepción |
| T5 | `CircuitoConUsuarioDesactivado_NavegaAlIngreso` | 5 | integration | `RevalidacionDeCircuito` con `SesionVigenteAsync` → false y reloj adelantado el intervalo | `ValidateAuthenticationStateAsync` devuelve false y la página navega al ingreso con recarga |
| T6 | `Tokens_DefinenColoresTipografiaEspaciadoYRadio` | 6 | unit | Leer `tokens.css` | Están las variables de la lista del AC 6 con sus valores |
| T7 | `NingunHexadecimal_FueraDeTokens` | 7 | unit | Recorrer `wwwroot/**/*.css` salvo `tokens.css` y `lib/` | 0 coincidencias de `#[0-9a-fA-F]{3,8}\b` |
| T8 | `NingunComponente_UsaStyleEnLinea_NiHexadecimal` | 8 | unit | Recorrer `Components/**/*.razor` | 0 coincidencias de `style="` y de hexadecimales |
| T9 | `HojaDeEstilos_NoAnulaElFoco` | 9 | unit | Leer `app.css` | No hay `outline: none` sin otra regla `outline` en el mismo selector; existe la regla de foco de 3 px |
| T10 | `ParesDeColor_CumplenContrasteAA` [Theory] | 10 | unit | Cada par declarado de `tokens.css` | Contraste ≥ 4,5 (texto) o ≥ 3 (bordes) |
| T11 | `Marco_MuestraBarraSuperiorYMenuDeUsuario` | 11, 12 | integration | GET de una sección con sesión de administrador | Barra con "Las Acacias", `<details>` con "Cambiar contraseña" y el form POST "Cerrar sesión" con token; no hay botón "Salir" suelto |
| T12 | `CerrarSesion_SigueSiendoPostConAntifalsificacion` | 12 | integration | POST a `Salir` sin token y con token | Sin token 400; con token 302 al ingreso y la cookie vencida |
| T13 | `Menu_AgrupaPorRol_EnElOrdenDefinido` [Theory] | 13 | unit | Los tres roles | Grupos y opciones exactamente como el AC 13 |
| T14 | `InicioDespuesDeIngresar_NoCambia` [Theory] | 14 | unit | Los tres roles | Médicos, Turnos y Mi agenda |
| T15 | `OpcionActiva_TieneAriaCurrent_TambienEnSubrutas` | 15 | integration | GET `/personal/admin/medicos/{id}` | Sólo "Médicos" tiene `aria-current="page"` y la clase activa |
| T16 | `ItemDeMenu_AceptaContador_YHoyNoMuestraNinguno` | 16, 17 | unit | bUnit del menú con una sección con `Pendientes = 3` y el menú real | El de prueba muestra "3"; el real no muestra ningún contador ni estilo en línea |
| T17 | `PantallaChica_MuestraBotonMenu` | 18 | integration | GET de una sección | Existe el `<details>` "☰ Menú" con la clase que lo muestra bajo 992 px |
| T18 | `Migas_YTitulo_CoincidenConElMenu` [Theory] | 19 | integration | A2, A1, nuevo médico, ficha, A4 | Las migas del AC 19 y el `<h1>` igual al texto del menú; no aparece "Volver a Médicos" |
| T19 | `Notificacion_SeAnuncia_YDesapareceALos4Segundos` | 20 | unit | bUnit con reloj falso: mostrar "Especialidad creada", avanzar 3,9 s y 0,2 s | Dentro de `aria-live`; visible a 3,9 s, ausente a 4,1 s |
| T20 | `BaseDeDemo_SinRegistrosDeSonda` | 21 | integration | Consulta del AC 21 en la base de la prueba en vivo | 0 filas |
| T21 | `PaginaAbm_TieneUnUnicoPrimario_YElTotal` | 22 | unit | bUnit con título "Especialidades", total 18 y un botón | "Especialidades · 18" y un solo `.btn-primary` |
| T22 | `Buscador_FiltraA300ms_YConEnter` | 23 | unit | bUnit con reloj falso: escribir y avanzar 299 ms, 1 ms; luego escribir y Enter | Sin filtrar a 299 ms, filtrado a 300 ms; con Enter, en el acto |
| T23 | `LimpiarFiltros_VuelveALosValoresPorDefecto` | 24, 25 | unit | Búsqueda, estado "Todos" y página 2 aplicados | Muestra los filtros activos; tras "Limpiar filtros", todo en el defecto y estado "Activos" |
| T24 | `EstadoDelListado_SeLeeYEscribeEnLaUrl` | 26 | unit | `EstadoDeListado` desde y hacia una URL con todos los parámetros | Ida y vuelta iguales; los valores por defecto no aparecen en la URL |
| T25 | `ValorDesconocidoEnLaUrl_VuelveAlDefecto` [Theory] | 80 | unit | `orden=xyz`, `pagina=-3`, `tamano=7`, `estado=otro` | Cada uno en su defecto, sin excepción |
| T26 | `Tabla_OrdenaConIndicadorYAriaSort` | 27 | unit | bUnit: clic en el encabezado Médico dos veces | ▲ y `aria-sort="ascending"`, luego ▼ y `descending`; números con la clase de alineación a la derecha |
| T27 | `Tabla_DistingueCargaVacioYSinResultados` [Theory] | 28 | unit | Cargando, 0 sin filtros, 0 con filtro "Pereira" | Esqueleto; vacío con la acción; "No hay médicos que coincidan con 'Pereira'" + "Limpiar filtros" |
| T28 | `Tabla_ErrorDeCarga_MuestraReintentar` | 83 | unit | Consulta que lanza | "No pudimos cargar la lista." y "Reintentar", que vuelve a consultar |
| T29 | `Tabla_EnCelular_DibujaTarjetas` | 29 | unit | bUnit: marcado de la tabla | Cada fila tiene su versión tarjeta (clase visible bajo 768 px) con nombre, badge y "⋯"; el alta tiene la clase fija abajo |
| T30 | `MenuDeAcciones_TieneNombreAccesible_YTeclado` | 30 | unit | bUnit: Enter, flecha abajo, Esc | `aria-label` con el registro; abre, mueve el foco y al cerrar vuelve al botón |
| T31 | `Paginador_ApareceDesde26_YDiceMostrando` [Theory] | 31 | unit | 25, 26 y 124 registros | Oculto; "Mostrando 1–25 de 26"; "Mostrando 1–25 de 124" con 25/50/100 |
| T32 | `Insignia_SiempreTieneTexto` | 32 | unit | Cada variante | Texto no vacío y su clase `.badge-estado--*` |
| T33 | `Formulario_MarcaObligatorios_YEnlazaErrores` | 33 | unit | bUnit de `FormularioAbm` con un obligatorio y un opcional | "\* Obligatorio", asterisco, "(opcional)", `for`, `aria-describedby` al error; Guardar antes que Cancelar; foco en el primero |
| T34 | `Formulario_ValidaAlSalirDelCampo_YAlEnviar` | 34 | unit | Blur en un campo vacío; enviar con dos errores | Error del campo sin enviar; al enviar, los dos y el foco en el primero |
| T35 | `Guardar_SeDeshabilita_YDiceGuardando` | 35 | unit | Envío con una tarea pendiente y doble clic | Botón deshabilitado con "Guardando…"; el envío se llama una vez |
| T36 | `SalirConCambios_PideConfirmacion` | 36 | unit | Cambiar un campo y navegar; luego "Cancelar" | Diálogo "Tenés cambios sin guardar. ¿Salir igual?"; `NavigationLock` con `ConfirmExternalNavigation` |
| T37 | `SelectConBusqueda_Desde11Opciones` [Theory] | 37 | unit | 10 y 11 opciones | Select común; con 11, campo de búsqueda que filtra |
| T38 | `Dialogo_AtrapaElFoco_CierraConEsc_YLoDevuelve` | 38 | unit | Abrir desde un botón, Tab al final, Esc | El foco vuelve al primero del diálogo; al cerrar, vuelve al botón; atributos ARIA presentes |
| T39 | `Confirmacion_TieneVerboConsecuencia_YFocoEnCancelar` | 39 | unit | Modo normal y modo "no se puede" | Título, consecuencia, botón rojo con verbo y foco en Cancelar; en el otro modo, sólo "Entendido" |
| T40 | `Detalle_AbreEnLectura_YPieDeTrazabilidad` | 40 | unit | Con un solo registro de auditoría y con dos | "Editar"; "Creado por …" y luego "· Modificado por …" con dd/mm/aaaa HH:mm |
| T41 | `Formatos_DeFechaHoraYPersona` [Theory] | 41 | unit | 2026-11-16 08:05 UTC, "Ríos", "Pablo" | "16/11/2026", "05:05" (Córdoba), "Ríos, Pablo" |
| T42 | `Listados_PaginanOrdenanYCuentanEnLaBase` [Theory] | 42, 81 | integration | Testcontainers con 60 médicos; página 2 de 25; página 9 | 25 ítems y total 60; página 9 devuelve la 3 |
| T43 | `Especialidades_TraenMedicosAsociados_YBuscanSinTildes` | 43 | integration | Cardiología con 3 médicos; búsqueda "cardiologia" | La encuentra con `MedicosAsociados = 3`; `ContarMedicosAsync` da 3 |
| T44 | `Medicos_FiltranPorEspecialidadYEstadoDeAgenda` [Theory] | 44, 82 | integration | Médicos en los cuatro estados; filtro por cada uno; búsqueda de 101 caracteres | Cada filtro devuelve sólo los suyos; la de 101 no filtra |
| T45 | `Usuarios_FiltranPorRolYEstado` | 45 | integration | Usuarios de los tres roles, uno inactivo | Cada filtro devuelve los suyos, ordenados por nombre |
| T46 | `Trazabilidad_DevuelveCreacionYUltimaModificacion` | 46 | integration | Médico creado y editado dos veces | Primero `medico.creado`, último el segundo `medico.editado` |
| T47 | `ListasAnteriores_DevuelvenLoMismo` | 91 | integration | `ListarAsync` y `ListarActivasAsync` antes y después | Mismo contenido y orden |
| T48 | `Especialidades_AltaEnModal_ConLasValidacionesDeHoy` | 47, 48, 84 | integration | bUnit de A2: abrir el modal, enviar vacío, 81 caracteres, duplicada, válida | Sin formulario fijo; mensajes de hoy con el modal abierto; con la válida se cierra y aparece "Especialidad creada" |
| T49 | `Especialidades_FiltrosColumnas_YEstadoSoloConTodas` | 49, 50, 55 | integration | A2 con activas e inactivas | Buscador y filtro; columna Estado sólo con "Todas"; Médicos asociados a la derecha |
| T50 | `Especialidades_DesactivarYReactivar_ConfirmanConCantidad` | 51, 52, 53, 85 | integration | Cardiología con 12 médicos, otra con 0, Pediatría inactiva, un doble que rechaza | Sin botones rojos en filas; los textos de los AC 52 y 53; el rechazo pasa a "no se puede" |
| T51 | `Especialidades_VacioYSinResultados` | 54 | integration | Sin especialidades; búsqueda sin coincidencias | Los dos textos del AC 54 |
| T52 | `Medicos_ListadoConFiltrosOrdenYPaginacion` | 56, 57, 58, 59, 60, 63 | integration | A1 con 30 médicos | Encabezado, placeholder, filtros, orden por defecto, paginador; vacío y sin resultados |
| T53 | `Medicos_BadgeDeAgendaPorSituacion` [Theory] | 61 | unit | Las cinco `SituacionDeAgenda` (incluida vigente con programada) | Los textos y clases del AC 61; nunca "rige desde" |
| T54 | `Medicos_AccionesEditarYVincularUsuario` | 62 | integration | Fila de un médico | "Editar" y "⋯" con "Vincular usuario"; ni "Ver agenda" ni "Desactivar" |
| T55 | `Ficha_LecturaEdicion_GuardarCancelar_YMensajes` | 64, 65, 66, 67, 68, 70, 71, 86, 87, 88 | integration | Alta, ficha, editar, matrícula duplicada, sin cambios, sin especialidades, inexistente, conflicto | Lo de cada AC: lectura, badge, pie, campos y ayudas, error bajo Matrícula, "Médico creado/actualizado", vacíos y mensajes |
| T56 | `Alta_AvisaHomonimo_SinBloquear` | 69 | integration | Existe "Pereyra, Martín José"; alta de "pereyra, martin jose" con otra matrícula | Aviso del AC 69; "Guardar igual" lo crea |
| T57 | `Usuarios_ModalBadgesMenuConfirmaciones_YContrasenaEnDialogo` | 72, 73, 74, 75, 76, 77, 78, 79 | integration | A4 con usuarios en cada estado y la fila propia | Lo de cada AC; la contraseña sólo en el diálogo y ausente al cerrarlo |
| T58 | `PaginasAbm_SinSesionOConOtroRol_SiguenEn302O403` [Theory] | 89 | integration | GET a A1, A2, A4 sin cookie y como recepcionista | 302 al ingreso; 403 con el texto de hoy |
| R1 | Suite de Dominio y Aplicación existente sin cambios | 90 | integration | `dotnet test` de los dos proyectos | 0 fallas, sin archivos de prueba existentes modificados |
| R2 | Pruebas HTTP de formularios reemplazadas | 92 | integration | Revisión de los cinco `FormName` en las pruebas | Ninguna prueba hace POST de esos formularios; sus comportamientos están en T48–T57 |
| R3 | CI y cobertura | 93 | integration | CI del PR | Verde; líneas ≥ 80 % en Web |

> 61 verificaciones: T1–T58 más R1–R3. Los casos de cada `[Theory]` se suman al cerrar. No hay E2E con Playwright (el proyecto `HospitalTurnos.E2E` llega con la
> 10.1); en su lugar, la **matriz en vivo** del Compose recorre un paso por AC de las secciones C, D, G, H,
> I, J y los modos de falla visibles.

#### Given/Fixture

- **T2–T4, T48–T57** — bUnit `BunitContext` con `AddAuthorization()` y un `AuthenticationState` con los
  claims de `ClaimsPersonal` (como `MenuLateralTests`); los casos de uso reales contra PostgreSQL de
  Testcontainers (como `EspecialidadesPaginaTests`), salvo T4 y el rechazo de T50 que usan un doble.
- **T5** — `RevalidacionDeCircuito` con un `IIngresoPersonal` doble y `FakeTimeProvider`; se invoca
  `ValidateAuthenticationStateAsync` directamente.
- **T19, T22** — `FakeTimeProvider` registrado como `TimeProvider` en el contenedor de bUnit.
- **T42–T46** — fábrica de datos de `DatosDeCatalogo` (3.2) extendida con N médicos.

## Tasks / Subtasks

- [ ] [SEQ] Task 1: Salvaguarda de estado — verificar lo de "Que existe hoy" contra `main` antes de tocar
  nada (AC: todos)
- [ ] [SEQ] Task 2: Base interactiva (AC 1–5): `RevalidacionDeCircuito` registrado en `Program.cs`,
  `ActorDelCircuito`, `LimiteDeErrores`. T1–T5.
- [ ] [SEQ] Task 3: Hoja de estilos (AC 6–10): `tokens.css`, `app.css` reescrito, carga en `App.razor`.
  T6–T10.
- [ ] [SEQ] Task 4: Marco y menú (AC 11–19): `MarcoPersonal`, `MenuDeUsuario`, `MigasDePan`, grupos en
  `RutasPersonal` sin tocar `InicioDe`. T11–T18.
- [ ] [SEQ] Task 5: Componentes comunes (AC 20, 22–41, 80, 83), cada uno con su prueba antes de seguir.
  T19, T21–T41.
- [ ] [SEQ] Task 6: Consultas de lectura (AC 42–46, 81, 82, 91). T42–T47.
- [ ] [PAR] Task 7: Migrar A2 (AC 47–55, 84, 85). T48–T51.
- [ ] [PAR] Task 8: Migrar A1 (AC 56–71, 86–88). T52–T56.
- [ ] [PAR] Task 9: Migrar A4 (AC 72–79). T57.
- [ ] [SEQ] Task 10: Reemplazar las pruebas HTTP de formularios y correr la suite (AC 89, 90, 92, 93). T58,
  R1–R3.
- [ ] [SEQ] Task 11: Limpiar la base de demo y correr la matriz en vivo (AC 21). T20.

### Task Dependencies

T1 → T2 → T3 → T4 → T5 → T6 → (T7, T8, T9 en paralelo) → T10 → T11. Ninguna pantalla se migra antes de que
sus componentes tengan prueba en verde.

## Dev Notes

### Que existe hoy

#### En hospital-turnos (verificado 2026-10-17 contra `main` en `85c73b8`)

- **`src/HospitalTurnos.Web/Program.cs:33-34,101-102`** — ya registra `AddCascadingAuthenticationState()`,
  `AddInteractiveServerComponents()` y `AddInteractiveServerRenderMode()`. No hay que agregar el modo: sólo
  usarlo por página. Ninguna página declara `@rendermode` hoy.
- **`src/HospitalTurnos.Web/Personal/ValidadorSesionPersonal.cs`** — `ValidatePrincipal` revalida la cookie
  cada `SesionPersonal.IntervaloRevalidacion` con `IIngresoPersonal.SesionVigenteAsync(id, sello, ct)`. Corre
  sólo en pedidos HTTP; en un circuito no corre.
- **`src/HospitalTurnos.Web/Personal/ActorDeSesion.cs:14`** — `Desde(ClaimsPrincipal)`; se reutiliza con el
  `User` del `AuthenticationState`.
- **Páginas** `Especialidades.razor`, `Medicos.razor`, `MedicoNuevo.razor`, `FichaMedico.razor`,
  `UsuariosPersonal.razor` — usan `[CascadingParameter] HttpContext`, `[SupplyParameterFromForm]`,
  `FormName`, `ManejadorGlobalDeErrores.FijarEstadoEnPagina` y `style="background-color: #1F5FA8;"`. En una
  página interactiva `HttpContext` es null: todo eso se reemplaza.
- **`TablaMedicos.razor`** — `TextoDe(SituacionDeAgenda)` produce "Sin agenda (rige desde dd/MM)"; lo usa
  también la ficha. Se reemplaza por el badge (AC 61).
- **`src/HospitalTurnos.Web/Personal/RutasPersonal.cs:33-69`** — `Seccion(Ruta, Titulo)`,
  `SeccionesDe(rol)` (lista plana por rol) e `InicioDe(rol)` (la primera de la lista). Al agrupar, `InicioDe`
  tiene que devolver la ruta explícita de cada rol (AC 14).
- **`MenuLateral.razor`** — nombre y botón "Salir" arriba; enlaces con `style="color: #1F5FA8;"`.
- **`wwwroot/app.css`** — plantilla de fábrica: `#1b6ec2`, `h1:focus { outline: none; }`.
- **`Especialidad.Normalizar`** (`Dominio/Catalogo/Especialidad.cs:43`) — ya compara sin tildes ni
  mayúsculas; el buscador usa `NombreNormalizado`.
- **Auditoría** — `turnos_app` tiene `SELECT, INSERT` sobre `registros_auditoria`
  (`Migraciones/20261016010013_AgregarRegistroAuditoria.cs:42`): la trazabilidad se lee sin tocar permisos.
- **Pruebas afectadas** — `EspecialidadesHttpTests` (14), `MedicosHttpTests` (22), `UsuariosPersonalHttpTests`
  (34), `EspecialidadesPaginaTests`, `TablaMedicosTests`, `EstadoDeAgendaEnA1HttpTests`, `MenuLateralTests`.
  bUnit 2.11.3 ya está en `Directory.Packages.props`.
- **No existe:** ninguna pantalla interactiva, ningún componente común, `tokens.css`, consultas paginadas,
  lectura de auditoría por entidad, proyecto E2E.

### Patron de referencia verificado

| Proyecto | Ancla | Qué se verificó y qué se toma |
|---|---|---|
| Plantilla oficial (`dotnet new blazor --auth Individual --interactivity Server --all-interactive false`, SDK 10.0.400) | `Components/Account/IdentityRevalidatingAuthenticationStateProvider.cs:12-26` | Hereda de `RevalidatingServerAuthenticationStateProvider`, fija `RevalidationInterval` y en `ValidateAuthenticationStateAsync` compara el sello. `RevalidacionDeCircuito` igual, con `SesionPersonal.IntervaloRevalidacion` y `IIngresoPersonal.SesionVigenteAsync` (no hay `UserManager` en Web). |
| Plantilla oficial | `Program.cs:16` | `AddScoped<AuthenticationStateProvider, …>()`. Se registra igual. |
| Plantilla oficial | `Components/Pages/Counter.razor:2` | `@rendermode InteractiveServer` en la página, no global. Se toma por página, con `prerender: false`. |
| Plantilla oficial | `Components/Layout/NavMenu.razor:16-22` y `NavMenu.razor.css:94` | `NavLink` con clase activa por ruta. El menú usa `NavLink` con `Match="NavLinkMatch.Prefix"` para las subrutas (AC 15) y agrega `aria-current`. |
| hospital-turnos (2.1) | `tests/HospitalTurnos.Web.Tests/Personal/MenuLateralTests.cs` | bUnit con `AuthenticationState` armado a mano. Se toma para todas las pruebas de componente. |
| hospital-turnos (3.1) | `tests/HospitalTurnos.Web.Tests/Catalogo/EspecialidadesPaginaTests.cs` | bUnit contra los casos de uso reales con Testcontainers. Se toma para T48–T57. |

El proyecto guía de la materia (`changomas-testing-csharp`) no tiene Blazor ni bUnit (verificado en la 2.1
con `grep -rln -E "bunit|Bunit|razor"` → 0); por eso la referencia es la plantilla oficial.

### Que hay que hacer (la delta)

1. Hacer interactivas las tres pantallas ABM sin tocar los casos de uso: el actor sale del
   `AuthenticationState`, los errores se muestran en la página y la sesión se revalida en el circuito.
2. Construir los componentes comunes con sus pruebas (sección E) y la hoja de estilos única.
3. Agregar consultas de lectura paginadas y la lectura de trazabilidad (sección F), sin reglas nuevas.
4. Migrar A2, A1 y A4 a los componentes, con los textos de la guía.
5. Reemplazar las pruebas HTTP de formularios por pruebas de componente.

### Decisiones de esta story (con su porqué)

- **Sin prerender:** evita leer dos veces y el parpadeo de datos; la carga se ve con el esqueleto (guía).
- **Layout y menú estáticos:** el layout no puede ser interactivo sin volver interactiva toda la app, y el
  ingreso necesita SSR; `<details>` nativo da el menú de usuario y el ☰ sin JavaScript propio.
- **Notificación y espera del buscador con el reloj inyectado:** se prueban con `FakeTimeProvider`, nunca
  esperando tiempo real (lección de la sesión 07).
- **Homónimo como aviso:** la guía pide detectarlo antes de guardar; bloquearlo sería una regla nueva.
- **Reactivar y desbloquear sin confirmación:** UX §1.5 confirma sólo lo irreversible; ninguno lo es.
- **Contraseña temporal en un diálogo, no en un toast:** el toast se va a los 4 s y la contraseña se muestra
  una sola vez.

### Archivos a crear/modificar

| Accion | Archivo |
|--------|---------|
| Crear | Los 38 archivos de `files_to_create` (componentes en `Components/Comunes/`, `tokens.css`, revalidación, actor, consultas, pruebas) |
| Modificar | `Program.cs` (registro de `RevalidacionDeCircuito`), `App.razor` (carga de `tokens.css`), `LayoutPersonal.razor` (usa `MarcoPersonal`), `MenuLateral.razor` (grupos, activo, sin "Salir"), `RutasPersonal.cs` (grupos, `InicioDe` explícito, `Pendientes`) |
| Modificar | Las cinco páginas ABM y `TablaMedicos.razor` / `CamposMedico.razor` (migración) |
| Modificar | `ICatalogoEspecialidades`, `IGestionMedicos`, `IGestionUsuariosPersonal` y sus implementaciones (métodos paginados nuevos; los actuales quedan) |
| Modificar | Las siete clases de prueba de Web listadas en `files_to_modify` |

### Lo que NO hay que hacer

- **NO cambiar reglas, mensajes de regla ni acciones de auditoría** — la story es de presentación y lectura.
- **NO hacer interactivos el ingreso, el cambio de contraseña ni `Salir`** — escriben la cookie en la
  respuesta HTTP (2.1).
- **NO agregar JavaScript propio ni librerías de componentes** — ADR-08: todo en C#.
- **NO borrar `ListarAsync` ni `ListarActivasAsync`** — los usan 3.2, 4.2 y las stories que vienen.
- **NO cambiar `InicioDe` al reordenar el menú** — cambiaría a dónde entra cada rol.
- **NO usar `Task.Delay` sin el `TimeProvider`** — la notificación y el buscador se prueban con reloj falso.
- **NO usar `FijarEstadoEnPagina` en páginas interactivas** — no hay respuesta HTTP que modificar.
- **NO mostrar la contraseña temporal en un toast ni dejarla en el estado del circuito después de "Listo".**

### Como verificar que funciona

```bash
# 1. Pruebas (Docker para Testcontainers). test-gate.sh no reconoce .slnx: correr por proyecto.
dotnet test tests/HospitalTurnos.Dominio.Tests
dotnet test tests/HospitalTurnos.Aplicacion.Tests
dotnet test tests/HospitalTurnos.Web.Tests
# Esperado: 0 fallas; dotnet test --filter "Story=3.3" lista T1–T58 (más los casos de cada [Theory])

# 2. Compose y matriz en vivo
docker compose up -d --build
# Como administrador, con teclado y también a 375 px de ancho: un paso por AC de C, D, G, H, I, J
# y los modos de falla visibles (80–88).

# 3. Base de demo sin sondas (AC 21). Esperado: 0 filas
docker compose exec db psql -U postgres -d turnos -c \
  "select nombre from medicos where nombre ilike '%sonda%' or apellido ilike '%sonda%'
   union all select nombre from especialidades where nombre ilike '%sonda%';"
```

### Salvaguarda de BD

1. `select count(*) from registros_auditoria where entidad_tipo = 'medico';` — la trazabilidad tiene datos
   que leer (si da 0 en la base de demo, igual se prueba con Testcontainers).
2. `\dp registros_auditoria` — `turnos_app` tiene `SELECT`. Si no, DETENERSE: la story asume que no se tocan
   permisos.
3. Los nombres de columnas de `medicos`, `especialidades` y `registros_auditoria` se toman de las
   configuraciones EF existentes, no se suponen.

### References

- [Source: docs/ux/guia-ux-ui-abm.md] — todas las secciones.
- [Source: docs/ux/analisis-brechas-abm.md] — tablas 1 a 4 bis y decisiones D1–D4.
- [Source: docs/03-ux.md#0-estándar-obligatorio-la-guía-uxui] — excepciones decididas.
- [Source: docs/04-arquitectura.md#51-componentes-de-interfaz-reutilizables] — componentes y reglas de uso;
  ADR-08.
- [Source: docs/project-context.md] — reglas que se respetan siempre.
- [Source: docs/stories/2-1-ingreso-personal.md] — por qué S0 queda estático y la nota de revalidación.

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

- Análisis de contexto completo: guía de desarrollo creada a partir del análisis de brechas y las decisiones
  D1–D4 del docente (2026-10-17).

### File List
