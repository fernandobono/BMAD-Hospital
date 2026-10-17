---
title: "Análisis de brechas: pantallas ABM existentes contra la guía UX/UI"
fecha: 2026-10-17
estado: aprobado (D1–D4 decididas el 2026-10-17)
fuente: docs/ux/guia-ux-ui-abm.md
---

# Análisis de brechas: pantallas ABM contra la guía UX/UI

Se compararon las pantallas terminadas de **Especialidades (A2, story 3.1)** y **Médicos (A1, story 3.2:
listado, alta y ficha)**, más el marco común (layout y menú lateral), contra
[`guia-ux-ui-abm.md`](guia-ux-ui-abm.md). Sólo se listan los puntos que **no** se cumplen hoy.

## Cómo leer la columna Impacto

| Código | Qué significa | ¿Entra en la story correctiva sin tocar la lógica de negocio? |
|---|---|---|
| **P** | Sólo presentación: marcado, estilos, textos, componentes. | Sí |
| **I** | Necesita interactividad en el navegador (hoy todo el personal es SSR estático, sin JavaScript propio). | Depende de la **decisión D1** |
| **C** | Necesita una consulta nueva de lectura (contar, filtrar, paginar, ordenar). No cambia reglas. | Sí, si se acepta que una consulta de lectura no es "lógica de negocio" (**decisión D2**) |
| **N** | Necesita una regla o capacidad de negocio que hoy no existe (renombrar, desactivar médicos, historial). | No: story aparte (**decisión D3**) |

## 1. Marco común (layout y menú) — afecta a todas las pantallas

| Pantalla | Punto de la guía | Estado actual | Cambio necesario | Impacto |
|---|---|---|---|---|
| Marco | Estructura: barra superior + menú lateral + contenido | Sin barra superior; nombre del sistema y usuario dentro del menú lateral | Barra superior con "Las Acacias" a la izquierda y menú de usuario a la derecha | P |
| Marco | "Cerrar sesión" dentro del menú de usuario (nombre ▾) con "Cambiar contraseña" | Botón "Salir" suelto junto al nombre, arriba de todo | Menú desplegable de usuario con "Cambiar contraseña" y "Cerrar sesión" ("Mi perfil" no existe hoy: se omite) | P (+ I si el desplegable no es `<details>` nativo) |
| Marco | Migas de pan y título igual al texto del menú | No hay migas; los enlaces dicen "Volver a Médicos" | Componente de migas ("Catálogos / Especialidades", "Médicos / Ríos, Pablo") | P |
| Menú | Opciones agrupadas por tarea, con rótulo de grupo | Lista plana de enlaces | Grupos Agenda · Catálogos · Administración (tabla de la guía) | P |
| Menú | Opción activa resaltada + `aria-current="page"` | No se marca | Resaltar la sección actual por ruta | P |
| Menú | Ítems de navegación (ícono + texto, sin subrayado, ≥ 40 px) | Hipervínculos subrayados con color fijo en línea | Estilo de ítem de navegación desde la hoja de estilos | P |
| Menú | Contadores de pendientes | No hay | Se deja el hueco en el componente; el primer contador (propuestas) lo trae la 4.3 | C (diferido a 4.3) |
| Menú | Menú ☰ superpuesto bajo 992 px; contraer a íconos | No hay versión para pantallas chicas | Botón ☰ con panel superpuesto (CSS + `<details>` o JS mínimo); contraer: **Conviene**, se omite | P / I |
| Estilos | Hoja de estilos única con tokens (`tokens.css`) | `app.css` es la plantilla de fábrica (azul `#1b6ec2`); cada pantalla repite `style="background-color: #1F5FA8"` | Crear `tokens.css`, personalizar Bootstrap con `--bs-*`, quitar todo `style="…"` y hexadecimales de las vistas | P |
| Estilos | Clases de componente (`.page-header`, `.filter-bar`, `.table-actions`, `.badge-estado--*`) | No existen | Crearlas en la hoja de estilos | P |
| Estilos | Escala tipográfica (28/20/16/14), grilla de 8 px | Tamaños por defecto de Bootstrap; `font-size: 16px` en línea en el layout | Tomar la escala de los tokens | P |
| Accesibilidad | Foco visible, nunca `outline: none` sin reemplazo | `app.css` tiene `h1:focus { outline: none; }` | Quitar la regla o reemplazarla | P |
| Feedback | Notificación de éxito (toast) arriba a la derecha, 4 s, `aria-live` | Alerta verde fija arriba del formulario | Componente de notificación; en SSR se dibuja en el pedido siguiente y se oculta sola con CSS | P (I si se quiere cierre/Deshacer) |
| Datos | Sin datos de prueba visibles en ambientes de usuario | La base de demo (Compose) muestra médicos "Sonda…" con hashes | Limpiar la base de demo y que las sondas de prueba en vivo borren lo que crean | Operativo (no es código de la app) |

