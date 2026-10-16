---
epic: 1
story: "1.2"
title: "Reloj del hospital (zona Córdoba) y manejador global de errores sin detalles técnicos."
status: done
sprint: 1
priority: "P0"
approach: "[CREATE]"
depends_on: ["1.1"]
files_to_create:
  - "src/HospitalTurnos.Infraestructura/Tiempo/RelojHospital.cs"
  - "src/HospitalTurnos.Aplicacion/Comun/TimeProviderExtensions.cs"
  - "src/HospitalTurnos.Dominio/Excepciones/ReglaDeNegocioException.cs"
  - "src/HospitalTurnos.Dominio/Excepciones/ConflictoException.cs"
  - "src/HospitalTurnos.Dominio/Excepciones/NoEncontradoException.cs"
  - "src/HospitalTurnos.Dominio/Excepciones/AccesoDenegadoException.cs"
  - "src/HospitalTurnos.Web/Errores/ManejadorGlobalDeErrores.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Tiempo/RelojHospitalTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Comun/TimeProviderExtensionsTests.cs"
  - "tests/HospitalTurnos.Dominio.Tests/Arquitectura/HoraDelSistemaTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Ayudas/RegistroEnMemoria.cs"
  - "tests/HospitalTurnos.Web.Tests/Ayudas/ProveedorDeRegistroEnMemoria.cs"
  - "tests/HospitalTurnos.Web.Tests/Ayudas/FiltroRutaQueFalla.cs"
  - "tests/HospitalTurnos.Web.Tests/Errores/ManejadorGlobalDeErroresTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Errores/ErroresDeExtremoAExtremoTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Tiempo/RegistroDelRelojTests.cs"
files_to_modify:
  - "src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs"
  - "src/HospitalTurnos.Web/Program.cs"
  - "src/HospitalTurnos.Web/Components/Pages/Error.razor"
---

# Story 1.2: Reloj del hospital (zona Córdoba) y manejador global de errores sin detalles técnicos.

Status: done

## Story

Como sistema de turnos del Hospital Municipal Las Acacias,
quiero que toda la hora salga de un único reloj inyectable en la zona `America/Argentina/Cordoba`, y que cualquier error llegue al usuario como un mensaje claro sin detalles técnicos (con el detalle completo sólo en el registro técnico),
para cumplir NFR-06 ("todas las fechas y horas se manejan en hora de Argentina") y NFR-09 ("los errores no muestran detalles técnicos al usuario; quedan en el registro técnico"), y que las pruebas de reglas con fechas (agendas, reservas, cancelaciones hasta 2 h antes) puedan fijar la hora.

## Acceptance Criteria

### Reloj del hospital — NFR-06, Arq. §7.8

1. **Given** un `RelojHospital` creado sin argumentos **When** se lee `LocalTimeZone` **Then** `Id == "America/Argentina/Cordoba"` y `BaseUtcOffset == TimeSpan.FromHours(-3)`.
2. **Given** un `FakeTimeProvider` con `SetLocalTimeZone(Córdoba)` y hora UTC `2026-10-09T12:00:00Z` **When** se llama `AhoraLocal()` **Then** devuelve `2026-10-09 09:00:00` con `Kind == DateTimeKind.Unspecified` (hora local del hospital, contrato técnico "Fechas de negocio").
3. **Given** la hora UTC `2026-10-10T02:30:00Z` **When** se llama `AhoraLocal()` **Then** devuelve `2026-10-09 23:30:00`: el día local es el **anterior** al día UTC. *(Borde: cambio de fecha.)*
4. **Given** un id de zona que la máquina no tiene (`"America/Inexistente"`) **When** se crea `new RelojHospital("America/Inexistente")` **Then** lanza `InvalidOperationException` cuyo mensaje contiene el id de la zona y la palabra `tzdata`, con la `TimeZoneNotFoundException` original como `InnerException`. *(Modo de falla de AC 1: en un contenedor sin base de zonas, la app no arranca con una hora equivocada sino con un error claro.)*
5. **Given** `HospitalTurnos.Web` arrancada **When** se resuelve `TimeProvider` dos veces del contenedor de dependencias **Then** las dos veces es la **misma** instancia y es `RelojHospital` (registrado como singleton por `AgregarInfraestructura`).
6. **Given** todo archivo `.cs` bajo `src/`, salvo `Infraestructura/Tiempo/RelojHospital.cs` y `Infraestructura/Persistencia/Migraciones/` **When** se busca `DateTime.Now`, `DateTime.UtcNow`, `DateTime.Today`, `DateTimeOffset.Now` o `DateTimeOffset.UtcNow` **Then** hay 0 apariciones. *(Modo de falla: alguien lee la hora del sistema y la prueba con reloj fijo deja de controlarla.)*

### Excepciones del dominio — contrato técnico "Errores"

7. (declarativo) En `src/HospitalTurnos.Dominio/Excepciones/` existen `ReglaDeNegocioException(string mensaje) : Exception`, `ConflictoException(string mensaje) : ReglaDeNegocioException`, `NoEncontradoException(string mensaje) : Exception` y `AccesoDenegadoException(string mensaje) : Exception`. El mensaje está escrito **para el usuario**, en castellano (lo que se le muestra tal cual).

### Manejador global de errores — NFR-09

