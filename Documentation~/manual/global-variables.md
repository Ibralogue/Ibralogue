### Variables

Ibralogue has a flexible variable system with two scopes: **global** variables that persist across all dialogue files, and **local** variables scoped to a single file.

Variables are resolved at **runtime**, meaning changes to variable values are reflected immediately in any dialogue that references them, without reparsing.

#### Reading Variables in Dialogue

Reference any variable with the `$` prefix. Variables work anywhere in dialogue -- text, speaker names, function arguments, metadata values, choices, and jump targets.

```text
[NPC]
Hi, $PLAYERNAME.
[$PLAYERNAME]
Hi. What's up?
```

#### Setting Variables from Code

Register global variables through `DialogueGlobals.GlobalVariables` or the `VariableStore`:

```cs
// Legacy API (still works)
DialogueGlobals.GlobalVariables.Add("PLAYERNAME", "Ibrahim");

// New API with typed values
VariableStore.SetGlobal("HEALTH", 100.0);
VariableStore.SetGlobal("QUEST_DONE", false);
```

#### Setting Variables from Dialogue

Use `{{Set(...)}}` to assign a variable from within a dialogue file:

```text
{{Set($GOLD, 100)}}
{{Set($NAME, "Alice")}}
{{Set($QUEST_DONE, true)}}
```

The value can be any expression, including references to other variables and arithmetic:

```text
{{Set($HEALTH, $HEALTH - 10)}}
{{Set($TOTAL, $BASE_PRICE * $QUANTITY)}}
{{Set($GREETING, "Hello " + $PLAYERNAME)}}
```

If the variable already exists in any scope, `{{Set(...)}}` updates it in place. If it does not exist, a new **local** variable is created, scoped to the current dialogue file.

#### Global vs Local Scope

Variables set from C# are always global. Variables created by `{{Set(...)}}` in a dialogue file are local by default.

To explicitly declare a variable as global from within dialogue, use `{{Global(...)}}`:

```text
{{Global($PLAYER_SCORE, 0)}}
```

This creates (or updates) a global variable that persists across all dialogue files. If only a name is provided, the variable is registered without a value:

```text
{{Global($PLAYER_SCORE)}}
```

#### Resolution Order

When a variable is referenced, Ibralogue checks scopes in this order:

1. **Local** (current dialogue file)
2. **Global** (set via `VariableStore.SetGlobal` or `{{Global(...)}}`)
3. **Legacy** (`DialogueGlobals.GlobalVariables`)

The first match wins. This means a local variable can shadow a global variable of the same name.

#### Variables in Function Arguments

Variables are resolved inside function arguments:

```text
[NPC]
You received {{GiveItem($REWARD)}}.
The $TARGET takes {{FormatDamage($DMG)}} damage!
```

#### Variables in Metadata and Choices

Variables are also resolved in metadata values and choice text:

```text
[NPC]
How do you feel? ## emotion:$MOOD
- Go to $LOCATION -> $LOCATION ## quest:$QUESTID
```

#### Managing Variables from Code

The `VariableStore` API provides full control:

```cs
// Set a global variable
VariableStore.SetGlobal("SCORE", 100.0);

// Set a file-local variable
VariableStore.SetLocal("myDialogue", "temp", "hello");

// Read a variable (checks local, then global, then legacy)
object value = VariableStore.Resolve("myDialogue", "SCORE");

// Clear local variables for a specific asset
VariableStore.ClearLocals("myDialogue");

// Clear all variables (does not affect legacy DialogueGlobals)
VariableStore.ClearAll();
```

#### Missing Variables

If a variable is referenced but has no entry in any scope, the `$VARIABLE` text is left as-is.
