# Guía de estándar UX/UI para pantallas ABM

Oct 17, 2026 · @Fernando Bono

## Propósito y alcance

Esta guía define cómo se construye toda pantalla de ABM (alta, baja, modificación y consulta) del sistema, para que cada entidad se use igual, se aprenda una sola vez y no exponga al usuario a errores evitables.

Aplica a listados, formularios de alta y edición, pantallas de detalle y acciones de desactivación. Es normativa: lo marcado como **Debe** es obligatorio para aprobar un desarrollo; lo marcado como **Conviene** es recomendado.

Cada regla se apoya en un principio de usabilidad conocido, para que el equipo entienda el porqué y pueda resolver casos que la guía no prevé.

## Diagnóstico de las pantallas actuales

Las tres pantallas funcionan, pero resuelven el mismo problema de tres formas distintas y dejan al usuario sin feedback, sin atajos y con acciones destructivas demasiado expuestas.

| Pantalla | Problema observado | Principio afectado | Gravedad |
| --- | --- | --- | --- |
| Médicos (listado) | El campo "Buscar" no dice qué busca (¿nombre, matrícula, especialidad?) ni tiene placeholder | Reconocer antes que recordar | Media |
| Médicos (listado) | No hay filtros por especialidad ni por estado de agenda, que son las preguntas más frecuentes | Flexibilidad y eficiencia | Alta |
| Médicos (listado) | No hay contador de resultados, paginación ni orden por columna | Visibilidad del estado del sistema | Media |
| Médicos (listado) | El estado de agenda es texto plano; "Sin agenda (rige desde 16/11)" es ambiguo: no queda claro si algo empieza o termina | Lenguaje del usuario | Alta |
| Médicos (listado) | La única acción es el link del nombre; no se ven Editar ni Desactivar | Visibilidad, affordance | Media |
| Médicos (listado) | Conviven datos de prueba ("Sonda4E653A4A", hashes) con datos reales | Confianza, prevención de errores | Alta |
| Detalle de médico | Se entra directo en modo edición: no hay vista de solo lectura | Prevención de errores | Media |
| Detalle de médico | No hay botón Cancelar ni aviso al salir con cambios sin guardar | Control y libertad del usuario | Alta |
| Detalle de médico | No se marcan campos obligatorios ni el formato esperado de la matrícula | Prevención de errores | Media |
| Detalle de médico | "Estado de agenda" se muestra pero no ofrece cómo gestionarla | Visibilidad, próximos pasos | Media |
| Detalle de médico | "Usuario del sistema: Sin usuario" no explica para qué sirve vincularlo | Ayuda y documentación | Baja |
| Especialidades | Un botón rojo "Desactivar" en cada fila: ruido visual y riesgo de clic accidental | Prevención de errores, ley de Fitts | Alta |
| Especialidades | El alta inline compite con el listado y difiere del patrón de Médicos (botón "Nuevo médico") | Consistencia y estándares | Media |
| Especialidades | No hay búsqueda, edición del nombre ni forma de ver o reactivar las inactivas | Control y libertad del usuario | Alta |
| Especialidades | La columna Estado repite "Activa" en todas las filas y no aporta información | Diseño minimalista | Baja |
| Especialidades | No se informa cuántos médicos usan cada especialidad antes de desactivarla | Prevención de errores | Alta |

## Principios de usabilidad que rigen el estándar

Toda decisión de esta guía se justifica con uno de estos principios; ante un caso no previsto, se resuelve con ellos.

