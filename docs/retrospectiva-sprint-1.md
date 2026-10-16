---
documento: Retrospectiva del sprint 1
proyecto: Gestión de turnos — Hospital Municipal Las Acacias (caso simulado)
fecha: 2026-10-16
participantes: el docente y el equipo BMAD — Bob (facilita), John (producto), Winston (arquitectura), Mary (análisis), Amelia (desarrollo), Quinn (QA)
alcance: stories 1.1, 1.2, 1.3, 1.4, 1.5 y 2.1 (todas en done y en main)
---

# Retrospectiva del sprint 1

**Objetivo del sprint:** "La base andando: se levanta todo, se entra como personal." **Cumplido.**
Las 6 stories están en `main`, con la CI en verde y probadas en vivo.

## 1. Lo medido

El plan no fijó una duración para los sprints (supuesto S-35): había que medirla en este. El tiempo
sale de las horas de las conversaciones de cada sesión. "Reloj" es de punta a punta, sin la noche
entre el 15 y el 16. "Activo" descuenta las pausas de más de 15 minutos: puede quedarse corto,
porque algunas pausas fueron esperas de la CI.

| Story | Criterios | Pruebas estimadas en el plan | Verificaciones en la story | Vueltas de revisión | Reloj | Activo |
|---|---|---|---|---|---|---|
| 1.1 Estructura en 4 capas | 27 | 3 | 12 (+2 de Compose) | 4 | 1 h 14 | 1 h 14 |
| 1.2 Reloj y errores | 20 | 6 | 9 | — | 0 h 44 | 0 h 44 |
| 1.3 Auditoría | 17 | 7 | 8 | — | 1 h 04 | 0 h 49 |
| 1.4 Bandeja de emails | 26 | 8 | 13 | 5 | 3 h 18 | 1 h 54 |
| 1.5 Integración continua | 18 | 1 | 5 | — | 2 h 36 | 1 h 04 |
| 2.1 Ingreso del personal | 37 | 9 | 30 | 4 | 2 h 49 | 1 h 41 |
| **Total** | **145** | **34** | **77** | | **11 h 45** | **7 h 26** |

Al cierre corren **179 casos de prueba** (las verificaciones con varios casos cuentan cada uno), con
**97,8 %** de cobertura de líneas.

**Velocidad del sprint 1:**

- **1 h 58 de reloj por story**, en promedio (mediana: 1 h 56).
- **21 minutos de reloj por prueba estimada en el plan** (705 min / 34).
- Las pruebas del plan se quedaron cortas **2,3 veces** (34 estimadas, 77 escritas). La que más se
  desvió fue la 2.1: 9 estimadas, 30 escritas.

## 2. Qué funcionó

- **Validar la story contra el repo de hoy antes de programar.** En las 6 stories la validación
  encontró que la story describía algo que ya no existía. Corregirla costó minutos; descubrirlo en
  medio del código habría costado una vuelta entera. *(Mary)*
- **Pruebas primero, viéndolas fallar.** Varias veces el "rojo" mostró algo que la story no preveía,
  como el error 500 al salir sin el código de seguridad en la 2.1. *(Amelia)*
- **Las pruebas de stories anteriores como red.** La prueba de secretos de la 1.1 frenó un cambio de
  la 2.1 antes de escribir una línea. *(Quinn)*
- **El reloj inyectado.** El vencimiento de 30 minutos de la 2.1 se probó en segundos, sin esperar.
  La decisión de la 1.2 pagó su costo. *(Winston)*
- **La revisión en vueltas hasta que sale limpia.** La T30 nació para cubrir una rama y encontró un
  mensaje de error vacío. *(Quinn)*
- **Probar en vivo con Docker.** Encontró un defecto de la 1.1 que las pruebas automáticas no veían,
  porque corrían la app dentro del proceso de pruebas. *(Amelia)*

## 3. Qué no funcionó

- **Las stories se escribieron una semana antes que el código.** Cada una describía el repo del 9 de
  octubre. La validación lo atajó siempre, pero es trabajo repetido. *(Mary)*
- **La estimación de pruebas.** Se quedó corta 2,3 veces. No sirve como número de pruebas, aunque
  sí sirve para comparar el tamaño de las stories entre sí. *(John)*
- **Las stories con pantallas y prueba en vivo tardan más.** La 1.4, la 1.5 y la 2.1 se llevaron tres
  cuartos del tiempo. Desde el sprint 4 casi todas son así. *(Bob)*
- **El semáforo de la CI avisa, pero no frena.** `main` no está protegido: GitHub deja mergear un PR
  con la CI en rojo. Quedó como decisión pendiente desde la 1.5. *(Winston)*
- **La cobertura todavía mide poco.** El 97,8 % se calcula sobre pocas líneas de reglas de negocio.
  Empieza a significar algo con la agenda (4.1). *(Quinn)*

## 4. Qué cambiamos para el sprint 2

| # | Acción | Quién |
|---|---|---|
| A-1 | Las stories del sprint 2 se escriben **ahora**, contra el código real, y se validan otra vez justo antes de programar cada una. | Mary |
| A-2 | La columna "Pruebas (est.)" del plan se mantiene como **medida de tamaño**. El tiempo se proyecta con la velocidad medida. | John |
| A-3 | Decidir si se protege `main` para que no se pueda mergear con la CI en rojo. | Docente |
| A-4 | Cada story con reglas de tiempo las prueba con el reloj inyectado. Ninguna prueba espera tiempo real. | Amelia |
| A-5 | Medir el tiempo de cada story del sprint 2 igual que en el sprint 1, para corregir la proyección. | Bob |

## 5. Proyección de los sprints 2 a 7

**Decisión del docente (2026-10-16):** proyectar **por tamaño**, a 21 minutos de reloj por prueba
estimada en el plan. Así las stories grandes pesan más que las chicas. Contando por story (~2 h cada
una), el total habría dado ~55 h y habría subestimado stories como la 5.3.

| Sprint | Objetivo | Pruebas (est.) | Tiempo proyectado |
|---|---|---|---|
| S1 | La base andando | 34 | **11 h 45 (medido)** |
| S2 | El hospital se puede configurar | 36 | ~12 h 30 |
| S3 | Los médicos tienen agenda aprobada | 34 | ~11 h 45 |
| S4 | La recepción da y cancela turnos | 40 | ~13 h 50 |
| S5 | El paciente saca turnos por la web | 45 | ~15 h 30 |
| S6 | Licencias, agendas del día y auditoría | 47 | ~16 h 15 |
| S7 | Listo para producción | 13 | ~4 h 30 |
| **S2 a S7** | | **215** | **~74 h** |

**Cuidado con este número.** Sale de un solo sprint, y ese sprint casi no tuvo reglas de negocio.
Desde el S2 llegan la agenda (4.1) y después las pantallas. Si el S2 se desvía más de un 20 %, se
recalcula todo con la velocidad de los dos sprints (acción A-5).

**Actualización del mismo día (revisión de las stories del S2):** se sumó la story 2.3 (contraseña
olvidada, 4 pruebas estimadas) al S3, que pasa a ~13 h 10. El total de S2 a S7 queda en ~76 h. Las
stories del S2 ya escritas suman 129 pruebas (3,6 veces lo estimado): es probable que el S2 tarde
19 a 20 h y obligue a recalcular antes de lo previsto.
