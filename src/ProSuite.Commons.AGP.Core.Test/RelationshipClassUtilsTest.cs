using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Testing;

namespace ProSuite.Commons.AGP.Core.Test;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class RelationshipClassUtilsTest
{
	private const string OriginName = "origin";
	private const string DestinationName = "destination";
	private const string SimpleRelationshipClassName = "simple_rel";
	private const string AttributedRelationshipClassName = "attributed_rel";

	// Not "KEY": this is a reserved word in SQLite, and a mobile geodatabase renames the field.
	private const string LinkFieldName = "LINK_ID";

	private string _tempDirectory;

	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		CoreHostProxy.Initialize();

		_tempDirectory = Path.Combine(Path.GetTempPath(),
		                              $"{nameof(RelationshipClassUtilsTest)}_{Guid.NewGuid():N}");
		Directory.CreateDirectory(_tempDirectory);
	}

	[OneTimeTearDown]
	public void OneTimeTearDown()
	{
		try
		{
			Directory.Delete(_tempDirectory, true);
		}
		catch (IOException)
		{
			// The geodatabase files can still be locked by Pro.
		}
		catch (UnauthorizedAccessException)
		{
			// The geodatabase files can still be locked by Pro.
		}
	}

	#region Memory geodatabase

	/// <summary>
	/// Documents the Pro behaviour that makes the explicit memory geodatabase check in
	/// <see cref="RelationshipClassUtils.TryAddRelationshipClassDefinitions"/> necessary.
	/// If a future Pro version changes this, the check can be reconsidered.
	/// </summary>
	[Test]
	public void MemoryGeodatabase_ProThrowsForBothRelationshipClassDefinitionTypes()
	{
		using var gdb = CreateMemoryGeodatabase();

		Assert.Throws<InvalidOperationException>(() => gdb
			                                         .GetDefinitions<
				                                         RelationshipClassDefinition>());
		Assert.Throws<InvalidOperationException>(() => gdb
			                                         .GetDefinitions<
				                                         AttributedRelationshipClassDefinition>());
	}

	[Test]
	public void MemoryGeodatabase_ProCannotCreateRelationshipClass()
	{
		using var gdb = CreateMemoryGeodatabase();

		var schemaBuilder = new SchemaBuilder(gdb);
		TableDescription origin = CreateTableDescription(OriginName);
		TableDescription destination = CreateTableDescription(DestinationName);

		Assert.Throws<InvalidOperationException>(() => schemaBuilder.Create(
			                                         new RelationshipClassDescription(
				                                         SimpleRelationshipClassName, origin,
				                                         destination,
				                                         RelationshipCardinality.OneToMany,
				                                         LinkFieldName, LinkFieldName)));
	}

	[Test]
	public void TryAddRelationshipClassDefinitions_MemoryGeodatabase_AddsNothing()
	{
		using var gdb = CreateMemoryGeodatabase();

		var result = new List<RelationshipClassDefinition>();

		bool success = RelationshipClassUtils.TryAddRelationshipClassDefinitions(
			gdb, result, out string message);

		Assert.False(success);
		Assert.IsEmpty(result);
		Assert.NotNull(message);
		Console.WriteLine(message);

		Assert.IsEmpty(RelationshipClassUtils.GetRelationshipClassDefinitions(gdb));
		Assert.IsEmpty(RelationshipClassUtils.GetRelationshipClasses(gdb));
	}

	[Test]
	public void DatasetMethods_MemoryGeodatabaseTable_ReturnNothing()
	{
		using var gdb = CreateMemoryGeodatabase();

		var schemaBuilder = new SchemaBuilder(gdb);
		schemaBuilder.Create(CreateTableDescription(OriginName));
		Assert.True(schemaBuilder.Build(), string.Join("; ", schemaBuilder.ErrorMessages));

		using Table table = gdb.OpenDataset<Table>(OriginName);

		AssertDatasetMethodsReturnNothing(table);
	}

	#endregion

	#region File and mobile geodatabase

	[TestCase(".gdb")]
	[TestCase(".geodatabase")]
	public void TryAddRelationshipClassDefinitions_AddsBothTypes(string extension)
	{
		using var gdb = CreateGeodatabaseWithRelationshipClasses(extension);
		Assert.AreEqual(GeodatabaseType.LocalDatabase, gdb.GetGeodatabaseType());

		var result = new List<RelationshipClassDefinition>();
		bool success = RelationshipClassUtils.TryAddRelationshipClassDefinitions(
			gdb, result, out string message);

		Assert.True(success);
		Assert.Null(message);
		Assert.AreEqual(new[] { AttributedRelationshipClassName, SimpleRelationshipClassName },
		                GetSortedNames(result));
		Assert.AreEqual(1, result.OfType<AttributedRelationshipClassDefinition>().Count());

		var filtered = new List<RelationshipClassDefinition>();
		Assert.True(RelationshipClassUtils.TryAddRelationshipClassDefinitions(
			            gdb, filtered, out _,
			            d => d is not AttributedRelationshipClassDefinition));
		Assert.AreEqual(new[] { SimpleRelationshipClassName }, GetSortedNames(filtered));
	}

	[TestCase(".gdb")]
	[TestCase(".geodatabase")]
	public void DatasetMethods_FindRelatedClassesAndRows(string extension)
	{
		using var gdb = CreateGeodatabaseWithRelationshipClasses(extension);

		using Table origin = gdb.OpenDataset<Table>(OriginName);
		using Table destination = gdb.OpenDataset<Table>(DestinationName);

		// Both relationship classes lead from origin to destination.
		Assert.AreEqual(new[] { OriginName, OriginName },
		                Unqualify(RelationshipClassUtils.GetOriginClassNames(destination)));
		Assert.AreEqual(new[] { DestinationName, DestinationName },
		                Unqualify(RelationshipClassUtils.GetDestinationClassNames(origin)));
		Assert.IsEmpty(RelationshipClassUtils.GetOriginClassNames(origin));

		List<Table> originClasses = RelationshipClassUtils.GetOriginClasses(destination).ToList();
		try
		{
			Assert.AreEqual(2, originClasses.Count);
			Assert.AreEqual(new[] { OriginName, OriginName },
			                Unqualify(originClasses.Select(t => t.GetName())));
		}
		finally
		{
			originClasses.ForEach(t => t.Dispose());
		}

		long originOid = 0;
		long destinationOid = 0;
		gdb.ApplyEdits(() =>
		{
			using RowBuffer originBuffer = origin.CreateRowBuffer();
			originBuffer[LinkFieldName] = 7;
			using Row originRow = origin.CreateRow(originBuffer);
			originOid = originRow.GetObjectID();

			using RowBuffer destinationBuffer = destination.CreateRowBuffer();
			destinationBuffer[LinkFieldName] = 7;
			using Row destinationRow = destination.CreateRow(destinationBuffer);
			destinationOid = destinationRow.GetObjectID();
		});

		List<Row> relatedOriginRows =
			RelationshipClassUtils.GetRelatedOriginRows(destination, new[] { destinationOid })
			                      .ToList();
		try
		{
			// Only the simple relationship class relates the rows by the link field. The
			// attributed relationship class has no relationship rows.
			Assert.AreEqual(new[] { originOid },
			                relatedOriginRows.Select(r => r.GetObjectID()).ToArray());
		}
		finally
		{
			relatedOriginRows.ForEach(r => r.Dispose());
		}
	}

	#endregion

	#region Shapefile

	/// <summary>
	/// A shapefile's data store is not a geodatabase. Before, the methods that take a
	/// dataset cast the data store to Geodatabase and threw InvalidCastException.
	/// </summary>
	[Test]
	public void DatasetMethods_Shapefile_ReturnNothing()
	{
		using FileSystemDatastore datastore = OpenShapefileDatastore();
		using FeatureClass shapefile = datastore.OpenDataset<FeatureClass>("TLM_STRASSE_clip");

		using Datastore shapefileDatastore = shapefile.GetDatastore();
		Assert.IsNotInstanceOf<ArcGIS.Core.Data.Geodatabase>(shapefileDatastore);

		AssertDatasetMethodsReturnNothing(shapefile);
	}

	#endregion

	private static void AssertDatasetMethodsReturnNothing(Table table)
	{
		Assert.IsEmpty(RelationshipClassUtils.GetOriginClasses(table));
		Assert.IsEmpty(RelationshipClassUtils.GetOriginClassNames(table));
		Assert.IsEmpty(RelationshipClassUtils.GetDestinationClassNames(table));
		Assert.IsEmpty(RelationshipClassUtils.GetRelatedOriginRows(table, new long[] { 1 }));
		Assert.IsEmpty(RelationshipClassUtils.GetRelationshipClassDefinitionsForAnnotation(table));
	}

	private static string[] GetSortedNames(IEnumerable<RelationshipClassDefinition> definitions)
	{
		return Unqualify(definitions.Select(d => d.GetName())).OrderBy(n => n).ToArray();
	}

	/// <summary>
	/// Removes the qualifier from the names. A mobile geodatabase qualifies names with "main.".
	/// </summary>
	private static string[] Unqualify(IEnumerable<string> names)
	{
		return names.Select(ModelElementNameUtils.GetUnqualifiedName).ToArray();
	}

	private static ArcGIS.Core.Data.Geodatabase CreateMemoryGeodatabase()
	{
		// A memory geodatabase name stays in use after the geodatabase is disposed.
		return SchemaBuilder.CreateGeodatabase(
			new MemoryConnectionProperties($"RelationshipClassUtilsTest_{Guid.NewGuid():N}"));
	}

	/// <summary>
	/// Creates a file geodatabase (extension .gdb) or a mobile geodatabase (extension
	/// .geodatabase) with an origin and a destination table, a simple relationship class and
	/// an attributed relationship class between them.
	/// </summary>
	private ArcGIS.Core.Data.Geodatabase CreateGeodatabaseWithRelationshipClasses(
		string extension)
	{
		string path = Path.Combine(_tempDirectory, $"{Guid.NewGuid():N}{extension}");
		var uri = new Uri(path, UriKind.Absolute);

		ArcGIS.Core.Data.Geodatabase gdb =
			extension == ".gdb"
				? SchemaBuilder.CreateGeodatabase(new FileGeodatabaseConnectionPath(uri))
				: SchemaBuilder.CreateGeodatabase(new MobileGeodatabaseConnectionPath(uri));

		var schemaBuilder = new SchemaBuilder(gdb);
		TableDescription origin = CreateTableDescription(OriginName);
		TableDescription destination = CreateTableDescription(DestinationName);
		schemaBuilder.Create(origin);
		schemaBuilder.Create(destination);
		schemaBuilder.Create(new RelationshipClassDescription(
			                     SimpleRelationshipClassName, origin, destination,
			                     RelationshipCardinality.OneToMany, LinkFieldName, LinkFieldName));
		schemaBuilder.Create(new AttributedRelationshipClassDescription(
			                     AttributedRelationshipClassName, origin, destination,
			                     RelationshipCardinality.ManyToMany,
			                     LinkFieldName, "ORIGIN_LINK_ID", LinkFieldName,
			                     "DESTINATION_LINK_ID"));
		Assert.True(schemaBuilder.Build(), string.Join("; ", schemaBuilder.ErrorMessages));

		return gdb;
	}

	private static FileSystemDatastore OpenShapefileDatastore()
	{
		// SchemaBuilder cannot create shapefiles, so the WorkList test data is used.
		string path = TestDataPreparer.ExtractZip(
			"Shapefile.zip", @"..\ProSuite.AGP.WorkList.Test\TestData").GetPath();

		return new FileSystemDatastore(
			new FileSystemConnectionPath(new Uri(path, UriKind.Absolute),
			                             FileSystemDatastoreType.Shapefile));
	}

	private static TableDescription CreateTableDescription(string name)
	{
		return new TableDescription(
			name, new List<FieldDescription>
			      {
				      new FieldDescription(LinkFieldName, FieldType.Integer)
			      });
	}
}
