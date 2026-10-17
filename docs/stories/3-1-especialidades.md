---
epic: 3
story: "3.1"
title: "Especialidades: alta, desactivación y reactivación (pantalla A2)"
status: done
sprint: 2
priority: "P0"
approach: "[ADAPT]"
depends_on: ["1.1", "1.2", "1.3", "2.1", "2.2"]
files_to_create:
  - "src/HospitalTurnos.Dominio/Catalogo/Especialidad.cs"
  - "src/HospitalTurnos.Aplicacion/Catalogo/ICatalogoEspecialidades.cs"
  - "src/HospitalTurnos.Infraestructura/Catalogo/CatalogoEspecialidadesEnBase.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/EspecialidadConfiguracion.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/<marca>_AgregarEspecialidades.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/<marca>_AgregarEspecialidades.Designer.cs"
  - "src/HospitalTurnos.Web/Components/Personal/Administracion/Especialidades.razor"
  - "tests/HospitalTurnos.Dominio.Tests/Catalogo/EspecialidadTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Catalogo/CatalogoEspecialidadesTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Catalogo/ContratoTurnosFuturosTests.cs"
  - "tests/HospitalTurnos.Aplicacion.Tests/Ayudas/InterceptorQueSeAdelanta.cs"
  - "tests/HospitalTurnos.Web.Tests/Catalogo/EspecialidadesHttpTests.cs"
  - "tests/HospitalTurnos.Web.Tests/Catalogo/EspecialidadesPaginaTests.cs"
files_to_modify:
  - "src/HospitalTurnos.Dominio/Auditoria/AccionesAuditoria.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/HospitalDbContext.cs"
  - "src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/HospitalDbContextModelSnapshot.cs"
  - "src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs"
  - "src/HospitalTurnos.Web/Components/Personal/Secciones/SeccionesAdministracion.razor"
  - "tests/HospitalTurnos.Web.Tests/Personal/SesionYPermisosHttpTests.cs"
---

# Story 3.1: Especialidades: alta, desactivación y reactivación (pantalla A2)

Status: done

## Story

Como administradora del Hospital Las Acacias (Lucía),
quiero dar de alta las especialidades que atiende el hospital y desactivar las que dejan de atenderse,
para que los médicos se carguen sólo en especialidades vigentes (story 3.2) y los pacientes y la
recepción elijan sólo entre ellas (stories 5.2, 5.5 y 6.4).

## Acceptance Criteria

> Pantalla A2 de `docs/03-ux.md` §7: "Lista simple con alta y desactivar". Ruta
> `/personal/admin/especialidades` (ya definida en `RutasPersonal.Especialidades` por la 2.1), sólo
> para el rol `Administrador`. Mensajes en castellano rioplatense.
>
> **Esta story está escrita con las decisiones que tomó el docente el 2026-10-16** (sección
> "Decisiones tomadas por el docente", al final de Dev Notes; numeradas como en
> `revision-sprint-2.md`). No queda ninguna abierta.
>
> **Campos de auditoría de esta story** (valen para los AC 5, 9 y 11; cumplen los campos obligatorios
> de la story 1.3). El actor es el que devuelve `IActorVigente` (2.2): leído de la base, no de la
> sesión.
>
> | Campo | Valor |
> |---|---|
> | `UsuarioId` | id del administrador que hizo el cambio |
> | `Quien` | su `NombreCompleto`, leído de la base |
> | `Rol` | `Administrador` |
> | `Canal` | `CanalAuditoria.Personal` (contrato §Auditoría: "administración de … catálogo") |
> | `EntidadTipo` | `"Especialidad"` |
> | `EntidadId` | id de la especialidad, como texto |
> | `Detalle` | `{"nombre":"<Nombre tal como quedó guardado>"}` |

### Reglas del dominio — FR-12, NFR-10

1. **Given** un nombre con espacios de más (`"  Clínica   médica "`) **When** se crea una
   `Especialidad` **Then** nace con `Activa = true` y `Nombre = "Clínica médica"` (sin espacios al
   principio ni al final, y los espacios internos repetidos quedan en uno). **Given** un nombre vacío,
   sólo con espacios, o de más de 80 caracteres después de limpiarlo **Then** lanza
   `ReglaDeNegocioException` con `Ingresá el nombre de la especialidad.` o
   `El nombre de la especialidad tiene como máximo 80 caracteres.` respectivamente.
2. Dos nombres son **la misma especialidad** si coinciden sin distinguir mayúsculas, tildes ni
   espacios repetidos: `"Cardiología"`, `"cardiologia"` y `" CARDIOLOGÍA "` dan la misma clave
   `NombreNormalizado = "CARDIOLOGIA"`. La clave se calcula en el dominio
   (`Especialidad.Normalizar(nombre)`) y se guarda en su propia columna con índice único.
3. **Given** una especialidad activa **When** se la desactiva **Then** queda `Activa = false`.
   **Given** una ya desactivada **When** se la desactiva otra vez **Then** lanza `ConflictoException`
   con `La especialidad Cardiología ya está desactivada.` (con su nombre).
4. **Given** una especialidad desactivada **When** se la reactiva **Then** queda `Activa = true`.
   **Given** una activa **When** se la reactiva **Then** lanza `ConflictoException` con
   `La especialidad Cardiología ya está activa.` (decisión 6).

### Alta — FR-12

5. **Given** un administrador con sesión y un nombre que no existe **When** crea la especialidad
   **Then** se guarda una fila en `especialidades` con `activa = true` y, en la **misma** transacción,
   un `RegistroAuditoria` con `Accion = AccionesAuditoria.EspecialidadCreada` y los campos de la tabla
   de arriba. Si algo falla al guardar, no queda ni la especialidad ni el registro.
6. **Given** que ya existe una especialidad con la misma clave del AC 2 **When** se intenta crear otra
   **Then** lanza `ConflictoException`, no se guarda nada y no se audita. El mensaje usa el nombre
   **guardado**, no el tipeado:
   - si la existente está activa: `Ya existe la especialidad Cardiología.`
   - si está desactivada: `Ya existe la especialidad Cardiología, desactivada. Reactivala desde la lista.`
7. **Given** dos altas **simultáneas** con la misma clave (dos administradores, o un doble clic que
   llega dos veces) **When** las dos pasan la verificación previa **Then** el índice único de la base
   rechaza la segunda (`23505` sobre `ix_especialidades_nombre_normalizado`), el adaptador la traduce
   a la **misma** `ConflictoException` del AC 6, y queda exactamente una fila y un registro de
   auditoría. Nunca un `500`.
8. **Given** el formulario de alta de A2 con el nombre vacío o de más de 80 caracteres **When** se
   envía **Then** la respuesta es `200` con la misma A2, el mensaje debajo del campo
   (`Ingresá el nombre de la especialidad.` / `El nombre de la especialidad tiene como máximo 80 caracteres.`),
   no se guarda nada y no se audita. La validación corre también en el servidor (NFR-10: un POST
   armado a mano da el mismo resultado) y, además, el dominio la repite (AC 1) para cualquier otro
   llamador del caso de uso.

### Desactivar y reactivar — FR-12

9. **Given** una especialidad activa **When** el administrador la desactiva **Then** queda
   `activa = false` (la fila **no** se borra), se audita `AccionesAuditoria.EspecialidadDesactivada`
   con los campos de la tabla, y deja de aparecer en `ListarActivasAsync` (AC 16). Los médicos que la
   tengan asignada (story 3.2) la conservan: FR-12 condiciona la desactivación a los turnos futuros,
   no a los médicos.
10. **Hoy no hay turnos** (la tabla de turnos la crea la story 5.3), así que ninguna especialidad
    puede tener turnos futuros y desactivar **no consulta** turnos. No es un atajo: es lo que dice
    FR-12 sobre el sistema de hoy. Para que la regla no se pierda, una prueba (T15) falla en cuanto el
    modelo de datos tenga una entidad llamada `Turno` **o** cualquier entidad mapeada a una tabla
    `turnos` (aunque la clase se llame distinto), con un mensaje que manda a cumplir el contrato del
    AC 17 y la herencia de la 3.2 (decisiones 5 y 7, I-8 de la revisión).
11. **Given** una especialidad desactivada **When** el administrador la reactiva **Then** queda
    `activa = true`, se audita `AccionesAuditoria.EspecialidadReactivada` y vuelve a aparecer en
    `ListarActivasAsync` (decisión 6).
12. **Given** un id que no existe **When** se pide desactivar o reactivar **Then** lanza
    `NoEncontradoException` con `La especialidad ya no existe. Actualizá la lista.` y no se audita.

### Permisos — FR-11, R-13, Arq. §7.7

13. El control está **en el caso de uso**, no sólo en la pantalla (Arq. §7.7), y se mira **en la
    base** con el servicio compartido de la 2.2,
    `IActorVigente.ExigirAdministradorActivoAsync(actor, mensaje, ct)` (decisión 16): la sesión puede
    atrasar hasta 5 minutos. **Given** un actor que **no** es un administrador activo en la base (otro
    rol, un administrador desactivado hace menos de 5 minutos, o un id que no existe) **When** llama a
    crear, desactivar o reactivar **Then** lanza `AccesoDenegadoException` con
    `Sólo el administrador puede modificar las especialidades.`, sin cambios ni auditoría. La pantalla
    sigue con su `[Authorize(Roles = Administrador)]`: otro rol recibe el `403` legible de la 2.1
    (AC 13 y prueba T19 de la 2.1, que sigue cubriendo esta ruta); el administrador recién
    desactivado que todavía tiene sesión recibe `403` con el texto de arriba en el cartel de A2.
