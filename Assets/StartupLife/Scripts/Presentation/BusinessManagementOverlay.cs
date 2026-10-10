#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using StartupLife.Core;
using UnityEngine;
using UnityEngine.UI;

namespace StartupLife.Presentation
{
    // Installs into the EXISTING LifePanel after GameSession is ready.
    // No scene YAML rewrite, no phantom income, and no command on refresh/open.
    // ScrollRect hierarchy: ScrollRoot -> Viewport(Image+Mask) -> Content(VLG+CSF).
    public sealed class BusinessManagementOverlay : MonoBehaviour
    {
        private LifeScreenViewController host = null!;
        private Func<BusinessPortfolioSnapshot> readPortfolio = null!;
        private TMP_FontAsset font = null!;
        private GameObject panel = null!;
        private TMP_Text selectedValue = null!;
        private TMP_Text closeCaption = null!;
        private Button previous = null!;
        private Button next = null!;
        private Button pause = null!;
        private Button resume = null!;
        private Button budget = null!;
        private Button standard = null!;
        private Button premium = null!;
        private Button reinvest = null!;
        private Button close = null!;
        private readonly Dictionary<string, Button> launchButtons =
            new Dictionary<string, Button>(StringComparer.Ordinal);
        private string selectedId = "";
        private bool closeArmed;

        public static BusinessManagementOverlay Install(LifeScreenViewController view,
            Func<BusinessPortfolioSnapshot> portfolio, TMP_Text existingFontSource)
        {
            if (view == null || portfolio == null || existingFontSource == null ||
                existingFontSource.font == null)
                throw new ArgumentException("Business UI requires the existing life screen, portfolio and font.");
            var component = view.GetComponent<BusinessManagementOverlay>();
            if (component == null) component = view.gameObject.AddComponent<BusinessManagementOverlay>();
            if (component.panel != null) return component;
            component.host = view;
            component.readPortfolio = portfolio;
            component.font = existingFontSource.font;
            component.Build();
            component.Refresh();
            return component;
        }

        public bool IsOpen => panel != null && panel.activeSelf;

        public void Open()
        {
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            closeArmed = false;
            Refresh();
        }

        public void Hide()
        {
            closeArmed = false;
            if (panel != null) panel.SetActive(false);
        }

        private void Build()
        {
            var toggle = ButtonObject("BusinessManagementButton", "Kinh doanh",
                transform, Open, 78);
            var toggleLayout = toggle.GetComponent<LayoutElement>();
            toggleLayout.ignoreLayout = true;
            var toggleRect = (RectTransform)toggle.transform;
            toggleRect.anchorMin = toggleRect.anchorMax = new Vector2(1, 1);
            toggleRect.pivot = new Vector2(1, 1);
            toggleRect.sizeDelta = new Vector2(290, 78);
            toggleRect.anchoredPosition = new Vector2(-22, -18);

            panel = Node("BusinessManagementOverlay", transform,
                typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            panel.GetComponent<LayoutElement>().ignoreLayout = true;
            var bounds = (RectTransform)panel.transform;
            bounds.anchorMin = new Vector2(0.03f, 0.03f);
            bounds.anchorMax = new Vector2(0.97f, 0.97f);
            bounds.offsetMin = bounds.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.18f, 0.98f);
            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 22, 22);
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Label("BusinessManagementTitle", "Quản lý doanh nghiệp", panel.transform, 39, 72);
            ButtonObject("BusinessManagementCloseButton", "Đóng bảng",
                panel.transform, Hide, 70);
            var finance = Label("LastSettledFinanceValue", "Chưa có quyết toán kinh doanh",
                panel.transform, 28, 106);
            var portfolio = Label("BusinessPortfolioValue", "Chưa có doanh nghiệp",
                panel.transform, 26, 128);
            host.AttachBusinessReadouts(finance, portfolio);

