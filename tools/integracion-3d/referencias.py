"""Referencias rotas entre assets de Unity: GUID que un archivo usa y que ningun .meta define.

  referencias.py <carpeta a revisar> <salida.txt> [--metas <carpeta> ...] [--base <referencias-origen.txt>]

  <carpeta a revisar>  donde estan las escenas, prefabs y materiales que se examinan.
  --metas              carpetas adicionales cuyos .meta tambien cuentan como destino valido
                       (el resto de Assets, Library/PackageCache, Packages).
  --base               un informe anterior (el del proyecto de origen). Con el, solo son error las
                       referencias rotas que NO estaban ya rotas alli.

Cada linea de la salida es  `<guid>\t<archivo que lo usa>`, con la ruta relativa a la carpeta revisada.
Sale con codigo 1 si hay referencias rotas nuevas.
"""
import argparse
import os
import re
import sys

TEXTO = {".unity", ".prefab", ".mat", ".asset", ".controller", ".anim", ".overridecontroller",
         ".physicmaterial", ".lighting", ".playable", ".mask", ".preset", ".spriteatlas"}
REFERENCIA = re.compile(rb"guid:\s*([0-9a-f]{32})")
GUID_META = re.compile(rb"^guid:\s*([0-9a-f]{32})", re.MULTILINE)
# Recursos internos del editor y del motor: no tienen .meta.
INTERNOS = {b"0000000000000000e000000000000000", b"0000000000000000f000000000000000",
            b"0000000000000000d000000000000000", b"00000000000000000000000000000000"}


def guids_definidos(carpetas):
    definidos = set()
    for raiz in carpetas:
        for carpeta, _, archivos in os.walk(raiz):
            for nombre in archivos:
                if not nombre.endswith(".meta"):
                    continue
                with open(os.path.join(carpeta, nombre), "rb") as f:
                    m = GUID_META.search(f.read(4096))
                if m:
                    definidos.add(m.group(1))
    return definidos


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("carpeta")
    p.add_argument("salida")
    p.add_argument("--metas", nargs="*", default=[])
    p.add_argument("--base")
    args = p.parse_args()

    definidos = guids_definidos([args.carpeta] + args.metas) | INTERNOS
    rotas = set()
    revisados = 0
    for carpeta, _, archivos in os.walk(args.carpeta):
        for nombre in archivos:
            if os.path.splitext(nombre)[1].lower() not in TEXTO:
                continue
            completa = os.path.join(carpeta, nombre)
            with open(completa, "rb") as f:
                datos = f.read()
            if not datos.startswith(b"%YAML"):
                continue   # un .asset binario (iluminacion horneada, por ejemplo)
            revisados += 1
            rel = os.path.relpath(completa, args.carpeta).replace("\\", "/")
            for guid in set(REFERENCIA.findall(datos)) - definidos:
                rotas.add((guid.decode(), rel))

    with open(args.salida, "w", encoding="utf-8") as f:
        for guid, rel in sorted(rotas, key=lambda r: (r[1], r[0])):
            f.write(f"{guid}\t{rel}\n")

    print(f"{revisados} archivos revisados, {len(definidos)} GUID conocidos, {len(rotas)} referencias rotas -> {args.salida}")
    if not args.base:
        return 0
    with open(args.base, encoding="utf-8") as f:
        previas = {tuple(l.rstrip("\n").split("\t")) for l in f if l.strip()}
    nuevas = sorted(rotas - previas, key=lambda r: (r[1], r[0]))
    print(f"{len(nuevas)} no estaban en la base ({args.base})")
    for guid, rel in nuevas[:60]:
        print(f"  NUEVA  {guid}  {rel}")
    return 1 if nuevas else 0


if __name__ == "__main__":
    sys.exit(main())
