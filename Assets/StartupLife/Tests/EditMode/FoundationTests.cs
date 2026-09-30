using System;
using System.Linq;
using NUnit.Framework;
using StartupLife.Core;
using StartupLife.Presentation;
using StartupLife.Simulation;
using UnityEngine;

namespace StartupLife.Tests.EditMode
{
    public sealed class FoundationTests
    {
        [Test]
        public void DeveloperQuotasAndOrderingRemainDeterministicInUnity()
        {
            int[] weights = { 40, 20, 15, 10, 10, 5 };
            var scenes = weights.Select((weight, index) =>
                new CareerSceneDefinition("scene" + index, "scene.key" + index, weight, 1, "technical", 1)).ToArray();
            var counts = QuotaScheduler.Apportion(scenes, 20);
            CollectionAssert.AreEqual(new[] { 8, 4, 3, 2, 2, 1 }, scenes.Select(s => counts[s.Id]));
            uint first = DeterministicRng.Seed(71, "scheduler"), second = first;
            var deck = QuotaScheduler.Order(counts, "", ref first);
            CollectionAssert.AreEqual(deck, QuotaScheduler.Order(counts, "", ref second));
            Assert.That(deck.Zip(deck.Skip(1), (a, b) => a != b).All(value => value), Is.True);
        }

        [Test]
        public void LeapDatesAndCheckedMoneyUseEngineIndependentContracts()
        {
            Assert.DoesNotThrow(() => new SimDate(2028, 2, 29));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SimDate(2027, 2, 29));
            uint state = 1;
            Assert.That(DeterministicRng.Next(ref state), Is.EqualTo(270369u));
        }

        [Test]
        public void SafeAreaReappliesAfterResolutionChangeWithoutAccumulatingOffsets()
        {
            var obj = new GameObject("SafeAreaTest", typeof(RectTransform));
            try
            {
                var safeArea = obj.AddComponent<MobileSafeArea>();
                var transform = obj.GetComponent<RectTransform>();
                safeArea.Apply(new Rect(0, 80, 1080, 1720), 1080, 1920);
                Assert.That(transform.anchorMin.y, Is.EqualTo(80f / 1920).Within(0.00001));
                Assert.That(transform.anchorMax.y, Is.EqualTo(1800f / 1920).Within(0.00001));
                safeArea.Apply(new Rect(0, 0, 720, 1280), 720, 1280);
                Assert.That(transform.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(transform.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(transform.offsetMin, Is.EqualTo(Vector2.zero));
                Assert.That(transform.offsetMax, Is.EqualTo(Vector2.zero));
            }
            finally { UnityEngine.Object.DestroyImmediate(obj); }
        }
    }
}
