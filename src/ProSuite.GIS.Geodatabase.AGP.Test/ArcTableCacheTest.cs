using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using NUnit.Framework;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase.AGP.Test;

/// <summary>
/// Regression test: disposing an <see cref="ArcTable"/> must remove it from its parent
/// workspace's table cache (<c>_tablesByName</c>). Otherwise, a later OpenTable returns the
/// cached, now-disposed instance and any use throws ObjectDisposedException.
/// </summary>
[TestFixture]
[Apartment(ApartmentState.STA)]
public class ArcTableCacheTest
{
	private ArcGIS.Core.Data.Geodatabase _gdb;

	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		CoreHostProxy.Initialize();

		_gdb = SchemaBuilder.CreateGeodatabase(
			new MemoryConnectionProperties("ArcTableCacheTest"));
	}

	[OneTimeTearDown]
	public void OneTimeTearDown()
	{
		_gdb?.Dispose();
	}

	[Test]
	public void Disposing_a_cached_table_removes_it_from_the_workspace_cache()
	{
		const string tableName = "cache_tbl";
		CreateTable(tableName);

		var workspace = ArcWorkspace.Create(_gdb);

		// First open: opens and caches the table in the workspace.
		var firstOpen = (ArcTable) workspace.OpenTable(tableName);

		// Precondition: the workspace really caches the table (same instance returned).
		Assert.AreSame(firstOpen, workspace.OpenTable(tableName),
		               "Precondition: OpenTable must return the cached instance.");

		firstOpen.Dispose();

		// After disposal the workspace must NOT hand back the disposed instance.
		ITable reopened = workspace.OpenTable(tableName);

		Assert.AreNotSame(firstOpen, reopened,
		                  "After dispose, OpenTable returned the cached disposed table - " +
		                  "the table did not remove itself from the workspace cache.");

		Assert.DoesNotThrow(
			() => reopened.RowCount(null),
			"The table returned after disposing the cached one must be usable.");
	}

	private void CreateTable(string name)
	{
		var schemaBuilder = new SchemaBuilder(_gdb);

		schemaBuilder.Create(new TableDescription(
			                     name, new List<FieldDescription>
			                           {
				                           new FieldDescription("NAME", FieldType.String)
			                           }));

		Assert.True(schemaBuilder.Build(), $"Failed to create test table {name}.");
	}
}
