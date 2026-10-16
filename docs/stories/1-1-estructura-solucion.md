---
epic: 1
story: "1.1"
title: "Estructura de la solución en 4 capas, con Docker Compose (app + PostgreSQL + Mailpit). Reemplaza el código inicial."
status: review
sprint: 1
priority: "P0"
approach: "[CREATE]"
depends_on: []
runtime_not_verifiable:
  - ac: 8
    reason: "La convención snake_case sólo se nota en una tabla, y en esta story el modelo no tiene ninguna entidad: la primera tabla llega con la story 1.3. Corriendo la app no hay nada que mirar."
    covered_by: T7
  - ac: 18
    reason: "Habla de la fábrica de pruebas (HospitalWebApplicationFactory), que sólo existe dentro de la suite de pruebas; la app en ejecución no la usa."
    covered_by: T11
files_to_create:
  - "Directory.Build.props"
  - "src/Directory.Build.props"
  - "Directory.Packages.props"
  - ".config/dotnet-tools.json"
  - ".gitattributes"
  - ".dockerignore"
  - ".env.ejemplo"
  - "docker-compose.yml"
  - "docker/postgres/initdb/01-roles.sh"
  - "scripts/smoke-compose.sh"
  - "src/HospitalTurnos.Dominio/HospitalTurnos.Dominio.csproj"
  - "src/HospitalTurnos.Aplicacion/HospitalTurnos.Aplicacion.csproj"
  - "src/HospitalTurnos.Infraestructura/HospitalTurnos.Infraestructura.csproj"
  - "src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/HospitalDbContext.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/ConvencionSnakeCase.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/AplicadorDeMigraciones.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/FabricaDeContextoEnDiseno.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/"
  - "src/HospitalTurnos.Web/HospitalTurnos.Web.csproj"
  - "src/HospitalTurnos.Web/Program.cs"
  - "src/HospitalTurnos.Web/Dockerfile"
  - "src/HospitalTurnos.Web/appsettings.json"
  - "src/HospitalTurnos.Web/appsettings.Development.json"
  - "src/HospitalTurnos.Web/Components/"
  - "src/HospitalTurnos.Web/Components/Pages/Home.razor"
  - "src/HospitalTurnos.Web/Salud/ChequeoBaseDeDatos.cs"
  - "tests/HospitalTurnos.Dominio.Tests/HospitalTurnos.Dominio.Tests.csproj"
  - "tests/HospitalTurnos.Dominio.Tests/Arquitectura/RaizDelRepo.cs"
  - "tests/HospitalTurnos.Dominio.Tests/Arquitectura/EstructuraDeLaSolucionTests.cs"
  - "tests/HospitalTurnos.Dominio.Tests/Arquitectura/SecretosTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/HospitalTurnos.Aplicacion.Tests.csproj"
  - "tests/HospitalTurnos.Aplicacion.Tests/Persistencia/ConvencionSnakeCaseTests.cs"
  - "tests/HospitalTurnos.Web.Tests/HospitalTurnos.Web.Tests.csproj"
  - "tests/HospitalTurnos.Web.Tests/PostgresFixture.cs"
  - "tests/HospitalTurnos.Web.Tests/ColeccionPostgres.cs"
  - "tests/HospitalTurnos.Web.Tests/HospitalWebApplicationFactory.cs"
  - "tests/HospitalTurnos.Web.Tests/Ayudas/FiltroIpRemota.cs"
  - "tests/HospitalTurnos.Web.Tests/Salud/SaludTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Arranque/ArranqueTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Arranque/RolesDeBaseTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Arranque/HttpsDetrasDelProxyTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Arranque/FabricaDePruebasTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Inicio/InicioTests.cs"
files_to_modify:
  - "HospitalTurnos.slnx"
---

# Story 1.1: Estructura de la solución en 4 capas, con Docker Compose (app + PostgreSQL + Mailpit). Reemplaza el código inicial.

Status: review

## Story

Como equipo que construye el sistema de turnos del Hospital Municipal Las Acacias,
quiero la solución armada en las 4 capas del contrato técnico (Dominio, Aplicación, Infraestructura, Web), con PostgreSQL de dos roles, Mailpit y la aplicación levantándose con un solo `docker compose up` (sin ninguna contraseña versionada) y lista para correr detrás del proxy HTTPS,
para que todas las stories siguientes se escriban sobre la arquitectura aprobada (Arq. §5) y no sobre el código inicial con SQLite, que no la sigue (Arq. §10).

## Acceptance Criteria

### Estructura de la solución

1. **Given** el repo con el código inicial (`src/HospitalTurnos.Api/` y `tests/HospitalTurnos.Tests/` en `HospitalTurnos.slnx`) **When** se completa la story **Then** esas dos carpetas no existen y `HospitalTurnos.slnx` lista exactamente estos 7 proyectos: `HospitalTurnos.Dominio`, `HospitalTurnos.Aplicacion`, `HospitalTurnos.Infraestructura`, `HospitalTurnos.Web`, `HospitalTurnos.Dominio.Tests`, `HospitalTurnos.Aplicacion.Tests`, `HospitalTurnos.Web.Tests`.
2. **Given** los 7 `.csproj` **When** se leen sus `ProjectReference` **Then** coinciden con la tabla "Proyectos" del contrato técnico: Dominio no referencia nada; Aplicacion → Dominio; Infraestructura → Aplicacion, Dominio; Web → Aplicacion, Infraestructura; Dominio.Tests → Dominio; Aplicacion.Tests → Aplicacion, Infraestructura; Web.Tests → Web. Ningún proyecto de `src/` referencia a `HospitalTurnos.Web`, y Dominio no referencia a ninguna otra capa.
3. (declarativo) `Directory.Build.props` (raíz) fija `TargetFramework=net10.0`, `Nullable=enable` e `ImplicitUsings=enable` para todos los proyectos. `src/Directory.Build.props` importa el de la raíz y agrega `TreatWarningsAsErrors=true` (sólo `src/`, como pide el contrato). `Directory.Packages.props` tiene `ManagePackageVersionsCentrally=true` y fija las versiones de **todos** los paquetes de la tabla "Paquetes" del contrato; ningún `.csproj` declara `Version` en un `PackageReference`. `dotnet build HospitalTurnos.slnx -c Release` termina con 0 advertencias y 0 errores.

### Base de datos y roles

4. **Given** el volumen de datos de PostgreSQL vacío **When** arranca el servicio `db` (o el contenedor de Testcontainers con el mismo script) **Then** `docker/postgres/initdb/01-roles.sh` crea `turnos_owner` (dueño de la base `hospital_turnos`, y por eso del esquema `public`) y `turnos_app` (con `CONNECT` y `USAGE` sobre `public`, sin `CREATE`), y deja privilegios por defecto para que toda tabla futura de `turnos_owner` en `public` dé a `turnos_app` `SELECT, INSERT, UPDATE, DELETE`, y toda secuencia futura `USAGE, SELECT`.
5. **Given** una conexión con `turnos_app` **When** intenta `CREATE TABLE prueba (id int)` en `public` **Then** PostgreSQL la rechaza con `SqlState = 42501` (insufficient_privilege). *(Modo de falla de AC 4: la app nunca puede cambiar el esquema.)*
6. **Given** `HospitalDbContext` sin entidades y la migración `Inicial` vacía **When** arranca `HospitalTurnos.Web` **Then** aplica las migraciones pendientes con `ConnectionStrings:Migraciones` (rol `turnos_owner`) **antes** de atender pedidos, `__EFMigrationsHistory` tiene la fila de `Inicial` y `turnos_app` la puede leer (`SELECT` concedido por los privilegios por defecto de AC 4). Volver a arrancar no falla (las migraciones son idempotentes).
7. **Given** que falta `ConnectionStrings:App` o `ConnectionStrings:Migraciones`, o que la base no responde al arrancar **When** arranca `HospitalTurnos.Web` **Then** el arranque se corta con `InvalidOperationException` cuyo mensaje nombra la cadena (`"Falta la cadena de conexión 'App'"` / `"Falta la cadena de conexión 'Migraciones'"` / `"No se pudieron aplicar las migraciones: la base no responde o rechazó al rol dueño (ConnectionStrings:Migraciones)."`). La app no queda atendiendo sin base. Las dos cadenas se validan con la configuración **final** (después de `Build()`), así que una cadena agregada por la fábrica de pruebas cuenta.
8. **Given** la convención `snake_case` de `HospitalDbContext.OnModelCreating` **When** se convierte un nombre PascalCase **Then** `RegistrosAuditoria → registros_auditoria`, `MensajesSalientes → mensajes_salientes`, `FalloDefinitivoEl → fallo_definitivo_el`, `CuerpoHtml → cuerpo_html`, `UsuarioId → usuario_id`, `Id → id`; y `OnModelCreating` la aplica a tablas, columnas, claves, claves foráneas e índices de toda entidad. **Contrato con 1.3 y 1.4**: la story 1.3 declara `DbSet<RegistroAuditoria> RegistrosAuditoria` y obtiene la tabla `registros_auditoria` (nombre del contrato técnico) sin configurar el nombre a mano; ídem 1.4 con `mensajes_salientes`. (Un `ToTable` explícito con el mismo nombre es redundante pero no rompe; uno con otro nombre sí contradice el contrato.)

