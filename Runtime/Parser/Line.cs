using UnityEngine;

namespace Ibralogue.Parser
{
    /// <summary>
    /// The Line struct contains information about an individual line of dialogue.
    /// </summary>
    /// <remarks>
    /// A list of these Line structs makes up a conversation, and a list of conversations makes up a single dialogue file,
    /// due to being able to have multiple conversations in a single file for dialogue branching.
    /// </remarks>
    public class Line : IMetadata
	{
		public string Speaker;
		public LineContent LineContent;
		public Sprite SpeakerImage;

		public bool HasMetadata(string key) =>
			LineContent.Metadata.ContainsKey(key);

		public bool TryGetMetadataValue(string key, out string value)
		{
			if (LineContent.Metadata.ContainsKey(key) && LineContent.Metadata[key] != null)
			{
				value = LineContent.Metadata[key];
				return true;
			}
			value = null;
			return false;
		}
	}
}