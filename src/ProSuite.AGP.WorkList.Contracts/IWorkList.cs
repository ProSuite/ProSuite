using System;
using System.Collections.Generic;
using System.ComponentModel;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IWorkList : IRowCache, INotifyPropertyChanged
	{
		[NotNull]
		string Name { get; set; }

		[NotNull]
		string DisplayName { get; }

		[CanBeNull]
		Envelope Extent { get; }

		WorkItemVisibility Visibility { get; set; }

		[CanBeNull]
		Geometry AreaOfInterest { get; set; }

		bool QueryLanguageSupported { get; }

		[CanBeNull]
		IWorkItem Current { get; }

		int CurrentIndex { get; set; }
		IWorkItemRepository Repository { get; }

		event EventHandler<WorkListChangedEventArgs> WorkListChanged;

		/// <summary>Yield all work items subject to list settings and the given filter.</summary>
		/// <param name="filter">optional QueryFilter or SpatialQueryFilter</param>
		/// <param name="ignoreListSettings">if true, ignore Visibility and AreaOfInterest</param>
		/// <param name="startIndex"></param>
		/// <returns></returns>
		[NotNull]
		IEnumerable<IWorkItem> GetItems([CanBeNull] QueryFilter filter = null,
		                                bool ignoreListSettings = false,
		                                int startIndex = -1);

		int Count([CanBeNull] QueryFilter filter = null, bool ignoreListSettings = false);

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

		void SetVisited([NotNull] IWorkItem item);

		void Commit();

		void SetStatus([NotNull] IWorkItem item, WorkItemStatus status);

		void RefreshItems();

		bool IsValid(out string message);

		IAttributeReader GetAttributeReader(long forSourceClassId);
	}
}
