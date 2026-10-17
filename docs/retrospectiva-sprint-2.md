---
documento: Retrospectiva del sprint 2
proyecto: Gestión de turnos — Hospital Municipal Las Acacias (caso simulado)
fecha: 2026-10-17
participantes: el docente y el equipo BMAD — Bob (facilita), John (producto), Winston (arquitectura), Mary (análisis), Amelia (desarrollo), Quinn (QA)
alcance: stories 2.2, 3.1, 3.2 y 4.1 (todas en done y en main)
---

# Retrospectiva del sprint 2

**Objetivo del sprint:** "El hospital se puede configurar." **Cumplido.**
Las 4 stories están en `main` (PR #7 a #10), con la CI en verde y probadas en vivo.

## 1. Lo medido

El tiempo sale de los registros de las sesiones 09 a 12, que tienen la hora de cada paso. Se mide
con dos cortes:

- **Hasta el PR:** desde el pedido hasta que se abre el PR con la story terminada.
- **Sesión completa:** hasta el final de la sesión, con el merge y la exportación. **Es el corte
  que usó el sprint 1**, así que es el que sirve para comparar.

"Activo" descuenta las pausas de más de 15 minutos. Hubo dos: 1 h 15 en la 2.2, esperando la
decisión sobre la legibilidad de las pantallas, y 35 minutos en la 3.1, después de abrir el PR.

| Story | Criterios | Pruebas estimadas en el plan | Verificaciones en la story | Vueltas de revisión | Hasta el PR: reloj | Hasta el PR: activo | Sesión: reloj | Sesión: activo |
|---|---|---|---|---|---|---|---|---|
| 2.2 Gestión de usuarios | 27 | 8 | 31 | 3 | 2 h 28 | 1 h 14 | 2 h 58 | 1 h 29 |
| 3.1 Especialidades | 19 | 6 | 23 | 1 | 0 h 30 | 0 h 30 | 1 h 22 | 0 h 46 |
| 3.2 Médicos | 24 | 8 | 31 | 2 | 1 h 07 | 1 h 07 | 1 h 17 | 1 h 17 |
| 4.1 Dominio de agenda | 32 | 14 | 44 | 3 | 0 h 35 | 0 h 35 | 0 h 54 | 0 h 54 |
| **Total** | **102** | **36** | **129** | | **4 h 40** | **3 h 26** | **6 h 31** | **4 h 26** |

Los registros de cada sesión redondearon algún minuto distinto (la 2.2 figura con 2 h 29 y la 3.1
con 29 minutos). Esta tabla sale de las horas exactas.

Al cierre corren **481 casos de prueba** (eran 179 al terminar el sprint 1), con **97 %** de
cobertura de líneas.

**Velocidad del sprint 2, comparada con el sprint 1:**

| | Sprint 1 | Sprint 2 |
|---|---|---|
| Reloj del sprint (sesión completa) | 11 h 45 | **6 h 31** |
| Reloj por story (promedio) | 1 h 58 | **1 h 38** |
| Minutos por prueba estimada en el plan | 21 | **11** |
| Minutos por verificación escrita | 9,2 | **3,0** |

- El sprint 2 tardó **un 48 % menos** que las ~12 h 30 proyectadas.
- El cierre de cada sesión (merge, exportación, capturas, página del plan) pesó más que en el
  sprint 1: 1 h 51 de reloj y 1 h 00 activa en total, contra unos 35 minutos en el sprint 1.

## 2. Pruebas estimadas contra pruebas reales

| | Estimadas en el plan | Escritas | Veces |
|---|---|---|---|
| 2.2 | 8 | 31 | 3,9 |
| 3.1 | 6 | 23 | 3,8 |
| 3.2 | 8 | 31 | 3,9 |
| 4.1 | 14 | 44 | 3,1 |
| **Sprint 2** | **36** | **129** | **3,6** |
| Sprint 1 | 34 | 77 | 2,3 |

- La estimación se quedó corta **3,6 veces**, más que en el sprint 1.
- Pero se quedó corta **parejo**: entre 3,1 y 3,9 veces en las cuatro stories. Como medida del
  tamaño relativo de las stories sigue sirviendo (acción A-2 del sprint 1).
- **Ni las pruebas estimadas ni las escritas predijeron el tiempo.** La 4.1 era la más grande
  (14 estimadas, 44 escritas) y fue la segunda más rápida. Lo que pesó fue otra cosa (sección 3).
- La revisión de las stories del sprint 2 había previsto 19 a 20 h, multiplicando el tiempo por las
  129 pruebas escritas. Fue al revés: más pruebas y la mitad del tiempo.

## 3. Por qué entró en mucho menos de lo proyectado

1. **El reuso.** La 2.2 construyó las piezas que las demás usaron sin tocarlas: el actor de la
   sesión, el control de permiso en la base, el cartel de error con su estado
   (`FijarEstadoEnPagina`) y las ayudas de prueba. La 3.1 dejó el doble de prueba para las carreras
   y la 3.2 lo usó dos veces. La 2.2 tardó 2 h 28 hasta el PR; las tres siguientes, 2 h 12 entre las
   tres. *(Amelia)*
2. **El sprint 1 pagó la infraestructura.** La bandeja de emails, la CI y el Compose se llevaron
   tres cuartos del sprint 1, con esperas de la CI incluidas. La velocidad de 21 minutos venía de
   ese tipo de trabajo, que en el sprint 2 ya no hubo. *(Winston)*
3. **La 4.1 era dominio puro.** 32 de sus 44 pruebas no necesitan base ni pantalla: corren en menos
   de una décima de segundo, sin Docker. *(Quinn)*
4. **Stories escritas contra el código real (acción A-1).** Se escribieron un día antes, no una
   semana. La validación igual encontró textos viejos en las cuatro, pero ninguno obligó a
   rehacer el plan. *(Mary)*
5. **Decisiones rápidas.** Salvo una pausa de 1 h 15 en la 2.2, cada pregunta se contestó en
   minutos. En el sprint 1 hubo más esperas, varias de la CI. *(Bob)*
6. **Las cuatro sesiones fueron el mismo día, seguidas.** Cada una arrancó con todo fresco. No es
   seguro que se repita. *(John)*

## 4. Qué funcionó

- **La pieza compartida en vez de la copia.** Blazor .NET 10 no escribe el cuerpo de una página que
  responde 404. La 2.2 lo resolvió una vez, en el manejador de errores, y la 3.1 y la 3.2 lo usaron.
  *(Winston)*
- **Romper el código a propósito para probar la prueba.** En la 2.2 se sacó el candado de la base y
  la prueba de los dos administradores falló 5 de 5 veces. Con el candado pasa siempre. *(Quinn)*
- **Las carreras, resueltas por la base y probadas sin suerte.** Un índice único deja entrar a uno
  solo. El doble de prueba de la 3.1 inserta lo mismo por otra conexión justo antes de guardar, así
  la carrera ocurre siempre. En vivo: diez altas a la vez, una entra y nueve reciben la regla.
  *(Amelia)*
- **El contrato con la story siguiente, con pruebas.** La 3.1, la 3.2 y la 4.1 tienen un criterio
  que fija lo que la próxima va a usar, con pruebas asignadas. La 4.1 encontró el contrato de la 3.2
  tal como estaba escrito. *(Mary)*
- **El reloj inyectado, otra vez (acción A-4).** El corte de sesión a los 5 minutos (2.2) y la regla
  de los 7 días a las 23:30 del viernes en Córdoba, que en UTC ya es sábado (4.1), se probaron en
  segundos. *(Quinn)*
- **El tester no técnico.** En la 2.2 encontró cuatro problemas de legibilidad que se arreglaron, y
  en la guía de prueba encontró pasos fuera de orden en la 2.2, la 3.1 y la 3.2. *(Bob)*
- **Leer el SQL que genera la migración.** En la 4.1 la convención de nombres convertía la columna
  de sistema `xmin` en una columna nueva `version`. Sólo se vio leyendo el SQL. *(Winston)*

## 5. Qué no funcionó

- **Las stories escritas antes siguen quedando viejas.** Aunque se escribieron un día antes, cada
  story anterior cambió el repo: la validación corrigió siete lugares en la 3.1 y siete en la 3.2.
  Una decisión de la 2.2 (el cartel del 404) tuvo que volver a mano a las stories siguientes.
  *(Mary)*
- **El criterio de contrato nació sin prueba.** En la 3.2 y en la 4.1 la validación marcó que no
  tenía pruebas asignadas. Se asignaron, pero tendría que venir así desde que se escribe. *(Mary)*
- **La previsión de 19 a 20 h se equivocó de dirección.** Se calculó con las pruebas escritas, que
  no predicen el tiempo. *(John)*
- **`main` sigue sin proteger.** La acción A-3 quedó pendiente: hoy GitHub responde "Branch not
  protected". *(Winston)*
- **La CI tiene dos avisos que vencen pronto.** Las acciones usan Node.js 20 y `ubuntu-latest` pasa
  a Ubuntu 26 desde el 19/10, dentro de dos días. *(Winston)*
- **Los scripts de la prueba en vivo fallan por el script, no por la app.** En la 3.1 buscaban una
  clase CSS que Blazor reemplaza; en la 3.2, un enlace que el menú también tiene. Las dos veces la
  pantalla estaba bien. *(Amelia)*
- **El cierre de sesión creció.** Pasó de unos 35 minutos en todo el sprint 1 a 1 h 51 en el
  sprint 2. *(Bob)*

## 6. Las lecciones técnicas de las cuatro stories

| # | Lección | Story |
|---|---|---|
| L-1 | Blazor .NET 10 no escribe el cuerpo de una página que responde 404: el estado se fija con `ManejadorGlobalDeErrores.FijarEstadoEnPagina`, justo antes de enviar la respuesta. | 2.2 |
| L-2 | Una prueba de concurrencia se valida rompiendo el código: sin el candado tiene que fallar siempre. | 2.2 |
| L-3 | Las carreras las resuelve un índice único en la base. La app traduce el rechazo a la regla, nunca a un error interno. | 3.1, 3.2 |
| L-4 | Para probar una carrera sin depender de la suerte, un doble de prueba inserta lo mismo por otra conexión justo antes de guardar. | 3.1, 3.2 |
| L-5 | Los nombres que no se pueden repetir se comparan por una clave sin acentos, sin mayúsculas y sin espacios de más, con índice único sobre la clave. | 3.1 |
| L-6 | Si dos páginas atienden la misma ruta, Blazor da un error interno: al crear la pantalla real hay que quitar la "en construcción". | 3.1 |
| L-7 | Una prueba-trampa puede guardar un requisito que todavía no se puede cumplir: la T15 falla el día que aparezca la tabla de turnos. | 3.1 |
| L-8 | Blazor dibuja la página una vez antes de terminar de leer los datos: el formulario se dibuja recién cuando el modelo está listo. | 3.2 |
| L-9 | Al editar, se revalida sólo lo que cambió: un médico con la especialidad desactivada después igual se puede corregir. | 3.2 |
| L-10 | La protección contra cambios simultáneos usa la columna de sistema `xmin`. La convención de nombres la convertía en una columna nueva: hay que fijar el nombre y leer el SQL generado. | 4.1 |
| L-11 | La base guarda la hora en UTC. Una ayuda de prueba que arma horas con −03:00 rompe la prueba, no el código. | 4.1 |
| L-12 | Las reglas de calendario se prueban en el borde de la zona horaria: 23:30 del viernes en Córdoba ya es sábado en UTC. | 4.1 |
| L-13 | Un dominio sin pantalla se prueba en vivo con una sonda fuera del repo que usa el código compilado contra la base real. | 4.1 |
| L-14 | Los scripts que manejan el navegador buscan por texto o por rol, no por clases CSS que el framework cambia, y cuentan con que el menú repite enlaces. | 3.1, 3.2 |

## 7. Las acciones del sprint 1

| # | Acción | Estado |
|---|---|---|
| A-1 | Escribir las stories contra el código real y revalidarlas antes de programar. | **Hecha.** Igual quedaron textos viejos (sección 5). |
| A-2 | "Pruebas (est.)" como medida de tamaño; el tiempo, con la velocidad medida. | **Hecha.** Sigue siendo pareja como medida de tamaño. |
| A-3 | Decidir si se protege `main`. | **Pendiente.** |
| A-4 | Probar las reglas de tiempo con el reloj inyectado. | **Hecha** en la 2.2 y la 4.1. |
| A-5 | Medir el tiempo de cada story del sprint 2. | **Hecha** (esta retrospectiva). |

## 8. Qué cambiamos para el sprint 3

| # | Acción | Quién |
|---|---|---|
| B-1 | Cada criterio de contrato con la story siguiente nace con sus pruebas asignadas. | Mary |
| B-2 | Cuando una story cambia una pieza que otras stories ya escritas citan, se corrigen esas stories en la misma sesión. | Mary |
| B-3 | Resolver los avisos de la CI (Node.js 20 y Ubuntu 26) antes de la primera story del sprint 3. | Winston |
| B-4 | Decidir la acción A-3 (proteger `main`). | Docente |
| B-5 | Medir con los dos cortes, hasta el PR y la sesión completa. La proyección usa la sesión completa. | Bob |
| B-6 | Las stories con pantalla nueva cuentan en el plan la pasada del tester no técnico. | John |

## 9. Proyección de los sprints 3 a 7

**Decisión del docente (2026-10-17): opción A**, por tamaño con la velocidad de los dos sprints
(15,7 minutos de reloj por prueba estimada). La retrospectiva del sprint 1 había dicho que, si el
sprint 2 se desviaba más de un 20 %, se recalculaba con los dos sprints. Se desvió un 48 %.

Los sprints 3 a 7 suman **183 pruebas estimadas** (S3 38, S4 40, S5 45, S6 47, S7 13) en
**25 stories**. Las opciones:

| Opción | Cómo se calcula | S3 | S4 | S5 | S6 | S7 | **S3 a S7** |
|---|---|---|---|---|---|---|---|
| **A (elegida)** | Por tamaño, con los dos sprints: 1 096 min / 70 pruebas = 15,7 min por prueba estimada | ~9 h 55 | ~10 h 25 | ~11 h 45 | ~12 h 15 | ~3 h 25 | **~48 h** |
| B | Por tamaño, sólo con el sprint 2: 11 min por prueba estimada | ~6 h 55 | ~7 h 15 | ~8 h 10 | ~8 h 30 | ~2 h 20 | **~33 h** |
| C | Por story, con los dos sprints: 1 h 50 por story | ~9 h 10 | ~7 h 20 | ~11 h | ~12 h 45 | ~5 h 30 | **~46 h** |
| D | Sin cambios: 21 min por prueba estimada | ~13 h 20 | ~14 h | ~15 h 45 | ~16 h 25 | ~4 h 35 | **~64 h** |

- **A** es lo que se acordó en la retrospectiva del sprint 1. Mezcla un sprint de infraestructura y
  uno de reuso, y lo que viene tiene de los dos: pantallas nuevas (la propuesta del médico, la
  aprobación, la web del paciente) y piezas que ya existen.
- **B** supone que el reuso sigue igual. Es optimista: el S4 trae la reserva con carreras y el S5,
  el ingreso con código de un solo uso y los emails.
- **C** ignora el tamaño: la 5.3 (14 pruebas) pesaría lo mismo que la 6.6 (4).
- **D** ignora lo que midió el sprint 2. Sirve como techo.

**Proyección vigente (opción A):**

| Sprint | Objetivo | Pruebas (est.) | Tiempo |
|---|---|---|---|
| S1 | La base andando | 34 | **11 h 45 (medido)** |
| S2 | El hospital se puede configurar | 36 | **6 h 31 (medido)** |
| S3 | Los médicos tienen agenda aprobada | 38 | ~9 h 55 |
| S4 | La recepción da y cancela turnos | 40 | ~10 h 25 |
| S5 | El paciente saca turnos por la web | 45 | ~11 h 45 |
| S6 | Licencias, agendas del día y auditoría | 47 | ~12 h 15 |
| S7 | Listo para producción | 13 | ~3 h 25 |
| **S3 a S7** | | **183** | **~48 h** |

El tiempo es de sesión completa, igual que lo medido. Se vuelve a recalcular al cerrar el sprint 3,
con los tres sprints, si se desvía más de un 20 %.

**Actualización del mismo día (revisión de las stories del S3):** se sumaron dos stories chicas al
S4: la 2.4 (email del personal en A4, 3 pruebas estimadas) y la 2.5 (carrera en las acciones de A4,
3). El S4 pasa a 46 pruebas y ~12 h 00. Los sprints 3 a 7 quedan en 189 pruebas y ~49 h 20.
