using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	/// <summary>
	/// An intelligent moniker for a dataset in a geodatabase, which can be used to identify
	/// the dataset without having to open it. Can be used to fulfil the AO IName interfaces.
	/// </summary>
	public class GdbDatasetMoniker
	{
		public GdbDatasetMoniker([NotNull] string name,
		                         esriDatasetType datasetType,
		                         int objectClassId = -1,
		                         string aliasName = null,
		                         string category = null,
		                         esriGeometryType shapeType = esriGeometryType.esriGeometryNull,
		                         string shapeFieldName = null,
		                         esriFeatureType featureType = esriFeatureType.esriFTSimple)
		{
			Name = name;
			DatasetType = datasetType;
			ObjectClassId = objectClassId;
			AliasName = aliasName;
			Category = category;
			ShapeType = shapeType;
			ShapeFieldName = shapeFieldName;
			FeatureType = featureType;
		}

		[NotNull]
		public string Name { get; }

		public esriDatasetType DatasetType { get; }

		public int ObjectClassId { get; }

		[CanBeNull]
		public string AliasName { get; }

		[CanBeNull]
		public string Category { get; }

		public esriGeometryType ShapeType { get; }

		[CanBeNull]
		public string ShapeFieldName { get; }

		public esriFeatureType FeatureType { get; }

		public bool IsFeatureClass => DatasetType == esriDatasetType.esriDTFeatureClass;

		[NotNull]
		public static GdbDatasetMoniker FromTable([NotNull] GdbTable table)
		{
			esriGeometryType shapeType = esriGeometryType.esriGeometryNull;
			string shapeFieldName = null;
			esriFeatureType featureType = esriFeatureType.esriFTSimple;

			if (table is IFeatureClass featureClass)
			{
				shapeType = featureClass.ShapeType;
				shapeFieldName = featureClass.ShapeFieldName;
				featureType = featureClass.FeatureType;
			}

			return new GdbDatasetMoniker(
				table.Name,
				table.DatasetType,
				table.ObjectClassID,
				table.AliasName,
				null,
				shapeType,
				shapeFieldName,
				featureType);
		}
	}
}
