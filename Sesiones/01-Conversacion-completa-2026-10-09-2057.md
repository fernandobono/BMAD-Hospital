# Sesión 01 — Conversación completa

**Inicio:** 9 de octubre de 2026, 20:57 (hora de Argentina)
**Participantes:** el docente y el equipo de agentes BMAD (Mary, John, Winston, Sally).

> Transcripción de la conversación: todos los mensajes del docente y todas las respuestas del
> equipo, en orden. No incluye las operaciones internas (lecturas de archivos, comandos,
> búsquedas). La versión resumida, con comentarios para la clase, es
> [01-Inicio-2026-10-09-2030.md](01-Inicio-2026-10-09-2030.md).

---

### 👤 Docente · 09/10 20:57

quiero hacer una gestion de turnos de un hospital

### 🤖 Equipo

Antes de arrancar con los turnos, necesito saber para qué lo querés.

### 🤖 Equipo (cuadro de opciones)

**¿Para qué es la gestión de turnos del hospital?**
- Caso de estudio para MyD: Un caso para la materia: enunciado, casos de uso, diagramas UML (clases, secuencia, estados) y quizás una consigna para los alumnos.
- Aplicación que funcione: Un sistema real o un prototipo que se pueda usar: pacientes, médicos, agenda y reserva de turnos.
**¿Qué alcance querés para la primera versión?**
- Lo básico: Pacientes, médicos con especialidad, agenda, y reservar o cancelar un turno.
- Intermedio: Lo básico más obras sociales, lista de espera, recordatorios y estados del turno (ausente, atendido).
- Completo: Lo intermedio más sobreturnos, guardias, consultorios, reportes y roles (recepción, médico, admin).

---

### 👤 Docente (respuesta en el cuadro de opciones) · 09/10 20:58

- **¿Para qué es la gestión de turnos del hospital?** → Aplicación que funcione
- **¿Qué alcance querés para la primera versión?** → Lo básico

### 🤖 Equipo (cuadro de opciones)

**¿Cómo querés que esté hecha la app de turnos?**
- Página web publicada (Recommended): Una página en claude.ai con datos compartidos. Se usa ya desde el navegador, sin instalar nada, y varias personas ven los mismos turnos.
- C# / .NET con base de datos: Un proyecto en tu máquina (API + front), con código orientado a objetos y tests. Sirve también para mostrar en clase. Lleva más tiempo.
- Next.js + base de datos: Una app web moderna en tu máquina, lista para subir a un hosting.

---

### 👤 Docente (respuesta en el cuadro de opciones) · 09/10 20:59

- **¿Cómo querés que esté hecha la app de turnos?** → C# / .NET con base de datos

### 🤖 Equipo

Esqueleto listo. Ahora el modelo del dominio.

*[el docente interrumpió]*

---

### 👤 Docente · 09/10 21:01

porque te pusiste a tranajar si no sabemos que hacer, llama al equipo bmad

### 🤖 Equipo

No hay una instalación de BMAD en este proyecto; armo la mesa con los agentes que tenés instalados (Mary, John, Winston y Sally).

🎉 **Modo equipo activado.** Hoy están Mary (analista), John (producto), Winston (arquitecto) y Sally (experiencia de usuario).

📊 **Mary**: ¡Me encanta! "Turnos de un hospital" puede ser cosas muy distintas. Antes de dibujar nada quiero saber quién pide el turno: ¿el paciente desde su casa, o la recepción por teléfono y ventanilla? Cambia todo el diseño.

🎨 **Sally**: Imaginate a doña Rosa, 74 años, llamando a las 7 de la mañana. O a la recepcionista con 20 personas en la fila. Son dos pantallas totalmente distintas. Yo arrancaría por una sola de esas personas.

🏗️ **Winston**: Coincido en no tocar la arquitectura todavía. Con .NET y una base vamos bien para cualquier respuesta. Lo que cambia la arquitectura es si hay acceso público por internet o si es sólo uso interno.

📋 **John**: Primero el porqué. "Una aplicación que funcione" puede querer decir dos cosas muy distintas, y de eso depende todo el camino:

- **Material para la materia**: la app es el ejemplo resuelto. Pesan los artefactos (brief, requisitos, casos de uso, UML, stories) tanto como el código.
- **Uso real en un hospital concreto**: pesan las reglas reales, las personas que lo van a usar y el circuito que se usa hoy.

**John pregunta: ¿para qué es? ¿Material de clase, uso real en un hospital (¿cuál?) o las dos cosas?**

_[Esperando tu respuesta…]_

Podés hablarle a cualquiera del equipo por su nombre.
`[E] Salir del modo equipo`

---

### 👤 Docente · 09/10 21:03

es para un ejemplo de clase pero simulando un caso real, hagamos el proyecto completo como si fuera real

### 🤖 Equipo

📋 **John**: Perfecto. Entonces inventamos un hospital creíble y lo tratamos como cliente real. El camino: brief → requisitos (PRD) → experiencia de usuario → arquitectura → stories → código con tests.

📊 **Mary**: Yo armo el caso: un hospital mediano con varias especialidades y un circuito de turnos con problemas reales. Por ejemplo, gente que no viene y turnos que se pierden.