14. **Given** un POST a A2 (alta o cambio de estado) sin el token antifalsificación **When** llega
    **Then** responde `400` y no cambia nada (mismo criterio que la 2.1, AC 8).

### Pantalla A2 — UX §7 y §8

15. **Given** un administrador con sesión **When** abre `/personal/admin/especialidades` **Then**
    recibe `200` con:
    - `<h1>Especialidades</h1>` (el mismo título que el menú; la 2.1 T19 lo verifica);
    - arriba, el formulario de alta: campo con etiqueta visible `Nombre` y botón `Agregar`;
    - debajo, una tabla con **todas** las especialidades, ordenadas por `NombreNormalizado`, con el
      nombre, el estado en texto (`Activa` / `Desactivada`, nunca sólo color) y un botón por fila:
      `Desactivar` si está activa, `Reactivar` si está desactivada. Los botones van en **un solo**
      formulario con token y mandan un campo `Accion` = `desactivar:<id>` o `reactivar:<id>`, como A4
      de la 2.2 (M-8 de la revisión);
    - sin especialidades: `Todavía no hay especialidades. Agregá la primera con el formulario de arriba.`
      (UX §8, estado vacío con la acción siguiente) y sin tabla.
    - **Resultado de cada acción**, en la misma página:
      - alta correcta → `200`, cartel `role="status"` con `Especialidad Traumatología agregada.`, el
        campo vacío y la fila nueva en la tabla;
      - desactivar / reactivar correcto → `200`, cartel `role="status"` con
        `Especialidad Cardiología desactivada.` / `… reactivada.` y la fila con su estado nuevo;
      - error de regla con conflicto (`ConflictoException`, AC 3, 4, 6 y 7) → `409`; regla de
        validación (`ReglaDeNegocioException`, AC 1, si un POST esquiva la validación del formulario)
        → `400`; un valor de `Accion` que la pantalla no ofrece (POST armado, por ejemplo
        `borrar:<id>`, o un id que no es número) → `400` con
        `No reconocemos esa acción. Recargá la página.`; id inexistente (AC 12) → `404`; actor que ya
        no es administrador activo (AC 13) → `403`. En todos, cartel `role="alert"` **arriba del
        formulario** con el texto de la regla (UX §8 "Error de regla de negocio") y la tabla al día.
        El campo de alta conserva lo tipeado. El código lo fija
        `ManejadorGlobalDeErrores.FijarEstadoEnPagina(HttpContext, e)` de la 2.2 (decisión 1): la
        página no escribe códigos a mano, y el `404` también llega con su cartel.

### Contrato con otras stories

16. **Hacia la story 3.2 (Médicos)** — la 3.2 **lee** este catálogo por el puerto
    `ICatalogoEspecialidades` (Aplicacion/Catalogo):
    - `ListarActivasAsync(ct)` → las activas, ordenadas por `NombreNormalizado`, como
      `EspecialidadResumen(long Id, string Nombre, bool Activa)`. Es la lista del selector de A1.
    - `BuscarAsync(long id, ct)` → la especialidad o `null`. La 3.2 la usa para validar **en el
      servidor** que el `EspecialidadId` recibido existe y está activa al dar de alta un médico o al
      cambiarle la especialidad (NFR-10).
    - El id es `long` (identity de PostgreSQL, como `RegistroAuditoria` y `MensajeSaliente`). La 3.2
      declara `Medico.EspecialidadId` (`long`) con clave foránea a `especialidades.id` y
      `OnDelete(DeleteBehavior.Restrict)`: una especialidad nunca se borra (AC 9 y AC 19).
    - Una especialidad **desactivada** sigue existiendo: los médicos que la tienen la conservan, y la
      ficha del médico (3.2) la muestra con su nombre. Qué hace la 3.2 al editar un médico cuya
      especialidad está desactivada lo decide la 3.2.
    - Las piezas compartidas son las de la 2.2 (su AC 26), con la misma firma para la 3.2:
      `ActorDeSesion.Desde(User)`, `EsEsperable`/`FijarEstadoEnPagina`, `IActorVigente`, la carpeta
      `Components/Personal/Administracion/`, `FormularioHttp.EnviarAsync` y `UsuariosDePrueba`.
      Esta story **agrega** una: `InterceptorQueSeAdelanta` (Aplicacion.Tests/Ayudas), el doble que
      prueba la carrera contra un índice único; la 3.2 lo reusa con su propio `INSERT`.
17. **Hacia la story 5.3 (Reserva) — FR-12 completo (decisión 5).** Cuando la 5.3 cree la entidad
    de turnos (se llame `Turno` o no), en la **misma** story:
    - (a) `CatalogoEspecialidadesEnBase.DesactivarAsync` cuenta los turnos **futuros** y **no
      cancelados** de la especialidad (futuro = después de `TimeProvider.AhoraLocal()`, contrato
      §Hora) y, si hay alguno, lanza `ConflictoException` con
      `No se puede desactivar Cardiología: tiene N turnos futuros.` — sin auditar ni cambiar nada;
    - (b) A2 muestra `Desactivar` **deshabilitado** en esas filas, con el motivo en el atributo
      `title` (UX A2: "con el motivo al pasar el mouse") y también como texto visible para el lector
      de pantalla (NFR-05);
    - (c) la reserva verifica **dentro de su transacción** que la especialidad del turno siga activa
      (si no, una desactivación y una reserva simultáneas dejarían un turno futuro en una
      especialidad desactivada);
    - (d) reemplaza la prueba-trampa T15 por las pruebas reales de (a) y (c), con `FakeTimeProvider`,
      **y** las de la herencia de la 3.2 (su AC 23: no se cambia la especialidad de un médico con
      turnos futuros, decisión 7).
    Si la 5.3 guarda la especialidad en el turno o la toma del médico es decisión de la 5.3. Para que
    esta herencia no dependa de leer stories viejas: la trampa T15 salta por el nombre de la clase
    **o** de la tabla, y su mensaje nombra las dos obligaciones; además, el plan tiene que decir en la
    fila de la 5.3 "hereda: 3.1 AC 17 y 3.2 AC 23 (cambio de especialidad)" (cambio del docente al
    plan, I-8 (b) de la revisión).
18. **Hacia las stories 5.2, 5.5 y 6.4** — los selectores de especialidad de R1 y de P6 (paso 1)
    usan `ListarActivasAsync`: una especialidad desactivada no se ofrece para turnos nuevos.

### No-regresión

19. Siguen en verde las pruebas de las stories 1.1 a 2.2. En particular, la 2.1 T19
    (`Secciones_SegunRol_PermiteLasSuyasYDevuelve403LegibleEnLasAjenas`) sigue pasando las 33
    combinaciones: para `/personal/admin/especialidades` ya **no** exige el texto
    `Esta sección se habilita en una próxima versión.` (la sección ahora existe: se suma a la lista
    `RutasYaConstruidas` que creó la 2.2) y sigue exigiendo el
    `<h1>Especialidades</h1>` y el `403` para los otros roles. `turnos_app` **no** tiene `DELETE` sobre
    `especialidades`, y la protección de `registros_auditoria` (1.3) no se toca.

### Tests (23 verificaciones)

