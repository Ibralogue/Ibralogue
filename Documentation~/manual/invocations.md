### Function Invocations

Function invocations allow you to call static C# methods directly from your dialogue files. They are enclosed in double curly braces: `{{FunctionName}}`.

#### Built-in Functions

Ibralogue ships with a standard library of functions that work out of the box:

| Function | Description |
|----------|-------------|
| `{{Image(path)}}` | Changes the speaker portrait. Works as a standalone command or inline. |
| `{{Audio(clipId)}}` | Plays an audio clip through the engine's audio provider. |
| `{{Wait(seconds)}}` | Pauses the text animation for the given duration. |
| `{{Speed(multiplier)}}` | Changes the typewriter speed. 2 = twice as fast, 0.5 = half speed. |

See [Basic Syntax](basic-syntax.md) for usage examples.

#### Defining a Custom Dialogue Function

Any static method with the `[DialogueFunction]` attribute can be invoked from Ibralogue:

```cs
[DialogueFunction]
public static void Die()
{
    Debug.Log("Dead.");
}
```

#### Invoking from Dialogue

```text
[NPC]
Time to die.
{{Die}}
```

When this line is displayed, the `Die` method is called.

#### Invocation Timing

When a function invocation appears inline within dialogue text, it fires at its **character position** during the animated display effect (typewriter, punch, etc.). This lets you trigger side effects at precise moments in the text:

```text
[NPC]
I have a gift for you. {{Audio(fanfare)}} Ta-da!
```

The `Audio` function fires when the typewriter reaches the space after "you. " -- not when the line first appears.

Functions that return a string (see below) are an exception: they fire immediately **before** the animation starts, so their returned text is part of the full line from the beginning.

#### Functions that Return Strings

If a dialogue function returns a `string`, the return value is inserted directly into the dialogue text at the position of the invocation. This is useful for injecting dynamic content like dates, names, or computed values.

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

The player would see something like "Today is Wednesday." depending on the current day.

#### Accessing the Engine from a Function

A dialogue function can optionally accept a `DialogueEngineBase` parameter. When it does, Ibralogue passes the current engine instance, giving the function access to the full engine API.

```cs
[DialogueFunction]
public static void PauseForDrama(DialogueEngineBase engine)
{
    engine.PauseConversation();
    // Resume after some delay elsewhere
}
```

This works for both void and string-returning functions. If the function has no parameters, Ibralogue calls it without arguments as usual.

#### Using Global Variables as Arguments

[Variables](global-variables.md) are resolved inside function arguments, so you can pass dynamic values:

```text
[NPC]
You received {{GiveItem($REWARD)}}.
```

If `REWARD` is set to `"Sword"`, the function `GiveItem` receives `"Sword"` as its argument.

#### Assembly Search

By default, Ibralogue searches for dialogue functions in `Assembly-CSharp` and its own assembly. If your dialogue functions live in other assemblies, you can configure this on the `SimpleDialogueEngine` component:

- **Search All Assemblies**: Enable this to search every loaded assembly.
- **Included Assemblies**: Add specific assembly names to the search list.
