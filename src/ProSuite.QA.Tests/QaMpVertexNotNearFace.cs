using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.PointEnumerators;
using IPnt = ProSuite.Commons.Geom.IPnt;
using Pnt = ProSuite.Commons.Geom.Pnt;
using SegmentUtils_ = ProSuite.QA.Container.Geometry.SegmentUtils_;

namespace ProSuite.QA.Tests
{
	[ProximityTest]
	public class QaMpVertexNotNearFace : ContainerTest
	{
		public enum OffsetMethod
		{
			Vertical,
			Perpendicular
		}

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string FaceDoesNotDefineValidPlane =
				"FaceDoesNotDefineValidPlane";

			public const string FaceNotCoplanar = "FaceNotCoplanar";

			public const string PointTooCloseBelowFace =
				"PointTooCloseBelowFace";

			public const string PointTooCloseAboveFace =
				"PointTooCloseAboveFace";

			public const string PointInNonCoplanarityOfFace =
				"PointInNonCoplanarityOfFace";

			public Code() : base("MpVertexNotNearFace") { }
		}

		#endregion

		private const bool _defaultVerifyWithinFeature = false;
		private const bool _defaultReportNonCoplanarity = false;
		private const bool _defaultIgnoreNonCoplanarFaces = false;
		private const double _defaultPlaneCoincidence = -1;
		private const OffsetMethod _defaultCheckMethod = OffsetMethod.Vertical;

		private readonly double _minimumDistanceAbove;
		private readonly double _minimumDistanceBelow;

		private readonly double _xySrTolerance;
		private readonly double _zSrTolerance;

		private double _minimumSlopeDegrees;
		private double _minimumSlopeTan2;

		private IList<IFeatureClassFilter> _filters;
		private IList<QueryFilterHelper> _filterHelpers;
		private readonly IEnvelope _envelopeTemplate = new EnvelopeClass();

		[Doc(nameof(DocStrings.QaMpVertexNotNearFace_0))]
		public QaMpVertexNotNearFace(
			[Doc(nameof(DocStrings.QaMpVertexNotNearFace_multiPatchClass))] [NotNull]
			IReadOnlyFeatureClass
				multiPatchClass,
			[Doc(nameof(DocStrings.QaMpVertexNotNearFace_vertexClasses))] [NotNull]
			IList<IReadOnlyFeatureClass>
				vertexClasses,
			[Doc(nameof(DocStrings.QaMpVertexNotNearFace_minimumDistanceAbove))]
			double minimumDistanceAbove,
			[Doc(nameof(DocStrings.QaMpVertexNotNearFace_minimumDistanceBelow))]
			double minimumDistanceBelow)
			: base(
				CastToTables(Union(new[] { multiPatchClass }, vertexClasses)
					             .Cast<IReadOnlyFeatureClass>()))
		{
			_minimumDistanceAbove = minimumDistanceAbove;
			_minimumDistanceBelow = minimumDistanceBelow;

			VerifyWithinFeature = _defaultVerifyWithinFeature;
			ReportNonCoplanarity = _defaultReportNonCoplanarity;
			IgnoreNonCoplanarFaces = _defaultIgnoreNonCoplanarFaces;
			CheckMethod = _defaultCheckMethod;

			PlaneCoincidence = _defaultPlaneCoincidence;

			var geodataset = (IReadOnlyGeoDataset) multiPatchClass;
			var srt = (ISpatialReferenceTolerance) geodataset.SpatialReference;
			_xySrTolerance = srt.XYTolerance;
			_zSrTolerance = srt.ZTolerance;
		}

		[InternallyUsedTest]
		public QaMpVertexNotNearFace(QaMpVertexNotNearFaceDefinition definition)
			: this((IReadOnlyFeatureClass) definition.MultiPatchClass,
			       definition.VertexClasses.Cast<IReadOnlyFeatureClass>()
			                 .ToList(),
			       definition.MinimumDistanceAbove,
			       definition.MinimumDistanceBelow
			)
		{
			CoplanarityTolerance = definition.CoplanarityTolerance;
			ReportNonCoplanarity = definition.ReportNonCoplanarity;
			IgnoreNonCoplanarFaces = definition.IgnoreNonCoplanarFaces;
			VerifyWithinFeature = definition.VerifyWithinFeature;
			PointCoincidence = definition.PointCoincidence;
			EdgeCoincidence = definition.EdgeCoincidence;
			PlaneCoincidence = definition.PlaneCoincidence;
			MinimumSlopeDegrees = definition.MinimumSlopeDegrees;
		}