| # | Test | AC | Nivel | Escenario | Resultado esperado |
|---|------|----|-------|-----------|--------------------|
| T1 | `Constructor_NombreValido_NaceActivaConElNombreLimpio` | 1 | unit | `[Theory]`: `"Traumatología"`, `"  Clínica   médica "` | `Activa = true`; `Nombre` = `"Traumatología"` / `"Clínica médica"` |
| T2 | `Constructor_NombreVacioOLargo_LanzaReglaDeNegocio` | 1 | unit | `[Theory]`: `""`, `"   "`, 81 letras `a` | `ReglaDeNegocioException` (y **no** `ConflictoException`) con el texto del AC 1 que corresponde |
| T3 | `Normalizar_MayusculasTildesYEspacios_DevuelveLaMismaClave` | 2 | unit | `[Theory]`: `"Cardiología"`, `"cardiologia"`, `" CARDIOLOGÍA "`, `"Cardio  logía"` | Las tres primeras → `"CARDIOLOGIA"`; la cuarta → `"CARDIO LOGIA"` (distinta) |
| T4 | `Desactivar_SegunEstado_DesactivaOLanzaConflicto` | 3 | unit | `[Theory]`: (a) activa; (b) ya desactivada | (a) `Activa = false`; (b) `ConflictoException` con `La especialidad Cardiología ya está desactivada.` y sigue `false` |
| T5 | `Reactivar_SegunEstado_ReactivaOLanzaConflicto` | 4 | unit | `[Theory]`: (a) desactivada; (b) activa | (a) `Activa = true`; (b) `ConflictoException` con `La especialidad Cardiología ya está activa.` |
| T6 | `CrearAsync_Administrador_GuardaActivaYRegistraAuditoria` | 5,16 | integration | Actor = administrador propio (`UsuariosDePrueba`, nombre `Admin <marca>`); nombre `Esp <marca>` | Devuelve el id; la fila tiene `activa = true` y `nombre_normalizado` = clave del AC 2; `BuscarAsync(id)` la devuelve; 1 fila de auditoría con `entidad_id` = id, `accion = especialidad.creada`, `canal = Personal`, `rol = Administrador`, `usuario_id` = id del administrador, `quien = Admin <marca>` (leído de la base), `entidad_tipo = Especialidad`, `detalle = {"nombre":"Esp <marca>"}` |
| T7 | `CrearAsync_NombreRepetido_LanzaConflictoSinGuardarNiAuditar` | 6 | integration | `[Theory]` con una especialidad propia `Neumonología <marca>` creada antes: (a) activa, se intenta `neumonologia <marca>`; (b) desactivada, se intenta `NEUMONOLOGÍA <marca>` | `ConflictoException` con el texto del AC 6 para (a) / (b), con el nombre **guardado**; sigue habiendo 1 fila con esa clave; ninguna fila de auditoría `especialidad.creada` nueva para esa clave |
| T8 | `CrearAsync_OtraAltaGanaLaCarrera_TraduceElIndiceUnicoAConflicto` | 5,7 | integration | Contexto con `InterceptorQueSeAdelanta` (con el `INSERT` de una especialidad con la misma clave): justo antes del `SaveChanges`, otra conexión la inserta | `ConflictoException` con `Ya existe la especialidad <nombre>.` (no `DbUpdateException`); 1 sola fila con esa clave; 0 filas de auditoría `especialidad.creada` del intento perdedor (el registro no se guarda sin el cambio) |
| T9 | `DesactivarAsync_Activa_LaDesactivaAuditaYSaleDeLasActivas` | 9,10,16 | integration | Especialidad propia activa | Fila con `activa = false` (sigue existiendo); 1 fila de auditoría `especialidad.desactivada` con los campos de la tabla; `ListarActivasAsync` no la trae; `BuscarAsync` la trae con `Activa = false` |
| T10 | `ReactivarAsync_Desactivada_LaReactivaAuditaYVuelveALasActivas` | 11,16 | integration | Especialidad propia desactivada | `activa = true`; 1 fila `especialidad.reactivada`; `ListarActivasAsync` la trae |
| T11 | `CambiarEstadoAsync_IdInexistenteOEstadoRepetido_LanzaSinAuditar` | 3,4,12 | integration | `[Theory]`: (a) desactivar id `long.MaxValue`; (b) reactivar id `long.MaxValue`; (c) desactivar una ya desactivada; (d) reactivar una activa | (a, b) `NoEncontradoException` con el texto del AC 12; (c, d) `ConflictoException` con el texto del AC 3 / 4; en (c, d) 0 filas de auditoría nuevas con su id y el estado no cambió |
| T12 | `Escrituras_ActorQueNoEsAdministradorActivo_LanzaAccesoDenegadoSinCambios` | 13 | integration | `[Theory]` actores (a) usuario propio `Recepcionista`; (b) usuario propio `Medico`; (c) administrador propio con `Activo = false` en la base (su actor sigue diciendo `Administrador`, como una sesión atrasada); (d) `UsuarioId` = Guid nuevo con rol `Administrador` × operaciones crear / desactivar / reactivar (12 casos) sobre especialidades propias | `AccesoDenegadoException` con el texto del AC 13; ninguna fila nueva ni cambio de `activa`; 0 filas de auditoría con su id o su nombre |
| T13 | `ListarActivasAsync_ConActivasYDesactivadas_DevuelveSoloActivasOrdenadas` | 16,18 | integration | Tres propias con la misma marca: `<marca> Ortopedia` (activa), `<marca> Clínica` (activa), `<marca> Alergia` (desactivada) | Filtrando por la marca: `[<marca> Clínica, <marca> Ortopedia]`, en ese orden, todas con `Activa = true` |
| T14 | `PermisosDeLaBase_TurnosApp_NoPuedeBorrarEspecialidades` | 9,19 | integration | `has_table_privilege('turnos_app', 'especialidades', …)` con `CadenaDuenio` | `SELECT`, `INSERT`, `UPDATE` → `true`; `DELETE` → `false`; `registros_auditoria` `UPDATE` → sigue `false` |
| T15 | `ModeloDeDatos_SinTurnos_ElContratoDeTurnosFuturosSigueVigente` | 10,17 | unit | `ContextoDePrueba.Crear(<cadena sin servidor>).Model` (no abre conexión); se recorren todas sus entidades | Ninguna tiene `ClrType.Name == "Turno"` **ni** `GetTableName() == "turnos"`. Si aparece alguna, falla con: `La story 5.3 agregó los turnos: cumplí el contrato de la story 3.1 (AC 17: desactivar una especialidad con turnos futuros) y la herencia de la 3.2 (AC 23: no cambiar la especialidad de un médico con turnos futuros), y reemplazá esta prueba por las de turnos futuros.` |
| T16 | `GetEspecialidades_Administrador_MuestraTablaOrdenadaConEstadoYBoton` | 15 | integration | `admin1`; dos propias `<marca> B` (activa) y `<marca> A` (desactivada) | `200`; `<h1>Especialidades</h1>`; la fila de `<marca> A` aparece **antes** que la de `<marca> B`; `<marca> A` con `Desactivada` y botón `Reactivar`; `<marca> B` con `Activa` y botón `Desactivar`; formulario con etiqueta `Nombre` y botón `Agregar` |
| T17 | `PostAlta_NombreNuevo_LaAgregaYAuditaConElAdministradorDeLaSesion` | 5,15 | integration | `admin1` (`Lucía Pérez`); POST alta `<marca> Traumatología` | `200`; `role="status"` con `Especialidad <marca> Traumatología agregada.`; la fila en la tabla; el campo `Nombre` vacío; 1 fila de auditoría con `usuario_id` = id de `admin1`, `quien = Lucía Pérez`, `rol = Administrador` |
| T18 | `PostAlta_NombreVacioOLargo_MuestraMensajeBajoElCampoSinGuardar` | 8 | integration | `[Theory]`: `""` y 81 caracteres con marca | `200`; mensaje del AC 8 debajo del campo; ninguna fila nueva con la marca; 0 filas de auditoría con la marca |
| T19 | `PostAlta_NombreRepetido_Devuelve409ConElCartelDeLaRegla` | 6,15 | integration | Especialidad propia `<marca> Pediatría` creada antes; POST alta `<marca> pediatria` | `409`; `role="alert"` con `Ya existe la especialidad <marca> Pediatría.`; el campo conserva `<marca> pediatria`; la tabla sigue con una sola fila de esa clave |
| T20 | `PostCambioDeEstado_ActivaYDesactivada_MuestraElEstadoNuevo` | 9,11,15 | integration | Especialidad propia activa; POST `Accion=desactivar:<id>`; después POST `Accion=reactivar:<id>` | Primero `200` con `Especialidad <nombre> desactivada.` y la fila con `Desactivada` / `Reactivar`; después `200` con `… reactivada.` y la fila con `Activa` / `Desactivar` |
| T21 | `PostCambioDeEstado_ErroresDeRegla_MuestraCartelConSuCodigo` | 3,12,13,15 | integration | `[Theory]`: (a) POST `desactivar:<id>` de una ya desactivada (armado a mano: la pantalla no ofrece ese botón); (b) POST `desactivar:9223372036854775807`; (c) POST `borrar:<id>` de una propia activa; (d) administrador propio ingresa, se lo desactiva en la base y, **sin** adelantar el reloj, envía `desactivar:<id>` de una propia activa | (a) `409` + `role="alert"` con el texto del AC 3; (b) `404` + el texto del AC 12; (c) `400` + `No reconocemos esa acción. Recargá la página.`; (d) `403` + el texto del AC 13; en todos, nunca `500` ni la página `/Error`, y en la base nada cambió |
| T22 | `PostEspecialidades_SinToken_Devuelve400SinCambios` | 14 | integration | `[Theory]`: (a) POST alta sin token con `<marca> Sin token`; (b) POST `desactivar:<id>` sin token de una propia activa | `400`; (a) ninguna fila con la marca; (b) sigue `activa = true`; 0 filas de auditoría de las dos |
| T23 | `PaginaEspecialidades_SinEspecialidades_MuestraElEstadoVacio` | 15 | unit | bUnit con un `ICatalogoEspecialidades` de prueba que devuelve lista vacía | Texto `Todavía no hay especialidades. Agregá la primera con el formulario de arriba.`; sin `<table>`; el formulario de alta está |

Todas llevan `[Trait("Story", "3.1")]` y un `[Trait("AC", "n")]` por AC de su columna.
T1–T5 van en `HospitalTurnos.Dominio.Tests`; T6–T15 en `HospitalTurnos.Aplicacion.Tests` (T15 sin
base); T16–T23 en `HospitalTurnos.Web.Tests` (T23 con bUnit).
Además se **modifica** (no se suma) la 2.1 T19, por el AC 19.

> El plan estimaba 6 pruebas; quedan 23 porque cada camino feliz lleva su modo de falla (repetido,
> carrera, inexistente, sin permiso, sin token, validación) y la pantalla se prueba por HTTP. Está en
> línea con lo medido en el sprint 1 (2,3 veces la estimación; ver retrospectiva).

**Por qué no hay E2E (regla 6.1 del formato).** El proyecto `tests/HospitalTurnos.E2E/` no existe
todavía (contrato §Proyectos; lo crea la story 10.1). A2 es una página de **renderizado estático en
el servidor** (SSR) sin JavaScript propio, igual que S0 en la 2.1: lo que hace el navegador es un GET
y un POST de formulario, que T16–T22 reproducen con `HttpClient` sobre la aplicación real, con token
antifalsificación y cookies; el estado vacío se prueba con bUnit (T23). La verificación manual en el
navegador queda en "Como verificar" y su resultado se anota en el Dev Agent Record.

#### Given/Fixture

- **Hora:** esta story no tiene ninguna regla que dependa de la hora. El único reloj que corre es el
  de la auditoría (`AuditoriaEnBase` usa el `TimeProvider` inyectado). Ninguna prueba espera tiempo
  real. El "futuro" de FR-12 lo medirá la 5.3 con `AhoraLocal()` y `FakeTimeProvider` (AC 17).
- **Base compartida y sin limpiadores** (contrato §Pruebas y §Auditoría): `especialidades` tiene
  índice único y no se borra, así que **cada prueba usa una marca propia** en los nombres
  (`<marca>` = `Guid.NewGuid().ToString("N")[..8]`) y filtra por ella o por los ids que creó. Nada de
  `TRUNCATE`.
