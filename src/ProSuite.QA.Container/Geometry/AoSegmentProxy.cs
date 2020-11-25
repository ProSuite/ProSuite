using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;

namespace ProSuite.QA.Container.Geometry
{
	[CLSCompliant(false)]
	public class AoSegmentProxy : SegmentProxy
	{
		private static readonly IPoint _qPoint = new PointClass();
		private static ILine _qTangent;

		[NotNull] private readonly Box _extent;

		private IPolyline _segmentLine;

		/// <summary>
		/// Initializes a new instance of the <see cref="AoSegmentProxy"/> class.
		/// </summary>
		/// <param name="segment">The segment (must be a copy, i.e. not straight from a recycling segment enumerator).</param>
		/// <param name="partIndex">Index of the part.</param>
		/// <param name="segmentIndex">Index of the segment.</param>
		public AoSegmentProxy([NotNull] ISegment segment, int partIndex, int segmentIndex)
			: base(partIndex, segmentIndex)
		{
			Assert.ArgumentNotNull(segment, nameof(segment));

			InnerSegment = segment;
			_extent = QaGeometryUtils.CreateBox(segment);
		}

		public override ISpatialReference SpatialReference => InnerSegment.SpatialReference;

		public override double Length => InnerSegment.Length;

		private ILine QTangent => _qTangent ?? (_qTangent = new LineClass());

		public override SegmentProxy GetSubCurve(double fromRatio, double toRatio)
		{
			ICurve subCurve;
			const bool asRatio = true;
			InnerSegment.GetSubcurve(fromRatio, toRatio, asRatio, out subCurve);

			var segment = subCurve as ISegment;
			Assert.NotNull(segment, "segment expected from GetSubCurve() on segment");

			// TODO revise returning the same SegmentIndex. Correct?
			// maybe return something more narrow than SegmentProxy? Just what the caller needs?

			return new AoSegmentProxy(segment, PartIndex, SegmentIndex);
		}

		public override WKSEnvelope GetSubCurveBox(double fromRatio, double toRatio)
		{
			WKSEnvelope box = new WKSEnvelope();
			if (fromRatio > 0 || toRatio < 1)
			{
				ICurve subCurve;
				const bool asRatio = true;
				InnerSegment.GetSubcurve(fromRatio, toRatio, asRatio, out subCurve);
				var segment = subCurve as ISegment;
				Assert.NotNull(segment, "segment expected from GetSubCurve() on segment");
				segment.QueryWKSEnvelope(ref box);
			}
			else
			{
				InnerSegment.QueryWKSEnvelope(ref box);
			}

			return box;
		}

		[CLSCompliant(false)]
		[NotNull]
		public ISegment InnerSegment { get; }

		[CLSCompliant(false)]
		public override IPolyline GetPolyline(bool forceCreation)
		{
			if (_segmentLine == null || forceCreation)
			{
				IPolyline segmentLine = GeometryFactory.CreatePolyline(InnerSegment);

				if (forceCreation)
				{
					return segmentLine;
				}

				_segmentLine = segmentLine;
			}

			return _segmentLine;
		}

		public override bool IsLinear
		{
			get
			{
				// TODO circular arcs/beziers etc. may also be linear
				// -> e.g. ICircularArc.IsLine property
				// --> what should be returned here, the segment type
				//     or the actual form of the segment?
				bool linear = InnerSegment is ILine;
				return linear;
			}
		}

		public override Pnt GetStart(bool as3D)
		{
			InnerSegment.QueryFromPoint(_qPoint);
			return CreatePoint(_qPoint, as3D);
		}

		public override Pnt GetEnd(bool as3D)
		{
			InnerSegment.QueryToPoint(_qPoint);
			return CreatePoint(_qPoint, as3D);
		}

		public override IPnt GetPointAt(double fraction)
		{
			const bool as3D = false;
			return GetPointAt(fraction, as3D);
		}

		public override IPnt GetPointAt(double fraction, bool as3D)
		{
			const bool asRatio = true;
			InnerSegment.QueryPoint(esriSegmentExtension.esriNoExtension,
			                        fraction, asRatio, _qPoint);

			return CreatePoint(_qPoint, as3D);
		}

		public override double GetDirectionAt(double fraction)
		{
			InnerSegment.QueryTangent(esriSegmentExtension.esriExtendTangents, fraction, true, 1,
			                          QTangent);
			QTangent.QueryFromPoint(_qPoint);
			double x0, y0;
			_qPoint.QueryCoords(out x0, out y0);

			QTangent.QueryToPoint(_qPoint);
			double x1, y1;
			_qPoint.QueryCoords(out x1, out y1);

			double dir = Math.Atan2(y1 - y0, x1 - x0);
			return dir;
		}

		public override void QueryOffset(Pnt point, out double offset, out double along)
		{
			along = 0;
			offset = 0;
			bool rightSide = false;
			_qPoint.PutCoords(point.X, point.Y);
			InnerSegment.QueryPointAndDistance(esriSegmentExtension.esriExtendTangents, _qPoint,
			                                   true, _qPoint, ref along, ref offset, ref rightSide);
		}

		[NotNull]
		private static Pnt CreatePoint([NotNull] IPoint p, bool as3D)
		{
			return as3D
				       ? QaGeometryUtils.CreatePoint3D(p)
				       : QaGeometryUtils.CreatePoint2D(p);
		}

		//[NotNull]
		//public ISegment BaseSegment
		//{
		//    get { return _segment; }
		//}

		public override IPnt Min => _extent.Min;

		public override IPnt Max => _extent.Max;
	}
}
