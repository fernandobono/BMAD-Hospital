---
epic: 1
story: "1.4"
title: "Bandeja de salida de emails con proceso de fondo y reintentos"
status: ready-for-dev
sprint: 1
priority: "P0"
approach: "[CREATE]"
depends_on: ["1.1", "1.2", "1.3"]
driver: |
  ADR-05: un SMTP caído no puede hacer fallar una reserva ni perder un aviso (FR-20, FR-28, FR-29).
  Patrón bandeja de salida con reintentos a 1, 5 y 15 min (S-33). Ver docs/04-arquitectura.md §7.4.
files_to_create:
  - src/HospitalTurnos.Aplicacion/Email/MensajeEmail.cs
  - src/HospitalTurnos.Aplicacion/Email/IBandejaDeSalida.cs
  - src/HospitalTurnos.Aplicacion/Email/IEnviadorEmail.cs
  - src/HospitalTurnos.Aplicacion/Email/IManejadorFalloDefinitivo.cs
  - src/HospitalTurnos.Infraestructura/Email/MensajeSaliente.cs
  - src/HospitalTurnos.Infraestructura/Email/BandejaDeSalidaEnBase.cs
  - src/HospitalTurnos.Infraestructura/Email/ProcesadorBandejaDeSalida.cs
  - src/HospitalTurnos.Infraestructura/Email/EnviadorEmailSmtp.cs
  - src/HospitalTurnos.Infraestructura/Email/OpcionesSmtp.cs
  - src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/MensajeSalienteConfiguracion.cs
  - src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/AAAAMMDDHHMMSS_AgregarBandejaDeSalida.cs
  - tests/HospitalTurnos.Aplicacion.Tests/Email/BandejaDeSalidaTests.cs
  - tests/HospitalTurnos.Aplicacion.Tests/Email/ProcesadorBandejaDeSalidaTests.cs
  - tests/HospitalTurnos.Aplicacion.Tests/Email/EnviadorEmailSmtpTests.cs
  - tests/HospitalTurnos.Aplicacion.Tests/Email/Dobles/EnviadorEmailFalso.cs
  - tests/HospitalTurnos.Aplicacion.Tests/Email/Dobles/ManejadorFalloDefinitivoFalso.cs
  - tests/HospitalTurnos.Aplicacion.Tests/Email/LimpiezaDeBandeja.cs
  - tests/HospitalTurnos.Aplicacion.Tests/Ayudas/RegistroEnMemoria.cs
  - tests/HospitalTurnos.Web.Tests/Arranque/ConfiguracionSmtpTests.cs
files_to_modify:
  - src/HospitalTurnos.Infraestructura/Persistencia/HospitalDbContext.cs
  - src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/HospitalDbContextModelSnapshot.cs
  - src/HospitalTurnos.Infraestructura/HospitalTurnos.Infraestructura.csproj
  - src/HospitalTurnos.Web/Program.cs
  - src/HospitalTurnos.Web/appsettings.Development.json
  - docker-compose.yml
  - .env.ejemplo
  - tests/HospitalTurnos.Web.Tests/HospitalWebApplicationFactory.cs
---

# Story 1.4: Bandeja de salida de emails con proceso de fondo y reintentos

Status: ready-for-dev

## Story

Como sistema de turnos del Hospital Las Acacias,
quiero que los casos de uso **encolen** los emails en una tabla dentro de su misma transacción y que un
proceso de fondo los envíe cada 15 segundos, reintentando a 1, 5 y 15 minutos y avisando a un manejador por
tipo cuando un email se da por perdido,
para que un servidor de correo caído no haga fallar una reserva ni un bloqueo, ningún aviso se pierda en
silencio y el paciente que no recibió el email termine en la lista "Pacientes para avisar" (FR-29, story 7.2).

Requisitos: arquitectura §7.4 y ADR-05 (`docs/04-arquitectura.md:235-243`, `:302`), S-33 (`docs/04-arquitectura.md:317`),
FR-20, FR-28, FR-29 (`docs/02-prd.md:192`, `:206-207`).

## Acceptance Criteria

### Encolar (dentro de la transacción del caso de uso)

1. **Given** un `FakeTimeProvider` en `2026-10-12T11:00:00Z` **When** un caso de uso llama
   `IBandejaDeSalida.Encolar(new MensajeEmail("ana@ejemplo.com", "Asunto", "texto", "<p>html</p>", "codigo-ingreso", "P-1"))`
   y después `SaveChangesAsync()` **Then** `mensajes_salientes` tiene una fila con esos datos, `intentos = 0`,
   `creado_el = proximo_intento_el = 2026-10-12 11:00:00+00` y `enviado_el`, `fallo_definitivo_el`,
   `manejado_el` en `NULL` (pendiente, lista para el próximo ciclo).
2. **Given** un `Encolar(...)` **When** no se llama `SaveChangesAsync()` y se descarta el contexto **Then**
   no queda ninguna fila: `Encolar` agrega y **no guarda** (contrato §Bandeja de salida). *(modo de falla de AC 1)*

### Envío

3. **Given** un mensaje pendiente con `proximo_intento_el <= ahora` y un `IEnviadorEmail` que responde bien
   **When** corre un ciclo (`ProcesarPendientesAsync`) **Then** el enviador recibe **una** llamada con los
   mismos `Para`, `Asunto`, `CuerpoTexto`, `CuerpoHtml`; la fila queda con `enviado_el = ahora` e
   `intentos = 1` **And** un segundo ciclo no vuelve a enviarlo.
4. **Given** un mensaje pendiente con `proximo_intento_el` en el futuro **When** corre un ciclo **Then** el
   enviador no recibe ninguna llamada. *(modo de falla: no se adelanta un reintento)*

### Reintentos — SMTP caído (S-33)

5. **Given** un `IEnviadorEmail` que siempre lanza excepción (SMTP caído) **When** corren los ciclos
   avanzando el reloj **Then** el intento 1 falla y deja `proximo_intento_el = fallo + 1 min`; el intento 2 →
   `+ 5 min`; el intento 3 → `+ 15 min`; cada fallo incrementa `intentos` y guarda `ultimo_error`
   (`TipoDeExcepción: mensaje`, máximo 500 caracteres) **And** a los 59 s del primer fallo un ciclo **no** reintenta.
   Las esperas cuentan desde el fallo anterior.
6. **Given** el mismo mensaje con 3 fallos **When** el **4.º intento** (el 3.er reintento) también falla
   **Then** la fila queda con `fallo_definitivo_el = ahora`, `intentos = 4` **And** ningún ciclo posterior
   (avanzando 1 hora) lo vuelve a intentar.
   *Regla del contrato técnico (§Bandeja de salida, corregido tras la revisión, hallazgo I-9): 1 intento + 3 reintentos (a 1, 5 y 15 min, S-33); al fallar el 4.º intento se marca `FalloDefinitivoEl`.*

### Fallo definitivo y manejadores por Tipo

7. **Given** un mensaje de `Tipo = "aviso-bloqueo"`, `ReferenciaId = "T-77"`, un manejador registrado para
   `"aviso-bloqueo"` y otro para `"codigo-ingreso"` **When** el mensaje llega a fallo definitivo (AC 6)
   **Then** en el **mismo ciclo** se invoca **una vez** sólo el manejador de `"aviso-bloqueo"`, con
   `FalloDefinitivoEmail(MensajeId, "aviso-bloqueo", "T-77", Para, Motivo = ultimo_error)`, y la fila queda con
   `manejado_el = ahora`. Los cambios que el manejador agregue al contexto se guardan en la misma transacción que `manejado_el`.