8. **Given** una petición que acepta JSON **When** el código lanza una excepción del dominio **Then** `ManejadorGlobalDeErrores` responde `application/problem+json` con `status` y `title = excepción.Message`, según: `ConflictoException` → **409** (no 400, aunque herede de `ReglaDeNegocioException`); `ReglaDeNegocioException` → 400; `NoEncontradoException` → 404; `AccesoDenegadoException` → 403. Estas son esperables: se registran con nivel `Information`, **nunca** `Error`.
9. **Given** una excepción **no** prevista (por ejemplo `InvalidOperationException("detalle secreto")`) **When** la procesa el manejador **Then** responde 500 con `title = "Algo falló. Probá de nuevo en unos minutos."`, sin `detail`, sin el mensaje interno, sin el nombre del tipo ni la pila; **And** registra con nivel `Error` la excepción completa en el `ILogger<ManejadorGlobalDeErrores>`. *(Hace falta registrarla en el manejador: en .NET 10 el middleware deja de registrar la excepción cuando un `IExceptionHandler` la maneja.)*
10. **Given** la app corriendo **When** una ruta lanza una excepción no prevista y la petición trae `Accept: application/json` **Then** la respuesta HTTP es 500 `application/problem+json` con el mensaje genérico de AC 9 y **sin** el texto `detalle secreto`.
11. **Given** la app corriendo **When** la misma ruta la pide un navegador (`Accept: text/html,...`) **Then** el manejador no escribe JSON y deja que el middleware muestre la página `/Error`: 500 HTML con el mensaje genérico, **sin** `detalle secreto`, **sin** `Request ID` y **sin** el texto `Development Mode` de la plantilla. *(Segundo "layout" de la respuesta de error.)* El middleware no vuelve a registrar la excepción al re-ejecutar `/Error` (ver AC 14).
12. **Given** el entorno `Development` **When** pasa lo mismo de AC 11 **Then** el resultado es el mismo: la página de excepciones para desarrolladores (con la pila) **no** se muestra, porque `UseExceptionHandler` se registra en todos los entornos. El desarrollador ve el detalle en la consola (registro técnico), no en el navegador.

### No-regresión

13. **Given** la base caída **When** `GET /salud` **Then** sigue respondiendo 503 `{"estado":"sin conexión con la base"}` como en la story 1.1 (AC 10 de 1.1): el chequeo de salud no lanza excepciones, así que no pasa por el manejador. Todas las pruebas con `Story=1.1` siguen pasando.

### Un solo registro por excepción (M-1)

14. **Given** una excepción en una petición, en cualquiera de los dos "layouts" (JSON o HTML) **When** termina la respuesta **Then** el registro técnico tiene **exactamente una** entrada por esa excepción, la de `ManejadorGlobalDeErrores`: nivel `Information` si es del dominio (y ninguna entrada `Error` de ninguna categoría), nivel `Error` si es inesperada. *(Modo de falla: en el camino HTML el manejador devuelve `false`, el middleware re-ejecuta `/Error` y, sin configurarlo, registra la excepción con nivel `Error` — una regla de negocio quedaría como `Error`, contra AC 8, y lo inesperado quedaría dos veces.)*

### Tests (9 verificaciones + 1 de regresión)

> **Ajuste vs. la estimación (6 → 9):** se suman la prueba del modo de falla de la zona horaria (AC 4), la que impide leer la hora del sistema (AC 6), que es la única forma de que NFR-06 no se rompa en silencio en stories futuras, y (por la revisión adversarial, M-1) la que exige un solo registro por excepción en los dos "layouts" (T9). Los dos "layouts" del error (JSON y HTML) y el entorno `Development` van en una sola `[Theory]` (T8).

| # | Test | AC | Nivel | Escenario | Resultado esperado |
|---|------|----|-------|-----------|--------------------|
| T1 | `LocalTimeZone_RelojSinArgumentos_EsCordobaMenosTres` | 1 | unit | `new RelojHospital()` | `Id == "America/Argentina/Cordoba"`, `BaseUtcOffset == -03:00` |
| T2 | `AhoraLocal_InstanteUtc_DevuelveHoraLocalDeCordobaSinKind` | 2, 3 | unit | `[Theory]`: `2026-10-09T12:00Z → 2026-10-09 09:00`; `2026-10-10T02:30Z → 2026-10-09 23:30` | Valor exacto y `Kind == Unspecified` |
| T3 | `Constructor_ZonaInexistente_LanzaErrorQueNombraLaZona` | 4 | unit | `new RelojHospital("America/Inexistente")` | `InvalidOperationException`; mensaje contiene `America/Inexistente` y `tzdata`; `InnerException is TimeZoneNotFoundException` |
| T4 | `TimeProvider_ContenedorDeLaWeb_EsUnSoloRelojHospital` | 5 | integration | `HospitalWebApplicationFactory` (de 1.1) + `Services.GetRequiredService<TimeProvider>()` dos veces | `IsType<RelojHospital>` y `Same(primera, segunda)` |
| T5 | `CodigoFuente_ArchivosDeSrc_NoLeenLaHoraDelSistema` | 6 | unit | Recorre `src/**/*.cs` desde `RaizDelRepo` (de 1.1) salvo las 2 exclusiones | Lista de `archivo:línea` con apariciones vacía; si no, el `Assert` imprime cada una |
| T6 | `TryHandleAsync_ExcepcionDelDominio_DevuelveCodigoYMensajeDeLaRegla` | 7, 8 | unit | `[Theory]` 4 casos: Conflicto→409, ReglaDeNegocio→400, NoEncontrado→404, AccesoDenegado→403 | Devuelve `true`; `StatusCode` esperado; `title` = mensaje; `RegistroEnMemoria` sin entradas de nivel `Error` |
| T7 | `TryHandleAsync_ExcepcionInesperada_Devuelve500GenericoYRegistraDetalle` | 9 | unit | `InvalidOperationException("detalle secreto")`, `Accept: application/json` | Devuelve `true`; 500; `title` genérico; cuerpo sin `detalle secreto` ni `InvalidOperationException`; una entrada `Error` con esa excepción |
| T8 | `Get_RutaQueFalla_MuestraMensajeGenericoSinDetalles` | 10, 11, 12 | integration | `[Theory]` 3 casos: (`application/json`, `Testing`), (`text/html`, `Testing`), (`text/html`, `Development`); ruta `/prueba/falla` agregada sólo en el test | 500; JSON: `problem+json` con `title` genérico; HTML: contiene `Algo falló. Probá de nuevo en unos minutos.`; nunca `detalle secreto`, `Request ID` ni `Development Mode` |
| T9 | `Get_RutaQueFalla_RegistraLaExcepcionUnaSolaVez` | 11, 14 | integration | `[Theory]` 4 casos (excepción, `Accept`): (`ReglaDeNegocioException`, `application/json`); (`ReglaDeNegocioException`, `text/html`); (`InvalidOperationException`, `application/json`); (`InvalidOperationException`, `text/html`). Rutas `/prueba/falla-regla` y `/prueba/falla` agregadas sólo en el test; `ProveedorDeRegistroEnMemoria` captura todas las categorías | Regla: 1 entrada `Information` de `ManejadorGlobalDeErrores` y 0 entradas `Error` en total. Inesperada: exactamente 1 entrada `Error` en total, de `ManejadorGlobalDeErrores`, con esa excepción |
| R1 | `Salud_BaseCaidaDespuesDeArrancar_Devuelve503SinDetalles` (story 1.1, T2) | 13 | integration | Se vuelve a correr sin cambios | Sigue pasando |

