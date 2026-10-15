#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StartupLife.Presentation
{
    // Cosmetic draft only. This is NOT authoritative GameState, a receipt, or
    // a gameplay trait bonus. CreateCharacter's old AppearanceId stays intact.
    public enum CharacterVisualSlot
    {
        Appearance, Face, Hair, HairColor, SkinTone, Accessory, Outfit, Trait
    }

    public enum CharacterSceneContext
    {
        Home, Work, Study, Business
    }

    public sealed class CharacterVisualOption
    {
        public string Id { get; }
        public CharacterVisualSlot Slot { get; }
        public CharacterVisualOption(string id, CharacterVisualSlot slot)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Trim() != id)
                throw new ArgumentException("Option ID must be nonblank and canonical.", nameof(id));
            if (!Enum.IsDefined(typeof(CharacterVisualSlot), slot))
                throw new ArgumentOutOfRangeException(nameof(slot));
            Id = id;
            Slot = slot;
        }
    }

    public sealed class CharacterLookDraft
    {
        public string AppearanceId { get; }
        public string FaceId { get; }
        public string HairId { get; }
        public string HairColorId { get; }
        public string SkinToneId { get; }
        public string AccessoryId { get; }
        public string OutfitId { get; }
        public string TraitId { get; }

        internal CharacterLookDraft(string appearance, string face, string hair, string hairColor,
            string skinTone, string accessory, string outfit, string trait)
        {
            AppearanceId = appearance;
            FaceId = face;
            HairId = hair;
            HairColorId = hairColor;
            SkinToneId = skinTone;
            AccessoryId = accessory;
            OutfitId = outfit;
            TraitId = trait;
        }

        public string Get(CharacterVisualSlot slot)
        {
            switch (slot)
            {
                case CharacterVisualSlot.Appearance: return AppearanceId;
                case CharacterVisualSlot.Face: return FaceId;
                case CharacterVisualSlot.Hair: return HairId;
                case CharacterVisualSlot.HairColor: return HairColorId;
                case CharacterVisualSlot.SkinTone: return SkinToneId;
                case CharacterVisualSlot.Accessory: return AccessoryId;
                case CharacterVisualSlot.Outfit: return OutfitId;
                case CharacterVisualSlot.Trait: return TraitId;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        internal CharacterLookDraft With(CharacterVisualSlot slot, string id)
        {
            return new CharacterLookDraft(
                slot == CharacterVisualSlot.Appearance ? id : AppearanceId,
                slot == CharacterVisualSlot.Face ? id : FaceId,
                slot == CharacterVisualSlot.Hair ? id : HairId,
                slot == CharacterVisualSlot.HairColor ? id : HairColorId,
                slot == CharacterVisualSlot.SkinTone ? id : SkinToneId,
                slot == CharacterVisualSlot.Accessory ? id : AccessoryId,
                slot == CharacterVisualSlot.Outfit ? id : OutfitId,
                slot == CharacterVisualSlot.Trait ? id : TraitId);
        }
    }

    // Data-driven option validation, independent of gender presentation.
    // No SpriteRenderer or disk access: one in-memory catalog per creator flow.
    public sealed class CharacterLookCatalog
    {
        private readonly IReadOnlyDictionary<CharacterVisualSlot, HashSet<string>> allowed;
        private readonly IReadOnlyDictionary<CharacterVisualSlot, string> defaults;
        private static readonly CharacterVisualSlot[] Slots =
            (CharacterVisualSlot[])Enum.GetValues(typeof(CharacterVisualSlot));

        public CharacterLookCatalog(IEnumerable<CharacterVisualOption> options,
            IReadOnlyDictionary<CharacterVisualSlot, string> defaultIds)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (defaultIds == null) throw new ArgumentNullException(nameof(defaultIds));

            var choices = new Dictionary<CharacterVisualSlot, HashSet<string>>();
            foreach (var slot in Slots)
                choices.Add(slot, new HashSet<string>(StringComparer.Ordinal));
            foreach (var option in options)
            {
                if (option == null) throw new ArgumentException("Null option.", nameof(options));
                if (!choices[option.Slot].Add(option.Id))
                    throw new ArgumentException("Duplicate cosmetic ID within one slot: " + option.Id, nameof(options));
            }
            var initial = new Dictionary<CharacterVisualSlot, string>();
            foreach (var slot in Slots)
            {
                if (!defaultIds.TryGetValue(slot, out var id) || !choices[slot].Contains(id))
                    throw new ArgumentException("Missing or invalid default for " + slot, nameof(defaultIds));
                initial.Add(slot, id);
            }
            if (initial[CharacterVisualSlot.Appearance] != "base.female" &&
                initial[CharacterVisualSlot.Appearance] != "base.male")
                throw new ArgumentException("Default appearance must be an existing legacy ID.", nameof(defaultIds));
            if (!choices[CharacterVisualSlot.Appearance].SetEquals(new[] { "base.female", "base.male" }))
                throw new ArgumentException("Appearance choices must preserve both legacy IDs.", nameof(options));
            allowed = new ReadOnlyDictionary<CharacterVisualSlot, HashSet<string>>(choices);
            defaults = new ReadOnlyDictionary<CharacterVisualSlot, string>(initial);
        }

        public bool Contains(CharacterVisualSlot slot, string id) =>
            allowed.TryGetValue(slot, out var ids) && id != null && ids.Contains(id);

        public CharacterLookDraft NewDraft(string? legacyAppearanceId = null)
        {
            var appearance = legacyAppearanceId ?? defaults[CharacterVisualSlot.Appearance];
            if (!Contains(CharacterVisualSlot.Appearance, appearance))
                throw new ArgumentException("Unknown legacy appearance ID.", nameof(legacyAppearanceId));
            return new CharacterLookDraft(appearance,
                defaults[CharacterVisualSlot.Face], defaults[CharacterVisualSlot.Hair],
                defaults[CharacterVisualSlot.HairColor], defaults[CharacterVisualSlot.SkinTone],
                defaults[CharacterVisualSlot.Accessory], defaults[CharacterVisualSlot.Outfit],
                defaults[CharacterVisualSlot.Trait]);
        }

        public CharacterLookDraft Choose(CharacterLookDraft current, CharacterVisualSlot slot, string id)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (!Contains(slot, id))
                throw new ArgumentException("Unrecognized cosmetic choice for " + slot + ".", nameof(id));
            // Choosing Nam/Nữ cannot silently change any other cosmetic, trait,
            // career, money, learning speed or user-visible occupation outfit.
            return current.Get(slot) == id ? current : current.With(slot, id);
        }

        public void Validate(CharacterLookDraft draft)
        {
            if (draft == null) throw new ArgumentNullException(nameof(draft));
            foreach (var slot in Slots)
                if (!Contains(slot, draft.Get(slot)))
                    throw new ArgumentException("Draft has an unknown option for " + slot, nameof(draft));
        }
    }

    // Career clothes are a visual overlay recipe. The underlying player's
    // chosen outfit and look must not change when their job/activity changes.
    public sealed class CharacterWardrobeRule
    {
        public string CareerId { get; }
        public CharacterSceneContext Context { get; }
        public string OutfitId { get; }

        public CharacterWardrobeRule(string careerId, CharacterSceneContext context, string outfitId)
        {
            if (string.IsNullOrWhiteSpace(careerId) || careerId.Trim() != careerId)
                throw new ArgumentException("A canonical career ID is required.", nameof(careerId));
            if (string.IsNullOrWhiteSpace(outfitId) || outfitId.Trim() != outfitId)
                throw new ArgumentException("A canonical outfit ID is required.", nameof(outfitId));
            if (!Enum.IsDefined(typeof(CharacterSceneContext), context) ||
                context != CharacterSceneContext.Work)
                throw new ArgumentException("Occupation override is permitted only during Work context.", nameof(context));
            CareerId = careerId;
            Context = context;
            OutfitId = outfitId;
        }
    }

    public sealed class CharacterWardrobeResolver
    {
        private readonly CharacterLookCatalog catalog;
        private readonly IReadOnlyDictionary<string, string> workOutfits;

        public CharacterWardrobeResolver(CharacterLookCatalog catalog, IEnumerable<CharacterWardrobeRule> rules)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var rule in rules)
            {
                if (rule == null) throw new ArgumentException("Null wardrobe rule.", nameof(rules));
                if (!catalog.Contains(CharacterVisualSlot.Outfit, rule.OutfitId))
                    throw new ArgumentException("Wardrobe rule refers to unregistered outfit: " + rule.OutfitId, nameof(rules));
                if (!map.TryAdd(rule.CareerId, rule.OutfitId))
                    throw new ArgumentException("Duplicate career wardrobe rule: " + rule.CareerId, nameof(rules));
            }
            workOutfits = new ReadOnlyDictionary<string, string>(map);
        }

        public string Resolve(CharacterLookDraft draft, string? careerId, CharacterSceneContext context)
        {
            catalog.Validate(draft);
            if (!Enum.IsDefined(typeof(CharacterSceneContext), context))
                throw new ArgumentOutOfRangeException(nameof(context));
            if (context == CharacterSceneContext.Work &&
                !string.IsNullOrEmpty(careerId) && workOutfits.TryGetValue(careerId, out var uniform))
                return uniform;
            return draft.OutfitId; // personal outfit at home, study, business or unknown career
        }
    }
}
