using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Transformers
{
	public class JoinedRowFactoryWithBaseRow : JoinedRowFactory
	{
		private readonly GdbTable _baseRowRowTable;
		private readonly int _baseRowIndex;
		private readonly IDictionary<int, int> _indexLookup;

		public JoinedRowFactoryWithBaseRow(
			GdbTable joinedSchema,
			IReadOnlyTable geometryEndClass,
			IReadOnlyTable otherEndClass)
			: base(joinedSchema, geometryEndClass, otherEndClass)
		{
			int baseRowsFieldIdxResult = JoinedSchema.FindField(InvolvedRowUtils.BaseRowField);

			var baseRowField = JoinedSchema.Fields.Field[baseRowsFieldIdxResult];

			_baseRowRowTable = new GdbTable(null, "BASE_ROW_TBL");
			_baseRowIndex = _baseRowRowTable.AddFieldT(baseRowField);

			_indexLookup = new Dictionary<int, int>(1);
			_indexLookup.Add(baseRowsFieldIdxResult, _baseRowIndex);
		}

		#region Overrides of JoinedRowFactory

		protected override void CreatingRowCore(MultiListValues joinedValueList,
		                                        IReadOnlyRow leftRow,
		                                        IReadOnlyRow otherRow)
		{
			var involvedRows = new List<IReadOnlyRow>(2)
			                   {
				                   leftRow,
				                   otherRow
			                   };

			VirtualRow rowContainingBaseRows = _baseRowRowTable.CreateRow();

			joinedValueList.AddRow(rowContainingBaseRows, _indexLookup);

			rowContainingBaseRows.set_Value(_baseRowIndex, involvedRows);
		}

		#endregion
	}
}
