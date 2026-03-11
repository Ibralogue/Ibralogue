using System.Collections.Generic;

namespace Ibralogue.Parser
{
	/// <summary>
	/// Recursive descent parser that converts a flat token stream into an AST.
	/// The parser is purely structural — it does not perform variable substitution,
	/// resource loading, or any other side effects.
	/// </summary>
	internal class DialogueAstParser
	{
		private readonly List<DialogueToken> _tokens;
		private readonly DiagnosticBag _diagnostics;
		private int _position;

		public DialogueAstParser(List<DialogueToken> tokens, DiagnosticBag diagnostics)
		{
			_tokens = tokens;
			_diagnostics = diagnostics;
			_position = 0;
		}

		/// <summary>
		/// Parses the token stream into a DialogueDocument.
		/// </summary>
		public DialogueDocument Parse()
		{
			SourcePosition start = Current().Span.Start;
			List<ConversationNode> conversations = new List<ConversationNode>();

			SkipBlankLines();

			// Parse conversations until end of file
			while (!IsAtEnd())
			{
				ConversationNode conversation = ParseConversation(conversations.Count == 0);
				if (conversation != null)
					conversations.Add(conversation);
			}

			SourceSpan span = new SourceSpan(start, Previous().Span.End);
			return new DialogueDocument(conversations, span);
		}

		/// <summary>
		/// Parses a single conversation block.
		/// A conversation starts with an optional {{ConversationName(name)}} command
		/// and contains one or more dialogue lines and optional choices.
		/// </summary>
		private ConversationNode ParseConversation(bool isFirst)
		{
			SourcePosition start = Current().Span.Start;
			string name = "Default";

			// Check for conversation name command
			if (Check(DialogueTokenType.Command))
			{
				DialogueToken command = Current();
				string commandName = ExtractCommandName(command.Value);

				if (commandName == "ConversationName" || commandName == "DialogueName")
				{
					name = ExtractCommandArgument(command.Value);
					Advance();
					SkipBlankLines();
				}
			}

			List<DialogueLineNode> lines = new List<DialogueLineNode>();
			List<ChoiceNode> choices = new List<ChoiceNode>();

			// Parse dialogue lines until we hit another conversation, a choice block, or end of file
			while (!IsAtEnd())
			{
				SkipBlankLines();
				if (IsAtEnd())
					break;

				// Skip standalone comments at the conversation level
				if (Check(DialogueTokenType.Comment))
				{
					Advance();
					continue;
				}

				// Check if we've reached the start of a new conversation
				if (Check(DialogueTokenType.Command))
				{
					string cmdName = ExtractCommandName(Current().Value);
					if (cmdName == "ConversationName" || cmdName == "DialogueName")
						break;
				}

				// Parse choices
				if (Check(DialogueTokenType.Choice))
				{
					while (Check(DialogueTokenType.Choice))
					{
						ChoiceNode choice = ParseChoice(lines.Count);
						if (choice != null)
							choices.Add(choice);
						SkipBlankLines();
					}
					continue;
				}

				// Parse a dialogue line (speaker + sentences)
				if (Check(DialogueTokenType.Speaker))
				{
					DialogueLineNode dialogueLine = ParseDialogueLine();
					if (dialogueLine != null)
						lines.Add(dialogueLine);
					continue;
				}

				// Handle command lines within a conversation (Image is handled inside ParseDialogueLine)
				if (Check(DialogueTokenType.Command))
				{
					_diagnostics.ReportWarning(Current().Span,
						$"Unexpected command outside of a dialogue line: {Current().Value}");
					Advance();
					SkipBlankLines();
					continue;
				}

				// Unexpected token — skip with warning
				if (!Check(DialogueTokenType.EndOfFile))
				{
					_diagnostics.ReportWarning(Current().Span,
						$"Unexpected content outside of a dialogue line: '{Current().Lexeme}'");
					SkipLine();
					continue;
				}

				break;
			}

			SourceSpan span = new SourceSpan(start, Previous().Span.End);
			return new ConversationNode(name, lines, choices, span);
		}

