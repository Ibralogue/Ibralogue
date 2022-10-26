using UnityEngine;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEditor.Experimental.AssetImporters;

namespace Ibralogue.Editor
{
    [ScriptedImporter(2, "ibra")]
    public class IbraImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            DialogueAsset subAsset = ScriptableObject.CreateInstance<DialogueAsset>();
            subAsset.Content = File.ReadAllText(ctx.assetPath);

            ctx.AddObjectToAsset("text", subAsset);
            ctx.SetMainObject(subAsset);
        }
    }
}
