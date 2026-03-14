### Function Invocations

Function invocations allow you to call static C# methods directly from your dialogue files. They are enclosed in double curly braces: `{{FunctionName}}`.

#### Defining a Dialogue Function

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
