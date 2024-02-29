using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProSuite.Commons.IO;

namespace ProSuite.Commons.Test.IO
{
	[TestFixture]
	public class CsvTest
	{
		[Test]
		public void CsvReaderTest()
		{
			const string csv =
				"Hello,\"happy\",world\r\nHow, do\t, \" you \" ,do? \n \r\n 3 empty (\"\"\"\"\"\") fields: ,  ,, \"\" ";
			var reader = new CsvReader(new StringReader(csv));
			Assert.AreEqual(0, reader.Values.Count);

			Assert.True(reader.ReadRecord());
			Assert.AreEqual(3, reader.Values.Count);
			Assert.AreEqual("Hello", reader.Values[0]);
			Assert.AreEqual("happy", reader.Values[1]);
			Assert.AreEqual("world", reader.Values[2]);
			Assert.AreEqual(1, reader.RecordNumber);
			Assert.AreEqual(2, reader.LineNumber);

			Assert.True(reader.ReadRecord());
			Assert.AreEqual(4, reader.Values.Count);
			Assert.AreEqual("How", reader.Values[0]);
			Assert.AreEqual("do", reader.Values[1]);
			Assert.AreEqual(" you ", reader.Values[2]);
			Assert.AreEqual("do?", reader.Values[3]);
			Assert.AreEqual(2, reader.RecordNumber);
			Assert.AreEqual(3, reader.LineNumber);

			Assert.True(reader.ReadRecord());
			Assert.AreEqual(1, reader.Values.Count);
			Assert.IsEmpty(reader.Values[0]);
			Assert.AreEqual(3, reader.RecordNumber);
			Assert.AreEqual(4, reader.LineNumber);

			Assert.True(reader.ReadRecord());
			Assert.AreEqual(4, reader.Values.Count);
			Assert.AreEqual("3 empty (\"\") fields:", reader.Values[0]);
			Assert.AreEqual(string.Empty, reader.Values[1]);
			Assert.AreEqual(string.Empty, reader.Values[2]);
			Assert.AreEqual(string.Empty, reader.Values[3]);
			Assert.AreEqual(4, reader.RecordNumber);
			Assert.AreEqual(4, reader.LineNumber); // no line terminator on last line

			Assert.False(reader.ReadRecord());
			Assert.AreEqual(0, reader.Values.Count);
			Assert.AreEqual(4, reader.RecordNumber);
			Assert.AreEqual(4, reader.LineNumber);

			reader.Dispose();

			var ex = Assert.Catch(() => reader.ReadRecord());
			Console.WriteLine(@"Expected exception: {0}", ex.Message);
		}

		[Test]
		public void CsvReaderNewlineTest()
		{
			const string csv = "One \"\"\"1\"\"\" here \nTwo\rThree\r\nFour\n\rSix\r\n\"\n\r\n\r\"";
			var reader = new CsvReader(new StringReader(csv));

			Assert.True(reader.ReadRecord());
			Assert.AreEqual(1, reader.Values.Count);
			Assert.AreEqual("One \"1\" here", reader.Values[0]);
			Assert.AreEqual(1, reader.RecordNumber);
			Assert.AreEqual(2, reader.LineNumber);

			Assert.True(reader.ReadRecord());
			Assert.AreEqual(1, reader.Values.Count);
			Assert.AreEqual("Two", reader.Values[0]);
			Assert.AreEqual(2, reader.RecordNumber);
			Assert.AreEqual(3, reader.LineNumber);

			Assert.True(reader.ReadRecord());
			Assert.AreEqual(1, reader.Values.Count);
			Assert.AreEqual("Three", reader.Values[0]);
			Assert.AreEqual(3, reader.RecordNumber);
			Assert.AreEqual(4, reader.LineNumber);

			Assert.True(reader.ReadRecord());
			Assert.AreEqual(1, reader.Values.Count);
			Assert.AreEqual("Four", reader.Values[0]);
			Assert.AreEqual(4, reader.RecordNumber);
			Assert.AreEqual(5, reader.LineNumber);

			Assert.True(reader.ReadRecord());
			Assert.AreEqual(1, reader.Values.Count);
			Assert.IsEmpty(reader.Values[0]);
			Assert.AreEqual(5, reader.RecordNumber);
			Assert.AreEqual(6, reader.LineNumber);

			Assert.True(reader.ReadRecord());
			Assert.AreEqual(1, reader.Values.Count);
			Assert.AreEqual("Six", reader.Values[0]);
			Assert.AreEqual(6, reader.RecordNumber);
			Assert.AreEqual(7, reader.LineNumber);

			Assert.True(reader.ReadRecord());
			Assert.AreEqual(1, reader.Values.Count);
			Assert.AreEqual("\n\r\n\r", reader.Values[0]);
			Assert.AreEqual(7, reader.RecordNumber);
			Assert.AreEqual(10, reader.LineNumber);

			Assert.False(reader.ReadRecord());

			reader.Dispose();
		}

