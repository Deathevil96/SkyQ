---
name: project-taxi-strip-pending
description: "Owner's taxi strip piece model (2026-11-09) is pushed but not wired; owner will implement it on the Mac"
metadata:
  node_type: memory
  type: project
  originSessionId: 4656ef84-2165-4f44-9ea2-21d8b7e672e8
  modified: 2026-11-09T03:27:22.096Z
---

On 2026-11-09 the owner added a Meshy taxi strip piece model at `Assets/SkyQueue/Art/3D/Meshy_AI_taxi_strip_piece_20261109_generate_e0e0adf9/` and `..._texture_152bacb7/`. It was pushed to GitHub (commit 95054b6) but is **not wired into the game**. The owner moved to the Mac and will implement it there.

**Why:** the owner wanted the asset in the repo so it is on the Mac; the work itself was postponed.
**How to apply:** at the start of a Mac session, treat wiring the taxi strip piece as the first open task. Inspect the glb (orientation, pivot, tiling length), work out where it belongs (the taxiway/road strip by the stands, possibly per world), make it data-driven in config, and check it with a Play-mode screenshot. Related: [[project-skyqueue]].
