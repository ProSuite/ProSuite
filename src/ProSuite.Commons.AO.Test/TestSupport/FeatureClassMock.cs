using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Test.TestSupport
{
	public class FeatureClassMock : ObjectClassMock, IReadOnlyFeatureClass, IFeatureClass,
	                                IGeoDataset
	{
		private const int _gridSize = 1000;
		private readonly bool _hasM;
		private readonly bool _hasZ;
		private const string _shapeFieldName = "SHAPE";
		[NotNull] private readonly ISpatialReference _spatialReference;
		private readonly GeometryBuilder _geometryBuilder;

		#region Constructors

		//public FeatureClassMock(string name,
		//                        esriGeometryType shapeType,
		//                        int? objectClassId = null)
		//	: this(name, name, shapeType, objectClassId) { }

		//public FeatureClassMock(string name,
		//						string aliasName,
		//						esriGeometryType shapeType,
		//						int? objectClassId = null)
		//	: this(name, aliasName, shapeType,
		//		   objectClassId,
		//		   esriFeatureType.esriFTSimple,
		//		   CreateDefaultSpatialReference())
		//{ }

		public FeatureClassMock(string name,
		                        esriGeometryType shapeType,
		                        int? objectClassId = null,
		                        esriFeatureType featureType = esriFeatureType.esriFTSimple)
			: this(name, name, shapeType, objectClassId, featureType) { }

		public FeatureClassMock(string name,
		                        string aliasName,
		                        esriGeometryType shapeType,
		                        int? objectClassId = null,
		                        esriFeatureType featureType = esriFeatureType.esriFTSimple)
			: this(name, aliasName, shapeType, objectClassId,
			       featureType, CreateDefaultSpatialReference()) { }

		public FeatureClassMock(string name,
		                        esriGeometryType shapeType,
		                        int? objectClassId,
		                        esriFeatureType featureType,
		                        [NotNull] ISpatialReference spatialReference,
		                        bool hasZ = true,
		                        bool hasM = false)
			: this(name, name, shapeType, objectClassId, featureType,
			       spatialReference, hasZ, hasM) { }

		public FeatureClassMock(string name,
		                        string aliasName,
		                        esriGeometryType shapeType,
		                        int? objectClassId,
		                        esriFeatureType featureType,
		                        [NotNull] ISpatialReference spatialReference,
		                        bool hasZ = true,
		                        bool hasM = false)
			: base(name, aliasName, objectClassId)
		{
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			ShapeType = shapeType;
			FeatureType = featureType;
			_spatialReference = spatialReference;
			_hasZ = hasZ;
			_hasM = hasM;

			_geometryBuilder =
				new GeometryBuilder(spatialReference, shapeType, hasZ, hasM);

			ShapeField = CreateShapeField();
			AddFields(ShapeField);
		}

		#endregion

		public IField ShapeField { get; }

		#region IGeoDataset Members

		ISpatialReference IReadOnlyGeoDataset.SpatialReference => _spatialReference;
		ISpatialReference IGeoDataset.SpatialReference => _spatialReference;

		IEnvelope IReadOnlyGeoDataset.Extent => Extent;
		IEnvelope IGeoDataset.Extent => Extent;
		protected virtual IEnvelope Extent => throw new NotImplementedException();

		#endregion

		#region IFeatureClass Members

		IFeature IFeatureClass.CreateFeature() => CreateFeature();

		public FeatureMock CreateFeature()
		{
			return CreateFeature(GetNextOID());
		}

		private FeatureMock CreateFeature(int oid)
		{
			return new FeatureMock(oid, this);
		}

		public override IObject CreateObject(int oid)
		{
			return CreateFeature(oid);
		}

		public IRow CreateRow()
		{
			return CreateFeature();
		}

#if Server11 || ARCGIS_12_0_OR_GREATER
		IFeature IFeatureClass.GetFeature(long OID) => throw new NotImplementedException();
#else
		IFeature IFeatureClass.GetFeature(int OID) => throw new NotImplementedException();
#endif

		public IFeatureCursor GetFeatures(object fids, bool Recycling)
		{
			throw new NotImplementedException();
		}

		public IFeatureBuffer CreateFeatureBuffer()
		{
			throw new NotImplementedException();
		}

#if Server11 || ARCGIS_12_0_OR_GREATER
		long IFeatureClass.FeatureCount(IQueryFilter QueryFilter) =>
			throw new NotImplementedException();
#else
		int IFeatureClass.FeatureCount(IQueryFilter QueryFilter) => throw new NotImplementedException();
#endif

		IFeatureCursor IFeatureClass.Search(IQueryFilter filter, bool Recycling) =>
			(IFeatureCursor) Search(filter, Recycling);

		public IFeatureCursor Update(IQueryFilter filter, bool Recycling)
		{
			throw new NotImplementedException();
		}

		public IFeatureCursor Insert(bool useBuffering)
		{
			throw new NotImplementedException();
		}

		public ISelectionSet Select(IQueryFilter QueryFilter,
		                            esriSelectionType selType,
		                            esriSelectionOption selOption,
		                            IWorkspace selectionContainer)
		{
			throw new NotImplementedException();
		}

		public esriGeometryType ShapeType { get; }

		public esriFeatureType FeatureType { get; }

		public string ShapeFieldName => _shapeFieldName;

		public IField AreaField { get; set; }

		public IField LengthField { get; set; }

		public IFeatureDataset FeatureDataset
		{
			get { throw new NotImplementedException(); }
		}

		public int FeatureClassID => ObjectClassID;

		#endregion

		public IGeometry CreateGeometry(params Pt[] points)
		{
			return _geometryBuilder.CreateGeometry(points);
			//IGeometry geometry = CreateEmptyGeometry();

			//IPointCollection pointCollection = geometry as IPointCollection;

			//if (pointCollection != null)
			//{
			//    object missing = Type.Missing;
			//    foreach (Pt pt in points)
			//    {
			//        pointCollection.AddPoint(pt.CreatePoint(), ref missing, ref missing);
			//    }
			//}
			//else
			//{
			//    IPoint point = geometry as IPoint;

			//    if (point != null)
			//    {
			//        Assert.AreEqual(1, points.Length, "Invalid point count");
			//        points[0].ConfigurePoint(point);
			//    }
			//}

			//ITopologicalOperator topoOp = geometry as ITopologicalOperator;
			//if (topoOp != null)
			//{
			//    // topoOp.Simplify();
			//}

			//return geometry;
		}

		public IGeometry CreateEmptyGeometry()
		{
			return _geometryBuilder.CreateEmptyGeometry();
			//IGeometryFactory3 factory = new GeometryEnvironmentClass();
			//IGeometry geometry;
			//factory.CreateEmptyGeometryByType(_shapeType, out geometry);

			//ConfigureGeometry(geometry);

			//return geometry;
		}

		public void ConfigureGeometry(IGeometry geometry)
		{
			_geometryBuilder.ConfigureGeometry(geometry);
			//Assert.NotNull(geometry);

			//if (_hasZ)
			//{
			//    ((IZAware) geometry).ZAware = true;
			//}

			//if (_hasM)
			//{
			//    ((IMAware) geometry).MAware = true;
			//}

			//geometry.SpatialReference = _spatialReference;
		}

		public IFeature CreateFeature(IFeature prototype, bool withNewOid = false)
		{
			IFeature result = withNewOid
				                  ? CreateFeature()
				                  : new FeatureMock(prototype.OID, this);

			result.Shape = prototype.ShapeCopy;

			for (var i = 0; i < result.Fields.FieldCount; i++)
			{
				result.set_Value(i, prototype.Value[i]);
			}

			return result;
		}

		public IFeature CreateFeature(params Pt[] points)
		{
			return CreateFeature(CreateGeometry(points));
		}

		public IFeature CreateFeature(IGeometry geometry)
		{
			IFeature feature = CreateFeature();

			if (geometry.SpatialReference == null)
			{
				geometry.SpatialReference = _spatialReference;
			}

			feature.Shape = geometry;
			return feature;
		}

		protected override esriDatasetType GetDatasetType()
		{
			return esriDatasetType.esriDTFeatureClass;
		}

		private IField CreateShapeField()
		{
			return FieldUtils.CreateShapeField("SHAPE", ShapeType,
			                                   _spatialReference,
			                                   _gridSize, _hasZ, _hasM);
		}

		private static ISpatialReference CreateDefaultSpatialReference()
		{
			var spatialReference =
				(ISpatialReference3)
				SpatialReferenceUtils.CreateSpatialReference(
					WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			var controlPrecision = (IControlPrecision2) spatialReference;

			//Determines whether you are constructing a high or low.
			controlPrecision.IsHighPrecision = true;
			var spatialReferenceResolution =
				(ISpatialReferenceResolution) spatialReference;

			//These three methods are the keys, construct horizon, then set the default x,y resolution and tolerance.
			spatialReferenceResolution.ConstructFromHorizon();

			//Set the default x,y resolution value.
			spatialReferenceResolution.SetDefaultXYResolution();

			spatialReferenceResolution.set_XYResolution(true, 0.00001);
			spatialReferenceResolution.set_ZResolution(true, 0.00001);

			//Set the default x,y tolerance value.
			var spatialReferenceTolerance = (ISpatialReferenceTolerance) spatialReference;

			spatialReferenceTolerance.XYTolerance = 0.0001;
			spatialReferenceTolerance.ZTolerance = 0.0001;

			return spatialReference;
		}
	}
}
