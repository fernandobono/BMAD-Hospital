---
epic: 1
story: "1.3"
title: "Registro de auditoría: tabla, escritura en la misma transacción, protección con permisos y disparador"
status: review
sprint: 1
priority: "P0"
approach: "[CREATE]"
depends_on: ["1.1", "1.2"]
driver: |
  R-12 / FR-35 / DOM-03 piden un registro que nadie pueda alterar; ADR-07 decide protegerlo en la
  base (permisos + disparador), no sólo en el código. Ver docs/04-arquitectura.md §7.5.
files_to_create:
  - src/HospitalTurnos.Dominio/Auditoria/RegistroAuditoria.cs
  - src/HospitalTurnos.Dominio/Auditoria/CanalAuditoria.cs
  - src/HospitalTurnos.Dominio/Auditoria/AccionesAuditoria.cs
  - src/HospitalTurnos.Aplicacion/Auditoria/IAuditoria.cs
  - src/HospitalTurnos.Aplicacion/Auditoria/ActorAuditoria.cs
  - src/HospitalTurnos.Infraestructura/Auditoria/AuditoriaEnBase.cs
  - src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/RegistroAuditoriaConfiguracion.cs
  - src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/AAAAMMDDHHMMSS_AgregarRegistroAuditoria.cs
  - tests/HospitalTurnos.Dominio.Tests/Auditoria/RegistroAuditoriaTests.cs
  - tests/HospitalTurnos.Aplicacion.Tests/PostgresFixture.cs
  - tests/HospitalTurnos.Aplicacion.Tests/ColeccionPostgres.cs
  - tests/HospitalTurnos.Aplicacion.Tests/Ayudas/ContextoDePrueba.cs
  - tests/HospitalTurnos.Aplicacion.Tests/Auditoria/AuditoriaEnBaseTests.cs
  - tests/HospitalTurnos.Aplicacion.Tests/Auditoria/ProteccionAuditoriaTests.cs
files_to_modify:
  - src/HospitalTurnos.Infraestructura/Persistencia/HospitalDbContext.cs
  - src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/HospitalDbContextModelSnapshot.cs
  - src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs
  - tests/HospitalTurnos.Aplicacion.Tests/HospitalTurnos.Aplicacion.Tests.csproj
  - tests/HospitalTurnos.Web.Tests/Arranque/RolesDeBaseTests.cs
---

# Story 1.3: Registro de auditoría: tabla, escritura en la misma transacción, protección con permisos y disparador

Status: review

## Story

Como hospital que tiene que responder ante la Ley 25.326 por cada turno dado, cancelado o bloqueado,
quiero que cada cambio deje un `RegistroAuditoria` guardado **en la misma transacción** del cambio y
que la base rechace cualquier intento de modificarlo o borrarlo: la aplicación no tiene permiso, y un
disparador rechaza `UPDATE`/`DELETE`/`TRUNCATE` de **cualquier** rol mientras esté activo,
para que el registro sea completo (no hay cambio sin registro ni registro sin cambio) e inalterable
aunque haya un error de código o alguien tenga la contraseña de la aplicación.

**Alcance de la garantía (corregido tras la revisión, hallazgo I-8):** el dueño de la tabla (`turnos_owner`)
podría desactivar el disparador o borrar la tabla. Por eso la garantía real es: la aplicación no puede
alterar el registro, y nadie lo altera *por accidente*. Contra un dueño malicioso protege una regla
operativa: en producción la contraseña del dueño **no vive en el contenedor de la aplicación**; las
migraciones se corren aparte (ver Dev Notes, "Qué garantiza el disparador y qué no").

Requisitos: FR-33, FR-35, DOM-03, R-12, ADR-07 (`docs/02-prd.md:217-219`, `docs/02-prd.md:141`,
`docs/04-arquitectura.md:247-251`, `docs/04-arquitectura.md:304`).

## Acceptance Criteria

### Entidad del dominio

1. **Given** datos válidos (`Quien`, `Rol`, `Accion`, `EntidadTipo`, `EntidadId` no vacíos y `Cuando`
   con offset cero) **When** se construye un `RegistroAuditoria` **Then** el objeto queda con esos
   valores y **no expone setters públicos** (las propiedades son `{ get; private set; }`).
2. **Given** `Quien`, `Rol`, `Accion`, `EntidadTipo` o `EntidadId` nulos, vacíos o sólo espacios **When** se
   construye un `RegistroAuditoria` **Then** lanza `ArgumentException` con el nombre del parámetro
   (`paramName`) que falló. *(modo de falla de AC 1)*
3. **Given** un `Cuando` con offset distinto de cero (p. ej. `-03:00`) **When** se construye un
   `RegistroAuditoria` **Then** lanza `ArgumentException` (`paramName = "cuando"`): las marcas de
   auditoría van en UTC (contrato §Base de datos, arquitectura §7.8) y Npgsql rechaza `timestamptz`
   con offset ≠ 0 recién al guardar, lejos del error real. *(modo de falla de AC 1)*

### Escritura en la misma transacción

4. **Given** un `HospitalDbContext` conectado con `ConnectionStrings:App` (rol `turnos_app`) y un
   `FakeTimeProvider` fijo en `2026-10-12T11:00:00Z` **When** se llama
   `IAuditoria.Registrar(actor, AccionesAuditoria.TurnoReservado, "Turno", "T-123", new { turnoId = "T-123" })`
   y luego `SaveChangesAsync()` **Then** la tabla `registros_auditoria` tiene exactamente una fila con
   `entidad_id = 'T-123'`, `cuando = 2026-10-12 11:00:00+00`, `canal = 'Recepcion'` (texto, no número),
   los demás campos del actor y `detalle` como `jsonb` con la clave `turnoId`.
5. **Given** un `Registrar(...)` **When** **no** se llama `SaveChangesAsync()` y se descarta el
   contexto **Then** no queda ninguna fila: `Registrar` agrega al contexto y **no guarda** (contrato §Auditoría).
   *(modo de falla de AC 4: quien olvida guardar no deja un registro huérfano)*
6. **Given** dos `Registrar(...)` en el mismo contexto, el segundo con `accion` de 101 caracteres
   (la columna admite 100) **When** se llama `SaveChangesAsync()` **Then** lanza `DbUpdateException`
   (Postgres `22001`) **And** no queda **ninguna** de las dos filas: todo lo que va en el mismo
   `SaveChangesAsync` se confirma o se descarta junto. *(modo de falla de AC 4 — ver decisión de diseño en Dev Notes)*

### Protección en la base

7. **Given** una fila de auditoría existente **When** el rol `turnos_app` ejecuta `UPDATE … WHERE id = @id`,
   `DELETE … WHERE id = @id` o `TRUNCATE registros_auditoria` **Then** Postgres responde
   `42501 insufficient_privilege` y la fila sigue igual.
