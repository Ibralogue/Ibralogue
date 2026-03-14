using System.Collections.Generic;
using Ibralogue.Localization;

namespace Ibralogue.Parser
{
	/// <summary>
	/// Resolves runtime dialogue lines by substituting variable references with
	/// current values from the <see cref="VariableStore"/> and computing function
	/// invocation character indices. When a localization provider is active,
	/// translated text is used in place of the original fragments.
	/// </summary>
	internal static class LineResolver
	{
		/// <summary>
		/// Rebuilds a line's text, speaker, and jump target from its unresolved
		/// sentence fragments (or localized text) using the current variable state.
		/// </summary>
		public static void Resolve(RuntimeLine runtimeLine, string assetName,
			ILocalizationProvider localization = null)
		{
			List<SentenceNode> sentences = runtimeLine.Sentences;

			if (localization != null && runtimeLine.LocalizationKey != null)
			{
				string translated = localization.Resolve(runtimeLine.LocalizationKey);
				if (translated != null)
					sentences = FragmentParser.Parse(translated);
			}

			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			List<Invocation> invocations = new List<Invocation>();
			int charOffset = 0;

			bool hasVisibleText = false;
			for (int i = 0; i < sentences.Count; i++)
			{
				if (hasVisibleText && SentenceHasVisibleText(sentences[i]))
				{
					sb.Append('\n');
					charOffset++;
				}

				int lengthBefore = sb.Length;
				foreach (InlineNode fragment in sentences[i].Fragments)
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
					else if (fragment is InvocationNode funcNode)
					{
						List<string> resolvedArgs = new List<string>(funcNode.Arguments.Count);
						foreach (string arg in funcNode.Arguments)
							resolvedArgs.Add(ResolveVariablesInString(arg, assetName));

						invocations.Add(new Invocation(
							funcNode.FunctionName,
							resolvedArgs,
							charOffset,
							funcNode.Span.Start.Line,
							funcNode.Span.Start.Column));
					}
				}

				if (sb.Length > lengthBefore)
					hasVisibleText = true;
			}

			string speaker;
			if (runtimeLine.Line.Silent)
			{
				speaker = "";
			}
			else if (localization != null && runtimeLine.SpeakerLocalizationKey != null)
			{
				string translatedSpeaker = localization.Resolve(runtimeLine.SpeakerLocalizationKey);
				speaker = translatedSpeaker != null
					? ResolveVariablesInString(translatedSpeaker, assetName)
					: ResolveVariablesInString(runtimeLine.RawSpeaker, assetName);
			}
			else
			{
				speaker = ResolveVariablesInString(runtimeLine.RawSpeaker, assetName);
			}

			runtimeLine.Line.Speaker = speaker;
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
		/// variable references and localized text substituted.
		/// </summary>
		public static List<Choice> ResolveChoices(RuntimeChoicePoint choicePoint, string assetName,
			ILocalizationProvider localization = null)
		{
			List<Choice> resolved = new List<Choice>(choicePoint.Choices.Count);
			foreach (ChoiceData data in choicePoint.Choices)
			{
				string choiceText = data.RawText;
				if (localization != null && data.LocalizationKey != null)
				{
					string translated = localization.Resolve(data.LocalizationKey);
					if (translated != null)
						choiceText = translated;
				}

				Dictionary<string, string> resolvedMeta = new Dictionary<string, string>();
				foreach (KeyValuePair<string, string> kv in data.RawMetadata)
					resolvedMeta[kv.Key] = ResolveVariablesInString(kv.Value, assetName);

				resolved.Add(new Choice
				{
					ChoiceName = ResolveVariablesInString(choiceText, assetName),
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

		private static bool SentenceHasVisibleText(SentenceNode sentence)
		{
			foreach (InlineNode fragment in sentence.Fragments)
			{
				if (fragment is TextNode || fragment is VariableReferenceNode)
					return true;
			}
			return false;
		}

		private static bool IsAlphanumeric(char c)
		{
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
		}
	}
}
