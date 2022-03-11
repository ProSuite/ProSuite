using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	public class TrTableJoin : ITableTransformer<IReadOnlyTable>
	{
		private readonly IReadOnlyTable _t0;
		private readonly IReadOnlyTable _t1;
		private readonly string _relationName;
		private readonly JoinType _joinType;
		private readonly List<IReadOnlyTable> _involved;

		private IReadOnlyTable _joined;
		private string _transformerName;

		[Doc(nameof(DocStrings.TrTableJoin_0))]
		public TrTableJoin(
			[NotNull] [Doc(nameof(DocStrings.TrTableJoin_t0))]
			IReadOnlyTable t0,
			[NotNull] [Doc(nameof(DocStrings.TrTableJoin_t1))]
			IReadOnlyTable t1,
			[NotNull] [Doc(nameof(DocStrings.TrTableJoin_relationName))]
			string relationName,
			[Doc(nameof(DocStrings.TrTableJoin_joinType))]
			JoinType joinType)
		{
			_t0 = t0;
			_t1 = t1;
			_relationName = relationName;
			_joinType = joinType;
			_involved = new List<IReadOnlyTable> { t0, t1 };
		}

		IList<IReadOnlyTable> IInvolvesTables.InvolvedTables => _involved;

		public IReadOnlyTable GetTransformed()
		{
			if (_joined == null)
			{
				IRelationshipClass relClass =
					((IFeatureWorkspace) _t0.Workspace).OpenRelationshipClass(
						_relationName);
				_joined = RelationshipClassUtils.GetQueryTable(
					relClass, _involved, _joinType, whereClause: null,
					queryTableName: _transformerName);
			}

			return _joined;
		}

		object ITableTransformer.GetTransformed() => GetTransformed();

		string ITableTransformer.TransformerName
		{
			get => _transformerName;
			set => _transformerName = value;
		}

		void IInvolvesTables.SetConstraint(int tableIndex, string condition)
		{
			// TODO
		}

		void IInvolvesTables.SetSqlCaseSensitivity(int tableIndex, bool useCaseSensitiveQaSql)
		{
			//TODO
		}
	}
}
