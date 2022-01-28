using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class TableNameColumnInfo : ColumnInfo
	{
		private readonly bool _qualified;

		public TableNameColumnInfo([NotNull] IReadOnlyTable table,
		                           [NotNull] string columnName,
		                           bool qualified = false) :
			base(table, columnName, typeof(string))
		{
			_qualified = qualified;
		}

		public override IEnumerable<string> BaseFieldNames => new string[] { };

		protected override object ReadValueCore(IReadOnlyRow row)
		{
			return _qualified
				       ? Table.Name
				       : DatasetUtils.GetTableName(Table.Workspace, Table.Name);
		}
	}
}
