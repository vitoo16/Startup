using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using StartupLife.Presentation;

namespace StartupLife.Editor
{
    public static class MobileBaselineBuilder
    {
        const string Root = "Assets/StartupLife";
        public const string ScenePath = Root + "/Scenes/MobileBaseline.unity";
        const string GlyphSample = "ă â đ ê ô ơ ư Á À Ả Ã Ạ ấ ề ộ ớ ự Nguyễn Phương Khởi nghiệp Thành phố Hồ Chí Minh";

        [MenuItem("Startup Life/Foundation/Create mobile baseline")]
        public static void Create()
        {
            if (TMP_Settings.instance) { Build(); return; }
            double deadline = EditorApplication.timeSinceStartup + 120;
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            EditorApplication.CallbackFunction poll = null;
            poll = () =>
            {
                if (!TMP_Settings.instance && EditorApplication.timeSinceStartup < deadline) return;
                EditorApplication.update -= poll;
                if (!TMP_Settings.instance) { Debug.LogError("TMP resource import timed out."); return; }
                Build();
            };
            EditorApplication.update += poll;
        }

        static void Build()
        {
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Baseline exists. Review it before replacing it.");
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>("Assets/Settings/Renderer2D.asset");
            if (!pipeline || !renderer) throw new InvalidOperationException("The generated Universal 2D template assets are required.");
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            Directory.CreateDirectory(Root + "/Scenes");
            Directory.CreateDirectory(Root + "/Data");
            Directory.CreateDirectory(Root + "/Localization");
            AssetDatabase.Refresh();
            EditorSettings.serializationMode = SerializationMode.ForceText;
            PlayerSettings.companyName = "Startup Life";
            PlayerSettings.productName = "Startup Life";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
            Application.targetFrameRate = 60;

            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(Root + "/UI/Fonts/NotoSans-Regular.ttf");
            if (!sourceFont) throw new InvalidOperationException("Licensed Noto Sans source is missing.");
            var font = TMP_FontAsset.CreateFontAsset(sourceFont, 64, 8,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic);
            font.name = "NotoSans Vietnamese SDF";
            AssetDatabase.CreateAsset(font, Root + "/UI/Fonts/NotoSansVietnamese.asset");
            foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
            AssetDatabase.AddObjectToAsset(font.material, font);
            if (!font.TryAddCharacters(GlyphSample, out string missing))
                throw new InvalidOperationException("Missing Vietnamese glyphs: " + missing);
            EditorUtility.SetDirty(font);
            var theme = ScriptableObject.CreateInstance<MobileTheme>();
            AssetDatabase.CreateAsset(theme, Root + "/Data/MobileTheme.asset");

            var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (!settings)
            {
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                AssetDatabase.CreateAsset(settings, Root + "/Localization/LocalizationSettings.asset");
                LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }
            var locale = LocalizationEditorSettings.GetLocales().FirstOrDefault(l => l.Identifier.Code == "vi");
            if (!locale)
            {
                locale = Locale.CreateLocale("vi");
                AssetDatabase.CreateAsset(locale, Root + "/Localization/Vietnamese.asset");
                LocalizationEditorSettings.AddLocale(locale);
            }
            LocalizationSettings.ProjectLocale = locale;
            LocalizationSettings.StartupLocaleSelectors.Clear();
            LocalizationSettings.StartupLocaleSelectors.Add(new SpecificLocaleSelector { LocaleId = locale.Identifier });
            var collection = LocalizationEditorSettings.CreateStringTableCollection("FoundationUI", Root + "/Localization");
            var table = (StringTable)collection.GetTable(locale.Identifier);
            table.AddEntry("foundation.title", "STARTUP LIFE");
            table.AddEntry("foundation.subtitle", "Một khởi đầu, nhiều lựa chọn");
            table.AddEntry("foundation.district", "Khu phố An Bình · Thành phố hư cấu");
            table.AddEntry("foundation.header", "Hôm nay của bạn");
            table.AddEntry("foundation.description", "Đi làm, học thêm, dành thời gian cho ý tưởng của mình.");
            table.AddEntry("foundation.glyphs", GlyphSample);
            table.AddEntry("foundation.status", "Bản kiểm tra giao diện · Chưa có vòng chơi");
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(collection);
            EditorUtility.SetDirty(settings);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("MainCamera", typeof(Camera)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.backgroundColor = theme.background;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            var light = new GameObject("GlobalLight2D", typeof(Light2D)).GetComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1;
            var canvas = new GameObject("MobileCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler),
                typeof(UnityEngine.UI.GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            var safeArea = new GameObject("SafeArea", typeof(RectTransform), typeof(MobileSafeArea));
            safeArea.transform.SetParent(canvas.transform, false);
            var area = safeArea.GetComponent<RectTransform>();
            area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
            area.offsetMin = area.offsetMax = Vector2.zero;
            var layout = safeArea.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(theme.padding, theme.padding, theme.padding, theme.padding);
            layout.spacing = theme.gap;
            layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            Label("Title", "foundation.title", theme.title, safeArea.transform, font, theme.ink, 120);
            Label("Subtitle", "foundation.subtitle", theme.heading, safeArea.transform, font, theme.accent, 110);
            Label("District", "foundation.district", theme.caption, safeArea.transform, font, theme.ink, 100);
            Label("Today", "foundation.header", theme.heading, safeArea.transform, font, theme.ink, 110);
            Label("Description", "foundation.description", theme.body, safeArea.transform, font, theme.ink, 190);
            Label("GlyphSample", "foundation.glyphs", theme.body, safeArea.transform, font, theme.ink, 260);
            Label("Status", "foundation.status", theme.caption, safeArea.transform, font, theme.accent, 120);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[StartupLifeBaseline] Created portrait uGUI/TMP/Vietnamese baseline.");
        }

        static void Label(string name, string key, float size, Transform parent, TMP_FontAsset font, Color color, float height)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(UnityEngine.UI.LayoutElement));
            obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.color = color; text.raycastTarget = false;
            obj.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = height;
            var localized = obj.AddComponent<LocalizeStringEvent>();
            localized.StringReference.SetReference("FoundationUI", key);
            UnityEventTools.AddPersistentListener(localized.OnUpdateString, text.SetText);
        }
    }
}
