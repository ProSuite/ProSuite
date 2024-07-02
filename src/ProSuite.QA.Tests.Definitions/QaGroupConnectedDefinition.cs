using System.Collections.Generic;
using System.ComponentModel;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	// TODO:
	// - revise DocStrings for QaGroupConnected_RecheckMultiplePartIssues and QaGroupConnected_CompleteGroupsOutsideTestArea
	// - document influence of 'errorReporting' on available options (Recheck, OutsideTestArea), and vice versa
	// - control default value for 'ErrorReporting' property (ddx editor/customize)
	// - evaluate if other issue types should also be addressed using CompleteGroupsOutsideTestarea (e.g. branches --> cycles)
	//   -relevant: cycle, inside branches
	// - document which combinations of RecheckMultiplePartIssues and CompleteGroupsOutsideTestArea make sense
	// - does RecheckMultiplePartIssues *always* only reduce errors, or could additional errors be found?

	/// <summary>
	/// Check if polylines with same attributes are connected
	/// </summary>
	[UsedImplicitly]
	[LinearNetworkTest]
	public class QaGroupConnectedDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> PolylineClasses { get; }
		public IList<string> GroupBy { get; }
		public string ValueSeparator { get; }

		public ShapeAllowed AllowedShape { get; }

		public double MinimumErrorConnectionLineLength { get; }

		private const GroupErrorReporting _defaultErrorReporting =
			GroupErrorReporting.ReferToFirstPart;

		private const double _defaultIgnoreGapsLongerThan = -1;
		private const bool _defaultReportIndividualGaps = false;

		private const bool _defaultCompleteGroupsOutsideTestArea = false;

		[Doc(nameof(DocStrings.QaGroupConnected_0))]
		public QaGroupConnectedDefinition(
			[Doc(nameof(DocStrings.QaGroupConnected_polylineClass))]
			IFeatureClassSchemaDef polylineClass,
			[Doc(nameof(DocStrings.QaGroupConnected_groupBy))] [NotNull]
			IList<string> groupBy,
			[Doc(nameof(DocStrings.QaGroupConnected_allowedShape))]
			ShapeAllowed allowedShape)
			: this(new[] { polylineClass }, groupBy, null, allowedShape,
			       _defaultErrorReporting, -1) { }

		[Doc(nameof(DocStrings.QaGroupConnected_1))]
		public QaGroupConnectedDefinition(
			[Doc(nameof(DocStrings.QaGroupConnected_polylineClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> polylineClasses,
			[Doc(nameof(DocStrings.QaGroupConnected_groupBy))] [NotNull]
			IList<string> groupBy,
			[Doc(nameof(DocStrings.QaGroupConnected_valueSeparator))] [CanBeNull]
			string valueSeparator,
			[Doc(nameof(DocStrings.QaGroupConnected_allowedShape))]
			ShapeAllowed allowedShape,
			[Doc(nameof(DocStrings.QaGroupConnected_errorReporting))]
			[DefaultValue(GroupErrorReporting.ShortestGaps)]
			GroupErrorReporting errorReporting,
			[Doc(nameof(DocStrings.QaGroupConnected_minimumErrorConnectionLineLength))]
			double minimumErrorConnectionLineLength)
			: base(CastToTables(polylineClasses))
		{
			Assert.ArgumentNotNull(groupBy, nameof(groupBy));

			PolylineClasses = polylineClasses;
			GroupBy = groupBy;
			ValueSeparator = valueSeparator;
			AllowedShape = allowedShape;
			ErrorReporting = errorReporting;
			MinimumErrorConnectionLineLength = minimumErrorConnectionLineLength;
		}

		[TestParameter(_defaultReportIndividualGaps)]
		[Doc(nameof(DocStrings.QaGroupConnected_ReportIndividualGaps))]
		public bool ReportIndividualGaps { get; set; } = _defaultReportIndividualGaps;

		[TestParameter(_defaultIgnoreGapsLongerThan)]
		[Doc(nameof(DocStrings.QaGroupConnected_IgnoreGapsLongerThan))]
		public double IgnoreGapsLongerThan { get; set; } = _defaultIgnoreGapsLongerThan;

		// NOTE: currently not exposed as test parameter
		//[TestParameter]
		[Doc(nameof(DocStrings.QaGroupConnected_RecheckMultiplePartIssues))]
		public bool RecheckMultiplePartIssues { get; set; }

		[TestParameter(_defaultCompleteGroupsOutsideTestArea)]
		[Doc(nameof(DocStrings.QaGroupConnected_CompleteGroupsOutsideTestArea))]
		public bool CompleteGroupsOutsideTestArea { get; set; } =
			_defaultCompleteGroupsOutsideTestArea;

		[Doc(nameof(DocStrings.QaGroupConnected_errorReporting))]
		[DefaultValue(_defaultErrorReporting)]
		[PublicAPI]
		public GroupErrorReporting ErrorReporting { get; set; }
	}
}
