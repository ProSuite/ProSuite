using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
				new FieldDescription("ORIGIN_FK", FieldType.String) { Length = 38 }
			});

		schemaBuilder.Create(originDescription);
		schemaBuilder.Create(destinationDescription);
		schemaBuilder.Create(
			new RelationshipClassDescription(
				"ORIGIN_DEST_REL", originDescription, destinationDescription,
				RelationshipCardinality.OneToMany,
				"UUID", "ORIGIN_FK"));

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
