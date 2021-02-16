using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public abstract class AssociationDescription
	{
		private readonly ITable _table1;
		private readonly ITable _table2;

		protected AssociationDescription([NotNull] ITable table1,
		                                 [NotNull] ITable table2)
		{
			_table1 = table1;
			_table2 = table2;
		}

		[NotNull]
		public ITable Table1 => _table1;

		[NotNull]
		public ITable Table2 => _table2;
	}
}
