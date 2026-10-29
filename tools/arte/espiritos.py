"""Assombrações originais: Alma da Lamparina, Visagem da Neblina, Saci-Pererê, Olho-Gordo,
Cuca (bruxa-jacaré de Monteiro Lobato) e Bernúncia (bicho de pano do Boi-de-Mamão) — 2 quadros cada."""
import math
from PIL import Image, ImageChops, ImageDraw, ImageFilter
S=4; W=256*S
import os, sys
def canvas(): return Image.new('RGBA',(W,W),(0,0,0,0))
def down(im): return im.resize((256,256),Image.LANCZOS)
def glow(im, center, r, color, a=140):
    g=canvas(); d=ImageDraw.Draw(g)
    d.ellipse([center[0]-r,center[1]-r,center[0]+r,center[1]+r], fill=color+(a,))
    g=g.filter(ImageFilter.GaussianBlur(r*0.45)); im.alpha_composite(g)
def pts_wave(x0,x1,y,amp,n,phase):
    return [(x0+(x1-x0)*i/n, y+amp*math.sin(i/n*math.pi*4+phase)) for i in range(n+1)]
OUTLINE=(58,40,72,255)

# ---------- Alma da Lamparina (Assombração)
def alma(frame):
    im=canvas(); ph=frame*math.pi
    bob=int(10*S*math.sin(ph))
    cx=128*S; top=40*S+bob
    # corpo: domo + laterais que afinam + barra ondulada
    body=[]
    for i in range(0,181,6):
        a=math.radians(180+i); body.append((cx+70*S*math.cos(a), top+70*S+70*S*math.sin(a)))
    body+= [(cx+78*S, top+150*S)]
    body+= list(reversed(pts_wave(cx-80*S, cx+80*S, top+190*S, 10*S, 24, ph)))[:]
    body+= [(cx-78*S, top+150*S)]
    sh=canvas(); d=ImageDraw.Draw(sh)
    d.ellipse([cx-60*S,222*S,cx+60*S,236*S],fill=(40,30,60,60))
    im.alpha_composite(sh.filter(ImageFilter.GaussianBlur(6*S)))
    glow(im,(cx,top+110*S),72*S,(200,180,255),90)
    d=ImageDraw.Draw(im)
    d.polygon(body, fill=(236,228,248,235), outline=OUTLINE)
    d.line(body+[body[0]], fill=OUTLINE, width=5*S, joint='curve')
    # sombra lateral
    shade=[(cx+30*S,top+20*S),(cx+70*S,top+70*S),(cx+78*S,top+150*S),(cx+60*S,top+185*S),(cx+40*S,top+120*S)]
    sm=Image.new('L',(W,W),0); ImageDraw.Draw(sm).polygon(body,fill=255)
    sl=canvas(); ImageDraw.Draw(sl).polygon(shade, fill=(205,190,232,200))
    im.paste(sl,(0,0),Image.composite(sl,canvas(),sm).getchannel('A'))
    d=ImageDraw.Draw(im)
    d.line(body+[body[0]], fill=OUTLINE, width=5*S, joint='curve')
    # olhos ocos e boquinha
    for ex in (-26,26):
        d.ellipse([cx+(ex-13)*S, top+58*S, cx+(ex+13)*S, top+92*S], fill=(60,42,82,255))
        d.ellipse([cx+(ex-6)*S, top+64*S, cx+(ex)*S, top+72*S], fill=(255,255,255,230))
    d.ellipse([cx-8*S, top+104*S, cx+8*S, top+122*S], fill=(60,42,82,255))
    # bracinho segurando a lamparina
    hx=cx+66*S; hy=top+128*S
    d.line([(cx+46*S,top+112*S),(hx,hy)], fill=OUTLINE, width=14*S)
    d.line([(cx+46*S,top+112*S),(hx,hy)], fill=(236,228,248,255), width=8*S)
    # lamparina
    lx=hx+6*S; ly=hy+8*S
    glow(im,(lx,ly+20*S),34*S,(255,190,60),170)
    d=ImageDraw.Draw(im)
    d.line([(lx,ly-6*S),(lx,ly+4*S)],fill=OUTLINE,width=4*S)
    d.rectangle([lx-16*S,ly+4*S,lx+16*S,ly+34*S],fill=(255,214,120,255),outline=OUTLINE,width=4*S)
    flame_h=14+4*frame
    d.polygon([(lx,ly+(30-flame_h)*S),(lx-7*S,ly+26*S),(lx+7*S,ly+26*S)],fill=(255,120,40,255))
    d.rectangle([lx-18*S,ly+32*S,lx+18*S,ly+40*S],fill=(120,80,50,255),outline=OUTLINE,width=3*S)
    return down(im)

