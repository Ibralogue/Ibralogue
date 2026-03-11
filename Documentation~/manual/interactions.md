### Interactions

Interaction components provide a convenient way to start dialogue from Unity events like button clicks, triggers, or other gameplay systems. They sit between your game logic and the `SimpleDialogueEngine`.

All interactions share a few common fields:

| Field | Description |
|-------|-------------|
| `Dialogue Engine` | Reference to the `SimpleDialogueEngine` in your scene. |
| `Interaction Dialogues` | An array of `DialogueAsset` files available to this interaction. |
| `OnConversationStart` | UnityEvent fired when the dialogue starts. |
| `OnConversationEnd` | UnityEvent fired when the dialogue ends. |

Call `StartDialogue()` to begin the interaction. This is typically wired to a button click or other Unity event.

#### SingleInteraction

Plays a specific dialogue file from the array by index.

| Field | Description |
|-------|-------------|
| `Index` | Which dialogue asset to play from the array (zero-based). |

This is the simplest interaction type and the one used in the [Quickstart](quickstart.md).

#### RandomInteraction

Picks a random dialogue file from the array each time `StartDialogue` is called. Useful for NPCs with varied idle chatter.

#### CircularInteraction

Steps through the dialogue array in order. Each call to `StartDialogue` plays the next file in sequence.

| Field | Description |
|-------|-------------|
| `Loop` | When enabled (default), wraps back to the first file after reaching the last. When disabled, stays on the last file. |

This is useful for NPCs whose dialogue progresses over time, like a story told across multiple interactions.

#### Example Setup

1. Add a `SimpleDialogueEngine` to a GameObject.
2. Add a `CircularInteraction` to your NPC.
3. Assign the engine reference and populate the `Interaction Dialogues` array with your `.ibra` files.
4. Wire a trigger or button to call `CircularInteraction.StartDialogue`.

Each time the player interacts with the NPC, they will hear the next piece of dialogue in the sequence.
