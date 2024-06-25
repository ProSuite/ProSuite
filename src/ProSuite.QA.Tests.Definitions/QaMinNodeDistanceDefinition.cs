using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Text;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[ProximityTest]
	[LinearNetworkTest]
	[ZValuesTest]
	public class QaMinNodeDistanceDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> FeatureClasses { get; }
		public double Near { get; }
		public double Tolerance { get; }
		public bool Is3D { get; }
		public double MaxZDifference { get; }
		public string ValidRelationConstraint { get; }

		private readonly bool _is3D;
		private readonly double _maxZDifference;
		private readonly string _validRelationConstraintSql;

		private readonly double _searchDistanceSquared;
		private readonly double _toleranceSquared;

		private const int _noMaxZDifference = -1;

		[Doc(nameof(DocStrings.QaMinNodeDistance_0))]
		public QaMinNodeDistanceDefinition(
			[Doc(nameof(DocStrings.QaMinNodeDistance_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
			double near,
			[Doc(nameof(DocStrings.QaMinNodeDistance_is3D))]
			bool is3D)
			: this(featureClass, near, double.NaN,
			       is3D, _noMaxZDifference, null) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_1))]
		public QaMinNodeDistanceDefinition(
			[Doc(nameof(DocStrings.QaMinNodeDistance_featureClasses))]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
			double near,
			[Doc(nameof(DocStrings.QaMinNodeDistance_is3D))]
			bool is3D)
			: this(featureClasses, near, double.NaN, is3D) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_2))]
		public QaMinNodeDistanceDefinition(
			[Doc(nameof(DocStrings.QaMinNodeDistance_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
			double near,
			[Doc(nameof(DocStrings.QaMinNodeDistance_tolerance))]
			double tolerance,
			[Doc(nameof(DocStrings.QaMinNodeDistance_is3D))]
			bool is3D)
			: this(featureClass, near, tolerance, is3D, _noMaxZDifference, null) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_3))]
		public QaMinNodeDistanceDefinition(
			[Doc(nameof(DocStrings.QaMinNodeDistance_featureClasses))]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
			double near,
			[Doc(nameof(DocStrings.QaMinNodeDistance_tolerance))]
			double tolerance,
			[Doc(nameof(DocStrings.QaMinNodeDistance_is3D))]
			bool is3D)
			: this(featureClasses, near, tolerance, is3D, _noMaxZDifference, null) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_4))]
		public QaMinNodeDistanceDefinition(
			[Doc(nameof(DocStrings.QaMinNodeDistance_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
			double near,
			[Doc(nameof(DocStrings.QaMinNodeDistance_maxZDifference))]
			double maxZDifference)
			: this(featureClass, near, double.NaN, false,
			       maxZDifference, null) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_5))]
		public QaMinNodeDistanceDefinition(
			[Doc(nameof(DocStrings.QaMinNodeDistance_featureClasses))]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
			double near,
			[Doc(nameof(DocStrings.QaMinNodeDistance_maxZDifference))]
			double maxZDifference)
			: this(featureClasses, near, double.NaN, maxZDifference) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_6))]
		public QaMinNodeDistanceDefinition(
				[Doc(nameof(DocStrings.QaMinNodeDistance_featureClass))]
				IFeatureClassSchemaDef featureClass,
				[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
				double near,
				[Doc(nameof(DocStrings.QaMinNodeDistance_tolerance))]
				double tolerance,
				[Doc(nameof(DocStrings.QaMinNodeDistance_maxZDifference))]
				double maxZDifference)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, near, tolerance, maxZDifference, null) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_7))]
		public QaMinNodeDistanceDefinition(
				[Doc(nameof(DocStrings.QaMinNodeDistance_featureClasses))]
				IList<IFeatureClassSchemaDef> featureClasses,
				[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
				double near,
				[Doc(nameof(DocStrings.QaMinNodeDistance_tolerance))]
				double tolerance,
				[Doc(nameof(DocStrings.QaMinNodeDistance_maxZDifference))]
				double maxZDifference)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, near, tolerance, maxZDifference, null) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_1))]
		public QaMinNodeDistanceDefinition(
			[Doc(nameof(DocStrings.QaMinNodeDistance_featureClasses))]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
			double near)
			: this(featureClasses, near, double.NaN, false) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_9))]
		public QaMinNodeDistanceDefinition(
			[Doc(nameof(DocStrings.QaMinNodeDistance_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
			double near,
			[Doc(nameof(DocStrings.QaMinNodeDistance_tolerance))]
			double tolerance,
			[Doc(nameof(DocStrings.QaMinNodeDistance_maxZDifference))]
			double maxZDifference,
			[Doc(nameof(DocStrings.QaMinNodeDistance_validRelationConstraint))] [CanBeNull]
			string validRelationConstraint)
			: this(featureClass, near, tolerance, false,
			       maxZDifference, validRelationConstraint) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_10))]
		public QaMinNodeDistanceDefinition(
			[Doc(nameof(DocStrings.QaMinNodeDistance_featureClasses))]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
			double near,
			[Doc(nameof(DocStrings.QaMinNodeDistance_tolerance))]
			double tolerance,
			[Doc(nameof(DocStrings.QaMinNodeDistance_maxZDifference))]
			double maxZDifference,
			[Doc(nameof(DocStrings.QaMinNodeDistance_validRelationConstraint))] [CanBeNull]
			string validRelationConstraint)
			: this(featureClasses, near, tolerance, false,
			       maxZDifference, validRelationConstraint) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_11))]
		public QaMinNodeDistanceDefinition(
			[Doc(nameof(DocStrings.QaMinNodeDistance_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
			double near,
			[Doc(nameof(DocStrings.QaMinNodeDistance_tolerance))]
			double tolerance,
			[Doc(nameof(DocStrings.QaMinNodeDistance_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaMinNodeDistance_validRelationConstraint))] [CanBeNull]
			string validRelationConstraint)
			: this(
				featureClass, near, tolerance, is3D, _noMaxZDifference, validRelationConstraint) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_12))]
		public QaMinNodeDistanceDefinition(
			[Doc(nameof(DocStrings.QaMinNodeDistance_featureClasses))]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
			double near,
			[Doc(nameof(DocStrings.QaMinNodeDistance_tolerance))]
			double tolerance,
			[Doc(nameof(DocStrings.QaMinNodeDistance_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaMinNodeDistance_validRelationConstraint))] [CanBeNull]
			string validRelationConstraint)
			: this(
				featureClasses, near, tolerance, is3D, _noMaxZDifference,
				validRelationConstraint) { }

		private QaMinNodeDistanceDefinition(
			[NotNull] IFeatureClassSchemaDef featureClass,
			double near, double tolerance, bool is3D, double maxZDifference,
			[CanBeNull] string validRelationConstraint)
			: this(new[] { featureClass }, near, tolerance, is3D, maxZDifference,
			       validRelationConstraint) { }

		private QaMinNodeDistanceDefinition(
			[NotNull] IList<IFeatureClassSchemaDef> featureClasses,
			double near, double tolerance, bool is3D, double maxZDifference,
			[CanBeNull] string validRelationConstraint)
			: base(CastToTables(
				       (IList<IFeatureClassSchemaDef>)
				       (IEnumerable<IFeatureClassSchemaDef>) featureClasses))
		{
			FeatureClasses = featureClasses;
			Near = near;
			Tolerance = tolerance;
			Is3D = is3D;
			MaxZDifference = maxZDifference;
			ValidRelationConstraint = validRelationConstraint;

			_searchDistanceSquared = near * near;
			_toleranceSquared = tolerance < 0
				                    ? 0
				                    : tolerance * tolerance;
			_is3D = is3D;
			_maxZDifference = maxZDifference;

			_validRelationConstraintSql = StringUtils.IsNotEmpty(validRelationConstraint)
				                              ? validRelationConstraint
				                              : null;
		}
	}
}
