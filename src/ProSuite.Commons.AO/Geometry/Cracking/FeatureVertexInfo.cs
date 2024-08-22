using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry.Generalize;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geometry.Cracking
{
	/// <summary>
	/// Maintains information on special points/vertices of a feature in relation with other features.
	/// These are intersection points with other features, crack points used for topological correctness, i.e.
	/// intersection points with other geometries that do not (yet) exist, and points to be deleted.
	/// </summary>
	public class FeatureVertexInfo
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		// TODO: Remove _perimeter together with snap tolerance!
		[CanBeNull] private readonly IEnvelope _perimeter;

		private IPolyline _originalClippedPolyline;

		private IMultipoint _originalClippedPoints;

		private IPointCollection _crackPoints;

		/// <summary>
		/// Initializes a new instance of the <see cref="FeatureVertexInfo"/> class.
		/// </summary>
		/// <param name="feature">The feature for which special vertices shall be calculated</param>
		/// <param name="perimeter">The area of interest</param>
		/// <param name="snapTolerance">The tolerance to be used to snap to existing vertices from a crack vertex</param>
		/// <param name="minimumSegmentLength">The minimum segment length to be respected for this feature</param>
		public FeatureVertexInfo([NotNull] IFeature feature,
		                         [CanBeNull] IEnvelope perimeter,
		                         double? snapTolerance = null,
		                         double? minimumSegmentLength = null)
		{
			Feature = feature;
			_perimeter = perimeter;

			// Make sure no 0.0 tolerances are used. The minimum that makes sense is the tolerance.
			SnapTolerance = snapTolerance == null || (double) snapTolerance > 0
				                ? snapTolerance
				                : null;

			MinimumSegmentLength =
				minimumSegmentLength == null || (double) minimumSegmentLength > 0
					? minimumSegmentLength
					: null;
		}

		[NotNull]
		public FeatureVertexInfo Clone([CanBeNull] IGeometry subSelectionPerimeter)
		{
			var result = new FeatureVertexInfo(Feature, _perimeter, SnapTolerance,
			                                   MinimumSegmentLength);

			if (CrackPoints != null)
			{
				result.CrackPoints = new List<CrackPoint>();

				foreach (CrackPoint crackPoint in CrackPoints)
				{
					if (subSelectionPerimeter == null ||
					    GeometryUtils.Contains(subSelectionPerimeter, crackPoint.Point))
					{
						result.CrackPoints.Add(crackPoint);
					}
				}
			}

			if (IntersectionPoints != null)
			{
				result.IntersectionPoints = GetFilteredPoints(IntersectionPoints,
				                                              subSelectionPerimeter);
			}

			if (CrackPointCollection != null)
			{
				result.CrackPointCollection = GetFilteredPoints(CrackPointCollection,
				                                                subSelectionPerimeter);
			}

			if (PointsToDelete != null)
			{
				result.PointsToDelete = GetFilteredPoints(PointsToDelete, subSelectionPerimeter);
			}

			if (NonCrackablePoints != null)
			{
				result.NonCrackablePoints = GetFilteredPoints(NonCrackablePoints,
				                                              subSelectionPerimeter);
			}

			if (NonDeletablePoints != null)
			{
				result.NonDeletablePoints = GetFilteredPoints(NonDeletablePoints,
				                                              subSelectionPerimeter);
			}

			if (ShortSegments != null)
			{
				result.ShortSegments = GeneralizeUtils.GetFilteredSegments(ShortSegments,
					subSelectionPerimeter,
					true);
			}

			if (NonRemovableShortSegments != null)
			{
				result.NonRemovableShortSegments =
					GeneralizeUtils.GetFilteredSegments(NonRemovableShortSegments,
					                                    subSelectionPerimeter, true);
			}

			return result;
		}

		private static IPointCollection GetFilteredPoints(IPointCollection inputPoints,
		                                                  IGeometry subSelectionPerimeter)
		{
			IPointCollection resultPoints;
			if (subSelectionPerimeter == null)
			{
				resultPoints = (IPointCollection) GeometryFactory.Clone((IGeometry) inputPoints);
			}
			else
			{
				resultPoints = (IPointCollection) IntersectionUtils.GetIntersectionPoints(
					subSelectionPerimeter, (IGeometry) inputPoints);
			}

			return resultPoints;
		}

		public IFeature Feature { get; }

		public double? SnapTolerance { get; }

		public double? MinimumSegmentLength { get; set; }

		public bool LinearizeSegments { get; set; }

		[NotNull]
		public IPolyline OriginalClippedPolyline
		{
			get
			{
				if (_originalClippedPolyline == null)
				{
					SetOriginalClippedPolyline(Feature, _perimeter);
				}

				return Assert.NotNull(_originalClippedPolyline);
			}
		}

		public IMultipoint OriginalClippedPoints
		{
			get
			{
				IEnumerable<IPoint> pointsInPerimeter =
					GeometryUtils.GetPoints((IPointCollection) OriginalClippedPolyline).Where(
						point =>
							_perimeter == null ||
							GeometryUtils.Intersects(_perimeter, point));

				return _originalClippedPoints ??
				       (_originalClippedPoints =
					        GeometryFactory.CreateMultipoint(pointsInPerimeter));
			}
		}

		[CanBeNull]
		public IPointCollection IntersectionPoints { get; private set; }

		[CanBeNull]
		public IList<CrackPoint> CrackPoints { get; private set; }

		[CanBeNull]
		public IPointCollection CrackPointCollection
		{
			get
			{
				if (_crackPoints != null && _crackPoints.PointCount == 0)
				{
					return null;
				}

				return _crackPoints;
			}

			private set { _crackPoints = value; }
		}

		[CanBeNull]
		public IPointCollection PointsToDelete { get; set; }

		public bool HasPointsToDelete
			=> PointsToDelete != null && PointsToDelete.PointCount > 0;

		[CanBeNull]
		public IPointCollection NonCrackablePoints { get; set; }

		[CanBeNull]
		public IPointCollection NonDeletablePoints { get; set; }

		[CanBeNull]
		public IList<esriSegmentInfo> ShortSegments { get; set; }

		public bool HasShortSegments => ShortSegments != null && ShortSegments.Count > 0;

		[CanBeNull]
		public IList<esriSegmentInfo> NonRemovableShortSegments { get; set; }

		public esriGeometryType GeometryType => ((IFeatureClass) Feature.Class).ShapeType;

		public void AddCrackPoints(IList<CrackPoint> crackPoints)
		{
			if (CrackPoints == null)
			{
				CrackPoints = crackPoints;
			}
			else
			{
				foreach (CrackPoint crackPoint in crackPoints)
				{
					CrackPoints.Add(crackPoint);
				}
			}

			foreach (CrackPoint crackPoint in crackPoints)
			{
				AddToCrackPointCollections(crackPoint);
			}
		}

		private void AddToCrackPointCollections(CrackPoint crackPoint)
		{
			if (crackPoint.ViolatesMinimumSegmentLength)
			{
				AddNonCrackPoint(crackPoint.Point);
			}
			else
			{
				AddCrackPoint(crackPoint.Point);

				//if (crackPoint.TargetVertexOnlyDifferentInZ)
				//{
				//	if (crackPoint.PlanarPointLocationIndex != null && CrackPointCollection != null)
				//	{
				//		AddCrackPointOnlyDifferentInZ(
				//			CrackPointCollection.get_Point((int) crackPoint.PlanarPointLocationIndex));
				//	}
				//	else
				//	{
				//		AddCrackPointOnlyDifferentInZ(crackPoint.Point);
				//	}
				//}
			}
		}

		public void AddIntersectionPoints([CanBeNull] IPointCollection pointCollection)
		{
			if (pointCollection == null || ((IGeometry) pointCollection).IsEmpty)
			{
				return;
			}

			if (IntersectionPoints == null)
			{
				IntersectionPoints =
					(IPointCollection) GeometryFactory.CreateMultipoint(pointCollection);
			}
			else
			{
				IntersectionPoints.AddPointCollection(pointCollection);
			}
		}

		private void AddCrackPoint(IPoint point)
		{
			if (CrackPointCollection == null)
			{
				CrackPointCollection =
					(IPointCollection) GeometryFactory.CreateMultipoint(point);
			}
			else
			{
				AddPoint(point, CrackPointCollection);
			}
		}

		private void AddNonCrackPoint(IPoint point)
		{
			if (NonCrackablePoints == null)
			{
				NonCrackablePoints = (IPointCollection) GeometryFactory.CreateMultipoint(point);
			}
			else
			{
				AddPoint(point, NonCrackablePoints);
			}
		}

		public void SimplifyCrackPoints()
		{
			if (CrackPointCollection != null)
			{
				GeometryUtils.Simplify((IGeometry) CrackPointCollection);
			}
		}

		[CanBeNull]
		public IPointCollection GetCrackPoints([CanBeNull] IGeometry inArea)
		{
			IPointCollection pointCollection = CrackPointCollection;

			return GetPoints(pointCollection, inArea);
		}

		[NotNull]
		public IList<CrackPoint> GetCrackPoints3d([CanBeNull] IGeometry inArea)
		{
			IList<CrackPoint> result;

			if (inArea == null)
			{
				result = CrackPoints;
			}
			else
			{
				result = CrackPoints?.Where(cp => GeometryUtils.Contains(inArea, cp.Point))
				                    .ToList();
			}

			return result ?? new List<CrackPoint>(0);
		}

		[CanBeNull]
		public IPointCollection GetPointsToDelete(IGeometry withinArea)
		{
			return GetPoints(PointsToDelete, withinArea);
		}

		public override string ToString()
		{
			return $"FeatureVertexInfo for {GdbObjectUtils.ToString(Feature)}";
		}

		public string ToString(bool includeProtectedPoints)
		{
			return
				$"{ToString()}. Protected points: {GeometryUtils.ToString((IGeometry) CrackPointCollection)}";
		}

		public void Dispose()
		{
			if (PointsToDelete != null)
			{
				Marshal.ReleaseComObject(PointsToDelete);
				PointsToDelete = null;
			}

			if (IntersectionPoints != null)
			{
				Marshal.ReleaseComObject(IntersectionPoints);
				IntersectionPoints = null;
			}

			if (CrackPointCollection != null)
			{
				Marshal.ReleaseComObject(CrackPointCollection);
				CrackPointCollection = null;
			}

			if (NonCrackablePoints != null)
			{
				Marshal.ReleaseComObject(NonCrackablePoints);
				NonCrackablePoints = null;
			}

			if (NonDeletablePoints != null)
			{
				Marshal.ReleaseComObject(NonDeletablePoints);
				NonDeletablePoints = null;
			}

			if (_originalClippedPolyline != null)
			{
				Marshal.ReleaseComObject(_originalClippedPolyline);
				_originalClippedPolyline = null;
			}

			if (ShortSegments != null)
			{
				foreach (esriSegmentInfo segmentInfo in ShortSegments)
				{
					Marshal.ReleaseComObject(segmentInfo.pSegment);
				}

				ShortSegments = null;
			}

			if (NonRemovableShortSegments != null)
			{
				foreach (esriSegmentInfo segmentInfo in NonRemovableShortSegments)
				{
					Marshal.ReleaseComObject(segmentInfo.pSegment);
				}

				NonRemovableShortSegments = null;
			}
		}

		#region Non-public methods

		private void SetOriginalClippedPolyline([NotNull] IFeature feature,
		                                        [CanBeNull] IEnvelope envelope)
		{
			IGeometry inputGeometry = feature.Shape;

			if (inputGeometry.GeometryType == esriGeometryType.esriGeometryMultiPatch)
			{
				// use line salad, but do not clip (it's extremely un-simple)
				_originalClippedPolyline = CrackUtils.CreatePolylineSalad(inputGeometry);
			}
			else if (envelope == null)
			{
				_originalClippedPolyline = GeometryFactory.CreatePolyline(feature.Shape);
			}
			else
			{
				IEnvelope clipEnvelope = GeometryFactory.Clone(envelope);

				double expansionTol = GetEnvelopeExpansionTolerance();
				clipEnvelope.Expand(expansionTol, expansionTol, false);

				_originalClippedPolyline = GetClippedOutline(feature.Shape, clipEnvelope);
			}
		}

		private double GetEnvelopeExpansionTolerance()
		{
			double? result = null;
			if (SnapTolerance != null)
			{
				result = SnapTolerance;
			}

			if (MinimumSegmentLength != null)
			{
				if (result == null || result < MinimumSegmentLength)
				{
					result = MinimumSegmentLength;
				}
			}

			result = result ?? GeometryUtils.GetXyTolerance(Feature);

			// When zoomed in to a very large scale intersection artefacts appear due to clipped segments
			// Expand envelope with maximum segment length (alternatively consider 
			// using n times the tolerance, where n is at least 4)
			var segmentCollection = Feature.Shape as ISegmentCollection;

			if (segmentCollection != null)
			{
				ISegment longestSegment = GeometryUtils.GetLongestSegment(segmentCollection);

				if (longestSegment != null && longestSegment.Length > result)
				{
					result = longestSegment.Length;
				}
			}

			return (double) result;
		}

		private static IPolyline GetClippedOutline(IGeometry geometry,
		                                           IEnvelope clipEnvelope)
		{
			IPolyline geometryAsLine = GeometryFactory.CreatePolyline(geometry);

			IPolyline result = GeometryUtils.GetClippedPolyline(geometryAsLine, clipEnvelope);

			// in case of cut polygon boundaries: merge the lines at poly start/end point:
			GeometryUtils.Simplify(result, true, true);

			Marshal.ReleaseComObject(geometryAsLine);

			return result;
		}

		[CanBeNull]
		private static IPointCollection GetPoints(IPointCollection pointCollection,
		                                          IGeometry inArea)
		{
			if (pointCollection == null ||
			    pointCollection.PointCount == 0)
			{
				return null;
			}

			if (inArea == null)
			{
				return pointCollection;
			}

			if (GeometryUtils.Disjoint(inArea, (IGeometry) pointCollection))
			{
				return null;
			}

			// because intersecting multipoints with envelope is not implemented:
			var withinPolygon = inArea as IPolygon;
			withinPolygon = withinPolygon ?? GeometryFactory.CreatePolygon(inArea);

			var result =
				(IPointCollection)
				IntersectionUtils.GetIntersection((IGeometry) pointCollection, withinPolygon);

			return result;
		}

		private static void AddPoint([NotNull] IPoint point,
		                             [NotNull] IPointCollection toCollection)
		{
			Assert.ArgumentNotNull(point, nameof(point));
			Assert.ArgumentNotNull(toCollection, nameof(toCollection));

			object missing = Type.Missing;
			toCollection.AddPoint(point, ref missing, ref missing);
		}

		#endregion
	}
}
