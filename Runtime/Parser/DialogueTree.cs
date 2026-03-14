using System.Collections.Generic;
using Ibralogue.Parser.Expressions;

namespace Ibralogue.Parser
{
	/// <summary>
	/// Root node of a parsed dialogue file, containing one or more conversations.
	/// </summary>
	internal class DialogueTree
	{
		public readonly List<ConversationNode> Conversations;
		public readonly SourceSpan Span;

		public DialogueTree(List<ConversationNode> conversations, SourceSpan span)
		{
			Conversations = conversations;
			Span = span;
		}
	}

	/// <summary>
	/// A named block of dialogue content including lines, choices, conditionals, and commands.
	/// </summary>
	internal class ConversationNode
	{
		public readonly string Name;
		public readonly List<ContentNode> Content;
		public readonly SourceSpan Span;

		public ConversationNode(string name, List<ContentNode> content, SourceSpan span)
		{
			Name = name;
			Content = content;
			Span = span;
		}
	}

	/// <summary>
	/// Base type for top-level content within a conversation: dialogue lines,
	/// conditional blocks, and variable commands.
	/// </summary>
	internal abstract class ContentNode
	{
		public readonly SourceSpan Span;

		protected ContentNode(SourceSpan span)
		{
			Span = span;
		}
	}

	/// <summary>
	/// A single dialogue line: a speaker followed by one or more sentences of content.
	/// </summary>
	internal class DialogueLineNode : ContentNode
	{
		public readonly string Speaker;
		public readonly SourceSpan SpeakerSpan;
		public readonly List<SentenceNode> Sentences;
		public readonly string JumpTarget;

		public DialogueLineNode(string speaker, SourceSpan speakerSpan, List<SentenceNode> sentences,
			string jumpTarget, SourceSpan span) : base(span)
		{
			Speaker = speaker;
			SpeakerSpan = speakerSpan;
			Sentences = sentences;
			JumpTarget = jumpTarget;
		}
	}

	/// <summary>
	/// A single sentence within a dialogue line, composed of text segments,
	/// inline functions, variable references, and metadata annotations.
	/// </summary>
	internal class SentenceNode
	{
		public readonly List<InlineNode> Fragments;
		public readonly Dictionary<string, string> Metadata;
		public readonly SourceSpan Span;

		public SentenceNode(List<InlineNode> fragments, Dictionary<string, string> metadata,
			SourceSpan span)
		{
			Fragments = fragments;
			Metadata = metadata;
			Span = span;
		}
	}

	/// <summary>
	/// Base type for inline content within a sentence.
	/// </summary>
	internal abstract class InlineNode
	{
		public readonly SourceSpan Span;

		protected InlineNode(SourceSpan span)
		{
			Span = span;
		}
	}

	/// <summary>
	/// A plain text fragment within a sentence.
	/// </summary>
	internal class TextNode : InlineNode
	{
		public readonly string Text;

		public TextNode(string text, SourceSpan span) : base(span)
		{
			Text = text;
		}
	}

	/// <summary>
	/// An inline invocation within a sentence: {{Name}} or {{Name(arg1, arg2)}}
	/// </summary>
	internal class InvocationNode : InlineNode
	{
		public readonly string FunctionName;
		public readonly List<string> Arguments;

		public int CharacterIndex;

		public InvocationNode(string functionName, List<string> arguments, SourceSpan span) : base(span)
		{
			FunctionName = functionName;
			Arguments = arguments;
		}
	}

	/// <summary>
	/// A global variable reference within a sentence: $VARIABLE.
	/// </summary>
	internal class VariableReferenceNode : InlineNode
	{
		public readonly string VariableName;

		public VariableReferenceNode(string variableName, SourceSpan span) : base(span)
		{
			VariableName = variableName;
		}
	}

	/// <summary>
	/// A choice option that directs to another conversation or continues with >>.
	/// </summary>
	internal class ChoiceNode
	{
		public readonly string Text;
		public readonly string TargetConversation;
		public readonly Dictionary<string, string> Metadata;
		public readonly SourceSpan Span;

		public ChoiceNode(string text, string targetConversation, Dictionary<string, string> metadata,
			SourceSpan span)
		{
			Text = text;
			TargetConversation = targetConversation;
			Metadata = metadata;
			Span = span;
		}
	}

	/// <summary>
	/// A group of choices presented to the player at a specific point in the content.
	/// </summary>
	internal class ChoiceGroupNode : ContentNode
	{
		public readonly List<ChoiceNode> Choices;

		public ChoiceGroupNode(List<ChoiceNode> choices, SourceSpan span) : base(span)
		{
			Choices = choices;
		}
	}

	/// <summary>
	/// A single branch within a conditional block (If, ElseIf, or Else).
	/// </summary>
	internal class ConditionalBranch
	{
		/// <summary>
		/// The condition expression to evaluate. Null for the Else branch.
		/// </summary>
		public readonly ExpressionNode Condition;

		public readonly List<ContentNode> Body;
		public readonly SourceSpan Span;

		public ConditionalBranch(ExpressionNode condition, List<ContentNode> body, SourceSpan span)
		{
			Condition = condition;
			Body = body;
			Span = span;
		}
	}

	/// <summary>
	/// A conditional block: {{If}} / {{ElseIf}} / {{Else}} / {{EndIf}}.
	/// Contains one or more branches evaluated in order at runtime.
	/// </summary>
	internal class ConditionalBlockNode : ContentNode
	{
		public readonly List<ConditionalBranch> Branches;

		public ConditionalBlockNode(List<ConditionalBranch> branches, SourceSpan span) : base(span)
		{
			Branches = branches;
		}
	}

	/// <summary>
	/// A variable assignment command: {{Set($Var, expression)}}.
	/// </summary>
	internal class SetCommandNode : ContentNode
	{
		public readonly string VariableName;
		public readonly ExpressionNode Value;

		public SetCommandNode(string variableName, ExpressionNode value, SourceSpan span) : base(span)
		{
			VariableName = variableName;
			Value = value;
		}
	}

	/// <summary>
	/// A global variable declaration: {{Global($Var)}} or {{Global($Var, expression)}}.
	/// </summary>
	internal class GlobalDeclNode : ContentNode
	{
		public readonly string VariableName;
		public readonly ExpressionNode DefaultValue;

		public GlobalDeclNode(string variableName, ExpressionNode defaultValue, SourceSpan span) : base(span)
		{
			VariableName = variableName;
			DefaultValue = defaultValue;
		}
	}
}
