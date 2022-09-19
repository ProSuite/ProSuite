using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public abstract class RowFilter : InvolvesTablesBase, IRowFilter
	{
		public string Name { get; set; }

		protected RowFilter([NotNull] IEnumerable<IReadOnlyTable> tables)
			: base(tables) { }

		public abstract bool VerifyExecute(IReadOnlyRow row);
	}
}