### Endpoint de salud

9. **Given** la app arrancada y la base disponible **When** `GET /salud` **Then** responde `200 OK`, `Content-Type: application/json`, cuerpo `{"estado":"ok"}`, en menos de 3 s.
10. **Given** la app arrancada y la base caída **después** del arranque **When** `GET /salud` **Then** responde `503 Service Unavailable` con cuerpo `{"estado":"sin conexión con la base"}` en 5 s o menos (el chequeo tiene un tope de 3 s), y el cuerpo **no** contiene texto de la excepción (ni `Npgsql`, ni `Exception`, ni la cadena de conexión). *(NFR-09 aplicado al chequeo.)*

### Página de inicio

11. **Given** la app arrancada **When** `GET /` **Then** responde `200` HTML con `<html lang="es">`, un `<h1>` con el texto `Hospital Municipal Las Acacias` y el subtítulo `Gestión de turnos`. **And** las páginas de ejemplo de la plantilla no existen: `GET /counter` y `GET /weather` responden `404`.

### Docker Compose

12. **Given** un clon limpio con Docker y `cp .env.ejemplo .env` **When** `docker compose up -d --build` en la raíz **Then** quedan corriendo `db` (`postgres:17`, 5432 publicado sólo en `127.0.0.1`, con healthcheck `pg_isready` en estado healthy), `mailpit` (`axllent/mailpit`, 1025 SMTP y 8025 web) y `web` (build de `src/HospitalTurnos.Web/Dockerfile`, 8080, `depends_on: db: condition: service_healthy`); `GET http://localhost:8080/salud` → 200 y `GET http://localhost:8025` → 200 dentro de 60 s.
13. **Given** el compose levantado **When** `docker compose stop db` **Then** `GET http://localhost:8080/salud` → 503 sin detalles técnicos; **And** después de `docker compose start db`, sondeando cada 2 s, `/salud` vuelve a 200 **dentro de 30 s** sin reiniciar `web` (el primer chequeo puede fallar por conexiones rotas del pool: se espera por condición, no un pedido único).

### No-regresión

14. (declarativo) Nada de las stories nuevas referencia `HospitalTurnos.Api` (`grep -rn "HospitalTurnos.Api" src tests` → 0). El código inicial queda recuperable en la historia de git (ver Task 1) para reaprovechar `Medico.HorariosDelDia` en la story 4.1 (Arq. §10).

### Secretos — contrato técnico "Secretos" (I-4, I-16)

15. **Given** un clon limpio **sin** `.env` (y sin las variables de contraseña en el entorno) **When** `docker compose up -d --build` (o `docker compose config`) **Then** Compose se corta antes de crear ningún contenedor, con código ≠ 0, y el mensaje de error contiene `copiá .env.ejemplo a .env`. *(Modo de falla de AC 12: nunca arranca con contraseñas vacías ni inventadas.)*
16. **Given** los archivos versionados **When** se leen `docker-compose.yml`, `src/HospitalTurnos.Web/appsettings.json`, `src/HospitalTurnos.Web/appsettings.Development.json`, `.env.ejemplo` y `.gitignore` **Then** `docker-compose.yml` exige `POSTGRES_PASSWORD`, `TURNOS_OWNER_PASSWORD` y `TURNOS_APP_PASSWORD` con `${…:?…}` y no tiene ningún valor por defecto `${…:-…}`; ningún `appsettings*.json` contiene `ConnectionStrings` ni `Password`; `.env.ejemplo` define las tres variables; `.gitignore` ignora `.env`.

### HTTPS detrás del proxy — NFR-03 (I-7)

17. **Given** la app en un entorno que **no** es `Development`, con puerto HTTPS configurado (`https_port=443`) **When** llega `GET /salud` **Then**: (a) desde una red confiable (loopback, o una red de `ProxyInverso:RedesConfiables`) con `X-Forwarded-Proto: https` → 200 con encabezado `Strict-Transport-Security`; (b) desde una red confiable **sin** ese encabezado → 307 a `https://…/salud`; (c) desde una red **no** confiable con `X-Forwarded-Proto: https` → 307 (el encabezado falsificado se ignora). **And** en `Development` no hay redirección ni `Strict-Transport-Security`.

### Infraestructura de pruebas (I-14, I-15)

18. **Given** `HospitalWebApplicationFactory` en el entorno `Development` (que carga `appsettings.Development.json` y `user-secrets`) **When** se resuelve `HospitalDbContext` **Then** su cadena de conexión apunta al host y puerto del contenedor de la `PostgresFixture` (las cadenas de la fábrica son la última fuente de configuración y siempre ganan).
19. **Given** el compose levantado **When** se recrea el contenedor `web` (`docker compose up -d --force-recreate web`) **Then** el archivo de claves de protección de datos que existía antes sigue en `/claves-proteccion` (volumen `claves-proteccion`): sesiones y formularios abiertos sobreviven a un redespliegue (M-11).
20. **Given** `01-roles.sh` copiado **sin** permiso de ejecución (modo `0644`; el entrypoint de `postgres` lo **incluye** con `.` en vez de ejecutarlo) **When** arranca el contenedor con el volumen vacío **Then** los dos roles existen igual y `turnos_app` tiene `CONNECT`: el script no usa `set -u` ni corta el entrypoint (I-14).

### Tests (14 verificaciones)

> **Ajuste vs. la estimación (3 → 14):** el plan estimó 3 (salud, inicio, estructura). Se suman los modos de falla que la guía exige por cada happy path (base caída → 503, arranque sin base, rol sin permisos), la convención `snake_case` (contrato del que dependen 1.3 y 1.4), el smoke de compose y, por la revisión adversarial, secretos fuera del repo (T9, E2), HTTPS detrás del proxy (T10), la fábrica de pruebas que no se deja pisar la cadena (T11) y el script de roles incluido sin bit ejecutable (T12). Todas son baratas salvo las de Testcontainers, que comparten un solo contenedor por colección. La imagen Docker en CI la construye la story 1.5 (job `imagen`); el smoke completo de Compose (E1, E2) se corre a mano en esta story.

