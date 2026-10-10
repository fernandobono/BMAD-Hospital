---
documento: Revisión adversarial de las stories del sprint 1
proyecto: Gestión de turnos — Hospital Municipal Las Acacias (caso simulado)
fecha: 2026-10-09
alcance: 1.1, 1.2, 1.3, 1.4, 1.5, 2.1 (antes de implementar)
contexto: 00-contrato-tecnico.md, 04-arquitectura.md, 02-prd.md, 03-ux.md, 05-epicas-y-plan.md, story-format.md (GOLD)
---

# Revisión adversarial — Sprint 1

## Resumen

| Severidad | Cantidad | Stories más afectadas |
|---|---|---|
| CRÍTICO | 3 | 1.3 + 2.1 (fixture), 2.1 (auditoría del ingreso fallido, verificación con `dotnet ef database update`) |
| IMPORTANTE | 17 | 2.1 (7), 1.4 (2), 1.5 (3), 1.1 (2), 1.2 (1), 1.3 (1), transversal (1) |
| MENOR | 16 | todas |

**Veredicto:** ninguna story está lista para implementar "sin preguntas" mientras sigan los 3
críticos. Los tres se corrigen con cambios de texto chicos, sin rediseño.

### Puntos pedidos que se verificaron

| Punto | Resultado | Evidencia |
|---|---|---|
| (a) ¿Identity 10 usa el reloj del sistema y no `TimeProvider`? | **Cierto.** La story 2.1 acierta. | Descompilado `Microsoft.Extensions.Identity.Core.dll` 10.0.11: `IsLockedOutAsync` compara contra `DateTimeOffset.UtcNow` y `AccessFailedAsync` fija `DateTimeOffset.UtcNow.Add(DefaultLockoutTimeSpan)`; el ensamblado no menciona `TimeProvider`. La cookie sí toma el `TimeProvider` de DI (`PostConfigureAuthenticationSchemeOptions` en `Microsoft.AspNetCore.Authentication.dll`, y `AddAuthentication` hace `TryAddSingleton(TimeProvider.System)`). |
| (b) ¿Canal `Recepcion` para el ingreso de cualquier integrante del personal? | **No tiene sentido** para médico ni administrador. Ver I-1. | PRD FR-33: canal = "web o recepción", pensado para cambios de turno; CE-01 mide turnos por canal. |
| (c) ¿`FOR UPDATE SKIP LOCKED` con EF Core, concretamente? | **Bien resuelto.** `FromSql` **sin componer** + `ToListAsync()` dentro de `BeginTransactionAsync`, una transacción por mensaje; `SingleOrDefault()` en memoria. `FOR UPDATE` exige privilegio `UPDATE` (correcto). | Story 1.4 §Implementación concreta. |
| (d) ¿El disparador "incluso contra el dueño" choca con migraciones o con borrar bases de prueba? | **No bloquea**, pero la garantía está sobrevendida. Ver I-8. | `DROP TABLE`/`DROP DATABASE`/disposición del contenedor no disparan nada; un `UPDATE` de relleno en una migración futura sí, y el dueño puede `DISABLE TRIGGER`. |
| Afirmación de 1.2: "en .NET 10 el middleware deja de registrar la excepción cuando un `IExceptionHandler` la maneja" | **Cierta.** | Descompilado `ExceptionHandlerMiddlewareImpl` 10.0.11: `flag = result == ExceptionHandledType.ExceptionHandlerService` suprime el registro. Consecuencia no prevista en M-1. |

---

## CRÍTICOS

### C-1 · 1.3 + 2.1 (+1.1, 1.4) — Dos `PostgresFixture` distintas: la de Aplicacion.Tests no reproduce los permisos reales

