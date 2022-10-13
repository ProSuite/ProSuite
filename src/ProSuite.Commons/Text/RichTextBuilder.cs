using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Text
{
	/// <summary>
	/// A helper to build syntactically correct RTF documents.
	/// Useful in conjunction with the WinForms RichTextBox.
	/// A very small subset of RTF is accessible through this builder.
	/// Next on my wishlist: colors (needs color table, similar to fonttbl).
	/// <para/>
	/// See the <a href="http://www.biblioscape.com/rtf15_spec.htm">RTF 1.5 Spec</a>
	/// and <a href="https://en.wikipedia.org/wiki/Rich_Text_Format">Wikipedia</a>
	/// </summary>
	public class RichTextBuilder
	{
		private readonly IList<FontEntry> _fontTable;
		private readonly StringBuilder _body;
		private int _groupNesting; // not counting the outermost {\rtf1...} group
		private bool _needDelimiter;
		private bool _hasUnicodeChars;
		private int _lastNewline;

		// RTF is case sensitive and control words must consist of [a-z] only:
		private static readonly Regex CommandRegex = new Regex(@"^[a-z]+$");

		/// <summary>
		/// Avoid lines in the RTF file that are longer than this value.
		/// <para/>
		/// The RTF spec does not mandate a maximum line length.
		/// This parameter is merely a convenience in case the generated
		/// RTF file is to be opened in a text editor.
		/// <para/>
		/// Because commands are not broken, lines may be longer than this limit.
		/// </summary>
		public int LineLimit { get; set; }

		/// <summary>
		/// If <c>true</c>, emit <c>'\t'</c> in text as "\tab" (the RTF tab command);
		/// otherwise, emit <c>'\t'</c> as "\'09" (the usual hex escape).
		/// </summary>
		public bool ObeyTabs { get; set; }

		/// <summary>
		/// If <c>true</c>, emit any end-of-line in text as "\line" (the
		/// RTF newline command); otherwise, emit end-of-lines as hex escapes.
		/// </summary>
		public bool ObeyLines { get; set; } // iff true, translate '\n' in text to "\line "

		public RichTextBuilder()
		{
			_fontTable = new List<FontEntry>();
			_body = new StringBuilder();
			_groupNesting = 0;
			_needDelimiter = false;
			_hasUnicodeChars = false;
			_lastNewline = 0;

			LineLimit = 72;
			ObeyTabs = false;
			ObeyLines = false;

			SetDefaultFont("Microsoft Sans Serif", "nil");
		}

		/// <remarks>
		/// The <paramref name="family"/>, if present, should be one of:
		/// nil, roman, swiss, modern, script, decor, tech, bidi.
		/// It is supposed to help with font substitution, but appears
		/// to be of limited use in practice and therefore may best be
		/// omitted.
		/// </remarks>
		public void SetDefaultFont([NotNull] string name, string family = null)
		{
			if (_fontTable.Count < 1)
			{
				_fontTable.Add(new FontEntry(0, name, family));
			}
			else
			{
				_fontTable[0] = new FontEntry(0, name, family);
			}
		}

		/// <summary>
		/// Emit plain text to RTF. Special characters in <paramref name="text"/>
		/// will be escaped unless requested otherwise by <see cref="ObeyTabs"/>
		/// and <see cref="ObeyLines"/>.
		/// </summary>
		public RichTextBuilder Text(string text)
		{
			text = text ?? string.Empty;

			CheckLineLimit();

			if (_needDelimiter)
			{
				_body.Append(' ');
			}

			// TODO if ObeyEscapes: keep \- and \_ and \~ (but still escape \ { } and non-7bit-ascii) ??

			for (int i = 0; i < text.Length; i++)
			{
				char c = text[i];

				// Convert Windows CRLF and old Macintosh CR
				// end-of-line conventions to Unix LF only:

				if (c == '\r')
				{
					if (i + 1 < text.Length && text[i + 1] == '\n')
						continue;
					c = '\n';
				}

				if (c == '\t' && ObeyTabs)
				{
					_body.Append(@"\tab ");
					// todo omit trailing blank if next char is 'delimiting'
				}
				else if (c == '\n' && ObeyLines)
				{
					_body.Append(@"\line ");
					// todo omit trailing blank if next char is 'delimiting'
				}
				else if (c == '\\' || c == '{' || c == '}')
				{
					_body.Append('\\');
					_body.Append(c);
				}
				else if (c < 32)
				{
					_body.AppendFormat(@"\'{0:x2}", c & 0xFF);
				}
				else if (c < 127)
				{
					_body.Append(c);
				}
				else if (c < 256)
				{
					_body.AppendFormat(@"\'{0:x2}", c & 0xFF);
				}
				else
				{
					int u = c < 32768 ? c : c - 65536;
					_body.AppendFormat(@"\u{0}*", u);
					//_body.AppendFormat(@"\uc1\u{0}*", u);
					_hasUnicodeChars = true;
				}

				CheckLineLimit();
			}

			_needDelimiter = false;
			return this;
		}

		/// <summary>See <see cref="Text(string)"/>.</summary>
		[StringFormatMethod("format")]
		public RichTextBuilder TextFormat([NotNull] string format, params object[] args)
		{
			return Text(string.Format(format, args));
		}

		/// <summary>
		/// Append <c>\~</c> (backslash tilde),
		/// which represents a non-breaking space.
		/// </summary>
		public RichTextBuilder NonBreakingSpace()
		{
			AppendControlSymbol('~');
			return this;
		}

		/// <summary>
		/// Append <c>\_</c> (backslash underscore),
		/// which represents a non-breaking hyphen.
		/// </summary>
		public RichTextBuilder NonBreakingHyphen()
		{
			AppendControlSymbol('_');
			return this;
		}

		/// <summary>
		/// Append <c>\-</c> (backslash minus),
		/// which represents an optional hyphen.
		/// </summary>
		public RichTextBuilder OptionalHyphen()
		{
			AppendControlSymbol('-');
			return this;
		}

		/// <summary>
		/// Append <c>\line</c>, causing a line break.
		/// </summary>
		public RichTextBuilder LineBreak()
		{
			AppendControlWord("line");
			return this;
		}

		/// <summary>
		/// Append <c>\tab</c> to skip to the next tab stop.
		/// </summary>
		public RichTextBuilder Tab()
		{
			AppendControlWord("tab");
			return this;
		}

		/// <summary>
		/// Emit <paramref name="text"/> verbatim to RTF.
		/// Caller's duty to generate valid RTF.
		/// </summary>
		/// <remarks>
		/// Caller's job to end <paramref name="text"/>
		/// with a blank if necessary.
		/// </remarks>
		public RichTextBuilder Raw(string text)
		{
			if (_needDelimiter)
			{
				_body.Append(' ');
			}

			_body.Append(text ?? string.Empty);
			_needDelimiter = false; // assumption (see remarks)
			return this;
		}

		public RichTextBuilder BeginGroup()
		{
			_body.Append("{");
			_groupNesting += 1;
			_needDelimiter = false;
			return this;
		}

		public RichTextBuilder EndGroup()
		{
			if (_groupNesting < 1)
			{
				throw new InvalidOperationException("No open group");
			}

			_body.Append("}");
			_groupNesting -= 1;
			_needDelimiter = false;
			return this;
		}

		public RichTextBuilder Font([NotNull] string fontName, string family = null)
		{
			int i = 0;
			for (; i < _fontTable.Count; i++)
			{
				if (string.Equals(fontName, _fontTable[i].Name))
				{
					break;
				}
			}

			if (i < _fontTable.Count)
			{
				_fontTable[i] = new FontEntry(i, fontName, family);
			}
			else
			{
				_fontTable.Add(new FontEntry(i, fontName, family));
			}

			_body.AppendFormat(CultureInfo.InvariantCulture, @"\f{0}", i);
			_needDelimiter = true;
			return this;
		}

		/// <param name="fontSize">In typographic points</param>
		public RichTextBuilder FontSize(double fontSize)
		{
			fontSize *= 2;
			int halfpoints = (int) Math.Round(fontSize);
			if (halfpoints < 5)
				halfpoints = 5;
			else if (halfpoints > 288)
				halfpoints = 288;
			AppendControlWord("fs", halfpoints);
			return this;
		}

		public RichTextBuilder Bold()
		{
			AppendControlWord("b");
			return this;
		}

		public RichTextBuilder Bold(string text)
		{
			return BeginGroup().Bold().Text(text).EndGroup();
		}

		public RichTextBuilder BoldFormat(string format, params object[] args)
		{
			return BeginGroup().Bold().TextFormat(format, args).EndGroup();
		}

		public RichTextBuilder Italic()
		{
			AppendControlWord("i");
			return this;
		}

		public RichTextBuilder Italic(string text)
		{
			return BeginGroup().Italic().Text(text).EndGroup();
		}

		public RichTextBuilder ItalicFormat(string format, params object[] args)
		{
			return BeginGroup().Italic().TextFormat(format, args).EndGroup();
		}

		public RichTextBuilder Plain()
		{
			AppendControlWord("plain");
			return this;
		}

		public RichTextBuilder Control([NotNull] string word)
		{
			AppendControlWord(word);
			return this;
		}

		public RichTextBuilder Control([NotNull] string word, int value)
		{
			AppendControlWord(word, value);
			return this;
		}

		public RichTextBuilder Control(char symbol)
		{
			AppendControlSymbol(symbol);
			return this;
		}

		public string ToRtf()
		{
			if (_groupNesting > 0)
			{
				throw new InvalidOperationException("Open groups");
			}

			var sb = new StringBuilder();
			sb.Append(@"{\rtf1\ansi");
			if (_hasUnicodeChars)
			{
				// emit \ansicpg1252?
				sb.Append(
					@"\uc1"); // presently, we use '*' as the ANSI equivalent for any Unicode char
			}

			sb.Append(@"\deff0");
			EmitFontTable(sb);
			sb.Append(_body);
			sb.Append(@"}");
			return sb.ToString();
		}

		public override string ToString()
		{
			return ToRtf();
		}

		#region Private stuff

		/// <remarks>
		/// Avoid exceedingly long lines by inserting a newline
		/// every once in a while. Newlines are ignored by RTF,
		/// except that they delimit commands.
		/// </remarks>
		private void CheckLineLimit()
		{
			if (LineLimit <= 0)
			{
				return; // not configured, allow arbitrarily long lines
			}

			if (_body.Length - _lastNewline >= LineLimit)
			{
				_body.AppendLine();
				_lastNewline = _body.Length;
				_needDelimiter = false; // the newline delimits commands
			}
		}

		private void AppendControlWord(string word)
		{
			if (word == null)
				throw new ArgumentNullException();
			if (! CommandRegex.IsMatch(word))
				throw new ArgumentException("control word must consist of letters in [a-z] only");

			// No need to check _needDelimiter: the control word's
			// backslash delimits any dangling control word.

			CheckLineLimit();
			_body.Append('\\').Append(word);
			_needDelimiter = true;
		}

		private void AppendControlWord(string word, int value)
		{
			if (word == null)
				throw new ArgumentNullException();
			if (! CommandRegex.IsMatch(word))
				throw new ArgumentException("control word must consist of letters in [a-z] only");

			// No need to check _needDelimiter: the control word's
			// backslash delimits any dangling control word.

			CheckLineLimit();
			_body.AppendFormat(CultureInfo.InvariantCulture, "\\{0}{1}", word, value);
			_needDelimiter = true;
		}

		private void AppendControlSymbol(char symbol)
		{
			if (char.IsLetter(symbol))
			{
				throw new ArgumentException("A letter cannot be a control symbol");
			}

			// No need to check _needDelimiter: the control symbol's
			// backslash delimits any dangling control word.

			CheckLineLimit();
			_body.Append('\\').Append(symbol);
			_needDelimiter = false;
		}

		private void EmitFontTable(StringBuilder sb)
		{
			sb.AppendLine(@"{\fonttbl");
			foreach (var entry in _fontTable)
			{
				sb.Append(@"{\f").Append(entry.Id);
				if (! string.IsNullOrEmpty(entry.Family))
				{
					sb.Append(@"\f").Append(entry.Family);
				}

				sb.Append(" ").Append(entry.Name);
				sb.AppendLine(";}");
			}

			sb.AppendLine("}");
		}

		private struct FontEntry
		{
			public readonly int Id;
			public readonly string Family;
			public readonly string Name;

			public FontEntry(int id, string name, string family = null)
			{
				if (id < 0)
					throw new ArgumentException(@"font id must not be negative");
				if (string.IsNullOrEmpty(name))
					throw new ArgumentException(@"font name is required");

				Id = id;
				Name = name;
				Family = family;
			}
		}

		#endregion
	}
}
