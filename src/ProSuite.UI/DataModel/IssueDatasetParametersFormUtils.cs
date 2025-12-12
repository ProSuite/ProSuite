using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.UI.Core.DataModel;

namespace ProSuite.UI.DataModel
{
	public static class IssueDatasetParametersFormUtils
	{
		public static IssueDatasetParametersForm Create(
			IWorkspace workspace,
			ISpatialReference spatialReference,
			ICollection<string> missingTableNames,
			ICollection<ITable> existingTables)
		{
			var configKeywords =
				WorkspaceUtils.GetConfigurationKeywords(
					              workspace,
					              keyword => keyword.Name,
					              keyword => keyword.KeywordType ==
					                         esriConfigurationKeywordType
						                         .esriConfigurationKeywordGeneral)
				              .OrderBy(k => k).ToList();

			bool supportsPrivileges = workspace.Type ==
			                          esriWorkspaceType.esriRemoteDatabaseWorkspace;

			SpatialReferenceInfo spatialReferenceInfo = new SpatialReferenceInfo(
				spatialReference.Name,
				SpatialReferenceUtils.GetXyResolution(spatialReference),
				SpatialReferenceUtils.GetXyTolerance(spatialReference),
				SpatialReferenceUtils.GetZResolution(spatialReference),
				SpatialReferenceUtils.GetZTolerance(spatialReference),
				spatialReference is IGeographicCoordinateSystem);

			var datasetItems = existingTables
			                   .Select(t => new IssueDatasetParametersForm.DatasetItem(
				                           DatasetUtils.GetName(t), true,
				                           DatasetUtils.GetFeatureDatasetName(t)?.Name))
			                   .ToList();

			var form = new IssueDatasetParametersForm(
				configKeywords,
				supportsPrivileges,
				spatialReferenceInfo,
				missingTableNames,
				datasetItems);

			return form;
		}
	}
}
