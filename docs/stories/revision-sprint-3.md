---
documento: Revisión adversarial de las stories del sprint 3
proyecto: Gestión de turnos — Hospital Municipal Las Acacias (caso simulado)
fecha: 2026-10-17
alcance: 2.3, 4.2, 4.3, 4.4, 5.1 (antes de implementar; escritas en paralelo, un agente por story)
contexto: brief común del sprint 3, 00-contrato-tecnico.md, 03-ux.md, 05-epicas-y-plan.md, revision-sprint-2.md, retrospectiva-sprint-2.md (B-1, B-2, B-6), 4-1-dominio-agenda.md, código de main en a8432c3
---

# Revisión adversarial — Sprint 3

## Resumen

| Severidad | Cantidad | Stories más afectadas |
|---|---|---|
| CRÍTICO | 1 | 4.4 ↔ 4.2 (cómo se usa el editor de agenda dentro de otra página) |
| IMPORTANTE | 7 | 4.3 ↔ 4.2 (I-1, I-4, I-5), 4.4 ↔ 4.3 (I-2), 4.2 ↔ 5.1 (I-3), 4.2 + 4.4 (I-6), 4.3 + 4.4 (I-7) |
| MENOR | 11 | todas |

**Veredicto:** la 2.3 y la 5.1 se pueden implementar después de corregir el método de permiso
compartido (I-3). La 4.2 queda bien como dueña de los contratos, con dos agregados chicos (I-1 e
I-3). La 4.3 necesita igualar sus supuestos con la 4.2 (I-1) y arreglar dos pruebas que chocan con el
índice nuevo de la 4.2 (I-4). La 4.4 **no** arranca tal como está: arma su formulario de una forma que
el editor de la 4.2 no puede llenar, y su Task 1 sólo la detiene si el editor fuera interactivo, cosa
que no es (C-1). Todo se corrige con cambios de texto, sin rediseño.

### Puntos pedidos que se verificaron

| Punto | Resultado | Evidencia |
|---|---|---|
| Nombres que la 4.4 marcó `[ASUMIDO]` contra la 4.2 | **A-2** (`ResumenDeAgenda`, parámetro `Agenda`) y **A-3** (`DatosAgenda`, `DatosFranja`) coinciden. **A-1 no coincide**: la 4.4 asume parámetros `Agenda` y `FechaMinima` y un modelo que *contiene* un `FormularioAgenda`; la 4.2 fija `Input`, `MinimoVigenteDesde` y `TurnosPorSemanaAntes`, y un modelo que *es* (o hereda de) `FormularioAgenda`. Ver C-1. | 4.4 tabla de supuestos; 4.2 AC 24. |
| Nombres que la 4.4 asumió de la 4.3 | **A-5** (texto "Hay una agenda aprobada que empieza el dd/MM y va a reemplazar a esta.") y **A-6** (`EntidadTipo = "AgendaMedico"`) coinciden. **A-4 no coincide**: la 4.4 asume `BuscarAsync(long medicoId, AgendaMedico agendaNueva, ct)`; la 4.3 fija `BuscarAsync(AgendaMedico agendaNueva, DateOnly? hasta, ct)`. Ver I-2. | 4.4 tabla de supuestos; 4.3 AC 20. |
| Nombres que la 4.3 supuso de la 4.2 (S-1 a S-6) | **S-1 no coincide** (`ResumenAgenda` contra `ResumenDeAgenda`); **S-2 choca** con la T14 de la 4.2; **S-4 no coincide** (carpeta `Personal/Medico/` contra `Personal/Agendas/`, y el formato de la línea del rechazo); S-3 y S-5 coinciden; S-6 aplica (la 4.2 sí crea una ayuda de Web.Tests). Ver I-1 e I-5. | 4.3 AC 25; 4.2 AC 24, T14, frontmatter. |
| `ITurnosFueraDeAgenda` y `ProximaAprobadaDespuesDe` de la 4.3 en la 4.4 | La 4.4 no usa `ProximaAprobadaDespuesDe` ni el componente `AvisoTurnosFueraDeAgenda` que la 4.3 le deja (AC 26), y calcula "la aprobada que va a reemplazar" contra hoy y no contra la fecha de la corrección. Ver I-2. | 4.3 AC 19, 22, 26; 4.4 AC 8, 12, 19, 23. |
| Pantalla sin interactividad (4.2) y pruebas web de la 4.4 | **Incoherentes.** La 4.2 recomienda SSR (D-2): el cálculo y las marcas rojas se actualizan con botones del propio formulario (`Input.Accion` = `agregar-franja`, `quitar-franja:<i>`, `actualizar`). La 4.4 habla de "el editor y su cálculo en vivo", usa `OnValidSubmit` y no trata esos botones: con el motivo vacío, `+ Agregar franja` no haría nada. Sus pruebas T20–T27 no tocan ningún botón de edición. Ver C-1. | 4.2 AC 7, 8, 24, D-2; 4.4 "Por qué no hay E2E", Implementación concreta. |
| Piezas creadas dos veces | **Sí, el permiso por rol.** La 4.2 agrega `IActorVigente.ExigirMedicoActivoAsync` y, por dentro, un método **privado** `ExigirActivoConRolAsync(actor, rol, mensaje, ct)`; la 5.1 agrega un método **público** con ese mismo nombre y firma a la interfaz, y cada una crea su archivo de pruebas. También las ayudas de prueba de agendas (4.2 y 4.3) y los dobles de turnos fuera de agenda (4.3 y 4.4). Ver I-3, I-5 e I-2. | 4.2 AC 21 y "Implementación concreta"; 5.1 AC 18; frontmatters. |
| Métodos de `AgendaMedico` agregados por dos stories | **Sin choque de nombres.** La 4.2 no toca el dominio (lo prohíbe). La 4.3 agrega `FechaDeInicioYaPaso` y `ProximaAprobadaDespuesDe` y hace que `Aprobar` use el primero; la 4.4 agrega `CorreccionDeUrgencia` y `EsCorreccion`. Mismo archivo, en orden 4.3 → 4.4: no hay conflicto si se respeta el orden. La 4.4 además pasa `Franja.NombreDelDia` a público (ver I-6). | `AgendaMedico.cs` en `main`: ninguno de los cuatro existe; `Hoy` y `Fecha` son privados y los dos los usan desde la propia clase. |
| Migraciones y orden 2.3 → 4.2 → 4.3 → 4.4 → 5.1 | **Bien encadenadas.** Sólo dos: la 4.2 (`UnaPropuestaEditablePorMedico`, sólo crea un índice) y la 5.1 (`AgregarPacientes`, tabla y permisos sin `DELETE`). 2.3, 4.3 y 4.4 no tienen. Las dos van después de `20261017025538_AgregarAgendas` y la 5.1 ya avisa que la 4.2 puede haber sumado otra. Dos opciones de decisiones abiertas agregarían migraciones (4.3 D-1 opción 3, 4.4 D-5 opción 2). | Carpeta `Migraciones` de `main`. |
| Contratos con pruebas asignadas (B-1) | **Cumplido.** Todo AC de contrato tiene pruebas: 2.3 AC 16, 4.2 AC 24, 4.3 AC 25–28, 4.4 AC 27, 5.1 AC 21. Sólo quedan sin prueba propia los AC de no-regresión (2.3 AC 17, 4.3 AC 29, 4.4 AC 28), que los cubre la suite. Hueco: ninguna prueba de la 4.2 fija que el editor funcione con un modelo heredado, que es lo que la 4.4 necesita (C-1). | Tablas `### Tests` cruzadas con la columna AC. |
| Nombres citados como "existentes" contra `main` | **Todos existen.** `AgendaMedico` (constructor, `CambiarDuracion`, `CambiarVigenteDesde`, `AgregarFranja`, `QuitarFranja(dia, desde)`, `Enviar`, `Aprobar`, `Rechazar`, `AtiendeEn`, `TurnosPorSemana`, `QueRigeEn`, `LargoMaximoMotivo`), `Franja` (`NombreDelDia`, `Tramo`, `Inicio`, `Fin` internos), `EstadoDeAgenda.Calcular(agendas, hoy)`, `IConsultaAgendas` (dos métodos), `AgendaMedicoConfiguracion.IndiceUnaEnviada` y `xmin`, `IActorVigente` (un solo método, sin dobles), `GestionMedicosEnBase` (cinco parámetros, `Vinculables`, `NoExiste`), `RequireUniqueEmail = false`, `MensajeEmail`, `IBandejaDeSalida`, las 22 constantes de `AccionesAuditoria`, `RutasPersonal` (`MiPropuesta`, `Propuestas`, `Turnos`, `FichaMedico(long)`), las cuatro `Secciones*.razor` con sus `@page`, el comentario "los agrega la 4.4" en `FichaMedico.razor`, `CamposMedico` con parámetro `Input`, `AddInteractiveServerComponents`, `TimeProviderExtensions.AhoraLocal`, `RutasYaConstruidas`, `DatosDeAgenda` (con `Franja` que sólo conoce lunes a miércoles, como dice la 4.3), `InterceptorQueSeAdelanta(cadena, sql)` con `SeAdelanto`, `UsuariosDePrueba`, `DatosDeCatalogo.CrearMedicoAsync(…, usuarioPersonalId)` en los dos proyectos, `FormularioHttp.EnviarAsync(cliente, ruta, formulario, campos, conToken, accept)`, `PersonalWebApplicationFactory` (`Reloj`, `Registro`, `admin1` = Lucía Pérez), `AgendaBuilder`, `RelojDePrueba.En`, `ContratoTurnosFuturosTests`, `EnviadorEmailFalso`, `AuditoriaQueFalla`, `GestionUsuariosConBaseCaida` (único doble de `IGestionUsuariosPersonal`), `CambioDeDato` en `Dominio/Catalogo/`, `HoraDelSistemaTests`, y las pruebas de la 4.1 citadas (T16, T17, T28, T33, T38, T42, T44). Única cita imprecisa: M-9. | Lectura directa de `src/` y `tests/`. |
| Conteos | 2.3: 17 AC / 16 pruebas; 4.2: 25 / 45; 4.3: 29 / 34; 4.4: 28 / 27; 5.1: 22 / 33. **155 pruebas** contra 38 estimadas en el plan (4,1 veces; el S2 fue 3,6). Filas numeradas sin huecos en las cinco. | Tablas `### Tests`; plan, fila S3. |
| Tiempo en las pruebas | **Bien**: todo con `FakeTimeProvider`; nadie espera tiempo real. Bordes de medianoche probados en la 4.3 (T1, 23:30 del 01/11) y la 4.4 (T4, 23:30 del 16/10). | — |
| Seguridad | La 2.3 trata la contraseña temporal igual que la 2.2 (sólo en la respuesta, `no-store`, nunca auditada). Toda escritura de las cinco confirma el permiso en la base y se audita en la misma transacción. La 5.1 confirma el permiso también en las lecturas (datos personales). La 4.3 codifica el motivo en el HTML del email y deja la dirección fuera de la auditoría. | — |