8. **Given** un mensaje cuyo `Para` no es una dirección válida (`""`, `"paciente-sin-arroba"`, `"ana@"`)
   **When** se encola **Then** `Encolar` **no** lanza (el caso de uso no debe fallar por un email mal cargado)
   **And When** corre un ciclo **Then** el enviador **no** recibe llamadas, la fila queda con
   `fallo_definitivo_el = ahora`, `intentos = 0`, `ultimo_error = "Destinatario inválido"` (sin reintentos: reintentar no lo arregla).
9. **Given** un mensaje en fallo definitivo cuyo manejador lanza excepción la primera vez **When** corre el
   ciclo **Then** se revierten los cambios del manejador, `manejado_el` sigue `NULL`, se registra un log de nivel
   `Error` con el `MensajeId` **And When** corre el ciclo siguiente y el manejador responde bien **Then**
   `manejado_el` queda con la hora **And** el email **nunca** se vuelve a enviar (el enviador no suma llamadas).
   *(modo de falla de AC 7: un manejador roto no pierde al paciente ni reenvía el email)*
10. **Given** un mensaje en fallo definitivo con un `Tipo` sin manejadores registrados **When** corre el ciclo
    **Then** la fila queda con `manejado_el = ahora` y se registra un log `Warning` "sin manejador para el tipo {Tipo}". *(modo de falla de AC 7)*

### Concurrencia — dos instancias

11. **Given** un mensaje pendiente y **dos** `ProcesadorBandejaDeSalida` contra la misma base **When** la
    instancia A lo toma y está enviándolo, y en ese momento la instancia B corre un ciclo completo **Then** B
    no lo envía (lo saltea: `FOR UPDATE SKIP LOCKED`) y devuelve 0 procesados **And** al terminar A, el
    mensaje queda enviado **una sola vez** (total de llamadas a los dos enviadores = 1).

### Ciclo de vida del proceso de fondo

12. **Given** el proceso arrancado (`StartAsync`) y un envío en curso **When** el host se detiene
    (`StopAsync`, que cancela el `CancellationToken`) **Then** `StopAsync` termina en menos de 5 s sin
    excepción, no se registra ningún log `Error` **And** el envío cancelado **no** cuenta como intento: la fila
    sigue con `intentos = 0`, el mismo `proximo_intento_el` y `enviado_el = NULL` (la transacción se revierte).
13. **Given** el proceso arrancado y un primer ciclo que falla por una causa ajena a un mensaje (p. ej. la base
    no responde) **When** pasa el intervalo de 15 s del `TimeProvider` **Then** se registró un log `Error`, el
    proceso **sigue vivo** (`ExecuteTask` no terminó) y el ciclo siguiente envía el mensaje pendiente.
    *(modo de falla: con el comportamiento por defecto de .NET, una excepción no atrapada en `ExecuteAsync` detiene toda la aplicación)*

### Adaptador SMTP

14. **Given** Mailpit corriendo y `EnviadorEmailSmtp` configurado contra su puerto SMTP **When** se envía un
    `MensajeEmail` **Then** la API de Mailpit (`GET /api/v1/messages`) muestra 1 mensaje con ese asunto, ese
    destinatario y el remitente configurado.
15. **Given** `Email:Smtp:Host`/`Puerto` apuntando a un puerto donde no escucha nadie **When** se llama
    `EnviarAsync` **Then** lanza excepción (no la traga): es lo que el procesador convierte en reintento. *(modo de falla de AC 14)*

### Configuración SMTP

16. **Given** `Email:Smtp:Host` o `Email:Smtp:RemitenteDireccion` ausente, vacío o sólo espacios (p. ej. producción
    sin la variable de entorno) **When** arranca `HospitalTurnos.Web` **Then** el arranque se corta con
    `OptionsValidationException` cuyo mensaje contiene `Falta Email:Smtp:Host` / `Falta Email:Smtp:RemitenteDireccion`.
    *(modo de falla de AC 14: con valores de desarrollo en `appsettings.json`, producción arrancaría "bien" y cada
    email fallaría en silencio hasta el fallo definitivo, 21 minutos después — hallazgo I-10)*

### No-regresion

17. (declarativo) Las pruebas de las stories 1.1, 1.2 y 1.3 siguen en verde; la aplicación arranca con el
    proceso de fondo registrado (el smoke de Docker Compose de 1.1 sigue pasando). Lo controla R1.
18. (declarativo) Ningún archivo de `src/` fuera de `RelojHospital.cs` y de `Migraciones/` lee la hora del sistema: toda hora del procesador sale del `TimeProvider` (NFR-06). Lo controla R2.

### Tests (13 verificaciones + 2 de regresión)