8. **Given** una fila de auditoría existente y los disparadores activos **When** el rol `turnos_owner`
   (dueño de la tabla, con todos los permisos) ejecuta `UPDATE`, `DELETE` o `TRUNCATE` sobre ella **Then**
   el disparador lo rechaza con `P0001` y un mensaje que contiene `registros_auditoria es de sólo inserción`,
   y la fila sigue igual. *(El disparador frena a cualquier rol mientras esté activo; no impide que el dueño
   lo desactive: ver "Qué garantiza el disparador y qué no".)*
9. **Given** la base de pruebas creada con el mismo `01-roles.sh` de la story 1.1 (privilegios por defecto
   activos: sobre `__EFMigrationsHistory`, que crea el mismo dueño, `turnos_app` **sí** tiene `UPDATE`)
   **When** se aplican las migraciones **Then** `turnos_app` tiene sobre `registros_auditoria` **exactamente**
   los privilegios `INSERT` y `SELECT` (ni uno más) y la tabla tiene los dos disparadores
   `auditoria_sin_update_ni_delete` y `auditoria_sin_truncate`. *(modo de falla: privilegios por defecto que
   se cuelan; la verificación previa demuestra que el `REVOKE` de la migración es lo que los saca)*

### No-regresion

10. (declarativo) Las pruebas de las stories 1.1 y 1.2 siguen pasando (`dotnet test` completo en verde). Lo controla R1.
11. (declarativo) Ningún archivo de `src/` fuera de `RelojHospital.cs` y de `Migraciones/` lee la hora del sistema (NFR-06): `Cuando` sale de `TimeProvider.GetUtcNow()`. Lo controla R2 (la prueba T5 de la story 1.2, que recorre todo `src/`).

### Tests (8 verificaciones + 2 de regresión)

| # | Test | AC | Nivel | Escenario | Resultado esperado |
|---|------|----|-------|-----------|--------------------|
| T1 | `Constructor_CamposObligatoriosVacios_LanzaArgumentException` | 1,2 | unit | `[Theory]` con `quien`, `rol`, `accion`, `entidadTipo`, `entidadId` en `null`, `""` y `"  "`; además un caso válido que verifica los valores | `ArgumentException` con `ParamName` correcto; el caso válido conserva los valores |
| T2 | `Constructor_CuandoConOffsetDistintoDeCero_LanzaArgumentException` | 3 | unit | `cuando = 2026-10-12T08:00:00-03:00` | `ArgumentException`, `ParamName == "cuando"` |
| T3 | `Registrar_SinGuardar_NoPersisteNada` | 5 | integration | `Registrar` con `EntidadId` único (Guid), se descarta el contexto sin guardar | `SELECT count(*) … WHERE entidad_id = @id` = 0 |
| T4 | `Registrar_ConSaveChanges_PersisteElRegistroCompletoComoTurnosApp` | 4 | integration | Contexto con `CadenaApp`, `FakeTimeProvider` fijo, `Registrar` + `SaveChangesAsync` | 1 fila; `cuando`, actor, `canal = 'Recepcion'`, `accion`; `detalle` parseado con `JsonDocument` tiene `turnoId` |
| T5 | `SaveChangesAsync_UnaFilaDelMismoGuardadoFalla_NoQuedaNingunRegistro` | 6 | integration | Dos `Registrar` con el mismo `EntidadId` único; el segundo con `accion` de 101 caracteres | `DbUpdateException` (`PostgresException.SqlState == "22001"`); count = 0 |
| T6 | `ModificarRegistro_ComoTurnosApp_RechazadoPorPermisos` | 7 | integration | `[Theory]` UPDATE / DELETE / TRUNCATE con `NpgsqlConnection(CadenaApp)` sobre una fila recién insertada | `PostgresException.SqlState == "42501"`; la fila sigue con los mismos valores |
| T7 | `ModificarRegistro_ComoTurnosOwner_RechazadoPorDisparador` | 8 | integration | `[Theory]` UPDATE / DELETE / TRUNCATE con `NpgsqlConnection(CadenaDuenio)` sobre una fila recién insertada | `SqlState == "P0001"`, `MessageText` contiene `registros_auditoria es de sólo inserción`; la fila sigue igual |
| T8 | `Migracion_RegistrosAuditoria_SoloOtorgaInsertYSelectATurnosApp` | 9 | integration | **Verificación previa (hallazgo C-1):** `has_table_privilege('turnos_app', '"__EFMigrationsHistory"', 'UPDATE')`; después, `information_schema.role_table_grants`, `has_table_privilege('turnos_app', 'registros_auditoria', 'UPDATE')` y `pg_trigger` (todo como `CadenaDuenio`) | Previa = `true` (si da `false`, la fixture no reproduce los privilegios por defecto y la prueba falla con ese mensaje); privilegios de `turnos_app` sobre `registros_auditoria` = {`INSERT`, `SELECT`}; `UPDATE` = `false`; disparadores = {`auditoria_sin_update_ni_delete`, `auditoria_sin_truncate`} |
| R1 | Todas las pruebas de las stories 1.1 y 1.2 (`--filter "Story=1.1\|Story=1.2"`) | 10 | regresión | Se vuelven a correr sin cambios después de agregar la migración | Siguen en verde |
| R2 | `CodigoFuente_ArchivosDeSrc_NoLeenLaHoraDelSistema` (story 1.2, T5) | 11 | unit | Se vuelve a correr sin cambios con los archivos nuevos de `Auditoria/` | Sigue pasando (0 apariciones) |

Traits: cada test nuevo lleva `[Trait("Story", "1.3")]` y un `[Trait("AC", "n")]` por AC de su fila. R1 y R2 conservan sus Traits de 1.1/1.2; se corren con `--filter "Story=1.1|Story=1.2"`.

#### Given/Fixture

- **`PostgresFixture` (todas las de integración)** — es la **única** especificación de la fixture, la del
  contrato técnico (§Pruebas) y la story 1.1 (§Given/Fixture): `postgres:17`, base `hospital_turnos`,
  el mismo `docker/postgres/initdb/01-roles.sh` copiado con `WithResourceMapping`, propiedades
  `CadenaDuenio` y `CadenaApp`, migraciones aplicadas como dueño. Esta story **no** escribe una fixture
  propia: copia la de `tests/HospitalTurnos.Web.Tests/PostgresFixture.cs` a
  `tests/HospitalTurnos.Aplicacion.Tests/PostgresFixture.cs` **idéntica salvo el `namespace`** (en el
  sprint 1 no hay proyecto de utilidades compartido). Las clases de prueba llevan `[Collection("Postgres")]`
  (definida en `ColeccionPostgres.cs`, fuera de la fixture para que la copia siga idéntica).
