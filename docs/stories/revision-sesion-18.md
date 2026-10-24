---
documento: Revisión de las stories 3.4, 3.5 y 9.2
proyecto: Gestión de turnos — Hospital Municipal Las Acacias (caso simulado)
fecha: 2026-10-23
alcance: 3.4 (renombrar especialidad, S3), 3.5 (desactivar y reactivar médico, S3), 9.2 (historial por registro, S6)
contexto: guía UX/UI (docs/ux/guia-ux-ui-abm.md), análisis de brechas (decisión D3), 3.3 y sus componentes comunes, 00-contrato-tecnico.md, 4.3 y 4.4 (ready-for-dev), código de main en 7951d55
---

# Revisión de las stories 3.4, 3.5 y 9.2

Las tres se escribieron en paralelo (un agente por story, con un brief común) y cada una pasó por el
validador de stories y por una revisión adversarial antes de programar. Orden del sprint 3 decidido por
el docente en esta sesión: **3.4 → 4.3 → 4.4 → 3.5 → 5.1**. La 9.2 va en el S6, después de la 9.1.

## Resumen

| Story | AC | Pruebas | Validador | Revisión adversarial | Preguntas |
|---|---|---|---|---|---|
| 3.4 Renombrar especialidad | 37 | 32 | 2 avisos, corregidos | 4 ajustes, ninguno bloquea | 5 (S-40 a S-44) |
| 3.5 Desactivar y reactivar médico | 33 | 36 | 2 avisos, corregidos | 1 bloqueante (mecánico) + 4 ajustes | 7 (S-50 a S-56) + 1 nueva (S-57) |
| 9.2 Historial por registro | 29 | 23 | 2 avisos, corregidos | 1 bloqueante (= S-60) + 5 ajustes | 6 (S-60 a S-65) |

**Hallazgo transversal:** el brief daba por hecho que A1 y A2 tenían concurrencia optimista (`xmin`). No la
tienen: la 3.2 aceptó "gana el último" y sólo `agendas_medico` usa `xmin`. Por eso la 3.4 (S-40) y la 3.5
(S-56) lo plantean como decisión, con la misma recomendación.

**Avisos del validador (ya corregidos):** enlaces de vuelta `followed_up_by` en la 3.1 (← 3.4), la 3.2
(← 3.5) y la 3.3 (← 9.2); citas por número de línea reemplazadas por el miembro del código.

## Ajustes de la revisión adversarial (se aplican sin decisión)

| # | Story | Qué falta | Arreglo |
|---|---|---|---|
| A-1 | 3.4 | Con la pregunta "Tenés cambios sin guardar" abierta, Esc la cierra y el modal de afuera la vuelve a abrir (el evento sube). | AC 28: Esc en esa pregunta equivale a "Seguir editando" y no la reabre; caso (c) en T26. |
| A-2 | 3.4 | No dice qué pasa si el renombre saca la fila de la vista filtrada (`?buscar=card` → "Pediatría") o la cambia de página. | Criterio nuevo y caso en T22: la fila desaparece, el aviso "Especialidad actualizada" se muestra igual y la página queda en un rango válido. |
| A-3 | 3.4 | Cancelar o Esc mientras dice "Guardando…" no está cubierto (sólo el doble Guardar). | AC 19: mientras guarda, Cancelar y Esc no hacen nada; se prueba con el doble de T25. |
| A-4 | 3.4 | Las opciones del menú "⋯" abierto en la tarjeta miden 40 px. | Si se acepta S-43: la regla de 44 px incluye `.menu-de-acciones__opcion` y T28 lo verifica. |
| A-5 | 3.5 | **Bloqueante.** La columna `activo` NOT NULL sin valor por defecto rompe una prueba de la 3.2 que inserta médicos con SQL directo. | AC 33 y `files_to_modify`: ese `INSERT` (en `GestionMedicosEnBaseTests.cs`) suma `activo`. |
| A-6 | 3.5 | La 4.4 describe la ficha del médico como SSR con `?resultado=`; la ficha real es interactiva y usa `AlcancePorOperacion` (3.3 AC 94). | Task 1: si la 4.4 llega así, la 3.5 consulta con `Operaciones.EjecutarAsync`; anotar dónde queda el texto del AC 27. |
| A-7 | 3.5 | `runtime_not_verifiable` dice que la CI del PR "todavía no existe"; existe. | Corregir el motivo y nombrar el workflow en R3. |
| A-8 | 3.5 | La cláusula "`?estado=otro` se ignora" del AC 16 no la ejercita ninguna prueba. | T20 suma esa URL: resultado "Activos" y sin el filtro "Estado: …". |
| A-9 | 9.2 | El texto de desactivar y reactivar da por existentes acciones que trae la 3.5. | AC 5, tercer punto, condicionado a la 3.5 en `done` (como ya dice la Task 1). |
| A-10 | 9.2 | El traductor del detalle sólo atrapa JSON roto; un JSON válido con otra forma lanzaría. | AC 22 y T4: detalle bien formado pero inesperado no lanza (`TryGet*`). |
| A-11 | 9.2 | Las pruebas web mueven el reloj compartido de la fábrica de pruebas, contra la convención del repo. | Reloj falso propio del contexto bUnit; T14 ordena por `Id`. |
| A-12 | 9.2 | `TablaAbm` dibuja tabla y tarjetas a la vez: "la primera fila" es ambiguo; faltan `Entidad` y `Columnas`. | Selectores acotados a `tbody tr` y `.tarjeta-abm`; `HistorialDeRegistro` pasa `Entidad`. |
| A-13 | 9.2 | "Cumple la guía en lo que aplica" no es observable. | AC 19 enumera los puntos de la lista de la guía que se verifican, con su prueba, y nombra la excepción de la URL (S-64). |

