---
documento: Revisión adversarial de las stories del sprint 2
proyecto: Gestión de turnos — Hospital Municipal Las Acacias (caso simulado)
fecha: 2026-10-16
alcance: 2.2, 3.1, 3.2, 4.1 (antes de implementar; escritas en paralelo)
contexto: 00-contrato-tecnico.md, 02-prd.md, 03-ux.md, 04-arquitectura.md, 05-epicas-y-plan.md, retrospectiva-sprint-1.md, story-format.md (GOLD), código de main en c3413cc
---

# Revisión adversarial — Sprint 2

## Resumen

| Severidad | Cantidad | Stories más afectadas |
|---|---|---|
| CRÍTICO | 1 | 3.2 ↔ 4.1 (contrato del estado de agenda) |
| IMPORTANTE | 8 | transversal (I-1, I-2, I-3, I-6), 3.2 + 4.1 (I-4), 3.2 (I-5), 2.2 + 3.2 (I-7), 3.1 + 3.2 (I-8) |
| MENOR | 12 | todas |

**Veredicto:** 2.2 y 3.1 se pueden implementar después de corregir las piezas compartidas (I-1, I-2).
La 4.1 **no** arranca tal como está: su Task 1 compara contra un contrato de la 3.2 que no es el que
la 3.2 escribe, y manda a DETENERSE (C-1). Todo se corrige con cambios de texto, sin rediseño.

### Puntos pedidos que se verificaron

| Punto | Resultado | Evidencia |
|---|---|---|
| Tipo de Id: ¿qué usa el código? | Las entidades propias usan **`long` identity**; Identity usa `Guid`. La 3.1 (`Especialidad`) y la 4.1 (`AgendaMedico`, `Franja`) siguen `long`; la 3.2 pone `Medico.Id` en `int`. Ver I-4. | `RegistroAuditoriaConfiguracion` y `MensajeSalienteConfiguracion` (`// long → identity`); contrato §Auditoría (`Id` long). |
| Carpeta de páginas | **Distinta**: 2.2 `Components/Personal/Administracion/`; 3.1 y 3.2 `Components/Personal/Admin/`. Ver I-2. | Frontmatter de las tres. |
| ¿Quién crea el enum de estado de agenda? | **Contradicción**: la 3.2 crea `Dominio/Agendas/EstadoAgendaMedico`; la 4.1 espera `EstadoAgenda` en `Aplicacion/Catalogo/IGestionMedicos.cs` "creado por la 3.2". Ver C-1. | 3.2 AC 4/23; 4.1 AC 22/30, Task 1, Dev Notes. |
| Piezas compartidas (`ActorDeSesion`, cartel de error, ayudas de prueba) | Especificadas **dos o tres veces** con firmas distintas. Ver I-1. | 2.2 AC 26; 3.1 frontmatter y Task 1; 3.2 Given/Fixture. |
| Orden 2.2 → 3.1 → 3.2 → 4.1 y migraciones | **Bien encadenado**: cada story genera su migración con `dotnet ef migrations add` y regenera el snapshot; ninguna usa `database update`. Falta 2.2 en `depends_on` de 4.1 (M-3). | `GestionUsuariosPersonal` → `AgregarEspecialidades` → `AgregarMedicos` → `AgregarAgendas`. |
| Nombres citados contra el código | **Casi todos existen** y en la línea citada: `HospitalDbContext:29-33`, `AgregarBandejaDeSalida:47-56`, `IdentidadPersonal:193-197` (`user_name_index`), `AccesoDenegado.razor:21`, `SesionYPermisosHttpTests:119` (exige el texto de construcción en toda sección permitida), `SeccionesAdministracion.razor` (6 `@page`), `FormularioHttp` es `static partial`, `PersonalWebApplicationFactory` arranca el reloj en la hora real, `AuditoriaEnBase(contexto, reloj)`, `ContextoDePrueba.Crear`, `RelojHospital.LocalTimeZone`, bUnit en Web.Tests, `TimeProvider.Testing` en Dominio.Tests. Fallan: el enum y el constructor de C-1, `RegistroEnMemoria` como destino de `LogTo` (M-2) y la "T25 de la 2.1" que cita la 3.2 (M-12). | Lectura directa de `src/` y `tests/`. |
| La convención snake_case con `HasDatabaseName`, `ToTable` y entidades sin `DbSet` | **Funciona** como suponen 3.2 y 4.1: `ASnakeCase` deja igual un nombre ya en snake_case; una entidad sin `DbSet` (`Franja`) necesita `ToTable("franjas_agenda")`, que la 4.1 pone. Los `CHECK` no se renombran (4.1 lo sabe). | `ConvencionSnakeCase.Aplicar`. |
| Tiempo en las pruebas | **Bien**: ninguna prueba espera tiempo real. 2.2 usa `FakeTimeProvider(DateTimeOffset.UtcNow)` a propósito (Identity bloquea con el reloj del sistema); 4.1 prueba la fecha local a las 23:30 de Córdoba (T12). | 2.2 Given/Fixture; 4.1 T10–T17. |
| Integración contra PostgreSQL real | **Sí**, en las cuatro (Testcontainers y la `PostgresFixture` de cada proyecto). | — |
| Conteos declarados = filas | **Coinciden**: 2.2 27 AC / 31 pruebas; 3.1 19 / 23; 3.2 24 / 31; 4.1 32 / 43. Todo AC tiene prueba, salvo los de contrato (M-4). | Tablas `### Tests`. |
| Seguridad | Contraseña temporal bien tratada (sólo en la respuesta, `no-store`, nunca en registro ni auditoría, generador criptográfico). Toda escritura de 2.2, 3.1 y 3.2 se audita en la misma transacción. Validación en pantalla y servidor en las tres pantallas. Hueco: el permiso en el caso de uso no se controla igual en las tres (I-3). | — |