Cada test nuevo lleva `[Trait("Story", "1.2")]` y un `[Trait("AC", "n")]` por AC cubierto. R1 conserva sus Traits de 1.1; se corre con `--filter "Story=1.1"`.

#### Given/Fixture

- **T2** — `var reloj = new FakeTimeProvider(new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero)); reloj.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Cordoba"));` → `reloj.AhoraLocal()`.
- **T6/T7** — `DefaultHttpContext` con `Response.Body = new MemoryStream()` y `Request.Headers.Accept = "application/json"`. `IProblemDetailsService` real: `new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider().GetRequiredService<IProblemDetailsService>()`. El registro técnico es `RegistroEnMemoria<ManejadorGlobalDeErrores> : ILogger<ManejadorGlobalDeErrores>`, un doble **escrito a mano** que guarda `(LogLevel, Exception?, string mensaje)` en una lista (mismo estilo que el fake `RepositorioVentasEnMemoria` de la guía). Leer el cuerpo con `JsonDocument`: `System.Text.Json` escapa la `ó` (`falló`), así que **no** comparar el texto crudo.
- **T4, T8, T9** — clases con `[Collection("Postgres")]` y la `HospitalWebApplicationFactory` de 1.1. **Caso `Development` (I-15):** con `UseEnvironment("Development")` se cargan `appsettings.Development.json` y `user-secrets`; no le ganan a las cadenas de la prueba porque la fábrica las inyecta como **última** fuente de configuración (`ConfigureAppConfiguration` + `AddInMemoryCollection`, contrato técnico §"Pruebas"). Lo vigila la prueba T11 de la story 1.1; si esa prueba no existe o falla, DETENERSE: T8 estaría hablando con otra base.
- **T8, T9** — rutas que fallan, agregadas **sólo** desde el test: `factory.WithWebHostBuilder(b => { b.UseEnvironment(entorno); b.ConfigureTestServices(s => s.AddTransient<IStartupFilter, FiltroRutaQueFalla>()); })`. `FiltroRutaQueFalla.Configure(next)` devuelve `app => { next(app); app.Map("/prueba/falla", a => a.Run(_ => throw new InvalidOperationException("detalle secreto"))); app.Map("/prueba/falla-regla", a => a.Run(_ => throw new ReglaDeNegocioException("No hay turnos ese día."))); }`. Se agrega **después** de `next(app)` para quedar dentro del pipeline de la app, debajo de `UseExceptionHandler`. Si la excepción no llega al manejador, anotarlo como spec anchor y usar otra forma de inyectar la ruta en el test (nunca una ruta de prueba en el código de producción).
- **T8 JSON** — parsear el cuerpo con `JsonDocument` y comparar `title`. **T8 HTML** — comparar el texto del cuerpo (en HTML la `ó` sale literal o como `&#xF3;`: normalizar con `WebUtility.HtmlDecode` antes de buscar).
- **T9** — `ProveedorDeRegistroEnMemoria : ILoggerProvider`, escrito a mano: crea un `ILogger` por categoría que anota `(Categoria, LogLevel, Exception?, string mensaje)` en una lista común (thread-safe, `ConcurrentQueue`). Se registra con `ConfigureTestServices(s => s.AddSingleton<ILoggerProvider>(proveedor))` (el `LoggerFactory` toma todos los `ILoggerProvider` del contenedor). Se lee **después** de recibir la respuesta completa. Cada caso usa su propio proveedor (instancia nueva) para no contar entradas de otro caso.

## Tasks / Subtasks

