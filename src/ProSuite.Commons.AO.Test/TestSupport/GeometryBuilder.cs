using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Test.TestSupport
{
	public class GeometryBuilder
	{
		private readonly bool _hasM;
		private readonly bool _hasZ;
		private readonly esriGeometryType _shapeType;
		[NotNull] private readonly ISpatialReference _spatialReference;

		/// <summary>
		/// Initializes a new instance of the <see cref="GeometryBuilder"/> class.
		/// </summary>
		/// <param name="spatialReference">The spatial reference.</param>
		/// <param name="shapeType">Type of the shape.</param>
		/// <param name="hasZ">if set to <c>true</c> the created geometries are Z aware.</param>
		/// <param name="hasM">if set to <c>true</c> the created geometries are M aware.</param>
		public GeometryBuilder([NotNull] ISpatialReference spatialReference,
		                       esriGeometryType shapeType,
		                       bool hasZ = false,
		                       bool hasM = false)
		{
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			_spatialReference = spatialReference;
			_shapeType = shapeType;
			_hasZ = hasZ;
			_hasM = hasM;
		}

		public bool SimplifyResult { get; set; } = true;

		[NotNull]
		public IGeometry CreateGeometry(params Part[] parts)
		{
			var partGeometries = new IGeometry[parts.Length];

			for (var i = 0; i < parts.Length; i++)
			{
				partGeometries[i] = CreateGeometry(parts[i].Points);
			}

			return GeometryUtils.UnionGeometries(partGeometries);
		}

		[NotNull]
		public IGeometry CreateGeometry(params Pt[] points)
		{
			return CreateGeometry(new List<Pt>(points));
		}

		[NotNull]
		private IGeometry CreateGeometry([NotNull] IList<Pt> points)
		{
			IGeometry geometry = CreateEmptyGeometry();

			if (_shapeType == esriGeometryType.esriGeometryMultiPatch)
			{
				geometry = CreateEmptyGeometry(esriGeometryType.esriGeometryPolygon);
			}

			FillGeometry(geometry, points);

			if (SimplifyResult)
			{
				var topoOp = geometry as ITopologicalOperator;
				topoOp?.Simplify();
			}

			if (_shapeType == esriGeometryType.esriGeometryMultiPatch)
			{
				geometry = GeometryFactory.CreateMultiPatch((IPolygon) geometry);
			}

			return geometry;
		}

		private void FillGeometry(IGeometry emptyGeometry, IList<Pt> points)
		{
			var pointCollection = emptyGeometry as IPointCollection;

			if (pointCollection != null)
			{
				object missing = Type.Missing;
				foreach (Pt pt in points)
				{
					pointCollection.AddPoint(pt.CreatePoint(), ref missing, ref missing);
				}
			}
			else
			{
				var point = emptyGeometry as IPoint;

				if (point != null)
				{
					Assert.AreEqual(1, points.Count, "Invalid point count");
					points[0].ConfigurePoint(point);
				}

				else
				{
					throw new NotSupportedException(
						$"Unsupported shape type: {_shapeType}");
				}
			}
		}

		[NotNull]
		public IGeometry CreateEmptyGeometry()
		{
			esriGeometryType geometryType = _shapeType;

			return CreateEmptyGeometry(geometryType);
		}

		private IGeometry CreateEmptyGeometry(esriGeometryType geometryType)
		{
			var factory = (IGeometryFactory3) GeometryUtils.GeometryBridge;
			IGeometry geometry;
			factory.CreateEmptyGeometryByType(geometryType, out geometry);

			ConfigureGeometry(geometry);

			return geometry;
		}

		public void ConfigureGeometry([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			if (_hasZ)
			{
				((IZAware) geometry).ZAware = true;
			}

			if (_hasM)
			{
				((IMAware) geometry).MAware = true;
			}

			geometry.SpatialReference = _spatialReference;
		}
	}
}