- **Dominio.Tests (T1–T5):** sin base ni servicios; `new Especialidad("Cardiología")`.
- **Aplicacion.Tests (T6–T14):** `[Collection("Postgres")]` (la de la 1.3). Arman el
  `ServiceProvider` igual que `IngresoPersonalIdentityTests.Proveedor()` de la 2.1
  (`AddLogging`, `AgregarInfraestructura` y `AddIdentidadPersonal` con `ConnectionStrings:App = CadenaApp`),
  con un alcance por operación, como en la web. El actor es un **administrador real** de la base,
  creado con `UsuariosDePrueba.CrearAsync(proveedor, [Roles.Administrador], nombre: "Admin <marca>")`
  y `UsuariosDePrueba.ActorDe(admin, Roles.Administrador)` (ayuda de la 2.2): el caso de uso lo
  confirma en la base con `IActorVigente` (AC 13). T12 (c) crea el suyo con `activo: false`.
- **T8 — la carrera, sin depender de la suerte:** `InterceptorQueSeAdelanta : SaveChangesInterceptor`
  (nuevo, `tests/HospitalTurnos.Aplicacion.Tests/Ayudas/`, sólo de pruebas; la 3.2 lo reusa). Se
  construye con `(string cadena, string sqlQueInserta)`; en `SavingChangesAsync`, **una sola vez**,
  abre otra `NpgsqlConnection` con esa cadena y ejecuta ese SQL. Acá:
  `INSERT INTO especialidades (nombre, nombre_normalizado, activa) VALUES (<nombre>, <clave>, true)`
  con los valores escritos en el propio SQL (el constructor sólo recibe la cadena y el SQL; los
  valores son la marca de la prueba, no datos de un usuario). Se agrega en el `ServiceProvider` de la prueba con
  `servicios.ConfigureDbContext<HospitalDbContext>(o => o.AddInterceptors(interceptor))`
  (EF Core 9 o posterior; el proyecto usa EF Core 10). Así la verificación previa no ve nada y el
  índice único sí: se prueba la traducción del `23505`, no la verificación previa.
- **T14:** consulta directa con `NpgsqlConnection(fixture.CadenaDuenio)`.
- **T15:** `ContextoDePrueba.Crear("Host=no-se-usa;Database=no-se-usa")` (la ayuda de la 1.3): leer
  `Model` no abre conexión. No lleva `[Collection("Postgres")]`.
- **Web.Tests (T16–T22):** `[Collection("Postgres")]` y `PersonalWebApplicationFactory` de la 2.1
  (hereda de `HospitalWebApplicationFactory`). Ingresan con el usuario fijo `admin1` /
  `Admin-2026xx` (`Lucía Pérez`) usando `FormularioHttp.IngresarAsync`, porque **no** cambian su
  estado. T21 (d) usa un administrador propio
  (`FormularioHttp.CrearUsuarioAsync(app, [Roles.Administrador], "Admin-Propio-21")`), como la 2.2
  T26 (f). El POST de A2 se arma con `FormularioHttp.EnviarAsync(cliente, ruta, formulario, campos, conToken)`
  de la 2.2 (sin ayuda nueva): formulario de alta `alta-especialidad` con el campo `Alta.Nombre`;
  formulario de la tabla `acciones-especialidades` con `Accion` = `desactivar:<id>` |
  `reactivar:<id>`. El cartel de éxito se lee con `FormularioHttp.Estado(html)` y el de error con
  `Alerta(html)`. Los textos se comparan después de `WebUtility.HtmlDecode`.
- **Datos previos de las pruebas web** (T16, T19–T22): se crean resolviendo
  `ICatalogoEspecialidades` del contenedor de la fábrica, con el actor de `admin1` (que existe en la
  base de la fábrica: `IActorVigente` lo acepta), no por formulario (es más rápido y no es lo que se
  prueba ahí).
- **T23 (bUnit):** `BunitContext` como `MenuLateralTests` de la 2.1, con el mismo
  `AntiforgeryStateProvider` de prueba, autorización de prueba con rol `Administrador`, y un
  `ICatalogoEspecialidades` de prueba escrito a mano que devuelve listas vacías. Es un doble de
  prueba, no código de producción.

## Tasks / Subtasks

- [x] [SEQ] Task 1: Salvaguarda de estado (AC: todos)
  - [x] Confirmar que la 2.2 está en `main` (`depends_on`) y que la suite está en verde
        (`bash scripts/ci/local.sh`).
  - [x] Confirmar que existen, con estos nombres: `HospitalDbContext` (con `ApplyConfiguration` a mano
        y `ConvencionSnakeCase.Aplicar` al final de `OnModelCreating`), `AccionesAuditoria`,
        `CanalAuditoria.Personal`, `IAuditoria.Registrar(...)`, `ConflictoException`,
        `ReglaDeNegocioException`, `NoEncontradoException`, `AccesoDenegadoException`,
        `RutasPersonal.Especialidades`, `SeccionesAdministracion.razor` con el `@page` de
        especialidades, `PersonalWebApplicationFactory`, `FormularioHttp`, `ContextoDePrueba`.
  - [x] Confirmar las piezas compartidas de la 2.2 (su AC 26) con **su firma real** y anotarlas en el
        Debug Log: `ActorDeSesion.Desde(ClaimsPrincipal)`, `ManejadorGlobalDeErrores.EsEsperable` y
        `FijarEstadoEnPagina(HttpContext, Exception)`, `IActorVigente.ExigirAdministradorActivoAsync(actor, mensajeSiNoLoEs, ct)`,
        la carpeta `Components/Personal/Administracion/`, `FormularioHttp.EnviarAsync` / `Estado`,
        `RutasYaConstruidas` en la 2.1 T19 y `UsuariosDePrueba` en Aplicacion.Tests/Ayudas. Esta
        story **no** crea ninguna de ellas: si falta una, DETENERSE (la 2.2 no está completa).
  - [x] Si algo no coincide → DETENERSE y corregir la story primero.
- [x] [SEQ] Task 2: Dominio (AC: 1–4)
  - [x] `Dominio/Catalogo/Especialidad.cs` como dice "Implementación concreta".
  - [x] Constantes nuevas en `AccionesAuditoria` (AC 5, 9, 11).
  - [x] T1–T5 en verde.
- [x] [SEQ] Task 3: Persistencia y migración (AC: 2, 7, 9, 19)
  - [x] `EspecialidadConfiguracion` y su `ApplyConfiguration` en `HospitalDbContext`, **antes** de
        `ConvencionSnakeCase.Aplicar`; `DbSet<Especialidad> Especialidades`.
  - [x] Migración `AgregarEspecialidades` con `dotnet ef migrations add` (ver Como verificar) y el
        bloque de permisos al final de `Up` (sin `DELETE` para `turnos_app`).
  - [x] Revisar a mano el snapshot y la migración generada: tabla `especialidades`, índice único
        `ix_especialidades_nombre_normalizado`, columna `activa` **sin** valor por defecto en la base.
  - [x] Correr la Salvaguarda de BD.
- [x] [SEQ] Task 4: Caso de uso (AC: 5–7, 9–13, 16)
  - [x] `ICatalogoEspecialidades` + `EspecialidadResumen` en `Aplicacion/Catalogo/`.
  - [x] `CatalogoEspecialidadesEnBase` en `Infraestructura/Catalogo/`, registrado `Scoped` en
        `AgregarInfraestructura`.
  - [x] `InterceptorQueSeAdelanta` (Ayudas) y T6–T15 en verde.
- [x] [SEQ] Task 5: Pantalla A2 (AC: 8, 13–15, 19)
  - [x] Quitar `@page "/personal/admin/especialidades"` de `SeccionesAdministracion.razor` (las otras
        cuatro rutas quedan).
  - [x] `Components/Personal/Administracion/Especialidades.razor` (SSR estático,
        `@layout LayoutPersonal`, `[Authorize(Roles = Administrador)]`), en la carpeta de A4 (2.2).
  - [x] Sumar `/personal/admin/especialidades` a `RutasYaConstruidas` en la 2.1 T19 (AC 19).
  - [x] T16–T23 en verde.
- [x] [SEQ] Task 6: Cierre (AC: 19)
  - [x] Suite completa (`dotnet test`) y `bash scripts/ci/local.sh` en verde.
  - [x] Verificación manual en el navegador (Como verificar, paso 4) y su resultado en el Dev Agent
        Record.

### Task Dependencies

T1 → T2 → T3 → T4 → T5 → T6 (secuencial: cada capa usa la anterior).

## Dev Notes

### Que existe hoy

#### En hospital-turnos (verificado 2026-10-16 contra `main` en `c3413cc`, con 1.1 a 2.1 en done; revalidado en la sesión 10 contra `1a300d6`, con la 2.2 en done)

- **No existe** nada de lo que lista `files_to_create`, ni ninguna carpeta `Catalogo/` en ningún
  proyecto. No hay ninguna entidad de negocio todavía: las del modelo son `RegistroAuditoria` (1.3),
  `MensajeSaliente` (1.4) y las de Identity (2.1).
- `HospitalDbContext` (`Infraestructura/Persistencia/HospitalDbContext.cs`): hereda de
  `IdentityDbContext<UsuarioPersonal, IdentityRole<Guid>, Guid>`; declara los `DbSet` como
  `public DbSet<X> Xs => Set<X>();` (junto a `RegistrosAuditoria` y `MensajesSalientes`); en
  `OnModelCreating` llama primero a `base.OnModelCreating`, después **cada configuración a mano**
  (`ApplyConfiguration`, "no hay ApplyConfigurationsFromAssembly") y **al final**
  `ConvencionSnakeCase.Aplicar`.
