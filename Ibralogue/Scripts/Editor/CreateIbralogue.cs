using UnityEditor;

namespace Ibralogue.Editor
{
    public static class CreateIbralogue
    {
        private const string TemplatePath = "Assets/Ibralogue/Ibralogue/Templates/";
        private const string DefaultScript = "DefaultDialogue.ibra.txt";

        [MenuItem("Assets/Create/Ibralogue", false, 50)]
        public static void CreateDialogue()
        {
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(TemplatePath + DefaultScript, "New Dialogue.ibra");
        }
    }
}
