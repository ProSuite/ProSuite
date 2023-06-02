using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.GeoDb
{
	public class TableField : ITableField
	{
		public TableField([NotNull] string name,
		                  FieldType fieldType,
		                  int fieldLength = -1)
		{
			Name = name;
			FieldType = fieldType;
			FieldLength = fieldLength;
		}

		public string Name { get; }

		public FieldType FieldType { get; }

		public int FieldLength { get; }
	}
}
