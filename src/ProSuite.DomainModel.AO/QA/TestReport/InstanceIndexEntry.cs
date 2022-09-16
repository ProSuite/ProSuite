using System.Xml;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	internal class InstanceIndexEntry : IndexEntry
	{
		private readonly IncludedInstanceBase _includedInstance;

		public InstanceIndexEntry([NotNull] IncludedInstanceBase includedInstance)
		{
			Assert.ArgumentNotNull(includedInstance, nameof(includedInstance));

			_includedInstance = includedInstance;
		}

		public override void Render(XmlDocument xmlDocument, XmlElement context)
		{
			XmlElement anchor = xmlDocument.CreateElement("a");

			if (_includedInstance.Obsolete)
			{
				anchor.SetAttribute("class", "obsoleteIndex");
			}

			anchor.SetAttribute("href", string.Format("#{0}", _includedInstance.Key));
			anchor.SetAttribute("title", _includedInstance.IndexTooltip);
			anchor.AppendChild(xmlDocument.CreateTextNode(_includedInstance.Title));

			context.AppendChild(anchor);
			context.AppendChild(xmlDocument.CreateElement("br"));
		}
	}
}
