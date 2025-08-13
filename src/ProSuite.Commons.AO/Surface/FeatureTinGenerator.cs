using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Diagnostics;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Surface
{
	public class FeatureTinGenerator : ITinGenerator
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly SimpleTerrain _terrainDef;
		private readonly double? _tinBufferDistance;

		public FeatureTinGenerator([NotNull] SimpleTerrain simpleTerrain,
		                           double? tinBufferDistance)
		{
			_terrainDef = simpleTerrain;
			_tinBufferDistance = tinBufferDistance;
		}

		/// <summary>
		/// Whether the TIN should be generated using the constrained delaunay triangulation, i.e. the
		/// breaklines should not be densified with Steiner points.
		/// </summary>
		public bool UseConstrainedDelaunayTriangulation { get; set; }

		/// <summary>
		/// Whether the created TIN may not fully cover the requested area at the boundary of the input
		/// source data. If false, an exception is raised when the requested area exceeds the input data
		/// extent.
		/// </summary>
		public bool AllowIncompleteInterpolationDomainAtBoundary { get; set; }

		/// <summary>
		/// Method that determines whether a given envelope already exceeds the data area of the source data
		/// and hence no further expansion of the extent should be performed, even if the resulting TIN does
		/// not cover the requested extent.
		/// </summary>
		public Predicate<IEnvelope> ExceedsDataArea { get; set; }

		/// <summary>
		/// Method that will try to provide an envelope that should guarantee complete cover of the requested AOI
		/// by the created TIN's interpolation domain. If an envelope is returned it will be used to select features
		/// that are added to the TIN.
		/// </summary>
		public Func<IEnvelope, IEnvelope> TryProvideGuaranteedCover { get; set; }

		/// <summary>
		/// An optional coordinate transformer that transforms the source data to the target spatial reference
		/// before it is added to the TIN.
		/// </summary>
		public ICoordinateTransformer CoordinateTransformer { protected get; set; }

		/// <summary>
		/// Suggests subdivisions of the area of interest that are not expected to exceed the specified
		/// maximum points in the source data.
		/// </summary>
		/// <param name="areaOfInterest">The AOI in the target spatial reference.</param>
		/// <param name="maxTinPointCount">The maximum expected number of points per subdivision.</param>
		/// <returns></returns>
		public IList<IEnvelope> SuggestSubdivisions(IEnvelope areaOfInterest,
		                                            int maxTinPointCount)
		{
			// In the fall-back implementation the AOI is quadrupled, the more subtle approach enlarges it by max 8 * buffer width
			var enlargementFactor = 4d;
			if (TryProvideGuaranteedCover != null)
			{
				// Could be wrong if TryProvideGuaranteedCover fails to provide a result. Consider using TryProvideGuaranteedCover 
				// for each subdivision's enlargement.
				enlargementFactor = 2d;
			}

			maxTinPointCount = (int) (maxTinPointCount / enlargementFactor);

			IList<IEnvelope> resultSubdivisions =
				TinCreationUtils.GetAreaOfInterestSubdivisions(
					_terrainDef, areaOfInterest, maxTinPointCount);

			return resultSubdivisions;
		}

		/// <summary>
		/// Generates a TIN that covers the specified area of interest unless the TIN is at the boundary of the terrain
		/// and AllowIncompleteInterpolationDomainAtBoundary is true.
		/// ASSUMPTION: Large empty areas (i.e. lakes) are surrounded by breaklines. This is only relevant if TryProvideGuaranteedCover
		/// cannot provide an envelope whose data does not produce a TIN that covers the entire requested areaOfInterest.
		/// </summary>
		/// <param name="areaOfInterest">The AOI in the target spatial reference.</param>
		/// <param name="trackCancel"></param>
		/// <returns></returns>
		public ITin GenerateTin(IEnvelope areaOfInterest,
		                        ITrackCancel trackCancel = null)
		{
			// Read relevant features + some margin
			// Ensure that there are points along the boundary (outside the aoi) to ensure a correct surface despite only reading an extent
			IEnvelope aoiInSourceSpatialRef =
				CoordinateTransformer?.ProjectBack(areaOfInterest) ??
				GeometryFactory.Clone(areaOfInterest);

			Assert.True(
				SpatialReferenceUtils.AreEqual(aoiInSourceSpatialRef.SpatialReference,
				                               _terrainDef.SpatialReference),
				"Spatial reference of aoi does not conform to terrain data.");

			IEnvelope expandedSearchArea = GetExpandedAoi(aoiInSourceSpatialRef);

			Stopwatch watch =
				_msg.DebugStartTiming("Creating TIN for {0}",
				                      GeometryUtils.Format(areaOfInterest));

			var memoryUsageInfo = new MemoryUsageInfo();

			IEnvelope guaranteedCover =
				TryProvideGuaranteedCover?.Invoke(aoiInSourceSpatialRef);

			if (guaranteedCover != null)
			{
				expandedSearchArea = guaranteedCover;
			}

			ITin result = GenerateTin(_terrainDef.DataSources, aoiInSourceSpatialRef,
			                          expandedSearchArea, trackCancel);

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat(
					"Created TIN and stored for diagnostic purposes in {0}.",
					TinCreationUtils.SaveTinToScratchWorkspace(result));
			}

			_msg.DebugStopTiming(watch, "Created tin with {0} nodes. {1}",
			                     result.DataNodeCount,
			                     memoryUsageInfo.Refresh());

			return result;
		}

		[NotNull]
		private ITin GenerateTin(
			[NotNull] IList<SimpleTerrainDataSource> terrainDataSources,
			[NotNull] IEnvelope aoiInSourceSpatialRef,
			[NotNull] IEnvelope searchAreaInSourceSpatialRef,
			[CanBeNull] ITrackCancel trackCancel)
		{
			ITinEdit tinEdit =
				CreateNewTin(searchAreaInSourceSpatialRef,
				             UseConstrainedDelaunayTriangulation);

			AddFeaturesToTin(tinEdit, terrainDataSources,
			                 searchAreaInSourceSpatialRef);

			IPolygon interpolationDomain =
				GetInterpolationDomainInSourceSpatialRef(tinEdit);

			while (interpolationDomain == null || interpolationDomain.IsEmpty ||
			       ! GeometryUtils.Contains(interpolationDomain, aoiInSourceSpatialRef))
			{
				// Auto-grow to detect if we're in a lake. This could bring us close to the 4GB limit...
				if (trackCancel != null && ! trackCancel.Continue())
				{
					return (ITin) tinEdit;
				}

				searchAreaInSourceSpatialRef = TryExpandTin(
					searchAreaInSourceSpatialRef, tinEdit, aoiInSourceSpatialRef,
					terrainDataSources);

				if (searchAreaInSourceSpatialRef == null)
				{
					if (AllowIncompleteInterpolationDomainAtBoundary)
					{
						return (ITin) tinEdit;
					}

					// The terrain boundary has been exceeded and additional searching won't find more data
					throw new InvalidDataException(
						string.Format(
							"The extent {0} is not sufficiently covered by terrain data",
							GeometryUtils.ToString(aoiInSourceSpatialRef, true)));
				}

				interpolationDomain = GetInterpolationDomainInSourceSpatialRef(tinEdit);
			}

			return (ITin) tinEdit;
		}

		[CanBeNull]
		private IEnvelope TryExpandTin(
			[NotNull] IEnvelope currentSearchArea,
			[NotNull] ITinEdit tinEdit,
			[NotNull] IEnvelope areaOfInterest,
			[NotNull] IEnumerable<SimpleTerrainDataSource> terrainDataSources)
		{
			IPolygon interpolationDomain =
				GetInterpolationDomainInSourceSpatialRef(tinEdit);

			IEnvelope areaToSearch = EnlargeSearchArea(currentSearchArea, areaOfInterest,
			                                           interpolationDomain,
			                                           _terrainDef.Extent);

			_msg.DebugFormat("Enlarged search area: {0}",
			                 GeometryUtils.ToString(areaToSearch, true));

			if (areaToSearch == null || areaToSearch.IsEmpty)
			{
				return null;
			}

			var additionalArea =
				(IPolygon) IntersectionUtils.Difference(
					GeometryFactory.CreatePolygon(areaToSearch),
					GeometryFactory.CreatePolygon(currentSearchArea));

			// NOTE: Large empty areas are currently expected to be surrounded by closed breaklines
			// -> Implement alternative (feature by feature) addition to tin
			IEnumerable<SimpleTerrainDataSource> polycurveSources =
				terrainDataSources.Where(
					source =>
						source.TinSurfaceType != esriTinSurfaceType.esriTinMassPoint);

			AddFeaturesToTin(tinEdit, polycurveSources, additionalArea);

			return areaToSearch;
		}

		private ITinEdit CreateNewTin([NotNull] IEnvelope expandedAoiInSourceSpatialRef,
		                              bool constrainedDelaunay = false)
		{
			IEnvelope expectedEnvInTargetSpatialRef =
				CoordinateTransformer?.Project(expandedAoiInSourceSpatialRef) ??
				expandedAoiInSourceSpatialRef;

			ISpatialReference sr = CoordinateTransformer != null
				                       ? CoordinateTransformer.TargetSpatialReference
				                       : _terrainDef.SpatialReference;

			ITinEdit tinEdit = new TinClass();
			tinEdit.InitNew(expectedEnvInTargetSpatialRef);

			tinEdit.SetSpatialReference(sr);

			if (constrainedDelaunay)
			{
				((ITinEdit2) tinEdit).SetToConstrainedDelaunay();
			}

			return tinEdit;
		}

		private IEnvelope GetExpandedAoi(IEnvelope areaOfInterest)
		{
			IEnvelope result = GeometryFactory.Clone(areaOfInterest);

			double expandX = _tinBufferDistance ?? areaOfInterest.Width / 2;
			double expandY = _tinBufferDistance ?? areaOfInterest.Height / 2;

			result.Expand(expandX, expandY, false);

			return result;
		}

		/// <summary>
		/// Returns an enlarged version of the provided aoi by checking in which directions the area with missing
		/// data touches the area of interest, i.e. in which directions a further search would (hopefully) provide more data.
		/// </summary>
		/// <param name="searchedExtent"></param>
		/// <param name="areaOfInterest"></param>
		/// <param name="currentInterpolationDomain"></param>
		/// <param name="maxExtent"></param>
		/// <returns>The enlarged extent or null, if the search area exceeds the terrain extent.</returns>
		[CanBeNull]
		private IEnvelope EnlargeSearchArea(
			[NotNull] IEnvelope searchedExtent,
			[NotNull] IEnvelope areaOfInterest,
			[CanBeNull] IPolygon currentInterpolationDomain,
			[NotNull] IEnvelope maxExtent)
		{
			IEnvelope result = GeometryFactory.Clone(searchedExtent);

			_msg.VerboseDebug(
				() =>
					$"Enlarging searched extent: {Environment.NewLine}{GeometryUtils.ToString(searchedExtent)} {Environment.NewLine}Current TIN: {Environment.NewLine}{GeometryUtils.ToString(currentInterpolationDomain)} {Environment.NewLine}for AOI {Environment.NewLine}{GeometryUtils.ToString(areaOfInterest)}");

			if (ExceedsMaxExtent(searchedExtent, maxExtent))
			{
				_msg.VerboseDebug(
					() => $"Max extent {GeometryUtils.ToString(maxExtent)} is exceeded");

				return null;
			}

			if (currentInterpolationDomain == null || currentInterpolationDomain.IsEmpty)
			{
				// double the size
				result.Expand(2.0, 2.0, true);
			}
			else
			{
				if (GeometryUtils.Disjoint(areaOfInterest, currentInterpolationDomain))
				{
					// we're completely outside the current tin -> enlarge only at the far end where the current search extent does not
					// intersect the interpolation domain...
					_msg.Debug(
						"EnlargeSearchArea: Using previously searched extent as area of interest.");

					areaOfInterest = searchedExtent;
				}

				if (! TryEnlargeTowardsNoDataWhereEntireEdgeIsOutside(areaOfInterest,
					    currentInterpolationDomain,
					    result))
				{
					EnlargeTowardsNoDataWhereEdgeIsPartiallyOutside(
						result, areaOfInterest,
						currentInterpolationDomain);
				}
			}

			return result;
		}

		private bool ExceedsMaxExtent(IEnvelope testEnvelope, IEnvelope maxExtent)
		{
			if (testEnvelope.XMin < maxExtent.XMin)
			{
				// already searched outside the terrain extent, give up
				return true;
			}

			if (testEnvelope.YMin < maxExtent.YMin)
			{
				// already searched outside the terrain extent, give up
				return true;
			}

			if (testEnvelope.XMax > maxExtent.XMax)
			{
				// already searched outside the terrain extent, give up
				return true;
			}

			if (testEnvelope.YMax > maxExtent.YMax)
			{
				// already searched outside the terrain extent, give up
				return true;
			}

			if (ExceedsDataArea != null && ExceedsDataArea(testEnvelope))
			{
				return true;
			}

			return false;
		}

		private static void EnlargeTowardsNoDataWhereEdgeIsPartiallyOutside(
			[NotNull] IEnvelope extentToEnlarge,
			[NotNull] IEnvelope aoi,
			[NotNull] IPolygon currentInterpolationDomain)
		{
			IGeometry areaWithMissingData =
				IntersectionUtils.Difference(
					GeometryFactory.CreatePolygon(aoi), currentInterpolationDomain);

			if (areaWithMissingData.IsEmpty)
			{
				// There is no difference, no data is missing:
				extentToEnlarge.SetEmpty();
				return;
			}

			IEnvelope missingDataEnvelope = areaWithMissingData.Envelope;

			double tolerance = GeometryUtils.GetXyTolerance(areaWithMissingData);

			if (MathUtils.AreEqual(missingDataEnvelope.XMin, aoi.XMin, tolerance))
			{
				// enlarge to left
				extentToEnlarge.XMin -= aoi.Width / 2;
			}

			if (MathUtils.AreEqual(missingDataEnvelope.YMin, aoi.YMin, tolerance))
			{
				// enlarge to south
				extentToEnlarge.YMin -= aoi.Height / 2;
			}

			if (MathUtils.AreEqual(missingDataEnvelope.XMax, aoi.XMax, tolerance))
			{
				// enlarge to right
				extentToEnlarge.XMax += aoi.Width / 2;
			}

			if (MathUtils.AreEqual(missingDataEnvelope.YMax, aoi.YMax, tolerance))
			{
				// enlarge to north
				extentToEnlarge.YMax += aoi.Height;
			}
		}

		private static bool TryEnlargeTowardsNoDataWhereEntireEdgeIsOutside(
			IEnvelope aoi, IPolygon currentInterpolationDomain,
			IEnvelope extentToEnlarge)
		{
			var result = false;

			IGeometry areaWithMissingData =
				IntersectionUtils.Difference(
					GeometryFactory.CreatePolygon(aoi), currentInterpolationDomain);

			if (areaWithMissingData.IsEmpty)
			{
				return false;
			}

			IEnvelope missingDataEnvelope = areaWithMissingData.Envelope;

			double tolerance = GeometryUtils.GetXyTolerance(areaWithMissingData);

			// if top left and lower left is uncovered
			if (MathUtils.AreEqual(missingDataEnvelope.XMin, aoi.XMin, tolerance) &&
			    MathUtils.AreEqual(missingDataEnvelope.YMin, aoi.YMin, tolerance) &&
			    MathUtils.AreEqual(missingDataEnvelope.YMax, aoi.YMax, tolerance))
			{
				// enlarge to left
				extentToEnlarge.XMin -= aoi.Width / 2;
				result = true;
			}

			// if lower left and lower right is uncovered
			if (MathUtils.AreEqual(missingDataEnvelope.YMin, aoi.YMin, tolerance) &&
			    MathUtils.AreEqual(missingDataEnvelope.XMin, aoi.XMin, tolerance) &&
			    MathUtils.AreEqual(missingDataEnvelope.XMax, aoi.XMax, tolerance))
			{
				// enlarge to south
				extentToEnlarge.YMin -= aoi.Height / 2;
				result = true;
			}

			// if lower right and upper right is uncovered
			if (MathUtils.AreEqual(missingDataEnvelope.XMax, aoi.XMax, tolerance) &&
			    MathUtils.AreEqual(missingDataEnvelope.YMin, aoi.YMin, tolerance) &&
			    MathUtils.AreEqual(missingDataEnvelope.YMax, aoi.YMax, tolerance))
			{
				// enlarge to right
				extentToEnlarge.XMax += aoi.Width / 2;
				result = true;
			}

			// if upper left and upper right is uncovered
			if (MathUtils.AreEqual(missingDataEnvelope.YMax, aoi.YMax, tolerance) &&
			    MathUtils.AreEqual(missingDataEnvelope.XMin, aoi.XMin, tolerance) &&
			    MathUtils.AreEqual(missingDataEnvelope.XMax, aoi.XMax, tolerance))
			{
				// enlarge to north
				extentToEnlarge.YMax += aoi.Height / 2;
				result = true;
			}

			return result;
		}

		private void AddFeaturesToTin(
			[NotNull] ITinEdit tin,
			[NotNull] IEnumerable<SimpleTerrainDataSource> terrainDataSources,
			[NotNull] IGeometry inExtent)
		{
			foreach (SimpleTerrainDataSource terrainDataSource in terrainDataSources)
			{
				IFeatureClass featureClass = terrainDataSource.FeatureClass;

				string className = DatasetUtils.GetName(featureClass);

				_msg.DebugFormat("Adding {0} data to TIN in extent {1}", className,
				                 GeometryUtils.ToString(inExtent.Envelope, true));

				Assert.True(
					CoordinateTransformer == null ||
					SpatialReferenceUtils.AreEqual(
						DatasetUtils.GetSpatialReference(featureClass),
						CoordinateTransformer.SourceSpatialReference, false, true),
					"Unexpected source data coordinate system.");

				// Avoid huge lakes completely outside AOI to become part of TIN only
				// because their env intersects:
				esriSpatialRelEnum spatialRel =
					featureClass.ShapeType == esriGeometryType.esriGeometryPolygon ||
					featureClass.ShapeType == esriGeometryType.esriGeometryPolyline
						? esriSpatialRelEnum.esriSpatialRelIntersects
						: esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;

				// What might remain problematic are huge breaklines that cross the AOI boundary
				// and therefore define the TIN across vast areas for which points have not been
				// read resulting in missing points. Possibly they could be converted to polygons,
				// clipped along the boundary and reverted into polylines. This would be required
				// if there were large areas without points outside the closed polylines.
				IQueryFilter filter = GdbQueryUtils.CreateSpatialFilter(
					featureClass, inExtent, spatialRel, true);

				filter.WhereClause = terrainDataSource.WhereClause;

				esriTinSurfaceType surfaceType = terrainDataSource.TinSurfaceType;

				try
				{
					AddFeaturesToTin(tin, surfaceType, featureClass, filter, inExtent);
				}
				catch (COMException comException)
				{
					const int errorCodeOutOfMemory = -2147219442;

					if (comException.ErrorCode == errorCodeOutOfMemory)
					{
						Marshal.ReleaseComObject(tin);
					}

					throw;
				}
			}
		}

		protected virtual void AddFeaturesToTin(ITinEdit tin, esriTinSurfaceType surfaceType,
		                                        IFeatureClass featureClass, IQueryFilter filter,
		                                        IGeometry inExtent)
		{
			// NOTE: According to the documentation, useShapeZ == false means that the
			//       M-value should be used for height instead of Z -> always use shape's Z
			object useShapeZ = true;
			IGeometry shape = null;

			bool isClipping =
				(surfaceType == esriTinSurfaceType.esriTinHardClip ||
				 surfaceType == esriTinSurfaceType.esriTinSoftClip) &&
				featureClass.ShapeType == esriGeometryType.esriGeometryPolygon;

			foreach (IFeature feature in GdbQueryUtils.GetFeatures(
				         featureClass, filter, recycle: true))
			{
				shape = feature.Shape;

				if (shape.IsEmpty)
				{
					continue;
				}

				if (isClipping)
				{
					IPolyline polyline = GeometryFactory.CreatePolyline(shape);

					if (GeometryUtils.Disjoint(polyline, inExtent))
					{
						Marshal.ReleaseComObject(polyline);

						continue;
					}
				}

				try
				{
					CoordinateTransformer?.Transform(shape);

					tin.AddShapeZ(shape, surfaceType, 0, ref useShapeZ);
				}
				catch (Exception e)
				{
					_msg.Debug(
						$"Error adding data from {GdbObjectUtils.ToString(feature)} to TIN " +
						$"(having {((ITin) tin).DataNodeCount} points)", e);
					throw;
				}
			}

			if (shape != null)
			{
				Marshal.ReleaseComObject(shape);
			}
		}

		private IPolygon GetInterpolationDomainInSourceSpatialRef(ITinEdit tin)
		{
			tin.Refresh();

			IPolygon interpolationDomain = ((IFunctionalSurface) tin).Domain;

			if (interpolationDomain == null)
			{
				return null;
			}

			// Project back to source SR:
			return CoordinateTransformer?.ProjectBack(interpolationDomain) ??
			       interpolationDomain;
		}
	}
}
