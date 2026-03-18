using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using System.Text;
using Ibralogue.Parser;

namespace Ibralogue.Editor
{
    [ScriptedImporter(1, "ibra")]
    public class IbralogueImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueAsset>();
            dialogue.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            dialogue.Content = File.ReadAllText(ctx.assetPath, Encoding.UTF8);

            ctx.AddObjectToAsset("Dialogue", dialogue);
            ctx.SetMainObject(dialogue);

            if (IbralogueSettings.ShouldValidateOnImport)
                ValidateDialogue(ctx, dialogue);
        }

        private static void ValidateDialogue(AssetImportContext ctx, DialogueAsset dialogue)
        {
            DiagnosticBag diagnostics = DialogueParser.Validate(
                dialogue.Content ?? "", dialogue.name ?? "unknown");

            bool strict = IbralogueSettings.IsStrictMode;

            foreach (Diagnostic diagnostic in diagnostics.Diagnostics)
            {
                string message = $"{dialogue.name}.ibra (line {diagnostic.Span.Start.Line}): {diagnostic.Message}";

                bool isError = diagnostic.Severity == DiagnosticSeverity.Error
                               || (strict && diagnostic.Severity == DiagnosticSeverity.Warning);

                if (isError)
                    ctx.LogImportError(message);
                else
                    ctx.LogImportWarning(message);
            }
        }
    }
}
