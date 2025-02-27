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
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Core.Test;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.AGP.WorkList.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class WorkListTest
	{
		private Polygon _poly0;
		private Polygon _poly1;
		private Geodatabase _geodatabase;
		private Table _issuePoints;
		private Table _issueLines;
		private IWorkItemRepository _repository;

		[SetUp]
		public void SetUp()
		{
			// http://stackoverflow.com/questions/8245926/the-current-synchronizationcontext-may-not-be-used-as-a-taskscheduler
			SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

			//_geodatabase =
			//	new Geodatabase(
			//		new FileGeodatabaseConnectionPath(new Uri(_emptyIssuesGdb, UriKind.Absolute)));

			//_issuePoints = _geodatabase.OpenDataset<Table>(_issuePointsName);
			//_issueLines = _geodatabase.OpenDataset<Table>(_issueLinesName);

			//var tablesByGeodatabase = new Dictionary<Datastore, List<Table>>
			//                          {
			//	                          {
			//		                          _geodatabase,
			//		                          new List<Table> {_issuePoints, _issueLines}
			//	                          }
			//                          };

			//IWorkItemStateRepository stateRepository =
			//	new XmlWorkItemStateRepository(@"C:\temp\states.xml", null, null);
			//_repository = new IssueItemRepository(new List<Table> { _issuePoints, _issueLines }, stateRepository);
		}

		[TearDown]
		public void TearDown()
		{
			_issuePoints?.Dispose();
			_issueLines?.Dispose();
			_geodatabase?.Dispose();
			//_repository?.Dispose();
		}

		[OneTimeSetUp]
		public void SetupFixture()
		{
			// Host must be initialized on an STA thread:
			//Host.Initialize();
			CoreHostProxy.Initialize();

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

		private const string _emptyIssuesGdb =
			@"C:\git\ProSuite\src\ProSuite.AGP.WorkList.Test\TestData\issues_empty.gdb";

		private readonly string _issuePolygons = "IssuePolygons";
		private readonly string _issuePointsName = "IssuePoints";
		private readonly string _issueLinesName = "IssueLines";

		#region item chunks tests

		[Test]
		public void Can_get_items_by_task_id_only_once()
		{
			var capacity = 8000;
			var items = new List<IWorkItem>(capacity);

			for (int i = 0; i < capacity; i++)
			{
				items.Add(new WorkItemMock(i));
			}

			var workList = new SelectionWorkList(new ItemRepositoryMock(items), "foo", "nice foo");

			List<IWorkItem> list;
			Assert.True(workList.TryGetItems(42, out list));
			Assert.NotNull(list);
			Assert.IsNotEmpty(list);

			Assert.False(workList.TryGetItems(42, out list));
			Assert.Null(list);
			Assert.IsNull(list);
		}

		[Test]
		public void Only_three_tasks_can_get_items()
		{
			var capacity = 4200;
			var items = new List<IWorkItem>(capacity);

			for (int i = 0; i < capacity; i++)
			{
				items.Add(new WorkItemMock(i));
			}

			var workList = new SelectionWorkList(new ItemRepositoryMock(items), "foo", "nice foo");

			Assert.True(workList.TryGetItems(42, out List<IWorkItem> firstList));
			Assert.NotNull(firstList);
			Assert.IsNotEmpty(firstList);
			Assert.AreEqual(1400, firstList.Count);

			Assert.False(workList.TryGetItems(42, out List<IWorkItem> secondList));
			Assert.Null(secondList);
			Assert.IsNull(secondList);

			Assert.True(workList.TryGetItems(1, out List<IWorkItem> thirdList));
			Assert.NotNull(thirdList);
			Assert.IsNotEmpty(thirdList);
			Assert.AreEqual(1400, thirdList.Count);

			Assert.True(workList.TryGetItems(2, out List<IWorkItem> fourthList));
			Assert.NotNull(fourthList);
			Assert.IsNotEmpty(fourthList);
			Assert.AreEqual(1400, fourthList.Count);

			Assert.False(workList.TryGetItems(3, out List<IWorkItem> fifthList));
			Assert.Null(fifthList);

			Assert.False(workList.TryGetItems(4, out List<IWorkItem> sixthList));
			Assert.Null(sixthList);
		}

		[Test]
		public void Can_get_evenly_distributetd_item_chunks()
		{
			var capacity = 35;
			var items = new List<IWorkItem>(capacity);

			for (int i = 0; i < capacity; i++)
			{
				items.Add(new WorkItemMock(i));
			}

			var workList = new SelectionWorkList(new ItemRepositoryMock(items), "foo", "nice foo");

			Assert.True(workList.TryGetItems(1, out List<IWorkItem> firstList));
			Assert.NotNull(firstList);
			Assert.IsNotEmpty(firstList);
			Assert.AreEqual(13, firstList.Count);

			Assert.True(workList.TryGetItems(2, out List<IWorkItem> secondList));
			Assert.NotNull(secondList);
			Assert.IsNotEmpty(secondList);
			Assert.AreEqual(11, secondList.Count);

			Assert.True(workList.TryGetItems(3, out List<IWorkItem> thirdList));
			Assert.NotNull(thirdList);
			Assert.IsNotEmpty(thirdList);
			Assert.AreEqual(11, thirdList.Count);
		}

		[Test]
		public void Can_process_item_chunks_for_very_few_items()
		{
			var capacity = 1;
			var items = new List<IWorkItem>(capacity);

			for (int i = 0; i < capacity; i++)
			{
				items.Add(new WorkItemMock(i));
			}

			var workList = new SelectionWorkList(new ItemRepositoryMock(items), "foo", "nice foo");

			Assert.True(workList.TryGetItems(1, out List<IWorkItem> firstList));
			Assert.NotNull(firstList);
			Assert.IsNotEmpty(firstList);
			Assert.AreEqual(1, firstList.Count);

			Assert.True(workList.TryGetItems(2, out List<IWorkItem> secondList));
			Assert.NotNull(secondList);
			Assert.IsEmpty(secondList);

			Assert.True(workList.TryGetItems(3, out List<IWorkItem> thirdList));
			Assert.NotNull(thirdList);
			Assert.IsEmpty(thirdList);
		}

		[Test]
		public void Can_process_item_chunks_for_three_items()
		{
			var capacity = 3;
			var items = new List<IWorkItem>(capacity);

			for (int i = 0; i < capacity; i++)
			{
				items.Add(new WorkItemMock(i));
			}

			var workList = new SelectionWorkList(new ItemRepositoryMock(items), "foo", "nice foo");

			Assert.True(workList.TryGetItems(1, out List<IWorkItem> firstList));
			Assert.NotNull(firstList);
			Assert.IsNotEmpty(firstList);
			Assert.AreEqual(1, firstList.Count);

			Assert.True(workList.TryGetItems(2, out List<IWorkItem> secondList));
			Assert.NotNull(secondList);
			Assert.IsNotEmpty(secondList);
			Assert.AreEqual(1, secondList.Count);

			Assert.True(workList.TryGetItems(3, out List<IWorkItem> thirdList));
			Assert.NotNull(thirdList);
			Assert.IsNotEmpty(thirdList);
			Assert.AreEqual(1, thirdList.Count);
		}

		#endregion

		#region work list navigation tests

		[Test]
		public void Can_go_next()
		{
			// The items have to be visible to enable the work list to go to the next item.
			// Normally GoNearest() sets Item.Visible = true.
			IWorkItem item1 = new WorkItemMock(1) {Visited = true};
			IWorkItem item2 = new WorkItemMock(2) {Visited = true};
			IWorkItem item3 = new WorkItemMock(3) {Visited = true};
			IWorkItem item4 = new WorkItemMock(4) {Visited = true};
			var repository =
				new ItemRepositoryMock(new List<IWorkItem> {item1, item2, item3, item4});

			IWorkList wl = new MemoryQueryWorkList(repository, "work list");

			wl.GoNext();
			Assert.AreEqual(item2, wl.Current);
			Assert.True(wl.Current?.Visited);

			wl.GoNext();
			Assert.AreEqual(item3, wl.Current);
			Assert.True(wl.Current?.Visited);

			wl.GoNext();
			Assert.AreEqual(item4, wl.Current);
			Assert.True(wl.Current?.Visited);

			Assert.False(wl.CanGoNext());
			Assert.AreEqual(item4, wl.Current);
		}

		[Test]
		public void Can_go_first_again()
		{
			IWorkItem item1 = new WorkItemMock(1);
			IWorkItem item2 = new WorkItemMock(2);
			IWorkItem item3 = new WorkItemMock(3);
			IWorkItem item4 = new WorkItemMock(4);
			var repository =
				new ItemRepositoryMock(new List<IWorkItem> {item1, item2, item3, item4});

			IWorkList wl = new MemoryQueryWorkList(repository, "work list");

			wl.GoNext();
			Assert.AreEqual(item1, wl.Current);
			Assert.True(wl.Current?.Visited);

			wl.GoNext();
			Assert.AreEqual(item2, wl.Current);
			Assert.True(wl.Current?.Visited);

			wl.GoNext();
			Assert.AreEqual(item3, wl.Current);
			Assert.True(wl.Current?.Visited);

			// go first again
			wl.GoFirst();
			Assert.AreEqual(item1, wl.Current);
			Assert.True(wl.Current?.Visited);

			wl.GoNext();
			Assert.AreEqual(item2, wl.Current);
			Assert.True(wl.Current?.Visited);

			// go first again
			wl.GoFirst();
			Assert.AreEqual(item1, wl.Current);
			Assert.True(wl.Current?.Visited);
		}

		[Test]
		public void Can_go_previous()
		{
			IWorkItem item1 = new WorkItemMock(1);
			IWorkItem item2 = new WorkItemMock(2);
			IWorkItem item3 = new WorkItemMock(3);
			IWorkItem item4 = new WorkItemMock(4);
			var repository =
				new ItemRepositoryMock(new List<IWorkItem> {item1, item2, item3, item4});

			IWorkList wl = new MemoryQueryWorkList(repository, "work list");

			wl.GoNext();
			Assert.AreEqual(item1, wl.Current);
			Assert.True(wl.Current?.Visited);

			wl.GoNext();
			Assert.AreEqual(item2, wl.Current);
			Assert.True(wl.Current?.Visited);

			wl.GoNext();
			Assert.AreEqual(item3, wl.Current);
			Assert.True(wl.Current?.Visited);

			// go previous
			wl.GoPrevious();
			Assert.AreEqual(item2, wl.Current);
			Assert.True(wl.Current?.Visited);

			// go previous again
			wl.GoPrevious();
			Assert.AreEqual(item1, wl.Current);
			Assert.True(wl.Current?.Visited);

			wl.GoNext();
			Assert.AreEqual(item2, wl.Current);
			Assert.True(wl.Current?.Visited);
		}

		[Test]
		public void Can_go_nearest()
		{
			MapPoint pt7 = PolygonConstruction.CreateMapPoint(7, 0, 0);
			MapPoint pt10 = PolygonConstruction.CreateMapPoint(10, 0, 0);
			MapPoint pt15 = PolygonConstruction.CreateMapPoint(15, 0, 0);

			var item7 = new WorkItemMock(7, pt7);
			var item10 = new WorkItemMock(10, pt10);
			var item15 = new WorkItemMock(15, pt15);

			var repository = new ItemRepositoryMock(new[] {item7, item10, item15});

			IWorkList wl = new MemoryQueryWorkList(repository, nameof(Can_go_nearest));

			Geometry reference = PolygonConstruction.CreateMapPoint(11, 0, 0);

			// go to item10
			Assert.True(wl.CanGoNearest());
			wl.GoNearest(reference);
			Assert.AreEqual(item10, wl.Current);
			Assert.True(wl.Current?.Visited);

			// go to item7
			Assert.True(wl.CanGoNearest());
			Assert.NotNull(wl.Current);
			wl.GoNearest(wl.Current.Extent);
			Assert.AreEqual(item7, wl.Current);
			Assert.True(wl.Current?.Visited);

			// go to item15
			Assert.True(wl.CanGoNearest());
			Assert.NotNull(wl.Current);
			wl.GoNearest(wl.Current.Extent);
			Assert.AreEqual(item15, wl.Current);
			Assert.True(wl.Current?.Visited);

			// Now all are visited, what is the next item? None because there is no
			// more item *after* the last item15.
			// Now we need to go to item *before* the last one.
			Assert.False(wl.CanGoNearest());

			Assert.True(wl.CanGoPrevious());
			wl.GoPrevious();
			Assert.AreEqual(item10, wl.Current);

			// Now we can go nearest again which is item7 (nearest to item10)
			Assert.True(wl.CanGoNearest());
			Assert.NotNull(wl.Current);
			wl.GoNearest(wl.Current.Extent);
			Assert.AreEqual(item7, wl.Current);
			Assert.True(wl.Current?.Visited);
		}

		[Test]
		public void Cannot_go_first_again_if_first_item_is_set_done()
		{
			IWorkItem item1 = new WorkItemMock(1);
			IWorkItem item2 = new WorkItemMock(2);
			IWorkItem item3 = new WorkItemMock(3);
			IWorkItem item4 = new WorkItemMock(4);
			var repository =
				new ItemRepositoryMock(new List<IWorkItem> {item1, item2, item3, item4});

			IWorkList wl = new MemoryQueryWorkList(repository, "work list");

			wl.GoFirst();
			Assert.AreEqual(item1, wl.Current);
			Assert.True(wl.Current?.Visited);

			wl.GoNext();
			Assert.AreEqual(item2, wl.Current);
			Assert.True(wl.Current?.Visited);

			wl.GoFirst();
			Assert.AreEqual(item1, wl.Current);
			Assert.True(wl.Current?.Visited);

			// set status done and update work list
			wl.Current.Status = WorkItemStatus.Done;
			wl.SetStatus(wl.Current, WorkItemStatus.Done);

			// second item is now the first in work list
			// because first item is set to done and therefor 'not visible'
			wl.GoFirst();
			Assert.AreEqual(item2, wl.Current);
			Assert.True(wl.Current?.Visited);
		}

		#endregion

		[Test]
		public void Can_handle_WorkList_extent_on_insert()
		{
			InsertFeature(_issuePointsName, _poly0);
			InsertFeature(_issueLinesName, _poly0);

			try
			{
				IWorkList workList = new IssueWorkList(_repository, "work list");
				AssertEqual(_poly0.Extent, workList.Extent);

				Assert.AreEqual(2, workList.GetItems().ToList().Count);

				InsertFeature(_issuePointsName, _poly1);

				var inserts = new Dictionary<Table, List<long>>
				              {{_issuePoints, new List<long> {2}}};
				var deletes = new Dictionary<Table, List<long>>();
				var updates = new Dictionary<Table, List<long>>();

				((IRowCache) workList).ProcessChanges(inserts, deletes, updates);

				Assert.AreEqual(3, workList.GetItems().ToList().Count);

				Envelope newExtent = GeometryUtils.Union(_poly0.Extent, _poly1.Extent);

				AssertEqual(newExtent, workList.Extent);
			}
			finally
			{
				DeleteAllRows(_issuePointsName);
				DeleteAllRows(_issueLinesName);
			}
		}

		[Test]
		public void Can_handle_WorkList_extent_on_update()
		{
			InsertFeature(_issuePointsName, _poly0);
			InsertFeature(_issueLinesName, _poly0);

			try
			{
				IWorkList workList = new IssueWorkList(_repository, "work list");
				AssertEqual(_poly0.Extent, workList.Extent);

				Assert.AreEqual(2, workList.GetItems().ToList().Count);

				UpdateFeatureGeometry(_issuePointsName, _poly1);
				UpdateFeatureGeometry(_issueLinesName, _poly1);

				var inserts = new Dictionary<Table, List<long>>();
				var deletes = new Dictionary<Table, List<long>>();
				var updates = new Dictionary<Table, List<long>>
				              {
					              {_issuePoints, new List<long> {1}},
					              {_issueLines, new List<long> {1}}
				              };

				((IRowCache) workList).ProcessChanges(inserts, deletes, updates);

				var items = workList.GetItems().ToList();
				Assert.AreEqual(2, items.Count);

				List<long> ids = items.Select(i => i.OID).ToList().ConvertAll(i => (long) i);
				QueryFilter filter = GdbQueryUtils.CreateFilter(ids);

				foreach (IWorkItem item in workList.GetItems(filter))
				{
					AssertEqual(_poly1.Extent, item.Extent);
				}

				AssertEqual(_poly1.Extent, workList.Extent);
			}
			finally
			{
				DeleteAllRows(_issuePointsName);
				DeleteAllRows(_issueLinesName);
			}
		}

		[Test]
		public void Can_handle_WorkList_extent_on_delete()
		{
			InsertFeature(_issuePointsName, _poly0);
			InsertFeature(_issueLinesName, _poly1);

			try
			{
				IWorkList workList = new IssueWorkList(_repository, "work list");
				Envelope newExtent = GeometryUtils.Union(_poly0.Extent, _poly1.Extent);
				AssertEqual(newExtent, workList.Extent);

				Assert.AreEqual(2, workList.GetItems().ToList().Count);

				DeleteRow(_issueLinesName);

				var inserts = new Dictionary<Table, List<long>>();
				var deletes = new Dictionary<Table, List<long>> {{_issueLines, new List<long> {1}}};
				var updates = new Dictionary<Table, List<long>>();

				((IRowCache) workList).ProcessChanges(inserts, deletes, updates);

				Assert.AreEqual(1, workList.GetItems().ToList().Count);

				AssertEqual(_poly0.Extent, workList.Extent);
			}
			finally
			{
				DeleteAllRows(_issuePointsName);
				DeleteAllRows(_issueLinesName);
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
			TestUtils.InsertRows(_emptyIssuesGdb, _issuePolygons, polygon, rowCount);

			try
			{
				var uri = new Uri(_emptyIssuesGdb, UriKind.Absolute);

				var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(uri));

				var table = geodatabase.OpenDataset<Table>(_issuePolygons);
				Dictionary<Datastore, List<Table>> tablesByGeodatabase =
					new Dictionary<Datastore, List<Table>>
					{
						{geodatabase, new List<Table> {table}}
					};

				IWorkItemStateRepository stateRepository =
					new XmlWorkItemStateRepository(@"C:\temp\states.xml", null, null);
				IWorkItemRepository repository =
					new IssueItemRepository(new List<Table> { table }, stateRepository);

				IWorkList workList = new MemoryQueryWorkList(repository, "work list");
				workList.AreaOfInterest = areaOfInterest;

				IEnumerable<IWorkItem> items = workList.GetItems();

				Assert.AreEqual(0, items.Count());
			}
			finally
			{
				TestUtils.DeleteAllRows(_emptyIssuesGdb, _issuePolygons);
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
			TestUtils.InsertRows(_emptyIssuesGdb, _issuePolygons, polygon, rowCount);

			try
			{
				var uri = new Uri(_emptyIssuesGdb, UriKind.Absolute);

				var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(uri));
				var table = geodatabase.OpenDataset<Table>(_issuePolygons);
				Dictionary<Datastore, List<Table>> tablesByGeodatabase =
					new Dictionary<Datastore, List<Table>>
					{
						{geodatabase, new List<Table> {table}}
					};

				IWorkItemStateRepository stateRepository =
					new XmlWorkItemStateRepository(@"C:\temp\states.xml", null, null);
				IWorkItemRepository repository =
					new IssueItemRepository(new List<Table> { table }, stateRepository);

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
				TestUtils.DeleteAllRows(_emptyIssuesGdb, _issuePolygons);
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
			TestUtils.InsertRows(_emptyIssuesGdb, _issuePolygons, polygon, rowCount);

			try
			{
				var uri = new Uri(_emptyIssuesGdb, UriKind.Absolute);

				var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(uri));

				var table = geodatabase.OpenDataset<Table>(_issuePolygons);
				Dictionary<Datastore, List<Table>> tablesByGeodatabase =
					new Dictionary<Datastore, List<Table>>
					{
						{geodatabase, new List<Table> {table}}
					};

				IWorkItemStateRepository stateRepository =
					new XmlWorkItemStateRepository(@"C:\temp\states.xml", null, null);
				IWorkItemRepository repository =
					new IssueItemRepository(new List<Table> { table }, stateRepository);

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
				TestUtils.DeleteAllRows(_emptyIssuesGdb, _issuePolygons);
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
			TestUtils.InsertRows(_emptyIssuesGdb, _issuePolygons, polygon, rowCount);

			try
			{
				var uri = new Uri(_emptyIssuesGdb, UriKind.Absolute);

				var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(uri));
				var table = geodatabase.OpenDataset<Table>(_issuePolygons);
				Dictionary<Datastore, List<Table>> tablesByGeodatabase =
					new Dictionary<Datastore, List<Table>>
					{
						{geodatabase, new List<Table> {table}}
					};

				IWorkItemStateRepository stateRepository =
					new XmlWorkItemStateRepository(@"C:\temp\states.xml", null, null);
				IWorkItemRepository repository =
					new IssueItemRepository(new List<Table> { table }, stateRepository);

				IWorkList workList = new GdbQueryWorkList(repository, "work list");

				var items = workList.GetItems().Cast<IssueItem>().ToList();

				Assert.AreEqual("Bart", items[0].IssueCodeDescription);
				Assert.AreEqual("Bart", items[1].IssueCodeDescription);
				Assert.AreEqual("Bart", items[2].IssueCodeDescription);
				Assert.AreEqual("Bart", items[3].IssueCodeDescription);
			}
			finally
			{
				TestUtils.DeleteAllRows(_emptyIssuesGdb, _issuePolygons);
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
