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
using ProSuite.Commons.Text;
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
	public void Can_count_rdbms_workItems_measure_performance()
	{
		using var geodatabase = new Geodatabase(new DatabaseConnectionProperties(EnterpriseDatabaseType.Oracle)
		                                        {
			                                        AuthenticationMode = AuthenticationMode.OSA,
			                                        Instance = "TOPGIST",
			                                        Database = string.Empty
		                                        });

		using var lines = geodatabase.OpenDataset<FeatureClass>("TOPGIS_TLM.TLM_ERRORS_LINE");
		using var multipatchs = geodatabase.OpenDataset<FeatureClass>("TOPGIS_TLM.TLM_ERRORS_MULTIPATCH");
		using var multipoints = geodatabase.OpenDataset<FeatureClass>("TOPGIS_TLM.TLM_ERRORS_MULTIPOINT");
		using var polygons = geodatabase.OpenDataset<FeatureClass>("TOPGIS_TLM.TLM_ERRORS_POLYGON");

		var tables = new List<FeatureClass> { lines, multipatchs, multipoints, polygons };
		var sourceClasses = new List<ISourceClass>(tables.Count);

		Dictionary<IntPtr, Datastore> datastoresByHandle = new Dictionary<IntPtr, Datastore>();

		foreach (Table table in tables)
		{
			TableDefinition tableDefinition = table.GetDefinition();

			DbSourceClassSchema schema = CreateStatusSchema(tableDefinition);

			Datastore datastore = table.GetDatastore();
			datastoresByHandle.TryAdd(datastore.Handle, datastore);

			var sourceClass =
				new DatabaseSourceClass(new GdbTableIdentity(table), schema, null, null);

			sourceClasses.Add(sourceClass);
		}

		Assert.True(datastoresByHandle.Count == 1,
		            "Multiple geodatabases are referenced by the work list's source classes.");

		var gdb = (Geodatabase) datastoresByHandle.First().Value;
		var itemRepository =
			new DbStatusWorkItemRepository(sourceClasses, new WorkItemStateRepositoryMock(), gdb);

		IWorkList wl = new IssueWorkList(itemRepository, WorkListTestUtils.GetAOI(), "uniqueName", "displayName");

		var watch = new Stopwatch();
		watch.Start();

		wl.Visibility = WorkItemVisibility.All; // get all items not only Todo
		List<IWorkItem> items = wl.GetItems().ToList();
		int itemsCount = items.Count;
		
		watch.Stop();

		Assert.NotNull(wl.GetExtent());
		Console.WriteLine($"items count {itemsCount}");
		Console.WriteLine($"{watch.ElapsedMilliseconds:N0} ms");

		var filter = new QueryFilter();
		filter.SubFields = "OBJECTID";

		watch.Reset();
		watch.Start();
		items = wl.Search(filter).ToList();

		Console.WriteLine($"items {itemsCount}");
		Console.WriteLine($"{watch.ElapsedMilliseconds:N0} ms");

		Assert.AreEqual(itemsCount, items.Count);
	}

	[Test]
	public void Can_count_fgdb_workItems_measure_performance()
	{
		string path = TestDataPreparer.ExtractZip("TLM_ERRORS.gdb.zip").GetPath();

		using var geodatabase =
			new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(path, UriKind.Absolute)));
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

			var sourceClass =
				new DatabaseSourceClass(new GdbTableIdentity(table), schema, null, null);

			sourceClasses.Add(sourceClass);
		}

		Assert.True(datastoresByHandle.Count == 1,
		            "Multiple geodatabases are referenced by the work list's source classes.");

		var gdb = (Geodatabase) datastoresByHandle.First().Value;
		var itemRepository =
			new DbStatusWorkItemRepository(sourceClasses, new WorkItemStateRepositoryMock(), gdb);

		IWorkList wl = new IssueWorkList(itemRepository, WorkListTestUtils.GetAOI(), "uniqueName", "displayName");

		var watch = new Stopwatch();
		watch.Start();

		wl.Visibility = WorkItemVisibility.All; // get all items not only Todo
		List<IWorkItem> items = wl.GetItems().ToList();
		int itemsCount = items.Count;

		watch.Stop();

		Assert.NotNull(wl.GetExtent());
		Console.WriteLine($"items count {itemsCount}");
		Console.WriteLine($"{watch.ElapsedMilliseconds:N0} ms");

		var filter = new QueryFilter();
		filter.SubFields = "OBJECTID";

		watch.Reset();
		watch.Start();
		items = wl.Search(filter).ToList();

		Console.WriteLine($"items {itemsCount}");
		Console.WriteLine($"{watch.ElapsedMilliseconds:N0} ms");

		Assert.AreEqual(itemsCount, items.Count);
	}

	[Test]
	public void Can_get_extent_db_workitems()
	{
		string path = TestDataPreparer.ExtractZip("TLM_ERRORS.gdb.zip").GetPath();

		using var geodatabase =
			new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(path, UriKind.Absolute)));
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

			var sourceClass =
				new DatabaseSourceClass(new GdbTableIdentity(table), schema, null, null);

			sourceClasses.Add(sourceClass);
		}

		Assert.True(datastoresByHandle.Count == 1,
		            "Multiple geodatabases are referenced by the work list's source classes.");

		var gdb = (Geodatabase) datastoresByHandle.First().Value;

		var itemRepository =
			new DbStatusWorkItemRepository(sourceClasses, new WorkItemStateRepositoryMock(), gdb);

		IWorkList wl = new IssueWorkList(itemRepository, WorkListTestUtils.GetAOI(), "uniqueName", "displayName");
		wl.Visibility = WorkItemVisibility.All; // get all items not only Todo

		SpatialReference ch1903plus = SpatialReferenceBuilder.CreateSpatialReference(2056);

		Geometry visibleExtent = EnvelopeBuilderEx.CreateEnvelope(
			new Coordinate2D(2624810, 1184300),
			new Coordinate2D(2929350, 1186910), ch1903plus);

		List<IWorkItem> items =
			wl.GetItems(GdbQueryUtils.CreateSpatialFilter(visibleExtent)).ToList();

		Envelope extent = wl.GetExtent();
		Assert.NotNull(extent);
		Assert.False(extent.IsEmpty);
		Assert.True(GeometryUtils.Intersects(visibleExtent, extent));

		Assert.AreEqual(3861, items.Count);
	}

	[Test]
	public void Can_open_fgdb_IssueWorkList()
	{
		string path = TestDataPreparer.ExtractZip("issues.gdb.zip").GetPath();

		using var geodatabase =
			new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(path, UriKind.Absolute)));
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

			var sourceClass =
				new DatabaseSourceClass(new GdbTableIdentity(table), schema, null, null);

			sourceClasses.Add(sourceClass);
		}

		Assert.True(datastoresByHandle.Count == 1,
		            "Multiple geodatabases are referenced by the work list's source classes.");

		var gdb = (Geodatabase) datastoresByHandle.First().Value;

		var itemRepository =
			new DbStatusWorkItemRepository(sourceClasses, new WorkItemStateRepositoryMock(), gdb);

		IWorkList wl = new IssueWorkList(itemRepository, WorkListTestUtils.GetAOI(), "uniqueName", "displayName");
		wl.Visibility = WorkItemVisibility.All; // get all items not only Todo
		List<IWorkItem> items = wl.GetItems().Take(20).ToList();

		Assert.NotNull(wl.GetExtent());
		Assert.AreEqual(62, items.Count);
	}

	[Test]
	public void Can_open_fgdb_SelectionWorkList()
	{
		string path = TestDataPreparer.ExtractZip("issues.gdb.zip").GetPath();

		using var geodatabase =
			new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(path, UriKind.Absolute)));
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
			var sourceClass =
				new SelectionSourceClass(new GdbTableIdentity(table), datastore, schema, oids);

			sourceClasses.Add(sourceClass);
		}

		var repository =
			new SelectionItemRepository(sourceClasses, new WorkItemStateRepositoryMock());

		var wl = new SelectionWorkList(repository, WorkListTestUtils.GetAOI(), "uniqueName", "displayName");
		List<IWorkItem> items = wl.GetItems().ToList();

		Assert.NotNull(wl.GetExtent());
		Assert.AreEqual(9, items.Count);
	}

	[Test]
	public void LearningTest_oracle_geodatabase_handle_with_osa()
	{
		var gdb0 = new Geodatabase(new DatabaseConnectionProperties(EnterpriseDatabaseType.Oracle)
		                           {
			                           AuthenticationMode = AuthenticationMode.OSA,
			                           Instance = "TOPGIST",
			                           Database = string.Empty
		                           });
		Console.WriteLine(gdb0.GetPath());

		IntPtr gdb0Handle = gdb0.Handle;

		Connector connector = gdb0.GetConnector();

		if (connector is DatabaseConnectionProperties sde)
		{
			var gdb1 = new Geodatabase(sde);
			Console.WriteLine(gdb1.GetPath());

			IntPtr gdb1Handle = gdb1.Handle;

			Assert.AreEqual(gdb0Handle, gdb1Handle);
		}
	}

	[Test]
	public void LearningTest_oracle_geodatabase_handle()
	{
		var gdb0 = new Geodatabase(new DatabaseConnectionProperties(EnterpriseDatabaseType.Oracle)
		                           {
			                           AuthenticationMode = AuthenticationMode.OSA,
			                           Instance = "TOPGIST",
			                           Database = string.Empty
		                           });
		Uri path0 = gdb0.GetPath();

		var sde = (DatabaseConnectionProperties) gdb0.GetConnector();

		var gdb1 = new Geodatabase(sde);
		Uri path1 = gdb1.GetPath();

		Console.WriteLine(path0);
		Console.WriteLine(path1);

		Assert.AreEqual(path0, path1);
		Assert.AreEqual(gdb0.Handle, gdb1.Handle);
		Assert.AreEqual((int) gdb0.Handle, (int) gdb1.Handle);
	}

	[Test]
	public void LearningTest_impact_of_dispose_on_oracle_geodatabase_handle()
	{
		var gdb0 = new Geodatabase(new DatabaseConnectionProperties(EnterpriseDatabaseType.Oracle)
		                           {
			                           AuthenticationMode = AuthenticationMode.OSA,
			                           Instance = "TOPGIST",
			                           Database = string.Empty
		                           });
		Uri path0 = gdb0.GetPath();

		var sde = (DatabaseConnectionProperties) gdb0.GetConnector();
		gdb0.Dispose();

		var gdb1 = new Geodatabase(sde);
		Uri path1 = gdb1.GetPath();
		gdb1.Dispose();

		Console.WriteLine(path0);
		Console.WriteLine(path1);

		Assert.AreNotEqual(path0, path1);
		Assert.AreEqual(gdb0.Handle, gdb1.Handle);
		Assert.AreEqual((int) gdb0.Handle, (int) gdb1.Handle);
	}

	[Test]
	public void LearningTest_impact_of_different_geodatabase_instances_on_geodatabase_handles()
	{
		var gdb0 = new Geodatabase(new DatabaseConnectionProperties(EnterpriseDatabaseType.Oracle)
		                           {
			                           AuthenticationMode = AuthenticationMode.OSA,
			                           Instance = "TOPGIST",
			                           Database = string.Empty
		                           });
		Uri path0 = gdb0.GetPath();

		var gdb1 = new Geodatabase(new DatabaseConnectionProperties(EnterpriseDatabaseType.Oracle)
		                           {
			                           AuthenticationMode = AuthenticationMode.OSA,
			                           Instance = "TOPGIST",
			                           Database = string.Empty
		                           });
		Uri path1 = gdb1.GetPath();

		Console.WriteLine(path0);
		Console.WriteLine(path1);

		Assert.AreEqual(path0, path1);
		Assert.AreEqual(gdb0.Handle, gdb1.Handle);
		Assert.AreEqual((int) gdb0.Handle, (int) gdb1.Handle);
	}

	[Test]
	public void Can_create_SelectionWorkList_from_Shapefile()
	{
		string path = @"C:\temp\Shapefile";
		using var fileSystem =
			new FileSystemDatastore(new FileSystemConnectionPath(new Uri(path, UriKind.Absolute),
			                                                     FileSystemDatastoreType
				                                                     .Shapefile));

		var shapefile = fileSystem.OpenDataset<FeatureClass>("TLM_STRASSE_clip");

		var tables = new List<Table> { shapefile };
		var sourceClasses = new List<SelectionSourceClass>(tables.Count);

		foreach (Table table in tables)
		{
			TableDefinition tableDefinition = table.GetDefinition();

			SourceClassSchema schema = CreateSchema(tableDefinition);

			Datastore datastore = table.GetDatastore();
			List<long> oids = [0, 1, 2, 3];
			var sourceClass =
				new SelectionSourceClass(new GdbTableIdentity(table), datastore, schema, oids);

			sourceClasses.Add(sourceClass);
		}

		var repository =
			new SelectionItemRepository(sourceClasses, new WorkItemStateRepositoryMock());

		var wl = new SelectionWorkList(repository, WorkListTestUtils.GetAOI(), "uniqueName", "displayName");
		List<IWorkItem> items = wl.GetItems().ToList();

		Assert.NotNull(wl.GetExtent());
		Assert.AreEqual(4, items.Count);
	}

	[Test]
	public void LearingTest_append_subfields()
	{
		string filter = "OBJECTID   ,STATUS";

		string[] subfields = filter.Trim().Split(",");

		var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string subfield in subfields)
		{
			set.Add(subfield.Trim());
		}

		string additional = "SHAPE";
		string[] additionalSubfields = additional.Trim().Split(",");

		foreach (string subfield in additionalSubfields)
		{
			set.Add(subfield.Trim());
		}

		List<string> list = set.ToList();
		Assert.AreEqual("OBJECTID", list[0]);
		Assert.AreEqual("STATUS", list[1]);
		Assert.AreEqual("SHAPE", list[2]);
	}

	[Test]
	public void LearingTest_append_subfields2()
	{
		var filter = new QueryFilter();

		QueryFilter queryFilter = AppendSubfields(filter);

		List<string> list = queryFilter.SubFields.Split(",").ToList();
		Assert.AreEqual("OBJECTID", list[0]);
		Assert.AreEqual("STATUS", list[1]);
		Assert.AreEqual("SHAPE", list[2]);
		Assert.AreEqual("fooBar", list[3]);
	}

	private static QueryFilter AppendSubfields(QueryFilter filter)
	{
		filter.SubFields = "OBJECTID   ,STATUS";

		var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string subfield in filter.SubFields.Trim().Split(","))
		{
			set.Add(subfield.Trim());
		}

		string additional = "SHAPE,   fooBar ";

		foreach (string subfield in additional.Trim().Split(","))
		{
			set.Add(subfield.Trim());
		}

		filter.SubFields = StringUtils.Concatenate(set, ",");

		return filter;
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

		return new DbSourceClassSchema(objectIDField, shapeField, "STATUS",
		                               tableDefinition.FindField("STATUS"),
		                               (int) IssueCorrectionStatus.NotCorrected,
		                               (int) IssueCorrectionStatus.Corrected);
	}
}
