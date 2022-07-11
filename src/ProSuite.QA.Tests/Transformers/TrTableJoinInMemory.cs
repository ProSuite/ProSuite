using System;
using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
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

		private IReadOnlyTable _joinedTable;

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
		[Doc(nameof(DocTrStrings.TrTableJoinInMemory_manyToManyTable))]
		public IReadOnlyTable ManyToManyTable
		{
			get => _manyToManyTable;
			set => _manyToManyTable = value;
		}

		[TestParameter]
		[Doc(nameof(DocTrStrings.TrTableJoinInMemory_manyToManyTableLeftKey))]
		public string ManyToManyTableLeftKey
		{
			get => _manyToManyTableLeftKey;
			set => _manyToManyTableLeftKey = StringUtils.IsNullOrEmptyOrBlank(value) ? null : value;
		}

		[TestParameter]
		[Doc(nameof(DocTrStrings.TrTableJoinInMemory_manyToManyTableRightKey))]
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
			if (_joinedTable == null)
			{
				AssociationDescription association = CreateAssociationDescription();

				string joinTableName = ((ITableTransformer) this).TransformerName;

				_joinedTable = TableJoinUtils.CreateJoinedGdbFeatureClass(
					association, _leftTable, joinTableName, _joinType);
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

		#endregion
	}
}
