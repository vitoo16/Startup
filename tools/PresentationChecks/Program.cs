#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using StartupLife.Core;
using StartupLife.Application;
using StartupLife.Infrastructure;
using StartupLife.Presentation;
using StartupLife.Content;

internal static class Program
{
    private static int passed;
    private static readonly List<string> failures = new List<string>();

    private static int Main(string[] args)
    {
        Check("visual composer yields stable rig layering and palette tokens", VisualComposerLayers);
        Check("visual composer maps work uniform without overwriting personal outfit", VisualComposerUniform);
        Check("occluded hair disappears only for headwear option", VisualComposerOcclusion);
        Check("missing independently authored side/back is rejected", VisualComposerRejectsMissingViews);
        Check("sprite must have independent approval and catalog registration", VisualComposerRejectsUnapproved);
        Check("duplicate rig parts and malformed catalog entries rejected", VisualComposerRejectsConflicts);
        Check("trait changes cannot mutate sprite composition", VisualComposerIgnoresTrait);
        Check("creator text scale 100/150/200 expands touch controls", CreatorTextScale);
        Check("creator text scale rejects invalid and clamps navigation", CreatorTextScaleInvalid);
        Check("creator touch geometry scales for low and high dpi", CreatorTouchResponsive);
        Check("creator touch geometry rejects invalid screen values", CreatorTouchInvalid);
        Check("four-step character draft navigation stays bounded", CharacterCreatorNavigation);
        Check("creator traits and appearance remain independent", CharacterCreatorIndependence);
        Check("cosmetic catalog rejects duplicate and invalid IDs", CosmeticCatalogRejectsInvalid);
        Check("Nam/Nu changes only appearance, not trait/hair/face/outfit", AppearanceSwitchIsCosmetic);
        Check("cosmetic choices preserve independent personal traits", CosmeticIndependence);
        Check("career wardrobe resolver scopes uniforms to work only", WardrobeIsContextual);
        Check("unknown career uses personal outfit and does not crash", WardrobeFallback);
        Check("unrecognized outfit and duplicate mapping are rejected", WardrobeCatalogGuards);
        Check("unrecognized legacy appearance has no silent fallback", LegacyAppearanceGuard);
        Check("unknown visual draft choices are rejected", UnknownCosmeticGuard);
        Check("M12 Vietnamese dashboard localization has stable keys and complete onboarding",
            DashboardTranslations);
        Check("M15 event choice localization keys are scoped, unique and immutable", EventChoiceTranslations);
        Check("initial snapshot selects character creation and copies read model", InitialProjection);
        Check("create character dispatches current run revision and injected command id", CreateDispatch);
        Check("life actions dispatch frozen commands from latest snapshot revision", ActionDispatch);
        Check("business commands preserve canonical kind, revision and payload", BusinessManagementDispatch);
        Check("playback acknowledgement binds current activity", PlaybackDispatch);
        Check("day and month advancement use boundary intent and project reached state", AdvanceDispatch);
        Check("active course read model flows through unchanged", ActiveCourseProjection);
        Check("empty command id source fails before dispatch", EmptyCommandIdRejected);
        Check("run id is read again for every dispatch", CurrentRunId);
        Check("empty ids block both direct and batch dispatch", EmptyBatchIdsRejected);
        Check("backward playback returns application rejection without rewards", BackwardPlaybackRejected);
        Check("old and restored course retries preserve original outcome and current projection", HistoricalCourseRetry);

        var report = new { suite = "Startup Life M7 presentation adapter — .NET, not Unity", passed, failed = failures.Count, failures };
        if (args.Length > 0)
        {
            var path = Path.GetFullPath(args[0]);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        }

        foreach (var failure in failures) Console.Error.WriteLine(failure);
        Console.WriteLine($"{passed}/{passed + failures.Count} passed." + (args.Length > 0 ? " Report: " + Path.GetFullPath(args[0]) : ""));
        return failures.Count == 0 ? 0 : 1;
    }

    private static CharacterVisualComposer FixtureVisualComposer(bool allViews = true,
        bool headwear = true)
    {
        var catalog = CharacterCatalog();
        var wardrobe = new CharacterWardrobeResolver(catalog, new[]
        {
            new CharacterWardrobeRule("developer", CharacterSceneContext.Work, "outfit.office")
        });
        var views = allViews
            ? new[] { CharacterRigView.Front, CharacterRigView.ThreeQuarterLeft,
                CharacterRigView.Side, CharacterRigView.Back }
            : new[] { CharacterRigView.Front };
        var parts = new List<CharacterVisualLayer>();
        foreach (var view in views)
        {
            // Pure mock catalog: these are NOT real/registered production sprites.
            parts.Add(new CharacterVisualLayer(CharacterVisualSlot.Face, "face.soft",
                view, "face", "fixture.face", 13));
            parts.Add(new CharacterVisualLayer(CharacterVisualSlot.Face, "face.angular",
                view, "face", "fixture.face.alt", 13));
            parts.Add(new CharacterVisualLayer(CharacterVisualSlot.Hair, "hair.short",
                view, "hair_front", "fixture.hair", 14));
            parts.Add(new CharacterVisualLayer(CharacterVisualSlot.Hair, "hair.long",
                view, "hair_back", "fixture.hair.back", 11));
            parts.Add(new CharacterVisualLayer(CharacterVisualSlot.Hair, "hair.long",
                view, "hair_front", "fixture.hair", 14));
            parts.Add(new CharacterVisualLayer(CharacterVisualSlot.Outfit, "outfit.casual",
                view, "outfit_torso", "fixture.casual", 15));
            parts.Add(new CharacterVisualLayer(CharacterVisualSlot.Outfit, "outfit.office",
                view, "outfit_torso", "fixture.office", 15));
            parts.Add(new CharacterVisualLayer(CharacterVisualSlot.Outfit, "outfit.apron",
                view, "outfit_torso", "fixture.apron", 15));
            if (headwear)
                parts.Add(new CharacterVisualLayer(CharacterVisualSlot.Accessory,
                    "accessory.none", view, "accessory_unused", "fixture.empty", 18));
        }
        var ids = new[] { "fixture.face", "fixture.face.alt", "fixture.hair",
            "fixture.hair.back", "fixture.casual", "fixture.office", "fixture.apron",
            "fixture.empty" };
        return new CharacterVisualComposer(catalog, wardrobe, parts, ids);
    }

