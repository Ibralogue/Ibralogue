namespace Ibralogue.Parser
{
	/// <summary>
	/// The type of a lexed token.
	/// </summary>
	internal enum DialogueTokenType
	{
		/// <summary>A speaker declaration: [SpeakerName]</summary>
		Speaker,

		/// <summary>Plain text content on a line.</summary>
		Text,

		/// <summary>An inline function invocation: {{FunctionName}}</summary>
		Function,

		/// <summary>A built-in command invocation: {{Command(argument)}}</summary>
		Command,

		/// <summary>A choice declaration: - ChoiceText -> Target</summary>
		Choice,

		/// <summary>A line comment: # comment text</summary>
		Comment,

		/// <summary>A metadata annotation: ## key:value</summary>
		Metadata,

		/// <summary>A global variable reference: $VariableName</summary>
		Variable,

		/// <summary>End of a source line.</summary>
		EndOfLine,

		/// <summary>End of the source text.</summary>
		EndOfFile
	}

	/// <summary>
	/// A token produced by the lexer, carrying its type, text value, and source location.
	/// </summary>
	internal readonly struct DialogueToken
	{
		public readonly DialogueTokenType Type;

		/// <summary>
		/// The raw text of the token as it appeared in source.
		/// </summary>
		public readonly string Lexeme;

		/// <summary>
		/// The extracted value of the token (e.g. speaker name without brackets,
		/// function name without braces, variable name without $).
		/// </summary>
		public readonly string Value;

		/// <summary>
		/// The source location of this token.
		/// </summary>
		public readonly SourceSpan Span;

		public DialogueToken(DialogueTokenType type, string lexeme, string value, SourceSpan span)
		{
			Type = type;
			Lexeme = lexeme;
			Value = value;
			Span = span;
		}

		public override string ToString()
		{
			return $"{Type}(\"{Value}\") at {Span}";
		}
	}
}
