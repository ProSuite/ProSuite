using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.IO
{
	/// <summary>
	/// Writer for CSV files (comma separated values).
	/// </summary>
	/// <remarks>
	/// Non-string values are converted to string using
	/// <see cref="CultureInfo.InvariantCulture"/>.
	/// <para/>
	/// The writer automatically quotes a field's contents
	/// if it contains one of the characters  that have a special
	/// role or if it has leading or trailing white space.
	/// <para/>
	/// The record separator is always a newline (CRLF on Windows).
	/// <para/>
	/// The <see cref="FieldSeparator"/> defaults to a comma.
	/// The <see cref="QuoteChar"/> defaults to a double quote.
	/// Both settings can be changed to any character you like
	/// (but make sure they are different from each other and
	/// different from the record separator).
	/// <para/>
	/// To generate a CSV file that can easily be read by Excel,
	/// use a semicolon (<c>;</c>) as the <see cref="FieldSeparator"/>
	/// and leave the <see cref="QuoteChar"/> at its default.
	/// </remarks>
	public class CsvWriter : IDisposable
	{
		[CanBeNull] private TextWriter _writer;

		public CsvWriter([NotNull] TextWriter writer,
		                 char fieldSeparator = ',',
		                 char quoteChar = '"')
		{
			Assert.ArgumentNotNull(writer, nameof(writer));

			_writer = writer;

			QuoteChar = quoteChar;
			FieldSeparator = fieldSeparator;
			QuoteAllFields = false;

			RecordCount = 0;
		}

		[PublicAPI]
		public char QuoteChar { get; set; }

		[PublicAPI]
		public char FieldSeparator { get; set; }

		[PublicAPI]
		public bool QuoteAllFields { get; set; }

		public int RecordCount { get; private set; }

		/// <summary>
		/// Write the given argument string to the underlying
		/// text writer, followed by a line terminator. Write
		/// this string verbatim, that is, without quoting.
		/// Be careful, as you can create an invalid CSV file.
		/// </summary>
		[NotNull]
		public CsvWriter WriteLine([CanBeNull] string text)
		{
			CheckDisposed();

			Assert.NotNull(_writer).WriteLine(text ?? string.Empty);

			return this;
		}

		[NotNull]
		public CsvWriter WriteRecord([NotNull] params object[] values)
		{
			return WriteRecord((IEnumerable<object>) values);
		}

		[NotNull]
		public CsvWriter WriteRecord([NotNull] IEnumerable<object> values)
		{
			Assert.ArgumentNotNull(values, nameof(values));

			CheckDisposed();

			StringBuilder buffer = null;
			string q = null, qq = null;

			foreach (object value in values)
			{
				if (buffer == null)
				{
					buffer = new StringBuilder();
					q = new string(QuoteChar, 1);
					qq = new string(QuoteChar, 2);
				}
				else
				{
					buffer.Append(FieldSeparator);
				}

				string text = Convert.ToString(value, CultureInfo.InvariantCulture) ??
				              string.Empty;

				if (QuoteAllFields || NeedQuotes(text))
				{
					AppendQuoted(buffer, text, q, qq);
				}
				else
				{
					buffer.Append(text);
				}
			}

			Assert.NotNull(_writer).WriteLine(buffer);

			RecordCount += 1;

			return this;
		}

		public void Flush()
		{
			CheckDisposed();

			Assert.NotNull(_writer).Flush();
		}

		public void Dispose()
		{
			if (_writer != null)
			{
				_writer.Dispose();
				_writer = null;
			}
		}

		#region Private methods

		private void CheckDisposed()
		{
			if (_writer == null)
			{
				throw new ObjectDisposedException(GetType().Name);
			}
		}

		private static void AppendQuoted([NotNull] StringBuilder sb,
		                                 [NotNull] string text,
		                                 [NotNull] string q,
		                                 [NotNull] string qq)
		{
			sb.Append(q);

			int index = sb.Length;
			sb.Append(text);
			sb.Replace(q, qq, index, text.Length);

			sb.Append(q);
		}

		private bool NeedQuotes([NotNull] string text)
		{
			if (text.Length > 0 &&
			    (char.IsWhiteSpace(text[0]) ||
			     char.IsWhiteSpace(text[text.Length - 1])))
			{
				return true;
			}

			return text.IndexOf(QuoteChar) >= 0 ||
			       text.IndexOf(FieldSeparator) >= 0 ||
			       text.IndexOf('\r') >= 0 ||
			       text.IndexOf('\n') >= 0;
		}

		#endregion
	}
}
