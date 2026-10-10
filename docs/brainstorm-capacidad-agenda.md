---
documento: Brainstorm — capacidad de la agenda de un médico
proyecto: Gestión de turnos — Hospital Municipal Las Acacias (caso simulado)
fecha: 2026-10-09
participantes: Mary (analista), John (producto), Winston (arquitectura), Sally (UX)
estado: cerrado (2026-10-09)
---

# Brainstorm — ¿Cuántos turnos puede dar un médico por día?

**Disparador:** el PRD calcula los turnos por día como horas de atención ÷ duración del turno,
con una duración mínima de 10 minutos. La investigación mostró que no hay una norma con tope y
que la referencia es de 3 a 4 consultas por hora (15 a 20 minutos).

**Pregunta del brainstorm:** ¿cómo evita el sistema agendas imposibles de cumplir, sin quitarle
flexibilidad al hospital?

## Ideas

Ideas sin filtrar, agrupadas por enfoque. Esfuerzo: B = bajo, M = medio, A = alto.

### A. Duración del turno

| # | Idea | Quién | Esfuerzo |
|---|---|---|---|
| 1 | Subir la duración mínima de 10 a 15 minutos para todos. | John | B |
| 2 | Duración mínima **por especialidad**: clínica 15, pediatría 20, cardiología 30. El médico no puede proponer menos. | Mary | B |
| 3 | Dos tipos de turno: **primera vez** (más largo) y **control** (más corto). Es lo más común en hospitales reales: un paciente nuevo lleva más tiempo. | Mary | M |

### B. Topes

| # | Idea | Quién | Esfuerzo |
|---|---|---|---|
| 4 | Tope de turnos por día, configurable por especialidad. | John | B |
| 5 | Tope de consultas por hora (por ejemplo, 4), sin importar la duración. | Winston | B |
| 6 | La agenda propuesta no puede superar la **carga horaria contratada** del médico (por ejemplo, 35 h semanales). | Mary | M |
| 7 | Pausa obligatoria: después de 4 horas seguidas de atención, 20 minutos sin turnos. | Sally | B |

### C. Aviso en vez de prohibición

| # | Idea | Quién | Esfuerzo |
|---|---|---|---|
| 8 | Sin tope duro: al aprobar una propuesta, el administrador ve una alerta si pasa de 4 consultas por hora o de la carga horaria. Decide él. | John | B |
| 9 | Mostrar, en la pantalla de aprobación, la comparación con el promedio de la especialidad ("este médico ofrece 40 % más turnos que sus colegas"). | Sally | M |

### D. Reparto de la capacidad

| # | Idea | Quién | Esfuerzo |
|---|---|---|---|
| 10 | **Cupo por canal**: por ejemplo, 70 % de los turnos para la web y 30 % reservados para la recepción, así quien no tiene internet no se queda sin turno. | Sally | M |
| 11 | **Cupo del día**: algunos turnos se liberan recién a las 7 de la mañana del mismo día, para casos que no pueden esperar semanas. | Mary | M |
| 12 | Apertura escalonada: la agenda se abre a 60 días, pero una parte se libera recién 7 días antes. Evita que se agote todo el primer día. | John | M |
| 13 | **Sobrecupo calculado** por ausentismo: si falta el 25 %, se dan algunos turnos extra, como las aerolíneas. Polémico: si vienen todos, la espera se dispara. | Winston | A |

### E. Restricciones físicas

| # | Idea | Quién | Esfuerzo |
|---|---|---|---|
| 14 | **Consultorios**: cada franja se asigna a un consultorio, y dos médicos no pueden usar el mismo consultorio a la vez. Es lo que motivó el rechazo del Dr. Pereyra en el recorrido RU-07b. | Winston | A |

### F. Aprender de los datos (versiones siguientes)

| # | Idea | Quién | Esfuerzo |
|---|---|---|---|
| 15 | Registrar la hora real de inicio y fin de cada consulta, y sugerir la duración según lo medido. | Winston | A |
| 16 | Medir la demanda no atendida (búsquedas sin turno libre) para decidir dónde hacen falta más horas. | Mary | M |

## Primera lectura del equipo

- **John**: para la v1 me quedo con la **2** (mínimo por especialidad) y la **8** (alerta al aprobar). Son baratas, resuelven el problema y dejan la decisión en manos de una persona.
- **Sally**: la **10** (cupo por canal) me preocupa más que el tope. Si la web se lleva todos los turnos, el paciente mayor que llama por teléfono se queda afuera, que es justo a quien queremos cuidar.
- **Mary**: la **3** (primera vez / control) es la que más se parece a un hospital real. Si es material de clase, también es la más rica para modelar.
- **Winston**: la **14** (consultorios) es real pero cara: suma una entidad y un control de choques. La dejaría para la v2. La **13** (sobrecupo) la descartaría: con datos de salud no conviene especular con la espera.

## Decisión

**Sin tope** (docente, 2026-10-09). Los turnos por día salen de la agenda aprobada:
horas de atención ÷ duración del turno. Ninguna idea entra en la primera versión; quedan como
banco de ideas para versiones siguientes.
