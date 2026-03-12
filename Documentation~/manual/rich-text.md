### Rich Text

Ibralogue supports TextMeshPro rich text tags in dialogue text. Tags are passed through to the TMP component as-is, so any tag that TextMeshPro supports will work.

#### Example

```text
[NPC]
Why don't we go <color=yellow>fishing</color>?
[NPC]
That sounds <b>great</b>!
```

The word "fishing" will appear in yellow, and "great" will appear in bold.

#### Common Tags

| Tag | Effect |
|-----|--------|
| `<b>text</b>` | Bold |
| `<i>text</i>` | Italic |
| `<color=red>text</color>` | Colored text |
| `<color=#FF0000>text</color>` | Colored text (hex) |
| `<size=24>text</size>` | Font size |
| `<sprite name="icon">` | Inline sprite |

For a full list of supported tags, see the [TextMeshPro documentation](https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/RichText.html/).

#### Interaction with Views

Rich text works with all built-in views. The TypewriterDialogueView and PunchDialogueView both use TMP's `maxVisibleCharacters` for their reveal effects, which means tags are applied correctly even while text is being revealed. A partially revealed `<color=red>` word will still appear red.
