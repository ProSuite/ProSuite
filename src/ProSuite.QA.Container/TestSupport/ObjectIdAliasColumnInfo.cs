using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.QA.Container.TestSupport
{
	public class ObjectIdAliasColumnInfo : ColumnInfo
	{
		private readonly List<string> _baseFieldNames = new List<string>();

		public ObjectIdAliasColumnInfo([NotNull] IReadOnlyTable table,
		                               [NotNull] string columnName)
			: base(table, columnName, typeof(int))
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(columnName, nameof(columnName));

			string fieldName = table.OIDFieldName;

			if (! StringUtils.IsNullOrEmptyOrBlank(fieldName))
			{
				_baseFieldNames.Add(fieldName);
			}
		}

		public override IEnumerable<string> BaseFieldNames => _baseFieldNames;

		protected override object ReadValueCore(IReadOnlyRow row)
		{
			return row.HasOID ? (object) row.OID : DBNull.Value;
		}
	}
}
