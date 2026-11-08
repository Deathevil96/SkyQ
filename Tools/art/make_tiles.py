"""Makes the owner's ground textures seamless for world-space tiling.
Asphalt: trims the lighter border rows/columns, then cross-fades with a half-offset copy so opposite edges match.
Garden: already tileable; resized only."""
import os as _os
REPO = _os.path.abspath(_os.path.join(_os.path.dirname(__file__), "..", "..")).replace("\\", "/") + "/"
import os
import numpy as np
from PIL import Image

SRC = REPO + "Assets/SkyQueue/Art/2D/"
OUT = REPO + "Assets/SkyQueue/Art/Textures/"
os.makedirs(OUT, exist_ok=True)


def seamless(img, trim=0.05):
    w, h = img.size
    img = img.crop((int(w * trim), int(h * trim), int(w * (1 - trim)), int(h * (1 - trim)))).resize((1024, 1024), Image.LANCZOS)
    a = np.asarray(img).astype(np.float32)
    b = np.roll(np.roll(a, 512, axis=0), 512, axis=1)   # seams of b sit in the middle of a, and vice versa
    y, x = np.mgrid[0:1024, 0:1024]
    # weight of the original: 1 in the centre, 0 at the edges (where b is seam-free)
    wx = 1 - np.abs((x - 511.5) / 511.5)
    wy = 1 - np.abs((y - 511.5) / 511.5)
    m = np.clip(np.minimum(wx, wy) * 3.0, 0, 1)[..., None]
    out = a * m + b * (1 - m)
    return Image.fromarray(out.clip(0, 255).astype(np.uint8))


seamless(Image.open(SRC + "Stylized Grey Asphalt Texture-1.png").convert("RGB")).save(OUT + "asphalt_tile.png")
Image.open(SRC + "Tileable Pink Cherry Blossom Garden-2.png").convert("RGB").resize((1024, 1024), Image.LANCZOS).save(OUT + "garden_tile.png")

# preview: 2x2 tiling of the fixed asphalt
t = Image.open(OUT + "asphalt_tile.png").resize((300, 300))
p = Image.new("RGB", (600, 600))
for i in range(2):
    for j in range(2):
        p.paste(t, (i * 300, j * 300))
p.save(REPO + "Temp/art_src/asphalt_preview.png")
print("ok")
