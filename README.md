# MotionKit

A high-performance **recording and playback** framework for Unity, by **Ajoy Games**.

Record interactions on any GameObject in the Unity Editor or Play Mode, then generate reusable assets —
a recording asset, animation clip(s), a prefab, a preview and metadata — that play back at runtime with a
clean API. No manual animation authoring required.

MotionKit is a **generic** recording framework (Desktop, Mobile, WebGL, VR, AR, Quest, Vision Pro,
OpenXR). It is modular, allocation-free during playback, and extensible without modifying core code.

## Install (UPM)

Package Manager ▸ **Add package from git URL…**:

```
https://github.com/thisisajajkumar/MotionKit.git
```

Requires Unity **2020.3 LTS or newer**. No third-party plugins. XR is an optional, guarded module.

## Quick start
1. `Tools ▸ MotionKit ▸ Setup Wizard ▸ Run All` (creates profiles, output folder, module registry).
2. Open `Window ▸ MotionKit ▸ Recorder`.
3. Select one or more GameObjects and click **+ Add Selected Objects**.
4. Choose a **Profile** and which **Track Types** to capture.
5. (Optional) enter Play Mode so your objects animate.
6. **Start Recording** → perform the interaction → **Stop**.
7. Scrub / trim on the timeline, then **Save & Generate Assets**.
8. Drop the generated `*.prefab` into a scene and play it:

```csharp
using AjoyGames.MotionKit.Components;

InteractionPlayer.Play(recordingPrefab);
InteractionPlayer.SetSpeed(0.5f);
InteractionPlayer.SetLoop(true);
InteractionPlayer.Pause();
InteractionPlayer.Resume();
InteractionPlayer.Stop();
```

## What gets generated
```
<OutputFolder>/<RecordingName>/
  Recording.asset     ← source of truth (tracks, events, version)
  <Name>.prefab       ← playable prefab (InteractionPlayer + event binder)
  <Name>.anim         ← baked AnimationClip(s) (one per clip range if multi-clip)
  Preview.png         ← thumbnail
  Metadata.asset      ← queryable summary
```

## Key concepts
- **No runtime reflection** — an editor code generator emits a registry of all track/event/migration
  modules; runtime just loads it. IL2CPP / AOT / WebGL / console safe.
- **Persistent GUID bindings** — recorded objects bind by a stable id that survives rename,
  reparenting, prefab nesting and scene reloads (path fallback for legacy data).
- **Versioned + migratable** — recordings store file/package/Unity versions and timestamps; old
  recordings auto-upgrade on load.
- **Plugin SDK** — add custom tracks, players, recorders, event handlers and migrations without editing
  MotionKit source. See `Documentation~/ExtendingMotionKit.md`.

See `Documentation~/Architecture.md` for the full design and `Documentation~/API.md` for the public API.
