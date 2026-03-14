using System.Collections.Generic;

namespace Ibralogue.Parser
{
	/// <summary>
	/// Converts an Ibralogue dialogue file into tokens.
	/// </summary>
	internal class DialogueLexer
	{
		private readonly string _source;
		private readonly DiagnosticBag _diagnostics;

		private int _position;
		private int _line;
		private int _column;

		private readonly List<DialogueToken> _tokens = new List<DialogueToken>();

		public DialogueLexer(string source, DiagnosticBag diagnostics)
		{
			_source = source ?? string.Empty;
			_diagnostics = diagnostics;
			_position = 0;
			_line = 1;
			_column = 1;
		}

		/// <summary>
		/// Tokenizes the entire source text and returns the token list.
		/// </summary>
		public List<DialogueToken> Tokenize()
		{
			_tokens.Clear();
			_position = 0;
			_line = 1;
			_column = 1;

			while (!IsAtEnd())
			{
				TokenizeLine();
			}

			AddToken(DialogueTokenType.EndOfFile, "", "", CurrentPosition());
			return _tokens;
		}

		/// <summary>
		/// Tokenizes a single source line and emits an EndOfLine token at the end.
		/// </summary>
		private void TokenizeLine()
		{
			SkipWhitespace();

			if (IsAtEnd())
				return;

			if (Peek() == '\n')
			{
				EmitEndOfLine();
				return;
			}

			if (Peek() == '\r')
			{
				Advance();
				if (!IsAtEnd() && Peek() == '\n')
				{
					EmitEndOfLine();
					return;
				}
				EmitEndOfLine();
				return;
			}

			char current = Peek();

			if (current == '\\' && IsEscapedLineStart())
			{
				Advance(); // skip the backslash
				TokenizeTextLine();
			}
			else if (current == '#')
			{
				if (PeekNext() == '#')
					TokenizeMetadata();
				else
					TokenizeComment();
			}
			else if (current == '[')
			{
				TokenizeSpeaker();
			}
			else if (current == '-' && LooksLikeChoice())
			{
				TokenizeChoice();
			}
			else if (current == '{' && PeekNext() == '{' && IsCommandLine())
			{
				TokenizeCommand();
			}
			else
			{
				TokenizeTextLine();
			}

			SkipToEndOfLine();
			if (!IsAtEnd())
			{
				EmitEndOfLine();
			}
		}

		/// <summary>
		/// Scans a line comment: # anything until end of line.
		/// </summary>
		private void TokenizeComment()
		{
			SourcePosition start = CurrentPosition();
			int startPos = _position;

			Advance();

			while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r')
				Advance();

			string lexeme = Substring(startPos, _position);
			string value = lexeme.Length > 1 ? lexeme.Substring(1).Trim() : "";
			AddToken(DialogueTokenType.Comment, lexeme, value, start);
		}

		/// <summary>
		/// Scans a metadata annotation: ## key:value key2:value2
		/// </summary>
		private void TokenizeMetadata()
		{
			SourcePosition start = CurrentPosition();
			int startPos = _position;

			// Skip the ## characters
			Advance();
			Advance();

			while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r')
				Advance();

			string lexeme = Substring(startPos, _position);
			string value = lexeme.Length > 2 ? lexeme.Substring(2).Trim() : "";
			AddToken(DialogueTokenType.Metadata, lexeme, value, start);
		}

		/// <summary>
		/// Scans a speaker declaration: [SpeakerName]
		/// </summary>
		private void TokenizeSpeaker()
		{
			SourcePosition start = CurrentPosition();
			int startPos = _position;

			// Skip the opening [
			Advance();

			int nameStart = _position;
			while (!IsAtEnd() && Peek() != ']' && Peek() != '\n' && Peek() != '\r')
				Advance();

			if (IsAtEnd() || Peek() != ']')
			{
				SourceSpan errorSpan = MakeSpan(start);
				_diagnostics.ReportError(errorSpan, "Unterminated speaker name, expected ']'");
				return;
			}

			string name = Substring(nameStart, _position);

			// Skip the closing ]
			Advance();

			string lexeme = Substring(startPos, _position);
			AddToken(DialogueTokenType.Speaker, lexeme, name.Trim(), start);
		}

