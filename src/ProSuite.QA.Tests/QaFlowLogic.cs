using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
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

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check if there is always exactly one outgoing vertex
	/// </summary>
	[UsedImplicitly]
	[LinearNetworkTest]
	public class QaFlowLogic : QaNetworkBase
	{
		private readonly IList<string> _flipExpressions;
		private readonly bool _allowMultipleOutgoingLines;
		private IList<RowCondition> _flipConditions;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string OutgoingLines_None = "OutgoingLines.None";
			public const string OutgoingLines_Multiple = "OutgoingLines.Multiple";

			public Code() : base("FlowLogic") { }
		}

		#endregion

		#region constructors

		[Doc(nameof(DocStrings.QaFlowLogic_0))]
		public QaFlowLogic(
			[Doc(nameof(DocStrings.QaFlowLogic_polylineClass))]
			IReadOnlyFeatureClass polylineClass)
			: this(new[] {polylineClass}) { }

		[Doc(nameof(DocStrings.QaFlowLogic_1))]
		public QaFlowLogic(
				[Doc(nameof(DocStrings.QaFlowLogic_polylineClasses))]
				IList<IReadOnlyFeatureClass> polylineClasses)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polylineClasses, null, false) { }

		[Doc(nameof(DocStrings.QaFlowLogic_2))]
		public QaFlowLogic(
				[Doc(nameof(DocStrings.QaFlowLogic_polylineClasses))]
				IList<IReadOnlyFeatureClass> polylineClasses,
				[Doc(nameof(DocStrings.QaFlowLogic_flipExpressions))]
				IList<string> flipExpressions)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polylineClasses, flipExpressions, false) { }

		[Doc(nameof(DocStrings.QaFlowLogic_2))]
		public QaFlowLogic(
			[Doc(nameof(DocStrings.QaFlowLogic_polylineClasses))]
			IList<IReadOnlyFeatureClass> polylineClasses,
			[Doc(nameof(DocStrings.QaFlowLogic_flipExpressions))]
			IList<string> flipExpressions,
			[Doc(nameof(DocStrings.QaFlowLogic_allowMultipleOutgoingLines))]
			bool allowMultipleOutgoingLines)
			: base(CastToTables((IEnumerable<IReadOnlyFeatureClass>) polylineClasses), false)
		{
			Assert.ArgumentNotNull(polylineClasses, nameof(polylineClasses));
			Assert.ArgumentCondition(flipExpressions == null ||
			                         flipExpressions.Count <= 1 ||
			                         flipExpressions.Count == polylineClasses.Count,
			                         "The number of flip expressions must be either 0, 1 " +
			                         "(-> same flip expression used for all feature classes) " +
			                         "or else, equal to the number of feature classes)");

			if (flipExpressions != null && flipExpressions.Count > 0)
			{
				_flipExpressions = new List<string>();

				for (int tableIndex = 0; tableIndex < polylineClasses.Count; tableIndex++)
				{
					_flipExpressions.Add(flipExpressions.Count == 1
						                     ? flipExpressions[0]
						                     : flipExpressions[tableIndex]);
				}
			}

			_allowMultipleOutgoingLines = allowMultipleOutgoingLines;
		}

		[InternallyUsedTest]
		public QaFlowLogic(QaFlowLogicDefinition definition)
			: this(definition.PolylineClasses.Cast<IReadOnlyFeatureClass>()
				  .ToList(),
				  definition.FlipExpressions,
				  definition.AllowMultipleOutgoingLines
				   )
		{ }

		#endregion

		protected override void ConfigureQueryFilter(int tableIndex,
		                                             ITableFilter queryFilter)
		{
			if (_flipExpressions != null)
			{
				IReadOnlyTable table = InvolvedTables[tableIndex];

				foreach (string fieldName in
				         ExpressionUtils.GetExpressionFieldNames(table,
				                                                 _flipExpressions[tableIndex]))
				{
					// .AddField checks for multiple entries !					
					queryFilter.AddField(fieldName);
				}
			}

			base.ConfigureQueryFilter(tableIndex, queryFilter);
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			int errorCount = base.CompleteTileCore(args);
			if (ConnectedLinesList == null)
			{
				return errorCount;
			}

			foreach (List<DirectedRow> connectedRows in ConnectedLinesList)
			{
				int outgoingCount = GetOutgoingCount(connectedRows);

				if (outgoingCount == 1)
				{
					// normal allowed case
					continue;
				}

				if (outgoingCount > 1 && _allowMultipleOutgoingLines)
				{
					// multiple outgoing, but allowed
					continue;
				}

				string description = string.Format("Node has {0} outgoing lines",
				                                   outgoingCount);
				IPoint fromPoint = connectedRows[0].FromPoint;

				IssueCode issueCode = outgoingCount == 0
					                      ? Codes[Code.OutgoingLines_None]
					                      : Codes[Code.OutgoingLines_Multiple];

				errorCount += ReportError(
					description, GetInvolvedRows(connectedRows),
					fromPoint, issueCode,
					TestUtils.GetShapeFieldName(connectedRows[0].Row.Row));
			}

			return errorCount;
		}

		private int GetOutgoingCount([NotNull] IEnumerable<DirectedRow> connectedRows)
		{
			return connectedRows.Count(dirRow => ! IsBackward(dirRow));
		}

		[NotNull]
		private static InvolvedRows GetInvolvedRows(
			[NotNull] IEnumerable<DirectedRow> connectedRows)
		{
			Assert.ArgumentNotNull(connectedRows, nameof(connectedRows));

			return InvolvedRowUtils.GetInvolvedRows(
				connectedRows.Select(dirRow => dirRow.Row.Row));
		}

		private bool IsBackward([NotNull] DirectedRow dirRow)
		{
			RowCondition flipCondition = GetFlipCondition(dirRow.Row);

			return flipCondition != null && flipCondition.IsFulfilled(dirRow.Row.Row)
				       ? ! dirRow.IsBackward
				       : dirRow.IsBackward;
		}

		[CanBeNull]
		private RowCondition GetFlipCondition([NotNull] ITableIndexRow row)
		{
			if (_flipExpressions == null)
			{
				return null;
			}

			if (_flipConditions == null)
			{
				_flipConditions = CreateFlipConditions(
					_flipExpressions, InvolvedTables);
			}

			return _flipConditions.Count == 1
				       ? _flipConditions[0]
				       : _flipConditions[row.TableIndex];
		}

		[NotNull]
		private IList<RowCondition> CreateFlipConditions(
			[NotNull] IList<string> flipExpressions,
			[NotNull] IList<IReadOnlyTable> tables)
		{
			Assert.ArgumentNotNull(flipExpressions, nameof(flipExpressions));
			Assert.ArgumentNotNull(tables, nameof(tables));
			Assert.ArgumentCondition(flipExpressions.Count == tables.Count,
			                         "collection count mismatch");

			return tables.Select(
				             (t, tableIndex) => new RowCondition(
					             t,
					             flipExpressions[tableIndex],
					             undefinedConstraintIsFulfilled: false,
					             caseSensitive: GetSqlCaseSensitivity(tableIndex)))
			             .ToList();
		}
	}
}
