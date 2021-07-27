#if UNITY_EDITOR
using UnityEditor;

namespace Ibralogue
{
    public class CreateIbralogue
    {
        private const string TemplatePath = "Assets/Ibralogue/Templates/";
        private const string DefaultScript = "DefaultDialogue.ibra.txt";

        [MenuItem("Assets/Create/Ibralogue", false, 50)]
        public static void CreateDialogue()
        {
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(TemplatePath + DefaultScript, "New Dialogue.ibra");
        }
        
        // [OnOpenAsset(2)]
        // private static bool OpenDialogue(int instanceID, int line)
        // {
        //     string objectPath = AssetDatabase.GetAssetPath(EditorUtility.InstanceIDToObject(instanceID));
        //     string objectExtension = Path.GetExtension(objectPath);
        //     return objectExtension == ".ibra";
        // }
    }
}
#endif