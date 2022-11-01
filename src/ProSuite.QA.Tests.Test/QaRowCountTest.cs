using System;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaRowCountTest
	{
		private const esriGeometryType _gt = esriGeometryType.esriGeometryPolygon;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanDetectTooManyRows()
		{
			var verified = new FeatureClassMock(1, "v", _gt) { RowCountResult = 2001 };

			var test = new QaRowCount(ReadOnlyTableFactory.Create(verified), 1000, 2000);

			var runner = new QaTestRunner(test);
			runner.Execute();

			AssertUtils.OneError(runner, "RowCount.TooManyRows");
		}

		[Test]
		public void CanDetectTooFewRows()
		{
			var verified = new FeatureClassMock(1, "v", _gt) { RowCountResult = 999 };

			var test = new QaRowCount(ReadOnlyTableFactory.Create(verified), 1000, 2000);

			var runner = new QaTestRunner(test);
			runner.Execute();

			AssertUtils.OneError(runner, "RowCount.TooFewRows");
		}

		[Test]
		public void CanDetectTooManyRowsWithReferenceTables()
		{
			var verified = new FeatureClassMock(1, "v", _gt) { RowCountResult = 1001 };
			var r1 = new FeatureClassMock(2, "r1", _gt) { RowCountResult = 400 };
			var r2 = new FeatureClassMock(3, "r2", _gt) { RowCountResult = 600 };

			var test = new QaRowCount(
				ReadOnlyTableFactory.Create(verified),
				new[]
				{
					ReadOnlyTableFactory.Create(r1),
					ReadOnlyTableFactory.Create(r2)
				}, "0", "0");

			var runner = new QaTestRunner(test);
			runner.Execute();

			AssertUtils.OneError(runner, "RowCount.TooManyRows");
		}

		[Test]
		public void CanDetectTooFewRowsWithReferenceTables()
		{
			var verified = new FeatureClassMock(1, "v", _gt) { RowCountResult = 999 };
			var r1 = new FeatureClassMock(2, "r1", _gt) { RowCountResult = 400 };
			var r2 = new FeatureClassMock(3, "r2", _gt) { RowCountResult = 600 };

			var test = new QaRowCount(
				ReadOnlyTableFactory.Create(verified),
				new[] { ReadOnlyTableFactory.Create(r1), ReadOnlyTableFactory.Create(r2) }, "0",
				"0");

			var runner = new QaTestRunner(test);
			runner.Execute();

			AssertUtils.OneError(runner, "RowCount.TooFewRows");
		}

		[Test]
		public void CanDetectTooFewRowsWithPercentOffset()
		{
			var verified = new FeatureClassMock(1, "v", _gt) { RowCountResult = 899 };
			var r = new FeatureClassMock(2, "r1", _gt) { RowCountResult = 1000 };

			var test = new QaRowCount(
				ReadOnlyTableFactory.Create(verified), new[] { ReadOnlyTableFactory.Create(r) },
				"-10%", "+10%");

			var runner = new QaTestRunner(test);
			runner.Execute();

			AssertUtils.OneError(runner, "RowCount.TooFewRows");
		}

		[Test]
		public void CanDetectTooManyRowsWithPercentOffset()
		{
			var verified = new FeatureClassMock(1, "v", _gt) { RowCountResult = 1101 };
			var r = new FeatureClassMock(2, "r1", _gt) { RowCountResult = 1000 };

			var test = new QaRowCount(
				ReadOnlyTableFactory.Create(verified),
				new[] { ReadOnlyTableFactory.Create(r) }, "-10%", "+10%");

			var runner = new QaTestRunner(test);
			runner.Execute();

			AssertUtils.OneError(runner, "RowCount.TooManyRows");
		}

		[Test]
		public void CanDetectTooFewRowsWithOffset()
		{
			var verified = new FeatureClassMock(1, "v", _gt) { RowCountResult = 899 };
			var r = new FeatureClassMock(2, "r1", _gt) { RowCountResult = 1000 };

			var test = new QaRowCount(
				ReadOnlyTableFactory.Create(verified), new[] { ReadOnlyTableFactory.Create(r) },
				"-100", "+100");

			var runner = new QaTestRunner(test);
			runner.Execute();

			AssertUtils.OneError(runner, "RowCount.TooFewRows");
		}

		[Test]
		public void CanDetectTooManyRowsWithOffset()
		{
			var verified = new FeatureClassMock(1, "v", _gt) { RowCountResult = 1101 };
			var r = new FeatureClassMock(2, "r1", _gt) { RowCountResult = 1000 };

			var test = new QaRowCount(
				ReadOnlyTableFactory.Create(verified), new[] { ReadOnlyTableFactory.Create(r) },
				"-100", "+100");

			var runner = new QaTestRunner(test);
			runner.Execute();

			AssertUtils.OneError(runner, "RowCount.TooManyRows");
		}

		[Test]
		public void CanAllowRowCountAtMinimumValueWithReferenceTable()
		{
			var verified = new FeatureClassMock(1, "v", _gt) { RowCountResult = 900 };
			var r = new FeatureClassMock(2, "r1", _gt) { RowCountResult = 1000 };

			var test = new QaRowCount(
				ReadOnlyTableFactory.Create(verified), new[] { ReadOnlyTableFactory.Create(r) },
				"-10%", "+10%");

			var runner = new QaTestRunner(test);
			runner.Execute();

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanAllowRowCountAtMaximumValueWithReferenceTable()
		{
			var verified = new FeatureClassMock(1, "v", _gt) { RowCountResult = 1100 };
			var r = new FeatureClassMock(2, "r1", _gt) { RowCountResult = 1000 };

			var test = new QaRowCount(
				ReadOnlyTableFactory.Create(verified), new[] { ReadOnlyTableFactory.Create(r) },
				"-10%", "+10%");

			var runner = new QaTestRunner(test);
			runner.Execute();

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanAllowUndefinedMaximumOffset()
		{
			var verified = new FeatureClassMock(1, "v", _gt) { RowCountResult = 2000 };
			var r = new FeatureClassMock(2, "r", _gt) { RowCountResult = 1000 };

			var test = new QaRowCount(
				ReadOnlyTableFactory.Create(verified), new[] { ReadOnlyTableFactory.Create(r) },
				"0", "");

			var runner = new QaTestRunner(test);
			runner.Execute();

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanAllowUndefinedMinimumOffset()
		{
			var verified = new FeatureClassMock(1, "v", _gt) { RowCountResult = 500 };
			var r = new FeatureClassMock(2, "r", _gt) { RowCountResult = 1000 };

			var test = new QaRowCount(
				ReadOnlyTableFactory.Create(verified), new[] { ReadOnlyTableFactory.Create(r) }, "",
				"0");

			var runner = new QaTestRunner(test);
			runner.Execute();

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CantDefineTestWithoutOffset()
		{
			Assert.Throws<ArgumentException>(
				delegate
				{
					var verified = new FeatureClassMock(1, "v", _gt) { RowCountResult = 500 };
					var r = new FeatureClassMock(2, "r", _gt) { RowCountResult = 1000 };

					new QaRowCount(
						ReadOnlyTableFactory.Create(verified),
						new[] { ReadOnlyTableFactory.Create(r) }, " ", null);
				});
		}
	}
}