# ---------- Visagem da Neblina
def visagem(frame):
    mist=canvas(); d=ImageDraw.Draw(mist); ph=frame*math.pi
    cx=128*S
    blobs=[(0,70,58),(-30,110,46),(28,118,48),(-8,150,40),(20,178,30),(-14,200,22),(6,218,14)]
    for i,(dx,y,r) in enumerate(blobs):
        sx=dx+8*math.sin(ph+i)
        d.ellipse([cx+(sx-r)*S,(y-r)*S,cx+(sx+r)*S,(y+r)*S], fill=(150,196,192,150))
    # capuz
    d.pieslice([cx-62*S,22*S,cx+62*S,146*S],180,360,fill=(110,160,160,190))
    mist=mist.filter(ImageFilter.GaussianBlur(5*S))
    im=canvas(); im.alpha_composite(mist)
    # rosto oco dentro do capuz
    d=ImageDraw.Draw(im)
    d.ellipse([cx-40*S,52*S,cx+40*S,118*S],fill=(30,48,58,220))
    for ex in (-16,16):
        glow(im,(cx+ex*S,84*S),20*S,(170,255,240),220)
    d=ImageDraw.Draw(im)
    blink= 4 if frame==1 else 9
    for ex in (-16,16):
        d.ellipse([cx+(ex-7)*S,(84-blink)*S,cx+(ex+7)*S,(84+blink)*S],fill=(220,255,250,255))
    # fios de neblina
    for k in range(3):
        pts=[(cx+(-50+k*40+12*math.sin(t/3+ph))*S,(150+t*3)*S) for t in range(0,25)]
        d.line(pts,fill=(200,235,230,120),width=3*S,joint='curve')
    return down(im)

