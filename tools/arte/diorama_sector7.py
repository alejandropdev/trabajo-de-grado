"""
Diorama del Sector 7 (exploracion de direccion de arte, ronda 2, 2026-11-15).

Construye por codigo una sala de turno retrofuturista (costillas en arco que se pierden en la niebla, la puerta
redonda de la boveda al fondo con su anillo de neon, tuberias, cables colgantes, una consola aerodinamica con su
pantalla encendida y una lampara de cupula) y la renderiza con sombreado de dos bandas, niebla volumetrica y
contraluz. Sin texturas ni modelos de terceros: solo primitivas y curvas.

Uso (sin abrir Blender):
  "C:/Program Files/Blender Foundation/Blender 5.1/blender.exe" -b --factory-startup -P tools/arte/diorama_sector7.py -- <salida.png>
"""
import math
import sys

import bpy

SALIDA = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else "//diorama.png"


def hex_rgb(h, a=1.0):
    h = h.lstrip("#")
    c = [int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)]
    c = [x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c]   # a lineal
    return (c[0], c[1], c[2], a)


def motor(escena):
    for nombre in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT"):
        try:
            escena.render.engine = nombre
            return nombre
        except TypeError:
            continue
    escena.render.engine = "CYCLES"
    return "CYCLES"


def toon(nombre, base, sombra, luz_borde=None):
    """Material de dos bandas: la luz difusa se parte en sombra/luz con un corte duro (Shader to RGB, solo Eevee)."""
    m = bpy.data.materials.new(nombre)
    m.use_nodes = True
    n = m.node_tree.nodes
    l = m.node_tree.links
    n.clear()
    sal = n.new("ShaderNodeOutputMaterial")
    dif = n.new("ShaderNodeBsdfDiffuse")
    a_rgb = n.new("ShaderNodeShaderToRGB")
    rampa = n.new("ShaderNodeValToRGB")
    rampa.color_ramp.interpolation = "CONSTANT"
    rampa.color_ramp.elements[0].position = 0.0
    rampa.color_ramp.elements[0].color = hex_rgb(sombra)
    rampa.color_ramp.elements[1].position = 0.18
    rampa.color_ramp.elements[1].color = hex_rgb(base)
    em = n.new("ShaderNodeEmission")
    l.new(dif.outputs[0], a_rgb.inputs[0])
    l.new(a_rgb.outputs[0], rampa.inputs[0])
    if luz_borde:
        # Borde de luz: un fresnel duro suma el color del contraluz en los cantos.
        fres = n.new("ShaderNodeLayerWeight")
        fres.inputs[0].default_value = 0.35
        corte = n.new("ShaderNodeMath")
        corte.operation = "GREATER_THAN"
        corte.inputs[1].default_value = 0.72
        mezcla = n.new("ShaderNodeMix")
        mezcla.data_type = "RGBA"
        mezcla.inputs["B"].default_value = hex_rgb(luz_borde)
        l.new(fres.outputs["Facing"], corte.inputs[0])
        l.new(corte.outputs[0], mezcla.inputs["Factor"])
        l.new(rampa.outputs[0], mezcla.inputs["A"])
        l.new(mezcla.outputs["Result"], em.inputs[0])
    else:
        l.new(rampa.outputs[0], em.inputs[0])
    l.new(em.outputs[0], sal.inputs[0])
    return m


def emisivo(nombre, color, fuerza):
    m = bpy.data.materials.new(nombre)
    m.use_nodes = True
    n = m.node_tree.nodes
    n.clear()
    sal = n.new("ShaderNodeOutputMaterial")
    em = n.new("ShaderNodeEmission")
    em.inputs[0].default_value = hex_rgb(color)
    em.inputs[1].default_value = fuerza
    m.node_tree.links.new(em.outputs[0], sal.inputs[0])
    return m


def poner(obj, mat):
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    for p in getattr(obj.data, "polygons", []):
        p.use_smooth = True
    return obj


def biselar(obj, ancho, segmentos=4):
    b = obj.modifiers.new("Bisel", "BEVEL")
    b.width = ancho
    b.segments = segmentos
    return obj


def curva(nombre, puntos, grosor, mat):
    datos = bpy.data.curves.new(nombre, "CURVE")
    datos.dimensions = "3D"
    datos.bevel_depth = grosor
    datos.bevel_resolution = 4
    sp = datos.splines.new("BEZIER")
    sp.bezier_points.add(len(puntos) - 1)
    for bp, p in zip(sp.bezier_points, puntos):
        bp.co = p
        bp.handle_left_type = bp.handle_right_type = "AUTO"
    obj = bpy.data.objects.new(nombre, datos)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    return obj