| Principio | Qué exige en un ABM |
| --- | --- |
| Visibilidad del estado del sistema (Nielsen 1) | Cada acción muestra resultado: cargando, guardado, error. El listado dice cuántos registros hay y qué filtro está aplicado. |
| Correspondencia con el mundo real (Nielsen 2) | Términos del negocio, no de la base de datos. Estados con nombres que el usuario usaría en voz alta. |
| Control y libertad del usuario (Nielsen 3) | Siempre hay Cancelar, Volver y Deshacer. Lo desactivado se puede reactivar. |
| Consistencia y estándares (Nielsen 4, ley de Jakob) | Todas las entidades usan el mismo layout, los mismos verbos y las mismas posiciones de botones. |
| Prevención de errores (Nielsen 5) | Validar antes de enviar, confirmar lo destructivo, impedir duplicados, mostrar dependencias. |
| Reconocer antes que recordar (Nielsen 6) | Placeholders, selects con búsqueda, filtros visibles, datos clave en el listado. |
| Flexibilidad y eficiencia (Nielsen 7) | Búsqueda instantánea, filtros guardados en la URL, atajos de teclado, Enter para enviar. |
| Diseño estético y minimalista (Nielsen 8) | Solo la información que ayuda a decidir; nada repetido en cada fila. |
| Ayuda a reconocer y recuperarse de errores (Nielsen 9) | Mensajes junto al campo, en lenguaje claro, diciendo cómo corregir. |
| Ayuda y documentación (Nielsen 10) | Texto de ayuda breve bajo los campos no obvios. |
| Proximidad y similitud (Gestalt) | Lo relacionado va junto; acciones del mismo tipo comparten estilo. |
| Ley de Fitts | Acciones frecuentes grandes y cerca; acciones peligrosas lejos y pequeñas. |
| Ley de Hick | Pocas opciones visibles a la vez; el resto, en un menú de más acciones. |
| Jerarquía visual | Un único botón primario por vista; el ojo debe saber dónde ir primero. |

## Patrón estándar de pantalla de listado

Todo listado tiene cuatro zonas fijas, siempre en este orden: encabezado, barra de filtros, tabla y pie de paginación.

**1. Encabezado**

- **Debe** mostrar el título en plural ("Médicos") y, a su lado, el total: "Médicos · 124".
- **Debe** ubicar la acción de alta como único botón primario, alineado a la derecha del título: "Nuevo médico".
- **Conviene** sumar acciones secundarias (Exportar, Importar) como botones secundarios o dentro de un menú "Más acciones".

**2. Barra de filtros**

- **Debe** tener un buscador con placeholder que diga qué busca: "Buscar por apellido, nombre o matrícula".
- **Debe** filtrar mientras se escribe (con debounce de 300 ms) o con Enter; el botón "Buscar" es opcional.
- **Debe** ofrecer filtros por los atributos que el usuario consulta seguido (en Médicos: Especialidad y Estado de agenda), como selects junto al buscador.
- **Debe** mostrar los filtros activos y un enlace "Limpiar filtros".
- **Debe** incluir un filtro de estado "Activos / Inactivos / Todos" en toda entidad con baja lógica, con "Activos" por defecto.
- **Conviene** reflejar búsqueda, filtros, orden y página en la URL, para poder compartir y volver con el botón Atrás.

**3. Tabla**

- **Debe** usar como primera columna el identificador humano del registro ("Apellido, Nombre"), como enlace al detalle.
- **Debe** alinear texto a la izquierda, números y montos a la derecha, y fechas en formato dd/mm/aaaa.
- **Debe** mostrar estados como badges con texto y color, nunca solo color.
- **Debe** permitir ordenar por columnas clave, con un indicador de orden (▲▼) en el encabezado.
- **Debe** concentrar las acciones por fila en la última columna: una acción frecuente visible (Editar) y el resto en un menú "⋯". Las acciones destructivas nunca se muestran como botón en cada fila.
- **Conviene** fijar el encabezado de la tabla al hacer scroll y resaltar la fila al pasar el mouse.
- **Conviene** mostrar columnas derivadas útiles para decidir: en Especialidades, la cantidad de médicos asociados.

**4. Pie**

- **Debe** paginar a partir de 25 registros, con texto "Mostrando 1–25 de 124" y selector de 25 / 50 / 100 por página.
- **Debe** paginar del lado del servidor cuando la entidad pueda superar los 500 registros.

## Patrón estándar de alta y edición

El formulario se elige por tamaño de la entidad y siempre ofrece guardar, cancelar y corregir sin perder datos.

**Dónde vive el formulario**

