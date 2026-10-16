---
epic: 1
story: "1.5"
title: "Integración continua: compilar, probar y medir cobertura en cada push"
status: done
sprint: 1
priority: "P1"
approach: "[ADAPT]"
depends_on: ["1.1", "1.2", "1.3", "1.4"]
files_to_create:
  - ".github/workflows/ci.yml"
  - "scripts/ci/cobertura.sh"
  - "scripts/ci/sca.sh"
  - "scripts/ci/imagen.sh"
  - "scripts/ci/local.sh"
  - "scripts/ci/pruebas/probar-workflow.py"
  - "scripts/ci/pruebas/probar-umbral.sh"
  - "scripts/ci/pruebas/probar-imagen.sh"
  - "scripts/ci/pruebas/datos/cobertura-alta.cobertura.xml"
  - "scripts/ci/pruebas/datos/cobertura-baja.cobertura.xml"
  - "scripts/ci/pruebas/datos/cobertura-sin-lineas.cobertura.xml"
  - "scripts/ci/pruebas/datos/cobertura-solo-dominio.cobertura.xml"
  - "scripts/ci/pruebas/datos/Dockerfile.roto"
files_to_modify:
  - ".config/dotnet-tools.json"
  - ".gitignore"
---

# Story 1.5: Integración continua: compilar, probar y medir cobertura en cada push

Status: done

## Story

Como docente que mantiene el sistema de turnos del Hospital Las Acacias (material de clase),
quiero un flujo de GitHub Actions que en cada push compile, corra las pruebas por niveles de la
pirámide, mida la cobertura de Dominio y Aplicacion contra el umbral del 80 %, revise las
dependencias vulnerables y construya la imagen Docker de la aplicación,
para que ningún cambio que rompa la compilación, una prueba o la cobertura de las reglas de
negocio (NFR-11) pase sin que nos enteremos, y para que los alumnos vean la pirámide ejecutándose.

## Acceptance Criteria

### Estructura del flujo

1. El archivo `.github/workflows/ci.yml` se dispara con `push` (todas las ramas), `pull_request`
   y `workflow_dispatch`. Declara `permissions: contents: read` y los trabajos (jobs), en este orden
   de dependencias:

   | Job (id) | Nombre visible | `needs` | Qué corre |
   |---|---|---|---|
   | `compilar` | `0 · Compilar` | — | `dotnet restore` + `dotnet build --no-restore -c Release` |
   | `unit` | `1 · Pruebas unitarias` | `compilar` | `tests/HospitalTurnos.Dominio.Tests` con cobertura |
   | `integracion` | `2 · Pruebas de integración` | `unit` | `tests/HospitalTurnos.Aplicacion.Tests` y `tests/HospitalTurnos.Web.Tests` con cobertura (Testcontainers) |
   | `cobertura` | `3 · Cobertura (NFR-11)` | `unit`, `integracion` | `scripts/ci/cobertura.sh` sobre los reportes de los dos jobs anteriores |
   | `sca` | `· Dependencias vulnerables (SCA)` | `compilar` | `scripts/ci/sca.sh` |
   | `imagen` | `· Imagen Docker` | `compilar` | `scripts/ci/imagen.sh` (AC 13) |

   Todos los jobs usan `runs-on: ubuntu-latest` y `actions/checkout@v4`; todos salvo `imagen` (que
   compila dentro de Docker) usan además `actions/setup-dotnet@v4`
   con `dotnet-version: '10.0.x'` (el repo no tiene `global.json`).
   Ningún paso usa `continue-on-error`.

### Compilación y pruebas

2. **Given** un push con un error de compilación o una advertencia (los proyectos de `src/` tienen
   `TreatWarningsAsErrors`) **When** corre el flujo **Then** el job `compilar` falla y ningún otro
   job arranca (todos dependen de él directa o indirectamente).
3. **Given** un push donde falla una prueba de `HospitalTurnos.Dominio.Tests` **When** corre el
   flujo **Then** el job `unit` falla, `integracion` y `cobertura` no arrancan, y los resultados
   (`.trx` y cobertura) se suben igual como artefacto `resultados-unit` (paso con `if: always()`).
4. **Given** un push donde falla una prueba de integración, o Docker no está disponible para
   Testcontainers **When** corre el job `integracion` **Then** el job falla con el error de la prueba
   o de Testcontainers; **ninguna** prueba se saltea por falta de Docker. Los resultados se suben
   igual como `resultados-integracion` (`if: always()`).

### Cobertura — NFR-11

5. **Given** los reportes `coverage.cobertura.xml` de los jobs `unit` e `integracion` **When**
   `scripts/ci/cobertura.sh <carpeta-con-reportes> <carpeta-salida>` corre **Then** ReportGenerator
   (herramienta local fijada en `.config/dotnet-tools.json`, el mismo manifiesto que creó la story 1.1
   con `dotnet-ef`) los une filtrando
   `-assemblyfilters:+HospitalTurnos.Dominio;+HospitalTurnos.Aplicacion`, genera `Summary.json`,
   `SummaryGithub.md` y el HTML; si `summary.linecoverage >= 80` el script imprime
   `Cobertura de líneas: N % (umbral 80 %) — OK` y sale con 0. En CI, el resumen Markdown se agrega
   a `$GITHUB_STEP_SUMMARY` y la carpeta se sube como artefacto `reporte-cobertura` (`if: always()`).
6. **Given** una cobertura de líneas de Dominio + Aplicacion menor a 80 % **When** corre el script
   **Then** imprime `::error::Cobertura de líneas N % < 80 % en Dominio y Aplicacion (NFR-11)`, sale
   con 1, el job `cobertura` falla y el flujo queda en rojo. El reporte HTML se sube igual, para ver
   qué líneas faltan.
