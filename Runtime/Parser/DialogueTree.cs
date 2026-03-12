using System.Collections.Generic;

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
	/// A named block of dialogue lines and optional choices.
	/// </summary>
	internal class ConversationNode
	{
		public readonly string Name;
		public readonly List<DialogueLineNode> Lines;
		public readonly List<ChoiceNode> Choices;
		public readonly SourceSpan Span;

		public ConversationNode(string name, List<DialogueLineNode> lines, List<ChoiceNode> choices,
			SourceSpan span)
		{
			Name = name;
			Lines = lines;
			Choices = choices;
			Span = span;
		}
	}

	/// <summary>
	/// A single dialogue line: a speaker followed by one or more sentences of content.
	/// </summary>
	internal class DialogueLineNode
	{
		public readonly string Speaker;
		public readonly SourceSpan SpeakerSpan;
		public readonly List<SentenceNode> Sentences;
		public readonly string ImagePath;
		public readonly string JumpTarget;
		public readonly SourceSpan Span;

		public DialogueLineNode(string speaker, SourceSpan speakerSpan, List<SentenceNode> sentences,
			string imagePath, string jumpTarget, SourceSpan span)
		{
			Speaker = speaker;
			SpeakerSpan = speakerSpan;
			Sentences = sentences;
			ImagePath = imagePath;
			JumpTarget = jumpTarget;
			Span = span;
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
	/// An inline function invocation within a sentence: {{FunctionName}}
	/// </summary>
	internal class FunctionInvocationNode : InlineNode
	{
		public readonly string FunctionName;

		/// <summary>
		/// The character position in the final rendered text where this function
		/// should insert its result (for string-returning functions).
		/// Computed during analysis.
		/// </summary>
		public int CharacterIndex;

		public FunctionInvocationNode(string functionName, SourceSpan span) : base(span)
		{
			FunctionName = functionName;
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
	/// A choice option that directs to another conversation.
	/// </summary>
	internal class ChoiceNode
	{
		public readonly string Text;
		public readonly string TargetConversation;
		public readonly Dictionary<string, string> Metadata;
		public readonly int LineIndex;
		public readonly SourceSpan Span;

		public ChoiceNode(string text, string targetConversation, Dictionary<string, string> metadata,
			int lineIndex, SourceSpan span)
		{
			Text = text;
			TargetConversation = targetConversation;
			Metadata = metadata;
			LineIndex = lineIndex;
			Span = span;
		}
	}
}
