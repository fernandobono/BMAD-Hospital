---
epic: 2
story: "2.1"
title: "Ingreso del personal con usuario y contraseña, roles y vencimiento de sesión"
status: ready-for-dev
sprint: 1
priority: "P0"
approach: "[ADAPT]"
depends_on: ["1.1", "1.2", "1.3", "1.5"]
files_to_create:
  - "src/HospitalTurnos.Dominio/Personal/Roles.cs"
  - "src/HospitalTurnos.Aplicacion/Personal/IIngresoPersonal.cs"
  - "src/HospitalTurnos.Infraestructura/Identidad/UsuarioPersonal.cs"
  - "src/HospitalTurnos.Infraestructura/Identidad/IngresoPersonalIdentity.cs"
  - "src/HospitalTurnos.Infraestructura/Identidad/SembradorPersonal.cs"
  - "src/HospitalTurnos.Infraestructura/Identidad/OpcionesAdministradorInicial.cs"
  - "src/HospitalTurnos.Infraestructura/Identidad/RegistroIdentidad.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/<marca>_IdentidadPersonal.cs"
  - "src/HospitalTurnos.Web/Personal/RutasPersonal.cs"
  - "src/HospitalTurnos.Web/Personal/SesionPersonal.cs"
  - "src/HospitalTurnos.Web/Personal/ValidadorSesionPersonal.cs"
  - "src/HospitalTurnos.Web/Personal/EndpointsPersonal.cs"
  - "src/HospitalTurnos.Web/Components/Personal/Ingreso.razor"
  - "src/HospitalTurnos.Web/Components/Personal/AccesoDenegado.razor"
  - "src/HospitalTurnos.Web/Components/Personal/LayoutPersonal.razor"
  - "src/HospitalTurnos.Web/Components/Personal/LayoutIngreso.razor"
  - "src/HospitalTurnos.Web/Components/Personal/MenuLateral.razor"
  - "src/HospitalTurnos.Web/Components/Personal/SeccionEnConstruccion.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Secciones/SeccionesRecepcion.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Secciones/SeccionesRecepcionYAdministracion.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Secciones/SeccionesMedico.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Secciones/SeccionesAdministracion.razor"
  - "tests/HospitalTurnos.Aplicacion.Tests/Personal/IngresoPersonalIdentityTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Personal/SembradorPersonalTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Personal/Dobles/HasherQueCuenta.cs"
  - "tests/HospitalTurnos.Web.Tests/SinParalelismo.cs"
  - "tests/HospitalTurnos.Web.Tests/Personal/PersonalWebApplicationFactory.cs"
  - "tests/HospitalTurnos.Web.Tests/Personal/FormularioHttp.cs"
  - "tests/HospitalTurnos.Web.Tests/Personal/IngresoPersonalHttpTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Personal/SesionYPermisosHttpTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Personal/ArranquePersonalTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Personal/MenuLateralTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Personal/ConfiguracionVersionadaTests.cs"
files_to_modify:
  - "src/HospitalTurnos.Infraestructura/Persistencia/HospitalDbContext.cs"
  - "src/HospitalTurnos.Dominio/Auditoria/AccionesAuditoria.cs"
  - "src/HospitalTurnos.Dominio/Auditoria/CanalAuditoria.cs"
  - "src/HospitalTurnos.Web/Program.cs"
  - "src/HospitalTurnos.Web/Dockerfile"
  - "src/HospitalTurnos.Web/Components/Routes.razor"
  - "src/HospitalTurnos.Web/Components/_Imports.razor"
  - "docker-compose.yml"
  - ".env.ejemplo"
---

# Story 2.1: Ingreso del personal con usuario y contraseña, roles y vencimiento de sesión

Status: ready-for-dev

## Story

Como integrante del personal del Hospital Las Acacias (recepcionista, médico o administrador),
quiero entrar al sistema con mi usuario y mi contraseña y ver sólo las secciones de mi rol,
para trabajar en lo mío sin poder tocar lo de otros, y que si me levanto del puesto la sesión se
cierre sola a los 30 minutos sin uso.

## Acceptance Criteria

> Rutas del personal: todas bajo `/personal/` (la web del paciente del sprint 5 queda con la raíz).
> Mensajes en castellano rioplatense, como en `docs/03-ux.md`.
> Los AC 24 a 27 se agregaron en la revisión adversarial y están en su sección temática; los
> números 1 a 23 no cambiaron, para no romper referencias desde otras stories.
>
> **Campos de auditoría del ingreso** (valen para los AC 1, 2b, 3, 5 y 24; cumplen los campos
> obligatorios de la story 1.3, que lanza `ArgumentException` si alguno viene vacío):
>
> | Campo | Valor |
> |---|---|
> | `UsuarioId` | id del usuario (`Guid` como texto) |
> | `Quien` | `NombreCompleto` del usuario |
> | `Rol` | su único rol; `"(sin rol)"` si no tiene ninguno; `"(varios roles)"` si tiene más de uno |
> | `Canal` | `CanalAuditoria.Personal` (valor del contrato: acciones del personal fuera de turnos) |
> | `EntidadTipo` | `"UsuarioPersonal"` |
> | `EntidadId` | id del usuario (el mismo de `UsuarioId`) |
> | `Detalle` | `null` en el ingreso exitoso; en los demás, `{"motivo":"..."}` del AC que corresponda. **Nunca** la contraseña ni lo que la persona tipeó |

### Ingreso (pantalla S0) — FR-09

1. **Given** un usuario activo con un rol y contraseña correcta **When** envía el formulario de
   `/personal/ingreso` **Then** la respuesta es `302` al inicio de su rol (tabla del AC 13), con la
   cookie `.HospitalTurnos.Personal` del esquema `"Personal"` (`HttpOnly`, `SameSite=Strict`, sin
   `Expires`: cookie de sesión; `Secure` según el AC 26), `AccessFailedCount` vuelve a 0, y se guarda
   **un** `RegistroAuditoria` con `Accion = AccionesAuditoria.IngresoPersonal` y los campos de la tabla
   de arriba.
2. **Given** (a) un usuario que **no existe**, o (b) uno que existe con contraseña incorrecta **When**
   envía el formulario **Then** la respuesta es `200` con la misma S0, el mensaje
   `Usuario o contraseña incorrectos.` dentro de un elemento `role="alert"` (**el mismo texto en los
   dos casos**), el campo usuario conserva lo escrito en la página devuelta (no se guarda en ningún
   lado), la contraseña vuelve vacía y no hay cookie `Personal`. Además:
   - (a) **No** se escribe en `registros_auditoria`. Se escribe en el `ILogger`, nivel `Warning`,
     `Ingreso del personal fallido: el usuario no existe.` **sin** el texto tipeado (ni usuario ni
     contraseña). Para que el tiempo de respuesta no delate que el usuario no existe, la contraseña
     se verifica igual contra un hash ficticio (mismo `IPasswordHasher`, mismo costo PBKDF2).
   - (b) Su `AccessFailedCount` sube en 1 y se guarda un registro
     `AccionesAuditoria.IngresoPersonalFallido` con los campos de la tabla y
     `Detalle = {"motivo":"contrasena-incorrecta"}`.
3. **Given** un usuario con 4 intentos fallidos seguidos **When** falla el 5.º **Then** queda
   bloqueado 15 minutos (`LockoutEnd` entre ahora + 14 y ahora + 16 minutos), ve **el mismo**
   `Usuario o contraseña incorrectos.` (el bloqueo **no** se muestra: si se mostrara, revelaría qué
   usuarios existen) y se registra `AccionesAuditoria.IngresoPersonalBloqueado` con
   `Detalle = {"motivo":"bloqueo"}`. **Given** ese usuario bloqueado **When** envía la contraseña
   **correcta** **Then** ve el mismo mensaje genérico, no recibe cookie y se registra otro
   `IngresoPersonalBloqueado` con `Detalle = {"motivo":"intento-durante-bloqueo"}`. **Given** un
   usuario inexistente **When** se intenta 5 veces o más **Then** cada respuesta es idéntica a la de
   un usuario real bloqueado (mismo texto, mismo estado `200`).
4. **Given** un usuario cuyo `LockoutEnd` ya pasó **When** envía la contraseña correcta **Then**
   ingresa como en el AC 1 y su `AccessFailedCount` queda en 0.
5. **Given** un usuario con `Activo = false`, o uno sin ningún rol asignado, que envía la contraseña
   **correcta** **When** envía el formulario **Then** no recibe cookie, ve
   `Tu usuario está desactivado. Pedile al administrador que lo reactive.` o
   `Tu usuario no tiene un rol asignado. Pedile al administrador que lo revise.` respectivamente, y se
   registra `IngresoPersonalFallido` con los campos de la tabla (`Rol` = su rol si está desactivado
   con rol; `"(sin rol)"` si no tiene) y `Detalle` `{"motivo":"desactivado"}` o `{"motivo":"sin-rol"}`.
   (El estado se informa sólo a quien ya probó conocer la contraseña.)
6. **Given** el usuario o la contraseña vacíos (o el usuario con más de 100 caracteres) **When**
   envía el formulario **Then** ve debajo de cada campo `Ingresá tu usuario.` / `Ingresá tu contraseña.`
   (o `El usuario tiene como máximo 100 caracteres.`), la validación ocurre también en el servidor
   (NFR-10: un POST armado a mano da el mismo resultado), no se consulta Identity, **no** se audita y
   el `AccessFailedCount` de un usuario existente no cambia.
7. **Given** un `ReturnUrl` que empieza con `/personal/` y no con `//` **When** el ingreso es
   correcto **Then** el `302` va a esa ruta. **Given** cualquier otro `ReturnUrl`
   (`https://malo.example`, `//malo.example`, `/otra-cosa`) **Then** se ignora y el `302` va al
   inicio del rol (sin redirección abierta).
8. **Given** un POST a `/personal/ingreso` sin el token antifalsificación (antiforgery) **When**
   llega al servidor **Then** responde `400` y no se intenta el ingreso (ni auditoría ni contador).
