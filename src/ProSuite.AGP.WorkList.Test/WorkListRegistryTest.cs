using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;

namespace ProSuite.AGP.WorkList.Test;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class WorkListRegistryTest
{
	[Test]
	public void Can_register_worklists()
	{
		var factory = WorkListEnvironmentFactory.Instance;

		factory.RegisterEnvironment<SelectionWorkList>()
		       .WithPath(_ => new SelectionWorkListEnvironmentMock());
		factory.RegisterEnvironment<IssueWorkList>()
		       .WithItemStore(itemStore => new IssueWorkListEnvironmentMock(itemStore));

		string path = @"C:\data\swisstopo\gotop\gotop-t\Worklists\gotop.iwl";
		string name = WorkListUtils.GetWorklistName(path, out string typeName);

		IWorkListRegistry registry = WorkListRegistry.Instance;
		Assert.True(registry.TryAdd(new LayerBasedWorkListFactory(name, typeName, path)));

		Assert.Null(factory.CreateWorkEnvironment(path, typeName));

		factory.AddStore<IssueWorkList>(new WorkListItemDatastoreMock());

		Assert.NotNull(factory.CreateWorkEnvironment(path, typeName));
	}

	[Test]
	public void Can_register_worklists3()
	{
		var factory = WorkListEnvironmentFactory.Instance;

		factory.RegisterEnvironment<IssueWorkList>()
		       .WithPath(path => new IssueWorkListEnvironmentMock(path));

		string path = @"C:\data\swisstopo\gotop\gotop-t\Worklists\gotop.iwl";
		string name = WorkListUtils.GetWorklistName(path, out string typeName);

		var workListFactory = new WorkListFactoryMock(name, typeName, path);
		IWorkList workList = workListFactory.GetAsync().Result;
		Assert.NotNull(workList);
	}

	[Test]
	public void Can_register_worklists2()
	{
		var factory = WorkListEnvironmentFactory.Instance;

		factory.RegisterEnvironment<IssueWorkList>()
		       .WithPath(path => new IssueWorkListEnvironmentMock(path));

		string path = @"C:\data\swisstopo\gotop\issues.gdb";
		string typeName = "ProSuite.AGP.WorkList.Domain.IssueWorkList";

		Assert.NotNull(factory.CreateWorkEnvironment(path, typeName));
	}
}

public class IssueWorkListEnvironmentMock : IWorkEnvironment
{
	public IssueWorkListEnvironmentMock(string path) { }

	public IssueWorkListEnvironmentMock(IWorkListItemDatastore itemDatastore) { }

	public Task<IWorkList> CreateWorkListAsync(string uniqueName, string path)
	{
		var worklist = new WorkListMock();
		return Task.FromResult<IWorkList>(worklist);
	}

	public void LoadWorkListLayer(IWorkList worklist, string workListDefinitionFilePath)
	{
		throw new NotImplementedException();
	}

	public void LoadAssociatedLayers(IWorkList worklist)
	{
		throw new NotImplementedException();
	}

	public string GetDisplayName()
	{
		throw new NotImplementedException();
	}

	public bool WorkListFileExistsInProjectFolder(out string worklistFilePath)
	{
		throw new NotImplementedException();
	}

	public bool AllowBackgroundLoading { get; set; }

	public string WorkListFile { get; set; }

	public Task<IWorkList> CreateWorkListAsync(string uniqueName)
	{
		throw new NotImplementedException();
	}
}

public class SelectionWorkListEnvironmentMock : IWorkEnvironment
{
	public Task<IWorkList> CreateWorkListAsync(string uniqueName, string path)
	{
		return Task.FromResult(default(IWorkList));
	}

	public void LoadWorkListLayer(IWorkList worklist, string workListDefinitionFilePath)
	{
		throw new NotImplementedException();
	}

	public void LoadAssociatedLayers(IWorkList worklist)
	{
		throw new NotImplementedException();
	}

	public string GetDisplayName()
	{
		throw new NotImplementedException();
	}

	public bool WorkListFileExistsInProjectFolder(out string worklistFilePath)
	{
		throw new NotImplementedException();
	}

	public bool AllowBackgroundLoading { get; set; }

	public Task<IWorkList> CreateWorkListAsync(string uniqueName)
	{
		throw new NotImplementedException();
	}
}

public class WorkListFactoryMock : IWorkListFactory
{
	private readonly string _typeName;
	private readonly string _path;

	public string Name { get; }

	public WorkListFactoryMock(string tableName, string typeName, string path)
	{
		_typeName = typeName;
		_path = path;
		Name = tableName;
	}

	public IWorkList Get()
	{
		throw new NotImplementedException();
	}

	public Task<IWorkList> GetAsync()
	{
		IWorkEnvironment workEnvironment =
			WorkListEnvironmentFactory.Instance.CreateWorkEnvironment(_path, _typeName);

		Assert.NotNull(workEnvironment);

		IWorkList worklist = workEnvironment.CreateWorkListAsync(Name, _path).Result;

		return Task.FromResult(worklist);
	}
}

