# MotionKit

![Unity](https://img.shields.io/badge/Unity-2020.3%2B-black)
![License](https://img.shields.io/badge/License-MIT-green)
![Platform](https://img.shields.io/badge/Platform-All-blue)

MotionKit is a lightweight, high-performance recording and playback framework for Unity.

It enables developers to record GameObject motion, replay interactions, and build reusable animations without depending on Timeline or complex animation workflows.

Designed for production use with minimal runtime overhead.

---

## Features

- Record multiple GameObjects
- Record complete hierarchies
- Transform Recording
- Active State Recording
- Event Recording
- High-performance playback
- Zero runtime reflection
- Minimal GC allocations
- Optional AnimationClip export
- UPM package
- Cross-platform support
- Extensible architecture

---

## Supported Platforms

- Windows
- macOS
- Linux
- Android
- iOS
- WebGL
- Meta Quest
- OpenXR
- XR Interaction Toolkit

---

## Supported Unity Versions

- Unity 2020.3 LTS+
- Unity 2021 LTS
- Unity 2022 LTS
- Unity 6+

---

## Installation

In Unity, open **Window ▸ Package Manager ▸ + ▸ Add package from git URL…** and paste:

```
https://github.com/thisisajaikumar/MotionKit.git
```

To install a specific pinned version, append the git tag:

```
https://github.com/thisisajaikumar/MotionKit.git#1.0.0
```

Or install as an embedded package.

---

## Quick Start

1. Open **Tools → MotionKit → Recorder**
2. Add one or more GameObjects.
3. Press **Record**.
4. Perform the interaction.
5. Press **Stop**.
6. Save the Motion asset.
7. Attach **MotionPlayer** and play the recording.

---

## Philosophy

MotionKit focuses on:

- Performance
- Simplicity
- Minimal runtime overhead
- Production-ready architecture

Every feature must justify its runtime cost.

---

## Roadmap

### Version 1

- Transform Recording
- Active State Recording
- Event Recording
- AnimationClip Export

### Version 2

- Animator Module
- UI Module
- Physics Module
- Meta SDK Integration
- OpenXR Module

### Version 3

- Multiplayer Recording
- Addressables Support
- Recording Compression Improvements
- Cloud Sync

---

## Contributing

Contributions, bug reports, and feature requests are welcome.

Please open an issue before submitting major changes.

---

## License

MIT License

---

Made with ❤️ by **Ajoy Games**