| # | Test | AC | Nivel | Escenario | Resultado esperado |
|---|------|----|-------|-----------|--------------------|
| T1 | `Salud_BaseDisponible_Devuelve200ConEstadoOk` | 9 | integration | App con `HospitalWebApplicationFactory` sobre el PostgreSQL de `PostgresFixture` | 200, JSON `{"estado":"ok"}` |
| T2 | `Salud_BaseCaidaDespuesDeArrancar_Devuelve503SinDetalles` | 10 | integration | Contenedor propio del test; la app arranca, se hace `StopAsync()` del contenedor, `GET /salud` | 503, `{"estado":"sin conexión con la base"}`, sin `Npgsql`/`Exception`, respuesta ≤ 5 s |
| T3 | `Inicio_Get_MuestraNombreDelHospitalYNoLasPaginasDeEjemplo` | 11 | integration | `GET /`, `GET /counter`, `GET /weather` | `/` 200 con `lang="es"`, `Hospital Municipal Las Acacias` y `Gestión de turnos`; los otros dos 404 |
| T4 | `Arranque_SinCadenaOBaseInaccesible_FallaConMensajeQueNombraLaCadena` | 7 | integration | `[Theory]` 3 casos: sin `App`; sin `Migraciones`; `Migraciones` apuntando a `Host=127.0.0.1;Port=1;Timeout=2` | `CreateClient()` lanza; la excepción (o su `InnerException`) es `InvalidOperationException` con el fragmento esperado de AC 7 |
| T5 | `RolApp_TablaDeMigracionesDelDuenio_PuedeLeerla` | 4, 6 | integration | `AplicadorDeMigraciones.AplicarAsync(CadenaDuenio)` dos veces; luego `SELECT "MigrationId" FROM "__EFMigrationsHistory"` como `turnos_app` | No falla la segunda vez; devuelve todas las migraciones del proyecto, cada una una vez, incluida la que termina en `_Inicial` (ajustado en la story 1.3: con la segunda migración, "una sola fila" dejó de valer) |
| T6 | `RolApp_CrearTabla_EsRechazadoPorPermisos` | 4, 5 | integration | Como `turnos_app`: `CREATE TABLE prueba (id int)` | `PostgresException` con `SqlState == "42501"` |
| T7 | `ASnakeCase_NombrePascalCase_DevuelveMinusculasConGuionBajo` | 8 | unit | `[Theory]` con los 6 pares de AC 8 | Cada par convierte exacto |
| T8 | `Solucion_ProyectosYReferencias_CumplenElContratoTecnico` | 1, 2, 3, 14 | unit | Lee `HospitalTurnos.slnx`, los 7 `.csproj`, `Directory.Packages.props` y `src/Directory.Build.props` desde la raíz del repo | 7 proyectos exactos; referencias iguales a la tabla del contrato; no existen las carpetas viejas; `ManagePackageVersionsCentrally=true`; `TreatWarningsAsErrors=true` en `src/`; ningún `PackageReference` con `Version` |
| T9 | `Repositorio_ArchivosVersionados_NoTienenContrasenas` | 16 | unit | Lee desde la raíz del repo los 5 archivos de AC 16 | Cada contraseña del compose con `:?` y ninguna con `:-`; ningún `appsettings*.json` con `ConnectionStrings` ni `Password` (sin distinguir mayúsculas); `.env.ejemplo` con las 3 variables; `.gitignore` con la línea `.env`. Si falla, el `Assert` dice qué archivo y qué línea |
| T10 | `Salud_DetrasDelProxy_RespetaHttpsSoloDeRedesConfiables` | 17 | integration | `[Theory]` 5 casos (entorno, IP remota, `X-Forwarded-Proto`, redes configuradas): (`Testing`, 127.0.0.1, `https`, —); (`Testing`, 127.0.0.1, sin encabezado, —); (`Testing`, 203.0.113.5, `https`, —); (`Testing`, 203.0.113.5, `https`, `203.0.113.0/24`); (`Development`, 127.0.0.1, sin encabezado, —) | 200 + `Strict-Transport-Security`; 307 a `https://turnos.lasacacias.example/salud`; 307; 200 + `Strict-Transport-Security`; 200 sin `Strict-Transport-Security` |
| T11 | `Fabrica_EntornoDevelopment_UsaLasCadenasDeLaFixture` | 18 | integration | `HospitalWebApplicationFactory` con `UseEnvironment("Development")`; en un scope, `HospitalDbContext.Database.GetConnectionString()` | `Host` y `Port` (leídos con `NpgsqlConnectionStringBuilder`) iguales a los de `fixture.CadenaApp`; `Username == "turnos_app"` |
| T12 | `ScriptDeRoles_SinBitEjecutable_CreaLosRolesIgual` | 20 | integration | Contenedor propio (no la colección) con `01-roles.sh` mapeado en modo `0644` | `SELECT count(*) FROM pg_roles WHERE rolname IN ('turnos_owner','turnos_app')` = 2; `has_database_privilege('turnos_app','hospital_turnos','CONNECT')` = true |
| E1 | `Smoke_DockerCompose_LevantaYSaludResponde` | 7, 12, 13, 19 | e2e | `scripts/smoke-compose.sh` con `.env` presente: `up -d --build`, espera `/salud` = 200 (tope 60 s), `web` sin `ConnectionStrings:App` termina en ≤ 30 s nombrando la cadena (Change Log 2026-10-15), Mailpit 8025 = 200, `stop db` → 503, `start db` → 200 sondeando (tope 30 s), anota el archivo de claves de `/claves-proteccion`, `up -d --force-recreate web`, espera `/salud` = 200 y comprueba que el archivo sigue | Sale con código 0 e imprime el nombre del test; con código ≠ 0 y el paso que falló si algo no responde |
| E2 | `Smoke_DockerComposeSinEnv_FallaConMensajeClaro` | 15 | e2e | Mismo script, primer paso: `env -u POSTGRES_PASSWORD -u TURNOS_OWNER_PASSWORD -u TURNOS_APP_PASSWORD docker compose --env-file /dev/null config` | Código ≠ 0 y la salida de error contiene `copiá .env.ejemplo a .env`; el script imprime `Smoke_DockerComposeSinEnv_FallaConMensajeClaro OK` |

Cada test xUnit lleva `[Trait("Story", "1.1")]` y un `[Trait("AC", "n")]` por AC cubierto (regla del contrato técnico). E1 y E2 no son xUnit: el script imprime `<nombre> OK` por cada uno.

#### Given/Fixture

- **PostgresFixture** (Web.Tests) — la especificación es la del contrato técnico §"Pruebas › `PostgresFixture`" (C-1): `postgres:17`, base `hospital_turnos`, contraseñas al azar por corrida, `01-roles.sh` copiado con `WithResourceMapping` en modo `0755`, `CadenaDuenio` y `CadenaApp`, migraciones aplicadas como dueño en `InitializeAsync`. Es **el mismo** script del compose: no hay dos versiones de los roles. Esta story crea la copia de Web.Tests; la de Aplicacion.Tests la copia la story 1.3.
- **ColeccionPostgres** — `[CollectionDefinition("Postgres")]`; T1, T3, T5, T6, T10 y T11 llevan `[Collection("Postgres")]` (un solo contenedor, sin clases en paralelo sobre la misma base).
- **HospitalWebApplicationFactory** — `WebApplicationFactory<Program>`, recibe la `PostgresFixture`. En `ConfigureWebHost`: `UseEnvironment("Testing")` (sobreescribible) y `ConfigureAppConfiguration(c => c.AddInMemoryCollection(...))` con `ConnectionStrings:App = CadenaApp` y `ConnectionStrings:Migraciones = CadenaDuenio`. **Por qué no `UseSetting`** (I-15): con `UseEnvironment("Development")` se cargan `appsettings.Development.json` y `user-secrets` **después** de esos valores y les pueden ganar; la fuente en memoria agregada por la fábrica es la última y gana siempre. Para "quitar" una cadena (T4) se la pone en `null` en esa misma fuente. T11 vigila que esto siga siendo cierto.
- **T2** — usa su propia instancia de `PostgresFixture` (no la colección), porque detiene el contenedor.
- **T4** — no necesita contenedor. Caso "base inaccesible": `ConnectionStrings:App` y `ConnectionStrings:Migraciones` = `Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x;Timeout=2`.
- **T8, T9** — la raíz del repo se busca subiendo desde `AppContext.BaseDirectory` hasta encontrar `HospitalTurnos.slnx` (`RaizDelRepo`, mismo patrón que `BuscarRaizDelRepo` del proyecto guía, ver Dev Notes). Los `.csproj` se leen con `XDocument`; el compose y el `.gitignore`, como texto línea por línea.
- **T10** — `factory.WithWebHostBuilder(b => { b.UseEnvironment(entorno); b.UseSetting("https_port", "443"); si hay red: b.ConfigureAppConfiguration(c => c.AddInMemoryCollection(new() { ["ProxyInverso:RedesConfiables:0"] = red })); b.ConfigureTestServices(s => s.AddTransient<IStartupFilter>(_ => new FiltroIpRemota(ip))); })`. `FiltroIpRemota.Configure(next)` devuelve `app => { app.Use((ctx, sig) => { ctx.Connection.RemoteIpAddress = ip; return sig(ctx); }); next(app); }` — va **antes** de `next(app)` para que `UseForwardedHeaders` vea la IP. Cliente con `AllowAutoRedirect = false` y encabezado `Host: turnos.lasacacias.example` (HSTS no se envía a `localhost`, que está en `HstsOptions.ExcludedHosts`).
- **T11** — la misma fábrica de la colección con `WithWebHostBuilder(b => b.UseEnvironment("Development"))`.
- **T12** — contenedor armado en el test con el mismo `PostgreSqlBuilder` de la fixture pero `WithResourceMapping(..., Unix.FileMode644)` y contraseñas al azar. Si la sobrecarga de `WithResourceMapping` de la versión instalada no acepta modo, anotar spec anchor: el caso 0644 es justamente el que se da por defecto.

