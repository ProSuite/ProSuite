using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	public class TrMakeTable : ITableTransformer<IReadOnlyTable>
	{
		[NotNull] private readonly IReadOnlyTable _baseTable;
		[NotNull] private readonly string _viewOrTableName;
		private readonly List<IReadOnlyTable> _involvedTables;

		[DocTr(nameof(DocTrStrings.TrMakeTable_0))]
		public TrMakeTable(
			[NotNull] [DocTr(nameof(DocTrStrings.TrMakeTabl_baseTable))]
			IReadOnlyTable baseTable,
			[NotNull] [DocTr(nameof(DocTrStrings.TrMakeTabl_viewOrTableName))]
			string viewOrTableName)
		{
			_baseTable = baseTable;
			_viewOrTableName = viewOrTableName;
			_involvedTables = new List<IReadOnlyTable> {baseTable};
		}

		#region Implementation of IInvolvesTables

		public IList<IReadOnlyTable> InvolvedTables => _involvedTables;

		public void SetConstraint(int tableIndex, string condition)
		{
			// Not applicable
		}

		public void SetSqlCaseSensitivity(int tableIndex, bool useCaseSensitiveQaSql)
		{
			// Not applicable
		}

		#endregion

		#region Implementation of ITableTransformer

		object ITableTransformer.GetTransformed()
		{
			return GetTransformed();
		}

		public IReadOnlyTable GetTransformed()
		{
			IReadOnlyTable baseTable = _involvedTables[0];
			IWorkspace workspace = baseTable.Workspace;

			ITable resultTable = DatasetUtils.OpenTable(workspace, _viewOrTableName);

			return ReadOnlyTableFactory.Create(resultTable);
		}

		string ITableTransformer.TransformerName { get; set; }

		#endregion
	}
}