- **Contextos** — `ContextoDePrueba.Crear(string cadena)` (en `Ayudas/`) arma
  `new HospitalDbContext(new DbContextOptionsBuilder<HospitalDbContext>().UseNpgsql(cadena).Options)`
  (ajustar a la firma real del constructor que dejó 1.1, Task 1). T3–T5 usan `fixture.CadenaApp`.
- **T4** — Actor: `new ActorAuditoria(UsuarioId: "u-1", Quien: "Ana Pérez", Rol: "Recepcionista", Canal: CanalAuditoria.Recepcion)`.
  Reloj: `new FakeTimeProvider(new DateTimeOffset(2026, 10, 12, 11, 0, 0, TimeSpan.Zero))` con
  `SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Cordoba"))`.
  `EntidadId`: `$"T-{Guid.NewGuid():N}"` (los tests comparten la base y la tabla **no se puede truncar**:
  se filtra siempre por un `EntidadId` propio del test).
- **T6/T7** — La fila se inserta antes con `Registrar` + `SaveChangesAsync`; se lee su `id`. El `UPDATE` es
  `UPDATE registros_auditoria SET quien = 'X' WHERE id = @id`; el `DELETE` es `DELETE FROM registros_auditoria WHERE id = @id`.
  **Ojo:** un `DELETE` que no encuentra filas no dispara el disparador por fila y "pasa"; por eso siempre `WHERE id = @id` de una fila existente.
- **T8** — Verificación previa: `SELECT has_table_privilege('turnos_app', '"__EFMigrationsHistory"', 'UPDATE')`
  (el nombre lleva comillas dobles dentro del literal porque la tabla tiene mayúsculas). Después:
  `SELECT privilege_type FROM information_schema.role_table_grants WHERE grantee = 'turnos_app' AND table_name = 'registros_auditoria'`
  y `SELECT tgname FROM pg_trigger WHERE tgrelid = 'registros_auditoria'::regclass AND NOT tgisinternal`.

## Tasks / Subtasks

- [x] [SEQ] Task 1: Salvaguarda de estado (AC: todos)
  - [x] Confirmar que la story 1.1 está implementada: existen `src/HospitalTurnos.Infraestructura/Persistencia/HospitalDbContext.cs`, la carpeta `Persistencia/Migraciones/`, los proyectos `HospitalTurnos.Dominio.Tests` y `HospitalTurnos.Aplicacion.Tests`, y `src/HospitalTurnos.Api/` ya **no** existe.
  - [x] Anotar la firma real del constructor de `HospitalDbContext` y si `OnModelCreating` usa `ApplyConfigurationsFromAssembly` (decide si hay que registrar la configuración a mano).
  - [x] Confirmar que 1.1 crea los roles `turnos_owner` y `turnos_app` en el arranque de Docker Compose (sin esos roles el `GRANT` de la migración falla).
  - [x] Confirmar si `TimeProvider` ya está registrado (`RelojHospital`, story 1.2). Si **no** está: las pruebas igual corren (usan `FakeTimeProvider`), pero `AuditoriaEnBase` no se resuelve en la app. **DETENERSE** y avisar; no registrar `TimeProvider.System` como parche (viola NFR-06).
  - [x] Confirmar que existe `tests/HospitalTurnos.Web.Tests/PostgresFixture.cs` (story 1.1) con `CadenaDuenio`, `CadenaApp`, `01-roles.sh` vía `WithResourceMapping` y migraciones como dueño (contrato §Pruebas). Si no coincide → DETENERSE.
  - [x] Si `tests/HospitalTurnos.Aplicacion.Tests/PostgresFixture.cs` ya existe, comparar con la de Web.Tests (`diff` ignorando la línea `namespace`): tiene que ser idéntica. **No** extenderla ni agregarle métodos: lo propio de cada story va en `Ayudas/` o en la clase de prueba.
  - [x] Archivo de solución: el contrato dice `HospitalTurnos.sln`; el 2026-10-09 el repo tenía `HospitalTurnos.slnx`. Usar el que haya dejado 1.1.
  - [x] Si CUALQUIER asunción no coincide → DETENERSE → corregir la story primero.
- [x] [SEQ] Task 2: Dominio (AC: 1, 2, 3)
  - [x] `CanalAuditoria { Web, Recepcion, Personal, Sistema }` con el significado de cada valor en comentarios (ver Dev Notes).
  - [x] `RegistroAuditoria` con constructor validado, setters privados y constructor sin parámetros `private` para EF.
  - [x] `AccionesAuditoria` con las constantes de FR-33 (ver Dev Notes).
  - [x] T1, T2 en `Dominio.Tests`.
- [x] [SEQ] Task 3: Puerto y adaptador (AC: 4, 5)
  - [x] `ActorAuditoria` (record) e `IAuditoria` en Aplicacion.
  - [x] `AuditoriaEnBase : IAuditoria` en Infraestructura: toma `Cuando` de `TimeProvider.GetUtcNow()`, serializa `detalle` con `JsonSerializerOptions.Web` y hace `Add` sin guardar.
  - [x] Registrar `servicios.AddScoped<IAuditoria, AuditoriaEnBase>()` en `AgregarInfraestructura` (`ServiciosDeInfraestructura.cs`), al lado del reloj. `Program.cs` no cambia.
- [x] [SEQ] Task 4: Persistencia y migración (AC: 4, 6, 7, 8, 9)
  - [x] `RegistroAuditoriaConfiguracion` (largos, `jsonb`, enum como texto; **sin** `ToTable`: el nombre `registros_auditoria` sale de la convención de 1.1) y `DbSet<RegistroAuditoria> RegistrosAuditoria` en `HospitalDbContext`.
  - [x] `dotnet ef migrations add AgregarRegistroAuditoria …` (comando en Dev Notes) y agregar al final de `Up` el SQL de permisos y disparadores; al principio de `Down`, el SQL inverso.
- [x] [SEQ] Task 5: Fixture e integración (AC: 4-9)
  - [x] Copiar `PostgresFixture` de Web.Tests (idéntica salvo el `namespace`), crear `ColeccionPostgres` (`[CollectionDefinition("Postgres")] … : ICollectionFixture<PostgresFixture>`) y `Ayudas/ContextoDePrueba.cs`.
  - [x] En `HospitalTurnos.Aplicacion.Tests.csproj`: `<None Include="..\..\docker\postgres\initdb\01-roles.sh" Link="initdb/01-roles.sh" CopyToOutputDirectory="PreserveNewest" />` (igual que Web.Tests). Los paquetes ya están.
  - [x] T3-T5 en `AuditoriaEnBaseTests`, T6-T8 en `ProteccionAuditoriaTests`.
