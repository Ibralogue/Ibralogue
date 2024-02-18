using UnityEditor;

namespace Ibralogue.Editor
{
    public static class CreateIbralogue
    {
        private const string TemplatePath = "Assets/Ibralogue/Editor/Templates/";
        private const string DefaultScript = "DefaultDialogue.ibra.txt";

        /// <summary>
        /// Creates an Ibralogue file using Unity's <see cref="ProjectWindowUtil.CreateScriptAssetFromTemplateFile(string, string)"/> function.
        /// </summary>
        [MenuItem("Assets/Create/Ibralogue", false, 50)]
        public static void CreateDialogue()
        {
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(TemplatePath + DefaultScript, "New Dialogue.ibra");
        }
    }
}
