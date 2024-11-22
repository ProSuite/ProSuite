using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList
{
	/// <summary>
	/// Defines the elements of a <see cref="DatabaseSourceClass"/>. Consider serializing this class
	/// together with the work items in order to maintain the basic schema of the database source.
	/// </summary>
	public class DbStatusSourceClassDefinition
	{
		public DbStatusSourceClassDefinition(Table table, string definitionQuery,
		                                     WorkListStatusSchema statusSchema)
		{
			Table = table;
			DefinitionQuery = definitionQuery;
			StatusSchema = statusSchema;
		}

		public Table Table { get; init; }
		public string DefinitionQuery { get; init; }
		public WorkListStatusSchema StatusSchema { get; init; }
	}
}
