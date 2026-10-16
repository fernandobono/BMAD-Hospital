# Sesión 03 — Conversación completa

**Inicio:** 15 de octubre de 2026, 20:54 (hora de Argentina)
**Participantes:** el docente y el equipo de agentes BMAD (Amelia, la desarrolladora, y los revisores).

> Transcripción de la conversación: todos los mensajes del docente y todas las respuestas del
> equipo, en orden. No incluye las operaciones internas (lecturas de archivos, comandos,
> búsquedas) ni los avisos automáticos de las tareas en segundo plano. La versión resumida, con
> comentarios para la clase, es [03-Story-1-2-2026-10-15-2054.md](03-Story-1-2-2026-10-15-2054.md).

---

### 👤 Docente · 15/10 20:54

Sigamos con BMAD-Hospital (~/SourceCode/hospital-turnos). Esta es la sesión 03.

Estado: la story 1.1 está mergeada en main (sesión 02). Las stories 1.2 a 2.1 del sprint 1 están aprobadas.

Implementemos la story docs/stories/1-2-reloj-y-errores.md con el circuito completo, igual que la 1.1:
validación, plan para que yo lo apruebe, pruebas primero, revisores y prueba en vivo.

Reglas de esta sesión:
- Sin registro de horas y sin pase adversarial.
- Una decisión por vez, con la opción recomendada primero.
- Al terminar: commit, push y merge a main, sólo cuando yo lo apruebe.
- Al cerrar: exportá la sesión en Sesiones/ con el resumen, la transcripción completa y las capturas de lo que abras en el navegador.

### 🤖 Equipo

Arranco el circuito completo (sin registro de horas ni pase adversarial, como pediste).

Rama `story/1-2-reloj-y-errores` creada y el circuito arrancado; ahora lanzo la validación de la story.

Lanzo el validador de la story en segundo plano (~3-5 min). Mientras, leo la story yo para preparar el plan.

Mientras corre el validador, hago la salvaguarda de estado de la Task 1 (sólo lectura).

La imagen ya trae la zona de Córdoba (el Dockerfile no se toca). Verifico que la opción de .NET que pide la story exista en el SDK instalado.

La story pasó la validación sin nada que bloquee, y el punto de partida está en orden:

- Las pruebas de la story 1.1 dan 15 de 15 en verde.
- Hoy nadie lee la hora del sistema en el código.
- La imagen de la web ya trae la zona horaria de Córdoba, así que el Dockerfile no se toca.
- La opción de .NET para registrar cada error una sola vez existe en la versión instalada.

El validador encontró dos detalles de redacción en la story:

1. El estado figura distinto en el encabezado (`in-progress`) y en el cuerpo (`ready-for-dev`). Es cosmético y queda anotado.
2. La Task 5 pide agregar un paquete de pruebas a dos proyectos que ya lo tienen, porque lo dejó la 1.1. En la práctica esa tarea no cambia nada.

**¿Cómo seguimos?**
- **(a) Corregir la story antes de implementar (recomendado):** marco la Task 5 como "ya resuelta por la 1.1", saco esos dos proyectos de la lista de archivos a modificar y alineo el estado del cuerpo. Así la story describe lo que de verdad se hizo.
- **(b) Seguir sin tocarla:** los dos avisos quedan registrados en la corrida.
- **(c) Cancelar.**

Una aclaración aparte: la herramienta sugiere un modelo más grande para esta story. Es sólo una sugerencia; sigo con el actual salvo que prefieras cambiarlo con `/model`.

---

### 👤 Docente · 15/10 21:07

a

### 🤖 Equipo

Re-corro el validador sobre la story corregida (~1 min).

Mientras tanto leo el paso de armado del plan.

Story re-validada: limpia. Cierro la validación y armo el plan.

El plan está listo. Las pruebas se escriben primero; cada bloque arranca con ellas en rojo y recién después va el código:

