"""Build the campaign pixel assets with Pillow from the original roster."""
from pathlib import Path
from random import Random

from PIL import Image, ImageDraw, ImageFilter

ASSETS = Path(__file__).resolve().parents[1] / "Assets/Campaign/Resources/BioBot"
CELL = 64
FRAMES = 4
ROWS = 48
INK = "#152438"


def canvas():
    return Image.new("RGBA", (CELL, CELL))


def polygon(draw, points, fill, outline=INK):
    draw.polygon(points, fill=fill, outline=outline)


def duck(frame):
    img = canvas()
    d = ImageDraw.Draw(img)
    # Feet are drawn behind the belly and will be refracted below the waterline.
    kick = (0, 3, 1, -2)[frame]
    polygon(d, [(26, 36), (30, 38), (27 + kick, 47), (19 + kick, 46), (21 + kick, 43)], "#e78843")
    polygon(d, [(34, 36), (38, 37), (40 - kick, 45), (34 - kick, 49), (30 - kick, 46)], "#ffb65e")
    polygon(d, [(8, 26), (16, 28), (25, 25), (35, 25), (44, 30), (43, 37), (35, 43), (23, 43), (15, 40), (11, 35)], "#f0c07c")
    polygon(d, [(14, 34), (24, 38), (37, 37), (42, 33), (42, 37), (35, 42), (23, 42), (16, 39)], "#bc754c", None)
    polygon(d, [(13, 29), (23, 27), (34, 29), (32, 33), (24, 37), (17, 34)], "#5b645d")
    d.line([(17, 30), (23, 31), (27, 30)], fill="#a2ab81", width=2)
    d.line([(18, 34), (24, 36), (31, 32)], fill="#765998", width=2)
    polygon(d, [(32, 28), (32, 21), (35, 16), (43, 13), (50, 15), (54, 19), (53, 25), (47, 27), (42, 32), (36, 34)], "#338579")
    polygon(d, [(34, 21), (39, 15), (45, 15), (48, 17), (39, 20), (36, 25)], "#6fc79d", None)
    d.line([(33, 27), (37, 29), (42, 29)], fill="#fce6a6", width=2)
    polygon(d, [(50, 20), (55, 20), (61, 23), (58, 26), (50, 25), (48, 23)], "#f1a34d")
    d.line([(53, 22), (58, 23)], fill="#ffe18c", width=1)
    d.rectangle((46, 18, 48, 20), fill=INK)
    d.point((46, 18), fill="#fff6cc")
    return img


def otter(frame):
    img = canvas()
    d = ImageDraw.Draw(img)
    tail = (0, 2, 1, -2)[frame]
    polygon(d, [(24, 33), (17, 40), (8, 44 + tail), (5, 42 + tail), (13, 36), (18, 30)], "#80595c")
    polygon(d, [(15, 28), (22, 22), (29, 25), (35, 27), (48, 29), (51, 35), (45, 42), (31, 44), (21, 40), (15, 35)], "#a66b59")
    polygon(d, [(19, 32), (29, 35), (47, 32), (47, 37), (42, 41), (30, 42), (23, 38)], "#d49b72", None)
    polygon(d, [(14, 27), (13, 18), (16, 15), (20, 16), (24, 14), (27, 16), (31, 14), (33, 17), (32, 24), (29, 30), (21, 32)], "#c48a69")
    d.ellipse((20, 22, 31, 29), fill="#f3cd96")
    d.rectangle((25, 22, 27, 23), fill=INK)
    d.point((19, 21), fill=INK)
    d.point((29, 20), fill=INK)
    d.line([(16, 26), (22, 25)], fill="#fff0bf")
    d.line([(27, 26), (33, 26)], fill="#fff0bf")
    y = (0, -1, 0, 1)[frame]
    d.ellipse((35, 18+y, 51, 34+y), fill="#ffe28a", outline="#7a4935", width=2)
    d.polygon([(42, 21+y), (48, 26+y), (42, 31+y), (39, 26+y)], fill="#ffad52")
    d.rectangle((40, 22+y, 43, 24+y), fill="#fff8c8")
    polygon(d, [(27, 27), (35, 26), (39, 29), (37, 32), (28, 31)], "#b97c63")
    polygon(d, [(39, 34), (47, 31), (51, 33), (48, 37), (41, 38)], "#84555a")
    return img


def turtle(frame):
    img = canvas()
    d = ImageDraw.Draw(img)
    stroke = (0, 3, 0, -2)[frame]
    polygon(d, [(31, 32), (40, 34), (43 + stroke, 41), (38 + stroke, 48), (33, 47), (32, 42), (27, 39)], "#50a28a")
    polygon(d, [(20, 32), (19, 42), (12 - stroke, 46), (8, 44), (10, 38), (15, 34)], "#388575")
    polygon(d, [(38, 30), (45, 25), (53, 25), (58, 30), (56, 34), (49, 37), (42, 37)], "#79bf8c")
    d.rectangle((52, 28, 54, 30), fill=INK)
    d.point((52, 28), fill="#f9eeb0")
    polygon(d, [(9, 35), (10, 27), (15, 21), (23, 17), (36, 18), (43, 23), (47, 30), (43, 36), (33, 40), (18, 39)], "#3d6259")
    polygon(d, [(11, 28), (16, 22), (24, 19), (35, 19), (42, 23), (45, 30), (35, 34), (21, 34)], "#72a26d")
    polygon(d, [(25, 20), (35, 20), (40, 25), (35, 30), (25, 30), (21, 25)], "#558363")
    d.line([(15, 25), (20, 24), (23, 21)], fill="#bed18b", width=2)
    d.line([(17, 34), (21, 36), (34, 37), (42, 34)], fill="#d8a665", width=2)
    return img


