using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.DomainModel.Core.QA
{
	/// <summary>
	/// Provides the output schema (i.e. the fields) of a transformer, for callers that must
	/// know which fields a transformer-fed parameter value offers - for example the SQL query
	/// builder, when it builds an expression against the output of a transformer instead of
	/// against a stored dataset.
	/// </summary>
	/// <remarks>
	/// The output schema of a transformer is not stored in the data dictionary: it results
	/// from the transformation and can only be determined by instantiating the transformer
	/// against real data. Implementations therefore need data access (a geodatabase
	/// connection); environments without it provide no implementation, and callers fall back
	/// to treating a transformer-fed value as a value of unknown schema.
	/// </remarks>
	public interface ITransformerOutputSchemaProvider
	{
		/// <summary>
		/// Opens the output table of the specified transformer and returns its schema.
		/// </summary>
		/// <exception cref="System.InvalidOperationException">The output schema cannot be
		/// determined, e.g. because the master database is not accessible or because the
		/// transformer is incompletely configured.</exception>
		[NotNull]
		ITableSchemaDef GetOutputSchema([NotNull] TransformerConfiguration transformer);
	}
}