## 2. Especialidades (A2)

| Pantalla | Punto de la guía | Estado actual | Cambio necesario | Impacto |
|---|---|---|---|---|
| Especialidades | Título en plural con total ("Especialidades · 18") | Sólo "Especialidades" | Encabezado estándar con total | P |
| Especialidades | Alta como único botón primario "Nueva especialidad"; nunca formulario fijo encima del listado | Formulario de alta fijo arriba de la tabla, botón "Agregar" | Botón "Nueva especialidad" que abre un **modal** de un campo | I (modal) — en SSR puede ser una página propia o `<dialog>` nativo: **D1** |
| Especialidades | Buscador por nombre | No hay | Buscador con placeholder "Buscar por nombre" | P (filtro en memoria: la lista es chica) |
| Especialidades | Filtro Activas / Inactivas / Todas, "Activas" por defecto | Se ven todas mezcladas | Filtro de estado en la barra de filtros, reflejado en la URL | P |
| Especialidades | Filtros activos visibles + "Limpiar filtros" | No hay filtros | Agregar | P |
| Especialidades | Columna "Médicos asociados" (Conviene) y confirmación con la cantidad | No se informa | Contar médicos por especialidad | C |
| Especialidades | Estado como badge con texto, útil sólo con "Todas" | Texto "Activa"/"Desactivada" repetido en cada fila | Badge `Activa` (verde) / `Inactiva` (gris); la columna se ve sólo con "Todas" | P |
| Especialidades | Acciones en la última columna: Editar visible + "⋯" con Desactivar/Reactivar; nunca botón destructivo por fila | Botón "Desactivar" en cada fila | Menú "⋯" por fila con Desactivar o Reactivar | P (menú con `<details>` o JS mínimo) |
| Especialidades | "Editar" (renombrar) | El dominio no permite renombrar una especialidad | **Fuera de alcance**: renombrar es una capacidad nueva | N |
| Especialidades | Confirmación que nombra el registro y la consecuencia, botones con verbo, foco en Cancelar | Desactiva al primer clic, sin confirmar | Diálogo "¿Desactivar Cardiología? N médicos la tienen asignada…" con "Desactivar especialidad" (rojo) y "Cancelar" | I (modal) o página de confirmación en SSR: **D1**; la cantidad es C |
| Especialidades | Bloqueo explicado cuando hay dependencias | Lo hará la 5.3 (turnos futuros); hoy no hay turnos | Sin cambio ahora; el componente de confirmación debe aceptar un "no se puede" | — (heredado por 5.3) |
| Especialidades | Notificación con "Deshacer" (Conviene) | No hay | Se omite (Conviene) | — |
| Especialidades | Estado vacío inicial con acción; "sin resultados" distinto | Vacío: "Agregá la primera con el formulario de arriba" | "Todavía no hay especialidades. Creá la primera." + botón; "No hay especialidades que coincidan con '…'" + "Limpiar filtros" | P |
| Especialidades | Microcopy: "Nueva especialidad", éxito "Especialidad creada" | "Agregar", "Especialidad X agregada." | Textos según la tabla de microcopy | P |
| Especialidades | Tabla: 48 px de fila, separadores suaves, sin bordes verticales; vista de tarjetas en celular | Tabla Bootstrap con ancho máximo en línea; sin vista de celular | Componente de tabla estándar con tarjetas bajo 768 px | P |

