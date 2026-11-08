"""Exhaust puffs for taxiing planes: the signal-smoke frames recoloured to soft white/grey (shading and alpha kept).
in:  Art/Sprites/SignalSmoke/smoke_XX.png   out: Art/Sprites/Exhaust/puff_XX.png"""
import os as _os
REPO = _os.path.abspath(_os.path.join(_os.path.dirname(__file__), "..", "..")).replace("\\", "/") + "/"
import os, glob
import numpy as np
from PIL import Image

ROOT = REPO + "Assets/SkyQueue/Art/Sprites/"
os.makedirs(ROOT + "Exhaust", exist_ok=True)
for f in sorted(glob.glob(ROOT + "SignalSmoke/smoke_*.png")):
    a = np.asarray(Image.open(f).convert("RGBA")).astype(np.float32) / 255.0
    rgb, alpha = a[..., :3], a[..., 3:]
    lum = (0.299 * rgb[..., 0] + 0.587 * rgb[..., 1] + 0.114 * rgb[..., 2])[..., None]
    lo, hi = np.percentile(lum[alpha[..., 0] > 0.5], [5, 95]) if (alpha > 0.5).any() else (0.0, 1.0)
    shade = np.clip((lum - lo) / max(1e-3, hi - lo), 0, 1)
    white = np.array([0.97, 0.97, 1.0], np.float32)
    grey = np.array([0.72, 0.74, 0.80], np.float32)
    col = grey + (white - grey) * shade
    out = np.concatenate([col, alpha * 0.9], axis=2)
    Image.fromarray((out * 255).clip(0, 255).astype(np.uint8), "RGBA").save(ROOT + "Exhaust/" + os.path.basename(f).replace("smoke", "puff"))
print("ok")
