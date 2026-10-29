#!/usr/bin/env python3
"""Trilha ORIGINAL do Ervas do Brasil, sintetizada em código (sem samples, sem músicas de terceiros).

    python3 tools/musica/compor.py      # gera Assets/Audio/Resources/Music/*.ogg (precisa de numpy e ffmpeg)

Faixas (loops perfeitos de 16 compassos):
    trilha_dia    "Quintal ao Sol"          groove inspirado em ijexá, 100 bpm, Ré maior pentatônica
    trilha_feira  "Feira de Raiz Funda"     groove inspirado em baião, 118 bpm, Lá mixolídio
    trilha_noite  "Lamparina na Neblina"    groove inspirado em reggae one-drop, 72 bpm, Mi dórico
"""
import os, subprocess, numpy as np

SR = 32000
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Audio', 'Resources', 'Music')
rng = np.random.default_rng(7)

def hz(midi): return 440.0 * 2 ** ((midi - 69) / 12)

class Track:
    def __init__(self, bpm, bars=16):
        self.bpm = bpm
        self.beat = 60.0 / bpm
        self.n = int(round(bars * 4 * self.beat * SR))
        self.buf = np.zeros(self.n)
    def at(self, beat): return int(round(beat * self.beat * SR))
    def add(self, start_beat, sig, gain=1.0):
        i = self.at(start_beat) % self.n
        idx = (np.arange(len(sig)) + i) % self.n     # cauda volta para o início: loop sem emenda
        np.add.at(self.buf, idx, sig * gain)

def env(n, a=0.005, d=0.3, curve=4.0):
    t = np.arange(n) / SR
    att = np.clip(t / max(a, 1e-4), 0, 1)
    return att * np.exp(-curve * t / max(d, 1e-4))

# ---------------- instrumentos ----------------
def pluck(f, dur=1.2, bright=0.5):          # Karplus-Strong: kalimba/violão
    n = int(dur * SR); p = max(2, int(SR / f))
    buf = rng.uniform(-1, 1, p) * (0.6 + bright * 0.4)
    out = np.zeros(n); y = buf.copy(); k = 0
    for i in range(n):
        v = y[k]; nxt = y[(k + 1) % p]
        y[k] = 0.5 * (v + nxt) * 0.996
        out[i] = v; k = (k + 1) % p
    return out * env(n, 0.002, dur, 2.5)

def kalimba(f, dur=1.4):
    n = int(dur * SR); t = np.arange(n) / SR
    s = np.sin(2 * np.pi * f * t) + 0.25 * np.sin(2 * np.pi * f * 5.4 * t) * np.exp(-30 * t)
    return s * env(n, 0.002, dur * 0.5, 3)

def bass(f, dur=0.5):
    n = int(dur * SR); t = np.arange(n) / SR
    s = np.sin(2 * np.pi * f * t) + 0.3 * np.sin(2 * np.pi * 2 * f * t)
    return np.tanh(1.5 * s) * env(n, 0.006, dur, 2.2)

def kick(dur=0.35, f0=110, f1=45):
    n = int(dur * SR); t = np.arange(n) / SR
    f = f1 + (f0 - f1) * np.exp(-25 * t)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * env(n, 0.001, dur, 6)

def noise(dur, hp=0.0):
    n = int(dur * SR); x = rng.uniform(-1, 1, n)
    if hp > 0:
        x = np.append(x[0], x[1:] - hp * x[:-1])
    return x

def shaker(dur=0.06): return noise(dur, 0.95) * env(int(dur * SR), 0.003, dur, 5) * 0.5
def rim(dur=0.08):
    n = int(dur * SR); t = np.arange(n) / SR
    return (0.6 * np.sin(2 * np.pi * 1700 * t) + 0.5 * noise(dur, 0.9)) * env(n, 0.001, dur, 8)
def hat(dur=0.05): return noise(dur, 0.98) * env(int(dur * SR), 0.001, dur, 9) * 0.18

def bell(f, dur=0.5):                        # agogô
    n = int(dur * SR); t = np.arange(n) / SR
    s = np.sin(2 * np.pi * f * t) + 0.5 * np.sin(2 * np.pi * f * 2.76 * t) + 0.25 * np.sin(2 * np.pi * f * 5.4 * t)
    return s * env(n, 0.001, dur * 0.4, 4) * 0.35

def triangle(dur=0.4, open_=False):
    n = int(dur * SR); t = np.arange(n) / SR
    s = np.sin(2 * np.pi * 3150 * t) + 0.6 * np.sin(2 * np.pi * 4650 * t)
    return s * env(n, 0.001, dur if open_ else dur * 0.15, 3) * 0.12

