using System;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.GIS.Geodatabase.API;
using ProSuite.GIS.Geometry.AGP;
using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geodatabase.AGP
{
	public class ArcGeometryDef : IGeometryDef
	{
		private readonly ShapeDescription _shapeDescription;
		private readonly GeometryType? _geometryType;
		private readonly SpatialReference _spatialReference;
		private readonly bool? _hasZ;
		private readonly bool? _hasM;

		public ArcGeometryDef([NotNull] ShapeDescription shapeDescription)
		{
			_shapeDescription = shapeDescription;
		}

		public ArcGeometryDef([NotNull] FeatureClassDefinition featureClassDefinition)
			: this(featureClassDefinition.GetShapeType(),
			       featureClassDefinition.GetSpatialReference(), featureClassDefinition.HasZ(),
			       featureClassDefinition.HasM()) { }

		public ArcGeometryDef(GeometryType geometryType,
		                      SpatialReference spatialReference,
		                      bool hasZ,
		                      bool hasM)
		{
			_geometryType = geometryType;
			_spatialReference = spatialReference;
			_hasZ = hasZ;
			_hasM = hasM;
		}

		#region Implementation of IGeometryDef

		public int AvgNumPoints => throw new NotImplementedException();

		public esriGeometryType GeometryType =>
			(esriGeometryType) (_shapeDescription?.GeometryType ??
			                    _geometryType ??
			                    ArcGIS.Core.Geometry.GeometryType.Unknown);

		public double get_GridSize(int index)
		{
			throw new NotImplementedException();
		}

		public int GridCount => throw new NotImplementedException();

		public ISpatialReference SpatialReference => _shapeDescription != null
			                                             ? new ArcSpatialReference(
				                                             _shapeDescription.SpatialReference)
			                                             : new ArcSpatialReference(
				                                             _spatialReference);

		public bool HasZ => _shapeDescription?.HasZ ?? _hasZ ?? false;

		public bool HasM => _shapeDescription?.HasM ?? _hasM ?? false;

		#endregion
	}
}
