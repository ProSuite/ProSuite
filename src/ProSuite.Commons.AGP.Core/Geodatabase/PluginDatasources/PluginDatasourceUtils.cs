using System;
using System.Text;

namespace ProSuite.Commons.AGP.Core.Geodatabase.PluginDatasources;

public static class PluginDatasourceUtils
{
	/// <summary>
	/// Create a connection path Uri from the given <paramref name="pseudoHost"/>
	/// (a marker like "dangles" or "wireframe") and the given
	/// <paramref name="pseudoPath"/> (any string). The result is
	/// such that the given <paramref name="pseudoPath"/> "survives"
	/// being stored to and loaded from a project/map/layer file.
	/// </summary>
	/// <remarks>
	/// A plugin datasource is identified by the plugin datasource's
	/// DAML ID and the "connectionPath". These two items are stored
	/// in CIM in the form "DATABASE=connectionPath;IDENTIFIER=damlID"
	/// with no escaping and no encoding; moreover, Pro prepends the
	/// project path if the connectionPath does not look absolute
	/// (both confirmed by Esri Inc. on 2025-01-08).
	/// <para/>
	/// Consequently, a connectionPath and a pluginDamlID must
	/// not contain '=' nor ';', and we must either strip the
	/// project path that Pro prepends to the connectionPath,
	/// or make connectionPath look absolute (Uri.IsAbsoluteUri
	/// is not sufficient, the local path must be absolute).
	/// Esri suggested we use a UNC path, which seems to work.
	/// But we still have to encode '=' and ';' (see below).
	/// </remarks>
	public static Uri MakeConnectionPath(string pseudoHost, string pseudoPath)
	{
		if (string.IsNullOrWhiteSpace(pseudoHost))
			throw new ArgumentNullException(nameof(pseudoHost));
		if (string.IsNullOrEmpty(pseudoPath))
			throw new ArgumentNullException(nameof(pseudoPath));

		var encoded = PseudoPathEncode(pseudoPath);

		return new Uri($"\\\\{pseudoHost}\\{encoded}");
	}

	/// <summary>See <see cref="MakeConnectionPath"/></summary>
	public static bool TryParseConnectionPath(Uri connectionPath, string pseudoHost, out string pseudoPath)
	{
		if (connectionPath is null)
			throw new ArgumentNullException(nameof(connectionPath));
		if (string.IsNullOrWhiteSpace(pseudoHost))
			throw new ArgumentNullException(nameof(pseudoHost));

		if (! string.Equals(connectionPath.Host, pseudoHost, StringComparison.OrdinalIgnoreCase))
		{
			pseudoPath = null;
			return false;
		}

		// Don't use Uri's OriginalString (we don't control it);
		// use AbsolutePath or PathAndQuery or LocalPath, and
		// beware that they may have been %-encoded by Uri class.

		pseudoPath = connectionPath.PathAndQuery;

		if (pseudoPath is not null && pseudoPath.Length > 0 && pseudoPath[0] is '/' or '\\')
		{
			// strip leading '/' or '\' in absolute path:
			pseudoPath = pseudoPath.Substring(1);
		}

		pseudoPath = PseudoPathDecode(pseudoPath);

		return true;
	}

	#region Pseudo URL encoding

	// Reserved URL chars:   : / ? # [ ] @ ! $ % & ' ( ) * + , ; =
	// Available URL chars:  A-Z, a-z, 0-9, - . _ ~
	// Unsafe URL chars:     { } | \ ^ ~ [ ] `   (by RFC 1738)
	//
	// We have to encode '=' and ';' because of the way ArcGIS Pro stores
	// connection info in the CIM and the layer/map files. Encode them as
	// ~3B and ~3D, because the tilde is safe enough for us and not touched
	// by the Uri class, and the scheme is extensible (URL encoding with
	// ~ instead of % and only for our two problem characters).
	// Do NOT URL-encode, because this is done by the Uri class (and maybe
	// Pro itself) and in the end we don't know how many times to decode!

	private const char EscapeChar = '~';

	private static string PseudoPathEncode(string text)
	{
		if (text is null) return null;

		const string hex = "0123456789ABCDEF";

		var sb = new StringBuilder();

		foreach (var c in text)
		{
			if (c == '=' || c == ';')
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

	private static string PseudoPathDecode(string text)
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
