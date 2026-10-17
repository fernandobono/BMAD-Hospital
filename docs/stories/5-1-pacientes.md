---
epic: 5
story: "5.1"
title: "Pacientes: búsqueda por DNI, alta y edición (parte PACIENTE de la pantalla R1)"
status: ready-for-dev
sprint: 3
priority: "P0"
approach: "[CREATE]"
depends_on: ["1.2", "1.3", "2.1", "2.2", "3.1", "3.2", "4.2"]
files_to_create:
  - "src/HospitalTurnos.Dominio/Pacientes/ReglaDni.cs"
  - "src/HospitalTurnos.Dominio/Pacientes/Paciente.cs"
  - "src/HospitalTurnos.Aplicacion/Pacientes/IGestionPacientes.cs"
  - "src/HospitalTurnos.Infraestructura/Pacientes/GestionPacientesEnBase.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/PacienteConfiguracion.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/<marca>_AgregarPacientes.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/<marca>_AgregarPacientes.Designer.cs"
  - "src/HospitalTurnos.Web/Components/Personal/Recepcion/TurnosRecepcion.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Recepcion/PanelPaciente.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Recepcion/CamposPaciente.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Recepcion/FormularioPaciente.cs"
  - "tests/HospitalTurnos.Dominio.Tests/Pacientes/ReglaDniTests.cs"
  - "tests/HospitalTurnos.Dominio.Tests/Pacientes/PacienteTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Pacientes/DatosDePacientes.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Pacientes/GestionPacientesEnBaseTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Pacientes/PersistenciaPacientesTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Pacientes/DatosDePacientes.cs"
  - "tests/HospitalTurnos.Web.Tests/Pacientes/PacientesHttpTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Pacientes/PanelPacienteTests.cs"
files_to_modify:
  - "src/HospitalTurnos.Dominio/Auditoria/AccionesAuditoria.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/HospitalDbContext.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/HospitalDbContextModelSnapshot.cs"
  - "src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs"
  - "src/HospitalTurnos.Web/Personal/RutasPersonal.cs"
  - "src/HospitalTurnos.Web/Components/_Imports.razor"
  - "tests/HospitalTurnos.Web.Tests/Personal/SesionYPermisosHttpTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Personal/ActorVigenteIdentityTests.cs"
files_to_delete:
  - "src/HospitalTurnos.Web/Components/Personal/Secciones/SeccionesRecepcion.razor"
---

# Story 5.1: Pacientes: búsqueda por DNI, alta y edición (parte PACIENTE de la pantalla R1)

Status: ready-for-dev

## Story

Como recepcionista del Hospital Las Acacias (Carla),
quiero buscar a un paciente por su DNI, darlo de alta en el momento si no existe y corregir sus datos,
para tener a la persona cargada antes de darle un turno por teléfono o en la ventanilla (RU-03) sin
pasar por la planilla y sin cargar dos veces al mismo paciente (R-08).

## Acceptance Criteria

> Pantalla **R1** de `docs/03-ux.md` §5, **sólo la columna PACIENTE** (FR-01, FR-02, FR-03). Los
> "turnos futuros" del paciente llegan con la 5.3 y la columna NUEVO TURNO y el manejo con teclado,
> con la 5.2 y la 5.5. Ruta `/personal/recepcion/turnos` (ya definida en `RutasPersonal.Turnos` por
> la 2.1, inicio del rol y primera entrada del menú de la recepción), sólo para el rol
> `Recepcionista` (decisión 19). La página es de **renderizado estático en el servidor**
> (SSR: el servidor arma el HTML y el navegador envía formularios comunes), como A1 y A4; la 5.5 la
> vuelve interactiva. Mensajes en castellano rioplatense.
>
> **Campos de auditoría** (valen para los AC 6 y 12; cumplen los campos obligatorios de la 1.3). El
> actor es el que devuelve `IActorVigente` (leído de la base, no de la sesión, AC 17):
>
> | Campo | Valor |
> |---|---|
> | `UsuarioId` | id del recepcionista que opera |
> | `Quien` | su nombre completo, leído de la base |
> | `Rol` | `Recepcionista` |
> | `Canal` | `CanalAuditoria.Personal` (acción del personal que **no** es un cambio de turno: contrato técnico §Auditoría; `Recepcion` queda para los turnos) |
> | `EntidadTipo` | `"Paciente"` |
> | `EntidadId` | id del paciente (`long` como texto) |
> | `Detalle` | alta: `{"dni","nombre","apellido","telefono","email"}` con los valores guardados (`email` `null` si no tiene); edición: `{"dni":"…","cambios":{"<campo>":{"antes":"…","despues":"…"}}}` sólo con los campos que cambiaron. El `dni` va siempre: la 9.1 filtra la auditoría por DNI (FR-34) |

### Búsqueda por DNI — FR-01

