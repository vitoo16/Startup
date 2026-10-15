using NUnit.Framework;
using StartupLife.Presentation;

namespace StartupLife.Tests.EditMode
{
    public sealed class CharacterCreatorTouchPolicyTests
    {
        [TestCase(0.33333334f, 0f, 360, 96f, 144f)]
        [TestCase(1f, 440f, 1080, 96f, 132f)]
        [TestCase(0.5f, 160f, 360, 96f, 96f)]
        [TestCase(1f, 0f, 1080, 96f, 144f)]
        public void TouchHeightAccountsForCanvasScaleAndDpi(float scale,
            float dpi, int width, float baseline, float expected)
        {
            var calculated = CharacterCreatorTouchPolicy.MinimumCanvasUnits(
                scale, dpi, width, baseline);
            Assert.That(calculated, Is.EqualTo(expected).Within(0.02f));
        }

        [TestCase(0, 1f)]
        [TestCase(1, 1.5f)]
        [TestCase(2, 2f)]
        public void VietnameseTextScaleIsExplicitAndBounded(int step, float factor)
        {
            Assert.That(CharacterCreatorTextScalePolicy.Multiplier(step),
                Is.EqualTo(factor));
            Assert.That(CharacterCreatorTextScalePolicy.ControlHeight(96f, 36f, factor),
                Is.GreaterThanOrEqualTo(96f));
        }

        [TestCase(640, 24f, 590f, 0f, 0.33333334f, 560f, 0f)]
        [TestCase(640, 24f, 590f, 144f, 0.33333334f, 560f, 360f)]
        [TestCase(640, 300f, 300f, 280f, 0.33333334f, 560f, 0f)]
        [TestCase(640, 24f, 250f, 300f, 0.5f, 300f, 200f)]
        public void KeyboardInsetNeverCoversReservedSafeViewport(int height,
            float safeBottom, float safeHeight, float keyboardTop, float scale,
            float reserved, float expected)
        {
            Assert.That(CharacterCreatorKeyboardPolicy.BottomInsetCanvasUnits(
                height, safeBottom, safeHeight, keyboardTop, scale, reserved),
                Is.EqualTo(expected).Within(.02f));
        }

        [Test]
        public void InvalidScreenMetricsFailFastInsteadOfShrinkingTargets()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                CharacterCreatorTouchPolicy.MinimumCanvasUnits(0f, 160f, 360, 96f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                CharacterCreatorTouchPolicy.MinimumCanvasUnits(1f, 0f, 0, 96f));
        }
    }
}
