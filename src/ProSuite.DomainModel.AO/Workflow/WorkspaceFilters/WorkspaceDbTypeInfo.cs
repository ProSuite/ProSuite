using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Gdb;

namespace ProSuite.DomainModel.AO.Workflow.WorkspaceFilters
{
	public class WorkspaceDbTypeInfo
	{
		public WorkspaceDbTypeInfo([NotNull] string name, WorkspaceDbType dbType)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			Name = name;
			DBType = dbType;
		}

		[NotNull]
		public string Name { get; private set; }

		public WorkspaceDbType DBType { get; private set; }
	}
}
