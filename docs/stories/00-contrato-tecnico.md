---
documento: Contrato técnico compartido de las stories
proyecto: Gestión de turnos — Hospital Municipal Las Acacias (caso simulado)
fecha: 2026-10-09
autor: Winston (arquitectura)
estado: vigente
version: 1.1
entrada: docs/04-arquitectura.md (v1.0)
cambios: v1.1 (2026-10-09) aplica la revisión adversarial del sprint 1 (C-1, I-1, I-7, I-8, I-9, I-15, I-16, M-4, M-11, M-15)
---

# Contrato técnico compartido

Nombres, ubicaciones y convenciones que **todas** las stories usan. Si una story necesita
apartarse, primero se cambia este archivo.

## Repositorio

- Ruta: `~/SourceCode/hospital-turnos` (git local, sin remoto todavía).
- Hoy existe código **descartable** del arranque: `src/HospitalTurnos.Api/` y
  `tests/HospitalTurnos.Tests/`, ya agregados a `HospitalTurnos.slnx`. La story 1.1 los borra y los
  saca de la solución. Nada de ese código se referencia desde las stories nuevas.
- Plataforma: .NET 10 (`net10.0`), SDK 10.0.400 instalado. `Nullable` y `ImplicitUsings` activos.
  `TreatWarningsAsErrors` activo en todos los proyectos de `src/`.

## Proyectos

| Proyecto | Ruta | Referencia a |
|---|---|---|
| `HospitalTurnos.Dominio` | `src/HospitalTurnos.Dominio/` | — |
| `HospitalTurnos.Aplicacion` | `src/HospitalTurnos.Aplicacion/` | Dominio |
| `HospitalTurnos.Infraestructura` | `src/HospitalTurnos.Infraestructura/` | Aplicacion, Dominio |
| `HospitalTurnos.Web` | `src/HospitalTurnos.Web/` (Blazor Web App) | Aplicacion, Infraestructura |
| `HospitalTurnos.Dominio.Tests` | `tests/HospitalTurnos.Dominio.Tests/` | Dominio |
| `HospitalTurnos.Aplicacion.Tests` | `tests/HospitalTurnos.Aplicacion.Tests/` | Aplicacion, Infraestructura |
| `HospitalTurnos.Web.Tests` | `tests/HospitalTurnos.Web.Tests/` | Web |

El proyecto E2E (`tests/HospitalTurnos.E2E/`) **no** se crea en el sprint 1.

## Paquetes (versiones fijadas en `Directory.Packages.props`, gestión central)

| Paquete | Dónde |
|---|---|
| `Npgsql.EntityFrameworkCore.PostgreSQL` 10.x | Infraestructura |
| `Microsoft.EntityFrameworkCore.Design` 10.x | Web (para migraciones) |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 10.x | Infraestructura |
| `MailKit` (última estable) | Infraestructura |
| `xunit` 2.9.x, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `coverlet.collector` | todos los tests |
| `Testcontainers.PostgreSql` (última estable) | Aplicacion.Tests, Web.Tests |
| `Microsoft.AspNetCore.Mvc.Testing` 10.x | Web.Tests |
| `Microsoft.Extensions.TimeProvider.Testing` 10.x (`FakeTimeProvider`) | todos los tests |
| `bunit` (última estable) | Web.Tests |

## Base de datos

- PostgreSQL 17, levantado con `docker compose up -d` (servicio `db`, puerto 5432 publicado sólo en
  `127.0.0.1`). Base: **`hospital_turnos`**.
- Dos roles: **`turnos_owner`** (dueño del esquema, corre migraciones) y **`turnos_app`** (con el que
  corre la aplicación). Cadenas de conexión: `ConnectionStrings:Migraciones` y `ConnectionStrings:App`.
  Los crea **un solo script**, `docker/postgres/initdb/01-roles.sh` (story 1.1), que usan el Compose y
  las pruebas: no hay dos versiones de los roles.
