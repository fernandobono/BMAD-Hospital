---
epic: 2
story: "2.2"
title: "Gestión de usuarios del personal: alta con contraseña temporal, desactivar, reactivar y desbloquear"
status: review
sprint: 2
priority: "P0"
approach: "[ADAPT]"
depends_on: ["2.1", "1.2", "1.3"]
files_to_create:
  - "src/HospitalTurnos.Dominio/Personal/ReglasUsuarioPersonal.cs"
  - "src/HospitalTurnos.Aplicacion/Personal/IGestionUsuariosPersonal.cs"
  - "src/HospitalTurnos.Aplicacion/Personal/ICambioContrasenaPersonal.cs"
  - "src/HospitalTurnos.Aplicacion/Personal/IActorVigente.cs"
  - "src/HospitalTurnos.Infraestructura/Identidad/GestionUsuariosPersonalIdentity.cs"
  - "src/HospitalTurnos.Infraestructura/Identidad/CambioContrasenaPersonalIdentity.cs"
  - "src/HospitalTurnos.Infraestructura/Identidad/ActorVigenteIdentity.cs"
  - "src/HospitalTurnos.Infraestructura/Identidad/ContrasenaTemporal.cs"
  - "src/HospitalTurnos.Infraestructura/Identidad/FabricaPrincipalPersonal.cs"
  - "src/HospitalTurnos.Infraestructura/Identidad/DescriptorErroresIdentity.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/<marca>_GestionUsuariosPersonal.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/<marca>_GestionUsuariosPersonal.Designer.cs"
  - "src/HospitalTurnos.Web/Personal/ActorDeSesion.cs"
  - "src/HospitalTurnos.Web/Personal/CambioDeContrasenaObligatorio.cs"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/UsuariosPersonal.razor"
  - "src/HospitalTurnos.Web/Components/Personal/CambiarContrasena.razor"
  - "tests/HospitalTurnos.Aplicacion.Tests/Personal/GestionUsuariosPersonalIdentityTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Personal/CambioContrasenaPersonalIdentityTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Personal/ContrasenaTemporalTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Personal/Dobles/ValidadorUsuarioQueAceptaTodo.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Personal/Dobles/AuditoriaQueFalla.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Ayudas/UsuariosDePrueba.cs"
  - "tests/HospitalTurnos.Web.Tests/Personal/UsuariosPersonalHttpTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Personal/CambioContrasenaHttpTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Personal/Dobles/GestionUsuariosConBaseCaida.cs"
files_to_modify:
  - "src/HospitalTurnos.Infraestructura/Identidad/UsuarioPersonal.cs"
  - "src/HospitalTurnos.Infraestructura/Identidad/IngresoPersonalIdentity.cs"
  - "src/HospitalTurnos.Infraestructura/Identidad/RegistroIdentidad.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/HospitalDbContextModelSnapshot.cs"
  - "src/HospitalTurnos.Dominio/Auditoria/AccionesAuditoria.cs"
  - "src/HospitalTurnos.Aplicacion/Personal/IIngresoPersonal.cs"
  - "src/HospitalTurnos.Web/Program.cs"
  - "src/HospitalTurnos.Web/Personal/RutasPersonal.cs"
  - "src/HospitalTurnos.Web/Errores/ManejadorGlobalDeErrores.cs"
  - "src/HospitalTurnos.Web/Components/Personal/Ingreso.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Secciones/SeccionesAdministracion.razor"
  - "tests/HospitalTurnos.Web.Tests/Personal/FormularioHttp.cs"
  - "tests/HospitalTurnos.Web.Tests/Personal/SesionYPermisosHttpTests.cs"
---

# Story 2.2: Gestión de usuarios del personal: alta con contraseña temporal, desactivar, reactivar y desbloquear

Status: review

## Story

Como administradora del Hospital Las Acacias (Lucía),
quiero dar de alta a cada integrante del personal con su rol y una contraseña temporal, y poder
desactivarlo, reactivarlo o desbloquearlo,
para que cada persona entre con su propio usuario, que quien se va deje de entrar en el momento, y
que nadie dependa de tocar la base de datos para administrar accesos.

## Acceptance Criteria

> Pantalla A4 (UX §7 "A4 · Usuarios del personal"), ruta `/personal/admin/usuarios`, sólo rol
> `Administrador` (la protege la story 2.1, AC 13). Reemplaza la sección "en construcción".
> Renderizado estático en el servidor (SSR, sin circuito interactivo), como S0 de la 2.1.
> Mensajes en castellano rioplatense. "Ahora" sale siempre del `TimeProvider` inyectado.
>
> **Campos de auditoría de esta story** (cumplen los obligatorios de la 1.3, que lanza
> `ArgumentException` si alguno viene vacío):
>
> | Campo | Valor |
> |---|---|
> | `UsuarioId` | id de quien actúa, leído de la base: el administrador (en el cambio de contraseña, el propio usuario) |
> | `Quien` | su `NombreCompleto`, leído de la base |
> | `Rol` | su único rol (`Administrador` en A4; en el cambio de contraseña, el rol del usuario) |
> | `Canal` | `CanalAuditoria.Personal` |
> | `EntidadTipo` | `"UsuarioPersonal"` |
> | `EntidadId` | id del usuario afectado (`Guid` como texto) |
> | `Detalle` | ver cada AC. **Nunca** una contraseña, ni temporal ni nueva |

### Pantalla A4 — listado (FR-10)

1. **Given** un administrador ingresado **When** pide `GET /personal/admin/usuarios` **Then** recibe
   `200` con `<h1>Usuarios del personal</h1>`, **sin** el texto
   `Esta sección se habilita en una próxima versión.`, con el encabezado `Cache-Control: no-store`, y
   una tabla con las columnas `Usuario`, `Nombre`, `Rol`, `Estado` y `Acciones`. Hay una fila por
   cada usuario del personal, ordenadas por `NombreCompleto` y después por nombre de usuario:
   - `Rol`: `Recepcionista`, `Médico` o `Administrador`; `(sin rol)` o `(varios roles)` si los datos
     vienen de antes de esta story (mismos textos que la auditoría de la 2.1).
   - `Estado`: `Activo` o `Desactivado`; además `Bloqueado hasta las HH:mm` (hora del hospital) si su
     `LockoutEnd` es posterior a ahora, y `Debe cambiar la contraseña` si tiene la marca del AC 3.
   - `Acciones` (botones dentro de un único formulario con token): `Desactivar` si está activo
     (**salvo en la fila del propio administrador**, que en lugar del botón dice `(sos vos)`),
     `Reactivar` si está desactivado, `Desbloquear` si está bloqueado ahora.
2. **Given** un usuario con rol `Recepcionista` o `Medico` **When** pide la pantalla **Then** recibe
   el `403` legible de la 2.1 (AC 13). **When** envía a mano un POST del formulario de alta (con un
   token válido de su propia sesión) **Then** recibe `302` a `/personal/acceso-denegado` y no se crea
   nada.

### Alta con contraseña temporal (FR-10, S-30)

3. **Given** un administrador ingresado **When** envía el formulario de alta con `Nombre completo`,
   `Usuario` y un `Rol` válidos **Then**:
   - se crea un `UsuarioPersonal` con `UserName` = lo escrito sin espacios en los extremos y en
     minúsculas, `NombreCompleto` sin espacios en los extremos, **exactamente un** rol, `Activo = true`
     y `DebeCambiarContrasena = true`;
   - su contraseña es una contraseña temporal generada por el servidor (AC 4), guardada sólo como hash;
   - la respuesta es `200`, con `Cache-Control: no-store`, el formulario vacío, la fila nueva en la
     tabla, y un elemento `role="status"` con
     `Creaste el usuario <usuario> (<rol>). Contraseña temporal: <contraseña>. Anotala y dásela a la persona: no se vuelve a mostrar. La tiene que cambiar en su primer ingreso.`
     La contraseña va dentro de `<code id="contrasena-temporal">`;
   - se guarda **un** registro `AccionesAuditoria.UsuarioPersonalCreado` con los campos de la tabla y
     `Detalle = {"usuario":"<usuario>","nombreCompleto":"<nombre>","rol":"<rol>"}`.
4. La contraseña temporal tiene 12 caracteres, la genera `RandomNumberGenerator` (generador
   criptográfico) y **cumple la política real** de Identity del proyecto: al menos 10 caracteres, una
   mayúscula, una minúscula, un número y un símbolo (ver "Que existe hoy": la 2.1 dejó
   `RequireNonAlphanumeric` en `true`). No usa caracteres que se confunden al dictarla (`0 O o 1 l I`).
   **Nunca** se guarda en texto, ni en el `ILogger`, ni en `registros_auditoria`, ni aparece en otra
   respuesta: un `GET` posterior de A4 no la trae.
5. **Given** el formulario de alta con datos inválidos **When** se envía **Then** la respuesta es `200`
   con el mensaje debajo de cada campo, no se crea ningún usuario y no se audita. La validación corre
   también en el servidor, en la página **y** en el caso de uso (NFR-10: un POST armado a mano da el
   mismo resultado):
   - nombre vacío → `Ingresá el nombre completo.`; más de 120 caracteres →
     `El nombre completo tiene como máximo 120 caracteres.`
   - usuario vacío → `Ingresá el nombre de usuario.`; fuera de `^[A-Za-z0-9._-]{3,100}$` →
     `El usuario tiene entre 3 y 100 caracteres: letras sin acento, números, punto, guion o guion bajo.`
   - rol vacío o que no está en `Roles.Todos` (por ejemplo `Superusuario` en un POST armado) →
     `Elegí un rol.`
   Debajo del campo `Usuario` hay siempre una ayuda: `Por ejemplo: ana.ruiz. Letras sin acento,
   números, punto, guion o guion bajo.` (marca de legibilidad de la pantalla, sesión 09).
6. **Given** que ya existe un usuario con ese nombre, **sin importar mayúsculas** (`Recepcion1` choca con
   `recepcion1`) **When** se envía el alta **Then** la respuesta es `409`, con
   `Ya existe un usuario con el nombre <usuario>. Elegí otro.` en un elemento `role="alert"`, los
   campos conservan lo escrito, y no se crea ni se audita nada. Lo mismo si dos altas con el mismo
   nombre llegan a la vez: el índice único `user_name_index` de la base rechaza la segunda (error
   `23505`) y se traduce al mismo `ConflictoException`.
7. El alta es atómica: crear el usuario, asignarle el rol y guardar la auditoría van en **una**
   transacción. **Given** que falla cualquiera de los tres pasos **When** se envía el alta **Then** no
   queda ningún usuario creado y el error sigue su camino (AC 18).

### Desactivar y reactivar (FR-10)

8. **Given** un usuario activo que no es el administrador que actúa **When** el administrador pulsa
   `Desactivar` **Then** queda `Activo = false`, su sello de seguridad (`SecurityStamp`) cambia
   (`UserManager.UpdateSecurityStampAsync`, contrato 2.1 AC 22 (b)), se audita
   `UsuarioPersonalDesactivado` con `Detalle = {"usuario":"<usuario>"}`, y la respuesta es `200` con
   `role="status"`
   `Desactivaste a <usuario>. Si tenía una sesión abierta, se corta en 5 minutos como máximo.`
   Su sesión abierta se corta en la siguiente revalidación (2.1 AC 25): a los 6 minutos, su próximo
   pedido recibe `302` al ingreso.
9. **Given** un administrador **When** intenta desactivarse a sí mismo (con un POST armado: la pantalla
   no le muestra el botón, AC 1) **Then** recibe `400` con
   `No podés desactivar tu propio usuario. Pedíselo a otro administrador.` en `role="alert"` y nada
   cambia.
10. **Siempre queda al menos un administrador activo.** Se garantiza así: (a) sólo un administrador
    **activo en la base** puede desactivar (AC 15); (b) nadie se desactiva a sí mismo (AC 9); (c) la
    desactivación toma un candado de la base (`pg_advisory_xact_lock`) y revisa (a) **después** de
    tomarlo. **Given** dos administradores, A y B, que se desactivan uno al otro **al mismo tiempo**
    **When** llegan los dos pedidos **Then** exactamente uno se cumple, el otro recibe
    `AccesoDenegadoException` (su actor ya no está activo) y uno de los dos sigue activo.
11. **Given** un usuario desactivado **When** el administrador pulsa `Reactivar` **Then** queda
    `Activo = true` (sin tocar su contraseña, su bloqueo ni su marca de cambio de contraseña), se
    audita `UsuarioPersonalReactivado` con `Detalle = {"usuario":"<usuario>"}`, la respuesta es `200`
    con `Reactivaste a <usuario>. Ya puede ingresar con su contraseña.`, y puede ingresar con la
    contraseña que tenía.
12. **Given** un usuario que ya está en el estado pedido (desactivar a uno desactivado, reactivar a uno
    activo: pasa con dos pestañas abiertas o dos administradores) **When** se envía la acción
    **Then** la respuesta es `409` con `Ese usuario ya estaba desactivado.` o
    `Ese usuario ya estaba activo.`, y no se audita nada.

### Desbloquear (contrato 2.1 AC 22 (c))

13. **Given** un usuario bloqueado por intentos (`LockoutEnd` posterior a ahora) **When** el
    administrador pulsa `Desbloquear` **Then** queda `LockoutEnd = null` y `AccessFailedCount = 0`, se
    audita `UsuarioPersonalDesbloqueado` con `Detalle = {"usuario":"<usuario>"}`, la respuesta es `200`
    con `Desbloqueaste a <usuario>. Ya puede volver a intentar el ingreso.`, y puede ingresar en el acto
    con su contraseña correcta.
