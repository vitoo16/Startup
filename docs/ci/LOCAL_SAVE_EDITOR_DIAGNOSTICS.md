# Local save diagnosis for Character Creator testing (Unity Editor)

**Status:** Tooling only. Unity 6000.3.25f1 compilation and visual PlayMode still require a real Editor run.

## Why this exists

`FirstPlayable.unity` has `CharacterCreationPanel` active and `LifePanel` inactive in serialized scene data. `StartupLifeBootstrapper.RefreshMode()` enables Life only when the loaded `FirstPlayableState.Name` is nonempty. `GameSession.NewState()` creates an empty name. If the Game View shows a named character after opening this scene, either an existing checkpoint was restored or an already-running Play Mode still holds the character in memory. Removing save files while Play Mode is running cannot reset the in-memory character.

## What the new Editor startup receipt proves

On each Play Mode initialization, Console prints a line starting with `[StartupLife][SaveBoot]`. It is emitted by `StartupLifeBootstrapper` immediately after the physical V3 or V4 store read, before any UI visibility decisions. Fields:

- `status=Missing`: no valid primary or backup was read; the first run should have no character.
- `status=Valid`: primary save was read and accepted.
- `status=RecoveredBackup`: the secondary `startup-life.json.backup` was used because the primary could not be read as a valid checkpoint.
- `primaryExists`, `backupExists`: whether corresponding files exist at read time.
- `revision`, `characterPresent`: the read checkpoint's state, without logging personal names.

If `status=Missing` and `characterPresent=False` but the Life panel still appears in a *fresh Play Mode*, capture the whole Console and Hierarchy: this points to view or lifecycle handling rather than a hidden save. Stop Play Mode first before moving any files. If the user removed the folder while Play Mode was running, the current in-memory state does not reset until a fresh start.

## Safe UI procedure

1. Select `Assets/StartupLife/Scenes/FirstPlayable.unity`.
2. From the Unity Editor top menu select **Startup Life → Debug → Show actual save location** while Play Mode is running (or stopped). The dialog and Console show the **actual** `Application.persistentDataPath`, primary/backup checkpoint file count, and whether the Play Mode snapshot has a character.
3. **Stop** Play Mode. Select **Startup Life → Debug → Archive test save for new character**. Review the confirmation and opt in. This moves the primary checkpoint and its `.backup` (if present) into a timestamped `RunArchives` subfolder within the same local save directory; it never deletes them or touches the repository.
4. Start Play Mode again. With no checkpoint, Character Creator should be visible.
5. To resume a prior character: Stop Play Mode, move that archived group of files back to the parent save directory after preserving any newer test save.

If step 2 finds no files **while Play Mode is already displaying an existing character**, stop and start Play Mode once before trying anything else. If it still happens, record the dialog + full Unity Console, Editor version, scene name, and HEAD commit SHA rather than deleting other folders.

**Guardrails:** Both commands are Editor-only, not part of player builds. Archiving refuses execution while entering or running Play Mode. It requires an explicit dialog confirmation; a failed move attempts rollback and reports both locations for manual inspection. It does not change the save schema, replace the user profile, modify the source scene, or certify Unity QA.

**Validation:** `tools/SaveArchiveChecks/` compiles the same `LocalCheckpointArchiver` runtime source and exercises file I/O, backup preservation and a held writer lock. `tools/tests/test_editor_save_diagnostics.py` also checks the Editor integration and confirmation gate. A green static result is *not* proof of the user workflow or real Unity compilation.


## Player menu (runtime candidate, not yet Unity-verified)

In `FirstPlayable`, the **Menu** button at the top opens **Tiếp tục hành trình**, **Trò chơi mới**, and **Cài đặt**. For a **restored named character**, the menu opens automatically so the player chooses Continue or New Game. For a **fresh run with no character**, Character Creator opens directly; the small Menu entry remains available. The new-game confirmation calls the same `LocalCheckpointArchiver` to move primary + backup before reloading the current scene. It refuses a running work playback, or a missing checkpoint from a named in-memory character. Cancel/Back never writes to the checkpoint. The Settings pane currently exposes reduced motion, not the complete audio/language catalog.

The `.lock` file is deliberately retained so concurrent save writers use the same gate. Only the primary and its recovery backup are moved; unrelated or partial temporary files are left untouched. Keep other open Unity/player processes closed while resetting a run: an idle second process could create a new checkpoint **after** the archive releases its lock. `RunArchives` is a recovery folder, not a full multi-slot save UI. Restore a prior pair manually only after stopping Play Mode and protecting any newer checkpoint.

**Not yet verified:** Unity Editor compilation, manual navigation, saved-run reload, mobile size/accessibility, and on-device visual acceptance. This code cannot be labeled a production-approved title screen before those gates.
