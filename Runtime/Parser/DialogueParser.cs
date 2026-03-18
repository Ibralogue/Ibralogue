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
		/// </returns>
		/// <exception cref="ArgumentException">Thrown when the dialogue has no conversations.</exception>
		public static List<Conversation> ParseDialogue(DialogueAsset dialogueAsset)
		{
			if (dialogueAsset == null)
				throw new ArgumentNullException(nameof(dialogueAsset));

			if (_cache.TryGetValue(dialogueAsset, out List<Conversation> cached))
				return cached;

			string source = dialogueAsset.Content ?? "";
			string assetName = dialogueAsset.name ?? "unknown";

			DiagnosticBag diagnostics = new DiagnosticBag();

			DialoguePreprocessor preprocessor = new DialoguePreprocessor(diagnostics, assetName);
			source = preprocessor.Process(source);

			DialogueLexer lexer = new DialogueLexer(source, diagnostics);
			List<DialogueToken> tokens = lexer.Tokenize();

			DialogueTreeGenerator generator = new DialogueTreeGenerator(tokens, diagnostics);
			DialogueTree tree = generator.ParseIntoTree();

			DialogueAnalyzer analyzer = new DialogueAnalyzer(diagnostics, assetName);
			List<Conversation> conversations = analyzer.Analyze(tree);

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
		/// Runs the full parser pipeline on raw source text and returns diagnostics
		/// without logging. Used by the importer for import-time validation.
		/// </summary>
		internal static DiagnosticBag Validate(string source, string assetName)
		{
			DiagnosticBag diagnostics = new DiagnosticBag();

			try
			{
				DialoguePreprocessor preprocessor = new DialoguePreprocessor(diagnostics, assetName);
				source = preprocessor.Process(source);

				DialogueLexer lexer = new DialogueLexer(source, diagnostics);
				List<DialogueToken> tokens = lexer.Tokenize();

				DialogueTreeGenerator generator = new DialogueTreeGenerator(tokens, diagnostics);
				DialogueTree tree = generator.ParseIntoTree();

				DialogueAnalyzer analyzer = new DialogueAnalyzer(diagnostics, assetName);
				analyzer.Analyze(tree);
			}
			catch (System.Exception)
			{
				// Swallow parse exceptions during validation; the diagnostics bag
				// already contains the structured errors.
			}

			return diagnostics;
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
