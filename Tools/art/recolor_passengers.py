"""Bakes one small texture per (character archetype, gameplay colour).
Only the outfit (the archetype's dominant saturated hue) is recoloured; skin, eyes, white trim and soles are kept.
Shading is preserved by scaling the target colour with each pixel's brightness relative to the outfit's typical brightness.
Input: pax_src_{a,b,c,d}.png exported from the glb textures (scratchpad). Output: Art/Textures/Passengers/pax_{arch}_{colour}.png"""
import os as _os
REPO = _os.path.abspath(_os.path.join(_os.path.dirname(__file__), "..", "..")).replace("\\", "/") + "/"
import os, sys
import numpy as np
from PIL import Image

SP = sys.argv[1] if len(sys.argv) > 1 else REPO + "Temp/art_src/"
OUT = REPO + "Assets/SkyQueue/Art/Textures/Passengers/"
os.makedirs(OUT, exist_ok=True)
PALETTE = ["FF8C1A", "FFD61F", "338CFF", "4CD933", "F23333", "FF73BF", "9940E6", "734726"]   # GameConfig.palette order
SIZE = 256


def rgb_to_hsv(a):
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    mx, mn = a.max(axis=2), a.min(axis=2)
    d = mx - mn
    h = np.zeros_like(mx)
    m = d > 1e-5
    rm = m & (mx == r); gm = m & (mx == g) & ~rm; bm = m & ~rm & ~gm
    h[rm] = ((g - b)[rm] / d[rm]) % 6
    h[gm] = (b - r)[gm] / d[gm] + 2
    h[bm] = (r - g)[bm] / d[bm] + 4
    h = h * 60.0
    s = np.where(mx > 1e-5, d / np.maximum(mx, 1e-5), 0)
    return h, s, mx


for arch in "abcd":
    img = Image.open(SP + "pax_src_" + arch + ".png").convert("RGB").resize((SIZE * 2, SIZE * 2), Image.LANCZOS)
    a = np.asarray(img).astype(np.float32) / 255.0
    h, s, v = rgb_to_hsv(a)
    # dominant saturated hue = the outfit
    strong = (s > 0.55) & (v > 0.3)
    hist, edges = np.histogram(h[strong], bins=36, range=(0, 360))
    dom = (edges[np.argmax(hist)] + 5.0)
    dh = np.minimum(np.abs(h - dom), 360 - np.abs(h - dom))
    mask = np.clip((s - 0.45) / 0.15, 0, 1) * np.clip((28 - dh) / 8, 0, 1) * (v > 0.2)
    vref = np.percentile(v[mask > 0.5], 85) if (mask > 0.5).any() else 1.0
    for ci, hexc in enumerate(PALETTE):
        tgt = np.array([int(hexc[i:i + 2], 16) for i in (0, 2, 4)], np.float32) / 255.0
        shade = np.clip(v / vref, 0, 1.25)[..., None]
        rec = np.clip(tgt[None, None, :] * shade, 0, 1)
        out = a * (1 - mask[..., None]) + rec * mask[..., None]
        Image.fromarray((out * 255).astype(np.uint8)).resize((SIZE, SIZE), Image.LANCZOS).save(OUT + "pax_%s_%d.png" % (arch, ci))
    print(arch, "dominant hue %.0f, outfit %.0f%% of texture" % (dom, (mask > 0.5).mean() * 100))
