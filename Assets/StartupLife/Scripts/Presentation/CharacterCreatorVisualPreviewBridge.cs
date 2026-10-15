#nullable enable
using System;
using UnityEngine;

namespace StartupLife.Presentation
{
    // Optional adapter from in-memory creator choices to a SINGLE approved,
    // pre-authored character rig. Not added to the current FirstPlayable scene:
    // Art/Approved/registry.json contains zero independently approved sprites.
    // Unity integration must configure from the independently approved registry
    // and an art-reviewed character palette. Test fixtures are NOT approvals.
    public sealed class CharacterCreatorVisualPreviewBridge : MonoBehaviour
    {
        private CharacterVisualComposer? composer;
        private CharacterVisualAssembler? assembler;
        private string? bakedSkinToneId;
        private string? bakedHairColorId;
        private CharacterLookDraft? currentDraft;
        private CharacterRigView currentView = CharacterRigView.Front;
        private CharacterSceneContext currentContext = CharacterSceneContext.Home;
        private string? currentCareerId;

        public bool IsConfigured => composer != null && assembler != null;
        public bool IsShowing { get; private set; }
        public CharacterRigView View => currentView;

        // Does NOT discover assets, mark a sprite approved, instantiate a prefab,
        // clone SpriteRenderers, or update gameplay/save data. A caller must have
        // already validated real sprite IDs/rights and configured the shared rig.
        public void Configure(CharacterVisualComposer approvedComposer,
            CharacterVisualAssembler approvedAssembler, string authoredSkinToneId,
            string authoredHairColorId)
        {
            if (IsConfigured) throw new InvalidOperationException("Preview rig is already configured.");
            if (approvedComposer == null) throw new ArgumentNullException(nameof(approvedComposer));
            if (approvedAssembler == null || approvedAssembler.PartCount == 0)
                throw new ArgumentException("A preconfigured articulated rig is required.", nameof(approvedAssembler));
            if (string.IsNullOrWhiteSpace(authoredSkinToneId) ||
                string.IsNullOrWhiteSpace(authoredHairColorId))
                throw new ArgumentException("Approved baked palette IDs are required.");
            composer = approvedComposer;
            assembler = approvedAssembler;
            bakedSkinToneId = authoredSkinToneId;
            bakedHairColorId = authoredHairColorId;
            assembler.Clear();
            IsShowing = false;
        }

        public bool TryShow(CharacterLookDraft draft)
        {
            if (draft == null) throw new ArgumentNullException(nameof(draft));
            currentDraft = draft;
            if (!IsConfigured) return false;
            // The assembler does not apply palette tokens yet. Never present a
            // default-color sprite as if a different skin/hair color were shown.
            if (!string.Equals(draft.SkinToneId, bakedSkinToneId, StringComparison.Ordinal) ||
                !string.Equals(draft.HairColorId, bakedHairColorId, StringComparison.Ordinal))
            {
                HideRenderedSprites();
                return false;
            }

            try
            {
                var plan = composer!.Compose(draft, currentCareerId, currentContext, currentView);
                // A gender-specific body layer is required to prevent misleading
                // previews that look identical for both appearance options.
                var bodyPresent = false;
                for (var i = 0; i < plan.Layers.Count; i++)
                {
                    var layer = plan.Layers[i];
                    if (layer.Slot == CharacterVisualSlot.Appearance &&
                        layer.OptionId == draft.AppearanceId)
                    {
                        bodyPresent = true;
                        break;
                    }
                }
                if (!bodyPresent)
                {
                    HideRenderedSprites();
                    return false;
                }
                assembler!.Apply(plan); // No hierarchy recreation; preflights sprite approval.
                IsShowing = true;
                return true;
            }
            catch (ArgumentException)
            {
                // Missing/invalid option: a stale previous character may not
                // masquerade as the newly chosen hair, body or work uniform.
                HideRenderedSprites();
                return false;
            }
            catch (InvalidOperationException)
            {
                HideRenderedSprites();
                return false;
            }
        }

        public bool SetView(CharacterRigView view)
        {
            if (!Enum.IsDefined(typeof(CharacterRigView), view))
                throw new ArgumentOutOfRangeException(nameof(view));
            currentView = view;
            return currentDraft != null && TryShow(currentDraft);
        }

        public bool SetSceneContext(CharacterSceneContext context, string? careerId)
        {
            if (!Enum.IsDefined(typeof(CharacterSceneContext), context))
                throw new ArgumentOutOfRangeException(nameof(context));
            currentContext = context;
            currentCareerId = careerId;
            return currentDraft != null && TryShow(currentDraft);
        }

        public void Hide()
        {
            currentDraft = null;
            HideRenderedSprites();
        }

        private void HideRenderedSprites()
        {
            if (assembler != null) assembler.Clear();
            IsShowing = false;
        }
    }
}
