using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace StartupLife.Presentation
{
    public sealed class LocalizedKeyLabel : MonoBehaviour
    {
        public const string Table = "FirstPlayableUI";

        [SerializeField] private LocalizeStringEvent localizer;

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
            return label;
        }

        public void SetKey(string key)
        {
            if (!localizer) throw new MissingReferenceException("LocalizedKeyLabel requires a LocalizeStringEvent.");
            localizer.StringReference.SetReference(Table, key);
            localizer.StringReference.RefreshString();
        }
    }
}