- **Dónde:** 1.1 §Given/Fixture (`tests/HospitalTurnos.Web.Tests/PostgresFixture.cs`, usa `docker/postgres/initdb/01-roles.sh`, expone `CadenaDuenio`); 1.3 §Implementación concreta (`tests/HospitalTurnos.Aplicacion.Tests/Infraestructura/PostgresFixture.cs`, crea los roles con SQL propio, expone `CadenaOwner`); 2.1 Task 1 y T1–T9.
- **Problema:** la fixture de 1.3 crea `turnos_owner`/`turnos_app` con 3 sentencias y **no** aplica los `ALTER DEFAULT PRIVILEGES`, `GRANT CONNECT/USAGE` ni `REVOKE CREATE` del script de 1.1. 1.1 dice explícitamente "no hay dos versiones de los roles"; 1.3 crea la segunda.
- **Por qué importa (bug seguro):**
  1. **2.1 T1–T9** corren en Aplicacion.Tests contra `CadenaApp`. Las 7 tablas de Identity las crea la migración como `turnos_owner`; en esa fixture `turnos_app` no recibe ningún privilegio sobre ellas → `42501 permission denied` en el primer `FindByNameAsync`. La "Salvaguarda de BD" de 2.1 no lo detecta: mira la base de Compose (donde los privilegios por defecto sí existen), da `t` y por lo tanto **no** agrega los `GRANT`.
  2. **1.3 T6 y T8** dicen proteger contra "privilegios por defecto que se cuelan", pero en esa fixture no hay privilegios por defecto: pasan aunque se borre el `REVOKE ALL` de la migración. La prueba central de ADR-07 no prueba lo que dice.
- **Corrección:** una sola especificación de `PostgresFixture` en el contrato técnico (§Pruebas): mismo `01-roles.sh` vía `WithResourceMapping` en los dos proyectos, mismas propiedades (`CadenaDuenio`/`CadenaApp` — elegir un nombre), migraciones como dueño. 1.3 la copia de 1.1 en vez de escribir la suya. Agregar a 1.3 T8 una verificación previa: `has_table_privilege('turnos_app', '__EFMigrationsHistory', 'UPDATE') = true` (demuestra que los privilegios por defecto están activos y que el `REVOKE` es lo que los saca).

### C-2 · 2.1 AC 2 y AC 5 — El registro de auditoría del ingreso fallido no cumple los campos obligatorios de 1.3

- **Dónde:** 2.1 AC 2 (usuario inexistente → `UsuarioId = null`), AC 5 (sin rol), T2(a), T5(b); 1.3 AC 2 (`Quien`, `Rol`, `Accion`, `EntidadTipo`, `EntidadId` no vacíos o `ArgumentException`).
- **Problema:** 2.1 sólo define `EntidadTipo`/`EntidadId`/`Rol` para el ingreso exitoso (AC 1). Para "usuario inexistente" no hay id de entidad ni rol; para "sin rol" no hay rol. Ninguna AC dice qué valores van.
- **Por qué importa:** si el agente pasa `null`/`""` (lo natural), el constructor de `RegistroAuditoria` lanza → el ingreso con un usuario mal escrito devuelve el **500 genérico** en vez de "Usuario o contraseña incorrectos." (y T2(a) falla). Si inventa valores, 9.1 recibe datos sin contrato.
- **Corrección:** fijar en AC 2 y AC 5, por caso: `Rol` (`"(sin rol)"` / `"(desconocido)"`), `EntidadTipo = "UsuarioPersonal"`, `EntidadId` (id si existe; si no, un valor fijo como `"desconocido"` — ver I-2 sobre no guardar lo tipeado). Agregarlos a la columna "Resultado esperado" de T2 y T5.

### C-3 · 2.1 §Salvaguarda de BD y §Como verificar — Usa `dotnet ef database update`, que 1.1 prohíbe y que falla