    private static void VisualComposerLayers()
    {
        var catalog = CharacterCatalog();
        var composer = FixtureVisualComposer();
        var look = catalog.Choose(catalog.NewDraft(), CharacterVisualSlot.Hair, "hair.long");
        foreach (var view in new[] { CharacterRigView.Front, CharacterRigView.ThreeQuarterLeft,
            CharacterRigView.Side, CharacterRigView.Back })
        {
            var plan = composer.Compose(look, null, CharacterSceneContext.Home, view);
            Equal(view, plan.View);
            Equal("outfit.casual", plan.OutfitId);
            Equal("skin.warm", plan.SkinToneId);
            Equal("haircolor.ink", plan.HairColorId);
            True(plan.Layers.Count >= 4);
            True(plan.Layers[0].SortingOrder <= plan.Layers[plan.Layers.Count - 1].SortingOrder);
            True(plan.Layers.Any(x => x.PartId == "hair_back"));
        }
    }

    private static void VisualComposerUniform()
    {
        var catalog = CharacterCatalog();
        var look = catalog.NewDraft("base.male");
        var composer = FixtureVisualComposer();
        var work = composer.Compose(look, "developer", CharacterSceneContext.Work, CharacterRigView.Front);
        Equal("outfit.office", work.OutfitId);
        True(work.Layers.Any(x => x.SpriteAssetId == "fixture.office"));
        var home = composer.Compose(look, "developer", CharacterSceneContext.Home, CharacterRigView.Front);
        Equal("outfit.casual", home.OutfitId);
        True(home.Layers.Any(x => x.SpriteAssetId == "fixture.casual"));
        Equal("outfit.casual", look.OutfitId);
        var unknown = composer.Compose(look, "unknown", CharacterSceneContext.Work, CharacterRigView.Front);
        Equal("outfit.casual", unknown.OutfitId);
    }

    private static void VisualComposerOcclusion()
    {
        var catalog = CharacterCatalog();
        var wardrobe = new CharacterWardrobeResolver(catalog, Array.Empty<CharacterWardrobeRule>());
        var layers = new[]
        {
            new CharacterVisualLayer(CharacterVisualSlot.Face, "face.soft",
                CharacterRigView.Front, "face", "approved.face", 13),
            new CharacterVisualLayer(CharacterVisualSlot.Hair, "hair.short",
                CharacterRigView.Front, "hair_front", "approved.hair", 14),
            new CharacterVisualLayer(CharacterVisualSlot.Outfit, "outfit.casual",
                CharacterRigView.Front, "outfit_torso", "approved.shirt", 15),
            new CharacterVisualLayer(CharacterVisualSlot.Accessory, "accessory.none",
                CharacterRigView.Front, "hat", "approved.hat", 18, new[] { "hair_front" })
        };
        var composer = new CharacterVisualComposer(catalog, wardrobe, layers,
            new[] { "approved.face", "approved.hair", "approved.shirt", "approved.hat" });
        var result = composer.Compose(catalog.NewDraft(), null,
            CharacterSceneContext.Home, CharacterRigView.Front);
        True(result.Layers.Any(x => x.PartId == "hat"));
        True(result.Layers.All(x => x.PartId != "hair_front"));
        Equal("hair.short", catalog.NewDraft().HairId);
    }

    private static void VisualComposerRejectsMissingViews()
    {
        var composer = FixtureVisualComposer(false);
        var draft = CharacterCatalog().NewDraft();
        ThrowsInvalidOperation(() => composer.Compose(draft, null,
            CharacterSceneContext.Home, CharacterRigView.Side));
        ThrowsArgument(() => composer.Compose(draft, null,
            CharacterSceneContext.Home, (CharacterRigView)99));
    }

    private static void VisualComposerRejectsUnapproved()
    {
        var cat = CharacterCatalog();
        var wardrobe = new CharacterWardrobeResolver(cat, Array.Empty<CharacterWardrobeRule>());
        ThrowsArgument(() => new CharacterVisualComposer(cat, wardrobe,
            new[] { new CharacterVisualLayer(CharacterVisualSlot.Face, "face.soft",
                CharacterRigView.Front, "face", "Art/Source/raw.png", 13) },
            new[] { "approved.face" }));
        ThrowsArgument(() => new CharacterVisualComposer(cat, wardrobe,
            new[] { new CharacterVisualLayer(CharacterVisualSlot.Face, "face.missing",
                CharacterRigView.Front, "face", "approved.face", 13) },
            new[] { "approved.face" }));
    }

    private static void VisualComposerRejectsConflicts()
    {
        var cat = CharacterCatalog();
        var wardrobe = new CharacterWardrobeResolver(cat, Array.Empty<CharacterWardrobeRule>());
        var layer = new CharacterVisualLayer(CharacterVisualSlot.Face, "face.soft",
            CharacterRigView.Front, "face", "approved.face", 13);
        ThrowsArgument(() => new CharacterVisualComposer(cat, wardrobe,
            new[] { layer, layer }, new[] { "approved.face" }));
        ThrowsArgument(() => new CharacterVisualLayer(CharacterVisualSlot.Trait, "trait.careful",
            CharacterRigView.Front, "magic", "approved.face", 13));
        ThrowsArgument(() => new CharacterVisualLayer(CharacterVisualSlot.Hair, "hair.short",
            CharacterRigView.Front, "hair", "approved.hair", 14, new[] { "hair" }));
    }

