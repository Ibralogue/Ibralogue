using Ibralogue.Parser;
using UnityEngine;

namespace Ibralogue.Plugins
{
	/// <summary>
	/// Automatically plays audio when a dialogue line has an <c>audio</c> metadata
	/// key. The clip ID from the metadata is passed to the engine's
	/// <see cref="IAudioProvider"/>. Stops playback when the view is cleared.
	/// <code>
	/// [NPC] ## audio:vo_npc_greeting
	/// Hello there, traveler.
	/// </code>
	/// </summary>
	[RequireComponent(typeof(DialogueEngineBase))]
	public class AudioLinePlugin : EnginePlugin
	{
		[Tooltip("The metadata key to read the audio clip ID from.")]
		[SerializeField] private string metadataKey = "audio";

		private DialogueEngineBase _engine;

		private void Awake()
		{
			_engine = GetComponent<DialogueEngineBase>();
		}

		public override void Display(Line line)
		{
			if (line.LineContent.Metadata == null) return;
			if (!line.LineContent.Metadata.TryGetValue(metadataKey, out string clipId)) return;
			if (string.IsNullOrEmpty(clipId)) return;

			IAudioProvider provider = _engine.AudioProvider;
			if (provider != null)
				provider.Play(clipId);
		}

		public override void Clear()
		{
			IAudioProvider provider = _engine.AudioProvider;
			if (provider != null)
				provider.Stop();
		}
	}
}