- [x] [SEQ] Task 1: Salvaguarda de estado (AC: todos)
  - [x] La story 1.1 está `done`: existen `src/HospitalTurnos.Web/Program.cs`, `src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs` (con `AgregarInfraestructura`), `src/HospitalTurnos.Web/Components/Pages/Error.razor`, `tests/HospitalTurnos.Web.Tests/HospitalWebApplicationFactory.cs`, `tests/HospitalTurnos.Dominio.Tests/Arquitectura/RaizDelRepo.cs`. Si falta alguno → DETENERSE.
  - [x] `dotnet test HospitalTurnos.slnx --filter "Story=1.1"` en verde antes de tocar nada (incluye T11 de 1.1: la fábrica usa las cadenas de la fixture también en `Development`).
  - [x] `grep -rn -e "DateTime.Now" -e "DateTime.UtcNow" -e "DateTimeOffset.Now" -e "DateTimeOffset.UtcNow" -e "DateTime.Today" src` → anotar si ya hay apariciones (no debería: 1.1 no usa la hora).
  - [x] `ls /usr/share/zoneinfo/America/Argentina/Cordoba` en la máquina y `docker compose run --rm --entrypoint ls web /usr/share/zoneinfo/America/Argentina/Cordoba` en la imagen de Web. Si la imagen no la tiene, agregar `tzdata` en el `Dockerfile` y anotarlo.
- [x] [PAR] Task 2: Excepciones del dominio (AC: 7)
- [x] [PAR] Task 3: `RelojHospital` + `TimeProviderExtensions.AhoraLocal` + registro singleton en `AgregarInfraestructura` (AC: 1–5)
- [x] [SEQ] Task 4: `ManejadorGlobalDeErrores` + registro en `Program.cs` (con `SuppressDiagnosticsCallback`) + `Error.razor` genérica (AC: 8–12, 14) — necesita Task 2
- [x] [SEQ] Task 5: Referencias de pruebas: `Microsoft.Extensions.TimeProvider.Testing` en Aplicacion.Tests y Web.Tests (AC: 2, 3) — **ya resuelta por la story 1.1**: los dos `.csproj` lo referencian y la versión está fijada en `Directory.Packages.props`. No hay cambios.
- [x] [SEQ] Task 6: Tests T1–T9, `ProveedorDeRegistroEnMemoria`, `FiltroRutaQueFalla` (AC: 1–12, 14)
- [x] [SEQ] Task 7: Verificación: build Release 0 advertencias, `--filter "Story=1.2"`, regresión `--filter "Story=1.1"`, prueba manual en compose; Dev Agent Record (AC: 13)

### Task Dependencies

T1 → (T2 ∥ T3) → T4 → T5 → T6 → T7
T2 y T3 son independientes entre sí.

## Dev Notes

### Que existe hoy

#### Al escribir esta story (2026-10-09), antes de implementar 1.1

- `src/HospitalTurnos.Api/Endpoints/ManejadorDeErrores.cs` (28 líneas) — un `IExceptionHandler` del código inicial. **La story 1.1 lo borra**; se copia acá porque es la idea de partida (no se referencia el archivo):
  ```csharp
  public class ManejadorDeErrores(IProblemDetailsService problemas) : IExceptionHandler
  {
      public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception error, CancellationToken ct)
      {
          var estado = error switch
          {
              NoEncontradoException => StatusCodes.Status404NotFound,
              ConflictoException => StatusCodes.Status409Conflict,
              ReglaDeNegocioException => StatusCodes.Status400BadRequest,
              _ => 0
          };
          if (estado == 0) return false; // lo inesperado sigue al manejador por defecto, sin exponer detalles
          ...
  ```
  Lo que **le falta** y esta story agrega: el 403 de `AccesoDenegadoException`, el 500 genérico propio (con `return false` el mensaje lo decidía el default, no nosotros), el registro técnico explícito y la respuesta HTML para el navegador.
- `src/HospitalTurnos.Api/Dominio/ReglaDeNegocioException.cs` (10 líneas) — mismas tres excepciones en un solo archivo, con constructor primario `(string mensaje)`. También lo borra 1.1. Se recrean en `Dominio/Excepciones/`, un archivo por clase, y se suma `AccesoDenegadoException`.

#### Después de la story 1.1 (de lo que depende esta)

- `src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs` — `AgregarInfraestructura(this IServiceCollection, IConfiguration)` registra el `HospitalDbContext`. Acá se agrega el singleton del reloj.
- `src/HospitalTurnos.Web/Program.cs` — pipeline de la plantilla Blazor con `/salud`. Empieza con `app.UseForwardedHeaders()` y un bloque `if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }` (NFR-03, story 1.1). De la plantilla puede quedar `app.UseExceptionHandler("/Error", createScopeForErrors: true)` dentro de un `if (!IsDevelopment())`: esta story lo saca de ahí y lo pone, en todos los entornos, justo después de `UseForwardedHeaders` (AC 12). El bloque HSTS/HTTPS de 1.1 no se toca.
- `src/HospitalTurnos.Web/Components/Pages/Error.razor` — página de la plantilla que muestra `Request ID` y un párrafo `Development Mode`. Se reemplaza por el mensaje genérico.
- `tests/HospitalTurnos.Web.Tests/HospitalWebApplicationFactory.cs` + `PostgresFixture.cs` — fábrica sobre PostgreSQL real (T4, T8).
- `tests/HospitalTurnos.Dominio.Tests/Arquitectura/RaizDelRepo.cs` — encuentra la raíz del repo (T5).
- `tests/HospitalTurnos.Web.Tests/ColeccionPostgres.cs` — colección `"Postgres"` (contrato técnico §"Pruebas").
- **NO existe — se crea en esta story:** `RelojHospital`, `TimeProviderExtensions`, las 4 excepciones en `Dominio/Excepciones/`, `ManejadorGlobalDeErrores`, `RegistroEnMemoria`, `ProveedorDeRegistroEnMemoria`, `FiltroRutaQueFalla`.

