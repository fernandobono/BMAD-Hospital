---
epic: 3
story: "3.2"
title: "Médicos: alta, edición y listado con estado de agenda; vínculo con el usuario médico"
status: done
sprint: 2
priority: "P0"
approach: "[CREATE]"
depends_on: ["1.2", "1.3", "2.1", "2.2", "3.1"]
files_to_create:
  - "src/HospitalTurnos.Dominio/Catalogo/Medico.cs"
  - "src/HospitalTurnos.Dominio/Catalogo/CambioDeDato.cs"
  - "src/HospitalTurnos.Dominio/Agendas/EstadoAgendaMedico.cs"
  - "src/HospitalTurnos.Dominio/Agendas/SituacionDeAgenda.cs"
  - "src/HospitalTurnos.Aplicacion/Catalogo/IGestionMedicos.cs"
  - "src/HospitalTurnos.Infraestructura/Catalogo/GestionMedicosEnBase.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/MedicoConfiguracion.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/<marca>_AgregarMedicos.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/<marca>_AgregarMedicos.Designer.cs"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/Medicos.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/MedicoNuevo.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/FichaMedico.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/TablaMedicos.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/CamposMedico.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/FormularioMedico.cs"
  - "tests/HospitalTurnos.Dominio.Tests/Catalogo/MedicoTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Catalogo/GestionMedicosEnBaseTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Catalogo/DatosDeCatalogo.cs"
  - "tests/HospitalTurnos.Web.Tests/Catalogo/MedicosHttpTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Catalogo/TablaMedicosTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Catalogo/DatosDeCatalogo.cs"
files_to_modify:
  - "src/HospitalTurnos.Dominio/Auditoria/AccionesAuditoria.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/HospitalDbContext.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/HospitalDbContextModelSnapshot.cs"
  - "src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs"
  - "src/HospitalTurnos.Web/Personal/RutasPersonal.cs"
  - "src/HospitalTurnos.Web/Components/Personal/Secciones/SeccionesAdministracion.razor"
  - "src/HospitalTurnos.Web/Components/_Imports.razor"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/UsuariosPersonal.razor"
  - "tests/HospitalTurnos.Web.Tests/Personal/SesionYPermisosHttpTests.cs"
followed_up_by:
  - id: "3.3"
    date: "2026-10-17"
    type: "extension"
    summary: "A1 pasa al estándar: listado con filtros, orden, paginación y badges; ficha en modo lectura"
  - id: "3.5"
    date: "2026-10-23"
    type: "extension"
    summary: "Los médicos pasan a tener baja lógica: desactivar y reactivar desde A1 (menú de la fila y sección final de la ficha), con filtro Activos/Inactivos/Todos"
---

# Story 3.2: Médicos: alta, edición y listado con estado de agenda; vínculo con el usuario médico

Status: done

## Story

Como administradora del Hospital Las Acacias (Lucía),
quiero dar de alta a cada médico con su nombre, apellido, matrícula y especialidad, corregir sus datos
y vincularlo con su usuario del sistema,
para que el hospital tenga su plantel cargado, cada médico pueda después armar su propia agenda con su
usuario (RU-07) y yo vea de un vistazo quién tiene agenda y quién no.

## Acceptance Criteria

> Pantalla A1 de `docs/03-ux.md` §7 (FR-13) y el vínculo usuario–médico de FR-10. Rutas bajo
> `/personal/admin/medicos`, sólo para el rol `Administrador` (tabla de permisos del PRD §7.8).
> Todas las páginas son de **renderizado estático en el servidor** (SSR: el servidor arma el HTML
> completo y el navegador envía formularios comunes, sin conexión interactiva), como S0 de la 2.1.
> Mensajes en castellano rioplatense.
>
> **Campos de auditoría** (valen para los AC 6, 16 y 22; cumplen los campos obligatorios de la 1.3).
> El actor es el que devuelve `IActorVigente` de la 2.2 (leído de la base, no de la sesión):
>
> | Campo | Valor |
> |---|---|
> | `UsuarioId` | id del administrador que opera |
> | `Quien` | su nombre completo, leído de la base |
> | `Rol` | `Administrador` |
> | `Canal` | `CanalAuditoria.Personal` (acción del personal que no es un cambio de turno) |
> | `EntidadTipo` | `"Medico"` |
> | `EntidadId` | id del médico (`long` como texto) |
> | `Detalle` | alta: `{"nombre","apellido","matricula","especialidadId","usuarioPersonalId"}` con los valores guardados; edición: `{"cambios":{"<campo>":{"antes":"…","despues":"…"}}}` sólo con los campos que cambiaron |

### Listado (pantalla A1) — FR-13

1. **Given** un administrador ingresado **When** pide `/personal/admin/medicos` **Then** recibe `200`
   con `<h1>Médicos</h1>` (exacto, sin atributos: lo compara la T19 de la 2.1), un enlace
   `Nuevo médico` a `/personal/admin/medicos/nuevo` y una tabla con las columnas **Médico**
   (`Apellido, Nombre`), **Matrícula**, **Especialidad** y **Estado de agenda**, ordenada por apellido
   y después por nombre. Cada nombre es un enlace a la ficha `/personal/admin/medicos/{id}`. El texto
   `Esta sección se habilita en una próxima versión.` ya no aparece.
