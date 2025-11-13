using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Definition class for QaNeighbourAreas
	/// </summary>
	[UsedImplicitly]
	[TopologyTest]
	public class QaNeighbourAreasDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef PolygonClass { get; }
		public string Constraint { get; }
		public bool AllowPointIntersection { get; }
		public IEnumerable<string> Fields { get; }
		public FieldListType FieldListType { get; }

		private static readonly char[] _tokenSeparators = { ' ', ',', ';' };

		[Doc(nameof(DocStrings.QaNeighbourAreas_0))]
		public QaNeighbourAreasDefinition(
				[Doc(nameof(DocStrings.QaNeighbourAreas_polygonClass))] [NotNull]
				IFeatureClassSchemaDef polygonClass,
				[Doc(nameof(DocStrings.QaNeighbourAreas_constraint))] [CanBeNull]
				string constraint)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polygonClass, constraint, false) { }

		[Doc(nameof(DocStrings.QaNeighbourAreas_1))]
		public QaNeighbourAreasDefinition(
			[Doc(nameof(DocStrings.QaNeighbourAreas_polygonClass))] [NotNull]
			IFeatureClassSchemaDef polygonClass,
			[Doc(nameof(DocStrings.QaNeighbourAreas_constraint))] [CanBeNull]
			string constraint,
			[Doc(nameof(DocStrings.QaNeighbourAreas_allowPointIntersection))]
			bool allowPointIntersection)
			: base(polygonClass)
		{
			PolygonClass = polygonClass;
			Constraint = constraint;
			AllowPointIntersection = allowPointIntersection;
		}

		[Doc(nameof(DocStrings.QaNeighbourAreas_2))]
		public QaNeighbourAreasDefinition(
			[Doc(nameof(DocStrings.QaNeighbourAreas_polygonClass))] [NotNull]
			IFeatureClassSchemaDef polygonClass,
			[Doc(nameof(DocStrings.QaNeighbourAreas_allowPointIntersection))]
			bool allowPointIntersection)
			: this(polygonClass, allowPointIntersection, string.Empty,
			       FieldListType.IgnoredFields) { }

		[Doc(nameof(DocStrings.QaNeighbourAreas_3))]
		public QaNeighbourAreasDefinition(
			[Doc(nameof(DocStrings.QaNeighbourAreas_polygonClass))] [NotNull]
			IFeatureClassSchemaDef polygonClass,
			[Doc(nameof(DocStrings.QaNeighbourAreas_allowPointIntersection))]
			bool allowPointIntersection,
			[Doc(nameof(DocStrings.QaNeighbourAreas_fieldsString))] [CanBeNull]
			string fieldsString,
			[Doc(nameof(DocStrings.QaNeighbourAreas_fieldListType))]
			FieldListType fieldListType)
			: this(polygonClass, allowPointIntersection,
			       GetTokens(fieldsString), fieldListType) { }

		[Doc(nameof(DocStrings.QaNeighbourAreas_4))]
		public QaNeighbourAreasDefinition(
			[Doc(nameof(DocStrings.QaNeighbourAreas_polygonClass))] [NotNull]
			IFeatureClassSchemaDef polygonClass,
			[Doc(nameof(DocStrings.QaNeighbourAreas_allowPointIntersection))]
			bool allowPointIntersection,
			[Doc(nameof(DocStrings.QaNeighbourAreas_fields))] [NotNull]
			IEnumerable<string> fields,
			[Doc(nameof(DocStrings.QaNeighbourAreas_fieldListType))]
			FieldListType fieldListType)
			: base(polygonClass)
		{
			Assert.ArgumentNotNull(polygonClass, nameof(polygonClass));
			Assert.ArgumentNotNull(fields, nameof(fields));

			PolygonClass = polygonClass;
			AllowPointIntersection = allowPointIntersection;
			Fields = fields ?? new List<string>();
			FieldListType = fieldListType;
		}

		public static IEnumerable<string> GetTokens([CanBeNull] string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				yield break;
			}

			foreach (
				string token in
				text.Split(_tokenSeparators, StringSplitOptions.RemoveEmptyEntries))
			{
				if (string.IsNullOrEmpty(token))
				{
					continue;
				}

				yield return token.Trim();
			}
		}
	}
}
