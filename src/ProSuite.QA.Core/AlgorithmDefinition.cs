using System.Collections.Generic;
using ProSuite.Commons.Db;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Core
{
	/// <summary>
	/// Base class for instance definitions. The definitions can be instantiated in every
	/// environment in order to get the metadata.
	/// </summary>
	public abstract class AlgorithmDefinition
	{
		public IList<IDbTableSchema> InvolvedTables { get; }

		protected AlgorithmDefinition([NotNull] IDbTableSchema involvedTable) : this(
			new[] { involvedTable }) { }

		protected AlgorithmDefinition([NotNull] IEnumerable<IDbTableSchema> involvedTables)
		{
			Assert.ArgumentNotNull(involvedTables, nameof(involvedTables));

			InvolvedTables = new List<IDbTableSchema>(involvedTables);
		}
	}
}