🎨 **Sally**: Para escribir el caso necesito saber quién pide el turno. Si lo pide el paciente, hay que diseñar para cualquiera. Si lo pide la recepción, hay que diseñar para alguien rápido y entrenado.

**Mary pregunta: ¿quién pide el turno en la primera versión?**

- **A.** Sólo la recepción (por teléfono o en ventanilla)
- **B.** Sólo el paciente, por la web
- **C.** Los dos

_[Esperando tu respuesta…]_

`[E] Salir del modo equipo`

---

### 👤 Docente · 09/10 21:04

c

### 🤖 Equipo

🏗️ **Winston**: Con la opción C hay dos accesos. La recepción usa la app adentro del hospital. El paciente entra por internet, así que necesita identificarse de forma segura. Eso ya lo anoto como requisito.

🎨 **Sally**: Y dos experiencias: una pantalla rápida para la recepcionista y una muy simple para el paciente, usable desde el celular.

📊 **Mary**: Propongo no seguir preguntando de a una. Yo escribo el brief completo con supuestos razonables marcados como tales: tipo de hospital, especialidades, reglas de cancelación, cómo se identifica el paciente. Vos lo leés y corregís lo que no te cierre. Es más rápido y te da algo concreto para mirar.

📋 **John**: De acuerdo. El brief quedaría en `docs/` dentro del proyecto `~/SourceCode/hospital-turnos`, junto al código.

**John pregunta: ¿Mary arma el brief con supuestos y vos lo corregís?**

_[Esperando tu respuesta…]_

`[E] Salir del modo equipo`

---

### 👤 Docente · 09/10 21:06

si

### 🤖 Equipo

📊 **Mary**: ¡Listo el brief! Inventé el **Hospital Municipal Las Acacias**, un hospital público de una ciudad del interior de Córdoba. Está en [docs/01-brief.md](../../../../../SourceCode/hospital-turnos/docs/01-brief.md).

Los supuestos que más cambian el resto del proyecto:

- **R-04**: cada paciente puede tener un solo turno futuro por especialidad, para que nadie acapare.
- **R-06**: el paciente cancela por la web hasta 2 horas antes; después, sólo la recepción.
- **S-14**: el paciente entra con DNI y un código que le llega por email, sin contraseña.
- **Fuera de esta versión**: obras sociales, recordatorios, lista de espera y presentes/ausentes.

📋 **John**: Antes de escribir los requisitos necesito cerrar la pregunta abierta P-01, porque es la que más agranda el alcance.

**John pregunta: si un médico se toma licencia con turnos ya dados, ¿la primera versión permite bloquear esos días y avisar a los pacientes? ¿O lo dejamos para después?**

_[Esperando tu respuesta…]_

También podés corregir cualquier supuesto del brief.
`[E] Salir del modo equipo`

---

### 👤 Docente · 09/10 21:07

La primera lo debe contemplar

### 🤖 Equipo

📊 **Mary**: Anotado en el brief. El bloqueo de agenda entra en la primera versión, con tres reglas nuevas:

- **R-09**: el administrador bloquea días u horas de un médico, y en ese período no se ofrecen turnos.
- **R-10**: los turnos ya dados pasan a "Cancelado por el hospital". A quien tiene email se le avisa por email. Los que no tienen email aparecen en una lista para que la recepción los llame.
- **R-11**: ese paciente puede sacar un turno nuevo aunque ya tenga otro de la especialidad.

🏗️ **Winston**: Esto suma envío de emails, que ya necesitábamos para el código de ingreso del paciente. Así que no agrega una pieza nueva.

📋 **John**: Queda la pregunta P-02, que define si el médico es usuario del sistema o no.

**John pregunta: ¿el médico entra al sistema a ver su agenda del día, o la recepción le imprime la lista?**

_[Esperando tu respuesta…]_

`[E] Salir del modo equipo`

---

### 👤 Docente · 09/10 21:08

Las dos

### 🤖 Equipo

📊 **Mary**: Anotado. El médico entra a ver su agenda del día, y la recepción también puede imprimirla.

🏗️ **Winston**: Con esto el sistema tiene cuatro tipos de usuario: paciente, recepcionista, médico y administrador. El personal del hospital entra con usuario y contraseña, y cada uno ve sólo lo suyo. El médico ve su agenda, no la de los demás.

🎨 **Sally**: La lista impresa la diseño en una hoja A4, con letra grande y el horario bien visible.

📋 **John**: Última pregunta abierta, la P-03. Con datos de salud de por medio, esto pesa.

**John pregunta: ¿el sistema registra quién dio, canceló o bloqueó cada turno, y cuándo? Sirve ante reclamos y lo pide la ley de datos personales, pero suma trabajo.**

_[Esperando tu respuesta…]_

`[E] Salir del modo equipo`

---

### 👤 Docente · 09/10 21:09

Si hacelo

### 🤖 Equipo

📊 **Mary**: Anotado. Cada turno dado, cancelado o bloqueado queda registrado con quién lo hizo, cuándo y por qué canal. Ese registro no se puede modificar ni borrar. Con esto se cerraron las tres preguntas abiertas del brief.

