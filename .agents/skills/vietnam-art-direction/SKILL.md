---
name: vietnam-art-direction
description: Use whenever creating, generating, reviewing or integrating Startup Life visuals, environments, props, clothing, food, signage, localized UI, business scenes or narrative details intended to feel Vietnamese. Prevents generic "Asian" AI art and stereotype-heavy localization.
---

# Vietnam Art Direction — Startup Life

## Goal
Create a contemporary Vietnamese everyday-life identity that feels observed and specific, while remaining cute, clean and internationally readable.

## Core rule
Vietnamese identity comes from:
- spatial details;
- objects;
- language;
- commerce;
- routines;
- mobility;
- social behavior.

It does not come from filling every frame with iconic symbols.

## Required scene process

For every scene, write a small locality contract:

```yaml
scene:
  purpose:
  socioeconomic_context:
  environment_type:
  local_anchors:
    - ...
    - ...
  prohibited_stereotypes:
    - ...
  text_strategy:
  palette_notes:
```

Use 2–4 local anchors per scene.

## Good anchor categories
- motorbike/scooter context;
- helmets;
- Vietnamese storefront proportions;
- QR payment stand;
- delivery pickup/packing;
- café furniture appropriate to the venue;
- familiar drink/food service objects;
- air-conditioner/fan details;
- urban shop-house cues where appropriate;
- contemporary Vietnamese workwear/casualwear;
- Vietnamese-language interface or signage applied correctly.

## Do not overuse
- nón lá;
- national flags;
- lanterns;
- old-town aesthetic;
- rice paddies;
- every famous Vietnamese dish.

Use these only when the actual scene calls for them.

## Vietnamese text
Do not accept AI-garbled Vietnamese.

For final production:
- generate background signboards blank;
- apply correct text through TMP/UI or controlled authored texture;
- verify full diacritics;
- verify wording sounds natural.

Test string:
`ă â đ ê ô ơ ư Á À Ả Ã Ạ ấ ề ộ ớ ự`

## Character clothing
Use contemporary clothing first.
Career outfit communicates profession without costume exaggeration.

Examples:
- developer/office: casual-smart, polo, tee, shirt, cardigan;
- marketing/sales: smart casual;
- F&B: practical uniform/apron;
- founder/night: casual home/work clothing.

## Business environments
A Vietnamese café/kiosk should be locally plausible without becoming documentary realism.
Keep:
- clean chibi proportions;
- simplified shapes;
- visual hierarchy suitable for mobile;
- a few recognizable local cues.

## Image-generation prompts
Prompts must state:
- contemporary Vietnam;
- specific scene function;
- Golden Asset visual style;
- camera angle;
- palette;
- local anchors;
- explicitly forbidden stereotype cluster;
- no readable text unless text is being produced separately.

## Review questions
1. Could this exact asset be dropped into a generic Korean/Japanese/US mobile game unchanged?
   - If yes, it may be insufficiently localized.
2. Are local details natural or performative?
3. Is any Vietnamese text malformed?
4. Did the scene add too many icons at once?
5. Does it still match the project's Golden Assets?
