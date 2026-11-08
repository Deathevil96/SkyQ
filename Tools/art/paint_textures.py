"""Turns each plane's white+blue livery into a 'paint' texture that the game tints with the gameplay colour.
Body (white/light grey) stays light -> becomes the full gameplay colour when multiplied.
Blue trim -> mid grey -> a darker shade of the gameplay colour.
Dark parts (windows, tyres, propeller) stay dark."""
import os as _os
REPO = _os.path.abspath(_os.path.join(_os.path.dirname(__file__), "..", "..")).replace("\\", "/") + "/"
import os, colorsys
import numpy as np
from PIL import Image

SP = REPO + "Temp/art_src/"
OUT = REPO + "Assets/SkyQueue/Art/Textures/"
os.makedirs(OUT, exist_ok=True)
TRIM_SHADE = 0.55   # trim brightness relative to its own luminance

for size in ("16", "24", "40"):
    a = np.asarray(Image.open(SP + "src_" + size + ".png").convert("RGB")).astype(np.float32) / 255.0
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    mx, mn = a.max(axis=2), a.min(axis=2)
    sat = np.where(mx > 1e-4, (mx - mn) / np.maximum(mx, 1e-4), 0)
    lum = 0.299 * r + 0.587 * g + 0.114 * b
    blue = (b > r + 0.12) & (b > g + 0.05) & (sat > 0.3)
    # soft mask for anti-aliased trim edges
    trim = np.clip((sat - 0.25) / 0.2, 0, 1) * blue
    grey_body = np.clip(lum * 1.08, 0, 1)                 # light, desaturated body
    grey_trim = np.clip((0.35 + lum) * TRIM_SHADE, 0, 1)  # darker shade for the stripes / tail
    out = grey_body * (1 - trim) + grey_trim * trim
    img = Image.fromarray((np.stack([out] * 3, axis=2) * 255).clip(0, 255).astype(np.uint8))
    img.resize((1024, 1024), Image.LANCZOS).save(OUT + "plane" + size + "_paint.png")
    print(size, "trim px %.1f%%" % (blue.mean() * 100))
