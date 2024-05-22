using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Globalization;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Constraints;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Checks Constraints on a table
	/// </summary>
	[UsedImplicitly]
	[AttributeTest]
	public class QaConstraint : ContainerTest
	{
		[NotNull] private readonly IReadOnlyTable _table;
		private readonly string _constraint;

		private readonly bool _usesSimpleConstraint;
		private readonly int _errorDescriptionVersion;

		private TableView _simpleConstraintHelper;
		private readonly IList<ConstraintNode> _constraintNodes;
		private bool _constraintNodesInitialized;
		private string _simpleConstraintAffectedComponent;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string ErrorEvaluatingExpression = "ErrorEvaluatingExpression";
			public const string ConstraintNotFulfilled = "ConstraintNotFulfilled";

			public Code() : base("Constraints") { }
		}

		#endregion

		#region Constructors

		// TEST: Static factory method. But its probably better to directly call the last constructor
		public static QaConstraint Create(QaConstraintDefinition def)
		{
			return new QaConstraint(def);
		}

		[Doc(nameof(DocStrings.QaConstraint_0))]
		public QaConstraint(
				[Doc(nameof(DocStrings.QaConstraint_table))] [NotNull]
				IReadOnlyTable table,
				[Doc(nameof(DocStrings.QaConstraint_constraint))]
				string constraint)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, constraint, 0) { }

		[Doc(nameof(DocStrings.QaConstraint_1))]
		[InternallyUsedTest]
		public QaConstraint(
				[Doc(nameof(DocStrings.QaConstraint_table))] [NotNull]
				IReadOnlyTable table,
				[Doc(nameof(DocStrings.QaConstraint_constraints))] [NotNull]
				IList<ConstraintNode> constraints)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, constraints, 0) { }

		[Doc(nameof(DocStrings.QaConstraint_0))]
		[InternallyUsedTest]
		public QaConstraint(
			[Doc(nameof(DocStrings.QaConstraint_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaConstraint_constraint))]
			string constraint,
			int errorDescriptionVersion)
			: base(table)
		{
			_table = table;
			_constraint = constraint;
			_usesSimpleConstraint = true;
			_errorDescriptionVersion = errorDescriptionVersion;
		}

		[Doc(nameof(DocStrings.QaConstraint_1))]
		[InternallyUsedTest]
		public QaConstraint(
			[Doc(nameof(DocStrings.QaConstraint_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaConstraint_constraints))] [NotNull]
			IList<ConstraintNode> constraints,
			int errorDescriptionVersion)
			: base(table)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(constraints, nameof(constraints));

			_table = table;
			_constraintNodes = constraints;
			_usesSimpleConstraint = false;
			_errorDescriptionVersion = errorDescriptionVersion;
		}

		/// <summary>
		/// Constructor using Definition. Must be the last constructor!
		/// </summary>
		/// <param name="constraintDef"></param>
		[InternallyUsedTest]
		public QaConstraint([NotNull] QaConstraintDefinition constraintDef)
			: base(constraintDef.InvolvedTables.Cast<IReadOnlyTable>())
		{
			_table = (IReadOnlyTable) constraintDef.Table;
			_constraint = constraintDef.Constraint;

			if (constraintDef.ConstraintNodes != null)
			{
				_constraintNodes = constraintDef.ConstraintNodes.Select(c => new ConstraintNode(c))
				                                .ToList();
			}
			_usesSimpleConstraint = constraintDef.UsesSimpleConstraint;
			_errorDescriptionVersion= constraintDef.ErrorDescriptionVersion;
		}

		#endregion

		public override bool IsQueriedTable(int tableIndex)
		{
			return false;
		}

		public override bool IsGeometryUsedTable(int tableIndex)
		{
			return AreaOfInterest != null;
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			return false;
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			if (! _usesSimpleConstraint)
			{
				if (! _constraintNodesInitialized)
				{
					CreateNodeHelpers(_table, _constraintNodes, GetSqlCaseSensitivity(tableIndex));
					_constraintNodesInitialized = true;
				}

				return CheckNodes(row, _constraintNodes, new List<TableView>());
			}

			if (_simpleConstraintHelper == null)
			{
				const bool useAsConstraint = true;
				_simpleConstraintHelper = TableViewFactory.Create(_table, _constraint,
					useAsConstraint,
					GetSqlCaseSensitivity(
						tableIndex));
			}

			if (_simpleConstraintHelper.MatchesConstraint(row))
			{
				return NoError;
			}

			string description = _simpleConstraintHelper.ToString(row, constraintOnly: true);

			if (StringUtils.IsNullOrEmptyOrBlank(description))
			{
				description = _simpleConstraintHelper.Constraint?.Trim();

				if (string.IsNullOrEmpty(description))
				{
					description = "<no constraint>";
				}
			}

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row),
				GetErrorGeometry(row),
				Codes[Code.ConstraintNotFulfilled],
				GetSimpleConstraintAffectedComponent());
		}

		[CanBeNull]
		private string GetSimpleConstraintAffectedComponent()
		{
			if (_simpleConstraintAffectedComponent == null)
			{
				var uniqueFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				foreach (string fieldName in ExpressionUtils.GetExpressionFieldNames(
					         _table, _simpleConstraintHelper.Constraint ?? string.Empty))
				{
					uniqueFieldNames.Add(fieldName);
				}

				_simpleConstraintAffectedComponent = uniqueFieldNames.Count == 1
					                                     ? uniqueFieldNames.ToList()[0]
					                                     : string.Empty;
			}

			return _simpleConstraintAffectedComponent.Length > 0
				       ? _simpleConstraintAffectedComponent
				       : null;
		}

		protected override void ConfigureQueryFilter(int tableIndex,
		                                             ITableFilter queryFilter)
		{
			base.ConfigureQueryFilter(tableIndex, queryFilter);
			queryFilter.SubFields = "*";
		}

		private static void CreateNodeHelpers(
			[NotNull] IReadOnlyTable table,
			[NotNull] ICollection<ConstraintNode> constraints,
			bool caseSensitive)
		{
			string concatenatedConditions = ConcatenateConditions(constraints);

			if (! string.IsNullOrEmpty(concatenatedConditions))
			{
				const bool useAsConstraint = true;
				TableView baseHelper = TableViewFactory.Create(table, concatenatedConditions,
				                                               useAsConstraint, caseSensitive);
				baseHelper.Constraint = string.Empty;

				CreateHelpers(constraints, baseHelper);
			}
		}

		[NotNull]
		private static string ConcatenateConditions(
			[NotNull] IEnumerable<ConstraintNode> constraints)
		{
			var sb = new StringBuilder();

			ConcatenateConditions(sb, constraints);

			return sb.ToString();
		}

		private int CheckNodes([NotNull] IReadOnlyRow row,
		                       [NotNull] IEnumerable<ConstraintNode> constraintNodes,
		                       [NotNull] IList<TableView> parentHelpers)
		{
			var errorCount = 0;
			var first = true;

			foreach (ConstraintNode constraintNode in constraintNodes)
			{
				if (parentHelpers.Count == 0 && first)
				{
					constraintNode.Helper.ClearRows();
					try
					{
						constraintNode.Helper.Add(row);
					}
					catch (Exception e)
					{
						// error evaluating expression. May be a reportable
						// data error (e.g. a CONVERT() function failing because
						// of an un-convertable input type)
						string description = string.Format(
							"Error evaluating expression: {0}", e.Message);
						errorCount += ReportError(description,
						                          InvolvedRowUtils.GetInvolvedRows(row),
						                          GetErrorGeometry(row),
						                          Codes[Code.ErrorEvaluatingExpression],
						                          constraintNode.AffectedComponent);

						return errorCount;
					}

					first = false;
				}

				bool valid = CheckNode(constraintNode);

				if (valid && constraintNode.Nodes.Count > 0)
				{
					parentHelpers.Add(constraintNode.Helper);
					errorCount += CheckNodes(row, constraintNode.Nodes, parentHelpers);

					parentHelpers.RemoveAt(parentHelpers.Count - 1);
				}
				else if (! valid && constraintNode.Nodes.Count == 0)
				{
					string description = GetErrorDescription(row, parentHelpers,
					                                         constraintNode.Helper,
					                                         constraintNode.Description);

					IssueCode issueCode = constraintNode.IssueCode ??
					                      Codes[Code.ConstraintNotFulfilled];

					object[] values = { GetFieldValues(row, constraintNode.Helper, parentHelpers) };

					errorCount += ReportError(
						description, InvolvedRowUtils.GetInvolvedRows(row),
						GetErrorGeometry(row),
						issueCode, constraintNode.AffectedComponent, values: values);
				}
			}

			return errorCount;
		}

		private static bool CheckNode([NotNull] ConstraintNode constraintNode)
		{
			TableView tableView = constraintNode.Helper;

			bool caseSensitive = tableView.CaseSensitive;
			var resetCaseSensitivity = false;

			if (constraintNode.CaseSensitivityOverride != null)
			{
				tableView.CaseSensitive = constraintNode.CaseSensitivityOverride.Value;
				resetCaseSensitivity = true;
			}

			try
			{
				return tableView.FilteredRowCount == 1;
			}
			finally
			{
				if (resetCaseSensitivity)
				{
					tableView.CaseSensitive = caseSensitive;
				}
			}
		}

		[CanBeNull]
		private IGeometry GetErrorGeometry([NotNull] IReadOnlyRow row)
		{
			return TestUtils.GetInvolvedShapeCopy(row);
		}

		[NotNull]
		private string GetErrorDescription(
			[NotNull] IReadOnlyRow row,
			[NotNull] ICollection<TableView> parentHelpers,
			[NotNull] TableView filterHelper,
			[CanBeNull] string constraintDescription)
		{
			var sb = new StringBuilder();

			bool useOldFormat = _errorDescriptionVersion == 0 &&
			                    string.IsNullOrEmpty(constraintDescription);

			if (useOldFormat)
			{
				// TODO remove this, once Allowed Errors don't rely on the description for invalidation
				// >>>
				if (row.HasOID && row.Table.HasOID) // ESRI Bug in TableQueryName
				{
					sb.Append("OID = " + row.OID + ": ");
				}

				sb.AppendFormat(parentHelpers.Count > 0
					                ? "Invalid value combination:"
					                : "Invalid value:");
				// <<<
			}
			else
			{
				if (! string.IsNullOrEmpty(constraintDescription))
				{
					sb.AppendFormat("{0} - ", constraintDescription);
				}

				// TODO "Invalid value:" if only one field is involved
				// -> change this only after allowed error comparison is independent of error description
				sb.AppendFormat("Invalid value combination:");
			}

			AppendFieldValues(row, sb, filterHelper, parentHelpers);

			return sb.ToString();
		}

		private static void AppendFieldValues(
			[NotNull] IReadOnlyRow row,
			[NotNull] StringBuilder sb,
			[NotNull] TableView filterHelper,
			[NotNull] IEnumerable<TableView> parentHelpers)
		{
			var addedConstraintFieldNames =
				new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			const bool constraintOnly = true;

			foreach (TableView helper in parentHelpers)
			{
				sb.AppendFormat(" {0};",
				                helper.ToString(row, constraintOnly,
				                                addedConstraintFieldNames));
			}

			sb.AppendFormat(" {0}",
			                filterHelper.ToString(row, constraintOnly,
			                                      addedConstraintFieldNames));
		}

		[NotNull]
		private static string GetFieldValues(
			[NotNull] IReadOnlyRow row,
			[NotNull] TableView filterHelper,
			[NotNull] IEnumerable<TableView> parentHelpers)
		{
			return CultureInfoUtils.ExecuteUsing(
				CultureInfo.InvariantCulture,
				() =>
				{
					var sb = new StringBuilder();
					AppendFieldValues(row, sb, filterHelper, parentHelpers);
					return sb.ToString().Trim();
				});
		}

		private static void ConcatenateConditions(
			[NotNull] StringBuilder sb,
			[NotNull] IEnumerable<ConstraintNode> constraintNodes)
		{
			foreach (ConstraintNode constraintNode in constraintNodes)
			{
				if (sb.Length > 0)
				{
					sb.Append(" AND ");
				}

				sb.Append(constraintNode.Condition);

				ConcatenateConditions(sb, constraintNode.Nodes);
			}
		}

		private static void CreateHelpers(
			[NotNull] IEnumerable<ConstraintNode> constraintNodes,
			[NotNull] TableView baseHelper)
		{
			foreach (ConstraintNode constraintNode in constraintNodes)
			{
				TableView filter = baseHelper.Clone();

				filter.Constraint = constraintNode.Condition;
				constraintNode.Helper = filter;

				CreateHelpers(constraintNode.Nodes, baseHelper);
			}
		}
	}
}