def fish(frame):
    img = canvas()
    d = ImageDraw.Draw(img)
    wag = (0, 2, 0, -2)[frame]
    polygon(d, [(22, 28), (7 + wag, 17), (9 - wag, 32), (7 + wag, 43), (23, 35)], "#488fb1")
    polygon(d, [(28, 26), (32, 15), (37, 15), (43, 26)], "#b5d9c8")
    polygon(d, [(33, 36), (42, 37), (35, 46 + wag), (29, 43)], "#43a1a9")
    polygon(d, [(18, 29), (25, 23), (36, 22), (49, 25), (56, 29), (58, 32), (52, 37), (41, 41), (29, 41), (20, 37)], "#58b5bb")
    polygon(d, [(21, 32), (33, 32), (49, 29), (55, 32), (51, 36), (40, 39), (28, 39)], "#bbdfc8", None)
    for n in range(3):
        x = 26 + n * 6
        polygon(d, [(x, 24), (x + 3, 24), (x + 5, 32), (x + 2, 37)], "#376d82", None)
    polygon(d, [(38, 32), (34, 34), (30, 42 + wag), (37, 40), (43, 34)], "#65a5a5")
    d.rectangle((48, 28, 51, 30), fill=INK)
    d.point((48, 28), fill="#fdf4bb")
    d.line([(54, 32), (57, 32)], fill="#435662")
    return img


def robot(frame):
    img = canvas()
    d = ImageDraw.Draw(img)
    polygon(d, [(10, 26), (19, 24), (20, 38), (11, 39), (6, 35), (6, 30)], "#475c77")
    d.line([(8, 30), (8, 35)], fill=("#9af1df", "#477e9b")[frame % 2], width=2)
    polygon(d, [(13, 28), (18, 22), (29, 19), (31, 13), (34, 12), (40, 13), (42, 20), (53, 22), (60, 27), (61, 33), (56, 39), (45, 44), (27, 44), (17, 40), (12, 35)], "#df8e55")
    polygon(d, [(15, 28), (20, 23), (30, 21), (42, 21), (52, 23), (57, 26), (33, 26), (24, 30)], "#f6bd69", None)
    polygon(d, [(18, 36), (26, 39), (44, 39), (55, 36), (51, 40), (44, 43), (28, 43), (18, 39)], "#a35a55", None)
    polygon(d, [(35, 25), (52, 25), (57, 28), (57, 33), (53, 36), (38, 35), (33, 31)], "#245576")
    d.polygon([(36, 27), (51, 27), (55, 29), (51, 32), (39, 32)], fill="#58c9c1")
    d.line([(39, 27), (47, 27)], fill="#c1f4d3", width=2)
    d.rectangle((31, 15, 39, 17), fill="#7bead6")
    d.rectangle((19, 32, 24, 34), fill="#713f4c")
    d.point((54, 29), fill="#eeffcd")
    return img