---

## CRÍTICOS

### C-1 · 4.4 ↔ 4.2 — La 4.4 usa el editor de agenda de una forma que el editor no admite

- **Dónde:** 4.4 tabla de supuestos (`[ASUMIDO A-1]`), AC 18, Given/Fixture "Web", Task 1,
  Implementación concreta (`CorregirAgenda.razor` y `FormularioCorreccion`), "Por qué no hay E2E";
  4.2 AC 7, 8, 24 (viñetas `EditorDeAgenda` y `FormularioAgenda`), T9, T14.
- **Problema:** las dos stories describen el mismo componente de forma distinta, y no es sólo el nombre:

  | Punto | Dice la 4.2 (dueña) | Dice la 4.4 |
  |---|---|---|
  | Parámetros del editor | `Input` (`FormularioAgenda`), `MinimoVigenteDesde` (`DateOnly?`), `TurnosPorSemanaAntes` (`int?`), "y sólo éstos" | `Agenda` y `FechaMinima` (`DateOnly`) |
  | Modelo de la página | La propiedad se llama `Input` y **es** un `FormularioAgenda`; "no `sealed`: la 4.4 puede heredarlo para sumar el motivo" | `FormularioCorreccion` **contiene** un `FormularioAgenda Agenda` |
  | Nombres de los campos | `Input.DuracionTurnoMinutos`, `Input.Franjas[i].…`, `Input.Accion` (fijos en el editor; T9 los verifica) | `Input.Agenda.…` (implícito) |
  | Botones de edición | Envían el formulario con `Input.Accion`; la página primero llama `AplicarAccionDeEdicion()` y vuelve a dibujar sin guardar | No existen para la 4.4: `OnValidSubmit` + un solo botón `Corregir agenda` sin valor |
  | Cálculo de turnos | Se actualiza con cada botón (SSR) | "El editor y su cálculo en vivo son de la 4.2" |

- **Por qué importa (bloqueo seguro):** los campos que dibuja el editor llevan el nombre `Input.…`;
  con el modelo de la 4.4 (`Input.Agenda.…`) los valores no llegan a la agenda corregida. Con
  `OnValidSubmit`, apretar `+ Agregar franja` o `Quitar` con el motivo todavía vacío sólo muestra
  "Escribí el motivo de la corrección." y la fila no se agrega: la pantalla no se puede usar en el
  orden natural (primero la grilla, después el motivo). Y la Task 1 de la 4.4 sólo manda a detenerse
  "si el editor es interactivo"; como la 4.2 es SSR, el agente reemplaza los nombres y sigue. Es el
  mismo error del C-1 del sprint 2: un contrato escrito dos veces que no coincide.
