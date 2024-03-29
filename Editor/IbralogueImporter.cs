using UnityEngine;
using System.IO;
using Ibralogue.Parser;


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
        /// <summary>
        /// Creates an instance of a scriptable object of type <see cref="DialogueAsset "/> and adds the contents of ctx file to the asset.
        /// </summary>
        /// <param name="ctx">The context for importing the asset.</param>
        public override void OnImportAsset(AssetImportContext ctx)
        {
            DialogueAsset subAsset = ScriptableObject.CreateInstance<DialogueAsset>();
            subAsset.Content = File.ReadAllText(ctx.assetPath);

            ctx.AddObjectToAsset("text", subAsset);
            ctx.SetMainObject(subAsset);
        }
    }
}
