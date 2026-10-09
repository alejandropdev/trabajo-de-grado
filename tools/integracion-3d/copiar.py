"""Copia el arbol de Assets de un proyecto de Unity dentro de otro sin duplicar ningun GUID.

  copiar.py <Assets de origen> <carpeta de destino> <Assets del proyecto que recibe> <excepciones.txt>

Se copia todo, con sus .meta, salvo los assets cuyo GUID YA existe en el proyecto que recibe (el mismo
asset, importado en los dos proyectos: los «TMP Essentials»). Duplicar un GUID rompe el proyecto, y las
referencias del origen siguen resolviendo, porque apuntan a ese mismo GUID. Lo que no se copia queda escrito
en <excepciones.txt>, que es la lista que despues acepta `inventario.py comparar`.

No sobrescribe: si un archivo ya esta en el destino, se deja como esta (se puede relanzar sin riesgo).
"""
import os
import re
import shutil
import sys

GUID = re.compile(rb"^guid:\s*([0-9a-f]{32})", re.MULTILINE)


def guid_de(ruta):
    with open(ruta, "rb") as f:
        m = GUID.search(f.read(4096))
    return m.group(1) if m else None


def main():
    origen, destino, receptor, salida = sys.argv[1:5]
    destino_abs = os.path.abspath(destino)

    existentes = set()
    for carpeta, _, archivos in os.walk(receptor):
        if os.path.abspath(carpeta).startswith(destino_abs):
            continue
        for nombre in archivos:
            if nombre.endswith(".meta"):
                existentes.add(guid_de(os.path.join(carpeta, nombre)))

    excepciones, copiados, ya_estaban = [], 0, 0
    for carpeta, _, archivos in os.walk(origen):
        for nombre in archivos:
            completa = os.path.join(carpeta, nombre)
            rel = os.path.relpath(completa, origen)
            meta = completa if nombre.endswith(".meta") else completa + ".meta"
            if os.path.isfile(meta) and guid_de(meta) in existentes:
                excepciones.append(rel.replace("\\", "/"))
                continue
            final = os.path.join(destino, rel)
            if os.path.exists(final):
                ya_estaban += 1
                continue
            os.makedirs(os.path.dirname(final), exist_ok=True)
            shutil.copy2(completa, final)
            copiados += 1

    with open(salida, "w", encoding="utf-8") as f:
        f.write("# Assets del origen que NO se copian: su GUID ya existe en el proyecto que recibe.\n")
        f.write("\n".join(sorted(excepciones)) + "\n")
    print(f"copiados: {copiados}   ya estaban: {ya_estaban}   no copiados (GUID repetido): {len(excepciones)} -> {salida}")


if __name__ == "__main__":
    main()
