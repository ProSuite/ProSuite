using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;

namespace ProSuite.AGP.WorkList.Domain
{
	// todo daro: refactor
	public abstract class WorkListFactoryBase : IWorkListFactory
	{
		protected IWorkList WorkList { get; set; }

		public abstract string Name { get; }

		public abstract IWorkList Get();
	}

	public class WorkListFactory : WorkListFactoryBase
	{
		public WorkListFactory(IWorkList workList)
		{
			WorkList = workList;
		}

		public override string Name => WorkList.Name;

		public override IWorkList Get()
		{
			return WorkList;
		}
	}

	public class XmlBasedWorkListFactory : WorkListFactoryBase
	{
		private readonly string _path;

		// todo daro: get name from path?
		public XmlBasedWorkListFactory(string path, string name)
		{
			Name = name;
			_path = path;
		}

		public override string Name { get; }

		public override IWorkList Get()
		{
			if (WorkList == null)
			{
				XmlWorkListDefinition definition = XmlWorkItemStateRepository.Import(_path);

				WorkList = WorkListUtils.Create(definition);

				WorkList.Name = Name;
			}

			return WorkList;
		}
	}
}
