---
name: feedback-working-style
description: "How the owner wants SkyQueue work done - autonomy, no hardcoding, industry-standard code, performance first"
metadata:
  node_type: memory
  type: feedback
  originSessionId: 4656ef84-2165-4f44-9ea2-21d8b7e672e8
  modified: 2026-11-08T00:54:13.486Z
---

- Keep code industry-standard, organized and professional; **optimization is a main focus from the start** (no per-frame allocations, GPU instancing, cached delegates).
- **Do not hardcode** values: rules, colours, layout, timings, level tables go in config/data.
- When the owner is away ("going to sleep, don't stop"), keep implementing autonomously; they review bugs later.
- Do not use throwaway test bots silently: say so, and promote them into real tests.
- A level they can clear "without planning" is a defect - difficulty must demand planning.

**Why:** stated mid-session while iterating on the puzzle; they judge by playing it in the editor and on their phone.
**How to apply:** verify with the EditMode suite and headless bots, then a real Play-mode run; report honestly what was/was not verified.
