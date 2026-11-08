"""Desert world textures -> Art/Textures (1024, seamless).
Sand and concrete already tile; the dirt road has lighter dusty side edges, so its border is trimmed and cross-faded."""
import os as _os
REPO = _os.path.abspath(_os.path.join(_os.path.dirname(__file__), "..", "..")).replace("\\", "/") + "/"
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
import numpy as np
from PIL import Image

SRC = REPO + "Assets/SkyQueue/Art/2D/"
OUT = REPO + "Assets/SkyQueue/Art/Textures/"


def seamless(img, trim_x, trim_y):
    w, h = img.size
    img = img.crop((int(w * trim_x), int(h * trim_y), int(w * (1 - trim_x)), int(h * (1 - trim_y)))).resize((1024, 1024), Image.LANCZOS)
    a = np.asarray(img).astype(np.float32)
    b = np.roll(np.roll(a, 512, axis=0), 512, axis=1)
    y, x = np.mgrid[0:1024, 0:1024]
    m = np.clip(np.minimum(1 - np.abs((x - 511.5) / 511.5), 1 - np.abs((y - 511.5) / 511.5)) * 3.0, 0, 1)[..., None]
    return Image.fromarray((a * m + b * (1 - m)).clip(0, 255).astype(np.uint8))


Image.open(SRC + "Cartoon Desert Sand with Pebbles and Grass.png").convert("RGB").resize((1024, 1024), Image.LANCZOS).save(OUT + "desert_sand_tile.png")
seamless(Image.open(SRC + "Stylized Reddish-Brown Dirt Road Texture.png").convert("RGB"), 0.14, 0.02).save(OUT + "desert_dirt_tile.png")
Image.open(SRC + "Weathered Concrete with Sandy Stains.png").convert("RGB").resize((1024, 1024), Image.LANCZOS).save(OUT + "desert_strip_tile.png")
print("ok")