7. **Given** que no hay ningún reporte de cobertura en la carpeta, o el resumen da
   `coverablelines = 0` **When** corre el script **Then** imprime
   `::error::No hay cobertura medible de Dominio y Aplicacion` y sale con 1. **Nunca** un verde sin
   haber medido. **Given** que el resumen trae uno solo de los dos ensamblados (falta el reporte de
   Dominio o el de Aplicacion) **Then** imprime `::error::Falta la cobertura de HospitalTurnos.<Ensamblado>`
   y sale con 1: el umbral no se mide sobre una sola capa.
8. La cobertura de ramas se informa en el resumen pero **no** es umbral (NFR-11 habla de cobertura
   de las reglas; el umbral acordado es de líneas).

### Dependencias vulnerables (SCA)

9. **Given** un paquete con vulnerabilidad conocida (directo o transitivo) **When** corre
   `scripts/ci/sca.sh` **Then** imprime `::error::Hay dependencias con vulnerabilidades conocidas.`
   y sale con 1. Sin vulnerabilidades imprime `Sin vulnerabilidades conocidas.` y sale con 0. El
   veredicto sale de `dotnet list package --vulnerable --include-transitive --format json` contado con
   `jq` (paquetes con `vulnerabilities`), **no** del texto de la salida: el texto cambia con el idioma
   del SDK. El script muestra igual el listado legible.
10. **Given** que `dotnet list package --vulnerable` mismo falla (sin red, restore roto) **When**
    corre `scripts/ci/sca.sh` **Then** sale con el código de error de ese comando (el script usa
    `set -euo pipefail`): un comando que falló no se lee como "no hay vulnerabilidades". El script
    muestra la salida de `dotnet list` y `::error::No se pudo listar las dependencias (dotnet list salió con N).`
    Si el listado sale bien pero trae avisos (`problems`, por ejemplo no pudo bajar la base de
    vulnerabilidades), imprime `::error::No se pudo revisar las dependencias: el listado trae avisos.` y sale con 1;
    si no trae ningún proyecto, `::error::No se pudo revisar las dependencias: el listado no trae ningún proyecto.` y sale con 1.

### Verificación local

11. `scripts/ci/local.sh` corre, en el mismo orden, **los mismos comandos** que el flujo
    (compilar → unitarias → integración → cobertura → SCA → imagen) y termina con 0 si todo pasa,
    para que el docente reproduzca la CI en su máquina sin subir nada. Los scripts funcionan con el
    bash 3.2 de macOS y con el bash de `ubuntu-latest`.
### No-regresión

12. No se modifica ningún proyecto ni prueba de las stories 1.1 a 1.4; no se agrega ningún filtro
    que excluya pruebas (`--filter`) en el flujo ni en `local.sh`. `.gitignore` agrega `resultados/`
    y `reporte-cobertura/` sin quitar entradas existentes. `.config/dotnet-tools.json` (creado por la
    story 1.1) **conserva** `dotnet-ef` y suma `dotnet-reportgenerator-globaltool` 5.5.9: el manifiesto
    se modifica, no se vuelve a crear.
    `@runtime-exempt(DODRT-C8): es una restricción de no-regresión, no una acción del usuario; la evidencia es el git diff sin cambios en src/ y tests/ y el manifiesto con las dos herramientas`

### Imagen Docker

13. **Given** el `Dockerfile` de la story 1.1 (`src/HospitalTurnos.Web/Dockerfile`) **When** corre
    `scripts/ci/imagen.sh` (job `imagen` y paso final de `local.sh`) **Then** el script hace
    `docker build -f src/HospitalTurnos.Web/Dockerfile -t hospital-turnos-web:ci .` desde la raíz,
    verifica que la imagen trae el archivo de zona horaria `/usr/share/zoneinfo/America/Argentina/Cordoba`
    (lo necesita el reloj del hospital de la story 1.2) copiándolo con `docker create` + `docker cp -L`
    (sigue enlaces; funciona aunque la imagen no tenga shell), imprime `Imagen construida con zona horaria.` y sale
    con 0. **Given** un `Dockerfile` roto (o una imagen sin ese archivo) **Then** imprime
    `::error::No se pudo construir la imagen Docker` (o `::error::La imagen no trae la zona horaria America/Argentina/Cordoba`)
    y sale con 1. El script acepta como primer argumento opcional otro `Dockerfile` (lo usa T4) y
    borra siempre el contenedor temporal (`trap`).

### Corrida en GitHub Actions

14. **Given** la rama de la story subida a GitHub (`https://github.com/fernandobono/BMAD-Hospital`,
    repo público; el push lo aprueba el docente) **When** GitHub Actions corre el flujo `ci.yml`
    disparado por ese push **Then** los seis jobs terminan en verde, el resumen de la corrida muestra
    la tabla de cobertura de `SummaryGithub.md`, y la corrida tiene los artefactos
    `resultados-unit`, `resultados-integracion` y `reporte-cobertura`. La protección de ramas
    (exigir el flujo en verde para mergear) **no** es parte de esta story.

### Tests (5 verificaciones)

