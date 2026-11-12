using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ArcGIS.Core;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase.AGP.Test;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class ArcRelationshipClassTest
{
	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		CoreHostProxy.Initialize();
	}

	[Test]
	public void GetRelationship_finds_relationship_regardless_of_argument_order()
	{
		using ArcGIS.Core.Data.Geodatabase geodatabase =
			CreateGeodatabase("ArcRelationshipClassTest_GetRelationship");

		CreateOneToManySchema(geodatabase);

		using Table originTable = geodatabase.OpenDataset<Table>("ORIGIN_TABLE");
		using Table destinationTable = geodatabase.OpenDataset<Table>("DEST_TABLE");
		using RelationshipClass proRelationshipClass =
			geodatabase.OpenDataset<RelationshipClass>("ORIGIN_DEST_REL");

		// Insert an unrelated origin row first so that the related origin row's OID
		// differs from the destination row's OID. This rules out any coincidental
		// match that would mask a swapped-arguments bug.
		using Row unrelatedOriginRow = InsertOrigin(originTable, "UNRELATED");
		using Row originRow = InsertOrigin(originTable, "O1");
		using Row destinationRow = InsertDestination(destinationTable, "O1");

		Assert.AreNotEqual(originRow.GetObjectID(), destinationRow.GetObjectID(),
		                   "Test setup requires origin and destination OIDs to differ.");

		var relationshipClass = ArcRelationshipClass.Create(proRelationshipClass);

		IObject originObject = ArcGeodatabaseUtils.ToArcRow(originRow);
		IObject destinationObject = ArcGeodatabaseUtils.ToArcRow(destinationRow);

		// Correct order already works today:
		IRelationship correctOrderResult =
			relationshipClass.GetRelationship(originObject, destinationObject);

		Assert.NotNull(correctOrderResult,
		               "Relationship not found with origin/destination in the correct order.");

		// Swapped order must also find the relationship:
		IRelationship swappedOrderResult =
			relationshipClass.GetRelationship(destinationObject, originObject);

		Assert.NotNull(swappedOrderResult,
		               "Relationship not found when origin/destination arguments are swapped.");

		Assert.AreEqual(originObject.OID, swappedOrderResult.OriginObject.OID);
		Assert.AreEqual(destinationObject.OID, swappedOrderResult.DestinationObject.OID);
	}

	[Test]
	public void Failed_property_caching_can_be_retried_on_the_correct_thread()
	{
		using ArcGIS.Core.Data.Geodatabase geodatabase =
			CreateGeodatabase("ArcRelationshipClassTest_RetryCaching");

		CreateOneToManySchema(geodatabase);

		using Table originTable = geodatabase.OpenDataset<Table>("ORIGIN_TABLE");

		ArcWorkspace.Create(geodatabase);

		ArcTable arcTable = ArcGeodatabaseUtils.ToArcTable(originTable);
		IRelationshipClass relationshipClass =
			arcTable.get_RelationshipClasses(esriRelRole.esriRelRoleAny).Single();

		Assert.AreSame(arcTable,
		               ArcGeodatabaseUtils.ToArcTable(originTable, eagerPropertyCaching: true));

		for (int i = 0; i < 2; i++)
		{
			Exception exception = TestThreadUtils.RunOnOtherThread(
				() => arcTable.get_RelationshipClasses(esriRelRole.esriRelRoleAny).ToList());

			Assert.IsInstanceOf<CalledOnWrongThreadException>(exception);
		}

		arcTable.get_RelationshipClasses(esriRelRole.esriRelRoleAny).ToList();

		esriRelCardinality cardinality = default;
		string originPrimaryKey = null;
		string originForeignKey = null;
		string destinationClassAlias = null;

		Exception cachedAccessException = TestThreadUtils.RunOnOtherThread(() =>
		{
			cardinality = relationshipClass.Cardinality;
			originPrimaryKey = relationshipClass.OriginPrimaryKey;
			originForeignKey = relationshipClass.OriginForeignKey;
			destinationClassAlias = relationshipClass.DestinationClass.AliasName;
		});

		Assert.IsNull(cachedAccessException);
		Assert.AreEqual(esriRelCardinality.esriRelCardinalityOneToMany, cardinality);
		Assert.AreEqual("UUID", originPrimaryKey);
		Assert.AreEqual("ORIGIN_FK", originForeignKey);
		Assert.AreEqual("DEST_TABLE", destinationClassAlias);
	}

	[Test]
	public void Relationship_classes_of_table_and_related_tables_are_readable_from_other_thread()
	{
		using ArcGIS.Core.Data.Geodatabase geodatabase =
			CreateGeodatabase("ArcRelationshipClassTest_RelatedTables");

		CreateOneToManySchema(geodatabase, withDetailTable: true);

		using Table originTable = geodatabase.OpenDataset<Table>("ORIGIN_TABLE");

		var originClassNotInWorkspaceCache = new ArcTable(originTable);
		originClassNotInWorkspaceCache.CacheProperties();

		IObjectClass destinationClass =
			originClassNotInWorkspaceCache.get_RelationshipClasses(esriRelRole.esriRelRoleAny)
			                              .Single().DestinationClass;
		destinationClass.get_RelationshipClasses(esriRelRole.esriRelRoleAny).ToList();

		List<string> originRelClassNames = null;
		List<string> destinationRelClassNames = null;
		string detailClassAlias = null;

		Exception exception = TestThreadUtils.RunOnOtherThread(() =>
		{
			originRelClassNames =
				originClassNotInWorkspaceCache.get_RelationshipClasses(esriRelRole.esriRelRoleAny)
				                              .Select(r => r.Name).ToList();

			List<IRelationshipClass> destinationRelClasses =
				destinationClass.get_RelationshipClasses(esriRelRole.esriRelRoleAny).ToList();

			destinationRelClassNames = destinationRelClasses.Select(r => r.Name).ToList();
			detailClassAlias = destinationRelClasses.Single(r => r.Name == "DEST_DETAIL_REL")
			                                        .DestinationClass.AliasName;
		});

		Assert.IsNull(exception);
		CollectionAssert.AreEquivalent(new[] { "ORIGIN_DEST_REL" }, originRelClassNames);
		CollectionAssert.AreEquivalent(new[] { "ORIGIN_DEST_REL", "DEST_DETAIL_REL" },
		                               destinationRelClassNames);
		Assert.AreEqual("DETAIL_TABLE", detailClassAlias);
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

	private static void CreateOneToManySchema(ArcGIS.Core.Data.Geodatabase geodatabase,
	                                          bool withDetailTable = false)
	{
		var schemaBuilder = new SchemaBuilder(geodatabase);

		var originDescription = new TableDescription(
			"ORIGIN_TABLE",
			new List<FieldDescription>
			{
				new FieldDescription("UUID", FieldType.String) { Length = 38 }
			});

		var destinationDescription = new TableDescription(
			"DEST_TABLE",
			new List<FieldDescription>
			{
				new FieldDescription("ORIGIN_FK", FieldType.String) { Length = 38 },
				new FieldDescription("UUID", FieldType.String) { Length = 38 }
			});

		schemaBuilder.Create(originDescription);
		schemaBuilder.Create(destinationDescription);
		schemaBuilder.Create(
			new RelationshipClassDescription(
				"ORIGIN_DEST_REL", originDescription, destinationDescription,
				RelationshipCardinality.OneToMany,
				"UUID", "ORIGIN_FK"));

		if (withDetailTable)
		{
			var detailDescription = new TableDescription(
				"DETAIL_TABLE",
				new List<FieldDescription>
				{
					new FieldDescription("DEST_FK", FieldType.String) { Length = 38 }
				});

			schemaBuilder.Create(detailDescription);
			schemaBuilder.Create(
				new RelationshipClassDescription(
					"DEST_DETAIL_REL", destinationDescription, detailDescription,
					RelationshipCardinality.OneToMany,
					"UUID", "DEST_FK"));
		}

		Assert.True(schemaBuilder.Build(), "Failed to create test schema.");
	}

	private static Row InsertOrigin(Table table, string uuid)
	{
		using RowBuffer rowBuffer = table.CreateRowBuffer();

		rowBuffer["UUID"] = uuid;

		return table.CreateRow(rowBuffer);
	}

	private static Row InsertDestination(Table table, string originFk)
	{
		using RowBuffer rowBuffer = table.CreateRowBuffer();

		rowBuffer["ORIGIN_FK"] = originFk;

		return table.CreateRow(rowBuffer);
	}
}