9. **Given** que la base no responde durante el ingreso **When** un navegador envía el formulario
   (pedido con `Accept: text/html`) **Then** recibe `500` con la página `/Error` de la story 1.2 y su
   mensaje genérico `Algo falló. Probá de nuevo en unos minutos.`, no recibe cookie, y el detalle
   queda sólo en el `ILogger` (NFR-09). La excepción **no** se atrapa en la página de ingreso: la
   maneja el `ManejadorGlobalDeErrores` de 1.2.

### Vencimiento de la sesión — NFR-04, S-24

10. **Given** una sesión iniciada **When** pasan 31 minutos sin ningún pedido (reloj de prueba) y pide
    `/personal/recepcion/turnos` **Then** recibe `302` a `/personal/ingreso?ReturnUrl=%2Fpersonal%2Frecepcion%2Fturnos`,
    y al volver a ingresar termina en `/personal/recepcion/turnos` (UX §8, "Sesión vencida").
11. **Given** una sesión iniciada **When** hace un pedido a los 20 minutos y otro a los 40 (20 desde
    el anterior) **Then** los dos responden `200`: el vencimiento es de 30 minutos **sin uso**
    (deslizante), no 30 minutos desde el ingreso.
25. **Given** una sesión iniciada **When** en la base el usuario pasa a `Activo = false`, se borra, o
    cambia su sello de seguridad (`SecurityStamp`, lo cambia la story 2.2 al cambiar la contraseña)
    **Then** la sesión sigue a lo sumo 5 minutos (intervalo de revalidación, medido con el reloj del
    hospital) y el primer pedido posterior recibe `302` a `/personal/ingreso?ReturnUrl=<ruta>` con la
    cookie `Personal` vencida en la respuesta. Mientras el usuario sigue válido, la revalidación es
    invisible (`200`) y renueva la cookie.

### Roles y permisos — FR-11, R-13

12. **Given** nadie ingresado **When** pide cualquier ruta de la tabla del AC 13 **Then** recibe `302`
    a `/personal/ingreso?ReturnUrl=<ruta>`. `/personal/ingreso` y `/personal/acceso-denegado` son las
    únicas rutas del personal sin ingreso.
13. Cada sección exige su rol. **Given** un usuario **When** pide una sección de su rol **Then**
    recibe `200` con el título de la sección y `Esta sección se habilita en una próxima versión.`;
    **When** pide una sección de otro rol **Then** la respuesta final (siguiendo la redirección a
    `/personal/acceso-denegado`) es `403` con `No tenés permiso para entrar a esta sección.` y un
    enlace al inicio de su rol — nunca un `500` ni una página en blanco.

    | Ruta | Pantalla (UX §3) | Recepcionista | Medico | Administrador |
    |---|---|:-:|:-:|:-:|
    | `/personal/recepcion/turnos` | R1 Turnos | ✔ (inicio) | | |
    | `/personal/agendas` | R2 Agendas | ✔ | | ✔ |
    | `/personal/para-avisar` | R3 Pacientes para avisar | ✔ | | ✔ |
    | `/personal/medico/mi-agenda` | M1 Mi agenda | | ✔ (inicio) | |
    | `/personal/medico/mi-propuesta` | M2 Mi propuesta de agenda | | ✔ | |
    | `/personal/admin/medicos` | A1 Médicos | | | ✔ (inicio) |
    | `/personal/admin/propuestas` | A6 Propuestas de agenda | | | ✔ |
    | `/personal/admin/especialidades` | A2 Especialidades | | | ✔ |
    | `/personal/admin/bloqueos` | A3 Bloqueos de agenda | | | ✔ |
    | `/personal/admin/usuarios` | A4 Usuarios del personal | | | ✔ |
    | `/personal/admin/auditoria` | A5 Auditoría | | | ✔ |

14. **Given** un usuario ingresado **When** se dibuja el menú lateral del layout del personal
    **Then** muestra **sólo** las secciones de su rol, en el orden del mapa de navegación de UX §3,
    más su `NombreCompleto` y un botón `Salir` arriba (como R1 en UX §5):
    - Recepcionista (3): R1 Turnos, R2 Agendas, R3 Pacientes para avisar.
    - Medico (2): M1 Mi agenda, M2 Mi propuesta de agenda.
    - Administrador (8): A1 Médicos, A6 Propuestas de agenda, A2 Especialidades, A3 Bloqueos de
      agenda, A4 Usuarios del personal, A5 Auditoría, R3 Pacientes para avisar, R2 Agendas.
24. **Given** un usuario con **más de un** rol asignado (FR-09: "cada usuario tiene un rol") que envía
    la contraseña correcta **When** envía el formulario **Then** no recibe cookie, ve
    `Tu usuario tiene más de un rol asignado. Pedile al administrador que lo revise.` y se registra
    `IngresoPersonalFallido` con `Rol = "(varios roles)"` y `Detalle = {"motivo":"varios-roles"}`.
    (Así `RutasPersonal.InicioDe(rol)` nunca recibe un rol ambiguo.)

### Cerrar sesión

15. **Given** una sesión iniciada **When** envía `Salir` (POST a `/personal/salir` con token
    antifalsificación) **Then** la cookie `Personal` se borra, recibe `302` a `/personal/ingreso`, y
    un pedido posterior a una sección protegida vuelve a pedir ingreso. **Given** un POST a
    `/personal/salir` sin token **Then** responde `400` y la sesión sigue.

### Administrador inicial

16. **Given** una base sin usuarios del personal y la configuración
    `Personal:AdministradorInicial:Usuario`, `:Contrasena` y `:NombreCompleto` con valores (en Docker,
    variables `Personal__AdministradorInicial__*` tomadas de `.env`) **When** arranca la aplicación
    **Then** existen los 3 roles de `Roles` y un usuario activo con rol `Administrador`; el `ILogger`
    informa `Administrador inicial creado: <usuario>` sin la contraseña.
17. **Given** que ya hay al menos un usuario del personal **When** arranca con cualquier configuración
    de administrador inicial (válida, vacía o con una contraseña fuera de política) **Then** no se crea
    ni se modifica ningún usuario (no se pisa la contraseña de nadie), **no** se valida la contraseña
    configurada (no hay nada que sembrar) y el arranque sigue; los roles faltantes, si los hubiera, se
    crean.
18. **"Sin configuración"** = falta la sección, **o** `Usuario` o `Contrasena` están vacíos o tienen
    sólo espacios (es lo que pasa con un clon limpio sin `.env`: Compose pasa las variables vacías y
    para .NET la clave existe con valor `""`). **Given** una base sin usuarios y sin configuración
    **When** arranca **Then** crea sólo los roles, escribe **una** advertencia
    `No hay usuarios del personal y falta Personal:AdministradorInicial (Usuario y Contrasena). La aplicación arranca sin administrador: completá ADMIN_INICIAL_USUARIO y ADMIN_INICIAL_CONTRASENA en .env y reiniciá.`
    y la aplicación **arranca igual** (no lanza: con `restart: unless-stopped` un error acá dejaría el
    contenedor reiniciándose en bucle y rompería el smoke E1 de la story 1.1).
19. **Given** una base sin usuarios y `Usuario` y `Contrasena` con valor, pero la contraseña no cumple
    la política (AC 21) **When** arranca **Then** el arranque falla con `InvalidOperationException` cuyo
    mensaje nombra `Personal:AdministradorInicial:Contrasena` y lista las reglas incumplidas (sin la
    contraseña), y no queda ningún usuario creado. Es un error de configuración explícito: mejor
    cortar con un mensaje claro que arrancar sin el administrador que alguien quiso crear.
20. Ningún archivo versionado trae una contraseña, ni de desarrollo (contrato §Secretos):
    `appsettings*.json` no tiene la clave `Contrasena`; `docker-compose.yml` toma la del administrador
    de `${ADMIN_INICIAL_CONTRASENA:-}` (sin valor por defecto); `.env.ejemplo` (lo crea la story 1.1)
    suma `ADMIN_INICIAL_USUARIO`, `ADMIN_INICIAL_CONTRASENA` y `ADMIN_INICIAL_NOMBRE`, con la contraseña
    vacía. `.env` ya está en `.gitignore`.

### Contraseñas y transporte — NFR-03

21. Las contraseñas se guardan con el hash de Identity (PBKDF2), nunca en texto. Política:
    mínimo 10 caracteres, con mayúscula, minúscula y dígito (los valores por defecto de Identity,
    salvo el largo, que sube de 6 a 10). Bloqueo: `MaxFailedAccessAttempts = 5`,
    `DefaultLockoutTimeSpan = 15 min`, `AllowedForNewUsers = true`. El usuario inexistente paga el
    mismo costo de verificación que uno real (hash ficticio, AC 2a).
26. **Given** un entorno distinto de `Development` **When** se emite la cookie `Personal` **Then**
    lleva `Secure` **siempre** (`CookieSecurePolicy.Always`), aunque el pedido llegue por HTTP (detrás
    de un proxy que termina TLS). En `Development` usa `SameAsRequest` para poder probar con
    `http://localhost`. (`UseForwardedHeaders` y HSTS los configura la story 1.1.)
27. **Given** `ProteccionDeDatos:Carpeta` con valor (en Compose, un volumen) **When** la aplicación
    se reinicia o se reconstruye la imagen **Then** las sesiones abiertas y los tokens
    antifalsificación siguen siendo válidos: las claves de protección de datos se guardan en esa
    carpeta (`PersistKeysToFileSystem`) con `SetApplicationName("HospitalTurnos")`. Sin valor, se usa
    el comportamiento por defecto del framework (pruebas, `dotnet run`).

### Contrato con otras stories

