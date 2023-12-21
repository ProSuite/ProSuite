using System.Globalization;
using System.Threading;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.Construction;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaSegmentLengthTest
	{
		private IFeatureWorkspace _testWs;
		private int _errorCount;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();

			_testWs = TestWorkspaceUtils.CreateTestFgdbWorkspace("TestMinSegmentLength");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void TestFormat()
		{
			CultureInfo origCulture = Thread.CurrentThread.CurrentCulture;

			try
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

				IFeatureClass featureClass =
					TestWorkspaceUtils.CreateSimpleFeatureClass(
						_testWs, "TestFormat", esriGeometryType.esriGeometryPolyline);

				// make sure the table is known by the workspace
				((IWorkspaceEdit)_testWs).StartEditing(false);
				((IWorkspaceEdit)_testWs).StopEditing(true);

				{
					// Create error Feature
					IFeature row = featureClass.CreateFeature();
					const double x = 2600000;
					const double y = 1200000;
					row.Shape =
						GeometryFactory.CreateLine(
							GeometryFactory.CreatePoint(x, y),
							GeometryFactory.CreatePoint(x + 0.3, y));
					row.Store();
				}

				var test =
					new QaSegmentLength(ReadOnlyTableFactory.Create(featureClass), 0.5, false);
				test.QaError += Test_QaFormatError;
				_errorCount = 0;
				test.Execute();
				Assert.AreEqual(1, _errorCount);
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = origCulture;
			}
		}

		[Test]
		public void TestMultipatch()
		{
			IFeatureClass featureClass =
				TestWorkspaceUtils.CreateSimpleFeatureClass(
					_testWs, "TestMultipatch", esriGeometryType.esriGeometryMultiPatch,
					xyTolerance: 0.001, hasZ: true);

			// make sure the table is known by the workspace
			((IWorkspaceEdit)_testWs).StartEditing(false);
			((IWorkspaceEdit)_testWs).StopEditing(true);

			{
				// Create error Feature
				IFeature row = featureClass.CreateFeature();
				const double x = 2600000;
				const double y = 1200000;
				const double z = 500;
				row.Shape = new MultiPatchConstruction().StartRing(x, y, z)
														.Add(x + 0.3, y, z)
														.Add(x + 0.3, y, z + 10)
														.MultiPatch;

				row.Store();
			}

			var test = new QaSegmentLength(ReadOnlyTableFactory.Create(featureClass), 0.5);
			test.QaError += Test_QaFormatError;
			_errorCount = 0;
			test.Execute();
			Assert.AreEqual(1, _errorCount);
		}

		private void Test_QaFormatError(object sender, QaErrorEventArgs e)
		{
			string expected = string.Format("Segment length {0}  < {1} ", 0.3, 0.5);

			Assert.AreEqual(expected, e.QaError.Description);
			_errorCount++;
		}
	}
}
