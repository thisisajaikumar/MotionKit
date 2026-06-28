# MotionKit Public API

## Runtime playback — `InteractionPlayer` (`AjoyGames.MotionKit.Components`)
Static (operate on the most-recently-played instance, pooled):
```csharp
InteractionPlayer.Play(GameObject recordingPrefab);
InteractionPlayer.Play(GameObject recordingPrefab, Vector3 position, Quaternion rotation, Transform parent = null);
InteractionPlayer.Stop();
InteractionPlayer.Pause();
InteractionPlayer.Resume();
InteractionPlayer.SetSpeed(float speed);
InteractionPlayer.SetLoop(bool loop);
```
Instance:
```csharp
player.PlayInstance(); player.StopInstance(); player.PauseInstance(); player.ResumeInstance();
player.SetSpeedInstance(float); player.SetLoopInstance(bool); player.SetReverseInstance(bool);
player.SeekInstance(float time);
player.Recording; player.IsPlaying; player.Engine;
```

## Playback engine — `InteractionPlaybackEngine` (`...Playback`)
```csharp
bool Prepare(InteractionRecording recording, GameObject root);
void Play(); void Pause(); void Resume(); void Stop();
void SetSpeed(float); void SetLoop(bool); void SetReverse(bool);
void Seek(float time); void Tick(float deltaTime); void Evaluate(float time);
PlaybackState State; float Duration; bool IsActive; bool IsPrepared;
```

## Object pooling — `RecordingPool` (`...Playback`)
```csharp
RecordingPool.Shared.Prewarm(prefab, count);
GameObject Get(prefab, position, rotation, parent = null);
void Release(GameObject instance);
```

## Data — `InteractionRecording` (`...Data`)
```csharp
RecordingVersion Version; string RecordingName; int Fps; float Duration; int FrameCount;
BindingMode BindingMode; List<RecordedObject> Objects; List<RecordedEvent> Events; List<ClipRange> ClipRanges;
float RecalculateDuration(); void SortEvents();
```

## Registry — `ModuleRegistry` (`AjoyGames.MotionKit`)
```csharp
IReadOnlyList<ITrackModule> TrackModules;
bool TryGetTrackModule(string id, out ITrackModule);
ITrackPlayer CreatePlayerFor(RecordedTrack);
IRecordedEventHandler CreateEventHandler(string handlerId);
IReadOnlyList<IRecordingMigration> GetMigrations();
```

## Migration — `RecordingMigrator` (`...Migration`)
```csharp
bool EnsureUpToDate(InteractionRecording recording);
```

## Editor asset pipeline — `AssetBuilder` (`...Editor.AssetGeneration`)
```csharp
AssetBuildResult Build(InteractionRecording recording, IReadOnlyList<GameObject> sourceRoots, AssetBuildSettings settings);
```

## Event registration helpers (`...Events.Handlers`)
```csharp
DelegateEventHandler.Register(string key, Action<RecordedEvent>);
CommandEventHandler.Register(string command, Action<GameObject, RecordedEvent>);
```
