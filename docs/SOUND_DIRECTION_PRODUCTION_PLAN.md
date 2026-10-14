# Startup Life — Sound Direction & Audio Production Contract
**Status:** ACTIVE planning and approval policy; no audio asset is approved by this document. **Date:** 2026-10-15. **Owner:** GPT-6 Sound Director / Technical Director. **Release:** production-first iOS/Android, portrait-first. **Precedence:** AGENTS.md, PRODUCTION_EXECUTION_ROADMAP.md, PRODUCTION_ASSET_IP_POLICY.md, GDD; this contract governs audio. No silent change to gameplay or prior approvals.

## 1. Sonic identity
**Working identity:** Tiny Vietnamese Everyday Life — warm, intimate, handcrafted, playful, unmistakably grounded in modern everyday Vietnam. Reinforce a small, original pixel world and the arc from employment to entrepreneurship, without mocking accents, regional communities, occupations or disabilities. Avoid imitating any recognizable game, artist, soundtrack, sound mark, UI cue, voice or copyrighted recording.

**Five design axes:** warm / handmade / miniature / contemporary Vietnamese / responsive.
- Sounds support actions and emotion; never overwhelm economic information or screen-reader/notification accessibility.
- Local ambience: distant motorbike, fan, tiles/slippers, market movement, rain on sheet metal, doors, kitchen utensils. Only record with location/participant permission and avoid identifiable private voices.
- Composed music: original melodic cells and harmonic choices; optional self-recorded instruments inspired by the physical timbres of đàn tranh, sáo trúc, đàn bầu, blended with original toy percussion and soft synthesis. Instruments and cultural context are not exclusive IP. Do not sample existing performances without suitable clearance.
- Character vocalizations: newly recorded/synthesized nonlexical micro-phrases, respectful and sufficiently distinct from existing character-speech systems; avoid recognizable celebrity/child likeness without consent.
- Financial feedback: understated tactile receipts, paper, desk-object transients and rewarding musical punctuation, never casino/slot-machine arpeggios.

## 2. Deliverables and measurable targets
All counts below are **planning budgets, not completion claims or proof of quality**. Tune after representative vertical-slice coverage. Track both unique source recordings and exported variants so inflated duplicated variants do not count as distinct authored content.

| Domain | Production target | Minimum coverage |
| --- | ---: | --- |
| UI/navigation/accessibility | 80 SFX variants | select, confirm, cancel, disabled/error, modal, screen change, alert with quiet-mode alternative |
| Character/foley/vocal | 120 | walk per surface, idle, gestures, talking, fatigue, celebration; animation event sync |
| Vietnam environments/weather | 150 | morning/day/evening/night, rain, indoor/outdoor, scene transitions |
| Business/career interactions | 200 | coffee, food, retail, office, customer, tools, transaction, employment scenes as actually scoped |
| Economy/progression | 50 | salary, cost, income, purchase, investment, failure, expansion |
| Narrative/events | 100 | decision states, successes, setbacks, milestones |
| **Initial curated SFX pool** | **700** | coverage audited by named gameplay events, not indiscriminate playback |
| Music | **8–12 original cue families** | menu/home/town/career/business/study/weather/tension/recovery/milestones; stems and transitions where useful |

Long-term 2,000–3,000 exports is a conditional capacity ceiling, **not** a release requirement. Never prioritize asset count over context, clarity, legal provenance or performance.

## 3. Implementation architecture
Unity 6.3 audio integration behind a small interface (e.g. `IAudioService`), event-driven and data-addressed: `AudioEventCatalog` maps stable semantic IDs to clip variants, weights, bus, spatial rules, priorities, and cooldowns. Avoid hardcoded clip names in gameplay, duplicate managers, or attaching hundreds of always-playing AudioSources.

Preferred initial stack: **native Unity AudioSource + AudioMixer** to minimize runtime/plugin dependency. FMOD/Wwise require a separately approved ADR demonstrating feature value, licensing and mobile cost. Proposed buses: Master, Music, Ambience, Foley, UI, Character, Business, Notifications; use snapshots/ducking and user-controlled independent sliders. No unverified mixer edits; use a live Unity Editor for actual mixer changes.

Adaptive music: layered original stems with bar-aligned changes for life/day/weather/business intensity, crossfades, no surprise loudness jumps; calm state preserved during long sessions. Character foley uses animation events for grounded foot contact and actions, not unrelated step timers. Randomization must be perceptually safe and deterministic where replay/save contract requires; sound is generally presentation-only and must not mutate simulation state.

## 4. Audio asset pipeline / legal clearance
1. Brief with event ID, emotional purpose, Vietnamese context, duration, frequency, priority and listening-device target.
2. Clean-room original recording, original composition/synthesis, or documented licensed/commissioned source. Prefer first-party assets. No extracted YouTube/game/movie audio, ripped music, cloned recognizable voices, unlicensed sample packs, copied motifs, or prompting to sound like a named soundtrack.
3. Record rights chain **per recording, performance, voice, composition, sample, plugin and AI generation**. AI commercial-use terms and human authorship/ownership uncertainty must be recorded; no unsupported claim of exclusive rights or global uniqueness.
4. Produce minimum three materially distinct original candidates for high-impact cues (theme, UI signature, player voice, milestone). Document differences rather than pitch-shifting one copied source.
5. Technical QC: peak/clipping, DC offset, unwanted noise, loop seams, phase, loudness, mono-fold, masking and repeat fatigue. Store editable session and uncompressed masters; derive mobile compressed imports.
6. In-engine context demonstration and real-device audition. Independent audio/IP clearance and **explicit owner listening approval** per gate below.
7. Only then enter approved catalog and make audible in shipping builds. Unapproved material may live in isolated audition/test environments, never be silently shipped.

