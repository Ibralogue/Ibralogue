using System.Collections.Generic;

namespace Ibralogue.Parser
{
	/// <summary>
	/// Shared helpers for parsing the <c>Name(arg1, arg2)</c> invocation syntax
	/// used by commands, inline functions, and preprocessor directives.
	/// </summary>
	internal static class InvocationSyntax
	{
		/// <summary>
		/// Extracts the name portion from a <c>"Name(args)"</c> string.
		/// Returns the full trimmed value when no parentheses are present.
		/// </summary>
		public static string ExtractName(string value)
		{
			int parenIndex = value.IndexOf('(');
			return parenIndex >= 0 ? value.Substring(0, parenIndex).Trim() : value.Trim();
		}

		/// <summary>
		/// Extracts the raw argument string from between the parentheses in
		/// a <c>"Name(args)"</c> value. Returns an empty string when no
		/// parentheses are present.
		/// </summary>
		public static string ExtractRawArgument(string value)
		{
			int open = value.IndexOf('(');
			int close = value.LastIndexOf(')');
			if (open >= 0 && close > open)
				return value.Substring(open + 1, close - open - 1);
			return "";
		}

		/// <summary>
		/// Splits a raw comma-separated argument string into individually trimmed parts.
		/// Returns an empty list for null, empty, or whitespace-only input.
		/// </summary>
		public static List<string> SplitArguments(string rawArgs)
		{
			List<string> result = new List<string>();
			if (string.IsNullOrEmpty(rawArgs) || rawArgs.Trim().Length == 0)
				return result;

			string[] parts = rawArgs.Split(',');
			foreach (string part in parts)
			{
				string trimmed = part.Trim();
				if (trimmed.Length > 0)
					result.Add(trimmed);
			}
			return result;
		}

		/// <summary>
		/// Strips surrounding double or single quotes from a string, if present.
		/// </summary>
		public static string StripQuotes(string value)
		{
			if (value.Length >= 2)
			{
				if ((value[0] == '"' && value[value.Length - 1] == '"') ||
					(value[0] == '\'' && value[value.Length - 1] == '\''))
				{
					return value.Substring(1, value.Length - 2);
				}
			}
			return value;
		}
	}
}
