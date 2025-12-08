using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;

namespace ProSuite.Commons.AO.Geoprocessing
{
	public class TiledProcessingService
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IFeatureProcessor _processor;

		public TiledProcessingService(IFeatureProcessor processor)
		{
			_processor = processor;
		}

		public double ProcessingTileSize { get; set; } = 2000;

		public void Process(IPolygon processArea,
		                    IProgressFeedback progressFeedback,
		                    ITrackCancel trackCancel)
		{
			if (! _processor.RequiresTiledDataAccess)
			{
				SetMessageOnContinue(trackCancel,
				                     "Processing all features (no tiled data access).");

				_processor.Process(processArea, progressFeedback, trackCancel);
			}
			else
			{
				double tileOverlap = _processor.GetRequiredTileOverlap();

				IList<IEnvelope> tiles = TilingUtils.GetRegularSubdivisions(processArea,
					ProcessingTileSize,
					tileOverlap);

				// TODO: Add _processor.WriteResultInfo() at the end to allow flushing result info to .csv?
				var tileCount = 0;
				foreach (IEnvelope tile in tiles)
				{
					IPolygon processingAreaTile =
						GeometryUtils.GetClippedPolygon(processArea, tile);

					_msg.DebugFormat("Processing features in tile {0} of {1}: {2}",
					                 tileCount + 1, tiles.Count,
					                 GeometryUtils.ToString(tile, true));

					int featureCount = _processor.Process(processingAreaTile, progressFeedback,
					                                      trackCancel);

					tileCount++;

					if (trackCancel != null && ! trackCancel.Continue())
					{
						((IStepProgressor) trackCancel.Progressor).Message =
							$"Process canceled after {tileCount} processing tiles";

						return;
					}

					SetMessageOnContinue(trackCancel, "Processed {0} tiles of {1} ({2} features)",
					                     tileCount, tiles.Count, featureCount);
				}
			}

			SetMessageOnContinue(trackCancel,
			                     "Completed processing of {0} features, of which {1} were updated",
			                     _processor.ProcessedFeatureCount,
			                     _processor.UpdatedFeatureCount);
		}

		private static void SetMessageOnContinue([CanBeNull] ITrackCancel trackCancel,
		                                         [NotNull] string message,
		                                         params object[] args)
		{
			if (trackCancel == null || ! trackCancel.Continue())
			{
				return;
			}

			string displayMessage = string.Format(message, args);

			var stepProgressor = (IStepProgressor) trackCancel.Progressor;

			if (stepProgressor != null)
			{
				stepProgressor.Message = displayMessage;
			}

			if (SystemUtils.Is64BitProcess)
			{
				_msg.Info(displayMessage);
			}
		}
	}
}
