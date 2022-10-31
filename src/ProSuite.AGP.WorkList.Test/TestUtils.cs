using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Gdb;

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
				var definition =
					geodatabase.GetDefinition<FeatureClassDefinition>(featureClassName);
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

		public static Row GetRow(string path, string tableName, long oid)
		{
			var uri = new Uri(path, UriKind.Absolute);
			using (var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(uri)))
			{
				using (var table = geodatabase.OpenDataset<Table>(tableName))
				{
					return GdbQueryUtils.GetRow(table, oid);
				}
			}
		}

		public static void DeleteRow(string path, string featureClassName, long oid)
		{
			var uri = new Uri(path, UriKind.Absolute);
			using (var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(uri)))
			{
				using (var featureClass = geodatabase.OpenDataset<FeatureClass>(featureClassName))
				{
					var filter = new QueryFilter {ObjectIDs = new List<long> {oid}};
					featureClass.DeleteRows(filter);
				}
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
					if (featureClass.GetCount() != 0)
					{
						throw new InvalidOperationException();
					}
				}
			}
		}

		public static void UpdateDescription(string path, string featureClassName, int oid)
		{
			using (var geodatabase =
			       new Geodatabase(
				       new FileGeodatabaseConnectionPath(new Uri(path, UriKind.Absolute))))
			{
				using (var table = geodatabase.OpenDataset<Table>(featureClassName))
				{
					var row = GdbQueryUtils.GetRow(table, oid);
					row["Description"] = "Moe";
					row.Store();
				}
			}
		}

		public static void UpdateFeatureGeometry(string path, string featureClassName,
		                                         Geometry newGeometry, int oid)
		{
			using (var geodatabase =
			       new Geodatabase(
				       new FileGeodatabaseConnectionPath(new Uri(path, UriKind.Absolute))))
			{
				using (var featureClass = geodatabase.OpenDataset<FeatureClass>(featureClassName))
				{
					var feature = (Feature) GdbQueryUtils.GetRow(featureClass, oid);
					feature.SetShape(newGeometry);
					feature.Store();
				}
			}
		}
	}
}
