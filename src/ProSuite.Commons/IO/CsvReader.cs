using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.IO
{
	/// <summary>
	/// Reader for CSV files (comma separated values).
	/// </summary>
	/// <remarks>
	/// To read a file, call <see cref="ReadRecord"/> until it returns
	/// <c>false</c>; after each such call, <see cref="Values"/>
	/// contains the current record's values.
	/// <para/>
	/// The <see cref="FieldSeparator"/> and <see cref="QuoteChar"/>
	/// are configurable (they default to a comma and a double quote,
	/// respectively). The record separator is always a newline, that
	/// is, either CR, or LF, or CR immediately followed by LF.
	/// <para/>
	/// <see cref="RecordNumber"/> is the number of the record last read,
	/// whereas <see cref="LineNumber"/> is the reader's current line.
	/// Usually, LineNumber is one more than RecordNumber; it is larger
	/// if newlines occur within fields.
	/// <para/>
	/// If <see cref="SkipBlankLines"/> is <c>true</c>, lines that are
	/// empty or consist of white space only will be skipped.
	/// If <see cref="SkipCommentLines"/> is <c>true</c>, lines that
	/// begin with the <see cref="CommentChar"/> (optionally preceded
	/// by blanks and tabs) will be skipped.
	/// </remarks>
	public class CsvReader : IDisposable
	{
		private TextReader _reader;
		private readonly StringBuilder _buffer;

		private const int CR = 13, LF = 10;
		private const int CRLF = 0x10FFFF + 1; // max Unicode + 1

		public CsvReader([NotNull] TextReader reader, char fieldSeparator = ',',
		                 char quoteChar = '"')
		{
			Assert.ArgumentNotNull(reader, nameof(reader));

			_reader = reader;
			Values = new List<string>();
			_buffer = new StringBuilder();

			QuoteChar = quoteChar;
			FieldSeparator = fieldSeparator;

			SkipBlankLines = false;
			SkipCommentLines = false;
			CommentChar = '#';

			LineNumber = 1;
			RecordNumber = 0;
		}

		[PublicAPI]
		public char QuoteChar { get; set; }

		[PublicAPI]
		public char FieldSeparator { get; set; }

		public bool SkipBlankLines { get; set; }

		public bool SkipCommentLines { get; set; }

		[PublicAPI]
		public char CommentChar { get; set; }

		public int LineNumber { get; private set; }

		public int RecordNumber { get; private set; }

		[NotNull]
		public IList<string> Values { get; }

		/// <summary>
		/// Read the next record. If there is a next record,
		/// make its fields available through the <see cref="Values"/>
		/// property and return <c>true</c>. Otherwise, clear
		/// <see cref="Values"/> and return <c>false</c> (the reader
		/// has reached the end of the input).
		/// </summary>
		public bool ReadRecord()
		{
			CheckDisposed();

			Values.Clear();
			_buffer.Length = 0;

			int cc = Read();

			while (true)
			{
				if (cc < 0)
				{
					return false;
				}

				cc = SkipBlanks(cc);

				if (SkipCommentLines && cc == CommentChar)
				{
					cc = SkipLine(cc);
					continue;
				}

				if (SkipBlankLines && IsEndOfLine(cc))
				{
					cc = Read();
					continue;
				}

				nextField:
				cc = ReadField(cc);

				if (IsEndOfLine(cc))
				{
					RecordNumber += 1;
					return true;
				}

				if (cc == FieldSeparator)
				{
					cc = Read();
					cc = SkipBlanks(cc);
					goto nextField;
				}

				throw SyntaxError(LineNumber, "Unexpected trash after field value");
			}
		}

		public void Dispose()
		{
			if (_reader != null)
			{
				_reader.Dispose();
				_reader = null;
			}
		}

		#region Private methods

		private void CheckDisposed()
		{
			if (_reader == null)
			{
				throw new ObjectDisposedException(GetType().Name);
			}
		}

		private int Read()
		{
			int cc = _reader.Read();

			if (cc == CR || cc == LF)
			{
				if (cc == CR && _reader.Peek() == LF)
				{
					_reader.Read();
					cc = CRLF;
				}

				LineNumber += 1;
			}

			return cc;
		}

		private int SkipBlanks(int cc)
		{
			// Avoid Char.IsWhiteSpace: it includes our record separator!
			// Here we want: blank, horizontal tab, vertical tab, form feed
			// (Char.IsWhiteSpace also includes 0x85 and 0xA0, besides CR and LF)
			while (cc == 32 || cc == 9 || cc == 11 || cc == 12)
			{
				cc = Read();
			}

			return cc;
		}

		private int SkipLine(int cc)
		{
			while (! IsEndOfLine(cc))
			{
				cc = Read();
			}

			// Consume the EOL:
			return cc < 0 ? cc : Read();
		}

		private int ReadField(int cc)
		{
			// Assume leading blanks have been skipped

			_buffer.Length = 0; // clear
			var fieldLength = 0; // w/o trailing blanks

			while (cc != FieldSeparator && ! IsEndOfLine(cc))
			{
				if (cc == QuoteChar)
				{
					cc = ReadString((char) cc, _buffer);
					fieldLength = _buffer.Length;
				}
				else
				{
					_buffer.Append((char) cc);
					if (! char.IsWhiteSpace((char) cc))
					{
						fieldLength = _buffer.Length;
					}

					cc = Read();
				}
			}

			Values.Add(_buffer.ToString(0, fieldLength));

			return cc;
		}

		private int ReadString(char quote, StringBuilder buffer)
		{
			int cc;
			while ((cc = Read()) >= 0)
			{
				if (cc == CRLF)
				{
					// Within a quoted string, CRLF
					// is to be taken verbatim:
					buffer.Append('\r');
					buffer.Append('\n');
				}
				else if (cc == quote)
				{
					if (_reader.Peek() == quote)
					{
						buffer.Append(quote);
						Read();
					}
					else
					{
						return Read();
					}
				}
				else
				{
					buffer.Append((char) cc);
				}
			}

			throw SyntaxError(LineNumber, "Unterminated string");
		}

		private static bool IsEndOfLine(int cc)
		{
			// End-of-input is also end-of-line:
			return cc == CR || cc == LF || cc == CRLF || cc < 0;
		}

		private static Exception SyntaxError(int lineno, string message)
		{
			return new FormatException(string.Format("{0} (line {1})", message, lineno));
		}

		#endregion
	}
}
