using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	public class TrTableJoinInMemory : ITableTransformer<IReadOnlyTable>
	{
		private readonly IReadOnlyTable _leftTable;
		private readonly IReadOnlyTable _rightTable;
		private readonly string _leftTableKey;
		private readonly string _rightTableKey;

		private IReadOnlyTable _manyToManyTable;
		private string _manyToManyTableLeftKey;
		private string _manyToManyTableRightKey;

		private readonly JoinType _joinType;
		private readonly List<IReadOnlyTable> _involvedTables;

		private GdbTable _joinedTable;

		[DocTr(nameof(DocTrStrings.TrTableJoinInMemory_0))]
		public TrTableJoinInMemory(
			[NotNull] [DocTr(nameof(DocTrStrings.TrTableJoinInMemory_leftTable))]
			IReadOnlyTable leftTable,
			[NotNull] [DocTr(nameof(DocTrStrings.TrTableJoinInMemory_rightTable))]
			IReadOnlyTable rightTable,
			[NotNull] [DocTr(nameof(DocTrStrings.TrTableJoinInMemory_leftTableKey))]
			string leftTableKey,
			[NotNull] [DocTr(nameof(DocTrStrings.TrTableJoinInMemory_rightTableKey))]
			string rightTableKey,
			[DocTr(nameof(DocTrStrings.TrTableJoinInMemory_joinType))]
			JoinType joinType)
		{
			_leftTable = leftTable;
			_rightTable = rightTable;

			_leftTableKey = leftTableKey ?? throw new ArgumentNullException(nameof(leftTableKey));
			_rightTableKey = rightTableKey;

			_joinType = joinType;
			_involvedTables = new List<IReadOnlyTable> {leftTable, rightTable};
		}

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrTableJoinInMemory_manyToManyTable))]
		public IReadOnlyTable ManyToManyTable
		{
			get => _manyToManyTable;
			set
			{
				_manyToManyTable = value;
				_involvedTables.Add(_manyToManyTable);
			}
		}

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrTableJoinInMemory_manyToManyTableLeftKey))]
		public string ManyToManyTableLeftKey
		{
			get => _manyToManyTableLeftKey;
			set => _manyToManyTableLeftKey = StringUtils.IsNullOrEmptyOrBlank(value) ? null : value;
		}

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrTableJoinInMemory_manyToManyTableRightKey))]
		public string ManyToManyTableRightKey
		{
			get => _manyToManyTableRightKey;
			set => _manyToManyTableRightKey =
				       StringUtils.IsNullOrEmptyOrBlank(value) ? null : value;
		}

		#region Implementation of IInvolvesTables

		IList<IReadOnlyTable> IInvolvesTables.InvolvedTables => _involvedTables;

		public void SetConstraint(int tableIndex, string condition)
		{
			// TODO
		}

		public void SetSqlCaseSensitivity(int tableIndex, bool useCaseSensitiveQaSql)
		{
			// TODO
		}

		#endregion

		#region Implementation of ITableTransformer

		string ITableTransformer.TransformerName { get; set; }

		object ITableTransformer.GetTransformed()
		{
			return GetTransformed();
		}

		public IReadOnlyTable GetTransformed()
		{
			// TODO: In order to use the DataContainer at least for the left rows, wrap or subclass JoinedDataset
			if (_joinedTable == null)
			{
				AssociationDescription association = CreateAssociationDescription();

				string joinTableName = ((ITableTransformer) this).TransformerName;

				_joinedTable = TableJoinUtils.CreateJoinedGdbFeatureClass(
					association, _leftTable, joinTableName, _joinType);

				// To store the involved base rows in issue:
				IField baseRowField = FieldUtils.CreateBlobField(InvolvedRowUtils.BaseRowField);
				_joinedTable.AddField(baseRowField);

				JoinedDataset joinedDataset =
					(JoinedDataset) Assert.NotNull(_joinedTable.BackingDataset);

				joinedDataset.OnRowCreatingAction = AddBaseRowsAction;
			}

			return _joinedTable;
		}

		private AssociationDescription CreateAssociationDescription()
		{
			AssociationDescription association;

			if (ManyToManyTable == null &&
			    string.IsNullOrEmpty(_manyToManyTableLeftKey) &&
			    string.IsNullOrEmpty(_manyToManyTableRightKey))
			{
				association = new ForeignKeyAssociationDescription(
					_leftTable, _leftTableKey, _rightTable, _rightTableKey);
			}
			else
			{
				if (ManyToManyTable == null ||
				    string.IsNullOrEmpty(_manyToManyTableLeftKey) ||
				    string.IsNullOrEmpty(_manyToManyTableRightKey))
				{
					throw new ArgumentNullException(
						$"Many-to-many attributes ({nameof(ManyToManyTable)}, " +
						$"{nameof(ManyToManyTableLeftKey)}, {nameof(ManyToManyTableRightKey)}) " +
						"must all be null or all specified.");
				}

				association = new ManyToManyAssociationDescription(
					_leftTable, _leftTableKey, _rightTable, _rightTableKey,
					_manyToManyTable, _manyToManyTableLeftKey, _manyToManyTableRightKey);
			}

			return association;
		}

		private Action<JoinedValueList, IReadOnlyRow, IReadOnlyRow> AddBaseRowsAction
		{
			get
			{
				int baseRowsFieldIdxResult = _joinedTable.FindField(InvolvedRowUtils.BaseRowField);

				var baseRowField = _joinedTable.Fields.Field[baseRowsFieldIdxResult];

				GdbTable extraRowTable = new GdbTable(-1, "BASE_ROW_TBL");
				int baseRowsIndex = extraRowTable.AddFieldT(baseRowField);

				return (joinedRows, leftRow, otherRow) =>
				{
					var involvedRows = new List<IReadOnlyRow>(2);

					if (leftRow != null)
					{
						involvedRows.Add(leftRow);
					}

					if (otherRow != null)
					{
						involvedRows.Add(otherRow);
					}

					VirtualRow rowContainingBaseRows = extraRowTable.CreateRow();

					IDictionary<int, int> indexMatrix = new Dictionary<int, int>(1);
					indexMatrix.Add(baseRowsFieldIdxResult, baseRowsIndex);

					joinedRows.AddRow(rowContainingBaseRows, indexMatrix);

					rowContainingBaseRows.set_Value(baseRowsIndex, involvedRows);
				};
			}
		}

		#endregion
	}
}
