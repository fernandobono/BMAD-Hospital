#!/usr/bin/env python3
"""T1 WorkflowCi_EstructuraYActionlint_SinErrores — story 1.5.

Verifica que .github/workflows/ci.yml tenga la forma que pide la story (AC 1, 2, 3, 4, 8, 9, 10,
12 y 13) y que actionlint no encuentre errores. Se corre desde cualquier carpeta:

    python3 scripts/ci/pruebas/probar-workflow.py

Imprime cada diferencia que encuentra y sale con 1; si no hay ninguna, imprime el OK y sale con 0.
"""
import json
import os
import re
import subprocess
import sys

import yaml

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
NOMBRE = "T1 WorkflowCi_EstructuraYActionlint_SinErrores"

# Tabla del AC 1: id → (nombre visible, needs)
JOBS_ESPERADOS = {
    "compilar": ("0 · Compilar", []),
    "unit": ("1 · Pruebas unitarias", ["compilar"]),
    "integracion": ("2 · Pruebas de integración", ["unit"]),
    "cobertura": ("3 · Cobertura (NFR-11)", ["unit", "integracion"]),
    "sca": ("· Dependencias vulnerables (SCA)", ["compilar"]),
    "imagen": ("· Imagen Docker", ["compilar"]),
}

# Máquina y versiones de las acciones (acción B-3 de la retro del sprint 2): Node.js 24 y Ubuntu
# fijo. Subir de versión es un cambio de esta tabla, no de la lógica de la prueba.
MAQUINA = "ubuntu-24.04"
CHECKOUT = "actions/checkout@v7"
SETUP_DOTNET = "actions/setup-dotnet@v6"
VERSIONES = {
    "actions/upload-artifact": "actions/upload-artifact@v7",
    "actions/download-artifact": "actions/download-artifact@v8",
}

# Artefacto que sube cada job (AC 3, 4 y 5), siempre con if: always().
ARTEFACTOS = {
    "unit": "resultados-unit",
    "integracion": "resultados-integracion",
    "cobertura": "reporte-cobertura",
}

errores = []


def leer(ruta):
    with open(os.path.join(RAIZ, ruta), encoding="utf-8") as f:
        return f.read()


def como_lista(valor):
    if valor is None:
        return []
    return [valor] if isinstance(valor, str) else list(valor)


def revisar_workflow():
    try:
        texto = leer(".github/workflows/ci.yml")
    except FileNotFoundError:
        errores.append("no existe .github/workflows/ci.yml")
        return
    data = yaml.safe_load(texto)
    # PyYAML lee YAML 1.1: la clave `on:` llega como el booleano True.
    disparadores = data.get("on", data.get(True)) or {}
    for evento in ("push", "pull_request", "workflow_dispatch"):
        if evento not in disparadores:
            errores.append(f"falta el disparador `{evento}`")
    push = disparadores.get("push") or {}
    if isinstance(push, dict) and ("branches" in push or "branches-ignore" in push):
        errores.append("`push` filtra ramas: tiene que dispararse en todas")

    if data.get("permissions") != {"contents": "read"}:
        errores.append(f"`permissions` debe ser {{contents: read}}, es {data.get('permissions')!r}")

    jobs = data.get("jobs") or {}
    if list(jobs) != list(JOBS_ESPERADOS):
        errores.append(f"jobs {list(jobs)} != {list(JOBS_ESPERADOS)} (ids y orden de la tabla del AC 1)")

    nombres_trx = []
    for job_id, (nombre, needs) in JOBS_ESPERADOS.items():
        job = jobs.get(job_id)
        if job is None:
            continue
        if job.get("name") != nombre:
            errores.append(f"job `{job_id}`: nombre {job.get('name')!r}, se esperaba {nombre!r}")
        if sorted(como_lista(job.get("needs"))) != sorted(needs):
            errores.append(f"job `{job_id}`: needs {como_lista(job.get('needs'))}, se esperaba {needs}")
        if job.get("runs-on") != MAQUINA:
            errores.append(f"job `{job_id}`: runs-on debe ser {MAQUINA}")

        pasos = job.get("steps") or []
        usos = [p.get("uses", "") for p in pasos]
        if CHECKOUT not in usos:
            errores.append(f"job `{job_id}`: falta {CHECKOUT}")
        for uso in usos:
            base = uso.split("@")[0]
            if base in VERSIONES and uso != VERSIONES[base]:
                errores.append(f"job `{job_id}`: usa {uso}, se esperaba {VERSIONES[base]}")
        setup = [p for p in pasos if p.get("uses", "").startswith("actions/setup-dotnet")]
        if job_id == "imagen":
            if setup:
                errores.append("job `imagen`: no usa setup-dotnet (compila dentro de Docker)")
        elif not setup or setup[0].get("uses") != SETUP_DOTNET or \
                str((setup[0].get("with") or {}).get("dotnet-version")) != "10.0.x":
            errores.append(f"job `{job_id}`: falta {SETUP_DOTNET} con dotnet-version 10.0.x")

        for paso in pasos:
            run = paso.get("run", "")
            if "dotnet test" in run:
                nombres_trx += re.findall(r"LogFileName=([\w.-]+)", run)
                if run.count("dotnet test") != run.count("LogFileName="):
                    errores.append(f"job `{job_id}`: cada `dotnet test` necesita su LogFileName")
                if "XPlat Code Coverage" not in run:
                    errores.append(f"job `{job_id}`: `dotnet test` sin --collect:\"XPlat Code Coverage\"")

        subidas = [p for p in pasos if p.get("uses", "").startswith("actions/upload-artifact")]
        for p in subidas:
            if str(p.get("if", "")).strip() != "always()":
                errores.append(f"job `{job_id}`: la subida de artefacto no tiene if: always()")
        if job_id in ARTEFACTOS:
            subidos = [(p.get("with") or {}).get("name") for p in subidas]
            if ARTEFACTOS[job_id] not in subidos:
                errores.append(f"job `{job_id}`: no sube el artefacto `{ARTEFACTOS[job_id]}`")

    if len(nombres_trx) != len(set(nombres_trx)):
        errores.append(f"LogFileName repetido entre los `dotnet test`: {nombres_trx}")
    if len(nombres_trx) != 3:
        errores.append(f"se esperaban 3 `dotnet test` (Dominio, Aplicacion, Web), hay {len(nombres_trx)}")

    # continue-on-error en cualquier nivel (workflow, job o paso) y --filter en cualquier comando.
    if "continue-on-error" in texto:
        errores.append("ci.yml contiene continue-on-error")
    if "--filter" in texto:
        errores.append("ci.yml contiene --filter (AC 12)")

    for job_id, script in (("cobertura", "scripts/ci/cobertura.sh"), ("sca", "scripts/ci/sca.sh"),
                           ("imagen", "scripts/ci/imagen.sh")):
        runs = " ".join(p.get("run", "") for p in (jobs.get(job_id) or {}).get("steps") or [])
        if script not in runs:
            errores.append(f"job `{job_id}`: no corre {script}")

    # AC 11: local.sh corre los mismos comandos que el flujo, en el mismo orden.
    try:
        local = leer("scripts/ci/local.sh")
    except FileNotFoundError:
        return
    posicion = -1
    for job_id in ("compilar", "unit", "integracion", "cobertura", "sca", "imagen"):
        for paso in (jobs.get(job_id) or {}).get("steps") or []:
            comando = paso.get("run", "").strip()
            if not comando or comando == "dotnet restore" and job_id == "sca":
                continue  # el restore de sca ya lo hizo el paso 0 de local.sh
            encontrado = local.find(comando, posicion + 1)
            if encontrado < 0:
                errores.append(f"local.sh no corre (o no en el mismo orden) el comando del job `{job_id}`: {comando}")
            else:
                posicion = encontrado