## 3. Médicos — listado (A1)

| Pantalla | Punto de la guía | Estado actual | Cambio necesario | Impacto |
|---|---|---|---|---|
| Médicos | "Médicos · 124" con "Nuevo médico" primario a la derecha del título | Botón en un párrafo debajo del título, sin total | Encabezado estándar | P |
| Médicos | Buscador con placeholder "Buscar por apellido, nombre o matrícula" | Etiqueta "Buscar" sin placeholder | Placeholder y etiqueta explicativos | P |
| Médicos | Filtrar mientras se escribe (debounce 300 ms) o con Enter | Con Enter o botón "Buscar" (GET) | Ya cumple con Enter; el filtrado al escribir necesita **D1** | P (Enter) / I (al escribir) |
| Médicos | Filtros por Especialidad y Estado de agenda | No hay | Selects en la barra de filtros | C |
| Médicos | Filtro Activos / Inactivos / Todos | Los médicos no tienen baja lógica | **No aplica** mientras no exista "desactivar médico" | N |
| Médicos | Búsqueda, filtros, orden y página en la URL (Conviene) | La búsqueda ya va en la URL | Extender a filtros, orden y página | P/C |
| Médicos | Orden por columnas con ▲▼ | No hay | Orden por Médico, Matrícula, Especialidad | C |
| Médicos | Paginación desde 25: "Mostrando 1–25 de 124", 25/50/100 | No hay | Componente de pie de paginación | C |
| Médicos | Estado de agenda como badge con color + texto; tabla de estados de la guía | Texto plano; "Sin agenda (rige desde 16/11)" ambiguo; fecha sin año | Badges Vigente (verde) · Programada · desde dd/mm/aaaa (azul) · Pendiente de aprobación (ámbar) · Sin agenda (gris); "Cambia el dd/mm/aaaa" debajo cuando corresponde | P (es traducir `SituacionDeAgenda`, que ya trae los datos) |
| Médicos | Acciones por fila: Editar + "⋯" (Ver agenda, Vincular usuario, Desactivar) | Sólo el enlace del nombre | Columna Acciones con "Editar" y "⋯"; "Ver agenda" llega con 4.4/8.2, "Desactivar" no existe | P (Editar) / N (Desactivar) |
| Médicos | Estado vacío y sin resultados con "Limpiar filtros" | Los dos textos existen, sin enlace "Limpiar filtros" | Agregar el enlace y el formato estándar | P |
| Médicos | Vista en celular como tarjetas | No hay | Componente de tabla estándar | P |

## 4. Médicos — alta y ficha (A1)