2. **Given** el listado **When** se envía el buscador (formulario `GET` con el campo `buscar`)
   **Then** quedan sólo los médicos cuyo apellido, nombre o matrícula **contienen** el texto, sin
   distinguir mayúsculas; el campo conserva lo escrito. `%`, `_` y `\` se buscan como texto común
   (no son comodines). Un texto vacío o sólo con espacios no filtra. **Given** un texto de más de 100
   caracteres **Then** el listado no se filtra y debajo del campo aparece
   `La búsqueda tiene como máximo 100 caracteres.`
3. Estados vacíos (UX §8): **Given** que no hay ningún médico cargado **Then** en lugar de la tabla
   se lee `Todavía no hay médicos cargados. Usá «Nuevo médico» para dar de alta el primero.`
   **Given** una búsqueda sin resultados **Then** se lee
   `No hay médicos que coincidan con «<texto>». Probá con otro apellido o matrícula.`
4. **Estado de agenda** (UX A1: **Vigente**, **Sin agenda** o **Propuesta pendiente**, siempre como
   texto, nunca sólo color — NFR-05). Hasta la story 4.1 no existen agendas en el sistema, así que
   con esta story **todos** los médicos muestran `Sin agenda` (FR-13: "un médico recién creado no
   tiene agenda"). La 4.1 conecta el estado real **en el mismo sprint** (su AC 30; decisión 8). El
   componente que dibuja la tabla ya traduce una `SituacionDeAgenda` completa: `SinAgenda` →
   `Sin agenda`; `SinAgenda` con fecha `RigeDesde` → `Sin agenda (rige desde dd/MM)` (una agenda ya
   aprobada que todavía no rige, decisión 8); `Vigente` → `Vigente`; `PropuestaPendiente` →
   `Propuesta pendiente`. Así la 4.1 sólo cambia el cálculo, no la pantalla (contrato del AC 23).

### Alta — FR-13

5. **Given** un administrador **When** pide `/personal/admin/medicos/nuevo` **Then** ve un formulario
   con etiqueta visible en cada campo (NFR-05): `Nombre`, `Apellido`, `Matrícula`, `Especialidad`
   (lista con `Elegí una especialidad` y **sólo las especialidades activas**, por nombre) y
   `Usuario del sistema` (opcional: `Sin usuario` más los usuarios **activos** cuyo único rol es
   `Medico` y que no están vinculados a ningún médico, mostrados como `NombreCompleto (usuario)`,
   ordenados por nombre completo).
6. **Given** datos válidos **When** envía el formulario **Then** se guarda el médico (nombre y
   apellido sin espacios a los costados; matrícula normalizada: sin espacios a los costados, espacios
   internos repetidos reducidos a uno y en mayúsculas — ` mp  1234 ` se guarda `MP 1234`), se guarda
   **en la misma transacción** un registro `AccionesAuditoria.MedicoCreado` con los campos de la
   tabla de arriba, y la respuesta es `302` a `/personal/admin/medicos/{id}?resultado=alta`, donde se
   lee `Médico dado de alta.` en un elemento `role="status"` (lo anuncia el lector de pantalla, UX §9).
7. **Given** un campo inválido **When** envía el formulario **Then** responde `200` con el mensaje
   debajo de cada campo, conserva lo escrito y **no** guarda ni audita nada. La validación corre en el
   servidor también (NFR-10: un POST armado a mano da lo mismo) y el dominio la repite (la entidad
   `Medico` no nace inválida aunque alguien llame al caso de uso sin pasar por la pantalla):

   | Caso | Mensaje |
   |---|---|
   | Nombre vacío / más de 100 | `Ingresá el nombre.` / `El nombre tiene como máximo 100 caracteres.` |
   | Apellido vacío / más de 100 | `Ingresá el apellido.` / `El apellido tiene como máximo 100 caracteres.` |
   | Matrícula vacía | `Ingresá la matrícula.` |
   | Matrícula de más de 20 (ya normalizada) o con otros caracteres | `La matrícula tiene hasta 20 caracteres: letras, números, espacios, puntos, guiones o barras.` |
   | Sin especialidad elegida | `Elegí una especialidad.` |

8. **Given** que ya existe un médico con la matrícula `MP 1234` **When** se envía otro con
   ` mp 1234 ` (mismo valor normalizado) **Then** la respuesta es `409` con
   `Ya hay un médico con la matrícula MP 1234.` en un cartel `role="alert"` arriba del formulario
   (UX §8, "Error de regla de negocio"), los campos conservan lo escrito y no se guarda ni audita
   nada. **Given** dos altas simultáneas con la misma matrícula **Then** el índice único de la base
   deja entrar una sola y la otra recibe **el mismo** `409` con el mismo texto (nunca un `500`).
9. **Given** un POST armado a mano con una especialidad que no existe o está desactivada **Then** la
   respuesta es `400` con `La especialidad elegida no existe o está desactivada. Elegí otra.` en el
   cartel, sin guardar ni auditar.
10. **Given** que no hay ninguna especialidad activa **When** el administrador pide
    `/personal/admin/medicos/nuevo` **Then** ve
    `No hay especialidades activas. Creá una en Especialidades antes de dar de alta un médico.` con un
    enlace a `/personal/admin/especialidades`, y **no** se muestra el formulario. Un POST armado a mano
    en ese estado cae en el AC 9.

### Vínculo con el usuario médico — FR-10

11. Sólo se puede vincular un usuario **existente**, **activo** y cuyo **único** rol es `Medico`.
    **Given** un POST con un usuario que no cumple (no existe, tiene otro rol, tiene más de un rol o
    está desactivado) **Then** la respuesta es `400` con
    `El usuario elegido no se puede vincular: tiene que estar activo y tener el rol Médico.`, sin
    guardar ni auditar.
12. Un usuario se vincula **a lo sumo con un médico**. **Given** un usuario ya vinculado al médico
    Pereyra, Martín **When** se lo elige para otro médico (POST armado a mano: la lista del AC 5 no lo
    ofrece) **Then** `409` con `El usuario elegido ya está vinculado al médico Pereyra, Martín.`
    **Given** dos pedidos simultáneos que vinculan el mismo usuario **Then** el índice único de la base
    deja pasar uno y el otro recibe `409` con `El usuario elegido ya está vinculado a otro médico.`
    El vínculo es opcional: muchos médicos pueden estar `Sin usuario`.
13. **Given** la ficha de un médico vinculado **When** se elige `Sin usuario` y se guarda **Then**
    el médico queda sin vínculo y ese usuario vuelve a aparecer en la lista del AC 5 para otro médico.
    En la ficha, la lista de usuarios incluye además al usuario vinculado hoy a **ese** médico.
14. **Given** un usuario vinculado a un médico **When** el administrador abre A4
    (`/personal/admin/usuarios`, story 2.2) **Then** la fila de ese usuario muestra, en la columna
    **Médico vinculado**, `Apellido, Nombre` del médico. Un usuario con rol `Medico` **sin** vínculo
    muestra `Sin vincular` (FR-10 lo da como hecho: así el administrador ve qué le falta, decisión 17);
    los usuarios de otros roles la dejan vacía (UX A4: "médico vinculado (si es médico)"). La 2.2 no
    puede llenarla porque la tabla `medicos` nace acá.

### Ficha y edición — FR-13

15. **Given** un médico existente **When** el administrador pide `/personal/admin/medicos/{id}`
    **Then** ve `<h1>Apellido, Nombre</h1>`, el formulario del AC 5 con los datos actuales cargados, el
    **Estado de agenda** (AC 4) y un enlace `Volver a Médicos`. Si el médico pertenece a una
    especialidad que hoy está desactivada, la lista de especialidades la incluye igual, marcada
    `<Nombre> (desactivada)`, para que el formulario muestre el valor real.
16. **Given** cambios válidos **When** se guarda **Then** se actualiza el médico, se audita
    `AccionesAuditoria.MedicoEditado` con `Detalle` = sólo los campos que cambiaron (claves
    `nombre`, `apellido`, `matricula`, `especialidadId`, `usuarioPersonalId`; valores como texto,
    `null` si no había) en la misma transacción, y la respuesta es `302` a
    `/personal/admin/medicos/{id}?resultado=edicion` con `Cambios guardados.` en `role="status"`.
    **Given** que no cambió nada **Then** no se guarda ni se audita, y el `302` va a
    `?resultado=sin-cambios` con `No había cambios para guardar.`
17. Lo que **no cambia no se revalida**: **Given** un médico cuya especialidad fue desactivada después
    (3.1) y cuyo usuario vinculado fue desactivado después (2.2) **When** se le corrige sólo el nombre
    **Then** se guarda sin error. Las reglas de los AC 8, 9, 11 y 12 se aplican sólo al campo que
    cambia; la matrícula propia nunca es "repetida".
18. **Given** un id que no existe **When** se pide su ficha (GET) o se envía su formulario (POST)
    **Then** la respuesta es `404` con `No existe el médico pedido.` y un enlace `Volver a Médicos`
    (nunca un `500`). Un id que no es un número entero (`long`) no coincide con la ruta y cae en la
    página "no encontrada" de la 1.1.

### Permisos — FR-11, Arq. §7.7

19. **Given** un usuario con rol `Recepcionista` o `Medico` **When** pide `/personal/admin/medicos/nuevo`
    o `/personal/admin/medicos/{id}` **Then** la respuesta final es `403` con
    `No tenés permiso para entrar a esta sección.` (página de la 2.1). **Given** nadie ingresado
    **Then** `302` a `/personal/ingreso?ReturnUrl=<ruta>`.
20. El control también está **en el caso de uso** (Arq. §7.7) y se mira **en la base** con el
    servicio compartido de la 2.2, `IActorVigente.ExigirAdministradorActivoAsync` (decisión 16): la
    sesión puede atrasar hasta 5 minutos. **Given** un actor que no es un administrador activo en la
    base (otro rol, o un administrador desactivado que todavía tiene sesión) **When** llama a
    `CrearAsync` o `EditarAsync` **Then** lanza `AccesoDenegadoException` con
    `Sólo un administrador puede dar de alta o modificar médicos.` y no guarda ni audita nada. En las
    páginas, ese caso se ve como un cartel con `403` (mismo mecanismo de la 2.2, decisión 1).
21. **Given** un POST de alta **o** de edición (la ficha) sin el token antifalsificación (antiforgery)
    **Then** responde `400` y no se guarda ni se audita nada.

### Auditoría — FR-33, R-12

22. `AccionesAuditoria` suma dos constantes: `MedicoCreado = "medico.creado"` y
    `MedicoEditado = "medico.editado"`, con los campos de la tabla del inicio (estilo
    `<entidad>.<acción>` del contrato técnico §Auditoría). El registro y el cambio
    se guardan juntos o no se guarda ninguno (Arq. §7.5). La marca `Cuando` sale del `TimeProvider`
    inyectado (la pone `AuditoriaEnBase`, 1.3).

### Contrato con otras stories

23. **(Declarativo: lo cumplen los AC citados y la Task 1 de cada story siguiente, M-4 de la revisión;
    T18 y T22 fijan la parte que la 4.1 usa: `SituacionDeAgenda.SinAgenda` en la ficha y los cuatro
    textos de la tabla.)** Lo que esta story **fija** para las demás:
    - **3.1 (Especialidades)** — se cumple su contrato (su AC 16): el selector de A1 sale de
      `ICatalogoEspecialidades.ListarActivasAsync` y la validación en el servidor, de `BuscarAsync`
      (existe y `Activa`). `Medico.EspecialidadId` es `long`, como `Especialidad.Id`, con FK a
      `especialidades.id` `RESTRICT`. Un médico **conserva** una especialidad desactivada (AC 17), como
      dice la 3.1. La carrera contra los índices únicos se prueba con `InterceptorQueSeAdelanta` de
      la 3.1 (Aplicacion.Tests/Ayudas), sin un doble nuevo.
    - **2.2 (Usuarios y piezas compartidas)** — `Medico.UsuarioPersonalId` (`Guid?`) apunta a
      `asp_net_users.id` con FK `RESTRICT` e índice único. La 2.2 no vincula (contrato del brief);
      esta story suma la columna **Médico vinculado** a A4 (AC 14) con
      `IGestionMedicos.MedicosVinculadosAsync`. Se reusan, con la firma de la 2.2 (su AC 26), y no se
      crean: `ActorDeSesion.Desde(ClaimsPrincipal)`, `ManejadorGlobalDeErrores.EsEsperable`/`EstadoPara`/`FijarEstadoEnPagina`,
      `IActorVigente.ExigirAdministradorActivoAsync`, la carpeta `Components/Personal/Administracion/`,
      `FormularioHttp.EnviarAsync`/`Estado`, `RutasYaConstruidas` y `UsuariosDePrueba`.
    - **4.1 (Dominio de agenda) — contrato único del estado de agenda (C-1 de la revisión).** La 4.1
      copia este texto; si difieren, vale éste:
      - `Medico.Id` es `long` (identity, como toda entidad propia; decisión 15) y la tabla es
        `medicos`: `AgendaMedico.MedicoId` es `long` con FK a `medicos.id` (`bigint`).
      - **Esta story crea**, en `src/HospitalTurnos.Dominio/Agendas/`, el enum
        `EstadoAgendaMedico { SinAgenda, Vigente, PropuestaPendiente }` (`EstadoAgendaMedico.cs`) y el
        registro `SituacionDeAgenda(EstadoAgendaMedico Estado, DateOnly? RigeDesde = null)`
        (`SituacionDeAgenda.cs`), con `SituacionDeAgenda.SinAgenda` = `new(EstadoAgendaMedico.SinAgenda)`.
        Es lo único que A1 recibe: `MedicoListado.Agenda` y `DatosFichaMedico.Agenda`. La 4.1 no crea
        otro enum ni otra carpeta.
      - **Regla de qué estado corresponde** (decisión 8, la escribe la 4.1 en
        `Dominio/Agendas/EstadoDeAgenda.Calcular`): `PropuestaPendiente` si tiene alguna `Enviada`;
        si no, `Vigente` si alguna aprobada rige hoy; si no, `SinAgenda`, con `RigeDesde` = la
        `VigenteDesde` más próxima de las aprobadas que todavía no rigen (o `null` si no hay ninguna).
        Borradores y rechazadas no cuentan.
      - **Firma de `GestionMedicosEnBase`**: esta story la deja como
        `GestionMedicosEnBase(HospitalDbContext contexto, IAuditoria auditoria, ICatalogoEspecialidades catalogo, IActorVigente actorVigente)`.
        La 4.1 le **agrega al final** `IConsultaAgendas consultaAgendas` (cinco parámetros) y
        reemplaza, en `ListarAsync` y `ObtenerAsync`, la asignación
        `Agenda = SituacionDeAgenda.SinAgenda` (marcada con un comentario que nombra a la 4.1) por el
        valor de `IConsultaAgendas.EstadosDeAgendaAsync(ids)`: una llamada para todo el listado.
        Las pruebas de esta story resuelven `GestionMedicosEnBase` desde el contenedor (nunca la
        construyen a mano), así que la 4.1 no las toca.
      - **Quién conecta el estado real: la 4.1** (su AC 30), en este mismo sprint. La pantalla y
        `TablaMedicos` no cambian.
    - **4.2 y 8.1 (el médico de la sesión)** — "el caso de uso toma el médico de la sesión" (Arq.
      §7.7): `medicos.Where(m => m.UsuarioPersonalId == <NameIdentifier>)`. El índice único garantiza
      a lo sumo uno. **Un usuario médico sin vínculo** (decisión 17) entra a M1/M2 y ve, en lugar del
      contenido, el texto exacto
      `Tu usuario no está vinculado a un médico. Pedíselo al administrador.` (sin `500` ni pantalla
      vacía). Cada una de esas stories lo prueba.
    - **4.4 (Corregir agenda)** — el botón `Corregir agenda` y la agenda vigente en sólo lectura de la
      ficha (UX A1) los agrega la 4.4 en `FichaMedico.razor` (decisión 10). El registro de la ficha se
      llama `DatosFichaMedico` (I-5).
    - **5.3 (Reserva) — herencia** (decisión 7): la 5.3 **bloquea el cambio de especialidad** de un
      médico con turnos futuros (`No se puede cambiar la especialidad: el médico tiene turnos futuros.`),
      porque R-04 ("un turno futuro por especialidad") se calcula con la especialidad del médico. Hoy no
      hay turnos y esta story lo permite siempre. Para que no se pierda, la prueba-trampa T15 de la 3.1
      salta cuando aparecen los turnos y su mensaje nombra esta obligación (I-8).
    - **9.1 (Auditoría)** — lee `medico.creado` y `medico.editado` con `EntidadTipo = "Medico"`.

### No-regresión

24. Siguen en verde las pruebas de las stories 1.1 a 2.2 y 3.1. Cambios **esperados** en lo
    existente, y sólo éstos: `SeccionesAdministracion.razor` pierde `@page "/personal/admin/medicos"`;
    en la T19 de la 2.1 (`SesionYPermisosHttpTests`), `/personal/admin/medicos` se suma a la lista
    `RutasYaConstruidas` de la 2.2 y deja de esperar el texto de construcción (sigue esperando `200` +
    `<h1>Médicos</h1>` para el administrador y `403` para los otros roles). El menú lateral, la ruta de inicio del administrador
    (`/personal/admin/medicos`) y la protección de `registros_auditoria` no cambian.

### Tests (31 verificaciones)

| # | Test | AC | Nivel | Escenario | Resultado esperado |
|---|------|----|-------|-----------|--------------------|
| T1 | `Constructor_DatosValidos_RecortaYNormalizaLaMatricula` | 6 | unit | `new Medico("  Martín ", " Pereyra", " mp  1234 ", 3, null)` | `Nombre = "Martín"`, `Apellido = "Pereyra"`, `Matricula = "MP 1234"`, `EspecialidadId = 3`, `UsuarioPersonalId = null` |
| T2 | `Constructor_DatosInvalidos_LanzaReglaDeNegocioConElMensajeDelCampo` | 7 | unit | `[Theory]`: nombre `""`/`"  "`/101 car.; apellido ídem; matrícula `""`, 21 car., `"MP#12"`; `especialidadId` 0 y -1 | `ReglaDeNegocioException` con el mensaje exacto de la tabla del AC 7 en cada caso |
| T3 | `CambiarDatos_ConYSinCambios_DevuelveSoloLosCamposQueCambiaron` | 16 | unit | `[Theory]`: (a) mismos datos con otro formato de matrícula (` mp 1234`); (b) cambia apellido y matrícula; (c) cambia el vínculo de `null` a un `Guid` | (a) lista vacía y nada cambia; (b) 2 `CambioDeDato` (`apellido`, `matricula`) con antes y después; (c) 1 (`usuarioPersonalId`, antes `null`) |
| T4 | `CrearAsync_DatosValidos_GuardaElMedicoYAuditaElAlta` | 6, 22 | integration | `FakeTimeProvider` fijo; especialidad activa propia; actor = administrador propio real (`UsuariosDePrueba`); alta con usuario médico propio | Devuelve id (`long`) > 0; fila en `medicos` con la matrícula normalizada; 1 registro `medico.creado` con `entidad_id` = id, `entidad_tipo = Medico`, `canal = Personal`, `rol = Administrador`, `usuario_id` del actor, `quien` = su nombre leído de la base, `cuando` = la hora del reloj de prueba y `detalle` con los 5 campos |
| T5 | `CrearAsync_MatriculaRepetidaConOtroFormato_LanzaConflictoSinGuardar` | 8 | integration | Médico propio con `T-<marca>`; alta con ` t-<marca> ` | `ConflictoException` con `Ya hay un médico con la matrícula T-<MARCA>.`; un solo médico con esa matrícula; 0 registros de auditoría nuevos con esa matrícula en `detalle` |
| T6 | `GuardarAsync_OtroPedidoGanaLaCarrera_TraduceElIndiceUnicoAConflicto` | 8, 12 | integration | `[Theory]` (matrícula / usuario). `InterceptorQueSeAdelanta` de la 3.1, con el `INSERT` de un médico con la misma matrícula o el mismo usuario: lo ejecuta por **otra** conexión justo antes del `SaveChanges` | `ConflictoException` con el texto del AC 8 / AC 12 ("a otro médico"); queda una sola fila (la del interceptor); 0 registros de auditoría del alta perdida |
| T7 | `CrearAsync_EspecialidadInexistenteOInactiva_LanzaReglaDeNegocio` | 9 | integration | `[Theory]`: id que no existe; especialidad propia desactivada con el método de la 3.1 | `ReglaDeNegocioException` (no `ConflictoException`) con el texto del AC 9; 0 médicos con esa matrícula |
| T8 | `CrearAsync_UsuarioNoVinculable_LanzaReglaDeNegocio` | 11 | integration | `[Theory]`: `Guid` que no existe; usuario Recepcionista; usuario Medico desactivado; usuario con roles Medico y Recepcionista | `ReglaDeNegocioException` con el texto del AC 11 en los 4; nada guardado |
| T9 | `CrearAsync_UsuarioYaVinculado_LanzaConflictoNombrandoAlMedico` | 12 | integration | Usuario médico propio vinculado a `Prueba<marca>, Ana`; alta de otro médico con ese usuario | `ConflictoException` con `El usuario elegido ya está vinculado al médico Prueba<marca>, Ana.`; el segundo médico no existe |
| T10 | `EditarAsync_CambiaDatos_GuardaYAuditaSoloLosCambios` | 16, 22 | integration | Médico propio; edición que cambia apellido y especialidad (a otra activa) | Devuelve `true`; datos nuevos en la base; 1 registro `medico.editado` cuyo `detalle.cambios` tiene exactamente `apellido` y `especialidadId` con antes y después |
| T11 | `EditarAsync_SinCambios_NoGuardaNiAudita` | 16 | integration | Médico propio; edición con los mismos datos y la matrícula en minúsculas | Devuelve `false`; 0 registros `medico.editado` con su `entidad_id` |
| T12 | `EditarAsync_LoQueNoCambia_NoSeRevalida` | 17 | integration | Médico propio con especialidad y usuario propios; después se desactiva la especialidad (3.1) y el usuario (`Activo = false`); edición que sólo cambia el nombre | Devuelve `true`; nombre nuevo; especialidad y vínculo intactos |
| T13 | `EditarAsync_Desvincular_LiberaAlUsuarioParaOtroMedico` | 13 | integration | Médico A vinculado a un usuario propio; edición de A con `UsuarioPersonalId = null`; alta de B con ese usuario | A sin vínculo; B creado y vinculado; `UsuariosParaVincularAsync(B.Id)` incluye al usuario y `UsuariosParaVincularAsync(null)` ya no |
| T14 | `EditarAsync_MedicoInexistente_LanzaNoEncontrado` | 18 | integration | `EditarAsync(long.MaxValue, …)` | `NoEncontradoException` con `No existe el médico pedido.`; 0 registros con ese `entidad_id` |
| T15 | `EscriturasDelCatalogo_ActorQueNoEsAdministradorActivo_LanzaAccesoDenegadoSinGuardar` | 20 | integration | `[Theory]` (`CrearAsync`, `EditarAsync`) × actores reales de la base: usuario propio `Recepcionista`; usuario propio `Medico`; administrador propio con `Activo = false` (su actor sigue diciendo `Administrador`, como una sesión atrasada); 6 casos, con datos válidos | `AccesoDenegadoException` con el texto del AC 20; ni el alta ni el cambio quedan; 0 registros nuevos |
| T16 | `ListarAsync_Buscar_FiltraSinTratarComodines` | 2 | integration | Médicos propios `Prueba<marca>, Ana` (`T-<MARCA>-1`) y `Prueba<marca>, Beto` (`T-<MARCA>-2`). `[Theory]`: `prueba<marca>` (minúsculas); `beto`; `t-<marca>-2`; `%`; `_`; `"   "` (las aserciones miran sólo los resultados con la marca) | Ana y Beto en ese orden; sólo Beto; sólo Beto; ninguno de los dos (`%` y `_` no son comodines); los dos (no filtra) |
| T17 | `UsuariosParaVincularAsync_SoloMedicosActivosLibresMasElPropio` | 5, 13 | integration | Usuarios propios: médico libre, médico desactivado, recepcionista, médico vinculado al médico M | Con `null`: aparece sólo el libre (de los 4); con `M.Id`: el libre y el vinculado a M |
| T18 | `ObtenerAsync_EspecialidadDesactivadaDespues_DevuelveLaFichaConLaEspecialidadInactiva` | 15, 17, 23 | integration | Médico propio en una especialidad propia; después se desactiva con `ICatalogoEspecialidades.DesactivarAsync` (3.1) | `DatosFichaMedico` con el nombre de la especialidad, `EspecialidadActiva = false`, `Agenda = SituacionDeAgenda.SinAgenda`; `ListarActivasAsync` ya no la trae (por eso la ficha la agrega marcada, AC 15) |
| T19 | `MigracionMedicos_PermisosEIndices_ComoLosDeclaraLaStory` | 12, 24 | integration | Consultas a `pg_indexes` y `has_table_privilege` con `CadenaDuenio` | Índices únicos `ix_medicos_matricula` e `ix_medicos_usuario_personal_id`; `turnos_app` con `SELECT`, `INSERT`, `UPDATE` y **sin** `DELETE` sobre `medicos`; `registros_auditoria` sigue sin `UPDATE` |
| T20 | `GetMedicos_Administrador_MuestraListadoOrdenadoConSinAgenda` | 1, 4, 24 | integration | Ingreso de `admin1`; dos médicos propios `Prueba<marca>, Beto` y `Prueba<marca>, Ana`; GET con `buscar=<marca>` | `200`; `<h1>Médicos</h1>`; enlace a `/personal/admin/medicos/nuevo`; Ana antes que Beto, cada una con enlace a su ficha y `Sin agenda`; el cuerpo no contiene `Esta sección se habilita` |
| T21 | `GetMedicos_BusquedaSinResultadosOLarga_MuestraMensajeYConservaElTexto` | 2, 3 | integration | `[Theory]`: `buscar=nadie-<guid>`; `buscar` de 101 caracteres | (a) texto de "sin resultados" del AC 3 con el texto buscado y el campo con ese valor; (b) `La búsqueda tiene como máximo 100 caracteres.` y la tabla presente |
| T22 | `TablaMedicos_SegunEstadoDeAgendaYVacio_MuestraElTextoQueCorresponde` | 3, 4, 23 | unit | bUnit con `TablaMedicos`: (a) cuatro filas: `SinAgenda`; `SinAgenda` con `RigeDesde = 01/12/2026`; `Vigente`; `PropuestaPendiente`; (b) lista vacía sin búsqueda | (a) `Sin agenda`, `Sin agenda (rige desde 01/12)`, `Vigente`, `Propuesta pendiente` como texto en la celda de cada fila; (b) el texto "todavía no hay médicos" del AC 3 y ninguna `<table>` |
| T23 | `PostNuevo_DatosValidos_RedirigeALaFichaYAnunciaElAlta` | 5, 6 | integration | Especialidad activa y otra desactivada propias; usuario médico libre y otro vinculado propios. GET `/nuevo`: se miran las opciones; POST con el libre | Las listas tienen la activa y no la desactivada, el libre y no el vinculado; POST → `302` a `/personal/admin/medicos/{id}?resultado=alta`; la ficha tiene `role="status"` con `Médico dado de alta.` y `<h1>Apellido, Nombre</h1>` |
| T24 | `PostNuevo_CamposInvalidos_MuestraMensajePorCampoSinGuardar` | 7 | integration | `[Theory]`: todo vacío; nombre de 101; matrícula `MP#12` | `200`; los mensajes del AC 7 que corresponden; los valores escritos vuelven en los campos; 0 médicos nuevos con la marca |
| T25 | `PostNuevo_ReglaQueFalla_RespondeConElCodigoYElCartelConservandoLosDatos` | 8, 9, 11, 12 | integration | `[Theory]` (POST armado a mano): matrícula repetida → `409`; especialidad desactivada → `400`; usuario Recepcionista → `400`; usuario ya vinculado → `409` | Código esperado; `role="alert"` con el texto del AC; nombre y apellido escritos siguen en los campos; nada guardado |
| T26 | `Nuevo_SinEspecialidadesActivas_AvisaYNoOfreceElFormulario` | 10 | integration | `PostgresFixture` **propia** (base recién creada, sin especialidades; si la 3.1 siembra alguna, se desactivan antes); fábrica de la 2.1 sobre esa base | GET `/nuevo` → `200`, texto del AC 10 y enlace a `/personal/admin/especialidades`; no hay `<form` con `_handler` de alta |
| T27 | `PostFicha_EditarDatos_GuardaYAnunciaLosCambios` | 15, 16 | integration | Médico propio; GET de su ficha (campos cargados con sus datos); POST con otro apellido | GET: `<h1>`, valores en los campos, `Sin agenda`; POST `302` a `?resultado=edicion`; la ficha dice `Cambios guardados.` y el apellido nuevo |
| T28 | `Ficha_MedicoInexistente_Responde404ConEnlaceAlListado` | 18 | integration | `[Theory]`: GET `/personal/admin/medicos/9223372036854775807`; POST a esa ruta con token y `_handler` tomados de `/nuevo` | `404`; `No existe el médico pedido.`; enlace a `/personal/admin/medicos`; el cuerpo no contiene `Algo falló` |
| T29 | `RutasDeMedicos_SinRolAdministradorOSinSesion_NoEntran` | 19 | integration | `[Theory]`: `recepcion1` y `medico1` × (`/nuevo`, `/{id}` de un médico propio), siguiendo redirecciones; y sin sesión en las mismas dos rutas | Con sesión: `403` y `No tenés permiso para entrar a esta sección.`; sin sesión: `302` a `/personal/ingreso?ReturnUrl=<ruta codificada>` |
| T30 | `PostMedico_SinTokenAntiforgery_Responde400SinGuardar` | 21 | integration | `[Theory]` sin `__RequestVerificationToken`: (a) POST de alta con datos válidos y `_handler`; (b) POST a la ficha de un médico propio con otro apellido (M-5 de la revisión) | `400`; (a) 0 médicos con esa matrícula y 0 registros `medico.creado` con ella; (b) el apellido no cambió y 0 registros `medico.editado` con su id |
| T31 | `Usuarios_ColumnaMedicoVinculado_MuestraElMedicoOSinVincular` | 14 | integration | Usuario médico propio vinculado a `Prueba<marca>, Ana`; otro usuario médico propio sin vínculo; un usuario propio `Recepcionista`; GET `/personal/admin/usuarios` como `admin1` | La fila (`<tr>`) del vinculado contiene `Prueba<marca>, Ana`; la del médico sin vínculo contiene `Sin vincular`; la del recepcionista no contiene ninguno de los dos textos |