### Patron de referencia verificado

**Patrón**: excepciones del dominio con el mensaje para el usuario adentro, traducidas a códigos HTTP en el borde web. El proyecto guía (`~/SourceCode/changomas-testing-csharp`) lo hace **endpoint por endpoint** con `try/catch`; esta story lo **centraliza** en un `IExceptionHandler`. Reloj inyectable: **no aplica**, la guía no tiene.

| Proyecto | Ancla `archivo:linea` (o comando) | Qué se verificó exactamente |
|---|---|---|
| changomas-testing-csharp | `src/ChangoMas.Dominio/Excepciones/ProductoNoEncontradoException.cs:7-8` | Abrí el archivo: `ProductoNoEncontradoException(string codigo) : Exception($"Producto no encontrado, solicite asistencia (código {codigo}).")`. El mensaje es para el usuario, en castellano, armado en el dominio. Mismo criterio para nuestras 4 excepciones (AC 7). |
| changomas-testing-csharp | `src/ChangoMas.Api/Program.cs:77-91` | `try { ... } catch (ProductoNoEncontradoException ex) { return Results.NotFound(new { error = ..., mensaje = ex.Message }); } catch (RequiereAutorizacionException ex) { return Results.Conflict(...); }` — el mapeo excepción→código está **repetido en cada endpoint** (también `:97-105` y `:122-138`). Es el contraejemplo que motiva centralizarlo: con 34 stories, el `try/catch` repetido se olvidaría en alguna. |
| changomas-testing-csharp | `tests/ChangoMas.Tests.Integracion/ApiCajaTests.cs:49-53` | Comentario: "el contrato HTTP también se testea: tiene que ser 404, no 500". Mismo objetivo que T6 y T8. |
| changomas-testing-csharp | `tests/ChangoMas.Tests.Unit/Ayudas/RepositorioVentasEnMemoria.cs:6-20` | Fake escrito a mano, con comentario de clase que explica qué es un fake y una propiedad para espiar (`VecesQueSeGuardo`). `RegistroEnMemoria` sigue ese estilo en vez de usar un paquete de dobles. |
| changomas-testing-csharp | `grep -rl -e TimeProvider -e IExceptionHandler -e UseExceptionHandler --exclude-dir=bin --exclude-dir=obj --exclude-dir=.git .` | 0 resultados (corrido 2026-10-09). La guía no tiene reloj inyectable ni manejador global: **esas dos piezas no tienen patrón en el equipo y se crean acá** siguiendo el contrato técnico. La guía usa `DateTime.UtcNow` sólo en su infraestructura de E2E (`tests/ChangoMas.Tests.E2E/Infraestructura/AplicacionEnEjecucion.cs:156,158`), nunca en reglas de negocio. |

**Conteo / evidencia dura**: guía con 3 bloques `try/catch` de mapeo excepción→HTTP en `Program.cs` y 0 manejadores globales; 0 usos de `TimeProvider`.

### Que hay que hacer (la delta)

1. Crear las 4 excepciones del dominio en `Dominio/Excepciones/` (un archivo por clase).
2. Crear `RelojHospital : TimeProvider` con la zona de Córdoba y error claro si la zona no existe; registrarlo como singleton.
3. Crear la extensión `AhoraLocal(this TimeProvider)` en Aplicacion.
4. Crear `ManejadorGlobalDeErrores : IExceptionHandler` que distingue dominio / inesperado y JSON / HTML, y que registra el detalle.
5. Registrar `AddExceptionHandler<ManejadorGlobalDeErrores>()` y `AddProblemDetails()`; registrar `UseExceptionHandler` en todos los entornos, con `ExceptionHandlingPath = "/Error"`, `CreateScopeForErrors = true` y `SuppressDiagnosticsCallback` para que el único que registra sea el manejador (AC 14).
6. Reescribir `Error.razor` con el mensaje genérico.

**Diferencias clave vs. la guía y vs. el código inicial:**
- Guía: `try/catch` en cada endpoint. Hospital: un manejador global; los endpoints y páginas no atrapan excepciones del dominio.
- Código inicial: lo inesperado devolvía `false` y quedaba en manos del manejador por defecto. Hospital: el 500 y su mensaje los decide `ManejadorGlobalDeErrores` (JSON) o la página `/Error` (HTML).

### Implementacion concreta

```csharp
// src/HospitalTurnos.Infraestructura/Tiempo/RelojHospital.cs
/// El único lugar del sistema que sabe en qué zona está el hospital (NFR-06).
/// Hereda de TimeProvider: el resto del código pide la hora a TimeProvider y en las
/// pruebas se cambia por un FakeTimeProvider fijo.
public sealed class RelojHospital : TimeProvider
{
    public const string ZonaCordoba = "America/Argentina/Cordoba";
    private readonly TimeZoneInfo _zona;

    public RelojHospital() : this(ZonaCordoba) { }

    public RelojHospital(string idZona)
    {
        try { _zona = TimeZoneInfo.FindSystemTimeZoneById(idZona); }
        catch (TimeZoneNotFoundException ex)
        {
            // Por qué fallar al arrancar: una hora corrida 3 h daría turnos en horarios equivocados.
            throw new InvalidOperationException(
                $"No se encontró la zona horaria '{idZona}' en esta máquina. Instalá el paquete tzdata.", ex);
        }
    }

    public override TimeZoneInfo LocalTimeZone => _zona;
}

// src/HospitalTurnos.Aplicacion/Comun/TimeProviderExtensions.cs
public static class TimeProviderExtensions
{
    /// Hora local del hospital, sin zona (Kind=Unspecified): así se guardan los turnos (S-34).
    public static DateTime AhoraLocal(this TimeProvider reloj)
        => DateTime.SpecifyKind(reloj.GetLocalNow().DateTime, DateTimeKind.Unspecified);
}

// En AgregarInfraestructura:
services.AddSingleton<TimeProvider>(new RelojHospital());
```