- [x] [SEQ] Task 6: Verificación (AC: 10, 11)
  - [x] Correr los comandos de "Como verificar que funciona"; contar 8 pruebas con `Story=1.3` (T1, T6 y T7 son `[Theory]`: el runner muestra más casos, pero son 8 métodos) y R1, R2 en verde.

### Task Dependencies

T1 → T2 → T3 → T4 → T5 → T6 (secuencial: la migración necesita la entidad; las pruebas de integración necesitan la migración).

## Dev Notes

### Que existe hoy

#### En hospital-turnos (verificado 2026-10-15, con 1.1 y 1.2 en `main`)

- `HospitalTurnos.slnx` lista los 7 proyectos de la 1.1 (4 de `src/` y 3 de `tests/`); el código descartable de `Api` ya no existe.
- `HospitalDbContext(DbContextOptions<HospitalDbContext>)` en `src/HospitalTurnos.Infraestructura/Persistencia/`. **No** usa `ApplyConfigurationsFromAssembly`: `OnModelCreating` sólo llama `ConvencionSnakeCase.Aplicar(modelBuilder)` al final. La configuración de auditoría se aplica a mano **antes** de esa llamada.
- Migraciones en `Persistencia/Migraciones/` (hoy sólo `Inicial`); las aplica `AplicadorDeMigraciones` con la cadena `Migraciones` (dueño) al arrancar.
- `AgregarInfraestructura` (`src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs`) registra el `DbContext` con la cadena `App` y `RelojHospital` como `TimeProvider` singleton (1.2). `Program.cs` sólo llama a ese método: `IAuditoria` se registra **ahí**, no en `Program.cs`.
- `docker/postgres/initdb/01-roles.sh` crea `turnos_owner` y `turnos_app` (1.1).
- `tests/HospitalTurnos.Web.Tests/PostgresFixture.cs` y `ColeccionPostgres.cs` (1.1): la fixture que esta story copia.
- `HospitalTurnos.Aplicacion.Tests.csproj` ya tiene `Testcontainers.PostgreSql` y `Microsoft.Extensions.TimeProvider.Testing`; le falta el enlace a `01-roles.sh`.
- **No existe (lo crea esta story):** todo lo de `Auditoria/` en las tres capas, la tabla `registros_auditoria`, el disparador, la copia de `PostgresFixture` en Aplicacion.Tests.

#### Contrato que esta story implementa (`docs/stories/00-contrato-tecnico.md` §Auditoría y §Pruebas)

Campos de `RegistroAuditoria`: `Id` (long), `Cuando` (`DateTimeOffset` UTC), `UsuarioId` (string, nullable),
`Quien`, `Rol`, `Canal` (`CanalAuditoria`), `Accion` (constantes en `AccionesAuditoria`), `EntidadTipo`,
`EntidadId`, `Detalle` (jsonb). `IAuditoria.Registrar(...)` agrega sin guardar. Nombres fijados en el
contrato: `ActorAuditoria`, `AuditoriaEnBase`.

`CanalAuditoria { Web, Recepcion, Personal, Sistema }` (decisión tras la revisión, hallazgo I-1):

| Valor | Significa |
|---|---|
| `Web` | el paciente, por internet |
| `Recepcion` | cambios de turnos hechos por la recepción (FR-33 "web o recepción"; CE-01 compara `Web` contra éste) |
| `Personal` | acciones del personal fuera de los turnos (p. ej. ingreso al sistema, story 2.1) |
| `Sistema` | procesos automáticos (p. ej. la bandeja de salida) |

### Patron de referencia verificado

**Patron (parcial)**: lo que el proyecto guía de la materia sí resuelve se copia; lo que no tiene (Postgres, disparadores, permisos por rol) se crea acá por primera vez.

| Proyecto | Ancla `archivo:linea` (o comando) | Que se verifico exactamente |
|---|---|---|
| changomas-testing-csharp | `src/ChangoMas.Infraestructura/RepositorioVentasSqlite.cs:46-48` y `:88` | Abrí el archivo: `BeginTransactionAsync` → varios `INSERT` → `CommitAsync`; comentario "o se guarda la venta con todos sus items, o no se guarda nada". Es la misma garantía que T5 prueba sobre `SaveChangesAsync` (EF abre la transacción por nosotros). |
| changomas-testing-csharp | `tests/ChangoMas.Tests.Integracion/RepositorioSqliteTests.cs:7-17` y `:24-27` | Abrí el archivo: prueba de integración contra una base **real**, con el comentario de por qué un mock no alcanza, y una base por test para que sean independientes. Acá la base es compartida (contenedor caro) y la independencia se logra filtrando por un `EntidadId` único — la tabla no se puede truncar justamente por esta story. |
| changomas-testing-csharp | `tests/ChangoMas.Tests.Integracion/CajaWebApplicationFactory.cs:20` y `:28-32` | Abrí el archivo: fixture `IAsyncLifetime` que levanta la dependencia externa en `InitializeAsync` y la libera en `DisposeAsync`. `PostgresFixture` sigue la misma forma. |
| changomas-testing-csharp | `tests/ChangoMas.Tests.Integracion/ApiCajaTests.cs:18` | Abrí el archivo: `IClassFixture<CajaWebApplicationFactory>` para compartir la fixture. Acá se usa `ICollectionFixture` para que 1.3 y 1.4 compartan **un** contenedor. |
| changomas-testing-csharp | `grep -rniE "Testcontainers\|TimeProvider\|CREATE TRIGGER\|disparador" --include='*.cs' --include='*.csproj' --include='*.sql' .` | 0 resultados (corrido 2026-10-09). El proyecto guía usa SQLite y no tiene disparadores, permisos por rol ni reloj inyectado. |

**No aplica para**: disparador de sólo inserción, `REVOKE`/`GRANT` por rol y fixture de Testcontainers. Busqué en el proyecto guía (grep de arriba, 0 resultados) y no hay plugins hermanos (contrato §Patrón de referencia). Esta story crea el patrón por primera vez; 9.1 (consulta de auditoría) y toda story que audite lo reutilizan.

### Decision de diseño: cómo se prueba la atomicidad sin casos de uso

En el sprint 1 no hay ninguna entidad de negocio. Opciones evaluadas:

- **Elegida — `IAuditoria` + `SaveChangesAsync` directo, con una segunda fila que la base rechaza (T5).**
  Lo que esta story aporta es: (a) `Registrar` no guarda solo (T3) y (b) el registro viaja en el
  mismo `SaveChangesAsync` que cualquier otro cambio, y ese guardado es todo-o-nada (T5). Para (b)
  da igual si la fila que falla es un turno o un segundo registro: Postgres revierte la transacción entera.
  Ventajas: cero código de producción para pruebas, y la base de prueba es **exactamente** la de las
  migraciones (importa: los permisos y el disparador viven en la migración).
