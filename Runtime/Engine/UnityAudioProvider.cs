using UnityEngine;

namespace Ibralogue
{
	/// <summary>
	/// Built-in audio provider that plays clips using a Unity AudioSource.
	/// Clips are loaded via <see cref="Resources.Load{T}(string)"/> using the clip ID as the path.
	/// </summary>
	[RequireComponent(typeof(AudioSource))]
	public class UnityAudioProvider : MonoBehaviour, IAudioProvider
	{
		private AudioSource _audioSource;
		private AudioClip _cachedClip;
		private string _cachedClipId;

		private void Awake()
		{
			_audioSource = GetComponent<AudioSource>();
		}

		public void Play(string clipId)
		{
			if (string.IsNullOrEmpty(clipId))
				return;

			AudioClip clip;
			if (clipId == _cachedClipId && _cachedClip != null)
			{
				clip = _cachedClip;
			}
			else
			{
				clip = Resources.Load<AudioClip>(clipId);
				_cachedClipId = clipId;
				_cachedClip = clip;
			}

			if (clip == null)
			{
				DialogueLogger.LogWarning($"AudioClip not found at path: {clipId}");
				return;
			}

			_audioSource.clip = clip;
			_audioSource.Play();
		}

		public void Stop()
		{
			if (_audioSource != null && _audioSource.isPlaying)
				_audioSource.Stop();
		}
	}
}