Minimum provenance record: `asset_id`, semantic event IDs, author/recorder, created UTC, source path, composition/recording notes, equipment/DAW/plugins/samples, AI tool+prompt+terms if used, license proof reference, candidate paths and hashes, reviewer refs, audio QC, device audition, owner decision/message and timestamp, import path, approved version/replacement link. Confidential contracts and consent evidence stay off public Git; link to access-controlled evidence only.

## 5. MANDATORY owner review gate — no silent approval
**Non-negotiable:** If the user has not received an explicit review request with an actual playable preview, they must presume the audio has **NOT BEEN PRODUCED/REVIEWED**. If they have received a preview but have not explicitly approved that exact version, it is **NOT APPROVED**. Silence, time elapsed, AI self-critique, passing CI, merging documentation, or another agent's approval **NEVER** counts as owner sign-off.

Statuses:
`NOT_STARTED` → `IN_PROGRESS` → `PREVIEW_READY` → `AWAITING_OWNER_REVIEW` → `APPROVED_BY_OWNER` → `TECH_QA_PASSED` → `RELEASE_CLEARED`.
Additional states: `CHANGES_REQUESTED`, `BLOCKED_NO_PREVIEW`, `RIGHTS_PENDING`, `REJECTED`, `REVOKED`. A valid review submission sets `AWAITING_OWNER_REVIEW`; never auto-advance it.

When to notify the owner:
- before locking initial signature UI sound, main theme, core character voice and representative Vietnamese ambience (Golden sound assets);
- after a representative **in-game** mix/playback of each major sound family (UI, character, city/weather, careers/businesses, economy/events);
- after any audible major revision or new sound identity, on-device mismatch, suspected copyright conflict or fatigue issue;
- at the representative vertical slice, release candidate, and before marketing/store distribution.

Each notification must be **sent as a clear user-facing message in the active conversation or an owner-confirmed issue/review channel**; merely writing a TODO or committing files is not notifying the owner. A GitHub issue may track the review but is not proof the owner saw it. Without a supported outbound mechanism, do not claim notification delivery.

Mandatory review request template:
```text
🔊 AUDIO REVIEW REQUIRED — [gate ID / asset version]
What changed / sound intention:
Playable preview URL or attached file: [real, verified link]
In-game context / 15–60s captured demo: [real link or BLOCKED]
Source/provenance + legal status: [evidence]
Device(s) checked / listening conditions: [real evidence or NOT TESTED]
Please reply: APPROVE [gate ID + version] / REVISE [feedback] / REJECT.
Current status: AWAITING_OWNER_REVIEW (or BLOCKED_NO_PREVIEW).
Next production step blocked by this decision:
```
No fabricated preview links, simulated human feedback, or assumed acknowledgement. For player-specific listening, include both a dry cue and its mix in gameplay where feasible; offer headphone and phone-speaker samples. When owner requests changes, generate a **new version** and repeat the gate. A later change invalidates prior approval for the affected asset/mix.

Approval evidence must include exact IDs/versions, reply/decision reference and timestamp. Approval of one sample does not imply approval of an entire pack. Technical checks and independent IP clearance are separate required gates; owner approval alone is not a legal clearance.

## 6. Audio QA and mobile performance budgets (provisional)
- 100% shipping event IDs must resolve to a cleared asset or deliberate silence fallback, never broken/null asset references.
- Test looping and transitions over >=15 minutes representative play; repeat-fatigue listening test; no obvious clipped/distorted events and no unintended audible discontinuities.
- Test on low/mid Android and an iPhone when available, phone speaker and headphones, quiet and noisy backgrounds; document actual availability. No device means `NOT_TESTED`, never PASS.
- Evaluate sample/load/compression per asset: short UI/foley compressed-in-memory or ADPCM only where profiling supports it, music longer clips streamed where suitable, avoid avoidable stalls/clicks. Do not impose a universal codec without profiling.
- Provisional simultaneous voice budget <=16 total on representative low-tier device; report measured CPU/DSP, memory, latency and dropouts before locking targets. Target sustained audio memory <=40 MiB on low-tier devices, subject to measured tuning, not a claim of current achievement.
- Accessibility: separate Music/SFX/Ambience/Voice sliders, global mute, device silent-state expectations, no critical information conveyed exclusively by sound, reduced startling alerts and sound-off gameplay parity.
- Use non-sensitive build evidence: hashes, import settings, mixer diagram, captured playback, measured performance; log caveats.

## 7. Production gates / integration into roadmap
- **W01/P00:** lock identity, audio event inventory, source/rights schema, Goldens to audition; log owner review requirement. No audio approved by planning.
- **W03–W07:** Golden UI/voice/ambient prototypes, in-context sound audition tied to pixel character and visual vertical slice; owner must review **before** declaring sonic identity locked. Work on other unrelated streams may continue.
- **W08–W13:** career/business/economy/event cues tied to actual new interactions; bundle reviews by sound family without hiding individual rejected versions.
- **W14/P05:** mixer, adaptive stems, compression/perf, cross-device audition; explicit owner approval of representative complete gameplay mix.
- **W15–W16/P06:** rights/consent recheck, owner release-audio approval on exact RC version, regression & mobile QA. Any pending audio gate blocks audio sign-off and shipment of that audio.

**Session closeout required fields:** Audio assets created (or NONE); playable preview links (or NONE); owner review requested at [channel/date] (or NOT SENT); review status per gate; legal/rights evidence; technical/device test evidence; files and exact commit. Never represent planned assets as made or preview-ready until files exist.
