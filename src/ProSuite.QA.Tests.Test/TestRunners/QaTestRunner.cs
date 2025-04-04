using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Test.TestRunners
{
	public class QaTestRunner : QaTestRunnerBase, IDisposable
	{
		private readonly ITest _test;

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

		public int Execute([NotNull] IEnvelope boundingBox)
		{
			return _test.Execute(boundingBox);
		}

		public int Execute([NotNull] IPolygon area)
		{
			return _test.Execute(area);
		}

		public int Execute([NotNull] IEnumerable<IRow> selection)
		{
			return _test.Execute(selection.Select(x => ReadOnlyRow.Create(x)));
		}

		public int Execute([NotNull] IRow row)
		{
			return _test.Execute(ReadOnlyRow.Create(row));
		}
	}
}
