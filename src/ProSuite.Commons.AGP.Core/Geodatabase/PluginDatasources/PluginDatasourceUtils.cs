using System;
using System.Collections.Generic;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.Commons.AGP.Core.Geodatabase.PluginDatasources;

public static class PluginDatasourceUtils
{
	// A plugin datasource is identified by (1) the plugin
	// datasource's DAML ID and (2) the "connectionPath".
	// These two items are stored in CIM as a string of the
	// form "DATABASE=connectionPath;IDENTIFIER=damlID" with
	// no escaping and no encoding; moreover, Pro prepends
	// the project path if the connectionPath does not look
	// absolute (both confirmed by Esri Inc. on 2025-01-08).
	//
	// Consequently, a connectionPath and a pluginDamlID must
	// not contain '=' nor ';', and we must either strip the
	// project path that Pro prepends to the connectionPath,
	// or make connectionPath look absolute (Uri.IsAbsoluteUri
	// is not sufficient, the local path must be absolute).
	// Esri suggested we use a UNC path.
	//
	// Unsure about ';' and '=' signs in UNC path. Empirically
	// they work fine through save-exit-restart-load cycles.
	// However, in the repro case for Esri Inc. we found a
	// problem with an '=' sign in a path -- TODO investigate
	// and TODO a ';' still spoils it across a save-load cycle!

	public static Uri MakeConnectionPath(string marker, string pseudoPath)
	{
		if (string.IsNullOrWhiteSpace(marker))
			throw new ArgumentNullException(nameof(marker));
		if (string.IsNullOrEmpty(pseudoPath))
			throw new ArgumentNullException(nameof(pseudoPath));

		// TODO encode pseudoPath if it contains ';' and/or '=' but how?
		// URL encoding fails because it is somehow done by Pro or Uri class?
		// What else? Something like "{3B}" for ';'?

		return new Uri($"\\\\{marker}\\{pseudoPath}");
	}

	public static bool TryParseConnectionPath(Uri connectionPath, string marker, out string pseudoPath)
	{
		if (connectionPath is null)
			throw new ArgumentNullException(nameof(connectionPath));
		if (string.IsNullOrWhiteSpace(marker))
			throw new ArgumentNullException(nameof(marker));

		if (! string.Equals(connectionPath.Host, marker, StringComparison.OrdinalIgnoreCase))
		{
			pseudoPath = null;
			return false;
		}

		pseudoPath = connectionPath.PathAndQuery;

		if (pseudoPath is not null && pseudoPath.Length > 0 && pseudoPath[0] is '/' or '\\')
		{
			// strip leading / or \ in absolute path:
			pseudoPath = pseudoPath.Substring(1);
		}

		return true;
	}

	#region Quasi URL encoding

	// Reserved URL chars:   : / ? # [ ] @ ! $ % & ' ( ) * + , ; =
	// Available URL chars:  A-Z, a-z, 0-9, - . _ ~
	// Here we must encode '=' and ';', a more complete list would be:
	//   SP ! " # $ % & ' ( ) * + , - . / : ; < = > ? @ [ \ ] { | }
	// There's no need to encoded non-ASCII chars, as they are not
	// meta characters in XML or URLs or the foo=bar;baz=quux formats.
	// Our choice of characters to encode:

	private static readonly HashSet<char> CharsToEncode =
	[
		' ', '!', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',',
		':', ';', '<', '=', '>', '?', '@', '[', '\\', ']', '{', '|', '}'
	];

	private const char EscapeChar = '%';

	private static string ConnectionPathEncode(string text)
	{
		if (text is null) return null;

		if (! CharsToEncode.Contains(EscapeChar))
		{
			Assert.Fail($"Our escape char '{EscapeChar}' is not in {nameof(CharsToEncode)}");
		}

		const string hex = "0123456789ABCDEF";

		var sb = new StringBuilder();
		foreach (var c in text)
		{
			if (c < 128 && CharsToEncode.Contains(c))
			{
				sb.Append(EscapeChar);
				sb.Append(hex[(c >> 4) & 15]); // hi 4 bits
				sb.Append(hex[c & 15]); // lo 4 bits
			}
			else
			{
				sb.Append(c);
			}
		}

		return sb.ToString();
	}

	private static string ConnectionPathDecode(string text)
	{
		if (text is null) return null;

		var sb = new StringBuilder();

		for (int i = 0; i < text.Length; i++)
		{
			var c = text[i];
			int lo, hi;
			if (c == EscapeChar && i < text.Length - 2 &&
			    (hi = HexValue(text[i + 1])) >= 0 &&
			    (lo = HexValue(text[i + 2])) >= 0)
			{
				sb.Append((char)(hi * 16 + lo));
				i += 2;
			}
			else
			{
				sb.Append(c);
			}
		}

		return sb.ToString();
	}

	private static int HexValue(char hex)
	{
		if ('0' <= hex && hex <= '9') return hex - '0';
		if ('a' <= hex && hex <= 'f') return 10 + hex - 'a';
		if ('A' <= hex && hex <= 'F') return 10 + hex - 'A';
		return -1;
	}

	#endregion
}
