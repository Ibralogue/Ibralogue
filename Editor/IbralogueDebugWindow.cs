using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ibralogue.Editor
{
	/// <summary>
	/// Editor window that displays live VariableStore and VisitTracker state
	/// during play mode. Open via Window > Ibralogue > Debug Inspector.
	/// </summary>
	public class IbralogueDebugWindow : EditorWindow
	{
		private Vector2 _scrollPosition;
		private bool _showGlobals = true;
		private bool _showLocals = true;
		private bool _showVisits = true;
		private string _searchFilter = "";

		[MenuItem("Window/Ibralogue/Debug Inspector")]
		public static void Open()
		{
			GetWindow<IbralogueDebugWindow>("Ibralogue Debug");
		}

		private void OnEnable()
		{
			EditorApplication.playModeStateChanged += OnPlayModeChanged;
		}

		private void OnDisable()
		{
			EditorApplication.playModeStateChanged -= OnPlayModeChanged;
		}

		private void OnPlayModeChanged(PlayModeStateChange state)
		{
			Repaint();
		}

		private void OnInspectorUpdate()
		{
			if (Application.isPlaying)
				Repaint();
		}

		private void OnGUI()
		{
			if (!Application.isPlaying)
			{
				EditorGUILayout.HelpBox("Enter Play Mode to inspect live dialogue state.", MessageType.Info);
				return;
			}

			EditorGUILayout.Space(4);
			_searchFilter = EditorGUILayout.TextField("Search", _searchFilter);
			EditorGUILayout.Space(4);

			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

			DrawGlobals();
			DrawLocals();
			DrawVisits();

			EditorGUILayout.EndScrollView();

			EditorGUILayout.Space(4);
			DrawFooter();
		}

		private void DrawGlobals()
		{
			IReadOnlyDictionary<string, object> globals = VariableStore.Globals;

			_showGlobals = EditorGUILayout.Foldout(_showGlobals,
				$"Global Variables ({globals.Count})", true, EditorStyles.foldoutHeader);

			if (!_showGlobals) return;

			EditorGUI.indentLevel++;

			if (globals.Count == 0)
			{
				EditorGUILayout.LabelField("No global variables set.", EditorStyles.miniLabel);
			}
			else
			{
				foreach (KeyValuePair<string, object> kvp in globals)
				{
					if (!MatchesFilter(kvp.Key)) continue;
					DrawVariable(kvp.Key, kvp.Value);
				}
			}

			EditorGUI.indentLevel--;
			EditorGUILayout.Space(4);
		}

		private void DrawLocals()
		{
			IReadOnlyDictionary<string, Dictionary<string, object>> locals = VariableStore.Locals;

			_showLocals = EditorGUILayout.Foldout(_showLocals,
				$"Local Variables ({CountLocals(locals)})", true, EditorStyles.foldoutHeader);

			if (!_showLocals) return;

			EditorGUI.indentLevel++;

			if (locals.Count == 0)
			{
				EditorGUILayout.LabelField("No local variables set.", EditorStyles.miniLabel);
			}
			else
			{
				foreach (KeyValuePair<string, Dictionary<string, object>> asset in locals)
				{
					EditorGUILayout.LabelField(asset.Key, EditorStyles.boldLabel);
					EditorGUI.indentLevel++;

					foreach (KeyValuePair<string, object> kvp in asset.Value)
					{
						if (!MatchesFilter(kvp.Key) && !MatchesFilter(asset.Key)) continue;
						DrawVariable(kvp.Key, kvp.Value);
					}

					EditorGUI.indentLevel--;
				}
			}

			EditorGUI.indentLevel--;
			EditorGUILayout.Space(4);
		}

		private void DrawVisits()
		{
			IReadOnlyCollection<string> visits = VisitTracker.VisitedKeys;

			_showVisits = EditorGUILayout.Foldout(_showVisits,
				$"Visited Keys ({visits.Count})", true, EditorStyles.foldoutHeader);

			if (!_showVisits) return;

			EditorGUI.indentLevel++;

			if (visits.Count == 0)
			{
				EditorGUILayout.LabelField("No keys marked as visited.", EditorStyles.miniLabel);
			}
			else
			{
				foreach (string key in visits)
				{
					if (!MatchesFilter(key)) continue;
					EditorGUILayout.LabelField(key, "visited");
				}
			}

			EditorGUI.indentLevel--;
		}

		private void DrawVariable(string name, object value)
		{
			string valueStr = VariableStore.ToString(value);
			string typeStr = value != null ? value.GetType().Name : "null";
			EditorGUILayout.LabelField($"${name}", $"{valueStr}  ({typeStr})");
		}

		private void DrawFooter()
		{
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("Clear Variables"))
				VariableStore.ClearAll();

			if (GUILayout.Button("Clear Visits"))
				VisitTracker.Clear();

			EditorGUILayout.EndHorizontal();
		}

		private bool MatchesFilter(string text)
		{
			if (string.IsNullOrEmpty(_searchFilter)) return true;
			return text.IndexOf(_searchFilter, System.StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static int CountLocals(IReadOnlyDictionary<string, Dictionary<string, object>> locals)
		{
			int count = 0;
			foreach (KeyValuePair<string, Dictionary<string, object>> asset in locals)
				count += asset.Value.Count;
			return count;
		}
	}
}