## Tasks / Subtasks

- [x] [SEQ] Task 1: Salvaguarda de estado (AC: 1, 14)
  - [x] `git log --oneline -1`: hay commits; el código inicial está en `48f29a1` (Arq. §10 quiere reaprovechar `Medico.HorariosDelDia`).
  - [x] Confirmar que la solución es `HospitalTurnos.slnx` (no `.sln`): `ls HospitalTurnos.sln*`. Si apareció un `.sln`, usarlo y anotar el spec anchor.
  - [x] `dotnet --version` → `10.0.400` o superior; `docker info` responde.
  - [x] Confirmar que no existen todavía `Directory.Build.props`, `Directory.Packages.props`, `docker-compose.yml` en la raíz.
  - [x] `git check-ignore .env` → debe responder `.env` (el `.gitignore` ya lo ignora). Si no, DETENERSE: un `.env` podría versionarse.
- [x] [SEQ] Task 2: Borrar el código inicial (AC: 1, 14)
  - [x] `dotnet sln HospitalTurnos.slnx remove src/HospitalTurnos.Api/HospitalTurnos.Api.csproj tests/HospitalTurnos.Tests/HospitalTurnos.Tests.csproj`
  - [x] `rm -rf src/HospitalTurnos.Api tests/HospitalTurnos.Tests`
- [x] [SEQ] Task 3: Props de la solución y herramientas (AC: 3)
  - [x] `Directory.Build.props`, `src/Directory.Build.props` (importa el de arriba con `GetPathOfFileAbove`) y `Directory.Packages.props` con las versiones del contrato (fijar la última estable de cada uno con `dotnet package search <id> --exact-match`; EF/ASP.NET/TimeProvider.Testing en 10.0.x, xunit 2.9.x).
  - [x] `dotnet new tool-manifest` + `dotnet tool install dotnet-ef --version 10.0.*` (el `dotnet-ef` global de la máquina es 9.0.6 y no sirve para EF 10).
  - [x] `.gitattributes` con `*.sh text eol=lf` (el script de roles corre en Linux).
  - [x] `.env.ejemplo` con `POSTGRES_PASSWORD`, `TURNOS_OWNER_PASSWORD`, `TURNOS_APP_PASSWORD` y valores de ejemplo obvios (`cambiar-esta-clave-…`), con un comentario arriba: "Copiá este archivo a .env (que no se versiona)".
- [x] [SEQ] Task 4: Los 4 proyectos de `src/` (AC: 1, 2, 11, 16)
  - [x] `dotnet new classlib` para Dominio, Aplicacion, Infraestructura (borrar `Class1.cs`); `dotnet new blazor -n HospitalTurnos.Web -o src/HospitalTurnos.Web --interactivity Server` (plantilla completa: trae `Components/Pages/Error.razor`, que usa la story 1.2).
  - [x] Sacar de cada `.csproj` lo que ya fija `Directory.Build.props` y los `Version` de paquetes.
  - [x] Referencias entre proyectos según la tabla del contrato; `dotnet sln HospitalTurnos.slnx add ...` en las carpetas de solución `/src/` y `/tests/`.
  - [x] Borrar `Counter.razor`, `Weather.razor` y sus enlaces en `NavMenu.razor`; `Home.razor` con el `<h1>` y subtítulo de AC 11; `<html lang="es">` en `App.razor`.
  - [x] `dotnet user-secrets init --project src/HospitalTurnos.Web` (para `dotnet run` fuera de Compose; los secretos quedan fuera del repo). `appsettings*.json` sin `ConnectionStrings`.
- [x] [SEQ] Task 5: Persistencia (AC: 6, 7, 8)
  - [x] `HospitalDbContext` vacío + `ConvencionSnakeCase` + `AplicadorDeMigraciones` + `FabricaDeContextoEnDiseno` + `ServiciosDeInfraestructura.AgregarInfraestructura` (ver Implementación concreta).
  - [x] `dotnet ef migrations add Inicial --project src/HospitalTurnos.Infraestructura --startup-project src/HospitalTurnos.Web --output-dir Persistencia/Migraciones` (sin conectarse a la base).
- [x] [SEQ] Task 6: `Program.cs` de Web: infraestructura, validación de cadenas y migraciones al arrancar, encabezados del proxy + HSTS + redirección HTTPS, protección de datos, `/salud` con `ChequeoBaseDeDatos`, `public partial class Program;` (AC: 6, 7, 9, 10, 17, 19)
- [x] [PAR] Task 7: Docker: `01-roles.sh` (con `git update-index --chmod=+x` una vez que haya commit), `docker-compose.yml`, `Dockerfile`, `.dockerignore`, `scripts/smoke-compose.sh` (AC: 4, 12, 13, 15, 19, 20)
- [x] [SEQ] Task 8: Los 3 proyectos de tests, `PostgresFixture`, `ColeccionPostgres`, `HospitalWebApplicationFactory`, `FiltroIpRemota`, T1–T12 (AC: 1–11, 14, 16–18, 20)
- [x] [SEQ] Task 9: Verificación completa (build Release 0 advertencias, `dotnet test`, smoke E1 y E2) y Dev Agent Record

### Task Dependencies

T1 → T2 → T3 → T4 → T5 → T6 → T8 → T9 (secuencial)
T7 puede hacerse en paralelo con T4–T6; T8 lo necesita (el script de roles).

## Dev Notes

### Que existe hoy

#### En hospital-turnos (verificado 2026-10-15, al arrancar la implementación)

- Rama `story/1-1-estructura-solucion`, creada desde `main`. El código inicial quedó guardado en el commit `48f29a1` (recuperable para la story 4.1) y **ya fue borrado** con autorización del docente: `src/HospitalTurnos.Api/` y `tests/HospitalTurnos.Tests/` no existen y `HospitalTurnos.slnx` quedó vacío (`<Solution />`).
- La solución es `HospitalTurnos.slnx` (formato XML de .NET 10), **no** `HospitalTurnos.sln` como dicen el contrato técnico y Arq. §5. Esta story trabaja sobre el `.slnx`.
- Ya creados en este arranque (Task 3 y parte de Task 4): `Directory.Build.props`, `src/Directory.Build.props`, `Directory.Packages.props`, `.gitattributes`, `.env.ejemplo`, `.dockerignore`, `.config/dotnet-tools.json` (dotnet-ef 10.0.13) y los 4 proyectos de `src/` generados por plantilla (todavía sin ajustar).
- `.gitignore` ya ignora `bin/`, `obj/`, `.env` (y **no** ignora `.env.ejemplo`, que sí se versiona). No se toca.
- SDK `10.0.400`, Docker 28.4.0. Herramienta global `dotnet-ef` 9.0.6 (no sirve para EF 10 → manifiesto local). En .NET 10 `dotnet new tool-manifest` crea el manifiesto en la raíz: se movió a `.config/`.
- **NO existe todavía:** el resto de `files_to_create` (persistencia, `Program.cs` nuevo, Docker, pruebas).

### Patron de referencia verificado

