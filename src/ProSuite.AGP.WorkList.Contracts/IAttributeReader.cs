using ArcGIS.Core.Data;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IAttributeReader
	{
		T GetValue<T>(Row row, Attributes attribute);
	}
}
