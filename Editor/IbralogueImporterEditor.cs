using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Ibralogue.Editor
{
    [CustomEditor(typeof(IbralogueImporter))]
    public class IbralogueImporterEditor : ScriptedImporterEditor
    {
        private string _filePreview;
        private Vector2 _scrollPosition;

        public override void OnEnable()
        {
            base.OnEnable();

            var importer = (IbralogueImporter)target;
            _filePreview = System.IO.File.ReadAllText(importer.assetPath);
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("File Preview (.ibra)", EditorStyles.boldLabel);

            using (EditorGUILayout.ScrollViewScope scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition, GUILayout.MinHeight(200)))
            {
                _scrollPosition = scroll.scrollPosition;
                EditorGUILayout.SelectableLabel(_filePreview, EditorStyles.textArea, GUILayout.ExpandHeight(true));
            }

            ApplyRevertGUI();
        }
    }
}