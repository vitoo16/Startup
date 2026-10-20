using System;
using System.IO;
using StartupLife.Application;
using StartupLife.Content;
using StartupLife.Core;
using StartupLife.Infrastructure;
using StartupLife.Integration;
using StartupLife.Simulation;
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
        // The first-playable scene runs verified V4 events by default.
        // Existing V2/V3 checkpoints are promoted only through the verified
        // V4 gateway/CAS path; never rewrite them during a read.
        // Inspector opt-out is retained for targeted V3 compatibility QA.
        [SerializeField] private bool enableVerifiedEventV4 = true;
        [SerializeField] private int initialYear = 2026;
        [SerializeField] private int initialMonth = 9;
        [SerializeField] private int initialDay = 1;

        private ContentCatalog content;
        private GameSession session;
        private EventV4GameCommands eventCommands;
        private EventV4CommitGateway eventGateway;
        private FirstPlayableFlow flow;
        private FirstPlayableLifecycle lifecycle;
        private FirstPlayableMainMenu mainMenu;

        private string SavePath => Path.Combine(UnityEngine.Application.persistentDataPath, saveFileName);

        public bool IsReady { get; private set; }
        public FirstPlayableFlow Flow => flow;
        public ContentCatalog Catalog => content;
        public FirstPlayableState Snapshot => flow?.Refresh();
        public BusinessPortfolioSnapshot BusinessPortfolio =>
            eventCommands != null ? eventCommands.ReadBusinesses() : session?.ReadBusinesses();

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
                var historical = new BundledHistoricalV2ContentResolver();
                var economics = M9T02FunctionalEconomy.Create();
                var replay = new VersionedReceiptReplay(economics, content);
                var migration = new V2ToV3Migration(historical, replay);
                var staged = new StagedV3SaveSerializer(historical, replay);
                var serializer = new V3SaveCompatibilitySerializer(
                    content, GameSession.CreateRestoreValidator(), migration, staged);
                var path = SavePath;
                var commandIds = new GuidCommandIdSource();
                if (enableVerifiedEventV4)
                {
                    // Current authored roster is separate from the frozen V4
                    // release baseline needed by pre-fingerprint checkpoints.
                    var eventArchive = ReleasedFirstPlayableEventCatalog.CreateArchive(
                        FirstPlayableEventContent.Definitions,
                        FirstPlayableEventContent.Policies);
                    var eventSerializer = new EventV4CompatibilitySerializer(
                        new StagedEventV4SaveSerializer(serializer, eventArchive));
                    var eventStore = new AtomicFileSaveStore(path, eventSerializer);
                    var loaded = eventStore.Read();
#if UNITY_EDITOR
                    LogEditorSaveRead(path, loaded);
#endif
                    GameState initialV4 = null;
                    if (loaded.Status == LoadStatus.Missing)
                    {
                        // Migration of a freshly generated V2 run is read-only.
                        // The first V3 CreateCharacter receipt will be the sole
                        // persisted schema4 genesis, through the regular CAS.
                        var stagedV4 = eventSerializer.DeserializeAndValidate(
                            JsonSaveSerializer.Wrap(
                                JsonSaveSerializer.WriteObject(CreateInitialState()),
                                SaveSchema.HistoricalV2, 0));
                        if (stagedV4.Status != LoadStatus.Valid ||
                            stagedV4.State?.Revision != 0)
                        {
                            Fail("fatal.save_unavailable");
                            return;
                        }
                        initialV4 = stagedV4.State;
                    }
                    else if (loaded.Status != LoadStatus.Valid &&
                        loaded.Status != LoadStatus.RecoveredBackup)
                    {
                        Fail(string.IsNullOrEmpty(loaded.Reason)
                            ? "fatal.save_unavailable" : "reason." + loaded.Reason);
                        return;
                    }
                    var runner = new EventV4CoreCommandRunner(content, serializer,
                        economics, historical);
                    eventGateway = new EventV4CommitGateway(eventSerializer,
                        eventStore, FirstPlayableEventContent.Definitions,
                        FirstPlayableEventContent.Policies, runner,
                        automaticDailyChecks: true, initialState: initialV4);
                    eventCommands = new EventV4GameCommands(eventGateway, content, economics);
                    flow = new FirstPlayableFlow(eventCommands, commandIds);
                }
                else
                {
                    var store = new AtomicFileSaveStore(path, serializer);
                    var restored = GameSession.TryRestore(content, serializer, store,
                        out session, out var load, economicRules: economics,
                        archivedRules: historical);
#if UNITY_EDITOR
                    LogEditorSaveRead(path, load);
#endif
                    if (!restored)
                    {
                        if (load.Status != LoadStatus.Missing)
                        {
                            Fail(string.IsNullOrEmpty(load.Reason)
                                ? "fatal.save_unavailable" : "reason." + load.Reason);
                            return;
                        }

                        var initial = CreateInitialState();
                        // New runs still originate from immutable V2; the migration
                        // is zero-economic-effect and writes no checkpoint.
                        var promoted = EconomicV3PayloadCodec.ReadChecked(
                            migration.Migrate(JsonSaveSerializer.WriteObject(initial)));
                        promoted.Current!.EconomicV3 = promoted;
                        session = new GameSession(promoted.Current, content, serializer, store,
                            economicRules: economics, archivedRules: historical);
                    }
                    // Frozen M7 production contract stays unchanged.
                    flow = new FirstPlayableFlow(session, new GuidCommandIdSource());
                }

                characterCreation.Bind(flow, RefreshMode);
                lifeScreen.Bind(flow, content, workPlayback, daySummary);
                if (eventCommands != null)
                {
                    lifeScreen.BindCommittedFinance(() => eventCommands.ReadCommittedFinance());
                    lifeScreen.BindBusinessPortfolio(() => eventCommands.ReadBusinesses());
                    lifeScreen.BindBusinessOperations(() => eventCommands.ReadBusinessOperations());
                    lifeScreen.BindLifeDashboard(() => eventCommands.ReadLifeDashboard());
                }
                else
                {
                    lifeScreen.BindCommittedFinance(() => session.ReadCommittedFinance());
                    lifeScreen.BindBusinessPortfolio(() => session.ReadBusinesses());
                    lifeScreen.BindBusinessOperations(() => session.ReadBusinessOperations());
                    lifeScreen.BindLifeDashboard(() => session.ReadLifeDashboard());
                }
                lifeScreen.EnsureBusinessManagementUi();
                lifeScreen.EnsureLifeDashboardUi();
                if (eventGateway != null)
                    lifeScreen.BindEventChoices(eventGateway, commandIds);
                lifecycle = new FirstPlayableLifecycle(flow, workPlayback, RefreshMode);
                daySummary.Hide();
                IsReady = true;
                RefreshMode();
                lifecycle.RestoreAfterBootstrap();
                var menuFont = lifeScreen.GetComponentInChildren<TMPro.TMP_Text>(true);
                mainMenu = FirstPlayableMainMenu.Install(this, lifeScreen.transform.parent,
                    lifeScreen.UiTheme, menuFont ? menuFont.font : null);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Fail("fatal.bootstrap_failed");
            }
        }

        // Shared run creation across V3 and V4. No file is written here.
