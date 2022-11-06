using UnityEngine;
using System.IO;

#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace Ibralogue.Editor
{
    [ScriptedImporter(2, "ibra")]
    public class IbralogueImporter : ScriptedImporter
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
