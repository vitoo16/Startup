using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using StartupLife.Core;
using StartupLife.Application;
using System.Collections.Generic;
using System.Linq;

namespace StartupLife.Presentation
{
    public sealed class LifeScreenViewController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text nameValue;
        [SerializeField] private TMP_Text dateValue;
        [SerializeField] private TMP_Text timeValue;
        [SerializeField] private TMP_Text cashValue;
        [SerializeField] private TMP_Text lastSettledFinanceValue;
        [SerializeField] private TMP_Text businessPortfolioValue;
        [SerializeField] private TMP_Text rankValue;
        [SerializeField] private TMP_Text courseProgressValue;
        [SerializeField] private LocalizedKeyLabel careerValue;
        [SerializeField] private LocalizedKeyLabel courseNameValue;
        [SerializeField] private LocalizedKeyLabel status;
        [SerializeField] private UnityEngine.UI.Button acceptJobButton;
        [SerializeField] private UnityEngine.UI.Button workButton;
        [SerializeField] private UnityEngine.UI.Button buyCourseButton;
        [SerializeField] private UnityEngine.UI.Button studyButton;
        [SerializeField] private UnityEngine.UI.Button resignButton;

        private FirstPlayableFlow flow;
        private ContentCatalog content;
        private WorkShiftPlaybackController workPlayback;
        private DaySummaryModal daySummary;
        private StudyActionController studyActions;
        private Func<IReadOnlyList<BusinessFinanceDaySnapshot>> financeReader;
        private Func<BusinessPortfolioSnapshot> portfolioReader;
        private BusinessManagementOverlay businessOverlay;

        public void AttachBusinessReadouts(TMP_Text financeText, TMP_Text portfolioText)
        {
            lastSettledFinanceValue = financeText ?? throw new ArgumentNullException(nameof(financeText));
            businessPortfolioValue = portfolioText ?? throw new ArgumentNullException(nameof(portfolioText));
            Refresh();
        }

        public void EnsureBusinessManagementUi()
        {
            if (portfolioReader == null || financeReader == null)
                throw new InvalidOperationException("Business and finance read models must be bound before UI installation.");
            var fontSource = cashValue != null ? cashValue :
                GetComponentInChildren<TMP_Text>(true);
            businessOverlay = BusinessManagementOverlay.Install(this, portfolioReader, fontSource);
            Refresh();
        }


        public void BindBusinessPortfolio(Func<BusinessPortfolioSnapshot> readBusinessPortfolio)
        {
            portfolioReader = readBusinessPortfolio ?? throw new ArgumentNullException(nameof(readBusinessPortfolio));
            Refresh();
        }

        public void BindCommittedFinance(Func<IReadOnlyList<BusinessFinanceDaySnapshot>> readCommittedFinance)
        {
            financeReader = readCommittedFinance ?? throw new ArgumentNullException(nameof(readCommittedFinance));
            Refresh();
        }

        public void Bind(
            FirstPlayableFlow coordinator,
            ContentCatalog catalog,
            WorkShiftPlaybackController workController,
            DaySummaryModal summary)
        {
            flow = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            content = catalog ?? throw new ArgumentNullException(nameof(catalog));
            workPlayback = workController ?? throw new ArgumentNullException(nameof(workController));
            daySummary = summary ?? throw new ArgumentNullException(nameof(summary));
            studyActions = new StudyActionController(flow, Refresh, status);
            workPlayback.Bind(flow, content, Refresh, status);
            Refresh();
        }

        public void SetVisible(bool visible)
        {
            if (root) root.SetActive(visible);
            else gameObject.SetActive(visible);
        }