    private static void VisualComposerIgnoresTrait()
    {
        var cat = CharacterCatalog();
        var composer = FixtureVisualComposer();
        var baseLook = cat.NewDraft("base.female");
        var traitLook = cat.Choose(baseLook, CharacterVisualSlot.Trait, "trait.energetic");
        var basePlan = composer.Compose(baseLook, "developer",
            CharacterSceneContext.Work, CharacterRigView.Front);
        var otherPlan = composer.Compose(traitLook, "developer",
            CharacterSceneContext.Work, CharacterRigView.Front);
        Equal(basePlan.Layers.Count, otherPlan.Layers.Count);
        for (int i = 0; i < basePlan.Layers.Count; i++)
            True(object.ReferenceEquals(basePlan.Layers[i], otherPlan.Layers[i]));
    }

    private static void CreatorTextScale()
    {
        Equal(1f, CharacterCreatorTextScalePolicy.Multiplier(0));
        Equal(1.5f, CharacterCreatorTextScalePolicy.Multiplier(1));
        Equal(2f, CharacterCreatorTextScalePolicy.Multiplier(2));
        Equal(96f, CharacterCreatorTextScalePolicy.ControlHeight(96f, 36f, 1f));
        Equal(132f, CharacterCreatorTextScalePolicy.ControlHeight(96f, 36f, 1.5f));
        Equal(168f, CharacterCreatorTextScalePolicy.ControlHeight(96f, 36f, 2f));
    }

    private static void CreatorTextScaleInvalid()
    {
        Equal(0, CharacterCreatorTextScalePolicy.ClampStep(-10));
        Equal(2, CharacterCreatorTextScalePolicy.ClampStep(900));
        ThrowsArgument(() => CharacterCreatorTextScalePolicy.Multiplier(3));
        ThrowsArgument(() => CharacterCreatorTextScalePolicy.ControlHeight(96f, 36f, .5f));
    }

    private static void CreatorTouchResponsive()
    {
        Equal(144f, CharacterCreatorTouchPolicy.MinimumCanvasUnits(
            1f / 3f, 0f, 360, 96f));
        Equal(132f, CharacterCreatorTouchPolicy.MinimumCanvasUnits(
            1f, 440f, 1080, 96f));
        Equal(96f, CharacterCreatorTouchPolicy.MinimumCanvasUnits(
            .5f, 160f, 360, 96f));
        Equal(144f, CharacterCreatorTouchPolicy.MinimumCanvasUnits(
            1f, 0f, 1080, 96f));
    }

    private static void CreatorTouchInvalid()
    {
        ThrowsArgument(() => CharacterCreatorTouchPolicy.MinimumCanvasUnits(
            0f, 160f, 360, 96f));
        ThrowsArgument(() => CharacterCreatorTouchPolicy.MinimumCanvasUnits(
            1f, 160f, 0, 96f));
        ThrowsArgument(() => CharacterCreatorTouchPolicy.MinimumCanvasUnits(
            1f, 160f, 360, 0f));
    }

    private static CharacterCreatorDraftSession CharacterCreatorFixture()
    {
        var map = CharacterCatalog();
        var items = new[] {
            new CharacterCreatorChoice(CharacterVisualSlot.Appearance,"base.female","character.option.appearance.female"),
            new CharacterCreatorChoice(CharacterVisualSlot.Appearance,"base.male","character.option.appearance.male"),
            new CharacterCreatorChoice(CharacterVisualSlot.Face,"face.soft","character.option.face.soft"),
            new CharacterCreatorChoice(CharacterVisualSlot.Hair,"hair.short","character.option.hair.short"),
            new CharacterCreatorChoice(CharacterVisualSlot.Hair,"hair.long","character.option.hair.long"),
            new CharacterCreatorChoice(CharacterVisualSlot.HairColor,"haircolor.ink","character.option.haircolor.ink"),
            new CharacterCreatorChoice(CharacterVisualSlot.SkinTone,"skin.warm","character.option.skin.warm"),
            new CharacterCreatorChoice(CharacterVisualSlot.Accessory,"accessory.none","character.option.accessory.none"),
            new CharacterCreatorChoice(CharacterVisualSlot.Outfit,"outfit.casual","character.option.outfit.casual"),
            new CharacterCreatorChoice(CharacterVisualSlot.Trait,"trait.balanced","character.option.trait.balanced"),
            new CharacterCreatorChoice(CharacterVisualSlot.Trait,"trait.careful","character.option.trait.careful")
        };
        return new CharacterCreatorDraftSession(map,items);
    }

    private static void CharacterCreatorNavigation()
    {
        var draft = CharacterCreatorFixture();
        Equal(CharacterCreatorPage.Identity,draft.Page);
        draft.Previous();
        Equal(CharacterCreatorPage.Identity,draft.Page);
        draft.Next();Equal(CharacterCreatorPage.Look,draft.Page);
        draft.Next();Equal(CharacterCreatorPage.Wardrobe,draft.Page);
        draft.Next();Equal(CharacterCreatorPage.Traits,draft.Page);
        draft.Next();Equal(CharacterCreatorPage.Traits,draft.Page);
        draft.Previous();Equal(CharacterCreatorPage.Wardrobe,draft.Page);
    }

    private static void CharacterCreatorIndependence()
    {
        var draft = CharacterCreatorFixture();
        draft.Select(CharacterVisualSlot.Trait,"trait.careful");
        draft.Cycle(CharacterVisualSlot.Appearance,1);
        draft.Cycle(CharacterVisualSlot.Hair,1);
        Equal("base.male",draft.Draft.AppearanceId);
        Equal("trait.careful",draft.Draft.TraitId);
        Equal("hair.long",draft.Draft.HairId);
        draft.Cycle(CharacterVisualSlot.Appearance,-1);
        Equal("base.female",draft.Draft.AppearanceId);
        Equal("trait.careful",draft.Draft.TraitId);
        Equal("character.option.trait.careful",draft.LabelKey(CharacterVisualSlot.Trait));
        ThrowsArgument(() => draft.Select(CharacterVisualSlot.Face,"missing"));
        ThrowsArgument(() => draft.Cycle(CharacterVisualSlot.Face,0));
    }