def debris(kind, frame):
    img = canvas()
    d = ImageDraw.Draw(img)
    if kind == 0:  # Bottle with a dent, creased label and air pocket.
        polygon(d, [(9, 26), (19, 22), (36, 22), (42, 25), (49, 25), (54, 27), (56, 34), (48, 37), (41, 36), (35, 40), (16, 39), (9, 35)], "#4a9f97")
        polygon(d, [(11, 28), (18, 24), (36, 24), (42, 28), (37, 30), (20, 29), (14, 33)], "#81d4b4", None)
        polygon(d, [(22, 23), (35, 23), (37, 38), (24, 38)], "#e4a25e")
        d.line([(25, 26), (32, 26)], fill="#f8df9c", width=2)
        d.line([(27, 32), (34, 34)], fill="#995b58", width=2)
        polygon(d, [(48, 26), (56, 26), (59, 29), (58, 35), (50, 35)], "#a45c63")
        d.line([(50, 27), (56, 27)], fill="#e99970")
        x = (14, 16, 17, 15)[frame]
        d.line([(x, 28), (x+3, 26)], fill="#d9f4ca")
    elif kind == 1:  # Broken shell on the bed with radiating ribs.
        polygon(d, [(10, 37), (16, 28), (25, 23), (33, 24), (44, 27), (53, 36), (50, 41), (39, 45), (24, 44), (13, 42)], "#aa635b")
        polygon(d, [(15, 37), (19, 29), (27, 25), (35, 26), (43, 28), (49, 35), (36, 42), (24, 41)], "#e29b71", None)
        for x, y in [(18, 32), (24, 27), (32, 27), (40, 29), (46, 33)]:
            d.line([(x, y), (31, 39)], fill="#f4d3a1", width=2)
        polygon(d, [(35, 38), (42, 34), (52, 36), (48, 41), (39, 44), (23, 43)], "#844b57")
    elif kind == 2:  # Fish silhouette caught in visible, flexing nylon.
        img = fish(frame).resize((36, 36), Image.Resampling.NEAREST)
        tile = canvas()
        tile.alpha_composite(img, (15, 16))
        d = ImageDraw.Draw(tile)
        flex = (0, 2, 1, -1)[frame]
        for index, pts in enumerate(([(10, 41), (22, 20), (42, 18), (54, 33), (34, 47), (19, 35), (27, 19)],
                                    [(14, 47), (19, 32), (39, 22), (52, 24), (49, 39), (21, 43), (10, 37)])):
            path = [(x, y + flex * (i % 2)) for i, (x, y) in enumerate(pts)]
            d.line(path, fill="#24354f", width=3)
            d.line(path, fill=("#efe4a6", "#a7dbc8")[index], width=1)
        return tile
    elif kind == 7:  # Outline deforms as oil spreads over the water.
        sway = (0, 2, 1, -2)[frame]
        polygon(d, [(4, 34), (10, 28), (22, 26+sway), (31, 27), (41, 24-sway), (53, 27), (59, 32), (55, 37), (43, 40), (30, 38), (18, 41), (7, 38)], "#252940")
        d.line([(9, 34), (18, 30), (26, 31), (34, 30), (44, 27-sway), (52, 29)], fill="#7a678e", width=2)
        d.line([(16, 37), (26, 35), (34, 36), (46, 33), (52, 33)], fill="#3f7e86", width=2)
        d.line([(19+sway, 31), (24+sway, 31)], fill="#b79991")
        d.line([(38-sway, 34), (43-sway, 33)], fill="#a88e72")
    elif kind == 10:  # Salvage in an open copper housing, with a bright gold core.
        lift = (0, 1, 0, -1)[frame]
        polygon(d, [(12, 28), (20, 23), (29, 21), (46, 25), (52, 31),
                    (50, 39), (38, 44), (23, 43), (13, 37)], "#72565b")
        polygon(d, [(15, 28), (26, 24), (44, 26), (48, 31), (46, 38),
                    (28, 42), (17, 38)], "#b37c5b")
        polygon(d, [(23, 33), (30, 25), (41, 26), (45, 34), (39, 41),
                    (27, 40)], "#383e53")
        polygon(d, [(25, 32), (30, 25+lift), (37, 24+lift), (43, 32), (36, 39),
                    (28, 38)], "#f4ba60")
        d.line([(28, 32), (33, 26+lift), (38, 28+lift)], fill="#ffe9a3", width=2)
        d.line([(27, 35), (36, 37), (41, 33)], fill="#df8054", width=2)
        d.line([(15, 32), (20, 31), (24, 35)], fill="#f2ae6c")
        d.point((48, 29), fill="#ffe193")
        d.point((40, 19+lift), fill="#f9d17d")
    return img



