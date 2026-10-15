#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;
using StartupLife.Presentation;
using UnityEngine;

namespace StartupLife.Tests.EditMode
{
    public sealed class CharacterVisualAssemblerTests
    {
        private sealed class FakeApprovedSprites : IApprovedCharacterSpriteResolver
        {
            private readonly Dictionary<string, Sprite> sprites;
            public FakeApprovedSprites(Dictionary<string, Sprite> sprites) { this.sprites = sprites; }
            public bool TryResolveApprovedSprite(string id, out Sprite sprite) =>
                sprites.TryGetValue(id, out sprite!);
        }

        private static readonly CharacterVisualSlot[] Slots =
        {
            CharacterVisualSlot.Appearance, CharacterVisualSlot.Face,
            CharacterVisualSlot.Hair, CharacterVisualSlot.HairColor,
            CharacterVisualSlot.SkinTone, CharacterVisualSlot.Accessory,
            CharacterVisualSlot.Outfit, CharacterVisualSlot.Trait
        };

        private static CharacterLookCatalog LookCatalog()
        {
            var choices = new[] {
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
            var defaults = new Dictionary<CharacterVisualSlot, string> {
                [Slots[0]] = "base.female", [Slots[1]] = "face.soft",
                [Slots[2]] = "hair.short", [Slots[3]] = "haircolor.ink",
                [Slots[4]] = "skin.warm", [Slots[5]] = "accessory.none",
                [Slots[6]] = "outfit.casual", [Slots[7]] = "trait.balanced"
            };
            return new CharacterLookCatalog(choices, defaults);
        }

        private static CharacterVisualComposer Composer(CharacterLookCatalog looks)
        {
            var wardrobe = new CharacterWardrobeResolver(looks, new[] {
                new CharacterWardrobeRule("developer", CharacterSceneContext.Work, "outfit.office")
            });
            var layers = new[] {
                new CharacterVisualLayer(CharacterVisualSlot.Face, "face.soft",
                    CharacterRigView.Front, "face", "fixture.face", 13),
                new CharacterVisualLayer(CharacterVisualSlot.Hair, "hair.short",
                    CharacterRigView.Front, "hair_front", "fixture.hair", 14),
                new CharacterVisualLayer(CharacterVisualSlot.Outfit, "outfit.casual",
                    CharacterRigView.Front, "outfit_torso", "fixture.casual", 15),
                new CharacterVisualLayer(CharacterVisualSlot.Outfit, "outfit.office",
                    CharacterRigView.Front, "outfit_torso", "fixture.office", 15)
            };
            return new CharacterVisualComposer(looks, wardrobe, layers,
                new[] { "fixture.face", "fixture.hair", "fixture.casual", "fixture.office" });
        }

        private sealed class Rig : IDisposable
        {
            public readonly GameObject Root = new GameObject("ReusableCharacterRig");
            public readonly CharacterVisualAssembler Assembler;
            public readonly Dictionary<string, SpriteRenderer> Renderers =
                new Dictionary<string, SpriteRenderer>(StringComparer.Ordinal);
            public readonly Dictionary<string, Sprite> Sprites =
                new Dictionary<string, Sprite>(StringComparer.Ordinal);
            private readonly List<Texture2D> textures = new List<Texture2D>();

            public Rig(bool hasOffice = true)
            {
                foreach (var name in new[] { "face", "hair_front", "outfit_torso" })
                {
                    var obj = new GameObject(name, typeof(SpriteRenderer));
                    obj.transform.SetParent(Root.transform, false);
                    Renderers.Add(name, obj.GetComponent<SpriteRenderer>());
                }
                foreach (var id in new[] { "fixture.face", "fixture.hair",
                             "fixture.casual", "fixture.office" })
                {
                    if (id == "fixture.office" && !hasOffice) continue;
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    texture.filterMode = FilterMode.Point;
                    texture.Apply(false, false);
                    textures.Add(texture);
                    Sprites.Add(id, Sprite.Create(texture, new Rect(0, 0, 2, 2),
                        new Vector2(.5f, .5f), 32f));
                }
                Assembler = Root.AddComponent<CharacterVisualAssembler>();
                Assembler.Configure(new FakeApprovedSprites(Sprites),
                    new[] {
                        new CharacterRigPartBinding("face", Renderers["face"]),
                        new CharacterRigPartBinding("hair_front", Renderers["hair_front"]),
                        new CharacterRigPartBinding("outfit_torso", Renderers["outfit_torso"])
                    }, 32);
            }
            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(Root);
                foreach (var sprite in Sprites.Values)
                    UnityEngine.Object.DestroyImmediate(sprite);
                foreach (var tex in textures)
                    UnityEngine.Object.DestroyImmediate(tex);
            }
        }

