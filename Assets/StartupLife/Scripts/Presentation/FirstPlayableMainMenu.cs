using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StartupLife.Presentation
{
    /// <summary>
    /// Player-controlled home menu. Never owns the simulation, checkpoint or
    /// character state. The bootstrapper alone authorizes a new-run archive.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FirstPlayableMainMenu : MonoBehaviour
    {
        private StartupLifeBootstrapper bootstrap;
        private MobileTheme theme;
        private TMP_FontAsset font;
        private GameObject screen;
        private GameObject mainPage;
        private GameObject settingsPage;
        private GameObject confirmationPage;
        private Button continueButton;
        private LocalizedKeyLabel motionStatus;
        private LocalizedKeyLabel feedback;

        public static FirstPlayableMainMenu Install(StartupLifeBootstrapper owner,
            Transform safeArea, MobileTheme palette, TMP_FontAsset typeface)
        {
            if (!owner || !safeArea || !palette || !typeface)
                throw new ArgumentException("Menu requires bootstrap, SafeArea, UI theme and font.");
            var menu = owner.GetComponent<FirstPlayableMainMenu>();
            if (!menu) menu = owner.gameObject.AddComponent<FirstPlayableMainMenu>();
            if (menu.screen) return menu;
            menu.bootstrap = owner;
            menu.theme = palette;
            menu.font = typeface;
            menu.Build(safeArea);
            // Existing runs enter through the player-facing choice, not a
            // silent jump to the old dashboard. A fresh run starts directly
            // in Character Creator without an unnecessary extra click.
            var state = owner.Snapshot;
            if (state != null && !string.IsNullOrEmpty(state.Name)) menu.Open();
            return menu;
        }

        private void Build(Transform safeArea)
        {
            var open = Button("StartupMainMenuOpenButton", "dashboard.menu.open",
                safeArea, Open);
            var rect = (RectTransform)open.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(205f, theme.touchTarget);
            rect.anchoredPosition = new Vector2(0f, -18f);
            // Buttons inside screens are part of a vertical layout, but the
            // always-available entry button must remain anchored to SafeArea.
            screen = Node("StartupMainMenuOverlay", safeArea, typeof(Image));
            Stretch((RectTransform)screen.transform);
            screen.GetComponent<Image>().color = theme.background;
            screen.GetComponent<Image>().raycastTarget = true;
            mainPage = Page("StartupMainMenuHome", screen.transform);
            var homeBody = PageBody(mainPage);
            Text("StartupMainMenuTitle", "dashboard.menu.title", homeBody, theme.title);
            continueButton = Button("StartupMainMenuContinueButton", "dashboard.menu.continue",
                homeBody, Close).GetComponent<Button>();
            Button("StartupMainMenuNewGameButton", "dashboard.menu.new",
                homeBody, RequestNewGame);
            Button("StartupMainMenuSettingsButton", "dashboard.menu.settings",
                homeBody, OpenSettings);
            Button("StartupMainMenuBackButton", "dashboard.menu.back",
                homeBody, Close);

            settingsPage = Page("StartupMainMenuSettings", screen.transform);
            var settingsBody = PageBody(settingsPage);
            Text("StartupMainMenuSettingsTitle", "dashboard.menu.settings",
                settingsBody, theme.heading);
            motionStatus = Text("StartupMainMenuMotionState", "dashboard.menu.motion.enabled",
                settingsBody, theme.body).GetComponent<LocalizedKeyLabel>();
            Button("StartupMainMenuMotionToggleButton", "dashboard.menu.motion.toggle",
                settingsBody, ToggleMotion);
            Button("StartupMainMenuSettingsBackButton", "dashboard.menu.back",
                settingsBody, ShowHome);

            confirmationPage = Page("StartupMainMenuNewGameConfirmation", screen.transform);
            var confirmationBody = PageBody(confirmationPage);
            var warning = Text("StartupMainMenuWarning", "dashboard.menu.confirm",
                confirmationBody, theme.body);
            warning.GetComponent<LayoutElement>().preferredHeight = theme.touchTarget * 3.5f;
            Button("StartupMainMenuConfirmButton", "dashboard.menu.archive_new",
                confirmationBody, ConfirmNewGame);
            Button("StartupMainMenuCancelButton", "dashboard.menu.cancel",
                confirmationBody, ShowHome);
            feedback = Text("StartupMainMenuFeedback", "dashboard.menu.archive_failed",
                confirmationBody, theme.caption).GetComponent<LocalizedKeyLabel>();
            feedback.gameObject.SetActive(false);
            screen.SetActive(false);
        }

        private void Open()
        {
            if (!bootstrap || !bootstrap.IsReady) return;
            var state = bootstrap.Snapshot;
            continueButton.interactable = state != null && !string.IsNullOrEmpty(state.Name);
            screen.SetActive(true);
            screen.transform.SetAsLastSibling();
            ShowHome();
        }

        private void Close()
        {
            if (screen) screen.SetActive(false);
        }

        private void ShowHome()
        {
            mainPage.SetActive(true);
            settingsPage.SetActive(false);
            confirmationPage.SetActive(false);
            feedback.gameObject.SetActive(false);
            ResetScroll(mainPage);
        }

        private void OpenSettings()
        {
            mainPage.SetActive(false);
            confirmationPage.SetActive(false);
            settingsPage.SetActive(true);
            ResetScroll(settingsPage);
            RefreshMotionStatus();
        }

        private void RefreshMotionStatus()
        {
            motionStatus.SetKey(PlayerPrefs.GetInt(UiPanelReveal.ReducedMotionPreference, 0) != 0
                ? "dashboard.menu.motion.disabled" : "dashboard.menu.motion.enabled");
        }

        private void ToggleMotion()
        {
            var key = UiPanelReveal.ReducedMotionPreference;
            PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key, 0) == 0 ? 1 : 0);
            PlayerPrefs.Save();
            RefreshMotionStatus();
        }

        private void RequestNewGame()
        {
            var state = bootstrap.Snapshot;
            if (state == null) return;
            if (string.IsNullOrEmpty(state.Name))
            {
                // Fresh, never-committed character creation. No checkpoint is
                // moved and nothing is erased merely to revisit the creator.
                Close();
                return;
            }
            mainPage.SetActive(false);
            settingsPage.SetActive(false);
            confirmationPage.SetActive(true);
            feedback.gameObject.SetActive(false);
            ResetScroll(confirmationPage);
        }

        private void ConfirmNewGame()
        {
            if (bootstrap.TryArchiveAndStartNewGame(out var reason)) return;
            Debug.LogWarning("[StartupLife][Menu] New-game request refused: " + reason);
            feedback.gameObject.SetActive(true);
            feedback.SetKey("dashboard.menu.archive_failed");
        }

        private GameObject Page(string name, Transform parent)
        {
            var panel = Node(name, parent, typeof(Image));
            var bounds = (RectTransform)panel.transform;
            bounds.anchorMin = new Vector2(0.07f, 0.16f);
            bounds.anchorMax = new Vector2(0.93f, 0.85f);
            bounds.offsetMin = bounds.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = theme.panel;
            // Each page owns an independent vertical scroll position. Its
            // buttons retain their original GameObjects and listeners.
            var view = MobileScrollViewFactory.Create(panel.transform, name + "Scroll",
                theme.panel, new RectOffset(36, 36, 36, 36), 20f,
                TextAnchor.UpperCenter, 34f);
            Stretch((RectTransform)view.Scroll.transform);
            return panel;
        }

        private static Transform PageBody(GameObject page)
        {
            var body = page.transform.Find(page.name + "Scroll/Viewport/Content");
            if (!body) throw new InvalidOperationException("Main menu scroll hierarchy is missing.");
            return body;
        }

        private static void ResetScroll(GameObject page)
        {
            var scroll = page.GetComponentInChildren<ScrollRect>(true);
            if (scroll) scroll.verticalNormalizedPosition = 1f;
        }

        private GameObject Button(string name, string key, Transform parent, Action callback)
        {
            var node = Node(name, parent, typeof(Image), typeof(Button));
            var background = node.GetComponent<Image>();
            background.color = theme.accent;
            var button = node.GetComponent<Button>();
            button.targetGraphic = background;
            var colors = button.colors;
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.highlightedColor = new Color(0.93f, 0.93f, 0.93f, 1f);
            colors.fadeDuration = 0.09f;
            button.colors = colors;
            button.onClick.AddListener(() => callback());
            var rowLayout = node.GetComponent<RectTransform>();
            if (parent.GetComponent<VerticalLayoutGroup>())
                node.AddComponent<LayoutElement>().preferredHeight = theme.touchTarget;
            else
                rowLayout.sizeDelta = new Vector2(205f, theme.touchTarget);
            var label = Text(name + "Label", key, node.transform, theme.heading);
            var labelRect = (RectTransform)label.transform;
            Stretch(labelRect);
            labelRect.offsetMin = new Vector2(8f, 3f);
            labelRect.offsetMax = new Vector2(-8f, -3f);
            label.GetComponent<LayoutElement>().ignoreLayout = true;
            label.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
            label.GetComponent<TMP_Text>().color = Color.white;
            return node;
        }

        private GameObject Text(string name, string key, Transform parent, float size)
        {
            var node = Node(name, parent, typeof(TextMeshProUGUI), typeof(LayoutElement));
            var text = node.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.color = theme.ink;
            node.GetComponent<LayoutElement>().preferredHeight = Mathf.Max(theme.touchTarget, size * 2f);
            LocalizedKeyLabel.EnsureFor(text).SetKey(key);
            return node;
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

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
