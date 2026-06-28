# MotionKit Architecture

## Layers & assemblies
- `AjoyGames.MotionKit.Runtime` — data model, SDK interfaces, module registry, tracks, playback engine,
  components, universal event system, migration runner.
- `AjoyGames.MotionKit.Editor` — code generator, recording session/controller, asset-generation pipeline,
  timeline editor, recorder window, setup wizard, inspectors, validation.
- `AjoyGames.MotionKit.XR` — optional, guarded by version-defines; compiles inert without XR packages.
- `*.Tests.Runtime` / `*.Tests.Editor` — EditMode/PlayMode tests.

## Data model (source of truth)
`InteractionRecording` (ScriptableObject) holds the version header, a list of `RecordedObject` (each with a
persistent GUID, fallback path and a `[SerializeReference] List<RecordedTrack>`), the universal event stream,
and clip ranges for multi-clip export. Tracks store samples as struct-of-arrays so playback never allocates.

## No-runtime-reflection pipeline
Extensions are marked `[TrackModule]`, `[EventHandlerModule(id)]` or `[RecordingMigration]`. The editor
`RegistryGenerator` discovers them with `TypeCache` and writes, into each owning assembly's
`Generated/MotionKitGeneratedRegistry.gen.cs`, a class that registers each module via direct constructor
calls, invoked through `RuntimeInitializeOnLoadMethod` (play/builds) and `InitializeOnLoadMethod` (editor).
At runtime `ModuleRegistry` is populated only from generated code — no assembly scanning, no `Activator`.

## Recording
`RecordingController` stamps `PersistentObjectId`s, instantiates the applicable `ITrackRecorder`s per object
and drives a `TimeSampler` from `EditorApplication.update`. Output is appended into a transient
`RecordingSession` (snapshot undo/redo, trim, cancel, continue) — never directly into final assets.

## Asset generation (separate, repeatable)
`AssetBuilder` consumes `Recording.asset` and produces the prefab, clip(s), preview and metadata.
`AnimationClipBaker` bakes any track implementing `IBakableTrack`. Clip/metadata assets are overwritten in
place so references stay stable on regeneration.

## Playback (zero-alloc)
`InteractionPlaybackEngine.Prepare` resolves bindings once (`BindingResolver`, GUID-first), creates one
player per track and caches them. `Tick`/`Evaluate` use cursor-hinted `KeyframeSearch` — no allocations, no
LINQ, no per-frame `GetComponent`. `InteractionPlayer` is the MonoBehaviour facade + static API; `RecordingPool`
reuses spawned instances.

## Versioning & migration
`RecordingMigrator` runs the registered `IRecordingMigration` chain to bring a recording to the current file
version, on asset import and on `Prepare`.