            var scrolling = Node("BusinessManagementScroll", panel.transform,
                typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            scrolling.GetComponent<Image>().color = new Color(0.12f, 0.18f, 0.25f, 0.95f);
            var scrollLayout = scrolling.GetComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1;
            scrollLayout.minHeight = 160;
            var viewport = Node("Viewport", scrolling.transform, typeof(Image), typeof(Mask));
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = viewportRect.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = Node("Content", viewport.transform,
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            var stack = content.GetComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(12, 12, 14, 14);
            stack.spacing = 11;
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.childControlWidth = stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            var scroll = scrolling.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35;

            Label("OpenBusinessHeading", "Mở doanh nghiệp", content.transform, 30, 62);
            AddLaunch(content.transform, "online-store", "Mở cửa hàng trực tuyến", host.LaunchOnlineStore);
            AddLaunch(content.transform, "home-food-preorder", "Mở bếp đặt trước", host.LaunchHomeFoodPreorder);
            AddLaunch(content.transform, "freelance-service", "Nhận việc tự do", host.LaunchFreelanceService);
            AddLaunch(content.transform, "coffee-kiosk", "Mở quầy cà phê", host.LaunchCoffeeKiosk);

            Label("ManageBusinessHeading", "Doanh nghiệp đang sở hữu",
                content.transform, 30, 62);
            selectedValue = Label("SelectedBusinessValue", "Chưa có doanh nghiệp",
                content.transform, 29, 118);
            previous = ButtonObject("PreviousBusinessButton", "Doanh nghiệp trước",
                content.transform, () => Select(-1), 70).GetComponent<Button>();
            next = ButtonObject("NextBusinessButton", "Doanh nghiệp tiếp",
                content.transform, () => Select(1), 70).GetComponent<Button>();

            budget = ButtonObject("BusinessBudgetPricingButton", "Giá tiết kiệm",
                content.transform, () => ForSelected(id =>
                    host.SetBusinessPricing(id, PricingPosture.Budget)), 70).GetComponent<Button>();
            standard = ButtonObject("BusinessStandardPricingButton", "Giá tiêu chuẩn",
                content.transform, () => ForSelected(id =>
                    host.SetBusinessPricing(id, PricingPosture.Standard)), 70).GetComponent<Button>();
            premium = ButtonObject("BusinessPremiumPricingButton", "Giá cao cấp",
                content.transform, () => ForSelected(id =>
                    host.SetBusinessPricing(id, PricingPosture.Premium)), 70).GetComponent<Button>();
            pause = ButtonObject("PauseBusinessButton", "Tạm dừng hoạt động",
                content.transform, () => ForSelected(host.PauseBusiness), 70).GetComponent<Button>();
            resume = ButtonObject("ResumeBusinessButton", "Tiếp tục hoạt động",
                content.transform, () => ForSelected(host.ResumeBusiness), 70).GetComponent<Button>();
            reinvest = ButtonObject("ReinvestBusinessButton", "Tái đầu tư mức tối thiểu",
                content.transform, () => ForSelected(host.ReinvestMinimumBusiness), 70).GetComponent<Button>();
            var closeObject = ButtonObject("CloseBusinessButton", "Đóng doanh nghiệp",
                content.transform, RequestClose, 70);
            close = closeObject.GetComponent<Button>();
            closeCaption = closeObject.GetComponentInChildren<TMP_Text>(true);
            panel.SetActive(false);
        }

        private void AddLaunch(Transform content, string id, string caption, Action callback)
        {
            var go = ButtonObject("Launch" + id.Replace("-", "") + "Button",
                caption, content, () =>
                {
                    callback();
                    Refresh();
                }, 74);
            launchButtons.Add(id, go.GetComponent<Button>());
        }

        private void RequestClose()
        {
            if (!closeArmed)
            {
                closeArmed = true;
                closeCaption.text = "Nhấn lần nữa để xác nhận đóng";
                return;
            }
            closeArmed = false;
            ForSelected(host.CloseBusiness);
        }

        private void ForSelected(Action<string> command)
        {
            var owned = readPortfolio().Businesses.FirstOrDefault(x => x.InstanceId == selectedId);
            if (owned == null || !owned.IsActive) return;
            closeArmed = false;
            command(owned.InstanceId);
            Refresh();
        }

        private void Select(int offset)
        {
            var list = OrderedBusinesses();
            if (list.Length == 0) return;
            var index = Array.FindIndex(list, x => x.InstanceId == selectedId);
            selectedId = list[((index < 0 ? 0 : index) + offset + list.Length) % list.Length].InstanceId;
            closeArmed = false;
            Refresh();
        }

        private BusinessSnapshot[] OrderedBusinesses() =>
            readPortfolio().Businesses.OrderBy(x => x.DefinitionId, StringComparer.Ordinal)
                .ThenBy(x => x.InstanceId, StringComparer.Ordinal).ToArray();

        public void Refresh()
        {
            if (panel == null || readPortfolio == null) return;
            var list = OrderedBusinesses();
            if (list.All(x => x.InstanceId != selectedId))
                selectedId = list.Length == 0 ? "" : list[0].InstanceId;
            foreach (var item in launchButtons)
                item.Value.interactable = !list.Any(x =>
                    x.IsActive && x.DefinitionId == item.Key);
            var index = Array.FindIndex(list, x => x.InstanceId == selectedId);
            var current = index < 0 ? null : list[index];
            var active = current != null && current.IsActive;
            selectedValue.text = current == null ? "Chưa có doanh nghiệp để quản lý" :
                current.DefinitionId + " (" + (index + 1).ToString(CultureInfo.InvariantCulture) +
                "/" + list.Length.ToString(CultureInfo.InvariantCulture) + ")\n" +
                (active ? "Đang sở hữu" : "Đã đóng") +
                " · Giá hiện tại: " + current.PricingPosture;
            previous.interactable = next.interactable = list.Length > 1;
            pause.interactable = resume.interactable = active;
            budget.interactable = standard.interactable = premium.interactable = active;
            reinvest.interactable = close.interactable = active;
            if (!closeArmed) closeCaption.text = "Đóng doanh nghiệp";
        }

        private GameObject ButtonObject(string name, string caption, Transform parent,
            Action clicked, float height)
        {
            var go = Node(name, parent, typeof(Image), typeof(Button), typeof(LayoutElement));
            go.GetComponent<Image>().color = new Color(0.16f, 0.45f, 0.50f, 1f);
            var layout = go.GetComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;
            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            button.onClick.AddListener(() => clicked());
            var label = Label("Label", caption, go.transform, 29, height);
            label.alignment = TextAlignmentOptions.Center;
            var rect = (RectTransform)label.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12, 3);
            rect.offsetMax = new Vector2(-12, -3);
            label.GetComponent<LayoutElement>().ignoreLayout = true;
            return go;
        }

        private TMP_Text Label(string name, string message, Transform parent,
            float fontSize, float height)
        {
            var go = Node(name, parent, typeof(TextMeshProUGUI), typeof(LayoutElement));
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.text = message;
            go.GetComponent<LayoutElement>().preferredHeight = height;
            return text;
        }

        private static GameObject Node(string name, Transform parent, params Type[] types)
        {
            var components = new Type[types.Length + 1];
            components[0] = typeof(RectTransform);
            Array.Copy(types, 0, components, 1, types.Length);
            var go = new GameObject(name, components);
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
