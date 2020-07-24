using System;
using System.Collections;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AG.Gdb;

namespace ProSuite.AGP.WorkList.Service
{
	// todo daro: separate geometry processing code
	// todo daro: separate QueuedTask code
	public class WorkItemService : IWorkItemService<WorkItem>
	{
		private const int _initialCapacity = 1000;

		// Implement _objectMap.
		// 1) Use CoreObjectsBase.Handle re-read the handles for all work items in work list on startup.
		// 2) Use clever cache type rather than .NET Dictinary.
		//	if it is
		//	a) FileGDB workspace, use path
		//	b) SDE, use connection string incl. version info.
		// (Is it possible to edit different sde versions within the same ArcGIS Pro map?)
		private readonly Dictionary<GdbRowReference, WorkItem> _itemMap = new Dictionary<GdbRowReference, WorkItem>(_initialCapacity);

		private readonly List<WorkItem> _workItems = new List<WorkItem>(_initialCapacity);

		private readonly IWorkItemRepository _repository;
		private readonly SpatialReference _spatialReference;

		public WorkItemService(IWorkItemRepository repository, SpatialReference spatialReference)
		{
			_repository = repository;
			_spatialReference = spatialReference;

			foreach (WorkItem item in _repository.GetAll())
			{
				_itemMap.Add(item.Proxy, item);

				_workItems.Add(item);
			}
		}

		public IEnumerable<object[]> GetRowValues(QueryFilter filter, bool recycle)
		{
			yield break;
			//foreach (KeyValuePair<WorkItem, IReadOnlyList<Coordinate3D>> pair in _repository.GetItems(filter, recycle))
			//{
			//	WorkItem item = pair.Key;
			//	GdbRowReference reference = item.Proxy;

			//	// todo daro: fill item map initially? In a explicit method?

			//	if (!_itemMap.TryGetValue(reference, out WorkItem cachedItem))
			//	{
			//		// todo daro: what if I don't find items?
			//		continue;
			//	}

			//	IReadOnlyList<Coordinate3D> coordinates = pair.Value;
			//	object shape = CreateShape(coordinates);

			//	object[] values =
			//	{
			//		(int) cachedItem.OID, (int) cachedItem.Visited
			//	};

			//	yield return values;
			//}
		}

		public Envelope GetExtent()
		{
			throw new NotImplementedException();
		}

		public void ProcessChanges(IList<GdbRowReference> creates,
		                           IList<GdbRowReference> modifies,
		                           IList<GdbRowReference> deletes)
		{
			throw new NotImplementedException();
		}

		public void Invalidate()
		{
			throw new NotImplementedException();
		}

		public WorkItem GetItem(GdbRowReference reference)
		{
			throw new NotImplementedException();
		}

		public void ProcessChanges(IList creates, IList modifies, IList deletes)
		{
			throw new NotImplementedException();
		}

		private object CreateShape(IReadOnlyList<Coordinate3D> coordinates)
		{
			if (coordinates.Count > 1)
			{
				// todo daro: create box around point
				throw new NotImplementedException();
			}

			// todo daro: is it always Polygon? like a bounding box?
			// todo daro: buffer it up!
			return PolygonBuilder.CreatePolygon(coordinates, _spatialReference);
		}
	}
}
