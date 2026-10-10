using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using NUnit.Framework;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Testing;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase.AGP.Test;

/// <summary>
/// Tests <see cref="ArcTable.get_RelationshipClasses"/> for the data store types that can be
/// created in the test host. The attribute editor calls this method for every selected
/// object class.
/// </summary>
[TestFixture]
[Apartment(ApartmentState.STA)]
public class ArcTableRelationshipClassesTest
{
	private const string OriginName = "origin";
	private const string DestinationName = "destination";
	private const string RelationshipClassName = "origin_destination";

	// Not "KEY": this is a reserved word in SQLite, and a mobile geodatabase renames the field.
	private const string LinkFieldName = "LINK_ID";

	private string _tempDirectory;

	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		CoreHostProxy.Initialize();

		_tempDirectory = Path.Combine(Path.GetTempPath(),
		                              $"{nameof(ArcTableRelationshipClassesTest)}_{Guid.NewGuid():N}");
		Directory.CreateDirectory(_tempDirectory);
	}

	/// <summary>
	/// Regression test: in a memory geodatabase, Pro throws InvalidOperationException when
	/// reading relationship class definitions. The attribute editor failed with this
	/// exception when an object of a memory geodatabase was selected.
	/// </summary>
	[Test]
	public void MemoryGeodatabase_HasNoRelationshipClasses()
	{
		using var gdb = SchemaBuilder.CreateGeodatabase(
			new MemoryConnectionProperties(
				$"{nameof(ArcTableRelationshipClassesTest)}_{Guid.NewGuid():N}"));

		var schemaBuilder = new SchemaBuilder(gdb);
		schemaBuilder.Create(CreateTableDescription(OriginName));
		Assert.True(schemaBuilder.Build(), string.Join("; ", schemaBuilder.ErrorMessages));

		using Table table = gdb.OpenDataset<Table>(OriginName);
		ArcTable arcTable = ArcGeodatabaseUtils.ToArcTable(table);

		Assert.IsInstanceOf<ArcWorkspace>(arcTable.Workspace);
		Assert.IsEmpty(arcTable.get_RelationshipClasses(esriRelRole.esriRelRoleAny));
		Assert.IsEmpty(arcTable.Workspace.get_Datasets(esriDatasetType.esriDTRelationshipClass));
		Assert.IsEmpty(
			arcTable.Workspace.get_DatasetNames(esriDatasetType.esriDTRelationshipClass));
	}

	/// <summary>
	/// A shapefile is wrapped in a <see cref="BasicWorkspace"/>, which is not an
	/// <see cref="IFeatureWorkspace"/>. Therefore get_RelationshipClasses returns nothing
	/// before it reads any relationship class definitions.
	/// </summary>
	[Test]
	public void Shapefile_HasNoRelationshipClasses()
	{
		// SchemaBuilder cannot create shapefiles, so the WorkList test data is used.
		string path = TestDataPreparer.ExtractZip(
			"Shapefile.zip", @"..\ProSuite.AGP.WorkList.Test\TestData").GetPath();

		using var datastore = new FileSystemDatastore(
			new FileSystemConnectionPath(new Uri(path, UriKind.Absolute),
			                             FileSystemDatastoreType.Shapefile));
		using FeatureClass shapefile = datastore.OpenDataset<FeatureClass>("TLM_STRASSE_clip");

		ArcTable arcTable = ArcGeodatabaseUtils.ToArcTable(shapefile);

		Assert.IsInstanceOf<BasicWorkspace>(arcTable.Workspace);
		Assert.IsNotInstanceOf<IFeatureWorkspace>(arcTable.Workspace);
		Assert.IsEmpty(arcTable.get_RelationshipClasses(esriRelRole.esriRelRoleAny));
	}

	[TestCase(".gdb")]
	[TestCase(".geodatabase")]
	public void LocalGeodatabase_FindsRelationshipClass(string extension)
	{
		string path = Path.Combine(_tempDirectory, $"{Guid.NewGuid():N}{extension}");
		var uri = new Uri(path, UriKind.Absolute);

		using ArcGIS.Core.Data.Geodatabase gdb =
			extension == ".gdb"
				? SchemaBuilder.CreateGeodatabase(new FileGeodatabaseConnectionPath(uri))
				: SchemaBuilder.CreateGeodatabase(new MobileGeodatabaseConnectionPath(uri));

		var schemaBuilder = new SchemaBuilder(gdb);
		TableDescription origin = CreateTableDescription(OriginName);
		TableDescription destination = CreateTableDescription(DestinationName);
		schemaBuilder.Create(origin);
		schemaBuilder.Create(destination);
		schemaBuilder.Create(new RelationshipClassDescription(
			                     RelationshipClassName, origin, destination,
			                     RelationshipCardinality.OneToMany,
			                     LinkFieldName, LinkFieldName));
		Assert.True(schemaBuilder.Build(), string.Join("; ", schemaBuilder.ErrorMessages));

		using Table originTable = gdb.OpenDataset<Table>(OriginName);
		using Table destinationTable = gdb.OpenDataset<Table>(DestinationName);
		ArcTable arcOrigin = ArcGeodatabaseUtils.ToArcTable(originTable);
		ArcTable arcDestination = ArcGeodatabaseUtils.ToArcTable(destinationTable);

		Assert.AreEqual(new[] { RelationshipClassName },
		                GetUnqualifiedNames(
			                arcOrigin.get_RelationshipClasses(esriRelRole.esriRelRoleOrigin)));
		Assert.AreEqual(new[] { RelationshipClassName },
		                GetUnqualifiedNames(
			                arcDestination.get_RelationshipClasses(
				                esriRelRole.esriRelRoleDestination)));
		Assert.IsEmpty(arcOrigin.get_RelationshipClasses(esriRelRole.esriRelRoleDestination));
	}

	/// <summary>
	/// A mobile geodatabase qualifies names with "main.".
	/// </summary>
	private static string[] GetUnqualifiedNames(IEnumerable<IRelationshipClass> relationshipClasses)
	{
		return relationshipClasses
		       .Select(r => ModelElementNameUtils.GetUnqualifiedName(((IDataset) r).Name))
		       .ToArray();
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
