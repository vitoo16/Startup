# M9-T02 — Immutable v2 Source Archive Integrity Manifest

Contract: `M9-T02-TA-1.0-FINAL`; original baseline commit `051a931410f2dec0ec51906f7f06191a0deaf77c`.

Source content at baseline: `first-playable.v1` (real Unity-authored asset with `freelance-service` and `coffee-kiosk` revision `v1`).

This table records byte-exact SHA-256 reported by successful engine-free CI [run #37856916109](https://github.com/vitoo16/Startup/actions/runs/37856916109) and Git blob SHA-1. The archived copies have identical source bytes and remain read-only, even if current production evaluator/contents later evolve to v3.

| Frozen archive file | Git blob SHA-1 | SHA-256 |
|---|---|---|
| `Business.cs.txt` | `505e036003431fa31dab749311d4deffa098f667` | `c76306ddbc1a1c20418f0366a3323fb7cf84212e1ffaa12e0e69cd2944242a15` |
| `ContentCatalogSource.cs.txt` | `3cd16f2c1b5023d10e52f96f27bc68c6afc37bd4` | `b776a8f8ed04fa49422cf378931b31aa74d0114bc99cc4a066a328e4a79bdd9a` |
| `Definitions.cs.txt` | `55dead577b734e6d4497984160132dd01eb00242` | `35b70b7229029a144a5ec110a57bd1f985d6c274acfd49c66df843f2b341b8c0` |
| `FirstPlayableContent.asset.yaml` | `fe78dc29d590ecacca6056eba07a7b2cf4605557` | `1f637ade3b8cdb7334deab7cef75db5c25751f24d7654c6577d899cc8b999ae0` |
| `FirstPlayableContentTemplate.cs.txt` | `7bb5a829c5d1647b7df123249dd8a1233bda231a` | `927065c9559b6b223112a5ff39ccdde49794f160e59e2022300cc8bd9b207a3e` |
| `HistoricalV1Codec.cs.txt` | `3c407d78c3b344ee042408fc04fce4b2119dac15` | `487507084edc90bfb17b7c2be23c154394be7d7d986beb64d009859715df7fc4` |
| `JsonSaveSerializer.cs.txt` | `1733cdf32e9779dd5c489fd4c6a612eec2e9de28` | `ac4d8def1e01841ba8cd65a76f122962993f228cb483c72db7f390b8381450d4` |
| `SimulationEngine.cs.txt` | `4adbbce053db01ac5ee161e74838349740899f4b` | `871d51d694e1ad5451a08d1eb120f3ccf9ddff7e42d0b4eb247250776ad7a0e0` |

Enforced at every engine-free CI by `scripts/Test-M9T02FrozenV2.ps1`: exact Git SHA-1 AND SHA-256, with per-run JSON report uploaded under engine-free foundation reports.

**Scope boundary:** This archive preserves original authored v2 inputs and evaluator *source*. A compiled, registered v2 historical evaluator + frozen v2 wire DTO and adjacent 2→3 migration are NOT yet implemented. Do not claim SaveVersion 3 replay or release approval from this manifest.