22. Esta story **escribe** con `IAuditoria.Registrar(...)` (story 1.3) y la story 9.1 **lee** los
    registros: `AccionesAuditoria` suma las constantes `IngresoPersonal = "ingreso-personal"`,
    `IngresoPersonalFallido = "ingreso-personal-fallido"` e
    `IngresoPersonalBloqueado = "ingreso-personal-bloqueado"`, con los campos de la tabla del inicio y
    `Canal = CanalAuditoria.Personal`. Contrato hacia la story 2.2: (a) un rol por usuario — 2.2 no
    permite asignar dos (AC 24 es la red de seguridad); (b) 2.2 **lee y escribe**
    `UsuarioPersonal.Activo` y, al desactivar o cambiar la contraseña, llama a
    `UserManager.UpdateSecurityStampAsync` para que el AC 25 corte las sesiones abiertas; (c) desbloquear
    a alguien antes de los 15 minutos es de 2.2 (A4), con la auditoría de A5 como forma de enterarse.

### No-regresión

23. Siguen en verde las pruebas de las stories 1.1 a 1.5; el `ManejadorGlobalDeErrores` (1.2) y la
    protección de `registros_auditoria` (1.3: sólo `INSERT` y `SELECT` para `turnos_app`, disparador
    contra `UPDATE`/`DELETE`) no se tocan.

### Tests (28 verificaciones)

| # | Test | AC | Nivel | Escenario | Resultado esperado |
|---|------|----|-------|-----------|--------------------|
| T1 | `ValidarAsync_CredencialesCorrectas_DevuelveExitosoYRegistraIngreso` | 1,22,23 | integration | Usuario propio del test (Recepcionista, activo) con 2 fallos previos; contraseña correcta | `Exitoso`, principal con `ClaimTypes.Role = Recepcionista`, claim `nombre_completo` y claim de sello de seguridad; `AccessFailedCount = 0`; 1 fila con `entidad_id` = su id, `accion = ingreso-personal`, `canal = Personal`, `quien` = su nombre, `rol = Recepcionista`, `entidad_tipo = UsuarioPersonal`, `detalle` nulo |
| T2 | `ValidarAsync_ContrasenaIncorrecta_RegistraFalloConCamposObligatorios` | 2,22 | integration | Usuario propio del test; contraseña mala `Mala-Clave-1` | `CredencialesInvalidas`; 1 fila `ingreso-personal-fallido` con `usuario_id` y `entidad_id` = su id, `quien` = su nombre, `rol = Recepcionista`, `canal = Personal`, `entidad_tipo = UsuarioPersonal`, `detalle` = `{"motivo":"contrasena-incorrecta"}` (no contiene `Mala-Clave-1`); `AccessFailedCount = 1` |
| T3 | `ValidarAsync_QuintoIntentoFallido_BloqueaQuinceMinutosYRegistraBloqueo` | 3,21,22 | integration | Usuario propio; 5 contraseñas malas seguidas y después la correcta | 5.º → `Bloqueado`, `LockoutEnd` en [ahora+14, ahora+16] min; la correcta → `Bloqueado`; 2 filas `ingreso-personal-bloqueado` con su id y todos los campos obligatorios, motivos `bloqueo` e `intento-durante-bloqueo` |
| T4 | `ValidarAsync_BloqueoVencido_PermiteIngresarYReiniciaContador` | 4 | integration | Usuario propio; `LockoutEnd` puesto en la base a hace 1 minuto, `AccessFailedCount = 5`; contraseña correcta | `Exitoso`, `AccessFailedCount = 0` |
| T5 | `ValidarAsync_UsuarioDesactivadoSinRolOConVariosRoles_NoIngresaYRegistraMotivo` | 5,24 | integration | `[Theory]` con usuario propio: (a) `Activo = false` con rol Medico; (b) sin rol; (c) roles Recepcionista y Administrador; contraseña correcta en los tres | `Desactivado` / `SinRol` / `VariosRoles`; 1 fila `ingreso-personal-fallido` con `entidad_id` = su id y `rol` = `Medico` / `(sin rol)` / `(varios roles)`; `detalle` con motivo `desactivado` / `sin-rol` / `varios-roles` |
| T6 | `SembrarAsync_BaseSinUsuariosConConfiguracion_CreaRolesYAdministrador` | 16,21 | integration | Contenedor propio (base sin usuarios); config `admin` / `Clave-Inicial-2026` / `Administración General` | 3 roles; 1 usuario con rol Administrador, `Activo = true`; `PasswordHash` ≠ contraseña y `VerifyHashedPassword` = `Success`; el `ILogger` tiene `Administrador inicial creado: admin` y ningún mensaje contiene la contraseña |
| T7 | `SembrarAsync_YaHayUsuarios_NoCreaNiModificaNiValida` | 17 | integration | Contenedor propio con un usuario `recepcion1`; se guarda `PasswordHash` y `SecurityStamp`; `[Theory]` config de admin (a) válida; (b) con contraseña `corta1` | No lanza en ninguno; sigue habiendo 1 usuario; hash y sello sin cambios; no existe `admin` |
| T8 | `SembrarAsync_SinConfiguracionOVacia_SoloCreaRolesYAvisa` | 18 | integration | Contenedor propio (base sin usuarios); `[Theory]`: (a) sin la sección; (b) `Usuario = ""`, `Contrasena = ""`; (c) `Usuario = "  "`, `Contrasena = "  "`; (d) `Usuario = "admin"`, `Contrasena = ""` | No lanza; 3 roles, 0 usuarios, exactamente 1 advertencia con el texto del AC 18 |
| T9 | `SembrarAsync_ContrasenaFueraDePolitica_FallaSinCrearUsuario` | 19 | integration | Contenedor propio (base sin usuarios); config `admin` / `corta1` | `InvalidOperationException` cuyo mensaje contiene `Personal:AdministradorInicial:Contrasena` y no contiene `corta1`; 0 usuarios |
| T10 | `PostIngreso_CredencialesCorrectas_EmiteCookieYRedirigeAlInicioDelRol` | 1,13,26 | integration | `[Theory]` por rol (3 usuarios fijos); entorno `Testing`; GET S0 + POST con token | `302` con `Location.PathAndQuery` = `/personal/recepcion/turnos`, `/personal/medico/mi-agenda`, `/personal/admin/medicos`; `Set-Cookie` `.HospitalTurnos.Personal` con `httponly`, `samesite=strict`, `secure` y sin `expires` |
| T11 | `PostIngreso_CredencialesIncorrectasOBloqueado_MuestraElMismoMensajeSinCookie` | 2,3 | integration | `[Theory]` con usuario propio: (a) contraseña mala; (b) 5.º intento malo (queda bloqueado); (c) ya bloqueado, contraseña correcta | `200`; `role="alert"` con `Usuario o contraseña incorrectos.` en los tres; el cuerpo no contiene `bloque`; campo usuario con el valor enviado; sin `Set-Cookie` `.HospitalTurnos.Personal` |
| T12 | `PostIngreso_CamposVaciosOLargos_MuestraMensajePorCampoSinAuditar` | 6 | integration | `[Theory]`: (a) usuario y contraseña vacíos; (b) usuario propio del test y contraseña vacía; (c) usuario de 101 caracteres y contraseña cualquiera | `200`; (a) `Ingresá tu usuario.` y `Ingresá tu contraseña.`; (b) `Ingresá tu contraseña.`, 0 filas con `entidad_id` = su id y `AccessFailedCount` sin cambio; (c) `El usuario tiene como máximo 100 caracteres.` |
| T13 | `PostIngreso_ReturnUrl_SoloRedirigeARutasDelPersonal` | 7 | integration | `[Theory]`: `/personal/agendas`, `https://malo.example`, `//malo.example`, `/otra-cosa` (Recepcionista) | `Location.PathAndQuery` = `/personal/agendas` en el primero; `/personal/recepcion/turnos` en los otros tres |
| T14 | `Post_SinTokenAntiforgery_Devuelve400` | 8,15 | integration | `[Theory]`: POST a `/personal/ingreso` sin token con un usuario propio y su contraseña mala; POST a `/personal/salir` (con sesión) sin token | `400`; en ingreso, 0 filas con `entidad_id` = su id y `AccessFailedCount = 0`; en salir, la sección sigue respondiendo `200` |
| T15 | `PostIngreso_FallaLaBase_MuestraPaginaDeErrorGenericaSinCookie` | 9,23 | integration | Fábrica con `IIngresoPersonal` reemplazado por uno que lanza `NpgsqlException`; POST con `Accept: text/html` (como un navegador) | `500`; `WebUtility.HtmlDecode(cuerpo)` contiene `Algo falló. Probá de nuevo en unos minutos.`; sin cookie; el cuerpo no contiene `Npgsql`; el `ILogger` de 1.2 tiene la excepción |
| T16 | `PaginaProtegida_TreintaYUnMinutosSinUso_RedirigeAlIngresoYVuelve` | 10 | integration | Ingreso de Recepcionista; `FakeTimeProvider.Advance(31 min)`; GET `/personal/recepcion/turnos`; ingreso con el `ReturnUrl` recibido | `302` con `Location.PathAndQuery` del AC 10; tras ingresar, `302` a `/personal/recepcion/turnos` |
| T17 | `PaginaProtegida_PedidosCadaVeinteMinutos_MantieneLaSesion` | 11,25 | integration | Ingreso; `Advance(20 min)` + GET; `Advance(20 min)` + GET | Los dos GET `200` (la revalidación de un usuario válido no corta nada) |
| T18 | `PaginaProtegida_SinSesion_RedirigeAlIngresoConReturnUrl` | 12 | integration | `[Theory]` con las 11 rutas del AC 13, sin cookie | `302`; `Location.PathAndQuery` = `/personal/ingreso?ReturnUrl=<ruta codificada>` |
| T19 | `Secciones_SegunRol_PermiteLasSuyasYDevuelve403LegibleEnLasAjenas` | 13 | integration | `[Theory]` con las 33 combinaciones rol × ruta de la tabla del AC 13, siguiendo redirecciones | ✔ → `200` + título + texto de construcción; vacío → `403` + `No tenés permiso para entrar a esta sección.` + enlace al inicio del rol |
| T20 | `PostSalir_ConSesion_BorraCookieYVuelveAlIngreso` | 15 | integration | Ingreso; POST `/personal/salir` con token; GET `/personal/agendas` | `302` a `/personal/ingreso`; `Set-Cookie` que vence la cookie; el GET posterior da `302` al ingreso |
| T21 | `MenuLateral_SegunRol_MuestraSoloSusSeccionesEnElOrdenDeUx` | 14 | unit | bUnit, `[Theory]` por rol con autorización de prueba (usuario `Carla Gómez`) | Enlaces = exactamente las rutas del AC 14 para ese rol, **en ese orden** (Administrador: medicos, propuestas, especialidades, bloqueos, usuarios, auditoria, para-avisar, agendas); texto `Carla Gómez`; botón `Salir` dentro de un `form` con `action="/personal/salir"` |
| T22 | `ConfiguracionVersionada_AppSettingsYCompose_NoTraenContrasenaDelAdministrador` | 20,27 | unit | Lee `src/HospitalTurnos.Web/appsettings*.json`, `docker-compose.yml` y `.env.ejemplo` desde la raíz del repo | Ningún `appsettings*.json` tiene la clave `Contrasena`; compose usa exactamente `${ADMIN_INICIAL_CONTRASENA:-}` y monta el volumen `claves-proteccion` con `ProteccionDeDatos__Carpeta`; `.env.ejemplo` tiene las 3 variables `ADMIN_INICIAL_*` y la línea `ADMIN_INICIAL_CONTRASENA=` vacía |
| T23 | `ValidarAsync_UsuarioInexistente_NoAuditaYVerificaHashFicticio` | 2,21 | integration | Usuario `noexiste-<guid>`, contraseña `Clave-Tipeada-<guid>`; `IPasswordHasher` envuelto en `HasherQueCuenta`; `ILogger` de prueba | `CredencialesInvalidas`; 0 filas en `registros_auditoria` cuyo `quien`, `entidad_id` o `detalle` contenga el marcador; 1 `Warning` con el texto del AC 2a y ningún mensaje del `ILogger` contiene el usuario ni la contraseña tipeados; `VerifyHashedPassword` llamado 1 vez |
| T24 | `PostIngreso_UsuarioInexistenteSeisVeces_MismaRespuestaQueUnUsuarioBloqueado` | 2,3 | integration | 6 POST con `noexiste-<guid>`; por otro lado, un usuario propio bloqueado (5 fallos) que envía la contraseña correcta | Las 7 respuestas: `200` y el mismo texto del `role="alert"` (`Usuario o contraseña incorrectos.`), comparado con `WebUtility.HtmlDecode`; ninguna con `Set-Cookie` `Personal` |
| T25 | `Arranque_AdministradorInicialVacio_ArrancaSinSembrarYAvisa` | 18 | integration | `PostgresFixture` propia (base vacía); fábrica con `Personal:AdministradorInicial:Usuario = ""` y `:Contrasena = ""` (como Compose sin `.env`) | `CreateClient()` no lanza; `GET /salud` → `200`; 0 usuarios; el `ILogger` del sembrador tiene la advertencia del AC 18 |
| T26 | `SesionPersonal_UsuarioDesactivadoOSelloCambiado_SeCortaAlRevalidar` | 25 | integration | `[Theory]` con usuario propio Recepcionista: (a) `Activo = false` en la base; (b) `UpdateSecurityStampAsync`. Ingreso; cambio en la base; `Advance(4 min)` + GET `/personal/recepcion/turnos`; `Advance(2 min)` + GET | Primer GET `200` (todavía no revalida); segundo GET `302` con `Location.PathAndQuery` = `/personal/ingreso?ReturnUrl=%2Fpersonal%2Frecepcion%2Fturnos` y `Set-Cookie` que vence `.HospitalTurnos.Personal` |
| T27 | `CookiePersonal_SegunEntorno_SecureSoloFueraDeDevelopment` | 26 | integration | `[Theory]` con cliente `http://localhost`: (a) entorno `Testing`; (b) entorno `Development`; ingreso correcto | (a) `Set-Cookie` de `.HospitalTurnos.Personal` contiene `secure`; (b) no lo contiene |
| T28 | `ProteccionDeDatos_CarpetaCompartida_LaSesionSobreviveAOtraInstancia` | 27 | integration | `[Theory]`: fábrica A ingresa y se toma su cookie; fábrica B (otra instancia de la app, misma base) recibe la cookie en GET `/personal/recepcion/turnos`. (a) A y B con la misma `ProteccionDeDatos:Carpeta` temporal; (b) con carpetas distintas | (a) `200` y la carpeta tiene al menos un `key-*.xml`; (b) `302` al ingreso (prueba que la sesión depende de las claves guardadas) |

