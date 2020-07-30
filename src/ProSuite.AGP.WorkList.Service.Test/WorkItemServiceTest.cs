using System;
using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Hosting;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.DomainModel.DataModel;

namespace ProSuite.AGP.WorkList.Service.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class WorkItemServiceTest
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

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			// Nothing to do. There's no Host.Shutdown or similar
		}

		[Test]
		public void WorkItemService_LearningTest()
		{
			var path = @"C:\git\ProSuite\src\ProSuite.AGP.WorkList.Service.Test\TestData\errors.gdb";

			// todo daro: determine connection type
			var uri = new Uri(path, UriKind.Absolute);

			var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(uri));
			IWorkItemRepository repository =
				new ErrorItemRepository(new WorkspaceContext(geodatabase));

			repository.Register(new VectorDatasetMock {Name = "TLM_ERRORS_POLYGON"});

			Domain.WorkList workList = new Domain.ErrorWorkList(repository, "work list");

			IEnumerable<IWorkItem> rowValues = workList.GetItems();


		}
	}
}
