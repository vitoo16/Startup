"""Engine-free guard for the first player-facing Life journal composition.

Source contracts only: NOT a Unity compile, rendered screenshot, or art approval.
"""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
UI = ROOT / "Assets/StartupLife/Scripts/Presentation"
SCENE = UI / "LifeHomeComposition.cs"
LIFE = UI / "LifeScreenViewController.cs"
CATALOG = UI / "DashboardLocalizationCatalog.cs"
REGISTRY = ROOT / "Assets/StartupLife/Art/Approved/registry.json"


class LifeJournalSourceTests(unittest.TestCase):
    def test_reuses_existing_buttons_labels_and_one_scroll_factory(self):
        source = SCENE.read_text(encoding="utf-8")
        life = LIFE.read_text(encoding="utf-8")
        for name in ("LifeTitle", "NameRow", "DateRow", "TimeRow", "CashRow",
                     "CareerRow", "RankRow", "CourseRow", "CourseProgressRow",
                     "LifeStatus", "Actions"):
            self.assertIn('"' + name + '"', source)
        self.assertIn('MobileScrollViewFactory.Create(', source)
        self.assertIn('node.SetParent(', source)
        self.assertIn('actions.SetParent(', source)
        self.assertNotIn('new GameCommand(', source)
        self.assertNotIn('.onClick.AddListener', source)
        self.assertNotIn('File.Delete(', source)
        self.assertIn('LifeHomeComposition.Install(this,', life)
        self.assertIn('lifeHome?.Refresh(snapshot)', life)
        self.assertIn('if (transform.Find("LifeTitle") && transform.Find("Actions"))', life)

    def test_stage_remains_original_geometry_without_character_asset_promotion(self):
        source = SCENE.read_text(encoding="utf-8")
        registry = REGISTRY.read_text(encoding="utf-8")
        self.assertIn('LifeNeighborhoodStage', source)
        self.assertIn('StageWallStrip', source)
        self.assertIn('LaptopScreen', source)
        self.assertIn('RectNode(', source)
        self.assertNotIn('Resources.Load', source)
        self.assertNotIn('Art/Source', source)
        self.assertNotIn('Art/Approved', source)
        self.assertNotIn('AssetDatabase', source)
        self.assertIn('"approved_entries": []', registry)

    def test_life_moment_texts_are_localized_whole_sentences(self):
        catalog = CATALOG.read_text(encoding="utf-8")
        source = SCENE.read_text(encoding="utf-8")
        keys = ["life.home.stage", "life.home.progress", "life.home.actions",
                "life.home.newday", "life.home.work", "life.home.study",
                "life.home.evening"]
        for key in keys:
            self.assertIn('"' + key + '"', catalog)
            self.assertIn('"' + key + '"', source)
        self.assertIn('momentLabel.SetKey(key)', source)
        self.assertIn('if (key == activeMomentKey) return;', source)

    def test_idempotent_and_preserves_scroll_on_refresh(self):
        source = SCENE.read_text(encoding="utf-8")
        self.assertIn('if (!component.built)', source)
        self.assertIn('if (!built || snapshot == null', source)
        self.assertIn('ScrollToTopOnFreshEntry()', source)
        self.assertNotIn('scroll.verticalNormalizedPosition = 1f;\n            momentLabel.SetKey', source)


if __name__ == '__main__':
    unittest.main()
