using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase.AGP.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class ArcQueryTableTest
	{
		private ArcGIS.Core.Data.Geodatabase _gdb;

		private static readonly SpatialReference _sr = SpatialReferences.WGS84;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			CoreHostProxy.Initialize();

			_gdb = SchemaBuilder.CreateGeodatabase(
				new MemoryConnectionProperties("ArcQueryTableTest"));
		}

		[OneTimeTearDown]
		public void OneTimeTearDown()
		{
			_gdb?.Dispose();
		}

		[Test]
		public void Learning_test_query_table_shape_field_matches_by_name_or_qualification()
		{
			using FeatureClass queryFeatureClass =
				CreateJoinedQueryFeatureClass("LEARN_STREET", "LEARN_SIGN", "LEARN_STREET_SIGN_Q");

			Assert.False(queryFeatureClass.IsJoinedTable(),
			             "Database-level query tables are not exposed as joined tables.");

			using FeatureClassDefinition definition = queryFeatureClass.GetDefinition();

			string shapeFieldName = definition.GetShapeField();
			List<string> fieldNames =
				definition.GetFields().Select(field => field.Name).ToList();

			Assert.True(
				fieldNames.Any(fieldName => NamesMatch(fieldName, shapeFieldName)),
				$"Query table shape field '{shapeFieldName}' does not match any actual field. " +
				$"Fields: {string.Join(", ", fieldNames)}");
		}

		[Test]
		public void ToArcTable_keeps_query_table_shape_field_usable()
		{
			using FeatureClass queryFeatureClass =
				CreateJoinedQueryFeatureClass("WRAP_STREET", "WRAP_SIGN", "WRAP_STREET_SIGN_Q");

			var arcFeatureClass = (ArcFeatureClass)
				ArcGeodatabaseUtils.ToArcTable((Table) queryFeatureClass, keepJoins: true);

			string shapeFieldName = null;
			int shapeFieldIndex = -1;

			Assert.DoesNotThrow(() =>
			{
				shapeFieldName = arcFeatureClass.ShapeFieldName;
				shapeFieldIndex = arcFeatureClass.FindField(shapeFieldName);
			});

			Assert.False(string.IsNullOrEmpty(shapeFieldName));
			Assert.False(shapeFieldIndex < 0,
			             $"Shape field '{shapeFieldName}' could not be resolved in the wrapped query table.");

			IFeature feature = ((IFeatureClass) arcFeatureClass).Search(null, false).Single();

			Assert.NotNull(feature.ShapeCopy,
			               "The wrapped query table must expose the geometry through ShapeCopy.");
		}

		private FeatureClass CreateJoinedQueryFeatureClass(
			string featureClassName,
			string joinTableName,
			string queryTableName)
		{
			FeatureClass featureClass = DatasetUtils.CreateFeatureClass(
				_gdb, featureClassName,
				new List<FieldDescription>
				{
					new FieldDescription("UUID", FieldType.GUID),
					new FieldDescription("NAME", FieldType.String)
				},
				GeometryType.Point, _sr, hasZ: false);

			Table joinTable = CreateTable(
				joinTableName,
				new FieldDescription("STREET_FK", FieldType.GUID),
				new FieldDescription("KIND", FieldType.Integer));

			Guid joinKey = Guid.NewGuid();
			InsertFeature(featureClass, joinKey);
			InsertJoinRow(joinTable, joinKey);

			using FeatureClassDefinition featureClassDefinition = featureClass.GetDefinition();

			string featureClassDatasetName = featureClass.GetName();
			string joinDatasetName = joinTable.GetName();
			string objectIdFieldName = featureClassDefinition.GetObjectIDField();
			string shapeFieldName = featureClassDefinition.GetShapeField();

			var queryDef = new QueryDef
			               {
				               Tables =
					               $"{featureClassDatasetName} INNER JOIN {joinDatasetName} " +
					               $"ON {featureClassDatasetName}.UUID = {joinDatasetName}.STREET_FK",
				               SubFields =
					               $"{featureClassDatasetName}.UUID," +
					               $"{featureClassDatasetName}.NAME," +
					               $"{joinDatasetName}.OBJECTID," +
					               $"{joinDatasetName}.STREET_FK," +
					               $"{joinDatasetName}.KIND," +
					               $"{featureClassDatasetName}.{objectIdFieldName}," +
					               $"{featureClassDatasetName}.{shapeFieldName}"
			               };

			Table queryTable = _gdb.OpenQueryTable(
				new QueryTableDescription(queryDef)
				{
					Name = queryTableName,
					PrimaryKeys = $"{featureClassDatasetName}.{objectIdFieldName}"
				});

			Assert.IsInstanceOf<FeatureClass>(queryTable,
			                                  "The query table must materialize as a feature class.");

			return (FeatureClass) queryTable;
		}

		private Table CreateTable(string tableName, params FieldDescription[] fieldDescriptions)
		{
			var schemaBuilder = new SchemaBuilder(_gdb);

			schemaBuilder.Create(new TableDescription(tableName, fieldDescriptions.ToList()));

			Assert.True(schemaBuilder.Build(), $"Failed to create test table {tableName}.");

			return _gdb.OpenDataset<Table>(tableName);
		}

		private static void InsertFeature(FeatureClass featureClass, Guid joinKey)
		{
			using FeatureClassDefinition definition = featureClass.GetDefinition();
			using RowBuffer rowBuffer = featureClass.CreateRowBuffer();

			rowBuffer["UUID"] = FormatGuidValue(joinKey);
			rowBuffer["NAME"] = "street";
			rowBuffer[definition.GetShapeField()] = MapPointBuilderEx.CreateMapPoint(2600000, 1200000, _sr);

			using Feature _ = (Feature) featureClass.CreateRow(rowBuffer);
		}

		private static void InsertJoinRow(Table joinTable, Guid joinKey)
		{
			using RowBuffer rowBuffer = joinTable.CreateRowBuffer();

			rowBuffer["STREET_FK"] = FormatGuidValue(joinKey);
			rowBuffer["KIND"] = 1;

			using Row _ = joinTable.CreateRow(rowBuffer);
		}

		private static string FormatGuidValue(Guid guid)
		{
			return guid.ToString("B").ToUpperInvariant();
		}

		private static bool NamesMatch(string fieldName1, string fieldName2)
		{
			return string.Equals(fieldName1, fieldName2, StringComparison.OrdinalIgnoreCase) ||
			       string.Equals(GetUnqualifiedName(fieldName1), GetUnqualifiedName(fieldName2),
			                     StringComparison.OrdinalIgnoreCase);
		}

		private static string GetUnqualifiedName(string fieldName)
		{
			int separatorIndex = fieldName.LastIndexOf('.');

			return separatorIndex < 0
				       ? fieldName
				       : fieldName.Substring(separatorIndex + 1);
		}
	}
}
