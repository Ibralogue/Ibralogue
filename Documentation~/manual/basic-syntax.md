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

### Speaker Images

Character portraits can be attached to a dialogue line using the `Image` command. Ibralogue uses [Resources.Load](https://docs.unity3d.com/ScriptReference/Resources.Load.html) to find images, so sprites must be placed in a `Resources` folder.

For a file located at `Assets/Sprites/Resources/Portraits/AvaSmiling.png`:

```text
[Ava]
{{Image(Portraits/AvaSmiling)}}
It's a beautiful day outside!
```

The `{{Image(...)}}` command must appear on its own line, directly after the speaker tag.