		/// <summary>
		/// Parses a dialogue line: a [Speaker], followed by optional Image command,
		/// and one or more sentence lines.
		/// </summary>
		private DialogueLineNode ParseDialogueLine()
		{
			SourcePosition start = Current().Span.Start;

			// Expect a Speaker token
			DialogueToken speakerToken = Expect(DialogueTokenType.Speaker);
			if (speakerToken.Type == DialogueTokenType.EndOfFile)
				return null;

			string speaker = speakerToken.Value;
			SourceSpan speakerSpan = speakerToken.Span;
			SkipBlankLines();

			// Check for image command
			string imagePath = null;
			if (Check(DialogueTokenType.Command))
			{
				string cmdName = ExtractCommandName(Current().Value);
				if (cmdName == "Image")
				{
					imagePath = ExtractCommandArgument(Current().Value);
					Advance();
					SkipBlankLines();
				}
			}

			// Parse sentences until we hit a speaker, choice, conversation name, or end of file
			List<SentenceNode> sentences = new List<SentenceNode>();
			while (!IsAtEnd() && !Check(DialogueTokenType.Speaker) && !Check(DialogueTokenType.Choice))
			{
				// Stop if we hit a conversation-level command
				if (Check(DialogueTokenType.Command))
				{
					string cmdName = ExtractCommandName(Current().Value);
					if (cmdName == "ConversationName" || cmdName == "DialogueName")
						break;
					if (cmdName == "Image")
						break;
				}

				// Skip blank lines between sentences
				if (Check(DialogueTokenType.EndOfLine))
				{
					Advance();
					continue;
				}

				if (Check(DialogueTokenType.EndOfFile))
					break;

				// Skip standalone comments within dialogue lines
				if (Check(DialogueTokenType.Comment))
				{
					Advance();
					SkipBlankLines();
					continue;
				}

				SentenceNode sentence = ParseSentence();
				if (sentence != null)
					sentences.Add(sentence);
			}

			SourceSpan span = new SourceSpan(start, Previous().Span.End);
			return new DialogueLineNode(speaker, speakerSpan, sentences, imagePath, span);
		}

		/// <summary>
		/// Parses a single sentence line composed of text, functions, variables, and metadata.
		/// </summary>
		private SentenceNode ParseSentence()
		{
			SourcePosition start = Current().Span.Start;
			List<InlineNode> fragments = new List<InlineNode>();
			Dictionary<string, string> metadata = new Dictionary<string, string>();

			// Consume tokens until end of line
			while (!IsAtEnd() && !Check(DialogueTokenType.EndOfLine) && !Check(DialogueTokenType.EndOfFile))
			{
				DialogueToken token = Current();

				switch (token.Type)
				{
					case DialogueTokenType.Text:
						fragments.Add(new TextNode(token.Value, token.Span));
						Advance();
						break;

					case DialogueTokenType.Function:
						fragments.Add(new FunctionInvocationNode(token.Value, token.Span));
						Advance();
						break;

					case DialogueTokenType.Variable:
						fragments.Add(new VariableReferenceNode(token.Value, token.Span));
						Advance();
						break;

					case DialogueTokenType.Metadata:
						ParseMetadataValue(token.Value, metadata);
						Advance();
						break;

					case DialogueTokenType.Comment:
						// Skip inline comments
						Advance();
						break;

					default:
						_diagnostics.ReportWarning(token.Span,
							$"Unexpected token in sentence: {token.Type}");
						Advance();
						break;
				}
			}

			// Skip the end of line
			if (Check(DialogueTokenType.EndOfLine))
				Advance();

			if (fragments.Count == 0 && metadata.Count == 0)
				return null;

			SourceSpan span = new SourceSpan(start, Previous().Span.End);
			return new SentenceNode(fragments, metadata, span);
		}

