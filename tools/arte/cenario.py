"""Estações da casa, feira, ícones dos preparos e o mapa do sítio."""
import math, os, random, json
from estilo import *

# ---------------- props ----------------
def casa():
    im = canvas(260, 220); d = ImageDraw.Draw(im)
    soft_shadow(im, 130, 208, 118, 10)
    rrect(d, 30, 96, 230, 206, 4, (244, 236, 218, 255), 3)               # parede caiada
    rrect(d, 30, 180, 230, 206, 2, (214, 198, 170, 255), 0)               # barrado
    line(d, [(30, 180), (230, 180)], OUT, 2)
    poly(d, [(14, 104), (130, 30), (246, 104)], TERRA, 3)                 # telhado
    for k in range(1, 6):
        y = 30 + k * 13
        line(d, [(130 - (y - 30) * 1.55, y), (130 + (y - 30) * 1.55, y)], shade(TERRA, 0.82), 2)
    rrect(d, 176, 40, 196, 76, 2, (190, 110, 80, 255), 2.5)               # chaminé
    rrect(d, 110, 132, 150, 206, 3, BLUE, 3)                              # porta
    ell(d, 142, 172, 2.4, 2.4, (236, 196, 90, 255), 0)
    for x in (52, 178):                                                   # janelas
        rrect(d, x, 120, x + 30, 154, 2, (250, 238, 200, 255), 2.5)
        rrect(d, x - 10, 120, x, 154, 1, BLUE, 2)
        rrect(d, x + 30, 120, x + 40, 154, 1, BLUE, 2)
    for k in range(4):                                                     # vasinhos de ervas na janela
        ell(d, 56 + k * 7, 156, 3, 2.5, TERRA, 1.4)
        leaf(d, 56 + k * 7, 154, -90 + (k - 1.5) * 15, 8, 3, (100, 160, 90, 255), 'lance', 1.2, False)
    for k in range(3):                                                     # fumacinha
        ell(d, 188 + k * 6, 28 - k * 10, 6 + k * 2, 5 + k * 2, (230, 230, 226, 160 - k * 40), 0)
    return im

def caldeirao():
    im = canvas(120, 120); d = ImageDraw.Draw(im)
    soft_shadow(im, 60, 110, 44, 7)
    for x in (30, 60, 90):
        ell(d, x, 104, 12, 7, (150, 146, 140, 255), 2)
    glow(im, 60, 100, 26, (255, 150, 60), 150); d = ImageDraw.Draw(im)
    poly(d, [(46, 104), (52, 86), (58, 98), (62, 82), (68, 98), (74, 88), (76, 104)], (250, 150, 60, 255), 2)
    poly(d, [(54, 104), (60, 92), (66, 104)], (255, 220, 110, 255), 0)
    d.pieslice([P(16), P(30), P(104), P(110)], 0, 180, fill=(54, 50, 58, 255), outline=OUT, width=int(P(3)))
    ell(d, 60, 70, 46, 10, (70, 66, 74, 255), 3)
    ell(d, 60, 70, 38, 6, (120, 176, 96, 255), 0)
    for k in range(3):
        ell(d, 46 + k * 14, 56 - k * 9, 6 + k, 5 + k, (200, 236, 190, 150 - k * 35), 0)
    return im

def varal():
    im = canvas(160, 130); d = ImageDraw.Draw(im)
    soft_shadow(im, 80, 122, 70, 6)
    for x in (16, 144):
        rrect(d, x - 4, 20, x + 4, 122, 2, WOOD, 2.5)
    line(d, [(16, 30), (80, 40), (144, 30)], (120, 96, 70, 255), 2)
    cores = [(112, 164, 156), (156, 192, 146), (104, 146, 116), (124, 154, 94)]
    for k, x in enumerate((38, 62, 92, 118)):
        y = 36 if k in (1, 2) else 33
        line(d, [(x, y), (x, y + 8)], (180, 150, 100, 255), 1.6)
        for j, a in enumerate((70, 90, 110)):
            leaf(d, x, y + 8, a, 30, 6, rgba(cores[k]), 'lance', 1.6, False)
        rrect(d, x - 5, y + 4, x + 5, y + 10, 2, (200, 60, 50, 255), 1.4)
    return im

