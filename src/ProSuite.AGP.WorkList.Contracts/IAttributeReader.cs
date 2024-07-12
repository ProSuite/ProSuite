using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Domain;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IAttributeReader
	{
		T GetValue<T>(Row row, Attributes attribute);

		void ReadAttributes(Row fromRow, IIssueItem forItem, ISourceClass source);
	}
}
