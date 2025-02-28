using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	public class SubcurveFilter
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IExtentProvider _extentProvider;

		private ReshapeCurveFilterOptions _filterOptions;
		private bool _useMinimalTolerance;

		private List<IEnvelope> _currentlyVisibleExtents;
		private IPolygon _mustBeWithinSourceBuffer;
		private IPolygon _targetUnionPoly;
		private IPolyline _sourceTargetPolyUnionBoundary;

		private IGeometry _currentSourceGeometry;

		public SubcurveFilter([NotNull] IExtentProvider extentProvider)
		{
			_extentProvider = extentProvider;
		}

		[CanBeNull]
		public IPolyline ExclusionOutsideSourceBufferLine { get; private set; }

		public SubcurveFilter PrepareFilter(
			[NotNull] IList<IPolyline> preprocessedSourceLines,
			[NotNull] IList<IGeometry> targetGeometries,
			bool useMinimalTolerance,
			[NotNull] ReshapeCurveFilterOptions filterOptions)
		{
			_useMinimalTolerance = useMinimalTolerance;
			_filterOptions = filterOptions;

			ReleaseFilterObjects();

			if (filterOptions.OnlyInVisibleExtent)
			{
				Assert.NotNull(_extentProvider);
				_currentlyVisibleExtents = new List<IEnvelope>();

				// add the lens window extents
				_currentlyVisibleExtents.AddRange(
					_extentProvider.GetVisibleLensWindowExtents());

				// plus the main map window
				_currentlyVisibleExtents.Add(_extentProvider.GetCurrentExtent());
			}

			if (filterOptions.ExcludeOutsideTolerance)
			{
				// NOTE: Buffer lines / outlines -> otherwise we miss lines for individual reshapes
				//		 and clip on extent (pre-process) before buffering to improve performance

				var sourceOutline =
					(IPolyline) GeometryUtils.UnionGeometries(preprocessedSourceLines);

				const int logInfoPointCountThreshold = 10000;
				var bufferNotifications = new NotificationCollection();

				if (AdjustUtils.TryBuffer(sourceOutline,
				                          filterOptions.ExcludeTolerance,
				                          logInfoPointCountThreshold,
				                          "Calculating reshape line tolerance buffer...",
				                          bufferNotifications,
				                          out _mustBeWithinSourceBuffer))
				{
					ExclusionOutsideSourceBufferLine =
						GeometryFactory.CreatePolyline(
							Assert.NotNull(_mustBeWithinSourceBuffer));
				}
				else
				{
					_msg.WarnFormat(
						"Unable to calculate reshape line tolerance buffer: {0}",
						bufferNotifications.Concatenate(". "));
				}

				Marshal.ReleaseComObject(sourceOutline);
			}

			if (filterOptions.ExcludeResultingInOverlaps)
			{
				_targetUnionPoly = ReshapeUtils.CreateUnionPolygon(
					targetGeometries, _useMinimalTolerance);
			}

			return this;
		}

		public void PrepareForSource(IGeometry sourceGeometry)
		{
			// Always clone _currentSourceGeometry because it could be projected
			if (_currentSourceGeometry != null)
			{
				Marshal.ReleaseComObject(_currentSourceGeometry);
			}

			_currentSourceGeometry = GeometryFactory.Clone(sourceGeometry);

			if (_useMinimalTolerance)
			{
				// Setting minimum tolerance is important, otherwise the union might not 
				// be consistent with the difference lines
				GeometryUtils.SetMinimumXyTolerance(_currentSourceGeometry);
			}

			if (_sourceTargetPolyUnionBoundary != null)
			{
				Marshal.ReleaseComObject(_sourceTargetPolyUnionBoundary);
			}

			_sourceTargetPolyUnionBoundary = CreateSourceTargetPolyUnionBoundary(
				_currentSourceGeometry, _targetUnionPoly);
		}

		public bool IsExcluded(CutSubcurve cutSubcurve)
		{
			var highLevelPath =
				(IPolyline) GeometryUtils.GetHighLevelGeometry(cutSubcurve.Path, true);

			if (_currentlyVisibleExtents != null)
			{
				if (! FullyWithinAnyExtent(highLevelPath, cutSubcurve,
				                           _currentlyVisibleExtents))
				{
					return true;
				}
			}

			if (_filterOptions.OnlyResultingInRemovals)
			{
				var mustInteriorIntersectPoly = _currentSourceGeometry as IPolygon;

				if (mustInteriorIntersectPoly != null &&
				    ! GeometryUtils.InteriorIntersects(
					    mustInteriorIntersectPoly, highLevelPath))
				{
					return true;
				}
			}

			if (_mustBeWithinSourceBuffer != null)
			{
				if (! GeometryUtils.Contains(_mustBeWithinSourceBuffer, highLevelPath))
				{
					return true;
				}
			}

			if (_filterOptions.ExcludeResultingInOverlaps &&
			    _targetUnionPoly != null && _sourceTargetPolyUnionBoundary != null)
			{
				// Avoid Error in Disjoint/Contains: Spatial References of provided geometries are not consistent.
				// This happens sometimes, probably due to map SR different to data SR. 
				// But also with minimum tolerance: the subcurves are calculated in minimum tolerance
				GeometryUtils.EnsureSpatialReference(_targetUnionPoly,
				                                     highLevelPath.SpatialReference);

				GeometryUtils.EnsureSpatialReference(_sourceTargetPolyUnionBoundary,
				                                     highLevelPath.SpatialReference);

				if (ResultsInOverlaps(highLevelPath, _targetUnionPoly,
				                      _sourceTargetPolyUnionBoundary))
				{
					return true;
				}
			}

			Marshal.ReleaseComObject(highLevelPath);

			return false;
		}

		[CanBeNull]
		public IEnvelope GetBufferLineRefreshArea()
		{
			return ExclusionOutsideSourceBufferLine?.Envelope;
		}

		public void ReleaseBufferLine()
		{
			if (ExclusionOutsideSourceBufferLine != null)
			{
				Marshal.ReleaseComObject(ExclusionOutsideSourceBufferLine);
				ExclusionOutsideSourceBufferLine = null;
			}
		}

		private static bool FullyWithinAnyExtent(IPolyline highLevelPath,
		                                         CutSubcurve cutSubcurve,
		                                         IEnumerable<IEnvelope> extents)
		{
			foreach (IEnvelope extent in extents)
			{
				if (extent.IsEmpty)
				{
					continue;
				}

				if (GeometryUtils.Contains(extent, highLevelPath))
				{
					if (! cutSubcurve.CanReshape &&
					    (GeometryUtils.Touches(extent, highLevelPath.FromPoint) ||
					     GeometryUtils.Touches(extent, highLevelPath.ToPoint)))
					{
						// it was cut off by the extent
						continue;
					}

					return true;
				}
			}

			return false;
		}

		private static bool ResultsInOverlaps(
			[NotNull] IPolyline highLevelSubcurve,
			[CanBeNull] IPolygon targetUnionPoly,
			[CanBeNull] IPolyline sourceTargetPolyUnionBoundary)
		{
			if (targetUnionPoly != null)
			{
				if (GeometryUtils.InteriorIntersects(targetUnionPoly, highLevelSubcurve))
				{
					return true;
				}
			}

			if (sourceTargetPolyUnionBoundary != null)
			{
				// The subvcurve must be completely covered by the sourceTargetPolyUnionBoundary.
				// NOTE: Even when it traverses the from/to point of the closed polyline, it is contained!
				// (see GeometryUtilsTest.LearningTestContainsClosedPolyline())
				if (GeometryUtils.Contains(sourceTargetPolyUnionBoundary,
				                           highLevelSubcurve))
				{
					return true;
				}
			}

			return false;
		}

		[CanBeNull]
		private static IPolyline CreateSourceTargetPolyUnionBoundary(
			IGeometry sourceGeometry, IPolygon targetUnionPoly)
		{
			if (targetUnionPoly == null)
			{
				return null;
			}

			var sourcePolygon = sourceGeometry as IPolygon;

			if (sourcePolygon == null)
			{
				return null;
			}

			var fullUnionPolygon =
				(IPolygon) GeometryUtils.Union(targetUnionPoly, sourcePolygon);

			List<IRing> innerRings;
			IPolyline fullUnionOuterRings = GetOutermostBoundary(fullUnionPolygon,
			                                                     out innerRings);

			// Special case: if there are target polygons (or polygon parts) inside a full-union island, 
			// these islands should be excluded, i.e. added to the result, to block subcurves that would fill this island
			if (innerRings.Count > 0)
			{
				IGeometryCollection polylineToAdd = GetRingsToProtect(
					targetUnionPoly, innerRings,
					fullUnionOuterRings);

				if (polylineToAdd != null)
				{
					((IGeometryCollection) fullUnionOuterRings).AddGeometryCollection(
						polylineToAdd);
				}
			}

			Marshal.ReleaseComObject(fullUnionPolygon);

			return fullUnionOuterRings;
		}

		private static IPolyline GetOutermostBoundary(IPolygon fullUnionPolygon,
		                                              out List<IRing> innerRings)
		{
			var resultRings = new List<IPath>();
			innerRings = new List<IRing>();
			foreach (IRing ring in GeometryUtils.GetRings(fullUnionPolygon))
			{
				if (ring.IsExterior)
				{
					resultRings.Add(ring);
				}
				else
				{
					innerRings.Add(ring);
				}
			}

			IPolyline fullUnionOuterRings = GeometryFactory.CreatePolyline(resultRings);

			// TOP-4915: Must also handle boundary loops between source and target
			foreach (IPolygon boundaryLoop in BoundaryLoopUtils.GetBoundaryLoops(
				         fullUnionPolygon, GeometryUtils.GetXyTolerance(fullUnionPolygon)))
			{
				// remove from result, add to rings
				IPolyline boundaryLoopLine = GeometryFactory.CreatePolyline(boundaryLoop);

				fullUnionOuterRings =
					(IPolyline) IntersectionUtils.Difference(
						fullUnionOuterRings, boundaryLoopLine);

				innerRings.AddRange(GeometryUtils.GetRings(boundaryLoop));
			}

			return fullUnionOuterRings;
		}

		[CanBeNull]
		private static IGeometryCollection GetRingsToProtect(
			[NotNull] IPolygon targetUnionPoly,
			[NotNull] List<IRing> innerRings,
			[NotNull] IPolyline fullUnionOuterRings)
		{
			IPolyline fullUnionInnerRings =
				GeometryFactory.CreatePolyline(innerRings.ConvertAll(r => (IPath) r));

			var pathsToAdd = new List<IPath>();

			foreach (IPath targetUnionPart in GeometryUtils.GetPaths(targetUnionPoly))
			{
				IGeometry highLevelTargetUnionPart =
					GeometryUtils.GetHighLevelGeometry(targetUnionPart, true);

				if (GeometryUtils.Intersects(highLevelTargetUnionPart,
				                             fullUnionOuterRings))
				{
					continue;
				}

				foreach (IPath fullUnionInnerRing in GeometryUtils.GetPaths(
					         fullUnionInnerRings))
				{
					IGeometry highLevelFullUnionInnerRing =
						GeometryUtils.GetHighLevelGeometry(fullUnionInnerRing, true);

					if (GeometryUtils.Intersects(highLevelTargetUnionPart,
					                             highLevelFullUnionInnerRing))
					{
						// this inner ring needs protection from being filled
						pathsToAdd.Add(fullUnionInnerRing);
					}
				}
			}

			return pathsToAdd.Count == 0
				       ? null
				       : (IGeometryCollection) GeometryFactory.CreatePolyline(pathsToAdd);
		}

		private void ReleaseFilterObjects()
		{
			_currentlyVisibleExtents = null;

			if (_mustBeWithinSourceBuffer != null)
			{
				Marshal.ReleaseComObject(_mustBeWithinSourceBuffer);
				_mustBeWithinSourceBuffer = null;
			}

			ReleaseBufferLine();

			if (_targetUnionPoly != null)
			{
				Marshal.ReleaseComObject(_targetUnionPoly);
				_targetUnionPoly = null;
			}

			if (_sourceTargetPolyUnionBoundary != null)
			{
				Marshal.ReleaseComObject(_sourceTargetPolyUnionBoundary);
				_sourceTargetPolyUnionBoundary = null;
			}
		}
	}
}