| # | Test | AC | Nivel | Escenario | Resultado esperado |
|---|------|----|-------|-----------|--------------------|
| T1 | `WorkflowCi_EstructuraYActionlint_SinErrores` | 1,2,3,4,8,9,10,11,12,13 | unit | `scripts/ci/pruebas/probar-workflow.py` carga `ci.yml` con PyYAML y verifica disparadores, `permissions`, ids, nombres y `needs` de la tabla del AC 1 (incluido `imagen`), `if: always()` en los pasos de subida, ausencia de `continue-on-error` y de `--filter`, un `LogFileName` distinto por cada `dotnet test`, y que `sca.sh`, `cobertura.sh` e `imagen.sh` empiezan con `set -euo pipefail` y que `sca.sh` decide con `--format json` (no por texto); lee `.config/dotnet-tools.json` (tiene `dotnet-ef` **y** `dotnet-reportgenerator-globaltool` 5.5.9) y `.gitignore` (tiene `resultados/` y `reporte-cobertura/`); verifica que `local.sh` contiene el comando de cada paso del flujo, en el mismo orden (AC 11); después corre `actionlint` en Docker sobre el archivo | Imprime `T1 WorkflowCi_EstructuraYActionlint_SinErrores OK` y sale con 0; ante cualquier diferencia imprime cuál y sale con 1 |
| T2 | `UmbralCobertura_TresReportesDePrueba_PasaSoloSobreOchenta` | 5,6,7 | integration | `scripts/ci/pruebas/probar-umbral.sh` corre `cobertura.sh` (ReportGenerator real) en cinco casos: `cobertura-alta` (Dominio y Aplicacion al 90 %), `cobertura-baja` (Dominio y Aplicacion al 50 %, más un paquete `HospitalTurnos.Web` al 100 % que el filtro debe ignorar), `cobertura-sin-lineas` (sólo Web), `cobertura-solo-dominio` (Dominio al 100 %, sin Aplicacion) y una carpeta vacía | alta → código 0 y `— OK`; baja → código 1 y `::error::Cobertura de líneas 50` (prueba que Web no infla el número); vacía y sin-lineas → código 1 y `No hay cobertura medible`; solo-dominio → código 1 y `::error::Falta la cobertura de HospitalTurnos.Aplicacion`. Imprime `T2 ... OK` |
| T3 | `CiLocal_MismosComandosQueElFlujo_TerminaEnVerde` | 11,13 | integration | Con Docker andando, `scripts/ci/local.sh` sobre el repo con las stories 1.1–1.4 hechas | Sale con 0, imprime la cobertura ≥ 80 %, `Sin vulnerabilidades conocidas.` e `Imagen construida con zona horaria.`; deja el HTML en `reporte-cobertura/index.html` |
| T4 | `ImagenDocker_DockerfileRoto_FallaYElDelRepoConstruye` | 13 | integration | `scripts/ci/pruebas/probar-imagen.sh` corre `imagen.sh` dos veces: con `scripts/ci/pruebas/datos/Dockerfile.roto` y con el `Dockerfile` real | roto → código 1, `::error::No se pudo construir la imagen Docker` y el log trae el error `MSB1009` del proyecto inexistente (falló por el motivo esperado); real → código 0 e `Imagen construida con zona horaria.`; después de las dos corridas `docker ps -a` no muestra contenedores temporales del script. Imprime `T4 ... OK` |
| T5 | `CorridaGithub_PushDeLaRama_SeisJobsEnVerde` | 14 | e2e | Después del push aprobado, `gh run watch` sobre la corrida del flujo `CI` de la rama y `gh run view --json jobs` | Los seis jobs con `conclusion: success`; `gh run view` lista los tres artefactos; captura de la corrida en el navegador para el registro de la sesión |

> Estas cinco verificaciones son scripts o comandos, no pruebas xUnit: la regla de `[Trait("Story")]` del
> contrato no aplica. Cada script imprime su T-ID y su nombre para que el cruce tabla ↔ código
> los encuentre.

#### Given/Fixture

- **T2** — `cobertura-alta.cobertura.xml`: paquetes `HospitalTurnos.Dominio` y `HospitalTurnos.Aplicacion`,
  10 líneas cada uno, 9 con `hits="1"`. `cobertura-baja.cobertura.xml`: `HospitalTurnos.Dominio` y
  `HospitalTurnos.Aplicacion` con 2 líneas y 1 cubierta cada uno, y `HospitalTurnos.Web` con 10 líneas
  todas cubiertas (sin filtro daría 85,7 %). `cobertura-sin-lineas.cobertura.xml`: sólo un paquete
  `HospitalTurnos.Web`. `cobertura-solo-dominio.cobertura.xml`: sólo `HospitalTurnos.Dominio`, 10 de 10. El formato mínimo que
  ReportGenerator 5.5.9 acepta (verificado al escribir esta story):
  ```xml
  <?xml version="1.0" encoding="utf-8"?>
  <coverage line-rate="0.5" branch-rate="0" version="1.9" timestamp="1" lines-covered="1" lines-valid="2" branches-covered="0" branches-valid="0">
    <sources><source>/x/</source></sources>
    <packages><package name="HospitalTurnos.Dominio" line-rate="0.5" branch-rate="0" complexity="1">
      <classes><class name="A" filename="A.cs" line-rate="0.5" branch-rate="0" complexity="1"><methods/>
        <lines><line number="1" hits="1" branch="False"/><line number="2" hits="0" branch="False"/></lines>
      </class></classes></package></packages>
  </coverage>
  ```
  Con `-assemblyfilters:+HospitalTurnos.Dominio;+HospitalTurnos.Aplicacion` y
  `-reporttypes:JsonSummary` ese archivo da `"linecoverage": 50` en `Summary.json`.
- **T4** — `Dockerfile.roto`: dos líneas, `FROM mcr.microsoft.com/dotnet/sdk:10.0` y
  `RUN dotnet publish proyecto-que-no-existe.csproj` (falla en la construcción, sin depender de la red
  más allá de la imagen base).

## Tasks / Subtasks