| # | Test | AC | Nivel | Escenario | Resultado esperado |
|---|------|----|-------|-----------|--------------------|
| T1 | `Encolar_ConSaveChanges_QuedaPendienteParaEnvioInmediato` | 1 | integration | `Encolar` + `SaveChangesAsync` con contexto `turnos_app` y reloj fijo | 1 fila pendiente; `intentos = 0`; `creado_el = proximo_intento_el = ahora` |
| T2 | `Encolar_SinGuardar_NoPersisteNada` | 2 | integration | `Encolar` y se descarta el contexto | 0 filas |
| T3 | `ProcesarPendientes_EnvioExitoso_MarcaEnviadoYNoLoReenvia` | 3 | integration | Enviador falso que responde bien; dos ciclos | 1 llamada con los mismos datos; `enviado_el = ahora`; `intentos = 1` |
| T4 | `ProcesarPendientes_SmtpCaido_ReprogramaA1_5Y15Minutos` | 4,5 | integration | Enviador que siempre lanza; ciclos con `Advance(59s)`, `Advance(1s)`, `Advance(5min)` | Llamadas: 1, 1, 2, 3; `proximo_intento_el` = +1, +5, +15 min del último fallo; `ultimo_error` con el tipo de excepción |
| T5 | `ProcesarPendientes_FallaElTercerReintento_MarcaFalloDefinitivoEInvocaManejadorDelTipo` | 6,7 | integration | Igual que T4 + `Advance(15min)` (4.º intento falla) + `Advance(1h)`; manejadores para `aviso-bloqueo` y `codigo-ingreso` | 4 llamadas en total; `fallo_definitivo_el` y `manejado_el` con hora; sólo el manejador de `aviso-bloqueo` invocado 1 vez con `ReferenciaId = "T-77"` |
| T6 | `ProcesarPendientes_DestinatarioInvalido_FalloDefinitivoSinIntentarEnviar` | 8,10 | integration | `[Theory]` `""`, `"paciente-sin-arroba"`, `"ana@"`; `Tipo` sin manejadores | `Encolar` no lanza; 0 llamadas al enviador; `ultimo_error = "Destinatario inválido"`; `manejado_el` con hora; 1 log `Warning` |
| T7 | `ProcesarPendientes_ManejadorFalla_ReintentaManejadorSinReenviarEmail` | 9 | integration | Mensaje llevado a fallo definitivo; manejador que lanza la 1.ª vez y anda la 2.ª | Tras ciclo 1: `manejado_el` NULL + 1 log `Error`; tras ciclo 2: `manejado_el` con hora; llamadas al enviador sin cambio |
| T8 | `ProcesarPendientes_DosInstanciasALaVez_EnviaUnaSolaVez` | 11 | integration | A se bloquea dentro de `EnviarAsync` (compuerta); B corre un ciclo completo; se libera A | B devuelve 0; A devuelve 1; total de llamadas = 1; `enviado_el` con hora |
| T9 | `StopAsync_EnvioEnCurso_TerminaOrdenadoYElMensajeQuedaPendiente` | 12 | integration | `StartAsync`; enviador que espera hasta que lo cancelen; `StopAsync` | Termina < 5 s; 0 logs `Error`; `intentos = 0`; `proximo_intento_el` sin cambio; `enviado_el` NULL |
| T10 | `ExecuteAsync_FallaUnCiclo_RegistraErrorYSigueSondeando` | 13 | integration | Fábrica de scopes que lanza la 1.ª vez; mensaje pendiente; `Advance(15s)` | 1 log `Error`; `ExecuteTask.IsCompleted == false`; el mensaje se envía en el ciclo siguiente |
| T11 | `EnviarAsync_ContraMailpit_ElMensajeLlegaConAsuntoYDestinatario` | 14 | integration | Contenedor `axllent/mailpit` con Testcontainers; `EnviadorEmailSmtp` contra su puerto 1025 | `GET /api/v1/messages`: `total = 1`, asunto, `To[0].Address` y `From.Address` esperados |
| T12 | `EnviarAsync_ServidorInaccesible_LanzaExcepcion` | 15 | integration | `Host = "127.0.0.1"`, `Puerto = 1` (nadie escucha) | `await Assert.ThrowsAnyAsync<Exception>(...)` |
| T13 | `Arranque_SinConfiguracionSmtp_FallaConMensajeQueNombraLaClave` | 16 | integration | `[Theory]` 2 casos: `Email:Smtp:Host = ""`; `Email:Smtp:RemitenteDireccion = ""`. `HospitalWebApplicationFactory` (de 1.1) sobre la `PostgresFixture` de Web.Tests | `CreateClient()` lanza; la excepción (o su `InnerException`) es `OptionsValidationException` y su mensaje contiene `Falta Email:Smtp:Host` / `Falta Email:Smtp:RemitenteDireccion` |
| R1 | Todas las pruebas de 1.1, 1.2 y 1.3 (`--filter "Story=1.1\|Story=1.2\|Story=1.3"`) + smoke E1 de 1.1 (`scripts/smoke-compose.sh`) | 17 | regresión | Se vuelven a correr sin cambios, con el proceso de fondo registrado y la configuración SMTP nueva | Siguen en verde; E1 sale con 0 |
| R2 | `CodigoFuente_ArchivosDeSrc_NoLeenLaHoraDelSistema` (story 1.2, T5) | 18 | unit | Se vuelve a correr sin cambios con los archivos nuevos de `Email/` | Sigue pasando (0 apariciones) |

Son 13 pruebas contra las 8 estimadas en `docs/05-epicas-y-plan.md:41`: las 5 extra cubren modos de falla pedidos
para esta story (manejador roto, dos instancias, cierre ordenado, ciclo que falla, arranque sin configuración SMTP).

Traits: cada test nuevo lleva `[Trait("Story", "1.4")]` y un `[Trait("AC", "n")]` por AC de su fila. R1 y R2 conservan
sus Traits de 1.1/1.2/1.3.

#### Given/Fixture

- **Todas las de integración con base** — `[Collection("Postgres")]` + `PostgresFixture`: la **misma** fixture
  de la story 1.1 que copió la 1.3 en `tests/HospitalTurnos.Aplicacion.Tests/PostgresFixture.cs` (contrato §Pruebas:
  `01-roles.sh`, `CadenaDuenio`, `CadenaApp`, migraciones como dueño). Esta story **no** la modifica.
  **Al empezar cada prueba**: `await fixture.LimpiarBandejaAsync()`, un método de extensión de esta story
  (`Email/LimpiezaDeBandeja.cs`) que hace `TRUNCATE mensajes_salientes RESTART IDENTITY` con `fixture.CadenaDuenio`.
  Sin esto, los pendientes que deja una prueba (p. ej. T4) los procesa la siguiente. La colección hace que las
  clases que la usan corran de a una, así que vaciar la tabla no pisa a otra prueba en paralelo.
- **Registro técnico** — `RegistroEnMemoria<T> : ILogger<T>` en `Ayudas/`: el mismo doble escrito a mano de la story 1.2
  (mismo nombre y forma: guarda `(LogLevel, Exception?, string mensaje)` en una lista), copiado a este proyecto porque
  en el sprint 1 no hay proyecto de utilidades compartido.
- **Reloj** — `var reloj = new FakeTimeProvider(new DateTimeOffset(2026, 10, 12, 11, 0, 0, TimeSpan.Zero));`
  `reloj.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Cordoba"));`
- **Procesador de prueba** — helper `CrearProcesador(reloj, enviador, logger, params IManejadorFalloDefinitivo[] manejadores)` (con `logger` = `RegistroEnMemoria<ProcesadorBandejaDeSalida>`)
  que arma un `ServiceCollection` propio: `AddDbContext<HospitalDbContext>(o => o.UseNpgsql(fixture.CadenaApp))`,
  `AddSingleton<IEnviadorEmail>(enviador)`, cada manejador como `IManejadorFalloDefinitivo`, y devuelve
  `new ProcesadorBandejaDeSalida(provider.GetRequiredService<IServiceScopeFactory>(), reloj, logger)`.
  En T8 se llama dos veces con dos enviadores distintos: son dos "instancias" contra la misma base.
- **T8** — `EnviadorEmailFalso` de A con compuerta: `EnviarAsync` incrementa `Llamadas` y espera un
  `TaskCompletionSource`. Secuencia: `var tareaA = procA.ProcesarPendientesAsync(ct);` → esperar
  (sondeo, máx. 5 s) a que `enviadorA.Llamadas == 1` → `var nB = await procB.ProcesarPendientesAsync(ct);` →
  `enviadorA.Liberar();` → `var nA = await tareaA;`. Es determinista: no depende de cuánto tarde nada.
- **T9 / T10** — Con `FakeTimeProvider`, `Advance` sólo despierta un `Task.Delay` que **ya** se registró.
  En T10 avanzar en bucle: `while (enviador.Llamadas == 0) { reloj.Advance(TimeSpan.FromSeconds(15)); await Task.Delay(10); }`
  con tope de 5 s reales; si no, la prueba se cuelga cuando el `Advance` llega antes que el `Delay`.
- **T13** — Web.Tests, con la fixture y la fábrica de la story 1.1 (mismo esquema que `Arranque/ArranqueTests.cs`, T4 de 1.1).
  La clave vacía se inyecta con el mismo mecanismo con que la fábrica inyecta las cadenas de conexión (última fuente
  de configuración: gana a `appsettings*.json`). Las migraciones corren antes de la validación de opciones: por eso
  necesita la base de la fixture y no una cadena falsa.
- **`HospitalWebApplicationFactory`** (de 1.1) — desde esta story inyecta también una configuración SMTP válida
  (`Email:Smtp:Host = 127.0.0.1`, `Puerto = 1`, `Seguridad = None`, `RemitenteNombre`, `RemitenteDireccion =
  turnos@lasacacias.example`). Sin eso, toda prueba de Web.Tests que arranque la aplicación fallaría por AC 16.
  Con `Puerto = 1` nadie escucha: un email encolado en una prueba de Web sólo suma reintentos, nunca sale.
