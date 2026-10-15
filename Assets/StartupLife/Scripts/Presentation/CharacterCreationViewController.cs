using System;
using System.Collections.Generic;
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
        [SerializeField] private TextAsset draftCatalog;
        [SerializeField] private GameObject identityPage;
        [SerializeField] private GameObject lookPage;
        [SerializeField] private GameObject wardrobePage;
        [SerializeField] private GameObject traitsPage;
        [SerializeField] private UnityEngine.UI.ScrollRect creatorScroll;
        [SerializeField] private UnityEngine.UI.Button creatorBackButton;
        [SerializeField] private UnityEngine.UI.Button creatorNextButton;
        [SerializeField] private LocalizedKeyLabel faceValue;
        [SerializeField] private LocalizedKeyLabel hairValue;
        [SerializeField] private LocalizedKeyLabel hairColorValue;
        [SerializeField] private LocalizedKeyLabel skinToneValue;
        [SerializeField] private LocalizedKeyLabel accessoryValue;
        [SerializeField] private LocalizedKeyLabel outfitValue;
        [SerializeField] private LocalizedKeyLabel traitValue;

        [Serializable] private sealed class DraftCatalogJson
        {
            public string schema = "";
            public string status = "";
            public CatalogOption[] options = Array.Empty<CatalogOption>();
            public CatalogDefault[] defaults = Array.Empty<CatalogDefault>();
        }
        [Serializable] private sealed class CatalogOption
        {
            public string slot = "";
            public string id = "";
            public string labelKey = "";
        }
        [Serializable] private sealed class CatalogDefault
        {
            public string slot = "";
            public string id = "";
        }

        private CharacterCreatorDraftSession draft;
        private FirstPlayableFlow flow;
        private Action onCreated;
        private LocalizedKeyLabel appearanceLabel;
        private int age = 25;
        private string appearanceId = "base.female";

        public void Bind(FirstPlayableFlow coordinator, Action created)
        {
            flow = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            onCreated = created ?? throw new ArgumentNullException(nameof(created));
            // Old scenes without a repaired catalog retain the historical
            // identity-only creator. No unapproved visual preview is loaded.
            if (draftCatalog != null)
                draft = ParseDraft(draftCatalog);
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
            draft?.Select(CharacterVisualSlot.Appearance, appearanceId);
            RenderSelection();
        }

        public void SelectMale()
        {
            appearanceId = "base.male";
            draft?.Select(CharacterVisualSlot.Appearance, appearanceId);
            RenderSelection();
        }

        public void Submit()
        {
            if (flow == null) throw new InvalidOperationException("Character creation view is not bound.");
            // Cosmetic/trait selections are intentionally NOT serialized yet.
            // Only the historic appearance ID enters the authoritative command.
            var selectedAppearance = draft != null ? draft.Draft.AppearanceId : appearanceId;
            var result = flow.CreateCharacter(nameInput ? nameInput.text : "", age,
                "fresh", selectedAppearance).Command;
            if (result.Status == CommandStatus.Committed || result.Status == CommandStatus.AlreadyCommitted)
            {
                status?.SetKey("status.character.created");
                onCreated();
                return;
            }

            status?.SetKey("reason." + result.ReasonKey);
        }

        public void NextCreatorPage() { if (draft == null) return; draft.Next(); RenderSelection(); }
        public void PreviousCreatorPage() { if (draft == null) return; draft.Previous(); RenderSelection(); }

        public void PreviousFace() => Cycle(CharacterVisualSlot.Face, -1);
        public void NextFace() => Cycle(CharacterVisualSlot.Face, 1);
        public void PreviousHair() => Cycle(CharacterVisualSlot.Hair, -1);
        public void NextHair() => Cycle(CharacterVisualSlot.Hair, 1);
        public void PreviousHairColor() => Cycle(CharacterVisualSlot.HairColor, -1);
        public void NextHairColor() => Cycle(CharacterVisualSlot.HairColor, 1);
        public void PreviousSkinTone() => Cycle(CharacterVisualSlot.SkinTone, -1);
        public void NextSkinTone() => Cycle(CharacterVisualSlot.SkinTone, 1);
        public void PreviousAccessory() => Cycle(CharacterVisualSlot.Accessory, -1);
        public void NextAccessory() => Cycle(CharacterVisualSlot.Accessory, 1);
        public void PreviousOutfit() => Cycle(CharacterVisualSlot.Outfit, -1);
        public void NextOutfit() => Cycle(CharacterVisualSlot.Outfit, 1);
        public void PreviousTrait() => Cycle(CharacterVisualSlot.Trait, -1);
        public void NextTrait() => Cycle(CharacterVisualSlot.Trait, 1);

        private void Cycle(CharacterVisualSlot slot, int direction)
        {
            if (draft == null) return;
            draft.Cycle(slot, direction);
            RenderSelection();
        }

        private static CharacterCreatorDraftSession ParseDraft(TextAsset source)
        {
            var payload = JsonUtility.FromJson<DraftCatalogJson>(source.text);
            if (payload == null ||
                payload.schema != "startup-life.character-creator.draft.v1" ||
                payload.status != "DRAFT_COSMETIC_OPTIONS_NO_APPROVED_SPRITES" ||
                payload.options == null || payload.defaults == null)
                throw new InvalidOperationException("Character Creator catalog version/status invalid.");
            var options = new List<CharacterVisualOption>(payload.options.Length);
            var labels = new List<CharacterCreatorChoice>(payload.options.Length);
            foreach (var option in payload.options)
            {
                if (!Enum.TryParse(option.slot, false, out CharacterVisualSlot slot) ||
                    !Enum.IsDefined(typeof(CharacterVisualSlot), slot))
                    throw new InvalidOperationException("Invalid Character Creator slot: " + option.slot);
                options.Add(new CharacterVisualOption(option.id, slot));
                labels.Add(new CharacterCreatorChoice(slot, option.id, option.labelKey));
            }
            var defaults = new Dictionary<CharacterVisualSlot, string>();
            foreach (var choice in payload.defaults)
            {
                if (!Enum.TryParse(choice.slot, false, out CharacterVisualSlot slot) ||
                    !Enum.IsDefined(typeof(CharacterVisualSlot), slot) ||
                    !defaults.TryAdd(slot, choice.id))
                    throw new InvalidOperationException("Duplicate/invalid character default slot.");
            }
            return new CharacterCreatorDraftSession(
                new CharacterLookCatalog(options, defaults), labels);
        }

        private void ShowPage(GameObject page, CharacterCreatorPage current)
        {
            if (page && draft != null)
                page.SetActive(draft.Page == current);
        }

        private static void Label(LocalizedKeyLabel target, CharacterCreatorDraftSession state,
            CharacterVisualSlot slot)
        {
            if (target) target.SetKey(state.LabelKey(slot));
        }

        private void RenderSelection()
        {
            if (ageValue) ageValue.text = age.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var selectedAppearance = draft != null ? draft.Draft.AppearanceId : appearanceId;
            if (appearanceValue)
            {
                if (!appearanceLabel) appearanceLabel = LocalizedKeyLabel.EnsureFor(appearanceValue);
                appearanceLabel.SetKey(selectedAppearance == "base.female"
                    ? "character.appearance.female" : "character.appearance.male");
            }
            if (draft == null) return;

            ShowPage(identityPage, CharacterCreatorPage.Identity);
            ShowPage(lookPage, CharacterCreatorPage.Look);
            ShowPage(wardrobePage, CharacterCreatorPage.Wardrobe);
            ShowPage(traitsPage, CharacterCreatorPage.Traits);
            if (creatorBackButton) creatorBackButton.interactable = draft.CanGoBack;
            if (creatorNextButton) creatorNextButton.interactable = draft.CanGoNext;
            Label(faceValue, draft, CharacterVisualSlot.Face);
            Label(hairValue, draft, CharacterVisualSlot.Hair);
            Label(hairColorValue, draft, CharacterVisualSlot.HairColor);
            Label(skinToneValue, draft, CharacterVisualSlot.SkinTone);
            Label(accessoryValue, draft, CharacterVisualSlot.Accessory);
            Label(outfitValue, draft, CharacterVisualSlot.Outfit);
            Label(traitValue, draft, CharacterVisualSlot.Trait);
            // Reset only on explicit paging. No Canvas rebuild each frame.
            if (creatorScroll) creatorScroll.verticalNormalizedPosition = 1f;
        }
    }
}
