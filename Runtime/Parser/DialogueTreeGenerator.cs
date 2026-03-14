using System.Collections.Generic;
using Ibralogue.Parser.Expressions;

namespace Ibralogue.Parser
{
	/// <summary>
	/// Recursive descent parser that converts a flat token stream into an AST.
	/// The parser is purely structural — it does not perform variable substitution,
	/// resource loading, or any other side effects.
	/// </summary>
	internal class DialogueTreeGenerator
	{
		private readonly List<DialogueToken> _tokens;
		private readonly DiagnosticBag _diagnostics;
		private int _position;

		public DialogueTreeGenerator(List<DialogueToken> tokens, DiagnosticBag diagnostics)
		{
			_tokens = tokens;
			_diagnostics = diagnostics;
			_position = 0;
		}

		/// <summary>
		/// Parses the token stream into a DialogueTree.
		/// </summary>
		public DialogueTree ParseIntoTree()
		{
			SourcePosition start = Current().Span.Start;
			List<ConversationNode> conversations = new List<ConversationNode>();

			SkipBlankLines();

			while (!IsAtEnd())
			{
				ConversationNode conversation = ParseConversation(conversations.Count == 0);
				if (conversation != null)
					conversations.Add(conversation);
			}

			SourceSpan span = new SourceSpan(start, Previous().Span.End);
			return new DialogueTree(conversations, span);
		}

		/// <summary>
		/// Parses a single conversation block.
		/// A conversation starts with an optional {{ConversationName(name)}} command
		/// and contains dialogue lines, conditional blocks, variable commands, and optional choices.
		/// </summary>
		private ConversationNode ParseConversation(bool isFirst)
		{
			SourcePosition start = Current().Span.Start;
			string name = "Default";

			if (Check(DialogueTokenType.Command))
			{
				DialogueToken command = Current();
				string commandName = ExtractCommandName(command.Value);

				if (commandName == "ConversationName")
				{
					name = ExtractCommandArgument(command.Value);
					Advance();
					SkipBlankLines();
				}
			}

			List<ContentNode> content = new List<ContentNode>();
			ParseContentBlock(content, false);

			SourceSpan span = new SourceSpan(start, Previous().Span.End);
			return new ConversationNode(name, content, span);
		}

		/// <summary>
		/// Parses a sequence of content nodes (dialogue lines, choices, conditionals,
		/// Set, Global). Stops at EOF, a new ConversationName command, or (when inside
		/// a conditional branch) at ElseIf, Else, or EndIf.
		/// </summary>
		private void ParseContentBlock(List<ContentNode> content, bool insideConditional)
		{
			while (!IsAtEnd())
			{
				SkipBlankLines();
				if (IsAtEnd())
					break;

				if (Check(DialogueTokenType.Comment))
				{
					Advance();
					continue;
				}

				if (Check(DialogueTokenType.Command))
				{
					string cmdName = ExtractCommandName(Current().Value);
					if (cmdName == "ConversationName")
						break;
				}

				if (insideConditional && (Check(DialogueTokenType.ElseIf) || Check(DialogueTokenType.Else) || Check(DialogueTokenType.EndIf)))
					break;

				if (Check(DialogueTokenType.If))
				{
					ConditionalBlockNode conditional = ParseConditionalBlock();
					if (conditional != null)
						content.Add(conditional);
					continue;
				}

				if (Check(DialogueTokenType.Set))
				{
					SetCommandNode setNode = ParseSetCommand();
					if (setNode != null)
						content.Add(setNode);
					continue;
				}

				if (Check(DialogueTokenType.Global))
				{
					GlobalDeclNode globalNode = ParseGlobalDecl();
					if (globalNode != null)
						content.Add(globalNode);
					continue;
				}

				if (Check(DialogueTokenType.Choice))
				{
					ChoiceGroupNode group = ParseChoiceGroup();
					if (group != null)
						content.Add(group);
					continue;
				}

				if (Check(DialogueTokenType.Speaker))
				{
					DialogueLineNode dialogueLine = ParseDialogueLine();
					if (dialogueLine != null)
						content.Add(dialogueLine);
					continue;
				}

				if (Check(DialogueTokenType.Command))
				{
					_diagnostics.ReportWarning(Current().Span,
						$"Unexpected command outside of a dialogue line: {Current().Value}");
					Advance();
					SkipBlankLines();
					continue;
				}

				if (!Check(DialogueTokenType.EndOfFile))
				{
					_diagnostics.ReportWarning(Current().Span,
						$"Unexpected content outside of a dialogue line: '{Current().Lexeme}'");
					SkipLine();
					continue;
				}

				break;
			}
		}

