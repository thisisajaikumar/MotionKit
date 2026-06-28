# Extending MotionKit (Plugin SDK)

All extension points live in `AjoyGames.MotionKit.SDK`. You never edit MotionKit source: mark your type
with the right attribute and the editor code generator registers it (reflection-free) into your assembly.
Your assembly only needs to reference `AjoyGames.MotionKit.Runtime`.

## Custom track
```csharp
using UnityEngine;
using AjoyGames.MotionKit;
using AjoyGames.MotionKit.SDK;

[System.Serializable]
public sealed class MyTrackData : RecordedTrack
{
    public const string TypeId = "mygame.mytrack";
    public override string TrackTypeId => TypeId;
    public override float Duration => 0f;
    public override int KeyframeCount => 0;
}

public sealed class MyTrackRecorder : TrackRecorderBase<MyTrackData>
{
    public override void RecordFrame(float time) { }
    public override RecordedTrack EndRecord() { return new MyTrackData(); }
}

public sealed class MyTrackPlayer : TrackPlayerBase<MyTrackData>
{
    protected override bool OnPrepare() { return true; }
    public override void Evaluate(float time) { }
}

[TrackModule(Order = 200)]
public sealed class MyTrackModule : TrackModuleBase<MyTrackData, MyTrackRecorder, MyTrackPlayer>
{
    public MyTrackModule() : base(typeof(MyComponent)) { }
    public override string Id => MyTrackData.TypeId;
    public override string DisplayName => "My Track";
}
```

For tracks that record a fixed set of scalar channels, derive from
`AjoyGames.MotionKit.Tracks.Sampled.SampledTrackData/Recorder/Player` to get storage, keyframe reduction,
evaluation and optional clip baking for free (see `LightTrack`, `CameraTrack`). Implement `SDK.IBakableTrack`
to contribute curves to generated AnimationClips.

## Custom event handler
```csharp
[EventHandlerModule("mygame.spawn")]
public sealed class SpawnHandler : IRecordedEventHandler
{
    public string HandlerId => "mygame.spawn";
    public void Prepare(InteractionEventBinder binder, GameObject root) { }
    public void Invoke(in RecordedEvent e) { }
    public void OnStop() { }
}
```
Built-in handlers expose registration helpers for app code, e.g. `DelegateEventHandler.Register("key", e => ...)`
and `CommandEventHandler.Register("cmd", (root, e) => ...)`.

## Custom migration
```csharp
[RecordingMigration]
public sealed class Migration_1_to_2 : IRecordingMigration
{
    public int FromVersion => 1;
    public int ToVersion => 2;
    public void Migrate(InteractionRecording recording) { }
}
```
Bump `RecordingVersion.CurrentFileVersion` when you add a new format; old assets upgrade automatically.

## After adding modules
Run `Tools ▸ MotionKit ▸ Regenerate Module Registry` (or just let the next script reload do it).

## XR module
`AjoyGames.MotionKit.XR` is guarded by version-defines (`MOTIONKIT_XR_HANDS`, `MOTIONKIT_XRI`,
`MOTIONKIT_OPENXR`). It compiles to nothing without those packages, so non-XR projects are unaffected. The
included `XRHandTrack` is a reference implementation — validate it against your installed XR Hands version.
Future XR/AR integrations are intended to ship as separate add-on packages.
