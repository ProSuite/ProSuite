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
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList
{
	// todo daro: Rename to MapEditsRowCacheSynchronizer to indicate that only edits in the map
	// are catched? But that's somehow a implicit fact.
	// todo daro: is this the right namespace for this type?
	public class EditEventsRowCacheSynchronizer : IDisposable
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IRowCache _rowCache;
		private SubscriptionToken _eventToken;

		public EditEventsRowCacheSynchronizer([NotNull] IRowCache rowCache)
		{
			Assert.ArgumentNotNull(rowCache, nameof(rowCache));

			_rowCache = rowCache;
			WireEvents();
		}

		private void WireEvents()
		{
			_eventToken = EditCompletedEvent.Subscribe(OnEditCompleted);
		}

		private async Task OnEditCompleted(EditCompletedEventArgs args)
		{
			_msg.VerboseDebug(() => nameof(OnEditCompleted));

			try
			{
				switch (args.CompletedType)
				{
					case EditCompletedType.Save:
						break;
					case EditCompletedType.Discard:
						await QueuedTask.Run(() => { _rowCache.Invalidate(); });
						break;
					case EditCompletedType.Operation:
						ProcessChanges(args);
						break;
					case EditCompletedType.Undo:
					case EditCompletedType.Redo:
						await QueuedTask.Run(() => { ProcessChanges(args); });
						break;
					case EditCompletedType.Reconcile:
						break;
					case EditCompletedType.Post:
						break;
					default:
						throw new ArgumentOutOfRangeException(
							nameof(EditCompletedType),
							args.CompletedType,
							$"Unexpected EditCompletedType: {args.CompletedType}");
				}
			}
			catch (Exception ex)
			{
				_msg.Error($"Error {nameof(OnEditCompleted)}: {ex.Message}", ex);

				// todo: revise
				await Task.FromResult(0);
			}
		}

		private void ProcessChanges(EditCompletedEventArgs args)
		{
			// On Undo and Redo args.Members is not empty
			//if (! args.Members.Any(member => _rowCache.CanContain(member)))
			//{
			//	return;
			//}

			// todo daro: Use GdbTableIdentity and dispose tables immediately?
			Dictionary<Table, List<long>> creates = GetOidsByTable(args.Creates);
			Dictionary<Table, List<long>> deletes = GetOidsByTable(args.Deletes);
			Dictionary<Table, List<long>> modifies = GetOidsByTable(args.Modifies);
			try
			{
				_rowCache.ProcessChanges(creates, deletes, modifies);
			}
			finally
			{
				Dispose(creates.Keys);
				Dispose(deletes.Keys);
				Dispose(modifies.Keys);
			}
		}

		private static void Dispose(Dictionary<Table, List<long>>.KeyCollection tables)
		{
			foreach (Table table in tables)
			{
				table.Dispose();
			}
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

				Table table = featureLayer.GetTable();

				if (! result.ContainsKey(table))
				{
					result.Add(table, pair.Value.ToList());
				}
				else
				{
					result[table].AddRange(pair.Value);
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