		/// <summary>
		/// Parses a group of consecutive choice lines into a single ChoiceGroupNode.
		/// </summary>
		private ChoiceGroupNode ParseChoiceGroup()
		{
			SourcePosition start = Current().Span.Start;
			List<ChoiceNode> choices = new List<ChoiceNode>();

			while (Check(DialogueTokenType.Choice))
			{
				ChoiceNode choice = ParseChoice();
				if (choice != null)
					choices.Add(choice);
				SkipBlankLines();
			}

			if (choices.Count == 0)
				return null;

			SourceSpan span = new SourceSpan(start, Previous().Span.End);
			return new ChoiceGroupNode(choices, span);
		}

		/// <summary>
		/// Parses a conditional block starting from an {{If}} token through to {{EndIf}},
		/// including any {{ElseIf}} and {{Else}} branches. Supports nesting.
		/// </summary>
		private ConditionalBlockNode ParseConditionalBlock()
		{
			SourcePosition start = Current().Span.Start;
			List<ConditionalBranch> branches = new List<ConditionalBranch>();

			ExpressionNode ifCondition = ParseExpression(Current().Value);
			SourcePosition branchStart = Current().Span.Start;
			Advance();
			SkipBlankLines();

			List<ContentNode> ifBody = new List<ContentNode>();
			ParseContentBlock(ifBody, true);

			SourceSpan ifSpan = new SourceSpan(branchStart, Previous().Span.End);
			branches.Add(new ConditionalBranch(ifCondition, ifBody, ifSpan));

			while (Check(DialogueTokenType.ElseIf))
			{
				ExpressionNode elseIfCondition = ParseExpression(Current().Value);
				SourcePosition elseIfStart = Current().Span.Start;
				Advance();
				SkipBlankLines();

				List<ContentNode> elseIfBody = new List<ContentNode>();
				ParseContentBlock(elseIfBody, true);

				SourceSpan elseIfSpan = new SourceSpan(elseIfStart, Previous().Span.End);
				branches.Add(new ConditionalBranch(elseIfCondition, elseIfBody, elseIfSpan));
			}

			if (Check(DialogueTokenType.Else))
			{
				SourcePosition elseStart = Current().Span.Start;
				Advance();
				SkipBlankLines();

				List<ContentNode> elseBody = new List<ContentNode>();
				ParseContentBlock(elseBody, true);

				SourceSpan elseSpan = new SourceSpan(elseStart, Previous().Span.End);
				branches.Add(new ConditionalBranch(null, elseBody, elseSpan));
			}

			if (Check(DialogueTokenType.EndIf))
			{
				Advance();
				SkipBlankLines();
			}
			else
			{
				_diagnostics.ReportError(Current().Span, "Expected {{EndIf}} to close conditional block");
			}

			SourceSpan span = new SourceSpan(start, Previous().Span.End);
			return new ConditionalBlockNode(branches, span);
		}

		/// <summary>
		/// Parses a {{Set($Var, expression)}} command.
		/// </summary>
		private SetCommandNode ParseSetCommand()
		{
			SourcePosition start = Current().Span.Start;
			string rawArgs = Current().Value;
			Advance();
			SkipBlankLines();

			int commaIndex = FindTopLevelComma(rawArgs);
			if (commaIndex < 0)
			{
				_diagnostics.ReportError(new SourceSpan(start, Previous().Span.End),
					"{{Set}} requires two arguments: {{Set($Variable, expression)}}");
				return null;
			}

			string varPart = rawArgs.Substring(0, commaIndex).Trim();
			string exprPart = rawArgs.Substring(commaIndex + 1).Trim();

			if (varPart.Length > 0 && varPart[0] == '$')
				varPart = varPart.Substring(1);

			ExpressionNode value = ParseExpression(exprPart);
			SourceSpan span = new SourceSpan(start, Previous().Span.End);
			return new SetCommandNode(varPart, value, span);
		}