```csharp
// src/HospitalTurnos.Web/Errores/ManejadorGlobalDeErrores.cs (esqueleto)
public sealed class ManejadorGlobalDeErrores(
    IProblemDetailsService problemas, ILogger<ManejadorGlobalDeErrores> registro) : IExceptionHandler
{
    public const string MensajeGenerico = "Algo falló. Probá de nuevo en unos minutos.";

    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception error, CancellationToken ct)
    {
        // El orden importa: ConflictoException ES una ReglaDeNegocioException.
        var estado = error switch
        {
            ConflictoException => StatusCodes.Status409Conflict,
            ReglaDeNegocioException => StatusCodes.Status400BadRequest,
            NoEncontradoException => StatusCodes.Status404NotFound,
            AccesoDenegadoException => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };
        var esperable = estado != StatusCodes.Status500InternalServerError;

        if (esperable) registro.LogInformation("Regla de negocio: {Mensaje}", error.Message);
        else registro.LogError(error, "Error no previsto en {Ruta}", http.Request.Path);

        // Un navegador pide text/html explícito: que lo atienda la página /Error.
        // (Un navegador también manda */*, y el escritor de ProblemDetails lo aceptaría como JSON.)
        if (http.Request.GetTypedHeaders().Accept.Any(a => a.MediaType == "text/html")) return false;

        http.Response.StatusCode = estado;
        return await problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = null, // nunca pasar la excepción: algunos escritores la vuelcan en Development
            ProblemDetails = new ProblemDetails { Status = estado, Title = esperable ? error.Message : MensajeGenerico }
        });
    }
}
```

> Las excepciones del dominio que lleguen desde una página HTML también terminan en `/Error` con el mensaje genérico. Mostrar el mensaje de la regla dentro de cada pantalla es trabajo de cada story de pantalla (validan antes y atrapan la excepción del caso de uso en el formulario). Esta story no lo resuelve.

**Program.cs (cambios):**

```csharp
builder.Services.AddExceptionHandler<ManejadorGlobalDeErrores>();
builder.Services.AddProblemDetails();
...
app.UseForwardedHeaders();   // (story 1.1, queda primero)

// En TODOS los entornos (AC 12). La página de excepciones de desarrollador queda por fuera
// y nunca ve la excepción, porque este middleware ya la manejó.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    ExceptionHandlingPath = "/Error",
    CreateScopeForErrors = true,
    // Por qué suprimir siempre: ManejadorGlobalDeErrores ya registró cada excepción una vez y con
    // el nivel correcto. Sin esto, en el camino HTML (el manejador devuelve false) el middleware
    // vuelve a registrarla como Error al re-ejecutar /Error (AC 14, revisión M-1).
    SuppressDiagnosticsCallback = _ => true
});

if (!app.Environment.IsDevelopment())   // (story 1.1, sin cambios)
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
```

> `SuppressDiagnosticsCallback` existe en `ExceptionHandlerOptions` de .NET 10 (la revisión lo verificó en el código de 10.0.11). Si el SDK instalado no la tiene, anotar spec anchor y filtrar la categoría del middleware (`Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware`) a `None` en `appsettings.json`; T9 decide si alcanzó.

**Error.razor**: conservar `@page "/Error"`; quitar el `RequestId`, el `[CascadingParameter] HttpContext` si sólo servía para eso, y el bloque `Development Mode`. Mostrar un `<h1>Algo falló</h1>` y `<p>Algo falló. Probá de nuevo en unos minutos.</p>`, con un enlace a `/`.

### Archivos a crear/modificar

| Acción | Archivo |
|--------|---------|
| Crear | `src/HospitalTurnos.Infraestructura/Tiempo/RelojHospital.cs` |
| Crear | `src/HospitalTurnos.Aplicacion/Comun/TimeProviderExtensions.cs` |
| Crear | `src/HospitalTurnos.Dominio/Excepciones/ReglaDeNegocioException.cs`, `ConflictoException.cs`, `NoEncontradoException.cs`, `AccesoDenegadoException.cs` |
| Crear | `src/HospitalTurnos.Web/Errores/ManejadorGlobalDeErrores.cs` |
| Modificar | `src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs` (registrar `RelojHospital` como `TimeProvider` singleton) |
| Modificar | `src/HospitalTurnos.Web/Program.cs` (`AddExceptionHandler`, `AddProblemDetails`, `UseExceptionHandler` con opciones en todos los entornos) |
| ~~Modificar (si hace falta)~~ No se modificó | `src/HospitalTurnos.Web/Dockerfile` — la imagen ya trae `/usr/share/zoneinfo/America/Argentina/Cordoba` (verificado en la Task 1), así que no hizo falta instalar `tzdata` |
| Modificar | `src/HospitalTurnos.Web/Components/Pages/Error.razor` (mensaje genérico, sin Request ID ni Development Mode) |
| Crear | `tests/HospitalTurnos.Aplicacion.Tests/Tiempo/RelojHospitalTests.cs` (T1, T3), `Comun/TimeProviderExtensionsTests.cs` (T2) |
| Crear | `tests/HospitalTurnos.Dominio.Tests/Arquitectura/HoraDelSistemaTests.cs` (T5) |
| Crear | `tests/HospitalTurnos.Web.Tests/Ayudas/RegistroEnMemoria.cs`, `Ayudas/ProveedorDeRegistroEnMemoria.cs`, `Ayudas/FiltroRutaQueFalla.cs`, `Errores/ManejadorGlobalDeErroresTests.cs` (T6, T7), `Errores/ErroresDeExtremoAExtremoTests.cs` (T8, T9), `Tiempo/RegistroDelRelojTests.cs` (T4) |

