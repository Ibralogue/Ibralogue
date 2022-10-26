using UnityEngine;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEditor.Experimental.AssetImporters;

namespace Ibralogue.Editor
{
    [ScriptedImporter(1, "ibra")]
    public class IbraImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            TextAsset subAsset = new TextAsset(File.ReadAllText(ctx.assetPath));
            ctx.AddObjectToAsset("text", subAsset);
            ctx.SetMainObject(subAsset);
        }
    }
}
