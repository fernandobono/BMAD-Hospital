#!/usr/bin/env bash
# T4 ImagenDocker_DockerfileRoto_FallaYElDelRepoConstruye — story 1.5 (AC 13).
#
# Corre scripts/ci/imagen.sh dos veces:
#   con scripts/ci/pruebas/datos/Dockerfile.roto → 1, "No se pudo construir la imagen Docker" y el
#                                                 error MSB1009 (proyecto inexistente) en el log
#   con el Dockerfile real de la app             → 0 y "Imagen construida con zona horaria."
# y después confirma que no quedó ningún contenedor creado a partir de la imagen de prueba.
# Necesita Docker. La primera vez tarda 1-3 minutos (descarga las imágenes base).
set -euo pipefail

raiz="$(cd "$(dirname "$0")/../../.." && pwd)"
nombre="T4 ImagenDocker_DockerfileRoto_FallaYElDelRepoConstruye"
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

fallas=0

# caso <nombre> <código esperado> <texto esperado> [Dockerfile]
caso() {
  local caso_nombre="$1" esperado="$2" texto="$3"
  shift 3
  local rc=0
  (cd "$raiz" && bash scripts/ci/imagen.sh "$@") > "$tmp/$caso_nombre.log" 2>&1 || rc=$?
  if [ "$rc" != "$esperado" ]; then
    echo "  - $caso_nombre: salió con $rc, se esperaba $esperado"
    tail -20 "$tmp/$caso_nombre.log" | sed 's/^/      /'
    fallas=$((fallas + 1))
  elif ! grep -qF -- "$texto" "$tmp/$caso_nombre.log"; then
    echo "  - $caso_nombre: la salida no contiene \"$texto\""
    tail -20 "$tmp/$caso_nombre.log" | sed 's/^/      /'
    fallas=$((fallas + 1))
  fi
}

caso roto 1 "::error::No se pudo construir la imagen Docker" scripts/ci/pruebas/datos/Dockerfile.roto
# Que falle por el motivo esperado: MSBuild no encuentra el proyecto (MSB1009). Buscar sólo el nombre
# del archivo no alcanza, porque aparece también en el eco del RUN.
if ! grep -qF "MSB1009" "$tmp/roto.log"; then
  echo "  - roto: el log no trae el error MSB1009 del proyecto inexistente (falló por otro motivo)"
  fallas=$((fallas + 1))
fi
caso real 0 "Imagen construida con zona horaria."

restos="$(docker ps -a --filter ancestor=hospital-turnos-web:ci --format '{{.ID}}')"
if [ -n "$restos" ]; then
  echo "  - quedaron contenedores temporales del script: $restos"
  fallas=$((fallas + 1))
fi

if [ "$fallas" -ne 0 ]; then
  echo "$nombre FALLA ($fallas caso(s))"
  exit 1
fi
echo "$nombre OK"
