using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geoprocessing
{
	/// <summary>
	/// Provides functionality that encapsulates the sub-selection of features from a feature class,
	/// including where clause and selection set.
	/// </summary>
	public interface ISourceFeatures
	{
		IFeatureClass FeatureClass { get; }

		IWorkspace Workspace { get; }

		int TotalFeaturesDelivered { get; }

		IEnumerable<IFeature> ReadFeatures([NotNull] IGeometry area,
		                                   bool recycle = false,
		                                   [CanBeNull] ITrackCancel trackCancel = null);
	}
}
