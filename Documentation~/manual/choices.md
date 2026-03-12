### Choices

Choices let the player pick between options that branch the dialogue into different conversations.

#### Basic Syntax

A choice is a line starting with `-`, followed by the choice text, then `->`, then the name of the conversation to jump to:

```text
- Choice text -> TargetConversation
```

#### Example

```text
{{ConversationName(Initial)}}
[NPC]
How are you doing today?
- I'm fine -> Fine
- Not great -> NotGreat

{{ConversationName(Fine)}}
[NPC]
Good to hear!

{{ConversationName(NotGreat)}}
[NPC]
I'm sorry to hear that. Hope it gets better.
```

When the player reaches the choices, they are presented with "I'm fine" and "Not great". Selecting one jumps to the corresponding conversation.

#### Choices with Metadata

Choices support metadata using the same `##` syntax as dialogue lines:

```text
- Accept the quest -> QuestAccepted ## quest:main important
- Decline -> QuestDeclined ## quest:main
```

The metadata is accessible on the `Choice` object in code.

#### Placement

Choices must appear after at least one dialogue line within a conversation. They are always the last thing in a conversation block before the next `{{ConversationName(...)}}`.
