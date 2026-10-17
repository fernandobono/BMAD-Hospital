---
epic: 2
story: "2.3"
title: "Contraseña olvidada: el administrador genera otra contraseña temporal desde A4"
status: ready-for-dev
sprint: 3
priority: "P0"
approach: "[ADAPT]"
depends_on: ["2.1", "2.2"]
followup_of:
  - id: "2.2"
    type: "extension"
    summary: "A4 suma la acción de generar otra contraseña temporal a un usuario existente, reusando el generador y la marca de la 2.2"
followup_reason: deuda-declarada
files_to_create:
  - "docs/qa/2.3-pasos.md"
files_to_modify:
  - "src/HospitalTurnos.Dominio/Auditoria/AccionesAuditoria.cs"
  - "src/HospitalTurnos.Aplicacion/Personal/IGestionUsuariosPersonal.cs"
  - "src/HospitalTurnos.Infraestructura/Identidad/GestionUsuariosPersonalIdentity.cs"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/UsuariosPersonal.razor"
  - "tests/HospitalTurnos.Aplicacion.Tests/Personal/GestionUsuariosPersonalIdentityTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Personal/UsuariosPersonalHttpTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Personal/Dobles/GestionUsuariosConBaseCaida.cs"
---

# Story 2.3: Contraseña olvidada: el administrador genera otra contraseña temporal desde A4

Status: ready-for-dev

## Story

Como administradora del Hospital Las Acacias (Lucía),
quiero generarle una contraseña temporal nueva a una persona del personal que olvidó la suya,
para que vuelva a entrar en el día, elija su propia contraseña en el próximo ingreso, y nadie tenga
que tocar la base de datos para destrabarla.