**Patrón**: el proyecto guía de la materia ([changomas-testing-csharp](https://github.com/fernandobono/changomas-testing-csharp), .NET 10) es la única referencia del equipo (contrato técnico §"Patrón de referencia del equipo"). Se toma de ahí la separación en capas con el proyecto web como raíz de composición, la fábrica de pruebas de integración y el chequeo de salud; Docker, Testcontainers y gestión central de paquetes **no existen en la guía** y esta story los crea por primera vez.

| Proyecto | Ancla `archivo:linea` (o comando) | Qué se verificó exactamente |
|---|---|---|
| changomas-testing-csharp | `src/ChangoMas.Dominio/ChangoMas.Dominio.csproj:1-9` | Abrí el archivo: sin ningún `ProjectReference`. Igual que nuestro Dominio. |
| changomas-testing-csharp | `src/ChangoMas.Api/ChangoMas.Api.csproj:3-6` | El proyecto web referencia Aplicacion e Infraestructura: es la raíz de composición. Igual que nuestro Web. |
| changomas-testing-csharp | `src/ChangoMas.Infraestructura/ChangoMas.Infraestructura.csproj:3-5` | Infraestructura referencia **sólo Dominio**, porque allí los puertos viven en `src/ChangoMas.Dominio/Puertos/`. **Diferencia deliberada**: nuestro contrato pone los puertos en Aplicacion (`IAuditoria`, `IBandejaDeSalida`), por eso Infraestructura → Aplicacion, Dominio. |
| changomas-testing-csharp | `src/ChangoMas.Api/Program.cs:9-13` | Comentario de bloque que explica la composición de dependencias: misma densidad de comentarios "por qué" que pide el contrato. |
| changomas-testing-csharp | `src/ChangoMas.Api/Program.cs:48` | `app.MapGet("/health", () => Results.Ok(new { estado = "ok" }));` — **no chequea nada**. Nuestro `/salud` sí chequea la base y devuelve 503 (diferencia buscada, AC 10). |
| changomas-testing-csharp | `src/ChangoMas.Api/Program.cs:164-168` | `public partial class Program;` con comentario: hace falta para `WebApplicationFactory<Program>`. Se copia. |
| changomas-testing-csharp | `tests/ChangoMas.Tests.Integracion/CajaWebApplicationFactory.cs:20` y `:34-39` | `public class CajaWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime`; `ConfigureWebHost` cambia la base con `builder.UseSetting(...)` y `builder.UseEnvironment("Testing")`. `HospitalWebApplicationFactory` copia la forma (`WebApplicationFactory<Program>` + `ConfigureWebHost` + `UseEnvironment("Testing")`), **pero** inyecta las cadenas con `ConfigureAppConfiguration` + `AddInMemoryCollection` en vez de `UseSetting`: en `Development` otras fuentes le ganarían a `UseSetting` (revisión I-15). |
| changomas-testing-csharp | `tests/ChangoMas.Tests.Integracion/ApiCajaTests.cs:18` y `:24-32` | Clase de tests con `IClassFixture<CajaWebApplicationFactory>`; test de `/health` → 200. Mismo esqueleto para `SaludTests` (con nombres PascalCase del contrato, no el `El_health_check_responde_ok` de la guía). |
| changomas-testing-csharp | `tests/ChangoMas.Tests.E2E/Infraestructura/AplicacionEnEjecucion.cs:153-177` | `EsperarQueLaApiRespondaAsync`: espera por **condición** (que `/health` responda) con tope de 60 s, nunca `Sleep` fijo. `smoke-compose.sh` hace lo mismo con `/salud`. |
| changomas-testing-csharp | `tests/ChangoMas.Tests.E2E/Infraestructura/AplicacionEnEjecucion.cs:180-186` | `BuscarRaizDelRepo`: sube desde `AppContext.BaseDirectory` hasta encontrar el `.sln`. `RaizDelRepo` (T8) hace lo mismo buscando `HospitalTurnos.slnx`. |
| changomas-testing-csharp | `grep -rl -e Testcontainers -e Directory.Packages -e docker-compose --exclude-dir=bin --exclude-dir=obj --exclude-dir=.git .` | 0 resultados (corrido 2026-10-09). La guía usa SQLite en archivo temporal (`CajaWebApplicationFactory.cs:22-23`) y no tiene Docker: esas tres piezas **no tienen patrón** y se crean acá. |

**Conteo / evidencia dura**: 4/4 capas de la guía siguen la regla "dependencias hacia adentro"; 0 usos de Testcontainers, Docker o CPM en la guía.

### Que hay que hacer (la delta)

1. Borrar el código inicial y sacarlo del `.slnx`.
2. Crear los 7 proyectos con las referencias del contrato y los props centrales.
3. `HospitalDbContext` vacío con la convención `snake_case`, migración `Inicial` vacía y aplicación de migraciones al arrancar con el rol dueño.
4. Script de roles único, usado por el compose **y** por Testcontainers.
5. `docker-compose.yml` con `db`, `mailpit` y `web`, contraseñas desde `.env` (sin valores por defecto) y `.env.ejemplo`; `Dockerfile` multi-etapa de Web con la carpeta de claves.
6. Página de inicio mínima y `/salud` con chequeo real de la base.
7. Encabezados del proxy, HSTS y redirección HTTPS fuera de `Development` (NFR-03, parte de 1.1).

**Diferencias clave vs. la guía:**
- Guía: base SQLite creada a mano (`repositorio.CrearEsquema()`, `Program.cs:19-20`). Hospital: PostgreSQL con migraciones EF Core aplicadas por el rol dueño.
- Guía: `/health` siempre 200. Hospital: `/salud` 503 si no hay base.
- Guía: puertos en Dominio. Hospital: puertos en Aplicacion (contrato).

### Implementacion concreta

**Script de roles** (`docker/postgres/initdb/01-roles.sh`, lo usa el entrypoint de `postgres:17` sólo con el volumen vacío):

```bash
#!/usr/bin/env bash
# Por qué dos roles: la app (turnos_app) no puede cambiar el esquema ni, desde la story 1.3,
# tocar la auditoría. Sólo el dueño (turnos_owner) corre migraciones. Arq. §7.5.
#
# Por qué "set -eo pipefail" y NO "set -u": si el archivo no tiene bit ejecutable, el entrypoint
# de postgres lo INCLUYE con "." en vez de ejecutarlo, y un "set -u" quedaría activo en el propio
# entrypoint y podría cortarlo con "unbound variable" (revisión I-14).
set -eo pipefail

# Falta una contraseña: cortar con un mensaje claro, no crear un rol sin clave.
: "${TURNOS_OWNER_PASSWORD:?falta TURNOS_OWNER_PASSWORD (copiá .env.ejemplo a .env)}"
: "${TURNOS_APP_PASSWORD:?falta TURNOS_APP_PASSWORD (copiá .env.ejemplo a .env)}"

# Las claves y el nombre de la base van como variables de psql (:'x' y :"x"), así una comilla en
# la contraseña no rompe el SQL. Heredoc entre comillas: bash no expande nada adentro.
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
     -v base="$POSTGRES_DB" -v clave_owner="$TURNOS_OWNER_PASSWORD" -v clave_app="$TURNOS_APP_PASSWORD" <<-'SQL'
  CREATE ROLE turnos_owner LOGIN PASSWORD :'clave_owner';
  CREATE ROLE turnos_app   LOGIN PASSWORD :'clave_app';
  -- Dueño de la base = dueño del esquema public (pg_database_owner, PostgreSQL 15+).
  ALTER DATABASE :"base" OWNER TO turnos_owner;
  GRANT CONNECT ON DATABASE :"base" TO turnos_app;
  GRANT USAGE ON SCHEMA public TO turnos_app;
  REVOKE CREATE ON SCHEMA public FROM PUBLIC;
  ALTER DEFAULT PRIVILEGES FOR ROLE turnos_owner IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO turnos_app;
  ALTER DEFAULT PRIVILEGES FOR ROLE turnos_owner IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO turnos_app;
SQL
```

**Convención snake_case** (`ConvencionSnakeCase.ASnakeCase(string)`): inserta `_` antes de una mayúscula precedida por minúscula o dígito, y antes de una mayúscula seguida de minúscula y precedida por mayúscula; después pasa todo a minúsculas. En `OnModelCreating`, recorrer `modelBuilder.Model.GetEntityTypes()` y aplicar `SetTableName`, `SetColumnName` (con `StoreObjectIdentifier.Table`), `SetName` de claves, `SetConstraintName` de foráneas y `SetDatabaseName` de índices. Con 0 entidades no hace nada, pero queda lista para 1.3. El nombre de tabla parte del de `DbSet` (`RegistrosAuditoria`), no del de la clase.

**Migraciones al arrancar** (`AplicadorDeMigraciones.AplicarAsync(string cadenaDuenio, CancellationToken ct = default)`): arma un `DbContextOptions<HospitalDbContext>` propio con la cadena del dueño (no la del contenedor de DI, que usa `turnos_app`), llama a `Database.MigrateAsync(ct)` y envuelve `NpgsqlException` en el `InvalidOperationException` de AC 7. En desarrollo el contenedor `web` tiene las dos cadenas y migra al arrancar; en producción las migraciones son un paso aparte y la app no recibe la contraseña del dueño (contrato técnico, "Auditoría — garantía real del disparador").

**Fábrica de diseño** (`FabricaDeContextoEnDiseno : IDesignTimeDbContextFactory<HospitalDbContext>`): `UseNpgsql("Host=localhost;Database=hospital_turnos")`, **sin** usuario ni contraseña. Por qué: `dotnet ef migrations add` no se conecta a la base, pero necesita construir el contexto; sin esta fábrica intentaría leer `ConnectionStrings:App`, que ya no está en ningún archivo versionado.

**Program.cs (esqueleto):**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Infraestructura: DbContext con el rol de la app (turnos_app). La cadena se lee DIFERIDA (dentro
// del lambda de AddDbContext), así ve también la configuración que agregan las pruebas.
builder.Services.AgregarInfraestructura(builder.Configuration);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHealthChecks()
    .AddCheck<ChequeoBaseDeDatos>("base", timeout: TimeSpan.FromSeconds(3));

// NFR-03: el HTTPS lo termina el proxy. Sólo se le cree a X-Forwarded-* si viene de una red
// conocida: loopback (por defecto) + ProxyInverso:RedesConfiables. Nunca "confiar en todos".
builder.Services.AddOptions<ForwardedHeadersOptions>().Configure<IConfiguration>((o, config) =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    foreach (var red in config.GetSection("ProxyInverso:RedesConfiables").Get<string[]>() ?? [])
        o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(red));
});

// M-11: claves de protección de datos en una carpeta persistente (volumen en Compose), para que
// recrear el contenedor no invalide sesiones ni formularios abiertos.
var carpetaClaves = builder.Configuration["ProteccionDeDatos:Carpeta"];
var proteccion = builder.Services.AddDataProtection().SetApplicationName("HospitalTurnos");
if (!string.IsNullOrWhiteSpace(carpetaClaves))
    proteccion.PersistKeysToFileSystem(new DirectoryInfo(carpetaClaves));

var app = builder.Build();

// Las dos cadenas se validan con la configuración final (AC 7). Las migraciones las corre el
// dueño del esquema ANTES de atender a nadie.
ServiciosDeInfraestructura.CadenaDeConexion(app.Configuration, "App");
await AplicadorDeMigraciones.AplicarAsync(
    ServiciosDeInfraestructura.CadenaDeConexion(app.Configuration, "Migraciones"));

app.UseForwardedHeaders();          // primero: el resto del pipeline ve el esquema real (https)
// (la story 1.2 agrega acá UseExceptionHandler, en todos los entornos)
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
// ... resto del pipeline de la plantilla Blazor (sin su UseHsts/UseHttpsRedirection propios) ...
app.MapHealthChecks("/salud", new HealthCheckOptions { ResponseWriter = EscribirSalud });
app.Run();

public partial class Program;
```

- `ServiciosDeInfraestructura.CadenaDeConexion(IConfiguration, string nombre)` es `public static` y lanza `InvalidOperationException($"Falta la cadena de conexión '{nombre}'")` si la cadena falta o está vacía.
- `AgregarInfraestructura(this IServiceCollection, IConfiguration configuracion)` registra `AddDbContext<HospitalDbContext>((sp, o) => o.UseNpgsql(CadenaDeConexion(configuracion, "App")))`: el lambda corre al resolver el contexto, no al registrarlo.
- `ChequeoBaseDeDatos : IHealthCheck` usa `HospitalDbContext.Database.CanConnectAsync(ct)`; `false` o excepción → `HealthCheckResult.Unhealthy()` **sin** pasar la excepción a la respuesta. `MapHealthChecks` ya traduce Unhealthy a 503.
- `EscribirSalud` escribe `{"estado":"ok"}` o `{"estado":"sin conexión con la base"}` con `application/json`; nunca `report.Entries[..].Exception`.
- `KnownIPNetworks` (con `System.Net.IPNetwork`) es la propiedad de .NET 10; `KnownNetworks` está marcada obsoleta y con `TreatWarningsAsErrors` no compila. Si el SDK instalado dice otra cosa, anotar spec anchor.
- En producción el puerto HTTPS de la redirección se fija con `ASPNETCORE_HTTPS_PORT=443`. Sin ese valor (Compose de desarrollo, `TestServer`), `UseHttpsRedirection` no redirige y lo avisa una vez en el registro: es lo esperado, el proxy redirige antes.

**docker-compose.yml** (contraseñas **sólo** desde `.env`, sin valores por defecto; contrato técnico "Secretos"):

```yaml
# Contraseñas: copiá .env.ejemplo a .env (que no se versiona). Sin .env, Compose se corta (AC 15).
services:
  db:
    image: postgres:17
    environment:
      POSTGRES_DB: hospital_turnos
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?copiá .env.ejemplo a .env}
      TURNOS_OWNER_PASSWORD: ${TURNOS_OWNER_PASSWORD:?copiá .env.ejemplo a .env}
      TURNOS_APP_PASSWORD: ${TURNOS_APP_PASSWORD:?copiá .env.ejemplo a .env}
    ports: ["127.0.0.1:5432:5432"]   # sólo desde esta máquina
    volumes:
      - datos-db:/var/lib/postgresql/data
      - ./docker/postgres/initdb:/docker-entrypoint-initdb.d:ro
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d hospital_turnos"]
      interval: 5s
      retries: 10
  mailpit:
    image: axllent/mailpit
    ports: ["1025:1025", "8025:8025"]
  web:
    build: { context: ., dockerfile: src/HospitalTurnos.Web/Dockerfile }
    ports: ["8080:8080"]
    environment:
      # En desarrollo el contenedor tiene también la cadena del dueño y migra al arrancar.
      # En producción NO: las migraciones son un paso aparte (contrato técnico, I-8).
      ConnectionStrings__App: Host=db;Database=hospital_turnos;Username=turnos_app;Password=${TURNOS_APP_PASSWORD:?copiá .env.ejemplo a .env}
      ConnectionStrings__Migraciones: Host=db;Database=hospital_turnos;Username=turnos_owner;Password=${TURNOS_OWNER_PASSWORD:?copiá .env.ejemplo a .env}
      ProteccionDeDatos__Carpeta: /claves-proteccion
    volumes:
      - claves-proteccion:/claves-proteccion
    depends_on:
      db: { condition: service_healthy }
      mailpit: { condition: service_started }
    # La app no puede ser el proceso 1 del contenedor: Linux ignora ahí la señal con la que .NET
    # termina tras un error, y el arranque fallido (AC 7) quedaba girando al 100% de CPU.
    init: true
    restart: unless-stopped
