using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Text;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[MValuesTest]
	public class QaMeasuresAtPointsDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef PointClass { get; }
		public string ExpectedMValueExpression { get; set; }
		public IList<IFeatureClassSchemaDef> LineClasses { get; }
		public double SearchDistance { get; }
		public double MTolerance { get; }
		public LineMSource LineMSource { get; }
		public bool RequireLine { get; }
		public bool IgnoreUndefinedExpectedMValue { get; }
		public string MatchExpression { get; }

		private readonly IFeatureClassSchemaDef _pointClass;
		private readonly string _expectedMValueExpression;
		private readonly LineMSource _lineMSource;
		private readonly bool _requireLine;
		private readonly bool _ignoreUndefinedExpectedMValue;
		private readonly string _matchExpression;
		private readonly double[] _mTolerance;
		private readonly int _tableCount;

		[Doc(nameof(DocStrings.QaMeasuresAtPoints_0))]
		public QaMeasuresAtPointsDefinition(
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_pointClass))] [NotNull]
			IFeatureClassSchemaDef pointClass,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_expectedMValueExpression))] [CanBeNull]
			string expectedMValueExpression,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_lineClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> lineClasses,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_searchDistance))]
			double searchDistance,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_mTolerance))]
			double mTolerance,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_lineMSource))]
			LineMSource lineMSource,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_requireLine))]
			bool requireLine)
			: this(
				pointClass, expectedMValueExpression, lineClasses, searchDistance, mTolerance,
				// ReSharper disable once IntroduceOptionalParameters.Global
				lineMSource, requireLine, false, null) { }

		[Doc(nameof(DocStrings.QaMeasuresAtPoints_0))]
		public QaMeasuresAtPointsDefinition(
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_pointClass))] [NotNull]
			IFeatureClassSchemaDef pointClass,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_expectedMValueExpression))] [CanBeNull]
			string expectedMValueExpression,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_lineClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> lineClasses,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_searchDistance))]
			double searchDistance,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_mTolerance))]
			double mTolerance,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_lineMSource))]
			LineMSource lineMSource,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_requireLine))]
			bool requireLine,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_ignoreUndefinedExpectedMValue))]
			bool ignoreUndefinedExpectedMValue,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_matchExpression))] [CanBeNull]
			string matchExpression)
			: base((IEnumerable<IFeatureClassSchemaDef>) Union(new[] { pointClass }, lineClasses))
		{
			Assert.NotNull(pointClass, "pointClass");
			Assert.NotNull(lineClasses, "lineClasses");
			Assert.True(searchDistance >= 0, "SearchDistance < 0");

			PointClass = pointClass;
			ExpectedMValueExpression = expectedMValueExpression;
			LineClasses = lineClasses;
			SearchDistance = searchDistance;
			MTolerance = mTolerance;
			LineMSource = lineMSource;
			RequireLine = requireLine;
			IgnoreUndefinedExpectedMValue = ignoreUndefinedExpectedMValue;
			MatchExpression = matchExpression;

			_tableCount = lineClasses.Count + 1;
			_pointClass = pointClass;
			_expectedMValueExpression = StringUtils.IsNotEmpty(expectedMValueExpression)
				                            ? expectedMValueExpression
				                            : null;

			_lineMSource = lineMSource;
			_requireLine = requireLine;
			_ignoreUndefinedExpectedMValue = ignoreUndefinedExpectedMValue;
			_matchExpression = StringUtils.IsNotEmpty(matchExpression)
				                   ? matchExpression
				                   : null;
		}
	}
}
