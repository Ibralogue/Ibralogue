### Engine Plugins

Engine plugins let you hook into the dialogue display lifecycle without modifying the engine or views. They are useful for things like showing character portraits, playing audio, or triggering animations per line.

#### How Plugins Work

Any `EnginePlugin` component on the same GameObject as the `SimpleDialogueEngine` is automatically discovered. The engine calls into each plugin at two points:

- `Display(Conversation, int lineIndex)` is called every time a new dialogue line is shown.
- `Clear()` is called when the view is cleared (between lines and at conversation end).

#### Built-in: PortraitImagePlugin

Ibralogue includes a `PortraitImagePlugin` that handles speaker portrait display. Add it to your engine GameObject and assign a Unity UI `Image` component to its `Speaker Portrait` field.

When a dialogue line has a `SpeakerImage` set (via the `{{Image(...)}}` command), the plugin displays it. When there is no image, the portrait is hidden.

#### Creating a Custom Plugin

Subclass `EnginePlugin` and implement the two abstract methods:

```cs
using Ibralogue;
using Ibralogue.Plugins;
using UnityEngine;

public class AudioPlugin : EnginePlugin
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip talkSound;

    public override void Display(Conversation conversation, int lineIndex)
    {
        // Play a sound when each line appears
        audioSource.PlayOneShot(talkSound);
    }

    public override void Clear()
    {
        audioSource.Stop();
    }
}
```

Place the component on the same GameObject as your `SimpleDialogueEngine`. It will be picked up automatically.
