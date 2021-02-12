using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA.IssuePersistence
{
	public interface IDeletableErrorRowFilter
	{
		bool IsDeletable([NotNull] IRow errorRow,
		                 [NotNull] QualityCondition qualityCondition);
	}
}
