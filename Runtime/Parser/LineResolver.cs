using System.Collections.Generic;

namespace Ibralogue.Parser
{
	/// <summary>
	/// Resolves runtime dialogue lines by substituting variable references with
	/// current values from the <see cref="VariableStore"/> and computing function
	/// invocation character indices.
	/// </summary>
	internal static class LineResolver
	{
		/// <summary>
		/// Rebuilds a line's text, speaker, and jump target from its unresolved
		/// sentence fragments using the current variable state.
		/// </summary>
		public static void Resolve(RuntimeLine runtimeLine, string assetName)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			List<FunctionInvocation> invocations = new List<FunctionInvocation>();
			int charOffset = 0;

			for (int i = 0; i < runtimeLine.Sentences.Count; i++)
			{
				if (i > 0)
				{
					sb.Append('\n');
					charOffset++;
				}

				foreach (InlineNode fragment in runtimeLine.Sentences[i].Fragments)
				{
					if (fragment is TextNode textNode)
					{
						sb.Append(textNode.Text);
						charOffset += textNode.Text.Length;
					}
					else if (fragment is VariableReferenceNode varNode)
					{
						string value = VariableStore.ToString(
							VariableStore.Resolve(assetName, varNode.VariableName));
						sb.Append(value);
						charOffset += value.Length;
					}
					else if (fragment is FunctionInvocationNode funcNode)
					{
						List<string> resolvedArgs = new List<string>(funcNode.Arguments.Count);
						foreach (string arg in funcNode.Arguments)
							resolvedArgs.Add(ResolveVariablesInString(arg, assetName));

						invocations.Add(new FunctionInvocation(
							funcNode.FunctionName,
							resolvedArgs,
							charOffset,
							funcNode.Span.Start.Line,
							funcNode.Span.Start.Column));
					}
				}
			}

			runtimeLine.Line.Speaker = runtimeLine.Line.Silent
				? ""
				: ResolveVariablesInString(runtimeLine.RawSpeaker, assetName);
			runtimeLine.Line.JumpTarget = ResolveVariablesInString(runtimeLine.RawJumpTarget, assetName);
			runtimeLine.Line.LineContent.Text = sb.ToString();
			runtimeLine.Line.LineContent.Invocations = invocations;

			Dictionary<string, string> resolvedMeta = new Dictionary<string, string>();
			foreach (KeyValuePair<string, string> kv in runtimeLine.Line.LineContent.Metadata)
				resolvedMeta[kv.Key] = ResolveVariablesInString(kv.Value, assetName);
			runtimeLine.Line.LineContent.Metadata = resolvedMeta;
		}

		/// <summary>
		/// Replaces $VARIABLE references in a plain string using the current variable state.
		/// </summary>
		public static string ResolveVariablesInString(string text, string assetName)
		{
			if (text == null || text.IndexOf('$') < 0)
				return text;

			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			int i = 0;

			while (i < text.Length)
			{
				if (text[i] == '$')
				{
					i++;
					int nameStart = i;
					while (i < text.Length && IsAlphanumeric(text[i]))
						i++;

					string varName = text.Substring(nameStart, i - nameStart);
					if (varName.Length > 0)
					{
						object value = VariableStore.Resolve(assetName, varName);
						sb.Append(value != null ? VariableStore.ToString(value) : "$" + varName);
					}
					else
					{
						sb.Append('$');
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

		/// <summary>
		/// Resolves a RuntimeChoicePoint into a list of Choice objects with
		/// variable references substituted.
		/// </summary>
		public static List<Choice> ResolveChoices(RuntimeChoicePoint choicePoint, string assetName)
		{
			List<Choice> resolved = new List<Choice>(choicePoint.Choices.Count);
			foreach (ChoiceData data in choicePoint.Choices)
			{
				Dictionary<string, string> resolvedMeta = new Dictionary<string, string>();
				foreach (KeyValuePair<string, string> kv in data.RawMetadata)
					resolvedMeta[kv.Key] = ResolveVariablesInString(kv.Value, assetName);

				resolved.Add(new Choice
				{
					ChoiceName = ResolveVariablesInString(data.RawText, assetName),
					LeadingConversationName = ResolveVariablesInString(data.RawTarget, assetName),
					Metadata = resolvedMeta
				});
			}
			return resolved;
		}

		/// <summary>
		/// Finds the first RuntimeChoicePoint in a content tree.
		/// </summary>
		public static RuntimeChoicePoint FindChoicePoint(List<RuntimeContentNode> content)
		{
			foreach (RuntimeContentNode node in content)
			{
				if (node is RuntimeChoicePoint cp)
					return cp;
			}
			return null;
		}

		/// <summary>
		/// Collects all RuntimeLine nodes from a content tree (including inside conditional branches).
		/// </summary>
		public static List<RuntimeLine> CollectLines(List<RuntimeContentNode> content)
		{
			List<RuntimeLine> lines = new List<RuntimeLine>();
			CollectLinesRecursive(content, lines);
			return lines;
		}

		private static void CollectLinesRecursive(List<RuntimeContentNode> content, List<RuntimeLine> lines)
		{
			foreach (RuntimeContentNode node in content)
			{
				if (node is RuntimeLine line)
					lines.Add(line);
				else if (node is RuntimeConditionalBlock cond)
				{
					foreach (RuntimeBranch branch in cond.Branches)
						CollectLinesRecursive(branch.Body, lines);
				}
			}
		}

		private static bool IsAlphanumeric(char c)
		{
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
		}
	}
}