| Tamaño de la entidad | Contenedor | Ejemplo |
| --- | --- | --- |
| 1 a 3 campos simples | Modal o panel lateral, abierto desde el botón "Nueva …" | Especialidad |
| 4 a 12 campos | Página propia | Médico |
| Más de 12 campos o pasos dependientes | Página con secciones o asistente por pasos | Agenda del médico |

El alta nunca se coloca como formulario fijo encima del listado: empuja la tabla hacia abajo y compite con ella.

**Detalle frente a edición**

- **Debe** abrir el detalle en modo lectura, con un botón "Editar". Así se evita modificar por accidente al solo consultar.
- **Conviene** organizar el detalle en pestañas o secciones (Datos, Agenda, Usuario, Historial) cuando la entidad tiene subrecursos.
- **Debe** mostrar al pie "Creado por / Modificado por" con fecha, para trazabilidad.

**Campos**

- **Debe** usar una sola columna, con la etiqueta arriba del campo y un ancho máximo de 640 px.
- **Debe** ordenar los campos como el usuario piensa la entidad: identidad, luego clasificación, luego configuración.
- **Debe** marcar los obligatorios con asterisco y una nota "\* Obligatorio" al inicio; si casi todos lo son, marcar los opcionales con "(opcional)".
- **Debe** indicar formatos con texto de ayuda debajo del campo: "Ejemplo: MP 1234".
- **Debe** usar el control adecuado: select con búsqueda cuando hay más de 10 opciones, radios cuando hay 2 a 4, interruptor para sí/no.
- **Debe** poner el foco en el primer campo al abrir el formulario.

**Validación**

- **Debe** validar cada campo al salir de él (blur) y todo el formulario al enviar.
- **Debe** mostrar el error debajo del campo, en rojo y con ícono, diciendo cómo corregirlo: "La matrícula ya está registrada para Pereyra, Martín José".
- **Debe** validar en el servidor también; el cliente es solo comodidad.
- **Debe** detectar duplicados probables (mismo apellido y nombre, misma matrícula) antes de guardar.

**Botones y envío**

- **Debe** ubicar los botones al pie: primario "Guardar" y secundario "Cancelar", siempre en el mismo orden en todo el sistema.
- **Debe** deshabilitar el botón y mostrar "Guardando…" mientras se procesa, para evitar dobles envíos.
- **Debe** avisar si el usuario intenta salir con cambios sin guardar.
- **Debe**, tras guardar, volver al detalle o al listado y mostrar una notificación: "Médico actualizado".

## Baja, desactivación y acciones destructivas

La baja es lógica por defecto, se pide de forma deliberada, informa sus consecuencias y se puede revertir.

- **Debe** usar baja lógica (Activo / Inactivo) en toda entidad referenciada por otras. La eliminación física queda reservada a registros sin dependencias y a perfiles administradores.
- **Debe** ubicar "Desactivar" dentro del menú "⋯" de la fila o en el detalle, en una sección final separada. Nunca como botón rojo repetido en cada fila.
- **Debe** pedir confirmación en un diálogo que nombre el registro y la consecuencia: "¿Desactivar Cardiología? 12 médicos la tienen asignada; no podrá elegirse en nuevos médicos."
- **Debe** rotular los botones del diálogo con el verbo, no con Sí/No: "Desactivar especialidad" (rojo) y "Cancelar" (secundario, con el foco por defecto).
- **Debe** bloquear la baja cuando rompe una regla de negocio y explicar por qué: "No se puede desactivar: hay turnos futuros asignados".
- **Debe** ofrecer "Reactivar" para los registros inactivos, visibles con el filtro de estado.
- **Conviene** mostrar una notificación con "Deshacer" durante 5 a 8 segundos tras desactivar.
- **Conviene**, en bajas masivas o irreversibles, pedir que el usuario escriba el nombre del registro para confirmar.

El color rojo se reserva para la acción destructiva confirmada y para errores. Usarlo en cada fila lo vuelve ruido y le quita fuerza de alerta.

## Estados, estados vacíos y feedback

El usuario nunca debe preguntarse si algo pasó: cada estado del sistema y de los datos tiene una representación definida.

**Estados de negocio como badges**

