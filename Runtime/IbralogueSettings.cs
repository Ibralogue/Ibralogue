using UnityEngine;

namespace Ibralogue
{
	public enum LogLevel
	{
		ErrorsOnly,
		WarningsAndErrors,
		Verbose
	}

	/// <summary>
	/// Project-wide settings for Ibralogue. Create via
	/// <b>Edit > Project Settings > Ibralogue</b> or place an instance
	/// at <c>Assets/Resources/IbralogueSettings.asset</c>.
	/// </summary>
	public class IbralogueSettings : ScriptableObject
	{
		private const string ResourcePath = "IbralogueSettings";

		// --- Import ---

		[Header("Import")]
		[Tooltip("Run the parser on .ibra files at import time and surface syntax errors in the console.")]
		[SerializeField] private bool validateOnImport = true;

		// --- Logging ---

		[Header("Logging")]
		[Tooltip("Controls which messages Ibralogue writes to the console at runtime.\n\n" +
		         "Errors Only: Only errors.\n" +
		         "Warnings And Errors: Warnings and errors.\n" +
		         "Verbose: All messages including debug info.")]
		[SerializeField] private LogLevel logLevel = LogLevel.WarningsAndErrors;

		// --- Runtime ---

		[Header("Runtime")]
		[Tooltip("Clear all VariableStore variables when a new scene is loaded.")]
		[SerializeField] private bool clearVariablesOnSceneLoad;

		[Tooltip("Clear all VisitTracker records when a new scene is loaded.")]
		[SerializeField] private bool clearVisitsOnSceneLoad;

		// --- Public API ---

		public bool ValidateOnImport => validateOnImport;
		public LogLevel LogLevel => logLevel;
		public bool ClearVariablesOnSceneLoad => clearVariablesOnSceneLoad;
		public bool ClearVisitsOnSceneLoad => clearVisitsOnSceneLoad;

		// --- Singleton ---

		private static IbralogueSettings _instance;

		/// <summary>
		/// Returns the project settings instance, loading from Resources on first access.
		/// Returns null if no settings asset exists.
		/// </summary>
		public static IbralogueSettings Instance
		{
			get
			{
				if (_instance == null)
					_instance = Resources.Load<IbralogueSettings>(ResourcePath);
				return _instance;
			}
		}

		// --- Static convenience accessors with safe defaults ---

		public static bool ShouldValidateOnImport
		{
			get
			{
				IbralogueSettings s = Instance;
				return s == null || s.ValidateOnImport;
			}
		}

		public static LogLevel ActiveLogLevel
		{
			get
			{
				IbralogueSettings s = Instance;
				return s != null ? s.LogLevel : LogLevel.WarningsAndErrors;
			}
		}
	}
}
