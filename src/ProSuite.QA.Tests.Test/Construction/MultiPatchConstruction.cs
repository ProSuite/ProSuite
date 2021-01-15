using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;

namespace ProSuite.QA.Tests.Test.Construction
{
	[CLSCompliant(false)]
	public class MultiPatchConstruction
	{
		private readonly IMultiPatch _multiPatch;
		private IGeometry _currentPatch;
		private esriMultiPatchRingType _currentRingType;

		public MultiPatchConstruction()
		{
			_multiPatch = new MultiPatchClass();
		}

		public IMultiPatch MultiPatch
		{
			get
			{
				EndPatch();
				return _multiPatch;
			}
		}

		private IPoint CreatePoint(double x, double y, double z, int id)
		{
			((IZAware) _multiPatch).ZAware = true;

			((IPointIDAware) _multiPatch).PointIDAware = true;
			IPoint p = GeometryFactory.CreatePoint(x, y, z);
			((IPointIDAware) p).PointIDAware = true;
			p.ID = id;
			return p;
		}

		public MultiPatchConstruction StartRing(double x, double y, double z)
		{
			return StartRing(GeometryFactory.CreatePoint(x, y, z));
		}

		public MultiPatchConstruction StartRing(double x, double y, double z, int id)
		{
			return StartRing(CreatePoint(x, y, z, id));
		}

		public MultiPatchConstruction StartRing(IPoint p)
		{
			EndPatch();
			_currentPatch = new RingClass();
			_currentRingType = esriMultiPatchRingType.esriMultiPatchRing;

			var newRing = (IPointCollection) _currentPatch;
			object missing = Type.Missing;
			newRing.AddPoint(p, ref missing, ref missing);

			return this;
		}

		public MultiPatchConstruction StartOuterRing(double x, double y, double z)
		{
			return StartOuterRing(GeometryFactory.CreatePoint(x, y, z));
		}

		public MultiPatchConstruction StartOuterRing(double x, double y, double z, int id)
		{
			return StartOuterRing(CreatePoint(x, y, z, id));
		}

		public MultiPatchConstruction StartOuterRing(IPoint p)
		{
			StartRing(p);
			_currentRingType = esriMultiPatchRingType.esriMultiPatchOuterRing;
			return this;
		}

		public MultiPatchConstruction StartInnerRing(double x, double y, double z)
		{
			return StartInnerRing(GeometryFactory.CreatePoint(x, y, z));
		}

		public MultiPatchConstruction StartInnerRing(double x, double y, double z, int id)
		{
			return StartInnerRing(CreatePoint(x, y, z, id));
		}

		public MultiPatchConstruction StartInnerRing(IPoint p)
		{
			StartRing(p);
			_currentRingType = esriMultiPatchRingType.esriMultiPatchInnerRing;

			return this;
		}

		public MultiPatchConstruction StartFirstRing(double x, double y, double z)
		{
			return StartFirstRing(GeometryFactory.CreatePoint(x, y, z));
		}

		public MultiPatchConstruction StartFirstRing(double x, double y, double z, int id)
		{
			return StartFirstRing(CreatePoint(x, y, z, id));
		}

		public MultiPatchConstruction StartFirstRing(IPoint p)
		{
			StartRing(p);
			_currentRingType = esriMultiPatchRingType.esriMultiPatchFirstRing;

			return this;
		}

		public MultiPatchConstruction StartFan(double x, double y, double z)
		{
			return StartFan(GeometryFactory.CreatePoint(x, y, z));
		}

		public MultiPatchConstruction StartFan(double x, double y, double z, int id)
		{
			return StartFan(CreatePoint(x, y, z, id));
		}

		public MultiPatchConstruction StartFan(IPoint p)
		{
			EndPatch();
			object missing = Type.Missing;

			IPointCollection newFan = new TriangleFanClass();
			newFan.AddPoint(p, ref missing, ref missing);

			_currentPatch = (IGeometry) newFan;

			return this;
		}

		public MultiPatchConstruction StartStrip(double x, double y, double z)
		{
			return StartStrip(GeometryFactory.CreatePoint(x, y, z));
		}

		public MultiPatchConstruction StartStrip(double x, double y, double z, int id)
		{
			return StartStrip(CreatePoint(x, y, z, id));
		}

		public MultiPatchConstruction StartStrip(IPoint p)
		{
			EndPatch();
			object missing = Type.Missing;

			IPointCollection newStrip = new TriangleStripClass();
			newStrip.AddPoint(p, ref missing, ref missing);

			_currentPatch = (IGeometry) newStrip;

			return this;
		}

		public MultiPatchConstruction StartTris(double x, double y, double z)
		{
			return StartTris(GeometryFactory.CreatePoint(x, y, z));
		}

		public MultiPatchConstruction StartTris(double x, double y, double z, int id)
		{
			return StartTris(CreatePoint(x, y, z, id));
		}

		public MultiPatchConstruction StartTris(IPoint p)
		{
			EndPatch();
			object missing = Type.Missing;

			IPointCollection newTris = new TrianglesClass();
			newTris.AddPoint(p, ref missing, ref missing);

			_currentPatch = (IGeometry) newTris;

			return this;
		}

		public MultiPatchConstruction Add(double x, double y, double z)
		{
			return Add(GeometryFactory.CreatePoint(x, y, z));
		}

		public MultiPatchConstruction Add(double x, double y, double z, int id)
		{
			return Add(CreatePoint(x, y, z, id));
		}

		public MultiPatchConstruction Add(IPoint point)
		{
			var geometry = (IPointCollection) _currentPatch;
			object missing = Type.Missing;
			geometry.AddPoint(point, ref missing, ref missing);

			return this;
		}

		private void EndPatch()
		{
			object missing = Type.Missing;
			if (_currentPatch == null)
			{
				return;
			}

			var geometryCollection = (IGeometryCollection) _multiPatch;
			var ring = _currentPatch as IRing;
			if (ring != null)
			{
				((IZAware) ring).ZAware = true;
				if (! ring.IsClosed)
				{
					ring.Close();
				}
				else if (ring.FromPoint.Z != ring.ToPoint.Z)
				{
					((IPointCollection) ring).AddPoint(ring.FromPoint, ref missing, ref missing);
				}
			}

			geometryCollection.AddGeometry(_currentPatch, ref missing, ref missing);

			if (ring != null && _currentRingType != esriMultiPatchRingType.esriMultiPatchRing)
			{
				_multiPatch.PutRingType(ring, _currentRingType);
			}

			_currentPatch = null;
		}
	}
}
