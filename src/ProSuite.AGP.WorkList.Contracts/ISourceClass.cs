using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface ISourceClass
	{
		string Name { get; }
		GdbTableReference Identity { get; }
		IAttributeReader AttributeReader { get; }
	}
}
