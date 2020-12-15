using System.Xml;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	internal class TestIndexEntry : IndexEntry
	{
		private readonly IncludedTestBase _includedTest;

		public TestIndexEntry([NotNull] IncludedTestBase includedTest)
		{
			Assert.ArgumentNotNull(includedTest, nameof(includedTest));

			_includedTest = includedTest;
		}

		public override void Render(XmlDocument xmlDocument, XmlElement context)
		{
			XmlElement anchor = xmlDocument.CreateElement("a");

			if (_includedTest.Obsolete)
			{
				anchor.SetAttribute("class", "obsoleteIndex");
			}

			anchor.SetAttribute("href", string.Format("#{0}", _includedTest.Key));
			anchor.SetAttribute("title", _includedTest.IndexTooltip);
			anchor.AppendChild(xmlDocument.CreateTextNode(_includedTest.Title));

			context.AppendChild(anchor);
			context.AppendChild(xmlDocument.CreateElement("br"));
		}
	}
}