using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class WorkItemRepositoryTest
	{
		private Polygon _poly0;
		private Polygon _poly1;
		private Geodatabase _geodatabase;
		private Table _table0;
		private Table _table1;
		private IssueItemRepository _repository;

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

			IRepository stateRepository = new XmlWorkItemStateRepository(@"C:\temp\states.xml");
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

		[Test]
		public void Foo()
		{
			InsertFeature(_featureClass0, _poly0);
			InsertFeature(_featureClass0, _poly0);

			try
			{
				IWorkList workList = new IssueWorkList(_repository, "work list");

				List<IWorkItem> items = workList.GetItems().ToList();
				Assert.AreEqual(2, items.Count);

				IWorkItem item = items[0];
				item.Visited = true;
				item.Status = WorkItemStatus.Done;

				workList.Update(item);

				item = workList.GetItems(null, true).ToList()[0];
				
				// WorkItemStatus.Done
				Assert.AreEqual(WorkItemStatus.Done, item.Status);
			}
			finally
			{
				DeleteAllRows(_featureClass0);
			}
		}

		private object GetStatusFromDatabase(string featureClass, string statusField)
		{
			object status;

			var uri = new Uri(_emptyIssuesGdb, UriKind.Absolute);
			using (var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(uri)))
			{
				using (var fc = geodatabase.OpenDataset<FeatureClass>(featureClass))
				{
					Row row = GdbQueryUtils.GetRow(fc, 1);
					status = row[statusField];
				}
			}

			return status;
		}

		#region same as in WorkListTest

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

		#endregion
	}
}