- **T11** — `new ContainerBuilder().WithImage("axllent/mailpit").WithPortBinding(1025, true).WithPortBinding(8025, true)
  .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8025).ForPath("/api/v1/messages"))).Build()`
  (paquete `Testcontainers`, que ya viene con `Testcontainers.PostgreSql`). Usar el mismo tag de imagen que `docker-compose.yml`.

## Tasks / Subtasks

- [ ] [SEQ] Task 1: Salvaguarda de estado (AC: todos)
  - [ ] Confirmar que 1.1 está implementada: proyectos de las 4 capas, `HospitalDbContext`, carpeta de migraciones, `docker-compose.yml` con `db` y `mailpit`, roles `turnos_owner`/`turnos_app`.
  - [ ] Confirmar que 1.2 está implementada: `RelojHospital` registrado como `TimeProvider` singleton.
  - [ ] Confirmar que 1.3 está implementada (`depends_on`): existen `tests/HospitalTurnos.Aplicacion.Tests/PostgresFixture.cs` (copia idéntica de la de Web.Tests salvo el `namespace`), `ColeccionPostgres.cs` y la migración `AgregarRegistroAuditoria`. Si falta algo → DETENERSE (las dos stories agregan migración y tocan el snapshot: en paralelo chocan).
  - [ ] Confirmar que existe `.env.ejemplo` (lo crea 1.1) y cómo inyecta la configuración `HospitalWebApplicationFactory` (anotar el mecanismo).
  - [ ] Confirmar si `MailKit` ya está referenciado en `HospitalTurnos.Infraestructura.csproj` y versionado en `Directory.Packages.props`.
  - [ ] Si CUALQUIER asunción no coincide → DETENERSE → corregir la story primero.
- [ ] [SEQ] Task 2: Puertos en Aplicación (AC: 1, 2, 7)
  - [ ] `MensajeEmail`, `IBandejaDeSalida`, `IEnviadorEmail`, `IManejadorFalloDefinitivo` + `FalloDefinitivoEmail`.
- [ ] [SEQ] Task 3: Entidad, configuración y migración (AC: 1, 5, 6)
  - [ ] `MensajeSaliente` con la política de reintentos dentro (`RegistrarFallo`).
  - [ ] `MensajeSalienteConfiguracion`, `DbSet<MensajeSaliente> MensajesSalientes`, índice parcial de pendientes.
  - [ ] `dotnet ef migrations add AgregarBandejaDeSalida …` + SQL de permisos al final de `Up`.
- [ ] [SEQ] Task 4: `BandejaDeSalidaEnBase` (AC: 1, 2, 8) + T1, T2.
- [ ] [SEQ] Task 5: `ProcesadorBandejaDeSalida` (AC: 3-13) + dobles de prueba (`EnviadorEmailFalso`, `ManejadorFalloDefinitivoFalso`, `RegistroEnMemoria`) + `LimpiezaDeBandeja` + T3-T10.
- [ ] [PAR] Task 6: `EnviadorEmailSmtp` + `OpcionesSmtp` + configuración (AC: 14, 15, 16) + T11, T12.
  - [ ] `Email:Smtp` **sólo** en `appsettings.Development.json` (sin usuario ni clave). `appsettings.json` **no** lleva la sección.
  - [ ] `docker-compose.yml`, servicio `web`: las claves `Email__Smtp__*` de Dev Notes (punto 8); usuario y clave sólo desde `.env`.
  - [ ] `.env.ejemplo`: agregar `SMTP_USUARIO=` y `SMTP_CLAVE=` vacías, con un comentario de para qué son.
- [ ] [SEQ] Task 7: Registro en DI (`Program.cs`), fábrica de Web.Tests y verificación (AC: 16, 17, 18)
  - [ ] `HospitalWebApplicationFactory`: inyectar la configuración SMTP válida (Given/Fixture) y T13 en `Arranque/ConfiguracionSmtpTests.cs`.
  - [ ] Correr "Como verificar que funciona"; contar 13 métodos con `Story=1.4` y R1, R2 en verde.

### Task Dependencies

T1 → T2 → T3 → T4 → T5 → T7
T6 puede hacerse en paralelo con T4-T5 (sólo depende de T2: usa `MensajeEmail` e `IEnviadorEmail`).

## Dev Notes

### Que existe hoy

#### En hospital-turnos (verificado 2026-10-09)

- Sólo existen el código descartable `src/HospitalTurnos.Api/` y `tests/HospitalTurnos.Tests/` (lo borra la story 1.1) y la documentación. No hay `docker-compose.yml` ni `Directory.Packages.props` en la raíz todavía.
- **No existe todavía (lo crea la story 1.1):** las 4 capas, `HospitalDbContext`, migraciones, `docker-compose.yml` con `db` (5432) y `mailpit` (1025 SMTP / 8025 web), roles `turnos_owner`/`turnos_app`, gestión central de paquetes con `MailKit` y `Testcontainers.PostgreSql` en la lista.
- **No existe todavía (lo crea la story 1.2):** `RelojHospital : TimeProvider` (singleton, zona Córdoba).
- **Lo crea la story 1.3 (dependencia):** la copia de `PostgresFixture` en Aplicacion.Tests (idéntica a la de Web.Tests de 1.1) y `[CollectionDefinition("Postgres")]` en `ColeccionPostgres.cs`.
- **Lo crea la story 1.1:** `.env.ejemplo` (variables sin valor real; `.env` está en `.gitignore`) y `HospitalWebApplicationFactory`.
- **No existe (lo crea esta story):** todo `Email/` en Aplicación e Infraestructura y la tabla `mensajes_salientes`.

#### Contrato que esta story implementa (`docs/stories/00-contrato-tecnico.md` §Bandeja de salida y §Pruebas)

`MensajeSaliente` en `Infraestructura/Email/`; `IBandejaDeSalida.Encolar(MensajeEmail)` agrega sin guardar;
`MensajeEmail(Para, Asunto, CuerpoTexto, CuerpoHtml, Tipo, ReferenciaId?)`; `ProcesadorBandejaDeSalida : BackgroundService`
cada 15 s; 1 intento + 3 reintentos (1/5/15 min), al fallar el 4.º intento `FalloDefinitivoEl` + `IManejadorFalloDefinitivo` por `Tipo`
(los usa la story 7.2); `IEnviadorEmail` → `EnviadorEmailSmtp` (MailKit), config `Email:Smtp:*`.
La arquitectura §7.4 llama a la tabla `MensajesSalientes`; por la convención `snake_case` del contrato se llama `mensajes_salientes`.

#### Nombres que esta story propuso y que el contrato ya fija (hallazgo I-9)

Primero se cambia el contrato, después la story: estos nombres están en el contrato técnico y **no** se cambian.

- `IEnviadorEmail` e `IManejadorFalloDefinitivo` van en `src/HospitalTurnos.Aplicacion/Email/` (puertos, como `IBandejaDeSalida`).
- Implementación de `IBandejaDeSalida`: `BandejaDeSalidaEnBase` (Infraestructura/Email). Opciones SMTP: `OpcionesSmtp`.
- Columna nueva `ManejadoEl`: separa "el email se dio por perdido" de "el manejador ya actuó", para reintentar el manejador sin reenviar el email (AC 9).

### Patron de referencia verificado

