"""Reduce los modelos pesados que usa la escena del recorrido 3D. Se ejecuta DENTRO de Blender:

  blender -b --factory-startup -P tools/integracion-3d/reducir_modelos.py -- [--solo <texto>] [--forzar]

Para cada FBX que la escena instancia y que pesa demasiado, escribe un modelo nuevo
`Assets/Mundo3D/Optimizado/Modelos/<nombre>_opt.fbx`: todas sus piezas unidas en UNA malla (los materiales
quedan como sub-mallas, las UV se conservan) y reducida hasta su presupuesto de triangulos. Los FBX
originales no se tocan. Deja la tabla de antes y despues en `tools/integracion-3d/informe-reduccion.csv`,
que es tambien de donde Unity saca que modelo sustituye a cual.

  --solo <texto>   solo los modelos cuya ruta contiene ese texto (para probar uno)
  --forzar         rehace los que ya existen

Los presupuestos estan en PRESUPUESTOS y en EXCEPCIONES, aqui abajo: subir uno y relanzar con --forzar --solo.
"""
import csv
import os
import re
import sys
import time

import bpy

RAIZ = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
PROYECTO = os.path.join(RAIZ, "game", "proyecto de grado")
ESCENA = os.path.join(PROYECTO, "Assets", "Mundo3D", "Externo", "malfarie", "Levels", "Level1.unity")
ORIGEN = os.path.join(PROYECTO, "Assets", "Mundo3D", "Externo")
DESTINO = os.path.join(PROYECTO, "Assets", "Mundo3D", "Optimizado", "Modelos")
INFORME = os.path.join(RAIZ, "tools", "integracion-3d", "informe-reduccion.csv")

# Perfil «equilibrado»: triangulos que le quedan a cada modelo, segun lo que es.
PRESUPUESTOS = {
    "grande": 25000,     # un mueble de mas de 400.000 triangulos de origen
    "normal": 12000,     # un mueble o un objeto
    "repetido": 5000,    # esta mas de 5 veces en la escena
    "pequeno": 2000,     # mide menos de medio metro
}
# Por debajo de esto no merece la pena tocarlo.
MINIMO_DE_TRIANGULOS = 8000
MINIMO_DE_PIEZAS = 3
# Un FBX del que tiran decenas de prefabs no es un mueble: es el almacen de piezas de un pack, y ya son ligeras.
USOS_DE_UN_KIT = 40
# Lo que no se reduce nunca (el nivel: su forma ES la colision) y los presupuestos puestos a mano.
NO_TOCAR = ("Mapa3D",)
EXCEPCIONES = {
    # La ropa y los maniquies son superficies finas y organicas: por debajo de esto se les abren huecos.
    "Maniqui1.fbx": 70000,
    "Maniqui2.fbx": 50000,
    "Maniqui3.fbx": 90000,
    "Maniqui4.fbx": 70000,
    "Maniqui6.fbx": 70000,
    "RackRopa.fbx": 90000,
    "ArmarioRopa.fbx": 45000,
    "PilaRopa.fbx": 8000,
    "Springtrap.fbx": 30000,
}


def instancias_por_fbx():
    """Cuantas veces instancia la escena cada FBX, directamente o dentro de un prefab: {ruta: veces}."""
    por_guid = {}
    for carpeta, _, archivos in os.walk(ORIGEN):
        for nombre in archivos:
            if not nombre.lower().endswith((".fbx.meta", ".prefab.meta")):
                continue
            with open(os.path.join(carpeta, nombre), encoding="utf-8", errors="replace") as f:
                m = re.search(r"guid: ([0-9a-f]{32})", f.read(300))
            if m:
                por_guid[m.group(1)] = os.path.join(carpeta, nombre[:-5])

    # Una instancia de un modelo o de un prefab, o una malla suelta de un FBX (asi los usan los prefabs de los packs).
    referencia = re.compile(r"(?:m_SourcePrefab|m_Mesh): \{fileID: -?\d+, guid: ([0-9a-f]{32})")
    veces = {}

    def contar(archivo, multiplicador, fondo):
        with open(archivo, encoding="utf-8", errors="replace") as f:
            texto = f.read()
        instancias = re.findall(r"m_SourcePrefab: \{fileID: \d+, guid: ([0-9a-f]{32})", texto)
        # Un objeto que usa las mallas de un FBX sin instanciarlo cuenta una vez por archivo, tenga las piezas que tenga.
        sueltas = set(referencia.findall(texto)) - set(instancias)
        for guid in instancias + sorted(sueltas):
            ruta = por_guid.get(guid)
            if ruta is None:
                continue
            if ruta.lower().endswith(".fbx"):
                veces[ruta] = veces.get(ruta, 0) + multiplicador
            elif fondo < 6:
                contar(ruta, multiplicador, fondo + 1)

    contar(ESCENA, 1, 0)
    return veces


