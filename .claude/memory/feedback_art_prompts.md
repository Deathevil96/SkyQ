---
name: feedback-art-prompts
description: "Art prompts must spell out delivery details the generator gets wrong (background, separate parts), not just say \"transparent\""
metadata:
  node_type: memory
  type: feedback
  originSessionId: 4656ef84-2165-4f44-9ea2-21d8b7e672e8
  modified: 2026-11-08T14:03:20.366Z
---

When writing generation prompts for the owner, state every delivery requirement that the pipeline depends on, explicitly in the prompt text itself.

**Why:** the owner said "you should have mentioned the transparency in the prompts then" after the emoji/booster sheets came back with a painted checkerboard (image tools can't output real alpha; "transparent background" alone is not enough). Same class of issue: the helicopter came as one mesh, so the rotor had to be split in code.

**How to apply:** 2D prompts end with "on a perfectly flat solid magenta (#FF00FF) background: no checkerboard, no gradient, no glow or shadow on the background" + "pieces separated by empty space"; 3D prompts say which parts must be separate meshes (rotors, windsock sock) and the orientation/pivot. Prompts live on the art-pack artifact (see [[project-skyqueue]]) and `ART_ASSET_PLAN.md`.