**Patron (parcial)**: el proyecto guía resuelve los dobles de prueba hechos a mano, la transacción todo-o-nada y el adaptador que no traga errores; no tiene bandeja de salida, proceso de fondo, reloj inyectado ni bloqueo de filas.

| Proyecto | Ancla `archivo:linea` (o comando) | Que se verifico exactamente |
|---|---|---|
| changomas-testing-csharp | `tests/ChangoMas.Tests.Unit/Ayudas/RepositorioVentasEnMemoria.cs:15` y `:20` | Abrí el archivo: fake escrito a mano que implementa el puerto y además "espía" (`VecesQueSeGuardo`). `EnviadorEmailFalso` y `ManejadorFalloDefinitivoFalso` siguen esa forma (contador `Llamadas`, lista de lo recibido). No se usa Moq: no está en la lista de paquetes del contrato. |
| changomas-testing-csharp | `src/ChangoMas.Infraestructura/PasarelaPagoHttp.cs:26-28` | Abrí el archivo: comentario "Tragarse esto con un catch vacío es el fallo silencioso" + `EnsureSuccessStatusCode()`. `EnviadorEmailSmtp` tampoco atrapa nada (AC 15); quien decide qué hacer con el error es el procesador. |
| changomas-testing-csharp | `src/ChangoMas.Infraestructura/RepositorioVentasSqlite.cs:46-48` y `:88` | Abrí el archivo: `BeginTransactionAsync` … `CommitAsync`. El procesador abre una transacción **por mensaje** con el mismo criterio. |
| changomas-testing-csharp | `tests/ChangoMas.Tests.Integracion/CajaWebApplicationFactory.cs:28-32` | Abrí el archivo: la dependencia externa (WireMock) se levanta en `InitializeAsync`. T11 levanta Mailpit igual, con Testcontainers. |
| changomas-testing-csharp | `grep -rniE "BackgroundService\|TimeProvider\|SKIP LOCKED\|outbox\|bandeja" --include='*.cs' --include='*.csproj' .` | 0 resultados (corrido 2026-10-09). |

**No aplica para**: bandeja de salida, `BackgroundService`, `FOR UPDATE SKIP LOCKED` y `FakeTimeProvider`. Busqué en el proyecto guía (grep de arriba, 0 resultados) y no hay plugins hermanos (contrato §Patrón de referencia). Esta story crea el patrón por primera vez; lo reusan 6.1 (código de ingreso), 6.5 (confirmación) y 7.2 (avisos de bloqueo).

### Decisiones de diseño

- **Dos instancias a la vez → `SELECT … FOR UPDATE SKIP LOCKED`, de a un mensaje por transacción.**
  Cada instancia toma una fila y la deja bloqueada mientras la envía; la otra instancia, en lugar de esperar,
  la saltea y sigue con la siguiente. Alternativas descartadas: (a) un candado global (`pg_advisory_lock`):
  funciona, pero una instancia queda ociosa siempre; (b) "reservar" la fila con una columna `tomado_hasta` y
  confirmar antes de enviar: no deja la transacción abierta durante el envío, pero agrega vencimientos de
  reserva y otra columna para un volumen de ~400 turnos por día. Con `SKIP LOCKED` la base hace el trabajo y
  si el proceso muere a mitad de un envío, la transacción se revierte sola y el mensaje vuelve a quedar pendiente.
  Costo aceptado: la transacción queda abierta mientras dura el envío; se acota con un timeout SMTP de 30 s.
- **Garantía "al menos una vez", no "exactamente una vez".** Si el servidor SMTP acepta el email y el proceso
  muere antes del `COMMIT`, el email se reenvía. Es inevitable sin cooperación del receptor y es preferible a
  perder un aviso. Para dos instancias **sanas** (AC 11) sí se envía una sola vez.
- **Destinatario inválido = fallo definitivo inmediato** (AC 8): reintentar no lo arregla, y el paciente tiene
  que llegar cuanto antes a "Pacientes para avisar". `Encolar` no valida la dirección para que un email mal
  cargado no haga fallar la reserva o el bloqueo (ADR-05).
- **El manejador corre en una segunda fase, con su propia transacción** (AC 7, 9, 10). Si corriera en la misma
  transacción que el fallo y lanzara excepción, el rollback volvería el mensaje a pendiente y se reenviaría.
  Con `manejado_el` separado, el email no se reenvía y el manejador se reintenta en cada ciclo hasta que funcione.
- **La hora la decide la aplicación, no la base**: la consulta compara contra un parámetro `@ahora` sacado del
  `TimeProvider`, **nunca** contra `now()` de Postgres. Si no, `FakeTimeProvider` no controla los reintentos y las pruebas no son deterministas.
- **Prueba contra Mailpit incluida (T11)**: es la única que prueba el código de MailKit (conexión, remitente,
  cuerpo texto + HTML). Sin ella, `EnviadorEmailSmtp` llega a producción sin haber enviado nunca un email.
  Usa Testcontainers (sin paquete nuevo) y no depende de que Docker Compose esté levantado.

### Que hay que hacer (la delta)

1. **Aplicación** (`src/HospitalTurnos.Aplicacion/Email/`):
   ```csharp
   public sealed record MensajeEmail(string Para, string Asunto, string CuerpoTexto, string CuerpoHtml,
                                     string Tipo, string? ReferenciaId);

   public interface IBandejaDeSalida
   {
       /// Agrega el mensaje SIN guardar: lo persiste el SaveChangesAsync del caso de uso (arquitectura §7.4).
       void Encolar(MensajeEmail mensaje);
   }

   public interface IEnviadorEmail
   {
       /// Lanza excepción si no pudo entregar al servidor SMTP. No atrapa nada.
       Task EnviarAsync(MensajeEmail mensaje, CancellationToken ct);
   }

   public sealed record FalloDefinitivoEmail(long MensajeId, string Tipo, string? ReferenciaId, string Para, string Motivo);

   public interface IManejadorFalloDefinitivo
   {
       string Tipo { get; }   // p. ej. "aviso-bloqueo" (lo registra la story 7.2)
       /// Agrega cambios al contexto SIN guardar; el procesador guarda junto con ManejadoEl.
       Task ManejarAsync(FalloDefinitivoEmail fallo, CancellationToken ct);
   }
   ```
2. **Entidad** `MensajeSaliente` (Infraestructura/Email), setters privados:
   `Id` (long), `Para`, `Asunto`, `CuerpoTexto`, `CuerpoHtml`, `Tipo`, `ReferenciaId?`, `CreadoEl`, `Intentos`,
   `ProximoIntentoEl`, `EnviadoEl?`, `FalloDefinitivoEl?`, `ManejadoEl?`, `UltimoError?` (todas las marcas `DateTimeOffset` UTC).
   ```csharp
   // Esperas entre intentos (S-33). Fallo 1 → +1 min, 2 → +5, 3 → +15, 4 → definitivo.
   public static readonly TimeSpan[] EsperasEntreReintentos =
       [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15)];

   public void RegistrarFallo(DateTimeOffset ahora, string error)
   {
       Intentos++;
       UltimoError = error.Length > 500 ? error[..500] : error;
       if (Intentos > EsperasEntreReintentos.Length) FalloDefinitivoEl = ahora;
       else ProximoIntentoEl = ahora + EsperasEntreReintentos[Intentos - 1];
   }
   // MarcarEnviado(ahora): Intentos++; EnviadoEl = ahora.
   // MarcarFalloDefinitivo(ahora, motivo): FalloDefinitivoEl = ahora; UltimoError = motivo.
   // MarcarManejado(ahora): ManejadoEl = ahora.
   ```
