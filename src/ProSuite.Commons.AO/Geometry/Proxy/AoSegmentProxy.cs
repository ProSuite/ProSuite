using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using System;
using System.Threading;

namespace ProSuite.Commons.AO.Geometry.Proxy
{
	public class AoSegmentProxy : SegmentProxy
	{
		private static ThreadLocal<ILine> _qTangent;
		private static ThreadLocal<IPoint> _qPoint;

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
			_extent = ProxyUtils.CreateBox(segment);
		}

		public override ISpatialReference SpatialReference => InnerSegment.SpatialReference;

		public override double Length => InnerSegment.Length;

		private ILine QTangent
		{
			get
			{
				if (_qTangent == null)
				{
					_qTangent = new ThreadLocal<ILine>(() => new LineClass());
				}

				return _qTangent.Value;
			}
		}

		private static IPoint QPoint
		{
			get
			{
				if (_qPoint == null)
				{
					_qPoint = new ThreadLocal<IPoint>(() => new PointClass());
				}

				return _qPoint.Value;
			}
		}

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

		[NotNull]
		public ISegment InnerSegment { get; }

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
			InnerSegment.QueryFromPoint(QPoint);
			return CreatePoint(QPoint, as3D);
		}

		public override Pnt GetEnd(bool as3D)
		{
			InnerSegment.QueryToPoint(QPoint);
			return CreatePoint(QPoint, as3D);
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
									fraction, asRatio, QPoint);

			return CreatePoint(QPoint, as3D);
		}

		public override double GetDirectionAt(double fraction)
		{
			InnerSegment.QueryTangent(esriSegmentExtension.esriExtendTangents, fraction, true, 1,
									  QTangent);
			QTangent.QueryFromPoint(QPoint);
			double x0, y0;
			QPoint.QueryCoords(out x0, out y0);

			QTangent.QueryToPoint(QPoint);
			double x1, y1;
			QPoint.QueryCoords(out x1, out y1);

			double dir = Math.Atan2(y1 - y0, x1 - x0);
			return dir;
		}

		public override void QueryOffset(Pnt point, out double offset, out double along)
		{
			along = 0;
			offset = 0;
			bool rightSide = false;
			QPoint.PutCoords(point.X, point.Y);
			InnerSegment.QueryPointAndDistance(esriSegmentExtension.esriExtendTangents, QPoint,
											   true, QPoint, ref along, ref offset, ref rightSide);
		}

		[NotNull]
		private static Pnt CreatePoint([NotNull] IPoint p, bool as3D)
		{
			return as3D
					   ? ProxyUtils.CreatePoint3D(p)
					   : ProxyUtils.CreatePoint2D(p);
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
