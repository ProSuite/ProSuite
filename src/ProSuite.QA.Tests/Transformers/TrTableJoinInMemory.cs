using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	public class TrTableJoinInMemory : ITableTransformer<IReadOnlyTable>
	{
		private readonly IReadOnlyTable _t0;
		private readonly IReadOnlyTable _t1;
		private readonly string _relationName;
		private readonly JoinType _joinType;
		private readonly List<IReadOnlyTable> _involvedTables;

		private IReadOnlyTable _joinedTable;
		private string _transformerName;

		[DocTr(nameof(DocTrStrings.TrTableJoinInMemory_0))]
		public TrTableJoinInMemory(
			[NotNull] [DocTr(nameof(DocTrStrings.TrTableJoinInMemory_t0))]
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
			_involvedTables = new List<IReadOnlyTable> {t0, t1};
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

		object ITableTransformer.GetTransformed()
		{
			return GetTransformed();
		}

		public IReadOnlyTable GetTransformed()
		{
			if (_joinedTable == null)
			{
				IWorkspace ws = _involvedTables.Select(x => x.Workspace).FirstOrDefault();

				Assert.NotNull(ws);
				List<ITable> involved = new List<ITable>();
				_involvedTables.ForEach(
					x => involved.Add(((IFeatureWorkspace) ws).OpenTable(x.Name)));

				IRelationshipClass relClass =
					((IFeatureWorkspace) _t0.Workspace).OpenRelationshipClass(
						_relationName);

				IFeatureClass geometryEndClass =
					((IFeatureWorkspace) ws).OpenTable(_t0.Name) as IFeatureClass;

				Assert.NotNull(geometryEndClass, "First table must be a feature class");

				_joinedTable =
					TableJoinUtils.CreateJoinedGdbFeatureClass(
						relClass, geometryEndClass, _relationName, _joinType);
			}

			return _joinedTable;
		}

		string ITableTransformer.TransformerName
		{
			get => _transformerName;
			set => _transformerName = value;
		}

		#endregion
	}
}