- `DbContext`: `HospitalDbContext` en `src/HospitalTurnos.Infraestructura/Persistencia/`.
- Migraciones EF Core en `src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/`. Las aplica el
  arranque de la app con `ConnectionStrings:Migraciones`; **nunca** `dotnet ef database update` (usaría
  `turnos_app`, que no tiene `CREATE`). Para generar migraciones sin base hay una fábrica de diseño
  (`FabricaDeContextoEnDiseno`, story 1.1).
- Tablas y columnas en `snake_case` (convención aplicada en `OnModelCreating`; no hace falta `ToTable`).
- Fechas de negocio (turnos, franjas): `DateTime` con `Kind=Unspecified` → `timestamp without time zone`
  (hora local del hospital, S-34). Marcas técnicas y de auditoría: `DateTimeOffset` UTC → `timestamptz`.

## Secretos (I-16)

- **Ninguna contraseña se versiona, ni siquiera las de desarrollo.** Ni en `docker-compose.yml`, ni en
  `appsettings*.json`, ni en el código de pruebas.
- Desarrollo con Compose: `.env` en la raíz (ya ignorado por `.gitignore`) + **`.env.ejemplo`**
  versionado con las mismas claves y valores de ejemplo. `docker-compose.yml` lee `.env` y exige cada
  contraseña con `${VARIABLE:?copiá .env.ejemplo a .env}` (sin valores por defecto): con un clon limpio
  sin `.env`, `docker compose up` se corta antes de crear nada con ese mensaje. Para empezar:
  `cp .env.ejemplo .env`.
- `dotnet run` fuera de Compose: las cadenas van en `dotnet user-secrets` del proyecto Web (fuera del
  repo). `appsettings.Development.json` no tiene cadenas de conexión.
- Pruebas: las contraseñas las genera la `PostgresFixture` al azar en cada corrida.
- Producción: variables de entorno del servidor. La contraseña del dueño **no** va al contenedor de la
  app (ver "Auditoría — garantía real del disparador").

## HTTPS — NFR-03

- El HTTPS lo termina el proxy inverso (Arq. §4). La app confía en `X-Forwarded-Proto`/`X-Forwarded-For`
  sólo si vienen de una red conocida: loopback (lo que trae ASP.NET Core por defecto) más las redes de
  `ProxyInverso:RedesConfiables` (lista CIDR, vacía por defecto).
- Dueños: **story 1.1** — `UseForwardedHeaders` (primer middleware) y, fuera de `Development`,
  `UseHsts` + `UseHttpsRedirection`. **Story 2.1** — cookie del personal con `Secure` siempre fuera de
  `Development`, y el hash lento de las contraseñas.

## Docker Compose (`docker-compose.yml` en la raíz)

| Servicio | Imagen | Puertos |
|---|---|---|
| `db` | `postgres:17` | `127.0.0.1:5432` |
| `mailpit` | `axllent/mailpit` | 1025 (SMTP), 8025 (web) |
| `web` | build de `src/HospitalTurnos.Web/Dockerfile` | 8080 |

Las contraseñas salen de `.env` (ver "Secretos"). Las claves de protección de datos de ASP.NET Core
viven en el volumen `claves-proteccion`, para que recrear el contenedor no invalide sesiones ni
formularios abiertos (M-11).

## Hora — NFR-06

- Toda la hora sale de `TimeProvider` inyectado. **Prohibido** `DateTime.Now`, `DateTime.UtcNow`,
  `DateTimeOffset.Now` fuera de `RelojHospital`.
- `RelojHospital : TimeProvider` en `src/HospitalTurnos.Infraestructura/Tiempo/RelojHospital.cs`,
  con `LocalTimeZone` = `America/Argentina/Cordoba`. Registrado como singleton.
- Extensión `AhoraLocal(this TimeProvider)` → `DateTime` local del hospital (Kind=Unspecified),
  en `src/HospitalTurnos.Aplicacion/Comun/TimeProviderExtensions.cs`.
- Tests: `FakeTimeProvider` con `SetLocalTimeZone` en Córdoba.

## Errores — NFR-09