def pilao():
    im = canvas(90, 130); d = ImageDraw.Draw(im)
    soft_shadow(im, 45, 122, 30, 6)
    poly(d, [(18, 60), (72, 60), (64, 100), (68, 122), (22, 122), (26, 100)], WOOD, 3)
    line(d, [(26, 100), (64, 100)], WOOD_D, 2)
    ell(d, 45, 60, 27, 7, WOOD_D, 2.5)
    ell(d, 45, 60, 20, 4, (130, 176, 96, 255), 0)
    stroke_line(d, [(50, 58), (66, 12)], WOOD, 7)
    ell(d, 66, 12, 7, 5, WOOD, 2)
    return im

def feira():
    im = canvas(200, 170); d = ImageDraw.Draw(im)
    soft_shadow(im, 100, 162, 90, 8)
    for x in (24, 176):
        rrect(d, x - 4, 40, x + 4, 162, 2, WOOD, 2.5)
    for k in range(6):                                                   # toldo listrado
        x0 = 10 + k * 30
        poly(d, [(x0, 30), (x0 + 30, 30), (x0 + 30, 58), (x0 + 15, 66), (x0, 58)], TERRA if k % 2 == 0 else CREAM, 2.4)
    rrect(d, 14, 112, 186, 136, 3, WOOD, 3)                              # balcão
    rrect(d, 20, 136, 180, 162, 3, WOOD_D, 3)
    for k, col in enumerate([(112, 164, 156), (156, 192, 146), (214, 46, 38), (236, 196, 90)]):
        x = 40 + k * 38
        rrect(d, x - 15, 96, x + 15, 114, 6, BASKET, 2.2)
        for j in range(4):
            ell(d, x - 9 + j * 6, 96 - (j % 2) * 3, 5, 4, rgba(col), 1.5)
    for k, col in enumerate([(200, 140, 60), (120, 170, 110), (150, 100, 170)]):   # garrafinhas
        x = 150 + k * 10
        rrect(d, x - 4, 74, x + 4, 96, 2, rgba(col, 230), 1.8)
        rrect(d, x - 2, 68, x + 2, 75, 1, WOOD, 1.2)
    return im

# ---------------- ícones dos preparos ----------------
def icon_base():
    return canvas(64, 64)

def icon_cha(c):
    im = icon_base(); d = ImageDraw.Draw(im)
    ell(d, 32, 52, 24, 6, CREAM, 2.4)
    d.pieslice([P(12), P(18), P(52), P(56)], 0, 180, fill=CREAM, outline=OUT, width=int(P(2.4)))
    ell(d, 32, 37, 20, 4.5, rgba(c), 2)
    d.arc([P(46), P(28), P(60), P(44)], 270, 90, fill=OUT, width=int(P(2.4)))
    for k in range(2):
        line(d, [(26 + k * 10, 26), (24 + k * 10, 18), (28 + k * 10, 10)], (240, 240, 236, 200), 2)
    leaf(d, 18, 40, -150, 12, 4, (110, 170, 90, 255), 'lance', 1.4, False)
    return im

def icon_garrafa(c):
    im = icon_base(); d = ImageDraw.Draw(im)
    rrect(d, 20, 22, 44, 58, 8, (190, 130, 60, 220), 2.4)
    rrect(d, 27, 10, 37, 24, 2, (190, 130, 60, 220), 2.2)
    rrect(d, 26, 6, 38, 12, 2, WOOD, 2)
    for k in range(3):
        leaf(d, 26 + k * 6, 54, -80 - k * 6, 20, 3.5, rgba(c), 'lance', 1.2, False)
    return im

def icon_elixir(c):
    im = icon_base(); d = ImageDraw.Draw(im)
    glow(im, 32, 42, 18, c, 110); d = ImageDraw.Draw(im)
    ell(d, 32, 42, 17, 16, (230, 240, 236, 200), 2.4)
    d.pieslice([P(15), P(26), P(49), P(58)], 10, 170, fill=rgba(c), outline=None)
    rrect(d, 27, 12, 37, 28, 2, (230, 240, 236, 220), 2.2)
    rrect(d, 26, 8, 38, 14, 2, WOOD, 2)
    for (x, y) in ((22, 20), (46, 24), (44, 14)):
        ell(d, x, y, 1.6, 1.6, (255, 250, 210, 255), 0)
    return im

def icon_xarope(c):
    im = icon_base(); d = ImageDraw.Draw(im)
    rrect(d, 18, 20, 46, 58, 4, (120, 70, 40, 230), 2.4)
    rrect(d, 26, 10, 38, 22, 2, (120, 70, 40, 230), 2.2)
    rrect(d, 24, 6, 40, 12, 2, (230, 230, 226, 255), 2)
    rrect(d, 22, 30, 42, 46, 2, CREAM, 1.8)
    leaf(d, 26, 42, -40, 14, 4, rgba(c), 'heart', 1.2, False)
    return im

