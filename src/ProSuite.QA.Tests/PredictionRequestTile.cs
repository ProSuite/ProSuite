using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	public class PredictionRequestTile
	{
		public PredictionRequestTile([NotNull] string imagePath,
		                         [NotNull] IEnvelope coreExtent,
		                         [NotNull] RasterTileInfo tileInfo)
		{
			ImagePath = imagePath;
			CoreExtent = coreExtent;
			TileInfo = tileInfo;
		}

		[NotNull] public string ImagePath { get; }
		[NotNull] public IEnvelope CoreExtent { get; }
		[NotNull] public RasterTileInfo TileInfo { get; }
	}
}
