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
