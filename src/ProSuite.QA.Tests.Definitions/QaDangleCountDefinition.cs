using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[LinearNetworkTest]
	[TopologyTest]
	[UsedImplicitly]
	public class QaDangleCountDefinition : AlgorithmDefinition
	{
		public double Tolerance { get;}
		public IList<string> DangleCountExpressions { get;}
		public IList<IFeatureClassSchemaDef> PolylineClasses { get;}

		[Doc(nameof(DocStrings.QaDangleCount_0))]
		public QaDangleCountDefinition(
			[Doc(nameof(DocStrings.QaDangleCount_polylineClass))] [NotNull]
			IFeatureClassSchemaDef polylineClass,
			[Doc(nameof(DocStrings.QaDangleCount_dangleCountExpression))] [NotNull]
			string dangleCountExpression,
			[Doc(nameof(DocStrings.QaDangleCount_tolerance))]
			double tolerance)
			: this(new[] { polylineClass }, new[] { dangleCountExpression }, tolerance) { }

		[Doc(nameof(DocStrings.QaDangleCount_1))]
		public QaDangleCountDefinition(
			[Doc(nameof(DocStrings.QaDangleCount_polylineClasses))] [NotNull]
			IList<IFeatureClassSchemaDef>
				polylineClasses,
			[Doc(nameof(DocStrings.QaDangleCount_dangleCountExpressions))] [NotNull]
			IList<string>
				dangleCountExpressions,
			[Doc(nameof(DocStrings.QaDangleCount_tolerance))]
			double tolerance)
			: base(polylineClasses)
		{
			Assert.ArgumentNotNull(polylineClasses, nameof(polylineClasses));
			Assert.ArgumentNotNull(dangleCountExpressions,
			                       nameof(dangleCountExpressions));
			Assert.ArgumentCondition(
				dangleCountExpressions.Count == 1 ||
				dangleCountExpressions.Count == polylineClasses.Count,
				"The number of dangle count expressions must be either 1 or equal to the number of polyline classes");
			Assert.ArgumentCondition(tolerance >= 0, "Invalid tolerance: {0}", tolerance);
			PolylineClasses = polylineClasses;
			DangleCountExpressions = dangleCountExpressions;
			Tolerance = tolerance;
		}
	}
}
