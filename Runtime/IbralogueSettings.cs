using UnityEngine;

namespace Ibralogue
{
	public enum LogLevel
	{
		ErrorsOnly,
		WarningsAndErrors,
		Verbose
	}

	public enum DiagnosticStrictness
	{
		Normal,
		Strict
	}

	/// <summary>
	/// Project-wide settings for Ibralogue. Create via
	/// <b>Edit > Project Settings > Ibralogue</b> or place an instance
	/// at <c>Assets/Resources/IbralogueSettings.asset</c>.
	/// These settings apply across the entire project, regardless of how many
	/// dialogue engines exist in your scenes.
	/// </summary>
	public class IbralogueSettings : ScriptableObject
	{
		private const string ResourcePath = "IbralogueSettings";

		// ── Import ──────────────────────────────────────────────────

		[Header("Import")]
		[Tooltip("Run the full parser pipeline on .ibra files at import time and " +
		         "surface syntax errors immediately in the Unity console.")]
		[SerializeField] private bool validateOnImport = true;

		[Tooltip("Normal: warnings are logged but do not block playback.\n" +
		         "Strict: warnings are promoted to errors during import validation.")]
		[SerializeField] private DiagnosticStrictness diagnosticStrictness = DiagnosticStrictness.Normal;

		// ── Diagnostics ─────────────────────────────────────────────

		[Header("Diagnostics")]
		[Tooltip("Controls which messages Ibralogue writes to the console at runtime.\n\n" +
		         "Errors Only: Only errors.\n" +
		         "Warnings And Errors: Warnings and errors.\n" +
		         "Verbose: All messages including debug info.")]
		[SerializeField] private LogLevel logLevel = LogLevel.WarningsAndErrors;

		// ── Localization ────────────────────────────────────────────

		[Header("Localization")]
		[Tooltip("IETF BCP 47 language tag for the language your .ibra files are " +
		         "written in. Used as the fallback when no localization provider " +
		         "is active. Common values: en, en-US, de, fr, ja, zh-Hans.")]
		[SerializeField] private string baseLocale = "en";

		// ── State Management ────────────────────────────────────────

		[Header("State Management")]
		[Tooltip("Automatically clear all VariableStore variables when a new scene loads. " +
		         "Enable this for projects where dialogue state should not persist across scenes.")]
		[SerializeField] private bool clearVariablesOnSceneLoad;

		[Tooltip("Automatically clear all VisitTracker records when a new scene loads.")]
		[SerializeField] private bool clearVisitsOnSceneLoad;

		[Tooltip("Automatically invalidate the dialogue parser cache when a new scene loads. " +
		         "Enable this if you modify .ibra assets at runtime and need fresh parses per scene.")]
		[SerializeField] private bool clearParseCacheOnSceneLoad;

		// ── Public API ──────────────────────────────────────────────

		public bool ValidateOnImport => validateOnImport;
		public DiagnosticStrictness Strictness => diagnosticStrictness;
		public LogLevel LogLevel => logLevel;
		public string BaseLocale => baseLocale;
		public bool ClearVariablesOnSceneLoad => clearVariablesOnSceneLoad;
		public bool ClearVisitsOnSceneLoad => clearVisitsOnSceneLoad;
		public bool ClearParseCacheOnSceneLoad => clearParseCacheOnSceneLoad;

		// ── Singleton ───────────────────────────────────────────────

		private static IbralogueSettings _instance;

		/// <summary>
		/// Returns the project settings instance, loading from Resources on first
		/// access. Returns null if no settings asset exists.
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

		// ── Static convenience accessors with safe defaults ────────

		public static bool ShouldValidateOnImport
		{
			get
			{
				IbralogueSettings s = Instance;
				return s == null || s.ValidateOnImport;
			}
		}

		public static bool IsStrictMode
		{
			get
			{
				IbralogueSettings s = Instance;
				return s != null && s.Strictness == DiagnosticStrictness.Strict;
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

		public static string ActiveBaseLocale
		{
			get
			{
				IbralogueSettings s = Instance;
				return s != null ? s.BaseLocale : "en";
			}
		}
	}
}
