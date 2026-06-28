using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using AjoyGames.MotionKit;
using AjoyGames.MotionKit.Data;
using AjoyGames.MotionKit.Migration;
using AjoyGames.MotionKit.Playback;
using AjoyGames.MotionKit.SDK;
using AjoyGames.MotionKit.Tracks;

namespace AjoyGames.MotionKit.Tests
{
    public sealed class MotionKitEditModeTests
    {
        [Test]
        public void Compression_HalfRoundTrip_IsApproximatelyLossless()
        {
            foreach (float value in new[] { 0f, 1f, -1f, 0.5f, 123.25f, -0.001953125f })
            {
                ushort half = CompressionUtility.FloatToHalf(value);
                float back = CompressionUtility.HalfToFloat(half);
                Assert.That(back, Is.EqualTo(value).Within(0.01f), "half round-trip for " + value);
            }
        }

        [Test]
        public void Compression_ReduceLinearKeys_RemovesCollinearInterior()
        {
            var times = new List<float> { 0f, 1f, 2f, 3f, 4f };
            var values = new List<float> { 0f, 1f, 2f, 3f, 4f };
            int count = CompressionUtility.ReduceLinearKeys(times, values, 0.0001f);
            Assert.AreEqual(2, count, "linear ramp should collapse to two endpoints");
            Assert.AreEqual(0f, values[0]);
            Assert.AreEqual(4f, values[1]);
        }

        [Test]
        public void KeyframeSearch_FindsCorrectSegment()
        {
            float[] times = { 0f, 1f, 2f, 3f };
            int hint = 0;
            Assert.AreEqual(0, KeyframeSearch.FindLower(times, 4, 0.5f, ref hint));
            Assert.AreEqual(2, KeyframeSearch.FindLower(times, 4, 2.5f, ref hint));
            Assert.AreEqual(3, KeyframeSearch.FindLower(times, 4, 99f, ref hint));
            Assert.AreEqual(0.5f, KeyframeSearch.SegmentT(times, 4, 0, 0.5f), 1e-5f);
        }

        [Test]
        public void Registry_CreatesPlayerForTransformTrack()
        {
            ModuleRegistry.RegisterTrackModule(new TransformTrackModule());
            var data = new TransformTrackData();
            ITrackPlayer player = ModuleRegistry.CreatePlayerFor(data);
            Assert.IsNotNull(player);
            Assert.IsInstanceOf<TransformTrackPlayer>(player);
        }

        [Test]
        public void BindingResolver_ComputesAndResolvesPath()
        {
            var root = new GameObject("Root");
            var child = new GameObject("Child");
            child.transform.SetParent(root.transform);

            Assert.AreEqual("Child", BindingResolver.ComputePath(root.transform, child.transform));

            var resolver = new BindingResolver();
            resolver.Build(root);
            Assert.AreSame(child, resolver.ResolveByPath("Child"));
            Assert.AreSame(root, resolver.ResolveByPath(""));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Playback_EvaluatesTransformTrack()
        {
            ModuleRegistry.RegisterTrackModule(new TransformTrackModule());

            var go = new GameObject("Target");
            var rec = ScriptableObject.CreateInstance<InteractionRecording>();
            rec.Fps = 10;
            rec.Version = RecordingVersion.CreateCurrent(Application.unityVersion);

            var td = new TransformTrackData();
            td.SetData(true,
                new[] { 0f, 1f },
                new[] { Vector3.zero, new Vector3(0f, 5f, 0f) },
                new[] { Quaternion.identity, Quaternion.identity },
                new[] { Vector3.one, Vector3.one });

            var ro = new RecordedObject { DisplayName = "Target", PersistentId = "", HierarchyPath = "" };
            ro.Tracks.Add(td);
            rec.Objects.Add(ro);
            rec.RecalculateDuration();

            var engine = new InteractionPlaybackEngine();
            Assert.IsTrue(engine.Prepare(rec, go));
            engine.Evaluate(0.5f);

            Assert.That(go.transform.localPosition.y, Is.EqualTo(2.5f).Within(0.001f));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(rec);
        }

        [Test]
        public void Migration_UpgradesOldRecordingToCurrentVersion()
        {
            ModuleRegistry.RegisterMigration(new Migration_0_to_1());

            var rec = ScriptableObject.CreateInstance<InteractionRecording>();
            rec.RecordingName = "Legacy";
            rec.Version = new RecordingVersion { FileVersion = 0 };

            bool changed = RecordingMigrator.EnsureUpToDate(rec);
            Assert.IsTrue(changed);
            Assert.AreEqual(RecordingVersion.CurrentFileVersion, rec.Version.FileVersion);

            Object.DestroyImmediate(rec);
        }

        [Test]
        public void Playback_EvaluatesEveryRecordedObject()
        {
            ModuleRegistry.RegisterTrackModule(new TransformTrackModule());

            var root = new GameObject("Root");
            var a = new GameObject("A"); a.transform.SetParent(root.transform);
            var b = new GameObject("B"); b.transform.SetParent(root.transform);
            string idA = a.AddComponent<PersistentObjectId>().EnsureId();
            string idB = b.AddComponent<PersistentObjectId>().EnsureId();

            var rec = ScriptableObject.CreateInstance<InteractionRecording>();
            rec.Fps = 10;
            rec.Version = RecordingVersion.CreateCurrent(Application.unityVersion);
            rec.Objects.Add(MakeTransformObject("A", idA, "Root/A", new Vector3(0f, 5f, 0f)));
            rec.Objects.Add(MakeTransformObject("B", idB, "Root/B", new Vector3(0f, 0f, 7f)));
            rec.RecalculateDuration();

            var engine = new InteractionPlaybackEngine();
            Assert.IsTrue(engine.Prepare(rec, root));
            engine.Evaluate(0.5f);

            // Both recorded objects must move independently — this is the multi-object regression guard.
            Assert.That(a.transform.localPosition.y, Is.EqualTo(2.5f).Within(0.001f), "object A did not play");
            Assert.That(b.transform.localPosition.z, Is.EqualTo(3.5f).Within(0.001f), "object B did not play");

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(rec);
        }

        private static RecordedObject MakeTransformObject(string name, string id, string path, Vector3 endPosition)
        {
            var td = new TransformTrackData();
            td.SetData(true,
                new[] { 0f, 1f },
                new[] { Vector3.zero, endPosition },
                new[] { Quaternion.identity, Quaternion.identity },
                new[] { Vector3.one, Vector3.one });
            var ro = new RecordedObject { DisplayName = name, PersistentId = id, HierarchyPath = path };
            ro.Tracks.Add(td);
            return ro;
        }
    }
}
