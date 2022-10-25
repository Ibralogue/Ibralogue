### Comments

`#` is used to denote comments. There are no "block" comments.

```text
# Reminder: Need to rephrase this
[NPC]
Time to die.
```

### Metadata
A double-hashtag `##` can be used instead to denote metadata. This metadata can then be accessed via the `DialogueManager`:
```
[NPC]
Nice to meet you! ## option-1
```

...which can then be accessed via code like so:
```cs
private void GetOption() 
{
    if(dialogueManager.ParsedConversations[0].Lines[0].HasMetadata("option-1"))
    {
        Debug.Log("Line has the specified metadata string.");
    }
}
```
