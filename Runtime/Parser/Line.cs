namespace Ibralogue.Parser
{
    /// <summary>
    /// Contains information about an individual line of dialogue, including
    /// its speaker, text content, metadata, and optional jump target.
    /// </summary>
    public class Line : IMetadata
	{
		public string Speaker { get; internal set; }
		public LineContent LineContent { get; internal set; }
		public string JumpTarget { get; internal set; }

		/// <summary>
		/// When true, the engine processes this line (invocations, metadata) without
		/// displaying it in the dialogue view. Set by the [>>] speaker syntax.
		/// </summary>
		public bool Silent { get; internal set; }

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
