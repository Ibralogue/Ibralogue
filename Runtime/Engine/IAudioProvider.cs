namespace Ibralogue
{
	/// <summary>
	/// Provides audio playback for dialogue lines. Implement this interface to
	/// integrate Ibralogue with any audio backend (Unity AudioSource, FMOD, Wwise, etc.).
	/// </summary>
	public interface IAudioProvider
	{
		/// <summary>
		/// Plays an audio clip identified by the given clip ID (typically a Resources path
		/// or an identifier understood by the audio backend).
		/// </summary>
		void Play(string clipId);

		/// <summary>
		/// Stops the currently playing audio.
		/// </summary>
		void Stop();
	}
}