🏗️ **Winston**: El registro lo guardo aparte de los turnos, en una tabla donde sólo se agregan filas. Así nadie puede "corregir" la historia.

📋 **John**: El brief quedó completo, en su versión 0.4. Quedan 16 supuestos marcados que nadie corrigió todavía: el hospital, las especialidades, los plazos y las reglas de cancelación.

**John pregunta: ¿das por buenos esos supuestos y arranco el PRD (los requisitos)? ¿O querés revisar el brief antes?**

_[Esperando tu respuesta…]_

`[E] Salir del modo equipo`

---

### 👤 Docente · 09/10 21:10

abrime el documento de supuestos

### 🤖 Equipo

Te lo abrí en el editor: [01-brief.md](../../../../../SourceCode/hospital-turnos/docs/01-brief.md). Los supuestos están marcados como **[SUPUESTO S-nn]**, con la búsqueda los encontrás rápido.

Cuando lo revises, decime qué corregir, o "aprobado" para que John arranque los requisitos.

`[E] Salir del modo equipo`

---

### 👤 Docente · 09/10 21:12

validado los supuestos, sigue

### 🤖 Equipo

📋 **John**: El PRD (el documento de requisitos) está listo y te lo abrí en el editor: [02-prd.md](../../../../../SourceCode/hospital-turnos/docs/02-prd.md).

- **8 recorridos de usuario**: los 4 roles, más qué ve cada uno cuando algo falla.
- **35 requisitos funcionales** en 8 grupos, con una tabla de permisos por rol.
- **11 requisitos de calidad** medibles: velocidad, seguridad, accesibilidad y respaldos.
- **Trazabilidad**: cada regla del brief apunta a su recorrido y a sus requisitos.

Agregué 9 supuestos nuevos (S-17 a S-25), listados al final del documento. Los que más pesan:

- **S-20**: el código del paciente tiene 6 dígitos y vence a los 10 minutos.
- **S-21**: si se cambian los horarios de un médico, no se cancelan los turnos ya dados; sólo se avisa.
- **S-23**: si se levanta un bloqueo, los turnos ya cancelados no vuelven.

🎨 **Sally**: Cuando lo apruebes, sigo yo con el diseño de las pantallas.

**John pregunta: ¿aprobás el PRD o querés corregir algo?**

_[Esperando tu respuesta…]_

`[E] Salir del modo equipo`

---

### 👤 Docente · 09/10 21:19

aprobado

### 🤖 Equipo

PRD aprobado. Ahora Sally escribe el diseño de pantallas (~3 min).

### 🤖 Equipo

🎨 **Sally**: Te abrí el diseño de pantallas en el editor: [03-ux.md](../../../../../SourceCode/hospital-turnos/docs/03-ux.md).

- **Paciente**: 7 pantallas para el celular, una pregunta por pantalla. Sacar un turno lleva 3 pasos.
- **Recepción**: todo pasa en una sola pantalla y se maneja con teclado, para dar el turno en menos de un minuto.
- **Médico y administración**: la agenda con su hoja para imprimir, los bloqueos con confirmación reforzada y la auditoría de sólo lectura.
- Están dibujadas las pantallas clave, y cada recorrido del PRD tiene sus pantallas.

Hay 5 supuestos nuevos (S-26 a S-30). El que más cambia la experiencia:

- **S-28**: el paciente elige especialidad y horario, pero no el médico. La recepción sí puede elegir médico.

🏗️ **Winston**: Cuando lo apruebes, sigo con la arquitectura.

**Sally pregunta: ¿aprobás el diseño, o querés que el paciente también pueda elegir médico?**

_[Esperando tu respuesta…]_

`[E] Salir del modo equipo`

---

### 👤 Docente · 09/10 21:23

me gustaria que el paciente eliga el medico tambien

### 🤖 Equipo

🎨 **Sally**: Listo. El paciente ahora saca el turno en 4 pasos: especialidad → médico → día y horario → confirmar.

- En el paso del médico, cada uno aparece con su **próximo día libre**, así el paciente compara sin entrar a cada uno.
- La primera opción es **"El primer turno libre, con cualquier médico"**, para quien no tiene preferencia.
- Un médico sin lugar en los próximos 60 días aparece en gris, con la leyenda "Sin turnos disponibles".

📋 **John**: También lo agregué al PRD (versión 1.1), para que los documentos no se contradigan.

**Sally pregunta: ¿aprobás el diseño de pantallas?**

_[Esperando tu respuesta…]_

`[E] Salir del modo equipo`

---

### 👤 Docente · 09/10 21:24

cuantos turnos por dia permite un medico?

### 🤖 Equipo

📋 **John**: Hoy no hay un número fijo. Sale de dos datos que carga el administrador:

**turnos por día = horas de atención de ese día ÷ duración del turno**

| Ejemplo | Franja | Duración | Turnos |
|---|---|---|---|
| Clínica médica | 8 a 12 (4 h) | 20 min | 12 |
| Pediatría | 8 a 12 (4 h) | 30 min | 8 |
| Traumatología, mañana y tarde | 8 a 12 + 14 a 18 (8 h) | 15 min | 32 |

