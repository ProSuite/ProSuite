using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Core.Test;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.AGP.WorkList.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class WorkListTest
	{
		private Polygon _poly0;
		private Polygon _poly1;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			// Host must be initialized on an STA thread:
			//Host.Initialize();
			CoreHostProxy.Initialize();

			_poly0 = PolygonConstruction
			         .StartPolygon(0, 0, 0)
			         .LineTo(0, 40, 0)
			         .LineTo(40, 40, 0)
			         .LineTo(40, 0, 0)
			         .ClosePolygon();

			_poly1 = PolygonConstruction
			         .StartPolygon(0, 0, 0)
			         .LineTo(0, 80, 0)
			         .LineTo(80, 80, 0)
			         .LineTo(80, 0, 0)
			         .ClosePolygon();
		}

		#region item chunks tests

		//[Test]
		//public void Can_get_items_by_task_id_only_once()
		//{
		//	var capacity = 8000;
		//	var items = new List<IWorkItem>(capacity);

		//	for (int i = 0; i < capacity; i++)
		//	{
		//		items.Add(new WorkItemMock(i));
		//	}

		//	var workList = new SelectionWorkList(new ItemRepositoryMock(items), "foo", "nice foo");

		//	List<IWorkItem> list;
		//	Assert.True(workList.TryGetItems(42, out list));
		//	Assert.NotNull(list);
		//	Assert.IsNotEmpty(list);

		//	Assert.False(workList.TryGetItems(42, out list));
		//	Assert.Null(list);
		//	Assert.IsNull(list);
		//}

		//[Test]
		//public void Only_three_tasks_can_get_items()
		//{
		//	var capacity = 4200;
		//	var items = new List<IWorkItem>(capacity);

		//	for (int i = 0; i < capacity; i++)
		//	{
		//		items.Add(new WorkItemMock(i));
		//	}

		//	var workList = new SelectionWorkList(new ItemRepositoryMock(items), "foo", "nice foo");

		//	Assert.True(workList.TryGetItems(42, out List<IWorkItem> firstList));
		//	Assert.NotNull(firstList);
		//	Assert.IsNotEmpty(firstList);
		//	Assert.AreEqual(1400, firstList.Count);

		//	Assert.False(workList.TryGetItems(42, out List<IWorkItem> secondList));
		//	Assert.Null(secondList);
		//	Assert.IsNull(secondList);

		//	Assert.True(workList.TryGetItems(1, out List<IWorkItem> thirdList));
		//	Assert.NotNull(thirdList);
		//	Assert.IsNotEmpty(thirdList);
		//	Assert.AreEqual(1400, thirdList.Count);

		//	Assert.True(workList.TryGetItems(2, out List<IWorkItem> fourthList));
		//	Assert.NotNull(fourthList);
		//	Assert.IsNotEmpty(fourthList);
		//	Assert.AreEqual(1400, fourthList.Count);

		//	Assert.False(workList.TryGetItems(3, out List<IWorkItem> fifthList));
		//	Assert.Null(fifthList);

		//	Assert.False(workList.TryGetItems(4, out List<IWorkItem> sixthList));
		//	Assert.Null(sixthList);
		//}

		//[Test]
		//public void Can_get_evenly_distributetd_item_chunks()
		//{
		//	var capacity = 35;
		//	var items = new List<IWorkItem>(capacity);

		//	for (int i = 0; i < capacity; i++)
		//	{
		//		items.Add(new WorkItemMock(i));
		//	}

		//	var workList = new SelectionWorkList(new ItemRepositoryMock(items), "foo", "nice foo");

		//	Assert.True(workList.TryGetItems(1, out List<IWorkItem> firstList));
		//	Assert.NotNull(firstList);
		//	Assert.IsNotEmpty(firstList);
		//	Assert.AreEqual(13, firstList.Count);

		//	Assert.True(workList.TryGetItems(2, out List<IWorkItem> secondList));
		//	Assert.NotNull(secondList);
		//	Assert.IsNotEmpty(secondList);
		//	Assert.AreEqual(11, secondList.Count);

		//	Assert.True(workList.TryGetItems(3, out List<IWorkItem> thirdList));
		//	Assert.NotNull(thirdList);
		//	Assert.IsNotEmpty(thirdList);
		//	Assert.AreEqual(11, thirdList.Count);
		//}

		//[Test]
		//public void Can_process_item_chunks_for_very_few_items()
		//{
		//	var capacity = 1;
		//	var items = new List<IWorkItem>(capacity);

		//	for (int i = 0; i < capacity; i++)
		//	{
		//		items.Add(new WorkItemMock(i));
		//	}

		//	var workList = new SelectionWorkList(new ItemRepositoryMock(items), "foo", "nice foo");

		//	Assert.True(workList.TryGetItems(1, out List<IWorkItem> firstList));
		//	Assert.NotNull(firstList);
		//	Assert.IsNotEmpty(firstList);
		//	Assert.AreEqual(1, firstList.Count);

		//	Assert.True(workList.TryGetItems(2, out List<IWorkItem> secondList));
		//	Assert.NotNull(secondList);
		//	Assert.IsEmpty(secondList);

		//	Assert.True(workList.TryGetItems(3, out List<IWorkItem> thirdList));
		//	Assert.NotNull(thirdList);
		//	Assert.IsEmpty(thirdList);
		//}

		//[Test]
		//public void Can_process_item_chunks_for_three_items()
		//{
		//	var capacity = 3;
		//	var items = new List<IWorkItem>(capacity);

		//	for (int i = 0; i < capacity; i++)
		//	{
		//		items.Add(new WorkItemMock(i));
		//	}

		//	var workList = new SelectionWorkList(new ItemRepositoryMock(items), "uniqueName", "displayName");

		//	Assert.True(workList.TryGetItems(1, out List<IWorkItem> firstList));
		//	Assert.NotNull(firstList);
		//	Assert.IsNotEmpty(firstList);
		//	Assert.AreEqual(1, firstList.Count);

		//	Assert.True(workList.TryGetItems(2, out List<IWorkItem> secondList));
		//	Assert.NotNull(secondList);
		//	Assert.IsNotEmpty(secondList);
		//	Assert.AreEqual(1, secondList.Count);

		//	Assert.True(workList.TryGetItems(3, out List<IWorkItem> thirdList));
		//	Assert.NotNull(thirdList);
		//	Assert.IsNotEmpty(thirdList);
		//	Assert.AreEqual(1, thirdList.Count);
		//}

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
			var repo = new ItemRepositoryMock(new List<IWorkItem> {item1, item2, item3, item4});
			IWorkList wl = new SelectionWorkList(repo, "uniqueName", "displayName");

			// important to get items from DB because the items are loaded lazyly
			//IEnumerable<IWorkItem> _ = wl.GetItems(GdbQueryUtils.CreateFilter(new List<long>(2){2,3}));
			IEnumerable<IWorkItem> _ = wl.GetItems().ToList();

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
		public void Can_go_next_visited()
		{
			// The items have to be visible to enable the work list to go to the next item.
			// Normally GoNearest() sets Item.Visible = true.
			// Item3 was never visited. GoNext should never hit item3.
			IWorkItem item1 = new WorkItemMock(1) { Visited = true };
			IWorkItem item2 = new WorkItemMock(2) { Visited = true };
			IWorkItem item3 = new WorkItemMock(3);
			IWorkItem item4 = new WorkItemMock(4) { Visited = true };
			var repo = new ItemRepositoryMock(new List<IWorkItem> { item1, item2, item3, item4 });
			IWorkList wl = new SelectionWorkList(repo, "uniqueName", "displayName");

			// important to get items from DB because the items are loaded lazyly
			//IEnumerable<IWorkItem> _ = wl.GetItems(GdbQueryUtils.CreateFilter(new List<long>(2){2,3}));
			IEnumerable<IWorkItem> _ = wl.GetItems().ToList();

			Assert.True(wl.CanGoNext());
			Assert.False(wl.CanGoPrevious());
			Assert.False(wl.CanGoFirst());
			wl.GoNext();
			Assert.AreEqual(item2, wl.Current);
			Assert.True(wl.Current?.Visited);

			Assert.True(wl.CanGoNext());
			Assert.True(wl.CanGoPrevious());
			Assert.True(wl.CanGoFirst());
			wl.GoNext();
			Assert.AreEqual(item4, wl.Current);
			Assert.True(wl.Current?.Visited);

			// end of visited items reached
			Assert.False(wl.CanGoNext());
			Assert.True(wl.CanGoPrevious());
			Assert.True(wl.CanGoFirst());
		}

		[Test]
		public void Can_go_first_again()
		{
			// The items have to be visible to enable the work list to go to the next item.
			// Normally GoNearest() sets Item.Visible = true.
			IWorkItem item1 = new WorkItemMock(1) { Visited = true };
			IWorkItem item2 = new WorkItemMock(2) { Visited = true };
			IWorkItem item3 = new WorkItemMock(3) { Visited = true };
			IWorkItem item4 = new WorkItemMock(4) { Visited = true };
			var repo = new ItemRepositoryMock(new List<IWorkItem> { item1, item2, item3, item4 });
			IWorkList wl = new SelectionWorkList(repo, "uniqueName", "displayName");

			IEnumerable<IWorkItem> _ = wl.GetItems().ToList();

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
			// The items have to be visible to enable the work list to go to the next item.
			// Normally GoNearest() sets Item.Visible = true.
			IWorkItem item1 = new WorkItemMock(1) { Visited = true };
			IWorkItem item2 = new WorkItemMock(2) { Visited = true };
			IWorkItem item3 = new WorkItemMock(3) { Visited = true };
			IWorkItem item4 = new WorkItemMock(4) { Visited = true };
			var repo = new ItemRepositoryMock(new List<IWorkItem> { item1, item2, item3, item4 });
			IWorkList wl = new SelectionWorkList(repo, "uniqueName", "displayName");

			IEnumerable<IWorkItem> _ = wl.GetItems().ToList();
			
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

			var repo = new ItemRepositoryMock([item7, item10, item15]);
			IWorkList wl = new SelectionWorkList(repo, "uniqueName", "displayName");

			// important to get items from DB because the items are loaded lazyly
			//IEnumerable<IWorkItem> _ = wl.GetItems(GdbQueryUtils.CreateFilter(new List<long>(2){2,3}));
			IEnumerable<IWorkItem> _ = wl.GetItems().ToList();

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
			Assert.AreEqual(item7, wl.Current);

			// Now we can go nearest again which is item10 (nearest to item7)
			Assert.True(wl.CanGoNearest());
			Assert.NotNull(wl.Current);
			wl.GoNearest(wl.Current.Extent);
			Assert.AreEqual(item10, wl.Current);
			Assert.True(wl.Current?.Visited);
		}

		[Test]
		public void Cannot_go_first_again_if_first_item_is_set_done()
		{
			// The items have to be visible to enable the work list to go to the next item.
			// Normally GoNearest() sets Item.Visible = true.
			IWorkItem item1 = new WorkItemMock(1) { Visited = true };
			IWorkItem item2 = new WorkItemMock(2) { Visited = true };
			IWorkItem item3 = new WorkItemMock(3) { Visited = true };
			IWorkItem item4 = new WorkItemMock(4) { Visited = true };
			var repo = new ItemRepositoryMock(new List<IWorkItem> { item1, item2, item3, item4 });
			IWorkList wl = new SelectionWorkList(repo, "uniqueName", "displayName");

			IEnumerable<IWorkItem> _ = wl.GetItems().ToList();

			wl.GoNext();
			Assert.AreEqual(item2, wl.Current);
			Assert.True(wl.Current?.Visited);

			wl.GoFirst();
			Assert.AreEqual(item1, wl.Current);
			Assert.True(wl.Current?.Visited);

			// set status done and update work list
			wl.Current.Status = WorkItemStatus.Done;
			wl.SetStatusAsync(wl.Current, WorkItemStatus.Done);

			wl.GoNext();
			Assert.AreEqual(item2, wl.Current);
			Assert.True(wl.Current?.Visited);

			// second item is now the first in work list
			// because first item is set to done and therefor 'not visible'
			Assert.False(wl.CanGoFirst());
		}

		[Test]
		public void Can_go_next_and_add_items_incremently()
		{
			IWorkItem item1 = new WorkItemMock(1) { Visited = true };
			IWorkItem item2 = new WorkItemMock(2) { Visited = true };
			IWorkItem item3 = new WorkItemMock(3) { Visited = true };
			IWorkItem item4 = new WorkItemMock(4) { Visited = true };
			IWorkItem item5 = new WorkItemMock(5) { Visited = true };
			IWorkItem item6 = new WorkItemMock(6) { Visited = true };
			var repo = new ItemRepositoryMock(new List<IWorkItem> { item1, item2, item3, item4, item5, item6 });
			var wl = new SelectionWorkList(repo, "uniqueName", "displayName");

			IEnumerable<IWorkItem> _ = wl.GetItems(GdbQueryUtils.CreateFilter([2, 3, 4])).ToList();

			Assert.AreEqual(item2, wl.Current);
			Assert.True(wl.Current?.Visited);

			Assert.True(wl.CanGoNext());
			Assert.False(wl.CanGoPrevious());
			Assert.False(wl.CanGoFirst());
			wl.GoNext();
			Assert.AreEqual(item3, wl.Current);
			Assert.True(wl.Current?.Visited);

			Assert.True(wl.CanGoNext());
			Assert.True(wl.CanGoPrevious());
			Assert.True(wl.CanGoFirst());
			wl.GoNext();
			Assert.AreEqual(item4, wl.Current);
			Assert.True(wl.Current?.Visited);

			// end of visited items reached
			Assert.False(wl.CanGoNext());
			Assert.True(wl.CanGoPrevious());
			Assert.True(wl.CanGoFirst());

			// get remaining items
			IEnumerable<IWorkItem> __ = wl.GetItems(GdbQueryUtils.CreateFilter([1, 5, 6])).ToList();

			Assert.True(wl.CanGoNext());
			Assert.True(wl.CanGoPrevious());
			Assert.True(wl.CanGoFirst());
			wl.GoNext();
			Assert.AreEqual(item1, wl.Current);
			Assert.True(wl.Current?.Visited);
		}

		#endregion

		[Test]
		public void Can_handle_WorkList_extent_on_insert()
		{
			GdbRowIdentity rowId = WorkListTestUtils.CreateRowProxy(1);
			GdbTableIdentity tableId = WorkListTestUtils.CreateTableProxy();
			IWorkItem item1 = new WorkItemMock(rowId, tableId, _poly0) { Visited = true };
			var repo = new ItemRepositoryMock(new List<IWorkItem> { item1 });
			IWorkList wl = new SelectionWorkList(repo, "uniqueName", "displayName");

			// important to get items from DB because the items are loaded lazyly
			Assert.AreEqual(1, wl.GetItems().ToList().Count);

			AssertEqual(_poly0.Extent, wl.Extent);

			GdbRowIdentity rowId2 = WorkListTestUtils.CreateRowProxy(2);
			IWorkItem item2 = new WorkItemMock(rowId2, tableId, _poly1) { Visited = true };
			repo.Add(item2);

			var inserts = new Dictionary<GdbTableIdentity, List<long>> { { tableId, [2] } };
			var deletes = new Dictionary<GdbTableIdentity, List<long>>();
			var updates = new Dictionary<GdbTableIdentity, List<long>>();

			wl.ProcessChanges(inserts, deletes, updates);

			Assert.AreEqual(2, wl.GetItems().ToList().Count);

			// assert oid is still the same
			Assert.AreEqual(1, item1.OID);
			Assert.AreEqual(2, item2.OID);
			AssertEqual(_poly1.Extent, wl.Extent);

			Envelope envelope = _poly0.Extent.Union(_poly1.Extent);
			Assert.True(GeometryUtils.Intersects(wl.Extent, envelope));
			Assert.True(GeometryUtils.Contains(wl.Extent, envelope));
		}

		[Test]
		public void Can_handle_WorkList_extent_on_update()
		{
			GdbRowIdentity rowId = WorkListTestUtils.CreateRowProxy(1);
			GdbTableIdentity tableId = WorkListTestUtils.CreateTableProxy();
			IWorkItem item1 = new WorkItemMock(rowId, tableId, _poly0) { Visited = true };
			var repo = new ItemRepositoryMock(new List<IWorkItem> { item1 });
			IWorkList wl = new SelectionWorkList(repo, "uniqueName", "displayName");

			// important to get items from DB because the items are loaded lazyly
			Assert.AreEqual(1, wl.GetItems().ToList().Count);

			AssertEqual(_poly0.Extent, wl.Extent);

			// Update Extext
			item1.SetExtent(_poly1.Extent);

			var inserts = new Dictionary<GdbTableIdentity, List<long>>();
			var deletes = new Dictionary<GdbTableIdentity, List<long>>();
			var updates = new Dictionary<GdbTableIdentity, List<long>>{ { tableId, [item1.OID] } };

			wl.ProcessChanges(inserts, deletes, updates);

			// get items again because item1 was invalidated
			Assert.AreEqual(1, wl.GetItems().ToList().Count);

			// assert oid is still the same
			Assert.AreEqual(1, item1.OID);
			AssertEqual(_poly1.Extent, wl.Extent);
		}

		[Test]
		public void Can_handle_WorkList_extent_on_delete()
		{
			GdbRowIdentity rowId1 = WorkListTestUtils.CreateRowProxy(1);
			GdbRowIdentity rowId2 = WorkListTestUtils.CreateRowProxy(2);
			GdbTableIdentity tableId = WorkListTestUtils.CreateTableProxy();

			IWorkItem item1 = new WorkItemMock(rowId1, tableId, _poly0) { Visited = true };
			IWorkItem item2 = new WorkItemMock(rowId2, tableId, _poly1) { Visited = true };
			var repo = new ItemRepositoryMock(new List<IWorkItem> { item1, item2 });
			IWorkList wl = new SelectionWorkList(repo, "uniqueName", "displayName");

			// important to get items from DB because the items are loaded lazyly
			Assert.AreEqual(2, wl.GetItems().ToList().Count);

			Envelope envelope = _poly0.Extent.Union(_poly1.Extent);
			Assert.True(GeometryUtils.Intersects(wl.Extent, envelope));
			Assert.True(GeometryUtils.Contains(wl.Extent, envelope));

			var inserts = new Dictionary<GdbTableIdentity, List<long>>();
			var deletes = new Dictionary<GdbTableIdentity, List<long>> { { tableId, [2] } };
			var updates = new Dictionary<GdbTableIdentity, List<long>>();

			wl.ProcessChanges(inserts, deletes, updates);

			// remove it from repo mock too
			Assert.True(repo.Remove(item2));
			Assert.AreEqual(1, wl.GetItems().ToList().Count);

			AssertEqual(_poly0.Extent, wl.Extent);
		}

		[Test]
		public void LearningTest()
		{
			GdbTableIdentity tableId = WorkListTestUtils.CreateTableProxy();
			GdbRowIdentity rowId1 = WorkListTestUtils.CreateRowProxy(1);
			IWorkItem item1 = new WorkItemMock(rowId1, tableId, _poly0) { Visited = true };
			var repo = new ItemRepositoryMock(new List<IWorkItem> { item1 });
			IWorkList wl = new SelectionWorkList(repo, "uniqueName", "displayName");

			IEnumerable<IWorkItem> _ = wl.GetItems().ToList();
			Assert.AreEqual(1, wl.GetItems().ToList().Count);

			// Note: work item has a minimum length/width of 30!!
			AssertEqual(_poly0.Extent, wl.Extent);
		}

		private static void AssertEqual(Envelope expected, Envelope actual)
		{
			Assert.True(AreEqual(expected, actual));
		}

		private static bool AreEqual(Envelope expected, Envelope actual)
		{
			// 1.1 is default expansion of work items
			Envelope envelope = expected.Expand(1.1, 1.1, true);
			return envelope.IsEqual(actual);
		}
	}
}
