using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.Commons.Testing;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.AGP.WorkList.Test;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class WorkListDbTest
{
	[OneTimeSetUp]
	public void OnTimeSetUp()
	{
		// Host must be initialized on an STA thread:
		//Host.Initialize();
		CoreHostProxy.Initialize();
	}

	[Test]
	public void Can_count_db_workItems_measure_performance()
	{
		string path = TestDataPreparer.ExtractZip("TLM_ERRORS.gdb.zip").GetPath();

		using var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(path, UriKind.Absolute)));
		using var lines = geodatabase.OpenDataset<FeatureClass>("TLM_ERRORS_LINE");
		using var multipatchs = geodatabase.OpenDataset<FeatureClass>("TLM_ERRORS_MULTIPATCH");
		using var multipoints = geodatabase.OpenDataset<FeatureClass>("TLM_ERRORS_MULTIPOINT");
		using var polygons = geodatabase.OpenDataset<FeatureClass>("TLM_ERRORS_POLYGON");

		var tables = new List<FeatureClass> { lines, multipatchs, multipoints, polygons };
		var sourceClasses = new List<ISourceClass>(tables.Count);

		Dictionary<IntPtr, Datastore> datastoresByHandle = new Dictionary<IntPtr, Datastore>();

		foreach (Table table in tables)
		{
			TableDefinition tableDefinition = table.GetDefinition();

			DbSourceClassSchema schema = CreateStatusSchema(tableDefinition);

			Datastore datastore = table.GetDatastore();
			datastoresByHandle.TryAdd(datastore.Handle, datastore);

			var sourceClass = new DatabaseSourceClass(new GdbTableIdentity(table), datastore, schema, null, null);

			sourceClasses.Add(sourceClass);
		}

		Assert.True(datastoresByHandle.Count == 1,
		            "Multiple geodatabases are referenced by the work list's source classes.");

		var gdb = (Geodatabase) datastoresByHandle.First().Value;
		var itemRepository = new DbStatusWorkItemRepository(sourceClasses, new EmptyWorkItemStateRepository(), gdb);

		var wl = new IssueWorkList(itemRepository, "uniqueName", "displayName");

		var watch = new Stopwatch();
		watch.Start();

		int itemsCount = wl.Count();

		watch.Stop();

		Console.WriteLine($"items count {itemsCount}");
		Console.WriteLine($"{watch.ElapsedMilliseconds:N0} ms");

		var filter = new QueryFilter();
		filter.SubFields = "OBJECTID";

		watch.Reset();
		watch.Start();
		List<IWorkItem> items = wl.GetItems(filter).ToList();

		Console.WriteLine($"items {itemsCount}");
		Console.WriteLine($"{watch.ElapsedMilliseconds:N0} ms");

		Assert.AreEqual(itemsCount, items.Count);
	}

	[Test]
	public void Can_get_extent_db_workitems()
	{
		string path = TestDataPreparer.ExtractZip("TLM_ERRORS.gdb.zip").GetPath();

		using var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(path, UriKind.Absolute)));
		using var lines = geodatabase.OpenDataset<FeatureClass>("TLM_ERRORS_LINE");
		using var multipatchs = geodatabase.OpenDataset<FeatureClass>("TLM_ERRORS_MULTIPATCH");
		using var multipoints = geodatabase.OpenDataset<FeatureClass>("TLM_ERRORS_MULTIPOINT");
		using var polygons = geodatabase.OpenDataset<FeatureClass>("TLM_ERRORS_POLYGON");

		var tables = new List<FeatureClass> { lines, multipatchs, multipoints, polygons };
		var sourceClasses = new List<ISourceClass>(tables.Count);

		Dictionary<IntPtr, Datastore> datastoresByHandle = new Dictionary<IntPtr, Datastore>();

		foreach (Table table in tables)
		{
			TableDefinition tableDefinition = table.GetDefinition();

			DbSourceClassSchema schema = CreateStatusSchema(tableDefinition);

			Datastore datastore = table.GetDatastore();

			datastoresByHandle.TryAdd(datastore.Handle, datastore);

			var sourceClass = new DatabaseSourceClass(new GdbTableIdentity(table), datastore, schema, null, null);

			sourceClasses.Add(sourceClass);
		}

		Assert.True(datastoresByHandle.Count == 1,
		            "Multiple geodatabases are referenced by the work list's source classes.");

		var gdb = (Geodatabase)datastoresByHandle.First().Value;

		var itemRepository =
			new DbStatusWorkItemRepository(sourceClasses, new EmptyWorkItemStateRepository(), gdb);

		var wl = new IssueWorkList(itemRepository, "uniqueName", "displayName");

		SpatialReference ch1903plus = SpatialReferenceBuilder.CreateSpatialReference(2056);

		Geometry visibleExtent = EnvelopeBuilderEx.CreateEnvelope(
			new Coordinate2D(2624810, 1184300),
			new Coordinate2D(2929350, 1186910), ch1903plus);

		List<IWorkItem> items = wl.GetItems(GdbQueryUtils.CreateSpatialFilter(visibleExtent)).ToList();

		Envelope extent = wl.Extent;
		Assert.NotNull(extent);
		Assert.False(extent.IsEmpty);
		Assert.True(GeometryUtils.Intersects(visibleExtent, extent));

		Assert.AreEqual(3861, items.Count);
	}

	[Test]
	public void Can_open_fgdb_IssueWorkList()
	{
		string path = TestDataPreparer.ExtractZip("issues.gdb.zip").GetPath();

		using var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(path, UriKind.Absolute)));
		using var rows = geodatabase.OpenDataset<Table>("IssueRows");
		using var lines = geodatabase.OpenDataset<FeatureClass>("IssueLines");
		using var multipatchs = geodatabase.OpenDataset<FeatureClass>("IssueMultipatches");
		using var multipoints = geodatabase.OpenDataset<FeatureClass>("IssuePoints");
		using var polygons = geodatabase.OpenDataset<FeatureClass>("IssuePolygons");

		var tables = new List<Table> { rows, lines, multipatchs, multipoints, polygons };
		var sourceClasses = new List<ISourceClass>(tables.Count);

		Dictionary<IntPtr, Datastore> datastoresByHandle = new Dictionary<IntPtr, Datastore>();

		foreach (Table table in tables)
		{
			TableDefinition tableDefinition = table.GetDefinition();

			DbSourceClassSchema schema = CreateStatusSchema(tableDefinition);

			Datastore datastore = table.GetDatastore();
			datastoresByHandle.TryAdd(datastore.Handle, datastore);

			var sourceClass = new DatabaseSourceClass(new GdbTableIdentity(table), datastore, schema, null, null);

			sourceClasses.Add(sourceClass);
		}

		Assert.True(datastoresByHandle.Count == 1,
		            "Multiple geodatabases are referenced by the work list's source classes.");

		var gdb = (Geodatabase)datastoresByHandle.First().Value;

		var itemRepository =
			new DbStatusWorkItemRepository(sourceClasses, new EmptyWorkItemStateRepository(), gdb);

		var wl = new IssueWorkList(itemRepository, "uniqueName", "displayName");
		List<IWorkItem> items = wl.GetItems().ToList();

		Assert.AreEqual(62, items.Count);
	}

	[Test]
	public void Can_open_fgdb_SelectionWorkList()
	{
		string path = TestDataPreparer.ExtractZip("issues.gdb.zip").GetPath();

		using var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(path, UriKind.Absolute)));
		using var rows = geodatabase.OpenDataset<Table>("IssueRows");
		using var lines = geodatabase.OpenDataset<FeatureClass>("IssueLines");
		using var multipatchs = geodatabase.OpenDataset<FeatureClass>("IssueMultipatches");
		using var multipoints = geodatabase.OpenDataset<FeatureClass>("IssuePoints");
		using var polygons = geodatabase.OpenDataset<FeatureClass>("IssuePolygons");

		Dictionary<Table, List<long>> selection = new Dictionary<Table, List<long>>();
		selection.Add(rows, []);
		selection.Add(lines, [1, 2, 3, 4, 5]);
		selection.Add(multipatchs, []);
		selection.Add(multipoints, [5, 7, 12, 9]);
		selection.Add(polygons, []);

		var sourceClasses = new List<SelectionSourceClass>(selection.Count);

		foreach ((Table table, List<long> oids) in selection)
		{
			TableDefinition tableDefinition = table.GetDefinition();

			SourceClassSchema schema = CreateSchema(tableDefinition);

			Datastore datastore = table.GetDatastore();
			var sourceClass = new SelectionSourceClass(new GdbTableIdentity(table), datastore, schema, oids);

			sourceClasses.Add(sourceClass);
		}

		var repository = new SelectionItemRepository(sourceClasses, new EmptyWorkItemStateRepository());

		var wl = new SelectionWorkList(repository, "uniqueName", "displayName");
		List<IWorkItem> items = wl.GetItems().ToList();

		Assert.AreEqual(9, items.Count);
	}

	[Test]
	public void Can_create_SelectionWorkList_from_Shapefile()
	{
		string path = @"C:\temp\Shapefile";
		using var fileSystem =
			new FileSystemDatastore(new FileSystemConnectionPath(new Uri(path, UriKind.Absolute),
			                                                     FileSystemDatastoreType.Shapefile));

		var shapefile = fileSystem.OpenDataset<FeatureClass>("TLM_STRASSE_clip");

		var tables = new List<Table> { shapefile };
		var sourceClasses = new List<SelectionSourceClass>(tables.Count);

		foreach (Table table in tables)
		{
			TableDefinition tableDefinition = table.GetDefinition();

			SourceClassSchema schema = CreateSchema(tableDefinition);

			Datastore datastore = table.GetDatastore();
			List<long> oids = [0, 1, 2, 3];
			var sourceClass = new SelectionSourceClass(new GdbTableIdentity(table), datastore, schema, oids);

			sourceClasses.Add(sourceClass);
		}

		var repository = new SelectionItemRepository(sourceClasses, new EmptyWorkItemStateRepository());

		var wl = new SelectionWorkList(repository, "uniqueName", "displayName");
		List<IWorkItem> items = wl.GetItems().ToList();

		Assert.AreEqual(4, items.Count);
	}

	private static SourceClassSchema CreateSchema(TableDefinition tableDefinition)
	{
		string objectIDField = tableDefinition.GetObjectIDField();

		string shapeField = null;

		if (tableDefinition is FeatureClassDefinition featureClassDefinition)
		{
			shapeField = featureClassDefinition.GetShapeField();
		}

		return new SourceClassSchema(objectIDField, shapeField);
	}

	private static DbSourceClassSchema CreateStatusSchema(TableDefinition tableDefinition)
	{
		string objectIDField = tableDefinition.GetObjectIDField();

		string shapeField = null;

		if (tableDefinition is FeatureClassDefinition featureClassDefinition)
		{
			shapeField = featureClassDefinition.GetShapeField();
		}

		return new DbSourceClassSchema(objectIDField, shapeField, "STATUS", tableDefinition.FindField("STATUS"),
		                                (int) IssueCorrectionStatus.NotCorrected,
		                                (int) IssueCorrectionStatus.Corrected);
	}
}

public class NoOpAttributeReader : IAttributeReader
{
	public T GetValue<T>(Row row, Attributes attribute)
	{
		return default(T);
	}

	public void ReadAttributes(Row fromRow, IWorkItem forItem, ISourceClass source) { }

	public IList<InvolvedTable> ParseInvolved(string involvedString, bool hasGeometry)
	{
		return new List<InvolvedTable>(0);
	}

	public string GetName(Attributes attribute)
	{
		return "fooName";
	}
}
