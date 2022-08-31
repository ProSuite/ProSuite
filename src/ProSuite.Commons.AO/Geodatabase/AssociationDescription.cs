using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public abstract class AssociationDescription
	{
		protected AssociationDescription([NotNull] IReadOnlyTable table1,
		                                 [NotNull] IReadOnlyTable table2)
		{
			Table1 = table1;
			Table2 = table2;
		}

		/// <summary>
		/// The 'left' table in the join.
		/// </summary>
		[NotNull]
		public IReadOnlyTable Table1 { get; set; }

		/// <summary>
		/// The 'right' table in the join.
		/// </summary>
		[NotNull]
		public IReadOnlyTable Table2 { get; set; }
	}
}
