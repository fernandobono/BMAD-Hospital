---
documento: Épicas, stories y plan de sprints
proyecto: Gestión de turnos — Hospital Municipal Las Acacias (caso simulado)
version: 1.1
fecha: 2026-10-16
autor: John (producto) con Winston, Sally y Mary
estado: aprobado (2026-10-09)
entrada: docs/02-prd.md (v1.2) · docs/03-ux.md (v1.0) · docs/04-arquitectura.md (v1.0)
---

# Épicas, stories y plan de sprints

> Cada story va a tener su propio archivo en `docs/stories/` con criterios de aceptación
> numerados y la lista de pruebas por nombre (formato Mary+GOLD). Este documento es el índice
> y el plan. Las pruebas son una **estimación**; el número final lo fija cada story.

## 1. Épicas

| Épica | Qué entrega | Requisitos | Pantallas |
|---|---|---|---|
| **E1 Fundaciones** | La solución en capas andando: base, reloj, errores, auditoría, bandeja de emails, CI y Docker. | NFR-03, 06, 08, 09, FR-33, FR-35 | — |
| **E2 Personal y acceso** | Ingreso del personal con roles y gestión de usuarios. | FR-09 a FR-11, NFR-04 | S0, A4 |
| **E3 Catálogo** | Especialidades y médicos. | FR-12, FR-13 | A1, A2 |
| **E4 Agendas** | Propuesta del médico, aprobación, vigencia y corrección de urgencia. | FR-14 a FR-15, R-14 | M2, A6, A1 |
| **E5 Recepción** | Pacientes, disponibilidad, reserva y cancelación desde la ventanilla. | FR-01 a FR-03, FR-16 a FR-19, FR-22 a FR-24, NFR-02 | R1 |
| **E6 Web del paciente** | Ingreso con código, registro, sacar, ver y cancelar turnos. | FR-04 a FR-08, FR-16b, FR-18 a FR-21 | P1 a P7 |
| **E7 Bloqueos y avisos** | Bloqueo de agenda, cancelación masiva, avisos y lista para llamar. | FR-25 a FR-30, R-09 a R-11 | A3, R3 |
| **E8 Agenda del día** | Agenda del médico en pantalla e impresa. | FR-31, FR-32 | M1, R2 |
| **E9 Auditoría** | Consulta de la auditoría por el administrador. | FR-34 | A5 |
| **E10 Calidad final** | Carga, accesibilidad, recorridos completos y respaldo. | NFR-01, NFR-05, NFR-07, NFR-08, NFR-11 | — |

## 2. Stories

### E1 — Fundaciones

| Story | Título | Requisitos | Pruebas (est.) |
|---|---|---|---|
| 1.1 | Estructura de la solución en 4 capas, con Docker Compose (app + PostgreSQL + Mailpit). Reemplaza el código inicial. | Arq. §5 | 3 |
| 1.2 | Reloj del hospital (zona Córdoba) y manejador global de errores sin detalles técnicos. | NFR-06, NFR-09 | 6 |
| 1.3 | Registro de auditoría: tabla, escritura en la misma transacción, protección con permisos y disparador. | FR-33, FR-35, DOM-03 | 7 |
| 1.4 | Bandeja de salida de emails con proceso de fondo y reintentos. | Arq. §7.4, S-33 | 8 |
| 1.5 | Integración continua: compilar, probar y medir cobertura en cada push. | NFR-11 | 1 |

> Conteo final fijado por las stories después de la revisión adversarial (2026-10-09): 1.1 = 14
> verificaciones (12 xUnit + 2 de Compose); 1.2 = 9 + 1 de regresión. La estimación de la tabla queda
> como estaba; el total del sprint se recalcula al cerrarlo.

### E2 — Personal y acceso

| Story | Título | Requisitos | Pruebas (est.) |
|---|---|---|---|
| 2.1 | Ingreso del personal con usuario y contraseña, roles y vencimiento de sesión. | FR-09, FR-11, NFR-04 | 9 |
| 2.2 | Gestión de usuarios del personal: alta con contraseña temporal, desactivar, reactivar. | FR-10, S-30 | 8 |
| 2.3 | Contraseña olvidada: el administrador genera otra contraseña temporal desde A4. *Agregada el 2026-10-16 (revisión del S2, decisión 2).* | FR-10, S-30 | 4 |
| 2.4 | Email del personal en A4: el administrador carga y corrige el email de un usuario. Completa el aviso al médico de la 4.3 (FR-14e). *Agregada el 2026-10-17 (revisión del S3, decisión 1).* | FR-10, FR-14e | 3 |
| 2.5 | Desactivar, reactivar y desbloquear en A4 con dos administradores a la vez: el segundo recibe un 409, como en la 2.3. *Agregada el 2026-10-17 (revisión del S3, decisión 26).* | FR-10 | 3 |

