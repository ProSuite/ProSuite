using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.DataModel
{
	public abstract class WorkspaceContextBase : IWorkspaceContext
	{
		protected WorkspaceContextBase(GdbWorkspaceReference workspace)
		{
			Workspace = workspace;
		}

		public GdbWorkspaceReference Workspace { get; }

		[CanBeNull]
		public FeatureClass OpenFeatureClass([NotNull] string name)
		{
			//FeatureClassDefinition featureClassDefinition = Geodatabase.GetDefinitions<FeatureClassDefinition>().Where(d => d.GetName() == "foo").FirstOrDefault();
			//featureClassDefinition.f

			return (FeatureClass) OpenTable(name);
		}

		[CanBeNull]
		public Table OpenTable(string name)
		{
			// todo daro exception handling
			using (Geodatabase geodatabase = OpenGeodatabase())
			{
				return geodatabase.OpenDataset<Table>(name);
			}
		}

		public virtual void Dispose() { }

		public Geodatabase OpenGeodatabase()
		{
			return Workspace.OpenGeodatabase();
		}

		public bool Contains(GdbTableReference proxy)
		{
			return Equals(proxy.WorkspaceReference, Workspace);
		}
	}
}
