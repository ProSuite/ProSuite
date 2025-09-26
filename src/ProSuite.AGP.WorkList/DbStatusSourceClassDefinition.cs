using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	/// <summary>
	/// Defines the elements of a <see cref="DatabaseSourceClass"/>. Consider serializing this class
	/// together with the work items in order to maintain the basic schema of the database source.
	/// </summary>
	/// 
	public class SourceClassDefinition
	{
		public SourceClassDefinition([NotNull] Table table,
									 [NotNull] SourceClassSchema schema)
		{
			Table = table;
			Schema = schema;
		}

		[NotNull]
		public Table Table { get; }

		[NotNull]
		public SourceClassSchema Schema { get; }
	}

	public class DbStatusSourceClassDefinition : SourceClassDefinition
	{
		public DbStatusSourceClassDefinition([NotNull] Table table,
											 [CanBeNull] string definitionQuery,
											 [NotNull] DbSourceClassSchema schema) : base(table, schema)
		{
			DefinitionQuery = definitionQuery;
		}

		[CanBeNull]
		public string DefinitionQuery { get; }

		[CanBeNull]
		public IAttributeReader AttributeReader { get; set; }
	}
}