14. **Given** un usuario que no está bloqueado ahora (nunca lo estuvo, o su `LockoutEnd` ya pasó)
    **When** se envía `Desbloquear` **Then** la respuesta es `409` con `Ese usuario no está bloqueado.`
    y no se audita nada.

### Errores comunes de las acciones de A4

15. **Given** que quien actúa ya **no** es un administrador activo en la base (lo desactivaron hace
    menos de 5 minutos y su sesión todavía no se revalidó, o su id no existe) **When** envía cualquier
    acción de A4 (alta, desactivar, reactivar, desbloquear) **Then** recibe `403` con
    `No tenés permiso para administrar usuarios.` en `role="alert"` y nada cambia. El caso de uso lo
    revisa siempre, aunque la página ya pida el rol: la sesión puede tener hasta 5 minutos de atraso.
    El control lo hace un **servicio único**, `IActorVigente.ExigirAdministradorActivoAsync`, que esta
    story crea y que la 3.1 y la 3.2 reusan con la misma firma (decisión 16 del docente).
16. **Given** una acción sobre un id que no existe **When** se envía **Then** `404` con
    `No encontramos ese usuario. Recargá la página.` **Given** un valor de acción que la pantalla no
    ofrece (POST armado, por ejemplo `borrar:<id>`) **Then** `400` con
    `No reconocemos esa acción. Recargá la página.` En los dos casos nada cambia.
17. **Given** un POST a cualquiera de los tres formularios de esta story (alta, acciones de A4, cambio
    de contraseña) **sin** token antifalsificación **When** llega **Then** responde `400` y nada cambia.
18. **Given** que la base no responde durante una acción de A4 **When** un navegador envía el
    formulario (`Accept: text/html`) **Then** recibe `500` con la página `/Error` de la 1.2
    (`Algo falló. Probá de nuevo en unos minutos.`), sin ninguna contraseña en el cuerpo, y el detalle
    queda sólo en el `ILogger` (NFR-09). La página **no** atrapa errores inesperados: sólo los tres
    tipos de excepción del dominio (ver "Decisiones").

### Cambio de contraseña en el primer ingreso (S-30)

19. **Given** un usuario con `DebeCambiarContrasena = true` **When** ingresa en S0 con su contraseña
    temporal **Then** recibe la cookie `Personal` como cualquier ingreso (y se audita `IngresoPersonal`
    como en 2.1 AC 1), pero el `302` va a `/personal/cambiar-contrasena`; si traía un `ReturnUrl`
    válido (2.1 AC 7), a `/personal/cambiar-contrasena?ReturnUrl=<ruta codificada>`. Su sesión lleva
    el claim `ClaimsPersonal.DebeCambiarContrasena = "si"`.
20. **Given** una sesión con ese claim **When** pide cualquier ruta que empieza con `/personal/`, salvo
    `/personal/cambiar-contrasena`, `/personal/salir`, `/personal/ingreso` y
    `/personal/acceso-denegado` **Then** recibe `302` a `/personal/cambiar-contrasena`. `Salir`
    funciona igual: la pantalla de cambio tiene un botón `Salir` (formulario a `/personal/salir` con
    token), porque su layout no tiene menú. Las rutas fuera de `/personal/` (archivos estáticos, `/salud`, `/Error`) no se
    tocan.
21. **Given** la pantalla `/personal/cambiar-contrasena` (layout de ingreso, sin menú) **When** envía
    `Contraseña actual`, `Contraseña nueva` y `Repetí la contraseña nueva` válidas **Then**, en una
    transacción, el hash cambia, el sello de seguridad cambia, `DebeCambiarContrasena` queda en
    `false` y se audita `ContrasenaPersonalCambiada` (actor = el propio usuario) con
    `Detalle = {"motivo":"primer-ingreso"}`. En la misma respuesta se emite una cookie nueva (sin el
    claim y con el sello nuevo, para que la revalidación de 2.1 AC 25 no la corte) y un `302` al
    `ReturnUrl` si es válido y no es la propia pantalla, si no al inicio de su rol. Después la
    contraseña temporal ya no sirve y la nueva sí.
22. **Given** datos que no sirven **When** se envía el cambio **Then** la respuesta es `200`, con los
    mensajes en un `role="alert"`, uno por renglón; si alguno es de la política de la nueva (AC 24),
    van debajo del título `La contraseña nueva no sirve:`. La contraseña no cambia, la marca sigue, no se audita y la sesión
    sigue obligada a cambiar (AC 20):
    - contraseña actual incorrecta → `La contraseña actual no es correcta.` (no suma intentos de
      bloqueo: ver "Decisiones");
    - la nueva y la repetida distintas → `Las contraseñas nuevas no coinciden.`;
    - la nueva igual a la actual → `La nueva contraseña tiene que ser distinta de la actual.`;
    - la nueva fuera de política → un mensaje por regla incumplida (AC 24).
    Campos vacíos → `Ingresá tu contraseña actual.`, `Ingresá la contraseña nueva.`,
    `Repetí la contraseña nueva.`, también en el servidor.
23. **Given** un usuario **sin** la marca, ingresado **When** abre `/personal/cambiar-contrasena` por su
    dirección y cambia su contraseña **Then** funciona igual que el AC 21, con
    `Detalle = {"motivo":"voluntario"}`. La pantalla no se agrega al menú (rompería la prueba del menú
    de la 2.1, T21): se llega por la dirección.

### Mensajes de Identity en castellano

24. Identity usa `DescriptorErroresIdentity` (registrado con `AddErrorDescriber`). Los textos:

    | Error de Identity | Texto |
    |---|---|
    | `PasswordTooShort(n)` | `La contraseña tiene que tener al menos {n} caracteres.` |
    | `PasswordRequiresUpper` | `La contraseña tiene que tener al menos una mayúscula.` |
    | `PasswordRequiresLower` | `La contraseña tiene que tener al menos una minúscula.` |
    | `PasswordRequiresDigit` | `La contraseña tiene que tener al menos un número.` |
    | `PasswordRequiresNonAlphanumeric` | `La contraseña tiene que tener al menos un símbolo, como - o #.` |
    | `PasswordMismatch` | `La contraseña actual no es correcta.` |
    | `DuplicateUserName(u)` | `Ya existe un usuario con el nombre {u}. Elegí otro.` |
    | `InvalidUserName(u)` | `El usuario {u} tiene caracteres no permitidos.` |

    La pantalla de cambio muestra debajo de los campos la ayuda
    `Mínimo 10 caracteres, con mayúscula, minúscula, número y símbolo.`

### Contrato con otras stories

25. Esta story **escribe** con `IAuditoria.Registrar(...)` (1.3) y la 9.1 **lee**. `AccionesAuditoria`
    suma: `UsuarioPersonalCreado = "usuario-personal.creado"`,
    `UsuarioPersonalDesactivado = "usuario-personal.desactivado"`,
    `UsuarioPersonalReactivado = "usuario-personal.reactivado"`,
    `UsuarioPersonalDesbloqueado = "usuario-personal.desbloqueado"` y
    `ContrasenaPersonalCambiada = "usuario-personal.contrasena-cambiada"`, con los campos de la tabla del inicio.
    Estilo de las acciones (contrato técnico §Auditoría, decisión del docente del 2026-10-16 sobre
    M-7): `<entidad>.<acción>` en kebab-case, con la entidad antes del punto (`usuario-personal.…`,
    `especialidad.…`, `medico.…`), como las 9 de la 1.3. Las tres de la 2.1 quedan como están.
26. Contratos que esta story cumple o deja escritos:
    - **2.1 (contrato del AC 22)**: (a) un rol por usuario: el alta asigna exactamente uno y no hay
      forma de agregar otro; (b) desactivar llama a `UpdateSecurityStampAsync` y el cambio de
      contraseña cambia el sello (lo hace `ChangePasswordAsync` de Identity), así la revalidación del
      AC 25 corta las sesiones; (c) el desbloqueo es de A4.
    - **3.2 (Médicos)**: esta story **no** vincula usuarios con médicos. 3.2 crea el vínculo con un
      `UsuarioPersonal` de rol `Roles.Medico` por su `Id` (`Guid`). Esta story **nunca borra**
      usuarios (sólo los desactiva), así un vínculo no queda apuntando a nada. Desactivar a un usuario
      médico no está bloqueado acá: qué pasa con su médico lo decide 3.2. La columna
      `Médico vinculado` de A4 (UX §7) la agrega **3.2**, porque la entidad `Medico` todavía no existe.
    - **3.1, 3.2 y siguientes (piezas compartidas: esta story es su única dueña)**. Las demás las
      **reusan con la misma firma**, no las vuelven a crear ni las copian:
      - `ActorDeSesion.Desde(ClaimsPrincipal)` (Web/Personal) arma el `ActorAuditoria` desde la sesión.
      - `ManejadorGlobalDeErrores.EstadoPara(Exception)` y `EsEsperable(Exception)` traducen una
        excepción del dominio al mismo código que usa el manejador global.
        `ManejadorGlobalDeErrores.FijarEstadoEnPagina(HttpContext, Exception)` fija ese código en la
        respuesta de una página SSR (el 404, al empezar a enviarla: ver "Implementación concreta").
        Toda página SSR del administrador muestra el cartel de regla con
        `catch (Exception e) when (ManejadorGlobalDeErrores.EsEsperable(e))` +
        `FijarEstadoEnPagina(HttpContext, e)`.
      - `IActorVigente.ExigirAdministradorActivoAsync(actor, mensajeSiNoLoEs, ct)` (Aplicacion/Personal,
        implementado en Infraestructura/Identidad) confirma **en la base** que el actor es un
        administrador activo y devuelve el actor con su nombre leído de la base (decisión 16).
      - Carpeta de las páginas del administrador: `Components/Personal/Administracion/` (I-2 de la
        revisión). A2 (3.1) y A1 (3.2) van ahí.
      - Pruebas: `FormularioHttp.EnviarAsync(...)` y `FormularioHttp.Estado(html)` arman el POST de un
        formulario SSR con nombre y leen el `role="status"`; la lista `RutasYaConstruidas` de
        `SesionYPermisosHttpTests` (2.1 T19) es donde cada story anota la sección que deja de estar "en
        construcción"; `UsuariosDePrueba` (Aplicacion.Tests/Ayudas) crea un usuario propio con sus
        roles y arma su `ActorAuditoria`.

### No-regresión

27. Siguen en verde las pruebas de las stories 1.1 a 2.1. Única prueba de otra story que se toca: la
    T19 de la 2.1 (`Secciones_SegunRol_PermiteLasSuyasYDevuelve403LegibleEnLasAjenas`), que deja de
    exigir el texto "en construcción" para las rutas de `RutasYaConstruidas` (hoy sólo
    `/personal/admin/usuarios`); el resto de su verificación (título, `403` legible) no cambia. Los
    usuarios que no se crean por A4 (el administrador inicial del sembrador, los fijos de las pruebas)
    quedan con `DebeCambiarContrasena = false` (la migración pone `false` en las filas existentes y la
    clase lo trae por defecto), así que la 2.1 T10 sigue dando `302` directo al inicio del rol. La
    protección de `registros_auditoria` (1.3) no se toca.

### Tests (31 verificaciones)

