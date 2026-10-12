#nullable enable
using System;
using UnityEngine;

namespace StartupLife.Presentation
{
    // One uGUI ScrollRect hierarchy for the portrait overlays. Only the
    // style data differs between Career, Business and Event screens. The
    // factory owns no commands, journal state or safe-area calculations.
    public static class MobileScrollViewFactory
    {
        public const float MinimumPortraitViewportHeight = 160f;

        public readonly struct View
        {
            public UnityEngine.UI.ScrollRect Scroll { get; }
            public GameObject Content { get; }

            internal View(UnityEngine.UI.ScrollRect scroll, GameObject content)
            {
                Scroll = scroll;
                Content = content;
            }
        }

        public static View Create(Transform parent, string name, Color background,
            RectOffset padding, float spacing, TextAnchor alignment,
            float scrollSensitivity)
        {
            if (parent == null || string.IsNullOrWhiteSpace(name) ||
                padding == null || spacing < 0f || scrollSensitivity <= 0f)
                throw new ArgumentException("A valid parent, style and scroll identity are required.");

            var root = Node(name, parent, typeof(UnityEngine.UI.Image),
                typeof(UnityEngine.UI.ScrollRect), typeof(UnityEngine.UI.LayoutElement));
            root.GetComponent<UnityEngine.UI.Image>().color = background;
            var rootLayout = root.GetComponent<UnityEngine.UI.LayoutElement>();
            rootLayout.flexibleHeight = 1f;
            rootLayout.minHeight = MinimumPortraitViewportHeight;

            var viewport = Node("Viewport", root.transform,
                typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Mask));
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = viewportRect.offsetMax = Vector2.zero;
            viewport.GetComponent<UnityEngine.UI.Image>().color =
                new Color(1f, 1f, 1f, 0.01f);
            viewport.GetComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;

            var content = Node("Content", viewport.transform,
                typeof(UnityEngine.UI.VerticalLayoutGroup),
                typeof(UnityEngine.UI.ContentSizeFitter));
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            var stack = content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            stack.padding = padding;
            stack.spacing = spacing;
            stack.childAlignment = alignment;
            stack.childControlWidth = stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            content.GetComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit =
                UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

            var scroll = root.GetComponent<UnityEngine.UI.ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = scrollSensitivity;
            return new View(scroll, content);
        }

        private static GameObject Node(string name, Transform parent, params Type[] types)
        {
            var components = new Type[types.Length + 1];
            components[0] = typeof(RectTransform);
            Array.Copy(types, 0, components, 1, types.Length);
            var node = new GameObject(name, components);
            node.transform.SetParent(parent, false);
            return node;
        }
    }
}