### E3 — Catálogo

| Story | Título | Requisitos | Pruebas (est.) |
|---|---|---|---|
| 3.1 | Especialidades: alta y desactivación (si no hay turnos futuros). | FR-12 | 6 |
| 3.2 | Médicos: alta, edición y listado con estado de agenda; vínculo con usuario médico. | FR-13 | 8 |

### E4 — Agendas

| Story | Título | Requisitos | Pruebas (est.) |
|---|---|---|---|
| 4.1 | Dominio de agenda: franjas, duración, superposición, cálculo de horarios del día y vigencia. | FR-14, FR-14d, R-02 | 14 |
| 4.2 | El médico arma, guarda y envía su propuesta de agenda. | FR-14, FR-14b | 9 |
| 4.3 | El administrador aprueba o rechaza propuestas, con aviso de turnos fuera de agenda. | FR-14c, FR-14e, FR-15 | 10 |
| 4.4 | Corrección de agenda por urgencia, con motivo obligatorio. | FR-14f, S-32 | 5 |

### E5 — Recepción

| Story | Título | Requisitos | Pruebas (est.) |
|---|---|---|---|
| 5.1 | Pacientes: búsqueda por DNI, alta y edición. | FR-01, FR-02, FR-03 | 10 |
| 5.2 | Disponibilidad: horarios libres de un médico y primeros 10 de una especialidad. | FR-16, FR-16b, FR-17 | 12 |
| 5.3 | Reserva de turno con todas las reglas y protección contra reservas simultáneas. **Hereda** de la 3.1 (AC 17) que no se pueda desactivar una especialidad con turnos futuros, y de la 3.2 (AC 23) que no se pueda cambiar la especialidad de un médico con turnos futuros; la prueba T15 de la 3.1 lo recuerda. **Hereda** también de la 4.3 (AC 27, FR-15) implementar `ITurnosFueraDeAgenda` contra la tabla de turnos, y de la 5.1 (AC de contrato) la FK `Turno.PacienteId` a `pacientes.id`; la trampa T17 de la 4.3 lo recuerda. | FR-18, FR-19, R-01, R-04, R-05, NFR-02, FR-12 | 14 |
| 5.4 | Cancelación de turno por la recepción. | FR-22, FR-23, FR-24 | 6 |
| 5.5 | Pantalla de recepción R1 completa, manejable con teclado. | RU-03, RU-04, CE-04 | 8 |

### E6 — Web del paciente

| Story | Título | Requisitos | Pruebas (est.) |
|---|---|---|---|
| 6.1 | Ingreso con DNI, email y código de un solo uso, con bloqueo por intentos. | FR-07, FR-08, S-20, S-26 | 12 |
| 6.2 | Primer registro del paciente por la web. | FR-04, FR-05 | 7 |
| 6.3 | Mis turnos y cancelación por el paciente (hasta 2 h antes). | FR-21, R-06, S-27 | 8 |
| 6.4 | Sacar turno en 4 pasos: especialidad, médico, horario, confirmar. | FR-16b, FR-18, FR-19 | 10 |
| 6.5 | Email de confirmación de turno. | FR-20 | 4 |
| 6.6 | Mis datos: ver datos personales y cambiar teléfono. | FR-06, DOM-04 | 4 |

### E7 — Bloqueos y avisos

| Story | Título | Requisitos | Pruebas (est.) |
|---|---|---|---|
| 7.1 | Bloquear agenda: vista previa de afectados y confirmación con BLOQUEAR. | FR-25, FR-26, S-29 | 8 |
| 7.2 | Cancelación por el hospital, avisos por email y pendientes para llamar, en una transacción. | FR-27, FR-28, FR-29, R-10, R-11 | 11 |
| 7.3 | Lista "Pacientes para avisar": marcar avisado y "darle turno". | FR-29, RU-05 | 6 |
| 7.4 | Levantar un bloqueo futuro. | FR-30, S-23 | 4 |

### E8 — Agenda del día

| Story | Título | Requisitos | Pruebas (est.) |
|---|---|---|---|
| 8.1 | Mi agenda del médico (sólo la propia). | FR-31, R-13 | 6 |
| 8.2 | Agenda de cualquier médico para la recepción, con hoja A4 para imprimir. | FR-32 | 6 |

### E9 — Auditoría

| Story | Título | Requisitos | Pruebas (est.) |
|---|---|---|---|
| 9.1 | Consulta de auditoría con filtros, sólo lectura. | FR-34, FR-35 | 6 |

### E10 — Calidad final

