namespace Ibralogue.Parser
{
	/// <summary>
	/// Represents a position in source text, tracking line, column, and absolute offset.
	/// </summary>
	internal readonly struct SourcePosition
	{
		/// <summary>
		/// The 1-based line number in the source text.
		/// </summary>
		public readonly int Line;

		/// <summary>
		/// The 1-based column number in the source text.
		/// </summary>
		public readonly int Column;

		/// <summary>
		/// The 0-based absolute character offset in the source text.
		/// </summary>
		public readonly int Offset;

		public SourcePosition(int line, int column, int offset)
		{
			Line = line;
			Column = column;
			Offset = offset;
		}

		public override string ToString()
		{
			return $"({Line}:{Column})";
		}
	}

	/// <summary>
	/// Represents a range in source text from a start position to an end position.
	/// </summary>
	internal readonly struct SourceSpan
	{
		public readonly SourcePosition Start;
		public readonly SourcePosition End;

		public SourceSpan(SourcePosition start, SourcePosition end)
		{
			Start = start;
			End = end;
		}

		public override string ToString()
		{
			return $"{Start}-{End}";
		}
	}
}
