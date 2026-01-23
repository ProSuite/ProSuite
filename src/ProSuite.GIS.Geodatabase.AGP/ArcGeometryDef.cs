using System;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Geometry;
using ProSuite.GIS.Geometry.AGP;
using ProSuite.GIS.Geometry.API;
using API_IGeometryDef = ProSuite.GIS.Geodatabase.API.IGeometryDef;

namespace ProSuite.GIS.Geodatabase.AGP
{
	public class ArcGeometryDef : API_IGeometryDef
	{
		private readonly ShapeDescription _shapeDescription;
		private readonly GeometryType? _geometryType;
		private readonly SpatialReference _spatialReference;
		private readonly bool? _hasZ;
		private readonly bool? _hasM;

		public ArcGeometryDef(ShapeDescription shapeDescription)
		{
			_shapeDescription = shapeDescription;
		}

		/// <summary>
		/// Alternative constructor for enterprise geodatabases with qualified field names
		/// that would cause ShapeDescription to throw ArgumentException.
		/// </summary>
		public ArcGeometryDef(FeatureClassDefinition featureClassDefinition)
		{
			_shapeDescription = null;
			_geometryType = featureClassDefinition.GetShapeType();
			_spatialReference = featureClassDefinition.GetSpatialReference();
			_hasZ = featureClassDefinition.HasZ();
			_hasM = featureClassDefinition.HasM();
		}

		#region Implementation of IGeometryDef

		public int AvgNumPoints => throw new NotImplementedException();

		public esriGeometryType GeometryType => _shapeDescription != null
			? (esriGeometryType) _shapeDescription.GeometryType
			: (esriGeometryType) _geometryType.Value;

		public double get_GridSize(int index)
		{
			throw new NotImplementedException();
		}

		public int GridCount => throw new NotImplementedException();

		public ISpatialReference SpatialReference => _shapeDescription != null
			? new ArcSpatialReference(_shapeDescription.SpatialReference)
			: new ArcSpatialReference(_spatialReference);

		public bool HasZ => _shapeDescription != null
			? _shapeDescription.HasZ
			: _hasZ.Value;

		public bool HasM => _shapeDescription != null
			? _shapeDescription.HasM
			: _hasM.Value;

		#endregion
	}
}
