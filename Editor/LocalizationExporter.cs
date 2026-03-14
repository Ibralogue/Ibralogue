using System.Collections.Generic;
using System.IO;
using System.Text;
using Ibralogue.Parser;
using UnityEditor;
using UnityEngine;

namespace Ibralogue.Editor
{
	public static class LocalizationExporter
	{
		[MenuItem("Assets/Ibralogue/Export Localization Template", false, 100)]
		public static void ExportTemplate()
		{
			Object selected = Selection.activeObject;
			string assetPath = AssetDatabase.GetAssetPath(selected);

			if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".ibra"))
			{
				Debug.LogError("[Ibralogue] Select an .ibra file to export a localization template.");
				return;
			}

			string source = File.ReadAllText(assetPath, Encoding.UTF8);
			string assetName = Path.GetFileNameWithoutExtension(assetPath);

			DiagnosticBag diagnostics = new DiagnosticBag();
			DialoguePreprocessor preprocessor = new DialoguePreprocessor(diagnostics, assetName);
			source = preprocessor.Process(source);

			DialogueLexer lexer = new DialogueLexer(source, diagnostics);
			List<DialogueToken> tokens = lexer.Tokenize();

			DialogueTreeGenerator generator = new DialogueTreeGenerator(tokens, diagnostics);
			DialogueTree tree = generator.ParseIntoTree();

			DialogueAnalyzer analyzer = new DialogueAnalyzer(diagnostics, assetName);
			List<Conversation> conversations = analyzer.Analyze(tree);

			StringBuilder csv = new StringBuilder();
			csv.AppendLine("key,text");

			HashSet<string> speakerKeys = new HashSet<string>();

			foreach (Conversation conversation in conversations)
				CollectEntries(conversation.Content, csv, speakerKeys);

			string directory = Path.GetDirectoryName(assetPath);
			string outputPath = Path.Combine(directory, $"{assetName}.csv");
			File.WriteAllText(outputPath, csv.ToString(), Encoding.UTF8);

			AssetDatabase.Refresh();
			Debug.Log($"[Ibralogue] Localization template exported to {outputPath}");
		}

		[MenuItem("Assets/Ibralogue/Export Localization Template", true)]
		public static bool ExportTemplateValidation()
		{
			string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
			return !string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".ibra");
		}

		private static void CollectEntries(List<RuntimeContentNode> content, StringBuilder csv,
			HashSet<string> speakerKeys)
		{
			foreach (RuntimeContentNode node in content)
			{
				if (node is RuntimeLine line)
				{
					string text = BuildRawText(line.Sentences);
					csv.Append(CsvEscape(line.LocalizationKey));
					csv.Append(',');
					csv.AppendLine(CsvEscape(text));

					if (line.SpeakerLocalizationKey != null && speakerKeys.Add(line.SpeakerLocalizationKey))
					{
						csv.Append(CsvEscape(line.SpeakerLocalizationKey));
						csv.Append(',');
						csv.AppendLine(CsvEscape(line.RawSpeaker));
					}
				}
				else if (node is RuntimeChoicePoint choicePoint)
				{
					foreach (ChoiceData choice in choicePoint.Choices)
					{
						csv.Append(CsvEscape(choice.LocalizationKey));
						csv.Append(',');
						csv.AppendLine(CsvEscape(choice.RawText));
					}
				}
				else if (node is RuntimeConditionalBlock cond)
				{
					foreach (RuntimeBranch branch in cond.Branches)
						CollectEntries(branch.Body, csv, speakerKeys);
				}
			}
		}

		private static string BuildRawText(List<SentenceNode> sentences)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < sentences.Count; i++)
			{
				if (i > 0) sb.Append('\n');
				foreach (InlineNode fragment in sentences[i].Fragments)
				{
					if (fragment is TextNode textNode)
						sb.Append(textNode.Text);
					else if (fragment is VariableReferenceNode varNode)
						sb.Append('$').Append(varNode.VariableName);
					else if (fragment is FunctionInvocationNode funcNode)
					{
						sb.Append("{{").Append(funcNode.FunctionName);
						if (funcNode.Arguments.Count > 0)
							sb.Append('(').Append(string.Join(", ", funcNode.Arguments)).Append(')');
						sb.Append("}}");
					}
				}
			}
			return sb.ToString();
		}

		private static string CsvEscape(string value)
		{
			if (value == null) return "";
			if (value.IndexOf(',') >= 0 || value.IndexOf('"') >= 0 || value.IndexOf('\n') >= 0)
				return "\"" + value.Replace("\"", "\"\"") + "\"";
			return value;
		}
	}
}