Todas llevan `[Trait("Story", "2.1")]` y un `[Trait("AC", "n")]` por AC de su columna.
T1–T9 y T23 van en `HospitalTurnos.Aplicacion.Tests`; T10–T22 y T24–T28 en `HospitalTurnos.Web.Tests`
(T21 con bUnit, T22 leyendo archivos).

> El estimado del plan era ~9 pruebas; quedan 28 porque la story cubre ingreso, bloqueo sin
> revelar usuarios, sesión y su revalidación, 11 rutas por rol, salida, siembra, cookie segura y
> claves persistentes, y cada camino feliz lleva su modo de falla.

**Por qué no hay E2E (regla 6.1 del formato).** El proyecto `tests/HospitalTurnos.E2E/` no existe en
el sprint 1 (contrato §Proyectos; lo crea la story 10.1). La interfaz se cubre así:
(1) S0 es un formulario de **renderizado estático en el servidor** (SSR) sin JavaScript propio: lo
que hace el navegador es un GET y un POST de formulario, que T10–T20 y T24–T28 reproducen con
`HttpClient` sobre la aplicación real (`WebApplicationFactory`), incluido el token antifalsificación y
las cookies; (2) el menú, que es lo único que depende del rol al dibujar, se prueba con bUnit (T21);
(3) la verificación manual del navegador queda documentada en "Como verificar" (paso a paso con
Docker Compose), y su resultado se anota en el Dev Agent Record. El recorrido en navegador entra
en la story 10.1.

#### Given/Fixture

- **Base de datos — una sola `PostgresFixture`** (contrato §Pruebas): la misma clase en
  `tests/HospitalTurnos.Web.Tests/` (la crea la story 1.1) y en `tests/HospitalTurnos.Aplicacion.Tests/`
  (copia idéntica, la crea la story 1.3): usa `docker/postgres/initdb/01-roles.sh` vía
  `WithResourceMapping`, base `hospital_turnos`, aplica las migraciones como dueño y expone
  `CadenaDuenio` y `CadenaApp`. Esta story **no** crea ni modifica ninguna de las dos. Las pruebas de
  ingreso usan `CadenaApp` (`turnos_app`), así que prueban los permisos reales que deja el script.
- **Aplicacion.Tests — T1–T5 y T23**: `[Collection("Postgres")]` (la colección de la story 1.3,
  compartida con 1.3 y 1.4). Arman un `ServiceProvider` con `AgregarInfraestructura` (1.1) y
  `AddIdentidadPersonal` sobre una configuración en memoria (`ConnectionStrings:App = CadenaApp`,
  `ConnectionStrings:Migraciones = CadenaDuenio`). Como la base es compartida y
  `registros_auditoria` no se puede limpiar, **cada test crea su propio usuario**
  (`p-<guid de 8>`) y filtra la auditoría por `entidad_id` = su id (T23: por el marcador tipeado).
- **Aplicacion.Tests — T6–T9**: necesitan una base **sin usuarios**, que la compartida no garantiza.
  Cada uno levanta su **propia** instancia (`await using var pg = new PostgresFixture(); await pg.InitializeAsync();`),
  igual que 1.1 T2. Son 4 contenedores más (unos segundos cada uno); a cambio, no dependen del orden.
- **Usuarios fijos de Web.Tests** (los siembra `PersonalWebApplicationFactory` de forma idempotente: si
  ya existe, no lo crea): `recepcion1` / `Recepcion-2026` (Recepcionista, `Carla Gómez`),
  `medico1` / `Medico-2026x` (Medico, `Diego Fernández`), `admin1` / `Admin-2026xx` (Administrador,
  `Lucía Pérez`). Todas cumplen la política del AC 21. **Sólo** se usan en flujos que no cambian su
  estado (ingreso correcto, secciones, salir). Todo test que falla contraseñas, bloquea, desactiva,
  cambia el sello o asigna roles crea su propio usuario con `FormularioHttp.CrearUsuarioAsync(...)`
  (`p-<guid de 8>`). Las contraseñas de prueba viven en el código de las pruebas: no abren ningún
  ambiente.
- **`PersonalWebApplicationFactory : HospitalWebApplicationFactory`** (la de 1.1, que ya inyecta las
  cadenas con `AddInMemoryCollection`, ver revisión I-15): agrega en `ConfigureTestServices` el
  `FakeTimeProvider` y los dobles de `ILogger`, y fija `Personal:AdministradorInicial:Usuario` y
  `:Contrasena` en `""`. Un solo nombre de fábrica por proyecto, con herencia explícita. Variantes de
  entorno o configuración (T15, T25, T27, T28) con `WithWebHostBuilder(...)`.
- **Web.Tests sin paralelismo**: `SinParalelismo.cs` contiene sólo
  `[assembly: CollectionBehavior(DisableTestParallelization = true)]`, con un comentario de por qué: las
  fábricas de 1.1, 1.2 y 2.1 comparten PostgreSQL y aplican migraciones y siembra al arrancar; en
  paralelo, dos arranques simultáneos pelean por la misma base y los conteos se vuelven inestables.
  Equivale a una colección única sin tocar las clases de prueba de otras stories.
- **Cliente HTTP** (`FormularioHttp.cs`): `CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true })`
  (T19 sigue redirecciones; T27 usa `http://localhost` a propósito). Con `https` el cliente reenvía la
  cookie `Secure` del AC 26. Los `Location` vienen **absolutos** (`https://localhost/personal/...`): se
  compara `Location.PathAndQuery`, nunca el texto completo.
- **Cómo se arma el POST**: `GET /personal/ingreso`; del HTML se toman los `input hidden`
  `__RequestVerificationToken` y `_handler` (lo agrega Blazor por el `FormName="ingreso"`); POST
  `application/x-www-form-urlencoded` a `/personal/ingreso` con esos dos campos más
  `Input.Usuario` y `Input.Contrasena`. Los textos se comparan después de `WebUtility.HtmlDecode`.