- `ConvencionSnakeCase` renombra tabla (desde el nombre del `DbSet`), columnas, claves, foráneas e
  índices: `Especialidades` → `especialidades`, `NombreNormalizado` → `nombre_normalizado`,
  `IX_Especialidades_NombreNormalizado` → `ix_especialidades_nombre_normalizado`. **No** hace falta
  `ToTable` ni `HasDatabaseName`.
- Ids: `RegistroAuditoria.Id` y `MensajeSaliente.Id` son `long` con identity de Npgsql (comentario
  `// long → identity (convención de Npgsql)` en sus configuraciones). `UsuarioPersonal` usa `Guid`
  porque lo fija Identity.
- `UsuarioPersonal.Activo`: el comentario de `HospitalDbContext` explica por qué **no** lleva
  `HasDefaultValue(true)` en la base (EF tomaría `false` como "sin valor"). Vale igual para
  `Especialidad.Activa`.
- Permisos: `docker/postgres/initdb/01-roles.sh` da a `turnos_app` `SELECT, INSERT, UPDATE, DELETE`
  en toda tabla nueva del dueño, y `USAGE, SELECT` en las secuencias. La migración
  `20261016020303_AgregarBandejaDeSalida.cs` (líneas 47–56) muestra cómo se le quita lo que no usa:
  `REVOKE ALL` + `GRANT` de lo justo + `GRANT USAGE` de la secuencia con `pg_get_serial_sequence`.
- Auditoría: `IAuditoria.Registrar(ActorAuditoria actor, string accion, string entidadTipo, string entidadId, object? detalle = null)`
  **agrega sin guardar**; el detalle se serializa con `JsonSerializerOptions.Web`. `AccionesAuditoria`
  tiene hoy 17 constantes: 9 con forma `entidad.accion` (de la arquitectura), 3 de la 2.1 en
  kebab-case (`ingreso-personal`, …) y 5 de la 2.2 con forma `entidad.accion`
  (`usuario-personal.creado`, …). Esta story sigue la forma `entidad.accion` del contrato
  §Auditoría, como la 2.2: `especialidad.creada`.
- Errores: `ManejadorGlobalDeErrores` (1.2) traduce `ConflictoException` → 409,
  `ReglaDeNegocioException` → 400, `NoEncontradoException` → 404, `AccesoDenegadoException` → 403,
  **pero** si el pedido trae `Accept: text/html` devuelve `false` y la respuesta es la página
  `/Error` con el texto **genérico** `Algo falló. Probá de nuevo en unos minutos.` (`Error.razor` no
  muestra el mensaje de la regla). Por eso A2 muestra la regla en su propia página (decisión 1).
- Web: `RutasPersonal.Especialidades = "/personal/admin/especialidades"`, título `Especialidades`,
  en el menú del administrador (3.º lugar). Hoy la atiende `SeccionesAdministracion.razor` (cinco
  `@page`, `[Authorize(Roles = Administrador)]`, `<SeccionEnConstruccion>`). `Ingreso.razor` es el
  modelo de formulario SSR: `EditForm` con `FormName`, `[SupplyParameterFromForm]`,
  `DataAnnotationsValidator`, cartel `role="alert"`, y **sin** `try/catch` genérico.
  `Administracion/UsuariosPersonal.razor` (2.2, líneas 200-212) muestra cómo una página SSR del
  administrador fija el código del cartel: `ManejadorGlobalDeErrores.FijarEstadoEnPagina(HttpContext, e)`.
  No se usa `Response.StatusCode = …` a mano: en .NET 10, Blazor SSR no escribe el cuerpo de una página
  que terminó de dibujarse con `404`, y el cartel no llegaría.
- El actor de la sesión: el principal trae `ClaimTypes.NameIdentifier` (id), `ClaimTypes.Role`
  (un solo rol, AC 24 de la 2.1) y `ClaimsPersonal.NombreCompleto = "nombre_completo"`
  (`Aplicacion/Personal/IIngresoPersonal.cs`). Hoy **nadie** arma un `ActorAuditoria` desde la sesión
  (la 2.1 lo arma desde el usuario de Identity). Lo agrega la 2.2, que se implementa antes, como
  `ActorDeSesion.Desde(ClaimsPrincipal)`, junto con `IActorVigente` (su AC 26); esta story los usa.
- Pruebas: la 2.1 T19 (`SesionYPermisosHttpTests.Secciones_SegunRol_PermiteLasSuyasYDevuelve403LegibleEnLasAjenas`) recorre las 11 rutas × 3 roles
  y, cuando el rol tiene permiso, exige `<h1>{título}</h1>` **y** `TextoEnConstruccion`. La 2.2 le
  agregó la lista `RutasYaConstruidas` (hoy sólo `/personal/admin/usuarios`); esta story suma su
  ruta ahí (AC 19).

#### Lo que dicen los documentos

- PRD FR-12: "El administrador puede crear especialidades y desactivarlas si no tienen turnos
  futuros." No pide editar el nombre ni reactivar (reactivar se suma por la decisión 6).
- UX A2: "Lista simple con alta y desactivar. 'Desactivar' se deshabilita si hay turnos futuros, con
  el motivo al pasar el mouse." UX §8: error de regla = "cartel arriba del formulario"; estado vacío
  con la acción siguiente.
- Arq. §6: `Especialidad { Nombre, Activa }`, `Especialidad "1" <-- "*" Medico`. Arq. §7.7: el
  permiso se controla en el caso de uso.
- Plan: la 3.1 es del sprint 2, después de la 2.2 y antes de la 3.2 y la 4.1.
- Retrospectiva del sprint 1, acción A-1: la story se escribe contra el código real y se vuelve a
  validar justo antes de programar (Task 1).

### Patron de referencia verificado

El proyecto guía de la materia (contrato §Patrón de referencia) no tiene catálogo con altas y bajas
lógicas, ni Blazor. Los patrones de esta story salen **del propio repo**, ya probados en el sprint 1:

| Patrón | Ancla `archivo:línea` | Qué se toma |
|---|---|---|
| Configuración de entidad sin `ToTable` | `src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/MensajeSalienteConfiguracion.cs:9-21` | `IEntityTypeConfiguration<T>`, `HasKey` sobre `long`, `HasMaxLength`, índice con `HasIndex`; el nombre lo pone la convención. |
| Registro de la configuración | `HospitalDbContext.OnModelCreating` (`src/HospitalTurnos.Infraestructura/Persistencia/HospitalDbContext.cs`, hoy líneas 29-33) | `ApplyConfiguration` a mano, antes de `ConvencionSnakeCase.Aplicar`. |
| Quitar permisos que la app no usa | `src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/20261016020303_AgregarBandejaDeSalida.cs:47-56` | `REVOKE ALL` + `GRANT SELECT, INSERT, UPDATE` + `GRANT USAGE` de la secuencia. |
| Puerto en Aplicacion, adaptador en Infraestructura | `src/HospitalTurnos.Aplicacion/Personal/IIngresoPersonal.cs:9` y `src/HospitalTurnos.Infraestructura/Identidad/RegistroIdentidad.cs` (`AddScoped<IIngresoPersonal, IngresoPersonalIdentity>`) | El caso de uso es una interfaz de Aplicacion; la implementación con `HospitalDbContext` vive en Infraestructura (Aplicacion no conoce EF). |
| Registro de servicios scoped | `AgregarInfraestructura` (`src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs`, hoy línea 25) | `AddScoped` junto a `IAuditoria`: mismo alcance que el contexto, así el cambio y la auditoría usan el mismo contexto. |
| Formulario SSR con validación doble y cartel | `src/HospitalTurnos.Web/Components/Personal/Ingreso.razor:19-35,45,82` | `EditForm` + `FormName` + `[SupplyParameterFromForm]` + `DataAnnotationsValidator` + `role="alert"`. |
| Página SSR que fija su código de estado | `src/HospitalTurnos.Web/Components/Personal/Administracion/UsuariosPersonal.razor:200-212` y `src/HospitalTurnos.Web/Errores/ManejadorGlobalDeErrores.cs:40` | `catch (… when EsEsperable(e))` + `FijarEstadoEnPagina(HttpContext, e)`: 400/403/409 en el acto y el 404 al empezar a enviar la respuesta, para que el cartel llegue. |
| Pruebas de caso de uso contra la base real | `tests/HospitalTurnos.Aplicacion.Tests/Personal/IngresoPersonalIdentityTests.cs:27-40` | `ServiceProvider` con `AgregarInfraestructura` y `CadenaApp`, un alcance por operación, reemplazos de prueba registrados al final. |
| Pruebas HTTP de un formulario SSR | `tests/HospitalTurnos.Web.Tests/Personal/FormularioHttp.cs` | GET + token + `_handler` + POST, `Destino`, `Alerta`. |
| Componente con autorización en bUnit | `tests/HospitalTurnos.Web.Tests/Personal/MenuLateralTests.cs:11-17,40-44` | `BunitContext`, `AntiforgeryStateProvider` de prueba, `AddAuthorization().SetRoles(...)`. |

### Que hay que hacer (la delta)

1. Entidad `Especialidad` con sus reglas (nombre limpio, clave de comparación, desactivar, reactivar).
2. Tabla `especialidades` con índice único por la clave, sin `DELETE` para la app.
3. Caso de uso `ICatalogoEspecialidades` que confirma en la base que el actor es administrador
   activo (`IActorVigente`, 2.2), guarda y audita en una sola
   transacción, y traduce la carrera del índice único a un `409`.
