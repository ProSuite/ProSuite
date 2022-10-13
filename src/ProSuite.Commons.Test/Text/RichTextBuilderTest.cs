using System;
using NUnit.Framework;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.Test.Text
{
	[TestFixture]
	public class RichTextBuilderTest
	{
		[Test]
		public void EmptyTextTest()
		{
			var builder = new RichTextBuilder();
			var rtf = builder.ToRtf();
			Console.WriteLine(rtf);
		}

		[Test]
		public void FormattedTextTest()
		{
			var builder = new RichTextBuilder();
			builder.LineLimit = 999; // long lines are fine for this test
			builder.Text("This is ").Bold("bold").Text(" and ").Italic("italic").Text(" text.");
			builder.LineBreak();
			builder.Text("And this is ");
			builder.BeginGroup().Bold().Italic().Text("Bold Italic").EndGroup().Text(" text.");
			var rtf = builder.ToRtf();
			Console.WriteLine(rtf);
			int i = rtf.IndexOf("This is ", StringComparison.Ordinal);
			Assert.True(i > 0);
			Assert.AreEqual(
				@"This is {\b bold} and {\i italic} text.\line And this is {\b\i Bold Italic} text.}",
				rtf.Substring(i));
		}

		[Test]
		public void LongLineWrappingTest()
		{
			var builder = new RichTextBuilder();
			builder.LineLimit = 20; // insert newline if line exceeds 20 chars
			builder.Text("The newline has no meaning to RTF").LineBreak();
			builder.Text("except it acts as a delimiter for commands.").LineBreak();
			builder.Text("We can therefore inject a newline every once ");
			builder.Text("in a while - even within a word - to avoid ");
			builder.Text("excessively long lines in the RTF file.");
			var rtf = builder.ToRtf();
			Console.WriteLine(rtf);

			int i = rtf.IndexOf("The newl", StringComparison.Ordinal);
			Assert.True(i > 0);
			var lines = rtf.Substring(i)
			               .Split(new[] {Environment.NewLine}, StringSplitOptions.None);
			var expected = new[]
			               {
				               "The newline has no m", "eaning to RTF\\line e",
				               "xcept it acts as a d", "elimiter for command",
				               "s.\\line We can there", "fore inject a newlin",
				               "e every once in a wh", "ile - even within a ",
				               "word - to avoid exce", "ssively long lines i",
				               "n the RTF file.}"
			               };
			Assert.AreEqual(expected.Length, lines.Length);
			for (int j = 0; j < expected.Length; j++)
			{
				Assert.AreEqual(expected[j], lines[j]);
			}
		}

		[Test]
		public void UnicodeCharTest()
		{
			var builder = new RichTextBuilder();
			builder.Text("Lab\u0393Value");
			var rtf = builder.ToRtf();
			Console.WriteLine(rtf);
			int i = rtf.IndexOf("Lab", StringComparison.Ordinal);
			Assert.True(i > 0);
			Assert.AreEqual(@"Lab\u915*Value}", rtf.Substring(i));
		}

		[Test]
		public void DefaultFontTest()
		{
			var builder = new RichTextBuilder();
			builder.SetDefaultFont("Arial");
			var rtf = builder.Text("Hello").ToRtf();
			Console.WriteLine(rtf);
			int i = rtf.IndexOf("\\fonttbl", StringComparison.Ordinal);
			int j = rtf.IndexOf(" Arial;", StringComparison.Ordinal);
			int k = rtf.IndexOf("Hello", StringComparison.Ordinal);
			Assert.True(i > 0 && j > i && k > j);
		}

		[Test]
		public void FontChangeTest()
		{
			var builder = new RichTextBuilder();
			builder.SetDefaultFont("Arial");
			builder.Text("Start").LineBreak();
			builder.BeginGroup().Font("Times New Roman");
			builder.Text("Times New Roman").EndGroup().LineBreak();
			builder.BeginGroup().Font("Arial").Text("Arial").EndGroup().LineBreak();
			var rtf = builder.ToRtf();
			Console.WriteLine(rtf);
			int i = rtf.IndexOf(@"{\f1 Times New Roman}", StringComparison.Ordinal);
			int j = rtf.IndexOf(@"{\f0 Arial}", StringComparison.Ordinal);
			Assert.True(i > 0 && j > i);
		}

		[Test]
		public void ControlSymbolTest()
		{
			var builder = new RichTextBuilder();

			builder.Text("!!!").LineBreak()
			       .Text("Joh.")
			       .NonBreakingSpace()
			       .Text("Seb.")
			       .NonBreakingSpace()
			       .Text("Bach ")
			       .Italic()
			       .Text("multi").Plain().NonBreakingHyphen().Text("valued ")
			       .Text("trans")
			       .OptionalHyphen()
			       .Text("po")
			       .OptionalHyphen()
			       .Text("si")
			       .OptionalHyphen()
			       .Text("tion");

			var rtf = builder.ToRtf();
			int i = rtf.IndexOf("!!!", StringComparison.Ordinal);
			Assert.AreEqual(
				"!!!\\line Joh.\\~Seb.\\~Bach \\i multi\\plain\\_valued trans\\-po\\-si\\-tion}",
				rtf.Substring(i));
		}
	}
}
