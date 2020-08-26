using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.DataModel
{
	public abstract class WorkspaceContextBase : IWorkspaceContext
	{
		protected WorkspaceContextBase(GdbWorkspaceIdentity workspace)
		{
			Workspace = workspace;
		}

		public GdbWorkspaceIdentity Workspace { get; }

		[CanBeNull]
		public FeatureClass OpenFeatureClass([NotNull] string name)
		{
			//FeatureClassDefinition featureClassDefinition = Geodatabase.GetDefinitions<FeatureClassDefinition>().Where(d => d.GetName() == "foo").FirstOrDefault();
			//featureClassDefinition.f

			return (FeatureClass) OpenTable(name);
		}

		// todo daro: try to open a list of tables and dispose Geodatabase afterwards
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

		public bool Contains(GdbTableIdentity proxy)
		{
			return Equals(proxy.Workspace, Workspace);
		}
	}
}
