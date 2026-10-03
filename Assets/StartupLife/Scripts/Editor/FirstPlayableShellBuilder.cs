using System;
using System.Linq;
using TMPro;
using StartupLife.Content;
using StartupLife.Presentation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace StartupLife.Editor
{
    public static class FirstPlayableShellBuilder
    {
        const string Root = "Assets/StartupLife";
        public const string ScenePath = Root + "/Scenes/FirstPlayable.unity";

        [MenuItem("Startup Life/First Playable/Create shell")]
        public static void Create()
        {
            if (System.IO.File.Exists(ScenePath))
                throw new InvalidOperationException("FirstPlayable scene exists. Review it before replacing it.");

            FirstPlayableContentBuilder.CreateOrUpdate();

            var theme = AssetDatabase.LoadAssetAtPath<MobileTheme>(Root + "/Data/MobileTheme.asset");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/UI/Fonts/NotoSansVietnamese.asset");
            var contentAsset = AssetDatabase.LoadAssetAtPath<StartupLifeContentCatalogAsset>(FirstPlayableContentBuilder.AssetPath);
            if (!theme || !font || !contentAsset)
                throw new InvalidOperationException("Run the mobile baseline and content builders before creating the first playable shell.");

            var locale = LocalizationEditorSettings.GetLocales().FirstOrDefault(x => x.Identifier.Code == "vi");
            if (!locale) throw new InvalidOperationException("Vietnamese locale is required.");
            var collection = LocalizationEditorSettings.GetStringTableCollections()
                .FirstOrDefault(x => x.TableCollectionName == LocalizedKeyLabel.Table) ??
                LocalizationEditorSettings.CreateStringTableCollection(LocalizedKeyLabel.Table, Root + "/Localization");
            var table = (StringTable)collection.GetTable(locale.Identifier);
            AddStrings(table);
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(collection);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(theme);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvas = new GameObject("FirstPlayableCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler),
                typeof(UnityEngine.UI.GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            var safeArea = new GameObject("SafeArea", typeof(RectTransform), typeof(MobileSafeArea));
            safeArea.transform.SetParent(canvas.transform, false);
            Stretch(safeArea.GetComponent<RectTransform>());

            var fatal = LocalizedValue("FatalStatus", "fatal.none", safeArea.transform, font, theme, 110);
            fatal.gameObject.SetActive(true);

            var characterPanel = Panel("CharacterCreationPanel", safeArea.transform, theme);
            var character = characterPanel.AddComponent<CharacterCreationViewController>();
            StaticLabel("CharacterTitle", "character.title", characterPanel.transform, font, theme.heading, theme.ink, 100);
            StaticLabel("CharacterNameLabel", "character.name", characterPanel.transform, font, theme.body, theme.ink, 70);
            var nameInput = Input("CharacterNameInput", characterPanel.transform, font, theme);
            StaticLabel("CharacterAgeLabel", "character.age", characterPanel.transform, font, theme.body, theme.ink, 70);
            var ageRow = Row("AgeRow", characterPanel.transform, theme.gap);
            var ageMinus = Button("AgeMinusButton", "character.age.minus", ageRow.transform, font, theme);
            var ageValue = PlainValue("AgeValue", ageRow.transform, font, theme.body, theme.ink, 100);
            var agePlus = Button("AgePlusButton", "character.age.plus", ageRow.transform, font, theme);
            StaticLabel("AppearanceLabel", "character.appearance", characterPanel.transform, font, theme.body, theme.ink, 70);
            var appearanceRow = Row("AppearanceRow", characterPanel.transform, theme.gap);
            var female = Button("FemaleButton", "character.appearance.female", appearanceRow.transform, font, theme);
            var appearanceValue = PlainValue("AppearanceValue", appearanceRow.transform, font, theme.body, theme.accent, 100);
            var male = Button("MaleButton", "character.appearance.male", appearanceRow.transform, font, theme);
            var create = Button("CreateCharacterButton", "character.create", characterPanel.transform, font, theme);
            var characterStatus = LocalizedValue("CharacterStatus", "status.ready", characterPanel.transform, font, theme, 80);

            var lifePanel = Panel("LifePanel", safeArea.transform, theme);
            var life = lifePanel.AddComponent<LifeScreenViewController>();
            var work = lifePanel.AddComponent<WorkShiftPlaybackController>();
            StaticLabel("LifeTitle", "life.title", lifePanel.transform, font, theme.heading, theme.ink, 90);
            var nameValue = DataRow("Name", "life.name", lifePanel.transform, font, theme, out _);
            var dateValue = DataRow("Date", "life.date", lifePanel.transform, font, theme, out _);
            var timeValue = DataRow("Time", "life.time", lifePanel.transform, font, theme, out _);
            var cashValue = DataRow("Cash", "life.cash", lifePanel.transform, font, theme, out _);
            var careerRow = LabelRow("Career", "life.career", lifePanel.transform, font, theme);
            var careerValue = LocalizedValue("CareerValue", "career.none", careerRow.transform, font, theme, 70);
            var rankValue = DataRow("Rank", "life.rank", lifePanel.transform, font, theme, out _);
            var courseRow = LabelRow("Course", "life.course", lifePanel.transform, font, theme);
            var courseName = LocalizedValue("CourseNameValue", "course.none", courseRow.transform, font, theme, 70);
            var courseProgress = DataRow("CourseProgress", "life.course_progress", lifePanel.transform, font, theme, out _);
            var lifeStatus = LocalizedValue("LifeStatus", "status.ready", lifePanel.transform, font, theme, 90);

            var actions = new GameObject("Actions", typeof(RectTransform), typeof(UnityEngine.UI.VerticalLayoutGroup));
            actions.transform.SetParent(lifePanel.transform, false);
            var actionsLayout = actions.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            actionsLayout.spacing = theme.gap / 2f;
            actionsLayout.childControlHeight = true;
            actionsLayout.childControlWidth = true;
            actionsLayout.childForceExpandHeight = false;
            actionsLayout.childForceExpandWidth = true;

            var accept = Button("AcceptDeveloperButton", "action.accept_developer", actions.transform, font, theme);
            var workButton = Button("WorkButton", "action.work", actions.transform, font, theme);
            var buyCourse = Button("BuyCourseButton", "action.buy_course", actions.transform, font, theme);
            var study = Button("Study60Button", "action.study_60", actions.transform, font, theme);
            var day = Button("AdvanceDayButton", "action.advance_day", actions.transform, font, theme);
            var resign = Button("ResignButton", "action.resign", actions.transform, font, theme);

            var summaryRoot = Panel("DaySummaryModal", safeArea.transform, theme);
            var summary = summaryRoot.AddComponent<DaySummaryModal>();
            StaticLabel("SummaryTitle", "summary.title", summaryRoot.transform, font, theme.heading, theme.ink, 90);
            var summaryDate = DataRow("SummaryDate", "summary.date", summaryRoot.transform, font, theme, out _);
            var summaryCash = DataRow("SummaryCash", "summary.cash", summaryRoot.transform, font, theme, out _);
            var summaryXp = DataRow("SummaryXp", "summary.xp", summaryRoot.transform, font, theme, out _);
            var closeSummary = Button("CloseSummaryButton", "summary.close", summaryRoot.transform, font, theme);

            var bootstrapObject = new GameObject("StartupLifeBootstrapper");
            var bootstrap = bootstrapObject.AddComponent<StartupLifeBootstrapper>();

            Assign(character, "root", characterPanel);
            Assign(character, "nameInput", nameInput);
            Assign(character, "ageValue", ageValue);
            Assign(character, "appearanceValue", appearanceValue);
            Assign(character, "status", characterStatus);

            Assign(life, "root", lifePanel);
            Assign(life, "nameValue", nameValue);
            Assign(life, "dateValue", dateValue);
            Assign(life, "timeValue", timeValue);
            Assign(life, "cashValue", cashValue);
            Assign(life, "rankValue", rankValue);
            Assign(life, "courseProgressValue", courseProgress);
            Assign(life, "careerValue", careerValue);
            Assign(life, "courseNameValue", courseName);
            Assign(life, "status", lifeStatus);
            Assign(life, "acceptJobButton", accept);
            Assign(life, "workButton", workButton);
            Assign(life, "buyCourseButton", buyCourse);
            Assign(life, "studyButton", study);
            Assign(life, "resignButton", resign);

            Assign(summary, "root", summaryRoot);
            Assign(summary, "dateValue", summaryDate);
            Assign(summary, "cashDeltaValue", summaryCash);
            Assign(summary, "careerXpValue", summaryXp);

            Assign(bootstrap, "contentAsset", contentAsset);
            Assign(bootstrap, "characterCreation", character);
            Assign(bootstrap, "lifeScreen", life);
            Assign(bootstrap, "workPlayback", work);
            Assign(bootstrap, "daySummary", summary);
            Assign(bootstrap, "fatalStatus", fatal);

            UnityEventTools.AddPersistentListener(ageMinus.onClick, character.DecreaseAge);
            UnityEventTools.AddPersistentListener(agePlus.onClick, character.IncreaseAge);
            UnityEventTools.AddPersistentListener(female.onClick, character.SelectFemale);
            UnityEventTools.AddPersistentListener(male.onClick, character.SelectMale);
            UnityEventTools.AddPersistentListener(create.onClick, character.Submit);

            UnityEventTools.AddPersistentListener(accept.onClick, life.AcceptDeveloper);
            UnityEventTools.AddPersistentListener(workButton.onClick, life.RunWorkShift);
            UnityEventTools.AddPersistentListener(buyCourse.onClick, life.PurchaseCommunicationCourse);
            UnityEventTools.AddPersistentListener(study.onClick, life.Study60Minutes);
            UnityEventTools.AddPersistentListener(day.onClick, life.AdvanceDay);
            UnityEventTools.AddPersistentListener(resign.onClick, life.Resign);
            UnityEventTools.AddPersistentListener(closeSummary.onClick, summary.Hide);

            characterPanel.SetActive(true);
            lifePanel.SetActive(false);
            summaryRoot.SetActive(false);

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            var existing = EditorBuildSettings.scenes.Where(x => x.path != ScenePath).ToList();
            existing.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = existing.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[StartupLifeFirstPlayable] Created source-wired first playable shell.");
        }

        static void CreateCamera(MobileTheme theme)
        {
            var camera = new GameObject("MainCamera", typeof(Camera)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = theme.background;
            camera.clearFlags = CameraClearFlags.SolidColor;
        }

        static GameObject Panel(string name, Transform parent, MobileTheme theme)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.VerticalLayoutGroup));
            obj.transform.SetParent(parent, false);
            Stretch(obj.GetComponent<RectTransform>());
            obj.GetComponent<UnityEngine.UI.Image>().color = theme.panel;
            var layout = obj.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(theme.padding, theme.padding, theme.padding, theme.padding);
            layout.spacing = theme.gap;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            return obj;
        }

        static GameObject Row(string name, Transform parent, float spacing)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.HorizontalLayoutGroup), typeof(UnityEngine.UI.LayoutElement));
            obj.transform.SetParent(parent, false);
            var layout = obj.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            obj.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 110;
            return obj;
        }

        static GameObject LabelRow(string name, string key, Transform parent, TMP_FontAsset font, MobileTheme theme)
        {
            var row = Row(name + "Row", parent, theme.gap);
            StaticLabel(name + "Label", key, row.transform, font, theme.caption, theme.ink, 70);
            return row;
        }

        static TMP_Text DataRow(string name, string key, Transform parent, TMP_FontAsset font, MobileTheme theme, out GameObject row)
        {
            row = LabelRow(name, key, parent, font, theme);
            return PlainValue(name + "Value", row.transform, font, theme.body, theme.ink, 70);
        }

        static TMP_Text StaticLabel(string name, string key, Transform parent, TMP_FontAsset font, float size, Color color, float height)
        {
            var text = PlainValue(name, parent, font, size, color, height);
            var localized = text.gameObject.AddComponent<LocalizeStringEvent>();
            localized.StringReference.SetReference(LocalizedKeyLabel.Table, key);
            UnityEventTools.AddPersistentListener(localized.OnUpdateString, text.SetText);
            return text;
        }

        static LocalizedKeyLabel LocalizedValue(string name, string key, Transform parent, TMP_FontAsset font, MobileTheme theme, float height)
        {
            var text = PlainValue(name, parent, font, theme.body, theme.ink, height);
            var localizer = text.gameObject.AddComponent<LocalizeStringEvent>();
            localizer.StringReference.SetReference(LocalizedKeyLabel.Table, key);
            UnityEventTools.AddPersistentListener(localizer.OnUpdateString, text.SetText);
            var adapter = text.gameObject.AddComponent<LocalizedKeyLabel>();
            Assign(adapter, "localizer", localizer);
            return adapter;
        }

        static TMP_Text PlainValue(string name, Transform parent, TMP_FontAsset font, float size, Color color, float height)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(UnityEngine.UI.LayoutElement));
            obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
            text.enableWordWrapping = true;
            obj.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = height;
            return text;
        }

        static TMP_InputField Input(string name, Transform parent, TMP_FontAsset font, MobileTheme theme)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(TMP_InputField), typeof(UnityEngine.UI.LayoutElement));
            root.transform.SetParent(parent, false);
            root.GetComponent<UnityEngine.UI.Image>().color = Color.white;
            root.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = theme.touchTarget;

            var viewport = new GameObject("TextArea", typeof(RectTransform), typeof(UnityEngine.UI.RectMask2D));
            viewport.transform.SetParent(root.transform, false);
            Stretch(viewport.GetComponent<RectTransform>(), 24);

            var text = PlainValue("Text", viewport.transform, font, theme.body, theme.ink, theme.touchTarget);
            Stretch(text.rectTransform);
            var placeholder = PlainValue("Placeholder", viewport.transform, font, theme.body, new Color(theme.ink.r, theme.ink.g, theme.ink.b, 0.45f), theme.touchTarget);
            Stretch(placeholder.rectTransform);
            var placeholderLocalization = placeholder.gameObject.AddComponent<LocalizeStringEvent>();
            placeholderLocalization.StringReference.SetReference(LocalizedKeyLabel.Table, "character.name.placeholder");
            UnityEventTools.AddPersistentListener(placeholderLocalization.OnUpdateString, placeholder.SetText);

            var input = root.GetComponent<TMP_InputField>();
            input.textViewport = viewport.GetComponent<RectTransform>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = 80;
            input.lineType = TMP_InputField.LineType.SingleLine;
            return input;
        }

        static UnityEngine.UI.Button Button(string name, string key, Transform parent, TMP_FontAsset font, MobileTheme theme)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button), typeof(UnityEngine.UI.LayoutElement));
            obj.transform.SetParent(parent, false);
            var image = obj.GetComponent<UnityEngine.UI.Image>();
            image.color = theme.accent;
            var button = obj.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            obj.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = theme.touchTarget;
            var label = StaticLabel("Label", key, obj.transform, font, theme.body, Color.white, theme.touchTarget);
            Stretch(label.rectTransform, 12);
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }

        static void AddStrings(StringTable table)
        {
            Add(table, "fatal.none", "");
            Add(table, "fatal.content_missing", "Thiếu dữ liệu trò chơi.");
            Add(table, "fatal.save_unavailable", "Không thể mở dữ liệu đã lưu.");
            Add(table, "fatal.bootstrap_failed", "Không thể khởi động trò chơi.");

            Add(table, "character.title", "Tạo nhân vật");
            Add(table, "character.name", "Tên của bạn");
            Add(table, "character.name.placeholder", "Nhập tên");
            Add(table, "character.age", "Tuổi");
            Add(table, "character.age.minus", "−");
            Add(table, "character.age.plus", "+");
            Add(table, "character.appearance", "Nhân vật");
            Add(table, "character.appearance.female", "Nữ");
            Add(table, "character.appearance.male", "Nam");
            Add(table, "character.create", "Bắt đầu");

            Add(table, "life.title", "Hôm nay của bạn");
            Add(table, "life.name", "Tên");
            Add(table, "life.date", "Ngày");
            Add(table, "life.time", "Giờ");
            Add(table, "life.cash", "Tiền mặt");
            Add(table, "life.career", "Công việc");
            Add(table, "life.rank", "Cấp bậc");
            Add(table, "life.course", "Khóa học");
            Add(table, "life.course_progress", "Tiến độ");

            Add(table, "career.none", "Chưa có việc");
            Add(table, "career.developer", "Lập trình viên");
            Add(table, "course.none", "Chưa học khóa nào");
            Add(table, "course.communication", "Giao tiếp cơ bản");
            Add(table, "course.problem", "Giải quyết vấn đề");

            Add(table, "action.accept_developer", "Nhận việc Lập trình viên");
            Add(table, "action.work", "Đi làm");
            Add(table, "action.buy_course", "Mua khóa Giao tiếp");
            Add(table, "action.study_60", "Học 60 phút");
            Add(table, "action.advance_day", "Kết thúc ngày");
            Add(table, "action.resign", "Nghỉ việc");

            Add(table, "status.ready", "");
            Add(table, "status.character.created", "Nhân vật đã sẵn sàng.");
            Add(table, "status.career.accepted", "Đã nhận việc.");
            Add(table, "status.career.resigned", "Đã nghỉ việc.");
            Add(table, "status.course.purchased", "Đã đăng ký khóa học.");
            Add(table, "status.course.progressed", "Đã học thêm.");
            Add(table, "status.course.completed", "Đã hoàn thành khóa học.");
            Add(table, "status.work.complete", "Ca làm việc đã hoàn tất.");
            Add(table, "status.day.completed", "Đã sang ngày mới.");

            Add(table, "scene.coding", "Tập trung viết mã");
            Add(table, "scene.meeting", "Họp cùng đội");
            Add(table, "scene.bug-fixing", "Sửa lỗi");
            Add(table, "scene.client-discussion", "Trao đổi với khách hàng");
            Add(table, "scene.demo", "Trình bày bản demo");
            Add(table, "scene.documentation", "Viết tài liệu");

            Add(table, "reason.career.required", "Bạn cần có công việc trước.");
            Add(table, "reason.character.invalid", "Thông tin nhân vật chưa hợp lệ.");
            Add(table, "reason.content.missing", "Nội dung này chưa khả dụng.");
            Add(table, "reason.appearance.invalid", "Nhân vật đã chọn chưa hợp lệ.");
            Add(table, "reason.career.already_employed", "Bạn đang có công việc.");
            Add(table, "reason.economy.insufficient_cash", "Bạn chưa đủ tiền.");
            Add(table, "reason.course.active", "Bạn đang học một khóa khác.");
            Add(table, "reason.course.target_met", "Kỹ năng đã đạt mục tiêu của khóa.");
            Add(table, "reason.course.prerequisite", "Bạn chưa đủ điều kiện học khóa này.");
            Add(table, "reason.arrears.blocked", "Hãy thanh toán khoản còn thiếu trước.");
            Add(table, "reason.time.unavailable", "Khoảng thời gian này chưa thể dùng.");
            Add(table, "reason.playback.stale", "Cảnh này đã được xử lý.");
            Add(table, "reason.command.stale_state", "Trạng thái vừa thay đổi, hãy thử lại.");
            Add(table, "reason.state.invalid", "Trạng thái trò chơi không hợp lệ.");
            Add(table, "reason.save.write_failed", "Không thể lưu tiến trình.");
            Add(table, "reason.save.invalid", "Dữ liệu lưu không hợp lệ.");
            Add(table, "reason.save.content_id", "Dữ liệu lưu cần nội dung chưa có.");
            Add(table, "reason.save.content_version", "Phiên bản nội dung của dữ liệu lưu chưa được hỗ trợ.");

            Add(table, "advance.PlayerChoice", "Cần đưa ra lựa chọn trước.");
            Add(table, "advance.InvalidRequest", "Không thể chuyển ngày lúc này.");
            Add(table, "advance.InvalidState", "Trạng thái hiện tại không thể chuyển ngày.");
            Add(table, "advance.PersistenceOrRuleFailure", "Không thể hoàn tất chuyển ngày.");
            Add(table, "advance.RecoveryRequired", "Cần khôi phục dữ liệu lưu.");

            Add(table, "summary.title", "Tổng kết ngày");
            Add(table, "summary.date", "Ngày mới");
            Add(table, "summary.cash", "Thay đổi tiền");
            Add(table, "summary.xp", "Kinh nghiệm nghề");
            Add(table, "summary.close", "Đóng");
        }

        static void Add(StringTable table, string key, string value)
        {
            var entry = table.GetEntry(key);
            if (entry == null) table.AddEntry(key, value);
            else entry.Value = value;
        }

        static void Assign(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException(target.GetType().Name + "." + propertyName + " was not found.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
