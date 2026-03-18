### Choices

Choices let the player pick between options that branch the dialogue into different conversations or continue the current one.

#### Basic Syntax

A choice is a line starting with `-`, followed by the choice text, then `->`, then the target:

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

#### Continue Choices

Use `>>` as the target to continue the dialogue without jumping to a different conversation:

```text
[NPC]
Do you even listen to me?
- Nope -> Angry
- Sure -> >>

[NPC]
I feel like you don't
- Yeah, I don't -> Angry
- Sure -> >>

[NPC]
... if you say so
```

When the player selects a `>>` choice, the dialogue continues to the next line in the same conversation. This avoids needing to create separate conversations for simple "stay on this path" options.

A conversation can have any number of choice groups, each followed by more dialogue.

#### Choices with Metadata

Choices support metadata using the same `##` syntax as dialogue lines:

```text
- Accept the quest -> QuestAccepted ## quest:main important
- Decline -> QuestDeclined ## quest:main
```

The metadata is accessible on the `Choice` object in code.

#### Filtering Choices from Code

The engine's `ChoiceFilter` property lets external systems hide, reorder, or modify choices based on game state before they are shown to the player. The filter receives the resolved list and returns the filtered list:

```cs
dialogueEngine.ChoiceFilter = (choices) =>
{
    // Hide choices the player can't afford
    return choices.Where(c =>
    {
        if (c.Metadata != null && c.Metadata.TryGetValue("cost", out string cost))
            return playerGold >= int.Parse(cost);
        return true;
    }).ToList();
};
```

```text
- Buy the sword -> BuySword ## cost:100
- Buy the shield -> BuyShield ## cost:50
- Just looking -> >>
```

Set `ChoiceFilter` to `null` to disable filtering. The filter runs after `{{If}}` conditional evaluation, so both systems can work together.

#### Placement

Choices can appear anywhere after a dialogue line within a conversation. Multiple choice groups can be interspersed with dialogue lines throughout a conversation.
