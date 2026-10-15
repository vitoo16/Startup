using System;
using System.IO;
using System.Linq;
using StartupLife.Content;
using StartupLife.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace StartupLife.Editor
{
    /// <summary>Runs existing builders safely and verifies their serialized result after reopening.</summary>
    public static class FirstPlayableRuntimeGate
    {
        public static void BuildAndVerify()
        {
            if (!File.Exists(MobileBaselineBuilder.ScenePath)) MobileBaselineBuilder.Create();
            Require(TMP_Settings.instance, "TMP Essential Resources must be imported before the batch builder.");
            FirstPlayableContentBuilder.CreateOrUpdate();
            if (!File.Exists(FirstPlayableShellBuilder.ScenePath)) FirstPlayableShellBuilder.Create();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            // Existing scenes predate multi-page Character Creator. The repair
            // routine is idempotent and verifies the reopened serialized scene.
            FirstPlayableShellBuilder.RepairGeneratedAssetReferences();
            VerifyScene();
            Debug.Log("[StartupLifeM7] Repaired AND reopened/verified first playable Character Creator.");
        }

        public static void VerifyScene()
        {
            var scene = EditorSceneManager.OpenScene(FirstPlayableShellBuilder.ScenePath, OpenSceneMode.Single);
            var objects = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(transform => transform.gameObject).ToArray();
            Require(objects.All(obj => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj) == 0), "Missing script in generated scene.");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/StartupLife/UI/Fonts/NotoSansVietnamese.asset");
            Require(objects.Select(obj => obj.GetComponent<TMP_Text>()).Where(value => value).All(text => text.font == font), "Generated text font reference is missing.");
            var bootstrap = objects.Select(obj => obj.GetComponent<StartupLifeBootstrapper>()).SingleOrDefault(value => value);
            Require(bootstrap, "Composition root missing.");
            CheckReferences(bootstrap, "contentAsset", "characterCreation", "lifeScreen", "workPlayback", "daySummary", "fatalStatus");
            var authored = new SerializedObject(bootstrap).FindProperty("contentAsset").objectReferenceValue;
            Require(authored == AssetDatabase.LoadAssetAtPath<StartupLifeContentCatalogAsset>(FirstPlayableContentBuilder.AssetPath), "Wrong Content asset assigned.");
            var creator = objects.Select(obj => obj.GetComponent<CharacterCreationViewController>())
                .Single(value => value);
            CheckReferences(creator, "root", "nameInput", "ageValue", "appearanceValue",
                "status", "draftCatalog", "identityPage", "lookPage", "wardrobePage",
                "traitsPage", "creatorScroll", "creatorBackButton", "creatorNextButton",
                "faceValue", "hairValue", "hairColorValue", "skinToneValue",
                "accessoryValue", "outfitValue", "traitValue");
            var draft = new SerializedObject(creator).FindProperty("draftCatalog").objectReferenceValue;
            Require(draft == AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/StartupLife/Data/CharacterCreatorDraftCatalog.json"),
                "Character Creator must use the canonical draft option catalog.");
            var creatorPanel = objects.Single(obj => obj.name == "CharacterCreationPanel").transform;
            var touchSizer = creatorPanel.GetComponent<CharacterCreatorTouchSizer>();
            Require(touchSizer && new SerializedObject(touchSizer)
                .FindProperty("uiTheme").objectReferenceValue ==
                AssetDatabase.LoadAssetAtPath<MobileTheme>("Assets/StartupLife/Data/MobileTheme.asset"),
                "Character Creator touch policy must use the serialized pixel theme.");
            var keyboard = creatorPanel.GetComponent<CharacterCreatorKeyboardAvoidance>();
            Require(keyboard, "Creator needs keyboard inset handler.");
            CheckReferences(keyboard, "nameInput", "rootCanvas");
            var textScaler = creatorPanel.GetComponent<CharacterCreatorTextScaler>();
            Require(textScaler, "Missing responsive text-size controller.");
            CheckReferences(textScaler, "touchSizer", "scaleValue");
            var textSizeRow = objects.Single(obj => obj.name == "CreatorTextSizeRow");
            Require(textSizeRow.transform.IsChildOf(creatorPanel) &&
                textSizeRow.transform.parent.name == "CreatorIdentityPage",
                "Font-size control must remain inside scrollable creator identity page.");
            foreach (var label in new[] {
                "CreatorTextSmallerButton", "CreatorTextLargerButton"
            })
            {
                var button = objects.Single(obj => obj.name == label)
                    .GetComponent<UnityEngine.UI.Button>();
                Require(button && button.onClick.GetPersistentEventCount() == 1 &&
                    button.onClick.GetPersistentTarget(0) == textScaler,
                    "Text size control lost serialized callback: " + label);
            }
            var creatorScroll = objects.Single(obj => obj.name == "CharacterCreatorScroll")
                .GetComponent<UnityEngine.UI.ScrollRect>();
            Require(creatorScroll && creatorScroll.vertical && !creatorScroll.horizontal &&
                creatorScroll.content && creatorScroll.viewport &&
                creatorScroll.transform.parent == creatorPanel,
                "Character Creator requires a valid vertical scroll inside the panel.");
            var paging = objects.Single(obj => obj.name == "CreatorPageNavigation");
            var submit = objects.Single(obj => obj.name == "CreateCharacterButton");
            Require(paging.transform.parent == creatorPanel &&
                submit.transform.parent == creatorPanel &&
                !paging.transform.IsChildOf(creatorScroll.content) &&
                !submit.transform.IsChildOf(creatorScroll.content),
                "Navigation/submit buttons must remain outside the scroll viewport.");
            Require(objects.Single(obj => obj.name == "CreatorIdentityPage").activeSelf &&
                !objects.Single(obj => obj.name == "CreatorLookPage").activeSelf &&
                !objects.Single(obj => obj.name == "CreatorWardrobePage").activeSelf &&
                !objects.Single(obj => obj.name == "CreatorTraitsPage").activeSelf,
                "Identity must be the only initially active creator page.");
            foreach (var name in new[] {
                "CreatorPreviousPageButton", "CreatorNextPageButton",
                "CreatorFacePreviousButton", "CreatorFaceNextButton",
                "CreatorHairPreviousButton", "CreatorHairNextButton",
                "CreatorHairColorPreviousButton", "CreatorHairColorNextButton",
                "CreatorSkinTonePreviousButton", "CreatorSkinToneNextButton",
                "CreatorAccessoryPreviousButton", "CreatorAccessoryNextButton",
                "CreatorOutfitPreviousButton", "CreatorOutfitNextButton",
                "CreatorTraitPreviousButton", "CreatorTraitNextButton"
            })
            {
                var button = objects.Single(obj => obj.name == name)
                    .GetComponent<UnityEngine.UI.Button>();
                Require(button && button.onClick.GetPersistentEventCount() == 1 &&
                    button.onClick.GetPersistentTarget(0) == creator,
                    name + " missing exactly one creator action.");
            }
            CheckReferences(objects.Select(obj => obj.GetComponent<LifeScreenViewController>()).Single(value => value),
                "root", "nameValue", "dateValue", "timeValue", "cashValue", "rankValue", "courseProgressValue", "careerValue", "courseNameValue", "status",
                "acceptJobButton", "workButton", "buyCourseButton", "studyButton", "resignButton");
            CheckReferences(objects.Select(obj => obj.GetComponent<DaySummaryModal>()).Single(value => value), "root", "dateValue", "cashValue", "careerXpValue");
            var canvas = objects.Select(obj => obj.GetComponent<Canvas>()).Single(value => value);
            Require(canvas.GetComponent<UnityEngine.UI.CanvasScaler>().referenceResolution == new Vector2(1080, 1920), "Canvas must be portrait 1080x1920.");
            Require(objects.Any(obj => obj.GetComponent<MobileSafeArea>()), "Safe area missing.");
            var eventSystem = objects.Select(obj => obj.GetComponent<EventSystem>()).Single(value => value);
            Require(eventSystem.GetComponent<InputSystemUIInputModule>(), "Input System UI module missing.");
            Require(objects.Any(obj => obj.GetComponent<Camera>() && obj.CompareTag("MainCamera")), "Main Camera missing.");
            foreach (var name in new[] { "CreateCharacterButton", "AcceptDeveloperButton", "WorkButton", "BuyCourseButton", "Study60Button", "AdvanceDayButton", "ResignButton", "CloseSummaryButton" })
            {
                var button = objects.Single(obj => obj.name == name).GetComponent<UnityEngine.UI.Button>();
                Require(button && button.onClick.GetPersistentEventCount() == 1 && button.onClick.GetPersistentTarget(0), name + " binding missing.");
            }
            Require(objects.Single(obj => obj.name == "CharacterCreationPanel").activeSelf, "Fresh entry panel should be visible.");
            Require(!objects.Single(obj => obj.name == "LifePanel").activeSelf, "Life panel should be hidden before bootstrap.");
            Require(!objects.Single(obj => obj.name == "DaySummaryModal").activeSelf, "Day summary should initially be hidden.");
        }

        private static void CheckReferences(UnityEngine.Object target, params string[] names)
        {
            var serialized = new SerializedObject(target);
            foreach (var name in names)
            {
                var property = serialized.FindProperty(name);
                Require(property != null && property.objectReferenceValue, target.GetType().Name + "." + name + " is not serialized.");
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
