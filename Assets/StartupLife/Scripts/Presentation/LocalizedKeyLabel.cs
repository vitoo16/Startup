using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace StartupLife.Presentation
{
    public sealed class LocalizedKeyLabel : MonoBehaviour
    {
        public const string Table = "FirstPlayableUI";

        [SerializeField] private LocalizeStringEvent localizer;
        private TMP_Text target;
        // Localization StringTables can resolve asynchronously. A queued
        // result must never overwrite the published copy of a historical V4
        // ticket after its source has switched to immutable release text.
        private bool usesFrozenVietnamese;

        internal static LocalizedKeyLabel EnsureFor(TMP_Text text)
        {
            if (!text) throw new MissingReferenceException("LocalizedKeyLabel requires a TMP_Text target.");

            var localizer = text.GetComponent<LocalizeStringEvent>();
            var label = text.GetComponent<LocalizedKeyLabel>();
            var createdLocalizer = !localizer;
            if (createdLocalizer)
                localizer = text.gameObject.AddComponent<LocalizeStringEvent>();

            if (!label) label = text.gameObject.AddComponent<LocalizedKeyLabel>();
            label.localizer = localizer;
            label.target = text;
            // Installed exactly once for programmatically generated labels;
            // repeated EnsureFor calls never stack callback subscriptions.
            if (createdLocalizer)
                localizer.OnUpdateString.AddListener(label.ApplyLocalizedValue);
            return label;
        }

        private void ApplyLocalizedValue(string value)
        {
            if (!usesFrozenVietnamese && target)
                target.SetText(value);
        }

        public void SetKey(string key)
        {
            if (!localizer) throw new MissingReferenceException("LocalizedKeyLabel requires a LocalizeStringEvent.");
            // Clear the frozen mode before enabling Unity's updater again.
            usesFrozenVietnamese = false;
            localizer.StringReference.SetReference(Table, key);
            if (!localizer.enabled) localizer.enabled = true;
            localizer.StringReference.RefreshString();
        }

        // Use only for a verified, immutable historical release snapshot.
        // Disabling Unity's asynchronous updater prevents current StringTables
        // from overwriting the originally published Vietnamese story text.
        public void SetFrozenVietnamese(string value)
        {
            if (!localizer || !target || string.IsNullOrWhiteSpace(value))
                throw new MissingReferenceException("Frozen event text requires a localized TMP label.");
            usesFrozenVietnamese = true;
            localizer.enabled = false;
            target.SetText(value);
        }
    }
}
