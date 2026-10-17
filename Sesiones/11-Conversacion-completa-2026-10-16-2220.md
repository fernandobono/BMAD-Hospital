# Sesión 11 — Conversación completa

**Inicio:** 16 de octubre de 2026, 22:20 (hora de Argentina)
**Participantes:** el docente y el equipo de agentes BMAD (Amelia implementa; Mary valida la story;
los revisores y un tester no técnico revisan).

> Transcripción de la conversación: todos los mensajes del docente y todas las respuestas del
> equipo, en orden. No incluye las operaciones internas (lecturas de archivos, comandos,
> búsquedas) ni los avisos automáticos de las tareas en segundo plano. La versión resumida, con
> comentarios para la clase, es [11-Story-3-2-2026-10-16-2220.md](11-Story-3-2-2026-10-16-2220.md).

---

### 👤 Docente · 16/10 22:20

<pasted_content>
Sigamos con BMAD-Hospital (~/SourceCode/hospital-turnos). Esta es la sesión 11.

Estado: el sprint 1 está cerrado y del sprint 2 ya están hechas la 2.2 (gestión de usuarios, PR #7) y
la 3.1 (especialidades, PR #8), las dos mergeadas en main y en done. Los registros de las sesiones 09 y
10 están en main. Quedan del sprint 2, en este orden: 3.2 → 4.1 (las dos ready-for-dev).

Implementemos la story docs/stories/3-2-medicos.md con el circuito completo, igual que la 3.1:
validación contra el repo de hoy, plan para que yo lo apruebe, pruebas primero, revisores y prueba en
vivo. Antes de pedirme el merge, el PR tiene que tener la CI en verde. Medí cuánto tarda la story, para
comparar con la proyección del sprint 2 (21 min por prueba estimada en el plan).

Ojo al validar, dos cosas de la story que quedaron viejas:
- El cartel de error todavía usa `Response.StatusCode = EstadoPara(e)`. Hay que usar la pieza
  compartida `ManejadorGlobalDeErrores.FijarEstadoEnPagina(HttpContext, e)`, como la 2.2 y la 3.1.
- La ficha del médico fija el 404 a mano (`Response.StatusCode = 404`). Con Blazor .NET 10 una página
  SSR que responde 404 sale sin cuerpo: hay que resolverlo con el mismo mecanismo.
Corregilo en la story antes de implementar.

Reglas de esta sesión:
- Sin registro de horas.
- Una decisión por vez, con la opción recomendada primero.
- El tiempo se prueba siempre con el reloj inyectado, nunca esperando tiempo real.
- Commit, push y PR sólo cuando yo lo apruebe.
- Al cerrar: exportá la sesión en Sesiones/ con el resumen, la transcripción completa (sin contraseñas
  ni direcciones de ngrok: el repo es público) y las capturas de lo que abras en el navegador (A1: alta
  de médico, cambio de especialidad, desactivar y reactivar, los carteles de error y la corrida de la
  CI del PR), y actualizá la página del plan de sprints (https://claude.ai/artifact/2Cns8Kvpu48cWqpaBHPvWV).
</pasted_content>

---

### 🤖 Equipo

Arranco la sesión 11 con la story 3.2. Anoto la hora de inicio para medir la duración y abro el circuito completo del pipeline. Sin registro de horas, como pediste.

Rama `story/3-2-medicos` creada y pipeline arrancado. Ahora reviso cómo saltear los pasos de Clockify y ADO (ninguno aplica acá).

Los pasos de Clockify y ADO quedan salteados con constancia: pediste sin registro de horas y el repo no usa ADO. Paso a corregir la story y validarla.

Ahora leo las partes de la story que hay que corregir.

Aplico las correcciones a la story.

Story corregida en los 7 lugares. Ahora lanzo a Mary (el validador) contra el repo de hoy; tarda ~3-5 min.

Validador anunciado: corre sólo `tycon-story-validator` (el repo declara a propósito que no usa paquetes de revisores).

Mientras Mary valida, leo la story completa para preparar el plan.

La story es grande: 24 criterios y 31 pruebas (el plan estimaba 8, así que la proyección es 8 × 21 min = 2 h 48). Mientras Mary termina, miro las piezas de la 3.1 que voy a reusar.

Mary sigue validando la story. Cuando termine te traigo el resultado y, si está limpia, el plan para aprobar.

Mary terminó. El cambio del cartel de error y del 404 quedó bien aplicado: no queda ningún lugar con el patrón viejo.

- **No bloquea nada.** Ningún cambio del código de hoy contradice lo que la story manda hacer.
- **Hay 7 textos viejos.** Son de antes de la 2.2 y la 3.1:
  - el commit contra el que se verificó;
  - cuántas secciones "en construcción" quedan (4, no 6);
  - cuántos tipos de registro de auditoría hay (20, no 12);
  - las 2 migraciones nuevas;
  - las ayudas de prueba que ya existen;
  - dos citas con números de línea que se movieron;
  - el estado escrito en el cuerpo de la story, que no coincide con el de la cabecera.
- Son correcciones de texto, como en la sesión 10.

---

### 🤖 Equipo (cuadro de opciones)

**La validación encontró 7 textos viejos en la story (nada que bloquee). ¿Cómo seguimos?**
- Corregir y seguir (Recomendado): Corrijo los 7 textos, Mary vuelve a validar y paso al plan.
- Seguir sin corregir: Paso al plan con los textos viejos anotados como pendientes.
- Cancelar: Detengo el pipeline acá.

---

### 👤 Docente (respuesta en el cuadro de opciones) · 16/10 22:28

- Corregir y seguir (Recomendado)

---

### 🤖 Equipo

Los 7 textos quedaron corregidos. Mary hace la segunda validación (~1 min).

La segunda validación encontró un solo texto viejo más ("no existe la carpeta Catalogo"). Ya lo corregí y la validación quedó cerrada. Ahora armo el plan con Amelia.

Ya tengo todo para armar el plan. Lo escribo en la carpeta de la corrida y te paso el resumen.

---

### 🤖 Equipo (cuadro de opciones)

**¿Aprobás el plan de 4 capas (31 pruebas, pruebas primero) para la story 3.2?**
- Aprobar (Recomendado): Arranco con la línea de base y la capa de dominio.
- Cambiar algo: Decime qué ajustar antes de empezar.
- Cancelar: Detengo el pipeline acá.

---

### 👤 Docente (respuesta en el cuadro de opciones) · 16/10 22:33

- Aprobar (Recomendado)

---

### 🤖 Equipo

Plan aprobado. Corro la línea de base: la suite de `main` completa (~3 min, en segundo plano). Mientras tanto escribo las pruebas del dominio.

Escribo las pruebas del dominio (T1–T3).

Espero que termine la línea de base antes de compilar nada, para no mezclarla con archivos nuevos.

La línea de base está en verde: 300 pruebas (33 + 119 + 148). Ahora confirmo que las pruebas del dominio fallan por la razón correcta.

Rojas por la razón correcta: faltan `Medico` y `CambioDeDato`. Escribo el dominio.

El dominio quedó en verde (46 pruebas, antes 33). Paso a la capa de la base y las operaciones: primero leo lo que voy a reusar.

Escribo las ayudas de datos y las 16 pruebas de la capa (T4–T19).

Simplifico la prueba del buscador, que había quedado enredada.

Pruebas escritas. Ahora el código de esta capa: el puerto, la configuración de la tabla y el adaptador.

Compila. Antes de la migración corro las pruebas nuevas para confirmar que fallan por la razón correcta (la tabla todavía no existe).

Fallan por la razón correcta: el modelo tiene cambios sin migración. Genero `AgregarMedicos`.

Las 16 pruebas de la capa (32 casos) pasan en verde, incluidas las dos carreras. Levanto Compose para la salvaguarda de la base (~2 min, en segundo plano) y sigo con las pantallas.

Mientras tanto, leo las piezas de prueba de la Web que voy a reusar.

Escribo las ayudas de datos de la Web y las 12 pruebas de pantalla (T20–T31).

La salvaguarda de la base coincide con la story en todo: columnas, índices únicos, claves foráneas, permisos sin borrado y auditoría sin modificación. Sigo con las pruebas de pantalla.

Sigo con las pantallas: saco la ruta vieja, sumo las rutas nuevas y la columna de A4.

Para que el error de la matrícula aparezca debajo del campo (como pide el AC 7), el formulario reusa la regla del dominio. Expongo esa regla en `Medico`, así la expresión sigue en un solo lugar.

Escribo las dos páginas que faltan: el alta y la ficha del médico.

Escritas las páginas. Corro las pruebas de la Web de esta story, junto con las de la 2.1 y la 2.2 que toqué (~2 min).

Tres fallas. Dos devuelven un error genérico en vez de la página, así que miro el registro de la app para ver la causa.

La causa: la ficha se dibuja una primera vez antes de terminar de cargar, y el formulario todavía no tiene modelo. Lo dibujo sólo cuando el modelo está listo.

Queda una: la opción vacía de la lista no aparece como la busca la prueba. Miro cómo la dibuja Blazor.

Blazor escribe `<option value>` (sin `=""`), y el navegador lo toma como valor vacío. La página está bien: ajusto la prueba y quito el volcado.

Las 12 pruebas de pantalla (25 casos) pasan. Verifico que estén las 31 pruebas de la story, con sus nombres.

La suite completa está corriendo. Mientras tanto anoto en la story lo que agregué que la story no nombraba.

Espero el resultado de la suite completa para cerrar la implementación.

Paso 4 cerrado: sin dependencias nuevas y con las pruebas escritas antes del código. Ahora calculo qué revisores corresponden.

Los revisores salen 6. Hay un hallazgo mecánico: el criterio 23 no tiene prueba asignada. La story lo declara "declarativo" (es el contrato con otras stories). Miro cómo lo resolvió la 3.1.

Esto va al informe de hallazgos y lo decidimos juntos al final de la ronda de revisores. Antes miro si corresponde el pase adversarial.

El pase adversarial no corre: el repo lo tiene apagado en su configuración. Dejo la constancia y anuncio los revisores.

Mientras corren los revisores, lanzo la CI local con cobertura en segundo plano (~6-7 min).

Seguridad y cobertura salieron limpios. Lanzo los dos que faltan: criterios sin prueba y fallos silenciosos.

Corrieron los 6 revisores esperados. Registro la constancia, cierro el paso y armo el informe de hallazgos.

---

### 🤖 Equipo (cuadro de opciones)

**El criterio 23 (contrato con otras stories) no tiene prueba asignada en la tabla. ¿Cómo lo resolvemos?**
- Asignarle T18 y T22 (Recomendado): Sumo el 23 a la columna de T18 y T22 y a sus marcas en el código: ya fijan lo que la 4.1 va a usar. Como en la 3.1. Re-corro los revisores sobre el cambio.
- Aceptarlo como declarativo: Queda como está, sin prueba asignada, con la excepción anotada en el informe y en la story.

---

### 👤 Docente (respuesta en el cuadro de opciones) · 16/10 23:14

- Asignarle T18 y T22 (Recomendado)

---

### 🤖 Equipo

La CI local quedó en verde, con 95,6 % de líneas cubiertas (umbral 80 %; en la 3.1 era 98,7 %). Miro qué bajó antes de seguir.

La CI local ya pasó, así que tildo esa tarea. Actualizo el informe y paso a la vuelta de revisores sobre el cambio.

El control mecánico ya no encuentra nada. Ahora la vuelta 2 de revisores: el arnés decide cuáles re-corren según lo que cambió.

Seguridad y cobertura limpios en la vuelta 2. Mientras terminan los otros dos, rearmo Compose con la versión nueva para la prueba en vivo (~2 min).

Vuelta 2 limpia en los 4 revisores. Registro la constancia y paso a la prueba en vivo. Primero miro cómo quedó registrado el humo de la 3.1, para repetir el método.

Repito el método de la sesión 10: un script de Playwright que maneja Chrome contra la app de Compose, registrado con el registrador de humos. Antes miro qué pide la matriz por criterio.

Script listo. Corro el primer grupo (pantalla) a través del registrador de humos. Las credenciales van sólo por variable de entorno y no quedan en el registro.

Una sola falla, y es del script: el menú lateral también tiene un enlace a Especialidades, así que encontró dos. Miro la captura para confirmar que la pantalla está bien.

La pantalla está bien. Corrijo la verificación para que busque el enlace dentro de la página y no en el menú, y repito el grupo con datos marcados, sin pisar las capturas.

Pantalla: 0 fallas. Corro el grupo de errores (carteles 400, 409, 404 y 403, sin token, otros roles).

Errores: 0 fallas. El 404 de la ficha llega con su cartel dentro de la pantalla, que es justo lo que corregimos en la story. Corro carrera y base.

Los cuatro grupos dieron 0 fallas. Registro la matriz por criterio.

Matriz en verde: los 24 de 24 criterios corrieron en vivo. Falta la guía para un tester y su revisión de legibilidad. Miro cómo quedó la de la 3.1.

Escribo la guía para el tester: 24 secciones, una por criterio, en lenguaje llano.

Guía escrita. Lanzo la revisión de legibilidad: un "tester no técnico" lee la guía y las capturas de las pantallas (~3 min).

El control no exige la guía (no reconoce pantallas `.razor`), pero mantengo el circuito de la 2.2 y la 3.1.

Mientras tanto reviso dos capturas clave: el 404 con su cartel y el cambio de especialidad.

Las dos capturas muestran lo que pide la story. Mientras termina el tester, registro la constancia de la prueba en vivo y leo el paso de preparar el commit.

Espero la revisión del tester para cerrar la prueba en vivo y preparar el commit.

El tester marcó 10 cosas en la guía y 3 en las pantallas. Leo sus propuestas para corregir la guía, que es trabajo mío.

Aplico las 10 correcciones a la guía.

Guía corregida. Lanzo la segunda pasada del tester sobre la guía (~2 min). Las marcas de las pantallas las decidimos después.

---

### 🤖 Equipo (cuadro de opciones)

**Las 3 marcas del tester sobre las pantallas salen de pedidos armados a mano o repiten la marca de la 3.1. ¿Qué hacemos?**
- Dejarlo como está (Recomendado): Las anoto como aceptadas en la story y en la revisión de legibilidad, con el motivo. Cierro la prueba en vivo.
- Cambiar los textos: Por ejemplo "Sólo un administrador activo…". Cambio la story, el código, las pruebas y la prueba en vivo, y hago otra vuelta corta de revisores (~15 min).

---

### 👤 Docente (respuesta en el cuadro de opciones) · 16/10 23:25

- Dejarlo como está (Recomendado)

---

### 🤖 Equipo

Mientras el tester hace la tercera pasada, anoto en la story el resultado de la prueba en vivo y las marcas aceptadas.

Todas las tareas de la story quedaron tildadas. Espero la tercera pasada del tester para cerrar.

El paso 8 quedó cerrado con todos los controles en verde. Limpio el estado del pipeline, paso la story a `review` y guardo lo aprendido en la memoria del proyecto.

**La 3.2 está lista para commitear.** El administrador ya puede:

- dar de alta médicos;
- corregir sus datos, incluida la especialidad;
- buscarlos en el listado;
- vincularlos con su usuario, y ver ese vínculo en "Usuarios del personal".

| | Estado |
|---|---|
| Suite completa | Hecho: 370 en verde (antes 300); las 31 de la story con sus nombres; CI local en verde, 95,6 % de líneas |
| Revisores | Hecho: 2 vueltas (6 y 4); el criterio 23 quedó asignado a T18 y T22, como decidiste |
| Prueba en vivo | Hecho: 24 de 24 criterios en Chrome contra Compose, con 18 capturas; el 404 de la ficha llega con su cartel |
| Tester no técnico | Hecho: la guía quedó OK en la tercera pasada; las 3 marcas de pantalla quedaron aceptadas |
| Tiempo | 66 min hasta el commit preparado, contra 2 h 48 proyectadas (8 pruebas estimadas × 21 min) |

Una aclaración sobre las capturas que pediste: los médicos no se desactivan, porque la story no lo permite. Lo que capturé fue desactivar y reactivar la **especialidad** (y el usuario) de un médico, y que igual se puede corregir.

Mensaje de commit preparado:

```
feat(catalogo): médicos con alta, edición, búsqueda y vínculo con el usuario médico (story 3.2)

La administradora da de alta a cada médico con nombre, apellido, matrícula y especialidad,
corrige sus datos y lo vincula con su usuario, y ve quién tiene agenda (FR-13, FR-10).

- Medico (Dominio/Catalogo): invariantes en el constructor, matrícula normalizada
  (" mp  1234 " → "MP 1234"), CambiarDatos devuelve sólo lo que cambió. EstadoAgendaMedico
  y SituacionDeAgenda en Dominio/Agendas: el contrato con la 4.1.
- Tabla medicos con índices únicos de matrícula y de usuario vinculado, FK RESTRICT y sin
  DELETE para la app (migración AgregarMedicos).
- IGestionMedicos + GestionMedicosEnBase: permiso confirmado en la base con IActorVigente,
  alta en una transacción con su auditoría medico.*, edición que audita y revalida sólo lo
  que cambia, búsqueda sin comodines, carreras de los dos índices (23505) traducidas a 409.
- A1 en renderizado estático: listado con buscador y "Sin agenda", alta y ficha; carteles
  con FijarEstadoEnPagina de la 2.2, también el 404 de la ficha (llega con su cuerpo).
  A4 suma la columna "Médico vinculado" con "Sin vincular".
- 31 verificaciones (T1-T31), 370 pruebas (antes 300), cobertura de líneas 95,6 %;
  prueba en vivo en Chrome contra el Compose, matriz por AC 24/24.

Revisores: 2 vueltas (6 y 4). ACCOV-C5 sobre el AC 23 (declarativo) resuelto asignándole
T18 y T22 (decisión del docente). Legibilidad: 3 marcas de pantalla aceptadas por el docente.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

Archivos: los 30 de código y pruebas, la story y la guía del tester con sus dos revisiones (`docs/qa/3.2-*`). Las capturas van con la exportación de la sesión.

---

### 🤖 Equipo (cuadro de opciones)

**¿Hago el commit, el push de la rama y abro el PR para que corra la CI?**
- Sí, commit + push + PR (Recomendado): Commit con este mensaje, push de story/3-2-medicos y PR contra main. Después espero la CI y te pido el merge.
- Sólo el commit: Commiteo local y espero antes del push.
- Cambiar el mensaje: Decime qué ajustar.

---

### 👤 Docente (respuesta en el cuadro de opciones) · 16/10 23:27

- Sí, commit + push + PR (Recomendado)

---

### 🤖 Equipo

La carpeta de memoria compartida del repo está vacía y nunca se commiteó: este repo es público y las sesiones anteriores tampoco la incluyeron. La dejo afuera. Hago el commit, el push y el PR.

Commit hecho y rama subida. Las capturas quedaron afuera a propósito: van con la exportación de la sesión. Abro el PR con el mismo formato que el #8.

PR #9 abierto. Un aviso del arnés interpretó el push como si fuera a `main`, pero fue a la rama de la story. Miro cómo se resolvió en la sesión 10 antes de tocar nada.

Es el mismo aviso falso que en la sesión 10: se deja sin registrar a propósito, y se registran el merge y el `done`. La CI del PR está corriendo (~3-4 min); la sigo en segundo plano.

Mientras corre, preparo la captura de la corrida de la CI con el script que se usó en la sesión 10.

Mientras espero la CI, leo la página del plan de sprints para saber qué hay que actualizar después.

Tengo el plan leído; lo actualizo después del merge. Espero la CI del PR.

La CI sigue: compilar ya pasó y están corriendo las pruebas, el escaneo de dependencias y la imagen. Te aviso cuando termine.

El freno insiste con el aviso falso: el commit `79ed6fa` está en la rama de la story, no en `main`. Le pido al control que lo juzgue contra el repo.

Latido registrado: quedó en esta máquina, sin archivo nuevo en el repo. La CI del PR sigue corriendo y te aviso cuando termine.

La CI del PR #9 pasó todos sus controles en 3 min 47 s. Saco las capturas del PR y de la corrida.

---

### 🤖 Equipo (cuadro de opciones)

**PR #9 has the CI green. Should I merge it into main and move story 3.2 to done?**
- Yes, merge (Recommended): Merge the PR as with #8, update main, move 3.2 to done in its own commit and push it. Then I export the session and update the sprint plan.
- Not yet: I leave the PR open and wait for your go-ahead.

---

### 👤 Docente (respuesta en el cuadro de opciones) · 16/10 23:32

- Yes, merge (Recommended)

---

### 🤖 Equipo

Merge done. Now I move the story to `done`: its own commit on `main`, the same as with the 3.1.

Página del plan actualizada en local: la 3.2 pasa a "Hecha" con 1 h 07 medido. La publico en la misma dirección.

Publicada en la misma dirección. Saco su captura y paso a exportar la sesión. Primero miro cómo se armó la transcripción de la sesión 10.

Uso el mismo conversor, ajustado a la sesión 11. Tapa las direcciones de ngrok y no incluye las operaciones internas.

Hay un bloque falso: la nota de una imagen que leí aparece como mensaje del docente. La filtro en el conversor y vuelvo a exportar.

Ahora escribo el resumen de la sesión, con el formato de la sesión 10.

La memoria de la sesión 10 dice que hay que tapar también las rutas absolutas en los mensajes del equipo. Lo reviso en la transcripción.

No hay rutas absolutas ni datos personales en los archivos de la sesión 11. Actualizo la memoria del proyecto con esta sesión.

Sólo falta subir el registro de la sesión. Antes de eso, re-exporto la transcripción, para que llegue hasta esta pregunta.

---

### 🤖 Equipo (cuadro de opciones)

**¿Commiteo y subo a main el registro de la sesión 11 (resumen, transcripción completa y 21 capturas), como el de la sesión 10?**
- Sí, commit + push a main (Recomendado): Commit "Sesiones/11: implementación de la story 3.2" directo en main, como el de la sesión 10.
- Lo reviso antes: Dejo los archivos sin commitear en Sesiones/ para que los mires.

---

### 👤 Docente (respuesta en el cuadro de opciones) · 16/10 23:35

- Sí, commit + push a main (Recomendado)