        [Test]
        public void WorkOutfitSwapsOnlyOneRendererWithoutRecreatingRig()
        {
            using (var rig = new Rig())
            {
                var look = LookCatalog();
                var composer = Composer(look);
                var draft = look.NewDraft();
                var casual = composer.Compose(draft, "", CharacterSceneContext.Home,
                    CharacterRigView.Front);
                var work = composer.Compose(draft, "developer", CharacterSceneContext.Work,
                    CharacterRigView.Front);
                Assert.That(rig.Assembler.Apply(casual), Is.True);
                var face = rig.Renderers["face"];
                var faceSprite = face.sprite;
                var rootCount = rig.Root.transform.childCount;
                Assert.That(rig.Assembler.Apply(work), Is.True);
                Assert.That(rig.Renderers["outfit_torso"].sprite,
                    Is.SameAs(rig.Sprites["fixture.office"]));
                Assert.That(face.sprite, Is.SameAs(faceSprite));
                Assert.That(rig.Root.transform.childCount, Is.EqualTo(rootCount));
                Assert.That(rig.Assembler.Apply(work), Is.False,
                    "Identical apply should not dirty any SpriteRenderer.");
                Assert.That(rig.Assembler.Apply(casual), Is.True);
                Assert.That(rig.Renderers["outfit_torso"].sprite,
                    Is.SameAs(rig.Sprites["fixture.casual"]));
                Assert.That(rig.Assembler.PartCount, Is.EqualTo(3));
            }
        }

        [Test]
        public void MissingSpriteFailsBeforeAnyLayerChanges()
        {
            using (var rig = new Rig(hasOffice: false))
            {
                var catalog = LookCatalog();
                var composer = Composer(catalog);
                var draft = catalog.NewDraft();
                Assert.That(rig.Assembler.Apply(composer.Compose(draft, null,
                    CharacterSceneContext.Home, CharacterRigView.Front)), Is.True);
                var original = rig.Renderers["outfit_torso"].sprite;
                Assert.Throws<InvalidOperationException>(() => rig.Assembler.Apply(
                    composer.Compose(draft, "developer",
                        CharacterSceneContext.Work, CharacterRigView.Front)));
                Assert.That(rig.Renderers["outfit_torso"].sprite, Is.SameAs(original));
                Assert.That(rig.Renderers["hair_front"].enabled, Is.True);
                Assert.That(rig.Renderers["face"].enabled, Is.True);
            }
        }

        [Test]
        public void ConfigureRejectsExternalOrDuplicateRendererAndDoesNotTouchRig()
        {
            var host = new GameObject("rig", typeof(CharacterVisualAssembler));
            var member = new GameObject("part", typeof(SpriteRenderer));
            var outsider = new GameObject("outsider", typeof(SpriteRenderer));
            member.transform.SetParent(host.transform, false);
            try
            {
                var target = host.GetComponent<CharacterVisualAssembler>();
                var fake = new FakeApprovedSprites(new Dictionary<string, Sprite>());
                var good = new CharacterRigPartBinding("face", member.GetComponent<SpriteRenderer>());
                Assert.Throws<ArgumentException>(() => target.Configure(fake,
                    new[] { good, new CharacterRigPartBinding("another", member.GetComponent<SpriteRenderer>()) }, 32));
                Assert.Throws<ArgumentException>(() => target.Configure(fake,
                    new[] { new CharacterRigPartBinding("face", outsider.GetComponent<SpriteRenderer>()) }, 32));
                Assert.That(target.PartCount, Is.EqualTo(0));
                Assert.That(member.GetComponent<SpriteRenderer>().enabled, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(outsider);
            }
        }

        [Test]
        public void ClearDisablesOnlySpriteSlotsButPreservesJointTransforms()
        {
            using (var rig = new Rig())
            {
                var before = rig.Renderers["hair_front"].transform;
                var pivotPosition = new Vector3(.25f, .5f, 0f);
                before.localPosition = pivotPosition;
                var catalog = LookCatalog();
                rig.Assembler.Apply(Composer(catalog).Compose(catalog.NewDraft(), null,
                    CharacterSceneContext.Home, CharacterRigView.Front));
                rig.Assembler.Clear();
                Assert.That(rig.Renderers["hair_front"].enabled, Is.False);
                Assert.That(rig.Renderers["hair_front"].sprite, Is.Null);
                Assert.That(before.localPosition, Is.EqualTo(pivotPosition));
                Assert.That(rig.Root.transform.childCount, Is.EqualTo(3));
            }
        }
    }
}