    private static CharacterLookCatalog CharacterCatalog()
    {
        var options = new[]
        {
            new CharacterVisualOption("base.male", CharacterVisualSlot.Appearance),
            new CharacterVisualOption("base.female", CharacterVisualSlot.Appearance),
            new CharacterVisualOption("face.soft", CharacterVisualSlot.Face),
            new CharacterVisualOption("face.angular", CharacterVisualSlot.Face),
            new CharacterVisualOption("hair.short", CharacterVisualSlot.Hair),
            new CharacterVisualOption("hair.long", CharacterVisualSlot.Hair),
            new CharacterVisualOption("haircolor.ink", CharacterVisualSlot.HairColor),
            new CharacterVisualOption("skin.warm", CharacterVisualSlot.SkinTone),
            new CharacterVisualOption("accessory.none", CharacterVisualSlot.Accessory),
            new CharacterVisualOption("outfit.casual", CharacterVisualSlot.Outfit),
            new CharacterVisualOption("outfit.office", CharacterVisualSlot.Outfit),
            new CharacterVisualOption("outfit.apron", CharacterVisualSlot.Outfit),
            new CharacterVisualOption("trait.balanced", CharacterVisualSlot.Trait),
            new CharacterVisualOption("trait.careful", CharacterVisualSlot.Trait),
            new CharacterVisualOption("trait.friendly", CharacterVisualSlot.Trait),
            new CharacterVisualOption("trait.energetic", CharacterVisualSlot.Trait)
        };
        var defaults = new Dictionary<CharacterVisualSlot, string>
        {
            [CharacterVisualSlot.Appearance] = "base.female",
            [CharacterVisualSlot.Face] = "face.soft",
            [CharacterVisualSlot.Hair] = "hair.short",
            [CharacterVisualSlot.HairColor] = "haircolor.ink",
            [CharacterVisualSlot.SkinTone] = "skin.warm",
            [CharacterVisualSlot.Accessory] = "accessory.none",
            [CharacterVisualSlot.Outfit] = "outfit.casual",
            [CharacterVisualSlot.Trait] = "trait.balanced"
        };
        return new CharacterLookCatalog(options, defaults);
    }

    private static void CosmeticCatalogRejectsInvalid()
    {
        var options = new[] {
            new CharacterVisualOption("base.female", CharacterVisualSlot.Appearance),
            new CharacterVisualOption("base.female", CharacterVisualSlot.Appearance)
        };
        ThrowsArgument(() => new CharacterVisualOption(" ", CharacterVisualSlot.Hair));
        ThrowsArgument(() => new CharacterVisualOption(" hair", CharacterVisualSlot.Hair));
        ThrowsArgument(() => new CharacterLookCatalog(options,
            new Dictionary<CharacterVisualSlot, string>()));
        ThrowsArgument(() => CharacterCatalog().Choose(
            CharacterCatalog().NewDraft(), CharacterVisualSlot.Hair, "hair.unknown"));
    }

    private static void AppearanceSwitchIsCosmetic()
    {
        var catalog = CharacterCatalog();
        var female = catalog.NewDraft("base.female");
        female = catalog.Choose(female, CharacterVisualSlot.Face, "face.angular");
        female = catalog.Choose(female, CharacterVisualSlot.Hair, "hair.long");
        female = catalog.Choose(female, CharacterVisualSlot.Trait, "trait.careful");
        var male = catalog.Choose(female, CharacterVisualSlot.Appearance, "base.male");
        Equal("base.female", female.AppearanceId);
        Equal("base.male", male.AppearanceId);
        Equal(female.FaceId, male.FaceId);
        Equal(female.HairId, male.HairId);
        Equal(female.TraitId, male.TraitId);
        Equal(female.OutfitId, male.OutfitId);
        True(!object.ReferenceEquals(female, male));
        True(object.ReferenceEquals(male,
            catalog.Choose(male, CharacterVisualSlot.Appearance, "base.male")));
    }

    private static void CosmeticIndependence()
    {
        var catalog = CharacterCatalog();
        var girl = catalog.NewDraft("base.female");
        var boy = catalog.NewDraft("base.male");
        foreach (var trait in new[] { "trait.careful", "trait.friendly", "trait.energetic", "trait.balanced" })
        {
            Equal(trait, catalog.Choose(girl, CharacterVisualSlot.Trait, trait).TraitId);
            Equal(trait, catalog.Choose(boy, CharacterVisualSlot.Trait, trait).TraitId);
        }
        catalog.Validate(girl);
        catalog.Validate(boy);
    }

    private static void WardrobeIsContextual()
    {
        var catalog = CharacterCatalog();
        var resolver = new CharacterWardrobeResolver(catalog, new[]
        {
            new CharacterWardrobeRule("developer", CharacterSceneContext.Work, "outfit.office"),
            new CharacterWardrobeRule("barista", CharacterSceneContext.Work, "outfit.apron")
        });
        var draft = catalog.Choose(catalog.NewDraft(), CharacterVisualSlot.Outfit, "outfit.casual");
        Equal("outfit.office", resolver.Resolve(draft, "developer", CharacterSceneContext.Work));
        Equal("outfit.apron", resolver.Resolve(draft, "barista", CharacterSceneContext.Work));
        foreach (var context in new[] {
            CharacterSceneContext.Home, CharacterSceneContext.Study, CharacterSceneContext.Business })
            Equal("outfit.casual", resolver.Resolve(draft, "barista", context));
        Equal("outfit.casual", draft.OutfitId);
        Equal("base.female", draft.AppearanceId);
    }

