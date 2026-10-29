"""Raizeira e aldeões no estilo Quintal Minimalista (chibi, 3 direções x 3 quadros)."""
import math, os
from estilo import *

W, H = 120, 200
CX, FEET = 60, 192

RAIZEIRA = dict(skin=SKIN, hair=HAIR, shirt=SHIRT, apron=APRON, pants=PANTS, boot=BOOT,
                hat='scarf', scarf=SCARF, scarf_dot=SCARF_DOT, braid=True, basket=True, curls=True)

# Um visual por aldeão, com a mesma chave de Village.Ids (Core/Village.cs): o jogo põe npc_{id}.png em quem tem o pedido.
# Antes eram 3 visuais presos ao lugar na vila, e o Tonhão da Venda chegava a aparecer com o da Benzedeira.
ALDEOES = {
    'ze': dict(skin=(118, 78, 52, 255), hair=(70, 60, 56, 255), shirt=(96, 132, 176, 255), apron=None, pants=(110, 96, 80, 255),
               boot=(80, 56, 40, 255), hat='straw', beard=(200, 196, 190, 255), basket=False),                     # Seu Zé do Engenho
    'cida': dict(skin=(140, 92, 62, 255), hair=(60, 44, 36, 255), shirt=(120, 160, 90, 255), apron=(250, 236, 210, 255),
                 pants=(120, 160, 90, 255), boot=(110, 80, 56, 255), hat='scarf', scarf=(240, 170, 60, 255),
                 scarf_dot=(255, 250, 235, 255), curls=True, dress=True, basket=False),                            # Dona Cida, da feira
    'tonhao': dict(skin=(150, 100, 70, 255), hair=(34, 26, 22, 255), shirt=(176, 64, 52, 255), apron=None,
                   pants=(62, 62, 74, 255), boot=(60, 44, 34, 255), hat='short', mustache=(34, 26, 22, 255), basket=False),  # Tonhão da Venda
    'maria': dict(skin=(186, 130, 92, 255), hair=(214, 210, 204, 255), shirt=(246, 240, 228, 255), apron=None, pants=(246, 240, 228, 255),
                  boot=(120, 90, 60, 255), hat='bun', dress=True, rosary=True, basket=False),                      # Benzedeira Maria
    'juca': dict(skin=(98, 64, 44, 255), hair=(36, 26, 22, 255), shirt=(236, 200, 90, 255), apron=None, pants=(70, 92, 120, 255),
                 boot=(70, 50, 40, 255), hat='cap', capcolor=(200, 70, 56, 255), stripes=(210, 120, 60, 255), basket=False),  # Pescador Juca
    'lucia': dict(skin=(200, 150, 110, 255), hair=(92, 58, 40, 255), shirt=(150, 120, 190, 255), apron=None,
                  pants=(150, 120, 190, 255), boot=(90, 60, 50, 255), hat='bun', glasses=True, dress=True, basket=False),  # Professora Lúcia
}

# Vó benzedeira, avó da raizeira (ícone do app, ver icone.py): mesma pele da neta, cabelo branco em coque,
# óculos redondos, xale de chita no tom do lenço da neta, terço e ramo de arruda. Não entra no gerador do jogo.
VO = dict(skin=SKIN, hair=(222, 218, 210, 255), hair_d=(184, 178, 172, 255), shirt=SHIRT, apron=None, pants=SHIRT,
          boot=BOOT, hat='bun', curls=True, glasses=True, dress=True, rosary=True, basket=False,
          shawl=SCARF, shawl_dot=SCARF_DOT, bead=(110, 62, 44, 255), rue=(110, 164, 150, 255), rue_flower=(240, 204, 70, 255))

def legs(d, look, view, ph):
    if view == 'right':
        for k, s in ((0, -1), (1, 1)):
            off = s * ph * 7
            col = shade(look['pants'], 0.85 if k == 0 else 1)
            rrect(d, CX - 6 + off, 148, CX + 6 + off, FEET - 4, 5, col, 2.2)
            ell(d, CX + 3 + off, FEET - 3, 9, 5, look['boot'], 2.2)
        return
    for k, side in enumerate((-1, 1)):
        lift = (ph * side) * 4
        x0 = CX + side * 10
        rrect(d, x0 - 7, 148, x0 + 7, FEET - 4 - max(0, lift), 5, look['pants'], 2.2)
        ell(d, x0, FEET - 3 - max(0, lift), 9, 5, look['boot'], 2.2)