- **Dónde:** 2.1 "Salvaguarda de BD" ("Después de `dotnet ef database update`…") y "Como verificar" paso 1; 1.1 "Lo que NO hay que hacer".
- **Problema:** el `HospitalDbContext` de DI usa `ConnectionStrings:App` (`turnos_app`, sin `CREATE`). `dotnet ef database update` corre en `Development` con esa cadena → falla con `42501`. La salvaguarda es obligatoria y termina en "DETENERSE": el agente queda bloqueado antes de empezar las pruebas de Web. Además, el paso 5 usa `-d turnos`; la base se llama `hospital_turnos` (1.1 AC 4).
- **Corrección:** reemplazar por "levantar la app (`docker compose up -d --build`, o `dotnet run` con Compose arriba): las migraciones las aplica el arranque con `ConnectionStrings:Migraciones`". Corregir `-d hospital_turnos`.

---

## IMPORTANTES

### I-1 · 2.1 AC 1, 2, 3, 5 — Canal `Recepcion` para el ingreso de todo el personal (punto b)

- **Problema:** `CanalAuditoria { Web, Recepcion, Sistema }` responde a FR-33 "canal (web o recepción)" de los **cambios de turno**. Un médico o un administrador que ingresa no opera "por la recepción". Además CE-01 ("turnos por la web ≥ 40 %") se mide con el canal de la auditoría: mezclar ingresos con canal `Recepcion` ensucia esa métrica si 9.1 no filtra por acción.
- **Por qué importa:** el registro legal queda con un dato falso; 9.1 heredará la confusión.
- **Corrección (elegir una, y cambiar el contrato primero):** (a) agregar `CanalAuditoria.Personal` (interfaz del personal) y usarlo en 2.1; o (b) usar `Sistema` para eventos de seguridad. Dejar escrito en el contrato qué significa cada canal. Nota de alcance: auditar ingresos no lo pide FR-33; si se mantiene, decirlo como decisión (no es scope creep grave, pero hoy no está justificado).

### I-2 · 2.1 AC 2 — Se guarda para siempre lo que el usuario tipeó, en una tabla que no se puede borrar

- **Problema:** `Quien = "usuario: <lo escrito>"` en `registros_auditoria`, inmutable por diseño (1.3). Es frecuente que alguien escriba la contraseña en el campo usuario: queda en un registro legal que ni el dueño puede corregir. Además cualquiera con acceso a la pantalla hace crecer sin límite una tabla que nunca se purga.
- **Corrección:** para usuario inexistente no guardar el texto (o guardar un hash corto); los intentos fallidos van al `ILogger` o a una tabla purgable, y en la auditoría legal sólo el bloqueo. Si se mantiene, AC explícito del riesgo aceptado.

### I-3 · 2.1 AC 2 y AC 3 — El mensaje de bloqueo revela qué usuarios existen

- **Problema:** AC 2 iguala el mensaje para no enumerar usuarios, pero AC 3 lo anula: 5 intentos con un usuario real muestran "quedó bloqueado"; con uno inexistente, nunca. También hay diferencia de tiempos: el inexistente responde sin calcular PBKDF2.
- **Corrección:** (a) contar fallos también por nombre inexistente (en memoria o tabla) y mostrar el mismo bloqueo, o (b) mostrar el mensaje genérico aun bloqueado y dejar el bloqueo sólo en la auditoría; y verificar un hash ficticio cuando el usuario no existe. Agregar test del modo de falla "usuario inexistente × 5".

### I-4 · 2.1 AC 18–20 + 1.1 AC 12 — Con un clon limpio sin `.env`, la app no arranca

- **Problema:** Compose pasa `Personal__AdministradorInicial__Usuario=` y `__Contrasena=` **vacíos**. Para la configuración de .NET la clave existe con valor `""`. Si el sembrador mira "¿existe la sección?", cae en AC 19 (contraseña fuera de política) → `InvalidOperationException` al arrancar → `restart: unless-stopped` en bucle, y el smoke E1 de 1.1 (que sigue en la suite de regresión) falla.
- **Corrección:** AC 18 explícito: "usuario o contraseña vacíos o sólo espacios = sin configuración". Test nuevo con valores vacíos.

### I-5 · 2.1 AC 9 / T15 — No dice con qué `Accept` se pide, y el resultado cambia

