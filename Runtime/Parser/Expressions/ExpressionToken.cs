namespace Ibralogue.Parser.Expressions
{
	internal enum ExpressionTokenType
	{
		/// <summary>A variable reference: $Name</summary>
		Variable,

		/// <summary>A string literal: "text"</summary>
		String,

		/// <summary>A numeric literal: 42, 3.14</summary>
		Number,

		/// <summary>A boolean literal: true or false</summary>
		Boolean,

		/// <summary>==</summary>
		Equal,

		/// <summary>!=</summary>
		NotEqual,

		/// <summary>&lt;</summary>
		LessThan,

		/// <summary>&gt;</summary>
		GreaterThan,

		/// <summary>&lt;=</summary>
		LessOrEqual,

		/// <summary>&gt;=</summary>
		GreaterOrEqual,

		/// <summary>+</summary>
		Plus,

		/// <summary>-</summary>
		Minus,

		/// <summary>*</summary>
		Star,

		/// <summary>/</summary>
		Slash,

		/// <summary>AND keyword</summary>
		And,

		/// <summary>OR keyword</summary>
		Or,

		/// <summary>NOT keyword</summary>
		Not,

		/// <summary>(</summary>
		LeftParen,

		/// <summary>)</summary>
		RightParen,

		/// <summary>End of expression input.</summary>
		EndOfInput
	}

	internal readonly struct ExpressionToken
	{
		public readonly ExpressionTokenType Type;
		public readonly string Lexeme;
		public readonly int Position;

		public ExpressionToken(ExpressionTokenType type, string lexeme, int position)
		{
			Type = type;
			Lexeme = lexeme;
			Position = position;
		}

		public override string ToString()
		{
			return $"{Type}(\"{Lexeme}\") at {Position}";
		}
	}
}
