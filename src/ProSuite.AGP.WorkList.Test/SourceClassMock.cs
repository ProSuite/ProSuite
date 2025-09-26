using System;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Test;

public class SourceClassMock : ISourceClass
{
	public string Name { get; }
	public GdbTableIdentity TableIdentity { get; } = WorkListTestUtils.CreateTableProxy();
	public IAttributeReader AttributeReader { get; set; }
	public bool HasGeometry { get; }
	public string DefinitionQuery { get; }
	public string OIDField { get; set; }
	public string ShapeField { get; set; }
	public string StatusField { get; set; }

	public string GetRelevantSubFields(bool excludeGeometry)
	{
		throw new NotImplementedException();
	}

	public bool Uses(ITableReference tableReference)
	{
		throw new NotImplementedException();
	}

	public T OpenDataset<T>() where T : Table
	{
		throw new NotImplementedException();
	}

	public void AdaptFilter(QueryFilter filter, WorkItemStatus? statusFilter)
	{
		throw new NotImplementedException();
	}

	public string CreateWhereClause(WorkItemStatus? statusFilter)
	{
		throw new NotImplementedException();
	}

	public long GetUniqueTableId()
	{
		return TableIdentity.Id;
	}

	public void EnsureValidFilter(ref QueryFilter filter, WorkItemStatus? statusFilter,
	                              bool excludeGeometry)
	{
		throw new NotImplementedException();
	}
}
