using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[LinearNetworkTest]
	public class QaOrphanNodeDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> PointClasses { get; }
		public IList<IFeatureClassSchemaDef> PolylineClasses { get; }
		public OrphanErrorType ErrorType { get; }

		[Doc(nameof(DocStrings.QaOrphanNode_0))]
		public QaOrphanNodeDefinition(
				[Doc(nameof(DocStrings.QaOrphanNode_pointClasses))]
				IList<IFeatureClassSchemaDef> pointClasses,
				[Doc(nameof(DocStrings.QaOrphanNode_polylineClasses))]
				IList<IFeatureClassSchemaDef> polylineClasses)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(pointClasses, polylineClasses, OrphanErrorType.Both) { }

		[Doc(nameof(DocStrings.QaOrphanNode_1))]
		public QaOrphanNodeDefinition(
				[Doc(nameof(DocStrings.QaOrphanNode_pointClass))]
				IFeatureClassSchemaDef pointClass,
				[Doc(nameof(DocStrings.QaOrphanNode_polylineClass))]
				IFeatureClassSchemaDef polylineClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(pointClass, polylineClass, OrphanErrorType.Both) { }

		[Doc(nameof(DocStrings.QaOrphanNode_2))]
		public QaOrphanNodeDefinition(
			[Doc(nameof(DocStrings.QaOrphanNode_pointClasses))]
			IList<IFeatureClassSchemaDef> pointClasses,
			[Doc(nameof(DocStrings.QaOrphanNode_polylineClasses))]
			IList<IFeatureClassSchemaDef> polylineClasses,
			[Doc(nameof(DocStrings.QaOrphanNode_errorType))]
			OrphanErrorType errorType)
			: base(pointClasses.Union(polylineClasses))
		{
			PointClasses = pointClasses;
			PolylineClasses = polylineClasses;
			ErrorType = errorType;
		}

		[Doc(nameof(DocStrings.QaOrphanNode_3))]
		public QaOrphanNodeDefinition(
			[Doc(nameof(DocStrings.QaOrphanNode_pointClass))]
			IFeatureClassSchemaDef pointClass,
			[Doc(nameof(DocStrings.QaOrphanNode_polylineClass))]
			IFeatureClassSchemaDef polylineClass,
			[Doc(nameof(DocStrings.QaOrphanNode_errorType))]
			OrphanErrorType errorType)
			: this(new[] { pointClass }, new[] { polylineClass },
			       errorType) { }
	}
}