def icon_bacia(c):
    im = icon_base(); d = ImageDraw.Draw(im)
    d.pieslice([P(8), P(22), P(56), P(60)], 0, 180, fill=WOOD, outline=OUT, width=int(P(2.4)))
    ell(d, 32, 41, 24, 6, (150, 196, 214, 255), 2.2)
    for k, a in enumerate((-30, -150, -70)):
        leaf(d, 22 + k * 10, 40, a, 12, 4, rgba(c), 'lance', 1.3, False)
    return im

def icon_patua(c):
    im = icon_base(); d = ImageDraw.Draw(im)
    d.arc([P(18), P(4), P(46), P(34)], 200, 340, fill=OUT, width=int(P(2)))
    rrect(d, 18, 24, 46, 54, 8, (200, 56, 48, 255), 2.4)
    line(d, [(20, 30), (44, 30)], (240, 210, 120, 255), 2)
    for k, a in enumerate((-60, -110)):
        leaf(d, 36 + k * 3, 26, a, 14, 5, rgba(c), 'round', 1.3, False)
    return im

def icon_defumacao(c):
    im = icon_base(); d = ImageDraw.Draw(im)
    for k in range(4):
        ell(d, 32 + math.sin(k) * 6, 30 - k * 7, 6 + k, 5 + k, (220, 220, 216, 170 - k * 30), 0)
    d.pieslice([P(12), P(30), P(52), P(60)], 0, 180, fill=TERRA, outline=OUT, width=int(P(2.4)))
    ell(d, 32, 45, 20, 4, (90, 70, 60, 255), 1.8)
    for k in range(3):
        ell(d, 24 + k * 8, 44, 3, 1.6, (255, 150, 60, 255), 0)
    return im

def icon_pote(c):
    im = icon_base(); d = ImageDraw.Draw(im)
    rrect(d, 14, 26, 50, 56, 8, (230, 240, 236, 210), 2.4)
    rrect(d, 12, 18, 52, 28, 3, rgba(c), 2.2)
    rrect(d, 18, 38, 46, 52, 6, rgba(c, 200), 0)
    return im

def icon_cuia(c):
    im = icon_base(); d = ImageDraw.Draw(im)
    stroke_line(d, [(40, 34), (50, 6)], (200, 200, 196, 255), 2.4)
    ell(d, 32, 44, 18, 16, (170, 120, 64, 255), 2.4)
    ell(d, 32, 32, 14, 4, rgba(c), 1.8)
    line(d, [(18, 46), (46, 46)], (130, 86, 46, 255), 2)
    return im

def icon_ramo(c, flower=None):
    im = icon_base(); d = ImageDraw.Draw(im)
    stroke_line(d, [(20, 56), (40, 10)], (90, 130, 70, 255), 2)
    for k in range(5):
        t = k / 5
        x, y = 20 + 20 * t, 56 - 46 * t
        leaf(d, x, y, -150 + (k % 2) * 120, 12, 5, rgba(c), 'round', 1.4, False)
    if flower:
        for k in range(3):
            ell(d, 38 + k * 4, 12 + k * 4, 2.8, 2.8, rgba(flower), 1)
    return im

def icon_pa():
    """pá de cavar canteiros, vendida na feira"""
    im = icon_base(); d = ImageDraw.Draw(im)
    stroke_line(d, [(46, 8), (30, 36)], WOOD, 4)                          # cabo
    stroke_line(d, [(40, 6), (52, 12)], WOOD_D, 4)                        # empunhadura
    poly(d, [(26, 32), (36, 38), (28, 56), (18, 58), (14, 50)], (178, 184, 190, 255), 2.4)   # lâmina
    line(d, [(20, 50), (28, 40)], (228, 232, 236, 255), 1.6)               # brilho
    ell(d, 40, 58, 9, 3, SOIL, 1.6)                                       # terra cavada
    return im

ICON_BY_TYPE = {'Cha': icon_cha, 'Garrafada': icon_garrafa, 'Elixir': icon_elixir, 'Xarope': icon_xarope,
                'Banho': icon_bacia, 'Sorte': icon_bacia, 'Amuleto': icon_patua, 'Defumacao': icon_defumacao,
                'Banir': icon_defumacao, 'Revelar': icon_ramo}

