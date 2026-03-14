using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ibralogue.Parser
{
	/// <summary>
	/// Converts the AST into runtime content structures.
	/// Variables are NOT resolved here -- they are preserved as references
	/// for the engine to resolve at display time. Localization keys are
	/// assigned to every displayable content node.
	/// </summary>
	internal class DialogueAnalyzer
	{
		private readonly DiagnosticBag _diagnostics;
		private readonly string _assetName;

		private string _conversationName;
		private int _lineIndex;
		private int _choiceIndex;

		public DialogueAnalyzer(DiagnosticBag diagnostics, string assetName = null)
		{
			_diagnostics = diagnostics;
			_assetName = assetName ?? "unknown";
		}

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
			_conversationName = node.Name;
			_lineIndex = 0;
			_choiceIndex = 0;

			return new Conversation
			{
				Name = node.Name,
				Content = AnalyzeContentList(node.Content)
			};
		}

		private List<RuntimeContentNode> AnalyzeContentList(List<ContentNode> nodes)
		{
			List<RuntimeContentNode> result = new List<RuntimeContentNode>();

			foreach (ContentNode node in nodes)
			{
				if (node is DialogueLineNode lineNode)
				{
					result.Add(AnalyzeDialogueLine(lineNode));
				}
				else if (node is ChoiceGroupNode choiceGroup)
				{
					List<ChoiceData> choices = new List<ChoiceData>();
					foreach (ChoiceNode choiceNode in choiceGroup.Choices)
						choices.Add(AnalyzeChoice(choiceNode));
					result.Add(new RuntimeChoicePoint(choices));
				}
				else if (node is ConditionalBlockNode condNode)
				{
					result.Add(AnalyzeConditionalBlock(condNode));
				}
				else if (node is SetCommandNode setNode)
				{
					result.Add(new RuntimeSetCommand(setNode.VariableName, setNode.Value));
				}
				else if (node is GlobalDeclNode globalNode)
				{
					result.Add(new RuntimeGlobalDecl(globalNode.VariableName, globalNode.DefaultValue));
				}
			}

			return result;
		}

		private RuntimeLine AnalyzeDialogueLine(DialogueLineNode node)
		{
			Sprite speakerImage = null;
			if (!string.IsNullOrEmpty(node.ImagePath))
			{
				speakerImage = Resources.Load<Sprite>(node.ImagePath);
				if (speakerImage == null)
				{
					_diagnostics.ReportError(node.Span,
						$"Invalid image path '{node.ImagePath}' in {_assetName}");
				}
			}

			List<FunctionInvocation> invocations = new List<FunctionInvocation>();
			foreach (SentenceNode sentence in node.Sentences)
				CollectInvocations(sentence, invocations);

			Dictionary<string, string> metadata = CollectMetadata(node.Sentences);

			string locKey = metadata.TryGetValue("loc", out string customKey)
				? customKey
				: $"{_conversationName}.line.{_lineIndex}";

			string speakerKey = node.Speaker != ">>"
				? $"speaker.{node.Speaker}"
				: null;

			bool silent = node.Speaker == ">>";
			_lineIndex++;

			Line line = new Line
			{
				Speaker = silent ? "" : node.Speaker,
				LineContent = new LineContent
				{
					Text = "",
					Invocations = invocations,
					Metadata = metadata
				},
				SpeakerImage = speakerImage,
				JumpTarget = node.JumpTarget,
				Silent = silent
			};

			return new RuntimeLine(line, node.Sentences, node.Speaker, node.JumpTarget,
				locKey, speakerKey);
		}

		private ChoiceData AnalyzeChoice(ChoiceNode node)
		{
			string locKey = node.Metadata.TryGetValue("loc", out string customKey)
				? customKey
				: $"{_conversationName}.choice.{_choiceIndex}";

			_choiceIndex++;

			return new ChoiceData(node.Text, node.TargetConversation, node.Metadata, locKey);
		}

		private void CollectInvocations(SentenceNode sentence, List<FunctionInvocation> invocations)
		{
			foreach (InlineNode fragment in sentence.Fragments)
			{
				if (fragment is FunctionInvocationNode funcNode)
				{
					invocations.Add(new FunctionInvocation(
						funcNode.FunctionName,
						new List<string>(funcNode.Arguments),
						0,
						funcNode.Span.Start.Line,
						funcNode.Span.Start.Column));
				}
			}
		}

		private Dictionary<string, string> CollectMetadata(List<SentenceNode> sentences)
		{
			Dictionary<string, string> metadata = new Dictionary<string, string>();
			foreach (SentenceNode sentence in sentences)
			{
				foreach (KeyValuePair<string, string> kv in sentence.Metadata)
				{
					if (!metadata.ContainsKey(kv.Key))
						metadata.Add(kv.Key, kv.Value);
				}
			}
			return metadata;
		}

		private RuntimeConditionalBlock AnalyzeConditionalBlock(ConditionalBlockNode node)
		{
			List<RuntimeBranch> branches = new List<RuntimeBranch>();

			foreach (ConditionalBranch branch in node.Branches)
			{
				List<RuntimeContentNode> body = AnalyzeContentList(branch.Body);
				branches.Add(new RuntimeBranch(branch.Condition, body));
			}

			return new RuntimeConditionalBlock(branches);
		}
	}
}
