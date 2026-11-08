"""Cuts the owner's art sheets into transparent sprites for Unity.

Background removal: pixels that look like the sheet background AND are connected to a cell border become transparent
(flood fill), so same-coloured details enclosed by an outline (eyes, steam puffs, asphalt inside the pad) are kept.
Edges get a 1-2 px alpha feather to avoid a hard fringe.
"""
import os as _os
REPO = _os.path.abspath(_os.path.join(_os.path.dirname(__file__), "..", "..")).replace("\\", "/") + "/"
import os, sys
import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage

SRC = REPO + "Assets/SkyQueue/Art/2D/"
OUT = REPO + "Assets/SkyQueue/Art/Sprites/"


def cut_cell(rgb, is_bg, out_size, pad=8):
    """rgb: HxWx3 uint8 cell. is_bg: function(arr)->bool mask of background-like pixels."""
    bg_like = is_bg(rgb.astype(np.int32))
    labels, n = ndimage.label(bg_like)
    border = set(np.unique(np.concatenate([labels[0, :], labels[-1, :], labels[:, 0], labels[:, -1]]))) - {0}
    bg = np.isin(labels, list(border))
    # grow the background 1 px into the anti-aliased fringe, then feather
    bg = ndimage.binary_dilation(bg, iterations=1) & ~ndimage.binary_erosion(~bg_like, iterations=0) if False else bg
    fg = ~bg
    # drop tiny isolated specks (background noise), keep every real part of the drawing
    fl, fn = ndimage.label(fg)
    if fn > 1:
        sizes = ndimage.sum(fg, fl, range(1, fn + 1))
        keep = np.isin(fl, [i + 1 for i, s in enumerate(sizes) if s >= fg.size * 0.002])
        fg = keep
    bg = ~fg
    alpha = np.where(bg, 0, 255).astype(np.uint8)
    a_img = Image.fromarray(alpha).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(0.8))
    img = Image.fromarray(rgb).convert("RGBA")
    img.putalpha(a_img)
    bbox = a_img.point(lambda v: 255 if v > 20 else 0).getbbox()
    img = img.crop(bbox)
    w, h = img.size
    side = max(w, h) + pad * 2
    canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    canvas.paste(img, ((side - w) // 2, (side - h) // 2))
    return canvas.resize((out_size, out_size), Image.LANCZOS)


def grid(img, cols, rows):
    W, H = img.size
    for r in range(rows):
        for c in range(cols):
            yield r * cols + c, np.array(img.crop((c * W // cols, r * H // rows, (c + 1) * W // cols, (r + 1) * H // rows)))


def light_neutral(a):   # emoji sheet: near-white checker background
    mx, mn = a.max(axis=2), a.min(axis=2)
    return (mn > 222) & (mx - mn < 16)


def dark_slate(a):      # booster sheet: dark blue-grey background with soft shadows
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    mx, mn = a.max(axis=2), a.min(axis=2)
    return (mx < 112) & (mx - mn < 24) & (b >= r - 4)


def magenta(a):         # sheets generated on the requested flat #FF00FF background (art page rule "2D backgrounds")
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    return (r > 170) & (b > 170) & (g < 110) & (np.abs(r - b) < 70)


if __name__ != "__main__":
    raise SystemExit   # imported for the helpers only

os.makedirs(OUT + "Emoji", exist_ok=True)
os.makedirs(OUT + "Boosters", exist_ok=True)

emoji_names = ["angry", "sleepy", "confused", "shocked", "sad", "bored"]
sheet = Image.open(SRC + "Expressive Emoji Sticker Sheet.png").convert("RGB")
for i, cell in grid(sheet, 3, 2):
    cut_cell(cell, light_neutral, 256).save(OUT + "Emoji/emoji_" + emoji_names[i] + ".png")

booster_names = ["vip", "arrange", "jumble", "extra_slot"]
sheet = Image.open(SRC + "Colourful Travel Game Asset Sheet.png").convert("RGB")
for i, cell in grid(sheet, 2, 2):
    cut_cell(cell, dark_slate, 256).save(OUT + "Boosters/booster_" + booster_names[i] + ".png")

print("ok")
