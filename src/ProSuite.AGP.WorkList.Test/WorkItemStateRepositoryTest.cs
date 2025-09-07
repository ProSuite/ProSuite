using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using NUnit.Framework;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.Commons.Testing;

namespace ProSuite.AGP.WorkList.Test;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class WorkItemStateRepositoryTest
{
	[OneTimeSetUp]
	public void SetupFixture()
	{
		// Host must be initialized on an STA thread:
		CoreHostProxy.Initialize();
	}

	[Test]
	public void Can_refresh_workitems_with_persisted_states()
	{
		string path = TestDataPreparer.FromDirectory().GetPath($"{nameof(Can_refresh_workitems_with_persisted_states)}.xml");
		var stateRepo = new XmlSelectionItemStateRepository(path, "stateRepo", typeof(IssueWorkList));

		IWorkItem item1 = new WorkItemMock(1);
		IWorkItem item2 = new WorkItemMock(2);
		IWorkItem item3 = new WorkItemMock(3);
		IWorkItem item4 = new WorkItemMock(4);

		var repo =
			new ItemRepositoryMock(new List<IWorkItem> { item1, item2, item3, item4 }, stateRepo);
		IWorkList wl = new IssueWorkList(repo, WorkListTestUtils.GetAOI(), "uniqueName", "displayName");

		List<IWorkItem> items = wl.Search(new SpatialQueryFilter()).ToList();

		IWorkItem first = items.First();
		Assert.True(first.Visited);

		Assert.AreEqual(WorkItemStatus.Done, first.Status);
	}

	[Test]
	public void Can_persist_workitem_states()
	{
		string path = TestDataPreparer.FromDirectory().GetPath($"{nameof(Can_persist_workitem_states)}.xml");

		try
		{
			var stateRepo = new XmlSelectionItemStateRepository(path, "stateRepo", typeof(IssueWorkList));

			IWorkItem item1 = new WorkItemMock(1);
			IWorkItem item2 = new WorkItemMock(2);
			IWorkItem item3 = new WorkItemMock(3);
			IWorkItem item4 = new WorkItemMock(4);

			var repo =
				new ItemRepositoryMock(new List<IWorkItem> { item1, item2, item3, item4 }, stateRepo);
			IWorkList wl = new IssueWorkList(repo, WorkListTestUtils.GetAOI(), "uniqueName", "displayName");

			List<IWorkItem> items = wl.Search(null).ToList();

			items.ForEach(item => Assert.False(item.Visited));

			IWorkItem first = items.First();

			repo.SetVisited(first);
			repo.SetStatusAsync(first, WorkItemStatus.Done);

			wl.Commit();

			wl.Visibility = WorkItemVisibility.All; // get all items not only Todo
			items = wl.Search(null).ToList();
			first = items.First();

			Assert.True(first.Visited);
			Assert.AreEqual(WorkItemStatus.Done, first.Status);

			Assert.True(File.Exists(path));
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}
}