		/// <summary>
		/// Parses a choice: - ChoiceText -> TargetConversation ## metadata
		/// </summary>
		private ChoiceNode ParseChoice(int lineIndex)
		{
			SourcePosition start = Current().Span.Start;
			DialogueToken choiceToken = Expect(DialogueTokenType.Choice);
			if (choiceToken.Type == DialogueTokenType.EndOfFile)
				return null;

			string value = choiceToken.Value;

			// Split on -> to get choice text and target
			// Also extract metadata (## ...) from the value
			Dictionary<string, string> metadata = new Dictionary<string, string>();
			string metadataPart = "";

			int metadataIndex = value.IndexOf("##");
			if (metadataIndex >= 0)
			{
				metadataPart = value.Substring(metadataIndex + 2).Trim();
				value = value.Substring(0, metadataIndex).Trim();
				ParseMetadataValue(metadataPart, metadata);
			}

			int arrowIndex = value.IndexOf("->");
			if (arrowIndex < 0)
			{
				_diagnostics.ReportError(choiceToken.Span,
					"Choice is missing '->' separator between choice text and target conversation");
				SkipBlankLines();
				SourceSpan errSpan = new SourceSpan(start, Previous().Span.End);
				return new ChoiceNode(value.Trim(), "", metadata, lineIndex, errSpan);
			}

			string choiceText = value.Substring(0, arrowIndex).Trim();
			string target = value.Substring(arrowIndex + 2).Trim();

			SkipBlankLines();
			SourceSpan span = new SourceSpan(start, Previous().Span.End);
			return new ChoiceNode(choiceText, target, metadata, lineIndex, span);
		}

		/// <summary>
		/// Parses metadata value string into key-value pairs.
		/// Format: "key:value key2:value2" or just "tag" (key=value=tag).
		/// </summary>
		private void ParseMetadataValue(string metadataText, Dictionary<string, string> metadata)
		{
			if (string.IsNullOrEmpty(metadataText))
				return;

			string[] parts = metadataText.Split(' ');
			foreach (string part in parts)
			{
				if (string.IsNullOrEmpty(part))
					continue;

				int colonIndex = part.IndexOf(':');
				if (colonIndex < 0)
				{
					if (!metadata.ContainsKey(part))
						metadata.Add(part, part);
				}
				else
				{
					string key = part.Substring(0, colonIndex);
					string val = part.Substring(colonIndex + 1);
					if (!metadata.ContainsKey(key))
						metadata.Add(key, val);
				}
			}
		}

		/// <summary>
		/// Extracts the command name from a command value string.
		/// e.g. "ConversationName(Init)" returns "ConversationName"
		/// e.g. "ConversationName" returns "ConversationName"
		/// </summary>
		private string ExtractCommandName(string commandValue)
		{
			int parenIndex = commandValue.IndexOf('(');
			return parenIndex >= 0 ? commandValue.Substring(0, parenIndex) : commandValue;
		}

		/// <summary>
		/// Extracts the argument from a command value string.
		/// e.g. "ConversationName(Init)" returns "Init"
		/// </summary>
		private string ExtractCommandArgument(string commandValue)
		{
			int openParen = commandValue.IndexOf('(');
			int closeParen = commandValue.LastIndexOf(')');
			if (openParen >= 0 && closeParen > openParen)
				return commandValue.Substring(openParen + 1, closeParen - openParen - 1);
			return "";
		}

		private DialogueToken Current()
		{
			if (_position >= _tokens.Count)
				return _tokens[_tokens.Count - 1]; // Return EOF token
			return _tokens[_position];
		}

		private DialogueToken Previous()
		{
			if (_position <= 0)
				return _tokens[0];
			return _tokens[_position - 1];
		}

		private bool IsAtEnd()
		{
			return Current().Type == DialogueTokenType.EndOfFile;
		}

		private bool Check(DialogueTokenType type)
		{
			return Current().Type == type;
		}

		private DialogueToken Advance()
		{
			DialogueToken token = Current();
			if (!IsAtEnd())
				_position++;
			return token;
		}

		private DialogueToken Expect(DialogueTokenType type)
		{
			if (Check(type))
				return Advance();

			_diagnostics.ReportError(Current().Span,
				$"Expected {type} but found {Current().Type}");
			return new DialogueToken(DialogueTokenType.EndOfFile, "", "", Current().Span);
		}

		private void SkipBlankLines()
		{
			while (Check(DialogueTokenType.EndOfLine))
				Advance();
		}

		private void SkipLine()
		{
			while (!IsAtEnd() && !Check(DialogueTokenType.EndOfLine))
				Advance();
			if (Check(DialogueTokenType.EndOfLine))
				Advance();
		}
	}
}
