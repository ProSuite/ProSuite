using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[LinearNetworkTest]
	public class QaPseudoNodesDefinition : AlgorithmDefinition
	{
		[NotNull]
		public IList<IFeatureClassSchemaDef> PolylineClasses { get; }

		public IList<IList<string>> IgnoreFields { get; }

		[CanBeNull]
		public IList<IFeatureClassSchemaDef> ValidPseudoNodes { get; }

		[Doc(nameof(DocStrings.QaPseudoNodes_0))]
		[InternallyUsedTest]
		public QaPseudoNodesDefinition(
			[Doc(nameof(DocStrings.QaPseudoNodes_polylineClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> polylineClasses,
			[Doc(nameof(DocStrings.QaPseudoNodes_ignoreFields_0))] [NotNull]
			IList<IList<string>> ignoreFields,
			[Doc(nameof(DocStrings.QaPseudoNodes_validPseudoNodes))] [NotNull]
			IList<IFeatureClassSchemaDef> validPseudoNodes)
			: base(polylineClasses.Union(validPseudoNodes))
		{
			AssertEqualCount(polylineClasses, ignoreFields);

			PolylineClasses = polylineClasses;
			IgnoreFields = ignoreFields;
			ValidPseudoNodes = validPseudoNodes;
		}

		[Doc(nameof(DocStrings.QaPseudoNodes_1))]
		public QaPseudoNodesDefinition(
			[Doc(nameof(DocStrings.QaPseudoNodes_polylineClass))] [NotNull]
			IFeatureClassSchemaDef polylineClass,
			[Doc(nameof(DocStrings.QaPseudoNodes_ignoreFields_1))] [NotNull]
			string[] ignoreFields,
			[Doc(nameof(DocStrings.QaPseudoNodes_validPseudoNode))] [NotNull]
			IFeatureClassSchemaDef validPseudoNode)
			: this(new[] { polylineClass }, new IList<string>[] { ignoreFields },
			       new[] { validPseudoNode }) { }

		[Doc(nameof(DocStrings.QaPseudoNodes_2))]
		[InternallyUsedTest]
		public QaPseudoNodesDefinition(
				[Doc(nameof(DocStrings.QaPseudoNodes_polylineClasses))] [NotNull]
				IList<IFeatureClassSchemaDef> polylineClasses,
				[Doc(nameof(DocStrings.QaPseudoNodes_ignoreFields_0))] [NotNull]
				IList<IList<string>> ignoreFields)
			// ReSharper disable once PossiblyMistakenUseOfParamsMethod
			: base(polylineClasses)
		{
			AssertEqualCount(polylineClasses, ignoreFields);

			PolylineClasses = polylineClasses;
			IgnoreFields = ignoreFields;
		}

		[Doc(nameof(DocStrings.QaPseudoNodes_3))]
		public QaPseudoNodesDefinition(
			[Doc(nameof(DocStrings.QaPseudoNodes_polylineClass))] [NotNull]
			IFeatureClassSchemaDef polylineClass,
			[Doc(nameof(DocStrings.QaPseudoNodes_ignoreFields_1))] [NotNull]
			string[] ignoreFields)
			: this(new[] { polylineClass },
			       new IList<string>[] { ignoreFields }) { }

		[Doc(nameof(DocStrings.QaPseudoNodes_0))]
		public QaPseudoNodesDefinition(
			[Doc(nameof(DocStrings.QaPseudoNodes_polylineClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> polylineClasses,
			[Doc(nameof(DocStrings.QaPseudoNodes_ignoreFieldLists))] [NotNull]
			IList<string> ignoreFieldLists,
			[Doc(nameof(DocStrings.QaPseudoNodes_validPseudoNodes))] [NotNull]
			IList<IFeatureClassSchemaDef> validPseudoNodes)
			: this(polylineClasses, ParseFieldLists(ignoreFieldLists), validPseudoNodes) { }

		[Doc(nameof(DocStrings.QaPseudoNodes_2))]
		public QaPseudoNodesDefinition(
			[Doc(nameof(DocStrings.QaPseudoNodes_polylineClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> polylineClasses,
			[Doc(nameof(DocStrings.QaPseudoNodes_ignoreFieldLists))] [NotNull]
			IList<string> ignoreFieldLists)
			: this(polylineClasses, ParseFieldLists(ignoreFieldLists)) { }

		private static List<IList<string>> ParseFieldLists(IList<string> fieldLists)
		{
			List<IList<string>> fields = new List<IList<string>>();
			foreach (string fieldList in fieldLists)
			{
				fields.Add(
					fieldList.Split(new[] { ',' },
					                StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim())
					         .ToList());
			}

			return fields;
		}

		[Doc(nameof(DocStrings.QaPseudoNodes_IgnoreLoopEndpoints))]
		[TestParameter(false)]
		public bool IgnoreLoopEndpoints { get; set; }

		[NotNull]
		private static IList<int> GetPolycurveClassIndices(
			[NotNull] IList<IFeatureClassSchemaDef> networkEdgeClasses,
			[NotNull] IList<IFeatureClassSchemaDef> exceptionClasses)
		{
			var result = new List<int>();

			int exceptionClassCount = exceptionClasses.Count;

			for (int exceptionClassIndex = 0;
			     exceptionClassIndex < exceptionClassCount;
			     exceptionClassIndex++)
			{
				IFeatureClassSchemaDef exceptionClass = exceptionClasses[exceptionClassIndex];

				if (exceptionClass.ShapeType != ProSuiteGeometryType.Point)
				{
					int involvedTableIndex = exceptionClassIndex +
					                         networkEdgeClasses.Count;

					result.Add(involvedTableIndex);
				}
			}

			return result;
		}

		private static void AssertEqualCount(
			[NotNull] ICollection<IFeatureClassSchemaDef> polylineClasses,
			[NotNull] ICollection<IList<string>> ignoreFields)
		{
			Assert.ArgumentNotNull(polylineClasses, nameof(polylineClasses));
			Assert.ArgumentNotNull(ignoreFields, nameof(ignoreFields));

			Assert.ArgumentCondition(ignoreFields.Count == polylineClasses.Count,
			                         "Number of polylineClasses ({0}) != number of ignoreField lists ({1})",
			                         polylineClasses.Count, ignoreFields.Count);
		}
	}
}
