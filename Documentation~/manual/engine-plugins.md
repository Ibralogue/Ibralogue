### Engine Plugins

Engine plugins let you hook into the dialogue display lifecycle without modifying the engine or views. They are useful for things like showing character portraits, playing audio, or triggering animations per line.

#### How Plugins Work

Any `EnginePlugin` component on the same GameObject as the `SimpleDialogueEngine` is automatically discovered. The engine calls into each plugin at two points:

- `Display(Line line)` is called every time a new dialogue line is shown. The `Line` object contains the speaker, text, metadata, and other properties for the current line.
- `Clear()` is called when the view is cleared (between lines and at conversation end).

#### Built-in: PortraitImagePlugin

Ibralogue includes a `PortraitImagePlugin` that handles speaker portrait display. Add it to your engine GameObject and assign a Unity UI `Image` component to its `Speaker Portrait` field.

The plugin reads the `image` metadata key from each line. When present, it loads the sprite via `Resources.Load` and displays it. When absent, the portrait is hidden.

Set the image in your dialogue file using the `Image` command or metadata:

```text
[NPC]
{{Image(Portraits/AvaSmiling)}}
Hello!

# Or equivalently:
[NPC]
Hello! ## image:Portraits/AvaSmiling
```

#### Audio Provider

Ibralogue supports per-line audio playback through the `IAudioProvider` interface. When a line has an `audio` metadata key, the engine calls `Play()` on the active provider.

**Built-in: UnityAudioProvider**

Add a `UnityAudioProvider` component (requires an `AudioSource`) to your engine GameObject. Assign it to the engine's `Audio Provider Component` field. Clips are loaded from Resources by path:

```text
[NPC]
Welcome! ## audio:Voiceover/welcome_001
```

**Custom audio backends (FMOD, Wwise, etc.)**

Implement `IAudioProvider` for your audio system:

```cs
using Ibralogue;
using UnityEngine;

public class FmodAudioProvider : MonoBehaviour, IAudioProvider
{
    public void Play(string clipId)
    {
        // Play using your audio system
        FMODUnity.RuntimeManager.PlayOneShot("event:/" + clipId);
    }

    public void Stop()
    {
        // Stop playback
    }
}
```

Assign the component to the engine's `Audio Provider Component` field.

#### Creating a Custom Plugin

Subclass `EnginePlugin` and implement the two abstract methods:

```cs
using Ibralogue.Parser;
using Ibralogue.Plugins;
using UnityEngine;

public class AudioPlugin : EnginePlugin
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip talkSound;

    public override void Display(Line line)
    {
        audioSource.PlayOneShot(talkSound);
    }

    public override void Clear()
    {
        audioSource.Stop();
    }
}
```

Place the component on the same GameObject as your `SimpleDialogueEngine`. It will be picked up automatically.

#### Per-Character Typing Sounds

The `TypewriterDialogueView` fires an `OnTypewriterEffectUpdated` event each time characters are revealed. Subscribe to this event to play per-speaker typing sounds:

```cs
typewriterView.OnTypewriterEffectUpdated.AddListener(() =>
{
    // Play a typing sound per character reveal
    // Use the current speaker to vary the sound
    audioSource.PlayOneShot(typingClip);
});
```
