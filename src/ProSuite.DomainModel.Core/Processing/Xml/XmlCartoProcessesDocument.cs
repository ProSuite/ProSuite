using System.Collections.Generic;
using System.Xml.Serialization;

namespace ProSuite.DomainModel.Core.Processing.Xml
{
	[XmlRoot("GdbProcesses")]
	public class XmlCartoProcessesDocument
	{
		[XmlArrayItem("ProcessGroup")]
		public List<XmlCartoProcessGroup> Groups { get; } =
			new List<XmlCartoProcessGroup>();

		[XmlArrayItem("Process")]
		public List<XmlCartoProcess> Processes { get; } = new List<XmlCartoProcess>();

		[XmlArrayItem("ProcessType")]
		public List<XmlCartoProcessType> Types { get; } = new List<XmlCartoProcessType>();
	}
}
