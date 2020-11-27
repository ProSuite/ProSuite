using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Processing
{
	public class CartoProcessConfig
	{
		private readonly IList<KeyValuePair<string, object>> _settings;

		public CartoProcessConfig(string name, string description = null)
		{
			Name = name ?? string.Empty;
			Description = description ?? string.Empty;

			_settings = new List<KeyValuePair<string, object>>(); // todo keep triples (name, value, lineno) for better error reporting
		}

		public static CartoProcessConfig FromString(string name, string text, string description = null)
		{
			var config = new CartoProcessConfig(name, description);
			config.LoadString(text);
			return config;
		}

		public string Name { get; }

		public string Description { get; }

		public IEnumerable<T> GetValues<T>(string parameterName)
		{
			var converter = TypeDescriptor.GetConverter(typeof(T));

			foreach (var pair in _settings)
			{
				if (string.Equals(pair.Key, parameterName, StringComparison.Ordinal))
				{
					yield return ConvertValue<T>(converter, pair.Value, parameterName);
				}
			}
		}

		public T GetOptionalValue<T>(string parameterName)
		{
			var values = GetValues<T>(parameterName).ToArray();
			if (values.Length < 1)
				return default;
			if (values.Length > 1)
				throw ConfigError("Parameter {0} is defined more than once", parameterName);
			return values[0];
		}

		public T GetRequiredValue<T>(string parameterName)
		{
			var values = GetValues<T>(parameterName).ToArray();
			if (values.Length < 1)
				throw ConfigError("Required parameter {0} is missing", parameterName);
			if (values.Length > 1)
				throw ConfigError("Parameter {0} is defined more than once", parameterName);
			return values[0];
		}

		private static T ConvertValue<T>(TypeConverter converter, object value, string name)
		{
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

		public void LoadString(string text)
		{
			_settings.Clear();

			if (text == null) return;

			var position = new Position();

			SkipWhite(text, position);

			while (position.Index < text.Length)
			{
				string name = ScanName(text, position);
				if (string.IsNullOrEmpty(name))
					throw SyntaxError(position, "Expect parameter name");

				SkipBlank(text, position);
				if (ScanOperator(text, position, ':', '=') == (char) 0)
					throw SyntaxError(position, "Expect '=' operator");
				SkipBlank(text, position);

				string value = ScanValue(text, position);
				if (value == null)
					throw SyntaxError(position, "Expect a value");
				_settings.Add(new KeyValuePair<string, object>(name, value));

				SkipWhite(text, position);
				SkipOperator(text, position, ';', '\n');
				SkipWhite(text, position);
			}
		}

		private static string ScanValue(string text, Position position)
		{
			if (position.Index >= text.Length)
				return null;

			char c = text[position.Index];
			if (c == '\'')
				return ScanSqlString(text, position);

			if (c == '"')
				return ScanString(text, position);

			return ScanToDelim(text, position, ';', '\n');
		}

		private static string ScanToDelim(string text, Position position, char d1, char d2)
		{
			char c;
			int anchor = position.Index;
			while (position.Index < text.Length && (c = text[position.Index]) != d1 && c != d2)
			{
				position.Advance(text);
			}

			return text.Substring(anchor, position.Index - anchor).Trim();
		}

		private static string ScanSqlString(string text, Position position)
		{
			Assert.True(position.Index < text.Length, "Bug");

			var sb = new StringBuilder();
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
						sb.Append(quote); // un-escape
						position.Advance(text); // skip 2nd apostrophe
					}
					else
					{
						return sb.ToString();
					}
				}
				else
				{
					sb.Append(cc);
				}
			}

			throw SyntaxError(position, "Unterminated string starting at position {0}", anchor);
		}

		private static string ScanString(string text, Position position)
		{
			Assert.True(position.Index < text.Length, "Bug");

			var sb = new StringBuilder();
			char quote = text[position.Index];
			int anchor = position.Index;
			position.Advance(text); // skip opening quote

			while (position.Index < text.Length)
			{
				char cc = text[position.Index];
				position.Advance(text);

				if (cc < ' ')
				{
					throw SyntaxError(position, "Control character in string");
				}

				if (cc == quote)
				{
					return sb.ToString();
				}

				if (cc == '\\')
				{
					if (position.Index >= text.Length)
					{
						break;
					}

					cc = text[position.Index];
					position.Advance(text);

					switch (cc)
					{
						case '"':
						case '\'':
						case '\\':
						case '/':
							sb.Append(cc);
							break;
						case 'b':
							sb.Append('\b');
							break;
						case 'f':
							sb.Append('\f');
							break;
						case 'n':
							sb.Append('\n');
							break;
						case 'r':
							sb.Append('\r');
							break;
						case 't':
							sb.Append('\t');
							break;
						case 'u':
							throw SyntaxError(position, "\\u#### is not yet implemented");
						default:
							throw SyntaxError(position, "Invalid escape '\\{0}' in string", cc);
					}
				}
				else
				{
					sb.Append(cc);
				}
			}

			throw SyntaxError(position, "Unterminated string starting at position {0}", anchor);
		}

		private static string ScanName(string text, Position position)
		{
			char cc;
			if (position.Index >= text.Length || (cc = text[position.Index]) != '_' && !char.IsLetter(cc))
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

		private static char ScanOperator(string text, Position position, char op1, char op2)
		{
			if (position.Index >= text.Length) return (char) 0;
			char c = text[position.Index];
			if (c != op1 && c != op2) return (char) 0;
			position.Advance(text);
			return c;
		}

		private static void SkipOperator(string text, Position position, char op1, char op2)
		{
			if (position.Index >= text.Length) return;
			char c = text[position.Index];
			if (c != op1 && c != op2) return;
			position.Advance(text);
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

		[StringFormatMethod("format")]
		private static FormatException SyntaxError(Position position, string format, params object[] args)
		{
			return new FormatException(string.Format(format, args) +
			                           $" (line {position.LineNumber} position {position.LinePosition}");
		}

		[StringFormatMethod("format")]
		private static Exception ConfigError(string format, params object[] args)
		{
			return new Exception(string.Format(format, args));
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
				if (text[Index] == '\n')
				{
					LinePosition = 0;
					LineNumber += 1;
				}

				LinePosition += 1;
				Index += 1;
			}
		}
	}
}
