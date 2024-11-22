using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts
{
	/// <summary>
	/// Encapsulates the physical geodatabase schema of database-backed work list items, i.e. the
	/// work list items from DbStatusWorkLists. This could be the FGDB issue schema or the
	/// traditional Error-Datasets from the production models.
	/// </summary>
	public interface IWorkListItemDatastore
	{
		/// <summary>
		/// Validates the datastore and returns a message if the validation fails. Must be called
		/// on the MCT.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		bool Validate(out string message);

		IEnumerable<Table> GetTables();

		Task<bool> TryPrepareSchema();

		Task<IList<Table>> PrepareTableSchema(IList<Table> dbTables);

		// TODO: Move this to another more dedicated interface IIssueTableSchema
		IAttributeReader CreateAttributeReader([NotNull] TableDefinition definition,
		                                       [NotNull] params Attributes[] attributes);

		string SuggestWorkListName();
	}
}