def triangulos(objeto):
    return sum(len(p.vertices) - 2 for p in objeto.data.polygons)


def vaciar():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def importar(ruta):
    """
    El importador de FBX de Blender 5.1 falla al leer una luz (escribe un ajuste de Cycles que ya no existe), y
    varios de estos modelos traen la luz de la escena en la que se hicieron. Las luces no interesan: se leen
    como una luz vacia.
    """
    from io_scene_fbx import import_fbx
    if not getattr(import_fbx, "_luces_ignoradas", False):
        import_fbx.blen_read_light = lambda fbx_tmpl, fbx_obj, settings: bpy.data.lights.new("Luz", "POINT")
        import_fbx._luces_ignoradas = True
    bpy.ops.import_scene.fbx(filepath=ruta)


def reducir(ruta, veces, salida):
    """Devuelve (estado, piezas, triangulos antes, triangulos despues, presupuesto, materiales)."""
    vaciar()
    importar(ruta)
    objetos = list(bpy.context.scene.objects)
    if any(o.type == "ARMATURE" for o in objetos):
        return "omitido: tiene esqueleto", 0, 0, 0, 0, 0
    mallas = [o for o in objetos if o.type == "MESH" and len(o.data.polygons) > 0]
    if not mallas:
        return "omitido: sin mallas", 0, 0, 0, 0, 0
    antes = sum(triangulos(o) for o in mallas)
    if antes < MINIMO_DE_TRIANGULOS and len(mallas) <= MINIMO_DE_PIEZAS:
        return "omitido: ya es ligero", len(mallas), antes, antes, 0, 0
    if veces > USOS_DE_UN_KIT:
        return "omitido: es un kit de piezas ligeras", len(mallas), antes, antes, 0, 0

    # Una sola malla, con las transformaciones ya aplicadas: lo que se exporta no depende de la jerarquia.
    bpy.ops.object.select_all(action="DESELECT")
    for o in mallas:
        o.select_set(True)
    bpy.context.view_layer.objects.active = mallas[0]
    bpy.ops.object.make_single_user(object=True, obdata=True)
    bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
    if len(mallas) > 1:
        bpy.ops.object.join()
    unida = bpy.context.view_layer.objects.active
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for o in list(bpy.context.scene.objects):
        if o != unida:
            bpy.data.objects.remove(o, do_unlink=True)

    nombre = os.path.basename(ruta)
    if nombre in EXCEPCIONES:
        presupuesto = EXCEPCIONES[nombre]
    elif max(unida.dimensions) < 0.5 and antes < 40000:
        # Solo si ademas es ligero: hay packs que guardan un mueble entero a escala de milimetros.
        presupuesto = PRESUPUESTOS["pequeno"]
    elif veces > 5:
        presupuesto = PRESUPUESTOS["repetido"]
    elif antes > 400000:
        presupuesto = PRESUPUESTOS["grande"]
    else:
        presupuesto = PRESUPUESTOS["normal"]

    if antes > presupuesto:
        modificador = unida.modifiers.new("Reducir", "DECIMATE")
        modificador.decimate_type = "COLLAPSE"
        modificador.ratio = presupuesto / antes
        modificador.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=modificador.name)
    despues = triangulos(unida)

    # Un modelo hecho de cientos de trocitos sueltos no baja: reducir no cruza los bordes de cada trozo. Se sueldan
    # los vertices que coinciden (los trozos pasan a ser una superficie) y se reduce otra vez.
    if despues > presupuesto * 1.3:
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        # Solo los que de verdad coinciden: con mas margen, un modelo de piezas pequeñas se funde en una mancha.
        bpy.ops.mesh.remove_doubles(threshold=max(unida.dimensions) * 0.00002)
        bpy.ops.object.mode_set(mode="OBJECT")
        soldada = triangulos(unida)
        if soldada > presupuesto:
            modificador = unida.modifiers.new("Reducir otra vez", "DECIMATE")
            modificador.decimate_type = "COLLAPSE"
            modificador.ratio = presupuesto / soldada
            modificador.use_collapse_triangulate = True
            bpy.ops.object.modifier_apply(modifier=modificador.name)
        despues = triangulos(unida)

    unida.name = os.path.splitext(nombre)[0] + "_opt"
    unida.data.name = unida.name
    bpy.ops.object.select_all(action="DESELECT")
    unida.select_set(True)
    os.makedirs(os.path.dirname(salida), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=salida, use_selection=True, object_types={"MESH"},
        apply_scale_options="FBX_SCALE_ALL", bake_space_transform=True,
        axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE",
        add_leaf_bones=False, bake_anim=False, path_mode="STRIP", embed_textures=False)
    return "reducido", len(mallas), antes, despues, presupuesto, len(unida.data.materials)