Todas llevan `[Trait("Story", "3.2")]` y un `[Trait("AC", "n")]` por AC de su columna.
T1–T3 en `HospitalTurnos.Dominio.Tests`; T4–T19 en `HospitalTurnos.Aplicacion.Tests`; T20–T31 en
`HospitalTurnos.Web.Tests` (T22 con bUnit). La T19 de la 2.1 se **modifica** (AC 24) y no cuenta acá.

> El plan estimaba 8 pruebas (como medida de tamaño, acción A-2 de la retrospectiva). Quedan 31
> porque cada regla (matrícula única, especialidad activa, vínculo válido y único) tiene su camino de
> error en el caso de uso **y** en la pantalla, hay dos carreras contra índices únicos, permisos en
> pantalla y en el caso de uso, y la edición tiene tres variantes (con cambios, sin cambios, sin
> revalidar lo que no cambia).

**Por qué no hay E2E (regla 6.1 del formato).** El proyecto `tests/HospitalTurnos.E2E/` no existe
todavía (lo crea la 10.1). Igual que en la 2.1: (1) A1 es SSR sin JavaScript propio, así que lo que hace
el navegador (GET, POST de formulario con token, cookies, redirecciones) lo reproducen T20–T31 con
`HttpClient` sobre la aplicación real (`WebApplicationFactory`) y PostgreSQL real; (2) lo que depende
sólo de los datos al dibujar (los tres textos del estado y el vacío) se prueba con bUnit (T22); (3) la
verificación en el navegador queda en "Como verificar" y su resultado va al Dev Agent Record.

