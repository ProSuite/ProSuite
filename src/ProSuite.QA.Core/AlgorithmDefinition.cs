using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.QA.Core
{
	/// <summary>
	/// Base class for instance definitions. The definitions have minimal dependencies and can be
	/// instantiated in every environment in order to get the metadata.
	/// </summary>
	public abstract class AlgorithmDefinition
	{
		public IList<ITableSchemaDef> InvolvedTables { get; }

		protected AlgorithmDefinition([NotNull] ITableSchemaDef involvedTable) : this(
			new[] { involvedTable }) { }

		protected AlgorithmDefinition([NotNull] IEnumerable<ITableSchemaDef> involvedTables)
		{
			Assert.ArgumentNotNull(involvedTables, nameof(involvedTables));

			InvolvedTables = new List<ITableSchemaDef>(involvedTables);
		}
	}
}
