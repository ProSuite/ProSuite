using System.Globalization;
using System.Threading;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Geodatabase;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaSegmentLengthTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _testWs;
		private int _errorCount;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();

			_testWs = TestWorkspaceUtils.CreateTestFgdbWorkspace("TestMinSegmentLength");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void TestFormat()
		{
			CultureInfo origCulture = Thread.CurrentThread.CurrentCulture;

			try
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

				IFeatureClass featureClass =
					TestWorkspaceUtils.CreateSimpleFeatureClass(_testWs,
					                                            "TestFormat", null,
					                                            esriGeometryType
						                                            .esriGeometryPolyline,
					                                            esriSRProjCS2Type
						                                            .esriSRProjCS_CH1903Plus_LV95);

				// make sure the table is known by the workspace
				((IWorkspaceEdit) _testWs).StartEditing(false);
				((IWorkspaceEdit) _testWs).StopEditing(true);

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

				var test = new QaSegmentLength(ReadOnlyTableFactory.Create(featureClass), 0.5, false);
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
					_testWs, "TestMultipatch", null, esriGeometryType.esriGeometryMultiPatch,
					esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, 0.001, true);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) _testWs).StartEditing(false);
			((IWorkspaceEdit) _testWs).StopEditing(true);

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
