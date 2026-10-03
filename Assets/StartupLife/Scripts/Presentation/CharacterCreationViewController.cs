using System;
using TMPro;
using UnityEngine;
using StartupLife.Core;

namespace StartupLife.Presentation
{
    public sealed class CharacterCreationViewController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_Text ageValue;
        [SerializeField] private TMP_Text appearanceValue;
        [SerializeField] private LocalizedKeyLabel status;

        private FirstPlayableFlowCoordinator flow;
        private Action onCreated;
        private int age = 25;
        private string appearanceId = "base.female";

        public void Bind(FirstPlayableFlowCoordinator coordinator, Action created)
        {
            flow = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            onCreated = created ?? throw new ArgumentNullException(nameof(created));
            RenderSelection();
        }

        public void SetVisible(bool visible)
        {
            if (root) root.SetActive(visible);
            else gameObject.SetActive(visible);
        }

        public void DecreaseAge()
        {
            age = Mathf.Max(18, age - 1);
            RenderSelection();
        }

        public void IncreaseAge()
        {
            age = Mathf.Min(40, age + 1);
            RenderSelection();
        }

        public void SelectFemale()
        {
            appearanceId = "base.female";
            RenderSelection();
        }

        public void SelectMale()
        {
            appearanceId = "base.male";
            RenderSelection();
        }

        public void Submit()
        {
            if (flow == null) throw new InvalidOperationException("Character creation view is not bound.");
            var result = flow.CreateCharacter(nameInput ? nameInput.text : "", age, "fresh", appearanceId);
            if (result.Status == CommandStatus.Committed || result.Status == CommandStatus.AlreadyCommitted)
            {
                status?.SetKey("status.character.created");
                onCreated();
                return;
            }

            status?.SetKey("reason." + result.ReasonKey);
        }

        private void RenderSelection()
        {
            if (ageValue) ageValue.text = age.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (appearanceValue) appearanceValue.text = appearanceId == "base.female" ? "♀" : "♂";
        }
    }
}