- Excepciones del dominio en `src/HospitalTurnos.Dominio/Excepciones/`:
  `ReglaDeNegocioException` (→ 400), `ConflictoException : ReglaDeNegocioException` (→ 409),
  `NoEncontradoException` (→ 404), `AccesoDenegadoException` (→ 403).
- `ManejadorGlobalDeErrores : IExceptionHandler` en `src/HospitalTurnos.Web/Errores/`.
  Lo inesperado → 500 con mensaje genérico "Algo falló. Probá de nuevo en unos minutos." y el
  detalle sólo al registro técnico (`ILogger`).

## Auditoría — R-12, ADR-07

- Entidad `RegistroAuditoria` en `src/HospitalTurnos.Dominio/Auditoria/`: `Id` (long), `Cuando`
  (`DateTimeOffset` UTC), `UsuarioId` (string, nullable), `Quien` (texto legible), `Rol`, `Canal`
  (enum `CanalAuditoria`), `Accion` (string, constantes en `AccionesAuditoria`), `EntidadTipo`,
  `EntidadId`, `Detalle` (jsonb).
- **`CanalAuditoria { Web, Recepcion, Personal, Sistema }`** — por dónde entró la acción (I-1):

  | Canal | Significa | Ejemplos |
  |---|---|---|
  | `Web` | El paciente, por internet. | Saca o cancela su turno. |
  | `Recepcion` | Un cambio de **turnos** hecho por la recepción (FR-33 "web o recepción"). | Reserva o cancela un turno en la ventanilla. |
  | `Personal` | Una acción del personal **que no es un cambio de turno**. | Ingreso al sistema, bloqueo por intentos, administración de usuarios, agendas o catálogo. |
  | `Sistema` | Un proceso automático, sin persona. | Bandeja de salida, tareas programadas. |

  La métrica CE-01 ("turnos por la web") cuenta sólo acciones de turnos, canal `Web` sobre
  `Web` + `Recepcion`; `Personal` y `Sistema` no entran.
- Puerto `IAuditoria` en `src/HospitalTurnos.Aplicacion/Auditoria/`:
  `Registrar(ActorAuditoria actor, string accion, string entidadTipo, string entidadId, object? detalle = null)`
  **agrega** el registro al `HospitalDbContext` sin guardar; se persiste con el `SaveChangesAsync` del
  caso de uso. `ActorAuditoria` (record, mismo lugar): `UsuarioId?`, `Quien`, `Rol`, `Canal`.
  Implementación: `AuditoriaEnBase` en `src/HospitalTurnos.Infraestructura/Auditoria/` (story 1.3).
- **Nombre de las acciones** (decisión del docente, 2026-10-16, M-7 de la revisión del S2):
  `<entidad>.<acción>` en kebab-case, con la entidad antes del punto (`turno.reservado`,
  `usuario-personal.creado`, `especialidad.desactivada`, `medico.creado`). La 9.1 filtra por la entidad.
  Excepción anotada: las tres de la 2.1 (`ingreso-personal`, `ingreso-personal-fallido`,
  `ingreso-personal-bloqueado`) quedan como están.
- Tabla `registros_auditoria`: `turnos_app` tiene sólo `INSERT` y `SELECT`; un disparador rechaza
  `UPDATE`, `DELETE` y `TRUNCATE`.

### Auditoría — garantía real del disparador (I-8)

- **Qué protege:** contra la aplicación (`turnos_app` ni siquiera tiene el permiso) y contra un error
  del dueño (un `UPDATE`/`DELETE`/`TRUNCATE` accidental, en una migración o a mano).
- **Qué no protege:** el dueño de la tabla técnicamente puede deshabilitar el disparador
  (`ALTER TABLE … DISABLE TRIGGER`) o borrar la tabla. No es una defensa contra un dueño malicioso.
- **Por eso:** la contraseña de `turnos_owner` **no** debe estar en el contenedor de la app en
  producción. En producción las migraciones se corren como un **paso aparte** (bundle de EF o un
  contenedor de una sola corrida) y la app sólo recibe `ConnectionStrings:App`. En desarrollo se acepta
  que el contenedor `web` tenga las dos cadenas y migre al arrancar.