		/// <summary>
		/// Scans a choice declaration: - ChoiceText -> TargetConversation
		/// </summary>
		private void TokenizeChoice()
		{
			SourcePosition start = CurrentPosition();
			int startPos = _position;

			// Skip the - character
			Advance();

			// Consume the rest of the line (includes "ChoiceText -> Target ## metadata")
			while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r')
				Advance();

			string lexeme = Substring(startPos, _position);
			string value = lexeme.Length > 1 ? lexeme.Substring(1).Trim() : "";
			AddToken(DialogueTokenType.Choice, lexeme, value, start);
		}

		/// <summary>
		/// Scans a built-in command that occupies the entire line: {{CommandName(argument)}} or {{Keyword}}.
		/// Emits specific token types for structural keywords (If, ElseIf, Else, EndIf, Set, Global).
		/// </summary>
		private void TokenizeCommand()
		{
			SourcePosition start = CurrentPosition();
			int startPos = _position;

			Advance();
			Advance();

			int nameStart = _position;
			while (!IsAtEnd() && Peek() != '(' && Peek() != '}' && Peek() != '\n' && Peek() != '\r')
				Advance();

			string commandName = Substring(nameStart, _position).Trim();

			string argument = "";
			if (!IsAtEnd() && Peek() == '(')
			{
				Advance();
				int argStart = _position;
				while (!IsAtEnd() && Peek() != ')' && Peek() != '\n' && Peek() != '\r')
					Advance();

				argument = Substring(argStart, _position);

				if (!IsAtEnd() && Peek() == ')')
					Advance();
				else
					_diagnostics.ReportError(MakeSpan(start), "Unterminated command argument, expected ')'");
			}

			if (!IsAtEnd() && Peek() == '}')
				Advance();
			if (!IsAtEnd() && Peek() == '}')
				Advance();

			string lexeme = Substring(startPos, _position);

			DialogueTokenType tokenType = ResolveCommandTokenType(commandName);
			string value = tokenType == DialogueTokenType.Command
				? (argument.Length > 0 ? $"{commandName}({argument})" : commandName)
				: argument;

			AddToken(tokenType, lexeme, value, start);
		}

		private static DialogueTokenType ResolveCommandTokenType(string commandName)
		{
			switch (commandName)
			{
				case "If": return DialogueTokenType.If;
				case "ElseIf": return DialogueTokenType.ElseIf;
				case "Else": return DialogueTokenType.Else;
				case "EndIf": return DialogueTokenType.EndIf;
				case "Set": return DialogueTokenType.Set;
				case "Global": return DialogueTokenType.Global;
				default: return DialogueTokenType.Command;
			}
		}