- **Problema:** el `ManejadorGlobalDeErrores` de 1.2 responde JSON salvo que el pedido traiga `text/html`. `HttpClient` no manda `Accept` por defecto → T15 recibe `problem+json` (con la `ó` escapada como `ó`), no la página que ve el usuario. Buscar el texto crudo falla; si se busca en JSON, se prueba el "layout" equivocado.
- **Corrección:** T15 manda `Accept: text/html` (como un navegador al enviar el formulario), espera la página `/Error` y compara con `WebUtility.HtmlDecode`, igual que 1.2 T8.

### I-6 · 2.1 AC 22 → 2.2 — Desactivar un usuario no corta su sesión abierta

- **Problema:** la cookie propia del esquema `"Personal"` no usa el `SecurityStampValidator` de Identity, y nada revisa `Activo` por pedido. Cuando 2.2 desactive a alguien, su sesión sigue válida mientras la use (vencimiento deslizante: indefinido).
- **Corrección:** en 2.1, `CookieAuthenticationEvents.OnValidatePrincipal` que verifique `Activo` (y el sello de seguridad) cada N minutos; o AC de contrato explícito que 2.2 hereda, con su test.

### I-7 · Transversal (1.1, 2.1) — NFR-03 (HTTPS) quedó sin dueño

- **Problema:** el plan (§4) dice que NFR-03 se cumple "dentro de 1.1 y 2.1". 1.1 no menciona HTTPS ni proxy; 2.1 sólo cubre el hash. Detrás de un proxy que termina TLS, sin `UseForwardedHeaders` la app cree que es HTTP: la cookie `.HospitalTurnos.Personal` sale **sin `Secure`** (política por defecto `SameAsRequest`) y `UseHttpsRedirection`/HSTS no actúan.
- **Corrección:** en 2.1, `Cookie.SecurePolicy = Always` fuera de `Development` + AC/test; en 1.1 (o story nueva), `UseForwardedHeaders` configurado para el proxy. Actualizar el plan.

### I-8 · 1.3 AC 8 y ADR-07 — "Incluso el dueño" está sobrevendido (punto d)

- **Problema:** el dueño de la tabla puede `ALTER TABLE registros_auditoria DISABLE TRIGGER …` o `DROP TABLE`. Y la contraseña del dueño vive en el contenedor `web` (`ConnectionStrings__Migraciones` en Compose). Arq. §7.5 promete que "alguien con la contraseña de la aplicación" no puede alterarla: hoy sí puede.
  - Migraciones futuras: el DDL (`ADD COLUMN`, `ALTER TYPE`, `DROP`) no dispara el disparador; un `UPDATE` de relleno sí, y la migración tendrá que deshabilitarlo y volver a habilitarlo (es posible porque corre como dueño).
  - Bases de prueba: disponer el contenedor o `docker compose down -v` no chocan. Sí choca cualquier "limpiador" genérico futuro (Respawn, `TRUNCATE` de todas las tablas): debe excluir esta tabla.
- **Corrección:** (a) reformular AC 8/ADR-07 con la garantía real ("frena un `UPDATE`/`DELETE`/`TRUNCATE` accidental del dueño, no a un dueño malicioso"), o (b) sacar las credenciales del dueño de la app: migraciones en un paso aparte (bundle de EF o contenedor de una sola corrida) y la app sólo con `turnos_app`. Agregar a "Lo que NO hay que hacer" de stories futuras: no usar limpiadores que toquen `registros_auditoria`.

### I-9 · 1.4 AC 6 — Se aparta del contrato sin cambiarlo primero

- **Problema:** el contrato dice "al tercer fallo marca `FalloDefinitivoEl`"; 1.4 lo interpreta como 4 intentos (1 + 3 reintentos). La interpretación es razonable y coincide con S-33, pero el contrato dice "si una story necesita apartarse, primero se cambia este archivo". 7.2 va a leer el contrato.
- **Corrección:** actualizar el contrato: "1 intento + 3 reintentos (1, 5 y 15 min); al fallar el 4.º intento marca `FalloDefinitivoEl`". Ídem para lo que 1.4 "completa" (`IEnviadorEmail`, `IManejadorFalloDefinitivo`, `ManejadoEl`, `FalloDefinitivoEmail`).

