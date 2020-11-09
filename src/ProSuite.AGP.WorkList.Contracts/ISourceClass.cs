using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface ISourceClass
	{
		long Id { get; }

		string Name { get; }

		bool Uses(GdbTableIdentity table);
	}
}
