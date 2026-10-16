#!/usr/bin/env bash
# La integración continua en la máquina, sin subir nada (story 1.5, AC 11).
#
#   bash scripts/ci/local.sh
#
# Corre los mismos comandos que .github/workflows/ci.yml, en el mismo orden, y corta en el primer
# fallo: compilar → unitarias → integración → cobertura → SCA → imagen. Necesita Docker (las pruebas
# de integración levantan PostgreSQL con Testcontainers). Tarda unos 5 minutos.
set -euo pipefail

raiz="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$raiz"

paso() {
  echo
  echo "=== $1 ==="
}

rm -rf ./resultados

paso "0 · Compilar"
dotnet restore
dotnet build --no-restore -c Release

paso "1 · Pruebas unitarias"
dotnet test tests/HospitalTurnos.Dominio.Tests -c Release --logger "trx;LogFileName=unit.trx" --collect:"XPlat Code Coverage" --results-directory ./resultados/unit

paso "2 · Pruebas de integración"
dotnet test tests/HospitalTurnos.Aplicacion.Tests -c Release --logger "trx;LogFileName=aplicacion.trx" --collect:"XPlat Code Coverage" --results-directory ./resultados/integracion
dotnet test tests/HospitalTurnos.Web.Tests -c Release --logger "trx;LogFileName=web.trx" --collect:"XPlat Code Coverage" --results-directory ./resultados/integracion

paso "3 · Cobertura (NFR-11)"
bash scripts/ci/cobertura.sh ./resultados ./reporte-cobertura

paso "· Dependencias vulnerables (SCA)"
bash scripts/ci/sca.sh

paso "· Imagen Docker"
bash scripts/ci/imagen.sh

echo
echo "CI local en verde. Reporte de cobertura: reporte-cobertura/index.html"
echo "T3 CiLocal_MismosComandosQueElFlujo_TerminaEnVerde OK"