		[Test]
		public void CsvReaderValueTest()
		{
			// Our reader allows fields to be partially quoted like this:
			// foo,The message was: """be tolerant in what you take""",bar

			const string csv =
				"Tight, Trimmed,\" Padded \", \" within quotes only \" , Mix: \"\"\"fine\"\"\" ingredients ";

			var reader = new CsvReader(new StringReader(csv));

			Assert.True(reader.ReadRecord());
			Assert.AreEqual(5, reader.Values.Count);
			Assert.AreEqual("Tight", reader.Values[0]);
			Assert.AreEqual("Trimmed", reader.Values[1]);
			Assert.AreEqual(" Padded ", reader.Values[2]);
			Assert.AreEqual(" within quotes only ", reader.Values[3]);
			Assert.AreEqual("Mix: \"fine\" ingredients", reader.Values[4]);

			Assert.False(reader.ReadRecord());

			reader.Dispose();
		}

		[Test]
		public void CsvReaderEmptyFieldsTest()
		{
			// Empty fields are returned.
			// A record consisting only of an empty field is still returned.
			// A blank line is returned as one empty field.
			// There's no way to distinguish an empty line from a blank line.

			const string csv = " # Comment line \r\n" +
			                   ", ,  ,\"\", \"\", \" \" \r\n";

			var reader = new CsvReader(new StringReader(csv));

			Assert.True(reader.ReadRecord());
			Assert.AreEqual(1, reader.Values.Count);
			Assert.AreEqual("# Comment line", reader.Values[0]);

			Assert.True(reader.ReadRecord());
			Assert.AreEqual(6, reader.Values.Count);
			Assert.IsEmpty(reader.Values[0]);
			Assert.IsEmpty(reader.Values[1]);
			Assert.IsEmpty(reader.Values[2]);
			Assert.IsEmpty(reader.Values[3]);
			Assert.IsEmpty(reader.Values[4]);
			Assert.AreEqual(" ", reader.Values[5]);

			Assert.IsFalse(reader.ReadRecord());

			reader.Dispose();
		}

		[Test]
		public void CsvFileEndTest()
		{
			const char sep = ';';
			const string csv1 = "a;\r\na;\r\n"; // file ends with CR LF EOF
			const string csv2 = "a;\r\na;"; // file ends with just EOF

			// Both "files" must yield the same records and fields!

			Action<CsvReader> asserter =
				reader =>
				{
					Assert.IsTrue(reader.ReadRecord());
					Assert.AreEqual(2, reader.Values.Count);
					Assert.AreEqual(string.Empty, reader.Values[1]);

					Assert.IsTrue(reader.ReadRecord());
					Assert.AreEqual(2, reader.Values.Count);
					Assert.AreEqual(string.Empty, reader.Values[1]);

					Assert.IsFalse(reader.ReadRecord());
				};

			using (var reader = new CsvReader(new StringReader(csv1), sep))
			{
				asserter(reader);
			}

			using (var reader = new CsvReader(new StringReader(csv2), sep))
			{
				asserter(reader);
			}
		}

		[Test]
		public void CsvReaderSkipBlankTest()
		{
			//                  1   2 3   4    5       6   7 8
			const string csv = "\r\n\n\r  \r\n,\r\n\"\"\r\n\n";
			var reader = new CsvReader(new StringReader(csv)) {SkipBlankLines = true};

			Assert.IsTrue(reader.ReadRecord());
			Assert.AreEqual(1, reader.RecordNumber);
			Assert.AreEqual(6, reader.LineNumber);
			Assert.AreEqual(2, reader.Values.Count);
			Assert.IsEmpty(reader.Values[0]);
			Assert.IsEmpty(reader.Values[1]);

			Assert.IsTrue(reader.ReadRecord());
			Assert.AreEqual(2, reader.RecordNumber);
			Assert.AreEqual(7, reader.LineNumber);
			Assert.AreEqual(1, reader.Values.Count);
			Assert.IsEmpty(reader.Values[0]); // the only field is empty, but quoted

			Assert.IsFalse(reader.ReadRecord());
			Assert.AreEqual(2, reader.RecordNumber);
			Assert.AreEqual(8, reader.LineNumber);
			Assert.AreEqual(0, reader.Values.Count);
		}

		[Test]
		public void CsvReaderSkipCommentTest()
		{
			const string csv = "# Comment\r\n,#Not a comment\r\n  #Another";
			var reader = new CsvReader(new StringReader(csv)) {SkipCommentLines = true};

			Assert.IsTrue(reader.ReadRecord());
			Assert.AreEqual(1, reader.RecordNumber);
			Assert.AreEqual(3, reader.LineNumber);
			Assert.AreEqual(2, reader.Values.Count);
			Assert.AreEqual("", reader.Values[0]);
			Assert.AreEqual("#Not a comment", reader.Values[1]);

			Assert.IsFalse(reader.ReadRecord());
		}

