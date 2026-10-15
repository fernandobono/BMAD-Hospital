#!/usr/bin/env bash
# Smoke de la story 1.1 contra el Compose real: E2 (sin .env) y E1 (levanta, cae la base, vuelve,
# recrea web). Éste sí se ejecuta (nadie lo incluye), así que -u es seguro.
# Toda espera es por condición con tope, nunca un sleep fijo (patrón del proyecto guía).
set -euo pipefail

cd "$(dirname "$0")/.."

URL_SALUD="http://localhost:8080/salud"
URL_MAILPIT="http://localhost:8025"

fallar() { echo "FALLÓ: $1" >&2; exit 1; }

codigo() { curl -s -o /dev/null -w "%{http_code}" --max-time 5 "$1" || true; }

# esperar <url> <código> <tope en segundos>: sondea cada 2 s hasta que responda ese código.
esperar() {
  local url="$1" esperado="$2" tope="$3" inicio=$SECONDS
  while (( SECONDS - inicio < tope )); do
    [[ "$(codigo "$url")" == "$esperado" ]] && return 0
    sleep 2
  done
  return 1
}

# --- E2: sin .env (ni las variables en el entorno), Compose se corta con un mensaje claro ---------
salida_e2=$(env -u POSTGRES_PASSWORD -u TURNOS_OWNER_PASSWORD -u TURNOS_APP_PASSWORD \
  docker compose --env-file /dev/null config 2>&1) && fallar "E2: docker compose config no se cortó sin .env"
grep -q "copiá .env.ejemplo a .env" <<<"$salida_e2" || fallar "E2: el error no dice 'copiá .env.ejemplo a .env': $salida_e2"
echo "Smoke_DockerComposeSinEnv_FallaConMensajeClaro OK"

# --- E1: el Compose completo ------------------------------------------------------------------
[[ -f .env ]] || fallar "E1: falta .env (cp .env.ejemplo .env)"

docker compose up -d --build || fallar "E1: docker compose up"
esperar "$URL_SALUD" 200 60 || fallar "E1: /salud no respondió 200 en 60 s"

# AC 7 en el contenedor real: sin la cadena App, el proceso termina (no queda colgado) y la nombra.
# Una cadena vacía Compose la ignora; un espacio cuenta como "falta". Tope de 30 s: si vence, perl
# mata al cliente con SIGALRM (código 142) y eso es una falla, aunque el mensaje ya se haya escrito.
# El contenedor lleva nombre fijo para borrarlo siempre: matar al cliente no lo detiene.
CONTENEDOR_AC7=hospital-turnos-smoke-ac7
trap 'docker rm -f "$CONTENEDOR_AC7" >/dev/null 2>&1 || true' EXIT
rc_ac7=0
salida_ac7=$(perl -e 'alarm 30; exec @ARGV' docker compose run --rm --no-deps --name "$CONTENEDOR_AC7" \
  -e 'ConnectionStrings__App= ' web 2>&1) || rc_ac7=$?
docker rm -f "$CONTENEDOR_AC7" >/dev/null 2>&1 || true
(( rc_ac7 != 0 )) || fallar "E1: la app arrancó sin la cadena App"
(( rc_ac7 != 142 )) || fallar "E1: sin la cadena App el contenedor no terminó en 30 s (quedó colgado)"
grep -q "Falta la cadena de conexión 'App'" <<<"$salida_ac7" || fallar "E1: sin la cadena App el arranque falló por otro motivo: $salida_ac7"
esperar "$URL_MAILPIT" 200 60 || fallar "E1: Mailpit no respondió 200 en 60 s"

docker compose stop db >/dev/null || fallar "E1: docker compose stop db"
esperar "$URL_SALUD" 503 10 || fallar "E1: con la base detenida /salud no dio 503"
cuerpo=$(curl -s --max-time 5 "$URL_SALUD" || true)
[[ "$cuerpo" == '{"estado":"sin conexión con la base"}' ]] || fallar "E1: cuerpo con la base caída: $cuerpo"

docker compose start db >/dev/null || fallar "E1: docker compose start db"
esperar "$URL_SALUD" 200 30 || fallar "E1: /salud no volvió a 200 en 30 s sin reiniciar web"

# M-11: las claves de protección de datos sobreviven a recrear el contenedor web.
claves_antes=$(docker compose exec -T web sh -c 'ls /claves-proteccion' | sort)
[[ -n "$claves_antes" ]] || fallar "E1: /claves-proteccion está vacía"
docker compose up -d --force-recreate web >/dev/null || fallar "E1: recrear web"
esperar "$URL_SALUD" 200 60 || fallar "E1: /salud no respondió 200 después de recrear web"
claves_despues=$(docker compose exec -T web sh -c 'ls /claves-proteccion' | sort)
while read -r archivo; do
  grep -qxF "$archivo" <<<"$claves_despues" || fallar "E1: se perdió la clave $archivo al recrear web"
done <<<"$claves_antes"

echo "Smoke_DockerCompose_LevantaYSaludResponde OK"