    private static void WardrobeFallback()
    {
        var catalog = CharacterCatalog();
        var resolver = new CharacterWardrobeResolver(catalog,
            new[] { new CharacterWardrobeRule("developer", CharacterSceneContext.Work, "outfit.office") });
        var draft = catalog.NewDraft("base.male");
        Equal("outfit.casual", resolver.Resolve(draft, "unknown-career", CharacterSceneContext.Work));
        Equal("outfit.casual", resolver.Resolve(draft, null, CharacterSceneContext.Work));
        Equal("outfit.casual", resolver.Resolve(draft, "developer", CharacterSceneContext.Home));
    }

    private static void WardrobeCatalogGuards()
    {
        var catalog = CharacterCatalog();
        ThrowsArgument(() => new CharacterWardrobeRule("developer", CharacterSceneContext.Home, "outfit.office"));
        ThrowsArgument(() => new CharacterWardrobeRule(" developer", CharacterSceneContext.Work, "outfit.office"));
        ThrowsArgument(() => new CharacterWardrobeResolver(catalog,
            new[] { new CharacterWardrobeRule("developer", CharacterSceneContext.Work, "outfit.not-approved") }));
        ThrowsArgument(() => new CharacterWardrobeResolver(catalog, new[]
        {
            new CharacterWardrobeRule("developer", CharacterSceneContext.Work, "outfit.office"),
            new CharacterWardrobeRule("developer", CharacterSceneContext.Work, "outfit.apron")
        }));
    }

    private static void LegacyAppearanceGuard()
    {
        var catalog = CharacterCatalog();
        ThrowsArgument(() => catalog.NewDraft("appearance.new-schema"));
        Equal("base.female", catalog.NewDraft().AppearanceId);
    }

    private static void UnknownCosmeticGuard()
    {
        var catalog = CharacterCatalog();
        True(!catalog.Contains(CharacterVisualSlot.Hair, "base.female"));
        ThrowsArgument(() => catalog.Choose(catalog.NewDraft(), CharacterVisualSlot.Face, "hair.short"));
        ThrowsArgument(() => catalog.Choose(catalog.NewDraft(), (CharacterVisualSlot)99, "face.soft"));
    }

    private static void ThrowsArgument(Action action)
    {
        try { action(); } catch (ArgumentException) { return; }
        throw new Exception("Expected ArgumentException.");
    }