- [x] [SEQ] Task 1: Salvaguarda de estado (AC: todos)
  - [x] Confirmar que la story 1.1 está hecha: existen los 7 proyectos del contrato, hay **un solo**
        archivo de solución en la raíz (hoy hay `HospitalTurnos.slnx`; el contrato dice `.sln` — los
        comandos de esta story no nombran la solución, así que sirven para cualquiera de los dos,
        pero si hubiera dos `dotnet build` falla) y `dotnet build -c Release` da 0 errores.
  - [x] Confirmar que los 3 proyectos de pruebas referencian `coverlet.collector` (contrato §Paquetes).
  - [x] Correr `dotnet test tests/HospitalTurnos.Web.Tests` aunque todavía no tenga pruebas: con
        xUnit 2.9 sobre VSTest, cero pruebas sale con 0. Si sale distinto, anotarlo como spec anchor.
  - [x] Confirmar `docker info` (Testcontainers, actionlint y la imagen lo necesitan) y `jq --version`.
  - [x] Confirmar que existen `.config/dotnet-tools.json` con `dotnet-ef` y `src/HospitalTurnos.Web/Dockerfile`
        (los crea la story 1.1), y que las stories 1.2, 1.3 y 1.4 están hechas (sin sus pruebas, T3 no
        llega al 80 %).
  - [x] Si algo no coincide → DETENERSE y corregir la story primero.
- [x] [SEQ] Task 2: Herramienta local de ReportGenerator (AC: 5, 12)
  - [x] **Sólo** `dotnet tool install dotnet-reportgenerator-globaltool --version 5.5.9`: el manifiesto
        `.config/dotnet-tools.json` ya existe (story 1.1, con `dotnet-ef`). **No** correr
        `dotnet new tool-manifest` (falla si el archivo existe y con `--force` lo pisa y se pierde `dotnet-ef`).
  - [x] Verificar después: `dotnet tool list` muestra las dos herramientas.
- [x] [SEQ] Task 3: `scripts/ci/cobertura.sh` (AC: 5, 6, 7, 8)
  - [x] `set -euo pipefail`; argumentos: carpeta de reportes (busca `**/coverage.cobertura.xml` y
        `*.cobertura.xml`) y carpeta de salida.
  - [x] Si no hay archivos → error del AC 7 sin llamar a ReportGenerator.
  - [x] `dotnet tool restore` + `dotnet reportgenerator` con filtro y `-reporttypes:"JsonSummary;MarkdownSummaryGithub;Html"`.
  - [x] Leer `summary.coverablelines` y `summary.linecoverage` con `jq`; comparar con 80 (umbral en
        una variable `UMBRAL_LINEAS=80` al principio, con comentario que cita NFR-11).
  - [x] Si existe `$GITHUB_STEP_SUMMARY`, agregarle `SummaryGithub.md`.
- [x] [PAR] Task 4: `scripts/ci/sca.sh` (AC: 9, 10)
  - [x] Adaptar el paso del proyecto guía (ver Patrón) con `set -euo pipefail`, pero decidiendo con
        `--format json` + `jq` en vez de buscar texto (el texto depende del idioma del SDK).
- [x] [PAR] Task 4b: `scripts/ci/imagen.sh` (AC: 13)
  - [x] `set -euo pipefail`; argumento opcional `DOCKERFILE` (por defecto `src/HospitalTurnos.Web/Dockerfile`).
  - [x] `docker build -f "$DOCKERFILE" -t hospital-turnos-web:ci .`; si falla, `::error::` del AC 13 y salir con 1.
  - [x] `id=$(docker create hospital-turnos-web:ci)` + `trap 'docker rm -f "$id" >/dev/null' EXIT`;
        `docker cp -L "$id:/usr/share/zoneinfo/America/Argentina/Cordoba" -` a `/dev/null`; si falla, el otro `::error::`.
- [x] [SEQ] Task 5: `.github/workflows/ci.yml` (AC: 1, 2, 3, 4, 5, 8, 12, 13)
  - [x] Jobs, nombres y `needs` exactamente como la tabla del AC 1. Comentario de cabecera en
        castellano que explique por qué el orden (feedback rápido), en la densidad del proyecto guía.
  - [x] `unit`: `dotnet test tests/HospitalTurnos.Dominio.Tests -c Release --logger "trx;LogFileName=unit.trx" --collect:"XPlat Code Coverage" --results-directory ./resultados/unit`; subir `resultados-unit` con `if: always()`.
  - [x] `integracion`: lo mismo para `Aplicacion.Tests` y `Web.Tests` en `./resultados/integracion`, con **un
        nombre de `.trx` por proyecto** (`LogFileName=aplicacion.trx` y `LogFileName=web.trx`; con el mismo
        nombre el segundo pisa al primero); subir `resultados-integracion` con `if: always()`.
  - [x] `cobertura`: `actions/download-artifact@v4` con `pattern: resultados-*` y `merge-multiple: true` en `./resultados`; `bash scripts/ci/cobertura.sh ./resultados ./reporte-cobertura`; subir `reporte-cobertura` con `if: always()`.
  - [x] `sca`: `dotnet restore` + `bash scripts/ci/sca.sh`.
  - [x] `imagen`: checkout + `bash scripts/ci/imagen.sh` (Docker viene en `ubuntu-latest`; no hace falta `setup-dotnet`).
- [x] [SEQ] Task 6: `scripts/ci/local.sh` (AC: 11, 13)
  - [x] Mismos comandos que el flujo, en el mismo orden (compilar → unitarias → integración → cobertura →
        SCA → imagen), cortando en el primer fallo; borra `resultados/` al empezar.
