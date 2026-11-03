"""Read-only business catalog launch UI wiring (no Unity runtime required).

Source joins complement the authoritative M8/M9 economic and save suites.
They do not prove mobile layout, Unity compilation or launch eligibility.
"""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def read(path):
    return (ROOT / path).read_text(encoding="utf-8")


class BusinessCatalogLaunchWiringTests(unittest.TestCase):
    def test_launch_controls_are_created_from_authoritative_catalog(self):
        ui = read("Assets/StartupLife/Scripts/Presentation/BusinessManagementOverlay.cs")
        projection = read("Assets/StartupLife/Scripts/Application/BusinessOperationsProjection.cs")
        self.assertIn("SyncLaunchOptions(operations.LaunchOptions);", ui)
        self.assertIn("offers.Select(x => x.DefinitionId)", ui)
        self.assertIn("launchButtons.TryGetValue(offer.DefinitionId", ui)
        self.assertIn("button.transform.SetSiblingIndex(index);", ui)
        self.assertIn("launchButtons.Remove(removed);", ui)
        self.assertIn("launchLabels.Remove(removed);", ui)
        self.assertIn("button.interactable = offer.CanRequestLaunch;", ui)
        self.assertIn("host.LaunchBusiness(id);", ui)
        self.assertIn("DashboardLocalizationCatalog.TryFindValue(option.NameKey", ui)
        self.assertIn("DashboardLocalizationCatalog.TryFindValue(business.NameKey", ui)
        self.assertIn("option.MinimumInvestmentVnd.ToString", ui)
        self.assertIn("option.RequiredOwnerMinutes.HasValue", ui)
        self.assertIn("option.OperationMode == BusinessOperationMode.SideHustleCompatible", ui)
        self.assertNotIn("AddLaunch(content.transform,", ui)
        self.assertNotIn("launchCaptions", ui)
        self.assertNotIn("return business.Type switch", ui)
        self.assertIn("content.Businesses.Values.OrderBy(x => x.Id, StringComparer.Ordinal)", projection)
        self.assertIn("OperationMode = definition.OperationMode;", projection)
        self.assertIn("MinimumInvestmentVnd = definition.MinimumStartupInvestment;", projection)

    def test_existing_command_path_and_legacy_unity_events_remain(self):
        controller = read("Assets/StartupLife/Scripts/Presentation/LifeScreenViewController.cs")
        simulation = read("Assets/StartupLife/Scripts/Simulation/SimulationEngine.cs")
        self.assertIn("public void LaunchBusiness(string definitionId) => LaunchAuthoredBusiness(definitionId);",
                      controller)
        self.assertIn("content.Businesses.TryGetValue(definitionId, out var definition)", controller)
        self.assertIn("flow.LaunchBusiness(definition.Id, definition.Revision,", controller)
        self.assertIn("checked((int)definition.MinimumStartupInvestment)", controller)
        for name in ("LaunchOnlineStore", "LaunchHomeFoodPreorder",
                     "LaunchFreelanceService", "LaunchCoffeeKiosk"):
            self.assertIn("public void " + name + "()", controller)
        self.assertIn("case CommandKind.LaunchBusiness:", simulation)
        self.assertNotIn("state.Cash =", read(
            "Assets/StartupLife/Scripts/Presentation/BusinessManagementOverlay.cs"))

    def test_nested_scroll_layout_remains_size_driven_and_reusable(self):
        ui = read("Assets/StartupLife/Scripts/Presentation/BusinessManagementOverlay.cs")
        self.assertIn('Node("BusinessLaunchOptionList", content.transform,', ui)
        self.assertIn("typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)", ui)
        self.assertIn("ContentSizeFitter.FitMode.PreferredSize", ui)
        self.assertIn("private const float ButtonHeight = 96f;", ui)
        self.assertIn("MobileScrollViewFactory.Create(", ui)


if __name__ == "__main__":
    unittest.main()
