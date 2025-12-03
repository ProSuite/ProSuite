using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Progress;

namespace ProSuite.Commons.AO.Geoprocessing
{
	/// <summary>
	/// Interface used by TiledProcessingService for tile-wise processing of features.
	/// </summary>
	public interface IFeatureProcessor
	{
		int ProcessedFeatureCount { get; }

		int UpdatedFeatureCount { get; }

		int Process([NotNull] IGeometry area,
		            [CanBeNull] IProgressFeedback progressFeedback,
		            [CanBeNull] ITrackCancel trackCancel);

		double GetRequiredTileOverlap();

		bool RequiresTiledDataAccess { get; }
	}
}
