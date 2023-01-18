using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class FieldMappingRowProxy : RowProxy
	{
		[NotNull] private readonly IDictionary<int, int> _mapping;

		/// <summary>
		/// Initializes a new instance of the <see cref="FieldMappingRowProxy"/> class.
		/// </summary>
		/// <param name="baseRow">The base row.</param>
		/// <param name="fieldMapping">The field mapping.</param>
		/// <param name="table">The table.</param>
		/// <param name="oid">The oid.</param>
		public FieldMappingRowProxy([NotNull] IRow baseRow,
		                            [NotNull] IDictionary<int, int> fieldMapping,
		                            [NotNull] ITable table,
		                            long oid)
			: base(table, oid)
		{
			Assert.ArgumentNotNull(baseRow, nameof(baseRow));
			Assert.ArgumentNotNull(fieldMapping, nameof(fieldMapping));

			BaseRow = baseRow;
			_mapping = fieldMapping;
		}

		[NotNull]
		public IRow BaseRow { get; }

		protected override object GetValueCore(int fieldIndex)
		{
			return BaseRow.Value[_mapping[fieldIndex]];
		}
	}
}
