using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[ProximityTest]
	public class QaPointOnLineDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef PointClass { get; }
		public IList<IFeatureClassSchemaDef> NearClasses { get; }
		public double Near { get; }

		[Doc(nameof(DocStrings.QaPointOnLine_0))]
		public QaPointOnLineDefinition(
			[NotNull] [Doc(nameof(DocStrings.QaPointOnLine_pointClass))]
			IFeatureClassSchemaDef pointClass,
			[NotNull] [Doc(nameof(DocStrings.QaPointOnLine_nearClasses))]
			IList<IFeatureClassSchemaDef> nearClasses,
			[Doc(nameof(DocStrings.QaPointOnLine_near))]
			double near)
			: base(new[] { pointClass }.Union(nearClasses))
		{
			PointClass = pointClass;
			NearClasses = nearClasses;
			Near = near;
		}
	}
}
