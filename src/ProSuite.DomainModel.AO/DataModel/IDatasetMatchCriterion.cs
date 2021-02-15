using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.DataModel
{
	public interface IDatasetMatchCriterion
	{
		bool IsSatisfied([NotNull] IDatasetName datasetName,
		                 [NotNull] out string reason);
	}
}
