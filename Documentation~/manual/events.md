### Events

The dialogue engine exposes several UnityEvents that let you react to conversation state changes. These can be wired up in the Inspector or from code.

#### Persistent Events

These events are configured in the Inspector and survive across conversation switches. They are never cleared automatically.

| Event | When it fires |
|-------|---------------|
| `PersistentOnConversationStart` | When a dialogue starts. |
| `PersistentOnConversationEnd` | When a dialogue ends. |

Use these for things that should always happen, like showing or hiding the dialogue UI.

#### Conversation Events

These events can be subscribed to from code. Listeners are **not** cleared between conversations, so you can subscribe once and receive callbacks for every conversation.

| Event | Type | When it fires |
|-------|------|---------------|
| `OnConversationStart` | `UnityEvent` | When a dialogue starts. |
| `OnConversationEnd` | `UnityEvent` | When a dialogue ends. |

```cs
dialogueEngine.OnConversationStart.AddListener(() =>
{
    Debug.Log("Conversation started");
});
```

#### When Start and End Fire

The start events fire once when `StartConversation` or `ResumeFromProgress` begins a dialogue, before its first line is displayed. The end events fire once when the engine goes idle: the dialogue ran out of lines, `StopConversation` was called, or another dialogue replaced it.

[Jumps](jumps.md) and [choices](choices.md) that move to another conversation in the same file are part of the dialogue. They fire neither event, and `History` carries on through them.

The engine is already idle when the end events fire. `IsConversationActive` is false, `CurrentLine` is null and `History` has been cleared (unless `PersistHistory` is on). This means an end listener can start the next dialogue directly:

```cs
dialogueEngine.OnConversationEnd.AddListener(() =>
{
    dialogueEngine.StartConversation(nextDialogue);
});
```

#### Callbacks for a Single Dialogue

The events above fire for every dialogue the engine plays. To hear about one dialogue only, pass callbacks when starting it:

```cs
dialogueEngine.StartConversation(dialogueAsset, 0,
    onStart: () => Debug.Log("This dialogue started"),
    onEnd: () => Debug.Log("This dialogue ended"));
```

Both are optional. `onStart` is invoked right after `OnConversationStart` and `onEnd` right after `OnConversationEnd`, and neither is called for any later dialogue. `ResumeFromProgress` takes the same two callbacks. [Interaction](interactions.md) components use them for their own events.

#### Interruptions

The engine plays one dialogue at a time and does not queue. Starting a dialogue while another is running ends the running one, fires its end events, and then starts the new one.

While those end events fire, `InterruptedProgress` holds where the interrupted dialogue had got to. It is null at any other time, so a listener can use it to tell an interruption from a dialogue that finished or was stopped:

```cs
ConversationProgress held = null;

dialogueEngine.StartConversation(monologue, onEnd: () =>
{
    // Null when the monologue finished or was stopped.
    held = dialogueEngine.InterruptedProgress;
});

// Later, once the engine is idle again:
if (held != null)
    dialogueEngine.ResumeFromProgress(monologue, held);
```

The progress points at the line that was on screen when the interruption happened, so that line is displayed again on resume.

A dialogue started from an end listener during an interruption is ignored. The dialogue doing the interrupting already has the floor, so a listener that chains dialogues together does not need to check for this case.

If your game needs dialogues to wait their turn, keep the queue in your own code: start the next one when `IsConversationActive` is false, and use the `onEnd` callback to know when each one is over.

#### Typed Dialogue Events

These events pass data to listeners, making it easy for external systems (quests, analytics, voiceover, etc.) to react to dialogue content.

| Event | Type | When it fires |
|-------|------|---------------|
| `OnLineDisplayed` | `LineEvent` | After a line is resolved and set on the view. Passes the `Line`. |
| `OnChoicesPresented` | `ChoiceListEvent` | When choices are shown to the player. Passes `List<Choice>`. |
| `OnChoiceSelected` | `ChoiceEvent` | When the player picks a choice. Passes the selected `Choice`. |

```cs
dialogueEngine.OnLineDisplayed.AddListener((line) =>
{
    Debug.Log($"{line.Speaker}: {line.LineContent.Text}");
});

dialogueEngine.OnChoiceSelected.AddListener((choice) =>
{
    Debug.Log($"Player chose: {choice.ChoiceName}");
});
```

#### Pause and Resume Events

| Event | When it fires |
|-------|---------------|
| `OnConversationPaused` | When `PauseConversation()` is called. |
| `OnConversationResumed` | When `ResumeConversation()` is called. |

These are public and can be configured in the Inspector or from code.

#### Conversation History

The engine keeps a log of every line displayed. Access it via the `History` property:

```cs
IReadOnlyList<Line> log = dialogueEngine.History;

foreach (Line line in log)
    Debug.Log($"{line.Speaker}: {line.LineContent.Text}");
```

By default, the history is cleared when a conversation stops. To keep it across conversations (for a persistent dialogue log UI), set `PersistHistory` to true:

```cs
dialogueEngine.PersistHistory = true;
```

Call `ClearHistory()` to clear it manually.

#### View Events

The dialogue view also exposes events. See the [Dialogue Views](dialogue-views.md) page for `OnSetView` and `OnLineComplete`.

#### Interaction Events

Interaction components (`SingleInteraction`, `RandomInteraction`, `CircularInteraction`) have their own `OnConversationStart` and `OnConversationEnd` fields in the Inspector. These fire only for the dialogue that interaction started. See the [Interactions](interactions.md) page for details.