| # | Test | AC | Nivel | Escenario | Resultado esperado |
|---|------|----|-------|-----------|--------------------|
| T1 | `CrearAsync_DatosValidos_CreaUsuarioActivoConContrasenaTemporalYAudita` | 3,4,25 | integration | Actor = admin propio del test; alta `  Ana Ruiz-<m>  ` / `  Ana.Ruiz-<m>  ` / `Medico` (`<m>` = 8 hex de un Guid) | Devuelve id y contraseña; en la base: `user_name` = `ana.ruiz-<m>`, nombre sin espacios, `activo`, `debe_cambiar_contrasena`, 1 rol `Medico`; `VerifyHashedPassword(hash, temporal)` = `Success` y el hash ≠ la temporal; 1 fila `usuario-personal.creado` con `entidad_id` = id nuevo, `usuario_id`/`quien`/`rol` del admin, canal `Personal`, detalle con usuario, nombre y rol, y **sin** la temporal; ningún mensaje del `ILogger` contiene la temporal |
| T2 | `CrearAsync_UsuarioRepetido_LanzaConflictoSinCrearNiAuditar` | 6 | integration | `[Theory]`: usuario propio existente `p-<m>`; alta con (a) `p-<m>`; (b) `P-<M>` | `ConflictoException` con `Ya existe un usuario con el nombre …`; sigue habiendo 1 usuario con ese nombre normalizado; 0 filas `usuario-personal.creado` con detalle que contenga `<m>` |
| T3 | `CrearAsync_DatosInvalidos_LanzaReglaDeNegocioSinCrear` | 5 | integration | `[Theory]` con usuario `p-<m>` salvo donde se indica: (a) nombre `""`; (b) nombre de 121 caracteres; (c) usuario `a b`; (d) usuario `ab`; (e) rol `Superusuario`; (f) rol `""` | `ReglaDeNegocioException` con el texto del AC 5 que corresponde; 0 usuarios con ese nombre |
| T4 | `CrearAsync_ChoqueConElIndiceUnico_LanzaConflicto` | 6 | integration | Usuario propio `p-<m>`; proveedor con `IUserValidator<UsuarioPersonal>` reemplazado por `ValidadorUsuarioQueAceptaTodo` (Identity no revisa duplicados, como dos altas simultáneas); alta con `p-<m>` | `ConflictoException` (traducida del `23505` de `user_name_index`); sigue habiendo 1 usuario con ese nombre |
| T5 | `CrearAsync_FallaLaAuditoria_NoQuedaElUsuario` | 7 | integration | `IAuditoria` reemplazada por `AuditoriaQueFalla` (lanza en `Registrar`); alta válida `p-<m>` | La excepción sale del caso de uso; `FindByNameAsync("p-<m>")` = `null` (la transacción se deshizo) |
| T6 | `ContrasenaTemporal_Generar_CumplePoliticaSinConfundiblesYNoSeRepite` | 4 | unit | 500 llamadas a `ContrasenaTemporal.Generar()`; cada una se pasa por `PasswordValidator<UsuarioPersonal>` con las `IdentityOptions` del proyecto (`AddIdentidadPersonal` sin base) | Todas de largo 12, todas `Succeeded`, ninguna contiene `0 O o 1 l I`, las 500 distintas |
| T7 | `DesactivarAsync_UsuarioActivo_LoDesactivaCambiaSelloYAudita` | 8,25,26 | integration | Usuario propio Recepcionista; se guarda su sello; admin propio desactiva | `activo = false`; sello distinto; `SesionVigenteAsync(id, selloViejo)` = `false`; 1 fila `usuario-personal.desactivado` con `entidad_id` = su id y detalle `{"usuario":"p-<m>"}` |
| T8 | `DesactivarAsync_ASiMismo_LanzaReglaSinCambios` | 9 | integration | Admin propio se desactiva a sí mismo | `ReglaDeNegocioException` (no `ConflictoException`) con el texto del AC 9; sigue activo, mismo sello; 0 filas con su id como `entidad_id` y acción `usuario-personal.desactivado` |
| T9 | `DesactivarAsync_DosAdministradoresALaVez_QuedaUnoActivo` | 10 | integration | Admins propios A y B; `Task.WhenAll` de "A desactiva a B" y "B desactiva a A", cada uno en su alcance (contexto y conexión propios) | Exactamente una tarea termina bien y la otra lanza `AccesoDenegadoException`; exactamente uno de A y B sigue `activo` |
| T10 | `ReactivarAsync_UsuarioDesactivado_VuelveAIngresarConSuContrasena` | 11,25 | integration | Usuario propio creado con `Activo = false`; admin reactiva; después `IIngresoPersonal.ValidarAsync` con su contraseña | `activo = true`; `debe_cambiar_contrasena` y `access_failed_count` sin cambio; 1 fila `usuario-personal.reactivado`; ingreso `Exitoso` |
| T11 | `DesactivarOReactivar_EstadoQueYaTenia_LanzaConflictoSinAuditar` | 12 | integration | `[Theory]`: (a) desactivar a uno ya desactivado; (b) reactivar a uno activo | `ConflictoException` con `Ese usuario ya estaba desactivado.` / `Ese usuario ya estaba activo.`; 0 filas de esas acciones con su id |
| T12 | `DesbloquearAsync_UsuarioBloqueado_LoDesbloqueaYAudita` | 13,25 | integration | Usuario propio con 5 `AccessFailedAsync` (queda bloqueado 15 min); admin desbloquea; después `ValidarAsync` con la contraseña correcta | `lockout_end` nulo, `access_failed_count = 0`; 1 fila `usuario-personal.desbloqueado`; ingreso `Exitoso` |
| T13 | `DesbloquearAsync_UsuarioNoBloqueado_LanzaConflicto` | 14 | integration | `[Theory]`: (a) nunca bloqueado; (b) `LockoutEnd` = reloj − 1 minuto | `ConflictoException` con `Ese usuario no está bloqueado.`; 0 filas `usuario-personal.desbloqueado` con su id |
| T14 | `Operaciones_ActorQueNoEsAdministradorActivo_LanzaAccesoDenegado` | 15 | integration | `[Theory]` 4 operaciones (crear, desactivar, reactivar, desbloquear) × 3 actores: (a) usuario propio Recepcionista; (b) admin propio con `Activo = false` en la base; (c) `UsuarioId` = Guid nuevo | `AccesoDenegadoException` con `No tenés permiso para administrar usuarios.` en los 12 casos; el usuario objetivo sin cambios; 0 filas de auditoría con su id |
| T15 | `Operaciones_UsuarioInexistente_LanzaNoEncontrado` | 16 | integration | `[Theory]` desactivar / reactivar / desbloquear un Guid nuevo, actor admin propio | `NoEncontradoException` con `No encontramos ese usuario. Recargá la página.` |
| T16 | `ListarAsync_UsuariosEnVariosEstados_DevuelveRolEstadoYBloqueo` | 1 | integration | Usuarios propios con nombres `Zz-<m>-1` (Medico, activo), `Zz-<m>-2` (Recepcionista, desactivado), `Zz-<m>-3` (Administrador, bloqueado, con marca), `Zz-<m>-4` (sin rol) | En la lista, en ese orden relativo; `Rol` = `Medico` / `Recepcionista` / `Administrador` / `(sin rol)`; `Activo` correcto; `BloqueadoHasta` no nulo sólo en el 3.º; `DebeCambiarContrasena` sólo en el 3.º |
| T17 | `CambiarAsync_ContrasenaTemporal_CambiaHashSelloYBanderaYAudita` | 19,21,25 | integration | Alta por `CrearAsync` (rol Recepcionista); `ValidarAsync` con la temporal; `CambiarAsync(id, temporal, "Nueva-Clave-2026")` | El ingreso con la temporal es `Exitoso` con `DebeCambiarContrasena = true` y el claim `debe_cambiar_contrasena = si`; el cambio devuelve éxito, rol `Recepcionista` y un principal **sin** ese claim y con el sello nuevo; en la base la marca es `false` y el sello cambió; `ValidarAsync` con la temporal → `CredencialesInvalidas`, con la nueva → `Exitoso` sin marca; 1 fila `usuario-personal.contrasena-cambiada` con `usuario_id` = `entidad_id` = su id y detalle `{"motivo":"primer-ingreso"}` |
| T18 | `CambiarAsync_DatosInvalidos_DevuelveMensajesEnCastellanoSinCambiar` | 22,24 | integration | `[Theory]` con usuario propio con marca: (a) actual `Mala-Clave-99`; (b) nueva igual a la actual; (c) nueva `abc` | No exitoso, con: (a) `La contraseña actual no es correcta.`; (b) `La nueva contraseña tiene que ser distinta de la actual.`; (c) los 4 textos de largo, mayúscula, número y símbolo del AC 24 (minúscula no, `abc` la tiene); hash, sello y marca sin cambio; `access_failed_count = 0`; 0 filas `usuario-personal.contrasena-cambiada` con su id |
| T19 | `CambiarAsync_UsuarioSinBandera_CambiaYAuditaComoVoluntario` | 23 | integration | Usuario propio sin marca; cambio válido | Éxito; 1 fila `usuario-personal.contrasena-cambiada` con detalle `{"motivo":"voluntario"}` |
| T20 | `GetUsuarios_Administrador_MuestraTablaConAccionesSegunEstado` | 1,27 | integration | `admin1` ingresa; usuarios propios: activo, desactivado, bloqueado (5 fallos); GET A4 | `200`; `<h1>Usuarios del personal</h1>`; no contiene el texto de construcción; `Cache-Control` con `no-store`; la fila de cada uno trae su estado y **sólo** su botón (`value="desactivar:<id>"`, `reactivar:<id>`, `desbloquear:<id>`); la fila de `admin1` **no** tiene `desactivar:<id de admin1>` y dice `(sos vos)` |
| T21 | `Usuarios_OtrosRoles_NoVenNiModifican` | 2 | integration | `[Theory]` `recepcion1`, `medico1`: GET A4 siguiendo redirecciones; después POST del alta (`_handler=alta-usuario`) con el token tomado de su propia página de inicio | GET: `403` y `No tenés permiso para entrar a esta sección.`; POST: `302` con destino que empieza con `/personal/acceso-denegado`; el usuario del alta no existe |
| T22 | `PostAlta_DatosValidos_MuestraLaContrasenaTemporalUnaSolaVez` | 3,4 | integration | `admin1`; alta `Persona <m>` / `p-<m>` / `Recepcionista`; después GET A4; después ingreso de `p-<m>` con la temporal en otro cliente | POST: `200`, `no-store`, `role="status"` con el texto del AC 3, `<code id="contrasena-temporal">` con 12 caracteres, fila nueva, campos de alta vacíos; ninguna entrada de `ProveedorDeRegistroEnMemoria` ni el `detalle` de su fila de auditoría contiene la temporal; GET: no contiene la temporal ni `contrasena-temporal`; ingreso: `302` a `/personal/cambiar-contrasena` |
| T23 | `PostAlta_DatosInvalidosOArmados_MuestraMensajesSinCrear` | 5 | integration | `[Theory]`: (a) los tres campos vacíos; (b) usuario `con espacio`; (c) rol `Superusuario` (POST armado) | `200`; (a) los 3 mensajes; (b) el mensaje de usuario; (c) `Elegí un rol.`; en los tres, la ayuda `Por ejemplo: ana.ruiz.` debajo de `Usuario`; 0 usuarios nuevos con ese nombre |
| T24 | `PostAlta_UsuarioRepetido_Devuelve409ConCartel` | 6 | integration | `admin1`; alta con usuario `Recepcion1` | `409`; `role="alert"` con `Ya existe un usuario con el nombre recepcion1. Elegí otro.`; el campo usuario conserva `Recepcion1`; `recepcion1` sigue siendo uno solo |
| T25 | `PostAcciones_SegunEstado_CambianElEstadoYConfirman` | 8,11,13 | integration | `[Theory]` con usuario propio y `admin1`: (a) activo → `desactivar:<id>`; (b) desactivado → `reactivar:<id>`; (c) bloqueado → `desbloquear:<id>` | `200`; `role="status"` con el texto del AC; en la base el estado nuevo; la fila muestra el estado nuevo en la misma respuesta (la tabla se recarga después de la acción) |
| T26 | `PostAcciones_Errores_MuestranCartelConSuCodigo` | 9,12,14,15,16 | integration | `[Theory]`: (a) admin propio ingresa y envía `desactivar:<su id>`; (b) `desactivar:` a uno ya desactivado; (c) `desbloquear:` a uno no bloqueado; (d) `desactivar:<Guid nuevo>`; (e) `borrar:<id>`; (f) admin propio ingresa, se lo desactiva en la base y, **sin** adelantar el reloj, envía `reactivar:` a un desactivado | (a) `400`; (b) `409`; (c) `409`; (d) `404`; (e) `400`; (f) `403`; cada uno con su texto exacto en `role="alert"`; en la base nada cambió |
| T27 | `Desactivar_SesionAbiertaDelUsuario_SeCortaAlRevalidar` | 8,26 | integration | Usuario propio Recepcionista ingresa en el cliente B; `admin1` lo desactiva desde el cliente A; `Reloj.Advance(6 min)`; B pide `/personal/recepcion/turnos` | `302` con destino `/personal/ingreso?ReturnUrl=%2Fpersonal%2Frecepcion%2Fturnos` |
| T28 | `PrimerIngreso_ContrasenaTemporal_ObligaACambiarlaAntesDeEntrar` | 19,20,21 | integration | `[Theory]` (a) sin `ReturnUrl`; (b) con `ReturnUrl=/personal/agendas`. `admin1` da de alta `p-<m>` Recepcionista por A4. En otro cliente: ingreso con la temporal; GET `/personal/recepcion/turnos`; GET `/salud` (todavía obligado); GET y POST del cambio (`Nueva-Clave-2026` dos veces); GET de la sección; `Salir`; ingreso con la temporal; ingreso con la nueva | Ingreso: `302` a `/personal/cambiar-contrasena` (b: `…?ReturnUrl=%2Fpersonal%2Fagendas`); GET sección: `302` a `/personal/cambiar-contrasena`; GET `/salud`: `200` (las rutas fuera de `/personal/` no se tocan, AC 20); la pantalla de cambio trae el formulario `Salir` (`action="/personal/salir"`); POST: `302` a `/personal/recepcion/turnos` (b: `/personal/agendas`) con `Set-Cookie` de `.HospitalTurnos.Personal`; GET sección: `200`; `Salir` mientras tanto funciona; temporal: `200` con `Usuario o contraseña incorrectos.`; nueva: `302` directo al inicio |
| T29 | `PostCambiarContrasena_Errores_MuestraMensajesYSigueObligado` | 22 | integration | `[Theory]` con usuario dado de alta por A4 e ingresado con la temporal: (a) repetida distinta; (b) actual mala; (c) nueva `corta`; (d) los tres campos vacíos | `200`; `role="alert"` con el texto del AC 22 / AC 24 que corresponde ((c) además con el título `La contraseña nueva no sirve:`; (d): los 3 de campos vacíos); después GET `/personal/recepcion/turnos` sigue dando `302` a `/personal/cambiar-contrasena` |
| T30 | `Post_SinTokenAntiforgery_Devuelve400SinCambios` | 17 | integration | `[Theory]` POST sin `__RequestVerificationToken`: (a) alta válida `p-<m>`; (b) `desactivar:<id>` de un usuario propio activo; (c) cambio de contraseña de un usuario con marca | `400` en los tres; (a) no existe `p-<m>`; (b) sigue activo; (c) la marca sigue |
| T31 | `PostAlta_FallaLaBase_MuestraErrorGenericoSinContrasena` | 18 | integration | Fábrica con `IGestionUsuariosPersonal` reemplazado por `GestionUsuariosConBaseCaida`; POST de alta con `Accept: text/html` | `500`; el cuerpo decodificado contiene `Algo falló. Probá de nuevo en unos minutos.` y no contiene `Npgsql`, `secreto` ni `contrasena-temporal`; el `ILogger` del manejador de 1.2 tiene la excepción |

