"""Estilo visual 'Quintal Minimalista' do Ervas do Brasil.
Formas chapadas e arredondadas, contorno ameixa suave, paleta terrosa.
Tudo é desenhado em 4x (supersampling) e reduzido no final."""
import math, random
from PIL import Image, ImageDraw, ImageFilter

S = 4  # supersampling

# ---------- paleta ----------
OUT = (58, 40, 52, 255)          # contorno ameixa
SOIL = (128, 88, 58, 255)
SOIL_D = (98, 66, 44, 255)
SOIL_L = (150, 108, 72, 255)
WOOD = (150, 104, 64, 255)
WOOD_D = (112, 76, 46, 255)
CREAM = (244, 232, 206, 255)
SKIN = (156, 102, 66, 255)
SKIN_D = (132, 84, 52, 255)
HAIR = (52, 34, 28, 255)
SCARF = (206, 104, 64, 255)
SCARF_DOT = (236, 186, 84, 255)
APRON = (98, 140, 78, 255)
APRON_D = (78, 116, 62, 255)
SHIRT = (242, 230, 204, 255)
PANTS = (146, 116, 84, 255)
BOOT = (96, 62, 40, 255)
BASKET = (210, 166, 96, 255)
BASKET_D = (176, 132, 70, 255)
TERRA = (196, 98, 62, 255)
BLUE = (72, 112, 160, 255)
WHITE = (250, 246, 236, 255)

def rgba(c, a=255):
    return (c[0], c[1], c[2], a)

def shade(c, f):
    return (max(0, min(255, int(c[0] * f))), max(0, min(255, int(c[1] * f))), max(0, min(255, int(c[2] * f))), c[3] if len(c) > 3 else 255)

def canvas(w, h):
    return Image.new('RGBA', (w * S, h * S), (0, 0, 0, 0))

def finish(im, trim=True, pad=3):
    out = im.resize((im.width // S, im.height // S), Image.LANCZOS)
    if trim:
        bb = out.getchannel('A').point(lambda v: 255 if v > 8 else 0).getbbox()
        if bb:
            out = out.crop(bb)
            c = Image.new('RGBA', (out.width + 2 * pad, out.height + 2 * pad), (0, 0, 0, 0))
            c.paste(out, (pad, pad))
            out = c
    return out

def P(v):
    """escala coordenadas lógicas para o canvas supersampled"""
    return v * S

def poly(d, pts, fill, w=3, outline=OUT):
    pts = [(P(x), P(y)) for x, y in pts]
    d.polygon(pts, fill=fill)
    if w:
        d.line(pts + [pts[0]], fill=outline, width=int(P(w)), joint='curve')

def ell(d, cx, cy, rx, ry, fill, w=3, outline=OUT):
    box = [P(cx - rx), P(cy - ry), P(cx + rx), P(cy + ry)]
    d.ellipse(box, fill=fill, outline=outline if w else None, width=int(P(w)) if w else 0)

def rrect(d, x0, y0, x1, y1, r, fill, w=3, outline=OUT):
    d.rounded_rectangle([P(x0), P(y0), P(x1), P(y1)], int(P(r)), fill=fill, outline=outline if w else None, width=int(P(w)) if w else 0)

def line(d, pts, fill=OUT, w=3):
    d.line([(P(x), P(y)) for x, y in pts], fill=fill, width=int(P(w)), joint='curve')

def stroke_line(d, pts, fill, w):
    """linha com contorno (galhos, caules grossos)"""
    line(d, pts, OUT, w + 3)
    line(d, pts, fill, w)

def leaf_points(x, y, ang, length, width, shape='oval', n=18):
    """contorno de uma folha que nasce em (x,y) apontando para 'ang' (graus; -90 = para cima)"""
    a = math.radians(ang)
    ux, uy = math.cos(a), math.sin(a)
    px, py = -uy, ux
    def half(t):
        if shape == 'round':
            return width * math.sin(math.pi * t) ** 0.55
        if shape == 'lance':
            return width * math.sin(math.pi * t ** 0.85) ** 1.3
        if shape == 'heart':
            return width * (math.sin(math.pi * min(1, t ** 0.6)) ** 0.8)
        if shape == 'blade':
            return width * (1 - t) ** 0.35 * min(1, t * 6 + 0.35)
        if shape == 'thick':
            return width * (1 - t) ** 0.8
        if shape == 'spiky':
            base = width * math.sin(math.pi * t) ** 0.7
            return base * (1 + 0.28 * math.sin(t * math.pi * 7))
        return width * math.sin(math.pi * t) ** 0.8  # oval
    left, right = [], []
    for i in range(n + 1):
        t = i / n
        cx, cy = x + ux * length * t, y + uy * length * t
        h = half(t)
        left.append((cx + px * h, cy + py * h))
        right.append((cx - px * h, cy - py * h))
    return left + right[::-1]

def leaf(d, x, y, ang, length, width, color, shape='oval', w=2.2, rib=True):
    pts = leaf_points(x, y, ang, length, width, shape)
    poly(d, pts, color, w)
    if rib and width > 5 and shape not in ('thick',):
        a = math.radians(ang)
        line(d, [(x, y), (x + math.cos(a) * length * 0.8, y + math.sin(a) * length * 0.8)], shade(color, 0.72), 1.4)

def mound(d, cx, by, w, color=SOIL):
    """montinho de terra (base de todos os estágios)"""
    pts = []
    for i in range(0, 181, 10):
        t = math.radians(180 + i)
        pts.append((cx + w * math.cos(t), by - 4 + w * 0.32 * math.sin(t)))
    pts += [(cx + w, by), (cx - w, by)]
    poly(d, pts, color, 2.5)
    for k in (-0.45, 0.1, 0.5):
        ell(d, cx + w * k, by - 6 - abs(k) * 2, 2.2, 1.4, SOIL_D, 0)

def glow(im, cx, cy, r, color, a=120):
    g = Image.new('RGBA', im.size, (0, 0, 0, 0))
    ImageDraw.Draw(g).ellipse([P(cx - r), P(cy - r), P(cx + r), P(cy + r)], fill=(color[0], color[1], color[2], a))
    g = g.filter(ImageFilter.GaussianBlur(P(r) * 0.45))
    im.alpha_composite(g)

def soft_shadow(im, cx, cy, rx, ry, a=55):
    g = Image.new('RGBA', im.size, (0, 0, 0, 0))
    ImageDraw.Draw(g).ellipse([P(cx - rx), P(cy - ry), P(cx + rx), P(cy + ry)], fill=(40, 28, 36, a))
    im.alpha_composite(g.filter(ImageFilter.GaussianBlur(P(2))))
