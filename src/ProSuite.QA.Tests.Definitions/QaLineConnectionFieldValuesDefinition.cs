using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	/*
	 * Beispiele:
	 * ----------
	 *
	 * Punkt-Attributwert muss irgendeinem der Werte auf den Linien entsprechen (Beispiel Kanalnamen)
	 *
	 * - LineFieldValuesConstraint: NoConstraint
	 * - PointFieldValuesConstraint: AllEqualAndMatchAnyLineValue
	 *
	 *
	 * Punkt-Attributwert muss dem häufigsten der Werte auf den Linien entsprechen (Beispiel Betriebsgruppe / Betriebsstatus).
	 *
	 * - LineFieldValuesConstraint: NoConstraint
	 * - PointFieldValuesConstraint: AllEqualAndMatchMostFrequentLineValue
	 *
	 *
	 * Attributwerte auf den Linien müssen gleich sein, ausser ein bestimmter Punkttyp liegt auf der Verbindung (Beispiel Wechsel amtliche Nummer)
	 *
	 * - LineFieldValuesConstraint: AllEqualOrValidPointExists
	 * - PointFieldValuesConstraint: NoConstraint
	 * - pointField = null
	 *
	 *
	 * TODO add option to ignore null values? Not sure if needed
	 * TODO add explicit tolerance? Or (optionally) get maximum tolerance from feature classes?
	 */

	[UsedImplicitly]
	[LinearNetworkTest]
	public class QaLineConnectionFieldValuesDefinition : AlgorithmDefinition
	{

		public IList<IFeatureClassSchemaDef> LineClasses { get; }
		public IList<string> LineFields { get; }
		public LineFieldValuesConstraint LineFieldValuesConstraint { get; }
		public IList<IFeatureClassSchemaDef> PointClasses { get; }
		public IList<string> PointFields { get; }
		public PointFieldValuesConstraint PointFieldValuesConstraint { get; }
		public IList<string> AllowedPointsExpressions { get; }
		//public string AllowedPointsExpressions { get; }

		[CanBeNull] private static TestIssueCodes _codes;


		[Doc(nameof(DocStrings.QaLineConnectionFieldValues_0))]
		public QaLineConnectionFieldValuesDefinition(
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineClass))] [NotNull]
			IFeatureClassSchemaDef lineClass,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineField))] [NotNull]
			string lineField,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineFieldValuesConstraint))]
			LineFieldValuesConstraint lineFieldValuesConstraint,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointClass))] [NotNull]
			IFeatureClassSchemaDef pointClass,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointField))] [CanBeNull]
			string pointField,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointFieldValuesConstraint))]
			PointFieldValuesConstraint pointFieldValuesConstraint)
			: this(new[] { lineClass }, new[] { lineField }, lineFieldValuesConstraint,
				   pointClass, pointField, pointFieldValuesConstraint)
		{ }

		[Doc(nameof(DocStrings.QaLineConnectionFieldValues_1))]
		public QaLineConnectionFieldValuesDefinition(
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineClasses))] [NotNull]
			IList<IFeatureClassSchemaDef>
				lineClasses,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineFields))] [NotNull]
			IList<string> lineFields,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineFieldValuesConstraint))]
			LineFieldValuesConstraint lineFieldValuesConstraint,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointClass))] [NotNull]
			IFeatureClassSchemaDef pointClass,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointField))] [CanBeNull]
			string pointField,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointFieldValuesConstraint))]
			PointFieldValuesConstraint pointFieldValuesConstraint)
			: this(
				lineClasses, lineFields, lineFieldValuesConstraint, pointClass, pointField,
				// ReSharper disable once IntroduceOptionalParameters.Global
				pointFieldValuesConstraint, null)
		{ }

		[Doc(nameof(DocStrings.QaLineConnectionFieldValues_2))]
		public QaLineConnectionFieldValuesDefinition(
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineClass))] [NotNull]
			IFeatureClassSchemaDef lineClass,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineField))] [NotNull]
			string lineField,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineFieldValuesConstraint))]
			LineFieldValuesConstraint lineFieldValuesConstraint,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointClass))] [NotNull]
			IFeatureClassSchemaDef pointClass)
			: this(new[] { lineClass }, new[] { lineField }, lineFieldValuesConstraint,
				   pointClass, null, PointFieldValuesConstraint.NoConstraint)
		{ }

		[Doc(nameof(DocStrings.QaLineConnectionFieldValues_3))]
		public QaLineConnectionFieldValuesDefinition(
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineClasses))] [NotNull]
			IList<IFeatureClassSchemaDef>
				lineClasses,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineFields))] [NotNull]
			IList<string> lineFields,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineFieldValuesConstraint))]
			LineFieldValuesConstraint lineFieldValuesConstraint,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointClass))] [NotNull]
			IFeatureClassSchemaDef pointClass,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointField))] [CanBeNull]
			string pointField,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointFieldValuesConstraint))]
			PointFieldValuesConstraint pointFieldValuesConstraint,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_allowedPointsExpression))]
			[CanBeNull]
			string
				allowedPointsExpression)
			: this(lineClasses, lineFields, lineFieldValuesConstraint, new[] { pointClass },
				   string.IsNullOrEmpty(pointField) ? null : new[] { pointField },
				   pointFieldValuesConstraint,
				   string.IsNullOrEmpty(allowedPointsExpression)
					   ? null
					   : new[] { allowedPointsExpression })
		{ }

		[Doc(nameof(DocStrings.QaLineConnectionFieldValues_4))]
		public QaLineConnectionFieldValuesDefinition(
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineClasses))] [NotNull]
			IList<IFeatureClassSchemaDef>
				lineClasses,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineFields))] [NotNull]
			IList<string> lineFields,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineFieldValuesConstraint))]
			LineFieldValuesConstraint lineFieldValuesConstraint,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointClasses))] [NotNull]
			IList<IFeatureClassSchemaDef>
				pointClasses,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointFields))] [CanBeNull]
			IList<string>
				pointFields,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointFieldValuesConstraint))]
			PointFieldValuesConstraint pointFieldValuesConstraint,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_allowedPointsExpressions))]
			[CanBeNull]
			IList<string>
				allowedPointsExpressions)
			: base(CastToTables(lineClasses, pointClasses))
		{
			Assert.ArgumentNotNull(lineClasses, nameof(lineClasses));
			Assert.ArgumentNotNull(lineFields, nameof(lineFields));
			Assert.ArgumentNotNull(pointClasses, nameof(pointClasses));
			Assert.ArgumentCondition(
				lineFields.Count == 1 || lineFields.Count == lineClasses.Count,
				"lineFields must be either a single field that exists in all lineClasses, or one field per lineClass");

			Assert.ArgumentCondition(
				pointFields == null || pointFields.Count == 0 || pointFields.Count == 1 ||
				pointFields.Count == pointClasses.Count,
				"pointFields must be either null, a single field that exists in all pointClasses, or one field per pointClass");

			Assert.ArgumentCondition(
				allowedPointsExpressions == null || allowedPointsExpressions.Count == 0 ||
				allowedPointsExpressions.Count == 1 ||
				allowedPointsExpressions.Count == pointClasses.Count,
				"allowedPointsExpressions must be either null, a single expression that exists in all pointClasses, or one expression per pointClass");

			LineClasses = lineClasses;
			LineFields = lineFields;
			LineFieldValuesConstraint = lineFieldValuesConstraint;
			PointClasses = pointClasses;
			PointFields = pointFields;
			PointFieldValuesConstraint = pointFieldValuesConstraint;
			AllowedPointsExpressions = allowedPointsExpressions;
		}
	}
}
