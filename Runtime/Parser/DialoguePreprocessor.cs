using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Ibralogue.Parser
{
	/// <summary>
	/// Preprocesses raw dialogue source text before lexing.
	/// Expands <c>{{Include(AssetName)}}</c> and <c>{{Include(AssetName, ConversationName)}}</c>
	/// directives by substituting the referenced content inline.
	/// </summary>
	internal class DialoguePreprocessor
	{
		private readonly DiagnosticBag _diagnostics;
		private readonly HashSet<string> _includeStack;

		// Matches a standalone {{Include(...)}} line, capturing the arguments inside the parens.
		// Group 1 = everything between the outermost parentheses.
		// Allows quoted arguments: {{Include("file")}}, {{Include("file", "conv")}}
		private static readonly Regex IncludePattern = new Regex(
			@"^[ \t]*\{\{Include\((.+?)\)\}\}[ \t]*\r?$",
			RegexOptions.Multiline | RegexOptions.Compiled);

		/// <summary>
		/// Creates a new preprocessor.
		/// </summary>
		/// <param name="diagnostics">Diagnostic bag for reporting errors and warnings.</param>
		/// <param name="currentAssetName">
		/// Name of the asset being processed, used for circular include detection.
		/// </param>
		public DialoguePreprocessor(DiagnosticBag diagnostics, string currentAssetName)
		{
			_diagnostics = diagnostics;
			_includeStack = new HashSet<string> { currentAssetName };
		}

		/// <summary>
		/// Internal constructor used during recursive expansion to carry the include stack.
		/// </summary>
		private DialoguePreprocessor(DiagnosticBag diagnostics, HashSet<string> includeStack)
		{
			_diagnostics = diagnostics;
			_includeStack = includeStack;
		}

		/// <summary>
		/// Expands all <c>{{Include(...)}}</c> directives in the source text.
		/// </summary>
		public string Process(string source)
		{
			if (string.IsNullOrEmpty(source))
				return source;

			return IncludePattern.Replace(source, match =>
			{
				string args = match.Groups[1].Value;
				int lineNumber = CountLines(source, match.Index);
				return ExpandInclude(args, lineNumber);
			});
		}

		/// <summary>
		/// Expands a single include directive.
		/// </summary>
		private string ExpandInclude(string args, int lineNumber)
		{
			// Parse arguments: AssetName or "AssetName", with optional ConversationName
			string[] parts = args.Split(',');
			string assetName = StripQuotes(parts[0].Trim());
			string conversationName = parts.Length > 1 ? StripQuotes(parts[1].Trim()) : null;

			// Circular include detection
			if (_includeStack.Contains(assetName))
			{
				SourceSpan span = MakeSpan(lineNumber);
				_diagnostics.ReportError(span,
					$"Circular include detected: '{assetName}' is already in the include chain");
				return "";
			}

			// Load the referenced asset
			DialogueAsset asset = Resources.Load<DialogueAsset>(assetName);
			if (asset == null)
			{
				SourceSpan span = MakeSpan(lineNumber);
				_diagnostics.ReportError(span,
					$"Include failed: DialogueAsset '{assetName}' not found in Resources");
				return "";
			}

			string includedSource = asset.Content ?? "";

			// Recursively expand includes in the loaded content
			HashSet<string> childStack = new HashSet<string>(_includeStack) { assetName };
			DialoguePreprocessor childProcessor = new DialoguePreprocessor(_diagnostics, childStack);
			includedSource = childProcessor.Process(includedSource);

			// If a specific conversation was requested, extract just that block
			if (conversationName != null)
			{
				string extracted = ExtractConversation(includedSource, conversationName);
				if (extracted == null)
				{
					SourceSpan span = MakeSpan(lineNumber);
					_diagnostics.ReportWarning(span,
						$"Include: conversation '{conversationName}' not found in '{assetName}'");
					return "";
				}
				return extracted;
			}

			return includedSource;
		}

		/// <summary>
		/// Extracts a single conversation block from raw dialogue source text by scanning for
		/// <c>{{ConversationName(name)}}</c> headers.
		/// Returns the text from the header line through to the next header or end of text.
		/// Returns null if the conversation is not found.
		/// </summary>
		internal static string ExtractConversation(string source, string conversationName)
		{
			// Pattern matches {{ConversationName(X)}} on its own line
			string escapedName = Regex.Escape(conversationName);
			Regex headerPattern = new Regex(
				@"^[ \t]*\{\{ConversationName\(" + escapedName + @"\)\}\}[ \t]*$",
				RegexOptions.Multiline);

			Match startMatch = headerPattern.Match(source);
			if (!startMatch.Success)
				return null;

			int blockStart = startMatch.Index;

			// Find the next conversation header after this one
			Regex anyHeaderPattern = new Regex(
				@"^[ \t]*\{\{ConversationName\([^)]*\)\}\}[ \t]*$",
				RegexOptions.Multiline);

			Match nextMatch = anyHeaderPattern.Match(source, startMatch.Index + startMatch.Length);
			int blockEnd = nextMatch.Success ? nextMatch.Index : source.Length;

			return source.Substring(blockStart, blockEnd - blockStart).TrimEnd('\r', '\n');
		}

		/// <summary>
		/// Counts the 1-based line number at a given character offset in the source.
		/// </summary>
		private static int CountLines(string source, int offset)
		{
			int line = 1;
			for (int i = 0; i < offset && i < source.Length; i++)
			{
				if (source[i] == '\n')
					line++;
			}
			return line;
		}

		/// <summary>
		/// Strips surrounding double or single quotes from a string, if present.
		/// </summary>
		private static string StripQuotes(string value)
		{
			if (value.Length >= 2)
			{
				if ((value[0] == '"' && value[value.Length - 1] == '"') ||
					(value[0] == '\'' && value[value.Length - 1] == '\''))
				{
					return value.Substring(1, value.Length - 2);
				}
			}
			return value;
		}

		/// <summary>
		/// Creates a minimal SourceSpan for diagnostic reporting at a given line.
		/// </summary>
		private static SourceSpan MakeSpan(int line)
		{
			SourcePosition pos = new SourcePosition(line, 1, 0);
			return new SourceSpan(pos, pos);
		}
	}
}
