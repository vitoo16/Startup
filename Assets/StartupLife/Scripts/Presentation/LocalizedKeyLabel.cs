using UnityEngine;
using UnityEngine.Localization.Components;

namespace StartupLife.Presentation
{
    public sealed class LocalizedKeyLabel : MonoBehaviour
    {
        public const string Table = "FirstPlayableUI";

        [SerializeField] private LocalizeStringEvent localizer;

        public void SetKey(string key)
        {
            if (!localizer) throw new MissingReferenceException("LocalizedKeyLabel requires a LocalizeStringEvent.");
            localizer.StringReference.SetReference(Table, key);
            localizer.StringReference.RefreshString();
        }
    }
}