def torso(d, look, view, ph, arms=True, hold=None):
    sw = ph * 6
    if view == 'right':
        if look.get('braid'):
            for k in range(5):
                ell(d, CX - 14 - k * 0.5, 84 + k * 10, 5, 6, look['hair'], 2)
        if look.get('basket'):
            rrect(d, CX - 30, 118, CX - 8, 142, 6, BASKET, 2.2)
            line(d, [(CX - 26, 124), (CX - 12, 124)], BASKET_D, 1.6)
        rrect(d, CX - 16, 96, CX + 16, 154, 12, look['shirt'], 2.4)
        if look.get('apron'):
            rrect(d, CX + 2, 104, CX + 18, 158, 6, look['apron'], 2.2)
        if look.get('dress'):
            poly(d, [(CX - 18, 120), (CX + 18, 120), (CX + 24, 166), (CX - 22, 166)], look['shirt'], 2.4)
        if arms:
            rrect(d, CX - 5 - sw, 102, CX + 7 - sw, 140, 6, look['shirt'], 2.2)
            ell(d, CX + 1 - sw, 142, 5.5, 5.5, look['skin'], 2)
        return
    # frente / costas
    if look.get('basket') and view == 'down':
        line(d, [(CX - 18, 100), (CX + 30, 136)], BASKET_D, 2.4)
    rrect(d, CX - 24, 96, CX + 24, 154, 13, look['shirt'], 2.4)
    if look.get('stripes'):
        for y in (110, 122, 134):
            line(d, [(CX - 22, y), (CX + 22, y)], look['stripes'], 3)
    if look.get('dress'):
        poly(d, [(CX - 24, 124), (CX + 24, 124), (CX + 32, 168), (CX - 32, 168)], look['shirt'], 2.4)
    if look.get('apron'):
        if view == 'down':
            rrect(d, CX - 17, 100, CX + 17, 162, 7, look['apron'], 2.2)
            rrect(d, CX - 10, 132, CX + 10, 148, 3, APRON_D, 1.6)
            line(d, [(CX - 4, 132), (CX - 4, 124)], (90, 150, 80, 255), 2)
            leaf(d, CX - 4, 126, -110, 10, 4, (120, 170, 90, 255), 'lance', 1.4, False)
        else:
            line(d, [(CX - 16, 104), (CX + 16, 104)], look['apron'], 3)
            ell(d, CX - 6, 118, 6, 4, look['apron'], 1.8)
            ell(d, CX + 6, 118, 6, 4, look['apron'], 1.8)
    if look.get('rosary') and view == 'down':
        for k in range(9):
            a = math.pi * (0.15 + 0.7 * k / 8)
            ell(d, CX + math.cos(a) * 16, 100 + math.sin(a) * 20, 2, 2, (120, 70, 50, 255), 0)
        line(d, [(CX, 120), (CX, 128)], (120, 70, 50, 255), 2)
        line(d, [(CX - 3, 124), (CX + 3, 124)], (120, 70, 50, 255), 2)
    if arms:
        for side in (-1, 1):
            s = sw * side if view == 'down' else -sw * side
            if hold and side == 1:
                continue
            rrect(d, CX + side * 24 - 6, 100 + s, CX + side * 24 + 6, 138 + s, 6, look['shirt'], 2.2)
            ell(d, CX + side * 24, 140 + s, 5.5, 5.5, look['skin'], 2)
    if look.get('basket') and view == 'down':
        rrect(d, CX + 22, 128, CX + 44, 152, 6, BASKET, 2.2)
        line(d, [(CX + 25, 136), (CX + 41, 136)], BASKET_D, 1.6)
        leaf(d, CX + 30, 130, -100, 12, 4, (110, 166, 90, 255), 'lance', 1.4, False)

