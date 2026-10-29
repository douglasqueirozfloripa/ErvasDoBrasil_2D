"""23 plantas x 5 estágios (0 semente, 1 broto, 2 jovem, 3 crescendo, 4 madura)."""
import math, random
from estilo import *

# hábito, forma da folha, cor da folha, flor/fruto, cor da semente, extras
SPECS = {
    'boldo':            dict(habit='bush',   leaf='round', color=(156, 192, 146), flower=('spike', (150, 120, 200)), seed=(150, 90, 60), velvet=True),
    'camomila':         dict(habit='feathery', leaf='needle', color=(112, 164, 84), flower=('daisy', (250, 246, 236)), seed=(214, 190, 140)),
    'mulungu':          dict(habit='tree',   leaf='oval',  color=(102, 152, 74), flower=('tube', (232, 92, 54)), seed=(176, 52, 44)),
    'espada_sao_jorge': dict(habit='clump',  leaf='blade', color=(64, 116, 66), band=(118, 168, 96), edge=(222, 204, 98), flower=None, seed=(96, 140, 70)),
    'arruda':           dict(habit='bush',   leaf='fern',  color=(112, 164, 156), flower=('star', (240, 204, 70)), seed=(120, 90, 60)),
    'guarana':          dict(habit='vine',   leaf='oval',  color=(84, 136, 64), flower=('eyefruit', (200, 48, 40)), seed=(40, 30, 30)),
    'quebra_pedra':     dict(habit='feathery', leaf='tiny', color=(138, 186, 96), flower=('dots', (250, 246, 236)), seed=(170, 150, 100)),
    'erva_cidreira':    dict(habit='bush',   leaf='lance', color=(146, 186, 104), flower=('spike', (186, 150, 214)), seed=(150, 120, 80)),
    'maracuja':         dict(habit='vine',   leaf='heart', color=(76, 136, 64), flower=('passion', (130, 90, 190)), seed=(60, 40, 40)),
    'guaco':            dict(habit='vine',   leaf='heart', color=(88, 148, 74), flower=None, seed=(110, 90, 60)),
    'hortela':          dict(habit='bush',   leaf='spiky', color=(86, 176, 96), flower=None, seed=(120, 90, 60), small=True),
    'espinheira_santa': dict(habit='bush',   leaf='spiky', color=(74, 116, 74), flower=('berry', (200, 70, 50)), seed=(160, 60, 40)),
    'gengibre':         dict(habit='tuft',   leaf='lance', color=(118, 166, 84), flower=None, seed=(214, 178, 110), rhizome=True),
    'babosa':           dict(habit='rosette', leaf='thick', color=(128, 176, 136), flower=None, seed=(150, 170, 130), dots=True),
    'jambu':            dict(habit='bush',   leaf='heart', color=(94, 154, 74), flower=('button', (242, 196, 50)), seed=(140, 110, 70), small=True),
    'capim_santo':      dict(habit='tuft',   leaf='grass', color=(156, 194, 92), flower=None, seed=(190, 170, 110)),
    'carqueja':         dict(habit='wing',   leaf='wing',  color=(124, 154, 94), flower=None, seed=(140, 120, 80)),
    'erva_mate':        dict(habit='tree',   leaf='oval',  color=(64, 124, 56), flower=('berry', (196, 50, 46)), seed=(120, 70, 40), small_tree=True),
    'guine':            dict(habit='bush',   leaf='lance', color=(84, 124, 74), flower=('spike', (250, 246, 236)), seed=(110, 100, 70), small=True),
    'alecrim':          dict(habit='needle', leaf='needle', color=(104, 146, 116), flower=('dots', (120, 150, 220)), seed=(110, 90, 60)),
    'pimenteira':       dict(habit='bush',   leaf='oval',  color=(74, 144, 64), flower=('pepper', (214, 46, 38)), seed=(236, 210, 140)),
    'comigo_ninguem_pode': dict(habit='big', leaf='oval',  color=(78, 140, 72), varieg=(222, 232, 182), flower=None, seed=(90, 120, 70)),
    'manjericao':       dict(habit='bush',   leaf='oval',  color=(70, 160, 72), flower=('spike', (250, 246, 236)), seed=(60, 50, 40), small=True),
}