- **Descartada — entidad de prueba sólo en el proyecto de tests.** Obliga a abrir un constructor
  `protected` en `HospitalDbContext` para heredarlo y a crear la tabla de prueba fuera de las migraciones:
  la base de prueba deja de ser la real, y el código de producción cambia para servir a una prueba.
- **Obligación hacia adelante:** cada story que escriba un cambio de negocio auditado (5.3, 5.4, 6.3, 7.2, 4.3…) incluye **su** prueba "si el guardado falla, no queda ni el cambio ni el registro" con la entidad real.

### Que hay que hacer (la delta)

1. **Dominio** (`src/HospitalTurnos.Dominio/Auditoria/`):
   ```csharp
   public enum CanalAuditoria
   {
       Web,        // el paciente, por internet
       Recepcion,  // cambios de turnos hechos por la recepción (CE-01 compara Web contra éste)
       Personal,   // acciones del personal fuera de los turnos (p. ej. ingreso al sistema)
       Sistema     // procesos automáticos
   }

   public sealed class RegistroAuditoria
   {
       public long Id { get; private set; }
       public DateTimeOffset Cuando { get; private set; }
       public string? UsuarioId { get; private set; }
       public string Quien { get; private set; } = null!;
       public string Rol { get; private set; } = null!;
       public CanalAuditoria Canal { get; private set; }
       public string Accion { get; private set; } = null!;
       public string EntidadTipo { get; private set; } = null!;
       public string EntidadId { get; private set; } = null!;
       public string? Detalle { get; private set; }   // JSON ya serializado → columna jsonb

       private RegistroAuditoria() { } // para EF

       public RegistroAuditoria(DateTimeOffset cuando, string? usuarioId, string quien, string rol,
           CanalAuditoria canal, string accion, string entidadTipo, string entidadId, string? detalle)
       {
           if (cuando.Offset != TimeSpan.Zero)
               throw new ArgumentException("La marca de auditoría tiene que estar en UTC.", nameof(cuando));
           ArgumentException.ThrowIfNullOrWhiteSpace(quien);
           ArgumentException.ThrowIfNullOrWhiteSpace(rol);
           ArgumentException.ThrowIfNullOrWhiteSpace(accion);
           ArgumentException.ThrowIfNullOrWhiteSpace(entidadTipo);
           ArgumentException.ThrowIfNullOrWhiteSpace(entidadId);
           // ... asignaciones
       }
   }
   ```
   `AccionesAuditoria` (`public static class`), una constante por acción de FR-33 (`docs/02-prd.md:217`):
   `TurnoReservado = "turno.reservado"`, `TurnoCancelado = "turno.cancelado"`, `AgendaBloqueada = "agenda.bloqueada"`,
   `BloqueoLevantado = "bloqueo.levantado"`, `PacienteMarcadoAvisado = "paciente.marcado-avisado"`,
   `PropuestaAgendaEnviada = "propuesta-agenda.enviada"`, `PropuestaAgendaAprobada = "propuesta-agenda.aprobada"`,
   `PropuestaAgendaRechazada = "propuesta-agenda.rechazada"`, `AgendaCorregida = "agenda.corregida"`.
2. **Aplicación** (`src/HospitalTurnos.Aplicacion/Auditoria/`):
   ```csharp
   public sealed record ActorAuditoria(string? UsuarioId, string Quien, string Rol, CanalAuditoria Canal);

   public interface IAuditoria
   {
       /// Agrega el registro al contexto SIN guardar: lo persiste el SaveChangesAsync del caso de uso,
       /// así el cambio y su registro se guardan juntos o no se guarda ninguno (arquitectura §7.5).
       void Registrar(ActorAuditoria actor, string accion, string entidadTipo, string entidadId, object? detalle = null);
   }
   ```
   El actor lo arma quien llama. Cómo se obtiene el actor de la sesión es de la story 2.1 (no de ésta).
3. **Infraestructura** — `AuditoriaEnBase(HospitalDbContext contexto, TimeProvider reloj) : IAuditoria`:
   `Cuando = reloj.GetUtcNow()`; `Detalle = detalle is null ? null : JsonSerializer.Serialize(detalle, JsonSerializerOptions.Web)`;
   `contexto.RegistrosAuditoria.Add(new RegistroAuditoria(...))`. **Nunca** llama `SaveChanges`.
4. **Configuración EF** (`RegistroAuditoriaConfiguracion : IEntityTypeConfiguration<RegistroAuditoria>`):
   **sin** `ToTable` (la convención `snake_case` de 1.1 ya da `registros_auditoria` a partir del `DbSet`, 1.1 AC 8); `Id` identity; largos máximos `UsuarioId` 100, `Quien` 200, `Rol` 50,
   `Accion` 100, `EntidadTipo` 100, `EntidadId` 100 (todos `IsRequired()` salvo `UsuarioId`);
   `Canal` con `HasConversion<string>().HasMaxLength(20)` (legible en un registro legal);
   `Detalle` con `HasColumnType("jsonb")`; `Cuando` es `DateTimeOffset` → `timestamptz`.
   Agregar `public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();` a `HospitalDbContext`.
5. **Migración** `AgregarRegistroAuditoria`: lo que genera EF + este SQL al final de `Up` (ver Implementación concreta).
6. **`PostgresFixture`** copiada de la de 1.1 (ver Given/Fixture), `ColeccionPostgres`, `ContextoDePrueba` y las pruebas T1-T8.

### Implementacion concreta

**SQL de la migración (al final de `Up`, con `migrationBuilder.Sql(...)`):**

```sql
-- Por qué REVOKE ALL primero: si la story 1.1 configuró ALTER DEFAULT PRIVILEGES para turnos_app,
-- la tabla nace con UPDATE/DELETE. Revocar todo y otorgar sólo lo necesario no depende de eso.
REVOKE ALL ON TABLE registros_auditoria FROM turnos_app;
GRANT SELECT, INSERT ON TABLE registros_auditoria TO turnos_app;
-- Inofensivo si la columna es identity; necesario si alguien la cambia a serial.
DO $$ BEGIN
  EXECUTE format('GRANT USAGE ON SEQUENCE %s TO turnos_app',
                 pg_get_serial_sequence('registros_auditoria', 'id'));
END $$;

-- Los permisos frenan a la aplicación; el disparador rechaza UPDATE/DELETE/TRUNCATE de cualquier rol
-- mientras esté activo, también del dueño (ADR-07). El dueño podría desactivarlo: por eso su contraseña
-- no vive en el contenedor de la aplicación en producción (ver "Qué garantiza el disparador y qué no").
CREATE FUNCTION rechazar_cambios_auditoria() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  RAISE EXCEPTION 'registros_auditoria es de sólo inserción: % no permitido', TG_OP
    USING ERRCODE = 'P0001';
END $$;

CREATE TRIGGER auditoria_sin_update_ni_delete
  BEFORE UPDATE OR DELETE ON registros_auditoria
  FOR EACH ROW EXECUTE FUNCTION rechazar_cambios_auditoria();

-- TRUNCATE no dispara los disparadores por fila: necesita uno por sentencia.
CREATE TRIGGER auditoria_sin_truncate
  BEFORE TRUNCATE ON registros_auditoria
  FOR EACH STATEMENT EXECUTE FUNCTION rechazar_cambios_auditoria();
```

