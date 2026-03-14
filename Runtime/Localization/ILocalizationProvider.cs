namespace Ibralogue.Localization
{
	/// <summary>
	/// Provides translated strings for dialogue localization.
	/// Implement this interface to integrate Ibralogue with any localization backend.
	/// </summary>
	public interface ILocalizationProvider
	{
		/// <summary>
		/// Looks up a translated string by localization key.
		/// Returns null if no translation exists for the key, in which case
		/// the engine falls back to the original text from the dialogue file.
		/// </summary>
		string Resolve(string key);
	}
}
