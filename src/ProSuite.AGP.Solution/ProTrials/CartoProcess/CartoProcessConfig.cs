using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.ProTrials.CartoProcess
{
	public class CartoProcessConfig
	{
		private readonly IList<KeyValuePair<string, object>> _settings;

		public CartoProcessConfig(string name, string description = null)
		{
			Name = name ?? string.Empty;
			Description = description ?? string.Empty;

			_settings = new List<KeyValuePair<string, object>>();
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
					yield return (T) converter.ConvertFrom(pair.Value);
				}
			}
		}

		public T GetValue<T>(string parameterName)
		{
			return GetValues<T>(parameterName).SingleOrDefault();
		}

		public void LoadString(string text)
		{
			_settings.Clear();

			if (text == null) return;

			int index = 0;

			SkipWhite(text, ref index);

			while (index < text.Length)
			{
				string name = ScanName(text, ref index);
				if (string.IsNullOrEmpty(name))
					throw SyntaxError("Expect parameter name (position {0})", index);

				SkipWhite(text, ref index);
				if (ScanOperator(text, ref index, ':', '=') == (char) 0)
					throw SyntaxError("Expect '=' operator (position {0})", index);
				SkipWhite(text, ref index);

				string value = ScanValue(text, ref index);
				if (value == null)
					throw SyntaxError("Expect value (position {0})", index);
				_settings.Add(new KeyValuePair<string, object>(name, value));

				SkipWhite(text, ref index);
				SkipOperator(text, ref index, ';', '\n');
				SkipWhite(text, ref index);
			}
		}

		private static string ScanValue(string text, ref int index)
		{
			if (index >= text.Length)
				return null;

			char c = text[index];
			if (c == '\'')
				return ScanSqlString(text, ref index);

			if (c == '"')
				return ScanString(text, ref index);

			return ScanToDelim(text, ref index, ';', '\n');
		}

		private static string ScanToDelim(string text, ref int index, char d1, char d2)
		{
			char c;
			int anchor = index;
			while (index < text.Length && (c = text[index]) != d1 && c != d2)
			{
				index += 1;
			}

			return text.Substring(anchor, index - anchor).Trim();
		}

		private static string ScanSqlString(string text, ref int index)
		{
			Assert.True(index < text.Length, "Bug");
			Assert.True(text[index] == '\'', "Bug");

			var sb = new StringBuilder();
			char quote = text[index];
			int anchor = index++; // skip opening apostrophe

			while (index < text.Length)
			{
				char cc = text[index++];

				if (cc == quote)
				{
					if (index < text.Length && text[index] == quote)
					{
						sb.Append(quote); // un-escape
						index += 1; // skip 2nd apostrophe
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

			throw SyntaxError("Unterminated string starting at position {0}", anchor);
		}

		private static string ScanString(string text, ref int index)
		{
			Assert.True(index < text.Length, "Bug");
			Assert.True(text[index] == '"', "Bug");

			var sb = new StringBuilder();
			char quote = text[index];
			int anchor = index++; // skip opening quote

			while (index < text.Length)
			{
				char cc = text[index++];

				if (cc < ' ')
				{
					throw SyntaxError("Control character in string (position {0})", index - 1);
				}

				if (cc == quote)
				{
					return sb.ToString();
				}

				if (cc == '\\')
				{
					if (index >= text.Length)
					{
						break;
					}

					switch (cc = text[index++])
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
							throw SyntaxError("\\u#### is not yet implemented (position {0}", index);
						default:
							throw SyntaxError("Invalid escape '\\{0}' in string (position {1})", cc, index);
					}
				}
				else
				{
					sb.Append(cc);
				}
			}

			throw SyntaxError("Unterminated string starting at position {0}", anchor);
		}

		private static string ScanName(string text, ref int index)
		{
			char cc;
			if (index >= text.Length || ((cc = text[index]) != '_' && !char.IsLetter(cc)))
			{
				return null; // not a name at text[index...]
			}

			int anchor = index;
			while (index < text.Length && ((cc = text[index]) == '_' || char.IsLetterOrDigit(cc)))
			{
				index += 1;
			}

			return text.Substring(anchor, index - anchor);
		}

		private static char ScanOperator(string text, ref int index, char op1, char op2)
		{
			if (index >= text.Length) return (char) 0;
			char c = text[index];
			if (c != op1 && c != op2) return (char) 0;
			index += 1;
			return c;
		}

		private static void SkipOperator(string text, ref int index, char op1, char op2)
		{
			if (index >= text.Length) return;
			char c = text[index];
			if (c != op1 && c != op2) return;
			index += 1;
		}

		private static void SkipWhite(string text, ref int index)
		{
			while (index < text.Length && char.IsWhiteSpace(text, index))
			{
				index += 1;
			}
		}

		[StringFormatMethod("format")]
		private static FormatException SyntaxError(string format, params object[] args)
		{
			return new FormatException(string.Format(format, args));
		}
	}
}
