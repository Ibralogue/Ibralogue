### Events

The dialogue engine exposes several UnityEvents that let you react to conversation state changes. These can be wired up in the Inspector or from code.

#### Persistent Events

These events are configured in the Inspector and survive across conversation switches. They are never cleared automatically.

| Event | When it fires |
|-------|---------------|
| `PersistentOnConversationStart` | When any conversation starts. |
| `PersistentOnConversationEnd` | When any conversation ends. |

Use these for things that should always happen, like showing or hiding the dialogue UI.

#### Transient Events

These events are code-only (`[HideInInspector]`) and are cleared every time a conversation stops. They are useful for one-off reactions tied to a specific conversation.

| Event | When it fires |
|-------|---------------|
| `OnConversationStart` | When a conversation starts. Cleared on stop. |
| `OnConversationEnd` | When a conversation ends. Cleared on stop. |

```cs
dialogueEngine.OnConversationStart.AddListener(() =>
{
    Debug.Log("Conversation started");
});
```

Because transient listeners are cleared after each conversation, you do not need to remove them manually.

#### Pause and Resume Events

| Event | When it fires |
|-------|---------------|
| `OnConversationPaused` | When `PauseConversation()` is called. |
| `OnConversationResumed` | When `ResumeConversation()` is called. |

These are public and can be configured in the Inspector or from code.

#### View Events

The dialogue view also exposes events. See the [Dialogue Views](dialogue-views.md) page for `OnSetView` and `OnLineComplete`.

#### Interaction Events

Interaction components (`SingleInteraction`, `RandomInteraction`, `CircularInteraction`) have their own `OnConversationStart` and `OnConversationEnd` fields in the Inspector. These are wired to the engine's events when `StartDialogue` is called. See the [Interactions](interactions.md) page for details.
