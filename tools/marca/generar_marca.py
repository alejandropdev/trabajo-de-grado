"""Genera los recursos de marca del ejecutable a partir del logo fuente.

    python tools/marca/generar_marca.py

Entrada : game/proyecto de grado/Assets/Art/Backgrounds/logo.png  (RGBA, fondo transparente)
Salida  : game/proyecto de grado/Assets/Art/Branding/
            IconoNexus.png       1024x1024, el icono del .exe y de la barra de tareas
            LogoArranque.png     1600x1000, el logo + "NEXUS PROTOCOL" para la pantalla de arranque

El script de editor ConfiguracionDeMarca.cs es el que los conecta con PlayerSettings.
Requiere Pillow (pip install pillow).
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

RAIZ = Path(__file__).resolve().parents[2]
PROYECTO = RAIZ / "game" / "proyecto de grado"
FUENTE_LOGO = PROYECTO / "Assets" / "Art" / "Backgrounds" / "logo.png"
FUENTES = PROYECTO / "Assets" / "Resources" / "Fuentes"
SALIDA = PROYECTO / "Assets" / "Art" / "Branding"

TINTA = (238, 243, 247, 255)    # Tema.ink      #EEF3F7
CIAN = (34, 211, 238, 255)      # Tema.cyan     #22D3EE
TENUE = (148, 161, 178, 255)    # Tema.inkMuted #94A1B2


def logo_recortado() -> Image.Image:
    """El logo sin el ruido de alfa casi nulo que trae el PNG y recortado a su contenido."""
    im = Image.open(FUENTE_LOGO).convert("RGBA")
    r, g, b, a = im.split()
    a = a.point(lambda v: 0 if v <= 8 else v)
    im = Image.merge("RGBA", (r, g, b, a))
    return im.crop(a.getbbox())


def en_cuadrado(im: Image.Image, lado: int, margen: float) -> Image.Image:
    util = int(lado * (1 - 2 * margen))
    escala = util / max(im.size)
    pieza = im.resize((round(im.width * escala), round(im.height * escala)), Image.LANCZOS)
    lienzo = Image.new("RGBA", (lado, lado), (0, 0, 0, 0))
    lienzo.alpha_composite(pieza, ((lado - pieza.width) // 2, (lado - pieza.height) // 2))
    return lienzo


def ancho(draw: ImageDraw.ImageDraw, texto: str, fuente, espaciado: int) -> float:
    return sum(draw.textlength(c, font=fuente) for c in texto) + espaciado * (len(texto) - 1)


def escribir(draw, x, y, texto, fuente, color, espaciado):
    for c in texto:
        draw.text((x, y), c, font=fuente, fill=color)
        x += draw.textlength(c, font=fuente) + espaciado
    return x


def logo_de_arranque(logo: Image.Image) -> Image.Image:
    W, H = 1600, 1000
    lienzo = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    alto_logo = 620
    escala = alto_logo / logo.height
    pieza = logo.resize((round(logo.width * escala), alto_logo), Image.LANCZOS)
    lienzo.alpha_composite(pieza, ((W - pieza.width) // 2, 20))

    draw = ImageDraw.Draw(lienzo)
    titulo = ImageFont.truetype(str(FUENTES / "Orbitron-Bold.ttf"), 112)
    subtitulo = ImageFont.truetype(str(FUENTES / "Rajdhani-SemiBold.ttf"), 52)

    a1, a2, hueco, esp = "NEXUS", "PROTOCOL", 44, 10
    total = ancho(draw, a1, titulo, esp) + hueco + ancho(draw, a2, titulo, esp)
    x = (W - total) / 2
    y = 690
    x = escribir(draw, x, y, a1, titulo, TINTA, esp) + hueco - esp
    escribir(draw, x, y, a2, titulo, CIAN, esp)

    sub = "SIMULADOR DE PROYECTOS DE SOFTWARE"
    esp2 = 6
    escribir(draw, (W - ancho(draw, sub, subtitulo, esp2)) / 2, 850, sub, subtitulo, TENUE, esp2)
    return lienzo


def main():
    SALIDA.mkdir(parents=True, exist_ok=True)
    logo = logo_recortado()
    en_cuadrado(logo, 1024, 0.03).save(SALIDA / "IconoNexus.png", optimize=True)
    logo_de_arranque(logo).save(SALIDA / "LogoArranque.png", optimize=True)
    print("Listo:", *(p.name for p in sorted(SALIDA.glob("*.png"))))


if __name__ == "__main__":
    main()
