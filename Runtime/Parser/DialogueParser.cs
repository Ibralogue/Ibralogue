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
		/// <summary>
		/// Parses a dialogue asset into a list of conversations.
		/// </summary>
		/// <returns>
		/// A list of <see cref="Conversation"/> objects representing all conversations in the dialogue file.
		/// </returns>
		/// <exception cref="ArgumentException">Thrown when the dialogue has no conversations.</exception>
		public static List<Conversation> ParseDialogue(DialogueAsset dialogueAsset)
		{
			if (dialogueAsset == null)
				throw new ArgumentNullException(nameof(dialogueAsset));

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

			return conversations;
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
