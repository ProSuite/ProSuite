using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList;

public class SelectionSourceClass : SourceClass
{
	public List<long> Oids { get; }

	public SelectionSourceClass(GdbTableIdentity tableIdentity,
	                            SourceClassSchema schema,
	                            List<long> oids)
		: base(tableIdentity, schema)
	{
		Oids = oids;
	}

	public override long GetUniqueTableId()
	{
		// NOTE: We want to support
		// - un-registered tables, such as shape files
		// - tables from different geodatabases

		return WorkListUtils.GetUniqueTableIdAcrossWorkspaces(TableIdentity);
	}

	protected override void EnsureValidFilterCore(ref QueryFilter filter,
	                                              WorkItemStatus? statusFilter)
	{
		filter.ObjectIDs = Oids;

		if (filter is SpatialQueryFilter spatialFilter)
		{
			// Probably depends on the count of OIDs vs. the spatial filter's selectivity:
			spatialFilter.SearchOrder = SearchOrder.Attribute;
		}
	}
}
