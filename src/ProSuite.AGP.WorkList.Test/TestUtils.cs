using System;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using NUnit.Framework;

namespace ProSuite.AGP.WorkList.Test
{
	public static class TestUtils
	{
		public static void InsertRows(string path,
		                              string featureClassName, Geometry polygon, int rowCount)
		{
			var uri = new Uri(path, UriKind.Absolute);
			using (var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(uri)))
			{
				var definition = geodatabase.GetDefinition<FeatureClassDefinition>(featureClassName);
				string shapeField = definition.GetShapeField();

				using (var featureClass = geodatabase.OpenDataset<FeatureClass>(featureClassName))
				{
					for (var i = 0; i < rowCount; i++)
					{
						object value = "Bart";
						InsertRow(featureClass, shapeField, polygon, value);
					}
				}
			}
		}

		private static void InsertRow(FeatureClass featureClass, string shapeField,
		                              Geometry polygon,
		                              object description)
		{
			using (RowBuffer buffer = featureClass.CreateRowBuffer())
			{
				buffer[shapeField] = polygon;
				buffer["Description"] = description;
				featureClass.CreateRow(buffer);
			}
		}

		public static void DeleteAllRows(string path, string featureClassName)
		{
			var uri = new Uri(path, UriKind.Absolute);
			using (var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(uri)))
			{
				using (var featureClass = geodatabase.OpenDataset<FeatureClass>(featureClassName))
				{
					// delete all
					featureClass.DeleteRows(new QueryFilter());
					Assert.True(featureClass.GetCount() == 0);
				}
			}
		}
	}
}