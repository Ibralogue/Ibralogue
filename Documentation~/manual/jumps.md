### Jumps

The `Jump` invocation lets you automatically move from one conversation to another without requiring a player choice. When the dialogue line finishes displaying, the engine switches to the target conversation.

#### Basic Syntax

Use `{{Jump(...)}}` on its own line within a dialogue line.

```text
{{ConversationName(Greeting)}}
[NPC]
{{Jump(Farewell)}}
Welcome, traveller.

{{ConversationName(Farewell)}}
[NPC]
Safe travels, friend.
```

After the player advances past "Welcome, traveller.", the engine jumps to the "Farewell" conversation automatically.

#### Placement

The `{{Jump(...)}}` invocation can appear before or after the sentence text, as long as it is inside a dialogue line (after a `[Speaker]` tag):

```text
[NPC]
{{Jump(NextConversation)}}
This line will jump after it finishes.
```

```text
[NPC]
This line will also jump after it finishes.
{{Jump(NextConversation)}}
```

Both forms are equivalent. The jump always happens after the player advances past the line.

#### Variables in Jump Targets

Jump targets support [variables](global-variables.md):

```text
[NPC]
{{Jump($NEXT_CONVERSATION)}}
Moving on...
```

The variable is resolved at runtime when the line is displayed, just like variables in dialogue text.
