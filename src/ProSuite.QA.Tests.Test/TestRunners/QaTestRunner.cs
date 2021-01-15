using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test.TestRunners
{
	public class QaTestRunner : QaTestRunnerBase, IDisposable
	{
		private readonly ITest _test;

		[CLSCompliant(false)]
		public QaTestRunner([NotNull] ITest test)
		{
			Assert.ArgumentNotNull(test, nameof(test));

			_test = test;

			_test.QaError += ProcessError;
		}

		public void Dispose()
		{
			_test.QaError -= ProcessError;
		}

		public override int Execute()
		{
			return _test.Execute();
		}

		[CLSCompliant(false)]
		public int Execute([NotNull] IEnvelope boundingBox)
		{
			return _test.Execute(boundingBox);
		}

		[CLSCompliant(false)]
		public int Execute([NotNull] IPolygon area)
		{
			return _test.Execute(area);
		}

		[CLSCompliant(false)]
		public int Execute([NotNull] IEnumerable<IRow> selection)
		{
			return _test.Execute(selection);
		}

		[CLSCompliant(false)]
		public int Execute([NotNull] IRow row)
		{
			return _test.Execute(row);
		}
	}
}
