"""Deja los retratos de personaje listos para el juego (768x768, RGBA, fondo transparente de verdad).

    python tools/retratos/preparar_retratos.py                # los de abajo, a Assets/Resources/Ilustraciones/personajes
    python tools/retratos/preparar_retratos.py --salida DIR   # a otra carpeta (para revisarlos antes)

Los generadores de imagen entregan los retratos con defectos que en el juego se ven:
  * Kenji: el "tablero de transparencia" viene dibujado en los pixeles (PNG sin canal alfa).
  * Todos: alfa fantasma (1..8 en el fondo, 253/254 en el cuerpo) que pinta un halo oscuro sobre los paneles.
  * Elena: 1024x1536, y el juego espera retratos cuadrados (el rostro redondo recorta un cuadrado: ver Rostro()
    en PantallaDelDia.Diapositivas.cs).

Los originales se guardan en docs/07-prefabs/retratos-originales/ y de ahi se lee: volver a ejecutar es seguro.
Los nombres de salida son el id que calcula MaterialesNexus.IdDePersonaje (minusculas, sin tildes).
Requiere Pillow, numpy y scipy.
"""
import argparse
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

RAIZ = Path(__file__).resolve().parents[2]
ORIGINALES = RAIZ / "docs" / "07-prefabs" / "retratos-originales"
DESTINO = RAIZ / "game" / "proyecto de grado" / "Assets" / "Resources" / "Ilustraciones" / "personajes"
LADO = 768

# Solo para Kenji (coordenadas del original de 1254x1254): hasta donde llega la cabeza y donde esta el rostro.
ALTO_CABEZA = 380
CARA = (520, 110, 770, 380)

# nombre del original -> (id de salida, fondo, caja cuadrada (x0, y0, x1, y1) o None, borde roto abajo)
# fondo: "tablero" = tablero de transparencia dibujado; "alfa" = ya trae alfa (solo se limpia); "negro" = fondo negro liso.
RETRATOS = {
    "Kenji-Sato-Retrato.png": ("kenji-sato", "tablero", None, False),
    "Derek-Hartman.png": ("derek-hartman", "alfa", None, False),
    "Elena-Gomez.png": ("elena-gomez", "alfa", (-48, 0, 1072, 1120), True),
    "Rosa-Villamil.png": ("rosa-villamil", "negro", None, False),
    "Efrain-Mosquera.png": ("efrain-mosquera", "negro", None, False),
    "Recepcion-N1.png": ("la-recepcion-de-n1", "negro", None, False),   # el cliente del nivel 1: un lugar, no una persona
}


def quitar_tablero(rgb):
    """Mascara de fondo: pixeles grises claros conectados con el borde de arriba, izquierda o derecha.

    No se siembra desde abajo: el busto toca el borde inferior y su camisa tiene grises claros que no son fondo."""
    r = rgb.astype(np.int16)
    candidato = (r.min(2) >= 185) & ((r.max(2) - r.min(2)) <= 9)
    etiquetas, _ = ndimage.label(candidato)
    semillas = np.unique(np.concatenate([etiquetas[0], etiquetas[:, 0], etiquetas[:, -1]]))
    fondo = np.isin(etiquetas, semillas[semillas > 0])
    # Bolsillos de tablero atrapados entre mechones de pelo: no tocan el borde, asi que el flood-fill no llega.
    # Solo en la cabeza, y sin el rostro, donde si hay grises claros legitimos (ojos, cuello de la camisa).
    zona = np.zeros_like(candidato)
    zona[:ALTO_CABEZA] = True
    x0, y0, x1, y1 = CARA
    zona[y0:y1, x0:x1] = False
    fondo |= candidato & zona
    fondo = ndimage.binary_dilation(fondo, iterations=1)       # se come la franja clara del contorno
    return ndimage.gaussian_filter((~fondo).astype(np.float32), 0.8)


