# Sesión 14 — Conversación completa

**Inicio:** 17 de octubre de 2026, 08:53 (hora de Argentina)
**Participantes:** el docente y el equipo de agentes BMAD (Winston ajusta la CI; Mary valida la story;
Amelia implementa; los revisores y un tester no técnico revisan).

> Transcripción de la conversación: todos los mensajes del docente y todas las respuestas del
> equipo, en orden. No incluye las operaciones internas (lecturas de archivos, comandos,
> búsquedas) ni los avisos automáticos de las tareas en segundo plano. La versión resumida, con
> comentarios para la clase, es [14-Story-2-3-2026-10-17-0853.md](14-Story-2-3-2026-10-17-0853.md).

---

### 👤 Docente · 17/10 08:53

Sigamos con BMAD-Hospital (~/SourceCode/hospital-turnos). Esta es la sesión 14.

Estado: los sprints 1 y 2 están cerrados, con sus retrospectivas (docs/retrospectiva-sprint-1.md y
docs/retrospectiva-sprint-2.md). En la sesión 13 se recalculó la proyección (opción A: 15,7 min de
reloj por prueba estimada, con los dos sprints) y se escribieron, revisaron y aprobaron las stories
del sprint 3: 2.3, 4.2, 4.3, 4.4 y 5.1 (ready-for-dev, en main con el PR #11). Orden de
implementación: 2.3 → 4.2 → 4.3 → 4.4 → 5.1. El registro de la sesión 13 está en Sesiones/.

Primero, dos pendientes de la retrospectiva (acciones B-3 y B-4), de a una:
1. La CI tiene dos avisos: las acciones usan Node.js 20 y ubuntu-latest pasa a Ubuntu 26 desde el
   19/10. Proponé cómo resolverlos y, cuando lo apruebe, hacelo en su propia rama y PR, con la CI en
   verde antes de pedirme el merge.
2. Decidir si se protege main para que no se pueda mergear con la CI en rojo (opciones, con la
   recomendada primero).

Después implementemos la story docs/stories/2-3-contrasena-olvidada.md con el circuito completo,
igual que la 4.1: validación contra el repo de hoy, plan para que yo lo apruebe, pruebas primero,
revisores, tester no técnico y prueba en vivo. Antes de pedirme el merge, el PR tiene que tener la
CI en verde. Medí cuánto tarda con los dos cortes de la retrospectiva (hasta el PR y sesión
completa), contra la proyección: 4 pruebas estimadas × 15,7 min ≈ 1 h 03.

Reglas de esta sesión:
- Sin registro de horas.
- Una decisión por vez, con la opción recomendada primero.
- El tiempo se prueba siempre con el reloj inyectado, nunca esperando tiempo real.
- Commit, push y PR sólo cuando yo lo apruebe.
- Al cerrar: exportá la sesión en Sesiones/ con el resumen, la transcripción completa (sin
  contraseñas ni direcciones de ngrok: el repo es público) y las capturas de lo que abras en el
  navegador (A4: generar la contraseña temporal, el primer ingreso con el cambio obligatorio, el
  rechazo para un usuario desactivado, y las corridas de la CI de los PR), y actualizá la página del
  plan de sprints (https://claude.ai/artifact/2Cns8Kvpu48cWqpaBHPvWV).

### 🤖 Equipo

Arranco: tomo la hora de inicio y miro el estado del repo y la CI.

Ya tengo el diagnóstico de los dos avisos de la CI. Te paso la propuesta.

**Aviso 1 — Node.js 20.** Las cuatro acciones (descargar el código, instalar .NET, subir y bajar resultados) están en versiones viejas que corren sobre Node.js 20. Las versiones nuevas ya corren sobre Node 24. Mirando sus notas de cambios, ninguna rompe lo que usamos. La única diferencia de comportamiento: la de bajar resultados ahora corta la corrida si el archivo llega alterado, y eso es más seguro.

**Aviso 2 — Ubuntu 26.** Hoy la máquina de la CI es Ubuntu 24.04 porque así está "la última". Propongo fijar `ubuntu-24.04` con nombre propio. Así el pase a 26 queda como una decisión nuestra y no como una sorpresa el lunes 19, en el medio de una story.

| Cambio | Detalle |
|---|---|
| Acciones | checkout v4→v7, setup-dotnet v4→v6, upload-artifact v4→v7, download-artifact v4→v8 |
| Máquina | `ubuntu-latest` → `ubuntu-24.04` en los 6 trabajos, con un comentario que explica por qué |
| Rama y PR | `ci/avisos-node24-ubuntu`, la CI tiene que salir en verde y sin los dos avisos antes de pedirte el merge |

Lo que se pierde: 24.04 también va a vencer algún día (alrededor de 2028), así que queda anotado que es una postergación a propósito.

**¿Apruebo así o preferís pasar ya a `ubuntu-26.04`?**

---

### 👤 Docente · 17/10 09:05

Si vamos con la 24.04

### 🤖 Equipo

Ahora el comentario de cabecera que explica por qué la máquina queda fija, y reviso otras menciones.

Hay una prueba del propio workflow que exige `ubuntu-latest`; la miro antes de seguir.

La prueba del workflow fija las versiones viejas; la actualizo para que exija las nuevas y la corro.

Falla contra el archivo viejo y pasa con el nuevo (incluye el revisor `actionlint`). Dejo constancia en la story 1.5, que describía las versiones viejas.

El cambio de la CI está listo en la rama `ci/avisos-node24-ubuntu`, pero todavía no está commiteado.

| Cosa | Estado |
|---|---|
| Acciones en versiones de Node.js 24 y máquina fija en `ubuntu-24.04` | Hecho |
| Prueba del workflow | Hecho. Ahora exige las versiones nuevas. Falla con el archivo viejo y pasa con el nuevo, incluido el revisor `actionlint` |
| Nota en la story 1.5, que describía las versiones viejas | Hecho |
| CI en GitHub | Todavía no corrió: para eso hace falta subir la rama |

Apareció algo que no estaba en la propuesta: la prueba del workflow exigía `ubuntu-latest` y `@v4`. Sin ese ajuste, la prueba habría fallado.

El mensaje del commit sería este:

```
ci: acciones con Node.js 24 y Ubuntu 24.04 fijo (acción B-3)

checkout v7, setup-dotnet v6, upload-artifact v7, download-artifact v8;
runs-on ubuntu-24.04 en lugar de ubuntu-latest. probar-workflow.py exige
las versiones nuevas; nota en la story 1.5.
```

**¿Apruebo commit, push y PR?** Después espero la CI en verde y sin los dos avisos, y recién ahí te pido el merge.

---

### 👤 Docente · 17/10 09:11

Si

### 🤖 Equipo

Engram no tenía nada nuevo que exportar. Commiteo, subo y abro el PR.

PR #12 abierto. Espero la CI (unos 3 minutos) y después reviso si quedaron los avisos.

Mientras corre la CI, preparo la segunda decisión (proteger `main`). Antes miro cómo se vino trabajando sobre `main`.

Mientras termina la CI: hoy `main` no tiene ninguna protección. Además, los merges se hacen en la máquina y después se suben, y la nota de "done" y el registro de la sesión se suben directo a `main`. Eso pesa en la decisión de proteger `main`, y te la planteo apenas te pida el merge del PR #12.

La CI del PR #12 salió en verde: pasaron los 6 trabajos, en 3 min 42 s.

| Cosa | Resultado |
|---|---|
| Aviso de Node.js 20 | Desapareció: la corrida no tiene ningún aviso |
| Máquina | `ubuntu-24.04`, confirmado en el registro de la corrida |
| Corrida | [39031611522](https://github.com/fernandobono/BMAD-Hospital/actions/runs/39031611522) |

**¿Mergeo el PR #12 a `main`?** Lo haría desde GitHub, como el PR #11. Después te planteo la decisión de proteger `main`.

---

### 👤 Docente · 17/10 09:21

si

### 🤖 Equipo (cuadro de opciones)

**¿Cómo protegemos main (acción B-4)?**
- PR obligatorio + CI verde (Recomendada): Regla de GitHub sobre main: todo entra por PR, los 6 trabajos de la CI tienen que estar en verde y no se puede reescribir la historia. Sin excepción ni para vos. La nota de 'done' y el registro de la sesión van como últimos commits del PR, antes del merge. Es la práctica real que conviene mostrar en clase.
- Lo mismo, con excepción para vos: Igual que la primera, pero como dueño podés saltearla. Sirve como red ante un descuido en la web, pero no frena un push directo: GitHub sólo avisa.
- Sin protección, por disciplina: Como hasta ahora: no se pide el merge sin la CI en verde. Ya funcionó en dos sprints, pero depende de que nadie se olvide.

---

### 👤 Docente (respuesta en el cuadro de opciones) · 17/10 09:25

Your questions have been answered: "¿Cómo protegemos main (acción B-4)?"="PR obligatorio + CI verde (Recomendada)". You can now continue with these answers in mind.

### 🤖 Equipo

Configuro la regla en GitHub con los nombres exactos de los 6 trabajos de la CI.

Mismo circuito que la 4.1, más el tester no técnico (acción B-6). Arranca el reloj de la story; leo la story 2.3.

La story está muy completa: 17 criterios y 16 pruebas, con el código casi entero escrito. Miro cómo se orquestó la 4.1 para repetir el mismo circuito.

En la 4.1 el circuito fue la skill `tycon-pipeline`. La invoco igual para la 2.3.

### 🤖 Equipo

El docente pidió "sin registro de horas": salteo el reloj, igual que en la 4.1. Sin configuración de ADO, el pedido en ADO tampoco aplica. Paso a validar la story.

Lanzo al validador de la story en segundo plano (unos 3 a 5 minutos). Mientras tanto, yo mismo confirmo contra el código de hoy lo que la story dice que existe.

Todo lo que cita la story existe con esos nombres. Corro la suite completa como línea de base, antes de tocar nada (unos 3 minutos, en segundo plano).

El control de pruebas no deja correr la suite entera sin alcance. Corro la línea de base proyecto por proyecto, uno tras otro, que sí está permitido.

El control no reconoce la variable del bucle. Escribo las rutas de los tres proyectos a mano.

El validador terminó sin nada que bloquee. Antes de darle la razón, confirmo uno de sus avisos: dice que `FormularioHttp.EnviarAsync` no existe, pero las pruebas de A4 lo usan.

`EnviarAsync` sí existe (`FormularioHttp.cs:121`): ese aviso del validador es un error suyo. Miro cómo se manejaron los vínculos entre stories antes.

La story está bien anclada al código de hoy y no hay nada que bloquee. Quedan arreglos chicos, y es la primera story del repo que declara que continúa a otra (la 2.2).

| Aviso | Qué es | Propuesta |
|---|---|---|
| La 2.2 no dice que la 2.3 la continúa | Falta el vínculo de vuelta | Agregarlo en la 2.2 |
| Estado y commit de referencia viejos | La línea del estado dice `ready-for-dev` y el commit verificado es el anterior al PR #12 | Actualizar los dos |
| **Lo encontré yo:** una ayuda de prueba lee todo como texto | `"desbloqueado": true` es un valor de verdad y la ayuda se rompe con él (T1 y T3) | Anotar en la story que esas dos pruebas leen el detalle sin esa ayuda, con un lector propio de JSON |
| El criterio 17 (no romper nada) no tiene fila de prueba | Lo cubre la suite completa, igual que en las stories anteriores | Dejarlo así |
| "`EnviarAsync` no existe" | Falso: existe | Nada |

**¿Arreglo la story así antes de armar el plan?** Es lo que recomiendo. La alternativa es seguir sin arreglarla, dejando los avisos registrados.

La línea de base salió en verde: 481 pruebas (138 de Dominio, 169 de Aplicacion y 174 de Web), sin fallas. Es el mismo total que dejó la 4.1.

Sigo esperando tu respuesta: **¿arreglo la story antes de armar el plan?**

---

### 👤 Docente · 17/10 09:34

si arreglala

### 🤖 Equipo

Arreglé las dos stories. Vuelvo a correr el validador sobre la versión corregida (unos 2 minutos, en segundo plano) y mientras tanto leo el paso del plan.

Engram no tiene notas de este proyecto: las lecciones previas salen de la propia story y de los registros de las sesiones. Invoco a Amelia (`bmad-dev-story`) en modo plan, sin tocar archivos.

### 🤖 Equipo

La segunda validación salió limpia: el único aviso es el del criterio 17, que dejamos así a propósito. Guardo el informe y armo el plan.

El plan está listo. Es el mismo circuito de la 4.1: cada tramo escribe primero sus pruebas.

| Tramo | Qué | Pruebas |
|---|---|---|
| 1 · Pruebas del caso de uso | Las 9 pruebas contra PostgreSQL real, antes del código. Deberían arrancar en rojo, porque el método todavía no existe | T1–T9 |
| 2 · El caso de uso | Generar la temporal, invalidar la anterior, desbloquear y auditar sin la contraseña, todo en una sola transacción. Si otro administrador cambió al usuario recién, se responde con un cartel y no con un error interno. Rompo a propósito esa traducción para ver fallar a T8 | T1–T9 en verde |
| 3 · Pantalla A4 | Primero las pruebas HTTP. Después el botón "Nueva contraseña temporal" en las filas de usuarios activos ajenos, y el mensaje con la contraseña | T10–T16 |
| 4 · Verificación | Suite completa y CI local, guía del tester y pasada del tester no técnico, revisores, prueba en vivo en Chrome con las capturas que pediste | — |

- No hay migración ni pantalla nueva, y la 2.2 no se toca.
- Son 16 pruebas contra las 4 que estimaba el plan de sprints. Para medir el tiempo, la referencia sigue siendo la proyección de 1 h 03.

El plan completo está en `.advisors/pipeline-runs/2.3-20261017122735/step-02-bmad-plan.md`.

**¿Apruebo el plan y arranco con el tramo 1?**

---

### 👤 Docente · 17/10 09:44

si

### 🤖 Equipo

Escribí T1–T9. Agrego dos referencias que faltan y las corro para ver el rojo esperado: no tienen que compilar, porque el método todavía no existe.

El rojo es el correcto: falla porque falta `ContrasenaRestablecida`, que es justo lo que la story pide crear. Ahora el tramo 2: la constante, el contrato, el doble de pruebas y la implementación.

Compila sin advertencias. Corro T1–T9 contra PostgreSQL (unos 30 segundos).

T1–T9 en verde: son 11 casos, porque T6 prueba tres actores. Ahora rompo a propósito la traducción de la carrera, para confirmar que T8 la detecta.

Falló exactamente como pedía la story: llega `InvalidOperationException` con `ConcurrencyFailure`. Restauro la traducción.

Escribo T10–T16 con la herramienta de edición, al final de las pruebas HTTP de A4.

Corro T10–T16 antes de tocar la pantalla: tienen que fallar porque falta el botón (alrededor de 1 minuto).

Mientras corre la suite, escribo la guía del tester. Tomo como modelo la de la 2.2.

El portón frenó porque la story toca 6 proyectos y no sabe cuál mirar. Veo cómo se resolvió en la 4.1.

La solución del repo es `.slnx` (el formato nuevo) y el portón busca `.sln`. Pruebo pasándole la raíz como alcance (unos 3 minutos, en segundo plano).

Mientras corre, escribo la guía del tester. Voy criterio por criterio y en castellano llano.

El portón no reconoce las soluciones `.slnx`, el formato nuevo de .NET 10. Busco cómo cerró este paso la 4.1.

En la 4.1 se resolvió igual: el portón no reconoce `.slnx` y se corrió proyecto por proyecto. Hago lo mismo con los tres proyectos de pruebas, uno tras otro (unos 2 minutos, en segundo plano).

Mientras corre, preparo la pasada del tester no técnico. Miro el formato que dejó la 3.2.

La pasada sobre las capturas de pantalla viene después de la prueba en vivo. La pasada sobre la guía la lanzo ya, en segundo plano.

Corren dos cosas en segundo plano: el portón de pruebas, proyecto por proyecto, y la pasada del tester no técnico sobre la guía. Te aviso cuando terminen las dos.

El tester dejó 13 marcas, casi todas de continuidad: no queda claro qué contraseña tiene ana.ruiz en cada paso, ni si está bloqueada o con la sesión abierta. Reescribo la guía con contraseñas de nombre fijo y una tabla de estados. Antes confirmo los textos reales de pantalla.

El ingreso muestra "Usuario o contraseña incorrectos." también cuando la persona está bloqueada, a propósito, desde la 2.1. Reescribo la guía.

En el menú no hay un enlace a "Cambiar contraseña". Corrijo esa línea de la guía antes de relanzar al tester.

Mientras corren la CI local y el tester, completo el registro del desarrollador en la story y guardo la primera pasada del tester en la carpeta de la corrida.

Tildé las tareas 1 a 5. Espero la CI local y la segunda pasada del tester para cerrar la implementación y lanzar a los revisores.

Quedan 3 marcas chicas. Antes de corregirlas, confirmo dónde está "Salir" en la pantalla.

Las 3 marcas quedaron corregidas. Lanzo la tercera pasada del tester, que tiene que dar el visto bueno.

Siguen corriendo la CI local y la tercera pasada del tester.

La guía tiene el visto bueno del tester: tercera pasada, 0 marcas. La CI local sigue corriendo.

La CI local salió en verde, con 97 % de cobertura de líneas (el umbral es 80 %). Cierro la implementación y calculo qué revisores corresponden.

Hay un hallazgo crítico que tenés que decidir vos, porque cambia la tabla de pruebas de la story.

- El chequeo mecánico exige que cada criterio tenga al menos una prueba asignada. El criterio 17 (no romper lo de la 2.2) no tiene ninguna; hoy lo cubren la suite completa y el filtro de la 2.2.
- En la 4.1 se resolvió asignándole una prueba que de verdad lo controla. En la 3.2 se aceptó como declarativo.
- Propongo asignárselo a T10, que ya mira cada fila de A4. Le sumo que los botones de la 2.2 siguen estando en su lugar: "Desactivar" en la fila activa, "Reactivar" en la desactivada y "Desbloquear" en la bloqueada. Así T10 cuida también que el botón nuevo no rompió los viejos.
- Si lo hago ahora, antes de lanzar a los revisores, se ahorra una segunda vuelta.

**¿Le asigno el criterio 17 a T10 con esos tres controles?** Es lo que recomiendo. La alternativa es aceptarlo como declarativo, como en la 3.2.

---

### 👤 Docente · 17/10 10:03

oka asigname esos 3 controles

### 🤖 Equipo

Ahora la story: la fila de T10 en la tabla y una entrada en el registro de cambios.

T10 pasa y el chequeo mecánico ya no encuentra nada. Calculo el set de revisores con las rutas escritas una por una.

Mientras revisan, levanto la app con Docker para la prueba en vivo (unos 3 minutos, en segundo plano).

El revisor de cobertura no encontró nada que bloquee. Lanzo el quinto revisor (criterios contra código).

Seguridad salió limpia. La revisión de la story trae dos avisos administrativos, que arreglo ahora. Lanzo el sexto revisor (fallos silenciosos).

La revisión de alcance salió limpia. Quedan en vuelo 2 revisores. Mientras tanto preparo la prueba en vivo; miro cómo se manejó el navegador en la 4.1.

Terminaron los 6 revisores. Ninguno encontró algo crítico, y hay tres avisos sobre las pruebas. Primero confirmo el conteo de revisores.

Los dos pasan. Anoto los cambios en la tabla de la story y armo el informe consolidado de la vuelta 1.

La vuelta 1 de revisores quedó cerrada. Como cambié pruebas después de los hallazgos, la regla exige otra vuelta sobre lo que cambió. Calculo a quiénes les toca.

Mientras corre la vuelta 2, preparo el guion de la prueba en vivo. Busco el Playwright que usaron las sesiones anteriores.

Paso a la prueba en vivo. Repaso cómo cerró este paso la 4.1, para dejar la evidencia en el mismo formato.

La 2.2 resolvió en vivo dos casos difíciles: le quitó un permiso a la base para que fallara la auditoría, y detuvo la base de datos. Busco ese guion para reusar el mismo método.

Escribo el guion de la 2.3 con las mismas ayudas. Las capturas tapan la contraseña temporal, porque el repo es público.

Corro el primer grupo (pantalla) a través del registrador de humos del arnés, contra la app levantada (alrededor de 1 minuto).

El archivo `.env` tiene un valor con espacio y el shell no lo pudo cargar. Leo las dos credenciales por separado, sin mostrarlas.

Grupo pantalla: 21 de 21 controles OK. Corro los otros tres grupos: bloqueo, errores y fallas. El último detiene y vuelve a levantar la base (unos 2 minutos).

Todos los grupos salieron en verde. Corro la matriz por criterio y miro las capturas.

La matriz da 17 de 17 criterios corridos en vivo. Miro las capturas para confirmar que la contraseña sale tapada.

Las capturas están bien y la contraseña sale tapada. Lanzo la pasada del tester no técnico sobre la pantalla y las capturas (la pasada de interfaz que pide la acción B-6).

Mientras el tester mira las capturas, vuelvo a pasar el portón por el proyecto Web, porque T10 y T14 cambiaron después de la corrida anterior (algo más de 1 minuto, en segundo plano).

Mientras esperamos, completo el registro del desarrollador en la story.

La pasada de pantalla también dio OK, sin marcas. La guardo y miro qué quedó sin tildar en la story.

Las dos casillas abiertas son los avisos que aceptamos sin arreglar, y quedan visibles a propósito. Espero el portón del proyecto Web para preparar el commit.

El portón Web sigue corriendo. Mientras tanto armo el mensaje del commit.

La story 2.3 quedó implementada y verificada. Falta tu aprobación para commit, push y PR.

| Cosa | Estado |
|---|---|
| Pruebas | 503 en verde (16 nuevas de la story, en 22 casos), sin romper nada. Cobertura del 97 % |
| Revisores | 2 vueltas (6 y 4), sin nada crítico. Arreglé 3 avisos y quedan 2 aceptados, anotados en el commit |
| Tester no técnico | Guía con visto bueno en la tercera pasada; pantalla sin marcas |
| Prueba en vivo en Chrome | 17 de 17 criterios, incluidos 10 pedidos a la vez y la base detenida |
| Tiempo hasta acá | 51 minutos, contra 1 h 03 proyectada |

Hay una cosa que cambia por la protección de `main` que configuramos hoy. La nota de "done" y el registro de la sesión ya no se pueden subir directo a `main`. Van como últimos commits de este mismo PR, antes del merge.

El mensaje del commit está en el scratchpad: `feat(personal): contraseña olvidada, otra temporal desde A4 (story 2.3)`. Suma 16 archivos: el código, las pruebas, las dos stories, la guía con las dos pasadas del tester y las 4 capturas.

**¿Apruebo commit, push y PR?**

---

### 👤 Docente · 17/10 10:50

si

### 🤖 Equipo

PR #13 abierto. El aviso de retrospectiva vuelve a confundir esta rama con `main`; corro el latido que pide y espero la CI (unos 4 minutos, en segundo plano).

Mientras corre la CI, preparo el registro de la sesión. Busco cómo se armó la transcripción completa en la sesión 13.

Tengo el exportador de la transcripción. La exportación final la hago al cerrar. Mientras tanto confirmo que la protección de `main` actúa sobre este PR.

La protección funciona: GitHub marca el PR #13 como **bloqueado** mientras la CI corre. Espero el resultado.

La CI salió en verde: 6 de 6 en 3 min 30 s, sin avisos. Pero el PR sigue marcado como bloqueado, y averiguo por qué.

### 🤖 Equipo (cuadro de opciones)

**¿Cómo cerramos la 2.3 con main protegida?**
- Merge ahora + PR de cierre (Recomendada): Mergeo el PR #13 ya. Después abro un PR chico con la nota de 'done', el registro completo de la sesión 14 (que incluye el merge y la actualización del plan de sprints) y las capturas. Ese PR también pasa la CI y te pido su merge. El registro queda completo.
- Todo dentro del PR #13: Sumo ahora al PR #13 la nota de 'done' y el registro de la sesión, espero la CI de nuevo (unos 4 minutos) y recién ahí mergeo. Un solo merge, pero el registro termina antes del merge y sin la actualización de la página del plan.

---

### 👤 Docente (respuesta en el cuadro de opciones) · 17/10 10:59

Your questions have been answered: "¿Cómo cerramos la 2.3 con main protegida?"="Merge ahora + PR de cierre (Recomendada)". You can now continue with these answers in mind.

### 🤖 Equipo

PR #13 mergeado: la 2.3 ya está en `main`. Sigo con el cierre en un PR aparte. Primero paso la story a `done`.

Las capturas de GitHub salieron bien: la corrida del PR #12 ya no muestra avisos. Ahora actualizo la página del plan de sprints. Primero la leo.

Para cargar los tiempos en la página, saco de la conversación a qué hora llegó cada respuesta tuya.

Ya tengo los tiempos. Escribo el resumen de la sesión 14 siguiendo el formato de la 13.

Resumen escrito. Ahora actualizo la página del plan de sprints sobre la versión que acabo de leer.

Es la misma versión que está publicada. Le aplico los cambios: la 2.3 pasa a hecha, se actualizan los conteos y la medición, y entran la CI y la protección de `main`.

Página publicada. La capturo desde el archivo local, como en la sesión 13.

La página quedó bien. Exporto la transcripción completa y reviso que no tenga contraseñas reales ni direcciones de ngrok.

Me pasé: al sacar esos bloques también se fueron las respuestas del equipo que venían después. Vuelvo a exportar y saco solo el encabezado y el texto de cada skill.
