#!/usr/bin/env python3
"""Ícone do app: a Vó benzedeira (avó da raizeira) no estilo "Quintal Minimalista".

Uso (na raiz do projeto):
    pip3 install pillow
    python3 tools/arte/icone.py    # salva Assets/Icon/app_icon.png, Assets/Icon/app_icon_macos.png e tools/arte/preview_icone.png

Roda sozinho e NÃO mexe em Assets/Art (o ícone não é Sprite do jogo).
O BuildTools.cs (Configure) usa Assets/Icon/app_icon.png como ícone padrão do app.

O desenho é feito numa grade de 128 unidades (1 unidade = 8 px no ícone final) com as primitivas de estilo.py,
em supersampling, e reduzido para 1024. Saem duas versões:
  app_icon.png        iOS: quadrado opaco e sangrado (o iOS recorta os cantos sozinho).
  app_icon_macos.png  macOS: o sistema NÃO recorta, então o formato vem pronto, na grade da Apple:
                      "squircle" de 824 px centrado na tela de 1024 (100 px de margem), cantos contínuos
                      e sombra suave, como os outros ícones do Dock.
"""
import math, os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SAIDA = os.path.join(ROOT, 'Assets', 'Icon', 'app_icon.png')
SAIDA_MAC = os.path.join(ROOT, 'Assets', 'Icon', 'app_icon_macos.png')
PREVIEW = os.path.join(HERE, 'preview_icone.png')

from PIL import Image, ImageDraw, ImageFilter
import estilo
from estilo import OUT, CREAM, APRON, APRON_D, SKIN_D, WOOD, shade, ell, rrect, line, poly, leaf
from personagens import VO

TAM = 1024          # px do ícone final
U = 128             # grade lógica do desenho
SS = 4              # supersampling
W = 2.2             # contorno (unidades) — mesmo peso visual dos personagens do jogo
CX, HY = 64, 60     # centro da cabeça

FUNDO = APRON                    # verde do avental
HALO = CREAM                     # caiado atrás da Vó
FOLHA = (120, 164, 92, 255)      # folhas chapadas do fundo
FOLHA_D = APRON_D
BOCHECHA = (214, 120, 100, 110)


def P(v):
    return estilo.P(v)


def traco(d, pts, cor=OUT, w=1.6):
    """linha com pontas arredondadas (rugas, armação dos óculos, caule)"""
    line(d, pts, cor, w)
    for x, y in (pts[0], pts[-1]):
        ell(d, x, y, w / 2, w / 2, cor, 0)


def bezier(p0, p1, p2, n=16):
    return [((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0],
             (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]) for t in (i / n for i in range(n + 1))]