- Una migración futura que necesite un `UPDATE` de relleno sobre `registros_auditoria` deshabilita y
  vuelve a habilitar el disparador dentro de la misma migración, y lo justifica en un comentario.
- Las pruebas **no** usan limpiadores genéricos (Respawn, `TRUNCATE` de todas las tablas) que toquen
  `registros_auditoria`: filtran sus aserciones por un marcador propio.

## Bandeja de salida de emails — Arq. §7.4

- Entidad `MensajeSaliente` en `src/HospitalTurnos.Infraestructura/Email/`, tabla `mensajes_salientes`.
  Marcas `DateTimeOffset` UTC: `CreadoEl`, `ProximoIntentoEl`, `EnviadoEl?`, `FalloDefinitivoEl?` y
  **`ManejadoEl?`** (el manejador del fallo definitivo ya actuó: separa "el email se dio por perdido" de
  "ya se avisó", para reintentar el manejador sin reenviar el email).
- Puerto `IBandejaDeSalida` en `src/HospitalTurnos.Aplicacion/Email/`: `Encolar(MensajeEmail)` agrega
  sin guardar (misma transacción del caso de uso). Implementación: `BandejaDeSalidaEnBase`
  (Infraestructura/Email).
- `MensajeEmail` (record): `Para`, `Asunto`, `CuerpoTexto`, `CuerpoHtml`, `Tipo` (string, ej.
  `"codigo-ingreso"`), `ReferenciaId` (string, nullable).
- `ProcesadorBandejaDeSalida : BackgroundService`: cada 15 s toma pendientes. **1 intento + 3 reintentos
  (a 1, 5 y 15 min); al fallar el 4.º intento marca `FalloDefinitivoEl`** e invoca los
  `IManejadorFalloDefinitivo` registrados para ese `Tipo` (los usa la story 7.2), con un
  `FalloDefinitivoEmail(MensajeId, Tipo, ReferenciaId?, Para, Motivo)`.
- Puertos `IEnviadorEmail` e `IManejadorFalloDefinitivo` en `src/HospitalTurnos.Aplicacion/Email/`.
  `IEnviadorEmail` con implementación `EnviadorEmailSmtp` (MailKit), opciones `OpcionesSmtp` desde
  `Email:Smtp:*`. Sin valores en `appsettings.json`: si falta la configuración, el arranque falla
  (`ValidateOnStart`), igual que con las cadenas de conexión.

## Personal y roles — R-13

- `UsuarioPersonal : IdentityUser<Guid>` en `src/HospitalTurnos.Infraestructura/Identidad/`, con
  `NombreCompleto` y `Activo`.
- Roles como constantes en `src/HospitalTurnos.Dominio/Personal/Roles.cs`:
  `Recepcionista`, `Medico`, `Administrador`.
- **Cada usuario del personal tiene exactamente un rol** (PRD FR-09: "Cada usuario tiene un rol"). La
  gestión de usuarios (2.2) no permite asignar dos (M-15).
- Esquema de cookie del personal: `"Personal"`. El del paciente (sprint 5): `"Paciente"`.

## Pruebas