4. Pantalla A2 real en lugar de la sección "en construcción".
5. El contrato escrito (y una prueba-trampa) para que la 5.3 complete la regla de turnos futuros.

### Decisiones de esta story (con su porqué)

- **La comparación de nombres ignora mayúsculas, tildes y espacios repetidos** (AC 2). En P6 el
  paciente elige de una lista: `Cardiología` y `Cardiologia` como dos opciones serían un error del
  catálogo, no dos especialidades. La clave va en su columna con índice único (como el
  `NormalizedUserName` de Identity) para que la base, y no sólo el código, impida el duplicado.
- **No se borra nunca: se desactiva.** Los médicos (3.2) y, después, los turnos apuntan a la
  especialidad; borrarla rompería esa historia. Por eso `turnos_app` no tiene `DELETE` sobre la tabla
  (AC 19, T14).
- **El permiso se controla también en el caso de uso** (Arq. §7.7), **en la base**, con
  `IActorVigente` de la 2.2 (decisión 16). Con sólo el `Rol` del actor (que sale de la cookie), un
  administrador desactivado seguiría creando y desactivando especialidades hasta 5 minutos (I-3 de
  la revisión). El actor que devuelve el servicio es el que va a la auditoría.
- **La clave quita también la tilde de la ñ** (M-9 de la revisión): `FormD` separa la `ñ` en `n` +
  tilde y la tilde se descarta, así que `Señal` y `Senal` dan la misma clave. Para nombres de
  especialidades es aceptable: no hay dos especialidades que sólo se distingan por una ñ.
- **Los botones de la tabla mandan un solo campo `Accion = "tipo:id"`**, como A4 de la 2.2 (M-8 de
  la revisión): un único formulario con un nombre fijo, una forma ya razonada en la 2.2, y un valor
  que no se reconoce da `400` en lugar de caer en un enlace de parámetros que habría que verificar.
- **No se puede editar el nombre.** FR-12 no lo pide y la UX A2 no lo muestra. Un error de tipeo se
  corrige desactivando y creando la correcta.
- **Largo máximo 80** caracteres: el nombre más largo esperable ("Otorrinolaringología pediátrica",
  31) entra holgado, y el límite evita que un POST armado a mano guarde un texto enorme.
- **La auditoría usa el canal `Personal`** (contrato §Auditoría: "administración de … catálogo"). La
  métrica CE-01 no se ensucia.

### Decisiones tomadas por el docente (2026-10-16)

Numeradas como en `revision-sprint-2.md` ("Decisiones abiertas consolidadas").

- **Decisión 5 — Cómo se cumple hoy "desactivarlas si no tienen turnos futuros" (FR-12)** (era D1).
  **Elegido:** desactivar sin consultar turnos ahora, con el contrato escrito para la 5.3 (AC 17) y la
  prueba-trampa **reforzada** (T15 salta por el nombre de la clase **o** por la tabla `turnos`, y su
  mensaje nombra también la herencia de la 3.2). Hoy no puede haber turnos, así que la condición se
  cumple siempre: no es un atajo ni un stub. Queda descartada la interfaz "contador de turnos" que
  devuelve 0 (sería un stub en producción).
- **Decisión 6 — Reactivar** (era D2). **Elegido:** sumar "Reactivar" (AC 4 y 11). Sin eso, desactivar
  es irreversible y el nombre queda bloqueado para siempre por el índice único.
- **Decisión 1 — Dónde se ve el error de regla en A2** (era D3). **Elegido:** cartel en la propia
  página con el texto de la regla y el código del manejador global, **con el mecanismo de la 2.2**:
  `catch (Exception e) when (ManejadorGlobalDeErrores.EsEsperable(e))` +
  `ManejadorGlobalDeErrores.FijarEstadoEnPagina(HttpContext, e)`. Así A2
  también muestra el `403` de `AccesoDenegadoException` (AC 13) y nunca escribe un código a mano.
- **Decisión 16 — Permiso en el caso de uso.** **Elegido:** se confirma en la base en cada cambio con
  `IActorVigente` de la 2.2 (AC 13, T12, T21).
- **Decisión 7 — Cambiar la especialidad de un médico con turnos futuros** (de la 3.2). La bloquea la
  5.3; esta story sólo la nombra en el mensaje de la trampa T15 para que no se pierda (I-8).

### Implementacion concreta

**`Dominio/Catalogo/Especialidad.cs`**:

```csharp
namespace HospitalTurnos.Dominio.Catalogo;

/// <summary>Una especialidad del hospital (FR-12). No se borra nunca: se desactiva.</summary>
public sealed class Especialidad
{
    public const int LargoMaximo = 80;

    public long Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    // La clave con la que se comparan nombres (AC 2). Va en su columna con índice único:
    // la base, no sólo el código, impide "Cardiología" y "cardiologia" a la vez.
    public string NombreNormalizado { get; private set; } = null!;
    public bool Activa { get; private set; }

    private Especialidad() { } // para EF

    public Especialidad(string nombre)
    {
        Nombre = Limpiar(nombre);            // lanza ReglaDeNegocioException (AC 1)
        NombreNormalizado = Normalizar(Nombre);
        Activa = true;                       // en la clase, no como default de la base (ver Dev Notes)
    }

    // Trim + espacios internos colapsados; vacío o > LargoMaximo → ReglaDeNegocioException.
    public static string Limpiar(string? nombre) { ... }

    // Limpiar + quitar tildes (NormalizationForm.FormD y descartar UnicodeCategory.NonSpacingMark)
    // + ToUpperInvariant. Sin cultura: el resultado no depende del servidor.
    public static string Normalizar(string nombre) { ... }

    public void Desactivar()
    {
        if (!Activa) throw new ConflictoException($"La especialidad {Nombre} ya está desactivada.");
        Activa = false;
    }

    public void Reactivar()
    {
        if (Activa) throw new ConflictoException($"La especialidad {Nombre} ya está activa.");
        Activa = true;
    }
}
```

**`AccionesAuditoria`** — tres constantes nuevas, en un bloque comentado como el de la 2.1:

```csharp
// Story 3.1: el catálogo de especialidades (canal Personal).
public const string EspecialidadCreada = "especialidad.creada";
public const string EspecialidadDesactivada = "especialidad.desactivada";
public const string EspecialidadReactivada = "especialidad.reactivada";
```

**`EspecialidadConfiguracion`**: `HasKey(e => e.Id)`; `Nombre` y `NombreNormalizado` con
`HasMaxLength(Especialidad.LargoMaximo).IsRequired()`; `HasIndex(e => e.NombreNormalizado).IsUnique()`;
`Activa` sin `HasDefaultValue`. En `HospitalDbContext`:
`public DbSet<Especialidad> Especialidades => Set<Especialidad>();` y
`modelBuilder.ApplyConfiguration(new EspecialidadConfiguracion());` junto a las otras dos.

**Permisos, al final del `Up` de la migración** (mismo patrón que `AgregarBandejaDeSalida`):

```sql
-- La app crea, lista, desactiva y reactiva; nunca borra (los médicos y los turnos apuntan acá).
REVOKE ALL ON TABLE especialidades FROM turnos_app;
GRANT SELECT, INSERT, UPDATE ON TABLE especialidades TO turnos_app;
DO $$ BEGIN
  EXECUTE format('GRANT USAGE ON SEQUENCE %s TO turnos_app', pg_get_serial_sequence('especialidades', 'id'));
END $$;
```

**`Aplicacion/Catalogo/ICatalogoEspecialidades.cs`**:

```csharp
public interface ICatalogoEspecialidades
{
    Task<IReadOnlyList<EspecialidadResumen>> ListarAsync(CancellationToken ct);         // A2: todas
    Task<IReadOnlyList<EspecialidadResumen>> ListarActivasAsync(CancellationToken ct);  // 3.2, 5.2, 6.4
    Task<EspecialidadResumen?> BuscarAsync(long id, CancellationToken ct);              // 3.2 valida
    Task<long> CrearAsync(ActorAuditoria actor, string nombre, CancellationToken ct);
    Task DesactivarAsync(ActorAuditoria actor, long id, CancellationToken ct);
    Task ReactivarAsync(ActorAuditoria actor, long id, CancellationToken ct);
}

public sealed record EspecialidadResumen(long Id, string Nombre, bool Activa);
```

**`CatalogoEspecialidadesEnBase(HospitalDbContext contexto, IAuditoria auditoria, IActorVigente actorVigente)`**
(Infraestructura/Catalogo) — el orden importa. `SinPermiso` es la constante con el texto del AC 13:

```csharp
public async Task<long> CrearAsync(ActorAuditoria actor, string nombre, CancellationToken ct)
{
    // 1. permiso EN LA BASE (AC 13, decisión 16), antes de tocar nada; el actor devuelto es el que se audita
    actor = await actorVigente.ExigirAdministradorActivoAsync(actor, SinPermiso, ct);
    var nueva = new Especialidad(nombre);                // 2. reglas del nombre (AC 1)
    var existente = await contexto.Especialidades.AsNoTracking()
        .FirstOrDefaultAsync(e => e.NombreNormalizado == nueva.NombreNormalizado, ct);
    if (existente is not null) throw Repetida(existente); // 3. mensaje con el nombre guardado (AC 6)
    contexto.Especialidades.Add(nueva);
    // 4. El id lo da la base al guardar: se guarda primero y se audita con el id real, todo dentro de
    //    UNA transacción explícita, para que el cambio y su registro se guarden juntos o ninguno (AC 5).
    await using var tx = await contexto.Database.BeginTransactionAsync(ct);
    try { await contexto.SaveChangesAsync(ct); }
    catch (DbUpdateException e) when (e.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ix_especialidades_nombre_normalizado" })
    {
        // 5. Otra alta ganó la carrera entre la verificación y el guardado (AC 7).
        await tx.RollbackAsync(ct);
        var ganadora = await contexto.Especialidades.AsNoTracking()
            .FirstAsync(x => x.NombreNormalizado == nueva.NombreNormalizado, ct);
        throw Repetida(ganadora);
    }
    auditoria.Registrar(actor, AccionesAuditoria.EspecialidadCreada, "Especialidad",
        nueva.Id.ToString(CultureInfo.InvariantCulture), new { nombre = nueva.Nombre });
    await contexto.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);
    return nueva.Id;
}
```