def hostile(row, frame):
    img = canvas()
    d = ImageDraw.Draw(img)
    sway = (0, 2, 1, -1)[frame]
    if row == 20:  # Sludge skimmer, low in the water.
        polygon(d, [(4, 35), (13, 30), (20, 25), (34, 22), (44, 24), (52, 29), (60, 32),
                    (57, 38), (49, 41), (32, 40+sway), (20, 44), (9, 40)], "#3c394f")
        polygon(d, [(13, 34), (20, 28), (33, 24), (43, 26), (49, 31), (34, 32),
                    (26, 37), (19, 37)], "#59516d", None)
        polygon(d, [(8, 35), (17, 34), (24, 39), (15, 41)], "#263447", None)
        d.line([(23, 29), (33, 26), (42, 27)], fill="#8b777c", width=2)
        d.line([(17, 34), (28, 35), (36, 32)], fill="#783f58", width=2)
        d.line([(14, 35), (18, 35)], fill="#ff945f", width=2)
        d.point((15, 34), fill="#ffe19d")
        d.line([(39, 37), (46, 36), (51, 37)], fill="#456b75")
    elif row == 21:  # Torn bag with knotted handles and drifting strips.
        polygon(d, [(9, 26), (14, 18), (22, 16), (24, 10), (29, 9), (32, 15), (35, 19),
                    (45, 16), (50, 22), (45, 27), (50, 34), (47, 45+sway),
                    (40, 39), (35, 47), (32, 40), (21, 42+sway), (19, 37), (9, 36)], "#9c9294")
        polygon(d, [(13, 27), (20, 19), (29, 18), (34, 22), (31, 27), (23, 25),
                    (17, 32), (12, 32)], "#c1b7a0", None)
        polygon(d, [(15, 33), (25, 26), (36, 28), (45, 26), (43, 34), (46, 42+sway),
                    (40, 38), (35, 40), (33, 36), (24, 38)], "#566276")
        d.line([(26, 16), (26, 12), (29, 11), (30, 15)], fill="#d8c2a5")
        d.line([(37, 32), (41, 31), (44, 36+sway)], fill="#789286", width=2)
        d.line([(17, 32), (21, 31)], fill="#ffac73", width=2)
    elif row == 22:  # Scrap crawler, with claws that brace on the bottom.
        for i in range(3):
            x = 21+i*11
            step = sway if i % 2 == 0 else -sway
            polygon(d, [(x-4, 33), (x+1, 32), (x+6, 39), (x+4+step, 47), (x-1+step, 47),
                        (x+1, 42)], "#6b525e")
            d.point((x+step, 45), fill="#d08265")
        polygon(d, [(13, 31), (19, 25), (29, 22), (41, 23), (52, 31), (50, 36),
                    (44, 39), (27, 39), (18, 37)], "#a76b60")
        polygon(d, [(17, 30), (25, 25), (39, 25), (47, 29), (39, 32), (27, 31)], "#ce8b70", None)
        polygon(d, [(32, 23), (38, 17), (45, 19), (44, 25)], "#5c7881")
        polygon(d, [(18, 32), (12, 29), (6, 31+sway), (5, 36+sway), (10, 36+sway), (9, 39+sway),
                    (14, 39+sway), (19, 37)], "#bd795f")
        d.line([(20, 32), (23, 32)], fill="#fdb16a", width=2)
        d.line([(27, 34), (40, 34)], fill="#6c535f")
    elif row == 23:  # Can armor with oxidized edges and a narrow visor.
        polygon(d, [(19, 38), (25, 37), (26+sway, 45), (19+sway, 47), (16+sway, 45)], "#596779")
        polygon(d, [(38, 37), (43, 37), (47-sway, 43), (42-sway, 46), (38-sway, 45)], "#43576d")
        polygon(d, [(18, 20), (23, 16), (42, 15), (49, 19), (48, 36), (43, 41), (25, 41),
                    (18, 36)], "#8c958f")
        polygon(d, [(19, 26), (22, 23), (45, 23), (48, 26), (47, 32), (43, 34), (21, 33)], "#7f5560")
        d.line([(23, 27), (34, 27), (41, 29)], fill="#ff9f61", width=2)
        d.line([(24, 26), (31, 26)], fill="#fbd998")
        d.line([(22, 20), (32, 17), (43, 18)], fill="#e7c6a6", width=2)
        d.line([(21, 35), (25, 38), (35, 38)], fill="#dcb19a")
        d.line([(41, 21), (46, 20), (46, 24)], fill="#bc7e63")
    elif row == 24:  # Twin-hull dredger with a brass dive ballast and lit intake.
        lift = (0, 1, 0, -1)[frame]
        polygon(d, [(9, 32), (15, 24), (41, 23), (56, 28), (54, 34), (44, 39),
                    (19, 38)], "#375069")
        polygon(d, [(13, 27), (30, 25), (47, 26), (52, 29), (43, 32), (19, 32)], "#7c8091", None)
        polygon(d, [(18, 37), (23, 28), (50, 28), (61, 33), (56, 41), (49, 44),
                    (25, 44)], "#56495c")
        polygon(d, [(21, 37), (27, 32), (54, 32), (58, 35), (49, 40), (28, 41)], "#a76b61", None)
        d.line([(23, 39), (36, 40), (50, 38), (56, 35)], fill="#df9a72", width=2)
        polygon(d, [(20, 26), (23, 18), (37, 18), (43, 25), (38, 31),
                    (22, 31)], "#6d6576")
        polygon(d, [(22, 24), (26, 20), (36, 20), (41, 24), (36, 27), (23, 27)], "#778e95", None)
        d.rectangle((25, 22, 30, 24), fill="#ffd081")
        d.point((25, 22), fill="#fff4bd")
        polygon(d, [(31, 17), (35, 12+lift), (41, 12+lift), (43, 16+lift),
                    (41, 20), (35, 21)], "#a97962")
        d.line([(36, 14+lift), (40, 14+lift)], fill="#edb177", width=2)
        d.line([(14, 34), (18, 33), (22, 33)], fill="#70c9c7", width=2)
        d.rectangle((46, 34, 48, 36), fill="#263448")
        d.rectangle((41, 35, 43, 37), fill="#263448")
    else:  # Tanker boss with a broad draft, smokestacks and a lit bridge.
        polygon(d, [(17, 23), (17, 17), (28, 15), (35, 15), (36, 21), (49, 22),
                    (55, 26), (52, 33), (19, 33)], "#67737c")
        for x, top in [(37, 11), (46, 14)]:
            polygon(d, [(x, 24), (x+1, top), (x+6, top), (x+6, 24)], "#626070")
            d.line([(x+1, top+2), (x+5, top+2)], fill="#d1866a")
            d.line([(x+3, top-2), (x+6+sway, top-5)], fill="#948589", width=2)
        polygon(d, [(4, 29), (12, 28), (24, 29), (47, 28), (59, 31), (54, 40),
                    (46, 46), (24, 45), (11, 40)], "#784d61")
        polygon(d, [(6, 31), (18, 32), (47, 31), (56, 33), (50, 39), (19, 39), (10, 36)], "#bb735f", None)
        d.line([(12, 36), (27, 36), (49, 35)], fill="#e19c70", width=2)
        for x in (21, 25, 29):
            d.rectangle((x, 19, x+2, 21), fill="#ffd28a")
        d.line([(7, 30), (21, 30), (35, 30), (47, 29)], fill="#dfaa83")
        d.line([(32, 43), (45, 43), (51, 39)], fill="#405664", width=2)
    return img


