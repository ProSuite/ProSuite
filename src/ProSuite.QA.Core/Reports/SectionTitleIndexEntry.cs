using System.Xml;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Core.Reports
{
	internal class SectionTitleIndexEntry : IndexEntry
	{
		private readonly string _title;

		public SectionTitleIndexEntry([NotNull] string title)
		{
			Assert.ArgumentNotNullOrEmpty(title, nameof(title));

			_title = title;
		}

		public override void Render(XmlDocument xmlDocument, XmlElement context)
		{
			XmlElement div = xmlDocument.CreateElement("div");
			div.SetAttribute("class", "indexSectionTitle");
			div.AppendChild(xmlDocument.CreateTextNode(_title));

			context.AppendChild(div);
		}
	}
}
