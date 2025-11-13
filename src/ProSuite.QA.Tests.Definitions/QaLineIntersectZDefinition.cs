using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check if there are any crossing lines that are too close
	/// to each other within several line layers
	/// </summary>
	[UsedImplicitly]
	[TopologyTest]
	[ZValuesTest]
	[LinearNetworkTest]
	[IntersectionParameterTest]
	public class QaLineIntersectZDefinition : AlgorithmDefinition
	{
		private const string _zDifferenceColumn = "_ZDifference";

		public IList<IFeatureClassSchemaDef> PolylineClasses { get; }
		public double MinimumZDifference { get; }
		public double MaximumZDifference { get; }
		public string Constraint { get; }

		[Doc(nameof(DocStrings.QaLineIntersectZ_0))]
		public QaLineIntersectZDefinition(
			[Doc(nameof(DocStrings.QaLineIntersectZ_polylineClasses))]
			IList<IFeatureClassSchemaDef> polylineClasses,
			[Doc(nameof(DocStrings.QaLineIntersectZ_limit_0))]
			double limit)
			: this(polylineClasses, limit, string.Empty) { }

		[Doc(nameof(DocStrings.QaLineIntersectZ_1))]
		public QaLineIntersectZDefinition(
			[Doc(nameof(DocStrings.QaLineIntersectZ_polylineClass))]
			IFeatureClassSchemaDef polylineClass,
			[Doc(nameof(DocStrings.QaLineIntersectZ_limit_0))]
			double limit)
			: this(polylineClass, limit, string.Empty) { }

		[Doc(nameof(DocStrings.QaLineIntersectZ_2))]
		public QaLineIntersectZDefinition(
			[Doc(nameof(DocStrings.QaLineIntersectZ_polylineClass))]
			IFeatureClassSchemaDef polylineClass,
			[Doc(nameof(DocStrings.QaLineIntersectZ_limit_1))]
			double limit,
			[Doc(nameof(DocStrings.QaLineIntersectZ_constraint))]
			string constraint)
			: this(new[] { polylineClass }, limit, constraint) { }

		[Doc(nameof(DocStrings.QaLineIntersectZ_3))]
		public QaLineIntersectZDefinition(
			[Doc(nameof(DocStrings.QaLineIntersectZ_polylineClasses))]
			IList<IFeatureClassSchemaDef> polylineClasses,
			[Doc(nameof(DocStrings.QaLineIntersectZ_limit_1))]
			double limit,
			[Doc(nameof(DocStrings.QaLineIntersectZ_constraint))]
			string constraint)
			: this(polylineClasses, limit, 0, constraint) { }

		[Doc(nameof(DocStrings.QaLineIntersectZ_4))]
		public QaLineIntersectZDefinition(
			[Doc(nameof(DocStrings.QaLineIntersectZ_polylineClasses))]
			IList<IFeatureClassSchemaDef> polylineClasses,
			[Doc(nameof(DocStrings.QaLineIntersectZ_minimumZDifference))]
			double minimumZDifference,
			[Doc(nameof(DocStrings.QaLineIntersectZ_maximumZDifference))]
			double maximumZDifference,
			[Doc(nameof(DocStrings.QaLineIntersectZ_constraint))]
			string constraint)
			: base(polylineClasses)
		{
			PolylineClasses = polylineClasses;
			MinimumZDifference = minimumZDifference;
			MaximumZDifference = maximumZDifference;
			Constraint = constraint;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaLineIntersectZ_MinimumZDifferenceExpression))]
		public string MinimumZDifferenceExpression { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaLineIntersectZ_MaximumZDifferenceExpression))]
		public string MaximumZDifferenceExpression { get; set; }
	}
}