- **T15**: el pedido lleva `Accept: text/html` (como un navegador al enviar el formulario). Sin ese
  encabezado el `ManejadorGlobalDeErrores` de 1.2 responde `problem+json` y la prueba miraría la
  respuesta equivocada.
- **Registro técnico**: `RegistroEnMemoria<T>` (doble de `ILogger` escrito a mano de la story 1.2;
  si el contrato §Pruebas fija otro nombre para el doble de Aplicacion.Tests, usar ese).
- **`HasherQueCuenta`** (T23): implementa `IPasswordHasher<UsuarioPersonal>` envolviendo el
  `PasswordHasher<UsuarioPersonal>` real y cuenta las llamadas a `VerifyHashedPassword`.
- **T16/T17/T26 — reloj**: la fábrica reemplaza el `TimeProvider` (el `RelojHospital` de la story 1.2) por
  un `FakeTimeProvider` con `SetLocalTimeZone` en `America/Argentina/Cordoba`; la cookie y su
  revalidación lo leen porque `SesionPersonal` asigna `options.TimeProvider` desde el contenedor (ver
  Dev Notes).
- **T4 — bloqueo vencido**: Identity calcula el bloqueo con `DateTimeOffset.UtcNow` (no con
  `TimeProvider`, ver Dev Notes), así que el test escribe `LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(-1)`
  con `UserManager.SetLockoutEndDateAsync` en vez de adelantar un reloj.
- **T25**: `PostgresFixture` propia (base recién creada, sin usuarios), como 1.1 T2.
- **T28**: carpeta temporal por caso (`Directory.CreateTempSubdirectory()`), borrada al final; las dos
  fábricas usan la misma `PostgresFixture`.

## Tasks / Subtasks

