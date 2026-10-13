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

        internal static LocalizedKeyLabel EnsureFor(TMP_Text text)
        {
            if (!text) throw new MissingReferenceException("LocalizedKeyLabel requires a TMP_Text target.");

            var localizer = text.GetComponent<LocalizeStringEvent>();
            var label = text.GetComponent<LocalizedKeyLabel>();
            if (!localizer)
            {
                localizer = text.gameObject.AddComponent<LocalizeStringEvent>();
                localizer.OnUpdateString.AddListener(value => text.SetText(value));
            }

            if (!label) label = text.gameObject.AddComponent<LocalizedKeyLabel>();
            label.localizer = localizer;
            label.target = text;
            return label;
        }

        public void SetKey(string key)
        {
            if (!localizer) throw new MissingReferenceException("LocalizedKeyLabel requires a LocalizeStringEvent.");
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
            localizer.enabled = false;
            target.SetText(value);
        }
    }
}