volumes:
  datos-db:
  claves-proteccion:
```

**Dockerfile**: contexto = raíz del repo (necesita los `Directory.*.props` y los otros proyectos). Etapa `mcr.microsoft.com/dotnet/sdk:10.0` (`dotnet restore` + `dotnet publish src/HospitalTurnos.Web -c Release -o /app`) → etapa `mcr.microsoft.com/dotnet/aspnet:10.0` (escucha en 8080 por defecto). En la etapa final, `RUN mkdir -p /claves-proteccion && chown $APP_UID /claves-proteccion` y después `USER $APP_UID`: un volumen nombrado nuevo copia el dueño de la carpeta de la imagen, así el usuario sin privilegios puede escribir las claves. `.dockerignore` excluye `**/bin`, `**/obj`, `.git`, `tests/`, `.env`.

**smoke-compose.sh**: `set -euo pipefail` (éste sí se ejecuta, no lo incluye nadie). Primero E2 (sin `.env`), después E1. Cada espera es por condición con tope (bucle `curl` cada 2 s), nunca `sleep` fijo. Imprime `<nombre del test> OK` o el paso que falló.

### Archivos a crear/modificar

| Acción | Archivo |
|--------|---------|
| Borrar | `src/HospitalTurnos.Api/` (carpeta entera) |
| Borrar | `tests/HospitalTurnos.Tests/` (carpeta entera) |
| Modificar | `HospitalTurnos.slnx` (sacar los 2 proyectos viejos, agregar los 7 nuevos) |
| Crear | `Directory.Build.props`, `src/Directory.Build.props`, `Directory.Packages.props` |
| Crear | `.config/dotnet-tools.json` (dotnet-ef 10.0.x), `.gitattributes`, `.dockerignore`, `.env.ejemplo` |
| Crear | `docker-compose.yml`, `docker/postgres/initdb/01-roles.sh`, `scripts/smoke-compose.sh` |
| Crear | `src/HospitalTurnos.Dominio/HospitalTurnos.Dominio.csproj` |
| Crear | `src/HospitalTurnos.Aplicacion/HospitalTurnos.Aplicacion.csproj` |
| Crear | `src/HospitalTurnos.Infraestructura/HospitalTurnos.Infraestructura.csproj`, `ServiciosDeInfraestructura.cs` |
| Crear | `src/HospitalTurnos.Infraestructura/Persistencia/HospitalDbContext.cs`, `ConvencionSnakeCase.cs`, `AplicadorDeMigraciones.cs`, `FabricaDeContextoEnDiseno.cs`, `Migraciones/*_Inicial.cs` (generado) |
| Crear | `src/HospitalTurnos.Web/` (plantilla `blazor`), `Program.cs`, `Dockerfile`, `appsettings*.json`, `Components/Pages/Home.razor`, `Salud/ChequeoBaseDeDatos.cs` |
| Crear | `tests/HospitalTurnos.Dominio.Tests/` (`.csproj`, `Arquitectura/RaizDelRepo.cs`, `Arquitectura/EstructuraDeLaSolucionTests.cs`, `Arquitectura/SecretosTests.cs`) |
| Crear | `tests/HospitalTurnos.Aplicacion.Tests/` (`.csproj`, `Persistencia/ConvencionSnakeCaseTests.cs`) |
| Crear | `tests/HospitalTurnos.Web.Tests/` (`.csproj`, `PostgresFixture.cs`, `ColeccionPostgres.cs`, `HospitalWebApplicationFactory.cs`, `Ayudas/FiltroIpRemota.cs`, `Salud/SaludTests.cs`, `Arranque/ArranqueTests.cs`, `Arranque/RolesDeBaseTests.cs` (T5, T6, T12), `Arranque/HttpsDetrasDelProxyTests.cs` (T10), `Arranque/FabricaDePruebasTests.cs` (T11), `Inicio/InicioTests.cs`) |

### Lo que NO hay que hacer

- **NO borrar el código inicial sin un commit previo** — nunca se guardó en git; borrarlo lo pierde para siempre y la story 4.1 quiere reaprovechar ideas (Arq. §10). Si no hay commit, se detiene y se le pide al docente.
- **NO crear `HospitalTurnos.sln`** — ya existe `HospitalTurnos.slnx`; tener dos soluciones divide la verdad.
- **NO usar `dotnet ef database update`** — usaría la cadena `App` (`turnos_app`, sin `CREATE`) y fallaría. Las migraciones las aplica la app al arrancar con el dueño.
- **NO dar a `turnos_app` permisos de dueño ni `CREATE`** — la story 1.3 se apoya en que la app no pueda alterar el esquema ni la auditoría (ADR-07).
- **NO poner contraseñas ni cadenas de conexión en ningún archivo versionado** — ni en `appsettings*.json`, ni en `docker-compose.yml` (ni como valor por defecto `${X:-clave}`), ni en el código de pruebas. Van en `.env` (Compose), `user-secrets` (`dotnet run`) o variables de entorno (producción). Si falta la cadena, el arranque debe fallar (AC 7), no usar un valor por defecto escondido. Lo vigila T9.
- **NO usar `set -u` en `01-roles.sh`** — si el archivo llega sin bit ejecutable, el entrypoint lo incluye y el `-u` le quedaría activo (I-14). Las variables obligatorias se verifican con `: "${X:?mensaje}"`. Lo vigila T12.
- **NO interpolar las contraseñas con `${…}` dentro del SQL del script** — una comilla en la clave rompe el SQL; van como variables de `psql`.
- **NO confiar en `X-Forwarded-*` de cualquiera** — ni `KnownIPNetworks.Clear()`/`KnownProxies.Clear()` ni `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` (que limpia las listas): cualquiera podría decir que llegó por HTTPS. Lo vigila T10 (caso IP no confiable).
- **NO inyectar las cadenas de prueba con `UseSetting`** — en `Development` otras fuentes les ganan (I-15). Usar la fuente en memoria de la fábrica. Lo vigila T11.
- **NO usar `EFCore.NamingConventions`** — el contrato dice que la convención se aplica en `OnModelCreating`, y escribirla es material de clase.
- **NO crear el proyecto `HospitalTurnos.E2E`** — el contrato lo deja fuera del sprint 1.
- **NO agregar Identity, MailKit ni el reloj** — son de las stories 2.1, 1.4 y 1.2. Sus versiones sí van en `Directory.Packages.props` (se fijan una vez), pero no se referencian todavía.
- **NO esperar con `Thread.Sleep`/`sleep` fijo** en el smoke ni en los tests — esperar por condición con tope (patrón de la guía, `AplicacionEnEjecucion.cs:153-177`).
- **NO devolver el texto de la excepción en `/salud`** — es un detalle técnico (NFR-09).
- **NO compartir el contenedor de T2 ni el de T12** — T2 lo detiene y T12 lo arma con otro modo de archivo; usar instancias propias.
- **NO crear los roles con SQL propio en las pruebas** — la `PostgresFixture` usa el mismo `01-roles.sh` (contrato técnico, C-1).

### Como verificar que funciona

```bash
cd ~/SourceCode/hospital-turnos

# Secretos: nada versionado; la primera vez se copia el ejemplo
git check-ignore .env                          # .env
grep -n -e ":-" -e "_dev" docker-compose.yml   # sin resultados
cp -n .env.ejemplo .env

# Estructura
dotnet sln HospitalTurnos.slnx list            # 7 proyectos, ninguno HospitalTurnos.Api / HospitalTurnos.Tests
test ! -d src/HospitalTurnos.Api && test ! -d tests/HospitalTurnos.Tests && echo "código inicial borrado"
grep -rn "HospitalTurnos.Api" src tests        # sin resultados

# Compilación sin advertencias
dotnet build HospitalTurnos.slnx -c Release    # 0 Advertencias, 0 Errores

# Pruebas de la story (Docker tiene que estar corriendo para Testcontainers)
dotnet test HospitalTurnos.slnx --filter "Story=1.1"
dotnet test HospitalTurnos.slnx --filter "Story=1.1&AC=10"   # sólo base caída

# Compose completo + smoke E1/E2
docker compose up -d --build
curl -s -i http://localhost:8080/salud         # 200 {"estado":"ok"}
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8025   # 200 (Mailpit)
curl -s http://localhost:8080/ | grep -c "Hospital Municipal Las Acacias"   # 1 o más
docker compose exec db psql -U turnos_app -d hospital_turnos -c "CREATE TABLE x (id int);"   # ERROR: permission denied for schema public
docker compose stop db && curl -s -i http://localhost:8080/salud   # 503 {"estado":"sin conexión con la base"}
docker compose start db
curl -s -i -H "X-Forwarded-Proto: https" http://localhost:8080/salud   # 200 sin Strict-Transport-Security: la IP del host no es una red confiable
bash scripts/smoke-compose.sh                  # Smoke_DockerComposeSinEnv_FallaConMensajeClaro OK + Smoke_DockerCompose_LevantaYSaludResponde OK

# dotnet run fuera de Compose (secretos fuera del repo)
dotnet user-secrets set "ConnectionStrings:App" "Host=localhost;Database=hospital_turnos;Username=turnos_app;Password=<la de .env>" --project src/HospitalTurnos.Web
dotnet user-secrets set "ConnectionStrings:Migraciones" "Host=localhost;Database=hospital_turnos;Username=turnos_owner;Password=<la de .env>" --project src/HospitalTurnos.Web

# El script de roles sólo corre con el volumen vacío: para re-probarlo
docker compose down -v && docker compose up -d --build
```

## Dev Agent Record

### Agent Model Used

Claude Opus 5.5 (claude-opus-5-5), como Amelia (desarrolladora del equipo BMAD).

### Debug Log References

- Build Release: 0 advertencias, 0 errores.
- Pruebas: Dominio.Tests 2/2, Aplicacion.Tests 6/6, Web.Tests 15/15 (23 casos = T1–T12 contando cada caso de los `[Theory]`).
- Smoke E1 y E2 en verde contra el Compose real; evidencia en vivo de 18 de 20 AC. Los AC 8 y 18 se declararon no verificables corriendo la app (`runtime_not_verifiable`) y el docente los aprobó.

### Completion Notes List

- Pruebas escritas antes que el código (T1–T12 y las ayudas de prueba), después Docker, después `src/`.
- Desvíos de detalle, sin cambio de comportamiento (revisados contra la spec, sin hallazgos):
  - `Directory.Packages.props` fija también `Microsoft.EntityFrameworkCore` y `.Relational` 10.0.13 con `CentralPackageTransitivePinningEnabled`: Npgsql 10.0.3 traía EF 10.0.4 y las pruebas mezclaban versiones (MSB3277).
  - `PostgresFixture` tiene un constructor interno (modo del script, aplicar o no migraciones) y `CadenaSuperusuario`, para T2 y T12, que necesitan su propio contenedor.
  - Testcontainers 4.16 cambió la firma de `WithResourceMapping` (uid, gid, modo): se pasa `fileMode:` por nombre.
  - `/salud` escribe el JSON como texto fijo: el serializador escapaba la "ó" como `\u00F3`.
  - En .NET 10 `dotnet new tool-manifest` crea el manifiesto en la raíz: se movió a `.config/`.
  - `.dockerignore` excluye además `Sesiones/` y `docs/`.
- Arreglos por la revisión: Mailpit publicado sólo en 127.0.0.1; `/salud` deja la causa de la falla en el registro del servidor (sin mostrarla en la respuesta).
- Hallazgo en la prueba en vivo del AC 7: sin `init: true` el contenedor `web` quedaba colgado al 100% de CPU al fallar el arranque (la app era el proceso 1). Ver Change Log.
- En macOS, un script montado como carpeta en Docker aparece como ejecutable aunque tenga modo 0644: para probar el AC 20 a mano hay que copiarlo (`docker cp`), como hace T12.
- Pase adversarial: salteado por decisión del docente.

### File List

- Borrados: `src/HospitalTurnos.Api/`, `tests/HospitalTurnos.Tests/` (respaldo en el commit 48f29a1).
- Modificados: `HospitalTurnos.slnx`, `docs/stories/1-1-estructura-solucion.md`.
- Raíz: `Directory.Build.props`, `Directory.Packages.props`, `.config/dotnet-tools.json`, `.gitattributes`, `.dockerignore`, `.env.ejemplo`, `docker-compose.yml`, `docker/postgres/initdb/01-roles.sh`, `scripts/smoke-compose.sh`.
- `src/Directory.Build.props`; `src/HospitalTurnos.Dominio/`, `src/HospitalTurnos.Aplicacion/` (sólo `.csproj`).
- `src/HospitalTurnos.Infraestructura/`: `.csproj`, `ServiciosDeInfraestructura.cs`, `Persistencia/HospitalDbContext.cs`, `ConvencionSnakeCase.cs`, `AplicadorDeMigraciones.cs`, `FabricaDeContextoEnDiseno.cs`, `Migraciones/*_Inicial.cs` + snapshot.
- `src/HospitalTurnos.Web/`: plantilla Blazor (sin Counter/Weather), `.csproj`, `Program.cs`, `Dockerfile`, `Salud/ChequeoBaseDeDatos.cs`, `Components/App.razor`, `Components/Pages/Home.razor`, `Components/Layout/NavMenu.razor`.
- `tests/HospitalTurnos.Dominio.Tests/`: `.csproj`, `Arquitectura/RaizDelRepo.cs`, `EstructuraDeLaSolucionTests.cs`, `SecretosTests.cs`.
- `tests/HospitalTurnos.Aplicacion.Tests/`: `.csproj`, `Persistencia/ConvencionSnakeCaseTests.cs`.
- `tests/HospitalTurnos.Web.Tests/`: `.csproj`, `PostgresFixture.cs`, `ColeccionPostgres.cs`, `HospitalWebApplicationFactory.cs`, `Ayudas/FiltroIpRemota.cs`, `Salud/SaludTests.cs`, `Inicio/InicioTests.cs`, `Arranque/ArranqueTests.cs`, `Arranque/RolesDeBaseTests.cs`, `Arranque/HttpsDetrasDelProxyTests.cs`, `Arranque/FabricaDePruebasTests.cs`.

### Change Log


- 2026-10-15 — `init: true` en el servicio `web` del Compose (aprobado por el docente). Al juntar evidencia en vivo del AC 7 se vio que, sin `ConnectionStrings:App`, la imagen escribía el error pero el proceso no terminaba: como proceso 1 del contenedor, la señal de cierre de .NET se ignora y queda al 100% de CPU. Con `init` sale en 2 s con código ≠ 0. Las pruebas en memoria (T4) no podían verlo.
- 2026-10-15 — Story 1.3 (aprobado por el docente): T5 deja de exigir una sola migración; pide todas las del proyecto, cada una una vez, con `_Inicial` incluida.

## Review Follow-ups (Advisors)

<!-- tycon-pipeline: generado por Step 6/7. Cada item = finding advisor no resuelto (CRITICAL waived o WARNING). Al resolver el finding en el código, marcá [x]. Step 0/1 detecta los abiertos al retomar (pipeline.js followups-status). -->

- [x] **[CRITICAL]** ACCOV-C1 — `docs/stories/1-1-estructura-solucion.md` — E1/E2 son el script scripts/smoke-compose.sh (no xUnit, como declara la story); corrieron en verde vía smoke-run.sh. Confirmado por tycon-ac-coverage-checker. <!-- fkey:ACCOV-C1|docs/stories/1-1-estructura-solucion.md -->
- [x] **[WARNING]** SEC-W7 — `docker-compose.yml` — Puertos de Mailpit publicados sólo en 127.0.0.1 <!-- fkey:SEC-W7|docker-compose.yml -->
- [x] **[WARNING]** SILENT-C2 — `src/HospitalTurnos.Web/Salud/ChequeoBaseDeDatos.cs` — LogWarning con la excepción; la respuesta sigue sin detalles; OperationCanceledException del pedido se propaga <!-- fkey:SILENT-C2|src/HospitalTurnos.Web/Salud/ChequeoBaseDeDatos.cs -->
- [ ] **[WARNING]** TEST-W2 — `tests/HospitalTurnos.Web.Tests` — En observación, no frena: test-gate.sh no reconoce .slnx y se corrió por proyecto (23/23 en verde); la cobertura por líneas no se midió en esta story. <!-- fkey:TEST-W2|tests/HospitalTurnos.Web.Tests -->
- [x] **[CRITICAL]** DODRT-C8 — `docker-compose.yml` — Evidencia en vivo del AC 7: sin la cadena App el contenedor quedaba colgado al 100% de CPU (la app era PID 1). init: true en web, aprobado por el docente; Change Log de la story. <!-- fkey:DODRT-C8|docker-compose.yml -->
- [x] **[WARNING]** SILENT-W7 — `scripts/smoke-compose.sh` — El paso del AC 7 distingue tope vencido (142) de falla, y borra el contenedor con nombre fijo (trap EXIT). <!-- fkey:SILENT-W7|scripts/smoke-compose.sh -->
