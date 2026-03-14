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
		/// Sets a variable in the global scope, accessible from all dialogue files.
		/// </summary>
		public static void SetGlobal(string name, object value)
		{
			_globals[name] = value;
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
			scope[name] = value;
		}

		/// <summary>
		/// Sets a variable using the scoping rule: if the variable already exists in any scope,
		/// updates it in place. Otherwise creates it as a local in the given asset.
		/// </summary>
		public static void Set(string assetName, string name, object value)
		{
			if (_locals.TryGetValue(assetName, out Dictionary<string, object> scope) && scope.ContainsKey(name))
			{
				scope[name] = value;
				return;
			}

			if (_globals.ContainsKey(name))
			{
				_globals[name] = value;
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
		/// Checks whether a variable exists in any scope.
		/// </summary>
		public static bool IsDefined(string assetName, string name)
		{
			return Resolve(assetName, name) != null;
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
	}
}