		[Test]
		public void CsvWriterTest()
		{
			var buffer = new StringBuilder();
			var writer = new CsvWriter(new StringWriter(buffer));

			writer.WriteRecord("One", "Two", "Three");
			writer.WriteRecord(); // blank line
			writer.WriteRecord("QuoteChar", new string(writer.QuoteChar, 1));
			writer.WriteRecord("FieldSeparator", new string(writer.FieldSeparator, 1));
			writer.WriteRecord("a\"b\"c", "line\nbreak", "line\rbreak", "line\r\nbreak");
			writer.WriteRecord(" leading", "trailing ", " blanks ");

			writer.Dispose();

			var ex = Assert.Catch(() => writer.WriteRecord());
			Console.WriteLine(@"Expected exception: {0}", ex.Message);

			const string expected =
				"One,Two,Three\r\n" +
				"\r\n" +
				"QuoteChar,\"\"\"\"\r\n" +
				"FieldSeparator,\",\"\r\n" +
				"\"a\"\"b\"\"c\",\"line\nbreak\",\"line\rbreak\",\"line\r\nbreak\"\r\n" +
				"\" leading\",\"trailing \",\" blanks \"\r\n";

			Assert.AreEqual("\r\n", Environment.NewLine);
			Assert.AreEqual(expected, buffer.ToString());
		}

		[Test]
		public void CsvWriterEmptyTest()
		{
			var buffer = new StringBuilder();

			new CsvWriter(new StringWriter(buffer)).Dispose();

			Assert.IsEmpty(buffer.ToString());

			buffer.Length = 0; // clear

			new CsvWriter(new StringWriter(buffer)).WriteRecord().Dispose();

			Assert.AreEqual(Environment.NewLine, buffer.ToString());

			buffer.Length = 0; // clear

			new CsvWriter(new StringWriter(buffer)).WriteRecord(string.Empty).Dispose();

			Assert.AreEqual(Environment.NewLine, buffer.ToString());
		}

		[Test]
		public void CsvDefaultSettingsTest()
		{
			// The defaults of properties must not change
			// or client code may break!

			var reader = new CsvReader(TextReader.Null);

			Assert.AreEqual('"', reader.QuoteChar);
			Assert.AreEqual(',', reader.FieldSeparator);

			Assert.AreEqual(false, reader.SkipBlankLines);
			Assert.AreEqual(false, reader.SkipCommentLines);
			Assert.AreEqual('#', reader.CommentChar);

			var writer = new CsvWriter(TextWriter.Null);

			Assert.AreEqual('"', writer.QuoteChar);
			Assert.AreEqual(',', writer.FieldSeparator);

			Assert.AreEqual(false, writer.QuoteAllFields);
		}

		[Test]
		public void CsvRoundtripTest()
		{
			const char sep = ';';

			var buffer = new StringBuilder();

			using (var writer = new CsvWriter(new StringWriter(buffer), sep))
			{
				writer.WriteRecord("One", "Two", "Three");
				writer.WriteRecord(); // blank line
				writer.WriteRecord("QuoteChar", new string(writer.QuoteChar, 1));
				writer.WriteRecord("FieldSeparator", new string(writer.FieldSeparator, 1));
				writer.WriteRecord("a\"b\"c", "line\nbreak", "line\rbreak", "line\r\nbreak");
				writer.WriteRecord(" leading", "trailing ", " blanks ");
			}

			string csv = buffer.ToString();

			using (var reader = new CsvReader(new StringReader(csv), sep))
			{
				Assert.True(reader.ReadRecord());
				Assert.AreEqual("One|Two|Three",
				                reader.Values.Aggregate((s, t) => string.Concat(s, "|", t)));

				Assert.True(reader.ReadRecord());
				Assert.AreEqual("",
				                reader.Values.Aggregate((s, t) => string.Concat(s, "|", t)));

				Assert.True(reader.ReadRecord());
				Assert.AreEqual("QuoteChar|\"",
				                reader.Values.Aggregate((s, t) => string.Concat(s, "|", t)));

				Assert.True(reader.ReadRecord());
				Assert.AreEqual("FieldSeparator|" + sep,
				                reader.Values.Aggregate((s, t) => string.Concat(s, "|", t)));

				Assert.True(reader.ReadRecord());
				Assert.AreEqual("a\"b\"c|line\nbreak|line\rbreak|line\r\nbreak",
				                reader.Values.Aggregate((s, t) => string.Concat(s, "|", t)));

				Assert.True(reader.ReadRecord());
				Assert.AreEqual(" leading|trailing | blanks ",
				                reader.Values.Aggregate((s, t) => string.Concat(s, "|", t)));

				Assert.False(reader.ReadRecord());
			}
		}
	}
}
