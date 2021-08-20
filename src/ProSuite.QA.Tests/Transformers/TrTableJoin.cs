using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	public class TrTableJoin : ITableTransformer<ITable>
	{
		private readonly ITable _t0;
		private readonly ITable _t1;
		private readonly string _relationName;
		private readonly JoinType _joinType;
		private readonly List<ITable> _involved;

		private ITable _joined;

		public TrTableJoin([NotNull] ITable t0, [NotNull] ITable t1,
		                   [NotNull] string relationName, JoinType joinType)
		{
			_t0 = t0;
			_t1 = t1;
			_relationName = relationName;
			_joinType = joinType;
			_involved = new List<ITable> {t0, t1};
		}

		IList<ITable> IInvolvesTables.InvolvedTables => _involved;

		public ITable GetTransformed()
		{
			if (_joined == null)
			{
				IRelationshipClass relClass =
					((IFeatureWorkspace) ((IDataset) _t0).Workspace).OpenRelationshipClass(
						_relationName);
				_joined = RelationshipClassUtils.GetQueryTable(
					relClass, _involved, _joinType, whereClause: null);
			}

			return _joined;
		}

		object ITableTransformer.GetTransformed() => GetTransformed();

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