- [x] [SEQ] Task 7: Verificaciones T1–T4 y `.gitignore` (AC: 1–13)
  - [x] `probar-workflow.py`, `probar-umbral.sh`, `probar-imagen.sh` y los cinco datos de prueba.
  - [x] Agregar `resultados/` y `reporte-cobertura/` a `.gitignore`.
  - [x] Correr T1, T2, T3, T4 y anotar el resultado en el Dev Agent Record.
- [x] [SEQ] Task 8: Corrida en GitHub (AC: 14) — con el push aprobado: `gh run watch`, `gh run view --json jobs,artifacts` y captura de la corrida (T5).

### Task Dependencies

Task 1 → 2 → 3 → 5 → 6 → 7 → 8 (secuencial; la Task 8 va después del commit y del push aprobados).
Las Tasks 4 y 4b pueden hacerse en paralelo con las 2–3 (no dependen de ReportGenerator).

## Dev Notes

### Que existe hoy

#### En hospital-turnos

Verificado el 2026-10-16, con las stories 1.1 a 1.4 en `main`:

- **No existe** `.github/`. El repo tiene remoto: `origin` → `https://github.com/fernandobono/BMAD-Hospital`
  (público, rama por defecto `main`). Hasta hoy no corre ningún flujo de GitHub Actions.
- `.config/dotnet-tools.json` existe (story 1.1) con `dotnet-ef` 10.0.13; esta story le suma
  ReportGenerator. `scripts/` tiene sólo `smoke-compose.sh` (1.1); esta story crea `scripts/ci/`.
- Solución en la raíz: `HospitalTurnos.slnx` (un solo archivo de solución), con los 7 proyectos del
  contrato. No hay `global.json`.
- Los 3 proyectos de pruebas (`Dominio.Tests`, `Aplicacion.Tests`, `Web.Tests`) usan xUnit 2.9.3 sobre
  VSTest (`Microsoft.NET.Test.Sdk` 18.11.0) y referencian `coverlet.collector` 10.1.0 (gestión central
  en `Directory.Packages.props`). `Aplicacion.Tests` y `Web.Tests` usan `Testcontainers.PostgreSql` 4.16.0.
- `src/HospitalTurnos.Web/Dockerfile` existe (1.1): compila con `sdk:10.0` desde la raíz del repo y
  corre sobre `aspnet:10.0` con usuario sin privilegios.
- `.gitignore` ya ignora `.env` y también `coverage*.json` y `coverage*.xml`
  (por eso los datos de prueba de T2 se llaman `cobertura-*.cobertura.xml`, que no caen en ese patrón).
- Las pruebas de reglas con cobertura medible las crearon las stories 1.2, 1.3 y 1.4.

#### En la máquina del docente (verificado al escribir la story, 2026-10-09)

- SDK `10.0.400`; `docker` en `/usr/local/bin/docker`; `jq` en `/usr/bin/jq`; `python3` con PyYAML.
- `dotnet-reportgenerator-globaltool 5.5.9` instalado como herramienta **global** (no está en el
  PATH del shell: vive en `~/.dotnet/tools`). Por eso se fija como herramienta **local** del repo:
  CI y docente usan la misma versión y el mismo comando (`dotnet reportgenerator`).
- **ReportGenerator 5.5.9 no tiene opción de umbral mínimo** (`reportgenerator -help` no lista
  `minimumCoverageThresholds`; es de la edición paga). Por eso el umbral se compara en el script con
  `jq` sobre `Summary.json`. Verificado: con un reporte de prueba, `rc=0` y `"linecoverage": 50`; sin
  reportes (`-reports:nada/**/*.xml`), `rc=1`; con un reporte que sólo trae `HospitalTurnos.Web`,
  `rc=0`, `"coverablelines": 0` y `"linecoverage": null` (por eso el AC 7 mira `coverablelines`
  antes de comparar). `Summary.json` lista los ensamblados medidos en `coverage.assemblies[].name`
  (de ahí sale el chequeo de las dos capas). La salida Markdown se llama `SummaryGithub.md`.
- `actionlint` **no** está instalado; se usa por Docker: `docker run --rm -v "$PWD:/repo" -w /repo rhysd/actionlint:latest`.

### Patron de referencia verificado

**Patrón**: CI por niveles de la pirámide con `needs` encadenados, resultados subidos con
`if: always()` y SCA en paralelo que falla si hay vulnerabilidades — tomado del proyecto guía.

| Proyecto | Ancla `archivo:línea` | Qué se verificó exactamente |
|---|---|---|
| changomas-testing-csharp | `.github/workflows/ci.yml:1-15` | Comentario de cabecera que explica el orden como estrategia de feedback rápido. Se replica el estilo. |
| changomas-testing-csharp | `.github/workflows/ci.yml:19-23` | Disparadores `push` (sólo `main`), `pull_request`, `workflow_dispatch`. **Acá se cambia** a todas las ramas: la story pide "en cada push". |
| changomas-testing-csharp | `.github/workflows/ci.yml:32-41` | Job `compilar`: checkout + setup-dotnet `10.0.x` + `restore` + `build --no-restore --configuration Release`. Se copia tal cual. |
| changomas-testing-csharp | `.github/workflows/ci.yml:44-69` | Job `unit` con `needs: compilar`, `--collect:"XPlat Code Coverage"`, `--results-directory`, y subida con `if: always()` (comentario líneas 62-63). Se copia. |
| changomas-testing-csharp | `.github/workflows/ci.yml:94-104` | Job `integracion` con `needs: [unit, contrato]`. Acá no hay Pact: `needs: unit`. El guía **no** junta cobertura de integración ni la sube; acá sí, porque Aplicacion se cubre con pruebas de integración. |
| changomas-testing-csharp | `.github/workflows/ci.yml:140-160` | Job `seguridad` (SCA) con `needs: compilar`: `dotnet list package --vulnerable --include-transitive 2>&1 \| tee vulnerables.txt` y `grep -q "tiene la siguiente\|has the following vulnerable"`. Se adapta: **sin** `pipefail` el código de salida de `dotnet list` se pierde en el `tee` (AC 10), y el `grep` por texto no ve el castellano real del SDK: acá se decide con `--format json` (AC 9). |

