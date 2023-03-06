using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.Processing.Utils;

namespace ProSuite.Processing
{
	public class CartoProcessConfig : IEnumerable<KeyValuePair<string,string>>
	{
		private const StringComparison Comparison = StringComparison.OrdinalIgnoreCase;

		private readonly IList<Setting> _settings;

		private CartoProcessConfig([NotNull] IList<Setting> settings,
		                           string name, string typeAlias, string description)
		{
			_settings = settings ?? throw new ArgumentNullException(nameof(settings));

			Name = name;
			TypeAlias = typeAlias;
			Description = description;
		}

		public static CartoProcessConfig Parse(string text, bool lenient = false)
		{
			// 1. try parse as XML
			// 2. assume new plain text format

			if (text is null) return null;

			try
			{
				var element = XElement.Parse(text);

				if (element.Name == "Process")
				{
					return FromProcess(element);
				}

				if (element.Name == "ProcessGroup")
				{
					return FromProcessGroup(element);
				}

				throw new CartoConfigException(
					$"Invalid element: {element.Name} (expect Process or ProcessGroup)");
			}
			catch (XmlException)
			{
				return FromText(text, lenient);
			}
		}

		public string Name { get; private set; }

		public string TypeAlias { get; private set; }

		public string Description { get; private set; }

		public int Count => _settings.Count;

		public IEnumerable<string> GetAllNames()
		{
			// In original order, but with duplicates removed:
			return _settings.Select(s => s.Name).Distinct();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
		{
			return _settings.Select(s => new KeyValuePair<string, string>(s.Name, s.Value))
			                .GetEnumerator();
		}

		public IEnumerable<string> GetValues(string parameterName)
		{
			foreach (var setting in _settings)
			{
				if (string.Equals(setting.Name, parameterName, Comparison))
				{
					yield return setting.Value;
				}
			}
		}

		public IEnumerable<T> GetValues<T>(string parameterName)
		{
			var converter = TypeDescriptor.GetConverter(typeof(T));

			foreach (var setting in _settings)
			{
				if (string.Equals(setting.Name, parameterName, Comparison))
				{
					yield return ConvertValue<T>(converter, setting.Value, parameterName);
				}
			}
		}

		/// <summary>
		/// Get the value of the given <paramref name="parameterName"/>
		/// converted to type <typeparamref name="T"/>, or the given
		/// <paramref name="defaultValue"/> if no such parameter in config.
		/// </summary>
		public T GetValue<T>(string parameterName, T defaultValue)
		{
			var values = GetValues(parameterName).ToArray();
			if (values.Length < 1)
				return defaultValue;
			if (values.Length > 1)
				throw ConfigError("Parameter {0} is defined more than once", parameterName);
			var value = values[0];
			if (string.IsNullOrWhiteSpace(value))
				return defaultValue;
			var converter = TypeDescriptor.GetConverter(typeof(T));
			return ConvertValue<T>(converter, value, parameterName);
		}

		/// <summary>
		/// Get the value of the given <paramref name="parameterName"/>
		/// converted to type <typeparamref name="T"/>, or throw an
		/// exception if no such parameter in config.
		/// </summary>
		public T GetValue<T>(string parameterName)
		{
			var values = GetValues(parameterName).ToArray();
			if (values.Length < 1)
				throw ConfigError("Required parameter {0} is missing", parameterName);
			if (values.Length > 1)
				throw ConfigError("Parameter {0} is defined more than once", parameterName);
			var value = values[0];
			if (string.IsNullOrWhiteSpace(value))
				throw ConfigError("Required parameter {0} is missing", parameterName);
			var converter = TypeDescriptor.GetConverter(typeof(T));
			return ConvertValue<T>(converter, value, parameterName);
		}

		// Difference between GetValue<string>() and GetString() is that
		// the latter treats an empty value as the empty string, whereas
		// the former treats an empty value as missing.

		public string GetString(string parameterName, string defaultValue)
		{
			var values = GetValues(parameterName).ToArray();
			if (values.Length < 1)
				return defaultValue;
			if (values.Length > 1)
				throw ConfigError("Parameter {0} is defined more than once", parameterName);
			return values[0]?.Trim() ?? defaultValue;
		}

		public string GetString(string parameterName)
		{
			var values = GetValues(parameterName).ToArray();
			if (values.Length < 1)
				throw ConfigError("Required parameter {0} is missing", parameterName);
			if (values.Length > 1)
				throw ConfigError("Parameter {0} is defined more than once", parameterName);
			return values[0]?.Trim();
		}

		/// <summary>
		/// All parameter values of the given <paramref name="parameterName"/>
		/// joined into one string; may be empty if no such parameter exists.
		/// </summary>
		public string GetJoined(string parameterName, string separator)
		{
			return string.Join(separator ?? string.Empty, GetValues(parameterName));
		}

		private static T ConvertValue<T>(TypeConverter converter, string value, string name)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return default;
			}