### Lo que NO hay que hacer

- **NO usar `DateTime.Now`/`UtcNow`/`Today` ni `DateTimeOffset.Now`/`UtcNow` fuera de `RelojHospital`** — la prueba con reloj fijo deja de controlar la hora (contrato técnico "Hora"). El contrato nombra tres; esta story agrega `DateTime.Today` y `DateTimeOffset.UtcNow` porque leen el mismo reloj del sistema. Lo vigila T5.
- **NO calcular la hora de Córdoba con `AddHours(-3)`** — funciona hoy, pero esconde la regla en cada llamada; la zona vive en un solo lugar.
- **NO leer la zona de configuración** — el contrato la fija; un valor configurable es una forma más de equivocarse (YAGNI). El constructor con id existe sólo para probar el modo de falla.
- **NO registrar `RelojHospital` como scoped o transient** — es un singleton sin estado; el contrato lo pide así (AC 5).
- **NO poner `ReglaDeNegocioException` antes que `ConflictoException` en el `switch`** — un conflicto devolvería 400 en vez de 409 (AC 8).
- **NO pasar `Exception = error` al `ProblemDetailsContext`** ni completar `Detail` con el mensaje interno — es exactamente el detalle técnico que NFR-09 prohíbe.
- **NO dejar `UseExceptionHandler` dentro del `if (!IsDevelopment())`** — en Development se vería la pila en el navegador y T8 (caso `Development`) falla.
- **NO agregar una ruta que lance excepciones al código de producción** para probar — la ruta `/prueba/falla` existe sólo dentro del test (T8).
- **NO registrar como `Error` las excepciones del dominio** — son esperables (un horario ocupado, un paciente inexistente); llenarían el registro técnico de falsas alarmas.
- **NO atrapar excepciones en los endpoints o páginas para traducirlas a HTTP** — ese es el patrón repetido de la guía que esta story reemplaza.
- **NO dejar que el middleware también registre la excepción** — una sola entrada por excepción, la del manejador (AC 14). Lo vigila T9.
- **NO inyectar las cadenas de prueba con `UseSetting` en una fábrica nueva** — en `Development` les ganan otras fuentes (I-15); usar la `HospitalWebApplicationFactory` de 1.1.

### Fuera de alcance (anotado para la story 5.5)

- **Errores dentro de un circuito interactivo de Blazor** (M-2): NFR-09 se cumple acá sólo para pedidos HTTP. Una excepción en un componente con interactividad de servidor no pasa por `UseExceptionHandler` ni por `ManejadorGlobalDeErrores`. Hoy no hay pantallas interactivas. La story 5.5 (primera pantalla interactiva, R1) tiene que agregar un `ErrorBoundary` con el mensaje genérico y asegurar `DetailedErrors = false` fuera de `Development` (la plantilla trae `"DetailedErrors": true` en `appsettings.Development.json`).

### Como verificar que funciona

```bash
cd ~/SourceCode/hospital-turnos

dotnet build HospitalTurnos.slnx -c Release                    # 0 Advertencias, 0 Errores

# Pruebas de la story (Docker corriendo para T4 y T8)
dotnet test HospitalTurnos.slnx --filter "Story=1.2"
dotnet test HospitalTurnos.slnx --filter "Story=1.2&AC=4"      # zona inexistente
dotnet test HospitalTurnos.slnx --filter "Story=1.2&AC=11"     # página de error HTML
dotnet test HospitalTurnos.slnx --filter "Story=1.2&AC=14"     # un solo registro por excepción

# Regresión de 1.1
dotnet test HospitalTurnos.slnx --filter "Story=1.1"

# Nadie lee la hora del sistema fuera del reloj (debe mostrar sólo RelojHospital.cs, o nada)
grep -rn -e "DateTime.Now" -e "DateTime.UtcNow" -e "DateTime.Today" \
         -e "DateTimeOffset.Now" -e "DateTimeOffset.UtcNow" src --include="*.cs" | grep -v "/Migraciones/"

# La imagen tiene la zona de Córdoba
docker compose up -d --build
docker compose exec web ls /usr/share/zoneinfo/America/Argentina/Cordoba

# /salud sigue igual con la base caída (no pasa por el manejador)
docker compose stop db && curl -s -i http://localhost:8080/salud   # 503 {"estado":"sin conexión con la base"}
docker compose start db
```

## Dev Agent Record

### Agent Model Used

Claude Opus 5.5 (claude-opus-5-5), como Amelia (desarrolladora del equipo BMAD).

### Debug Log References