---

## CRÍTICOS

### C-1 · 3.2 ↔ 4.1 — El contrato del estado de agenda está escrito dos veces y no coincide

- **Dónde:** 3.2 AC 4, AC 23 (viñetas "4.1" y "Quien conecte el estado real"), Task 1, Implementación
  concreta (`EstadoAgendaMedico`, constructor de `GestionMedicosEnBase`); 4.1 AC 22, AC 30, Task 1,
  Task 2, Dev Notes "Que existe hoy", "Decisiones" y `EstadoDeAgenda`, Given/Fixture T43.
- **Problema:** cada story dice que la otra fija el contrato, y los dos textos se contradicen en cinco
  puntos:

  | Punto | Dice la 3.2 | Dice la 4.1 |
  |---|---|---|
  | Nombre del enum | `EstadoAgendaMedico` | `EstadoAgenda` |
  | Dónde vive | `Dominio/Agendas/EstadoAgendaMedico.cs` | `Aplicacion/Catalogo/IGestionMedicos.cs` |
  | Quién fija los valores | "los que fija la 4.1 en su AC 22" | "el de la 3.2" |
  | Regla de qué estado gana | "la del AC 22 de la 4.1" | "la Decisión abierta 2 de la 3.2" (esa decisión es el **formato de la matrícula**) |
  | Quién conecta el estado real | "la story que resuelva la DA-4 de la 4.1; allí se recomienda la 4.2" (la DA-4 de la 4.1 es sobre dónde vive `HorariosDelDia`) | la propia 4.1, en su AC 30 |
  | Constructor de `GestionMedicosEnBase` | `(contexto, auditoria, catalogo)` | `(contexto, auditoria)`; T43 lo arma con `(contexto, auditoria, consulta)` |

- **Por qué importa (bloqueo seguro):** la Task 1 de la 4.1 exige encontrar `EstadoAgenda` en
  `IGestionMedicos.cs` y la línea `EstadoAgenda = EstadoAgenda.SinAgenda`; si no coincide, DETENERSE.
  Con la 3.2 implementada como está escrita, no coincide nunca. Si el agente "arregla" en vez de
  detenerse, lo natural es crear el segundo enum: dos tipos para las mismas tres palabras. T43 tampoco
  compila (faltan parámetros). Además, la regla "una aprobada que todavía no rige da `Sin agenda`" se
  presenta como ya decidida por el docente, y nadie la decidió (ver decisión 8).
- **Corrección:** fijar el contrato una vez, en la 3.2, y que la 4.1 lo copie:
  1. El enum es `EstadoAgendaMedico` en `Dominio/Agendas/` (lo crea la 3.2, como ya escribe).
     Ventaja: como vive en el dominio, `EstadoDeAgenda.Calcular` puede vivir también en
     `Dominio/Agendas/` y la 4.1 no necesita la carpeta `Aplicacion/Agendas/EstadoDeAgenda.cs`.
  2. En la 4.1, reemplazar `EstadoAgenda` por `EstadoAgendaMedico` en AC 22, AC 29, AC 30, Task 1,
     Task 2, Dev Notes y código; quitar la cita a "Decisión abierta 2 de la 3.2" y presentar la
     regla como decisión propia (decisión 8 de esta revisión).
  3. Constructor: `GestionMedicosEnBase(contexto, auditoria, catalogo, consultaAgendas)`; T43 y la
     T6 de la 3.2 (que arma el adaptador a mano) con los cuatro parámetros.
  4. En la 3.2 AC 23: "el estado real lo conecta la 4.1 (su AC 30), en el mismo sprint".

---

## IMPORTANTES

### I-1 · 2.2, 3.1, 3.2 — Las piezas compartidas se especifican dos o tres veces, con firmas distintas

- **Dónde:** 2.2 AC 26 y Task 5–6; 3.1 frontmatter (`ActorDeSesion.cs` en `files_to_create`), Task 1,
  Implementación concreta; 3.2 Given/Fixture y "Manejo en las páginas".
- **Problema:**

  | Pieza | 2.2 | 3.1 | 3.2 |
  |---|---|---|---|
  | Actor desde la sesión | crea `ActorDeSesion.Desde(ClaimsPrincipal)` | **también** lo pone en `files_to_create`, como extensión `ComoActorAuditoria(this ClaimsPrincipal)` | usa `ActorDeSesion.Desde` |
  | Excepción → código en páginas SSR | `ManejadorGlobalDeErrores.EsEsperable` / `EstadoPara` (un solo mapeo) | `catch` propio de `ReglaDeNegocioException` y `NoEncontradoException`, con 409/400/404 escritos a mano | `catch (ReglaDeNegocioException)` con 409/400 a mano |
  | POST de formulario en pruebas | `FormularioHttp.EnviarAsync(cliente, ruta, formulario, campos, conToken, accept)` | "archivo propio; `FormularioHttp` sin cambios" | `FormularioHttp.EnviarFormularioAsync(cliente, ruta, campos, conToken)` |
  | Lista de secciones construidas (2.1 T19) | `RutasYaConstruidas` | `SeccionesConstruidas` | "el mismo criterio" |
  | Doble de la carrera contra un índice único | — | `InterceptorQueAdelantaOtraAlta` | `InterceptorQueSeAdelanta` (misma idea) |

