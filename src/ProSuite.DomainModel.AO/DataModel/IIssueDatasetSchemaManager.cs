using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.DataModel
{
	public interface IIssueDatasetSchemaManager
	{
		[NotNull]
		ICollection<string> GetMissingErrorDatasets(
			[NotNull] IWorkspace schemaOwnerWorkspace,
			[NotNull] out IList<ITable> existingTables);

		[NotNull]
		ICollection<ITable> CreateMissingErrorDatasets(
			[NotNull] IWorkspace schemaOwnerWorkspace,
			[NotNull] ISpatialReference spatialReference,
			[CanBeNull] string featureDatasetName,
			[CanBeNull] string configKeyword,
			double gridSize1,
			double gridSize2,
			double gridSize3,
			[NotNull] IEnumerable<string> readers,
			[NotNull] IEnumerable<string> writers);
	}
}
