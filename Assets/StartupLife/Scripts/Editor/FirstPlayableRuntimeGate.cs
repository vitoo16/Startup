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
            VerifyScene();
            Debug.Log("[StartupLifeM7] Builders and reopened serialized scene verification PASS.");
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
            CheckReferences(objects.Select(obj => obj.GetComponent<CharacterCreationViewController>()).Single(value => value),
                "root", "nameInput", "ageValue", "appearanceValue", "status");
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