### I-10 · 1.4 §Que hay que hacer (punto 8) — Valores SMTP de desarrollo en `appsettings.json`

- **Problema:** para pasar `ValidateOnStart`, 1.4 pone `localhost:1025` en `appsettings.json` (todos los entornos). En producción, si falta la variable de entorno, la app arranca "bien" y cada email falla en silencio hasta el fallo definitivo, 21 minutos después. Contradice el criterio de 1.1 (si falta la configuración, el arranque falla).
- **Corrección:** sección `Email:Smtp` sólo en `appsettings.Development.json`; Compose y las fábricas de prueba la pasan; `ValidateOnStart` corta el arranque si falta. Test: arranque sin `Email:Smtp:Host` falla con mensaje claro.

### I-11 · 1.5 Task 2 — Choca con el manifiesto de herramientas de 1.1

- **Problema:** 1.1 ya crea `.config/dotnet-tools.json` (con `dotnet-ef`). 1.5 lo lista en `files_to_create` y corre `dotnet new tool-manifest`, que falla si existe; con `--force` lo pisa y se pierde `dotnet-ef`.
- **Corrección:** mover el archivo a `files_to_modify` y sólo `dotnet tool install dotnet-reportgenerator-globaltool --version 5.5.9`.

### I-12 · 1.5 frontmatter — `depends_on: ["1.1"]` no alcanza

- **Problema:** T3 exige cobertura ≥ 80 % y "las stories 1.1–1.4 hechas". Implementada después de 1.1 sola, T3 falla (no hay pruebas de reglas).
- **Corrección:** `depends_on: ["1.1", "1.2", "1.3", "1.4"]` (y 2.1, cuyo AC 23 pide 1.5 en verde, debería listar 1.5).

### I-13 · 1.1 Tests (nota) y 1.5 — La imagen Docker nunca se construye en CI

- **Problema:** 1.1 dice que el smoke de Compose "lo reutiliza la story 1.5"; 1.5 no lo menciona. Un `Dockerfile` roto (o sin `tzdata`, ver 1.2 Task 1) pasa la CI en verde.
- **Corrección:** en 1.5, job `imagen` (`docker build -f src/HospitalTurnos.Web/Dockerfile .`, y opcionalmente `scripts/smoke-compose.sh`); o corregir la frase de 1.1.

### I-14 · 1.1 §Implementación (script de roles) — `set -u` en un script que el entrypoint puede *incluir* en vez de ejecutar

- **Problema:** el entrypoint de `postgres` ejecuta un `.sh` si es ejecutable y lo **incluye** (`. "$f"`) si no. Por Testcontainers, `WithResourceMapping` copia con modo 644 por defecto; desde un clon en Windows o sin el bit guardado en git, también. Incluido, el `set -euo pipefail` queda activo en el entrypoint y puede cortarlo con "unbound variable".
- **Corrección:** sacar `-u` (dejar `set -eo pipefail`), o garantizar el bit ejecutable (`git update-index --chmod=+x` + modo `0755` en `WithResourceMapping`). Anotarlo en "Lo que NO hay que hacer".

### I-15 · 1.2 T8 caso `Development` — Las cadenas de `appsettings.Development.json` pueden pisar las del test

- **Problema:** con `UseEnvironment("Development")` se carga `appsettings.Development.json` (que 1.1 llena con `localhost:5432`). Las fuentes JSON se agregan después de los valores de `UseSetting` y pueden ganarles: el test terminaría hablando con el PostgreSQL de Compose (o con nada). No se verificó el orden para .NET 10; es un riesgo concreto.
- **Corrección:** en `HospitalWebApplicationFactory`, inyectar las cadenas con `ConfigureAppConfiguration(c => c.AddInMemoryCollection(...))` (última fuente, gana siempre) y documentarlo en Given/Fixture.

