"""Engine-free contracts for incremental Unity uGUI polish (NOT visual QA)."""

from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
UI = ROOT / "Assets/StartupLife/Scripts/Presentation"
OVERLAY = UI / "LifeDashboardOverlay.cs"
CATALOG = UI / "DashboardLocalizationCatalog.cs"
REVEAL = UI / "UiPanelReveal.cs"


class UiMotionSourceTests(unittest.TestCase):
    def test_reveal_is_pixel_safe_unscaled_and_reduced_motion_aware(self):
        source = REVEAL.read_text(encoding="utf-8")
        self.assertIn("Time.unscaledDeltaTime", source)
        self.assertIn("CanvasGroup", source)
        self.assertIn("ReducedMotionPreference", source)
        self.assertIn("playedSinceEnable", source)
        self.assertIn("private void OnDisable()", source)
        self.assertIn("canvasGroup.alpha = 1f;", source)
        self.assertNotIn("transform.localScale", source)
        self.assertNotIn("anchoredPosition =", source)
        self.assertNotIn("Time.timeScale =", source)

    def test_screen_entry_and_dashboard_reuse_one_component(self):
        creator = (UI / "CharacterCreationViewController.cs").read_text(encoding="utf-8")
        life = (UI / "LifeScreenViewController.cs").read_text(encoding="utf-8")
        dashboard = OVERLAY.read_text(encoding="utf-8")
        self.assertIn("UiPanelReveal.Show(target)", creator)
        self.assertIn("UiPanelReveal.Show(target)", life)
        self.assertIn("UiPanelReveal.Show(panel)", dashboard)
        self.assertIn("UiPanelReveal.Show(pages[section])", dashboard)
        self.assertIn("colors.pressedColor", dashboard)
        self.assertIn("button.colors = colors", dashboard)

    def test_revised_vietnamese_headings_keep_authoritative_catalog_join(self):
        dashboard = OVERLAY.read_text(encoding="utf-8")
        catalog = CATALOG.read_text(encoding="utf-8")
        for key, sentence in (
            ("dashboard.career.current", "Công việc của bạn"),
            ("dashboard.finance.heading", "Thu chi của bạn"),
            ("dashboard.business.heading", "Góc kinh doanh"),
        ):
            self.assertIn(f'"{key}", "{sentence}"', catalog)
            self.assertIn(f'"{sentence}"', dashboard)
        self.assertNotIn("còn phần lẻ được giữ chính xác trong save", dashboard)
        self.assertNotIn("Nguồn vốn và thời gian vận hành được kiểm tra trước mỗi lệnh", dashboard)


if __name__ == "__main__":
    unittest.main()
