using System;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain
{
	public class SelectionWorkList : WorkList, IDisposable
	{
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

		public void Dispose()
		{
			DeactivateRowCacheSynchronization();
		}
	}
}