3. **Configuración EF**: **sin** `ToTable` (la convención `snake_case` de 1.1 ya da `mensajes_salientes` a partir del `DbSet`, 1.1 AC 8); largos `Para` 320, `Asunto` 200, `Tipo` 50,
   `ReferenciaId` 100, `UltimoError` 500; cuerpos `text`. Índice parcial para la consulta del ciclo:
   `HasIndex(m => m.ProximoIntentoEl).HasFilter("enviado_el IS NULL AND fallo_definitivo_el IS NULL")`.
   `public DbSet<MensajeSaliente> MensajesSalientes => Set<MensajeSaliente>();` en `HospitalDbContext`.
4. **Migración `AgregarBandejaDeSalida`** — al final de `Up`:
   ```sql
   -- La aplicación inserta (Encolar) y actualiza (procesador); nunca borra.
   REVOKE ALL ON TABLE mensajes_salientes FROM turnos_app;
   GRANT SELECT, INSERT, UPDATE ON TABLE mensajes_salientes TO turnos_app;
   DO $$ BEGIN
     EXECUTE format('GRANT USAGE ON SEQUENCE %s TO turnos_app',
                    pg_get_serial_sequence('mensajes_salientes', 'id'));
   END $$;
   ```
   (`SELECT … FOR UPDATE` necesita privilegio `UPDATE`: ya está.)
5. **`BandejaDeSalidaEnBase(HospitalDbContext, TimeProvider)`**: `ArgumentNullException.ThrowIfNull(mensaje)`;
   `Add(new MensajeSaliente(...))` con `CreadoEl = ProximoIntentoEl = reloj.GetUtcNow()`. No valida `Para` (AC 8). No guarda.
6. **`ProcesadorBandejaDeSalida(IServiceScopeFactory, TimeProvider, ILogger<ProcesadorBandejaDeSalida>) : BackgroundService`**
   con `public Task<int> ProcesarPendientesAsync(CancellationToken ct)` (un ciclo; público para las pruebas) — ver Implementación concreta.
7. **`EnviadorEmailSmtp(IOptions<OpcionesSmtp>)`** y **`OpcionesSmtp`**: `Host`, `Puerto`, `Usuario?`, `Clave?`,
   `Seguridad` (`MailKit.Security.SecureSocketOptions`, `None` en desarrollo, `StartTls` en producción),
   `RemitenteNombre`, `RemitenteDireccion`. `ValidateDataAnnotations().ValidateOnStart()` con
   `[Required(ErrorMessage = "Falta Email:Smtp:Host")]` en `Host` y `[Required(ErrorMessage = "Falta Email:Smtp:RemitenteDireccion")]`
   en `RemitenteDireccion` (falla al arrancar, no al primer email; `[Required]` ya rechaza vacío y sólo espacios — AC 16).
8. **DI en `Program.cs`**:
   ```csharp
   builder.Services.AddOptions<OpcionesSmtp>().BindConfiguration("Email:Smtp").ValidateDataAnnotations().ValidateOnStart();
   builder.Services.AddScoped<IBandejaDeSalida, BandejaDeSalidaEnBase>();
   builder.Services.AddTransient<IEnviadorEmail, EnviadorEmailSmtp>();
   builder.Services.AddHostedService<ProcesadorBandejaDeSalida>();
   ```
   **Dónde vive cada valor** (contrato §Secretos: ninguna contraseña versionada; hallazgo I-10):

   | Lugar | Qué lleva |
   |---|---|
   | `appsettings.json` | **nada** de `Email:Smtp`. Si falta la configuración, el arranque se corta (AC 16), igual que con las cadenas de conexión (1.1 AC 7) |
   | `appsettings.Development.json` | `"Email": { "Smtp": { "Host": "localhost", "Puerto": 1025, "Seguridad": "None", "RemitenteNombre": "Hospital Las Acacias", "RemitenteDireccion": "turnos@lasacacias.example" } }` — Mailpit no pide usuario ni clave |
   | `docker-compose.yml`, servicio `web` | `Email__Smtp__Host: mailpit`, `Email__Smtp__Puerto: "1025"`, `Email__Smtp__Seguridad: None`, `Email__Smtp__RemitenteNombre: Hospital Las Acacias`, `Email__Smtp__RemitenteDireccion: turnos@lasacacias.example`, `Email__Smtp__Usuario: ${SMTP_USUARIO:-}`, `Email__Smtp__Clave: ${SMTP_CLAVE:-}` |
   | `.env` (no versionado) / `.env.ejemplo` (versionado, sin valores) | `SMTP_USUARIO=` y `SMTP_CLAVE=` |
   | Producción | todas las claves `Email__Smtp__*` por variables de entorno, con `Seguridad = StartTls` |

### Implementacion concreta

```csharp
public sealed class ProcesadorBandejaDeSalida(
    IServiceScopeFactory scopes, TimeProvider reloj, ILogger<ProcesadorBandejaDeSalida> logger) : BackgroundService
{
    public static readonly TimeSpan IntervaloDeSondeo = TimeSpan.FromSeconds(15);
    private const int LoteMaximo = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcesarPendientesAsync(stoppingToken); }
            // Por qué atrapar todo: desde .NET 8, una excepción que escapa de ExecuteAsync DETIENE la aplicación entera.
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Falló un ciclo de la bandeja de salida; se reintenta en {Intervalo}", IntervaloDeSondeo);
            }
            // El Delay con TimeProvider es lo que permite a FakeTimeProvider controlar el sondeo.
            await Task.Delay(IntervaloDeSondeo, reloj, stoppingToken);
        }
    }

    public async Task<int> ProcesarPendientesAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var enviador = scope.ServiceProvider.GetRequiredService<IEnviadorEmail>();
        var procesados = 0;

        // Fase 1: enviar. Una transacción por mensaje.
        for (var i = 0; i < LoteMaximo; i++)
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var ahora = reloj.GetUtcNow();
            // ToListAsync directo: si se compone (FirstOrDefault), EF lo envuelve en una subconsulta.
            var mensaje = (await db.MensajesSalientes.FromSql($"""
                SELECT * FROM mensajes_salientes
                WHERE enviado_el IS NULL AND fallo_definitivo_el IS NULL AND proximo_intento_el <= {ahora}
                ORDER BY proximo_intento_el, id
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """).ToListAsync(ct)).SingleOrDefault();
            if (mensaje is null) break;

            if (!EsDestinatarioValido(mensaje.Para))
                mensaje.MarcarFalloDefinitivo(ahora, "Destinatario inválido");
            else
            {
                try
                {
                    await enviador.EnviarAsync(mensaje.AMensajeEmail(), ct);
                    mensaje.MarcarEnviado(reloj.GetUtcNow());
                }
                // Si el host se está apagando, la excepción sigue: la transacción se revierte y no cuenta como intento.
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    mensaje.RegistrarFallo(reloj.GetUtcNow(), $"{ex.GetType().Name}: {ex.Message}");
                    logger.LogWarning("Falló el envío del mensaje {MensajeId} ({Tipo}), intento {Intentos}",
                        mensaje.Id, mensaje.Tipo, mensaje.Intentos);   // sin Para ni cuerpo: son datos personales
                }
            }
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            db.ChangeTracker.Clear();
            procesados++;
        }

        // Fase 2: manejadores de fallo definitivo. Los que fallan en este ciclo se excluyen
        // para no volver a tomarlos en el mismo bucle (si no, un manejador roto lo cuelga).
        var fallidosEsteCiclo = new List<long>();
        // ... mismo esquema: SELECT … WHERE fallo_definitivo_el IS NOT NULL AND manejado_el IS NULL
        //     AND NOT (id = ANY({fallidosEsteCiclo.ToArray()})) ORDER BY id LIMIT 1 FOR UPDATE SKIP LOCKED
        //     → manejadores = scope.ServiceProvider.GetServices<IManejadorFalloDefinitivo>().Where(m => m.Tipo == mensaje.Tipo)
        //     → ninguno: LogWarning("sin manejador para el tipo {Tipo}") ; si hay: await cada ManejarAsync(...)
        //     → mensaje.MarcarManejado(ahora); SaveChanges; Commit
        //     → si un manejador lanza: Rollback, ChangeTracker.Clear(), LogError(ex, … {MensajeId}), fallidosEsteCiclo.Add(id)
        return procesados;
    }

    // MimeKit acepta "paciente-sin-arroba" como dirección local: por eso se exige "@" con algo a cada lado.
    private static bool EsDestinatarioValido(string para) =>
        MailboxAddress.TryParse(para, out var dir)
        && dir.Address.IndexOf('@') is > 0 and var arroba
        && arroba < dir.Address.Length - 1;
}
```

