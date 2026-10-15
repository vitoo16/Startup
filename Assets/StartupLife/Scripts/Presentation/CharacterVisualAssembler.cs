#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StartupLife.Presentation
{
    // An implementation must resolve only sprite IDs cleared in
    // Art/Approved/registry.json. There is intentionally NO production resolver
    // until art/IP/Unity BeautyCell approval is complete.
    public interface IApprovedCharacterSpriteResolver
    {
        bool TryResolveApprovedSprite(string spriteAssetId, out Sprite sprite);
    }

    [Serializable]
    public sealed class CharacterRigPartBinding
    {
        [SerializeField] private string partId = "";
        [SerializeField] private SpriteRenderer? spriteRenderer;

        public string PartId => partId;
        public SpriteRenderer? Renderer => spriteRenderer;

        public CharacterRigPartBinding(string partId, SpriteRenderer renderer)
        {
            if (string.IsNullOrWhiteSpace(partId) || partId.Trim() != partId)
                throw new ArgumentException("Noncanonical rig part.", nameof(partId));
            this.partId = partId;
            spriteRenderer = renderer != null ? renderer :
                throw new ArgumentNullException(nameof(renderer));
        }
    }

    // Reuses the same preauthored transforms, SpriteRenderers and Animator rig.
    // Apply() never instantiates, reparents, destroys, or modifies animation
    // transforms. Only approved sprite bindings and sorting order may change.
    public sealed class CharacterVisualAssembler : MonoBehaviour
    {
        private IApprovedCharacterSpriteResolver? source;
        private CharacterRigPartBinding[] bindings = Array.Empty<CharacterRigPartBinding>();
        private readonly Dictionary<string, int> index =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private Sprite?[] stagedSprites = Array.Empty<Sprite?>();
        private bool[] stagedVisible = Array.Empty<bool>();
        private int[] stagedOrders = Array.Empty<int>();
        private readonly HashSet<int> stagedParts = new HashSet<int>();
        private int expectedPixelsPerUnit;
        private bool configured;

        public int PartCount => configured ? bindings.Length : 0;

        // Called once with the already-authored rig in a prototype preview or
        // in an independently approved prefab. No auto-creation or scene scan.
        public void Configure(IApprovedCharacterSpriteResolver approvedSource,
            IReadOnlyList<CharacterRigPartBinding> rigBindings, int pixelsPerUnit)
        {
            if (configured) throw new InvalidOperationException("Rig is already configured.");
            if (approvedSource == null) throw new ArgumentNullException(nameof(approvedSource));
            if (rigBindings == null) throw new ArgumentNullException(nameof(rigBindings));
            if (pixelsPerUnit <= 0) throw new ArgumentOutOfRangeException(nameof(pixelsPerUnit));
            if (rigBindings.Count == 0)
                throw new ArgumentException("An articulated rig needs authored sprite slots.", nameof(rigBindings));

            var validated = new CharacterRigPartBinding[rigBindings.Count];
            var lookup = new Dictionary<string, int>(rigBindings.Count, StringComparer.Ordinal);
            var distinctRenderers = new HashSet<SpriteRenderer>();
            for (var i = 0; i < validated.Length; i++)
            {
                var part = rigBindings[i];
                if (part == null || string.IsNullOrWhiteSpace(part.PartId) ||
                    part.PartId.Trim() != part.PartId || part.Renderer == null)
                    throw new ArgumentException("Null or invalid authored rig binding.", nameof(rigBindings));
                if (!part.Renderer.transform.IsChildOf(transform))
                    throw new ArgumentException("The renderer must belong to the shared rig.", nameof(rigBindings));
                if (!lookup.TryAdd(part.PartId, i) || !distinctRenderers.Add(part.Renderer))
                    throw new ArgumentException("Duplicate rig part or SpriteRenderer.", nameof(rigBindings));
                validated[i] = part;
            }

            // Only after validation; failure must not touch any renderer.
            foreach (var pair in lookup) index.Add(pair.Key, pair.Value);
            bindings = validated;
            stagedSprites = new Sprite?[validated.Length];
            stagedVisible = new bool[validated.Length];
            stagedOrders = new int[validated.Length];
            expectedPixelsPerUnit = pixelsPerUnit;
            source = approvedSource;
            configured = true;
            Clear();
        }

        public bool Apply(CharacterVisualPlan plan)
        {
            if (!configured || source == null)
                throw new InvalidOperationException("An approved sprite source and rig are required.");
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (plan.Layers.Count > bindings.Length)
                throw new InvalidOperationException("The plan exceeds the authored rig capacity.");

            Array.Clear(stagedSprites, 0, stagedSprites.Length);
            Array.Clear(stagedVisible, 0, stagedVisible.Length);
            Array.Clear(stagedOrders, 0, stagedOrders.Length);
            stagedParts.Clear();

            // Resolve all layers first, with no visible updates on failure.
            for (var layerIndex = 0; layerIndex < plan.Layers.Count; layerIndex++)
            {
                var layer = plan.Layers[layerIndex];
                if (layer == null || !index.TryGetValue(layer.PartId, out var position) ||
                    !stagedParts.Add(position))
                    throw new InvalidOperationException("Unknown or duplicate rig part in visual plan.");
                if (!source.TryResolveApprovedSprite(layer.SpriteAssetId, out var sprite) ||
                    sprite == null)
                    throw new InvalidOperationException(
                        "Visual plan refers to a sprite without verified approval.");
                var texture = sprite.texture;
                if (Mathf.Abs(sprite.pixelsPerUnit - expectedPixelsPerUnit) > 0.001f ||
                    texture == null || texture.filterMode != FilterMode.Point ||
                    texture.mipmapCount != 1)
                    throw new InvalidOperationException("Approved sprite is not pixel-art import compatible.");
                stagedSprites[position] = sprite;
                stagedVisible[position] = true;
                stagedOrders[position] = layer.SortingOrder;
            }

            var changed = false;
            for (var i = 0; i < bindings.Length; i++)
            {
                // Was checked as non-null in Configure. Unity can destroy the
                // object afterward, so reject rather than spawn a replacement.
                var renderer = bindings[i].Renderer;
                if (renderer == null)
                    throw new InvalidOperationException("The authored rig has a missing renderer.");
            }
            for (var i = 0; i < bindings.Length; i++)
            {
                var renderer = bindings[i].Renderer!;
                var visible = stagedVisible[i];
                var sprite = stagedSprites[i];
                if (renderer.sprite != sprite)
                {
                    renderer.sprite = sprite;
                    changed = true;
                }
                if (visible && renderer.sortingOrder != stagedOrders[i])
                {
                    renderer.sortingOrder = stagedOrders[i];
                    changed = true;
                }
                if (renderer.enabled != visible)
                {
                    renderer.enabled = visible;
                    changed = true;
                }
            }
            return changed;
        }

        public void Clear()
        {
            if (!configured) return;
            foreach (var part in bindings)
            {
                var renderer = part.Renderer;
                if (renderer == null)
                    throw new InvalidOperationException("The authored rig has a missing renderer.");
                renderer.sprite = null;
                renderer.enabled = false;
            }
        }
    }
}
