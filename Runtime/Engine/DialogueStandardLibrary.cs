using Ibralogue.Plugins;
using Ibralogue.Views;
using UnityEngine;

namespace Ibralogue
{
	/// <summary>
	/// Standard invocations that ship with Ibralogue.
	/// These follow the same {{Name(args)}} syntax as the rest of the language
	/// and can be used inline in dialogue text.
	/// </summary>
	public static class DialogueStandardLibrary
	{
		/// <summary>
		/// Changes the speaker portrait. On its own line, fires at line start.
		/// Inline in text, fires at that position.
		/// <code>
		/// [NPC]
		/// Hello! {{Image(Portraits/Surprised)}} Whoa!
		/// </code>
		/// </summary>
		[DialogueInvocation]
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
		[DialogueInvocation]
		public static void Audio(DialogueEngineBase engine, string clipId)
		{
			IAudioProvider provider = engine.AudioProvider;
			if (provider != null)
				provider.Play(clipId);
			else
				DialogueLogger.LogWarning("{{Audio}} called but no IAudioProvider is configured on the engine");
		}

		/// <summary>
		/// Pauses the text animation for the given number of seconds.
		/// <code>
		/// [NPC]
		/// And the winner is... {{Wait(2)}} you!
		/// </code>
		/// </summary>
		[DialogueInvocation]
		public static void Wait(DialogueEngineBase engine, float seconds)
		{
			engine.RequestWait(seconds);
		}

		/// <summary>
		/// Changes the text reveal speed for the current line. A multiplier of 2
		/// is twice as fast, 0.5 is half speed. Works with any animated view.
		/// Speed resets to the configured default on the next line.
		/// <code>
		/// [NPC]
		/// This is normal. {{Speed(0.3)}} This... is... slow.
		/// </code>
		/// </summary>
		[DialogueInvocation]
		public static void Speed(DialogueEngineBase engine, float multiplier)
		{
			DialogueViewBase view = engine.GetComponentInChildren<DialogueViewBase>();
			if (view != null)
				view.SetSpeed(multiplier);
		}

		/// <summary>
		/// Pauses the dialogue engine. The conversation halts until
		/// <c>ResumeConversation()</c> is called from code. Use this to
		/// hand control to an external system (cutscene, animation, minigame)
		/// and resume when it finishes.
		/// <code>
		/// [NPC]
		/// {{PauseEngine}}
		/// Watch this!
		/// </code>
		/// </summary>
		[DialogueInvocation]
		public static void PauseEngine(DialogueEngineBase engine)
		{
			engine.PauseConversation();
		}

		/// <summary>
		/// Resumes a paused dialogue engine from within dialogue.
		/// Typically not needed since external systems call
		/// <c>ResumeConversation()</c> directly, but available for
		/// cases where dialogue itself should trigger the resume
		/// (e.g., after a timed wait).
		/// </summary>
		[DialogueInvocation]
		public static void ResumeEngine(DialogueEngineBase engine)
		{
			engine.ResumeConversation();
		}

		/// <summary>
		/// Marks a key as visited. Check from dialogue with
		/// <c>{{If(Visited("Tavern"))}}</c> or from code with
		/// <c>VisitTracker.HasVisited("Tavern")</c>.
		/// <code>
		/// [NPC]
		/// {{MarkVisited(Tavern)}}
		/// Welcome to the tavern!
		/// </code>
		/// </summary>
		[DialogueInvocation]
		public static void MarkVisited(string key)
		{
			VisitTracker.Mark(key);
		}

		/// <summary>
		/// Returns true if the given key has been marked as visited.
		/// Intended for use in conditionals.
		/// <code>
		/// {{If(Visited("Tavern"))}}
		/// [NPC]
		/// You've been here before.
		/// {{EndIf}}
		/// </code>
		/// </summary>
		[DialogueInvocation]
		public static bool Visited(string key)
		{
			return VisitTracker.HasVisited(key);
		}
	}
}