| Pantalla | Punto de la guía | Estado actual | Cambio necesario | Impacto |
|---|---|---|---|---|
| Ficha | Detalle en modo lectura con botón "Editar" | Se entra directo a la edición | Vista de lectura; "Editar" muestra el formulario (en SSR: `?modo=edicion` o ruta `/editar`) | P |
| Ficha | Pestañas Datos · Agenda · Usuario del sistema · Historial (Conviene) | Una sola página | Secciones Datos y Usuario ahora; Agenda la trae la 4.4; Historial necesita leer la auditoría por registro | P / N (Historial) |
| Ficha | Badge de estado de agenda al lado del nombre | "Estado de agenda: **Vigente**" en un párrafo | Badge en el encabezado | P |
| Ficha | Pie "Creado por / Modificado por" con fecha | No hay | Leer los dos últimos eventos de auditoría del médico | C |
| Ficha | Sección final "Desactivar médico" | No existe la baja de médicos | **Fuera de alcance** | N |
| Alta y ficha | Migas "Médicos / Ríos, Pablo" en vez de "Volver a Médicos" | "Volver a Médicos" | Componente de migas | P |
| Alta y ficha | Nota "\* Obligatorio" y asterisco en obligatorios | No se marcan | Marcar Nombre, Apellido, Matrícula, Especialidad; Usuario "(opcional)" | P |
| Alta y ficha | Texto de ayuda de formato "Ejemplo: MP 1234" | No hay | Ayuda bajo Matrícula con el formato real que valida el dominio | P |
| Alta y ficha | Ayuda en Usuario del sistema: "Vinculalo para que el médico pueda ver y gestionar su agenda" | "Sin usuario" sin explicación | Texto de ayuda | P |
| Alta y ficha | Select con búsqueda si hay más de 10 opciones | Select común | Especialidades y usuarios pueden superar 10: select con búsqueda | I (**D1**) — sin interactividad, `<input list>` + `<datalist>` |
| Alta y ficha | Foco en el primer campo al abrir | No | `autofocus` en Nombre | P |
| Alta y ficha | Validación al salir del campo (blur) | Sólo al enviar (SSR) | Necesita **D1**; en SSR se cumple sólo "al enviar" + atributos HTML (`required`, `maxlength`) | I |
| Alta y ficha | Error debajo del campo, en rojo y **con ícono**, asociado con `aria-describedby` | Mensaje debajo, sin ícono ni `aria-describedby`; errores de regla en cartel arriba | Componente de campo con ícono y `aria-describedby` | P |
| Alta y ficha | Duplicado probable (mismo apellido y nombre) antes de guardar | Sólo matrícula duplicada (regla del dominio) | Aviso no bloqueante de homónimo | C (aviso) — si bloquea, N |
| Alta y ficha | Botones al pie: "Guardar" primario + "Cancelar" secundario | "Dar de alta" / "Guardar cambios", sin Cancelar | "Guardar" + "Cancelar" (Cancelar vuelve a la lectura o al listado) | P |
| Alta y ficha | Botón deshabilitado con "Guardando…" | No | Necesita JS mínimo o interactividad (**D1**) | I |
| Alta y ficha | Aviso al salir con cambios sin guardar | No | `beforeunload` (JS mínimo) o interactividad (**D1**) | I |
| Alta y ficha | Tras guardar, notificación "Médico creado" / "Médico actualizado" | "Médico dado de alta." / "Cambios guardados." en alerta fija | Textos y componente de notificación | P |
| Alta y ficha | Una columna, etiquetas arriba, ancho máximo 640 px | Una columna con `max-width: 32rem` en línea | Clase del formulario estándar (640 px) | P |

## 4 bis. Usuarios del personal (A4) — sumada por la decisión D4

| Pantalla | Punto de la guía | Estado actual | Cambio necesario | Impacto |
|---|---|---|---|---|
| Usuarios | Título con total y "Nuevo usuario" como único primario | Formulario "Dar de alta" fijo arriba de la tabla | Encabezado estándar; el alta (3 campos) en un modal | P (modal: D1 = A) |
| Usuarios | Buscador "Buscar por usuario o nombre" + filtros Rol y Activos/Inactivos/Todos | No hay | Barra de filtros estándar | P/C |
| Usuarios | Identificador humano como primera columna | Primera columna "Usuario" (el nombre de ingreso) | Primera columna "Nombre" (el modelo guarda el nombre completo en un solo campo, se muestra tal cual); A4 no tiene detalle, así que no es enlace | P |
| Usuarios | Estados como badges con texto | Texto "Activo"/"Desactivado" + líneas sueltas "Bloqueado hasta…", "Debe cambiar la contraseña" | Badges Activo (verde) · Inactivo (gris) · Bloqueado hasta HH:mm (ámbar) · Contraseña temporal (azul) | P |
| Usuarios | Acciones: una visible + "⋯"; nunca destructiva por fila | Hasta 3 botones por fila (Desactivar en rojo, Desbloquear, Nueva contraseña temporal) | "⋯" con Nueva contraseña temporal, Desbloquear, Desactivar/Reactivar; la fila propia sin "⋯" de baja | P |
| Usuarios | Confirmación que nombra el registro y la consecuencia | Desactivar y "Nueva contraseña temporal" actúan al primer clic | Diálogo: "¿Desactivar a ana.ruiz? No va a poder ingresar." y "¿Generar otra contraseña para ana.ruiz? La actual deja de servir." | P (D1 = A) |
| Usuarios | Paginación y orden | No hay | Pie estándar y orden por Nombre, Rol | C |
| Usuarios | Contraseña temporal mostrada una vez | Alerta verde con `<code>` | Se mantiene en un diálogo propio (no un toast que se va a los 4 s), con "Copiar" | P |
| Usuarios | Microcopy | "Dar de alta", "Desactivado" | "Nuevo usuario", "Inactivo", "Usuario creado" | P |