# ---------- Saci
def saci(frame):
    im=canvas(); ph=frame
    hop=(-14*S if frame==1 else 0)+18*S
    cx=128*S
    # redemoinho
    d=ImageDraw.Draw(im)
    for k in range(4):
        r=(30+k*14)*S; y=238*S-k*12*S
        d.arc([cx-r,y-r*0.28,cx+r,y+r*0.28], start=200+frame*40, end=520+frame*40, fill=(150,120,90,200-k*35), width=4*S)
    sk=(107,66,38,255); skd=(84,50,28,255)
    # perna única
    d.line([(cx,150*S+hop),(cx+4*S,205*S+hop)],fill=OUTLINE,width=20*S)
    d.line([(cx,150*S+hop),(cx+4*S,205*S+hop)],fill=sk,width=13*S)
    d.ellipse([cx-10*S,198*S+hop,cx+22*S,214*S+hop],fill=skd,outline=OUTLINE,width=4*S)
    # calção
    d.rounded_rectangle([cx-30*S,126*S+hop,cx+30*S,160*S+hop],10*S,fill=(232,196,120,255),outline=OUTLINE,width=5*S)
    # tronco
    d.rounded_rectangle([cx-26*S,92*S+hop,cx+26*S,134*S+hop],14*S,fill=sk,outline=OUTLINE,width=5*S)
    # braços
    d.line([(cx-24*S,104*S+hop),(cx-48*S,128*S+hop)],fill=OUTLINE,width=13*S); d.line([(cx-24*S,104*S+hop),(cx-48*S,128*S+hop)],fill=sk,width=7*S)
    d.line([(cx+24*S,104*S+hop),(cx+46*S,92*S+hop)],fill=OUTLINE,width=13*S); d.line([(cx+24*S,104*S+hop),(cx+46*S,92*S+hop)],fill=sk,width=7*S)
    # cachimbo
    px=cx+50*S; py=86*S+hop
    d.line([(px-8*S,py+6*S),(px+10*S,py)],fill=(90,55,30,255),width=5*S)
    d.rounded_rectangle([px+6*S,py-14*S,px+20*S,py+4*S],4*S,fill=(120,72,40,255),outline=OUTLINE,width=3*S)
    smoke=canvas(); sd=ImageDraw.Draw(smoke)
    for i in range(5):
        r=(6+i*3)*S; sx=px+(13+6*math.sin(i+frame*2))*S; sy=py-(20+i*14)*S
        sd.ellipse([sx-r,sy-r,sx+r,sy+r],fill=(220,220,220,150-i*25))
    im.alpha_composite(smoke.filter(ImageFilter.GaussianBlur(3*S))); d=ImageDraw.Draw(im)
    # cabeça
    hy=62*S+hop
    d.ellipse([cx-34*S,hy-32*S,cx+34*S,hy+34*S],fill=sk,outline=OUTLINE,width=5*S)
    for ex in (-12,12):
        d.ellipse([cx+(ex-8)*S,hy-6*S,cx+(ex+8)*S,hy+12*S],fill=(255,255,255,255),outline=OUTLINE,width=2*S)
        d.ellipse([cx+(ex-3)*S,hy-1*S,cx+(ex+5)*S,hy+9*S],fill=(40,25,15,255))
    d.arc([cx-14*S,hy+6*S,cx+14*S,hy+24*S],start=20,end=160,fill=OUTLINE,width=4*S)
    # gorro vermelho
    cap=[(cx-36*S,hy-14*S),(cx+36*S,hy-14*S),(cx+20*S,hy-44*S),(cx-6*S,hy-62*S),(cx-44*S,hy-76*S+(6*S if frame else 0)),(cx-20*S,hy-40*S)]
    d.polygon(cap,fill=(214,52,44,255)); d.line(cap+[cap[0]],fill=OUTLINE,width=5*S,joint='curve')
    d.line([(cx-34*S,hy-16*S),(cx+34*S,hy-16*S)],fill=(170,30,30,255),width=6*S)
    return down(im)

