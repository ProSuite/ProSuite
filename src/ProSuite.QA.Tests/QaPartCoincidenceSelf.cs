using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Coincidence;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[ProximityTest]
	public class QaPartCoincidenceSelf : QaNearCoincidenceBase
	{
		[Doc(nameof(DocStrings.QaPartCoincidenceSelf_0))]
		public QaPartCoincidenceSelf(
				[Doc(nameof(DocStrings.QaPartCoincidence_featureClass))]
				IReadOnlyFeatureClass featureClass,
				[Doc(nameof(DocStrings.QaPartCoincidence_near))]
				double near,
				[Doc(nameof(DocStrings.QaPartCoincidence_minLength))]
				double minLength,
				[Doc(nameof(DocStrings.QaPartCoincidence_is3D))]
				bool is3D)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, near, minLength, is3D, 1000.0) { }

		[Doc(nameof(DocStrings.QaPartCoincidenceSelf_0))]
		public QaPartCoincidenceSelf(
			[Doc(nameof(DocStrings.QaPartCoincidence_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaPartCoincidence_near))]
			double near,
			[Doc(nameof(DocStrings.QaPartCoincidence_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaPartCoincidence_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaPartCoincidence_tileSize))]
			double tileSize)
			: this(new[] {featureClass}, near, minLength, is3D, tileSize) { }

		[Doc(nameof(DocStrings.QaPartCoincidenceSelf_2))]
		public QaPartCoincidenceSelf(
				[Doc(nameof(DocStrings.QaPartCoincidence_featureClasses))]
				IEnumerable<IReadOnlyFeatureClass>
					featureClasses,
				[Doc(nameof(DocStrings.QaPartCoincidence_near))]
				double near,
				[Doc(nameof(DocStrings.QaPartCoincidence_minLength))]
				double minLength,
				[Doc(nameof(DocStrings.QaPartCoincidence_is3D))]
				bool is3D)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, near, minLength, is3D, 1000.0) { }

		[Doc(nameof(DocStrings.QaPartCoincidenceSelf_2))]
		public QaPartCoincidenceSelf(
			[Doc(nameof(DocStrings.QaPartCoincidence_featureClasses))]
			IEnumerable<IReadOnlyFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaPartCoincidence_near))]
			double near,
			[Doc(nameof(DocStrings.QaPartCoincidence_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaPartCoincidence_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaPartCoincidence_tileSize))]
			double tileSize)
			: this(featureClasses, near, minLength, minLength, is3D, tileSize, 0) { }

		[Doc(nameof(DocStrings.QaPartCoincidenceSelf_2))]
		public QaPartCoincidenceSelf(
				[Doc(nameof(DocStrings.QaPartCoincidence_featureClasses))]
				IEnumerable<IReadOnlyFeatureClass>
					featureClasses,
				[Doc(nameof(DocStrings.QaPartCoincidence_near))]
				double near,
				[Doc(nameof(DocStrings.QaPartCoincidence_minLength))]
				double minLength)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, near, minLength, false, 1000.0) { }

		[Doc(nameof(DocStrings.QaPartCoincidenceSelf_2))]
		public QaPartCoincidenceSelf(
			[Doc(nameof(DocStrings.QaPartCoincidence_featureClasses))]
			IEnumerable<IReadOnlyFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaPartCoincidence_near))]
			double near,
			[Doc(nameof(DocStrings.QaPartCoincidence_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaPartCoincidence_tileSize))]
			double tileSize)
			: this(featureClasses, near, minLength, false, tileSize) { }

		[Doc(nameof(DocStrings.QaPartCoincidenceSelf_6))]
		public QaPartCoincidenceSelf(
			[Doc(nameof(DocStrings.QaPartCoincidence_featureClasses))]
			IEnumerable<IReadOnlyFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaPartCoincidence_near))]
			double near,
			[Doc(nameof(DocStrings.QaPartCoincidence_connectedMinLength))]
			double connectedMinLength,
			[Doc(nameof(DocStrings.QaPartCoincidence_disjointMinLength))]
			double disjointMinLength,
			[Doc(nameof(DocStrings.QaPartCoincidence_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaPartCoincidence_tileSize))]
			double tileSize,
			[Doc(nameof(DocStrings.QaPartCoincidence_coincidenceTolerance))]
			double coincidenceTolerance)
			: base(
				featureClasses, near, new ConstantFeatureDistanceProvider(near / 2),
				new ConstantPairDistanceProvider(connectedMinLength),
				new ConstantPairDistanceProvider(disjointMinLength),
				is3D, coincidenceTolerance) { }

		// TODO document
		[InternallyUsedTest] // not yet for public use
		[Doc(nameof(DocStrings.QaPartCoincidenceSelf_7))]
		private QaPartCoincidenceSelf(
			[Doc(nameof(DocStrings.QaPartCoincidence_featureClasses))]
			ICollection<IReadOnlyFeatureClass> featureClasses,
			double searchDistance,
			[NotNull] IEnumerable<string> nearExpressions,
			[NotNull] IEnumerable<string> connectedMinLengthExpressions,
			[NotNull] IEnumerable<string> disjointMinLengthExpressionsSql,
			[Doc(nameof(DocStrings.QaPartCoincidence_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaPartCoincidence_coincidenceTolerance))]
			double coincidenceTolerance)
			: this(featureClasses, searchDistance,
			       new ExpressionBasedDistanceProvider(nearExpressions, featureClasses),
			       new ExpressionBasedDistanceProvider(connectedMinLengthExpressions,
			                                           featureClasses),
			       new ExpressionBasedDistanceProvider(disjointMinLengthExpressionsSql,
			                                           featureClasses),
			       is3D, coincidenceTolerance) { }

		[InternallyUsedTest]
		public QaPartCoincidenceSelf([NotNull] QaPartCoincidenceSelfDefinition definition)
	: this(definition.FeatureClasses.Cast<IReadOnlyFeatureClass>(),
		   definition.Near, definition.ConnectedMinLength, definition.DisjointMinLength,
		   definition.Is3D, definition.TileSize, definition.CoincidenceTolerance)
		{
			if (definition.IgnoreNeighborConditions != null)
			{
				IgnoreNeighborConditions = definition.IgnoreNeighborConditions;
			}
		}

		/// <summary>
		/// needed to set sqlCaseSensitivity
		/// </summary>
		private QaPartCoincidenceSelf(
			[NotNull] IEnumerable<IReadOnlyFeatureClass> featureClasses,
			double searchDistance,
			[NotNull] ExpressionBasedDistanceProvider nearExpressionsProvider,
			[NotNull] ExpressionBasedDistanceProvider connectedMinLengthExpressionsProvider,
			[NotNull] ExpressionBasedDistanceProvider disjointMinLengthExpressionsSqlProvider,
			bool is3D, double coincidenceTolerance)
			: base(featureClasses, searchDistance,
			       nearExpressionsProvider,
			       connectedMinLengthExpressionsProvider,
			       disjointMinLengthExpressionsSqlProvider,
			       is3D, coincidenceTolerance)
		{
			nearExpressionsProvider.GetSqlCaseSensitivityForTableIndex = GetSqlCaseSensitivity;
			connectedMinLengthExpressionsProvider.GetSqlCaseSensitivityForTableIndex =
				GetSqlCaseSensitivity;
			disjointMinLengthExpressionsSqlProvider.GetSqlCaseSensitivityForTableIndex =
				GetSqlCaseSensitivity;
		}

		protected override bool IsDirected => false;

		[TestParameter]
		[Doc(nameof(DocStrings.QaPartCoincidenceSelf_IgnoreNeighborConditions))]
		public IList<string> IgnoreNeighborConditions
		{
			get { return IgnoreNeighborConditionsSqlFullMatrix; }
			set
			{
				IgnoreNeighborConditionsSqlFullMatrix = value;
				if (value.Count > 0)
				{
					foreach (var sql in value)
					{
						AddCustomQueryFilterExpression(sql);
					}
				}
			}
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			IgnoreUndirected = false;
			return base.ExecuteCore(row, tableIndex);
		}
	}
}