def arco_pts(cx, cy, rx, ry, a0, a1, n=24):
    return [(cx + rx * math.cos(math.radians(a0 + (a1 - a0) * i / n)),
             cy + ry * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]


# ---------- fundo ----------
def fundo(d):
    d.rectangle([0, 0, P(U), P(U)], fill=FUNDO)
    # folhas chapadas entrando pelos cantos (decoração; podem ser cortadas pela máscara)
    for x, y, ang, comp, larg, cor in (
            (-4, 30, -20, 34, 10, FOLHA), (-6, 52, 12, 30, 9, FOLHA_D),
            (132, 26, 200, 34, 10, FOLHA_D), (134, 50, 168, 28, 9, FOLHA),
            (10, -6, 70, 26, 8, FOLHA_D), (118, -6, 110, 26, 8, FOLHA),
            (-6, 100, -30, 30, 9, FOLHA), (134, 104, 210, 30, 9, FOLHA)):
        leaf(d, x, y, ang, comp, larg, cor, 'oval', W * 0.8)
    ell(d, CX, HY + 2, 46, 46, HALO, W)


# ---------- Vó ----------
def busto(d, v):
    xale, ponto = v['shawl'], v['shawl_dot']
    # corpo: ombros cobertos pelo xale
    rrect(d, CX - 46, HY + 30, CX + 46, U + 24, 26, xale, W)
    # decote em V da blusa caiada
    poly(d, [(CX - 15, HY + 26), (CX + 15, HY + 26), (CX + 3, HY + 54), (CX - 3, HY + 54)], v['shirt'], W)
    # aba do xale cruzando por cima (volume com um tom mais escuro)
    poly(d, [(CX + 15, HY + 26), (CX + 26, HY + 30), (CX + 6, U + 4), (CX - 6, U + 4), (CX + 3, HY + 54)], xale, W)
    line(d, [(CX + 22, HY + 36), (CX + 6, HY + 72)], shade(xale, 0.8), 1.4)
    for x, y in ((CX - 34, HY + 44), (CX - 22, HY + 58), (CX - 36, HY + 64), (CX + 14, HY + 50),
                 (CX + 34, HY + 46), (CX + 26, HY + 62), (CX - 14, HY + 68), (CX + 40, HY + 64)):
        ell(d, x, y, 2.2, 2.2, ponto, 0)
    # terço no pescoço com a cruz
    contas = bezier((CX - 14, HY + 27), (CX, HY + 62), (CX + 14, HY + 27), 13)
    for x, y in contas[1:-1]:
        ell(d, x, y, 1.7, 1.7, v['bead'], 0)
    cy = HY + 47
    traco(d, [(CX, cy), (CX, cy + 9)], v['bead'], 1.8)
    traco(d, [(CX - 3, cy + 3), (CX + 3, cy + 3)], v['bead'], 1.8)


def nuvem(d, circulos, cor, w=W):
    """grupo de círculos com um contorno só (cabelo fofo)"""
    for x, y, r in circulos:
        ell(d, x, y, r + w / 2, r + w / 2, OUT, 0)
    for x, y, r in circulos:
        ell(d, x, y, r - w / 2, r - w / 2, cor, 0)


def cabeca(d, v):
    pele, cab, cab_d = v['skin'], v['hair'], v['hair_d']
    # coque com grampo de madeira
    grampo = [(CX - 21, HY - 46), (CX + 20, HY - 34)]
    traco(d, grampo, OUT, 4.4); traco(d, grampo, WOOD, 2.2)
    ell(d, CX, HY - 38, 13.5, 11, cab, W)
    d.arc([P(CX - 7.5), P(HY - 44), P(CX + 7.5), P(HY - 33)], 200, 340, fill=cab_d, width=int(P(1.4)))
    # cabelo branco fofo em volta (os cachos da neta, embranquecidos)
    cachos = [(CX + 31 * math.cos(math.radians(a)), HY + 2 + 30 * math.sin(math.radians(a)), r)
              for a, r in ((160, 9), (190, 10), (222, 10), (254, 10), (286, 10), (318, 10), (350, 10), (20, 9))]
    nuvem(d, cachos, cab)
    for x, y, r in cachos[1:-1]:
        d.arc([P(x - r * 0.55), P(y - r * 0.55), P(x + r * 0.55), P(y + r * 0.55)], 200, 290, fill=cab_d, width=int(P(1.3)))
    # rosto
    ell(d, CX, HY, 30, 29, pele, W)
    # cabelo repartido no meio, em duas ondas sobre a testa
    topo = arco_pts(CX, HY, 30.5, 29.5, 180, 360)
    direita = bezier((CX + 30.5, HY + 2), (CX + 12, HY - 1), (CX, HY - 19))
    esquerda = bezier((CX, HY - 19), (CX - 12, HY - 1), (CX - 30.5, HY + 2))
    poly(d, topo + direita + esquerda, cab, 0)
    line(d, direita, OUT, W); line(d, esquerda, OUT, W)
    traco(d, [(CX, HY - 19), (CX, HY - 28)], cab_d, 1.4)
    for side in (-1, 1):
        traco(d, bezier((CX + side * 4, HY - 17), (CX + side * 14, HY - 10), (CX + side * 24, HY - 8), 8), cab_d, 1.3)
    # olhos de pontinho
    for side in (-1, 1):
        ex = CX + side * 11
        ell(d, ex, HY + 6, 3, 3.8, OUT, 0)
        ell(d, ex + 1, HY + 4.6, 1, 1, (255, 255, 255, 230), 0)
        ell(d, CX + side * 19, HY + 16, 5.5, 3.2, BOCHECHA, 0)
    # óculos redondos
    for side in (-1, 1):
        ell(d, CX + side * 11, HY + 6, 7.5, 7.5, None, 1.6)
        d.arc([P(CX + side * 11 - 5), P(HY + 1), P(CX + side * 11 + 5), P(HY + 11)], 200, 250,
              fill=(255, 255, 255, 200), width=int(P(1.1)))
    d.arc([P(CX - 4), P(HY + 3), P(CX + 4), P(HY + 9)], 200, 340, fill=OUT, width=int(P(1.6)))
    # sorriso com as dobrinhas da idade (um tom mais escuro da pele)
    d.arc([P(CX - 6), P(HY + 11), P(CX + 6), P(HY + 21)], 20, 160, fill=OUT, width=int(P(1.9)))
    for side in (-1, 1):
        d.arc([P(CX + side * 8.5 - 2.5), P(HY + 13), P(CX + side * 8.5 + 2.5), P(HY + 19)],
              280 if side < 0 else 200, 340 if side < 0 else 260, fill=SKIN_D, width=int(P(1.2)))


def ramo(d, v):
    """mão erguida benzendo com o ramo de arruda (mão redonda como a dos personagens do jogo)"""
    hx, hy = CX + 32, HY + 38
    # braço: manga da blusa saindo de baixo do xale
    cotovelo, pulso = (CX + 20, U + 12), (hx - 1, hy + 4)
    traco(d, [cotovelo, pulso], OUT, 12 + 2 * W); traco(d, [cotovelo, pulso], v['shirt'], 12)
    ux, uy = cotovelo[0] - pulso[0], cotovelo[1] - pulso[1]
    n = math.hypot(ux, uy); ux, uy = ux / n, uy / n
    bx, by = pulso[0] + ux * 9, pulso[1] + uy * 9            # barra da manga (um tom mais escuro)
    line(d, [(bx - uy * 5.6, by + ux * 5.6), (bx + uy * 5.6, by - ux * 5.6)], shade(v['shirt'], 0.86), 1.6)
    # caule (passa pela mão e sobra um tiquinho embaixo)
    caule = bezier((hx - 2, hy + 9), (hx + 1.5, hy - 22), (hx + 7, hy - 48), 12)
    traco(d, caule, OUT, 3.8); traco(d, caule, (90, 140, 70, 255), 1.6)
    for t, a in ((0.38, -152), (0.46, -28), (0.6, -158), (0.68, -22), (0.82, -150), (0.88, -36)):
        x, y = caule[int(t * 12)]
        leaf(d, x, y, a, 11, 4.6, v['rue'], 'round', 1.6, False)
    for fx, fy in ((hx + 7, hy - 50), (hx + 3, hy - 47), (hx + 10.5, hy - 46)):
        ell(d, fx, fy, 2.4, 2.4, v['rue_flower'], 1.2)
    ell(d, hx, hy, 7.5, 7.5, v['skin'], W)


def desenha(v=VO):
    antigo = estilo.S
    estilo.S = TAM // U * SS
    try:
        im = Image.new('RGBA', (P(U), P(U)), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        fundo(d)
        busto(d, v)
        cabeca(d, v)
        ramo(d, v)
    finally:
        estilo.S = antigo
    return im.resize((TAM, TAM), Image.LANCZOS).convert('RGB')


# grade de ícones do macOS (Big Sur em diante), em px de uma tela de 1024
MAC_CORPO = 824         # lado do squircle
MAC_EXPOENTE = 5.0      # superelipse |x|^n + |y|^n = 1: aproxima os cantos contínuos da Apple
MAC_SOMBRA = (0, 12, 14, 0.32)   # deslocamento x, y, desfoque, opacidade


def mascara_squircle(lado, n=MAC_EXPOENTE, ss=4):
    """máscara L do squircle (superelipse), suavizada por supersampling"""
    big = lado * ss
    r = big / 2
    pts = []
    for i in range(720):
        t = 2 * math.pi * i / 720
        c, s = math.cos(t), math.sin(t)
        pts.append((r + r * math.copysign(abs(c) ** (2 / n), c), r + r * math.copysign(abs(s) ** (2 / n), s)))
    m = Image.new('L', (big, big), 0)
    ImageDraw.Draw(m).polygon(pts, fill=255)
    return m.resize((lado, lado), Image.LANCZOS)


def versao_macos(icone):
    """arte reduzida ao squircle de 824 px, centrada em 1024 com fundo transparente e sombra"""
    margem = (TAM - MAC_CORPO) // 2
    corpo = icone.resize((MAC_CORPO, MAC_CORPO), Image.LANCZOS).convert('RGBA')
    mask = mascara_squircle(MAC_CORPO)
    corpo.putalpha(mask)

    dx, dy, blur, opac = MAC_SOMBRA
    sombra_a = Image.new('L', (TAM, TAM), 0)
    sombra_a.paste(mask.point(lambda a: int(a * opac)), (margem + dx, margem + dy))
    sombra_a = sombra_a.filter(ImageFilter.GaussianBlur(blur))
    out = Image.new('RGBA', (TAM, TAM), (0, 0, 0, 0))
    out.putalpha(sombra_a)
    out.alpha_composite(corpo, (margem, margem))
    return out


def folha_preview(icone, caminho):
    """ícone grande com a máscara arredondada do iOS + tamanhos pequenos para conferir a leitura"""
    bg = (238, 230, 210)
    sheet = Image.new('RGB', (1120, 560), bg)
    m = Image.new('L', (TAM, TAM), 0)
    ImageDraw.Draw(m).rounded_rectangle([0, 0, TAM - 1, TAM - 1], int(TAM * 0.225), fill=255)
    big = Image.composite(icone, Image.new('RGB', (TAM, TAM), bg), m).resize((512, 512), Image.LANCZOS)
    sheet.paste(big, (24, 24))
    x = 560
    for s in (256, 128, 64, 32):
        sheet.paste(icone.resize((s, s), Image.LANCZOS), (x, 24 + (256 - s)))
        x += s + 24
    # 64 px ampliado (como o olho vê no Dock pequeno) para conferir a silhueta
    sheet.paste(icone.resize((64, 64), Image.LANCZOS).resize((256, 256), Image.NEAREST), (560, 290))
    sheet.save(caminho)


def main():
    icone = desenha()
    os.makedirs(os.path.dirname(SAIDA), exist_ok=True)
    icone.save(SAIDA)
    versao_macos(icone).save(SAIDA_MAC)
    folha_preview(icone, PREVIEW)
    print('Ícone iOS:', SAIDA)
    print('Ícone macOS:', SAIDA_MAC)
    print('Preview:', PREVIEW)


if __name__ == '__main__':
    main()
