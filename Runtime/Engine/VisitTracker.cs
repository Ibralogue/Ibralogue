using System;
using System.Collections.Generic;

namespace Ibralogue
{
	/// <summary>
	/// Opt-in visit tracking for dialogue conversations and arbitrary keys.
	/// Nothing is tracked unless <see cref="Mark"/> is called explicitly.
	/// Visited state can be checked from dialogue using
	/// <c>{{If(Visited("Tavern"))}}</c>.
	/// </summary>
	public static class VisitTracker
	{
		private static readonly HashSet<string> _visited = new HashSet<string>();

		internal static IReadOnlyCollection<string> VisitedKeys => _visited;

		/// <summary>
		/// Marks a key as visited.
		/// </summary>
		public static void Mark(string key)
		{
			if (!string.IsNullOrEmpty(key))
				_visited.Add(key);
		}

		/// <summary>
		/// Returns true if the key has been marked as visited.
		/// </summary>
		public static bool HasVisited(string key)
		{
			return key != null && _visited.Contains(key);
		}

		/// <summary>
		/// Clears all visit records.
		/// </summary>
		public static void Clear()
		{
			_visited.Clear();
		}

		/// <summary>
		/// Exports all visited keys for serialization by a save system.
		/// </summary>
		public static VisitSnapshot ExportState()
		{
			VisitSnapshot snapshot = new VisitSnapshot();
			foreach (string key in _visited)
				snapshot.Keys.Add(key);
			return snapshot;
		}

		/// <summary>
		/// Restores visit state from a previously exported snapshot.
		/// Clears all existing records before importing.
		/// </summary>
		public static void ImportState(VisitSnapshot snapshot)
		{
			if (snapshot == null)
				throw new ArgumentNullException(nameof(snapshot));

			_visited.Clear();
			foreach (string key in snapshot.Keys)
				_visited.Add(key);
		}

	}

	/// <summary>
	/// A serializable snapshot of all visited keys. Use with
	/// <see cref="VisitTracker.ExportState"/> and <see cref="VisitTracker.ImportState"/>
	/// to integrate with save/load systems.
	/// </summary>
	[Serializable]
	public class VisitSnapshot
	{
		public List<string> Keys = new List<string>();
	}
}
