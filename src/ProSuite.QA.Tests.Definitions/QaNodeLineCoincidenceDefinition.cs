using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[LinearNetworkTest]
	public class QaNodeLineCoincidenceDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef NodeClass { get; }
		public IList<IFeatureClassSchemaDef> NearClasses { get; }
		public IList<double> NearTolerances { get; }
		public double WithinPolylineTolerance { get; }
		public bool IgnoreNearEndpoints { get; }
		public bool Is3D { get; }

		private const double _defaultCoincidenceTolerance = -1;

		[Doc(nameof(DocStrings.QaNodeLineCoincidence_0))]
		public QaNodeLineCoincidenceDefinition(
				[Doc(nameof(DocStrings.QaNodeLineCoincidence_nodeClass))] [NotNull]
				IFeatureClassSchemaDef nodeClass,
				[Doc(nameof(DocStrings.QaNodeLineCoincidence_nearClasses))] [NotNull]
				IList<IFeatureClassSchemaDef> nearClasses,
				[Doc(nameof(DocStrings.QaNodeLineCoincidence_near))]
				double near)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(nodeClass, nearClasses, near, false) { }

		[Doc(nameof(DocStrings.QaNodeLineCoincidence_1))]
		public QaNodeLineCoincidenceDefinition(
				[Doc(nameof(DocStrings.QaNodeLineCoincidence_nodeClass))] [NotNull]
				IFeatureClassSchemaDef nodeClass,
				[Doc(nameof(DocStrings.QaNodeLineCoincidence_nearClasses))] [NotNull]
				IList<IFeatureClassSchemaDef> nearClasses,
				[Doc(nameof(DocStrings.QaNodeLineCoincidence_near))]
				double near,
				[Doc(nameof(DocStrings.QaNodeLineCoincidence_ignoreNearEndpoints))]
				bool ignoreNearEndpoints)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(nodeClass, nearClasses, near, ignoreNearEndpoints, false) { }

		[Doc(nameof(DocStrings.QaNodeLineCoincidence_1))]
		public QaNodeLineCoincidenceDefinition(
			[Doc(nameof(DocStrings.QaNodeLineCoincidence_nodeClass))] [NotNull]
			IFeatureClassSchemaDef nodeClass,
			[Doc(nameof(DocStrings.QaNodeLineCoincidence_nearClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> nearClasses,
			[Doc(nameof(DocStrings.QaNodeLineCoincidence_near))]
			double near,
			[Doc(nameof(DocStrings.QaNodeLineCoincidence_ignoreNearEndpoints))]
			bool ignoreNearEndpoints,
			[Doc(nameof(DocStrings.QaNodeLineCoincidence_is3D))]
			bool is3D)
			: this(nodeClass, nearClasses, new[] { near }, near, ignoreNearEndpoints, is3D) { }

		[Doc(nameof(DocStrings.QaNodeLineCoincidence_3))]
		public QaNodeLineCoincidenceDefinition(
			[Doc(nameof(DocStrings.QaNodeLineCoincidence_nodeClass))] [NotNull]
			IFeatureClassSchemaDef nodeClass,
			[Doc(nameof(DocStrings.QaNodeLineCoincidence_nearClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> nearClasses,
			[Doc(nameof(DocStrings.QaNodeLineCoincidence_nearTolerances))] [NotNull]
			IList<double> nearTolerances,
			[Doc(nameof(DocStrings.QaNodeLineCoincidence_withinPolylineTolerance))]
			double withinPolylineTolerance,
			[Doc(nameof(DocStrings.QaNodeLineCoincidence_ignoreNearEndpoints))]
			bool ignoreNearEndpoints,
			[Doc(nameof(DocStrings.QaNodeLineCoincidence_is3D))]
			bool is3D) :
			base(CastToTables(new[] { nodeClass }, nearClasses))
		{
			Assert.ArgumentNotNull(nodeClass, nameof(nodeClass));
			Assert.ArgumentNotNull(nearClasses, nameof(nearClasses));
			Assert.ArgumentNotNull(nearTolerances, nameof(nearTolerances));
			Assert.ArgumentCondition(
				nearTolerances.Count == 1 || nearTolerances.Count == nearClasses.Count,
				"Invalid number of near tolerances: either one tolerance must be specified (used for all), " +
				"or one tolerance per nearClasses (in the same order)");

			NodeClass = nodeClass;
			NearClasses = nearClasses;
			NearTolerances = nearTolerances;
			WithinPolylineTolerance = withinPolylineTolerance;
			IgnoreNearEndpoints = ignoreNearEndpoints;
			Is3D = is3D;
		}

		[TestParameter(_defaultCoincidenceTolerance)]
		[Doc(nameof(DocStrings.QaNodeLineCoincidence_CoincidenceTolerance))]
		public double CoincidenceTolerance { get; set; }
	}
}
