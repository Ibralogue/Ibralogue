using UnityEngine;

namespace Ibralogue
{
	public static class DialogueLogger
	{
		public static void LogWarning(string message, Object context = null)
		{
			Debug.LogWarning($"[Ibralogue] {message}", context);
		}

		public static void LogWarning(int line, int column, string message, Object context = null)
		{
			Debug.LogWarning($"[Ibralogue] [line {line}:{column}] {message}", context);
		}

		public static void LogError(string message, Object context = null)
		{
			Debug.LogError($"[Ibralogue] {message}", context);
		}

		public static void LogError(int line, string message, Object context = null)
		{
			Debug.LogError($"[Ibralogue] [line {line}] {message}", context);
		}
	}
}
