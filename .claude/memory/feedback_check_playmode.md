---
name: feedback-check-playmode
description: "Before any Unity edit, refresh, compile or test run, check whether the editor is in Play mode and do not change anything if it is"
metadata:
  node_type: memory
  type: feedback
  originSessionId: 4656ef84-2165-4f44-9ea2-21d8b7e672e8
  modified: 2026-11-08T06:57:19.424Z
---

On every user command, first check whether the Unity editor is in Play mode (read `mcpforunity://editor/state`). If it is, do not change anything: no script/asset edits, no refresh/compile, no test runs.

**Why:** the user tests in Play mode; changes made then are not saved, and my refresh/compile/test runs while Play mode was active caused domain-reload disconnects and stalled the editor several times.

**How to apply:** check state first; if playing, only read/analyse and tell the user to exit Play mode (or wait) before I apply changes. See [[reference-dev-environment]] for other editor quirks.
