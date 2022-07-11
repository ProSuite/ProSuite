using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public abstract class AssociationDescription
	{
		private readonly IReadOnlyTable _table1;
		private readonly IReadOnlyTable _table2;

		protected AssociationDescription([NotNull] IReadOnlyTable table1,
		                                 [NotNull] IReadOnlyTable table2)
		{
			_table1 = table1;
			_table2 = table2;
		}

		/// <summary>
		/// The 'left' table in the join.
		/// </summary>
		[NotNull]
		public IReadOnlyTable Table1 => _table1;

		/// <summary>
		/// The 'right' table in the join.
		/// </summary>
		[NotNull]
		public IReadOnlyTable Table2 => _table2;
	}
}