Los manejadores se resuelven del **mismo scope** que `db`: si usan repositorios registrados como scoped, escriben en el mismo `HospitalDbContext` y se guardan en la misma transacción que `manejado_el`.

`EnviadorEmailSmtp.EnviarAsync`:
```csharp
var mime = new MimeMessage();
mime.From.Add(new MailboxAddress(op.RemitenteNombre, op.RemitenteDireccion));
mime.To.Add(MailboxAddress.Parse(mensaje.Para));
mime.Subject = mensaje.Asunto;
mime.Body = new BodyBuilder { TextBody = mensaje.CuerpoTexto, HtmlBody = mensaje.CuerpoHtml }.ToMessageBody();
using var smtp = new SmtpClient { Timeout = 30_000 };   // acota cuánto queda abierta la transacción del procesador
await smtp.ConnectAsync(op.Host, op.Puerto, op.Seguridad, ct);
if (!string.IsNullOrEmpty(op.Usuario)) await smtp.AuthenticateAsync(op.Usuario, op.Clave, ct);
await smtp.SendAsync(mime, ct);
await smtp.DisconnectAsync(true, ct);
```

**Comando de la migración:**
```bash
cd ~/SourceCode/hospital-turnos
dotnet ef migrations add AgregarBandejaDeSalida \
  --project src/HospitalTurnos.Infraestructura \
  --startup-project src/HospitalTurnos.Web \
  --output-dir Persistencia/Migraciones
```

**`LimpiezaDeBandeja`** (`tests/HospitalTurnos.Aplicacion.Tests/Email/LimpiezaDeBandeja.cs`) — extensión, **no** un método
nuevo de `PostgresFixture` (la fixture es la misma copia en los dos proyectos de pruebas y no se toca):
```csharp
// La bandeja sí se puede vaciar entre pruebas (a diferencia de registros_auditoria, que tiene disparador).
// Sólo esta tabla: un limpiador genérico chocaría con el disparador de la auditoría (story 1.3).
public static async Task LimpiarBandejaAsync(this PostgresFixture fixture)
{
    await using var con = new NpgsqlConnection(fixture.CadenaDuenio);
    await con.OpenAsync();
    await new NpgsqlCommand("TRUNCATE mensajes_salientes RESTART IDENTITY;", con).ExecuteNonQueryAsync();
}
```

### Archivos a crear/modificar

| Accion | Archivo |
|--------|---------|
| Crear | `src/HospitalTurnos.Aplicacion/Email/MensajeEmail.cs` |
| Crear | `src/HospitalTurnos.Aplicacion/Email/IBandejaDeSalida.cs` |
| Crear | `src/HospitalTurnos.Aplicacion/Email/IEnviadorEmail.cs` |
| Crear | `src/HospitalTurnos.Aplicacion/Email/IManejadorFalloDefinitivo.cs` (incluye `FalloDefinitivoEmail`) |
| Crear | `src/HospitalTurnos.Infraestructura/Email/MensajeSaliente.cs` |
| Crear | `src/HospitalTurnos.Infraestructura/Email/BandejaDeSalidaEnBase.cs` |
| Crear | `src/HospitalTurnos.Infraestructura/Email/ProcesadorBandejaDeSalida.cs` |
| Crear | `src/HospitalTurnos.Infraestructura/Email/EnviadorEmailSmtp.cs` |
| Crear | `src/HospitalTurnos.Infraestructura/Email/OpcionesSmtp.cs` |
| Crear | `src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/MensajeSalienteConfiguracion.cs` |
| Crear | `src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/AAAAMMDDHHMMSS_AgregarBandejaDeSalida.cs` (prefijo lo pone `dotnet ef`) |
| Crear | `tests/HospitalTurnos.Aplicacion.Tests/Email/BandejaDeSalidaTests.cs` (T1, T2) |
| Crear | `tests/HospitalTurnos.Aplicacion.Tests/Email/ProcesadorBandejaDeSalidaTests.cs` (T3-T10) |
| Crear | `tests/HospitalTurnos.Aplicacion.Tests/Email/EnviadorEmailSmtpTests.cs` (T11, T12) |
| Crear | `tests/HospitalTurnos.Aplicacion.Tests/Email/Dobles/EnviadorEmailFalso.cs` (modos: responde bien / siempre lanza / compuerta / espera cancelación; cuenta `Llamadas` y guarda lo recibido) |
| Crear | `tests/HospitalTurnos.Aplicacion.Tests/Email/Dobles/ManejadorFalloDefinitivoFalso.cs` (`Tipo` configurable, guarda los `FalloDefinitivoEmail`, opción "falla la primera vez") |
| Crear | `tests/HospitalTurnos.Aplicacion.Tests/Ayudas/RegistroEnMemoria.cs` (el mismo doble de la story 1.2: `ILogger<T>` que guarda nivel, excepción y mensaje; evita sumar un paquete fuera del contrato) |
| Crear | `tests/HospitalTurnos.Aplicacion.Tests/Email/LimpiezaDeBandeja.cs` (extensión `LimpiarBandejaAsync`) |
| Crear | `tests/HospitalTurnos.Web.Tests/Arranque/ConfiguracionSmtpTests.cs` (T13) |
| Modificar | `src/HospitalTurnos.Infraestructura/Persistencia/HospitalDbContext.cs` (`MensajesSalientes`) |
| Modificar | `src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/HospitalDbContextModelSnapshot.cs` (lo regenera `dotnet ef`) |
| Modificar | `src/HospitalTurnos.Infraestructura/HospitalTurnos.Infraestructura.csproj` (`MailKit`, si 1.1 no lo agregó) |
| Modificar | `src/HospitalTurnos.Web/Program.cs` (registro DI) |
| Modificar | `src/HospitalTurnos.Web/appsettings.Development.json` (`Email:Smtp`, sin usuario ni clave) |
| Modificar | `docker-compose.yml` (servicio `web`: claves `Email__Smtp__*`, ver Dev Notes punto 8) |
| Modificar | `.env.ejemplo` (`SMTP_USUARIO=`, `SMTP_CLAVE=` vacías) |
| Modificar | `tests/HospitalTurnos.Web.Tests/HospitalWebApplicationFactory.cs` (configuración SMTP válida para las pruebas de Web) |