| Story | Título | Requisitos | Pruebas (est.) |
|---|---|---|---|
| 10.1 | Recorridos E2E de los 8 recorridos de usuario, con auditoría de accesibilidad. | RU-01 a RU-08, NFR-05 | 10 |
| 10.2 | Prueba de carga con 30 usuarios simultáneos. | NFR-01 | 2 |
| 10.3 | Respaldo diario y procedimiento de restauración probado. | NFR-08 | 1 |

**Total: 37 stories · ~259 pruebas estimadas.**

## 3. Plan de sprints

**Equipo:** el docente junto con los agentes de IA (Mary, John, Winston, Sally y el agente de desarrollo).
Los sprints son **incrementos de entrega**, no bloques de calendario: cada uno cierra con algo que se
puede mostrar. La duración real se mide al cerrar el sprint 1 y con eso se proyecta el resto. **[SUPUESTO S-35]** El orden sigue las dependencias: no se puede reservar
sin agendas, ni avisar sin bandeja de emails.

| Sprint | Objetivo | Stories | Pruebas (est.) | Tiempo |
|---|---|---|---|---|
| **S1** | La base andando: se levanta todo, se entra como personal. | 1.1, 1.2, 1.3, 1.4, 1.5, 2.1 | 34 → **77 reales** | **11 h 45 medido** |
| **S2** | El hospital se puede configurar. | 2.2, 3.1, 3.2, 4.1 | 36 → **129 reales** | **6 h 31 medido** |
| **S3** | Los médicos tienen agenda aprobada. | 2.3, 4.2, 4.3, 4.4, 5.1 | 38 | ~9 h 55 |
| **S4** | La recepción da y cancela turnos. *Primera versión usable.* | 2.4, 2.5, 5.2, 5.3, 5.4, 5.5 | 46 | ~12 h 00 |
| **S5** | El paciente saca turnos por la web. | 6.1, 6.2, 6.3, 6.4, 6.5, 6.6 | 45 | ~11 h 45 |
| **S6** | Licencias, agendas del día y auditoría. | 7.1, 7.2, 7.3, 7.4, 8.1, 8.2, 9.1 | 47 | ~12 h 15 |
| **S7** | Listo para producción. | 10.1, 10.2, 10.3 | 13 | ~3 h 25 |
| | | **37 stories** | **~259** | **~67 h 35** (18 h 16 medidas + ~49 h 20) |

**Velocidad medida** (retrospectivas en `docs/retrospectiva-sprint-1.md` y
`docs/retrospectiva-sprint-2.md`): el S1 tardó 11 h 45 de reloj para 34 pruebas estimadas (21 min por
prueba) y el S2, 6 h 31 para 36 (11 min por prueba). Los sprints 3 a 7 se proyectan por tamaño con la
velocidad de los dos sprints juntos: **15,7 minutos por prueba estimada** (decisión del docente,
2026-10-17). El tiempo es de sesión completa, con el merge y la exportación. Se recalcula al cerrar el
S3 si se desvía más de un 20 %.

**Lo que enseñó el S2:** sus stories tenían 129 pruebas contra 36 estimadas (3,6 veces) y se esperaba
que tardara 19 a 20 h. Tardó 6 h 31: el reuso de las piezas de la 2.2 pesó más que la cantidad de
pruebas.

**Hitos:**

- Fin de S4: la recepción reemplaza la planilla de Excel. Ya cumple el objetivo de cero turnos pisados (CE-03).
- Fin de S5: abre la web del paciente (CE-01).
- Fin de S7: versión 1 completa.

## 4. Cobertura de requisitos

Todos los FR del PRD (FR-01 a FR-35, con FR-14b a FR-14f y FR-16b) tienen al menos una story.
Los NFR-02, 03, 04 y 10 no tienen story propia: se cumplen dentro de las stories que tocan esa
parte (5.3, 1.1 y 2.1, 2.1, y todas las de formularios).

NFR-03 (HTTPS y contraseñas), repartido así (revisión I-7, contrato técnico §"HTTPS — NFR-03"):
- **1.1**: la app detrás del proxy que termina el HTTPS — `UseForwardedHeaders` sólo desde redes
  confiables y, fuera de desarrollo, HSTS y redirección a HTTPS (AC 17, prueba T10).
- **2.1**: la cookie del personal con `Secure` fuera de desarrollo, y el hash lento de las contraseñas.

## 5. Supuestos nuevos

| Id | Supuesto |
|---|---|
| S-35 | Los sprints no tienen duración fija: la velocidad real se mide en el sprint 1 y se proyecta el resto. **Medida el 2026-10-16:** 21 min de reloj por prueba estimada. **Recalculada el 2026-10-17** con los sprints 1 y 2: 15,7 min. |
