"""Production Career Batch A source and Unity-authoring contracts."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]


def read(path):
    return (ROOT / path).read_text(encoding="utf-8")


class CareerContentBatchATests(unittest.TestCase):
    def test_gdd_batch_a_is_data_driven_and_preserves_developer_source(self):
        source = read("Assets/StartupLife/Scripts/Content/FirstPlayableContentTemplate.cs")
        self.assertIn("MarketingCareer(),", source)
        self.assertIn("SalesCareer(),", source)
        self.assertIn("OfficeAdminCareer()", source)
        self.assertIn('Id = "marketing"', source)
        self.assertIn('Id = "sales"', source)
        self.assertIn('Id = "office-admin"', source)
        self.assertIn('NameKey = "career.marketing"', source)
        self.assertIn('NameKey = "career.sales"', source)
        self.assertIn('NameKey = "career.office-admin"', source)
        self.assertIn('ProfessionScene("marketing-campaign-planning", 25', source)
        self.assertIn('ProfessionScene("marketing-analytics", 20', source)
        self.assertIn('ProfessionScene("sales-negotiation", 20', source)
        self.assertIn('ProfessionScene("office-document-processing", 25', source)
        self.assertIn('Rank("senior", 3000, 90, 18000000', source)
        self.assertIn('Rank("senior", 3000, 90, 16000000', source)
        self.assertIn('Rank("senior", 3000, 90, 14000000', source)
        self.assertIn('Version = "first-playable.v1"', source)
        self.assertIn('Id = "developer"', source)
        self.assertIn('MonthlySalary = 10000000', source)
        self.assertIn('MonthlySalary = 15000000', source)

    def test_content_builder_rebuilds_asset_and_checks_career_and_business_catalogs(self):
        builder = read("Assets/StartupLife/Scripts/Editor/FirstPlayableContentBuilder.cs")
        self.assertIn("FirstPlayableContentTemplate.Create()", builder)
        self.assertIn("RefreshCareerCatalogAndLocalization()", builder)
        self.assertIn("DashboardLocalizationEditorSync.SyncVietnamese()", builder)
        self.assertIn("catalog.Businesses.Count != 4", builder)
        self.assertIn("catalog.Careers.Count != 6", builder)
        self.assertIn('catalog.Careers["marketing"]', builder)
        self.assertIn('catalog.Careers["sales"]', builder)
        self.assertIn('catalog.Careers["office-admin"]', builder)
        self.assertIn("VerifyCareer(", builder)
        self.assertIn("RequiredXp", builder)
        self.assertIn("RequiredServiceDays", builder)
        self.assertIn("GrantSkillId", builder)
        self.assertIn("GrantLevel", builder)
        self.assertIn("catalog.Businesses[FirstPlayableContentTemplate.OnlineStoreId]", builder)
        self.assertIn("catalog.Businesses[FirstPlayableContentTemplate.HomeFoodPreorderId]", builder)

    def test_career_ui_and_work_scene_strings_are_authored(self):
        ui = read("Assets/StartupLife/Scripts/Presentation/LifeDashboardOverlay.cs")
        self.assertIn("DashboardLocalizationCatalog.TryFindValue(key, out var value)", ui)
        table = read("Assets/StartupLife/Scripts/Presentation/DashboardLocalizationCatalog.cs")
        for key in ("career.developer", "career.marketing", "career.sales", "career.office-admin"):
            self.assertIn(f'new KeyValuePair<string,string>("{key}"', table)
        for key in (
            "scene.marketing-campaign-planning",
            "scene.marketing-content-review",
            "scene.marketing-analytics",
            "scene.marketing-team-meeting",
            "scene.marketing-client-presentation",
            "scene.marketing-partner-communication",
            "scene.sales-prospecting",
            "scene.sales-needs-discovery",
            "scene.sales-product-demo",
            "scene.sales-negotiation",
            "scene.sales-follow-up",
            "scene.sales-sales-report",
            "scene.office-document-processing",
            "scene.office-schedule-coordination",
            "scene.office-data-review",
            "scene.office-internal-support",
            "scene.office-record-keeping",
            "scene.office-team-briefing",
        ):
            self.assertIn(f'new KeyValuePair<string,string>("{key}"', table)
        self.assertIn("public static bool TryFindValue(string key, out string value)", table)

    def test_engine_free_catalog_checks_and_unity_asset_gate_cover_batch_a(self):
        checks = read("tools/ContentCatalogChecks/Program.cs")
        self.assertIn("Equal(6, catalog.Careers.Count)", checks)
        self.assertIn('var marketing = catalog.Careers["marketing"]', checks)
        self.assertIn('var sales = catalog.Careers["sales"]', checks)
        self.assertIn('var office = catalog.Careers["office-admin"]', checks)
        self.assertIn("QuotaScheduler.Order(expected", checks)
        unity = read("Assets/StartupLife/Tests/EditMode/ContentCatalogTests.cs")
        self.assertIn("catalog.Careers.Count, Is.EqualTo(6)", unity)
        self.assertIn('catalog.Careers["marketing"]', unity)
        self.assertIn('catalog.Careers["sales"]', unity)
        self.assertIn('catalog.Careers["office-admin"]', unity)


if __name__ == "__main__":
    unittest.main()