- [ ] [SEQ] Task 1: Salvaguarda de estado y de BD (AC: todos)
  - [ ] Confirmar que existen (los crean las stories 1.1, 1.2, 1.3 y 1.5): `HospitalDbContext`, la carpeta
        `Persistencia/Migraciones/` con migraciones aplicables, `RelojHospital` registrado como
        `TimeProvider`, `ManejadorGlobalDeErrores`, `IAuditoria`, `RegistroAuditoria`,
        `CanalAuditoria`, `AccionesAuditoria`, las dos `PostgresFixture` (Web.Tests y Aplicacion.Tests,
        **idénticas**: `diff` entre los dos archivos sin diferencias salvo el `namespace`),
        `HospitalWebApplicationFactory`, `RegistroEnMemoria`, `.env.ejemplo` y la CI de 1.5 en verde
        (`bash scripts/ci/local.sh`). Anotar en el Debug Log la firma exacta de `IAuditoria.Registrar(...)`
        y cómo convierte 1.1 los nombres a `snake_case` en `OnModelCreating`.
  - [ ] Confirmar que `CanalAuditoria` tiene el valor `Personal` (contrato §Auditoría). Si 1.3 no lo
        agregó, agregarlo al final del enum (no cambia los valores existentes) y anotarlo.
  - [ ] Confirmar que existen `src/HospitalTurnos.Web/Components/Routes.razor`, `_Imports.razor`,
        `src/HospitalTurnos.Web/Dockerfile` y `docker-compose.yml` (los crea la story 1.1); si
        `Roles.cs` o `UsuarioPersonal.cs` ya existieran, reutilizarlos y anotarlo.
  - [ ] Confirmar que `Infraestructura` referencia `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
        y `Web.Tests` referencia `Microsoft.AspNetCore.Mvc.Testing`, `bunit`, `Testcontainers.PostgreSql`
        y `Microsoft.Extensions.TimeProvider.Testing` (contrato §Paquetes). Si falta alguno, agregarlo
        al `.csproj` sin versión (la fija `Directory.Packages.props`).
  - [ ] Si algo no coincide → DETENERSE y corregir la story primero.
- [ ] [SEQ] Task 2: Dominio y Aplicacion (AC: 1–5, 22, 24, 25)
  - [ ] `Roles.cs` con `Recepcionista`, `Medico`, `Administrador` y `Todos`.
  - [ ] `IIngresoPersonal` con `ValidarAsync(usuario, contrasena, ct)` y
        `SesionVigenteAsync(usuarioId, selloDeSeguridad, ct)`; `ResultadoIngreso` (`Exitoso`,
        `CredencialesInvalidas`, `Bloqueado`, `Desactivado`, `SinRol`, `VariosRoles`; con
        `ClaimsPrincipal?` y `Rol?`) + `ClaimsPersonal.NombreCompleto = "nombre_completo"`.
  - [ ] Tres constantes nuevas en `AccionesAuditoria` (AC 22).
- [ ] [SEQ] Task 3: Infraestructura — Identity y migración (AC: 21, Salvaguarda de BD)
  - [ ] `UsuarioPersonal : IdentityUser<Guid>` con `NombreCompleto` (requerido, 120) y `Activo` (default `true`).
  - [ ] `HospitalDbContext : IdentityDbContext<UsuarioPersonal, IdentityRole<Guid>, Guid>`;
        `base.OnModelCreating(builder)` **antes** de la conversión a `snake_case`.
  - [ ] `RegistroIdentidad.AddIdentidadPersonal(services, configuration)`: `AddIdentityCore<UsuarioPersonal>`
        con las opciones del AC 21 y `User.RequireUniqueEmail = false`, `.AddRoles<IdentityRole<Guid>>()`,
        `.AddEntityFrameworkStores<HospitalDbContext>()`; registra `IIngresoPersonal`, `SembradorPersonal`
        y `OpcionesAdministradorInicial` (sección `Personal:AdministradorInicial`).
  - [ ] Migración `IdentidadPersonal` con `dotnet ef migrations add` (ver Como verificar).
  - [ ] Correr la Salvaguarda de BD (abajo).
- [ ] [SEQ] Task 4: Infraestructura — ingreso y siembra (AC: 1–5, 16–19, 21, 22, 24)
  - [ ] `IngresoPersonalIdentity` con la secuencia de "Implementación concreta" (incluido el hash
        ficticio y el `ILogger` del usuario inexistente).
  - [ ] `SembradorPersonal.SembrarAsync()` con la regla "sin configuración" del AC 18.
  - [ ] `HasherQueCuenta` y T1–T9, T23 en verde.
- [ ] [SEQ] Task 5: Web — sesión, rutas y endpoints (AC: 7, 8, 10–12, 15, 25–27)
  - [ ] `SesionPersonal.AddSesionPersonal(services, configuration, environment)`: esquema `"Personal"`
        como `DefaultScheme`, cookie del AC 1, `ExpireTimeSpan = 30 min`, `SlidingExpiration = true`,
        `LoginPath`, `AccessDeniedPath`, `LogoutPath`, `options.TimeProvider` desde el contenedor,
        `Cookie.SecurePolicy` del AC 26, `EventsType = typeof(ValidadorSesionPersonal)` y protección de
        datos del AC 27.
  - [ ] `ValidadorSesionPersonal : CookieAuthenticationEvents` (AC 25, ver Implementación concreta).
  - [ ] `RutasPersonal`: constantes de las 11 rutas, título de cada una, `InicioDe(rol)`,
        `SeccionesDe(rol)` en el orden del AC 14 y `EsReturnUrlValido(url)`.
  - [ ] `EndpointsPersonal.MapEndpointsPersonal(app)`: `POST /personal/salir` con `[FromForm]`.
  - [ ] `Program.cs`: `AddIdentidadPersonal`, `AddSesionPersonal`, `AddCascadingAuthenticationState`,
        `AddAuthorization`; en el pipeline `UseAuthentication`, `UseAuthorization`, `UseAntiforgery`
        (en ese orden, después de `UseForwardedHeaders` de 1.1 y antes de `MapRazorComponents`);
        `MapEndpointsPersonal`; al arrancar, después de las migraciones que corre la story 1.1,
        `await SembradorPersonal.SembrarAsync()`.
- [ ] [SEQ] Task 6: Web — pantallas (AC: 1–7, 9, 13, 14, 24)
  - [ ] `Ingreso.razor` (`@page "/personal/ingreso"`, `@layout LayoutIngreso`, `[AllowAnonymous]`, SSR
        estático). `Bloqueado` y `CredencialesInvalidas` se muestran con **el mismo** texto.
  - [ ] `AccesoDenegado.razor` (`[AllowAnonymous]`, pone `HttpContext.Response.StatusCode = 403`).
  - [ ] `LayoutPersonal` + `MenuLateral` + `SeccionEnConstruccion` + las 4 páginas de `Secciones/` con
        su `[Authorize(Roles = ...)]` y varios `@page` cada una.
  - [ ] `Routes.razor`: `AuthorizeRouteView` con `DefaultLayout="typeof(LayoutPersonal)"` para las
        rutas del personal (o `@layout` en cada página de `Secciones/`).
- [ ] [PAR] Task 7: Configuración sin secretos y claves persistentes (AC: 20, 27)
  - [ ] `docker-compose.yml`, servicio `web`: `Personal__AdministradorInicial__Usuario: ${ADMIN_INICIAL_USUARIO:-}`,
        `Personal__AdministradorInicial__Contrasena: ${ADMIN_INICIAL_CONTRASENA:-}`,
        `Personal__AdministradorInicial__NombreCompleto: ${ADMIN_INICIAL_NOMBRE:-Administración}`,
        `ProteccionDeDatos__Carpeta: /home/app/claves-proteccion`, volumen
        `claves-proteccion:/home/app/claves-proteccion` y `claves-proteccion:` en `volumes:`.
  - [ ] `Dockerfile` (etapa final, **antes** de `USER $APP_UID` si lo hay):
        `RUN mkdir -p /home/app/claves-proteccion && chown $APP_UID /home/app/claves-proteccion`
        (sin esto, Docker crea la carpeta del volumen como `root` y la app no puede escribir).
  - [ ] `.env.ejemplo`: sumar las tres variables `ADMIN_INICIAL_*` con la contraseña vacía y un
        comentario de cómo usarlas, sin quitar lo que puso 1.1.
- [ ] [SEQ] Task 8: Pruebas de Web y verificación manual (AC: todos)
  - [ ] `SinParalelismo.cs`, `PersonalWebApplicationFactory`, `FormularioHttp`, T10–T22 y T24–T28 en verde.
  - [ ] Suite completa (`dotnet test`) y `bash scripts/ci/local.sh` sin regresiones (AC 23).
  - [ ] Verificación manual en el navegador (Como verificar, paso 4) y su resultado en el Dev Agent Record.

### Task Dependencies

T1 → T2 → T3 → T4 → T5 → T6 → T8 (secuencial)
T7 puede hacerse en paralelo con T4–T6.

## Dev Notes

### Que existe hoy

#### En hospital-turnos (verificado 2026-10-09)

- Sólo el código descartable `src/HospitalTurnos.Api/` y `tests/HospitalTurnos.Tests/`, listado en
  `HospitalTurnos.slnx`. **Lo borra la story 1.1.** Nada de eso se usa.
- `.gitignore:7` ignora `.env` (por eso las contraseñas viven ahí).
- **No existe** nada de lo que lista `files_to_create`. Lo que esta story **usa** y crean otras:
  - `HospitalDbContext`, `Persistencia/Migraciones/`, `docker-compose.yml`, `Dockerfile`, `.env.ejemplo`,
    `Program.cs` de Web (con `UseForwardedHeaders` y HSTS), `Routes.razor`, `_Imports.razor`, la
    convención `snake_case`, la `PostgresFixture` de Web.Tests y `HospitalWebApplicationFactory` —
    **lo crea la story 1.1**.
  - `RelojHospital` (como `TimeProvider`), `ManejadorGlobalDeErrores` y `RegistroEnMemoria` — **lo
    crea la story 1.2**.
  - `RegistroAuditoria`, `CanalAuditoria` (con `Personal`), `AccionesAuditoria`, `IAuditoria`, los
    permisos de `turnos_app` sobre `registros_auditoria`, la `PostgresFixture` de Aplicacion.Tests
    (copia idéntica de la de 1.1) y la colección `"Postgres"` — **lo crea la story 1.3**.
  - La CI (`scripts/ci/local.sh` y `.github/workflows/ci.yml`) — **la crea la story 1.5**.
- Nombres fijados por el contrato (`docs/stories/00-contrato-tecnico.md`, §Personal y roles):
  `UsuarioPersonal : IdentityUser<Guid>` en `Infraestructura/Identidad/` con `NombreCompleto` y
  `Activo`; roles en `Dominio/Personal/Roles.cs`; esquema de cookie `"Personal"`.

#### En el framework (.NET 10.0.11, verificado con `ilspycmd` sobre `~/.dotnet/shared/Microsoft.AspNetCore.App/10.0.11/`)

- `Microsoft.AspNetCore.Authentication.dll`: `PostConfigureAuthenticationSchemeOptions` asigna a cada
  esquema el `TimeProvider` del contenedor si no tiene uno, y `AddAuthentication` hace
  `TryAddSingleton(TimeProvider.System)`. O sea: la cookie **ya** usaría el reloj del hospital; igual
  se asigna explícito en `SesionPersonal` para que la prueba no dependa del orden de registro.
- `Microsoft.Extensions.Identity.Core.dll`: `UserManager.IsLockedOutAsync` compara
  `LockoutEnd >= DateTimeOffset.UtcNow` y el bloqueo se fija con
  `DateTimeOffset.UtcNow.Add(Options.Lockout.DefaultLockoutTimeSpan)` (`DefaultLockoutTimeSpan` por
  defecto = 5 min). **Identity no usa `TimeProvider`.** Es código del framework, no nuestro: no rompe
  la regla de NFR-06 del contrato, pero obliga a probar el bloqueo vencido escribiendo `LockoutEnd`
  (T4) en lugar de adelantar el reloj.
- El `SecurityStampValidator` de Identity necesita `SignInManager` (que no se registra, ver "Lo que
  NO hay que hacer") y sólo está cableado al esquema `Identity.Application`. Por eso la revalidación
  del AC 25 es propia (`ValidadorSesionPersonal`), con la misma idea: comparar el sello del claim con
  el de la base cada cierto intervalo.

### Patron de referencia verificado

**Patrón 1 — fábrica de pruebas de integración** (proyecto guía de la materia):

| Proyecto | Ancla `archivo:línea` | Qué se verificó exactamente |
|---|---|---|
| changomas-testing-csharp | `tests/ChangoMas.Tests.Integracion/CajaWebApplicationFactory.cs:20` | `public class CajaWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime`. `HospitalWebApplicationFactory` (1.1) tiene la misma forma y `PersonalWebApplicationFactory` hereda de ella. |
| changomas-testing-csharp | `.../CajaWebApplicationFactory.cs:28-32` | `InitializeAsync` arranca la dependencia externa (ahí, WireMock). Acá la base la da `PostgresFixture` (1.1) y la fábrica de 2.1 siembra los 3 usuarios fijos. |
| changomas-testing-csharp | `.../CajaWebApplicationFactory.cs:34-39` | `ConfigureWebHost` cambia la configuración y el entorno (`UseEnvironment("Testing")`, línea 38). Acá además `ConfigureTestServices` para el `FakeTimeProvider` y los dobles (y en T15 para reemplazar `IIngresoPersonal`). |
| changomas-testing-csharp | `.../CajaWebApplicationFactory.cs:41-48` | `DisposeAsync` libera la dependencia externa y llama a `base.DisposeAsync()`. |
| changomas-testing-csharp | `tests/ChangoMas.Tests.Integracion/ApiCajaTests.cs:18` y `:27` | Uso: `public class ApiCajaTests(CajaWebApplicationFactory fabrica) : IClassFixture<CajaWebApplicationFactory>` y `fabrica.CreateClient()`. Acá, `CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false })`. |

**Patrón 2 — Identity, cookie, formulario SSR y antifalsificación: no está en el guía (opción B).**
`grep -rln -E "Identity|Antiforgery|AddCookie|bunit|Bunit" --include='*.cs' --include='*.csproj' --include='*.razor' ~/SourceCode/changomas-testing-csharp`
→ 0 resultados (el guía es una API sin usuarios ni Blazor). La referencia es la **plantilla oficial**
del SDK instalado, generada con `dotnet new blazor --auth Individual -n Tpl --no-restore` (SDK 10.0.400):

| Archivo de la plantilla | Línea | Qué se verificó y qué se toma |
|---|---|---|
| `Program.cs` | 14, 18, 30, 36, 57 | `AddCascadingAuthenticationState()`, `AddAuthentication(...)`, `AddIdentityCore<ApplicationUser>(...)`, `.AddSignInManager()`, `app.UseAntiforgery()`. Se toman `AddCascadingAuthenticationState`, `AddIdentityCore` y `UseAntiforgery`. **No** se usan `AddIdentityCookies` ni `AddSignInManager`: el contrato fija el esquema `"Personal"` y la cookie se emite en Web con `HttpContext.SignInAsync("Personal", ...)`. |
| `Components/Account/Pages/Login.razor` | 21 | `<EditForm ... method="post" OnSubmit="LoginUser" FormName="login">`: formulario SSR estático. Se toma con `FormName="ingreso"`. |
| `Components/Account/Pages/Login.razor` | 81 | `[SupplyParameterFromForm]` sobre el modelo del formulario. Se toma (por eso los campos se llaman `Input.Usuario` / `Input.Contrasena`). |
| `Components/Account/Pages/Login.razor` | 124, 138 | `PasswordSignInAsync(..., lockoutOnFailure: false)` y rama `result.IsLockedOut`, que muestra un mensaje distinto. Acá el bloqueo **sí** está activo (FR-09 + pedido del docente: 5 intentos, 15 min), la verificación se hace con `UserManager` (ver Implementación concreta) y el bloqueo **no** se muestra distinto (AC 3). |
| `Components/Account/IdentityComponentsEndpointRouteBuilderExtensions.cs` | 44-49 | `accountGroup.MapPost("/Logout", async (..., [FromForm] string returnUrl) => { await signInManager.SignOutAsync(); ... })`. Se toma la forma: `MapPost` con un parámetro `[FromForm]`, que hace que `UseAntiforgery` valide el token (AC 15). |

**Patrón 3 — Testcontainers: no está en el guía (opción B).** El mismo `grep` con
`testcontainers` sobre el guía → 0 resultados (usa SQLite en archivo,
`CajaWebApplicationFactory.cs:22-23`). Se usa la `PostgresFixture` única que fija el contrato (§Pruebas).

### Que hay que hacer (la delta)

1. Identity "núcleo" (sin sus pantallas ni su cookie) sobre `HospitalDbContext`, con `UsuarioPersonal`,
   roles y la política del AC 21.
2. Un adaptador `IngresoPersonalIdentity` que decide el resultado del ingreso y audita, y una página
   S0 que, si el resultado es `Exitoso`, emite la cookie `"Personal"`.
3. Sesión de 30 minutos deslizante, revalidada contra la base cada 5 minutos, con la ruta de ingreso
   y la de acceso denegado; cookie `Secure` fuera de Development y claves persistentes.
4. Layout del personal con menú por rol y 11 secciones de relleno protegidas por rol.
5. Salida con POST protegido.
6. Siembra del administrador inicial desde configuración, sólo con la base vacía.

**Diferencias clave vs la plantilla oficial:**
- Plantilla: `SignInManager.PasswordSignInAsync` + esquema `Identity.Application`. Acá: `UserManager`
  paso a paso + esquema `"Personal"` (contrato), porque el paciente tendrá su propio esquema en el
  sprint 5 y porque así la lógica de ingreso queda probada sin HTTP (T1–T5, T23).
- Plantilla: bloqueo apagado y mensaje propio de bloqueo. Acá: encendido, 5 intentos, 15 minutos, y el
  mismo mensaje genérico que cualquier fallo.
- Plantilla: ingreso por email. Acá: por nombre de usuario (`User.RequireUniqueEmail = false`).

### Decisiones de esta story (con su porqué)

- **Por qué se audita el ingreso** (FR-33 no lo pide: habla de cambios de turno). Como el bloqueo no
  se muestra en pantalla (AC 3), la auditoría (pantalla A5, story 9.1) es **la única forma** de que
  el administrador sepa que alguien quedó bloqueado o que una cuenta recibe intentos. Por eso se
  auditan el ingreso exitoso, los fallos de usuarios **existentes** y los bloqueos. El canal es
  `CanalAuditoria.Personal`, no `Recepcion`: un médico o un administrador que ingresa no opera "por
  la recepción", y la métrica CE-01 (turnos por canal) no se ensucia con ingresos.
- **Por qué el usuario inexistente no se audita** (revisión I-2): `registros_auditoria` es inmutable
  (1.3). Es común escribir la contraseña en el campo usuario; guardarlo dejaría una contraseña en un
  registro legal que ni el dueño puede corregir, y cualquiera con acceso a la pantalla haría crecer
  la tabla sin límite. Va al `ILogger` sin el texto tipeado.
- **Por qué el bloqueo no se muestra** (revisión I-3): si 5 intentos con un usuario real dicen
  "bloqueado" y con uno inexistente nunca, la pantalla revela qué usuarios existen. Costo aceptado:
  quien quedó bloqueado cree que se equivocó; a los 15 minutos vuelve a poder, o le pide al
  administrador que lo desbloquee (2.2), que lo ve en A5.
- **Revalidación de la sesión en 2.1, no en 2.2** (revisión I-6). Sin ella, cuando 2.2 desactive a
  alguien, su sesión seguiría válida mientras la use (vencimiento deslizante: indefinido). Se hace
  acá porque es barato (un `CookieAuthenticationEvents` y una consulta por usuario cada 5 minutos) y
  porque es código de la cookie, que es de esta story: dejarlo para 2.2 obligaría a reabrir
  `SesionPersonal`. Costo aceptado: hasta 5 minutos de demora entre desactivar y cortar.
- **Riesgos aceptados** (revisión M-13 e I-2): sin límite por dirección IP, cualquiera en la red del
  hospital puede bloquear 15 minutos a un usuario cuyo nombre conozca con 5 intentos; y los intentos
  contra un usuario existente bloqueado dejan una fila de auditoría cada uno. Se aceptan porque el
  personal entra desde la red del hospital (Arq. §7.6 pide el límite por IP para el paciente, no
  para el personal) y porque esas filas son justamente la evidencia del ataque.

### Implementacion concreta

**Secuencia de `IngresoPersonalIdentity.ValidarAsync(usuario, contrasena, ct)`** — el orden importa:

```csharp
// 1. Buscar. Si no existe: verificar contra un hash ficticio (mismo costo PBKDF2, para que el
//    tiempo de respuesta no delate que el usuario no existe), registrar en el ILogger SIN lo tipeado
//    y devolver CredencialesInvalidas. NO se audita (registros_auditoria es inmutable).
var u = await _usuarios.FindByNameAsync(usuario);
if (u is null)
{
    _hasher.VerifyHashedPassword(UsuarioFicticio, HashFicticio, contrasena); // resultado ignorado
    _log.LogWarning("Ingreso del personal fallido: el usuario no existe.");
    return ResultadoIngreso.CredencialesInvalidas();
}
// HashFicticio: se calcula una vez (Lazy) con el mismo IPasswordHasher y una frase fija del
// código; no es una credencial de nada.
// 2. Actor de auditoría: Quien = NombreCompleto; Rol = su único rol, "(sin rol)" o "(varios roles)";
//    Canal = Personal; EntidadTipo = "UsuarioPersonal"; EntidadId = UsuarioId = u.Id.ToString().
// 3. Si está bloqueado, ni siquiera miramos la contraseña: auditar Bloqueado
//    ({"motivo":"intento-durante-bloqueo"}) y devolver Bloqueado.
if (await _usuarios.IsLockedOutAsync(u)) ...
// 4. Contraseña mala: AccessFailedAsync suma 1 y, al 5.º, fija LockoutEnd.
if (!await _usuarios.CheckPasswordAsync(u, contrasena))
{
    await _usuarios.AccessFailedAsync(u);
    // si ahora IsLockedOutAsync(u) → auditar IngresoPersonalBloqueado ({"motivo":"bloqueo"}), devolver Bloqueado
    // si no → auditar IngresoPersonalFallido ({"motivo":"contrasena-incorrecta"}), devolver CredencialesInvalidas
}
// 5. Contraseña buena: recién ahora informamos desactivado / sin rol / varios roles (sólo a quien la conoce).
// 6. Éxito: ResetAccessFailedCountAsync, armar el principal con IUserClaimsPrincipalFactory
//    (ya trae NameIdentifier, Role y el claim del sello de seguridad) y agregarle nombre_completo;
//    auditar IngresoPersonal.
// 7. _auditoria.Registrar(...) sólo AGREGA (story 1.3): cerrar con _db.SaveChangesAsync(ct)
//    ANTES de devolver, así la cookie nunca se emite si la auditoría no se guardó (AC 9).
```

La página S0 traduce `Bloqueado` y `CredencialesInvalidas` al **mismo** texto (AC 3).

**Cookie, reloj, seguridad y claves (en `SesionPersonal`)**:

```csharp
public static readonly TimeSpan IntervaloRevalidacion = TimeSpan.FromMinutes(5); // AC 25

