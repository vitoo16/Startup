"""Career dashboard selection must use active catalog, not demo-only developer.

Engine-free source joins supplement deterministic SimulationChecks; neither
claims Unity Editor/PlayMode or physical-device visual evidence.
"""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]


def source(path):
    return (ROOT / path).read_text(encoding="utf-8")


class CareerDashboardWiringTests(unittest.TestCase):
    def test_catalog_offers_and_hiring_are_delegated_to_existing_commands(self):
        model = source("Assets/StartupLife/Scripts/Application/LifeDashboardProjection.cs")
        policy = source("Assets/StartupLife/Scripts/Application/CareerSelectionPolicy.cs")
        view = source("Assets/StartupLife/Scripts/Presentation/LifeScreenViewController.cs")
        ui = source("Assets/StartupLife/Scripts/Presentation/LifeDashboardOverlay.cs")
        self.assertIn("public IReadOnlyList<CareerDashboardItem> Careers { get; }", model)
        self.assertIn("content.Careers.Values.OrderBy(x => x.Id, StringComparer.Ordinal)", model)
        self.assertIn("MonthlySalariesVnd = Array.AsReadOnly(definition.Ranks.Select(x => x.MonthlySalary).ToArray())", model)
        self.assertIn("string.Join(\" → \", selectedOffer.MonthlySalariesVnd.Select(Money))", ui)
        self.assertIn("FormatWorkDays(selectedOffer.WorkDays)", ui)
        self.assertIn("CareerBusinessAffinityPolicy.ForCareer(definition.Id, content)", model)
        self.assertIn("FormatPreferredBusinesses(selectedOffer.PreferredBusinessIds)", ui)
        self.assertIn("DashboardLocalizationCatalog.TryFindValue(key, out var value)", ui)
        self.assertIn("label.text = title;", ui)
        self.assertIn("private static string FormatWorkDays(IReadOnlyList<DayOfWeek> days)", ui)
        self.assertIn("WorkDays = Array.AsReadOnly(definition.WorkDays.ToArray())", model)
        self.assertIn("CareerSelectionPolicy.ChooseCareerId(model, selectedCareerId)", ui)
        self.assertIn("CareerSelectionPolicy.Evaluate(model, selectedOffer)", ui)
        self.assertIn("SyncCareerOptions(model.Careers)", ui)
        self.assertIn("host.AcceptCareer(selectedCareerId)", ui)
        self.assertIn("content.Careers.ContainsKey(careerId)", view)
        self.assertIn("flow.AcceptJob(careerId).Command", view)
        self.assertIn("public void AcceptDeveloper() => AcceptCareer(\"developer\")", view)
        self.assertIn("dashboard.Career.CurrentEmployment?.CareerId", policy)
        self.assertIn("dashboard.Career.IsEmployed", policy)
        self.assertNotIn("state.Employment =", policy)
        self.assertNotIn("PlayerPrefs", policy)
        self.assertNotIn("careerPage, host.AcceptDeveloper", ui)
        self.assertNotIn("Nhận việc lập trình", ui)

    def test_shared_picker_reuses_course_ui_and_prevents_leaks_on_catalog_refresh(self):
        ui = source("Assets/StartupLife/Scripts/Presentation/LifeDashboardOverlay.cs")
        self.assertIn("SyncSelectionOptions(offers, careerChoices, careerButtons, careerButtonLabels", ui)
        self.assertIn("SyncSelectionOptions(catalog, courseChoices, courseButtons, courseButtonLabels", ui)
        self.assertIn("private void SyncSelectionOptions<T>(", ui)
        self.assertIn("buttons.TryGetValue(id, out var button)", ui)
        self.assertIn("buttons.Add(capturedId, button)", ui)
        self.assertIn("originalFontSizes.Remove(labels[oldId]);", ui)
        self.assertIn("Destroy(previous.gameObject);", ui)
        self.assertIn("careerChoices = Node(\"LifeCareerChoiceList\"", ui)
        self.assertIn("courseChoices = Node(\"LifeCourseChoiceList\"", ui)
        self.assertIn("option.interactable = !career.IsEmployed", ui)
        self.assertIn("private const float TouchHeight = 96f;", ui)
        self.assertIn("MobileScrollViewFactory.Create(", ui)

    def test_career_timeline_reads_only_committed_history(self):
        biography = source("Assets/StartupLife/Scripts/Application/CareerBiographyProjection.cs")
        policy = source("Assets/StartupLife/Scripts/Application/CareerSelectionPolicy.cs")
        ui = source("Assets/StartupLife/Scripts/Presentation/LifeDashboardOverlay.cs")
        sim = source("Assets/StartupLife/Scripts/Simulation/SimulationEngine.cs")
        self.assertIn("EmploymentPeriodCount", biography)
        self.assertIn("DistinctProfessionCount", biography)
        self.assertIn("ProfessionChangeCount", biography)
        self.assertIn("ElapsedCalendarDays", biography)
        self.assertIn("new CareerTenureSnapshot(x, content, claims, state.DateIso)", biography)
        self.assertIn("CareerTransitionContext.ReturningProfession", policy)
        self.assertIn("CareerTransitionContext.ChangingProfession", policy)
        self.assertIn("career.PreviousEmployments.Reverse()", ui)
        self.assertIn("job.UnpaidClaimCount > 0", ui)
        self.assertIn("job.MonthlySalaryAtRecordedRankVnd.HasValue", ui)
        self.assertIn("CareerSelectionPolicy.TransitionContext(model, selectedOffer)", ui)
        self.assertNotIn("state.Employment =", biography)
        self.assertNotIn("state.Employment =", policy)
        self.assertIn("state.PreviousEmployment.Add(state.Employment)", sim)

    def test_existing_saves_and_frozen_gameplay_are_unchanged(self):
        meta = source("Assets/StartupLife/Scripts/Application/CareerSelectionPolicy.cs.meta")
        self.assertIn("guid: 885ed96afc82416ca2960c382974b429", meta)
        sim = source("Assets/StartupLife/Scripts/Simulation/SimulationEngine.cs")
        self.assertIn('if (state.Employment != null) Fail("career.already_employed")', sim)
        self.assertIn('if (!content.Careers.TryGetValue(id, out var career)) Fail("content.missing")', sim)
        flow = source("Assets/StartupLife/Scripts/Presentation/FirstPlayableFlow.cs")
        self.assertIn("public FirstPlayableActionResult AcceptJob(string careerId)", flow)
        self.assertIn("new GameCommand(CommandKind.AcceptJob, careerId)", flow)


if __name__ == "__main__":
    unittest.main()
