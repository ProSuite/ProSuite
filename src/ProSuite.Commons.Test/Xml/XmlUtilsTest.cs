using System;
using System.Xml;
using NUnit.Framework;
using ProSuite.Commons.Xml;

namespace ProSuite.Commons.Test.Xml
{
	[TestFixture]
	public class XmlUtilsTest
	{
		[Test]
		public void CanFormatValidXml()
		{
			const string xml = "<a><b>content</b></a>";
			string formatted = XmlUtils.Format(xml);

			Console.WriteLine(formatted);

			Assert.AreEqual("<a>\r\n  <b>content</b>\r\n</a>", formatted);
		}

		[Test]
		public void CantFormatMalformedXml()
		{
			const string xml = "<a><b>content</a>";

			try
			{
				string formatted = XmlUtils.Format(xml);
				Assert.Fail("Should not format malformed xml (result: {0})", formatted);
			}
			catch (XmlException e)
			{
				Console.WriteLine(@"Expected exception: {0}", e.Message);
			}
		}

		[Test]
		public void CanEscapeInvalidXmlCharacters()
		{
			string text = "#aaaa" + '\x0B' + "bbb" + '\x0C' + "#";
			Console.WriteLine(text);

			string escaped = XmlUtils.EscapeInvalidCharacters(text);

			Console.WriteLine(escaped);

			Assert.AreEqual("#aaaa[0x0B]bbb[0x0C]#", escaped);
		}

		
	}
}
