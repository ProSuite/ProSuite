using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using Array = System.Array;

namespace ProSuite.Commons.AO.Raster
{
	public static class ElevationRasterExportUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private static readonly int _defaultBlockSize =
			SystemUtils.Is64BitProcess ? 1024 : 128;

		private const int _rasterBandIdx = 0;

		/// <summary>
		/// Writes an ESRI ASCII raster file using the current culture's decimal separator.
		/// </summary>
		/// <param name="rasterPath"></param>
		/// <param name="rasterName"></param>
		/// <param name="outputFile"></param>
		/// <param name="noDataValue"></param>
		/// <param name="zValueFormat"></param>
		public static void ToAsciiRaster([NotNull] string rasterPath,
										 [NotNull] string rasterName,
										 [NotNull] string outputFile,
										 float noDataValue = -9999,
										 string zValueFormat = null)
		{
			try
			{
				Stopwatch watch = _msg.DebugStartTiming();

				IRasterDataset2 rasterDataset =
					(IRasterDataset2)DatasetUtils.OpenRasterDataset(
						rasterPath, rasterName);

				ToAsciiRaster(rasterDataset, outputFile, noDataValue, 0,
							  zValueFormat);

				Marshal.ReleaseComObject(rasterDataset);

				_msg.DebugStopTiming(watch, "Exported {0} to {1}", rasterName,
									 outputFile);
			}
			catch (Exception e)
			{
				_msg.Debug("Error creating ascii raster", e);
				throw;
			}
		}

		/// <summary>
		/// Writes an ESRI ASCII raster file using the current culture's decimal separator.
		/// </summary>
		/// <param name="rasterDataset"></param>
		/// <param name="outputFile"></param>
		/// <param name="noDataValue"></param>
		/// <param name="blockSize"></param>
		/// <param name="zValueFormat"></param>
		public static void ToAsciiRaster([NotNull] IRasterDataset2 rasterDataset,
										 [NotNull] string outputFile,
										 float noDataValue,
										 int blockSize = 0,
										 string zValueFormat = null)
		{
			try
			{
				_msg.DebugFormat(
					"Writing ASCII raster {0} using NoData value {1} and z format {2}",
					outputFile, noDataValue, zValueFormat);

				using (RasterTextWriter rasterTextWriter =
					new RasterTextWriter(outputFile))
				{
					IRaster raster = rasterDataset.CreateFullRaster();

					var rasterProperties = (IRasterProps)raster;
					float inputNoData = RasterUtils.GetNoDataValue(rasterProperties);

					rasterTextWriter.WriteAsciiGridHeader(rasterProperties, noDataValue);

					foreach (var pixelBlock in GetPixelBlocks(raster, blockSize))
					{
						RasterBlockDefinition blockDefinition = pixelBlock.Key;
						Array pixels = pixelBlock.Value;

						for (long j = 0; j < blockDefinition.Height; j++)
						{
							var sb = new StringBuilder();
							for (long i = 0; i < blockDefinition.Width; i++)
							{
								float z = (float)pixels.GetValue(i, j);

								// ReSharper disable once CompareOfFloatsByEqualityOperator
								// because this is performance-critical
								if (z == inputNoData)
								{
									z = noDataValue;
								}

								// ToString is 50% of this method's cost!
								sb.Append(z.ToString(zValueFormat));
								sb.Append(" ");
							}

							rasterTextWriter.AppendDataLine(sb.ToString());
						}
					}

					Marshal.ReleaseComObject(raster);
				}
			}
			catch (Exception ex)
			{
				_msg.Debug(ex.Message, ex);
				throw;
			}
		}

		/// <summary>
		/// Writes a space-delimited XYZ file using the current culture's decimal separator.
		/// </summary>
		/// <param name="rasterPath"></param>
		/// <param name="rasterName"></param>
		/// <param name="outputFile"></param>
		/// <param name="noDataValue"></param>
		/// <param name="zValueFormat"></param>
		public static void ToXyz([NotNull] string rasterPath,
								 [NotNull] string rasterName,
								 [NotNull] string outputFile,
								 float noDataValue = -9999,
								 string zValueFormat = null)
		{
			try
			{
				Stopwatch watch = _msg.DebugStartTiming();

				IRasterDataset2 rasterDataset =
					(IRasterDataset2)DatasetUtils.OpenRasterDataset(
						rasterPath, rasterName);

				ToXyz(rasterDataset, outputFile, noDataValue, zValueFormat);

				Marshal.ReleaseComObject(rasterDataset);

				_msg.DebugStopTiming(watch, "Exported {0} to {1}", rasterName,
									 outputFile);
			}
			catch (Exception e)
			{
				_msg.Debug("Error creating ascii raster", e);
				throw;
			}
		}

