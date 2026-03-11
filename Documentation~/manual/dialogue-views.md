### Dialogue Views

A dialogue view controls how text is presented to the player. Every `SimpleDialogueEngine` requires a view assigned to its `Dialogue View` field.

Ibralogue ships with two built-in views. You can also create your own by subclassing `DialogueViewBase`.

#### TypewriterDialogueView

Reveals text one character at a time, like a typewriter.

| Field | Description |
|-------|-------------|
| `Character Delay` | Seconds between each character reveal. Default: `0.03`. |
| `Character Window` | Number of characters revealed per step. Default: `1`. |

The `OnTypewriterEffectUpdated` event fires each time characters are revealed. This is useful for playing a typing sound effect.

The delay can also be changed at runtime:

```cs
typewriterView.SetCharacterDelay(0.01f);
```

#### PunchDialogueView

Reveals text one word at a time.

| Field | Description |
|-------|-------------|
| `Word Delay` | Seconds between each word reveal. Default: `0.2`. |

The `OnPunchEffectUpdated` event fires each time a new word becomes visible.

```cs
punchView.SetWordDelay(0.1f);
```

#### Skipping the Effect

Both built-in views support skipping the reveal animation. Call `SkipViewEffect()` to instantly show all text for the current line:

```cs
dialogueView.SkipViewEffect();
```

#### Shared Fields

All views inherit these fields from `DialogueViewBase`:

| Field | Description |
|-------|-------------|
| `Name Text` | A TextMeshProUGUI component for displaying the speaker name. |
| `Sentence Text` | A TextMeshProUGUI component for displaying the dialogue text. |
| `Choice Button Holder` | A Transform that serves as the parent for instantiated choice buttons. |
| `Choice Button` | A prefab with a `ChoiceButton` component. Used when choices are displayed. |

The `OnSetView` event fires every time a new dialogue line is displayed. The `OnLineComplete` event fires when the view finishes its display effect.

#### Creating a Custom View

To create your own view, subclass `DialogueViewBase` and override the relevant methods:

```cs
public class MyCustomView : DialogueViewBase
{
    public override void SetView(Conversation conversation, int lineIndex)
    {
        base.SetView(conversation, lineIndex);
        // Your custom display logic here
    }

    public override bool IsStillDisplaying()
    {
        // Return true while your effect is still running
        return false;
    }

    public override void SkipViewEffect()
    {
        // Instantly complete your effect
    }

    public override void ClearView(EnginePlugin[] plugins)
    {
        base.ClearView(plugins);
        // Any additional cleanup
    }
}
```

The engine calls `SetView` when a line should be displayed, waits until `IsStillDisplaying` returns `false`, and then waits for the player to advance. `ClearView` is called before each new line and when the conversation ends.
