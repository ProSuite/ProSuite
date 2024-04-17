using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IWorkListItemDatastore
	{
		bool Validate(out string message);

		IEnumerable<Table> GetTables();

		Task<bool> TryPrepareSchema();

		Task<IList<Table>> PrepareTableSchema(IList<Table> dbTables);

		// TODO: Move this to another more dedicated interface IIssueTableSchema
		IAttributeReader CreateAttributeReader([NotNull] TableDefinition definition,
		                                       [NotNull] params Attributes[] attributes);
	}
}
