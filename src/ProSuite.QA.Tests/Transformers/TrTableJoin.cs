using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Container;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[TableTransformer]
	public class TrTableJoin : ITableTransformer<IReadOnlyTable>
	{
		private readonly IReadOnlyTable _t0;
		private readonly IReadOnlyTable _t1;
		private readonly string _relationName;
		private readonly JoinType _joinType;
		private readonly List<IReadOnlyTable> _involved;

		private IReadOnlyTable _joined;
		private string _transformerName;

		[DocTr(nameof(DocTrStrings.TrTableJoin_0))]
		public TrTableJoin(
			[NotNull] [DocTr(nameof(DocTrStrings.TrTableJoin_t0))]
			IReadOnlyTable t0,
			[NotNull] [DocTr(nameof(DocTrStrings.TrTableJoin_t1))]
			IReadOnlyTable t1,
			[NotNull] [DocTr(nameof(DocTrStrings.TrTableJoin_relationName))]
			string relationName,
			[DocTr(nameof(DocTrStrings.TrTableJoin_joinType))]
			JoinType joinType)
		{
			_t0 = t0;
			_t1 = t1;
			_relationName = relationName;
			_joinType = joinType;
			_involved = new List<IReadOnlyTable> { t0, t1 };
		}

		[InternallyUsedTest]
		public TrTableJoin(TrTableJoinDefinition definition)
			: this((IReadOnlyTable)definition.T0,
			       (IReadOnlyTable)definition.T1,
			       definition.RelationName,
			       definition.JoinType)
		{ }

		IList<IReadOnlyTable> IInvolvesTables.InvolvedTables => _involved;

		public IReadOnlyTable GetTransformed()
		{
			if (_joined == null)
			{
				IWorkspace ws = _involved.Select(x => x.Workspace).FirstOrDefault();
				Assert.NotNull(ws);
				List<ITable> involved = new List<ITable>();
				_involved.ForEach(x => involved.Add(((IFeatureWorkspace) ws).OpenTable(x.Name)));

				IRelationshipClass relClass =
					((IFeatureWorkspace) _t0.Workspace).OpenRelationshipClass(
						_relationName);
				_joined = RelationshipClassUtils.GetReadOnlyQueryTable(
					relClass, involved, _joinType, whereClause: null,
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
			throw new NotImplementedException(
				"Advice: Apply constraint where at usages of this transformer");
		}

		void IInvolvesTables.SetSqlCaseSensitivity(int tableIndex, bool useCaseSensitiveQaSql)
		{
			//TODO
		}
	}
}