services.AddScoped<ValidadorSesionPersonal>();
services.AddAuthentication(o => o.DefaultScheme = "Personal")
    .AddCookie("Personal", o =>
    {
        o.Cookie.Name = ".HospitalTurnos.Personal";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Strict;
        // NFR-03: detrás de un proxy que termina TLS el pedido llega por HTTP; igual la cookie
        // tiene que viajar sólo cifrada. En Development se deja probar con http://localhost.
        o.Cookie.SecurePolicy = entorno.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        o.ExpireTimeSpan = TimeSpan.FromMinutes(30);   // NFR-04, S-24
        o.SlidingExpiration = true;                    // "sin uso", no "desde el ingreso"
        o.LoginPath = "/personal/ingreso";
        o.AccessDeniedPath = "/personal/acceso-denegado";
        o.LogoutPath = "/personal/salir";
        o.EventsType = typeof(ValidadorSesionPersonal);
    });
// Por qué explícito: la prueba de vencimiento (T16) necesita que la cookie lea el mismo
// reloj que el resto del sistema, sin depender del orden en que se registraron los servicios.
services.AddOptions<CookieAuthenticationOptions>("Personal")
    .Configure<TimeProvider>((o, reloj) => o.TimeProvider = reloj);

// AC 27: sin esto, cada reconstrucción del contenedor genera claves nuevas y corta todas las
// sesiones y los formularios abiertos.
var proteccion = services.AddDataProtection().SetApplicationName("HospitalTurnos");
var carpeta = configuration["ProteccionDeDatos:Carpeta"];
if (!string.IsNullOrWhiteSpace(carpeta))
    proteccion.PersistKeysToFileSystem(new DirectoryInfo(carpeta));
```

Las claves quedan sin cifrar dentro del volumen (el framework lo avisa con una advertencia al
arrancar). Se acepta: el volumen sólo es accesible desde el servidor de Docker.

**`ValidadorSesionPersonal : CookieAuthenticationEvents`** (AC 25):

```csharp
public override async Task ValidatePrincipal(CookieValidatePrincipalContext contexto)
{
    var ahora = contexto.Options.TimeProvider!.GetUtcNow();
    var emitida = contexto.Properties.IssuedUtc;
    // Dentro del intervalo no se consulta la base: un pedido normal no paga la consulta.
    if (emitida is not null && ahora - emitida.Value < SesionPersonal.IntervaloRevalidacion) return;

    var id = contexto.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
    var sello = contexto.Principal?.FindFirstValue(_opcionesIdentity.ClaimsIdentity.SecurityStampClaimType);
    if (id is null || !await _ingreso.SesionVigenteAsync(id, sello, contexto.HttpContext.RequestAborted))
    {
        contexto.RejectPrincipal();                                   // el pedido sigue como anónimo → 302 al ingreso
        await contexto.HttpContext.SignOutAsync("Personal");          // y la cookie se vence en la respuesta
        return;
    }
    contexto.ShouldRenew = true;                                      // nueva IssuedUtc: próxima revisión en 5 min
}
```

`SesionVigenteAsync` (en `IngresoPersonalIdentity`): el usuario existe, `Activo == true`, tiene
exactamente un rol y su `SecurityStamp` es igual al del claim. Si la base no responde, la excepción
**no** se atrapa: el pedido termina en la página de error genérica de 1.2 (no se deja pasar a nadie
sin revisar).

Ojo con el deslizamiento: la cookie se **renueva** recién cuando pasó más de la mitad de la ventana
(15 min) o cuando la revalidación pide `ShouldRenew`. Por eso T17 usa pasos de 20 minutos: con pasos
de 10 y sin revalidación no se renovaría y la prueba no demostraría nada.

**S0 al ingresar bien**: `await HttpContext.SignInAsync("Personal", resultado.Principal!, new AuthenticationProperties { IsPersistent = false })`
y después `NavigationManager.NavigateTo(destino)` (en SSR estático se traduce en `302`). `destino` =
`ReturnUrl` si `RutasPersonal.EsReturnUrlValido` (empieza con `/personal/`, no con `//`), si no
`RutasPersonal.InicioDe(rol)`.

**Sembrador — "sin configuración"** (AC 18): `string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(Contrasena)`.
Orden: crear roles faltantes → si ya hay usuarios, terminar (AC 17, sin validar nada) → si no hay
configuración, advertencia y terminar (AC 18) → validar la contraseña con los `IPasswordValidator`
de Identity; si falla, `InvalidOperationException` (AC 19) → crear el administrador.

### Salvaguarda de BD (obligatoria antes de implementar las pruebas de Web)

