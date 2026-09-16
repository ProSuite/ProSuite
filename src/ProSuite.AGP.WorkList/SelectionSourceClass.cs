using System.Collections.Generic;
using System.Linq;
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
		: base(tableIdentity)
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
	                                              bool ignoreDefinitionQuery)
	{
		if (filter.ObjectIDs.Count == 0)
		{
			// the filter is not constraint with object IDs:
			// just use the selection OIDs
			filter.ObjectIDs = Oids;
		}
		else
		{
			// the filter is already constraint with object IDs:
			// use the intersection of OIds
			// or, if there is no intersection, make sure the filter returns nothing by setting the ObjectIDs to -1
			var remainingObjectIDs = filter.ObjectIDs.Intersect(Oids).ToList();
			filter.ObjectIDs = remainingObjectIDs.Count > 0
				                   ? remainingObjectIDs
				                   : new List<long> { -1 };
		}

		if (filter is SpatialQueryFilter spatialFilter)
		{
			// Probably depends on the count of OIDs vs. the spatial filter's selectivity:
			spatialFilter.SearchOrder = SearchOrder.Attribute;
		}
	}
}
