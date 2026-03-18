using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ibralogue.Editor
{
	/// <summary>
	/// Surfaces <see cref="IbralogueSettings"/> under Edit > Project Settings > Ibralogue.
	/// Auto-creates the settings asset in Resources if it does not exist.
	/// </summary>
	public static class IbralogueSettingsProvider
	{
		private const string SettingsPath = "Assets/Resources/IbralogueSettings.asset";

		[SettingsProvider]
		public static SettingsProvider CreateProvider()
		{
			return new SettingsProvider("Project/Ibralogue", SettingsScope.Project)
			{
				label = "Ibralogue",
				guiHandler = OnGUI,
				keywords = new[] { "Ibralogue", "Dialogue", "Import", "Validate" }
			};
		}

		private static UnityEditor.Editor _cachedEditor;

		private static void OnGUI(string searchContext)
		{
			IbralogueSettings settings = GetOrCreateSettings();

			if (_cachedEditor == null || _cachedEditor.target != settings)
				UnityEditor.Editor.CreateCachedEditor(settings, null, ref _cachedEditor);

			EditorGUI.BeginChangeCheck();
			_cachedEditor.OnInspectorGUI();
			if (EditorGUI.EndChangeCheck())
			{
				EditorUtility.SetDirty(settings);
				AssetDatabase.SaveAssetIfDirty(settings);
			}
		}

		private static IbralogueSettings GetOrCreateSettings()
		{
			IbralogueSettings settings = IbralogueSettings.Instance;
			if (settings != null)
				return settings;

			// Search project for any existing instance
			string[] guids = AssetDatabase.FindAssets("t:IbralogueSettings");
			if (guids.Length > 0)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[0]);
				settings = AssetDatabase.LoadAssetAtPath<IbralogueSettings>(path);
				if (settings != null)
					return settings;
			}

			// Create at default path
			string directory = Path.GetDirectoryName(SettingsPath);
			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);

			settings = ScriptableObject.CreateInstance<IbralogueSettings>();
			AssetDatabase.CreateAsset(settings, SettingsPath);
			AssetDatabase.SaveAssets();

			return settings;
		}
	}
}
