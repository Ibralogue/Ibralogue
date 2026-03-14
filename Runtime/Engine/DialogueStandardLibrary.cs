using Ibralogue.Plugins;
using Ibralogue.Views;
using UnityEngine;

namespace Ibralogue
{
	/// <summary>
	/// Built-in dialogue functions that ship with Ibralogue.
	/// These follow the same {{Name(args)}} syntax as the rest of the language
	/// and can be used inline in dialogue text.
	/// </summary>
	public static class DialogueStandardLibrary
	{
		/// <summary>
		/// Changes the speaker portrait. Works both as a standalone command
		/// (stored as metadata, applied at line start) and inline in text
		/// (fires at character position during animated display).
		/// <code>
		/// [NPC]
		/// Hello! {{Image(Portraits/Surprised)}} Whoa!
		/// </code>
		/// </summary>
		[DialogueFunction]
		public static void Image(DialogueEngineBase engine, string path)
		{
			PortraitImagePlugin plugin = engine.GetComponent<PortraitImagePlugin>();
			if (plugin != null)
				plugin.SetImage(path);
		}

		/// <summary>
		/// Plays an audio clip through the engine's audio provider.
		/// <code>
		/// [NPC]
		/// Watch out! {{Audio(SFX/explosion)}}
		/// </code>
		/// </summary>
		[DialogueFunction]
		public static void Audio(DialogueEngineBase engine, string clipId)
		{
			IAudioProvider provider = engine.AudioProvider;
			if (provider != null)
				provider.Play(clipId);
			else
				Debug.LogWarning("[Ibralogue] {{Audio}} called but no IAudioProvider is configured on the engine");
		}

		/// <summary>
		/// Pauses the text animation for the given number of seconds.
		/// <code>
		/// [NPC]
		/// And the winner is... {{Wait(2)}} you!
		/// </code>
		/// </summary>
		[DialogueFunction]
		public static void Wait(DialogueEngineBase engine, float seconds)
		{
			engine.RequestWait(seconds);
		}

		/// <summary>
		/// Changes the typewriter speed. A multiplier of 2 is twice as fast,
		/// 0.5 is half speed. Only affects TypewriterDialogueView.
		/// <code>
		/// [NPC]
		/// This is normal. {{Speed(0.3)}} This... is... slow.
		/// </code>
		/// </summary>
		[DialogueFunction]
		public static void Speed(DialogueEngineBase engine, float multiplier)
		{
			TypewriterDialogueView typewriter =
				engine.GetComponentInChildren<TypewriterDialogueView>();

			if (typewriter != null && multiplier > 0f)
			{
				float baseDelay = typewriter.GetCharacterDelay();
				typewriter.SetCharacterDelay(baseDelay / multiplier);
			}
		}
	}
}