- Salvaguarda (Task 1): archivos de 1.1 presentes; `Story=1.1` 15/15 en verde antes de tocar nada; 0 lecturas de la hora del sistema en `src/`; la zona `America/Argentina/Cordoba` está en la máquina y en la imagen de Web, así que el `Dockerfile` **no se tocó**; `SuppressDiagnosticsCallback` existe en ASP.NET Core 10.0.11.
- Build Release: 0 advertencias, 0 errores.
- Pruebas (suite completa por proyecto, `test-gate.sh` no reconoce `.slnx`): Dominio.Tests 3/3, Aplicacion.Tests 10/10, Web.Tests 28/28 (incluye las 15 de la 1.1: R1 y el resto siguen en verde). La 1.2 suma 18 casos (T1–T9 contando cada caso de los `[Theory]`).
- Cobertura de los archivos nuevos de `src/` (coverlet, medida a mano): 100% de líneas.
- Prueba en vivo contra el Compose reconstruido: 14/14 AC observados corriendo la app (zona, reloj, cambio de día, los cuatro errores del dominio, el 500 genérico en JSON y en HTML, Development, registro único, `/salud` con la base caída). Capturas en `Sesiones/img/03-*.png`.

### Completion Notes List

- Pruebas escritas antes que el código. Fase roja: no compilaban por las piezas que la story manda crear; con el código de A y B, T8/T9 seguían en rojo por aserción (la excepción se escapaba sin manejar y en `Development` aparecía la página técnica) hasta conectar el manejador en `Program.cs`.
- T5 es una prueba guardiana: pasa desde el principio porque nadie lee la hora del sistema. Se comprobó que sí atrapa una lectura plantando un `DateTime.Today` temporal en `Dominio` (falló nombrando archivo y línea) y sacándolo.
- `FiltroRutaQueFalla` con `IStartupFilter` después de `next(app)` alcanzó: la excepción llega al manejador (no hizo falta otra forma de inyectar la ruta).
- `SuppressDiagnosticsCallback` alcanzó para el registro único (T9 en verde en los 4 casos); no hizo falta filtrar la categoría del middleware.
- La prueba unitaria del manejador usa el `IProblemDetailsService` real tal como lo describe la story; no necesitó más servicios.
- Task 5 ya estaba resuelta por la 1.1 (se corrigió la story antes de implementar, a pedido del validador).
- Pase adversarial: salteado por decisión del docente.

### File List

- Creados en `src/`: `HospitalTurnos.Infraestructura/Tiempo/RelojHospital.cs`, `HospitalTurnos.Aplicacion/Comun/TimeProviderExtensions.cs`, `HospitalTurnos.Dominio/Excepciones/{ReglaDeNegocio,Conflicto,NoEncontrado,AccesoDenegado}Exception.cs`, `HospitalTurnos.Web/Errores/ManejadorGlobalDeErrores.cs`.
- Modificados en `src/`: `HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs`, `HospitalTurnos.Web/Program.cs`, `HospitalTurnos.Web/Components/Pages/Error.razor`.
- Creados en `tests/`: `HospitalTurnos.Aplicacion.Tests/Tiempo/RelojHospitalTests.cs`, `Comun/TimeProviderExtensionsTests.cs`; `HospitalTurnos.Dominio.Tests/Arquitectura/HoraDelSistemaTests.cs`; `HospitalTurnos.Web.Tests/Ayudas/{RegistroEnMemoria,ProveedorDeRegistroEnMemoria,FiltroRutaQueFalla}.cs`, `Errores/{ManejadorGlobalDeErrores,ErroresDeExtremoAExtremo}Tests.cs`, `Tiempo/RegistroDelRelojTests.cs`.
- Sin cambios (previstos "si hace falta"): `src/HospitalTurnos.Web/Dockerfile`, los `.csproj` de pruebas.
- Documentación: esta story.

### Change Log

- 2026-10-15 — Antes de implementar (a pedido del validador, aprobado por el docente): Task 5 marcada como ya resuelta por la 1.1 y sus dos `.csproj` fuera de `files_to_modify`; estado del cuerpo alineado.
- 2026-10-15 — Tras la revisión: el `Dockerfile` sale de `files_to_modify` (la imagen ya trae la zona). Sin cambios de código por la revisión.


## Review Follow-ups (Advisors)

<!-- tycon-pipeline: generado por Step 6/7. Cada item = finding advisor no resuelto (CRITICAL waived o WARNING). Al resolver el finding en el código, marcá [x]. Step 0/1 detecta los abiertos al retomar (pipeline.js followups-status). -->

- [x] **[CRITICAL]** ACCOV-C1 — `docs/stories/1-2-reloj-y-errores.md` — Falso positivo: R1 es la prueba de la 1.1 en tests/HospitalTurnos.Web.Tests/Salud/SaludTests.cs:41, existe y pasa (confirmado por tycon-ac-coverage-checker). Aceptado como falso positivo por el docente. <!-- fkey:ACCOV-C1|docs/stories/1-2-reloj-y-errores.md -->
- [x] **[WARNING]** VAL-W15 — `docs/stories/1-2-reloj-y-errores.md` — Dockerfile sacado de files_to_modify y anotado como no modificado (la imagen ya trae la zona). <!-- fkey:VAL-W15|docs/stories/1-2-reloj-y-errores.md -->
- [ ] **[WARNING]** SILENT-W2 — `src/HospitalTurnos.Web/Errores/ManejadorGlobalDeErrores.cs` — Intencional y declarado en la story: el mensaje de la regla en pantalla es trabajo de cada story de pantalla. <!-- fkey:SILENT-W2|src/HospitalTurnos.Web/Errores/ManejadorGlobalDeErrores.cs -->
- [ ] **[WARNING]** TEST-W2 — `tests/HospitalTurnos.Web.Tests` — test-gate no cruza cobertura con .slnx; medida a mano con coverlet: 100% de líneas en los archivos nuevos (step-04-coverage.md). <!-- fkey:TEST-W2|tests/HospitalTurnos.Web.Tests -->
