"""Cuts the UI kit and the VFX sheet into transparent sprites.

Both sheets were generated on busy backgrounds (soft coloured glow / white fog), so a flat-colour key cannot work.
Instead every piece is cropped by an explicit box and the background is the *smooth* region connected to the crop
border: pixels whose local gradient is low. The art has crisp outlines, so the flood stops there, and enclosed flat
areas (button fills, panel cream, cloud whites) are kept. Edges get a 1 px feather.

out: Art/Sprites/UI/*.png, Art/Sprites/VFX/{poof,confetti,sparkle,dust}_N.png
UI 9-slice borders are printed at the end (set them in the sprite import settings; the import script does that).
"""
import os as _os
REPO = _os.path.abspath(_os.path.join(_os.path.dirname(__file__), "..", "..")).replace("\\", "/") + "/"
import os, json
import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage

SRC = REPO + "Assets/SkyQueue/Art/2D/"
OUT_UI = REPO + "Assets/SkyQueue/Art/Sprites/UI/"
OUT_FX = REPO + "Assets/SkyQueue/Art/Sprites/VFX/"


def gradient(rgb):
    a = np.asarray(Image.fromarray(rgb).filter(ImageFilter.GaussianBlur(1.0))).astype(np.float32)
    g = np.zeros(a.shape[:2], np.float32)
    for c in range(3):
        gx = ndimage.sobel(a[..., c], axis=1); gy = ndimage.sobel(a[..., c], axis=0)
        g = np.maximum(g, np.hypot(gx, gy))
    return g / 8.0   # sobel gain -> roughly "levels per pixel"


def cut(rgb, thresh=6.0, drop_border_parts=False, min_part=0.002):
    """Returns RGBA (uint8) with the smooth border-connected background removed."""
    g = gradient(rgb)
    edge = ndimage.binary_dilation(g > thresh, iterations=1)            # close 1 px gaps in outlines
    smooth = ~edge
    lab, _ = ndimage.label(smooth)
    border = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    bg = np.isin(lab, list(border))
    bg = ndimage.binary_dilation(bg, iterations=1) & (g < thresh * 3)   # give back the dilation ring, eat soft halo
    fg = ndimage.binary_fill_holes(~bg)
    fl, fn = ndimage.label(fg)
    if fn:
        keep = []
        sizes = ndimage.sum(fg, fl, range(1, fn + 1))
        edge_ids = set(np.unique(np.concatenate([fl[0], fl[-1], fl[:, 0], fl[:, -1]])))
        for i, s in enumerate(sizes):
            if s < fg.size * min_part: continue
            if drop_border_parts and (i + 1) in edge_ids: continue
            keep.append(i + 1)
        fg = np.isin(fl, keep)
    alpha = Image.fromarray(np.where(fg, 255, 0).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.7))
    img = Image.fromarray(rgb).convert("RGBA")
    img.putalpha(alpha)
    return img


def hsv(rgba):
    a = rgba[..., :3].astype(np.float32) / 255.0
    mx, mn = a.max(axis=2), a.min(axis=2); d = mx - mn
    h = np.zeros_like(mx); m = d > 1e-5
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    rm = m & (mx == r); gm = m & (mx == g) & ~rm; bm = m & ~rm & ~gm
    h[rm] = ((g - b)[rm] / d[rm]) % 6; h[gm] = (b - r)[gm] / d[gm] + 2; h[bm] = (r - g)[bm] / d[bm] + 4
    return h * 60.0, np.where(mx > 1e-5, d / np.maximum(mx, 1e-5), 0), mx


def stars(rgb, grey):
    """The three stars above a result ribbon, cut by colour (gold or grey fill + dark outline); thin strokes removed."""
    a = np.dstack([rgb, np.full(rgb.shape[:2], 255, np.uint8)])
    h, s, v = hsv(a)
    if grey: m = (s < 0.2) & (v > 0.2) & (v < 0.85)
    else:    m = (h > 12) & (h < 62) & (s > 0.45) & (v > 0.35)
    m &= np.asarray(cut(rgb))[..., 3] > 127          # never the glow behind the sheet
    H, W = m.shape                                     # the panel border arcs run beside and below the stars
    m[int(H * 0.56):] = False; m[:, :int(W * 0.17)] = False; m[:, int(W * 0.83):] = False
    m = ndimage.binary_opening(ndimage.binary_fill_holes(ndimage.binary_closing(m, iterations=2)), iterations=3)
    lab, n = ndimage.label(m)
    sizes = ndimage.sum(m, lab, range(1, n + 1))
    m = np.isin(lab, (np.argsort(sizes)[::-1][:3] + 1).tolist())   # the three stars
    m = ndimage.binary_dilation(m, iterations=1)
    out = Image.fromarray(a, "RGBA")
    out.putalpha(Image.fromarray(np.where(m, 255, 0).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.7)))
    return trim(out)


