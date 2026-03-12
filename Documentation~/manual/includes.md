### Includes

The `Include` directive lets you pull content from other `.ibra` files into the current one. This is useful for reusing common conversations, like shared choice branches, across multiple dialogue files.

#### Setup

Included files are loaded using [Resources.Load](https://docs.unity3d.com/ScriptReference/Resources.Load.html), so they must be placed in a `Resources` folder somewhere in your project. For example, a file at `Assets/Dialogue/Resources/SharedChoices.ibra` would be referenced as `SharedChoices`.

#### Including an Entire File

To include all the content of another dialogue file, use `{{Include(...)}}` on its own line:

```text
[NPC]
How are you?
- Fine -> FineResponse
- Terrible -> TerribleResponse

{{Include(SharedChoices)}}
```

The contents of `SharedChoices.ibra` are inserted in place of the `{{Include(...)}}` line, as if you had copied and pasted them.

#### Including a Specific Conversation

If the included file contains multiple conversations and you only need one, pass the conversation name as a second argument:

```text
{{Include(SharedChoices, FineResponse)}}
```

This will only pull in the `FineResponse` conversation block from the file.

#### Recursive Includes

Included files can themselves contain `{{Include(...)}}` directives. Ibralogue will expand them recursively.

Circular includes (where file A includes file B which includes file A) are detected and reported as an error.

#### Example

`SharedChoices.ibra` (in a Resources folder):

```text
{{ConversationName(FineResponse)}}
[NPC]
Good to hear!

{{ConversationName(TerribleResponse)}}
[NPC]
I'm sorry. Hope things get better.
```

`main_dialogue.ibra`:

```text
{{ConversationName(Greeting)}}
[NPC]
Hello, traveller. How are you?
- Fine -> FineResponse
- Terrible -> TerribleResponse

{{Include(SharedChoices)}}
```

The result is the same as if you had written all three conversations in one file.