def reed(f, dur=0.5, vib=5.5):               # sanfona/melódica
    n = int(dur * SR); t = np.arange(n) / SR
    ph = 2 * np.pi * np.cumsum(f * (1 + 0.006 * np.sin(2 * np.pi * vib * t))) / SR
    s = sum((1 / k) * np.sin(k * ph) for k in (1, 2, 3, 4, 5)) + 0.6 * np.sin(ph * 1.003)
    e = np.minimum(1, t / 0.03) * np.minimum(1, (dur - t) / 0.06).clip(0, 1)
    return s * e * 0.35

def organ(freqs, dur=0.18):                  # skank de órgão
    n = int(dur * SR); t = np.arange(n) / SR
    s = sum(np.sin(2 * np.pi * f * t) + 0.5 * np.sin(4 * np.pi * f * t) for f in freqs)
    return s / len(freqs) * env(n, 0.003, dur, 3) * 0.5

def pad(freqs, dur):
    n = int(dur * SR); t = np.arange(n) / SR
    s = sum(np.sin(2 * np.pi * f * t + np.sin(2 * np.pi * 0.3 * t)) for f in freqs) / len(freqs)
    e = np.minimum(1, t / 1.5) * np.minimum(1, (dur - t) / 1.5).clip(0, 1)
    return s * e * 0.25

# ---------------- faixas ----------------
def dia():
    tr = Track(100)
    D, G, Bm, A = (50, 54, 57), (55, 59, 62), (47, 50, 54), (45, 49, 52)
    prog = [D, D, G, G, Bm, Bm, A, A] * 2
    agogo = [(0, 'L'), (1, 'H'), (1.5, 'H'), (2, 'L'), (3, 'H'), (3.5, 'L')]
    for bar in range(16):
        b0 = bar * 4
        for pos, hl in agogo:
            tr.add(b0 + pos, bell(880 if hl == 'H' else 660), 0.8)
        for s in range(16):
            tr.add(b0 + s / 4, shaker(), 0.9 if s % 2 else 0.45)
        for pos in (0, 2, 3.5):
            tr.add(b0 + pos, kick(0.3, 95, 55), 0.7)
        root = prog[bar][0]
        for pos, dur, octv in ((0, 0.9, 0), (1.5, 0.4, 0), (2, 0.9, 0), (3.5, 0.4, 12)):
            tr.add(b0 + pos, bass(hz(root - 12 + octv), dur), 0.55)
        for pos in (0.5, 2.5):
            for m in prog[bar]:
                tr.add(b0 + pos, pluck(hz(m + 12), 0.8, 0.3), 0.12)
    # melodia (graus da pentatônica de Ré), frase A e frase B
    scale = [62, 64, 66, 69, 71, 74, 76, 78, 81]
    A_ = [(0, 4, 1), (1, 3, .5), (1.5, 2, .5), (2, 3, 1), (3, 4, 1), (4, 5, 1.5), (5.5, 4, .5), (6, 3, 2)]
    B_ = [(0, 2, .5), (0.5, 3, .5), (1, 4, 1), (2, 6, 1), (3, 5, .5), (3.5, 4, .5), (4, 3, 1), (5, 2, 1), (6, 0, 2)]
    phrases = [A_, B_, A_, [(p, d + 1 if d < 7 else d, l) for p, d, l in B_]]
    for i, ph in enumerate(phrases * 2):
        base = 8 * (i % 8)
        for pos, deg, l in ph:
            tr.add(base + pos, kalimba(hz(scale[min(deg, 8)] + 12), 1.0 + l * 0.4), 0.32)
    return tr