		/// <summary>
		/// Parses a {{Global($Var)}} or {{Global($Var, expression)}} declaration.
		/// </summary>
		private GlobalDeclNode ParseGlobalDecl()
		{
			SourcePosition start = Current().Span.Start;
			string rawArgs = Current().Value;
			Advance();
			SkipBlankLines();

			int commaIndex = FindTopLevelComma(rawArgs);
			string varPart;
			ExpressionNode defaultValue = null;

			if (commaIndex >= 0)
			{
				varPart = rawArgs.Substring(0, commaIndex).Trim();
				string exprPart = rawArgs.Substring(commaIndex + 1).Trim();
				defaultValue = ParseExpression(exprPart);
			}
			else
			{
				varPart = rawArgs.Trim();
			}

			if (varPart.Length > 0 && varPart[0] == '$')
				varPart = varPart.Substring(1);

			SourceSpan span = new SourceSpan(start, Previous().Span.End);
			return new GlobalDeclNode(varPart, defaultValue, span);
		}

		/// <summary>
		/// Finds the first comma that is not nested inside parentheses.
		/// </summary>
		private static int FindTopLevelComma(string text)
		{
			int depth = 0;
			for (int i = 0; i < text.Length; i++)
			{
				if (text[i] == '(') depth++;
				else if (text[i] == ')') depth--;
				else if (text[i] == ',' && depth == 0) return i;
			}
			return -1;
		}

		private ExpressionNode ParseExpression(string expressionText)
		{
			ExpressionLexer lexer = new ExpressionLexer(expressionText);
			List<ExpressionToken> tokens = lexer.Tokenize();
			ExpressionParser parser = new ExpressionParser(tokens);
			return parser.Parse();
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

			// Check for jump command before sentences
			string jumpTarget = null;
			if (Check(DialogueTokenType.Command))
			{
				string cmdName = ExtractCommandName(Current().Value);
				if (cmdName == "Jump")
				{
					jumpTarget = ExtractCommandArgument(Current().Value);
					Advance();
					SkipBlankLines();
				}
			}

			List<SentenceNode> sentences = new List<SentenceNode>();
			while (!IsAtEnd() && !Check(DialogueTokenType.Speaker) && !Check(DialogueTokenType.Choice)
				&& !IsStructuralToken(Current().Type))
			{
				if (Check(DialogueTokenType.Command))
				{
					string cmdName = ExtractCommandName(Current().Value);
					if (cmdName == "ConversationName")
						break;
					if (cmdName == "Image")
						break;

					// Handle Jump command after sentences
					if (cmdName == "Jump")
					{
						if (jumpTarget != null)
						{
							_diagnostics.ReportWarning(Current().Span,
								"Duplicate Jump command in dialogue line; using last value");
						}
						jumpTarget = ExtractCommandArgument(Current().Value);
						Advance();
						SkipBlankLines();
						break;
					}
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
			return new DialogueLineNode(speaker, speakerSpan, sentences, imagePath, jumpTarget, span);
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
						string funcName = InvocationSyntax.ExtractName(token.Value);
						List<string> funcArgs = InvocationSyntax.SplitArguments(
							InvocationSyntax.ExtractRawArgument(token.Value));
						fragments.Add(new FunctionInvocationNode(funcName, funcArgs, token.Span));
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
		private ChoiceNode ParseChoice()
		{
			SourcePosition start = Current().Span.Start;
			DialogueToken choiceToken = Expect(DialogueTokenType.Choice);
			if (choiceToken.Type == DialogueTokenType.EndOfFile)
				return null;

			string value = choiceToken.Value;

			Dictionary<string, string> metadata = new Dictionary<string, string>();

			int metadataIndex = value.IndexOf("##");
			if (metadataIndex >= 0)
			{
				string metadataPart = value.Substring(metadataIndex + 2).Trim();
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
				return new ChoiceNode(value.Trim(), "", metadata, errSpan);
			}

			string choiceText = value.Substring(0, arrowIndex).Trim();
			string target = value.Substring(arrowIndex + 2).Trim();

			SkipBlankLines();
			SourceSpan span = new SourceSpan(start, Previous().Span.End);
			return new ChoiceNode(choiceText, target, metadata, span);
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

		private static string ExtractCommandName(string commandValue)
		{
			return InvocationSyntax.ExtractName(commandValue);
		}

		private static string ExtractCommandArgument(string commandValue)
		{
			return InvocationSyntax.ExtractRawArgument(commandValue);
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

		private static bool IsStructuralToken(DialogueTokenType type)
		{
			return type == DialogueTokenType.If || type == DialogueTokenType.ElseIf
				|| type == DialogueTokenType.Else || type == DialogueTokenType.EndIf
				|| type == DialogueTokenType.Set || type == DialogueTokenType.Global;
		}
	}
}
