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
            DashboardLocalizationEditorSync.Apply(table);
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(collection);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene unloads unused assets. Reload asset references before serialized wiring.
            theme = AssetDatabase.LoadAssetAtPath<MobileTheme>(Root + "/Data/MobileTheme.asset");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/UI/Fonts/NotoSansVietnamese.asset");
            contentAsset = AssetDatabase.LoadAssetAtPath<StartupLifeContentCatalogAsset>(FirstPlayableContentBuilder.AssetPath);
            CreateCamera(theme);
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

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
            ConfigureFatalStatus(fatal.GetComponent<TMP_Text>(), theme);
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
            var appearanceValue = LocalizedValue("AppearanceValue", "character.appearance.female", appearanceRow.transform, font, theme, 100);
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
            Assign(character, "appearanceValue", appearanceValue.GetComponent<TMP_Text>());
            Assign(character, "status", characterStatus);

            Assign(life, "root", lifePanel);
            Assign(life, "uiTheme", theme);
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
            Assign(summary, "cashValue", summaryCash);
            Assign(summary, "careerXpValue", summaryXp);

            Assign(bootstrap, "contentAsset", contentAsset);
            Assign(bootstrap, "characterCreation", character);
            Assign(bootstrap, "lifeScreen", life);
            Assign(bootstrap, "workPlayback", work);
            Assign(bootstrap, "daySummary", summary);
            Assign(bootstrap, "fatalStatus", fatal);
            // Scene regeneration must preserve the M15 production event path.
            // This is an explicit serialized bool, not a code-only initializer.
            Assign(bootstrap, "enableVerifiedEventV4", true);

            UnityEventTools.AddPersistentListener(ageMinus.onClick, character.DecreaseAge);
            UnityEventTools.AddPersistentListener(agePlus.onClick, character.IncreaseAge);
            UnityEventTools.AddPersistentListener(female.onClick, character.SelectFemale);
            UnityEventTools.AddPersistentListener(male.onClick, character.SelectMale);
            UnityEventTools.AddPersistentListener(create.onClick, character.Submit);
            UpgradeCharacterCreator(characterPanel, character, font, theme);

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

        [MenuItem("Startup Life/First Playable/Repair assets and Character Creator")]
        public static void RepairGeneratedAssetReferences()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var content = AssetDatabase.LoadAssetAtPath<StartupLifeContentCatalogAsset>(FirstPlayableContentBuilder.AssetPath);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/UI/Fonts/NotoSansVietnamese.asset");
            if (!content || !font) throw new InvalidOperationException("Authored Content and font assets are required.");
            var bootstrap = UnityEngine.Object.FindAnyObjectByType<StartupLifeBootstrapper>();
            if (!bootstrap) throw new InvalidOperationException("FirstPlayable composition root is missing.");
            Assign(bootstrap, "contentAsset", content);
            // Upgrading an existing scene must also opt into the verified V4
            // gateway; regenerating from scratch and upgrading agree.
            Assign(bootstrap, "enableVerifiedEventV4", true);
            foreach (var text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                text.font = font;
                EditorUtility.SetDirty(text);
            }
            var theme = AssetDatabase.LoadAssetAtPath<MobileTheme>(Root + "/Data/MobileTheme.asset");
            if (!theme) throw new InvalidOperationException("Missing authoritative pixel UI theme.");
            // LifePanel starts inactive; default FindAnyObjectByType skipped it.
            var life = UnityEngine.Object.FindAnyObjectByType<LifeScreenViewController>(
                FindObjectsInactive.Include);
            if (!life) throw new InvalidOperationException("Missing life screen for UI theme migration.");
            Assign(life, "uiTheme", theme);
            var character = UnityEngine.Object.FindAnyObjectByType<CharacterCreationViewController>(
                FindObjectsInactive.Include);
            if (!character) throw new InvalidOperationException("Missing character creator.");
            UpgradeCharacterCreator(character.gameObject, character, font, theme);
            foreach (var layout in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.VerticalLayoutGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                layout.spacing = theme.gap / 2f;
                EditorUtility.SetDirty(layout);
            }
            foreach (var row in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.HorizontalLayoutGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var height = row.GetComponent<UnityEngine.UI.LayoutElement>();
                if (!height) continue;
                // New creator selectors and navigation retain their 96-unit
                // baseline when the existing scene's layout is repaired.
                height.preferredHeight = row.name.StartsWith("Creator", StringComparison.Ordinal)
                    ? theme.touchTarget : 90;
                EditorUtility.SetDirty(height);
            }
            foreach (var button in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var element = button.GetComponent<UnityEngine.UI.LayoutElement>();
                element.minHeight = theme.touchTarget;
                EditorUtility.SetDirty(element);
            }
            var locale = LocalizationEditorSettings.GetLocales().Single(value => value.Identifier.Code == "vi");
            var collection = LocalizationEditorSettings.GetStringTableCollections().Single(value => value.TableCollectionName == LocalizedKeyLabel.Table);
            var table = (StringTable)collection.GetTable(locale.Identifier);
            AddStrings(table);
            DashboardLocalizationEditorSync.Apply(table);
            EditorUtility.SetDirty(table);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            FirstPlayableRuntimeGate.VerifyScene();
        }

        public static void RepairFatalSaveStatus()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var fatal = GameObject.Find("FatalStatus").GetComponent<TMP_Text>();
            var theme = AssetDatabase.LoadAssetAtPath<MobileTheme>(Root + "/Data/MobileTheme.asset");
            ConfigureFatalStatus(fatal, theme);
            var locale = LocalizationEditorSettings.GetLocales().Single(value => value.Identifier.Code == "vi");
            var collection = LocalizationEditorSettings.GetStringTableCollections().Single(value => value.TableCollectionName == LocalizedKeyLabel.Table);
            var table = (StringTable)collection.GetTable(locale.Identifier);
            AddStrings(table);
            DashboardLocalizationEditorSync.Apply(table);
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        [Serializable] private sealed class CreatorOptionsDocument
        {
            public CreatorTextEntry[] options = Array.Empty<CreatorTextEntry>();
        }
        [Serializable] private sealed class CreatorTextEntry
        {
            public string labelKey = "";
            public string label = "";
        }

        private const string CreatorOptionsPath =
            Root + "/Data/CharacterCreatorDraftCatalog.json";

        // Explicit, idempotent migration. Existing scenes keep their serialized
        // identity controls and onClick listeners; nothing is regenerated.
        private static void UpgradeCharacterCreator(GameObject panel,
            CharacterCreationViewController character, TMP_FontAsset font, MobileTheme theme)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(CreatorOptionsPath);
            if (!asset) throw new InvalidOperationException("Character Creator catalog missing.");
            Assign(character, "draftCatalog", asset);
            // Attach once even when the rest of the scene has already migrated.
            var touchSizer = panel.GetComponent<CharacterCreatorTouchSizer>();
            if (!touchSizer) touchSizer = panel.AddComponent<CharacterCreatorTouchSizer>();
            Assign(touchSizer, "uiTheme", theme);
            if (panel.transform.Find("CharacterCreatorScroll") != null) return;

            var scroll = MobileScrollViewFactory.Create(panel.transform,
                "CharacterCreatorScroll", theme.background,
                new RectOffset(12, 12, 16, 16), theme.gap / 2f,
                TextAnchor.UpperLeft, 32f);
            scroll.Scroll.transform.SetAsFirstSibling();
            var identity = CreatorSection("CreatorIdentityPage", scroll.Content.transform, theme);
            var look = CreatorSection("CreatorLookPage", scroll.Content.transform, theme);
            var wardrobe = CreatorSection("CreatorWardrobePage", scroll.Content.transform, theme);
            var traits = CreatorSection("CreatorTraitsPage", scroll.Content.transform, theme);
            foreach (var name in new[] { "CharacterTitle", "CharacterNameLabel",
                "CharacterNameInput", "CharacterAgeLabel", "AgeRow",
                "AppearanceLabel", "AppearanceRow" })
            {
                var child = panel.transform.Find(name);
                if (!child) throw new InvalidOperationException("Missing historical identity control: " + name);
                child.SetParent(identity.transform, false);
            }
            StaticLabel("CharacterLookTitle", "character.look.title", look.transform,
                font, theme.heading, theme.ink, 100);
            StaticLabel("CharacterPreviewPending", "character.preview.pending", look.transform,
                font, theme.caption, theme.ink, 96);
            CreatorSelector("Face", "character.face", "character.option.face.soft",
                look.transform, character, font, theme, character.PreviousFace, character.NextFace);
            CreatorSelector("Hair", "character.hair", "character.option.hair.short",
                look.transform, character, font, theme, character.PreviousHair, character.NextHair);
            CreatorSelector("HairColor", "character.haircolor", "character.option.haircolor.ink",
                look.transform, character, font, theme, character.PreviousHairColor, character.NextHairColor);
            CreatorSelector("SkinTone", "character.skintone", "character.option.skin.warm",
                look.transform, character, font, theme, character.PreviousSkinTone, character.NextSkinTone);
            StaticLabel("CharacterWardrobeTitle", "character.wardrobe.title", wardrobe.transform,
                font, theme.heading, theme.ink, 100);
            CreatorSelector("Outfit", "character.outfit", "character.option.outfit.casual",
                wardrobe.transform, character, font, theme, character.PreviousOutfit, character.NextOutfit);
            CreatorSelector("Accessory", "character.accessory", "character.option.accessory.none",
                wardrobe.transform, character, font, theme, character.PreviousAccessory, character.NextAccessory);
            StaticLabel("CharacterTraitsTitle", "character.trait.title", traits.transform,
                font, theme.heading, theme.ink, 100);
            StaticLabel("CharacterTraitsNote", "character.trait.note", traits.transform,
                font, theme.caption, theme.ink, 142);
            CreatorSelector("Trait", "character.trait", "character.option.trait.balanced",
                traits.transform, character, font, theme, character.PreviousTrait, character.NextTrait);

            var navigation = Row("CreatorPageNavigation", panel.transform, theme.gap);
            navigation.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = theme.touchTarget;
            var back = Button("CreatorPreviousPageButton", "character.page.previous",
                navigation.transform, font, theme);
            var next = Button("CreatorNextPageButton", "character.page.next",
                navigation.transform, font, theme);
            UnityEventTools.AddPersistentListener(back.onClick, character.PreviousCreatorPage);
            UnityEventTools.AddPersistentListener(next.onClick, character.NextCreatorPage);
            navigation.transform.SetSiblingIndex(1);
            var submit = panel.transform.Find("CreateCharacterButton");
            if (submit) submit.SetSiblingIndex(2);
            StaticLabel("CreatorUnsavedNote", "character.cosmetic.unsaved", panel.transform,
                font, theme.caption, theme.ink, 76);

            Assign(character, "identityPage", identity);
            Assign(character, "lookPage", look);
            Assign(character, "wardrobePage", wardrobe);
            Assign(character, "traitsPage", traits);
            Assign(character, "creatorScroll", scroll.Scroll);
            Assign(character, "creatorBackButton", back);
            Assign(character, "creatorNextButton", next);
            look.SetActive(false);
            wardrobe.SetActive(false);
            traits.SetActive(false);
        }

        private static GameObject CreatorSection(string name, Transform parent, MobileTheme theme)
        {
            var obj = new GameObject(name, typeof(RectTransform),
                typeof(UnityEngine.UI.VerticalLayoutGroup));
            obj.transform.SetParent(parent, false);
            var layout = obj.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = theme.gap / 2f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return obj;
        }

        private static void CreatorSelector(string name, string key, string initialKey,
            Transform parent, CharacterCreationViewController character,
            TMP_FontAsset font, MobileTheme theme, UnityEngine.Events.UnityAction previous,
            UnityEngine.Events.UnityAction next)
        {
            StaticLabel("Character" + name + "Label", key, parent,
                font, theme.body, theme.ink, 66);
            var row = Row("Creator" + name + "Row", parent, theme.gap / 2f);
            row.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = theme.touchTarget;
            var backButton = Button("Creator" + name + "PreviousButton",
                "character.option.previous", row.transform, font, theme);
            var value = LocalizedValue("Creator" + name + "Value", initialKey,
                row.transform, font, theme, theme.touchTarget);
            var nextButton = Button("Creator" + name + "NextButton",
                "character.option.next", row.transform, font, theme);
            UnityEventTools.AddPersistentListener(backButton.onClick, previous);
            UnityEventTools.AddPersistentListener(nextButton.onClick, next);
            var fieldName = name.Substring(0, 1).ToLowerInvariant() + name.Substring(1) + "Value";
            Assign(character, fieldName, value);
        }

        static void ConfigureFatalStatus(TMP_Text text, MobileTheme theme)
        {
            Stretch(text.rectTransform, theme.padding);
            text.alignment = TextAlignmentOptions.Center;
        }

        static void CreateCamera(MobileTheme theme)
        {
            var camera = new GameObject("MainCamera", typeof(Camera)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            PixelWorldCameraSetup.Configure(camera);
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
            layout.spacing = theme.gap / 2f;
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
            obj.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 90;
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
            text.textWrappingMode = TextWrappingModes.Normal;
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
            obj.GetComponent<UnityEngine.UI.LayoutElement>().minHeight = theme.touchTarget;
            var label = StaticLabel("Label", key, obj.transform, font, theme.body, Color.white, theme.touchTarget);
            Stretch(label.rectTransform, 12);
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }

        static void AddStrings(StringTable table)
        {
            Add(table, "fatal.none", " ");
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
            Add(table, "character.look.title", "Diện mạo");
            Add(table, "character.wardrobe.title", "Trang phục");
            Add(table, "character.trait.title", "Phong cách cá nhân");
            Add(table, "character.preview.pending", "Đang chuẩn bị hình nhân vật đạt chuẩn. Các lựa chọn hiện chỉ là bản nháp.");
            Add(table, "character.trait.note", "Đặc tính do bạn chọn, không phụ thuộc Nam/Nữ. Chưa ảnh hưởng gameplay.");
            Add(table, "character.cosmetic.unsaved", "Chọn thử ngoại hình; hiện chỉ tên, tuổi và Nam/Nữ được lưu.");
            Add(table, "character.face", "Khuôn mặt");
            Add(table, "character.hair", "Kiểu tóc");
            Add(table, "character.haircolor", "Màu tóc");
            Add(table, "character.skintone", "Màu da");
            Add(table, "character.outfit", "Quần áo");
            Add(table, "character.accessory", "Phụ kiện");
            Add(table, "character.trait", "Đặc tính");
            Add(table, "character.page.previous", "Quay lại");
            Add(table, "character.page.next", "Tiếp theo");
            Add(table, "character.option.previous", "−");
            Add(table, "character.option.next", "+");
            var optionAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(CreatorOptionsPath);
            if (!optionAsset) throw new InvalidOperationException("Character Creator text catalog missing.");
            var optionTexts = JsonUtility.FromJson<CreatorOptionsDocument>(optionAsset.text);
            if (optionTexts == null || optionTexts.options == null)
                throw new InvalidOperationException("Invalid Character Creator localized catalog.");
            foreach (var option in optionTexts.options)
                Add(table, option.labelKey, option.label);

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

            Add(table, "status.ready", "Sẵn sàng.");
            Add(table, "status.character.created", "Nhân vật đã sẵn sàng.");
            Add(table, "status.career.accepted", "Đã nhận việc.");
            Add(table, "status.career.resigned", "Đã nghỉ việc.");
            Add(table, "status.course.purchased", "Đã đăng ký khóa học.");
            Add(table, "status.course.progressed", "Đã học thêm.");
            Add(table, "status.course.completed", "Đã hoàn thành khóa học.");
            Add(table, "status.work.complete", "Ca làm việc đã hoàn tất.");
            Add(table, "status.day.completed", "Đã sang ngày mới.");
            Add(table, "status.business.launched", "Đã mở doanh nghiệp.");
            Add(table, "status.business.paused", "Đã tạm dừng hoạt động.");
            Add(table, "status.business.resumed", "Đã lên lịch tiếp tục hoạt động.");
            Add(table, "status.business.pricing", "Giá mới có hiệu lực từ ngày mô phỏng kế tiếp.");
            Add(table, "status.business.reinvested", "Đã tái đầu tư vào doanh nghiệp.");
            Add(table, "status.business.closed", "Đã đóng doanh nghiệp.");

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
            Add(table, "reason.save.read_failed", "Không thể đọc dữ liệu đã lưu.");
            Add(table, "reason.save.unrecoverable", "Không thể khôi phục dữ liệu đã lưu.");
            Add(table, "reason.save.content_id", "Dữ liệu lưu cần nội dung chưa có.");
            Add(table, "reason.save.content_version", "Phiên bản nội dung của dữ liệu lưu chưa được hỗ trợ.");

            Add(table, "advance.PlayerChoice", "Cần đưa ra lựa chọn trước.");
            Add(table, "advance.InvalidRequest", "Không thể chuyển ngày lúc này.");
            Add(table, "advance.InvalidState", "Trạng thái hiện tại không thể chuyển ngày.");
            Add(table, "advance.PersistenceOrRuleFailure", "Không thể hoàn tất chuyển ngày.");
            Add(table, "advance.RecoveryRequired", "Cần khôi phục dữ liệu lưu.");

            Add(table, "summary.title", "Tổng kết ngày");
            Add(table, "summary.date", "Ngày mới");
            Add(table, "summary.cash", "Tiền mặt");
            Add(table, "summary.xp", "Tổng XP nghề");
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

        static void Assign(UnityEngine.Object target, string propertyName, bool value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.Boolean)
                throw new InvalidOperationException(target.GetType().Name + "." + propertyName +
                    " must be a serialized boolean.");
            property.boolValue = value;
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
