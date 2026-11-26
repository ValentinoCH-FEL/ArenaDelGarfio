"""Compone la portada y las 6 imágenes de la intro con los sprites del equipo.

Uso (desde la raíz del repositorio):  python PixelArt/generar_portada.py
Requiere Pillow. Todo se dibuja pequeño y se amplía sin suavizado para que se vea en pixel art.
"""
import random
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
ART = ROOT / "Assets/_Project/Resources/PixelArt"
INTRO = ROOT / "Assets/_Project/Resources/Intro"
PORTADA = ROOT / "Assets/_Project/Resources/Portada"

AZUL, ROJO = (45, 108, 196), (196, 52, 52)
ORO, NEGRO, BLANCO = (242, 190, 60), (34, 26, 24), (240, 244, 250)


def sprite(path):
    return Image.open(ART / f"{path}.png").convert("RGBA")


def char(kind, team, frame=0):
    return sprite(f"Personajes/{kind}_{team}").crop((frame * 32, 0, frame * 32 + 32, 32))


def paste(img, s, cx, base, scale=1, flip=False):
    """Pega un sprite centrado en cx y apoyado en la línea base."""
    if flip:
        s = s.transpose(Image.FLIP_LEFT_RIGHT)
    if scale != 1:
        s = s.resize((s.width * scale, s.height * scale), Image.NEAREST)
    img.alpha_composite(s, (int(cx - s.width / 2), int(base - s.height)))


def tile(img, name, box):
    t = sprite(f"Texturas/{name}")
    x0, y0, x1, y1 = box
    layer = Image.new("RGBA", (x1 - x0, y1 - y0))
    for y in range(0, y1 - y0, t.height):
        for x in range(0, x1 - x0, t.width):
            layer.paste(t, (x, y))
    img.alpha_composite(layer, (x0, y0))