def recipe_icons(data_dir, out_dir):
    plants = {p['id']: p for p in json.load(open(os.path.join(data_dir, 'plants.json')))['plantas']}
    recipes = json.load(open(os.path.join(data_dir, 'recipes.json')))['receitas']
    def hexc(h):
        h = h.lstrip('#'); return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))
    jobs = []
    for p in plants.values():
        if not p.get('remedio'): continue
        eff = p.get('efeitoRemedio') or 'Cura'
        tipo = 'Cha' if eff == 'Cura' else eff
        if p['id'] == 'erva_mate': fn = icon_cuia
        elif p['id'] == 'babosa': fn = icon_pote
        elif p['id'] == 'jambu': fn = lambda c: icon_ramo(c, (242, 196, 50))
        elif p['id'] == 'guarana': fn = icon_cha
        else: fn = ICON_BY_TYPE.get(tipo, icon_cha)
        jobs.append(('remedio_' + p['id'], fn, hexc(p['cor'])))
    for r in recipes:
        main = plants[r['ingredientes'][0]['plantId']]
        jobs.append((r['id'], ICON_BY_TYPE.get(r['tipo'], icon_elixir), hexc(main['cor'])))
    os.makedirs(out_dir, exist_ok=True)
    for rid, fn, col in jobs:
        finish(fn(col), pad=2).save(os.path.join(out_dir, f'icon_{rid}.png'))
    finish(icon_pa(), pad=2).save(os.path.join(out_dir, 'icon_pa.png'))
    return len(jobs) + 1

# ---------------- mapa ----------------
PPU = 48               # pixels por unidade do mundo
WW, WH = 36, 24        # mundo em unidades (igual ao GameManager)

def wx(x): return (x + WW / 2) * PPU
def wy(y): return (WH / 2 - y) * PPU

