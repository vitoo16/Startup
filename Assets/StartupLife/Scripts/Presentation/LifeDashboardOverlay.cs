#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TMPro;
using StartupLife.Application;
using StartupLife.Core;
using UnityEngine;
using UnityEngine.UI;

namespace StartupLife.Presentation
{
    // Five-tab portrait dashboard. The hierarchy is installed into LifePanel,
    // which is already a child of MobileSafeArea in FirstPlayable.unity.
    // Switch/open/refresh are purely read-only and do not submit any command.
    public sealed class LifeDashboardOverlay : MonoBehaviour
    {
        public enum Section { Career, Skills, Finance, Business, Profile }

        private const float TouchHeight = 96f;
        private static readonly CultureInfo CurrencyCulture = CultureInfo.GetCultureInfo("vi-VN");
        private static readonly Section[] Sections =
            { Section.Career, Section.Skills, Section.Finance, Section.Business, Section.Profile };
        private static readonly string[] Titles =
            { "Nghề nghiệp", "Kỹ năng", "Tài chính", "Kinh doanh", "Hồ sơ" };
        private static readonly string[] ShortTitles =
            { "Nghề", "Kỹ năng", "Tiền", "Cửa hàng", "Hồ sơ" };
        // UI color roles come from the same serialized theme as the scene.
        // All text roles use opaque ink or high-contrast white, not faded alpha.
        private MobileTheme theme = null!;
        private Color Background => theme.background;
        private Color Surface => theme.panel;
        private Color Ink => theme.ink;
        private Color Accent => theme.accent;
        private Color Muted => theme.ink;

        private LifeScreenViewController host = null!;
        private Func<LifeDashboardSnapshot> reader = null!;
        private TMP_FontAsset font = null!;
        private GameObject panel = null!;
        private TMP_Text heading = null!;
        private ScrollRect scroll = null!;
        private Section selected = Section.Career;
        private readonly Dictionary<Section, Button> nav = new Dictionary<Section, Button>();
        private readonly Dictionary<Section, GameObject> pages = new Dictionary<Section, GameObject>();
        private TMP_Text careerSummary = null!;
        private TMP_Text careerHistory = null!;
        private TMP_Text salary = null!;
        private Button accept = null!;
        private Button work = null!;
        private Button resign = null!;
        private TMP_Text skills = null!;
        private TMP_Text courses = null!;
        private Button purchase = null!;
        private Button study = null!;
        private TMP_Text finance = null!;
        private TMP_Text arrears = null!;
        private TMP_Text business = null!;
        private TMP_Text profile = null!;
        private TMP_Text guideTitle = null!;
        private TMP_Text guideBody = null!;
        private TMP_Text textScaleLabel = null!;
        private Button previousGuide = null!;
        private Button nextGuide = null!;
        private Button finishGuide = null!;
        private int guideIndex;
        private float textScale = 1f;
        private readonly Dictionary<TMP_Text, float> originalFontSizes = new Dictionary<TMP_Text, float>();
        private const string TextScalePreference = "startup-life.ui.text-scale.v1";
        private const string TutorialPreferencePrefix = "startup-life.ui.guide.v";

        public static LifeDashboardOverlay Install(LifeScreenViewController view,
            Func<LifeDashboardSnapshot> read, TMP_Text fontSource)
        {
            if (view == null || read == null || fontSource == null || fontSource.font == null)
                throw new ArgumentException("Dashboard needs a bound life view, data reader and Vietnamese TMP font.");
            var overlay = view.GetComponent<LifeDashboardOverlay>();
            if (overlay == null) overlay = view.gameObject.AddComponent<LifeDashboardOverlay>();
            if (overlay.panel != null) return overlay;
            overlay.host = view;
            overlay.reader = read;
            overlay.font = fontSource.font;
            overlay.theme = view.UiTheme;
            overlay.Build();
            return overlay;
        }

        public bool IsOpen => panel != null && panel.activeSelf;
        public Section SelectedSection => selected;

