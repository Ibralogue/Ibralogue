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

Reference a global variable with the `$` prefix:

```text
[NPC]
Hi, $PLAYERNAME.
[$PLAYERNAME]
Hi. What's up?
```

Variables can be used in both dialogue text and speaker names. If the variable `PLAYERNAME` is set to "Ibrahim", the above would display as:

```
NPC: Hi, Ibrahim.
Ibrahim: Hi. What's up?
```

#### Updating Variables at Runtime

Since `GlobalVariables` is a standard dictionary, you can update values at any time:

```cs
DialogueGlobals.GlobalVariables["PLAYERNAME"] = "New Name";
```

The new value will be used the next time the dialogue is parsed.

#### Missing Variables

If a variable is referenced in a dialogue file but has no entry in the dictionary, Ibralogue will emit a warning and leave the `$VARIABLE` text as-is.
