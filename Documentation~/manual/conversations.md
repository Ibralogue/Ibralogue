### Conversations

A single `.ibra` file can contain multiple conversations. Conversations are named blocks of dialogue that can be jumped to by name, which is how branching dialogue works.

#### Naming a Conversation

Use `{{ConversationName(...)}}` on its own line to mark the start of a conversation:

```ibra
{{ConversationName(Greeting)}}
[NPC]
Hello, traveller.
[NPC]
What brings you here?
```

Everything after the `{{ConversationName(...)}}` line belongs to that conversation, until the next `{{ConversationName(...)}}` or the end of the file.

#### Multiple Conversations

```ibra
{{ConversationName(Greeting)}}
[NPC]
Hello, traveller.

{{ConversationName(Farewell)}}
[NPC]
Safe travels, friend.
```

This file contains two conversations: "Greeting" and "Farewell".

#### Default Conversation

If no `{{ConversationName(...)}}` is specified, the conversation is named "Default". This is fine for simple files that only need one conversation:

```ibra
[NPC]
Hello World!
```

[Comments](comments.md) and blank lines above the first `{{ConversationName(...)}}` are ignored, so a file can open with notes without creating a "Default" conversation in front of the named one.

#### Empty Files

A file with no dialogue in it, or with nothing but comments, contains a single empty "Default" conversation. Starting it fires the start and end events and displays nothing, so placeholder files are safe to wire up before they are written. The importer reports a warning for them.

#### Starting a Specific Conversation

By default, `StartConversation` begins at the first conversation in the file. You can pass a `startIndex` to start at a different one:

```cs
dialogueManager.StartConversation(dialogueAsset, startIndex: 1);
```

#### Jumping Between Conversations

You can jump to a conversation by name directly in a dialogue file using the `{{Jump(...)}}` invocation:

```ibra
{{ConversationName(Greeting)}}
[NPC]
{{Jump(Farewell)}}
Hello, traveller.

{{ConversationName(Farewell)}}
[NPC]
Safe travels, friend.
```

After the player advances past "Hello, traveller.", the engine automatically switches to the "Farewell" conversation. See the [Jumps](jumps.md) page for more.

You can also jump from code at runtime using `JumpTo`:

```cs
dialogueManager.JumpTo("Farewell");
```

This moves the running dialogue to the named conversation. A conversation must be in progress, otherwise `JumpTo` throws.

Moving between conversations, whether by `{{Jump(...)}}`, `JumpTo` or a choice, does not end the dialogue. `OnConversationStart` and `OnConversationEnd` fire once for the whole dialogue, not once per conversation. See the [Events](events.md) page for details.

Jumping is also how choices work behind the scenes. See the [Choices](choices.md) page for more.
