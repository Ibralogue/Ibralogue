using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ibralogue.Parser
{
	/// <summary>
	/// The main entry point for parsing Ibralogue dialogue files.
	/// </summary>
	public static class DialogueParser
	{
		private static readonly Dictionary<DialogueAsset, List<Conversation>> _cache =
			new Dictionary<DialogueAsset, List<Conversation>>();

		/// <summary>
		/// Parses a dialogue asset into a list of conversations. Results are cached
		/// so that repeated calls with the same asset skip the parsing pipeline.
		/// </summary>
		/// <returns>
		/// A list of <see cref="Conversation"/> objects representing all conversations in the dialogue file.
		/// There is always at least one: an empty file parses to a single empty conversation.
		/// </returns>
		public static List<Conversation> ParseDialogue(DialogueAsset dialogueAsset)
		{
			if (dialogueAsset == null)
				throw new ArgumentNullException(nameof(dialogueAsset));

			if (_cache.TryGetValue(dialogueAsset, out List<Conversation> cached))
				return cached;

			DiagnosticBag diagnostics = new DiagnosticBag();
			List<Conversation> conversations = Parse(
				dialogueAsset.Content ?? "", dialogueAsset.name ?? "unknown", diagnostics);

			ReportDiagnostics(diagnostics);

			_cache[dialogueAsset] = conversations;
			return conversations;
		}

		/// <summary>
		/// Removes a specific asset from the parse cache, forcing a re-parse
		/// on the next call to <see cref="ParseDialogue"/>.
		/// </summary>
		public static void InvalidateCache(DialogueAsset dialogueAsset)
		{
			if (dialogueAsset != null)
				_cache.Remove(dialogueAsset);
		}

		/// <summary>
		/// Clears the entire parse cache.
		/// </summary>
		public static void ClearCache()
		{
			_cache.Clear();
		}

		/// <summary>
		/// Runs the full parser pipeline on raw source text and returns every
		/// warning and error it found, without logging or caching anything.
		/// Used by the importer for import-time validation, and by project
		/// tooling that wants to check dialogue files itself.
		/// </summary>
		public static IReadOnlyList<Diagnostic> Validate(string source, string assetName)
		{
			DiagnosticBag diagnostics = new DiagnosticBag();

			try
			{
				Parse(source ?? "", assetName ?? "unknown", diagnostics);
			}
			catch (Exception exception)
			{
				// Not every stage reports through the bag: a malformed expression
				// throws, and would throw again when the dialogue is played.
				diagnostics.ReportError(default, exception.Message);
			}

			return diagnostics.Diagnostics;
		}

		private static List<Conversation> Parse(string source, string assetName, DiagnosticBag diagnostics)
		{
			DialoguePreprocessor preprocessor = new DialoguePreprocessor(diagnostics, assetName);
			source = preprocessor.Process(source);

			DialogueLexer lexer = new DialogueLexer(source, diagnostics);
			List<DialogueToken> tokens = lexer.Tokenize();

			DialogueTreeGenerator generator = new DialogueTreeGenerator(tokens, diagnostics);
			DialogueTree tree = generator.ParseIntoTree();

			DialogueAnalyzer analyzer = new DialogueAnalyzer(diagnostics, assetName);
			return analyzer.Analyze(tree);
		}

		/// <summary>
		/// Logs all collected diagnostics through the existing DialogueLogger system.
		/// </summary>
		private static void ReportDiagnostics(DiagnosticBag diagnostics)
		{
			foreach (Diagnostic diagnostic in diagnostics.Diagnostics)
			{
				int line = diagnostic.Span.Start.Line;
				string message = diagnostic.Message;

				switch (diagnostic.Severity)
				{
					case DiagnosticSeverity.Error:
						DialogueLogger.LogError(line, message);
						break;
					case DiagnosticSeverity.Warning:
						DialogueLogger.LogWarning(line, diagnostic.Span.Start.Column, message);
						break;
				}
			}
		}
	}
}