**No** usar `dotnet ef database update` (story 1.1, "Lo que NO hay que hacer": usaría la cadena `App`
de `turnos_app`, sin `CREATE`, y falla con `42501`). Las migraciones las aplica **el arranque** de la
app con `ConnectionStrings:Migraciones` (rol dueño):

```bash
cp .env.ejemplo .env     # si no existe: completar las contraseñas de la base (1.1); ADMIN_INICIAL_* puede quedar vacío
docker compose up -d --build     # la app aplica IdentidadPersonal al arrancar
```

Después, contra el PostgreSQL de Compose (mismo `01-roles.sh` que usa la `PostgresFixture` de las
pruebas, así que el resultado vale para las dos):

1. `docker compose exec db psql -U postgres -d hospital_turnos -c "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_name LIKE 'asp_net%' ORDER BY 1;"`
   → anotar los nombres reales (dependen de cómo 1.1 convierte a `snake_case`). Se esperan 7 tablas
   de Identity.
2. `SELECT has_table_privilege('turnos_app', '<tabla de usuarios del paso 1>', 'SELECT,INSERT,UPDATE,DELETE');`
   → tiene que dar `t` (lo dan los privilegios por defecto de `01-roles.sh`). Si da `f`, el script de
   roles de 1.1 no se aplicó como debe: **no** taparlo con `GRANT` en la migración; detenerse.
3. `SELECT has_table_privilege('turnos_app', 'registros_auditoria', 'UPDATE');` → tiene que seguir
   dando `f` (AC 23).
4. Si CUALQUIER resultado no coincide → DETENERSE → corregir la story primero.

### Archivos a crear/modificar

| Acción | Archivo |
|---|---|
| Crear | Los de `files_to_create` del frontmatter |
| Modificar | `src/HospitalTurnos.Infraestructura/Persistencia/HospitalDbContext.cs` (hereda de `IdentityDbContext<...>`; `base.OnModelCreating` primero) |
| Modificar | `src/HospitalTurnos.Dominio/Auditoria/AccionesAuditoria.cs` (3 constantes, AC 22) |
| Modificar | `src/HospitalTurnos.Dominio/Auditoria/CanalAuditoria.cs` (sólo si 1.3 no trae `Personal`; Task 1) |
| Modificar | `src/HospitalTurnos.Web/Program.cs` (registro, pipeline, siembra) |
| Modificar | `src/HospitalTurnos.Web/Dockerfile` (carpeta de claves con dueño `$APP_UID`, AC 27) |
| Modificar | `src/HospitalTurnos.Web/Components/Routes.razor` (`AuthorizeRouteView`) |
| Modificar | `src/HospitalTurnos.Web/Components/_Imports.razor` (`Microsoft.AspNetCore.Authorization`, `Microsoft.AspNetCore.Components.Authorization`) |
| Modificar | `docker-compose.yml` (variables del administrador inicial, carpeta y volumen de claves; AC 20, 27) |
| Modificar | `.env.ejemplo` (las 3 variables `ADMIN_INICIAL_*`, AC 20) |

### Lo que NO hay que hacer

- **NO implementar el cambio de contraseña del primer ingreso ni la gestión de usuarios (A4)**: son
  de la story 2.2. Acá `/personal/admin/usuarios` es una sección de relleno.
- **NO usar `AddIdentity` ni `AddDefaultIdentity` ni `AddIdentityCookies`**: registran los esquemas
  `Identity.Application` y compañía, y las pantallas de Identity; el contrato fija el esquema
  `"Personal"` y la cookie del paciente llegará en el sprint 5 con el suyo.
- **NO poner ninguna contraseña en el repo, ni de desarrollo** (contrato §Secretos): ni en
  `appsettings*.json`, ni en `docker-compose.yml` (sin valores por defecto en `${...:-}`), ni en
  `.env.ejemplo`. Van en `.env`, que está en `.gitignore`. Las de las pruebas viven en el código de
  las pruebas y no abren ningún ambiente.
- **NO usar `dotnet ef database update`**: falla con `turnos_app` (ver Salvaguarda de BD).
- **NO distinguir en el mensaje "usuario inexistente", "contraseña incorrecta" ni "bloqueado"**:
  revelaría qué usuarios existen (AC 2 y 3).
- **NO guardar lo que la persona tipeó** en `registros_auditoria` ni en el `ILogger` (ni usuario
  inexistente ni contraseña), tampoco cuando falla la siembra.
- **NO pasar `null` ni `""` en `Rol`, `EntidadTipo` ni `EntidadId`**: el constructor de
  `RegistroAuditoria` (1.3) lanza `ArgumentException` y el ingreso terminaría en el 500 genérico. Usar
  los valores de la tabla del inicio de los AC.
- **NO envolver el ingreso ni `NavigateTo` en `try/catch (Exception)`**: en SSR estático `NavigateTo`
  se implementa con una excepción de navegación que un `catch` genérico se traga (no habría `302`), y
  los errores de base tienen que llegar al `ManejadorGlobalDeErrores` de 1.2 (AC 9).
- **NO hacer interactiva la página S0** (`@rendermode InteractiveServer`): la cookie se escribe en la
  respuesta HTTP y en un circuito interactivo no hay respuesta HTTP que escribir. El layout y las
  secciones de relleno también quedan estáticos; cada story que haga una pantalla interactiva
  (desde la 5.5) pone el modo de renderizado en **su** página.
- **NO renovar la sesión desde un circuito interactivo** en esta story: hoy no hay pantallas
  interactivas. Cuando aparezcan (5.5), "sin uso" y la revalidación del AC 25 deberán incluir la
  actividad del circuito; queda anotado para esa story.
- **NO llamar a `DateTime.Now`/`UtcNow` en nuestro código** (contrato §Hora). Lo de Identity es del
  framework y se documenta arriba; la revalidación usa `Options.TimeProvider`.
- **NO limitar intentos por dirección IP**: la arquitectura (§7.6) lo pide para el paciente, no para
  el personal (riesgo aceptado, ver Decisiones).
- **NO correr las pruebas de Web.Tests en paralelo** ni contar filas de auditoría sin filtrar por un
  id propio del test (base compartida).
- **NO crear el proyecto E2E**: no existe en el sprint 1 (contrato).

### Como verificar que funciona

```bash
cd ~/SourceCode/hospital-turnos

# 1. Migración (proyecto de migraciones = Infraestructura, arranque = Web). No se conecta a la base;
#    si el diseño de EF pide las cadenas de conexión, darlas como indica la story 1.1.
dotnet ef migrations add IdentidadPersonal \
  --project src/HospitalTurnos.Infraestructura --startup-project src/HospitalTurnos.Web \
  --output-dir Persistencia/Migraciones
# La migración se APLICA al arrancar la app (no con dotnet ef database update): ver Salvaguarda de BD.

# 2. Pruebas de esta story (necesita Docker para Testcontainers)
dotnet test --filter "Story=2.1"
# Esperado: 28 pruebas declaradas (más los casos de cada [Theory]), 0 fallas
dotnet test --filter "Story=2.1&AC=10"      # sólo el vencimiento de sesión

# 3. Suite completa y CI local (AC 23)
dotnet test
bash scripts/ci/local.sh; echo "rc=$?"     # rc=0

# 4. Verificación manual en el navegador (en lugar del E2E del sprint 1)
cp .env.ejemplo .env      # completar contraseñas de la base, ADMIN_INICIAL_USUARIO=admin y una ADMIN_INICIAL_CONTRASENA de 10+ caracteres
docker compose down -v && docker compose up -d --build
open http://localhost:8080/personal/admin/medicos
# Esperado, en este orden:
#  a. redirige a /personal/ingreso?ReturnUrl=%2Fpersonal%2Fadmin%2Fmedicos
#  b. con una contraseña mala: "Usuario o contraseña incorrectos." y el usuario sigue escrito
#  c. con la buena: entra a "Médicos" con el texto de sección en construcción y un menú de 8 secciones
#     en el orden del AC 14
#  d. "Salir" vuelve al ingreso; el botón Atrás del navegador no muestra la sección (pide ingreso)
#  e. con un usuario que no existe: el mismo "Usuario o contraseña incorrectos."
#  f. 5 contraseñas malas seguidas con admin: sigue el mismo mensaje (el bloqueo NO se muestra);
#     con la buena tampoco entra (bloqueado 15 min)
#  g. docker compose up -d --build otra vez (sin down -v) con una sesión abierta: la sesión sigue (AC 27)
docker compose logs web | grep -i "administrador inicial"   # sin la contraseña

# 4b. Clon limpio sin .env completo (AC 18): ADMIN_INICIAL_* vacíos y base nueva
docker compose down -v && docker compose up -d --build
docker compose logs web | grep "No hay usuarios del personal"   # la advertencia; /salud responde 200

# 5. La auditoría quedó escrita (canal Personal; el usuario inexistente no aparece)
docker compose exec db psql -U postgres -d hospital_turnos \
  -c "SELECT cuando, quien, rol, canal, accion, entidad_id, detalle FROM registros_auditoria ORDER BY id DESC LIMIT 10;"
```

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

### Change Log

- 2026-10-09: Story escrita (Mary).
- 2026-10-09: Correcciones de la revisión adversarial del sprint 1: C-1 (una sola `PostgresFixture`,
  no se crea ni se modifica), C-2 (campos obligatorios de auditoría por caso), C-3 (sin
  `dotnet ef database update`; base `hospital_turnos`), I-1 (`CanalAuditoria.Personal`), I-2 (el
  usuario inexistente no se audita), I-3 (bloqueo con mensaje genérico, hash ficticio, T24), I-4
  (configuración vacía = sin configuración; T8, T25), I-5 (T15 con `Accept: text/html`), I-6
  (revalidación de sesión, AC 25, T26), I-7 (cookie `Secure`, AC 26, T27), I-12 (`depends_on` 1.5),
  I-16 (sin contraseñas versionadas), I-17 (Web.Tests sin paralelismo y usuarios propios por test),
  M-4, M-6, M-7, M-11 (claves persistentes, AC 27, T28), M-13, M-15 (un rol por usuario, AC 24),
  M-16. 27 AC, 28 verificaciones.