			try
			{
				return (T) converter.ConvertFrom(value);
			}
			catch (Exception ex)
			{
				throw ConfigError("Cannot convert {0} to type {1}: {2}",
				                  name, typeof(T).Name, ex.Message);
			}
		}

		[StringFormatMethod("format")]
		private static Exception ConfigError(string format, params object[] args)
		{
			return new CartoConfigException(string.Format(format, args));
		}

		#region Formatting

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.Append(nameof(Name)).Append(" = ");
			sb.Append(Name ?? string.Empty).AppendLine();

			sb.Append(nameof(TypeAlias)).Append(" = ");
			sb.Append(TypeAlias ?? string.Empty).AppendLine();

			if (! string.IsNullOrEmpty(Description))
			{
				sb.Append(nameof(Description)).Append(" = ");
				sb.Append(Description).AppendLine();
			}

			sb.AppendLine();

			foreach (var setting in _settings)
			{
				if (string.IsNullOrWhiteSpace(setting.Name)) continue;
				if (string.Equals(setting.Name, nameof(Name), Comparison)) continue;
				if (string.Equals(setting.Name, nameof(TypeAlias), Comparison)) continue;
				if (string.Equals(setting.Name, nameof(Description), Comparison)) continue;

				sb.Append(setting.Name ?? "NN");
				sb.Append(" = ");
				sb.Append(setting.Value ?? string.Empty);
				sb.AppendLine();
			}