| Bloque | Qué se hace | Pruebas |
|---|---|---|
| A · Reloj | Un reloj único con la zona de Córdoba que falla con un mensaje claro si la máquina no la tiene. Se agrega un atajo "hora local del hospital" y una prueba que impide leer la hora del sistema en cualquier otro lugar. | T1–T5 |
| B · Errores | Cuatro errores del dominio, con el mensaje escrito para el usuario, y el manejador que los traduce: 409, 400, 404, 403, o 500 genérico. | T6–T7 |
| C · Web | El manejador se activa en todos los entornos, también en desarrollo. La página `/Error` pasa a decir "Algo falló. Probá de nuevo en unos minutos." Cada error se registra una sola vez. | T8–T9 y la regresión de la 1.1 |
| Verificación | Compilación sin advertencias, todas las pruebas, y prueba en vivo con el compose y capturas del navegador. | — |

En total son 16 archivos nuevos y 3 modificados. El Dockerfile y los proyectos de prueba no cambian.

Tres riesgos a la vista:
- **Prueba en vivo de un error real:** la app no tiene, ni debe tener, una ruta que falle a propósito. En el navegador muestro la página `/Error`, y la falla real la demuestran las pruebas T8 y T9.
- **Modo desarrollo:** si apareciera la página con la pila técnica, sería señal de que el manejador quedó mal ubicado. La prueba T8 lo controla.
- **Pruebas unitarias del manejador:** quizá necesiten un contenedor de servicios mínimo. No cambia nada de lo que se prueba.

**¿Apruebo el plan para pasar a la implementación?** Recomiendo que sí.

El plan detallado está en (la carpeta local de la corrida).

---

### 👤 Docente · 15/10 21:09

si aprobado

### 🤖 Equipo

Plan aprobado. El chequeo previo de alcance se saltea por su condición documentada: la story quedó limpia y el plan no toca archivos fuera de los declarados. Empiezo escribiendo las pruebas.

Escribo las pruebas del bloque A (reloj).

Uso la herramienta de escritura para que el registro vea los archivos.

Ahora las pruebas del bloque B (manejador de errores).

Y las del bloque C (extremo a extremo).

Bloque B: excepciones y manejador.

Compilo y corro las pruebas de la story: espero A y B en verde y C todavía en rojo, porque la web aún no usa el manejador (~1-2 min).

Rojo legítimo: la excepción se escapa sin manejar, y en desarrollo aparece la página técnica. Conecto el manejador en la web y reescribo la página `/Error`.

Todo en verde: 13 de 13 en web. Antes de seguir confirmo que la prueba guardiana de la hora realmente atrapa una lectura del sistema: planto una a propósito y la saco.

La guardiana atrapa la lectura plantada, y la sonda ya está borrada. Sigo con los portones del paso 4: compilación en Release y la suite completa.

Compilación en Release: 0 advertencias. Corro la suite completa con cobertura, proyecto por proyecto (~2-3 min, en segundo plano).

Mientras corre, actualizo el registro de desarrollo de la story.

Story actualizada. Espero el resultado de la suite completa.

Mido la cobertura de los archivos nuevos con un corrido aparte (~40 s).

Cobertura combinada de los archivos nuevos: 100% de líneas (el manejador 25/25 y el reloj 14/14). Cierro el paso 4.

Paso 4 cerrado. El chequeo de mutantes no sabe correr pruebas de .NET y sólo avisa, sin frenar; lo compensé con la sonda manual. Paso a los revisores.

Dos revisores terminaron sin bloqueos. El aviso sobre la regresión de la 1.1 ya está cubierto: los 28 casos de web incluyen sus 15 pruebas. Lanzo los dos que faltan.

Mientras terminan los dos últimos, corrijo en la story el aviso del Dockerfile. Es sólo documentación: lo saco de la lista de archivos a modificar y anoto por qué no se tocó.

Cinco de seis terminados, ninguno bloquea. Falta el de fallos silenciosos.

Ahora dejo constancia de que el pase adversarial se salteó por decisión tuya, y armo el informe de hallazgos.

El portón pide que el apagado del pase adversarial quede declarado. Lo anoto en la configuración local del repo, que no se versiona: es tu decisión de estas sesiones.

La revisión dio esto:

- **Pruebas:** la suite completa pasa 41 de 41, incluidas las 15 de la 1.1, y los archivos nuevos quedaron cubiertos al 100%.
- **Seguridad y alcance:** limpios. No hay nada fuera de lo que pide la story ni dependencias nuevas.
- **Ya arreglado:** la story declaraba el Dockerfile como archivo a modificar. Lo corregí, porque no hizo falta tocarlo.
- **Avisos que no cambian código:**
  - Un error de regla que llega desde el navegador muestra la página genérica. Es intencional y está escrito en la story.
  - La herramienta no pudo medir la cobertura sola, así que la medí a mano.

