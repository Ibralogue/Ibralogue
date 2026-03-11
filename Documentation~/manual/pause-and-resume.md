### Pause and Resume

Conversations can be paused and resumed at runtime. This is useful when opening an inventory, entering a cutscene, or any situation where dialogue should wait.

#### Pausing

```cs
dialogueEngine.PauseConversation();
```

This pauses the current conversation. The view's display effect is paused, and the engine stops advancing. If no conversation is active or the conversation is already paused, this does nothing.

#### Resuming

```cs
dialogueEngine.ResumeConversation();
```

This resumes a paused conversation from where it left off.

#### Checking Pause State

```cs
if (dialogueEngine.IsConversationPaused())
{
    // conversation is paused
}
```

#### Events

The engine fires `OnConversationPaused` when paused and `OnConversationResumed` when resumed. See the [Events](events.md) page for details.
