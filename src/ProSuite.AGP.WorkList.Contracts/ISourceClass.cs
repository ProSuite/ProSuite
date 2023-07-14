using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface ISourceClass
	{
		string Name { get; }

		[CanBeNull]
		IAttributeReader AttributeReader { get; }

		bool HasGeometry { get; }

		bool Uses(GdbTableIdentity table);
	}
}