### I-16 · 1.1 vs 2.1 — Política de secretos contradictoria

- **Problema:** 2.1 "Lo que NO hay que hacer": "NO poner ninguna contraseña en el repo (ni en `appsettings.Development.json`, ni en `docker-compose.yml`)". 1.1 pone contraseñas de desarrollo de la base en ambos (`owner_dev`, `app_dev`, `postgres_dev`) y publica el 5432 en el host.
- **Corrección:** fijar la regla en el contrato: o "las de desarrollo locales se aceptan versionadas" (y 2.1 lo dice así), o todas por `.env` + `.env.ejemplo` desde 1.1.

### I-17 · 2.1 T12, T14 (y Web.Tests en general) — Conteos sobre una base compartida con clases en paralelo

- **Problema:** varias fábricas (`HospitalWebApplicationFactory`, `PersonalWebApplicationFactory`) sobre el mismo PostgreSQL; xUnit corre clases de colecciones distintas en paralelo. "0 registros nuevos de auditoría" sin filtro es inestable, y dos apps pueden correr migraciones y siembra a la vez.
- **Corrección:** una `[Collection]` única para Web.Tests con base de datos, y aserciones filtradas por un marcador propio (usuario único por test).

---

## MENORES

| # | Story · lugar | Problema | Corrección |
|---|---|---|---|
| M-1 | 1.2 AC 8 / AC 11 | Cuando el manejador devuelve `false` (camino HTML), el middleware re-ejecuta `/Error` y **registra con nivel `Error`** (verificado en el código de 10.0.11): una excepción del dominio pedida desde HTML queda como `Error`, contra AC 8, y lo inesperado se registra dos veces. | `UseExceptionHandler(new ExceptionHandlerOptions { ExceptionHandlingPath = "/Error", CreateScopeForErrors = true, SuppressDiagnosticsCallback = c => c.Exception is excepción del dominio })` o dejar escrito el comportamiento. |
| M-2 | 1.2 | NFR-09 sólo cubre pedidos HTTP. Los errores dentro de un circuito interactivo de Blazor no pasan por el manejador (ni `DetailedErrors`). Hoy no hay pantallas interactivas. | Nota explícita para 5.5: `ErrorBoundary` + `DetailedErrors=false`. |
| M-3 | 1.3, 1.4 | Ponen `ToTable("registros_auditoria")` / `ToTable("mensajes_salientes")` a mano; 1.1 AC 8 promete que no hace falta. Inofensivo, pero el contrato entre stories dice otra cosa. | Quitar el `ToTable` o corregir 1.1 AC 8. |
| M-4 | 1.1, 1.2, 1.3, 1.4, 2.1 | Nombres que no coinciden: `CadenaDuenio` vs `CadenaOwner`; `RegistroEnMemoria` vs `LoggerQueAnota` (mismo doble, dos nombres); `HospitalWebApplicationFactory` vs `PersonalWebApplicationFactory`; 2.1 lista en `files_to_create` la `PostgresFixture` de Web.Tests que ya crea 1.1, y dice que la de Aplicacion.Tests "la crea 1.1 o 1.3" (sólo 1.3). | Unificar en el contrato. |
| M-5 | 1.3 AC 10–11, 1.4 AC 16–17 | AC sin fila en la tabla de pruebas (regla GOLD #2 / ACCOV-C5); se verifican "con comandos". | Agregar fila tipo "R1 regresión" como hace 1.2, o marcarlos declarativos. |
| M-6 | 2.1 T13, T18 | El `Location` de la cookie (`RedirectToLogin`) y de `NavigateTo` es **absoluto** (`http://localhost/...`). Comparar contra `/personal/...` exacto falla. | Comparar `Location.PathAndQuery`. |
| M-7 | 2.1 AC 14 | El orden del menú del administrador (Agendas, Para avisar, Médicos, …) no sigue UX §3 (A1, A6, A2, A3, A4, A5, R3, y R2). | Orden por rol tomado de UX §3. |
| M-8 | 1.3, 1.4 | Las dos agregan migración y tocan el snapshot; si se implementan en paralelo, conflicto. 1.4 además usa la fixture de 1.3. | `depends_on` de 1.4 incluye 1.3. |
| M-9 | 1.5 T1 | PyYAML lee la clave `on:` como `True` (YAML 1.1): buscar `data["on"]` falla. | Avisarlo en Dev Notes. |
| M-10 | 1.5 job `integracion` | Dos proyectos con `--logger "trx;LogFileName=…"` fijo: el segundo pisa el `.trx` del primero. | Un nombre por proyecto. |
| M-11 | 1.1 / 2.1 | Las claves de protección de datos quedan dentro del contenedor: cada `docker compose up --build` invalida sesiones y tokens antifalsificación (y deja advertencias). | Volumen para las claves o `PersistKeysToDbContext`. |
| M-12 | 1.5 AC 5 | La política de reintentos (S-33) vive en `MensajeSaliente`, en Infraestructura, que queda fuera de la medición de NFR-11. | Aceptarlo explícito o medir esa clase. |
| M-13 | 2.1 AC 3 | Sin límite por IP, cualquiera en la red bloquea a cualquier médico con 5 intentos. | Dejarlo como riesgo aceptado en la story. |
| M-14 | 1.1 AC 13 | Después de `start db`, el primer chequeo puede fallar por conexiones rotas del pool. | El smoke espera "200 dentro de N s" con sondeo, no un pedido único. |
| M-15 | 2.1 AC 1 / AC 13 | No define qué pasa con un usuario con dos roles (`InicioDe(rol)` ambiguo). 2.2 podría permitirlo. | "Un rol por usuario" en el contrato, o regla de prioridad. |
| M-16 | 2.1 AC 9 | Si el formulario envuelve el ingreso en `try/catch (Exception)`, puede tragarse la excepción de navegación de `NavigateTo` en SSR. | Nota en "Lo que NO hay que hacer": no envolver `NavigateTo` en `catch` genérico. |

---

## Lo que salió bien

- Cada story tiene modos de falla por camino feliz y anclas reales al proyecto guía.
- 1.4 resuelve bien concurrencia, apagado ordenado y "al menos una vez", con pruebas deterministas.
- 1.2 verifica el cambio de .NET 10 del registro de excepciones (cierto).
- 2.1 verifica que Identity no usa `TimeProvider` (cierto) y prueba el bloqueo vencido escribiendo `LockoutEnd`.

---

## Estado de la corrección (2026-10-09)

**Aplicados los 36 hallazgos** (pedido del docente: corregir todo). Decisiones transversales tomadas
al corregir: una sola `PostgresFixture`; canal de auditoría nuevo `Personal`; no se guarda lo tipeado
cuando el usuario no existe; el bloqueo muestra el mismo mensaje genérico; secretos sólo en `.env`
(con `.env.ejemplo` versionado); HTTPS repartido entre 1.1 (proxy, HSTS) y 2.1 (cookie `Secure`);
la sesión del personal se revalida cada 5 minutos (I-6, en 2.1). La parte de I-8 que tocaba la
arquitectura se aplicó en `04-arquitectura.md` §7.5 y ADR-07.

| Story | Criterios | Pruebas |
|---|---|---|
| 1.1 | 20 | 14 |
| 1.2 | 14 | 9 |
| 1.3 | 11 | 8 |
| 1.4 | 18 | 13 |
| 1.5 | 13 | 4 |
| 2.1 | 27 | 28 |
| **Total** | **103** | **76** |

Supuestos de .NET 10 sin verificar todavía (cada story trae alternativa): `KnownIPNetworks` en
lugar de `KnownNetworks`, y `SuppressDiagnosticsCallback` en el manejador de errores. Se confirman
al implementar.

