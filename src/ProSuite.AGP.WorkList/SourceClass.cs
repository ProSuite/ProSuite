using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	public class SourceClass : ISourceClass
	{
		private GdbTableIdentity _identity;

		protected SourceClass(GdbTableIdentity identity)
		{
			_identity = identity;
		}

		public long Id => _identity.Id;

		[NotNull]
		public string Name => _identity.Name;

		public bool Uses(GdbTableIdentity table)
		{
			return _identity.Equals(table);
		}
	}
}
