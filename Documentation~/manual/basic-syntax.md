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

`{{Image(...)}}` also works inline to change the portrait mid-sentence. It fires at the character position during the typewriter effect:

```text
[NPC]
Hello! {{Image(Portraits/Surprised)}} I didn't expect that!
```

### Audio

Attach audio to a dialogue line using the `audio` metadata key or the built-in `{{Audio(...)}}` function:

```text
[NPC]
Welcome to the shop! ## audio:Voiceover/shop_greeting
```

Inline audio fires at the character position during animated display:

```text
[NPC]
And then... {{Audio(SFX/explosion)}} BOOM!
```

Audio requires an audio provider component on the engine's GameObject. Ibralogue ships with `UnityAudioProvider` for Unity's built-in AudioSource. For FMOD, Wwise, or other backends, implement the `IAudioProvider` interface. See [Engine Plugins](engine-plugins.md) for setup details.

### Wait and Speed

Use `{{Wait(seconds)}}` to pause the text animation mid-sentence:

```text
[NPC]
And the winner is... {{Wait(2)}} you!
```

Use `{{Speed(multiplier)}}` to change the typewriter reveal speed mid-sentence. A multiplier of 2 is twice as fast, 0.5 is half speed:

```text
[NPC]
This is normal speed. {{Speed(0.3)}} This... is... very... slow. {{Speed(2)}} And this is fast!
```

### Inline Triggers

All inline [function invocations](invocations.md) fire at their character position during animated display. This means they trigger when the typewriter or punch effect reaches that point in the text, not all at once.

Functions that return text (inserting dynamic content) are the exception -- they fire immediately before the animation starts, so their returned text is part of the full line.

Beyond the built-in functions, you can define your own with `[DialogueFunction]`. See [Function Invocations](invocations.md) for details.

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