def medio_arco(nombre, radio, grueso, y, z0, mat):
    """Arco de 180° (de suelo a suelo pasando por arriba) como curva con grosor: las costillas de la sala."""
    puntos = []
    for i in range(25):
        a = math.pi * i / 24
        puntos.append((math.cos(a) * radio, y, z0 + math.sin(a) * radio * 1.25))
    datos = bpy.data.curves.new(nombre, "CURVE")
    datos.dimensions = "3D"
    datos.bevel_depth = grueso
    datos.bevel_resolution = 6
    sp = datos.splines.new("POLY")
    sp.points.add(len(puntos) - 1)
    for p, c in zip(sp.points, puntos):
        p.co = (c[0], c[1], c[2], 1)
    obj = bpy.data.objects.new(nombre, datos)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    return obj


def construir():
    escena = bpy.context.scene
    usado = motor(escena)
    escena.render.resolution_x = 1600
    escena.render.resolution_y = 900
    vistas = [i.identifier for i in escena.view_settings.bl_rna.properties["view_transform"].enum_items]
    escena.view_settings.view_transform = "AgX" if "AgX" in vistas else "Filmic"
    if usado.startswith("BLENDER_EEVEE"):
        ee = escena.eevee
        for attr, val in (("taa_render_samples", 64), ("volumetric_tile_size", "4"), ("volumetric_samples", 96),
                          ("use_volumetric_shadows", True), ("volumetric_end", 70.0)):
            try:
                setattr(ee, attr, val)
            except (AttributeError, TypeError):
                pass
    else:
        escena.cycles.samples = 96

    # Mundo: azul de bunker y una niebla volumetrica verdeazulada.
    mundo = bpy.data.worlds.new("Mundo")
    escena.world = mundo
    mundo.use_nodes = True
    wn = mundo.node_tree.nodes
    wn.clear()
    wsal = wn.new("ShaderNodeOutputWorld")
    fondo = wn.new("ShaderNodeBackground")
    fondo.inputs[0].default_value = hex_rgb("#06121A")
    vol = wn.new("ShaderNodeVolumePrincipled")
    vol.inputs["Color"].default_value = hex_rgb("#4FA8E0")
    vol.inputs["Density"].default_value = 0.03
    vol.inputs["Anisotropy"].default_value = 0.6
    mundo.node_tree.links.new(fondo.outputs[0], wsal.inputs["Surface"])
    mundo.node_tree.links.new(vol.outputs[0], wsal.inputs["Volume"])

    acero = toon("Acero", "#2E4A52", "#12232A", luz_borde="#8FEAFF")
    acero_osc = toon("AceroOscuro", "#16262D", "#0A1418")
    suelo_m = toon("Suelo", "#1B2A31", "#0C1519")
    laton = toon("Laton", "#B5843A", "#5A3B16", luz_borde="#F2C14E")
    crema = toon("Crema", "#DDE6E8", "#6E8088", luz_borde="#FFFFFF")
    mostaza = toon("Mostaza", "#C9A227", "#6B5512")
    neon = emisivo("Neon", "#5FD8F5", 18.0)
    pantalla = emisivo("Pantalla", "#8FEAFF", 6.0)
    ambar = emisivo("Ambar", "#F2C14E", 14.0)
    cable_m = toon("Cable", "#0B1114", "#05080A")

    # Suelo y techo (el techo cierra la sala: sin cielo dentro del bunker).
    bpy.ops.mesh.primitive_plane_add(size=90, location=(0, -20, 0))
    poner(bpy.context.object, suelo_m)
    bpy.ops.mesh.primitive_plane_add(size=90, location=(0, -20, 6.6))
    poner(bpy.context.object, acero_osc)

    # Costillas en arco aerodinamico, cada 3,2 m, perdiendose en la niebla; una de cada tres con neon por dentro.
    for i in range(10):
        y = -2.0 - i * 3.2
        medio_arco("Costilla%d" % i, 4.6, 0.3, y, 0.0, acero)
        if i % 3 == 1:
            medio_arco("NeonCostilla%d" % i, 4.2, 0.035, y + 0.12, 0.0, neon)

    # La puerta de la boveda al fondo: disco, anillo de neon y cubo con radios.
    fondo_y = -33.0
    bpy.ops.mesh.primitive_cylinder_add(radius=3.4, depth=0.6, vertices=96, location=(0, fondo_y, 3.5), rotation=(math.radians(90), 0, 0))
    biselar(poner(bpy.context.object, acero), 0.12)
    bpy.ops.mesh.primitive_torus_add(major_radius=3.55, minor_radius=0.07, major_segments=128, location=(0, fondo_y + 0.35, 3.5), rotation=(math.radians(90), 0, 0))
    poner(bpy.context.object, neon)
    bpy.ops.mesh.primitive_cylinder_add(radius=0.9, depth=0.4, vertices=48, location=(0, fondo_y + 0.4, 3.5), rotation=(math.radians(90), 0, 0))
    biselar(poner(bpy.context.object, laton), 0.08)
    for k in range(8):
        a = k * math.pi / 4
        x1, z1 = math.cos(a) * 0.9, 3.5 + math.sin(a) * 0.9
        x2, z2 = math.cos(a) * 3.0, 3.5 + math.sin(a) * 3.0
        curva("Radio%d" % k, [(x1, fondo_y + 0.45, z1), (x2, fondo_y + 0.45, z2)], 0.08, laton)
    # Contraluz detras de la puerta: la luz que se cuela por el borde y dibuja rayos en la niebla.
    bpy.ops.object.light_add(type="SPOT", location=(0, fondo_y - 2.0, 3.6))
    tras = bpy.context.object
    tras.data.energy = 60000
    tras.data.spot_size = math.radians(100)
    tras.data.color = hex_rgb("#8FEAFF")[:3]
    tras.rotation_euler = (math.radians(90), 0, 0)   # el foco mira por -Z local: girado 90° en X mira a +Y (a la camara)

    # Tuberias de laton a lo largo de la pared izquierda y cables colgando entre costillas.
    pass  # curva("Tuberia1", [(-3.9, -5, 0.6), (-4.1, -14, 0.8), (-3.9, -24, 0.7), (-3.5, -33, 0.6)], 0.17, laton)
    pass  # curva("Tuberia2", [(-3.6, -5, 1.25), (-3.9, -16, 1.5), (-3.5, -33, 1.3)], 0.09, laton)
    for i in range(7):
        y0 = -2.0 - i * 3.2
        y1 = y0 - 3.2
        x = 1.7 if i % 2 else -1.1
        curva("Cable%d" % i, [(x, y0, 5.6), (x + 0.15, (y0 + y1) / 2, 4.5 - 0.25 * (i % 3)), (x, y1, 5.6)], 0.035, cable_m)

    # Consola aerodinamica en primer plano, a la derecha: cuerpo redondeado, pantalla redonda encendida, diales.
    bpy.ops.mesh.primitive_cube_add(size=1, location=(2.4, -2.6, 0.75))
    cuerpo = bpy.context.object
    cuerpo.scale = (1.3, 0.85, 1.5)
    biselar(poner(cuerpo, crema), 0.32, 8)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(2.4, -2.8, 1.85))
    cabeza = bpy.context.object
    cabeza.scale = (1.1, 0.6, 0.7)
    cabeza.rotation_euler = (math.radians(-16), 0, 0)
    biselar(poner(cabeza, crema), 0.28, 8)
    bpy.ops.mesh.primitive_cylinder_add(radius=0.3, depth=0.05, vertices=48, location=(2.4, -2.47, 1.95), rotation=(math.radians(74), 0, 0))
    poner(bpy.context.object, pantalla)
    for k, dx in enumerate((-0.42, 0.0, 0.42)):
        bpy.ops.mesh.primitive_cylinder_add(radius=0.1, depth=0.08, location=(2.4 + dx, -2.15, 1.15), rotation=(math.radians(90), 0, 0))
        poner(bpy.context.object, mostaza if k == 1 else laton)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(2.4, -2.16, 0.16))
    franja = bpy.context.object
    franja.scale = (1.25, 0.02, 0.12)
    poner(franja, mostaza)

    # Lampara de cupula colgando: luz calida que separa el primer plano del fondo frio.
    curva("Cordon", [(1.4, -3.6, 6.6), (1.4, -3.6, 4.0)], 0.02, cable_m)
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.55, location=(1.4, -3.6, 3.8))
    cup = bpy.context.object
    cup.scale = (1, 1, 0.5)
    poner(cup, laton)
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.17, location=(1.4, -3.6, 3.56))
    poner(bpy.context.object, ambar)
    bpy.ops.object.light_add(type="POINT", location=(1.4, -3.6, 3.3))
    calida = bpy.context.object
    calida.data.energy = 350
    calida.data.color = hex_rgb("#F2A24E")[:3]
    calida.data.shadow_soft_size = 0.2

    # Relleno frio muy bajo.
    bpy.ops.object.light_add(type="SUN", location=(0, 0, 10))
    sol = bpy.context.object
    sol.data.energy = 0.4
    sol.data.color = hex_rgb("#5FD8F5")[:3]
    sol.rotation_euler = (math.radians(35), 0, math.radians(20))

    # Camara a la altura de los ojos, mirando al fondo de la sala.
    bpy.ops.object.camera_add(location=(-0.6, 5.0, 1.6))
    cam = bpy.context.object
    cam.rotation_euler = (math.radians(87), 0, math.radians(183))   # mira a -Y: hacia el fondo de la sala
    cam.data.lens = 22
    escena.camera = cam

    escena.render.filepath = SALIDA
    print("[diorama] motor:", usado)
    bpy.ops.render.render(write_still=True)
    print("[diorama] listo:", SALIDA)


bpy.ops.wm.read_factory_settings(use_empty=True)
construir()
