using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;

namespace ProSuite.QA.Container.Geometry
{
	internal abstract class WksSegmentProxyBase : SegmentProxy
	{
		protected readonly IWKSPointCollection _wksPointCollection;

		protected WksSegmentProxyBase([NotNull] IWKSPointCollection wksPointCollection,
		                              int partIndex, int index)
			: base(partIndex, index)
		{
			Assert.ArgumentNotNull(wksPointCollection, nameof(wksPointCollection));

			_wksPointCollection = wksPointCollection;
		}

		public override bool IsLinear => true;

		public override IPolyline GetPolyline(bool forceCreation)
		{
			// TODO too expensive for the current call frequency. In any case convert to method
			var points = new[] {FromPoint, ToPoint};
			IPointCollection4 pointCollection = new PolylineClass();
			GeometryUtils.SetWKSPointZs(pointCollection, points);

			var line = (IPolyline) pointCollection;
			line.SpatialReference = _wksPointCollection.SpatialReference;
			return line;
		}

		public override ISpatialReference SpatialReference =>
			_wksPointCollection.SpatialReference;

		protected abstract WKSPointZ FromPoint { get; }

		protected abstract WKSPointZ ToPoint { get; }

		public override double Length
		{
			get
			{
				const bool as3D = false;
				const int dimension = 2;

				return Math.Sqrt(GetStart(as3D).Dist2(GetEnd(as3D), dimension));
			}
		}

		public override double GetDirectionAt(double fraction)
		{
			WKSPointZ from = FromPoint;
			WKSPointZ to = ToPoint;
			double dir = Math.Atan2(to.Y - from.Y, to.X - from.X);
			return dir;
		}

		public override SegmentProxy GetSubCurve(double fromRatio, double toRatio)
		{
			// TODO implement based on FromPoint, ToPoint, return WksSegmentProxy
			// The bidirectional dependency between PartProxy and WksSegmentProxy made this hard, 
			// -> break that cycle (using an interface implemented on PartProxy, giving
			// WksSegmentProxy just what it needs --> use alternate implementation of that interface for the
			// segment constructed here)

			throw new NotImplementedException();
		}

		public override WKSEnvelope GetSubCurveBox(double fromRatio, double toRatio)
		{
			WKSPointZ from = fromRatio > 0
				                 ? QaGeometryUtils.GetWksPoint(GetPointAt(fromRatio))
				                 : FromPoint;
			WKSPointZ to = toRatio < 1
				               ? QaGeometryUtils.GetWksPoint(GetPointAt(toRatio))
				               : ToPoint;
			WKSEnvelope box = new WKSEnvelope
			                  {
				                  XMin = Math.Min(from.X, to.X),
				                  YMin = Math.Min(from.Y, to.Y),
				                  XMax = Math.Max(from.X, to.X),
				                  YMax = Math.Max(from.Y, to.Y)
			                  };
			return box;
		}

		public override IPnt Min
		{
			get
			{
				// TODO consider caching point reference, hitcounts are extremely large here
				WKSPointZ from = FromPoint;
				WKSPointZ to = ToPoint;
				return new Pnt2D(Math.Min(from.X, to.X), Math.Min(from.Y, to.Y));
			}
		}

		public override IPnt Max
		{
			get
			{
				// TODO consider caching point reference, hitcounts are extremely large here
				WKSPointZ from = FromPoint;
				WKSPointZ to = ToPoint;
				return new Pnt2D(Math.Max(from.X, to.X), Math.Max(from.Y, to.Y));
			}
		}

		public override Pnt GetStart(bool as3D)
		{
			WKSPointZ from = FromPoint;
			return GetPoint(from, as3D);
		}

		public override Pnt GetEnd(bool as3D)
		{
			WKSPointZ to = ToPoint;
			return GetPoint(to, as3D);
		}

		public override IPnt GetPointAt(double fraction)
		{
			return GetPointAt(fraction, false);
		}

		public override IPnt GetPointAt(double fraction, bool as3D)
		{
			WKSPointZ from = FromPoint;
			WKSPointZ to = ToPoint;

			Pnt p0 = GetPoint(from, as3D);
			Pnt p1 = GetPoint(to, as3D);

			IPnt at = p0 + fraction * (p1 - p0);
			return at;
		}

		public override void QueryOffset(Pnt point, out double offset, out double along)
		{
			WKSPointZ from = FromPoint;
			WKSPointZ to = ToPoint;

			Pnt p0 = GetPoint(from, as3D: false);
			Pnt p1 = GetPoint(to, as3D: false);

			Pnt l0 = p1 - p0;
			Pnt l1 = point - p0;

			double vector = l0.VectorProduct(l1);
			double scalar = l0 * l1;

			double l2 = l0 * l0;
			along = scalar / l2;
			offset = vector / Length;
		}

		[NotNull]
		private static Pnt GetPoint(WKSPointZ wks, bool as3D)
		{
			Pnt p = ! as3D
				        ? (Pnt) new Pnt2D(wks.X, wks.Y)
				        : new Pnt3D(wks.X, wks.Y, wks.Z);
			return p;
		}
	}
}