        public void Open()
        {
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            Select(selected);
            UiPanelReveal.Show(panel);
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        public void Select(Section section)
        {
            if (!pages.ContainsKey(section)) throw new ArgumentOutOfRangeException(nameof(section));
            selected = section;
            foreach (var page in pages) page.Value.SetActive(page.Key == section);
            if (panel != null && panel.activeInHierarchy)
                UiPanelReveal.Show(pages[section]);
            foreach (var button in nav)
            {
                button.Value.GetComponent<Image>().color =
                    button.Key == section ? Accent : Surface;
                var label = button.Value.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.color = button.Key == section ? Color.white : Ink;
                button.Value.interactable = button.Key != section;
            }
            ApplyStaticTranslation(heading, Titles[(int)section]);
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
            Refresh();
        }

        public void Refresh()
        {
            // A closed overlay cannot affect gameplay or allocate UI text.
            if (!IsOpen) return;
            var model = reader();
            var career = model.Career;
            var current = career.CurrentEmployment;
            careerSummary.text = current == null
                ? (career.IsFullTimeFounder ? "Đang tập trung khởi nghiệp" : "Hiện chưa có công việc")
                : "Đang làm: " + DisplayCareer(current.CareerNameKey) +
                  "\nCấp nghề: " + (current.RankIndex + 1).ToString(CultureInfo.InvariantCulture) +
                  " · Kinh nghiệm: " + current.CareerXp.ToString("N0", CurrencyCulture) +
                  "\nLương mỗi tháng: " + Money(current.MonthlySalaryAtRecordedRankVnd ?? 0);
            careerHistory.text = career.PreviousEmployments.Count == 0
                ? "Chưa có công việc trước đây"
                : string.Join("\n", career.PreviousEmployments.Select(x =>
                    DisplayCareer(x.CareerNameKey) + " · " + x.StartedIso + " – " +
                    (x.EndedIso ?? "hiện tại") + " · bậc " +
                    (x.RankIndex + 1).ToString(CultureInfo.InvariantCulture)));
            salary.text = "Lương chờ nhận: " + Money(career.OutstandingSalary.FloorToInt64()) +
                (career.OutstandingSalary.Denominator != System.Numerics.BigInteger.One
                    ? "" : "");
            accept.interactable = career.HasCharacter && !career.IsEmployed;
            work.interactable = career.IsEmployed;
            resign.interactable = career.IsEmployed;

            skills.text = string.Join("\n", model.Skills.Select(x =>
                DisplaySkill(x.NameKey) + "  •  Cấp " +
                x.Level.ToString(CultureInfo.InvariantCulture) + "/5" +
                "  •  XP " + x.Exposure.ToString("N0", CurrencyCulture) +
                (x.NextLevelExposure.HasValue
                    ? "/" + x.NextLevelExposure.Value.ToString("N0", CurrencyCulture)
                    : " · Tối đa")));
            courses.text = string.Join("\n", model.Courses.Select(x =>
                DisplayCourse(x.NameKey) + " · lên cấp " +
                x.TargetLevel.ToString(CultureInfo.InvariantCulture) +
                "\n" + Money(x.PriceVnd) + " · khoảng " +
                x.BaseStudyMinutes.ToString(CultureInfo.InvariantCulture) + " phút" +
                (x.IsActive && x.TargetUnits.HasValue && x.TargetUnits.Value > 0
                    ? " · đang học " + (x.ProgressUnits!.Value * 100 / x.TargetUnits.Value)
                        .ToString(CultureInfo.InvariantCulture) + "%"
                    : "")));
            purchase.interactable = model.Game.ActiveCourse == null &&
                model.Finance.OutstandingArrearsVnd == 0;
            study.interactable = model.Game.ActiveCourse != null;

            finance.text = "Tiền mặt: " + Money(model.Finance.CashVnd) +
                "\nNợ cần trả: " + Money(model.Finance.OutstandingArrearsVnd) +
                "\nLương chờ nhận: " + Money(model.Finance.UnpaidSalaryExact.FloorToInt64()) +
                "\nDoanh thu đã chốt: " +
                    Money(model.Finance.CommittedBusinessGrossVnd) +
                "\nLãi đã chốt: " +
                    Money(model.Finance.CommittedBusinessProfitVnd);
            arrears.text = model.Finance.Arrears.Count == 0
                ? "Hiện không có khoản nợ đến hạn."
                : string.Join("\n", model.Finance.Arrears.Select(x =>
                    "Đến hạn " + x.DueIso + " · " + Money(x.OutstandingVnd)));

            business.text = "Doanh nghiệp đang hoạt động: " +
                career.ActiveBusinessCount.ToString(CultureInfo.InvariantCulture) +
                "\nMỗi quyết định đều cần cân nhắc vốn và thời gian." +
                "\nMở Quản lý doanh nghiệp để xem lịch, giá bán và thu chi.";
            profile.text = "Tên: " + model.Game.Name +
                "\nHôm nay: " + model.Game.Instant.Date +
                "\n" + (career.IsFullTimeFounder ? "Vai trò: Nhà sáng lập toàn thời gian" :
                    career.IsEmployed ? "Vai trò: Người đi làm" : "Vai trò: Đang tìm hướng đi") +
                "\nCông việc từng trải qua: " +
                    career.PreviousEmployments.Count.ToString(CultureInfo.InvariantCulture) +
                "\nDoanh nghiệp đang hoạt động: " +
                    career.ActiveBusinessCount.ToString(CultureInfo.InvariantCulture);
            RefreshGuidance(model.Game.RunId);
            textScaleLabel.text = "Cỡ chữ giao diện: " +
                Math.Round(textScale * 100f).ToString(CultureInfo.InvariantCulture) + "%";
        }

        private static string GuidePreference(string runId) =>
            TutorialPreferencePrefix +
            OnboardingGuideCatalog.Version.ToString(CultureInfo.InvariantCulture) +
            "." + runId;

        private void RefreshGuidance(string runId)
        {
            var completed = PlayerPrefs.GetInt(GuidePreference(runId), 0) ==
                OnboardingGuideCatalog.Version;
            var key = OnboardingGuideCatalog.At(guideIndex);
            guideTitle.text = completed ? "Đã xem hướng dẫn" :
                "Bước " + (guideIndex + 1).ToString(CultureInfo.InvariantCulture) +
                "/" + OnboardingGuideCatalog.Topics.Count.ToString(CultureInfo.InvariantCulture) +
                " · " + GuideHeading(key);
            ApplyStaticTranslation(guideBody, completed
                ? "Bạn có thể xem lại hướng dẫn bất kỳ lúc nào."
                : GuideExplanation(key));
            previousGuide.interactable = !completed && guideIndex > 0;
            nextGuide.interactable = !completed &&
                guideIndex < OnboardingGuideCatalog.Topics.Count - 1;
            finishGuide.interactable = !completed;
        }

        private void ChangeGuide(int delta)
        {
            guideIndex = Mathf.Clamp(guideIndex + delta, 0,
                OnboardingGuideCatalog.Topics.Count - 1);
            Refresh();
        }

        private void CompleteGuide()
        {
            var model = reader();
            PlayerPrefs.SetInt(GuidePreference(model.Game.RunId), OnboardingGuideCatalog.Version);
            PlayerPrefs.Save();
            Refresh();
        }

        private void ReplayGuide()
        {
            var model = reader();
            guideIndex = 0;
            PlayerPrefs.DeleteKey(GuidePreference(model.Game.RunId));
            PlayerPrefs.Save();
            Refresh();
        }

        private void ChangeTextScale(float step)
        {
            textScale = Mathf.Clamp(
                (float)Math.Round((textScale + step) * 10f) / 10f, 0.9f, 1.2f);
            PlayerPrefs.SetFloat(TextScalePreference, textScale);
            PlayerPrefs.Save();
            foreach (var item in originalFontSizes)
                if (item.Key) item.Key.fontSize = item.Value * textScale;
            Refresh();
        }

        // First-playable Vietnamese fallback strings. Before release, transfer
        // these whole sentences into the FirstPlayableUI string table with
        // ICU variables; never concatenate translated fragments.
        private static string GuideHeading(string key) => key switch
        {
            "onboarding.skills.free" => "Kỹ năng có thể luyện miễn phí",
            "onboarding.study.paid" => "Khóa học cần học phí",
            "onboarding.business.compatibility" => "Chọn kinh doanh phù hợp",
            "onboarding.time.automatic" => "Lịch được phân bổ tự động",
            "onboarding.resign.not-forced" => "Nghỉ việc không bắt buộc khởi nghiệp",
            _ => throw new ArgumentOutOfRangeException(nameof(key))
        };

        private static string GuideExplanation(string key) => key switch
        {
            "onboarding.skills.free" =>
                "Đi làm và trải nghiệm có thể tăng kỹ năng miễn phí. Bạn không cần mua khóa học để bắt đầu học hỏi.",
            "onboarding.study.paid" =>
                "Khóa học có giá và yêu cầu riêng. Xem học phí, thời lượng và trình độ trước khi mua; học mới tiêu tốn thời gian.",
            "onboarding.business.compatibility" =>
                "Một số nghề có thể kinh doanh thêm buổi tối. Quầy cà phê cần thời gian trực tiếp ban ngày và không phù hợp khi còn làm toàn thời gian.",
            "onboarding.time.automatic" =>
                "Game tự chia thời gian cho công việc và các doanh nghiệp. Xem thời gian phân bổ trước khi mở thêm; không cần tự xếp lịch từng giờ.",
            "onboarding.resign.not-forced" =>
                "Nghỉ việc giúp giải phóng thời gian, nhưng không bắt buộc mở doanh nghiệp. Bạn có thể học, tìm việc lại hoặc khởi nghiệp khi đã sẵn sàng.",
            _ => throw new ArgumentOutOfRangeException(nameof(key))
        };

        private static void ApplyStaticTranslation(TMP_Text text, string fallback)
        {
            text.text = fallback;
            if (DashboardLocalizationCatalog.TryFindKey(fallback, out var key))
                LocalizedKeyLabel.EnsureFor(text).SetKey(key);
        }

        private void OpenBusiness()
        {
            Hide();
            host.OpenBusinessManagement();
        }

        private void Build()
        {
            textScale = Mathf.Clamp(PlayerPrefs.GetFloat(TextScalePreference, 1f), 0.9f, 1.2f);
            var open = ButtonNode("LifeDashboardButton", "Tổng quan", transform, Open);
            var openLayout = open.GetComponent<LayoutElement>();
            openLayout.ignoreLayout = true;
            var openRect = (RectTransform)open.transform;
            openRect.anchorMin = openRect.anchorMax = new Vector2(0, 1);
            openRect.pivot = new Vector2(0, 1);
            openRect.sizeDelta = new Vector2(270, TouchHeight);
            openRect.anchoredPosition = new Vector2(22, -18);

            panel = Node("LifeDashboardOverlay", transform, typeof(Image), typeof(VerticalLayoutGroup),
                typeof(LayoutElement));
            panel.GetComponent<LayoutElement>().ignoreLayout = true;
            panel.GetComponent<Image>().color = Background;
            var bounds = (RectTransform)panel.transform;
            bounds.anchorMin = new Vector2(0.025f, 0.035f);
            bounds.anchorMax = new Vector2(0.975f, 0.94f);
            bounds.offsetMin = bounds.offsetMax = Vector2.zero;
            var column = panel.GetComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(20, 20, 20, 20);
            column.spacing = 12;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            column.childAlignment = TextAnchor.UpperCenter;

            var header = Node("LifeDashboardHeader", panel.transform, typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            header.GetComponent<LayoutElement>().preferredHeight = TouchHeight;
            var headerRow = header.GetComponent<HorizontalLayoutGroup>();
            headerRow.spacing = 12;
            headerRow.childControlWidth = headerRow.childControlHeight = true;
            headerRow.childForceExpandWidth = false;
            heading = TextNode("LifeDashboardTitle", header.transform, "Nghề nghiệp", 41, Ink);
            heading.GetComponent<LayoutElement>().flexibleWidth = 1;
            ButtonNode("LifeDashboardCloseButton", "Đóng", header.transform, Hide)
                .GetComponent<LayoutElement>().preferredWidth = 150;

            var tabBar = Node("LifeDashboardTabs", panel.transform, typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            tabBar.GetComponent<LayoutElement>().preferredHeight = TouchHeight;
            var tabLayout = tabBar.GetComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 6;
            tabLayout.childControlWidth = tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childForceExpandHeight = false;
            for (var i = 0; i < Sections.Length; i++)
            {
                var captured = Sections[i];
                var button = ButtonNode("LifeTab" + captured + "Button", ShortTitles[i],
                    tabBar.transform, () => Select(captured));
                var layout = button.GetComponent<LayoutElement>();
                layout.flexibleWidth = 1;
                layout.minWidth = 0;
                nav.Add(captured, button.GetComponent<Button>());
            }

            var lifeScroll = MobileScrollViewFactory.Create(panel.transform,
                "LifeDashboardScroll", Surface, new RectOffset(20, 20, 22, 22),
                18f, TextAnchor.UpperLeft, 32f);
            scroll = lifeScroll.Scroll;
            var content = lifeScroll.Content;

            foreach (var section in Sections)
            {
                var page = Node("LifePage" + section, content.transform,
                    typeof(VerticalLayoutGroup), typeof(LayoutElement));
                var layout = page.GetComponent<VerticalLayoutGroup>();
                layout.spacing = 20;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                // Parent content's layout must size each page from its children.
                pages.Add(section, page);
            }

            var careerPage = pages[Section.Career].transform;
            Subheading(careerPage, "Công việc của bạn");
            careerSummary = TextNode("LifeCareerSummary", careerPage, "", 31, Ink);
            salary = TextNode("LifeUnpaidSalary", careerPage, "", 29, Ink);
            accept = ButtonNode("LifeCareerAcceptButton", "Nhận việc lập trình",
                careerPage, host.AcceptDeveloper).GetComponent<Button>();
            work = ButtonNode("LifeCareerWorkButton", "Đi làm",
                careerPage, host.RunWorkShift).GetComponent<Button>();
            resign = ButtonNode("LifeCareerResignButton", "Nghỉ việc",
                careerPage, host.Resign).GetComponent<Button>();
            Subheading(careerPage, "Lịch sử nghề nghiệp");
            careerHistory = TextNode("LifeCareerHistory", careerPage, "", 29, Muted);

            var skillPage = pages[Section.Skills].transform;
            Subheading(skillPage, "Tiến độ kỹ năng");
            skills = TextNode("LifeSkillsValue", skillPage, "", 30, Ink);
            Subheading(skillPage, "Các khóa học");
            courses = TextNode("LifeCoursesValue", skillPage, "", 29, Ink);
            purchase = ButtonNode("LifeBuyCourseButton", "Mua khóa Giao tiếp",
                skillPage, host.PurchaseCommunicationCourse).GetComponent<Button>();
            study = ButtonNode("LifeStudyButton", "Học 60 phút",
                skillPage, host.Study60Minutes).GetComponent<Button>();

            var financePage = pages[Section.Finance].transform;
            Subheading(financePage, "Thu chi của bạn");
            finance = TextNode("LifeFinanceValue", financePage, "", 30, Ink);
            Subheading(financePage, "Các khoản nợ");
            arrears = TextNode("LifeArrearsValue", financePage, "", 29, Muted);

            var businessPage = pages[Section.Business].transform;
            Subheading(businessPage, "Góc kinh doanh");
            business = TextNode("LifeBusinessValue", businessPage, "", 30, Ink);
            ButtonNode("LifeOpenBusinessButton", "Mở quản lý doanh nghiệp",
                businessPage, OpenBusiness);

            var profilePage = pages[Section.Profile].transform;
            Subheading(profilePage, "Thông tin hành trình");
            profile = TextNode("LifeProfileValue", profilePage, "", 30, Ink);
            TextNode("LifeProfileHelp", profilePage,
                "Bạn có thể làm việc, học kỹ năng và khởi nghiệp theo tốc độ của mình.",
                28, Muted);
            Subheading(profilePage, "Cài đặt hiển thị");
            textScaleLabel = TextNode("LifeTextScaleValue", profilePage, "", 30, Ink);
            ButtonNode("LifeTextSmallerButton", "Chữ nhỏ hơn",
                profilePage, () => ChangeTextScale(-0.1f));
            ButtonNode("LifeTextLargerButton", "Chữ lớn hơn",
                profilePage, () => ChangeTextScale(0.1f));
            Subheading(profilePage, "Hướng dẫn hành trình");
            guideTitle = TextNode("LifeGuideTitle", profilePage, "", 32, Accent);
            guideBody = TextNode("LifeGuideBody", profilePage, "", 29, Ink);
            previousGuide = ButtonNode("LifeGuidePreviousButton", "Bước trước",
                profilePage, () => ChangeGuide(-1)).GetComponent<Button>();
            nextGuide = ButtonNode("LifeGuideNextButton", "Bước tiếp",
                profilePage, () => ChangeGuide(1)).GetComponent<Button>();
            finishGuide = ButtonNode("LifeGuideFinishButton", "Đã hiểu",
                profilePage, CompleteGuide).GetComponent<Button>();
            ButtonNode("LifeGuideReplayButton", "Xem lại hướng dẫn",
                profilePage, ReplayGuide);
            panel.SetActive(false);
        }

        private void Subheading(Transform parent, string value) =>
            TextNode("LifeSectionHeading", parent, value, 34, Accent);

        private GameObject ButtonNode(string name, string value, Transform parent, Action onClick)
        {
            var go = Node(name, parent, typeof(Image), typeof(Button), typeof(LayoutElement));
            go.GetComponent<Image>().color = Accent;
            var size = go.GetComponent<LayoutElement>();
            size.minHeight = TouchHeight;
            size.preferredHeight = TouchHeight;
            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            // Press/highlight feedback uses the existing theme-colored Image;
            // no new texture, shader or off-grid transform is needed.
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.56f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(() => onClick());
            var label = TextNode(name + "Label", go.transform, value, 27, Color.white);
            label.alignment = TextAlignmentOptions.Center;
            var rect = (RectTransform)label.transform;
            Stretch(rect);
            rect.offsetMin = new Vector2(6, 3);
            rect.offsetMax = new Vector2(-6, -3);
            label.GetComponent<LayoutElement>().ignoreLayout = true;
            return go;
        }

        private TMP_Text TextNode(string name, Transform parent, string value,
            float size, Color color)
        {
            var node = Node(name, parent, typeof(TextMeshProUGUI), typeof(LayoutElement));
            var label = node.GetComponent<TextMeshProUGUI>();
            label.font = font;
            originalFontSizes.Add(label, size);
            label.fontSize = size * textScale;
            label.color = color;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            ApplyStaticTranslation(label, value);
            node.GetComponent<LayoutElement>().minHeight = 50;
            return label;
        }

        private static GameObject Node(string name, Transform parent, params Type[] components)
        {
            var types = new Type[components.Length + 1];
            types[0] = typeof(RectTransform);
            Array.Copy(components, 0, types, 1, components.Length);
            var node = new GameObject(name, types);
            node.transform.SetParent(parent, false);
            return node;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static string Money(long value) => value.ToString("N0", CurrencyCulture) + " ₫";

        // Temporary Vietnamese display catalog for the first five-tab pass.
        // No gameplay IDs, skill math, price or economic balance are authored here.
        private static string DisplayCareer(string? key) =>
            key == "career.developer" ? "Lập trình viên" : key ?? "Chưa xác định";

        private static string DisplaySkill(string key) => key switch
        {
            "skill.communication" => "Giao tiếp",
            "skill.negotiation" => "Đàm phán",
            "skill.time-management" => "Quản lý thời gian",
            "skill.problem-solving" => "Giải quyết vấn đề",
            "skill.networking" => "Xây dựng quan hệ",
            "skill.leadership" => "Lãnh đạo",
            _ => key
        };

        private static string DisplayCourse(string key) => key switch
        {
            "course.communication" => "Khóa Giao tiếp",
            "course.problem" => "Khóa Giải quyết vấn đề",
            _ => key
        };
    }
}