		private static bool IsStructuralKeyword(string name)
		{
			switch (name)
			{
				case "ConversationName":
				case "If":
				case "ElseIf":
				case "Else":
				case "EndIf":
				case "Set":
				case "Global":
				case "Jump":
				case "Include":
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Scans a text line, which may contain inline functions ({{Name}}),
		/// variable references ($Name), and trailing metadata (## key:value).
		/// Produces multiple tokens for a single source line.
		/// </summary>
		private void TokenizeTextLine()
		{
			while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r')
			{
				if (Peek() == '\\' && IsEscapedInline())
				{
					TokenizeTextSegment();
					continue;
				}

				if (Peek() == '#' && PeekNext() == '#')
				{
					TokenizeMetadata();
					break;
				}

				if (Peek() == '{' && PeekNext() == '{')
				{
					TokenizeInlineFunction();
					continue;
				}

				if (Peek() == '$')
				{
					TokenizeVariable();
					continue;
				}

				TokenizeTextSegment();
			}
		}

		/// <summary>
		/// Scans an inline function invocation within a text line: {{FunctionName}}
		/// </summary>
		private void TokenizeInlineFunction()
		{
			SourcePosition start = CurrentPosition();
			int startPos = _position;

			Advance();
			Advance();

			int nameStart = _position;
			while (!IsAtEnd() && !(Peek() == '}' && PeekNext() == '}') && Peek() != '\n' && Peek() != '\r')
				Advance();

			string name = Substring(nameStart, _position).Trim();

			if (!IsAtEnd() && Peek() == '}' && PeekNext() == '}')
			{
				Advance();
				Advance();
			}
			else
			{
				_diagnostics.ReportError(MakeSpan(start), "Unterminated function invocation, expected '}}'");
			}

			string lexeme = Substring(startPos, _position);
			AddToken(DialogueTokenType.Function, lexeme, name, start);
		}

		/// <summary>
		/// Scans a global variable reference: $VariableName
		/// </summary>
		private void TokenizeVariable()
		{
			SourcePosition start = CurrentPosition();
			int startPos = _position;

			// Skip the $
			Advance();

			int nameStart = _position;
			while (!IsAtEnd() && IsAlphanumeric(Peek()))
				Advance();

			string name = Substring(nameStart, _position);
			string lexeme = Substring(startPos, _position);

			if (name.Length == 0)
			{
				_diagnostics.ReportWarning(MakeSpan(start), "Empty variable name after '$'");
			}

			AddToken(DialogueTokenType.Variable, lexeme, name, start);
		}

		/// <summary>
		/// Scans a plain text segment until the next special character or end of line.
		/// </summary>
		private void TokenizeTextSegment()
		{
			SourcePosition start = CurrentPosition();
			int startPos = _position;

			while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r')
			{
				char c = Peek();

				// Backslash escape: skip the \ and consume the next character(s) as text
				if (c == '\\' && IsEscapedInline())
				{
					Advance(); // skip the backslash
					Advance(); // consume the escaped character
					continue;
				}

				if (c == '{' && PeekNext() == '{')
					break;
				if (c == '$' && IsAlphanumeric(PeekNext()))
					break;
				if (c == '#' && PeekNext() == '#')
					break;

				Advance();
			}

			string lexeme = Substring(startPos, _position);
			string text = StripEscapeBackslashes(lexeme);
			if (text.Length > 0)
			{
				AddToken(DialogueTokenType.Text, lexeme, text, start);
			}
		}

		/// <summary>
		/// Checks whether a backslash at the current position is escaping a
		/// reserved line-start sequence (#, ##, [, -, or {{).
		/// </summary>
		private bool IsEscapedLineStart()
		{
			if (_position + 1 >= _source.Length)
				return false;

			char next = _source[_position + 1];
			return next == '#' || next == '[' || next == '-' || (next == '{' && _position + 2 < _source.Length && _source[_position + 2] == '{');
		}

		/// <summary>
		/// Checks whether a backslash at the current position is escaping an
		/// inline reserved sequence ({{, $, or ##).
		/// </summary>
		private bool IsEscapedInline()
		{
			if (_position + 1 >= _source.Length)
				return false;

			char next = _source[_position + 1];

			if (next == '{' && _position + 2 < _source.Length && _source[_position + 2] == '{')
				return true;
			if (next == '$')
				return true;
			if (next == '#' && _position + 2 < _source.Length && _source[_position + 2] == '#')
				return true;

			return false;
		}

		/// <summary>
		/// Checks whether the current line looks like a choice (contains ->).
		/// Peeks ahead without advancing the position.
		/// </summary>
		private bool LooksLikeChoice()
		{
			int peekPos = _position + 1;
			while (peekPos < _source.Length && _source[peekPos] != '\n' && _source[peekPos] != '\r')
			{
				if (_source[peekPos] == '-' && peekPos + 1 < _source.Length && _source[peekPos + 1] == '>')
					return true;
				peekPos++;
			}
			return false;
		}

		/// <summary>
		/// Checks whether the current line is a standalone structural keyword.
		/// Only known keywords (ConversationName, If, Set, Jump, etc.) on their
		/// own line are treated as commands. Everything else falls through to
		/// text/function handling.
		/// </summary>
		private bool IsCommandLine()
		{
			int peekPos = _position + 2; // skip {{

			int nameStart = peekPos;
			while (peekPos < _source.Length && _source[peekPos] != '(' && _source[peekPos] != '}'
				   && _source[peekPos] != '\n' && _source[peekPos] != '\r')
				peekPos++;

			int nameEnd = peekPos;

			if (peekPos < _source.Length && _source[peekPos] == '(')
			{
				while (peekPos < _source.Length && _source[peekPos] != ')' && _source[peekPos] != '\n')
					peekPos++;
				if (peekPos < _source.Length && _source[peekPos] == ')')
					peekPos++;
			}

			if (peekPos + 1 < _source.Length && _source[peekPos] == '}' && _source[peekPos + 1] == '}')
			{
				peekPos += 2;

				while (peekPos < _source.Length && _source[peekPos] == ' ')
					peekPos++;

				bool isEndOfLine = peekPos >= _source.Length || _source[peekPos] == '\n' || _source[peekPos] == '\r';
				if (!isEndOfLine)
					return false;

				string name = _source.Substring(nameStart, nameEnd - nameStart).Trim();
				return IsStructuralKeyword(name);
			}

			return false;
		}

		private void EmitEndOfLine()
		{
			SourcePosition pos = CurrentPosition();
			if (!IsAtEnd() && Peek() == '\r')
				Advance();
			if (!IsAtEnd() && Peek() == '\n')
			{
				Advance();
				AddToken(DialogueTokenType.EndOfLine, "\n", "", pos);
			}
			else
			{
				AddToken(DialogueTokenType.EndOfLine, "", "", pos);
			}
			_line++;
			_column = 1;
		}

		private void SkipWhitespace()
		{
			while (!IsAtEnd() && (Peek() == ' ' || Peek() == '\t'))
				Advance();
		}

		private void SkipToEndOfLine()
		{
			while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r')
				Advance();
		}

		private bool IsAtEnd()
		{
			return _position >= _source.Length;
		}

		private char Peek()
		{
			if (IsAtEnd()) return '\0';
			return _source[_position];
		}

		private char PeekNext()
		{
			if (_position + 1 >= _source.Length) return '\0';
			return _source[_position + 1];
		}

		private char Advance()
		{
			char c = _source[_position];
			_position++;
			_column++;
			return c;
		}

		private SourcePosition CurrentPosition()
		{
			return new SourcePosition(_line, _column, _position);
		}

		private SourceSpan MakeSpan(SourcePosition start)
		{
			return new SourceSpan(start, CurrentPosition());
		}

		private string Substring(int startIndex, int endIndex)
		{
			if (endIndex <= startIndex) return "";
			return _source.Substring(startIndex, endIndex - startIndex);
		}

		private void AddToken(DialogueTokenType type, string lexeme, string value, SourcePosition start)
		{
			SourceSpan span = new SourceSpan(start, CurrentPosition());
			_tokens.Add(new DialogueToken(type, lexeme, value, span));
		}

		/// <summary>
		/// Removes backslash escape characters that precede reserved sequences
		/// ({{, $, ##) from the given text. A literal backslash can be written as \\.
		/// </summary>
		private static string StripEscapeBackslashes(string text)
		{
			if (text.IndexOf('\\') < 0)
				return text;

			System.Text.StringBuilder sb = new System.Text.StringBuilder(text.Length);
			for (int i = 0; i < text.Length; i++)
			{
				if (text[i] == '\\' && i + 1 < text.Length)
				{
					char next = text[i + 1];
					// Strip backslash before reserved sequences
					if (next == '{' || next == '$' || next == '#' || next == '\\')
					{
						continue; // skip the backslash, the next char will be appended normally
					}
				}
				sb.Append(text[i]);
			}
			return sb.ToString();
		}

		private static bool IsAlphanumeric(char c)
		{
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
		}
	}
}