> Por qué una transacción explícita y no un solo `SaveChangesAsync`: el `EntidadId` de la auditoría es
> el id de la especialidad, que recién existe después del primer guardado (identity). Con la
> transacción, si falla el segundo guardado no queda la especialidad sin su registro.
> Ojo: después de un `DbUpdateException`, la entidad `nueva` sigue en el contexto como `Added`; como
> se lanza enseguida y el contexto es del alcance del pedido, no se vuelve a guardar.

`DesactivarAsync` / `ReactivarAsync`: `actor = await actorVigente.ExigirAdministradorActivoAsync(actor, SinPermiso, ct)` → buscar **con seguimiento** por
id (`null` → `NoEncontradoException` del AC 12) → `especialidad.Desactivar()` / `Reactivar()` (las
reglas del AC 3/4 viven en el dominio) → `Registrar(...)` con la acción que corresponda →
**un solo** `SaveChangesAsync` (acá el id ya existe). Sin consulta de turnos (decisión 5, AC 10).

`ListarAsync` y `ListarActivasAsync`: `AsNoTracking`, `OrderBy(e => e.NombreNormalizado)`,
proyección a `EspecialidadResumen`. El actor de la sesión lo arma la página con
`ActorDeSesion.Desde(HttpContext.User)` de la 2.2; esta story no crea ninguna otra forma.

**`Components/Personal/Administracion/Especialidades.razor`** — SSR estático (sin `@rendermode`,
como S0), en la misma carpeta que A4 de la 2.2. Lleva sus propios `@using` de
`HospitalTurnos.Aplicacion.Catalogo` y `HospitalTurnos.Web.Errores`:

- `@page "/personal/admin/especialidades"`, `@layout LayoutPersonal`,
  `@attribute [Authorize(Roles = HospitalTurnos.Dominio.Personal.Roles.Administrador)]`,
  `<PageTitle>Especialidades · Las Acacias</PageTitle>`, `<h1>Especialidades</h1>`.
- **Dos formularios con nombres distintos** (Blazor no admite dos formularios con el mismo
  `FormName` en la misma página):
  - `EditForm FormName="alta-especialidad"` con
    `[SupplyParameterFromForm(FormName = "alta-especialidad")] FormularioAlta? Alta`
    (`[Required(ErrorMessage = …)]` y `[StringLength(80, ErrorMessage = …)]` con los textos del AC 8),
    `DataAnnotationsValidator`, `<label for="nombre">Nombre</label>`, botón `Agregar`.
  - **Un solo** `<form method="post" @formname="acciones-especialidades" @onsubmit="EjecutarAccionAsync">`
    con `<AntiforgeryToken />` que envuelve la tabla, igual que A4 de la 2.2 (M-8). Cada botón es
    `<button type="submit" name="Accion" value="desactivar:@e.Id">Desactivar</button>` o
    `value="reactivar:@e.Id"`; se lee con
    `[SupplyParameterFromForm(FormName = "acciones-especialidades")] string? Accion`. El navegador
    manda sólo el botón presionado. `Accion` se separa en `tipo:id`; un tipo que no es `desactivar`
    ni `reactivar`, o un id que no es `long`, lanza
    `ReglaDeNegocioException("No reconocemos esa acción. Recargá la página.")` dentro del mismo `try`.
- `ListarAsync` se llama en `OnInitializedAsync` y **otra vez** después de cada acción (la tabla
  muestra el estado nuevo).
- Acciones: `var actor = ActorDeSesion.Desde(HttpContext.User);` (2.2) y llamada al caso de uso
  dentro de este `try`, el mismo de A4 (decisión 1):

```csharp
try { /* caso de uso */ }
catch (Exception e) when (ManejadorGlobalDeErrores.EsEsperable(e))   // sólo las del dominio
{
    ManejadorGlobalDeErrores.FijarEstadoEnPagina(HttpContext, e);   // 409/400/404/403
    alerta = e.Message;
}
await CargarAsync();
```

  Éxito: `estado = "Especialidad X agregada."` (o desactivada / reactivada) y, en el alta,
  `Alta = new()` para vaciar el campo.
- Cartel de error `<div class="alert alert-danger" role="alert">` y de éxito
  `<div class="alert alert-success" role="status">`, arriba del formulario de alta (UX §8; los dos los
  anuncia el lector de pantalla, UX §9).

**`SeccionesAdministracion.razor`**: sólo se borra la línea `@page "/personal/admin/especialidades"`.

**2.1 T19 (AC 19)**: en `SesionYPermisosHttpTests`, se suma `/personal/admin/especialidades` a la lista
`RutasYaConstruidas` que creó la 2.2. Las otras dos aserciones (`<h1>` y `403`) no cambian. Así la
prueba de la 2.1 sigue cuidando los permisos de las 11 rutas.

### Salvaguarda de BD (obligatoria antes de implementar las pruebas de Web)

**No** usar `dotnet ef database update` (usaría `turnos_app`, sin `CREATE`). Las migraciones las
aplica el arranque con `ConnectionStrings:Migraciones`:

```bash
docker compose up -d --build     # la app aplica AgregarEspecialidades al arrancar
```

Contra el PostgreSQL de Compose (mismo `01-roles.sh` que la `PostgresFixture`):

1. `docker compose exec db psql -U postgres -d hospital_turnos -c "\d especialidades"` → columnas
   `id` (bigint, identity), `nombre` y `nombre_normalizado` (`character varying(80)`, not null),
   `activa` (boolean, not null, **sin** default) e índice único `ix_especialidades_nombre_normalizado`.
2. `SELECT has_table_privilege('turnos_app','especialidades','DELETE');` → `f`; con
   `'SELECT,INSERT,UPDATE'` → `t`.
3. `SELECT has_table_privilege('turnos_app','registros_auditoria','UPDATE');` → sigue en `f`.
4. Si CUALQUIER resultado no coincide → DETENERSE → corregir la story primero.

### Archivos a crear/modificar

| Acción | Archivo |
|---|---|
| Crear | Los de `files_to_create` del frontmatter |
| Modificar | `src/HospitalTurnos.Dominio/Auditoria/AccionesAuditoria.cs` (3 constantes) |
| Modificar | `src/HospitalTurnos.Infraestructura/Persistencia/HospitalDbContext.cs` (`DbSet` + `ApplyConfiguration`) |
| Modificar | `src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/HospitalDbContextModelSnapshot.cs` (lo regenera `dotnet ef`) |
| Modificar | `src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs` (`AddScoped<ICatalogoEspecialidades, CatalogoEspecialidadesEnBase>()`) |
| Modificar | `src/HospitalTurnos.Web/Components/Personal/Secciones/SeccionesAdministracion.razor` (quitar un `@page`) |
| Modificar | `tests/HospitalTurnos.Web.Tests/Personal/SesionYPermisosHttpTests.cs` (una ruta más en `RutasYaConstruidas`, AC 19) |

### Lo que NO hay que hacer

- **NO borrar especialidades** (ni en el código ni con permisos de la base): se desactivan.
- **NO crear una interfaz "turnos futuros" con una implementación que devuelva 0** ni nada parecido:
  es un stub (decisión 5). Tampoco crear la tabla de turnos: es de la 5.3.
- **NO implementar la edición del nombre**: FR-12 no la pide.
- **NO tocar médicos, agendas ni el selector de especialidad de A1**: son de la 3.2 y la 4.x.
- **NO atrapar `Exception` sin filtro** en la página: sólo
  `when (ManejadorGlobalDeErrores.EsEsperable(e))` (decisión 1). Lo inesperado va al
  `ManejadorGlobalDeErrores` (NFR-09).
- **NO volver a crear las piezas de la 2.2** (`ActorDeSesion`, otro mapeo excepción → código, otro
  control de permiso, otra ayuda de POST, otra carpeta de páginas): se reusan con su firma.
- **NO hacer interactiva la página** (`@rendermode`): el resto del personal es SSR estático (2.1).
- **NO usar `ApplyConfigurationsFromAssembly`** ni `ToTable`: el repo registra cada configuración a
  mano y la convención pone los nombres.
- **NO poner `HasDefaultValue(true)` en `Activa`** (ver Que existe hoy).
- **NO llamar a `DateTime.Now`/`UtcNow`** (contrato §Hora).
- **NO limpiar tablas en las pruebas** ni contar filas sin la marca propia (base compartida).
- **NO usar `dotnet ef database update`.**
- **NO crear el proyecto E2E.**

### Como verificar que funciona