Cada entidad con estados publica su tabla de estados: nombre visible, color, significado y si admite acción. Ejemplo propuesto para el estado de agenda:

| Estado visible | Color del badge | Significado | Reemplaza a |
| --- | --- | --- | --- |
| Vigente | Verde | Tiene agenda activa hoy | "Vigente" |
| Programada · desde 16/11/2026 | Azul | Tiene agenda aprobada que empieza en esa fecha | "Sin agenda (rige desde 16/11)" |
| Pendiente de aprobación | Ámbar | Hay una propuesta de agenda esperando revisión | "Propuesta pendiente" |
| Sin agenda | Gris | No tiene agenda cargada ni programada | "Sin agenda" |

Si un médico tiene agenda vigente y otra programada, se muestra la vigente y debajo, en texto pequeño, "Cambia el 16/11/2026".

**Estados de la interfaz**

- **Carga:** esqueletos de filas (skeleton) en tablas; spinner solo en botones.
- **Vacío inicial:** ilustración o ícono, una frase y la acción: "Todavía no hay especialidades. Creá la primera." con el botón "Nueva especialidad".
- **Sin resultados:** "No hay médicos que coincidan con 'Pereira'" y un enlace "Limpiar filtros". Es distinto del vacío inicial.
- **Error de carga:** mensaje claro y botón "Reintentar"; nunca una pantalla en blanco ni un stack trace.
- **Éxito:** notificación (toast) arriba a la derecha, 4 segundos, con el verbo en pasado: "Especialidad creada".
- **Error al guardar:** el formulario conserva lo cargado y muestra el error junto al campo o en un resumen arriba del formulario.

## Diseño visual

La base visual se toma de Bootstrap 5, que el sistema ya usa, y se fija en tokens para que todas las pantallas se vean iguales.

**Jerarquía de botones**

| Nivel | Estilo | Uso | Cantidad por vista |
| --- | --- | --- | --- |
| Primario | Relleno, color de marca | La acción principal (Nuevo, Guardar) | Una |
| Secundario | Borde, sin relleno | Alternativas (Cancelar, Exportar, Buscar) | Pocas |
| Terciario | Enlace | Navegación y acciones menores (Volver, Limpiar filtros) | Libre |
| Peligro | Relleno rojo | Solo dentro del diálogo de confirmación | Una |

**Espaciado y tipografía**

- Grilla de 8 px: márgenes y separaciones en 8, 16, 24, 32 px.
- Escala tipográfica fija: título de página 28 px, subtítulo 20 px, cuerpo 16 px, texto de ayuda 14 px.
- Separar el encabezado de la página de su contenido con 24 px como mínimo; agrupar campos relacionados con 16 px y separar grupos con 32 px (proximidad).

**Color**

- Colores semánticos únicos: verde = éxito o vigente, ámbar = pendiente o atención, rojo = error o peligro, azul = información o programado, gris = inactivo o neutro.
- El color nunca es el único portador del significado: siempre va acompañado de texto o ícono.

**Tablas**

- Altura de fila de 48 px; densidad compacta de 36 px opcional para listados largos.
- Separadores horizontales suaves, sin bordes verticales.

**Hoja de estilos única y tokens de diseño**

Todo el aspecto visual sale de una sola hoja de estilos del sistema; ninguna vista define colores, tamaños ni tipografías por su cuenta.

- **Debe** existir un archivo de tokens (`tokens.css` o `_variables.scss`) con colores, tipografía, espaciados, radios y sombras como variables CSS.
- **Debe** personalizarse Bootstrap a partir de esos tokens (variables Sass o `--bs-*`), no sobrescribiendo clases en cada pantalla.
- **Debe** prohibirse el estilo inline (`style="…"`) y los colores en hexadecimal fuera del archivo de tokens; se controla en la revisión de código o con Stylelint.
- **Debe** haber clases de componente reutilizables para lo que se repite: `.badge-estado--vigente`, `.page-header`, `.filter-bar`, `.table-actions`.
- **Conviene** publicar una página interna de referencia ("style guide" viva) que muestre cada componente con su código, y versionar la hoja de estilos.