#### Given/Fixture

- **Base de datos**: la `PostgresFixture` única de cada proyecto (contrato §Pruebas), sin cambios.
  En Aplicacion.Tests vive en `tests/HospitalTurnos.Aplicacion.Tests/PostgresFixture.cs` (verificado
  hoy; el contrato dice `Infraestructura/`, pero el archivo real está en la raíz del proyecto). Toda
  clase con base usa `[Collection("Postgres")]`.
- **Base compartida**: `medicos` y `especialidades` no se limpian (otras pruebas las usan) y
  `registros_auditoria` no se puede limpiar. **Cada prueba crea sus propios datos** con una marca
  `<marca>` = `Guid.NewGuid().ToString("N")[..8]`: especialidades `Esp<marca>`, médicos con apellido
  `Prueba<marca>` y matrícula `T-<MARCA>` (o `T-<MARCA>-n`), usuarios `p-<marca>`. Las aserciones
  filtran por esa marca o por el id propio; **nunca** se cuentan filas totales.
- **`DatosDeCatalogo`** (un archivo por proyecto de pruebas, métodos de extensión como pide el
  contrato; **esta story es su dueña** y la 4.1 los reusa): `CrearEspecialidadAsync(activa)` (con
  `ICatalogoEspecialidades.CrearAsync` y, si va inactiva, `DesactivarAsync` de la 3.1, con un actor
  administrador real) y `CrearMedicoAsync(...)` (directo con `HospitalDbContext`, sin pasar por el
  caso de uso, para armar el "Given"; devuelve el id `long`).
- **Aplicacion.Tests — T4–T19**: `ServiceProvider` armado como en `IngresoPersonalIdentityTests`
  (2.1): `AgregarInfraestructura` + `AddIdentidadPersonal` sobre una configuración en memoria con
  `CadenaApp` y `CadenaDuenio`; usuarios propios creados con `UsuariosDePrueba.CrearAsync` de la 2.2
  (roles asegurados). El actor es un **administrador real** de la base:
  `UsuariosDePrueba.ActorDe(admin, Roles.Administrador)`; el caso de uso lo confirma con
  `IActorVigente` (AC 20). Un alcance (`CreateScope`) por operación, como un pedido web.
  `GestionMedicosEnBase` **siempre** se resuelve del contenedor (`IGestionMedicos`), nunca con `new`:
  así, cuando la 4.1 le agregue un parámetro, estas pruebas no cambian.
- **T4 — reloj**: se registra un `FakeTimeProvider` fijo (`2026-11-02 13:00 UTC`, zona Córdoba)
  después de `AgregarInfraestructura` (lo último registrado gana) y se compara `cuando`. Ninguna
  prueba espera tiempo real.
- **T6 — carrera**: el mismo contenedor, más
  `servicios.ConfigureDbContext<HospitalDbContext>(o => o.AddInterceptors(interceptor))` (como la
  3.1 T8). El interceptor es `InterceptorQueSeAdelanta(CadenaApp, sql)` de la 3.1
  (Aplicacion.Tests/Ayudas), con
  `INSERT INTO medicos (nombre, apellido, matricula, especialidad_id, usuario_personal_id) VALUES (...)`
  con la matrícula (o el usuario) del caso; lo ejecuta una sola vez, por otra conexión. Así la
  verificación previa ya pasó y el índice único es el que frena: es exactamente la carrera del AC 8 /
  AC 12.
- **T19**: consulta `pg_indexes` (`indexname`, `indexdef LIKE 'CREATE UNIQUE INDEX%'`) y
  `has_table_privilege('turnos_app', 'medicos', '<privilegio>')` con una conexión de `CadenaDuenio`.
- **Web.Tests**: `PersonalWebApplicationFactory` de la 2.1 (sin cambios) y sus usuarios fijos
  `admin1`, `recepcion1`, `medico1`, que **sólo** ingresan y navegan (no cambian de estado). Los
  usuarios a vincular se crean con `FormularioHttp.CrearUsuarioAsync(app, [Roles.Medico], clave)`.
  Datos de catálogo con `DatosDeCatalogo` desde `app.Services.CreateScope()`.
- **Cómo se arma el POST de A1**: con `FormularioHttp.EnviarAsync(cliente, ruta, "medico", campos, conToken)`
  de la 2.2 (sin ayuda nueva): hace el `GET`, toma `__RequestVerificationToken` y manda
  `_handler = medico` (el `FormName`) con `Input.Nombre`, `Input.Apellido`, `Input.Matricula`,
  `Input.EspecialidadId` e `Input.UsuarioPersonalId` (vacío = sin usuario). Los textos se comparan
  después de `WebUtility.HtmlDecode`; los `Location`, con `FormularioHttp.Destino` (ruta y consulta).
- **T26**: base nueva propia, con el mismo patrón que `ArranquePersonalTests.cs:22` (Web.Tests) y
  `SembradorPersonalTests.cs:22` (Aplicacion.Tests): `var pg = new PostgresFixture(); await pg.InitializeAsync();`
  en un `try/finally` con `DisposeAsync` (aplica todas las migraciones), y
  `new PersonalWebApplicationFactory(pg)`. Se acepta el costo de un contenedor más para esta prueba
  (M-10 de la revisión): el aviso depende de la base vacía, no sólo del dibujo.
- **T22**: bUnit (`BunitContext`) renderiza `TablaMedicos` pasándole la lista como parámetro: no
  necesita servicios ni base.
- **T31**: depende de la pantalla A4 de la 2.2. La prueba busca el `<tr>` que contiene el nombre de
  usuario `p-<marca>` y mira su texto; no depende del orden de las columnas.

## Tasks / Subtasks

- [x] [SEQ] Task 1: Salvaguarda de estado (AC: todos) — **revalidar contra el código el día que se
      implementa** (acción A-1 de la retrospectiva: esta story se escribió antes de la 2.2 y la 3.1)
  - [x] Confirmar que 2.2 y 3.1 están en `main` con su CI en verde (`bash scripts/ci/local.sh`).
  - [x] De la **3.1**, confirmar lo que su story fija (AC 16 de la 3.1) y anotarlo en el Debug Log:
        `ICatalogoEspecialidades` con `ListarActivasAsync` y `BuscarAsync(long id, ct)`,
        `EspecialidadResumen(long Id, string Nombre, bool Activa)`, `Especialidad.Id` `long`,
        `DbSet<Especialidad> Especialidades`, la página A2 en `Components/Personal/Administracion/`,
        `CatalogoEspecialidadesEnBase(contexto, auditoria, actorVigente)`, `InterceptorQueSeAdelanta`
        en Aplicacion.Tests/Ayudas y si siembra especialidades (T26). Si algo cambió al implementarla,
        ajustar esta story antes de programar.
  - [x] De la **2.2**, confirmar las piezas compartidas con su firma real (su AC 26):
        `ActorDeSesion.Desde(ClaimsPrincipal)`, `ManejadorGlobalDeErrores.EsEsperable`/`EstadoPara`/`FijarEstadoEnPagina`,
        `IActorVigente.ExigirAdministradorActivoAsync(actor, mensajeSiNoLoEs, ct)`,
        `FormularioHttp.EnviarAsync`/`Estado`, `RutasYaConstruidas` en la 2.1 T19 y
        `UsuariosDePrueba`; que A4 es `Components/Personal/Administracion/UsuariosPersonal.razor` y
        **no** tiene la columna "Médico vinculado". Esta story **no** crea ninguna de esas piezas: si
        falta una, DETENERSE.
  - [x] Confirmar que **no** existe `src/HospitalTurnos.Dominio/Agendas/` (esta story crea ahí
        `EstadoAgendaMedico` y `SituacionDeAgenda`; la 4.1 se implementa después y los reutiliza,
        AC 23).
  - [x] Confirmar que `SeccionesAdministracion.razor` todavía tiene `@page "/personal/admin/medicos"`
        y que `RutasPersonal.Medicos` sigue siendo `/personal/admin/medicos`.
  - [x] Confirmar que no existen `medicos` ni nada de `files_to_create`.
  - [x] Si algo no coincide → DETENERSE y corregir la story primero.
- [x] [SEQ] Task 2: Dominio (AC: 6, 7, 16, 22)
  - [x] `Medico` y `CambioDeDato` en `Dominio/Catalogo/`; `EstadoAgendaMedico` y `SituacionDeAgenda`
        en `Dominio/Agendas/` (ver Implementación concreta).
  - [x] `MedicoCreado` y `MedicoEditado` en `AccionesAuditoria` (AC 22).
  - [x] T1–T3 en verde.
- [x] [SEQ] Task 3: Persistencia y migración (AC: 8, 12, 23)
  - [x] `MedicoConfiguracion` y `DbSet<Medico> Medicos` en `HospitalDbContext`, con
        `ApplyConfiguration` **antes** de `ConvencionSnakeCase.Aplicar`.
  - [x] Migración `AgregarMedicos` con `dotnet ef migrations add` y, al final de su `Up`, el bloque de
        permisos (Implementación concreta). El snapshot se actualiza solo con el comando.
  - [x] Correr la Salvaguarda de BD.
- [x] [SEQ] Task 4: Caso de uso (AC: 2, 5, 6, 8, 9, 11–13, 16–18, 20, 22)
  - [x] `IGestionMedicos` y sus tipos en `Aplicacion/Catalogo/`.
  - [x] `GestionMedicosEnBase` en `Infraestructura/Catalogo/`, registrado en `AgregarInfraestructura`.
  - [x] `DatosDeCatalogo` y T4–T19 en verde (el interceptor es el de la 3.1).
- [x] [SEQ] Task 5: Web (AC: 1–5, 7–10, 13–19, 21, 24)
  - [x] `RutasPersonal`: `NuevoMedico` y `FichaMedico(long id)`.
  - [x] `FormularioMedico`, `CamposMedico`, `TablaMedicos`, `Medicos`, `MedicoNuevo`, `FichaMedico`,
        en `Components/Personal/Administracion/` (la carpeta de A4 y A2).
  - [x] Quitar `@page "/personal/admin/medicos"` de `SeccionesAdministracion.razor`.
  - [x] Columna **Médico vinculado** en la página A4 de la 2.2, con `Sin vincular` (AC 14).
  - [x] `_Imports.razor`: `@using HospitalTurnos.Aplicacion.Catalogo` y
        `@using HospitalTurnos.Dominio.Agendas` (las páginas de la carpeta comparten espacio de nombres:
        no hace falta un `@using` de la carpeta).
- [x] [SEQ] Task 6: Pruebas de Web y verificación (AC: todos)
  - [x] `DatosDeCatalogo` de Web.Tests y T20–T31 en verde.
  - [x] `/personal/admin/medicos` en `RutasYaConstruidas` de la T19 de la 2.1 (AC 24).
  - [x] `dotnet test` completo y `bash scripts/ci/local.sh` sin regresiones.
  - [x] Verificación manual en el navegador (Como verificar, paso 4) con su resultado en el Dev Agent Record.

### Task Dependencies

T1 → T2 → T3 → T4 → T5 → T6 (secuencial). Dentro de la T5, `TablaMedicos` y su prueba T22 pueden
hacerse en paralelo con el resto de las páginas.

## Dev Notes

### Que existe hoy

#### En hospital-turnos (verificado 2026-10-16 contra `main` en `9a21b7f`, con 1.1 a 2.2 y 3.1 en done; la story de la 4.1 se escribió en paralelo con ésta y su contrato está leído)