# ---------- Olho-Gordo
def olhogordo(frame):
    im=canvas(); cx=128*S; cy=110*S+(6*S if frame else 0)
    glow(im,(cx,cy),78*S,(120,200,90),80)
    d=ImageDraw.Draw(im)
    # asinhas de folha roxa
    for sgn in (-1,1):
        flap=12*S if frame else 0
        leaf=[(cx+sgn*60*S,cy),(cx+sgn*118*S,cy-30*S-flap),(cx+sgn*104*S,cy+10*S),(cx+sgn*120*S,cy+30*S-flap//2),(cx+sgn*64*S,cy+22*S)]
        d.polygon(leaf,fill=(120,72,150,255)); d.line(leaf+[leaf[0]],fill=OUTLINE,width=4*S)
        d.line([(cx+sgn*62*S,cy+8*S),(cx+sgn*110*S,cy-4*S)],fill=(80,45,105,255),width=3*S)
    # globo
    R=66*S
    d.ellipse([cx-R,cy-R,cx+R,cy+R],fill=(250,240,214,255),outline=OUTLINE,width=6*S)
    # veias
    for a in (20,60,200,250,320):
        ar=math.radians(a); d.line([(cx+R*0.95*math.cos(ar),cy+R*0.95*math.sin(ar)),(cx+R*0.6*math.cos(ar+0.2),cy+R*0.6*math.sin(ar+0.2))],fill=(214,120,120,200),width=2*S)
    # íris verde-inveja
    r=34*S; d.ellipse([cx-r,cy-r+4*S,cx+r,cy+r+4*S],fill=(92,180,70,255),outline=(40,90,40,255),width=4*S)
    r=16*S; d.ellipse([cx-r,cy-r+4*S,cx+r,cy+r+4*S],fill=(25,20,30,255))
    d.ellipse([cx-18*S,cy-16*S,cx-6*S,cy-4*S],fill=(255,255,255,240))
    # pálpebra roxa (semicerrada = olhar de inveja)
    lid_y = cy-10*S if frame else cy-26*S
    lid=canvas(); ld=ImageDraw.Draw(lid)
    ld.rectangle([cx-R-10*S,cy-R-10*S,cx+R+10*S,lid_y],fill=(110,62,140,255))
    mask=Image.new('L',(W,W),0); ImageDraw.Draw(mask).ellipse([cx-R,cy-R,cx+R,cy+R],fill=255)
    im.paste(lid,(0,0),Image.composite(lid,canvas(),mask).getchannel('A'))
    d=ImageDraw.Draw(im)
    d.ellipse([cx-R,cy-R,cx+R,cy+R],outline=OUTLINE,width=6*S)
    # cílios-espinhos
    for i in range(-3,4):
        x=cx+i*18*S; y=lid_y-1
        d.polygon([(x-5*S,y),(x+5*S,y),(x+i*4*S,y-22*S)],fill=OUTLINE)
    # rabinho de fumaça
    for k in range(6):
        rr=(10-k)*S; d.ellipse([cx-rr+int(8*S*math.sin(k+frame)),cy+R+k*12*S-rr,cx+rr+int(8*S*math.sin(k+frame)),cy+R+k*12*S+rr],fill=(120,72,150,200-k*30))
    return down(im)


# ---------- utilitários das criaturas do folclore (Cuca, Bernúncia)
def _smooth(pts, it=3):
    """arredonda um polígono fechado (Chaikin) — formas fofas sem cantos duros"""
    for _ in range(it):
        out=[]
        for i in range(len(pts)):
            (x0,y0),(x1,y1)=pts[i],pts[(i+1)%len(pts)]
            out+=[(0.75*x0+0.25*x1,0.75*y0+0.25*y1),(0.25*x0+0.75*x1,0.25*y0+0.75*y1)]
        pts=out
    return pts

def _shape(d, pts, fill, w=5):
    d.polygon(pts, fill=fill)
    if w: d.line(pts+pts[:2], fill=OUTLINE, width=w*S, joint='curve')

def _rot(pts, ox, oy, ang):
    """gira pontos lógicos (x,y) em torno da dobradiça (ox,oy) já em pixels supersampled"""
    c,s=math.cos(math.radians(ang)),math.sin(math.radians(ang))
    return [(ox+(x*c-y*s)*S, oy+(x*s+y*c)*S) for x,y in pts]

def _clip_onto(im, layer, pts):
    """cola 'layer' em 'im' só dentro do polígono pts (estampas de tecido)"""
    m=Image.new('L',(W,W),0); ImageDraw.Draw(m).polygon(pts,fill=255)
    layer.putalpha(ImageChops.multiply(layer.getchannel('A'), m)); im.alpha_composite(layer)

def _ground_shadow(im, cx, rx):
    sh=canvas(); ImageDraw.Draw(sh).ellipse([cx-rx*S,222*S,cx+rx*S,236*S],fill=(40,30,60,60))
    im.alpha_composite(sh.filter(ImageFilter.GaussianBlur(6*S)))

# ---------- Cuca (a de Monteiro Lobato): bruxa-jacaré de cabelo de palha
def cuca(frame):
    im=canvas(); cx=128*S; bob=3*S if frame else 0
    def ps(lst, dx=0, dy=0): return [(cx+(x+dx)*S, (y+dy)*S+bob) for x,y in lst]
    GREEN=(118,170,86,255); GREEN_D=(88,136,66,255); BELLY=(214,220,146,255)
    HAIRC=(242,206,96,255); HAIR_D=(212,168,66,255)
    DRESS=(118,78,150,255); DRESS_D=(94,60,124,255); SHAWL=(156,100,64,255)
    _ground_shadow(im, cx, 64)
    d=ImageDraw.Draw(im)
    # cabeleira de palha caindo pelas costas (atrás de tudo)
    hair=[(-8,20),(-46,20),(-72,40),(-82,74),(-80,110),(-86,146),(-80,174),(-68,164),(-60,184),(-48,166),(-36,182),(-24,164),(-12,150),
          (24,140),(30,162),(44,150),(56,160),(54,124),(40,90),(30,52),(14,24)]
    hair=ps(_smooth(hair,2))
    _shape(d, hair, HAIRC)
    for sx,sy,ex,ey in [(-58,50,-66,160),(-40,70,-46,166),(-70,90,-76,150),(40,100,46,148),(32,110,30,150)]:
        d.line(ps([(sx+(ex-sx)*t/10+3*math.sin(t*0.63), sy+(ey-sy)*t/10) for t in range(11)]), fill=HAIR_D, width=3*S, joint='curve')
    # vestido roxo de bruxa com estrelinhas de cantiga de ninar
    dress=[(-26,102),(26,102),(42,114),(50,150),(64,204),(46,212),(24,204),(4,213),(-16,204),(-36,213),(-56,206),(-64,204),(-50,150),(-42,114)]
    dress=ps(_smooth(dress,2))
    _shape(d, dress, DRESS)
    for x,y,r in [(-30,176,5),(20,186,4),(-6,160,3.5),(38,166,3.5),(-44,196,3.5)]:
        st=[(x+(r if k%2==0 else r*0.45)*math.cos(math.radians(-90+k*36)), y+(r if k%2==0 else r*0.45)*math.sin(math.radians(-90+k*36))) for k in range(10)]
        d.polygon(ps(st), fill=(250,222,110,255))
    # pezinhos de jacaré
    for fx in (-22,22):
        d.ellipse([cx+(fx-13)*S,209*S+bob,cx+(fx+13)*S,222*S+bob], fill=GREEN, outline=OUTLINE, width=4*S)
        for k in (-7,0,7):
            d.ellipse([cx+(fx+k-2)*S,216*S+bob,cx+(fx+k+2)*S,221*S+bob], fill=(250,246,236,255))
    # xale marrom sobre os ombros
    shawl=[(-46,112),(-24,104),(24,104),(46,112),(36,128),(0,150),(-36,128)]
    _shape(d, ps(_smooth(shawl,2)), SHAWL, 4)
    st=[(r*math.cos(math.radians(-90+k*36)), 140+r*math.sin(math.radians(-90+k*36))) for k in range(10) for r in [7 if k%2==0 else 3]]
    _shape(d, ps(st), (250,222,110,255), 2)
    # braço esquerdo segurando o saco (balança)
    d.line(ps([(-40,118),(-58,146),(-62,160)]), fill=OUTLINE, width=19*S, joint='curve')
    d.line(ps([(-40,118),(-58,146),(-62,160)]), fill=DRESS, width=11*S, joint='curve')
    sway=8 if frame else -8
    hx,hy=cx-62*S, 164*S+bob
    sack=_smooth([(-6,2),(6,2),(8,10),(20,22),(20,40),(10,48),(-10,48),(-20,40),(-20,22),(-8,10)],2)
    _shape(d, _rot(sack,hx,hy,sway), (200,160,100,255), 4)
    d.line(_rot([(-8,8),(8,8)],hx,hy,sway), fill=(150,98,64,255), width=4*S)
    moon_c=_rot([(0,30)],hx,hy,sway)[0]
    mr=9*S
    d.ellipse([moon_c[0]-mr,moon_c[1]-mr,moon_c[0]+mr,moon_c[1]+mr], fill=(250,222,110,255))
    d.ellipse([moon_c[0]-mr+5*S,moon_c[1]-mr-2*S,moon_c[0]+mr+5*S,moon_c[1]+mr-2*S], fill=(200,160,100,255))
    d.ellipse([hx-8*S,hy-8*S,hx+8*S,hy+8*S], fill=GREEN, outline=OUTLINE, width=4*S)
    # braço direito erguido, garrinhas de "buuu"
    d.line(ps([(40,118),(60,140),(70,124)]), fill=OUTLINE, width=19*S, joint='curve')
    d.line(ps([(40,118),(60,140),(70,124)]), fill=DRESS, width=11*S, joint='curve')
    for a in (-60,-20,20):
        tip=(70+14*math.cos(math.radians(a-30)), 120+14*math.sin(math.radians(a-30)))
        d.line(ps([(70,122),tip]), fill=OUTLINE, width=9*S); d.line(ps([(70,122),tip]), fill=GREEN, width=4*S)
    d.ellipse(ps([(62,114),(78,130)])[0]+ps([(62,114),(78,130)])[1], fill=GREEN, outline=OUTLINE, width=4*S)
    # cabeça de jacaré: crânio + focinho comprido para a direita
    d.ellipse(ps([(-46,38),(34,108)])[0]+ps([(-46,38),(34,108)])[1], fill=GREEN, outline=OUTLINE, width=5*S)
    low=[(12,88),(52,88),(82,90),(88,96),(80,104),(46,106),(12,104)]
    _shape(d, ps(_smooth(low,2)), BELLY)
    up=[(4,54),(40,58),(72,62),(90,68),(96,78),(88,88),(56,90),(20,92),(4,92)]
    _shape(d, ps(_smooth(up,2)), GREEN)
    # recobre a junção crânio/focinho
    d.ellipse(ps([(-40,44),(28,102)])[0]+ps([(-40,44),(28,102)])[1], fill=GREEN)
    # dentinhos
    for tx in (30,46,62,76):
        d.polygon(ps([(tx-4,89),(tx+4,89),(tx,97)]), fill=(255,252,240,255), outline=OUTLINE)
    for tx in (38,54,70):
        d.polygon(ps([(tx-3,100),(tx+3,100),(tx,93)]), fill=(255,252,240,255), outline=OUTLINE)
    # narinas, escamas e bochecha
    for nx in (82,90):
        d.ellipse(ps([(nx-2.5,68),(nx+2.5,73)])[0]+ps([(nx-2.5,68),(nx+2.5,73)])[1], fill=(50,70,40,255))
    for sx,sy in [(-26,70),(-14,62),(-30,86),(44,66),(58,70)]:
        d.ellipse(ps([(sx-4,sy-3),(sx+4,sy+3)])[0]+ps([(sx-4,sy-3),(sx+4,sy+3)])[1], fill=GREEN_D)
    d.ellipse(ps([(0,84),(16,94)])[0]+ps([(0,84),(16,94)])[1], fill=(236,140,130,170))
    # franjinha de palha caindo na testa
    fr=[(-44,58),(-34,40),(-14,34),(4,40),(-6,50),(-16,46),(-24,56),(-34,52)]
    _shape(d, ps(_smooth(fr,2)), HAIRC, 4)
    # olhos amarelos em calombos no alto da cabeça (piscam no quadro 1)
    for ex,ey,r in [(-28,41,15),(6,36,14)]:
        d.ellipse(ps([(ex-r,ey-r),(ex+r,ey+r)])[0]+ps([(ex-r,ey-r),(ex+r,ey+r)])[1], fill=GREEN, outline=OUTLINE, width=5*S)
        if frame:
            d.arc(ps([(ex-9,ey-6),(ex+9,ey+6)])[0]+ps([(ex-9,ey-6),(ex+9,ey+6)])[1], start=10, end=170, fill=OUTLINE, width=4*S)
        else:
            ri=r-5
            d.ellipse(ps([(ex-ri,ey-ri),(ex+ri,ey+ri)])[0]+ps([(ex-ri,ey-ri),(ex+ri,ey+ri)])[1], fill=(250,214,60,255), outline=OUTLINE, width=2*S)
            d.ellipse(ps([(ex,ey-6),(ex+6,ey+6)])[0]+ps([(ex,ey-6),(ex+6,ey+6)])[1], fill=(30,22,34,255))
            d.ellipse(ps([(ex-4,ey-6),(ex,ey-2)])[0]+ps([(ex-4,ey-6),(ex,ey-2)])[1], fill=(255,255,255,240))
    return down(im)

# ---------- Bernúncia (Boi-de-Mamão de Florianópolis): bicho de pano que engole criança
def bernuncia(frame):
    im=canvas(); ph=frame*math.pi
    RED=(214,64,56,255); YEL=(242,196,72,255); BLU=(70,112,180,255)
    HEAD=(66,160,176,255); HEAD_D=(48,128,142,255); CREAMY=(255,252,240,255)
    _ground_shadow(im, 132*S, 100)
    d=ImageDraw.Draw(im)
    # corpo comprido de pano (ondula entre os quadros)
    u=6 if frame else -6
    body=[(28,174),(46,150),(72,118+u),(104,96-u),(136,100+u),(160,108),(178,140),(178,196),
          (46,196),(34,188)]
    body=[(x*S,y*S) for x,y in _smooth(body,3)]
    # pernas de quem dança lá dentro (pisam alternado)
    for i,lx in enumerate((60,84,122,146)):
        lift=-5 if (i+frame)%2 else 0
        y0=184; y1=212+lift
        d.line([(lx*S,y0*S),(lx*S,y1*S)], fill=OUTLINE, width=15*S)
        d.line([(lx*S,y0*S),(lx*S,y1*S)], fill=(70,82,124,255), width=8*S)
        d.rounded_rectangle([(lx-7)*S,(y1-2)*S,(lx+13)*S,(y1+8)*S], 4*S, fill=(96,62,40,255), outline=OUTLINE, width=4*S)
    # pompom de fitas na ponta do rabo
    tx,ty=32*S,176*S
    for a,c in [(196,RED),(164,BLU),(226,YEL)]:
        a=math.radians(a+(12 if frame else 0)); e=(tx+20*S*math.cos(a), ty+20*S*math.sin(a))
        d.line([(tx,ty),e], fill=OUTLINE, width=11*S); d.line([(tx,ty),e], fill=c, width=5*S)
    # estampa de chita: faixas vermelho/amarelo/azul com bolinhas
    pat=canvas(); pd=ImageDraw.Draw(pat)
    cols=[RED,YEL,BLU]
    for k in range(-2,14):
        x0=k*22; c=cols[k%3]
        pd.polygon([((x0+0)*S,0),((x0+22)*S,0),((x0+22-30)*S,W),((x0-30)*S,W)], fill=c)
        dot=CREAMY if c!=YEL else RED
        for j in range(0,256,22):
            xx=x0+11-j*30/256; yy=j+ (11 if k%2 else 0)
            pd.ellipse([(xx-3.5)*S,(yy-3.5)*S,(xx+3.5)*S,(yy+3.5)*S], fill=dot)
    _clip_onto(im, pat, body)
    d=ImageDraw.Draw(im)
    d.line(body+body[:2], fill=OUTLINE, width=5*S, joint='curve')
    # barrado franjado na barra do pano
    for x in range(42,180,10):
        d.polygon([(x*S,192*S),((x+10)*S,192*S),((x+5)*S,(200+(2 if (x//10+frame)%2 else 0))*S)], fill=YEL, outline=OUTLINE)
    # cabeça: bocarra escancarada (abre/fecha entre os quadros)
    a_up,a_dn=(-36,28) if frame==0 else (-20,14)
    hx,hy=152*S,132*S
    # crina de fitas atrás da cabeça, caindo sobre o pescoço
    for i,c in enumerate([RED,YEL,BLU]):
        base=_rot([(-14+i*12,-50-i*5)],hx,hy,a_up)[0]
        pts=[(base[0]-k*6*S, base[1]-k*6*S+3*S*math.sin(k*1.4+ph+i)) for k in range(5)]
        d.line(pts, fill=OUTLINE, width=12*S, joint='curve'); d.line(pts, fill=c, width=6*S, joint='curve')
    upper=_smooth([(-28,8),(-30,-26),(-12,-54),(18,-62),(50,-56),(74,-38),(86,-16),(84,-2),(60,2),(20,2),(0,8)],2)
    lower=_smooth([(-20,2),(20,2),(56,2),(78,0),(84,10),(78,24),(52,34),(16,34),(-14,26)],2)
    lip_up=[(0,4),(30,2),(60,2),(82,0)]; lip_dn=[(82,2),(56,4),(26,4),(0,4)]
    mouth=_rot(lip_up,hx,hy,a_up)+_rot(lip_dn,hx,hy,a_dn)+[(hx-8*S,hy)]
    _shape(d, mouth, (150,34,50,255))
    th=_rot([(18,0)],hx,hy,(a_up+a_dn)/2)[0]
    d.ellipse([th[0]-14*S,th[1]-12*S,th[0]+14*S,th[1]+12*S], fill=(96,20,40,255))
    _shape(d, _rot(_smooth([(14,0),(40,-10),(66,-6),(70,2),(40,6),(14,4)],2),hx,hy,a_dn), (236,120,140,255), 3)
    _shape(d, _rot(lower,hx,hy,a_dn), HEAD)
    _shape(d, _rot(upper,hx,hy,a_up), HEAD)
    # dentes redondinhos de pano
    for t in (30,50,68):
        d.polygon(_rot([(t-6,0),(t+6,0),(t,12)],hx,hy,a_up), fill=CREAMY, outline=OUTLINE, width=2*S)
    for t in (38,60):
        d.polygon(_rot([(t-6,4),(t+6,4),(t,-8)],hx,hy,a_dn), fill=CREAMY, outline=OUTLINE, width=2*S)
    # bolinhas no focinho e narina
    for x,y in [(30,-30),(56,-36),(64,-16),(40,-14)]:
        c=_rot([(x,y)],hx,hy,a_up)[0]; d.ellipse([c[0]-4*S,c[1]-4*S,c[0]+4*S,c[1]+4*S], fill=YEL)
    for x,y in [(40,20),(64,16)]:
        c=_rot([(x,y)],hx,hy,a_dn)[0]; d.ellipse([c[0]-4*S,c[1]-4*S,c[0]+4*S,c[1]+4*S], fill=YEL)
    c=_rot([(78,-26)],hx,hy,a_up)[0]; d.ellipse([c[0]-3*S,c[1]-2*S,c[0]+3*S,c[1]+3*S], fill=HEAD_D)
    # olhos esbugalhados (as pupilas chacoalham)
    for i,(ex,ey,r) in enumerate([(4,-50,14),(32,-58,15)]):
        c=_rot([(ex,ey)],hx,hy,a_up)[0]
        d.ellipse([c[0]-r*S,c[1]-r*S,c[0]+r*S,c[1]+r*S], fill=CREAMY, outline=OUTLINE, width=5*S)
        po=[(4,4),(-3,5)][frame] if i==0 else [(5,2),(2,6)][frame]
        d.ellipse([c[0]+(po[0]-6)*S,c[1]+(po[1]-6)*S,c[0]+(po[0]+6)*S,c[1]+(po[1]+6)*S], fill=(30,22,34,255))
        d.ellipse([c[0]+(po[0]-4)*S,c[1]+(po[1]-5)*S,c[0]+(po[0]-1)*S,c[1]+(po[1]-2)*S], fill=(255,255,255,240))
    return down(im)

def generate(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    for name, fn in [('alma', alma), ('visagem', visagem), ('saci', saci), ('olhogordo', olhogordo),
                     ('cuca', cuca), ('bernuncia', bernuncia)]:
        for f in (0, 1):
            fn(f).save(os.path.join(out_dir, f'spirit_{name}_{f}.png'))