def quitar_negro(rgb, umbral=2, grosor=5):
    """Mascara para fondo negro liso. El fondo mide 0-1 (ruido de hasta 2); las sombras de la ropa y la tinta del
    dibujo son casi negras pero casi siempre por encima de 2, asi que ese es el umbral. Aun asi hay trazos finos
    unidos al fondo: se quedan solo las ZONAS ANCHAS (una apertura de 'grosor' px borra los trazos finos) que tocan
    el borde. Un umbral mayor (14) llena de agujeros las chaquetas oscuras."""
    negro = rgb.max(2) <= umbral
    etiquetas, _ = ndimage.label(negro)
    bordes = np.concatenate([etiquetas[0], etiquetas[-1], etiquetas[:, 0], etiquetas[:, -1]])
    semillas = np.unique(bordes)
    conectado = np.isin(etiquetas, semillas[semillas > 0])
    nucleo = ndimage.binary_opening(conectado, structure=np.ones((grosor, grosor)))
    # Los 2 px de borde suavizado entre el fondo y el dibujo: grises muy oscuros pegados al fondo.
    nucleo = ndimage.binary_dilation(nucleo, iterations=2) & (rgb.max(2) <= 10)
    return ndimage.gaussian_filter((~nucleo).astype(np.float32), 0.8)


def limpiar_alfa(alfa, minimo=40, maximo=235):
    """Lo casi transparente pasa a transparente y lo casi opaco a opaco; en medio, una rampa suave."""
    return np.clip((alfa - minimo) / float(maximo - minimo), 0, 1)


def quitar_islas(alfa, area_minima=3000):
    # Umbral bajo: las motas sueltas conservan un halo tenue (alfa < 0.5) que, con umbral alto, se quedaria.
    etiquetas, n = ndimage.label(alfa > 0.01)
    if n == 0:
        return alfa
    areas = ndimage.sum(np.ones_like(alfa), etiquetas, index=np.arange(1, n + 1))
    chicas = [i + 1 for i, a in enumerate(areas) if a < area_minima]
    if chicas:
        alfa = alfa * ~np.isin(etiquetas, chicas)
    return alfa


def borde_roto(alfa, margen=14, amplitud=16, semilla=7):
    """Borde inferior irregular, como el de los otros retratos (un corte recto se veria a papel recortado)."""
    h, w = alfa.shape
    rng = np.random.default_rng(semilla)
    x = np.arange(w)
    ruido = (np.sin(x / 31.0) + 0.6 * np.sin(x / 11.0 + 1) + 0.5 * rng.standard_normal(w)) / 2.1
    corte = h - margen - np.abs(ruido) * amplitud
    mascara = (np.arange(h)[:, None] < corte[None, :]).astype(np.float32)
    return alfa * ndimage.gaussian_filter(mascara, 0.7)


def preparar(origen: Path, salida: Path, tablero, caja, roto):
    im = Image.open(origen)
    rgb = np.array(im.convert("RGB"))
    if tablero == "tablero":
        alfa = quitar_tablero(rgb)
    elif tablero == "negro":
        alfa = quitar_negro(rgb)
    else:
        alfa = np.array(im.convert("RGBA"))[..., 3].astype(np.float32)
        alfa = limpiar_alfa(alfa)
    alfa = quitar_islas(alfa)
    imagen = Image.fromarray(np.dstack([rgb, np.round(alfa * 255).astype(np.uint8)]))

    if caja:                                                   # cuadrado: se rellena con transparente lo que sobre
        x0, y0, x1, y1 = caja
        lienzo = Image.new("RGBA", (x1 - x0, y1 - y0), (0, 0, 0, 0))
        lienzo.paste(imagen, (-x0, -y0))
        imagen = lienzo
    if roto:
        a = np.array(imagen)
        a[..., 3] = np.round(borde_roto(a[..., 3] / 255.0) * 255).astype(np.uint8)
        imagen = Image.fromarray(a)

    assert imagen.width == imagen.height, "el retrato debe quedar cuadrado"
    imagen.resize((LADO, LADO), Image.LANCZOS).save(salida, optimize=True)   # Pillow premultiplica el alfa al reducir


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--salida", type=Path, default=DESTINO)
    args = ap.parse_args()
    args.salida.mkdir(parents=True, exist_ok=True)
    for nombre, (id_, tablero, caja, roto) in RETRATOS.items():
        origen = ORIGINALES / nombre
        if not origen.exists():
            print("FALTA el original:", origen)
            continue
        preparar(origen, args.salida / (id_ + ".png"), tablero, caja, roto)
        print("listo:", id_ + ".png")


if __name__ == "__main__":
    main()