#if UNITY_EDITOR
        // Development-only evidence of the actual physical save read. Do not
        // log character names or checkpoint payloads; this is not a save writer.
        private static void LogEditorSaveRead(string path, LoadResult read)
        {
            Debug.Log("[StartupLife][SaveBoot] path=" + path +
                " status=" + read.Status +
                " primaryExists=" + File.Exists(path) +
                " backupExists=" + File.Exists(path + ".backup") +
                " revision=" + (read.State?.Revision.ToString() ?? "none") +
                " characterPresent=" + (read.State != null &&
                    !string.IsNullOrEmpty(read.State.Name)));
        }
#endif

        private GameState CreateInitialState()
        {
            var guid = Guid.NewGuid();
            var seed = BitConverter.ToUInt64(guid.ToByteArray(), 0);
            if (seed == 0) seed = 1;
            return GameSession.NewState(content, "run-" + guid.ToString("N"),
                seed, new SimDate(initialYear, initialMonth, initialDay));
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

        public bool TryArchiveAndStartNewGame(out string reason)
        {
            reason = string.Empty;
            if (!IsReady || lifecycle == null || flow == null || workPlayback.IsRunning)
            {
                reason = "Game is not ready or a work shift is in progress.";
                return false;
            }
            if (string.IsNullOrEmpty(flow.Refresh().Name))
            {
                // A brand-new draft does not own a committed character save.
                reason = "A new character is already being created.";
                return false;
            }
            lifecycle.SetPaused(true);
            try
            {
                // Archive both the primary and its recovery backup under the
                // same cross-process save lock. No new-run state is written here.
                var archive = LocalCheckpointArchiver.Archive(SavePath);
                if (archive.Length == 0)
                {
                    reason = "Expected committed checkpoint was not present. Restart the scene to inspect the save.";
                    Fail("fatal.save_unavailable");
                    return false;
                }
                Debug.Log("[StartupLife][Menu] Existing run archived at " + archive);
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                UnityEngine.SceneManagement.SceneManager.LoadScene(scene.name,
                    UnityEngine.SceneManagement.LoadSceneMode.Single);
                return true;
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                // An interrupted archive or a failed scene reload must not
                // resume the old in-memory session and recreate a checkpoint
                // in the path reserved for the next run. Fail closed instead.
                Fail("fatal.save_unavailable");
                reason = "Unable to archive and reload safely. Restart the scene and inspect RunArchives.";
                return false;
            }
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