public class WorkListMock : IWorkList
{
	public void Invalidate()
	{
		throw new NotImplementedException();
	}

	public void Invalidate(IEnumerable<Table> tables)
	{
		throw new NotImplementedException();
	}

	public void ProcessChanges(Dictionary<Table, List<long>> inserts,
	                           Dictionary<Table, List<long>> deletes,
	                           Dictionary<Table, List<long>> updates)
	{
		throw new NotImplementedException();
	}

	public bool CanContain(Table table)
	{
		throw new NotImplementedException();
	}

	public string Name { get; set; }
	public string DisplayName { get; }

	public bool NavigateInAllMapViews { get; set; }
	public double MinimumScaleDenominator { get; set; }
	public bool CacheBufferedItemGeometries { get; set; }
	public bool AlwaysUseDraftMode { get; set; }
	public double ItemDisplayBufferDistance { get; set; }
	public int? MaxBufferedItemCount { get; set; }
	public int? MaxBufferedShapePointCount { get; set; }

	public Envelope Extent => throw new NotImplementedException();

	public WorkItemVisibility? Visibility { get; set; }
	public Geometry AreaOfInterest { get; set; }
	public bool QueryLanguageSupported { get; }
	public IWorkItem CurrentItem { get; }
	public int CurrentIndex { get; set; }
	public IWorkItemRepository Repository { get; }
	public long? TotalCount { get; set; }
	public event EventHandler<WorkListChangedEventArgs> WorkListChanged;

	public IEnumerable<IWorkItem> Search(QueryFilter filter)
	{
		throw new NotImplementedException();
	}

	public IEnumerable<IWorkItem> Search(SpatialQueryFilter filter)
	{
		throw new NotImplementedException();
	}

	public IEnumerable<IWorkItem> GetItems(QueryFilter filter, WorkItemStatus? itemStatus,
	                                       bool excludeGeometry = false)
	{
		throw new NotImplementedException();
	}

	public int CountLoadedItems()
	{
		throw new NotImplementedException();
	}

	public long CountLoadedItems(out int todo)
	{
		throw new NotImplementedException();
	}

	public bool CanGoFirst()
	{
		throw new NotImplementedException();
	}

	public void GoFirst()
	{
		throw new NotImplementedException();
	}

	public void GoTo(long oid)
	{
		throw new NotImplementedException();
	}

	public bool CanGoNearest()
	{
		throw new NotImplementedException();
	}

	public void GoNearest(Geometry reference, Predicate<IWorkItem> match = null,
	                      params Polygon[] contextPerimeters)
	{
		throw new NotImplementedException();
	}

	public bool CanGoNext()
	{
		throw new NotImplementedException();
	}

	public void GoNext()
	{
		throw new NotImplementedException();
	}

	public bool CanGoPrevious()
	{
		throw new NotImplementedException();
	}

	public void GoPrevious()
	{
		throw new NotImplementedException();
	}

	public bool CanSetStatus()
	{
		throw new NotImplementedException();
	}

	public void SetVisited(IList<IWorkItem> items, bool visited)
	{
		throw new NotImplementedException();
	}

	public void Commit()
	{
		throw new NotImplementedException();
	}

	public Task SetStatusAsync(IWorkItem item, WorkItemStatus status)
	{
		throw new NotImplementedException();
	}

	public bool IsValid(out string message)
	{
		throw new NotImplementedException();
	}

	public IAttributeReader GetAttributeReader(long forSourceClassId)
	{
		throw new NotImplementedException();
	}

	public void EnsureRowCacheSynchronized()
	{
		throw new NotImplementedException();
	}

	public void DeactivateRowCacheSynchronization()
	{
		throw new NotImplementedException();
	}

	public Geometry GetItemDisplayGeometry(IWorkItem item)
	{
		throw new NotImplementedException();
	}

	public void SetItemsGeometryDraftMode(bool enable)
	{
		throw new NotImplementedException();
	}

	public void Rename(string name)
	{
		throw new NotImplementedException();
	}

	public void Invalidate(Envelope geometry)
	{
		throw new NotImplementedException();
	}

	public void Invalidate(List<long> oids)
	{
		throw new NotImplementedException();
	}

	public void UpdateExistingItemGeometries(QueryFilter filter)
	{
		throw new NotImplementedException();
	}

	public void Count()
	{
		throw new NotImplementedException();
	}

	public Row GetCurrentItemSourceRow(bool readOnly = true)
	{
		throw new NotImplementedException();
	}

	public void LoadItems()
	{
		throw new NotImplementedException();
	}

	public void RefreshItems(QueryFilter filter)
	{
		throw new NotImplementedException();
	}

	public void LoadItems(QueryFilter filter, WorkItemStatus? statusFilter = null)
	{
		throw new NotImplementedException();
	}
}
