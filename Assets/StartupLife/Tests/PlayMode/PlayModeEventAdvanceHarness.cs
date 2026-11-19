using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StartupLife.Core;
using StartupLife.Presentation;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace StartupLife.Tests.PlayMode
{
    // Exercises the actual V4 choice modal and resumed day batch. A pending
    // event is a valid interruption, never permission to skip a time boundary.
    internal static class PlayModeEventAdvanceHarness
    {
        private const int MaxChoicesPerDay = 8;

        internal static IEnumerator ThroughFlow(
            StartupLifeBootstrapper bootstrap, Action<AdvanceResult> completed)
        {
            Assert.That(bootstrap, Is.Not.Null);
            var startingDate = bootstrap.Snapshot.Instant.Date;
            for (var attempt = 0; attempt < MaxChoicesPerDay; attempt++)
            {
                var result = bootstrap.Flow.AdvanceDay().Advance;
                bootstrap.RefreshMode();
                if (result.StopReason == "TargetReached")
                {
                    Assert.That(bootstrap.Snapshot.Instant.Date,
                        Is.EqualTo(startingDate.AddDays(1)));
                    completed(result);
                    yield break;
                }
                Assert.That(result.StopReason, Is.EqualTo("PlayerChoice"),
                    "A V4 day may pause for a choice, but not silently fail: " +
                    result.StopReason);
                yield return ResolveVisibleChoice(bootstrap);
            }
            Assert.Fail("V4 day did not finish after resolving bounded event choices.");
        }

        internal static IEnumerator ThroughView(StartupLifeBootstrapper bootstrap)
        {
            Assert.That(bootstrap, Is.Not.Null);
            var startingDate = bootstrap.Snapshot.Instant.Date;
            for (var attempt = 0; attempt < MaxChoicesPerDay; attempt++)
            {
                var advance = GameObject.Find("AdvanceDayButton")?.GetComponent<Button>();
                Assert.That(advance, Is.Not.Null);
                Assert.That(advance.interactable, Is.True);
                advance.onClick.Invoke();
                yield return null;
                if (bootstrap.Snapshot.Instant.Date.Equals(startingDate.AddDays(1)))
                    yield break;
                Assert.That(bootstrap.Snapshot.Instant.Date, Is.EqualTo(startingDate),
                    "A blocked day cannot quietly advance multiple calendar dates.");
                yield return ResolveVisibleChoice(bootstrap);
            }
            Assert.Fail("Day advance button did not finish after resolving bounded event choices.");
        }

        private static IEnumerator ResolveVisibleChoice(StartupLifeBootstrapper bootstrap)
        {
            bootstrap.RefreshMode();
            var overlay = UnityEngine.Object.FindAnyObjectByType<EventChoiceOverlay>();
            Assert.That(overlay, Is.Not.Null,
                "V4 PlayerChoice must be accompanied by the real pending choice modal.");
            Assert.That(overlay.IsOpen, Is.True);

            Button choice = null;
            var deadline = Time.realtimeSinceStartup + 3f;
            do
            {
                yield return null;
                // Current authored stories put the defer/decline option last.
                // It remains a genuine gameplay CAS decision, not a synthetic
                // bypass of the event checkpoint.
                choice = overlay.GetComponentsInChildren<Button>(true)
                    .Where(x => x.name == "EventChoiceButton" &&
                        x.gameObject.activeInHierarchy && x.interactable)
                    .LastOrDefault();
            } while (choice == null && Time.realtimeSinceStartup < deadline);

            Assert.That(choice, Is.Not.Null,
                "No selectable, fully revealed V4 event choice was exposed.");
            choice.onClick.Invoke();
            yield return null;
            Assert.That(overlay.IsOpen, Is.False,
                "V4 choice resolution must dismiss the actual pending modal.");
        }
    }
}
