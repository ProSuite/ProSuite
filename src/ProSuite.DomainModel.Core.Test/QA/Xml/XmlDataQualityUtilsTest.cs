using System;
using System.Globalization;
using NUnit.Framework;
using ProSuite.DomainModel.Core.QA.Xml;

namespace ProSuite.DomainModel.Core.Test.QA.Xml
{
	[TestFixture]
	public class XmlDataQualityUtilsTest
	{
		[Test]
		public void CanFormatAndParse()
		{
			DateTime dateTime = DateTime.Now;

			string formatted = XmlDataQualityUtils.Format(dateTime);

			Console.WriteLine(formatted);

			DateTime? parsed = XmlDataQualityUtils.ParseDateTime(formatted);

			Console.WriteLine(parsed);

			Assert.IsNotNull(parsed);
			Assert.AreEqual(dateTime.Kind, parsed.Value.Kind);
			Assert.AreEqual(dateTime.ToString(CultureInfo.CurrentCulture),
			                parsed.Value.ToString(CultureInfo.CurrentCulture));
			Assert.AreNotEqual((double) dateTime.Ticks, parsed.Value.Ticks);
			Assert.IsFalse(DateTime.Equals(dateTime, parsed.Value));

			Assert.IsTrue(XmlDataQualityUtils.AreEqual(dateTime, parsed));
		}

		// TODO add tests for importing, exporting and transferring metadata
	}
}
