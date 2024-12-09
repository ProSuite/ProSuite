using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[TableTransformer]
	public class TrMakeTable : ITableTransformer<IReadOnlyTable>
	{
		[NotNull] private IReadOnlyTable _baseTable;

		private string _viewOrTableName;

		private IQueryDescription _queryDescription;

		private List<IReadOnlyTable> _involvedTables;

		[DocTr(nameof(DocTrStrings.TrMakeTable_0))]
		public TrMakeTable(
			[NotNull] [DocTr(nameof(DocTrStrings.TrMakeTable_baseTable))]
			IReadOnlyTable baseTable,
			[NotNull] [DocTr(nameof(DocTrStrings.TrMakeTable_viewOrTableName))]
			string viewOrTableName)
		{
			InitializeViewOrTableName(baseTable, viewOrTableName);
		}

		[DocTr(nameof(DocTrStrings.TrMakeTable_1))]
		public TrMakeTable(
			[NotNull] [DocTr(nameof(DocTrStrings.TrMakeTable_baseTable))]
			IReadOnlyTable baseTable,
			[NotNull] [DocTr(nameof(DocTrStrings.TrMakeTable_sql))]
			string sql,
			[NotNull] [DocTr(nameof(DocTrStrings.TrMakeTable_objectIdField))]
			string objectIdField)
		{
			InitializeSqlQuery(baseTable, sql, objectIdField);
		}

		[InternallyUsedTest]
		public TrMakeTable(
			[NotNull] TrMakeTableDefinition definition)

		{
			if (definition.Sql != null && definition.ObjectIdField != null)
			{
				InitializeSqlQuery((IReadOnlyTable) definition.BaseTable, definition.Sql,
				                   definition.ObjectIdField);
			}
			else if (definition.ViewOrTableName != null)
			{
				InitializeViewOrTableName((IReadOnlyTable) definition.BaseTable,
				                          definition.ViewOrTableName);
			}
			else
			{
				throw new ArgumentException(
					"Invalid parameter combination either sql and ObjectIdField must not be null or ViewOrTableName must not be null.");
			}
		}

		private void InitializeViewOrTableName(IReadOnlyTable baseTable, string viewOrTableName)
		{
			_baseTable = baseTable;
			_viewOrTableName = viewOrTableName;
			_involvedTables = new List<IReadOnlyTable> { baseTable };
		}

		private void InitializeSqlQuery(IReadOnlyTable baseTable, string sql, string objectIdField)
		{
			_baseTable = baseTable;

			ISqlWorkspace sqlWorkspace = baseTable.Workspace as ISqlWorkspace;

			if (sqlWorkspace == null)
			{
				throw new NotSupportedException(
					$"The workspace of {baseTable.Name} does not support query classes");
			}

			_queryDescription =
				DatasetUtils.CreateQueryDescription(sqlWorkspace, sql, objectIdField);

			_involvedTables = new List<IReadOnlyTable> { baseTable };
		}

		#region Implementation of IInvolvesTables

		public IList<IReadOnlyTable> InvolvedTables => _involvedTables;

		public void SetConstraint(int tableIndex, string condition)
		{
			// Not applicable
			throw new InvalidOperationException(
				"TrMakeTable does not support filter conditions on the base table.");
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
			return _queryDescription != null ? CreateQueryLayerClass() : OpenExistingTable();
		}

		private IReadOnlyTable OpenExistingTable()
		{
			Assert.NotNullOrEmpty(_viewOrTableName, "Table or view name not defined");

			IWorkspace workspace = _baseTable.Workspace;

			ITable resultTable = DatasetUtils.OpenTable(workspace, _viewOrTableName);

			GdbTable wrappedResult = resultTable is IFeatureClass featureClass
				                         ? new GdbFeatureClass(featureClass, true)
				                         : new GdbTable(resultTable, true);

			// Wrap to allow assigning a custom name:
			wrappedResult.Rename(TransformerName);

			return wrappedResult;
		}

		private IReadOnlyTable CreateQueryLayerClass()
		{
			ISqlWorkspace sqlWorksapce = (ISqlWorkspace) _baseTable.Workspace;

			if (_baseTable is IReadOnlyFeatureClass baseFeatureClass &&
			    _queryDescription.IsSpatialQuery)
			{
				_queryDescription.SpatialReference = baseFeatureClass.SpatialReference;
				_queryDescription.GeometryType = baseFeatureClass.ShapeType;
			}

			ITable queryTable = DatasetUtils.CreateQueryLayerClass(
				sqlWorksapce, _queryDescription, TransformerName);

			// Wrap to allow assigning a custom name, rather than <currentUser>.%<assignedName>
			GdbTable wrappedResult = queryTable is IFeatureClass featureClass
				                         ? new GdbFeatureClass(featureClass, true)
				                         : new GdbTable(queryTable, true);

			wrappedResult.Rename(TransformerName);

			return wrappedResult;
		}

		public string TransformerName { get; set; }

		#endregion
	}
}