- **Corrección:** fijar el uso una sola vez, en la 4.2, y que la 4.4 lo copie:
  1. En la 4.4, `FormularioCorreccion : FormularioAgenda` (hereda) con `Motivo` y
     `ConfirmaTurnosFuera`; la propiedad de la página se llama `Input`;
     `<EditorDeAgenda Input="Input" MinimoVigenteDesde="datos.Hoy" TurnosPorSemanaAntes="@vigente?.TurnosPorSemana()" />`.
  2. `OnSubmit` (no `OnValidSubmit`), con el mismo orden que M2: `AplicarAccionDeEdicion()` →
     acción desconocida → validar → corregir. Botones de la página con `Input.Accion` = `corregir` y
     `corregir-igual` (el "Corregir igual" del AC 23).
  3. Sacar de la 4.4 "cálculo en vivo"; sumar a T23 o a una prueba nueva "`+ Agregar franja` con el
     motivo vacío agrega la fila y no muestra el error del motivo" (se recalculan los conteos).
  4. En la 4.2 (B-1): una prueba bUnit más en `ComponentesDeAgendaTests` que dibuja `EditorDeAgenda`
     con un modelo que **hereda** de `FormularioAgenda` y verifica los mismos nombres de campo. Así el
     contrato que la 4.4 usa queda probado desde la 4.2.
  5. Quitar la marca `[ASUMIDO A-1]` con los nombres reales, y la condición de la Task 1 de la 4.4
     pasa a ser "si el editor no tiene el contrato del AC 24 de la 4.2, DETENERSE".

---

## IMPORTANTES

### I-1 · 4.3 ↔ 4.2 — El resumen de agenda y la línea del rechazo en M2 no coinciden

- **Dónde:** 4.3 AC 4, 17, 25 (S-1, S-2, S-4), frontmatter (`files_to_modify` con
  "<la página M2 de la 4.2> (supuesto S-4…)"), Task 5, T23, T34; 4.2 AC 5, 24 (viñetas `ResumenDeAgenda`
  y "4.3"), T14, T41, registro `VistaPropuesta`.
- **Problema:**

  | Punto | 4.2 (dueña) | 4.3 (supuesto) |
  |---|---|---|
  | Nombre del resumen | `ResumenDeAgenda` | `ResumenAgenda` |
  | Marca `← cambia` | Expone `ResumenDeAgenda.TextoDeFranja(Franja)` para que la 4.3 compare; T14 exige que los parámetros sean **exactamente** `Agenda` y `Forma` | Agrega un parámetro `CompararCon` al componente "sin cambiar lo que las pruebas de la 4.2 verifican" (imposible: T14 lo verifica) |
  | Carpeta de M2 | `Components/Personal/Agendas/MiPropuesta.razor` (descarta `Medico/` a propósito, por el choque con el tipo `Medico`) | `Components/Personal/Medico/MiPropuesta.razor` |
  | Línea del rechazo | `Rechazada el dd/MM/yyyy a las HH:mm.`; propone que la 4.3 la cambie a `… por <Quien>.` y actualice T41 | Agrega `— Lucía Pérez, 12/11 10:05` (formato `dd/MM HH:mm`) después de la línea |
  | De dónde sale "quién" | Un dato `RechazadaPor` al final de `VistaPropuesta`, cargado por `PropuestaAgendaEnBase` | `IAprobacionPropuestas.QuienRechazoAsync` (el puerto del administrador) inyectado en la página del médico |

- **Por qué importa:** la 4.3 ya prevé "si difieren, vale la 4.2", pero en dos puntos no alcanza con
  renombrar: agregar `CompararCon` rompe la T14 de la 4.2, y la línea del rechazo queda con dos
  formatos de fecha en el mismo renglón. Además la 4.3 no lista `MiPropuestaHttpTests.cs` (T41), ni
  `IPropuestaAgenda.cs` / `PropuestaAgendaEnBase.cs` si se usa `RechazadaPor`; y su `files_to_modify`
  tiene una línea que no es una ruta (un validador de stories la marca).
- **Corrección:** (a) en la 4.2, sumar ahora el parámetro opcional `[Parameter] AgendaMedico? CompararCon`
  a `ResumenDeAgenda` (marca `← cambia` en las franjas que no están igual en la otra), con su caso en
  T13 y en T14: cuesta una prueba y evita que la 4.3 toque un componente cerrado; (b) en la 4.3,
  S-1/S-2/S-4 con los nombres reales y la ruta real; (c) "quién rechazó" por `VistaPropuesta.RechazadaPor`
  (parámetro con valor por defecto `null`, para no romper los `new VistaPropuesta(...)` de la 4.2), y
  quitar `QuienRechazoAsync` de `IAprobacionPropuestas` o dejarlo sólo como la consulta que usa
  `PropuestaAgendaEnBase`; (d) una sola línea, según la decisión 7: recomendado
  `Rechazada el dd/MM/yyyy a las HH:mm por Lucía Pérez.`; T41 de la 4.2 y T34 de la 4.3 con ese
  texto; (e) `files_to_modify` de la 4.3 con las rutas reales.

### I-2 · 4.4 ↔ 4.3 — El punto de extensión de FR-15 se usa con otra firma y sin las piezas que deja la 4.3

- **Dónde:** 4.4 `[ASUMIDO A-4]`, AC 8, 12, 19, 23, T15, T16, T27, `TurnosFueraDeAgendaDePrueba`,
  `CorreccionWebApplicationFactory`, `DatosParaCorregir.AprobadaFuturaDesde`; 4.3 AC 19, 20, 22, 26, T18,
  T31, T33, `TurnosFueraDeAgendaEspia`, `TurnosFueraDeAgendaFijos`.
- **Problema:**
  1. Firma distinta (`medicoId, agendaNueva` contra `agendaNueva, hasta`), y la 4.4 no calcula `hasta`
     con `ProximaAprobadaDespuesDe`, que es lo que la 4.3 le pide en su AC 26.
  2. La 4.4 escribe a mano el texto `⚠ N turnos ya dados…` en lugar de reusar `AvisoTurnosFueraDeAgenda`
     (4.3 AC 22, probado por T33 "lo que la 4.4 recibe").
  3. Dobles repetidos: `TurnosFueraDeAgendaDePrueba` (4.4) hace lo mismo que `TurnosFueraDeAgendaEspia`
     (4.3), en el mismo proyecto; y `CorreccionWebApplicationFactory` hace lo que la 4.3 resuelve con
     `WithWebHostBuilder` + `TurnosFueraDeAgendaFijos`.
  4. El aviso "va a reemplazar a esta" (AC 19) usa `AprobadaFuturaDesde` = la próxima aprobada **desde
     hoy**. Si la administradora elige un `Rige desde` posterior a esa aprobada, el aviso es falso. La
     regla correcta es `ProximaAprobadaDespuesDe(aprobadas, correccion.VigenteDesde)`, de la 4.3.
  5. La experiencia no es la misma: en A6 se aprueba sin paso extra (el aviso está antes del botón);
     en la corrección hay que confirmar con "Corregir igual". Puede estar bien (en la 4.4 la agenda se
     escribe en el mismo formulario y el aviso no se puede mostrar antes), pero nadie lo decidió.
- **Por qué importa:** es el punto que la 5.3 va a implementar de verdad; si cada story lo llama
  distinto, el día que haya turnos una de las dos queda mal. Es la I-1 del sprint 2 repetida.
