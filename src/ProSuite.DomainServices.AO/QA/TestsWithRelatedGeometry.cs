using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA
{
	internal class TestsWithRelatedGeometry
	{
		public TestsWithRelatedGeometry([NotNull] IReadOnlyTable table, [NotNull] IList<ITest> tests,
		                                [NotNull] IObjectDataset objectDataset,
		                                [NotNull]
		                                IEnumerable<IList<IRelationshipClass>> relClassChains)
		{
			Table = table;
			Tests = tests;
			ObjectDataset = objectDataset;
			RelClassChains = relClassChains;
		}

		[NotNull]
		public IReadOnlyTable Table { get; }

		[NotNull]
		public IList<ITest> Tests { get; }

		[CanBeNull]
		public IEnumerable<IList<IRelationshipClass>> RelClassChains { get; }

		[NotNull]
		public IObjectDataset ObjectDataset { get; }

		public bool HasAnyAssociationsToFeatureClasses { get; set; }
	}
}
