using System.Collections.Generic;

namespace Ibralogue.Parser.Expressions
{
	/// <summary>
	/// Tokenizes an expression string (the content inside an {{If(...)}} or {{Set(..., ...)}} invocation)
	/// into a flat list of expression tokens.
	/// </summary>
	internal class ExpressionLexer
	{
		private readonly string _source;
		private int _position;
		private readonly List<ExpressionToken> _tokens = new List<ExpressionToken>();

		public ExpressionLexer(string source)
		{
			_source = source ?? string.Empty;
		}

		public List<ExpressionToken> Tokenize()
		{
			_tokens.Clear();
			_position = 0;

			while (!IsAtEnd())
			{
				SkipWhitespace();
				if (IsAtEnd())
					break;

				char c = Peek();

				if (c == '$')
				{
					ReadVariable();
				}
				else if (c == '"')
				{
					ReadString();
				}
				else if (IsDigit(c) || (c == '.' && !IsAtEnd(1) && IsDigit(PeekAt(1))))
				{
					ReadNumber();
				}
				else if (c == '=' && PeekAt(1) == '=')
				{
					AddToken(ExpressionTokenType.Equal, "==");
					_position += 2;
				}
				else if (c == '!' && PeekAt(1) == '=')
				{
					AddToken(ExpressionTokenType.NotEqual, "!=");
					_position += 2;
				}
				else if (c == '<' && PeekAt(1) == '=')
				{
					AddToken(ExpressionTokenType.LessOrEqual, "<=");
					_position += 2;
				}
				else if (c == '>' && PeekAt(1) == '=')
				{
					AddToken(ExpressionTokenType.GreaterOrEqual, ">=");
					_position += 2;
				}
				else if (c == '<')
				{
					AddToken(ExpressionTokenType.LessThan, "<");
					_position++;
				}
				else if (c == '>')
				{
					AddToken(ExpressionTokenType.GreaterThan, ">");
					_position++;
				}
				else if (c == '+')
				{
					AddToken(ExpressionTokenType.Plus, "+");
					_position++;
				}
				else if (c == '-')
				{
					AddToken(ExpressionTokenType.Minus, "-");
					_position++;
				}
				else if (c == '*')
				{
					AddToken(ExpressionTokenType.Star, "*");
					_position++;
				}
				else if (c == '/')
				{
					AddToken(ExpressionTokenType.Slash, "/");
					_position++;
				}
				else if (c == '(')
				{
					AddToken(ExpressionTokenType.LeftParen, "(");
					_position++;
				}
				else if (c == ')')
				{
					AddToken(ExpressionTokenType.RightParen, ")");
					_position++;
				}
				else if (IsAlpha(c))
				{
					ReadKeywordOrBool();
				}
				else
				{
					_position++;
				}
			}

			_tokens.Add(new ExpressionToken(ExpressionTokenType.EndOfInput, "", _position));
			return _tokens;
		}

		private void ReadVariable()
		{
			int start = _position;
			_position++; // skip $

			while (!IsAtEnd() && IsAlphanumeric(Peek()))
				_position++;

			string lexeme = _source.Substring(start, _position - start);
			_tokens.Add(new ExpressionToken(ExpressionTokenType.Variable, lexeme, start));
		}

		private void ReadString()
		{
			int start = _position;
			_position++; // skip opening "

			while (!IsAtEnd() && Peek() != '"')
			{
				if (Peek() == '\\' && !IsAtEnd(1))
					_position++; // skip escaped character
				_position++;
			}

			if (!IsAtEnd())
				_position++; // skip closing "

			string lexeme = _source.Substring(start, _position - start);
			_tokens.Add(new ExpressionToken(ExpressionTokenType.String, lexeme, start));
		}

		private void ReadNumber()
		{
			int start = _position;
			bool hasDot = false;

			while (!IsAtEnd() && (IsDigit(Peek()) || Peek() == '.'))
			{
				if (Peek() == '.')
				{
					if (hasDot) break;
					hasDot = true;
				}
				_position++;
			}

			string lexeme = _source.Substring(start, _position - start);
			_tokens.Add(new ExpressionToken(ExpressionTokenType.Number, lexeme, start));
		}

		private void ReadKeywordOrBool()
		{
			int start = _position;

			while (!IsAtEnd() && IsAlphanumeric(Peek()))
				_position++;

			string word = _source.Substring(start, _position - start);

			switch (word)
			{
				case "AND":
					_tokens.Add(new ExpressionToken(ExpressionTokenType.And, word, start));
					break;
				case "OR":
					_tokens.Add(new ExpressionToken(ExpressionTokenType.Or, word, start));
					break;
				case "NOT":
					_tokens.Add(new ExpressionToken(ExpressionTokenType.Not, word, start));
					break;
				case "true":
				case "false":
					_tokens.Add(new ExpressionToken(ExpressionTokenType.Boolean, word, start));
					break;
				default:
					_tokens.Add(new ExpressionToken(ExpressionTokenType.Variable, word, start));
					break;
			}
		}

		private void AddToken(ExpressionTokenType type, string lexeme)
		{
			_tokens.Add(new ExpressionToken(type, lexeme, _position));
		}

		private void SkipWhitespace()
		{
			while (!IsAtEnd() && (_source[_position] == ' ' || _source[_position] == '\t'))
				_position++;
		}

		private bool IsAtEnd()
		{
			return _position >= _source.Length;
		}

		private bool IsAtEnd(int offset)
		{
			return _position + offset >= _source.Length;
		}

		private char Peek()
		{
			return _source[_position];
		}

		private char PeekAt(int offset)
		{
			int index = _position + offset;
			return index < _source.Length ? _source[index] : '\0';
		}

		private static bool IsDigit(char c)
		{
			return c >= '0' && c <= '9';
		}

		private static bool IsAlpha(char c)
		{
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
		}

		private static bool IsAlphanumeric(char c)
		{
			return IsAlpha(c) || IsDigit(c);
		}
	}
}
