#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StartupLife.Presentation
{
    public enum CharacterCreatorPage { Identity, Look, Wardrobe, Traits }

    public sealed class CharacterCreatorChoice
    {
        public CharacterVisualSlot Slot { get; }
        public string Id { get; }
        public string LabelKey { get; }
        public CharacterCreatorChoice(CharacterVisualSlot slot, string id, string labelKey)
        {
            if (!Enum.IsDefined(typeof(CharacterVisualSlot), slot))
                throw new ArgumentOutOfRangeException(nameof(slot));
            if (string.IsNullOrWhiteSpace(id) || id.Trim() != id)
                throw new ArgumentException("Invalid option ID.", nameof(id));
            if (string.IsNullOrWhiteSpace(labelKey) ||
                !labelKey.StartsWith("character.option.", StringComparison.Ordinal))
                throw new ArgumentException("Invalid localization key.", nameof(labelKey));
            Slot = slot;
            Id = id;
            LabelKey = labelKey;
        }
    }

    // Session only. This object is never serialized into historic save receipts.
    public sealed class CharacterCreatorDraftSession
    {
        private readonly CharacterLookCatalog catalog;
        private readonly IReadOnlyDictionary<CharacterVisualSlot, IReadOnlyList<CharacterCreatorChoice>> options;
        public CharacterCreatorPage Page { get; private set; }
        public CharacterLookDraft Draft { get; private set; }
        public bool CanGoBack => Page > CharacterCreatorPage.Identity;
        public bool CanGoNext => Page < CharacterCreatorPage.Traits;

        public CharacterCreatorDraftSession(CharacterLookCatalog catalog,
            IEnumerable<CharacterCreatorChoice> orderedChoices, string? legacyAppearanceId = null)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            if (orderedChoices == null) throw new ArgumentNullException(nameof(orderedChoices));
            var map = new Dictionary<CharacterVisualSlot, List<CharacterCreatorChoice>>();
            foreach (CharacterVisualSlot slot in Enum.GetValues(typeof(CharacterVisualSlot)))
                map.Add(slot, new List<CharacterCreatorChoice>());
            foreach (var option in orderedChoices)
            {
                if (option == null || !catalog.Contains(option.Slot, option.Id))
                    throw new ArgumentException("UI option missing from valid catalog.", nameof(orderedChoices));
                var list = map[option.Slot];
                foreach (var prior in list)
                    if (prior.Id == option.Id)
                        throw new ArgumentException("Duplicate option in UI catalog.", nameof(orderedChoices));
                list.Add(option);
            }
            var immutable = new Dictionary<CharacterVisualSlot, IReadOnlyList<CharacterCreatorChoice>>();
            foreach (var pair in map)
            {
                if (pair.Value.Count == 0)
                    throw new ArgumentException("Every cosmetic slot needs a localized option.", nameof(orderedChoices));
                immutable.Add(pair.Key, Array.AsReadOnly(pair.Value.ToArray()));
            }
            options = new ReadOnlyDictionary<CharacterVisualSlot, IReadOnlyList<CharacterCreatorChoice>>(immutable);
            Draft = catalog.NewDraft(legacyAppearanceId);
            foreach (var pair in options) LabelKey(pair.Key);
            Page = CharacterCreatorPage.Identity;
        }

        public string LabelKey(CharacterVisualSlot slot)
        {
            var selected = Draft.Get(slot);
            foreach (var option in Choices(slot))
                if (option.Id == selected)
                    return option.LabelKey;
            throw new InvalidOperationException("Selected option has no localization key.");
        }

        public IReadOnlyList<CharacterCreatorChoice> Choices(CharacterVisualSlot slot)
        {
            if (!options.TryGetValue(slot, out var result))
                throw new ArgumentOutOfRangeException(nameof(slot));
            return result;
        }

        public void Select(CharacterVisualSlot slot, string id)
        {
            foreach (var option in Choices(slot))
                if (option.Id == id)
                {
                    Draft = catalog.Choose(Draft, slot, id);
                    return;
                }
            throw new ArgumentException("Option not in localized UI catalog.", nameof(id));
        }

        public void Cycle(CharacterVisualSlot slot, int direction)
        {
            if (direction != -1 && direction != 1)
                throw new ArgumentOutOfRangeException(nameof(direction));
            var list = Choices(slot);
            for (var i = 0; i < list.Count; i++)
                if (list[i].Id == Draft.Get(slot))
                {
                    Draft = catalog.Choose(Draft, slot,
                        list[(i + direction + list.Count) % list.Count].Id);
                    return;
                }
            throw new InvalidOperationException("Draft no longer belongs to UI choices.");
        }

        public void Next() { if (CanGoNext) Page++; }
        public void Previous() { if (CanGoBack) Page--; }
    }
}
