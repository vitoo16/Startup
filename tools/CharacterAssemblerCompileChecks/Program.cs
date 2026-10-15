#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Presentation;
using UnityEngine;

internal static class Program
{
    private sealed class SpriteSource : IApprovedCharacterSpriteResolver
    {
        private readonly Dictionary<string, Sprite> entries;
        internal SpriteSource(Dictionary<string, Sprite> map) { entries = map; }
        public bool TryResolveApprovedSprite(string id, out Sprite sprite)
        {
            if (entries.TryGetValue(id, out var found))
            {
                sprite = found;
                return true;
            }
            sprite = null!;
            return false;
        }
    }

    private sealed class Fixture
    {
        internal readonly CharacterLookCatalog Looks;
        internal readonly CharacterVisualComposer Composer;
        internal readonly CharacterVisualAssembler Assembler;
        internal readonly SpriteRenderer Face;
        internal readonly SpriteRenderer Hair;
        internal readonly SpriteRenderer Clothes;
        internal readonly Sprite HomeSprite;
        internal readonly Sprite WorkSprite;

        internal Fixture(bool includeOffice = true, bool badHairImport = false)
        {
            var options = new[]
            {
                new CharacterVisualOption("base.female", CharacterVisualSlot.Appearance),
                new CharacterVisualOption("base.male", CharacterVisualSlot.Appearance),
                new CharacterVisualOption("face.soft", CharacterVisualSlot.Face),
                new CharacterVisualOption("hair.short", CharacterVisualSlot.Hair),
                new CharacterVisualOption("haircolor.ink", CharacterVisualSlot.HairColor),
                new CharacterVisualOption("skin.warm", CharacterVisualSlot.SkinTone),
                new CharacterVisualOption("accessory.none", CharacterVisualSlot.Accessory),
                new CharacterVisualOption("outfit.casual", CharacterVisualSlot.Outfit),
                new CharacterVisualOption("outfit.office", CharacterVisualSlot.Outfit),
                new CharacterVisualOption("trait.balanced", CharacterVisualSlot.Trait)
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
            Looks = new CharacterLookCatalog(options, defaults);
            var outfits = new CharacterWardrobeResolver(Looks, new[]
            {
                new CharacterWardrobeRule("developer", CharacterSceneContext.Work, "outfit.office")
            });
            Composer = new CharacterVisualComposer(Looks, outfits,
                new[]
                {
                    new CharacterVisualLayer(CharacterVisualSlot.Face, "face.soft",
                        CharacterRigView.Front, "face", "approved.face", 13),
                    new CharacterVisualLayer(CharacterVisualSlot.Hair, "hair.short",
                        CharacterRigView.Front, "hair_front", "approved.hair", 14),
                    new CharacterVisualLayer(CharacterVisualSlot.Outfit, "outfit.casual",
                        CharacterRigView.Front, "outfit_torso", "approved.home", 15),
                    new CharacterVisualLayer(CharacterVisualSlot.Outfit, "outfit.office",
                        CharacterRigView.Front, "outfit_torso", "approved.work", 15)
                },
                new[] { "approved.face", "approved.hair", "approved.home", "approved.work" });

            var root = new Transform();
            Face = new SpriteRenderer { transform = new Transform { Parent = root } };
            Hair = new SpriteRenderer { transform = new Transform { Parent = root } };
            Clothes = new SpriteRenderer { transform = new Transform { Parent = root } };
            HomeSprite = new Sprite();
            WorkSprite = new Sprite();
            var hairSprite = new Sprite();
            if (badHairImport) hairSprite.texture.filterMode = FilterMode.Bilinear;
            var map = new Dictionary<string, Sprite>
            {
                ["approved.face"] = new Sprite(),
                ["approved.hair"] = hairSprite,
                ["approved.home"] = HomeSprite
            };
            if (includeOffice) map.Add("approved.work", WorkSprite);

            Assembler = new CharacterVisualAssembler { transform = root };
            Assembler.Configure(new SpriteSource(map),
                new[] {
                    new CharacterRigPartBinding("face", Face),
                    new CharacterRigPartBinding("hair_front", Hair),
                    new CharacterRigPartBinding("outfit_torso", Clothes)
                }, 32);
        }

        internal CharacterVisualPlan Home() =>
            Composer.Compose(Looks.NewDraft(), null, CharacterSceneContext.Home, CharacterRigView.Front);
        internal CharacterVisualPlan Work() =>
            Composer.Compose(Looks.NewDraft(), "developer", CharacterSceneContext.Work, CharacterRigView.Front);
    }

