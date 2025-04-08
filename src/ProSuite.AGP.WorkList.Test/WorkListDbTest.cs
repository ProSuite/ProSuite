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
using ProSuite.Commons.AGP.Hosting;
using ProSuite.Commons.Testing;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.AGP.WorkList.Test;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class WorkListDbTest
{
	private string _path;

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
		ITestDataArchive testDataArchive = TestDataPreparer.ExtractZip("TLM_ERRORS.gdb.zip");
		_path = testDataArchive.GetPath();

		using var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(_path, UriKind.Absolute)));
		using var lines = geodatabase.OpenDataset<FeatureClass>("TLM_ERRORS_LINE");
		using var multipatchs = geodatabase.OpenDataset<FeatureClass>("TLM_ERRORS_MULTIPATCH");
		using var multipoints = geodatabase.OpenDataset<FeatureClass>("TLM_ERRORS_MULTIPOINT");
		using var polygons = geodatabase.OpenDataset<FeatureClass>("TLM_ERRORS_POLYGON");

		var tables = new List<FeatureClass> { lines, multipatchs, multipoints, polygons };
		var sourceClassDefinitions = new List<DbStatusSourceClassDefinition>(tables.Count);

		foreach (Table table in tables)
		{
			string defQuery = GetDefaultDefinitionQuery(table);

			TableDefinition tableDefinition = table.GetDefinition();

			WorkListStatusSchema statusSchema = CreateStatusSchema(tableDefinition);

			var sourceClassDef = new DbStatusSourceClassDefinition(table, defQuery, statusSchema)
			                     {
				                     AttributeReader = new NoOpAttributeReader()
			                     };

			sourceClassDefinitions.Add(sourceClassDef);
		}

		var itemRepository =
			new DbStatusWorkItemRepository(sourceClassDefinitions,
			                               new EmptyWorkItemStateRepository());

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
		ITestDataArchive testDataArchive = TestDataPreparer.ExtractZip("TLM_ERRORS.gdb.zip");
		_path = testDataArchive.GetPath();

		using var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(_path, UriKind.Absolute)));
		using var lines = geodatabase.OpenDataset<FeatureClass>("TLM_ERRORS_LINE");
		using var multipatchs = geodatabase.OpenDataset<FeatureClass>("TLM_ERRORS_MULTIPATCH");
		using var multipoints = geodatabase.OpenDataset<FeatureClass>("TLM_ERRORS_MULTIPOINT");
		using var polygons = geodatabase.OpenDataset<FeatureClass>("TLM_ERRORS_POLYGON");

		var tables = new List<FeatureClass> { lines, multipatchs, multipoints, polygons };
		var sourceClassDefinitions = new List<DbStatusSourceClassDefinition>(tables.Count);

		foreach (Table table in tables)
		{
			string defQuery = GetDefaultDefinitionQuery(table);

			TableDefinition tableDefinition = table.GetDefinition();

			WorkListStatusSchema statusSchema = CreateStatusSchema(tableDefinition);

			var sourceClassDef = new DbStatusSourceClassDefinition(table, defQuery, statusSchema)
			                     {
				                     AttributeReader = new NoOpAttributeReader()
			                     };

			sourceClassDefinitions.Add(sourceClassDef);
		}

		var itemRepository =
			new DbStatusWorkItemRepository(sourceClassDefinitions,
			                               new EmptyWorkItemStateRepository());

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

	private string GetDefaultDefinitionQuery(Table table)
	{
		return null;
	}

	private static WorkListStatusSchema CreateStatusSchema(TableDefinition tableDefinition)
	{
		return new WorkListStatusSchema("STATUS", tableDefinition.FindField("STATUS"),
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