- **Corrección:** en la 4.4, la firma del AC 20 de la 4.3 con `hasta` calculado por
  `ProximaAprobadaDespuesDe` (T15 verifica la agenda y el `hasta`); el aviso con
  `AvisoTurnosFueraDeAgenda`; el aviso de reemplazo calculado contra la fecha de la corrección (al
  dibujar con el `Rige desde` del formulario); reusar `TurnosFueraDeAgendaEspia` y
  `TurnosFueraDeAgendaFijos` (borrar los dos archivos de la 4.4 del `files_to_create`); y llevar el
  punto 5 a la decisión 25.

### I-3 · 4.2 ↔ 5.1 — El permiso por rol en la base se crea dos veces

- **Dónde:** 4.2 AC 21, Task 2, "Implementación concreta" (`ExigirMedicoActivoAsync` y el privado
  `ExigirActivoConRolAsync`), T15, archivo `ActorVigenteMedicoTests.cs`; 5.1 AC 18, 21 (viñeta
  "Sprint 3"), Task 1, Task 3, T16, archivo `ActorVigenteIdentityTests.cs`.
- **Problema:** la 4.2 (que va antes) suma a la interfaz un método por rol y deja el general como
  **privado** dentro de `ActorVigenteIdentity`, con el nombre y la firma
  `ExigirActivoConRolAsync(actor, rol, mensaje, ct)`. La 5.1 dice "si ya existe un método general, usalo",
  pero lo busca **en la interfaz**, no lo encuentra y agrega uno **público** con el mismo nombre y la misma
  firma: no compila hasta que alguien lo note. Además las dos crean un archivo de pruebas para la misma
  clase y prueban los mismos casos (activo, desactivado, otro rol, dos roles, inexistente).
- **Por qué importa:** la regla 1 del brief ("reusar, no copiar") y la decisión 16 del S2 quedan con dos
  caminos para lo mismo; la 2.2 ya mostró que el permiso escrito dos veces termina distinto.
- **Corrección:** la 4.2 es dueña. Agrega a `IActorVigente` el método general **público**
  `ExigirActivoConRolAsync(actor, rol, mensajeSiNoLoEs, ct)` y lo usa con `Roles.Medico` (sin
  `ExigirMedicoActivoAsync`); `ExigirAdministradorActivoAsync` lo llama sin cambiar su firma. Un solo
  archivo de pruebas, `Aplicacion.Tests/Personal/ActorVigenteIdentityTests.cs`, creado por la 4.2 (T15
  con los casos de las dos). La 5.1 lo usa con `Roles.Recepcionista`, saca el método, la Task 3 y T16 (o
  T16 pasa a ser sólo "recepcionista activo pasa / desactivado no", sobre el método de la 4.2) y suma
  `"4.2"` a su `depends_on`.

### I-4 · 4.3 ↔ 4.2 — Dos pruebas de la 4.3 arman datos que el índice nuevo de la 4.2 prohíbe

- **Dónde:** 4.3 T3 ("M4 con un borrador, una rechazada y una aprobada") y T15 ("otra agenda propia
  rechazada" del mismo médico, como se lee); 4.2 AC 19 y D-1 (índice `ux_agendas_medico_una_editable`:
  una sola `Borrador` o `Rechazada` por médico).
- **Problema:** con la 4.2 implementada, el "Given" de T3 falla con `23505` al guardar la segunda
  agenda editable del mismo médico, y T15 también si las dos rechazadas son del mismo médico. La 4.3 se
  escribió sin conocer la D-1 de la 4.2.
- **Por qué importa:** son fallas seguras al implementar la 4.3, en el armado y no en lo que se prueba.
  Y si alguien las "arregla" sacando el índice, se pierde la defensa de la 4.2.
- **Corrección:** T3 con el borrador y la rechazada en médicos distintos (M4 y M6); T15 con la "otra
  agenda rechazada por Ana Uno" de otro médico propio. Si el docente elige otra opción en la decisión 2,
  revisar las dos de nuevo. Opcional: `RechazarAsync` traduce también el `23505` de ese índice (M-10).

### I-5 · 4.2 + 4.3 — Las ayudas de prueba de agendas se escriben dos veces, con firmas distintas

- **Dónde:** 4.2 `Aplicacion.Tests/Agendas/DatosDePropuesta.cs` y `Web.Tests/Agendas/DatosDePropuesta.cs`;
  4.3 `Aplicacion.Tests/Agendas/DatosDePropuestas.cs` y `Web.Tests/Agendas/DatosDePropuestas.cs`.
- **Problema:**

  | Ayuda | 4.2 | 4.3 |
  |---|---|---|
  | Médico con usuario (Aplicacion.Tests) | `MedicoConUsuarioAsync(proveedor, bool activo = true)` → `(long, ActorAuditoria)` | `MedicoConUsuarioAsync(proveedor, string? email)` → `(long, Guid)` |
  | Guardar una agenda (Web.Tests) | `GuardarAgendaAsync(app, AgendaMedico agenda)` | `GuardarAgendaAsync(app, medicoId, estado, vigenteDesde, reloj, franjas)` |
  | Reloj corrido (Web.Tests) | `RelojCorrido(base_, dias)` | `RelojCorridoDias(base_, dias)` |

  Dos archivos casi con el mismo nombre en cada carpeta (`DatosDePropuesta` y `DatosDePropuestas`) y
  dos métodos de extensión con el mismo nombre sobre el mismo tipo.
- **Por qué importa:** compila (las firmas se distinguen), pero cada story futura de agendas va a
  tener que elegir, y la próxima suma una tercera. El supuesto S-6 de la 4.3 ya lo anticipaba.
- **Corrección:** la 4.2 es dueña de `DatosDePropuesta` en los dos proyectos. Su
  `MedicoConUsuarioAsync` suma el parámetro opcional `string? email = null` (lo fija con
  `UserManager.SetEmailAsync`, como describe la 4.3) y devuelve también el `Guid` del usuario. La 4.3
  saca sus dos archivos del `files_to_create` y usa los de la 4.2 (su `AdminPropioAsync`,
  `RegistrosDe` y `MensajesDe` pasan a `DatosDePropuesta` de Aplicacion.Tests como agregado de la 4.3).
  Un solo nombre para el reloj: `RelojCorridoDias`, como la ayuda privada de la 4.1.

### I-6 · 4.2 + 4.4 — La auditoría de una misma entidad queda con dos formatos de franja

- **Dónde:** 4.2 AC 17 (`"dia": 0–6`, `"hasta": "HH:mm"`, la medianoche como `00:00`); 4.4 tabla de
  auditoría e Implementación concreta (`"dia": "lunes"`, `"hasta": "24:00"`), y su cambio de
  `Franja.NombreDelDia` de interno a público "para no escribir otro arreglo de días".
- **Problema:** `propuesta-agenda.enviada` y `agenda.corregida` describen franjas de la misma entidad
  (`AgendaMedico`) con distinto tipo de día y distinta medianoche.