`Down` (antes del `DropTable` que genera EF):
```sql
DROP TRIGGER IF EXISTS auditoria_sin_truncate ON registros_auditoria;
DROP TRIGGER IF EXISTS auditoria_sin_update_ni_delete ON registros_auditoria;
DROP FUNCTION IF EXISTS rechazar_cambios_auditoria();
```

**Fixture de las pruebas de integración**: esta story **no** define una `PostgresFixture` propia. Copia la
de la story 1.1 (contrato §Pruebas) tal cual, y agrega sólo dos archivos chicos:

```csharp
// tests/HospitalTurnos.Aplicacion.Tests/ColeccionPostgres.cs
// Un solo contenedor para todo el proyecto: arrancarlo cuesta segundos. La colección además hace que
// las clases que la usan corran de a una (no en paralelo) contra la misma base.
[CollectionDefinition("Postgres")]
public sealed class ColeccionPostgres : ICollectionFixture<PostgresFixture>;

// tests/HospitalTurnos.Aplicacion.Tests/Ayudas/ContextoDePrueba.cs
public static class ContextoDePrueba
{
    // Ajustar a la firma real del constructor de HospitalDbContext que dejó 1.1 (Task 1).
    public static HospitalDbContext Crear(string cadena) =>
        new(new DbContextOptionsBuilder<HospitalDbContext>().UseNpgsql(cadena).Options);
}
```

Por qué importa que sea la misma fixture (hallazgo C-1 de la revisión): una fixture que crea los roles con
SQL propio no aplica los privilegios por defecto de `01-roles.sh`. Con ella, T6 y T8 pasarían aunque se
borrara el `REVOKE ALL` de la migración, y las pruebas de otras stories que usan `CadenaApp` sobre tablas
creadas por el dueño fallarían con `42501`. La verificación previa de T8 deja esto controlado.

Paquetes en `HospitalTurnos.Aplicacion.Tests.csproj`: `Testcontainers.PostgreSql` y
`Microsoft.Extensions.TimeProvider.Testing` ya están (1.1). Falta sólo el `<None Include=… 01-roles.sh …>` de Task 5.

**Comando de la migración:**
```bash
cd ~/SourceCode/hospital-turnos
dotnet ef migrations add AgregarRegistroAuditoria \
  --project src/HospitalTurnos.Infraestructura \
  --startup-project src/HospitalTurnos.Web \
  --output-dir Persistencia/Migraciones
```

### Archivos a crear/modificar

| Accion | Archivo |
|--------|---------|
| Crear | `src/HospitalTurnos.Dominio/Auditoria/RegistroAuditoria.cs` |
| Crear | `src/HospitalTurnos.Dominio/Auditoria/CanalAuditoria.cs` |
| Crear | `src/HospitalTurnos.Dominio/Auditoria/AccionesAuditoria.cs` |
| Crear | `src/HospitalTurnos.Aplicacion/Auditoria/IAuditoria.cs` |
| Crear | `src/HospitalTurnos.Aplicacion/Auditoria/ActorAuditoria.cs` |
| Crear | `src/HospitalTurnos.Infraestructura/Auditoria/AuditoriaEnBase.cs` |
| Crear | `src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/RegistroAuditoriaConfiguracion.cs` |
| Crear | `src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/AAAAMMDDHHMMSS_AgregarRegistroAuditoria.cs` (prefijo lo pone `dotnet ef`) |
| Crear | `tests/HospitalTurnos.Dominio.Tests/Auditoria/RegistroAuditoriaTests.cs` (T1, T2) |
| Crear | `tests/HospitalTurnos.Aplicacion.Tests/PostgresFixture.cs` (copia idéntica de la de Web.Tests, salvo el `namespace`) |
| Crear | `tests/HospitalTurnos.Aplicacion.Tests/ColeccionPostgres.cs` (`[CollectionDefinition("Postgres")]`) |
| Crear | `tests/HospitalTurnos.Aplicacion.Tests/Ayudas/ContextoDePrueba.cs` |
| Crear | `tests/HospitalTurnos.Aplicacion.Tests/Auditoria/AuditoriaEnBaseTests.cs` (T3, T4, T5) |
| Crear | `tests/HospitalTurnos.Aplicacion.Tests/Auditoria/ProteccionAuditoriaTests.cs` (T6, T7, T8) |
| Modificar | `src/HospitalTurnos.Infraestructura/Persistencia/HospitalDbContext.cs` (agregar `RegistrosAuditoria`; aplicar la configuración si no hay `ApplyConfigurationsFromAssembly`) |
| Modificar | `src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/HospitalDbContextModelSnapshot.cs` (lo regenera `dotnet ef`) |
| Modificar | `src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs` (`AddScoped<IAuditoria, AuditoriaEnBase>()` en `AgregarInfraestructura`) |
| Modificar | `tests/HospitalTurnos.Aplicacion.Tests/HospitalTurnos.Aplicacion.Tests.csproj` (enlace a `01-roles.sh`; los paquetes ya están) |
| Modificar | `tests/HospitalTurnos.Web.Tests/Arranque/RolesDeBaseTests.cs` (T5 de la 1.1: deja de exigir una sola migración; aprobado por el docente) |

### Lo que NO hay que hacer

