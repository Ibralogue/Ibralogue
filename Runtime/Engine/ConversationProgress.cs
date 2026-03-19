using System;

namespace Ibralogue
{
	/// <summary>
	/// A serializable snapshot of the engine's position within a conversation.
	/// Use with <see cref="DialogueEngineBase.ExportProgress"/> and
	/// <see cref="DialogueEngineBase.ResumeFromProgress"/> to save and restore
	/// mid-conversation state.
	/// </summary>
	[Serializable]
	public class ConversationProgress
	{
		/// <summary>
		/// The name of the DialogueAsset that was playing.
		/// </summary>
		public string AssetName;

		/// <summary>
		/// The name of the active conversation within the asset.
		/// </summary>
		public string ConversationName;

		/// <summary>
		/// How many displayable nodes (lines and choice points) had been
		/// processed when the snapshot was taken.
		/// </summary>
		public int DisplayedNodeCount;
	}
}
