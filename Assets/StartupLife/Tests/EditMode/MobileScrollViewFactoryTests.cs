using NUnit.Framework;
using StartupLife.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace StartupLife.Tests.EditMode
{
    public sealed class MobileScrollViewFactoryTests
    {
        [TestCase("LifeDashboardScroll", 20, 22, 18f, TextAnchor.UpperLeft, 32f)]
        [TestCase("BusinessManagementScroll", 12, 14, 11f, TextAnchor.UpperCenter, 35f)]
        [TestCase("EventChoiceScroll", 12, 14, 14f, TextAnchor.UpperCenter, 35f)]
        public void AllPortraitOverlayStylesReuseTheSameClippedVerticalHierarchy(
            string name, int horizontalPadding, int verticalPadding, float spacing,
            TextAnchor alignment, float sensitivity)
        {
            var parent = new GameObject("PortraitSafeArea", typeof(RectTransform));
            try
            {
                var color = new Color(0.12f, 0.18f, 0.25f, 0.95f);
                var view = MobileScrollViewFactory.Create(parent.transform, name,
                    color, new RectOffset(horizontalPadding, horizontalPadding,
                        verticalPadding, verticalPadding), spacing, alignment, sensitivity);
                var scroll = view.Scroll;
                var root = scroll.gameObject;
                var viewport = scroll.viewport;
                var content = (RectTransform)view.Content.transform;
                var stack = view.Content.GetComponent<VerticalLayoutGroup>();

                Assert.That(root.name, Is.EqualTo(name));
                Assert.That(root.transform.parent, Is.SameAs(parent.transform));
                Assert.That(root.transform.childCount, Is.EqualTo(1));
                Assert.That(viewport.name, Is.EqualTo("Viewport"));
                Assert.That(viewport.parent, Is.SameAs(root.transform));
                Assert.That(viewport.childCount, Is.EqualTo(1));
                Assert.That(content.name, Is.EqualTo("Content"));
                Assert.That(content.parent, Is.SameAs(viewport));
                Assert.That(scroll.content, Is.SameAs(content));
                Assert.That(scroll.horizontal, Is.False);
                Assert.That(scroll.vertical, Is.True);
                Assert.That(scroll.movementType, Is.EqualTo(ScrollRect.MovementType.Clamped));
                Assert.That(scroll.scrollSensitivity, Is.EqualTo(sensitivity));
                Assert.That(root.GetComponent<Image>().color, Is.EqualTo(color));
                Assert.That(root.GetComponent<LayoutElement>().flexibleHeight, Is.EqualTo(1f));
                Assert.That(root.GetComponent<LayoutElement>().minHeight,
                    Is.EqualTo(MobileScrollViewFactory.MinimumPortraitViewportHeight));
                Assert.That(viewport.GetComponent<Image>(), Is.Not.Null);
                Assert.That(viewport.GetComponent<Mask>().showMaskGraphic, Is.False);
                Assert.That(viewport.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(viewport.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(viewport.offsetMin, Is.EqualTo(Vector2.zero));
                Assert.That(viewport.offsetMax, Is.EqualTo(Vector2.zero));
                Assert.That(content.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
                Assert.That(content.anchorMax, Is.EqualTo(new Vector2(1f, 1f)));
                Assert.That(content.pivot, Is.EqualTo(new Vector2(0.5f, 1f)));
                Assert.That(content.anchoredPosition, Is.EqualTo(Vector2.zero));
                Assert.That(content.sizeDelta, Is.EqualTo(Vector2.zero));
                Assert.That(stack.padding.left, Is.EqualTo(horizontalPadding));
                Assert.That(stack.padding.right, Is.EqualTo(horizontalPadding));
                Assert.That(stack.padding.top, Is.EqualTo(verticalPadding));
                Assert.That(stack.padding.bottom, Is.EqualTo(verticalPadding));
                Assert.That(stack.spacing, Is.EqualTo(spacing));
                Assert.That(stack.childAlignment, Is.EqualTo(alignment));
                Assert.That(stack.childControlWidth && stack.childControlHeight, Is.True);
                Assert.That(stack.childForceExpandWidth && !stack.childForceExpandHeight, Is.True);
                Assert.That(view.Content.GetComponent<ContentSizeFitter>().verticalFit,
                    Is.EqualTo(ContentSizeFitter.FitMode.PreferredSize));
            }
            finally { Object.DestroyImmediate(parent); }
        }

        [Test]
        public void MissingParentIsRejectedBeforeAnyScrollHierarchyIsConstructed()
        {
            Assert.Throws<System.ArgumentException>(() => MobileScrollViewFactory.Create(
                null, "MissingParent", Color.white, new RectOffset(0, 0, 0, 0),
                0f, TextAnchor.UpperCenter, 35f));
        }
    }
}
