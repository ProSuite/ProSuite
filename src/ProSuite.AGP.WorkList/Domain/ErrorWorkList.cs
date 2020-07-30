using System;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Domain
{
	public class ErrorWorkList : WorkList
	{
		public ErrorWorkList(IWorkItemRepository repository, string name) :
			base(repository, name) { }

		public override void Dispose()
		{
			throw new NotImplementedException();
		}
	}
}