    private static void Assert(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }

    private static void Main()
    {
        int count = 0;
        void Case(string name, Action action)
        {
            action();
            count++;
            Console.WriteLine("PASS engine-free simulated Unity surface: " + name);
        }

        Case("reuse existing renderer instances and preserve sort", () =>
        {
            var f = new Fixture();
            var owned = new[] { f.Face, f.Hair, f.Clothes };
            Assert(f.Assembler.Apply(f.Home()), "Home should render");
            Assert(ReferenceEquals(f.Clothes.sprite, f.HomeSprite), "Wrong home sprite");
            Assert(f.Assembler.Apply(f.Work()), "Work outfit should differ");
            Assert(ReferenceEquals(f.Clothes.sprite, f.WorkSprite), "Wrong work sprite");
            Assert(f.Face.enabled && f.Hair.enabled && f.Clothes.enabled, "Missing actor part");
            Assert(f.Face.sortingOrder == 13 && f.Hair.sortingOrder == 14 &&
                   f.Clothes.sortingOrder == 15, "Wrong layer order");
            Assert(ReferenceEquals(owned[0], f.Face) &&
                   ReferenceEquals(owned[1], f.Hair) &&
                   ReferenceEquals(owned[2], f.Clothes), "Renderer unexpectedly changed");
            Assert(f.Assembler.Apply(f.Work()) == false, "Identical apply must be a no-op");
        });
        Case("missing approved sprite causes no partial work uniform", () =>
        {
            var f = new Fixture(includeOffice: false);
            f.Assembler.Apply(f.Home());
            var old = f.Clothes.sprite;
            Throws<InvalidOperationException>(() => f.Assembler.Apply(f.Work()));
            Assert(ReferenceEquals(f.Clothes.sprite, old) &&
                   f.Face.enabled && f.Hair.enabled, "Actor partially updated on rejected plan");
        });
        Case("bad pixel import rejected before modifying slots", () =>
        {
            var f = new Fixture(badHairImport: true);
            Throws<InvalidOperationException>(() => f.Assembler.Apply(f.Home()));
            Assert(!f.Face.enabled && !f.Hair.enabled && !f.Clothes.enabled,
                "Partially rendered failed pixel import");
        });
        Case("clear retains same rig parts", () =>
        {
            var f = new Fixture();
            f.Assembler.Apply(f.Home());
            f.Assembler.Clear();
            Assert(f.Face.sprite == null && f.Hair.sprite == null && f.Clothes.sprite == null,
                "Clear did not remove sprite parts");
            Assert(!f.Face.enabled && !f.Hair.enabled && !f.Clothes.enabled,
                "Clear did not disable sprite slots");
        });
        Case("external and duplicate renderers rejected", () =>
        {
            var root = new Transform();
            var local = new SpriteRenderer { transform = new Transform { Parent = root } };
            var external = new SpriteRenderer { transform = new Transform() };
            var empty = new SpriteSource(new Dictionary<string, Sprite>());
            var a = new CharacterVisualAssembler { transform = root };
            Throws<ArgumentException>(() => a.Configure(empty, new[] {
                new CharacterRigPartBinding("face", external)
            }, 32));
            Throws<ArgumentException>(() => a.Configure(empty, new[] {
                new CharacterRigPartBinding("face", local),
                new CharacterRigPartBinding("hair", local)
            }, 32));
            Assert(a.PartCount == 0 && local.enabled, "Failed binding mutated rig");
        });
        Console.WriteLine("PASS " + count + " character assembler C# tests. " +
            "Unity Editor/device MUST be verified separately.");
    }
}