W, H = 170, 180
CX, BY = 85, 170

def draw_leaf_kind(d, x, y, ang, L, Wd, spec, rnd):
    c = rgba(spec['color'])
    c = shade(c, 0.9 + rnd.random() * 0.2)
    kind = spec['leaf']
    if kind == 'fern':  # arruda: folha recortada (3 lóbulos arredondados)
        a = math.radians(ang)
        for k, off in enumerate((-28, 0, 28)):
            leaf(d, x + math.cos(a) * L * 0.35, y + math.sin(a) * L * 0.35, ang + off, L * (0.55 if off else 0.7), Wd * 0.55, c, 'round', 2, rib=False)
        line(d, [(x, y), (x + math.cos(a) * L * 0.4, y + math.sin(a) * L * 0.4)], shade(c, 0.7), 1.6)
        return
    if kind == 'needle':
        a = math.radians(ang)
        line(d, [(x, y), (x + math.cos(a) * L, y + math.sin(a) * L)], OUT, 4.2)
        line(d, [(x, y), (x + math.cos(a) * L, y + math.sin(a) * L)], c, 2.4)
        return
    if kind == 'tiny':
        leaf(d, x, y, ang, L * 0.5, Wd * 0.4, c, 'oval', 1.6, rib=False)
        return
    if kind == 'grass':
        leaf(d, x, y, ang, L, Wd * 0.35, c, 'blade', 1.8, rib=False)
        return
    if kind == 'wing':
        leaf(d, x, y, ang, L, Wd * 0.45, c, 'lance', 1.8, rib=False)
        return
    shape = {'round': 'round', 'lance': 'lance', 'heart': 'heart', 'blade': 'blade', 'thick': 'thick', 'spiky': 'spiky'}.get(kind, 'oval')
    leaf(d, x, y, ang, L, Wd, c, shape, 1.6 + min(1.0, L / 40))
    if spec.get('velvet'):
        a = math.radians(ang)
        for t in (0.35, 0.6):
            ell(d, x + math.cos(a) * L * t + 2, y + math.sin(a) * L * t - 1, 1.2, 1.2, (230, 240, 225, 160), 0)
    if spec.get('varieg'):
        a = math.radians(ang)
        for t in (0.3, 0.5, 0.7):
            ell(d, x + math.cos(a) * L * t, y + math.sin(a) * L * t, Wd * 0.28, Wd * 0.22, rgba(spec['varieg'], 230), 0)