> Origen: decisión 2 del docente en la revisión del sprint 2 (`revision-sprint-2.md`, "Decisiones
> abiertas consolidadas"; la 2.2 la dejó afuera a propósito y lo escribió en sus "Decisiones tomadas
> por el docente"; plan, fila 2.3). Hoy quien olvida la suya sólo sale tocando la base.

## Acceptance Criteria

> Pantalla A4 (UX §7 "A4 · Usuarios del personal"), ruta `/personal/admin/usuarios`, sólo rol
> `Administrador`. Es la misma página de la 2.2: esta story le suma **un botón por fila** dentro del
> formulario único de acciones (`acciones-usuarios`, `Accion = "<tipo>:<id>"`), con el tipo nuevo
> `restablecer`. Renderizado estático en el servidor (SSR), sin circuito interactivo, como el resto
> de A4. Mensajes en castellano rioplatense. "Ahora" sale siempre del `TimeProvider` inyectado.
>
> **Se reusa todo lo de la 2.2, sin copiarlo** (su AC 26): `ContrasenaTemporal.Generar()`, la marca
> `UsuarioPersonal.DebeCambiarContrasena` y el cambio obligatorio del próximo ingreso (2.2 AC 19–21,
> middleware `CambioDeContrasenaObligatorio`), `IActorVigente.ExigirAdministradorActivoAsync`,
> `ActorDeSesion.Desde`, `ManejadorGlobalDeErrores.EsEsperable`/`FijarEstadoEnPagina`, el
> `Cache-Control: no-store` de A4 y, en las pruebas, `UsuariosDePrueba`, `InterceptorQueSeAdelanta`,
> `AuditoriaQueFalla`, `FormularioHttp.EnviarAsync`/`Estado`/`Alerta` y `GestionUsuariosConBaseCaida`.
>
> **Campos de auditoría de esta story** (los obligatorios de la 1.3):
>
> | Campo | Valor |
> |---|---|
> | `UsuarioId` | id del administrador que actúa, **devuelto por `IActorVigente`** (leído de la base) |
> | `Quien` | su `NombreCompleto`, leído de la base |
> | `Rol` | `Administrador` |
> | `Canal` | `CanalAuditoria.Personal` |
> | `EntidadTipo` | `"UsuarioPersonal"` |
> | `EntidadId` | id del usuario afectado (`Guid` como texto) |
> | `Detalle` | `{"usuario":"<usuario>","desbloqueado":true\|false}`. **Nunca** la contraseña, ni la temporal nueva ni la anterior |

### Generar otra contraseña temporal (FR-10, S-30)

1. **Given** un administrador ingresado y un usuario del personal **activo** que no es él **When**
   pulsa `Nueva contraseña temporal` en la fila de ese usuario **Then**, en **una** transacción:
   - el usuario recibe una contraseña temporal nueva generada por `ContrasenaTemporal.Generar()`
     (12 caracteres, la política real de Identity, sin `0 O o 1 l I`: 2.2 AC 4), guardada sólo como
     hash. **La contraseña anterior deja de servir**;
   - queda `DebeCambiarContrasena = true`;
   - su sello de seguridad (`SecurityStamp`) cambia;
   - se guarda **un** registro `AccionesAuditoria.ContrasenaPersonalRestablecida` con los campos de
     la tabla del inicio;
   - la respuesta es `200`, con `Cache-Control: no-store`, el elemento `role="status"` del AC 13 con
     la contraseña dentro de `<code id="contrasena-temporal">`, y la fila del usuario muestra
     `Debe cambiar la contraseña` en la misma respuesta (la tabla se recarga después de la acción,
     como en la 2.2).
   No cambian su nombre, su usuario, su rol, su estado `Activo` ni su vínculo con un médico (3.2).
2. La contraseña temporal **nunca** se guarda en texto, ni en el `ILogger`, ni en
   `registros_auditoria`, ni aparece en otra respuesta: un `GET` posterior de A4 no la trae (mismo
   trato que 2.2 AC 4).
3. **Given** que el usuario tenía una sesión abierta **When** el administrador le genera la
   contraseña nueva **Then** esa sesión se corta en la siguiente revalidación (2.1 AC 25): a los 6
   minutos, su próximo pedido recibe `302` al ingreso. Lo produce el cambio de sello del AC 1; no hay
   código aparte.
4. **Given** la contraseña temporal nueva **When** la persona ingresa en S0 con ella **Then** pasa
   exactamente lo de 2.2 AC 19–21: `302` a `/personal/cambiar-contrasena`, todo `/personal/` la
   manda ahí hasta que cambie, y al cambiarla la marca vuelve a `false`. Con la contraseña anterior,
   S0 responde `Usuario o contraseña incorrectos.` El cambio se audita como en la 2.2, con
   `{"motivo":"primer-ingreso"}` (decisión 18).

### Usuario bloqueado, desactivado o el propio administrador

5. **Given** un usuario **bloqueado por intentos** (`LockoutEnd` posterior a ahora, 2.1) **When** el
   administrador le genera la contraseña nueva **Then**, además de lo del AC 1, en la misma
   transacción queda `LockoutEnd = null` y `AccessFailedCount = 0`, el `Detalle` lleva
   `"desbloqueado":true`, y el mensaje del AC 13 termina con `Su usuario también quedó desbloqueado.`
   La persona puede ingresar en el acto con la temporal. Para un usuario no bloqueado,
   `"desbloqueado":false` y `AccessFailedCount` queda en `0` igual (arranca de cero con la
   contraseña nueva) (decisión 14).
6. **Given** un usuario **desactivado** **When** se envía `restablecer:<id>` (POST armado: la pantalla
   no muestra el botón, AC 12) **Then** la respuesta es `409` con
   `Ese usuario está desactivado. Reactivalo antes de generarle otra contraseña.` en `role="alert"`,
   y nada cambia: ni hash, ni sello, ni marca, ni auditoría (decisión 15).
7. **Given** un administrador **When** se envía `restablecer:<su propio id>` (POST armado: su fila
   dice `(sos vos)` y no tiene el botón, AC 12) **Then** la respuesta es `400` con
   `No podés generarte una contraseña temporal a vos mismo. Si querés otra, cambiala en /personal/cambiar-contrasena.`
   en `role="alert"`, y nada cambia (decisión 16).

### Errores de la acción

8. **Given** que quien actúa ya **no** es un administrador activo en la base (lo desactivaron hace
   menos de 5 minutos, o su id no existe) **When** envía `restablecer:<id>` **Then** recibe `403` con
   `No tenés permiso para administrar usuarios.` en `role="alert"` y nada cambia. Lo controla
   `IActorVigente.ExigirAdministradorActivoAsync`, igual que las otras acciones de A4 (2.2 AC 15).
9. **Given** un id que no existe **When** se envía `restablecer:<id>` **Then** `404` con
   `No encontramos ese usuario. Recargá la página.` (el cartel de la 2.2, fijado con
   `FijarEstadoEnPagina` para que llegue el cuerpo) y nada cambia. Un tipo de acción desconocido
   sigue dando el `400` de 2.2 AC 16.
10. **Atómico.** Contraseña, marca, sello, desbloqueo y auditoría van en **una** transacción. **Given**
    que falla cualquier paso (por ejemplo, la auditoría) **When** se envía la acción **Then** la
    contraseña anterior sigue sirviendo, la marca y el sello quedan como estaban, y el error sigue
    su camino (AC 15).
11. **Carrera.** **Given** que, entre que el caso de uso lee al usuario y guarda, otro pedido lo
    cambia (dos administradores a la vez, o uno que lo desactiva mientras otro le genera la
    contraseña) **When** se guarda **Then** la base rechaza el segundo cambio por el sello de
    concurrencia de Identity (`concurrency_stamp`), el caso de uso lo traduce a
    `ConflictoException("Otro administrador cambió a ese usuario recién. Recargá la página y probá de nuevo.")`
    (`409` con cartel, **nunca** un error interno) y no queda nada del pedido rechazado.

### Pantalla A4

12. En la columna `Acciones`, cada fila de un usuario **activo** que **no** es el administrador que
    mira tiene, además de los botones de la 2.2, el botón
    `<button type="submit" name="Accion" value="restablecer:<id>">Nueva contraseña temporal</button>`.
    Lo tienen también los usuarios bloqueados, los de `(sin rol)` y los de `(varios roles)`. **No** lo
    tienen los desactivados (AC 6) ni la fila propia, que sigue diciendo `(sos vos)` (AC 7).
13. El mensaje de éxito, en `role="status"`:
    `Nueva contraseña temporal para <usuario>: <contraseña>. Anotala y dásela a la persona: no se vuelve a mostrar. La anterior ya no sirve y la tiene que cambiar en su próximo ingreso.`
    con la contraseña dentro de `<code id="contrasena-temporal">`; si además se desbloqueó (AC 5), al
    final ` Su usuario también quedó desbloqueado.` Se genera directamente, **sin** un paso de
    confirmación previo (decisión 17).
14. **Given** un POST de `restablecer:<id>` **sin** token antifalsificación **When** llega **Then**
    `400` y nada cambia. **Given** un usuario `Recepcionista` o `Medico` que envía ese POST a mano
    (con un token válido de su propia sesión) **Then** `302` a `/personal/acceso-denegado` y nada
    cambia (la protección `[Authorize(Roles = Administrador)]` de la página, 2.1 AC 13).
15. **Given** que la base no responde durante la acción **When** un navegador la envía
    (`Accept: text/html`) **Then** `500` con la página `/Error` de la 1.2
    (`Algo falló. Probá de nuevo en unos minutos.`), sin ninguna contraseña ni detalle técnico en el
    cuerpo; el detalle queda sólo en el `ILogger` (NFR-09). La página sigue atrapando **sólo** las
    excepciones del dominio.

### Contrato con otras stories

16. Lo que esta story **fija**, con sus pruebas (B-1):
    - **9.1 (A5, auditoría)** — `AccionesAuditoria` suma
      `ContrasenaPersonalRestablecida = "usuario-personal.contrasena-restablecida"` (estilo
      `<entidad>.<acción>` del contrato técnico §Auditoría, entidad `usuario-personal` como las cinco
      de la 2.2), con `EntidadTipo = "UsuarioPersonal"` y el `Detalle` `{"usuario","desbloqueado"}`.
      **Pruebas:** T1 (forma completa con `desbloqueado` falso), T3 (`desbloqueado` verdadero).
    - **`IGestionUsuariosPersonal`** suma **un** método con esta firma (Implementación concreta):
      `Task<ContrasenaRestablecida> RestablecerContrasenaAsync(Guid usuarioId, ActorAuditoria actor, CancellationToken ct)`.
      Las firmas de la 2.2 no cambian. Todo el que implemente la interfaz lo implementa: hoy sólo
      `GestionUsuariosPersonalIdentity` y el doble `GestionUsuariosConBaseCaida`. **Pruebas:** T1–T9
      (implementación real), T16 (doble).
    - **Stories del sprint 3 (4.2, 4.3, 4.4, 5.1)** — ninguna toca A4 ni usuarios: esta story no les
      fija nada ni depende de ellas. Va primera en el orden del sprint porque destraba al personal
      que olvida su contraseña mientras se prueban las demás. La 5.1 también suma constantes a
      `AccionesAuditoria`, después de ésta en el orden: no chocan.
    - **Stories del sprint 4 que tocan A4** — la **2.4** ("Email del personal en A4", decisión 1) suma
      el campo email al alta y a la edición de A4, y la **2.5** (decisión 26) traduce la carrera en
      desactivar, reactivar y desbloquear. Las dos van después: esta story no les deja nada propio
      fuera de lo que ya es de la 2.2 (el formulario único `acciones-usuarios`, `AsegurarSinCarrera`
      queda como modelo para la 2.5).

### No-regresión

17. Siguen en verde, **sin modificarlas**, las pruebas de las stories 1.1 a 4.1. En particular las de
    A4 de la 2.2 (T20, T25, T26), que buscan en cada fila los textos `desactivar:`, `reactivar:` y
    `desbloquear:`: el valor nuevo `restablecer:` no contiene ninguno de los tres. No hay migración
    (la columna `debe_cambiar_contrasena` y el `concurrency_stamp` ya existen). La protección de
    `registros_auditoria` (1.3) no se toca.

### Tests (16 verificaciones)

| # | Test | AC | Nivel | Escenario | Resultado esperado |
|---|------|----|-------|-----------|--------------------|
| T1 | `RestablecerContrasenaAsync_UsuarioActivo_GeneraTemporalMarcaYAudita` | 1,2,3,16 | integration | Proveedor con `CapturaDeRegistros`; admin propio; usuario propio `Recepcionista` (contraseña `UsuariosDePrueba.Clave`), se guardan su sello y su hash; el admin restablece | Devuelve id, usuario, temporal de 12 caracteres y `Desbloqueado = false`; en la base: `debe_cambiar_contrasena = true`, sello distinto, hash distinto; `VerifyHashedPassword(hash, temporal)` = `Success` y con `Clave-Correcta-1` = `Failed`; `SesionVigenteAsync(id, selloViejo)` = `false`; nombre, usuario, rol y `activo` sin cambio; 1 fila `usuario-personal.contrasena-restablecida` con `entidad_id` = su id, actor del admin (`ActorAuditado`), canal `Personal`, detalle con `usuario` = `p-<m>` y `desbloqueado` falso, **sin** la temporal; ningún mensaje de la captura contiene la temporal |
| T2 | `RestablecerContrasenaAsync_ConLaTemporal_IngresaObligadoYCambiaComoPrimerIngreso` | 4 | integration | Usuario propio; restablecer; `IIngresoPersonal.ValidarAsync` con la anterior y con la temporal; `ICambioContrasenaPersonal.CambiarAsync(id, temporal, "Nueva-Clave-2026")` | Anterior → `CredencialesInvalidas`; temporal → `Exitoso` con `DebeCambiarContrasena = true` y el claim `debe_cambiar_contrasena = si`; el cambio es exitoso, la marca queda `false` y hay 1 fila `usuario-personal.contrasena-cambiada` con detalle `{"motivo":"primer-ingreso"}` |
| T3 | `RestablecerContrasenaAsync_UsuarioBloqueado_TambienLoDesbloquea` | 5,16 | integration | Usuario propio con 5 `AccessFailedAsync` (bloqueado 15 min); restablecer; después `ValidarAsync` con la temporal | `Desbloqueado = true`; `lockout_end` nulo, `access_failed_count = 0`; detalle con `desbloqueado` verdadero; ingreso `Exitoso` (no `Bloqueado`) |
| T4 | `RestablecerContrasenaAsync_UsuarioDesactivado_LanzaConflictoSinCambios` | 6 | integration | Usuario propio creado con `activo: false`; restablecer | `ConflictoException` con el texto del AC 6; hash, sello y marca sin cambio; 0 filas `usuario-personal.contrasena-restablecida` con su id |
| T5 | `RestablecerContrasenaAsync_ASiMismo_LanzaReglaSinCambios` | 7 | integration | Admin propio se restablece a sí mismo | `ReglaDeNegocioException` (**no** `ConflictoException`) con el texto del AC 7; su hash y su sello sin cambio; 0 filas con su id como `entidad_id` y esa acción |
| T6 | `RestablecerContrasenaAsync_ActorQueNoEsAdministradorActivo_LanzaAccesoDenegado` | 8 | integration | `[Theory]` 3 actores: (a) usuario propio `Recepcionista`; (b) admin propio con `Activo = false` en la base; (c) `UsuarioId` = Guid nuevo | `AccesoDenegadoException` con `No tenés permiso para administrar usuarios.` en los 3; objetivo sin cambios; 0 filas con su id |
| T7 | `RestablecerContrasenaAsync_UsuarioInexistente_LanzaNoEncontrado` | 9 | integration | Admin propio; Guid nuevo | `NoEncontradoException` con `No encontramos ese usuario. Recargá la página.` |
| T8 | `RestablecerContrasenaAsync_OtroCambioSeAdelanta_LanzaConflictoSinCambios` | 11 | integration | Admin y usuario creados con el proveedor **sin** interceptor; segundo proveedor con `InterceptorQueSeAdelanta(fixture.CadenaApp, "UPDATE asp_net_users SET concurrency_stamp = 'otro-<m>' WHERE id = '<id>'")`; restablecer con ése | `interceptor.SeAdelanto` = `true`; `ConflictoException` con el texto del AC 11; la contraseña anterior sigue verificando, marca `false`, `lockout_end` igual; 0 filas `usuario-personal.contrasena-restablecida` con su id |
| T9 | `RestablecerContrasenaAsync_FallaLaAuditoria_NoCambiaNada` | 10 | integration | Admin y usuario creados con el proveedor normal; segundo proveedor con `IAuditoria` = `AuditoriaQueFalla`; restablecer | La excepción sale del caso de uso; la contraseña anterior sigue verificando; marca `false`; sello igual al guardado antes |
| T10 | `GetUsuarios_BotonNuevaContrasena_SoloEnActivosAjenos` | 12 | integration | `admin1`; usuarios propios: activo, desactivado, bloqueado (5 fallos); GET A4 | Fila del activo y del bloqueado: `value="restablecer:<id>"` y el texto `Nueva contraseña temporal`; fila del desactivado: no contiene `restablecer:`; fila de `admin1`: no contiene `restablecer:` y dice `(sos vos)` |
| T11 | `PostRestablecer_UsuarioActivo_MuestraLaTemporalUnaSolaVez` | 1,2,4,13 | integration | `admin1`; usuario propio `Recepcionista` con `ClavePropia`; POST `acciones-usuarios` con `Accion = restablecer:<id>`; después GET A4; después, en otro cliente, ingreso con `ClavePropia` y con la temporal | POST: `200`, `no-store`, `Estado(html)` = texto del AC 13 con su usuario y la temporal, `<code id="contrasena-temporal">` con 12 caracteres; la fila dice `Debe cambiar la contraseña`; ninguna entrada de `app.Registro` ni el `detalle` de su fila de auditoría contiene la temporal; GET: no contiene la temporal ni `contrasena-temporal`; ingreso con la anterior: `200` con `Usuario o contraseña incorrectos.`; con la temporal: `302` a `/personal/cambiar-contrasena` |
| T12 | `PostRestablecer_UsuarioBloqueado_AvisaQueTambienQuedoDesbloqueado` | 5,13 | integration | `admin1`; usuario propio con `FallarAsync(app, id, 5)`; POST `restablecer:<id>` | `200`; `Estado(html)` termina con `Su usuario también quedó desbloqueado.`; su fila ya no dice `Bloqueado hasta las` ni tiene `desbloquear:` |
| T13 | `PostRestablecer_Errores_MuestranCartelConSuCodigo` | 6,7,8,9 | integration | `[Theory]`: (a) admin propio (`Admin-Propio-26`) ingresa y envía `restablecer:<su id>`; (b) `admin1` a un desactivado; (c) `admin1` a un Guid nuevo; (d) admin propio ingresa, se lo desactiva en la base (`MarcarInactivoAsync`) y, **sin** adelantar el reloj, envía `restablecer:` a un usuario activo | (a) `400`; (b) `409`; (c) `404`; (d) `403`; cada uno con su texto exacto en `Alerta(html)`; en la base, el sello y la marca del objetivo (y del admin propio en (a)) sin cambio |
| T14 | `Restablecer_SesionAbiertaDelUsuario_SeCortaAlRevalidar` | 3 | integration | Usuario propio `Recepcionista` ingresa en el cliente B; `admin1` le restablece la contraseña desde el cliente A; `app.Reloj.Advance(6 min)`; B pide `/personal/recepcion/turnos` | `302` con destino `/personal/ingreso?ReturnUrl=%2Fpersonal%2Frecepcion%2Fturnos` |
| T15 | `PostRestablecer_SinTokenOOtroRol_NoCambiaNada` | 14 | integration | `[Theory]`: (a) `admin1` envía `restablecer:<id>` sin `__RequestVerificationToken`; (b) `recepcion1` envía `restablecer:<id>` con el token de su propia página de inicio (como la T21 de la 2.2) | (a) `400`; (b) `302` con destino que empieza con `/personal/acceso-denegado`; en los dos, sello, hash y marca del objetivo sin cambio |
| T16 | `PostRestablecer_FallaLaBase_MuestraErrorGenericoSinContrasena` | 15,16 | integration | Fábrica con `IGestionUsuariosPersonal` = `GestionUsuariosConBaseCaida`; POST `restablecer:<Guid nuevo>` con `Accept: text/html` | `500`; el cuerpo decodificado contiene `Algo falló. Probá de nuevo en unos minutos.` y no contiene `Npgsql`, `secreto` ni `contrasena-temporal`; `app.Registro` tiene la excepción |

Todas llevan `[Trait("Story", "2.3")]` y un `[Trait("AC", "n")]` por AC de su columna. El AC 17
(no-regresión) lo verifican la suite completa y `dotnet test --filter "Story=2.2"` sin cambios en
esas pruebas ("Como verificar", pasos 2 y 3).
T1–T9 van en `tests/HospitalTurnos.Aplicacion.Tests/Personal/GestionUsuariosPersonalIdentityTests.cs`
y T10–T16 en `tests/HospitalTurnos.Web.Tests/Personal/UsuariosPersonalHttpTests.cs`, **en las mismas
clases** de la 2.2, para reusar sus ayudas privadas (ver Given/Fixture) en lugar de copiarlas. Las
pruebas de la 2.2 de esos archivos no se tocan.

> El estimado del plan era 4 pruebas; quedan 16. La acción en sí es una sola, pero trae los mismos
> modos de falla que las otras acciones de A4 (permiso en la base, 404 con cartel, token, base caída,
> atomicidad), más tres que son sólo suyos: usuario bloqueado, desactivado y la carrera con otro
> cambio. La relación (4 veces) está en línea con la del sprint 2 (3,1 a 3,9).

**Por qué no hay E2E (regla 6.1 del formato).** El proyecto `tests/HospitalTurnos.E2E/` todavía no
existe (lo crea la 10.1). La acción es un botón de un formulario SSR sin JavaScript propio: T10–T16
reproducen el GET y el POST del navegador con `HttpClient` sobre la app real (token y cookies
incluidos). La verificación en el navegador queda en "Como verificar", paso 4, con su resultado en el
Dev Agent Record.

#### Given/Fixture

- **Base**: la `PostgresFixture` de cada proyecto y la colección `"Postgres"`; no se crean ni se
  modifican. Base compartida y `registros_auditoria` sin limpiar: **cada prueba crea sus propios
  usuarios** y filtra la auditoría por `entidad_id`.
- **Aplicacion.Tests — ayudas ya existentes de `GestionUsuariosPersonalIdentityTests`** (privadas, por
  eso las pruebas van en esa clase): `Proveedor(reemplazos)`, `RelojDeCordoba()` (arranca en la hora
  real: Identity fija `LockoutEnd` con el reloj del sistema), `Marca()`, `ConGestionAsync`,
  `RecargarAsync`, `AdministradorAsync`, `CapturaDeRegistros`, `FilasDeAsync`, `Detalle`,
  `ActorAuditado`. Usuarios con `UsuariosDePrueba.CrearAsync(proveedor, roles, activo, nombre)` y
  contraseña `UsuariosDePrueba.Clave`. Si alguna de esas ayudas tiene otro nombre al implementar, se
  usa la que existe (Task 1), no se escribe otra.
- **Verificar una contraseña sin ingresar**: `IPasswordHasher<UsuarioPersonal>.VerifyHashedPassword`
  sobre el usuario recargado, como la T1 de la 2.2.
- **T3**: 5 llamadas a `UserManager.AccessFailedAsync` en un alcance (como la T12 de la 2.2).
- **T6 (b)**: admin propio creado con `activo: false`. **T6 (c)**: un `ActorAuditoria` armado a mano
  con `UsuarioId = Guid.NewGuid().ToString()`, rol `Administrador`, canal `Personal`.
- **T8 — `InterceptorQueSeAdelanta`** (3.1, Aplicacion.Tests/Ayudas): se dispara una sola vez, en el
  **primer** `SaveChanges` de cualquier contexto del proveedor. Por eso el admin y el usuario se crean
  con un proveedor **sin** interceptor, y la acción corre en un segundo proveedor
  `Proveedor(s => s.ConfigureDbContext<HospitalDbContext>(o => o.AddInterceptors(interceptor)))`. El
  primer guardado de la acción es el de `RemovePasswordAsync` (`UserStore.UpdateAsync`): el `UPDATE`
  del interceptor, por otra conexión y confirmado en el acto, cambia el `concurrency_stamp`, y el
  `UPDATE … WHERE concurrency_stamp = <el leído>` de Identity no encuentra la fila. Es el mismo doble
  que la 3.1 y la 3.2 usan para el índice único, con otro SQL: no se crea un doble nuevo. **Romper a
  propósito** (L-2): sin la traducción de `ConcurrencyFailure`, T8 tiene que fallar con
  `InvalidOperationException`.
- **T9 — `AuditoriaQueFalla`** (2.2, Aplicacion.Tests/Personal/Dobles), registrada sólo en el
  segundo proveedor (`s.AddScoped<IAuditoria, AuditoriaQueFalla>()`).
- **Web.Tests — ayudas ya existentes de `UsuariosPersonalHttpTests`**: `Admin1Async`,
  `IngresadoAsync`, `AccionAsync(cliente, accion, conToken)`, `HtmlAsync`, `FilaDe`, `BuscarAsync`,
  `MarcarInactivoAsync`, `ClavePropia`; y de `FormularioHttp`: `CrearUsuarioAsync(app, roles, contrasena, activo:)`,
  `FallarAsync(app, id, veces)`, `RecargarAsync(app, id)`, `IngresarAsync`, `Destino`, `Estado`,
  `Alerta`, `Cliente`. Fábrica `PersonalWebApplicationFactory` sin cambios (`app.Reloj`,
  `app.Registro`). `admin1` actúa sólo cuando **el objetivo** es un usuario propio: su estado nunca
  cambia. T13 (a) y (d) usan un administrador propio.
- **T11**: la temporal se lee con la expresión de la 2.2 sobre `<code id="contrasena-temporal">(.*?)</code>`.
- **T14**: los dos clientes salen de la misma fábrica y comparten `app.Reloj` (como la T27 de la 2.2).
- **T15 (b)**: el token sale de la página de inicio de `recepcion1` (A4 le responde `403`), como hace
  la T21 de la 2.2; el POST se arma igual que allí.
- **T16**: `app.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddScoped<IGestionUsuariosPersonal, GestionUsuariosConBaseCaida>()))`
  y `Accept: text/html` en el POST (sin ese encabezado el manejador de la 1.2 responde
  `problem+json`: lección de la 2.1 T15). `GestionUsuariosConBaseCaida.RestablecerContrasenaAsync`
  lanza `new NpgsqlException("detalle secreto de la base")`, como sus otros métodos de cambio.
- **Textos**: se comparan después de `WebUtility.HtmlDecode`.
- **Reloj**: ninguna prueba espera tiempo real. El corte de sesión (T14) se prueba adelantando el
  `FakeTimeProvider`; "bloqueado ahora" se decide con `reloj.GetUtcNow()` (A-4).

## Tasks / Subtasks

- [ ] [SEQ] Task 1: Salvaguarda de estado (AC: todos)
  - [ ] Confirmar contra `main` que existe y dice lo que cita "Que existe hoy": `IGestionUsuariosPersonal`
        (cinco métodos), `GestionUsuariosPersonalIdentity` (constructor, `SinPermiso`, `EntidadTipo`,
        `BuscarAsync`, `Asegurar`), `ContrasenaTemporal.Generar`, `UsuarioPersonal.DebeCambiarContrasena`,
        `AccionesAuditoria` (sin `ContrasenaPersonalRestablecida`), `UsuariosPersonal.razor` (`EjecutarAccionAsync`
        con el `switch` de tres tipos, `alta`/`confirmacion`/`cartel`, `idPropio`), `GestionUsuariosConBaseCaida`,
        `InterceptorQueSeAdelanta`, `AuditoriaQueFalla`, y las ayudas privadas citadas en Given/Fixture.
  - [ ] Confirmar en `HospitalDbContextModelSnapshot.cs` que `concurrency_stamp` de `asp_net_users` es
        `IsConcurrencyToken()`. No hay migración: no se toca la base.
  - [ ] Las decisiones 14 a 18 están tomadas con la opción recomendada (ver "Decisiones tomadas por el
        docente"): los AC y las pruebas ya las reflejan.
  - [ ] `dotnet test` en verde antes de tocar nada; anotar los totales por proyecto.
  - [ ] Si algo no coincide → DETENERSE y corregir la story primero.
- [ ] [SEQ] Task 2: Dominio y Aplicacion (AC: 1, 16)
  - [ ] `AccionesAuditoria.ContrasenaPersonalRestablecida = "usuario-personal.contrasena-restablecida"`,
        en el bloque de la 2.2 con un comentario `// Story 2.3`.
  - [ ] `IGestionUsuariosPersonal.RestablecerContrasenaAsync` y el registro `ContrasenaRestablecida`.
  - [ ] `GestionUsuariosConBaseCaida.RestablecerContrasenaAsync` (si no, Web.Tests no compila).
- [ ] [SEQ] Task 3: Pruebas de Aplicacion primero (AC: 1–11, 16)
  - [ ] T1–T9 en `GestionUsuariosPersonalIdentityTests`; correrlas y anotar el rojo (no compila o falla
        por el método ausente: rojo legítimo).
- [ ] [SEQ] Task 4: Infraestructura (AC: 1–11)
  - [ ] `GestionUsuariosPersonalIdentity.RestablecerContrasenaAsync` (Implementación concreta) y la
        ayuda `AsegurarSinCarrera`.
  - [ ] T1–T9 en verde. Romper a propósito: sin la traducción de `ConcurrencyFailure`, T8 falla;
        anotarlo en el Debug Log y restaurar.
- [ ] [SEQ] Task 5: Web (AC: 12–15)
  - [ ] T10–T16 en `UsuariosPersonalHttpTests` (rojo primero).
  - [ ] `UsuariosPersonal.razor`: el botón por fila, el `case "restablecer"` y el mensaje del AC 13
        (Implementación concreta).
  - [ ] T10–T16 en verde.
- [ ] [SEQ] Task 6: Verificación y pasada del tester no técnico (AC: todos; B-6)
  - [ ] Suite completa y `bash scripts/ci/local.sh` sin regresiones (AC 17).
  - [ ] Escribir `docs/qa/2.3-pasos.md` con el formato de `docs/qa/2.2-pasos.md` (un bloque por AC,
        en castellano llano, con usuario y resultado esperado).
  - [ ] Pasada del **tester no técnico** sobre esa guía y sobre A4 con el botón nuevo (texto del botón,
        el mensaje con la contraseña, el cartel de cada error). Sus hallazgos vuelven al docente antes
        de cambiar textos de los AC.
  - [ ] Prueba en vivo (Como verificar, paso 4). Los scripts del navegador buscan por **texto o rol**
        (el botón por su nombre `Nueva contraseña temporal` dentro de la fila del usuario), no por
        clases CSS (L-14). Resultado en el Dev Agent Record.

### Task Dependencies

T1 → T2 → T3 → T4 → T5 → T6 (secuencial). Dentro de T6, la guía `2.3-pasos.md` y la prueba en vivo
pueden ir en paralelo; la pasada del tester va después de la guía.

## Dev Notes

### Que existe hoy

#### En hospital-turnos (verificado 2026-10-17 contra `main`, commit `a8432c3`, con 1.1 a 4.1 en done)

- `IGestionUsuariosPersonal` (Aplicacion/Personal): `ListarAsync`, `CrearAsync`, `DesactivarAsync`,
  `ReactivarAsync`, `DesbloquearAsync`; registros `NuevoUsuarioPersonal`, `AltaUsuarioPersonal(Id, Usuario, Rol, ContrasenaTemporal)`,
  `UsuarioPersonalFila(Id, Usuario, NombreCompleto, Rol, Activo, BloqueadoHasta, DebeCambiarContrasena)`.
  La implementan sólo `GestionUsuariosPersonalIdentity` y el doble de pruebas `GestionUsuariosConBaseCaida`.
- `GestionUsuariosPersonalIdentity` (Infraestructura/Identidad), constructor
  `(UserManager<UsuarioPersonal> usuarios, HospitalDbContext contexto, IAuditoria auditoria, IActorVigente actorVigente, IdentityErrorDescriber errores, TimeProvider reloj)`.
  Constantes privadas `SinPermiso = "No tenés permiso para administrar usuarios."` y
  `EntidadTipo = "UsuarioPersonal"`. Cada acción: `BeginTransactionAsync` → `ExigirAdministradorActivoAsync`
  → `BuscarAsync` (lanza `NoEncontradoException("No encontramos ese usuario. Recargá la página.")`) →
  regla → cambio con `UserManager` → `auditoria.Registrar(admin, …)` → `SaveChangesAsync` → `CommitAsync`.
  `Asegurar(IdentityResult)` lanza `InvalidOperationException` si Identity falla. `DesbloquearAsync`
  decide "bloqueado" con `usuario.LockoutEnd > reloj.GetUtcNow()` y usa `SetLockoutEndDateAsync(usuario, null)`
  + `ResetAccessFailedCountAsync`. **Hoy ninguna acción traduce `ConcurrencyFailure`**: una carrera
  terminaría en `Asegurar` → 500 (ver Decisiones).
- `ContrasenaTemporal.Generar()` (Infraestructura/Identidad, estática): 12 caracteres, uno de cada
  grupo, sin confundibles, `RandomNumberGenerator`.
- `UsuarioPersonal.DebeCambiarContrasena` (`bool`, por defecto `false`), columna
  `debe_cambiar_contrasena` (migración `20261016212111_GestionUsuariosPersonal`).
- `IngresoPersonalIdentity.ValidarAsync` (2.1/2.2): paso 3, **si está bloqueado no mira la
  contraseña** (`IsLockedOutAsync` → `Bloqueado`); paso 5, con la contraseña buena, un desactivado da
  `Desactivado` (S0: `Tu usuario está desactivado. Pedile al administrador que lo reactive.`); paso 6,
  éxito con la marca. Por eso una temporal nueva a un bloqueado no sirve hasta que vence el bloqueo,
  y a un desactivado no sirve nunca (decisiones 14 y 15).
- `CambioContrasenaPersonalIdentity.CambiarAsync`: audita `motivo = "primer-ingreso"` si la marca está
  en `true`, si no `"voluntario"` (decisión 18).
- `AccionesAuditoria`: las 9 de la 1.3, 3 de la 2.1, 5 de la 2.2 (`usuario-personal.creado`,
  `.desactivado`, `.reactivado`, `.desbloqueado`, `.contrasena-cambiada`), 3 de la 3.1, 2 de la 3.2.
  No existe `contrasena-restablecida`.
- `UsuariosPersonal.razor` (Components/Personal/Administracion, modificada por la 3.2 con la columna
  `Médico vinculado`): `OnInitializedAsync` fija `Cache-Control: no-store` y `idPropio`; un solo
  `<form @formname="acciones-usuarios">` con `[SupplyParameterFromForm(FormName = "acciones-usuarios")] string? Accion`;
  `EjecutarAccionAsync` separa `tipo:id`, busca el usuario en `filas` para el mensaje y tiene un
  `switch` con `desactivar`, `reactivar`, `desbloquear` y `default` → `ReglaDeNegocioException(AccionDesconocida)`;
  `catch (Exception e) when (ManejadorGlobalDeErrores.EsEsperable(e))` → `MostrarCartel(e)`
  (`FijarEstadoEnPagina` + `cartel`); `await CargarAsync()` al final. El `role="status"` dibuja `alta`
  (con `<code id="contrasena-temporal">`) **o** `confirmacion` (texto simple). La fila propia dice
  `(sos vos)` en lugar de `Desactivar`.
- `RegistroIdentidad.AddIdentidadPersonal` usa `AddIdentityCore` **sin** `AddDefaultTokenProviders`.
- Pruebas: `GestionUsuariosPersonalIdentityTests` y `UsuariosPersonalHttpTests` (2.2) con las ayudas
  privadas citadas en Given/Fixture; `InterceptorQueSeAdelanta(string cadena, string sqlQueInserta)`
  (3.1) en Aplicacion.Tests/Ayudas; `AuditoriaQueFalla` en Aplicacion.Tests/Personal/Dobles;
  `GestionUsuariosConBaseCaida` en Web.Tests/Personal/Dobles. La guía de la 2.2 para el tester:
  `docs/qa/2.2-pasos.md`.

#### En el framework (verificado con `ilspycmd` sobre `Microsoft.Extensions.Identity.Core.dll` 10.0.11 y `Microsoft.AspNetCore.Identity.EntityFrameworkCore.dll` 10.0.13, la versión de `Directory.Packages.props`)

- `UserManager.ResetPasswordAsync(user, token, nueva)` exige un token de `GeneratePasswordResetTokenAsync`,
  que necesita un proveedor de tokens. Con `AddIdentityCore` sin `AddDefaultTokenProviders` no hay
  ninguno: **no se usa**.
- `RemovePasswordAsync(user)` → `UpdatePasswordHash(…, null, validatePassword: false)` → deja el hash
  en `null` y llama a `UpdateSecurityStampInternal`; después `UpdateUserAsync`.
  `AddPasswordAsync(user, p)` falla con `UserAlreadyHasPassword` si el hash no es `null`; si no,
  `UpdatePasswordHash` (valida contra la política, hashea, cambia el sello) y `UpdateUserAsync`. O
  sea: **quitar y poner** cambia el sello sin llamar aparte a `UpdateSecurityStampAsync`, y valida la
  temporal contra la política real.
- `UserStore.UpdateAsync` (EF): `Attach`, `ConcurrencyStamp = Guid.NewGuid()`, `Update`, `SaveChanges`;
  si salta `DbUpdateConcurrencyException`, **devuelve** `IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure())`
  (código `ConcurrencyFailure`), no lanza. `ConcurrencyStamp` es token de concurrencia en el modelo
  (snapshot: `IsConcurrencyToken()`, columna `concurrency_stamp`). Es la "última defensa en la base"
  de esta story (regla 4 del brief), como el índice único en las de catálogo.
- `AutoSaveChanges = true`: cada llamada de `UserManager` guarda sobre el `HospitalDbContext` del
  alcance. Con la transacción abierta en ese contexto, todo queda dentro.

### Patron de referencia verificado

Proyecto único (sin plugins hermanos): el patrón es el de la propia 2.2 y la 3.1, en este repo.

| Ancla | Qué se verificó y qué se toma |
|---|---|
| `GestionUsuariosPersonalIdentity.DesbloquearAsync` | Transacción → actor vigente → objetivo → regla → `UserManager` → auditoría → guardar → confirmar. Se toma igual, y el desbloqueo (`SetLockoutEndDateAsync(null)` + `ResetAccessFailedCountAsync`) se repite tal cual para la decisión 14. |
| `GestionUsuariosPersonalIdentity.CrearAsync` | `ContrasenaTemporal.Generar()` y la temporal que vuelve sólo en el registro de respuesta, nunca auditada. Se toma igual. |
| `GestionUsuariosPersonalIdentity.DesactivarAsync` | La regla "no a uno mismo" con `ReglaDeNegocioException` (400), comparando con el id que devuelve `IActorVigente`. Se toma para el AC 7. |
| `UsuariosPersonal.razor` (`EjecutarAccionAsync`, bloque `role="status"` de `alta`) | Un `case` más en el `switch` del formulario único, y la contraseña en `<code id="contrasena-temporal">`. Se toma sin cambiar el patrón. |
| `CatalogoEspecialidadesTests` (3.1, carrera con `InterceptorQueSeAdelanta`) | La carrera provocada por otra conexión justo antes del guardado, con la aserción `SeAdelanto`. Se toma para T8 con un `UPDATE` en lugar de un `INSERT`. |
| `UsuariosPersonalHttpTests` (2.2: `PostAlta_DatosValidos_MuestraLaContrasenaTemporalUnaSolaVez`, `PostAcciones_Errores_MuestranCartelConSuCodigo`, `Desactivar_SesionAbiertaDelUsuario_SeCortaAlRevalidar`, `Post_SinTokenAntiforgery_Devuelve400SinCambios`, `PostAlta_FallaLaBase_MuestraErrorGenericoSinContrasena`) | Forma de cada prueba HTTP de A4. T11–T16 las siguen. |

### Que hay que hacer (la delta)

1. Un método nuevo en el caso de uso de A4: restablecer la contraseña (con desbloqueo), auditado, en
   una transacción, con la carrera traducida.
2. Un botón por fila y un mensaje en A4.
3. Una constante de auditoría.
4. La guía del tester (`docs/qa/2.3-pasos.md`).

Sin migración, sin pantalla nueva, sin cambios en el ingreso ni en el cambio de contraseña.

### Decisiones de esta story (con su porqué)

- **Quitar y poner la contraseña, no el token de restablecimiento.** El token exige un proveedor que
  hoy no está registrado (`AddIdentityCore` a secas) y sirve para enlaces por email, que acá no hay.
  `RemovePasswordAsync` + `AddPasswordAsync` dentro de la transacción hacen lo mismo con lo que ya
  existe, cambian el sello y validan la temporal contra la política.
- **La carrera se traduce a `ConflictoException`.** Identity ya trae el control (el `concurrency_stamp`):
  sin traducirlo, dos administradores sobre el mismo usuario darían un 500. Se traduce sólo el código
  `ConcurrencyFailure`; cualquier otro fallo de Identity sigue a `Asegurar`. No se agrega un candado
  `pg_advisory_xact_lock` como en `DesactivarAsync`: allí se protege una regla entre **dos** usuarios
  (siempre queda un administrador); acá el choque es sobre **una** fila, y para eso alcanza su sello.
- **La temporal se muestra en pantalla una sola vez, no se manda por email**, por lo mismo que en la
  2.2: el personal no tiene email obligatorio y la respuesta va con `no-store`.
- **Sin redirección después de la acción** (como el alta de la 2.2): redirigir obligaría a guardar la
  contraseña para mostrarla en el GET. Si se recarga la página, el navegador reenvía el POST y se
  genera **otra** temporal: la anterior (que nadie anotó) deja de servir y la nueva se muestra. No daña
  nada y queda auditado.
- **Texto del botón: `Nueva contraseña temporal`.** UX no lo fija. Nombra lo que la persona va a
  recibir, en las mismas palabras que el alta (`Contraseña temporal: …`). Lo revisa la pasada del
  tester no técnico (Task 6).
- **Usuarios `(sin rol)` o `(varios roles)` también tienen el botón.** No pueden ingresar hasta que se
  corrija su rol (2.1), pero eso no depende de su contraseña: no hay motivo para negarla.

### Decisiones tomadas por el docente (2026-10-17)

Numeradas como en `revision-sprint-3.md` ("Decisiones abiertas consolidadas"). Se aceptaron todas con
la opción recomendada; los AC y las pruebas ya las reflejan.

- **Decisión 14 — Usuario bloqueado por intentos** (era D-1; AC 5, 13, T3, T12). **Elegido:** generar
  la temporal también lo desbloquea, en la misma acción y el mismo registro (`desbloqueado` verdadero).
  Quien olvidó su contraseña casi siempre se bloqueó probando; sin desbloquear, la temporal no
  serviría hasta 15 minutos (el ingreso ni mira la contraseña de un bloqueado).
- **Decisión 15 — Usuario desactivado** (era D-2; AC 6, 12, T4, T10, T13 b). **Elegido:** no se puede
  (`409`, sin botón). Una temporal para alguien que no puede entrar no sirve, y entregar credenciales
  a quien se fue es un riesgo; reactivar es un paso aparte y auditado.
- **Decisión 16 — El administrador sobre sí mismo** (era D-3; AC 7, 12, T5, T10, T13 a). **Elegido:**
  no se puede (`400`, sin botón, `(sos vos)`). Si está ingresado tiene `/personal/cambiar-contrasena`;
  si la olvidó, se la genera otro administrador.
- **Decisión 17 — Confirmación antes de generar** (era D-4; AC 13). **Elegido:** sin paso de
  confirmación. La temporal nueva se ve en pantalla en el acto y queda auditada.
- **Decisión 18 — Motivo auditado del cambio posterior** (era D-5; AC 4, T2). **Elegido:** queda
  `"primer-ingreso"`, como hoy en la 2.2. En A5 el registro `contrasena-restablecida` anterior explica
  el porqué; la 2.2 no se toca.
- **Decisión 26 — La carrera en las otras acciones de A4.** Las acciones de la 2.2 (`DesactivarAsync`,
  `ReactivarAsync`, `DesbloquearAsync`) tampoco traducen `ConcurrencyFailure`: dos administradores
  sobre el mismo usuario a la vez darían un 500 en lugar del cartel. **Elegido:** lo resuelve una
  story chica aparte, la **2.5**, en el sprint 4. Esta story lo resuelve sólo para la acción nueva y
  no toca las otras tres (AC 17).

### Implementacion concreta

**`IGestionUsuariosPersonal.cs`** (se suma; lo demás no cambia):

```csharp
/// Genera otra contraseña temporal para quien olvidó la suya (story 2.3). La anterior deja de servir.
Task<ContrasenaRestablecida> RestablecerContrasenaAsync(Guid usuarioId, ActorAuditoria actor, CancellationToken ct);

// La contraseña viaja sólo en este objeto, de vuelta a la página que la muestra una vez (como AltaUsuarioPersonal).
public sealed record ContrasenaRestablecida(Guid Id, string Usuario, string ContrasenaTemporal, bool Desbloqueado);
```

**`GestionUsuariosPersonalIdentity.RestablecerContrasenaAsync`**:

```csharp
private const string OtroCambio = "Otro administrador cambió a ese usuario recién. Recargá la página y probá de nuevo.";

public async Task<ContrasenaRestablecida> RestablecerContrasenaAsync(Guid usuarioId, ActorAuditoria actor, CancellationToken ct)
{
    await using var tx = await contexto.Database.BeginTransactionAsync(ct);
    var admin = await actorVigente.ExigirAdministradorActivoAsync(actor, SinPermiso, ct);   // AC 8
    var usuario = await BuscarAsync(usuarioId);                                            // AC 9
    if (usuario.Id == Guid.Parse(admin.UsuarioId!))                                        // AC 7 (decisión 16)
        throw new ReglaDeNegocioException(
            "No podés generarte una contraseña temporal a vos mismo. Si querés otra, cambiala en /personal/cambiar-contrasena.");
    if (!usuario.Activo)                                                                   // AC 6 (decisión 15)
        throw new ConflictoException("Ese usuario está desactivado. Reactivalo antes de generarle otra contraseña.");

    var desbloqueado = usuario.LockoutEnd > reloj.GetUtcNow();
    var temporal = ContrasenaTemporal.Generar();
    usuario.DebeCambiarContrasena = true;   // la guarda el primer UpdateUserAsync de abajo
    // Quitar y poner: los dos cambian el sello (la sesión abierta se corta al revalidar, AC 3) y AddPassword
    // valida la temporal contra la política. ResetPasswordAsync no: pide un proveedor de tokens que no hay.
    AsegurarSinCarrera(await usuarios.RemovePasswordAsync(usuario));   // primer guardado: acá salta la carrera (AC 11)
    AsegurarSinCarrera(await usuarios.AddPasswordAsync(usuario, temporal));
    if (desbloqueado)                                                  // AC 5 (decisión 14)
        AsegurarSinCarrera(await usuarios.SetLockoutEndDateAsync(usuario, null));
    AsegurarSinCarrera(await usuarios.ResetAccessFailedCountAsync(usuario));
    auditoria.Registrar(admin, AccionesAuditoria.ContrasenaPersonalRestablecida, EntidadTipo,
        usuario.Id.ToString(), new { usuario = usuario.UserName, desbloqueado });   // nunca la contraseña
    await contexto.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);
    return new ContrasenaRestablecida(usuario.Id, usuario.UserName!, temporal, desbloqueado);
}

// El sello de concurrencia de Identity es la última defensa: otro pedido cambió la fila entre la lectura y
// el guardado. Se traduce a la regla (409 con cartel), nunca a un error interno. Lo demás, como Asegurar.
private static void AsegurarSinCarrera(IdentityResult resultado)
{
    if (resultado.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.ConcurrencyFailure)))
        throw new ConflictoException(OtroCambio);
    Asegurar(resultado);
}
```

Si la transacción no se confirma (cualquier excepción, incluida la de `AsegurarSinCarrera`), el
`await using` la deshace: la contraseña anterior sigue (AC 10, 11).

**`GestionUsuariosConBaseCaida`** (Web.Tests/Personal/Dobles): suma
`RestablecerContrasenaAsync(...) => throw new NpgsqlException("detalle secreto de la base");`.

**`UsuariosPersonal.razor`**:

- Campo nuevo `private ContrasenaRestablecida? restablecida;`.
- En el bloque `role="status"`, una rama más entre `alta` y `confirmacion`:

```razor
else if (restablecida is not null)
{
    @* La contraseña se dibuja acá una sola vez, como la del alta (story 2.3, AC 2). *@
    <div class="alert alert-success" role="status">Nueva contraseña temporal para @restablecida.Usuario: <code id="contrasena-temporal">@restablecida.ContrasenaTemporal</code>. Anotala y dásela a la persona: no se vuelve a mostrar. La anterior ya no sirve y la tiene que cambiar en su próximo ingreso.@(restablecida.Desbloqueado ? " Su usuario también quedó desbloqueado." : "")</div>
}
```

- En la columna `Acciones`, después de los botones de la 2.2 y fuera de la rama de la fila propia,
  para filas con `fila.Activo && fila.Id != idPropio`:
  `<button type="submit" class="btn btn-sm btn-outline-secondary" name="Accion" value="restablecer:@fila.Id">Nueva contraseña temporal</button>`.
- En el `switch` de `EjecutarAccionAsync`:

```csharp
case "restablecer":
    restablecida = await Gestion.RestablecerContrasenaAsync(id, actor, ct);
    break;
```

  El `catch` filtrado, `MostrarCartel` y el `await CargarAsync()` final ya existen: la fila vuelve con
  `Debe cambiar la contraseña` y sin `Bloqueado hasta` en la misma respuesta.

**`AccionesAuditoria.cs`**: `public const string ContrasenaPersonalRestablecida = "usuario-personal.contrasena-restablecida";`
con el comentario `// Story 2.3: contraseña olvidada, generada otra temporal desde A4.`

### Archivos a crear/modificar

| Acción | Archivo |
|---|---|
| Crear | `docs/qa/2.3-pasos.md` (guía del tester, Task 6) |
| Modificar | `AccionesAuditoria.cs` (1 constante, AC 16) |
| Modificar | `IGestionUsuariosPersonal.cs` (método y registro nuevos) |
| Modificar | `GestionUsuariosPersonalIdentity.cs` (`RestablecerContrasenaAsync`, `AsegurarSinCarrera`, `OtroCambio`) |
| Modificar | `UsuariosPersonal.razor` (botón, `case`, mensaje) |
| Modificar | `GestionUsuariosPersonalIdentityTests.cs` (T1–T9, sin tocar las de la 2.2) |
| Modificar | `UsuariosPersonalHttpTests.cs` (T10–T16, sin tocar las de la 2.2) |
| Modificar | `GestionUsuariosConBaseCaida.cs` (el método nuevo) |

### Lo que NO hay que hacer

- **NO crear otro generador de contraseñas** ni copiar `ContrasenaTemporal`; **NO** otra marca de
  "debe cambiar": se reusan las de la 2.2.
- **NO usar `ResetPasswordAsync` ni registrar `AddDefaultTokenProviders`** (ver Decisiones).
- **NO mostrar, registrar ni auditar la temporal** fuera de la única respuesta: ni en el `ILogger`, ni
  en `Detalle`, ni en `TempData`, ni en la dirección (`?pwd=`).
- **NO mandarla por email** ni por la bandeja de salida.
- **NO redirigir después de la acción** (perdería la contraseña; ver Decisiones).
- **NO crear una pantalla aparte** ni un formulario nuevo: es un tipo más del formulario único
  `acciones-usuarios` (Blazor no admite dos formularios con el mismo nombre).
- **NO atrapar `Exception` sin filtro** en la página: sólo `when (ManejadorGlobalDeErrores.EsEsperable(e))`.
- **NO cambiar las acciones de la 2.2** (ni su manejo de la carrera) ni sus pruebas: AC 17; la carrera
  en esas acciones es de la 2.5 (decisión 26).
- **NO cambiar `CambioContrasenaPersonalIdentity`** (decisión 18).
- **NO usar `DateTime.Now`/`UtcNow` ni `DateTimeOffset.UtcNow`** en `src/`: "bloqueado ahora" con
  `reloj.GetUtcNow()`.
- **NO cambiar los usuarios fijos** (`recepcion1`, `medico1`, `admin1`) en ninguna prueba.
- **NO contar filas de `registros_auditoria` sin filtrar** por un id propio.
- **NO crear una migración**: no hay cambios de modelo.

### Como verificar que funciona

```bash
cd ~/SourceCode/hospital-turnos

# 1. Pruebas de esta story (Docker para Testcontainers)
dotnet test --filter "Story=2.3"
# Esperado: 16 pruebas declaradas (más los casos de cada [Theory]), 0 fallas
dotnet test --filter "Story=2.3&AC=11"     # sólo la carrera

# 2. Las de la 2.2 siguen igual (AC 17)
dotnet test --filter "Story=2.2"

# 3. Suite completa y CI local
dotnet test
bash scripts/ci/local.sh; echo "rc=$?"    # rc=0

# 4. Verificación manual en el navegador
docker compose up -d --build
open http://localhost:8080/personal/ingreso
# Esperado, en este orden (ventana normal: administrador; ventana privada: la otra persona):
#  a. Como administrador, en "Usuarios del personal" dar de alta "Ana Ruiz" / "ana.ruiz" / Médico y
#     anotar la temporal. En la privada, entrar con ella y elegir "Clave-Propia-2026". Salir.
#  b. Como administrador, en la fila de ana.ruiz: botón "Nueva contraseña temporal". En la fila
#     propia NO hay ese botón (dice "(sos vos)").
#  c. Pulsarlo: cartel verde "Nueva contraseña temporal para ana.ruiz: …" con la contraseña; la fila
#     dice "Debe cambiar la contraseña". "Atrás" y volver a entrar a la pantalla NO la muestran otra vez.
#  d. En la privada: con "Clave-Propia-2026" → "Usuario o contraseña incorrectos."; con la temporal →
#     pantalla "Elegí tu contraseña"; elegir otra y entrar.
#  e. En la privada, 5 contraseñas malas con ana.ruiz → como administrador la fila dice "Bloqueado
#     hasta las HH:mm"; "Nueva contraseña temporal" → el cartel termina con "Su usuario también quedó
#     desbloqueado."; en la privada entra en el acto con la temporal y elige otra.
#  f. Con ana.ruiz ingresada en la privada, generarle otra temporal desde la normal; a los 5 minutos,
#     cualquier clic en la privada la manda al ingreso.
#  g. "Desactivar" a ana.ruiz: su fila ya no tiene "Nueva contraseña temporal".

# 5. La auditoría quedó escrita, sin contraseñas
docker compose exec db psql -U postgres -d hospital_turnos \
  -c "SELECT cuando, quien, rol, accion, entidad_id, detalle FROM registros_auditoria WHERE accion = 'usuario-personal.contrasena-restablecida' ORDER BY id DESC LIMIT 5;"
```

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

### Change Log

- 2026-10-17: Story escrita (Mary, skill `tycon-create-story`) contra el código de `main` (`a8432c3`),
  con el resumen común del sprint 3. 17 AC, 16 verificaciones, 5 decisiones abiertas para el docente.
- 2026-10-17: Correcciones de `revision-sprint-3.md`. Decisiones 14 a 18 tomadas por el docente con la
  opción recomendada (marcas reemplazadas y sección "Decisiones tomadas"); la nota sobre la carrera en
  las otras acciones de A4 pasa a la story 2.5 (decisión 26); AC 16 nombra la 2.4 y la 2.5 del sprint
  4. Sin cambios en AC ni pruebas: 17 AC, 16 verificaciones.