```css
:root {
  /* Color de marca y semánticos */
  --color-primario: #1f5fa8;
  --color-exito: #1e7e34;
  --color-atencion: #b26a00;
  --color-peligro: #c62828;
  --color-info: #1565c0;
  --color-neutro: #6c757d;

  /* Tipografía */
  --fuente-base: "Inter", system-ui, sans-serif;
  --texto-titulo: 1.75rem;   /* 28 px */
  --texto-subtitulo: 1.25rem; /* 20 px */
  --texto-cuerpo: 1rem;      /* 16 px */
  --texto-ayuda: 0.875rem;   /* 14 px */

  /* Espaciado (grilla de 8 px) */
  --esp-1: 0.5rem;
  --esp-2: 1rem;
  --esp-3: 1.5rem;
  --esp-4: 2rem;

  --radio: 0.5rem;
}

.badge-estado--vigente   { background: color-mix(in srgb, var(--color-exito) 15%, white);   color: var(--color-exito); }
.badge-estado--pendiente { background: color-mix(in srgb, var(--color-atencion) 15%, white); color: var(--color-atencion); }
```

Los valores de color del ejemplo son ilustrativos: se reemplazan por la paleta institucional y se verifica su contraste AA.

## Accesibilidad y responsive

El nivel mínimo exigido es WCAG 2.2 AA: una pantalla que no lo cumple no se considera terminada.

**Accesibilidad**

- **Debe** tener contraste de 4,5:1 en texto normal y 3:1 en texto grande y controles.
- **Debe** asociar cada etiqueta a su campo (`<label for>`) y cada error al campo con `aria-describedby`.
- **Debe** poder operarse completo con teclado: Tab en orden lógico, Enter para enviar, Esc para cerrar diálogos.
- **Debe** mostrar el foco con un contorno visible; nunca `outline: none` sin reemplazo.
- **Debe** anunciar notificaciones con `aria-live` y devolver el foco al elemento que abrió un diálogo al cerrarlo.
- **Debe** tener áreas de clic de al menos 44 × 44 px en táctil y 24 × 24 px en escritorio.
- **Debe** dar nombre accesible a los botones de solo ícono (`aria-label="Más acciones para Ríos, Pablo"`).

**Responsive**

- Desde 992 px: tabla completa.
- Entre 768 y 991 px: se ocultan columnas secundarias; los filtros pasan a un panel desplegable.
- Menos de 768 px: cada fila se convierte en tarjeta con el nombre, el badge de estado y el menú "⋯"; el botón de alta queda fijo abajo.
- Los formularios ocupan el 100 % del ancho en celular y los botones se apilan con el primario arriba.

## Microcopy y nomenclatura

Los mismos verbos y formatos en todo el sistema hacen que el usuario no tenga que reaprender cada pantalla.

| Situación | Texto estándar | Evitar |
| --- | --- | --- |
| Crear | "Nuevo médico" / "Nueva especialidad" | "Agregar", "Alta", "Crear registro" mezclados |
| Guardar edición | "Guardar" | "Guardar cambios" en unas pantallas y "Aceptar" en otras |
| Salir sin guardar | "Cancelar" | "Cerrar", "Volver" como sinónimos |
| Baja lógica | "Desactivar" / "Reactivar" | "Eliminar" cuando no se elimina |
| Volver al listado | "← Médicos" (migas de pan) | "Volver a Médicos" suelto |
| Confirmación | "¿Desactivar Cardiología?" | "¿Está seguro?" |
| Éxito | "Médico creado" | "Operación exitosa" |
| Error | "Ingresá una matrícula. Ejemplo: MP 1234" | "Campo inválido", "Error 500" |

**Formatos**

- Personas: "Apellido, Nombre" en listados; "Nombre Apellido" en textos corridos.
- Fechas: dd/mm/aaaa siempre con año; fechas relativas ("hace 2 días") solo con la fecha exacta en tooltip.
- Mayúscula solo al inicio de títulos y botones ("Nuevo médico", no "Nuevo Médico").
- Un mismo tratamiento en todo el sistema: voseo o usted, sin mezclar.

