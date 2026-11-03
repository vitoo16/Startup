"""P04 Business Batch A is authored but NOT silently activated in v1 saves.

The actual C# constructor, schedule and demand behavior is exercised by the
ContentCatalogChecks and EconomicChecks suites. These source checks protect
the version boundary and localization/enum wiring without claiming Unity QA.
"""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]


def source(path):
    return (ROOT / path).read_text(encoding="utf-8")


class BusinessBatchASourceTests(unittest.TestCase):
    def test_candidate_catalog_is_explicit_versioned_opt_in(self):
        src = source("Assets/StartupLife/Scripts/Content/FirstPlayableContentTemplate.cs")
        self.assertIn('Version = "first-playable.v1"', src)
        self.assertIn('source.Version = "first-playable.biz-a.v1";', src)
        self.assertIn("public static ContentCatalogSource CreateBusinessBatchA()", src)
        self.assertIn("public static ContentCatalog BuildBusinessBatchACatalog()", src)
        for id_ in ("marketing-agency", "small-cafe", "fashion-store"):
            self.assertIn('= "' + id_ + '";', src)
        self.assertIn("source.Businesses = source.Businesses.Concat(new[]", src)
        self.assertIn("BusinessOperationMode.SideHustleCompatible, 1200000", src)
        self.assertIn("BusinessOperationMode.FullTimeRequired, 6000000", src)
        self.assertIn("BusinessOperationMode.FullTimeRequired, 4000000", src)

    def test_economics_preserve_m9_v1_and_require_seven_complete_profiles(self):
        econ = source("Assets/StartupLife/Scripts/Content/M9T02FunctionalEconomy.cs")
        defs = source("Assets/StartupLife/Scripts/Core/EconomicDefinitions.cs")
        enums = source("Assets/StartupLife/Scripts/Core/Business.cs")
        self.assertIn('new BusinessEconomicCatalog("m9-t02.functional-fixture-v1", "v1"', econ)
        self.assertIn('new BusinessEconomicCatalog("p04.biz-a.preview-v1"', econ)
        self.assertIn('"biz-a.v1", "p04-biz-a.rules-v1"', econ)
        self.assertIn("baseline.Pools.Values.Concat(extraPools)", econ)
        self.assertIn("baseline.Profiles.Values.Concat(extraProfiles)", econ)
        self.assertIn('catalogRevision == "biz-a.v1" ? 7 : 4', defs)
        self.assertIn("Profiles.Count != requiredBusinesses", defs)
        self.assertIn("MarketingAgency = 5", enums)
        self.assertIn("SmallCafe = 6", enums)
        self.assertIn("FashionStore = 7", enums)
        self.assertNotIn('source.Version = "first-playable.biz-a.v1";',
                         source("Assets/StartupLife/Scripts/Content/ContentCatalogSource.cs"))

    def test_bootstrap_uses_verified_rule_selection_without_implicit_upgrade(self):
        bootstrap = source("Assets/StartupLife/Scripts/Presentation/StartupLifeBootstrapper.cs")
        selector = source("Assets/StartupLife/Scripts/Infrastructure/VerifiedBusinessContentSelector.cs")
        self.assertIn("enableBusinessBatchAForNewRuns = false;", bootstrap)
        self.assertIn("initialV2Content = contentAsset.BuildCatalog();", bootstrap)
        self.assertIn("GameSession.NewState(initialV2Content,", bootstrap)
        self.assertIn("VerifiedBusinessContentSelector.Select(primary, backup,", bootstrap)
        self.assertIn("baselineEventSerializer, batchEventSerializer,", bootstrap)
        self.assertIn("serializer, batchSerializer, enableBusinessBatchAForNewRuns", bootstrap)
        self.assertIn("new AtomicFileSaveStore(path, eventSerializer)", bootstrap)
        self.assertIn("new AtomicFileSaveStore(path, serializer)", bootstrap)
        self.assertIn("BusinessContentTrack.BusinessBatchA", bootstrap)
        self.assertIn("if (primary == null && backup == null)", selector)
        self.assertIn("var chosen = Verify(primary, baseline, batchA) ??", selector)
        self.assertIn("Verify(backup, baseline, batchA);", selector)
        self.assertIn("if (oldStatus == LoadStatus.Valid)", selector)
        self.assertIn("throw new InvalidOperationException(", selector)
        self.assertNotIn("File.WriteAllBytes", selector)
        self.assertNotIn("AtomicFileSaveStore", selector)

    def test_v3_v4_content_replay_regression_is_wired(self):
        economic = source("tools/EconomicChecks/Program.cs")
        for phrase in (
            "P04-BIZ-03 new business receipts survive V3 cold replay",
            "P04-BIZ-03 V4 source replay accepts seven-business fresh frontier",
            "P04-BIZ-03 old V3 ruleset is not silently reinterpreted",
            "VerifiedBusinessContentSelector.Select(sourceBytes, null,",
            "VerifiedBusinessContentSelector.Select(saved, null,",
            "File.ReadAllBytes(filename).SequenceEqual(saved)",
        ):
            self.assertIn(phrase, economic)
        self.assertIn(
            "BusinessBatchA = 2",
            source("Assets/StartupLife/Scripts/Infrastructure/VerifiedBusinessContentSelector.cs"))

    def test_new_names_are_unique_in_shared_vietnamese_manifest(self):
        src = source("Assets/StartupLife/Scripts/Presentation/DashboardLocalizationCatalog.cs")
        keys = re.findall(r'new KeyValuePair<string,string>\("([^"]+)"', src)
        self.assertEqual(len(keys), len(set(keys)))
        for key in ("business.marketing-agency", "business.small-cafe",
                    "business.fashion-store", "helper.cafe-assistant"):
            self.assertIn(key, keys)

if __name__ == "__main__":
    unittest.main()
