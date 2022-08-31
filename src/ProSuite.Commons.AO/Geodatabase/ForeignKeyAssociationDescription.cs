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

		/// <summary>
		/// Whether the association is a declared 1:1 association. It will be assumed that both
		/// keys are unique and both tables can be used as primary key table. This increases the
		/// flexibility in the choice of ID for outer joins.
		/// </summary>
		public bool HasOneToOneCardinality { get; set; }

		#region Overrides of Object

		public override string ToString()
		{
			string associationName = HasOneToOneCardinality ? "One-to-One" : "Many-to-One";

			return
				$"{associationName} association between referencing table {ReferencingTable.Name} " +
				$"and referenced table {ReferencedTable.Name} using referencing (foreign) key " +
				$"{ReferencingKeyName} and referenced (primary) key {ReferencedKeyName}.";
		}

		#endregion
	}
}
