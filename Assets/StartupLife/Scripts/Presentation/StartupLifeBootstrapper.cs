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
        // Explicitly staged: default V3 production behavior is unchanged.
        // Enable only for a pre-existing, verified V2/V3/V4 checkpoint.
        [SerializeField] private bool enableVerifiedEventV4;
        [SerializeField] private int initialYear = 2026;
        [SerializeField] private int initialMonth = 9;
        [SerializeField] private int initialDay = 1;

        private ContentCatalog content;
        private GameSession session;
        private EventV4GameCommands eventCommands;
        private EventV4CommitGateway eventGateway;
        private FirstPlayableFlow flow;
        private FirstPlayableLifecycle lifecycle;

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
                var path = Path.Combine(UnityEngine.Application.persistentDataPath, saveFileName);
                var commandIds = new GuidCommandIdSource();
                IGameCommands gameCommands;
                if (enableVerifiedEventV4)
                {
                    var eventSerializer = new EventV4CompatibilitySerializer(
                        new StagedEventV4SaveSerializer(serializer,
                            FirstPlayableEventContent.Definitions,
                            FirstPlayableEventContent.Policies));
                    var eventStore = new AtomicFileSaveStore(path, eventSerializer);
                    var loaded = eventStore.Read();
                    if (loaded.Status != LoadStatus.Valid &&
                        loaded.Status != LoadStatus.RecoveredBackup)
                    {
                        // No speculative default seed write outside the real CAS
                        // authority. Fresh-run V4 genesis remains a separate gate.
                        Debug.LogWarning("Verified V4 opt-in requires an existing authenticated save: " +
                            loaded.Status + "; " + loaded.Reason);
                        Fail(loaded.Status == LoadStatus.Missing
                            ? "fatal.save_unavailable"
                            : string.IsNullOrEmpty(loaded.Reason)
                                ? "fatal.save_unavailable" : "reason." + loaded.Reason);
                        return;
                    }
                    var runner = new EventV4CoreCommandRunner(content, serializer,
                        economics, historical);
                    eventGateway = new EventV4CommitGateway(eventSerializer,
                        eventStore, FirstPlayableEventContent.Definitions,
                        FirstPlayableEventContent.Policies, runner,
                        automaticDailyChecks: true);
                    eventCommands = new EventV4GameCommands(eventGateway, content, economics);
                    gameCommands = eventCommands;
                }
                else
                {
                    var store = new AtomicFileSaveStore(path, serializer);
                    if (!GameSession.TryRestore(content, serializer, store,
                        out session, out var load, economicRules: economics,
                        archivedRules: historical))
                    {
                        if (load.Status != LoadStatus.Missing)
                        {
                            Fail(string.IsNullOrEmpty(load.Reason)
                                ? "fatal.save_unavailable" : "reason." + load.Reason);
                            return;
                        }

                        var guid = Guid.NewGuid();
                        var bytes = guid.ToByteArray();
                        var seed = BitConverter.ToUInt64(bytes, 0);
                        if (seed == 0) seed = 1;
                        var runId = "run-" + guid.ToString("N");
                        var initial = GameSession.NewState(
                            content, runId, seed,
                            new SimDate(initialYear, initialMonth, initialDay));
                        // New runs still originate from immutable V2; the migration
                        // is zero-economic-effect and writes no checkpoint.
                        var promoted = EconomicV3PayloadCodec.ReadChecked(
                            migration.Migrate(JsonSaveSerializer.WriteObject(initial)));
                        promoted.Current!.EconomicV3 = promoted;
                        session = new GameSession(promoted.Current, content, serializer, store,
                            economicRules: economics, archivedRules: historical);
                    }
                    gameCommands = session;
                }

                flow = new FirstPlayableFlow(gameCommands, commandIds);
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