def strip_panel(img, from_frac=0.5):
    """Ribbons were drawn over a panel. Remove the panel cream everywhere and its gold border in the lower part;
    holes that end up enclosed (star highlights) are filled back, thin leftover strokes are opened away."""
    a = np.asarray(img).copy()
    h, s, v = hsv(a)
    alpha = a[..., 3] > 127
    cream = (v > 0.8) & (s < 0.4) & (h > 15) & (h < 65)
    alpha = ndimage.binary_fill_holes(alpha & ~cream)
    lower = np.zeros(alpha.shape, bool); lower[int(alpha.shape[0] * from_frac):] = True
    gold = ((h < 230) | (h > 330)) & (s > 0.3)   # any saturated non-purple pixel (panel border, glow); the ribbon is purple
    pale = s < 0.35                             # glow / rays: lower part only (the ribbon's top highlight is pale)
    alpha = ndimage.binary_fill_holes(alpha & ~gold & ~(lower & pale))
    alpha = ndimage.binary_opening(alpha, iterations=2)
    lab, n = ndimage.label(alpha)
    if n:
        sizes = ndimage.sum(alpha, lab, range(1, n + 1))
        main = int(np.argmax(sizes)) + 1
        keep = []
        for i, sz in enumerate(sizes):
            part = lab == i + 1
            if i + 1 == main: keep.append(i + 1)   # the ribbon only
        alpha = ndimage.binary_erosion(np.isin(lab, keep), iterations=2)   # shave the 2 px fringe left by the panel glow
    a[..., 3] = np.asarray(Image.fromarray(np.where(alpha, 255, 0).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.7)))
    return Image.fromarray(a, "RGBA")


def drop_junk(img):
    """VFX cells: remove dark specks and thin grey strokes left from the fog/noise around the sheet."""
    a = np.asarray(img).copy()
    fg = a[..., 3] > 127
    h, s, v = hsv(a)
    lab, n = ndimage.label(fg)
    for i, sl in enumerate(ndimage.find_objects(lab)):
        part = lab[sl] == i + 1
        area = part.sum()
        dark = v[sl][part].mean() < 0.7   # every real piece (confetti, cloud, dust, sparkle) is bright; noise is ~0.55
        thin = area / float(part.size) < 0.15 and max(part.shape) > 30
        grey = s[sl][part].mean() < 0.12 and area < fg.sum() * 0.02
        if dark or thin or grey: a[..., 3][sl][part] = 0
    near = ndimage.binary_dilation(a[..., 3] > 127, iterations=3)
    a[..., 3][~near] = 0                        # soft feather pixels of removed junk
    return Image.fromarray(a, "RGBA")


def trim(img, pad=4):
    bb = img.getchannel("A").point(lambda v: 255 if v > 16 else 0).getbbox()
    if bb is None: return img
    img = img.crop(bb)
    out = Image.new("RGBA", (img.width + pad * 2, img.height + pad * 2), (0, 0, 0, 0))
    out.paste(img, (pad, pad))
    return out


# --------------------------------------------------------------------------------------------------------- UI kit
# boxes in sheet pixels (1536 x 1024): x0, y0, x1, y1 (a few px of background around each piece)
UI = {
    "btn_blue":       (14, 12, 252, 94),
    "btn_grey":       (14, 162, 252, 242),
    "btn_green":      (256, 28, 536, 146),
    "round_restart":  (538, 26, 660, 146),
    "round_pause":    (662, 26, 784, 146),
    "banner_wings":   (790, 26, 1126, 146),
    "coin":           (1132, 34, 1246, 146),
    "coin_bar":       (1250, 36, 1520, 140),
    "ribbon_win":     (214, 162, 574, 352),
    "ribbon_lose":    (604, 174, 966, 352),
    "ribbon_purple":  (70, 560, 446, 690),
    "close_x":        (1420, 190, 1500, 262),
    "toggle_on":      (756, 674, 880, 746),
    "icon_music":     (560, 674, 636, 742),
    "icon_sound":     (560, 762, 646, 832),
    "btn_home_small": (244, 446, 342, 530),
    "btn_retry_small":(338, 446, 436, 530),
    "btn_play_small": (432, 446, 548, 530),
    "header_pause":   (606, 560, 836, 664),
    "banner_yellow":  (992, 674, 1460, 786),
    "banner_red":     (980, 778, 1470, 886),
    "bar_check":      (976, 894, 1500, 1004),
    "bar_people":     (1010, 572, 1322, 670),
    "icon_seat":      (1326, 574, 1434, 670),
    "panel_src":      (44, 560, 464, 1004),
}
# pieces that are 9-sliced in Unity (borders written to slices.json as [left, bottom, right, top])
SLICE = {"btn_blue", "btn_grey", "btn_green", "bar_check", "bar_people", "coin_bar"}
RIBBON_HUES = {"green": 105.0, "red": 355.0, "blue": 212.0}   # target hues for the purple ribbon copies

