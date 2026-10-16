#!/usr/bin/env bash
# Imagen Docker de la aplicación — story 1.5, AC 13.
#
#   bash scripts/ci/imagen.sh [Dockerfile]     (por defecto src/HospitalTurnos.Web/Dockerfile)
#
# Construye la imagen desde la raíz del repo y verifica que trae la zona horaria de Córdoba, que
# usa el reloj del hospital (story 1.2). El archivo se copia con docker create + docker cp -L, que
# funciona aunque la imagen no tenga shell. El contenedor temporal se borra siempre.
set -euo pipefail

raiz="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$raiz"

dockerfile="${1:-src/HospitalTurnos.Web/Dockerfile}"
imagen="hospital-turnos-web:ci"
zona="/usr/share/zoneinfo/America/Argentina/Cordoba"

if ! docker build -f "$dockerfile" -t "$imagen" .; then
  echo "::error::No se pudo construir la imagen Docker"
  exit 1
fi

id="$(docker create "$imagen")"
trap 'docker rm -f "$id" > /dev/null' EXIT

# -L sigue enlaces: un enlace a un archivo inexistente no cuenta como zona horaria.
if ! docker cp -L "$id:$zona" - > /dev/null; then
  echo "::error::La imagen no trae la zona horaria America/Argentina/Cordoba"
  exit 1
fi
echo "Imagen construida con zona horaria."