- Nombre: `Metodo_Escenario_Resultado` en PascalCase.
- Cada test lleva `[Trait("Story", "N.M")]` y un `[Trait("AC", "n")]` por AC cubierto.
- Los comentarios de código explican el **por qué**, en castellano, en la densidad del proyecto guía
  [changomas-testing-csharp](https://github.com/fernandobono/changomas-testing-csharp) (es material de clase).

### `PostgresFixture` — una sola especificación (C-1)

Integración contra PostgreSQL real con Testcontainers. No hay proyecto de utilidades compartido en el
sprint 1, así que la **misma clase** vive copiada en los dos proyectos que la necesitan; las dos copias
son idénticas salvo el `namespace`:

| Proyecto | Ruta | La crea |
|---|---|---|
| Web.Tests | `tests/HospitalTurnos.Web.Tests/PostgresFixture.cs` | story 1.1 |
| Aplicacion.Tests | `tests/HospitalTurnos.Aplicacion.Tests/Infraestructura/PostgresFixture.cs` | la primera story que la necesite (1.3), **copiando** la de Web.Tests |

- `public sealed class PostgresFixture : IAsyncLifetime`.
- Contenedor: `PostgreSqlBuilder` con imagen `postgres:17`, base **`hospital_turnos`**, usuario
  `postgres`. Contraseñas (`postgres`, `turnos_owner`, `turnos_app`) generadas al azar en cada corrida
  (`Guid.NewGuid().ToString("N")`); las de los roles se pasan como variables `TURNOS_OWNER_PASSWORD` y
  `TURNOS_APP_PASSWORD`.
- Roles: copia **`docker/postgres/initdb/01-roles.sh`** a `/docker-entrypoint-initdb.d/` con
  `WithResourceMapping`, en modo ejecutable (`0755`). El script llega a la carpeta de salida con
  `<None Include="..\..\docker\postgres\initdb\01-roles.sh" Link="initdb/01-roles.sh" CopyToOutputDirectory="PreserveNewest" />`
  (desde Aplicacion.Tests la ruta relativa es la misma). **Prohibido** crear los roles con SQL propio:
  sin los `ALTER DEFAULT PRIVILEGES`, `GRANT` y `REVOKE` del script, las pruebas no reproducen los
  permisos reales.
- Expone **`CadenaDuenio`** (`turnos_owner`) y **`CadenaApp`** (`turnos_app`), armadas con
  `NpgsqlConnectionStringBuilder` sobre la cadena del contenedor cambiando `Username`/`Password`.
- `InitializeAsync`: arranca el contenedor y aplica las migraciones **como dueño**
  (`AplicadorDeMigraciones.AplicarAsync(CadenaDuenio)`). `DisposeAsync`: dispone el contenedor.
- Colección xUnit: `[CollectionDefinition("Postgres")] public sealed class ColeccionPostgres :
  ICollectionFixture<PostgresFixture>;` en cada proyecto. **Toda** clase de prueba con base usa
  `[Collection("Postgres")]`: un solo contenedor por proyecto y ninguna clase en paralelo sobre la
  misma base. Una prueba que **detiene** el contenedor usa su propia instancia, fuera de la colección.
- Ayudas propias de una story (limpiar una tabla, crear un contexto) van en un archivo aparte, como
  métodos de extensión sobre `PostgresFixture`; la clase común no se edita.
- Sin limpiadores que toquen `registros_auditoria` (ver "Auditoría — garantía real del disparador").

### Otros nombres comunes de pruebas (M-4)

- Fábrica web: **`HospitalWebApplicationFactory`** (`tests/HospitalTurnos.Web.Tests/`, story 1.1). Inyecta
  las cadenas de la `PostgresFixture` como **última** fuente de configuración, para que
  `appsettings.Development.json`, `user-secrets` o variables de entorno no le ganen (I-15). Una story
  que necesite otra configuración **hereda** de ella; no escribe una fábrica desde cero.
- Doble del registro técnico: **`RegistroEnMemoria<T> : ILogger<T>`**, escrito a mano, en `Ayudas/`
  del proyecto que lo use. Para capturar todas las categorías en una prueba de integración:
  `ProveedorDeRegistroEnMemoria : ILoggerProvider` (story 1.2).

## Patrón de referencia del equipo

El patrón de referencia es el
**proyecto guía de la materia**, [changomas-testing-csharp](https://github.com/fernandobono/changomas-testing-csharp) (mismas capas, .NET 10):

- Capas `Dominio / Aplicacion / Infraestructura / Api` y puertos en `src/ChangoMas.Dominio/Puertos/`.
- Fábrica de pruebas de integración: `tests/ChangoMas.Tests.Integracion/CajaWebApplicationFactory.cs:20`
  (`WebApplicationFactory<Program>, IAsyncLifetime`, configuración por `UseSetting` en `ConfigureWebHost`, línea 34).
- CI por niveles de la pirámide: `.github/workflows/ci.yml` (compilar → unit → integración → SCA).