```bash
cd ~/SourceCode/hospital-turnos

# 1. Migración (proyecto de migraciones = Infraestructura, arranque = Web)
dotnet ef migrations add AgregarEspecialidades \
  --project src/HospitalTurnos.Infraestructura --startup-project src/HospitalTurnos.Web \
  --output-dir Persistencia/Migraciones
# Agregar a mano el bloque de permisos al final de Up (Implementación concreta).

# 2. Pruebas de esta story (Docker para Testcontainers)
dotnet test --filter "Story=3.1"
# Esperado: 23 pruebas declaradas (más los casos de cada [Theory]), 0 fallas
dotnet test --filter "Story=2.1&AC=13"     # la 2.1 T19 ajustada, 33 casos en verde

# 3. Suite completa y CI local (AC 19)
dotnet test
bash scripts/ci/local.sh; echo "rc=$?"     # rc=0

# 4. Verificación manual en el navegador
docker compose up -d --build
open http://localhost:8080/personal/admin/especialidades
# Esperado, en este orden (ingresando como el administrador inicial):
#  a. sin especialidades: "Todavía no hay especialidades. Agregá la primera con el formulario de arriba."
#  b. agregar "  Cardiología " → "Especialidad Cardiología agregada." y la fila "Cardiología · Activa · Desactivar"
#  c. agregar "CARDIOLOGIA" → cartel rojo "Ya existe la especialidad Cardiología." y el campo con lo tipeado
#  d. enviar el alta vacía → "Ingresá el nombre de la especialidad." debajo del campo
#  e. agregar "Pediatría" y "Clínica médica" → la tabla las ordena: Cardiología, Clínica médica, Pediatría
#  f. "Desactivar" en Pediatría → "Especialidad Pediatría desactivada." y la fila con "Desactivada · Reactivar"
#  g. agregar "pediatria" → "Ya existe la especialidad Pediatría, desactivada. Reactivala desde la lista."
#  h. "Reactivar" en Pediatría → vuelve a "Activa"
#  i. ingresar como recepcionista y abrir la misma ruta → "No tenés permiso para entrar a esta sección." (403)

# 5. La auditoría quedó escrita (canal Personal)
docker compose exec db psql -U postgres -d hospital_turnos \
  -c "SELECT cuando, quien, rol, canal, accion, entidad_tipo, entidad_id, detalle FROM registros_auditoria WHERE entidad_tipo = 'Especialidad' ORDER BY id DESC LIMIT 10;"
```

## Dev Agent Record

### Agent Model Used

Claude Opus 5.5 (sesión 10, 2026-10-16), con el circuito completo del equipo.

### Debug Log References

- Task 1 (salvaguarda): suite de `main` en `1a300d6` en verde antes de tocar nada (Dominio 20/20,
  Aplicacion 94/94). Piezas de la 2.2 con su firma real: `ActorDeSesion.Desde(ClaimsPrincipal)`,
  `ManejadorGlobalDeErrores.EsEsperable(Exception)` y `FijarEstadoEnPagina(HttpContext, Exception)`,
  `IActorVigente.ExigirAdministradorActivoAsync(ActorAuditoria, string, CancellationToken)`,
  `FormularioHttp.EnviarAsync`/`Estado`/`Alerta`/`ValorDe`, `RutasYaConstruidas` (sólo usuarios) y
  `UsuariosDePrueba.CrearAsync`/`ActorDe`.
- Salvaguarda de BD (Compose): `especialidades` con `id` bigint identity, `nombre` y
  `nombre_normalizado` varchar(80) not null, `activa` boolean not null sin default, índice único
  `ix_especialidades_nombre_normalizado`; `turnos_app` DELETE = f, SELECT/INSERT/UPDATE = t;
  `registros_auditoria` UPDATE = f.
- Migración generada: `20261017000841_AgregarEspecialidades` (marca UTC, como las anteriores), con el
  bloque REVOKE/GRANT al final de `Up`.

### Completion Notes List

- Análisis de contexto completo (skill `tycon-create-story`): documentos, código de `main` y story 2.1
  como modelo. Guía para el desarrollo creada.
- Implementada con pruebas primero, capa por capa (dominio → base → caso de uso → pantalla). 23
  verificaciones (T1–T23, 51 casos) más la 2.1 T19 ajustada; suite completa 300 pruebas en verde
  (antes 249); `scripts/ci/local.sh` rc=0, cobertura de líneas 98,7 % y de ramas 93,7 %.
- Revisores: 7 de 7 en la primera vuelta, sin hallazgos. Pase adversarial apagado (decisión del docente).
- Verificación manual en el navegador (Como verificar, paso 4): en Chrome contra el Compose, con
  Playwright, pasos a–i en verde (la primera corrida, con la base vacía, verificó el estado vacío; un
  selector del script estaba mal y se repitió con nombres marcados). Además: 404 con su cartel dentro
  de A2, 403 del administrador desactivado con la sesión abierta, diez altas simultáneas → un 200 y
  nueve 409, permisos de la base. Matriz por AC 19/19 en vivo. Capturas en `Sesiones/img/10-a2-*.png`.
- El formulario de alta recorta los espacios de los extremos antes de validar (como A4); los internos
  repetidos los colapsa el dominio.
- Revisión de legibilidad (tester no técnico): la guía `docs/qa/3.1-pasos.md` explica ahora los dos
  casos del AC 13. Marca de pantalla **aceptada por el docente** (sesión 10): al administrador recién
  desactivado el cartel "Sólo el administrador puede modificar las especialidades." le resulta
  contradictorio, porque el menú todavía lo muestra como administrador; el texto queda como lo fija el AC 13.

### File List

- src/HospitalTurnos.Dominio/Catalogo/Especialidad.cs (nuevo)
- src/HospitalTurnos.Dominio/Auditoria/AccionesAuditoria.cs
- src/HospitalTurnos.Aplicacion/Catalogo/ICatalogoEspecialidades.cs (nuevo)
- src/HospitalTurnos.Infraestructura/Catalogo/CatalogoEspecialidadesEnBase.cs (nuevo)
- src/HospitalTurnos.Infraestructura/Persistencia/Configuraciones/EspecialidadConfiguracion.cs (nuevo)
- src/HospitalTurnos.Infraestructura/Persistencia/HospitalDbContext.cs
- src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/20261017000841_AgregarEspecialidades.cs (nuevo)
- src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/20261017000841_AgregarEspecialidades.Designer.cs (nuevo)
- src/HospitalTurnos.Infraestructura/Persistencia/Migraciones/HospitalDbContextModelSnapshot.cs
- src/HospitalTurnos.Infraestructura/ServiciosDeInfraestructura.cs
- src/HospitalTurnos.Web/Components/Personal/Administracion/Especialidades.razor (nuevo)
- src/HospitalTurnos.Web/Components/Personal/Secciones/SeccionesAdministracion.razor
- tests/HospitalTurnos.Dominio.Tests/Catalogo/EspecialidadTests.cs (nuevo)
- tests/HospitalTurnos.Aplicacion.Tests/Ayudas/InterceptorQueSeAdelanta.cs (nuevo)
- tests/HospitalTurnos.Aplicacion.Tests/Catalogo/CatalogoEspecialidadesTests.cs (nuevo)
- tests/HospitalTurnos.Aplicacion.Tests/Catalogo/ContratoTurnosFuturosTests.cs (nuevo)
- tests/HospitalTurnos.Web.Tests/Catalogo/EspecialidadesHttpTests.cs (nuevo)
- tests/HospitalTurnos.Web.Tests/Catalogo/EspecialidadesPaginaTests.cs (nuevo)
- tests/HospitalTurnos.Web.Tests/Personal/SesionYPermisosHttpTests.cs
- docs/qa/3.1-pasos.md, docs/qa/3.1-legibilidad.md, docs/qa/3.1-legibilidad-ui.md (nuevos)

### Change Log

- 2026-10-16: Story escrita (Mary) contra el código de `main` en `c3413cc`, para el sprint 2. 19 AC,
  23 verificaciones. Tres decisiones abiertas para el docente (D1 turnos futuros, D2 reactivar,
  D3 dónde se ve el error de regla); escrita con las opciones recomendadas.
- 2026-10-16: Corregida con la revisión adversarial (`revision-sprint-2.md`) y las decisiones del
  docente. Sale `ActorDeSesion.cs` (lo crea la 2.2) y la página pasa a
  `Components/Personal/Administracion/` (I-1, I-2); la página usa `EsEsperable`/`EstadoPara` y
  `FormularioHttp.EnviarAsync` de la 2.2; el permiso se mira en la base con `IActorVigente` (I-3,
  decisión 16: T12 suma administrador desactivado e id inexistente, T21 suma el `403` y la acción no
  reconocida); el interceptor de la carrera pasa a `Ayudas/InterceptorQueSeAdelanta`, parametrizado
  por el SQL, para que la 3.2 lo reuse; la trampa T15 mira también la tabla `turnos` y nombra la
  herencia de la 3.2 (I-8); botones con `Accion = "tipo:id"` (M-8); ñ en "Decisiones" (M-9).
  Siguen 19 AC y 23 verificaciones; ninguna decisión abierta.
- 2026-10-16 (sesión 10, antes de implementar): el cartel de error usa la pieza compartida
  `ManejadorGlobalDeErrores.FijarEstadoEnPagina(HttpContext, e)` de la 2.2 en lugar de
  `Response.StatusCode = EstadoPara(e)`: Blazor (.NET 10) no escribe el cuerpo de una página SSR que
  responde `404`, y el cartel del AC 12 no llegaría. El patrón de referencia pasa a
  `UsuariosPersonal.razor`. Sin cambios en AC ni en verificaciones.
- 2026-10-16 (sesión 10, validación): textos al día con `main` en `1a300d6` (2.2 en done):
  `AccionesAuditoria` con 17 constantes y la forma `entidad.accion` de la 2.2,
  `SeccionesAdministracion.razor` con cinco `@page` (quedan cuatro), `RutasYaConstruidas` y las
  referencias por miembro en lugar de número de línea en los archivos que esta story modifica.
- 2026-10-16 (sesión 10, implementación): implementada y verificada (300 pruebas, 98,7 % de líneas,
  matriz en vivo 19/19). Marca de legibilidad del cartel 403 aceptada por el docente sin cambiar el AC 13.
