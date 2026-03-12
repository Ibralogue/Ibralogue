### Comments

`#` is used to denote comments. Commented lines are ignored by the parser. There are no block comments.

```text
# Reminder: Need to rephrase this
[NPC]
Time to die.
```

Comments can also appear between dialogue lines:

```text
[NPC]
Hello there.
# The next line is intentionally dramatic
[NPC]
We need to talk.
```

### Metadata

A double-hashtag `##` denotes metadata. Metadata is attached to the dialogue line it appears on and can be accessed from code.

```text
[NPC]
Nice to meet you! ## greeting
```

You can check for metadata on a line like so:

```cs
if (dialogueManager.ParsedConversations[0].Lines[0].HasMetadata("greeting"))
{
    Debug.Log("This line is a greeting.");
}
```

#### Multiple Metadata Tags

One line can carry more than one piece of metadata, separated by spaces:

```text
[NPC]
This is a sentence with a lot of metadata. ## cool funny epic sad
```

#### Key-Value Metadata

Metadata can also carry key-value pairs, separated by a colon:

```text
[NPC]
Today is a sad day. ## emotion:sad
```

```cs
if (dialogueManager.ParsedConversations[0].Lines[0].TryGetMetadata("emotion", out string value))
{
    Debug.Log(value); // "sad"
}
```
