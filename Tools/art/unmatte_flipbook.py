"""Turns a sprite sheet painted on flat magenta (#FF00FF) into frames with real alpha.
Semi-transparent smoke is a blend of the smoke colour and magenta, so alpha is recovered per pixel by projecting the
pixel onto the line magenta -> reference smoke colour, then the colour is un-blended. Frames are written as
<out>/<name>_00.png ... (row-major).
usage: python unmatte_flipbook.py <sheet.png> <out_dir> <name> <cols> <rows> [frame_px]"""
import os, sys
import numpy as np
from PIL import Image

src, out_dir, name = sys.argv[1], sys.argv[2], sys.argv[3]
cols, rows = int(sys.argv[4]), int(sys.argv[5])
size = int(sys.argv[6]) if len(sys.argv) > 6 else 256
os.makedirs(out_dir, exist_ok=True)

a = np.asarray(Image.open(src).convert("RGB")).astype(np.float32) / 255.0
M = np.array([1.0, 0.0, 1.0], np.float32)
d = a - M
dist = np.linalg.norm(d, axis=2)
# reference colour: the most opaque pixels (farthest from magenta)
ref = np.median(a[dist > np.percentile(dist, 99)], axis=0)
axis = ref - M
alpha = np.clip((d @ axis) / float(axis @ axis), 0, 1)
alpha[alpha < 0.04] = 0
col = np.clip(M + d / np.maximum(alpha[..., None], 1e-3), 0, 1)
rgba = np.concatenate([col, alpha[..., None]], axis=2)
img = Image.fromarray((rgba * 255).astype(np.uint8), "RGBA")

W, H = img.size
cw, ch = W / cols, H / rows
for r in range(rows):
    for c in range(cols):
        cell = img.crop((int(c * cw), int(r * ch), int((c + 1) * cw), int((r + 1) * ch)))
        side = max(cell.size)
        sq = Image.new("RGBA", (side, side), (0, 0, 0, 0))
        sq.paste(cell, ((side - cell.size[0]) // 2, (side - cell.size[1]) // 2))
        sq.resize((size, size), Image.LANCZOS).save(os.path.join(out_dir, "%s_%02d.png" % (name, r * cols + c)))
print("ok ref colour", (ref * 255).astype(int), "frames", cols * rows)