def reducir_por_piezas(ruta, proporcion, salida):
    """
    El mismo modelo, pero SIN unir: cada pieza sigue siendo un objeto con su nombre y su sitio, y solo pierde
    triangulos. Es lo que se usa donde el nivel escondio o movio piezas del modelo: alli no vale una malla unida,
    pero si cambiar la malla de cada pieza por su version reducida.
    """
    vaciar()
    importar(ruta)
    for o in [o for o in bpy.context.scene.objects if o.type not in ("MESH", "EMPTY")]:
        bpy.data.objects.remove(o, do_unlink=True)
    mallas = [o for o in bpy.context.scene.objects if o.type == "MESH" and len(o.data.polygons) > 0]
    bpy.ops.object.select_all(action="DESELECT")
    for o in mallas:
        o.select_set(True)
    bpy.context.view_layer.objects.active = mallas[0]
    bpy.ops.object.make_single_user(object=True, obdata=True)
    for o in mallas:
        antes = triangulos(o)
        if antes < 300:
            continue   # una pieza asi de pequeña ya no tiene de donde quitar
        bpy.context.view_layer.objects.active = o
        modificador = o.modifiers.new("Reducir", "DECIMATE")
        modificador.decimate_type = "COLLAPSE"
        modificador.ratio = max(proporcion, 200.0 / antes)
        modificador.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=modificador.name)
    bpy.ops.export_scene.fbx(
        filepath=salida, use_selection=False, object_types={"MESH", "EMPTY"},
        apply_scale_options="FBX_SCALE_UNITS", bake_space_transform=False,
        axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE",
        add_leaf_bones=False, bake_anim=False, path_mode="STRIP", embed_textures=False)
    return sum(triangulos(o) for o in mallas)


def main():
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    solo = args[args.index("--solo") + 1] if "--solo" in args else None
    forzar = "--forzar" in args

    previas = {}
    if os.path.isfile(INFORME):
        with open(INFORME, newline="", encoding="utf-8") as f:
            previas = {fila["modelo"]: fila for fila in csv.DictReader(f)}

    filas = dict(previas)
    for ruta, veces in sorted(instancias_por_fbx().items()):
        rel = os.path.relpath(ruta, ORIGEN).replace("\\", "/")
        if solo and solo.lower() not in rel.lower():
            continue
        if any(t in rel for t in NO_TOCAR):
            filas[rel] = {"modelo": rel, "veces": veces, "estado": "omitido: es el nivel", "optimizado": ""}
            continue
        nombre = os.path.splitext(os.path.basename(ruta))[0] + "_opt.fbx"
        salida = os.path.join(DESTINO, nombre)
        if not forzar and rel in previas and (previas[rel]["estado"] != "reducido" or os.path.isfile(salida)):
            continue
        t = time.time()
        por_piezas = ""
        try:
            estado, piezas, antes, despues, presupuesto, materiales = reducir(ruta, veces, salida)
            if estado == "reducido":
                reducir_por_piezas(ruta, min(1.0, presupuesto / antes), salida[:-8] + "_partes.fbx")
                por_piezas = "Assets/Mundo3D/Optimizado/Modelos/" + nombre[:-8] + "_partes.fbx"
        except Exception as e:   # un modelo que Blender no sabe leer no para a los demas
            estado, piezas, antes, despues, presupuesto, materiales = "ERROR: " + str(e)[:120], 0, 0, 0, 0, 0
        filas[rel] = {
            "modelo": rel, "veces": veces, "estado": estado, "piezas": piezas, "triangulos_antes": antes,
            "triangulos_despues": despues, "presupuesto": presupuesto, "materiales": materiales,
            "optimizado": "Assets/Mundo3D/Optimizado/Modelos/" + nombre if estado == "reducido" else "",
            "por_piezas": por_piezas,
        }
        print("[reducir] %-52s %-22s %8d -> %6d  (%d piezas, x%d, %.0fs)" %
              (rel[-52:], estado[:22], antes, despues, piezas, veces, time.time() - t), flush=True)

    campos = ["modelo", "veces", "estado", "piezas", "triangulos_antes", "triangulos_despues", "presupuesto",
              "materiales", "optimizado", "por_piezas"]
    with open(INFORME, "w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=campos, extrasaction="ignore")
        w.writeheader()
        for rel in sorted(filas):
            w.writerow(filas[rel])

    hechas = [f for f in filas.values() if f.get("estado") == "reducido"]
    antes = sum(int(f["triangulos_antes"]) * int(f["veces"]) for f in hechas)
    despues = sum(int(f["triangulos_despues"]) * int(f["veces"]) for f in hechas)
    print("[reducir] %d modelos reducidos. En la escena: %d -> %d triangulos. Tabla: %s" %
          (len(hechas), antes, despues, INFORME), flush=True)


main()