- **Por qué importa:** la 9.1 (A5) va a leer las dos para mostrar la historia de una agenda; con dos
  formatos tiene que saber de qué story vino cada registro. Y abrir `NombreDelDia` sólo para esto deja
  público un detalle que la 4.2 da por interno (su Task 1 lo verifica).
- **Corrección:** un solo formato, el de la 4.2 (día como número de la base, horas `HH:mm`, medianoche
  `00:00`), escrito en el AC 17 de la 4.2 como contrato para la 4.4 y la 9.1. La 4.4 lo copia, no toca
  `Franja.cs` (sale de `files_to_modify`) y T10 verifica el formato.

### I-7 · 4.3 + 4.4 — FR-14e ("el médico recibe un email") no se cumple con lo recomendado

- **Dónde:** 4.3 D-1, AC 15, 16; 4.4 D-3, AC 14; 2.3 AC 16 ("ninguna toca A4").
- **Problema:** el email del médico existiría en `UsuarioPersonal.Email` (lo trae Identity), pero nadie
  lo carga: el alta de A4 no lo pide, el sembrador tampoco, y A4 no tiene edición de usuarios. Con la
  opción recomendada por la 4.3, en la práctica no sale ningún email y la pantalla dice "sin email". La
  4.3 lo dice con honestidad; la 4.4 se apoya en eso para no mandar email.
- **Por qué importa:** FR-14e queda a medias sin que el plan lo diga, y la 4.3 sugiere sumarlo "a la 2.3",
  que dice lo contrario. Las dos decisiones (4.3 D-1 y 4.4 D-3) son la misma pregunta.
- **Corrección:** decisión 1 de abajo (consolidada). Si queda la recomendada, anotar en el plan una
  story corta "email del personal en A4 (alta y edición)" y que FR-14e se completa ahí.

---

## MENORES

| # | Story · lugar | Problema | Corrección |
|---|---|---|---|
| M-1 | Las cinco · frontmatter y `Status:` | `status: drafted` no es un estado del vocabulario (`backlog`, `ready-for-dev`, `in-progress`, `review`, `done`): los scripts de estado no las reconocen. | Pasar a `ready-for-dev` al aprobar la revisión. |
| M-2 | 4.3 AC 1, 7, 17 · 4.4 AC 19 vs 4.2 | Fechas `dd/MM` en unas pantallas y `dd/MM/yyyy` en otras (la UX dibuja `dd/MM`; la 4.2 eligió con año). En A6 conviven las dos. | Elegir una regla: con año en textos que dicen "desde"/"el"; sin año sólo en columnas angostas. Aplicarla en las tres. |
| M-3 | 4.3 frontmatter, Task 5 | Agrega `@using HospitalTurnos.Aplicacion.Agendas` a `_Imports.razor`, que ya agrega la 4.2. | "Si no está" o sacarlo. |
| M-4 | 4.3 Task Dependencies | "Pueden ir en paralelo la 2.3 y la 5.1": la 5.1 toca cinco archivos que también tocan la 4.2/4.3 (`SesionYPermisosHttpTests`, `_Imports.razor`, `ServiciosDeInfraestructura`, `RutasPersonal`, `IActorVigente`) y la 2.3 comparte `AccionesAuditoria` con la 5.1. En paralelo hay conflictos de merge. | Respetar el orden del brief; quitar la frase o limitarla a "la 2.3 con la 4.x". |
| M-5 | 5.1 `depends_on` | No incluye la 4.2, aunque reusa su método de permiso (I-3). | Agregar `"4.2"`. |
| M-6 | 4.4 AC 3, Implementación | La fecha pasada se compara a mano (`vigenteDesde < hoy`) con otro mensaje; la 4.3 deja la misma comparación en `FechaDeInicioYaPaso`. | Opcional: usar la regla de la 4.3 para la comparación y conservar el mensaje propio. |
| M-7 | 4.4 frontmatter | Sus pruebas web van en `Web.Tests/Catalogo/`; las de agendas de la 4.2 y la 4.3, en `Web.Tests/Agendas/`. | `Web.Tests/Agendas/CorreccionDeAgendaHttpTests.cs`. |
| M-8 | 4.2 AC 25 | El caso `"duracion"` de `Base_ValoresFueraDeRango_LosRechazanLosControles` (4.1) inserta un segundo `Borrador` para un médico que ya tiene uno. Sigue en verde con el índice nuevo porque PostgreSQL revisa el `CHECK` antes que el índice único, pero nadie lo dejó escrito. | Nombrarlo en la no-regresión de la 4.2, para que nadie lo "arregle" mal si cambia. |
| M-9 | 4.2 Given/Fixture Web | Dice que `RelojCorrido` es "la misma idea que la ayuda privada" de `EstadoDeAgendaEnA1HttpTests`; esa ayuda se llama `RelojCorridoDias`. | Usar ese nombre (ver I-5). |
| M-10 | 4.3 `RechazarAsync` | No traduce el `23505` de `ux_agendas_medico_una_editable`: si hubiera una editable (datos armados a mano), rechazar da `500`. La 4.2 lo impide por su AC 18. | Aceptarlo por escrito, o traducirlo a `ConflictoException` (una línea). |
| M-11 | Transversal · conteos | 155 pruebas contra 38 estimadas (4,1 veces; el S2 fue 3,6). Las más grandes en relación: 4.4 (27 contra 5) y 4.2 (45 contra 9). El plan mide el tiempo por prueba **estimada**, así que no cambia la proyección, pero el desvío ya se ve. | Aviso para el docente; sin cambio en las stories. |

---

## Decisiones abiertas consolidadas

Las cinco stories traen 25 decisiones; juntando la repetida (email del médico) quedan **24**. La
revisión agrega **2** (25 y 26), que salen de los hallazgos. La 7 suma a la decisión de la 4.3 el
formato de la línea, que salió de I-1.

