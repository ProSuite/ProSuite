using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Text;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests.Network
{
	public class QaConnectionRuleHelper
	{
		private readonly DataView _countView;
		// helper object to verify an expression TODO: is there another way to verify an expression

		private readonly TableView[] _mainRuleFilterHelpers;
		private string _countFilter;
		private List<TableView> _countRuleFilterHelpers;

		private QaConnectionRuleHelper([NotNull] ICollection<ITableSchemaDef> tables)
		{
			Assert.ArgumentNotNull(tables, nameof(tables));

			_mainRuleFilterHelpers = new TableView[tables.Count];

			var countTable = new DataTable();
			countTable.Columns.Add("dummy");
			countTable.Rows.Add("dummy");
			countTable.AcceptChanges();

			_countView = new DataView(countTable);
		}

		private string CountFilter
		{
			get { return _countFilter; }
			set { _countFilter = value; }
		}

		[NotNull]
		internal TableView[] MainRuleFilterHelpers => _mainRuleFilterHelpers;

		private void AddCountRuleFilterHelper([NotNull] TableView countRuleFilterHelper)
		{
			if (_countRuleFilterHelpers == null)
			{
				_countRuleFilterHelpers = new List<TableView>();
			}

			_countRuleFilterHelpers.Add(countRuleFilterHelper);
		}

		internal bool VerifyCountRules()
		{
			if (_countRuleFilterHelpers == null)
			{
				return true;
			}

			if (CountFilter == null)
			{
				return true;
			}

			string filter = _countFilter;
			int i = 0;
			foreach (TableView tableView in _countRuleFilterHelpers)
			{
				filter = filter.Replace("{" + i + "}",
				                        tableView.FilteredRowCount.ToString(
					                        CultureInfo.InvariantCulture));
				i++;
			}

			_countView.RowFilter = filter;
			return _countView.Count == 1;
		}

		[NotNull]
		public static IList<QaConnectionRuleHelper> CreateList(
			[NotNull] IList<QaConnectionRule> rules,
			[NotNull] out TableView[] tableFilterHelpers)
		{
			IList<ITableSchemaDef> tableList = rules[0].TableList;

			foreach (QaConnectionRule rule in rules)
			{
				if (tableList != rule.TableList)
				{
					throw new InvalidOperationException(
						"All TableLists of the rules must be the same.");
				}
			}

			var result = new List<QaConnectionRuleHelper>(rules.Count);

			int tableCount = tableList.Count;

			tableFilterHelpers = new TableView[tableCount];
			var baseConditions = new StringBuilder[tableCount];

			foreach (QaConnectionRule rule in rules)
			{
				for (int i = 0; i < tableCount; i++)
				{
					string selectionExpression = rule.SelectionExpressions[i];

					if (! StringUtils.IsNotEmpty(selectionExpression))
					{
						continue;
					}

					if (baseConditions[i] == null)
					{
						baseConditions[i] = new StringBuilder(selectionExpression);
					}
					else
					{
						baseConditions[i].AppendFormat(" AND {0}", selectionExpression);
					}
				}

				if (rule.Constraint != null)
				{
					foreach (
						KeyValuePair<string, QaConnectionCountRule> pair in
						rule.CountRulesByVariableName)
					{
						int i = tableList.IndexOf(pair.Value.Table);
						baseConditions[i]
							.AppendFormat(" AND {0}", pair.Value.CountSelectionExpression);
					}
				}
			}

			string startsInLower = QaConnections.StartsIn.ToLower();

			for (int tableIndex = 0; tableIndex < tableCount; tableIndex++)
			{
				ITableSchemaDef table = tableList[tableIndex];

				string lowerCaseCondition =
					baseConditions[tableIndex].ToString().ToLower().Replace(startsInLower, "true");

				TableView tableFilterHelper =
					TableViewFactory.Create((IReadOnlyTable) table, lowerCaseCondition);
				tableFilterHelper.Constraint = null;

				if (((IReadOnlyFeatureClass) table).ShapeType ==
				    esriGeometryType.esriGeometryPolyline)
				{
					tableFilterHelper.AddColumn(QaConnections.StartsIn, typeof(bool));
				}

				tableFilterHelpers[tableIndex] = tableFilterHelper;
			}

			foreach (QaConnectionRule rule in rules)
			{
				var newRuleHelper = new QaConnectionRuleHelper(tableList);

				for (int i = 0; i < tableCount; i++)
				{
					string selectionExpression = rule.SelectionExpressions[i];

					if (! StringUtils.IsNotEmpty(selectionExpression))
					{
						continue;
					}

					TableView filterHelper = tableFilterHelpers[i].Clone();

					filterHelper.Constraint = selectionExpression;
					newRuleHelper.MainRuleFilterHelpers[i] = filterHelper;
				}

				if (rule.Constraint != null)
				{
					string constraint = rule.Constraint;
					int i = 0;
					foreach (
						KeyValuePair<string, QaConnectionCountRule> pair in
						rule.CountRulesByVariableName)
					{
						constraint = constraint.Replace(pair.Key, "{" + i + "}");

						int tableIndex = tableList.IndexOf(pair.Value.Table);
						TableView helper = tableFilterHelpers[tableIndex].Clone();
						helper.Constraint = pair.Value.CountSelectionExpression;

						newRuleHelper.AddCountRuleFilterHelper(helper);
						i++;
					}

					newRuleHelper.CountFilter = constraint;
				}

				result.Add(newRuleHelper);
			}

			return result;
		}
	}
}
