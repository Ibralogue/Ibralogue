### Functions

Ibralogue uses the `{{Name(args)}}` syntax for two purposes: **keywords** that control dialogue structure, and **functions** that trigger behavior at runtime.

#### Keyword Functions

Keywords are structural. They shape which content plays and how the dialogue is organized. They are NOT functions and cannot be used inline in text.

| Keyword | Purpose |
|---------|---------|
| `{{ConversationName(name)}}` | Defines a named conversation block |
| `{{Jump(target)}}` | Jumps to another conversation after the current line |
| `{{If(expr)}}` / `{{ElseIf(expr)}}` / `{{Else}}` / `{{EndIf}}` | Conditional flow control |
| `{{Set($var, expr)}}` | Assigns a variable |
| `{{Global($var, expr)}}` | Declares a global variable |
| `{{Include(asset)}}` | Includes another dialogue file |

See [Conversations](conversations.md), [Conditionals](conditionals.md), and [Variables](global-variables.md) for details.

#### Standard Functions

Ibralogue ships with built-in functions for common tasks. These can be placed on their own line (fires at line start) or inline in text (fires at that point in the text):

| Function | Description |
|----------|-------------|
| `{{Image(path)}}` | Changes the speaker portrait via the `PortraitImagePlugin`. |
| `{{Audio(clipId)}}` | Plays an audio clip via the engine's `IAudioProvider`. |
| `{{Wait(seconds)}}` | Pauses the display for the given duration. |
| `{{Speed(multiplier)}}` | Changes the text reveal speed. 2 = twice as fast, 0.5 = half speed. Affects animated views only. |

```text
[NPC]
{{Image(Portraits/Happy)}}
{{Audio(Voiceover/greeting)}}
Hello there!

[NPC]
And the winner is... {{Wait(2)}} you!

[NPC]
Hello! {{Image(Portraits/Surprised)}} I didn't expect that!
```

`{{Wait(seconds)}}` and `{{Speed(multiplier)}}` are meaningful with animated [dialogue views](dialogue-views.md) like the typewriter or punch views. With views that display text instantly, Wait still inserts a timed pause but Speed has no effect.

#### Custom Functions

Any static C# method with the `[DialogueFunction]` attribute can be called from dialogue:

```cs
[DialogueFunction]
public static void Die()
{
    Debug.Log("Dead.");
}
```

```text
[NPC]
Time to die.
{{Die}}
```

#### Functions that Return Strings

If a function returns a `string`, the return value is inserted into the dialogue text at the position of the invocation. These always fire before the line is displayed so that the full text is known upfront.

```cs
[DialogueFunction]
public static string GetDay()
{
    return System.DateTime.Now.DayOfWeek.ToString();
}
```

```text
[NPC]
Today is {{GetDay}}.
```

The player sees "Today is Wednesday." (or whichever day it is).

#### Accessing the Engine

A function can optionally accept a `DialogueEngineBase` parameter to access the engine API:

```cs
[DialogueFunction]
public static void PauseForDrama(DialogueEngineBase engine)
{
    engine.PauseConversation();
}
```

#### Variables as Arguments

[Variables](global-variables.md) are resolved inside function arguments:

```text
[NPC]
You received {{GiveItem($REWARD)}}.
```

If `REWARD` is `"Sword"`, the function `GiveItem` receives `"Sword"`.

#### Assembly Search

By default, Ibralogue searches for functions in `Assembly-CSharp` and its own assembly. If your functions live in other assemblies, configure this on the `SimpleDialogueEngine` component:

- **Search All Assemblies**: Enable this to search every loaded assembly.
- **Included Assemblies**: Add specific assembly names to the search list.
