using System.Xml;

namespace EsriDE.ProSuite.DomainModel.QA.TestReport
{
	internal class SectionTitleIndexEntry : IndexEntry
	{
		private readonly string _title;

		public SectionTitleIndexEntry(string title)
		{
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