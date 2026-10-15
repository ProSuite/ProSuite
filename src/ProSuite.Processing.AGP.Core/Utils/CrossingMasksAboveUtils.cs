using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;

namespace ProSuite.Processing.AGP.Core.Utils;

/// <summary>
/// In GeniusDB there was a relation table "OUAREL" containing
/// (CLASSNAME, ABOVEOID, MASKOID) tuples. For K2, it was decided
/// to drop this table (simplify data model) and instead have a
/// field "ABOVE_FEATURES" on the OUMASK classes, which contains
/// a formatted string representing the above features.
/// Format: "CLASSNAME,OID{,OID}{;CLASSNAME,OID{,OID}}"
/// </summary>
public static class CrossingMasksAboveUtils
{
	public static void SetAboveFeatures(Feature maskFeature, IEnumerable<Feature> aboveFeatures, string fieldName)
	{
		if (maskFeature is null)
			throw new ArgumentNullException(nameof(maskFeature));
		if (aboveFeatures is null)
			throw new ArgumentNullException(nameof(aboveFeatures));
		if (string.IsNullOrEmpty(fieldName))
			throw new ArgumentNullException(nameof(fieldName));

		var dict = new Dictionary<string, List<long>>(StringComparer.OrdinalIgnoreCase);

		foreach (var aboveFeature in aboveFeatures)
		{
			long aboveOid = aboveFeature.GetObjectID();
			using var aboveClass = aboveFeature.GetTable();
			var name = aboveClass.GetName();
			name = DatasetNameUtils.UnqualifyDatasetName(name);

			if (!dict.TryGetValue(name, out var list))
			{
				list = new List<long>();
				dict[name] = list;
			}

			list.Add(aboveOid);
		}

		string text = FormatAboveFeatures(dict);
		maskFeature[fieldName] = text;
		maskFeature.Store();
	}

	public static List<Feature> GetAboveFeatures(Feature maskFeature, string fieldName)
	{
		if (maskFeature is null)
			throw new ArgumentNullException(nameof(maskFeature));
		if (string.IsNullOrEmpty(fieldName))
			throw new ArgumentNullException(nameof(fieldName));

		object value = maskFeature[fieldName];
		string text = Convert.ToString(value, CultureInfo.InvariantCulture);
		var dict = ParseAboveFeatures(text);
		if (dict is null) return new List<Feature>();

		using var maskClass = maskFeature.GetTable();
		using var maskStore = maskClass.GetDatastore();
		if (maskStore is not Geodatabase gdb) return new List<Feature>();

		var result = new List<Feature>();

		foreach ((string name, List<long> list) in dict)
		{
			if (string.IsNullOrEmpty(name)) continue;
			if (list is null || list.Count < 1) continue;
			using var aboveClass = gdb.OpenDataset<FeatureClass>(name);
			var aboveFeatures = GdbQueryUtils.GetFeatures(aboveClass, list, null, recycle: false);
			result.AddRange(aboveFeatures);
		}

		return result;
	}

	public static string FormatAboveFeatures(IDictionary<string, List<long>> dict)
	{
		if (dict is null) return null; // null stays null

		if (dict.Count < 1) return string.Empty;

		var sb = new StringBuilder();
		foreach ((string name, List<long> list) in dict.OrderBy(pair => pair.Key))
		{
			if (string.IsNullOrEmpty(name)) continue;
			if (list is null || list.Count < 1) continue;
			if (sb.Length > 0) sb.Append(';');
			ValidateName(name);
			sb.Append(name);
			foreach (var oid in list.OrderBy(x => x))
			{
				sb.Append(',');
				sb.Append(Convert.ToString(oid, CultureInfo.InvariantCulture));
			}
		}

		return sb.ToString();
	}

	public static Dictionary<string, List<long>> ParseAboveFeatures(string text)
	{
		if (text is null) return null; // null stays null

		var result = new Dictionary<string, List<long>>(StringComparer.OrdinalIgnoreCase);

		// do:
		//   skip white
		//   if eos: stop
		//   scan name
		//   skip white
		//   while scan ",":
		//     skip white
		//     scan oid
		//     skip white
		// while scan ";"
		// skip white
		// expect eos

		int index = 0;

		do
		{
			SkipWhite(text, ref index);
			if (index >= text.Length) break;
			if (!ScanName(text, ref index, out var name))
				throw new FormatException($"Expect name at position {index}");
			SkipWhite(text, ref index);
			var oids = new List<long>();
			while (ScanChar(text, ref index, ','))
			{
				SkipWhite(text, ref index);
				if (!ScanOID(text, ref index, out long oid))
					throw new FormatException($"Expect OID at position {index}");
				oids.Add(oid);
				SkipWhite(text, ref index);
			}

			if (name is not null && oids.Count > 0)
			{
				if (result.TryGetValue(name, out var list))
				{
					list.AddRange(oids);
					list.Sort();
				}
				else
				{
					oids.Sort();
					result.Add(name, oids);
				}
			}
		} while (ScanChar(text, ref index, ';'));

		SkipWhite(text, ref index);
		if (index < text.Length)
			throw new FormatException("Unexpected input after end");

		return result;
	}

	private static void SkipWhite(string text, ref int index)
	{
		while (index < text.Length && char.IsWhiteSpace(text, index))
		{
			index += 1;
		}
	}

	private static bool ScanChar(string text, ref int index, char expect)
	{
		if (index >= text.Length) return false;
		if (text[index] != expect) return false;
		index += 1;
		return true;
	}

	private static bool ScanOID(string text, ref int index, out long oid)
	{
		oid = 0;
		if (index >= text.Length) return false;
		if (!char.IsAsciiDigit(text[index])) return false;

		char cc;
		long value = 0;
		while (index < text.Length && char.IsAsciiDigit(cc = text[index]))
		{
			index += 1;
			value *= 10;
			value += cc - '0';
		}

		oid = value;
		return true;
	}

	private static bool ScanName(string text, ref int index, out string name)
	{
		// first char must be in [a-zA-Z_]
		// following chars must be in [a-zA-Z_0-9]
		// TODO allow qualified names?
		// TODO allow quoted names?

		name = null;
		if (index >= text.Length) return false;
		if (!char.IsAsciiLetter(text[index])) return false;

		int start = index;
		while (index < text.Length && (char.IsAsciiLetterOrDigit(text[index]) || text[index] == '_'))
		{
			index += 1;
		}

		name = text.Substring(start, index - start);
		return true;
	}

	private static void ValidateName(string name)
	{
		if (name is null) return;
		if (name.Length < 1)
			throw new FormatException("Invalid name: must not be empty");
		if (! char.IsAsciiLetter(name[0]) && name[0] != '_')
			throw new FormatException($"Invalid name: “{name}” (must start with an ASCII letter or an underscore)");
		for (int i = 1; i < name.Length; i++)
		{
			if (!char.IsAsciiLetterOrDigit(name[i]) && name[i] != '_')
				throw new FormatException($"Invalid name: “{name}” (must contain only ASCII letters or digits or underscores)");
		}
	}
}
