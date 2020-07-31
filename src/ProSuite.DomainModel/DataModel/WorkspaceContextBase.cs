using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.DataModel
{
	public abstract class WorkspaceContextBase : IWorkspaceContext
	{
		protected WorkspaceContextBase([NotNull] Geodatabase geodatabase)
		{
			Geodatabase = geodatabase;
		}

		[CanBeNull]
		public Geodatabase Geodatabase { get; private set; }

		[CanBeNull]
		public FeatureClass OpenFeatureClass([NotNull] string name)
		{
			FeatureClassDefinition featureClassDefinition = Geodatabase.GetDefinitions<FeatureClassDefinition>().Where(d => d.GetName() == "foo").FirstOrDefault();
			//featureClassDefinition.f

			return (FeatureClass) OpenTable(name);
		}

		[CanBeNull]
		public abstract Table OpenTable([NotNull] string name);

		public virtual void Dispose()
		{
			Geodatabase?.Dispose();
			Geodatabase = null;
		}
	}
}
