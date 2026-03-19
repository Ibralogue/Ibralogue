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
if (dialogueEngine.IsConversationPaused)
{
    // conversation is paused
}
```

#### Pausing from Dialogue

Use the `{{PauseEngine}}` standard invocation to pause directly from a `.ibra` file. The engine halts until `ResumeConversation()` is called from code:

```text
[NPC]
Watch this!
{{PauseEngine}}

[NPC]
Pretty cool, right?
```

```cs
// External system calls this when ready
dialogueEngine.ResumeConversation();
```

This is useful when an invocation triggers an animation, cutscene, or minigame and the dialogue should wait for it to finish.

#### Events

The engine fires `OnConversationPaused` when paused and `OnConversationResumed` when resumed. See the [Events](events.md) page for details.
