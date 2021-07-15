using System.ComponentModel;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.SchemaInfo
{
	public class FeatureClassProperties : ObjectClassProperties
	{
		public FeatureClassProperties([NotNull] IFeatureClass featureClass)
			: base(featureClass)
		{
			ShapeType = GetGeometryTypeText(featureClass.ShapeType);
			FeatureType = GetFeatureTypeText(featureClass.FeatureType);
			FeatureDataset = featureClass.FeatureDataset == null
				                 ? string.Empty
				                 : featureClass.FeatureDataset.Name;
			HasM = DatasetUtils.HasM(featureClass);
			HasZ = DatasetUtils.HasZ(featureClass);

			var geoDataset = featureClass as IGeoDataset;
			if (geoDataset != null)
			{
				if (geoDataset.Extent != null)
				{
					Extent = new ExtentProperties(geoDataset.Extent);
				}

				if (geoDataset.SpatialReference != null)
				{
					SpatialReference = new SpatialReferenceProperties(geoDataset.SpatialReference);
				}
			}
		}

		[Category(PropertyCategories.FeatureClass)]
		[DisplayName("Feature Dataset")]
		[UsedImplicitly]
		public string FeatureDataset { get; private set; }

		[Category(PropertyCategories.FeatureClass)]
		[DisplayName("Feature Type")]
		[UsedImplicitly]
		public string FeatureType { get; private set; }

		[Category(PropertyCategories.Geometry)]
		[DisplayName("Shape Type")]
		[UsedImplicitly]
		public string ShapeType { get; private set; }

		[Category(PropertyCategories.Geometry)]
		[DisplayName("Has Z values")]
		[UsedImplicitly]
		public bool HasZ { get; private set; }

		[Category(PropertyCategories.Geometry)]
		[DisplayName("Has M values")]
		[UsedImplicitly]
		public bool HasM { get; private set; }

		[Category(PropertyCategories.Geometry)]
		[UsedImplicitly]
		[TypeConverter(typeof(AllPropertiesConverter))]
		public ExtentProperties Extent { get; private set; }

		[Category(PropertyCategories.Geometry)]
		[DisplayName("Spatial Reference")]
		[UsedImplicitly]
		[TypeConverter(typeof(AllPropertiesConverter))]
		public SpatialReferenceProperties SpatialReference { get; private set; }

		[NotNull]
		private static string GetFeatureTypeText(esriFeatureType featureType)
		{
			switch (featureType)
			{
				case esriFeatureType.esriFTSimple:
					return "Simple";

				case esriFeatureType.esriFTSimpleJunction:
					return "Simple Junction";

				case esriFeatureType.esriFTSimpleEdge:
					return "Simple Edge";

				case esriFeatureType.esriFTComplexJunction:
					return "Complex Junction";

				case esriFeatureType.esriFTComplexEdge:
					return "Complex Edge";

				case esriFeatureType.esriFTAnnotation:
					return "Annotation";

				case esriFeatureType.esriFTCoverageAnnotation:
					return "Coverage Annotation";

				case esriFeatureType.esriFTDimension:
					return "Dimension";

				case esriFeatureType.esriFTRasterCatalogItem:
					return "Raster Catalog Item";

				default:
					return featureType.ToString();
			}
		}

		[NotNull]
		private static string GetGeometryTypeText(esriGeometryType shapeType)
		{
			switch (shapeType)
			{
				case esriGeometryType.esriGeometryPoint:
					return "Point";

				case esriGeometryType.esriGeometryMultipoint:
					return "Multipoint";

				case esriGeometryType.esriGeometryPolyline:
					return "Polyline";

				case esriGeometryType.esriGeometryPolygon:
					return "Polygon";

				case esriGeometryType.esriGeometryMultiPatch:
					return "Multipatch";

				default:
					return shapeType.ToString();
			}
		}
	}
}
