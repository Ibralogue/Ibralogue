### Global Variables

Global variables allow you to define values in code that can be referenced in dialogue files. They are useful for things like player names and other values that change at runtime.

#### Defining Variables

Register variables through `DialogueGlobals.GlobalVariables`:

```cs
private void Awake()
{
    DialogueGlobals.GlobalVariables.Add("PLAYERNAME", "Ibrahim");
}
```

#### Using Variables in Dialogue

Reference a global variable with the `$` prefix. Variables are resolved anywhere in your dialogue -- text, speaker names, function arguments, metadata values, choices, etc.

```text
[NPC]
Hi, $PLAYERNAME.
[$PLAYERNAME]
Hi. What's up?
```

If the variable `PLAYERNAME` is set to "Ibrahim", the above would display as:

```
NPC: Hi, Ibrahim.
Ibrahim: Hi. What's up?
```

#### Variables in Function Arguments

Global variables can be passed directly as function arguments:

```text
[NPC]
You received {{GiveItem($REWARD)}}.
The $TARGET takes {{FormatDamage($DMG)}} damage!
```

This lets your dialogue functions receive dynamic values without hardcoding them in the dialogue file.

#### Variables in Metadata and Choices

Variables are also resolved in metadata values and choice text:

```text
[NPC]
How do you feel? ## emotion:$MOOD
- Go to $LOCATION -> $LOCATION ## quest:$QUESTID
```

#### Updating Variables at Runtime

Since `GlobalVariables` is a standard dictionary, you can update values at any time:

```cs
DialogueGlobals.GlobalVariables["PLAYERNAME"] = "New Name";
```

The new value will be used the next time the dialogue is parsed.

#### Missing Variables

If a variable is referenced in a dialogue file but has no entry in the dictionary, Ibralogue will emit a warning and leave the `$VARIABLE` text as-is.