def primera_linea_de_codigo(texto):
    for linea in texto.splitlines():
        limpia = linea.strip()
        if limpia and not limpia.startswith("#"):
            return limpia
    return ""


def revisar_scripts():
    for script in ("scripts/ci/sca.sh", "scripts/ci/cobertura.sh", "scripts/ci/imagen.sh",
                   "scripts/ci/local.sh"):
        try:
            texto = leer(script)
        except FileNotFoundError:
            errores.append(f"no existe {script}")
            continue
        if primera_linea_de_codigo(texto) != "set -euo pipefail":
            errores.append(f"{script}: la primera línea de código tiene que ser `set -euo pipefail`")
        if "continue-on-error" in texto or "--filter" in texto:
            errores.append(f"{script}: contiene continue-on-error o --filter")
    try:
        sca = leer("scripts/ci/sca.sh")
        # El texto de `dotnet list` cambia con el idioma del SDK: el veredicto sale del JSON.
        if "--format json" not in sca or "vulnerabilities" not in sca:
            errores.append("sca.sh: tiene que decidir con `--format json` y contar `vulnerabilities` con jq")
    except FileNotFoundError:
        pass


def revisar_herramientas_y_gitignore():
    herramientas = json.loads(leer(".config/dotnet-tools.json")).get("tools", {})
    if "dotnet-ef" not in herramientas:
        errores.append(".config/dotnet-tools.json perdió dotnet-ef")
    rg = herramientas.get("dotnet-reportgenerator-globaltool")
    if not rg or rg.get("version") != "5.5.9":
        errores.append(".config/dotnet-tools.json: falta dotnet-reportgenerator-globaltool 5.5.9")
    lineas = [l.strip() for l in leer(".gitignore").splitlines()]
    for entrada in ("resultados/", "reporte-cobertura/"):
        if entrada not in lineas:
            errores.append(f".gitignore: falta `{entrada}`")


def correr_actionlint():
    if not os.path.exists(os.path.join(RAIZ, ".github/workflows/ci.yml")):
        return
    proc = subprocess.run(
        ["docker", "run", "--rm", "-v", f"{RAIZ}:/repo", "-w", "/repo", "rhysd/actionlint:latest",
         ".github/workflows/ci.yml"],
        capture_output=True, text=True)
    if proc.returncode != 0:
        errores.append(f"actionlint salió con {proc.returncode}:\n{proc.stdout}{proc.stderr}")


revisar_workflow()
revisar_scripts()
revisar_herramientas_y_gitignore()
correr_actionlint()

if errores:
    print(f"{NOMBRE} FALLA")
    for e in errores:
        print(f"  - {e}")
    sys.exit(1)
print(f"{NOMBRE} OK")
