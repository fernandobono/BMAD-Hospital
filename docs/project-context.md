---
documento: Contexto del proyecto para los agentes
proyecto: Gestión de turnos — Hospital Municipal Las Acacias (caso simulado)
fecha: 2026-10-17
estado: vigente
---

# Contexto del proyecto — reglas que se respetan siempre

Este archivo lo leen los agentes antes de escribir una story o implementarla. Resume reglas que no
son obvias; el detalle vive en los documentos que se citan.

## Documentos que mandan

| Documento | Para qué |
|---|---|
| [`02-prd.md`](02-prd.md) | Qué hace el sistema (FR, NFR, reglas R-xx) |
| [`03-ux.md`](03-ux.md) | Pantallas y recorridos; su §0 fija la guía UX/UI como estándar |
| [`ux/guia-ux-ui-abm.md`](ux/guia-ux-ui-abm.md) (y `.pdf`) | **Estándar UX/UI obligatorio** para toda pantalla |
| [`04-arquitectura.md`](04-arquitectura.md) | Capas, tecnología y componentes de interfaz (§5.1) |
| [`stories/00-contrato-tecnico.md`](stories/00-contrato-tecnico.md) | Nombres compartidos entre stories |

## Interfaz: la guía UX/UI es obligatoria (desde 2026-10-17)

1. **Toda story con UI cumple la guía** y tiene el criterio de aceptación *"Cumple la guía UX/UI
   (`docs/ux/guia-ux-ui-abm.md`) y usa los componentes definidos en la arquitectura §5.1"*. El checklist
   del final de la guía se usa en la revisión y en las pruebas de aceptación.
2. **Si la guía no cubre algo, se pregunta al docente antes de decidir.** No se inventa un patrón. Los
   huecos conocidos (agenda del día, pantalla operativa de recepción, comparación lado a lado, asistente
   de bloqueo…) están en [`ux/analisis-brechas-abm.md` §8](ux/analisis-brechas-abm.md).
3. **Se arma con los componentes comunes** de `HospitalTurnos.Web/Components/Comunes/` (arquitectura
   §5.1): `PaginaAbm`, `BarraDeFiltros`, `TablaAbm`, `MenuDeAcciones`, `Paginador`, `InsigniaDeEstado`,
   `FormularioAbm`/`CampoDeFormulario`, `VistaDeDetalle`, `DialogoModal`, `DialogoDeConfirmacion`,
   `Notificaciones`. Si falta una variante, se extiende el componente; no se copia en la página.
4. **Sin estilos propios en las vistas:** prohibido `style="…"` y colores en hexadecimal fuera de
   `wwwroot/css/tokens.css`. Clases de componente en `wwwroot/app.css`.
5. **Modo de renderizado:** las pantallas ABM del personal son interactivas en el servidor, puesto por
   página y sin prerender (ADR-08). El ingreso del personal y el cambio de contraseña siguen en SSR
   estático (escriben la cookie). La web del paciente es SSR estático sin JavaScript (NFR-05); si un punto
   de la guía pide interactividad en una página del paciente, se pregunta.
6. **En un circuito interactivo no hay `HttpContext`:** el actor sale del `AuthenticationState`; los
   errores del dominio se muestran en la página, sin código HTTP. `FijarEstadoEnPagina` es sólo para
   páginas estáticas.
7. **Decisiones del docente que precisan la guía:** fechas `dd/mm/aaaa` con año en el personal y fecha
   larga en la web del paciente; ámbar `#B45309` para pendiente/atención/cancelado por el hospital; sans
   serif del sistema; voseo en todo el sistema; la guía rige también la web del paciente.
8. **Microcopy de la guía:** "Nuevo …", "Guardar", "Cancelar", "Desactivar"/"Reactivar", confirmación
   "¿Desactivar X?" con la consecuencia, éxito en pasado ("Especialidad creada"), personas como
   "Apellido, Nombre" en listados.
9. **Sin datos de prueba visibles:** las sondas y pruebas en vivo borran lo que crean; la base de demo
   usa nombres verosímiles.

## Reglas de trabajo que ya regían

- No se escribe código sin story aprobada (`ready-for-dev`), ni siquiera scripts.
- La hora sale siempre del reloj inyectado (`TimeProvider`), nunca de `DateTime.Now`; las pruebas de
  tiempo usan el reloj falso, nunca esperas reales.
- Errores sin detalles técnicos al usuario (NFR-09): el mensaje dice qué pasó y qué hacer.
- Las acciones que cambian datos dejan auditoría `<entidad>.<acción>` en la misma transacción.
- `main` está protegida: todo entra por PR con la CI en verde.