- **No existe** ninguno de los archivos de `files_to_create` (las carpetas `Catalogo` de `src/` y
  `tests/` ya existen: son de la 3.1) ni la tabla `medicos`. La 2.2 y la 3.1 ya están en `main` (orden del sprint: 2.2 → 3.1 → 3.2 → 4.1); la
  Task 1 confirma sus piezas con la firma real.
- `HospitalDbContext` (`Infraestructura/Persistencia/`) hereda de
  `IdentityDbContext<UsuarioPersonal, IdentityRole<Guid>, Guid>`; expone `RegistrosAuditoria` y
  `MensajesSalientes` como `DbSet => Set<T>()`; aplica cada configuración **a mano**
  (`ApplyConfiguration`, sin `ApplyConfigurationsFromAssembly`) y **al final**
  `ConvencionSnakeCase.Aplicar(modelBuilder)`, que pone en `snake_case` tablas (desde el nombre del
  `DbSet`: `Medicos` → `medicos`), columnas, claves, FK e índices (`IX_Medicos_Matricula` →
  `ix_medicos_matricula`).
- Migraciones en `Persistencia/Migraciones/`: `20261015225443_Inicial`,
  `20261016010013_AgregarRegistroAuditoria`, `20261016020303_AgregarBandejaDeSalida`,
  `20261016180914_IdentidadPersonal`, `20261016212111_GestionUsuariosPersonal` (2.2) y
  `20261017000841_AgregarEspecialidades` (3.1); `AgregarMedicos` va después de esta última. Las aplica el arranque con
  `AplicadorDeMigraciones` y el rol dueño; **nunca** `dotnet ef database update`.
  `AgregarBandejaDeSalida.cs:47-55` es el modelo de permisos por tabla (`REVOKE ALL` + `GRANT` de lo que
  se usa), porque `01-roles.sh` le da a `turnos_app` `SELECT, INSERT, UPDATE, DELETE` por defecto en
  toda tabla nueva.
- Auditoría (1.3): `IAuditoria.Registrar(ActorAuditoria actor, string accion, string entidadTipo, string entidadId, object? detalle = null)`
  **agrega sin guardar**; `ActorAuditoria(string? UsuarioId, string Quien, string Rol, CanalAuditoria Canal)`;
  `AuditoriaEnBase` pone `Cuando` con el `TimeProvider` y serializa `detalle` con
  `JsonSerializerOptions.Web` (propiedades en camelCase; las **claves de un diccionario no** se
  convierten: por eso `CambioDeDato.Campo` ya viene en camelCase). `CanalAuditoria.Personal` existe.
  `AccionesAuditoria` hoy: 20 constantes: 9 de turnos y agendas (`turno.reservado`, …), las 3 de la 2.1
  en kebab-case (`ingreso-personal`, …), las 5 `usuario-personal.*` de la 2.2 y las 3 `especialidad.*`
  de la 3.1. `IAuditoria` se registra en `ServiciosDeInfraestructura.AgregarInfraestructura`.
- Errores (1.2): `ReglaDeNegocioException` (400), `ConflictoException : ReglaDeNegocioException` (409),
  `NoEncontradoException` (404), `AccesoDenegadoException` (403) en `Dominio/Excepciones/`.
  `ManejadorGlobalDeErrores`, ante un pedido de navegador (`Accept: text/html`), deja que se dibuje
  `/Error`, que **siempre** dice `Algo falló. Probá de nuevo en unos minutos.` La story 1.2 lo dejó
  escrito (línea 261): "mostrar el mensaje de la regla dentro de cada pantalla es trabajo de cada story
  de pantalla (validan antes y atrapan la excepción del caso de uso en el formulario)". Por eso las
  páginas de esta story atrapan **sólo** las excepciones del dominio, con
  `when (ManejadorGlobalDeErrores.EsEsperable(e))` y fijan el código con
  `ManejadorGlobalDeErrores.FijarEstadoEnPagina(HttpContext, e)` (2.2, decisión 1): toma el código de
  `EstadoPara(e)` y, si es `404`, lo fija recién al empezar a enviar la respuesta, porque Blazor (.NET 10)
  no escribe el cuerpo de una página SSR que terminó de dibujarse en `404` y el cartel no llegaría.
- Personal (2.1): `RutasPersonal` (`Web/Personal/`) ya define `Medicos = "/personal/admin/medicos"`,
  título `Médicos`, y es el inicio del administrador. `SeccionesAdministracion.razor` tiene 4 `@page`
  (médicos, propuestas, bloqueos, auditoría; usuarios y especialidades salieron con la 2.2 y la 3.1) con
  `[Authorize(Roles = Roles.Administrador)]` y `@layout LayoutPersonal`, y dibuja
  `SeccionEnConstruccion`. `AccesoDenegado.razor:21` fija su `403` a mano
  (`HttpContext.Response.StatusCode = ...`) y conserva su cuerpo; con un `404` eso **no** sirve (la
  página saldría sin cuerpo): por eso esta story nunca fija un código a mano y usa siempre
  `FijarEstadoEnPagina`. `Ingreso.razor:22-45` es el modelo de
  formulario SSR: `EditForm` con `method="post"`, `FormName` y `[SupplyParameterFromForm]`.
  `ClaimsPersonal.NombreCompleto = "nombre_completo"` (en `Aplicacion/Personal/IIngresoPersonal.cs`).
  `Roles.Medico = "Medico"` (sin acento: es el texto que guarda Identity).
- `UsuarioPersonal : IdentityUser<Guid>` (`Infraestructura/Identidad/`) con `NombreCompleto` (120) y
  `Activo`. Los roles se leen con los `DbSet` de Identity (`Users`, `UserRoles`, `Roles`) del mismo
  contexto: tablas `asp_net_users`, `asp_net_user_roles`, `asp_net_roles`.
- Pruebas de Web (2.1 y 2.2): `PersonalWebApplicationFactory`, `FormularioHttp` (`Cliente`, `ValorDe`,
  `TokenDeAsync`, `IngresarAsync`, `Destino`, `Alerta`, `Estado`, `EnviarAsync`, `CrearUsuarioAsync`),
  `SinParalelismo.cs`. La T19 (`SesionYPermisosHttpTests.Secciones_SegunRol_...`) exige, para cada
  sección permitida, `<h1>{Titulo}</h1>`, y el texto de construcción salvo en las rutas de
  `RutasYaConstruidas` (hoy `/personal/admin/usuarios` y `/personal/admin/especialidades`): esta story
  suma `/personal/admin/medicos` (AC 24).
- Código descartable del arranque (`git show 48f29a1:src/HospitalTurnos.Api/Dominio/Medico.cs`): ya
  usaba `int Id` (**no** se toma: el código descartable no es contrato, decisión 15), `long EspecialidadId`, invariantes en el constructor con `ReglaDeNegocioException` y
  `NombreCompleto => $"{Apellido}, {Nombre}"`. Se toma la forma; **no** la duración del turno ni los
  horarios (son de la agenda, 4.1).

#### En el framework (.NET 10)

- `DbUpdateException` envuelve una `Npgsql.PostgresException` con `SqlState = "23505"`
  (`PostgresErrorCodes.UniqueViolation`) y `ConstraintName` = nombre del índice cuando se viola un
  índice único. Con eso se traduce la carrera a `ConflictoException` sin depender del texto del error.
- `EF.Functions.ILike(columna, patron, "\\")` (Npgsql) traduce a `ILIKE ... ESCAPE '\'`: con el patrón
  escapado, `%` y `_` dejan de ser comodines (AC 2).
- Una columna `identity` recibe su valor recién al guardar: el id del médico nuevo no existe antes del
  primer `SaveChangesAsync`. Por eso el alta usa una transacción explícita (ver Implementación concreta).

### Patron de referencia verificado

| Proyecto | Ancla `archivo:línea` | Qué se verificó y qué se toma |
|---|---|---|
| changomas-testing-csharp (guía) | `src/ChangoMas.Dominio/ItemVenta.cs:8-15` | Entidad que valida en el constructor y no nace inválida, con setters privados (`:18`). `Medico` igual, con `ReglaDeNegocioException` (contrato §Errores) en vez de `ArgumentOutOfRangeException`. |
| changomas-testing-csharp (guía) | `src/ChangoMas.Dominio/Puertos/IRepositorioVentas.cs:4-8` | Puerto con `Task` y `CancellationToken`. Acá el puerto vive en Aplicacion (como `IIngresoPersonal` de la 2.1) porque el caso de uso usa la base y la auditoría. |
| hospital-turnos (2.1) | `src/HospitalTurnos.Infraestructura/Identidad/IngresoPersonalIdentity.cs` (la clase y `AuditarAsync`) | Adaptador en Infraestructura que implementa un puerto de Aplicacion, recibe `IAuditoria` y `HospitalDbContext` y cierra con **un** `SaveChangesAsync` que guarda cambio y auditoría juntos. Se toma igual. |
| hospital-turnos (1.4) | `src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/20261016020303_AgregarBandejaDeSalida.cs:47-55` | `REVOKE ALL` + `GRANT` sólo de lo que se usa + `GRANT USAGE` de la secuencia. Se copia para `medicos` (sin `DELETE`). |
| hospital-turnos (1.3/1.4) | `src/HospitalTurnos.Infraestructura/Persistencia/HospitalDbContext.cs` (`OnModelCreating`) | Configuración por clase `IEntityTypeConfiguration<T>` aplicada a mano antes de la convención. |
| hospital-turnos (2.1) | `src/HospitalTurnos.Web/Components/Personal/Ingreso.razor:22`, `:45`, `:75` | Formulario SSR (`EditForm` + `FormName` + `[SupplyParameterFromForm]`) y `NavigateTo` fuera de todo `try` (en SSR se implementa con una excepción). |

El guía no tiene índices únicos ni traducción de errores de la base (`grep -rn -E "IsUnique|HasIndex|23505|DbUpdateException"`
sobre `~/SourceCode/changomas-testing-csharp` → 0 resultados; usa SQLite a mano). Para eso se usa la
API de Npgsql descrita arriba.

### Que hay que hacer (la delta)

1. La entidad `Medico` con sus invariantes y la tabla `medicos` con dos índices únicos (matrícula y
   usuario vinculado) y permisos sin `DELETE`.
2. Un caso de uso (puerto + adaptador) que lista, busca, da de alta y edita médicos, valida
   especialidad activa y vínculo, audita y traduce las carreras a conflicto.
3. Tres páginas SSR (listado, alta, ficha) que reemplazan la sección en construcción de A1, y la
   columna "Médico vinculado" en A4.

### Decisiones de esta story (con su porqué)

- **Sin desactivar ni borrar médicos.** FR-13 y UX A1 sólo piden alta, edición y listado. La
  migración no le da `DELETE` a `turnos_app`: un médico con turnos o agendas no puede desaparecer.
- **El vínculo vive en `Medico` (`UsuarioPersonalId`), no en `UsuarioPersonal`.** El dominio no conoce
  a Identity (Arq. §5: las dependencias apuntan hacia adentro); `Medico` guarda sólo el `Guid`, y la FK
  la declara Infraestructura. Así 4.2 y 8.1 encuentran "el médico de la sesión" con una consulta.
- **Sólo se vinculan usuarios activos con el único rol `Medico`**, y la regla se aplica cuando el vínculo
  **cambia** (AC 17): si la 2.2 desactiva a un médico, su ficha se puede seguir corrigiendo sin
  romper el vínculo. Mismo criterio para la especialidad desactivada.
- **El permiso se controla también en el caso de uso, en la base** (Arq. §7.7, decisión 16), con
  `IActorVigente` de la 2.2, aunque la página ya pida `Administrador`: un POST armado a mano, una
  pantalla futura o un administrador recién desactivado con la sesión atrasada no se saltean la regla.
- **Matrícula normalizada antes de comparar** (mayúsculas y espacios simples): `mp 1234` y `MP  1234`
  son la misma matrícula para una persona; si se guardaran distintas, el índice único no la protegería.
- **Verificación previa + índice único.** La verificación da el mensaje con el nombre del médico que ya
  tiene el usuario; el índice es lo que de verdad garantiza la regla con dos administradores a la vez
  (mismo criterio que NFR-02 para los turnos).
