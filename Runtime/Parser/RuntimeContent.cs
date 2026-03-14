using System.Collections.Generic;
using Ibralogue.Parser.Expressions;

namespace Ibralogue.Parser
{
	/// <summary>
	/// Base type for runtime content nodes within a conversation.
	/// The engine walks these nodes to drive dialogue playback.
	/// </summary>
	internal abstract class RuntimeContentNode
	{
	}

	/// <summary>
	/// A displayable dialogue line with unresolved fragments preserved
	/// for runtime variable substitution.
	/// </summary>
	internal class RuntimeLine : RuntimeContentNode
	{
		public readonly Line Line;
		public readonly List<SentenceNode> Sentences;
		public readonly string RawSpeaker;
		public readonly string RawJumpTarget;

		public RuntimeLine(Line line, List<SentenceNode> sentences, string rawSpeaker, string rawJumpTarget)
		{
			Line = line;
			Sentences = sentences;
			RawSpeaker = rawSpeaker;
			RawJumpTarget = rawJumpTarget;
		}
	}

	/// <summary>
	/// A point in the content tree where the player is presented with choices.
	/// </summary>
	internal class RuntimeChoicePoint : RuntimeContentNode
	{
		public readonly List<ChoiceData> Choices;

		public RuntimeChoicePoint(List<ChoiceData> choices)
		{
			Choices = choices;
		}
	}

	/// <summary>
	/// A single choice option with its raw (unresolved) text and target.
	/// </summary>
	internal class ChoiceData
	{
		public readonly string RawText;
		public readonly string RawTarget;
		public readonly Dictionary<string, string> RawMetadata;

		public ChoiceData(string rawText, string rawTarget, Dictionary<string, string> rawMetadata)
		{
			RawText = rawText;
			RawTarget = rawTarget;
			RawMetadata = rawMetadata;
		}
	}

	/// <summary>
	/// A conditional block with branches evaluated at runtime.
	/// </summary>
	internal class RuntimeConditionalBlock : RuntimeContentNode
	{
		public readonly List<RuntimeBranch> Branches;

		public RuntimeConditionalBlock(List<RuntimeBranch> branches)
		{
			Branches = branches;
		}
	}

	/// <summary>
	/// A single branch within a conditional block.
	/// </summary>
	internal class RuntimeBranch
	{
		/// <summary>
		/// The condition to evaluate. Null for the Else branch.
		/// </summary>
		public readonly ExpressionNode Condition;

		public readonly List<RuntimeContentNode> Body;

		public RuntimeBranch(ExpressionNode condition, List<RuntimeContentNode> body)
		{
			Condition = condition;
			Body = body;
		}
	}

	/// <summary>
	/// A variable assignment executed when the engine reaches this node.
	/// </summary>
	internal class RuntimeSetCommand : RuntimeContentNode
	{
		public readonly string VariableName;
		public readonly ExpressionNode Value;

		public RuntimeSetCommand(string variableName, ExpressionNode value)
		{
			VariableName = variableName;
			Value = value;
		}
	}

	/// <summary>
	/// A global variable declaration executed when the engine reaches this node.
	/// </summary>
	internal class RuntimeGlobalDecl : RuntimeContentNode
	{
		public readonly string VariableName;
		public readonly ExpressionNode DefaultValue;

		public RuntimeGlobalDecl(string variableName, ExpressionNode defaultValue)
		{
			VariableName = variableName;
			DefaultValue = defaultValue;
		}
	}
}