### Lo que NO hay que hacer

- **NO enviar emails desde un caso de uso** ni llamar `IEnviadorEmail` fuera del procesador: es exactamente lo que ADR-05 prohíbe (un SMTP caído haría fallar la reserva).
- **NO llamar `SaveChanges` dentro de `Encolar`**: el mensaje tiene que guardarse con el cambio o no guardarse.
- **NO usar `now()` de Postgres ni `DateTime.UtcNow`** en la consulta o en la entidad: la hora sale del `TimeProvider` (NFR-06 y pruebas deterministas).
- **NO componer LINQ sobre el `FromSql` con `FOR UPDATE`** (`.FirstOrDefaultAsync()`, `.Where(...)`): EF lo mete en una subconsulta. `ToListAsync()` y después `SingleOrDefault()` en memoria.
- **NO tomar un lote entero en una sola transacción**: si el `COMMIT` falla después de enviar 50, se reenvían 50. Una transacción por mensaje.
- **NO contar como intento un envío cancelado por el apagado** (AC 12): el filtro `when (!ct.IsCancellationRequested)` es lo que lo evita.
- **NO dejar que una excepción escape de `ExecuteAsync`**: desde .NET 8 apaga la aplicación entera (`BackgroundServiceExceptionBehavior.StopHost`).
- **NO usar `Thread.Sleep` ni `Task.Delay` sin `TimeProvider`** en el procesador: las pruebas tardarían minutos reales.
- **NO registrar el procesador como scoped** ni inyectarle `HospitalDbContext` por constructor: es singleton (hosted service); el contexto se pide a un scope nuevo por ciclo.
- **NO escribir `Para`, asunto ni cuerpo en los logs**: son datos personales (Ley 25.326). Se loguea `MensajeId`, `Tipo` e `Intentos`.
- **NO borrar mensajes enviados** ni dar `DELETE` a `turnos_app`: la limpieza histórica no está pedida (YAGNI) y el historial sirve para soporte.
- **NO distinguir errores SMTP permanentes (5xx) de transitorios**: todos se reintentan; un 550 termina en fallo definitivo igual, 21 minutos después. Refinarlo es una story futura si hace falta.
- **NO implementar ningún manejador real** (la lista "Pacientes para avisar" es de la story 7.2) **ni constantes de `Tipo`**: las define cada story que encola (6.1, 6.5, 7.2).
- **NO agregar Moq ni `Microsoft.Extensions.Diagnostics.Testing`**: no están en el contrato; los dobles son a mano, como en el proyecto guía.
- **NO poner la sección `Email:Smtp` en `appsettings.json`** ni ninguna contraseña SMTP en un archivo versionado (ni en `appsettings.Development.json` ni en `docker-compose.yml`): usuario y clave van por `.env` / variables de entorno, y `.env.ejemplo` sólo nombra las variables (hallazgo I-10, contrato §Secretos).
- **NO modificar `PostgresFixture`** ni escribir una propia: es la copia de 1.1 que hizo 1.3. Lo propio de esta story va en `LimpiezaDeBandeja`.
- **NO poner `ToTable(...)`**: el nombre `mensajes_salientes` sale de la convención de 1.1.
- **NO usar un limpiador genérico** (Respawn, `TRUNCATE` de todas las tablas) para vaciar la bandeja: chocaría con el disparador de `registros_auditoria` (story 1.3). Sólo `TRUNCATE mensajes_salientes`.

### Salvaguarda de BD (obligatoria antes de implementar)

```bash
cd ~/SourceCode/hospital-turnos
DB=$(docker compose exec -T db printenv POSTGRES_DB)
docker compose exec -T db psql -U postgres -d "$DB" -c "SELECT rolname FROM pg_roles WHERE rolname IN ('turnos_owner','turnos_app');"
# Esperado: 2 filas. Si falta alguna → DETENERSE.
docker compose exec -T db psql -U postgres -d "$DB" -c "SELECT to_regclass('mensajes_salientes');"
# Esperado: vacío (NULL). Si ya existe → DETENERSE.
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8025/api/v1/messages
# Esperado: 200 (Mailpit de Docker Compose andando).
```
(Si el superusuario no es `postgres`: `docker compose exec -T db printenv POSTGRES_USER`.)

### Como verificar que funciona

```bash
cd ~/SourceCode/hospital-turnos
docker info > /dev/null && echo "docker OK"          # Testcontainers necesita Docker andando

dotnet build                                           # 0 errores, 0 advertencias
dotnet test --filter "Story=1.4"                       # 13 métodos en verde
dotnet test --filter "Story=1.4" --list-tests          # contar T1..T13 (T6 y T13 son [Theory])
dotnet test --filter "Story=1.4&AC=11"                 # sólo la de dos instancias
dotnet test --filter "Story=1.1|Story=1.2|Story=1.3"   # R1 y R2 (AC 17, 18)
bash scripts/smoke-compose.sh                          # R1: smoke E1 de 1.1 con el proceso de fondo y la config SMTP
dotnet test                                            # suite completa en verde

# AC 18 (lo controla R2) — a mano, mismo criterio que 1.2 AC 6 (esperado: sólo RelojHospital.cs)
grep -rnE "DateTime(Offset)?\.(Now|UtcNow|Today)" src/ --include="*.cs" | grep -v "/Migraciones/"

# Ninguna configuración SMTP en appsettings.json ni claves versionadas (esperado: sin resultados)
grep -n '"Email"' src/HospitalTurnos.Web/appsettings.json
grep -rnE "Clave|Usuario" src/HospitalTurnos.Web/appsettings*.json

# Prueba a mano: encolar con SQL directo y ver que llega a Mailpit en <= 15 s
docker compose up -d --build
DB=$(docker compose exec -T db printenv POSTGRES_DB)
docker compose exec -T db psql -U turnos_app -d "$DB" -c \
  "INSERT INTO mensajes_salientes (para, asunto, cuerpo_texto, cuerpo_html, tipo, creado_el, intentos, proximo_intento_el)
   VALUES ('ana@ejemplo.com','Prueba 1.4','texto','<p>html</p>','prueba', now(), 0, now());"
sleep 20
curl -s http://localhost:8025/api/v1/messages | jq '.messages[0].Subject'
# Esperado: "Prueba 1.4"
docker compose exec -T db psql -U turnos_app -d "$DB" -c "SELECT id, intentos, enviado_el IS NOT NULL AS enviado FROM mensajes_salientes;"
# Esperado: intentos = 1, enviado = t
```

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

### Change Log

- 2026-10-09: Story escrita (Mary, analista BMAD).
- 2026-10-09: Correcciones de la revisión adversarial (`revision-sprint-1.md`): C-1 (usa la `PostgresFixture` única, sin modificarla; `CadenaDuenio`), I-9 (regla de reintentos y nombres tomados del contrato), I-10 (SMTP fuera de `appsettings.json`, secretos por `.env`, AC 16 + T13), M-3 (sin `ToTable`), M-4 (`RegistroEnMemoria`), M-5 (R1, R2 para AC 17 y 18), M-8 (`depends_on` incluye 1.3).
