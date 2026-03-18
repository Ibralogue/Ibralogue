### Events

The dialogue engine exposes several UnityEvents that let you react to conversation state changes. These can be wired up in the Inspector or from code.

#### Persistent Events

These events are configured in the Inspector and survive across conversation switches. They are never cleared automatically.

| Event | When it fires |
|-------|---------------|
| `PersistentOnConversationStart` | When any conversation starts. |
| `PersistentOnConversationEnd` | When any conversation ends. |

Use these for things that should always happen, like showing or hiding the dialogue UI.

#### Conversation Events

These events can be subscribed to from code. Listeners are **not** cleared between conversations, so you can subscribe once and receive callbacks for every conversation.

| Event | Type | When it fires |
|-------|------|---------------|
| `OnConversationStart` | `UnityEvent` | When a conversation starts. |
| `OnConversationEnd` | `UnityEvent` | When a conversation ends. |

```cs
dialogueEngine.OnConversationStart.AddListener(() =>
{
    Debug.Log("Conversation started");
});
```

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

Interaction components (`SingleInteraction`, `RandomInteraction`, `CircularInteraction`) have their own `OnConversationStart` and `OnConversationEnd` fields in the Inspector. These are wired to the engine's events when `StartDialogue` is called. See the [Interactions](interactions.md) page for details.
