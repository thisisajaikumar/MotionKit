using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using AjoyGames.MotionKit;
using AjoyGames.MotionKit.Data;
using AjoyGames.MotionKit.Playback;
using AjoyGames.MotionKit.Tracks;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AjoyGames.MotionKit.Tests
{
    public sealed class MotionKitPlayModeTests
    {
        [Test]
        public void Evaluate_IsAllocationFree()
        {
            ModuleRegistry.RegisterTrackModule(new TransformTrackModule());

            var go = new GameObject("Target");
            var rec = ScriptableObject.CreateInstance<InteractionRecording>();
            rec.Fps = 30;
            rec.Version = RecordingVersion.CreateCurrent(Application.unityVersion);

            var td = new TransformTrackData();
            td.SetData(true,
                new[] { 0f, 0.5f, 1f },
                new[] { Vector3.zero, Vector3.up, Vector3.up * 2f },
                new[] { Quaternion.identity, Quaternion.identity, Quaternion.identity },
                new[] { Vector3.one, Vector3.one, Vector3.one });

            var ro = new RecordedObject { DisplayName = "Target", PersistentId = "", HierarchyPath = "" };
            ro.Tracks.Add(td);
            rec.Objects.Add(ro);
            rec.RecalculateDuration();

            var engine = new InteractionPlaybackEngine();
            Assert.IsTrue(engine.Prepare(rec, go));

            engine.Evaluate(0.3f); // warm up cursor / JIT

            Assert.That(() => engine.Evaluate(0.7f), Is.Not.AllocatingGCMemory());

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(rec);
        }
    }
}
