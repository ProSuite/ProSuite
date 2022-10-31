using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestContainer;

namespace ProSuite.QA.Tests.Test.TestRunners
{
	public class QaContainerTestRunner : QaTestRunnerBase
	{
		public QaContainerTestRunner(double tileSize, params ITest[] tests)
		{
			TestContainer = new TestContainer { TileSize = tileSize };
			foreach (ITest test in tests)
			{
				TestContainer.AddTest(test);
			}

			TestContainer.QaError += ProcessError;
		}

		[NotNull]
		public TestContainer TestContainer { get; }

		public int Execute([NotNull] IEnvelope boundingBox)
		{
			TestContainer.KeepErrorGeometry = KeepGeometry;
			return TestContainer.Execute(boundingBox);
		}

		public int Execute([NotNull] IPolygon polygon)
		{
			TestContainer.KeepErrorGeometry = KeepGeometry;
			return TestContainer.Execute(polygon);
		}

		public int Execute([NotNull] IList<ISelectionSet> selectionsList)
		{
			TestContainer.KeepErrorGeometry = KeepGeometry;
			return TestContainer.Execute(selectionsList);
		}

		public override int Execute()
		{
			TestContainer.KeepErrorGeometry = KeepGeometry;
			return TestContainer.Execute();
		}
	}
}
