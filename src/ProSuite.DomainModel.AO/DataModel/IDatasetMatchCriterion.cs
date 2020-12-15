using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.DataModel
{
	[CLSCompliant(false)]
	public interface IDatasetMatchCriterion
	{
		bool IsSatisfied([NotNull] IDatasetName datasetName,
		                 [NotNull] out string reason);
	}
}
