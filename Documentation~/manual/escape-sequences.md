### Escape Sequences

Ibralogue uses several characters and character sequences as syntax. To use these literally in dialogue text, prefix them with a backslash (`\`).

#### Escaping Inline Syntax

Within a dialogue line, `{{`, `$`, and `##` have special meaning. A backslash prevents them from being interpreted as syntax:

```text
[NPC]
The price is \$50 per item.
Use \{{curly braces}} for templates.
See section \## for more info.
```

This displays as:

```
NPC: The price is $50 per item.
NPC: Use {{curly braces}} for templates.
NPC: See section ## for more info.
```

Without the backslashes, `$50` would be treated as a [variable](global-variables.md), `{{curly braces}}` as a [function invocation](invocations.md), and `##` as [metadata](comments.md).

#### Escaping Line-Start Syntax

Some syntax is only special at the start of a line: `#` (comments), `##` (metadata), `[` (speaker names), `-` (choices), and `{{` (commands). A backslash at the start of the line prevents this:

```text
[NPC]
\# This is not a comment.
\[Not a speaker]
```

This displays as:

```
NPC: # This is not a comment.
NPC: [Not a speaker]
```

#### Literal Backslash

To include a literal backslash in dialogue text, use a double backslash:

```text
[NPC]
The file is at C:\\Users\\NPC\\Documents.
```

#### Summary

| Escape | Result |
|--------|--------|
| `\{{` | Literal `{{` |
| `\$` | Literal `$` |
| `\##` | Literal `##` |
| `\#` | Literal `#` |
| `\[` | Literal `[` |
| `\-` | Literal `-` |
| `\\` | Literal `\` |