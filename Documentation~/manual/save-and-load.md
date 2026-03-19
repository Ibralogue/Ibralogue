### Save and Load

Ibralogue provides three serializable snapshots that together capture the complete dialogue state. Use them with any serialization backend (JSON, binary, PlayerPrefs, etc.).

#### Saving

```cs
// Capture all dialogue state
VariableSnapshot variables = VariableStore.ExportState();
VisitSnapshot visits = VisitTracker.ExportState();
ConversationProgress progress = engine.ExportProgress(); // null if not mid-conversation
```

All three objects are `[Serializable]` and contain only primitive types (strings, ints, lists, dictionaries).

`ExportProgress()` returns null when no conversation is active. If the player is mid-line or mid-choice when you save, the current node will re-display from the beginning on load (the typewriter restarts, choices are re-presented).

#### Loading

Restore state in this order: variables and visits first, then conversation progress. The progress restore relies on the variable state being correct to re-evaluate conditionals during the skip.

```cs
// Restore state
VariableStore.ImportState(savedVariables);
VisitTracker.ImportState(savedVisits);

if (savedProgress != null)
    engine.ResumeFromProgress(dialogueAsset, savedProgress);
```

`ResumeFromProgress` parses the asset, finds the conversation by name, then walks the content tree forward to the saved position. Variable assignment commands (`{{Set}}`, `{{Global}}`) are skipped during the walk because their effects are already captured in the variable snapshot. Conditionals are re-evaluated using the restored variable state to take the correct branches.

#### What Each Snapshot Contains

| Snapshot | Contents |
|----------|----------|
| `VariableSnapshot` | All global and local (per-asset) variables as string key-value pairs. |
| `VisitSnapshot` | All visited keys as a list of strings. |
| `ConversationProgress` | Asset name, conversation name, and how many displayable nodes were processed. |

#### Resilience to Content Changes

If the `.ibra` file was modified between save and load, the engine walks as far as it can. If the conversation is shorter than expected, the engine resumes at the end (which triggers conversation stop). If the conversation name no longer exists, the engine logs a warning and starts from the beginning.

#### Without Mid-Conversation Saves

If your game only saves between conversations, you only need variable and visit snapshots:

```cs
// Save (between conversations)
VariableSnapshot variables = VariableStore.ExportState();
VisitSnapshot visits = VisitTracker.ExportState();

// Load
VariableStore.ImportState(variables);
VisitTracker.ImportState(visits);
```

The next time a conversation starts, conditionals and visited checks will reflect the restored state.
