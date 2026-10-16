#!/usr/bin/env bash
# Cobertura de las reglas de negocio contra el umbral de NFR-11 (story 1.5, AC 5 a 8).
#
#   bash scripts/ci/cobertura.sh <carpeta-con-reportes> <carpeta-salida>
#
# Qué mide: la cobertura de LÍNEAS de los ensamblados HospitalTurnos.Dominio y
# HospitalTurnos.Aplicacion, sumando lo que cubren todas las pruebas (unitarias + integración).
# Infraestructura y Web quedan afuera: NFR-11 habla de las reglas de negocio, y las reglas viven en
# Dominio y Aplicacion (arquitectura §8). La cobertura de ramas se informa, pero no es umbral.
#
# ReportGenerator 5.5.9 no trae opción de umbral mínimo (es de la edición paga): por eso el número
# se compara acá, con jq sobre Summary.json. Sin reportes, o con 0 líneas medibles, el script falla:
# nunca un verde sin haber medido.
set -euo pipefail

# NFR-11: 80 % de líneas en Dominio + Aplicacion. Se puede pisar con la variable de entorno
# (por ejemplo UMBRAL_LINEAS=101 para ver que el control muerde).
UMBRAL_LINEAS="${UMBRAL_LINEAS:-80}"

if [ "$#" -ne 2 ]; then
  echo "Uso: bash scripts/ci/cobertura.sh <carpeta-con-reportes> <carpeta-salida>" >&2
  exit 2
fi

raiz="$(cd "$(dirname "$0")/../.." && pwd)"
entrada="$(cd "$1" && pwd)"
mkdir -p "$2"
salida="$(cd "$2" && pwd)"

# Los reportes de coverlet se llaman coverage.cobertura.xml; los datos de prueba, *.cobertura.xml.
reportes=""
while IFS= read -r archivo; do
  reportes="${reportes:+$reportes;}$archivo"
done < <(find "$entrada" -type f -name '*.cobertura.xml' | sort)

if [ -z "$reportes" ]; then
  echo "::error::No hay cobertura medible de Dominio y Aplicacion"
  echo "No se encontró ningún *.cobertura.xml en $entrada."
  exit 1
fi

cd "$raiz"
# ReportGenerator no limpia la carpeta de salida: sin esto, si no regenerara el resumen se leería el
# de una corrida anterior.
rm -f "$salida/Summary.json" "$salida/SummaryGithub.md"
dotnet tool restore > /dev/null
dotnet reportgenerator \
  "-reports:$reportes" \
  "-targetdir:$salida" \
  "-assemblyfilters:+HospitalTurnos.Dominio;+HospitalTurnos.Aplicacion" \
  "-reporttypes:JsonSummary;MarkdownSummaryGithub;Html" \
  "-verbosity:Warning"

resumen="$salida/Summary.json"
if [ -n "${GITHUB_STEP_SUMMARY:-}" ] && [ -f "$salida/SummaryGithub.md" ]; then
  cat "$salida/SummaryGithub.md" >> "$GITHUB_STEP_SUMMARY"
fi

lineas_medibles="$(jq -r '.summary.coverablelines // 0' "$resumen")"
if [ "$lineas_medibles" -eq 0 ]; then
  echo "::error::No hay cobertura medible de Dominio y Aplicacion"
  echo "Los reportes no traen líneas de HospitalTurnos.Dominio ni de HospitalTurnos.Aplicacion."
  exit 1
fi

# Las dos capas tienen que estar medidas: si falta el reporte de una, el umbral se mediría sobre la otra.
for ensamblado in HospitalTurnos.Dominio HospitalTurnos.Aplicacion; do
  if ! jq -e --arg nombre "$ensamblado" '[.coverage.assemblies[].name] | index($nombre) != null' "$resumen" > /dev/null; then
    echo "::error::Falta la cobertura de $ensamblado"
    echo "Ningún reporte trae líneas de $ensamblado: ¿corrieron sus pruebas con --collect:\"XPlat Code Coverage\"?"
    exit 1
  fi
done

cobertura="$(jq -r '.summary.linecoverage' "$resumen")"
ramas="$(jq -r '.summary.branchcoverage // "sin ramas"' "$resumen")"
echo "Cobertura de ramas: $ramas % (se informa, no es umbral)"

if jq -e --argjson umbral "$UMBRAL_LINEAS" '.summary.linecoverage >= $umbral' "$resumen" > /dev/null; then
  echo "Cobertura de líneas: $cobertura % (umbral $UMBRAL_LINEAS %) — OK"
else
  echo "::error::Cobertura de líneas $cobertura % < $UMBRAL_LINEAS % en Dominio y Aplicacion (NFR-11)"
  echo "El detalle, línea por línea, está en $salida/index.html"
  exit 1
fi
