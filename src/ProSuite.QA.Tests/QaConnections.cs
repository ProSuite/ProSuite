using System.Collections.Generic;
using System.Data;
using System.Linq;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.PolygonGrower;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Network;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[InternallyUsedTest]
	public class QaConnections : QaNetworkBase
	{
		[Doc(nameof(DocStrings.QaConnections_StartsIn))]
		public static readonly string StartsIn = "_StartsIn";

		private TableView[] _tableFilterHelpers;
		private IList<QaConnectionRuleHelper> _ruleHelpers;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string RulesNotFulfilled = "RulesNotFulfilled";

			public Code() : base("LineConnections") { }
		}

		#endregion

		#region Constructors

		[Doc(nameof(DocStrings.QaConnections_0))]
		public QaConnections(
			[Doc(nameof(DocStrings.QaConnections_featureClasses))]
			IList<IReadOnlyFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaConnections_rules_0))]
			IList<string[]> rules)
			: base(CastToTables((IEnumerable<IReadOnlyFeatureClass>) featureClasses), false)
		{
			Init(rules);
		}

		[Doc(nameof(DocStrings.QaConnections_1))]
		public QaConnections(
			[Doc(nameof(DocStrings.QaConnections_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaConnections_rules_1))]
			IEnumerable<string> rules)
			: base(featureClass, false)
		{
			List<string[]> ruleArrays = rules.Select(rule => new[] {rule}).ToList();

			Init(ruleArrays);
		}

		[Doc(nameof(DocStrings.QaConnections_2))]
		public QaConnections(
				[Doc(nameof(DocStrings.QaConnections_featureClasses))]
				IList<IReadOnlyFeatureClass> featureClasses,
				[Doc(nameof(DocStrings.QaConnections_rules_1))]
				IList<QaConnectionRule> rules)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, rules, 0) { }

		[Doc(nameof(DocStrings.QaConnections_3))]
		public QaConnections(
			[Doc(nameof(DocStrings.QaConnections_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaConnections_rules_1))]
			IList<QaConnectionRule> rules)
			: this(new[] {featureClass}, rules) { }

		// TODO document/private?
		public QaConnections(
			IList<IReadOnlyFeatureClass> featureClasses,
			IList<QaConnectionRule> rules,
			double tolerance)
			: base(
				CastToTables((IEnumerable<IReadOnlyFeatureClass>) featureClasses), tolerance, false,
				null)
		{
			_ruleHelpers = QaConnectionRuleHelper.CreateList(rules, out _tableFilterHelpers);
		}

		[InternallyUsedTest]
		public QaConnections([NotNull] QaConnectionsDefinition definition)
			: this(definition.FeatureClasses.Cast<IReadOnlyFeatureClass>().ToList(),
			       definition.Rules, definition.Tolerance) { }

		#endregion

		protected override int CompleteTileCore(TileInfo args)
		{
			int errorCount = base.CompleteTileCore(args);

			if (ConnectedElementsList == null)
			{
				return errorCount;
			}

			return errorCount + ConnectedElementsList.Sum(
				       connectedRows => CheckRows(connectedRows));
		}

		protected override void ConfigureQueryFilter(int tableIndex,
		                                             ITableFilter queryFilter)
		{
			base.ConfigureQueryFilter(tableIndex, queryFilter);
			IReadOnlyTable table = InvolvedTables[tableIndex];

			foreach (QaConnectionRuleHelper ruleHelper in _ruleHelpers)
			{
				string constraint = ruleHelper.MainRuleFilterHelpers[tableIndex]?.Constraint;
				if (constraint == null)
				{
					continue;
				}

				foreach (
					string fieldName in ExpressionUtils.GetExpressionFieldNames(table, constraint))
				{
					queryFilter.AddField(fieldName); // .AddField checks for multiple entries !
				}
			}
		}

		private void Init([NotNull] ICollection<string[]> rules)
		{
			var ruleList = new List<QaConnectionRule>(rules.Count);

			foreach (string[] rule in rules)
			{
				List<ITableSchemaDef> tables = InvolvedTables.Cast<ITableSchemaDef>().ToList();
				ruleList.Add(new QaConnectionRule(tables, rule));
			}

			_ruleHelpers = QaConnectionRuleHelper.CreateList(ruleList, out _tableFilterHelpers);
		}

		private int CheckRows([NotNull] IList<NetElement> connectedElements)
		{
			int tableCount = _tableFilterHelpers.Length;
			for (int i = 0; i < tableCount; i++)
			{
				_tableFilterHelpers[i].ClearRows();
			}

			int connectedElementsCount = connectedElements.Count;

			var connectedRows = new List<IReadOnlyRow>(connectedElementsCount);
			var connectedRowTableIndices = new List<int>(connectedElementsCount);

			foreach (NetElement netElement in connectedElements)
			{
				TableIndexRow row = netElement.Row;

				connectedRows.Add(row.Row);

				int tableIndex = row.TableIndex;
				connectedRowTableIndices.Add(tableIndex);

				TableView baseHelper = _tableFilterHelpers[tableIndex];

				DataRow helperRow = baseHelper.Add(row.Row);
				Assert.NotNull(helperRow, "no row returned");

				if (netElement is DirectedRow directedRow)
				{
					helperRow[StartsIn] = ! directedRow.IsBackward;
				}
			}

			bool fulfilledRuleFound = false;
			foreach (QaConnectionRuleHelper ruleHelper in _ruleHelpers)
			{
				// check if all rows comply to the current rule 
				int matchingRowsCount = 0;
				for (int tableIndex = 0; tableIndex < tableCount; tableIndex++)
				{
					matchingRowsCount += GetMatchingRowsCount(tableIndex, ruleHelper);
				}

				Assert.True(matchingRowsCount <= connectedElementsCount,
				            "Unexpected matching rows count: {0}; total connected rows: {1}",
				            matchingRowsCount, connectedElementsCount);

				if (matchingRowsCount == connectedElementsCount && ruleHelper.VerifyCountRules())
				{
					// all rows comply to the current rule,
					// so one rule if fulfilled and no further checking needed
					fulfilledRuleFound = true;
					break;
				}
			}

			if (fulfilledRuleFound)
			{
				// TODO apply further checks?
				return NoError;
			}

			// no rule fulfills all the rows
			const string description = "Rows do not fulfill rules";
			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(connectedRows),
				connectedElements[0].NetPoint, Codes[Code.RulesNotFulfilled], null);
		}

		private int GetMatchingRowsCount(int tableIndex,
		                                 [NotNull] QaConnectionRuleHelper ruleHelper)
		{
			TableView helper = ruleHelper.MainRuleFilterHelpers[tableIndex];

			return helper?.FilteredRowCount ?? _tableFilterHelpers[tableIndex].FilteredRowCount;
		}
	}
}