**Lo que el guía no tiene (opción B, con evidencia)**:

- **Umbral de cobertura**: `grep -rn -i -E "reportgenerator|threshold" --include='*.yml' --include='*.csproj' .`
  en el guía → sólo aparece `threshold` en `docs/T05-No-Funcionales.md:38` (umbrales de k6, no de
  cobertura). El guía recolecta cobertura pero no la mide contra un umbral. Esta story crea ese paso.
- **Testcontainers**: el mismo `grep` con `testcontainers` → 0 resultados; el guía usa SQLite en
  archivo (`tests/ChangoMas.Tests.Integracion/CajaWebApplicationFactory.cs:22-23,36`). Acá la base es
  PostgreSQL real; en `ubuntu-latest` Docker viene instalado, así que no hace falta `services:`.
- **E2E**: el guía tiene el job `e2e` (`ci.yml:107-137`). Acá no: el proyecto E2E no existe en el
  sprint 1 (contrato §Proyectos). Lo agrega la story 10.1.
- **Imagen Docker**: el guía no tiene `Dockerfile`. Acá la imagen es lo que se despliega: un
  `Dockerfile` roto pasaría la CI en verde si nadie lo construye (job `imagen`, AC 13).

**Por qué el job `imagen` no corre el smoke de Compose (`scripts/smoke-compose.sh` de 1.1)**: el
smoke necesita un `.env` con las contraseñas de la base (contrato §Secretos: no hay contraseñas
versionadas, ni de desarrollo) y los puertos 5432/8080 libres; en la máquina del docente chocaría con
su Compose de desarrollo. La construcción + la zona horaria atrapan lo que se rompe al tocar el
`Dockerfile`; el arranque real de la app contra PostgreSQL ya lo prueban las pruebas de integración
(`WebApplicationFactory` + Testcontainers). El smoke E1 de 1.1 sigue siendo manual.

### Que hay que hacer (la delta)

1. Crear `ci.yml` copiando la forma del guía, con 5 cambios: push en todas las ramas, `permissions`
   mínimos, cobertura también en integración, un job `cobertura` que une y mide, y un job `imagen`
   que construye la imagen Docker.
2. Sacar los comandos con lógica a `scripts/ci/*.sh` para que el docente los corra igual en su
   máquina, sin depender de subir la rama.
3. Sumar ReportGenerator 5.5.9 al manifiesto de herramientas que ya creó la 1.1.
4. Escribir las verificaciones T1–T4 y los datos de prueba; con el push aprobado, ver la corrida real (T5).

**Cómo se mide la cobertura (NFR-11)** — para el comentario del script y el README de clase:

- Qué: **cobertura de líneas** de los ensamblados `HospitalTurnos.Dominio` y
  `HospitalTurnos.Aplicacion`, **sumando** lo que cubren todas las pruebas (unitarias + integración).
  Infraestructura y Web quedan afuera: NFR-11 habla de las reglas de negocio, y la arquitectura
  (§8) dice que las reglas viven en Dominio y Aplicacion.
- Umbral: 80 % de líneas. Las ramas se informan, no se exigen.
- **Riesgo aceptado (M-12)**: la política de reintentos del envío de emails (S-33) vive en
  `MensajeSaliente`, en Infraestructura, y por eso **no** entra en el número del 80 %. Se acepta así:
  la prueban las pruebas de integración de la story 1.4, que corren en el job `integracion`; si
  alguien la rompe, el flujo queda en rojo por esas pruebas aunque la cobertura no baje. Medir una
  sola clase de Infraestructura obligaría a filtros por clase que mezclan criterios.
- Si baja: el job `cobertura` queda en rojo y con él el flujo. Que eso **impida mergear** depende de
  la protección de ramas de GitHub, que se configura al crear el repo (decisión del docente, fuera
  de esta story).

### Lo que NO hay que hacer

- **NO hacer push sin la aprobación del docente** (AC 14), ni configurar la protección de ramas.
- **NO usar `continue-on-error` ni `|| true`** en pasos de prueba o de cobertura: convierte un rojo en
  verde. El `|| true` del guía (`ci.yml:121`) es para instalar una herramienta, no aplica acá.
- **NO esconder errores con `2>/dev/null` ni con pipes sin `pipefail`**: una salida vacía de un
  comando que falló no es "no hay vulnerabilidades".
- **NO decidir el SCA por el texto de la salida**: cambia con el idioma del SDK. Verificado el
  2026-10-16 con `Newtonsoft.Json` 12.0.1: en castellano dice "tiene los paquetes vulnerables
  siguientes", que no contiene el "tiene la siguiente" del proyecto guía.
- **NO usar `coverlet.msbuild` ni `/p:Threshold`**: el contrato fija `coverlet.collector`, y el umbral
  por proyecto no sirve cuando Aplicacion se cubre desde otro proyecto de pruebas.
- **NO medir la cobertura de toda la solución**: Web e Infraestructura bajarían el número por
  razones que no son reglas de negocio, y el umbral dejaría de significar lo que NFR-11 pide.
