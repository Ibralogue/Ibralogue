using System.Collections.Generic;
using System.Globalization;

namespace Ibralogue.Parser.Expressions
{
	/// <summary>
	/// Recursive-descent parser that converts a list of expression tokens into an AST.
	///
	/// Precedence (lowest to highest):
	///   OR
	///   AND
	///   NOT
	///   == != &lt; &gt; &lt;= &gt;=
	///   + -
	///   * /
	///   unary - (negation)
	///   primary (literals, variables, parenthesized groups)
	/// </summary>
	internal class ExpressionParser
	{
		private readonly List<ExpressionToken> _tokens;
		private int _position;

		public ExpressionParser(List<ExpressionToken> tokens)
		{
			_tokens = tokens;
			_position = 0;
		}

		/// <summary>
		/// Parses the token list into an expression AST. Throws <see cref="System.Exception"/>
		/// on malformed input.
		/// </summary>
		public ExpressionNode Parse()
		{
			ExpressionNode node = ParseOr();

			if (Current().Type != ExpressionTokenType.EndOfInput)
				throw new System.Exception($"Unexpected token '{Current().Lexeme}' at position {Current().Position}");

			return node;
		}

		private ExpressionNode ParseOr()
		{
			ExpressionNode left = ParseAnd();

			while (Current().Type == ExpressionTokenType.Or)
			{
				Advance();
				ExpressionNode right = ParseAnd();
				left = new BinaryNode(left, ExpressionTokenType.Or, right);
			}

			return left;
		}

		private ExpressionNode ParseAnd()
		{
			ExpressionNode left = ParseNot();

			while (Current().Type == ExpressionTokenType.And)
			{
				Advance();
				ExpressionNode right = ParseNot();
				left = new BinaryNode(left, ExpressionTokenType.And, right);
			}

			return left;
		}

		private ExpressionNode ParseNot()
		{
			if (Current().Type == ExpressionTokenType.Not)
			{
				Advance();
				ExpressionNode operand = ParseNot();
				return new UnaryNode(ExpressionTokenType.Not, operand);
			}

			return ParseComparison();
		}

		private ExpressionNode ParseComparison()
		{
			ExpressionNode left = ParseAdditive();

			ExpressionTokenType type = Current().Type;
			if (type == ExpressionTokenType.Equal || type == ExpressionTokenType.NotEqual ||
				type == ExpressionTokenType.LessThan || type == ExpressionTokenType.GreaterThan ||
				type == ExpressionTokenType.LessOrEqual || type == ExpressionTokenType.GreaterOrEqual)
			{
				Advance();
				ExpressionNode right = ParseAdditive();
				left = new BinaryNode(left, type, right);
			}

			return left;
		}

		private ExpressionNode ParseAdditive()
		{
			ExpressionNode left = ParseMultiplicative();

			while (Current().Type == ExpressionTokenType.Plus ||
				   Current().Type == ExpressionTokenType.Minus)
			{
				ExpressionTokenType op = Current().Type;
				Advance();
				ExpressionNode right = ParseMultiplicative();
				left = new BinaryNode(left, op, right);
			}

			return left;
		}

		private ExpressionNode ParseMultiplicative()
		{
			ExpressionNode left = ParseUnary();

			while (Current().Type == ExpressionTokenType.Star ||
				   Current().Type == ExpressionTokenType.Slash)
			{
				ExpressionTokenType op = Current().Type;
				Advance();
				ExpressionNode right = ParseUnary();
				left = new BinaryNode(left, op, right);
			}

			return left;
		}

		private ExpressionNode ParseUnary()
		{
			if (Current().Type == ExpressionTokenType.Minus)
			{
				Advance();
				ExpressionNode operand = ParseUnary();
				return new UnaryNode(ExpressionTokenType.Minus, operand);
			}

			return ParsePrimary();
		}

		private ExpressionNode ParsePrimary()
		{
			ExpressionToken token = Current();

			switch (token.Type)
			{
				case ExpressionTokenType.Number:
					Advance();
					double number = double.Parse(token.Lexeme, CultureInfo.InvariantCulture);
					return new LiteralNode(number);

				case ExpressionTokenType.String:
					Advance();
					string text = token.Lexeme;
					if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"')
						text = text.Substring(1, text.Length - 2);
					return new LiteralNode(text);

				case ExpressionTokenType.Boolean:
					Advance();
					return new LiteralNode(token.Lexeme == "true");

				case ExpressionTokenType.Variable:
					Advance();
					string name = token.Lexeme;
					if (name.Length > 0 && name[0] == '$')
						name = name.Substring(1);
					return new VariableNode(name);

				case ExpressionTokenType.LeftParen:
					Advance();
					ExpressionNode expr = ParseOr();
					if (Current().Type != ExpressionTokenType.RightParen)
						throw new System.Exception($"Expected ')' at position {Current().Position}");
					Advance();
					return expr;

				default:
					throw new System.Exception($"Unexpected token '{token.Lexeme}' at position {token.Position}");
			}
		}

		private ExpressionToken Current()
		{
			if (_position >= _tokens.Count)
				return new ExpressionToken(ExpressionTokenType.EndOfInput, "", _position);
			return _tokens[_position];
		}

		private void Advance()
		{
			_position++;
		}
	}
}
