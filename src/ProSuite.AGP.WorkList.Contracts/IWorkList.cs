using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts
{
	/// <summary>
	/// A WorkList is a named list of work items.
	/// It maintains a current item and provides
	/// navigation to change the current item.
	/// </summary>
	public interface IWorkList : IDisposable
	{
		[NotNull]
		string Name { get; }

		[CanBeNull]
		Envelope Extent { get; }

		WorkItemVisibility Visibility { get; set; }

		[CanBeNull]
		Polygon AreaOfInterest { get; set; }

		bool QueryLanguageSupported { get; }

		/// <summary>Yield all work items subject to list settings and the given filter.</summary>
		/// <param name="filter">optional QueryFilter or SpatialQueryFilter</param>
		/// <param name="ignoreListSettings">if true, ignore Visibility and AreaOfInterest</param>
		/// <returns></returns>
		[NotNull]
		IEnumerable<IWorkItem> GetItems(QueryFilter filter = null, bool ignoreListSettings = false);

		/// <summary>Equivalent to GetItems(filter).Count(), but may be faster</summary>
		int Count(QueryFilter filter = null, bool ignoreListSettings = false);

		/* Navigation */

		[CanBeNull]
		IWorkItem Current { get; }

		bool CanGoFirst();
		void GoFirst();

		bool CanGoNearest();
		void GoNearest();

		bool CanGoNext();
		void GoNext();

		bool CanGoPrevious();
		void GoPrevious();

		event EventHandler<WorkListChangedEventArgs> WorkListChanged;
	}
}
