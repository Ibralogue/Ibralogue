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
    }
}
#endif