El máximo posible es de **60 turnos**: 10 horas de atención (8 a 18) con turnos de 10 minutos, la duración mínima.

🏗️ **Winston**: Un turno que no entra completo al final de la franja no se ofrece. Por ejemplo, con turnos de 25 minutos en una franja de 8 a 12 entran 9, y los últimos 15 minutos quedan libres.

**John pregunta: ¿alcanza con esta cuenta, o el hospital pone además un tope de turnos por día por médico (por ejemplo, 20), aunque la franja dé para más?**

_[Esperando tu respuesta…]_

`[E] Salir del modo equipo`

---

### 👤 Docente · 09/10 21:25

estas suponiendo que cada medico define su agenda?

### 🤖 Equipo

📋 **John**: Buena pregunta, porque los documentos no son del todo claros:

- **Quién la carga**: el administrador, nunca el médico. El médico sólo mira su agenda (FR-13, FR-14, recorrido RU-07).
- **Quién la decide**: acá está la ambigüedad. El brief dice "cada médico define la duración de su turno" (supuesto S-09), pero no dice si lo decide el médico o el hospital por él.

📊 **Mary**: En un hospital público lo habitual es que la dirección o el jefe de cada servicio fije los horarios. El médico cumple su carga horaria y no elige libremente. Y muchas veces la duración del turno es la misma para toda la especialidad. Por ejemplo, 20 minutos en clínica y 30 en pediatría.

**John pregunta: ¿cómo funciona en Las Acacias?**

- **A.** El hospital decide los horarios y la duración de cada médico, y el administrador los carga.
- **B.** El hospital decide los horarios, pero la duración es una sola por especialidad.
- **C.** Cada médico propone su agenda y el administrador la aprueba y la carga.

_[Esperando tu respuesta…]_

`[E] Salir del modo equipo`

---

### 👤 Docente · 09/10 21:25

c

### 🤖 Equipo

📋 **John**: Anotado: cada médico propone su agenda y el administrador la aprueba. Me falta un detalle que cambia bastante el alcance.

🏗️ **Winston**: Si la propuesta se hace dentro del sistema, aparece un circuito nuevo. El médico carga sus franjas y la duración como "propuesta", el administrador la aprueba o la rechaza con un motivo, y recién al aprobarla se habilitan los turnos. Son pantallas, estados y avisos nuevos. Si la propuesta se hace por fuera (mail, nota o en persona), el sistema queda como está: el administrador carga lo que ya aprobó.

**John pregunta: ¿dónde propone el médico su agenda?**

- **A.** Por fuera del sistema; el administrador sólo carga lo aprobado (no suma trabajo).
- **B.** Dentro del sistema, con aprobación del administrador (suma un circuito nuevo).

_[Esperando tu respuesta…]_

`[E] Salir del modo equipo`

---

### 👤 Docente · 09/10 21:26

b

### 🤖 Equipo

El circuito quedó agregado en los tres documentos: brief, PRD y diseño de pantallas.

🏗️ **Winston**: Así funciona ahora:

- **El médico** arma su propuesta en el sistema: la duración del turno, las franjas de cada día y desde cuándo rige. Mientras la arma, ve cuántos turnos por semana ofrece. La propuesta pasa por estos estados: borrador → enviada → aprobada o rechazada.
- **El administrador** ve la agenda vigente y la propuesta lado a lado, con los cambios marcados. La aprueba o la rechaza, y para rechazar tiene que escribir el motivo. El médico recibe un email con la respuesta.
- **Mientras tanto** sigue rigiendo la agenda anterior. Un médico nuevo no ofrece turnos hasta que le aprueban la primera propuesta.

📋 **John**: Sumé 2 supuestos:

- **S-31**: una propuesta rige como mínimo 7 días después de enviarla.
- **S-32**: en una urgencia, el administrador puede corregir una agenda sin propuesta del médico, dejando el motivo.

Sigue abierta la pregunta anterior: ¿el hospital pone un tope de turnos por día por médico?

**John pregunta: ¿tope por día (¿cuántos?) o alcanza con la cuenta franjas ÷ duración? Y con eso, ¿aprobás el diseño de pantallas?**

_[Esperando tu respuesta…]_

`[E] Salir del modo equipo`

---

### 👤 Docente · 09/10 21:28

respecto al tope, podes investigar que hacen los hospitales similares a ete y me contas aasi decido

### 🤖 Equipo

Investigo qué hacen hospitales públicos parecidos (~2 min).

### 🤖 Equipo

📊 **Mary**: Investigué, y la respuesta corta es que **no hay un tope de turnos por día que el sistema deba imponer**. El límite real lo da la agenda.

- **No encontré una norma con tope.** Ni la nacional sobre consultorios externos ni nada de Córdoba fija turnos por médico por día. La norma dice que la cantidad de médicos se ajusta a la demanda, y cada hospital organiza sus agendas.
- **El ritmo de referencia es de 3 a 4 consultas por hora.** La prensa cita a la OMS con 15 a 20 minutos por paciente. Un estudio de la UCEMA sobre Córdoba calcula 3 consultas por hora, con médicos contratados por 35 horas semanales. Otro artículo da 15 minutos como tiempo habitual para clínica médica.
- **Ojo con nuestro mínimo:** el PRD permite turnos de 10 minutos, que es menos que cualquier referencia.