			sb.AppendLine();
			return sb.ToString();
		}

		#endregion

		#region XML Element Parsing

		/// <summary>
		/// Parse config from given Process element of old .cp.xml
		/// </summary>
		private static CartoProcessConfig FromProcess(XElement process)
		{
			if (process is null) return null;

			var processName = (string) process.Attribute("name");
			var description = (string) process.Element("Description") ??
			                  (string) process.Attribute("description");

			var typeElement = process.Element("TypeReference");
			var typeAlias = (string) typeElement?.Attribute("name");

			var parameters = process.Element("Parameters")?.Elements("Parameter");

			var nameRegex = new Regex(@"^[_a-z][_a-z0-9]*$", RegexOptions.IgnoreCase);
			var multiRegex = new Regex(@"\d+$"); // eager: matches "123" in "foo123" (not only "23" or "3")

			var settings = new List<Setting>();

			foreach (var e in parameters ?? Enumerable.Empty<XElement>())
			{
				var name = (string) e.Attribute("name");
				var value = (string) e.Attribute("value");

				if (string.IsNullOrEmpty(name)) continue;

				if (! nameRegex.IsMatch(name))
				{
					throw new CartoConfigException(
						$"Invalid parameter name: {name} (must match /{nameRegex}/)");
				}

				var match = multiRegex.Match(name);
				if (match.Success && match.Index > 0)
				{
					// old config did not allow multi-valued parameters; instead we had Foo1, Foo2, etc.
					// translate to new config, which allows repeated parameters; use plural name by convention
					var stem = name.Substring(0, match.Index); // strip trailing digits
					name = stem.EndsWith("s") ? stem : string.Concat(stem, "s");
				}

				settings.Add(new Setting(name, value, e.GetLineNumber()));
			}

			return new CartoProcessConfig(settings, processName, typeAlias, description);
		}

		/// <summary>
		/// Parse config from given ProcessGroup element of old .cp.xml
		/// </summary>
		private static CartoProcessConfig FromProcessGroup(XElement processGroup)
		{
			if (processGroup is null) return null;

			var processName = (string) processGroup.Attribute("name");
			var description = (string) processGroup.Element("Description") ??
			                  (string) processGroup.Attribute("description");

			var typeElement = processGroup.Element("AssociatedGroupProcessTypeReference") ??
			                  processGroup.Element("GroupProcessTypeReference") ??
			                  processGroup.Element("TypeReference");
			var typeAlias = (string)typeElement?.Attribute("name");

			var processes = processGroup.Element("Processes")?.Elements("Process");

			var settings = new List<Setting>();

			foreach (var e in processes ?? Enumerable.Empty<XElement>())
			{
				const string name = "Processes";
				var value = (string) e.Attribute("name");
				if (string.IsNullOrEmpty(value)) continue;

				settings.Add(new Setting(name, value, e.GetLineNumber()));
			}

			return new CartoProcessConfig(settings, processName, typeAlias, description);
		}

		#endregion

		#region Config Text Parsing

		/// <summary>
		/// Parse config from given simple text format: name = value {; name = value}
		/// </summary>
		/// <param name="text">Config text to be parsed</param>
		/// <param name="lenient">Iff true, ignore syntax errors parse as much as possible</param>
		private static CartoProcessConfig FromText(string text, bool lenient = false)
		{
			var settings = new List<Setting>();
			LoadPairs(settings, text, lenient);

			var name = GetString(settings, nameof(Name));
			var typeAlias = GetString(settings, nameof(TypeAlias));
			var description = GetString(settings, nameof(Description));

			return new CartoProcessConfig(settings, name, typeAlias, description);
		}

		private static string GetString(IEnumerable<Setting> settings, string parameterName)
		{
			var values = settings.Where(s => string.Equals(s.Name, parameterName, Comparison))
			                     .Select(s => s.Value).ToArray();
			if (values.Length < 1)
				return null;
			if (values.Length > 1)
				throw ConfigError("Parameter {0} is defined more than once", parameterName);
			return values[0]?.Trim();
		}

		private static void LoadPairs(ICollection<Setting> result, string text, bool lenient = false)
		{
			if (text == null) return;

			var position = new Position();
			var sb = new StringBuilder();

			SkipWhite(text, position);

			while (position.Index < text.Length)
			{
				if (text[position.Index] == '#')
				{
					SkipLine(text, position);
					SkipWhite(text, position);
					continue;
				}

				try
				{
					string name = ScanName(text, position);
					if (string.IsNullOrEmpty(name))
						throw SyntaxError(position, "Expect parameter name");

					SkipBlank(text, position);
					if (ScanOperator(text, position, ':', '=') == (char) 0)
						throw SyntaxError(position, "Expect '=' operator");
					SkipBlank(text, position);

					string value = ScanValue(text, position, sb);
					if (value == null)
						throw SyntaxError(position, "Expect a value");
					result.Add(new Setting(name, value, position.LineNumber));

					SkipWhite(text, position);
				}
				catch (FormatException)
				{
					if (! lenient) throw;

					// Skip faulty line and go on:
					SkipLine(text, position);
					SkipWhite(text, position);
				}
			}
		}

		/// <summary>Sloppy line parse, may be used for "intellisense"</summary>
		/// <returns>index relative to start of line</returns>
		public static int ParseLine(string text, int textIndex, out string line,
		                            out int nameStart, out int nameLength, out int valueStart)
		{
			line = GetLineAtIndex(text, textIndex, out int lineIndex);

			nameLength = 0;
			valueStart = -1;

			var position = new Position();
			SkipWhite(line, position);
			nameStart = position.Index;

			var name = ScanName(line, position);
			if (name is null) return lineIndex;
			nameLength = name.Length; // or: position.Index - nameStart

			SkipBlank(line, position);
			var op = ScanOperator(line, position, ':', '=');
			if (op == (char) 0) return lineIndex;
			SkipBlank(line, position);

			valueStart = position.Index;
			return lineIndex;
		}

		private static string GetLineAtIndex(string text, int textIndex, out int lineIndex)
		{
			if (textIndex > text.Length) textIndex = text.Length;
			else if (textIndex < 0) textIndex = 0;
			int i = textIndex;
			while (i > 0 && text[i - 1] != '\n' && text[i - 1] != '\r') i--;
			lineIndex = textIndex - i;
			int j = textIndex;
			while (j < text.Length && text[j] != '\n' && text[j] != '\r') j++;
			return text.Substring(i, j - i);
		}


		private static string ScanName(string text, Position position)
		{
			if (position.Index >= text.Length)
			{
				return null; // at end of text
			}

			char cc;
			if ((cc = text[position.Index]) != '_' && !char.IsLetter(cc))
			{
				return null; // not a name at text[index...]
			}

			int anchor = position.Index;
			while (position.Index < text.Length && ((cc = text[position.Index]) == '_' || char.IsLetterOrDigit(cc)))
			{
				position.Advance(text);
			}

			return text.Substring(anchor, position.Index - anchor);
		}

		private static string ScanValue(string text, Position position, StringBuilder sb)
		{
			sb.Clear();

			if (position.Index >= text.Length)
			{
				return null; // at end of text
			}

			while (position.Index < text.Length)
			{
				char cc = text[position.Index];
				if (cc == '\'')
				{
					ScanSqlString(text, position, sb); // NB: SQL '' string <> Shell '' string
				}
				else if (cc == '"')
				{
					ScanString(text, position, sb);
				}
				else if (cc == '#')
				{
					SkipLine(text, position);
					break;
				}
				else if (cc == '\r' || cc == '\n')
				{
					ScanEndOfLine(text, position);
					break;
				}
				else if (cc == ' ' || cc == '\t' || cc == '\v')
				{
					SkipBlank(text, position);
					sb.Append(' ');
				}
				else
				{
					sb.Append(cc);
					position.Advance(text);
				}
			}

			return sb.TrimEnd().ToString();
		}

		private static void ScanSqlString(string text, Position position, StringBuilder sb)
		{
			Assert.True(position.Index < text.Length, "Bug");

			char quote = text[position.Index];
			int anchor = position.Index;
			position.Advance(text); // skip opening apostrophe

			while (position.Index < text.Length)
			{
				char cc = text[position.Index];
				position.Advance(text);

				if (cc == quote)
				{
					if (position.Index < text.Length && text[position.Index] == quote)
					{
						position.Advance(text); // skip 2nd apostrophe
					}
					else
					{
						sb.Append(text.Substring(anchor, position.Index - anchor));
						return;
					}
				}
			}

			throw SyntaxError(position, "Unterminated string starting at position {0}", anchor);
		}

		private static void ScanString(string text, Position position, StringBuilder sb)
		{
			Assert.True(position.Index < text.Length, "Bug");

			char quote = text[position.Index];
			int anchor = position.Index;
			position.Advance(text); // skip opening quote

			while (position.Index < text.Length)
			{
				char cc = text[position.Index];
				position.Advance(text);

				if (cc == quote)
				{
					sb.Append(text.Substring(anchor, position.Index - anchor));
					return;
				}

				if (cc == '\\')
				{
					if (position.Index >= text.Length)
					{
						break;
					}

					position.Advance(text);

					// here just accept any escaped character
					// typically allowed: " ' \ / b f n r t v u####
				}
			}

			throw SyntaxError(position, "Unterminated string starting at position {0}", anchor);
		}

		private static char ScanOperator(string text, Position position, char op1, char op2)
		{
			if (position.Index >= text.Length) return (char) 0;
			char c = text[position.Index];
			if (c != op1 && c != op2) return (char) 0;
			position.Advance(text);
			return c;
		}

		private static void SkipLine(string text, Position position)
		{
			while (position.Index < text.Length)
			{
				char cc = text[position.Index];
				position.Advance(text);
				if (cc == '\n' || cc == '\r') break;
			}
		}

		private static void SkipWhite(string text, Position position)
		{
			while (position.Index < text.Length && char.IsWhiteSpace(text, position.Index))
			{
				position.Advance(text);
			}
		}

		private static void SkipBlank(string text, Position position)
		{
			const char blank = ' ';
			const char tab = '\t';

			char cc;
			while (position.Index < text.Length && ((cc = text[position.Index]) == blank || cc == tab))
			{
				position.Advance(text);
			}
		}

		private static void ScanEndOfLine(string text, Position position)
		{
			// expect one of: end-of-text | CR | CR LF | LF
			if (position.Index >= text.Length) return;
			bool found = text[position.Index] == '\n' || text[position.Index] == '\r';
			if (found) position.Advance(text);
		}

		[StringFormatMethod("format")]
		private static FormatException SyntaxError(Position position, string format, params object[] args)
		{
			return new FormatException(string.Format(format, args) +
			                           $" (line {position.LineNumber} position {position.LinePosition}");
		}

		#endregion

		#region Nested types

		private readonly struct Setting
		{
			public string Name { get; }
			public string Value { get; }
			private int LineNumber { get; }

			public Setting(string name, string value, int lineNumber)
			{
				Name = name ?? throw new ArgumentNullException(nameof(name));
				Value = value;
				LineNumber = lineNumber;
			}

			public override string ToString()
			{
				return $"{Name} = {Value} (line {LineNumber})";
			}
		}

		private class Position
		{
			public int Index { get; private set; }
			public int LineNumber { get; private set; }
			public int LinePosition { get; private set; }

			public Position()
			{
				Index = 0;
				LineNumber = 1;
				LinePosition = 1;
			}

			public void Advance(string text)
			{
				if (Index < text.Length)
				{
					// Cope with all end-of-line conventions: CR, CR LF, LF

					if (text[Index] == '\r')
					{
						int next = Index + 1;
						if (next < text.Length && text[next] == '\n')
						{
							Index += 1;
						}

						LinePosition = 0;
						LineNumber += 1;
					}
					else if (text[Index] == '\n')
					{
						LinePosition = 0;
						LineNumber += 1;
					}

					LinePosition += 1;
					Index += 1;
				}
			}
		}

		#endregion
	}
}
