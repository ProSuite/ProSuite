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
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList
{
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

		public void Dispose()
		{
			UnwireEvents();
		}

		private void WireEvents()
		{
			_eventToken = EditCompletedEvent.Subscribe(OnEditCompleted);
		}

		private async Task OnEditCompleted(EditCompletedEventArgs e)
		{
			_msg.VerboseDebug(() => nameof(OnEditCompleted));

			try
			{
				switch (e.CompletedType)
				{
					case EditCompletedType.Save:
						break;
					case EditCompletedType.Discard:
						await QueuedTask.Run(() => { _rowCache.Invalidate(); });
						break;
					case EditCompletedType.Operation:
					case EditCompletedType.Undo:
					case EditCompletedType.Redo:
						await QueuedTask.Run(() => { ProcessChanges(e); });
						break;
					case EditCompletedType.Reconcile:
						await QueuedTask.Run(() => { _rowCache.Invalidate(); });
						break;
					case EditCompletedType.Post:
						break;
					case EditCompletedType.Unknown:
						break;
					default:
						throw new ArgumentOutOfRangeException(
							nameof(EditCompletedType),
							e.CompletedType,
							$@"Unexpected EditCompletedType: {e.CompletedType}");
				}
			}
			catch (Exception ex)
			{
				_msg.Error($"Error {nameof(OnEditCompleted)}: {ex.Message}", ex);
			}
		}

		private void ProcessChanges(EditCompletedEventArgs args)
		{
			// On Undo and Redo e.Members is not empty
			//if (! e.Members.Any(member => _rowCache.CanContain(member)))
			//{
			//	return;
			//}

			var fullTableInvalidations = new List<Table>();

			// TODO: Try prevent too many calls
			foreach (MapMember mapMember in args.InvalidateAllMembers)
			{
				if (mapMember is BasicFeatureLayer featureLayer)
				{
					// Test if the layer is still in the map?
					Table table = featureLayer.GetTable();

					if (_rowCache.CanContain(table))
					{
						fullTableInvalidations.Add(table);
					}
				}
			}

			if (fullTableInvalidations.Count > 0)
			{
				_rowCache.Invalidate(fullTableInvalidations);
			}

			// Note This event is fired (to) many times!
			// Add work list to map -> it fires 6 times. There are
			// 6 layers in group layer QA..
			// Remove group layer, save project and add work list again.
			// The event fires more often! Wtf..?!
			if (args.Creates.IsEmpty && args.Deletes.IsEmpty && args.Modifies.IsEmpty)
			{
				return;
			}

			var createsByLayer = SelectionUtils.GetSelection(args.Creates);
			var deletesByLayer = SelectionUtils.GetSelection(args.Deletes);
			var modsByLayer = SelectionUtils.GetSelection(args.Modifies);

			Dictionary<Table, List<long>> creates = GetOidsByTable(createsByLayer);
			Dictionary<Table, List<long>> deletes = GetOidsByTable(deletesByLayer);
			Dictionary<Table, List<long>> modifies = GetOidsByTable(modsByLayer);

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

		private Dictionary<Table, List<long>> GetOidsByTable(
			Dictionary<MapMember, List<long>> oidsByMapMember)
		{
			var result = new Dictionary<Table, List<long>>(oidsByMapMember.Count);

			var tableByHandle = new Dictionary<IntPtr, Table>(oidsByMapMember.Count);

			var oidsByHandle = new Dictionary<IntPtr, List<long>>();

			foreach (var pair in oidsByMapMember)
			{
				MapMember mapMember = pair.Key;
				IReadOnlyCollection<long> oids = pair.Value;

				if (! (mapMember is FeatureLayer featureLayer))
				{
					continue;
				}

				Table table = featureLayer.GetTable();

				if (! _rowCache.CanContain(table))
				{
					continue;
				}

				if (oidsByHandle.ContainsKey(table.Handle))
				{
					oidsByHandle[table.Handle].AddRange(oids);
				}
				else
				{
					tableByHandle.Add(table.Handle, table);
					oidsByHandle.Add(table.Handle, oids.ToList());
				}
			}

			foreach (KeyValuePair<IntPtr, Table> pair in tableByHandle)
			{
				IntPtr handle = pair.Key;
				Table table = pair.Value;

				if (oidsByHandle.TryGetValue(handle, out List<long> oids))
				{
					result.Add(table, oids.Distinct().ToList());
				}
			}

			return result;
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
