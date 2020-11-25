using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.Geometry
{
	internal class PatchProxy : IWKSPointCollection
	{
		[NotNull] private readonly WKSPointZ[] _points;

		private readonly int _minPartIndex;

		private readonly esriGeometryType _patchType;

		#region Constructors

		public PatchProxy(int patchIndex, int minPartIndex,
		                  [NotNull] IPointCollection4 baseGeometry)
		{
			PatchIndex = patchIndex;
			_minPartIndex = minPartIndex;
			SpatialReference = ((IGeometry) baseGeometry).SpatialReference;

			_points = new WKSPointZ[baseGeometry.PointCount];
			GeometryUtils.QueryWKSPointZs(baseGeometry, _points);

			_patchType = ((IGeometry) baseGeometry).GeometryType;
		}

		#endregion

		public IList<WKSPointZ> Points => _points;

		public ISpatialReference SpatialReference { get; }

		[PublicAPI]
		public int PatchIndex { get; }

		[NotNull]
		public IEnumerable<SegmentProxy> GetSegments()
		{
			return GetPlanes().SelectMany(plane => plane.GetSegments());
		}

		[NotNull]
		public IEnumerable<PlaneProxy> GetPlanes()
		{
			int planeCount = PlanesCount;
			for (int iPlane = 0; iPlane < planeCount; iPlane++)
			{
				yield return GetPlane(iPlane);
			}
		}

		[NotNull]
		public PlaneProxy GetPlane(int planeIndex)
		{
			PlaneProxy plane;
			switch (_patchType)
			{
				case esriGeometryType.esriGeometryRing:
					if (planeIndex != 0)
					{
						throw new InvalidOperationException("planeIndex out of range");
					}

					plane = new RingPlaneProxy(_minPartIndex, this);
					break;
				case esriGeometryType.esriGeometryTriangleFan:
					if (planeIndex < 0 || planeIndex >= _points.Length - 2)
					{
						throw new InvalidOperationException("planeIndex out of range");
					}

					plane = new TriPlaneProxy(_minPartIndex + planeIndex, this, 0,
					                          planeIndex + 1,
					                          planeIndex + 2);
					break;
				case esriGeometryType.esriGeometryTriangleStrip:
					if (planeIndex < 0 || planeIndex >= _points.Length - 2)
					{
						throw new InvalidOperationException("planeIndex out of range");
					}

					plane = new TriPlaneProxy(_minPartIndex + planeIndex, this,
					                          planeIndex,
					                          planeIndex + 1, planeIndex + 2);
					break;
				case esriGeometryType.esriGeometryTriangles:
					if (planeIndex < 0 || planeIndex >= _points.Length / 3)
					{
						throw new InvalidOperationException("planeIndex out of range");
					}

					int i = planeIndex * 3;
					plane = new TriPlaneProxy(_minPartIndex + planeIndex, this, i, i + 1,
					                          i + 2);
					break;
				default:
					throw new InvalidOperationException(
						"Invalid geometry type " + _patchType);
			}

			return plane;
		}

		public int PlanesCount
		{
			get
			{
				switch (_patchType)
				{
					case esriGeometryType.esriGeometryRing:
						return 1;

					case esriGeometryType.esriGeometryTriangleFan:
					case esriGeometryType.esriGeometryTriangleStrip:
						return _points.Length - 2;

					case esriGeometryType.esriGeometryTriangles:
						return _points.Length / 3;

					default:
						return 0;
				}
			}
		}
	}
}
