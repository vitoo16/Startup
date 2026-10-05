using System;
using System.IO;
using StartupLife.Application;
using StartupLife.Content;
using StartupLife.Core;
using StartupLife.Infrastructure;
using UnityEngine;

namespace StartupLife.Presentation
{
    public sealed class StartupLifeBootstrapper : MonoBehaviour
    {
        [SerializeField] private StartupLifeContentCatalogAsset contentAsset;
        [SerializeField] private CharacterCreationViewController characterCreation;
        [SerializeField] private LifeScreenViewController lifeScreen;
        [SerializeField] private WorkShiftPlaybackController workPlayback;
        [SerializeField] private DaySummaryModal daySummary;
        [SerializeField] private LocalizedKeyLabel fatalStatus;
        [SerializeField] private string saveFileName = "startup-life.json";
        [SerializeField] private int initialYear = 2026;
        [SerializeField] private int initialMonth = 9;
        [SerializeField] private int initialDay = 1;

        private ContentCatalog content;
        private GameSession session;
        private FirstPlayableFlow flow;
        private FirstPlayableLifecycle lifecycle;

        public bool IsReady { get; private set; }
        public FirstPlayableFlow Flow => flow;
        public ContentCatalog Catalog => content;
        public FirstPlayableState Snapshot => flow?.Refresh();

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (IsReady) return;
            if (!contentAsset)
            {
                Fail("fatal.content_missing");
                return;
            }

            try
            {
                content = contentAsset.BuildCatalog();
                var serializer = new JsonSaveSerializer(
                    content,
                    GameSession.CreateRestoreValidator(),
                    new SyntheticV0Migration(),
                    new V1ToV2Migration());
                var path = Path.Combine(UnityEngine.Application.persistentDataPath, saveFileName);
                var store = new AtomicFileSaveStore(path, serializer);

                if (!GameSession.TryRestore(content, serializer, store, out session, out var load))
                {
                    if (load.Status != LoadStatus.Missing)
                    {
                        Fail(string.IsNullOrEmpty(load.Reason) ? "fatal.save_unavailable" : "reason." + load.Reason);
                        return;
                    }

                    var guid = Guid.NewGuid();
                    var bytes = guid.ToByteArray();
                    var seed = BitConverter.ToUInt64(bytes, 0);
                    if (seed == 0) seed = 1;
                    var runId = "run-" + guid.ToString("N");
                    var initial = GameSession.NewState(
                        content,
                        runId,
                        seed,
                        new SimDate(initialYear, initialMonth, initialDay));
                    session = new GameSession(initial, content, serializer, store);
                }

                flow = new FirstPlayableFlow(session, new GuidCommandIdSource());
                characterCreation.Bind(flow, RefreshMode);
                lifeScreen.Bind(flow, content, workPlayback, daySummary);
                lifecycle = new FirstPlayableLifecycle(flow, workPlayback, RefreshMode);
                daySummary.Hide();
                IsReady = true;
                RefreshMode();
                lifecycle.RestoreAfterBootstrap();
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Fail("fatal.bootstrap_failed");
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            HandleApplicationPause(pauseStatus);
        }

        private void OnApplicationQuit()
        {
            if (IsReady) lifecycle?.Quit();
        }

        public void HandleApplicationPause(bool pauseStatus)
        {
            if (!IsReady || lifecycle == null) return;
            lifecycle.SetPaused(pauseStatus);
        }

        public void RefreshMode()
        {
            if (flow == null) return;
            var hasCharacter = !string.IsNullOrEmpty(flow.Refresh().Name);
            characterCreation?.SetVisible(!hasCharacter);
            lifeScreen?.SetVisible(hasCharacter);
            if (hasCharacter) lifeScreen?.Refresh();
        }

        private void Fail(string key)
        {
            IsReady = false;
            characterCreation?.SetVisible(false);
            lifeScreen?.SetVisible(false);
            daySummary?.Hide();
            fatalStatus?.SetKey(key);
        }
    }
}