| # | Decisión | Origen | Recomendada | ¿De acuerdo? |
|---|---|---|---|---|
| 1 | De dónde sale el email del médico (y si la corrección avisa por email) | 4.3 D-1 + 4.4 D-3 | El `Email` del usuario médico vinculado, leído por la 4.3; la 4.4 no manda email; cargar el campo queda para una story corta "email del personal en A4" | **Sí, con aviso**: FR-14e queda a medias hasta esa story (I-7); anotarla en el plan. Alternativas: la 4.3 agrega el campo a A4 (A4 hoy no tiene edición de usuarios: es más que un campo), o un email propio en la ficha del médico (migración en `medicos`). |
| 2 | ¿Un médico puede tener más de un borrador? | 4.2 D-1 | Una sola editable por médico, con índice único parcial | **Sí**; obliga a corregir 4.3 T3 y T15 (I-4). |
| 3 | M2 con renderizado en el servidor o interactiva | 4.2 D-2 | En el servidor (SSR), como todo el personal | **Sí**. Si se eligiera interactiva, la pantalla de corrección de la 4.4 también tendría que serlo (C-1). |
| 4 | Con qué arranca una propuesta nueva | 4.2 D-3 | Con la duración y las franjas de la última aprobada | **Sí**. |
| 5 | Cómo es la grilla de franjas | 4.2 D-4 | Tabla con una fila por franja | **Sí**. |
| 6 | "Enviar" envía lo que está en pantalla | 4.2 D-5 | Sí, en un paso | **Sí**. |
| 7 | Quién rechazó en M2: cómo se escribe | 4.3 D-2 + I-1 | Nombre completo de la auditoría, en una sola línea: `Rechazada el dd/MM/yyyy a las HH:mm por Lucía Pérez.` | **Sí** (la UX dibuja "— Lucía P., 12/11 10:05"; el nombre completo evita adivinar el apellido). |
| 8 | Contador con cero pendientes | 4.3 D-3 | Sin número | **Sí**. |
| 9 | Orden del listado A6 | 4.3 D-4 | Por "Rige desde", la más próxima primero | **Sí**. |
| 10 | Desde cuándo puede regir una corrección | 4.4 D-1 | Desde hoy o después; la pantalla propone hoy | **Sí**. |
| 11 | Propuesta enviada pendiente al corregir | 4.4 D-2 | Sigue enviada, con aviso | **Sí**. |
| 12 | Corregir la agenda de un médico sin ninguna | 4.4 D-4 | Sí, se puede | **Sí**. |
| 13 | Dónde queda el motivo de la corrección | 4.4 D-5 | Sólo en la auditoría, sin migración | **Sí**. |
| 14 | Contraseña nueva a un usuario bloqueado | 2.3 D-1 | También lo desbloquea | **Sí**. |
| 15 | Contraseña nueva a un desactivado | 2.3 D-2 | No (409, sin botón) | **Sí**. |
| 16 | El administrador sobre sí mismo | 2.3 D-3 | No (400, sin botón) | **Sí**. |
| 17 | Confirmación antes de generar la contraseña | 2.3 D-4 | Sin paso de confirmación | **Sí**. |
| 18 | Motivo auditado del cambio posterior | 2.3 D-5 | Queda `primer-ingreso` | **Sí**. |
| 19 | Quién puede ver y cargar pacientes | 5.1 D-1 | Sólo la recepción | **Sí**. |
| 20 | Cómo se escribe el DNI | 5.1 D-2 | Con puntos y espacios, sin ceros adelante, 7 u 8 números | **Sí**. |
| 21 | Qué teléfono se acepta | 5.1 D-3 | 6 a 15 números, con espacios, guiones, paréntesis y `+` | **Sí**. |
| 22 | Dos pacientes con el mismo email | 5.1 D-4 | Sí, se permite | **Sí**. |
| 23 | El DNI en la dirección de la página | 5.1 D-5 | No: búsqueda por POST y `?paciente=<id>` | **Sí**. |
| 24 | Auditar quién consultó a un paciente | 5.1 D-6 | No | **Sí**. |
| 25 | Turnos fuera de agenda: ¿confirmación extra? | esta revisión (I-2) | A6 sin paso extra (el aviso está antes del botón); la corrección con "Corregir igual", porque la agenda se escribe en el mismo formulario. Las dos con el mismo puerto y el mismo componente de aviso | Nueva. |
| 26 | La carrera en las otras acciones de A4 (desactivar, reactivar, desbloquear) | nota de la 2.3 | Story chica aparte en el S4: traducir `ConcurrencyFailure` a `409` como hace la 2.3 | Nueva. |

---

## Lo que salió bien

- Las cinco verificaron el código real: todos los nombres que citan como existentes están en `main`.
  Los errores de nombres están sólo en lo que se marcó como supuesto, y por eso se ven.
- La 4.3 y la 4.4 marcaron sus supuestos (`S-n`, `[ASUMIDO A-n]`) y dijeron que vale la dueña. Es la
  lección del C-1 del S2 aplicada: el choque aparece en la revisión y no al programar.
- B-1 cumplido: cada AC de contrato nace con pruebas, y la 4.2 incluso prueba con reflexión los
  nombres y parámetros de sus componentes (T14).
- La 4.2 encontró dos trampas de EF antes de caer: un segundo `HasIndex` sobre la misma columna sin
  nombre pisa el filtro del primero, y cambiar sólo franjas no controla `xmin` del padre.
- La 2.3 leyó el código de Identity 10 y descartó `ResetPasswordAsync` (pide un proveedor de tokens que
  no está registrado); usa el sello de concurrencia como última defensa y lo prueba con el mismo doble
  de carreras.
- La 4.3 justificó por qué su puerto que hoy devuelve vacío no es un stub, a diferencia del que la 3.1
  descartó, y lo ata con una prueba-trampa con mensaje para la 5.3.
- La 5.1 confirma el permiso en la base también en las lecturas (datos personales) y deja el DNI fuera
  de la dirección de la página.

---

## Decisiones del docente y correcciones aplicadas (2026-10-17)

### Decisiones elegidas

Las 26 decisiones se aceptaron con la opción recomendada de la tabla de arriba, incluidas la 25 y la
26. En cada story las marcas `[DECISIÓN ABIERTA D-n]` se reemplazaron por "(decisión n)" y quedó una
sección "Decisiones tomadas por el docente (2026-10-17)", como en la 4.1. Precisiones del docente: en
la decisión 1, el email se carga con una story corta nueva, **2.4 "Email del personal en A4"**
(sprint 4); en la decisión 26, la carrera va a una story chica nueva, **2.5** (sprint 4).

| # | Decisión | Elegido | Dónde quedó |
|---|---|---|---|
| 1 | Email del médico; email en la corrección | El `Email` del usuario médico vinculado, leído por la 4.3; la 4.4 no manda; lo carga la 2.4 (S4). FR-14e a medias hasta entonces, sin error si está vacío | 4.3 AC 15, 16, 29 (contrato con la 2.4), T7, T14; 4.4 AC 14; 2.3 AC 16 |
| 2 | Más de un borrador | Una editable por médico, índice único parcial | 4.2 AC 18, 19; 4.3 T3, T15 (I-4) |
| 3 | M2 SSR o interactiva | SSR | 4.2 AC 7, 8, 10; 4.4 AC 18 |
| 4 | Con qué arranca una propuesta | Con la última aprobada | 4.2 AC 4 |
| 5 | Grilla de franjas | Tabla, una fila por franja | 4.2 AC 7 |
| 6 | "Enviar" envía lo de la pantalla | Sí, en un paso | 4.2 AC 15 |
| 7 | Quién rechazó en M2 | Nombre completo en una línea: `Rechazada el dd/MM/yyyy a las HH:mm por Lucía Pérez.` | 4.2 AC 5, 24; 4.3 AC 17, T15, T34 |
| 8 | Contador con cero | Sin número | 4.3 AC 3 |
| 9 | Orden de A6 | Por "Rige desde" | 4.3 AC 1 |
| 10 | Desde cuándo rige una corrección | Desde hoy; propone hoy | 4.4 AC 3 |
| 11 | Propuesta enviada al corregir | Sigue enviada, con aviso | 4.4 AC 13, 19 |
| 12 | Corregir sin agenda | Sí | 4.4 AC 15 |
| 13 | Dónde queda el motivo | Sólo en la auditoría | 4.4 AC 2 |
| 14–18 | Contraseña olvidada | Desbloquea; no a desactivados; no a sí mismo; sin confirmación; `primer-ingreso` | 2.3 AC 4–7, 13 |
| 19–24 | Pacientes | Sólo recepción; DNI con puntos y sin ceros; teléfono 6–15 números; email repetido sí; DNI fuera de la dirección; sin auditar consultas | 5.1 AC 2, 3, 7, 10, 16, 20 |
| 25 | Turnos fuera: confirmación extra | A6 sin paso extra; la corrección con `Corregir igual`; mismo puerto y mismo componente | 4.3 AC 22, 26; 4.4 AC 12, 23 |
| 26 | Carrera en desactivar/reactivar/desbloquear | Story 2.5 (S4), sin especificar acá | 2.3 "Decisiones tomadas", AC 16, "Lo que NO" |

