"""Regression guards for Chef, Barista, six-skill courses and advisory affinity.

This source-level suite supplements the real .NET catalog/presentation checks,
not a substitute for Unity Editor, PlayMode or economic replay approval.
"""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

def src(path):
    return (ROOT / path).read_text(encoding="utf-8")


class CareerContentBatchBTests(unittest.TestCase):
    def test_two_distinct_professions_and_six_scene_quota_source(self):
        source = src("Assets/StartupLife/Scripts/Content/FirstPlayableContentTemplate.cs")
        self.assertIn("ChefCareer(),", source)
        self.assertIn("BaristaCareer()", source)
        for snippet in (
            'Id = "chef"', 'Id = "barista"',
            'NameKey = "career.chef"', 'NameKey = "career.barista"',
            'ProfessionScene("chef-preparation", 25, "time-management", 1)',
            'ProfessionScene("chef-cooking", 30, "problem-solving", 1)',
            'ProfessionScene("chef-plating", 10, "time-management", 1)',
            'ProfessionScene("chef-team-coordination", 10, "leadership", 1)',
            'ProfessionScene("chef-supplier-discussion", 10, "negotiation", 1)',
            'ProfessionScene("chef-rush-period", 15, "time-management", 2)',
            'ProfessionScene("barista-opening-prep", 20, "time-management", 1)',
            'ProfessionScene("barista-drink-preparation", 30, "time-management", 1)',
            'ProfessionScene("barista-customer-orders", 20, "communication", 2)',
            'ProfessionScene("barista-payment-handoff", 10, "communication", 1)',
            'ProfessionScene("barista-restocking", 10, "problem-solving", 1)',
            'ProfessionScene("barista-regulars", 10, "networking", 2)',
            'Rank("senior", 3000, 90, 17500000, "leadership", 2)',
            'Rank("senior", 3000, 90, 13000000, "networking", 2)',
            "WorkDays = TuesdayToSunday()",
        ):
            self.assertIn(snippet, source)

    def test_all_six_transferable_skills_have_study_courses(self):
        source = src("Assets/StartupLife/Scripts/Content/FirstPlayableContentTemplate.cs")
        for snippet in (
            'Id = "communication-basics"', 'Id = "problem-course"',
            'StarterCourse("negotiation-basics", "negotiation", 420, 220000)',
            'StarterCourse("time-management-basics", "time-management", 360, 180000)',
            'StarterCourse("networking-basics", "networking", 480, 250000)',
            'StarterCourse("leadership-basics", "leadership", 540, 300000)',
        ):
            self.assertIn(snippet, source)
        builder = src("Assets/StartupLife/Scripts/Editor/FirstPlayableContentBuilder.cs")
        self.assertIn("catalog.Careers.Count != 6", builder)
        self.assertIn("catalog.Courses.Count != 6", builder)
        self.assertIn('catalog.Careers["chef"]', builder)
        self.assertIn('catalog.Careers["barista"]', builder)

    def test_advisory_affinity_is_data_driven_and_does_not_change_economy(self):
        model = src("Assets/StartupLife/Scripts/Application/LifeDashboardProjection.cs")
        self.assertIn("public static class CareerBusinessAffinityPolicy", model)
        self.assertIn("career.BusinessAffinityTags.Count == 0", model)
        self.assertIn("x.BusinessAffinityTags.Any(tags.Contains)", model)
        self.assertIn("OrderBy(x => x, StringComparer.Ordinal)", model)
        self.assertNotIn("private static readonly IReadOnlyDictionary<string, string[]> Preferred", model)
        source = src("Assets/StartupLife/Scripts/Content/FirstPlayableContentTemplate.cs")
        self.assertIn('BusinessAffinityTags = new[] { "food", "supplier-operations" }', source)
        self.assertIn('BusinessAffinityTags = new[] { "food", "customer-experience" }', source)
        self.assertIn('BusinessAffinityTags = businessAffinityTags', source)
        self.assertIn("BusinessAffinityTags = Array.Empty<string>()", src(
            "Assets/StartupLife/Scripts/Content/ContentCatalogSource.cs"))
        self.assertIn("ContentAffinityTags.Validate(businessAffinityTags", src(
            "Assets/StartupLife/Scripts/Core/Definitions.cs"))
        self.assertIn("ContentAffinityTags.Validate(businessAffinityTags", src(
            "Assets/StartupLife/Scripts/Core/Business.cs"))
        ui = src("Assets/StartupLife/Scripts/Presentation/LifeDashboardOverlay.cs")
        self.assertIn("FormatPreferredBusinesses(selectedOffer.PreferredBusinessIds)", ui)
        self.assertNotIn("CareerBusinessAffinityPolicy", src(
            "Assets/StartupLife/Scripts/Simulation/SimulationEngine.cs"))
        checks = src("tools/ContentCatalogChecks/Program.cs")
        self.assertIn("CareerBusinessAffinityPolicy.ForCareer", checks)
        self.assertIn("CareerBusinessAffinityPolicy.IsPreferred", checks)

    def test_localized_career_scenes_and_courses(self):
        catalog = src("Assets/StartupLife/Scripts/Presentation/DashboardLocalizationCatalog.cs")
        keys = [
            "career.chef", "career.barista",
            "scene.chef-preparation", "scene.chef-cooking", "scene.chef-plating",
            "scene.chef-team-coordination", "scene.chef-supplier-discussion",
            "scene.chef-rush-period", "scene.barista-opening-prep",
            "scene.barista-drink-preparation", "scene.barista-customer-orders",
            "scene.barista-payment-handoff", "scene.barista-restocking",
            "scene.barista-regulars", "course.negotiation",
            "course.time-management", "course.networking", "course.leadership",
        ]
        for key in keys:
            self.assertIn(f'new KeyValuePair<string,string>("{key}"', catalog)
        for key in ("course.none", "course.communication", "course.problem",
                    "course.negotiation", "course.time-management", "course.networking",
                    "course.leadership"):
            self.assertIn(f'new KeyValuePair<string,string>("{key}"', catalog)
        ui = src("Assets/StartupLife/Scripts/Presentation/LifeDashboardOverlay.cs")
        self.assertIn("DashboardLocalizationCatalog.TryFindValue(key, out var text)", ui)
        self.assertNotIn('"course.communication" =>', ui)
        shell = src("Assets/StartupLife/Scripts/Editor/FirstPlayableShellBuilder.cs")
        self.assertNotIn('Add(table, "course.communication"', shell)
        self.assertNotIn('Add(table, "course.problem"', shell)

    def test_frozen_v2_fingerprint_still_filters_original_catalog_only(self):
        checks = src("tools/EconomicChecks/Program.cs")
        self.assertIn('x.Id=="developer"', checks)
        self.assertIn('x.Id=="communication-basics" || x.Id=="problem-course"', checks)
        self.assertIn('x.Id=="freelance-service" || x.Id=="coffee-kiosk"', checks)


if __name__ == "__main__":
    unittest.main()
