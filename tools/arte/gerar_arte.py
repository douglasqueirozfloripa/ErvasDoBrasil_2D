#!/usr/bin/env python3
"""Gera TODA a arte do Ervas do Brasil no estilo "Quintal Minimalista".

Uso (na raiz do projeto):
    pip3 install pillow
    python3 tools/arte/gerar_arte.py            # regenera Assets/Art
    python3 tools/arte/gerar_arte.py --preview  # também salva tools/arte/preview.png

Cada parte fica num módulo para ajustar com facilidade:
    estilo.py       paleta, contorno e primitivas (folha, montinho, sombra...)
    plantas.py      23 plantas x 5 estágios (SPECS define hábito, folha, cor e flor)
    personagens.py  raizeira (3 direções x 3 quadros + poses) e aldeões
    espiritos.py    Alma da Lamparina, Visagem, Saci, Olho-Gordo, Cuca, Bernúncia
    cenario.py      casa, caldeirão, varal, pilão, feira, ícones dos preparos e mapa
"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
ART = os.path.join(ROOT, 'Assets', 'Art')
DATA = os.path.join(ROOT, 'Assets', 'Resources', 'Data')

import plantas, personagens, espiritos, cenario

def main():
    print('Plantas...');      ids = plantas.generate(os.path.join(ART, 'Plants'))
    print('Personagens...');  personagens.generate(os.path.join(ART, 'Characters'))
    print('Assombrações...'); espiritos.generate(os.path.join(ART, 'Characters'))
    print('Cenário, ícones e mapa...'); n = cenario.generate(ART, DATA)
    print(f'Pronto: {len(ids)} plantas x 5 estágios, {n} ícones de preparo, props, personagens e mapa em {ART}')
    if '--preview' in sys.argv:
        preview(os.path.join(HERE, 'preview.png'))

def preview(path):
    from PIL import Image, ImageDraw
    sheet = Image.new('RGBA', (1600, 900), (238, 230, 210, 255))
    x, y = 20, 20
    def put(fn, h):
        nonlocal x, y
        im = Image.open(fn).convert('RGBA')
        im = im.resize((max(1, int(im.width * h / im.height)), h))
        if x + im.width > 1580: x, y = 20, y + h + 20
        sheet.alpha_composite(im, (x, y)); x += im.width + 12
    for f in ['player_down_1', 'player_up_1', 'player_right_1', 'player_idle', 'player_grind', 'player_forage', 'npc_ze', 'npc_cida', 'npc_tonhao', 'npc_maria', 'npc_juca', 'npc_lucia']:
        put(os.path.join(ART, 'Characters', f + '.png'), 150)
    for f in ['spirit_alma_0', 'spirit_visagem_0', 'spirit_saci_0', 'spirit_olhogordo_0', 'spirit_cuca_0', 'spirit_bernuncia_0']:
        put(os.path.join(ART, 'Characters', f + '.png'), 120)
    x, y = 20, y + 170
    for pid in plantas.SPECS:
        put(os.path.join(ART, 'Plants', f'plant_{pid}_4.png'), 90)
    x, y = 20, y + 110
    for f in ['prop_casa', 'prop_caldeirao', 'prop_varal', 'prop_pilao', 'prop_feira']:
        put(os.path.join(ART, 'Environment', f + '.png'), 150)
    sheet.convert('RGB').save(path)
    print('Preview:', path)

if __name__ == '__main__':
    main()