📋 **John**: Mi recomendación: sin tope diario aparte. El control ya existe, porque el administrador aprueba cada agenda y ve cuántos turnos por semana ofrece el médico. Lo que sí cambiaría es subir la duración mínima de 10 a 15 minutos.

**John pregunta: ¿cuál preferís?**

- **A.** Sin tope diario, y la duración mínima sube a 15 minutos (recomendado).
- **B.** Además, un tope de turnos por día configurable por especialidad.
- **C.** Dejarlo como está (mínimo de 10 minutos, sin tope).

_[Esperando tu respuesta…]_

`[E] Salir del modo equipo`

Fuentes:
- [Resolución 431/2000 — Directrices de consultorios externos](https://www.argentina.gob.ar/normativa/nacional/norma-63323/texto)
- [UCEMA — Sueldos médicos bajos y falta de turnos](https://ucema.edu.ar/investigacion/tn-271122)
- [ABC Color — Consultas de 12 minutos (cita de la OMS)](https://www.abc.com.py/nacionales/2023/09/15/consultas-medicas-deben-durar-12-minutos-es-tiempo-suficiente/)
- [Medicina (Buenos Aires) vol. 83 — duración de la consulta](https://www.medicinabuenosaires.com/revistas/vol83-23/n3/489.pdf)

---

### 👤 Docente · 09/10 21:29

hagamos un braisntorm de esto

### 🤖 Equipo

El equipo tiró 16 ideas en 6 enfoques. Te abrí el documento en el editor: [brainstorm-capacidad-agenda.md](../../../../../SourceCode/hospital-turnos/docs/brainstorm-capacidad-agenda.md).

Cada uno defiende una distinta para la primera versión:

| Quién | Idea | Por qué |
|---|---|---|
| 📋 John | **2** Duración mínima por especialidad + **8** alerta al aprobar | Barato, y decide una persona |
| 🎨 Sally | **10** Cupo por canal (ej. 70 % web, 30 % recepción) | Que la web no deje sin turno al que llama por teléfono |
| 📊 Mary | **3** Turno de primera vez (más largo) y de control (más corto) | Es lo más real, y lo más rico para modelar en clase |
| 🏗️ Winston | **14** Consultorios para la versión 2; **13** sobrecupo, descartado | Uno es caro; el otro, riesgoso con pacientes |

**John pregunta: ¿qué ideas entran en la primera versión? Podés elegir varias por número.**

_[Esperando tu respuesta…]_

`[E] Salir del modo equipo`

---

### 👤 Docente · 09/10 21:30

sin tope

### 🤖 Equipo

📋 **John**: Anotado: sin tope. Los turnos por día salen de la agenda aprobada, horas de atención ÷ duración. Las 16 ideas quedan como banco para versiones siguientes, referenciadas en el PRD. La duración mínima sigue en 10 minutos; si la querés en 15, avisame.

🏗️ **Winston**: Con esto el diseño está cerrado de mi lado. Cuando lo apruebes, arranco la arquitectura.

**Sally pregunta: ¿aprobás el diseño de pantallas (versión 0.3, con la elección de médico y la propuesta de agenda)?**

_[Esperando tu respuesta…]_

`[E] Salir del modo equipo`

---

### 👤 Docente · 09/10 21:31

si todo aprobado

### 🤖 Equipo

Los tres documentos quedaron aprobados. Ahora Winston escribe la arquitectura (~4 min).

### 🤖 Equipo

🏗️ **Winston**: La arquitectura está lista y te la abrí en el editor: [04-arquitectura.md](../../../../../SourceCode/hospital-turnos/docs/04-arquitectura.md). Incluye diagramas de contexto, del modelo de clases y de estados.

Las decisiones que más conviene que valides:

- **Una sola aplicación en capas** (Dominio, Aplicación, Infraestructura, Web). Para 400 turnos por día, separarla en servicios no aporta nada.
- **Blazor para todas las pantallas**: todo en C#. Para el paciente son páginas livianas; para la recepción, una pantalla interactiva con teclado.
- **PostgreSQL**: es gratuita y aguanta varios puestos escribiendo a la vez. SQLite no aguanta eso, y SQL Server cuesta licencia.
- **Los turnos duplicados los frena la base de datos**, no sólo el código. Lo mismo pasa con la auditoría: nadie puede modificarla, ni siquiera la aplicación.

📋 **John**: El código escrito al principio no sigue esta arquitectura. Se reemplaza en la primera story.

**Winston pregunta: ¿aprobás la arquitectura? En especial Blazor y PostgreSQL.**

_[Esperando tu respuesta…]_

`[E] Salir del modo equipo`

---

### 👤 Docente · 09/10 21:35

si aprobado

### 🤖 Equipo

Arquitectura aprobada. Sigue el paso de cortar el trabajo en épicas y stories con un plan de sprints (~3 min).

### 🤖 Equipo

📋 **John**: El plan está listo y te lo abrí en el editor: [05-epicas-y-plan.md](../../../../../SourceCode/hospital-turnos/docs/05-epicas-y-plan.md).

| | |
|---|---|
| Épicas | 10 |
| Stories | 34 |
| Pruebas estimadas | ~249 |
| Sprints | 7, de 2 semanas |

Los hitos que importan:

- **Fin del sprint 4**: la recepción ya da y cancela turnos, y reemplaza la planilla de Excel.
- **Fin del sprint 5**: abre la web del paciente.
- **Fin del sprint 7**: versión 1 completa.

Todos los requisitos del PRD tienen al menos una story que los cubre.

🏗️ **Winston**: El orden respeta las dependencias. Primero la base y la bandeja de emails, después las agendas, y recién ahí las reservas.

**John pregunta: ¿aprobás el plan? Si sí, el siguiente paso es escribir cada story en su archivo, con criterios y pruebas por nombre. ¿Las escribo todas o sólo las del sprint 1?**

_[Esperando tu respuesta…]_

`[E] Salir del modo equipo`

---

### 👤 Docente · 09/10 21:37

cuantos sprint nos llevaria hacer todo?

### 🤖 Equipo

📋 **John**: El plan dice **7 sprints de 2 semanas, unas 14 semanas** (3 meses y medio). Pero te debo una aclaración honesta: esos 7 sprints salen de **agrupar las stories según de qué depende cada una**, no de medir la capacidad de un equipo. Nunca definimos quién lo construye, y sin eso cualquier número es una suposición.

🏗️ **Winston**: Según quién lo construya, la cuenta cambia mucho:

| Quién lo construye | Estimación aproximada |
|---|---|
| Equipo humano de 3 o 4 desarrolladores | Los 7 sprints son razonables: ~14 semanas. |
| Un solo desarrollador | El doble o más: 6 a 8 meses. |
| Vos con agentes de IA (implementar story por story con revisión) | El límite pasa a ser tu revisión y tus aprobaciones, no la escritura del código. Podría entrar en pocas semanas, pero no tengo una medición propia para prometer un número. |

**John pregunta: ¿quién lo va a construir? ¿Vos con agentes de IA, alumnos de la materia o un equipo simulado?**

_[Esperando tu respuesta…]_

`[E] Salir del modo equipo`

---

### 👤 Docente · 09/10 21:38

yo y todos ustedes

### 🤖 Equipo

📋 **John**: Entonces cambio la regla: los sprints dejan de ser "2 semanas" y pasan a ser **entregas**. Cada sprint termina con algo que se puede mostrar. Con IA, el ritmo no lo marca escribir código: lo marcan tus revisiones y aprobaciones. Ese ritmo hoy no lo conocemos, así que **lo medimos en el sprint 1** y con eso proyectamos los otros 6. Ya quedó así en el plan.

🏗️ **Winston**: El trabajo de cada story sería este:

1. Escribo la story con sus criterios y sus pruebas por nombre.
2. Una revisión crítica, antes de programar, busca huecos.
3. Vos la aprobás.
4. La implementamos con pruebas y revisores automáticos.
5. Vos aprobás el commit.

📊 **Mary**: Recomiendo escribir **sólo las 6 stories del sprint 1**. Lo que aprendamos ahí mejora las siguientes, y no queda un montón de stories escritas que después hay que corregir.

**John pregunta: ¿aprobás el plan y escribo las 6 stories del sprint 1?**

_[Esperando tu respuesta…]_

`[E] Salir del modo equipo`

---

### 👤 Docente · 09/10 21:39

si aproba

### 🤖 Equipo

Ahora escribo las 6 stories del sprint 1. Antes miro el proyecto guía de testing en C# para usarlo como patrón de referencia, porque el formato de stories lo exige (~1 min).

### 🤖 Equipo

Para que las 6 stories usen los mismos nombres, primero fijo un contrato técnico compartido. Después reparto la escritura entre 3 agentes en paralelo.

### 🤖 Equipo

Lanzo 3 agentes en paralelo, cada uno con 2 stories (~10 min). Te aviso cuando terminen.

### 🤖 Equipo

El plan quedó aprobado y las 6 stories del sprint 1 se están escribiendo.

- Fijé un **contrato técnico compartido** ([00-contrato-tecnico.md](../../../../../SourceCode/hospital-turnos/docs/stories/00-contrato-tecnico.md)), para que las 6 stories usen los mismos nombres de clases, carpetas y tablas.
- **3 agentes** escriben 2 stories cada uno, en paralelo. Tardan unos 10 minutos.

Cuando terminen, sigue esto:

1. Reviso que las 6 sean coherentes entre sí.
2. Corro la **revisión crítica** antes de programar, que busca huecos y contradicciones.
3. Te las paso para que las apruebes.

Te aviso cuando estén listas. Mientras tanto, podés seguir hablándome.

---

### 👤 Docente · 09/10 21:42

estas escribiendo las stories?

### 🤖 Equipo

Sí. Los 3 agentes siguen trabajando, pero todavía no escribieron ningún archivo: por ahora sólo está el contrato técnico. Antes de escribir, cada uno lee la guía de formato, el contrato, la arquitectura y el proyecto guía de C#.

Te aviso cuando terminen.

---

### 👤 Docente · 09/10 21:44

pero no estan generando codigo no?

### 🤖 Equipo

No. Les pedí explícitamente que **no escriban código** y que sólo creen archivos dentro de `docs/stories/`. Tampoco pueden commitear.

El código empieza recién cuando apruebes las stories. Igual, al terminar reviso que no hayan tocado nada fuera de esa carpeta.

---

### 👤 Docente · 09/10 21:45

cuanto tiempo estimas les falta?

### 🤖 Equipo

No tengo una medición, así que te doy un estimado. Arrancaron hace unos pocos minutos y calculé unos 10 minutos en total, así que les faltarían entre 5 y 8. Cada agente escribe 2 stories largas, con criterios, pruebas y notas técnicas, y antes lee varios documentos.

Me avisan solos cuando terminan, y ahí te aviso yo.

### 🤖 Equipo

Ya terminaron 2 de los 3 agentes. Las stories 1.1 a 1.4 están escritas, y sigue trabajando el que hace la 1.5 y la 2.1.

- Ninguno tocó código: sólo crearon archivos en `docs/stories/`.
- Las pruebas salieron más que las estimadas. Por ejemplo, la 1.1 pasó de 3 a 9 y la 1.4 de 8 a 12, porque sumaron casos de falla.
- Corregí dos inconsistencias menores que encontraron: el nombre del archivo de la solución y una dependencia de la 1.3.

Hay una decisión tuya pendiente. El repo **no tiene ningún commit**, y la story 1.1 borra el código del arranque. Para no perderlo (algunas ideas se reaprovechan en la 4.1), conviene hacer antes un **commit inicial** con ese código y los documentos.

¿Hago ese commit inicial en el repo local? No se sube a ningún lado.

### 🤖 Equipo

Terminó el tercer agente: ya están escritas las 6 stories del sprint 1. Lanzo en paralelo la revisión crítica, que sólo lee, busca huecos y contradicciones entre las 6, y deja los hallazgos en un archivo (~10 min).

### 🤖 Equipo

Están las 6 stories y la revisión crítica ya está corriendo.

| Story | Criterios | Pruebas (estimadas → reales) |
|---|---|---|
| 1.1 Estructura | 14 | 3 → 9 |
| 1.2 Reloj y errores | 13 | 6 → 8 |
| 1.3 Auditoría | 11 | 7 → 8 |
| 1.4 Bandeja de emails | 17 | 8 → 12 |
| 1.5 Integración continua | 12 | 1 → 3 |
| 2.1 Ingreso del personal | 23 | 9 → 22 |
| **Total** | **90** | **34 → 62** |

Las pruebas casi se duplicaron, porque cada caso normal ahora tiene su caso de falla. La revisión crítica va a verificar, entre otras cosas, dos decisiones de la 2.1 que me hacen ruido: cómo se cuenta el bloqueo por intentos y con qué canal se audita el ingreso del personal.

Sigue pendiente tu decisión sobre el **commit inicial** en el repo local, para no perder el código del arranque. ¿Lo hago?

### 🤖 Equipo

Terminó la revisión crítica: **36 hallazgos**, 3 críticos, 17 importantes y 16 menores. Hoy ninguna story está lista para programar, pero todo se arregla corrigiendo el texto de las stories, sin rediseñar nada.

Los 3 críticos:

- **1.1 y 1.3 arman la base de pruebas de forma distinta.** Con eso, las pruebas de ingreso fallarían por permisos, y la prueba de permisos de la auditoría pasaría sin probar nada.
- **2.1, ingreso fallido:** con un usuario inexistente, el registro de auditoría queda incompleto. La persona vería un error genérico en vez de "Usuario o contraseña incorrectos".
- **2.1, verificación:** usa un comando que la 1.1 prohíbe y que falla por permisos.

Otro hallazgo importante: el ingreso de médicos y administradores se auditaba como si fuera de la recepción. El detalle de los 36 está en [revision-sprint-1.md](../../../../../SourceCode/hospital-turnos/docs/stories/revision-sprint-1.md).

¿Corrijo los críticos y los importantes, y dejo los menores anotados para después?

---

### 👤 Docente · 09/10 23:31

Si corrige todo

### 🤖 Equipo

Los 3 agentes están corrigiendo, y ninguno toca código. Cuando terminen, reviso que los arreglos no se contradigan entre sí y te paso las stories para aprobar.

Queda pendiente, para antes de programar: ¿hago el **commit inicial** en el repo local, para no perder el código del arranque?

---

### 👤 Docente · 09/10 23:32

Commit local

### 🤖 Equipo

Hecho: el commit inicial quedó en el repo local, en la rama `master`, sin subir a ningún lado. Incluye los 4 documentos aprobados, el brainstorm y el código del arranque.

Dejé afuera a propósito el plan de sprints y las stories, porque los agentes los están corrigiendo ahora. Van en un segundo commit cuando apruebes las stories corregidas.

### 🤖 Equipo

Terminó el agente de la 1.3 y la 1.4, con todos sus hallazgos aplicados. El único punto que le quedaba afuera, el de la arquitectura, lo corregí yo. Siguen trabajando los otros dos.

Al final reviso un cruce que avisó: la 1.4 ahora necesita configuración de correo en la base de pruebas que arma la 1.1.

### 🤖 Equipo

Terminó el segundo agente: el contrato, la 1.1 y la 1.2 quedaron corregidos. El cruce de nombres entre stories que avisaba ya estaba resuelto: lo verifiqué y la 1.3 y la 1.4 usan los nombres del contrato.

Falta el agente de la 1.5 y la 2.1, la story con más hallazgos.

### 🤖 Equipo

Las 3 correcciones terminaron. Cierro los registros: actualizo el plan y marco la revisión como aplicada.

### 🤖 Equipo

Los **36 hallazgos están corregidos** en las 6 stories, y verifiqué que no se contradigan entre sí. Ninguna toca código todavía.

| Story | Criterios | Pruebas |
|---|---|---|
| 1.1 Estructura | 20 | 14 |
| 1.2 Reloj y errores | 14 | 9 |
| 1.3 Auditoría | 11 | 8 |
| 1.4 Bandeja de emails | 18 | 13 |
| 1.5 Integración continua | 13 | 4 |
| 2.1 Ingreso del personal | 27 | 28 |
| **Total** | **103** | **76** |

Los cambios de fondo los tomé yo, con la opción más segura en cada caso. El ingreso del personal se audita con su propio canal. No se guarda lo que la persona tipea si el usuario no existe. El bloqueo no revela qué usuarios existen. Ninguna contraseña queda en el repo. La sesión del personal se revalida cada 5 minutos, así un usuario desactivado queda afuera enseguida.

Quedan 2 detalles técnicos de .NET 10 que se confirman al programar; cada story trae una alternativa por si fallan. El resumen está al final de [revision-sprint-1.md](../../../../../SourceCode/hospital-turnos/docs/stories/revision-sprint-1.md).

¿Aprobás las 6 stories para empezar a implementar por la 1.1?

---

### 👤 Docente · 10/10 09:13

me abris la carpeta donde esta este proyecto en el finder

### 🤖 Equipo

Listo, te abrí en el Finder la carpeta `~/SourceCode/hospital-turnos`. Los documentos están en `docs/` y las stories en `docs/stories/`.

---

### 👤 Docente · 10/10 09:14

y las otras carpetas qeu son?

### 🤖 Equipo

| Carpeta / archivo | Qué es |
|---|---|
| `docs/` | Los documentos: brief, PRD, diseño, arquitectura, plan y las stories. |
| `src/` | El código del arranque, que **se descarta**: la story 1.1 lo borra y arma las 4 capas nuevas. |
| `tests/` | Las pruebas de ese mismo código del arranque, que también se descartan. |
| `HospitalTurnos.slnx` | El archivo de la solución de .NET, el que abrís con Visual Studio o Rider. |
| `.git/` (oculta) | El historial del repo, donde está el commit inicial. |
| `.engram/` (oculta) | La memoria persistente de los agentes, que se guarda junto al repo. |
| `.gitignore` | La lista de lo que git no tiene que guardar, como los archivos compilados. |

Hoy `src/` y `tests/` sólo tienen ese código provisorio. Cuando arranquemos la story 1.1, quedan con la estructura definitiva.

---

### 👤 Docente · 10/10 09:20

me creas un repo en github para poner este proyecto, de nombre "BMAD-Hospital" y subi ahi por favor asi lo puedo compartir con los chicos. crea una carpeta aparte de nombre Sesiones y ahi guardaremos las conversaciones por fecha, el registro de esta conversasion con el nombre 01-Inicio-2026-10-09-2030.md

### 🤖 Equipo

Antes de publicar reviso que no se filtre nada interno del equipo de trabajo, como rutas o nombres de clientes (~1 min).

### 🤖 Equipo

Ahora escribo el registro de la sesión para los alumnos.

### 🤖 Equipo

Commiteo todo y creo el repo público en GitHub, así los alumnos lo ven sin necesitar permisos.

### 🤖 Equipo

Creé el repo y subí el proyecto: **https://github.com/fernandobono/BMAD-Hospital**

- **Es público**, para que los alumnos lo vean sin pedir acceso. Si lo preferís privado, se cambia en un minuto.
- **Sesiones/01-Inicio-2026-10-09-2030.md**: el registro de esta conversación, desde "quiero hacer una gestión de turnos" hasta las stories revisadas. Tus mensajes van textuales y las respuestas del equipo, resumidas. Sumé recuadros "Para la clase", entre ellos el error de arrancar a programar sin definir el producto.
- **README**: agregué uno corto con el orden de lectura de los documentos, porque el repo no tenía portada.
- **Antes de subir** saqué dos menciones internas de tu trabajo que se habían colado en las stories. El enlace al proyecto guía ahora apunta a su repo público.
- Renombré la rama de `master` a `main`, igual que en el repo de testing.

Anoté en mi memoria la convención de `Sesiones/`, para cerrar cada próxima sesión con su registro.

Sigue pendiente aprobar las 6 stories para empezar a programar la 1.1.

---

### 👤 Docente · 10/10 09:24

y puedes volvar el contenido de esta conversasion en un archivo .md tambien dentro de sesiones con la fechayhora qeu arrancamos?
