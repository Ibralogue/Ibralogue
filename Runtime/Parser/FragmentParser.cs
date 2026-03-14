using System.Collections.Generic;

namespace Ibralogue.Parser
{
	/// <summary>
	/// Parses a raw translated text string into inline fragments, handling
	/// $VARIABLE references and {{Function}} invocations the same way the
	/// lexer handles them in source sentences.
	/// </summary>
	internal static class FragmentParser
	{
		/// <summary>
		/// Converts a raw text string (potentially from a localization table) into
		/// a list of SentenceNode objects, split on literal newlines.
		/// Each sentence's fragments include TextNode, VariableReferenceNode,
		/// and FunctionInvocationNode as appropriate.
		/// </summary>
		public static List<SentenceNode> Parse(string text)
		{
			List<SentenceNode> sentences = new List<SentenceNode>();
			if (string.IsNullOrEmpty(text))
			{
				sentences.Add(MakeSentence(new List<InlineNode>()));
				return sentences;
			}

			string[] lines = text.Split('\n');
			foreach (string line in lines)
				sentences.Add(ParseSentence(line));

			return sentences;
		}

		private static SentenceNode ParseSentence(string text)
		{
			List<InlineNode> fragments = new List<InlineNode>();
			int i = 0;
			int textStart = 0;
			SourceSpan dummySpan = new SourceSpan(new SourcePosition(0, 0, 0), new SourcePosition(0, 0, 0));

			while (i < text.Length)
			{
				if (text[i] == '$' && i + 1 < text.Length && IsAlpha(text[i + 1]))
				{
					if (i > textStart)
						fragments.Add(new TextNode(text.Substring(textStart, i - textStart), dummySpan));

					i++;
					int nameStart = i;
					while (i < text.Length && IsAlphanumeric(text[i]))
						i++;

					string varName = text.Substring(nameStart, i - nameStart);
					fragments.Add(new VariableReferenceNode(varName, dummySpan));
					textStart = i;
				}
				else if (text[i] == '{' && i + 1 < text.Length && text[i + 1] == '{')
				{
					if (i > textStart)
						fragments.Add(new TextNode(text.Substring(textStart, i - textStart), dummySpan));

					i += 2;
					int funcStart = i;
					while (i < text.Length && text[i] != '(' && text[i] != '}')
						i++;

					string funcName = text.Substring(funcStart, i - funcStart).Trim();

					List<string> args = new List<string>();
					if (i < text.Length && text[i] == '(')
					{
						i++;
						int argStart = i;
						while (i < text.Length && text[i] != ')')
							i++;

						string rawArgs = text.Substring(argStart, i - argStart);
						if (rawArgs.Length > 0)
						{
							foreach (string arg in rawArgs.Split(','))
								args.Add(arg.Trim());
						}

						if (i < text.Length && text[i] == ')')
							i++;
					}

					if (i < text.Length && text[i] == '}')
						i++;
					if (i < text.Length && text[i] == '}')
						i++;

					fragments.Add(new FunctionInvocationNode(funcName, args, dummySpan));
					textStart = i;
				}
				else
				{
					i++;
				}
			}

			if (i > textStart)
				fragments.Add(new TextNode(text.Substring(textStart, i - textStart), dummySpan));

			return MakeSentence(fragments);
		}

		private static SentenceNode MakeSentence(List<InlineNode> fragments)
		{
			SourceSpan dummySpan = new SourceSpan(new SourcePosition(0, 0, 0), new SourcePosition(0, 0, 0));
			return new SentenceNode(fragments, new Dictionary<string, string>(), dummySpan);
		}

		private static bool IsAlpha(char c)
		{
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
		}

		private static bool IsAlphanumeric(char c)
		{
			return IsAlpha(c) || (c >= '0' && c <= '9');
		}
	}
}