**Datos de prueba**

Los ambientes que ven usuarios (demo, UAT, producción) no deben mostrar registros generados por tests automáticos con hashes en el nombre. Los tests limpian lo que crean o corren en una base aislada; los datos de demostración usan nombres verosímiles.

## Navegación y menú principal

El menú debe decirle al usuario dónde está, qué puede hacer y dónde encontrar cada cosa, con las opciones agrupadas por tarea y no en una lista plana de enlaces.

**Diagnóstico del menú actual**

| Problema observado | Principio afectado | Gravedad |
| --- | --- | --- |
| No se marca la opción activa: estando en Especialidades, el menú se ve igual | Visibilidad del estado del sistema | Alta |
| Ocho enlaces en una lista plana, sin grupos | Ley de Hick, proximidad (Gestalt) | Media |
| Lo relacionado con agenda está disperso: Propuestas de agenda, Bloqueos de agenda y Agendas aparecen separados | Proximidad, modelo mental | Media |
| Las opciones lucen como hipervínculos subrayados, no como navegación | Consistencia y estándares | Baja |
| "Salir" está junto al nombre, arriba de todo y al alcance de un clic accidental | Prevención de errores, ley de Fitts | Media |
| No hay indicadores de trabajo pendiente (propuestas a revisar, pacientes a avisar) | Visibilidad del estado del sistema | Media |
| No hay versión para pantallas chicas ni opción de contraer | Flexibilidad y eficiencia | Media |

**Estructura de la aplicación**

- **Debe** usar un marco fijo: barra superior con el nombre del sistema a la izquierda y el menú de usuario a la derecha; menú lateral a la izquierda; contenido a la derecha.
- **Debe** ubicar "Cerrar sesión" dentro del menú de usuario (nombre ▾), junto con "Mi perfil" y "Cambiar contraseña".
- **Debe** mostrar migas de pan en el contenido ("Catálogos / Especialidades") y que el título de la página coincida con el texto del menú.

**Menú lateral**

- **Debe** agrupar las opciones por tarea, con un rótulo por grupo y no más de 7 opciones por grupo.
- **Debe** ordenar los grupos por frecuencia de uso: primero la operación diaria, al final la administración.
- **Debe** resaltar la opción activa con fondo, barra lateral de color y texto en negrita, y marcarla con `aria-current="page"`.
- **Debe** mostrar cada opción como un ítem de navegación (ícono + texto, sin subrayado) con un área de clic de al menos 40 px de alto.
- **Debe** mostrar solo las opciones que el rol del usuario puede usar; nunca opciones que llevan a "acceso denegado".
- **Conviene** mostrar contadores en las opciones con trabajo pendiente ("Propuestas de agenda 3").
- **Conviene** permitir contraer el menú a solo íconos (con el texto como tooltip) y recordar esa preferencia.
- **Debe**, en pantallas de menos de 992 px, ocultar el menú detrás de un botón ☰ que lo abre como panel superpuesto.

**Agrupación propuesta para Las Acacias**

| Grupo | Opciones |
| --- | --- |
| Agenda | Agendas · Propuestas de agenda · Bloqueos de agenda · Pacientes para avisar |
| Catálogos | Médicos · Especialidades |
| Administración | Usuarios del personal · Auditoría |

&#91;embedded content: Maqueta · marco de la aplicación y menú lateral\]

El menú de usuario se muestra abierto para ver qué contiene; los contadores 3 y 5 son de ejemplo. Los cuadrados representan íconos a definir.

## Aplicación: rediseño de las tres pantallas

Aplicando el estándar, las tres pantallas pasan a compartir el mismo esqueleto y solo cambian sus columnas y campos.

**Médicos (listado)**

1. Encabezado: "Médicos · 124" a la izquierda y "Nuevo médico" (primario) a la derecha.
2. Filtros en una línea: buscador "Buscar por apellido, nombre o matrícula", select Especialidad (con búsqueda), select Estado de agenda, select Activos/Inactivos/Todos y "Limpiar filtros".
3. Columnas: Médico (enlace) · Matrícula · Especialidad · Estado de agenda (badge) · Acciones (Editar + "⋯" con Ver agenda, Vincular usuario, Desactivar).
4. Pie: "Mostrando 1–25 de 124" y paginación.

