using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class ManyToManyAssociationDescription : AssociationDescription
	{
		public ManyToManyAssociationDescription(
			[NotNull] IReadOnlyTable table1,
			[NotNull] string table1KeyName,
			[NotNull] IReadOnlyTable table2,
			[NotNull] string table2KeyName,
			[NotNull] IReadOnlyTable associationTable,
			[NotNull] string associationTableKey1,
			[NotNull] string associationTableKey2)
			: base(table1, table2)
		{
			Table1KeyName = table1KeyName;
			Table2KeyName = table2KeyName;
			AssociationTable = associationTable;
			AssociationTableKey1 = associationTableKey1;
			AssociationTableKey2 = associationTableKey2;
		}

		[NotNull]
		public string Table1KeyName { get; set; }

		[NotNull]
		public string Table2KeyName { get; set; }

		/// <summary>
		/// The 'bridge' table connecting Table1 and Table2 in an m:n join.
		/// </summary>
		[NotNull]
		public IReadOnlyTable AssociationTable { get; set; }

		[NotNull]
		public string AssociationTableKey1 { get; set; }

		[NotNull]
		public string AssociationTableKey2 { get; set; }

		#region Overrides of Object

		public override string ToString()
		{
			return
				$"Many-to-many association between {Table1.Name} and {Table2.Name} using " +
				$"association table {AssociationTable.Name}. Table1 key: {Table1KeyName}, " +
				$"Table2 key: {Table2KeyName}, Association-Table1 key: {AssociationTableKey1}, " +
				$"Association-Table2 key: {AssociationTableKey2}.";
		}

		#endregion
	}
}
