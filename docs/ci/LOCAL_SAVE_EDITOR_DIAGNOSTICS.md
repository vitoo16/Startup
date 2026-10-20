# Local save diagnosis for Character Creator testing (Unity Editor)

**Status:** Tooling only. Unity 6000.3.25f1 compilation and visual PlayMode still require a real Editor run.

## Why this exists

`FirstPlayable.unity` has `CharacterCreationPanel` active and `LifePanel` inactive in serialized scene data. `StartupLifeBootstrapper.RefreshMode()` enables Life only when the loaded `FirstPlayableState.Name` is nonempty. `GameSession.NewState()` creates an empty name. If the Game View shows a named character after opening this scene, either an existing checkpoint was restored or an already-running Play Mode still holds the character in memory. Removing save files while Play Mode is running cannot reset the in-memory character.

## Safe UI procedure

1. Select `Assets/StartupLife/Scenes/FirstPlayable.unity`.
2. From the Unity Editor top menu select **Startup Life → Debug → Show actual save location** while Play Mode is running (or stopped). The dialog and Console show the **actual** `Application.persistentDataPath`, matching `startup-life.json*` file count, and whether the Play Mode snapshot has a character.
3. **Stop** Play Mode. Select **Startup Life → Debug → Archive test save for new character**. Review the confirmation and opt in. This moves local files matching `startup-life.json*` into a timestamped `EditorTestSaveArchives` subfolder within the same local save directory; it never deletes them or touches the repository.
4. Start Play Mode again. With no checkpoint, Character Creator should be visible.
5. To resume a prior character: Stop Play Mode, move that archived group of files back to the parent save directory after preserving any newer test save.

If step 2 finds no files **while Play Mode is already displaying an existing character**, stop and start Play Mode once before trying anything else. If it still happens, record the dialog + full Unity Console, Editor version, scene name, and HEAD commit SHA rather than deleting other folders.

**Guardrails:** Both commands are Editor-only, not part of player builds. Archiving refuses execution while entering or running Play Mode. It requires an explicit dialog confirmation; a failed move attempts rollback and reports both locations for manual inspection. It does not change the save schema, replace the user profile, modify the source scene, or certify Unity QA.

**Validation:** Engine-free contract tests in `tools/tests/test_editor_save_diagnostics.py` check file naming, actual persistent-data API usage, guardrails and safe file move strategy. A green static result is *not* proof of the user workflow or real Unity compilation.
