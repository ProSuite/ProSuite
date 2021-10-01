using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainServices.AO.QA.Standalone.RuleBased
{
	public interface IObjectDatasetLookup
	{
		[NotNull]
		ObjectDataset GetDataset([NotNull] IObjectClass objectClass);
	}
}
