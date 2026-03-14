### Conditionals

Conditionals let you show or hide dialogue lines based on variable state. They are evaluated at runtime as the engine reaches them, so earlier `{{Set(...)}}` invocations within the same conversation affect later conditions.

#### Basic If/Else

```text
[Doctor]
Let me take a look at you.
{{If($HEALTH > 50)}}
[Doctor]
You're in good shape.
{{Else}}
[Doctor]
You need medical attention.
{{EndIf}}
```

The `{{If(...)}}` block is followed by its body, an optional `{{Else}}` branch, and closed with `{{EndIf}}`.

#### ElseIf

For multiple conditions, use `{{ElseIf(...)}}`:

```text
{{If($RANK == "gold")}}
[NPC]
Welcome back, gold member!
{{ElseIf($RANK == "silver")}}
[NPC]
Welcome, silver member.
{{Else}}
[NPC]
Welcome, visitor.
{{EndIf}}
```

Branches are checked in order. The first one whose condition is true is executed. If none match and an `{{Else}}` branch exists, it runs instead.

#### Nesting

Conditional blocks can be nested:

```text
{{If($QUEST_STARTED)}}
  {{If($HAS_KEY)}}
  [Guard]
  Go right ahead.
  {{Else}}
  [Guard]
  You need the key first.
  {{EndIf}}
{{EndIf}}
```

Indentation is optional and ignored by the parser. Use it for readability.

#### Expressions

Conditions support comparisons, boolean logic, and arithmetic.

**Comparison operators:** `==`, `!=`, `<`, `>`, `<=`, `>=`

```text
{{If($GOLD >= 100)}}
{{If($NAME != "Nobody")}}
```

**Boolean operators:** `AND`, `OR`, `NOT`

```text
{{If($HEALTH > 0 AND $MANA > 10)}}
{{If($IS_FRIENDLY OR $REPUTATION > 50)}}
{{If(NOT $GAME_OVER)}}
```

**Arithmetic:** `+`, `-`, `*`, `/`

Arithmetic is available in `{{Set(...)}}` expressions and conditions:

```text
{{Set($TOTAL, $PRICE * $QTY)}}
{{If($HEALTH - $DAMAGE > 0)}}
```

**Parentheses** for grouping:

```text
{{If(($A OR $B) AND $C)}}
```

**Literal values:**

- Strings: `"hello"`
- Numbers: `42`, `3.14`
- Booleans: `true`, `false`

#### Type Coercion

When both sides of a comparison look like numbers, they are compared numerically. Otherwise they are compared as strings.

For truthiness (used by `NOT` and bare variables in conditions): `null`, empty string, `"false"`, `"0"`, and `0` are falsy. Everything else is truthy.

#### Conditionals with Set

Because conditions are evaluated as the engine reaches them, you can use `{{Set(...)}}` mid-conversation and have later conditions react:

```text
[NPC]
Here, take this key.
{{Set($HAS_KEY, true)}}

[Guard]
Let me check...
{{If($HAS_KEY)}}
[Guard]
You may pass.
{{EndIf}}
```