- **NO agregar el job E2E ni Playwright**: el proyecto no existe hasta la story 10.1.
- **NO correr `dotnet new tool-manifest`**: el manifiesto ya existe (1.1); sólo `dotnet tool install`.
- **NO buscar `data["on"]` en `probar-workflow.py`**: PyYAML lee YAML 1.1 y convierte la clave `on:`
  en el booleano `True`. Usar `data.get("on", data.get(True))`.
- **NO usar el mismo `LogFileName` en dos `dotnet test` que escriben en la misma carpeta**: el
  segundo `.trx` pisa al primero.
- **NO correr `docker compose down -v` desde estos scripts**: en la máquina del docente borraría su
  base de desarrollo.
- **NO agregar cache de NuGet** (`setup-dotnet` con `cache: true` exige `packages.lock.json`, que el
  contrato no define).

### Como verificar que funciona

```bash
cd ~/SourceCode/hospital-turnos

# T1 — estructura del flujo + actionlint (Docker)
python3 scripts/ci/pruebas/probar-workflow.py
# Esperado: "T1 WorkflowCi_EstructuraYActionlint_SinErrores OK", código 0

# T2 — el umbral con datos de prueba (ReportGenerator real)
bash scripts/ci/pruebas/probar-umbral.sh
# Esperado: "T2 UmbralCobertura_TresReportesDePrueba_PasaSoloSobreOchenta OK", código 0

# T4 — la imagen Docker (roto → 1, real → 0; ~1-3 min la primera vez)
bash scripts/ci/pruebas/probar-imagen.sh
# Esperado: "T4 ImagenDocker_DockerfileRoto_FallaYElDelRepoConstruye OK", código 0

# T3 — los mismos comandos del flujo, en la máquina (necesita Docker; ~3-5 min)
bash scripts/ci/local.sh; echo "rc=$?"
# Esperado: rc=0, "Cobertura de líneas: N % (umbral 80 %) — OK", "Sin vulnerabilidades conocidas.",
#           "Imagen construida con zona horaria."
open reporte-cobertura/index.html

# Ver que el umbral muerde de verdad: bajar el umbral a 101 y correr sólo la cobertura
UMBRAL_LINEAS=101 bash scripts/ci/cobertura.sh ./resultados ./reporte-cobertura; echo "rc=$?"
# Esperado: "::error::Cobertura de líneas ... < 101 %", rc=1
```

```bash
# T5 — después del push aprobado de la rama
gh run watch "$(gh run list --branch story/1-5-integracion-continua --workflow ci.yml --limit 1 --json databaseId --jq '.[0].databaseId')" --exit-status
gh run view --json jobs,artifacts
```

(Para que el último comando funcione, `cobertura.sh` toma el umbral de la variable de entorno
`UMBRAL_LINEAS` si está definida, y si no usa 80.)

## Dev Agent Record

### Agent Model Used

Claude Opus 5.5 (Amelia, bmad-dev-story) — sesión 06, 2026-10-16.

### Debug Log References

- Corrida del pipeline: `.advisors/pipeline-runs/1.5-20261016142849/` (local, no versionada).
- Evidencia de la prueba en vivo: `.advisors/dod-runtime/1.5-smoke-ledger.jsonl` y `1.5-smoke-outputs/`.

### Completion Notes List

- Pruebas primero: T1, T2 y T4 escritas y corridas en rojo antes de `ci.yml` y de los scripts.
- T1 OK (incluye actionlint por Docker), T2 OK (5 casos con ReportGenerator real), T4 OK, T3 OK:
  86 pruebas (20 + 34 + 32), cobertura de líneas de Dominio + Aplicacion 97,4 % (ramas 100 %),
  sin vulnerabilidades, imagen con zona horaria.
- Revisores: 3 vueltas (5, 4 y 4). Vuelta 1: el SCA no detectaba vulnerables con el SDK en castellano
  (verificado con un paquete vulnerable real) → decide con JSON; la cobertura exige las dos capas;
  `docker cp -L`; T4 más estricta. Vuelta 2: resumen viejo, listado sin proyectos, `MSB1009`.
  Vuelta 3: limpia.
- Prueba en vivo (copias del repo en una carpeta temporal): advertencia → corta en Compilar; prueba
  unitaria rota → corta antes de integración y deja el `.trx`; sin Docker → las de integración fallan
  (ninguna se saltea); umbral 101 → rojo; `log4net` 2.0.9 → rojo en castellano e inglés; origen de
  paquetes inexistente → rojo con el motivo.
- Hallazgo de la prueba en vivo: en este repo un paquete vulnerable ya rompe el `restore` (la 1.4 dejó
  NuGetAudit como error), así que la CI lo frena desde "Compilar"; el SCA queda como segunda red y
  para los avisos de severidad baja.
