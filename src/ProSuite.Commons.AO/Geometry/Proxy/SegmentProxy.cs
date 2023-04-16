using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using System;

namespace ProSuite.Commons.AO.Geometry.Proxy
{
	public abstract class SegmentProxy : ISegmentProxy
	{
		private Box _extent;

		//public IPoint Start;
		//public IPoint End;

		/// <summary>
		/// Initializes a new instance of the <see cref="SegmentProxy"/> class.
		/// </summary>
		/// <param name="partIndex">Index of the part.</param>
		/// <param name="segmentIndex">Index of the segment.</param>
		protected SegmentProxy(int partIndex, int segmentIndex)
		{
			PartIndex = partIndex;
			SegmentIndex = segmentIndex;
		}

		public int Dimension => 2;

		public override string ToString()
		{
			return $"Part: {PartIndex} Segment: {SegmentIndex}, Min {Min} Max {Max}";
		}

		[CanBeNull]
		public abstract ISpatialReference SpatialReference { get; }

		[NotNull]
		public IBox Extent => _extent ?? (_extent = new Box(Pnt.Create(Min),
															Pnt.Create(Max)));

		public SegmentHull CreateHull(double offset)
		{
			return new SegmentHull(this, offset, new RoundCap(), new RoundCap());
		}
		//public IQaGeometry Border
		//{
		//    get { throw new NotImplementedException(); }
		//}

		//public bool Intersects(IBox box)
		//{
		//    var thisBox = new Box(Point.Create(Min), Point.Create(Max));
		//    return thisBox.Intersects(box);
		//}

		public abstract double Length { get; }

		// TODO not yet implemented on WksSegmentProxy, not yet used (to be used in QaPartCoincidenceBase)
		[NotNull]
		public abstract SegmentProxy GetSubCurve(double fromRatio, double toRatio);

		public abstract WKSEnvelope GetSubCurveBox(double fromRatio, double toRatio);

		[NotNull]
		public IPolygon CreateBuffer(double distance)
		{
			const bool forceCreation = false;
			IPolyline polyLine = GetPolyline(forceCreation);

			IPolyline bufferInput;
			if (GeometryUtils.HasNonLinearSegments(polyLine))
			{
				if (polyLine.Length > 0)
				{
					bufferInput = GeometryFactory.Clone(polyLine);

					double xyTolerance = GeometryUtils.GetXyTolerance(polyLine);
					double xyResolution = GeometryUtils.GetXyResolution(polyLine);

					// note: buffering a non-linear segments always results in a densified buffer
					// apparently, the input is first densified with a large max. densify deviation
					// --> the input may not be contained in the buffer for small buffer distances
					// --> we need to densify the input ourselves to an appropriate max. deviation value

					double densifyDeviation = Math.Min(xyTolerance / 2, distance / 2);
					double minimumAllowedDeviation = xyResolution * 2.01;

					if (densifyDeviation < minimumAllowedDeviation)
					{
						densifyDeviation = minimumAllowedDeviation;
					}

					bufferInput.Densify(0, densifyDeviation);
				}
				else
				{
					bufferInput = GeometryFactory.CreateLine(polyLine.FromPoint, polyLine.ToPoint);
				}
			}
			else
			{
				bufferInput = polyLine;
			}

			using (var factory = new BufferFactory(explodeBuffers: false, densify: false))
			{
				IPolygon result = null;
				foreach (IPolygon polygon in factory.Buffer(bufferInput, distance))
				{
					Assert.Null(result, "more than 1 buffer geometry found");

					result = polygon;
				}

				return Assert.NotNull(result, "no buffer geometry found");
			}
		}

		[NotNull]
		public abstract IPnt Min { get; }

		[NotNull]
		public abstract IPnt Max { get; }

		public abstract bool IsLinear { get; }

		[NotNull]
		public abstract Pnt GetStart(bool as3D);

		[NotNull]
		public abstract Pnt GetEnd(bool as3D);

		[NotNull]
		public abstract IPnt GetPointAt(double fraction);

		[NotNull]
		public abstract IPnt GetPointAt(double fraction, bool as3D);

		public abstract void QueryOffset(Pnt point, out double offset, out double along);

		// TODO, isn't there an alternative to those NotImplementedExceptions? 
		// is it really IBox that needs to be implemented here?

		//double IBox.GetMaxExtent()
		//{
		//    throw new NotImplementedException();
		//}

		//IBox IBox.Clone()
		//{
		//    throw new NotImplementedException();
		//}

		//bool IBox.IsWithin(IBox box)
		//{
		//    throw new NotImplementedException();
		//}

		//bool IBox.IsWithin(IBox box, int[] dimensionList)
		//{
		//    throw new NotImplementedException();
		//}

		//bool IBox.IsWithin(IQaPoint point)
		//{
		//    throw new NotImplementedException();
		//}

		//bool IBox.IsWithin(IQaPoint point, int[] dimensionList)
		//{
		//    throw new NotImplementedException();
		//}

		//void IBox.Include(IBox box)
		//{
		//    throw new NotImplementedException();
		//}

		public int PartIndex { get; }

		public int SegmentIndex { get; }

		[NotNull]
		public abstract IPolyline GetPolyline(bool forceCreation);

		[CanBeNull]
		public ISegmentProxy GetPreviousSegment([NotNull] IGeometry geometry)
		{
			int index = SegmentIndex - 1;

			var geomColl = geometry as IGeometryCollection;
			ISegmentCollection segColl = geomColl == null
											 ? (ISegmentCollection)geometry
											 : (ISegmentCollection)
											 geomColl.Geometry[PartIndex];
			var path = (IPath)segColl;

			if (index < 0)
			{
				if (!path.IsClosed)
				{
					return null;
				}

				index = segColl.SegmentCount - 1;
			}

			var previousSegment = new DummySegmentProxy(PartIndex, index);

			return previousSegment;
		}

		[CanBeNull]
		public ISegmentProxy GetNextSegment([NotNull] IGeometry geometry)
		{
			int index = SegmentIndex + 1;

			var geomColl = geometry as IGeometryCollection;
			ISegmentCollection segColl = geomColl == null
											 ? (ISegmentCollection)geometry
											 : (ISegmentCollection)geomColl.Geometry[PartIndex];
			var path = (IPath)segColl;

			if (index >= segColl.SegmentCount)
			{
				if (!path.IsClosed)
				{
					return null;
				}

				index = 0;
			}

			var nextSegment = new DummySegmentProxy(PartIndex, index);

			return nextSegment;
		}

		public abstract double GetDirectionAt(double fraction);
	}
}
