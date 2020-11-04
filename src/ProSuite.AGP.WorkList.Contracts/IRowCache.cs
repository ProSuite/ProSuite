using System.Collections.Generic;
using ArcGIS.Core.Data;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IRowCache
	{
		/// <summary>
		///     Indicates that the cache should be completely invalidated (i.e. rows should
		///     be refetched), based on a given feature workspace.
		/// </summary>
		/// old, probably not needed anymore:
		/// void Invalidate([NotNull] IFeatureWorkspace featureWorkspace);
		void Invalidate();

		/// <summary>
		///     Passes all changes that happened in an edit operation to the cache for processing.
		/// </summary>
		/// <param name="inserts"></param>
		/// <param name="deletes">The deleted rows.</param>
		/// <param name="updates"></param>
		void ProcessChanges(Dictionary<Table, List<long>> inserts,
		                    Dictionary<Table, List<long>> deletes,
		                    Dictionary<Table, List<long>> updates);

		/// <summary>
		///     Determines whether the object cache can contain the specified dataset.
		///     This is used by the row cache synchronizer to ignore irrelevant change events.
		/// </summary>
		/// <param name="dataset">The dataset.</param>
		/// <returns>
		///     <c>true</c> if the row cache can contain the specified dataset; otherwise, <c>false</c>.
		///     Only changes for datasetes for which this returns <c>true</c> will be passed to
		///     <see cref="ProcessChanges" />.
		/// </returns>
		/// <remarks>
		///     If this check is potentially expensive for a given row cache implementation,
		///     <c>true</c> can be returned. In this case the implementation of <see cref="ProcessChanges" />
		///     should ignore any irrelevant instances.
		/// </remarks>
		/// old and some optimization, probably not needed anymore because we get the
		/// creates, modifies, deletes out of the EditCompletedEventArgs:
		/// 
		/// 
		/// NOT NEEDED ANYMORE!
		//bool CanContain([NotNull] Dataset dataset);
		//bool CanContain(Func<MapMember, bool> featuresModified);

		//bool CanContain(MapMember member);
	}
}
