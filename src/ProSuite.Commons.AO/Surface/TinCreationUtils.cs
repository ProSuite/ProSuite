using System;
using System.Collections.Generic;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.GeoDatabaseExtensions;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using Path = System.IO.Path;

namespace ProSuite.Commons.AO.Surface
{
	public static class TinCreationUtils
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public static int SuggestMaxTinPointCount([CanBeNull] string envVarMaxPointCount)
		{
			// Default value: be very conservative in non-large-address-aware 32-bit process
			// -> Unit test runner is not large-address-aware
			int maxTinPointCount;
			if (SystemUtils.Is64BitProcess)
			{
				maxTinPointCount = 20000000;
			}
			else
			{
				if (SystemUtils.IsLargeAddressAware)
				{
					maxTinPointCount = 7500000;
				}
				else
				{
					maxTinPointCount = 2000000;
				}
			}

			if (string.IsNullOrEmpty(envVarMaxPointCount))
			{
				return maxTinPointCount;
			}

			string maxTinPoints =
				Environment.GetEnvironmentVariable(envVarMaxPointCount);

			if (! string.IsNullOrEmpty(maxTinPoints))
			{
				if (! int.TryParse(maxTinPoints, out maxTinPointCount))
				{
					_msg.WarnFormat("Invalid value for environment variable {0}",
					                envVarMaxPointCount);
				}

				_msg.InfoFormat(
					"Estimating TIN size for max point count {0} (defined by environment variable {1})",
					maxTinPointCount, envVarMaxPointCount);
			}
			else
			{
				_msg.InfoFormat(
					"Estimating TIN size for max point count {0} (environment variable {1} is not defined)",
					maxTinPointCount, envVarMaxPointCount);
			}

			return maxTinPointCount;
		}

		/// <summary>
		/// Returns envelopes that subdivide the provided area of interest into areas that contain
		/// no more points than maxPointCount.
		/// </summary>
		/// <param name="terrain"></param>
		/// <param name="areaOfInterest"></param>
		/// <param name="maxPointCount"></param>
		/// <param name="resolution"></param>
		/// <returns></returns>
		public static IList<IEnvelope> GetAreaOfInterestSubdivisions(
			[NotNull] ITerrain terrain,
			[NotNull] IEnvelope areaOfInterest,
			int maxPointCount, double resolution)
		{
			Assert.True(
				SpatialReferenceUtils.AreEqual(
					terrain.SpatialReference, areaOfInterest.SpatialReference,
					comparePrecisionAndTolerance: false,
					compareVerticalCoordinateSystems: false),
				"AOI is expected in target spatial reference");

			var result = new List<IEnvelope>();

			Func<IEnvelope, double> estimatePointsFunc =
				aoi => terrain.GetPointCount(aoi, resolution);

			double tolerance = GeometryUtils.GetXyTolerance(areaOfInterest);
			double minArea = tolerance * tolerance * 100 * 100;

			AddSubdivisions(estimatePointsFunc, areaOfInterest, maxPointCount, minArea,
			                result);

			return result;
		}

		public static string SaveTinToScratchWorkspace([NotNull] ITin inMemoryTin)
		{
			string scratchDir =
				Assert.NotNull(
					Path.GetDirectoryName(
						WorkspaceUtils.CreateScratchWorkspace().PathName));

			string tinPath = Path.Combine(scratchDir, Path.GetRandomFileName());

			object overwrite = true;

			((ITinEdit) inMemoryTin).SaveAs(tinPath, ref overwrite);

			return tinPath;
		}

		/// <summary>
		/// Returns envelopes that subdivide the provided area of interest into areas that contain
		/// no more points than maxPointCount.
		/// </summary>
		/// <param name="terrainDef"></param>
		/// <param name="areaOfInterest"></param>
		/// <param name="maxPointCount"></param>
		/// <returns></returns>
		public static IList<IEnvelope> GetAreaOfInterestSubdivisions(
			[NotNull] SimpleTerrain terrainDef,
			[NotNull] IEnvelope areaOfInterest,
			int maxPointCount)
		{
			var result = new List<IEnvelope>();

			Func<IEnvelope, double> estimatePointsFunc =
				aoi => terrainDef.GetPointCount(aoi);

			const int minimumPointCount = 1000;
			double minArea = 1 / terrainDef.PointDensity * minimumPointCount;

			AddSubdivisions(estimatePointsFunc, areaOfInterest, maxPointCount, minArea,
			                result);

			return result;
		}

		private static void AddSubdivisions(
			[NotNull] Func<IEnvelope, double> estimatePointsFunc,
			[NotNull] IEnvelope areaOfInterest,
			int maxTinPointCount,
			double minArea,
			[NotNull] IList<IEnvelope> resultList)
		{
			double pointCount = estimatePointsFunc(areaOfInterest);

			if (pointCount <= maxTinPointCount)
			{
				_msg.DebugFormat("Expected point count for {0}: {1}",
				                 GeometryUtils.Format(areaOfInterest), pointCount);

				resultList.Add(areaOfInterest);
			}
			else if (areaOfInterest.Width * areaOfInterest.Height < minArea)
			{
				_msg.DebugFormat(
					"Subdivision area is smaller than min area {0} ({1} points).",
					GeometryUtils.Format(areaOfInterest), pointCount);

				resultList.Add(areaOfInterest);
			}
			else
			{
				_msg.DebugFormat(
					"Expected point count for {0}: {1} exceeds the maximum tin points ({2})",
					GeometryUtils.Format(areaOfInterest), pointCount, maxTinPointCount);

				foreach (IEnvelope half in CutInHalf(areaOfInterest))
				{
					AddSubdivisions(estimatePointsFunc, half, maxTinPointCount, minArea,
					                resultList);
				}
			}
		}

		private static IEnumerable<IEnvelope> CutInHalf(IEnvelope envelope)
		{
			IEnvelope result1 = GeometryFactory.Clone(envelope);
			IEnvelope result2 = GeometryFactory.Clone(envelope);

			// cut longer edge in half
			if (envelope.Height > envelope.Width)
			{
				double cutY = (envelope.YMin + envelope.YMax) / 2;

				result1.YMax = cutY;
				result2.YMin = cutY;
			}
			else
			{
				double cutX = (envelope.XMin + envelope.XMax) / 2;

				result1.XMax = cutX;
				result2.XMin = cutX;
			}

			result1.SnapToSpatialReference();
			result2.SnapToSpatialReference();

			yield return result1;
			yield return result2;
		}
	}
}