Queda un único punto "crítico" que necesita tu decisión. El chequeo automático marcó que la prueba de regresión de la 1.1 (`/salud` con la base caída) "no existe". Es un falso positivo: el chequeo sólo buscó en los archivos de la 1.2, y esa prueba vive en los de la 1.1. Existe, pasó en esta corrida, y el revisor de criterios lo confirmó.

**¿Lo damos por falso positivo y seguimos a la prueba en vivo?**
- **(a) Sí, es falso positivo (recomendado):** queda registrado con su motivo y seguimos.
- **(b) No:** prefiero revisarlo yo primero.

El informe completo está en (la carpeta local de la corrida).

---

### 👤 Docente · 15/10 21:25

a

### 🤖 Equipo

Arranco la prueba en vivo. Primero reconstruyo la app en Docker con el código nuevo (~1-2 min, en segundo plano).

App arriba con el código nuevo. Veo qué criterios pide la matriz de prueba en vivo.

Escribo los chequeos en vivo de cada criterio como un guion fuera del repo. Cada uno pasa sólo si la app real responde lo que pide la story.

Corro los chequeos en vivo, registrados como evidencia (~3 min, en segundo plano).

Mientras corren, declaro en la story el único criterio que no se puede ver con la app en marcha: el AC 3, el cambio de día a las 23:30, que necesita un reloj fijo.

Siete de nueve chequeos pasan. Dos fallan, y miro por qué antes de tocar nada.

Los 9 chequeos en vivo pasan. Saco las capturas del navegador (Chrome sin ventana) para el registro de la sesión.

Matriz en verde: los 14 criterios se vieron corriendo. Dejo la constancia de la prueba en vivo y limpio los contenedores de la sonda.

Dejo la sonda fuera del repo, como corresponde, y en el informe anoto sólo su ruta.

No hace falta guía para el tester, porque la story no toca pantallas operables. Completo la story (Task 7, lista de archivos, historial de cambios) y paso a preparar el commit.

La story 1.2 quedó implementada y verificada, y pasó a "en revisión". Falta sólo tu autorización para el commit.

| Qué | Estado |
|---|---|
| Reloj único con la zona de Córdoba, y la app no arranca si falta la zona | Hecho |
| Errores sin detalles técnicos para el usuario; el detalle va sólo al registro técnico | Hecho |
| Pruebas, escritas antes que el código | 41 de 41: 18 casos nuevos más las 15 de la 1.1 |
| Revisión (6 revisores) | Sin bloqueos y sin cambios de código |
| Prueba en vivo en Docker, con capturas | Los 14 criterios vistos corriendo |

Dos cosas de la prueba en vivo:
- **Errores reales sin ruta de prueba:** para provocarlos usé una sonda que vive fuera del repo y se carga sólo en un contenedor aparte. Así se vio el manejador real, también en modo desarrollo, sin tocar el código de producción.
- **El AC 3 (cambio de día) se vio en vivo:** en UTC ya era 16/10 y en Córdoba seguía siendo 15/10. No te queda nada por firmar.

**¿Hago el commit en la rama `story/1-2-reloj-y-errores`?** Recomiendo que sí. Las capturas de `Sesiones/img/` van aparte, en el commit de cierre de la sesión, como en la 02. El push y el merge a main te los pido después, por separado.

El mensaje propuesto está en (la carpeta local de la corrida).

---

### 👤 Docente · 15/10 21:33

antes del commit hace resumen de la sesion y ponela junto a la conversacion como las otras y luego commit, push, pr, actauliza esto https://claude.ai/artifact/2Cns8Kvpu48cWqpaBHPvWV  y dame el prompt para poder seguir en una sesion limpia y cerrar esta

### 🤖 Equipo

Arranco: primero el registro de la sesión (resumen y transcripción completa), después el commit, el push y el PR, la página del plan y el prompt para seguir.

Armo la transcripción completa desde el registro de la conversación, con un conversor fuera del repo, y limpio las referencias internas como en las sesiones anteriores.

Transcripción lista. Escribo el resumen curado con los recuadros "Para la clase" y las capturas.

Resumen listo. Regenero la transcripción para que incluya hasta este pedido y reviso qué entra en los commits.