Todas llevan `[Trait("Story", "2.2")]` y un `[Trait("AC", "n")]` por AC de su columna.
T1–T19 van en `HospitalTurnos.Aplicacion.Tests` (T6 sin base); T20–T31 en `HospitalTurnos.Web.Tests`.
Además se **modifica** la T19 de la 2.1 (AC 27): conserva su `[Trait("Story", "2.1")]`.

> El estimado del plan era 8 pruebas; quedan 31 porque la story cubre alta, contraseña temporal,
> desactivar, reactivar, desbloquear, el cambio obligatorio del primer ingreso y la garantía de que
> siempre queda un administrador, y cada camino feliz lleva sus modos de falla (la retrospectiva del
> sprint 1, acción A-2, dejó el estimado como medida de tamaño, no de cantidad).

**Por qué no hay E2E (regla 6.1 del formato).** El proyecto `tests/HospitalTurnos.E2E/` todavía no
existe (contrato §Proyectos; lo crea la story 10.1). A4 y la pantalla de cambio son formularios SSR
sin JavaScript propio, como S0: lo que hace el navegador es GET y POST de formulario, y T20–T31 lo
reproducen con `HttpClient` sobre la app real (token antifalsificación y cookies incluidos). La
verificación en el navegador queda en "Como verificar", paso 4, con su resultado en el Dev Agent Record.

#### Given/Fixture

- **Base**: la misma `PostgresFixture` de cada proyecto y la colección `"Postgres"` (contrato
  §Pruebas). Esta story no las crea ni las modifica. La base es compartida y `registros_auditoria` no
  se limpia: **cada prueba crea sus propios usuarios** (`p-<m>`, `<m>` = 8 hex de un `Guid`) y filtra la
  auditoría por `entidad_id` (o por `<m>` dentro de `detalle`).
- **Aplicacion.Tests — proveedor**: igual que `Proveedor(...)` de
  `tests/HospitalTurnos.Aplicacion.Tests/Personal/IngresoPersonalIdentityTests.cs` (`AddLogging`,
  `AgregarInfraestructura`, `AddIdentidadPersonal`, reemplazos al final), más
  `AddSingleton<TimeProvider>(reloj)` con `FakeTimeProvider(DateTimeOffset.UtcNow)` y
  `SetLocalTimeZone` en Córdoba (arranca en la hora real porque Identity fija `LockoutEnd` con el reloj
  del sistema; ver Dev Notes). Un alcance (`CreateScope`) por operación, como un pedido web.
- **Actor de las pruebas de Aplicacion**: un administrador propio por prueba (usuario `p-<m>` con rol
  `Administrador`, contraseña `Clave-Correcta-1`) y
  `new ActorAuditoria(admin.Id.ToString(), admin.NombreCompleto, Roles.Administrador, CanalAuditoria.Personal)`.
  Los dos los arma **`UsuariosDePrueba`** (nuevo, `tests/HospitalTurnos.Aplicacion.Tests/Ayudas/`), que
  la 3.1 y la 3.2 reusan:
  `Task<UsuarioPersonal> CrearAsync(ServiceProvider proveedor, string[] roles, bool activo = true, string nombre = "Persona de Prueba")`
  (misma forma que el `CrearUsuarioAsync` privado de `IngresoPersonalIdentityTests`, que no se toca:
  nombre `p-<m>`, contraseña `Clave-Correcta-1`) y
  `ActorAuditoria ActorDe(UsuarioPersonal usuario, string rol)` (canal `Personal`).
  Estos administradores quedan activos en la base compartida: **ninguna** prueba cuenta
  administradores de toda la tabla.
- **T4 — `ValidadorUsuarioQueAceptaTodo : IUserValidator<UsuarioPersonal>`**: devuelve siempre
  `IdentityResult.Success`. Se registra con
  `servicios.RemoveAll<IUserValidator<UsuarioPersonal>>(); servicios.AddScoped<IUserValidator<UsuarioPersonal>, ValidadorUsuarioQueAceptaTodo>();`.
- **T5 — `AuditoriaQueFalla : IAuditoria`**: `Registrar` lanza
  `InvalidOperationException("auditoría caída en la prueba")`.
- **T9**: los dos pedidos en alcances distintos, lanzados con `Task.Run` y esperados con
  `Task.WhenAll` dentro de un `try` por tarea que guarda el resultado (éxito o tipo de excepción).
- **T12**: `FormularioHttp.FallarAsync` no existe en Aplicacion.Tests; se llama 5 veces a
  `UserManager.AccessFailedAsync` en un alcance.
- **T13 (b)**: `UserManager.SetLockoutEndDateAsync(u, reloj.GetUtcNow().AddMinutes(-1))`.
- **Web.Tests — fábrica**: `PersonalWebApplicationFactory` (2.1) sin cambios. `admin1` / `Admin-2026xx`
  actúa en los flujos donde **el objetivo** es un usuario propio: el estado de `admin1` nunca cambia.
  T26 (a) y (f) usan un administrador propio (`FormularioHttp.CrearUsuarioAsync(app, [Roles.Administrador], "Admin-Propio-26")`).
  T31 usa `app.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddScoped<IGestionUsuariosPersonal, GestionUsuariosConBaseCaida>()))`.
- **`GestionUsuariosConBaseCaida`** (T31): `ListarAsync` devuelve una lista vacía (la página tiene que
  poder dibujarse); `CrearAsync` lanza `new NpgsqlException("detalle secreto de la base")`; el resto,
  lo mismo que `CrearAsync`.
- **Cómo se arma el POST** (`FormularioHttp.EnviarAsync`, nuevo): `GET` de la ruta, toma
  `__RequestVerificationToken` del HTML y hace POST `application/x-www-form-urlencoded` a la misma ruta
  con `_handler = <nombre del formulario>` más los campos. Parámetros:
  `(HttpClient cliente, string ruta, string formulario, IDictionary<string,string> campos, bool conToken = true, string? accept = null)`.
  Nombres: formulario de alta `alta-usuario` con `Alta.NombreCompleto`, `Alta.Usuario`, `Alta.Rol`;
  acciones `acciones-usuarios` con `Accion` = `desactivar:<id>` | `reactivar:<id>` | `desbloquear:<id>`;
  cambio `cambio-contrasena` con `Input.Actual`, `Input.Nueva`, `Input.Repetida`.
- **`FormularioHttp.Estado(html)`** (nuevo): como `Alerta(html)` pero para `role="status"`. La
  contraseña temporal se lee con una expresión regular sobre `<code id="contrasena-temporal">(.*?)</code>`.
- **T27**: los dos clientes salen de la misma fábrica, así comparten el `FakeTimeProvider`
  (`app.Reloj`).
- **T31**: el POST lleva `Accept: text/html` (como un navegador); sin ese encabezado el manejador de 1.2
  responde `problem+json` y la prueba miraría la respuesta equivocada (lección de 2.1 T15).
- **Textos**: se comparan después de `WebUtility.HtmlDecode`.

## Tasks / Subtasks

- [x] [SEQ] Task 1: Salvaguarda de estado (AC: todos)
  - [x] Confirmar que existen y dicen lo que cita "Que existe hoy": `UsuarioPersonal`,
        `IngresoPersonalIdentity`, `RegistroIdentidad`, `IIngresoPersonal`, `AccionesAuditoria`,
        `ManejadorGlobalDeErrores`, `RutasPersonal`, `SeccionesAdministracion.razor` (con `@page "/personal/admin/usuarios"`),
        `PersonalWebApplicationFactory`, `FormularioHttp`, la T19 de la 2.1 con `TextoEnConstruccion`.
  - [x] Confirmar que `RegistroIdentidad.AddIdentidadPersonal` **no** cambia `RequireNonAlphanumeric`
        (queda `true`, valor por defecto de Identity) y anotarlo en el Debug Log.
  - [x] `dotnet test` en verde antes de tocar nada; anotar los totales por proyecto.
  - [x] Si algo no coincide → DETENERSE y corregir la story primero.
- [x] [SEQ] Task 2: Dominio y Aplicacion (AC: 3, 5, 19, 21–23, 25)
  - [x] `ReglasUsuarioPersonal` (Dominio/Personal): `LargoMaximoNombre = 120`,
        `PatronUsuario = "^[A-Za-z0-9._-]{3,100}$"` y los textos de error del AC 5.
  - [x] Cinco constantes en `AccionesAuditoria` (AC 25).
  - [x] `IGestionUsuariosPersonal` con sus registros; `IActorVigente` (pieza compartida, AC 26);
        `ICambioContrasenaPersonal` con `ResultadoCambioContrasena`; en `IIngresoPersonal.cs`, `ResultadoIngreso` suma
        `bool DebeCambiarContrasena = false` y `ClaimsPersonal.DebeCambiarContrasena = "debe_cambiar_contrasena"`.
- [x] [SEQ] Task 3: Infraestructura — modelo y migración (AC: 3, 27)
  - [x] `UsuarioPersonal.DebeCambiarContrasena` (`bool`, sin valor por defecto en la base).
  - [x] Migración `GestionUsuariosPersonal` con `dotnet ef migrations add` (ver Como verificar).
        Tiene que agregar sólo `debe_cambiar_contrasena boolean NOT NULL DEFAULT false` a `asp_net_users`.
  - [x] Correr la Salvaguarda de BD.
- [x] [SEQ] Task 4: Infraestructura — casos de uso (AC: 3–16, 19, 21–24)
  - [x] `DescriptorErroresIdentity` y `.AddErrorDescriber<DescriptorErroresIdentity>()` en `RegistroIdentidad`;
        registrar `IGestionUsuariosPersonal`, `ICambioContrasenaPersonal`, `IActorVigente` →
        `ActorVigenteIdentity` y `FabricaPrincipalPersonal` (scoped).
  - [x] `ContrasenaTemporal.Generar()`.
  - [x] `FabricaPrincipalPersonal`; `IngresoPersonalIdentity` la usa en el paso 6 y devuelve la marca.
  - [x] `GestionUsuariosPersonalIdentity` y `CambioContrasenaPersonalIdentity` (Implementación concreta).
  - [x] `UsuariosDePrueba`, dobles y T1–T19 en verde.
- [x] [SEQ] Task 5: Web (AC: 1–3, 5, 6, 8, 9, 11–23)
  - [x] `ManejadorGlobalDeErrores`: extraer el `switch` a `public static int EstadoPara(Exception)` y sumar
        `public static bool EsEsperable(Exception)` y `public static void FijarEstadoEnPagina(HttpContext, Exception)`;
        `TryHandleAsync` usa los dos primeros (mismo comportamiento).
  - [x] `ActorDeSesion`, `CambioDeContrasenaObligatorio` (y `app.UseCambioDeContrasenaObligatorio()` en
        `Program.cs` entre `UseAuthentication()` y `UseAuthorization()`), `RutasPersonal.CambiarContrasena`.
  - [x] Sacar `@page "/personal/admin/usuarios"` de `SeccionesAdministracion.razor`; crear
        `Administracion/UsuariosPersonal.razor` (abre la carpeta única de las páginas del
        administrador, AC 26) y `CambiarContrasena.razor`.
  - [x] `Ingreso.razor`: si `resultado.DebeCambiarContrasena`, el destino es la pantalla de cambio (AC 19).
- [x] [SEQ] Task 6: Pruebas de Web y verificación (AC: todos)
  - [x] `FormularioHttp.EnviarAsync` y `Estado`; `RutasYaConstruidas` en la T19 de la 2.1.
  - [x] `GestionUsuariosConBaseCaida` y T20–T31 en verde.
  - [x] Suite completa y `bash scripts/ci/local.sh` sin regresiones (AC 27).
  - [x] Verificación manual en el navegador (Como verificar, paso 4) y su resultado en el Dev Agent Record.

### Task Dependencies

T1 → T2 → T3 → T4 → T5 → T6 (secuencial).
Dentro de T4, `DescriptorErroresIdentity` y `ContrasenaTemporal` pueden hacerse en paralelo.

## Dev Notes

### Que existe hoy

#### En hospital-turnos (verificado 2026-10-16 contra `main`, commit `c3413cc`, con 1.1 a 2.1 en done)

- `src/HospitalTurnos.Infraestructura/Identidad/UsuarioPersonal.cs`: `UsuarioPersonal : IdentityUser<Guid>`
  con `NombreCompleto` (`""` por defecto) y `Activo` (`true` por defecto). **No** existe
  `DebeCambiarContrasena`: la crea esta story.
- `HospitalDbContext : IdentityDbContext<UsuarioPersonal, IdentityRole<Guid>, Guid>`. En
  `OnModelCreating`, `NombreCompleto` tiene `HasMaxLength(120).IsRequired()` y **a propósito** no hay
  `HasDefaultValue(true)` para `Activo` (comentario en el archivo: con un default de base, EF toma
  `false` como "sin valor"). La convención `ConvencionSnakeCase.Aplicar` va al final. Tablas de
  Identity: `asp_net_users`, `asp_net_roles`, `asp_net_user_roles`, etc.; índice único
  `user_name_index` sobre `normalized_user_name`
  (`Migraciones/20261016180914_IdentidadPersonal.cs:193-197`).
- Migraciones con marca de tiempo: `20261015225443_Inicial`, `20261016010013_AgregarRegistroAuditoria`,
  `20261016020303_AgregarBandejaDeSalida`, `20261016180914_IdentidadPersonal`, más el snapshot. Las
  aplica el arranque (`AplicadorDeMigraciones`) como `turnos_owner`.
