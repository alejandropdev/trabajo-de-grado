"""Inventario de un arbol de Assets de Unity, para comprobar que una integracion no pierde nada.

  generar  <carpeta> <manifiesto.csv>
      Escribe una linea por archivo: ruta relativa, SHA-256, tamano y, si es un .meta, su GUID.

  comparar <manifiesto.csv> <carpeta> [--excepciones <lista.txt>] [--informe <salida.txt>]
      Comprueba la carpeta contra el manifiesto y clasifica cada archivo:
        identico      mismo contenido
        cambiado      existe, y su contenido cambio (si es un .meta, se exige el mismo GUID)
        exceptuado    no esta, pero figura en la lista de excepciones (una ruta relativa por linea)
        FALTA         no esta y nadie lo justifico                      -> error
        GUID DISTINTO es un .meta y su GUID cambio                      -> error
      Sale con codigo 1 si hay algun error.

Se ejecuta con `python -I` (no importa nada de la carpeta que examina).
"""
import argparse
import csv
import hashlib
import os
import re
import sys

GUID = re.compile(rb"^guid:\s*([0-9a-f]{32})", re.MULTILINE)


def sha256(ruta):
    h = hashlib.sha256()
    with open(ruta, "rb") as f:
        for bloque in iter(lambda: f.read(1 << 20), b""):
            h.update(bloque)
    return h.hexdigest()


def guid_de(ruta):
    if not ruta.lower().endswith(".meta"):
        return ""
    with open(ruta, "rb") as f:
        m = GUID.search(f.read(4096))
    return m.group(1).decode() if m else ""


def recorrer(raiz):
    for carpeta, _, archivos in os.walk(raiz):
        for nombre in archivos:
            completa = os.path.join(carpeta, nombre)
            yield os.path.relpath(completa, raiz).replace("\\", "/"), completa


def generar(args):
    filas = sorted((rel, sha256(c), os.path.getsize(c), guid_de(c)) for rel, c in recorrer(args.carpeta))
    with open(args.manifiesto, "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["ruta", "sha256", "bytes", "guid"])
        w.writerows(filas)
    total = sum(fila[2] for fila in filas)
    print(f"{len(filas)} archivos, {total / 1048576:.1f} MB -> {args.manifiesto}")
    return 0


def comparar(args):
    excepciones = set()
    if args.excepciones:
        with open(args.excepciones, encoding="utf-8") as f:
            excepciones = {l.strip() for l in f if l.strip() and not l.startswith("#")}

    cuenta = {"identico": 0, "cambiado": 0, "exceptuado": 0, "FALTA": 0, "GUID DISTINTO": 0}
    detalle = []
    with open(args.manifiesto, newline="", encoding="utf-8") as f:
        for fila in csv.DictReader(f):
            rel = fila["ruta"]
            destino = os.path.join(args.carpeta, rel)
            if not os.path.isfile(destino):
                estado = "exceptuado" if rel in excepciones else "FALTA"
            elif fila["guid"] and guid_de(destino) != fila["guid"]:
                estado = "GUID DISTINTO"
            elif sha256(destino) == fila["sha256"]:
                estado = "identico"
            else:
                estado = "cambiado"
            cuenta[estado] += 1
            if estado != "identico":
                detalle.append(f"{estado}\t{rel}")

    resumen = "  ".join(f"{k}: {v}" for k, v in cuenta.items())
    print(resumen)
    if args.informe:
        with open(args.informe, "w", encoding="utf-8") as f:
            f.write(resumen + "\n" + "\n".join(sorted(detalle)) + "\n")
        print("detalle ->", args.informe)
    errores = cuenta["FALTA"] + cuenta["GUID DISTINTO"]
    if errores:
        for linea in sorted(detalle):
            if linea.startswith(("FALTA", "GUID")):
                print(linea)
    return 1 if errores else 0


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = p.add_subparsers(dest="orden", required=True)
    g = sub.add_parser("generar")
    g.add_argument("carpeta")
    g.add_argument("manifiesto")
    g.set_defaults(func=generar)
    c = sub.add_parser("comparar")
    c.add_argument("manifiesto")
    c.add_argument("carpeta")
    c.add_argument("--excepciones")
    c.add_argument("--informe")
    c.set_defaults(func=comparar)
    args = p.parse_args()
    sys.exit(args.func(args))


if __name__ == "__main__":
    main()
