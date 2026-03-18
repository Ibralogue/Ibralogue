using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ibralogue
{
	/// <summary>
	/// Handles project-wide scene lifecycle hooks based on <see cref="IbralogueSettings"/>.
	/// </summary>
	internal static class IbralogueSceneHandler
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize()
		{
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			IbralogueSettings settings = IbralogueSettings.Instance;
			if (settings == null) return;

			if (settings.ClearVariablesOnSceneLoad)
				VariableStore.ClearAll();

			if (settings.ClearVisitsOnSceneLoad)
				VisitTracker.Clear();
		}
	}
}
