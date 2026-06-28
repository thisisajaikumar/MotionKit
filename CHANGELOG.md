# Changelog

All notable changes to MotionKit are documented here.
This project adheres to [Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-06-28
### Added
- Modular track system with reflection-free, code-generated module registry (IL2CPP/AOT/WebGL/console safe).
- Built-in tracks: Transform, Animator, Material, Audio, Particle, Light, Camera, UI, Physics.
- Persistent object GUID bindings (`PersistentObjectId`) with hierarchy-path fallback.
- Recording session with snapshot undo/redo, trim, cancel and continue.
- Separate, repeatable asset-generation pipeline (recording asset, prefab, animation clip(s),
  preview, metadata) with single- and multi-clip export.
- Zero-allocation playback engine with speed, loop, reverse, pause/resume, seek and object pooling.
- Static and instance `InteractionPlayer` runtime API.
- Universal event system (UnityEvent, delegate, command, animator parameter, audio, timeline-signal).
- Recorder profiles (Mobile / Desktop / VR / WebGL presets + custom).
- Built-in lightweight timeline editor (scrub, trim, event markers, zoom, frame stepping, snapping).
- Versioned recordings with automatic migration on import and load.
- Plugin SDK (custom tracks, players, recorders, event handlers, migrations, asset builders).
- Optional, version-define-guarded XR module with an XR Hands reference track.
- Setup wizard, custom inspectors, recording validation, sample content and documentation.
- Packaged for Unity Package Manager; supports Unity 2020.3 LTS and newer.