os.makedirs(OUT_UI, exist_ok=True)
sheet = np.asarray(Image.open(SRC + "Glossy Casual Game UI Kit.png").convert("RGB"))
borders = {}
for name, (x0, y0, x1, y1) in UI.items():
    img = trim(cut(np.ascontiguousarray(sheet[y0:y1, x0:x1])))
    if name == "panel_src":
        # A clean 9-slice panel: mirror the clean bottom-left corner into all four corners, cream in the middle.
        a = np.asarray(img).copy()
        h, w = a.shape[:2]
        k = 84                                         # corner patch (covers the rounded border); left 84 px are clean
        corner = a[h - k:, :k]                          # bottom-left (the green button starts further right)
        left = corner[0:1]                              # its top row is already the straight left edge (1 x k)
        cream = corner[0, k - 1]                        # all pieces from one patch, so the colours match
        n = 2 * k + 8
        p = np.zeros((n, n, 4), np.uint8)
        p[:, :] = cream
        bl = corner; br = bl[:, ::-1]; tl = bl[::-1]; tr = tl[:, ::-1]
        p[n - k:, :k] = bl; p[n - k:, n - k:] = br; p[:k, :k] = tl; p[:k, n - k:] = tr
        p[k:n - k, :k] = left; p[k:n - k, n - k:] = left[:, ::-1]
        bottom = corner[:, k - 1:k]                     # its last column is already the straight bottom edge (k x 1)
        p[n - k:, k:n - k] = bottom; p[:k, k:n - k] = bottom[::-1]
        near = np.abs(p[..., :3].astype(int) - cream[:3].astype(int)).sum(axis=2) < 40
        p[near] = cream                                 # one flat cream inside: no seams between the patches
        Image.fromarray(p, "RGBA").save(OUT_UI + "panel.png")
        borders["panel"] = k
        continue
    if name.startswith("ribbon_") and name != "ribbon_purple":
        stars(np.ascontiguousarray(sheet[y0:y1, x0:x1]), grey=name == "ribbon_lose").save(OUT_UI + ("stars_gold" if name == "ribbon_win" else "stars_grey") + ".png")
        continue   # the win/lose ribbons overlap their panel; the purple ribbon is recoloured instead (below)
    if name == "ribbon_purple": img = trim(strip_panel(img), 2)
    img.save(OUT_UI + name + ".png")
    if name in SLICE:   # pills stretch horizontally: caps = half the height so the rounded ends never squash
        left = img.height if name.startswith(("coin", "bar")) else img.height // 2   # bars carry an icon disc on the left
        borders[name] = [left, img.height // 2 - 2, img.height // 2, img.height // 2 - 2]
    if name == "ribbon_purple":   # hue-rotated copies for the result / pause popups (shading and outline kept)
        a = np.asarray(img).astype(np.float32) / 255.0
        h, s, v = hsv(np.asarray(img))
        for tag, hue in RIBBON_HUES.items():
            nh = (h + (hue - 280.0)) % 360.0
            c = v * s; x = c * (1 - np.abs((nh / 60.0) % 2 - 1)); m = v - c
            k = (nh // 60).astype(int)
            r = np.select([k == 0, k == 1, k == 2, k == 3, k == 4], [c, x, 0, 0, x], c)
            g = np.select([k == 0, k == 1, k == 2, k == 3, k == 4], [x, c, c, x, 0], 0)
            b = np.select([k == 0, k == 1, k == 2, k == 3, k == 4], [0, 0, x, c, c], x)
            out = np.dstack([r + m, g + m, b + m, a[..., 3]])
            Image.fromarray((out * 255).clip(0, 255).astype(np.uint8), "RGBA").save(OUT_UI + "ribbon_" + tag + ".png")
json.dump(borders, open(OUT_UI + "slices.json", "w"), indent=1)

# ------------------------------------------------------------------------------------------------------- VFX sheet
os.makedirs(OUT_FX, exist_ok=True)
fx = np.asarray(Image.open(SRC + "Cartoon Game VFX Sprite Sheet.png").convert("RGB"))
H, W = fx.shape[:2]
rows = ["poof", "confetti", "sparkle", "dust"]
for r, name in enumerate(rows):
    for c in range(4):
        cell = np.ascontiguousarray(fx[r * H // 4:(r + 1) * H // 4, c * W // 4:(c + 1) * W // 4])
        img = drop_junk(cut(cell, thresh=5.0, drop_border_parts=True, min_part=0.0004))
        img = img.resize((256, int(256 * img.height / img.width)), Image.LANCZOS)
        side = max(img.size)
        sq = Image.new("RGBA", (side, side), (0, 0, 0, 0)); sq.paste(img, ((side - img.width) // 2, (side - img.height) // 2))
        sq.resize((256, 256), Image.LANCZOS).save(OUT_FX + "%s_%d.png" % (name, c))
print("ok", borders)