		[Doc(nameof(DocStrings.QaMpVertexNotNearFace_CoplanarityTolerance))]
		[TestParameter]
		public double CoplanarityTolerance { get; set; }

		[Doc(nameof(DocStrings.QaMpVertexNotNearFace_ReportNonCoplanarity))]
		[TestParameter(_defaultReportNonCoplanarity)]
		public bool ReportNonCoplanarity { get; set; }

		[Doc(nameof(DocStrings.QaMpVertexNotNearFace_IgnoreNonCoplanarFaces))]
		[TestParameter(_defaultIgnoreNonCoplanarFaces)]
		public bool IgnoreNonCoplanarFaces { get; set; }

		[Doc(nameof(DocStrings.QaMpVertexNotNearFace_VerifyWithinFeature))]
		[TestParameter(_defaultVerifyWithinFeature)]
		public bool VerifyWithinFeature { get; set; }

		[Doc(nameof(DocStrings.QaMpVertexNotNearFace_PointCoincidence))]
		[TestParameter]
		public double PointCoincidence { get; set; }

		[Doc(nameof(DocStrings.QaMpVertexNotNearFace_EdgeCoincidence))]
		[TestParameter]
		public double EdgeCoincidence { get; set; }

		[Doc(nameof(DocStrings.QaMpVertexNotNearFace_PlaneCoincidence))]
		[TestParameter(_defaultPlaneCoincidence)]
		public double PlaneCoincidence { get; set; }

		[Doc(nameof(DocStrings.QaMpVertexNotNearFace_MinimumSlopeDegrees))]
		[TestParameter]
		public double MinimumSlopeDegrees
		{
			get { return _minimumSlopeDegrees; }
			set
			{
				_minimumSlopeDegrees = value;
				double radians = MathUtils.ToRadians(value);
				double tan = Math.Tan(radians);
				_minimumSlopeTan2 = tan * tan;
			}
		}

		public OffsetMethod CheckMethod { get; set; }

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			if (tableIndex != 0)
			{
				return NoError;
			}

			var feature = row as IReadOnlyFeature;
			if (feature == null)
			{
				return NoError;
			}

			InitFilters();

			feature.Shape.QueryEnvelope(_envelopeTemplate);
			IGeometry searchGeometry = _envelopeTemplate;

			List<PlaneHelper> planeHelpers = null;

			var errorCount = 0;

			for (var relatedTableIndex = 1;
			     relatedTableIndex < InvolvedTables.Count;
			     relatedTableIndex++)
			{
				_filters[relatedTableIndex].FilterGeometry = searchGeometry;
				int i = relatedTableIndex;

				foreach (IReadOnlyRow relatedRow in
				         Search(InvolvedTables[i], _filters[i], _filterHelpers[i]))
				{
					var relatedFeature = (IReadOnlyFeature) relatedRow;
					if (! VerifyWithinFeature && relatedFeature == row)
					{
						continue;
					}

					if (planeHelpers == null)
					{
						planeHelpers = GetPlaneHelpers(feature);
					}

					IPointsEnumerator points =
						PointsEnumeratorFactory.Create(relatedFeature, null);

					foreach (PlaneHelper planeHelper in planeHelpers)
					{
						errorCount += Check(planeHelper, points, feature, relatedFeature);
					}
				}
			}

