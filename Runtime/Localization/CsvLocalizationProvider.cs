using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Ibralogue.Localization
{
	/// <summary>
	/// Built-in localization provider that loads translations from CSV files.
	/// CSV files follow the format: key,text (RFC 4180 quoting supported).
	/// Place translation files in a Resources folder using the naming convention
	/// {AssetName}.{locale} (e.g. MyDialogue.fr, MyDialogue.ja).
	/// </summary>
	public class CsvLocalizationProvider : MonoBehaviour, ILocalizationProvider
	{
		[SerializeField] private string locale;

		private readonly Dictionary<string, Dictionary<string, string>> _tables =
			new Dictionary<string, Dictionary<string, string>>();

		/// <summary>
		/// Changes the active locale. Call <see cref="LoadTable"/> afterward to load
		/// translations for a specific dialogue asset.
		/// </summary>
		public void SetLocale(string newLocale)
		{
			locale = newLocale;
			_tables.Clear();
		}

		/// <summary>
		/// Loads (or reloads) a translation table for a dialogue asset from Resources.
		/// Looks for a TextAsset at the path "{assetName}.{locale}" inside a Resources folder.
		/// </summary>
		public void LoadTable(string assetName)
		{
			if (string.IsNullOrEmpty(locale))
				return;

			string path = $"{assetName}.{locale}";
			TextAsset csv = Resources.Load<TextAsset>(path);
			if (csv == null)
			{
				_tables.Remove(assetName);
				return;
			}

			_tables[assetName] = ParseCsv(csv.text);
		}

		public string Resolve(string key)
		{
			foreach (KeyValuePair<string, Dictionary<string, string>> table in _tables)
			{
				if (table.Value.TryGetValue(key, out string value))
					return value;
			}
			return null;
		}

		/// <summary>
		/// Parses a CSV string into a key-value dictionary.
		/// Expects rows of: key,text (first row may be a header and is skipped
		/// if the first field is "key").
		/// </summary>
		internal static Dictionary<string, string> ParseCsv(string csv)
		{
			Dictionary<string, string> result = new Dictionary<string, string>();
			List<string> fields = new List<string>();
			bool isFirstRow = true;

			using (StringReader reader = new StringReader(csv))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					ParseCsvLine(line, reader, fields);

					if (fields.Count < 2)
						continue;

					if (isFirstRow)
					{
						isFirstRow = false;
						if (fields[0].Trim().ToLowerInvariant() == "key")
							continue;
					}

					result[fields[0].Trim()] = fields[1];
				}
			}

			return result;
		}

		private static void ParseCsvLine(string line, StringReader reader, List<string> fields)
		{
			fields.Clear();
			StringBuilder field = new StringBuilder();
			int i = 0;

			while (i <= line.Length)
			{
				if (i == line.Length)
				{
					fields.Add(field.ToString());
					break;
				}

				char c = line[i];

				if (c == '"')
				{
					i++;
					while (true)
					{
						if (i >= line.Length)
						{
							field.Append('\n');
							line = reader.ReadLine();
							if (line == null) break;
							i = 0;
							continue;
						}

						if (line[i] == '"')
						{
							if (i + 1 < line.Length && line[i + 1] == '"')
							{
								field.Append('"');
								i += 2;
							}
							else
							{
								i++;
								break;
							}
						}
						else
						{
							field.Append(line[i]);
							i++;
						}
					}
				}
				else if (c == ',')
				{
					fields.Add(field.ToString());
					field.Clear();
					i++;
				}
				else
				{
					field.Append(c);
					i++;
				}
			}
		}
	}
}