## Decisiones del docente

La story ya está escrita con la opción **recomendada** de cada una (marcada `[SUPUESTO S-xx]`).

### 3.4 Renombrar especialidad

| # | Pregunta | Recomendada | Otras |
|---|---|---|---|
| S-40 | Dos administradores renombran a la vez | `xmin` en `especialidades` + el nombre que vio la pantalla: el segundo recibe un aviso y guarda sobre lo último. También desactivar y reactivar detectan la carrera. | Gana el último (como médicos) · sólo comparar el nombre visto |
| S-41 | Esc en el modal con cambios | Pregunta como "Cancelar", sólo en el modal de edición; el alta de A2 y A4 se alinea en otra story | En todos los modales ahora · Esc cierra siempre (choca con la guía) |
| S-42 | FR-12 del PRD dice "crear y desactivar" | Sumar "renombrarlas" en el mismo PR | Dejarlo |
| S-43 | Botones táctiles de 44 × 44 px (hoy 40) | Subirlos a 44 px sólo en las tarjetas de celular (CSS); mejora también A1 y A4 | Story de accesibilidad aparte |
| S-44 | ¿El modal avisa cuántos médicos verán el nombre nuevo? | No: renombrar no es destructivo | Sí, como ayuda bajo el campo |

### 3.5 Desactivar y reactivar médico

| # | Pregunta | Recomendada | Otras |
|---|---|---|---|
| S-50 | Usuario del sistema vinculado y su acceso a M2 | Sigue activo y vinculado; M2 muestra un aviso y no deja guardar ni enviar; al reactivar todo vuelve | Desactivar también el usuario · desvincularlo |
| S-51 | Propuesta de agenda enviada y pendiente | Bloquea la baja con el motivo ("Revisala en Propuestas de agenda") | Se rechaza sola · queda pendiente |
| S-52 | Agenda aprobada y columna "Estado de agenda" | Las agendas no se tocan y el estado se sigue mostrando, junto a "Inactivo"; los turnos los frena el contrato con la 5.2 | La columna dice "Sin agenda" |
| S-53 | ¿Se corrige la agenda (4.4) de un inactivo? | No: la ficha lo explica y el caso de uso lo niega | Sí |
| S-54 | Dónde se ve "Inactivo" en A1 | Columna "Estado" sólo con el filtro "Todos", como Especialidades; en celular, insignia siempre | Al lado del nombre · columna siempre visible |
| S-55 | ¿Reactivar pide confirmación? | Sí, como Reactivar especialidad | No, como Reactivar usuario |
| S-56 | Concurrencia en `medicos` | `xmin`: el segundo administrador recibe un aviso claro; vale también para editar | Gana el último, como hoy |
| S-57 | *(nueva, de la revisión)* La pantalla "Corregir agenda" abierta por URL para un médico inactivo | Muestra el aviso de S-53 sin el editor y con enlace a la ficha | Redirigir a la ficha · fuera de alcance |

### 9.2 Historial por registro

| # | Pregunta | Recomendada | Otras |
|---|---|---|---|
| S-60 | ¿Quién define la consulta por registro y el traductor a texto llano? (la revisión lo marcó bloqueante: la story dice a la vez "depende de la 9.1" y "la 9.1 reutiliza lo mío") | La 9.2 los define y la 9.1 sigue antes en el plan. El AC 15 deja de afirmar qué hará la 9.1. La Task 1 dice: si la 9.1 ya trae una consulta por registro, se usa esa. Al escribir la 9.1 se le pasa esta definición | Esperar a escribir la 9.1 y reescribir la 9.2 · sacar la 9.1 de `depends_on` |
| S-61 | ¿Pestaña o sección? | Sección "Historial" al final de la ficha (la guía admite "pestañas o secciones") | Pestañas con un componente nuevo · página aparte |
| S-62 | ¿En qué fichas? | Sólo la del médico (la única con ficha); el componente queda común | También Especialidades y Usuarios con un modal · ellas como story nueva |
| S-63 | ¿Incluye los movimientos de su agenda? | No: la agenda es otro registro (se ve en la consulta de la 9.1) | Sí, buscando dentro del detalle |
| S-64 | Orden y página | Más reciente primero, 25 por página, la página no va a la URL | Página en la URL · más viejo primero |
| S-65 | Acciones de la 3.5 | Se toman de `AccionesAuditoria` al implementar; una prueba obliga a que toda acción del médico tenga texto | Mostrar también el detalle que guarde la 3.5 |

## Registro de decisiones

> **Decidido por el docente (2026-10-23):** se aceptan **en bloque** las 19 opciones recomendadas
> (S-40 a S-44, S-50 a S-57, S-60 a S-65) y los 13 ajustes A-1 a A-13. Las tres stories se corrigen,
> se vuelven a validar y pasan a `ready-for-dev`.
