---
name: reference-dev-environment
description: "Test device, tooling quirks and gotchas for the SkyQueue Unity project on this Windows machine"
metadata:
  node_type: memory
  type: reference
  originSessionId: 4656ef84-2165-4f44-9ea2-21d8b7e672e8
  modified: 2026-11-08T01:13:51.999Z
---

- Phone for checks: Samsung SM-S918B (S23 Ultra), adb serial RZCW2170Z7Y, 1440x3088, arm64. adb: `C:\Program Files\Unity\Hub\Editor\6000.3.17f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`.
- Unity MCP is connected; Play mode does not advance frames while the editor is unfocused - use `Time.captureDeltaTime` + `EditorApplication.Step()` in `execute_code` to simulate.
- Screenshot tool uses the camera path and omits the Screen-Space-Overlay HUD; save screenshots outside `Assets` (e.g. `Screenshots/`, git-ignored) to avoid asset import / domain reload.
- Platform switch triggers a recompile; wait for idle before `manage_build`.
- Device lessons: never `CreatePrimitive`/Shader.Find in code (physics module and unreferenced shaders are stripped in player builds); a Button-type InputAction ignores a second finger, use PassThrough; `adb shell input tap` is a ~0 ms press (good quick-tap test); cold-start with `am force-stop` then `am start -W` (monkey right after force-stop races and leaves a black screen). Dev builds log `[SkyQueue][perf]` lines (fps/frame ms/mono heap) readable with `adb logcat -s Unity`.
- Editor active build target is now Android (switched for device builds); EditMode tests still run.
- Use script files in the scratchpad for multi-line Python patches (bash heredocs with quotes break in the wrapper).