Ojo: las stories pendientes **2.4** (email en A4) y **2.5** (dos administradores a la vez en A4) tocan esta
pantalla; se escriben sobre la versión migrada.

## 5. Resumen

| Impacto | Cantidad de brechas | Qué decide |
|---|---|---|
| P — sólo presentación | ~45 | Entran directo en la story correctiva |
| I — interactividad | 7 | Decisión **D1** |
| C — consulta nueva de lectura | 8 | Decisión **D2** |
| N — capacidad de negocio nueva | 5 | Decisión **D3** |

## 6. Decisiones que necesito antes del paso 2

**D1 — ¿Cómo se cumplen los puntos que piden interactividad?** La arquitectura (§4) dice que el
personal usa Blazor **interactivo en el servidor**, pero desde la 2.1 todas las pantallas del personal
quedaron en SSR estático (la cookie de ingreso lo exige en S0, y nadie las pasó a interactivas
después). Opciones:

- **A.** Pantallas ABM interactivas en el servidor (como dice la arquitectura): modal, toast, blur,
  "Guardando…", aviso de cambios y búsqueda al escribir en C#. Cambia cómo se prueban (bUnit con
  eventos) y obliga a resolver la renovación de sesión en circuito, que la 2.1 dejó para la 5.5.
- **B.** Seguir en SSR estático y agregar un **único archivo JavaScript chico** del sistema para
  "Guardando…", aviso de cambios, filtrar al escribir y `<dialog>` nativo. Menos cambio, pero suma
  un segundo lenguaje.
- **C.** SSR estático puro: lo que pide interactividad se resuelve con páginas (confirmación en
  página propia, alta en página propia) y se declara como excepción a la guía.

> **Decidido por el docente (2026-10-17): opción A.** Las pantallas ABM del personal pasan a
> interactivas en el servidor; el ingreso (S0) sigue estático por la cookie.

**D2 — ¿Una consulta nueva de lectura (contar médicos por especialidad, filtrar, ordenar, paginar,
"creado por") entra en "sin tocar la lógica de negocio"?** Agrega métodos de lectura en Aplicación e
Infraestructura, sin reglas nuevas.

**D3 — Las capacidades nuevas (renombrar especialidad, desactivar médico, historial por registro,
filtro Activos/Inactivos de médicos). Los duplicados sin tildes ya los cumple la 3.1:** ¿quedan fuera de la correctiva como
stories nuevas en el backlog, o se descartan?

**D4 — Alcance de pantallas:** la guía alcanza "toda pantalla ABM". Además de A1 y A2 existe
**A4 Usuarios del personal** (2.2/2.3), que es ABM y tiene los mismos problemas, y **M2 Mi propuesta**
(4.2), que no es ABM pero usa formularios. ¿Entran en la correctiva?

