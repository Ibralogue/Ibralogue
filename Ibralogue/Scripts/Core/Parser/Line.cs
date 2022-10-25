using System.Collections.Generic;
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
		public Dictionary<string, string> Metadata = new Dictionary<string, string>();

		public bool HasTag(string key) =>
			Metadata.ContainsKey(key);

		public bool TryGetTagValue(string key, out string value)
		{
			if (Metadata.ContainsKey(key) && Metadata[key] != null)
			{
				value = Metadata[key];
				return true;
			}
			value = null;
			return false;
		}
	}
}