def feira():
    tr = Track(118)
    A7, D, E7, G = (57, 61, 64, 67), (62, 66, 69), (52, 56, 59, 62), (55, 59, 62)
    prog = [A7, A7, D, D, G, G, E7, E7] * 2
    for bar in range(16):
        b0 = bar * 4
        for pos in (0, 1.5, 2, 3.5):                          # zabumba: grave
            tr.add(b0 + pos, kick(0.3, 120, 60), 0.75 if pos in (0, 2) else 0.5)
        for pos in (1, 3):                                    # bacalhau
            tr.add(b0 + pos, rim(), 0.35)
        for s in range(16):                                   # triângulo
            tr.add(b0 + s / 4, triangle(0.4, s % 4 == 2), 1.0 if s % 4 == 2 else 0.6)
        root = prog[bar][0]
        for pos, dur in ((0, 0.7), (1.5, 0.4), (2, 0.7), (3, 0.4)):
            tr.add(b0 + pos, bass(hz(root - 12 if root > 52 else root), dur), 0.55)
        for pos in (0.5, 2.5):
            for m in prog[bar]:
                tr.add(b0 + pos, pluck(hz(m), 0.5, 0.6), 0.10)
    # sanfona (Lá mixolídio)
    sc = [57, 59, 61, 62, 64, 66, 67, 69, 71, 73, 74, 76]
    A_ = [(0, 7, .75), (.75, 8, .25), (1, 9, .5), (1.5, 8, .5), (2, 7, .5), (2.5, 6, .5), (3, 4, 1),
          (4, 5, .5), (4.5, 6, .5), (5, 7, .75), (5.75, 6, .25), (6, 4, 1.5)]
    B_ = [(0, 4, .5), (.5, 6, .5), (1, 7, 1), (2, 9, .5), (2.5, 10, .5), (3, 9, 1), (4, 7, .5), (4.5, 6, .5), (5, 4, .5), (5.5, 2, .5), (6, 0, 2)]
    for i, ph in enumerate([A_, A_, B_, A_, A_, B_, B_, A_]):
        if i in (0, 4): continue                               # respiro: entra no 2º bloco
        base = 8 * i
        for pos, deg, l in ph:
            tr.add(base + pos, reed(hz(sc[deg] + 12), l * tr.beat * 0.95), 0.33)
    return tr

def noite():
    tr = Track(72)
    Em, Am, Cmaj, Bm = (52, 55, 59), (57, 60, 64), (48, 52, 55), (47, 50, 54)
    prog = [Em, Em, Am, Am, Em, Em, Cmaj, Bm] * 2
    for bar in range(16):
        b0 = bar * 4
        tr.add(b0 + 2, kick(0.45, 90, 45), 0.8)               # one drop
        tr.add(b0 + 2, rim(), 0.4)
        for s in range(8):
            tr.add(b0 + s / 2, hat(), 0.5 if s % 2 else 0.25)
        for pos in (0.5, 1.5, 2.5, 3.5):                       # skank
            tr.add(b0 + pos, organ([hz(m + 12) for m in prog[bar]]), 0.55)
        root = prog[bar][0]
        line = [(0, 0, 1.0), (1.25, 7, .5), (1.75, 10, .25), (2.5, 0, .5), (3, 3, .5), (3.5, 5, .5)]
        for pos, iv, dur in line:
            tr.add(b0 + pos, bass(hz(root - 24 + iv), dur * tr.beat), 0.7)
        if bar % 2 == 0:
            tr.add(b0, pad([hz(m) for m in prog[bar]], 2 * 4 * tr.beat), 0.8)
    # melódica com eco (Mi dórico)
    sc = [64, 66, 67, 69, 71, 73, 74, 76]
    mel = [(0, 4, 1.5), (1.5, 3, .5), (2, 2, 2), (4, 4, .5), (4.5, 5, .5), (5, 4, 1), (6, 2, 2),
           (8, 1, 1.5), (9.5, 2, .5), (10, 4, 1), (11, 3, 1), (12, 2, 1), (13, 1, 1), (14, 0, 2)]
    for rep in (16, 48):
        for pos, deg, l in mel:
            note = reed(hz(sc[deg]), l * tr.beat * 0.9, vib=4.5)
            for k, g in enumerate((0.3, 0.14, 0.06)):         # eco de 3/4 de tempo
                tr.add(rep + pos + k * 0.75, note, g)
    return tr

def render(tr, name):
    x = tr.buf
    x = x - np.mean(x)
    x = np.tanh(1.2 * x / (np.max(np.abs(x)) + 1e-9)) * 0.85
    pcm = (x * 32767).astype(np.int16)
    os.makedirs(OUT, exist_ok=True)
    wav = os.path.join(OUT, name + '.wav')
    import wave
    with wave.open(wav, 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())
    ogg = os.path.join(OUT, name + '.ogg')
    try:
        subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-i', wav, '-c:a', 'libvorbis', '-q:a', '5', ogg], check=True)
        os.remove(wav)
        print('✓', ogg, f'{len(pcm) / SR:.1f}s')
    except Exception as e:
        print('ffmpeg indisponível, mantendo WAV:', wav, e)

if __name__ == '__main__':
    render(dia(), 'trilha_dia')
    render(feira(), 'trilha_feira')
    render(noite(), 'trilha_noite')