def sky(w, h, top, bottom, bands=8):
    img = Image.new("RGBA", (w, h))
    d = ImageDraw.Draw(img)
    for i in range(bands):
        k = i / (bands - 1)
        color = tuple(int(a + (b - a) * k) for a, b in zip(top, bottom))
        d.rectangle([0, i * h // bands, w, (i + 1) * h // bands], fill=color + (255,))
    return img


def mountains(img, base, height, color, snow, seed, step=70):
    rnd = random.Random(seed)
    d = ImageDraw.Draw(img)
    for x in range(-20, img.width + step, step):
        h = rnd.randint(height // 2, height)
        cx = x + rnd.randint(-15, 15)
        d.polygon([(cx - h, base), (cx, base - h), (cx + h, base)], fill=color)
        if snow:
            s = h // 4
            d.polygon([(cx - s, base - h + s), (cx, base - h), (cx + s, base - h + s),
                       (cx + s // 3, base - h + s - 3), (cx - s // 3, base - h + s + 2)], fill=BLANCO)


def clouds(img, seed, count=5, top=40):
    rnd = random.Random(seed)
    d = ImageDraw.Draw(img)
    for _ in range(count):
        x, y = rnd.randint(0, img.width), rnd.randint(6, top)
        for dx, w in ((0, 30), (8, 18), (-6, 40)):
            yy = y + (0 if w == 40 else -3 if w == 18 else 3)
            d.rectangle([x + dx, yy, x + dx + w, yy + 3], fill=(235, 238, 246, 200))


def landscape(w, h, ground="suelo_pradera", top=(58, 92, 160), bottom=(240, 176, 110), snow=True, seed=3):
    img = sky(w, h, top, bottom)
    clouds(img, seed)
    horizon = int(h * 0.52)
    mountains(img, horizon, int(h * 0.36), (90, 92, 130), snow, seed)
    mountains(img, horizon + 6, int(h * 0.2), (74, 108, 84), False, seed + 1, 50)
    tile(img, ground, (0, horizon, w, h))
    return img


def inca_wall(img, x0, x1, base, rows=2):
    tile(img, "muro_pradera", (x0, base - rows * 16, x1, base))
    d = ImageDraw.Draw(img)
    d.rectangle([x0, base - rows * 16 - 2, x1, base - rows * 16], fill=NEGRO)
    d.rectangle([x0, base, x1, base + 1], fill=NEGRO)


def river(img, x0, x1, y0, y1, water="agua_rio"):
    tile(img, water, (x0, y0, x1, y1))
    d = ImageDraw.Draw(img)
    d.rectangle([x0, y0, x1, y0 + 1], fill=BLANCO)
    d.rectangle([x0, y1 - 1, x1, y1], fill=NEGRO)


def banner(img, x, base, color):
    d = ImageDraw.Draw(img)
    d.rectangle([x, base - 46, x + 1, base], fill=NEGRO)
    d.rectangle([x + 2, base - 45, x + 20, base - 31], fill=color, outline=NEGRO)
    d.rectangle([x + 9, base - 41, x + 13, base - 35], fill=ORO)


def army(img, team, xs, base, flip, frame=0, seed=0):
    kinds = ["equilibrado", "veloz", "tanque"]
    for i, x in enumerate(xs):
        paste(img, char(kinds[(i + seed) % 3], team, frame), x, base + (i % 2) * 7, flip=flip)


def props(img, base, xs, name):
    for x in xs:
        paste(img, sprite(f"Objetos/{name}"), x, base)


def rope(img, a, b):
    d = ImageDraw.Draw(img)
    d.line([a, b], fill=(214, 176, 96), width=2)
    paste(img, sprite("Objetos/garfio").rotate(-90), b[0], b[1] + 8)


def save_intro(img, number):
    # Se sube la escena para que la banda de texto de la intro no tape a los personajes
    up = 24
    out = Image.new('RGBA', img.size)
    out.paste(img.crop((0, up, img.width, img.height)), (0, 0))
    out.paste(img.crop((0, img.height - up, img.width, img.height)), (0, img.height - up))
    img = out
    img.convert("RGB").resize((1600, 900), Image.NEAREST).save(INTRO / f"intro_{number}.jpg", quality=95)


def intro():
    W, H = 400, 225

    # 1. Muerte de Huayna Cápac: atardecer, el Inca y sus orejones de luto
    img = landscape(W, H, top=(44, 40, 82), bottom=(212, 110, 82), seed=5)
    inca_wall(img, 110, 290, 150, 3)
    paste(img, char("tanque", "azul"), 200, 176, 2)
    d = ImageDraw.Draw(img)
    d.rectangle([186, 104, 214, 108], fill=ORO, outline=NEGRO)
    army(img, "azul", [120, 150], 168, False, seed=2)
    army(img, "rojo", [252, 282], 168, True, seed=2)
    props(img, 166, [40, 360], "queuna")
    save_intro(img, 1)

    # 2. Dos hermanos: Cusco a la izquierda, Quito a la derecha
    img = landscape(W, H, seed=9)
    inca_wall(img, 0, 130, 150)
    inca_wall(img, 270, 400, 150)
    paste(img, char("tanque", "azul"), 110, 190, 3)
    paste(img, char("tanque", "rojo"), 290, 190, 3, flip=True)
    banner(img, 30, 175, AZUL)
    banner(img, 350, 175, ROJO)
    props(img, 172, [200], "queuna")
    save_intro(img, 2)

    # 3. Estalla la guerra: dos ejércitos frente a frente
    img = landscape(W, H, top=(70, 60, 96), bottom=(226, 140, 90), seed=12)
    army(img, "azul", [30, 62, 94, 126, 158], 170, False, 2)
    army(img, "rojo", [242, 274, 306, 338, 370], 170, True, 3, 1)
    army(img, "azul", [46, 78, 110, 142], 196, False, 4, 2)
    army(img, "rojo", [258, 290, 322, 354], 196, True, 5)
    for x, c in ((14, AZUL), (140, AZUL), (240, ROJO), (370, ROJO)):
        banner(img, x, 160, c)
    save_intro(img, 3)

    # 4. Chimborazo: las tropas de Quito avanzan hacia el sur
    img = sky(W, H, (72, 118, 178), (190, 214, 232))
    clouds(img, 21, 7)
    d = ImageDraw.Draw(img)
    d.polygon([(90, 130), (205, 24), (320, 130)], fill=(104, 112, 140))
    d.polygon([(165, 61), (205, 24), (246, 62), (224, 56), (206, 70), (190, 54)], fill=BLANCO)
    mountains(img, 132, 40, (88, 110, 100), False, 4, 55)
    tile(img, "suelo_puna", (0, 130, W, H))
    army(img, "rojo", [60, 94, 128, 162, 196], 168, True, 2)
    army(img, "rojo", [80, 114, 148, 182], 196, True, 4, 1)
    army(img, "azul", [330, 366], 182, False, 7)
    banner(img, 214, 170, ROJO)
    props(img, 160, [30, 290, 372], "ichu_grande")
    save_intro(img, 4)

    # 5. Cruce del Apurímac y Quipaipán
    img = landscape(W, H, ground="suelo_canon", top=(86, 70, 106), bottom=(238, 160, 96), snow=False, seed=17)
    river(img, 0, W, 158, 192)
    army(img, "azul", [46, 84, 122], 150, False, 0)
    army(img, "rojo", [250, 290, 330, 370], 218, True, 6)
    paste(img, char("equilibrado", "azul", 7), 205, 190)
    rope(img, (262, 195), (214, 172))
    props(img, 150, [20, 330, 386], "cactus")
    banner(img, 160, 150, AZUL)
    save_intro(img, 5)

    # 6. Tú peleas por el Cusco
    img = landscape(W, H, top=(40, 62, 130), bottom=(250, 196, 112), seed=23)
    inca_wall(img, 0, W, 134)
    river(img, 0, W, 150, 172)
    paste(img, char("equilibrado", "azul", 6), 130, 196, 3)
    rope(img, (166, 164), (262, 142))
    paste(img, char("equilibrado", "rojo"), 292, 150, 1, flip=True)
    banner(img, 40, 200, AZUL)
    props(img, 134, [346, 372], "queuna")
    save_intro(img, 6)


# Letra pixel de 5x7 solo con las letras del título
LETTERS = {
    "A": ["01110", "10001", "10001", "11111", "10001", "10001", "10001"],
    "D": ["11110", "10001", "10001", "10001", "10001", "10001", "11110"],
    "E": ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
    "F": ["11111", "10000", "10000", "11110", "10000", "10000", "10000"],
    "G": ["01110", "10001", "10000", "10111", "10001", "10001", "01110"],
    "I": ["11111", "00100", "00100", "00100", "00100", "00100", "11111"],
    "L": ["10000", "10000", "10000", "10000", "10000", "10000", "11111"],
    "N": ["10001", "11001", "11001", "10101", "10011", "10011", "10001"],
    "O": ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
    "R": ["11110", "10001", "10001", "11110", "10100", "10010", "10001"],
    " ": ["000"] * 7,
}


def title_image():
    text = "ARENA DEL GARFIO"
    cell = 3
    width = sum((len(LETTERS[c][0]) + 1) * cell for c in text)
    mask = Image.new("L", (width + 8, 7 * cell + 10), 0)
    d = ImageDraw.Draw(mask)
    x = 3
    for c in text:
        for row, bits in enumerate(LETTERS[c]):
            for col, bit in enumerate(bits):
                if bit == "1":
                    d.rectangle([x + col * cell, 3 + row * cell, x + (col + 1) * cell - 1, 3 + (row + 1) * cell - 1], fill=255)
        x += (len(LETTERS[c][0]) + 1) * cell

    out = Image.new("RGBA", mask.size)
    # Borde negro de 2 px y sombra
    for dx in range(-2, 3):
        for dy in range(-2, 5):
            out.paste(Image.new("RGBA", mask.size, NEGRO + (255,)), (dx, dy), mask)
    fill = Image.new("RGBA", mask.size, (255, 208, 74, 255))
    ImageDraw.Draw(fill).rectangle([0, 3 + 4 * cell, mask.width, mask.height], fill=(242, 140, 38, 255))
    out.paste(fill, (0, 0), mask)
    out.save(PORTADA / "titulo.png")


def logo_image():
    """Chakana (cruz andina) dorada con el garfio encima."""
    img = Image.new("RGBA", (48, 48))
    d = ImageDraw.Draw(img)
    for box in ([18, 2, 29, 45], [2, 18, 45, 29], [10, 10, 37, 37]):
        d.rectangle([box[0] - 2, box[1] - 2, box[2] + 2, box[3] + 2], fill=NEGRO)
    for box in ([18, 2, 29, 45], [2, 18, 45, 29], [10, 10, 37, 37]):
        d.rectangle(box, fill=ORO)
    d.ellipse([16, 16, 31, 31], fill=(30, 52, 96), outline=NEGRO, width=2)
    paste(img, sprite("Objetos/garfio"), 24, 40, 2)
    img.save(PORTADA / "logo.png")


def background():
    """Fondo 480x270: los Andes, los muros incas, el río y los dos bandos."""
    W, H = 480, 270
    img = landscape(W, H, top=(28, 40, 84), bottom=(232, 150, 96), seed=31)
    inca_wall(img, 0, 138, 168)
    inca_wall(img, 342, W, 168)
    river(img, 0, W, 214, 240)
    tile(img, "suelo_pradera", (0, 241, W, H))
    army(img, "azul", [22, 52, 82], 196, False, 0)
    army(img, "rojo", [398, 428, 458], 196, True, 0, 1)
    banner(img, 112, 190, AZUL)
    banner(img, 346, 190, ROJO)
    props(img, 262, [22, 458], "ichu_grande")
    props(img, 260, [96, 392], "roca_pradera")
    props(img, 158, [14, 466], "queuna")
    # Oscurecer un poco para que se lean los textos y botones
    img.alpha_composite(Image.new("RGBA", img.size, (10, 12, 26, 70)))
    img.convert("RGB").save(PORTADA / "fondo.png")


if __name__ == "__main__":
    PORTADA.mkdir(parents=True, exist_ok=True)
    intro()
    title_image()
    logo_image()
    background()
    print("Listo")
