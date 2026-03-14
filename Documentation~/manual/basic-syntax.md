## Basic Syntax

### Speakers

Use square brackets to define speaker names. Everything that follows belongs to that speaker until the next speaker tag is reached.

```text
[NPC]
Hi!
How are you?
```

In the example above, "Hi!" and "How are you?" are both part of the same dialogue line. They will be displayed together, joined by a newline.

### Separate Dialogue Lines

To split text into separate dialogue lines that the player advances through one at a time, use the speaker tag again:

```text
[NPC]
Hi!
[NPC]
How are you?
```

Now "Hi!" and "How are you?" are two separate lines. The player will see "Hi!" first, and then "How are you?" after advancing.

### Multiple Speakers

Different speakers are defined the same way:

```text
[Ava]
Hey there!
[Claire]
Oh, hey Ava.
[Ava]
How have you been?
```

### Silent Lines

Use `[>>]` as the speaker to create a line that runs functions without displaying anything in the dialogue view:

```text
[NPC]
Let me check something...
[>>]
{{RunCheck}}
[NPC]
All done!
```

The `[>>]` line is processed silently by the engine, so any [invocations](invocations.md) on it are called, but no dialogue box or speaker name is shown. This is useful for triggering game logic between visible dialogue lines.