- **NO llamar `SaveChanges` dentro de `Registrar`**: rompería la garantía "cambio y registro juntos". Un registro guardado aparte puede quedar aunque el cambio falle (o al revés).
- **NO usar `DateTime.UtcNow` ni `DateTimeOffset.UtcNow`** para `Cuando`: NFR-06; sale de `TimeProvider`. Y **no** registrar `TimeProvider.System` si falta `RelojHospital` (Task 1).
- **NO guardar `Cuando` con offset −03:00**: Npgsql rechaza `timestamptz` con offset ≠ 0; por eso el constructor lo valida (AC 3).
- **NO otorgar `UPDATE`/`DELETE` "por si acaso"** ni confiar en los privilegios por defecto: el `REVOKE ALL` + `GRANT` explícito es lo que T8 controla.
- **NO hacer `TRUNCATE registros_auditoria` para limpiar entre pruebas**: el disparador lo impide (y está bien). Aislar por `EntidadId` único.
- **NO comparar el `detalle` como texto**: `jsonb` reordena claves y quita espacios. Parsear con `JsonDocument`.
- **NO crear una entidad ni una tabla "de prueba"** en el código de producción para T5 (ver decisión de diseño).
- **NO crear la pantalla ni la consulta de auditoría** (FR-34): es la story 9.1. Tampoco índices para esas búsquedas: los define 9.1 con sus consultas reales.
- **NO resolver el actor desde la sesión**: es de la story 2.1. Acá el actor llega como parámetro.
- **NO escribir una `PostgresFixture` propia** ni crear los roles con SQL en las pruebas: se copia la de 1.1, que usa `01-roles.sh` (hallazgo C-1). Tampoco agregarle métodos: dejaría de ser idéntica.
- **NO poner `ToTable(...)`** en la configuración: el nombre sale de la convención de 1.1 (AC 8 de 1.1).
- **NO prometer más de lo que el disparador da**: frena a cualquier rol mientras está activo, pero el dueño puede desactivarlo o borrar la tabla. Blindar contra el dueño o el superusuario (`ALTER TABLE … DISABLE TRIGGER`, `session_replication_role`) no es de esta story; esa protección es operativa (la contraseña del dueño fuera del contenedor de la aplicación en producción, NFR-08 / story 10.3).

**Reglas para las stories futuras** (nacen acá; cada story que toque estos temas las copia a su "Lo que NO hay que hacer"):

- **Ninguna migración modifica ni borra filas de `registros_auditoria`**, ni desactiva el disparador para hacerlo. Si un cambio de esquema necesitara "corregir" filas viejas, no se puede: se agrega una columna nueva con `DEFAULT` (DDL, no dispara nada) o se deja la corrección como un registro nuevo.
- **Ningún "limpiador" de bases de prueba** (Respawn, `TRUNCATE` de todas las tablas, borrado masivo) toca `registros_auditoria`: el disparador lo rechaza. Las pruebas se aíslan por un `EntidadId` propio.

### Qué garantiza el disparador y qué no

| Quién | ¿Puede alterar una fila? | Por qué |
|---|---|---|
| La aplicación (`turnos_app`), incluso con un error de código o con su contraseña robada | No | Sólo tiene `INSERT` y `SELECT` (AC 7, 9) |
| El dueño (`turnos_owner`) con un `UPDATE`/`DELETE`/`TRUNCATE` accidental | No | El disparador lo rechaza mientras esté activo (AC 8) |
| El dueño a propósito (desactivando el disparador o borrando la tabla) | Sí | Es el dueño. Por eso en producción su contraseña **no vive en el contenedor de la aplicación**: las migraciones se corren en un paso aparte y la app sólo tiene la de `turnos_app` |
| El superusuario de PostgreSQL | Sí | Es un tema operativo (NFR-08 / story 10.3) |

En desarrollo, el Docker Compose de 1.1 aplica las migraciones al arrancar la app con la cadena del dueño:
es cómodo y no hay datos reales. Lo que no puede pasar es que esa configuración llegue a producción.

Descartar el contenedor de pruebas o hacer `docker compose down -v` no dispara nada: tirar una base de
prueba entera no choca con el disparador.

### Salvaguarda de BD (obligatoria antes de implementar)

Con Docker Compose de 1.1 levantado:
```bash
cd ~/SourceCode/hospital-turnos
DB=$(docker compose exec -T db printenv POSTGRES_DB)
docker compose exec -T db psql -U postgres -d "$DB" -c "SELECT rolname FROM pg_roles WHERE rolname IN ('turnos_owner','turnos_app');"
# Esperado: las dos filas. Si falta alguna → DETENERSE (la migración no puede hacer GRANT a un rol inexistente).
docker compose exec -T db psql -U postgres -d "$DB" -c "SELECT to_regclass('registros_auditoria');"
# Esperado: vacío (NULL). Si ya existe → DETENERSE y revisar qué la creó.
```
(Si el superusuario del compose no se llama `postgres`, usar el de `POSTGRES_USER`: `docker compose exec -T db printenv POSTGRES_USER`.)

### Como verificar que funciona

```bash
cd ~/SourceCode/hospital-turnos
docker info > /dev/null && echo "docker OK"          # Testcontainers necesita Docker andando

dotnet build                                           # 0 errores, 0 advertencias (TreatWarningsAsErrors)
dotnet test --filter "Story=1.3"                       # 8 métodos de prueba en verde
dotnet test --filter "Story=1.3" --list-tests          # contar: T1..T8
dotnet test --filter "Story=1.3&AC=8"                  # sólo la protección contra el dueño
dotnet test --filter "Story=1.1|Story=1.2"             # R1 y R2: 1.1 y 1.2 siguen en verde (AC 10, 11)
dotnet test                                            # suite completa
diff <(grep -v '^namespace' tests/HospitalTurnos.Web.Tests/PostgresFixture.cs) \
     <(grep -v '^namespace' tests/HospitalTurnos.Aplicacion.Tests/PostgresFixture.cs) && echo "fixture idéntica"

# AC 11 (lo controla R2) — a mano, mismo criterio que 1.2 AC 6 (esperado: sólo RelojHospital.cs)
grep -rnE "DateTime(Offset)?\.(Now|UtcNow|Today)" src/ --include="*.cs" | grep -v "/Migraciones/"

# Prueba a mano contra la base de Docker Compose, después de aplicar la migración
DB=$(docker compose exec -T db printenv POSTGRES_DB)
docker compose exec -T db psql -U turnos_app   -d "$DB" -c "DELETE FROM registros_auditoria;"
# Esperado: ERROR:  permission denied for table registros_auditoria
docker compose exec -T db psql -U turnos_owner -d "$DB" -c "TRUNCATE registros_auditoria;"
# Esperado: ERROR:  registros_auditoria es de sólo inserción: TRUNCATE no permitido
```

## Dev Agent Record

### Agent Model Used

Claude Opus 5.5 (claude-opus-5-5), como Amelia (desarrolladora del equipo BMAD).

### Debug Log References