def head(d, look, view):
    hy = 64
    if view == 'right':
        if look.get('curls') or look['hat'] in ('scarf',):
            ell(d, CX - 10, hy + 6, 22, 24, look['hair'], 2.4)
        ell(d, CX + 2, hy, 26, 27, look['skin'], 2.4)
        ell(d, CX + 27, hy + 6, 3.5, 3, look['skin'], 0)
        ell(d, CX + 14, hy + 2, 2.6, 3.4, OUT, 0)
        ell(d, CX + 12, hy + 12, 4, 2.4, (214, 120, 100, 120), 0)
        d.pieslice([P(CX - 24), P(hy - 26), P(CX + 28), P(hy + 22)], 150, 260, fill=look['hair'])
    else:
        if view == 'down' and look.get('curls'):
            for side in (-1, 1):
                for k in range(3):
                    ell(d, CX + side * (26 + k * 2), hy + k * 10, 8, 8, look['hair'], 2)
        ell(d, CX, hy, 29, 28, look['skin'] if view == 'down' else look['hair'], 2.4)
        if view == 'down':
            for side in (-1, 1):
                ell(d, CX + side * 10, hy + 4, 3, 4, OUT, 0)
                ell(d, CX + side * 10 + 1, hy + 2.6, 1, 1, (255, 255, 255, 220), 0)
                ell(d, CX + side * 17, hy + 13, 5, 3, (214, 120, 100, 110), 0)
            d.arc([P(CX - 6), P(hy + 9), P(CX + 6), P(hy + 18)], 20, 160, fill=OUT, width=int(P(2)))
            if look.get('glasses'):
                for side in (-1, 1):
                    ell(d, CX + side * 10, hy + 4, 6.5, 6.5, None, 1.6)
                line(d, [(CX - 3.5, hy + 3), (CX + 3.5, hy + 3)], OUT, 1.6)
            if look.get('beard'):
                poly(d, [(CX - 20, hy + 8), (CX + 20, hy + 8), (CX + 12, hy + 30), (CX, hy + 34), (CX - 12, hy + 30)], look['beard'], 2)
                d.arc([P(CX - 6), P(hy + 9), P(CX + 6), P(hy + 18)], 20, 160, fill=OUT, width=int(P(2)))
            if look.get('mustache'):
                # bigodão entre o nariz e o sorriso
                for side in (-1, 1):
                    ell(d, CX + side * 6, hy + 10.5, 7, 3.6, look['mustache'], 1.6)
        elif look.get('braid'):
            for k in range(6):
                ell(d, CX, hy + 24 + k * 10, 6, 7, look['hair'], 2)
    # chapéus
    hat = look['hat']
    if hat == 'scarf':
        if view == 'right':
            poly(d, [(CX - 22, hy - 6), (CX - 12, hy - 26), (CX + 10, hy - 30), (CX + 26, hy - 14), (CX + 24, hy - 4)], look['scarf'], 2.4)
            poly(d, [(CX - 22, hy - 4), (CX - 36, hy + 8), (CX - 26, hy + 12)], look['scarf'], 2)
        elif view == 'down':
            poly(d, [(CX - 30, hy - 2), (CX - 22, hy - 24), (CX, hy - 31), (CX + 22, hy - 24), (CX + 30, hy - 2), (CX, hy - 12)], look['scarf'], 2.4)
        else:
            poly(d, [(CX - 30, hy - 2), (CX - 22, hy - 24), (CX, hy - 31), (CX + 22, hy - 24), (CX + 30, hy - 2), (CX + 6, hy + 14), (CX - 6, hy + 14)], look['scarf'], 2.4)
            poly(d, [(CX - 4, hy + 10), (CX - 12, hy + 26), (CX - 2, hy + 22)], look['scarf'], 1.8)
            poly(d, [(CX + 4, hy + 10), (CX + 12, hy + 26), (CX + 2, hy + 22)], look['scarf'], 1.8)
        for (dx, dy) in ((-14, -14), (0, -22), (14, -14), (-4, -6), (8, -8)):
            if view == 'right': dx = dx * 0.7 + 4
            ell(d, CX + dx, hy + dy, 2, 2, look['scarf_dot'], 0)
    elif hat == 'straw':
        ell(d, CX, hy - 16, 44, 10, (226, 196, 120, 255), 2.4)
        rrect(d, CX - 22, hy - 40, CX + 22, hy - 14, 10, (226, 196, 120, 255), 2.4)
        line(d, [(CX - 22, hy - 20), (CX + 22, hy - 20)], (170, 80, 60, 255), 3)
    elif hat == 'bun':
        ell(d, CX, hy - 26, 12, 10, look['hair'], 2.2)
        if view != 'up':
            d.pieslice([P(CX - 29), P(hy - 28), P(CX + 29), P(hy + 10)], 180, 360, fill=look['hair'], outline=OUT, width=int(P(2.4)))
    elif hat == 'short':
        # cabelo curto, sem chapéu: uma calota de cabelo mais baixa que a do coque, e as costeletas
        if view != 'up':
            d.pieslice([P(CX - 29), P(hy - 31), P(CX + 29), P(hy + 3)], 180, 360, fill=look['hair'], outline=OUT, width=int(P(2.4)))
            if view == 'down':
                for side in (-1, 1):
                    rrect(d, CX + side * 25 - 3, hy - 14, CX + side * 25 + 3, hy + 2, 2, look['hair'], 1.6)
    elif hat == 'cap':
        d.pieslice([P(CX - 28), P(hy - 30), P(CX + 28), P(hy + 6)], 180, 360, fill=look['capcolor'], outline=OUT, width=int(P(2.4)))
        if view == 'down':
            ell(d, CX, hy - 11, 22, 5, shade(look['capcolor'], 0.8), 2)