def squid(frame):
    img = canvas()
    d = ImageDraw.Draw(img)
    curl = (-2, 1, 3, 0)[frame]
    # Each tentacle keeps a thick root and curls independently at the tip.
    for index, root in enumerate((20, 26, 33, 40, 45)):
        sweep = (-13, -7, 2, 9, 14)[index]
        tip = root + sweep
        wave = curl if index % 2 == 0 else -curl
        path = [(root, 36), (root + sweep // 3, 43),
                (tip - sweep // 4, 48 + wave), (tip, 45 + wave), (tip, 41 + wave)]
        d.line(path, fill=INK, width=8, joint="curve")
        d.line(path, fill=("#9d548d", "#bd5b8c", "#a95b9e")[index % 3], width=5, joint="curve")
        d.line(path[2:], fill="#ee92ae", width=2)
        d.rectangle((path[2][0], path[2][1] - 1, path[2][0] + 1, path[2][1]), fill="#ffbdc3")

    lift = (0, -1, -2, -1)[frame]
    silhouette = [(14, 28), (18, 19), (25, 13), (35, 9), (43, 13), (47, 20),
                  (52, 27), (52, 34), (46, 40), (35, 44), (23, 42), (16, 38)]
    polygon(d, [(x, y + lift) for x, y in silhouette], "#d975a2")
    polygon(d, [(16, 28), (21, 18), (30, 13), (39, 12), (43, 17), (39, 21), (28, 20), (21, 29)],
            "#f394ac", None)
    polygon(d, [(17, 35), (26, 39), (38, 38), (49, 32), (48, 37), (38, 42), (26, 42), (19, 39)],
            "#a55891", None)
    d.line([(23, 21), (28, 17), (34, 16)], fill="#ffd0bd", width=2)
    for x in (24, 39):
        d.rectangle((x, 27, x + 4, 32), fill=INK)
        d.point((x + 1, 27), fill="#fff0cf")
    d.line([(30, 34), (32, 36), (35, 36), (37, 34)], fill=INK, width=2)
    d.line([(19, 34), (22, 34)], fill="#ffb3ac", width=2)
    d.line([(44, 34), (47, 33)], fill="#ffb3ac", width=2)
    return img


def litterer(frame):
    img = canvas()
    d = ImageDraw.Draw(img)
    step = (0, 2, -1, 0)[frame]
    recoil = 3 if frame == 3 else 0
    polygon(d, [(19, 34), (48, 33), (55, 38), (54, 45), (47, 49), (21, 49), (15, 44)], "#344056")
    for x in (22, 31, 40, 49):
        d.rectangle((x + step, 42, x + step + 4, 46), fill="#6d7284")
        d.point((x + step + 2, 43), fill="#afafb0")
    # The tank cap, pressure gauge and hose sit above the wheeled chassis.
    polygon(d, [(36, 15), (42, 13), (51, 15), (55, 20), (54, 35), (49, 39), (37, 37), (34, 32), (34, 20)],
            "#478780")
    polygon(d, [(37, 19), (42, 16), (49, 17), (51, 20), (50, 27), (37, 29)],
            "#78b9a0", None)
    d.rectangle((41, 10, 47, 15), fill="#e29b66", outline=INK)
    d.line([(36, 31), (53, 28)], fill=INK, width=3)
    d.ellipse((44, 19, 53, 28), fill="#efd0a4", outline=INK)
    d.line([(48, 24), (51, 21 if frame == 3 else 23)], fill="#9d4e59", width=2)
    d.line([(44, 36), (39, 42), (27, 40), (25, 34)], fill=INK, width=5, joint="curve")
    d.line([(44, 36), (39, 41), (28, 39), (26, 33)], fill="#a56562", width=2)
    polygon(d, [(16, 23), (22, 19), (32, 20), (37, 25), (36, 34), (31, 39), (17, 37), (14, 32)], "#925868")
    polygon(d, [(17, 25), (22, 21), (30, 22), (33, 25), (22, 27), (17, 30)], "#cc796f", None)
    barrel = [(5, 23), (14, 22), (25, 25), (26, 31), (14, 34), (5, 32), (3, 29)]
    polygon(d, [(x + recoil, y) for x, y in barrel], "#66778b")
    d.line([(7 + recoil, 25), (14 + recoil, 24), (23 + recoil, 26)], fill="#a6b9bc", width=2)
    d.rectangle((5 + recoil, 26, 8 + recoil, 31), fill=INK)
    d.rectangle((27, 25, 31, 28), fill="#ffdb8a")
    d.point((27, 25), fill="#fff1c1")
    if frame == 3:
        polygon(d, [(1, 29), (5, 27), (2, 25), (7, 26), (9, 28), (6, 31), (2, 32)],
                "#a4f0d6", None)
    return img


def capped_can(frame):
    img = canvas()
    d = ImageDraw.Draw(img)
    bob = (0, -2, -1, 1)[frame]
    kick = (0, 2, 0, -2)[frame]
    polygon(d, [(44, 35 + bob), (53, 31 + bob + kick), (59, 34 + bob + kick),
                (55, 40 + bob), (60, 43 + bob - kick), (55, 46 + bob - kick), (44, 44 + bob)], "#4d7470")
    polygon(d, [(9, 36 + bob), (18, 29 + bob), (40, 29 + bob), (54, 37 + bob),
                (49, 44 + bob), (40, 50 + bob), (22, 50 + bob), (10, 45 + bob), (5, 41 + bob)], "#63867c")
    polygon(d, [(7, 41 + bob), (22, 43 + bob), (38, 42 + bob), (48, 39 + bob),
                (44, 45 + bob), (38, 49 + bob), (22, 49 + bob), (12, 45 + bob)], "#3b6264", None)
    # A bright screw cap and a broad orange brim keep the armor legible above the waterline.
    polygon(d, [(13, 35 + bob), (17, 19 + bob), (22, 15 + bob), (37, 13 + bob),
                (45, 17 + bob), (49, 31 + bob), (54, 35 + bob), (49, 39 + bob), (10, 39 + bob)], "#cf733b")
    polygon(d, [(18, 30 + bob), (21, 20 + bob), (25, 17 + bob), (37, 16 + bob),
                (41, 19 + bob), (32, 22 + bob), (26, 31 + bob)], "#ffc26e", None)
    polygon(d, [(38, 23 + bob), (45, 20 + bob), (48, 32 + bob),
                (36, 33 + bob)], "#9e463c", None)
    d.rounded_rectangle((23, 8 + bob, 37, 16 + bob), 2, fill="#efd87a", outline=INK)
    for x in (26, 30, 34):
        d.line((x, 11 + bob, x, 14 + bob), fill="#a96842")
    d.line((12, 35 + bob, 47, 34 + bob), fill="#ffab55", width=2)
    d.rectangle((12, 38 + bob, 24, 43 + bob), fill=INK)
    d.rectangle((13, 39 + bob, 17, 41 + bob), fill="#fff3bf")
    d.rectangle((20, 39 + bob, 23, 41 + bob), fill="#fff3bf")
    d.line((37, 44 + bob, 45, 42 + bob), fill="#96b3a1", width=2)
    return img


def clutter(frame):
    img = canvas()
    d = ImageDraw.Draw(img)
    flutter = (0, -1, -3, 1)[frame]
    polygon(d, [(7, 39), (18, 31), (30, 34), (41, 31), (54, 36), (58, 42),
                (50, 47), (34, 48), (20, 46), (10, 47)], "#534e53")
    # Creased plastic, a bent metal tab and an open loop of discarded cord.
    polygon(d, [(9, 40), (18, 27 + flutter), (24, 29), (31, 36), (25, 43), (14, 45)], "#7f7776")
    polygon(d, [(11, 39), (18, 29 + flutter), (23, 30), (20, 36), (28, 37), (19, 42)], "#ac9c93", None)
    polygon(d, [(23, 38), (27, 26), (35, 23), (41, 27), (44, 40), (33, 46)], "#8a796c")
    polygon(d, [(28, 29), (35, 25), (39, 28), (37, 35), (31, 35)], "#aa9280", None)
    d.line([(29, 38), (35, 39), (39, 36)], fill="#514952", width=2)
    polygon(d, [(37, 37), (43, 32), (51, 33), (55, 40), (50, 44), (40, 44)], "#778080")
    d.line([(43, 35), (49, 35), (52, 38)], fill="#afb5a9", width=2)
    d.line([(13, 43), (17, 36), (25, 35), (31, 41), (37, 44), (46, 42), (49, 37),
            (46, 29 + flutter), (51, 26 + flutter), (56, 29 + flutter)], fill=INK, width=4, joint="curve")
    d.line([(13, 43), (17, 36), (25, 35), (31, 41), (37, 44), (46, 42), (49, 37),
            (46, 29 + flutter), (51, 26 + flutter), (56, 29 + flutter)], fill="#938787", width=2, joint="curve")
    return img


def remove_shadow(img):
    # The source roster baked its contact rings into the last rows of each tile.
    # Remove only translucent pixels. Solid silhouettes and their dark outlines stay intact.
    px = img.load()
    for y in range(40, img.height):
        for x in range(img.width):
            if 0 < px[x, y][3] < 220:
                px[x, y] = (0, 0, 0, 0)
    return img


def build_roster():
    source = Image.open(ASSETS / "RosterV2.png").convert("RGBA")
    roster = Image.new("RGBA", (source.width, CELL * ROWS))
    fog = Image.new("RGBA", (CELL, CELL * ROWS))
    bot = Image.new("RGBA", (CELL * FRAMES, CELL))
    authored = {0: duck, 1: turtle, 2: otter, 3: fish, 4: squid, 45: litterer, 46: clutter, 47: capped_can}
    for row in range(ROWS):
        for frame in range(FRAMES):
            box = (frame * CELL, row * CELL, (frame + 1) * CELL, (row + 1) * CELL)
            if row in authored:
                tile = authored[row](frame)
            elif row in (20, 21, 22, 23, 24, 33):
                tile = hostile(row, frame)
            elif row - 34 in (0, 1, 2, 7, 10):
                tile = debris(row - 34, frame)
            else:
                tile = remove_shadow(source.crop(box))
            roster.paste(tile, box[:2])
            if frame == 0:
                # Fog uses a neutral silhouette; outlines and eye/label details disappear.
                alpha = tile.getchannel("A").filter(ImageFilter.GaussianBlur(5.3))
                silhouette = Image.new("RGBA", tile.size, "white")
                silhouette.putalpha(alpha)
                fog.paste(silhouette, (0, row * CELL))
    for frame in range(FRAMES):
        bot.paste(robot(frame), (frame * CELL, 0))
    roster.save(ASSETS / "TidalRoster.png")
    fog.save(ASSETS / "TidalSilhouettes.png")
    bot.save(ASSETS / "TidalBioBot.png")


def build_estuary():
    rng = Random(212)
    w, h = 384, 216
    img = Image.new("RGB", (w, h))
    d = ImageDraw.Draw(img)
    bands = ["#27354f", "#354458", "#50546a", "#685a70", "#9a706c", "#b18173", "#d59778"]
    for n, color in enumerate(bands):
        d.rectangle((0, n*12, w, n*12+11), fill=color)
    d.ellipse((291, 32, 323, 64), fill="#f5bd82")
    for _ in range(58):
        x, y = rng.randrange(w), rng.randrange(18, 78)
        d.line((x, y, x + rng.randrange(2, 19), y), fill=rng.choice(bands[2:6]))
    for color, y, step in [("#655e77", 76, 38), ("#46546e", 80, 24), ("#334c63", 85, 18)]:
        points = [(x, y - rng.randrange(3, 32)) for x in range(0, w + step, step)]
        d.polygon(points + [(w, 93), (0, 93)], fill=color)
    water = ["#337d86", "#2c6e80", "#295f77", "#24566e", "#214863", "#1c405a", "#193753", "#1b3149"]
    for n, color in enumerate(water):
        d.rectangle((0, 86+n*17, w, 102+n*17), fill=color)
    for _ in range(212):
        x, y = rng.randrange(w), rng.randrange(86, h)
        idx = min(7, (y-86)//17)
        d.line((x, y, x + rng.randrange(2, 13), y), fill=water[max(0, idx-rng.choice((0, 1, 1, 2)))])
    for x in (211, 244, 293, 334):
        d.polygon([(x, 89), (x+9, 89), (x-28, 188), (x-53, 174)], fill="#2a6577")
        d.line([(x, 90), (x-50, 178)], fill="#3a7380")
    for x in range(3, w, 22):
        y = 85 + rng.randrange(-1, 2)
        d.line((x, y, x + rng.randrange(5, 19), y), fill=rng.choice(("#94c6a5", "#c4d4a0", "#599f9c")))
    d.polygon([(0, 56), (18, 59), (34, 69), (53, 79), (86, 91), (62, 98), (0, 103)], fill="#263849")
    d.polygon([(0, 59), (22, 65), (44, 80), (79, 91), (45, 93), (14, 83), (0, 83)], fill="#53576a")
    d.polygon([(0, 136), (20, 148), (34, 188), (81, 203), (105, 215), (0, 215)], fill="#22374c")
    d.polygon([(384, 142), (366, 156), (348, 188), (312, 204), (282, 215), (384, 215)], fill="#233951")
    for side in (-1, 1):
        for _ in range(36):
            x = rng.randrange(0, 38) if side < 0 else rng.randrange(345, 384)
            y = rng.randrange(151, 216)
            t = rng.randrange(2, 10)
            d.polygon([(x, y), (x+t, y-3), (x+t+3, y+3), (x+2, y+4)], fill=rng.choice(("#34536a", "#3b5d70", "#29455c")))
        for _ in range(16):
            x = rng.randrange(1, 58) if side < 0 else rng.randrange(330, 381)
            y = rng.randrange(192, 216)
            length = rng.randrange(7, 35)
            color = rng.choice(("#346d68", "#608669", "#3f746f", "#6e8591"))
            d.line([(x, y), (x-side*3, y-length//2), (x, y-length)], fill=color, width=2)
            d.line([(x-side*3, y-length//2), (x-side*8, y-length+3)], fill=color)
    # Timber floodgate and its sea wall lamp in the far corner.
    for x in (7, 29, 50):
        d.rectangle((x, 67, x+3, 103), fill="#273349")
        d.line((x, 68, x, 97), fill="#a18477")
    d.polygon([(0, 70), (48, 70), (63, 75), (9, 79), (0, 77)], fill="#8b6f68")
    d.line([(0, 78), (9, 80), (63, 75)], fill="#574d59", width=2)
    d.line((12, 45, 12, 72), fill="#262e40", width=3)
    d.line((12, 47, 22, 47), fill="#474457", width=2)
    d.rectangle((21, 46, 25, 50), fill="#f7c57c", outline="#594557")
    img.save(ASSETS / "TidalEstuary.png")



def build_combat():
    sheet = Image.new("RGBA", (32 * 5, 32))
    for slot in range(5):
        tile = Image.new("RGBA", (32, 32))
        d = ImageDraw.Draw(tile)
        if slot == 0:  # Duck shot, a short teal water dart.
            polygon(d, [(4, 16), (11, 12), (23, 11), (29, 14), (29, 17),
                        (23, 21), (13, 20)], "#448f95")
            d.line([(9, 15), (16, 13), (24, 13)], fill="#9fe4c8", width=2)
            d.rectangle((23, 14, 26, 16), fill="#ddf5c9")
        elif slot == 1:  # Archerfish standard bubble.
            d.ellipse((9, 7, 27, 25), fill="#215879", outline="#183748", width=2)
            d.arc((10, 8, 26, 24), 135, 355, fill="#84d9de", width=2)
            d.rectangle((14, 11, 17, 13), fill="#ceefde")
            d.point((22, 20), fill="#4594b5")
            d.point((5, 15), fill="#65bfc6")
        elif slot == 2:  # Third shot; warm split tail and a faceted bright core.
            polygon(d, [(2, 10), (11, 12), (16, 8), (22, 9), (30, 15), (29, 18),
                        (23, 23), (16, 24), (10, 20), (2, 21), (7, 16)], "#e5a55f")
            polygon(d, [(11, 15), (17, 11), (23, 12), (28, 16),
                        (22, 20), (16, 21)], "#83d6d3")
            d.polygon([(15, 16), (21, 12), (25, 16), (20, 19)], fill="#fff4be")
            d.line([(4, 11), (9, 13)], fill="#f8d582")
            d.line([(4, 20), (9, 18)], fill="#f8d582")
        elif slot == 3:  # Shell shield, an upright spillway with raised ridges.
            polygon(d, [(4, 26), (6, 15), (11, 7), (16, 4), (22, 7), (27, 16),
                        (28, 26), (23, 29), (9, 29)], "#92565a")
            polygon(d, [(5, 25), (8, 16), (13, 8), (16, 6), (21, 8), (25, 16),
                        (26, 25), (20, 27), (10, 27)], "#d58c71")
            for x in (8, 12, 16, 20, 24):
                d.line([(x, 25), (16 + (x-16)//2, 9)], fill="#ffcf9d", width=2)
            d.line([(7, 25), (16, 27), (25, 25)], fill="#f6b883", width=2)
        else:  # Ballast shield, blue steel with heavy ribs.
            polygon(d, [(5, 9), (9, 5), (23, 5), (27, 9), (26, 24),
                        (21, 29), (11, 29), (6, 24)], "#395775")
            polygon(d, [(7, 10), (11, 7), (22, 7), (25, 10), (24, 22),
                        (20, 25), (12, 25), (8, 22)], "#729eb2")
            polygon(d, [(13, 10), (19, 10), (23, 15), (19, 20), (12, 20),
                        (10, 15)], "#496a87")
            d.line([(9, 9), (20, 9)], fill="#d0eee1", width=2)
            for x in (9, 22):
                d.point((x, 12), fill="#ffca83")
                d.point((x, 21), fill="#9ed8cc")
            d.rectangle((15, 14, 17, 17), fill="#8fe8da")
        sheet.paste(tile, (slot*32, 0))
    sheet.save(ASSETS / "TidalCombat.png")



def build_warning():
    # Solid pixel edges and a translucent face leave the refracted board visible.
    image = Image.new("RGBA", (80, 72))
    d = ImageDraw.Draw(image)
    d.polygon([(35, 3), (44, 3), (78, 61), (78, 69), (2, 69), (2, 61)],
              fill="#341d36", outline="#ff9971", width=3)
    d.polygon([(39, 10), (42, 10), (72, 62), (70, 64), (10, 64), (8, 62)],
              fill=(216, 76, 82, 160), outline="#e26a67", width=2)
    d.polygon([(35, 27), (45, 27), (43, 47), (37, 47)], fill="#ffe3a5")
    d.rectangle((36, 53, 44, 60), fill="#ffe3a5")
    d.line([(15, 50), (35, 14)], fill="#ffc38b", width=2)
    image.save(ASSETS / "TidalWarning.png")


if __name__ == "__main__":
    build_roster()
    build_estuary()
    build_combat()
    build_warning()
