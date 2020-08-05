using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Hosting;
using NUnit.Framework;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.DomainModel.DataModel;

namespace ProSuite.AGP.WorkList.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class WorkListTest
	{
		[SetUp]
		public void SetUp()
		{
			// http://stackoverflow.com/questions/8245926/the-current-synchronizationcontext-may-not-be-used-as-a-taskscheduler
			SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
		}

		[OneTimeSetUp]
		public void SetupFixture()
		{
			// Host must be initialized on an STA thread:
			Host.Initialize();
		}

		private string _emptyIssuesGdb = @"C:\git\ProSuite\src\ProSuite.AGP.WorkList.Test\TestData\issues_empty.gdb";
		private string _featureClassName = "IssuePolygons";

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
				var workspaceReference = new GdbWorkspaceIdentity(geodatabase);
				IWorkItemRepository repository = new ErrorItemRepository(new List<IWorkspaceContext>
				                                         {
					                                         new WorkspaceContext(workspaceReference)
				                                         });

				repository.RegisterDatasets(new List<GdbTableIdentity>
				                            {
					                            new GdbTableIdentity(
						                            _featureClassName, 42, workspaceReference)
				                            });

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
				var workspaceReference = new GdbWorkspaceIdentity(geodatabase);
				IWorkItemRepository repository = new ErrorItemRepository(new List<IWorkspaceContext>
				                                         {
					                                         new WorkspaceContext(workspaceReference)
				                                         });

				repository.RegisterDatasets(new List<GdbTableIdentity>
				                            {
					                            new GdbTableIdentity(
						                            _featureClassName, 42, workspaceReference)
				                            });

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
				var workspaceReference = new GdbWorkspaceIdentity(geodatabase);
				IWorkItemRepository repository = new ErrorItemRepository(new List<IWorkspaceContext>
				                                         {
					                                         new WorkspaceContext(workspaceReference)
				                                         });

				repository.RegisterDatasets(new List<GdbTableIdentity>
				                            {
					                            new GdbTableIdentity(
						                            _featureClassName, 42, workspaceReference)
				                            });

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
				var workspaceReference = new GdbWorkspaceIdentity(geodatabase);
				IWorkItemRepository repository = new ErrorItemRepository(new List<IWorkspaceContext>
				                                         {
					                                         new WorkspaceContext(workspaceReference)
				                                         });

				repository.RegisterDatasets(new List<GdbTableIdentity>
				                            {
					                            new GdbTableIdentity(
						                            _featureClassName, 42, workspaceReference)
				                            });

				IWorkList workList = new GdbQueryWorkList(repository, "work list");

				var items = workList.GetItems().Cast<ErrorItem>().ToList();

				Assert.AreEqual("Bart", items[0].Description);
				Assert.AreEqual("Bart", items[1].Description);
				Assert.AreEqual("Bart", items[2].Description);
				Assert.AreEqual("Bart", items[3].Description);
			}
			finally
			{
				TestUtils.DeleteAllRows(_emptyIssuesGdb, _featureClassName);
			}
		}
	}
}