&#91;embedded content: Maqueta · listado de Médicos\]

Los números marcan las cuatro zonas fijas de la lista anterior. Las matrículas son de ejemplo; el menú ⋯ agrupa Ver agenda, Vincular usuario y Desactivar.

**Detalle de médico**

1. Migas de pan "Médicos / Ríos, Pablo" y título con badge de estado de agenda al lado del nombre.
2. Pestañas: Datos · Agenda · Usuario del sistema · Historial.
3. Pestaña Datos en modo lectura, con botón "Editar"; al editar aparecen "Guardar" y "Cancelar" al pie.
4. Matrícula con ayuda "Ejemplo: MP 1234" y validación de duplicados.
5. Usuario del sistema con ayuda: "Vinculalo para que el médico pueda ver y gestionar su agenda".
6. Al final de Datos, sección "Desactivar médico" separada, con explicación de la consecuencia.

**Especialidades**

1. Encabezado: "Especialidades · 18" y "Nueva especialidad" (primario), que abre un modal de un campo.
2. Filtros: buscador por nombre y select Activas/Inactivas/Todas.
3. Columnas: Nombre · Médicos asociados · Estado (badge, útil cuando se filtra "Todas") · Acciones (Editar + "⋯" con Desactivar o Reactivar).
4. Desactivar abre la confirmación con la cantidad de médicos afectados.
5. El alta valida nombres duplicados sin distinguir mayúsculas ni tildes ("Cardiologia" = "Cardiología").

&#91;embedded content: Maqueta · Especialidades y confirmación de baja\]

Ya no hay un botón rojo por fila: la baja se pide desde el menú ⋯ y el diálogo informa cuántos médicos quedan afectados. Las cantidades son de ejemplo.

## Checklist de revisión para cada ABM

Un ABM se aprueba cuando cumple todos estos puntos; se usa en la revisión de código y en las pruebas de aceptación.

**Listado**

- [ ] Título en plural con total de registros
- [ ] Un único botón primario "Nuevo …"
- [ ] Buscador con placeholder que indica los campos buscados
- [ ] Filtros por los atributos más consultados y filtro Activos/Inactivos/Todos
- [ ] Filtros, orden y página reflejados en la URL
- [ ] Estados como badges con texto
- [ ] Acciones por fila en la última columna; las destructivas dentro del menú "⋯"
- [ ] Paginación con "Mostrando x–y de z"
- [ ] Estado vacío inicial y estado sin resultados diferenciados

**Formulario y detalle**

- [ ] Detalle en modo lectura con botón "Editar"
- [ ] Una columna, etiquetas arriba, obligatorios marcados
- [ ] Texto de ayuda en campos con formato
- [ ] Validación al salir del campo y en el servidor, con mensajes que dicen cómo corregir
- [ ] "Guardar" y "Cancelar" al pie, en el orden estándar
- [ ] Botón deshabilitado mientras guarda
- [ ] Aviso al salir con cambios sin guardar
- [ ] Notificación de éxito tras guardar

**Baja**

- [ ] Baja lógica con opción de reactivar
- [ ] Confirmación que nombra el registro y la consecuencia
- [ ] Botones del diálogo con verbo, foco en Cancelar
- [ ] Bloqueo explicado cuando hay dependencias

**Accesibilidad y presentación**

- [ ] Contraste AA verificado
- [ ] Operable completo con teclado y foco visible
- [ ] Vista en celular como tarjetas
- [ ] Textos según la tabla de microcopy
- [ ] Sin datos de prueba visibles en ambientes de usuario

**Navegación**

- [ ] Opción activa resaltada y con `aria-current="page"`
- [ ] La pantalla nueva está en el grupo de menú que le corresponde
- [ ] Título de página igual al texto del menú y migas de pan presentes
- [ ] Visible solo para los roles autorizados
- [ ] Contador de pendientes, si la pantalla tiene trabajo por resolver