- `.trx` de `dotnet test` copia la cobertura en `In/<máquina>/`: ReportGenerator la une sin duplicar.
- T5: push aprobado por el docente; corrida
  [38926262486](https://github.com/fernandobono/BMAD-Hospital/actions/runs/38926262486) disparada
  por el push del commit `d76f08f`: los 6 jobs en verde en 2 min 23 s, artefactos `resultados-unit`,
  `resultados-integracion` y `reporte-cobertura`; cobertura 97,4 % de líneas (38 de 39) y 100 % de
  ramas. El resumen del paso sólo se ve con sesión iniciada en GitHub; el artefacto trae el mismo
  `SummaryGithub.md`.
- Matriz por criterio: 14 de 14 corridos de verdad.
- Para seguir (fuera de esta story): la corrida avisa que las acciones `@v4` usan Node.js 20
  (deprecado) y que `ubuntu-latest` pasa a Ubuntu 26 el 19/10/2026; fijar las acciones por SHA
  (sugerencia del revisor de seguridad); la protección de ramas para exigir el flujo en verde.

### File List

- `.github/workflows/ci.yml` (nuevo)
- `scripts/ci/cobertura.sh`, `scripts/ci/sca.sh`, `scripts/ci/imagen.sh`, `scripts/ci/local.sh` (nuevos)
- `scripts/ci/pruebas/probar-workflow.py`, `probar-umbral.sh`, `probar-imagen.sh` (nuevos)
- `scripts/ci/pruebas/datos/cobertura-alta.cobertura.xml`, `cobertura-baja.cobertura.xml`,
  `cobertura-sin-lineas.cobertura.xml`, `cobertura-solo-dominio.cobertura.xml`, `Dockerfile.roto` (nuevos)
- `.config/dotnet-tools.json` (modificado: suma ReportGenerator 5.5.9)
- `.gitignore` (modificado: `resultados/`, `reporte-cobertura/`)
- `docs/stories/1-5-integracion-continua.md` (esta story)

### Change Log

- 2026-10-09: Story escrita (Mary).
- 2026-10-09: Correcciones de la revisión adversarial del sprint 1: I-11 (manifiesto de 1.1 se
  modifica, no se crea), I-12 (`depends_on` 1.1–1.4), I-13 (job `imagen`, AC 13, T4), M-9, M-10,
  M-12 (riesgo aceptado). 13 AC, 4 verificaciones.
- 2026-10-16: Validación antes de implementar (sesión 06): "Que existe hoy" al día con 1.1–1.4 en
  `main`; el repo ya tiene remoto en GitHub, así que el AC 11 deja de prohibir el push y se agrega el
  AC 14 (corrida real en GitHub Actions, T5). Decisión del docente: "Corregir y sumar GitHub".
- 2026-10-16: Revisores, vuelta 1 (decisión del docente: "Arreglar los 4"): SCA decide con
  `--format json` (el texto en castellano no coincidía: AC 9, T1, Task 4); la cobertura exige los dos
  ensamblados (AC 7, T2 con `cobertura-solo-dominio`); `docker cp -L` (AC 13); T4 exige el motivo del
  error del `Dockerfile.roto`; T1 compara los comandos de `local.sh` con los del flujo (AC 11). En la
  prueba en vivo apareció que `sca.sh` fallaba sin decir por qué si el listado no corría: ahora muestra
  la salida del SDK y sale con el mismo código (AC 10).
- 2026-10-16: Revisores, vuelta 2: AC 10 nombra los avisos y el listado sin proyectos; `cobertura.sh` borra
  el resumen anterior antes de medir; T4 exige `MSB1009`.


## Review Follow-ups (Advisors)

<!-- tycon-pipeline: generado por Step 6/7. Cada item = finding advisor no resuelto (CRITICAL waived o WARNING). Al resolver el finding en el código, marcá [x]. Step 0/1 detecta los abiertos al retomar (pipeline.js followups-status). -->

- [x] **[CRITICAL]** SILENT-W2 — `scripts/ci/sca.sh` — Confirmado por el orquestador con Newtonsoft.Json 12.0.1: el SDK en castellano imprime 'tiene los paquetes vulnerables siguientes', que no contiene 'tiene la siguiente' → local.sh diría 'Sin vulnerabilidades conocidas' con un paquete vulnerable. Escalado a CRITICAL. <!-- fkey:SILENT-W2|scripts/ci/sca.sh -->
- [x] **[WARNING]** SILENT-W2 — `scripts/ci/cobertura.sh` — Si falta el reporte de Dominio o de Aplicacion, el umbral se mide sobre una sola capa. <!-- fkey:SILENT-W2|scripts/ci/cobertura.sh -->
- [x] **[WARNING]** SILENT-W2 — `scripts/ci/imagen.sh` — docker cp sin -L; hoy Cordoba es archivo regular (verificado), endurecimiento. <!-- fkey:SILENT-W2|scripts/ci/imagen.sh -->
- [x] **[WARNING]** SILENT-W2 — `scripts/ci/pruebas/probar-imagen.sh` — El caso roto acepta cualquier falla de build. <!-- fkey:SILENT-W2|scripts/ci/pruebas/probar-imagen.sh -->
- [x] **[WARNING]** ACCOV-W1 — `scripts/ci/sca.sh` — AC 9/10: rutas de falla de sca.sh sin verificación ejecutada. → verificado en vivo: log4net 2.0.9 en rojo (castellano e inglés) y sin red en rojo. <!-- fkey:ACCOV-W1|scripts/ci/sca.sh -->
- [x] **[WARNING]** ACCOV-W1 — `scripts/ci/local.sh` — AC 11: T1 compara los comandos de local.sh con los de ci.yml en orden (mutación manual verificada en rojo). <!-- fkey:ACCOV-W1|scripts/ci/local.sh -->
- [x] **[WARNING]** ACCOV-W1 — `.github/workflows/ci.yml` — AC 2/3/4/6: flujo en rojo sólo verificado por estructura; se cubre en la prueba en vivo. → la prueba en vivo cubre los caminos en rojo. <!-- fkey:ACCOV-W1|.github/workflows/ci.yml -->
- [x] **[CRITICAL]** ACCOV-C1 — `scripts/ci/pruebas/` — tycon-ac-coverage-checker: falso positivo, el matcher no escanea scripts/ci/pruebas; T1-T4 existen e imprimen su T-ID; T5 es post-push. <!-- fkey:ACCOV-C1|scripts/ci/pruebas/ -->
- [x] **[WARNING]** VAL-W15 — `docs/stories/1-5-integracion-continua.md` — AC 10 nombra los avisos del listado; Task 7 'cinco' datos; T5 después de T4. <!-- fkey:VAL-W15|docs/stories/1-5-integracion-continua.md -->
