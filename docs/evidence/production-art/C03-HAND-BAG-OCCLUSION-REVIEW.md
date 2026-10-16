# C03 — Hand behind bag: owner occlusion correction

**Date:** 2026-10-16  
**Character:** Sáng Ngõ Nhỏ (C03), single FRONT view  
**Owner instruction:** Keep the chosen cute character; change ONLY how the near-side hand is hidden by the crossbody satchel.  
**Current review source:** `pixel_art_boy_with_brown_satchel.png`, SHA-256 `d2e7e9a79672e23408c0a48626ef3968af84f5564f44e14eac41e4add38cb25e`, 1171×1343 RGB PNG.  
**Status:** `OWNER_REQUESTED_HAND_BAG_OCCLUSION_CONCEPT_RECORDED_BINARY_PENDING`.

## Non-negotiable visual redline

The image-right hand adjacent to the bag is **behind** the bag, not grasping
or resting on its front. The bag body and flap should visibly cover the upper
portion of that hand. Only a small, natural section of skin may show **below**
the lower edge of the bag. The outline of the bag must remain uninterrupted
where it passes in front of the hand; no thumb or fingers should be painted
over the front flap. The diagonal strap remains connected at the top of the
bag. The other hand remains free and unchanged.

Keep everything else from the chosen character: fringe and face, friendly
smile, green jacket, cream shirt, blue-grey jeans, chunky shoes, bag color,
body shape, casual stance, and front-only framing. This is an **occlusion
correction**, not a redesigned outfit, pose, mascot, or art style.

### Layering for a future true 21-part native source

Author the covered hand's pixels *on its own* `hand_r` or `hand_l`
semantic layer as dictated by actual side assignment. Composite the localized
`accessory_front` bag body/flap **in front of** that hand in the overlap
region; the hand remains in the rig but cannot be rendered on top of the bag.
Any small below-bag sliver of exposed skin belongs to the hand layer.
Use precise sorting/order or masking on the affected hand/bag only.
**Do not globally push every hand behind all accessories**; hand-strap
interactions for other contexts have different rules.

Before integrating into Unity, examine the 1× rendered overlapping pixels,
8× nearest-neighbor, cutout component layers, and actual Unity sorting.
Validate at least:
- No exposed palm/thumb/fingertip on the bag front or flap.
- The bag contour cuts across the hand cleanly; only a small hand region below.
- Strap-to-bag junction remains visually continuous.
- Neither hand has disappeared or merged into trousers.
- The edit has not changed face, hair, garment silhouette or other anatomy.

## Accurate asset/provenance status

The latest image in the conversation is **an opaque high-resolution generated
illustration**, not a native 64×80 character sprite. It has a baked light
background, no transparent cutout and no validated 21-layer rig. Treat it as a
**draft visual reference**, never as an approved game asset. Independent
originality/IP, Unity animation and device BeautyCell are unperformed.

**Binary is NOT in Git as of this note.** The connected GitHub text operations
committed the reference record, but could not import the exact PNG bytes.
The expected draft path is:
`Assets/StartupLife/Art/Source/Characters/golden-character-c03/references/C03-hand-behind-bag-reference.png`.
It must remain out of `Assets/StartupLife/Art/Approved/`.

To complete the binary handoff, place the unchanged PNG at this path and run:

```bash
python scripts/Verify-C03HandBagReference.py
```

The script must confirm the exact hash and format; **do not** mark
`exact_binary_committed_to_git=true` until the binary is in a verified Git
commit. Existing owner-selected master remains an immutable historical
reference. This newer concept is not silently granted final owner aesthetic or
commercial shipping approval.

## Session closeout

**Skills:** startup-life-session-orchestrator, vietnam-art-direction,
startup-life-asset-quality-gate, 2d-pixel-perfect, sprite-editor (review only;
live Unity Editor unavailable).  
**Scope:** Owner-specific C03 visual reference and occlusion/sorting contract,
no other variants or systems.  
**Tests:** Local PNG SHA, width/height and RGB type verified; exact Git file
hash check pending actual binary upload.  
**Save impact:** None. **Approved registry:** untouched.  
**Open:** PNG binary upload, native-grid re-authoring/rigging, IP, Unity, device.
