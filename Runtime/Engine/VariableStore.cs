using System;
using System.Collections.Generic;
using System.Globalization;

namespace Ibralogue
{
	/// <summary>
	/// Centralized variable storage with global and per-asset local scoping.
	/// Resolution order: local (current asset) -> global.
	/// </summary>
	public static class VariableStore
	{
		private static readonly Dictionary<string, object> _globals = new Dictionary<string, object>();
		private static readonly Dictionary<string, Dictionary<string, object>> _locals = new Dictionary<string, Dictionary<string, object>>();

		/// <summary>
		/// Invoked whenever a variable is created or changed. Parameters are
		/// (variableName, oldValue, newValue). oldValue is null for new variables.
		/// </summary>
		public static event Action<string, object, object> OnVariableChanged;

		/// <summary>
		/// Sets a variable in the global scope, accessible from all dialogue files.
		/// </summary>
		public static void SetGlobal(string name, object value)
		{
			_globals.TryGetValue(name, out object oldValue);
			_globals[name] = value;
			OnVariableChanged?.Invoke(name, oldValue, value);
		}

		/// <summary>
		/// Sets a variable in a file-local scope, only accessible within that asset.
		/// </summary>
		public static void SetLocal(string assetName, string name, object value)
		{
			if (!_locals.TryGetValue(assetName, out Dictionary<string, object> scope))
			{
				scope = new Dictionary<string, object>();
				_locals[assetName] = scope;
			}
			scope.TryGetValue(name, out object oldValue);
			scope[name] = value;
			OnVariableChanged?.Invoke(name, oldValue, value);
		}

		/// <summary>
		/// Sets a variable using the scoping rule: if the variable already exists in any scope,
		/// updates it in place. Otherwise creates it as a local in the given asset.
		/// </summary>
		public static void Set(string assetName, string name, object value)
		{
			if (_locals.TryGetValue(assetName, out Dictionary<string, object> scope) && scope.ContainsKey(name))
			{
				scope.TryGetValue(name, out object oldLocal);
				scope[name] = value;
				OnVariableChanged?.Invoke(name, oldLocal, value);
				return;
			}

			if (_globals.ContainsKey(name))
			{
				_globals.TryGetValue(name, out object oldGlobal);
				_globals[name] = value;
				OnVariableChanged?.Invoke(name, oldGlobal, value);
				return;
			}

			SetLocal(assetName, name, value);
		}

		/// <summary>
		/// Resolves a variable by name: checks local scope first, then global scope.
		/// Returns null if the variable is not defined in any scope.
		/// </summary>
		public static object Resolve(string assetName, string name)
		{
			if (assetName != null && _locals.TryGetValue(assetName, out Dictionary<string, object> scope) && scope.TryGetValue(name, out object localVal))
				return localVal;

			if (_globals.TryGetValue(name, out object globalVal))
				return globalVal;

			return null;
		}

		/// <summary>
		/// Checks whether a variable exists in any scope, including variables
		/// explicitly set to null.
		/// </summary>
		public static bool IsDefined(string assetName, string name)
		{
			if (assetName != null && _locals.TryGetValue(assetName, out Dictionary<string, object> scope)
			    && scope.ContainsKey(name))
				return true;

			return _globals.ContainsKey(name);
		}

		/// <summary>
		/// Clears local variables for a specific asset.
		/// </summary>
		public static void ClearLocals(string assetName)
		{
			_locals.Remove(assetName);
		}

		/// <summary>
		/// Clears all variables, both global and local.
		/// </summary>
		public static void ClearAll()
		{
			_globals.Clear();
			_locals.Clear();
		}

		/// <summary>
		/// Converts a stored variable value to its string representation.
		/// Used when substituting variables into dialogue text at runtime.
		/// </summary>
		public static string ToString(object value)
		{
			if (value == null) return "";
			if (value is double d) return d.ToString(CultureInfo.InvariantCulture);
			if (value is bool b) return b ? "true" : "false";
			return value.ToString();
		}

		/// <summary>
		/// Exports a snapshot of all variable state for serialization by a save system.
		/// </summary>
		public static VariableSnapshot ExportState()
		{
			VariableSnapshot snapshot = new VariableSnapshot();

			foreach (KeyValuePair<string, object> kvp in _globals)
				snapshot.Globals[kvp.Key] = ToString(kvp.Value);

			foreach (KeyValuePair<string, Dictionary<string, object>> asset in _locals)
			{
				Dictionary<string, string> assetScope = new Dictionary<string, string>();
				foreach (KeyValuePair<string, object> kvp in asset.Value)
					assetScope[kvp.Key] = ToString(kvp.Value);
				snapshot.Locals[asset.Key] = assetScope;
			}

			return snapshot;
		}

		/// <summary>
		/// Restores variable state from a previously exported snapshot. Clears all
		/// existing variables before importing.
		/// </summary>
		public static void ImportState(VariableSnapshot snapshot)
		{
			if (snapshot == null)
				throw new ArgumentNullException(nameof(snapshot));

			ClearAll();

			foreach (KeyValuePair<string, string> kvp in snapshot.Globals)
				_globals[kvp.Key] = ParseStoredValue(kvp.Value);

			foreach (KeyValuePair<string, Dictionary<string, string>> asset in snapshot.Locals)
			{
				Dictionary<string, object> scope = new Dictionary<string, object>();
				foreach (KeyValuePair<string, string> kvp in asset.Value)
					scope[kvp.Key] = ParseStoredValue(kvp.Value);
				_locals[asset.Key] = scope;
			}
		}

		private static object ParseStoredValue(string value)
		{
			if (string.IsNullOrEmpty(value)) return null;
			if (value == "true") return true;
			if (value == "false") return false;
			if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
				return d;
			return value;
		}
	}

	/// <summary>
	/// A serializable snapshot of all dialogue variable state. Use with
	/// <see cref="VariableStore.ExportState"/> and <see cref="VariableStore.ImportState"/>
	/// to integrate with save/load systems.
	/// </summary>
	[Serializable]
	public class VariableSnapshot
	{
		public Dictionary<string, string> Globals = new Dictionary<string, string>();
		public Dictionary<string, Dictionary<string, string>> Locals = new Dictionary<string, Dictionary<string, string>>();
	}
}
