"""Course picker must bind authored course IDs, not the demo-only purchase button.

Source contract only; deterministic money/skill behavior is exercised by
SimulationChecks against the actual committed simulation and read model.
"""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]


def read(path):
    return (ROOT / path).read_text(encoding="utf-8")


class CourseDashboardWiringTests(unittest.TestCase):
    def test_multi_course_picker_is_catalog_driven(self):
        ui = read("Assets/StartupLife/Scripts/Presentation/LifeDashboardOverlay.cs")
        self.assertIn('model.Courses', ui)
        self.assertIn('CourseEnrollmentPolicy.ChooseCourseId(model, selectedCourseId)', ui)
        self.assertIn('CourseEnrollmentPolicy.Evaluate(model, selectedCourse)', ui)
        self.assertIn('foreach (var item in items)', ui)
        self.assertIn('SyncSelectionOptions(catalog, courseChoices, courseButtons, courseButtonLabels,', ui)
        self.assertIn('selectedCourseId = courseId;', ui)
        self.assertIn('host.PurchaseCourse(selectedCourseId);', ui)
        self.assertIn('CoursePurchaseAvailability.Available', ui)
        self.assertIn('CoursePurchaseAvailability.OutstandingArrears', ui)
        self.assertIn('CoursePurchaseAvailability.InsufficientCash', ui)
        self.assertNotIn('host.PurchaseCommunicationCourse', ui)
        self.assertNotIn('Mua khóa Giao tiếp', ui)

    def test_read_only_ui_can_reuse_rows_and_preserve_legacy_scenes(self):
        ui = read("Assets/StartupLife/Scripts/Presentation/LifeDashboardOverlay.cs")
        view = read("Assets/StartupLife/Scripts/Presentation/LifeScreenViewController.cs")
        actions = read("Assets/StartupLife/Scripts/Presentation/StudyActionController.cs")
        policy = read("Assets/StartupLife/Scripts/Application/CourseEnrollmentPolicy.cs")
        self.assertIn('buttons.Add(capturedId, button)', ui)
        self.assertIn('originalFontSizes.Remove(labels[oldId]);', ui)
        self.assertIn('button.GetComponent<Image>().color = isSelected ? Accent : Surface;', ui)
        self.assertIn('private const float TouchHeight = 96f;', ui)
        self.assertIn('MobileScrollViewFactory.Create(', ui)
        self.assertIn('public void PurchaseCourse(string courseId)', view)
        self.assertIn('content.Courses.ContainsKey(courseId)', view)
        self.assertIn('public CommandResult PurchaseCourse(string courseId)', actions)
        self.assertIn('flow.PurchaseCourse(courseId).Command', actions)
        self.assertIn('public void PurchaseCommunicationCourse()', view)
        self.assertIn('public CommandResult PurchaseCommunicationCourse()', actions)
        self.assertIn('PurchaseCourse("communication-basics")', actions)
        self.assertIn('dashboard.Finance.Arrears.Count != 0', policy)
        self.assertIn('dashboard.Finance.CashVnd < course.PriceVnd', policy)
        self.assertIn('dashboard.Game.ActiveCourse != null', policy)
        self.assertNotIn('state.Cash =', policy)
        self.assertNotIn('state.Course =', policy)
        self.assertNotIn('PlayerPrefs', policy)

    def test_new_application_source_tracks_unity_meta(self):
        meta = read("Assets/StartupLife/Scripts/Application/CourseEnrollmentPolicy.cs.meta")
        self.assertIn('fileFormatVersion: 2', meta)
        self.assertIn('guid: a4dfcc27b10c4ba8a7cf03650c7d38f5', meta)


if __name__ == "__main__":
    unittest.main()