def person(look, view='down', ph=0, hold=None):
    im = canvas(W, H); d = ImageDraw.Draw(im)
    soft_shadow(im, CX, FEET - 1, 26, 6)
    bob = -2 if ph != 0 else 0
    legs(d, look, view, ph)
    torso(d, look, view, ph, hold=hold)
    head(d, look, view)
    if hold == 'sprig':
        rrect(d, CX + 18, 92, CX + 30, 128, 6, look['shirt'], 2.2)
        ell(d, CX + 24, 92, 5.5, 5.5, look['skin'], 2)
        line(d, [(CX + 24, 92), (CX + 30, 60)], (90, 140, 70, 255), 2.4)
        for k, a in enumerate((-140, -40, -120, -60)):
            leaf(d, CX + 26 + k * 1.2, 82 - k * 6, a, 12, 5, (110, 164, 150, 255), 'round', 1.6, False)
        for k in range(3):
            ell(d, CX + 28 + k * 3, 58 + k * 4, 2.6, 2.6, (240, 204, 70, 255), 1)
    if bob:
        im = im.transform(im.size, Image.AFFINE, (1, 0, 0, 0, 1, -bob * S))
    return im

def forage(look):
    im = canvas(140, 200); d = ImageDraw.Draw(im)
    cx = 70
    soft_shadow(im, cx, 190, 34, 6)
    ell(d, cx - 16, 182, 16, 8, look['pants'], 2.2)
    ell(d, cx + 16, 182, 16, 8, look['pants'], 2.2)
    rrect(d, cx - 24, 120, cx + 24, 178, 13, look['shirt'], 2.4)
    rrect(d, cx - 17, 126, cx + 17, 180, 7, look['apron'], 2.2)
    # braço estendido ao chão
    line(d, [(cx + 20, 130), (cx + 42, 176)], OUT, 13); line(d, [(cx + 20, 130), (cx + 42, 176)], look['shirt'], 9)
    ell(d, cx + 44, 178, 5.5, 5.5, look['skin'], 2)
    for a in (-120, -60, -95):
        leaf(d, cx + 50, 186, a, 14, 5, (110, 170, 90, 255), 'lance', 1.5, False)
    rrect(d, cx - 54, 160, cx - 28, 188, 6, BASKET, 2.2)
    look2 = dict(look)
    global CX
    old = CX; CX = cx
    d2 = d
    head_img = canvas(W, H)
    CX = 60
    head(ImageDraw.Draw(head_img), look, 'down')
    CX = old
    head_img = head_img.transform(head_img.size, Image.AFFINE, (1, 0, 0, 0, 1, -24 * S))
    im.alpha_composite(head_img, (P(10), 0))
    return im

def grind(look):
    im = person(look, 'down', 0)
    d = ImageDraw.Draw(im)
    # pilão na frente
    poly(d, [(CX - 22, 132), (CX + 22, 132), (CX + 16, 160), (CX - 16, 160)], WOOD, 2.4)
    ell(d, CX, 132, 22, 6, WOOD_D, 2.2)
    ell(d, CX, 132, 16, 3.5, (110, 160, 90, 255), 0)
    stroke_line(d, [(CX + 4, 130), (CX + 18, 100)], WOOD, 5)
    for side in (-1, 1):
        ell(d, CX + side * 18, 128, 5.5, 5.5, look['skin'], 2)
    for k in range(5):
        ell(d, CX - 20 + k * 10, 120 - (k % 2) * 8, 1.8, 1.8, (150, 200, 110, 230), 0)
    return im

def generate(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    for view in ('down', 'up', 'right'):
        for i, ph in enumerate((-1, 0, 1)):
            finish(person(RAIZEIRA, view, ph), pad=2).save(os.path.join(out_dir, f'player_{view}_{i}.png'))
    finish(person(RAIZEIRA, 'down', 0, hold='sprig')).save(os.path.join(out_dir, 'player_idle.png'))
    finish(forage(RAIZEIRA)).save(os.path.join(out_dir, 'player_forage.png'))
    finish(grind(RAIZEIRA)).save(os.path.join(out_dir, 'player_grind.png'))
    # retrato: busto ampliado
    big = finish(person(RAIZEIRA, 'down', 0, hold='sprig'), trim=False)
    bust = big.crop((0, 20, W, 160)).resize((W * 3, 140 * 3), Image.LANCZOS)
    bust.save(os.path.join(out_dir, 'player_portrait.png'))
    for nome_id, look in ALDEOES.items():
        finish(person(look, 'down', 0)).save(os.path.join(out_dir, f'npc_{nome_id}.png'))