- **Los errores de regla se muestran en la pantalla con su código** (`409`/`400`/`404`/`403`) y el texto
  de la regla, como pide UX §8, porque el camino HTML del manejador global siempre muestra el mensaje
  genérico (1.2, línea 261). El código lo fija `ManejadorGlobalDeErrores.FijarEstadoEnPagina` (2.2) con
  el de `EstadoPara`: el mismo que daría el manejador para JSON, escrito en un solo lugar, y con el `404`
  diferido para que el cartel llegue en el cuerpo.
- **Éxito con redirección (`302` a la ficha con `?resultado=`)**: evita que "Atrás" o "Actualizar"
  reenvíe el alta (patrón POST-redirección-GET); SSR no tiene un lugar donde guardar el mensaje entre
  pedidos, así que viaja en la consulta y la página lo traduce a un texto fijo (nunca se muestra lo
  que venga en la consulta).
- **Búsqueda sin acentos no contemplada**: `fernandez` no encuentra `Fernández`. Se acepta en esta
  versión (la extensión `unaccent` de PostgreSQL agregaría una migración y un caso más); la recepción
  busca médicos por especialidad (5.2), no por este listado.
- **`Medico.Id` y `Medico.EspecialidadId` son `long`** (decisión 15), como toda entidad propia del
  código (`RegistroAuditoria`, `MensajeSaliente`, `Especialidad`, y en la 4.1 `AgendaMedico` y
  `Franja`). El `int` de la versión anterior salía de un argumento circular con la 4.1 y del código
  descartable, que no es contrato (I-4). Cambiarlo después obligaría a migrar columnas y FK de
  turnos, bloqueos y agendas; antes de programar es gratis.
- **`EstadoAgendaMedico` y `SituacionDeAgenda` nacen acá, en `Dominio/Agendas/`**, y el contrato con
  la 4.1 se escribe una sola vez, en el AC 23 (C-1 de la revisión). A1 los necesita para dibujar la
  columna antes de que exista la 4.1; la 4.1 sólo cambia el cálculo. La fecha `RigeDesde` existe
  para mostrar `Sin agenda (rige desde dd/MM)` (decisión 8): una agenda aprobada el 11/11 que rige el
  01/12 no se confunde con un médico sin nada cargado.
- **Edición simultánea del mismo médico: gana el último** (M-6 de la revisión, aceptado). Cada pedido
  lee el médico en el momento de guardar, así que el `antes` de su auditoría es lo que había en la
  base al leerlo; sólo si dos administradores guardan en el mismo instante, el `antes` del segundo
  puede no ver el cambio del primero. Es un caso raro en un plantel de decenas de médicos y no
  justifica agregar control de concurrencia (`xmin`) a esta tabla.
- **Nombres sin choque** (I-5 de la revisión): el registro de la ficha se llama `DatosFichaMedico`
  (la página `FichaMedico.razor` genera la clase `FichaMedico`), y la ruta de alta es
  `RutasPersonal.NuevoMedico` (la página es `MedicoNuevo.razor`).
- **La columna "Médico vinculado" de A4 se hace acá** (AC 14): UX A4 la pide y la 2.2 no la puede
  llenar porque la tabla nace en esta story. Se lee con `MedicosVinculadosAsync`, sin tocar el caso de
  uso de la 2.2.

### Decisiones tomadas por el docente (2026-10-16)

Numeradas como en `revision-sprint-2.md` ("Decisiones abiertas consolidadas").

- **Decisión 8 — Estado de agenda antes y después de la 4.1** (era DA-1). **Elegido:** esta story
  muestra `Sin agenda` para todos (es cierto: no hay tabla de agendas); la 4.1 conecta el estado
  real en el mismo sprint (su AC 30). Matiz del docente: una agenda aprobada que todavía no rige se
  muestra `Sin agenda (rige desde dd/MM)`. La pantalla lo dibuja desde ya (AC 4, T22) y la 4.1 lo
  calcula y lo prueba de punta a punta.
- **Decisión 9 — Formato de la matrícula** (era DA-2). **Elegido:** texto de 1 a 20 caracteres con
  letras, números, espacio, punto, guion y barra, normalizado a mayúsculas (`MP 1234`, `MN-45678`,
  `12345/7`). AC 6 y 7.
- **Decisión 7 — Cambiar la especialidad de un médico con turnos futuros** (era DA-3). **Elegido:** la
  bloquea la 5.3, como herencia escrita en el AC 23 y nombrada en la trampa T15 de la 3.1 (I-8).
  Esta story lo permite siempre porque hoy no hay turnos.
- **Decisión 10 — Agenda vigente y "Corregir agenda" en la ficha** (era DA-4). **Elegido:** los agrega
  la 4.4. Esta story sólo muestra el estado.
- **Decisión 15 — Tipo de `Medico.Id`.** **Elegido:** `long`.
- **Decisión 16 — Permiso en el caso de uso.** **Elegido:** se confirma en la base en cada cambio con
  `IActorVigente` de la 2.2 (AC 20, T15).
- **Decisión 17 — Usuario médico sin vincular.** **Elegido:** el vínculo sigue opcional (alta del
  usuario en A4 y del médico en A1 son pasos distintos), pero visible: A4 muestra `Sin vincular` para
  un usuario `Medico` sin vínculo (AC 14, T31), y el contrato fija el texto que muestran la 4.2 y la
  8.1: `Tu usuario no está vinculado a un médico. Pedíselo al administrador.` (AC 23).
- **Decisión 1 — Error de regla en pantalla.** **Elegido:** cartel en la página con el mecanismo de la
  2.2 (`EsEsperable` + `FijarEstadoEnPagina`, que usa `EstadoPara`).

### Implementacion concreta

**Dominio — `Medico`** (`Dominio/Catalogo/Medico.cs`):

```csharp
public sealed class Medico
{
    public const int LargoMaximoNombre = 100;
    public const int LargoMaximoMatricula = 20;

    public long Id { get; private set; }                 // long → identity, como toda entidad propia
    public string Nombre { get; private set; } = null!;
    public string Apellido { get; private set; } = null!;
    public string Matricula { get; private set; } = null!;
    public long EspecialidadId { get; private set; }       // tipo = el de Especialidad.Id (3.1)
    public Guid? UsuarioPersonalId { get; private set; }  // el dominio no conoce a Identity: sólo el id

    private Medico() { } // para EF

    public Medico(string nombre, string apellido, string matricula, long especialidadId, Guid? usuarioPersonalId)
    {
        (Nombre, Apellido, Matricula, EspecialidadId) = Validar(nombre, apellido, matricula, especialidadId);
        UsuarioPersonalId = usuarioPersonalId;
    }

    public string NombreCompleto => $"{Apellido}, {Nombre}";   // como lo muestran A1 y A6

    /// Valida todo ANTES de tocar nada y devuelve sólo lo que cambió (para la auditoría, AC 16).
    public IReadOnlyList<CambioDeDato> CambiarDatos(string nombre, string apellido, string matricula,
        long especialidadId, Guid? usuarioPersonalId) { ... }

    /// " mp  1234 " → "MP 1234". Pública: la usa el caso de uso para buscar la repetida.
    public static string NormalizarMatricula(string matricula) { ... }
}

public sealed record CambioDeDato(string Campo, string? Antes, string? Despues); // Campo en camelCase
```

**Dominio — `EstadoAgendaMedico` y `SituacionDeAgenda`** (`Dominio/Agendas/`, contrato del AC 23;
los crea esta story y la 4.1 los reusa):

```csharp
// EstadoAgendaMedico.cs
/// <summary>Las tres palabras de la columna de A1 (UX). La regla de cuál corresponde la escribe la 4.1.</summary>
public enum EstadoAgendaMedico
{
    SinAgenda,          // hoy no ofrece turnos (FR-13)
    Vigente,            // hoy rige una agenda aprobada
    PropuestaPendiente, // tiene una propuesta enviada que espera al administrador
}

// SituacionDeAgenda.cs
/// <summary>Lo que A1 muestra de la agenda de un médico. RigeDesde sólo con SinAgenda, cuando hay una
/// aprobada que todavía no rige (decisión 8: "Sin agenda (rige desde 01/12)").</summary>
public sealed record SituacionDeAgenda(EstadoAgendaMedico Estado, DateOnly? RigeDesde = null)
{
    public static readonly SituacionDeAgenda SinAgenda = new(EstadoAgendaMedico.SinAgenda);
}
```

Los mensajes de `Validar` son los de la tabla del AC 7 (los mismos textos que los atributos del
formulario: la pantalla y el dominio dicen lo mismo). Regla de la matrícula ya normalizada:
`^[A-ZÑ0-9 ./-]{1,20}$`.

**Aplicacion — `IGestionMedicos`** (`Aplicacion/Catalogo/IGestionMedicos.cs`):

```csharp
public interface IGestionMedicos
{
    Task<IReadOnlyList<MedicoListado>> ListarAsync(string? buscar, CancellationToken ct);
    Task<DatosFichaMedico?> ObtenerAsync(long id, CancellationToken ct);
    /// Activos, con el único rol Medico, sin vínculo; más el vinculado hoy a medicoId si viene.
    Task<IReadOnlyList<OpcionUsuarioMedico>> UsuariosParaVincularAsync(long? medicoId, CancellationToken ct);
    /// usuarioId → "Apellido, Nombre" (columna de A4, AC 14).
    Task<IReadOnlyDictionary<Guid, string>> MedicosVinculadosAsync(CancellationToken ct);
    Task<long> CrearAsync(DatosMedico datos, ActorAuditoria actor, CancellationToken ct);
    /// true si guardó; false si no había cambios (AC 16).
    Task<bool> EditarAsync(long id, DatosMedico datos, ActorAuditoria actor, CancellationToken ct);
}

public sealed record DatosMedico(string Nombre, string Apellido, string Matricula, long EspecialidadId, Guid? UsuarioPersonalId);
public sealed record MedicoListado(long Id, string Apellido, string Nombre, string Matricula, string Especialidad, SituacionDeAgenda Agenda);
// "Datos…" y no "FichaMedico": la página FichaMedico.razor ya genera una clase con ese nombre (I-5).
public sealed record DatosFichaMedico(long Id, DatosMedico Datos, string Especialidad, bool EspecialidadActiva, SituacionDeAgenda Agenda);
public sealed record OpcionUsuarioMedico(Guid Id, string Usuario, string NombreCompleto);
```

