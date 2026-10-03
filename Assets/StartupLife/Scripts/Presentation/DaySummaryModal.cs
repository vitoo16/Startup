using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace StartupLife.Presentation
{
    public sealed class DaySummaryModal : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text dateValue;
        [SerializeField, FormerlySerializedAs("cashDeltaValue")] private TMP_Text cashValue;
        [SerializeField] private TMP_Text careerXpValue;

        public void Hide()
        {
            if (root) root.SetActive(false);
            else gameObject.SetActive(false);
        }

        public void Show(FirstPlayableState snapshot)
        {
            if (dateValue) dateValue.text = snapshot.Instant.Date.ToString();
            if (cashValue) cashValue.text = snapshot.Cash.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
            if (careerXpValue) careerXpValue.text = snapshot.CareerXp.ToString(CultureInfo.InvariantCulture);
            if (root) root.SetActive(true);
            else gameObject.SetActive(true);
        }
    }
}
