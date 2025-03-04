using System;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.AGP.WorkList.Domain
{
	public class SelectionWorkList : WorkList, IDisposable
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Do not change this constructor at all, it is used for dynamic loading!
		/// </summary>
		/// <param name="repository"></param>
		/// <param name="uniqueName"></param>
		/// <param name="displayName"></param>
		[UsedImplicitly]
		public SelectionWorkList(IWorkItemRepository repository,
		                         string uniqueName,
		                         string displayName) :
			base(repository, uniqueName, null, displayName) { }

		protected override string GetDisplayNameCore()
		{
			return "Selection Work List";
		}

		private readonly Dictionary<int, int> _indexByTask = new(20);
		private readonly Dictionary<int, List<IWorkItem>> _itemsByIndex = new(20);
		private readonly List<List<IWorkItem>> _itemChunks = new(3);

		public bool TryGetItems(int taskId, out List<IWorkItem> result)
		{
			result = null;

			if (_indexByTask.ContainsKey(taskId))
			{
				return false;
			}

			foreach (List<IWorkItem> items in _itemChunks)
			{
				int index = _itemChunks.IndexOf(items);

				if (_itemsByIndex.TryGetValue(index, out List<IWorkItem> _))
				{
					continue;
				}

				_indexByTask.Add(taskId, index);
				_itemsByIndex.Add(index, items);

				result = items;
				return true;
			}

			return false;
		}

		public override void RefreshItems()
		{
			try
			{
				var items = Repository.GetItems(AreaOfInterest, WorkItemStatus.Todo).ToList();

				Items = new List<IWorkItem>(items.Count);
				RowMap.Clear();

				int chunkSize = items.Count / 3;
				int firstListChunkSize = chunkSize + items.Count % 3;

				_itemChunks.Clear();
				_itemChunks.Add(new List<IWorkItem>(firstListChunkSize));
				_itemChunks.Add(new List<IWorkItem>(chunkSize));
				_itemChunks.Add(new List<IWorkItem>(chunkSize));

				int itemsCount = 0;
				var listIndex = 0;
				List<IWorkItem> currentList = Assert.NotNull(_itemChunks[listIndex]);

				foreach (IWorkItem item in items)
				{
					RowMap[item.GdbRowProxy] = item;

					Items.Add(item);
					currentList.Add(item);

					// distribute the items to the 3 chunks
					itemsCount += 1;

					if (itemsCount == currentList.Capacity)
					{
						listIndex += 1;

						if (listIndex == _itemChunks.Capacity)
						{
							break;
						}

						currentList = Assert.NotNull(_itemChunks[listIndex]);
						itemsCount = 0;
					}
				}

				_msg.DebugFormat("Added {0} items to work list", Items.Count);

				// initializes the state repository if no states for
				// the work items are read yet
				Repository.UpdateVolatileState(Items);

				_msg.DebugFormat("Getting extents for {0} items...", Items.Count);
				// todo: daro EnvelopeBuilder as parameter > do not iterate again over items
				//			  look old work item implementation
				Extent = GetExtentFromItems(Items);
			}
			catch (Exception ex)
			{
				Gateway.ReportError(ex, _msg);
			}
		}

		public void Dispose()
		{
			// TODO: (daro) inline?
			DeactivateRowCacheSynchronization();

			foreach (IWorkItem item in Items)
			{
				item.Geometry = null;
			}

			Items.Clear();
			RowMap.Clear();
		}
	}
}