- `RegistroIdentidad.AddIdentidadPersonal`: `AddIdentityCore<UsuarioPersonal>` con
  `RequiredLength = 10`, mayúscula, minúscula, dígito, bloqueo 5 intentos / 15 min,
  `RequireUniqueEmail = false`, `.AddRoles<IdentityRole<Guid>>()`, `.AddEntityFrameworkStores<HospitalDbContext>()`;
  registra `IIngresoPersonal` → `IngresoPersonalIdentity` y `SembradorPersonal`. **Hallazgo:** no
  toca `RequireNonAlphanumeric`, que en Identity 10 vale `true` por defecto (verificado con
  `ilspycmd` sobre `Microsoft.Extensions.Identity.Core.dll` 10.0.11, `PasswordOptions`, línea 9). O sea:
  la política real **también exige un símbolo**, aunque el AC 21 de la 2.1 no lo nombra. Por eso las
  contraseñas fijas de las pruebas tienen guion (`Recepcion-2026`). Esta story no cambia la política:
  la documenta (AC 4, AC 24) y genera las temporales con símbolo.
- `IngresoPersonalIdentity` (2.1): pasos 1–7 de `ValidarAsync`; el paso 6 arma el principal con
  `IUserClaimsPrincipalFactory<UsuarioPersonal>` y le suma `ClaimsPersonal.NombreCompleto`;
  `AuditarAsync` registra y guarda; `Asegurar(IdentityResult)` lanza `InvalidOperationException` si
  Identity falla. `SesionVigenteAsync` compara existencia, `Activo`, sello y un solo rol.
- `IIngresoPersonal.cs` (Aplicacion/Personal): `ResultadoIngreso(EstadoIngreso Estado, ClaimsPrincipal? Principal = null, string? Rol = null)`
  con fábricas estáticas; `ClaimsPersonal.NombreCompleto = "nombre_completo"`.
- `SembradorPersonal` crea el administrador inicial con `UserManager.CreateAsync` (queda con
  `DebeCambiarContrasena = false` por el valor por defecto de la clase: AC 27).
- Auditoría: `IAuditoria.Registrar(ActorAuditoria actor, string accion, string entidadTipo, string entidadId, object? detalle = null)`
  **sólo agrega** (`AuditoriaEnBase.cs:14`); el detalle se serializa con `JsonSerializerOptions.Web`
  (nombres en camelCase). `ActorAuditoria(string? UsuarioId, string Quien, string Rol, CanalAuditoria Canal)`.
  `CanalAuditoria.Personal` existe.
- `AccionesAuditoria` (Dominio/Auditoria): hoy tiene 9 constantes de turnos y agenda con punto
  (`turno.reservado`) y las 3 de la 2.1 en kebab-case sin punto (`ingreso-personal`). Esta story usa
  `<entidad>.<acción>` con punto, como las 9 de la 1.3 (AC 25, decisión del docente sobre M-7).
- Excepciones del dominio (Dominio/Excepciones): `ReglaDeNegocioException` (→400),
  `ConflictoException : ReglaDeNegocioException` (→409), `NoEncontradoException` (→404),
  `AccesoDenegadoException` (→403). `ManejadorGlobalDeErrores.TryHandleAsync` las traduce con un
  `switch` y, **si el pedido acepta `text/html`, devuelve `false`** y el middleware muestra `/Error`,
  que siempre dice `Algo falló. Probá de nuevo en unos minutos.` (`Components/Pages/Error.razor`).
  Consecuencia: en una pantalla SSR, una regla de negocio que se deja escapar se ve como un error
  genérico y se pierde su texto. Ver "Decisiones".
- Web: `RutasPersonal.Usuarios = "/personal/admin/usuarios"` (título `Usuarios del personal`);
  `SeccionesAdministracion.razor` tiene 6 `@page` (medicos, propuestas, especialidades, bloqueos,
  usuarios, auditoria) con `[Authorize(Roles = Administrador)]` y dibuja `SeccionEnConstruccion`.
  `AccesoDenegado.razor:21` fija `HttpContext.Response.StatusCode = 403` y dibuja: así se sabe que una
  página SSR puede cambiar su código sin que `UseStatusCodePagesWithReExecute` la reemplace.
- `Ingreso.razor`: `EditForm` con `FormName="ingreso"`, `[SupplyParameterFromForm] Formulario? Input`, y en
  `IngresarAsync` `HttpContext.SignInAsync` seguido de `Navegacion.NavigateTo`. Sin `try/catch`.
- `SesionPersonal`: esquema `"Personal"`, 30 min deslizante, revalidación cada 5 min con
  `ValidadorSesionPersonal` (2.1 AC 25). `Program.cs`: `UseAuthentication()`, `UseAuthorization()`,
  `UseAntiforgery()` en ese orden.
- Pruebas: `PersonalWebApplicationFactory` (usuarios fijos `recepcion1`, `medico1`, `admin1`; `Reloj`
  `FakeTimeProvider`; `Registro` `ProveedorDeRegistroEnMemoria`); `FormularioHttp` (`Cliente`, `ValorDe`,
  `TokenDeAsync`, `IngresarAsync`, `SalirAsync`, `Destino`, `CookiesPersonal`, `Alerta`,
  `CrearUsuarioAsync`, `FallarAsync`, `RecargarAsync`); `SinParalelismo.cs` apaga el paralelismo de
  Web.Tests. La `PostgresFixture` de Aplicacion.Tests vive en
  `tests/HospitalTurnos.Aplicacion.Tests/PostgresFixture.cs` (el contrato dice `Infraestructura/`, el
  archivo real está en la raíz del proyecto: se usa el real, no se mueve).
- `SesionYPermisosHttpTests.Secciones_SegunRol_PermiteLasSuyasYDevuelve403LegibleEnLasAjenas` (2.1 T19) exige `TextoEnConstruccion` en **toda** sección
  permitida, incluida `/personal/admin/usuarios`: por eso esta story la modifica (AC 27).
- `MenuLateralTests` (2.1 T21) compara los enlaces del menú **exactamente**: agregar un enlace la rompe.

#### En el framework (.NET 10.0.11, verificado con `ilspycmd` sobre `~/.dotnet/shared/Microsoft.AspNetCore.App/10.0.11/Microsoft.Extensions.Identity.Core.dll`)

- `UserManager.ChangePasswordAsync` → `ChangePasswordCoreAsync`: verifica la actual
  (`PasswordMismatch` si no coincide) y llama a `UpdatePasswordHash`, que valida la nueva, cambia el
  hash y llama a `UpdateSecurityStampInternal` (decompilado, líneas 567-574 y 1794-1806). O sea:
  **cambiar la contraseña ya cambia el sello**; no hace falta llamar aparte a `UpdateSecurityStampAsync`.
  Una contraseña actual incorrecta **no** suma `AccessFailedCount`.
- `PasswordOptions`: `RequireNonAlphanumeric = true`, `RequiredUniqueChars = 1` por defecto.
  `UserOptions.AllowedUserNameCharacters` = letras sin acento, dígitos y `-._@+`. Nuestro patrón es más
  estricto (sin `@` ni `+`) y se valida antes.
- `IdentityErrorDescriber` tiene los métodos virtuales del AC 24 (en inglés por defecto).
- El `UserStore` de EF guarda en cada operación (`AutoSaveChanges = true`) sobre el `HospitalDbContext`
  del alcance. Si el caso de uso abre una transacción en ese contexto (`Database.BeginTransactionAsync`),
  todas las escrituras de `UserManager` y la auditoría quedan dentro. Un choque con `user_name_index`
  sale como `DbUpdateException` con `PostgresException { SqlState: "23505" }` (Identity no lo atrapa).

### Patron de referencia verificado

**Patrón 1 — fábrica y pruebas HTTP de formularios SSR**: los de la 2.1, en este mismo repo.

| Ancla `archivo:línea` | Qué se verificó y qué se toma |
|---|---|
| `src/HospitalTurnos.Web/Components/Personal/Ingreso.razor` (el `EditForm` de `FormName="ingreso"`) | `<EditForm Model="Input" method="post" OnValidSubmit="IngresarAsync" FormName="ingreso">` + `DataAnnotationsValidator`: formulario SSR validado en el servidor. Se toma para el alta y el cambio. |
| `src/HospitalTurnos.Web/Components/Personal/AccesoDenegado.razor:21` | Una página SSR fija su propio código de estado. Se toma para el `409/400/403/404` de los carteles. |
| `src/HospitalTurnos.Infraestructura/Identidad/SembradorPersonal.cs:63` | `Asegurar(IdentityResult)`: un resultado fallido de Identity lanza con código y descripción. Se repite en los casos de uso nuevos. |
| `tests/HospitalTurnos.Web.Tests/Personal/FormularioHttp.cs` (`CrearUsuarioAsync`) | `CrearUsuarioAsync` crea un usuario propio por prueba. Se usa tal cual. |
| `tests/HospitalTurnos.Web.Tests/Personal/PersonalWebApplicationFactory.cs` | Fábrica con reloj de prueba y registro en memoria. Se usa sin cambios. |

**Patrón 2 — cambio de contraseña: plantilla oficial** (no está en el proyecto guía
changomas-testing-csharp, que es una API sin usuarios). Generada con
`dotnet new blazor --auth Individual -n Tpl22 --no-restore` (SDK 10.0.400), archivo
`Components/Account/Pages/Manage/ChangePassword.razor`:

| Línea | Qué se verificó y qué se toma |
|---|---|
| 18 | `<EditForm Model="Input" FormName="change-password" OnValidSubmit="OnValidSubmitAsync" method="post">`. Se toma con `FormName="cambio-contrasena"`. |
| 78-83 | `UserManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword)`; si falla, muestra las descripciones de los errores. Se toma (con las descripciones en castellano del AC 24). |
| 85 | `SignInManager.RefreshSignInAsync(user)`: vuelve a emitir la cookie después del cambio para que la sesión siga. Acá no hay `SignInManager` (2.1, "Lo que NO hay que hacer"): se hace lo mismo con `HttpContext.SignInAsync("Personal", principalNuevo)`. |
| 106 | `[Compare("NewPassword", ...)]` para la repetición. Se toma con el texto del AC 22. |

La plantilla resuelve el "paso obligatorio antes de entrar" del doble factor con una pantalla aparte
(`Login.razor:132`, `result.RequiresTwoFactor` → otra página). Acá la marca viaja en un claim y la hace
cumplir un middleware (ver Decisiones).

**Patrón 3 — candado de PostgreSQL**: no hay uno previo en el repo ni en el guía (`grep -rn "advisory" src tests`
→ 0 resultados). Se usa `pg_advisory_xact_lock(bigint)` de PostgreSQL 17 (función estándar,
ejecutable por cualquier rol; se suelta sola al terminar la transacción).

### Que hay que hacer (la delta)

1. Una marca `DebeCambiarContrasena` en el usuario, con su migración.
2. Dos casos de uso: administrar usuarios (listar, crear, desactivar, reactivar, desbloquear) y cambiar
   la propia contraseña. Los dos auditan y escriben en una transacción.
3. La pantalla A4 en lugar de la sección en construcción, y la pantalla de cambio de contraseña.
4. Que el ingreso con contraseña temporal obligue a cambiarla antes de usar el sistema.
5. Mensajes de Identity en castellano.

### Decisiones de esta story (con su porqué)

- **La contraseña temporal se muestra en pantalla, una vez, y no se manda por email** (UX A4: "se
  muestra una sola vez"). Los usuarios del personal no tienen email obligatorio (2.1:
  `RequireUniqueEmail = false`, sin campo de email). Se responde con `Cache-Control: no-store` para que
  "Atrás" no la vuelva a mostrar desde la memoria del navegador.
- **No se redirige después del alta** (sin el patrón POST-redirect-GET). Redirigir obligaría a guardar
  la contraseña en algún lado para mostrarla en el GET. Costo aceptado: si se recarga la página, el
  navegador reenvía el alta y la respuesta es `409` (usuario repetido), que no daña nada.
- **El cambio obligatorio se hace con la sesión abierta y un claim, no antes de emitir la cookie.**
  El ingreso con la temporal ya está protegido por el bloqueo de la 2.1; un middleware de 15 líneas
  manda todo `/personal/` a la pantalla de cambio. La alternativa (no dar sesión hasta cambiar) pide
  un segundo formulario anónimo que vuelva a verificar la temporal y a contar intentos: dos lugares
  con la misma lógica de bloqueo.
- **Una contraseña actual equivocada en la pantalla de cambio no suma intentos de bloqueo.** Es lo que
  hace Identity (`ChangePasswordAsync`) y la pantalla exige una sesión abierta. Bloquear a alguien ya
  ingresado no cortaría su sesión (2.1 AC 25 no mira el bloqueo).
- **"Siempre queda un administrador" se garantiza por construcción, sin contar administradores.** Con
  "sólo un administrador activo desactiva" + "nadie se desactiva a sí mismo", después de cualquier
  desactivación sigue activo al menos quien actuó. El único agujero es la carrera (A y B se desactivan
  a la vez, cada uno ve al otro activo): el candado `pg_advisory_xact_lock` la cierra, porque el
  segundo revisa a su actor **después** de que el primero confirmó. Una regla "no desactivar al
  último" sería código que nunca se ejecuta.
- **El caso de uso revisa en la base que el actor sea un administrador activo**, aunque la página ya
  pida el rol: la sesión puede atrasar hasta 5 minutos (2.1 AC 25). Sin esto, un administrador recién
  desactivado podría seguir dando altas durante ese tiempo. El control vive en un servicio único
  (`IActorVigente`) y no en este caso de uso, porque la 3.1 y la 3.2 tienen el mismo hueco (I-3 de la
  revisión, decisión 16).
- **Errores de regla en pantallas SSR**: la página atrapa **sólo** las excepciones del dominio
  (`catch (Exception e) when (ManejadorGlobalDeErrores.EsEsperable(e))`), fija el código que fija el
  manejador global (`FijarEstadoEnPagina`, que usa `EstadoPara`) y muestra el mensaje en un cartel arriba del formulario, conservando
  lo escrito (UX §8, "Error de regla de negocio"). Lo inesperado sigue al manejador global (AC 18). El
  mapeo excepción → código vive en un solo lugar, el manejador de 1.2. El docente lo eligió para las
  tres pantallas del sprint (decisión 1): la 3.1 y la 3.2 usan este mismo mecanismo.
