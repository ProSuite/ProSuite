using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public abstract class RowFilter : InvolvesTablesBase, IRowFilter
	{
		public string Name { get; set; }

		protected RowFilter([NotNull] IEnumerable<ITable> tables)
			: base(tables) { }

		public abstract bool VerifyExecute(IRow row);
	}
}
