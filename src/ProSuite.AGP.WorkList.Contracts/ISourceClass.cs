using ArcGIS.Core.Data;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface ISourceClass
	{
		string Name { get; }
		IAttributeReader AttributeReader { get; }
		long Id { get; }

		bool Uses(Table table);
	}
}
