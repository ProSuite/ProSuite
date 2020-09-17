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
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class WorkListTest
	{
		private Polygon _poly0;
		private Polygon _poly1;
		private Geodatabase _geodatabase;
		private Table _table0;
		private Table _table1;
		private IWorkItemRepository _repository;

		[SetUp]
		public void SetUp()
		{
			// http://stackoverflow.com/questions/8245926/the-current-synchronizationcontext-may-not-be-used-as-a-taskscheduler
			SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

			
			_geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(_emptyIssuesGdb, UriKind.Absolute)));

			
			_table0 = _geodatabase.OpenDataset<Table>(_featureClass0);
			_table1 = _geodatabase.OpenDataset<Table>(_featureClass1);

			var tablesByGeodatabase = new Dictionary<Geodatabase, List<Table>>
			                          {
				                          {_geodatabase, new List<Table> {_table0, _table1}}
			                          };

			IRepository stateRepository = new XmlWorkItemStateRepository(_emptyIssuesGdb, @"C:\temp\states.xml");
			_repository = new IssueItemRepository(tablesByGeodatabase, stateRepository);
		}

		[TearDown]
		public void TearDown()
		{
			_table0?.Dispose();
			_table1?.Dispose();
			_geodatabase?.Dispose();
			//_repository?.Dispose();
		}

		[OneTimeSetUp]
		public void SetupFixture()
		{
			// Host must be initialized on an STA thread:
			//Host.Initialize();
			Commons.AGP.Hosting.CoreHostProxy.Initialize();


			_poly0 = PolygonConstruction
			         .StartPolygon(0, 0, 0)
			         .LineTo(0, 20, 0)
			         .LineTo(20, 20, 0)
			         .LineTo(20, 0, 0)
			         .ClosePolygon();

			_poly1 = PolygonConstruction
			         .StartPolygon(0, 0, 0)
			         .LineTo(0, 40, 0)
			         .LineTo(40, 40, 0)
			         .LineTo(40, 0, 0)
			         .ClosePolygon();
		}

		private const string _emptyIssuesGdb = @"C:\git\ProSuite\src\ProSuite.AGP.WorkList.Test\TestData\issues_empty.gdb";
		private string _featureClassName = "IssuePolygons";
		private string _featureClass0 = "featureClass0";
		private string _featureClass1 = "featureClass1";

		#region work list navigation tests

		[Test]
		public void CanGoNext()
		{
			IWorkItem item1 = new WorkItemMock(1);
			IWorkItem item2 = new WorkItemMock(2);
			IWorkItem item3 = new WorkItemMock(3);
			IWorkItem item4 = new WorkItemMock(4);
			var repository = new ItemRepositoryMock(new List<IWorkItem> {item1, item2, item3, item4});
			
			IWorkList wl = new MemoryQueryWorkList(repository, "work list");

			wl.GoFirst();
			Assert.AreEqual(item1, wl.Current);
			Assert.True(wl.Current?.Visited);

			wl.GoNext();
			Assert.AreEqual(item2, wl.Current);
			Assert.True(wl.Current?.Visited);

			wl.GoNext();
			Assert.AreEqual(item3, wl.Current);
			Assert.True(wl.Current?.Visited);

			wl.GoNext();
			Assert.AreEqual(item4, wl.Current);
			Assert.True(wl.Current?.Visited);

			// end of work list, current item is the same as before
			wl.GoNext();
			Assert.AreEqual(item4, wl.Current);
			Assert.True(wl.Current?.Visited);
		}

		#endregion

		[Test]
		public void Can_handle_WorkList_extent_on_insert()
		{
			InsertFeature(_featureClass0, _poly0);
			InsertFeature(_featureClass1, _poly0);

			try
			{
				IWorkList workList = new IssueWorkList(_repository, "work list");
				AssertEqual(_poly0.Extent, workList.Extent);

				Assert.AreEqual(2, workList.GetItems().ToList().Count);

				InsertFeature(_featureClass0, _poly1);

				var inserts = new Dictionary<Table, List<long>> {{_table0, new List<long> {2}}};
				var deletes = new Dictionary<Table, List<long>>();
				var updates = new Dictionary<Table, List<long>>();

				((IRowCache) workList).ProcessChanges(inserts, deletes, updates);

				Assert.AreEqual(3, workList.GetItems().ToList().Count);

				Envelope newExtent = GeometryUtils.Union(_poly0.Extent, _poly1.Extent);

				AssertEqual(newExtent, workList.Extent);
			}
			finally
			{
				DeleteAllRows(_featureClass0);
				DeleteAllRows(_featureClass1);
			}
		}

		[Test]
		public void Can_handle_WorkList_extent_on_update()
		{
			InsertFeature(_featureClass0, _poly0);
			InsertFeature(_featureClass1, _poly0);

			try
			{
				IWorkList workList = new IssueWorkList(_repository, "work list");
				AssertEqual(_poly0.Extent, workList.Extent);

				Assert.AreEqual(2, workList.GetItems().ToList().Count);

				UpdateFeatureGeometry(_featureClass0, _poly1);
				UpdateFeatureGeometry(_featureClass1, _poly1);

				var inserts = new Dictionary<Table, List<long>>();
				var deletes = new Dictionary<Table, List<long>>();
				var updates = new Dictionary<Table, List<long>> {{_table0, new List<long> {1}}, {_table1, new List<long> {1}}};

				((IRowCache) workList).ProcessChanges(inserts, deletes, updates);

				var items = workList.GetItems().ToList();
				Assert.AreEqual(2, items.Count);

				List<long> ids = items.Select(i => i.OID).ToList().ConvertAll(i => (long)i);
				QueryFilter filter = GdbQueryUtils.CreateFilter(ids);

				foreach (IWorkItem item in workList.GetItems(filter))
				{
					AssertEqual(_poly1.Extent, item.Extent);
				}

				AssertEqual(_poly1.Extent, workList.Extent);
			}
			finally
			{
				DeleteAllRows(_featureClass0);
				DeleteAllRows(_featureClass1);
			}
		}

		[Test]
		public void Can_handle_WorkList_extent_on_delete()
		{
			InsertFeature(_featureClass0, _poly0);
			InsertFeature(_featureClass1, _poly1);

			try
			{
				IWorkList workList = new IssueWorkList(_repository, "work list");
				Envelope newExtent = GeometryUtils.Union(_poly0.Extent, _poly1.Extent);
				AssertEqual(newExtent, workList.Extent);

				Assert.AreEqual(2, workList.GetItems().ToList().Count);

				DeleteRow(_featureClass1);

				var inserts = new Dictionary<Table, List<long>>();
				var deletes = new Dictionary<Table, List<long>> {{_table1, new List<long> {1}}};
				var updates = new Dictionary<Table, List<long>>();

				((IRowCache) workList).ProcessChanges(inserts, deletes, updates);

				Assert.AreEqual(1, workList.GetItems().ToList().Count);

				AssertEqual(_poly0.Extent, workList.Extent);
			}
			finally
			{
				DeleteAllRows(_featureClass0);
				DeleteAllRows(_featureClass1);
			}
		}

		[Test]
		public void Respect_AreaOfInterest_LearningTest()
		{
			Polygon polygon = PolygonConstruction
			                  .StartPolygon(0, 0)
			                  .LineTo(0, 20)
			                  .LineTo(20, 20)
			                  .LineTo(20, 0)
			                  .ClosePolygon();

			Polygon areaOfInterest = PolygonConstruction
			                         .StartPolygon(100, 100)
			                         .LineTo(100, 120)
			                         .LineTo(120, 120)
			                         .LineTo(120, 100)
			                         .ClosePolygon();

			var rowCount = 4;
			TestUtils.InsertRows(_emptyIssuesGdb, _featureClassName, polygon, rowCount);

			try
			{
				var uri = new Uri(_emptyIssuesGdb, UriKind.Absolute);

				var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(uri));

				var table = geodatabase.OpenDataset<Table>(_featureClassName);
				Dictionary<Geodatabase, List<Table>> tablesByGeodatabase = new Dictionary<Geodatabase, List<Table>>
					{
						{geodatabase, new List<Table> {table}}
					};

				IRepository stateRepository = new XmlWorkItemStateRepository(_emptyIssuesGdb, @"C:\temp\states.xml");
				IWorkItemRepository repository = new IssueItemRepository(tablesByGeodatabase, stateRepository);

				IWorkList workList = new MemoryQueryWorkList(repository, "work list");
				workList.AreaOfInterest = areaOfInterest;

				IEnumerable<IWorkItem> items = workList.GetItems();

				Assert.AreEqual(0, items.Count());
			}
			finally
			{
				TestUtils.DeleteAllRows(_emptyIssuesGdb, _featureClassName);
			}
		}

		[Test]
		public void Measure_performance_query_items_inMemory()
		{
			Polygon polygon = PolygonConstruction
			                  .StartPolygon(0, 0)
			                  .LineTo(0, 20)
			                  .LineTo(20, 20)
			                  .LineTo(20, 0)
			                  .ClosePolygon();

			Polygon areaOfInterest = PolygonConstruction
			                         .StartPolygon(0, 0)
			                         .LineTo(0, 100)
			                         .LineTo(100, 100)
			                         .LineTo(100, 0)
			                         .ClosePolygon();

			var rowCount = 10000;
			TestUtils.InsertRows(_emptyIssuesGdb, _featureClassName, polygon, rowCount);

			try
			{
				var uri = new Uri(_emptyIssuesGdb, UriKind.Absolute);

				var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(uri));
				var table = geodatabase.OpenDataset<Table>(_featureClassName);
				Dictionary<Geodatabase, List<Table>> tablesByGeodatabase = new Dictionary<Geodatabase, List<Table>>
				                                                           {
					                                                           {geodatabase, new List<Table> {table}}
				                                                           };

				IRepository stateRepository = new XmlWorkItemStateRepository(_emptyIssuesGdb, @"C:\temp\states.xml");
				IWorkItemRepository repository = new IssueItemRepository(tablesByGeodatabase, stateRepository);

				IWorkList workList = new MemoryQueryWorkList(repository, "work list");
				workList.AreaOfInterest = areaOfInterest;

				var filter = GdbQueryUtils.CreateSpatialFilter(areaOfInterest);

				var watch = new Stopwatch();
				watch.Start();

				IEnumerable<IWorkItem> items = workList.GetItems(filter);

				watch.Stop();
				Console.WriteLine($"{watch.ElapsedMilliseconds} ms");

				Assert.AreEqual(rowCount, items.Count());
			}
			finally
			{
				TestUtils.DeleteAllRows(_emptyIssuesGdb, _featureClassName);
			}
		}

		[Test]
		public void Measure_performance_query_items_from_GDB()
		{
			Polygon polygon = PolygonConstruction
			                  .StartPolygon(0, 0)
			                  .LineTo(0, 20)
			                  .LineTo(20, 20)
			                  .LineTo(20, 0)
			                  .ClosePolygon();

			Polygon areaOfInterest = PolygonConstruction
			                         .StartPolygon(0, 0)
			                         .LineTo(0, 100)
			                         .LineTo(100, 100)
			                         .LineTo(100, 0)
			                         .ClosePolygon();

			var rowCount = 10000;
			TestUtils.InsertRows(_emptyIssuesGdb, _featureClassName, polygon, rowCount);

			try
			{
				var uri = new Uri(_emptyIssuesGdb, UriKind.Absolute);

				var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(uri));

				var table = geodatabase.OpenDataset<Table>(_featureClassName);
				Dictionary<Geodatabase, List<Table>> tablesByGeodatabase = new Dictionary<Geodatabase, List<Table>>
				                                                           {
					                                                           {geodatabase, new List<Table> {table}}
				                                                           };

				IRepository stateRepository = new XmlWorkItemStateRepository(_emptyIssuesGdb, @"C:\temp\states.xml");
				IWorkItemRepository repository = new IssueItemRepository(tablesByGeodatabase, stateRepository);

				IWorkList workList = new GdbQueryWorkList(repository, "work list");
				workList.AreaOfInterest = areaOfInterest;
				
				var filter = GdbQueryUtils.CreateSpatialFilter(areaOfInterest);

				var watch = new Stopwatch();
				watch.Start();

				IEnumerable<IWorkItem> items = workList.GetItems(filter);

				watch.Stop();
				Console.WriteLine($"{watch.ElapsedMilliseconds} ms");

				Assert.AreEqual(rowCount, items.Count());
			}
			finally
			{
				TestUtils.DeleteAllRows(_emptyIssuesGdb, _featureClassName);
			}
		}

		[Test]
		public void WorkItemService_LearningTest()
		{
			Polygon polygon = PolygonConstruction
			                  .StartPolygon(0, 0)
			                  .LineTo(0, 20)
			                  .LineTo(20, 20)
			                  .LineTo(20, 0)
			                  .ClosePolygon();

			var rowCount = 4;
			TestUtils.InsertRows(_emptyIssuesGdb, _featureClassName, polygon, rowCount);

			try
			{
				var uri = new Uri(_emptyIssuesGdb, UriKind.Absolute);

				var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(uri));
				var table = geodatabase.OpenDataset<Table>(_featureClassName);
				Dictionary<Geodatabase, List<Table>> tablesByGeodatabase = new Dictionary<Geodatabase, List<Table>>
				                                                           {
					                                                           {geodatabase, new List<Table> {table}}
				                                                           };

				IRepository stateRepository = new XmlWorkItemStateRepository(_emptyIssuesGdb, @"C:\temp\states.xml");
				IWorkItemRepository repository = new IssueItemRepository(tablesByGeodatabase, stateRepository);

				IWorkList workList = new GdbQueryWorkList(repository, "work list");

				var items = workList.GetItems().Cast<IssueItem>().ToList();

				Assert.AreEqual("Bart", items[0].IssueCodeDescription);
				Assert.AreEqual("Bart", items[1].IssueCodeDescription);
				Assert.AreEqual("Bart", items[2].IssueCodeDescription);
				Assert.AreEqual("Bart", items[3].IssueCodeDescription);
			}
			finally
			{
				TestUtils.DeleteAllRows(_emptyIssuesGdb, _featureClassName);
			}
		}

		private static void InsertFeature(string featureClassName, Polygon polygon)
		{
			TestUtils.InsertRows(_emptyIssuesGdb, featureClassName, polygon, 1);
		}

		private static void UpdateFeatureGeometry(string featureClassName, Polygon polygon)
		{
			TestUtils.UpdateFeatureGeometry(_emptyIssuesGdb, featureClassName, polygon, 1);
		}

		private static void DeleteRow(string featureClassName)
		{
			TestUtils.DeleteRow(_emptyIssuesGdb, featureClassName, 1);
		}

		private static void DeleteAllRows(string featureClassName)
		{
			TestUtils.DeleteAllRows(_emptyIssuesGdb, featureClassName);
		}

		private static void AssertEqual(Envelope expected, Envelope actual)
		{
			Assert.True(AreEqual(expected, actual));
		}

		private static bool AreEqual(Envelope expected, Envelope actual)
		{
			// 1.1 is default expansion of work items
			return expected.Expand(1.1, 1.1, true).IsEqual(actual);
		}
	}
}