def draw_flower(d, im, x, y, spec, size, rnd):
    if not spec.get('flower'):
        return
    kind, col = spec['flower']
    col = rgba(col)
    s = size
    if kind == 'daisy':
        for k in range(8):
            a = k * math.pi / 4
            ell(d, x + math.cos(a) * s * 0.9, y + math.sin(a) * s * 0.9, s * 0.55, s * 0.55, col, 1.5)
        ell(d, x, y, s * 0.7, s * 0.7, (240, 196, 60, 255), 1.5)
    elif kind == 'spike':
        for k in range(4):
            ell(d, x, y - k * s * 0.9, s * 0.55 * (1 - k * 0.12), s * 0.5, col, 1.5)
    elif kind == 'star':
        pts = []
        for k in range(10):
            r = s if k % 2 == 0 else s * 0.45
            a = -math.pi / 2 + k * math.pi / 5
            pts.append((x + math.cos(a) * r, y + math.sin(a) * r))
        poly(d, pts, col, 1.5)
    elif kind == 'tube':
        poly(d, [(x - s * 0.4, y), (x + s * 0.4, y), (x + s * 0.8, y - s * 1.8), (x, y - s * 1.4), (x - s * 0.8, y - s * 1.8)], col, 1.8)
    elif kind == 'eyefruit':  # guaraná: fruto vermelho que abre e mostra o "olho"
        ell(d, x, y, s * 0.9, s, col, 1.8)
        ell(d, x, y + s * 0.1, s * 0.55, s * 0.55, WHITE, 0)
        ell(d, x, y + s * 0.15, s * 0.32, s * 0.32, (30, 22, 26, 255), 0)
    elif kind == 'passion':
        for k in range(10):
            a = k * math.pi / 5
            ell(d, x + math.cos(a) * s * 0.85, y + math.sin(a) * s * 0.85, s * 0.45, s * 0.45, WHITE, 1.2)
        ell(d, x, y, s * 0.75, s * 0.75, col, 1.2)
        ell(d, x, y, s * 0.3, s * 0.3, (240, 210, 90, 255), 0)
    elif kind == 'button':
        ell(d, x, y, s * 0.7, s * 0.85, col, 1.6)
        ell(d, x, y - s * 0.5, s * 0.35, s * 0.25, (196, 60, 40, 255), 0)
    elif kind == 'berry':
        for k in range(3):
            ell(d, x + (k - 1) * s * 0.7, y + (k % 2) * s * 0.4, s * 0.45, s * 0.45, col, 1.3)
    elif kind == 'pepper':
        poly(d, [(x - s * 0.35, y), (x + s * 0.35, y), (x + s * 0.15, y + s * 1.7), (x, y + s * 2.0), (x - s * 0.15, y + s * 1.7)], col, 1.6)
        line(d, [(x, y), (x, y - s * 0.4)], (60, 110, 50, 255), 1.6)
    elif kind == 'dots':
        for k in range(3):
            ell(d, x + (k - 1) * s * 0.8, y - (k % 2) * s * 0.6, s * 0.4, s * 0.4, col, 1.1)

def seed_stage(spec):
    im = canvas(W, H); d = ImageDraw.Draw(im)
    mound(d, CX, BY, 46)
    ell(d, CX + 2, BY - 18, 7, 5, rgba(spec['seed']), 2)
    ell(d, CX, BY - 20, 2, 1.2, (255, 255, 255, 140), 0)
    return im

def sprout_stage(spec):
    im = canvas(W, H); d = ImageDraw.Draw(im)
    mound(d, CX, BY, 46)
    c = rgba(spec['color'])
    stroke_line(d, [(CX, BY - 14), (CX, BY - 34)], shade(c, 0.85), 2.2)
    if spec['leaf'] in ('blade', 'grass', 'thick'):
        leaf(d, CX, BY - 14, -92, 30, 5, c, 'blade', 2)
    else:
        leaf(d, CX, BY - 34, -150, 16, 7, c, 'oval', 2)
        leaf(d, CX, BY - 34, -30, 16, 7, c, 'oval', 2)
    return im

