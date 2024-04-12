using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;

namespace ProSuite.AGP.QA.Worklist
{
	public interface IWorkListItemDatastore
	{
		bool Validate(out string message);

		IEnumerable<Table> GetTables();

		Task<bool> TryPrepareSchema();

		Task<IList<Table>> PrepareTableSchema(IList<Table> dbTables);
	}
}
