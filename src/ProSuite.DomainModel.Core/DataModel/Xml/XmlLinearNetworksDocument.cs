using System.Collections.Generic;
using System.Xml.Serialization;

namespace ProSuite.DomainModel.Core.DataModel.Xml
{
	[XmlRoot("XmlLinearNetworks")]
	public class XmlLinearNetworksDocument
	{
		private readonly List<XmlLinearNetwork> _networks =
			new List<XmlLinearNetwork>();

		[XmlArrayItem("LinearNetwork")]
		public List<XmlLinearNetwork> LinearNetworks
		{
			get { return _networks; }
		}
	}
}