### Cómo quedó cada hallazgo

| # | Cómo quedó |
|---|---|
| C-1 | 4.2 AC 24 fija el uso por la 4.4 (modelo que hereda, propiedad `Input`, `OnSubmit` con `AplicarAccionDeEdicion()` primero, `Input.Accion` = `corregir` / `corregir-igual`) y suma **T46** (el editor con un modelo heredado). 4.4: `FormularioCorreccion : FormularioAgenda`, `<EditorDeAgenda Input="Input" MinimoVigenteDesde=… TurnosPorSemanaAntes=… />`, `OnSubmit`; los botones del editor no piden el motivo (AC 18, 21) y lo prueba **T28** (nueva); sin "cálculo en vivo"; la Task 1 se detiene si el editor no tiene el contrato del AC 24 de la 4.2. |
| I-1 | (a) `ResumenDeAgenda` suma `CompararCon` en la 4.2 (AC 24, T14 y **T47** nueva). (b) La 4.3 usa los nombres y la ruta reales (AC 25 sin supuestos). (c) Quién rechazó por `VistaPropuesta.RechazadaPor` (`string? = null`, al final), cargado por `PropuestaAgendaEnBase`; `QuienRechazoAsync` sale de `IAprobacionPropuestas`; la 4.3 T15 se reescribe sobre `ObtenerAsync`. (d) Una línea, decisión 7; T34 de la 4.3 la verifica completa. La T41 de la 4.2 **no** cambia: su rechazada no tiene registro de auditoría y es el caso "sin dato" del contrato. (e) `files_to_modify` de la 4.3 con rutas reales (suma `IPropuestaAgenda.cs`, `PropuestaAgendaEnBase.cs`, `MiPropuesta.razor` y las dos `DatosDePropuesta`). |
| I-2 | 4.4: firma de la 4.3 con `hasta` por `ProximaAprobadaDespuesDe` (AC 12, T15 con y sin aprobada posterior); el aviso con `AvisoTurnosFueraDeAgenda` (AC 23); el aviso de reemplazo contra la fecha del formulario (`DatosParaCorregir.Aprobadas` en lugar de `AprobadaFuturaDesde`, AC 8 y 19, T28); reusa `TurnosFueraDeAgendaEspia` y `TurnosFueraDeAgendaFijos` (salen `TurnosFueraDeAgendaDePrueba.cs` y `CorreccionWebApplicationFactory.cs`); el punto 5, decisión 25. |
| I-3 | 4.2: `IActorVigente.ExigirActivoConRolAsync` público (sin `ExigirMedicoActivoAsync`); `ExigirAdministradorActivoAsync` lo llama; archivo `Aplicacion.Tests/Personal/ActorVigenteIdentityTests.cs` con T15 (casos del médico y del administrador). 5.1: lo usa con `Roles.Recepcionista`, no toca `IActorVigente` ni `ActorVigenteIdentity`, la Task 3 queda sin código de permiso y T16 se reduce a "recepcionista activo pasa / desactivado no" en el archivo de la 4.2; `"4.2"` en `depends_on`. |
| I-4 | 4.3 T3: el borrador (M4) y la rechazada (M6) en médicos distintos (el texto decía "4 médicos" y eran 5: quedan 6); T15: la otra rechazada es de otro médico. |
| I-5 | 4.2 dueña de `DatosDePropuesta` en los dos proyectos; `MedicoConUsuarioAsync(proveedor, activo, email)` → `(MedicoId, Actor, UsuarioId)`; un solo reloj: `RelojCorridoDias`. La 4.3 saca sus dos archivos y suma `AdminPropioAsync`, `RegistrosDe`, `MensajesDe` (Aplicacion) y `VincularMedicoConEmailAsync` (Web) a los de la 4.2. |
| I-6 | 4.2 AC 17 escribe el formato de franjas como contrato (día numérico, `HH:mm`, medianoche `00:00`; T23 lo verifica). 4.4 lo copia, `Franja.cs` sale de `files_to_modify` y T10 verifica `{"dia": 2, …}`. |
| I-7 | Decisión 1. 4.3 AC 16 dice que FR-14e queda a medias hasta la 2.4, sin error; AC 29 nuevo es el contrato con la 2.4 (la no-regresión pasa a AC 30). |
| M-1 | No se tocó, como pidió el docente: `status: drafted` lo cambia él al aprobar. |
| M-2 | Regla escrita en la 4.2 (intro de los AC) y en la 4.3: con año en los textos con "desde" o "el"; sin año sólo en columnas angostas. Cambian el aviso de reemplazo (4.3 AC 7, T24; 4.4 AC 19, T21) y la línea del rechazo (decisión 7). |
| M-3 | La 4.3 ya no toca `_Imports.razor` (lo agrega la 4.2). |
| M-4 | 4.3 Task Dependencies: se respeta el orden del sprint, con los cinco archivos compartidos nombrados. |
| M-5 | `"4.2"` en el `depends_on` de la 5.1. |
| M-6 | 4.4 AC 3 y su fábrica usan `FechaDeInicioYaPaso(reloj)` de la 4.3 con el mensaje propio. |
| M-7 | `Web.Tests/Agendas/CorreccionDeAgendaHttpTests.cs`. |
| M-8 | 4.2 AC 25 nombra el caso `"duracion"` de `Base_ValoresFueraDeRango_LosRechazanLosControles` y por qué sigue en verde. |
| M-9 | 4.2 Given/Fixture: `RelojCorridoDias`. |
| M-10 | Aceptado por escrito en "Decisiones de esta story" de la 4.3 (sólo con datos armados a mano). |
| M-11 | Aviso para el docente, sin cambio. Con las pruebas nuevas, 158 contra 38 estimadas (4,2 veces). |

**Conteos finales:** 2.3: 17 AC / 16 pruebas; 4.2: 25 / 47 (+T46, T47); 4.3: 30 / 34 (+AC 29); 4.4:
28 / 28 (+T28); 5.1: 22 / 33. **158 pruebas.** Todo AC tiene prueba salvo los de no-regresión (2.3
AC 17, 4.3 AC 30, 4.4 AC 28), que los cubre la suite.