    private static void EventChoiceTranslations()
    {
        var entries = EventLocalizationCatalog.Entries;
        // The two affordability/reward-unavailable labels introduced by M15
        // are required localized UI copy, not optional test exceptions.
        var sharedKeys = new[] { "title", "prompt", "retry", "changed", "unavailable",
            "reload", "insufficient_funds", "reward_unavailable", "time_unavailable",
            "after_shift_required" };
        Equal(sharedKeys.Length + FirstPlayableEventContent.VietnameseEntries.Count,
            entries.Count);
        Equal(entries.Count, entries.Select(x => x.Key)
            .Distinct(StringComparer.Ordinal).Count());
        True(entries.All(x => x.Key.StartsWith("event.", StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(x.Value)));
        var keys = new HashSet<string>(entries.Select(x => x.Key), StringComparer.Ordinal);
        foreach (var story in FirstPlayableEventContent.Definitions)
        {
            True(keys.Contains(story.TitleKey));
            True(keys.Contains(story.BodyKey));
            True(story.Choices.All(x => keys.Contains(x.LabelKey)));
        }
        foreach (var key in sharedKeys)
            True(entries.Any(x => x.Key == "event.choice." + key));
        ThrowsNotSupported(() =>
            ((IList<KeyValuePair<string,string>>)entries).Clear());
    }

    private static void DashboardTranslations()
    {
        var entries = DashboardLocalizationCatalog.Entries;
        True(entries.Count >= 45);
        Equal(entries.Count, entries.Select(x => x.Key)
            .Distinct(StringComparer.Ordinal).Count());
        True(entries.All(x => x.Key.StartsWith("dashboard.", StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(x.Value)));
        foreach (var text in new[] { "Nghề nghiệp", "Kỹ năng", "Tài chính", "Kinh doanh",
            "Hồ sơ", "Chữ nhỏ hơn", "Chữ lớn hơn", "Đã hiểu", "Xem lại hướng dẫn" })
        {
            True(DashboardLocalizationCatalog.TryFindKey(text, out var key));
            True(entries.Any(x => x.Key == key && x.Value == text));
        }
        foreach (var topic in new[] { "skills", "study", "compatibility", "time", "resign" })
        {
            True(entries.Any(x => x.Key == "dashboard.guide." + topic + ".title"));
            True(entries.Any(x => x.Key == "dashboard.guide." + topic + ".body"));
        }
        True(!DashboardLocalizationCatalog.TryFindKey("never-localized", out _));
        ThrowsNotSupported(() =>
            ((IList<KeyValuePair<string,string>>)entries).Clear());
    }

    private static void BusinessManagementDispatch()
    {
        var gateway = new FakeGateway(Snapshot(name: "Player", revision: 8));
        var flow = new FirstPlayableFlow(gateway, new SequenceIds());

        flow.LaunchBusiness("online-store", "v1", 100);
        Equal(CommandKind.LaunchBusiness, gateway.LastEnvelope!.Command.Kind);
        Equal("online-store", gateway.LastEnvelope.Command.ContentId);
        Equal("v1", gateway.LastEnvelope.Command.AppearanceId);
        Equal(100, gateway.LastEnvelope.Command.Amount);
        Equal(8L, gateway.LastEnvelope.ExpectedRevision);
        Equal("launch-business/1", gateway.LastEnvelope.CommandId);

        flow.ReinvestBusiness("run/business/1", 50);
        Equal(CommandKind.ReinvestBusiness, gateway.LastEnvelope!.Command.Kind);
        Equal(50, gateway.LastEnvelope.Command.Amount);

        flow.SetBusinessPricing("run/business/1", PricingPosture.Premium);
        Equal(CommandKind.SetBusinessPricing, gateway.LastEnvelope!.Command.Kind);
        Equal((int)PricingPosture.Premium, gateway.LastEnvelope.Command.Amount);

        flow.PauseBusiness("run/business/1");
        Equal(CommandKind.PauseBusiness, gateway.LastEnvelope!.Command.Kind);
        Equal("run/business/1", gateway.LastEnvelope.Command.ContentId);

        flow.ResumeBusiness("run/business/1");
        Equal(CommandKind.ResumeBusiness, gateway.LastEnvelope!.Command.Kind);

        flow.CloseBusiness("run/business/1");
        Equal(CommandKind.CloseBusiness, gateway.LastEnvelope!.Command.Kind);
        Equal("close-business/6", gateway.LastEnvelope.CommandId);
    }

    private static void InitialProjection()
    {
        var gateway = new FakeGateway(Snapshot(name: "", revision: 3, history: new[] { "one" }));
        var flow = new FirstPlayableFlow(gateway, new SequenceIds());
        var state = flow.Refresh();
        Equal(FirstPlayableScreen.CharacterCreation, state.Screen);
        Equal(3L, state.Revision);
        Equal("run-a", gateway.Current.RunId);
        Equal(1, state.History.Count);
        Equal("one", state.History[0]);
        True(!state.HasCareer); True(!state.CanStudy); True(!state.HasPendingPlayback);
        ThrowsNotSupported(() => ((IList<string>)state.History).Clear());
        ThrowsNotSupported(() => ((IDictionary<string, int>)state.SkillLevels).Add("forged", 5));
    }

    private static void CreateDispatch()
    {
        var gateway = new FakeGateway(Snapshot(name: "", revision: 7));
        gateway.Next = Snapshot(name: "An", revision: 8);
        var flow = new FirstPlayableFlow(gateway, new SequenceIds());
        var result = flow.CreateCharacter("An", 25, "fresh", "base.female");
        Equal("run-a", gateway.LastEnvelope!.RunId);
        Equal("create-character/1", gateway.LastEnvelope.CommandId);
        Equal(7L, gateway.LastEnvelope.ExpectedRevision);
        Equal(CommandKind.CreateCharacter, gateway.LastEnvelope.Command.Kind);
        Equal("fresh", gateway.LastEnvelope.Command.ContentId);
        Equal("base.female", gateway.LastEnvelope.Command.AppearanceId);
        Equal("An", gateway.LastEnvelope.Command.Name);
        Equal(25, gateway.LastEnvelope.Command.Amount);
        Equal(FirstPlayableScreen.Life, result.State.Screen);
        Equal("An", result.State.Name);
        Equal(8L, result.State.Revision);
    }

    private static void ActionDispatch()
    {
        var gateway = new FakeGateway(Snapshot(name: "An", revision: 10));
        var ids = new SequenceIds();
        var flow = new FirstPlayableFlow(gateway, ids);

        gateway.Next = Snapshot(name: "An", revision: 11, careerId: "developer");
        flow.AcceptJob("developer");
        AssertLast(gateway, "accept-job/1", 10, CommandKind.AcceptJob, "developer", 0);

        gateway.Next = Snapshot(name: "An", revision: 12, careerId: "developer", activeCourse: true);
        flow.PurchaseCourse("communication-basics");
        AssertLast(gateway, "purchase-course/2", 11, CommandKind.PurchaseCourse, "communication-basics", 0);

        gateway.Next = Snapshot(name: "An", revision: 13, careerId: "developer", activeCourse: true, courseProgress: 600000);
        var study = flow.Study(60);
        AssertLast(gateway, "study/3", 12, CommandKind.Study, "", 60);
        Equal(600000L, study.State.ActiveCourse!.ProgressUnits);

        gateway.Next = Snapshot(name: "An", revision: 14, careerId: "");
        flow.Resign();
        AssertLast(gateway, "resign/4", 13, CommandKind.Resign, "", 0);

        gateway.Next = Snapshot(name: "An", revision: 15);
        flow.AdvanceBoundary();
        AssertLast(gateway, "advance-boundary/5", 14, CommandKind.AdvanceBoundary, "", 0);
    }

    private static void PlaybackDispatch()
    {
        var gateway = new FakeGateway(Snapshot(name: "An", revision: 20, activity: "run-a/activity/2026-09-01/540", playback: 0));
        gateway.Next = Snapshot(name: "An", revision: 21, activity: "run-a/activity/2026-09-01/540", playback: 10);
        var flow = new FirstPlayableFlow(gateway, new SequenceIds());
        var result = flow.AcknowledgePlayback(10);
        AssertLast(gateway, "acknowledge-playback/1", 20, CommandKind.AcknowledgePlayback, "run-a/activity/2026-09-01/540", 10);
        Equal(10, result.State.PlaybackCursor);
        True(result.State.HasPendingPlayback);
    }

    private static void AdvanceDispatch()
    {
        var gateway = new FakeGateway(Snapshot(name: "An", revision: 30, minute: 600));
        var flow = new FirstPlayableFlow(gateway, new SequenceIds());

        gateway.Next = Snapshot(name: "An", revision: 35, minute: 1320);
        gateway.NextAdvance = new AdvanceResult(Array.Empty<CommandResult>(), new SimInstant(new SimDate(2026, 9, 1), 1320), "TargetReached");
        var day = flow.AdvanceDay();
        Equal("AdvanceDay", gateway.LastDispatch);
        AssertLast(gateway, "advance-day/1", 30, CommandKind.AdvanceBoundary, "", 0);
        Equal(1320, day.State.Instant.Minute);
        Equal(1320, day.Advance.ReachedInstant.Minute);

        gateway.Next = Snapshot(name: "An", revision: 50, date: "2026-10-01", minute: 480);
        gateway.NextAdvance = new AdvanceResult(Array.Empty<CommandResult>(), new SimInstant(new SimDate(2026, 10, 1), 480), "TargetReached");
        var month = flow.AdvanceMonth();
        Equal("AdvanceMonth", gateway.LastDispatch);
        AssertLast(gateway, "advance-month/2", 35, CommandKind.AdvanceBoundary, "", 0);
        Equal(10, month.State.Instant.Date.Month);
    }

    private static void ActiveCourseProjection()
    {
        var gateway = new FakeGateway(Snapshot(name: "An", revision: 5, activeCourse: true, courseProgress: 1200000));
        var state = new FirstPlayableFlow(gateway, new SequenceIds()).Refresh();
        True(state.CanStudy); True(state.ActiveCourse != null);
        Equal("course/1", state.ActiveCourse!.InstanceId);
        Equal("communication-basics", state.ActiveCourse.DefinitionId);
        Equal("communication", state.ActiveCourse.SkillId);
        Equal(1200000L, state.ActiveCourse.ProgressUnits);
        Equal(3600000L, state.ActiveCourse.TargetUnits);
        Equal(1, state.ActiveCourse.TargetLevel);
    }

    private static void EmptyCommandIdRejected()
    {
        var gateway = new FakeGateway(Snapshot(name: "An", revision: 1));
        var flow = new FirstPlayableFlow(gateway, new EmptyIds());
        ThrowsInvalidOperation(() => flow.AdvanceBoundary());
        True(gateway.LastEnvelope == null);
    }

    private static void CurrentRunId()
    {
        var gateway = new FakeGateway(Snapshot("An", 3));
        var flow = new FirstPlayableFlow(gateway, new SequenceIds());
        gateway.Next = Snapshot("An", 8, runId: "run-reloaded");
        flow.AdvanceBoundary();
        flow.AcceptJob("developer");
        Equal("run-reloaded", gateway.LastEnvelope!.RunId);
        Equal(8L, gateway.LastEnvelope.ExpectedRevision);
    }

    private static void EmptyBatchIdsRejected()
    {
        var gateway = new FakeGateway(Snapshot("An", 1));
        var flow = new FirstPlayableFlow(gateway, new EmptyIds());
        ThrowsInvalidOperation(() => flow.CreateCharacter("An", 25, "fresh", "base.female"));
        ThrowsInvalidOperation(() => flow.AdvanceDay());
        ThrowsInvalidOperation(() => flow.AdvanceMonth());
        True(gateway.LastEnvelope == null);
        Equal("", gateway.LastDispatch);
    }

    private static void BackwardPlaybackRejected()
    {
        var fixture = new SessionFixture();
        var flow = new FirstPlayableFlow(fixture.Session, new SequenceIds());
        Equal(CommandStatus.Committed, flow.CreateCharacter("An", 25, "fresh", "base.female").Command.Status);
        Equal(CommandStatus.Committed, flow.AcknowledgePlayback(10).Command.Status);
        var bytes = fixture.Session.ExportCheckpoint();
        var writes = fixture.Store.Writes;
        var result = flow.AcknowledgePlayback(9);
        Equal(CommandStatus.Rejected, result.Command.Status);
        Equal("playback.stale", result.Command.ReasonKey);
        True(result.Command.Outcome == null);
        Equal(10, result.State.PlaybackCursor);
        True(bytes.SequenceEqual(fixture.Session.ExportCheckpoint()));
        Equal(writes, fixture.Store.Writes);
    }

    private static void HistoricalCourseRetry()
    {
        var fixture = new SessionFixture();
        var ids = new RepeatIds();
        var flow = new FirstPlayableFlow(fixture.Session, ids);
        flow.CreateCharacter("An", 25, "fresh", "base.female");
        var purchase = flow.PurchaseCourse("communication-basics").Command;
        Equal(CommandStatus.Committed, purchase.Status);
        flow.AdvanceBoundary();
        flow.Study(60);
        var bytes = fixture.Session.ExportCheckpoint();
        var writes = fixture.Store.Writes;
        var repeated = flow.PurchaseCourse("communication-basics");
        Equal(CommandStatus.AlreadyCommitted, repeated.Command.Status);
        Equal(JsonSerializer.Serialize(purchase.Outcome), JsonSerializer.Serialize(repeated.Command.Outcome));
        Equal(600000L, repeated.State.ActiveCourse!.ProgressUnits);
        True(GameSession.TryRestore(fixture.Content, fixture.Serializer, fixture.Store, out var restored, out var load));
        Equal(LoadStatus.Valid, load.Status);
        var cold = new FirstPlayableFlow(restored!, ids).PurchaseCourse("communication-basics");
        Equal(CommandStatus.AlreadyCommitted, cold.Command.Status);
        Equal(JsonSerializer.Serialize(purchase.Outcome), JsonSerializer.Serialize(cold.Command.Outcome));
        Equal(600000L, cold.State.ActiveCourse!.ProgressUnits);
        Equal(writes, fixture.Store.Writes);
        True(bytes.SequenceEqual(fixture.Session.ExportCheckpoint()));
        True(bytes.SequenceEqual(restored!.ExportCheckpoint()));
    }

    private static void AssertLast(FakeGateway gateway, string id, long revision, CommandKind kind, string contentId, int amount)
    {
        True(gateway.LastEnvelope != null);
        Equal("run-a", gateway.LastEnvelope!.RunId);
        Equal(id, gateway.LastEnvelope.CommandId);
        Equal(revision, gateway.LastEnvelope.ExpectedRevision);
        Equal(kind, gateway.LastEnvelope.Command.Kind);
        Equal(contentId, gateway.LastEnvelope.Command.ContentId);
        Equal(amount, gateway.LastEnvelope.Command.Amount);
    }

    private static GameSnapshot Snapshot(string name, long revision, string date = "2026-09-01", int minute = 480,
        string careerId = "", bool activeCourse = false, long courseProgress = 0, string activity = "", int playback = 0,
        string[]? history = null, string runId = "run-a")
    {
        var state = new GameState
        {
            ContentVersion = "fixture.v1",
            RunId = runId,
            Revision = revision,
            DateIso = date,
            Minute = minute,
            Name = name,
            StartingAge = name.Length == 0 ? 0 : 25,
            BirthDateIso = name.Length == 0 ? "" : "2001-09-01",
            BackgroundId = name.Length == 0 ? "" : "fresh",
            AppearanceId = name.Length == 0 ? "" : "base.female",
            LearningSpeed = name.Length == 0 ? 0 : 10000,
            Cash = name.Length == 0 ? 0 : 1000000,
            CurrentActivity = activity,
            CurrentCue = activity.Length == 0 ? "" : "scene.coding",
            PlaybackCursor = playback,
            History = history?.ToList() ?? new List<string>(),
            Skills = name.Length == 0 ? new List<SkillState>() : new List<SkillState> { new SkillState { Id = "communication" } }
        };
        if (careerId.Length != 0)
            state.Employment = new EmploymentState { InstanceId = "employment/1", CareerId = careerId, Xp = 40, Rank = 1 };
        if (activeCourse)
            state.Course = new CourseState { InstanceId = "course/1", DefinitionId = "communication-basics", ProgressUnits = courseProgress };

        return new GameSnapshot(state, Catalog());
    }

    private static ContentCatalog Catalog()
    {
        var skill = new SkillDefinition("communication", "skill.communication", 100, 300, 600, 1000, 1500);
        var course = new CourseDefinition("communication-basics", "course.communication", "communication", 1, 0, 360, 200000);
        return new ContentCatalog("fixture.v1", new[] { skill }, Array.Empty<CareerDefinition>(), new[] { course },
            new[] { new CharacterStartDefinition("fresh", 1000000, 10000, "base.female") },
            new EconomyBalanceDefinition(5, 1, 500000), new DayScheduleDefinition(480, 1320));
    }

    private sealed class SequenceIds : ICommandIdSource
    {
        private int value;
        public string Next(string action) { value++; return action + "/" + value; }
    }

    private sealed class EmptyIds : ICommandIdSource
    {
        public string Next(string action) => "";
    }

    private sealed class RepeatIds : ICommandIdSource
    {
        public string Next(string action) => action + "/original";
    }

    private sealed class SessionFixture
    {
        public ContentCatalog Content { get; } = Catalog();
        public JsonSaveSerializer Serializer { get; }
        public MemoryStore Store { get; }
        public GameSession Session { get; }
        public SessionFixture()
        {
            Serializer = new JsonSaveSerializer(Content, GameSession.CreateRestoreValidator());
            Store = new MemoryStore(Serializer);
            Session = new GameSession(GameSession.NewState(Content, "run-real", 937, new SimDate(2026, 9, 1)), Content, Serializer, Store);
        }
    }

    private sealed class MemoryStore : ISaveStore
    {
        private readonly ISaveSerializer serializer;
        private byte[]? checkpoint;
        public int Writes { get; private set; }
        public MemoryStore(ISaveSerializer value) { serializer = value; }
        public LoadResult Read() => checkpoint == null ? new LoadResult(LoadStatus.Missing) : serializer.DeserializeAndValidate(checkpoint);
        public WriteStatus Commit(byte[] bytes, long revision)
        {
            var load = serializer.DeserializeAndValidate(bytes);
            if (load.Status != LoadStatus.Valid || load.State!.Revision != revision + 1 || (Read().State?.Revision ?? 0) != revision)
                return WriteStatus.Failed;
            checkpoint = (byte[])bytes.Clone();
            Writes++;
            return WriteStatus.Committed;
        }
    }

    private sealed class FakeGateway : IGameCommands
    {
        public GameSnapshot Current { get; private set; }
        public GameSnapshot? Next { private get; set; }
        public AdvanceResult? NextAdvance { private get; set; }
        public CommandEnvelope? LastEnvelope { get; private set; }
        public string LastDispatch { get; private set; } = "";

        public FakeGateway(GameSnapshot current) { Current = current; }

        public CommandResult Execute(CommandEnvelope command)
        {
            LastDispatch = "Execute"; LastEnvelope = command;
            if (Next != null) { Current = Next; Next = null; }
            return new CommandResult(CommandStatus.Committed, "", Current.Revision, "op");
        }

        public AdvanceResult AdvanceDay(CommandEnvelope request)
        {
            LastDispatch = "AdvanceDay"; LastEnvelope = request;
            if (Next != null) { Current = Next; Next = null; }
            var result = NextAdvance ?? new AdvanceResult(Array.Empty<CommandResult>(), Current.Instant, "TargetReached");
            NextAdvance = null; return result;
        }

        public AdvanceResult AdvanceMonth(CommandEnvelope request)
        {
            LastDispatch = "AdvanceMonth"; LastEnvelope = request;
            if (Next != null) { Current = Next; Next = null; }
            var result = NextAdvance ?? new AdvanceResult(Array.Empty<CommandResult>(), Current.Instant, "TargetReached");
            NextAdvance = null; return result;
        }

        public GameSnapshot Snapshot() => Current;
    }

    private static void Check(string name, Action action)
    {
        try { action(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { failures.Add(name + ": " + error.Message); Console.WriteLine("FAIL " + name); }
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception("Expected " + expected + ", got " + actual);
    }

    private static void True(bool value)
    {
        if (!value) throw new Exception("Expected true.");
    }

    private static void ThrowsNotSupported(Action action)
    {
        try { action(); } catch (NotSupportedException) { return; }
        throw new Exception("Expected NotSupportedException.");
    }

    private static void ThrowsInvalidOperation(Action action)
    {
        try { action(); } catch (InvalidOperationException) { return; }
        throw new Exception("Expected InvalidOperationException.");
    }
}
