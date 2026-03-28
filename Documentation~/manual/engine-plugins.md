### Engine Plugins

Engine plugins let you hook into the dialogue display lifecycle without modifying the engine or views. They are useful for things like showing character portraits, playing audio, or triggering animations per line.

#### How Plugins Work

Any `EnginePlugin` component on the same GameObject as the dialogue engine is automatically discovered. The engine calls into each plugin at several points during the conversation lifecycle:

| Method | When it fires |
|--------|---------------|
| `Display(Line line)` | Every time a new dialogue line is shown. |
| `Clear()` | When the view is cleared (between lines and at conversation end). |
| `OnConversationStart(Conversation)` | When a conversation begins. |
| `OnConversationEnd()` | When a conversation ends. |
| `OnChoicesPresented(List<Choice>)` | When choices are shown to the player. |
| `OnChoiceSelected(Choice)` | When the player selects a choice. |

`Display` and `Clear` are abstract and must be implemented. The other four methods are virtual with empty default implementations, so override only the ones you need.

#### Built-in: PortraitImagePlugin

Ibralogue includes a `PortraitImagePlugin` that handles speaker portrait display. Add it to your engine GameObject and assign a Unity UI `Image` component to its `Speaker Portrait` field.

The plugin reads the `image` metadata key from each line. When present, it loads the sprite via `Resources.Load` and displays it. When absent, the portrait is hidden.

Set the image in your dialogue file using the `Image` invocation or metadata:

```ibra
[NPC]
{{Image(Portraits/AvaSmiling)}}
Hello!

# Or equivalently:
[NPC]
Hello! ## image:Portraits/AvaSmiling
```

#### Built-in: AudioLinePlugin

`AudioLinePlugin` automatically plays audio when a dialogue line has an `audio` metadata key. The clip ID is passed to the engine's `IAudioProvider`. Playback stops when the view clears.

Add it to your engine GameObject alongside an `IAudioProvider` implementation:

```ibra
[NPC] ## audio:Voiceover/welcome_001
Welcome!
```

The metadata key is configurable via the **Metadata Key** field on the plugin (defaults to `audio`).

#### Audio Provider

Audio playback is handled through the `IAudioProvider` interface. Both the `{{Audio(clipId)}}` invocation and `AudioLinePlugin` use it.

**Built-in: UnityAudioProvider**

Add a `UnityAudioProvider` component (requires an `AudioSource`) to your engine GameObject. Assign it to the engine's `Audio Provider Component` field. Clips are loaded from Resources by path.

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

Subclass `EnginePlugin` and implement the abstract methods. Override any virtual lifecycle methods you need:

```cs
using System.Collections.Generic;
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

    public override void OnChoiceSelected(Choice choice)
    {
        Debug.Log($"Player chose: {choice.ChoiceName}");
    }
}
```

Place the component on the same GameObject as your dialogue engine. It will be picked up automatically.

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
