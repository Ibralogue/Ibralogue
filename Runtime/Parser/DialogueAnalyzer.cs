using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ibralogue.Parser
{
	/// <summary>
	/// Performs semantic analysis on the AST and converts it into runtime structures. 
	/// This includes converting function invocations, and builtins.
	/// </summary>
	internal class DialogueAnalyzer
	{
		private readonly DiagnosticBag _diagnostics;
		private readonly string _assetName;

		public DialogueAnalyzer(DiagnosticBag diagnostics, string assetName = null)
		{
			_diagnostics = diagnostics;
			_assetName = assetName ?? "unknown";
		}

		/// <summary>
		/// Converts a parsed AST document into the runtime Conversation list.
		/// </summary>
		public List<Conversation> Analyze(DialogueTree document)
		{
			List<Conversation> conversations = new List<Conversation>();

			foreach (ConversationNode conversationNode in document.Conversations)
			{
				Conversation conversation = AnalyzeConversation(conversationNode);
				conversations.Add(conversation);
			}

			if (conversations.Count == 0)
				throw new ArgumentException("Dialogue has no conversations");

			return conversations;
		}

		private Conversation AnalyzeConversation(ConversationNode node)
		{
			Conversation conversation = new Conversation
			{
				Name = ReplaceGlobalVariables(node.Name, node.Span),
				Lines = new List<Line>()
			};

			foreach (DialogueLineNode lineNode in node.Lines)
			{
				Line line = AnalyzeDialogueLine(lineNode);
				conversation.Lines.Add(line);
			}

			if (node.Choices.Count > 0)
			{
				conversation.Choices = new Dictionary<Choice, int>();
				foreach (ChoiceNode choiceNode in node.Choices)
				{
					Choice choice = AnalyzeChoice(choiceNode);
					conversation.Choices.Add(choice, choiceNode.LineIndex);
				}
			}

			return conversation;
		}

		private Line AnalyzeDialogueLine(DialogueLineNode node)
		{
			string speaker = ReplaceGlobalVariables(node.Speaker, node.SpeakerSpan);

			// Gather invocations/metadata from all sentences
			List<FunctionInvocation> invocations = new List<FunctionInvocation>();
			Dictionary<string, string> metadata = new Dictionary<string, string>();
			List<string> sentenceTexts = new List<string>();

			foreach (SentenceNode sentence in node.Sentences)
			{
				string text = BuildSentenceText(sentence, invocations);
				sentenceTexts.Add(text);

				foreach (KeyValuePair<string, string> kv in sentence.Metadata)
				{
					if (!metadata.ContainsKey(kv.Key))
						metadata.Add(kv.Key, kv.Value);
				}
			}

			string combinedText = string.Join("\n", sentenceTexts);

			Line line = new Line
			{
				Speaker = speaker,
				LineContent = new LineContent
				{
					Text = combinedText,
					Invocations = invocations,
					Metadata = metadata
				}
			};

			if (!string.IsNullOrEmpty(node.JumpTarget))
			{
				line.JumpTarget = ReplaceGlobalVariables(node.JumpTarget, node.Span);
			}

			if (!string.IsNullOrEmpty(node.ImagePath))
			{
				Sprite sprite = Resources.Load<Sprite>(node.ImagePath);
				if (sprite == null)
				{
					_diagnostics.ReportError(node.Span,
						$"Invalid image path '{node.ImagePath}' in {_assetName}");
				}
				line.SpeakerImage = sprite;
			}

			return line;
		}

		/// <summary>
		/// Builds the rendered text for a sentence, replacing variables and recording
		/// function invocation positions.
		/// </summary>
		private string BuildSentenceText(SentenceNode sentence, List<FunctionInvocation> invocations)
		{
			int charOffset = 0;
			System.Text.StringBuilder sb = new System.Text.StringBuilder();

			foreach (InlineNode fragment in sentence.Fragments)
			{
				if (fragment is TextNode textNode)
				{
					sb.Append(textNode.Text);
					charOffset += textNode.Text.Length;
				}
				else if (fragment is VariableReferenceNode varNode)
				{
					string value = ResolveVariable(varNode);
					sb.Append(value);
					charOffset += value.Length;
				}
				else if (fragment is FunctionInvocationNode funcNode)
				{
					funcNode.CharacterIndex = charOffset;
					invocations.Add(new FunctionInvocation(
						funcNode.FunctionName,
						charOffset,
						funcNode.Span.Start.Line,
						funcNode.Span.Start.Column));
				}
			}

			return sb.ToString();
		}

		private Choice AnalyzeChoice(ChoiceNode node)
		{
			return new Choice
			{
				ChoiceName = ReplaceGlobalVariables(node.Text, node.Span),
				LeadingConversationName = ReplaceGlobalVariables(node.TargetConversation, node.Span),
				Metadata = new Dictionary<string, string>(node.Metadata)
			};
		}

		/// <summary>
		/// Resolves a variable reference to its value from DialogueGlobals.
		/// </summary>
		private string ResolveVariable(VariableReferenceNode node)
		{
			if (DialogueGlobals.GlobalVariables.TryGetValue(node.VariableName, out string value))
				return value;

			_diagnostics.ReportWarning(node.Span,
				$"Variable declaration detected, ({node.VariableName}) but no entry found in dictionary!");
			return "$" + node.VariableName;
		}

		/// <summary>
		/// Replaces $VARIABLE references in a plain string using DialogueGlobals.
		/// Used for speaker names and other non-fragment strings.
		/// </summary>
		private string ReplaceGlobalVariables(string text, SourceSpan span)
		{
			if (text == null || text.IndexOf('$') < 0)
				return text;

			int i = 0;
			System.Text.StringBuilder sb = new System.Text.StringBuilder();

			while (i < text.Length)
			{
				if (text[i] == '$')
				{
					i++;
					int nameStart = i;
					while (i < text.Length && IsAlphanumeric(text[i]))
						i++;

					string varName = text.Substring(nameStart, i - nameStart);
					if (varName.Length > 0 && DialogueGlobals.GlobalVariables.TryGetValue(varName, out string value))
					{
						sb.Append(value);
					}
					else
					{
						if (varName.Length > 0)
						{
							_diagnostics.ReportWarning(span,
								$"Variable declaration detected, ({varName}) but no entry found in dictionary!");
						}
						sb.Append('$');
						sb.Append(varName);
					}
				}
				else
				{
					sb.Append(text[i]);
					i++;
				}
			}

			return sb.ToString();
		}

		private static bool IsAlphanumeric(char c)
		{
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
		}
	}
}
