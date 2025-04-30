using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IWorkList : IRowCache
	{
		[NotNull]
		string Name { get; set; }

		[NotNull]
		string DisplayName { get; }

		[CanBeNull]
		Envelope Extent { get; }

		WorkItemVisibility Visibility { get; set; }
		
		[NotNull]
		Geometry AreaOfInterest { get; set; }

		bool QueryLanguageSupported { get; }

		[CanBeNull]
		IWorkItem Current { get; }

		int CurrentIndex { get; set; }

		// TODO: daro hide?
		IWorkItemRepository Repository { get; }
		long? TotalCount { get; set; }

		event EventHandler<WorkListChangedEventArgs> WorkListChanged;

		IEnumerable<IWorkItem> Search(QueryFilter filter);

		IEnumerable<IWorkItem> GetItems([CanBeNull] QueryFilter filter = null,
		                                bool excludeGeometry = false);

		int Count();

		long Count(out int todo);

		bool CanGoFirst();

		void GoFirst();

		void GoTo(long oid);

		bool CanGoNearest();

		void GoNearest([NotNull] Geometry reference,
		               [CanBeNull] Predicate<IWorkItem> match = null,
		               params Polygon[] contextPerimeters);

		bool CanGoNext();

		void GoNext();

		bool CanGoPrevious();

		void GoPrevious();

		bool CanSetStatus();

		//void SetVisited([NotNull] IWorkItem item);
		void SetVisited(IList<IWorkItem> items, bool visited);

		void Commit();

		Task SetStatusAsync([NotNull] IWorkItem item, WorkItemStatus status);

		bool IsValid(out string message);

		IAttributeReader GetAttributeReader(long forSourceClassId);

		/// <summary>
		/// Ensures that the work list's row cache is synchronized with the underlying data store.
		/// Edits to the associated source tables will be reflected in the row cache.
		/// This is required for both the work list layer and the navigator to show the correct data.
		/// </summary>
		void EnsureRowCacheSynchronized();

		/// <summary>
		/// Deactivate the synchronization of the work list's row cache with the underlying data store.
		/// </summary>
		void DeactivateRowCacheSynchronization();

		Geometry GetItemGeometry(IWorkItem item);

		void SetItemsGeometryDraftMode(bool enable);

		void Rename(string name);

		void Invalidate(Envelope geometry);

		void Invalidate(List<long> oids);

		void UpdateItemGeometries(QueryFilter filter);

		IEnumerable<IWorkItem> GetItems(QueryFilter filter, WorkItemStatus? itemStatus = null, bool excludeGeometry = false);

		void ComputeTotalCount();
	}
}
