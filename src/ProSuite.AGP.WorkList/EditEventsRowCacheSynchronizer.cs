using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Events;
using ArcGIS.Desktop.Editing.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList
{
	// todo daro: Rename to MapEditsRowCacheSynchronizer to indicate that only edits in the map
	// are catched? But that's somehow a implicit fact.
	// todo daro: is this the right namespace for this type?
	public class EditEventsRowCacheSynchronizer : IDisposable
	{
		private readonly IRowCache _rowCache;
		private SubscriptionToken _eventToken;

		public EditEventsRowCacheSynchronizer(IRowCache rowCache)
		{
			_rowCache = rowCache;
			WireEvents();
		}

		private void WireEvents()
		{
			_eventToken = EditCompletedEvent.Subscribe(OnEditCompleted);
		}

		private Task OnEditCompleted(EditCompletedEventArgs args)
		{
			switch (args.CompletedType)
			{
				case EditCompletedType.Save:
					break;
				case EditCompletedType.Discard:
					QueuedTask.Run(() => { _rowCache.Invalidate(); });
					break;
				case EditCompletedType.Operation:
					ProcessChanges(args);
					break;
				case EditCompletedType.Undo:
				case EditCompletedType.Redo:
					QueuedTask.Run(() => { ProcessChanges(args); });
					break;
				case EditCompletedType.Reconcile:
					break;
				case EditCompletedType.Post:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			// todo: revise
			return Task.FromResult(0);
		}

		private void ProcessChanges(EditCompletedEventArgs args)
		{
			// On Undo and Redo args.Members is not empty
			//if (! args.Members.Any(member => _rowCache.CanContain(member)))
			//{
			//	return;
			//}

			// todo daro: try-finally to dispose tables?
			Dictionary<Table, List<long>> creates = GetOidsByTable(args.Creates);
			Dictionary<Table, List<long>> deletes = GetOidsByTable(args.Deletes);
			Dictionary<Table, List<long>> modifies = GetOidsByTable(args.Modifies);

			_rowCache.ProcessChanges(creates, deletes, modifies);
		}

		private static Dictionary<Table, List<long>> GetOidsByTable(
			IReadOnlyDictionary<MapMember, IReadOnlyCollection<long>> oidsByMapMember)
		{
			var result = new Dictionary<Table, List<long>>();

			foreach (KeyValuePair<MapMember, IReadOnlyCollection<long>> pair in oidsByMapMember)
			{
				MapMember mapMember = pair.Key;
				if (! (mapMember is FeatureLayer featureLayer))
				{
					continue;
				}

				using (var table = featureLayer.GetTable())
				{
					if (! result.ContainsKey(table))
					{
						result.Add(table, pair.Value.ToList());
					}
					else
					{
						result[table].AddRange(pair.Value);
					}
				}
			}

			return result;
		}

		public void Dispose()
		{
			UnwireEvents();
		}

		private void UnwireEvents()
		{
			if (_eventToken != null)
			{
				EditCompletedEvent.Unsubscribe(_eventToken);
			}
		}
	}
}