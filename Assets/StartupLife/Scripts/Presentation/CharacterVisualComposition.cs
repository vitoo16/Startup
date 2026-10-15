#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StartupLife.Presentation
{
    // Sprite views must be separately authored. Do not silently mirror a front
    // pose into a side/back variant: asymmetric hair, outfit and props would break.
    public enum CharacterRigView
    {
        Front, ThreeQuarterLeft, Side, Back
    }

    // No Sprite or Unity dependency: a visual recipe references ONLY catalog
    // IDs from the independently approved registry passed by its caller.
    public sealed class CharacterVisualLayer
    {
        public CharacterVisualSlot Slot { get; }
        public string OptionId { get; }
        public CharacterRigView View { get; }
        public string PartId { get; }
        public string SpriteAssetId { get; }
        public int SortingOrder { get; }
        public IReadOnlyList<string> CoveredPartIds { get; }

        public CharacterVisualLayer(CharacterVisualSlot slot, string optionId,
            CharacterRigView view, string partId, string spriteAssetId,
            int sortingOrder, IEnumerable<string>? coveredPartIds = null)
        {
            if (!CharacterVisualComposer.IsRenderableSlot(slot))
                throw new ArgumentException("Trait and color tokens do not own sprite layers.", nameof(slot));
            if (!Enum.IsDefined(typeof(CharacterRigView), view))
                throw new ArgumentOutOfRangeException(nameof(view));
            OptionId = Canonical(optionId, nameof(optionId));
            PartId = Canonical(partId, nameof(partId));
            SpriteAssetId = Canonical(spriteAssetId, nameof(spriteAssetId));
            if (sortingOrder < -1000 || sortingOrder > 1000)
                throw new ArgumentOutOfRangeException(nameof(sortingOrder));
            Slot = slot;
            View = view;
            SortingOrder = sortingOrder;
            var hidden = new List<string>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            if (coveredPartIds != null)
                foreach (var covered in coveredPartIds)
                {
                    var id = Canonical(covered, nameof(coveredPartIds));
                    if (id == PartId || !unique.Add(id))
                        throw new ArgumentException("Invalid or duplicate covered rig part.", nameof(coveredPartIds));
                    hidden.Add(id);
                }
            CoveredPartIds = Array.AsReadOnly(hidden.ToArray());
        }

        private static string Canonical(string? id, string label)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Trim() != id ||
                id.IndexOfAny(new[] { '/', '\\', ':', '\n', '\r', '\t' }) >= 0)
                throw new ArgumentException("Noncanonical catalog ID.", label);
            return id;
        }
    }

    // Immutable preview plan. Sprite swapping is deferred to the shared rig
    // renderer. Cosmetic previews must not dispatch GameCommands or read saves.
    public sealed class CharacterVisualPlan
    {
        public CharacterRigView View { get; }
        public string AppearanceId { get; }
        public string OutfitId { get; }
        public string SkinToneId { get; }
        public string HairColorId { get; }
        public IReadOnlyList<CharacterVisualLayer> Layers { get; }

        internal CharacterVisualPlan(CharacterRigView view, CharacterLookDraft draft,
            string outfitId, List<CharacterVisualLayer> layers)
        {
            View = view;
            AppearanceId = draft.AppearanceId;
            OutfitId = outfitId;
            SkinToneId = draft.SkinToneId; // Palette tokens, not extra per-color textures.
            HairColorId = draft.HairColorId;
            Layers = Array.AsReadOnly(layers.ToArray());
        }
    }

    public sealed class CharacterVisualComposer
    {
        private static readonly CharacterVisualSlot[] RenderableSlots =
        {
            CharacterVisualSlot.Appearance, CharacterVisualSlot.Face,
            CharacterVisualSlot.Hair, CharacterVisualSlot.Outfit,
            CharacterVisualSlot.Accessory
        };

        private readonly CharacterLookCatalog looks;
        private readonly CharacterWardrobeResolver wardrobe;
        private readonly Dictionary<string, IReadOnlyList<CharacterVisualLayer>> bindings;

        public CharacterVisualComposer(CharacterLookCatalog lookCatalog,
            CharacterWardrobeResolver wardrobeResolver, IEnumerable<CharacterVisualLayer> layerDefinitions,
            IEnumerable<string> independentlyApprovedSpriteIds)
        {
            looks = lookCatalog ?? throw new ArgumentNullException(nameof(lookCatalog));
            wardrobe = wardrobeResolver ?? throw new ArgumentNullException(nameof(wardrobeResolver));
            if (layerDefinitions == null) throw new ArgumentNullException(nameof(layerDefinitions));
            if (independentlyApprovedSpriteIds == null)
                throw new ArgumentNullException(nameof(independentlyApprovedSpriteIds));

            var approved = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in independentlyApprovedSpriteIds)
            {
                if (string.IsNullOrWhiteSpace(id) || id.Trim() != id || !approved.Add(id))
                    throw new ArgumentException("Invalid/duplicate approved asset reference.", nameof(independentlyApprovedSpriteIds));
            }
            var working = new Dictionary<string, List<CharacterVisualLayer>>(StringComparer.Ordinal);
            foreach (var layer in layerDefinitions)
            {
                if (layer == null) throw new ArgumentException("Null visual layer.", nameof(layerDefinitions));
                if (!looks.Contains(layer.Slot, layer.OptionId))
                    throw new ArgumentException("Layer option does not exist in the cosmetic catalog: " + layer.OptionId, nameof(layerDefinitions));
                if (!approved.Contains(layer.SpriteAssetId))
                    throw new ArgumentException("Visual layer uses an unapproved sprite: " + layer.SpriteAssetId, nameof(layerDefinitions));
                var key = Key(layer.Slot, layer.OptionId, layer.View);
                if (!working.TryGetValue(key, out var collection))
                {
                    collection = new List<CharacterVisualLayer>();
                    working.Add(key, collection);
                }
                foreach (var previous in collection)
                    if (previous.PartId == layer.PartId)
                        throw new ArgumentException("Duplicate part binding for option/view: " + layer.PartId, nameof(layerDefinitions));
                collection.Add(layer);
            }
            bindings = new Dictionary<string, IReadOnlyList<CharacterVisualLayer>>(StringComparer.Ordinal);
            foreach (var pair in working)
                bindings.Add(pair.Key, Array.AsReadOnly(pair.Value.ToArray()));
        }

        internal static bool IsRenderableSlot(CharacterVisualSlot slot) =>
            slot == CharacterVisualSlot.Appearance || slot == CharacterVisualSlot.Face ||
            slot == CharacterVisualSlot.Hair || slot == CharacterVisualSlot.Outfit ||
            slot == CharacterVisualSlot.Accessory;

        private static string Key(CharacterVisualSlot slot, string option, CharacterRigView view) =>
            ((int)slot).ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" +
            option + "|" + ((int)view).ToString(System.Globalization.CultureInfo.InvariantCulture);

        public CharacterVisualPlan Compose(CharacterLookDraft draft, string? careerId,
            CharacterSceneContext context, CharacterRigView view)
        {
            looks.Validate(draft);
            if (!Enum.IsDefined(typeof(CharacterRigView), view))
                throw new ArgumentOutOfRangeException(nameof(view));
            var workOutfit = wardrobe.Resolve(draft, careerId, context);
            var layers = new List<CharacterVisualLayer>(12);
            var occupied = new HashSet<string>(StringComparer.Ordinal);
            var obscured = new HashSet<string>(StringComparer.Ordinal);
            foreach (var slot in RenderableSlots)
            {
                var selectedId = slot == CharacterVisualSlot.Outfit ? workOutfit : draft.Get(slot);
                if (!bindings.TryGetValue(Key(slot, selectedId, view), out var slotLayers))
                {
                    if (slot == CharacterVisualSlot.Face || slot == CharacterVisualSlot.Hair ||
                        slot == CharacterVisualSlot.Outfit)
                        throw new InvalidOperationException("Required authored view is missing for " + slot + " / " + view);
                    continue; // appearance-independent base or 'none' accessory
                }
                foreach (var layer in slotLayers)
                {
                    if (!occupied.Add(layer.PartId))
                        throw new InvalidOperationException("Two options target the same rig part: " + layer.PartId);
                    layers.Add(layer);
                    foreach (var id in layer.CoveredPartIds)
                        obscured.Add(id);
                }
            }
            layers.RemoveAll(layer => obscured.Contains(layer.PartId));
            layers.Sort((a, b) =>
            {
                var order = a.SortingOrder.CompareTo(b.SortingOrder);
                return order != 0 ? order : string.CompareOrdinal(a.PartId, b.PartId);
            });
            if (layers.Count == 0)
                throw new InvalidOperationException("A cosmetic plan cannot render an empty actor.");
            return new CharacterVisualPlan(view, draft, workOutfit, layers);
        }
    }
}