- **Por qué importa:** las tres tienen un "si ya existe, usá ése". Pero un validador de stories que
  mire `files_to_create` marca a la 3.1 como incompleta si no crea `ActorDeSesion.cs`. Y 3.1/3.2 no
  atrapan `AccesoDenegadoException`: si el caso de uso la lanza (ver I-3), un navegador ve
  `Algo falló.` con un 500 en lugar del cartel. Es la lección nº 1 del sprint 1 (contratos que no
  coinciden), repetida dentro del mismo sprint.
- **Corrección:** la 2.2 es dueña de las cuatro primeras piezas, con sus nombres. La 3.1 saca
  `ActorDeSesion.cs` de `files_to_create` y su código, y usa `Desde`. La 3.1 y la 3.2 usan
  `catch (Exception e) when (ManejadorGlobalDeErrores.EsEsperable(e))` + `EstadoPara(e)`, y
  `FormularioHttp.EnviarAsync`. El nombre de la lista es `RutasYaConstruidas`. Opcional: un solo
  interceptor parametrizado por el SQL que inserta.

### I-2 · 2.2 vs 3.1/3.2 — Dos carpetas para las páginas del administrador

- **Dónde:** 2.2 `Components/Personal/Administracion/UsuariosPersonal.razor`; 3.1
  `Components/Personal/Admin/Especialidades.razor`; 3.2 `Components/Personal/Admin/*.razor` y
  modifica la de la 2.2 por su ruta.
- **Problema:** las tres son pantallas del mismo rol bajo `/personal/admin/`. La 3.2 lo nota y elige
  `Admin/` "salvo que el docente unifique".
- **Por qué importa:** dos espacios de nombres para lo mismo y un `@using` más en `_Imports.razor`
  (3.2 agrega el de `Admin`). Cada story futura de administración (4.3, 7.1, 9.1) tendría que elegir.
- **Corrección:** una sola carpeta. Recomendado `Administracion/`, en castellano completo como
  `Secciones/`, que nace primero (2.2). Cambiar 3.1 y 3.2 (frontmatter, Task 5, Implementación,
  `_Imports.razor`).

### I-3 · 2.2 vs 3.1/3.2 — El permiso en el caso de uso no se mide igual

- **Dónde:** 2.2 AC 15 y "Decisiones" (lee de la **base** que el actor sea administrador activo,
  porque la sesión atrasa hasta 5 minutos); 3.1 AC 13 y 3.2 AC 20 (sólo `actor.Rol`, que sale del
  claim de la cookie).
- **Problema:** un administrador desactivado sigue con su cookie hasta 5 minutos (2.1 AC 25). En ese
  rato, la 2.2 le niega todo; la 3.1 y la 3.2 le dejan crear y desactivar especialidades y dar de
  alta o editar médicos, y la auditoría lo registra como un administrador válido.
- **Por qué importa:** es el mismo hueco que la 2.2 explica y cierra, abierto en dos pantallas del
  mismo rol. Arq. §7.7 pide el control en el caso de uso justamente para no depender de la sesión.
- **Corrección:** (a) la 2.2 deja el control como servicio compartido (por ejemplo
  `IActorVigente.ExigirAdministradorActivoAsync(actor, ct)` en Infraestructura/Identidad) y la 3.1 y
  la 3.2 lo usan; sumar el caso "administrador desactivado en la base" a 3.1 T12 y 3.2 T15 (esas
  pruebas hoy usan un `UsuarioId` inventado: pasarían a crear un administrador real). O (b) dejar
  escrito en 3.1 y 3.2 que se acepta la ventana de 5 minutos. Recomendado (a): cuesta un método y
  dos casos de prueba.

### I-4 · 3.2 + 4.1 — `Medico.Id` es `int` por un argumento circular

