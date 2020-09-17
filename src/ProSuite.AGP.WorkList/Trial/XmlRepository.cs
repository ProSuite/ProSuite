using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Xml;

namespace ProSuite.AGP.WorkList.Trial
{
	public class XmlRepository : Repository<XmlWorkItem, XmlBasedWorkListDefinition>, IRepository<IWorkItemState>
	{
		private readonly string _path;

		public XmlRepository(string path)
		{
			_path = path;
		}

		protected override void Store(XmlBasedWorkListDefinition definition)
		{
			var helper = new XmlSerializationHelper<XmlBasedWorkListDefinition>();
			helper.SaveToFile(definition, definition.Path);
		}

		protected override XmlBasedWorkListDefinition CreateDefinition(List<XmlWorkItem> states)
		{
			return new XmlBasedWorkListDefinition {Path = _path, Items = states};
		}

		protected override List<XmlWorkItem> ReadStates()
		{
			if (! File.Exists(_path))
			{
				return new List<XmlWorkItem>();
			}

			var helper = new XmlSerializationHelper<XmlBasedWorkListDefinition>();

			XmlBasedWorkListDefinition definition = helper.ReadFromFile(_path);

			return definition.Items.Cast<XmlWorkItem>().ToList();
		}

		protected override XmlWorkItem CreateState(IWorkItem item)
		{
			var state = new XmlWorkItem(item.OID, item.Visited, WorkItemStatus.Unknown,
			                            new XmlGdbRowIdentity(item.Proxy));
			return state;
		}

		protected override IWorkItem RefreshCore(IWorkItem item, XmlWorkItem state)
		{
			item.Status = state.Status;

			return item;
		}

		protected override void UpdateCore(XmlWorkItem state, IWorkItem item)
		{
			state.Status = item.Status;
		}
	}

	public class JsonRepository : Repository<JsonWorkItem, JsonBasedWorkListDefinition>,
	                              IRepository<IWorkItemState>
	{
		protected override void Store(JsonBasedWorkListDefinition definition)
		{
			throw new System.NotImplementedException();
		}

		protected override JsonBasedWorkListDefinition CreateDefinition(List<JsonWorkItem> states)
		{
			throw new System.NotImplementedException();
		}

		protected override JsonWorkItem CreateState(IWorkItem item)
		{
			throw new System.NotImplementedException();
		}

		protected override List<JsonWorkItem> ReadStates()
		{
			throw new System.NotImplementedException();
		}
	}

	public class JsonBasedWorkListDefinition : IWorkListDefinition<JsonWorkItem>
	{
		public string Path { get; set; }
		public List<JsonWorkItem> Items { get; set; }
	}

	public class JsonWorkItem : IWorkItemState
	{
		public int OID { get; set; }
		public bool Visited { get; set; }
		public WorkItemStatus Status { get; set; }
	}
}