**Pendiente fuera de las stories (archivos que esta corrección no toca):** sumar al plan las stories
2.4 y 2.5 en el sprint 4 (decisiones 1 y 26) y la herencia de la 5.3 (4.3 AC 27); el aviso de tamaño
del S3 (M-11); `status` de las cinco (M-1).

---

## Segunda vuelta (2026-10-17)

Alcance: las cinco stories ya corregidas. Se verificó (1) que cada corrección de la tabla de arriba
esté en las stories y que no queden restos de lo viejo; (2) las costuras entre stories: firmas,
nombres, carpetas, dueño único de cada archivo nuevo y orden 2.3 → 4.2 → 4.3 → 4.4 → 5.1; (3) que cada
contrato tenga su prueba; (4) los nombres del código existente citados por primera vez, contra `main`
en `a8432c3`; (5) conteos con un script sobre las tablas.

| Severidad | Cantidad |
|---|---|
| CRÍTICO | 0 |
| IMPORTANTE | 0 |
| MENOR | 3 (corregidos en esta vuelta) |

**Lo que se confirmó**

- No quedan marcas `[DECISIÓN ABIERTA]` ni `[ASUMIDO]`, ni restos de `ExigirMedicoActivoAsync`,
  `QuienRechazoAsync`, `ResumenAgenda`, `DatosDePropuestas`, `RelojCorrido(`, `Personal/Medico/`,
  `FechaMinima`, `AprobadaFuturaDesde`, `TurnosFueraDeAgendaDePrueba`, `CorreccionWebApplicationFactory`
  ni "cálculo en vivo" (búsqueda en las cinco; sólo aparecen en los registros de cambios y en los "NO
  hacer").
- Firmas iguales en todas las que las citan: `ExigirActivoConRolAsync(actor, rol, mensajeSiNoLoEs, ct)`
  (4.2 la crea; 5.1 la usa); `ITurnosFueraDeAgenda.BuscarAsync(agendaNueva, hasta, ct)` con `hasta` por
  `ProximaAprobadaDespuesDe` (4.3 AC 20, 21, 26; 4.4 AC 12); `EditorDeAgenda` con `Input`,
  `MinimoVigenteDesde`, `TurnosPorSemanaAntes` (4.2 AC 24; 4.4 tabla de nombres, AC 18 y código);
  `ResumenDeAgenda` con `Agenda`, `Forma`, `CompararCon` (4.2 AC 24 y T14; 4.3 AC 4 y 25);
  `VistaPropuesta.RechazadaPor` (4.2 AC 24; 4.3 AC 17); el aviso de reemplazo con `dd/MM/yyyy` (4.3 AC 7,
  T24; 4.4 AC 19, T21, T28).
- Ningún archivo nuevo tiene dos dueños: `ActorVigenteIdentityTests.cs` y las dos `DatosDePropuesta` los
  crea la 4.2 y los modifican después la 4.3 / 5.1; los dobles de turnos fuera los crea la 4.3 y los
  usa la 4.4. Lo que una story modifica de otra va siempre después en el orden.
- Contratos con prueba: la línea del rechazo con nombre (4.3 T34) y sin nombre (4.2 T41); el editor con
  modelo heredado (4.2 T46) y su uso real (4.4 T21, T28); `CompararCon` (4.2 T47) y su uso en A6 (4.3
  T23); el permiso por rol (4.2 T15) y su uso por la recepción (5.1 T16); el formato de franjas (4.2 T23,
  4.4 T10); el contrato con la 2.4 (4.3 T7, T14).
- Nombres de `main` citados por primera vez: `PersistenciaAgendasTests.Base_ValoresFueraDeRango_LosRechazanLosControles`,
  la ayuda privada `RelojCorridoDias` de `EstadoDeAgendaEnA1HttpTests`, `IActorVigente` con un solo
  método y `ActorVigenteIdentity(UserManager<UsuarioPersonal>)`, `Franja.NombreDelDia` y `Tramo`
  `internal`, `AgendaMedico.Hoy`/`Fecha` privados, `DatosDeAgenda.Franja` (lunes a miércoles: las pruebas
  de Aplicacion de la 4.4 usan martes), `_Imports.razor` sin `Aplicacion.Agendas`, `RutasPersonal.FichaMedico(long)`,
  `TimeProviderExtensions.AhoraLocal`, `ActorDeSesion.Desde`, `ManejadorGlobalDeErrores.EsEsperable` y
  `FijarEstadoEnPagina`, `FormularioHttp.EnviarAsync(cliente, ruta, formulario, campos, conToken, accept)`,
  la constante de página `AccionDesconocida` (patrón de `Especialidades.razor`). Todos existen.
- Conteos: filas numeradas sin huecos y iguales al encabezado en las cinco (16, 47, 34, 28, 33); AC sin
  huecos (17, 25, 30, 28, 22).

**MENORES**

| # | Story · lugar | Problema | Corrección |
|---|---|---|---|
| M2-1 | 4.3 Given/Fixture T18 (`TurnosFueraDeAgendaEspia`) | El espía anotaba sólo `(long AgendaId, DateOnly? Hasta)`. La 4.4 lo reusa con una agenda todavía sin guardar (`Id = 0`) y su T15 verifica las franjas recibidas: con el id no alcanza. | **Corregido:** `Llamadas` guarda `(AgendaMedico Agenda, DateOnly? Hasta)`; la T18 de la 4.3 sigue igual (mira `Agenda.Id`). |
| M2-2 | 4.4 AC 8, 12, T15, T16, código | `ResultadoCorreccion` llevaba sólo la cantidad de turnos fuera, pero el componente `AvisoTurnosFueraDeAgenda` (I-2) necesita la lista para el desplegable `Ver`. | **Corregido:** `ResultadoCorreccion(long? AgendaId, IReadOnlyList<TurnoFueraDeAgenda> TurnosFueraDeAgenda)`; la página la pasa al componente. |
| M2-3 | 4.4 T18 | Verificaba que no hubiera email "al `para` del usuario", pero el usuario no tenía email cargado: la prueba pasaba aunque el código mandara. | **Corregido:** el usuario se crea con email con `MedicoConUsuarioAsync(…, email:)` de la 4.2. |

**Notas (sin cambio en las stories):**

- La 4.1 (cerrada) cita en su AC 31 el aviso de reemplazo con `dd/MM`. Vale el texto de la 4.3, dueña de
  la pantalla, con año (M-2). El AC 31 de la 4.1 es declarativo y ninguna prueba de la 4.1 fija ese
  texto: no hace falta tocarla.
- La pantalla de corrección hereda de `FormularioAgenda` también sus validaciones de campo (duración y
  `Rige desde` vacíos, filas incompletas). La 4.4 no las vuelve a probar: las prueban T8 y T40 de la 4.2.

**Veredicto:** limpia para implementar en el orden 2.3 → 4.2 → 4.3 → 4.4 → 5.1. **Nada requiere una
decisión del docente.** Siguen pendientes, fuera de las stories, los puntos de "Pendiente fuera de las
stories" (plan: 2.4, 2.5 y herencia de la 5.3; aviso de tamaño; `status`).
