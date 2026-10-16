#!/usr/bin/env bash
# T2 UmbralCobertura_TresReportesDePrueba_PasaSoloSobreOchenta — story 1.5 (AC 5, 6 y 7).
#
# Corre scripts/ci/cobertura.sh (con ReportGenerator de verdad) sobre los reportes de prueba de
# scripts/ci/pruebas/datos/ y mira el código de salida y el mensaje de cada caso:
#   alta (Dominio y Aplicacion 90 %)              → 0 y "— OK"
#   baja (Dominio y Aplicacion 50 % + Web 100 %)  → 1 y "Cobertura de líneas 50" (Web no tiene que inflar el número)
#   sin-lineas (sólo Web)                         → 1 y "No hay cobertura medible"
#   solo-dominio (Dominio 100 %, sin Aplicacion)  → 1 y "Falta la cobertura de HospitalTurnos.Aplicacion"
#   carpeta vacía                                 → 1 y "No hay cobertura medible"
# Funciona con el bash 3.2 de macOS.
set -euo pipefail

raiz="$(cd "$(dirname "$0")/../../.." && pwd)"
datos="$raiz/scripts/ci/pruebas/datos"
nombre="T2 UmbralCobertura_TresReportesDePrueba_PasaSoloSobreOchenta"
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

fallas=0

# caso <nombre> <archivo de datos o vacío> <código esperado> <texto esperado>
caso() {
  local caso_nombre="$1" archivo="$2" esperado="$3" texto="$4"
  local entrada="$tmp/$caso_nombre/entrada" salida="$tmp/$caso_nombre/salida"
  mkdir -p "$entrada"
  if [ -n "$archivo" ]; then
    cp "$datos/$archivo" "$entrada/"
  fi
  local rc=0
  # Sin GITHUB_STEP_SUMMARY: la prueba no escribe en el resumen de una corrida real.
  env -u GITHUB_STEP_SUMMARY bash "$raiz/scripts/ci/cobertura.sh" "$entrada" "$salida" \
    > "$tmp/$caso_nombre.log" 2>&1 || rc=$?
  if [ "$rc" != "$esperado" ]; then
    echo "  - $caso_nombre: salió con $rc, se esperaba $esperado"
    sed 's/^/      /' "$tmp/$caso_nombre.log"
    fallas=$((fallas + 1))
  elif ! grep -qF -- "$texto" "$tmp/$caso_nombre.log"; then
    echo "  - $caso_nombre: la salida no contiene \"$texto\""
    sed 's/^/      /' "$tmp/$caso_nombre.log"
    fallas=$((fallas + 1))
  fi
}

caso alta cobertura-alta.cobertura.xml 0 "— OK"
caso baja cobertura-baja.cobertura.xml 1 "::error::Cobertura de líneas 50"
caso sin-lineas cobertura-sin-lineas.cobertura.xml 1 "::error::No hay cobertura medible de Dominio y Aplicacion"
caso solo-dominio cobertura-solo-dominio.cobertura.xml 1 "::error::Falta la cobertura de HospitalTurnos.Aplicacion"
caso vacia "" 1 "::error::No hay cobertura medible de Dominio y Aplicacion"

if [ "$fallas" -ne 0 ]; then
  echo "$nombre FALLA ($fallas caso(s))"
  exit 1
fi
echo "$nombre OK"
