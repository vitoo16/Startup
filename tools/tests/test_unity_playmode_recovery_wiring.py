"""Source-level guard for the 2026-11-19 local Unity PlayMode triage.

This is NOT Unity compilation or proof that 24/24 PlayMode tests pass.
It preserves the fail-closed save path, visible V4 choice resolution,
explicit editor diagnostic expectations and strict responsive assertions.
"""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]


def read(relative):
    return (ROOT / relative).read_text(encoding="utf-8")


class UnityPlayModeRecoveryWiringTests(unittest.TestCase):
    def test_bad_saves_fail_closed_with_localized_recovery(self):
        selector = read("Assets/StartupLife/Scripts/Infrastructure/VerifiedBusinessContentSelector.cs")
        bootstrap = read("Assets/StartupLife/Scripts/Presentation/StartupLifeBootstrapper.cs")
        self.assertIn("UnverifiedBusinessContentException : InvalidOperationException", selector)
        self.assertIn("throw new UnverifiedBusinessContentException();", selector)
        self.assertIn("catch (UnverifiedBusinessContentException)", bootstrap)
        self.assertIn('Fail("reason.save.unrecoverable");', bootstrap)
        self.assertIn("enableVerifiedEventV4 = true;", bootstrap)
        self.assertIn("automaticDailyChecks: true", bootstrap)
        self.assertIn("enableBusinessBatchAForNewRuns = false;", bootstrap)
        self.assertNotIn("File.WriteAllBytes", selector)

    def test_save_boot_logs_are_explicit_not_suppressed(self):
        diagnostic = read("Assets/StartupLife/Tests/PlayMode/SaveBootLogExpectations.cs")
        lifecycle = read("Assets/StartupLife/Tests/PlayMode/FirstPlayableLifecycleTests.cs")
        flow = read("Assets/StartupLife/Tests/PlayMode/FirstPlayableFlowTests.cs")
        self.assertIn("LogAssert.Expect(LogType.Log, VerifiedRead);", diagnostic)
        self.assertIn("LogAssert.Expect(LogType.Log, new Regex(", diagnostic)
        self.assertIn("status=Unreadable", diagnostic)
        self.assertIn("SaveBootLogExpectations.ExpectUnreadablePrimary();", lifecycle)
        self.assertIn("SaveBootLogExpectations.ExpectVerifiedRead();", lifecycle)
        self.assertIn("SaveBootLogExpectations.ExpectVerifiedRead();", flow)
        self.assertNotIn("LogAssert.ignoreFailingMessages", diagnostic)
        self.assertNotIn("LogAssert.ignoreFailingMessages", lifecycle)
        self.assertNotIn("LogAssert.ignoreFailingMessages", flow)

    def test_day_advances_resume_genuine_player_choice_via_ui(self):
        harness = read("Assets/StartupLife/Tests/PlayMode/PlayModeEventAdvanceHarness.cs")
        lifecycle = read("Assets/StartupLife/Tests/PlayMode/FirstPlayableLifecycleTests.cs")
        flow = read("Assets/StartupLife/Tests/PlayMode/FirstPlayableFlowTests.cs")
        self.assertIn('Is.EqualTo("PlayerChoice")', harness)
        self.assertIn('"EventChoiceButton"', harness)
        self.assertIn("choice.onClick.Invoke();", harness)
        self.assertIn("MaxChoicesPerDay", harness)
        self.assertIn("bootstrap.Flow.AdvanceDay().Advance", harness)
        self.assertIn('GameObject.Find("AdvanceDayButton")', harness)
        self.assertIn("PlayModeEventAdvanceHarness.ThroughFlow(", lifecycle)
        self.assertIn("PlayModeEventAdvanceHarness.ThroughView(bootstrap)", flow)
        self.assertNotIn("automaticDailyChecks: false", harness)

    def test_localization_and_physical_resolution_gates_remain_strict(self):
        runtime = read("Assets/StartupLife/Scripts/Presentation/LifeDashboardOverlay.cs")
        flow = read("Assets/StartupLife/Tests/PlayMode/FirstPlayableFlowTests.cs")
        self.assertIn('"\\nNợ cần trả: "', runtime)
        self.assertIn('Does.Contain("Tiền mặt").And.Contain("Nợ cần trả")', flow)
        for name in (
            "FirstPlayableMainMenuResponsiveTests.cs",
            "CharacterCreatorResponsiveMatrixTests.cs",
        ):
            source = read("Assets/StartupLife/Tests/PlayMode/" + name)
            self.assertIn("Screen.SetResolution(", source)
            self.assertIn("Assert.That(Screen.width, Is.EqualTo(", source)
            self.assertIn("Assert.That(Screen.height, Is.EqualTo(", source)
            self.assertNotIn("Assert.Ignore(", source)
            self.assertNotIn("LogAssert.ignoreFailingMessages", source)


if __name__ == "__main__":
    unittest.main()
