using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.QA.Worklist
{
	public interface IWorkListItemDatastore
	{
		bool Validate(out string message);

		IEnumerable<Table> GetTables();

		Task<bool> TryPrepareSchema();

		Task<IList<Table>> PrepareTableSchema(IList<Table> dbTables);

		IAttributeReader CreateAttributeReader([NotNull] TableDefinition definition,
		                                       [NotNull] params Attributes[] attributes);
	}
}
