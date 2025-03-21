using System;
using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.AGP.Core.Test;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.AGP.WorkList.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class WorkItemTest
	{
		private Polygon _poly0;
		private Polygon _poly1;
		private Geodatabase _geodatabase;
		private Table _table0;
		private Table _table1;
		private ItemRepositoryMock _repository;

		[SetUp]
		public void SetUp()
		{
			// http://stackoverflow.com/questions/8245926/the-current-synchronizationcontext-may-not-be-used-as-a-taskscheduler
			SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

			_geodatabase =
				new Geodatabase(
					new FileGeodatabaseConnectionPath(new Uri(_emptyIssuesGdb, UriKind.Absolute)));

			_table0 = _geodatabase.OpenDataset<Table>(_featureClass0);
			_table1 = _geodatabase.OpenDataset<Table>(_featureClass1);

			var tablesByGeodatabase = new Dictionary<Datastore, List<Table>>
			                          {
				                          {_geodatabase, new List<Table> {_table0, _table1}}
			                          };

			IWorkItemStateRepository stateRepository =
				new XmlWorkItemStateRepository(@"C:\temp\states.xml", null, null);
			_repository = new ItemRepositoryMock(new List<Table> { _table0, _table1 }, stateRepository);
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

		private string _featureClassName = "IssuePolygons";
		private readonly string _featureClass0 = "featureClass0";
		private readonly string _featureClass1 = "featureClass1";

		[Test]
		public void Can_get_description_from_work_item()
		{
			try
			{
				TestUtils.InsertRows(_emptyIssuesGdb, _featureClass0, _poly0, 1);
				Row row = TestUtils.GetRow(_emptyIssuesGdb, _featureClass0, 1);

				var item = new SelectionItem(42, 44, row);
				Assert.NotNull(item.Description);

				string description = item.GetDescription();

				Assert.NotNull(description);
			}
			finally
			{
				TestUtils.DeleteAllRows(_emptyIssuesGdb, _featureClass0);
			}
		}
	}
}
