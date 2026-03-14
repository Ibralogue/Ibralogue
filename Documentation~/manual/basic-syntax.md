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

Character portraits can be attached to a dialogue line using the `Image` command or the `image` metadata key. The built-in `PortraitImagePlugin` loads sprites via [Resources.Load](https://docs.unity3d.com/ScriptReference/Resources.Load.html), so sprites must be placed in a `Resources` folder.

```text
[Ava]
{{Image(Portraits/AvaSmiling)}}
It's a beautiful day outside!
```

The same result using metadata:

```text
[Ava]
It's a beautiful day outside! ## image:Portraits/AvaSmiling
```

Both forms are equivalent. The `{{Image(...)}}` command is stored as `image` metadata on the line.

### Audio

Attach audio to a dialogue line using the `audio` metadata key:

```text
[NPC]
Welcome to the shop! ## audio:Voiceover/shop_greeting
```

Audio requires an audio provider component on the engine's GameObject. Ibralogue ships with `UnityAudioProvider` for Unity's built-in AudioSource. For FMOD, Wwise, or other backends, implement the `IAudioProvider` interface.

See [Engine Plugins](engine-plugins.md) for more details on audio setup.

### Inline Triggers

[Function invocations](invocations.md) placed inline in dialogue text fire at their character position during animated display. This means they trigger when the typewriter or punch effect reaches them, not all at once:

```text
[NPC]
Hello! {{PlaySFX(greeting)}} Welcome to my shop.
```

When the typewriter effect reaches the point after "Hello! ", the `PlaySFX` function fires. Functions that return text (inserting dynamic content) still fire immediately before the animation starts.

### Silent Lines

Use `[>>]` as the speaker to create a line that runs commands and invocations without displaying anything in the dialogue view:

```text
[NPC]
Let me check something...
[>>]
{{RunCheck}}
[NPC]
All done!
```

The `[>>]` line is processed silently by the engine -- any [function invocations](invocations.md) on it are called, but no dialogue box or speaker name is shown. This is useful for triggering game logic between visible dialogue lines.
