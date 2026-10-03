using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using StartupLife.Core;

namespace StartupLife.Presentation
{
    public sealed class DaySummaryModal : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text dateValue;
        [SerializeField] private TMP_Text cashDeltaValue;
        [SerializeField] private TMP_Text careerXpValue;

        public void Hide()
        {
            if (root) root.SetActive(false);
            else gameObject.SetActive(false);
        }

        public void Show(AdvanceResult result, GameSnapshot snapshot)
        {
            var cashDelta = result.Boundaries.Where(x => x.Outcome != null).Sum(x => x.Outcome!.CashDelta);
            var careerXp = result.Boundaries.Where(x => x.Outcome != null).Sum(x => x.Outcome!.CareerXpDelta);
            if (dateValue) dateValue.text = snapshot.Instant.Date.ToString();
            if (cashDeltaValue) cashDeltaValue.text = cashDelta.ToString("+#,0;-#,0;0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
            if (careerXpValue) careerXpValue.text = careerXp.ToString("+#,0;-#,0;0", CultureInfo.InvariantCulture);
            if (root) root.SetActive(true);
            else gameObject.SetActive(true);
        }
    }
}
