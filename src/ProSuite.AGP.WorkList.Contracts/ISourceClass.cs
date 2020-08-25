using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface ISourceClass
	{
		string Name { get; }
		IAttributeReader AttributeReader { get; }
		long Id { get; }

		bool Uses(GdbTableIdentity table);

		WorkItemStatus GetStatus(Row row);
	}
}