def mapa():
    rnd = random.Random(42)
    Wpx, Hpx = WW * PPU, WH * PPU
    im = Image.new('RGBA', (Wpx * 2, Hpx * 2), (126, 166, 88, 255))
    k = 2
    d = ImageDraw.Draw(im)
    def E(x, y, rx, ry, fill, w=0):
        d.ellipse([k * (x - rx), k * (y - ry), k * (x + rx), k * (y + ry)], fill=fill, outline=OUT if w else None, width=int(k * w) if w else 0)
    # manchas de grama
    for _ in range(260):
        x, y = rnd.uniform(0, Wpx), rnd.uniform(0, Hpx)
        E(x, y, rnd.uniform(30, 90), rnd.uniform(16, 40), (136, 176, 96, 255) if rnd.random() < 0.6 else (116, 156, 80, 255))
    # mata (canto superior esquerdo)
    mx0, mx1, my0, my1 = wx(-17), wx(-6), wy(11), wy(1.5)
    d.rounded_rectangle([k * mx0, k * my0, k * mx1, k * my1], radius=k * 60, fill=(92, 134, 74, 255))
    # caminhos de terra
    paths = [([(9.5, 2.0), (7, 0), (3, -2.8), (1.2, -3.8)], 56),      # casa → horta
             ([(3, -2.8), (-2, -2.6), (-6, -1.6)], 50),               # → vila
             ([(-6, -1.6), (-8, 0.5), (-9.5, 2.5)], 44),              # → mata
             ([(9.5, 2.0), (14, 1), (17.5, 1.5)], 40)]                # → estrada
    for layer, (col, extra) in enumerate((((170, 130, 86, 255), 8), ((206, 170, 116, 255), 0))):
        for pts, w in paths:
            pts2 = [(k * wx(x), k * wy(y)) for x, y in pts]
            ww = w + extra
            d.line(pts2, fill=col, width=k * ww)
            for x, y in pts2:
                d.ellipse([x - k * ww / 2, y - k * ww / 2, x + k * ww / 2, y + k * ww / 2], fill=col)
    # terreiro da casa e da vila
    E(wx(9.5), wy(2.8), 4.2 * PPU, 1.8 * PPU, (196, 160, 108, 255))
    E(wx(-6), wy(-4.3), 4.6 * PPU, 2.0 * PPU, (196, 160, 108, 255))
    # horta: gramado cercado, com porteira onde chega o caminho da casa. Os 12 canteiros (4x3) são desenhados
    # pelo jogo, um a um, iguais aos que a pá cava pelo sítio; antes havia aqui um terrão listrado que parecia
    # ter mais fileiras do que os canteiros de verdade
    hx0, hx1, hy0, hy1 = wx(-2.3), wx(5.1), wy(-4.3), wy(-9.1)
    d.rounded_rectangle([k * hx0, k * hy0, k * hx1, k * hy1], radius=k * 18, fill=(114, 152, 78, 255))
    def post(x, y):
        d.rectangle([k * (x - 3), k * (y - 26), k * (x + 3), k * y], fill=WOOD, outline=OUT, width=k * 2)
    def cerca_h(x0, x1, y):
        d.line([k * x0, k * (y - 18), k * x1, k * (y - 18)], fill=WOOD_D, width=k * 4)
        for x in range(int(x0), int(x1) + 1, 26):
            post(x, y)
    def cerca_v(x, y0, y1):
        d.line([k * x, k * (y0 - 18), k * x, k * (y1 - 18)], fill=WOOD_D, width=k * 4)
        for y in range(int(y0), int(y1) + 1, 24):
            post(x, y)
    fy0, fy1 = hy0 + 2, hy1 + 18
    cerca_v(hx0 - 20, fy0, fy1)
    cerca_v(hx1 + 20, fy0, fy1)
    cerca_h(hx0 - 20, wx(0.2), fy0)          # porteira entre x = 0.2 e 2.2
    cerca_h(wx(2.2), hx1 + 20, fy0)
    cerca_h(hx0 - 20, hx1 + 20, fy1)
    # lago
    E(wx(13.5), wy(-6.5), 3.2 * PPU, 1.8 * PPU, (110, 164, 196, 255), 3)
    E(wx(13.0), wy(-6.3), 2.2 * PPU, 1.0 * PPU, (140, 190, 214, 255))
    for i in range(5):
        E(wx(11 + i * 1.2), wy(-8.1 - (i % 2) * 0.2), 10, 22, (96, 140, 74, 255), 2)
    # árvores: bordas do mapa e mata (copa redonda chapada)
    def tree(x, y, r):
        E(x + r * 0.15, y + r * 0.85, r * 0.9, r * 0.3, (96, 132, 70, 255))
        d.rectangle([k * (x - r * 0.12), k * (y), k * (x + r * 0.12), k * (y + r * 0.8)], fill=WOOD_D, outline=OUT, width=k * 2)
        E(x, y - r * 0.1, r, r * 0.9, (66, 112, 64, 255), 3)
        E(x - r * 0.3, y - r * 0.35, r * 0.45, r * 0.35, (90, 140, 80, 255))
    spots = []
    for _ in range(30):
        x, y = rnd.uniform(mx0 + 30, mx1 - 30), rnd.uniform(my0 + 30, my1 - 20)
        spots.append((x, y, rnd.uniform(26, 44)))
    for _ in range(70):
        side = rnd.choice('tblr')
        if side == 't': x, y = rnd.uniform(0, Wpx), rnd.uniform(10, 70)
        elif side == 'b': x, y = rnd.uniform(0, Wpx), rnd.uniform(Hpx - 50, Hpx - 5)
        elif side == 'l': x, y = rnd.uniform(5, 60), rnd.uniform(0, Hpx)
        else: x, y = rnd.uniform(Wpx - 60, Wpx - 5), rnd.uniform(0, Hpx)
        spots.append((x, y, rnd.uniform(28, 48)))
    for x, y, r in sorted(spots, key=lambda s: s[1]):
        tree(x, y, r)
    # florzinhas e pedrinhas
    for _ in range(320):
        x, y = rnd.uniform(0, Wpx), rnd.uniform(0, Hpx)
        if mx0 < x < mx1 and my0 < y < my1: continue
        if hx0 - 20 < x < hx1 + 20 and hy0 - 20 < y < hy1 + 30: continue
        c = rnd.choice([(250, 246, 236), (240, 204, 70), (214, 120, 140), (180, 170, 160)])
        E(x, y, 3, 3, rgba(c))
    return im.resize((Wpx, Hpx), Image.LANCZOS)

def generate(art_dir, data_dir):
    os.makedirs(os.path.join(art_dir, 'Environment'), exist_ok=True)
    os.makedirs(os.path.join(art_dir, 'UI', 'Icons'), exist_ok=True)
    for name, fn in (('prop_casa', casa), ('prop_caldeirao', caldeirao), ('prop_varal', varal), ('prop_pilao', pilao), ('prop_feira', feira)):
        finish(fn()).save(os.path.join(art_dir, 'Environment', name + '.png'))
    n = recipe_icons(data_dir, os.path.join(art_dir, 'UI', 'Icons'))
    mapa().save(os.path.join(art_dir, 'Environment', 'map_fazenda.png'))
    return n
