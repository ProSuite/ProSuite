using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	/// <summary>
	/// Defines the elements of a <see cref="DatabaseSourceClass"/>. Consider serializing this class
	/// together with the work items in order to maintain the basic schema of the database source.
	/// </summary>
	public class DbStatusSourceClassDefinition
	{
		public DbStatusSourceClassDefinition([NotNull] Table table,
		                                     [CanBeNull] string definitionQuery,
		                                     [NotNull] WorkListStatusSchema statusSchema)
		{
			Table = table;
			DefinitionQuery = definitionQuery;
			StatusSchema = statusSchema;
		}

		[NotNull]
		public Table Table { get; }

		[NotNull]
		public WorkListStatusSchema StatusSchema { get; }

		[CanBeNull]
		public string DefinitionQuery { get; }

		[CanBeNull]
		public IAttributeReader AttributeReader { get; set; }
	}
}