		/// <summary>
		/// Writes a space-delimited XYZ file using the current culture's decimal separator.
		/// </summary>
		/// <param name="rasterDataset"></param>
		/// <param name="outputFile"></param>
		/// <param name="noDataValue"></param>
		/// <param name="zValueFormat"></param>
		public static void ToXyz([NotNull] IRasterDataset2 rasterDataset,
								 [NotNull] string outputFile,
								 float noDataValue,
								 string zValueFormat = null)
		{
			try
			{
				_msg.DebugFormat(
					"Writing XYZ file {0} using NoData value {1} and z format {2}",
					outputFile, noDataValue, zValueFormat);

				using (RasterTextWriter rasterTextWriter =
					new RasterTextWriter(outputFile))
				{
					IRaster raster = rasterDataset.CreateFullRaster();

					var rasterProperties = (IRasterProps)raster;
					float inputNoData = RasterUtils.GetNoDataValue(rasterProperties);

					rasterTextWriter.WriteXYZHeader();

					var cellSize = rasterProperties.MeanCellSize().X;

					var extent = rasterProperties.Extent;
					var xulCorner = extent.UpperLeft.X + cellSize / 2;
					var yulCorner = extent.UpperLeft.Y - cellSize / 2;

					double currentY = yulCorner;

					foreach (var pixelBlock in GetPixelBlocks(raster))
					{
						RasterBlockDefinition blockDefinition = pixelBlock.Key;
						Array pixels = pixelBlock.Value;

						for (long j = 0; j < blockDefinition.Height; j++)
						{
							for (long i = 0; i < blockDefinition.Width; i++)
							{
								float z = (float)pixels.GetValue(i, j);

								// ReSharper disable once CompareOfFloatsByEqualityOperator
								// because this is performance-critical
								if (z == inputNoData)
								{
									z = noDataValue;
								}

								double currentX = xulCorner + i * cellSize;

								// The 3 ToString() method's are 2/3 of the method's cost!
								// Consider optimization if x/y are integer values
								string zVal = z.ToString(zValueFormat);

								rasterTextWriter.AppendData(currentX);
								rasterTextWriter.AppendData(" ");

								rasterTextWriter.AppendData(currentY);
								rasterTextWriter.AppendData(" ");

								rasterTextWriter.AppendDataLine(zVal);
							}

							currentY -= cellSize;
						}
					}

					Marshal.ReleaseComObject(raster);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				throw;
			}
		}

		#region Private methods

		private static IEnumerable<KeyValuePair<RasterBlockDefinition, Array>>
			GetPixelBlocks(IRaster raster, int blockSize = -1)
		{
			var pixelBlockCursor = new PixelBlockCursor();
			pixelBlockCursor.InitByRaster(raster);

			IRasterProps rasterProperties = (IRasterProps)raster;
			int imageWidth = rasterProperties.Width;
			int imageHeight = rasterProperties.Height;

			int bufferSize = blockSize > 0 ? blockSize : _defaultBlockSize;

			pixelBlockCursor.UpdateBlockSize(imageWidth, bufferSize);
			int linesLeft = imageHeight;
			int lineStart = 0;

			while (linesLeft > 0)
			{
				int lineLast;
				if (linesLeft > bufferSize)
				{
					lineLast = bufferSize;
				}
				else
				{
					lineLast = linesLeft;
					// Unfortunately, the height of the block is not changed by calling
					// pixelBlockCursor.UpdateBlockSize(). Therefore the block definition
					// must also be returned.
				}

				var pixelblock3 = (IPixelBlock3)pixelBlockCursor.NextBlock(
					0, lineStart, imageWidth, lineLast);

				Array pixels = (Array)pixelblock3.PixelData[_rasterBandIdx];

				var blockDefinition = new RasterBlockDefinition
				{
					TopLeftCol = 0,
					TopLeftRow = lineStart,
					Width = imageWidth,
					Height = lineLast
				};

				yield return new KeyValuePair<RasterBlockDefinition, Array>(
					blockDefinition, pixels);

				linesLeft -= lineLast;
				lineStart += lineLast;

				Marshal.ReleaseComObject(pixelblock3);
			}

			Marshal.ReleaseComObject(pixelBlockCursor);
		}

		#endregion
	}
}
