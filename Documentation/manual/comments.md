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

One line can contain more than one piece of metadata, these are separated via spaces:
```
[NPC]
This is a sentence with a lot of metadata. ## cool funny epic sad
```
```cs
private void GetOption() 
{
    foreach(var pair in dialogueManager.ParsedConversations[0].Lines[0].Metadata)
    {
        Debug.Log(pair.Key); // will log cool, funny, epic, sad
    }
}
```

Metadata can have keys and values associated with it.
```
[NPC]
Today is a sad day. ## emotion:sad
```