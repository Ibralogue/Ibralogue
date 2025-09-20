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
        /// <summary>
        /// Creates an instance of a scriptable object of type <see cref="DialogueAsset "/> and adds the contents of ctx file to the asset.
        /// </summary>
        /// <param name="ctx">The context for importing the asset.</param>
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueAsset>();
            dialogue.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            dialogue.Content = File.ReadAllText(ctx.assetPath, Encoding.UTF8);

            ctx.AddObjectToAsset("Dialogue", dialogue);
            ctx.SetMainObject(dialogue);
        }
    }
}