`ListarAsync` con un `buscar` de más de 100 caracteres no filtra (la pantalla muestra el aviso del
AC 2); con texto, `ILike` con `%<texto escapado>%` sobre apellido, nombre y matrícula
(escapar `\`, `%` y `_` con `\`).

**Infraestructura — `GestionMedicosEnBase(HospitalDbContext contexto, IAuditoria auditoria, ICatalogoEspecialidades catalogo, IActorVigente actorVigente)`**
(`Infraestructura/Catalogo/`; firma del contrato del AC 23: la 4.1 le agrega al final
`IConsultaAgendas consultaAgendas`). `SinPermiso` es la constante con el texto del AC 20. El selector de especialidades de las páginas usa directamente
`ICatalogoEspecialidades.ListarActivasAsync` (contrato de la 3.1); la ficha agrega la del médico si
`BuscarAsync` la devuelve con `Activa = false` (AC 15). Alta:

```csharp
public async Task<long> CrearAsync(DatosMedico datos, ActorAuditoria actor, CancellationToken ct)
{
    // AC 20, EN LA BASE y antes de mirar nada; el actor devuelto (nombre leído de la base) es el que se audita
    actor = await actorVigente.ExigirAdministradorActivoAsync(actor, SinPermiso, ct);
    var medico = new Medico(datos.Nombre, datos.Apellido, datos.Matricula, datos.EspecialidadId, datos.UsuarioPersonalId);
    await ExigirEspecialidadActivaAsync(medico.EspecialidadId, ct);            // AC 9  → ReglaDeNegocio (catalogo.BuscarAsync, 3.1)
    await ExigirMatriculaLibreAsync(medico.Matricula, excepto: null, ct);      // AC 8  → Conflicto
    if (medico.UsuarioPersonalId is { } usuario)
        await ExigirUsuarioVinculableAsync(usuario, excepto: null, ct);        // AC 11 → ReglaDeNegocio, AC 12 → Conflicto

    // El id (identity) nace al guardar y la auditoría lo necesita: transacción explícita para que
    // médico y registro queden juntos o ninguno (Arq. §7.5).
    await using var transaccion = await contexto.Database.BeginTransactionAsync(ct);
    contexto.Medicos.Add(medico);
    await GuardarTraduciendoCarrerasAsync(medico, ct);
    auditoria.Registrar(actor, AccionesAuditoria.MedicoCreado, "Medico", medico.Id.ToString(), new
    {
        nombre = medico.Nombre, apellido = medico.Apellido, matricula = medico.Matricula,
        especialidadId = medico.EspecialidadId, usuarioPersonalId = medico.UsuarioPersonalId,
    });
    await contexto.SaveChangesAsync(ct);
    await transaccion.CommitAsync(ct);
    return medico.Id;
}

// AC 8 y 12: la carrera la frena el índice; el usuario ve la misma regla, no un 500.
private async Task GuardarTraduciendoCarrerasAsync(Medico medico, CancellationToken ct)
{
    try { await contexto.SaveChangesAsync(ct); }
    catch (DbUpdateException e) when (e.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.UniqueViolation } pg)
    {
        throw pg.ConstraintName switch
        {
            MedicoConfiguracion.IndiceMatricula => new ConflictoException($"Ya hay un médico con la matrícula {medico.Matricula}."),
            MedicoConfiguracion.IndiceUsuario => new ConflictoException("El usuario elegido ya está vinculado a otro médico."),
            _ => e,   // otro índice: no es una regla conocida, sigue como error (500 genérico)
        };
    }
}
```

`throw <expresión>` con `_ => e` relanza la misma excepción; si el analizador lo marca, usar un `if`
con `throw;`. Edición: primero `actorVigente.ExigirAdministradorActivoAsync` (AC 20); después buscar
con seguimiento (`FindAsync`) o `NoEncontradoException("No existe el
médico pedido.")`; `var cambios = medico.CambiarDatos(...)`; si `cambios.Count == 0` → `return false`
sin guardar; validar **sólo** los campos de `cambios` (AC 17; para el usuario, sólo si el nuevo valor no
es `null`); `Registrar(MedicoEditado, "Medico", id, new { cambios = cambios.ToDictionary(c => c.Campo, c => new { antes = c.Antes, despues = c.Despues }) })`;
un solo `GuardarTraduciendoCarrerasAsync` (el id ya existe: no hace falta transacción explícita).
Si una validación falla después de `CambiarDatos`, la entidad queda cambiada en memoria pero nunca se
guarda: el contexto es del pedido y se descarta.

`ExigirUsuarioVinculableAsync` consulta `Users` + `UserRoles` + `Roles` del mismo contexto (no hace
falta `UserManager`): existe, `Activo`, exactamente un rol y es `Roles.Medico`; si no →
`ReglaDeNegocioException` del AC 11. Si `Medicos.Any(m => m.UsuarioPersonalId == usuario && m.Id != excepto)`
→ `ConflictoException` del AC 12 con el `NombreCompleto` de ese médico.

Estado de agenda en `ListarAsync` y `ObtenerAsync`:

```csharp
// Todavía no existen agendas: ningún médico tiene una (FR-13). La 4.1 (su AC 30) reemplaza esta
// asignación por IConsultaAgendas.EstadosDeAgendaAsync (contrato, AC 23 de la 3.2).
Agenda = SituacionDeAgenda.SinAgenda,
```

**Persistencia — `MedicoConfiguracion`**:

```csharp
public const string IndiceMatricula = "ix_medicos_matricula";          // nombres ya en snake_case:
public const string IndiceUsuario = "ix_medicos_usuario_personal_id";  // los compara la traducción

medico.HasKey(m => m.Id);                                   // long → identity
medico.Property(m => m.Nombre).HasMaxLength(Medico.LargoMaximoNombre).IsRequired();
medico.Property(m => m.Apellido).HasMaxLength(Medico.LargoMaximoNombre).IsRequired();
medico.Property(m => m.Matricula).HasMaxLength(Medico.LargoMaximoMatricula).IsRequired();
medico.HasIndex(m => m.Matricula).IsUnique().HasDatabaseName(IndiceMatricula);
medico.HasIndex(m => m.UsuarioPersonalId).IsUnique().HasDatabaseName(IndiceUsuario); // varios NULL valen en PostgreSQL
medico.HasOne<Especialidad>().WithMany().HasForeignKey(m => m.EspecialidadId).OnDelete(DeleteBehavior.Restrict);
medico.HasOne<UsuarioPersonal>().WithMany().HasForeignKey(m => m.UsuarioPersonalId).OnDelete(DeleteBehavior.Restrict);
medico.Ignore(m => m.NombreCompleto);
```

Al final del `Up` de la migración `AgregarMedicos` (mismo patrón que `AgregarBandejaDeSalida`):

```csharp
// Los médicos no se borran (FR-13 sólo pide alta y edición). 01-roles.sh le da DELETE por defecto
// a toda tabla nueva: por eso REVOKE ALL y después sólo lo que usa.
migrationBuilder.Sql("""
    REVOKE ALL ON TABLE medicos FROM turnos_app;
    GRANT SELECT, INSERT, UPDATE ON TABLE medicos TO turnos_app;
    DO $$ BEGIN
      EXECUTE format('GRANT USAGE ON SEQUENCE %s TO turnos_app', pg_get_serial_sequence('medicos', 'id'));
    END $$;
    """);
```

**Web**:

- `RutasPersonal`: `public const string NuevoMedico = "/personal/admin/medicos/nuevo";` y
  `public static string FichaMedico(long id) => $"/personal/admin/medicos/{id}";`.
- El actor de la sesión sale de `ActorDeSesion.Desde(HttpContext.User)` de la 2.2 (firma confirmada en
  la Task 1); el caso de uso lo confirma en la base con `IActorVigente`.
- `Medicos.razor`: `@page "/personal/admin/medicos"`, `@layout LayoutPersonal`,
  `@attribute [Authorize(Roles = Roles.Administrador)]`, `<h1>Médicos</h1>`, buscador
  `<form method="get">` con `<label for="buscar">Buscar</label>` (un GET no necesita token) y
  `[SupplyParameterFromQuery(Name = "buscar")]`; dibuja `<TablaMedicos Medicos="..." Buscado="..." />`.
- `TablaMedicos.razor`: parámetros `IReadOnlyList<MedicoListado> Medicos` y `string? Buscado`; los dos
  vacíos del AC 3 y la traducción de `SituacionDeAgenda` a texto del AC 4: `SinAgenda` con
  `RigeDesde` → `$"Sin agenda (rige desde {RigeDesde:dd/MM})"` con `CultureInfo.InvariantCulture`.
- `MedicoNuevo.razor` (`@page "/personal/admin/medicos/nuevo"`) y `FichaMedico.razor`
  (`@page "/personal/admin/medicos/{Id:long}"`, `[Parameter] public long Id`; la restricción `:long`
  evita que `nuevo` choque con la ficha): `EditForm Model="Input" method="post" FormName="medico"`, `DataAnnotationsValidator`,
  `<CamposMedico ... />` y el cartel `role="alert"` arriba. Las listas de especialidades y usuarios se
  cargan en **cada** pedido (GET y POST): en SSR la página se vuelve a armar entera.
- `FormularioMedico` (modelo del formulario): `Nombre`, `Apellido`, `Matricula` (`string?`, con
  `[Required]` y `[StringLength]` y los mensajes del AC 7), `EspecialidadId` (`long?`, `[Required]`
  `Elegí una especialidad.`) y `UsuarioPersonalId` (`Guid?`, opcional: la opción `Sin usuario` tiene
  `value=""`). La regla de caracteres de la matrícula la da el dominio (un solo lugar para la
  expresión); el `[StringLength(20)]` del formulario mide lo escrito sin normalizar, así que el límite
  real lo pone el dominio.
- Manejo en las páginas, con el mecanismo de la 2.2 (decisión 1; no se atrapa nada más: lo
  inesperado va al manejador de la 1.2):

```csharp
private async Task GuardarAsync()
{
    long id;
    try
    {
        id = await Gestion.CrearAsync(Input!.ADatos(), ActorDeSesion.Desde(HttpContext.User), HttpContext.RequestAborted);
    }
    catch (Exception e) when (ManejadorGlobalDeErrores.EsEsperable(e))   // sólo las del dominio
    {
        // UX §8: el cartel arriba del formulario con la regla; el código es el del manejador (409/400/404/403).
        // FijarEstadoEnPagina difiere el 404: con Blazor .NET 10 una página SSR en 404 sale sin cuerpo.
        ManejadorGlobalDeErrores.FijarEstadoEnPagina(HttpContext, e);
        error = e.Message;
        return;
    }
    Navegacion.NavigateTo($"{RutasPersonal.FichaMedico(id)}?resultado=alta");   // fuera del try (SSR)
}
```

  En `FichaMedico.razor`, `ObtenerAsync` devuelve `null` →
  `ManejadorGlobalDeErrores.FijarEstadoEnPagina(HttpContext, new NoEncontradoException(MedicoInexistente))`
  y el texto del AC 18 (`MedicoInexistente = "No existe el médico pedido."`). **Nunca**
  `HttpContext.Response.StatusCode = 404` a mano: con Blazor .NET 10 la página saldría en `404` sin
  cuerpo, sin el texto ni el enlace (T28 lo detecta). En GET y en POST; en POST, `EditarAsync` lanza
  `NoEncontradoException` y se atrapa igual, por el mismo `FijarEstadoEnPagina`. `?resultado=` se traduce con un `switch` de tres valores conocidos a los textos de los
  AC 6 y 16; cualquier otro valor no muestra nada.
- A4 (2.2): la página inyecta `IGestionMedicos`, llama una vez a `MedicosVinculadosAsync` y, en la
  columna **Médico vinculado** de cada fila: si el usuario está en el diccionario, `Apellido, Nombre`;
  si no está y su `Rol` es `Roles.Medico`, `Sin vincular` (decisión 17); si no, vacío.

### Salvaguarda de BD (obligatoria antes de implementar las pruebas de Web)

Las migraciones las aplica **el arranque** (nunca `dotnet ef database update`):

```bash
docker compose up -d --build          # la app aplica AgregarMedicos al arrancar
docker compose exec db psql -U postgres -d hospital_turnos
```

1. `\d medicos` → columnas `id` (bigint, identity), `nombre`, `apellido` (varchar 100), `matricula`
   (varchar 20), `especialidad_id`, `usuario_personal_id` (uuid, nulo); índices únicos
   `ix_medicos_matricula` e `ix_medicos_usuario_personal_id`; FK a `especialidades` y a
   `asp_net_users`. Anotar los nombres reales de las FK.
2. `SELECT privilege_type FROM information_schema.role_table_grants WHERE grantee = 'turnos_app' AND table_name = 'medicos' ORDER BY 1;`
   → `INSERT`, `SELECT`, `UPDATE` (sin `DELETE`).
3. `SELECT has_table_privilege('turnos_app', 'registros_auditoria', 'UPDATE');` → sigue `f`.
4. Si CUALQUIER resultado no coincide → DETENERSE → corregir la story primero. **No** tapar un
   permiso faltante con otro `GRANT` sin entender por qué faltó.

### Archivos a crear/modificar

| Acción | Archivo |
|---|---|
| Crear | Los de `files_to_create` del frontmatter |
| Modificar | `src/HospitalTurnos.Dominio/Auditoria/AccionesAuditoria.cs` (2 constantes, AC 22) |
| Modificar | `src/HospitalTurnos.Infraestructura/Persistencia/HospitalDbContext.cs` (`DbSet<Medico>` y `ApplyConfiguration`) |
| Modificar | `.../Migraciones/HospitalDbContextModelSnapshot.cs` (lo regenera `dotnet ef migrations add`) |
| Modificar | `src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs` (`AddScoped<IGestionMedicos, GestionMedicosEnBase>()`) |
| Modificar | `src/HospitalTurnos.Web/Personal/RutasPersonal.cs` (`NuevoMedico`, `FichaMedico(long id)`) |
| Modificar | `src/HospitalTurnos.Web/Components/Personal/Secciones/SeccionesAdministracion.razor` (quitar el `@page` de médicos) |
| Modificar | `src/HospitalTurnos.Web/Components/_Imports.razor` (`@using` de `Aplicacion.Catalogo` y `Dominio.Agendas`) |
| Modificar | `src/HospitalTurnos.Web/Components/Personal/Administracion/UsuariosPersonal.razor` (A4 de la 2.2: columna «Médico vinculado» con `Sin vincular`, AC 14) |
| Modificar | `tests/HospitalTurnos.Web.Tests/Personal/SesionYPermisosHttpTests.cs` (una ruta más en `RutasYaConstruidas`, AC 24) |

### Lo que NO hay que hacer

- **NO hacer interactivas las páginas** (`@rendermode`): son SSR como S0; la interactividad empieza en
  la 5.5.
- **NO calcular un estado de agenda distinto de `Sin agenda`** ni crear tablas, entidades o consultas
  de agenda: es de la 4.1 (contrato, AC 23). Sí se dibujan desde ya los cuatro textos (AC 4, T22).
- **NO agregar "Corregir agenda" ni la agenda en sólo lectura** a la ficha: 4.4 (decisión 10).
- **NO volver a crear las piezas de la 2.2 ni el interceptor de la 3.1** (`ActorDeSesion`, otro mapeo
  excepción → código, otro control de permiso, otra ayuda de POST, otra carpeta de páginas, otro
  doble de carrera): se reusan con su firma.
- **NO construir `GestionMedicosEnBase` con `new` en las pruebas**: se resuelve del contenedor (así la
  4.1 puede agregarle un parámetro sin tocar estas pruebas).
- **NO desactivar ni borrar médicos**, ni dar `DELETE` a `turnos_app` sobre `medicos`.
- **NO crear usuarios ni cambiar roles desde A1**: es de la 2.2 (A4). A1 sólo vincula uno existente.
- **NO poner una navegación `Medico.Usuario` hacia `UsuarioPersonal` en el dominio**: Dominio no
  referencia a Infraestructura. Sólo el `Guid`.
- **NO atrapar `Exception` sin el filtro `EsEsperable`** ni envolver `NavigateTo` en un `try`: en SSR `NavigateTo` usa una
  excepción de navegación, y lo inesperado tiene que llegar al `ManejadorGlobalDeErrores` (1.2).
- **NO mostrar en la página lo que venga en `?resultado=`**: sólo los tres textos fijos.
- **NO armar el `LIKE` concatenando texto en SQL**: `EF.Functions.ILike` con el patrón escapado y
  parámetros.
- **NO contar filas totales** de `medicos`, `especialidades` ni `registros_auditoria` en las pruebas:
  la base es compartida (filtrar por la marca propia).
- **NO usar `dotnet ef database update`** ni `DateTime.Now`/`UtcNow` (contrato §Hora: la hora de la
  auditoría la pone `AuditoriaEnBase` con el `TimeProvider`).
- **NO crear el proyecto E2E**.

### Como verificar que funciona

```bash
cd ~/SourceCode/hospital-turnos

# 1. Migración (proyecto de migraciones = Infraestructura, arranque = Web). No se conecta a la base.
dotnet ef migrations add AgregarMedicos \
  --project src/HospitalTurnos.Infraestructura --startup-project src/HospitalTurnos.Web \
  --output-dir Persistencia/Migraciones
# Agregar a mano el bloque de permisos al final de Up (Implementación concreta).

# 2. Pruebas de esta story (Docker para Testcontainers)
dotnet test --filter "Story=3.2"
# Esperado: 31 pruebas declaradas (más los casos de cada [Theory]), 0 fallas
dotnet test --filter "Story=2.1"          # la T19 ajustada sigue en verde (AC 24)

# 3. Suite completa y CI local
dotnet test
bash scripts/ci/local.sh; echo "rc=$?"    # rc=0

# 4. Verificación manual en el navegador
docker compose up -d --build
open http://localhost:8080/personal/admin/medicos
# Esperado, en este orden (como administrador):
#  a. "Médicos" con el mensaje de "todavía no hay médicos" (base nueva) y el enlace "Nuevo médico"
#  b. "Nuevo médico" sin especialidades activas: el aviso con el enlace a Especialidades (crear una en A2)
#  c. alta de "Pereyra, Martín", matrícula " mp 1234 ": vuelve a la ficha con "Médico dado de alta."
#     y la matrícula "MP 1234"; en el listado figura con "Sin agenda"
#  d. otra alta con la matrícula "mp  1234": cartel "Ya hay un médico con la matrícula MP 1234."
#     y los datos siguen escritos
#  e. crear en A4 un usuario con rol Médico: A4 muestra "Sin vincular" en "Médico vinculado";
#     en la ficha de Pereyra elegirlo: "Cambios guardados."; A4 muestra "Pereyra, Martín" en la
#     columna; "Nuevo médico" ya no lo ofrece
#  f. buscar "pere" y "%": el primero encuentra a Pereyra, el segundo no encuentra nada
#  g. entrar como recepción y pedir /personal/admin/medicos/nuevo: "No tenés permiso..."

# 5. La auditoría quedó escrita
docker compose exec db psql -U postgres -d hospital_turnos \
  -c "SELECT cuando, quien, rol, canal, accion, entidad_id, detalle FROM registros_auditoria WHERE entidad_tipo = 'Medico' ORDER BY id DESC LIMIT 10;"
```

## Dev Agent Record

### Agent Model Used

Claude Opus 5.5 (Amelia, pipeline TYCON), sesión 11 de BMAD-Hospital (2026-10-16).

### Debug Log References

- Task 1: piezas de la 2.2 y la 3.1 confirmadas por el validador contra `main` en `9a21b7f` (firmas de
  `ICatalogoEspecialidades`, `CatalogoEspecialidadesEnBase`, `InterceptorQueSeAdelanta`,
  `UsuariosDePrueba`, `IActorVigente`, `FormularioHttp.EnviarAsync`/`Estado`, `RutasYaConstruidas`).
  La 3.1 no siembra especialidades (T26 sólo necesita la base nueva).
- Línea de base: 300 pruebas en verde (Dominio 33, Aplicación 119, Web 148).
- Salvaguarda de BD (Compose): `medicos` con las columnas e índices únicos de la story; FK
  `fk_medicos_especialidades_especialidad_id` y `fk_medicos_asp_net_users_usuario_personal_id`
  (`RESTRICT`); `turnos_app` con `INSERT`, `SELECT`, `UPDATE`; `registros_auditoria` sigue sin `UPDATE`.
- Rojo por la razón correcta en cada capa: Dominio y Web no compilaban (faltaban `Medico` y
  `TablaMedicos`); Aplicación fallaba por el modelo con cambios sin migración.
- La ficha se dibuja una primera vez antes de leer el médico: el `EditForm` se dibuja sólo con el
  modelo listo (si no, `EditForm requires either a Model…` y el 500 genérico).
- Blazor dibuja la opción vacía de un `<select>` como `<option value>`: la prueba lo acepta (el
  navegador lo toma como valor vacío).

### Completion Notes List

- 31 pruebas de la story (los mismos 31 nombres de la tabla) en verde.
- Cartel de error con `FijarEstadoEnPagina` en las tres situaciones (regla del alta, regla de la
  ficha, médico inexistente en GET y POST); ningún `StatusCode` fijado a mano.
- La ficha dibuja su formulario también en un POST de un id inexistente, para que el envío llegue al
  caso de uso y responda el `404` del AC 18; el alta, también en un POST sin especialidades activas
  (cae en el `400` del AC 9, como dice el AC 10).
- En la ficha, la lista de usuarios incluye al vinculado hoy aunque ya no cumpla (desactivado
  después): así guardar sin tocarlo no lo desvincula (AC 17).
- Verificación en el navegador (Como verificar, paso 4): un script de Playwright maneja Chrome contra
  la app del Compose en cuatro grupos (pantalla, errores, carrera, base), registrados con
  `smoke-run.sh`: 0 fallas y la matriz por AC en 24 de 24 corridos en vivo. Incluye el 404 de la
  ficha con su cartel dentro de la pantalla y dos carreras de diez pedidos a la vez desde dos
  sesiones (matrícula y vínculo): un 302, nueve 409 y una sola fila en cada una. Capturas en
  `Sesiones/img/11-*.png`.
- Legibilidad (tester no técnico): la guía `docs/qa/3.2-pasos.md` se corrigió con sus marcas. En
  las pantallas marcó tres cosas que el docente aceptó sin cambios: los carteles 400 y 409 del
  vínculo tomados de pedidos armados a mano (la lista no ofrece ese valor, así que el campo no lo
  muestra) y el cartel 403 del administrador recién desactivado (el formulario conserva lo escrito y
  el texto lo fija el AC 20, como en la 3.1).

### File List

- Crear: los 21 de `files_to_create` (migración `20261017014314_AgregarMedicos`).
- Modificar: los 9 de `files_to_modify`.

### Change Log

- 2026-10-16: Story escrita (Mary) contra `main` en `c3413cc`, antes de que existan la 2.2 y la 3.1
  (sprint 2, acción A-1 de la retrospectiva). 24 AC, 31 verificaciones, 4 decisiones abiertas.
- 2026-10-16: Corregida con la revisión adversarial (`revision-sprint-2.md`) y las decisiones del
  docente. Contrato único del estado de agenda con la 4.1 en el AC 23 (C-1): `EstadoAgendaMedico` y
  `SituacionDeAgenda` (nuevo, con `RigeDesde` para `Sin agenda (rige desde dd/MM)`, decisión 8) en
  `Dominio/Agendas/`, firma de `GestionMedicosEnBase` con cuatro parámetros (la 4.1 agrega el quinto)
  y la 4.1 conecta el estado real. `Medico.Id` `long` (I-4, decisión 15). Registro `DatosFichaMedico`
  y `RutasPersonal.NuevoMedico` (I-5). Páginas en `Components/Personal/Administracion/` (I-2). Se
  reusan las piezas de la 2.2 y el interceptor de la 3.1, sin crear otras (I-1); permiso en la base
  con `IActorVigente` (I-3, decisión 16; T15 suma el administrador desactivado). `Sin vincular` en
  A4 y texto para 4.2/8.1 (I-7, decisión 17). Herencia de la 5.3 en el AC 23 y en la trampa de la
  3.1 (I-8). AC 23 declarativo (M-4); T30 suma la ficha (M-5); M-6 y M-10 aceptados en
  "Decisiones"; cita de T26 corregida (M-12). Siguen 24 AC y 31 verificaciones; ninguna decisión
  abierta.
- 2026-10-16 (sesión 11): revalidada contra `main` en `9a21b7f` antes de implementar. El cartel de error
  usa `ManejadorGlobalDeErrores.FijarEstadoEnPagina(HttpContext, e)` (pieza de la 2.2) y la ficha
  inexistente le pasa una `NoEncontradoException`: nunca un `Response.StatusCode` a mano, porque con
  Blazor .NET 10 una página SSR en `404` sale sin cuerpo. "Que existe hoy" al día con la 2.2 y la 3.1
  (migraciones, constantes de auditoría, secciones en construcción, ayudas de prueba) y anclas por
  nombre de método en vez de número de línea.
- 2026-10-16 (sesión 11): implementada. Tres piezas chicas que la story no nombraba, sin regla nueva:
  `Medico.MensajeMatricula` y `Medico.MatriculaValida` (el formulario muestra el error de la matrícula
  debajo del campo, AC 7, con la expresión del dominio; por eso la matrícula del formulario no lleva
  `[StringLength]`, que mediría lo escrito sin normalizar) e `IGestionMedicos.LargoMaximoBusqueda`
  (el límite de 100 del AC 2, compartido por la pantalla y el caso de uso).
- 2026-10-16 (sesión 11, revisores): el AC 23 queda asignado a T18 y T22 (decisión del docente, como el
  contrato de la 3.1), para que el control de cobertura de criterios no lo deje sin prueba.


## Review Follow-ups (Advisors)

<!-- tycon-pipeline: generado por Step 6/7. Cada item = finding advisor no resuelto (CRITICAL waived o WARNING). Al resolver el finding en el código, marcá [x]. Step 0/1 detecta los abiertos al retomar (pipeline.js followups-status). -->

- [x] **[CRITICAL]** ACCOV-C5 — `docs/stories/3-2-medicos.md` — AC 23 asignado a T18 y T22 (decisión del docente) <!-- fkey:ACCOV-C5|docs/stories/3-2-medicos.md -->
- [x] **[WARNING]** VAL-W15 — `docs/stories/3-2-medicos.md` — tasks tildadas; queda la verificación en vivo <!-- fkey:VAL-W15|docs/stories/3-2-medicos.md -->