def plant_stage(spec, stage, pid):
    rnd = random.Random(hash(pid) % 1000 + stage)
    sc = {2: 0.45, 3: 0.72, 4: 1.0}[stage]
    im = canvas(W, H); d = ImageDraw.Draw(im)
    habit = spec['habit']
    base_y = BY - 10
    mound(d, CX, BY, 40 + 10 * sc)
    c = rgba(spec['color'])
    flowers = []
    small = 0.8 if spec.get('small') else 1.0

    if habit in ('bush', 'big'):
        big = habit == 'big'
        tiers = 2 + int(round(2 * sc))
        L = ((44 if big else 30) * sc + 8) * small
        Wd = ((17 if big else 10) * sc + 4) * small
        if spec['leaf'] == 'round': Wd *= 1.2
        step = (16 if big else 12) * sc + 4
        top_y = base_y - 4 - (tiers - 1) * step
        stroke_line(d, [(CX, base_y), (CX, top_y)], shade(c, 0.75), 2 + sc)
        for t in range(tiers):
            y = base_y - 4 - t * step
            spread = 1 - t / (tiers + 1.5)
            for side in (-1, 1):
                ang = -90 + side * (72 - t * 10) + rnd.uniform(-5, 5)
                draw_leaf_kind(d, CX + side * 2, y, ang, L * (0.65 + 0.45 * spread), Wd * (0.8 + 0.3 * spread), spec, rnd)
                if stage == 4 and t == tiers - 2:
                    a = math.radians(ang)
                    flowers.append((CX + math.cos(a) * L * 0.95, y + math.sin(a) * L * 0.95 - 4))
        draw_leaf_kind(d, CX, top_y, -90 + rnd.uniform(-6, 6), L * 0.75, Wd * 0.85, spec, rnd)
        if stage == 4:
            flowers.append((CX, top_y - L * 0.8))
        if spec['flower'] and spec['flower'][0] == 'spike' and stage == 4:
            flowers = [(CX + dx, top_y - L * 0.55 - abs(dx) * 0.3) for dx in (-18, 0, 18)]
    elif habit == 'feathery':
        stems = int(4 + 4 * sc)
        for i in range(stems):
            ang = -125 + 70 * i / max(1, stems - 1) + rnd.uniform(-6, 6)
            a = math.radians(ang); L = 70 * sc
            x1, y1 = CX + math.cos(a) * L, base_y + math.sin(a) * L
            stroke_line(d, [(CX, base_y), ((CX + x1) / 2 + rnd.uniform(-4, 4), (base_y + y1) / 2), (x1, y1)], shade(c, 0.85), 2)
            for k in range(1, 6):
                t = k / 6
                px, py = CX + (x1 - CX) * t, base_y + (y1 - base_y) * t
                draw_leaf_kind(d, px, py, ang - 50, 14 * sc + 4, 8, spec, rnd)
                draw_leaf_kind(d, px, py, ang + 50, 14 * sc + 4, 8, spec, rnd)
            if stage == 4: flowers.append((x1, y1))
    elif habit == 'needle':
        stems = int(5 + 3 * sc)
        for i in range(stems):
            ang = -130 + 80 * i / max(1, stems - 1)
            a = math.radians(ang); L = 66 * sc
            x1, y1 = CX + math.cos(a) * L, base_y + math.sin(a) * L
            stroke_line(d, [(CX, base_y), (x1, y1)], WOOD, 2.2)
            for k in range(2, 9):
                t = k / 9
                px, py = CX + (x1 - CX) * t, base_y + (y1 - base_y) * t
                draw_leaf_kind(d, px, py, ang - 55, 11, 3, spec, rnd)
                draw_leaf_kind(d, px, py, ang + 55, 11, 3, spec, rnd)
            if stage == 4 and i % 2 == 0: flowers.append((x1, y1 + 6))
    elif habit == 'tree':
        small_tree = spec.get('small_tree')
        th = (110 if not small_tree else 95) * sc
        tw = 7 * sc + 2
        poly(d, [(CX - tw, base_y), (CX + tw, base_y), (CX + tw * 0.6, base_y - th), (CX - tw * 0.6, base_y - th)], WOOD, 2.5)
        for side in (-1, 1):
            stroke_line(d, [(CX, base_y - th * 0.6), (CX + side * 34 * sc, base_y - th * 0.95)], WOOD, 3 * sc + 1)
        cy = base_y - th
        for i in range(int(10 + 14 * sc)):
            ang = rnd.uniform(-180, 0)
            r = rnd.uniform(0, 40 * sc)
            a = math.radians(ang)
            x0, y0 = CX + math.cos(a) * r, cy + math.sin(a) * r * 0.7 + 8 * sc
            draw_leaf_kind(d, x0, y0, rnd.uniform(-170, -10), 22 * sc + 6, 9 * sc + 2, spec, rnd)
            if stage == 4 and i % 4 == 0: flowers.append((x0 + rnd.uniform(-10, 10), y0 - 8))
    elif habit == 'vine':
        ph = 130 * sc + 20
        rrect(d, CX - 4, base_y - ph, CX + 4, base_y + 2, 2, WOOD, 2.2)
        pts = []
        for k in range(30):
            t = k / 29
            pts.append((CX + math.sin(t * math.pi * 3) * 14 * sc, base_y - t * ph * 0.95))
        stroke_line(d, pts, shade(c, 0.8), 2)
        for k in range(5, 30, 5):
            x0, y0 = pts[k]
            side = -1 if math.sin(k / 29 * math.pi * 3) > 0 else 1
            draw_leaf_kind(d, x0, y0, -90 + side * 75, 16 * sc + 7, 9 * sc + 3, spec, rnd)
            if stage == 4 and k % 10 == 5: flowers.append((x0 - side * 14, y0 + 4))
    elif habit == 'clump':  # espada-de-são-jorge
        n = int(4 + 4 * sc)
        for i in range(n):
            t = i / max(1, n - 1)
            ang = -90 + (t - 0.5) * 44 + rnd.uniform(-3, 3)
            L = (60 + 70 * math.sin(math.pi * (0.2 + 0.6 * t))) * sc
            pts = leaf_points(CX + (t - 0.5) * 22 * sc, base_y, ang, L, 9 * sc + 2, 'blade')
            poly(d, pts, c, 2.4)
            a = math.radians(ang)
            for k in range(1, 6):
                tt = k / 6.5
                bx, by_ = CX + (t - 0.5) * 22 * sc + math.cos(a) * L * tt, base_y + math.sin(a) * L * tt
                pa = a + math.pi / 2
                wv = (9 * sc + 2) * 0.7 * (1 - tt) ** 0.35
                line(d, [(bx - math.cos(pa) * wv, by_ - math.sin(pa) * wv), (bx + math.cos(pa) * wv, by_ + math.sin(pa) * wv + 2)], rgba(spec['band']), 2)
            if stage == 4:
                d.line([(P(x), P(y)) for x, y in pts[:len(pts) // 2]], fill=rgba(spec['edge']), width=int(P(1.4)))
    elif habit == 'tuft':
        n = int(6 + 8 * sc)
        for i in range(n):
            t = i / max(1, n - 1)
            ang = -90 + (t - 0.5) * 70 + rnd.uniform(-5, 5)
            draw_leaf_kind(d, CX + (t - 0.5) * 16 * sc, base_y, ang, (70 + 40 * rnd.random()) * sc, 12 * sc + 3, spec, rnd)
        if spec.get('rhizome') and stage >= 3:
            for k in range(3):
                ell(d, CX - 18 + k * 16, BY - 6, 9, 6, rgba(spec['seed']), 2)
    elif habit == 'rosette':  # babosa
        n = int(6 + 6 * sc)
        for i in range(n):
            t = i / max(1, n - 1)
            ang = -170 + 160 * t
            L = (40 + 30 * math.sin(math.pi * t)) * sc
            pts = leaf_points(CX, base_y - 4, ang, L, 10 * sc + 3, 'thick')
            poly(d, pts, shade(c, 0.9 + 0.2 * rnd.random()), 2.4)
            a = math.radians(ang)
            if spec.get('dots'):
                for tt in (0.3, 0.55):
                    ell(d, CX + math.cos(a) * L * tt, base_y - 4 + math.sin(a) * L * tt, 1.4, 1.4, (240, 248, 236, 220), 0)
    elif habit == 'wing':  # carqueja
        n = int(4 + 4 * sc)
        for i in range(n):
            ang = -125 + 70 * i / max(1, n - 1)
            a = math.radians(ang)
            x, y = CX, base_y
            for k in range(5):
                L = 18 * sc + 6
                nx, ny = x + math.cos(a) * L, y + math.sin(a) * L
                leaf(d, x, y, ang, L * 1.05, 6 * sc + 2, shade(c, 0.95 + 0.1 * (k % 2)), 'lance', 1.8, rib=False)
                x, y = nx, ny

    fsize = 6 + 3 * sc
    for (fx, fy) in flowers[:7]:
        draw_flower(d, im, fx, fy, spec, fsize, rnd)
    return im

def generate(out_dir):
    import os
    os.makedirs(out_dir, exist_ok=True)
    for pid, spec in SPECS.items():
        stages = [seed_stage(spec), sprout_stage(spec)] + [plant_stage(spec, s, pid) for s in (2, 3, 4)]
        for i, im in enumerate(stages):
            finish(im).save(os.path.join(out_dir, f'plant_{pid}_{i}.png'))
    return list(SPECS)
