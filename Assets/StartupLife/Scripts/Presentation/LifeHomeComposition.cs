using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StartupLife.Presentation
{
    /// <summary>
    /// Re-composes the existing source-wired Life screen without rebuilding its
    /// buttons, detaching event listeners or authoring shadow gameplay state.
    /// A quiet geometric neighborhood/workspace vignette is UI-only staging;
    /// it is NOT an approved character, animation or production art asset.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LifeHomeComposition : MonoBehaviour
    {
        private static readonly string[] HeaderRows =
            { "LifeTitle", "NameRow", "DateRow", "TimeRow" };
        private static readonly string[] ProgressRows =
            { "CashRow", "CareerRow", "RankRow", "CourseRow", "CourseProgressRow", "LifeStatus" };

        private TMP_Text momentText;
        private LocalizedKeyLabel momentLabel;
        private string activeMomentKey;
        private Button[] actionButtons;
        private Button acceptAction;
        private Button workAction;
        private Button studyAction;
        private Button advanceAction;
        private MobileTheme theme;
        private ScrollRect scroll;
        private bool built;

        public static LifeHomeComposition Install(LifeScreenViewController owner, TMP_Text fontSource)
        {
            if (!owner || !fontSource || !fontSource.font)
                throw new ArgumentException("Life home needs its existing controller and Vietnamese TMP font.");
            var component = owner.GetComponent<LifeHomeComposition>();
            if (!component) component = owner.gameObject.AddComponent<LifeHomeComposition>();
            if (!component.built) component.Build(owner.UiTheme, fontSource.font);
            return component;
        }

        private void Build(MobileTheme palette, TMP_FontAsset font)
        {
            theme = palette;
            // Preflight all original scene nodes BEFORE changing any parents.
            var header = new Transform[HeaderRows.Length];
            var progress = new Transform[ProgressRows.Length];
            for (var i = 0; i < header.Length; i++)
            {
                header[i] = transform.Find(HeaderRows[i]);
                if (!header[i]) throw new InvalidOperationException("Missing Life header: " + HeaderRows[i]);
            }
            for (var i = 0; i < progress.Length; i++)
            {
                progress[i] = transform.Find(ProgressRows[i]);
                if (!progress[i]) throw new InvalidOperationException("Missing Life progress row: " + ProgressRows[i]);
            }
            var actions = transform.Find("Actions");
            if (!actions) throw new InvalidOperationException("Life actions are missing.");

            // Reserve space for the persistent Dashboard and Business buttons
            // already anchored above this scroll view by their owning overlays.
            var view = MobileScrollViewFactory.Create(transform, "LifeHomeScroll", theme.background,
                new RectOffset(24, 24, 148, 32), 20f, TextAnchor.UpperLeft, 32f);
            scroll = view.Scroll;
            var content = view.Content.transform;

            var intro = Card("LifeJournalHeader", content, theme.panel, 16);
            foreach (var node in header) node.SetParent(intro.transform, false);
            // The original labels remain active, localized and in their
            // source order so existing runtime tests can still discover them.

            var stage = RectNode("LifeNeighborhoodStage", content, typeof(Image), typeof(LayoutElement));
            stage.GetComponent<Image>().color = theme.panel;
            stage.GetComponent<LayoutElement>().preferredHeight = 332f;
            stage.GetComponent<LayoutElement>().minHeight = 264f;
            BuildGeometricStage(stage.transform, font);

            var progressCard = Card("LifeProgressCard", content, theme.panel, 12);
            Label("LifeProgressHeading", "dashboard.life.home.progress", progressCard.transform, font, 36f, theme.accent);
            foreach (var node in progress) node.SetParent(progressCard.transform, false);

            var choices = Card("LifeActionCard", content, theme.panel, 12);
            Label("LifeActionHeading", "dashboard.life.home.actions", choices.transform, font, 36f, theme.accent);
            actions.SetParent(choices.transform, false);
            actionButtons = actions.GetComponentsInChildren<Button>(true);
            acceptAction = actions.Find("AcceptDeveloperButton")?.GetComponent<Button>();
            workAction = actions.Find("WorkButton")?.GetComponent<Button>();
            studyAction = actions.Find("Study60Button")?.GetComponent<Button>();
            advanceAction = actions.Find("AdvanceDayButton")?.GetComponent<Button>();
            foreach (var button in actionButtons)
            {
                // A visible pressed state, with all existing event listeners
                // and stable UI identities left untouched.
                var colors = button.colors;
                colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
                colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
                colors.fadeDuration = 0.08f;
                button.colors = colors;
            }

            built = true;
        }

        public void Refresh(FirstPlayableState snapshot)
        {
            if (!built || snapshot == null || !momentText) return;
            // Only one relevant action receives the accent; all other actions
            // remain available according to their authoritative interactable state.
            var primary = !string.IsNullOrEmpty(snapshot.CareerId) && snapshot.Instant.Minute < 17 * 60
                ? workAction : snapshot.ActiveCourse != null ? studyAction :
                  string.IsNullOrEmpty(snapshot.CareerId) ? acceptAction : advanceAction;
            if (primary == null || !primary.interactable) primary = advanceAction;
            foreach (var button in actionButtons)
            {
                var background = button.targetGraphic as Image;
                if (background) background.color = button == primary ? theme.accent : theme.ink;
            }
            var minute = snapshot.Instant.Minute;
            var key = minute >= 17 * 60 ? "dashboard.life.home.evening" :
                !string.IsNullOrEmpty(snapshot.CareerId) ? "dashboard.life.home.work" :
                snapshot.ActiveCourse != null ? "dashboard.life.home.study" : "dashboard.life.home.newday";
            if (key == activeMomentKey) return;
            activeMomentKey = key;
            momentLabel.SetKey(key);
        }

        // Called by the game's normal dashboard close/open lifecycle; never
        // scrolls the user to the top on every data refresh.
        public void ScrollToTopOnFreshEntry()
        {
            if (scroll) scroll.verticalNormalizedPosition = 1f;
        }

        private GameObject Card(string name, Transform parent, Color color, int spacing)
        {
            var node = RectNode(name, parent,
                typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            node.GetComponent<Image>().color = color;
            var layout = node.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var element = node.GetComponent<LayoutElement>();
            element.minHeight = 80;
            return node;
        }

        private void BuildGeometricStage(Transform parent, TMP_FontAsset font)
        {
            // 2D UI geometry marks the future actor stage without smuggling a
            // rejected C03 sprite into production. No fake body animation.
            // Rectangles use exact source-grid integers, flat theme colors and
            // nothing fetched from a third-party image library.
            var ink = theme.ink;
            var paper = theme.background;
            var warm = theme.accent;

            Block("StageWallStrip", parent, ink, new Vector2(0, 0.68f), new Vector2(1, 0.70f));
            Block("StageFloorStrip", parent, ink, new Vector2(0, 0.10f), new Vector2(1, 0.12f));
            Block("WindowFrame", parent, ink, new Vector2(0.68f, 0.28f), new Vector2(0.93f, 0.82f));
            Block("WindowGlass", parent, paper, new Vector2(0.69f, 0.30f), new Vector2(0.92f, 0.80f));
            Block("WindowCross", parent, ink, new Vector2(0.80f, 0.30f), new Vector2(0.82f, 0.80f));
            Block("WindowSill", parent, warm, new Vector2(0.65f, 0.28f), new Vector2(0.95f, 0.30f));
            Block("DeskTop", parent, ink, new Vector2(0.08f, 0.18f), new Vector2(0.66f, 0.23f));
            Block("DeskLegLeft", parent, ink, new Vector2(0.13f, 0.11f), new Vector2(0.16f, 0.18f));
            Block("DeskLegRight", parent, ink, new Vector2(0.59f, 0.11f), new Vector2(0.62f, 0.18f));
            Block("LaptopBody", parent, ink, new Vector2(0.30f, 0.23f), new Vector2(0.54f, 0.54f));
            Block("LaptopScreen", parent, paper, new Vector2(0.32f, 0.27f), new Vector2(0.52f, 0.51f));
            Block("Notebook", parent, warm, new Vector2(0.16f, 0.23f), new Vector2(0.26f, 0.26f));
            Block("Cup", parent, warm, new Vector2(0.56f, 0.23f), new Vector2(0.62f, 0.34f));
            Block("Hook", parent, warm, new Vector2(0.09f, 0.51f), new Vector2(0.13f, 0.53f));
            // These proportions intentionally do not constitute final world art.
            Label("StageHeading", "dashboard.life.home.stage", parent, font, 34f, ink,
                new Vector2(0.08f, 0.75f), new Vector2(0.62f, 0.98f));
            momentText = Label("StageMoment", "dashboard.life.home.newday", parent, font, 27f, ink,
                new Vector2(0.08f, 0.39f), new Vector2(0.63f, 0.72f));
            momentLabel = momentText.GetComponent<LocalizedKeyLabel>();
            activeMomentKey = "dashboard.life.home.newday";
        }

        private static void Block(string name, Transform parent, Color color, Vector2 low, Vector2 high)
        {
            var node = RectNode(name, parent, typeof(Image));
            var rect = (RectTransform)node.transform;
            rect.anchorMin = low;
            rect.anchorMax = high;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            node.GetComponent<Image>().color = color;
            node.GetComponent<Image>().raycastTarget = false;
        }

        private static TMP_Text Label(string name, string key, Transform parent,
            TMP_FontAsset font, float size, Color ink, Vector2? low = null, Vector2? high = null)
        {
            var node = RectNode(name, parent, typeof(TextMeshProUGUI));
            var text = node.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.color = ink;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            if (low.HasValue && high.HasValue)
            {
                var rect = (RectTransform)node.transform;
                rect.anchorMin = low.Value;
                rect.anchorMax = high.Value;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                node.AddComponent<LayoutElement>().ignoreLayout = true;
            }
            else node.AddComponent<LayoutElement>().preferredHeight = 62;
            LocalizedKeyLabel.EnsureFor(text).SetKey(key);
            return text;
        }

        private static GameObject RectNode(string name, Transform parent, params Type[] components)
        {
            var types = new Type[components.Length + 1];
            types[0] = typeof(RectTransform);
            Array.Copy(components, 0, types, 1, components.Length);
            var node = new GameObject(name, types);
            node.transform.SetParent(parent, false);
            return node;
        }
    }
}