- **La pantalla de cambio no va al menú**: la T21 de la 2.1 compara el menú exacto. Se llega por la
  redirección del primer ingreso o por la dirección (AC 23).
- **El administrador inicial (sembrador de la 2.1) no queda obligado a cambiar su contraseña.** La
  eligió quien instaló el sistema en `.env`; S-30 habla de "usuarios nuevos" dados de alta por A4.
- **Nombres de usuario en minúsculas.** Identity ya los compara sin mayúsculas; guardarlos en
  minúsculas evita que la tabla muestre `Ana.Ruiz` y `ana.ruiz` como si fueran distintos.

### Decisiones tomadas por el docente (2026-10-16)

Numeradas como en `revision-sprint-2.md` ("Decisiones abiertas consolidadas").

1. **Cómo se muestra un error de regla en una pantalla SSR** (decisión 1). **Elegido:** cartel en la
   propia página, con el código del manejador global y lo tipeado intacto, con
   `EsEsperable`/`EstadoPara` de esta story **para las tres pantallas** del sprint (A4, A2, A1). Así
   está escrito en los AC 6, 9, 12, 14–16 y en "Implementación concreta".
2. **Contraseña olvidada** (decisión 2). **Elegido:** una story nueva corta ("Generar una contraseña
   temporal nueva desde A4", reusa `ContrasenaTemporal` y la marca), **temprano en el sprint 3**: hoy
   quien olvida la suya sólo sale tocando la base. Esta story no la implementa. Sumarla al plan es
   tarea del docente.
3. **Cambiar el rol de un usuario existente** (decisión 3). **Elegido:** sólo al alta. Protege el
   vínculo usuario–médico de la 3.2.
4. **Vencimiento de la contraseña temporal** (decisión 4). **Elegido:** sin vencimiento (el PRD no da
   un número).
5. **Permiso del administrador en el caso de uso** (decisión 16). **Elegido:** se confirma en la base
   en cada cambio, con el servicio único `IActorVigente` que crea esta story y reusan la 3.1 y la 3.2.
6. **Usuario médico sin vincular** (decisión 17). Lo resuelve la 3.2: A4 muestra `Sin vincular` en la
   columna **Médico vinculado** de un usuario `Medico` sin vínculo. Esta story no agrega esa columna.

### Implementacion concreta

**Contratos de Aplicacion** (`IGestionUsuariosPersonal.cs`):

```csharp
public interface IGestionUsuariosPersonal
{
    Task<IReadOnlyList<UsuarioPersonalFila>> ListarAsync(CancellationToken ct);
    Task<AltaUsuarioPersonal> CrearAsync(NuevoUsuarioPersonal datos, ActorAuditoria actor, CancellationToken ct);
    Task DesactivarAsync(Guid usuarioId, ActorAuditoria actor, CancellationToken ct);
    Task ReactivarAsync(Guid usuarioId, ActorAuditoria actor, CancellationToken ct);
    Task DesbloquearAsync(Guid usuarioId, ActorAuditoria actor, CancellationToken ct);
}

public sealed record NuevoUsuarioPersonal(string? NombreCompleto, string? Usuario, string? Rol);
// La contraseña viaja sólo en este objeto, de vuelta a la página que la muestra una vez.
public sealed record AltaUsuarioPersonal(Guid Id, string Usuario, string Rol, string ContrasenaTemporal);
public sealed record UsuarioPersonalFila(Guid Id, string Usuario, string NombreCompleto, string Rol,
    bool Activo, DateTimeOffset? BloqueadoHasta, bool DebeCambiarContrasena);
```

`ICambioContrasenaPersonal.cs`:

```csharp
public interface ICambioContrasenaPersonal
{
    Task<ResultadoCambioContrasena> CambiarAsync(string usuarioId, string actual, string nueva, CancellationToken ct);
}

public sealed record ResultadoCambioContrasena(bool Exitoso, IReadOnlyList<string> Errores,
    ClaimsPrincipal? Principal = null, string? Rol = null);
```

**`IActorVigente`** (Aplicacion/Personal, pieza compartida, AC 26):

```csharp
public interface IActorVigente
{
    /// Confirma en la BASE (no en la sesión, que puede atrasar 5 minutos) que el actor es un usuario
    /// existente, activo y con el único rol Administrador. Si no, AccesoDenegadoException(mensajeSiNoLoEs).
    /// Devuelve el actor con Quien = NombreCompleto leído de la base, para la auditoría.
    Task<ActorAuditoria> ExigirAdministradorActivoAsync(ActorAuditoria actor, string mensajeSiNoLoEs, CancellationToken ct);
}
```

**`ActorVigenteIdentity`** (Infraestructura/Identidad, `Scoped`): usa `UserManager<UsuarioPersonal>`,
que trabaja sobre el `HospitalDbContext` del alcance. Por eso, llamado **dentro** de una transacción
abierta en ese contexto (como en `DesactivarAsync`, después del candado), lee lo que confirmó el otro
pedido.

```csharp
public async Task<ActorAuditoria> ExigirAdministradorActivoAsync(ActorAuditoria actor, string mensajeSiNoLoEs, CancellationToken ct)
{
    var admin = Guid.TryParse(actor.UsuarioId, out var id) ? await usuarios.FindByIdAsync(id.ToString()) : null;
    if (admin is null || !admin.Activo || await usuarios.GetRolesAsync(admin) is not [Roles.Administrador])
        throw new AccesoDenegadoException(mensajeSiNoLoEs);
    return new(admin.Id.ToString(), admin.NombreCompleto, Roles.Administrador, CanalAuditoria.Personal);
}
```

**`GestionUsuariosPersonalIdentity`** (Infraestructura/Identidad). Dependencias:
`UserManager<UsuarioPersonal>`, `HospitalDbContext`, `IAuditoria`, `IActorVigente`, `TimeProvider`.

```csharp
private const long CandadoAdministradores = 2_200_001;   // número fijo: sólo identifica este candado
private const string SinPermiso = "No tenés permiso para administrar usuarios.";

public async Task DesactivarAsync(Guid usuarioId, ActorAuditoria actor, CancellationToken ct)
{
    await using var tx = await contexto.Database.BeginTransactionAsync(ct);
    // Primero el candado, DESPUÉS leer actor y objetivo: así el segundo de dos pedidos simultáneos ve
    // lo que confirmó el primero (AC 10).
    await contexto.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({CandadoAdministradores})", ct);
    var admin = await actorVigente.ExigirAdministradorActivoAsync(actor, SinPermiso, ct);  // AC 15
    var usuario = await BuscarAsync(usuarioId);                        // NoEncontradoException (AC 16)
    if (usuario.Id == Guid.Parse(admin.UsuarioId!))
        throw new ReglaDeNegocioException("No podés desactivar tu propio usuario. Pedíselo a otro administrador.");
    if (!usuario.Activo)
        throw new ConflictoException("Ese usuario ya estaba desactivado.");
    usuario.Activo = false;
    // Guarda Activo y el sello nuevo juntos: la revalidación de 2.1 corta su sesión (AC 8).
    Asegurar(await usuarios.UpdateSecurityStampAsync(usuario));
    auditoria.Registrar(admin, AccionesAuditoria.UsuarioPersonalDesactivado, EntidadTipo,
        usuario.Id.ToString(), new { usuario = usuario.UserName });
    await contexto.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);
}
```

El actor que se audita es **el que devuelve `IActorVigente`** (id y nombre leídos de la base), no el de
la sesión.

- `CrearAsync`: validar `datos` con `ReglasUsuarioPersonal` (antes de abrir la transacción; lanza
  `ReglaDeNegocioException` con el texto del AC 5) → transacción → `actorVigente.ExigirAdministradorActivoAsync(actor, SinPermiso, ct)` →
  `var temporal = ContrasenaTemporal.Generar();` → `CreateAsync(usuario, temporal)` (si falla con
  `DuplicateUserName` → `ConflictoException` con su descripción; otro error → `Asegurar`) →
  `AddToRoleAsync` → `Registrar` → `SaveChangesAsync` → `CommitAsync` → devolver `AltaUsuarioPersonal`.
  Todo dentro de un `try { ... } catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })`
  que lanza la misma `ConflictoException` (AC 6, T4). El `DbUpdateException` es el **único** que se
  atrapa.
- `ReactivarAsync`: transacción → actor (`IActorVigente`) → objetivo → si ya está activo, `ConflictoException("Ese usuario ya estaba activo.")`
  → `Activo = true` → `Asegurar(await usuarios.UpdateAsync(usuario))` → auditar → guardar → confirmar.
  No toca sello, bloqueo ni marca.
- `DesbloquearAsync`: transacción → actor (`IActorVigente`) → objetivo → bloqueado = `usuario.LockoutEnd > reloj.GetUtcNow()`;
  si no, `ConflictoException("Ese usuario no está bloqueado.")` → `SetLockoutEndDateAsync(usuario, null)`
  → `ResetAccessFailedCountAsync(usuario)` → auditar → guardar → confirmar.
- `ListarAsync`: dos consultas `AsNoTracking` (usuarios ordenados por `NombreCompleto` y `UserName`;
  pares `UserRoles` × `Roles`) y el armado en memoria (el hospital tiene decenas de usuarios, no miles).
  `Rol`: el único, `"(sin rol)"` o `"(varios roles)"`. `BloqueadoHasta` = `LockoutEnd` si es posterior a
  `reloj.GetUtcNow()`, si no `null`.

**`CambioContrasenaPersonalIdentity.CambiarAsync`**:

```csharp
var usuario = await usuarios.FindByIdAsync(usuarioId);
var roles = usuario is null ? [] : await usuarios.GetRolesAsync(usuario);
if (usuario is null || !usuario.Activo || roles.Count != 1)
    throw new AccesoDenegadoException("Tu sesión ya no es válida. Volvé a ingresar.");
if (nueva == actual)
    return Fallido("La nueva contraseña tiene que ser distinta de la actual.");

await using var tx = await contexto.Database.BeginTransactionAsync(ct);
var cambio = await usuarios.ChangePasswordAsync(usuario, actual, nueva);   // también cambia el sello
if (!cambio.Succeeded)
    return Fallido([.. cambio.Errors.Select(e => e.Description)]);          // sin Commit: no queda nada
var motivo = usuario.DebeCambiarContrasena ? "primer-ingreso" : "voluntario";
usuario.DebeCambiarContrasena = false;
auditoria.Registrar(new ActorAuditoria(usuario.Id.ToString(), usuario.NombreCompleto, roles[0], CanalAuditoria.Personal),
    AccionesAuditoria.ContrasenaPersonalCambiada, "UsuarioPersonal", usuario.Id.ToString(), new { motivo });
await contexto.SaveChangesAsync(ct);   // guarda la marca y la auditoría
await tx.CommitAsync(ct);
return new(true, [], await fabricaDePrincipal.CrearAsync(usuario), roles[0]);
```

**`FabricaPrincipalPersonal.CrearAsync(UsuarioPersonal u)`**: lo que hoy hace el paso 6 de
`IngresoPersonalIdentity` (`IUserClaimsPrincipalFactory` + claim `nombre_completo`) y, si
`u.DebeCambiarContrasena`, el claim `ClaimsPersonal.DebeCambiarContrasena` con valor `"si"`.
`IngresoPersonalIdentity` la usa y devuelve
`ResultadoIngreso.Exitoso(principal, roles[0], encontrado.DebeCambiarContrasena)`.

**`ContrasenaTemporal.Generar()`** (clase estática):

```csharp
// Sin 0/O/o, 1/l/I: se dicta en voz alta o se copia a mano (AC 4).
private const string Mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
private const string Minusculas = "abcdefghijkmnpqrstuvwxyz";
private const string Numeros = "23456789";
private const string Simbolos = "-#.";
private const int Largo = 12;
// Uno de cada grupo (la política los exige), el resto de todos, y se mezcla. Todo con
// RandomNumberGenerator.GetInt32: un generador predecible daría contraseñas adivinables.
```

**`CambioDeContrasenaObligatorio`** (Web/Personal):

```csharp
public static IApplicationBuilder UseCambioDeContrasenaObligatorio(this IApplicationBuilder app) =>
    app.Use(async (http, siguiente) =>
    {
        var ruta = http.Request.Path.Value ?? "";
        if (http.User.HasClaim(ClaimsPersonal.DebeCambiarContrasena, "si")
            && ruta.StartsWith(RutasPersonal.Prefijo, StringComparison.OrdinalIgnoreCase)
            && !Permitidas.Contains(ruta))
        {
            http.Response.Redirect(RutasPersonal.CambiarContrasena);   // AC 20
            return;
        }
        await siguiente(http);
    });

private static readonly HashSet<string> Permitidas = new(StringComparer.OrdinalIgnoreCase)
    { RutasPersonal.CambiarContrasena, RutasPersonal.Salir, RutasPersonal.Ingreso, RutasPersonal.AccesoDenegado };
```

En `Program.cs`, entre `app.UseAuthentication();` y `app.UseAuthorization();` (necesita `User` ya leído
de la cookie).

**`ActorDeSesion.Desde(ClaimsPrincipal usuario)`** (Web/Personal): `new ActorAuditoria(NameIdentifier,
nombre_completo, Role, CanalAuditoria.Personal)`. Si falta un claim, `InvalidOperationException` (no
pasa con `[Authorize]`; si pasa, es un error de programación y va al 500).

**`UsuariosPersonal.razor`** (`Components/Personal/Administracion/`):

- `@page "/personal/admin/usuarios"`, `@layout LayoutPersonal`,
  `@attribute [Authorize(Roles = Roles.Administrador)]`, `<PageTitle>Usuarios del personal · Las Acacias</PageTitle>`,
  `<h1>Usuarios del personal</h1>` (la T19 de la 2.1 busca ese texto exacto).
- `OnInitialized`: `HttpContext.Response.Headers.CacheControl = "no-store";` y `Alta ??= new();`.
- **Ojo con el orden de SSR**: `OnInitializedAsync` corre **antes** del manejador del formulario. La
  tabla se carga en `CargarAsync()`, que se llama en `OnInitializedAsync` **y otra vez al final de cada
  manejador**; si no, la respuesta muestra el estado viejo (T25 lo verifica).
- Formulario de alta: `EditForm Model="Alta" FormName="alta-usuario" method="post"` con
  `DataAnnotationsValidator`, `[SupplyParameterFromForm(FormName = "alta-usuario")] FormularioAlta? Alta`.
  Campos `NombreCompleto` (`[Required]`, `[StringLength(120)]`), `Usuario` (`[Required]`,
  `[RegularExpression(ReglasUsuarioPersonal.PatronUsuario)]`) y `Rol` (`InputSelect` con opción vacía
  `Elegí un rol` y las tres de `Roles.Todos`, etiquetas `Recepcionista`, `Médico`, `Administrador`;
  `[Required]` más una revisión `Roles.Todos.Contains(Rol)` en el manejador, que agrega el error al
  `EditContext` con `ValidationMessageStore`).
- Acciones: **un solo** `<form method="post" @formname="acciones-usuarios" @onsubmit="EjecutarAccionAsync">`
  con `<AntiforgeryToken />` alrededor de la tabla; cada botón es
  `<button type="submit" name="Accion" value="desactivar:@fila.Id">Desactivar</button>`. El navegador
  envía sólo el botón pulsado. `[SupplyParameterFromForm(FormName = "acciones-usuarios")] string? Accion`.
  Por qué un solo formulario: Blazor no permite dos formularios con el mismo nombre en una página, y
  uno por fila obligaría a un nombre variable que `SupplyParameterFromForm` no puede recibir.
- Patrón de cada manejador:

```csharp
try
{
    var alta = await Gestion.CrearAsync(new(Alta!.NombreCompleto, Alta.Usuario, Alta.Rol),
        ActorDeSesion.Desde(HttpContext.User), HttpContext.RequestAborted);
    confirmacion = alta;          // se dibuja una vez; no se guarda en ningún otro lado
    Alta = new();
}
catch (Exception e) when (ManejadorGlobalDeErrores.EsEsperable(e))   // sólo las del dominio
{
    ManejadorGlobalDeErrores.FijarEstadoEnPagina(HttpContext, e);
    cartel = e.Message;           // role="alert" arriba del formulario
}
await CargarAsync();
```

  **Por qué `FijarEstadoEnPagina` y no `Response.StatusCode = EstadoPara(e)`** (hallado al implementar):
  en .NET 10, Blazor SSR no escribe el cuerpo de una página que terminó de dibujarse con código 404
  (`RazorComponentEndpointInvoker` lo deja para la página "no encontrada" del router): el cartel del
  AC 16 no llegaba. `FijarEstadoEnPagina` fija 400, 403 y 409 en el acto y el 404 en
  `Response.OnStarting`, justo antes de enviar los encabezados.

  `Accion` se separa en `tipo:id`; un tipo que no es `desactivar`, `reactivar` o `desbloquear`, o un id
  que no es `Guid`, lanza `ReglaDeNegocioException("No reconocemos esa acción. Recargá la página.")`
  dentro del mismo `try`.
- Bloqueo en pantalla: `TimeZoneInfo.ConvertTime(fila.BloqueadoHasta.Value, Reloj.LocalTimeZone):HH:mm`
  (`@inject TimeProvider Reloj`).

**`CambiarContrasena.razor`** (`Components/Personal/`): `@page "/personal/cambiar-contrasena"`,
`@layout LayoutIngreso`, `@attribute [Authorize]` (cualquier rol). `EditForm FormName="cambio-contrasena"`
con `Input.Actual`, `Input.Nueva`, `Input.Repetida` (`type="password"`, `[Required]` con los textos del
AC 22, `[Compare(nameof(Nueva), ErrorMessage = "Las contraseñas nuevas no coinciden.")]`). Si el usuario
tiene la marca, título `Elegí tu contraseña` y el texto `Tu contraseña es temporal. Elegí una nueva para seguir.`;
si no, título `Cambiar contraseña`. Si `CambiarAsync` no es exitoso, los errores van en el `role="alert"`
y los tres campos vuelven vacíos. Si es exitoso:
`await HttpContext.SignInAsync(SesionPersonal.Esquema, r.Principal!, new AuthenticationProperties { IsPersistent = false });`
y `NavigateTo(destino)` con `destino` = `ReturnUrl` si `RutasPersonal.EsReturnUrlValido(ReturnUrl)` y no
es `RutasPersonal.CambiarContrasena`; si no, `RutasPersonal.InicioDe(r.Rol)`. Misma captura filtrada que
A4 para `AccesoDenegadoException`.

**`Ingreso.razor`** (cambio mínimo en `IngresarAsync`, después del `SignInAsync`):

```csharp
var destino = RutasPersonal.EsReturnUrlValido(ReturnUrl) ? ReturnUrl! : RutasPersonal.InicioDe(resultado.Rol);
if (resultado.DebeCambiarContrasena)   // S-30: antes de usar el sistema, elegir la propia (AC 19)
    destino = RutasPersonal.EsReturnUrlValido(ReturnUrl)
        ? $"{RutasPersonal.CambiarContrasena}?ReturnUrl={Uri.EscapeDataString(ReturnUrl!)}"
        : RutasPersonal.CambiarContrasena;
Navegacion.NavigateTo(destino);
```

**`SesionYPermisosHttpTests` (2.1 T19)**: agregar
`private static readonly string[] RutasYaConstruidas = ["/personal/admin/usuarios"];` y cambiar
`Assert.Contains(TextoEnConstruccion, html);` por
`if (RutasYaConstruidas.Contains(ruta)) Assert.DoesNotContain(TextoEnConstruccion, html); else Assert.Contains(TextoEnConstruccion, html);`
con un comentario: cada story que construye una sección suma su ruta acá.

### Salvaguarda de BD (obligatoria antes de las pruebas de Web)

**No** usar `dotnet ef database update` (usaría `turnos_app`, sin `CREATE`). La migración la aplica el
arranque como `turnos_owner`:

```bash
docker compose up -d --build     # aplica GestionUsuariosPersonal al arrancar
```

Contra el PostgreSQL de Compose (mismo `01-roles.sh` que la `PostgresFixture`):

1. `docker compose exec db psql -U postgres -d hospital_turnos -c "SELECT column_name, data_type, is_nullable, column_default FROM information_schema.columns WHERE table_name = 'asp_net_users' AND column_name = 'debe_cambiar_contrasena';"`
   → una fila: `boolean`, `NO`, `false`.
2. `... -c "SELECT count(*) FROM asp_net_users WHERE debe_cambiar_contrasena;"` → `0` (los usuarios
   que ya existían, como el administrador inicial, no quedan obligados).
3. `... -c "SELECT has_table_privilege('turnos_app', 'asp_net_users', 'SELECT,INSERT,UPDATE'), has_function_privilege('turnos_app', 'pg_advisory_xact_lock(bigint)', 'EXECUTE'), has_table_privilege('turnos_app', 'registros_auditoria', 'UPDATE');"`
   → `t`, `t`, `f`.
4. Si cualquier resultado no coincide → DETENERSE → corregir la story primero. **No** tapar un `f`
   con un `GRANT` en la migración.

### Archivos a crear/modificar

| Acción | Archivo |
|---|---|
| Crear | Los de `files_to_create` del frontmatter |
| Modificar | `UsuarioPersonal.cs` (propiedad `DebeCambiarContrasena`) |
| Modificar | `IngresoPersonalIdentity.cs` (paso 6 con `FabricaPrincipalPersonal`; devuelve la marca) |
| Modificar | `RegistroIdentidad.cs` (`AddErrorDescriber<DescriptorErroresIdentity>()` y los 4 servicios nuevos, entre ellos `IActorVigente`) |
| Modificar | `HospitalDbContextModelSnapshot.cs` (lo regenera `dotnet ef migrations add`) |
| Modificar | `AccionesAuditoria.cs` (5 constantes, AC 25) |
| Modificar | `IIngresoPersonal.cs` (`ResultadoIngreso.DebeCambiarContrasena`, `ClaimsPersonal.DebeCambiarContrasena`) |
| Modificar | `Program.cs` (`app.UseCambioDeContrasenaObligatorio()`) |
| Modificar | `RutasPersonal.cs` (`CambiarContrasena = "/personal/cambiar-contrasena"`) |
| Modificar | `ManejadorGlobalDeErrores.cs` (`EstadoPara`, `EsEsperable` y `FijarEstadoEnPagina`, sin cambiar el comportamiento del manejador) |
| Modificar | `Ingreso.razor` (destino del AC 19) |
| Modificar | `SeccionesAdministracion.razor` (quitar `@page "/personal/admin/usuarios"`) |
| Modificar | `FormularioHttp.cs` (`EnviarAsync`, `Estado`) |
| Modificar | `SesionYPermisosHttpTests.cs` (`RutasYaConstruidas`, AC 27) |

### Lo que NO hay que hacer

- **NO vincular usuarios con médicos** ni mostrar la columna `Médico vinculado`: es de la 3.2.
- **NO borrar usuarios** (ni botón, ni método): sólo desactivar. Un usuario borrado deja sin dueño su
  auditoría y, desde la 3.2, el vínculo con su médico.
- **NO editar nombre, rol ni contraseña ajena** desde A4 (decisiones tomadas 2 y 3).
- **NO mostrar, registrar ni auditar la contraseña temporal** fuera de la única respuesta del alta: ni
  en el `ILogger`, ni en `Detalle`, ni en `TempData`, ni en una consulta (`?pwd=`).
- **NO mandar la contraseña por email** ni por la bandeja de salida.
- **NO poner `HasDefaultValue(...)` en `DebeCambiarContrasena`** en el modelo de EF: el mismo problema
  que `Activo` (comentario en `HospitalDbContext.OnModelCreating`). El `DEFAULT false` de la columna lo
  pone la migración para las filas existentes.
- **NO atrapar `Exception` sin filtro** en las páginas: sólo `when (ManejadorGlobalDeErrores.EsEsperable(e))`.
  `NavigateTo` en SSR usa una excepción de navegación que un `catch` genérico se traga (lección de 2.1).
- **NO hacer interactivas** A4 ni la pantalla de cambio (`@rendermode`): la cookie nueva del cambio se
  escribe en la respuesta HTTP.
- **NO agregar la pantalla de cambio al menú lateral**: rompe la 2.1 T21.
- **NO contar administradores para decidir** si se puede desactivar (ver Decisiones); **NO** sacar el
  candado "porque las pruebas pasan sin él": T9 puede pasar por casualidad sin candado.
- **NO usar `DateTime.Now`/`UtcNow` ni `DateTimeOffset.UtcNow`** en `src/` (la prueba de la 1.2 lo
  revisa). Para "bloqueado ahora", `reloj.GetUtcNow()`.
- **NO usar `dotnet ef database update`** (ver Salvaguarda).
- **NO cambiar los usuarios fijos** (`recepcion1`, `medico1`, `admin1`) en ninguna prueba: todo lo que
  cambia estado crea su propio usuario.
- **NO contar filas de `registros_auditoria` sin filtrar** por un id o marcador propio (base compartida).
- **NO paginar** A4: el hospital tiene decenas de usuarios.

### Como verificar que funciona

```bash
cd ~/SourceCode/hospital-turnos

# 1. Migración (no se conecta a la base)
dotnet ef migrations add GestionUsuariosPersonal \
  --project src/HospitalTurnos.Infraestructura --startup-project src/HospitalTurnos.Web \
  --output-dir Persistencia/Migraciones
git diff --stat src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/   # sólo la migración nueva y el snapshot

# 2. Pruebas de esta story (Docker para Testcontainers)
dotnet test --filter "Story=2.2"
# Esperado: 31 pruebas declaradas (más los casos de cada [Theory]), 0 fallas
dotnet test --filter "Story=2.2&AC=10"     # sólo la carrera de los dos administradores

# 3. Suite completa y CI local (AC 27)
dotnet test
bash scripts/ci/local.sh; echo "rc=$?"    # rc=0

# 4. Verificación manual en el navegador
docker compose up -d --build
open http://localhost:8080/personal/ingreso
# Esperado, en este orden (ingresar como el administrador inicial de .env):
#  a. Menú → "Usuarios del personal": tabla con el administrador, sin botón "Desactivar" en su fila
#  b. Alta "Ana Ruiz" / "ana.ruiz" / Médico: cartel con la contraseña temporal; "Atrás" y volver a
#     entrar a la pantalla NO la muestran otra vez
#  c. Alta "ana.ruiz" otra vez: "Ya existe un usuario con el nombre ana.ruiz. Elegí otro."
#  d. En otra ventana privada: ingresar como ana.ruiz con la temporal → pantalla "Elegí tu contraseña";
#     escribir /personal/medico/mi-agenda en la barra → vuelve a la pantalla de cambio;
#     cambiarla → entra a "Mi agenda"
#  e. Como administrador: "Desactivar" a ana.ruiz; en la ventana de ana.ruiz, a los 5 minutos
#     cualquier clic la manda al ingreso, y el ingreso dice "Tu usuario está desactivado..."
#  f. "Reactivar": ana.ruiz vuelve a entrar con SU contraseña (no la temporal)
#  g. 5 contraseñas malas con ana.ruiz → la tabla muestra "Bloqueado hasta las HH:mm";
#     "Desbloquear" → ana.ruiz entra en el acto

# 5. La auditoría quedó escrita, sin contraseñas
docker compose exec db psql -U postgres -d hospital_turnos \
  -c "SELECT cuando, quien, rol, accion, entidad_id, detalle FROM registros_auditoria WHERE accion LIKE 'usuario-personal.%' ORDER BY id DESC LIMIT 10;"
```

## Dev Agent Record

### Agent Model Used

Claude Opus 5.5 (`claude-opus-5-5`), sesión 09 (2026-10-16), circuito completo: validación, plan aprobado,
pruebas primero, revisores y prueba en vivo.

### Debug Log References

- Línea de base antes de tocar nada (`main` 462f415): Dominio 20, Aplicacion 52, Web 107 → 179 en verde.
- `RegistroIdentidad.AddIdentidadPersonal` no toca `RequireNonAlphanumeric`: queda en `true` (valor de
  Identity). Se dejó un comentario en el archivo.
- Salvaguarda de BD (Compose, después de `docker compose up -d --build`): `debe_cambiar_contrasena |
  boolean | NO | false`; `0` usuarios obligados; permisos `t | t | f`. Coincide con lo esperado.
- Migración generada `20261016212111_GestionUsuariosPersonal`: sólo `AddColumn<bool>` con
  `defaultValue: false` y su snapshot.
- Rojo de las pruebas: Aplicacion.Tests no compilaba por los tipos que la story manda crear (rojo
  legítimo); Web.Tests compilaba y 25 de 28 casos fallaban por las pantallas ausentes. Los 3 que ya
  pasaban son T30 (POST sin token → `400`): ese freno lo da el antifalsificación del framework desde la
  1.1, y la prueba queda como protección de no-regresión del AC 17.
- T9 sin el candado (`pg_advisory_xact_lock` comentado): falló 5 de 5 corridas; con el candado, verde.
  La prueba protege lo que dice.
- Suite completa (portón de pruebas, por proyecto porque el repo usa `.slnx`): Dominio 20, Aplicacion 94,
  Web 135 → 249 en verde (antes 179). `bash scripts/ci/local.sh`: rc 0, cobertura de líneas 98,1 %
  (umbral 80 %), ramas 100 %.
- Prueba en vivo (paso 4 de "Como verificar"): Chrome contra el Compose, 5 grupos registrados con
  `smoke-run.sh` + la CI local, todos con 0 fallas; matriz por AC 27/27 en vivo
  (`.advisors/dod-runtime/2.2-runtime-matrix.md`). Incluye la auditoría caída (INSERT revocado y
  restaurado), dos administradores a la vez y la base detenida. Capturas en `Sesiones/img/09-*`.
- GitHub Actions del PR #7 (commit 4286564): corridas 38981528710 (push) y 38981533395 (pull_request),
  6 de 6 controles en verde cada una (~3 min). Captura en `Sesiones/img/09-github-actions-corrida.png`.
- Revisores: 3 vueltas (7, 6 y 4 revisores), sin CRITICAL; los avisos quedaron resueltos o anotados en
  "Review Follow-ups".

### Completion Notes List

- **El 404 del cartel (AC 16) necesitó un ajuste a la "Implementación concreta".** En .NET 10, Blazor
  SSR no escribe el cuerpo de una página que terminó de dibujarse con `StatusCode = 404`
  (`RazorComponentEndpointInvoker`: lo deja para la página "no encontrada" del router). Fijar el 404
  en el manejador, como dice la story, devolvía 404 **sin** el cartel. La página A4 fija el 404 en
  `Response.OnStarting` (justo antes de enviar los encabezados); 400, 403 y 409 se fijan en el acto.
  Por decisión del docente (Hallazgos, vuelta 1) quedó como pieza compartida:
  `ManejadorGlobalDeErrores.FijarEstadoEnPagina` (AC 26 e "Implementación concreta" actualizados). La
  3.1 y la 3.2 lo reusan; sus stories todavía dicen `Response.StatusCode = EstadoPara(e)` y se
  corrigen al validarlas antes de implementar (pendiente anotado).
- `FormularioAlta` saca los espacios de los extremos al recibir el valor, así la página valida lo mismo
  que el caso de uso (`  ana.ruiz  ` es válido, AC 3).
- La pantalla de cambio de contraseña junta en un solo `role="alert"` los mensajes de los campos y los
  de Identity (AC 22); no repite mensajes debajo de cada campo.
- A4 muestra el rol `Medico` como `Médico` (el valor guardado no cambia).

### File List

- Creado: `docs/qa/2.2-pasos.md`
- Creado: `src/HospitalTurnos.Aplicacion/Personal/IActorVigente.cs`
- Creado: `src/HospitalTurnos.Aplicacion/Personal/ICambioContrasenaPersonal.cs`
- Creado: `src/HospitalTurnos.Aplicacion/Personal/IGestionUsuariosPersonal.cs`
- Modificado: `src/HospitalTurnos.Aplicacion/Personal/IIngresoPersonal.cs`
- Modificado: `src/HospitalTurnos.Dominio/Auditoria/AccionesAuditoria.cs`
- Creado: `src/HospitalTurnos.Dominio/Personal/ReglasUsuarioPersonal.cs`
- Creado: `src/HospitalTurnos.Infraestructura/Identidad/ActorVigenteIdentity.cs`
- Creado: `src/HospitalTurnos.Infraestructura/Identidad/CambioContrasenaPersonalIdentity.cs`
- Creado: `src/HospitalTurnos.Infraestructura/Identidad/ContrasenaTemporal.cs`
- Creado: `src/HospitalTurnos.Infraestructura/Identidad/DescriptorErroresIdentity.cs`
- Creado: `src/HospitalTurnos.Infraestructura/Identidad/FabricaPrincipalPersonal.cs`
- Creado: `src/HospitalTurnos.Infraestructura/Identidad/GestionUsuariosPersonalIdentity.cs`
- Modificado: `src/HospitalTurnos.Infraestructura/Identidad/IngresoPersonalIdentity.cs`
- Modificado: `src/HospitalTurnos.Infraestructura/Identidad/RegistroIdentidad.cs`
- Modificado: `src/HospitalTurnos.Infraestructura/Identidad/UsuarioPersonal.cs`
- Creado: `src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/20261016212111_GestionUsuariosPersonal.Designer.cs`
- Creado: `src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/20261016212111_GestionUsuariosPersonal.cs`
- Modificado: `src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/HospitalDbContextModelSnapshot.cs`
- Creado: `src/HospitalTurnos.Web/Components/Personal/Administracion/UsuariosPersonal.razor`
- Creado: `src/HospitalTurnos.Web/Components/Personal/CambiarContrasena.razor`
- Modificado: `src/HospitalTurnos.Web/Components/Personal/Ingreso.razor`
- Modificado: `src/HospitalTurnos.Web/Components/Personal/Secciones/SeccionesAdministracion.razor`
- Modificado: `src/HospitalTurnos.Web/Errores/ManejadorGlobalDeErrores.cs`
- Creado: `src/HospitalTurnos.Web/Personal/ActorDeSesion.cs`
- Creado: `src/HospitalTurnos.Web/Personal/CambioDeContrasenaObligatorio.cs`
- Modificado: `src/HospitalTurnos.Web/Personal/RutasPersonal.cs`
- Modificado: `src/HospitalTurnos.Web/Program.cs`
- Creado: `tests/HospitalTurnos.Aplicacion.Tests/Ayudas/UsuariosDePrueba.cs`
- Creado: `tests/HospitalTurnos.Aplicacion.Tests/Personal/CambioContrasenaPersonalIdentityTests.cs`
- Creado: `tests/HospitalTurnos.Aplicacion.Tests/Personal/ContrasenaTemporalTests.cs`
- Creado: `tests/HospitalTurnos.Aplicacion.Tests/Personal/Dobles/AuditoriaQueFalla.cs`
- Creado: `tests/HospitalTurnos.Aplicacion.Tests/Personal/Dobles/ValidadorUsuarioQueAceptaTodo.cs`
- Creado: `tests/HospitalTurnos.Aplicacion.Tests/Personal/GestionUsuariosPersonalIdentityTests.cs`
- Creado: `tests/HospitalTurnos.Web.Tests/Personal/CambioContrasenaHttpTests.cs`
- Creado: `tests/HospitalTurnos.Web.Tests/Personal/Dobles/GestionUsuariosConBaseCaida.cs`
- Modificado: `tests/HospitalTurnos.Web.Tests/Personal/FormularioHttp.cs`
- Modificado: `tests/HospitalTurnos.Web.Tests/Personal/SesionYPermisosHttpTests.cs`
- Creado: `tests/HospitalTurnos.Web.Tests/Personal/UsuariosPersonalHttpTests.cs`
- Modificado: `docs/stories/2-2-gestion-usuarios.md`

### Change Log

- 2026-10-16: Story escrita (Mary) contra el código de `main` (`c3413cc`), con el brief compartido del
  sprint 2. 27 AC, 31 verificaciones, 4 decisiones abiertas para el docente.
- 2026-10-16: Corregida con la revisión adversarial (`revision-sprint-2.md`) y las decisiones del
  docente. Esta story queda como **única dueña** de las piezas compartidas del sprint (I-1):
  `ActorDeSesion`, `EsEsperable`/`EstadoPara`, el servicio nuevo `IActorVigente` (I-3, decisión 16),
  la carpeta `Administracion/` (I-2), `FormularioHttp.EnviarAsync`/`Estado`, `RutasYaConstruidas` y
  la ayuda nueva `UsuariosDePrueba`. Estilo de acciones de auditoría escrito (M-7). T28 suma `/salud`
  (M-11). Decisiones 1–4, 16 y 17 tomadas; ninguna abierta. Siguen 27 AC y 31 verificaciones.
- 2026-10-16: Al validar antes de implementar (sesión 09): las citas por número de línea de archivos
  que esta story modifica pasan a nombrar el método o el formulario; la nota de `AccionesAuditoria` y la
  consulta del paso 5 de "Como verificar" quedan con `usuario-personal.` (con punto), como el AC 25.
  Ningún AC ni prueba cambia.
- 2026-10-16: Hallazgos de la vuelta 1 de revisores (sesión 09), con decisión del docente: el AC 26 suma
  `ManejadorGlobalDeErrores.FijarEstadoEnPagina(HttpContext, Exception)` como pieza compartida, porque
  Blazor .NET 10 no escribe el cuerpo de una página SSR en 404 (el cartel del AC 16 no llegaba).
  "Implementación concreta" y la tabla de archivos lo reflejan. El alta traduce a "usuario repetido"
  sólo el 23505 del índice `user_name_index` (lo nombra el AC 6). Ningún AC cambia de resultado; las
  pruebas no cambian.
- 2026-10-16: Revisión de legibilidad de las pantallas (tester no técnico, sesión 09), con decisión del
  docente: AC 1 suma `(sos vos)` en la fila propia; AC 5, la ayuda debajo de `Usuario`; AC 20, el botón
  `Salir` en la pantalla de cambio; AC 22, un mensaje por renglón y el título `La contraseña nueva no
  sirve:` para los de la política. T20, T23, T28 y T29 verifican esos textos. Queda fuera: al terminar
  el cambio la persona llega a su inicio, que todavía dice "en construcción" (lo construyen sus stories).

## Review Follow-ups (Advisors)

<!-- tycon-pipeline: generado por Step 6/7. Cada item = finding advisor no resuelto (CRITICAL waived o WARNING). Al resolver el finding en el código, marcá [x]. Step 0/1 detecta los abiertos al retomar (pipeline.js followups-status). -->

- [x] **[WARNING]** SPEC-W2 — `docs/stories/2-2-gestion-usuarios.md` — Decisión del docente: FijarEstadoEnPagina como pieza compartida en ManejadorGlobalDeErrores; AC 26, Implementación concreta y Change Log actualizados. <!-- fkey:SPEC-W2|docs/stories/2-2-gestion-usuarios.md -->
- [x] **[WARNING]** SILENT-W — `src/HospitalTurnos.Infraestructura/Identidad/GestionUsuariosPersonalIdentity.cs` — El catch del 23505 filtra por ConstraintName user_name_index. <!-- fkey:SILENT-W|src/HospitalTurnos.Infraestructura/Identidad/GestionUsuariosPersonalIdentity.cs -->
- [x] **[WARNING]** VAL-W15 — `docs/stories/2-2-gestion-usuarios.md` — File List, Tasks y Status del cuerpo se completan al preparar el commit; el ajuste del 404 pasó a Implementación concreta. <!-- fkey:VAL-W15|docs/stories/2-2-gestion-usuarios.md -->
- [x] **[WARNING]** VAL-W14 — `docs/stories/2-2-gestion-usuarios.md` — Ancla de FormularioHttp citada por CrearUsuarioAsync. <!-- fkey:VAL-W14|docs/stories/2-2-gestion-usuarios.md -->
- [ ] **[WARNING]** TEST-W — `src/HospitalTurnos.Infraestructura/Identidad/GestionUsuariosPersonalIdentity.cs` — Vuelta 2: sin prueba del 23505 de otro índice. El alta no puede provocarlo hoy (el único índice único que toca es user_name_index); la rama va al manejador global, cubierto por la 1.2. <!-- fkey:TEST-W|src/HospitalTurnos.Infraestructura/Identidad/GestionUsuariosPersonalIdentity.cs -->
