using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.GIS.Geodatabase.AGP;
using ProSuite.GIS.Geodatabase.API;
using FieldType = ArcGIS.Core.Data.FieldType;
using JoinType = ProSuite.Commons.GeoDb.JoinType;

namespace ProSuite.GIS.Geodatabase.AGP.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class QueryTableTest
	{
		private static readonly SpatialReference _sr = SpatialReferences.WGS84;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			CoreHostProxy.Initialize();
		}

		[Test]
		public void OpenQueryTable_returns_feature_with_shape_for_left_join_without_destination_row()
		{
			using ArcGIS.Core.Data.Geodatabase geodatabase =
				CreateGeodatabase("QueryTableTest_LeftJoin");

			CreateOneToManySchema(geodatabase);

			using FeatureClass arealFeatureClass = geodatabase.OpenDataset<FeatureClass>("AREAL");

			InsertAreal(arealFeatureClass, "A1", 19, 2600000, 1200000);

			var workspace = (ArcWorkspace) ArcWorkspace.Create(geodatabase);

			var queryFeatureClass = (IFeatureClass) workspace.OpenQueryTable(
				"AREAL_SCHOOL",
				new[] { "AREAL", "SCHOOL" },
				JoinType.LeftJoin,
				"AREAL.OBJEKTART = 19 AND SCHOOL.UUID IS NULL");

			string shapeFieldName = null;
			Assert.DoesNotThrow(() => shapeFieldName = queryFeatureClass.ShapeFieldName);
			Assert.False(string.IsNullOrEmpty(shapeFieldName));

			IFeature feature = queryFeatureClass.Search(null, false).Single();

			Assert.NotNull(feature.ShapeCopy);
		}

		[Test]
		public void OpenQueryTable_returns_feature_with_shape_for_many_to_many_relationship()
		{
			using ArcGIS.Core.Data.Geodatabase geodatabase =
				CreateGeodatabase("QueryTableTest_ManyToMany");

			CreateManyToManySchema(geodatabase);

			using FeatureClass buildingFeatureClass =
				geodatabase.OpenDataset<FeatureClass>("BUILDING");
			using Table usageTable = geodatabase.OpenDataset<Table>("USAGE");
			using Table relationshipTable = geodatabase.OpenDataset<Table>("BUILDING_USAGE");

			InsertBuilding(buildingFeatureClass, "B1", 2600100, 1200100);
			InsertUsage(usageTable, "U1", "school");
			InsertRelationshipRow(relationshipTable, "B1", "U1");

			var workspace = (ArcWorkspace) ArcWorkspace.Create(geodatabase);

			var queryFeatureClass = (IFeatureClass) workspace.OpenQueryTable(
				"BUILDING_USAGE",
				new[] { "BUILDING", "USAGE" },
				JoinType.InnerJoin);

			string shapeFieldName = null;
			Assert.DoesNotThrow(() => shapeFieldName = queryFeatureClass.ShapeFieldName);
			Assert.False(string.IsNullOrEmpty(shapeFieldName));

			IFeature feature = queryFeatureClass.Search(null, false).Single();

			Assert.NotNull(feature.ShapeCopy);
		}

		private static ArcGIS.Core.Data.Geodatabase CreateGeodatabase(string name)
		{
			string tempDirectory = Path.Combine(
				Path.GetTempPath(), $"{name}_{Guid.NewGuid():N}");
			Directory.CreateDirectory(tempDirectory);

			string geodatabasePath = Path.Combine(tempDirectory, $"{name}.gdb");

			SchemaBuilder.CreateGeodatabase(
				new FileGeodatabaseConnectionPath(new Uri(geodatabasePath)));

			return WorkspaceUtils.OpenFileGeodatabase(geodatabasePath);
		}

		private static void CreateOneToManySchema(ArcGIS.Core.Data.Geodatabase geodatabase)
		{
			var schemaBuilder = new SchemaBuilder(geodatabase);

			var arealDescription = new FeatureClassDescription(
				"AREAL",
				new List<FieldDescription>
				{
					new FieldDescription("UUID", FieldType.String) { Length = 38 },
					new FieldDescription("OBJEKTART", FieldType.Integer)
				},
				new ShapeDescription(GeometryType.Point, _sr));

			var schoolDescription = new TableDescription(
				"SCHOOL",
				new List<FieldDescription>
				{
					new FieldDescription("UUID", FieldType.String) { Length = 38 },
					new FieldDescription("AREAL_UUID", FieldType.String) { Length = 38 },
					new FieldDescription("NAME", FieldType.String) { Length = 50 }
				});

			schemaBuilder.Create(arealDescription);
			schemaBuilder.Create(schoolDescription);
			schemaBuilder.Create(
				new RelationshipClassDescription(
					"AREAL_SCHOOL", arealDescription, schoolDescription,
					RelationshipCardinality.OneToMany,
					"UUID", "AREAL_UUID"));

			Assert.True(schemaBuilder.Build(), "Failed to create 1:n query-table test schema.");
		}

		private static void CreateManyToManySchema(ArcGIS.Core.Data.Geodatabase geodatabase)
		{
			var schemaBuilder = new SchemaBuilder(geodatabase);

			var buildingDescription = new FeatureClassDescription(
				"BUILDING",
				new List<FieldDescription>
				{
					new FieldDescription("UUID", FieldType.String) { Length = 38 },
					new FieldDescription("NAME", FieldType.String) { Length = 50 }
				},
				new ShapeDescription(GeometryType.Point, _sr));

			var usageDescription = new TableDescription(
				"USAGE",
				new List<FieldDescription>
				{
					new FieldDescription("UUID", FieldType.String) { Length = 38 },
					new FieldDescription("KIND", FieldType.String) { Length = 50 }
				});

			schemaBuilder.Create(buildingDescription);
			schemaBuilder.Create(usageDescription);
			schemaBuilder.Create(
				new AttributedRelationshipClassDescription(
					"BUILDING_USAGE", buildingDescription, usageDescription,
					RelationshipCardinality.ManyToMany,
					"UUID", "BUILDING_UUID",
					"UUID", "USAGE_UUID"));

			Assert.True(schemaBuilder.Build(), "Failed to create m:n query-table test schema.");
		}

		private static void InsertAreal(
			FeatureClass featureClass,
			string uuid,
			int objektart,
			double x,
			double y)
		{
			using FeatureClassDefinition definition = featureClass.GetDefinition();
			using RowBuffer rowBuffer = featureClass.CreateRowBuffer();

			rowBuffer["UUID"] = uuid;
			rowBuffer["OBJEKTART"] = objektart;
			rowBuffer[definition.GetShapeField()] = MapPointBuilderEx.CreateMapPoint(x, y, _sr);

			using Feature _ = (Feature) featureClass.CreateRow(rowBuffer);
		}

		private static void InsertBuilding(
			FeatureClass featureClass,
			string uuid,
			double x,
			double y)
		{
			using FeatureClassDefinition definition = featureClass.GetDefinition();
			using RowBuffer rowBuffer = featureClass.CreateRowBuffer();

			rowBuffer["UUID"] = uuid;
			rowBuffer["NAME"] = "building";
			rowBuffer[definition.GetShapeField()] = MapPointBuilderEx.CreateMapPoint(x, y, _sr);

			using Feature _ = (Feature) featureClass.CreateRow(rowBuffer);
		}

		private static void InsertUsage(Table table, string uuid, string kind)
		{
			using RowBuffer rowBuffer = table.CreateRowBuffer();

			rowBuffer["UUID"] = uuid;
			rowBuffer["KIND"] = kind;

			using Row _ = table.CreateRow(rowBuffer);
		}

		private static void InsertRelationshipRow(
			Table table,
			string buildingUuid,
			string usageUuid)
		{
			using RowBuffer rowBuffer = table.CreateRowBuffer();

			rowBuffer["BUILDING_UUID"] = buildingUuid;
			rowBuffer["USAGE_UUID"] = usageUuid;

			using Row _ = table.CreateRow(rowBuffer);
		}
	}
}