1. **Given** un recepcionista ingresado **When** pide `/personal/recepcion/turnos` **Then** recibe
   `200` con `<h1>Turnos</h1>` (exacto, sin atributos: lo compara la T19 de la 2.1), un formulario con
   la etiqueta visible `DNI del paciente` (NFR-05), el campo con el foco puesto (`autofocus`: "el cursor
   arranca en el DNI", UX R1), el botón `Buscar` (Enter lo envía, como todo formulario) y, sin
   paciente cargado, el texto `Buscá al paciente por su DNI para ver sus datos o darlo de alta.`
   (estado vacío, UX §8). El texto `Esta sección se habilita en una próxima versión.` ya no aparece.
2. **Cómo se escribe el DNI** (decisión 20): se aceptan puntos y espacios (`23.456.789`,
   `23 456 789`) y se quitan, igual que los ceros adelante (`07123456` → `7123456`); lo que queda tiene
   que ser **7 u 8 números**. **Given** un DNI vacío **Then** `200` con `Ingresá el DNI.` debajo del
   campo. **Given** otro valor que no cumple (`123456`, `123456789`, `12a45678`, `23-456-789`) **Then**
   `200` con `El DNI tiene 7 u 8 números.` debajo del campo (texto de UX §8) y lo escrito sigue en el
   campo. En los dos casos no se busca nada. La regla vive en el dominio (`ReglaDni`, AC 10) y la usan la
   pantalla y el caso de uso: un POST armado a mano da lo mismo (NFR-10).
3. **Given** un paciente con DNI `23456789` **When** se busca `23.456.789` **Then** la respuesta es
   `302` a `/personal/recepcion/turnos?paciente={id}`: el DNI **no** viaja en la dirección
   (decisión 23). La búsqueda se envía por POST con token antifalsificación.
4. **Given** `/personal/recepcion/turnos?paciente={id}` de un paciente existente **Then** la columna
   **PACIENTE** muestra, como el boceto de UX R1:
   - `<Nombre> <Apellido> · DNI 23.456.789` (el DNI con puntos de miles, `ReglaDni.ParaMostrar`);
   - `Tel. <teléfono> · <email>`, o `Tel. <teléfono> · Sin email` si no tiene (la recepción tiene que
     saberlo: a ese paciente los avisos le llegan por teléfono, 7.2);
   - el enlace `Editar datos` a `/personal/recepcion/turnos?paciente={id}&editar=si`.

   El buscador sigue arriba, vacío y con el foco, para el siguiente paciente. No se muestra ninguna
   sección de turnos (la agrega la 5.3, AC 20).
5. **Given** que no hay ningún paciente con el DNI buscado (válido) **When** se busca **Then** `200`
   con `No hay ningún paciente con el DNI 23.456.789. Completá sus datos para darlo de alta.` en un
   elemento `role="status"` (lo anuncia el lector de pantalla, UX §9) y, en el mismo lugar, el
   formulario de alta con el DNI ya cargado, normalizado (UX R1: "se abre el alta en el mismo lugar
   con el DNI ya cargado").

### Alta — FR-02, R-08

6. **Given** el formulario de alta con `DNI`, `Nombre`, `Apellido`, `Teléfono` y `Email (opcional)`,
   cada uno con etiqueta visible, **When** se envía con datos válidos **Then** se guarda el paciente
   normalizado (DNI sólo con números; nombre y apellido sin espacios a los costados; teléfono sin
   espacios a los costados y con los internos repetidos reducidos a uno; email en minúsculas y sin
   espacios, o sin email si quedó vacío), se guarda **en la misma transacción** un registro
   `AccionesAuditoria.PacienteCreado` con los campos de la tabla de arriba, y la respuesta es `302` a
   `/personal/recepcion/turnos?paciente={id}&resultado=alta`, donde se lee `Paciente dado de alta.` en
   un elemento `role="status"` y los datos del AC 4.
7. **Given** un campo inválido **When** se envía el alta **Then** `200` con el mensaje debajo de cada
   campo, lo escrito sigue en los campos y **no** se guarda ni se audita nada. La validación corre en
   la pantalla y en el servidor (NFR-10) y el dominio la repite (`Paciente` no nace inválido aunque se
   llame al caso de uso sin pasar por la pantalla):

   | Caso | Mensaje |
   |---|---|
   | DNI vacío / no cumple el AC 2 | `Ingresá el DNI.` / `El DNI tiene 7 u 8 números.` |
   | Nombre vacío / más de 100 | `Ingresá el nombre.` / `El nombre tiene como máximo 100 caracteres.` |
   | Apellido vacío / más de 100 | `Ingresá el apellido.` / `El apellido tiene como máximo 100 caracteres.` |
   | Teléfono vacío | `Ingresá el teléfono.` |
   | Teléfono con otros caracteres, con menos de 6 o más de 15 números, o de más de 20 caracteres (decisión 21) | `El teléfono tiene entre 6 y 15 números; puede llevar espacios, guiones, paréntesis o un + adelante.` |
   | Email con forma inválida o de más de 254 caracteres | `El email no es válido. Revisalo o dejalo vacío.` |

8. **Given** que ya existe un paciente con DNI `23456789` **When** se envía un alta con `23.456.789`
   (mismo DNI normalizado) **Then** `409` con
   `Ya hay un paciente con el DNI 23.456.789. Buscalo por DNI para ver sus datos.` en un cartel
   `role="alert"` arriba del formulario (UX §8, "Error de regla de negocio"), los campos conservan lo
   escrito y no se guarda ni audita nada.
9. **Given** dos altas **simultáneas** con el mismo DNI (dos puestos de recepción, o un doble clic)
   **Then** el índice único de la base deja entrar una sola y la otra recibe **el mismo** `409` con el
   mismo texto del AC 8 (nunca un `500`), sin registro de auditoría del alta perdida.
10. **Persistencia.** Tabla `pacientes` con `id` (`bigint` identity, como toda entidad propia), `dni`
    (`varchar(8)`, `NOT NULL`, índice único **`ix_pacientes_dni`**), `nombre` y `apellido`
    (`varchar(100)`, `NOT NULL`), `telefono` (`varchar(20)`, `NOT NULL`) y `email` (`varchar(254)`,
    nulo, **sin** índice único: dos pacientes pueden compartir un email, decisión 22).
    `turnos_app` tiene `SELECT`, `INSERT` y `UPDATE` y **no** `DELETE` (los pacientes no se borran;
    el `UPDATE` también lo necesita el `SELECT … FOR UPDATE` de la 5.3, Arq. §7.2). La regla del DNI
    vive en `src/HospitalTurnos.Dominio/Pacientes/ReglaDni.cs` (`Normalizar`, `EsValido`, `ParaMostrar` y
    los dos mensajes), una sola vez para la recepción y para la web del paciente (6.1, 6.2).

### Edición — FR-03, S-18

11. **Given** el paciente cargado **When** se sigue `Editar datos`
    (`/personal/recepcion/turnos?paciente={id}&editar=si`) **Then** se ve, en la columna PACIENTE, el
    formulario con `Nombre`, `Apellido`, `Teléfono` y `Email (opcional)` cargados con los datos
    actuales, el DNI como texto (`DNI 23.456.789 (no se modifica)`, **sin** campo), el botón
    `Guardar cambios` y el enlace `Cancelar` a `/personal/recepcion/turnos?paciente={id}`.
12. **Given** cambios válidos **When** se guarda **Then** se actualiza el paciente, se audita
    `AccionesAuditoria.PacienteEditado` con `Detalle` = el `dni` y sólo los campos que cambiaron (claves
    `nombre`, `apellido`, `telefono`, `email`; valores como texto, `null` si no había) en la misma
    transacción, y la respuesta es `302` a `?paciente={id}&resultado=edicion` con `Cambios guardados.`
    en `role="status"`. **Given** que no cambió nada (también si sólo cambió el formato: el email en
    mayúsculas, espacios de más) **Then** no se guarda ni se audita, y el `302` va a
    `?paciente={id}&resultado=sin-cambios` con `No había cambios para guardar.` **Given** un campo
    inválido **Then** `200` con los mensajes de la tabla del AC 7 (salvo el DNI) y sin guardar.
13. **El DNI no se modifica** (FR-03, S-18). El formulario no lo tiene, el caso de uso de edición no
    lo recibe y `Paciente` no tiene ningún método que lo cambie. **Given** un POST de edición armado a
    mano con un campo `Input.Dni` distinto **Then** se ignora: el DNI guardado sigue igual y la
    auditoría no lo nombra entre los cambios.
14. **Given** un paciente que no existe **When** se pide `?paciente={id}` o `?paciente={id}&editar=si`
    (GET) o se envía su edición (POST) **Then** la respuesta es `404` con
    `No existe el paciente pedido. Buscalo por DNI.` en un cartel `role="alert"`, con el buscador
    arriba (nunca un `500`). Un `paciente` que no es un número entero (`?paciente=abc`) da **lo mismo**
    (no el `500` de una conversión fallida).
15. `?resultado=` se traduce con un `switch` de tres valores conocidos (`alta`, `edicion`,
    `sin-cambios`) a los textos de los AC 6 y 12; cualquier otro valor no muestra nada (nunca se
    muestra lo que venga en la consulta).

### Permisos — FR-11, DOM-01, Arq. §7.7

16. **Given** un usuario con rol `Administrador` o `Medico` **When** pide `/personal/recepcion/turnos`
    (con o sin `?paciente=`) **Then** la respuesta final es `403` con
    `No tenés permiso para entrar a esta sección.` (página de la 2.1; decisión 19).
    **Given** nadie ingresado **Then** `302` a `/personal/ingreso?ReturnUrl=<ruta>`.
17. El control también está **en el caso de uso** y se mira **en la base** (decisión 16 del S2: la
    sesión puede atrasar hasta 5 minutos), en las **cuatro** operaciones, también en las dos lecturas:
    son datos personales de salud y "sólo los ve quien los necesita" (DOM-01). **Given** un actor que
    no es un recepcionista activo en la base (otro rol, o un recepcionista desactivado que todavía tiene
    sesión) **When** llama a `BuscarPorDniAsync`, `ObtenerAsync`, `CrearAsync` o `EditarAsync` **Then**
    lanza `AccesoDenegadoException` con
    `Sólo la recepción puede consultar, dar de alta o modificar pacientes.` y no devuelve, guarda ni
    audita nada. En la página se ve como cartel con `403` (mecanismo de la 2.2, `FijarEstadoEnPagina`).
18. El control del AC 17 usa el método general de permiso por rol que **crea la 4.2** (su AC 21; I-3 de
    la revisión), sin crear otro:
    `IActorVigente.ExigirActivoConRolAsync(ActorAuditoria actor, string rol, string mensajeSiNoLoEs, CancellationToken ct)`,
    llamado con `Roles.Recepcionista`. Confirma en la base que el usuario existe, está activo y su
    **único** rol es `rol`, y devuelve el actor con `Quien` = nombre leído de la base, `Rol` = `rol` y
    canal `Personal`. Esta story no cambia `IActorVigente` ni `ActorVigenteIdentity`; sólo suma su prueba
    de la recepción (T16) al archivo `ActorVigenteIdentityTests.cs` que crea la 4.2.
19. **Given** un POST de búsqueda, de alta o de edición sin el token antifalsificación **Then**
    responde `400` y no se guarda ni se audita nada.

### Auditoría — FR-33, R-12

20. `AccionesAuditoria` suma `PacienteCreado = "paciente.creado"` y `PacienteEditado = "paciente.editado"`
    (estilo `<entidad>.<acción>` del contrato técnico; la entidad `paciente` ya existe en
    `PacienteMarcadoAvisado = "paciente.marcado-avisado"`). El registro y el cambio se guardan juntos o
    no se guarda ninguno (Arq. §7.5); `Cuando` sale del `TimeProvider` (la pone `AuditoriaEnBase`).
    Las búsquedas y las consultas de un paciente **no** se auditan: FR-33 lista sólo cambios
    (decisión 24).

### Contrato con otras stories

21. **Con pruebas asignadas desde ahora** (acción B-1 de la retrospectiva del S2): T1, T2, T4 y T18
    fijan lo que usa la 6.2; T19, lo que usa la 5.3; T21 y T33, lo que usan la 5.5 y la 7.3; T7 y T12,
    lo que lee la 9.1; T16, lo que usa de la 4.2. Lo que esta story **fija**:
    - **6.2 Registro por la web** (y 6.1, 6.6): usa **la misma** entidad `Paciente`, la misma tabla y
      la misma regla `ReglaDni` (`ReglaDni.Normalizar` + `ReglaDni.EsValido`, sin copiarla), y las mismas reglas de
      nombre, apellido, teléfono y email del constructor de `Paciente` (T4). Crea pacientes con su
      propio caso de uso, auditado con la **misma** constante `paciente.creado`, canal
      `CanalAuditoria.Web` y un actor sin `UsuarioId`. La carrera de dos registros con el mismo DNI la
      frena el mismo índice `ix_pacientes_dni` sin importar por qué puerta entró el alta (T18): la 6.2
      traduce ese `23505` como la T9 de esta story. FR-05 (DNI que ya existe sin email) y si un email
      cargado por la recepción cuenta como verificado los decide la 6.2; si agrega una columna, es su
      migración. La 6.1 busca al paciente por `ReglaDni.Normalizar` del DNI escrito. La 6.6 cambia el
      teléfono con `Paciente.CambiarDatos` (misma regla del teléfono).
    - **5.3 Reserva**: `Turno.PacienteId` es `long` con FK a `pacientes.id` (`RESTRICT`). La regla R-04
      bloquea la fila del paciente con `SELECT … FOR UPDATE` (Arq. §7.2): `turnos_app` ya puede hacerlo
      (T19). La 5.3 agrega la sección **TURNOS FUTUROS** debajo de `PanelPaciente` en
      `TurnosRecepcion.razor`; `PanelPaciente` no cambia.
    - **5.5 R1 completa**: la vuelve interactiva y manejable con teclado **reusando** `IGestionPacientes`
      (con sus cuatro firmas) y `PanelPaciente` (parámetro `DatosPaciente Paciente`, T33). Puede
      cambiar el mecanismo de búsqueda (sin URL) y el enlace `Editar datos`, no las reglas ni los
      mensajes.
    - **7.2 / 7.3 Avisos**: un paciente sin email tiene `Email == null` (7.2 lo manda a "Pacientes para
      avisar"). El botón "Darle turno" de R3 lleva a `RutasPersonal.Paciente(id)`
      (`/personal/recepcion/turnos?paciente={id}`), que carga al paciente (T21).
    - **9.1 Auditoría**: lee `paciente.creado` y `paciente.editado` con `EntidadTipo = "Paciente"` y
      el DNI en `detalle.dni` (T7, T12) para el filtro por DNI de FR-34.
    - **4.2 (sprint 3, va antes)**: esta story **usa** `IActorVigente.ExigirActivoConRolAsync` de la
      4.2 con `Roles.Recepcionista` (AC 18) y no crea otro método de permiso; la firma la fija la T15 de
      la 4.2 y el uso para la recepción, la T16 de esta story.

### No-regresión

22. Siguen en verde las pruebas de las stories 1.1 a 4.4. Cambios **esperados** en lo existente, y
    sólo éstos: se **borra** `Components/Personal/Secciones/SeccionesRecepcion.razor` (su único
    `@page` era `/personal/recepcion/turnos`; si quedara, Blazor daría un error por dos páginas con la
    misma ruta, L-6); en la T19 de la 2.1 (`SesionYPermisosHttpTests`), `/personal/recepcion/turnos`
    se suma a `RutasYaConstruidas` y deja de esperar el texto de construcción (sigue esperando `200` +
    `<h1>Turnos</h1>` para la recepción y `403` para los otros roles). El menú lateral, el inicio de
    la recepción (`/personal/recepcion/turnos`), la redirección tras el ingreso (2.1 T10) y la
    protección de `registros_auditoria` no cambian.

### Tests (33 verificaciones)

| # | Test | AC | Nivel | Escenario | Resultado esperado |
|---|------|----|-------|-----------|--------------------|
| T1 | `Normalizar_ConPuntosEspaciosYCerosAdelante_LosQuita` | 2, 21 | unit | `[Theory]`: `"23.456.789"`, `" 23 456 789 "`, `"07123456"`, `"7123456"`, `null` | `"23456789"`, `"23456789"`, `"7123456"`, `"7123456"`, `""` |
| T2 | `EsValido_SegunCantidadDeNumeros_SoloAceptaSieteUOcho` | 2, 21 | unit | `[Theory]` sobre lo ya normalizado: `"1234567"`, `"12345678"` (válidos); `"123456"`, `"123456789"`, `"12a45678"`, `"23-456-789"`, `""`, y `Normalizar("0000000")` (inválidos) | `true` en los dos primeros, `false` en el resto |
| T3 | `ParaMostrar_DniNormalizado_AgregaPuntosDeMiles` | 4 | unit | `[Theory]`: `"23456789"`, `"7123456"` | `"23.456.789"`, `"7.123.456"` |
| T4 | `Constructor_DatosValidos_GuardaLosDatosNormalizados` | 6, 21 | unit | `new Paciente(" 23.456.789 ", " Rosa ", " Domínguez ", " 351  555-1234 ", " Rosa@Gmail.COM ")`; y otro con email `"   "` | `Dni = "23456789"`, `Nombre = "Rosa"`, `Apellido = "Domínguez"`, `Telefono = "351 555-1234"`, `Email = "rosa@gmail.com"`; el segundo con `Email = null` |
| T5 | `Constructor_DatosInvalidos_LanzaReglaDeNegocioConElMensajeDelCampo` | 7 | unit | `[Theory]`: DNI `""` y `"123"`; nombre `""` y 101 car.; apellido ídem; teléfono `""`, `"12345"` (5 números), `"1234567890123456"` (16), `"351-555-12x4"`, 21 caracteres con 15 números; email `"rosa@"`, `"rosa gmail.com"`, 255 car. | `ReglaDeNegocioException` con el mensaje exacto de la tabla del AC 7 en cada caso |
| T6 | `CambiarDatos_ConYSinCambios_DevuelveSoloLosCamposQueCambiaron` | 12, 13 | unit | `[Theory]`: (a) mismos datos con otro formato (email en mayúsculas, teléfono con espacios de más); (b) cambia teléfono y email; (c) email a vacío | (a) lista vacía y nada cambia; (b) 2 `CambioDeDato` (`telefono`, `email`) con antes y después; (c) 1 (`email`, después `null`). En los tres, `Dni` igual |
| T7 | `CrearAsync_DatosValidos_GuardaElPacienteYAuditaElAlta` | 6, 20, 21 | integration | `FakeTimeProvider` fijo; actor = recepcionista propio real (`UsuariosDePrueba`, nombre `Recep <marca>`); alta con DNI propio escrito con puntos | Devuelve id (`long`) > 0; fila en `pacientes` con el DNI normalizado; 1 registro `paciente.creado` con `entidad_id` = id, `entidad_tipo = Paciente`, `canal = Personal`, `rol = Recepcionista`, `usuario_id` del actor, `quien = Recep <marca>` (leído de la base), `cuando` = la hora del reloj de prueba y `detalle` con los 5 campos (`dni` normalizado) |
| T8 | `CrearAsync_DniRepetidoConOtroFormato_LanzaConflictoSinGuardar` | 8 | integration | Paciente propio con DNI `<dni>`; alta con `<dni>` escrito con puntos | `ConflictoException` con el texto del AC 8 (DNI con puntos); una sola fila con ese DNI; 0 registros `paciente.creado` nuevos con ese `detalle.dni` |
| T9 | `CrearAsync_OtraAltaGanaLaCarrera_TraduceElIndiceUnicoAConflicto` | 9 | integration | `InterceptorQueSeAdelanta` de la 3.1 con `INSERT INTO pacientes (dni, nombre, apellido, telefono) VALUES ('<dni>', ...)`: lo ejecuta por **otra** conexión justo antes del `SaveChanges` | `ConflictoException` con el texto del AC 8 (no `DbUpdateException`); `SeAdelanto == true`; una sola fila con ese DNI (la del interceptor); 0 registros `paciente.creado` con ese `detalle.dni` |
| T10 | `BuscarPorDniAsync_ExisteONo_DevuelveElPacienteONull` | 3, 5 | integration | Paciente propio; `[Theory]`: su DNI con puntos; un DNI válido que no existe | El primero devuelve `DatosPaciente` con su id y datos; el segundo `null` |
| T11 | `BuscarPorDniAsync_DniInvalido_LanzaReglaDeNegocio` | 2 | integration | `[Theory]`: `""`, `"123"`, `"12a45678"` | `ReglaDeNegocioException` con `Ingresá el DNI.` / `El DNI tiene 7 u 8 números.` |
| T12 | `EditarAsync_CambiaDatos_GuardaYAuditaSoloLosCambios` | 12, 20, 21 | integration | Paciente propio; edición que cambia teléfono y email | Devuelve `true`; datos nuevos en la base; 1 registro `paciente.editado` con `detalle.dni` = su DNI y `detalle.cambios` con exactamente `telefono` y `email`, con antes y después |
| T13 | `EditarAsync_SinCambios_NoGuardaNiAudita` | 12 | integration | Paciente propio; edición con los mismos datos, email en mayúsculas | Devuelve `false`; 0 registros `paciente.editado` con su `entidad_id` |
| T14 | `EditarAsync_PacienteInexistente_LanzaNoEncontrado` | 14 | integration | `EditarAsync(long.MaxValue, …)` y `ObtenerAsync(long.MaxValue, …)` | Edición: `NoEncontradoException` con `No existe el paciente pedido. Buscalo por DNI.` y 0 registros con ese `entidad_id`; consulta: `null` |
| T15 | `OperacionesDePacientes_ActorQueNoEsRecepcionistaActivo_LanzaAccesoDenegado` | 17 | integration | `[Theory]` (`BuscarPorDniAsync`, `ObtenerAsync`, `CrearAsync`, `EditarAsync`) × actores reales de la base: administrador propio; médico propio; recepcionista propio con `Activo = false` (su actor sigue diciendo `Recepcionista`, como una sesión atrasada); 12 casos con datos válidos de un paciente propio | `AccesoDenegadoException` con el texto del AC 17; no devuelve datos; ni el alta ni el cambio quedan; 0 registros nuevos |
| T16 | `ExigirActivoConRolAsync_Recepcionista_PasaActivoYNoDesactivado` | 18, 21 | integration | En `ActorVigenteIdentityTests` (4.2). `[Theory]` sobre `ExigirActivoConRolAsync(…, Roles.Recepcionista, …)` de la 4.2: recepcionista propio activo; recepcionista propio desactivado | El activo devuelve el actor con `Quien` = nombre de la base, `Rol = Recepcionista` y canal `Personal`; el desactivado lanza `AccesoDenegadoException` con el mensaje pasado |
| T17 | `MigracionPacientes_PermisosEIndice_ComoLosDeclaraLaStory` | 10, 22 | integration | Consultas a `pg_indexes`, `information_schema.columns` y `has_table_privilege` con `CadenaDuenio` | `ix_pacientes_dni` es `CREATE UNIQUE INDEX` sobre `dni`; no hay índice único sobre `email`; `turnos_app` con `SELECT`, `INSERT`, `UPDATE` y **sin** `DELETE` sobre `pacientes`; `registros_auditoria` sigue sin `UPDATE` |
| T18 | `Pacientes_IndiceUnico_FrenaUnDniRepetidoPorCualquierPuerta` | 9, 21 | integration | Dos `Paciente` con el mismo DNI propio, guardados con `ContextoDePrueba.Crear(CadenaApp)` **sin** el caso de uso (como entraría la 6.2), en dos contextos | El segundo `SaveChangesAsync` lanza `DbUpdateException` con `PostgresException { SqlState = "23505", ConstraintName = "ix_pacientes_dni" }` (= `PacienteConfiguracion.IndiceDni`) |
| T19 | `Pacientes_FilaDeUnPaciente_SeBloqueaConElRolDeLaApp` | 10, 21 | integration | Paciente propio; con `CadenaApp`, transacción abierta: `SELECT id FROM pacientes WHERE id = @id FOR UPDATE` | Devuelve la fila sin `permission denied` (lo necesita la 5.3 para R-04) |
| T20 | `GetTurnos_Recepcion_MuestraElBuscadorDePacientes` | 1, 22 | integration | Ingreso de `recepcion1`; GET `/personal/recepcion/turnos` | `200`; `<h1>Turnos</h1>`; `<label for=…>DNI del paciente</label>` con su campo `autofocus`; botón `Buscar`; el texto del estado vacío del AC 1; el cuerpo no contiene `Esta sección se habilita` |
| T21 | `PostBuscar_DniExistente_RedirigeAlPacienteYMuestraSusDatos` | 3, 4, 21 | integration | Paciente propio con email; POST de búsqueda con el DNI escrito con puntos; seguir la redirección | `302` a `/personal/recepcion/turnos?paciente={id}` (= `RutasPersonal.Paciente(id)`; el `Location` **no** contiene el DNI); la página muestra `<Nombre> <Apellido> · DNI <con puntos>`, `Tel. … · <email>` y el enlace `Editar datos` a `?paciente={id}&editar=si` |
| T22 | `PostBuscar_DniInexistente_OfreceElAltaConElDniCargado` | 5 | integration | POST de búsqueda con un DNI válido propio que no existe, escrito con espacios | `200`; `role="status"` con el texto del AC 5 (DNI con puntos); hay un formulario de alta y su campo `Input.Dni` vale el DNI normalizado |
| T23 | `PostBuscar_DniInvalido_MuestraElMensajeDebajoDelCampo` | 2 | integration | `[Theory]`: `""`, `"123"`, `"12a45678"` | `200` (sin redirección); `Ingresá el DNI.` / `El DNI tiene 7 u 8 números.`; lo escrito sigue en el campo; no hay formulario de alta |
| T24 | `PostAlta_DatosValidos_RedirigeAlPacienteYAnunciaElAlta` | 6 | integration | POST de alta (`_handler = paciente-alta`) con DNI propio y email; seguir la redirección | `302` a `?paciente={id}&resultado=alta`; `role="status"` con `Paciente dado de alta.`; los datos del AC 4 |
| T25 | `PostAlta_CamposInvalidos_MuestraMensajePorCampoSinGuardar` | 7 | integration | `[Theory]`: todo vacío; teléfono `"12"`; email `"rosa@"` (resto válido, DNI propio) | `200`; los mensajes del AC 7 que corresponden; los valores escritos vuelven en los campos; 0 pacientes con ese DNI |
| T26 | `PostAlta_DniRepetido_Responde409ConElCartelConservandoLosDatos` | 8 | integration | Paciente propio; POST de alta con su DNI escrito con puntos y otro nombre | `409`; `role="alert"` con el texto del AC 8; el nombre escrito sigue en el campo; una sola fila con ese DNI |
| T27 | `GetEditar_MuestraLosDatosActualesYElDniSinCampo` | 11, 13 | integration | Paciente propio sin email; GET `?paciente={id}&editar=si` | `200`; campos `Input.Nombre`, `Input.Apellido`, `Input.Telefono` con sus valores y `Input.Email` vacío; el texto `DNI <con puntos> (no se modifica)`; **ningún** campo llamado `Input.Dni`; enlace `Cancelar` a `?paciente={id}` |
| T28 | `PostEditar_CambiaDatos_GuardaYAnunciaSinTocarElDni` | 12, 13 | integration | Paciente propio; POST de edición (`_handler = paciente-edicion`) a `?paciente={id}&editar=si` con otro teléfono **y** un `Input.Dni` distinto armado a mano | `302` a `?paciente={id}&resultado=edicion`; `Cambios guardados.`; teléfono nuevo en la base; DNI igual; el registro `paciente.editado` sólo nombra `telefono` |
| T29 | `PostEditar_SinCambios_AvisaQueNoHabiaCambios` | 12, 15 | integration | Paciente propio; POST de edición con los mismos datos; y GET `?paciente={id}&resultado=<script>` | `302` a `?resultado=sin-cambios` y `No había cambios para guardar.`; con el valor desconocido no hay `role="status"` y el cuerpo no contiene `<script>` |
| T30 | `Turnos_PacienteInexistenteOMalEscrito_Responde404ConElCartel` | 14 | integration | `[Theory]`: GET `?paciente=9223372036854775807`; GET `?paciente=abc`; GET `?paciente=9223372036854775807&editar=si`; POST de edición a `?paciente=9223372036854775807&editar=si` con token | `404`; `role="alert"` con `No existe el paciente pedido. Buscalo por DNI.`; el buscador está; el cuerpo no contiene `Algo falló` |
| T31 | `RutaDeTurnos_OtrosRolesOSinSesion_NoEntran` | 16 | integration | `[Theory]`: `admin1` y `medico1` × (`/personal/recepcion/turnos`, `?paciente={id}` de un paciente propio), siguiendo redirecciones; y sin sesión en las mismas dos rutas | Con sesión: `403` y `No tenés permiso para entrar a esta sección.` (y no aparece el nombre del paciente); sin sesión: `302` a `/personal/ingreso?ReturnUrl=<ruta codificada>` |
| T32 | `PostPacientes_SinTokenAntiforgery_Responde400SinGuardar` | 19 | integration | `[Theory]` sin `__RequestVerificationToken`: (a) búsqueda; (b) alta válida con DNI propio; (c) edición de un paciente propio con otro teléfono | `400`; (b) 0 pacientes con ese DNI; (c) el teléfono no cambió y 0 registros `paciente.editado` con su id |
| T33 | `PanelPaciente_ConYSinEmail_MuestraLosDatosComoLosLeeLaRecepcion` | 4, 21 | unit | bUnit con `PanelPaciente` y `Paciente = new DatosPaciente(7, "23456789", "Rosa", "Domínguez", "351-555-1234", …)`: (a) con `"rosa@gmail.com"`; (b) con `null` | (a) `Rosa Domínguez · DNI 23.456.789` y `Tel. 351-555-1234 · rosa@gmail.com`; (b) `Tel. 351-555-1234 · Sin email`; en los dos, enlace `Editar datos` a `/personal/recepcion/turnos?paciente=7&editar=si` |

Todas llevan `[Trait("Story", "5.1")]` y un `[Trait("AC", "n")]` por AC de su columna.
T1–T6 en `HospitalTurnos.Dominio.Tests`; T7–T19 en `HospitalTurnos.Aplicacion.Tests`; T20–T33 en
`HospitalTurnos.Web.Tests` (T33 con bUnit). La T19 de la 2.1 se **modifica** (AC 22) y no cuenta acá.

> El plan estimaba 10 pruebas (medida de tamaño, A-2). Quedan 33 (3,3 veces, como el S2) porque cada
> regla (formato del DNI, DNI único, campos, permiso en la base) tiene su camino de error en el caso de
> uso **y** en la pantalla, hay una carrera contra el índice único, la edición tiene tres variantes, el
> 404 tiene cuatro entradas y el contrato con 5.3, 5.5 y 6.2 nace con sus pruebas (B-1). La
> velocidad del S2 mostró que el reuso pesa más que el conteo: casi todo se copia de la 3.2.

**Por qué no hay E2E.** El proyecto `tests/HospitalTurnos.E2E/` no existe (lo crea la 10.1). Igual
que la 3.2: R1 en esta story es SSR sin JavaScript propio, así que lo que hace el navegador (POST de
formulario con token, cookies, redirecciones) lo reproducen T20–T32 con `HttpClient` sobre la
aplicación real y PostgreSQL real; lo que depende sólo de los datos al dibujar se prueba con bUnit
(T33); la verificación en el navegador queda en "Cómo verificar" y va al Dev Agent Record.

#### Given/Fixture

- **Base de datos**: la `PostgresFixture` de cada proyecto (contrato §Pruebas), sin cambios. En
  Aplicacion.Tests vive en `tests/HospitalTurnos.Aplicacion.Tests/PostgresFixture.cs` (la raíz del
  proyecto, no `Infraestructura/`). Toda clase con base usa `[Collection("Postgres")]`.
- **Base compartida**: `pacientes` no se limpia y `registros_auditoria` no se puede limpiar. **Cada
  prueba crea sus propios pacientes** con un DNI propio: `DatosDePacientes.DniPropio()` = un número de
  8 cifras al azar que empieza con 9 (`Random.Shared.Next(90_000_000, 100_000_000)`; los DNI reales de
  personas vivas casi no llegan ahí) y apellido `Prueba<marca>`
  (`<marca>` = `Guid.NewGuid().ToString("N")[..8]`). Las aserciones filtran por ese DNI, por el id
  propio o por `detalle->>'dni'`; **nunca** se cuentan filas totales.
- **`DatosDePacientes`** (un archivo por proyecto de pruebas, métodos de extensión, **esta story es su
  dueña** y la 5.3/5.5 los reusan): `DniPropio()` y `CrearPacienteAsync(…, email: null)` (directo con
  `HospitalDbContext`, sin el caso de uso, para armar el "Given"; devuelve el id `long`). En
  Aplicacion.Tests sobre `ServiceProvider`, en Web.Tests sobre `WebApplicationFactory<Program>`, como
  `DatosDeCatalogo` de la 3.2.
- **Aplicacion.Tests — T7–T16**: `ServiceProvider` armado como en `GestionMedicosEnBaseTests` (3.2):
  `AgregarInfraestructura` + `AddIdentidadPersonal` sobre una configuración en memoria con `CadenaApp`
  y `CadenaDuenio`; los dobles se registran al final (lo último gana). El actor es un **recepcionista
  real** de la base: `UsuariosDePrueba.CrearAsync(proveedor, [Roles.Recepcionista], …)` +
  `UsuariosDePrueba.ActorDe(usuario, Roles.Recepcionista)`. Un alcance por operación, como un pedido
  web. `GestionPacientesEnBase` **siempre** se resuelve del contenedor (`IGestionPacientes`), nunca
  con `new`.
- **T7 — reloj**: `FakeTimeProvider` fijo (`2026-11-02 13:00 UTC`, zona Córdoba) registrado después
  de `AgregarInfraestructura`, y se compara `cuando`. Ninguna prueba espera tiempo real (A-4).
- **T9 — carrera**: `servicios.ConfigureDbContext<HospitalDbContext>(o => o.AddInterceptors(interceptor))`
  como la 3.2 T6, con `InterceptorQueSeAdelanta(CadenaApp, sql)` de la 3.1 (Aplicacion.Tests/Ayudas).
  Los valores van escritos en el propio SQL (son la marca de la prueba; M2-1 del S2). No se crea otro
  doble.
- **T16**: va en `Aplicacion.Tests/Personal/ActorVigenteIdentityTests.cs` (lo crea la 4.2 con su T15; esta
  story le suma un método de prueba, sin tocar los de la 4.2). Usa `IActorVigente` del contenedor con
  usuarios de `UsuariosDePrueba` (el desactivado con `activo: false`). Los otros casos (otro rol, dos
  roles, inexistente, administrador) ya los prueba la T15 de la 4.2: no se repiten.
- **T17–T19**: conexión de `CadenaDuenio` para leer el catálogo de PostgreSQL (T17); `ContextoDePrueba`
  (1.3) con `CadenaApp` (T18); `NpgsqlConnection` con `CadenaApp` y `BeginTransaction` (T19).
- **Web.Tests**: `PersonalWebApplicationFactory` de la 2.1 (sin cambios) y sus usuarios fijos
  `recepcion1` (`Carla Gómez`), `admin1` y `medico1`, que sólo ingresan y navegan. Dar de alta y
  editar **pacientes** no cambia el estado de `recepcion1`.
- **Cómo se arman los POST de R1**: `FormularioHttp.EnviarAsync(cliente, ruta, formulario, campos, conToken)`
  de la 2.2 (sin ayuda nueva). Tres formularios con nombre: `buscar-paciente` (`Busqueda.Dni`),
  `paciente-alta` (`Input.Dni`, `Input.Nombre`, `Input.Apellido`, `Input.Telefono`, `Input.Email`) y
  `paciente-edicion` (los mismos sin DNI), este último contra la ruta
  `/personal/recepcion/turnos?paciente={id}&editar=si`. Los textos se comparan después de
  `WebUtility.HtmlDecode`; los `Location`, con `FormularioHttp.Destino`; carteles con
  `FormularioHttp.Alerta` y `Estado`.
- **T33**: bUnit (`BunitContext`) renderiza `PanelPaciente` con el registro como parámetro: no necesita
  servicios ni base.

## Tasks / Subtasks

- [ ] [SEQ] Task 1: Salvaguarda de estado (AC: todos) — **revalidar contra el código el día que se
      implementa** (A-1: esta story se escribe antes que 2.3, 4.2, 4.3 y 4.4, que van primero)
  - [ ] Confirmar que 2.3, 4.2, 4.3 y 4.4 están en `main` con su CI en verde (`bash scripts/ci/local.sh`).
  - [ ] Confirmar que `IActorVigente.ExigirActivoConRolAsync(actor, rol, mensajeSiNoLoEs, ct)` existe
        (lo crea la 4.2) y que existe `Aplicacion.Tests/Personal/ActorVigenteIdentityTests.cs`. Si no
        existen o tienen otra firma → DETENERSE y corregir AC 17, 18 y T16 antes de programar (B-2).
  - [ ] Confirmar las piezas que se reusan con su firma real: `ActorDeSesion.Desde`,
        `ManejadorGlobalDeErrores.EsEsperable`/`FijarEstadoEnPagina`, `FormularioHttp.EnviarAsync`/`Alerta`/`Estado`/`Destino`,
        `RutasYaConstruidas` (2.1 T19), `UsuariosDePrueba`, `InterceptorQueSeAdelanta`,
        `ContextoDePrueba`, `CambioDeDato` (en `Dominio/Catalogo/`). Esta story **no** crea ninguna.
  - [ ] Confirmar que `SeccionesRecepcion.razor` sigue teniendo un solo `@page`
        (`/personal/recepcion/turnos`) y que `RutasPersonal.Turnos` no cambió.
  - [ ] Confirmar que no existen la tabla `pacientes`, `Dominio/Pacientes/` ni nada de
        `files_to_create`, y que `AccionesAuditoria` no tiene ya `PacienteCreado`/`PacienteEditado`.
  - [ ] Anotar la última migración de `main` (hoy `20261017025538_AgregarAgendas`; 4.2–4.4 pueden
        sumar otras): `AgregarPacientes` va después.
  - [ ] Si algo no coincide → DETENERSE y corregir la story primero.
- [ ] [SEQ] Task 2: Dominio (AC: 2, 6, 7, 10, 12, 13, 20)
  - [ ] `ReglaDni` y `Paciente` en `Dominio/Pacientes/` (Implementación concreta).
  - [ ] `PacienteCreado` y `PacienteEditado` en `AccionesAuditoria`.
  - [ ] T1–T6 en verde.
- [ ] [SEQ] Task 3: Permiso de la recepción en la base (AC: 17, 18)
  - [ ] Sin código nuevo de permiso: se usa `ExigirActivoConRolAsync` de la 4.2 con `Roles.Recepcionista`.
  - [ ] T16 en `ActorVigenteIdentityTests` (rojo legítimo sólo si el método no existe: entonces
        DETENERSE, Task 1) y en verde; las pruebas de la 4.2 de ese archivo siguen en verde.
- [ ] [SEQ] Task 4: Persistencia y migración (AC: 9, 10)
  - [ ] `PacienteConfiguracion` y `DbSet<Paciente> Pacientes`, con `ApplyConfiguration` **antes** de
        `ConvencionSnakeCase.Aplicar`.
  - [ ] Migración `AgregarPacientes` con `dotnet ef migrations add` y el bloque de permisos al final de
        su `Up`. El snapshot se actualiza solo.
  - [ ] Correr la Salvaguarda de BD.
- [ ] [SEQ] Task 5: Caso de uso (AC: 2, 3, 5, 6, 8, 9, 12–14, 17, 20)
  - [ ] `IGestionPacientes` y sus registros en `Aplicacion/Pacientes/`.
  - [ ] `GestionPacientesEnBase` en `Infraestructura/Pacientes/`, registrado en `AgregarInfraestructura`.
  - [ ] `DatosDePacientes` (Aplicacion.Tests) y T7–T15, T17–T19 en verde.
- [ ] [SEQ] Task 6: Web (AC: 1–9, 11–16, 19, 22)
  - [ ] `RutasPersonal`: `Paciente(long id)` y `EditarPaciente(long id)`.
  - [ ] `FormularioPaciente`, `CamposPaciente`, `PanelPaciente` y `TurnosRecepcion` en
        `Components/Personal/Recepcion/` (carpeta nueva del rol, como `Administracion/`).
  - [ ] Borrar `Components/Personal/Secciones/SeccionesRecepcion.razor`.
  - [ ] `_Imports.razor`: `@using HospitalTurnos.Aplicacion.Pacientes` y `@using HospitalTurnos.Dominio.Pacientes`.
- [ ] [SEQ] Task 7: Pruebas de Web y verificación (AC: todos)
  - [ ] `DatosDePacientes` (Web.Tests) y T20–T33 en verde.
  - [ ] `/personal/recepcion/turnos` en `RutasYaConstruidas` de la T19 de la 2.1 (AC 22).
  - [ ] `dotnet test` completo y `bash scripts/ci/local.sh` sin regresiones.
  - [ ] Verificación en el navegador (Cómo verificar, paso 4) con script que busca por texto o rol
        (L-14), y su resultado en el Dev Agent Record.
  - [ ] Pasada del **tester no técnico** (B-6) sobre la guía `docs/qa/5.1-pasos.md` y sobre las
        pantallas (`docs/qa/5.1-legibilidad.md`, `docs/qa/5.1-legibilidad-ui.md`), como en la 3.2.

### Task Dependencies

T1 → T2 → T3 → T4 → T5 → T6 → T7. T3 puede hacerse en paralelo con T2 (no comparten archivos).
Dentro de la T6, `PanelPaciente` y su prueba T33 pueden ir en paralelo con la página.

## Dev Notes

### Qué existe hoy

#### En hospital-turnos (verificado 2026-10-17 contra `main` en `a8432c3`, con 1.1 a 4.1 en done; 2.3, 4.2, 4.3 y 4.4 se escriben en paralelo con ésta y se implementan antes)

- **No existe** nada de `files_to_create`, ni la carpeta `Dominio/Pacientes/`, ni la tabla `pacientes`.
- `RutasPersonal` (`Web/Personal/`) define `Turnos = "/personal/recepcion/turnos"` con título `Turnos`:
  es la primera sección del rol `Recepcionista` en el menú y su inicio (`InicioDe`). El menú ya existe:
  esta story no lo toca.
- `Components/Personal/Secciones/SeccionesRecepcion.razor` (2.1) tiene **un solo** `@page`
  (`/personal/recepcion/turnos`), `[Authorize(Roles = Recepcionista)]` y dibuja
  `SeccionEnConstruccion`. Agendas y Para avisar viven en `SeccionesRecepcionYAdministracion.razor`
  (no se tocan).
- `SesionYPermisosHttpTests` (2.1 T19): `RutasYaConstruidas` hoy tiene usuarios, especialidades y
  médicos; exige `<h1>{Titulo}</h1>` para cada sección permitida. `IngresoPersonalHttpTests`,
  `CambioContrasenaHttpTests`, `UsuariosPersonalHttpTests` y `MenuLateralTests` piden
  `/personal/recepcion/turnos` esperando `200` o la redirección al inicio: siguen valiendo.
- `IActorVigente` (`Aplicacion/Personal/`) tiene en `main` **sólo** `ExigirAdministradorActivoAsync(actor, mensajeSiNoLoEs, ct)`
  (la 4.2, que va antes, le suma el general `ExigirActivoConRolAsync`, y el del administrador pasa a
  llamarlo);
  lo implementa `ActorVigenteIdentity(UserManager<UsuarioPersonal>)` (`Infraestructura/Identidad/`):
  `FindByIdAsync` + `Activo` + `GetRolesAsync(...) is not [Roles.Administrador]`, y devuelve
  `new(admin.Id.ToString(), admin.NombreCompleto, Roles.Administrador, CanalAuditoria.Personal)`. Se
  registra en `RegistroIdentidad` (`AddScoped<IActorVigente, ActorVigenteIdentity>()`). Lo usan
  `GestionUsuariosPersonalIdentity`, `CatalogoEspecialidadesEnBase` y `GestionMedicosEnBase`. No hay
  un archivo de pruebas propio de `ActorVigenteIdentity` en `main` (lo crea la 4.2, T15; esta story le
  suma la T16).
- `AccionesAuditoria` (`Dominio/Auditoria/`): 22 constantes; la entidad `paciente` ya aparece en
  `PacienteMarcadoAvisado = "paciente.marcado-avisado"` (1.3). La 2.3 suma una (no choca).
- `HospitalDbContext`: `DbSet => Set<T>()` por entidad (`RegistrosAuditoria`, `MensajesSalientes`,
  `Especialidades`, `Medicos`, `AgendasMedico`), `ApplyConfiguration` a mano y al final
  `ConvencionSnakeCase.Aplicar` (`Pacientes` → `pacientes`; `HasDatabaseName("ix_pacientes_dni")`
  queda igual porque ya está en snake_case).
- Migraciones: la última es `20261017025538_AgregarAgendas`. `20261017014314_AgregarMedicos.cs` (al
  final de `Up`) es el modelo de permisos: `REVOKE ALL` + `GRANT SELECT, INSERT, UPDATE` + `GRANT USAGE`
  de la secuencia con `pg_get_serial_sequence`, porque `01-roles.sh` da `DELETE` por defecto.
- `GestionMedicosEnBase` (3.2) es el molde del caso de uso: actor confirmado en la base antes de todo,
  validación previa + índice único, alta con transacción explícita (el id identity nace al guardar y la
  auditoría lo necesita), `GuardarTraduciendoCarrerasAsync` con `catch (DbUpdateException e) when
  (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: … })`,
  edición con `CambiarDatos` que devuelve `CambioDeDato` y auditoría con `cambios.ToDictionary(...)`.
- `Medico` (3.2) es el molde de la entidad: `sealed partial class`, setters privados, constructor
  privado para EF, `Validar` que lanza `ReglaDeNegocioException` con el texto del formulario,
  `[GeneratedRegex]` para el formato. `CambioDeDato(string Campo, string? Antes, string? Despues)` vive
  en `Dominio/Catalogo/CambioDeDato.cs`: se **reusa** (no se crea otro en `Pacientes/`).
- Páginas (3.2): `FichaMedico.razor` y `MedicoNuevo.razor` son el molde del formulario SSR
  (`EditForm` + `FormName` + `[SupplyParameterFromForm(FormName = …)]`, `DataAnnotationsValidator`,
  `catch (Exception e) when (ManejadorGlobalDeErrores.EsEsperable(e))` + `FijarEstadoEnPagina`,
  `NavigateTo` fuera del `try`, `?resultado=` con `switch`, y "el formulario se dibuja sólo con el
  modelo listo", L-8). `FormularioMedico` es el molde del modelo con `IValidatableObject`.
- Pruebas: `UsuariosDePrueba` (`CrearAsync(proveedor, roles, activo, nombre)`, `ActorDe(usuario, rol)`),
  `InterceptorQueSeAdelanta(cadena, sqlQueInserta)` con `SeAdelanto`, `ContextoDePrueba.Crear(cadena)`
  en Aplicacion.Tests/Ayudas; `FormularioHttp` (`Cliente`, `IngresarAsync`, `EnviarAsync`, `Destino`,
  `Alerta`, `Estado`, `ValorDe`) y `PersonalWebApplicationFactory` (`Recepcion` = `recepcion1` /
  `Recepcion-2026` / `Carla Gómez`) en Web.Tests/Personal.
- La prueba-trampa de turnos de la 3.1 (`ContratoTurnosFuturosTests`) mira entidades `Turno` o tabla
  `turnos`: `pacientes` no la dispara.
- `appsettings.json` pone `Microsoft.AspNetCore` en `Warning`: hoy la aplicación no registra las
  direcciones pedidas, pero el historial del navegador del puesto y el registro del proxy sí (decisión 23).

#### En el framework (.NET 10)

- Una página SSR puede tener varios formularios: cada uno con su `FormName` y su
  `[SupplyParameterFromForm(FormName = …)]`; el POST trae `_handler` y Blazor llena sólo el modelo de
  ese formulario. Un `EditForm` sin `action` envía a la dirección actual, **con** su consulta
  (`?paciente={id}&editar=si`): así la edición sabe a qué paciente va.
- `[SupplyParameterFromQuery]` sobre un `long?` con un valor que no es número **no** se ignora: la
  conversión falla y termina en el `500` genérico. Por eso `paciente` se recibe como `string?` y se
  convierte con `long.TryParse` (AC 14, T30).
- `autofocus` es un atributo de HTML: funciona sin JavaScript en SSR.
- `SELECT … FOR UPDATE` en PostgreSQL exige permiso `UPDATE` sobre la tabla (T19).

### Patrón de referencia verificado

| Proyecto | Ancla | Qué se verificó y qué se toma |
|---|---|---|
| changomas-testing-csharp (guía) | `src/ChangoMas.Dominio/ItemVenta.cs` (constructor que valida, setters privados) | Entidad que no nace inválida. `Paciente` igual, con `ReglaDeNegocioException`. |
| hospital-turnos (3.2) | `src/HospitalTurnos.Dominio/Catalogo/Medico.cs` (`Validar`, `CambiarDatos`, `NormalizarMatricula`, `MatriculaValida`) | Normalizar antes de comparar, regla pública para que el formulario la reuse, cambios devueltos para la auditoría. Se copia la forma. |
| hospital-turnos (3.2) | `src/HospitalTurnos.Infraestructura/Catalogo/GestionMedicosEnBase.cs` (`CrearAsync`, `EditarAsync`, `GuardarTraduciendoCarrerasAsync`) | Permiso en la base primero, transacción explícita en el alta, traducción del `23505` por nombre de índice. Se copia la forma. |
| hospital-turnos (3.2) | `src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/20261017014314_AgregarMedicos.cs` (bloque `REVOKE`/`GRANT` al final de `Up`) | Permisos por tabla sin `DELETE`. Se copia para `pacientes`. |
| hospital-turnos (3.2) | `src/HospitalTurnos.Web/Components/Personal/Administracion/FichaMedico.razor` (`OnInitializedAsync`, `GuardarAsync`, `NoExiste`) | Formulario SSR, cartel con `FijarEstadoEnPagina`, 404 diferido, `?resultado=`. Se copia la forma. |
| hospital-turnos (3.1/3.2) | `tests/HospitalTurnos.Aplicacion.Tests/Catalogo/GestionMedicosEnBaseTests.cs` (`Proveedor`, `ConGestionAsync`) | Contenedor armado en la prueba, un alcance por operación, actor real. Se copia. |

### Qué hay que hacer (la delta)

1. La regla del DNI y la entidad `Paciente` en el dominio, con la tabla `pacientes`, su índice único y
   permisos sin `DELETE`.
2. Un caso de uso (puerto + adaptador) que busca por DNI, consulta, da de alta y edita, confirma en la
   base que el actor es un recepcionista activo, audita y traduce la carrera a conflicto.
3. El uso del método general de permiso por rol de la 4.2 con `Roles.Recepcionista` (sin código nuevo
   de permiso).
4. La columna PACIENTE de R1 (buscador, panel, alta y edición) en lugar de la sección en construcción.

### Decisiones de esta story (con su porqué)

- **R1 es SSR en esta story.** La 3.2 dejó escrito que "la interactividad empieza en la 5.5", y el
  plan da a la 5.5 "R1 completa, manejable con teclado". Hacerla interactiva ahora obligaría a otro
  modo de prueba (bUnit interactivo o E2E, que no existe) para una pantalla que la 5.5 rehace. El caso
  de uso y `PanelPaciente` sí quedan listos para la 5.5 (AC 21).
- **El permiso se mira en la base también en las lecturas** (AC 17). En la 3.2 sólo las escrituras lo
  miran; acá las lecturas devuelven datos personales de salud (DOM-01) y un recepcionista desactivado
  no debería poder seguir buscando pacientes los 5 minutos que atrasa la sesión. Cuesta una consulta
  por pedido.
- **El método general de la 4.2, no uno propio** (AC 18; I-3 de la revisión). La 4.2 (médico) y esta
  story (recepción) necesitan lo mismo que la 2.2 escribió para el administrador; lo crea una sola vez
  la que se implementa primero (la 4.2) y ésta lo reusa.
- **`ReglaDni` es una clase propia del dominio** (no métodos sueltos de `Paciente`): la usan también la
  búsqueda (sin paciente todavía), el ingreso del paciente (6.1) y el registro web (6.2). Se llama
  `ReglaDni` y no `Dni` porque `Paciente.Dni` es una propiedad: dentro de la entidad, `Dni.Exigir(...)`
  apuntaría a la propiedad y no compilaría (mismo choque que `FichaMedico` en la 3.2, I-5 del S2).
- **El DNI se guarda como texto de 7 u 8 números, no como número.** Es un identificador, no una
  cantidad; como texto se busca por igualdad con el índice y se muestra con puntos sin conversión.
  Quitar los ceros adelante (decisión 20) evita que `07123456` y `7123456` sean dos pacientes.
- **Edición simultánea del mismo paciente: gana el último** (igual que la 3.2, M-6 del S2). Dos
  recepcionistas corrigiendo el mismo teléfono en el mismo segundo es raro y no justifica `xmin`.
- **Sin desactivar ni borrar pacientes.** FR-01 a FR-03 no lo piden y un paciente va a tener turnos y
  auditoría que lo nombran. La migración no da `DELETE`.
- **Mensaje del DNI repetido con la acción siguiente** (UX §1.4): "Buscalo por DNI para ver sus datos",
  en lugar de abrirlo solo, para no mezclar el cartel de error con el panel.

### Decisiones tomadas por el docente (2026-10-17)

Numeradas como en `revision-sprint-3.md` ("Decisiones abiertas consolidadas"). Se aceptaron todas con
la opción recomendada; los AC y las pruebas ya las reflejan.

- **Decisión 19 — ¿Quién puede ver y cargar pacientes?** (era D-1; AC 16, 17, T15, T31). **Elegido:**
  sólo `Recepcionista`: buscar, ver, dar de alta y editar. El administrador ve datos de pacientes recién
  en R3 (7.3) y A5 (9.1), cada uno con su story.
- **Decisión 20 — ¿Cómo se escribe el DNI?** (era D-2; AC 2, T1, T2). **Elegido:** se aceptan puntos y
  espacios, se quitan los ceros adelante, y lo que queda tiene 7 u 8 números.
- **Decisión 21 — ¿Qué teléfono se acepta?** (era D-3; AC 7, T5). **Elegido:** números, espacios,
  guiones, paréntesis y un `+` adelante; entre 6 y 15 números y hasta 20 caracteres.
- **Decisión 22 — ¿Dos pacientes pueden tener el mismo email?** (era D-4; AC 10, T17). **Elegido:** sí
  (una madre carga a sus hijos con su email; el ingreso web es DNI **y** email).
- **Decisión 23 — ¿El DNI puede aparecer en la dirección de la página?** (era D-5; AC 3, T21).
  **Elegido:** no: búsqueda por POST y `?paciente=<id interno>`.
- **Decisión 24 — ¿Se audita quién consultó a un paciente?** (era D-6; AC 20). **Elegido:** no: FR-33 y
  DOM-03 piden registrar los cambios.

### Implementación concreta

**Dominio — `ReglaDni`** (`Dominio/Pacientes/ReglaDni.cs`):

```csharp
/// <summary>La regla del DNI (R-08, decisión 20). Una sola vez para la recepción (5.1) y la web (6.1, 6.2).</summary>
public static partial class ReglaDni
{
    public const string MensajeVacio = "Ingresá el DNI.";
    public const string MensajeFormato = "El DNI tiene 7 u 8 números.";   // texto de UX §8

    /// " 07.123.456 " → "7123456". Quita puntos, espacios y ceros adelante; no valida.
    public static string Normalizar(string? texto) { ... }

    /// Lo ya normalizado: 7 u 8 números (el primero nunca es 0 después de Normalizar).
    public static bool EsValido(string normalizado) => Formato().IsMatch(normalizado);

    /// Normaliza y valida; lanza ReglaDeNegocioException con MensajeVacio o MensajeFormato.
    public static string Exigir(string? texto) { ... }

    /// "23456789" → "23.456.789" (UX R1). Sin cultura: el separador no depende del servidor.
    public static string ParaMostrar(string normalizado) { ... }

    [GeneratedRegex("^[0-9]{7,8}$")]
    private static partial Regex Formato();
}
```

**Dominio — `Paciente`** (`Dominio/Pacientes/Paciente.cs`), con la forma de `Medico`:

```csharp
public sealed partial class Paciente
{
    public const int LargoMaximoNombre = 100;
    public const int LargoMaximoTelefono = 20;
    public const int LargoMaximoEmail = 254;
    public const string MensajeTelefono =
        "El teléfono tiene entre 6 y 15 números; puede llevar espacios, guiones, paréntesis o un + adelante.";
    public const string MensajeEmail = "El email no es válido. Revisalo o dejalo vacío.";

    public long Id { get; private set; }               // long → identity, como toda entidad propia
    public string Dni { get; private set; } = null!;   // 7 u 8 números; nunca cambia (S-18): sin setter público ni método
    public string Nombre { get; private set; } = null!;
    public string Apellido { get; private set; } = null!;
    public string Telefono { get; private set; } = null!;
    public string? Email { get; private set; }

    private Paciente() { } // para EF

    public Paciente(string dni, string nombre, string apellido, string telefono, string? email)
    {
        Dni = ReglaDni.Exigir(dni);
        (Nombre, Apellido, Telefono, Email) = Validar(nombre, apellido, telefono, email);
    }

    /// Valida todo ANTES de tocar nada y devuelve sólo lo que cambió (AC 12). No recibe el DNI (AC 13).
    public IReadOnlyList<CambioDeDato> CambiarDatos(string nombre, string apellido, string telefono, string? email) { ... }

    /// Para el formulario (mensaje debajo del campo) sin copiar las expresiones.
    public static string NormalizarTelefono(string? telefono) { ... }   // Trim + espacios internos a uno
    public static bool TelefonoValido(string normalizado) { ... }      // ^\+?[0-9 ()-]+$, 6–15 números, ≤ 20
    public static string? NormalizarEmail(string? email) { ... }       // Trim + minúsculas; vacío → null
    public static bool EmailValido(string normalizado) { ... }         // ^[^@\s]+@[^@\s]+\.[^@\s]+$, ≤ 254
}
```

`Validar` usa los mensajes de la tabla del AC 7 (nombre y apellido con los mismos textos que `Medico`).
`CambiarDatos` arma los `CambioDeDato` con claves `nombre`, `apellido`, `telefono`, `email` (camelCase:
la auditoría guarda las claves de diccionario sin convertir, como en la 3.2).

**Aplicacion — `IGestionPacientes`** (`Aplicacion/Pacientes/IGestionPacientes.cs`):

```csharp
public interface IGestionPacientes
{
    /// dni como lo escribió la recepción; ReglaDeNegocioException si no cumple (AC 2); null si no existe (AC 5).
    Task<DatosPaciente?> BuscarPorDniAsync(string dni, ActorAuditoria actor, CancellationToken ct);
    Task<DatosPaciente?> ObtenerAsync(long id, ActorAuditoria actor, CancellationToken ct);
    Task<long> CrearAsync(DatosNuevoPaciente datos, ActorAuditoria actor, CancellationToken ct);
    /// true si guardó; false si no había cambios (AC 12). Sin DNI: no se modifica (AC 13).
    Task<bool> EditarAsync(long id, DatosContactoPaciente datos, ActorAuditoria actor, CancellationToken ct);
}

public sealed record DatosPaciente(long Id, string Dni, string Nombre, string Apellido, string Telefono, string? Email);
public sealed record DatosNuevoPaciente(string Dni, string Nombre, string Apellido, string Telefono, string? Email);
public sealed record DatosContactoPaciente(string Nombre, string Apellido, string Telefono, string? Email);
```

**Permiso** (AC 18): sin código nuevo. `ExigirActivoConRolAsync` lo crea la 4.2 (su AC 21 e
"Implementación concreta"); esta story lo llama desde `ExigirRecepcionAsync` (abajo).

**Infraestructura — `GestionPacientesEnBase(HospitalDbContext contexto, IAuditoria auditoria, IActorVigente actorVigente)`**
(`Infraestructura/Pacientes/`, registrado `AddScoped<IGestionPacientes, GestionPacientesEnBase>()`).
Constantes: `SinPermiso` (AC 17), `NoExiste = "No existe el paciente pedido. Buscalo por DNI."`,
`EntidadTipo = "Paciente"`.

```csharp
public async Task<DatosPaciente?> BuscarPorDniAsync(string dni, ActorAuditoria actor, CancellationToken ct)
{
    await ExigirRecepcionAsync(actor, ct);                // AC 17, también en las lecturas (DOM-01)
    var normalizado = ReglaDni.Exigir(dni);                    // AC 2 → ReglaDeNegocioException
    return await contexto.Pacientes.AsNoTracking().Where(p => p.Dni == normalizado)
        .Select(p => new DatosPaciente(p.Id, p.Dni, p.Nombre, p.Apellido, p.Telefono, p.Email))
        .FirstOrDefaultAsync(ct);                          // índice único: a lo sumo uno
}

public async Task<long> CrearAsync(DatosNuevoPaciente datos, ActorAuditoria actor, CancellationToken ct)
{
    actor = await ExigirRecepcionAsync(actor, ct);        // el actor devuelto (nombre de la base) es el que se audita
    var paciente = new Paciente(datos.Dni, datos.Nombre, datos.Apellido, datos.Telefono, datos.Email);
    if (await contexto.Pacientes.AnyAsync(p => p.Dni == paciente.Dni, ct))
        throw DniRepetido(paciente.Dni);                  // AC 8
    // El id nace al guardar y la auditoría lo necesita: transacción explícita (como el alta de médicos).
    await using var transaccion = await contexto.Database.BeginTransactionAsync(ct);
    contexto.Pacientes.Add(paciente);
    await GuardarTraduciendoCarreraAsync(paciente, ct);   // AC 9
    auditoria.Registrar(actor, AccionesAuditoria.PacienteCreado, EntidadTipo, Texto(paciente.Id), new
    {
        dni = paciente.Dni, nombre = paciente.Nombre, apellido = paciente.Apellido,
        telefono = paciente.Telefono, email = paciente.Email,
    });
    await contexto.SaveChangesAsync(ct);
    await transaccion.CommitAsync(ct);
    return paciente.Id;
}

private Task<ActorAuditoria> ExigirRecepcionAsync(ActorAuditoria actor, CancellationToken ct) =>
    actorVigente.ExigirActivoConRolAsync(actor, Roles.Recepcionista, SinPermiso, ct);

private static ConflictoException DniRepetido(string dni) =>
    new($"Ya hay un paciente con el DNI {ReglaDni.ParaMostrar(dni)}. Buscalo por DNI para ver sus datos.");
```

`GuardarTraduciendoCarreraAsync`: igual que la 3.2, `catch (DbUpdateException e) when (e.InnerException
is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: PacienteConfiguracion.IndiceDni })`
→ `throw DniRepetido(paciente.Dni)`; otro índice sigue como error (500 genérico).
`ObtenerAsync`: permiso, después `null` si no existe. `EditarAsync`: permiso; `FirstOrDefaultAsync` con
seguimiento o `NoEncontradoException(NoExiste)`; `var cambios = paciente.CambiarDatos(...)`; si no hay
cambios → `false` sin guardar; `Registrar(PacienteEditado, EntidadTipo, id, new { dni = paciente.Dni,
cambios = cambios.ToDictionary(c => c.Campo, c => new { antes = c.Antes, despues = c.Despues }) })` y un
solo `SaveChangesAsync` (no toca el DNI: no hay carrera que traducir).

**Persistencia — `PacienteConfiguracion`**:

```csharp
public const string IndiceDni = "ix_pacientes_dni";   // ya en snake_case: lo compara la traducción del 23505

paciente.HasKey(p => p.Id);                                            // long → identity
paciente.Property(p => p.Dni).HasMaxLength(8).IsRequired();
paciente.HasIndex(p => p.Dni).IsUnique().HasDatabaseName(IndiceDni);   // la última defensa de R-08 (AC 9)
paciente.Property(p => p.Nombre).HasMaxLength(Paciente.LargoMaximoNombre).IsRequired();
paciente.Property(p => p.Apellido).HasMaxLength(Paciente.LargoMaximoNombre).IsRequired();
paciente.Property(p => p.Telefono).HasMaxLength(Paciente.LargoMaximoTelefono).IsRequired();
paciente.Property(p => p.Email).HasMaxLength(Paciente.LargoMaximoEmail);   // nulo y sin índice único (decisión 22)
```

Al final del `Up` de `AgregarPacientes` (patrón de `AgregarMedicos`):

```csharp
// Los pacientes no se borran (FR-01 a FR-03 no lo piden; tendrán turnos y auditoría). UPDATE también lo
// pide el SELECT … FOR UPDATE de la reserva (5.3, Arq. §7.2). 01-roles.sh da DELETE por defecto: REVOKE ALL.
migrationBuilder.Sql("""
    REVOKE ALL ON TABLE pacientes FROM turnos_app;
    GRANT SELECT, INSERT, UPDATE ON TABLE pacientes TO turnos_app;
    DO $$ BEGIN
      EXECUTE format('GRANT USAGE ON SEQUENCE %s TO turnos_app', pg_get_serial_sequence('pacientes', 'id'));
    END $$;
    """);
```

**Web**:

- `RutasPersonal`: `public static string Paciente(long id) => $"{Turnos}?paciente={id}";` y
  `public static string EditarPaciente(long id) => $"{Turnos}?paciente={id}&editar=si";`.
- `TurnosRecepcion.razor` (`Components/Personal/Recepcion/`): `@page "/personal/recepcion/turnos"`,
  `@layout LayoutPersonal`, `@attribute [Authorize(Roles = Roles.Recepcionista)]`, `<h1>Turnos</h1>`.
  Parámetros: `[SupplyParameterFromQuery(Name = "paciente")] string? PacienteTexto` (convertido con
  `long.TryParse`, AC 14), `[SupplyParameterFromQuery(Name = "editar")] string? Editar`,
  `[SupplyParameterFromQuery(Name = "resultado")] string? Resultado`, y tres modelos
  `[SupplyParameterFromForm(FormName = "buscar-paciente")] FormularioBusqueda? Busqueda`,
  `[SupplyParameterFromForm(FormName = "paciente-alta")] FormularioPaciente? Input`,
  `[SupplyParameterFromForm(FormName = "paciente-edicion", Name = "Input")] FormularioPaciente? Edicion`.
  Los dos formularios dibujan los campos con `CamposPaciente`, cuyo parámetro se llama `Input` (como
  `CamposMedico`): los nombres de los campos son siempre `Input.*`, y por eso la edición fija
  `Name = "Input"`. Si el compilador o la prueba muestran otro nombre, anotarlo en el Debug Log y
  corregir el Given/Fixture.
  Arriba, siempre, el buscador (`EditForm Model="Busqueda" method="post" FormName="buscar-paciente"`,
  `<label for="dni">DNI del paciente</label>`, `<InputText id="dni" autofocus …>`, botón `Buscar`).
  Debajo, la columna PACIENTE según el caso: estado vacío (AC 1), panel (AC 4), alta (AC 5–9), edición
  (AC 11–13) o cartel 404 (AC 14).
- La búsqueda válida y encontrada hace `Navegacion.NavigateTo(RutasPersonal.Paciente(id))` **fuera** del
  `try`; no encontrada deja `Input = new() { Dni = normalizado }` y el texto del AC 5. Un DNI inválido
  se muestra debajo del campo con la validación del formulario (`FormularioBusqueda` valida con
  `ReglaDni.Normalizar` + `ReglaDni.EsValido` y los mensajes de `ReglaDni`); el `ReglaDeNegocioException` del caso de
  uso cubre el POST armado a mano.
- Manejo de errores, con el mecanismo de la 2.2 (no se atrapa nada más):

```csharp
catch (Exception e) when (ManejadorGlobalDeErrores.EsEsperable(e))   // sólo las del dominio
{
    ManejadorGlobalDeErrores.FijarEstadoEnPagina(HttpContext, e);   // 409/400/403 en el acto; 404 diferido
    error = e.Message;                                               // cartel role="alert" arriba del formulario
    return;
}
```

  El paciente inexistente (GET y POST) usa
  `FijarEstadoEnPagina(HttpContext, new NoEncontradoException(...))`. **Nunca**
  `HttpContext.Response.StatusCode = 404` a mano: con Blazor .NET 10 la página saldría sin cuerpo (L-1).
- `PanelPaciente.razor`: parámetro `[Parameter, EditorRequired] public DatosPaciente Paciente`; dibuja
  las dos líneas del AC 4 con `ReglaDni.ParaMostrar` y el enlace a `RutasPersonal.EditarPaciente(Paciente.Id)`.
- `CamposPaciente.razor`: los campos con su `<label>` y `ValidationMessage` (con parámetro `MostrarDni`
  para el alta; en la edición dibuja el DNI como texto).
- `FormularioPaciente` (`IValidatableObject`, como `FormularioMedico`): `Dni`, `Nombre`, `Apellido`,
  `Telefono`, `Email` (`string?`), `[Required]`/`[StringLength]` con los mensajes del AC 7, y en
  `Validate` las reglas del dominio (`ReglaDni.EsValido`, `Paciente.TelefonoValido`, `Paciente.EmailValido`)
  para el mensaje debajo del campo. `ANuevo()` → `DatosNuevoPaciente`; `AContacto()` →
  `DatosContactoPaciente` (sin DNI, AC 13); `Desde(DatosPaciente)`. En el mismo archivo,
  `FormularioBusqueda` con `Dni`.
- El actor sale de `ActorDeSesion.Desde(HttpContext.User)`; el caso de uso lo confirma en la base.

### Salvaguarda de BD (obligatoria antes de las pruebas de Web)

Las migraciones las aplica **el arranque** (nunca `dotnet ef database update`):

```bash
docker compose up -d --build          # la app aplica AgregarPacientes al arrancar
docker compose exec db psql -U postgres -d hospital_turnos
```

1. `\d pacientes` → `id` (bigint, identity), `dni` (varchar 8, not null), `nombre`, `apellido`
   (varchar 100, not null), `telefono` (varchar 20, not null), `email` (varchar 254, nulo); índice único
   `ix_pacientes_dni`; ningún índice sobre `email`.
2. `SELECT privilege_type FROM information_schema.role_table_grants WHERE grantee = 'turnos_app' AND table_name = 'pacientes' ORDER BY 1;`
   → `INSERT`, `SELECT`, `UPDATE` (sin `DELETE`).
3. `SELECT has_table_privilege('turnos_app', 'registros_auditoria', 'UPDATE');` → sigue `f`.
4. Si CUALQUIER resultado no coincide → DETENERSE → corregir la story primero.

### Archivos a crear/modificar

| Acción | Archivo |
|---|---|
| Crear | Los de `files_to_create` del frontmatter |
| Modificar | `src/HospitalTurnos.Dominio/Auditoria/AccionesAuditoria.cs` (2 constantes, AC 20) |
| Modificar | `tests/HospitalTurnos.Aplicacion.Tests/Personal/ActorVigenteIdentityTests.cs` (de la 4.2: suma T16, AC 18) |
| Modificar | `src/HospitalTurnos.Infraestructura/Persistencia/HospitalDbContext.cs` (`DbSet<Paciente>` y `ApplyConfiguration`) |
| Modificar | `.../Migraciones/HospitalDbContextModelSnapshot.cs` (lo regenera `dotnet ef migrations add`) |
| Modificar | `src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs` (`AddScoped<IGestionPacientes, GestionPacientesEnBase>()`) |
| Modificar | `src/HospitalTurnos.Web/Personal/RutasPersonal.cs` (`Paciente(long)`, `EditarPaciente(long)`) |
| Modificar | `src/HospitalTurnos.Web/Components/_Imports.razor` (`@using` de `Aplicacion.Pacientes` y `Dominio.Pacientes`) |
| Modificar | `tests/HospitalTurnos.Web.Tests/Personal/SesionYPermisosHttpTests.cs` (una ruta más en `RutasYaConstruidas`, AC 22) |
| Borrar | `src/HospitalTurnos.Web/Components/Personal/Secciones/SeccionesRecepcion.razor` (AC 22) |

### Lo que NO hay que hacer

- **NO hacer interactiva la página** (`@rendermode`): es de la 5.5.
- **NO mostrar turnos del paciente** ni la columna NUEVO TURNO, ni un lugar vacío para ellos: son de la
  5.3 y la 5.2 (sin "en construcción" dentro de R1).
- **NO tocar el menú lateral** ni `RutasPersonal.Turnos`.
- **NO volver a crear las piezas compartidas** (`ActorDeSesion`, otro mapeo excepción → código, otra
  ayuda de POST, otro doble de carrera, otro `CambioDeDato`) ni un segundo método de permiso por rol:
  es el de la 4.2.
- **NO tocar `IActorVigente` ni `ActorVigenteIdentity`** (AC 18).
- **NO cambiar la firma ni el comportamiento de `ExigirAdministradorActivoAsync`.**
- **NO permitir cambiar el DNI** por ningún camino (ni formulario, ni caso de uso, ni método de la
  entidad).
- **NO poner el DNI en la dirección** (`?dni=`) ni en el registro técnico (decisión 23).
- **NO auditar búsquedas ni consultas** (decisión 24) ni guardar el DNI fuera de `detalle` en la auditoría.
- **NO borrar pacientes** ni dar `DELETE` a `turnos_app`.
- **NO construir `GestionPacientesEnBase` con `new` en las pruebas**; **NO contar filas totales**.
- **NO atrapar `Exception` sin el filtro `EsEsperable`** ni envolver `NavigateTo` en un `try`.
- **NO mostrar lo que venga en `?resultado=`**: sólo los tres textos fijos.
- **NO usar `dotnet ef database update`** ni `DateTime.Now`/`UtcNow`.
- **NO crear el proyecto E2E** ni agregar librerías.

### Cómo verificar que funciona

```bash
cd ~/SourceCode/hospital-turnos

# 1. Migración (proyecto de migraciones = Infraestructura, arranque = Web). No se conecta a la base.
dotnet ef migrations add AgregarPacientes \
  --project src/HospitalTurnos.Infraestructura --startup-project src/HospitalTurnos.Web \
  --output-dir Persistencia/Migraciones
# Agregar a mano el bloque de permisos al final de Up (Implementación concreta).

# 2. Pruebas de esta story (Docker para Testcontainers)
dotnet test --filter "Story=5.1"
# Esperado: 33 pruebas declaradas (más los casos de cada [Theory]), 0 fallas
dotnet test --filter "Story=2.1|Story=2.2|Story=3.1|Story=3.2"   # T19 ajustada y el permiso del admin sin cambios

# 3. Suite completa y CI local
dotnet test
bash scripts/ci/local.sh; echo "rc=$?"    # rc=0

# 4. Verificación en el navegador (script que busca por texto o rol, L-14)
docker compose up -d --build
open http://localhost:8080/personal/ingreso      # entrar como recepción
# Esperado, en este orden:
#  a. "Turnos", el campo "DNI del paciente" con el cursor puesto y "Buscá al paciente por su DNI…"
#  b. buscar "123": "El DNI tiene 7 u 8 números."
#  c. buscar "30.111.222" (no existe): "No hay ningún paciente con el DNI 30.111.222…" y el alta
#     con el DNI cargado
#  d. alta de "Rosa Domínguez", teléfono "351  555-1234", sin email: "Paciente dado de alta.",
#     "Tel. 351 555-1234 · Sin email"; la dirección dice ?paciente=<número> y no el DNI
#  e. otra alta con "30111222" desde otra búsqueda armada: "Ya hay un paciente con el DNI 30.111.222…"
#  f. "Editar datos": el DNI no tiene campo; poner email "Rosa@Gmail.com": "Cambios guardados." y
#     se ve "rosa@gmail.com"; guardar de nuevo sin tocar: "No había cambios para guardar."
#  g. pedir ?paciente=999999999: "No existe el paciente pedido. Buscalo por DNI."
#  h. entrar como administrador y pedir /personal/recepcion/turnos: "No tenés permiso…"

# 5. La auditoría quedó escrita
docker compose exec db psql -U postgres -d hospital_turnos \
  -c "SELECT cuando, quien, rol, canal, accion, entidad_id, detalle FROM registros_auditoria WHERE entidad_tipo = 'Paciente' ORDER BY id DESC LIMIT 10;"

# 6. Tester no técnico (B-6): guía docs/qa/5.1-pasos.md y revisión de las pantallas
#    (docs/qa/5.1-legibilidad.md y docs/qa/5.1-legibilidad-ui.md), como en la 3.2.
```

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

### Change Log

- 2026-10-17: Story escrita (Mary) contra `main` en `a8432c3`, en paralelo con 2.3, 4.2, 4.3 y 4.4
  (sprint 3). 22 AC, 33 verificaciones, 6 decisiones abiertas para el docente (D-1 a D-6).
- 2026-10-17: Correcciones de `revision-sprint-3.md` y decisiones 19 a 24 del docente (opción
  recomendada; marcas reemplazadas y sección "Decisiones tomadas"). I-3: el permiso por rol es el
  `ExigirActivoConRolAsync` de la 4.2 (AC 18 y 21 reescritos; sin cambios en `IActorVigente` ni en
  `ActorVigenteIdentity`; la Task 3 queda sin código de permiso; T16 reducida a la recepción, en el
  archivo de pruebas de la 4.2). M-5: `"4.2"` en `depends_on`. 22 AC, 33 verificaciones.