- **Dónde:** 3.2 "Decisiones" ("la 4.1 escribe `int` en sus firmas"); 4.1 AC 23 y Task 1 ("`int`,
  como `Medico.Id` de la 3.2").
- **Problema:** cada una toma el `int` de la otra. El código usa `long` para toda entidad propia
  (`RegistroAuditoria`, `MensajeSaliente`), y la 3.1 y la 4.1 (`AgendaMedico`, `Franja`) también.
  El único origen real es el código descartable (`48f29a1`), que no es contrato.
- **Por qué importa:** queda un tipo distinto sólo para el médico, que referencian turnos, bloqueos y
  agendas (5.3, 7.1, 4.x). Cambiarlo después obliga a migrar columnas y FK; antes de programar es
  gratis.
- **Corrección:** decisión 15. Recomendado `long`: en 3.2 `Medico.Id`, `RutasPersonal.FichaMedico(long)`,
  ruta `{Id:long}`, T14 con `long.MaxValue`, T28 con `9223372036854775807`; en 4.1 `MedicoId`,
  firmas de `IConsultaAgendas` y columna `medico_id bigint`.

### I-5 · 3.2 — `FichaMedico` nombra a tres cosas distintas

- **Dónde:** 3.2 `IGestionMedicos.cs` (`record FichaMedico`), `FichaMedico.razor` (la clase del
  componente se llama `FichaMedico`) y `RutasPersonal.FichaMedico(int id)`.
- **Problema:** dentro de `FichaMedico.razor`, el tipo `FichaMedico` es el propio componente, no el
  record. `FichaMedico? ficha = await Gestion.ObtenerAsync(...)` no compila. Con el
  `@using HospitalTurnos.Aplicacion.Catalogo` que la 3.2 agrega a `_Imports.razor`, el nombre queda
  ambiguo también en otras páginas.
- **Por qué importa:** error seguro al compilar la página principal de la story. Se arregla rápido,
  pero el agente renombra a su gusto y la 4.4 (que agrega la agenda a esa ficha) hereda otro nombre.
- **Corrección:** renombrar el record a `DatosFichaMedico` (o la página a `PaginaFichaMedico`).
  Revisar lo mismo para `MedicoNuevo` (constante y componente: no chocan hoy, pero conviene
  `RutasPersonal.NuevoMedico`).

### I-6 · Transversal — El sprint pasó de 36 pruebas estimadas a 128 (3,6 veces)

- **Dónde:** 2.2 (8 → 31), 3.1 (6 → 23), 3.2 (8 → 31), 4.1 (14 → 43); plan v1.1, S2 "~12 h 30".
- **Problema:** en el sprint 1 la relación fue 2,3 veces. Parte del aumento son reglas que el PRD no
  pide: cambio de contraseña voluntario (2.2 AC 23), reactivar especialidades (3.1 AC 4, 11), aprobar
  sólo con fecha futura (4.1 AC 11), concurrencia optimista de la aprobación (4.1 AC 27, que es de la
  4.3), y el estado real en A1 (4.1 AC 30, sí pedido por el brief). Cada una está justificada, pero
  juntas cambian el tamaño.
- **Por qué importa:** a la velocidad medida en el sprint 1 por prueba **escrita** (11 h 45 / 77 ≈
  9 min), 128 pruebas dan unas 19–20 h, no 12 h 30. La proyección del plan (por prueba estimada)
  queda corta, y la acción A-5 de la retrospectiva (medir) va a mostrar un desvío que ya se ve hoy.
- **Corrección:** decidir qué agregados quedan (decisiones 6 y 11 de abajo, más 2.2 AC 23 y 4.1
  AC 27) y anotar en el plan que el S2 se espera más largo que su estimado. Sin cambiar el método de
  la retrospectiva: es un aviso para el docente, no un error de las stories.

### I-7 · 2.2 + 3.2 — FR-10 "un usuario médico queda vinculado a un médico" queda sin garantía

- **Dónde:** FR-10; 2.2 crea usuarios `Medico` sin vínculo; 3.2 AC 11–13 hace el vínculo opcional;
  3.2 AC 23 deja a la 4.2 y la 8.1 "decidir qué mostrar".
- **Problema:** se puede tener, sin límite de tiempo, un usuario médico que entra a M1/M2 y no es
  ningún médico. Ninguna story dice qué ve. A4 tampoco lo señala: la columna "Médico vinculado"
  queda vacía igual para un recepcionista que para un médico sin vincular.
- **Por qué importa:** FR-10 lo dice como hecho, no como opción. Es el primer paso de RU-07 (Lucía da
  de alta a la Dra. Méndez y le crea el usuario).
- **Corrección:** decisión 17. Recomendado: mantener el vínculo opcional en la 3.2 (los dos pasos son
  pantallas distintas), pero (a) en A4, para usuarios `Medico` sin vínculo, mostrar `Sin vincular`
  en la columna (3.2 AC 14 y T31); y (b) escribir en el contrato de la 3.2 el texto que deben mostrar
  la 4.2 y la 8.1: `Tu usuario no está vinculado a un médico. Pedíselo al administrador.`

### I-8 · 3.1 + 3.2 — Las obligaciones que heredan la 5.3 viven sólo en estas stories

- **Dónde:** 3.1 AC 17 (cuatro obligaciones para la 5.3) y T15 (prueba-trampa); 3.2 Decisión abierta 3
  (bloquear el cambio de especialidad con turnos futuros).
- **Problema:** la 5.3 se escribe en el sprint 4. El único aviso automático es T15, que busca una
  entidad con `ClrType.Name == "Turno"`. Si la 5.3 la llama `TurnoReservado`, o si los turnos entran
  por otra entidad, la trampa no salta y FR-12 queda a medias sin que nadie lo note. La obligación
  de la 3.2 no tiene ni trampa.
- **Por qué importa:** FR-12 ("desactivarlas si no tienen turnos futuros") se cumple hoy porque no hay
  turnos; la regla completa depende de que alguien lea dos stories viejas.
- **Corrección:** (a) T15 busca también cualquier tabla llamada `turnos` (`GetTableName()`), no sólo
  el nombre de la clase; (b) agregar al plan, en la fila de la 5.3, "hereda: 3.1 AC 17 y la decisión
  7 de la revisión del S2" (es un cambio del docente al plan, no de las stories).

---

## MENORES

| # | Story · lugar | Problema | Corrección |
|---|---|---|---|
| M-1 | 4.1 `AgendaMedicoConfiguracion` | La FK sombra `AgendaMedicoId` se crea por nombre, sin `IsRequired()`: EF la hace anulable y la columna `agenda_medico_id` admite `NULL`. Con `Cascade`, quitar una franja igual la borra, pero la base no impide una franja huérfana. | `.HasForeignKey("AgendaMedicoId").IsRequired()` y, en el AC 23, `NOT NULL`. |
| M-2 | 4.1 T41, T43 | "`LogTo` hacia `RegistroEnMemoria`": ese doble es un `ILogger<T>`; `LogTo` pide un `Action<string>`. | `LogTo(comandos.Add, [RelationalEventId.CommandExecuted])` con una `List<string>`. |
| M-3 | 4.1 frontmatter | `depends_on` no incluye la 2.2, aunque modifica `GestionMedicosEnBase` (3.2) y usa `DatosDeCatalogo`, que crea usuarios de Identity. | Agregar `"2.2"`. |
| M-4 | 3.2 AC 23, 4.1 AC 31 | AC de contrato sin fila de prueba (igual que M-5 del sprint 1). | Marcarlos "declarativos". |
| M-5 | 3.2 AC 21, T30 | La falta de token se prueba sólo en el alta; el POST de la ficha (edición) no. | Un caso más en T30. |
| M-6 | 3.2 edición | Dos administradores que editan el mismo médico: gana el último y el `antes` de su auditoría no es lo que había en la base. | Aceptarlo explícito, o `xmin` como la 4.1. |
| M-7 | 2.2, 3.1, 3.2, 4.1 + contrato | `AccionesAuditoria` queda con dos estilos: `turno.reservado` / `propuesta-agenda.enviada` (1.3) y `usuario-personal-creado` / `medico-creado` (S2). La 4.2/4.3 van a usar los de punto. A5 (9.1) filtra por tipo. | Fijar un estilo en el contrato (o un prefijo por entidad) antes de la 4.2. |
| M-8 | 3.1 vs 2.2 | Dos formas de leer el botón de cada fila: 2.2 un campo `Accion = "tipo:id"` (probado en su razonamiento); 3.1 dos parámetros `[SupplyParameterFromForm(Name = ...)]` que la propia story pide "verificar en el rojo". | Usar la forma de la 2.2 en A2. |
| M-9 | 3.1 AC 2 | `Normalizar` con `FormD` también le saca la tilde a la ñ: `Señal` y `Senal` dan la misma clave. Para especialidades es aceptable. | Decirlo en "Decisiones". |
| M-10 | 3.2 T26 | Levanta un contenedor PostgreSQL propio para una sola prueba (segundos de más en cada corrida y en la CI). | Aceptable; o probar el estado vacío con bUnit, como T22. |
| M-11 | 2.2 AC 20 | "Las rutas fuera de `/personal/` no se tocan" no tiene prueba. | Un caso en T28: `/salud` con la sesión obligada da `200`. |
| M-12 | 3.2 Given/Fixture T26 | Cita "como la T25 de la 2.1"; el patrón de base nueva está en `ArranquePersonalTests.cs:22` y `SembradorPersonalTests.cs:22`. | Corregir la cita. |

---

## Decisiones abiertas consolidadas

Las cuatro stories traen 15 decisiones; juntando las repetidas quedan **14**. La revisión agrega **3**
(15 a 17), que salen de los hallazgos.

| # | Decisión | Origen | Recomendada | ¿De acuerdo? |
|---|---|---|---|---|
| 1 | Cómo se ve un error de regla en una pantalla SSR | 2.2 DA-1, 3.1 D3 (y 3.2 lo hace sin preguntarlo) | Cartel en la propia página, con el código del manejador global y lo tipeado intacto | **Sí**, con el mecanismo de la 2.2 (`EsEsperable`/`EstadoPara`) para las tres (I-1). |
| 2 | Contraseña olvidada | 2.2 DA-2 | Story nueva corta: "generar otra temporal desde A4" | **Sí**, pero ponerla temprano en el S3: hoy quien olvida la clave sólo sale tocando la base. |
| 3 | Cambiar el rol de un usuario existente | 2.2 DA-3 | Sólo al alta | **Sí** (protege el vínculo de la 3.2). |
| 4 | Vencimiento de la contraseña temporal | 2.2 DA-4 | Sin vencimiento | **Sí** (el PRD no da un número). |
| 5 | FR-12: desactivar especialidades sin consultar turnos hoy | 3.1 D1 | Desactivar sin consultar, contrato con la 5.3 y prueba-trampa | **Sí**, con la trampa reforzada (I-8). |
| 6 | Reactivar especialidades | 3.1 D2 | Sumar "Reactivar" | **Sí**: sin eso, desactivar es irreversible y el nombre queda bloqueado. |
| 7 | Cambiar la especialidad de un médico con turnos futuros | 3.2 DA-3 | La 5.3 bloquea el cambio | **Sí**; anotarlo como herencia de la 5.3 (I-8). |
| 8 | Qué muestra A1 antes y después de la 4.1, y qué estado tiene una aprobada que todavía no rige | 3.2 DA-1 + 4.1 AC 22 | La 3.2 muestra `Sin agenda`; la 4.1 conecta el estado real en el mismo sprint; una aprobada futura da `Sin agenda` | **Sí con un matiz**: la 3.2 DA-1 queda resuelta por la 4.1 AC 30. Lo de la aprobada futura nadie lo decidió (C-1); se puede aceptar, pero la administradora va a ver `Sin agenda` semanas después de aprobar (RU-07: aprobada el 11/11, rige el 1/12). Alternativa barata: `Sin agenda (rige desde 01/12)`. |
| 9 | Formato de la matrícula | 3.2 DA-2 | Texto de 1 a 20 con letras, números, espacio, punto, guion y barra, en mayúsculas | **Sí**. |
| 10 | Agenda vigente y "Corregir agenda" en la ficha (UX A1) | 3.2 DA-4 | En la 4.4 | **Sí**. |
| 11 | Aprobar una propuesta cuya fecha de inicio ya pasó | 4.1 DA-1 | No: 409 y rechazarla | **Sí**; la 4.3 tiene que mostrarlo antes del botón, no sólo al fallar. |
| 12 | Franjas que no son múltiplo de la duración | 4.1 DA-2 | Aceptarlas y no ofrecer el sobrante | **Sí**. |
| 13 | Una aprobada nueva con fecha anterior a otra aprobada que todavía no empezó | 4.1 DA-3 | Regla de Arq. §6 tal cual y aviso en la 4.3 | **Sí**. |
| 14 | `HorariosDelDia` y `AtiendeEn` en `AgendaMedico` y no en `Medico` | 4.1 DA-4 | En `AgendaMedico`, y actualizar el diagrama de Arq. §6 | **Sí**. |
| 15 | Tipo de `Medico.Id` | esta revisión (I-4) | `long`, como el resto | Nueva. |
| 16 | Permiso del administrador en el caso de uso: ¿se mira en la base o alcanza la sesión? | esta revisión (I-3) | En la base, con un servicio que crea la 2.2 | Nueva. |
| 17 | Usuario médico sin vincular | esta revisión (I-7) | Vínculo opcional, pero A4 muestra `Sin vincular` y el contrato fija el texto de M1/M2 | Nueva. |

---

## Lo que salió bien

- Las cuatro verificaron el código real (lección nº 1 del sprint 1) y casi todas las anclas existen.
- La 2.2 encontró que Identity 10 exige un símbolo por defecto (`RequireNonAlphanumeric`), que la 2.1
  no nombraba, y que `ChangePasswordAsync` ya cambia el sello de seguridad.
- La 2.2 cierra "siempre queda un administrador" por construcción y prueba la carrera con un candado
  de PostgreSQL, sin contar filas de una base compartida.
- 3.1 y 3.2 prueban la carrera contra el índice único con un interceptor determinista, no con suerte.
- La 4.1 prueba la fecha local del hospital a las 23:30 (el error más probable de la regla S-31) y
  deja la regla "qué agenda rige" una sola vez, en el dominio.
- Ninguna story dejó un stub en producción: la 3.1 descartó explícitamente el "contador que devuelve 0".

---

## Decisiones del docente y correcciones aplicadas (2026-10-16)

### Decisiones elegidas

| # | Decisión | Elegido | Dónde quedó |
|---|---|---|---|
| 1 | Error de regla en pantalla SSR | Cartel en la página, con `EsEsperable`/`EstadoPara` de la 2.2 en A4, A2 y A1 | 2.2 AC 26; 3.1 AC 15; 3.2 "Manejo en las páginas" |
| 2 | Contraseña olvidada | Story nueva corta, temprano en el S3 | 2.2 "Decisiones tomadas"; falta sumarla al plan (docente) |
| 3 | Cambiar el rol de un usuario | Sólo al alta | 2.2 |
| 4 | Vencimiento de la temporal | Sin vencimiento | 2.2 |
| 5 | FR-12 sin consultar turnos hoy | Desactivar sin consultar, contrato con la 5.3 y trampa reforzada | 3.1 AC 10, 17, T15 |
| 6 | Reactivar especialidades | Sí | 3.1 AC 4, 11 |
| 7 | Especialidad de un médico con turnos futuros | La bloquea la 5.3 (herencia) | 3.2 AC 23; mensaje de la trampa 3.1 T15 |
| 8 | Estado de A1 y aprobada que todavía no rige | 3.2 muestra `Sin agenda`; la 4.1 conecta el estado real en el S2; una aprobada futura se ve `Sin agenda (rige desde dd/MM)` | 3.2 AC 4, 23, T22; 4.1 AC 22, 30, T32, T41, T43 y T44 (nueva) |
| 9 | Formato de la matrícula | 1 a 20: letras, números, espacio, punto, guion y barra, en mayúsculas | 3.2 AC 6, 7 |
| 10 | Agenda vigente y "Corregir agenda" en la ficha | En la 4.4 | 3.2 AC 23 |
| 11 | Aprobar con fecha de inicio pasada | No (409); la 4.3 lo muestra antes del botón | 4.1 AC 11, 31 |
| 12 | Franjas no múltiplo de la duración | Se aceptan, sin ofrecer el sobrante | 4.1 AC 15 |
| 13 | Aprobada nueva antes de otra aprobada futura | Regla de Arq. §6 tal cual; aviso en la 4.3 | 4.1 AC 20, 31 |
| 14 | `HorariosDelDia`/`AtiendeEn` en `AgendaMedico` | Sí; el diagrama de Arq. §6 se actualiza al cerrar el sprint (Winston) | 4.1 |
| 15 | Tipo de `Medico.Id` | `long` | 3.2 y 4.1 (`medico_id bigint`) |
| 16 | Permiso del administrador en el caso de uso | En la base, en cada cambio, con `IActorVigente` (lo crea la 2.2; lo reusan 3.1 y 3.2) | 2.2 AC 15, 26; 3.1 AC 13, T12, T21; 3.2 AC 20, T15 |
| 17 | Usuario médico sin vincular | Vínculo opcional; A4 muestra `Sin vincular`; texto fijo para 4.2 y 8.1 | 3.2 AC 14, 23, T31 |

### Cómo quedó cada hallazgo

- **C-1**: contrato único en el AC 23 de la 3.2, copiado por la 4.1: `EstadoAgendaMedico` y el nuevo `SituacionDeAgenda` (con `RigeDesde`) en `Dominio/Agendas/`, ambos creados por la 3.2; `EstadoDeAgenda.Calcular` pasa al dominio; `GestionMedicosEnBase` con cuatro parámetros en la 3.2 y la 4.1 agrega `IConsultaAgendas` al final; las pruebas lo resuelven del contenedor, así la 4.1 no las toca; el estado real lo conecta la 4.1 (AC 30); sin citas a decisiones inexistentes.
- **I-1**: la 2.2 es la única dueña de `ActorDeSesion.Desde`, `EsEsperable`/`EstadoPara`, `FormularioHttp.EnviarAsync`/`Estado`, `RutasYaConstruidas` y la ayuda nueva `UsuariosDePrueba`; 3.1 y 3.2 las reusan. El interceptor de carrera es uno solo, `Ayudas/InterceptorQueSeAdelanta` (3.1), parametrizado por el SQL.
- **I-2**: una sola carpeta, `Components/Personal/Administracion/`, para A4, A2 y A1.
- **I-3**: servicio `IActorVigente` (decisión 16); 3.1 T12 y 3.2 T15 suman el administrador desactivado y usan administradores reales.
- **I-4**: `Medico.Id` `long` en la 3.2 (rutas `{Id:long}`, T14 y T28 con el máximo de `long`) y en la 4.1 (`MedicoId`, firmas, `medico_id bigint`).
- **I-5**: registro `DatosFichaMedico` y `RutasPersonal.NuevoMedico`.
- **I-6**: los agregados quedan (decisiones 6 y 11; 2.2 AC 23 y 4.1 AC 27 se mantienen como estaban: no hubo decisión en contra). Conteos finales: 31 + 23 + 31 + 44 = 129 pruebas. Falta la nota del plan "el S2 se espera más largo que su estimado" (docente).
- **I-7**: decisión 17 (3.2 AC 14, 23 y T31).
- **I-8**: (a) T15 de la 3.1 salta por la clase `Turno` o por la tabla `turnos`, y su mensaje nombra la obligación de la 3.1 y la de la 3.2; la 3.2 la escribe como herencia en su AC 23. (b) La línea en la fila de la 5.3 del plan queda para el docente.
- **M-1**: FK `agenda_medico_id` con `IsRequired()` y `NOT NULL` en el AC 23 de la 4.1.
- **M-2**: `LogTo(comandos.Add, [RelationalEventId.CommandExecuted])` con una `List<string>` en T41 y T43.
- **M-3**: `"2.2"` en el `depends_on` de la 4.1.
- **M-4**: 3.2 AC 23 y 4.1 AC 31 marcados como declarativos.
- **M-5**: T30 de la 3.2 suma el POST de la ficha sin token.
- **M-6**: aceptado y escrito en "Decisiones" de la 3.2 (gana el último).
- **M-7**: estilo `<entidad>-<acción>` del S2 escrito en la 2.2 AC 25 y citado en la 3.2. Fijarlo en el contrato técnico antes de la 4.2 queda para el docente.
- **M-8**: A2 usa un solo formulario con `Accion = "tipo:id"`, como A4; T21 suma la acción no reconocida.
- **M-9**: la ñ escrita en "Decisiones" de la 3.1.
- **M-10**: aceptado y escrito en el Given/Fixture de la 3.2.
- **M-11**: T28 de la 2.2 suma `/salud` con la sesión obligada (`200`).
- **M-12**: la 3.2 cita `ArranquePersonalTests.cs:22` y `SembradorPersonalTests.cs:22`.

**Pendiente fuera de las stories (archivos que esta corrección no toca):** sumar al plan la story de
contraseña olvidada (dec. 2), la herencia de la 5.3 (I-8 b) y el aviso de tamaño del S2 (I-6); fijar
el estilo de acciones en el contrato técnico (M-7); actualizar el diagrama de Arq. §6 (dec. 14).

---

## Segunda vuelta (2026-10-16)

Alcance: sólo las correcciones de la primera vuelta. Se verificó (1) que cada corrección declarada en
"Cómo quedó cada hallazgo" esté en las stories; (2) que no haya contradicciones nuevas entre las
cuatro (tipos, firmas, carpetas, dueño único de cada archivo nuevo, orden 2.2 → 3.1 → 3.2 → 4.1,
migraciones); (3) conteos y ACs; (4) cada nombre del código existente citado, contra `main` en
`c3413cc`.

| Severidad | Cantidad |
|---|---|
| CRÍTICO | 0 |
| IMPORTANTE | 0 |
| MENOR | 2 (las dos corregidas en esta vuelta) |

**Lo que se confirmó**

- Las 21 correcciones (C-1, I-1 a I-8, M-1 a M-12) y las 17 decisiones están en el lugar que dice la
  tabla de arriba. No quedan restos de lo viejo: `EstadoAgenda` sin `Medico`, `Personal/Admin/`,
  `ComoActorAuditoria`, `EnviarFormularioAsync`, `SeccionesConstruidas`, `int id`, `{Id:int}`,
  `record FichaMedico`, constructor de dos o tres parámetros, "T25 de la 2.1" (búsqueda en las cuatro).
- Firmas iguales en las cuatro: `IActorVigente.ExigirAdministradorActivoAsync(actor, mensajeSiNoLoEs, ct)`;
  `GestionMedicosEnBase` con cuatro parámetros en la 3.2 y `IConsultaAgendas` quinto en la 4.1;
  `EstadoAgendaMedico` y `SituacionDeAgenda(EstadoAgendaMedico, DateOnly? RigeDesde)` en
  `Dominio/Agendas/` (los crea la 3.2) y `EstadoDeAgenda.Calcular` ahí mismo (la 4.1); la regla de la
  decisión 8 escrita igual en 3.2 AC 23 y 4.1 AC 22; `UsuariosDePrueba.CrearAsync(proveedor, roles, activo, nombre)`
  y `FormularioHttp.EnviarAsync(cliente, ruta, formulario, campos, conToken, accept)` usados con esa
  forma; `InterceptorQueSeAdelanta(cadena, sql)` en Aplicacion.Tests/Ayudas.
- Ningún archivo nuevo tiene dos dueños (cruce de los cuatro `files_to_create`); lo que una story
  modifica de otra va siempre después en el orden (3.2 → `UsuariosPersonal.razor`; 4.1 →
  `GestionMedicosEnBase.cs`). Una sola carpeta `Components/Personal/Administracion/`.
- Migraciones encadenadas: `GestionUsuariosPersonal` → `AgregarEspecialidades` → `AgregarMedicos`
  (FK a `especialidades` y `asp_net_users`) → `AgregarAgendas` (FK `bigint` a `medicos`); todas con
  `dotnet ef migrations add`, ninguna con `database update`.
- Conteos: 2.2 27 AC / 31 pruebas; 3.1 19 / 23; 3.2 24 / 31; 4.1 32 / 44 (129). Filas = declarado,
  ACs sin huecos, todo AC con prueba salvo los dos declarativos (3.2 AC 23, 4.1 AC 31).
- Nombres del código: todos los citados existen y en la línea citada (`HospitalDbContext:19-20, 29-33`,
  `AgregarBandejaDeSalida:47-56`, `IdentidadPersonal:193-197`, `AccesoDenegado.razor:21`,
  `Ingreso.razor:19-35, 45, 72, 75, 82`, `SembradorPersonal:63`, `FormularioHttp:104`
  (`CrearUsuarioAsync(app, roles, contrasena, …)`), `SesionYPermisosHttpTests:119` (`[Trait("AC","13")]`,
  33 casos), `IngresoPersonalIdentityTests:27-40`, `ServiciosDeInfraestructura:25`,
  `IIngresoPersonal:9`, `ArranquePersonalTests:22`, `SembradorPersonalTests:22`,
  `PersonalWebApplicationFactory(PostgresFixture)` con `admin1`/`Admin-2026xx`/`Lucía Pérez`,
  `RegistroEnMemoria<T> : ILogger<T>`, `LimpiezaDeBandeja`, las 9 + 3 constantes de
  `AccionesAuditoria`, `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3, y las anclas del proyecto guía).

**MENORES**

| # | Story · lugar | Problema | Corrección |
|---|---|---|---|
| M2-1 | 3.1 Given/Fixture T8 | `InterceptorQueSeAdelanta` se construye con `(string cadena, string sqlQueInserta)` (la firma que reusa la 3.2), pero el texto decía que el `INSERT` va "con parámetros": esa firma no tiene dónde recibirlos. | **Corregido en esta vuelta:** los valores van escritos en el propio SQL (son la marca de la prueba). |
| M2-2 | 4.1 Given/Fixture "Contextos" | Decía `ContextoDePrueba` "(ayuda de la 1.4)"; la creó la 1.3 (commit `cd201b5`, y así lo cita la 3.1). | **Corregido en esta vuelta:** "(ayuda de la 1.3)". |

**Veredicto:** limpia para implementar en el orden 2.2 → 3.1 → 3.2 → 4.1. Nada requiere una decisión
del docente. Siguen pendientes, fuera de las stories, los cinco puntos de "Pendiente fuera de las
stories" (plan, contrato técnico y Arq. §6).

## M-7, decidido después de la segunda vuelta (2026-10-16)

**Decisión del docente:** las acciones de auditoría se nombran `<entidad>.<acción>`, como las 9 de la
1.3. Quedó fijado en el contrato técnico §Auditoría. Las tres de la 2.1 (`ingreso-personal…`) quedan
como excepción anotada. En las stories se renombraron las 10 acciones del S2:
`usuario-personal.creado`, `.desactivado`, `.reactivado`, `.desbloqueado`, `.contrasena-cambiada`;
`especialidad.creada`, `.desactivada`, `.reactivada`; `medico.creado`, `.editado`.
