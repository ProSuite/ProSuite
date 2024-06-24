using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.GeoDb
{
	public class TableField : ITableField
	{
		public TableField([NotNull] string name,
		                  FieldType fieldType,
		                  int fieldLength = -1,
		                  string aliasName = null)
		{
			Name = name;
			FieldType = fieldType;
			FieldLength = fieldLength;
			AliasName = aliasName;
		}

		public string Name { get; }

		public string AliasName { get; set; }

		public FieldType FieldType { get; }

		public int FieldLength { get; }
	}
}
