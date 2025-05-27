using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using NUnit.Framework;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.AGP.WorkList.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class WorkItemStateRepositoryTest
	{
		private Geodatabase _geodatabase;
		private Table _table0;

		private readonly string _issuesGdb =
			@"C:\git\ProSuite\src\ProSuite.AGP.WorkList.Test\TestData\issues.gdb";

		private readonly string _featureClass = "IssuePolygons";

		//private string _statesXml = @"C:\temp\states.xml";
		private string _statesXml =
			@"C:\git\ProSuite\src\ProSuite.AGP.WorkList.Test\TestData\a_selection_work_list.xml";

		private ItemRepositoryMock _repository;

		[SetUp]
		public void SetUp()
		{
			// http://stackoverflow.com/questions/8245926/the-current-synchronizationcontext-may-not-be-used-as-a-taskscheduler
			SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

			_geodatabase =
				new Geodatabase(
					new FileGeodatabaseConnectionPath(new Uri(_issuesGdb, UriKind.Absolute)));

			_table0 = _geodatabase.OpenDataset<Table>(_featureClass);

			var tablesByGeodatabase = new Dictionary<Datastore, List<Table>>
			                          {
				                          {_geodatabase, new List<Table> {_table0}}
			                          };

			IWorkItemStateRepository stateRepository =
				new XmlWorkItemStateRepository(@"C:\temp\states.xml", null, null);
			_repository = new ItemRepositoryMock(new List<Table> { _table0 }, stateRepository);
		}

		[OneTimeSetUp]
		public void SetupFixture()
		{
			// Host must be initialized on an STA thread:
			CoreHostProxy.Initialize();
		}

		[Test]
		public void Can_refresh_with_persisted_visited_state()
		{
			try
			{
				List<IWorkItem> items = _repository.GetItems().ToList();

				items.ForEach(item => Assert.False(item.Visited));

				IWorkItem first = items.First();
				Assert.False(first.Visited);

				first.Visited = true;
				first.Status = WorkItemStatus.Done;

				_repository.UpdateVolatileState(items);
				_repository.Commit();

				items = _repository.GetItems().ToList();
				first = items.First();

				Assert.True(first.Visited);
				Assert.AreEqual(WorkItemStatus.Done, first.Status);
			}
			finally
			{
				// reset visited state
				List<IWorkItem> items = _repository.GetItems().ToList();

				items.First().Visited = false;
				items.First().Status = WorkItemStatus.Todo;

				_repository.UpdateVolatileState(items);
				_repository.Commit();
			}
		}

		[Test]
		public void Can_discard_volatile_visited_state()
		{
			try
			{
				List<IWorkItem> items = _repository.GetItems().ToList();

				items.ForEach(item => Assert.False(item.Visited));

				IWorkItem first = items.First();
				Assert.False(first.Visited);

				first.Visited = true;
				first.Status = WorkItemStatus.Done;

				_repository.UpdateVolatileState(items);

				items = _repository.GetItems().ToList();
				first = items.First();
				Assert.True(first.Visited);
				Assert.AreEqual(WorkItemStatus.Done, first.Status);

				_repository.Discard();

				items = _repository.GetItems().ToList();
				first = items.First();
				Assert.False(first.Visited);
				Assert.AreEqual(WorkItemStatus.Todo, first.Status);
			}
			finally
			{
				// reset visited state
				//List<IWorkItem> items = _repository.GetItems().ToList();

				//items.First().Visited = false;
				//items.First().Status = WorkItemStatus.Todo;

				//_repository.UpdateVolatileState(items);
				//_repository.Commit();
			}
		}
	}
}