- Salvaguarda (Task 1): `Story=1.1|Story=1.2` 41/41 en verde antes de tocar nada; Docker andando; `dotnet-ef` 10.0.13 local; `01-roles.sh` con privilegios por defecto (SELECT/INSERT/UPDATE/DELETE) para `turnos_app`; `HospitalDbContext(DbContextOptions<HospitalDbContext>)` sin `ApplyConfigurationsFromAssembly`; `RelojHospital` registrado en `AgregarInfraestructura`.
- Rojos vistos (pruebas primero): (1) no compilaban, faltaban las clases; (2) con la entidad y sin migración, EF se negó a migrar (`PendingModelChangesWarning`): el modelo cambió y no hay migración; (3) con la migración y **sin** el SQL de protección: T3–T5 en verde, pero la app **pudo** hacer `UPDATE` y `DELETE` (privilegios por defecto), el dueño pudo todo y T8 falló. `TRUNCATE` como app ya fallaba (los privilegios por defecto no lo incluyen). (4) Con el SQL: verde.
- R1 rompió una prueba de la 1.1 (T5, `RolApp_TablaDeMigracionesDelDuenio_PuedeLeerla`): exigía **una sola** migración aplicada y ahora hay dos. No es un error de la app: la aserción quedaba vieja con cualquier migración nueva. Decisión del docente: hacerla robusta (todas las migraciones del proyecto, cada una una vez, con `_Inicial`). AC 10 decía "sin cambios": es la única excepción.
- `Id`: identity por convención de Npgsql (`IdentityByDefaultColumn`).
- Build: 0 advertencias (`-warnaserror`). Pruebas (`test-gate.sh` no reconoce `.slnx`, se corrió por proyecto): Dominio.Tests 20/20, Aplicacion.Tests 20/20, Web.Tests 28/28 (68/68; incluye las de 1.1 y 1.2).
- Cobertura de los archivos nuevos de `src/` (coverlet, a mano): 100% salvo `RegistroAuditoria.cs` 30/31 (el constructor privado para EF, que sólo se usa al leer con EF).
- Prueba en vivo contra el Compose reconstruido (el arranque aplicó la migración con el dueño): 11/11 AC observados. Una sonda fuera del repo (`DOTNET_STARTUP_HOOKS`, contenedor aparte con la misma imagen) usó la `IAuditoria` que resuelve la app; `psql` como `turnos_app` y `turnos_owner` mostró los rechazos por permisos y por disparador, y los privilegios exactos.
- Las filas de la prueba en vivo quedan en la base de desarrollo (no se pueden borrar: es la garantía); su `entidad_id` empieza con `T-vivo-`.

### Completion Notes List

- 8 métodos de prueba con `Story=1.3` (27 casos: T1 tiene 16, T6 y T7 tienen 3 cada uno).
- `IAuditoria` se registra como `Scoped` en `AgregarInfraestructura`; `Program.cs` no cambió.
- `PostgresFixture` de Aplicacion.Tests idéntica a la de Web.Tests salvo el `namespace` (verificado con `diff`). `ColeccionPostgres` también es copia.
- Pase adversarial: salteado por decisión del docente.

### File List

- Creados en `src/`: `HospitalTurnos.Dominio/Auditoria/{RegistroAuditoria,CanalAuditoria,AccionesAuditoria}.cs`, `HospitalTurnos.Aplicacion/Auditoria/{IAuditoria,ActorAuditoria}.cs`, `HospitalTurnos.Infraestructura/Auditoria/AuditoriaEnBase.cs`, `HospitalTurnos.Infraestructura/Persistencia/Configuraciones/RegistroAuditoriaConfiguracion.cs`, `HospitalTurnos.Infraestructura/Persistencia/Migraciones/20261016010013_AgregarRegistroAuditoria.cs` (+ `.Designer.cs`, generado).
- Modificados en `src/`: `HospitalTurnos.Infraestructura/Persistencia/HospitalDbContext.cs`, `HospitalTurnos.Infraestructura/Persistencia/Migraciones/HospitalDbContextModelSnapshot.cs` (regenerado), `HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs`.
- Creados en `tests/`: `HospitalTurnos.Dominio.Tests/Auditoria/RegistroAuditoriaTests.cs`; `HospitalTurnos.Aplicacion.Tests/{PostgresFixture,ColeccionPostgres}.cs`, `Ayudas/ContextoDePrueba.cs`, `Auditoria/{AuditoriaEnBase,ProteccionAuditoria}Tests.cs`.
- Modificados en `tests/`: `HospitalTurnos.Aplicacion.Tests/HospitalTurnos.Aplicacion.Tests.csproj` (enlace a `01-roles.sh`); `HospitalTurnos.Web.Tests/Arranque/RolesDeBaseTests.cs` (T5 de la 1.1, ver Change Log).
- Documentación: esta story y la tabla de pruebas de la 1.1 (T5).

### Change Log

- 2026-10-09: Story escrita (Mary, analista BMAD).
- 2026-10-09: Correcciones de la revisión adversarial (`revision-sprint-1.md`): C-1 (una sola `PostgresFixture`, copiada de 1.1; verificación previa de privilegios por defecto en T8), I-1 (`CanalAuditoria.Personal` y significado de cada canal), I-8 (garantía real del disparador, reglas para migraciones y limpiadores futuros), M-3 (sin `ToTable`), M-4 (`CadenaDuenio`), M-5 (R1, R2 para AC 10 y 11).
- 2026-10-15: Antes de implementar (a pedido del validador, aprobado por el docente): "Qué existe hoy" actualizado con 1.1 y 1.2 en `main`; `IAuditoria` se registra en `AgregarInfraestructura` (`ServiciosDeInfraestructura.cs` reemplaza a `Program.cs` en `files_to_modify`); los paquetes de pruebas ya estaban; estado del cuerpo alineado.
- 2026-10-15: Durante la implementación (aprobado por el docente): T5 de la 1.1 ajustada para no exigir una sola migración; `RolesDeBaseTests.cs` entra en `files_to_modify`.


## Review Follow-ups (Advisors)

<!-- tycon-pipeline: generado por Step 6/7. Cada item = finding advisor no resuelto (CRITICAL waived o WARNING). Al resolver el finding en el código, marcá [x]. Step 0/1 detecta los abiertos al retomar (pipeline.js followups-status). -->

- [x] **[CRITICAL]** ACCOV-C5 — `docs/stories/1-3-auditoria.md` — tycon-ac-coverage-checker: AC 10 lo asigna R1 (regresión declarativa con --filter Story=1.1|Story=1.2), no hay gap. <!-- fkey:ACCOV-C5|docs/stories/1-3-auditoria.md -->
- [x] **[CRITICAL]** ACCOV-C1 — `docs/stories/1-3-auditoria.md` — tycon-ac-coverage-checker: R1 no es un test con nombre (corre los de 1.1/1.2) y R2 existe en tests/HospitalTurnos.Dominio.Tests/Arquitectura/HoraDelSistemaTests.cs:12. <!-- fkey:ACCOV-C1|docs/stories/1-3-auditoria.md -->
