using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Xml;

namespace ProSuite.AGP.WorkList.Domain.Persistence.Xml
{
	public class XmlWorkItemStateRepository : WorkItemStateRepository<XmlWorkItemState, XmlWorkListDefinition>
	{
		private readonly string _geodatabasePath;
		private readonly string _xmlFilePath;

		public XmlWorkItemStateRepository(string geodatabasePath, string xmlFilePath)
		{
			_geodatabasePath = geodatabasePath;
			_xmlFilePath = xmlFilePath;
		}

		protected override void Store(XmlWorkListDefinition definition)
		{
			var helper = new XmlSerializationHelper<XmlWorkListDefinition>();
			helper.SaveToFile(definition, _xmlFilePath);
		}

		protected override XmlWorkListDefinition CreateDefinition(List<XmlWorkItemState> states)
		{
			return new XmlWorkListDefinition { GeodatabasePath = _geodatabasePath, Items = states};
		}

		protected override List<XmlWorkItemState> ReadStates()
		{
			if (! File.Exists(_xmlFilePath))
			{
				return new List<XmlWorkItemState>();
			}

			var helper = new XmlSerializationHelper<XmlWorkListDefinition>();

			XmlWorkListDefinition definition = helper.ReadFromFile(_xmlFilePath);

			return definition.Items.ToList();
		}

		protected override XmlWorkItemState CreateState(IWorkItem item)
		{
			var state = new XmlWorkItemState(item.OID, item.Visited, WorkItemStatus.Unknown,
			                            new XmlGdbRowIdentity(item.Proxy));
			return state;
		}

		protected override IWorkItem RefreshCore(IWorkItem item, XmlWorkItemState state)
		{
			item.Status = state.Status;

			return item;
		}

		protected override void UpdateCore(XmlWorkItemState state, IWorkItem item)
		{
			state.Status = item.Status;
		}
	}
}
