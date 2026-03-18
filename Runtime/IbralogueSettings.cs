using UnityEngine;

namespace Ibralogue
{
	/// <summary>
	/// Project-wide settings for Ibralogue. Create via
	/// <b>Edit > Project Settings > Ibralogue</b> or place an instance
	/// at <c>Assets/Resources/IbralogueSettings.asset</c>.
	/// </summary>
	public class IbralogueSettings : ScriptableObject
	{
		private const string ResourcePath = "IbralogueSettings";

		[Header("Import")]
		[Tooltip("When enabled, .ibra files are validated through the parser at import time. " +
		         "Syntax errors are surfaced immediately in the console.")]
		[SerializeField] private bool validateOnImport = true;

		/// <summary>
		/// When true, the importer runs the parser on every .ibra file at import time
		/// and surfaces diagnostics in the console.
		/// </summary>
		public bool ValidateOnImport => validateOnImport;

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

		/// <summary>
		/// Returns the settings instance, or falls back to defaults when no asset exists.
		/// </summary>
		public static bool ShouldValidateOnImport
		{
			get
			{
				IbralogueSettings settings = Instance;
				return settings == null || settings.ValidateOnImport;
			}
		}
	}
}