> **Decidido por el docente (2026-10-17):** **D2** sí, las consultas de sólo lectura entran en la
> correctiva. **D3** renombrar especialidad, desactivar médico e historial por registro van al backlog
> como stories nuevas. **D4** la correctiva cubre A1, A2 y A4 (M2 queda afuera).

## 7. Choques entre la guía y la UX aprobada (`03-ux.md`)

| Tema | Guía | `03-ux.md` | Propuesta |
|---|---|---|---|
| Formato de fecha | dd/mm/aaaa siempre | §2: "Martes 10 de noviembre, 10:30. Nunca `10/11`" | dd/mm/aaaa en el personal; fecha larga en la web del paciente |
| Desactivar especialidad | Dentro del menú "⋯" con confirmación | §7 A2: botón "Desactivar" deshabilitado con el motivo al pasar el mouse | Manda la guía; el bloqueo se explica en el diálogo |
| Color de cancelado por el hospital | Ámbar `--color-atencion` = `#b26a00` | Naranja `#B45309` | Un solo token; verificar contraste AA |
| Tipografía | "Inter" | Sans serif del sistema | Sans serif del sistema (no se cargan fuentes externas) |
| Alcance | ABM del personal | La web del paciente tiene sus propios principios (un paso por pantalla, sin JavaScript) | La guía rige el sistema del personal; la web del paciente sólo toma tokens, microcopy y accesibilidad |

> **Decidido por el docente (2026-10-17):** fechas dd/mm/aaaa en el personal y fecha larga en la web
> del paciente; token único `#B45309` para atención/cancelado por el hospital; sans serif del sistema;
> **la guía rige completa también la web del paciente**. Desactivar especialidad sigue la guía (menú "⋯"
> + diálogo), porque la guía es obligatoria.

## 8. Patrones que las stories pendientes necesitan y la guía no cubre

| Patrón | Stories que lo necesitan | Qué falta definir |
|---|---|---|
| Comparación lado a lado (vigente vs. propuesta, con cambios marcados) | 4.3 (A6) | Cómo se marca un cambio, color y texto |
| Rechazo con motivo obligatorio (diálogo con campo de texto) | 4.3, 4.4 | Si el diálogo de confirmación admite campos y validación |
| Grilla semanal de franjas horarias (editor y lectura) | 4.2 (hecha), 4.4, A1 pestaña Agenda | Representación de la semana, franjas, duración, celular |
| **Vista de agenda / calendario del día** (lista de turnos por hora, huecos libres) | 8.1 (M1), 8.2 (R2) | Todo: vista de día/semana, estados de turno, navegación entre días |
| Hoja para imprimir (A4) | 8.2 | Estilos de impresión |
| **Pantalla operativa "todo en una"** con teclado (buscar paciente → horarios → reservar) | 5.1, 5.5 (R1) | La guía asume listado + formulario separados; R1 junta varios paneles y atajos (Enter/Esc) |
| Búsqueda por identificador exacto (DNI) con "no existe → crear" | 5.1 | Flujo buscar-o-crear |
| Selector de horarios libres (primeros 10, por médico o especialidad) | 5.2, 5.5, 6.4 | Presentación de disponibilidad |
| Asistente en pasos con vista previa de afectados y confirmación escribiendo una palabra | 7.1 | La guía lo menciona (Conviene, "escribir el nombre") pero no el asistente ni la vista previa |
| Lista de trabajo con acción por fila que se marca hecha ("Marcar avisado", "Darle turno") | 7.3 (R3) | Estado "hecho" de la fila, contador en el menú |
| Filtro por rango de fechas y tipo de acción | 9.1 (A5) | Control de rango de fechas |
| Estados de turno (Reservado, Cancelado por el paciente, Cancelado por el hospital) como badges | 5.x, 6.3, 8.x | La tabla de estados del turno con sus colores de la guía |
| Web del paciente (celular, un paso por pantalla, sin JavaScript) | 6.1 a 6.6 | Si la guía aplica y cuánto (ver §7) |
| Campo de código de un solo uso | 6.1 | Formato y accesibilidad del código |
