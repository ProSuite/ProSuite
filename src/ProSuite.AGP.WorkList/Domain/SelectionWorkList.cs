using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.SpatialIndex;
using ProSuite.Commons.Logging;
using System.Collections.Generic;

namespace ProSuite.AGP.WorkList.Domain;

public class SelectionWorkList : WorkList
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public SelectionWorkList([NotNull] IWorkItemRepository repository,
	                         [CanBeNull] Geometry areaOfInterest,
	                         [NotNull] string uniqueName,
	                         [NotNull] string displayName) :
		base(repository, areaOfInterest, uniqueName, displayName) { }

	protected override IEnumerable<long> ProcessUpdatesCore(
		Table table, List<long> oids,
		SpatialHashSearcher<IWorkItem> searcher,
		List<long> insertedOids)
	{
		QueryFilter filter = GdbQueryUtils.CreateFilter(oids);

		foreach ((WorkItem item, Geometry geometry) in Repository.GetItems<WorkItem>(
			         table, filter, ignoreDefinitionQuery: true))
		{
			// This overridden method is the same as its base method except the
			// Repository.Refresh(item) here.
			// All work lists read their items' status from the database.
			// The Selection Work List is different. It reads the status from
			// the work list file. This happens here.
			Repository.Refresh(item);

			if (! TryGetItem(item.GdbRowProxy, out IWorkItem cachedItem))
			{
				// The edit was reported as an update but the item is not in the cache.
				// This happens e.g. when an insert is reported as a modify by the edit
				// event. Treat it as an insert so the new item still shows up.
				_msg.Debug(
					$"Update for {item.GdbRowProxy} but item is not in the cache. Treating as insert.");

				// Assign extent/display geometry before adding so the item is
				// registered in the spatial searcher (see ProcessInserts).
				SetItemGeometry(item, geometry);

				if (! TryAddItem(item))
				{
					_msg.Debug($"Cannot add {item} as insert.");
					continue;
				}

				//Repository.Refresh(item);

				// Report as an insert (invalidated by extent), not as an update: the
				// layer's display cache does not know this feature's OID yet.
				insertedOids.Add(item.OID);
				continue;
			}

			// Keep the cached geometry/extent in sync with the source, regardless of a
			// status change. The extent is always maintained (also updates the
			// SpatialHashSearcher); the buffered display geometry only when
			// CacheBufferedItemGeometries is set.
			if (geometry != null && cachedItem.HasExtent)
			{
				Envelope extent = Assert.NotNull(cachedItem.Extent);

				searcher?.Remove(cachedItem,
				                 extent.XMin, extent.YMin,
				                 extent.XMax, extent.YMax);

				SetItemGeometry(cachedItem, geometry);

				searcher?.Add(cachedItem, CreateEnvelope(cachedItem));
			}

			// Update cached item's state from database item. IWorkItem.Status
			// also is updated in GdbItemRepository.SetStatusCoreAsync().
			cachedItem.Status = item.Status;

			yield return cachedItem.OID;
		}
	}
}
