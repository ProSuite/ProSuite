using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	[CLSCompliant(false)]
	public class TableNameColumnInfo : ColumnInfo
	{
		private readonly bool _qualified;

		public TableNameColumnInfo([NotNull] ITable table,
		                           [NotNull] string columnName,
		                           bool qualified = false) :
			base(table, columnName, typeof(string))
		{
			_qualified = qualified;
		}

		public override IEnumerable<string> BaseFieldNames => new string[] { };

		protected override object ReadValueCore(IRow row)
		{
			return _qualified
				       ? DatasetUtils.GetName(Table)
				       : DatasetUtils.GetUnqualifiedName(Table);
		}
	}
}