			return errorCount;
		}

		[NotNull]
		private List<PlaneHelper> GetPlaneHelpers([NotNull] IReadOnlyFeature feature)
		{
			const bool includeAssociatedParts = true;
			SegmentsPlaneProvider planeProvider =
				SegmentsPlaneProvider.Create(feature, includeAssociatedParts);

			var indexedMultiPatch = (IIndexedMultiPatch) planeProvider.IndexedSegments;

			var result = new List<PlaneHelper>();

			SegmentsPlane segmentsPlane;
			while ((segmentsPlane = planeProvider.ReadPlane()) != null)
			{
				result.Add(PlaneHelper.Create(segmentsPlane, indexedMultiPatch, this));
			}

			return result;
		}

		private int Check([NotNull] PlaneHelper planeHelper,
		                  [NotNull] IPointsEnumerator points,
		                  [NotNull] IReadOnlyFeature feature,
		                  [NotNull] IReadOnlyFeature related)
		{
			var errorCount = 0;

			if (ReportNonCoplanarity)
			{
				errorCount += planeHelper.ReportErrors(feature);
			}

			if (! planeHelper.IsPlaneDefined)
			{
				return errorCount;
			}

			if (IgnoreNonCoplanarFaces && ! planeHelper.IsCoplanar)
			{
				return errorCount;
			}

			if (_minimumSlopeTan2 > 0 && planeHelper.GetSlopeTan2() < _minimumSlopeTan2)
			{
				return errorCount;
			}

			IBox box = planeHelper.GetSearchBox();

			foreach (Pnt point in points.GetPoints(box))
			{
				errorCount += Check(planeHelper, point, points.XYTolerance, CheckMethod,
				                    feature, related);
			}

			return errorCount;
		}

		private int Check([NotNull] PlaneHelper planeHelper,
		                  [NotNull] Pnt point,
		                  double xyTolerance,
		                  OffsetMethod checkMethod,
		                  [NotNull] IReadOnlyFeature feature,
		                  [NotNull] IReadOnlyFeature related)
		{
			if (IsCoincident(planeHelper, point, xyTolerance))
			{
				return NoError;
			}

			switch (checkMethod)
			{
				case OffsetMethod.Vertical:
					return CheckNonCoincidentVertical(planeHelper, point, feature,
					                                  related);

				case OffsetMethod.Perpendicular:
					throw new NotImplementedException(
						"Perpendicular not yet implemented");

				default:
					throw new ArgumentOutOfRangeException(
						nameof(checkMethod), checkMethod, @"Unknown check method");
			}
		}

		private int CheckNonCoincidentVertical([NotNull] PlaneHelper planeHelper,
		                                       [NotNull] Pnt point,
		                                       [NotNull] IReadOnlyFeature feature,
		                                       [NotNull] IReadOnlyFeature related)
		{
			if (! planeHelper.IsInFootprint(point))
			{
				return NoError;
			}

			double zMin = planeHelper.GetMinPlaneZ(point);
			if (double.IsNaN(zMin))
			{
				return NoError; // TODO revise
			}

			double dzMin = zMin - point[2];
			double zMax = planeHelper.GetMaxPlaneZ(point);
			double dzMax = point[2] - zMax;

			if (PlaneCoincidence >= 0 &&
			    dzMax - PlaneCoincidence < 0 &&
			    dzMin - PlaneCoincidence < 0)
			{
				// TODO : is this the right criteria to ignore checks?
				return NoError;
			}

			var errorCount = 0;

			if (dzMin >= 0 && dzMin < _minimumDistanceBelow)
			{
				string cmp =
					FormatLengthComparison(
						dzMin, "<", _minimumDistanceBelow,
						feature.Shape.SpatialReference);

				errorCount +=
					ReportError(
						$"Point is {cmp} below face",
						InvolvedRowUtils.GetInvolvedRows(feature, related),
						GeometryFactory.CreatePoint(point.X, point.Y, point[2]),
						Codes[Code.PointTooCloseBelowFace], null);
			}

			if (dzMax >= 0 && dzMax < _minimumDistanceAbove)
			{
				string cmp =
					FormatLengthComparison(
						dzMax, "<", _minimumDistanceAbove,
						feature.Shape.SpatialReference);

				errorCount +=
					ReportError(
						$"Point is {cmp} above face",
						InvolvedRowUtils.GetInvolvedRows(feature, related),
						GeometryFactory.CreatePoint(point.X, point.Y, point[2]),
						Codes[Code.PointTooCloseAboveFace], null);
			}

			if (dzMin <= 0 && dzMax <= 0)
			{
				errorCount +=
					ReportError(
						"Point is within non-coplanarity of face",
						InvolvedRowUtils.GetInvolvedRows(feature, related),
						GeometryFactory.CreatePoint(point.X, point.Y, point[2]),
						Codes[Code.PointInNonCoplanarityOfFace], null);
			}

			return errorCount;
		}

		private bool IsCoincident([NotNull] PlaneHelper planeHelper,
		                          [NotNull] Pnt point,
		                          double xyTolerance)
		{
			double near = Math.Max(xyTolerance, PointCoincidence);
			double near2 = near * near;

			double nearEdge2 = 0;
			if (EdgeCoincidence > 0)
			{
				double nearEdge = Math.Max(xyTolerance, EdgeCoincidence);
				nearEdge2 = nearEdge * nearEdge;
			}

			const bool as3D = true;
			foreach (SegmentProxy segment in planeHelper.GetSegments())
			{
				if (segment.GetStart(as3D).Dist2(point) < near2)
				{
					return true;
				}

				if (EdgeCoincidence > 0)
				{
					double fraction =
						SegmentUtils_.GetClosestPointFraction(segment, point, as3D);
					if (fraction >= 0 && fraction <= 1)
					{
						IPnt edgePoint = segment.GetPointAt(fraction, as3D);

						if (point.Dist2(edgePoint) < nearEdge2)
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		private int ReportCollinearSegments([NotNull] IMultiPatch errorGeometry,
		                                    [NotNull] IReadOnlyFeature feature)
		{
			const string description =
				"The segments of this face are collinear and do not define a valid plane";

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(feature), errorGeometry,
				Codes[Code.FaceDoesNotDefineValidPlane], null);
		}

		private int ReportNonCoplanarFace(int segmentsCount,
		                                  double maxOffset,
		                                  [NotNull] IMultiPatch errorGeometry,
		                                  [NotNull] IReadOnlyFeature involvedFeature)
		{
			string comparison = FormatLengthComparison(
				maxOffset, ">", CoplanarityTolerance,
				involvedFeature.Shape.SpatialReference);

			return ReportError(
				$"Face with {segmentsCount} segments is not planar, max. offset = {comparison}",
				InvolvedRowUtils.GetInvolvedRows(involvedFeature), errorGeometry,
				Codes[Code.FaceNotCoplanar], null);
		}

		private class PlaneVerticalHelper : PlaneHelper
		{
			private static readonly ThreadLocal<IPoint> _query =
				new ThreadLocal<IPoint>(() => new PointClass());

			private IPolygon _footPrint;
			private Box _footPrintBox;

			public PlaneVerticalHelper([NotNull] SegmentsPlane segmentsPlane,
			                           [NotNull] IIndexedMultiPatch indexedMultipatch)
				: base(segmentsPlane)
			{
				Assert.ArgumentNotNull(indexedMultipatch, nameof(indexedMultipatch));
				// not yet used
			}

			public override bool IsInFootprint(Pnt point)
			{
				_query.Value.PutCoords(point.X, point.Y);

				bool disjoint = ((IRelationalOperator) Footprint).Disjoint(_query.Value);
				return ! disjoint;
			}

			public override double GetMinPlaneZ(Pnt point)
			{
				// TODO hack to avoid exception
				if (MathUtils.AreEqual(0, Plane.C)) return double.NaN;

				double zPlane = Plane.GetZ(point.X, point.Y) -
				                MinOffset * Math.Abs(UnitNormal[2]);
				return zPlane;
			}

			public override double GetMaxPlaneZ(Pnt point)
			{
				double zPlane = Plane.GetZ(point.X, point.Y) +
				                MaxOffset * Math.Abs(UnitNormal[2]);
				return zPlane;
			}

			private IPolygon Footprint => _footPrint ??
			                              (_footPrint =
				                               SegmentUtils_.CreatePolygon(
					                               SegmentsPlane.Segments));

			private Box FootprintBox
				=> _footPrintBox ??
				   (_footPrintBox = ProxyUtils.CreateBox(Footprint));

			public override IBox GetSearchBox()
			{
				return FootprintBox;
			}
		}

		private abstract class PlaneHelper
		{
			private Vector _unitNormal;
			private double _minOffset;
			private double _maxOffset;
			private QaMpVertexNotNearFace _parent;
			private bool _coplanar;

			[NotNull]
			public static PlaneHelper Create(
				[NotNull] SegmentsPlane segmentsPlane,
				[NotNull] IIndexedMultiPatch indexedMultipatch,
				[NotNull] QaMpVertexNotNearFace parent)
			{
				if (parent.CheckMethod != OffsetMethod.Vertical)
				{
					throw new NotImplementedException();
				}

				PlaneHelper planeHelper =
					new PlaneVerticalHelper(segmentsPlane, indexedMultipatch);

				planeHelper._parent = parent;

				return planeHelper;
			}

			protected double MinOffset
			{
				get
				{
					InitPlaneOffsets(false, null);
					return _minOffset;
				}
			}

			protected double MaxOffset
			{
				get
				{
					InitPlaneOffsets(false, null);
					return _maxOffset;
				}
			}

			protected PlaneHelper([NotNull] SegmentsPlane segmentsPlane)
			{
				SegmentsPlane = segmentsPlane;
				_minOffset = 1;
				_maxOffset = -1;
			}

			[NotNull]
			protected SegmentsPlane SegmentsPlane { get; }

			[NotNull]
			protected Plane3D Plane => SegmentsPlane.Plane;

			public bool IsPlaneDefined => SegmentsPlane.Plane.IsDefined;

			[NotNull]
			public abstract IBox GetSearchBox();

			protected Vector UnitNormal =>
				_unitNormal ?? (_unitNormal = Plane.GetUnitNormal());

			public bool IsCoplanar
			{
				get
				{
					InitPlaneOffsets(false, null);
					return _coplanar;
				}
			}

			private int InitPlaneOffsets(bool reportErrors,
			                             [CanBeNull] IReadOnlyFeature involvedFeature)
			{
				const int noError = 0;

				if (_minOffset <= 0 && _maxOffset >= 0)
				{
					return noError;
				}

				Plane3D plane = Plane;
				var unitNormal = UnitNormal;

				// double nf = plane.Nf;

				_minOffset = 0;
				_maxOffset = 0;
				var segmentsCount = 0;
				foreach (SegmentProxy segment in SegmentsPlane.Segments)
				{
					segmentsCount++;
					IPnt point = segment.GetStart(true);
					// double f = normal.X * point.X + normal.Y * point.Y + normal.Z * point[2] + nf;
					double distanceSigned =
						plane.GetDistanceSigned(point.X, point.Y, point[2]);
					double offset = Math.Abs(distanceSigned);

					if (distanceSigned > 0 == unitNormal[2] > 0
					   ) // oriented same as normal
					{
						_maxOffset = Math.Max(offset, _maxOffset);
					}
					else
					{
						_minOffset = Math.Min(-offset, _minOffset);
					}
				}

				var coplanarityTolerance = GeomUtils.AdjustCoplanarityTolerance(
					plane, _parent.CoplanarityTolerance,
					_parent._zSrTolerance, _parent._xySrTolerance);

				double maxOffset = Math.Max(Math.Abs(_maxOffset), Math.Abs(_minOffset));
				_coplanar = maxOffset < coplanarityTolerance;

				if (_coplanar || ! reportErrors || involvedFeature == null)
				{
					return noError;
				}

				IMultiPatch errorGeometry =
					SegmentUtils_.CreateMultiPatch(SegmentsPlane.Segments);
				return _parent.ReportNonCoplanarFace(segmentsCount, maxOffset,
				                                     errorGeometry, involvedFeature);
			}

			[NotNull]
			public IEnumerable<SegmentProxy> GetSegments()
			{
				return SegmentsPlane.Segments;
			}

			public abstract bool IsInFootprint([NotNull] Pnt point);

			public abstract double GetMinPlaneZ([NotNull] Pnt point);

			public abstract double GetMaxPlaneZ([NotNull] Pnt point);

			public double GetSlopeTan2()
			{
				var normal = UnitNormal;
				var normalZ = normal[2];
				if (Math.Abs(normalZ) < double.Epsilon)
				{
					return double.MaxValue;
				}

				double tan2 = (normal.X * normal.X + normal.Y * normal.Y) /
				              (normalZ * normalZ);
				return tan2;
			}

			public int ReportErrors([NotNull] IReadOnlyFeature feature)
			{
				if (! Plane.IsDefined)
				{
					IMultiPatch errorGeometry =
						SegmentUtils_.CreateMultiPatch(SegmentsPlane.Segments);
					return _parent.ReportCollinearSegments(errorGeometry, feature);
				}

				return InitPlaneOffsets(true, feature);
			}
		}

		private void InitFilters()
		{
			if (_filters != null)
			{
				return;
			}

			CopyFilters(out _filters, out _filterHelpers);

			foreach (var filter in _filters)
			{
				filter.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelIntersects;
			}
		}
	}
}
