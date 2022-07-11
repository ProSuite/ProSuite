using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class ForeignKeyAssociationDescription : AssociationDescription
	{
		private readonly string _referencingKeyName;
		private readonly string _referencedKeyName;

		public ForeignKeyAssociationDescription([NotNull] IReadOnlyTable referencingTable,
		                                        [NotNull] string referencingKeyName,
		                                        [NotNull] IReadOnlyTable referencedTable,
		                                        [NotNull] string referencedKeyName)
			: base(referencingTable, referencedTable)
		{
			_referencingKeyName = referencingKeyName;
			_referencedKeyName = referencedKeyName;
		}

		/// <summary>
		/// The key field in the <see cref="ReferencingTable"/>.
		/// </summary>
		[NotNull]
		public string ReferencingKeyName => _referencingKeyName;

		/// <summary>
		/// The key field in the <see cref="ReferencedTable"/>.
		/// </summary>
		[NotNull]
		public string ReferencedKeyName => _referencedKeyName;

		/// <summary>
		/// The 'left' table in the join, containing the <see cref="ReferencingKeyName"/>.
		/// </summary>
		public IReadOnlyTable ReferencingTable => Table1;

		/// <summary>
		/// The 'right' table in the join, containing the <see cref="ReferencedKeyName"/>.
		/// </summary>
		public IReadOnlyTable ReferencedTable => Table2;
	}
}
