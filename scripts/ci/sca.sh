#!/usr/bin/env bash
# Dependencias con vulnerabilidades conocidas (SCA) — story 1.5, AC 9 y 10.
#
#   bash scripts/ci/sca.sh        (después de un dotnet restore)
#
# Lista los paquetes vulnerables, directos y transitivos, de toda la solución. Si hay alguno, falla.
#
# El veredicto sale del listado en JSON y no del texto: el texto cambia con el idioma del SDK (en
# castellano dice "tiene los paquetes vulnerables siguientes"; buscar una frase fija dejaba pasar
# vulnerables en una máquina en otro idioma). Si el propio `dotnet list` falla (sin red, restore
# roto) el script muestra su salida y sale con ese mismo código: un comando que falló no se lee como
# "no hay vulnerabilidades".
set -euo pipefail

raiz="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$raiz"

listado="$(mktemp)"
trap 'rm -f "$listado"' EXIT

codigo=0
dotnet list package --vulnerable --include-transitive --format json > "$listado" || codigo=$?
if [ "$codigo" -ne 0 ]; then
  cat "$listado"
  echo "::error::No se pudo listar las dependencias (dotnet list salió con $codigo)."
  exit "$codigo"
fi

# Avisos del propio listado (por ejemplo, no se pudo bajar la base de vulnerabilidades): sin esos
# datos no se puede decir que no hay vulnerables.
problemas="$(jq '[.. | objects | .problems? // empty | .[]] | length' "$listado")"
if [ "$problemas" -ne 0 ]; then
  jq -r '.. | objects | .problems? // empty | .[] | "  \(.level // "aviso"): \(.text // .)"' "$listado"
  echo "::error::No se pudo revisar las dependencias: el listado trae avisos."
  exit 1
fi

# Un listado sin proyectos no revisó nada.
proyectos="$(jq '.projects // [] | length' "$listado")"
if [ "$proyectos" -eq 0 ]; then
  echo "::error::No se pudo revisar las dependencias: el listado no trae ningún proyecto."
  exit 1
fi

vulnerables="$(jq '[.projects[].frameworks[]? | (.topLevelPackages // []) + (.transitivePackages // []) | .[] | select(.vulnerabilities)] | length' "$listado")"
if [ "$vulnerables" -ne 0 ]; then
  jq -r '.projects[] | .path as $proyecto | .frameworks[]? | (.topLevelPackages // []) + (.transitivePackages // []) | .[] | select(.vulnerabilities)
         | "  \($proyecto | split("/") | last) · \(.id) \(.resolvedVersion) · \([.vulnerabilities[].severity] | join(", ")) · \([.vulnerabilities[].advisoryurl] | join(" "))"' "$listado"
  echo "::error::Hay dependencias con vulnerabilidades conocidas."
  exit 1
fi
echo "Sin vulnerabilidades conocidas."