        public void Refresh()
        {
            if (flow == null || content == null) return;
            var snapshot = flow.Refresh();
            var culture = CultureInfo.GetCultureInfo("vi-VN");

            if (nameValue) nameValue.text = snapshot.Name;
            if (dateValue) dateValue.text = snapshot.Instant.Date.ToString();
            if (timeValue)
            {
                var hour = snapshot.Instant.Minute / 60;
                var minute = snapshot.Instant.Minute % 60;
                timeValue.text = hour.ToString("00", CultureInfo.InvariantCulture) + ":" +
                                 minute.ToString("00", CultureInfo.InvariantCulture);
            }
            if (cashValue) cashValue.text = snapshot.Cash.ToString("N0", culture) + " ₫";
            if (lastSettledFinanceValue)
            {
                var settled = financeReader?.Invoke()?.OrderByDescending(x => x.DateIso,
                    StringComparer.Ordinal).FirstOrDefault();
                lastSettledFinanceValue.text = settled == null
                    ? "Chưa có quyết toán kinh doanh"
                    : settled.DateIso + " · Doanh thu " +
                      settled.GrossRevenueVnd.ToString("N0", culture) + " ₫ · Lợi nhuận " +
                      settled.ProfitVnd.ToString("N0", culture) + " ₫";
            }
            if (businessPortfolioValue)
            {
                var businesses = portfolioReader?.Invoke()?.Businesses;
                if (businesses == null || businesses.Count == 0)
                    businessPortfolioValue.text = "Chưa có doanh nghiệp";
                else
                    businessPortfolioValue.text = string.Join("\n",
                        businesses.OrderBy(x => x.DefinitionId, StringComparer.Ordinal)
                            .ThenBy(x => x.InstanceId, StringComparer.Ordinal)
                            .Select(x => x.DefinitionId + " · " +
                                (x.IsActive ? "Đang sở hữu" : "Đã đóng") +
                                " · " + x.PricingPosture));
            }

            if (rankValue) rankValue.text = string.IsNullOrEmpty(snapshot.CareerId)
                ? "—"
                : (snapshot.Rank + 1).ToString(CultureInfo.InvariantCulture);

            careerValue?.SetKey(string.IsNullOrEmpty(snapshot.CareerId)
                ? "career.none"
                : content.Careers[snapshot.CareerId].NameKey);

            if (snapshot.ActiveCourse == null)
            {
                courseNameValue?.SetKey("course.none");
                if (courseProgressValue) courseProgressValue.text = "—";
            }
            else
            {
                courseNameValue?.SetKey(content.Courses[snapshot.ActiveCourse.DefinitionId].NameKey);
                if (courseProgressValue)
                {
                    var percent = snapshot.ActiveCourse.TargetUnits == 0
                        ? 0
                        : (int)(snapshot.ActiveCourse.ProgressUnits * 100 / snapshot.ActiveCourse.TargetUnits);
                    courseProgressValue.text = percent.ToString(CultureInfo.InvariantCulture) + "%";
                }
            }

            var employed = !string.IsNullOrEmpty(snapshot.CareerId);
            if (acceptJobButton) acceptJobButton.interactable = !employed;
            if (workButton) workButton.interactable = employed && !workPlayback.IsRunning;
            if (buyCourseButton) buyCourseButton.interactable = snapshot.ActiveCourse == null && snapshot.Arrears == 0;
            if (studyButton) studyButton.interactable = snapshot.ActiveCourse != null;
            if (resignButton) resignButton.interactable = employed;
            businessOverlay?.Refresh();
        }

        public void AcceptDeveloper()
        {
            Publish(flow.AcceptJob("developer").Command, "status.career.accepted");
        }

        public void RunWorkShift()
        {
            workPlayback.RunShift();
            Refresh();
        }

        public void PurchaseCommunicationCourse()
        {
            studyActions.PurchaseCommunicationCourse();
        }

        public void Study60Minutes()
        {
            studyActions.Study60Minutes();
        }

        public void AdvanceDay()
        {
            var result = flow.AdvanceDay().Advance;
            Refresh();
            if (result.StopReason == "TargetReached")
            {
                status?.SetKey("status.day.completed");
                daySummary.Show(flow.Refresh());
            }
            else
            {
                status?.SetKey("advance." + result.StopReason);
            }
        }

        public void Resign()
        {
            Publish(flow.Resign().Command, "status.career.resigned");
        }

        // UnityEvent bindings for optional business management UI. No implicit
        // cost or simulation step is executed when this view merely refreshes.
        // Investment amount and content revision always come from authored content.
        private void LaunchAuthoredBusiness(string definitionId)
        {
            if (content == null || !content.Businesses.TryGetValue(definitionId, out var definition))
                throw new ArgumentException("Unknown authored business.", nameof(definitionId));
            Publish(flow.LaunchBusiness(definition.Id, definition.Revision,
                checked((int)definition.MinimumStartupInvestment)).Command, "status.business.launched");
        }

        public void LaunchOnlineStore() => LaunchAuthoredBusiness("online-store");

        public void LaunchHomeFoodPreorder() => LaunchAuthoredBusiness("home-food-preorder");

        public void LaunchFreelanceService() => LaunchAuthoredBusiness("freelance-service");

        public void LaunchCoffeeKiosk() => LaunchAuthoredBusiness("coffee-kiosk");

        public void ReinvestMinimumBusiness(string instanceId)
        {
            var portfolio = portfolioReader?.Invoke() ??
                throw new InvalidOperationException("Missing authoritative portfolio reader.");
            var owned = portfolio.Businesses.Single(x => x.InstanceId == instanceId && x.IsActive);
            var definition = content.Businesses[owned.DefinitionId];
            ReinvestBusiness(instanceId, checked((int)definition.MinimumReinvestment));
        }

        public void PauseBusiness(string instanceId) => Publish(
            flow.PauseBusiness(instanceId).Command, "status.business.paused");

        public void ResumeBusiness(string instanceId) => Publish(
            flow.ResumeBusiness(instanceId).Command, "status.business.resumed");

        public void SetBusinessPricing(string instanceId, PricingPosture pricing) => Publish(
            flow.SetBusinessPricing(instanceId, pricing).Command, "status.business.pricing");

        public void ReinvestBusiness(string instanceId, int amount) => Publish(
            flow.ReinvestBusiness(instanceId, amount).Command, "status.business.reinvested");

        public void CloseBusiness(string instanceId) => Publish(
            flow.CloseBusiness(instanceId).Command, "status.business.closed");

        private void Publish(CommandResult result, string successKey)
        {
            if (result.Status == CommandStatus.Committed || result.Status == CommandStatus.AlreadyCommitted)
                status?.SetKey(successKey);
            else
                status?.SetKey("reason." + result.ReasonKey);
            Refresh();
        }
    }
}
