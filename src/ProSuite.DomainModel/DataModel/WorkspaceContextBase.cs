using ArcGIS.Core.Data;
using EsriDE.ProSuite.DomainModel.Core.DataModel;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.DataModel
{
	public abstract class WorkspaceContextBase : IWorkspaceContext
	{
		protected WorkspaceContextBase(Geodatabase geodatabase)
		{
			Geodatabase = geodatabase;
		}

		[CanBeNull]
		public Geodatabase Geodatabase { get; private set; }

		public FeatureClass OpenFeatureClass(IVectorDataset dataset)
		{
			return (FeatureClass) OpenTable(dataset);
		}

		public abstract Table OpenTable(IObjectDataset dataset);

		public virtual void Dispose()
		{
			Geodatabase?.Dispose();
			Geodatabase = null;
		}
	}
}
