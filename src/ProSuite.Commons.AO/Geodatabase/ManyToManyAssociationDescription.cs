using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class ManyToManyAssociationDescription : AssociationDescription
	{
		private readonly string _table1KeyName;
		private readonly string _table2KeyName;
		private readonly ITable _associationTable;
		private readonly string _associationTableKey1;
		private readonly string _associationTableKey2;

		public ManyToManyAssociationDescription(
			[NotNull] ITable table1,
			[NotNull] string table1KeyName,
			[NotNull] ITable table2,
			[NotNull] string table2KeyName,
			[NotNull] ITable associationTable,
			[NotNull] string associationTableKey1,
			[NotNull] string associationTableKey2)
			: base(table1, table2)
		{
			_table1KeyName = table1KeyName;
			_table2KeyName = table2KeyName;
			_associationTable = associationTable;
			_associationTableKey1 = associationTableKey1;
			_associationTableKey2 = associationTableKey2;
		}

		[NotNull]
		public string Table1KeyName => _table1KeyName;

		[NotNull]
		public string Table2KeyName => _table2KeyName;

		[NotNull]
		public ITable AssociationTable => _associationTable;

		[NotNull]
		public string AssociationTableKey1 => _associationTableKey1;

		[NotNull]
		public string AssociationTableKey2 => _associationTableKey2;
	}
}
