### Interactions

Interaction components provide a convenient way to start dialogue from Unity events like button clicks, triggers, or other gameplay systems. They sit between your game logic and the dialogue engine.

All interactions share a few common fields:

| Field | Description |
|-------|-------------|
| `Dialogue Engine` | Reference to the dialogue engine (`DialogueEngineBase`) in your scene. |
| `Interaction Dialogues` | An array of `DialogueAsset` files available to this interaction. |
| `OnConversationStart` | UnityEvent fired when this interaction's dialogue starts. |
| `OnConversationEnd` | UnityEvent fired when this interaction's dialogue ends, including when another dialogue replaces it. |

Call `StartDialogue()` to begin the interaction. This is typically wired to a button click or other Unity event.

The two events belong to the dialogue the interaction started. They do not fire for dialogues started by other interactions or from code, and a [jump](jumps.md) or choice inside the dialogue does not fire them again.

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

1. Add a `SimpleDialogueEngine` (or your own engine subclass) to a GameObject.
2. Add a `CircularInteraction` to your NPC.
3. Assign the engine reference and populate the `Interaction Dialogues` array with your `.ibra` files.
4. Wire a trigger or button to call `CircularInteraction.StartDialogue`.

Each time the player interacts with the NPC, they will hear the next piece of dialogue in the sequence.

#### Creating a Custom Interaction

Subclass `BaseInteraction`, implement `StartDialogue`, and start the dialogue through the protected `StartConversation` method. Going through it, rather than calling the engine directly, is what ties the interaction's events to its own dialogue:

```cs
using Ibralogue.Interactions;

public class FirstTimeInteraction : BaseInteraction
{
    private bool _met;

    public override void StartDialogue()
    {
        StartConversation(GetDialogueAsset(_met ? 1 : 0));
        _met = true;
    }

    protected override void OnInteractionEnded()
    {
        // Runs when this interaction's dialogue is over,
        // just before its OnConversationEnd event.
    }
}
```

#### Keeping an Interrupted Dialogue

When another dialogue starts over an interaction's dialogue, the interaction's `OnInterrupted` method is called with the position it had reached. Return true to keep the dialogue instead of ending it, and pass the progress to `ContinueConversation` when it should carry on:

```cs
using Ibralogue;
using Ibralogue.Interactions;

public class MonologueInteraction : BaseInteraction
{
    private ConversationProgress _held;

    public override void StartDialogue()
    {
        StartConversation(GetDialogueAsset(0));
    }

    protected override bool OnInterrupted(ConversationProgress progress)
    {
        _held = progress;
        return true;
    }

    public void CarryOn()
    {
        if (_held == null) return;

        ContinueConversation(GetDialogueAsset(0), _held);
        _held = null;
    }
}
```

A kept dialogue has not ended, so `OnInteractionEnded` and the `OnConversationEnd` event do not fire when it is interrupted, and `OnConversationStart` does not fire again when it continues. The line that was on screen is displayed again. See [Interruptions](events.md#interruptions) for how the engine handles one dialogue starting over another.
